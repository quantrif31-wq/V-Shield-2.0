import cv2
import time
import numpy as np
import os
import re
import urllib.parse
import urllib.request
from pyzbar import pyzbar
from threading import Thread, Lock
from fastapi import FastAPI, Response
from fastapi.middleware.cors import CORSMiddleware
import uvicorn


PREVIEW_WIDTH = 960
PREVIEW_HEIGHT = 540
CANDIDATE_REQUIRED_COUNT = 2
CANDIDATE_WINDOW_MS = 800
IDLE_SLEEP_SECONDS = 0.02


state = {
    "running": False,
    "scan_enabled": False,
    "locked": False,
    "qr": "",
    "rtsp": "",
    "connected": False,
    "frame_ready": False,
    "phase": "idle",
    "candidate_payload": "",
    "candidate_source": "",
    "candidate_seen_count": 0,
    "locked_payload": "",
    "locked_at": 0,
}

lock = Lock()
frame_lock = Lock()

latest_frame = None
stop_flag = False
qr_detector = cv2.QRCodeDetector()


app = FastAPI()
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


def now_ms():
    return int(time.time() * 1000)


def enhance(img):
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    return cv2.equalizeHist(gray)


def fit_preview(frame):
    h, w = frame.shape[:2]
    if w <= PREVIEW_WIDTH and h <= PREVIEW_HEIGHT:
        return frame

    ratio = min(PREVIEW_WIDTH / float(w), PREVIEW_HEIGHT / float(h))
    target_size = (max(1, int(w * ratio)), max(1, int(h * ratio)))
    return cv2.resize(frame, target_size, interpolation=cv2.INTER_AREA)


def prefer_qr_stream(rtsp):
    if not rtsp:
        return rtsp
    return re.sub(r"([?&]subtype=)1\b", r"\g<1>0", rtsp, flags=re.IGNORECASE)


def resolve_camera_source(raw_source):
    source = prefer_qr_stream(raw_source)
    if not source:
        return ""

    lowered = source.lower()
    if lowered.startswith("go2rtc:"):
        return source

    if "stream.html?src=" in lowered:
        try:
            parsed = urllib.parse.urlparse(source)
            query = urllib.parse.parse_qs(parsed.query or "")
            stream_name = (query.get("src") or [""])[0].strip()
            if stream_name:
                return f"go2rtc:{stream_name}"
        except Exception:
            pass

    return source


def build_go2rtc_snapshot_url(stream_name):
    safe_name = urllib.parse.quote(stream_name, safe="")
    stamp = now_ms()
    return f"http://go2rtc:1984/api/frame.jpeg?src={safe_name}&_={stamp}"


def fetch_snapshot_frame(snapshot_url):
    try:
        with urllib.request.urlopen(snapshot_url, timeout=5) as response:
            data = response.read()
        if not data:
            return None
        encoded = np.frombuffer(data, dtype=np.uint8)
        if encoded.size == 0:
            return None
        return cv2.imdecode(encoded, cv2.IMREAD_COLOR)
    except Exception:
        return None


def clear_candidate_state_unlocked():
    state["candidate_payload"] = ""
    state["candidate_source"] = ""
    state["candidate_seen_count"] = 0


def unlock_state_unlocked():
    state["locked"] = False
    state["qr"] = ""
    state["locked_payload"] = ""
    state["locked_at"] = 0


def set_phase_unlocked():
    if not state["running"]:
        state["phase"] = "idle"
        return

    if not state["connected"] or not state["frame_ready"]:
        state["phase"] = "connecting"
        return

    if state["locked"] and state["locked_payload"]:
        state["phase"] = "locked"
        return

    if state["scan_enabled"]:
        if state["candidate_payload"] and state["candidate_seen_count"] > 0:
            state["phase"] = "candidate_found"
        else:
            state["phase"] = "scanning"
        return

    state["phase"] = "idle"


def update_phase():
    with lock:
        set_phase_unlocked()


def decode_qr(frame):
    h, w, _ = frame.shape
    rois = [
        frame,
        frame[h // 4:3 * h // 4, w // 4:3 * w // 4],
        frame[h // 6:5 * h // 6, w // 6:5 * w // 6],
        frame[0:h // 2, :],
        frame[:, w // 5:4 * w // 5],
    ]

    for roi in rois:
        gray = enhance(roi)
        variants = [
            gray,
            cv2.GaussianBlur(gray, (3, 3), 0),
            cv2.GaussianBlur(gray, (5, 5), 0),
            cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)[1],
            cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)[1],
            cv2.convertScaleAbs(gray, alpha=1.5, beta=0),
            cv2.adaptiveThreshold(
                gray, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY, 31, 2
            ),
        ]

        for variant in variants:
            for scale in [1.0, 1.5, 2.0, 3.0]:
                resized = (
                    cv2.resize(
                        variant,
                        None,
                        fx=scale,
                        fy=scale,
                        interpolation=cv2.INTER_CUBIC,
                    )
                    if scale != 1.0
                    else variant
                )

                barcodes = pyzbar.decode(resized)
                if barcodes:
                    return barcodes[0].data.decode(errors="ignore")

                decoded, _, _ = qr_detector.detectAndDecode(resized)
                if decoded:
                    return decoded.strip()

    return None


def decode_qr_fast(frame):
    gray = enhance(frame)
    quick_variants = [
        gray,
        cv2.GaussianBlur(gray, (3, 3), 0),
        cv2.convertScaleAbs(gray, alpha=1.35, beta=0),
    ]

    for variant in quick_variants:
        decoded, _, _ = qr_detector.detectAndDecode(variant)
        if decoded:
            return decoded.strip()

        barcodes = pyzbar.decode(variant)
        if barcodes:
            return barcodes[0].data.decode(errors="ignore")

    return None


def decode_live_frame(frame):
    preview = fit_preview(frame)
    h_preview, w_preview = preview.shape[:2]
    center_preview = preview[
        int(h_preview * 0.1):int(h_preview * 0.92),
        int(w_preview * 0.12):int(w_preview * 0.88),
    ]

    for source_name, candidate in [
        ("preview", preview),
        ("center-preview", center_preview),
    ]:
        if candidate is None or getattr(candidate, "size", 0) == 0:
            continue
        decoded = decode_qr_fast(candidate)
        if decoded:
            return decoded, source_name

    attempts = [("raw", frame), ("preview", preview)]
    h, w = frame.shape[:2]
    center = frame[int(h * 0.1):int(h * 0.92), int(w * 0.12):int(w * 0.88)]
    if center.size:
        attempts.append(("center", center))
        attempts.append(("center-preview", fit_preview(center)))

    for source_name, candidate in attempts:
        if candidate is None or getattr(candidate, "size", 0) == 0:
            continue
        decoded = decode_qr(candidate)
        if decoded:
            return decoded, source_name

    return None, ""


def lock_candidate_unlocked(payload, source_name):
    state["locked"] = True
    state["qr"] = payload
    state["locked_payload"] = payload
    state["locked_at"] = now_ms()
    state["scan_enabled"] = False
    state["candidate_payload"] = payload
    state["candidate_source"] = source_name
    state["candidate_seen_count"] = max(
        state["candidate_seen_count"], CANDIDATE_REQUIRED_COUNT
    )
    set_phase_unlocked()


def process_candidate(payload, source_name):
    with lock:
        if not state["scan_enabled"] or state["locked"]:
            return False

        if not payload:
            if state["candidate_payload"]:
                clear_candidate_state_unlocked()
            set_phase_unlocked()
            return False

        payload = str(payload).strip()
        if not payload:
            clear_candidate_state_unlocked()
            set_phase_unlocked()
            return False

        current = state["candidate_payload"]
        if current == payload:
            state["candidate_seen_count"] += 1
        else:
            state["candidate_payload"] = payload
            state["candidate_source"] = source_name
            state["candidate_seen_count"] = 1

        state["candidate_source"] = source_name

        if state["candidate_seen_count"] >= CANDIDATE_REQUIRED_COUNT:
            lock_candidate_unlocked(payload, source_name)
            return True

        set_phase_unlocked()
        return False


def build_preview_frame():
    with frame_lock:
        frame = None if latest_frame is None else latest_frame.copy()

    if frame is None:
        frame = np.zeros((270, 480, 3), dtype=np.uint8)
        cv2.putText(
            frame,
            "NO CAMERA",
            (150, 140),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.8,
            (255, 255, 255),
            2,
        )

    with lock:
        locked_payload = state["locked_payload"]
        phase = state["phase"]

    if phase in ("scanning", "candidate_found"):
        cv2.putText(
            frame,
            "SCANNING...",
            (10, 30),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.7,
            (0, 165, 255),
            2,
        )

    if locked_payload:
        cv2.putText(
            frame,
            f"LOCKED: {locked_payload}",
            (10, 60),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.7,
            (0, 255, 0),
            2,
        )

    return fit_preview(frame)


def camera_worker():
    global latest_frame

    cap = None
    current_source = ""
    snapshot_stream_name = ""

    while not stop_flag:
        with lock:
            running = state["running"]
            source = state["rtsp"]

        if not running:
            if cap:
                cap.release()
                cap = None
            current_source = ""
            snapshot_stream_name = ""
            with lock:
                state["connected"] = False
                state["frame_ready"] = False
                set_phase_unlocked()
            time.sleep(0.05)
            continue

        if source != current_source:
            current_source = source
            snapshot_stream_name = ""
            if cap:
                cap.release()
            cap = None

            if current_source.startswith("go2rtc:"):
                snapshot_stream_name = current_source.split(":", 1)[1].strip()
            else:
                cap = cv2.VideoCapture(current_source)
                cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)

            with lock:
                state["connected"] = False
                state["frame_ready"] = False
                clear_candidate_state_unlocked()
                unlock_state_unlocked()
                set_phase_unlocked()

        frame = None
        if snapshot_stream_name:
            frame = fetch_snapshot_frame(build_go2rtc_snapshot_url(snapshot_stream_name))
            if frame is None:
                with lock:
                    state["connected"] = False
                    state["frame_ready"] = False
                    set_phase_unlocked()
                time.sleep(0.1)
                continue
        else:
            if not cap or not cap.isOpened():
                with lock:
                    state["connected"] = False
                    state["frame_ready"] = False
                    set_phase_unlocked()
                time.sleep(0.2)
                continue

            ret, frame = cap.read()
            if not ret or frame is None:
                with lock:
                    state["connected"] = False
                    state["frame_ready"] = False
                    set_phase_unlocked()
                time.sleep(0.05)
                continue

        if frame is None:
            with lock:
                state["connected"] = False
                state["frame_ready"] = False
                set_phase_unlocked()
            time.sleep(0.05)
            continue

        with frame_lock:
            latest_frame = frame.copy()

        with lock:
            state["connected"] = True
            state["frame_ready"] = True
            set_phase_unlocked()

        time.sleep(0.01)

    if cap:
        cap.release()


def scan_worker():
    print("QR scan thread ready")

    while not stop_flag:
        with lock:
            should_scan = state["running"] and state["scan_enabled"] and not state["locked"]

        if not should_scan:
            time.sleep(IDLE_SLEEP_SECONDS)
            continue

        with frame_lock:
            frame = None if latest_frame is None else latest_frame.copy()

        if frame is None:
            with lock:
                state["frame_ready"] = False
                set_phase_unlocked()
            time.sleep(IDLE_SLEEP_SECONDS)
            continue

        payload, source_name = decode_live_frame(frame)
        did_lock = process_candidate(payload, source_name)
        if did_lock:
            print("QR LOCKED:", payload)

        time.sleep(IDLE_SLEEP_SECONDS)


@app.post("/qr/start")
def api_start(data: dict):
    rtsp = resolve_camera_source(data.get("rtsp"))

    with lock:
        state["rtsp"] = rtsp
        state["running"] = True
        state["scan_enabled"] = False
        state["connected"] = False
        state["frame_ready"] = False
        clear_candidate_state_unlocked()
        unlock_state_unlocked()
        set_phase_unlocked()

    return {"success": True}


@app.post("/qr/scan")
def api_scan():
    with lock:
        clear_candidate_state_unlocked()
        unlock_state_unlocked()
        state["scan_enabled"] = True
        set_phase_unlocked()

    return {"success": True}


@app.post("/qr/reset")
def api_reset():
    with lock:
        clear_candidate_state_unlocked()
        unlock_state_unlocked()
        state["scan_enabled"] = False
        set_phase_unlocked()

    return {"success": True}


@app.post("/qr/stop")
def api_stop():
    with lock:
        state["running"] = False
        state["scan_enabled"] = False
        state["connected"] = False
        state["frame_ready"] = False
        clear_candidate_state_unlocked()
        unlock_state_unlocked()
        state["rtsp"] = ""
        set_phase_unlocked()

    return {"success": True}


@app.get("/qr/result")
def api_result():
    with lock:
        return dict(state)


@app.get("/qr/frame.jpg")
def api_frame_jpg():
    frame = build_preview_frame()
    ok, buf = cv2.imencode(".jpg", frame, [int(cv2.IMWRITE_JPEG_QUALITY), 90])
    if not ok:
        return Response(status_code=500)

    return Response(
        content=buf.tobytes(),
        media_type="image/jpeg",
        headers={
            "Cache-Control": "no-cache, no-store, must-revalidate",
            "Pragma": "no-cache",
            "Expires": "0",
        },
    )


def main():
    global stop_flag

    Thread(target=camera_worker, daemon=True).start()
    Thread(target=scan_worker, daemon=True).start()
    Thread(
        target=lambda: uvicorn.run(app, host="0.0.0.0", port=8001),
        daemon=True,
    ).start()

    headless_mode = str(os.getenv("QR_HEADLESS", "")).strip().lower() in (
        "1",
        "true",
        "yes",
        "on",
    )
    if headless_mode:
        print("QR service running in headless mode")
        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            stop_flag = True
        return

    print("\n=== QR FINAL PRO ===")
    print("i = nhap RTSP")
    print("o = mo cam")
    print("r = scan")
    print("q = thoat\n")

    current_rtsp = ""

    while True:
        frame = build_preview_frame()
        cv2.imshow("QR FINAL", frame)
        key = cv2.waitKey(1) & 0xFF

        if key == ord("i"):
            current_rtsp = input("RTSP: ").strip()
            with lock:
                state["rtsp"] = current_rtsp

        elif key == ord("o"):
            with lock:
                state["running"] = True
                set_phase_unlocked()

        elif key == ord("r"):
            api_scan()
            print("scanning...")

        elif key == ord("q"):
            break

    stop_flag = True
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
