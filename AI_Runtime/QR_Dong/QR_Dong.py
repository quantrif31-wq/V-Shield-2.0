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
CANDIDATE_PERSIST_MS = 800
RESULT_HOLD_MS = 1500
SAME_CODE_COOLDOWN_MS = 4000
VANISH_RESET_MS = 2000
IDLE_SLEEP_SECONDS = 0.008
SCAN_TARGET_INTERVAL_SECONDS = 0.02
BURST_SCAN_WINDOW_SECONDS = 1.8
FULL_SCAN_INTERVAL_SECONDS = 0.08
BURST_FULL_SCAN_INTERVAL_SECONDS = 0.03
FRAME_DECODE_BUDGET_SECONDS = 0.06
BURST_FRAME_DECODE_BUDGET_SECONDS = 0.09
FAST_SCALE_FACTORS = (1.0, 1.35, 1.7, 2.1)
FULL_SCALE_FACTORS = (1.0, 1.4, 1.8, 2.4, 3.0)


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
    "candidate_last_seen_at": 0,
    "locked_payload": "",
    "locked_at": 0,
    "locked_expires_at": 0,
    "scan_started_at": 0.0,
    "scan_frame_seq": 0,
    "scan_session_id": 0,
    "last_fired_payload": "",
    "last_fired_at": 0,
    "last_vanish_at": 0,
    "cooldown_payload": "",
    "cooldown_until": 0,
    "session_active": False,
}

lock = Lock()
frame_lock = Lock()

latest_frame = None
latest_frame_seq = 0
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


def slog(msg):
    t = time.time()
    ms = int((t - int(t)) * 1000)
    print(f"[QR] {time.strftime('%H:%M:%S', time.localtime(t))}.{ms:03d} {msg}")


def enhance(img):
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    return cv2.equalizeHist(gray)


def sharpen(gray):
    blurred = cv2.GaussianBlur(gray, (0, 0), 1.2)
    return cv2.addWeighted(gray, 1.7, blurred, -0.7, 0)


def fit_preview(frame):
    h, w = frame.shape[:2]
    if w <= PREVIEW_WIDTH and h <= PREVIEW_HEIGHT:
        return frame

    ratio = min(PREVIEW_WIDTH / float(w), PREVIEW_HEIGHT / float(h))
    target_size = (max(1, int(w * ratio)), max(1, int(h * ratio)))
    return cv2.resize(frame, target_size, interpolation=cv2.INTER_AREA)


def build_scan_regions(frame, prefix="raw"):
    if frame is None or getattr(frame, "size", 0) == 0:
        return []

    h, w = frame.shape[:2]
    if h < 2 or w < 2:
        return [(prefix, frame)]

    def crop(y1, y2, x1, x2):
        y1 = max(0, min(h, int(y1)))
        y2 = max(0, min(h, int(y2)))
        x1 = max(0, min(w, int(x1)))
        x2 = max(0, min(w, int(x2)))
        if y2 - y1 < 12 or x2 - x1 < 12:
            return None
        return frame[y1:y2, x1:x2]

    regions = [(prefix, frame)]
    specs = [
        ("center", 0.08, 0.92, 0.10, 0.90),
        ("wide-center", 0.12, 0.94, 0.05, 0.95),
        ("top", 0.00, 0.76, 0.00, 1.00),
        ("bottom", 0.18, 1.00, 0.00, 1.00),
        ("left", 0.00, 1.00, 0.00, 0.78),
        ("right", 0.00, 1.00, 0.22, 1.00),
        ("top-left", 0.00, 0.82, 0.00, 0.82),
        ("top-right", 0.00, 0.82, 0.18, 1.00),
        ("bottom-left", 0.18, 1.00, 0.00, 0.82),
        ("bottom-right", 0.18, 1.00, 0.18, 1.00),
        ("entry-left", 0.05, 0.95, 0.00, 0.58),
        ("entry-right", 0.05, 0.95, 0.42, 1.00),
        ("entry-top", 0.00, 0.58, 0.05, 0.95),
        ("entry-bottom", 0.42, 1.00, 0.05, 0.95),
    ]

    for name, y1, y2, x1, x2 in specs:
        region = crop(h * y1, h * y2, w * x1, w * x2)
        if region is not None:
            regions.append((f"{prefix}-{name}", region))

    return regions


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
    go2rtc_host = os.getenv("QR_GO2RTC_HOST", "go2rtc")
    return f"http://{go2rtc_host}:1984/api/frame.jpeg?src={safe_name}&_={stamp}"


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
    state["candidate_last_seen_at"] = 0


def mark_scan_started_unlocked():
    state["scan_session_id"] = int(state.get("scan_session_id") or 0) + 1
    state["scan_started_at"] = time.perf_counter()
    state["scan_frame_seq"] = latest_frame_seq


def reset_scan_session_unlocked(scan_enabled=False):
    clear_candidate_state_unlocked()
    unlock_state_unlocked()
    state["scan_enabled"] = scan_enabled
    if scan_enabled:
        mark_scan_started_unlocked()
    else:
        state["scan_session_id"] = int(state.get("scan_session_id") or 0) + 1
        state["scan_started_at"] = 0.0
        state["scan_frame_seq"] = latest_frame_seq
        state["session_active"] = False


def unlock_state_unlocked():
    state["locked"] = False
    state["qr"] = ""
    state["locked_payload"] = ""
    state["locked_at"] = 0
    state["locked_expires_at"] = 0


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

    if state["cooldown_payload"]:
        state["phase"] = "cooldown"
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


def should_abort_decode(scan_session_id, deadline_at=0.0):
    if deadline_at and time.perf_counter() >= deadline_at:
        return True
    if not scan_session_id:
        return False
    with lock:
        return scan_session_id != int(state.get("scan_session_id") or 0)


def decode_qr(frame, scan_session_id=0, deadline_at=0.0):
    for _, roi in build_scan_regions(frame):
        if should_abort_decode(scan_session_id, deadline_at):
            return None
        raw_gray = cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY)
        eq = cv2.equalizeHist(raw_gray)
        sharp = sharpen(eq)
        variants = [
            raw_gray,
            eq,
            sharp,
            cv2.GaussianBlur(eq, (3, 3), 0),
            cv2.GaussianBlur(eq, (5, 5), 0),
            cv2.bilateralFilter(eq, 5, 50, 50),
            cv2.threshold(eq, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)[1],
            cv2.threshold(eq, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)[1],
            cv2.threshold(sharp, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)[1],
            cv2.convertScaleAbs(raw_gray, alpha=1.5, beta=0),
            cv2.convertScaleAbs(sharp, alpha=1.25, beta=0),
            cv2.adaptiveThreshold(
                eq, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY, 31, 2
            ),
            cv2.adaptiveThreshold(
                sharp, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY, 31, 2
            ),
        ]

        for variant in variants:
            if should_abort_decode(scan_session_id, deadline_at):
                return None
            for scale in FULL_SCALE_FACTORS:
                if should_abort_decode(scan_session_id, deadline_at):
                    return None
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
                    data = barcodes[0].data.decode(errors="ignore")
                    slog(f"decode_qr pyzbar OK: {data}")
                    return data

                decoded, _, _ = qr_detector.detectAndDecode(resized)
                if decoded:
                    data = decoded.strip()
                    slog(f"decode_qr cv2 OK: {data}")
                    return data

    return None


def decode_qr_fast(frame, scan_session_id=0, deadline_at=0.0):
    if should_abort_decode(scan_session_id, deadline_at):
        return None
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    eq = cv2.equalizeHist(gray)
    sharp = sharpen(eq)
    quick_variants = [
        gray,
        eq,
        sharp,
        cv2.GaussianBlur(eq, (3, 3), 0),
        cv2.threshold(eq, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)[1],
        cv2.convertScaleAbs(gray, alpha=1.35, beta=0),
        cv2.convertScaleAbs(sharp, alpha=1.2, beta=0),
    ]

    for variant in quick_variants:
        if should_abort_decode(scan_session_id, deadline_at):
            return None
        for scale in FAST_SCALE_FACTORS:
            if should_abort_decode(scan_session_id, deadline_at):
                return None
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
                data = barcodes[0].data.decode(errors="ignore")
                slog(f"decode_qr_fast pyzbar OK: {data}")
                return data

            decoded, _, _ = qr_detector.detectAndDecode(resized)
            if decoded:
                data = decoded.strip()
                slog(f"decode_qr_fast cv2 OK: {data}")
                return data

    return None


def decode_live_frame(frame, allow_full=True, scan_session_id=0, deadline_at=0.0):
    if should_abort_decode(scan_session_id, deadline_at):
        return None, ""
    preview = fit_preview(frame)
    slog(f"decode_live_frame: frame={frame.shape} preview={preview.shape}")

    gray = cv2.cvtColor(preview, cv2.COLOR_BGR2GRAY)
    eq = cv2.equalizeHist(gray)
    sharp = sharpen(gray)
    quick_variants = [
        ("preview-gray", gray),
        ("preview-eq", eq),
        ("preview-sharp", sharp),
        ("preview-blur", cv2.GaussianBlur(eq, (3, 3), 0)),
    ]

    # 1) fast whole-image pass: pyzbar + cv2 on a few grayscale variants
    for variant_name, img in quick_variants:
        if should_abort_decode(scan_session_id, deadline_at):
            return None, ""
        barcodes = pyzbar.decode(img)
        if barcodes:
            data = barcodes[0].data.decode(errors="ignore")
            slog(f"decode_live_frame pyzbar OK: {data} ({variant_name})")
            return data, variant_name
        decoded, _, _ = qr_detector.detectAndDecode(img)
        if decoded:
            data = decoded.strip()
            slog(f"decode_live_frame cv2 OK: {data} ({variant_name})")
            return data, variant_name

    # 2) detection-driven ROI decode: find QR boxes, crop + upscale to catch
    #    small or partially-visible codes that the whole-image pass missed.
    for variant_name, img in (("eq", eq), ("gray", gray)):
        if should_abort_decode(scan_session_id, deadline_at):
            return None, ""
        ok, points = qr_detector.detect(img)
        if not ok or points is None or len(points) == 0:
            continue
        for pts in points:
            if should_abort_decode(scan_session_id, deadline_at):
                return None, ""
            try:
                xs = [p[0][0] for p in pts]
                ys = [p[0][1] for p in pts]
                h, w = preview.shape[:2]
                x0, x1 = max(0, int(min(xs))), min(w, int(max(xs)))
                y0, y1 = max(0, int(min(ys))), min(h, int(max(ys)))
                if x1 - x0 < 8 or y1 - y0 < 8:
                    continue
                roi = preview[y0:y1, x0:x1]
                roi_big = cv2.resize(roi, None, fx=3.0, fy=3.0, interpolation=cv2.INTER_CUBIC)
                decoded, _, _ = qr_detector.detectAndDecode(roi_big)
                if not decoded:
                    barcodes = pyzbar.decode(roi_big)
                    if barcodes:
                        decoded = barcodes[0].data.decode(errors="ignore")
                if decoded:
                    data = decoded.strip()
                    slog(f"decode_live_frame ROI OK: {data} ({variant_name})")
                    return data, f"roi-{variant_name}"
            except Exception:
                continue

    if not allow_full or should_abort_decode(scan_session_id, deadline_at):
        return None, ""

    # 3) heavy fallback: region pyramid for very small / blurry codes
    preview_upscaled = cv2.resize(
        preview,
        None,
        fx=1.35,
        fy=1.35,
        interpolation=cv2.INTER_CUBIC,
    )

    attempts = []
    attempts.extend(build_scan_regions(frame, "raw"))
    attempts.extend(build_scan_regions(preview, "preview"))
    attempts.extend(build_scan_regions(preview_upscaled, "preview-upscaled"))

    for source_name, candidate in attempts:
        if should_abort_decode(scan_session_id, deadline_at):
            return None, ""
        if candidate is None or getattr(candidate, "size", 0) == 0:
            continue
        decoded = decode_qr(candidate, scan_session_id=scan_session_id, deadline_at=deadline_at)
        if decoded:
            slog(f"decode_live_frame found '{decoded}' via {source_name}")
            return decoded, source_name

    slog("decode_live_frame: no QR found in any variant")
    return None, ""


def lock_candidate_unlocked(payload, source_name):
    state["locked"] = True
    state["qr"] = payload
    state["locked_payload"] = payload
    state["locked_at"] = now_ms()
    state["locked_expires_at"] = now_ms() + RESULT_HOLD_MS
    state["scan_enabled"] = False
    state["candidate_payload"] = payload
    state["candidate_source"] = source_name
    state["candidate_seen_count"] = max(
        state["candidate_seen_count"], CANDIDATE_REQUIRED_COUNT
    )
    state["last_fired_payload"] = payload
    state["last_fired_at"] = now_ms()
    state["last_vanish_at"] = 0
    state["cooldown_payload"] = ""
    state["cooldown_until"] = 0
    state["session_active"] = True
    set_phase_unlocked()


def process_candidate(payload, source_name, scan_session_id):
    with lock:
        if (
            scan_session_id != int(state.get("scan_session_id") or 0)
            or not state["scan_enabled"]
            or state["locked"]
        ):
            return False

        now = now_ms()
        payload = str(payload or "").strip()

        if not payload:
            last_vanish = int(state.get("last_vanish_at") or 0)
            if not last_vanish:
                state["last_vanish_at"] = now
            elif (now - last_vanish) >= VANISH_RESET_MS:
                slog("process_candidate: code fully left scan zone for >= VANISH_RESET_MS, session ended")
                state["session_active"] = False
                state["cooldown_payload"] = ""
                state["cooldown_until"] = 0
                state["last_fired_payload"] = ""
                state["last_fired_at"] = 0
                state["last_vanish_at"] = 0
                clear_candidate_state_unlocked()
            set_phase_unlocked()
            return False

        last_vanish = int(state.get("last_vanish_at") or 0)
        if last_vanish:
            if (now - last_vanish) >= VANISH_RESET_MS:
                slog("process_candidate: new code after full exit, counting from scratch")
                state["session_active"] = False
                state["cooldown_payload"] = ""
                state["cooldown_until"] = 0
                state["last_fired_payload"] = ""
                state["last_fired_at"] = 0
            state["last_vanish_at"] = 0

        current = state["candidate_payload"]
        last_seen = int(state.get("candidate_last_seen_at") or 0)
        if current == payload and last_seen and (now - last_seen) <= CANDIDATE_PERSIST_MS:
            state["candidate_seen_count"] += 1
            slog(f"process_candidate: same payload '{payload}' count={state['candidate_seen_count']} src={source_name}")
        else:
            state["candidate_payload"] = payload
            state["candidate_source"] = source_name
            state["candidate_seen_count"] = 1
            slog(f"process_candidate: new payload '{payload}' count=1 src={source_name}")

        state["candidate_last_seen_at"] = now
        state["candidate_source"] = source_name

        if state["candidate_seen_count"] >= CANDIDATE_REQUIRED_COUNT:
            if payload == state.get("last_fired_payload"):
                state["cooldown_payload"] = payload
                state["cooldown_until"] = now + SAME_CODE_COOLDOWN_MS
                state["candidate_seen_count"] = 1
                slog(f"process_candidate: same code in cooldown '{payload}'")
                set_phase_unlocked()
                return False
            slog(f"process_candidate: LOCKED '{payload}'")
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
        cooldown_payload = state["cooldown_payload"]
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

    if cooldown_payload:
        cv2.putText(
            frame,
            f"COOLDOWN: {cooldown_payload}",
            (10, 90),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.7,
            (255, 165, 0),
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
    global latest_frame, latest_frame_seq

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
            with frame_lock:
                latest_frame = None
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
            with frame_lock:
                latest_frame = None

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
            latest_frame_seq += 1

        with lock:
            if not state["connected"]:
                slog(f"camera_worker: connected frame={frame.shape}")
            state["connected"] = True
            state["frame_ready"] = True
            set_phase_unlocked()

        time.sleep(0.01)

    if cap:
        cap.release()


def scan_worker():
    print("QR scan thread ready")
    last_scan_started_at = 0.0
    last_full_scan_at = 0.0
    last_processed_frame_seq = 0

    while not stop_flag:
        with lock:
            locked_expires_at = int(state.get("locked_expires_at") or 0)
            if (
                state["running"]
                and state["locked"]
                and locked_expires_at
                and now_ms() >= locked_expires_at
            ):
                slog("scan_worker: lock hold expired -> auto rearm")
                reset_scan_session_unlocked(scan_enabled=True)
                set_phase_unlocked()

            cooldown_until = int(state.get("cooldown_until") or 0)
            if state.get("cooldown_payload") and cooldown_until and now_ms() >= cooldown_until:
                slog("scan_worker: cooldown expired")
                state["cooldown_payload"] = ""
                state["cooldown_until"] = 0
                set_phase_unlocked()

            should_scan = state["running"] and state["scan_enabled"] and not state["locked"]
            scan_started_at = float(state.get("scan_started_at") or 0.0)
            scan_frame_seq = int(state.get("scan_frame_seq") or 0)
            scan_session_id = int(state.get("scan_session_id") or 0)

        if not should_scan:
            last_scan_started_at = 0.0
            last_full_scan_at = 0.0
            last_processed_frame_seq = 0
            time.sleep(IDLE_SLEEP_SECONDS)
            continue

        if scan_started_at != last_scan_started_at:
            last_scan_started_at = scan_started_at
            last_full_scan_at = 0.0
            last_processed_frame_seq = max(0, scan_frame_seq - 1)

        burst_mode = (
            scan_started_at > 0 and
            (time.perf_counter() - scan_started_at) <= BURST_SCAN_WINDOW_SECONDS
        )
        full_scan_interval = (
            BURST_FULL_SCAN_INTERVAL_SECONDS
            if burst_mode
            else FULL_SCAN_INTERVAL_SECONDS
        )
        now = time.perf_counter()
        allow_full = (now - last_full_scan_at) >= full_scan_interval
        frame_decode_budget = (
            BURST_FRAME_DECODE_BUDGET_SECONDS
            if burst_mode
            else FRAME_DECODE_BUDGET_SECONDS
        )

        with frame_lock:
            frame = None if latest_frame is None else latest_frame.copy()
            frame_seq = latest_frame_seq

        if frame_seq == last_processed_frame_seq:
            time.sleep(IDLE_SLEEP_SECONDS)
            continue

        if frame is None:
            with lock:
                state["frame_ready"] = False
                set_phase_unlocked()
            time.sleep(IDLE_SLEEP_SECONDS)
            continue

        scan_started = time.perf_counter()
        payload, source_name = decode_live_frame(
            frame,
            allow_full=allow_full,
            scan_session_id=scan_session_id,
            deadline_at=scan_started + frame_decode_budget,
        )
        last_processed_frame_seq = frame_seq
        if allow_full:
            last_full_scan_at = scan_started
        did_lock = process_candidate(payload, source_name, scan_session_id)
        if did_lock:
            print("QR LOCKED:", payload)

        elapsed = time.perf_counter() - scan_started
        sleep_for = max(IDLE_SLEEP_SECONDS, SCAN_TARGET_INTERVAL_SECONDS - elapsed)
        time.sleep(sleep_for)


@app.post("/qr/start")
def api_start(data: dict):
    rtsp = resolve_camera_source(data.get("rtsp"))

    with lock:
        state["rtsp"] = rtsp
        state["running"] = True
        state["connected"] = False
        state["frame_ready"] = False
        reset_scan_session_unlocked(scan_enabled=False)
        set_phase_unlocked()

    return {"success": True}


@app.post("/qr/scan")
def api_scan():
    with lock:
        reset_scan_session_unlocked(scan_enabled=True)
        set_phase_unlocked()

    return {"success": True}


@app.post("/qr/reset")
def api_reset():
    with lock:
        reset_scan_session_unlocked(scan_enabled=False)
        set_phase_unlocked()

    return {"success": True}


@app.post("/qr/stop")
def api_stop():
    with lock:
        state["running"] = False
        state["connected"] = False
        state["frame_ready"] = False
        reset_scan_session_unlocked(scan_enabled=False)
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
