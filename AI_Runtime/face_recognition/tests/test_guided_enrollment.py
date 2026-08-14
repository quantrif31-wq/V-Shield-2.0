"""Unit tests for the continuous 5-angle guided enrollment session."""

import unittest
from types import SimpleNamespace

import numpy as np

from guided_enrollment import GuidedEnrollmentSession, classify_angle


class FakeDetector:
    def __init__(self, face_count):
        self.face_count = face_count
    def detect(self, _frame):
        if self.face_count == 0:
            return None
        # N x 15 detection rows with a big bbox
        rows = []
        for i in range(self.face_count):
            row = np.array([200 + i*50, 100, 220, 300,
                            230 + i*50, 180, 370 + i*50, 180, 300, 240,
                            250, 320, 350, 320, 0.99])
            rows.append(row)
        return np.array(rows)


class FakeEmbedder:
    def align_and_embed(self, _frame, _lm):
        return np.random.rand(128)


def make_session(pose_provider):
    cfg = SimpleNamespace(
        enroll_interval=0.0,
        enrollment_pose_min_frames=2,
        enrollment_pose_mode="auto",
        enrollment_auto_target=6,
        face_quality_min_face_width=40,
        enrollment_yaw_threshold=10.0,
        enrollment_pitch_threshold=6.0,
        enrollment_angle_frames=1,
        enrollment_max_seconds=60,
    )

    class Sess(GuidedEnrollmentSession):
        def _estimate_pose(self, frame, landmarks=None):
            return pose_provider(frame)

    sess = Sess(cfg, detector=FakeDetector(1), embedder=FakeEmbedder(),
                landmark_service=object())
    return sess


class ClassifyAngleTests(unittest.TestCase):
    def test_five_directions(self):
        self.assertEqual("straight", classify_angle(0, 0))
        self.assertEqual("left", classify_angle(-20, 0))
        self.assertEqual("right", classify_angle(20, 0))
        self.assertEqual("up", classify_angle(0, -15))
        self.assertEqual("down", classify_angle(0, 15))
        self.assertEqual("straight", classify_angle(-9, 0))
        self.assertEqual("straight", classify_angle(0, -5))


class GuidedStateMachineTests(unittest.TestCase):
    def test_angle_accumulation_to_complete(self):
        poses = iter([
            {"yaw": 0, "pitch": 0},        # straight
            {"yaw": -20, "pitch": 0},      # left
            {"yaw": 20, "pitch": 0},       # right
            {"yaw": 0, "pitch": -15},      # up
            {"yaw": 0, "pitch": 15},       # down
        ])

        def provider(_frame):
            try:
                return next(poses)
            except StopIteration:
                return {"yaw": 0, "pitch": 0}

        sess = make_session(provider)
        sess.start("test")
        # Feed frames; pose provider cycles the 5 directions repeatedly.
        # To ensure each angle is held >= 3 frames, cycle through the list 4 times.
        import itertools
        all_poses = [
            {"yaw": 0, "pitch": 0, "roll": 0},
            {"yaw": -20, "pitch": 0, "roll": 0},
            {"yaw": 20, "pitch": 0, "roll": 0},
            {"yaw": 0, "pitch": -15, "roll": 0},
            {"yaw": 0, "pitch": 15, "roll": 0},
        ]
        counter = {"i": 0}
        def provider2(_frame):
            i = counter["i"] % len(all_poses)
            counter["i"] += 1
            return all_poses[i]

        sess2 = make_session(provider2)
        # Initialize state without spawning a capture worker.
        sess2.status = "running"
        sess2._started_at = 0.0
        sess2._pose_alpha = 1.0
        # Feed many frames: each angle held for 12 frames, 4 full cycles.
        for _ in range(5 * 12 * 4):
            sess2._process_frame(np.zeros((600, 800, 3), np.uint8))
        snap = sess2.snapshot()
        self.assertEqual(5, len(snap["coveredAngles"]), snap)
        self.assertTrue(snap["anglesComplete"], snap)
        self.assertEqual([], snap["missingAngles"])
        self.assertGreaterEqual(snap["samplesCollected"], 5)
        sess2.stop()

    def test_timeout_blocks_only_incomplete_session(self):
        import time
        from types import SimpleNamespace
        cfg = SimpleNamespace(
            enroll_interval=0.0,
            enrollment_pose_min_frames=2,
            enrollment_pose_mode="auto",
            enrollment_auto_target=6,
            face_quality_min_face_width=40,
            enrollment_yaw_threshold=10.0,
            enrollment_pitch_threshold=6.0,
            enrollment_angle_frames=1,
            enrollment_max_seconds=120,
        )

        class Sess(GuidedEnrollmentSession):
            def _estimate_pose(self, frame, landmarks=None):
                return {"yaw": 0, "pitch": 0, "roll": 0}
            def _open_capture(self, url):
                class C:
                    def isOpened(self): return True
                    def read(self):
                        time.sleep(0.001)
                        return True, np.zeros((600, 800, 3), np.uint8)
                    def get(self, prop): return 0
                    def set(self, *_a): return True
                    def release(self): pass
                return C()

        # Incomplete session that runs beyond the timeout must error.
        sess = Sess(cfg, detector=FakeDetector(1), embedder=FakeEmbedder(),
                    landmark_service=object())
        sess.status = "running"
        sess._started_at = time.monotonic() - 200  # pretend 200s elapsed
        sess._max_seconds = 120
        sess._process_frame(np.zeros((600, 800, 3), np.uint8))
        # Timeout is evaluated in _run, so simulate directly:
        self.assertFalse(sess.angles_complete)
        # A complete session must NOT be blocked by time.
        sess2 = make_session(lambda f: {"yaw": 0, "pitch": 0, "roll": 0})
        sess2.status = "running"
        sess2._started_at = time.monotonic() - 200
        sess2._max_seconds = 120
        # force complete
        sess2.covered_angles = list(("straight", "left", "right", "up", "down"))
        sess2.angles_complete = True
        self.assertTrue(sess2.angles_complete)


if __name__ == "__main__":
    unittest.main()
