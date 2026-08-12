# KẾ HOẠCH CẢI TẠO FACE ID — PHƯƠNG ÁN 1 (Commercial-Safe 100%)

> **Mục đích file này:** Kế hoạch nâng cấp hệ thống nhận diện khuôn mặt của V-Shield từ
> `face_recognition` (dlib) sang **YuNet + SFace (OpenCV Zoo)** — mã nguồn mở, miễn phí,
> **sạch 100% cho thương mại**, tận dụng **GPU RTX 3060**, chạy với **camera thường**.
> Mang file này qua máy có RTX 3060 để thực hiện.

- **Ngày:** 2026-08
- **Trạng thái:** Kế hoạch (chưa code)
- **Stack:** YuNet (MIT) + MediaPipe (Apache-2.0) + eDifFIQA (CC-BY-4.0) + **SFace (Apache-2.0)** + Silent-Face-Anti-Spoofing (Apache-2.0)

---

## 1. TỔNG QUAN

### 1.1 Hiện tại (cần thay)
| Thành phần | Công nghệ cũ | Vấn đề |
|---|---|---|
| Detector | dlib HOG (`face_recognition`) | Kém góc nghiêng >30°, thiếu sáng, xa >2m |
| Embedding | dlib ResNet 128-d | Độ chính xác trung bình (~96% LFW thực tế) |
| Metric | Euclid distance, ngưỡng 0.6 | Không tối ưu |
| Tốc độ | <10 FPS CPU | Chậm |
| Enrollment | Video, tự xử lý (dlib) | Không có hướng dẫn pose, không kiểm tra góc đủ |

### 1.2 Mục tiêu (Phương án 1)
- **Detector:** YuNet (OpenCV Zoo) — MIT, bắt mặt tốt 10–300px, kèm 5 landmark
- **Embedding:** SFace (OpenCV Zoo) — Apache-2.0, 128-d, cosine
- **Pose hướng dẫn quay đầu:** MediaPipe FaceLandmarker — Apache-2.0 (478 điểm 3D)
- **Chất lượng khung:** heuristic + eDifFIQA — CC-BY-4.0
- **Liveness:** Silent-Face-Anti-Spoofing — Apache-2.0
- **GPU:** ONNX Runtime CUDA trên RTX 3060
- **Yêu cầu thương mại:** ✅ Toàn bộ license MIT/Apache-2.0/CC-BY-4.0 → không cần xin phép

### 1.3 Nguyên tắc
- Giữ nguyên kiến trúc hiện có: **face-runtime (Python)** + **worker ASP.NET** + API contract
- Chỉ thay lõi `detect + embedding` và thêm module `pose/quality/liveness`
- Không đổi database model, không đổi API public
- Camera thường (webcam / IP RTSP), chạy GPU khi có RTX 3060, fallback CPU khi không có GPU

---

## 2. KIẾN TRÚC MỚI

### 2.1 Sơ đồ tổng thể
```
┌─────────────────────────────────────────────────────────────────────┐
│  FACE-RUNTIME (Python, container GPU)                                │
│                                                                      │
│  ┌─────────────┐   ┌──────────────┐   ┌───────────────────────────┐  │
│  │ CAPTURE     │ → │ DETECT+TRACK │ → │ POSE + QUALITY GATE       │  │
│  │ cv2/FFmpeg  │   │ YuNet + CSRT │   │ MediaPipe + eDifFIQA      │  │
│  │ (cam RTSP)  │   │ (ONNX CUDA)  │   │ (pose để hướng dẫn quay)  │  │
│  └─────────────┘   └──────┬───────┘   └────────────┬──────────────┘  │
│                           │                        │                 │
│                           ▼                        ▼                 │
│  ┌──────────────┐  ┌───────────────┐  ┌───────────────────────────┐  │
│  │ ALIGN 5-pt   │ →│ EMBEDDING     │→ │ AGGREGATE + MATCH         │  │
│  │ 112x112 warp │  │ SFace (ONNX)  │  │ cosine + FAISS (tùy chọn) │  │
│  └──────────────┘  └───────────────┘  └───────────────────────────┘  │
│                           │                    │                     │
│                           ▼                    ▼                     │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │  LIVENESS (Silent-FAS)  →  QUYẾT ĐỊNH (ok/unknown/alert)     │    │
│  └──────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
            │                          │
            ▼                          ▼
   ENROLLMENT WORKER            RECOGNITION API
   (tạo template cá nhân)       (quét realtime cổng)
```

### 2.2 Thành phần thư viện (toàn bộ free + commercial-safe)

| Thành phần | Model/Thư viện | License | Kích thước | Chạy GPU RTX 3060 |
|---|---|---|---|---|
| Face detector + 5 landmark | **YuNet** `face_detection_yunet_2023mar.onnx` | MIT | ~2 MB | ONNX Runtime CUDA |
| Face tracking | OpenCV CSRT / ByteTrack | Apache-2.0 / MIT | — | CPU (giải phóng GPU) |
| Pose (quay đầu) | **MediaPipe FaceLandmarker** | Apache-2.0 | ~4 MB | CPU realtime |
| Chất lượng khung | heuristic + **eDifFIQA(T)** | CC-BY-4.0 | ~5 MB | ONNX CUDA (tùy chọn) |
| Alignment | 5-point warp 112x112 | tự viết | — | CPU |
| **Embedding** | **SFace** `face_recognition_sface_2021dec.onnx` | **Apache-2.0** | ~2-4 MB | ONNX Runtime CUDA |
| Liveness | **Silent-Face-Anti-Spoofing** (MiniFASNet) | Apache-2.0 | ~10 MB | ONNX CUDA (model caffe→onnx) |
| Tìm kiếm | FAISS (tùy chọn khi > vài nghìn người) | MIT | — | GPU |

> **Không dùng:** InsightFace buffalo_l/SCRFD/ArcFace (model **non-commercial**), SER-FIQ
> (CC BY-NC-SA), FaceQnet (license không rõ). Chi tiết xem Phụ lục A.

---

## 3. CHI TIẾT TỪNG MODULE

### 3.1 Module CAPTURE
- `cv2.VideoCapture` với `CAP_FFMPEG`, buffer size = 1 (tránh trễ)
- Hỗ trợ: webcam (`0`), IP cam RTSP (`rtsp://...`), file test
- Xử lý tối đa **10–15 FPS** (đủ cho ra/vào cổng), giải phóng GPU
- Giữ nguyên `CameraSession` hiện tại của `camera_session.py`, chỉ thay lõi

### 3.2 Module DETECT + TRACK (YuNet + CSRT)
- **YuNet**: `cv2.FaceDetectorYN.create(model, "", (320,320), score_threshold=0.6, nms=0.3, top_k=5000)`
  - Output: bbox + **5 landmarks** (2 mắt, mũi, 2 mép miệng)
  - Chạy mỗi **5 frame** (tiết kiệm GPU)
- **CSRT tracker** giữ track giữa các frame detect → bbox mượt, không nhảy
- Nhiều người: duy trì map `track_id → bbox`, chỉ xử lý người chính diện (gần cam nhất)

### 3.3 Module POSE (hướng dẫn quay đầu) — ĐIỂM MỚI
- **MediaPipe FaceLandmarker** (478 điểm 3D) → ma trận `faceTransformationMatrixes` 3x3
- Tách **yaw / pitch / roll** từ ma trận (hoặc solvePnP với 5 landmark)
- **State machine hướng dẫn**:
  ```
  [Bắt đầu] → "Nhìn thẳng vào camera"
  → Đạt yaw≈0, pitch≈0 → "Từ từ quay mặt sang TRÁI"
  → yaw < -15° → "Từ từ quay mặt sang PHẢI"
  → yaw > +15° → "Ngẩng nhẹ lên trên"
  → pitch < -10° → "Cúi nhẹ xuống dưới"
  → pitch > +10° → "Hoàn tất, giữ yên 2 giây"
  ```
- **Lưới pose 3x3** (yaw × pitch): mỗi ô cần ≥1 frame đạt → đảm bảo **đủ góc**
- Lời nhắc hiển thị bằng tiếng Việt trên overlay video

### 3.4 Module QUALITY GATE
- **Blur:** Laplacian variance > ngưỡng (vd 30)
- **Độ sáng:** histogram mean trong khoảng hợp lý (vd 60–220)
- **Kích thước mặt:** bbox ≥ 80x80 (đủ gần), loại quá xa/nhỏ
- **Mắt mở:** EAR từ landmark (2 mắt) > ngưỡng
- **Che mặt:** số landmark ẩn/thấp confidence
- **eDifFIQA** (tùy chọn): điểm 0–1, chấp nhận ≥ 0.3

### 3.5 Module ALIGN
- Dùng 5 landmark (2 mắt + mũi + 2 mép miệng) warp về **112x112** chuẩn
- Công thức chuẩn giống InsightFace/SFace: dùng `cv2.estimateAffinePartial2D` + `warpAffine`
- Alignment làm tăng độ chính xác nhận diện đáng kể

### 3.6 Module EMBEDDING (SFace)
- **SFace**: `cv2.FaceRecognizerSF.create(model, "", score_threshold=0.363)` (cosine)
  - Hoặc chạy trực tiếp qua ONNX Runtime CUDA cho nhanh hơn
  - Input: 112x112, output **128-d embedding**
- Chuẩn hóa L2 trước khi lưu; so sánh bằng **cosine similarity**
- Ngưỡng cần **calibrate trên dữ liệu Việt thật** (không lấy mặc định 0.363)

### 3.7 Module AGGREGATE (tạo template 1 người)
- Lưu **nhiều template riêng** (mỗi góc pose 1 embedding) + **median vector** chung
- File template:
  ```json
  {
    "employee_id": 123,
    "version": 2,
    "embedding_dim": 128,
    "templates": [ "...", "..." ],       // base64 float32, mỗi góc pose
    "median_embedding": "...",           // base64 float32
    "pose_metadata": { "yaw_range": [-25,25], "pitch_range": [-15,15], "coverage": "3x3 full" },
    "quality_scores": [0.9, 0.8, ...],
    "created_at": "2026-08-12T...Z",
    "checksum": "sha256..."
  }
  ```

### 3.8 Module LIVENESS
- **Lúc đăng ký:** hành động theo lời nhắc (quay đầu đa góc) → tự chống ảnh tĩnh; thêm **blink detection** (mediapipe landmark mắt)
- **Lúc quét cổng:** **Silent-Face-Anti-Spoofing** (MiniFASNet) → chống ảnh/màn hình/đầu giả
- Convert model caffe → ONNX để chạy GPU

### 3.9 Module QUYẾT ĐỊNH (scan realtime)
- Mỗi người: cosine trên từng template → lấy **best match**
- Xác nhận khi: cosine ≥ ngưỡng (calibrate) **VÀ** liveness pass **VÀ** bền vững qua N frame (vd 3-5 frame liên tiếp)
- Trạng thái: `confirmed` / `unknown` / `alert` — giữ nguyên contract API hiện tại

---

## 4. MÔI TRƯỜNG & CÀI ĐẶT (máy RTX 3060)

### 4.1 Yêu cầu phần cứng/phần mềm
- **GPU:** NVIDIA RTX 3060 12GB (compute capability 8.6, Ampere)
- **OS:** Windows 11 (WSL2) hoặc Ubuntu 22.04 — khuyến nghị **Ubuntu 22.04** cho triển khai
- **Driver:** NVIDIA ≥ 535, CUDA 12.x
- **Docker:** NVIDIA Container Toolkit (`--gpus all`)

### 4.2 Cài driver + CUDA (nếu Ubuntu)
```bash
sudo apt-get update
sudo apt-get install -y nvidia-driver-535   # hoặc mới hơn
# reboot
nvidia-smi   # xác nhận thấy RTX 3060
```

### 4.3 Docker GPU
```bash
# NVIDIA Container Toolkit
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey | sudo gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg
curl -s -L https://nvidia.github.io/libnvidia-container/$distribution/libnvidia-container.list | \
  sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' | \
  sudo tee /etc/apt/sources.list.d/nvidia-container-toolkit.list
sudo apt-get update
sudo apt-get install -y nvidia-container-toolkit
sudo nvidia-ctk runtime configure --runtime=docker
sudo systemctl restart docker

# Kiểm tra
docker run --rm --gpus all nvidia/cuda:12.2.0-base-ubuntu22.04 nvidia-smi
```

### 4.4 requirements.txt (face-runtime mới)
```
opencv-python-headless==4.10.0.84
onnxruntime-gpu==1.18.1        # CUDA 12.x
numpy==1.26.4
Flask==3.1.3
flask-cors==6.0.2
mediapipe==0.10.14             # FaceLandmarker (pose)
pyodbc==5.1.0                  # giữ cho enrollment worker
scipy==1.13.1
```
> **Bỏ:** dlib, face-recognition, face-recognition-models
> **Lưu ý:** không cài `onnxruntime` (CPU) song song — sẽ rơi về CPU

### 4.5 Tải model (copy vào `runtime/face-models/`)
| File | Nguồn | License |
|---|---|---|
| `face_detection_yunet_2023mar.onnx` | github.com/opencv/opencv_zoo → models/face_detection_yunet | MIT |
| `face_recognition_sface_2021dec.onnx` | github.com/opencv/opencv_zoo → models/face_recognition_sface | Apache-2.0 |
| `face_landmarker.task` | Google MediaPipe models | Apache-2.0 |
| eDifFIQA `.onnx` (tùy chọn) | opencv_zoo → face_image_quality_assessment_ediffiqa | CC-BY-4.0 |
| MiniFASNet `.onnx` (liveness) | Silent-Face-Anti-Spoofing (convert caffe→onnx) | Apache-2.0 |

---

## 5. CẤU TRÚC THƯ MỤC MỚI (face-runtime)

```
AI_Runtime/face_recognition/
├── nhandienface.py            # Flask gateway (giữ nguyên, sửa nhỏ)
├── runtime_config.py          # thêm threshold, pose bins, quality
├── model_registry.py          # SỬA: đọc template JSON (không phải .pkl dlib)
├── enrollment_service.py      # SỬA: dùng YuNet+SFace+pose hướng dẫn
├── camera_session.py          # SỬA: lõi detect+embed = YuNet+SFace
├── camera_manager.py          # giữ
├── face_detector.py           # MỚI: wrapper YuNet + CSRT track
├── face_embedder.py           # MỚI: wrapper SFace (ONNX CUDA)
├── pose_guide.py              # MỚI: MediaPipe pose + state machine hướng dẫn
├── face_quality.py            # MỚI: heuristic + eDifFIQA
├── liveness.py                # MỚI: Silent-FAS
├── template_store.py          # MỚI: lưu/đọc template JSON + cosine search
├── requirements.txt           # CẬP NHẬT (bỏ dlib, thêm onnxruntime-gpu/mediapipe)
├── Dockerfile                 # CẬP NHẬT: base nvidia/cuda + --gpus
└── tests/                     # CẬP NHẬT test cho lõi mới
```

---

## 6. KẾ HOẠCH TRIỂN KHAI (theo giai đoạn)

### Giai đoạn 0 — Chuẩn bị (0.5 ngày)
- [ ] Cài driver NVIDIA + CUDA + Docker GPU (mục 4.2, 4.3)
- [ ] Kiểm tra `docker run --gpus all nvidia-smi` thấy RTX 3060
- [ ] Clone repo, tạo branch `feature/faceid-sface`

### Giai đoạn 1 — Thay lõi detect+embed (2-3 ngày)
- [ ] Viết `face_detector.py` (YuNet + CSRT)
- [ ] Viết `face_embedder.py` (SFace, ONNX CUDA)
- [ ] Sửa `camera_session.py`: dùng YuNet+SFace thay dlib
- [ ] Sửa `model_registry.py`: đọc template JSON
- [ ] Test đơn vị: detect 1 người, embed ổn định, cosine đúng

### Giai đoạn 2 — Pose hướng dẫn + quality (2-3 ngày)
- [ ] Viết `pose_guide.py` (MediaPipe + state machine quay đầu)
- [ ] Viết `face_quality.py` (blur/sáng/mắt mở/kích thước + eDifFIQA)
- [ ] Tích hợp vào `enrollment_service.py`
- [ ] Test: đăng ký 1 người bằng webcam, xem overlay hướng dẫn tiếng Việt

### Giai đoạn 3 — Liveness + template store (2 ngày)
- [ ] Viết `liveness.py` (Silent-FAS, convert onnx)
- [ ] Viết `template_store.py` (JSON + cosine + FAISS tùy chọn)
- [ ] Test: chống ảnh giả khi quét, template lưu/đọc đúng

### Giai đoạn 4 — Đóng gói Docker GPU (1 ngày)
- [ ] Sửa `Dockerfile` (base `nvidia/cuda:12.2.0-runtime-ubuntu22.04`, `--gpus all`)
- [ ] Cập nhật `docker-compose` (thêm `gpus: all`, mount model dir)
- [ ] Build image, chạy, kiểm tra health + model load

### Giai đoạn 5 — Calibrate + tích hợp (2-3 ngày)
- [ ] Thu thập video test người Việt (10-20 người, nhiều góc/sáng)
- [ ] **Calibrate ngưỡng cosine** (tối ưu FAR/FRR) — KHÔNG dùng mặc định
- [ ] Tích hợp worker ASP.NET (giữ contract cũ, đổi format template)
- [ ] Test end-to-end: đăng ký → lưu → quét cổng → xác nhận/từ chối

### Giai đoạn 6 — Kiểm thử + hạ tầng (2 ngày)
- [ ] Test: nhiều camera, ánh sáng xấu, góc khó, người Á Đông
- [ ] Test liveness: ảnh in, màn hình, video quay sẵn
- [ ] Benchmark FPS trên RTX 3060
- [ ] Viết tài liệu vận hành + kịch bản rollback

**Tổng ước lượng: ~12–17 ngày công.**

---

## 7. KIỂM THỬ (test plan)

### 7.1 Unit test
- `face_detector`: detect đúng bbox + 5 landmark, track ổn định
- `face_embedder`: embedding 128-d, L2 chuẩn, cosine đúng
- `pose_guide`: state machine chuyển đúng qua các lời nhắc
- `face_quality`: lọc frame mờ/tối/xa/che
- `template_store`: lưu/đọc JSON, tìm đúng người, không trùng nhầm

### 7.2 Integration test (end-to-end)
1. Đăng ký người A bằng webcam theo hướng dẫn quay đầu
2. Kiểm tra file template A được tạo đủ góc (3x3 full)
3. Quét realtime: A nhận diện → confirmed
4. Người B (chưa đăng ký) → unknown
5. Đưa ảnh A lên màn hình → liveness từ chối

### 7.3 Benchmark RTX 3060
| Metric | Mục tiêu |
|---|---|
| Detect (YuNet @640) | <5 ms/frame |
| Embedding (SFace) | <3 ms/face |
| Pipeline 1 người | 30-60 FPS |
| Nhiều camera (4) | vẫn realtime |

---

## 8. RISK & LƯU Ý

### License (điểm số 1 — đã xác nhận)
- ✅ **YuNet (MIT), SFace (Apache-2.0), MediaPipe (Apache-2.0), eDifFIQA (CC-BY-4.0), Silent-FAS (Apache-2.0)** — toàn bộ thương mại được, không cần xin phép
- ❌ **Tránh tuyệt đối:** InsightFace model (buffalo_*, SCRFD, ArcFace — non-commercial), SER-FIQ (CC BY-NC-SA), FaceQnet (license không rõ), các model train trên MS1M/WebFace (non-commercial)

### Kỹ thuật
- **Calibrate ngưỡng** trên dữ liệu Việt thật — ngưỡng mặc định 0.363 của SFace chỉ là điểm khởi đầu
- **Alignment bắt buộc** — quyết định lớn đến độ chính xác
- **Đăng ký nhiều ảnh/góc** — 1 frame đơn lẻ không đủ, luôn lưu median + nhiều template
- **Chất lượng ảnh khi đăng ký** — yêu cầu đủ sáng, nhìn thẳng, nhiều góc

### Vận hành
- Camera 2-5 FPS là đủ cho ra/vào cửa, không cần 30 FPS (tiết kiệm GPU cho nhiều camera)
- Nên có liveness ở cổng (Silent-FAS) — riêng "quay đầu" không chống được video quay sẵn
- GPU RTX 3060 dư sức cho 8-16 camera nếu dùng tracker + skip-frame

---

## 9. ROLLBACK
- Giữ branch `main` (dlib) làm bản dự phòng
- `git checkout main && docker compose up -d --build` = quay về trạng thái cũ
- File template mới (JSON) không tương thích dlib (.pkl) → nếu rollback, cần re-enroll hoặc giữ song song 2 format trong thời gian chuyển đổi

---

## PHỤ LỤC A — Ma trận license đầy đủ

| Thành phần | Code | Model pre-trained | Dataset train | Dùng thương mại? |
|---|---|---|---|---|
| InsightFace (buffalo_l/s/m/sc) | MIT | **Non-commercial** | WebFace600K (non-commercial) | ⚠️ KHÔNG (trừ khi mua license) |
| facenet-pytorch | MIT | MIT (.pt) | VGGFace2/CASIA (research-only) | ⚠️ Vùng xám |
| **SFace (OpenCV)** | **Apache-2.0** | **Apache-2.0** | — | ✅ **Sạch** |
| **YuNet (OpenCV)** | **MIT** | **MIT** | — | ✅ **Sạch** |
| **MediaPipe FaceLandmarker** | **Apache-2.0** | **Apache-2.0** | — | ✅ **Sạch** |
| **eDifFIQA(T)** | — | **CC-BY-4.0** | — | ✅ (phải ghi nguồn) |
| **Silent-FAS** | **Apache-2.0** | **Apache-2.0** | — | ✅ **Sạch** |
| GhostFaceNet | MIT | MIT (checkpoint) | MS1MV2/3 (non-commercial) | ⚠️ Vùng xám |
| AdaFace | MIT | MIT | WebFace/MS1M (non-commercial) | ⚠️ Vùng xám |
| SER-FIQ | — | — | — | ❌ CC BY-NC-SA |
| dlib (hiện tại) | BSD | BSD | — | ✅ (nhưng lỗi thời) |

---

## PHỤ LỤC B — Tài liệu tham khảo

- YuNet: https://github.com/opencv/opencv_zoo/tree/main/models/face_detection_yunet
- SFace: https://github.com/opencv/opencv_zoo/tree/main/models/face_recognition_sface
- eDifFIQA: https://github.com/opencv/opencv_zoo/tree/main/models/face_image_quality_assessment_ediffiqa
- MediaPipe Face Landmarker: https://ai.google.dev/edge/mediapipe/solutions/vision/face_landmarker
- Silent-Face-Anti-Spoofing: https://github.com/minivision-ai/Silent-Face-Anti-Spoofing
- InsightFace (license): https://github.com/deepinsight/insightface
- DeepFace (license kế thừa model): https://github.com/serengil/deepface
- ByteTrack: https://github.com/FoundationVision/ByteTrack
- Demo guided enrollment (tham khảo): https://github.com/Krishnaa-Vinod/pose-guided-capture
