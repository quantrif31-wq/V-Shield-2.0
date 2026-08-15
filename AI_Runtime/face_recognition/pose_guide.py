"""Guided face pose estimation and multi-angle enrollment guidance.

Uses MediaPipe FaceLandmarker (478 3D points) with the facial transformation
matrix to estimate yaw/pitch/roll and drive a Vietnamese-language state machine
that guides the subject through head turns for multi-angle enrollment.
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field

import numpy as np

# Pose bins for the 3x3 coverage grid (yaw x pitch), in degrees.
# Thresholds are intentionally wide so a real person can hit every cell with a
# modest head turn in front of an ordinary camera.
YAW_BINS = (-12.0, 12.0)
PITCH_BINS = (-8.0, 8.0)

# Frame needs to hold each pose bin for at least this many frames.
MIN_FRAMES_PER_BIN = 3

# Easier enrollment modes: fewer head turns for elderly users.
#   easy   -> only center + left + right (3 turns)
#   simple -> center + left + right + up + down (5 turns)
#   full   -> all 9 cells
EASY_TARGET_BINS = ["LM", "CM", "RM"]
SIMPLE_TARGET_BINS = ["LU", "CM", "RM", "LM", "CD"]
FULL_TARGET_BINS = None  # all 9


@dataclass
class PoseSample:
    yaw_deg: float
    pitch_deg: float
    roll_deg: float
    detection_score: float = 1.0
    landmarks_count: int = 478


@dataclass
class PoseGuideState:
    guidance: str
    progress: int = 0
    total: int = 9
    current_bin: str | None = None
    remaining: list[str] = field(default_factory=list)
    bins_covered: list[str] = field(default_factory=list)
    complete: bool = False
    just_covered: str | None = None
    same_bin: bool = False


def euler_from_matrix(matrix: np.ndarray) -> tuple[float, float, float]:
    """Extract (yaw, pitch, roll) in degrees from a MediaPipe 4x4 transform.

    Verified empirically against MediaPipe ``faceTransformationMatrixes``:
    rotating the input image (a pure roll) must move ``roll`` and leave ``yaw``
    near zero. The correct decomposition uses the raw rotation block (no
    transpose) with yaw/roll swapped relative to the naive formula — otherwise
    head turns (yaw) and head tilts (roll) are reported backwards.
    """
    rotation = np.asarray(matrix, dtype=np.float64).reshape(4, 4)[:3, :3]
    r = rotation

    sy = math.sqrt(r[0, 0] ** 2 + r[1, 0] ** 2)
    singular = sy < 1e-6

    if not singular:
        pitch = math.atan2(-r[2, 0], sy)
        roll = math.atan2(r[1, 0], r[0, 0])
        yaw = math.atan2(r[2, 1], r[2, 2])
    else:
        pitch = math.atan2(-r[2, 0], sy)
        roll = math.atan2(-r[1, 2], r[1, 1])
        yaw = 0.0

    return (
        math.degrees(yaw),
        math.degrees(pitch),
        math.degrees(roll),
    )


def pose_bin(yaw_deg: float, pitch_deg: float) -> str:
    """Return the 3x3 grid bin name for a yaw/pitch pair."""
    yaw_key = (
        "L" if yaw_deg < YAW_BINS[0]
        else "R" if yaw_deg > YAW_BINS[1]
        else "C"
    )
    pitch_key = (
        "U" if pitch_deg < PITCH_BINS[0]
        else "D" if pitch_deg > PITCH_BINS[1]
        else "M"
    )
    return f"{yaw_key}{pitch_key}"


ALL_BINS = [
    pose_bin(yaw, pitch)
    for yaw in (-30.0, 0.0, 30.0)
    for pitch in (-20.0, 0.0, 20.0)
]


class PoseGuide:
    """State machine guiding a subject through the pose coverage grid."""

    def __init__(
        self,
        *,
        min_frames_per_bin: int = MIN_FRAMES_PER_BIN,
        yaw_bins: tuple[float, float] = YAW_BINS,
        pitch_bins: tuple[float, float] = PITCH_BINS,
        target_bins: list[str] | None = None,
    ) -> None:
        global YAW_BINS, PITCH_BINS, ALL_BINS
        YAW_BINS = yaw_bins
        PITCH_BINS = pitch_bins
        ALL_BINS = [
            pose_bin(yaw, pitch)
            for yaw in (yaw_bins[0] - 5, 0.0, yaw_bins[1] + 5)
            for pitch in (pitch_bins[0] - 5, 0.0, pitch_bins[1] + 5)
        ]
        self.min_frames_per_bin = min_frames_per_bin
        self.target_bins = (
            [bin_name for bin_name in ALL_BINS if bin_name in target_bins]
            if target_bins
            else list(ALL_BINS)
        )
        self.reset()

    def reset(self) -> None:
        self._bin_frames: dict[str, int] = {bin_name: 0 for bin_name in ALL_BINS}
        self._coverage: set[str] = set()
        self._complete = False
        self._current_bin: str | None = None
        self._last_guidance = "Hãy nhìn thẳng vào camera"
        self._target_bin: str | None = None

    @property
    def complete(self) -> bool:
        return self._complete

    def update(self, yaw_deg: float, pitch_deg: float) -> PoseGuideState:
        """Consume one pose sample and return the next guidance instruction."""
        bin_name = pose_bin(yaw_deg, pitch_deg)
        was_covered = bin_name in self._coverage
        self._current_bin = bin_name
        # Only count frames that belong to an enrolment target cell.
        if bin_name in self.target_bins:
            self._bin_frames[bin_name] = self._bin_frames.get(bin_name, 0) + 1
            if self._bin_frames[bin_name] >= self.min_frames_per_bin:
                self._coverage.add(bin_name)

        just_covered = bin_name if not was_covered and bin_name in self._coverage else None
        self._complete = set(self._coverage) == set(self.target_bins)
        if self._complete:
            self._target_bin = None
        else:
            # Refresh the target whenever a bin was just covered so the guidance
            # always points at a not-yet-covered cell.
            self._target_bin = self._nearest_uncovered(bin_name)
        guidance = self._guidance_for(bin_name, yaw_deg, pitch_deg)
        self._last_guidance = guidance

        covered = [bin_name for bin_name in self.target_bins if bin_name in self._coverage]
        remaining = [bin_name for bin_name in self.target_bins if bin_name not in self._coverage]
        return PoseGuideState(
            guidance=guidance,
            progress=len(covered),
            total=len(self.target_bins),
            current_bin=bin_name,
            remaining=remaining,
            bins_covered=covered,
            complete=self._complete,
            just_covered=just_covered,
            same_bin=was_covered and bin_name in self._coverage,
        )

    def _nearest_uncovered(self, from_bin: str) -> str | None:
        """Return the uncovered target bin closest to ``from_bin``."""
        uncovered = [
            bin_name for bin_name in self.target_bins if bin_name not in self._coverage
        ]
        if not uncovered:
            return None
        return min(uncovered, key=lambda candidate: self._grid_distance(from_bin, candidate))

    @staticmethod
    def _grid_distance(a: str, b: str) -> int:
        yaw_index = {"L": 0, "C": 1, "R": 2}
        pitch_index = {"U": 0, "M": 1, "D": 2}
        dy = abs(yaw_index[a[0]] - yaw_index[b[0]])
        dp = abs(pitch_index[a[1]] - pitch_index[b[1]])
        return dy + dp

    def _guidance_for(self, bin_name: str, yaw_deg: float, pitch_deg: float) -> str:
        if self._complete:
            return "Hoàn tất, giữ yên 2 giây"

        target = self._target_bin or bin_name

        # Already holding the target cell: keep collecting until covered.
        if bin_name == target:
            return "Giữ yên, đang thu thập góc này"

        # Target lies elsewhere: steer one stable direction at a time, using a
        # wider hysteresis so jitter near bin edges does not flip the prompt.
        yaw_target = _YAW_ORDER.index(target[0])
        yaw_current = _YAW_ORDER.index(bin_name[0])
        pitch_target = _PITCH_ORDER.index(target[1])
        pitch_current = _PITCH_ORDER.index(bin_name[1])

        # Prefer the larger deviation to make the instruction unambiguous.
        yaw_gap = yaw_target - yaw_current
        pitch_gap = pitch_target - pitch_current
        if abs(yaw_gap) >= abs(pitch_gap) and yaw_gap != 0:
            return "Từ từ quay mặt sang TRÁI" if yaw_gap < 0 else "Từ từ quay mặt sang PHẢI"
        if pitch_gap != 0:
            return "Ngẩng nhẹ lên trên" if pitch_gap < 0 else "Cúi nhẹ xuống dưới"

        # Fallback: re-aim at the nearest uncovered cell.
        return "Giữ yên, đang thu thập nhiều góc"

    @staticmethod
    def _yaw_key(offset: int) -> str | None:
        return {-1: "L", 0: "C", 1: "R"}.get(offset)

    @staticmethod
    def _pitch_key(offset: int) -> str | None:
        return {-1: "U", 0: "M", 1: "D"}.get(offset)


_YAW_ORDER = ("L", "C", "R")
_PITCH_ORDER = ("U", "M", "D")
