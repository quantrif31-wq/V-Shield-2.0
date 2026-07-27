"""Thread-safe immutable model snapshots for the Face ID runtime."""

from __future__ import annotations

import pickle
import threading
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path

import numpy as np


@dataclass(frozen=True)
class ModelDescriptor:
    file_name: str
    subject_id: str
    encoding_count: int


@dataclass(frozen=True)
class ModelLoadError:
    file_name: str
    error_code: str
    message: str


@dataclass(frozen=True)
class RegistrySnapshot:
    version: int
    loaded_at: datetime
    model_directory: Path
    model_files: tuple[ModelDescriptor, ...]
    subject_ids: tuple[str, ...]
    encodings: tuple[np.ndarray, ...] = field(repr=False)
    successful_file_count: int = 0
    encoding_count: int = 0
    errors: tuple[ModelLoadError, ...] = ()


@dataclass(frozen=True)
class ReloadResult:
    success: bool
    previous_version: int
    current_snapshot: RegistrySnapshot
    errors: tuple[ModelLoadError, ...] = ()
    error_code: str | None = None


class FaceModelRegistry:
    """Publishes complete model snapshots without mutating active readers."""

    def __init__(self, model_directory: str | Path):
        self._model_directory = Path(model_directory).resolve()
        self._swap_lock = threading.Lock()
        self._reload_lock = threading.Lock()
        initial = self.build_snapshot(version=1)
        self._snapshot = initial

    @property
    def model_directory(self) -> Path:
        return self._model_directory

    def current_snapshot(self) -> RegistrySnapshot:
        # Object reference reads are atomic in CPython. The referenced snapshot
        # and its collections are immutable, so readers require no lock.
        return self._snapshot

    def build_snapshot(self, *, version: int) -> RegistrySnapshot:
        descriptors: list[ModelDescriptor] = []
        subject_ids: list[str] = []
        encodings: list[np.ndarray] = []
        errors: list[ModelLoadError] = []

        if not self._model_directory.is_dir():
            errors.append(
                ModelLoadError(
                    file_name="",
                    error_code="MODEL_DIRECTORY_NOT_FOUND",
                    message="Configured model directory does not exist.",
                )
            )
            return self._make_snapshot(
                version, descriptors, subject_ids, encodings, errors
            )

        model_paths = sorted(
            (
                path
                for path in self._model_directory.iterdir()
                if path.name.lower().endswith(".pkl")
            ),
            key=lambda path: path.name,
        )

        for model_path in model_paths:
            safe_path = self._validated_model_path(model_path)
            if safe_path is None:
                errors.append(
                    ModelLoadError(
                        file_name=model_path.name,
                        error_code="UNSAFE_MODEL_PATH",
                        message="Model file resolves outside the configured model directory.",
                    )
                )
                continue

            try:
                with safe_path.open("rb") as model_stream:
                    raw_encodings = pickle.load(model_stream)
            except Exception:
                errors.append(
                    ModelLoadError(
                        file_name=model_path.name,
                        error_code="MODEL_DESERIALIZATION_FAILED",
                        message="Model file could not be loaded.",
                    )
                )
                continue

            if not isinstance(raw_encodings, (list, tuple)):
                errors.append(
                    ModelLoadError(
                        file_name=model_path.name,
                        error_code="MODEL_STRUCTURE_INVALID",
                        message="Model file must contain a list or tuple of encodings.",
                    )
                )
                continue

            immutable_encodings: list[np.ndarray] = []
            invalid_encoding = False
            for raw_encoding in raw_encodings:
                try:
                    encoding = np.asarray(raw_encoding)
                    if (
                        encoding.ndim != 1
                        or encoding.shape[0] != 128
                        or not np.issubdtype(encoding.dtype, np.number)
                        or not np.all(np.isfinite(encoding))
                    ):
                        raise ValueError("invalid encoding")
                    contiguous = np.ascontiguousarray(encoding)
                    # A bytes-backed array cannot be made writable by callers.
                    immutable = np.frombuffer(
                        contiguous.tobytes(),
                        dtype=contiguous.dtype,
                    )
                    immutable_encodings.append(immutable)
                except (TypeError, ValueError):
                    invalid_encoding = True
                    break

            if invalid_encoding:
                errors.append(
                    ModelLoadError(
                        file_name=model_path.name,
                        error_code="MODEL_ENCODING_INVALID",
                        message="Model file contains an invalid face encoding.",
                    )
                )
                continue

            subject_id = self._subject_id_from_path(model_path)
            descriptors.append(
                ModelDescriptor(
                    file_name=model_path.name,
                    subject_id=subject_id,
                    encoding_count=len(immutable_encodings),
                )
            )
            encodings.extend(immutable_encodings)
            subject_ids.extend(subject_id for _ in immutable_encodings)

        return self._make_snapshot(
            version, descriptors, subject_ids, encodings, errors
        )

    def reload(self) -> ReloadResult:
        if not self._reload_lock.acquire(blocking=False):
            current = self.current_snapshot()
            return ReloadResult(
                success=False,
                previous_version=current.version,
                current_snapshot=current,
                error_code="RELOAD_IN_PROGRESS",
            )

        try:
            previous = self.current_snapshot()
            candidate = self.build_snapshot(version=previous.version + 1)
            if candidate.errors:
                return ReloadResult(
                    success=False,
                    previous_version=previous.version,
                    current_snapshot=previous,
                    errors=candidate.errors,
                    error_code="MODEL_RELOAD_REJECTED",
                )

            with self._swap_lock:
                # Only one reload can build at a time, so the version remains
                # based on the snapshot that was active before this build.
                self._snapshot = candidate

            return ReloadResult(
                success=True,
                previous_version=previous.version,
                current_snapshot=candidate,
            )
        finally:
            self._reload_lock.release()

    def _validated_model_path(self, model_path: Path) -> Path | None:
        try:
            resolved = model_path.resolve(strict=True)
            resolved.relative_to(self._model_directory)
        except (OSError, ValueError):
            return None

        if not resolved.is_file() or resolved.suffix.lower() != ".pkl":
            return None
        return resolved

    @staticmethod
    def _subject_id_from_path(model_path: Path) -> str:
        parts = model_path.stem.split("_")
        return parts[1] if len(parts) > 1 else model_path.stem

    def _make_snapshot(
        self,
        version: int,
        descriptors: list[ModelDescriptor],
        subject_ids: list[str],
        encodings: list[np.ndarray],
        errors: list[ModelLoadError],
    ) -> RegistrySnapshot:
        return RegistrySnapshot(
            version=version,
            loaded_at=datetime.now(timezone.utc),
            model_directory=self._model_directory,
            model_files=tuple(descriptors),
            subject_ids=tuple(subject_ids),
            encodings=tuple(encodings),
            successful_file_count=len(descriptors),
            encoding_count=len(encodings),
            errors=tuple(errors),
        )
