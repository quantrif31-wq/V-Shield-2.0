<template>
  <div class="page">
    <!-- HEADER -->
    <div class="topbar">
      <div>
        <h1>🎥 Giám sát trực tiếp</h1>
        <p>Theo dõi camera realtime</p>
      </div>

      <!-- NÚT CÀI ĐẶT -->
      <button class="gear-btn" @click="toggleSettings">
        ⚙️
      </button>
    </div>

    <!-- PANEL CÀI ĐẶT -->
    <transition name="fade">
      <div v-if="showSettings" class="settings-panel">
        <h3>Chọn camera (tối đa 4)</h3>

        <div class="cam-list">
          <div
            v-for="cam in cameras"
            :key="cam.cameraId"
            class="cam-item"
          >
            <span>{{ cam.cameraName }}</span>

            <!-- TOGGLE -->
            <label class="switch">
              <input
                type="checkbox"
                v-model="selectedMap[cam.cameraId]"
              />
              <span class="slider"></span>
            </label>
          </div>
        </div>
      </div>
    </transition>

    <!-- GRID CAMERA -->
    <div class="grid">
      <div
        v-for="cam in activeCams"
        :key="cam.cameraId"
        class="cam-card"
      >
        <div class="cam-head">
          <span>{{ cam.cameraName }}</span>

          <span
            class="status"
            :class="cam.previewHealthy ? 'ok' : 'wait'"
          >
            {{ cam.previewHealthy ? "LIVE" : "LOADING..." }}
          </span>
        </div>

        <div class="cam-preview">
          <!-- FIX NULL URL -->
          <iframe
            v-if="cam.urlView"
            :src="buildUrl(cam.urlView)"
            class="preview"
            @load="onLoad(cam)"
            @error="onError(cam)"
          ></iframe>

          <div v-else class="cam-off">
            Camera OFF
          </div>
        </div>
      </div>

      <!-- EMPTY -->
      <div v-if="!activeCams.length" class="empty">
        Chưa chọn camera
      </div>
    </div>
  </div>
</template>

<script setup>
import {
  ref,
  reactive,
  computed,
  onMounted,
  watchEffect
} from "vue"

import { getCameras } from "../services/setcamAPI"

// ===== STATE =====
const cameras = ref([])
const selectedMap = reactive({})
const showSettings = ref(false)

// ===== LOAD CAMERA =====
const loadCameras = async () => {
  try {
    const res = await getCameras()
    cameras.value = res || []

    // chọn mặc định 4 cam đầu
    cameras.value.forEach((cam, index) => {
      selectedMap[cam.cameraId] = index < 4
    })
  } catch (err) {
    console.error("Lỗi load camera:", err)
  }
}

// ===== BUILD URL (FIX NULL + FIX MÀN ĐEN) =====
const buildUrl = (url) => {
  if (!url || typeof url !== "string") return ""

  const clean = url.trim()
  if (!clean) return ""

  const sep = clean.includes("?") ? "&" : "?"
  return clean + sep + "t=" + Date.now()
}

// ===== ACTIVE CAMERA =====
const activeCams = computed(() => {
  return cameras.value
    .filter(c => selectedMap[c.cameraId])
    .slice(0, 4)
    .map(c => ({
      ...c,
      previewHealthy: false
    }))
})

// ===== STATUS =====
const onLoad = (cam) => {
  cam.previewHealthy = true
}

const onError = (cam) => {
  cam.previewHealthy = false
}

// ===== LIMIT 4 CAMERA =====
watchEffect(() => {
  const selectedIds = Object.keys(selectedMap).filter(
    id => selectedMap[id]
  )

  if (selectedIds.length > 4) {
    const last = selectedIds[selectedIds.length - 1]
    selectedMap[last] = false
    alert("Chỉ tối đa 4 camera")
  }
})

// ===== TOGGLE SETTINGS =====
const toggleSettings = () => {
  showSettings.value = !showSettings.value
}

// ===== INIT =====
onMounted(() => {
  loadCameras()
})
</script>

<style scoped>
.page {
  padding: 20px;
  background: #f4f6fb;
  min-height: 100vh;
}

/* HEADER */
.topbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.gear-btn {
  font-size: 22px;
  background: white;
  border: none;
  padding: 8px 12px;
  border-radius: 10px;
  cursor: pointer;
  box-shadow: 0 4px 10px rgba(0,0,0,0.1);
}

/* SETTINGS */
.settings-panel {
  background: white;
  padding: 16px;
  border-radius: 14px;
  margin-bottom: 16px;
  box-shadow: 0 6px 18px rgba(0,0,0,0.05);
}

.cam-list {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
}

.cam-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

/* TOGGLE */
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
  background: #ccc;
  border-radius: 24px;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  transition: .3s;
}

.slider::before {
  content: "";
  position: absolute;
  width: 18px;
  height: 18px;
  left: 3px;
  top: 3px;
  background: white;
  border-radius: 50%;
  transition: .3s;
}

.switch input:checked + .slider {
  background: #2563eb;
}

.switch input:checked + .slider::before {
  transform: translateX(22px);
}

/* GRID */
.grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16px;
}

.cam-card {
  background: white;
  border-radius: 16px;
  padding: 12px;
  box-shadow: 0 8px 20px rgba(0,0,0,0.05);
}

.cam-head {
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
  font-weight: 600;
}

.status.ok {
  color: #16a34a;
}

.status.wait {
  color: #f97316;
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
  color: white;
  height: 100%;
}

.empty {
  grid-column: span 2;
  text-align: center;
  padding: 40px;
  color: gray;
}

/* ANIMATION */
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