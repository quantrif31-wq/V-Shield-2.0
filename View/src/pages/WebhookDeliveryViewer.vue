<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Webhooks</span>
                <h1 class="page-title">Webhook Delivery Viewer</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadSubscriptions">Refresh</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Subscriptions</span><h2 class="panel-title">Webhook Subscriptions</h2></div>
                    <div class="panel-actions">
                        <button class="btn btn-secondary btn-sm" @click="showForm = true">+ Create</button>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="subscriptions.length === 0" class="empty-card">No webhook subscriptions.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Target URL</th><th>Secret</th><th>Event Types</th><th>Active</th><th>Created</th></tr></thead>
                        <tbody>
                            <tr v-for="s in subscriptions" :key="s.webhookSubscriptionId">
                                <td>{{ s.webhookSubscriptionId }}</td>
                                <td class="table-sub">{{ s.targetUrl }}</td>
                                <td class="table-sub">{{ s.secretReference?.substring(0, 8) }}...</td>
                                <td>{{ s.eventTypes === '*' ? 'All' : s.eventTypes }}</td>
                                <td><span class="badge" :class="s.isActive ? 'badge-success' : 'badge-secondary'">{{ s.isActive ? 'Active' : 'Inactive' }}</span></td>
                                <td class="table-sub">{{ new Date(s.createdAtUtc).toLocaleString() }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Status</span><h2 class="panel-title">Delivery Summary</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else>
                    <div class="kpi-row">
                        <div class="kpi-card"><strong>{{ deliveryStats.total }}</strong><span>Total</span></div>
                        <div class="kpi-card"><strong>{{ deliveryStats.delivered }}</strong><span>Delivered</span></div>
                        <div class="kpi-card"><strong>{{ deliveryStats.failed }}</strong><span>Failed</span></div>
                        <div class="kpi-card"><strong>{{ deliveryStats.pending }}</strong><span>Pending</span></div>
                    </div>
                    <div class="chart-container">
                        <h3>Delivery Status</h3>
                        <div class="pie-chart">
                            <div class="pie-segment delivered" :style="{ width: deliveredPercent + '%', transform: 'rotate(' + totalAngle + 'deg)' }"></div>
                            <div class="pie-segment failed" :style="{ width: failedPercent + '%', transform: 'rotate(' + (totalAngle + deliveredAngle) + 'deg)' }"></div>
                            <div class="pie-segment pending" :style="{ width: pendingPercent + '%', transform: 'rotate(' + (totalAngle + deliveredAngle + failedAngle) + 'deg)' }"></div>
                        </div>
                    </div>
                </div>
            </article>
        </section>
        <div v-if="showForm" class="modal-overlay" @click.self="showForm = false">
            <div class="modal-box">
                <h3>Create Webhook Subscription</h3>
                <div class="form-group">
                    <label>Target URL</label>
                    <input v-model="form.targetUrl" class="form-input" placeholder="https://example.com/webhook" />
                </div>
                <div class="form-group">
                    <label>Secret Reference</label>
                    <input v-model="form.secretReference" class="form-input" placeholder="secret-reference" />
                </div>
                <div class="form-group">
                    <label>Event Types</label>
                    <input v-model="form.eventTypes" class="form-input" placeholder="*, SecurityEvent, Alarm" />
                </div>
                <div class="form-group">
                    <label>Active</label>
                    <select v-model="form.isActive" class="form-select">
                        <option :value="true">Active</option>
                        <option :value="false">Inactive</option>
                    </select>
                </div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showForm = false">Cancel</button>
                    <button class="btn btn-primary" :disabled="busy" @click="submitSubscription">{{ busy ? 'Creating...' : 'Create' }}</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const subscriptions = ref([])
const loading = ref(true)
const busy = ref(false)
const showForm = ref(false)
const form = reactive({ targetUrl: '', secretReference: '', eventTypes: '*', isActive: true })

const deliveryStats = ref({ total: 0, delivered: 0, failed: 0, pending: 0 })
const deliveredPercent = ref(0)
const failedPercent = ref(0)
const pendingPercent = ref(0)
const totalAngle = ref(0)
const deliveredAngle = ref(0)
const failedAngle = ref(0)

async function loadSubscriptions() {
    loading.value = true
    try {
        const [subsRes, deliveriesRes] = await Promise.all([
            enterpriseApi.getWebhookSubscriptions({ limit: 50 }),
            enterpriseApi.getWebhookDeliveries({ limit: 100 })
        ])
        subscriptions.value = Array.isArray(subsRes.data) ? subsRes.data : []
        const deliveries = Array.isArray(deliveriesRes.data) ? deliveriesRes.data : []
        deliveryStats.value = {
            total: deliveries.length,
            delivered: deliveries.filter(d => d.status === 'Delivered').length,
            failed: deliveries.filter(d => d.status === 'Failed').length,
            pending: deliveries.filter(d => d.status !== 'Delivered' && d.status !== 'Failed').length
        }
        const total = deliveryStats.value.total || 1
        deliveredPercent.value = (deliveryStats.value.delivered / total) * 100
        failedPercent.value = (deliveryStats.value.failed / total) * 100
        pendingPercent.value = (deliveryStats.value.pending / total) * 100
        deliveredAngle.value = deliveredPercent.value / 360 * 360
        failedAngle.value = failedPercent.value / 360 * 360
        totalAngle.value = 360
    } catch { subscriptions.value = []; deliveryStats.value = { total: 0, delivered: 0, failed: 0, pending: 0 } }
    finally { loading.value = false }
}

async function submitSubscription() {
    if (!form.targetUrl.trim()) return
    busy.value = true
    try {
        await enterpriseApi.createWebhookSubscription({
            targetUrl: form.targetUrl.trim(),
            secretReference: form.secretReference?.trim(),
            eventTypes: form.eventTypes === '*' ? '*' : form.eventTypes.trim(),
            isActive: form.isActive
        })
        showForm.value = false
        form.targetUrl = ''
        form.secretReference = ''
        form.eventTypes = '*'
        form.isActive = true
        await loadSubscriptions()
    } finally { busy.value = false }
}

onMounted(loadSubscriptions)
</script>
<style>
.kpi-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 1rem; margin: 1rem 0; }
.kpi-card { background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 4px; padding: 1rem; text-align: center; }
.kpi-card strong { font-size: 1.5rem; display: block; }
.kpi-card span { font-size: 0.9rem; color: #6c757d; }
.chart-container { margin-top: 1rem; }
.pie-chart { position: relative; width: 200px; height: 200px; margin: 0 auto; border-radius: 50%; background: conic-gradient(#28a745 var(--angle1), transparent 0), conic-gradient(#dc3545 var(--angle2), transparent 0), conic-gradient(#ffc107 var(--angle3), transparent 0); }
.pie-segment { position: absolute; width: 100%; height: 100%; border-radius: 50%; }
</style>
