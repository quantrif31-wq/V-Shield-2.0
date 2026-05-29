FROM python:3.11-slim

WORKDIR /app

RUN apt-get update && apt-get install -y \
    libzbar0 \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender1 \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY QR_Dong.py .

EXPOSE 8001
CMD ["python", "QR_Dong.py"]
