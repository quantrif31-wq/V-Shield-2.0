"""JSON face template storage with cosine search.

Stores multi-pose SFace templates as non-executable JSON (version 2) while
retaining read support for the legacy dlib `.pkl` format so existing models
remain loadable during the transition.
"""

from __future__ import annotations

import base64
import hashlib
import json
import os
import pickle
import re
from pathlib import Path

import numpy as np

from face_embedder import EMBEDDING_DIM

# Legacy dlib .pkl convention: emp_<subject>_v<version>_<hash>.pkl
PKL_MODEL_NAME = re.compile(r"^emp_([^_]+)_v(\d+)_[0-9a-f]+\.pkl$")
# New SFace .json convention: emp_<subject>_v<version>_<hash>.json
JSON_MODEL_NAME = re.compile(r"^emp_([^_]+)_v(\d+)_[0-9a-f]+\.json$")

TEMPLATE_FORMAT_VERSION = 2


class TemplateStoreError(RuntimeError):
    """Raised when a face template file is invalid."""


class TemplateStructureError(TemplateStoreError):
    """Raised when a template file has the wrong top-level structure."""


class TemplateEncodingError(TemplateStoreError):
    """Raised when an individual template vector is invalid."""


def _base64_to_vector(value) -> np.ndarray:
    raw = base64.b64decode(str(value), validate=True)
    array = np.frombuffer(raw, dtype=np.float32)
    if array.shape != (EMBEDDING_DIM,):
        raise TemplateStoreError("Template vector has an invalid length.")
    if not np.all(np.isfinite(array)):
        raise TemplateStoreError("Template vector contains non-finite values.")
    return array.astype(np.float32)


def _vector_to_base64(vector: np.ndarray) -> str:
    return base64.b64encode(
        np.asarray(vector, dtype=np.float32).tobytes()
    ).decode("ascii")


def _median_vector(templates: list[np.ndarray]) -> np.ndarray:
    if not templates:
        raise TemplateStoreError("Cannot compute median of an empty template set.")
    stack = np.stack(templates, axis=0)
    median = np.median(stack, axis=0)
    norm = float(np.linalg.norm(median))
    if norm > 0:
        median = median / norm
    return median.astype(np.float32)


def cosine_similarity(a: np.ndarray, b: np.ndarray) -> float:
    vector_a = np.asarray(a, dtype=np.float64).reshape(-1)
    vector_b = np.asarray(b, dtype=np.float64).reshape(-1)
    denominator = float(
        np.linalg.norm(vector_a) * np.linalg.norm(vector_b)
    )
    if denominator == 0:
        return 0.0
    return float(np.dot(vector_a, vector_b) / denominator)


def cosine_distance(a: np.ndarray, b: np.ndarray) -> float:
    """Distance in [0, 2]; 0 means identical direction."""
    return 1.0 - cosine_similarity(a, b)


def save_template(
    path: str | Path,
    *,
    employee_id: int,
    version: int,
    templates: list[np.ndarray],
    quality_scores: list[float] | None = None,
    pose_metadata: dict | None = None,
    created_at: str | None = None,
) -> dict:
    """Write a version-2 JSON template atomically and return its payload."""
    if not templates:
        raise TemplateStoreError("At least one template vector is required.")
    vectors = [np.asarray(template, dtype=np.float32) for template in templates]
    if any(vector.shape != (EMBEDDING_DIM,) for vector in vectors):
        raise TemplateStoreError("All template vectors must be 128-dimensional.")

    payload = {
        "format_version": TEMPLATE_FORMAT_VERSION,
        "employee_id": int(employee_id),
        "version": int(version),
        "embedding_dim": EMBEDDING_DIM,
        "metric": "cosine",
        "templates": [_vector_to_base64(vector) for vector in vectors],
        "median_embedding": _vector_to_base64(_median_vector(vectors)),
        "quality_scores": [float(score) for score in (quality_scores or [])],
        "pose_metadata": pose_metadata or {},
        "created_at": created_at or "",
    }

    path = Path(path)
    if not path.name.lower().endswith(".json"):
        raise TemplateStoreError("Template file must use the .json extension.")

    payload["checksum"] = _payload_checksum(payload)
    temporary = path.with_suffix(".json.tmp")
    try:
        with temporary.open("w", encoding="utf-8") as stream:
            json.dump(payload, stream, ensure_ascii=False, sort_keys=True)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)
    return payload


def load_templates(path: str | Path) -> tuple[list[np.ndarray], dict]:
    """Load template vectors from a JSON or legacy .pkl file.

    Returns ``(vectors, metadata)``. For .pkl legacy files, metadata is empty
    and vectors are the stored dlib encodings.
    """
    path = Path(path)
    if path.name.lower().endswith(".json"):
        return _load_json_templates(path)
    if path.name.lower().endswith(".pkl"):
        return _load_pkl_templates(path)
    raise TemplateStoreError("Unsupported template file extension.")


def _load_json_templates(path: Path) -> tuple[list[np.ndarray], dict]:
    try:
        with path.open("r", encoding="utf-8") as stream:
            payload = json.load(stream)
    except (OSError, ValueError) as exc:
        raise TemplateStoreError("Template JSON could not be parsed.") from exc

    if not isinstance(payload, dict):
        raise TemplateStoreError("Template JSON must be an object.")
    if payload.get("format_version") != TEMPLATE_FORMAT_VERSION:
        raise TemplateStoreError("Unsupported template format version.")
    if payload.get("embedding_dim") != EMBEDDING_DIM:
        raise TemplateStoreError("Template embedding dimension is invalid.")

    if payload.get("checksum") != _payload_checksum(payload):
        raise TemplateStoreError("Template checksum does not match its content.")

    raw_templates = payload.get("templates")
    if not isinstance(raw_templates, list) or not raw_templates:
        raise TemplateStoreError("Template contains no usable vectors.")

    vectors: list[np.ndarray] = []
    for raw in raw_templates:
        try:
            vector = _base64_to_vector(raw)
        except Exception as exc:
            raise TemplateStoreError("Template vector is invalid.") from exc
        vectors.append(vector)
    return vectors, payload


def _load_pkl_templates(path: Path) -> tuple[list[np.ndarray], dict]:
    try:
        with path.open("rb") as stream:
            raw = pickle.load(stream)
    except Exception as exc:
        raise TemplateStoreError("Legacy model file could not be loaded.") from exc
    if not isinstance(raw, (list, tuple)) or not raw:
        raise TemplateStructureError("Legacy model file contains no encodings.")
    vectors: list[np.ndarray] = []
    for value in raw:
        array = np.asarray(value, dtype=np.float32)
        if (
            array.ndim != 1
            or array.shape[0] != EMBEDDING_DIM
            or not np.all(np.isfinite(array))
        ):
            raise TemplateEncodingError(
                "Legacy model contains an invalid encoding."
            )
        vectors.append(array)
    return vectors, {}


def _payload_checksum(payload: dict) -> str:
    digest = hashlib.sha256()
    for key in sorted(payload):
        if key == "checksum":
            continue
        digest.update(key.encode("utf-8"))
        digest.update(b":")
        digest.update(json.dumps(payload[key], sort_keys=True).encode("utf-8"))
    return digest.hexdigest()


def checksum_of_bytes(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def checksum_of_file(path: str | Path) -> str:
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()
