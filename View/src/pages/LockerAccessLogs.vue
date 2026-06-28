<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <h1>Nhật ký truy cập tủ locker</h1>
            <router-link to="/locker-manager" class="btn btn-secondary">← Quay lại</router-link>
        </div>

        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="ops-panel-header">
                    <h3>Lịch sử truy cập</h3>
                    <div class="filter-row">
                        <select v-model="filterCompartmentId" class="form-control" @change="loadLogs">
                            <option :value="null">Tất cả ngăn</option>
                            <option v-for="c in allCompartments" :key="c.lockerCompartmentId" :value="c.lockerCompartmentId">
                                {{ c.cabinet?.name || 'Tủ #' + c.lockerCabinetId }} - {{ c.code }}
                            </option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="loading-spinner">Đang tải...</div>
                <table class="data-table" v-else>
                    <thead><tr><th>Thời gian</th><th>Ngăn tủ</th><th>Hành động</th><th>Người thực hiện</th></tr></thead>
                    <tbody>
                        <tr v-for="log in logs" :key="log.lockerAccessLogId">
                            <td>{{ formatDateTime(log.accessedAtUtc) }}</td>
                            <td>{{ log.compartment?.cabinet?.name || '' }} - {{ log.compartment?.code || log.lockerCompartmentId }}</td>
                            <td><span :class="'badge badge-' + actionClass(log.action)">{{ actionLabel(log.action) }}</span></td>
                            <td>{{ log.accessedByUser?.fullName || log.accessedByUserId || '---' }}</td>
                        </tr>
                        <tr v-if="!logs.length"><td colspan="4" class="empty-state">Chưa có dữ liệu.</td></tr>
                    </tbody>
                </table>
            </article>
        </section>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { lostFoundApi } from '../services/enterpriseSecurityApi'

const route = useRoute()
const logs = ref([])
const allCompartments = ref([])
const loading = ref(false)
const filterCompartmentId = ref(route.query.compartmentId ? Number(route.query.compartmentId) : null)

onMounted(async () => {
    await Promise.all([loadLogs(), loadCompartments()])
})

async function loadLogs() {
    loading.value = true
    try {
        const params = { limit: 200 }
        if (filterCompartmentId.value) params.compartmentId = filterCompartmentId.value
        const res = await lostFoundApi.getLockerAccessLogs(params)
        logs.value = res.data || []
    } catch (e) { console.error(e) }
    finally { loading.value = false }
}

async function loadCompartments() {
    try {
        const res = await lostFoundApi.getLockerCabinets()
        const cabinets = res.data || []
        const all = []
        for (const c of cabinets) {
            const detail = await lostFoundApi.getLockerCabinetDetail(c.lockerCabinetId).catch(() => null)
            if (detail?.data?.compartments) {
                all.push(...detail.data.compartments.map(comp => ({ ...comp, cabinet: detail.data.cabinet || c })))
            }
        }
        allCompartments.value = all
    } catch (e) { /* ignore */ }
}

function formatDateTime(d) { return d ? new Date(d).toLocaleString('vi-VN') : '' }
function actionClass(a) { const m = { Assign: 'warning', Release: 'success' }; return m[a] || 'secondary' }
function actionLabel(a) { const m = { Assign: 'Gán', Release: 'Giải phóng' }; return m[a] || a }
</script>

<style scoped>
.filter-row { display: flex; gap: 0.5rem; align-items: center; }
.filter-row .form-control { width: 220px; }
.loading-spinner { text-align: center; padding: 2rem; color: var(--text-secondary); }
</style>
