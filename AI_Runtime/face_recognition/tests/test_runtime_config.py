"""Tests for centralized Face Runtime environment configuration."""

import os
import pathlib
import tempfile
import unittest
from unittest import mock

from runtime_config import (
    DEFAULT_ALERT_TIMEOUT,
    DEFAULT_CONFIRM_FRAMES,
    DEFAULT_ENCODE_INTERVAL,
    DEFAULT_FRAME_WIDTH,
    DEFAULT_HEADLESS_MODE,
    DEFAULT_JPEG_QUALITY,
    DEFAULT_MAX_CAMERAS,
    DEFAULT_LOST_TIMEOUT,
    DEFAULT_RECOGNIZE_TIMEOUT,
    DEFAULT_ROTATION,
    DEFAULT_STREAM_HEIGHT,
    DEFAULT_STREAM_WIDTH,
    DEFAULT_THRESHOLD,
    FaceRuntimeConfig,
    FaceRuntimeConfigError,
)


class FaceRuntimeConfigTests(unittest.TestCase):
    def test_defaults_match_pre_refactor_runtime_values(self):
        config = FaceRuntimeConfig.from_env({})

        self.assertEqual(DEFAULT_THRESHOLD, config.threshold)
        self.assertEqual(DEFAULT_CONFIRM_FRAMES, config.confirm_frames)
        self.assertEqual(DEFAULT_LOST_TIMEOUT, config.lost_timeout)
        self.assertEqual(DEFAULT_ENCODE_INTERVAL, config.encode_interval)
        self.assertEqual(DEFAULT_FRAME_WIDTH, config.frame_width)
        self.assertEqual(DEFAULT_ROTATION, config.rotation)
        self.assertEqual(DEFAULT_RECOGNIZE_TIMEOUT, config.recognize_timeout)
        self.assertEqual(DEFAULT_ALERT_TIMEOUT, config.alert_timeout)
        self.assertEqual(DEFAULT_STREAM_WIDTH, config.stream_width)
        self.assertEqual(DEFAULT_STREAM_HEIGHT, config.stream_height)
        self.assertEqual(DEFAULT_JPEG_QUALITY, config.jpeg_quality)
        self.assertEqual(DEFAULT_MAX_CAMERAS, config.max_cameras)
        self.assertEqual(5001, config.api_port)
        self.assertEqual(DEFAULT_HEADLESS_MODE, config.headless_mode)
        self.assertIsNone(config.snapshot_dir)
        self.assertIsNone(config.service_token)
        self.assertEqual(
            pathlib.Path(__file__).resolve().parents[3] / "runtime" / "face-data" / "input",
            config.enrollment_input_root,
        )
        self.assertEqual(
            pathlib.Path(__file__).resolve().parents[3] / "runtime" / "face-data" / "models" / "active",
            config.canonical_model_active_dir,
        )

    def test_numeric_environment_overrides_are_parsed(self):
        config = FaceRuntimeConfig.from_env(
            {
                "FACE_THRESHOLD": "0.42",
                "FACE_CONFIRM_FRAMES": "7",
                "FACE_LOST_TIMEOUT": "3.5",
                "FACE_ENCODE_INTERVAL": "1.25",
                "FACE_FRAME_WIDTH": "720",
                "FACE_ROTATION": "180",
                "FACE_RECOGNIZE_TIMEOUT": "9",
                "FACE_ALERT_TIMEOUT": "12.5",
                "FACE_STREAM_WIDTH": "1280",
                "FACE_STREAM_HEIGHT": "720",
                "FACE_JPEG_QUALITY": "95",
                "FACE_MAX_CAMERAS": "4",
            }
        )

        self.assertEqual(0.42, config.threshold)
        self.assertEqual(7, config.confirm_frames)
        self.assertEqual(3.5, config.lost_timeout)
        self.assertEqual(1.25, config.encode_interval)
        self.assertEqual(720, config.frame_width)
        self.assertEqual(180, config.rotation)
        self.assertEqual(9.0, config.recognize_timeout)
        self.assertEqual(12.5, config.alert_timeout)
        self.assertEqual(1280, config.stream_width)
        self.assertEqual(720, config.stream_height)
        self.assertEqual(95, config.jpeg_quality)
        self.assertEqual(4, config.max_cameras)

    def test_non_positive_max_cameras_is_rejected(self):
        for value in ("0", "-1", "not-a-number"):
            with self.subTest(value=value):
                with self.assertRaises(FaceRuntimeConfigError) as error:
                    FaceRuntimeConfig.from_env({"FACE_MAX_CAMERAS": value})
                self.assertIn("FACE_MAX_CAMERAS", str(error.exception))

    def test_absolute_model_path_is_preserved_even_when_missing(self):
        with tempfile.TemporaryDirectory(prefix="face config ") as temp_dir:
            root = pathlib.Path(temp_dir)
            missing = root / "models" / "active"
            input_root = root / "input"
            input_root.mkdir()

            config = FaceRuntimeConfig.from_env(
                {
                    "FACE_MODEL_DIR": str(missing),
                    "FACE_MODEL_STAGING_DIR": str(root / "models" / "staging"),
                    "FACE_MODEL_ARCHIVE_DIR": str(root / "models" / "archive"),
                    "FACE_MODEL_FAILED_DIR": str(root / "models" / "failed"),
                    "FACE_ENROLLMENT_INPUT_ROOT": str(input_root),
                }
            )

            self.assertEqual(missing.resolve(), config.model_dir)
            self.assertTrue(config.model_dir.is_dir())

    def test_relative_paths_resolve_from_repository_root(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            module_file = pathlib.Path(temp_dir) / "repo" / "AI_Runtime" / "face_recognition" / "runtime_config.py"
            expected_root = module_file.resolve().parents[2]
            input_root = expected_root / "face-input"
            input_root.mkdir(parents=True)
            config = FaceRuntimeConfig.from_env(
                {
                    "FACE_MODEL_DIR": "var/models/active",
                    "FACE_MODEL_STAGING_DIR": "var/models/staging",
                    "FACE_MODEL_ARCHIVE_DIR": "var/models/archive",
                    "FACE_MODEL_FAILED_DIR": "var/models/failed",
                    "FACE_ENROLLMENT_INPUT_ROOT": "face-input",
                    "FACE_SNAPSHOT_DIR": "var/face snapshots",
                },
                module_file=module_file,
            )

            self.assertEqual((expected_root / "var/models/active").resolve(), config.model_dir)
            self.assertEqual(
                (expected_root / "var/face snapshots").resolve(),
                config.snapshot_dir,
            )

    def test_fallback_uses_file_position_not_repository_name_or_cwd(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            module_file = (
                pathlib.Path(temp_dir)
                / "repository-with-a-different-name"
                / "AI_Runtime"
                / "face_recognition"
                / "runtime_config.py"
            )
            repo_root = module_file.resolve().parents[2]
            (repo_root / "runtime" / "face-data" / "input").mkdir(parents=True)
            expected = (
                repo_root
                / "API"
                / "API"
                / "API"
                / "wwwroot"
                / "uploads"
                / "VideoFace"
                / "FaceID"
            )

            with mock.patch("os.getcwd", return_value=str(pathlib.Path(temp_dir) / "unrelated")):
                config = FaceRuntimeConfig.from_env({}, module_file=module_file)

            self.assertEqual(expected.resolve(), config.model_dir)

    def test_invalid_integer_names_the_environment_variable(self):
        with self.assertRaisesRegex(FaceRuntimeConfigError, "FACE_CONFIRM_FRAMES"):
            FaceRuntimeConfig.from_env({"FACE_CONFIRM_FRAMES": "five"})

    def test_invalid_float_names_the_environment_variable(self):
        with self.assertRaisesRegex(FaceRuntimeConfigError, "FACE_THRESHOLD"):
            FaceRuntimeConfig.from_env({"FACE_THRESHOLD": "close"})

    def test_non_finite_float_is_rejected(self):
        with self.assertRaisesRegex(FaceRuntimeConfigError, "FACE_THRESHOLD"):
            FaceRuntimeConfig.from_env({"FACE_THRESHOLD": "nan"})

    def test_negative_timeout_is_rejected(self):
        with self.assertRaisesRegex(FaceRuntimeConfigError, "FACE_ALERT_TIMEOUT"):
            FaceRuntimeConfig.from_env({"FACE_ALERT_TIMEOUT": "-1"})

    def test_non_positive_dimensions_and_confirm_frames_are_rejected(self):
        invalid_values = {
            "FACE_CONFIRM_FRAMES": "0",
            "FACE_FRAME_WIDTH": "0",
            "FACE_STREAM_WIDTH": "-1",
            "FACE_STREAM_HEIGHT": "0",
        }

        for name, value in invalid_values.items():
            with self.subTest(name=name):
                with self.assertRaisesRegex(FaceRuntimeConfigError, name):
                    FaceRuntimeConfig.from_env({name: value})

    def test_jpeg_quality_outside_opencv_range_is_rejected(self):
        for value in ("-1", "101"):
            with self.subTest(value=value):
                with self.assertRaisesRegex(FaceRuntimeConfigError, "FACE_JPEG_QUALITY"):
                    FaceRuntimeConfig.from_env({"FACE_JPEG_QUALITY": value})

    def test_service_token_is_read_but_hidden_from_representation(self):
        token = "test-secret-that-must-not-be-rendered"

        config = FaceRuntimeConfig.from_env({"FACE_SERVICE_TOKEN": token})

        self.assertEqual(token, config.service_token)
        self.assertNotIn(token, repr(config))

    def test_environment_is_restored_after_test_override(self):
        original = os.environ.get("FACE_THRESHOLD")

        with mock.patch.dict(os.environ, {"FACE_THRESHOLD": "0.51"}, clear=False):
            self.assertEqual(0.51, FaceRuntimeConfig.from_env().threshold)

        self.assertEqual(original, os.environ.get("FACE_THRESHOLD"))

    def test_storage_overrides_create_model_directories_but_not_input(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir)
            input_root = root / "read-only-input"
            input_root.mkdir()
            model_root = root / "models"
            env = {
                "FACE_ENROLLMENT_INPUT_ROOT": str(input_root),
                "FACE_MODEL_DIR": str(model_root / "active"),
                "FACE_MODEL_STAGING_DIR": str(model_root / "staging"),
                "FACE_MODEL_ARCHIVE_DIR": str(model_root / "archive"),
                "FACE_MODEL_FAILED_DIR": str(model_root / "failed"),
            }

            config = FaceRuntimeConfig.from_env(env)

            self.assertEqual(input_root.resolve(), config.enrollment_input_root)
            self.assertTrue(config.model_dir.is_dir())
            self.assertTrue(config.model_staging_dir.is_dir())
            self.assertTrue(config.model_archive_dir.is_dir())
            self.assertTrue(config.model_failed_dir.is_dir())

    def test_missing_input_mount_is_rejected_and_not_created(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            missing_input = pathlib.Path(temp_dir) / "missing"
            with self.assertRaisesRegex(FaceRuntimeConfigError, "must exist"):
                FaceRuntimeConfig.from_env(
                    {"FACE_ENROLLMENT_INPUT_ROOT": str(missing_input)}
                )
            self.assertFalse(missing_input.exists())

    def test_read_only_input_root_is_accepted_without_writes(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir)
            input_root = root / "input"
            input_root.mkdir()
            input_root.chmod(0o555)
            model_root = root / "models"
            try:
                config = FaceRuntimeConfig.from_env(
                    {
                        "FACE_ENROLLMENT_INPUT_ROOT": str(input_root),
                        "FACE_MODEL_DIR": str(model_root / "active"),
                        "FACE_MODEL_STAGING_DIR": str(model_root / "staging"),
                        "FACE_MODEL_ARCHIVE_DIR": str(model_root / "archive"),
                        "FACE_MODEL_FAILED_DIR": str(model_root / "failed"),
                    }
                )
                self.assertEqual(input_root.resolve(), config.enrollment_input_root)
            finally:
                input_root.chmod(0o755)

    def test_parent_traversal_is_rejected(self):
        with self.assertRaisesRegex(FaceRuntimeConfigError, "parent traversal"):
            FaceRuntimeConfig.from_env({"FACE_MODEL_DIR": "../outside"})

    def test_model_directory_inside_input_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            input_root = pathlib.Path(temp_dir) / "input"
            input_root.mkdir()
            with self.assertRaisesRegex(FaceRuntimeConfigError, "must not be located inside"):
                FaceRuntimeConfig.from_env(
                    {
                        "FACE_ENROLLMENT_INPUT_ROOT": str(input_root),
                        "FACE_MODEL_STAGING_DIR": str(input_root / "models" / "staging"),
                        "FACE_MODEL_ARCHIVE_DIR": str(input_root / "models" / "archive"),
                        "FACE_MODEL_FAILED_DIR": str(input_root / "models" / "failed"),
                    }
                )

    def test_model_lifecycle_directories_require_same_filesystem(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir)
            input_root = root / "input"
            input_root.mkdir()
            model_root = root / "models"
            real_stat = pathlib.Path.stat

            def fake_stat(path, *args, **kwargs):
                result = real_stat(path, *args, **kwargs)
                if pathlib.Path(path).name == "archive":
                    return mock.Mock(st_dev=result.st_dev + 1)
                return result

            with mock.patch.object(pathlib.Path, "stat", fake_stat):
                with self.assertRaisesRegex(FaceRuntimeConfigError, "same filesystem"):
                    FaceRuntimeConfig.from_env(
                        {
                            "FACE_ENROLLMENT_INPUT_ROOT": str(input_root),
                            "FACE_MODEL_STAGING_DIR": str(model_root / "staging"),
                            "FACE_MODEL_ARCHIVE_DIR": str(model_root / "archive"),
                            "FACE_MODEL_FAILED_DIR": str(model_root / "failed"),
                        }
                    )

    def test_enrollment_defaults_and_overrides(self):
        config = FaceRuntimeConfig.from_env({})
        self.assertGreater(config.enrollment_min_encodings, 0)
        self.assertGreaterEqual(config.enrollment_max_frames, config.enrollment_min_encodings)
        overridden = FaceRuntimeConfig.from_env({
            "FACE_ENROLLMENT_MIN_ENCODINGS": "4",
            "FACE_ENROLLMENT_MAX_FRAMES": "20",
            "FACE_ENROLLMENT_FRAME_INTERVAL": "2",
            "FACE_ENROLLMENT_DUPLICATE_THRESHOLD": "0.25",
            "FACE_ENROLLMENT_MAX_VIDEO_BYTES": "4096",
        })
        self.assertEqual((4, 20, 2, .25, 4096), (
            overridden.enrollment_min_encodings,
            overridden.enrollment_max_frames,
            overridden.enrollment_frame_interval,
            overridden.enrollment_duplicate_threshold,
            overridden.enrollment_max_video_bytes))

    def test_enrollment_invalid_values_are_rejected(self):
        for env in (
            {"FACE_ENROLLMENT_MIN_ENCODINGS": "0"},
            {"FACE_ENROLLMENT_FRAME_INTERVAL": "0"},
            {"FACE_ENROLLMENT_DUPLICATE_THRESHOLD": "nan"},
            {"FACE_ENROLLMENT_MAX_VIDEO_BYTES": "0"},
            {"FACE_ENROLLMENT_MIN_ENCODINGS": "10", "FACE_ENROLLMENT_MAX_FRAMES": "9"},
        ):
            with self.subTest(env=env), self.assertRaises(FaceRuntimeConfigError):
                FaceRuntimeConfig.from_env(env)


if __name__ == "__main__":
    unittest.main()
