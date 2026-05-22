import os
import queue
import threading
import time
import urllib.request
from dataclasses import dataclass
from pathlib import Path
import tkinter as tk
from tkinter import filedialog, messagebox, ttk

import cv2
import numpy as np
from ultralytics import YOLO

MODEL_DIR = Path("models")
MODEL_PATH = MODEL_DIR / "yolov8s.pt"
YOLOV8S_URL = "https://github.com/ultralytics/assets/releases/download/v8.2.0/yolov8s.pt"
RETRAIN_CAPTURE_DIR = Path("retrain_capture")


@dataclass
class Track:
    track_id: int
    label: str
    first_seen: float
    last_seen: float
    hits: int
    consecutive_hits: int
    miss_count: int
    best_conf: float
    bbox: tuple[int, int, int, int]
    in_alert_roi: bool


class SimpleTracker:
    def __init__(self, dist_threshold=90.0, max_miss=12):
        self.dist_threshold = dist_threshold
        self.max_miss = max_miss
        self.next_id = 1
        self.tracks: dict[int, Track] = {}

    @staticmethod
    def _center(bbox):
        x1, y1, x2, y2 = bbox
        return ((x1 + x2) / 2.0, (y1 + y2) / 2.0)

    def update(self, detections, now):
        matched_track_ids = set()

        for det in detections:
            label, conf, bbox, in_alert_roi = det
            cx, cy = self._center(bbox)
            best_id = None
            best_dist = float("inf")

            for tid, tr in self.tracks.items():
                if tr.label != label:
                    continue
                tx, ty = self._center(tr.bbox)
                dist = float(np.hypot(cx - tx, cy - ty))
                if dist < best_dist and dist <= self.dist_threshold and tid not in matched_track_ids:
                    best_dist = dist
                    best_id = tid

            if best_id is None:
                tid = self.next_id
                self.next_id += 1
                self.tracks[tid] = Track(
                    track_id=tid,
                    label=label,
                    first_seen=now,
                    last_seen=now,
                    hits=1,
                    consecutive_hits=1,
                    miss_count=0,
                    best_conf=conf,
                    bbox=bbox,
                    in_alert_roi=in_alert_roi,
                )
                matched_track_ids.add(tid)
            else:
                tr = self.tracks[best_id]
                tr.last_seen = now
                tr.hits += 1
                tr.consecutive_hits += 1
                tr.miss_count = 0
                tr.best_conf = max(tr.best_conf, conf)
                tr.bbox = bbox
                tr.in_alert_roi = in_alert_roi
                matched_track_ids.add(best_id)

        to_delete = []
        for tid, tr in self.tracks.items():
            if tid not in matched_track_ids:
                tr.miss_count += 1
                tr.consecutive_hits = max(0, tr.consecutive_hits - 1)
                if tr.miss_count > self.max_miss:
                    to_delete.append(tid)
        for tid in to_delete:
            del self.tracks[tid]

    def stable_tracks(self, now, min_persistence_sec, min_hits):
        out = []
        for tr in self.tracks.values():
            persistence = now - tr.first_seen
            if persistence >= min_persistence_sec and tr.hits >= min_hits:
                out.append(tr)
        return out


class FrameGrabber:
    def __init__(self, app, source, is_rtsp):
        self.app = app
        self.source = source
        self.is_rtsp = is_rtsp
        self.latest_frame = None
        self.lock = threading.Lock()
        self.running = False
        self.thread = None
        self.last_ok_ts = 0.0
        self.cap = None
        self.reconnect_count = 0
        self.read_fail_count = 0
        self.frames_captured = 0

    def start(self):
        self.running = True
        self.thread = threading.Thread(target=self._loop, daemon=True)
        self.thread.start()

    def stop(self):
        self.running = False
        if self.thread is not None and self.thread.is_alive():
            self.thread.join(timeout=2.0)
        self.thread = None
        if self.cap is not None:
            self.cap.release()
            self.cap = None

    def read_latest(self):
        with self.lock:
            if self.latest_frame is None:
                return False, None
            return True, self.latest_frame.copy()

    def _loop(self):
        reconnect_sleep = 0.25
        while self.running:
            if self.cap is None or not self.cap.isOpened():
                self.cap = self.app._open_capture(self.source)
                if self.cap is None or not self.cap.isOpened():
                    time.sleep(reconnect_sleep)
                    continue
                self.reconnect_count += 1

            ok, frame = self.cap.read()
            if not ok:
                self.read_fail_count += 1
                if self.cap is not None:
                    self.cap.release()
                self.cap = None
                time.sleep(reconnect_sleep if self.is_rtsp else 0.05)
                continue

            self.last_ok_ts = time.time()
            self.frames_captured += 1
            with self.lock:
                # Keep only newest frame to avoid stale-buffer lag.
                self.latest_frame = frame


class FireSmokeApp:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("Commercial Fire/Smoke Guard - YOLOv8 Small")
        self.root.geometry("760x560")

        self.source_mode = tk.StringVar(value="rtsp")
        self.source_input = tk.StringVar(value="0")
        self.video_path = tk.StringVar()
        self.model_path = tk.StringVar(value=str(MODEL_PATH))
        self.rtsp_transport = tk.StringVar(value="tcp")

        self.conf_thresh = tk.DoubleVar(value=0.45)
        self.persistence_sec = tk.DoubleVar(value=3.0)
        self.min_hits = tk.IntVar(value=8)

        self.alert_rois_text = tk.StringVar(value="")
        self.ignore_rois_text = tk.StringVar(value="")

        self.enable_sensor_fusion = tk.BooleanVar(value=True)
        self.temp_value = tk.DoubleVar(value=30.0)
        self.temp_threshold = tk.DoubleVar(value=55.0)
        self.smoke_sensor_on = tk.BooleanVar(value=False)
        self.preview_denoise = tk.BooleanVar(value=True)

        self.save_conf_thresh = 0.01
        self.save_min_interval_sec = 0.25
        self.save_queue: queue.Queue = queue.Queue(maxsize=256)
        self.save_thread = None
        self.save_thread_running = False
        self.last_saved_at = 0.0

        self.is_running = False
        self.worker_thread = None

        self._build_ui()

    def _build_ui(self):
        frame = ttk.Frame(self.root, padding=12)
        frame.pack(fill=tk.BOTH, expand=True)

        ttk.Label(frame, text="3-Layer Fire/Smoke Detection (YOLO + Tracking + Business Logic)", font=("Segoe UI", 12, "bold")).pack(anchor=tk.W, pady=(0, 10))

        mode_box = ttk.LabelFrame(frame, text="Input", padding=10)
        mode_box.pack(fill=tk.X, pady=4)
        ttk.Radiobutton(mode_box, text="RTSP/Camera", value="rtsp", variable=self.source_mode, command=self._toggle_source).grid(row=0, column=0, sticky="w")
        ttk.Radiobutton(mode_box, text="Video", value="video", variable=self.source_mode, command=self._toggle_source).grid(row=0, column=1, sticky="w", padx=10)
        self.input_entry = ttk.Entry(mode_box, textvariable=self.source_input)
        self.input_entry.grid(row=1, column=0, columnspan=2, sticky="ew", pady=4)
        self.video_entry = ttk.Entry(mode_box, textvariable=self.video_path)
        self.video_entry.grid(row=2, column=0, sticky="ew", pady=4)
        self.video_btn = ttk.Button(mode_box, text="Browse", command=self._pick_video)
        self.video_btn.grid(row=2, column=1, sticky="ew", padx=(8, 0))
        ttk.Label(mode_box, text="RTSP transport").grid(row=3, column=0, sticky="w", pady=(4, 0))
        ttk.Combobox(mode_box, textvariable=self.rtsp_transport, values=["tcp", "udp"], width=8, state="readonly").grid(row=3, column=1, sticky="w", pady=(4, 0))
        mode_box.columnconfigure(0, weight=1)

        det_box = ttk.LabelFrame(frame, text="Layer 1 + 2: YOLO & Tracking", padding=10)
        det_box.pack(fill=tk.X, pady=4)
        ttk.Label(det_box, text="Confidence").grid(row=0, column=0, sticky="w")
        ttk.Entry(det_box, textvariable=self.conf_thresh, width=8).grid(row=0, column=1, sticky="w")
        ttk.Label(det_box, text="Persistence sec").grid(row=0, column=2, sticky="w", padx=(10, 0))
        ttk.Entry(det_box, textvariable=self.persistence_sec, width=8).grid(row=0, column=3, sticky="w")
        ttk.Label(det_box, text="Min hits per ID").grid(row=0, column=4, sticky="w", padx=(10, 0))
        ttk.Entry(det_box, textvariable=self.min_hits, width=8).grid(row=0, column=5, sticky="w")

        roi_box = ttk.LabelFrame(frame, text="Layer 3: ROI Business Rules", padding=10)
        roi_box.pack(fill=tk.X, pady=4)
        ttk.Label(roi_box, text="Alert ROI rectangles (x1,y1,x2,y2;...)").grid(row=0, column=0, sticky="w")
        ttk.Entry(roi_box, textvariable=self.alert_rois_text).grid(row=1, column=0, sticky="ew", pady=4)
        ttk.Label(roi_box, text="Ignore ROI rectangles (x1,y1,x2,y2;...)").grid(row=2, column=0, sticky="w")
        ttk.Entry(roi_box, textvariable=self.ignore_rois_text).grid(row=3, column=0, sticky="ew", pady=4)
        roi_box.columnconfigure(0, weight=1)

        sensor_box = ttk.LabelFrame(frame, text="Layer 3: Multi-sensor Fusion", padding=10)
        sensor_box.pack(fill=tk.X, pady=4)
        ttk.Checkbutton(sensor_box, text="Enable sensor fusion", variable=self.enable_sensor_fusion).grid(row=0, column=0, sticky="w")
        ttk.Label(sensor_box, text="Temp now (C)").grid(row=1, column=0, sticky="w")
        ttk.Entry(sensor_box, textvariable=self.temp_value, width=8).grid(row=1, column=1, sticky="w")
        ttk.Label(sensor_box, text="Temp threshold").grid(row=1, column=2, sticky="w", padx=(10, 0))
        ttk.Entry(sensor_box, textvariable=self.temp_threshold, width=8).grid(row=1, column=3, sticky="w")
        ttk.Checkbutton(sensor_box, text="Smoke IoT sensor ON", variable=self.smoke_sensor_on).grid(row=1, column=4, sticky="w", padx=(12, 0))
        ttk.Checkbutton(sensor_box, text="Preview anti-noise", variable=self.preview_denoise).grid(row=0, column=1, sticky="w", padx=(10, 0))

        model_box = ttk.LabelFrame(frame, text="Model", padding=10)
        model_box.pack(fill=tk.X, pady=4)
        ttk.Entry(model_box, textvariable=self.model_path).pack(fill=tk.X)

        btns = ttk.Frame(frame)
        btns.pack(fill=tk.X, pady=(10, 0))
        self.start_btn = ttk.Button(btns, text="Start", command=self.start)
        self.start_btn.pack(side=tk.LEFT)
        self.stop_btn = ttk.Button(btns, text="Stop", command=self.stop, state=tk.DISABLED)
        self.stop_btn.pack(side=tk.LEFT, padx=8)
        ttk.Button(btns, text="Download YOLOv8s", command=self.download_model).pack(side=tk.LEFT)

        ttk.Label(frame, text="Model goc YOLOv8s chua co class fire/smoke. Sau khi train, thay Model path bang file .pt moi.", foreground="#5b6470", wraplength=730).pack(anchor=tk.W, pady=(8, 0))
        ttk.Label(frame, text=f"Raw frame saver: ON -> {RETRAIN_CAPTURE_DIR} (low-conf={self.save_conf_thresh})", foreground="#4b5563", wraplength=730).pack(anchor=tk.W, pady=(3, 0))

        self._toggle_source()

    def _toggle_source(self):
        rtsp = self.source_mode.get() == "rtsp"
        self.input_entry.configure(state=tk.NORMAL if rtsp else tk.DISABLED)
        self.video_entry.configure(state=tk.DISABLED if rtsp else tk.NORMAL)
        self.video_btn.configure(state=tk.DISABLED if rtsp else tk.NORMAL)

    def _pick_video(self):
        path = filedialog.askopenfilename(filetypes=[("Video", "*.mp4;*.avi;*.mkv;*.mov"), ("All", "*.*")])
        if path:
            self.video_path.set(path)

    def download_model(self):
        try:
            MODEL_DIR.mkdir(parents=True, exist_ok=True)
            if not MODEL_PATH.exists():
                urllib.request.urlretrieve(YOLOV8S_URL, MODEL_PATH)
            self.model_path.set(str(MODEL_PATH))
            messagebox.showinfo("Done", f"Model ready at:\n{MODEL_PATH}")
        except Exception as exc:
            messagebox.showerror("Download error", str(exc))

    def _parse_rois(self, text):
        rois = []
        raw = text.strip()
        if not raw:
            return rois
        for seg in raw.split(";"):
            vals = [v.strip() for v in seg.split(",")]
            if len(vals) != 4:
                raise ValueError("ROI format must be x1,y1,x2,y2;...")
            x1, y1, x2, y2 = map(int, vals)
            rois.append((min(x1, x2), min(y1, y2), max(x1, x2), max(y1, y2)))
        return rois

    @staticmethod
    def _inside_any_roi(bbox, rois):
        if not rois:
            return True
        x1, y1, x2, y2 = bbox
        cx = (x1 + x2) // 2
        cy = (y1 + y2) // 2
        for rx1, ry1, rx2, ry2 in rois:
            if rx1 <= cx <= rx2 and ry1 <= cy <= ry2:
                return True
        return False

    def start(self):
        if self.is_running:
            return
        try:
            model_path = Path(self.model_path.get().strip())
            if not model_path.exists():
                self.download_model()
            if not model_path.exists():
                raise ValueError("Model file not found")

            source = self._resolve_source()
            conf = float(self.conf_thresh.get())
            persistence_sec = float(self.persistence_sec.get())
            min_hits = int(self.min_hits.get())
            alert_rois = self._parse_rois(self.alert_rois_text.get())
            ignore_rois = self._parse_rois(self.ignore_rois_text.get())
            temp_now = float(self.temp_value.get())
            temp_th = float(self.temp_threshold.get())
            sensor_smoke = bool(self.smoke_sensor_on.get())
            if persistence_sec <= 0 or min_hits <= 0:
                raise ValueError("Persistence sec and min hits must be > 0")
        except Exception as exc:
            messagebox.showerror("Input error", str(exc))
            return

        self.is_running = True
        self.start_btn.configure(state=tk.DISABLED)
        self.stop_btn.configure(state=tk.NORMAL)
        self._start_save_thread()
        self.worker_thread = threading.Thread(
            target=self._run_detection,
            args=(str(model_path), source, conf, persistence_sec, min_hits, alert_rois, ignore_rois),
            daemon=True,
        )
        self.worker_thread.start()

    def stop(self):
        self.is_running = False
        self._stop_save_thread()

    def _resolve_source(self):
        if self.source_mode.get() == "video":
            path = self.video_path.get().strip()
            if not path:
                raise ValueError("Missing video file")
            if not Path(path).exists():
                raise ValueError("Video file not found")
            return path

        value = self.source_input.get().strip()
        if value.isdigit():
            return int(value)
        if value.startswith("rtsp://") or value.startswith("http://") or value.startswith("https://"):
            return value
        raise ValueError("Invalid RTSP URL or camera index")

    def _run_detection(self, model_path, source, conf_thresh, persistence_sec, min_hits, alert_rois, ignore_rois):
        is_rtsp = isinstance(source, str) and source.startswith("rtsp://")
        grabber = FrameGrabber(self, source, is_rtsp=is_rtsp)
        grabber.start()
        wait_open_start = time.time()
        while self.is_running:
            ok, _ = grabber.read_latest()
            if ok:
                break
            if time.time() - wait_open_start > 8.0:
                self._notify_ui_error("Cannot open video source")
                grabber.stop()
                self._reset_controls()
                return
            time.sleep(0.05)

        model = YOLO(model_path)
        names = model.names if isinstance(model.names, dict) else {}
        fire_labels = {"fire", "flame", "burning"}
        smoke_labels = {"smoke", "fume"}

        tracker = SimpleTracker(dist_threshold=90.0, max_miss=12)
        infer_conf = min(conf_thresh, self.save_conf_thresh)
        fps_counter_start = time.time()
        fps_counter_frames = 0
        display_fps = 0.0
        processed_frames = 0
        dropped_frames = 0

        while self.is_running:
            ok, frame = grabber.read_latest()
            if not ok:
                # Reader thread is reconnecting in background; wait a bit.
                time.sleep(0.01)
                continue
            processed_frames += 1
            dropped_frames = max(0, grabber.frames_captured - processed_frames)

            fps_counter_frames += 1
            now_fps = time.time()
            if now_fps - fps_counter_start >= 1.0:
                display_fps = fps_counter_frames / (now_fps - fps_counter_start)
                fps_counter_start = now_fps
                fps_counter_frames = 0

            raw_frame = frame.copy()
            now = time.time()
            results = model.predict(frame, conf=infer_conf, verbose=False)
            dets = []
            save_candidate = False

            if results:
                boxes = results[0].boxes
                if boxes is not None and len(boxes) > 0:
                    for box in boxes:
                        cls_id = int(box.cls[0].item())
                        conf = float(box.conf[0].item())
                        label = str(names.get(cls_id, cls_id)).lower()
                        if label not in fire_labels and label not in smoke_labels:
                            continue
                        bbox = tuple(map(int, box.xyxy[0].tolist()))
                        save_candidate = True

                        # Business alert/tracking path keeps the stricter operator threshold.
                        if conf < conf_thresh:
                            continue

                        if ignore_rois and self._inside_any_roi(bbox, ignore_rois):
                            continue

                        in_alert_roi = self._inside_any_roi(bbox, alert_rois)
                        dets.append((label, conf, bbox, in_alert_roi))

            if save_candidate:
                self._enqueue_raw_frame(raw_frame, now)

            tracker.update(dets, now)
            stable = tracker.stable_tracks(now, persistence_sec, min_hits)

            fire_stable = [t for t in stable if t.label in fire_labels and t.in_alert_roi]
            smoke_stable = [t for t in stable if t.label in smoke_labels and t.in_alert_roi]

            sensor_fusion = bool(self.enable_sensor_fusion.get())
            temp_now = float(self.temp_value.get())
            temp_th = float(self.temp_threshold.get())
            smoke_iot = bool(self.smoke_sensor_on.get())
            temp_high = temp_now >= temp_th

            loud_alarm = False
            silent_alert = False
            if sensor_fusion:
                if (fire_stable or smoke_stable) and (temp_high or smoke_iot):
                    loud_alarm = True
                elif fire_stable or smoke_stable or temp_high or smoke_iot:
                    silent_alert = True
            else:
                loud_alarm = bool(fire_stable or smoke_stable)

            for rx1, ry1, rx2, ry2 in alert_rois:
                cv2.rectangle(frame, (rx1, ry1), (rx2, ry2), (0, 165, 255), 2)
                cv2.putText(frame, "ALERT ROI", (rx1, max(12, ry1 - 6)), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 165, 255), 2)
            for rx1, ry1, rx2, ry2 in ignore_rois:
                cv2.rectangle(frame, (rx1, ry1), (rx2, ry2), (120, 120, 120), 2)
                cv2.putText(frame, "IGNORE ROI", (rx1, max(12, ry1 - 6)), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (120, 120, 120), 2)

            for tr in tracker.tracks.values():
                x1, y1, x2, y2 = tr.bbox
                color = (40, 180, 255)
                if tr.label in fire_labels:
                    color = (0, 0, 255)
                elif tr.label in smoke_labels:
                    color = (170, 170, 170)
                cv2.rectangle(frame, (x1, y1), (x2, y2), color, 2)
                age = now - tr.first_seen
                tag = f"{tr.label} ID:{tr.track_id} hit:{tr.hits} t:{age:.1f}s"
                cv2.putText(frame, tag, (x1, max(12, y1 - 8)), cv2.FONT_HERSHEY_SIMPLEX, 0.52, color, 2)

            status = "SAFE"
            status_color = (0, 200, 0)
            if silent_alert:
                status = "SILENT ALERT (CHECK GUARD APP)"
                status_color = (0, 215, 255)
            if loud_alarm:
                status = "LOUD ALARM: FIRE/SMOKE CONFIRMED"
                status_color = (0, 0, 255)

            cv2.putText(frame, status, (14, 32), cv2.FONT_HERSHEY_SIMPLEX, 0.85, status_color, 3)
            cv2.putText(
                frame,
                f"stable_fire={len(fire_stable)} stable_smoke={len(smoke_stable)} temp={temp_now:.1f}/{temp_th:.1f}C iot_smoke={int(smoke_iot)}",
                (14, 60),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.56,
                (255, 255, 255),
                2,
            )
            cv2.putText(
                frame,
                f"fps={display_fps:.1f} cap={grabber.frames_captured} proc={processed_frames} drop={dropped_frames} reconn={grabber.reconnect_count} rfails={grabber.read_fail_count}",
                (14, 88),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.52,
                (255, 255, 0),
                2,
            )

            if self.preview_denoise.get():
                frame = cv2.GaussianBlur(frame, (3, 3), 0)

            cv2.imshow("Commercial Fire/Smoke Guard", frame)
            key = cv2.waitKey(1) & 0xFF
            if key == ord("q"):
                self.is_running = False
                break

        grabber.stop()
        cv2.destroyAllWindows()
        self._stop_save_thread()
        self._reset_controls()

    def _open_capture(self, source):
        if isinstance(source, str) and source.startswith("rtsp://"):
            transport = self.rtsp_transport.get().strip().lower()
            if transport == "udp":
                os.environ["OPENCV_FFMPEG_CAPTURE_OPTIONS"] = "rtsp_transport;udp"
            else:
                os.environ["OPENCV_FFMPEG_CAPTURE_OPTIONS"] = "rtsp_transport;tcp"
            cap = cv2.VideoCapture(source, cv2.CAP_FFMPEG)
            # Keep latency and stale frame buildup low for unstable networks.
            cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)
            return cap
        cap = cv2.VideoCapture(source)
        cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)
        return cap

    def _start_save_thread(self):
        RETRAIN_CAPTURE_DIR.mkdir(parents=True, exist_ok=True)
        self.save_thread_running = True
        self.save_thread = threading.Thread(target=self._save_worker, daemon=True)
        self.save_thread.start()

    def _stop_save_thread(self):
        self.save_thread_running = False
        if self.save_thread is not None and self.save_thread.is_alive():
            self.save_thread.join(timeout=2.0)
        self.save_thread = None

    def _enqueue_raw_frame(self, frame, now_ts):
        if now_ts - self.last_saved_at < self.save_min_interval_sec:
            return
        self.last_saved_at = now_ts
        try:
            self.save_queue.put_nowait((now_ts, frame))
        except queue.Full:
            # Drop frames when IO cannot keep up to protect realtime detection.
            pass

    def _save_worker(self):
        while self.save_thread_running or not self.save_queue.empty():
            try:
                ts, frame = self.save_queue.get(timeout=0.2)
            except queue.Empty:
                continue

            ms = int((ts - int(ts)) * 1000)
            stamp = time.strftime("%Y%m%d_%H%M%S", time.localtime(ts))
            out_path = RETRAIN_CAPTURE_DIR / f"{stamp}_{ms:03d}.jpg"
            cv2.imwrite(str(out_path), frame)
            self.save_queue.task_done()

    def _notify_ui_error(self, message):
        self.root.after(0, lambda: messagebox.showerror("Runtime error", message))

    def _reset_controls(self):
        self.is_running = False

        def _reset():
            self.start_btn.configure(state=tk.NORMAL)
            self.stop_btn.configure(state=tk.DISABLED)

        self.root.after(0, _reset)


def main():
    os.makedirs(MODEL_DIR, exist_ok=True)
    root = tk.Tk()
    style = ttk.Style(root)
    if "vista" in style.theme_names():
        style.theme_use("vista")
    app = FireSmokeApp(root)
    root.protocol("WM_DELETE_WINDOW", lambda: (app.stop(), root.destroy()))
    root.mainloop()


if __name__ == "__main__":
    main()
