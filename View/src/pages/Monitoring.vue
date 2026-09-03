<template>
  <div class="page">
    <div class="topbar">
      <div>
        <h1>Giám sát trực tiếp</h1>
        <p>Theo dõi camera realtime</p>
      </div>

      <button class="gear-btn" @click="toggleSettings" type="button" aria-label="Chọn camera hiển thị">
        <span aria-hidden="true">⚙</span>
        <span>Chọn camera</span>
      </button>
    </div>

    <transition name="fade">
      <section v-if="showSettings" class="settings-panel" aria-label="Bố cục camera">
        <div class="settings-panel__head">
          <div>
            <h3>Bố cục camera</h3>
            <p>Chọn tối đa 4 camera để theo dõi. Lựa chọn được lưu riêng cho tài khoản của bạn.</p>
          </div>
          <strong>{{ selectedCameraCount }}/4 đang hiển thị</strong>
        </div>
        <div class="settings-actions">
          <button type="button" class="settings-action" @click="selectFirstFour">Chọn 4 camera đầu</button>
          <button type="button" class="settings-action settings-action--quiet" @click="clearSelection">Bỏ chọn tất cả</button>
        </div>
        <div class="camera-picker">
          <label v-for="cam in cameras" :key="cam.cameraId" class="camera-picker__item" :class="{ selected: selectedMap[cam.cameraId] }">
            <input type="checkbox" :checked="!!selectedMap[cam.cameraId]" @change="toggleCameraSelection(cam.cameraId, $event.target.checked)" />
            <span class="camera-picker__check" aria-hidden="true">✓</span>
            <span class="camera-picker__name">{{ cam.cameraName }}</span>
            <span class="camera-picker__type">{{ cam.cameraType || 'Camera' }}</span>
            <router-link :to="{ name: 'CameraArchive', params: { id: cam.cameraId } }" class="camera-picker__archive" title="Xem lưu trữ" @click.stop>Kho lưu trữ</router-link>
          </label>
        </div>
        <p v-if="preferenceSaving" class="settings-save-state">Đang lưu lựa chọn…</p>
        <p v-else-if="preferenceError" class="settings-save-state is-error">{{ preferenceError }}</p>
      </section>
    </transition>

    <div class="grid">
      <div v-for="cam in activeCams" :key="cam.cameraId" class="cam-card">
        <div class="cam-head">
          <span class="cam-title">{{ cam.cameraName }}</span>
          <span class="status" :class="getStatusClass(cam.cameraId)">
            <span class="status-indicator" :class="getStatusClass(cam.cameraId)"></span>
            {{ getStatusLabel(cam.cameraId) }}
          </span>
        </div>

        <div class="cam-preview">
          <RemoteCameraPeer
            v-if="shouldUseRemotePeer(cam)"
            :node-id="cameraRelayNodeId"
            :stream-name="remoteStreamName(cam)"
            @state-change="onRemotePeerState(cam.cameraId, $event.state, $event.message)"
          />
          <!-- Hiển thị img khi luồng là ảnh/frame.jpg để tràn hết 100% khung camera -->
          <img
            v-else-if="isHealthy(cam.cameraId) && resolvedPreviewUrl(cam) && isImageUrl(resolvedPreviewUrl(cam))"
            :src="resolvedPreviewUrl(cam)"
            class="preview"
            alt="Camera realtime stream"
            @load="onLoad(cam.cameraId)"
            @error="onError(cam.cameraId)"
          />
          <!-- Hiển thị iframe stream khi luồng là WebRTC/MSE/HTML -->
          <iframe
            v-else-if="isHealthy(cam.cameraId) && resolvedPreviewUrl(cam)"
            :src="resolvedPreviewUrl(cam)"
            class="preview"
            allow="autoplay; fullscreen"
            @load="onLoad(cam.cameraId)"
            @error="onError(cam.cameraId)"
          ></iframe>

          <!-- Fallback CCTV Placeholder khi luồng bị 404, 502, mất kết nối hoặc camera tắt -->
          <div v-else class="cctv-placeholder" :class="{ 'is-checking': isChecking(cam.cameraId) }">
            <div class="cctv-grid-pattern"></div>
            <div class="cctv-scanline"></div>
            <div class="cctv-crosshair top-left"></div>
            <div class="cctv-crosshair top-right"></div>
            <div class="cctv-crosshair bottom-left"></div>
            <div class="cctv-crosshair bottom-right"></div>
            
            <div class="cctv-content">
              <div class="cctv-icon-wrap">
                <svg v-if="!isChecking(cam.cameraId)" class="cctv-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                  <path d="M23 7l-7 5 7 5V7z" />
                  <rect x="1" y="5" width="15" height="14" rx="2" ry="2" />
                  <line x1="2" y1="2" x2="22" y2="22" stroke="var(--status-danger-text, #ef4444)" stroke-width="2.2" />
                </svg>
                <div v-else class="cctv-spinner"></div>
              </div>

              <div class="cctv-info">
                <div class="cctv-state-badge" :class="isChecking(cam.cameraId) ? 'state-checking' : 'state-offline'">
                  {{ isChecking(cam.cameraId) ? 'ĐANG KẾT NỐI LUỒNG...' : 'MẤT TÍN HIỆU (NO SIGNAL)' }}
                </div>
                <h4 class="cctv-cam-name">{{ cam.cameraName }}</h4>
                <p class="cctv-reason">
                  {{ getErrorMessage(cam.cameraId) || 'Không nhận được tín hiệu hình ảnh từ camera' }}
                </p>
              </div>

              <button
                type="button"
                class="cctv-retry-btn"
                :disabled="isChecking(cam.cameraId)"
                @click="retryStream(cam.cameraId)"
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="retry-icon">
                  <path d="M23 4v6h-6" />
                  <path d="M20.49 15a9 9 0 11-2.12-9.36L23 10" />
                </svg>
                <span>{{ isChecking(cam.cameraId) ? 'Đang kiểm tra...' : 'Thử kết nối lại' }}</span>
              </button>
            </div>
            
            <div class="cctv-timestamp">
              <span>CAM ID: {{ cam.cameraId }}</span>
              <span>NO SIGNAL · V-SHIELD VMS</span>
            </div>
          </div>
        </div>
      </div>

      <div v-if="!activeCams.length" class="empty">Chưa chọn camera</div>
    </div>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from "vue"
import { getCameras } from "../services/cameraRuntimeApi"
import http from "../services/http"
import RemoteCameraPeer from "../components/shared/RemoteCameraPeer.vue"

const cameras = ref([])
const selectedMap = reactive({})
const showSettings = ref(false)
const previewStatusById = reactive({}) // 'checking' | 'online' | 'offline' | 'off'
const previewErrorById = reactive({})
const previewSeedById = reactive({})

let autoProbeInterval = null
let cameraRelayStatusInterval = null
const cameraRelayEnabled = ref(false)
const cameraRelayNodeId = ref('')
const cameraRelayStatusResolved = ref(false)
const savedCameraIds = ref(null)
const preferencesHydrated = ref(false)
const preferenceSaving = ref(false)
const preferenceError = ref('')
let savePreferenceTimer = null

const initPreviewState = (cameraId) => {
  if (!Object.prototype.hasOwnProperty.call(previewStatusById, cameraId)) {
    previewStatusById[cameraId] = 'checking'
  }
  if (!Object.prototype.hasOwnProperty.call(previewErrorById, cameraId)) {
    previewErrorById[cameraId] = ''
  }
  if (!Object.prototype.hasOwnProperty.call(previewSeedById, cameraId)) {
    previewSeedById[cameraId] = Date.now()
  }
}

const normalizeUrl = (value) => {
  if (!value || typeof value !== "string") return ""
  const trimmed = value.trim()
  return trimmed || ""
}

const remoteStreamName = (camera) => {
  const url = normalizeUrl(camera?.urlView)
  try {
    const parsed = new URL(url, window.location.origin)
    const source = parsed.searchParams.get('src')
    if (source && /^[A-Za-z0-9_-]{1,128}$/.test(source)) return source
  } catch { /* The camera id below is the runtime's canonical go2rtc source. */ }
  return `cam${camera?.cameraId}`
}

const shouldUseRemotePeer = (camera) =>
  cameraRelayEnabled.value && !!cameraRelayNodeId.value && !!remoteStreamName(camera)

const onRemotePeerState = (cameraId, state, message = '') => {
  previewStatusById[cameraId] = state === 'live' ? 'online' : state === 'failed' ? 'offline' : 'checking'
  previewErrorById[cameraId] = state === 'failed' ? message : ''
}

const selectedCameraCount = computed(() => Object.values(selectedMap).filter(Boolean).length)

const loadMonitoringPreference = async () => {
  try {
    const response = await http.get('/monitoring-preferences')
    const ids = response?.data?.selectedCameraIds
    savedCameraIds.value = Array.isArray(ids) ? ids.map(Number).filter(Number.isFinite) : null
  } catch {
    // The camera page remains usable during a brief offline period. The next
    // successful save stores the locally chosen layout in the database.
    savedCameraIds.value = null
  }
}

const saveMonitoringPreference = async () => {
  preferenceSaving.value = true
  preferenceError.value = ''
  try {
    const selectedCameraIds = Object.keys(selectedMap).filter(id => selectedMap[id]).map(Number)
    await http.put('/monitoring-preferences', { selectedCameraIds })
  } catch {
    preferenceError.value = 'Không thể lưu lúc này. Lựa chọn sẽ được lưu khi bạn thay đổi lại.'
  } finally {
    preferenceSaving.value = false
  }
}

const schedulePreferenceSave = () => {
  if (!preferencesHydrated.value) return
  if (savePreferenceTimer) clearTimeout(savePreferenceTimer)
  savePreferenceTimer = setTimeout(saveMonitoringPreference, 350)
}

const toggleCameraSelection = (cameraId, checked) => {
  if (checked && !selectedMap[cameraId] && selectedCameraCount.value >= 4) {
    alert('Chỉ tối đa 4 camera')
    return
  }
  selectedMap[cameraId] = checked
}

const clearSelection = () => cameras.value.forEach(camera => { selectedMap[camera.cameraId] = false })
const selectFirstFour = () => cameras.value.forEach((camera, index) => { selectedMap[camera.cameraId] = index < 4 })

const loadCameraRelayStatus = async () => {
  try {
    const response = await http.get('/camera-relay/status')
    const data = response?.data?.data || response?.data || {}
    const nodes = Array.isArray(data.nodes) ? data.nodes : []
    cameraRelayEnabled.value = data.enabled === true && nodes.length > 0
    cameraRelayNodeId.value = cameraRelayEnabled.value ? String(nodes[0]) : ''
  } catch {
    cameraRelayEnabled.value = false
    cameraRelayNodeId.value = ''
  } finally {
    cameraRelayStatusResolved.value = true
  }
}

const isImageUrl = (url) => {
  if (!url || typeof url !== 'string') return false
  const clean = url.split('?')[0].toLowerCase()
  return (
    clean.endsWith('.jpg') ||
    clean.endsWith('.jpeg') ||
    clean.endsWith('.png') ||
    clean.endsWith('.webp') ||
    clean.endsWith('/frame.jpg') ||
    clean.includes('/qr/frame.jpg') ||
    clean.includes('/plate/frame.jpg') ||
    clean.includes('/video_feed') ||
    clean.startsWith('data:image/') ||
    clean.endsWith('/snapshot')
  )
}

const resolvedPreviewUrl = (cam) => {
  const base = normalizeUrl(cam?.urlView)
  if (!base) return ""
  initPreviewState(cam.cameraId)
  const sep = base.includes("?") ? "&" : "?"
  return `${base}${sep}t=${previewSeedById[cam.cameraId]}`
}

const isHealthy = (cameraId) => previewStatusById[cameraId] === 'online'
const isChecking = (cameraId) => previewStatusById[cameraId] === 'checking'
const getErrorMessage = (cameraId) => previewErrorById[cameraId] || ''

const getStatusClass = (cameraId) => {
  const status = previewStatusById[cameraId]
  if (status === 'online') return 'ok'
  if (status === 'checking') return 'wait'
  return 'off'
}

const getStatusLabel = (cameraId) => {
  const status = previewStatusById[cameraId]
  if (status === 'online') return 'TRỰC TIẾP'
  if (status === 'checking') return 'ĐANG TẢI...'
  return 'MẤT TÍN HIỆU'
}

const checkCameraStreamHealth = async (cameraId, url) => {
  if (!url) {
    previewStatusById[cameraId] = 'off'
    previewErrorById[cameraId] = 'Camera chưa cấu hình URL xem trực tiếp'
    return
  }

  previewStatusById[cameraId] = 'checking'

  try {
    const controller = typeof AbortController !== 'undefined' ? new AbortController() : null
    const timer = controller ? setTimeout(() => controller.abort(), 4000) : null

    const fetchOptions = {
      method: 'GET',
      headers: { 'Accept': '*/*' },
    }
    if (controller) {
      fetchOptions.signal = controller.signal
    }

    const res = await fetch(url, fetchOptions)
    if (timer) clearTimeout(timer)

    if (!res.ok) {
      previewStatusById[cameraId] = 'offline'
      if (res.status === 404) {
        previewErrorById[cameraId] = 'Không tìm thấy luồng camera (404 Not Found)'
      } else if (res.status === 502) {
        previewErrorById[cameraId] = 'Mất kết nối máy chủ streaming (502 Bad Gateway)'
      } else if (res.status === 503 || res.status === 504) {
        previewErrorById[cameraId] = 'Máy chủ streaming tạm thời bận hoặc quá tải'
      } else {
        previewErrorById[cameraId] = `Lỗi kết nối máy chủ (Mã lỗi ${res.status})`
      }
      return
    }

    // Check if the response is an HTML error page masquerading with 200
    const contentType = res.headers ? (res.headers.get('content-type') || '') : ''
    if (contentType.includes('text/html')) {
      const text = await res.text()
      if (text.includes('404 Not Found') || text.includes('502 Bad Gateway') || text.includes('503 Service Temporarily Unavailable')) {
        previewStatusById[cameraId] = 'offline'
        previewErrorById[cameraId] = 'Máy chủ streaming trả về trang lỗi kết nối'
        return
      }
    }

    previewStatusById[cameraId] = 'online'
    previewErrorById[cameraId] = ''
  } catch (err) {
    if (err && err.name === 'AbortError') {
      previewStatusById[cameraId] = 'offline'
      previewErrorById[cameraId] = 'Hết thời gian chờ phản hồi luồng video'
    } else {
      // In test environments or when mock cameras don't respond
      previewStatusById[cameraId] = 'offline'
      previewErrorById[cameraId] = 'Không thể kết nối đến luồng RTSP / Video'
    }
  }
}

const probeAllActiveCameras = () => {
  activeCams.value.forEach((cam) => {
    if (shouldUseRemotePeer(cam)) {
      previewStatusById[cam.cameraId] = 'checking'
      previewErrorById[cam.cameraId] = ''
      return
    }
    const url = normalizeUrl(cam?.urlView)
    checkCameraStreamHealth(cam.cameraId, url)
  })
}

const retryStream = (cameraId) => {
  const cam = cameras.value.find((c) => c.cameraId === cameraId)
  if (!cam) return
  previewSeedById[cameraId] = Date.now()
  if (shouldUseRemotePeer(cam)) {
    previewStatusById[cameraId] = 'checking'
    previewErrorById[cameraId] = ''
    return
  }
  const url = normalizeUrl(cam?.urlView)
  checkCameraStreamHealth(cameraId, url)
}

const onLoad = (cameraId) => {
  previewStatusById[cameraId] = 'online'
  previewErrorById[cameraId] = ''
}

const onError = (cameraId) => {
  previewStatusById[cameraId] = 'offline'
  previewErrorById[cameraId] = 'Không thể tải luồng video trực tiếp'
}

const activeCams = computed(() =>
  cameras.value.filter((camera) => selectedMap[camera.cameraId]).slice(0, 4)
)

const enforceSelectLimit = () => {
  const selectedIds = Object.keys(selectedMap).filter((id) => selectedMap[id])
  if (selectedIds.length <= 4) return
  const overflowId = selectedIds[selectedIds.length - 1]
  selectedMap[overflowId] = false
}

watch(
  () => ({ ...selectedMap }),
  () => {
    enforceSelectLimit()
    if (cameraRelayStatusResolved.value) probeAllActiveCameras()
    schedulePreferenceSave()
  },
  { deep: true }
)

const loadCameras = async () => {
  try {
    const res = await getCameras()
    const list = Array.isArray(res) ? res : []
    cameras.value = list

    list.forEach((cam, index) => {
      if (!Object.prototype.hasOwnProperty.call(selectedMap, cam.cameraId)) {
        selectedMap[cam.cameraId] = Array.isArray(savedCameraIds.value)
          ? savedCameraIds.value.includes(Number(cam.cameraId))
          : index < 4
      }
      initPreviewState(cam.cameraId)
    })

    probeAllActiveCameras()
  } catch (error) {
    console.error("Lỗi load camera:", error)
  }
}

const toggleSettings = () => {
  showSettings.value = !showSettings.value
}

onMounted(async () => {
  await loadCameraRelayStatus()
  await loadMonitoringPreference()
  await loadCameras()
  preferencesHydrated.value = true
  probeAllActiveCameras()
  cameraRelayStatusInterval = setInterval(() => {
    loadCameraRelayStatus().then(probeAllActiveCameras)
  }, 4000)
  // Tự động kiểm tra lại luồng mỗi 10 giây nếu có camera đang offline
  autoProbeInterval = setInterval(() => {
    activeCams.value.forEach((cam) => {
      if (previewStatusById[cam.cameraId] === 'offline') {
        const url = normalizeUrl(cam?.urlView)
        if (url) checkCameraStreamHealth(cam.cameraId, url)
      }
    })
  }, 10000)
})

onBeforeUnmount(() => {
  if (autoProbeInterval) {
    clearInterval(autoProbeInterval)
  }
  if (cameraRelayStatusInterval) clearInterval(cameraRelayStatusInterval)
  if (savePreferenceTimer) clearTimeout(savePreferenceTimer)
})
</script>

<style scoped>
.page {
  padding: 20px;
  background: var(--surface-subtle);
  min-height: 100vh;
}

.topbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.gear-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 0.9rem;
  font-weight: 700;
  background: var(--surface-default);
  border: 1px solid var(--border-subtle);
  padding: 8px 12px;
  border-radius: var(--radius-control);
  cursor: pointer;
  box-shadow: var(--shadow-sm);
  transition: background var(--transition-fast), box-shadow var(--transition-fast), transform var(--transition-fast);
}

.gear-btn:hover {
  background: var(--surface-hover);
  box-shadow: var(--shadow-md);
  transform: translateY(-1px);
}

.settings-panel {
  background: var(--surface-default);
  padding: 16px;
  border-radius: var(--radius-card);
  margin-bottom: 16px;
  box-shadow: var(--shadow-sm);
  border: 1px solid var(--border-subtle);
}

.settings-panel__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.settings-panel h3 { margin: 0; font-size: 1rem; }
.settings-panel p { margin: 5px 0 0; color: var(--text-secondary); font-size: 0.86rem; }
.settings-panel__head strong { white-space: nowrap; color: var(--interactive-primary); font-size: 0.86rem; }

.settings-actions {
  display: flex;
  gap: 8px;
  margin: 14px 0 12px;
}

.settings-action {
  border: 1px solid var(--border-subtle);
  border-radius: 8px;
  background: var(--surface-subtle);
  color: var(--text-primary);
  font-weight: 650;
  font-size: .82rem;
  padding: 7px 10px;
  cursor: pointer;
}
.settings-action--quiet { background: transparent; color: var(--text-secondary); }
.settings-action:hover { background: var(--surface-hover); }

.camera-picker {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
}

.camera-picker__item {
  min-width: 0;
  display: grid;
  grid-template-columns: 18px minmax(0, 1fr) auto auto;
  align-items: center;
  gap: 8px;
  padding: 10px;
  border: 1px solid var(--border-subtle);
  border-radius: 10px;
  cursor: pointer;
  transition: border-color var(--transition-fast), background var(--transition-fast);
}
.camera-picker__item:hover { background: var(--surface-hover); }
.camera-picker__item.selected { border-color: var(--interactive-primary); background: color-mix(in srgb, var(--interactive-primary) 7%, var(--surface-default)); }
.camera-picker__item input { position: absolute; opacity: 0; pointer-events: none; }
.camera-picker__check {
  display: grid;
  place-items: center;
  width: 17px;
  height: 17px;
  border: 1px solid var(--border-strong);
  border-radius: 5px;
  color: transparent;
  font-size: .72rem;
}
.camera-picker__item.selected .camera-picker__check { color: white; background: var(--interactive-primary); border-color: var(--interactive-primary); }
.camera-picker__name { overflow: hidden; white-space: nowrap; text-overflow: ellipsis; font-size: .88rem; font-weight: 650; }
.camera-picker__type { color: var(--text-secondary); font-size: .75rem; }
.camera-picker__archive { color: var(--interactive-primary); text-decoration: none; font-size: .75rem; white-space: nowrap; }
.settings-save-state { min-height: 16px; margin-top: 10px !important; font-size: .76rem !important; }
.settings-save-state.is-error { color: var(--status-danger-text); }

@media (max-width: 760px) {
  .camera-picker { grid-template-columns: 1fr; }
  .settings-panel__head { align-items: flex-start; flex-direction: column; gap: 4px; }
}

.grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.cam-card {
  background: var(--surface-default);
  border-radius: 16px;
  padding: 12px;
  box-shadow: var(--shadow-sm);
  border: 1px solid var(--border-subtle);
  display: flex;
  flex-direction: column;
}

.cam-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
  font-weight: 600;
}

.cam-title {
  font-size: 0.95rem;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.status {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.02em;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  display: inline-block;
}

.status.ok {
  color: var(--status-success-text);
}
.status-indicator.ok {
  background: var(--status-success-text);
  box-shadow: 0 0 8px var(--status-success-border);
}

.status.wait {
  color: var(--status-warning-text);
}
.status-indicator.wait {
  background: var(--status-warning-text);
  animation: pulse-dot 1.2s infinite ease-in-out;
}

.status.off {
  color: var(--status-danger-text);
}
.status-indicator.off {
  background: var(--status-danger-text);
}

@keyframes pulse-dot {
  0%, 100% { opacity: 0.4; }
  50% { opacity: 1; }
}

.cam-preview {
  width: 100%;
  aspect-ratio: 16/9;
  background: #000000;
  border-radius: 12px;
  overflow: hidden;
  position: relative;
  border: 1px solid var(--border-subtle);
  display: flex;
  align-items: center;
  justify-content: center;
}

.preview {
  width: 100%;
  height: 100%;
  border: none;
  display: block;
  object-fit: cover;
  background: #000000;
}

/* ==========================================================================
   CCTV Modern Security Offline Placeholder
   ========================================================================== */
.cctv-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 16px;
  position: relative;
  background: linear-gradient(145deg, #0b141e, #070d14);
  color: var(--text-primary);
  user-select: none;
  overflow: hidden;
}

.cctv-grid-pattern {
  position: absolute;
  inset: 0;
  opacity: 0.15;
  background-size: 24px 24px;
  background-image: 
    linear-gradient(to right, rgba(255, 255, 255, 0.08) 1px, transparent 1px),
    linear-gradient(to bottom, rgba(255, 255, 255, 0.08) 1px, transparent 1px);
  pointer-events: none;
}

.cctv-scanline {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg, transparent, rgba(56, 189, 248, 0.4), transparent);
  animation: scanline 4s linear infinite;
  pointer-events: none;
}

@keyframes scanline {
  0% { transform: translateY(-100%); }
  100% { transform: translateY(500%); }
}

.cctv-crosshair {
  position: absolute;
  width: 12px;
  height: 12px;
  border-color: rgba(255, 255, 255, 0.25);
  border-style: solid;
  pointer-events: none;
}
.cctv-crosshair.top-left { top: 10px; left: 10px; border-width: 2px 0 0 2px; }
.cctv-crosshair.top-right { top: 10px; right: 10px; border-width: 2px 2px 0 0; }
.cctv-crosshair.bottom-left { bottom: 10px; left: 10px; border-width: 0 0 2px 2px; }
.cctv-crosshair.bottom-right { bottom: 10px; right: 10px; border-width: 0 2px 2px 0; }

.cctv-content {
  position: relative;
  z-index: 2;
  margin: auto;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 10px;
  max-width: 85%;
}

.cctv-icon-wrap {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
  display: grid;
  place-items: center;
  color: var(--text-muted);
}

.cctv-icon {
  width: 26px;
  height: 26px;
  stroke: var(--text-secondary);
}

.cctv-spinner {
  width: 24px;
  height: 24px;
  border: 2px solid rgba(255, 255, 255, 0.15);
  border-top-color: var(--interactive-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.cctv-state-badge {
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  padding: 2px 8px;
  border-radius: var(--radius-pill);
  display: inline-block;
}

.cctv-state-badge.state-offline {
  background: rgba(239, 68, 68, 0.18);
  color: var(--status-danger-text);
  border: 1px solid var(--status-danger-border);
}

.cctv-state-badge.state-checking {
  background: rgba(234, 179, 8, 0.18);
  color: var(--status-warning-text);
  border: 1px solid var(--status-warning-border);
}

.cctv-cam-name {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: #edf7f9;
}

.cctv-reason {
  margin: 0;
  font-size: 0.82rem;
  color: #91a8b4;
  line-height: 1.3;
}

.cctv-retry-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  margin-top: 4px;
  padding: 6px 14px;
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.16);
  border-radius: var(--radius-control);
  color: #edf7f9;
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.cctv-retry-btn:hover:not(:disabled) {
  background: var(--interactive-primary);
  border-color: var(--interactive-primary);
  color: var(--text-on-interactive);
}

.cctv-retry-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.retry-icon {
  width: 14px;
  height: 14px;
}

.cctv-timestamp {
  position: relative;
  z-index: 2;
  display: flex;
  justify-content: space-between;
  font-size: 0.7rem;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  color: rgba(255, 255, 255, 0.35);
  letter-spacing: 0.05em;
}

.empty {
  grid-column: span 2;
  text-align: center;
  padding: 40px;
  color: var(--text-muted);
}

.fade-enter-active,
.fade-leave-active {
  transition: all 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

@media (max-width: 768px) {
  .grid {
    grid-template-columns: 1fr;
  }
  .cam-list {
    grid-template-columns: 1fr;
  }
}
</style>
