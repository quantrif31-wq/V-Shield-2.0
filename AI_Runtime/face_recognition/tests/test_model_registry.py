"""Unit and concurrency tests for FaceModelRegistry."""

import pathlib
import pickle
import tempfile
import threading
import time
import unittest
from dataclasses import FrozenInstanceError

import numpy as np

from model_registry import FaceModelRegistry


def encoding(value):
    return np.full(128, value, dtype=np.float64)


def write_model(directory, name, encodings):
    path = pathlib.Path(directory) / name
    with path.open("wb") as stream:
        pickle.dump(encodings, stream)
    return path


class FaceModelRegistryTests(unittest.TestCase):
    def test_missing_directory_starts_with_empty_snapshot_and_warning(self):
        with tempfile.TemporaryDirectory() as parent:
            registry = FaceModelRegistry(pathlib.Path(parent) / "missing")

        snapshot = registry.current_snapshot()
        self.assertEqual(1, snapshot.version)
        self.assertEqual(0, snapshot.successful_file_count)
        self.assertEqual(0, snapshot.encoding_count)
        self.assertEqual("MODEL_DIRECTORY_NOT_FOUND", snapshot.errors[0].error_code)

    def test_empty_directory_starts_with_valid_empty_snapshot(self):
        with tempfile.TemporaryDirectory() as directory:
            snapshot = FaceModelRegistry(directory).current_snapshot()

        self.assertEqual((), snapshot.model_files)
        self.assertEqual((), snapshot.subject_ids)
        self.assertEqual((), snapshot.encodings)
        self.assertEqual((), snapshot.errors)

    def test_single_file_preserves_all_encodings_and_old_subject_convention(self):
        with tempfile.TemporaryDirectory() as directory:
            write_model(directory, "emp_17_timestamp.pkl", [encoding(1), encoding(2)])
            snapshot = FaceModelRegistry(directory).current_snapshot()

        self.assertEqual(1, snapshot.successful_file_count)
        self.assertEqual(2, snapshot.encoding_count)
        self.assertEqual(("17", "17"), snapshot.subject_ids)
        self.assertEqual("emp_17_timestamp.pkl", snapshot.model_files[0].file_name)
        self.assertEqual(2, snapshot.model_files[0].encoding_count)

    def test_multiple_files_are_loaded_in_deterministic_filename_order(self):
        with tempfile.TemporaryDirectory() as directory:
            write_model(directory, "emp_2_z.pkl", [encoding(2)])
            write_model(directory, "emp_1_a.pkl", [encoding(1), encoding(1)])
            snapshot = FaceModelRegistry(directory).current_snapshot()

        self.assertEqual(
            ("emp_1_a.pkl", "emp_2_z.pkl"),
            tuple(model.file_name for model in snapshot.model_files),
        )
        self.assertEqual(("1", "1", "2"), snapshot.subject_ids)
        self.assertEqual(3, snapshot.encoding_count)

    def test_filename_without_old_convention_uses_full_stem(self):
        with tempfile.TemporaryDirectory() as directory:
            write_model(directory, "alice.pkl", [encoding(1)])
            snapshot = FaceModelRegistry(directory).current_snapshot()

        self.assertEqual(("alice",), snapshot.subject_ids)

    def test_non_pkl_and_nested_files_are_ignored(self):
        with tempfile.TemporaryDirectory() as directory:
            pathlib.Path(directory, "ignored.txt").write_text("ignored")
            nested = pathlib.Path(directory, "nested")
            nested.mkdir()
            write_model(nested, "emp_9_nested.pkl", [encoding(9)])

            snapshot = FaceModelRegistry(directory).current_snapshot()

        self.assertEqual(0, snapshot.successful_file_count)
        self.assertEqual((), snapshot.errors)

    def test_corrupt_and_invalid_structure_are_sanitized_per_file(self):
        with tempfile.TemporaryDirectory() as directory:
            pathlib.Path(directory, "broken.pkl").write_bytes(b"not pickle")
            write_model(directory, "wrong.pkl", {"encoding": [1, 2, 3]})

            snapshot = FaceModelRegistry(directory).current_snapshot()

        self.assertEqual(
            ("MODEL_DESERIALIZATION_FAILED", "MODEL_STRUCTURE_INVALID"),
            tuple(error.error_code for error in snapshot.errors),
        )
        self.assertNotIn("pickle", snapshot.errors[0].message.lower())

    def test_startup_keeps_valid_models_when_another_file_is_broken(self):
        with tempfile.TemporaryDirectory() as directory:
            write_model(directory, "emp_7_ok.pkl", [encoding(7)])
            pathlib.Path(directory, "broken.pkl").write_bytes(b"not pickle")

            snapshot = FaceModelRegistry(directory).current_snapshot()

        self.assertEqual(1, snapshot.successful_file_count)
        self.assertEqual(1, snapshot.encoding_count)
        self.assertEqual(("7",), snapshot.subject_ids)
        self.assertEqual(1, len(snapshot.errors))

    def test_invalid_encoding_shape_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            write_model(directory, "emp_1_bad.pkl", [np.zeros(127)])
            snapshot = FaceModelRegistry(directory).current_snapshot()

        self.assertEqual(0, snapshot.encoding_count)
        self.assertEqual("MODEL_ENCODING_INVALID", snapshot.errors[0].error_code)

    def test_snapshot_and_encodings_cannot_be_mutated_by_caller(self):
        with tempfile.TemporaryDirectory() as directory:
            write_model(directory, "emp_1_ok.pkl", [encoding(1)])
            snapshot = FaceModelRegistry(directory).current_snapshot()

        with self.assertRaises(FrozenInstanceError):
            snapshot.version = 99
        with self.assertRaises(TypeError):
            snapshot.subject_ids[0] = "changed"
        with self.assertRaises(ValueError):
            snapshot.encodings[0][0] = 99
        with self.assertRaises(ValueError):
            snapshot.encodings[0].setflags(write=True)
        self.assertNotIn("array", repr(snapshot).lower())

    def test_reload_success_increments_version(self):
        with tempfile.TemporaryDirectory() as directory:
            registry = FaceModelRegistry(directory)
            write_model(directory, "emp_3_ok.pkl", [encoding(3)])

            result = registry.reload()

        self.assertTrue(result.success)
        self.assertEqual(1, result.previous_version)
        self.assertEqual(2, result.current_snapshot.version)
        self.assertEqual(("3",), result.current_snapshot.subject_ids)

    def test_strict_reload_failure_keeps_previous_snapshot(self):
        with tempfile.TemporaryDirectory() as directory:
            write_model(directory, "emp_1_ok.pkl", [encoding(1)])
            registry = FaceModelRegistry(directory)
            previous = registry.current_snapshot()
            pathlib.Path(directory, "broken.pkl").write_bytes(b"bad")

            result = registry.reload()

        self.assertFalse(result.success)
        self.assertEqual("MODEL_RELOAD_REJECTED", result.error_code)
        self.assertIs(previous, registry.current_snapshot())
        self.assertEqual(1, registry.current_snapshot().version)
        self.assertEqual(previous.subject_ids, registry.current_snapshot().subject_ids)
        self.assertEqual(previous.encoding_count, registry.current_snapshot().encoding_count)

    def test_symlink_outside_model_directory_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory, tempfile.TemporaryDirectory() as outside:
            target = write_model(outside, "outside.pkl", [encoding(1)])
            link = pathlib.Path(directory, "linked.pkl")
            try:
                link.symlink_to(target)
            except OSError:
                self.skipTest("Symbolic links are not available in this environment")

            snapshot = FaceModelRegistry(directory).current_snapshot()

        self.assertEqual(0, snapshot.encoding_count)
        self.assertEqual("UNSAFE_MODEL_PATH", snapshot.errors[0].error_code)


class FaceModelRegistryConcurrencyTests(unittest.TestCase):
    def test_readers_observe_consistent_snapshot_while_reload_swaps(self):
        with tempfile.TemporaryDirectory() as directory:
            write_model(directory, "emp_1_old.pkl", [encoding(1)])
            registry = FaceModelRegistry(directory)
            pathlib.Path(directory, "emp_1_old.pkl").unlink()
            write_model(directory, "emp_2_new.pkl", [encoding(2), encoding(2)])
            stop = threading.Event()
            failures = []

            def reader():
                while not stop.is_set():
                    snapshot = registry.current_snapshot()
                    if len(snapshot.subject_ids) != len(snapshot.encodings):
                        failures.append("length mismatch")
                        return
                    valid = (
                        snapshot.subject_ids == ("1",)
                        or snapshot.subject_ids == ("2", "2")
                    )
                    if not valid:
                        failures.append(snapshot.subject_ids)
                        return

            readers = [threading.Thread(target=reader) for _ in range(8)]
            for thread in readers:
                thread.start()
            result = registry.reload()
            stop.set()
            for thread in readers:
                thread.join(timeout=2)

        self.assertTrue(result.success)
        self.assertEqual([], failures)
        self.assertTrue(all(not thread.is_alive() for thread in readers))

    def test_snapshot_reads_are_not_blocked_during_slow_build(self):
        with tempfile.TemporaryDirectory() as directory:
            registry = FaceModelRegistry(directory)
            original_build = registry.build_snapshot
            build_started = threading.Event()
            release_build = threading.Event()

            def slow_build(*, version):
                build_started.set()
                self.assertTrue(release_build.wait(timeout=2))
                return original_build(version=version)

            registry.build_snapshot = slow_build
            worker = threading.Thread(target=registry.reload)
            worker.start()
            self.assertTrue(build_started.wait(timeout=1))

            started = time.monotonic()
            snapshot = registry.current_snapshot()
            read_duration = time.monotonic() - started
            release_build.set()
            worker.join(timeout=2)

        self.assertEqual(1, snapshot.version)
        self.assertLess(read_duration, 0.1)
        self.assertFalse(worker.is_alive())

    def test_second_concurrent_reload_returns_in_progress(self):
        with tempfile.TemporaryDirectory() as directory:
            registry = FaceModelRegistry(directory)
            original_build = registry.build_snapshot
            build_started = threading.Event()
            release_build = threading.Event()
            first_result = []

            def slow_build(*, version):
                build_started.set()
                self.assertTrue(release_build.wait(timeout=2))
                return original_build(version=version)

            registry.build_snapshot = slow_build
            worker = threading.Thread(target=lambda: first_result.append(registry.reload()))
            worker.start()
            self.assertTrue(build_started.wait(timeout=1))

            second = registry.reload()
            release_build.set()
            worker.join(timeout=2)

        self.assertFalse(second.success)
        self.assertEqual("RELOAD_IN_PROGRESS", second.error_code)
        self.assertTrue(first_result[0].success)
        self.assertFalse(worker.is_alive())


if __name__ == "__main__":
    unittest.main()
