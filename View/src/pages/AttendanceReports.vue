<template>
    <div class="page-container animate-in">
        <header class="page-header">
            <div>
                <h1 class="page-title">Báo cáo công</h1>
                <p class="page-subtitle">Thống kê chấm công theo ngày, theo tháng và theo xu hướng đi trễ/tăng ca.</p>
            </div>
            <button class="btn btn-secondary" @click="reloadAll">Làm mới</button>
        </header>

        <section class="stats-grid">
            <article class="stat-card blue">
                <div class="stat-icon blue">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M16 4h2a2 2 0 012 2v14a2 2 0 01-2 2H6a2 2 0 01-2-2V6a2 2 0 012-2h2"/><rect x="8" y="2" width="8" height="4" rx="1.5"/></svg>
                </div>
                <div class="stat-info">
                    <h3>{{ daily.scheduledEmployees || 0 }}</h3>
                    <p>Nhân viên có lịch hôm nay</p>
                </div>
            </article>
            <article class="stat-card orange">
                <div class="stat-icon orange">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>
                </div>
                <div class="stat-info">
                    <h3>{{ daily.checkedInEmployees || 0 }}</h3>
                    <p>Đã check-in hôm nay</p>
                </div>
            </article>
            <article class="stat-card red">
                <div class="stat-icon red">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 8v4"/><circle cx="12" cy="12" r="9"/><path d="M12 16h.01"/></svg>
                </div>
                <div class="stat-info">
                    <h3>{{ daily.notCheckedInEmployees || 0 }}</h3>
                    <p>Chưa check-in hôm nay</p>
                </div>
            </article>
            <article class="stat-card purple">
                <div class="stat-icon purple">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M8 6h13"/><path d="M8 12h13"/><path d="M8 18h13"/><path d="M3 6h.01"/><path d="M3 12h.01"/><path d="M3 18h.01"/></svg>
                </div>
                <div class="stat-info">
                    <h3>{{ daily.lateEmployees || 0 }}</h3>
                    <p>Đi trễ hôm nay</p>
                </div>
            </article>
            <article class="stat-card green">
                <div class="stat-icon green">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 12h18"/><path d="M12 3v18"/></svg>
                </div>
                <div class="stat-info">
                    <h3>{{ Number(daily.totalOvertimeHours || 0).toFixed(2) }}h</h3>
                    <p>Tổng giờ tăng ca hôm nay</p>
                </div>
            </article>
            <article class="stat-card cyan">
                <div class="stat-icon cyan">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 2l9 4v6c0 5-3.5 8.5-9 10-5.5-1.5-9-5-9-10V6l9-4z"/></svg>
                </div>
                <div class="stat-info">
                    <h3>{{ daily.pendingLeaveRequests || 0 }}</h3>
                    <p>Đơn nghỉ chờ duyệt</p>
                </div>
            </article>
        </section>

        <section class="card panel">
            <div class="toolbar-shell">
                <div class="toolbar-filters">
                    <label>
                        Tháng
                        <input v-model.number="filters.month" type="number" min="1" max="12" class="mini-input" />
                    </label>
                    <label>
                        Năm
                        <input v-model.number="filters.year" type="number" min="2020" max="2100" class="mini-input" />
                    </label>
                </div>
                <button class="btn btn-primary btn-sm" @click="loadMonthly">Xem báo cáo tháng</button>
            </div>

            <div v-if="loading" class="empty-card">Đang tải báo cáo...</div>
            <div v-else-if="error" class="empty-card">{{ error }}</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nhân viên</th>
                            <th>Phòng ban</th>
                            <th>Ngày công</th>
                            <th>Nghỉ phép</th>
                            <th>Vắng mặt</th>
                            <th>Đi trễ</th>
                            <th>Về sớm</th>
                            <th>Tổng giờ</th>
                            <th>Tăng ca</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in monthlyItems" :key="item.employeeId">
                            <td>{{ item.employeeName }}</td>
                            <td>{{ item.departmentName || '--' }}</td>
                            <td>{{ item.workDays }}</td>
                            <td>{{ item.leaveDays }}</td>
                            <td>{{ item.absentDays }}</td>
                            <td>{{ item.lateCount }}</td>
                            <td>{{ item.earlyLeaveCount }}</td>
                            <td>{{ Number(item.totalWorkingHours || 0).toFixed(2) }}h</td>
                            <td>{{ Number(item.overtimeHours || 0).toFixed(2) }}h</td>
                        </tr>
                        <tr v-if="monthlyItems.length === 0">
                            <td colspan="9">Không có dữ liệu tháng.</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>
    </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { getAttendanceDailyReport, getAttendanceMonthlyReport } from '../services/attendanceApi'

const loading = ref(false)
const error = ref('')
const daily = ref({})
const monthlyItems = ref([])

const now = new Date()
const filters = reactive({
    month: now.getMonth() + 1,
    year: now.getFullYear(),
})

const loadDaily = async () => {
    const { data } = await getAttendanceDailyReport()
    daily.value = data || {}
}

const loadMonthly = async () => {
    loading.value = true
    error.value = ''
    try {
        const { data } = await getAttendanceMonthlyReport({
            month: filters.month,
            year: filters.year,
        })
        monthlyItems.value = data?.items || []
    } catch (err) {
        error.value = err?.response?.data?.message || 'Không tải được báo cáo tháng.'
    } finally {
        loading.value = false
    }
}

const reloadAll = async () => {
    loading.value = true
    error.value = ''
    try {
        await Promise.all([loadDaily(), loadMonthly()])
    } catch (err) {
        error.value = err?.response?.data?.message || 'Không tải được báo cáo công.'
    } finally {
        loading.value = false
    }
}

onMounted(reloadAll)
</script>

<style scoped>
.panel {
    padding: 22px;
}

.mini-input {
    min-height: 40px;
    width: 90px;
    padding: 0 10px;
    margin-left: 8px;
    border-radius: 10px;
    border: 1px solid var(--border-color);
    background: var(--bg-input);
}
</style>

