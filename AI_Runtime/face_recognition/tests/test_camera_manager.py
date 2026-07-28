"""Concurrency and isolation tests for multi-camera Face ID sessions."""

from __future__ import annotations

import pathlib
import pickle
import tempfile
import threading
import time
import unittest
from unittest import mock

import numpy as np

from camera_manager import (
    CameraConflictError,
    CameraManager,
    CameraNotFoundError,
    InvalidCameraIdError,
    validate_camera_id,
)
from model_registry import FaceModelRegistry
from runtime_config import FaceRuntimeConfig


class FakeCapture:
    def __init__(self, url: str, *, opened: bool = True):
        self.url = url
        self.opened = opened
        self.released = False
        self.read_count = 0

    def set(self, *_args):
        return True

    def isOpened(self):
        return self.opened and not self.released

    def read(self):
        time.sleep(0.003)
        if self.released:
            return False, None
        self.read_count += 1
        value = 20 if "one" in self.url else 80
        return True, np.full((20, 30, 3), value, dtype=np.uint8)

    def release(self):
        self.released = True


class CameraManagerTests(unittest.TestCase):
    def setUp(self):
        self.models = tempfile.TemporaryDirectory(prefix="camera-manager-models-")
        self.storage = tempfile.TemporaryDirectory(prefix="camera-manager-storage-")
        storage_root = pathlib.Path(self.storage.name)
        input_root = storage_root / "input"
        input_root.mkdir()
        self.config = FaceRuntimeConfig.from_env(
            {
                "FACE_MODEL_DIR": self.models.name,
                "FACE_MAX_CAMERAS": "2",
                "FACE_ENROLLMENT_INPUT_ROOT": str(input_root),
                "FACE_MODEL_STAGING_DIR": str(storage_root / "models" / "staging"),
                "FACE_MODEL_ARCHIVE_DIR": str(storage_root / "models" / "archive"),
                "FACE_MODEL_FAILED_DIR": str(storage_root / "models" / "failed"),
            }
        )
        self.registry = FaceModelRegistry(self.models.name)
        self.captures = []

        def capture_factory(url):
            capture = FakeCapture(url)
            self.captures.append(capture)
            return capture

        self.capture_factory = capture_factory
        self.manager = CameraManager(
            self.registry, self.config, capture_factory=capture_factory
        )
        self.face_locations = mock.patch(
            "camera_session.face_recognition.face_locations", return_value=[]
        )
        self.face_locations.start()

    def tearDown(self):
        self.manager.shutdown_all()
        self.face_locations.stop()
        self.models.cleanup()
        self.storage.cleanup()

    def wait_until(self, predicate, timeout=1.5):
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            if predicate():
                return True
            time.sleep(0.01)
        return predicate()

    def start_two(self):
        one, _ = self.manager.start_session("camera-one", "fake://one", "lane-1")
        two, _ = self.manager.start_session("camera-two", "fake://two", "lane-2")
        self.assertTrue(
            self.wait_until(
                lambda: one.connection_status == two.connection_status == "connected"
            )
        )
        return one, two

    def test_two_sessions_are_created_with_independent_state(self):
        one, two = self.start_two()
        self.assertIsNot(one, two)
        self.assertIsNot(one.stop_event, two.stop_event)
        self.assertIsNot(one.session_lock, two.session_lock)
        self.assertIsNot(one.frame_lock, two.frame_lock)
        self.assertIsNot(one.cooldown_state, two.cooldown_state)
        self.assertIsNot(one.locked_images, two.locked_images)
        self.assertEqual(2, self.manager.list_sessions()["activeCount"])

    def test_two_different_urls_capture_concurrently(self):
        one, two = self.start_two()
        self.assertTrue(self.wait_until(lambda: all(cap.read_count > 1 for cap in self.captures)))
        frame_one, _ = one.latest_frame_copy()
        frame_two, _ = two.latest_frame_copy()
        self.assertEqual(20, int(frame_one[0, 0, 0]))
        self.assertEqual(80, int(frame_two[0, 0, 0]))

    def test_stop_camera_one_does_not_stop_camera_two(self):
        one, two = self.start_two()
        self.manager.stop_session("camera-one")
        self.assertFalse(one.enabled)
        self.assertTrue(two.enabled)
        self.assertEqual("connected", two.connection_status)

    def test_reset_camera_one_does_not_reset_camera_two(self):
        one, two = self.start_two()
        one.update_recognition_state(employee_id="one", confirm_count=4)
        two.update_recognition_state(employee_id="two", confirm_count=3)
        old_two_generation = two.generation
        self.manager.reset_session("camera-one")
        self.assertIsNone(one.result()["employee_id"])
        self.assertEqual("two", two.result()["employee_id"])
        self.assertEqual(3, two.result()["confirm_count"])
        self.assertEqual(old_two_generation, two.generation)

    def test_results_locked_images_confirm_and_cooldown_are_independent(self):
        one, two = self.start_two()
        one.update_recognition_state(
            employee_id="employee-one",
            confirm_count=5,
            locked_snapshot="one-snapshot",
            locked_face_crop="one-face",
            scan_locked=True,
        )
        one.cooldown_state["alert_triggered"] = True
        one.cooldown_state["distance_buffer"].append(0.2)
        self.assertEqual("employee-one", one.result()["employee_id"])
        self.assertIsNone(two.result()["employee_id"])
        self.assertEqual("one-snapshot", one.locked_image_result()["locked_snapshot"])
        self.assertIsNone(two.locked_image_result()["locked_snapshot"])
        self.assertEqual(5, one.result()["confirm_count"])
        self.assertEqual(0, two.result()["confirm_count"])
        self.assertTrue(one.cooldown_state["alert_triggered"])
        self.assertFalse(two.cooldown_state["alert_triggered"])
        self.assertEqual(0, len(two.cooldown_state["distance_buffer"]))

    def test_connection_error_is_isolated(self):
        captures = {}

        def factory(url):
            capture = FakeCapture(url, opened="broken" not in url)
            captures[url] = capture
            return capture

        manager = CameraManager(self.registry, self.config, capture_factory=factory)
        self.addCleanup(manager.shutdown_all)
        broken, _ = manager.start_session("broken", "fake://broken")
        healthy, _ = manager.start_session("healthy", "fake://one")
        self.assertTrue(
            self.wait_until(
                lambda: broken.connection_status == "error"
                and healthy.connection_status == "connected"
            )
        )
        self.assertTrue(healthy.enabled)
        self.assertEqual("Cannot open camera stream", broken.last_error)

    def test_same_id_and_url_is_idempotent(self):
        session, first_idempotent = self.manager.start_session("same", "fake://one")
        generation = session.generation
        same, second_idempotent = self.manager.start_session("same", "fake://one")
        self.assertFalse(first_idempotent)
        self.assertTrue(second_idempotent)
        self.assertIs(session, same)
        self.assertEqual(generation, same.generation)

    def test_same_active_id_with_different_url_conflicts(self):
        self.manager.start_session("same", "fake://one")
        with self.assertRaises(CameraConflictError):
            self.manager.start_session("same", "fake://two")

    def test_max_camera_limit_conflicts(self):
        self.start_two()
        with self.assertRaises(CameraConflictError):
            self.manager.start_session("camera-three", "fake://three")

    def test_stopped_session_can_restart_with_new_url(self):
        session, _ = self.manager.start_session("restart", "fake://one")
        first_generation = session.generation
        self.manager.stop_session("restart")
        restarted, idempotent = self.manager.start_session("restart", "fake://two")
        self.assertIs(session, restarted)
        self.assertFalse(idempotent)
        self.assertGreater(restarted.generation, first_generation)
        self.assertEqual("fake://two", restarted.stream_url)

    def test_invalid_camera_ids_are_rejected(self):
        invalid = ("", " ", "../camera", "a..b", "a/b", "a\\b", "camera id", "x" * 65)
        for camera_id in invalid:
            with self.subTest(camera_id=camera_id):
                with self.assertRaises(InvalidCameraIdError):
                    validate_camera_id(camera_id)
        self.assertEqual("gate-01.face_A", validate_camera_id("gate-01.face_A"))

    def test_unknown_camera_operations_return_not_found(self):
        for operation in (
            self.manager.get_status,
            self.manager.get_result,
            self.manager.get_locked_images,
            self.manager.stop_session,
            self.manager.reset_session,
        ):
            with self.subTest(operation=operation.__name__):
                with self.assertRaises(CameraNotFoundError):
                    operation("missing")

    def test_concurrent_start_and_stop_finishes_without_deadlock(self):
        failures = []

        def churn():
            try:
                for _ in range(10):
                    self.manager.start_session("churn", "fake://one")
                    self.manager.stop_session("churn")
            except Exception as error:
                failures.append(error)

        workers = [threading.Thread(target=churn) for _ in range(3)]
        for worker in workers:
            worker.start()
        for worker in workers:
            worker.join(timeout=5)
        self.assertTrue(all(not worker.is_alive() for worker in workers))
        self.assertEqual([], failures)

    def test_old_generation_cannot_publish_after_reset(self):
        session, _ = self.manager.start_session("generation", "fake://one")
        old_generation = session.generation
        self.manager.reset_session("generation")
        session._mark_face_seen(
            old_generation, time.time(), (1, 5, 5, 1), "old-frame", "old-face"
        )
        self.assertIsNone(session.result()["last_snapshot"])
        self.assertNotEqual(old_generation, session.generation)

    def test_manager_reads_are_not_blocked_by_slow_camera_open(self):
        opening = threading.Event()
        release = threading.Event()

        def slow_factory(url):
            opening.set()
            self.assertTrue(release.wait(timeout=2))
            return FakeCapture(url)

        manager = CameraManager(self.registry, self.config, capture_factory=slow_factory)
        self.addCleanup(manager.shutdown_all)
        manager.start_session("slow", "fake://slow")
        self.assertTrue(opening.wait(timeout=1))
        started = time.monotonic()
        payload = manager.list_sessions()
        elapsed = time.monotonic() - started
        release.set()
        self.assertEqual(1, payload["activeCount"])
        self.assertLess(elapsed, 0.1)

    def test_shutdown_releases_all_captures_and_workers(self):
        one, two = self.start_two()
        workers = (
            one.capture_worker, one.recognition_worker,
            two.capture_worker, two.recognition_worker,
        )
        self.manager.shutdown_all()
        self.assertTrue(all(capture.released for capture in self.captures))
        self.assertTrue(all(not worker.is_alive() for worker in workers if worker))

    def test_model_reload_does_not_restart_active_sessions(self):
        one, two = self.start_two()
        generations = (one.generation, two.generation)
        model_path = pathlib.Path(self.models.name, "emp_7_test.pkl")
        with model_path.open("wb") as stream:
            pickle.dump([np.zeros(128)], stream)
        result = self.registry.reload()
        self.assertTrue(result.success)
        self.assertEqual(generations, (one.generation, two.generation))
        self.assertTrue(one.enabled and two.enabled)

    def test_failed_model_reload_keeps_snapshot_for_both_sessions(self):
        one, two = self.start_two()
        previous = self.registry.current_snapshot()
        pathlib.Path(self.models.name, "broken.pkl").write_bytes(b"not pickle")
        result = self.registry.reload()
        self.assertFalse(result.success)
        self.assertIs(previous, self.registry.current_snapshot())
        self.assertEqual(
            previous.encoding_count,
            one.result()["total_encodings"],
        )
        self.assertEqual(
            previous.encoding_count,
            two.result()["total_encodings"],
        )

    def test_event_buffers_and_sequences_are_isolated_per_camera(self):
        one, two = self.start_two()
        one._emit_event_locked("Recognized", "1", 0.21, None, None)
        one._emit_event_locked("Unknown", None, 0.7, None, None)
        two._emit_event_locked("Recognized", "2", 0.25, None, None)

        one_events = one.events()
        two_events = two.events()
        self.assertEqual([1, 2], [item["sequence"] for item in one_events["events"]])
        self.assertEqual([1], [item["sequence"] for item in two_events["events"]])
        self.assertEqual("lane-1", one_events["events"][0]["laneId"])
        self.assertEqual("lane-2", two_events["events"][0]["laneId"])
        self.assertEqual(
            3,
            len({item["eventId"] for item in
                 one_events["events"] + two_events["events"]}),
        )

    def test_event_query_is_incremental_bounded_and_reports_gap(self):
        object.__setattr__(self.config, "event_buffer_size", 2)
        session, _ = self.manager.start_session("events", "fake://one", "lane-e")
        for subject in ("1", "2", "3", "4"):
            session._emit_event_locked("Recognized", subject, 0.2, None, None)

        result = session.events(after_sequence=1, limit=1)
        self.assertTrue(result["gapDetected"])
        self.assertTrue(result["hasMore"])
        self.assertEqual(3, result["oldestSequence"])
        self.assertEqual([3], [item["sequence"] for item in result["events"]])
        with self.assertRaises(ValueError):
            session.events(limit=201)

    def test_generation_reset_is_reported_without_removing_other_camera_events(self):
        one, two = self.start_two()
        one._emit_event_locked("Recognized", "1", 0.2, None, None)
        two._emit_event_locked("Recognized", "2", 0.2, None, None)
        previous_generation = one.generation
        self.manager.reset_session("camera-one")

        result = one.events(session_generation=previous_generation)
        self.assertTrue(result["gapDetected"])
        self.assertNotEqual(previous_generation, result["sessionGeneration"])
        self.assertEqual(1, len(two.events()["events"]))


if __name__ == "__main__":
    unittest.main()
