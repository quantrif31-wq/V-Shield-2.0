<template>
  <div class="page-container animate-in">
    <header class="page-header">
      <div>
        <span class="panel-kicker">Cá nhân</span>
        <h1 class="page-title">Đăng ký Face ID</h1>
        <p class="page-subtitle">Nhìn thẳng vào camera rồi làm theo hướng dẫn quay 5 góc. Hệ thống tự nhận diện và lưu mẫu cho tài khoản của bạn.</p>
      </div>
      <span v-if="status" class="badge" :class="status.hasFaceId ? 'info' : 'warn'">
        {{ status.hasFaceId ? 'Đã đăng ký Face ID' : 'Chưa đăng ký' }}
      </span>
    </header>

    <div class="faceid-grid">
      <div class="faceid-preview">
        <div class="video-wrapper" :class="overlayClass">
          <video ref="videoRef" class="video" autoplay playsinline muted></video>

          <div v-if="!streamActive" class="video-off">Bấm "Mở camera" để bắt đầu</div>

          <div v-if="streamActive && capturing" class="error-banner" v-show="errorMessage">
            ⚠ {{ errorMessage }}
          </div>

          <div v-if="streamActive && capturing" class="guided-overlay">
            <div class="guided-arrow" :class="arrowClass">{{ arrowGlyph }}</div>
            <div class="guided-prompt">{{ guidance }}</div>
          </div>

          <div v-if="streamActive && capturing" class="guided-grid">
            <div
              v-for="cell in gridCells"
              :key="cell.key"
              class="guided-cell"
              :class="cell.class"
            >
              {{ cell.label }}
            </div>
          </div>
        </div>

        <div class="controls">
          <button class="btn btn-secondary" :disabled="capturing || loadingModel" @click="openCamera">
            {{ streamActive ? 'Camera đang bật' : 'Mở camera' }}
          </button>
          <button class="btn btn-primary" :disabled="!streamActive || capturing || sending || loadingModel" @click="startCapture">
            {{ loadingModel ? 'Đang tải model...' : capturing ? 'Đang quay...' : 'Bắt đầu quay' }}
          </button>
          <button class="btn btn-outline" :disabled="!capturing" @click="stopCapture">Dừng</button>
        </div>

        <div v-if="capturing" class="progress">
          <div class="progress-track">
            <div class="progress-bar" :style="{ width: progressPercent + '%' }"></div>
          </div>
          <span>Đã đủ {{ guidedProgress }}/5 góc · {{ frames.length }} khung tốt</span>
        </div>

        <div v-if="error" class="error-box">{{ error }}</div>
      </div>

      <div class="faceid-side">
        <div v-if="status && status.hasFaceId" class="result-box">
          <h3>Thông tin Face ID hiện tại</h3>
          <div class="meta"><span>File:</span> <strong>{{ status.modelFileName || '---' }}</strong></div>
          <div class="meta"><span>Số mẫu:</span> <strong>{{ status.encodingCount || '---' }}</strong></div>
          <div class="meta"><span>Phiên bản:</span> <strong>v{{ status.version || '---' }}</strong></div>
        </div>

        <div v-else class="result-box">
          <h3>Hướng dẫn</h3>
          <ol>
            <li>Mở camera, nhìn thẳng vào ống kính.</li>
            <li>Bấm "Bắt đầu quay".</li>
            <li>Quay đầu theo hướng dẫn: thẳng → trái → phải → lên → xuống.</li>
            <li>Đủ 5 góc (lưới bên trái xanh hết) là có thể gửi.</li>
          </ol>
        </div>

        <button
          class="btn btn-primary submit-btn"
          :disabled="!frames.length || sending || capturing"
          @click="submit"
        >
          {{ sending ? 'Đang gửi...' : 'Gửi đăng ký' }}
        </button>

        <div v-if="successMsg" class="success-box">{{ successMsg }}</div>
        <div v-if="error" class="error-box">{{ error }}</div>

        <div v-if="frames.length" class="thumbs">
          <img v-for="(frame, index) in frames" :key="index" :src="frame" alt="frame" class="thumb" />
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { getMyFaceStatus, enrollSelf } from "../services/faceEnrollmentApi"
import { loadLandmarker, detectFace } from "../services/faceLandmarker"
import { PoseGuideClient } from "../services/poseGuideClient"

export default {
  name: "MyFaceId",
  data() {
    return {
      stream: null,
      streamActive: false,
      capturing: false,
      sending: false,
      loadingModel: false,
      frames: [],
      timer: null,
      error: "",
      successMsg: "",
      status: null,
      destroyed: false,
      guidance: "Nhìn thẳng vào camera",
      faceState: "none",
      guidedProgress: 0,
      guidedComplete: false,
      gridCells: [],
      arrow: "none"
    }
  },

  computed: {
    progressPercent() {
      return Math.min(100, Math.round((this.guidedProgress / 5) * 100))
    },
    overlayClass() {
      if (!this.streamActive || !this.capturing) return "overlay-off"
      if (this.faceState === "none" || this.faceState === "multiple") return "overlay-danger"
      if (this.guidedComplete) return "overlay-ok"
      return "overlay-wait"
    },
    errorMessage() {
      if (!this.capturing) return ""
      if (this.faceState === "none") return "Không thấy khuôn mặt — hãy đưa mặt vào khung"
      if (this.faceState === "multiple") return "Phát hiện nhiều khuôn mặt — chỉ để lại 1 người"
      return ""
    },
    arrowClass() {
      return {
        left: "arrow-left",
        right: "arrow-right",
        up: "arrow-up",
        down: "arrow-down",
        none: "arrow-center"
      }[this.arrow]
    },
    arrowGlyph() {
      return { left: "◀", right: "▶", up: "▲", down: "▼", none: "●" }[this.arrow]
    }
  },

  async mounted() {
    this.destroyed = false
    try {
      this.status = await getMyFaceStatus()
    } catch (e) {
      this.error = e?.response?.data?.message || e?.message || "Không tải được trạng thái."
    }
  },

  beforeUnmount() {
    this.destroyed = true
    this.stopCapture()
    this.releaseCamera()
  },

  methods: {
    async openCamera() {
      this.error = ""
      if (!navigator.mediaDevices?.getUserMedia) {
        this.error = "Trình duyệt không hỗ trợ camera. Cần dùng HTTPS hoặc localhost."
        return
      }
      try {
        const stream = await navigator.mediaDevices.getUserMedia({
          video: { width: { ideal: 640 }, height: { ideal: 480 }, facingMode: "user" },
          audio: false
        })
        this.releaseCamera()
        this.stream = stream
        this.streamActive = true
        this.$nextTick(() => {
          const video = this.$refs.videoRef
          if (video) {
            video.srcObject = stream
            video.play().catch(() => {})
          }
        })
      } catch (err) {
        this.error = "Không mở được camera: " + (err?.message || "lỗi không xác định")
      }
    },

    releaseCamera() {
      if (this.stream) {
        this.stream.getTracks().forEach((track) => track.stop())
        this.stream = null
      }
      this.streamActive = false
    },

    async startCapture() {
      if (!this.streamActive) return
      this.loadingModel = true
      this.error = ""
      try {
        await loadLandmarker()
      } catch (e) {
        this.error = "Không tải được model nhận diện: " + (e?.message || "")
        this.loadingModel = false
        return
      }
      this.loadingModel = false

      this.frames = []
      this.successMsg = ""
      this.guide = new PoseGuideClient(3)
      this.guidedProgress = 0
      this.guidedComplete = false
      this.gridCells = this.buildGrid([])
      this.capturing = true
      this._lastFrameData = null
      this._frameCounter = 0

      this.timer = setInterval(() => {
        if (this.destroyed) return
        this.processFrame()
      }, 150)
    },

    stopCapture() {
      if (this.timer) {
        clearInterval(this.timer)
        this.timer = null
      }
      this.capturing = false
    },

    processFrame() {
      const video = this.$refs.videoRef
      if (!video || !video.videoWidth) return

      const result = detectFace(video)
      this.faceState = result.faceState
      if (result.faceState !== "single") {
        this.guidance = result.faceState === "none" ? "Không thấy khuôn mặt" : "Nhiều khuôn mặt"
        return
      }

      const state = this.guide.update(result.yaw, result.pitch)
      this.guidance = state.guidance
      this.guidedProgress = state.progress
      this.guidedComplete = state.complete
      this.gridCells = this.buildGrid(state.coveredAngles)
      this.arrow = this.inferArrow(state.guidance)

      // Chỉ chụp frame khi có đúng 1 mặt và đủ khác biệt (tránh trùng lặp).
      this.captureIfDistinct(video)
    },

    captureIfDistinct(video) {
      const width = Math.min(640, video.videoWidth || 640)
      const height = Math.max(1, Math.round(width * (video.videoHeight / video.videoWidth)))
      if (!this._canvas) this._canvas = document.createElement("canvas")
      this._canvas.width = width
      this._canvas.height = height
      const context = this._canvas.getContext("2d")
      context.drawImage(video, 0, 0, width, height)

      const imageData = context.getImageData(0, 0, width, height).data
      if (this._lastFrameData) {
        let diff = 0
        for (let i = 0; i < imageData.length; i += 4000) {
          diff += Math.abs(imageData[i] - this._lastFrameData[i])
        }
        if (diff < 60) return
      }
      this._lastFrameData = imageData

      if (this.frames.length < 25) {
        this.frames.push(this._canvas.toDataURL("image/jpeg", 0.85))
      }
    },

    buildGrid(covered) {
      const order = ["straight", "left", "right", "up", "down"]
      const labels = { straight: "Thẳng", left: "Trái", right: "Phải", up: "Lên", down: "Xuống" }
      return order.map((key) => ({
        key,
        label: labels[key],
        class: covered.includes(key) ? "cell-ok" : "cell-wait"
      }))
    },

    inferArrow(guidance) {
      const text = String(guidance || "").toLowerCase()
      if (text.includes("phải")) return "right"
      if (text.includes("trái")) return "left"
      if (text.includes("lên") || text.includes("ngẩng")) return "up"
      if (text.includes("xuống") || text.includes("cúi")) return "down"
      return "none"
    },

    async submit() {
      if (!this.frames.length) return
      this.sending = true
      this.error = ""
      this.successMsg = ""
      try {
        const res = await enrollSelf(this.frames)
        this.successMsg = "Đăng ký Face ID thành công!"
        this.status = {
          hasFaceId: true,
          modelFileName: res?.modelFileName,
          checksum: res?.checksum,
          encodingCount: res?.encodingCount,
          version: res?.registryVersion
        }
        this.frames = []
      } catch (e) {
        this.error = e?.response?.data?.message || e?.message || "Gửi đăng ký thất bại."
      } finally {
        this.sending = false
      }
    }
  }
}
</script>

<style scoped>
.page-container { padding: 16px; }
.page-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; margin-bottom: 16px; }
.page-title { margin: 0; font-size: clamp(26px, 3.5vw, 36px); font-weight: 900; }
.page-subtitle { margin: 8px 0 0; color: var(--text-muted); }

.faceid-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.2fr) minmax(300px, 0.8fr);
  gap: 20px;
}
@media (max-width: 900px) {
  .faceid-grid { grid-template-columns: 1fr; }
}

.faceid-preview, .faceid-side {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.video-wrapper {
  width: 100%;
  aspect-ratio: 4 / 3;
  background: #000;
  border-radius: 12px;
  overflow: hidden;
  position: relative;
  border: 3px solid var(--border-color);
  transition: border-color 200ms ease, box-shadow 200ms ease;
}
.video-wrapper.overlay-wait { border-color: #eab308; box-shadow: 0 0 0 3px rgba(234,179,8,0.35); }
.video-wrapper.overlay-ok { border-color: #22c55e; box-shadow: 0 0 0 3px rgba(34,197,94,0.35); }
.video-wrapper.overlay-danger { border-color: #dc2626; box-shadow: 0 0 0 3px rgba(220,38,38,0.45); }
.video { width: 100%; height: 100%; object-fit: cover; display: block; }
.video-off {
  position: absolute; inset: 0;
  display: flex; align-items: center; justify-content: center;
  color: var(--text-muted); font-weight: 600;
}

.error-banner {
  position: absolute; top: 12px; left: 50%; transform: translateX(-50%);
  z-index: 20; background: rgba(220,38,38,0.9); color: #fff;
  padding: 8px 16px; border-radius: 999px; font-size: 0.9rem; font-weight: 800;
  max-width: 90%; text-align: center;
}

.guided-overlay {
  position: absolute; left: 12px; right: 12px; bottom: 12px;
  display: flex; flex-direction: column; align-items: center; gap: 6px; pointer-events: none;
}
.guided-arrow {
  width: 56px; height: 56px; border-radius: 50%;
  background: rgba(2,6,23,0.65); border: 3px solid #eab308; color: #eab308;
  display: flex; align-items: center; justify-content: center; font-size: 26px; font-weight: 900;
}
.guided-arrow.arrow-center { border-color: #94a3b8; color: #94a3b8; }
.guided-prompt {
  padding: 8px 16px; border-radius: 999px; background: rgba(2,6,23,0.78);
  color: #fff; font-size: 1rem; font-weight: 800; text-align: center; border: 2px solid #eab308;
}

.guided-grid {
  position: absolute; top: 12px; right: 12px;
  display: grid; grid-template-columns: 1fr; gap: 6px;
  background: rgba(2,6,23,0.7); padding: 8px; border-radius: 10px;
}
.guided-cell {
  min-width: 60px; height: 28px; padding: 0 10px; border-radius: 8px;
  display: flex; align-items: center; justify-content: center;
  font-size: 0.8rem; font-weight: 800; border: 2px solid var(--border-color); color: var(--text-muted);
}
.guided-cell.cell-wait { border-color: #eab308; color: #eab308; background: rgba(234,179,8,0.12); }
.guided-cell.cell-ok { border-color: #22c55e; color: #22c55e; background: rgba(34,197,94,0.15); }

.controls { display: flex; gap: 8px; flex-wrap: wrap; }
.progress-track { height: 8px; border-radius: 999px; background: var(--bg-input); border: 1px solid var(--border-color); overflow: hidden; }
.progress-bar { height: 100%; border-radius: 999px; background: var(--accent-primary); transition: width 150ms ease; }
.progress span { font-size: 0.85rem; color: var(--text-secondary); }

.error-box { padding: 10px 14px; border-radius: 8px; background: rgba(195,81,70,0.08); border: 1px solid rgba(195,81,70,0.2); color: var(--accent-danger); font-weight: 600; }
.success-box { padding: 10px 14px; border-radius: 8px; background: rgba(80,190,130,0.1); border: 1px solid rgba(80,190,130,0.25); color: var(--accent-success); font-weight: 700; }

.result-box { padding: 14px; border-radius: 12px; background: var(--bg-primary); border: 1px solid var(--border-color); }
.result-box h3 { margin: 0 0 10px; font-size: 1.05rem; }
.result-box ol { margin: 0; padding-left: 18px; display: flex; flex-direction: column; gap: 6px; font-size: 0.9rem; color: var(--text-secondary); }
.meta { display: flex; justify-content: space-between; font-size: 0.9rem; margin-bottom: 6px; }
.meta span { color: var(--text-secondary); }
.meta strong { font-family: monospace; }

.submit-btn { min-height: 46px; font-size: 1rem; }
.thumbs { display: grid; grid-template-columns: repeat(5, 1fr); gap: 6px; }
.thumb { width: 100%; aspect-ratio: 4/3; object-fit: cover; border-radius: 6px; border: 1px solid var(--border-color); }
</style>
