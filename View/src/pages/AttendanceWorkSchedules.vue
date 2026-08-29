<template>
    <div class="page-container animate-in">
        <header class="page-header">
            <div>
                <h1 class="page-title">Lịch làm việc</h1>
                <p class="page-subtitle">Lên lịch ca làm, theo dõi trạng thái làm việc theo ngày.</p>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" @click="showImportModal = true">Nhập dữ liệu</button>
                <button class="btn btn-secondary" @click="showExportModal = true">Xuất dữ liệu</button>
                <button class="btn btn-secondary" @click="openBulkModal">Tạo lịch hàng loạt</button>
                <button class="btn btn-primary" @click="openCreateModal">Tạo lịch làm</button>
            </div>
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
                    Có thể tạo hàng loạt theo nhiều nhân viên và khoảng ngày bằng nút ở đầu trang.
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
                            <div class="form-group" :class="{ 'has-error': fieldErrors.employeeId }">
                                <label>Nhân viên <span class="req">*</span></label>
                                <select v-model.number="form.employeeId" required @change="clearFieldError('employeeId')">
                                    <option :value="null">-- Chọn nhân viên --</option>
                                    <option v-for="emp in employees" :key="emp.employeeId" :value="emp.employeeId">
                                        {{ emp.fullName }}
                                    </option>
                                </select>
                                <p v-if="fieldErrors.employeeId" class="field-error" role="alert">{{ fieldErrors.employeeId }}</p>
                            </div>
                            <div class="form-group" :class="{ 'has-error': fieldErrors.shiftId }">
                                <label>Ca làm <span class="req">*</span></label>
                                <select v-model.number="form.shiftId" required @change="clearFieldError('shiftId')">
                                    <option :value="null">-- Chọn ca làm --</option>
                                    <option v-for="shift in shifts" :key="shift.shiftId" :value="shift.shiftId">
                                        {{ shift.shiftName }} ({{ formatTime(shift.startTime) }} - {{ formatTime(shift.endTime) }})
                                    </option>
                                </select>
                                <p v-if="fieldErrors.shiftId" class="field-error" role="alert">{{ fieldErrors.shiftId }}</p>
                            </div>
                        </div>
                        <div class="form-group" :class="{ 'has-error': fieldErrors.workDate }">
                            <label>Ngày làm <span class="req">*</span></label>
                            <input v-model="form.workDate" type="date" required @input="clearFieldError('workDate')" />
                            <p v-if="fieldErrors.workDate" class="field-error" role="alert">{{ fieldErrors.workDate }}</p>
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
            <div v-if="showBulkModal" class="modal-overlay" @click.self="closeBulkModal">
                <div class="modal wide-modal">
                    <div class="modal-header">
                        <h3 class="modal-title">Tạo lịch hàng loạt</h3>
                        <button class="modal-close" @click="closeBulkModal">✕</button>
                    </div>
                    <form @submit.prevent="submitBulkForm">
                        <div class="form-row">
                            <div class="form-group" :class="{ 'has-error': bulkErrors.shiftId }">
                                <label>Ca làm <span class="req">*</span></label>
                                <select v-model.number="bulkForm.shiftId" required @change="clearBulkError('shiftId')">
                                    <option :value="null">-- Chọn ca làm --</option>
                                    <option v-for="shift in shifts" :key="shift.shiftId" :value="shift.shiftId">
                                        {{ shift.shiftName }} ({{ formatTime(shift.startTime) }} - {{ formatTime(shift.endTime) }})
                                    </option>
                                </select>
                                <p v-if="bulkErrors.shiftId" class="field-error" role="alert">{{ bulkErrors.shiftId }}</p>
                            </div>
                            <div class="form-group">
                                <label>Phòng ban</label>
                                <select v-model="bulkForm.departmentId">
                                    <option value="">Tất cả phòng ban</option>
                                    <option v-for="dep in departments" :key="dep.departmentId" :value="String(dep.departmentId)">
                                        {{ dep.name }}
                                    </option>
                                </select>
                            </div>
                        </div>
                        <div class="form-row">
                            <div class="form-group" :class="{ 'has-error': bulkErrors.fromDate }">
                                <label>Từ ngày <span class="req">*</span></label>
                                <input v-model="bulkForm.fromDate" type="date" required @input="clearBulkError('fromDate'); clearBulkError('toDate')" />
                                <p v-if="bulkErrors.fromDate" class="field-error" role="alert">{{ bulkErrors.fromDate }}</p>
                            </div>
                            <div class="form-group" :class="{ 'has-error': bulkErrors.toDate }">
                                <label>Đến ngày <span class="req">*</span></label>
                                <input v-model="bulkForm.toDate" type="date" required @input="clearBulkError('toDate'); clearBulkError('fromDate')" />
                                <p v-if="bulkErrors.toDate" class="field-error" role="alert">{{ bulkErrors.toDate }}</p>
                            </div>
                        </div>
                        <div class="form-group" :class="{ 'has-error': bulkErrors.employeeIds }">
                            <label>Nhân viên áp dụng <span class="req">*</span></label>
                            <div class="bulk-toolbar">
                                <button type="button" class="btn btn-ghost btn-sm" @click="selectFilteredEmployees">Chọn theo bộ lọc</button>
                                <button type="button" class="btn btn-ghost btn-sm" @click="clearBulkEmployees">Bỏ chọn</button>
                                <span class="bulk-selection-label">Đã chọn {{ bulkForm.employeeIds.length }} nhân viên</span>
                            </div>
                            <div class="bulk-employee-list">
                                <label v-for="emp in filteredBulkEmployees" :key="emp.employeeId" class="bulk-employee-item">
                                    <input
                                        :checked="bulkForm.employeeIds.includes(emp.employeeId)"
                                        type="checkbox"
                                        @change="toggleBulkEmployee(emp.employeeId)"
                                    />
                                    <span>{{ emp.fullName }}</span>
                                    <small>{{ emp.departmentName || 'Chưa có phòng ban' }}</small>
                                </label>
                            </div>
                            <p v-if="bulkErrors.employeeIds" class="field-error" role="alert">{{ bulkErrors.employeeIds }}</p>
                        </div>
                        <div class="form-group">
                            <label>Ghi chú</label>
                            <textarea v-model="bulkForm.note" placeholder="Ghi chú áp dụng cho toàn bộ lịch"></textarea>
                        </div>

                        <p v-if="bulkError" class="error-text">{{ bulkError }}</p>

                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" @click="closeBulkModal">Hủy</button>
                            <button class="btn btn-primary" :disabled="saving">
                                {{ saving ? 'Đang tạo...' : 'Tạo hàng loạt' }}
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

        <ImportModal v-if="showImportModal" entity-type="WorkSchedule" entity-display-name="Lịch làm việc" @close="showImportModal = false" @import-complete="onImportComplete" />
        <ExportModal v-if="showExportModal" entity-type="WorkSchedule" entity-display-name="Lịch làm việc" :available-columns="['WorkScheduleId','EmployeeEmail','ShiftName','WorkDate','Status','Note']" @close="showExportModal = false" />
    </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { getAll as getEmployees } from '../services/employeeApi'
import { getDepartments } from '../services/lookupApi'
import ImportModal from '../components/import-export/ImportModal.vue'
import ExportModal from '../components/import-export/ExportModal.vue'
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
const bulkError = ref('')
const showModal = ref(false)
const showBulkModal = ref(false)
const showImportModal = ref(false)
const showExportModal = ref(false)
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

const fieldErrors = reactive({
    employeeId: '',
    shiftId: '',
    workDate: '',
})

const bulkForm = reactive({
    shiftId: null,
    departmentId: '',
    fromDate: '',
    toDate: '',
    employeeIds: [],
    note: '',
})

const bulkErrors = reactive({
    shiftId: '',
    fromDate: '',
    toDate: '',
    employeeIds: '',
})

const confirmDialog = reactive({
    open: false,
    scheduleId: null,
    employeeName: '',
    workDate: '',
})

const filteredBulkEmployees = computed(() => {
    if (!bulkForm.departmentId) return employees.value
    return employees.value.filter((emp) => String(emp.departmentId || '') === bulkForm.departmentId)
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

const onImportComplete = (result) => {
    showImportModal.value = false
    loadSchedules()
    const message = `${result.successCount} bản ghi thành công${result.errorCount ? `, ${result.errorCount} lỗi` : ''}`
    result.errorCount ? showToast(message, 'error') : showToast(message, 'success')
}

const openCreateModal = () => {
    isEdit.value = false
    editId.value = null
    modalError.value = ''
    Object.keys(fieldErrors).forEach((key) => { fieldErrors[key] = '' })
    Object.assign(form, {
        employeeId: null,
        shiftId: null,
        workDate: '',
        note: '',
    })
    showModal.value = true
}

const openBulkModal = () => {
    bulkError.value = ''
    Object.keys(bulkErrors).forEach((key) => { bulkErrors[key] = '' })
    bulkForm.shiftId = null
    bulkForm.departmentId = ''
    bulkForm.fromDate = filters.fromDate
    bulkForm.toDate = filters.toDate
    bulkForm.employeeIds = []
    bulkForm.note = ''
    showBulkModal.value = true
}

const openEditModal = (item) => {
    isEdit.value = true
    editId.value = item.scheduleId
    modalError.value = ''
    Object.keys(fieldErrors).forEach((key) => { fieldErrors[key] = '' })
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

const closeBulkModal = () => {
    showBulkModal.value = false
}

const toggleBulkEmployee = (employeeId) => {
    if (bulkForm.employeeIds.includes(employeeId)) {
        bulkForm.employeeIds = bulkForm.employeeIds.filter((id) => id !== employeeId)
        return
    }

    bulkForm.employeeIds = [...bulkForm.employeeIds, employeeId]
}

const selectFilteredEmployees = () => {
    bulkForm.employeeIds = filteredBulkEmployees.value.map((emp) => emp.employeeId)
}

const clearBulkEmployees = () => {
    bulkForm.employeeIds = []
}

const enumerateDates = (fromDate, toDate) => {
    const dates = []
    const start = new Date(fromDate)
    const end = new Date(toDate)

    for (let current = new Date(start); current <= end; current.setDate(current.getDate() + 1)) {
        dates.push(new Date(current).toISOString().slice(0, 10))
    }

    return dates
}

const submitBulkForm = async () => {
    bulkError.value = ''
    Object.keys(bulkErrors).forEach((key) => { bulkErrors[key] = '' })

    bulkErrors.shiftId = bulkForm.shiftId ? '' : 'Vui lòng chọn ca làm.'
    bulkErrors.fromDate = bulkForm.fromDate ? '' : 'Vui lòng chọn ngày bắt đầu.'
    bulkErrors.toDate = bulkForm.toDate ? '' : 'Vui lòng chọn ngày kết thúc.'
    if (bulkForm.fromDate && bulkForm.toDate) {
        if (bulkForm.toDate < bulkForm.fromDate) {
            bulkErrors.toDate = 'Ngày kết thúc không được nhỏ hơn ngày bắt đầu.'
        }
    }
    bulkErrors.employeeIds = bulkForm.employeeIds.length ? '' : 'Vui lòng chọn ít nhất một nhân viên.'

    if (Object.values(bulkErrors).some((msg) => msg)) return

    saving.value = true

    const dates = enumerateDates(bulkForm.fromDate, bulkForm.toDate)
    let created = 0
    let duplicated = 0
    let failed = 0
    const failedTargets = []

    try {
        for (const employeeId of bulkForm.employeeIds) {
            for (const workDate of dates) {
                try {
                    await createWorkSchedule({
                        employeeId,
                        shiftId: bulkForm.shiftId,
                        workDate,
                        note: bulkForm.note?.trim() || null,
                    })
                    created += 1
                } catch (err) {
                    if (err?.response?.status === 409) {
                        duplicated += 1
                    } else {
                        failed += 1
                        failedTargets.push(`NV#${employeeId} ${workDate}`)
                    }
                }
            }
        }

        closeBulkModal()
        await loadSchedules()

        const summary = [
            created ? `Tạo mới ${created}` : null,
            duplicated ? `bỏ qua ${duplicated} lịch trùng` : null,
            failed ? `lỗi ${failed}` : null,
        ].filter(Boolean).join(', ')

        showToast(summary ? `Hoàn tất tạo lịch hàng loạt: ${summary}.` : 'Không có lịch nào được tạo.', failed ? 'error' : 'success')

        if (failedTargets.length) {
            bulkError.value = `Một số lịch lỗi: ${failedTargets.slice(0, 5).join('; ')}${failedTargets.length > 5 ? '...' : ''}`
        }
    } finally {
        saving.value = false
    }
}

const submitForm = async () => {
    modalError.value = ''

    fieldErrors.employeeId = form.employeeId ? '' : 'Vui lòng chọn nhân viên.'
    fieldErrors.shiftId = form.shiftId ? '' : 'Vui lòng chọn ca làm.'
    fieldErrors.workDate = form.workDate ? '' : 'Vui lòng chọn ngày làm.'

    if (Object.values(fieldErrors).some((msg) => msg)) return

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

const clearFieldError = (field) => {
    if (fieldErrors[field]) fieldErrors[field] = ''
}

const clearBulkError = (field) => {
    if (bulkErrors[field]) bulkErrors[field] = ''
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
.header-actions {
    display: flex;
    gap: 10px;
}

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

.bulk-toolbar {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 12px;
    flex-wrap: wrap;
}

.bulk-selection-label {
    font-size: 0.84rem;
    color: var(--text-muted);
}

.bulk-employee-list {
    max-height: 280px;
    overflow: auto;
    border: 1px solid var(--border-color);
    border-radius: 14px;
    padding: 10px;
    background: var(--bg-input);
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 10px;
}

.bulk-employee-item {
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding: 10px 12px;
    border-radius: 12px;
    background: var(--surface-subtle);
    border: 1px solid var(--border-subtle);
}

.bulk-employee-item input {
    align-self: flex-start;
}

.bulk-employee-item small {
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

