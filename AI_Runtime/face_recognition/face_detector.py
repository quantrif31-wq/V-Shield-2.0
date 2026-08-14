"""YuNet face detector with optional lightweight tracking.

Wraps OpenCV's FaceDetectorYN (opencv-contrib-python) so detection runs on CPU
quickly at configurable intervals; heavier embedding runs on GPU elsewhere.
"""

from __future__ import annotations

from pathlib import Path

import cv2
import numpy as np


class FaceDetectorError(RuntimeError):
    """Raised when the YuNet detector cannot be initialized."""


class FaceDetector:
    """Detects faces with YuNet, returning OpenCV detections (N x 15)."""

    def __init__(
        self,
        model_path: str | Path,
        *,
        input_size: tuple[int, int] = (320, 320),
        score_threshold: float = 0.6,
        nms_threshold: float = 0.3,
        top_k: int = 5000,
    ) -> None:
        model_path = str(model_path)
        if not Path(model_path).is_file():
            raise FaceDetectorError(
                "YuNet model file was not found: %s" % model_path
            )
        try:
            self._detector = cv2.FaceDetectorYN.create(
                model_path,
                "",
                input_size,
                score_threshold,
                nms_threshold,
                top_k,
            )
        except Exception as exc:  # pragma: no cover - depends on build
            raise FaceDetectorError(
                "YuNet detector could not be created: %s" % exc
            ) from exc
        self._input_size = input_size

    def detect(self, frame: np.ndarray) -> np.ndarray | None:
        """Detect faces in a BGR frame.

        Returns an N x 15 array (x, y, w, h, five landmarks, score) or None.
        """
        if frame is None or frame.size == 0:
            return None
        height, width = frame.shape[:2]
        try:
            self._detector.setInputSize((width, height))
            ok, detections = self._detector.detect(frame)
        except Exception:
            return None
        if not ok or detections is None or len(detections) == 0:
            return None
        return np.asarray(detections, dtype=np.float64)

    @staticmethod
    def box_from_detection(detection: np.ndarray) -> tuple[int, int, int, int]:
        """Convert a YuNet row to (top, right, bottom, left) pixel box."""
        x, y, w, h = (float(value) for value in detection[:4])
        left = int(round(x))
        top = int(round(y))
        right = int(round(x + w))
        bottom = int(round(y + h))
        return top, right, bottom, left

    @staticmethod
    def landmarks_from_detection(detection: np.ndarray) -> np.ndarray:
        """Return the five landmarks as a (5, 2) float array.

        YuNet landmark order matches the InsightFace/SFace reference order:
        right eye, left eye, nose, right mouth corner, left mouth corner.
        """
        values = detection[4:14]
        return values.reshape(5, 2).astype(np.float64)
