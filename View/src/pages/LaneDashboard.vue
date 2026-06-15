<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Traffic & Access</span>
                <h1 class="page-title">Lane Dashboard</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" :disabled="loading" @click="loadAll">Refresh</button>
            </div>
        </div>

        <section class="metric-grid four">
            <article class="metric-tile"><span class="metric-label">Lanes</span><strong class="metric-value">{{ lanes.length }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Healthy</span><strong class="metric-value">{{ healthyCount }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Degraded</span><strong class="metric-value">{{ degradedCount }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Barriers</span><strong class="metric-value">{{ barrierCount }}</strong></article>
        </section>

        <section class="ops-panel" style="margin-bottom: 1rem;">
            <div class="panel-head">
                <h2 class="panel-title">Quick Navigation</h2>
            </div>
            <div class="chip-row">
                <router-link to="/gate-transit-monitor" class="btn btn-sm btn-primary">
                    Gate Transit Monitor
                </router-link>
                <router-link to="/exceptions" class="btn btn-sm btn-secondary">
                    Exception Cases
                </router-link>
                <router-link to="/barrier-panel" class="btn btn-sm btn-ghost">
                    Barrier Control
                </router-link>
            </div>
        </section>

        <section v-if="loading" class="ops-panel">
            <div class="empty-card">Loading lane health data...</div>
        </section>

        <section v-else class="metric-grid" style="grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));">
            <article
                v-for="l in lanes"
                :key="l.laneId"
                class="ops-panel lane-card"
                :class="{ 'degraded-panel': l.isDegraded }"
                @click="navigateToLane(l)"
                role="button"
                :tabindex="0"
                @keydown.enter="navigateToLane(l)"
            >
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">{{ l.direction }} lane</span>
                        <h2 class="panel-title" style="font-size: 1.1rem;">{{ l.name }}</h2>
                    </div>
                    <span class="soft-chip" :class="l.isDegraded ? 'danger' : 'success'">
                        {{ l.status }}
                    </span>
                </div>
                <div class="detail-grid" style="margin-top: 0.5rem;">
                    <div class="detail-row">
                        <span class="detail-label">Barriers</span>
                        <span>{{ l.barrierCount }}</span>
                    </div>
                    <div v-if="l.lastEventAt" class="detail-row">
                        <span class="detail-label">Last Event</span>
                        <span>{{ formatDate(l.lastEventAt) }}</span>
                    </div>
                    <div v-if="l.lastEventAgeMinutes != null" class="detail-row">
                        <span class="detail-label">Age</span>
                        <span>{{ Math.round(l.lastEventAgeMinutes) }} min</span>
                    </div>
                    <div v-if="l.lastPlateText" class="detail-row">
                        <span class="detail-label">Last Plate</span>
                        <span class="plate-badge">{{ l.lastPlateText }}</span>
                    </div>
                </div>
                <div v-if="l.barriers && l.barriers.length > 0" style="margin-top: 0.5rem;">
                    <div v-for="b in l.barriers" :key="b.barrierId" class="chip-row">
                        <span class="soft-chip" :class="stateClass(b.state)">{{ b.name }}: {{ b.state }}</span>
                    </div>
                </div>
                <div class="lane-card-footer">
                    <small class="text-muted">Click to open Gate Transit Monitor</small>
                </div>
            </article>
        </section>

        <section class="ops-panel" style="margin-top: 1rem;">
            <div class="panel-head">
                <h2 class="panel-title">Recent Lane Events</h2>
                <router-link to="/gate-transit-monitor" class="btn btn-sm btn-ghost">View Monitor</router-link>
            </div>
            <div v-if="loadingEvents" class="empty-card">Loading events...</div>
            <div v-else-if="events.length === 0" class="empty-card">No lane events recorded.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>Time</th><th>Lane</th><th>Type</th><th>Direction</th><th>Plate</th><th>Note</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="e in events" :key="e.laneEventId" class="clickable-row" @click="navigateToException(e)" role="button" :tabindex="0" @keydown.enter="navigateToException(e)">
                            <td>{{ formatDate(e.occurredAtUtc) }}</td>
                            <td>{{ e.lane?.name || '&mdash;' }}</td>
                            <td><span class="soft-chip" :class="eventTypeClass(e.eventType)">{{ e.eventType }}</span></td>
                            <td>{{ e.direction }}</td>
                            <td><span v-if="e.plateText" class="plate-badge">{{ e.plateText }}</span><span v-else>&mdash;</span></td>
                            <td>{{ e.note || '&mdash;' }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const router = useRouter()
const loading = ref(false)
const loadingEvents = ref(false)
const lanes = ref([])
const events = ref([])

const healthyCount = computed(() => lanes.value.filter(l => !l.isDegraded).length)
const degradedCount = computed(() => lanes.value.filter(l => l.isDegraded).length)
const barrierCount = computed(() => lanes.value.reduce((s, l) => s + (l.barrierCount || 0), 0))

function stateClass(s) {
    if (!s) return ''
    return s === 'Open' ? 'success' : s === 'Closed' ? 'muted' : s === 'Fault' || s === 'LockedClosed' ? 'danger' : 'warn'
}

function eventTypeClass(type) {
    if (!type) return ''
    const t = String(type || '').toUpperCase()
    if (t.includes('GRANTED') || t.includes('ALLOW') || t.includes('OPEN')) return 'success'
    if (t.includes('DENIED') || t.includes('DENY') || t.includes('CLOSE') || t.includes('LOCK')) return 'danger'
    if (t.includes('MANUAL') || t.includes('OVERRIDE') || t.includes('ESCALATION')) return 'warn'
    if (t.includes('DURESS')) return 'danger'
    return 'muted'
}

function formatDate(utc) {
    if (!utc) return '&mdash;'
    return new Date(utc).toLocaleString('vi-VN')
}

function navigateToLane(lane) {
    router.push({ name: 'GateTransitMonitor' })
}

function navigateToException(event) {
    router.push({ name: 'Exceptions', query: { eventId: event.laneEventId } })
}

async function loadAll() {
    loading.value = true
    loadingEvents.value = true
    try {
        const [healthRes, eventsRes] = await Promise.all([
            enterpriseApi.getLaneHealth(),
            enterpriseApi.getLaneEvents({ pageSize: 25 }),
        ])
        lanes.value = healthRes.data || []
        events.value = eventsRes.data?.items || []
    } catch (e) {
        console.error('Failed to load lane data', e)
    } finally {
        loading.value = false
        loadingEvents.value = false
    }
}

onMounted(loadAll)
</script>

<style scoped>
.degraded-panel { border-left: 3px solid var(--danger, #ef4444); }
.plate-badge {
    font-family: monospace;
    background: var(--surface-raised, #1e293b);
    padding: 0.15rem 0.5rem;
    border-radius: 4px;
    letter-spacing: 0.05em;
}
.lane-card {
    cursor: pointer;
    transition: box-shadow 0.15s ease, transform 0.15s ease;
}
.lane-card:hover {
    box-shadow: 0 4px 16px rgba(15, 23, 42, 0.1);
    transform: translateY(-2px);
}
.lane-card-footer {
    margin-top: 0.75rem;
    padding-top: 0.5rem;
    border-top: 1px solid var(--border, #e2e8f0);
}
.lane-card-footer small {
    color: var(--text-muted, #94a3b8);
    font-size: 0.75rem;
}
.clickable-row {
    cursor: pointer;
    transition: background 0.1s ease;
}
.clickable-row:hover {
    background: var(--surface-hover, #f1f5f9);
}
</style>
