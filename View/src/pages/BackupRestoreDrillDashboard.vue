<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Sao lưu & phục hồi</span>
                <h1 class="page-title">Diễn tập sao lưu/phục hồi</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadDrills">Làm mới</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Diễn tập</span><h2 class="panel-title">Diễn tập phục hồi</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="drills.length === 0" class="empty-card">Chưa có diễn tập phục hồi nào.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Hồ sơ</th><th>Trạng thái</th><th>Bắt đầu</th><th>RPO/RTO</th><th>Kết quả</th></tr></thead>
                        <tbody>
                            <tr v-for="d in drills" :key="d.restoreDrillId">
                                <td>{{ d.restoreDrillId }}</td>
                                <td>{{ d.profile }}</td>
                                <td><span class="badge" :class="d.status === 'Completed' ? 'badge-success' : 'badge-warn'">{{ runStatusLabel(d.status) }}</span></td>
                                <td class="table-sub">{{ new Date(d.startedAtUtc).toLocaleString() }}</td>
                                <td class="table-sub">{{ d.targetRpoMinutes }}min / {{ d.targetRtoMinutes }}min</td>
                                <td><span class="badge" :class="d.passed ? 'badge-success' : 'badge-danger'">{{ d.passed ? 'ĐẠT' : 'KHÔNG ĐẠT' }}</span></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Sao lưu</span><h2 class="panel-title">Các lượt sao lưu</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="backups.length === 0" class="empty-card">Chưa có lượt sao lưu nào.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Hồ sơ</th><th>Trạng thái</th><th>Bắt đầu</th><th>Dung lượng</th></tr></thead>
                        <tbody>
                            <tr v-for="b in backups" :key="b.backupRunId">
                                <td>{{ b.backupRunId }}</td>
                                <td>{{ b.profile }}</td>
                                <td><span class="badge" :class="b.status === 'Completed' ? 'badge-success' : b.status === 'Failed' ? 'badge-danger' : 'badge-warn'">{{ runStatusLabel(b.status) }}</span></td>
                                <td class="table-sub">{{ new Date(b.startedAtUtc).toLocaleString() }}</td>
                                <td>{{ b.sizeBytes ? (b.sizeBytes / (1024*1024)).toFixed(2) + ' MB' : '—' }}</td>
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

const runStatusLabelMap = { Completed: 'Hoàn tất', Failed: 'Lỗi', Running: 'Đang chạy', Pending: 'Đang chờ', Started: 'Đã khởi chạy', Cancelled: 'Đã hủy' }
function runStatusLabel(value) { return runStatusLabelMap[value] || value }

const backups = ref([])
const drills = ref([])
const loading = ref(true)

async function loadDrills() {
    loading.value = true
    try {
        const [backupsRes, drillsRes] = await Promise.all([
            enterpriseApi.getBackupRuns({ limit: 10 }),
            enterpriseApi.getRestoreDrills({ limit: 10 })
        ])
        backups.value = Array.isArray(backupsRes.data) ? backupsRes.data : []
        drills.value = Array.isArray(drillsRes.data) ? drillsRes.data : []
    } catch { backups.value = []; drills.value = [] }
    finally { loading.value = false }
}

onMounted(loadDrills)
</script>
