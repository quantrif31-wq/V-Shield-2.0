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
                <button class="btn btn-secondary" @click="deriveNow(null)" :disabled="actionLoading">
                    Tổng hợp từ zone
                </button>
                <button class="btn btn-secondary" @click="exportAttendanceExcel" :disabled="!attendances.length">
                    Xuất Excel
                </button>
                <button class="btn btn-ghost" @click="openAnomalyPanel">
                    🛡️ Bất thường
                </button>
            </div>
        </header>

        <section class="card panel attendance-panel">
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
                            <th>Ngày &amp; ca làm</th>
                            <th>Giờ vào</th>
                            <th>Giờ ra</th>
                            <th>Tổng công</th>
                            <th>Trạng thái</th>
                            <th class="text-right">Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <template v-for="item in paginatedAttendances" :key="item.attendanceId">
                            <tr>
                                <td>
                                    <strong class="cell-primary">{{ item.employeeName }}</strong>
                                    <small class="cell-secondary">{{ item.departmentName || 'Chưa có phòng ban' }}</small>
                                </td>
                                <td>
                                    <strong class="cell-primary">{{ formatDate(item.workDate) }}</strong>
                                    <small class="cell-secondary">{{ item.shiftName || 'Ngoài lịch' }}</small>
                                </td>
                                <td><strong class="cell-primary">{{ formatTime(item.checkIn) }}</strong></td>
                                <td><strong class="cell-primary" :class="{ muted: !item.checkOut }">{{ formatTime(item.checkOut) }}</strong></td>
                                <td>
                                    <strong class="cell-primary">{{ Number(item.totalWorkingHours || 0).toFixed(2) }} giờ</strong>
                                    <small v-if="attendanceNote(item)" class="cell-secondary" :class="attendanceNote(item).tone">{{ attendanceNote(item).text }}</small>
                                </td>
                                <td><span class="attendance-status" :class="statusTone(item.status)">{{ statusLabel(item.status) }}</span></td>
                                <td class="text-right">
                                    <div class="row-actions compact-actions">
                                        <button class="btn btn-secondary btn-sm" @click="openEditModal(item)">Sửa</button>
                                        <button class="detail-toggle" @click="toggleDetails(item.attendanceId)">
                                            Chi tiết <span :class="{ rotated: expandedAttendanceId === item.attendanceId }">⌄</span>
                                        </button>
                                    </div>
                                </td>
                            </tr>
                            <tr v-if="expandedAttendanceId === item.attendanceId" class="detail-row">
                                <td colspan="7">
                                    <div class="attendance-details">
                                        <div><span>Đi trễ</span><strong>{{ item.lateMinutes || 0 }} phút</strong></div>
                                        <div><span>Về sớm</span><strong>{{ item.earlyLeaveMinutes || 0 }} phút</strong></div>
                                        <div><span>Tăng ca</span><strong>{{ Number(item.overtimeHours || 0).toFixed(2) }} giờ</strong></div>
                                        <div><span>Nguồn</span><strong>{{ item.source || '--' }}</strong></div>
                                        <div><span>Trong khu vực</span><strong>{{ item.isZoneDerived ? `${Number(item.zoneDwellTime || 0).toFixed(1)} giờ` : '--' }}</strong></div>
                                        <button class="btn btn-ghost btn-sm" @click="showTransitTimeline(item)">Xem lộ trình</button>
                                    </div>
                                </td>
                            </tr>
                        </template>
                    </tbody>
                </table>
                <footer class="pagination-bar">
                    <div class="pagination-info">
                        <span>Hiển thị {{ attendances.length ? (currentPage - 1) * pageSize + 1 : 0 }}–{{ Math.min(currentPage * pageSize, attendances.length) }} trong {{ attendances.length }} bản ghi</span>
                        <span class="page-size-selector">
                            · Số dòng:
                            <select v-model="pageSize" class="size-select" @change="currentPage = 1">
                                <option :value="5">5</option>
                                <option :value="10">10</option>
                                <option :value="15">15</option>
                                <option :value="20">20</option>
                                <option :value="50">50</option>
                            </select>
                        </span>
                    </div>
                    <div class="pagination-actions">
                        <button class="btn btn-secondary btn-sm" :disabled="currentPage <= 1" @click="currentPage--">Trang trước</button>
                        <span>Trang {{ currentPage }} / {{ totalPages }}</span>
                        <button class="btn btn-secondary btn-sm" :disabled="currentPage >= totalPages" @click="currentPage++">Trang sau</button>
                    </div>
                </footer>
            </div>
        </section>

        <transition name="modal">
            <div v-if="showTransitModal" class="modal-overlay" @click.self="showTransitModal = false">
                <div class="modal wide-modal">
                    <div class="modal-header">
                        <h3 class="modal-title">Lộ trình zone — {{ transitTarget?.employeeName || '' }}</h3>
                        <button class="modal-close" @click="showTransitModal = false">✕</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="transitLoading" class="empty-card">Đang tải lộ trình...</div>
                        <div v-else-if="transits.length === 0" class="empty-card">Không có dữ liệu di chuyển qua zone trong ngày.</div>
                        <div v-else class="transit-timeline">
                            <div v-for="(t, idx) in transits" :key="t.zoneTransitId" class="transit-item">
                                <div class="transit-dot" :class="t.direction === 'IN' ? 'dot-in' : 'dot-out'"></div>
                                <div class="transit-line" v-if="idx < transits.length - 1"></div>
                                <div class="transit-content">
                                    <span class="transit-time">{{ formatDateTime(t.timestamp) }}</span>
                                    <span class="transit-dir-badge" :class="t.direction === 'IN' ? 'badge success' : 'badge warning'">
                                        {{ t.direction === 'IN' ? 'VÀO' : 'RA' }}
                                    </span>
                                    <span class="transit-zone">{{ t.securityZoneName }}</span>
                                    <span v-if="t.gateName" class="transit-gate">@ {{ t.gateName }}</span>
                                    <span class="transit-source">{{ t.source }}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </transition>

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

        <transition name="modal">
            <div v-if="showAnomalyModal" class="modal-overlay" @click.self="showAnomalyModal = false">
                <div class="modal wide-modal">
                    <div class="modal-header">
                        <h3 class="modal-title">Phát hiện bất thường chấm công</h3>
                        <div class="header-actions">
                            <button class="btn btn-secondary btn-sm" @click="runDetection" :disabled="anomalyLoading">
                                {{ anomalyLoading ? 'Đang phân tích...' : 'Quét ngay' }}
                            </button>
                            <button class="modal-close" @click="showAnomalyModal = false">✕</button>
                        </div>
                    </div>
                    <div class="modal-body">
                        <div v-if="anomalyLoading" class="empty-card">Đang phân tích dữ liệu chấm công...</div>
                        <div v-else-if="anomalies.length === 0" class="empty-card">
                            Không phát hiện bất thường nào.
                            <button class="btn btn-secondary btn-sm" style="margin-top:12px" @click="runDetection">Quét lại</button>
                        </div>
                        <div v-else class="anomaly-list">
                            <div v-for="a in anomalies" :key="a.anomalyId" class="anomaly-card" :class="a.severity">
                                <div class="anomaly-head">
                                    <span class="anomaly-type-badge" :class="a.severity">
                                        {{ anomalyTypeLabel(a.anomalyType) }}
                                    </span>
                                    <span class="anomaly-severity" :class="a.severity">
                                        {{ a.severity === 'cao' ? 'Cao' : a.severity === 'trung-binh' ? 'TB' : 'Thấp' }}
                                    </span>
                                    <span class="anomaly-status" :class="a.status">
                                        {{ a.status === 'Open' ? 'Mở' : a.status === 'Resolved' ? 'Đã xử lý' : 'FP' }}
                                    </span>
                                </div>
                                <p class="anomaly-desc">{{ a.description }}</p>
                                <div class="anomaly-meta">
                                    <span>{{ a.employee?.fullName || 'NV#' + a.employeeId }}</span>
                                    <span>{{ formatDate(a.workDate) }}</span>
                                    <span v-if="a.supportingData" class="anomaly-data">{{ a.supportingData }}</span>
                                </div>
                                <div v-if="a.status === 'Open'" class="anomaly-actions">
                                    <button class="btn btn-primary btn-sm" @click="resolveAnomalyHandler(a.anomalyId)">Đã xử lý</button>
                                    <button class="btn btn-ghost btn-sm" @click="falsePositiveHandler(a.anomalyId)">FP</button>
                                </div>
                                <div v-if="a.resolution" class="anomaly-resolution">
                                    <span>Xử lý: {{ a.resolution }}</span>
                                </div>
                            </div>
                        </div>
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
import { computed, onMounted, reactive, ref } from 'vue'
import { authState } from '../stores/auth'
import { getAll as getEmployees } from '../services/employeeApi'
import { getDepartments } from '../services/lookupApi'
import {
    attendanceStatusLabelMap,
    checkInAttendance,
    checkOutAttendance,
    getAttendances,
    updateAttendance,
    getAttendanceTransits,
    deriveAttendance,
    getAttendanceAnomalies,
    detectAttendanceAnomalies,
    resolveAnomaly,
    markAnomalyFalsePositive,
} from '../services/attendanceApi'

const myEmployeeId = authState.user?.employeeId || null

const attendances = ref([])
const employees = ref([])
const departments = ref([])
const currentPage = ref(1)
const pageSize = ref(10)
const expandedAttendanceId = ref(null)
const totalPages = computed(() => Math.max(1, Math.ceil(attendances.value.length / pageSize.value)))
const paginatedAttendances = computed(() => {
    const start = (currentPage.value - 1) * pageSize.value
    return attendances.value.slice(start, start + pageSize.value)
})
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

const attendanceSources = ['Manual', 'AccessLog', 'QR', 'FaceAI', 'Card', 'ZoneTransit']

const showTransitModal = ref(false)
const transitTarget = ref(null)
const transits = ref([])
const transitLoading = ref(false)

const showAnomalyModal = ref(false)
const anomalies = ref([])
const anomalyLoading = ref(false)

const anomalyTypeLabel = (type) => {
    const map = {
        BuddyPunching: 'Buddy Punch',
        SuspiciousTime: 'Giờ đáng ngờ',
        MissingCheckOut: 'Thiếu Check-out',
        ZoneMismatch: 'Lệch Zone',
        AbsencePattern: 'Vắng mặt lặp lại',
        DuplicateCheckIn: 'Trùng Check-in',
    }
    return map[type] || type
}

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
const statusTone = (status) => {
    if (['Late', 'EarlyLeave', 'LateAndEarlyLeave', 'ForgotCheckout'].includes(status)) return 'warning'
    if (['Completed', 'CheckedIn'].includes(status)) return 'success'
    if (status === 'Absent') return 'danger'
    return 'neutral'
}

const showToast = (message, type = 'success') => {
    if (toastTimer) clearTimeout(toastTimer)
    toast.value = { message, type }
    toastTimer = setTimeout(() => {
        toast.value = null
    }, 2800)
}

const formatDate = (value) => (value ? new Date(value).toLocaleDateString('vi-VN') : '--')
const formatTime = (value) => value
    ? new Date(value).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false })
    : '--:--'
const attendanceNote = (item) => {
    if (Number(item.lateMinutes || 0) > 0) return { text: `Trễ ${item.lateMinutes} phút`, tone: 'warning-text' }
    if (Number(item.earlyLeaveMinutes || 0) > 0) return { text: `Sớm ${item.earlyLeaveMinutes} phút`, tone: 'warning-text' }
    if (Number(item.overtimeHours || 0) > 0) return { text: `Tăng ca ${Number(item.overtimeHours).toFixed(2)} giờ`, tone: 'success-text' }
    return null
}
const toggleDetails = (attendanceId) => {
    expandedAttendanceId.value = expandedAttendanceId.value === attendanceId ? null : attendanceId
}
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

const escapeHtml = (value) => String(value ?? '--')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')

const exportAttendanceExcel = () => {
    if (!attendances.value.length) {
        showToast('Không có dữ liệu để xuất.', 'error')
        return
    }

    const headers = [
        'Nhân viên',
        'Phòng ban',
        'Ngày',
        'Ca làm',
        'Check-in',
        'Check-out',
        'Đi trễ (phút)',
        'Về sớm (phút)',
        'Tăng ca (giờ)',
        'Tổng giờ',
        'Trạng thái',
        'Nguồn',
        'Trong zone'
    ]

    const rows = attendances.value.map((item) => [
        item.employeeName,
        item.departmentName || '--',
        formatDate(item.workDate),
        item.shiftName || 'Ngoài lịch',
        formatDateTime(item.checkIn),
        formatDateTime(item.checkOut),
        item.lateMinutes ?? 0,
        item.earlyLeaveMinutes ?? 0,
        Number(item.overtimeHours || 0).toFixed(2),
        Number(item.totalWorkingHours || 0).toFixed(2),
        statusLabel(item.status),
        item.source || '--',
        item.isZoneDerived ? `${Number(item.zoneDwellTime || 0).toFixed(1)}h` : '--'
    ])

    const tableHtml = `
        <table>
            <thead>
                <tr>${headers.map((header) => `<th>${escapeHtml(header)}</th>`).join('')}</tr>
            </thead>
            <tbody>
                ${rows.map((row) => `<tr>${row.map((cell) => `<td>${escapeHtml(cell)}</td>`).join('')}</tr>`).join('')}
            </tbody>
        </table>
    `

    const blob = new Blob(
        [`\ufeff<html><head><meta charset="utf-8" /></head><body>${tableHtml}</body></html>`],
        { type: 'application/vnd.ms-excel;charset=utf-8;' }
    )

    const link = document.createElement('a')
    const from = filters.fromDate || 'tu-ngay'
    const to = filters.toDate || 'den-ngay'
    link.href = URL.createObjectURL(blob)
    link.download = `attendance-${from}-${to}.xls`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(link.href)
    showToast('Đã xuất file Excel.')
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
    currentPage.value = 1
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

const showTransitTimeline = async (item) => {
    transitTarget.value = item
    transits.value = []
    showTransitModal.value = true
    transitLoading.value = true
    try {
        const { data } = await getAttendanceTransits(item.attendanceId)
        transits.value = data || []
    } catch {
        showToast('Không tải được lộ trình zone.', 'error')
    } finally {
        transitLoading.value = false
    }
}

const deriveNow = async (employeeId) => {
    actionLoading.value = true
    try {
        const { data } = await deriveAttendance({ employeeId, date: filters.fromDate || undefined })
        const msg = data.message || (data.processed !== undefined ? `Đã xử lý ${data.processed} bản ghi` : 'Đã tổng hợp')
        showToast(msg)
        await loadAttendances()
    } catch (err) {
        showToast(err?.response?.data?.message || 'Tổng hợp thất bại.', 'error')
    } finally {
        actionLoading.value = false
    }
}

const openAnomalyPanel = async () => {
    showAnomalyModal.value = true
    anomalies.value = []
    anomalyLoading.value = true
    try {
        const { data } = await getAttendanceAnomalies({ maxResults: 50 })
        anomalies.value = data || []
    } catch {
        showToast('Không tải được dữ liệu bất thường.', 'error')
    } finally {
        anomalyLoading.value = false
    }
}

const runDetection = async () => {
    anomalyLoading.value = true
    try {
        const { data } = await detectAttendanceAnomalies()
        showToast(`Phát hiện ${data.detected} bất thường mới.`)
        await openAnomalyPanel()
    } catch (err) {
        showToast(err?.response?.data?.message || 'Quét thất bại.', 'error')
    } finally {
        anomalyLoading.value = false
    }
}

const resolveAnomalyHandler = async (id) => {
    try {
        await resolveAnomaly(id, { resolution: 'Da kiem tra va xu ly.' })
        showToast('Đã đánh dấu xử lý.')
        anomalies.value = anomalies.value.filter(a => a.anomalyId !== id)
    } catch (err) {
        showToast(err?.response?.data?.message || 'Thất bại.', 'error')
    }
}

const falsePositiveHandler = async (id) => {
    try {
        await markAnomalyFalsePositive(id)
        showToast('Đã đánh dấu false positive.')
        anomalies.value = anomalies.value.filter(a => a.anomalyId !== id)
    } catch (err) {
        showToast(err?.response?.data?.message || 'Thất bại.', 'error')
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

.attendance-panel {
    overflow: hidden;
}

.attendance-panel .table-container {
    border: 1px solid var(--border-subtle);
    background: var(--surface-default);
}

.attendance-panel .data-table {
    min-width: 980px;
    table-layout: fixed;
}

.attendance-panel .data-table th:first-child { width: 22%; }
.attendance-panel .data-table th:nth-child(2) { width: 16%; }
.attendance-panel .data-table th:nth-child(3),
.attendance-panel .data-table th:nth-child(4) { width: 9%; }
.attendance-panel .data-table th:nth-child(5) { width: 14%; }
.attendance-panel .data-table th:nth-child(6) { width: 13%; }
.attendance-panel .data-table th:nth-child(7) { width: 17%; }

.attendance-panel .data-table th {
    height: 50px;
    padding-inline: 16px;
    font-size: 0.7rem;
    letter-spacing: 0.13em;
}

.attendance-panel .data-table td {
    height: 74px;
    padding: 13px 16px;
}

.cell-primary,
.cell-secondary {
    display: block;
}

.cell-primary {
    color: var(--text-primary);
    font-size: 0.94rem;
    font-weight: 750;
}

.cell-primary.muted { color: var(--text-muted); }

.cell-secondary {
    margin-top: 6px;
    color: var(--text-muted);
    font-size: 0.78rem;
}

.warning-text { color: var(--accent-warning); }
.success-text { color: var(--accent-success); }

.attendance-status {
    display: inline-flex;
    align-items: center;
    min-height: 28px;
    padding: 5px 12px;
    border-radius: 999px;
    font-size: 0.76rem;
    font-weight: 750;
    white-space: nowrap;
}

.attendance-status.neutral { color: var(--text-muted); background: var(--status-neutral-bg); }
.attendance-status.warning { color: var(--accent-warning); background: var(--status-warning-bg); }
.attendance-status.success { color: var(--accent-success); background: var(--status-success-bg); }
.attendance-status.danger { color: var(--accent-danger); background: var(--status-danger-bg); }

.compact-actions {
    flex-wrap: nowrap;
    gap: 14px;
}

.compact-actions .btn {
    min-width: 54px;
    border-radius: 999px;
    background: var(--surface-default);
}

.detail-toggle {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    border: 0;
    background: transparent;
    color: var(--text-primary);
    font-size: 0.84rem;
    font-weight: 750;
    cursor: pointer;
    transition: color var(--transition-fast);
}

.detail-toggle:hover {
    color: var(--accent-primary);
}

.detail-toggle span {
    display: inline-block;
    transition: transform 180ms ease;
}

.detail-toggle span.rotated { transform: rotate(180deg); }

.detail-row td {
    height: auto !important;
    padding: 0 16px 14px !important;
    background: var(--surface-subtle);
}

.attendance-details {
    display: grid;
    grid-template-columns: repeat(5, minmax(110px, 1fr)) auto;
    align-items: center;
    gap: 14px;
    padding: 14px 16px;
    border: 1px solid var(--border-subtle);
    border-radius: 14px;
    background: var(--surface-default);
}

.attendance-details span,
.attendance-details strong { display: block; }

.attendance-details span {
    color: var(--text-muted);
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
}

.attendance-details strong {
    margin-top: 4px;
    color: var(--text-primary);
    font-size: 0.84rem;
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
    color: var(--text-on-interactive);
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

.wide-modal {
    max-width: 640px;
}

.transit-timeline {
    padding: 16px 0;
    position: relative;
}

.transit-item {
    display: flex;
    align-items: flex-start;
    gap: 12px;
    position: relative;
    min-height: 48px;
}

.transit-dot {
    width: 12px;
    height: 12px;
    border-radius: 50%;
    flex-shrink: 0;
    margin-top: 4px;
    z-index: 1;
}

.dot-in {
    background: var(--accent-success);
}

.dot-out {
    background: var(--accent-warning);
}

.transit-line {
    position: absolute;
    left: 5px;
    top: 16px;
    width: 2px;
    bottom: 0;
    background: var(--border-color);
}

.transit-content {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    align-items: center;
    padding-bottom: 16px;
}

.transit-time {
    font-weight: 600;
    min-width: 110px;
    font-size: 0.9rem;
    color: var(--text-secondary);
}

.transit-dir-badge {
    font-size: 0.75rem;
    font-weight: 700;
}

.transit-zone {
    font-weight: 600;
}

.transit-gate {
    font-size: 0.85rem;
    color: var(--text-secondary);
}

.transit-source {
    font-size: 0.75rem;
    padding: 2px 8px;
    border-radius: 8px;
    background: var(--surface-subtle);
}

.anomaly-list {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.anomaly-card {
    padding: 14px;
    border-radius: 16px;
    border: 1px solid var(--border-subtle);
    background: var(--surface-subtle);
}

.anomaly-card.cao {
    border-color: var(--status-danger-border);
    background: var(--status-danger-bg);
}

.anomaly-card.trung-binh {
    border-color: var(--status-warning-border);
    background: var(--status-warning-bg);
}

.anomaly-head {
    display: flex;
    gap: 8px;
    align-items: center;
    margin-bottom: 8px;
}

.anomaly-type-badge {
    font-size: 0.72rem;
    font-weight: 700;
    padding: 2px 10px;
    border-radius: 20px;
    background: var(--status-info-bg);
    color: var(--accent-primary);
}

.anomaly-type-badge.cao {
    background: var(--status-danger-bg);
    color: var(--status-danger-text);
}

.anomaly-type-badge.trung-binh {
    background: var(--status-warning-bg);
    color: var(--accent-warning);
}

.anomaly-severity {
    font-size: 0.68rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    padding: 1px 8px;
    border-radius: 12px;
}

.anomaly-severity.cao { background: var(--status-danger-bg); color: var(--status-danger-text); }
.anomaly-severity.trung-binh { background: var(--status-warning-bg); color: var(--accent-warning); }
.anomaly-severity.thap { background: var(--status-neutral-bg); color: var(--text-muted); }

.anomaly-status {
    font-size: 0.68rem;
    margin-left: auto;
    padding: 1px 8px;
    border-radius: 12px;
}

.anomaly-status.Open { background: var(--status-info-bg); color: var(--accent-primary); }
.anomaly-status.Resolved { background: var(--status-success-bg); color: var(--accent-success); }
.anomaly-status.FalsePositive { background: var(--status-neutral-bg); color: var(--text-muted); }

.anomaly-desc {
    font-size: 0.88rem;
    line-height: 1.6;
    color: var(--text-primary);
    margin: 0 0 8px 0;
}

.anomaly-meta {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    font-size: 0.78rem;
    color: var(--text-secondary);
}

.anomaly-data {
    font-family: monospace;
    background: var(--surface-subtle);
    padding: 1px 6px;
    border-radius: 4px;
}

.anomaly-actions {
    display: flex;
    gap: 8px;
    margin-top: 10px;
}

.anomaly-resolution {
    margin-top: 8px;
    font-size: 0.78rem;
    color: var(--text-muted);
    font-style: italic;
}
.pagination-bar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 16px 24px;
    border-top: 1px solid var(--border-color);
    background: var(--surface-default);
    color: var(--text-secondary);
    font-size: 0.9rem;
}
.pagination-actions {
    display: flex;
    align-items: center;
    gap: 12px;
}
.pagination-info {
    display: flex;
    align-items: center;
    gap: 8px;
}
.size-select {
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 2px 6px;
    background: var(--bg-input);
    color: var(--text-primary);
    margin-left: 6px;
    cursor: pointer;
    font-size: 0.85rem;
}
</style>

