<template>
    <div class="page-container ops-page animate-in" :class="{ 'timeline-embedded': embedded }">
        <div v-if="!embedded" class="page-header-bar">
            <div>
                <span class="panel-kicker">Situational awareness</span>
                <h1 class="page-title">Event Timeline</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" @click="showSiteMaps = true">Site Maps</button>
                <button class="btn btn-secondary" @click="showCreateEvent = true">+ Event</button>
                <button class="btn btn-primary" @click="loadEvents">Refresh</button>
            </div>
        </div>
        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Filters</span><h2 class="panel-title">Search Events</h2></div>
                </div>
                <div class="filter-row">
                    <input v-model="filters.eventType" class="form-input" placeholder="Event type" @input="debounceLoad" />
                    <input v-model="filters.plate" class="form-input" placeholder="Plate" @input="debounceLoad" />
                    <select v-model="filters.severity" class="form-select" @change="loadEvents">
                        <option value="">All Severity</option>
                        <option value="Info">Info</option>
                        <option value="Medium">Medium</option>
                        <option value="High">High</option>
                        <option value="Critical">Critical</option>
                    </select>
                    <select v-model="filters.subjectType" class="form-select" @change="loadEvents">
                        <option value="">All Subjects</option>
                        <option value="Employee">Employee</option>
                        <option value="Visitor">Visitor</option>
                        <option value="Contractor">Contractor</option>
                        <option value="Vehicle">Vehicle</option>
                    </select>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Events</span><h2 class="panel-title">Security Events</h2></div>
                    <div class="page-buttons">
                        <button class="page-btn" :disabled="page <= 1" @click="page--; loadEvents()">‹</button>
                        <button class="page-btn" disabled>{{ page }} / {{ totalPages }}</button>
                        <button class="page-btn" :disabled="page >= totalPages" @click="page++; loadEvents()">›</button>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="events.length === 0" class="empty-card">No events found.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Time</th><th>Type</th><th>Severity</th><th>Subject</th><th>Plate</th><th>Confidence</th><th>Summary</th><th></th></tr></thead>
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
                                    <button class="btn btn-danger btn-sm" @click.stop="deleteEvent(e)" title="Delete event">&times;</button>
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
                <h3>Event Detail</h3>
                <div class="detail-grid">
                    <div><strong>Type:</strong> {{ selectedEvent.eventType }}</div>
                    <div><strong>Severity:</strong> {{ selectedEvent.severity }}</div>
                    <div><strong>Source:</strong> {{ selectedEvent.sourceType }}:{{ selectedEvent.sourceId }}</div>
                    <div><strong>Subject:</strong> {{ selectedEvent.subjectType }} {{ selectedEvent.subjectId }}</div>
                    <div><strong>Plate:</strong> {{ selectedEvent.plateText || '—' }}</div>
                    <div><strong>Confidence:</strong> {{ selectedEvent.confidence }}</div>
                    <div><strong>Correlation ID:</strong> {{ selectedEvent.correlationId }}</div>
                    <div><strong>Summary:</strong> {{ selectedEvent.summary }}</div>
                </div>
                <div class="modal-actions">
                    <button class="btn btn-danger" @click="deleteEvent(selectedEvent)">Delete</button>
                    <button class="btn btn-secondary" @click="selectedEvent = null">Close</button>
                </div>
            </div>
        </div>

        <!-- Site Maps Modal -->
        <div v-if="showSiteMaps" class="modal-overlay" @click.self="showSiteMaps = false">
            <div class="modal-box wide-modal">
                <h3>Site Maps</h3>
                <div class="filter-row" style="margin-bottom:12px;">
                    <button class="btn btn-secondary btn-sm" @click="showNewMapForm = !showNewMapForm">+ New Map</button>
                    <button class="btn btn-primary btn-sm" @click="loadSiteMaps">Refresh</button>
                </div>

                <!-- New Map Form -->
                <div v-if="showNewMapForm" class="form-grid" style="margin-bottom:12px;">
                    <div class="form-group">
                        <label>Map Name</label>
                        <input v-model="newMapForm.name" class="form-input" placeholder="e.g. Floor 1" />
                    </div>
                    <div class="form-group">
                        <label>Site ID</label>
                        <input v-model.number="newMapForm.siteId" type="number" class="form-input" placeholder="Site" />
                    </div>
                    <div class="form-group">
                        <label>Dimensions</label>
                        <div class="chip-row">
                            <input v-model.number="newMapForm.width" type="number" class="form-input" placeholder="W" style="width:80px;" />
                            <span>x</span>
                            <input v-model.number="newMapForm.height" type="number" class="form-input" placeholder="H" style="width:80px;" />
                        </div>
                    </div>
                    <div class="chip-row">
                        <button class="btn btn-primary btn-sm" :disabled="mapBusy || !newMapForm.name" @click="createSiteMap">{{ mapBusy ? 'Creating...' : 'Create' }}</button>
                        <button class="btn btn-secondary btn-sm" @click="showNewMapForm = false">Cancel</button>
                    </div>
                </div>

                <div v-if="mapLoading" class="empty-card">Loading maps...</div>
                <div v-else-if="siteMaps.length === 0" class="empty-card">No site maps.</div>
                <div v-else class="map-list">
                    <div v-for="map in siteMaps" :key="map.siteMapId || map.id" class="map-card" @click="loadMapPlacements(map)">
                        <div class="map-card-header">
                            <strong>{{ map.name || map.mapName }}</strong>
                            <span class="text-muted">Site {{ map.siteId }}</span>
                        </div>
                        <div class="map-card-meta">
                            <span class="text-muted">{{ map.width || 0 }} x {{ map.height || 0 }}</span>
                            <button class="btn btn-sm btn-ghost" @click.stop="showPlacementsForMap(map)">Placements</button>
                        </div>
                    </div>
                </div>

                <!-- Map Placements Section -->
                <div v-if="selectedMap" style="margin-top:16px;">
                    <div class="detail-section-title">Placements — {{ selectedMap.name || selectedMap.mapName }}</div>
                    <div class="form-row" style="margin-bottom:8px;">
                        <div class="form-group" style="display:flex; gap:8px; align-items:end;">
                            <input v-model.number="placementForm.deviceId" type="number" class="form-input" placeholder="Device ID" style="width:100px;" />
                            <input v-model.number="placementForm.x" type="number" class="form-input" placeholder="X" style="width:70px;" />
                            <input v-model.number="placementForm.y" type="number" class="form-input" placeholder="Y" style="width:70px;" />
                            <button class="btn btn-primary btn-sm" :disabled="placementBusy || !placementForm.deviceId" @click="addPlacement">
                                {{ placementBusy ? 'Adding...' : 'Add' }}
                            </button>
                        </div>
                    </div>
                    <div v-if="placementLoading" class="empty-card">Loading placements...</div>
                    <div v-else-if="placements.length === 0" class="text-muted">No placements for this map.</div>
                    <div v-else class="table-container">
                        <table class="data-table">
                            <thead><tr><th>Device ID</th><th>Position</th></tr></thead>
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
                <h3>Create Security Event</h3>
                <div class="form-grid single">
                    <div class="form-group">
                        <label>Event Type</label>
                        <input v-model="createEventForm.eventType" class="form-input" placeholder="e.g. AccessGranted, TamperDetected" />
                    </div>
                    <div class="form-group">
                        <label>Severity</label>
                        <select v-model="createEventForm.severity" class="form-select">
                            <option value="Info">Info</option>
                            <option value="Medium">Medium</option>
                            <option value="High">High</option>
                            <option value="Critical">Critical</option>
                        </select>
                    </div>
                    <div class="form-row two">
                        <div class="form-group">
                            <label>Subject Type</label>
                            <input v-model="createEventForm.subjectType" class="form-input" placeholder="Employee, Vehicle" />
                        </div>
                        <div class="form-group">
                            <label>Subject ID</label>
                            <input v-model="createEventForm.subjectId" class="form-input" placeholder="Optional" />
                        </div>
                    </div>
                    <div class="form-group">
                        <label>Summary</label>
                        <textarea v-model="createEventForm.summary" class="form-input" rows="2" placeholder="Event description"></textarea>
                    </div>
                </div>
                <div v-if="createEventResult" class="alert alert-success">{{ createEventResult }}</div>
                <div v-else-if="createEventError" class="alert alert-danger">{{ createEventError }}</div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showCreateEvent = false">Cancel</button>
                    <button class="btn btn-primary" :disabled="createEventBusy || !createEventForm.eventType" @click="submitCreateEvent">
                        {{ createEventBusy ? 'Creating...' : 'Create' }}
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
    if (!confirm(`Delete event #${e.securityEventId}?`)) return
    try {
        await enterpriseApi.deleteEvent(e.securityEventId)
        if (selectedEvent.value?.securityEventId === e.securityEventId) selectedEvent.value = null
        await loadEvents()
    } catch { alert('Delete failed') }
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
    } catch { alert('Failed to create map') }
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
    } catch { alert('Failed to add placement') }
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
        createEventResult.value = 'Event created!'
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
