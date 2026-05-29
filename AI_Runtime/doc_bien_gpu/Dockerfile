FROM python:3.10-slim

WORKDIR /app

RUN apt-get update && apt-get install -y \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender1 \
    ffmpeg \
    libgl1 \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .

# NOTE:
# requirements hien tai co paddle/torch ban GPU.
# Service nay duoc dua vao profile "ai-heavy" de tranh bat buoc build trong lan chay docker dau tien.
RUN pip install --no-cache-dir -r requirements.txt

COPY docbien.py .
COPY best.pt ./best.pt

EXPOSE 5002
CMD ["python", "docbien.py"]
