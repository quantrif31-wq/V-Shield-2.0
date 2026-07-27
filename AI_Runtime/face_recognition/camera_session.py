"""Isolated camera capture and recognition state for the Face ID runtime."""

from __future__ import annotations

import base64
import threading
import time
from collections import deque
from datetime import datetime, timezone
from typing import Any, Callable

import cv2
import face_recognition
import numpy as np


MAX_READ_FAILS_BEFORE_WARN = 20
RECONNECT_DELAY_SEC = 1.0


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def _legacy_timestamp() -> str:
    return time.strftime("%Y-%m-%d %H:%M:%S")


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

        self.session_lock = threading.RLock()
        self.frame_lock = threading.Lock()
        self.generation = 0
        self.created_at = _utc_now()
        self.updated_at = self.created_at

        self._model_registry = model_registry
        self._config = config
        self._capture_factory = capture_factory
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
                last_recognition_at = self.last_recognition_at

            rgb = cv2.cvtColor(processed, cv2.COLOR_BGR2RGB)
            current_time = time.time()
            locations = face_recognition.face_locations(rgb, model="hog")
            if locations:
                face_location = locations[0]
                face_crop = self._crop_face(processed, face_location)
                snapshot_b64 = self._image_to_base64(processed)
                crop_b64 = self._image_to_base64(face_crop)
                if current_time - last_recognition_at > self._config.encode_interval:
                    encodings = face_recognition.face_encodings(rgb, [face_location])
                    # Exactly one immutable registry snapshot is used per recognition.
                    model_snapshot = self._model_registry.current_snapshot()
                    self._apply_recognition(
                        generation,
                        current_time,
                        processed,
                        face_location,
                        face_crop,
                        snapshot_b64,
                        crop_b64,
                        encodings,
                        model_snapshot,
                    )
                else:
                    self._mark_face_seen(
                        generation, current_time, face_location, snapshot_b64, crop_b64
                    )
            else:
                self._apply_no_face(generation, current_time)

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

    def _apply_recognition(
        self, generation: int, current_time: float, frame: np.ndarray,
        face_location: tuple[int, int, int, int], face_crop: np.ndarray | None,
        snapshot_b64: str | None, crop_b64: str | None,
        encodings: list[np.ndarray], model_snapshot: Any,
    ) -> None:
        with self.session_lock:
            if not self._is_current_locked(generation):
                return
            self._set_face_box_locked(face_location)
            self._last_snapshot = snapshot_b64
            self._last_face_crop = crop_b64
            if not self._tracking_active:
                self._tracking_active = True
                self._identity_confirmed = False
                self.confirmed_frames = 0
                self.cooldown_state["distance_buffer"].clear()
                self.cooldown_state["tracking_started_at"] = current_time
                self.cooldown_state["alert_triggered"] = False
                self._last_timeout = False
                self._last_alert = False
            self.last_face_seen_at = current_time
            if encodings and model_snapshot.encoding_count:
                distances = face_recognition.face_distance(
                    model_snapshot.encodings, encodings[0]
                )
                best_index = int(np.argmin(distances))
                distance = float(distances[best_index])
                is_match = distance < self._config.threshold
                self.cooldown_state["distance_buffer"].append(distance)
                self._last_distance = float(
                    sum(self.cooldown_state["distance_buffer"])
                    / len(self.cooldown_state["distance_buffer"])
                )
                self._last_face_match = bool(is_match)
                if is_match:
                    self.confirmed_frames += 1
                    self.confirmed_subject_id = model_snapshot.subject_ids[best_index]
                else:
                    self.confirmed_frames = 0
                    self._identity_confirmed = False
                    self.confirmed_subject_id = None
                if (
                    self.confirmed_frames >= self._config.confirm_frames
                    and not self._identity_confirmed
                ):
                    self._identity_confirmed = True
                    self._lock_result_locked("confirmed", frame, face_crop)
            else:
                self._last_face_match = False
                self.confirmed_subject_id = None
                self._last_distance = None
            self.last_recognition_at = current_time
            self._apply_timeouts_locked(current_time, frame, face_crop)
            self._publish_state_locked()

    def _mark_face_seen(
        self, generation: int, current_time: float,
        face_location: tuple[int, int, int, int],
        snapshot_b64: str | None, crop_b64: str | None,
    ) -> None:
        with self.session_lock:
            if not self._is_current_locked(generation):
                return
            self._set_face_box_locked(face_location)
            self._last_snapshot = snapshot_b64
            self._last_face_crop = crop_b64
            self.last_face_seen_at = current_time
            self._publish_state_locked()

    def _apply_no_face(self, generation: int, current_time: float) -> None:
        with self.session_lock:
            if not self._is_current_locked(generation):
                return
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
        if self._last_face_box is not None:
            box = self._last_face_box
            color = (0, 255, 255) if self._scan_locked else (0, 255, 0)
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
