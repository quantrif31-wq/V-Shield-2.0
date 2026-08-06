FROM python:3.10-slim-bullseye

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    cmake \
    ffmpeg \
    libatlas-base-dev \
    libgl1 \
    libglib2.0-0 \
    libjpeg-dev \
    liblapack-dev \
    libsm6 \
    libxext6 \
    libxrender1 \
    python3-dev \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt ./
RUN sed 's/^opencv-python==/opencv-python-headless==/' requirements.txt > requirements.docker.txt \
    && pip install --no-cache-dir -r requirements.docker.txt

COPY nhandienface.py ./
COPY runtime_config.py ./
COPY model_registry.py ./
COPY enrollment_service.py ./
COPY camera_session.py ./
COPY camera_manager.py ./

EXPOSE 5001
