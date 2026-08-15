"""Isolated camera capture and recognition state for the Face ID runtime."""

from __future__ import annotations

import base64
import threading
import time
import uuid
from collections import deque
from datetime import datetime, timezone
from typing import Any, Callable

import cv2
import numpy as np

from face_detector import FaceDetector
from template_store import cosine_distance


MAX_READ_FAILS_BEFORE_WARN = 20
RECONNECT_DELAY_SEC = 1.0


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def _legacy_timestamp() -> str:
    return time.strftime("%Y-%m-%d %H:%M:%S")


def _loc_to_box(loc: tuple[int, int, int, int]) -> dict[str, int]:
    top, right, bottom, left = loc
    return {
        "top": int(top), "right": int(right), "bottom": int(bottom),
        "left": int(left), "width": int(right - left), "height": int(bottom - top),
    }


def _iou(a: dict[str, int] | None, b: tuple[int, int, int, int]) -> float:
    if not a:
        return 0.0
    top, right, bottom, left = b
    xa1, ya1 = float(a.get("left", 0)), float(a.get("top", 0))
    xa2, ya2 = float(a.get("right", 0)), float(a.get("bottom", 0))
    xb1, yb1 = float(left), float(top)
    xb2, yb2 = float(right), float(bottom)
    ix1, iy1 = max(xa1, xb1), max(ya1, yb1)
    ix2, iy2 = min(xa2, xb2), min(ya2, yb2)
    iw, ih = max(0.0, ix2 - ix1), max(0.0, iy2 - iy1)
    inter = iw * ih
    a_area = max(0.0, xa2 - xa1) * max(0.0, ya2 - ya1)
    b_area = max(0.0, xb2 - xb1) * max(0.0, yb2 - yb1)
    union = a_area + b_area - inter
    if union <= 0:
        return 0.0
    return inter / union


class CameraSessionConflictError(RuntimeError):
    """Raised when an active camera is started with a different stream URL."""


class CameraSession:
    """Owns every mutable value and worker associated with one camera."""

    def __init__(
        self,
        camera_id: str,
        model_registry: Any,
        config: Any,
        *,
        capture_factory: Callable[[str], Any] | None = None,
        detector: Any | None = None,
        embedder: Any | None = None,
    ) -> None:
        self.camera_id = camera_id
        self.lane_id: str | None = None
        self.stream_url = ""
        self.enabled = False
        self.connection_status = "stopped"
        self.last_error: str | None = None
        self.capture: Any | None = None
        self.capture_worker: threading.Thread | None = None
        self.recognition_worker: threading.Thread | None = None
        self.stop_event = threading.Event()

        self.latest_frame: np.ndarray | None = None
        self.latest_frame_at = 0.0
        self.latest_jpeg: bytes | None = None
        self.latest_display_frame: np.ndarray | None = None

        self.recognition_result: dict[str, Any] = {}
        self.confirmed_subject_id: str | None = None
        self.confirmed_frames = 0
        self.last_face_seen_at = 0.0
        self.last_recognition_at = 0.0
        self.cooldown_state: dict[str, Any] = {
            "tracking_started_at": 0.0,
            "alert_triggered": False,
            "distance_buffer": deque(maxlen=5),
        }
        self.locked_images: dict[str, str | bytes | None] = {
            "frame_jpeg": None,
            "snapshot": None,
            "face_crop": None,
        }

        # Multi-face: live faces in the current frame + intruder captures.
        self.faces: list[dict[str, Any]] = []
        self._last_face_entries: list[dict[str, Any]] = []
        self.intruders: list[dict[str, Any]] = []
        self._intruder_keys: set[tuple] = set()
        self._max_faces = int(getattr(config, "enrollment_auto_target", 6) or 6)
        # Session-based multi-face tracking: track_id -> state dict.
        self._tracks: dict[str, dict[str, Any]] = {}
        self._next_track_id = 1

        self.session_lock = threading.RLock()
        self.frame_lock = threading.Lock()
        # OpenCV/FFmpeg can segfault when release() races with a blocking read().
        # Serialize native capture I/O so stop/reset never frees the capture while
        # the worker is still using it.
        self.capture_io_lock = threading.Lock()
        self.event_lock = threading.Lock()
        self._events: deque[dict[str, Any]] = deque(
            maxlen=getattr(config, "event_buffer_size", 500))
        self._event_sequence = 0
        self.generation = 0
        self.created_at = _utc_now()
        self.updated_at = self.created_at

        self._model_registry = model_registry
        self._config = config
        self._capture_factory = capture_factory
        self._detector = detector
        self._embedder = embedder
        self._tracking_active = False
        self._identity_confirmed = False
        self._last_face_box: dict[str, int] | None = None
        self._last_distance: float | None = None
        self._last_face_match = False
        self._last_timeout = False
        self._last_alert = False
        self._scan_locked = False
        self._lock_reason: str | None = None
        self._last_snapshot: str | None = None
        self._last_face_crop: str | None = None
        self._fps = 0
        self._message = "System initialized"
        self._last_update: str | None = None
        self._reset_recognition_locked("System initialized")

    def start(self, stream_url: str, lane_id: str | None = None) -> bool:
        """Start workers, returning True when the operation is idempotent."""
        clean_url = str(stream_url or "").strip()
        if not clean_url:
            raise ValueError("Camera stream URL is required.")

        with self.session_lock:
            if self.enabled:
                if self.stream_url != clean_url:
                    raise CameraSessionConflictError(
                        "Camera session is already active with another stream URL."
                    )
                if lane_id is not None:
                    self.lane_id = str(lane_id)
                    self._touch_locked()
                return True

            self.generation += 1
            generation = self.generation
            self.stop_event = threading.Event()
            self.stream_url = clean_url
            self.lane_id = None if lane_id is None else str(lane_id)
            self.enabled = True
            self.connection_status = "connecting"
            self.last_error = None
            self._reset_recognition_locked("Waiting camera worker to connect...")
            stop_event = self.stop_event
            self.capture_worker = threading.Thread(
                target=self._capture_loop,
                args=(generation, stop_event),
                name=f"face-capture-{self.camera_id}-{generation}",
                daemon=True,
            )
            self.recognition_worker = threading.Thread(
                target=self._recognition_loop,
                args=(generation, stop_event),
                name=f"face-recognition-{self.camera_id}-{generation}",
                daemon=True,
            )
            capture_worker = self.capture_worker
            recognition_worker = self.recognition_worker

        capture_worker.start()
        recognition_worker.start()
        return False

    def stop(self) -> None:
        with self.session_lock:
            self.generation += 1
            self.enabled = False
            self.connection_status = "stopped"
            self.last_error = None
            stop_event = self.stop_event
            stop_event.set()
            capture = self.capture
            self.capture = None
            workers = (self.capture_worker, self.recognition_worker)
            self.capture_worker = None
            self.recognition_worker = None
            self.stream_url = ""
            self.lane_id = None
            self._touch_locked()

        with self.capture_io_lock:
            self._release_capture(capture)
        self._join_workers(workers)
        with self.frame_lock:
            self.latest_frame = None
            self.latest_frame_at = 0.0
            self.latest_jpeg = None
            self.latest_display_frame = None
        with self.session_lock:
            self._reset_recognition_locked("Camera closed")

    def reset(self) -> None:
        """Reset recognition and invalidate old workers without changing the URL."""
        with self.session_lock:
            was_enabled = self.enabled
            stream_url = self.stream_url
            lane_id = self.lane_id
            self.generation += 1
            generation = self.generation
            old_event = self.stop_event
            old_event.set()
            capture = self.capture
            self.capture = None
            workers = (self.capture_worker, self.recognition_worker)
            self.capture_worker = None
            self.recognition_worker = None
            self.connection_status = "connecting" if was_enabled else "stopped"
            self.last_error = None
            self._reset_recognition_locked("ÄÃ£ reset tráº¡ng thÃ¡i nháº­n diá»‡n")

        with self.capture_io_lock:
            self._release_capture(capture)
        self._join_workers(workers)
        with self.frame_lock:
            self.latest_frame = None
            self.latest_frame_at = 0.0
            self.latest_jpeg = None
            self.latest_display_frame = None

        if not was_enabled:
            return

        with self.session_lock:
            if not self.enabled or self.generation != generation:
                return
            self.stream_url = stream_url
            self.lane_id = lane_id
            self.stop_event = threading.Event()
            stop_event = self.stop_event
            self.capture_worker = threading.Thread(
                target=self._capture_loop,
                args=(generation, stop_event),
                name=f"face-capture-{self.camera_id}-{generation}",
                daemon=True,
            )
            self.recognition_worker = threading.Thread(
                target=self._recognition_loop,
                args=(generation, stop_event),
                name=f"face-recognition-{self.camera_id}-{generation}",
                daemon=True,
            )
            capture_worker = self.capture_worker
            recognition_worker = self.recognition_worker
        capture_worker.start()
        recognition_worker.start()

    def metadata(self) -> dict[str, Any]:
        with self.session_lock:
            return {
                "cameraId": self.camera_id,
                "laneId": self.lane_id,
                "enabled": self.enabled,
                "connected": self.connection_status == "connected",
                "status": self.connection_status,
                "createdAt": self.created_at,
                "updatedAt": self.updated_at,
            }

    def status(self) -> dict[str, Any]:
        snapshot = self.result(include_images=False)
        return {
            "success": True,
            "camera_enabled": snapshot["camera_enabled"],
            "camera_connected": snapshot["camera_connected"],
            "ip": snapshot["ip"],
            "tracking_active": snapshot["tracking_active"],
            "identity_confirmed": snapshot["identity_confirmed"],
            "face_match": snapshot["face_match"],
            "employee_id": snapshot["employee_id"],
            "confirm_count": snapshot["confirm_count"],
            "distance": snapshot["distance"],
            "last_seen": snapshot["last_seen"],
            "bbox": snapshot["bbox"],
            "faces": snapshot.get("faces") or [],
            "timeout": snapshot["timeout"],
            "alert": snapshot["alert"],
            "scan_locked": snapshot["scan_locked"],
            "lock_reason": snapshot["lock_reason"],
            "fps": snapshot["fps"],
            "models_loaded": snapshot["models_loaded"],
            "total_encodings": snapshot["total_encodings"],
            "message": snapshot["message"],
            "last_update": snapshot["last_update"],
            "stream_url": "/api/camera/stream" if snapshot["camera_enabled"] else "",
        }

    def result(self, *, include_images: bool = True) -> dict[str, Any]:
        model_snapshot = self._model_registry.current_snapshot()
        with self.session_lock:
            payload = dict(self.recognition_result)
            payload.update(
                {
                    "success": True,
                    "camera_enabled": self.enabled,
                    "camera_connected": self.connection_status == "connected",
                    "ip": self.stream_url,
                    "face_model_dir": str(self._config.model_dir),
                    "models_loaded": model_snapshot.successful_file_count,
                    "total_encodings": model_snapshot.encoding_count,
                    "last_snapshot": self._last_snapshot if include_images else None,
                    "last_face_crop": self._last_face_crop if include_images else None,
                    "locked_snapshot": self.locked_images["snapshot"] if include_images else None,
                    "locked_face_crop": self.locked_images["face_crop"] if include_images else None,
                }
            )
            return payload

    def locked_image_result(self) -> dict[str, Any]:
        with self.session_lock:
            return {
                "success": True,
                "identity_confirmed": self._identity_confirmed,
                "employee_id": self.confirmed_subject_id,
                "scan_locked": self._scan_locked,
                "lock_reason": self._lock_reason,
                "locked_snapshot": self.locked_images["snapshot"],
                "locked_face_crop": self.locked_images["face_crop"],
            }

    def events(
        self, *, after_sequence: int = 0,
        session_generation: int | None = None, limit: int = 100,
    ) -> dict[str, Any]:
        if after_sequence < 0 or limit < 1 or limit > 200:
            raise ValueError("Event query is invalid.")
        now = time.time()
        with self.event_lock:
            self._prune_events_locked(now)
            generation = self.generation
            current = [event for event in self._events
                       if event["sessionGeneration"] == generation]
            oldest = current[0]["sequence"] if current else None
            latest = current[-1]["sequence"] if current else 0
            generation_reset = (
                session_generation is not None and
                session_generation != generation)
            gap = generation_reset or (
                oldest is not None and after_sequence > 0 and
                after_sequence < oldest - 1)
            selected = [event for event in current
                        if event["sequence"] > after_sequence][:limit]
            has_more = bool(selected and latest > selected[-1]["sequence"])
            return {
                "cameraId": self.camera_id,
                "sessionGeneration": generation,
                "oldestSequence": oldest,
                "latestSequence": latest,
                "events": [
                    {key: value for key, value in event.items()
                     if not key.startswith("_")}
                    for event in selected
                ],
                "hasMore": has_more,
                "gapDetected": gap,
            }

    def latest_frame_copy(self) -> tuple[np.ndarray | None, float]:
        with self.frame_lock:
            if self.latest_frame is None:
                return None, 0.0
            return self.latest_frame.copy(), self.latest_frame_at

    def mjpeg_generator(self):
        while True:
            with self.session_lock:
                if not self.enabled and self.stop_event.is_set():
                    break
                enabled = self.enabled
                connected = self.connection_status == "connected"
                locked = self._scan_locked
                locked_payload = self.locked_images["frame_jpeg"]
            with self.frame_lock:
                payload = locked_payload if locked and locked_payload else self.latest_jpeg
            if payload is None:
                frame = self._offline_frame()
                if enabled and not connected:
                    cv2.putText(
                        frame, "Dang ket noi lai camera...", (130, 220),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 165, 255), 2,
                    )
                payload = self._encode_jpeg(frame)
            if payload:
                yield b"--frame\r\nContent-Type: image/jpeg\r\n\r\n" + payload + b"\r\n"
            time.sleep(0.02 if enabled else 0.3)

    def update_recognition_state(self, **kwargs: Any) -> None:
        """Compatibility/test hook scoped to this session."""
        with self.session_lock:
            mapping = {
                "identity_confirmed": "_identity_confirmed",
                "employee_id": "confirmed_subject_id",
                "confirm_count": "confirmed_frames",
                "scan_locked": "_scan_locked",
                "lock_reason": "_lock_reason",
                "locked_snapshot": None,
                "locked_face_crop": None,
            }
            for key, value in kwargs.items():
                target = mapping.get(key, key)
                if key == "locked_snapshot":
                    self.locked_images["snapshot"] = value
                elif key == "locked_face_crop":
                    self.locked_images["face_crop"] = value
                elif hasattr(self, target):
                    setattr(self, target, value)
                self.recognition_result[key] = value
            self._publish_state_locked(**kwargs)

    def _capture_loop(self, generation: int, stop_event: threading.Event) -> None:
        capture = None
        try:
            while not stop_event.is_set():
                with self.session_lock:
                    if not self._is_current_locked(generation):
                        return
                    stream_url = self.stream_url
                capture = self._open_capture(stream_url)
                with self.session_lock:
                    if not self._is_current_locked(generation):
                        return
                    self.capture = capture
                    opened = capture.isOpened()
                    if not opened:
                        self.connection_status = "error"
                        self.last_error = "Cannot open camera stream"
                        self._message = self.last_error
                        self._publish_state_locked()
                        self.capture = None
                    else:
                        self.connection_status = "connected"
                        self.last_error = None
                        self._message = "Camera opened successfully"
                        self._publish_state_locked()
                if not opened:
                    self._release_capture(capture)
                    capture = None
                    stop_event.wait(RECONNECT_DELAY_SEC)
                    continue

                read_fail_count = 0
                while not stop_event.is_set():
                    try:
                        with self.capture_io_lock:
                            ok, frame = capture.read()
                    except Exception:
                        ok, frame = False, None
                    if not ok or frame is None:
                        read_fail_count += 1
                        if read_fail_count >= MAX_READ_FAILS_BEFORE_WARN:
                            with self.session_lock:
                                if self._is_current_locked(generation):
                                    self.connection_status = "error"
                                    self.last_error = "Camera stream unstable"
                                    self._message = (
                                        "Camera stream unstable, reconnecting..."
                                    )
                                    self._publish_state_locked()
                            break
                        stop_event.wait(0.02)
                        continue
                    read_fail_count = 0
                    frame = cv2.resize(
                        frame,
                        (self._config.stream_width, self._config.stream_height),
                    )
                    with self.session_lock:
                        if not self._is_current_locked(generation):
                            return
                    with self.frame_lock:
                        if generation != self.generation:
                            return
                        self.latest_frame = frame.copy()
                        self.latest_frame_at = time.time()

                self._release_capture(capture)
                capture = None
                with self.session_lock:
                    if self._is_current_locked(generation):
                        self.capture = None
                stop_event.wait(RECONNECT_DELAY_SEC)
        except Exception:
            with self.session_lock:
                if self._is_current_locked(generation):
                    self.connection_status = "error"
                    self.last_error = "Cannot open camera stream"
                    self._message = self.last_error
                    self._publish_state_locked()
        finally:
            self._release_capture(capture)
            with self.session_lock:
                if self._is_current_locked(generation):
                    self.capture = None

    def _recognition_loop(self, generation: int, stop_event: threading.Event) -> None:
        fps_counter = 0
        fps_started = time.time()
        last_frame_at = 0.0
        while not stop_event.is_set():
            try:
                with self.session_lock:
                    if not self._is_current_locked(generation):
                        return
                frame, frame_at = self.latest_frame_copy()
                if frame is None:
                    stop_event.wait(0.03)
                    continue
                if frame_at != last_frame_at:
                    last_frame_at = frame_at
                    fps_counter += 1
                processed = self._preprocess(frame)
                if processed is None:
                    stop_event.wait(0.01)
                    continue

                with self.session_lock:
                    if not self._is_current_locked(generation):
                        return
                    if self._scan_locked:
                        display = self._draw_overlay_locked(processed)
                        self._store_display(generation, display)
                        stop_event.wait(0.03)
                        continue

                current_time = time.time()
                detections = (
                    self._detector.detect(processed)
                    if self._detector is not None
                    else None
                )
                with self.session_lock:
                    if self._is_current_locked(generation):
                        self._update_tracks_locked(
                            processed, detections, current_time, generation)

                with self.session_lock:
                    if not self._is_current_locked(generation):
                        return
                    display = self._draw_overlay_locked(processed)
                    if time.time() - fps_started >= 1:
                        self._fps = fps_counter
                        fps_counter = 0
                        fps_started = time.time()
                        self._publish_state_locked(fps=self._fps)
                self._store_display(generation, display)
                stop_event.wait(0.01)
            except Exception:
                import traceback
                traceback.print_exc()
                try:
                    with self.session_lock:
                        self._message = "Recognition loop error"
                        self._publish_state_locked()
                except Exception:
                    pass
                stop_event.wait(0.5)

    def _update_tracks_locked(
        self, processed, detections, current_time, generation,
    ) -> None:
        """Session-based multi-face tracking.

        Each detected face is matched to an existing track by IoU. A track keeps
        its bbox/identity across frames; once confirmed it stops re-embedding to
        save GPU. A track that leaves the camera for track_lost_timeout ends its
        session. During track_grace_seconds a new face is only 'tracking'
        (never red) so it has time to be recognized.
        """
        if not self._is_current_locked(generation):
            return
        now = current_time
        config = self._config
        snapshot_b64 = self._image_to_base64(processed)
        grace = float(getattr(config, "track_grace_seconds", 2.0))
        lost_t = float(getattr(config, "track_lost_timeout", 2.0))
        embed_i = float(getattr(config, "track_embed_interval", 0.15))
        confirm_n = int(getattr(config, "track_confirm_frames", 3))

        # 1) Build detection boxes.
        dets = []
        if detections is not None and len(detections):
            limit = min(len(detections), max(1, self._max_faces))
            for i in range(limit):
                d = detections[i]
                loc = FaceDetector.box_from_detection(detections[i])
                lm = FaceDetector.landmarks_from_detection(detections[i])
                dets.append({
                    "loc": loc,
                    "landmarks": lm,
                    "crop": self._crop_face(processed, loc),
                    "area": max(0, loc[1]-loc[3]) * max(0, loc[2]-loc[0]),
                })
        dets.sort(key=lambda x: x["area"], reverse=True)

        # 2) Match detections to existing tracks (greedy IoU).
        used = set()
        matched = {}  # track_id -> detection idx
        track_ids = list(self._tracks.keys())
        for det_i, det in enumerate(dets):
            best_tid = None
            best_iou = 0.30
            for tid in track_ids:
                if tid in used:
                    continue
                t = self._tracks[tid]
                iou = _iou(t["bbox"], det["loc"])
                if iou >= best_iou:
                    best_iou = iou
                    best_tid = tid
            if best_tid is not None:
                used.add(best_tid)
                matched[best_tid] = det_i

        # 3) Update matched tracks + create new ones.
        live_tracks = []
        for det_i, det in enumerate(dets):
            tid = None
            for tid_candidate, used_i in matched.items():
                if used_i == det_i:
                    tid = tid_candidate
                    break
            if tid is None:
                tid = f"t{self._next_track_id}"
                self._next_track_id += 1
                self._tracks[tid] = {
                    "bbox": _loc_to_box(det["loc"]),
                    "first_seen": now,
                    "last_seen": now,
                    "last_embed_at": 0.0,
                    "confirm_count": 0,
                    "subject_id": None,
                    "status": "new",  # new -> tracking -> confirmed / intruder
                    "distance": None,
                    "intruder_captured": False,
                    "crop_b64": self._image_to_base64(det["crop"]),
                    "snapshot_b64": snapshot_b64,
                }
            t = self._tracks[tid]
            t["bbox"] = _loc_to_box(det["loc"])
            t["last_seen"] = now
            t["crop_b64"] = self._image_to_base64(det["crop"])
            t["snapshot_b64"] = snapshot_b64

            # Grace: no red during the first grace seconds. After grace, if the
            # face still has no identity at all -> mark intruder (red) so the UI
            # alerts; a face with a pending match keeps tracking to confirm.
            age = now - t["first_seen"]
            if t["status"] == "new":
                if age >= grace:
                    t["status"] = "tracking"
            elif t["status"] == "tracking":
                if age >= grace and t.get("subject_id") is None:
                    t["status"] = "intruder"
                    if not t.get("intruder_captured"):
                        t["intruder_captured"] = True
                        self._capture_track_intruder_locked(tid)

            # Embed only unconfirmed tracks at a faster interval.
            if t["status"] not in ("confirmed", "intruder"):
                if now - t["last_embed_at"] >= embed_i:
                    enc = None
                    if self._embedder is not None:
                        try:
                            enc = self._embedder.align_and_embed(
                                processed, det["landmarks"])
                        except Exception:
                            enc = None
                    t["last_embed_at"] = now
                    t["_enc"] = enc

            live_tracks.append(tid)

        # 4) Recognize tracks that have a fresh embedding.
        model_snapshot = self._model_registry.current_snapshot()
        for tid in live_tracks:
            t = self._tracks[tid]
            enc = t.get("_enc")
            if enc is None:
                continue
            if not model_snapshot.encoding_count:
                continue
            if model_snapshot.metric == "cosine":
                dists = np.asarray(
                    [cosine_distance(enc, k) for k in model_snapshot.encodings],
                    dtype=np.float64)
            else:
                dists = np.asarray(
                    [float(np.linalg.norm(np.asarray(enc)-np.asarray(k)))
                     for k in model_snapshot.encodings], dtype=np.float64)
            best = int(np.argmin(dists))
            distance = float(dists[best])
            t["distance"] = distance
            if distance < self._config.threshold:
                if t.get("subject_id") == model_snapshot.subject_ids[best]:
                    t["confirm_count"] += 1
                else:
                    t["confirm_count"] = 1
                    t["subject_id"] = model_snapshot.subject_ids[best]
                if t["confirm_count"] >= confirm_n:
                    t["status"] = "confirmed"
            else:
                if t.get("subject_id") is not None:
                    t["confirm_count"] = 0
                    t["subject_id"] = None
            t.pop("_enc", None)

        # 5) Tracks that were not seen this frame -> close their session.
        closed = []
        for tid in list(self._tracks.keys()):
            t = self._tracks[tid]
            if now - t["last_seen"] > lost_t:
                closed.append(tid)
        for tid in closed:
            t = self._tracks[tid]
            if t.get("status") == "confirmed":
                self._emit_event_locked("Recognized", t.get("subject_id"),
                                        t.get("distance"), model_snapshot, None)
            elif t.get("status") not in ("confirmed",):
                # Intruder: unknown or denied identity. Already recorded when it
                # turned red; capture only once if it leaves before that.
                if not t.get("intruder_captured"):
                    t["intruder_captured"] = True
                    self._capture_track_intruder_locked(tid)
            del self._tracks[tid]

        # 6) Publish faces array for the UI.
        self.faces = []
        primary_confirmed = None
        for tid in track_ids:
            t = self._tracks.get(tid)
            if t is None:
                continue
            self.faces.append({
                "id": tid,
                "bbox": t["bbox"],
                "employee_id": t.get("subject_id"),
                "distance": t.get("distance"),
                "match": t.get("status") == "confirmed",
                "status": t.get("status", "new"),
                "crop_b64": t.get("crop_b64"),
                "snapshot_b64": t.get("snapshot_b64"),
            })
            if t.get("status") == "confirmed" and primary_confirmed is None:
                primary_confirmed = t

        # Backward-compatible scalar state = first confirmed track (or first).
        if primary_confirmed is not None:
            self._tracking_active = True
            self._identity_confirmed = True
            self.confirmed_subject_id = primary_confirmed.get("subject_id")
            self.confirmed_frames = primary_confirmed.get("confirm_count", 0)
            self._last_distance = primary_confirmed.get("distance")
            self._last_face_match = True
            self._last_face_box = primary_confirmed.get("bbox")
            self._last_snapshot = primary_confirmed.get("snapshot_b64")
            self._last_face_crop = primary_confirmed.get("crop_b64")
            self.last_face_seen_at = now
        else:
            first = self._tracks.get(track_ids[0]) if track_ids else None
            if first is not None:
                self._tracking_active = True
                self._identity_confirmed = False
                self.confirmed_subject_id = first.get("subject_id")
                self.confirmed_frames = first.get("confirm_count", 0)
                self._last_distance = first.get("distance")
                self._last_face_match = first.get("status") == "confirmed"
                self._last_face_box = first.get("bbox")
                self._last_snapshot = first.get("snapshot_b64")
                self._last_face_crop = first.get("crop_b64")
                self.last_face_seen_at = now
        self._publish_state_locked()

    def _capture_track_intruder_locked(self, tid: str) -> None:
        """Record a track that left the camera as an intruder (unknown/denied)."""
        t = self._tracks.get(tid)
        if t is None:
            return
        try:
            import hashlib
            key_raw = (t.get("crop_b64") or t.get("snapshot_b64") or "")[:200]
            key = hashlib.sha256(key_raw.encode("utf-8", "ignore")).hexdigest()[:24]
            if key in self._intruder_keys:
                return
            self._intruder_keys.add(key)
            self.intruders.append({
                "id": key,
                "reason": "timeout",
                "capturedAtUtc": _utc_now(),
                "snapshot": t.get("snapshot_b64"),
                "faceCrop": t.get("crop_b64"),
                "employee_id": t.get("subject_id"),
                "distance": t.get("distance"),
            })
            if len(self.intruders) > 200:
                self.intruders = self.intruders[-200:]
        except Exception:
            pass

    def _apply_no_face(self, generation: int, current_time: float) -> None:
        with self.session_lock:
            if not self._is_current_locked(generation):
                return
            if self.faces:
                self.faces = []
                self._publish_state_locked()
            if (
                self._tracking_active
                and not self._scan_locked
                and current_time - self.last_face_seen_at > self._config.lost_timeout
            ):
                self._reset_recognition_locked("Face lost. State reset")
            else:
                self._message = (
                    f"Face scan locked: {self._lock_reason}"
                    if self._scan_locked else "No face detected"
                )
                self._publish_state_locked()

    def _apply_timeouts_locked(
        self, current_time: float, frame: np.ndarray, face_crop: np.ndarray | None
    ) -> None:
        if not self._tracking_active or self._identity_confirmed or self._scan_locked:
            return
        elapsed = current_time - float(self.cooldown_state["tracking_started_at"])
        if elapsed > self._config.recognize_timeout and not self._last_timeout:
            self.confirmed_frames = 0
            self._identity_confirmed = False
            self._last_face_match = False
            self.confirmed_subject_id = None
            self._last_timeout = True
            self._lock_result_locked("timeout", frame, face_crop)
        if (
            elapsed > self._config.alert_timeout
            and not self.cooldown_state["alert_triggered"]
            and not self._scan_locked
        ):
            self.cooldown_state["alert_triggered"] = True
            self._last_alert = True
            self._lock_result_locked("alert", frame, face_crop)

    def _lock_result_locked(
        self, reason: str, frame: np.ndarray | None, face_crop: np.ndarray | None
    ) -> None:
        if self._scan_locked:
            return
        self._scan_locked = True
        self._lock_reason = reason
        if frame is not None:
            self.locked_images["frame_jpeg"] = self._encode_jpeg(frame)
            self.locked_images["snapshot"] = self._image_to_base64(frame)
        if face_crop is not None and getattr(face_crop, "size", 0) > 0:
            self.locked_images["face_crop"] = self._image_to_base64(face_crop)
        if reason in {"timeout", "alert"}:
            self._emit_event_locked("Unknown", None, self._last_distance, None, None)
            self._capture_intruder_locked(reason)

    def _capture_intruder_locked(self, reason: str) -> None:
        """Record an intruder capture (unknown/unauthorized face) for review."""
        if self._scan_locked and reason in {"timeout", "alert"}:
            try:
                snapshot = self.locked_images.get("snapshot")
                crop = self.locked_images.get("face_crop")
                if not snapshot and not crop:
                    return
                key = None
                # Deduplicate by image content hash so each intruder is one card.
                raw = snapshot or crop
                if raw:
                    import hashlib
                    key = hashlib.sha256(str(raw).encode("utf-8", "ignore")).hexdigest()[:24]
                if key and key in self._intruder_keys:
                    return
                if key:
                    self._intruder_keys.add(key)
                self.intruders.append({
                    "id": key or str(uuid.uuid4())[:24],
                    "reason": reason,
                    "capturedAtUtc": _utc_now(),
                    "snapshot": snapshot,
                    "faceCrop": crop,
                    "employee_id": None,
                    "distance": self._last_distance,
                })
                if len(self.intruders) > 200:
                    self.intruders = self.intruders[-200:]
            except Exception:
                pass

    def intruder_result(self) -> dict[str, Any]:
        with self.session_lock:
            return {
                "success": True,
                "count": len(self.intruders),
                "intruders": list(self.intruders),
            }

    def clear_intruders(self) -> dict[str, Any]:
        with self.session_lock:
            count = len(self.intruders)
            self.intruders = []
            self._intruder_keys = set()
            return {"success": True, "cleared": count}

    def _emit_event_locked(
        self, event_type: str, subject_id: str | None, distance: float | None,
        snapshot: Any | None, descriptor: Any | None,
    ) -> None:
        self._event_sequence += 1
        event = {
            "eventId": str(uuid.uuid4()),
            "cameraId": self.camera_id,
            "laneId": self.lane_id,
            "sequence": self._event_sequence,
            "sessionGeneration": self.generation,
            "eventType": event_type,
            "subjectId": subject_id,
            "occurredAtUtc": _utc_now(),
            "distance": distance,
            "modelRegistryVersion": getattr(snapshot, "version", None),
            "modelFileName": getattr(descriptor, "file_name", None),
            "modelChecksumPrefix": (
                getattr(descriptor, "checksum", "")[:12] or None),
            "_createdMonotonic": time.time(),
        }
        with self.event_lock:
            self._prune_events_locked(event["_createdMonotonic"])
            self._events.append(event)

    def _prune_events_locked(self, now: float) -> None:
        cutoff = now - getattr(self._config, "event_retention_seconds", 3600)
        while self._events and self._events[0]["_createdMonotonic"] < cutoff:
            self._events.popleft()

    def _reset_recognition_locked(self, reason: str) -> None:
        self._tracking_active = False
        self._identity_confirmed = False
        self.confirmed_subject_id = None
        self.confirmed_frames = 0
        self.last_face_seen_at = 0.0
        self.last_recognition_at = 0.0
        self.cooldown_state["tracking_started_at"] = 0.0
        self.cooldown_state["alert_triggered"] = False
        self.cooldown_state["distance_buffer"].clear()
        self._last_face_box = None
        self._last_distance = None
        self._last_face_match = False
        self._last_timeout = False
        self._last_alert = False
        self._scan_locked = False
        self._lock_reason = None
        self.locked_images.update(
            {"frame_jpeg": None, "snapshot": None, "face_crop": None}
        )
        self._last_snapshot = None
        self._last_face_crop = None
        self.faces = []
        self._tracks = {}
        self._fps = 0
        self._message = reason
        self._publish_state_locked()

    def _publish_state_locked(self, **overrides: Any) -> None:
        payload = {
            "tracking_active": self._tracking_active,
            "identity_confirmed": self._identity_confirmed,
            "face_match": self._last_face_match,
            "employee_id": self.confirmed_subject_id,
            "confirm_count": self.confirmed_frames,
            "distance": self._last_distance,
            "last_seen": self.last_face_seen_at if self.last_face_seen_at > 0 else None,
            "bbox": self._last_face_box,
            "faces": list(self.faces),
            "timeout": self._last_timeout,
            "alert": self._last_alert,
            "scan_locked": self._scan_locked,
            "lock_reason": self._lock_reason,
            "fps": self._fps,
            "last_update": _legacy_timestamp(),
            "message": self._message,
        }
        payload.update(overrides)
        self.recognition_result = payload
        self._last_update = payload["last_update"]
        self._touch_locked()

    def _set_face_box_locked(self, location: tuple[int, int, int, int]) -> None:
        top, right, bottom, left = location
        self._last_face_box = {
            "top": int(top), "right": int(right), "bottom": int(bottom),
            "left": int(left), "width": int(right - left), "height": int(bottom - top),
        }

    def _draw_overlay_locked(self, frame: np.ndarray) -> np.ndarray:
        display = frame.copy()

        # Draw every live face: green confirmed, yellow tracking, red intruder.
        faces = list(getattr(self, "faces", []) or [])
        for face in faces:
            box = face.get("bbox")
            if not box:
                continue
            left, top = int(box.get("left", 0)), int(box.get("top", 0))
            right, bottom = int(box.get("right", 0)), int(box.get("bottom", 0))
            status = face.get("status", "new")
            if status == "confirmed":
                color = (0, 255, 0)
            elif status == "intruder":
                color = (0, 0, 255)
            else:
                color = (0, 255, 255)  # tracking / grace (yellow)
            cv2.rectangle(display, (left, top), (right, bottom), color, 2)
            label = str(face.get("employee_id") or "???")
            cv2.putText(
                display, label, (left, max(10, top - 8)),
                cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2,
            )

        # Primary single-face box overlay kept for compatibility.
        if self._last_face_box is not None and not faces:
            box = self._last_face_box
            color = (0, 255, 0) if self._last_face_match else (0, 0, 255)
            cv2.rectangle(
                display, (box["left"], box["top"]),
                (box["right"], box["bottom"]), color, 2,
            )
        cv2.putText(
            display, f"FPS: {self._fps}", (20, 40),
            cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 0, 0), 2,
        )
        return display

    def _store_display(self, generation: int, display: np.ndarray) -> None:
        encoded = self._encode_jpeg(display)
        with self.frame_lock:
            if generation != self.generation:
                return
            self.latest_display_frame = display.copy()
            locked = self.locked_images["frame_jpeg"]
            self.latest_jpeg = locked if self._scan_locked and locked else encoded

    def _preprocess(self, frame: np.ndarray) -> np.ndarray | None:
        height, width = frame.shape[:2]
        if width == 0:
            return None
        scale = self._config.frame_width / width
        resized = cv2.resize(frame, (self._config.frame_width, int(height * scale)))
        if self._config.rotation == 90:
            return cv2.rotate(resized, cv2.ROTATE_90_CLOCKWISE)
        if self._config.rotation == -90:
            return cv2.rotate(resized, cv2.ROTATE_90_COUNTERCLOCKWISE)
        if self._config.rotation == 180:
            return cv2.rotate(resized, cv2.ROTATE_180)
        return resized

    @staticmethod
    def _crop_face(
        frame: np.ndarray, location: tuple[int, int, int, int]
    ) -> np.ndarray | None:
        top, right, bottom, left = location
        height, width = frame.shape[:2]
        top, left = max(0, top), max(0, left)
        bottom, right = min(height, bottom), min(width, right)
        if right <= left or bottom <= top:
            return None
        crop = frame[top:bottom, left:right]
        return crop if crop.size else None

    def _open_capture(self, stream_url: str):
        if self._capture_factory is not None:
            capture = self._capture_factory(stream_url)
        else:
            capture = cv2.VideoCapture(stream_url, cv2.CAP_FFMPEG)
        try:
            capture.set(cv2.CAP_PROP_BUFFERSIZE, 1)
        except Exception:
            pass
        return capture

    def _encode_jpeg(self, frame: np.ndarray) -> bytes | None:
        ok, encoded = cv2.imencode(
            ".jpg", frame,
            [int(cv2.IMWRITE_JPEG_QUALITY), self._config.jpeg_quality],
        )
        return encoded.tobytes() if ok else None

    @staticmethod
    def _image_to_base64(frame: np.ndarray | None) -> str | None:
        if frame is None or frame.size == 0:
            return None
        ok, encoded = cv2.imencode(
            ".jpg", frame, [int(cv2.IMWRITE_JPEG_QUALITY), 90]
        )
        if not ok:
            return None
        return "data:image/jpeg;base64," + base64.b64encode(
            encoded.tobytes()
        ).decode("utf-8")

    def _offline_frame(self) -> np.ndarray:
        frame = np.zeros(
            (self._config.stream_height, self._config.stream_width, 3),
            dtype=np.uint8,
        )
        cv2.putText(
            frame, "Camera Offline", (190, 180),
            cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 255), 2,
        )
        return frame

    def _is_current_locked(self, generation: int) -> bool:
        return self.enabled and self.generation == generation

    def _touch_locked(self) -> None:
        self.updated_at = _utc_now()

    @staticmethod
    def _release_capture(capture: Any | None) -> None:
        if capture is not None:
            try:
                capture.release()
            except Exception:
                pass

    @staticmethod
    def _join_workers(workers: tuple[threading.Thread | None, ...]) -> None:
        current = threading.current_thread()
        for worker in workers:
            if worker is not None and worker is not current and worker.is_alive():
                worker.join(timeout=2.0)
