<template>
    <div class="page-container ops-page animate-in" :class="{ 'timeline-embedded': embedded }">
        <div v-if="!embedded" class="page-header-bar">
            <div>
                <span class="panel-kicker">Nhận thức tình huống</span>
                <h1 class="page-title">Dòng thời gian sự kiện</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" @click="showSiteMaps = true">Bản đồ khu vực</button>
                <button class="btn btn-secondary" @click="showCreateEvent = true">+ Sự kiện</button>
                <button class="btn btn-primary" @click="loadEvents">Làm mới</button>
            </div>
        </div>
        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Bộ lọc</span><h2 class="panel-title">Tìm kiếm sự kiện</h2></div>
                </div>
                <div class="filter-row">
                    <input v-model="filters.eventType" class="form-input" placeholder="Loại sự kiện" @input="debounceLoad" />
                    <input v-model="filters.plate" class="form-input" placeholder="Biển số" @input="debounceLoad" />
                    <select v-model="filters.severity" class="form-select" @change="loadEvents">
                        <option value="">Tất cả mức độ</option>
                        <option value="Info">Thông tin</option>
                        <option value="Medium">Trung bình</option>
                        <option value="High">Cao</option>
                        <option value="Critical">Nghiêm trọng</option>
                    </select>
                    <select v-model="filters.subjectType" class="form-select" @change="loadEvents">
                        <option value="">Tất cả đối tượng</option>
                        <option value="Employee">Nhân viên</option>
                        <option value="Visitor">Khách</option>
                        <option value="Contractor">Nhà thầu</option>
                        <option value="Vehicle">Phương tiện</option>
                    </select>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Sự kiện</span><h2 class="panel-title">Sự kiện an ninh</h2></div>
                    <div class="page-buttons">
                        <button class="page-btn" :disabled="page <= 1" @click="page--; loadEvents()">‹</button>
                        <button class="page-btn" disabled>{{ page }} / {{ totalPages }}</button>
                        <button class="page-btn" :disabled="page >= totalPages" @click="page++; loadEvents()">›</button>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="events.length === 0" class="empty-card">Không có sự kiện nào.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Thời gian</th><th>Loại</th><th>Mức độ</th><th>Đối tượng</th><th>Biển số</th><th>Độ tin cậy</th><th>Tóm tắt</th><th></th></tr></thead>
                        <tbody>
                            <tr v-for="e in events" :key="e.securityEventId" @click="selectEvent(e)" class="clickable-row">
                                <td class="table-sub">{{ new Date(e.occurredAtUtc).toLocaleString() }}</td>
                                <td><span class="badge badge-info">{{ e.eventType }}</span></td>
                                <td><span class="badge" :class="sevClass(e.severity)">{{ e.severity }}</span></td>
                                <td>{{ e.subjectType || '—' }}:{{ e.subjectId || '' }}</td>
                                <td><span v-if="e.plateText" class="plate-pill">{{ e.plateText }}</span><span v-else class="table-sub">—</span></td>
                                <td>{{ e.confidence != null ? (e.confidence * 100).toFixed(0) + '%' : '—' }}</td>
                                <td class="table-sub">{{ e.summary || '—' }}</td>
                                <td>
                                    <button class="btn btn-danger btn-sm" @click.stop="deleteEvent(e)" title="Xóa sự kiện">&times;</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>

        <!-- Event Detail Modal -->
        <div v-if="selectedEvent" class="modal-overlay" @click.self="selectedEvent = null">
            <div class="modal-box wide-modal">
                <h3>Chi tiết sự kiện</h3>
                <div class="detail-grid">
                    <div><strong>Loại:</strong> {{ selectedEvent.eventType }}</div>
                    <div><strong>Mức độ:</strong> {{ selectedEvent.severity }}</div>
                    <div><strong>Nguồn:</strong> {{ selectedEvent.sourceType }}:{{ selectedEvent.sourceId }}</div>
                    <div><strong>Đối tượng:</strong> {{ selectedEvent.subjectType }} {{ selectedEvent.subjectId }}</div>
                    <div><strong>Biển số:</strong> {{ selectedEvent.plateText || '—' }}</div>
                    <div><strong>Độ tin cậy:</strong> {{ selectedEvent.confidence }}</div>
                    <div><strong>ID liên kết:</strong> {{ selectedEvent.correlationId }}</div>
                    <div><strong>Tóm tắt:</strong> {{ selectedEvent.summary }}</div>
                </div>
                <div class="modal-actions">
                    <button class="btn btn-danger" @click="deleteEvent(selectedEvent)">Xóa</button>
                    <button class="btn btn-secondary" @click="selectedEvent = null">Đóng</button>
                </div>
            </div>
        </div>

        <!-- Site Maps Modal -->
        <div v-if="showSiteMaps" class="modal-overlay" @click.self="showSiteMaps = false">
            <div class="modal-box wide-modal">
                <h3>Bản đồ khu vực</h3>
                <div class="filter-row" style="margin-bottom:12px;">
                    <button class="btn btn-secondary btn-sm" @click="showNewMapForm = !showNewMapForm">+ Bản đồ mới</button>
                    <button class="btn btn-primary btn-sm" @click="loadSiteMaps">Làm mới</button>
                </div>

                <!-- New Map Form -->
                <div v-if="showNewMapForm" class="form-grid" style="margin-bottom:12px;">
                    <div class="form-group">
                        <label>Tên bản đồ</label>
                        <input v-model="newMapForm.name" class="form-input" placeholder="vd. Tầng 1" />
                    </div>
                    <div class="form-group">
                        <label>Mã khu vực</label>
                        <input v-model.number="newMapForm.siteId" type="number" class="form-input" placeholder="Khu vực" />
                    </div>
                    <div class="form-group">
                        <label>Kích thước</label>
                        <div class="chip-row">
                            <input v-model.number="newMapForm.width" type="number" class="form-input" placeholder="R" style="width:80px;" />
                            <span>x</span>
                            <input v-model.number="newMapForm.height" type="number" class="form-input" placeholder="C" style="width:80px;" />
                        </div>
                    </div>
                    <div class="chip-row">
                        <button class="btn btn-primary btn-sm" :disabled="mapBusy || !newMapForm.name" @click="createSiteMap">{{ mapBusy ? 'Đang tạo...' : 'Tạo' }}</button>
                        <button class="btn btn-secondary btn-sm" @click="showNewMapForm = false">Hủy</button>
                    </div>
                </div>

                <div v-if="mapLoading" class="empty-card">Đang tải bản đồ...</div>
                <div v-else-if="siteMaps.length === 0" class="empty-card">Chưa có bản đồ khu vực.</div>
                <div v-else class="map-list">
                    <div v-for="map in siteMaps" :key="map.siteMapId || map.id" class="map-card" @click="loadMapPlacements(map)">
                        <div class="map-card-header">
                            <strong>{{ map.name || map.mapName }}</strong>
                            <span class="text-muted">Khu vực {{ map.siteId }}</span>
                        </div>
                        <div class="map-card-meta">
                            <span class="text-muted">{{ map.width || 0 }} x {{ map.height || 0 }}</span>
                            <button class="btn btn-sm btn-ghost" @click.stop="showPlacementsForMap(map)">Vị trí</button>
                        </div>
                    </div>
                </div>

                <!-- Map Placements Section -->
                <div v-if="selectedMap" style="margin-top:16px;">
                    <div class="detail-section-title">Vị trí — {{ selectedMap.name || selectedMap.mapName }}</div>
                    <div class="form-row" style="margin-bottom:8px;">
                        <div class="form-group" style="display:flex; gap:8px; align-items:end;">
                            <input v-model.number="placementForm.deviceId" type="number" class="form-input" placeholder="Mã thiết bị" style="width:100px;" />
                            <input v-model.number="placementForm.x" type="number" class="form-input" placeholder="X" style="width:70px;" />
                            <input v-model.number="placementForm.y" type="number" class="form-input" placeholder="Y" style="width:70px;" />
                            <button class="btn btn-primary btn-sm" :disabled="placementBusy || !placementForm.deviceId" @click="addPlacement">
                                {{ placementBusy ? 'Đang thêm...' : 'Thêm' }}
                            </button>
                        </div>
                    </div>
                    <div v-if="placementLoading" class="empty-card">Đang tải vị trí...</div>
                    <div v-else-if="placements.length === 0" class="text-muted">Chưa có vị trí cho bản đồ này.</div>
                    <div v-else class="table-container">
                        <table class="data-table">
                            <thead><tr><th>Mã thiết bị</th><th>Vị trí</th></tr></thead>
                            <tbody>
                                <tr v-for="p in placements" :key="p.mapPlacementId || p.id">
                                    <td>{{ p.securityDeviceId || p.deviceId }}</td>
                                    <td>({{ p.xCoordinate || p.x }}, {{ p.yCoordinate || p.y }})</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>

        <!-- Create Event Modal -->
        <div v-if="showCreateEvent" class="modal-overlay" @click.self="showCreateEvent = false">
            <div class="modal-box">
                <h3>Tạo sự kiện an ninh</h3>
                <div class="form-grid single">
                    <div class="form-group">
                        <label>Loại sự kiện</label>
                        <input v-model="createEventForm.eventType" class="form-input" placeholder="vd. AccessGranted, TamperDetected" />
                    </div>
                    <div class="form-group">
                        <label>Mức độ</label>
                        <select v-model="createEventForm.severity" class="form-select">
                            <option value="Info">Thông tin</option>
                            <option value="Medium">Trung bình</option>
                            <option value="High">Cao</option>
                            <option value="Critical">Nghiêm trọng</option>
                        </select>
                    </div>
                    <div class="form-row two">
                        <div class="form-group">
                            <label>Loại đối tượng</label>
                            <input v-model="createEventForm.subjectType" class="form-input" placeholder="Employee, Vehicle" />
                        </div>
                        <div class="form-group">
                            <label>Mã đối tượng</label>
                            <input v-model="createEventForm.subjectId" class="form-input" placeholder="Không bắt buộc" />
                        </div>
                    </div>
                    <div class="form-group">
                        <label>Tóm tắt</label>
                        <textarea v-model="createEventForm.summary" class="form-input" rows="2" placeholder="Mô tả sự kiện"></textarea>
                    </div>
                </div>
                <div v-if="createEventResult" class="alert alert-success">{{ createEventResult }}</div>
                <div v-else-if="createEventError" class="alert alert-danger">{{ createEventError }}</div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showCreateEvent = false">Hủy</button>
                    <button class="btn btn-primary" :disabled="createEventBusy || !createEventForm.eventType" @click="submitCreateEvent">
                        {{ createEventBusy ? 'Đang tạo...' : 'Tạo' }}
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

defineProps({
    embedded: {
        type: Boolean,
        default: false,
    },
})

const events = ref([])
const loading = ref(true)
const page = ref(1)
const totalPages = ref(1)
const selectedEvent = ref(null)
const filters = reactive({ eventType: '', plate: '', severity: '', subjectType: '' })

// Site Maps
const showSiteMaps = ref(false)
const showNewMapForm = ref(false)
const mapLoading = ref(false)
const mapBusy = ref(false)
const siteMaps = ref([])
const selectedMap = ref(null)
const placements = ref([])
const placementLoading = ref(false)
const placementBusy = ref(false)
const newMapForm = ref({ name: '', siteId: null, width: 100, height: 100 })
const placementForm = ref({ deviceId: null, x: 50, y: 50 })

// Create Event
const showCreateEvent = ref(false)
const createEventBusy = ref(false)
const createEventResult = ref('')
const createEventError = ref('')
const createEventForm = ref({
    eventType: 'Info', severity: 'Info',
    subjectType: '', subjectId: '',
    summary: '',
})

let debounceTimer = null
function debounceLoad() {
    clearTimeout(debounceTimer)
    debounceTimer = setTimeout(() => { page.value = 1; loadEvents() }, 300)
}

async function loadEvents() {
    loading.value = true
    try {
        const params = { page: page.value, pageSize: 50 }
        if (filters.eventType) params.eventType = filters.eventType
        if (filters.plate) params.plate = filters.plate
        if (filters.severity) params.severity = filters.severity
        if (filters.subjectType) params.subjectType = filters.subjectType
        const res = await enterpriseApi.getEvents(params)
        events.value = res.data.items || []
        totalPages.value = Math.ceil((res.data.total || 0) / 50) || 1
    } catch { events.value = [] }
    finally { loading.value = false }
}

function selectEvent(e) { selectedEvent.value = e }

async function deleteEvent(e) {
    if (!confirm(`Xóa sự kiện #${e.securityEventId}?`)) return
    try {
        await enterpriseApi.deleteEvent(e.securityEventId)
        if (selectedEvent.value?.securityEventId === e.securityEventId) selectedEvent.value = null
        await loadEvents()
    } catch { alert('Xóa thất bại') }
}

function sevClass(s) {
    if (s === 'Critical' || s === 'High') return 'badge-danger'
    if (s === 'Medium') return 'badge-warn'
    return 'badge-info'
}

// --- Site Maps ---
async function loadSiteMaps() {
    mapLoading.value = true
    try {
        const res = await enterpriseApi.getSiteMaps()
        siteMaps.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
    } catch { siteMaps.value = [] }
    finally { mapLoading.value = false }
}

async function showPlacementsForMap(map) {
    selectedMap.value = map
    placementLoading.value = true
    placements.value = []
    try {
        const res = await enterpriseApi.getMapPlacements(map.siteMapId || map.id)
        placements.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
    } catch { placements.value = [] }
    finally { placementLoading.value = false }
}

async function createSiteMap() {
    if (!newMapForm.value.name) return
    mapBusy.value = true
    try {
        await enterpriseApi.createSiteMap({
            name: newMapForm.value.name,
            siteId: newMapForm.value.siteId || null,
            width: newMapForm.value.width,
            height: newMapForm.value.height,
        })
        newMapForm.value = { name: '', siteId: null, width: 100, height: 100 }
        showNewMapForm.value = false
        await loadSiteMaps()
    } catch { alert('Không thể tạo bản đồ') }
    finally { mapBusy.value = false }
}

async function addPlacement() {
    if (!selectedMap.value || !placementForm.value.deviceId) return
    placementBusy.value = true
    try {
        await enterpriseApi.addMapPlacement(selectedMap.value.siteMapId || selectedMap.value.id, {
            securityDeviceId: placementForm.value.deviceId,
            xCoordinate: placementForm.value.x,
            yCoordinate: placementForm.value.y,
        })
        placementForm.value = { deviceId: null, x: 50, y: 50 }
        await showPlacementsForMap(selectedMap.value)
    } catch { alert('Không thể thêm vị trí') }
    finally { placementBusy.value = false }
}

// --- Create Event ---
async function submitCreateEvent() {
    if (!createEventForm.value.eventType) return
    createEventBusy.value = true
    createEventResult.value = ''
    createEventError.value = ''
    try {
        await enterpriseApi.createEvent({
            eventType: createEventForm.value.eventType,
            severity: createEventForm.value.severity,
            subjectType: createEventForm.value.subjectType || null,
            subjectId: createEventForm.value.subjectId || null,
            summary: createEventForm.value.summary || null,
        })
        createEventResult.value = 'Đã tạo sự kiện!'
        createEventForm.value = { eventType: 'Info', severity: 'Info', subjectType: '', subjectId: '', summary: '' }
        showCreateEvent.value = false
        await loadEvents()
    } catch (e) {
        createEventError.value = e.response?.data?.message || e.message
    } finally {
        createEventBusy.value = false
    }
}

onMounted(loadEvents)
</script>

<style scoped>
.timeline-embedded {
    padding: 0;
}
</style>
