<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <h1>Duyệt yêu cầu nhận lại đồ</h1>
        </div>

        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="ops-panel-header">
                    <h3>Yêu cầu nhận lại</h3>
                    <div class="filter-row">
                        <select v-model="filter" class="form-control" @change="loadClaims">
                            <option value="">Tất cả</option>
                            <option value="Pending">Chờ duyệt</option>
                            <option value="Approved">Đã duyệt</option>
                            <option value="Completed">Hoàn tất</option>
                        </select>
                    </div>
                </div>
                <table class="data-table">
                    <thead><tr><th>Người nhận</th><th>CMND/CCCD</th><th>SĐT</th><th>Đồ vật</th><th>Ngày yêu cầu</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
                    <tbody>
                        <tr v-for="c in claims" :key="c.claimRequestId">
                            <td>{{ c.claimantName }}</td>
                            <td>{{ c.claimantIdNumber }}</td>
                            <td>{{ c.claimantPhone }}</td>
                            <td style="max-width:200px" class="text-truncate">{{ c.foundItem?.itemDescription || '---' }}</td>
                            <td>{{ formatDate(c.requestedAtUtc) }}</td>
                            <td><span :class="'badge badge-' + statusClass(c.status)">{{ statusLabel(c.status) }}</span></td>
                            <td>
                                <button v-if="c.status === 'Pending'" class="btn btn-sm btn-success" @click="approve(c)">Duyệt</button>
                                <button v-if="c.status === 'Approved'" class="btn btn-sm btn-primary" @click="complete(c)">Trả đồ</button>
                            </td>
                        </tr>
                        <tr v-if="!claims.length"><td colspan="7" class="empty-state">Chưa có yêu cầu nào.</td></tr>
                    </tbody>
                </table>
            </article>
        </section>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { lostFoundApi } from '../services/enterpriseSecurityApi'

const claims = ref([])
const filter = ref('')

onMounted(loadClaims)

async function loadClaims() {
    try {
        const res = await lostFoundApi.getClaimRequests({ status: filter.value || undefined })
        claims.value = res.data || []
    } catch (e) { console.error(e) }
}

async function approve(c) {
    try {
        await lostFoundApi.approveClaimRequest(c.claimRequestId)
        await loadClaims()
    } catch (e) { alert('Lỗi: ' + (e.response?.data?.message || e.message)) }
}

async function complete(c) {
    if (!confirm('Xác nhận đã trả đồ cho "' + c.claimantName + '"?')) return
    try {
        await lostFoundApi.completeClaimRequest(c.claimRequestId)
        await loadClaims()
    } catch (e) { alert('Lỗi: ' + (e.response?.data?.message || e.message)) }
}

function formatDate(d) { return d ? new Date(d).toLocaleDateString('vi-VN') : '' }
function statusClass(s) { const m = { Pending: 'warning', Approved: 'primary', Completed: 'success' }; return m[s] || 'secondary' }
function statusLabel(s) { const m = { Pending: 'Chờ duyệt', Approved: 'Đã duyệt', Completed: 'Hoàn tất' }; return m[s] || s }
</script>

<style scoped>
.text-truncate { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.filter-row { display: flex; gap: 0.5rem; align-items: center; }
.filter-row .form-control { width: 180px; }
</style>
