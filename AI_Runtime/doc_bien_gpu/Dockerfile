FROM python:3.10-slim

WORKDIR /app
ENV LPR_HEADLESS=1

RUN apt-get update && apt-get install -y \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender1 \
    ffmpeg \
    libgl1 \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.docker.txt .

# NOTE:
# requirements.docker.txt da loai bo cac dependency GPU (+cu118)
# de profile "ai-heavy" co the build tren moi truong Docker CPU.
RUN pip install --no-cache-dir --default-timeout=1000 --retries 10 -r requirements.docker.txt

COPY docbien.py .
COPY best.pt ./best.pt

EXPOSE 5002
CMD ["python", "docbien.py"]
