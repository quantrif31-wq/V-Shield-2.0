<template>
    <div class="page-container animate-in">
        <header class="page-header">
            <div>
                <h1 class="page-title">Lịch làm việc</h1>
                <p class="page-subtitle">Lên lịch ca làm, theo dõi trạng thái làm việc theo ngày.</p>
            </div>
            <button class="btn btn-primary" @click="openCreateModal">Tạo lịch làm</button>
        </header>

        <section class="card panel">
            <div class="toolbar-shell">
                <div class="toolbar-filters">
                    <select v-model="filters.employeeId" class="filter-select" @change="loadSchedules">
                        <option value="">Tất cả nhân viên</option>
                        <option v-for="emp in employees" :key="emp.employeeId" :value="String(emp.employeeId)">
                            {{ emp.fullName }}
                        </option>
                    </select>
                    <select v-model="filters.departmentId" class="filter-select" @change="loadSchedules">
                        <option value="">Tất cả phòng ban</option>
                        <option v-for="dep in departments" :key="dep.departmentId" :value="String(dep.departmentId)">
                            {{ dep.name }}
                        </option>
                    </select>
                    <select v-model="filters.shiftId" class="filter-select" @change="loadSchedules">
                        <option value="">Tất cả ca</option>
                        <option v-for="shift in shifts" :key="shift.shiftId" :value="String(shift.shiftId)">
                            {{ shift.shiftName }}
                        </option>
                    </select>
                    <select v-model="filters.status" class="filter-select" @change="loadSchedules">
                        <option value="">Tất cả trạng thái</option>
                        <option v-for="status in scheduleStatuses" :key="status" :value="status">
                            {{ statusLabel(status) }}
                        </option>
                    </select>
                </div>
            </div>

            <div class="toolbar-shell date-toolbar">
                <div class="toolbar-filters">
                    <label>
                        Từ ngày
                        <input v-model="filters.fromDate" type="date" class="date-input" @change="loadSchedules" />
                    </label>
                    <label>
                        Đến ngày
                        <input v-model="filters.toDate" type="date" class="date-input" @change="loadSchedules" />
                    </label>
                </div>
                <div class="todo-note">
                    TODO: Tạo lịch hàng loạt theo nhiều nhân viên/khoảng ngày sẽ triển khai ở bước tiếp theo.
                </div>
            </div>

            <div v-if="loading" class="empty-card">Đang tải lịch làm...</div>
            <div v-else-if="error" class="empty-card">{{ error }}</div>
            <div v-else-if="schedules.length === 0" class="empty-card">Chưa có lịch làm phù hợp.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nhân viên</th>
                            <th>Phòng ban</th>
                            <th>Ngày làm</th>
                            <th>Ca làm</th>
                            <th>Bắt đầu</th>
                            <th>Kết thúc</th>
                            <th>Trạng thái</th>
                            <th>Ghi chú</th>
                            <th class="text-right">Hành động</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in schedules" :key="item.scheduleId">
                            <td>{{ item.employeeName }}</td>
                            <td>{{ item.departmentName || '--' }}</td>
                            <td>{{ formatDate(item.workDate) }}</td>
                            <td>{{ item.shiftName }}</td>
                            <td>{{ formatTime(item.shiftStartTime) }}</td>
                            <td>{{ formatTime(item.shiftEndTime) }}</td>
                            <td>
                                <span class="badge info">{{ statusLabel(item.status) }}</span>
                            </td>
                            <td>{{ item.note || '--' }}</td>
                            <td class="text-right action-cell">
                                <button class="btn btn-secondary btn-sm" @click="openEditModal(item)">Sửa</button>
                                <button
                                    v-if="item.status !== 'Cancelled'"
                                    class="btn btn-danger btn-sm"
                                    @click="confirmCancel(item)"
                                >
                                    Hủy lịch
                                </button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <transition name="modal">
            <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
                <div class="modal">
                    <div class="modal-header">
                        <h3 class="modal-title">{{ isEdit ? 'Cập nhật lịch làm' : 'Tạo lịch làm' }}</h3>
                        <button class="modal-close" @click="closeModal">✕</button>
                    </div>
                    <form @submit.prevent="submitForm">
                        <div class="form-row">
                            <div class="form-group">
                                <label>Nhân viên</label>
                                <select v-model.number="form.employeeId" required>
                                    <option :value="null">-- Chọn nhân viên --</option>
                                    <option v-for="emp in employees" :key="emp.employeeId" :value="emp.employeeId">
                                        {{ emp.fullName }}
                                    </option>
                                </select>
                            </div>
                            <div class="form-group">
                                <label>Ca làm</label>
                                <select v-model.number="form.shiftId" required>
                                    <option :value="null">-- Chọn ca làm --</option>
                                    <option v-for="shift in shifts" :key="shift.shiftId" :value="shift.shiftId">
                                        {{ shift.shiftName }} ({{ formatTime(shift.startTime) }} - {{ formatTime(shift.endTime) }})
                                    </option>
                                </select>
                            </div>
                        </div>
                        <div class="form-group">
                            <label>Ngày làm</label>
                            <input v-model="form.workDate" type="date" required />
                        </div>
                        <div class="form-group">
                            <label>Ghi chú</label>
                            <textarea v-model="form.note" placeholder="Thêm ghi chú nếu có"></textarea>
                        </div>

                        <p v-if="modalError" class="error-text">{{ modalError }}</p>

                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" @click="closeModal">Hủy</button>
                            <button class="btn btn-primary" :disabled="saving">
                                {{ saving ? 'Đang lưu...' : 'Lưu' }}
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
                        <h3 class="modal-title">Xác nhận hủy lịch</h3>
                    </div>
                    <p>Bạn có chắc muốn hủy lịch của <strong>{{ confirmDialog.employeeName }}</strong> ngày <strong>{{ formatDate(confirmDialog.workDate) }}</strong>?</p>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="confirmDialog.open = false">Không</button>
                        <button class="btn btn-danger" @click="handleCancelSchedule" :disabled="saving">Hủy lịch</button>
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
    attendanceStatusLabelMap,
    cancelWorkSchedule,
    createWorkSchedule,
    getShifts,
    getWorkSchedules,
    updateWorkSchedule,
} from '../services/attendanceApi'

const schedules = ref([])
const shifts = ref([])
const employees = ref([])
const departments = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const modalError = ref('')
const showModal = ref(false)
const isEdit = ref(false)
const editId = ref(null)
const toast = ref(null)
let toastTimer = null

const scheduleStatuses = ['Scheduled', 'Worked', 'Leave', 'Absent', 'Cancelled', 'Changed']

const filters = reactive({
    employeeId: '',
    departmentId: '',
    shiftId: '',
    status: '',
    fromDate: '',
    toDate: '',
})

const form = reactive({
    employeeId: null,
    shiftId: null,
    workDate: '',
    note: '',
})

const confirmDialog = reactive({
    open: false,
    scheduleId: null,
    employeeName: '',
    workDate: '',
})

const showToast = (message, type = 'success') => {
    if (toastTimer) clearTimeout(toastTimer)
    toast.value = { message, type }
    toastTimer = setTimeout(() => {
        toast.value = null
    }, 2800)
}

const statusLabel = (status) => attendanceStatusLabelMap[status] || status || '--'

const formatDate = (value) => {
    if (!value) return '--'
    return new Date(value).toLocaleDateString('vi-VN')
}

const formatTime = (value) => {
    if (!value) return '--'
    if (typeof value === 'string') return value.slice(0, 5)
    return String(value).slice(0, 5)
}

const loadLookups = async () => {
    try {
        const [shiftRes, employeeRes, deptRes] = await Promise.all([
            getShifts({ isActive: true }),
            getEmployees(),
            getDepartments(),
        ])
        shifts.value = shiftRes.data || []
        employees.value = employeeRes.data || []
        departments.value = deptRes.data || []
    } catch {
        // Scope/permission may limit some lookup sources.
    }
}

const loadSchedules = async () => {
    loading.value = true
    error.value = ''
    try {
        const params = {}
        if (filters.employeeId) params.employeeId = Number(filters.employeeId)
        if (filters.departmentId) params.departmentId = Number(filters.departmentId)
        if (filters.shiftId) params.shiftId = Number(filters.shiftId)
        if (filters.status) params.status = filters.status
        if (filters.fromDate) params.fromDate = filters.fromDate
        if (filters.toDate) params.toDate = filters.toDate
        const { data } = await getWorkSchedules(params)
        schedules.value = data
    } catch (err) {
        error.value = err?.response?.data?.message || 'Không tải được lịch làm.'
    } finally {
        loading.value = false
    }
}

const openCreateModal = () => {
    isEdit.value = false
    editId.value = null
    modalError.value = ''
    Object.assign(form, {
        employeeId: null,
        shiftId: null,
        workDate: '',
        note: '',
    })
    showModal.value = true
}

const openEditModal = (item) => {
    isEdit.value = true
    editId.value = item.scheduleId
    modalError.value = ''
    Object.assign(form, {
        employeeId: item.employeeId,
        shiftId: item.shiftId,
        workDate: item.workDate?.slice(0, 10) || '',
        note: item.note || '',
    })
    showModal.value = true
}

const closeModal = () => {
    showModal.value = false
}

const submitForm = async () => {
    modalError.value = ''
    saving.value = true
    try {
        const payload = {
            employeeId: form.employeeId,
            shiftId: form.shiftId,
            workDate: form.workDate,
            note: form.note?.trim() || null,
        }
        if (isEdit.value && editId.value) {
            await updateWorkSchedule(editId.value, payload)
            showToast('Đã cập nhật lịch làm')
        } else {
            await createWorkSchedule(payload)
            showToast('Đã tạo lịch làm')
        }
        closeModal()
        await loadSchedules()
    } catch (err) {
        modalError.value = err?.response?.data?.message || 'Không thể lưu lịch làm.'
    } finally {
        saving.value = false
    }
}

const confirmCancel = (item) => {
    confirmDialog.open = true
    confirmDialog.scheduleId = item.scheduleId
    confirmDialog.employeeName = item.employeeName
    confirmDialog.workDate = item.workDate
}

const handleCancelSchedule = async () => {
    if (!confirmDialog.scheduleId) return
    saving.value = true
    try {
        await cancelWorkSchedule(confirmDialog.scheduleId)
        showToast('Đã hủy lịch làm')
        confirmDialog.open = false
        await loadSchedules()
    } catch (err) {
        showToast(err?.response?.data?.message || 'Hủy lịch thất bại.', 'error')
    } finally {
        saving.value = false
    }
}

onMounted(async () => {
    const now = new Date()
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1)
    const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0)
    filters.fromDate = firstDay.toISOString().slice(0, 10)
    filters.toDate = lastDay.toISOString().slice(0, 10)

    await loadLookups()
    await loadSchedules()
})
</script>

<style scoped>
.panel {
    padding: 22px;
}

.date-toolbar {
    margin: 12px 0 16px;
}

.date-input {
    min-height: 40px;
    padding: 0 10px;
    margin-left: 8px;
    border-radius: 10px;
    border: 1px solid var(--border-color);
    background: var(--bg-input);
}

.todo-note {
    font-size: 0.86rem;
    color: var(--text-muted);
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

