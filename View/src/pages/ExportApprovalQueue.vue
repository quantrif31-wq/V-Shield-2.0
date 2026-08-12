<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Xuất dữ liệu chứng cứ</span>
                <h1 class="page-title">Hàng đợi phê duyệt xuất dữ liệu</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadExports">Làm mới</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Yêu cầu</span><h2 class="panel-title">Yêu cầu xuất dữ liệu</h2></div>
                    <div class="panel-actions">
                        <select v-model="statusFilter" @change="loadExports" class="form-select">
                            <option value="">Tất cả</option>
                            <option value="PendingApproval">Đang chờ</option>
                            <option value="Approved">Đã phê duyệt</option>
                            <option value="Rejected">Đã từ chối</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="exports.length === 0" class="empty-card">Không có yêu cầu xuất dữ liệu.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Mã chứng cứ</th><th>Mã bộ sưu tập</th><th>Mục đích</th><th>Người nhận</th><th>Trạng thái</th><th>Thời gian yêu cầu</th><th>Thao tác</th></tr></thead>
                        <tbody>
                            <tr v-for="r in exports" :key="r.evidenceExportRequestId">
                                <td>{{ r.evidenceExportRequestId }}</td>
                                <td>{{ r.evidenceItemId || '—' }}</td>
                                <td>{{ r.evidenceCollectionId || '—' }}</td>
                                <td class="table-sub">{{ r.purpose.substring(0, 40) }}</td>
                                <td>{{ r.recipient }}</td>
                                <td><span class="badge" :class="r.status === 'Approved' ? 'badge-success' : r.status === 'Rejected' ? 'badge-danger' : 'badge-warn'">{{ statusLabels[r.status] || r.status }}</span></td>
                                <td class="table-sub">{{ new Date(r.requestedAtUtc).toLocaleString() }}</td>
                                <td>
                                    <button v-if="r.status === 'PendingApproval'" class="btn btn-success btn-sm" @click="approve(r)">Phê duyệt</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Thông tin</span><h2 class="panel-title">Hướng dẫn phê duyệt</h2></div>
                </div>
                <div class="empty-card">
                    <p>Mỗi lần xuất dữ liệu yêu cầu phê duyệt đặc quyền với MFA nâng cấp.</p>
                    <p>Watermark (ví dụ: mã vụ việc, tên người nhận) được nhúng khi phê duyệt.</p>
                    <p>Chữ ký được tạo tự động bằng HMAC-SHA256.</p>
                    <p>Băm chứng cứ được xác minh trước khi xuất để đảm bảo toàn vẹn.</p>
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

const statusLabels = { PendingApproval: 'Đang chờ', Approved: 'Đã phê duyệt', Rejected: 'Đã từ chối' }

async function loadExports() {
    loading.value = true
    try {
        const res = await enterpriseApi.getExportRequests({ status: statusFilter.value || undefined })
        exports.value = Array.isArray(res.data) ? res.data : []
    } catch { exports.value = [] }
    finally { loading.value = false }
}

async function approve(r) {
    const watermark = prompt('Văn bản watermark (ví dụ: Vụ việc #123):')
    if (!watermark) return
    try {
        await enterpriseApi.approveExportRequest(r.evidenceExportRequestId, { watermark })
        await loadExports()
    } catch { alert('Phê duyệt thất bại') }
}

onMounted(loadExports)
</script>

<style scoped>
.form-select:hover {
    border-color: var(--border-strong);
}
</style>
