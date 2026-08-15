<template>
  <div class="face-page animate-in">
    <header class="face-header">
      <div class="face-header-titles">
        <span class="face-kicker">Kiểm soát ra vào</span>
        <h1 class="face-title">Nhận diện khuôn mặt · Cổng {{ activeGateName || '—' }}</h1>
      </div>
      <div class="face-header-actions">
        <button class="tab-btn" :class="{ active: tab === 'scan' }" @click="tab = 'scan'">Quét</button>
        <button class="tab-btn" :class="{ active: tab === 'intruders' }" @click="tab = 'intruders'">
          Kẻ xâm nhập <span v-if="intruderCount" class="tab-badge">{{ intruderCount }}</span>
        </button>
        <span class="status-pill" :class="cameraRunning ? 'on' : 'off'">
          <i class="dot"></i>{{ cameraRunning ? 'Đang quét' : 'Sẵn sàng' }}
        </span>
      </div>
    </header>

    <!-- ============ TAB QUÉT ============ -->
    <template v-if="tab === 'scan'">
      <div class="control-bar">
        <div class="gate-picker">
          <label class="field-label">Chọn cổng</label>
          <select class="gate-select" v-model="selectedGateId" :disabled="loading" @change="onGateChange">
            <option :value="null" disabled>— Chọn cổng —</option>
            <option v-for="g in gates" :key="g.gateId" :value="g.gateId">
              {{ g.gateName }}{{ g.location ? ' · ' + g.location : '' }}
            </option>
          </select>
        </div>

        <div class="camera-picker">
          <label class="field-label">Camera</label>
          <select class="gate-select" v-model="cameraSearch" :disabled="loading" @change="onCameraChange">
            <option value="" disabled>— Chọn camera —</option>
            <option v-for="cam in allCameras" :key="cam.cameraId" :value="cam.cameraName">
              {{ cam.cameraName }} · {{ cam.cameraId }}
            </option>
          </select>
        </div>

        <div class="control-actions">
          <button class="btn btn-primary start-btn" :disabled="loading || !cameraIp" @click="handleStartOrReset">
            <span v-if="loading" class="btn-spinner"></span>
            <span>{{ loading ? 'Đang xử lý…' : (cameraRunning ? 'Reset' : 'Bắt đầu') }}</span>
          </button>
          <button class="btn btn-outline stop-btn" :disabled="loading || !cameraRunning" @click="handleTurnOff">Dừng</button>
        </div>
      </div>

      <div class="main-stage">
        <div class="stage-video">
          <div ref="videoWrapperRef" class="video-frame" :class="cameraRunning ? 'frame-live' : 'frame-idle'"
               @dblclick="handleDoubleClick">
            <iframe
              v-if="previewRunning && directCameraUrl"
              :key="directCameraKey"
              :src="directCameraUrl"
              class="video"
              title="Camera"
              allow="autoplay; fullscreen"
              frameborder="0"
            ></iframe>
            <div v-else class="video-placeholder">
              <div class="placeholder-icon">◉</div>
              <div class="placeholder-text">Chọn cổng + camera, bấm Bắt đầu</div>
            </div>

            <!-- Multi-face overlay: green confirmed / yellow tracking / red intruder -->
            <div
              v-for="(f, idx) in liveFaces"
              :key="idx"
              class="face-box"
              :class="faceBoxClass(f)"
              :style="faceBoxStyle(f)"
            >
              <span class="face-id" :class="faceIdClass(f)">
                {{ f.employee_id ? prefixId(f.employee_id, f.known) : '???' }}
              </span>
            </div>

            <div v-if="cameraRunning && !previewRunning" class="video-toast">Đang kết nối…</div>
          </div>
        </div>

        <aside class="stage-info">
          <section class="info-card">
            <h2 class="info-title">Kết quả</h2>
            <div class="big-id" :class="identityConfirmed ? 'id-hit' : 'id-empty'">
              {{ confirmedEmployeeId || '— — — —' }}
            </div>
            <div class="id-caption">Mã nhân viên đã xác nhận</div>

            <div class="info-row"><span>Trạng thái</span><span class="value" :class="'val-' + detectionState">{{ detectionLabel }}</span></div>
            <div class="info-row"><span>Độ khớp</span><span class="value">{{ distanceText }}</span></div>
            <div class="info-row"><span>Gương mặt trong khung</span><span class="value">{{ liveFaces.length }}</span></div>
            <div class="info-row"><span>Cổng</span><span class="value">{{ activeGateName || '—' }}</span></div>
            <div class="info-row"><span>FPS</span><span class="value">{{ fps }}</span></div>
            <div class="info-row" v-if="lastUpdate"><span>Cập nhật</span><span class="value dim">{{ lastUpdate }}</span></div>
          </section>

          <section class="info-card" v-if="liveFaces.length">
            <h2 class="info-title">Gương mặt trong khung</h2>
            <div v-for="(f, i) in liveFaces" :key="'f' + i" class="face-mini" :class="f.allowed ? 'mini-ok' : 'mini-denied'">
              <span>{{ f.employee_id ? prefixId(f.employee_id, f.known) : '???' }}</span>
              <span class="mini-dist">{{ f.distance != null ? f.distance.toFixed(3) : '—' }}</span>
            </div>
          </section>

          <div v-if="faceServiceError" class="error-box">{{ faceServiceError.message }}</div>
          <div v-if="message && !faceServiceError" class="toast-box">{{ message }}</div>
        </aside>
      </div>
    </template>

    <!-- ============ TAB KẺ XÂM NHẬP ============ -->
    <template v-else>
      <div class="intruder-toolbar">
        <div class="filter-chips">
          <button class="chip" :class="{ active: intruderFilter === '' }" @click="loadIntruders('')">Tất cả</button>
          <button class="chip" :class="{ active: intruderFilter === 'unknown' }" @click="loadIntruders('unknown')">Không nhận diện</button>
          <button class="chip" :class="{ active: intruderFilter === 'denied' }" @click="loadIntruders('denied')">Từ chối</button>
          <button class="chip" :class="{ active: intruderFilter === 'blacklist' }" @click="loadIntruders('blacklist')">Danh sách đen</button>
        </div>
        <button class="btn btn-outline" :disabled="intruders.length === 0" @click="clearAllIntruders">Xóa tất cả</button>
      </div>

      <div v-if="intruders.length === 0" class="empty-state">
        <div class="empty-icon">🛡</div>
        <div>Chưa có kẻ xâm nhập nào được ghi nhận.</div>
      </div>

      <div class="intruder-grid" v-else>
        <div v-for="item in intruders" :key="item.id" class="intruder-card" :class="'card-' + item.reason">
          <div class="intruder-photo">
            <img v-if="item.faceCropBase64" :src="item.faceCropBase64" alt="Kẻ xâm nhập" />
            <img v-else-if="item.snapshotBase64" :src="item.snapshotBase64" alt="Snapshot" />
            <div v-else class="photo-empty">Không có ảnh</div>
            <span class="intruder-badge" :class="'badge-' + item.reason">{{ badgeLabel(item.reason) }}</span>
          </div>
          <div class="intruder-body">
            <div class="intruder-title">
              {{ item.employeeName || (item.employeeId ? 'NV-' + item.employeeId : '#' + (item.id || item.cameraId || '')) }}
            </div>
            <div class="intruder-meta">
              <span v-if="item.reason === 'blacklist'">⚠ {{ item.reasonDetail || 'Danh sách đen' }}</span>
              <span v-else-if="item.reason === 'denied'">⛔ {{ item.reasonDetail || 'Không có quyền vào cổng' }}</span>
              <span v-else>👤 Không nhận diện được</span>
            </div>
            <div class="intruder-foot">
              <span>{{ item.gateName || 'Cổng —' }}</span>
              <span>{{ fmtTime(item.occurredAtUtc) }}</span>
            </div>
            <button class="btn btn-outline btn-sm intruder-del" @click="deleteOneIntruder(item.id)">Xóa</button>
          </div>
        </div>
      </div>
    </template>

    <!-- ===== POPUP XÁC NHẬN MẬT KHẨU (đổi cổng) ===== -->
    <div v-if="showPasswordModal" class="modal-backdrop" @click.self="showPasswordModal = false">
      <div class="modal-box">
        <h3>Đổi cổng</h3>
        <p>Nhập lại mật khẩu của bạn để xác nhận chuyển sang cổng <strong>{{ pendingGateName }}</strong>.</p>
        <input v-model="passwordInput" type="password" class="modal-input" placeholder="Mật khẩu"
               @keydown.enter="confirmPassword" autofocus />
        <div v-if="passwordError" class="error-box modal-error">{{ passwordError }}</div>
        <div class="modal-actions">
          <button class="btn btn-primary" :disabled="passwordVerifying || !passwordInput" @click="confirmPassword">
            {{ passwordVerifying ? 'Đang xác thực…' : 'Xác nhận' }}
          </button>
          <button class="btn btn-outline" :disabled="passwordVerifying" @click="showPasswordModal = false">Hủy</button>
        </div>
      </div>
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
import {
  getFaceGates,
  verifyFaceGatePassword,
  checkGateAccess,
  recordFaceGateResult,
  getFaceIntruders,
  deleteFaceIntruder
} from "../services/faceGateApi"
import { captureError, recordMetric } from "../services/observability"

export default {
  name: "FaceIdSecurity",

  props: {
    cameraId: { type: String, default: "monitoring-face-camera" },
    laneId: { type: String, default: null }
  },

  data() {
    return {
      tab: "scan",
      // gate
      gates: [],
      selectedGateId: null,
      activeGateName: "",
      showPasswordModal: false,
      pendingGateId: null,
      pendingGateName: "",
      passwordInput: "",
      passwordError: "",
      passwordVerifying: false,

      // camera
      cameraIp: "",
      currentIp: "",
      allCameras: [],
      cameraSearch: "",
      cameraRunning: false,
      cameraConnected: false,
      previewRunning: false,
      loading: false,

      // recognition
      employeeId: "",
      confirmedEmployeeId: "",
      trackingActive: false,
      identityConfirmed: false,
      faceMatch: false,
      confirmCount: 0,
      distance: null,
      bbox: null,
      faces: [],
      scanLocked: false,
      lockReason: "",
      fps: 0,
      message: "",
      lastUpdate: "",
      faceServiceError: null,

      // intruders
      intruders: [],
      intruderCount: 0,
      intruderFilter: "",
      intruderTimer: null,

      directCameraUrl: "",
      directCameraSourceUrl: "",
      directCameraKey: 0,
      previewHealthy: false,
      previewRetryCount: 0,
      previewRetryTimer: null,
      resultTimer: null,
      busyResult: false,
      destroyed: false
    }
  },

  computed: {
    activeCameraId() {
      return this.cameraId
    },

    liveFaces() {
      return (this.faces || []).map(f => ({
        ...f,
        allowed: !!f.match,
        known: !!f.employee_id,
        status: f.status || (f.match ? "confirmed" : "intruder")
      }))
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
      const n = Number(this.distance)
      if (Number.isNaN(n)) return "— — — —"
      return n.toFixed(4)
    }
  },

  async mounted() {
    this.destroyed = false
    await Promise.all([this.loadGates(), this.loadAllCameras()])
    await this.loadCurrentStatus()
    if (this.cameraRunning) this.startResultLoop()
    await this.loadIntruders("")
    this.startIntruderLoop()
  },

  beforeUnmount() {
    this.destroyed = true
    this.stopResultLoop()
    this.stopIntruderLoop()
    this.resetDirectPreview()
  },

  methods: {
    faceBoxClass(f) {
      if (f.status === "confirmed") return "box-ok"
      if (f.status === "intruder") return "box-denied"
      return "box-pending"
    },

    faceIdClass(f) {
      if (f.status === "confirmed") return "id-ok"
      if (f.status === "intruder") return "id-denied"
      return "id-pending"
    },

    prefixId(id, known) {
      return known ? `NV-${id}` : `KH-${id}`
    },
    fmtTime(v) {
      if (!v) return ""
      try {
        return new Date(v).toLocaleString("vi-VN", { hour: "2-digit", minute: "2-digit", day: "2-digit", month: "2-digit" })
      } catch { return v }
    },
    badgeLabel(reason) {
      return { unknown: "Không nhận diện", denied: "Từ chối", blacklist: "Danh sách đen" }[reason] || reason
    },
    faceBoxStyle(f) {
      const b = f.bbox || {}
      const w = 480
      const h = 270
      const leftPct = (b.left / w) * 100
      const topPct = (b.top / h) * 100
      const widthPct = ((b.right - b.left) / w) * 100
      const heightPct = ((b.bottom - b.top) / h) * 100
      return { left: leftPct + "%", top: topPct + "%", width: widthPct + "%", height: heightPct + "%" }
    },

    // ---------- gates ----------
    async loadGates() {
      try {
        const res = await getFaceGates()
        this.gates = Array.isArray(res?.gates) ? res.gates : []
      } catch (e) {
        console.warn("load gates error:", e)
      }
    },

    onGateChange() {
      const gate = this.gates.find(g => g.gateId === this.selectedGateId)
      if (!gate) return
      this.pendingGateId = gate.gateId
      this.pendingGateName = gate.gateName
      this.passwordInput = ""
      this.passwordError = ""
      this.showPasswordModal = true
    },

    async confirmPassword() {
      this.passwordVerifying = true
      this.passwordError = ""
      try {
        await verifyFaceGatePassword(this.passwordInput)
        this.activeGateName = this.pendingGateName
        this.showPasswordModal = false
        this.passwordInput = ""
        this.message = `Đã chọn cổng ${this.pendingGateName}. Nhấn Bắt đầu để quét.`
        this.stopResultLoop()
        this.clearResultStateOnly()
        this.resetDirectPreview()
        this.cameraRunning = false
      } catch (e) {
        this.passwordError = e?.response?.data?.message || e?.message || "Mật khẩu không đúng."
      } finally {
        this.passwordVerifying = false
      }
    },

    // ---------- camera ----------
    async loadAllCameras() {
      try {
        const list = await getCameras()
        this.allCameras = Array.isArray(list) ? list : []
      } catch (e) {
        console.warn("load cameras error:", e)
        this.allCameras = []
      }
    },

    async onCameraChange() {
      const cam = (this.allCameras || []).find(c => c.cameraName === this.cameraSearch)
      if (!cam) return
      this.cameraIp = cam.streamUrl || cam.urlView || ""
      try {
        const camera = await ensureCameraRegistered({
          cameraName: cam.cameraName,
          cameraType: "Face",
          streamUrl: cam.streamUrl || cam.urlView || "",
        })
        if (camera?.urlView) this.mountRegisteredPreview(camera, cam.streamUrl || "")
      } catch (e) {
        console.warn("camera preview error:", e)
      }
      this.stopResultLoop()
      this.clearResultStateOnly()
      this.resetDirectPreview()
      this.cameraRunning = false
      this.message = `Đã chọn camera ${cam.cameraName}`
    },

    async handleStartOrReset() {
      const ip = (this.cameraIp || "").trim()
      if (!ip) { alert("Vui lòng chọn camera trước"); return }
      if (!this.activeGateName) { alert("Vui lòng chọn cổng trước"); return }

      const startedAt = performance.now()
      try {
        this.loading = true
        if (!this.previewRunning) {
          const camera = await ensureCameraRegistered({
            cameraName: this.cameraSearch || "Face Monitor Camera",
            cameraType: "Face",
            streamUrl: ip,
          })
          this.mountRegisteredPreview(camera, ip)
        }

        this.clearResultStateOnly()

        if (!this.cameraRunning) {
          this.stopResultLoop()
          const res = await startCamera(this.activeCameraId, ip, this.laneId)
          this.clearFaceServiceError()
          if (!res?.success) { alert(res?.message || "Không thể bắt đầu"); return }
          this.cameraRunning = true
          this.message = "Đã bắt đầu quét"
          await this.refreshResult()
          this.startResultLoop()
          return
        }

        const res = await resetCamera(this.activeCameraId)
        this.clearFaceServiceError()
        this.message = res?.message || "Đã reset phiên"
        await this.refreshResult()
        if (!this.resultTimer) this.startResultLoop()
      } catch (e) {
        captureError(e, "face_gate_start_failure", { component: "FaceCamera" })
        this.handleFaceServiceError(e)
      } finally {
        recordMetric("face_gate_start", performance.now() - startedAt, { component: "FaceCamera" })
        this.loading = false
      }
    },

    async handleTurnOff() {
      try {
        this.loading = true
        this.stopResultLoop()
        try {
          const res = await stopCamera(this.activeCameraId)
          this.clearFaceServiceError()
          this.message = res?.message || "Đã dừng quét"
        } catch (e) {
          if (e?.status === 404) { this.clearFaceServiceError(); this.message = "Camera đã dừng" }
          else this.handleFaceServiceError(e)
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
        if (this.currentIp && !this.previewRunning) {
          this.mountDirectPreview(this.currentIp)
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
      this.cameraRunning = !!res.camera_enabled
      this.cameraConnected = !!res.camera_connected
      this.confirmedEmployeeId = res.employee_id || ""
      this.trackingActive = !!res.tracking_active
      this.identityConfirmed = !!res.identity_confirmed
      this.faceMatch = !!res.face_match
      this.confirmCount = Number(res.confirm_count || 0)
      this.distance = res.distance ?? null
      this.bbox = res.bbox || null
      this.faces = Array.isArray(res.faces) ? res.faces : []
      this.scanLocked = !!res.scan_locked
      this.lockReason = res.lock_reason || ""
      this.fps = Number(res.fps || 0)
      this.message = res.message || ""
      this.lastUpdate = res.last_update || ""

      await this.resolveFaces()

      if (!this.cameraRunning && allowTurnOffReset) {
        this.stopResultLoop()
        this.hardResetUiState()
      }
    },

    async resolveFaces() {
      if (!this.cameraRunning) return
      const seen = new Set()
      for (const f of this.faces || []) {
        if (!f.employee_id || seen.has(f.employee_id)) continue
        seen.add(f.employee_id)
        const empId = Number(f.employee_id)
        const gateId = this.selectedGateId || null
        try {
          const acc = await checkGateAccess(empId, gateId)
          if (acc?.success) {
            const decision = acc.blacklist ? "blacklist"
              : acc.allowed === true ? "allowed"
              : acc.allowed === false ? "denied" : "unknown"
            await recordFaceGateResult({
              decision,
              employeeId: empId,
              employeeName: acc.employeeName,
              gateId,
              gateName: this.activeGateName,
              laneId: this.laneId ? Number(this.laneId) : null,
              cameraId: this.activeCameraId,
              reasonDetail: acc.blacklistReason || acc.reason,
              distance: f.distance ?? null
            })
            if (decision !== "allowed") {
              await this.loadIntruderCount()
            }
          }
        } catch (e) {
          console.warn("resolve face access error:", e)
        }
      }
    },

    // ---------- intruders ----------
    async loadIntruders(filter) {
      this.intruderFilter = filter
      try {
        const res = await getFaceIntruders({ reason: filter || undefined, page: 1, pageSize: 100 })
        this.intruders = Array.isArray(res?.items) ? res.items : []
        this.intruderCount = Number(res?.total || this.intruders.length)
      } catch (e) {
        console.warn("load intruders error:", e)
      }
    },

    async loadIntruderCount() {
      try {
        const res = await getFaceIntruders({ page: 1, pageSize: 1 })
        this.intruderCount = Number(res?.total || 0)
      } catch (e) { /* ignore */ }
    },

    async deleteOneIntruder(id) {
      try {
        await deleteFaceIntruder(id)
        await this.loadIntruders(this.intruderFilter)
      } catch (e) {
        console.warn("delete intruder error:", e)
      }
    },

    async clearAllIntruders() {
      if (!window.confirm("Xóa tất cả kẻ xâm nhập?")) return
      const ids = this.intruders.map(i => i.id)
      for (const id of ids) {
        try { await deleteFaceIntruder(id) } catch (e) { /* continue */ }
      }
      await this.loadIntruders(this.intruderFilter)
    },

    startIntruderLoop() {
      this.stopIntruderLoop()
      this.intruderTimer = setInterval(() => {
        if (this.destroyed) return
        this.loadIntruderCount()
      }, 5000)
    },

    stopIntruderLoop() {
      if (this.intruderTimer) { clearInterval(this.intruderTimer); this.intruderTimer = null }
    },

    // ---------- polling ----------
    startResultLoop() {
      this.stopResultLoop()
      this.resultTimer = setInterval(async () => {
        if (this.destroyed || !this.cameraRunning || this.busyResult) return
        this.busyResult = true
        try { await this.refreshResult() } finally { this.busyResult = false }
      }, 500)
    },

    stopResultLoop() {
      if (this.resultTimer) { clearInterval(this.resultTimer); this.resultTimer = null }
      this.busyResult = false
    },

    clearResultStateOnly() {
      this.employeeId = ""
      this.confirmedEmployeeId = ""
      this.trackingActive = false
      this.identityConfirmed = false
      this.faceMatch = false
      this.confirmCount = 0
      this.distance = null
      this.bbox = null
      this.faces = []
      this.scanLocked = false
      this.lockReason = ""
      this.fps = 0
      this.message = ""
      this.lastUpdate = ""
    },

    hardResetUiState() {
      this.cameraRunning = false
      this.cameraConnected = false
      this.clearResultStateOnly()
    },

    clearFaceServiceError() { this.faceServiceError = null },

    handleFaceServiceError(error, { polling = false } = {}) {
      const normalized = normalizeFaceApiError(error)
      if (normalized.cancelled || this.destroyed) return
      this.faceServiceError = { code: normalized.code, message: normalized.message }
      this.cameraConnected = false
      if (shouldStopFacePolling(normalized)) this.stopResultLoop()
      if (!polling) alert(normalized.message)
    },

    mountDirectPreview(url) {
      const clean = String(url || "").trim()
      if (!clean) return
      if (this.previewRetryTimer) { clearTimeout(this.previewRetryTimer); this.previewRetryTimer = null }
      this.directCameraSourceUrl = clean
      this.directCameraUrl = this.buildDirectCameraUrl(clean)
      this.directCameraKey += 1
      this.previewHealthy = false
      this.previewRetryCount = 0
      this.previewRunning = true
    },

    resetDirectPreview() {
      if (this.previewRetryTimer) { clearTimeout(this.previewRetryTimer); this.previewRetryTimer = null }
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
        } catch { browserUrl = previewUrl }
      }
      if (!browserUrl) throw new Error("Camera chưa có URL preview.")
      this.mountDirectPreview(browserUrl)
    },

    buildDirectCameraUrl(inputUrl) {
      const raw = String(inputUrl || "").trim()
      if (!raw) return ""
      const sep = raw.includes("?") ? "&" : "?"
      return `${raw}${sep}t=${Date.now()}`
    },

    async handleDoubleClick() {
      try {
        const el = this.$refs.videoWrapperRef
        if (!el) return
        if (!document.fullscreenElement) {
          if (el.requestFullscreen) await el.requestFullscreen()
          else if (el.webkitRequestFullscreen) await el.webkitRequestFullscreen()
        } else {
          if (document.exitFullscreen) await document.exitFullscreen()
          else if (document.webkitExitFullscreen) await document.webkitExitFullscreen()
        }
      } catch (e) { console.warn("fullscreen:", e) }
    }
  }
}
</script>

<style scoped>
.face-page { padding: 20px 24px 28px; min-height: 100%; display: flex; flex-direction: column; gap: 18px; max-width: 1560px; margin: 0 auto; }

.face-header { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; flex-wrap: wrap; }
.face-kicker { font-size: 0.78rem; font-weight: 700; letter-spacing: 0.16em; text-transform: uppercase; color: var(--text-secondary, #94a3b8); }
.face-title { margin: 4px 0 0; font-size: clamp(24px, 3vw, 32px); font-weight: 900; letter-spacing: -0.02em; }
.face-header-actions { display: flex; align-items: center; gap: 10px; }
.tab-btn { padding: 8px 16px; border-radius: 999px; border: 1px solid var(--border-color, #e2e8f0); background: var(--bg-primary, #fff); font-weight: 700; cursor: pointer; font-size: 0.88rem; position: relative; }
.tab-btn.active { background: var(--accent-primary, #2563eb); color: #fff; border-color: var(--accent-primary, #2563eb); }
.tab-badge { display: inline-flex; align-items: center; justify-content: center; min-width: 18px; height: 18px; border-radius: 999px; background: #dc2626; color: #fff; font-size: 0.7rem; margin-left: 4px; padding: 0 4px; }
.status-pill { display: inline-flex; align-items: center; gap: 7px; padding: 8px 14px; border-radius: 999px; background: var(--bg-primary, #fff); border: 1px solid var(--border-color, #e2e8f0); font-size: 0.82rem; font-weight: 700; }
.status-pill .dot { width: 9px; height: 9px; border-radius: 50%; background: #94a3b8; }
.status-pill.on .dot { background: #22c55e; box-shadow: 0 0 0 3px rgba(34,197,94,0.2); }

.control-bar { display: flex; align-items: flex-end; gap: 18px; flex-wrap: wrap; padding: 14px 18px; border-radius: 16px; background: var(--bg-primary, #fff); border: 1px solid var(--border-color, #e2e8f0); box-shadow: 0 1px 3px rgba(15,23,42,0.05); }
.gate-picker, .camera-picker { flex: 1 1 220px; min-width: 200px; }
.field-label { display: block; font-size: 0.78rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.08em; color: var(--text-secondary, #64748b); margin-bottom: 6px; }
.gate-select { width: 100%; padding: 11px 14px; border-radius: 12px; border: 1px solid var(--border-color, #cbd5e1); background: var(--bg-input, #f8fafc); font-size: 0.95rem; outline: none; }
.gate-select:focus { border-color: var(--accent-primary, #2563eb); box-shadow: 0 0 0 3px rgba(37,99,235,0.15); }
.control-actions { display: flex; gap: 10px; }
.start-btn { min-width: 140px; padding: 11px 20px; font-size: 0.98rem; font-weight: 800; border-radius: 12px; display: inline-flex; align-items: center; justify-content: center; gap: 8px; }
.stop-btn { padding: 11px 18px; font-size: 0.95rem; font-weight: 700; border-radius: 12px; }
.btn-spinner { width: 16px; height: 16px; border: 2px solid rgba(255,255,255,0.4); border-top-color: #fff; border-radius: 50%; animation: spin 0.7s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

.main-stage { display: grid; grid-template-columns: minmax(0, 1fr) 320px; gap: 18px; flex: 1; min-height: 0; }
@media (max-width: 980px) { .main-stage { grid-template-columns: 1fr; } }
.stage-video { min-height: 0; display: flex; }
.video-frame { position: relative; width: 100%; aspect-ratio: 16/9; min-height: 360px; border-radius: 18px; overflow: hidden; background: #0b1120; border: 1px solid var(--border-color, #1e293b); transition: box-shadow 200ms ease, border-color 200ms ease; }
.video-frame.frame-live { border-color: rgba(34,197,94,0.6); box-shadow: 0 0 0 3px rgba(34,197,94,0.12), 0 20px 50px rgba(0,0,0,0.25); }
.video { width: 100%; height: 100%; border: 0; display: block; }
.video-placeholder { position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 12px; color: #64748b; }
.placeholder-icon { font-size: 46px; opacity: 0.5; }
.placeholder-text { font-size: 0.95rem; font-weight: 600; }
.video-toast { position: absolute; top: 14px; left: 50%; transform: translateX(-50%); background: rgba(2,6,23,0.8); color: #fff; padding: 8px 18px; border-radius: 999px; font-size: 0.85rem; font-weight: 700; }

.face-box { position: absolute; border: 2px solid #22c55e; border-radius: 6px; pointer-events: none; z-index: 5; transition: border-color 120ms ease; }
.face-box.box-denied { border-color: #dc2626; }
.face-box.box-pending { border-color: #eab308; }
.face-id { position: absolute; top: -26px; left: -2px; padding: 2px 8px; border-radius: 6px; font-size: 0.72rem; font-weight: 800; white-space: nowrap; background: rgba(34,197,94,0.9); color: #fff; }
.face-id.id-denied { background: rgba(220,38,38,0.9); }
.face-id.id-pending { background: rgba(234,179,8,0.9); }

.stage-info { display: flex; flex-direction: column; gap: 14px; min-height: 0; overflow-y: auto; }
.info-card { padding: 18px; border-radius: 16px; background: var(--bg-primary, #fff); border: 1px solid var(--border-color, #e2e8f0); box-shadow: 0 1px 3px rgba(15,23,42,0.05); }
.info-title { margin: 0 0 14px; font-size: 0.85rem; font-weight: 800; text-transform: uppercase; letter-spacing: 0.08em; color: var(--text-secondary, #64748b); }
.big-id { font-family: "JetBrains Mono", Consolas, monospace; font-size: 2.1rem; font-weight: 900; letter-spacing: 0.02em; padding: 10px 14px; border-radius: 12px; background: var(--bg-input, #f1f5f9); border: 1px solid var(--border-color, #e2e8f0); text-align: center; transition: all 200ms ease; }
.big-id.id-hit { background: rgba(34,197,94,0.12); border-color: rgba(34,197,94,0.5); color: #16a34a; }
.big-id.id-empty { color: var(--text-secondary, #94a3b8); }
.id-caption { text-align: center; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.12em; color: var(--text-secondary, #64748b); margin-top: 6px; margin-bottom: 14px; }
.info-row { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 8px 0; border-top: 1px solid var(--border-color, #eef2f7); font-size: 0.88rem; }
.info-row > span:first-child { color: var(--text-secondary, #64748b); }
.info-row .value { font-weight: 800; }
.value.val-hit { color: #16a34a; }
.value.val-locked { color: #dc2626; }
.value.val-verify { color: #eab308; }
.value.val-track { color: #2563eb; }
.value.dim { color: var(--text-secondary, #94a3b8); font-weight: 600; }

.face-mini { display: flex; justify-content: space-between; gap: 10px; padding: 7px 0; border-top: 1px solid var(--border-color, #eef2f7); font-size: 0.85rem; font-weight: 700; }
.face-mini.mini-ok { color: #16a34a; }
.face-mini.mini-denied { color: #dc2626; }
.mini-dist { font-weight: 600; color: var(--text-secondary, #94a3b8); }

.error-box { padding: 11px 14px; border-radius: 12px; background: rgba(220,38,38,0.08); border: 1px solid rgba(220,38,38,0.3); color: var(--accent-danger, #dc2626); font-weight: 700; font-size: 0.88rem; }
.toast-box { padding: 11px 14px; border-radius: 12px; background: var(--bg-input, #f1f5f9); border: 1px solid var(--border-color, #e2e8f0); color: var(--text-secondary, #475569); font-size: 0.88rem; }

.intruder-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; }
.filter-chips { display: flex; gap: 8px; flex-wrap: wrap; }
.chip { padding: 8px 14px; border-radius: 999px; border: 1px solid var(--border-color, #e2e8f0); background: var(--bg-primary, #fff); font-weight: 700; font-size: 0.84rem; cursor: pointer; }
.chip.active { background: var(--accent-primary, #2563eb); color: #fff; border-color: var(--accent-primary, #2563eb); }
.empty-state { padding: 60px 20px; text-align: center; color: var(--text-secondary, #94a3b8); font-size: 1rem; }
.empty-icon { font-size: 40px; margin-bottom: 10px; }
.intruder-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 14px; }
.intruder-card { border-radius: 14px; overflow: hidden; background: var(--bg-primary, #fff); border: 1px solid var(--border-color, #e2e8f0); box-shadow: 0 1px 3px rgba(15,23,42,0.05); }
.intruder-card.card-unknown { border-top: 4px solid #64748b; }
.intruder-card.card-denied { border-top: 4px solid #dc2626; }
.intruder-card.card-blacklist { border-top: 4px solid #7c3aed; }
.intruder-photo { position: relative; height: 160px; background: #0b1120; overflow: hidden; }
.intruder-photo img { width: 100%; height: 100%; object-fit: cover; }
.photo-empty { position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; color: #64748b; font-size: 0.9rem; }
.intruder-badge { position: absolute; top: 8px; left: 8px; padding: 3px 10px; border-radius: 999px; font-size: 0.72rem; font-weight: 800; color: #fff; }
.badge-unknown { background: #64748b; }
.badge-denied { background: #dc2626; }
.badge-blacklist { background: #7c3aed; }
.intruder-body { padding: 12px 14px; }
.intruder-title { font-weight: 800; font-size: 1rem; }
.intruder-meta { font-size: 0.82rem; color: var(--text-secondary, #64748b); margin-top: 4px; }
.intruder-foot { display: flex; justify-content: space-between; font-size: 0.76rem; color: var(--text-secondary, #94a3b8); margin-top: 8px; }
.intruder-del { margin-top: 10px; width: 100%; font-size: 0.82rem; color: var(--accent-danger, #dc2626); border-color: rgba(220,38,38,0.35); }

.modal-backdrop { position: fixed; inset: 0; z-index: 1000; background: rgba(2,6,23,0.6); display: flex; align-items: center; justify-content: center; padding: 16px; }
.modal-box { background: var(--bg-primary, #fff); border: 1px solid var(--border-color, #e2e8f0); border-radius: 14px; padding: 22px; max-width: 420px; width: 100%; box-shadow: 0 20px 50px rgba(0,0,0,0.35); }
.modal-box h3 { margin: 0 0 10px; font-size: 1.15rem; }
.modal-box p { margin: 6px 0 14px; font-size: 0.92rem; color: var(--text-secondary, #64748b); }
.modal-input { width: 100%; padding: 11px 14px; border-radius: 12px; border: 1px solid var(--border-color, #cbd5e1); background: var(--bg-input, #f8fafc); font-size: 0.95rem; outline: none; }
.modal-input:focus { border-color: var(--accent-primary, #2563eb); box-shadow: 0 0 0 3px rgba(37,99,235,0.15); }
.modal-error { margin-top: 10px; }
.modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 18px; }
</style>
