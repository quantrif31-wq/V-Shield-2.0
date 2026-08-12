<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Tuân thủ</span>
                <h1 class="page-title">Báo cáo tuân thủ</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="runReport">Chạy báo cáo</button>
                <button class="btn btn-secondary" @click="loadReports">Làm mới</button>
            </div>
        </div>
        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Báo cáo</span><h2 class="panel-title">Báo cáo đã tạo</h2></div>
                    <div class="panel-actions">
                        <select v-model="reportTypeFilter" @change="loadReports" class="form-select">
                            <option value="">Tất cả loại</option>
                            <option value="AccessReview">Rà soát truy cập</option>
                            <option value="TerminatedUserRevocation">Thu hồi quyền người nghỉ việc</option>
                            <option value="VisitorLog">Nhật ký khách</option>
                            <option value="EvidenceAccess">Truy cập bằng chứng</option>
                            <option value="PrivilegedAction">Thao tác đặc quyền</option>
                            <option value="AlarmSLA">SLA cảnh báo</option>
                            <option value="DeviceHealth">Sức khỏe thiết bị</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="reports.length === 0" class="empty-card">Chưa có báo cáo tuân thủ. Nhấn "Chạy báo cáo" để tạo báo cáo mới.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Loại</th><th>Kỳ</th><th>Trạng thái</th><th>Đầu ra</th><th>Đã tạo</th></tr></thead>
                        <tbody>
                            <tr v-for="r in reports" :key="r.complianceReportRunId">
                                <td><span class="badge badge-info">{{ reportTypeLabel(r.reportType) }}</span></td>
                                <td class="table-sub">{{ new Date(r.periodStartUtc).toLocaleDateString() }} — {{ new Date(r.periodEndUtc).toLocaleDateString() }}</td>
                                <td><span class="badge badge-success">{{ statusLabel(r.status) }}</span></td>
                                <td class="table-sub">{{ r.outputReference || '—' }}</td>
                                <td class="table-sub">{{ new Date(r.createdAtUtc).toLocaleString() }}</td>
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

const reports = ref([])
const loading = ref(true)
const reportTypeFilter = ref('')

const reportTypeLabelMap = { AccessReview: 'Rà soát truy cập', TerminatedUserRevocation: 'Thu hồi quyền người nghỉ việc', VisitorLog: 'Nhật ký khách', EvidenceAccess: 'Truy cập bằng chứng', PrivilegedAction: 'Thao tác đặc quyền', AlarmSLA: 'SLA cảnh báo', DeviceHealth: 'Sức khỏe thiết bị' }
function reportTypeLabel(value) { return reportTypeLabelMap[value] || value }

const statusLabelMap = { Completed: 'Hoàn tất', Running: 'Đang chạy', Pending: 'Đang chờ', Failed: 'Lỗi', Generated: 'Đã tạo', Started: 'Đã khởi chạy' }
function statusLabel(value) { return statusLabelMap[value] || value }

async function loadReports() {
    loading.value = true
    try {
        const res = await enterpriseApi.getComplianceReports({ reportType: reportTypeFilter.value || undefined, limit: 50 })
        reports.value = Array.isArray(res.data) ? res.data : []
    } catch { reports.value = [] }
    finally { loading.value = false }
}

async function runReport() {
    const type = prompt('Loại báo cáo (AccessReview, TerminatedUserRevocation, VisitorLog, EvidenceAccess, PrivilegedAction, AlarmSLA, DeviceHealth):', 'AccessReview')
    if (!type) return
    const days = prompt('Số ngày xem lại:', '30')
    if (!days) return
    const end = new Date()
    const start = new Date(end.getTime() - parseInt(days) * 86400000)
    try {
        await enterpriseApi.runComplianceReport({
            reportType: type,
            periodStartUtc: start.toISOString(),
            periodEndUtc: end.toISOString()
        })
        await loadReports()
    } catch { alert('Không thể chạy báo cáo') }
}

onMounted(loadReports)
</script>
