<template>
  <div class="archive-page">
    <div class="archive-topbar">
      <div>
        <h1>Luu tru video an ninh</h1>
        <p>Loc nhanh theo camera, cong/khu vuc va moc thoi gian.</p>
      </div>
      <button class="ghost-btn" @click="$router.back()">Quay lai</button>
    </div>

    <section class="filter-panel">
      <div class="filter-grid">
        <label class="field">
          <span>Camera</span>
          <select v-model="filters.cameraId">
            <option value="">Tất cả camera</option>
            <option v-for="cam in cameras" :key="cam.cameraId" :value="String(cam.cameraId)">
              {{ cam.cameraName }}
            </option>
          </select>
        </label>

        <label class="field">
          <span>Cong / Gate</span>
          <select v-model="filters.gateId">
            <option value="">Tất cả cổng</option>
            <option v-for="gate in gateOptions" :key="gate.gateId" :value="String(gate.gateId)">
              {{ gate.label }}
            </option>
          </select>
        </label>

        <label class="field">
          <span>Loai camera</span>
          <select v-model="filters.cameraType">
            <option value="">Tất cả loại</option>
            <option v-for="type in cameraTypes" :key="type" :value="type">
              {{ type }}
            </option>
          </select>
        </label>

        <label class="field">
          <span>Tu khoa</span>
          <input v-model.trim="filters.search" type="text" placeholder="Ten camera, gate, khu vuc..." />
        </label>

        <label class="field">
          <span>Tu ngay</span>
          <input v-model="filters.from" type="date" />
        </label>

        <label class="field">
          <span>Den ngay</span>
          <input v-model="filters.to" type="date" />
        </label>
      </div>

      <div class="filter-actions">
        <button class="primary-btn" @click="loadSegments(1)">Tim video</button>
        <button class="ghost-btn" @click="resetFilters">Dat lai</button>
      </div>
    </section>

    <section class="summary-strip">
      <article class="summary-card">
        <span class="summary-kicker">Doan video</span>
        <strong>{{ total }}</strong>
      </article>
      <article class="summary-card">
        <span class="summary-kicker">Camera hien co</span>
        <strong>{{ cameras.length }}</strong>
      </article>
      <article class="summary-card">
        <span class="summary-kicker">Đang lọc</span>
        <strong>{{ activeFilterLabel }}</strong>
      </article>
    </section>

    <div v-if="loading" class="empty-state">Đang tải dữ liệu lưu trữ...</div>
    <div v-else-if="segments.length === 0" class="empty-state">
      Chưa có đoạn video nào khớp bộ lọc hiện tại.
    </div>

    <section v-else class="segment-list">
      <article v-for="seg in segments" :key="seg.segmentId" class="segment-card">
        <div class="segment-head">
          <div>
            <h2>{{ seg.cameraName || `Camera #${seg.cameraId}` }}</h2>
            <p>{{ seg.gateName || "Chưa gán cổng" }}<span v-if="seg.gateLocation"> · {{ seg.gateLocation }}</span></p>
          </div>
          <div class="segment-badges">
            <span class="badge">{{ seg.cameraType || "Không rõ loại" }}</span>
            <span class="badge time">{{ formatDate(seg.startedAt) }}</span>
          </div>
        </div>

        <div class="segment-meta">
          <span>Ket thuc: {{ formatDate(seg.endedAt) }}</span>
          <span>Thoi luong: {{ formatDuration(seg.durationSeconds) }}</span>
          <span>Dung luong: {{ formatBytes(seg.fileSizeBytes) }}</span>
        </div>

        <video
          v-if="showVideoId === seg.segmentId"
          :src="seg.storageUrl"
          class="segment-video"
          controls
          preload="metadata"
        ></video>

        <div class="segment-actions">
          <button class="primary-btn small" @click="toggleVideo(seg.segmentId)">
            {{ showVideoId === seg.segmentId ? "An video" : "Xem video" }}
          </button>
          <a class="ghost-btn small" :href="seg.storageUrl" target="_blank" rel="noreferrer">Mo file</a>
        </div>
      </article>
    </section>

    <div v-if="totalPages > 1" class="pagination">
      <button class="ghost-btn small" :disabled="page <= 1" @click="loadSegments(page - 1)">Trang truoc</button>
      <span>Trang {{ page }} / {{ totalPages }}</span>
      <button class="ghost-btn small" :disabled="page >= totalPages" @click="loadSegments(page + 1)">Trang sau</button>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from "vue"
import { useRoute } from "vue-router"
import { getArchiveSegments, getCameras } from "../services/cameraRuntimeApi"

const route = useRoute()
const pageSize = 20

function normalizeRouteCameraId(value) {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? String(parsed) : ""
}

const cameras = ref([])
const segments = ref([])
const loading = ref(false)
const total = ref(0)
const page = ref(1)
const showVideoId = ref(null)

const filters = reactive({
  cameraId: normalizeRouteCameraId(route.params.id),
  gateId: "",
  cameraType: "",
  search: "",
  from: "",
  to: ""
})

const gateOptions = computed(() => {
  const map = new Map()
  for (const cam of cameras.value) {
    if (!cam.gateId) continue
    if (map.has(cam.gateId)) continue
    map.set(cam.gateId, {
      gateId: cam.gateId,
      label: cam.gateLocation ? `${cam.gateName} · ${cam.gateLocation}` : cam.gateName
    })
  }
  return Array.from(map.values()).sort((a, b) => a.label.localeCompare(b.label))
})

const cameraTypes = computed(() => {
  return Array.from(
    new Set(cameras.value.map((cam) => String(cam.cameraType || "").trim()).filter(Boolean))
  ).sort((a, b) => a.localeCompare(b))
})

const totalPages = computed(() => Math.max(1, Math.ceil(total.value / pageSize)))

const activeFilterLabel = computed(() => {
  const parts = []
  if (filters.cameraId) {
    const cam = cameras.value.find((item) => String(item.cameraId) === filters.cameraId)
    if (cam) parts.push(cam.cameraName)
  }
  if (filters.gateId) {
    const gate = gateOptions.value.find((item) => String(item.gateId) === filters.gateId)
    if (gate) parts.push(gate.label)
  }
  if (filters.cameraType) parts.push(filters.cameraType)
  return parts.join(" / ") || "Tất cả"
})

async function loadCameras() {
  const data = await getCameras()
  cameras.value = Array.isArray(data) ? data : []
}

function buildParams(targetPage = 1) {
  const params = { page: targetPage, pageSize }
  if (filters.cameraId) params.cameraId = Number(filters.cameraId)
  if (filters.gateId) params.gateId = Number(filters.gateId)
  if (filters.cameraType) params.cameraType = filters.cameraType
  if (filters.search) params.search = filters.search
  if (filters.from) params.from = new Date(`${filters.from}T00:00:00`).toISOString()
  if (filters.to) params.to = new Date(`${filters.to}T23:59:59`).toISOString()
  return params
}

async function loadSegments(targetPage = 1) {
  loading.value = true
  page.value = targetPage
  showVideoId.value = null

  try {
    const res = await getArchiveSegments(buildParams(targetPage))
    segments.value = Array.isArray(res.items) ? res.items : []
    total.value = Number(res.total || 0)
  } catch (error) {
    console.error("Không tải được kho lưu trữ:", error)
    segments.value = []
    total.value = 0
  } finally {
    loading.value = false
  }
}

function resetFilters() {
  filters.cameraId = normalizeRouteCameraId(route.params.id)
  filters.gateId = ""
  filters.cameraType = ""
  filters.search = ""
  filters.from = ""
  filters.to = ""
  loadSegments(1)
}

function toggleVideo(segmentId) {
  showVideoId.value = showVideoId.value === segmentId ? null : segmentId
}

function formatDate(value) {
  if (!value) return "Không rõ"
  return new Date(value).toLocaleString("vi-VN")
}

function formatDuration(seconds) {
  const safe = Number(seconds || 0)
  const hours = Math.floor(safe / 3600)
  const minutes = Math.floor((safe % 3600) / 60)
  const secs = Math.floor(safe % 60)
  if (hours > 0) return `${hours}h ${minutes}m ${secs}s`
  return `${minutes}m ${secs}s`
}

function formatBytes(bytes) {
  const safe = Number(bytes || 0)
  if (!safe) return "0 MB"
  const mb = safe / (1024 * 1024)
  if (mb >= 1024) return `${(mb / 1024).toFixed(2)} GB`
  return `${mb.toFixed(2)} MB`
}

watch(
  () => route.params.id,
  (nextId) => {
    filters.cameraId = normalizeRouteCameraId(nextId)
    loadSegments(1)
  }
)

onMounted(async () => {
  await loadCameras()
  await loadSegments(1)
})
</script>

<style scoped>
.archive-page {
  min-height: 100vh;
  padding: 24px;
  background:
    radial-gradient(circle at top left, rgba(14, 165, 233, 0.12), transparent 28%),
    linear-gradient(180deg, #f8fbff 0%, #eef4fb 100%);
}

.archive-topbar,
.filter-actions,
.segment-head,
.segment-meta,
.segment-actions,
.pagination,
.summary-strip {
  display: flex;
  gap: 12px;
}

.archive-topbar,
.segment-head,
.segment-meta,
.segment-actions,
.pagination,
.summary-strip {
  justify-content: space-between;
}

.archive-topbar,
.filter-panel,
.summary-card,
.segment-card {
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid rgba(148, 163, 184, 0.18);
  box-shadow: 0 18px 40px rgba(15, 23, 42, 0.08);
  border-radius: 20px;
}

.archive-topbar,
.filter-panel,
.segment-card {
  padding: 18px 20px;
}

.archive-topbar {
  align-items: center;
  margin-bottom: 18px;
}

.archive-topbar h1 {
  margin: 0;
  font-size: 32px;
}

.archive-topbar p,
.segment-head p {
  margin: 6px 0 0;
  color: #475569;
}

.filter-panel {
  margin-bottom: 18px;
}

.filter-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
  gap: 14px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 8px;
  color: #0f172a;
  font-weight: 600;
}

.field input,
.field select {
  border: 1px solid #cbd5e1;
  border-radius: 12px;
  padding: 10px 12px;
  font: inherit;
  color: #0f172a;
  background: white;
}

.filter-actions,
.segment-actions,
.pagination,
.summary-strip {
  align-items: center;
}

.filter-actions {
  margin-top: 14px;
  justify-content: flex-end;
}

.summary-strip {
  margin-bottom: 18px;
  flex-wrap: wrap;
}

.summary-card {
  flex: 1 1 180px;
  padding: 16px 18px;
}

.summary-kicker {
  display: block;
  margin-bottom: 8px;
  color: #64748b;
  text-transform: uppercase;
  font-size: 12px;
  letter-spacing: 0.08em;
}

.summary-card strong {
  font-size: 24px;
  color: #0f172a;
}

.segment-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.segment-head h2 {
  margin: 0;
  font-size: 22px;
}

.segment-badges {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.badge {
  border-radius: 999px;
  padding: 8px 12px;
  background: #e2e8f0;
  color: #0f172a;
  font-size: 13px;
  font-weight: 700;
}

.badge.time {
  background: #dbeafe;
  color: #1d4ed8;
}

.segment-meta {
  flex-wrap: wrap;
  color: #475569;
  margin: 14px 0;
}

.segment-video {
  width: 100%;
  max-height: 480px;
  border-radius: 16px;
  background: black;
}

.primary-btn,
.ghost-btn {
  border: none;
  border-radius: 12px;
  padding: 10px 16px;
  cursor: pointer;
  font-weight: 700;
  text-decoration: none;
}

.primary-btn {
  background: linear-gradient(135deg, #0f766e 0%, #0891b2 100%);
  color: white;
}

.ghost-btn {
  background: white;
  color: #0f172a;
  border: 1px solid #cbd5e1;
}

.small {
  padding: 8px 12px;
  font-size: 13px;
}

.empty-state {
  padding: 48px 24px;
  text-align: center;
  color: #64748b;
  background: rgba(255, 255, 255, 0.9);
  border-radius: 20px;
  border: 1px dashed #cbd5e1;
}

@media (max-width: 768px) {
  .archive-page {
    padding: 16px;
  }

  .archive-topbar,
  .segment-head,
  .segment-meta,
  .segment-actions,
  .pagination {
    flex-direction: column;
    align-items: flex-start;
  }

  .filter-actions {
    justify-content: flex-start;
    flex-wrap: wrap;
  }
}
</style>
