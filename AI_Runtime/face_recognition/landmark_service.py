"""Optional MediaPipe FaceLandmarker wrapper for pose guidance.

Keeps MediaPipe optional so the runtime degrades gracefully on systems where
the optional dependency is not installed. When available, it provides the 478
3D landmarks and the facial transformation matrix used by pose_guide.
"""

from __future__ import annotations

from pathlib import Path

import numpy as np


class LandmarkServiceError(RuntimeError):
    """Raised when MediaPipe landmarks cannot be initialized."""


class LandmarkService:
    """Wraps MediaPipe FaceLandmarker (IMAGE mode)."""

    def __init__(self, model_path: str | Path, *, num_faces: int = 1) -> None:
        model_path = str(model_path)
        if not Path(model_path).is_file():
            raise LandmarkServiceError(
                "Face landmarker model file was not found: %s" % model_path
            )
        try:
            import mediapipe as mp
            from mediapipe.tasks import python
            from mediapipe.tasks.python import vision
        except Exception as exc:  # pragma: no cover - optional dependency
            raise LandmarkServiceError(
                "MediaPipe is not available: %s" % exc
            ) from exc

        self._mp = mp
        self._vision = vision
        try:
            self._landmarker = vision.FaceLandmarker.create_from_options(
                vision.FaceLandmarkerOptions(
                    base_options=python.BaseOptions(
                        model_asset_path=model_path
                    ),
                    running_mode=vision.RunningMode.IMAGE,
                    num_faces=int(num_faces),
                    output_face_blendshapes=False,
                    output_facial_transformation_matrixes=True,
                )
            )
        except Exception as exc:  # pragma: no cover - depends on build
            raise LandmarkServiceError(
                "Face landmarker could not be created: %s" % exc
            ) from exc

    def estimate(
        self, frame_bgr: np.ndarray
    ) -> tuple[np.ndarray | None, np.ndarray | None]:
        """Return (landmarks_478, transformation_matrix_4x4) or (None, None).

        ``landmarks_478`` is a (478, 3) normalized array when a face is found.
        """
        if frame_bgr is None or frame_bgr.size == 0:
            return None, None
        try:
            rgb = self._mp.Image(
                image_format=self._mp.ImageFormat.SRGB,
                data=self._mp_image_data(frame_bgr),
            )
            result = self._landmarker.detect(rgb)
        except Exception:
            return None, None
        if not result.face_landmarks:
            return None, None

        raw = result.face_landmarks[0]
        landmarks = np.asarray(
            [[point.x, point.y, point.z] for point in raw],
            dtype=np.float64,
        ).reshape(478, 3)

        matrix = None
        if result.facial_transformation_matrixes:
            matrix = np.asarray(
                result.facial_transformation_matrixes[0], dtype=np.float64
            ).reshape(4, 4)
        return landmarks, matrix

    def _mp_image_data(self, frame_bgr: np.ndarray):
        import cv2

        return cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
