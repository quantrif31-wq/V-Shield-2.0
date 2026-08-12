<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">SIEM</span>
                <h1 class="page-title">Trạng thái xuất dữ liệu SIEM</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadExports">Làm mới</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Xuất dữ liệu</span><h2 class="panel-title">Sự kiện xuất dữ liệu SIEM</h2></div>
                    <div class="panel-actions">
                        <select v-model="statusFilter" @change="loadExports" class="form-select">
                            <option value="">Tất cả</option>
                            <option value="Pending">Đang chờ</option>
                            <option value="Completed">Hoàn thành</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="exports.length === 0" class="empty-card">Không có dữ liệu xuất SIEM.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Mã sự kiện</th><th>Loại sự kiện</th><th>Trạng thái</th><th>Mã tương quan</th><th>Thời gian tạo</th></tr></thead>
                        <tbody>
                            <tr v-for="e in exports" :key="e.outboxEventId">
                                <td>{{ e.outboxEventId }}</td>
                                <td>{{ e.sourceId || '—' }}</td>
                                <td><span class="badge badge-info">{{ e.eventType }}</span></td>
                                <td><span class="badge" :class="e.status === 'Completed' ? 'badge-success' : 'badge-warn'">{{ statusLabels[e.status] || e.status }}</span></td>
                                <td class="table-sub">{{ e.correlationId }}</td>
                                <td class="table-sub">{{ new Date(e.createdAtUtc).toLocaleString() }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Lược đồ</span><h2 class="panel-title">Kiểm tra lược đồ xuất dữ liệu</h2></div>
                </div>
                <div class="empty-card">
                    <p>Xuất dữ liệu SIEM sử dụng lược đồ sự kiện chuẩn hóa với mã tương quan.</p>
                    <p>Loại sự kiện hỗ trợ: SecurityEvent, Alarm, AccessDenied, v.v.</p>
                    <p>Payload bao gồm: nguồn, đích, thời gian, mức độ nghiêm trọng, tác nhân.</p>
                </div>
            </article>
        </section>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const exports = ref([])
const loading = ref(true)
const statusFilter = ref('')

const statusLabels = { Pending: 'Đang chờ', Completed: 'Hoàn thành' }

async function loadExports() {
    loading.value = true
    try {
        const res = await enterpriseApi.getSiemExports({ status: statusFilter.value || undefined })
        exports.value = Array.isArray(res.data) ? res.data : []
    } catch { exports.value = [] }
    finally { loading.value = false }
}

onMounted(loadExports)
</script>

<style scoped>
.form-select:hover {
    border-color: var(--border-strong);
}
</style>
