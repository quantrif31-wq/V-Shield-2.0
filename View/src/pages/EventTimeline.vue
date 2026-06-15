<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Situational awareness</span>
                <h1 class="page-title">Event Timeline</h1>
            </div>
            <div class="header-actions">
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
                        <thead><tr><th>Time</th><th>Type</th><th>Severity</th><th>Subject</th><th>Plate</th><th>Confidence</th><th>Summary</th></tr></thead>
                        <tbody>
                            <tr v-for="e in events" :key="e.securityEventId" @click="selectEvent(e)" class="clickable-row">
                                <td class="table-sub">{{ new Date(e.occurredAtUtc).toLocaleString() }}</td>
                                <td><span class="badge badge-info">{{ e.eventType }}</span></td>
                                <td><span class="badge" :class="sevClass(e.severity)">{{ e.severity }}</span></td>
                                <td>{{ e.subjectType || '—' }}:{{ e.subjectId || '' }}</td>
                                <td><span v-if="e.plateText" class="plate-pill">{{ e.plateText }}</span><span v-else class="table-sub">—</span></td>
                                <td>{{ e.confidence != null ? (e.confidence * 100).toFixed(0) + '%' : '—' }}</td>
                                <td class="table-sub">{{ e.summary || '—' }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>
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
                    <button class="btn btn-secondary" @click="selectedEvent = null">Close</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const events = ref([])
const loading = ref(true)
const page = ref(1)
const totalPages = ref(1)
const selectedEvent = ref(null)
const filters = reactive({ eventType: '', plate: '', severity: '', subjectType: '' })

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

function sevClass(s) {
    if (s === 'Critical' || s === 'High') return 'badge-danger'
    if (s === 'Medium') return 'badge-warn'
    return 'badge-info'
}

onMounted(loadEvents)
</script>
