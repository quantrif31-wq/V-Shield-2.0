<template>
    <div class="page-container animate-in">
        <header class="page-header">
            <div>
                <h1 class="page-title">Duyệt đơn nghỉ</h1>
                <p class="page-subtitle">Quản lý/Admin xử lý đơn xin nghỉ của nhân viên theo phòng ban.</p>
            </div>
        </header>

        <section class="card panel">
            <div class="toolbar-shell">
                <div class="toolbar-filters">
                    <select v-model="filters.departmentId" class="filter-select" @change="loadRequests">
                        <option value="">Tất cả phòng ban</option>
                        <option v-for="dep in departments" :key="dep.departmentId" :value="String(dep.departmentId)">
                            {{ dep.name }}
                        </option>
                    </select>
                    <select v-model="filters.employeeId" class="filter-select" @change="loadRequests">
                        <option value="">Tất cả nhân viên</option>
                        <option v-for="emp in employees" :key="emp.employeeId" :value="String(emp.employeeId)">
                            {{ emp.fullName }}
                        </option>
                    </select>
                    <select v-model="filters.status" class="filter-select" @change="loadRequests">
                        <option value="">Tất cả trạng thái</option>
                        <option value="Pending">Chờ duyệt</option>
                        <option value="Approved">Đã duyệt</option>
                        <option value="Rejected">Đã từ chối</option>
                        <option value="Cancelled">Đã hủy</option>
                    </select>
                </div>
            </div>

            <div v-if="loading" class="empty-card">Đang tải danh sách đơn...</div>
            <div v-else-if="error" class="empty-card">{{ error }}</div>
            <div v-else-if="requests.length === 0" class="empty-card">Không có đơn nghỉ phù hợp bộ lọc.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nhân viên</th>
                            <th>Phòng ban</th>
                            <th>Loại nghỉ</th>
                            <th>Từ ngày</th>
                            <th>Đến ngày</th>
                            <th>Lý do</th>
                            <th>Trạng thái</th>
                            <th>Ngày gửi</th>
                            <th class="text-right">Hành động</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in requests" :key="item.leaveRequestId">
                            <td>{{ item.employeeName }}</td>
                            <td>{{ item.departmentName || '--' }}</td>
                            <td>{{ leaveTypeLabel(item.leaveType) }}</td>
                            <td>{{ formatDate(item.startDate) }}</td>
                            <td>{{ formatDate(item.endDate) }}</td>
                            <td>{{ item.reason }}</td>
                            <td><span class="badge info">{{ statusLabel(item.status) }}</span></td>
                            <td>{{ formatDateTime(item.createdAt) }}</td>
                            <td class="text-right action-cell">
                                <template v-if="item.status === 'Pending'">
                                    <button class="btn btn-primary btn-sm" @click="handleApprove(item.leaveRequestId)">Duyệt</button>
                                    <button class="btn btn-danger btn-sm" @click="openRejectModal(item)">Từ chối</button>
                                </template>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <transition name="modal">
            <div v-if="rejectDialog.open" class="modal-overlay" @click.self="rejectDialog.open = false">
                <div class="modal mini">
                    <div class="modal-header">
                        <h3 class="modal-title">Từ chối đơn nghỉ</h3>
                    </div>
                    <div class="form-group">
                        <label>Lý do từ chối</label>
                        <textarea v-model="rejectDialog.reason" placeholder="Nhập lý do từ chối" />
                    </div>
                    <p v-if="modalError" class="error-text">{{ modalError }}</p>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="rejectDialog.open = false">Hủy</button>
                        <button class="btn btn-danger" @click="handleReject" :disabled="actionLoading">Xác nhận từ chối</button>
                    </div>
                </div>
            </div>
        </transition>

        <transition name="toast">
            <div v-if="toast" class="toast-card" :class="toast.type">{{ toast.message }}</div>
        </transition>
    </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { getAll as getEmployees } from '../services/employeeApi'
import { getDepartments } from '../services/lookupApi'
import {
    approveLeaveRequest,
    attendanceStatusLabelMap,
    getLeaveRequests,
    leaveTypeLabelMap,
    rejectLeaveRequest,
} from '../services/attendanceApi'

const loading = ref(false)
const actionLoading = ref(false)
const error = ref('')
const modalError = ref('')
const requests = ref([])
const employees = ref([])
const departments = ref([])
const toast = ref(null)
let toastTimer = null

const filters = reactive({
    departmentId: '',
    employeeId: '',
    status: 'Pending',
})

const rejectDialog = reactive({
    open: false,
    leaveRequestId: null,
    reason: '',
})

const statusLabel = (status) => attendanceStatusLabelMap[status] || status || '--'
const leaveTypeLabel = (type) => leaveTypeLabelMap[type] || type || '--'
const formatDate = (value) => (value ? new Date(value).toLocaleDateString('vi-VN') : '--')
const formatDateTime = (value) => (value ? new Date(value).toLocaleString('vi-VN') : '--')

const showToast = (message, type = 'success') => {
    if (toastTimer) clearTimeout(toastTimer)
    toast.value = { message, type }
    toastTimer = setTimeout(() => {
        toast.value = null
    }, 2800)
}

const loadLookups = async () => {
    try {
        const [empRes, depRes] = await Promise.all([getEmployees(), getDepartments()])
        employees.value = empRes.data || []
        departments.value = depRes.data || []
    } catch {
        // Permissions may restrict these endpoints for non-admin users.
    }
}

const loadRequests = async () => {
    loading.value = true
    error.value = ''
    try {
        const params = {}
        if (filters.departmentId) params.departmentId = Number(filters.departmentId)
        if (filters.employeeId) params.employeeId = Number(filters.employeeId)
        if (filters.status) params.status = filters.status
        const { data } = await getLeaveRequests(params)
        requests.value = data
    } catch (err) {
        error.value = err?.response?.data?.message || 'Không tải được danh sách đơn nghỉ.'
    } finally {
        loading.value = false
    }
}

const handleApprove = async (id) => {
    actionLoading.value = true
    try {
        await approveLeaveRequest(id)
        showToast('Đã duyệt đơn nghỉ')
        await loadRequests()
    } catch (err) {
        showToast(err?.response?.data?.message || 'Duyệt đơn thất bại.', 'error')
    } finally {
        actionLoading.value = false
    }
}

const openRejectModal = (item) => {
    modalError.value = ''
    rejectDialog.open = true
    rejectDialog.leaveRequestId = item.leaveRequestId
    rejectDialog.reason = ''
}

const handleReject = async () => {
    modalError.value = ''
    if (!rejectDialog.reason.trim()) {
        modalError.value = 'Vui lòng nhập lý do từ chối.'
        return
    }

    actionLoading.value = true
    try {
        await rejectLeaveRequest(rejectDialog.leaveRequestId, { rejectReason: rejectDialog.reason.trim() })
        rejectDialog.open = false
        showToast('Đã từ chối đơn nghỉ')
        await loadRequests()
    } catch (err) {
        modalError.value = err?.response?.data?.message || 'Không thể từ chối đơn.'
    } finally {
        actionLoading.value = false
    }
}

onMounted(async () => {
    await loadLookups()
    await loadRequests()
})
</script>

<style scoped>
.panel {
    padding: 22px;
}

.action-cell {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
}

.mini {
    width: min(520px, 100%);
}

.error-text {
    color: var(--accent-danger);
}

.toast-card {
    position: fixed;
    right: 24px;
    bottom: 24px;
    z-index: 1200;
    padding: 12px 18px;
    border-radius: 12px;
    background: var(--accent-success);
    color: #fff;
    box-shadow: var(--shadow-lg);
}

.toast-card.error {
    background: var(--accent-danger);
}

.modal-enter-active,
.modal-leave-active,
.toast-enter-active,
.toast-leave-active {
    transition: all 0.25s ease;
}

.modal-enter-from,
.modal-leave-to,
.toast-enter-from,
.toast-leave-to {
    opacity: 0;
    transform: translateY(12px);
}
</style>

