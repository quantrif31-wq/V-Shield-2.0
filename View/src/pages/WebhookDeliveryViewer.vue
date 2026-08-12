<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Webhook</span>
                <h1 class="page-title">Trình xem chuyển phát Webhook</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadSubscriptions">Làm mới</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Đăng ký</span><h2 class="panel-title">Đăng ký Webhook</h2></div>
                    <div class="panel-actions">
                        <button class="btn btn-secondary btn-sm" @click="showForm = true">+ Tạo mới</button>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="subscriptions.length === 0" class="empty-card">Chưa có đăng ký webhook.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>URL đích</th><th>Khóa bí mật</th><th>Loại sự kiện</th><th>Kích hoạt</th><th>Ngày tạo</th></tr></thead>
                        <tbody>
                            <tr v-for="s in subscriptions" :key="s.webhookSubscriptionId">
                                <td>{{ s.webhookSubscriptionId }}</td>
                                <td class="table-sub">{{ s.targetUrl }}</td>
                                <td class="table-sub">{{ s.secretReference?.substring(0, 8) }}...</td>
                                <td>{{ s.eventTypes === '*' ? 'Tất cả' : s.eventTypes }}</td>
                                <td><span class="badge" :class="s.isActive ? 'badge-success' : 'badge-secondary'">{{ s.isActive ? 'Hoạt động' : 'Không hoạt động' }}</span></td>
                                <td class="table-sub">{{ new Date(s.createdAtUtc).toLocaleString() }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Trạng thái</span><h2 class="panel-title">Tóm tắt chuyển phát</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else>
                    <div class="kpi-row">
                        <div class="kpi-card"><strong>{{ deliveryStats.total }}</strong><span>Tổng</span></div>
                        <div class="kpi-card"><strong>{{ deliveryStats.delivered }}</strong><span>Đã gửi</span></div>
                        <div class="kpi-card"><strong>{{ deliveryStats.failed }}</strong><span>Thất bại</span></div>
                        <div class="kpi-card"><strong>{{ deliveryStats.pending }}</strong><span>Chờ xử lý</span></div>
                    </div>
                    <div class="chart-container">
                        <h3>Trạng thái chuyển phát</h3>
                        <div class="pie-chart" :style="{ '--angle1': deliveredPercent + '%', '--angle2': failedPercent + '%', '--angle3': pendingPercent + '%' }">
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
                <h3>Tạo đăng ký Webhook</h3>
                <div class="form-group">
                    <label>URL đích</label>
                    <input v-model="form.targetUrl" class="form-input" placeholder="https://example.com/webhook" />
                </div>
                <div class="form-group">
                    <label>Tham chiếu khóa bí mật</label>
                    <input v-model="form.secretReference" class="form-input" placeholder="secret-reference" />
                </div>
                <div class="form-group">
                    <label>Loại sự kiện</label>
                    <input v-model="form.eventTypes" class="form-input" placeholder="*, SecurityEvent, Alarm" />
                </div>
                <div class="form-group">
                    <label>Kích hoạt</label>
                    <select v-model="form.isActive" class="form-select">
                        <option :value="true">Kích hoạt</option>
                        <option :value="false">Không hoạt động</option>
                    </select>
                </div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showForm = false">Hủy</button>
                    <button class="btn btn-primary" :disabled="busy" @click="submitSubscription">{{ busy ? 'Đang tạo...' : 'Tạo' }}</button>
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
.kpi-card { background: var(--surface-subtle); border: 1px solid var(--border-default); border-radius: 4px; padding: 1rem; text-align: center; }
.kpi-card strong { font-size: 1.5rem; display: block; }
.kpi-card span { font-size: 0.9rem; color: var(--text-muted); }
.chart-container { margin-top: 1rem; }
.pie-chart { position: relative; width: 200px; height: 200px; margin: 0 auto; border-radius: 50%; background: conic-gradient(var(--status-success-text) var(--angle1), transparent 0), conic-gradient(var(--status-danger-text) var(--angle2), transparent 0), conic-gradient(var(--status-warning-text) var(--angle3), transparent 0); }
.pie-segment { position: absolute; width: 100%; height: 100%; border-radius: 50%; }
</style>
