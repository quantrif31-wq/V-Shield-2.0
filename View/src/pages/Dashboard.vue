<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Dashboard overview</span>
                <h1 class="page-title">Bảng điều phối tổng quan</h1>
            </div>
            <div class="header-actions">
                <router-link to="/monitoring" class="btn btn-primary">Mở giám sát trực tiếp</router-link>
                <router-link to="/access-logs" class="btn btn-secondary">Tra cứu vào/ra</router-link>
                <router-link to="/pre-registrations" class="btn btn-secondary">Xem khách hẹn trước</router-link>
            </div>
        </div>

        <section class="metric-grid">
            <article class="metric-tile">
                <span class="metric-label">Xe đang trong bãi</span>
                <strong class="metric-value">{{ snapshot.vehiclesInside }}</strong>
                <span class="metric-note">Đọc trực tiếp từ `ParkingStatus = IN`.</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Khách dự kiến hôm nay</span>
                <strong class="metric-value">{{ snapshot.expectedVisitorsToday }}</strong>
                <span class="metric-note">Theo `ExpectedTimeIn` trong lịch hẹn trước.</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Lượt chờ duyệt</span>
                <strong class="metric-value">{{ snapshot.pendingRegistrations }}</strong>
                <span class="metric-note">Đơn khách vẫn đang ở trạng thái chờ xử lý.</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Ngoại lệ trong ngày</span>
                <strong class="metric-value">{{ snapshot.dailyExceptions }}</strong>
                <span class="metric-note">Gồm bypass, lỗi nhận diện hoặc trạng thái bất thường.</span>
            </article>
        </section>

        <section class="metric-grid">
            <article class="metric-tile">
                <span class="metric-label">Nhan vien dang lam hom nay</span>
                <strong class="metric-value">{{ snapshot.employeesWorkingToday || 0 }}</strong>
                <span class="metric-note">So nhan su co lich lam hom nay.</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Nhan vien chua check-in</span>
                <strong class="metric-value">{{ snapshot.employeesNotCheckedIn || 0 }}</strong>
                <span class="metric-note">So nhan su chua cham cong dau ca.</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Nhan vien di tre hom nay</span>
                <strong class="metric-value">{{ snapshot.employeesLateToday || 0 }}</strong>
                <span class="metric-note">So nhan su co phat sinh di tre.</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Don nghi cho duyet</span>
                <strong class="metric-value">{{ snapshot.pendingLeaveApprovals || 0 }}</strong>
                <span class="metric-note">Don xin nghi dang cho quan ly/Admin xu ly.</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Tong ca lam hom nay</span>
                <strong class="metric-value">{{ snapshot.totalShiftsToday || 0 }}</strong>
                <span class="metric-note">Tong lich ca duoc phan cong trong ngay.</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Tong gio tang ca hom nay</span>
                <strong class="metric-value">{{ Number(snapshot.totalOvertimeHoursToday || 0).toFixed(2) }}h</strong>
                <span class="metric-note">Gio lam ngoai ca da ghi nhan trong ngay.</span>
            </article>
        </section>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Traffic</span>
                        <h2 class="panel-title">Lưu lượng ra vào trong tuần</h2>
                    </div>
                    <div class="chip-row">
                        <span class="soft-chip">Vào</span>
                        <span class="soft-chip warn">Ra</span>
                    </div>
                </div>

                <div v-if="trafficChart.length" class="traffic-chart">
                    <div v-for="day in trafficChart" :key="day.label" class="chart-day">
                        <div class="chart-stack">
                            <div class="chart-bar in" :style="{ height: `${day.inPercent}%` }">
                                <span>{{ day.checkIn }}</span>
                            </div>
                            <div class="chart-bar out" :style="{ height: `${day.outPercent}%` }">
                                <span>{{ day.checkOut }}</span>
                            </div>
                        </div>
                        <strong>{{ day.label }}</strong>
                    </div>
                </div>
                <div v-else class="empty-card">Chưa có dữ liệu lưu lượng trong tuần này.</div>
            </article>

            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Live feed</span>
                        <h2 class="panel-title">Hoạt động mới nhất</h2>
                    </div>
                    <router-link to="/access-logs" class="btn btn-secondary btn-sm">Xem toàn bộ</router-link>
                </div>

                <div v-if="recentActivities.length" class="surface-list scrollable-panel">
                    <article v-for="activity in displayedActivities" :key="activity.logId" class="activity-item">
                        <div class="activity-dot" :class="activity.direction === 'IN' ? 'success' : 'warn'"></div>
                        <div class="activity-meta">
                            <strong>{{ formatTime(activity.timestamp) }}</strong>
                            <span>{{ activity.direction === 'IN' ? 'Vào' : 'Ra' }}</span>
                        </div>
                        <div class="activity-copy">
                            <strong>{{ activity.actorName }}</strong>
                            <p>
                                {{ activity.gateName }}
                                <template v-if="activity.capturedLicensePlate">- {{ activity.capturedLicensePlate }}</template>
                                <template v-else-if="activity.cameraName">- {{ activity.cameraName }}</template>
                            </p>
                            <div class="chip-row">
                                <span v-if="activity.isBypass" class="soft-chip danger">Bypass</span>
                                <span v-if="activity.exceptionReason" class="soft-chip warn">{{ activity.exceptionReason }}</span>
                                <span v-if="activity.resultStatus" class="soft-chip">{{ activity.resultStatus }}</span>
                            </div>
                        </div>
                    </article>
                </div>
                <div v-else class="empty-card">Chưa có bản ghi hoạt động nào để hiển thị.</div>
            </article>
        </section>

        <section v-if="intelligence" class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">AI Intelligence</span>
                        <h2 class="panel-title">Tổng quan thông minh</h2>
                    </div>
                    <span v-if="intelligenceLoading" class="soft-chip">Đang phân tích...</span>
                </div>
                <p class="intel-summary">{{ intelligence.summary }}</p>
                <div v-if="intelligence.insights && intelligence.insights.length" class="intel-insights">
                    <div v-for="insight in intelligence.insights" :key="insight.title"
                        class="intel-insight" :class="insight.type">
                        <span class="insight-icon">{{ insight.type === 'critical' ? '⚠️' : insight.type === 'warning' ? '⚡' : '💡' }}</span>
                        <div>
                            <strong>{{ insight.title }}</strong>
                            <p>{{ insight.detail }}</p>
                            <span class="insight-severity" :class="insight.severity">{{ insight.severity }}</span>
                        </div>
                    </div>
                </div>
            </article>

            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Trends</span>
                        <h2 class="panel-title">Dự báo tuần tới</h2>
                    </div>
                </div>
                <div v-if="intelligence.trends && intelligence.trends.length" class="trend-chart">
                    <div v-for="day in intelligence.trends" :key="day.label" class="trend-day">
                        <strong class="trend-label">{{ day.label }}</strong>
                        <div class="trend-stack">
                            <div class="trend-bar in" :style="{ height: getTrendPercent(day.predictedCheckIn, 'in') + '%' }">
                                <span>{{ day.predictedCheckIn }}</span>
                            </div>
                            <div class="trend-bar out" :style="{ height: getTrendPercent(day.predictedCheckOut, 'out') + '%' }">
                                <span>{{ day.predictedCheckOut }}</span>
                            </div>
                        </div>
                        <span class="trend-headcount">~{{ day.predictedHeadcount }} NV</span>
                        <span class="trend-conf">{{ day.confidence }}</span>
                    </div>
                </div>
                <div v-if="intelligence.comparison" class="trend-compare">
                    <div class="compare-item">
                        <span>So với hôm qua</span>
                        <strong :class="intelligence.comparison.attendanceVsYesterday > 0 ? 'up' : 'down'">
                            {{ intelligence.comparison.attendanceVsYesterday > 0 ? '+' : '' }}{{ intelligence.comparison.attendanceVsYesterday }}%
                        </strong>
                    </div>
                    <div class="compare-item">
                        <span>Dự báo tuần tới</span>
                        <strong :class="intelligence.comparison.trafficDirection === 'tang' ? 'up' : 'down'">
                            {{ intelligence.comparison.trafficDirection === 'tang' ? '+' : '-' }}{{ intelligence.comparison.trafficVsLastWeek }}%
                        </strong>
                    </div>
                </div>
            </article>
        </section>

        <section class="ops-grid three">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Visitors</span>
                        <h2 class="panel-title">Khách & hồ sơ</h2>
                    </div>
                </div>
                <div class="surface-list">
                    <div class="surface-item">
                        <div class="inline-stat">
                            <strong>{{ snapshot.guestProfiles }}</strong>
                            <span>Hồ sơ khách đang được lưu trong hệ thống</span>
                        </div>
                    </div>
                    <div class="surface-item">
                        <div class="inline-stat">
                            <strong>{{ snapshot.expectedVisitorsToday }}</strong>
                            <span>Khách dự kiến đến trong ngày hôm nay</span>
                        </div>
                    </div>
                </div>
            </article>

            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Devices</span>
                        <h2 class="panel-title">Camera & cổng</h2>
                    </div>
                </div>
                <div class="surface-list">
                    <div class="surface-item">
                        <div class="inline-stat">
                            <strong>{{ snapshot.camerasConfigured }}</strong>
                            <span>Camera đã cấu hình trong cơ sở dữ liệu</span>
                        </div>
                    </div>
                    <div class="surface-item">
                        <div class="inline-stat">
                            <strong>{{ snapshot.gatesConfigured }}</strong>
                            <span>Cổng truy cập đang được quản lý</span>
                        </div>
                    </div>
                </div>
            </article>

            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Biometrics</span>
                        <h2 class="panel-title">Độ phủ dữ liệu AI</h2>
                    </div>
                </div>
                <div class="surface-list">
                    <div class="surface-item">
                        <div class="inline-stat">
                            <strong>{{ snapshot.trainedEmployeeCount }}/{{ snapshot.employeeCount }}</strong>
                            <span>Nhân sự đã có model khuôn mặt</span>
                        </div>
                    </div>
                    <div class="surface-item">
                        <div class="inline-stat">
                            <strong>{{ snapshot.recognitionCoverage }}%</strong>
                            <span>Tỷ lệ nhân sự đã có dữ liệu nhận diện</span>
                        </div>
                    </div>
                </div>
            </article>
        </section>
    </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { getDashboardOverview, getDashboardIntelligence } from '../services/dashboardApi'

const maxActivities = 4

const isLoading = ref(true)
const loadError = ref('')
const snapshot = ref({
    vehiclesInside: 0,
    expectedVisitorsToday: 0,
    pendingRegistrations: 0,
    dailyCheckIn: 0,
    dailyCheckOut: 0,
    dailyExceptions: 0,
    camerasConfigured: 0,
    gatesConfigured: 0,
    guestProfiles: 0,
    employeeCount: 0,
    trainedEmployeeCount: 0,
    recognitionCoverage: 0,
    employeesWorkingToday: 0,
    employeesNotCheckedIn: 0,
    employeesLateToday: 0,
    pendingLeaveApprovals: 0,
    totalShiftsToday: 0,
    totalOvertimeHoursToday: 0,
})
const weeklyTraffic = ref([])
const recentActivities = ref([])
const intelligence = ref(null)
const intelligenceLoading = ref(false)

const displayedActivities = computed(() => recentActivities.value.slice(0, maxActivities))

const nowLabel = computed(() =>
    new Date().toLocaleDateString('vi-VN', {
        weekday: 'long',
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
    })
)

const trafficChart = computed(() => {
    const maxValue = Math.max(
        ...weeklyTraffic.value.flatMap((day) => [day.checkIn || 0, day.checkOut || 0]),
        1
    )

    return weeklyTraffic.value.map((day) => ({
        ...day,
        inPercent: Math.max(12, Math.round((day.checkIn / maxValue) * 100)),
        outPercent: Math.max(12, Math.round((day.checkOut / maxValue) * 100)),
    }))
})

const spotlightMessage = computed(() => {
    if (snapshot.value.dailyExceptions > 0) {
        return `Có ${snapshot.value.dailyExceptions} ngoại lệ trong ngày, nên ưu tiên rà soát mục Xử lý ngoại lệ và nhật ký ra vào.`
    }

    if (snapshot.value.pendingRegistrations > 0) {
        return `Hiện còn ${snapshot.value.pendingRegistrations} lượt đăng ký khách chờ duyệt, phù hợp để lễ tân xử lý sớm trước giờ cao điểm.`
    }

    return 'Luồng ra vào hôm nay đang ổn định. Có thể ưu tiên theo dõi camera, biển số và độ phủ dữ liệu nhận diện.'
})

const formatTime = (value) => {
    if (!value) return '--'
    return new Date(value).toLocaleTimeString('vi-VN', {
        hour: '2-digit',
        minute: '2-digit',
    })
}

const maxTrendIn = computed(() => Math.max(...(intelligence.value?.trends?.map(t => t.predictedCheckIn) || [1]), 1))
const maxTrendOut = computed(() => Math.max(...(intelligence.value?.trends?.map(t => t.predictedCheckOut) || [1]), 1))

const getTrendPercent = (value, type) => {
    const max = type === 'in' ? maxTrendIn.value : maxTrendOut.value
    return Math.max(12, Math.round((value / max) * 100))
}

const loadDashboard = async () => {
    isLoading.value = true
    loadError.value = ''
    try {
        const { data } = await getDashboardOverview()
        snapshot.value = { ...snapshot.value, ...(data.snapshot || {}) }
        weeklyTraffic.value = data.weeklyTraffic || []
        recentActivities.value = data.recentActivities || []
    } catch (error) {
        console.error('Dashboard load error:', error)
        loadError.value = 'Không thể tải dữ liệu tổng quan'
    } finally {
        isLoading.value = false
    }
}

const loadIntelligence = async () => {
    intelligenceLoading.value = true
    try {
        const { data } = await getDashboardIntelligence()
        intelligence.value = data
    } catch (error) {
        console.error('Intelligence load error:', error)
    } finally {
        intelligenceLoading.value = false
    }
}

onMounted(() => {
    loadDashboard()
    loadIntelligence()
})
</script>



<style scoped>
.ops-panel {
    display: flex;
    flex-direction: column;
}

.traffic-chart {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    gap: 12px;
    min-height: 250px;
    margin-top: auto;
}

.chart-day {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 14px;
}

.chart-stack {
    width: 100%;
    height: 280px;
    display: flex;
    align-items: flex-end;
    justify-content: center;
    gap: 8px;
}

.chart-bar {
    position: relative;
    width: min(24px, 100%);
    border-radius: 12px 12px 4px 4px;
}

.chart-bar span {
    position: absolute;
    left: 50%;
    top: -26px;
    transform: translateX(-50%);
    color: var(--text-muted);
    font-size: 0.72rem;
    font-weight: 700;
}

.chart-bar.in {
    background: linear-gradient(180deg, rgba(84, 196, 211, 0.24), var(--accent-primary));
}

.chart-bar.out {
    background: linear-gradient(180deg, rgba(216, 155, 55, 0.24), var(--accent-warning));
}

.chart-day strong {
    color: var(--text-secondary);
    font-size: 0.82rem;
}

.activity-item {
    display: grid;
    grid-template-columns: 12px 72px 1fr;
    gap: 14px;
    padding: 14px;
    border-radius: 20px;
    border: 1px solid rgba(24, 49, 77, 0.08);
    background: rgba(236, 244, 246, 0.72);
}

.activity-dot {
    width: 12px;
    height: 12px;
    margin-top: 4px;
    border-radius: 50%;
}

.activity-dot.success {
    background: var(--accent-success);
    box-shadow: 0 0 0 6px rgba(20, 134, 109, 0.1);
}

.activity-dot.warn {
    background: var(--accent-warning);
    box-shadow: 0 0 0 6px rgba(184, 111, 33, 0.1);
}

.activity-meta {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.activity-meta strong {
    color: var(--text-primary);
    font-size: 0.88rem;
}

.activity-meta span {
    color: var(--text-muted);
    font-size: 0.74rem;
    letter-spacing: 0.08em;
    text-transform: uppercase;
}

.activity-copy strong {
    color: var(--text-primary);
    font-size: 0.94rem;
}

.activity-copy p {
    margin-top: 6px;
    color: var(--text-secondary);
    font-size: 0.84rem;
}

.activity-copy .chip-row {
    margin-top: 10px;
}

.panel-head.compact {
    margin-bottom: 14px;
}

.scrollable-panel {
    max-height: 360px;
    overflow-y: auto;
    scrollbar-width: thin;
    scrollbar-color: rgba(24, 49, 77, 0.15) transparent;
}

.scrollable-panel::-webkit-scrollbar {
    width: 5px;
}

.scrollable-panel::-webkit-scrollbar-track {
    background: transparent;
}

.scrollable-panel::-webkit-scrollbar-thumb {
    background: rgba(24, 49, 77, 0.15);
    border-radius: 10px;
}




.intel-summary {
    font-size: 0.92rem;
    line-height: 1.7;
    color: var(--text-primary);
    padding: 12px 16px;
    background: linear-gradient(135deg, rgba(84, 196, 211, 0.06), rgba(84, 196, 211, 0.02));
    border-radius: 16px;
    border: 1px solid rgba(84, 196, 211, 0.12);
    margin-bottom: 16px;
}

.intel-insights {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.intel-insight {
    display: flex;
    gap: 12px;
    padding: 12px 14px;
    border-radius: 14px;
    border: 1px solid rgba(24, 49, 77, 0.08);
    background: rgba(236, 244, 246, 0.5);
}

.intel-insight.critical {
    border-color: rgba(200, 50, 50, 0.25);
    background: rgba(200, 50, 50, 0.04);
}

.intel-insight.warning {
    border-color: rgba(216, 155, 55, 0.25);
    background: rgba(216, 155, 55, 0.04);
}

.intel-insight.info {
    border-color: rgba(84, 196, 211, 0.2);
    background: rgba(84, 196, 211, 0.04);
}

.insight-icon {
    font-size: 1.2rem;
    line-height: 1.4;
}

.intel-insight div strong {
    display: block;
    font-size: 0.88rem;
    color: var(--text-primary);
    margin-bottom: 4px;
}

.intel-insight div p {
    font-size: 0.8rem;
    color: var(--text-secondary);
    line-height: 1.5;
    margin: 0 0 6px 0;
}

.insight-severity {
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    padding: 2px 8px;
    border-radius: 20px;
    background: rgba(24, 49, 77, 0.06);
    color: var(--text-muted);
}

.insight-severity.cao {
    background: rgba(200, 50, 50, 0.1);
    color: #c83232;
}

.insight-severity.trung-binh {
    background: rgba(216, 155, 55, 0.1);
    color: #b86f21;
}

.trend-chart {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    gap: 8px;
    min-height: 200px;
    margin-top: auto;
}

.trend-day {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
}

.trend-label {
    font-size: 0.78rem;
    color: var(--text-secondary);
}

.trend-stack {
    width: 100%;
    height: 200px;
    display: flex;
    align-items: flex-end;
    justify-content: center;
    gap: 4px;
}

.trend-bar {
    position: relative;
    width: min(18px, 100%);
    border-radius: 8px 8px 2px 2px;
    transition: height 0.4s ease;
}

.trend-bar span {
    position: absolute;
    left: 50%;
    top: -22px;
    transform: translateX(-50%);
    color: var(--text-muted);
    font-size: 0.65rem;
    font-weight: 700;
}

.trend-bar.in {
    background: linear-gradient(180deg, rgba(84, 196, 211, 0.2), var(--accent-primary));
}

.trend-bar.out {
    background: linear-gradient(180deg, rgba(216, 155, 55, 0.2), var(--accent-warning));
}

.trend-headcount {
    font-size: 0.7rem;
    color: var(--text-muted);
}

.trend-conf {
    font-size: 0.65rem;
    color: var(--text-muted);
    opacity: 0.6;
}

.trend-compare {
    display: flex;
    gap: 16px;
    margin-top: 16px;
    padding-top: 14px;
    border-top: 1px solid rgba(24, 49, 77, 0.06);
}

.compare-item {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.compare-item span {
    font-size: 0.76rem;
    color: var(--text-secondary);
}

.compare-item strong {
    font-size: 1.2rem;
}

.compare-item strong.up { color: var(--accent-success); }
.compare-item strong.down { color: var(--accent-warning); }

@media (max-width: 768px) {
    .activity-item {
        grid-template-columns: 12px 1fr;
    }

    .activity-meta {
        grid-column: 2;
        flex-direction: row;
        gap: 8px;
        align-items: center;
    }

    .activity-copy {
        grid-column: 2;
    }
}
</style>
