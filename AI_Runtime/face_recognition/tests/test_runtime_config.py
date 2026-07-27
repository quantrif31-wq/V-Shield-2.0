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
        self.assertEqual(5001, config.api_port)
        self.assertEqual(DEFAULT_HEADLESS_MODE, config.headless_mode)
        self.assertIsNone(config.snapshot_dir)
        self.assertIsNone(config.service_token)

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

    def test_absolute_model_path_is_preserved_even_when_missing(self):
        with tempfile.TemporaryDirectory(prefix="face config ") as temp_dir:
            missing = pathlib.Path(temp_dir) / "models not created"

            config = FaceRuntimeConfig.from_env({"FACE_MODEL_DIR": str(missing)})

            self.assertEqual(missing.resolve(), config.model_dir)
            self.assertFalse(config.model_dir.exists())

    def test_relative_paths_resolve_from_repository_root(self):
        module_file = pathlib.Path("/work/renamed-repository/AI_Runtime/face_recognition/runtime_config.py")
        config = FaceRuntimeConfig.from_env(
            {
                "FACE_MODEL_DIR": "var/face models",
                "FACE_SNAPSHOT_DIR": "var/face snapshots",
            },
            module_file=module_file,
        )
        expected_root = module_file.resolve().parents[2]

        self.assertEqual((expected_root / "var/face models").resolve(), config.model_dir)
        self.assertEqual(
            (expected_root / "var/face snapshots").resolve(),
            config.snapshot_dir,
        )

    def test_fallback_uses_file_position_not_repository_name_or_cwd(self):
        module_file = pathlib.Path(
            "/work/repository-with-a-different-name/AI_Runtime/face_recognition/runtime_config.py"
        )
        expected = (
            module_file.resolve().parents[2]
            / "API"
            / "API"
            / "API"
            / "wwwroot"
            / "uploads"
            / "VideoFace"
            / "FaceID"
        )

        with tempfile.TemporaryDirectory() as unrelated_cwd:
            with mock.patch("os.getcwd", return_value=unrelated_cwd):
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


if __name__ == "__main__":
    unittest.main()
