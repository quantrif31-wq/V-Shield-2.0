<template>
  <div class="page">
    <div class="topbar">
      <div>
        <h1>Giám sát trực tiếp</h1>
        <p>Theo dõi camera realtime</p>
      </div>

      <button class="gear-btn" @click="toggleSettings" type="button" aria-label="Mở cài đặt camera">
        ⚙
      </button>
    </div>

    <transition name="fade">
      <div v-if="showSettings" class="settings-panel">
        <h3>Chọn camera (tối đa 4)</h3>

        <div class="cam-list">
          <div v-for="cam in cameras" :key="cam.cameraId" class="cam-item">
            <div class="cam-item-info">
              <span>{{ cam.cameraName }}</span>
              <label class="switch rec-toggle" title="Ghi hình liên tục">
                <input type="checkbox" :checked="cam.isRecordingEnabled" @change="toggleRec($event, cam)" />
                <span class="slider"></span>
              </label>
              <router-link :to="{ name: 'CameraArchive', params: { id: cam.cameraId } }" class="archive-link" title="Xem lưu trữ">📁</router-link>
            </div>
            <label class="switch">
              <input type="checkbox" v-model="selectedMap[cam.cameraId]" />
              <span class="slider"></span>
            </label>
          </div>
        </div>
      </div>
    </transition>

    <div class="grid">
      <div v-for="cam in activeCams" :key="cam.cameraId" class="cam-card">
        <div class="cam-head">
          <span>{{ cam.cameraName }}</span>
          <span class="status" :class="isHealthy(cam.cameraId) ? 'ok' : 'wait'">
            {{ isHealthy(cam.cameraId) ? "LIVE" : "LOADING..." }}
          </span>
        </div>

        <div class="cam-preview">
          <iframe
            v-if="resolvedPreviewUrl(cam)"
            :src="resolvedPreviewUrl(cam)"
            class="preview"
            @load="onLoad(cam.cameraId)"
            @error="onError(cam.cameraId)"
          ></iframe>
          <div v-else class="cam-off">Camera tắt</div>
        </div>
      </div>

      <div v-if="!activeCams.length" class="empty">Chưa chọn camera</div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from "vue"
import { getCameras, toggleRecording } from "../services/cameraRuntimeApi"

const cameras = ref([])
const selectedMap = reactive({})
const showSettings = ref(false)
const previewHealthById = reactive({})
const previewSeedById = reactive({})

const initPreviewState = (cameraId) => {
  if (!Object.prototype.hasOwnProperty.call(previewHealthById, cameraId)) {
    previewHealthById[cameraId] = false
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

const resolvedPreviewUrl = (cam) => {
  const base = normalizeUrl(cam?.urlView)
  if (!base) return ""
  initPreviewState(cam.cameraId)
  const sep = base.includes("?") ? "&" : "?"
  return `${base}${sep}t=${previewSeedById[cam.cameraId]}`
}

const isHealthy = (cameraId) => !!previewHealthById[cameraId]

const onLoad = (cameraId) => {
  previewHealthById[cameraId] = true
}

const onError = (cameraId) => {
  previewHealthById[cameraId] = false
}

const activeCams = computed(() =>
  cameras.value.filter((camera) => selectedMap[camera.cameraId]).slice(0, 4)
)

const enforceSelectLimit = () => {
  const selectedIds = Object.keys(selectedMap).filter((id) => selectedMap[id])
  if (selectedIds.length <= 4) return
  const overflowId = selectedIds[selectedIds.length - 1]
  selectedMap[overflowId] = false
  alert("Chỉ tối đa 4 camera")
}

watch(
  () => ({ ...selectedMap }),
  () => {
    enforceSelectLimit()
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
        selectedMap[cam.cameraId] = index < 4
      }
      initPreviewState(cam.cameraId)
    })
  } catch (error) {
    console.error("Lỗi load camera:", error)
  }
}

const toggleSettings = () => {
  showSettings.value = !showSettings.value
}

const toggleRec = async (event, cam) => {
  const enabled = event.target.checked
  try {
    const res = await toggleRecording(cam.cameraId, enabled, null)
    cam.isRecordingEnabled = !!res?.isRecordingEnabled
    cam.recordingRetentionDays = Number(res?.recordingRetentionDays || cam.recordingRetentionDays || 30)
  } catch (e) {
    event.target.checked = !enabled
    console.error("Lỗi bật/tắt ghi hình:", e)
  }
}

onMounted(() => {
  loadCameras()
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
  font-size: 22px;
  background: var(--surface-default);
  border: none;
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
}

.cam-list {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.cam-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 10px;
  margin: -8px -10px;
  border-radius: var(--radius-control);
  transition: background var(--transition-fast);
}

.cam-item:hover {
  background: var(--surface-hover);
}

.cam-item-info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.rec-toggle {
  transform: scale(0.7);
}

.archive-link {
  font-size: 18px;
  text-decoration: none;
  cursor: pointer;
  transition: opacity var(--transition-fast);
}

.archive-link:hover {
  opacity: 0.7;
}

.switch {
  position: relative;
  width: 46px;
  height: 24px;
}

.switch input {
  display: none;
}

.slider {
  position: absolute;
  background: var(--interactive-disabled);
  border-radius: 24px;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  transition: 0.3s;
}

.slider::before {
  content: "";
  position: absolute;
  width: 18px;
  height: 18px;
  left: 3px;
  top: 3px;
  background: var(--surface-default);
  border-radius: 50%;
  transition: 0.3s;
}

.switch input:checked + .slider {
  background: #2563eb;
}

.switch input:checked + .slider::before {
  transform: translateX(22px);
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
}

.cam-head {
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
  font-weight: 600;
}

.status.ok {
  color: var(--status-success-text);
}

.status.wait {
  color: var(--accent-warning);
}

.cam-preview {
  width: 100%;
  aspect-ratio: 16/9;
  background: black;
  border-radius: 12px;
  overflow: hidden;
}

.preview {
  width: 100%;
  height: 100%;
  border: none;
}

.cam-off {
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-on-interactive);
  height: 100%;
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
</style>
