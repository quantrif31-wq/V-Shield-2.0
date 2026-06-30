<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Outbox</span>
                <h1 class="page-title">Outbox Events</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadEvents">Refresh</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Events</span><h2 class="panel-title">Pending Events</h2></div>
                    <div class="panel-actions">
                        <select v-model="statusFilter" @change="loadEvents" class="form-select">
                            <option value="">All</option>
                            <option value="Pending">Pending</option>
                            <option value="Dispatched">Dispatched</option>
                            <option value="Failed">Failed</option>
                            <option value="DeadLetter">Dead Letter</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="events.length === 0" class="empty-card">No outbox events.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Type</th><th>Aggregate</th><th>Event Type</th><th>Status</th><th>Correlation</th><th>Created</th><th>Actions</th></tr></thead>
                        <tbody>
                            <tr v-for="e in events" :key="e.outboxEventId">
                                <td>{{ e.outboxEventId }}</td>
                                <td>{{ e.eventType }}</td>
                                <td>{{ e.aggregateType }}</td>
                                <td>{{ e.eventType }}</td>
                                <td><span class="badge" :class="statusClass(e.status)">{{ e.status }}</span></td>
                                <td class="table-sub">{{ e.correlationId }}</td>
                                <td class="table-sub">{{ new Date(e.createdAtUtc).toLocaleString() }}</td>
                                <td>
                                    <button v-if="e.status === 'Pending'" class="btn btn-success btn-sm" @click="dispatchEvent(e.outboxEventId)">Dispatch</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Deliveries</span><h2 class="panel-title">Webhook Deliveries</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="deliveries.length === 0" class="empty-card">No webhook deliveries.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Event ID</th><th>Target</th><th>Signature</th><th>Attempts</th><th>Last Attempt</th><th>Status</th></tr></thead>
                        <tbody>
                            <tr v-for="d in deliveries" :key="d.webhookDeliveryId">
                                <td>{{ d.webhookDeliveryId }}</td>
                                <td>{{ d.outboxEventId || '—' }}</td>
                                <td class="table-sub">{{ d.targetUrl }}</td>
                                <td class="table-sub">{{ d.signature?.substring(0, 16) }}...</td>
                                <td>{{ d.attemptCount }}</td>
                                <td class="table-sub">{{ d.lastAttemptAtUtc ? new Date(d.lastAttemptAtUtc).toLocaleString() : '—' }}</td>
                                <td><span class="badge" :class="d.status === 'Delivered' ? 'badge-success' : 'badge-warn'">{{ d.status }}</span></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const events = ref([])
const deliveries = ref([])
const loading = ref(true)
const statusFilter = ref('')

async function loadEvents() {
    loading.value = true
    try {
        const [eventsRes, deliveriesRes] = await Promise.all([
            enterpriseApi.getOutboxEvents({ status: statusFilter.value || undefined }),
            enterpriseApi.getWebhookDeliveries({ limit: 50 })
        ])
        events.value = Array.isArray(eventsRes.data) ? eventsRes.data : []
        deliveries.value = Array.isArray(deliveriesRes.data) ? deliveriesRes.data : []
    } catch { events.value = []; deliveries.value = [] }
    finally { loading.value = false }
}

async function dispatchEvent(eventId) {
    if (!confirm(`Dispatch outbox event #${eventId}?`)) return
    try {
        await enterpriseApi.dispatchEvent(eventId)
        await loadEvents()
    } catch { alert('Dispatch failed') }
}

function statusClass(s) {
    if (s === 'Dispatched') return 'badge-success'
    if (s === 'Failed') return 'badge-danger'
    if (s === 'DeadLetter') return 'badge-danger'
    return 'badge-warn'
}

onMounted(loadEvents)
</script>
