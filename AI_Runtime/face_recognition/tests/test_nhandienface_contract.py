"""Characterization tests for the Flask API consumed by the V-Shield frontend."""

import importlib.util
import pathlib
import pickle
import sys
import tempfile
import unittest
from unittest import mock

import numpy as np


RUNTIME_PATH = pathlib.Path(__file__).resolve().parents[1] / "nhandienface.py"


class UnavailableCapture:
    def set(self, *_args):
        return True

    def isOpened(self):
        return False

    def release(self):
        return None


def load_runtime_without_models():
    """Import the runtime without reading real biometric model files."""
    module_name = "nhandienface_contract_target"
    sys.modules.pop(module_name, None)
    spec = importlib.util.spec_from_file_location(module_name, RUNTIME_PATH)
    module = importlib.util.module_from_spec(spec)
    runtime_directory = str(RUNTIME_PATH.parent)
    model_tempdir = tempfile.TemporaryDirectory(prefix="face-contract-models-")
    model_root = pathlib.Path(model_tempdir.name).parent / (
        pathlib.Path(model_tempdir.name).name + "-lifecycle"
    )
    input_root = model_root / "input"
    input_root.mkdir(parents=True)

    with mock.patch.dict(
        "os.environ",
        {
            "FACE_MODEL_DIR": model_tempdir.name,
            "FACE_ENROLLMENT_INPUT_ROOT": str(input_root),
            "FACE_MODEL_STAGING_DIR": str(model_root / "models" / "staging"),
            "FACE_MODEL_ARCHIVE_DIR": str(model_root / "models" / "archive"),
            "FACE_MODEL_FAILED_DIR": str(model_root / "models" / "failed"),
        },
        clear=False,
    ):
        sys.path.insert(0, runtime_directory)
        try:
            spec.loader.exec_module(module)
        finally:
            sys.path.remove(runtime_directory)

    module._contract_model_tempdir = model_tempdir
    module._contract_storage_root = model_root
    return module


class FaceRuntimeContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.runtime = load_runtime_without_models()

    @classmethod
    def tearDownClass(cls):
        import shutil

        cls.runtime._contract_model_tempdir.cleanup()
        shutil.rmtree(cls.runtime._contract_storage_root, ignore_errors=True)

    def setUp(self):
        self.model_tempdir = tempfile.TemporaryDirectory(prefix="face-endpoint-models-")
        self.runtime.model_registry = self.runtime.FaceModelRegistry(
            self.model_tempdir.name
        )
        self.runtime.camera_manager.shutdown_all()
        self.runtime.camera_manager = self.runtime.CameraManager(
            self.runtime.model_registry,
            self.runtime.CONFIG,
            capture_factory=lambda _url: UnavailableCapture(),
        )
        self.runtime.camera_manager.ensure_session("default")
        self.runtime.stop_event.clear()
        self.runtime.close_camera()
        self.runtime.reset_recognition_state("test setup")
        self.client = self.runtime.app.test_client()

    def tearDown(self):
        self.runtime.camera_manager.shutdown_all()
        self.runtime.stop_event.clear()
        self.model_tempdir.cleanup()

    def test_health_reports_runtime_and_empty_test_registry(self):
        response = self.client.get("/api/health")

        self.assertEqual(200, response.status_code)
        payload = response.get_json()
        self.assertTrue(payload["success"])
        self.assertEqual("FaceID API is running", payload["message"])
        self.assertFalse(payload["camera_enabled"])
        self.assertFalse(payload["camera_connected"])
        self.assertEqual("", payload["ip"])
        self.assertEqual(0, payload["models_loaded"])
        self.assertEqual(0, payload["total_encodings"])
        self.assertIn("last_update", payload)

    def test_status_before_camera_is_started_matches_frontend_contract(self):
        response = self.client.get("/api/camera/status")

        self.assertEqual(200, response.status_code)
        payload = response.get_json()
        expected_fields = {
            "success", "camera_enabled", "camera_connected", "ip",
            "tracking_active", "identity_confirmed", "face_match",
            "employee_id", "confirm_count", "distance", "last_seen",
            "bbox", "timeout", "alert", "scan_locked", "lock_reason",
            "fps", "models_loaded", "total_encodings", "message",
            "last_update", "stream_url",
        }
        self.assertTrue(expected_fields.issubset(payload))
        self.assertFalse(payload["camera_enabled"])
        self.assertFalse(payload["camera_connected"])
        self.assertEqual("", payload["stream_url"])
        self.assertIsNone(payload["employee_id"])

    def test_camera_on_requires_ip(self):
        response = self.client.post("/api/camera/on", json={})

        self.assertEqual(400, response.status_code)
        payload = response.get_json()
        self.assertFalse(payload["success"])
        self.assertIn("message", payload)

    def test_camera_on_records_requested_ip_when_capture_is_unavailable(self):
        camera_url = "http://camera.test:8080/video"

        response = self.client.post("/api/camera/on", json={"ip": camera_url})

        self.assertEqual(200, response.status_code)
        payload = response.get_json()
        self.assertTrue(payload["success"])
        self.assertEqual(camera_url, payload["ip"])
        self.assertEqual("/api/camera/stream", payload["stream_url"])
        status = self.client.get("/api/camera/status").get_json()
        self.assertTrue(status["camera_enabled"])
        self.assertFalse(status["camera_connected"])
        self.assertEqual(camera_url, status["ip"])
        self.assertEqual("/api/camera/stream", status["stream_url"])

    def test_camera_remains_disconnected_when_no_reader_worker_connects(self):
        self.client.post("/api/camera/on", json={"ip": "http://unreachable.test/video"})

        result = self.client.get("/api/camera/result")

        self.assertEqual(200, result.status_code)
        payload = result.get_json()
        self.assertTrue(payload["success"])
        self.assertTrue(payload["camera_enabled"])
        self.assertFalse(payload["camera_connected"])
        self.assertFalse(payload["identity_confirmed"])
        self.assertFalse(payload["face_match"])
        self.assertIsNone(payload["employee_id"])

    def test_reset_clears_locked_recognition_but_preserves_camera_request(self):
        camera_url = "http://camera.test/video"
        self.client.post("/api/camera/on", json={"ip": camera_url})
        self.runtime.update_recognition_state(
            identity_confirmed=True,
            face_match=True,
            employee_id=42,
            scan_locked=True,
            locked_snapshot="snapshot-data",
            locked_face_crop="face-data",
        )

        response = self.client.post("/api/camera/reset")

        self.assertEqual(200, response.status_code)
        self.assertTrue(response.get_json()["success"])
        status = self.client.get("/api/camera/status").get_json()
        self.assertTrue(status["camera_enabled"])
        self.assertEqual(camera_url, status["ip"])
        self.assertFalse(status["identity_confirmed"])
        self.assertFalse(status["scan_locked"])
        self.assertIsNone(status["employee_id"])

    def test_locked_images_exposes_current_locked_image_contract(self):
        response = self.client.get("/api/camera/locked-images")

        self.assertEqual(200, response.status_code)
        payload = response.get_json()
        self.assertEqual(
            {
                "success", "identity_confirmed", "employee_id",
                "scan_locked", "lock_reason", "locked_snapshot",
                "locked_face_crop",
            },
            set(payload),
        )
        self.assertIsNone(payload["locked_snapshot"])
        self.assertIsNone(payload["locked_face_crop"])

    def test_camera_off_clears_enabled_connected_and_ip(self):
        self.client.post("/api/camera/on", json={"ip": "http://camera.test/video"})

        response = self.client.post("/api/camera/off")

        self.assertEqual(200, response.status_code)
        self.assertTrue(response.get_json()["success"])
        status = self.client.get("/api/camera/status").get_json()
        self.assertFalse(status["camera_enabled"])
        self.assertFalse(status["camera_connected"])
        self.assertEqual("", status["ip"])
        self.assertEqual("", status["stream_url"])

    def test_legacy_and_camera_specific_routes_share_default_session(self):
        camera_url = "http://camera.test/default"
        self.client.post("/api/camera/on", json={"ip": camera_url})

        status = self.client.get("/api/cameras/default/status")

        self.assertEqual(200, status.status_code)
        self.assertEqual(camera_url, status.get_json()["ip"])
        self.client.post("/api/cameras/default/reset")
        self.assertFalse(
            self.client.get("/api/camera/status").get_json()["scan_locked"]
        )

    def test_camera_specific_routes_validate_and_report_missing_sessions(self):
        self.assertEqual(
            400,
            self.client.get("/api/cameras/a..b/status").status_code,
        )
        self.assertEqual(
            404,
            self.client.get("/api/cameras/missing/status").status_code,
        )

    def test_camera_list_does_not_expose_stream_urls(self):
        self.client.post(
            "/api/cameras/gate-01/start",
            json={"ip": "rtsp://user:secret@camera.test/live", "laneId": "lane-1"},
        )

        response = self.client.get("/api/cameras")

        self.assertEqual(200, response.status_code)
        serialized = response.get_data(as_text=True)
        self.assertNotIn("secret", serialized)
        self.assertNotIn("rtsp://", serialized)
        payload = response.get_json()
        session = next(
            item for item in payload["sessions"] if item["cameraId"] == "gate-01"
        )
        self.assertEqual("lane-1", session["laneId"])

    def test_camera_specific_start_is_idempotent_and_conflicts_on_new_url(self):
        first = self.client.post(
            "/api/cameras/gate-01/start",
            json={"ip": "fake://one", "laneId": "lane-1"},
        )
        second = self.client.post(
            "/api/cameras/gate-01/start",
            json={"ip": "fake://one", "laneId": "lane-1"},
        )
        conflict = self.client.post(
            "/api/cameras/gate-01/start",
            json={"ip": "fake://two", "laneId": "lane-1"},
        )

        self.assertEqual(200, first.status_code)
        self.assertFalse(first.get_json()["idempotent"])
        self.assertEqual(200, second.status_code)
        self.assertTrue(second.get_json()["idempotent"])
        self.assertEqual(409, conflict.status_code)
        self.assertEqual("CAMERA_CONFLICT", conflict.get_json()["errorCode"])

    def test_camera_limit_returns_conflict(self):
        self.client.post("/api/cameras/one/start", json={"ip": "fake://one"})
        self.client.post("/api/cameras/two/start", json={"ip": "fake://two"})

        response = self.client.post(
            "/api/cameras/three/start",
            json={"ip": "fake://three"},
        )

        self.assertEqual(409, response.status_code)
        self.assertEqual("CAMERA_CONFLICT", response.get_json()["errorCode"])

    def test_models_endpoint_does_not_expose_encodings_or_token(self):
        response = self.client.get("/api/models")

        self.assertEqual(200, response.status_code)
        payload = response.get_json()
        self.assertEqual(1, payload["version"])
        self.assertEqual(0, payload["successfulFileCount"])
        self.assertEqual(0, payload["encodingCount"])
        self.assertEqual([], payload["models"])
        self.assertEqual([], payload["errors"])
        serialized = response.get_data(as_text=True).lower()
        self.assertNotIn("encodings", serialized)
        self.assertNotIn("service_token", serialized)
        self.assertNotIn("serviceToken", serialized)
        self.assertNotIn(self.model_tempdir.name.lower(), serialized)

    def test_models_reload_successfully_publishes_new_version(self):
        model_path = pathlib.Path(self.model_tempdir.name) / "emp_42_test.pkl"
        with model_path.open("wb") as stream:
            pickle.dump([np.zeros(128), np.ones(128)], stream)

        response = self.client.post("/api/models/reload")

        self.assertEqual(200, response.status_code)
        payload = response.get_json()
        self.assertTrue(payload["success"])
        self.assertEqual(1, payload["previousVersion"])
        self.assertEqual(2, payload["currentVersion"])
        self.assertEqual(1, payload["successfulFileCount"])
        self.assertEqual(2, payload["encodingCount"])

        models = self.client.get("/api/models").get_json()
        self.assertEqual("42", models["models"][0]["subjectId"])
        self.assertNotIn("encodings", str(models).lower())

    def test_models_reload_rejects_corrupt_file_and_keeps_version(self):
        model_path = pathlib.Path(self.model_tempdir.name) / "broken.pkl"
        model_path.write_bytes(b"not a pickle")

        response = self.client.post("/api/models/reload")

        self.assertEqual(422, response.status_code)
        payload = response.get_json()
        self.assertFalse(payload["success"])
        self.assertEqual(1, payload["previousVersion"])
        self.assertEqual(1, payload["currentVersion"])
        self.assertEqual("MODEL_RELOAD_REJECTED", payload["errorCode"])
        self.assertEqual("MODEL_DESERIALIZATION_FAILED", payload["errors"][0]["errorCode"])
        self.assertNotIn("pickle", payload["errors"][0]["message"].lower())

    def test_models_reload_rejects_request_body_with_path(self):
        response = self.client.post(
            "/api/models/reload",
            json={"path": "C:/untrusted/models"},
        )

        self.assertEqual(400, response.status_code)
        self.assertEqual(
            "REQUEST_BODY_NOT_ALLOWED",
            response.get_json()["errorCode"],
        )

    def test_models_reload_returns_conflict_when_reload_is_in_progress(self):
        self.runtime.model_registry._reload_lock.acquire()
        try:
            response = self.client.post("/api/models/reload")
        finally:
            self.runtime.model_registry._reload_lock.release()

        self.assertEqual(409, response.status_code)
        payload = response.get_json()
        self.assertFalse(payload["success"])
        self.assertEqual("RELOAD_IN_PROGRESS", payload["errorCode"])
        self.assertEqual(payload["previousVersion"], payload["currentVersion"])

    def test_models_reload_unexpected_failure_returns_sanitized_500(self):
        with mock.patch.object(
            self.runtime.model_registry,
            "reload",
            side_effect=RuntimeError("sensitive filesystem detail"),
        ):
            response = self.client.post("/api/models/reload")

        self.assertEqual(500, response.status_code)
        payload = response.get_json()
        self.assertEqual("MODEL_RELOAD_INTERNAL_ERROR", payload["errorCode"])
        self.assertNotIn("sensitive filesystem detail", response.get_data(as_text=True))


if __name__ == "__main__":
    unittest.main()
