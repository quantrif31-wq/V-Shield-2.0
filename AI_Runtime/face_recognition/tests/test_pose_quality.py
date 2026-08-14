"""Unit tests for pose guidance and face quality gates."""

import unittest

import numpy as np

from face_quality import FaceQualityGate
from pose_guide import PoseGuide, euler_from_matrix, pose_bin


class PoseGuideTests(unittest.TestCase):
    def test_euler_from_identity_is_zero(self):
        identity = np.eye(4)
        yaw, pitch, roll = euler_from_matrix(identity)
        self.assertAlmostEqual(0.0, yaw, places=3)
        self.assertAlmostEqual(0.0, pitch, places=3)
        self.assertAlmostEqual(0.0, roll, places=3)

    def test_pose_bin_classification(self):
        self.assertEqual("CM", pose_bin(0.0, 0.0))
        self.assertEqual("LM", pose_bin(-40.0, 0.0))
        self.assertEqual("RM", pose_bin(40.0, 0.0))
        self.assertEqual("CU", pose_bin(0.0, -30.0))
        self.assertEqual("CD", pose_bin(0.0, 30.0))
        self.assertEqual("LU", pose_bin(-40.0, -30.0))

    def test_state_machine_reaches_complete_after_covering_all_bins(self):
        guide = PoseGuide(min_frames_per_bin=1)
        self.assertFalse(guide.complete)
        states = []
        for yaw in (-30.0, 0.0, 30.0):
            for pitch in (-20.0, 0.0, 20.0):
                states.append(guide.update(yaw, pitch))
        self.assertTrue(guide.complete)
        self.assertEqual(9, states[-1].progress)
        self.assertEqual("Hoàn tất, giữ yên 2 giây", states[-1].guidance)

    def test_guidance_uses_vietnamese_directions(self):
        DIRECTIONS = (
            "Từ từ quay mặt sang TRÁI",
            "Từ từ quay mặt sang PHẢI",
            "Ngẩng nhẹ lên trên",
            "Cúi nhẹ xuống dưới",
        )
        guide = PoseGuide(min_frames_per_bin=1)
        guide.update(0.0, 0.0)  # cover CM
        # Holding the covered center must steer to an uncovered cell.
        state = guide.update(0.0, 0.0)
        self.assertIn(state.guidance, DIRECTIONS)
        # Cover all four corners + center; holding CM still points somewhere.
        for yaw, pitch in ((-60, -40), (-60, 40), (60, -40), (60, 40)):
            guide.update(yaw, pitch)
        state = guide.update(0.0, 0.0)
        self.assertIn(state.guidance, DIRECTIONS)

    def test_requires_min_frames_per_bin(self):
        guide = PoseGuide(min_frames_per_bin=3)
        for _ in range(2):
            guide.update(0.0, 0.0)
        self.assertNotIn("CM", guide._coverage)
        guide.update(0.0, 0.0)
        self.assertIn("CM", guide._coverage)

    def test_easy_mode_completes_with_three_angles(self):
        from pose_guide import EASY_TARGET_BINS

        guide = PoseGuide(min_frames_per_bin=2, target_bins=EASY_TARGET_BINS)
        self.assertEqual(3, len(guide.target_bins))
        for _ in range(2):
            guide.update(0.0, 0.0)      # CM
        for _ in range(2):
            guide.update(-15.0, 0.0)    # LM
        for _ in range(2):
            guide.update(15.0, 0.0)     # RM
        self.assertTrue(guide.complete)
        self.assertEqual(3, guide.update(0.0, 0.0).progress)

    def test_just_covered_reports_only_new_bins(self):
        guide = PoseGuide(min_frames_per_bin=2)
        first = guide.update(0.0, 0.0)
        self.assertIsNone(first.just_covered)
        second = guide.update(0.0, 0.0)
        self.assertEqual("CM", second.just_covered)
        third = guide.update(0.0, 0.0)
        self.assertIsNone(third.just_covered)
        fourth = guide.update(30.0, 0.0)
        self.assertIsNone(fourth.just_covered)
        fifth = guide.update(30.0, 0.0)
        self.assertEqual("RM", fifth.just_covered)

    def test_steers_to_uncovered_bin_when_current_is_done(self):
        guide = PoseGuide(min_frames_per_bin=1)
        guide.update(0.0, 0.0)      # cover CM
        held = guide.update(0.0, 0.0)
        # Holding the covered CM must point at a not-yet-covered cell.
        self.assertIn(held.guidance, (
            "Từ từ quay mặt sang TRÁI",
            "Từ từ quay mặt sang PHẢI",
            "Ngẩng nhẹ lên trên",
            "Cúi nhẹ xuống dưới",
        ))
        # Cover the whole left column; holding CM now must steer to a new cell.
        guide.update(-40.0, -20.0)  # LU
        guide.update(-40.0, 0.0)    # LM
        guide.update(-40.0, 20.0)   # LD
        held = guide.update(0.0, 0.0)
        self.assertNotIn("Giữ yên", held.guidance)


class FaceQualityGateTests(unittest.TestCase):
    def setUp(self):
        self.gate = FaceQualityGate(min_face_width=20, min_eye_aspect_ratio=0.0)

    def test_good_face_passes(self):
        rng = np.random.default_rng(42)
        frame = rng.integers(60, 200, size=(100, 100, 3), dtype=np.uint8)
        result = self.gate.evaluate(frame, (0, 100, 100, 0))
        self.assertTrue(result.passed)
        self.assertEqual((), result.reasons)

    def test_dark_frame_is_rejected(self):
        frame = np.zeros((100, 100, 3), dtype=np.uint8)
        result = self.gate.evaluate(frame, (0, 100, 100, 0))
        self.assertFalse(result.passed)
        self.assertIn("brightness", result.reasons)

    def test_tiny_face_is_rejected(self):
        rng = np.random.default_rng(7)
        frame = rng.integers(60, 200, size=(100, 100, 3), dtype=np.uint8)
        result = self.gate.evaluate(frame, (0, 10, 10, 0))
        self.assertFalse(result.passed)
        self.assertIn("face-too-small", result.reasons)

    def test_blurred_face_is_rejected(self):
        frame = np.full((100, 100, 3), 120, dtype=np.uint8)
        gate = FaceQualityGate(
            min_face_width=5, min_eye_aspect_ratio=0.0, min_sharpness=100.0
        )
        result = gate.evaluate(frame, (0, 100, 100, 0))
        self.assertFalse(result.passed)
        self.assertIn("blur", result.reasons)


if __name__ == "__main__":
    unittest.main()
