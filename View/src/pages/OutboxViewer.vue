<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Hàng đợi gửi</span>
                <h1 class="page-title">Sự kiện hàng đợi gửi</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadEvents">Làm mới</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Sự kiện</span><h2 class="panel-title">Sự kiện chờ gửi</h2></div>
                    <div class="panel-actions">
                        <select v-model="statusFilter" @change="loadEvents" class="form-select">
                            <option value="">Tất cả</option>
                            <option value="Pending">Chờ gửi</option>
                            <option value="Dispatched">Đã gửi</option>
                            <option value="Failed">Thất bại</option>
                            <option value="DeadLetter">Thư chết</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="events.length === 0" class="empty-card">Không có sự kiện hàng đợi.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Loại</th><th>Tổng hợp</th><th>Loại sự kiện</th><th>Trạng thái</th><th>Tương quan</th><th>Ngày tạo</th><th>Thao tác</th></tr></thead>
                        <tbody>
                            <tr v-for="e in events" :key="e.outboxEventId">
                                <td>{{ e.outboxEventId }}</td>
                                <td>{{ e.eventType }}</td>
                                <td>{{ e.aggregateType }}</td>
                                <td>{{ e.eventType }}</td>
                                <td><span class="badge" :class="statusClass(e.status)">{{ statusLabel(e.status) }}</span></td>
                                <td class="table-sub">{{ e.correlationId }}</td>
                                <td class="table-sub">{{ new Date(e.createdAtUtc).toLocaleString() }}</td>
                                <td>
                                    <button v-if="e.status === 'Pending'" class="btn btn-success btn-sm" @click="dispatchEvent(e.outboxEventId)">Gửi đi</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Chuyển giao</span><h2 class="panel-title">Lần chuyển giao Webhook</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="deliveries.length === 0" class="empty-card">Không có lần chuyển giao webhook.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>ID sự kiện</th><th>Đích</th><th>Chữ ký</th><th>Số lần thử</th><th>Lần thử cuối</th><th>Trạng thái</th></tr></thead>
                        <tbody>
                            <tr v-for="d in deliveries" :key="d.webhookDeliveryId">
                                <td>{{ d.webhookDeliveryId }}</td>
                                <td>{{ d.outboxEventId || '—' }}</td>
                                <td class="table-sub">{{ d.targetUrl }}</td>
                                <td class="table-sub">{{ d.signature?.substring(0, 16) }}...</td>
                                <td>{{ d.attemptCount }}</td>
                                <td class="table-sub">{{ d.lastAttemptAtUtc ? new Date(d.lastAttemptAtUtc).toLocaleString() : '—' }}</td>
                                <td><span class="badge" :class="d.status === 'Delivered' ? 'badge-success' : 'badge-warn'">{{ deliveryLabel(d.status) }}</span></td>
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

const statusLabels = {
    Pending: 'Chờ gửi',
    Dispatched: 'Đã gửi',
    Failed: 'Thất bại',
    DeadLetter: 'Thư chết'
}
const deliveryLabels = {
    Delivered: 'Đã gửi',
    Failed: 'Thất bại'
}
function statusLabel(s) { return statusLabels[s] || s }
function deliveryLabel(s) { return deliveryLabels[s] || s }

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
    if (!confirm(`Gửi đi sự kiện hàng đợi #${eventId}?`)) return
    try {
        await enterpriseApi.dispatchEvent(eventId)
        await loadEvents()
    } catch { alert('Gửi đi thất bại') }
}

function statusClass(s) {
    if (s === 'Dispatched') return 'badge-success'
    if (s === 'Failed') return 'badge-danger'
    if (s === 'DeadLetter') return 'badge-danger'
    return 'badge-warn'
}

onMounted(loadEvents)
</script>
