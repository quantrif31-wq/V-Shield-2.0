<template>
    <div class="page-container animate-in">
        <header class="page-header bento-header">
            <div class="greeting">
                <h1 class="page-title">Lịch làm việc</h1>
                <p class="page-subtitle">Ca trực và lịch cá nhân</p>
            </div>
        </header>

        <div class="bento-card">
            <div class="table-toolbar">
                <div class="filter-box" style="display: flex; gap: 12px;">
                    <input type="month" v-model="filterMonth" class="sleek-input" style="max-width: 200px;" />
                </div>
            </div>
            <div v-if="loading" class="empty-layout">
                <div class="spinner-lg"></div>
                <p>Đang tải...</p>
            </div>
            <div v-else-if="error" class="empty-layout">
                <p style="color: var(--accent-danger);">{{ error }}</p>
            </div>
            <div v-else-if="schedules.length === 0" class="empty-layout">
                <p>Chưa có lịch làm việc.</p>
            </div>
            <div v-else class="sleek-table-container">
                <table class="sleek-table">
                    <thead>
                        <tr>
                            <th>Ngày</th>
                            <th>Ca</th>
                            <th>Thời gian</th>
                            <th>Trạng thái</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="s in schedules" :key="s.workScheduleId" class="table-row">
                            <td>{{ formatDate(s.workDate) }}</td>
                            <td>{{ s.shiftName || '—' }}</td>
                            <td class="text-muted">{{ s.startTime || '—' }} - {{ s.endTime || '—' }}</td>
                            <td>
                                <span class="status-pill minimal" :class="statusClass(s.status)">
                                    <span class="pill-dot"></span>
                                    {{ statusLabel(s.status) }}
                                </span>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { authState } from '../stores/auth'
import { getWorkSchedules } from '../services/attendanceApi'

const schedules = ref([])
const loading = ref(true)
const error = ref(null)
const filterMonth = ref('')

const now = new Date()
filterMonth.value = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`

function formatDate(d) {
    if (!d) return '—'
    const dt = new Date(d)
    return dt.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function statusClass(s) {
    if (s === 'Scheduled' || s === 'Worked') return 'active'
    if (s === 'Leave') return 'warning'
    if (s === 'Cancelled') return 'inactive'
    return ''
}

function statusLabel(s) {
    const map = { Scheduled: 'Đã lên lịch', Worked: 'Đã làm', Leave: 'Nghỉ', Cancelled: 'Đã hủy', Absent: 'Vắng' }
    return map[s] || s || '—'
}

async function fetchSchedules() {
    loading.value = true
    error.value = null
    try {
        const employeeId = authState.user?.employeeId
        if (!employeeId) { schedules.value = []; return }
        const params = { employeeId }
        if (filterMonth.value) {
            const [y, m] = filterMonth.value.split('-').map(Number)
            params.fromDate = new Date(y, m - 1, 1).toISOString()
            params.toDate = new Date(y, m, 0).toISOString()
        }
        const res = await getWorkSchedules(params)
        schedules.value = res.data || []
    } catch (e) {
        error.value = 'Không thể tải lịch làm việc.'
    } finally {
        loading.value = false
    }
}

watch(filterMonth, fetchSchedules)
onMounted(fetchSchedules)
</script>
