<template>
    <div class="page-container animate-in">
        <header class="page-header">
            <div>
                <h1 class="page-title">Đơn xin nghỉ</h1>
                <p class="page-subtitle">Gửi yêu cầu nghỉ phép và theo dõi trạng thái xử lý.</p>
            </div>
            <button class="btn btn-primary" @click="showModal = true">Tạo đơn nghỉ</button>
        </header>

        <section class="card panel">
            <div class="toolbar-shell">
                <div class="toolbar-filters">
                    <select v-model="filters.status" class="filter-select" @change="loadRequests">
                        <option value="">Tất cả trạng thái</option>
                        <option value="Pending">Chờ duyệt</option>
                        <option value="Approved">Đã duyệt</option>
                        <option value="Rejected">Đã từ chối</option>
                        <option value="Cancelled">Đã hủy</option>
                    </select>
                    <select v-model="filters.leaveType" class="filter-select" @change="loadRequests">
                        <option value="">Tất cả loại nghỉ</option>
                        <option v-for="type in leaveTypes" :key="type" :value="type">{{ leaveTypeLabel(type) }}</option>
                    </select>
                </div>
            </div>

            <div v-if="loading" class="empty-card">Đang tải đơn nghỉ...</div>
            <div v-else-if="error" class="empty-card">{{ error }}</div>
            <div v-else-if="requests.length === 0" class="empty-card">Bạn chưa có đơn nghỉ nào.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Loại nghỉ</th>
                            <th>Từ ngày</th>
                            <th>Đến ngày</th>
                            <th>Lý do</th>
                            <th>Trạng thái</th>
                            <th>Người duyệt</th>
                            <th>Ngày tạo</th>
                            <th class="text-right">Hành động</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in requests" :key="item.leaveRequestId">
                            <td>{{ leaveTypeLabel(item.leaveType) }}</td>
                            <td>{{ formatDate(item.startDate) }}</td>
                            <td>{{ formatDate(item.endDate) }}</td>
                            <td>{{ item.reason }}</td>
                            <td>
                                <span class="badge info">{{ statusLabel(item.status) }}</span>
                            </td>
                            <td>{{ item.approverName || '--' }}</td>
                            <td>{{ formatDateTime(item.createdAt) }}</td>
                            <td class="text-right">
                                <button
                                    v-if="item.status === 'Pending'"
                                    class="btn btn-danger btn-sm"
                                    @click="confirmCancel(item)"
                                >
                                    Hủy đơn
                                </button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <transition name="modal">
            <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
                <div class="modal">
                    <div class="modal-header">
                        <h3 class="modal-title">Tạo đơn xin nghỉ</h3>
                        <button class="modal-close" @click="showModal = false">✕</button>
                    </div>
                    <form @submit.prevent="submitRequest">
                        <div class="form-group">
                            <label>Loại nghỉ</label>
                            <select v-model="form.leaveType" required>
                                <option value="">-- Chọn loại nghỉ --</option>
                                <option v-for="type in leaveTypes" :key="type" :value="type">{{ leaveTypeLabel(type) }}</option>
                            </select>
                        </div>
                        <div class="form-row">
                            <div class="form-group">
                                <label>Từ ngày</label>
                                <input v-model="form.startDate" type="date" required />
                            </div>
                            <div class="form-group">
                                <label>Đến ngày</label>
                                <input v-model="form.endDate" type="date" required />
                            </div>
                        </div>
                        <div class="form-group">
                            <label>Lý do</label>
                            <textarea v-model="form.reason" required></textarea>
                        </div>
                        <p v-if="modalError" class="error-text">{{ modalError }}</p>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" @click="showModal = false">Hủy</button>
                            <button class="btn btn-primary" :disabled="actionLoading">
                                {{ actionLoading ? 'Đang gửi...' : 'Gửi đơn' }}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </transition>

        <transition name="modal">
            <div v-if="confirmDialog.open" class="modal-overlay" @click.self="confirmDialog.open = false">
                <div class="modal mini">
                    <div class="modal-header">
                        <h3 class="modal-title">Xác nhận hủy đơn</h3>
                    </div>
                    <p>Bạn có chắc muốn hủy đơn nghỉ này?</p>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="confirmDialog.open = false">Không</button>
                        <button class="btn btn-danger" @click="handleCancel" :disabled="actionLoading">Hủy đơn</button>
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
import {
    attendanceStatusLabelMap,
    cancelLeaveRequest,
    createLeaveRequest,
    getLeaveRequests,
    leaveTypeLabelMap,
} from '../services/attendanceApi'

const requests = ref([])
const loading = ref(false)
const actionLoading = ref(false)
const error = ref('')
const modalError = ref('')
const showModal = ref(false)
const toast = ref(null)
let toastTimer = null

const leaveTypes = ['AnnualLeave', 'SickLeave', 'UnpaidLeave', 'PersonalLeave', 'Other']

const filters = reactive({
    status: '',
    leaveType: '',
})

const form = reactive({
    leaveType: '',
    startDate: '',
    endDate: '',
    reason: '',
})

const confirmDialog = reactive({
    open: false,
    leaveRequestId: null,
})

const leaveTypeLabel = (type) => leaveTypeLabelMap[type] || type || '--'
const statusLabel = (status) => attendanceStatusLabelMap[status] || status || '--'

const showToast = (message, type = 'success') => {
    if (toastTimer) clearTimeout(toastTimer)
    toast.value = { message, type }
    toastTimer = setTimeout(() => {
        toast.value = null
    }, 2800)
}

const formatDate = (value) => (value ? new Date(value).toLocaleDateString('vi-VN') : '--')
const formatDateTime = (value) => (value ? new Date(value).toLocaleString('vi-VN') : '--')

const loadRequests = async () => {
    loading.value = true
    error.value = ''
    try {
        const params = {}
        if (filters.status) params.status = filters.status
        if (filters.leaveType) params.leaveType = filters.leaveType
        const { data } = await getLeaveRequests(params)
        requests.value = data
    } catch (err) {
        error.value = err?.response?.data?.message || 'Không tải được danh sách đơn nghỉ.'
    } finally {
        loading.value = false
    }
}

const submitRequest = async () => {
    modalError.value = ''
    actionLoading.value = true
    try {
        await createLeaveRequest({
            leaveType: form.leaveType,
            startDate: form.startDate,
            endDate: form.endDate,
            reason: form.reason.trim(),
        })
        showToast('Đã gửi đơn xin nghỉ')
        showModal.value = false
        Object.assign(form, { leaveType: '', startDate: '', endDate: '', reason: '' })
        await loadRequests()
    } catch (err) {
        modalError.value = err?.response?.data?.message || 'Không thể gửi đơn nghỉ.'
    } finally {
        actionLoading.value = false
    }
}

const confirmCancel = (item) => {
    confirmDialog.open = true
    confirmDialog.leaveRequestId = item.leaveRequestId
}

const handleCancel = async () => {
    if (!confirmDialog.leaveRequestId) return
    actionLoading.value = true
    try {
        await cancelLeaveRequest(confirmDialog.leaveRequestId)
        confirmDialog.open = false
        showToast('Đã hủy đơn nghỉ')
        await loadRequests()
    } catch (err) {
        showToast(err?.response?.data?.message || 'Không thể hủy đơn nghỉ.', 'error')
    } finally {
        actionLoading.value = false
    }
}

onMounted(loadRequests)
</script>

<style scoped>
.panel {
    padding: 22px;
}

.text-right {
    text-align: right;
}

.mini {
    width: min(420px, 100%);
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

