<template>
  <div class="face-page animate-in">
    <header class="face-header">
      <div class="face-header-titles">
        <span class="face-kicker">Giám sát an ninh</span>
        <h1 class="face-title">Nhận diện khuôn mặt</h1>
      </div>
      <div class="face-status-pill" v-if="activeCameraName">
        <span class="pill-dot" :class="cameraRunning ? 'dot-on' : 'dot-off'"></span>
        <span>{{ cameraRunning ? 'Đang quét' : 'Sẵn sàng' }}</span>
      </div>
    </header>

    <!-- Control bar -->
    <div class="control-bar">
      <div class="camera-picker">
        <label class="picker-label">Camera</label>
        <div class="picker-control">
          <input
            v-model="cameraSearch"
            type="text"
            class="picker-input"
            placeholder="Chọn hoặc tìm camera..."
            :disabled="loading"
            @focus="cameraOpen = true"
            @blur="cameraOpen = false"
            @keydown.enter.prevent="applyTypedCamera"
          />
          <div v-if="cameraOpen && cameraDropdownMatches.length" class="picker-dropdown">
            <div
              v-for="cam in cameraDropdownMatches"
              :key="cam.cameraId"
              class="picker-option"
              @mousedown.prevent
              @click="selectCamera(cam)"
            >
              <div class="option-name">{{ cam.cameraName }}</div>
              <div class="option-meta">ID {{ cam.cameraId }}{{ cam.gateName ? ' · ' + cam.gateName : '' }}</div>
            </div>
          </div>
          <div v-else-if="cameraOpen && camerasLoading" class="picker-hint">Đang tải camera…</div>
          <div v-else-if="cameraOpen && !allCameras.length" class="picker-hint">Chưa có camera.</div>
        </div>
      </div>

      <div class="control-actions">
        <button
          class="btn btn-primary start-btn"
          :disabled="loading || (!selectedConfiguration && !cameraIp.trim())"
          @click="handleStartOrReset"
        >
          <span v-if="loading" class="btn-spinner"></span>
          <span>{{ loading ? 'Đang xử lý…' : (cameraRunning ? 'Reset phiên' : 'Bắt đầu') }}</span>
        </button>
        <button
          class="btn btn-outline stop-btn"
          :disabled="loading || !cameraRunning"
          @click="handleTurnOff"
        >
          Dừng
        </button>
      </div>
    </div>

    <!-- Main stage -->
    <div class="main-stage">
      <div class="stage-video">
        <div
          ref="videoWrapperRef"
          class="video-frame"
          :class="cameraRunning ? 'frame-live' : 'frame-idle'"
          @dblclick="handleDoubleClick"
          @contextmenu="handleRightClick"
        >
          <iframe
            v-if="previewRunning && directCameraUrl"
            :key="directCameraKey"
            :src="directCameraUrl"
            class="video"
            title="Camera"
            allow="autoplay; fullscreen"
            frameborder="0"
            @load="handleDirectPreviewLoaded"
          ></iframe>
          <div v-else class="video-placeholder">
            <div class="placeholder-icon">◉</div>
            <div class="placeholder-text">{{ activeCameraName ? 'Chờ bắt đầu quét…' : 'Chọn camera và bấm Bắt đầu' }}</div>
          </div>

          <div v-if="cameraRunning && !previewRunning" class="video-toast">Đang kết nối…</div>
        </div>
      </div>

      <!-- Live result panel -->
      <aside class="stage-info">
        <section class="info-card">
          <h2 class="info-title">Kết quả nhận diện</h2>

          <div class="big-id" :class="identityConfirmed ? 'id-hit' : 'id-empty'">
            {{ employeeId || '— — — —' }}
          </div>
          <div class="id-caption">Mã nhân viên</div>

          <div class="info-row">
            <span>Trạng thái</span>
            <span class="value" :class="'val-' + detectionState">{{ detectionLabel }}</span>
          </div>
          <div class="info-row">
            <span>Độ khớp</span>
            <span class="value">{{ distanceText }} <small class="dim">(ngưỡng 0.35)</small></span>
          </div>
          <div class="info-row">
            <span>Số lần xác nhận</span>
            <span class="value">{{ confirmCount }}</span>
          </div>
          <div class="info-row">
            <span>Khung hình</span>
            <span class="value">{{ bboxText }}</span>
          </div>
          <div class="info-row">
            <span>FPS</span>
            <span class="value">{{ fps }}</span>
          </div>

          <div class="info-row" v-if="lastUpdate">
            <span>Cập nhật</span>
            <span class="value dim">{{ lastUpdate }}</span>
          </div>
        </section>

        <section class="info-card evidence-card" v-if="lockedSnapshot || lockedFaceCrop">
          <h2 class="info-title">Ảnh chụp</h2>
          <div class="evidence-grid">
            <img v-if="lockedSnapshot" :src="lockedSnapshot" class="evidence-img" alt="Toàn khung" />
            <img v-if="lockedFaceCrop" :src="lockedFaceCrop" class="evidence-img" alt="Crop khuôn mặt" />
          </div>
        </section>

        <div v-if="faceServiceError" class="error-box">{{ faceServiceError.message }}</div>
        <div v-if="message && !faceServiceError" class="toast-box">{{ message }}</div>
      </aside>
    </div>
  </div>
</template>

<script>
import {
  startCamera,
  stopCamera,
  resetCamera,
  getCameraStatus,
  getCameraResult,
  getLockedImages,
  normalizeFaceApiError,
  shouldStopFacePolling
} from "../services/faceApi"
import { ensureCameraRegistered, getCameras } from "../services/cameraRuntimeApi"
import {
  getFaceCameraConfigurations,
  startConfiguredFaceCamera,
  stopConfiguredFaceCamera
} from "../services/faceCameraConfigurationApi"
import { captureError, recordMetric } from "../services/observability"

export default {
  name: "FaceIdSecurity",

  props: {
    cameraId: {
      type: String,
      default: "monitoring-face-camera"
    },
    laneId: {
      type: String,
      default: null
    }
  },

  data() {
    return {
      cameraIp: "",
      savedConfigurations: [],
      selectedRuntimeCameraId: "",
      currentIp: "",
      activeCameraName: "",
      cameraRunning: false,
      cameraConnected: false,
      previewRunning: false,
      loading: false,

      allCameras: [],
      cameraSearch: "",
      cameraOpen: false,
      camerasLoading: false,

      employeeId: "",
      trackingActive: false,
      identityConfirmed: false,
      faceMatch: false,
      confirmCount: 0,
      distance: null,
      bbox: null,
      timeoutState: false,
      alert: false,

      lockedSnapshot: "",
      lockedFaceCrop: "",
      scanLocked: false,
      lockReason: "",

      fps: 0,
      message: "",
      lastUpdate: "",
      faceServiceError: null,

      directCameraUrl: "",
      directCameraSourceUrl: "",
      directCameraKey: 0,
      previewHealthy: false,
      previewRetryCount: 0,
      previewRetryTimer: null,

      resultTimer: null,
      busyResult: false,
      isFetchingLockedImages: false,
      destroyed: false
    }
  },

  computed: {
    selectedConfiguration() {
      return this.savedConfigurations.find(
        item => item.runtimeCameraId === this.selectedRuntimeCameraId
      ) || null
    },

    activeCameraId() {
      return this.selectedRuntimeCameraId || this.cameraId
    },

    bboxText() {
      if (!this.bbox) return "— — — —"
      return `L ${this.bbox.left} · T ${this.bbox.top} · R ${this.bbox.right} · B ${this.bbox.bottom}`
    },

    detectionLabel() {
      if (this.scanLocked) {
        if (this.lockReason === "confirmed") return "Đã xác nhận danh tính"
        if (this.lockReason === "timeout") return "Hết thời gian chờ"
        if (this.lockReason === "alert") return "Cảnh báo"
        return "Đã khóa"
      }
      if (!this.trackingActive) return "Idle"
      if (this.identityConfirmed) return "Đã nhận diện"
      if (this.faceMatch) return "Đang xác minh"
      return "Chưa nhận diện"
    },

    detectionState() {
      if (this.scanLocked) return this.lockReason === "confirmed" ? "hit" : "locked"
      if (this.identityConfirmed) return "hit"
      if (this.faceMatch) return "verify"
      if (this.trackingActive) return "track"
      return "idle"
    },

    distanceText() {
      const num = Number(this.distance)
      if (Number.isNaN(num)) return "— — — —"
      return num.toFixed(4)
    },

    cameraDropdownMatches() {
      const keyword = String(this.cameraSearch || "").trim().toLowerCase()
      let list = Array.isArray(this.allCameras) ? this.allCameras : []
      if (keyword) {
        list = list.filter(cam =>
          String(cam.cameraName || "").toLowerCase().includes(keyword) ||
          String(cam.cameraId || "").includes(keyword)
        )
      }
      return list.slice(0, 50)
    }
  },

  async mounted() {
    this.destroyed = false
    await this.loadAllCameras()
    await this.loadSavedConfigurations()
    if (!this.selectedConfiguration || this.selectedConfiguration.runtimeEnabled) {
      await this.loadCurrentStatus()
    }
    if (this.selectedConfiguration?.previewUrl && !this.previewRunning) {
      this.mountRegisteredPreview({ urlView: this.selectedConfiguration.previewUrl }, "")
    }
    if (this.cameraRunning) {
      this.startResultLoop()
    }
  },

  beforeUnmount() {
    this.destroyed = true
    this.stopResultLoop()
    this.resetDirectPreview()
  },

  activated() {
    this.destroyed = false
    if (this.cameraRunning) {
      if (this.currentIp && !this.previewRunning) {
        this.mountDirectPreview(this.currentIp)
      }
      this.startResultLoop()
    }
  },

  deactivated() {
    this.stopResultLoop()
  },

  methods: {
    async loadAllCameras() {
      if (this.destroyed) return
      this.camerasLoading = true
      try {
        const list = await getCameras()
        this.allCameras = Array.isArray(list) ? list : []
      } catch (error) {
        console.warn("Không tải được danh sách camera:", error)
        this.allCameras = []
      } finally {
        this.camerasLoading = false
      }
    },

    async loadSavedConfigurations() {
      try {
        const overview = await getFaceCameraConfigurations()
        this.savedConfigurations = Array.isArray(overview?.configurations)
          ? overview.configurations
          : []
        if (!this.selectedRuntimeCameraId && this.savedConfigurations.length) {
          this.selectedRuntimeCameraId = this.savedConfigurations[0].runtimeCameraId
        }
        const cfg = this.selectedConfiguration
        if (cfg) {
          this.activeCameraName = cfg.cameraName
          this.cameraSearch = cfg.cameraName
          this.cameraIp = cfg.streamUrlMasked || ""
        }
      } catch (error) {
        this.handleFaceServiceError(error, { polling: true })
      }
    },

    async selectCamera(cam) {
      if (!cam) return
      this.cameraIp = cam.streamUrl || cam.urlView || ""
      this.cameraSearch = cam.cameraName || ""
      this.activeCameraName = cam.cameraName || ""
      this.cameraOpen = false

      const match = this.savedConfigurations.find(
        item => item.cameraId === cam.cameraId || item.cameraName === cam.cameraName
      )
      this.selectedRuntimeCameraId = match ? match.runtimeCameraId : ""

      this.stopResultLoop()
      this.clearResultStateOnly()
      this.resetDirectPreview()
      this.cameraRunning = false

      try {
        const camera = await ensureCameraRegistered({
          cameraName: cam.cameraName || "Face Monitor Camera",
          cameraType: "Face",
          streamUrl: cam.streamUrl || cam.urlView || "",
        })
        if (camera?.urlView) {
          this.mountRegisteredPreview(camera, cam.streamUrl || "")
        }
      } catch (e) {
        console.warn("selectCamera preview error:", e)
      }
      this.message = `Đã chọn camera ${cam.cameraName || cam.cameraId}`
    },

    applyTypedCamera() {
      const kw = String(this.cameraSearch || "").trim().toLowerCase()
      const found = (Array.isArray(this.allCameras) ? this.allCameras : [])
        .find(cam => String(cam.cameraName || "").toLowerCase() === kw ||
                      String(cam.cameraId || "").toLowerCase() === kw)
      if (found) {
        this.selectCamera(found)
      }
    },

    async handleStartOrReset() {
      const initializationStartedAt = performance.now()
      const ip = (this.cameraIp || this.currentIp || "").trim()
      if (!this.selectedConfiguration && !ip) {
        alert("Vui lòng chọn camera trước")
        return
      }

      try {
        this.loading = true
        if (!this.selectedConfiguration) {
          const camera = await ensureCameraRegistered({
            cameraName: this.activeCameraName || "Face Monitor Camera",
            cameraType: "Face",
            streamUrl: ip,
          })
          this.currentIp = ip
          if (!this.previewRunning) {
            this.mountRegisteredPreview(camera, ip)
          }
        }

        this.clearResultStateOnly()

        if (!this.cameraRunning) {
          this.stopResultLoop()
          const res = this.selectedConfiguration
            ? await startConfiguredFaceCamera(this.activeCameraId)
            : await startCamera(this.activeCameraId, ip, this.laneId)
          this.clearFaceServiceError()
          if (!this.selectedConfiguration && !res?.success) {
            alert(res?.message || "Không thể khởi tạo phiên nhận diện")
            return
          }
          this.cameraRunning = true
          this.currentIp = ip || this.currentIp
          this.message = res?.configuration
            ? "Đã bắt đầu quét"
            : (res.message || "Đã bắt đầu quét")

          if (this.selectedConfiguration?.previewUrl && !this.previewRunning) {
            this.mountRegisteredPreview({ urlView: this.selectedConfiguration.previewUrl }, "")
          }
          await this.refreshResult()
          this.startResultLoop()
          return
        }

        const res = await resetCamera(this.activeCameraId)
        this.clearFaceServiceError()
        this.message = res?.message || "Đã reset phiên"
        await this.refreshResult()
        if (!this.resultTimer) {
          this.startResultLoop()
        }
      } catch (e) {
        captureError(e, "camera_initialization_failure", { component: "FaceCamera" })
        this.handleFaceServiceError(e)
      } finally {
        recordMetric("camera_initialization", performance.now() - initializationStartedAt, { component: "FaceCamera" })
        this.loading = false
      }
    },

    async handleTurnOff() {
      try {
        this.loading = true
        this.stopResultLoop()
        try {
          const res = this.selectedConfiguration
            ? await stopConfiguredFaceCamera(this.activeCameraId)
            : await stopCamera(this.activeCameraId)
          this.clearFaceServiceError()
          this.message = res?.message || "Đã dừng quét"
          if (this.selectedConfiguration) {
            await this.loadSavedConfigurations()
          }
        } catch (e) {
          if (e?.status === 404) {
            this.clearFaceServiceError()
            this.message = "Camera đã dừng"
            this.hardResetUiState()
            this.resetDirectPreview()
            return
          }
          this.handleFaceServiceError(e)
        }
        this.hardResetUiState()
        this.resetDirectPreview()
      } catch (e) {
        this.handleFaceServiceError(e)
      } finally {
        this.loading = false
      }
    },

    async loadCurrentStatus() {
      try {
        const res = await getCameraStatus(this.activeCameraId)
        this.clearFaceServiceError()
        await this.applyRealtimeState(res, false)
        if (this.currentIp) {
          this.cameraIp = this.currentIp
        }
        if (this.currentIp) {
          const camera = await ensureCameraRegistered({
            cameraName: "Face Monitor Camera",
            cameraType: "Face",
            streamUrl: this.currentIp,
          })
          this.mountRegisteredPreview(camera, this.currentIp)
        } else {
          this.resetDirectPreview()
        }
        if (this.cameraRunning) {
          await this.fetchLockedImagesIfNeeded(true)
        }
      } catch (e) {
        this.handleFaceServiceError(e, { polling: true })
      }
    },

    async refreshResult() {
      try {
        const res = await getCameraResult(this.activeCameraId)
        this.clearFaceServiceError()
        await this.applyRealtimeState(res, true)
      } catch (e) {
        this.handleFaceServiceError(e, { polling: true })
      }
    },

    async applyRealtimeState(res, allowTurnOffReset = true) {
      if (!res || this.destroyed) return
      const incomingCameraEnabled = !!res.camera_enabled

      this.cameraRunning = incomingCameraEnabled
      this.cameraConnected = !!res.camera_connected
      this.currentIp = res.ip || this.currentIp

      this.employeeId = res.employee_id || ""
      this.trackingActive = !!res.tracking_active
      this.identityConfirmed = !!res.identity_confirmed
      this.faceMatch = !!res.face_match
      this.confirmCount = Number(res.confirm_count || 0)
      this.distance = res.distance ?? null
      this.bbox = res.bbox || null
      this.timeoutState = !!res.timeout
      this.alert = !!res.alert

      this.scanLocked = !!res.scan_locked
      this.lockReason = res.lock_reason || ""

      this.fps = Number(res.fps || 0)
      this.message = res.message || ""
      this.lastUpdate = res.last_update || ""

      if (!this.scanLocked) {
        this.lockedSnapshot = ""
        this.lockedFaceCrop = ""
      }

      if (!incomingCameraEnabled && allowTurnOffReset) {
        this.stopResultLoop()
        this.hardResetUiState()
        return
      }

      if (this.scanLocked) {
        await this.fetchLockedImagesIfNeeded(false)
      }
    },

    async fetchLockedImagesIfNeeded(force = false) {
      if (this.destroyed) return
      if (!this.cameraRunning) return
      if (!this.scanLocked && !force) {
        this.lockedSnapshot = ""
        this.lockedFaceCrop = ""
        return
      }
      if (this.isFetchingLockedImages) return

      this.isFetchingLockedImages = true
      try {
        const res = await getLockedImages(this.activeCameraId)
        this.clearFaceServiceError()
        if (res?.scan_locked) {
          this.lockedSnapshot = res.locked_snapshot || ""
          this.lockedFaceCrop = res.locked_face_crop || ""
        } else {
          this.lockedSnapshot = ""
          this.lockedFaceCrop = ""
        }
      } catch (e) {
        this.handleFaceServiceError(e, { polling: true })
      } finally {
        this.isFetchingLockedImages = false
      }
    },

    startResultLoop() {
      this.stopResultLoop()
      this.resultTimer = setInterval(async () => {
        if (this.destroyed) return
        if (!this.cameraRunning) return
        if (this.busyResult) return
        this.busyResult = true
        try {
          await this.refreshResult()
        } finally {
          this.busyResult = false
        }
      }, 500)
    },

    stopResultLoop() {
      if (this.resultTimer) {
        clearInterval(this.resultTimer)
        this.resultTimer = null
      }
      this.busyResult = false
    },

    clearResultStateOnly() {
      this.employeeId = ""
      this.trackingActive = false
      this.identityConfirmed = false
      this.faceMatch = false
      this.confirmCount = 0
      this.distance = null
      this.bbox = null
      this.timeoutState = false
      this.alert = false
      this.lockedSnapshot = ""
      this.lockedFaceCrop = ""
      this.scanLocked = false
      this.lockReason = ""
      this.fps = 0
      this.message = ""
      this.lastUpdate = ""
    },

    hardResetUiState() {
      this.cameraRunning = false
      this.cameraConnected = false
      this.currentIp = ""
      this.clearResultStateOnly()
    },

    clearFaceServiceError() {
      this.faceServiceError = null
    },

    handleFaceServiceError(error, { polling = false } = {}) {
      const normalized = normalizeFaceApiError(error)
      if (normalized.cancelled || this.destroyed) return
      this.faceServiceError = { code: normalized.code, message: normalized.message }
      this.cameraConnected = false
      if (shouldStopFacePolling(normalized)) {
        this.stopResultLoop()
      }
      if (!polling) {
        alert(normalized.message)
      }
    },

    mountDirectPreview(url) {
      const cleanUrl = String(url || "").trim()
      if (!cleanUrl) return
      if (this.previewRetryTimer) {
        clearTimeout(this.previewRetryTimer)
        this.previewRetryTimer = null
      }
      this.directCameraSourceUrl = cleanUrl
      this.directCameraUrl = this.buildDirectCameraUrl(cleanUrl)
      this.directCameraKey += 1
      this.previewHealthy = false
      this.previewRetryCount = 0
      this.previewRunning = true
    },

    resetDirectPreview() {
      if (this.previewRetryTimer) {
        clearTimeout(this.previewRetryTimer)
        this.previewRetryTimer = null
      }
      this.directCameraUrl = ""
      this.directCameraSourceUrl = ""
      this.directCameraKey += 1
      this.previewHealthy = false
      this.previewRetryCount = 0
      this.previewRunning = false
    },

    mountRegisteredPreview(camera, sourceUrl) {
      const previewUrl = String(camera?.urlView || "").trim()
      const directWebUrl = /^https?:\/\//i.test(sourceUrl || "") ? String(sourceUrl).trim() : ""
      let browserUrl = previewUrl || directWebUrl

      if (previewUrl) {
        try {
          const parsed = new URL(previewUrl, window.location.origin)
          if (parsed.pathname.endsWith("/stream.html")) {
            parsed.searchParams.set("mode", "mse,webrtc")
            browserUrl = parsed.toString()
          }
        } catch {
          browserUrl = previewUrl
        }
      }

      if (!browserUrl) {
        throw new Error("Camera chưa có URL preview cho trình duyệt.")
      }
      this.mountDirectPreview(browserUrl)
    },

    buildDirectCameraUrl(inputUrl) {
      const raw = String(inputUrl || "").trim()
      if (!raw) return ""
      const sep = raw.includes("?") ? "&" : "?"
      return `${raw}${sep}t=${Date.now()}`
    },

    handleDirectPreviewLoaded() {
      this.previewHealthy = true
      this.previewRetryCount = 0
    },

    handleDirectPreviewError() {
      this.previewHealthy = false
      this.message = "Không nhận được hình ảnh camera. Kiểm tra địa chỉ và go2rtc."
      if (!this.previewRunning || !this.directCameraSourceUrl || this.previewRetryCount >= 1) return
      this.previewRetryCount += 1
      this.previewRetryTimer = setTimeout(() => {
        this.previewRetryTimer = null
        if (!this.previewRunning || !this.directCameraSourceUrl) return
        this.directCameraUrl = this.buildDirectCameraUrl(this.directCameraSourceUrl)
        this.directCameraKey += 1
      }, 1500)
    },

    async handleDoubleClick() {
      try {
        const el = this.$refs.videoWrapperRef
        if (!el) return
        if (!document.fullscreenElement) {
          if (el.requestFullscreen) await el.requestFullscreen()
          else if (el.webkitRequestFullscreen) await el.webkitRequestFullscreen()
          else if (el.msRequestFullscreen) await el.msRequestFullscreen()
        } else {
          if (document.exitFullscreen) await document.exitFullscreen()
          else if (document.webkitExitFullscreen) await document.webkitExitFullscreen()
          else if (document.msExitFullscreen) await document.msExitFullscreen()
        }
      } catch (error) {
        console.warn("Fullscreen error:", error)
      }
    },

    handleRightClick(event) {
      event?.preventDefault?.()
    }
  }
}
</script>

<style scoped>
.face-page {
  padding: 20px 24px 28px;
  min-height: 100%;
  display: flex;
  flex-direction: column;
  gap: 18px;
  max-width: 1560px;
  margin: 0 auto;
}

/* Header */
.face-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
}
.face-kicker {
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--text-secondary, #94a3b8);
}
.face-title {
  margin: 4px 0 0;
  font-size: clamp(24px, 3vw, 32px);
  font-weight: 900;
  letter-spacing: -0.02em;
}
.face-status-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  border-radius: 999px;
  background: var(--bg-primary, #fff);
  border: 1px solid var(--border-color, #e2e8f0);
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--text-secondary, #475569);
}
.pill-dot {
  width: 9px;
  height: 9px;
  border-radius: 50%;
}
.dot-on { background: #22c55e; box-shadow: 0 0 0 3px rgba(34,197,94,0.2); }
.dot-off { background: #94a3b8; }

/* Control bar */
.control-bar {
  display: flex;
  align-items: flex-end;
  gap: 18px;
  flex-wrap: wrap;
  padding: 14px 18px;
  border-radius: 16px;
  background: var(--bg-primary, #fff);
  border: 1px solid var(--border-color, #e2e8f0);
  box-shadow: 0 1px 3px rgba(15,23,42,0.05);
}
.camera-picker {
  flex: 1 1 260px;
  min-width: 240px;
}
.picker-label {
  display: block;
  font-size: 0.78rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--text-secondary, #64748b);
  margin-bottom: 6px;
}
.picker-control { position: relative; }
.picker-input {
  width: 100%;
  padding: 11px 14px;
  border-radius: 12px;
  border: 1px solid var(--border-color, #cbd5e1);
  background: var(--bg-input, #f8fafc);
  font-size: 0.95rem;
  outline: none;
  transition: border-color 150ms ease, box-shadow 150ms ease;
}
.picker-input:focus {
  border-color: var(--accent-primary, #2563eb);
  box-shadow: 0 0 0 3px rgba(37,99,235,0.15);
}
.picker-dropdown {
  position: absolute;
  top: calc(100% + 6px);
  left: 0; right: 0;
  z-index: 40;
  background: var(--bg-primary, #fff);
  border: 1px solid var(--border-color, #e2e8f0);
  border-radius: 12px;
  max-height: 280px;
  overflow-y: auto;
  box-shadow: 0 12px 32px rgba(15,23,42,0.18);
}
.picker-option {
  padding: 10px 14px;
  cursor: pointer;
  transition: background 120ms ease;
}
.picker-option:hover { background: var(--bg-hover, #f1f5f9); }
.option-name { font-weight: 700; font-size: 0.92rem; }
.option-meta { font-size: 0.78rem; color: var(--text-secondary, #64748b); margin-top: 2px; }
.picker-hint { padding: 10px 14px; font-size: 0.85rem; color: var(--text-secondary, #64748b); }

.control-actions {
  display: flex;
  gap: 10px;
}
.start-btn {
  min-width: 150px;
  padding: 11px 22px;
  font-size: 0.98rem;
  font-weight: 800;
  border-radius: 12px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}
.stop-btn {
  padding: 11px 20px;
  font-size: 0.95rem;
  font-weight: 700;
  border-radius: 12px;
}
.btn-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255,255,255,0.4);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

/* Main stage */
.main-stage {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 320px;
  gap: 18px;
  flex: 1;
  min-height: 0;
}
@media (max-width: 980px) {
  .main-stage { grid-template-columns: 1fr; }
}

.stage-video {
  min-height: 0;
  display: flex;
}
.video-frame {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 9;
  min-height: 360px;
  border-radius: 18px;
  overflow: hidden;
  background: #0b1120;
  border: 1px solid var(--border-color, #1e293b);
  transition: box-shadow 200ms ease, border-color 200ms ease;
  cursor: crosshair;
}
.video-frame.frame-live {
  border-color: rgba(34,197,94,0.6);
  box-shadow: 0 0 0 3px rgba(34,197,94,0.12), 0 20px 50px rgba(0,0,0,0.25);
}
.video {
  width: 100%;
  height: 100%;
  border: 0;
  display: block;
}
.video-placeholder {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  color: #64748b;
}
.placeholder-icon { font-size: 46px; opacity: 0.5; }
.placeholder-text { font-size: 0.95rem; font-weight: 600; }
.video-toast {
  position: absolute;
  top: 14px; left: 50%;
  transform: translateX(-50%);
  background: rgba(2,6,23,0.8);
  color: #fff;
  padding: 8px 18px;
  border-radius: 999px;
  font-size: 0.85rem;
  font-weight: 700;
}

/* Info panel */
.stage-info {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 0;
  overflow-y: auto;
}
.info-card {
  padding: 18px;
  border-radius: 16px;
  background: var(--bg-primary, #fff);
  border: 1px solid var(--border-color, #e2e8f0);
  box-shadow: 0 1px 3px rgba(15,23,42,0.05);
}
.info-title {
  margin: 0 0 14px;
  font-size: 0.85rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--text-secondary, #64748b);
}
.big-id {
  font-family: "JetBrains Mono", "SFMono-Regular", Consolas, monospace;
  font-size: 2.1rem;
  font-weight: 900;
  letter-spacing: 0.02em;
  padding: 10px 14px;
  border-radius: 12px;
  background: var(--bg-input, #f1f5f9);
  border: 1px solid var(--border-color, #e2e8f0);
  text-align: center;
  transition: all 200ms ease;
}
.big-id.id-hit {
  background: rgba(34,197,94,0.12);
  border-color: rgba(34,197,94,0.5);
  color: #16a34a;
}
.big-id.id-empty { color: var(--text-secondary, #94a3b8); }
.id-caption {
  text-align: center;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.12em;
  color: var(--text-secondary, #64748b);
  margin-top: 6px;
  margin-bottom: 14px;
}
.info-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 8px 0;
  border-top: 1px solid var(--border-color, #eef2f7);
  font-size: 0.88rem;
}
.info-row > span:first-child { color: var(--text-secondary, #64748b); }
.info-row .value { font-weight: 800; }
.value.val-hit { color: #16a34a; }
.value.val-locked { color: #dc2626; }
.value.val-verify { color: #eab308; }
.value.val-track { color: #2563eb; }
.value.dim { color: var(--text-secondary, #94a3b8); font-weight: 600; font-size: 0.82rem; }
.dim { color: var(--text-secondary, #94a3b8); }

.evidence-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
}
.evidence-img {
  width: 100%;
  aspect-ratio: 1/1;
  object-fit: cover;
  border-radius: 10px;
  border: 1px solid var(--border-color, #e2e8f0);
  background: #0b1120;
}

.error-box {
  padding: 11px 14px;
  border-radius: 12px;
  background: rgba(220,38,38,0.08);
  border: 1px solid rgba(220,38,38,0.3);
  color: var(--accent-danger, #dc2626);
  font-weight: 700;
  font-size: 0.88rem;
}
.toast-box {
  padding: 11px 14px;
  border-radius: 12px;
  background: var(--bg-input, #f1f5f9);
  border: 1px solid var(--border-color, #e2e8f0);
  color: var(--text-secondary, #475569);
  font-size: 0.88rem;
}
</style>
