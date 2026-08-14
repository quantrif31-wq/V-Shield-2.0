FROM nvidia/cuda:11.8.0-cudnn8-runtime-ubuntu22.04

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    python3 \
    python3-pip \
    python3-dev \
    ffmpeg \
    libglib2.0-0 \
    libjpeg-dev \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt ./
RUN python3 -m pip install --no-cache-dir -r requirements.txt \
    && python3 -m pip install --no-cache-dir --no-deps mediapipe==0.10.14 \
    && python3 -m pip install --no-cache-dir 'protobuf<5' matplotlib absl-py attrs flatbuffers sounddevice typer wrapt

COPY nhandienface.py ./
COPY runtime_config.py ./
COPY model_registry.py ./
COPY enrollment_service.py ./
COPY camera_session.py ./
COPY camera_manager.py ./
COPY face_detector.py ./
COPY face_embedder.py ./
COPY face_quality.py ./
COPY pose_guide.py ./
COPY landmark_service.py ./
COPY guided_enrollment.py ./
COPY template_store.py ./

EXPOSE 5001
