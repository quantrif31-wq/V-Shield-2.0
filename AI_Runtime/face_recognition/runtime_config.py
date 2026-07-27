"""Centralized, validated configuration for the Face ID Flask runtime."""

from __future__ import annotations

import math
import os
from dataclasses import dataclass, field
from pathlib import Path
from typing import Mapping


DEFAULT_THRESHOLD = 0.35
DEFAULT_CONFIRM_FRAMES = 5
DEFAULT_LOST_TIMEOUT = 2.0
DEFAULT_ENCODE_INTERVAL = 0.7
DEFAULT_FRAME_WIDTH = 480
DEFAULT_ROTATION = -90
DEFAULT_RECOGNIZE_TIMEOUT = 5.0
DEFAULT_ALERT_TIMEOUT = 8.0
DEFAULT_STREAM_WIDTH = 640
DEFAULT_STREAM_HEIGHT = 360
DEFAULT_JPEG_QUALITY = 80
DEFAULT_API_PORT = 5001
DEFAULT_HEADLESS_MODE = True
DEFAULT_MAX_CAMERAS = 2


class FaceRuntimeConfigError(ValueError):
    """Raised when a Face Runtime environment value is invalid."""


def repository_root(module_file: str | Path = __file__) -> Path:
    """Resolve the repository root from this module, independent of cwd/name."""
    path = Path(module_file).resolve()
    try:
        return path.parents[2]
    except IndexError as exc:
        raise FaceRuntimeConfigError(
            f"Cannot resolve repository root from module path: {path}"
        ) from exc


def default_model_dir(module_file: str | Path = __file__) -> Path:
    return repository_root(module_file) / "API" / "API" / "API" / "wwwroot" / "uploads" / "VideoFace" / "FaceID"


def _parse_int(
    env: Mapping[str, str],
    name: str,
    default: int,
    *,
    minimum: int | None = None,
    maximum: int | None = None,
) -> int:
    raw = env.get(name)
    if raw is None or not raw.strip():
        value = default
    else:
        try:
            value = int(raw.strip())
        except ValueError as exc:
            raise FaceRuntimeConfigError(f"{name} must be a valid integer") from exc

    if minimum is not None and value < minimum:
        raise FaceRuntimeConfigError(f"{name} must be greater than or equal to {minimum}")
    if maximum is not None and value > maximum:
        raise FaceRuntimeConfigError(f"{name} must be less than or equal to {maximum}")
    return value


def _parse_float(
    env: Mapping[str, str],
    name: str,
    default: float,
    *,
    minimum: float | None = None,
) -> float:
    raw = env.get(name)
    if raw is None or not raw.strip():
        value = default
    else:
        try:
            value = float(raw.strip())
        except ValueError as exc:
            raise FaceRuntimeConfigError(f"{name} must be a valid number") from exc

    if not math.isfinite(value):
        raise FaceRuntimeConfigError(f"{name} must be a finite number")
    if minimum is not None and value < minimum:
        raise FaceRuntimeConfigError(f"{name} must be greater than or equal to {minimum}")
    return value


def _parse_bool(
    env: Mapping[str, str],
    name: str,
    default: bool,
) -> bool:
    raw = env.get(name)
    if raw is None or not raw.strip():
        return default

    normalized = raw.strip().lower()
    if normalized in {"1", "true", "yes", "on"}:
        return True
    if normalized in {"0", "false", "no", "off"}:
        return False
    raise FaceRuntimeConfigError(
        f"{name} must be one of: 1, 0, true, false, yes, no, on, off"
    )


def _parse_path(
    env: Mapping[str, str],
    name: str,
    default: Path | None,
    *,
    base_dir: Path,
) -> Path | None:
    raw = env.get(name)
    if raw is None or not raw.strip():
        return default.resolve() if default is not None else None

    path = Path(raw.strip()).expanduser()
    if not path.is_absolute():
        path = base_dir / path
    return path.resolve()


@dataclass(frozen=True)
class FaceRuntimeConfig:
    model_dir: Path
    snapshot_dir: Path | None
    threshold: float
    confirm_frames: int
    lost_timeout: float
    encode_interval: float
    frame_width: int
    rotation: int
    recognize_timeout: float
    alert_timeout: float
    stream_width: int
    stream_height: int
    jpeg_quality: int
    max_cameras: int = DEFAULT_MAX_CAMERAS
    service_token: str | None = field(default=None, repr=False)
    api_port: int = DEFAULT_API_PORT
    headless_mode: bool = DEFAULT_HEADLESS_MODE

    @classmethod
    def from_env(
        cls,
        env: Mapping[str, str] | None = None,
        *,
        module_file: str | Path = __file__,
    ) -> "FaceRuntimeConfig":
        source = os.environ if env is None else env
        repo_root = repository_root(module_file)
        token = source.get("FACE_SERVICE_TOKEN")

        return cls(
            model_dir=_parse_path(
                source,
                "FACE_MODEL_DIR",
                default_model_dir(module_file),
                base_dir=repo_root,
            ),
            # Snapshots are currently held in memory as Base64. The optional
            # path is parsed now but intentionally not used for filesystem I/O.
            snapshot_dir=_parse_path(
                source,
                "FACE_SNAPSHOT_DIR",
                None,
                base_dir=repo_root,
            ),
            threshold=_parse_float(
                source, "FACE_THRESHOLD", DEFAULT_THRESHOLD, minimum=0.0
            ),
            confirm_frames=_parse_int(
                source, "FACE_CONFIRM_FRAMES", DEFAULT_CONFIRM_FRAMES, minimum=1
            ),
            lost_timeout=_parse_float(
                source, "FACE_LOST_TIMEOUT", DEFAULT_LOST_TIMEOUT, minimum=0.0
            ),
            encode_interval=_parse_float(
                source, "FACE_ENCODE_INTERVAL", DEFAULT_ENCODE_INTERVAL, minimum=0.0
            ),
            frame_width=_parse_int(
                source, "FACE_FRAME_WIDTH", DEFAULT_FRAME_WIDTH, minimum=1
            ),
            rotation=_parse_int(source, "FACE_ROTATION", DEFAULT_ROTATION),
            recognize_timeout=_parse_float(
                source,
                "FACE_RECOGNIZE_TIMEOUT",
                DEFAULT_RECOGNIZE_TIMEOUT,
                minimum=0.0,
            ),
            alert_timeout=_parse_float(
                source, "FACE_ALERT_TIMEOUT", DEFAULT_ALERT_TIMEOUT, minimum=0.0
            ),
            stream_width=_parse_int(
                source, "FACE_STREAM_WIDTH", DEFAULT_STREAM_WIDTH, minimum=1
            ),
            stream_height=_parse_int(
                source, "FACE_STREAM_HEIGHT", DEFAULT_STREAM_HEIGHT, minimum=1
            ),
            jpeg_quality=_parse_int(
                source,
                "FACE_JPEG_QUALITY",
                DEFAULT_JPEG_QUALITY,
                minimum=0,
                maximum=100,
            ),
            max_cameras=_parse_int(
                source, "FACE_MAX_CAMERAS", DEFAULT_MAX_CAMERAS, minimum=1
            ),
            service_token=token if token else None,
            api_port=_parse_int(source, "PORT", DEFAULT_API_PORT, minimum=1, maximum=65535),
            headless_mode=_parse_bool(source, "HEADLESS_MODE", DEFAULT_HEADLESS_MODE),
        )
