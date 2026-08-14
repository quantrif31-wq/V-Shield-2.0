"""SFace face embedding via ONNX Runtime (GPU preferred, CPU fallback).

Wraps the SFace ONNX model from OpenCV Zoo. Runs through ONNX Runtime using
CUDA when available and falls back to CPU otherwise, so the same code works on
GPU (RTX 3050/3060) and CPU machines.
"""

from __future__ import annotations

from pathlib import Path

import numpy as np


# InsightFace/SFace reference landmarks for 112x112 alignment.
REFERENCE_LANDMARKS = np.array(
    [
        [38.2946, 51.6963],
        [73.5318, 51.5014],
        [56.0252, 71.7366],
        [41.5493, 92.3655],
        [70.7299, 92.2041],
    ],
    dtype=np.float64,
)

INPUT_SIZE = (112, 112)
EMBEDDING_DIM = 128


class FaceEmbedderError(RuntimeError):
    """Raised when the SFace embedder cannot be initialized or run."""


class FaceEmbedder:
    """Produces 128-d L2-normalized SFace embeddings for aligned faces."""

    def __init__(
        self,
        model_path: str | Path,
        *,
        prefer_gpu: bool = True,
        gpu_device_id: int = 0,
    ) -> None:
        model_path = str(model_path)
        if not Path(model_path).is_file():
            raise FaceEmbedderError(
                "SFace model file was not found: %s" % model_path
            )

        import onnxruntime as ort

        self._backend = "cpu"
        providers = ["CPUExecutionProvider"]
        if prefer_gpu:
            gpu_providers = [
                ("CUDAExecutionProvider", {"device_id": int(gpu_device_id)}),
                "CPUExecutionProvider",
            ]
            try:
                self._session = ort.InferenceSession(
                    model_path, providers=gpu_providers
                )
            except Exception:
                try:
                    self._session = ort.InferenceSession(
                        model_path, providers=providers
                    )
                except Exception as exc:
                    raise FaceEmbedderError(
                        "SFace model could not be loaded: %s" % exc
                    ) from exc
            if "CUDAExecutionProvider" in self._session.get_providers():
                self._backend = "gpu"
        else:
            try:
                self._session = ort.InferenceSession(
                    model_path, providers=providers
                )
            except Exception as exc:
                raise FaceEmbedderError(
                    "SFace model could not be loaded: %s" % exc
                ) from exc

        self._input_name = self._session.get_inputs()[0].name
        self._output_name = self._session.get_outputs()[0].name

    @property
    def backend(self) -> str:
        return self._backend

    def embed_aligned(self, aligned: np.ndarray) -> np.ndarray:
        """Embed a pre-aligned 112x112 face.

        Returns a finite, L2-normalized 128-d float32 vector.
        """
        if aligned.shape[:2] != INPUT_SIZE:
            aligned = self.resize(aligned)
        # This SFace ONNX graph already contains its input normalization
        # (mean/scale) as internal layers, so the raw 0..255 BGR tensor is fed
        # directly as NCHW float32. Normalizing again here collapses the
        # embeddings and destroys discrimination between people.
        tensor = aligned.transpose(2, 0, 1).astype(np.float32)
        tensor = np.expand_dims(tensor, axis=0)
        output = self._session.run(
            [self._output_name], {self._input_name: tensor}
        )[0]
        vector = np.asarray(output, dtype=np.float64).reshape(-1)
        norm = float(np.linalg.norm(vector))
        if norm > 0:
            vector = vector / norm
        if vector.shape != (EMBEDDING_DIM,) or not np.all(np.isfinite(vector)):
            raise FaceEmbedderError("SFace produced an invalid embedding.")
        return vector.astype(np.float32)

    def align_and_embed(
        self, frame: np.ndarray, landmarks: np.ndarray
    ) -> np.ndarray:
        """Warp a face using five landmarks, then embed it."""
        aligned = self.align(frame, landmarks)
        return self.embed_aligned(aligned)

    @staticmethod
    def align(frame: np.ndarray, landmarks: np.ndarray) -> np.ndarray:
        """Return a 112x112 BGR crop aligned to the reference landmarks."""
        import cv2

        source = np.asarray(landmarks, dtype=np.float64).reshape(5, 2)
        transform, _ = cv2.estimateAffinePartial2D(
            source, REFERENCE_LANDMARKS, method=cv2.LMEDS
        )
        if transform is None:
            transform = cv2.estimateAffinePartial2D(
                source, REFERENCE_LANDMARKS
            )[0]
        aligned = cv2.warpAffine(
            frame,
            transform,
            INPUT_SIZE,
            flags=cv2.INTER_LINEAR,
            borderMode=cv2.BORDER_CONSTANT,
        )
        return aligned

    @staticmethod
    def resize(aligned: np.ndarray) -> np.ndarray:
        import cv2

        return cv2.resize(aligned, INPUT_SIZE, interpolation=cv2.INTER_LINEAR)
