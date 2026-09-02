"""Thread-safe lifecycle management for isolated Face ID camera sessions."""

from __future__ import annotations

import re
import threading
from typing import Any, Callable

from camera_session import CameraSession, CameraSessionConflictError


CAMERA_ID_PATTERN = re.compile(r"^[A-Za-z0-9_.-]{1,64}$")


class CameraManagerError(RuntimeError):
    status_code = 500
    error_code = "CAMERA_MANAGER_ERROR"


class InvalidCameraIdError(CameraManagerError):
    status_code = 400
    error_code = "INVALID_CAMERA_ID"


class CameraNotFoundError(CameraManagerError):
    status_code = 404
    error_code = "CAMERA_NOT_FOUND"


class CameraConflictError(CameraManagerError):
    status_code = 409
    error_code = "CAMERA_CONFLICT"


def validate_camera_id(camera_id: str) -> str:
    value = str(camera_id or "")
    if (
        not CAMERA_ID_PATTERN.fullmatch(value)
        or ".." in value
        or value != value.strip()
    ):
        raise InvalidCameraIdError("cameraId is invalid.")
    return value


class CameraManager:
    def __init__(
        self,
        model_registry: Any,
        config: Any,
        *,
        session_factory: Callable[..., CameraSession] = CameraSession,
        capture_factory: Callable[[str], Any] | None = None,
        detector: Any | None = None,
        embedder: Any | None = None,
    ) -> None:
        self._model_registry = model_registry
        self._config = config
        self._max_cameras = config.max_cameras
        self._session_factory = session_factory
        self._capture_factory = capture_factory
        self._detector = detector
        self._embedder = embedder
        self._sessions: dict[str, CameraSession] = {}
        self._lock = threading.RLock()

    @property
    def max_cameras(self) -> int:
        return self._max_cameras

    def list_sessions(self) -> dict[str, Any]:
        with self._lock:
            sessions = list(self._sessions.values())
        metadata = [session.metadata() for session in sessions]
        return {
            "maxCameras": self._max_cameras,
            "activeCount": sum(1 for item in metadata if item["enabled"]),
            "sessions": sorted(metadata, key=lambda item: item["cameraId"]),
        }

    def get_session(self, camera_id: str) -> CameraSession:
        clean_id = validate_camera_id(camera_id)
        with self._lock:
            session = self._sessions.get(clean_id)
        if session is None:
            raise CameraNotFoundError("Camera session was not found.")
        return session

    def ensure_session(self, camera_id: str) -> CameraSession:
        """Create a stopped session when compatibility routes need stable state."""
        clean_id = validate_camera_id(camera_id)
        with self._lock:
            session = self._sessions.get(clean_id)
            if session is None:
                session = self._session_factory(
                    clean_id,
                    self._model_registry,
                    self._config,
                    capture_factory=self._capture_factory,
                    detector=self._detector,
                    embedder=self._embedder,
                )
                self._sessions[clean_id] = session
            return session

    def start_session(
        self, camera_id: str, stream_url: str, lane_id: str | None = None
    ) -> tuple[CameraSession, bool]:
        clean_id = validate_camera_id(camera_id)
        clean_url = str(stream_url or "").strip()
        if not clean_url:
            raise ValueError("Camera stream URL is required.")

        # start() only mutates state and launches workers; camera open/read happens
        # in a worker after this short manager critical section is released.
        with self._lock:
            session = self._sessions.get(clean_id)
            if session is None:
                active = sum(
                    1 for current in self._sessions.values() if current.enabled
                )
                if active >= self._max_cameras:
                    raise CameraConflictError("Maximum active camera count reached.")
                session = self._session_factory(
                    clean_id,
                    self._model_registry,
                    self._config,
                    capture_factory=self._capture_factory,
                    detector=self._detector,
                    embedder=self._embedder,
                )
                self._sessions[clean_id] = session
            elif not session.enabled:
                active = sum(
                    1 for current in self._sessions.values() if current.enabled
                )
                if active >= self._max_cameras:
                    raise CameraConflictError("Maximum active camera count reached.")

            try:
                idempotent = session.start(clean_url, lane_id)
            except CameraSessionConflictError as exc:
                raise CameraConflictError(str(exc)) from exc
        return session, idempotent

    def stop_session(self, camera_id: str) -> CameraSession:
        try:
            session = self.get_session(camera_id)
            session.stop()
            return session
        except CameraNotFoundError:
            return self.ensure_session(camera_id)

    def reset_session(self, camera_id: str) -> CameraSession:
        session = self.get_session(camera_id)
        session.reset()
        return session

    def get_status(self, camera_id: str) -> dict[str, Any]:
        try:
            return self.get_session(camera_id).status()
        except CameraNotFoundError:
            return {
                "success": True,
                "camera_enabled": False,
                "camera_connected": False,
                "ip": "",
                "fps": 0,
                "message": "Camera session is stopped.",
                "last_update": "",
            }

    def get_result(self, camera_id: str) -> dict[str, Any]:
        return self.get_session(camera_id).result(include_images=True)

    def get_locked_images(self, camera_id: str) -> dict[str, Any]:
        return self.get_session(camera_id).locked_image_result()

    def get_intruders(self, camera_id: str) -> dict[str, Any]:
        return self.get_session(camera_id).intruder_result()

    def clear_intruders(self, camera_id: str) -> dict[str, Any]:
        return self.get_session(camera_id).clear_intruders()

    def get_events(self, camera_id: str, *, after_sequence: int = 0,
                   session_generation: int | None = None,
                   limit: int = 100) -> dict[str, Any]:
        return self.get_session(camera_id).events(
            after_sequence=after_sequence,
            session_generation=session_generation, limit=limit)

    def shutdown_all(self) -> None:
        with self._lock:
            sessions = list(self._sessions.values())
        for session in sessions:
            session.stop()
