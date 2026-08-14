"""Controlled, filesystem-safe face enrollment lifecycle."""

from __future__ import annotations

import base64
import hashlib
import os
import re
import threading
from dataclasses import asdict, dataclass
from pathlib import Path

import cv2
import numpy as np

from face_detector import FaceDetector
from face_quality import FaceQualityGate, QualityResult
from pose_guide import PoseGuide, PoseSample, euler_from_matrix
from template_store import (
    TemplateStoreError,
    checksum_of_file,
    cosine_distance,
    load_templates,
    save_template,
)


JOB_ID = re.compile(r"^[0-9a-fA-F-]{32,36}$")
SUBJECT_ID = re.compile(r"^[1-9][0-9]{0,9}$")
MODEL_NAME = re.compile(r"^emp_([1-9][0-9]{0,9})_v([1-9][0-9]*)_([0-9a-f]{8})\.(pkl|json)$")
VIDEO_EXTENSIONS = {".mp4", ".mov", ".avi"}


class EnrollmentError(RuntimeError):
    def __init__(self, code: str, message: str, status_code: int, **details):
        super().__init__(message)
        self.code = code
        self.status_code = status_code
        self.details = details


@dataclass(frozen=True)
class PrepareResult:
    candidateReference: str
    candidateChecksum: str
    totalInputFrames: int
    processedFrameCount: int
    usableFrameCount: int
    noFaceFrameCount: int
    multipleFaceFrameCount: int
    invalidFrameCount: int
    encodingCount: int
    qualityScore: float
    duplicateSubjectId: str | None = None
    duplicateDistance: float | None = None


class EnrollmentService:
    def __init__(self, config, registry, *, detector=None, embedder=None,
                 landmark_service=None):
        self.config = config
        self.registry = registry
        self._lock = threading.RLock()
        self.detector = detector
        self.embedder = embedder
        self.landmark_service = landmark_service
        self.quality_gate = FaceQualityGate(
            min_sharpness=getattr(config, "face_quality_min_sharpness", 30.0),
            min_brightness=getattr(config, "face_quality_min_brightness", 60.0),
            max_brightness=getattr(config, "face_quality_max_brightness", 220.0),
            min_face_width=getattr(config, "face_quality_min_face_width", 80),
            min_eye_aspect_ratio=getattr(config, "face_quality_min_eye_aspect_ratio", 0.18),
        )
        self.pose_guide = PoseGuide(
            min_frames_per_bin=getattr(config, "enrollment_pose_min_frames", 3),
        )

    def _detect_and_embed(
        self, frame_bgr: np.ndarray
    ) -> list[dict]:
        """Detect exactly one face and return its SFace embedding + pose.

        Returns a list of dicts with ``vector`` and ``pose``. An empty list
        means no single usable face was found in the frame.
        """
        if self.detector is None or self.embedder is None:
            return []
        detections = self.detector.detect(frame_bgr)
        if detections is None or len(detections) != 1:
            return []
        detection = detections[0]
        bbox = FaceDetector.box_from_detection(detection)
        landmarks = FaceDetector.landmarks_from_detection(detection)
        try:
            vector = self.embedder.align_and_embed(frame_bgr, landmarks)
        except Exception:
            return []
        array = np.asarray(vector, dtype=np.float64)
        if array.shape != (128,) or not np.all(np.isfinite(array)):
            return []

        pose = None
        if self.landmark_service is not None:
            pose = self._estimate_pose(frame_bgr)
        return [{"vector": array, "pose": pose, "bbox": bbox, "landmarks": landmarks}]

    def _estimate_pose(self, frame_bgr: np.ndarray) -> dict | None:
        try:
            _, matrix = self.landmark_service.estimate(frame_bgr)
        except Exception:
            return None
        if matrix is None:
            return None
        try:
            yaw, pitch, roll = euler_from_matrix(matrix)
        except Exception:
            return None
        return {"yaw": float(yaw), "pitch": float(pitch), "roll": float(roll)}

    def prepare_enrollment(self, job_id: str, subject_id: str, source_reference: str):
        self._validate_job_subject(job_id, subject_id)
        source = self._source_path(source_reference)
        candidate = self.config.model_staging_dir / f"{job_id.lower()}.json"
        if candidate.exists():
            encodings = self._read_candidate(candidate)
            return self._result_for_existing(candidate, encodings)

        capture = cv2.VideoCapture(str(source))
        if not capture.isOpened():
            capture.release()
            raise EnrollmentError("VideoUnreadable", "Video could not be opened.", 422)
        total = int(capture.get(cv2.CAP_PROP_FRAME_COUNT) or 0)
        processed = usable = no_face = multiple = invalid = 0
        encodings: list[np.ndarray] = []
        pose_samples: list[dict] = []
        quality_rejected: dict[str, int] = {}
        frame_index = 0
        try:
            while processed < self.config.enrollment_max_frames:
                ok, frame = capture.read()
                if not ok:
                    break
                frame_index += 1
                if (frame_index - 1) % self.config.enrollment_frame_interval:
                    continue
                processed += 1
                samples = self._detect_and_embed(frame)
                if not samples:
                    no_face += 1
                    continue
                sample = samples[0]
                value = np.asarray(sample["vector"], dtype=np.float64)
                if value.shape != (128,) or not np.all(np.isfinite(value)):
                    invalid += 1
                    continue
                quality = self.quality_gate.evaluate(
                    frame, sample["bbox"], sample["landmarks"]
                )
                if not quality.passed:
                    for reason in quality.reasons:
                        quality_rejected[reason] = quality_rejected.get(reason, 0) + 1
                    invalid += 1
                    continue
                encodings.append(value)
                if sample["pose"] is not None:
                    pose_samples.append(sample["pose"])
                usable += 1
        finally:
            capture.release()

        if len(encodings) < self.config.enrollment_min_encodings:
            if multiple and multiple * 2 >= max(1, processed):
                raise EnrollmentError(
                    "MultipleFacesDetected",
                    "Too many sampled frames contain multiple faces.", 422)
            raise EnrollmentError(
                "InsufficientUsableFrames",
                "Video does not contain enough usable single-face frames.", 422)
        duplicate_subject, duplicate_distance = self._duplicate(encodings, subject_id)
        if duplicate_subject is not None:
            raise EnrollmentError(
                "DuplicateIdentity",
                f"Candidate conflicts with subject {duplicate_subject} "
                f"(distance {duplicate_distance:.6f}).", 409,
                duplicateSubjectId=duplicate_subject,
                duplicateDistance=duplicate_distance)

        self.config.model_staging_dir.mkdir(parents=True, exist_ok=True)
        try:
            save_template(
                candidate,
                employee_id=int(subject_id),
                version=1,
                templates=encodings,
                quality_scores=[1.0] * len(encodings),
                pose_metadata=self._pose_metadata(pose_samples),
                created_at=_utc_now(),
            )
            self._read_candidate(candidate)
        finally:
            candidate.with_suffix(".json.tmp").unlink(missing_ok=True)
        checksum = self._sha256(candidate)
        score = round(usable / processed, 6) if processed else 0.0
        result = asdict(PrepareResult(
            candidate.name, checksum, total, processed, usable, no_face,
            multiple, invalid, len(encodings), score))
        result["qualityRejections"] = quality_rejected
        return result

    def activate_candidate(self, job_id, subject_id, version,
                           expected_checksum, expected_model_file_name):
        self._validate_job_subject(job_id, subject_id)
        match = MODEL_NAME.fullmatch(expected_model_file_name or "")
        if not match or match.group(1) != subject_id or int(match.group(2)) != int(version):
            raise EnrollmentError("InvalidModelName", "Expected model name is invalid.", 400)
        candidate = self.config.model_staging_dir / f"{job_id.lower()}.json"
        active = self.config.model_dir / expected_model_file_name
        with self._lock:
            snapshot = self.registry.current_snapshot()
            existing_new = next((m for m in snapshot.model_files
                                 if m.file_name == expected_model_file_name), None)
            if existing_new and existing_new.checksum == expected_checksum:
                return self._activation_result(existing_new, snapshot.version)
            if not candidate.exists():
                raise EnrollmentError("CandidateNotFound", "Candidate does not exist.", 404)
            if self._sha256(candidate) != expected_checksum:
                raise EnrollmentError("CandidateChecksumMismatch", "Candidate checksum differs.", 409)
            encodings = self._read_candidate(candidate)
            old = next((m for m in snapshot.model_files if m.subject_id == subject_id), None)
            old_path = self.config.model_dir / old.file_name if old else None
            archive = (self.config.model_archive_dir /
                       f"{job_id.lower()}-{old.file_name}") if old else None
            if active.exists():
                raise EnrollmentError("ModelAlreadyExists", "Target model already exists.", 409)
            try:
                if old_path:
                    os.replace(old_path, archive)
                os.replace(candidate, active)
                reload_result = self.registry.reload()
                if not reload_result.success:
                    raise EnrollmentError("RegistryReloadFailed", "Registry rejected activation.", 500)
                descriptor = next(m for m in reload_result.current_snapshot.model_files
                                  if m.file_name == expected_model_file_name)
                return self._activation_result(descriptor, reload_result.current_snapshot.version)
            except Exception:
                if active.exists():
                    os.replace(active, candidate)
                if archive and archive.exists() and old_path:
                    os.replace(archive, old_path)
                self.registry.reload()
                raise

    def discard_candidate(self, job_id: str):
        if not JOB_ID.fullmatch(job_id or ""):
            raise EnrollmentError("InvalidJobId", "Job ID is invalid.", 400)
        (self.config.model_staging_dir / f"{job_id.lower()}.json").unlink(missing_ok=True)
        return {"success": True}

    def enroll_from_images(self, subject_id: str, images) -> dict:
        """Enroll a subject directly from a batch of face images (base64/bytes).

        Reuses the SFace embedding format (128-d cosine) produced by the
        recognition pipeline. The JSON template is written atomically into the
        active model directory and the registry is reloaded immediately.
        """
        if not SUBJECT_ID.fullmatch(subject_id or ""):
            raise EnrollmentError("InvalidSubjectId", "Subject ID is invalid.", 400)
        if not isinstance(images, (list, tuple)) or not images:
            raise EnrollmentError("InvalidImages", "Image batch is required.", 400)
        if len(images) > 200:
            raise EnrollmentError("InvalidImages", "Image batch is too large.", 400)

        encodings: list[np.ndarray] = []
        pose_samples: list[dict] = []
        no_face = multiple = invalid = 0
        quality_rejected: dict[str, int] = {}
        for raw in images:
            try:
                img = self._decode_image(raw)
            except Exception:
                invalid += 1
                continue
            samples = self._detect_and_embed(img)
            if not samples:
                no_face += 1
                continue
            sample = samples[0]
            value = np.asarray(sample["vector"], dtype=np.float64)
            if value.shape != (128,) or not np.all(np.isfinite(value)):
                invalid += 1
                continue
            quality = self.quality_gate.evaluate(
                img, sample["bbox"], sample["landmarks"]
            )
            if not quality.passed:
                for reason in quality.reasons:
                    quality_rejected[reason] = quality_rejected.get(reason, 0) + 1
                invalid += 1
                continue
            encodings.append(value)
            if sample["pose"] is not None:
                pose_samples.append(sample["pose"])

        if len(encodings) < self.config.enrollment_min_encodings:
            raise EnrollmentError(
                "InsufficientUsableFrames",
                f"Only {len(encodings)} usable single-face samples; need at least "
                f"{self.config.enrollment_min_encodings}. Add more/clearer face frames.",
                422,
                encodingCount=len(encodings),
                noFaceCount=no_face,
                multipleFaceCount=multiple,
                invalidFrameCount=invalid)

        duplicate_subject, duplicate_distance = self._duplicate(encodings, subject_id)
        if duplicate_subject is not None:
            raise EnrollmentError(
                "DuplicateIdentity",
                f"Face matches existing subject {duplicate_subject} "
                f"(distance {duplicate_distance:.6f}).",
                409,
                duplicateSubjectId=duplicate_subject,
                duplicateDistance=duplicate_distance)

        with self._lock:
            snapshot = self.registry.current_snapshot()
            old = next((m for m in snapshot.model_files if m.subject_id == subject_id), None)
            old_path = self.config.model_dir / old.file_name if old else None
            archive = (self.config.model_archive_dir / f"live-{old.file_name}") if old else None

            file_name = self._next_model_name(subject_id, encodings)
            active = self.config.model_dir / file_name
            if active.exists():
                raise EnrollmentError(
                    "ModelAlreadyExists", "Target model already exists.", 409)
            try:
                save_template(
                    active,
                    employee_id=int(subject_id),
                    version=self._model_version_from_name(file_name),
                    templates=encodings,
                    quality_scores=[1.0] * len(encodings),
                    pose_metadata=self._pose_metadata(pose_samples),
                    created_at=_utc_now(),
                )
                if old_path:
                    os.replace(old_path, archive)
                reload_result = self.registry.reload()
                if not reload_result.success:
                    raise EnrollmentError(
                        "RegistryReloadFailed", "Registry rejected activation.", 500)
                descriptor = next(
                    (m for m in reload_result.current_snapshot.model_files
                     if m.file_name == file_name),
                    None)
                if descriptor is None:
                    raise EnrollmentError(
                        "RegistryReloadFailed", "Registry rejected activation.", 500)
                return {
                    **self._activation_result(descriptor, reload_result.current_snapshot.version),
                    "encodingCount": len(encodings),
                    "usedFrameCount": len(encodings),
                    "noFaceFrameCount": no_face,
                    "multipleFaceFrameCount": multiple,
                    "invalidFrameCount": invalid,
                    "message": "Live enrollment activated.",
                }
            except Exception:
                if active.exists():
                    active.unlink()
                if archive and archive.exists() and old_path:
                    os.replace(archive, old_path)
                self.registry.reload()
                raise

    def activate_live_model(
        self,
        subject_id: str,
        encodings: list[np.ndarray],
        pose_metadata: dict | None = None,
    ) -> dict:
        """Activate a captured multi-pose template for a subject.

        Used by the guided enrollment flow: writes a JSON template into the
        active model directory and reloads the registry atomically.
        """
        if not SUBJECT_ID.fullmatch(subject_id or ""):
            raise EnrollmentError(
                "InvalidSubjectId",
                "Mã đối tượng không hợp lệ. Vui lòng nhập mã số (không bắt đầu bằng số 0).",
                400)
        if not isinstance(encodings, (list, tuple)) or not encodings:
            raise EnrollmentError("InvalidImages", "Template vectors are required.", 400)

        cleaned: list[np.ndarray] = []
        for value in encodings:
            array = np.asarray(value, dtype=np.float64)
            if array.shape != (128,) or not np.all(np.isfinite(array)):
                raise EnrollmentError("InvalidImages", "Template vector is invalid.", 400)
            cleaned.append(array)

        if len(cleaned) < self.config.enrollment_min_encodings:
            raise EnrollmentError(
                "InsufficientUsableFrames",
                f"Only {len(cleaned)} usable samples; need at least "
                f"{self.config.enrollment_min_encodings}.",
                422,
                encodingCount=len(cleaned))

        duplicate_subject, duplicate_distance = self._duplicate(cleaned, subject_id)
        if duplicate_subject is not None:
            raise EnrollmentError(
                "DuplicateIdentity",
                f"Face matches existing subject {duplicate_subject} "
                f"(distance {duplicate_distance:.6f}).",
                409,
                duplicateSubjectId=duplicate_subject,
                duplicateDistance=duplicate_distance)

        with self._lock:
            snapshot = self.registry.current_snapshot()
            old = next((m for m in snapshot.model_files if m.subject_id == subject_id), None)
            old_path = self.config.model_dir / old.file_name if old else None
            archive = (self.config.model_archive_dir / f"guided-{old.file_name}") if old else None

            file_name = self._next_model_name(subject_id, cleaned)
            active = self.config.model_dir / file_name
            if active.exists():
                raise EnrollmentError(
                    "ModelAlreadyExists", "Target model already exists.", 409)
            try:
                save_template(
                    active,
                    employee_id=int(subject_id),
                    version=self._model_version_from_name(file_name),
                    templates=cleaned,
                    quality_scores=[1.0] * len(cleaned),
                    pose_metadata=pose_metadata or {},
                    created_at=_utc_now(),
                )
                if old_path:
                    os.replace(old_path, archive)
                reload_result = self.registry.reload()
                if not reload_result.success:
                    raise EnrollmentError(
                        "RegistryReloadFailed", "Registry rejected activation.", 500)
                descriptor = next(
                    (m for m in reload_result.current_snapshot.model_files
                     if m.file_name == file_name),
                    None)
                if descriptor is None:
                    raise EnrollmentError(
                        "RegistryReloadFailed", "Registry rejected activation.", 500)
                return {
                    **self._activation_result(descriptor, reload_result.current_snapshot.version),
                    "encodingCount": len(cleaned),
                    "message": "Guided enrollment activated.",
                }
            except Exception:
                if active.exists():
                    active.unlink()
                if archive and archive.exists() and old_path:
                    os.replace(archive, old_path)
                self.registry.reload()
                raise

    @staticmethod
    def _pose_metadata(pose_samples: list[dict]) -> dict:
        if not pose_samples:
            return {}
        yaws = [sample["yaw"] for sample in pose_samples]
        pitches = [sample["pitch"] for sample in pose_samples]
        return {
            "yaw_range": [round(min(yaws), 2), round(max(yaws), 2)],
            "pitch_range": [round(min(pitches), 2), round(max(pitches), 2)],
            "sample_count": len(pose_samples),
        }

    def _next_model_name(self, subject_id: str, encodings: list[np.ndarray]) -> str:
        snapshot = self.registry.current_snapshot()
        current = next(
            (m for m in snapshot.model_files if m.subject_id == subject_id), None)
        next_version = 1
        if current is not None:
            match = MODEL_NAME.fullmatch(current.file_name or "")
            if match:
                next_version = int(match.group(2)) + 1
        short_hash = hashlib.sha256(
            _encodings_bytes(encodings)
        ).hexdigest()[:8]
        return f"emp_{subject_id}_v{next_version}_{short_hash}.json"

    @staticmethod
    def _model_version_from_name(file_name: str) -> int:
        match = MODEL_NAME.fullmatch(file_name or "")
        if match:
            return int(match.group(2))
        return 1

    @staticmethod
    def _decode_image(raw) -> np.ndarray:
        if isinstance(raw, str):
            header, _, body = raw.partition(",")
            if header.startswith("data:") and body:
                raw = body
            data = base64.b64decode(raw)
        elif isinstance(raw, (bytes, bytearray)):
            data = bytes(raw)
        else:
            raise ValueError("Image must be a base64 string or raw bytes")
        if not data:
            raise ValueError("Image payload is empty")
        buffer = np.frombuffer(data, dtype=np.uint8)
        image = cv2.imdecode(buffer, cv2.IMREAD_COLOR)
        if image is None:
            raise ValueError("Image could not be decoded")
        return image

    def revoke_subject_model(self, subject_id: str):
        self._validate_job_subject("0" * 32, subject_id)
        with self._lock:
            snapshot = self.registry.current_snapshot()
            model = next((m for m in snapshot.model_files if m.subject_id == subject_id), None)
            if model is None:
                raise EnrollmentError("ModelNotFound", "Active subject model was not found.", 404)
            source = self.config.model_dir / model.file_name
            archive = self.config.model_archive_dir / f"revoked-{model.file_name}"
            os.replace(source, archive)
            result = self.registry.reload()
            if not result.success:
                os.replace(archive, source)
                self.registry.reload()
                raise EnrollmentError("RegistryReloadFailed", "Registry rejected revocation.", 500)
            return {"success": True, "registryVersion": result.current_snapshot.version}

    def _source_path(self, reference: str) -> Path:
        if (not reference or "://" in reference or Path(reference).is_absolute()
                or re.match(r"^[A-Za-z]:[\\/]", reference)):
            raise EnrollmentError("InvalidSourceReference", "Source reference is invalid.", 400)
        relative = Path(reference)
        if ".." in relative.parts or relative.suffix.lower() not in VIDEO_EXTENSIONS:
            raise EnrollmentError("InvalidSourceReference", "Source reference is invalid.", 400)
        root = self.config.enrollment_input_root.resolve()
        try:
            source = (root / relative).resolve(strict=True)
            source.relative_to(root)
        except (OSError, ValueError):
            raise EnrollmentError("SourceNotFound", "Managed source video was not found.", 404)
        if not source.is_file() or source.stat().st_size > self.config.enrollment_max_video_bytes:
            raise EnrollmentError("InvalidSourceVideo", "Source video is invalid or too large.", 422)
        return source

    def _duplicate(self, candidates, subject_id):
        snapshot = self.registry.current_snapshot()
        best_subject, best = None, float("inf")
        for candidate in candidates:
            for known_subject, known in zip(snapshot.subject_ids, snapshot.encodings):
                if known_subject == subject_id:
                    continue
                if snapshot.metric == "cosine":
                    distance = cosine_distance(candidate, known)
                else:
                    distance = float(
                        np.linalg.norm(
                            np.asarray(candidate, dtype=np.float64)
                            - np.asarray(known, dtype=np.float64)
                        )
                    )
                if distance < best:
                    best_subject, best = known_subject, distance
        if best_subject is not None and best < self.config.enrollment_duplicate_threshold:
            return best_subject, best
        return None, None

    @staticmethod
    def _validate_job_subject(job_id, subject_id):
        if not JOB_ID.fullmatch(job_id or ""):
            raise EnrollmentError("InvalidJobId", "Job ID is invalid.", 400)
        if not SUBJECT_ID.fullmatch(subject_id or ""):
            raise EnrollmentError("InvalidSubjectId", "Subject ID is invalid.", 400)

    @staticmethod
    def _read_candidate(path):
        try:
            vectors, _metadata = load_templates(path)
        except TemplateStoreError as exc:
            raise EnrollmentError("CandidateInvalid", "Candidate model is invalid.", 422) from exc
        if not vectors:
            raise EnrollmentError("CandidateInvalid", "Candidate model is invalid.", 422)
        result = []
        for array in vectors:
            if array.shape != (128,) or not np.all(np.isfinite(array)):
                raise EnrollmentError("CandidateInvalid", "Candidate model is invalid.", 422)
            result.append(array)
        return result

    def _result_for_existing(self, candidate, encodings):
        return asdict(PrepareResult(
            candidate.name, self._sha256(candidate), 0, 0, len(encodings), 0,
            0, 0, len(encodings), 1.0))

    @staticmethod
    def _activation_result(descriptor, registry_version):
        return {"modelFileName": descriptor.file_name,
                "checksum": descriptor.checksum,
                "encodingCount": descriptor.encoding_count,
                "registryVersion": registry_version}

    @staticmethod
    def _sha256(path):
        return checksum_of_file(path)


def _utc_now() -> str:
    from datetime import datetime, timezone

    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def _encodings_bytes(encodings: list[np.ndarray]) -> bytes:
    return b"".join(np.asarray(vector, dtype=np.float32).tobytes() for vector in encodings)
