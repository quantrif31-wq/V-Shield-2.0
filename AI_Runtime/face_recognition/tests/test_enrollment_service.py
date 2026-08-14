import pickle
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest import mock

import numpy as np

from enrollment_service import EnrollmentError, EnrollmentService
from model_registry import FaceModelRegistry


class FakeCapture:
    def __init__(self, frames, opened=True):
        self.frames = list(frames)
        self.opened = opened
        self.released = False
    def isOpened(self): return self.opened
    def read(self):
        return (True, self.frames.pop(0)) if self.frames else (False, None)
    def get(self, _): return len(self.frames)
    def release(self): self.released = True


class FakeDetector:
    def __init__(self, detections):
        self.detections = detections
    def detect(self, _frame):
        return self.detections


class FakeEmbedder:
    def __init__(self, vectors):
        self.vectors = vectors
    def align_and_embed(self, _frame, _landmarks):
        return self.vectors[0] if self.vectors else None


class PassAllQuality:
    def evaluate(self, *_args, **_kwargs):
        return SimpleNamespace(
            passed=True, reasons=(), sharpness=1.0, brightness=1.0,
            face_width=100, eye_aspect_ratio=1.0,
        )


class EnrollmentServiceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        root = Path(self.temp.name)
        self.input = root / "input"
        self.active = root / "models" / "active"
        self.staging = root / "models" / "staging"
        self.archive = root / "models" / "archive"
        self.failed = root / "models" / "failed"
        for path in (self.input / "video_notok", self.active, self.staging,
                     self.archive, self.failed):
            path.mkdir(parents=True)
        self.video = self.input / "video_notok" / "managed.mp4"
        self.video.write_bytes(b"video")
        self.config = SimpleNamespace(
            enrollment_input_root=self.input, model_dir=self.active,
            model_staging_dir=self.staging, model_archive_dir=self.archive,
            model_failed_dir=self.failed, enrollment_max_video_bytes=1024,
            enrollment_max_frames=5, enrollment_frame_interval=1,
            enrollment_min_encodings=2, enrollment_duplicate_threshold=.2)
        self.registry = FaceModelRegistry(self.active)
        self.service = EnrollmentService(self.config, self.registry)
        self.job = "12345678-1234-1234-1234-123456789abc"

    def tearDown(self): self.temp.cleanup()

    def _service_with(self, detector=None, embedder=None):
        service = EnrollmentService(self.config, self.registry,
                                    detector=detector, embedder=embedder)
        service.quality_gate = PassAllQuality()
        return service

    def test_rejects_absolute_traversal_extension_and_missing(self):
        for value in (str(self.video), "../managed.mp4", "video_notok/model.pkl",
                      "https://example/video.mp4", "video_notok/missing.mp4"):
            with self.subTest(value=value), self.assertRaises(EnrollmentError):
                self.service._source_path(value)

    def test_rejects_symlink_escape(self):
        outside = Path(self.temp.name) / "outside.mp4"
        outside.write_bytes(b"x")
        link = self.input / "video_notok" / "link.mp4"
        try: link.symlink_to(outside)
        except OSError: self.skipTest("symlink unavailable")
        with self.assertRaises(EnrollmentError):
            self.service._source_path("video_notok/link.mp4")

    def test_rejects_large_file(self):
        self.config.enrollment_max_video_bytes = 1
        with self.assertRaises(EnrollmentError):
            self.service._source_path("video_notok/managed.mp4")

    def test_unreadable_video_releases_capture(self):
        capture = FakeCapture([], False)
        with mock.patch("enrollment_service.cv2.VideoCapture", return_value=capture):
            with self.assertRaisesRegex(EnrollmentError, "opened"):
                self.service.prepare_enrollment(self.job, "1", "video_notok/managed.mp4")
        self.assertTrue(capture.released)

    def test_prepare_quality_checksum_idempotency_and_no_temp(self):
        capture = FakeCapture([np.zeros((2, 2, 3), np.uint8) for _ in range(2)])
        encoding = np.ones(128)
        detector = FakeDetector(np.ones((1, 15)))
        embedder = FakeEmbedder([encoding])
        service = self._service_with(detector, embedder)
        with mock.patch("enrollment_service.cv2.VideoCapture", return_value=capture):
            result = service.prepare_enrollment(
                self.job, "1", "video_notok/managed.mp4")
        self.assertEqual(2, result["encodingCount"])
        self.assertEqual(1.0, result["qualityScore"])
        self.assertEqual(64, len(result["candidateChecksum"]))
        self.assertFalse(any(self.staging.glob("*.tmp")))
        again = service.prepare_enrollment(
            self.job, "1", "video_notok/managed.mp4")
        self.assertEqual(result["candidateChecksum"], again["candidateChecksum"])
        self.assertEqual(0, self.registry.current_snapshot().encoding_count)

    def test_no_face_multiple_invalid_and_insufficient(self):
        cases = [
            (None, [], "InsufficientUsableFrames"),
            (None, [np.ones(128)], "InsufficientUsableFrames"),
        ]
        for detector_value, vectors, expected_code in cases:
            capture = FakeCapture([np.zeros((2, 2, 3), np.uint8)])
            detector = FakeDetector(detector_value)
            embedder = FakeEmbedder(vectors)
            service = self._service_with(detector, embedder)
            with mock.patch("enrollment_service.cv2.VideoCapture", return_value=capture):
                with self.assertRaises(EnrollmentError) as error:
                    service.prepare_enrollment(
                        self.job, "1", "video_notok/managed.mp4")
                self.assertEqual(expected_code, error.exception.code)
                self.assertTrue(capture.released)

    def test_duplicate_other_subject_rejected_same_subject_allowed(self):
        known = np.zeros(128)
        with (self.active / "emp_2_v1_aaaaaaaa.pkl").open("wb") as stream:
            pickle.dump([known], stream)
        registry = FaceModelRegistry(self.active)
        service = EnrollmentService(self.config, registry)
        self.assertEqual(("2", 0.0), service._duplicate([known], "1"))
        self.assertEqual((None, None), service._duplicate([known], "2"))

    def _candidate(self, subject="1"):
        from template_store import save_template

        candidate = self.staging / f"{self.job}.json"
        save_template(
            candidate,
            employee_id=1,
            version=1,
            templates=[np.ones(128), np.ones(128)],
        )
        checksum = self.service._sha256(candidate)
        return candidate, checksum, f"emp_{subject}_v2_12345678.json"

    def test_activation_archives_old_and_discard_is_idempotent(self):
        old = self.active / "emp_1_v1_aaaaaaaa.pkl"
        with old.open("wb") as stream: pickle.dump([np.zeros(128)], stream)
        self.registry = FaceModelRegistry(self.active)
        self.service = EnrollmentService(self.config, self.registry)
        candidate, checksum, filename = self._candidate()
        result = self.service.activate_candidate(self.job, "1", 2, checksum, filename)
        self.assertEqual(filename, result["modelFileName"])
        self.assertTrue((self.active / filename).exists())
        self.assertFalse(old.exists())
        self.assertTrue(any(self.archive.iterdir()))
        self.service.discard_candidate(self.job)
        self.service.discard_candidate(self.job)
        self.assertFalse(any(self.staging.glob("*.tmp")))

    def test_activation_checksum_mismatch_and_missing(self):
        _, _, filename = self._candidate()
        with self.assertRaises(EnrollmentError):
            self.service.activate_candidate(self.job, "1", 2, "0" * 64, filename)
        self.service.discard_candidate(self.job)
        with self.assertRaises(EnrollmentError):
            self.service.activate_candidate(self.job, "1", 2, "0" * 64, filename)

    def test_reload_failure_rolls_back(self):
        old = self.active / "emp_1_v1_aaaaaaaa.pkl"
        with old.open("wb") as stream: pickle.dump([np.zeros(128)], stream)
        self.registry = FaceModelRegistry(self.active)
        self.service = EnrollmentService(self.config, self.registry)
        candidate, checksum, filename = self._candidate()
        with mock.patch.object(self.registry, "reload",
                side_effect=[SimpleNamespace(success=False), self.registry.reload()]):
            with self.assertRaises(EnrollmentError):
                self.service.activate_candidate(self.job, "1", 2, checksum, filename)
        self.assertTrue(old.exists())
        self.assertTrue(candidate.exists())
        self.assertFalse((self.active / filename).exists())


if __name__ == "__main__":
    unittest.main()
