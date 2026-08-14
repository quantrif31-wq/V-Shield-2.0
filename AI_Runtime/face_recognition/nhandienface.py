"""Flask gateway for the isolated multi-camera Face ID runtime."""

from __future__ import annotations

import atexit
import json
import logging
import os
import site
import sys
import threading
import time
from typing import Any

import cv2
import numpy as np
from flask import Flask, Response, jsonify, request, stream_with_context
from flask_cors import CORS

from camera_manager import (
    CameraManager,
    CameraManagerError,
    CameraNotFoundError,
    validate_camera_id,
)
from face_detector import FaceDetector, FaceDetectorError
from face_embedder import FaceEmbedder, FaceEmbedderError
from guided_enrollment import GuidedEnrollmentError, GuidedEnrollmentSession
from landmark_service import LandmarkService, LandmarkServiceError
from model_registry import FaceModelRegistry
from runtime_config import FaceRuntimeConfig
from enrollment_service import EnrollmentError, EnrollmentService


if site.USER_SITE is None:
    site.USER_SITE = (
        os.path.dirname(sys.executable)
        if getattr(sys, "frozen", False)
        else os.getcwd()
    )

os.environ["OPENCV_FFMPEG_LOGLEVEL"] = "8"
logging.disable(logging.WARNING)

CONFIG = FaceRuntimeConfig.from_env(module_file=__file__)
API_PORT = CONFIG.api_port
HEADLESS_MODE = CONFIG.headless_mode
WINDOW_NAME = "FACEID MULTI-CAMERA SESSION MODE"

app = Flask(__name__)
CORS(app, resources={r"/api/*": {"origins": "*"}}, supports_credentials=False)

print("Loading face models...")
print("Face model directory:", CONFIG.model_dir)
model_registry = FaceModelRegistry(CONFIG.model_dir)

detector = None
embedder = None
landmark_service = None
if CONFIG.detector_path is not None:
    try:
        detector = FaceDetector(CONFIG.detector_path)
        print("YuNet detector loaded from:", CONFIG.detector_path)
    except FaceDetectorError as error:
        print("WARNING: face detector unavailable ->", error)
if CONFIG.embedder_path is not None:
    try:
        embedder = FaceEmbedder(
            CONFIG.embedder_path,
            prefer_gpu=CONFIG.prefer_gpu,
            gpu_device_id=CONFIG.gpu_device_id,
        )
        print("SFace embedder loaded from:", CONFIG.embedder_path)
        print("SFace backend:", embedder.backend)
    except FaceEmbedderError as error:
        print("WARNING: face embedder unavailable ->", error)
if CONFIG.landmark_path is not None:
    try:
        landmark_service = LandmarkService(CONFIG.landmark_path)
        print("MediaPipe FaceLandmarker loaded from:", CONFIG.landmark_path)
    except LandmarkServiceError as error:
        print("WARNING: face landmarker unavailable ->", error)

enrollment_service = EnrollmentService(
    CONFIG, model_registry, detector=detector, embedder=embedder,
    landmark_service=landmark_service,
)
guided_enrollment = GuidedEnrollmentSession(
    CONFIG, detector=detector, embedder=embedder,
    landmark_service=landmark_service,
)
initial_model_snapshot = model_registry.current_snapshot()
print("Loaded models:", initial_model_snapshot.successful_file_count)
print("Total encodings:", initial_model_snapshot.encoding_count)
for model_error in initial_model_snapshot.errors:
    target = model_error.file_name or "model directory"
    print(
        f"WARNING: {target} -> {model_error.error_code}: "
        f"{model_error.message}"
    )

camera_manager = CameraManager(
    model_registry, CONFIG, detector=detector, embedder=embedder
)
camera_manager.ensure_session("default")
stop_event = threading.Event()


def now_ts() -> str:
    return time.strftime("%Y-%m-%d %H:%M:%S")


def make_json_safe(value: Any) -> Any:
    if isinstance(value, dict):
        return {str(key): make_json_safe(item) for key, item in value.items()}
    if isinstance(value, (list, tuple)):
        return [make_json_safe(item) for item in value]
    if isinstance(value, np.bool_):
        return bool(value)
    if isinstance(
        value,
        (
            np.int8, np.int16, np.int32, np.int64,
            np.uint8, np.uint16, np.uint32, np.uint64,
        ),
    ):
        return int(value)
    if isinstance(value, (np.float16, np.float32, np.float64)):
        return float(value)
    if isinstance(value, np.ndarray):
        return value.tolist()
    return value


def _default_session():
    return camera_manager.ensure_session("default")


# Compatibility helpers retained for characterization tests and the debug view.
# They delegate to the same "default" CameraSession used by every legacy route.
def open_camera(ip: str) -> bool:
    camera_manager.start_session("default", ip)
    return True


def close_camera() -> None:
    _default_session().stop()


def reset_recognition_state(reason: str = "Recognition state reset") -> None:
    session = _default_session()
    if session.enabled:
        session.reset()
    else:
        with session.session_lock:
            session._reset_recognition_locked(reason)


def update_recognition_state(**kwargs: Any) -> None:
    _default_session().update_recognition_state(**kwargs)


def get_recognition_snapshot(include_images: bool = True) -> dict[str, Any]:
    return _default_session().result(include_images=include_images)


def get_camera_flags() -> tuple[bool, str, bool]:
    session = _default_session()
    with session.session_lock:
        return (
            session.enabled,
            session.stream_url,
            session.connection_status == "connected",
        )


def _error_payload(error: Exception) -> tuple[Response, int]:
    if isinstance(error, CameraManagerError):
        return jsonify(
            {
                "success": False,
                "errorCode": error.error_code,
                "message": str(error),
            }
        ), error.status_code
    if isinstance(error, ValueError):
        return jsonify(
            {
                "success": False,
                "errorCode": "INVALID_REQUEST",
                "message": str(error),
            }
        ), 400
    return jsonify(
        {
            "success": False,
            "errorCode": "CAMERA_INTERNAL_ERROR",
            "message": "Camera operation failed unexpectedly.",
        }
    ), 500


def _validated_lane_id(data: dict[str, Any]) -> str | None:
    lane_id = data.get("laneId")
    if lane_id is None:
        return None
    if not isinstance(lane_id, str) or len(lane_id) > 128:
        raise ValueError("laneId is invalid.")
    return lane_id


@app.get("/api/cameras")
def api_cameras():
    return jsonify(camera_manager.list_sessions()), 200


@app.post("/api/cameras/<camera_id>/start")
def api_camera_specific_start(camera_id: str):
    try:
        validate_camera_id(camera_id)
        data = request.get_json(silent=True)
        if not isinstance(data, dict):
            raise ValueError("A JSON request body is required.")
        stream_url = data.get("ip")
        if not isinstance(stream_url, str) or not stream_url.strip():
            raise ValueError("Camera stream URL is required.")
        lane_id = _validated_lane_id(data)
        session, idempotent = camera_manager.start_session(
            camera_id, stream_url, lane_id
        )
        return jsonify(
            {
                "success": True,
                "cameraId": session.camera_id,
                "laneId": session.lane_id,
                "idempotent": idempotent,
                "message": "Camera session is active.",
            }
        ), 200
    except Exception as error:
        return _error_payload(error)


@app.post("/api/cameras/<camera_id>/stop")
def api_camera_specific_stop(camera_id: str):
    try:
        camera_manager.stop_session(camera_id)
        return jsonify(
            {
                "success": True,
                "cameraId": camera_id,
                "message": "Camera session stopped.",
            }
        ), 200
    except Exception as error:
        return _error_payload(error)


@app.post("/api/cameras/<camera_id>/reset")
def api_camera_specific_reset(camera_id: str):
    try:
        camera_manager.reset_session(camera_id)
        return jsonify(
            {
                "success": True,
                "cameraId": camera_id,
                "message": "Camera recognition state reset.",
            }
        ), 200
    except Exception as error:
        return _error_payload(error)


@app.get("/api/cameras/<camera_id>/status")
def api_camera_specific_status(camera_id: str):
    try:
        return jsonify(camera_manager.get_status(camera_id)), 200
    except Exception as error:
        return _error_payload(error)


@app.get("/api/cameras/<camera_id>/result")
def api_camera_specific_result(camera_id: str):
    try:
        return jsonify(camera_manager.get_result(camera_id)), 200
    except Exception as error:
        return _error_payload(error)


@app.get("/api/cameras/<camera_id>/locked-images")
def api_camera_specific_locked_images(camera_id: str):
    try:
        return jsonify(camera_manager.get_locked_images(camera_id)), 200
    except Exception as error:
        return _error_payload(error)


@app.get("/api/cameras/<camera_id>/events")
def api_camera_specific_events(camera_id: str):
    try:
        after_sequence = int(request.args.get("afterSequence", "0"))
        limit = int(request.args.get("limit", "100"))
        raw_generation = request.args.get("sessionGeneration")
        generation = int(raw_generation) if raw_generation is not None else None
        return jsonify(camera_manager.get_events(
            camera_id, after_sequence=after_sequence,
            session_generation=generation, limit=limit)), 200
    except (TypeError, ValueError):
        return jsonify({"success": False, "errorCode": "INVALID_EVENT_QUERY",
                        "message": "Event query is invalid."}), 400
    except Exception as error:
        return _error_payload(error)


# Legacy camera routes are transitional aliases for cameraId="default".
@app.post("/api/camera/on")
def api_camera_on():
    try:
        data = request.get_json(silent=True) or {}
        ip = str(data.get("ip", "")).strip()
        if not ip:
            return jsonify(
                {"success": False, "message": "Thiáº¿u IP camera"}
            ), 400
        camera_manager.start_session("default", ip)
        return jsonify(
            {
                "success": True,
                "message": "Äang káº¿t ná»‘i camera",
                "ip": ip,
                "stream_url": "/api/camera/stream",
            }
        ), 200
    except Exception as error:
        response, status = _error_payload(error)
        if status == 409:
            return response, status
        return jsonify(
            {"success": False, "message": "Lá»—i báº­t camera"}
        ), status


@app.post("/api/camera/off")
def api_camera_off():
    try:
        _default_session().stop()
        return jsonify(
            {"success": True, "message": "ÄÃ£ táº¯t camera"}
        ), 200
    except Exception:
        return jsonify(
            {"success": False, "message": "Lá»—i táº¯t camera"}
        ), 500


@app.post("/api/camera/reset")
def api_camera_reset():
    try:
        _default_session().reset()
        return jsonify(
            {
                "success": True,
                "message": "ÄÃ£ reset tráº¡ng thÃ¡i nháº­n diá»‡n",
            }
        ), 200
    except Exception:
        return jsonify(
            {"success": False, "message": "Lá»—i reset tráº¡ng thÃ¡i"}
        ), 500


@app.get("/api/camera/status")
def api_camera_status():
    try:
        return jsonify(_default_session().status()), 200
    except Exception:
        return jsonify(
            {
                "success": False,
                "message": "Lá»—i láº¥y tráº¡ng thÃ¡i camera",
            }
        ), 500


@app.get("/api/camera/result")
def api_camera_result():
    try:
        return jsonify(_default_session().result(include_images=True)), 200
    except Exception:
        return jsonify(
            {
                "success": False,
                "message": "Lá»—i láº¥y káº¿t quáº£ nháº­n diá»‡n",
            }
        ), 500


@app.get("/api/camera/locked-images")
def api_camera_locked_images():
    try:
        return jsonify(_default_session().locked_image_result()), 200
    except Exception:
        return jsonify(
            {"success": False, "message": "Lá»—i láº¥y áº£nh lock"}
        ), 500


@app.get("/api/camera/stream")
def api_camera_stream():
    response = Response(
        stream_with_context(_default_session().mjpeg_generator()),
        mimetype="multipart/x-mixed-replace; boundary=frame",
    )
    response.headers["Cache-Control"] = "no-cache, no-store, must-revalidate"
    response.headers["Pragma"] = "no-cache"
    response.headers["Expires"] = "0"
    response.headers["Access-Control-Allow-Origin"] = "*"
    return response


@app.get("/api/camera/events")
def api_camera_events():
    def event_stream():
        last_payload = None
        while not stop_event.is_set():
            snapshot = _default_session().result(include_images=False)
            lite = {
                key: snapshot[key]
                for key in (
                    "success", "camera_enabled", "camera_connected", "ip",
                    "tracking_active", "identity_confirmed", "face_match",
                    "employee_id", "confirm_count", "distance", "last_seen",
                    "bbox", "timeout", "alert", "scan_locked", "lock_reason",
                    "fps", "message", "last_update",
                )
            }
            payload = json.dumps(make_json_safe(lite), ensure_ascii=False)
            if payload != last_payload:
                yield f"event: state\ndata: {payload}\n\n"
                last_payload = payload
            else:
                yield 'event: ping\ndata: {"ok":true}\n\n'
            time.sleep(0.2)

    response = Response(
        stream_with_context(event_stream()), mimetype="text/event-stream"
    )
    response.headers["Cache-Control"] = "no-cache"
    response.headers["X-Accel-Buffering"] = "no"
    response.headers["Connection"] = "keep-alive"
    response.headers["Access-Control-Allow-Origin"] = "*"
    return response


def registry_timestamp(value) -> str:
    return value.isoformat().replace("+00:00", "Z")


def registry_error_payload(error) -> dict[str, Any]:
    return {
        "fileName": error.file_name,
        "errorCode": error.error_code,
        "message": error.message,
    }


def registry_snapshot_payload(snapshot) -> dict[str, Any]:
    return {
        "version": snapshot.version,
        "loadedAt": registry_timestamp(snapshot.loaded_at),
        "modelDirectory": snapshot.model_directory.name,
        "successfulFileCount": snapshot.successful_file_count,
        "encodingCount": snapshot.encoding_count,
        "errorCount": len(snapshot.errors),
        "models": [
            {
                "fileName": model.file_name,
                "subjectId": model.subject_id,
                "checksum": model.checksum,
                "encodingCount": model.encoding_count,
                "registryVersion": snapshot.version,
            }
            for model in snapshot.model_files
        ],
        "errors": [registry_error_payload(error) for error in snapshot.errors],
    }


@app.get("/api/models")
def api_models():
    return jsonify(
        registry_snapshot_payload(model_registry.current_snapshot())
    ), 200


@app.post("/api/models/reload")
def api_models_reload():
    if request.get_data(cache=True).strip():
        return jsonify(
            {
                "success": False,
                "errorCode": "REQUEST_BODY_NOT_ALLOWED",
                "message": "Model reload does not accept a request body.",
            }
        ), 400
    try:
        result = model_registry.reload()
        snapshot = result.current_snapshot
        if result.error_code == "RELOAD_IN_PROGRESS":
            return jsonify(
                {
                    "success": False,
                    "previousVersion": result.previous_version,
                    "currentVersion": snapshot.version,
                    "successfulFileCount": snapshot.successful_file_count,
                    "encodingCount": snapshot.encoding_count,
                    "errorCount": 0,
                    "errorCode": "RELOAD_IN_PROGRESS",
                    "errors": [],
                }
            ), 409
        if not result.success:
            return jsonify(
                {
                    "success": False,
                    "previousVersion": result.previous_version,
                    "currentVersion": snapshot.version,
                    "successfulFileCount": snapshot.successful_file_count,
                    "encodingCount": snapshot.encoding_count,
                    "errorCount": len(result.errors),
                    "errorCode": result.error_code,
                    "errors": [
                        registry_error_payload(error) for error in result.errors
                    ],
                }
            ), 422
        return jsonify(
            {
                "success": True,
                "previousVersion": result.previous_version,
                "currentVersion": snapshot.version,
                "successfulFileCount": snapshot.successful_file_count,
                "encodingCount": snapshot.encoding_count,
                "errorCount": 0,
                "loadedAt": registry_timestamp(snapshot.loaded_at),
            }
        ), 200
    except Exception:
        return jsonify(
            {
                "success": False,
                "errorCode": "MODEL_RELOAD_INTERNAL_ERROR",
                "message": "Model registry reload failed unexpectedly.",
            }
        ), 500


@app.get("/api/health")
def api_health():
    enabled, ip, connected = get_camera_flags()
    snapshot = model_registry.current_snapshot()
    return jsonify(
        {
            "success": True,
            "message": "FaceID API is running",
            "camera_enabled": enabled,
            "camera_connected": connected,
            "ip": ip,
            "models_loaded": snapshot.successful_file_count,
            "total_encodings": snapshot.encoding_count,
            "last_update": now_ts(),
        }
    ), 200


def run_api_server() -> None:
    print(f"API running at: http://127.0.0.1:{API_PORT}")
    app.run(
        host="0.0.0.0",
        port=API_PORT,
        debug=False,
        use_reloader=False,
        threaded=True,
        )


def enrollment_error_response(error: EnrollmentError):
    return jsonify({"success": False, "failureCode": error.code,
                    "message": str(error), **error.details}), error.status_code


@app.post("/api/enrollments/<job_id>/prepare")
def prepare_enrollment(job_id):
    payload = request.get_json(silent=True)
    if not isinstance(payload, dict):
        return jsonify({"failureCode": "InvalidRequest", "message": "JSON body required."}), 400
    try:
        return jsonify(enrollment_service.prepare_enrollment(
            job_id, str(payload.get("subjectId", "")),
            str(payload.get("sourceReference", ""))))
    except EnrollmentError as error:
        return enrollment_error_response(error)
    except Exception:
        return jsonify({"failureCode": "EnrollmentFailed",
                        "message": "Enrollment preparation failed."}), 500


@app.post("/api/enrollments/<job_id>/activate")
def activate_enrollment(job_id):
    payload = request.get_json(silent=True)
    if not isinstance(payload, dict):
        return jsonify({"failureCode": "InvalidRequest", "message": "JSON body required."}), 400
    try:
        return jsonify(enrollment_service.activate_candidate(
            job_id, str(payload.get("subjectId", "")), payload.get("version"),
            str(payload.get("expectedChecksum", "")),
            str(payload.get("expectedModelFileName", ""))))
    except EnrollmentError as error:
        return enrollment_error_response(error)
    except Exception:
        return jsonify({"failureCode": "ActivationFailed",
                        "message": "Candidate activation failed."}), 500


@app.post("/api/enrollments/<job_id>/discard")
def discard_enrollment(job_id):
    try:
        return jsonify(enrollment_service.discard_candidate(job_id))
    except EnrollmentError as error:
        return enrollment_error_response(error)


@app.post("/api/enrollments/live")
def live_enrollment():
    payload = request.get_json(silent=True)
    if not isinstance(payload, dict):
        return jsonify({"failureCode": "InvalidRequest", "message": "JSON body required."}), 400
    try:
        return jsonify(enrollment_service.enroll_from_images(
            str(payload.get("subjectId", "")),
            payload.get("images")))
    except EnrollmentError as error:
        return enrollment_error_response(error)
    except Exception:
        return jsonify({"failureCode": "LiveEnrollmentFailed",
                        "message": "Live enrollment failed."}), 500


@app.post("/api/models/subjects/<subject_id>/revoke")
def revoke_subject_model(subject_id):
    try:
        return jsonify(enrollment_service.revoke_subject_model(subject_id))
    except EnrollmentError as error:
        return enrollment_error_response(error)
    except Exception:
        return jsonify({"failureCode": "RevokeFailed",
                        "message": "Model revocation failed."}), 500


def guided_enrollment_error_response(error: GuidedEnrollmentError):
    return jsonify({"success": False, "failureCode": error.code,
                    "message": str(error), **error.details}), error.status_code


@app.post("/api/enrollments/guided/start")
def guided_start():
    payload = request.get_json(silent=True)
    if not isinstance(payload, dict):
        return jsonify({"failureCode": "InvalidRequest", "message": "JSON body required."}), 400
    stream_url = str(payload.get("streamUrl", "") or "").strip()
    pose_mode = str(payload.get("poseMode", "") or "").strip() or None
    try:
        guided_enrollment.start(stream_url, pose_mode=pose_mode)
        return jsonify({
            "success": True,
            "sessionId": guided_enrollment.session_id,
            "snapshot": guided_enrollment.snapshot(),
        }), 200
    except GuidedEnrollmentError as error:
        return guided_enrollment_error_response(error)
    except Exception:
        return jsonify({"failureCode": "GuidedStartFailed",
                        "message": "Guided enrollment start failed."}), 500


@app.get("/api/enrollments/guided/progress")
def guided_progress():
    try:
        return jsonify({
            "success": True,
            "snapshot": guided_enrollment.snapshot(),
        }), 200
    except Exception:
        return jsonify({"failureCode": "GuidedProgressFailed",
                        "message": "Guided enrollment progress failed."}), 500


@app.post("/api/enrollments/guided/stop")
def guided_stop():
    try:
        guided_enrollment.stop()
        return jsonify({"success": True, "message": "Guided enrollment stopped."}), 200
    except Exception:
        return jsonify({"failureCode": "GuidedStopFailed",
                        "message": "Guided enrollment stop failed."}), 500


@app.post("/api/enrollments/guided/confirm")
def guided_confirm():
    payload = request.get_json(silent=True)
    if not isinstance(payload, dict):
        return jsonify({"failureCode": "InvalidRequest", "message": "JSON body required."}), 400
    subject_id = str(payload.get("subjectId", "") or "").strip()
    try:
        snapshot = guided_enrollment.snapshot()
        if not subject_id:
            raise GuidedEnrollmentError(
                "MissingSubjectId",
                "Chưa chọn đối tượng. Vui lòng nhập mã nhân viên hoặc khách trước khi xác nhận.",
                400)
        if snapshot["samplesCollected"] < CONFIG.enrollment_min_encodings:
            raise GuidedEnrollmentError(
                "InsufficientUsableFrames",
                f"Chỉ thu được {snapshot['samplesCollected']} mẫu; cần tối thiểu "
                f"{CONFIG.enrollment_min_encodings}. Hãy tiếp tục quay.", 422)
        if not snapshot["anglesComplete"]:
            missing = snapshot["missingAngles"]
            raise GuidedEnrollmentError(
                "InsufficientAngles",
                f"Chưa đủ 5 góc. Còn thiếu: {', '.join(missing)}. Hãy quay theo hướng dẫn.",
                422)
        vectors = guided_enrollment.captured_vectors()
        result = enrollment_service.activate_live_model(
            subject_id, vectors, guided_enrollment.captured_pose_metadata())
        guided_enrollment.stop()
        guided_enrollment.status = "idle"
        return jsonify({"success": True, **result}), 200
    except GuidedEnrollmentError as error:
        return guided_enrollment_error_response(error)
    except EnrollmentError as error:
        return enrollment_error_response(error)
    except Exception:
        return jsonify({"failureCode": "GuidedConfirmFailed",
                        "message": "Guided enrollment confirm failed."}), 500


def debug_view_loop() -> None:
    cv2.namedWindow(WINDOW_NAME, cv2.WINDOW_NORMAL)
    while not stop_event.is_set():
        session = _default_session()
        with session.frame_lock:
            display = (
                None
                if session.latest_display_frame is None
                else session.latest_display_frame.copy()
            )
        if display is None:
            display = np.zeros(
                (CONFIG.stream_height, CONFIG.stream_width, 3), dtype=np.uint8
            )
        cv2.imshow(WINDOW_NAME, display)
        if cv2.waitKey(1) & 0xFF == ord("q"):
            stop_event.set()
        time.sleep(0.01)


def shutdown_runtime() -> None:
    stop_event.set()
    camera_manager.shutdown_all()


atexit.register(shutdown_runtime)


def main() -> None:
    print("\n===== FACEID MULTI-CAMERA SESSION MODE =====")
    if HEADLESS_MODE:
        run_api_server()
    else:
        api_thread = threading.Thread(target=run_api_server, daemon=True)
        api_thread.start()
        debug_view_loop()
    shutdown_runtime()
    try:
        cv2.destroyAllWindows()
    except Exception:
        pass


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        shutdown_runtime()
