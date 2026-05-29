<template>
    <div class="page-container animate-in">
        <header class="page-header">
            <div>
                <h1 class="page-title">Bảng chấm công</h1>
                <p class="page-subtitle">Theo dõi check-in/check-out, đi trễ, về sớm và tăng ca của nhân viên.</p>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" @click="manualCheckIn" :disabled="!myEmployeeId || actionLoading">
                    Check-in thủ công
                </button>
                <button class="btn btn-primary" @click="manualCheckOut" :disabled="!myEmployeeId || actionLoading">
                    Check-out thủ công
                </button>
                <button class="btn btn-secondary" @click="showToast('TODO: tích hợp export Excel ở backend/export service', 'error')">
                    Xuất Excel
                </button>
            </div>
        </header>

        <section class="card panel">
            <div class="toolbar-shell">
                <div class="toolbar-filters">
                    <input v-model="filters.fromDate" type="date" class="date-input" @change="loadAttendances" />
                    <input v-model="filters.toDate" type="date" class="date-input" @change="loadAttendances" />
                    <select v-model="filters.employeeId" class="filter-select" @change="loadAttendances">
                        <option value="">Tất cả nhân viên</option>
                        <option v-for="emp in employees" :key="emp.employeeId" :value="String(emp.employeeId)">
                            {{ emp.fullName }}
                        </option>
                    </select>
                    <select v-model="filters.departmentId" class="filter-select" @change="loadAttendances">
                        <option value="">Tất cả phòng ban</option>
                        <option v-for="dep in departments" :key="dep.departmentId" :value="String(dep.departmentId)">
                            {{ dep.name }}
                        </option>
                    </select>
                    <select v-model="filters.status" class="filter-select" @change="loadAttendances">
                        <option value="">Tất cả trạng thái</option>
                        <option v-for="status in attendanceStatuses" :key="status" :value="status">
                            {{ statusLabel(status) }}
                        </option>
                    </select>
                </div>
            </div>

            <div v-if="loading" class="empty-card">Đang tải dữ liệu chấm công...</div>
            <div v-else-if="error" class="empty-card">{{ error }}</div>
            <div v-else-if="attendances.length === 0" class="empty-card">Không có bản ghi phù hợp.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nhân viên</th>
                            <th>Phòng ban</th>
                            <th>Ngày</th>
                            <th>Ca làm</th>
                            <th>Check-in</th>
                            <th>Check-out</th>
                            <th>Đi trễ</th>
                            <th>Về sớm</th>
                            <th>Tăng ca</th>
                            <th>Tổng giờ</th>
                            <th>Trạng thái</th>
                            <th>Nguồn</th>
                            <th class="text-right">Hành động</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in attendances" :key="item.attendanceId">
                            <td>{{ item.employeeName }}</td>
                            <td>{{ item.departmentName || '--' }}</td>
                            <td>{{ formatDate(item.workDate) }}</td>
                            <td>{{ item.shiftName || 'Ngoài lịch' }}</td>
                            <td>{{ formatDateTime(item.checkIn) }}</td>
                            <td>{{ formatDateTime(item.checkOut) }}</td>
                            <td>{{ item.lateMinutes }} phút</td>
                            <td>{{ item.earlyLeaveMinutes }} phút</td>
                            <td>{{ Number(item.overtimeHours || 0).toFixed(2) }}h</td>
                            <td>{{ Number(item.totalWorkingHours || 0).toFixed(2) }}h</td>
                            <td>
                                <span class="badge info">{{ statusLabel(item.status) }}</span>
                            </td>
                            <td>{{ item.source }}</td>
                            <td class="text-right">
                                <button class="btn btn-secondary btn-sm" @click="openEditModal(item)">Sửa</button>
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
                        <h3 class="modal-title">Cập nhật bản ghi công</h3>
                        <button class="modal-close" @click="showModal = false">✕</button>
                    </div>
                    <form @submit.prevent="submitEdit">
                        <div class="form-row">
                            <div class="form-group">
                                <label>Check-in</label>
                                <input v-model="editForm.checkIn" type="datetime-local" />
                            </div>
                            <div class="form-group">
                                <label>Check-out</label>
                                <input v-model="editForm.checkOut" type="datetime-local" />
                            </div>
                        </div>
                        <div class="form-row">
                            <div class="form-group">
                                <label>Trạng thái</label>
                                <select v-model="editForm.status">
                                    <option value="">Tự động tính</option>
                                    <option v-for="status in attendanceStatuses" :key="status" :value="status">
                                        {{ statusLabel(status) }}
                                    </option>
                                </select>
                            </div>
                            <div class="form-group">
                                <label>Nguồn</label>
                                <select v-model="editForm.source">
                                    <option value="">Giữ nguyên</option>
                                    <option v-for="source in attendanceSources" :key="source" :value="source">
                                        {{ source }}
                                    </option>
                                </select>
                            </div>
                        </div>
                        <div class="form-group">
                            <label>Ghi chú</label>
                            <textarea v-model="editForm.note"></textarea>
                        </div>
                        <p v-if="modalError" class="error-text">{{ modalError }}</p>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" @click="showModal = false">Hủy</button>
                            <button class="btn btn-primary" :disabled="actionLoading">
                                {{ actionLoading ? 'Đang lưu...' : 'Lưu thay đổi' }}
                            </button>
                        </div>
                    </form>
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
import { authState } from '../stores/auth'
import { getAll as getEmployees } from '../services/employeeApi'
import { getDepartments } from '../services/lookupApi'
import {
    attendanceStatusLabelMap,
    checkInAttendance,
    checkOutAttendance,
    getAttendances,
    updateAttendance,
} from '../services/attendanceApi'

const myEmployeeId = authState.user?.employeeId || null

const attendances = ref([])
const employees = ref([])
const departments = ref([])
const loading = ref(false)
const actionLoading = ref(false)
const error = ref('')
const modalError = ref('')
const showModal = ref(false)
const editingId = ref(null)
const toast = ref(null)
let toastTimer = null

const attendanceStatuses = [
    'NotCheckedIn',
    'CheckedIn',
    'Completed',
    'Late',
    'EarlyLeave',
    'LateAndEarlyLeave',
    'Absent',
    'Leave',
    'ForgotCheckout',
    'OutOfSchedule',
]

const attendanceSources = ['Manual', 'AccessLog', 'QR', 'FaceAI', 'Card']

const filters = reactive({
    fromDate: '',
    toDate: '',
    employeeId: '',
    departmentId: '',
    status: '',
})

const editForm = reactive({
    checkIn: '',
    checkOut: '',
    status: '',
    source: '',
    note: '',
})

const statusLabel = (status) => attendanceStatusLabelMap[status] || status || '--'

const showToast = (message, type = 'success') => {
    if (toastTimer) clearTimeout(toastTimer)
    toast.value = { message, type }
    toastTimer = setTimeout(() => {
        toast.value = null
    }, 2800)
}

const formatDate = (value) => (value ? new Date(value).toLocaleDateString('vi-VN') : '--')
const formatDateTime = (value) => {
    if (!value) return '--'
    return new Date(value).toLocaleString('vi-VN', {
        hour12: false,
        hour: '2-digit',
        minute: '2-digit',
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
    })
}

const toLocalDatetimeInput = (value) => {
    if (!value) return ''
    const dt = new Date(value)
    const pad = (n) => String(n).padStart(2, '0')
    return `${dt.getFullYear()}-${pad(dt.getMonth() + 1)}-${pad(dt.getDate())}T${pad(dt.getHours())}:${pad(dt.getMinutes())}`
}

const loadLookup = async () => {
    try {
        const [empRes, depRes] = await Promise.all([getEmployees(), getDepartments()])
        employees.value = empRes.data || []
        departments.value = depRes.data || []
    } catch {
        // Scope permission may limit lookup access.
    }
}

const loadAttendances = async () => {
    loading.value = true
    error.value = ''
    try {
        const params = {}
        if (filters.fromDate) params.fromDate = filters.fromDate
        if (filters.toDate) params.toDate = filters.toDate
        if (filters.employeeId) params.employeeId = Number(filters.employeeId)
        if (filters.departmentId) params.departmentId = Number(filters.departmentId)
        if (filters.status) params.status = filters.status
        const { data } = await getAttendances(params)
        attendances.value = data
    } catch (err) {
        error.value = err?.response?.data?.message || 'Không tải được dữ liệu chấm công.'
    } finally {
        loading.value = false
    }
}

const manualCheckIn = async () => {
    if (!myEmployeeId) return
    actionLoading.value = true
    try {
        await checkInAttendance({ employeeId: myEmployeeId, source: 'Manual' })
        showToast('Check-in thành công')
        await loadAttendances()
    } catch (err) {
        showToast(err?.response?.data?.message || 'Check-in thất bại.', 'error')
    } finally {
        actionLoading.value = false
    }
}

const manualCheckOut = async () => {
    if (!myEmployeeId) return
    actionLoading.value = true
    try {
        await checkOutAttendance({ employeeId: myEmployeeId, source: 'Manual' })
        showToast('Check-out thành công')
        await loadAttendances()
    } catch (err) {
        showToast(err?.response?.data?.message || 'Check-out thất bại.', 'error')
    } finally {
        actionLoading.value = false
    }
}

const openEditModal = (item) => {
    editingId.value = item.attendanceId
    modalError.value = ''
    Object.assign(editForm, {
        checkIn: toLocalDatetimeInput(item.checkIn),
        checkOut: toLocalDatetimeInput(item.checkOut),
        status: item.status || '',
        source: item.source || '',
        note: item.note || '',
    })
    showModal.value = true
}

const submitEdit = async () => {
    if (!editingId.value) return
    modalError.value = ''
    actionLoading.value = true
    try {
        const payload = {
            checkIn: editForm.checkIn ? new Date(editForm.checkIn).toISOString() : null,
            checkOut: editForm.checkOut ? new Date(editForm.checkOut).toISOString() : null,
            status: editForm.status || null,
            source: editForm.source || null,
            note: editForm.note?.trim() || null,
        }
        await updateAttendance(editingId.value, payload)
        showModal.value = false
        showToast('Đã cập nhật bản ghi chấm công')
        await loadAttendances()
    } catch (err) {
        modalError.value = err?.response?.data?.message || 'Không thể cập nhật bản ghi.'
    } finally {
        actionLoading.value = false
    }
}

onMounted(async () => {
    const now = new Date()
    const start = new Date(now.getFullYear(), now.getMonth(), 1)
    filters.fromDate = start.toISOString().slice(0, 10)
    filters.toDate = now.toISOString().slice(0, 10)
    await loadLookup()
    await loadAttendances()
})
</script>

<style scoped>
.panel {
    padding: 22px;
}

.date-input {
    min-height: 42px;
    border-radius: 12px;
    border: 1px solid var(--border-color);
    background: var(--bg-input);
    padding: 0 12px;
}

.text-right {
    text-align: right;
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

