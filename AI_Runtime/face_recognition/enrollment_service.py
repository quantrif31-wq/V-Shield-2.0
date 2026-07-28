"""Controlled, filesystem-safe face enrollment lifecycle."""

from __future__ import annotations

import hashlib
import os
import pickle
import re
import threading
from dataclasses import asdict, dataclass
from pathlib import Path

import cv2
import face_recognition
import numpy as np


JOB_ID = re.compile(r"^[0-9a-fA-F-]{32,36}$")
SUBJECT_ID = re.compile(r"^[1-9][0-9]{0,9}$")
MODEL_NAME = re.compile(r"^emp_([1-9][0-9]{0,9})_v([1-9][0-9]*)_([0-9a-f]{8})\.pkl$")
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
    def __init__(self, config, registry):
        self.config = config
        self.registry = registry
        self._lock = threading.RLock()

    def prepare_enrollment(self, job_id: str, subject_id: str, source_reference: str):
        self._validate_job_subject(job_id, subject_id)
        source = self._source_path(source_reference)
        candidate = self.config.model_staging_dir / f"{job_id.lower()}.pkl"
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
                rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                locations = face_recognition.face_locations(rgb, model="hog")
                if not locations:
                    no_face += 1
                    continue
                if len(locations) != 1:
                    multiple += 1
                    continue
                values = face_recognition.face_encodings(rgb, locations)
                if len(values) != 1:
                    invalid += 1
                    continue
                value = np.asarray(values[0], dtype=np.float64)
                if value.shape != (128,) or not np.all(np.isfinite(value)):
                    invalid += 1
                    continue
                encodings.append(value)
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
        temporary = candidate.with_suffix(".tmp")
        try:
            with temporary.open("xb") as stream:
                pickle.dump(encodings, stream, protocol=pickle.HIGHEST_PROTOCOL)
                stream.flush()
                os.fsync(stream.fileno())
            self._read_candidate(temporary)
            os.replace(temporary, candidate)
        finally:
            temporary.unlink(missing_ok=True)
        checksum = self._sha256(candidate)
        score = round(usable / processed, 6) if processed else 0.0
        return asdict(PrepareResult(
            candidate.name, checksum, total, processed, usable, no_face,
            multiple, invalid, len(encodings), score))

    def activate_candidate(self, job_id, subject_id, version,
                           expected_checksum, expected_model_file_name):
        self._validate_job_subject(job_id, subject_id)
        match = MODEL_NAME.fullmatch(expected_model_file_name or "")
        if not match or match.group(1) != subject_id or int(match.group(2)) != int(version):
            raise EnrollmentError("InvalidModelName", "Expected model name is invalid.", 400)
        candidate = self.config.model_staging_dir / f"{job_id.lower()}.pkl"
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
        (self.config.model_staging_dir / f"{job_id.lower()}.pkl").unlink(missing_ok=True)
        return {"success": True}

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
                distance = float(np.linalg.norm(candidate - known))
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
        with path.open("rb") as stream:
            values = pickle.load(stream)
        if not isinstance(values, (list, tuple)) or not values:
            raise EnrollmentError("CandidateInvalid", "Candidate model is invalid.", 422)
        result = []
        for value in values:
            array = np.asarray(value)
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
        digest = hashlib.sha256()
        with path.open("rb") as stream:
            for chunk in iter(lambda: stream.read(1024 * 1024), b""):
                digest.update(chunk)
        return digest.hexdigest()
