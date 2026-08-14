"""Face frame quality gate for enrollment and recognition.

Applies heuristic quality checks (blur, brightness, face size, eye openness)
before an aligned face is accepted as a usable sample.
"""

from __future__ import annotations

from dataclasses import dataclass

import cv2
import numpy as np


@dataclass(frozen=True)
class QualityResult:
    passed: bool
    reasons: tuple[str, ...]
    sharpness: float
    brightness: float
    face_width: int
    eye_aspect_ratio: float


class FaceQualityGate:
    """Filters low-quality face frames."""

    def __init__(
        self,
        *,
        min_sharpness: float = 30.0,
        min_brightness: float = 60.0,
        max_brightness: float = 220.0,
        min_face_width: int = 80,
        min_eye_aspect_ratio: float = 0.18,
    ) -> None:
        self.min_sharpness = min_sharpness
        self.min_brightness = min_brightness
        self.max_brightness = max_brightness
        self.min_face_width = min_face_width
        self.min_eye_aspect_ratio = min_eye_aspect_ratio

    def evaluate(
        self,
        frame_bgr: np.ndarray,
        bbox: tuple[int, int, int, int],
        landmarks_5: np.ndarray | None = None,
    ) -> QualityResult:
        """Evaluate one face crop.

        ``bbox`` is (top, right, bottom, left) in frame coordinates and
        ``landmarks_5`` is an optional (5, 2) array (eyes first) used for EAR.
        """
        reasons: list[str] = []
        gray = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2GRAY)

        top, right, bottom, left = bbox
        face = gray[max(0, top):max(top, bottom), max(0, left):max(left, right)]
        if face.size == 0:
            return QualityResult(False, ("no-face-region",), 0.0, 0.0, 0, 0.0)

        sharpness = float(cv2.Laplacian(face, cv2.CV_64F).var())
        brightness = float(np.mean(face))
        face_width = int(right - left)

        ear = 0.0
        if landmarks_5 is not None and len(landmarks_5) >= 4:
            ear = self._eye_aspect_ratio(landmarks_5)

        if sharpness < self.min_sharpness:
            reasons.append("blur")
        if brightness < self.min_brightness or brightness > self.max_brightness:
            reasons.append("brightness")
        if face_width < self.min_face_width:
            reasons.append("face-too-small")
        if self.min_eye_aspect_ratio > 0 and ear < self.min_eye_aspect_ratio:
            reasons.append("eyes-closed")

        return QualityResult(
            passed=not reasons,
            reasons=tuple(reasons),
            sharpness=sharpness,
            brightness=brightness,
            face_width=face_width,
            eye_aspect_ratio=ear,
        )

    @staticmethod
    def _eye_aspect_ratio(landmarks_5: np.ndarray) -> float:
        """Approximate EAR from the two eye landmark points.

        YuNet landmarks 0 (right eye) and 1 (left eye) are single points, so we
        use the inter-eye width and the distance to the nose as a coarse proxy
        for openness. A higher value indicates a more open frontal face.
        """
        points = np.asarray(landmarks_5, dtype=np.float64).reshape(5, 2)
        left_eye, right_eye = points[0], points[1]
        nose = points[2]
        eye_distance = float(np.linalg.norm(left_eye - right_eye))
        if eye_distance < 1e-6:
            return 0.0
        # Vertical distance from the eye midpoint to the nose tip is a rough
        # openness proxy for a frontal face; normalize by inter-eye distance.
        mid = (left_eye + right_eye) / 2.0
        return float(abs(nose[1] - mid[1]) / eye_distance)
