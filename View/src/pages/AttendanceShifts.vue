<template>
    <div class="page-container animate-in">
        <header class="page-header">
            <div>
                <h1 class="page-title">Ca làm việc</h1>
                <p class="page-subtitle">Quản lý cấu hình ca làm cho module chấm công.</p>
            </div>
            <button class="btn btn-primary" @click="openCreateModal">Thêm ca làm</button>
        </header>

        <section class="card panel">
            <div class="toolbar-shell">
                <div class="search-bar">
                    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="11" cy="11" r="8" />
                        <path d="M21 21l-4.35-4.35" />
                    </svg>
                    <input v-model="filters.search" placeholder="Tìm theo tên ca..." @input="debouncedLoad" />
                </div>
                <div class="toolbar-filters">
                    <select v-model="filters.isActive" class="filter-select" @change="loadShifts">
                        <option value="">Tất cả trạng thái</option>
                        <option value="true">Đang hoạt động</option>
                        <option value="false">Đã khóa</option>
                    </select>
                </div>
            </div>

            <div v-if="loading" class="empty-card">Đang tải dữ liệu...</div>
            <div v-else-if="error" class="empty-card">{{ error }}</div>
            <div v-else-if="shifts.length === 0" class="empty-card">Chưa có ca làm phù hợp bộ lọc.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Tên ca</th>
                            <th>Bắt đầu</th>
                            <th>Kết thúc</th>
                            <th>Nghỉ giữa ca</th>
                            <th>Cho phép trễ</th>
                            <th>Cho phép sớm</th>
                            <th>Trạng thái</th>
                            <th class="text-right">Hành động</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="shift in shifts" :key="shift.shiftId">
                            <td>{{ shift.shiftName }}</td>
                            <td>{{ formatTimeOnly(shift.startTime) }}</td>
                            <td>{{ formatTimeOnly(shift.endTime) }}</td>
                            <td>{{ shift.breakMinutes }} phút</td>
                            <td>{{ shift.allowedLateMinutes }} phút</td>
                            <td>{{ shift.allowedEarlyLeaveMinutes }} phút</td>
                            <td>
                                <span class="badge" :class="shift.isActive ? 'active' : 'inactive'">
                                    {{ shift.isActive ? 'Hoạt động' : 'Đã khóa' }}
                                </span>
                            </td>
                            <td class="text-right action-cell">
                                <button class="btn btn-secondary btn-sm" @click="openEditModal(shift)">Sửa</button>
                                <button
                                    v-if="shift.isActive"
                                    class="btn btn-danger btn-sm"
                                    @click="confirmDeactivate(shift)"
                                >
                                    Khóa
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
                        <h3 class="modal-title">{{ isEdit ? 'Cập nhật ca làm' : 'Thêm ca làm' }}</h3>
                        <button class="modal-close" @click="closeModal">✕</button>
                    </div>

                    <form @submit.prevent="submitForm">
                        <div class="form-group">
                            <label>Tên ca</label>
                            <input v-model="form.shiftName" required />
                        </div>
                        <div class="form-row">
                            <div class="form-group">
                                <label>Giờ bắt đầu</label>
                                <input v-model="form.startTime" type="time" required />
                            </div>
                            <div class="form-group">
                                <label>Giờ kết thúc</label>
                                <input v-model="form.endTime" type="time" required />
                            </div>
                        </div>
                        <div class="form-row">
                            <div class="form-group">
                                <label>Nghỉ giữa ca (phút)</label>
                                <input v-model.number="form.breakMinutes" type="number" min="0" />
                            </div>
                            <div class="form-group">
                                <label>Cho phép đi trễ (phút)</label>
                                <input v-model.number="form.allowedLateMinutes" type="number" min="0" />
                            </div>
                        </div>
                        <div class="form-row">
                            <div class="form-group">
                                <label>Cho phép về sớm (phút)</label>
                                <input v-model.number="form.allowedEarlyLeaveMinutes" type="number" min="0" />
                            </div>
                            <div class="form-group">
                                <label>Trạng thái</label>
                                <select v-model="form.isActive">
                                    <option :value="true">Hoạt động</option>
                                    <option :value="false">Đã khóa</option>
                                </select>
                            </div>
                        </div>

                        <p v-if="modalError" class="error-text">{{ modalError }}</p>

                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" @click="closeModal">Hủy</button>
                            <button type="submit" class="btn btn-primary" :disabled="saving">
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
                        <h3 class="modal-title">Xác nhận khóa ca</h3>
                    </div>
                    <p>Khóa ca <strong>{{ confirmDialog.name }}</strong>?</p>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="confirmDialog.open = false">Hủy</button>
                        <button class="btn btn-danger" @click="handleDeactivate" :disabled="saving">Khóa</button>
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
import { createShift, deactivateShift, getShifts, updateShift } from '../services/attendanceApi'

const loading = ref(false)
const saving = ref(false)
const error = ref('')
const modalError = ref('')
const shifts = ref([])
const showModal = ref(false)
const isEdit = ref(false)
const editId = ref(null)
const toast = ref(null)
let toastTimer = null

const filters = reactive({
    search: '',
    isActive: '',
})

const form = reactive({
    shiftName: '',
    startTime: '',
    endTime: '',
    breakMinutes: 0,
    allowedLateMinutes: 5,
    allowedEarlyLeaveMinutes: 5,
    isActive: true,
})

const confirmDialog = reactive({
    open: false,
    shiftId: null,
    name: '',
})

let searchTimer = null
const debouncedLoad = () => {
    if (searchTimer) clearTimeout(searchTimer)
    searchTimer = setTimeout(() => loadShifts(), 300)
}

const showToast = (message, type = 'success') => {
    if (toastTimer) clearTimeout(toastTimer)
    toast.value = { message, type }
    toastTimer = setTimeout(() => {
        toast.value = null
    }, 2800)
}

const normalizeTime = (value) => (value && value.length === 5 ? `${value}:00` : value)

const loadShifts = async () => {
    loading.value = true
    error.value = ''
    try {
        const params = {}
        if (filters.search.trim()) params.search = filters.search.trim()
        if (filters.isActive !== '') params.isActive = filters.isActive === 'true'
        const { data } = await getShifts(params)
        shifts.value = data
    } catch (err) {
        error.value = err?.response?.data?.message || 'Không tải được danh sách ca làm.'
    } finally {
        loading.value = false
    }
}

const resetForm = () => {
    Object.assign(form, {
        shiftName: '',
        startTime: '',
        endTime: '',
        breakMinutes: 0,
        allowedLateMinutes: 5,
        allowedEarlyLeaveMinutes: 5,
        isActive: true,
    })
}

const openCreateModal = () => {
    isEdit.value = false
    editId.value = null
    modalError.value = ''
    resetForm()
    showModal.value = true
}

const openEditModal = (shift) => {
    isEdit.value = true
    editId.value = shift.shiftId
    modalError.value = ''
    Object.assign(form, {
        shiftName: shift.shiftName,
        startTime: formatTimeOnly(shift.startTime),
        endTime: formatTimeOnly(shift.endTime),
        breakMinutes: shift.breakMinutes,
        allowedLateMinutes: shift.allowedLateMinutes,
        allowedEarlyLeaveMinutes: shift.allowedEarlyLeaveMinutes,
        isActive: shift.isActive,
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
            shiftName: form.shiftName.trim(),
            startTime: normalizeTime(form.startTime),
            endTime: normalizeTime(form.endTime),
            breakMinutes: Number(form.breakMinutes) || 0,
            allowedLateMinutes: Number(form.allowedLateMinutes) || 0,
            allowedEarlyLeaveMinutes: Number(form.allowedEarlyLeaveMinutes) || 0,
            isActive: !!form.isActive,
        }

        if (isEdit.value && editId.value) {
            await updateShift(editId.value, payload)
            showToast('Đã cập nhật ca làm')
        } else {
            await createShift(payload)
            showToast('Đã thêm ca làm')
        }
        closeModal()
        await loadShifts()
    } catch (err) {
        modalError.value = err?.response?.data?.message || 'Không thể lưu ca làm.'
    } finally {
        saving.value = false
    }
}

const confirmDeactivate = (shift) => {
    confirmDialog.open = true
    confirmDialog.shiftId = shift.shiftId
    confirmDialog.name = shift.shiftName
}

const handleDeactivate = async () => {
    if (!confirmDialog.shiftId) return
    saving.value = true
    try {
        await deactivateShift(confirmDialog.shiftId)
        confirmDialog.open = false
        showToast('Đã khóa ca làm')
        await loadShifts()
    } catch (err) {
        showToast(err?.response?.data?.message || 'Khóa ca thất bại.', 'error')
    } finally {
        saving.value = false
    }
}

const formatTimeOnly = (value) => {
    if (!value) return '--'
    if (typeof value === 'string') return value.slice(0, 5)
    return String(value).slice(0, 5)
}

onMounted(loadShifts)
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
    width: min(460px, 100%);
}

.error-text {
    color: var(--accent-danger);
    margin-top: 6px;
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

