<template>
  <div class="page">
    <div class="topbar">
      <div>
        <h1>Lưu trữ camera</h1>
        <p v-if="camera">Camera: {{ camera.cameraName }}</p>
      </div>
      <button class="back-btn" @click="$router.back()">← Quay lại</button>
    </div>

    <div class="filters">
      <label>Ngày bắt đầu:
        <input type="date" v-model="filterFrom" class="filter-input" />
      </label>
      <label>Ngày kết thúc:
        <input type="date" v-model="filterTo" class="filter-input" />
      </label>
      <button class="search-btn" @click="loadSegments(1)">Tìm</button>
    </div>

    <div v-if="segments.length === 0" class="empty">Chưa có đoạn ghi hình nào</div>

    <div class="segment-list">
      <div v-for="seg in segments" :key="seg.segmentId" class="segment-card">
        <div class="segment-info">
          <span class="time">{{ formatDate(seg.startedAt) }}</span>
          <span class="duration">{{ formatDuration(seg.durationSeconds) }}</span>
          <span class="size">{{ formatBytes(seg.fileSizeBytes) }}</span>
        </div>
        <video
          v-if="showVideoId === seg.segmentId"
          :src="seg.storageUrl"
          class="seg-video"
          controls
          autoplay
        ></video>
        <button class="play-btn" @click="play(seg.segmentId)">
          {{ showVideoId === seg.segmentId ? 'Ẩn' : 'Xem' }}
        </button>
      </div>
    </div>

    <div v-if="totalPages > 1" class="pagination">
      <button :disabled="page <= 1" @click="loadSegments(page - 1)">←</button>
      <span>{{ page }} / {{ totalPages }}</span>
      <button :disabled="page >= totalPages" @click="loadSegments(page + 1)">→</button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from "vue"
import { useRoute } from "vue-router"
import { getCameraById, getRecordedSegments } from "../services/cameraRuntimeApi"

const route = useRoute()
const cameraId = Number(route.params.id)
const camera = ref(null)
const segments = ref([])
const page = ref(1)
const total = ref(0)
const pageSize = 20
const filterFrom = ref("")
const filterTo = ref("")
const showVideoId = ref(null)

const totalPages = computed(() => Math.ceil(total.value / pageSize))

const loadCamera = async () => {
  try {
    camera.value = await getCameraById(cameraId)
  } catch (e) {
    console.error(e)
  }
}

const loadSegments = async (p = 1) => {
  page.value = p
  const params = { page: p, pageSize }
  if (filterFrom.value) params.from = new Date(filterFrom.value).toISOString()
  if (filterTo.value) params.to = new Date(filterTo.value).toISOString()
  try {
    const res = await getRecordedSegments(cameraId, params)
    segments.value = res.items || []
    total.value = res.total || 0
  } catch (e) {
    console.error(e)
    segments.value = []
  }
}

const formatDate = (d) => {
  if (!d) return ""
  const dt = new Date(d)
  return dt.toLocaleString("vi-VN")
}

const formatDuration = (sec) => {
  if (!sec) return ""
  const m = Math.floor(sec / 60)
  const s = Math.floor(sec % 60)
  return `${m}:${s.toString().padStart(2, "0")}`
}

const formatBytes = (b) => {
  if (!b) return ""
  const mb = b / (1024 * 1024)
  if (mb > 1000) return (mb / 1024).toFixed(1) + " GB"
  return mb.toFixed(1) + " MB"
}

const play = (id) => {
  showVideoId.value = showVideoId.value === id ? null : id
}

onMounted(() => {
  loadCamera()
  loadSegments()
})
</script>

<style scoped>
.page {
  padding: 20px;
  background: #f4f6fb;
  min-height: 100vh;
}

.topbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.back-btn {
  background: white;
  border: 1px solid #ddd;
  padding: 8px 16px;
  border-radius: 10px;
  cursor: pointer;
}

.filters {
  display: flex;
  gap: 12px;
  align-items: end;
  background: white;
  padding: 16px;
  border-radius: 14px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}

.filter-input {
  padding: 6px 10px;
  border: 1px solid #ccc;
  border-radius: 8px;
  margin-left: 6px;
}

.search-btn {
  background: #2563eb;
  color: white;
  border: none;
  padding: 8px 20px;
  border-radius: 10px;
  cursor: pointer;
}

.segment-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.segment-card {
  background: white;
  padding: 14px;
  border-radius: 14px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
}

.segment-info {
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
  font-size: 14px;
  color: #555;
}

.time {
  font-weight: 600;
  color: #222;
}

.seg-video {
  width: 100%;
  max-height: 400px;
  border-radius: 10px;
  margin-top: 8px;
}

.play-btn {
  background: #2563eb;
  color: white;
  border: none;
  padding: 6px 16px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 13px;
}

.empty {
  text-align: center;
  padding: 40px;
  color: gray;
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 12px;
  margin-top: 20px;
}

.pagination button {
  background: white;
  border: 1px solid #ddd;
  padding: 6px 14px;
  border-radius: 8px;
  cursor: pointer;
}

.pagination button:disabled {
  opacity: 0.4;
  cursor: default;
}
</style>
