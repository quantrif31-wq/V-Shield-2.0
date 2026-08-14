"""Continuous face enrollment with live pose coverage (video-based).

The subject looks at the camera and slowly turns their head; frames are
captured continuously while exactly one face is visible. Each frame is assigned
to one of five pose angles (straight / left / right / up / down). Recording
auto-pauses when no face or more than one face is detected, and resumes when
exactly one face is present. Once all five angles are covered the enrollment
may be confirmed at any time (further turning keeps collecting samples until
then). A hard 60-second limit aborts the session.
"""

from __future__ import annotations

import threading
import time
import uuid
from datetime import datetime, timezone
from typing import Any

import cv2
import numpy as np

from face_detector import FaceDetector
from pose_guide import euler_from_matrix

ANGLES = ("straight", "left", "right", "up", "down")
ANGLE_LABELS = {
    "straight": "Thẳng",
    "left": "Trái",
    "right": "Phải",
    "up": "Lên",
    "down": "Xuống",
}

DEFAULT_YAW_THRESHOLD = 10.0
DEFAULT_PITCH_THRESHOLD = 6.0
DEFAULT_ANGLE_FRAMES = 1
DEFAULT_MAX_SECONDS = 120


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


class GuidedEnrollmentError(RuntimeError):
    def __init__(self, code: str, message: str, status_code: int, **details):
        super().__init__(message)
        self.code = code
        self.status_code = status_code
        self.details = details


def classify_angle(
    yaw_deg: float,
    pitch_deg: float,
    *,
    yaw_threshold: float = DEFAULT_YAW_THRESHOLD,
    pitch_threshold: float = DEFAULT_PITCH_THRESHOLD,
) -> str:
    """Map a yaw/pitch pair to one of the five enrollment angles."""
    if yaw_deg < -yaw_threshold:
        return "left"
    if yaw_deg > yaw_threshold:
        return "right"
    if pitch_deg < -pitch_threshold:
        return "up"
    if pitch_deg > pitch_threshold:
        return "down"
    return "straight"


class GuidedEnrollmentSession:
    """Owns one camera stream and the continuous enrollment worker."""

    def __init__(
        self,
        config: Any,
        *,
        detector: Any | None = None,
        embedder: Any | None = None,
        landmark_service: Any | None = None,
        capture_factory: Any | None = None,
    ) -> None:
        self._config = config
        self._detector = detector
        self._embedder = embedder
        self._landmark_service = landmark_service
        self._capture_factory = capture_factory

        self.session_id = str(uuid.uuid4())
        self.stream_url = ""
        self.status = "idle"  # idle | running | complete | error
        self.last_error: str | None = None
        self.guidance = "Hãy nhìn thẳng vào camera"
        self.face_state = "none"  # none | single | multiple
        self.current_angle: str | None = None
        self.covered_angles: list[str] = []
        self.missing_angles: list[str] = list(ANGLES)
        self.angles_complete = False
        self.samples_collected = 0
        self.duration_ms = 0
        self.current_pose: dict | None = None
        self.complete = False

        self._embeddings: list[dict] = []
        self._capture: Any | None = None
        self._stop_event = threading.Event()
        self._worker: threading.Thread | None = None
        self._lock = threading.RLock()
        self._started_at = 0.0

        self._yaw_threshold = float(getattr(config, "enrollment_yaw_threshold", DEFAULT_YAW_THRESHOLD))
        self._pitch_threshold = float(getattr(config, "enrollment_pitch_threshold", DEFAULT_PITCH_THRESHOLD))
        self._angle_frames = int(getattr(config, "enrollment_angle_frames", DEFAULT_ANGLE_FRAMES))
        self._max_seconds = float(getattr(config, "enrollment_max_seconds", DEFAULT_MAX_SECONDS))

        # Low-pass filter for yaw/pitch to keep prompts stable under jitter.
        self._smooth_yaw: float | None = None
        self._smooth_pitch: float | None = None
        self._pose_alpha = 0.7

    def start(self, stream_url: str, pose_mode: str | None = None) -> None:
        clean_url = str(stream_url or "").strip()
        if not clean_url:
            raise GuidedEnrollmentError("InvalidStreamUrl", "Stream URL is required.", 400)

        with self._lock:
            if self.status == "running":
                raise GuidedEnrollmentError(
                    "EnrollmentInProgress",
                    "Guided enrollment is already running.", 409)
            self.session_id = str(uuid.uuid4())
            self.stream_url = clean_url
            self.status = "running"
            self.last_error = None
            self.guidance = "Hãy nhìn thẳng vào camera"
            self.face_state = "none"
            self.current_angle = None
            self.covered_angles = []
            self.missing_angles = list(ANGLES)
            self.angles_complete = False
            self.samples_collected = 0
            self.duration_ms = 0
            self.current_pose = None
            self.complete = False
            self._embeddings = []
            self._smooth_yaw = None
            self._smooth_pitch = None
            self._started_at = time.monotonic()
            self._stop_event.clear()
            self._worker = threading.Thread(
                target=self._run,
                args=(clean_url,),
                name=f"guided-enroll-{self.session_id[:8]}",
                daemon=True,
            )
            self._worker.start()

    def stop(self) -> None:
        with self._lock:
            self._stop_event.set()
            worker = self._worker
            capture = self._capture
            self._capture = None
        self._release_capture(capture)
        if worker is not None and worker is not threading.current_thread() and worker.is_alive():
            worker.join(timeout=3.0)
        with self._lock:
            if self.status == "running":
                self.status = "idle"

    def snapshot(self) -> dict:
        with self._lock:
            return {
                "sessionId": self.session_id,
                "status": self.status,
                "streamUrl": self.stream_url,
                "guidance": self.guidance,
                "faceState": self.face_state,
                "currentAngle": self.current_angle,
                "currentPose": self.current_pose,
                "progress": len(self.covered_angles),
                "totalAngles": len(ANGLES),
                "coveredAngles": list(self.covered_angles),
                "missingAngles": list(self.missing_angles),
                "anglesComplete": self.angles_complete,
                "samplesCollected": self.samples_collected,
                "durationMs": int(self.duration_ms),
                "lastError": self.last_error,
            }

    def captured_vectors(self) -> list[np.ndarray]:
        with self._lock:
            return [
                np.asarray(item["vector"], dtype=np.float64)
                for item in self._embeddings
            ]

    def captured_pose_metadata(self) -> dict:
        with self._lock:
            all_poses = [item["pose"] for item in self._embeddings if item.get("pose")]
            if not all_poses:
                return {}
            yaws = [p["yaw"] for p in all_poses]
            pitches = [p["pitch"] for p in all_poses]
            return {
                "yaw_range": [round(min(yaws), 2), round(max(yaws), 2)],
                "pitch_range": [round(min(pitches), 2), round(max(pitches), 2)],
                "angles_covered": list(self.covered_angles),
                "sample_count": len(all_poses),
            }

    def _run(self, stream_url: str) -> None:
        capture = None
        try:
            capture = self._open_capture(stream_url)
            with self._lock:
                self._capture = capture
                opened = capture.isOpened()
                if not opened:
                    self.status = "error"
                    self.last_error = "Cannot open camera stream"
                    self.guidance = "Không mở được camera"
                    return

            last_sample_at = 0.0
            while not self._stop_event.is_set():
                ok, frame = capture.read()
                if not ok or frame is None:
                    total_frames = int(capture.get(cv2.CAP_PROP_FRAME_COUNT) or 0)
                    if total_frames > 1:
                        try:
                            capture.set(cv2.CAP_PROP_POS_FRAMES, 0)
                            continue
                        except Exception:
                            pass
                    self._stop_event.wait(0.05)
                    continue
                now = time.time()
                with self._lock:
                    if self.status != "running":
                        return
                    if self.complete:
                        return
                    self.duration_ms = int((time.monotonic() - self._started_at) * 1000)
                    # Timeout only blocks an INCOMPLETE session. Once all five
                    # angles are covered the subject may keep going and confirm
                    # whenever they are ready.
                    if not self.angles_complete and self.duration_ms / 1000.0 > self._max_seconds:
                        self.status = "error"
                        self.last_error = "Thời gian vượt quá giới hạn. Vui lòng quay lại."
                        self.guidance = "Quá giới hạn thời gian. Bấm Bắt đầu để quay lại."
                        return
                    if now - last_sample_at < self._config.enroll_interval:
                        continue
                    last_sample_at = now
                    self._process_frame(frame)
        except Exception as exc:
            with self._lock:
                self.status = "error"
                self.last_error = str(exc)
        finally:
            self._release_capture(capture)
            with self._lock:
                if self._capture is capture:
                    self._capture = None

    def _process_frame(self, frame: np.ndarray) -> None:
        if self._detector is None or self._embedder is None:
            return

        detections = self._detector.detect(frame)

        # ---- Face state gating: none / multiple pause recording ----
        if detections is None or len(detections) == 0:
            self.face_state = "none"
            self.current_angle = None
            self.current_pose = None
            self.guidance = "Không thấy khuôn mặt. Hãy bước vào trước camera"
            return
        if len(detections) > 1:
            self.face_state = "multiple"
            self.current_angle = None
            self.current_pose = None
            self.guidance = "Phát hiện nhiều khuôn mặt. Chỉ để lại 1 người"
            return
        self.face_state = "single"

        detection = detections[0]
        landmarks = FaceDetector.landmarks_from_detection(detection)
        bbox = FaceDetector.box_from_detection(detection)

        top, right, bottom, left = bbox
        face_width = int(right - left)
        if face_width < getattr(self._config, "face_quality_min_face_width", 60):
            self.guidance = "Hãy lại gần camera hơn"
            self.current_pose = None
            return

        pose = None
        if self._landmark_service is not None:
            pose = self._estimate_pose(frame, landmarks)
        self.current_pose = pose
        if pose is None:
            self.guidance = "Chưa xác định được hướng mặt. Nhìn thẳng vào camera"
            return

        yaw = pose["yaw"]
        pitch = pose["pitch"]
        if self._smooth_yaw is None:
            self._smooth_yaw = yaw
            self._smooth_pitch = pitch
        else:
            self._smooth_yaw = self._pose_alpha * yaw + (1 - self._pose_alpha) * self._smooth_yaw
            self._smooth_pitch = self._pose_alpha * pitch + (1 - self._pose_alpha) * self._smooth_pitch
        pose = {"yaw": float(self._smooth_yaw), "pitch": float(self._smooth_pitch), "roll": pose["roll"]}
        self.current_pose = pose

        angle = classify_angle(
            pose["yaw"], pose["pitch"],
            yaw_threshold=self._yaw_threshold,
            pitch_threshold=self._pitch_threshold,
        )
        self.current_angle = angle

        if angle not in self.covered_angles:
            self._angle_hold_frames = getattr(self, "_angle_hold_frames", {})
            self._angle_hold_frames[angle] = self._angle_hold_frames.get(angle, 0) + 1
            if self._angle_hold_frames[angle] >= self._angle_frames:
                self.covered_angles.append(angle)
                self.missing_angles = [
                    a for a in ANGLES if a not in self.covered_angles
                ]
                self.angles_complete = len(self.covered_angles) >= len(ANGLES)

        # Capture a sample for every frame with exactly one face (continuous,
        # no per-angle snapshots). Cap per-angle to avoid unbounded growth.
        try:
            vector = self._embedder.align_and_embed(frame, landmarks)
            vector = np.asarray(vector, dtype=np.float64)
            if vector.shape == (128,) and np.all(np.isfinite(vector)):
                per_angle = sum(1 for item in self._embeddings if item.get("angle") == angle)
                if per_angle < 12:
                    self._embeddings.append({"vector": vector, "pose": pose, "angle": angle})
                    self.samples_collected = len(self._embeddings)
        except Exception:
            pass

        if self.angles_complete:
            self.guidance = (
                "Đã đủ 5 góc! Có thể bấm Xác nhận, hoặc tiếp tục quay để cải thiện mẫu."
            )
        else:
            self.guidance = self._guidance_for_missing()

    def _guidance_for_missing(self) -> str:
        missing = self.missing_angles
        if "straight" in missing:
            return "Nhìn thẳng vào camera"
        if "left" in missing:
            return "Từ từ quay mặt sang TRÁI"
        if "right" in missing:
            return "Từ từ quay mặt sang PHẢI"
        if "up" in missing:
            return "Ngẩng mặt nhẹ lên trên"
        if "down" in missing:
            return "Cúi mặt nhẹ xuống dưới"
        return "Đã đủ 5 góc, có thể bấm Xác nhận"

    def _estimate_pose(self, frame: np.ndarray, landmarks: np.ndarray | None = None) -> dict | None:
        pose = self._pose_solve_pnp(frame, landmarks)
        if pose is not None:
            return pose
        try:
            _, matrix = self._landmark_service.estimate(frame)
        except Exception:
            return None
        if matrix is None:
            return None
        try:
            yaw, pitch, roll = euler_from_matrix(matrix)
        except Exception:
            return None
        return {"yaw": float(yaw), "pitch": float(pitch), "roll": float(roll)}

    @staticmethod
    def _pose_solve_pnp(
        frame: np.ndarray, landmarks: np.ndarray | None
    ) -> dict | None:
        if landmarks is None or landmarks.shape != (5, 2):
            return None
        try:
            height, width = frame.shape[:2]
            model_3d = np.array([
                [-73.39331055, 29.97380829, 47.30763245],
                [73.39331055, 29.97380829, 47.30763245],
                [0.0, -3.0, 92.0],
                [-48.5881424, -45.80829144, 30.2684937],
                [48.5881424, -45.80829144, 30.2684937],
            ], dtype=np.float64)
            camera_matrix = np.array([
                [width, 0.0, width / 2.0],
                [0.0, width, height / 2.0],
                [0.0, 0.0, 1.0],
            ], dtype=np.float64)
            dist_coeffs = np.zeros((4, 1), dtype=np.float64)
            success, rvec, _ = cv2.solvePnP(
                model_3d, landmarks.astype(np.float64),
                camera_matrix, dist_coeffs,
                flags=cv2.SOLVEPNP_EPNP,
            )
            if not success:
                return None
            rotation, _ = cv2.Rodrigues(rvec)
            pitch = float(np.degrees(np.arcsin(-rotation[2, 0])))
            yaw = float(np.degrees(np.arctan2(rotation[1, 0], rotation[0, 0])))
            roll = float(np.degrees(np.arctan2(rotation[2, 1], rotation[2, 2])))
            return {"yaw": yaw, "pitch": pitch, "roll": roll}
        except Exception:
            return None

    def _open_capture(self, stream_url: str):
        if self._capture_factory is not None:
            return self._capture_factory(stream_url)
        capture = cv2.VideoCapture(stream_url, cv2.CAP_FFMPEG)
        try:
            capture.set(cv2.CAP_PROP_BUFFERSIZE, 1)
        except Exception:
            pass
        return capture

    @staticmethod
    def _release_capture(capture: Any | None) -> None:
        if capture is not None:
            try:
                capture.release()
            except Exception:
                pass
