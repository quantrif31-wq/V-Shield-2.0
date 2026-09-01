<template>
    <div class="page-container report-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Báo cáo thống kê</span>
                <h1 class="page-title">Tổng quan an ninh &amp; ra vào</h1>
                <p class="page-subtitle dashboard-subtitle">
                    Nhìn nhanh hoạt động 30 ngày gần nhất: lưu lượng ra vào, chấm công, khách thăm, cảnh báo
                    và sức khỏe thiết bị. Bấm vào từng chỉ số để xem chi tiết.
                </p>
            </div>
            <div class="header-actions">
                <span class="updated-at">Cập nhật {{ generatedAtLabel }}</span>
                <button type="button" class="btn btn-secondary" :disabled="loading" @click="loadAll">
                    Làm mới
                </button>
            </div>
        </div>

        <!-- KPI CARDS: trả lời nhanh "có ổn không?" -->
        <section class="kpi-grid">
            <article class="kpi-card" :class="kpiCardTone(kpis.todayAnomalies, 'warn')">
                <div class="kpi-icon">🚪</div>
                <div class="kpi-body">
                    <span class="kpi-label">Lượt ra/vào hôm nay</span>
                    <strong class="kpi-value">{{ kpis.todayTotal || 0 }}</strong>
                    <span class="kpi-note">
                        Vào {{ kpis.todayCheckIn || 0 }} · Ra {{ kpis.todayCheckOut || 0 }}
                    </span>
                </div>
            </article>

            <article class="kpi-card">
                <div class="kpi-icon">👥</div>
                <div class="kpi-body">
                    <span class="kpi-label">Khách đang có mặt</span>
                    <strong class="kpi-value">{{ kpis.checkedInVisitors || 0 }}</strong>
                    <span class="kpi-note">{{ kpis.todayVisitors || 0 }} lượt hôm nay</span>
                </div>
            </article>

            <article class="kpi-card" :class="kpiCardTone(kpis.todayAnomalies, 'danger')">
                <div class="kpi-icon">⚠️</div>
                <div class="kpi-body">
                    <span class="kpi-label">Bất thường hôm nay</span>
                    <strong class="kpi-value">{{ kpis.todayAnomalies || 0 }}</strong>
                    <span class="kpi-note">Bypass / từ chối / ngoại lệ</span>
                </div>
            </article>

            <article class="kpi-card" :class="kpiCardTone(kpis.criticalAlarms, 'danger')">
                <div class="kpi-icon">🚨</div>
                <div class="kpi-body">
                    <span class="kpi-label">Báo động đang mở</span>
                    <strong class="kpi-value">{{ kpis.openAlarms || 0 }}</strong>
                    <span class="kpi-note">{{ kpis.criticalAlarms || 0 }} nghiêm trọng</span>
                </div>
            </article>

            <article class="kpi-card" :class="kpiCardTone(kpis.offlineDevices + kpis.degradedDevices, 'warn')">
                <div class="kpi-icon">📡</div>
                <div class="kpi-body">
                    <span class="kpi-label">Thiết bị cần chú ý</span>
                    <strong class="kpi-value">{{ (kpis.offlineDevices || 0) + (kpis.degradedDevices || 0) }}</strong>
                    <span class="kpi-note">{{ kpis.offlineDevices || 0 }} mất kết nối · {{ kpis.degradedDevices || 0 }} suy giảm</span>
                </div>
            </article>
        </section>

        <!-- BIỂU ĐỒ CHÍNH: xu hướng 30 ngày -->
        <section class="panel-main">
            <div class="panel-head">
                <div>
                    <span class="panel-kicker">Xu hướng 30 ngày</span>
                    <h2 class="panel-title">Lượt ra vào theo ngày</h2>
                </div>
                <div class="legend">
                    <span class="legend-item"><i class="dot in"></i>Vào</span>
                    <span class="legend-item"><i class="dot out"></i>Ra</span>
                    <span class="legend-item"><i class="dot total"></i>Tổng</span>
                </div>
            </div>

            <div v-if="trafficByDay.length" class="line-chart">
                <svg :viewBox="lineViewBox" class="line-svg" preserveAspectRatio="none">
                    <defs>
                        <linearGradient id="areaGrad" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="0%" stop-color="#0f7c82" stop-opacity="0.25" />
                            <stop offset="100%" stop-color="#0f7c82" stop-opacity="0.02" />
                        </linearGradient>
                    </defs>

                    <template v-for="line in lineGrid" :key="'g' + line">
                        <line :x1="0" :x2="chartW" :y1="line" :y2="line" class="grid-line" />
                    </template>

                    <path :d="areaInPath" fill="url(#areaGrad)" />

                    <path :d="lineInPath" class="line-path in" />
                    <path :d="lineOutPath" class="line-path out" />

                    <template v-for="(point, i) in lineInPoints" :key="'pin' + i">
                        <circle :cx="point.x" :cy="point.y" r="2.6" class="point-dot in" />
                    </template>
                    <template v-for="(point, i) in lineOutPoints" :key="'pout' + i">
                        <circle :cx="point.x" :cy="point.y" r="2.6" class="point-dot out" />
                    </template>
                </svg>

                <div class="axis-x">
                    <span v-for="tick in xTicks" :key="tick.label">{{ tick.label }}</span>
                </div>
            </div>
            <div v-else class="empty-card">Chưa có dữ liệu ra vào trong 30 ngày qua.</div>
        </section>

        <!-- HÀNG GIỮA: so sánh theo cổng + tỷ trọng -->
        <section class="mid-grid">
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">So sánh theo cổng</span>
                        <h2 class="panel-title">Ra/vào theo cổng (30 ngày)</h2>
                    </div>
                </div>

                <div v-if="trafficByGate.length" class="gate-bars">
                    <div v-for="gate in trafficByGate" :key="gate.gate" class="gate-row">
                        <span class="gate-name" :title="gate.gate">{{ gate.gate }}</span>
                        <div class="gate-track">
                            <div class="gate-stack">
                                <div class="gate-bar in" :style="{ width: barPercent(gate.checkIn, maxGate) + '%' }"></div>
                                <div class="gate-bar out" :style="{ width: barPercent(gate.checkOut, maxGate) + '%' }"></div>
                            </div>
                        </div>
                        <span class="gate-total">{{ gate.total }}</span>
                    </div>
                    <div class="legend">
                        <span class="legend-item"><i class="dot in"></i>Vào</span>
                        <span class="legend-item"><i class="dot out"></i>Ra</span>
                    </div>
                </div>
                <div v-else class="empty-card">Chưa có dữ liệu theo cổng.</div>
            </article>

            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Tỷ trọng</span>
                        <h2 class="panel-title">Trạng thái chấm công (30 ngày)</h2>
                    </div>
                </div>

                <div v-if="attendanceStatus.length" class="donut-wrap">
                    <div class="donut-holder">
                        <svg viewBox="0 0 42 42" class="donut-svg">
                            <circle cx="21" cy="21" r="15.9" fill="none" class="donut-ring" />
                            <template v-for="(seg, i) in attendanceSegments" :key="'seg' + i">
                                <circle
                                    cx="21" cy="21" r="15.9" fill="none"
                                    class="donut-seg"
                                    :stroke="seg.color"
                                    :stroke-dasharray="`${seg.length} ${100 - seg.length}`"
                                    :stroke-dashoffset="seg.offset"
                                />
                            </template>
                        </svg>
                        <div class="donut-center">
                            <strong>{{ attendanceTotal }}</strong>
                            <span>bản ghi</span>
                        </div>
                    </div>
                    <ul class="donut-legend">
                        <li v-for="item in attendanceStatus" :key="item.status">
                            <i class="swatch" :style="{ background: statusColor(item.status) }"></i>
                            <span class="legend-label">{{ statusLabel(item.status) }}</span>
                            <span class="legend-count">{{ item.count }}</span>
                        </li>
                    </ul>
                </div>
                <div v-else class="empty-card">Chưa có dữ liệu chấm công.</div>
            </article>
        </section>

        <!-- HÀNG DƯỚI: heatmap theo giờ + khách thăm + bảng bất thường -->
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Phân bố theo giờ</span>
                        <h2 class="panel-title">Lượt ra/vào theo giờ trong ngày</h2>
                    </div>
                </div>

                <div v-if="hourlyByWeekday.length" class="heatmap">
                    <div class="heatmap-row heatmap-header">
                        <span class="heatmap-corner">Giờ</span>
                        <span v-for="hour in hourLabels" :key="'h' + hour" class="heatmap-hour">{{ hour }}</span>
                    </div>
                    <div v-for="row in hourlyByWeekday" :key="row.day" class="heatmap-row">
                        <span class="heatmap-corner">{{ row.day }}</span>
                        <div
                            v-for="cell in row.hours"
                            :key="row.day + cell.hour"
                            class="heatmap-cell"
                            :class="heatClass(cell.checkIn + cell.checkOut, maxHourly)"
                            :title="`${row.day} ${cell.hour}:00 — Vào ${cell.checkIn} · Ra ${cell.checkOut}`"
                        ></div>
                    </div>
                </div>
                <div v-else class="empty-card">Chưa có dữ liệu theo giờ.</div>
            </article>

            <article class="ops-panel visitor-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Khách thăm</span>
                        <h2 class="panel-title">Lượt khách theo ngày (30 ngày)</h2>
                    </div>
                    <div v-if="visitorTrend.length" class="panel-stats">
                        <div class="stat-pill" title="Tổng lượt khách trong 30 ngày qua">
                            <span class="stat-pill-label">Tổng:</span>
                            <strong class="stat-pill-value">{{ totalVisitors30d }}</strong>
                        </div>
                        <div class="stat-pill" title="Trung bình mỗi ngày">
                            <span class="stat-pill-label">TB:</span>
                            <strong class="stat-pill-value">{{ avgVisitorsDaily }}/ngày</strong>
                        </div>
                        <div class="stat-pill" title="Ngày có lượt khách cao nhất">
                            <span class="stat-pill-label">Cao nhất:</span>
                            <strong class="stat-pill-value">{{ peakVisitors }}</strong>
                        </div>
                    </div>
                </div>

                <div v-if="visitorTrend.length" class="visitor-chart-wrap" @mouseleave="hoveredVisitorIndex = null">
                    <div class="visitor-svg-container">
                        <svg :viewBox="`0 0 ${vChartW} ${vChartH}`" class="visitor-svg" preserveAspectRatio="none">
                            <defs>
                                <linearGradient id="visitorBarGrad" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="0%" stop-color="#54c4d3" />
                                    <stop offset="100%" stop-color="#0f7c82" />
                                </linearGradient>
                                <linearGradient id="visitorBarHoverGrad" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="0%" stop-color="#7ee4f0" />
                                    <stop offset="100%" stop-color="#0b5961" />
                                </linearGradient>
                            </defs>

                            <!-- Đường lưới ngang & nhãn trục Y -->
                            <template v-for="grid in visitorYGrid" :key="'vg' + grid.y">
                                <line :x1="vPadL" :x2="vChartW - vPadR" :y1="grid.y" :y2="grid.y" class="visitor-grid-line" />
                                <text :x="vPadL - 8" :y="grid.y + 3.5" class="visitor-axis-y-text" text-anchor="end">{{ grid.val }}</text>
                            </template>

                            <!-- Cột biểu đồ và vùng hover -->
                            <g v-for="(bar, i) in visitorBars" :key="'vbar' + i">
                                <!-- Background slot hover trigger -->
                                <rect
                                    :x="bar.slotX"
                                    :y="vPadT"
                                    :width="bar.slotW"
                                    :height="vPlotH"
                                    class="visitor-slot-rect"
                                    :class="{ active: hoveredVisitorIndex === i }"
                                    @mouseenter="hoveredVisitorIndex = i"
                                />

                                <!-- Thanh nền mờ -->
                                <rect
                                    :x="bar.x"
                                    :y="vPadT"
                                    :width="bar.barW"
                                    :height="vPlotH"
                                    rx="3"
                                    ry="3"
                                    class="visitor-bar-bg"
                                />

                                <!-- Cột dữ liệu thực tế -->
                                <rect
                                    :x="bar.x"
                                    :y="bar.y"
                                    :width="bar.barW"
                                    :height="bar.barH"
                                    rx="3"
                                    ry="3"
                                    class="visitor-bar-fill"
                                    :class="{ active: hoveredVisitorIndex === i }"
                                    :fill="hoveredVisitorIndex === i ? 'url(#visitorBarHoverGrad)' : 'url(#visitorBarGrad)'"
                                    @mouseenter="hoveredVisitorIndex = i"
                                />
                            </g>

                            <!-- Nhãn ngày trục X (hiển thị phân bố cách đều, tránh dính chữ) -->
                            <template v-for="tick in visitorXTicks" :key="'vtick' + tick.label + tick.x">
                                <text :x="tick.x" :y="vChartH - 6" class="visitor-axis-x-text" text-anchor="middle">
                                    {{ tick.label }}
                                </text>
                            </template>
                        </svg>

                        <!-- Tooltip tương tác hiển thị thông tin chi tiết ngày -->
                        <transition name="fade-fast">
                            <div
                                v-if="hoveredVisitor"
                                class="visitor-tooltip"
                                :style="tooltipPositionStyle"
                            >
                                <div class="tooltip-date">Ngày {{ hoveredVisitor.label }}</div>
                                <div class="tooltip-val">
                                    <strong>{{ hoveredVisitor.total }}</strong> lượt khách
                                </div>
                            </div>
                        </transition>
                    </div>
                </div>
                <div v-else class="empty-card">Chưa có dữ liệu khách thăm.</div>

                <div v-if="visitorStatus.length" class="visitor-status-cards">
                    <div
                        v-for="s in visitorStatus"
                        :key="s.status"
                        class="visitor-status-pill"
                        :class="'status-' + (s.status || '').toLowerCase()"
                    >
                        <span class="status-dot"></span>
                        <span class="status-name">{{ visitorStatusLabel(s.status) }}:</span>
                        <span class="status-val">{{ s.count }}</span>
                    </div>
                </div>
            </article>
        </section>

        <!-- Bảng bất thường gần đây: drill-down -->
        <section class="ops-panel">
            <div class="panel-head">
                <div>
                    <span class="panel-kicker">Bất thường gần đây</span>
                    <h2 class="panel-title">Sự kiện cần xem xét hôm nay</h2>
                </div>
                <router-link to="/access-logs" class="btn btn-secondary btn-sm">Xem nhật ký đầy đủ</router-link>
            </div>

            <div v-if="anomalies.length" class="anomaly-table">
                <div class="anomaly-row anomaly-head">
                    <span>Thời gian</span>
                    <span>Cổng</span>
                    <span>Chiều</span>
                    <span>Lý do</span>
                </div>
                <div v-for="(item, idx) in anomalies" :key="idx" class="anomaly-row">
                    <span>{{ formatTime(item.time) }}</span>
                    <span>{{ item.gate || '—' }}</span>
                    <span>
                        <span class="badge" :class="item.direction === 'IN' ? 'in-badge' : 'out-badge'">
                            {{ item.direction === 'IN' ? 'Vào' : 'Ra' }}
                        </span>
                    </span>
                    <span class="anomaly-note">{{ item.note }}</span>
                </div>
            </div>
            <div v-else class="empty-card">Không có bất thường nào hôm nay. Hoạt động diễn ra bình thường.</div>
        </section>

        <div v-if="loadError" class="alert-danger-bar">{{ loadError }}</div>
    </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { getDashboardReports } from '../services/dashboardApi'
import { useRealtimeSync } from '../composables/useRealtimeSync'

const loading = ref(true)
const loadError = ref('')
const report = ref(null)

useRealtimeSync(['AccessLog', 'Alarm', 'Employee', 'Vehicle', 'Camera', 'Gate', 'Visit'], () => {
    loadAll()
})

const kpis = computed(() => report.value?.kpis || {})
const trafficByDay = computed(() => report.value?.trafficByDay || [])
const trafficByGate = computed(() => report.value?.trafficByGate || [])
const hourlyByWeekday = computed(() => report.value?.hourlyByWeekday || [])
const attendanceStatus = computed(() => report.value?.attendanceStatus || [])
const attendanceTrend = computed(() => report.value?.attendanceTrend || [])
const visitorTrend = computed(() => report.value?.visitorTrend || [])
const visitorStatus = computed(() => report.value?.visitorStatus || [])
const alarmBySeverity = computed(() => report.value?.alarmBySeverity || [])
const alarmByState = computed(() => report.value?.alarmByState || [])
const deviceByStatus = computed(() => report.value?.deviceByStatus || [])
const anomalies = computed(() => report.value?.anomalies || [])

const generatedAtLabel = computed(() => {
    if (!report.value?.generatedAt) return 'vừa xong'
    return new Date(report.value.generatedAt).toLocaleString('vi-VN', {
        hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit',
    })
})

// ---- Line chart geometry ----
const chartW = 960
const chartH = 240
const padL = 8
const padR = 8
const padT = 12
const padB = 8
const plotW = chartW - padL - padR
const plotH = chartH - padT - padB

const maxTraffic = computed(() => Math.max(1, ...trafficByDay.value.map(d => d.total || 0)))

const linePoints = computed(() => {
    const arr = trafficByDay.value
    if (!arr.length) return { inPts: [], outPts: [] }
    const step = arr.length > 1 ? plotW / (arr.length - 1) : 0
    const y = (v) => padT + plotH - (Math.min(v, maxTraffic.value) / maxTraffic.value) * plotH
    return {
        inPts: arr.map((d, i) => ({ x: padL + i * step, y: y(d.checkIn || 0) })),
        outPts: arr.map((d, i) => ({ x: padL + i * step, y: y(d.checkOut || 0) })),
    }
})

const lineInPoints = computed(() => linePoints.value.inPts)
const lineOutPoints = computed(() => linePoints.value.outPts)

function buildPath(pts) {
    if (!pts.length) return ''
    return pts.map((p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `L ${p.x} ${p.y}`)).join(' ')
}

const lineInPath = computed(() => buildPath(lineInPoints.value))
const lineOutPath = computed(() => buildPath(lineOutPoints.value))

const areaInPath = computed(() => {
    const pts = lineInPoints.value
    if (!pts.length) return ''
    const top = buildPath(pts)
    const last = pts[pts.length - 1]
    const first = pts[0]
    return `${top} L ${last.x} ${padT + plotH} L ${first.x} ${padT + plotH} Z`
})

const lineViewBox = computed(() => `0 0 ${chartW} ${chartH}`)

const lineGrid = computed(() => {
    const lines = []
    for (let i = 0; i <= 4; i++) {
        lines.push(padT + (plotH / 4) * i)
    }
    return lines
})

const xTicks = computed(() => {
    const arr = trafficByDay.value
    if (!arr.length) return []
    const step = Math.max(1, Math.floor(arr.length / 6))
    const ticks = []
    for (let i = 0; i < arr.length; i += step) {
        ticks.push({ label: arr[i].label })
    }
    if (arr.length && ticks[ticks.length - 1]?.label !== arr[arr.length - 1].label) {
        ticks.push({ label: arr[arr.length - 1].label })
    }
    return ticks
})

// ---- Bar helpers ----
const maxGate = computed(() => Math.max(1, ...trafficByGate.value.map(g => g.total || 0)))
const maxVisitors = computed(() => Math.max(1, ...visitorTrend.value.map(d => d.total || 0)))
const maxHourly = computed(() => {
    let m = 1
    for (const row of hourlyByWeekday.value) {
        for (const cell of row.hours) {
            m = Math.max(m, cell.checkIn + cell.checkOut)
        }
    }
    return m
})

function barPercent(v, max) {
    return Math.max(2, Math.round((v / max) * 100))
}

// ---- Visitor chart geometry & stats ----
const vChartW = 600
const vChartH = 180
const vPadL = 32
const vPadR = 16
const vPadT = 20
const vPadB = 24
const vPlotW = vChartW - vPadL - vPadR
const vPlotH = vChartH - vPadT - vPadB

const totalVisitors30d = computed(() => visitorTrend.value.reduce((s, d) => s + (d.total || 0), 0))
const avgVisitorsDaily = computed(() => {
    const len = visitorTrend.value.length
    if (!len) return '0'
    return (totalVisitors30d.value / len).toFixed(1)
})
const peakVisitors = computed(() => Math.max(0, ...visitorTrend.value.map(d => d.total || 0)))

const visitorBars = computed(() => {
    const arr = visitorTrend.value
    if (!arr.length) return []
    const count = arr.length
    const slotW = vPlotW / count
    const barW = Math.max(4, Math.min(16, slotW * 0.65))
    const maxVal = maxVisitors.value || 1

    return arr.map((d, i) => {
        const val = d.total || 0
        const barH = val > 0 ? Math.max(4, (val / maxVal) * vPlotH) : 2
        const slotX = vPadL + i * slotW
        const x = vPadL + i * slotW + (slotW - barW) / 2
        const y = vPadT + vPlotH - barH
        return {
            date: d.date,
            label: d.label,
            total: val,
            slotX,
            slotW,
            x,
            y,
            barW,
            barH,
        }
    })
})

const visitorYGrid = computed(() => {
    const maxVal = maxVisitors.value || 1
    const midVal = Math.round(maxVal / 2)
    return [
        { y: vPadT, val: maxVal },
        { y: vPadT + vPlotH / 2, val: midVal },
        { y: vPadT + vPlotH, val: 0 },
    ]
})

const visitorXTicks = computed(() => {
    const arr = visitorTrend.value
    if (!arr.length) return []
    const count = arr.length
    const slotW = vPlotW / count
    if (count <= 7) {
        return arr.map((d, i) => ({
            label: d.label,
            x: vPadL + i * slotW + slotW / 2,
        }))
    }
    const step = Math.max(1, Math.floor(count / 5))
    const ticks = []
    for (let i = 0; i < count; i += step) {
        ticks.push({
            label: arr[i].label,
            x: vPadL + i * slotW + slotW / 2,
        })
    }
    const lastIdx = count - 1
    const lastTick = ticks[ticks.length - 1]
    const lastX = vPadL + lastIdx * slotW + slotW / 2
    if (lastTick && lastX - lastTick.x > 35) {
        ticks.push({
            label: arr[lastIdx].label,
            x: lastX,
        })
    }
    return ticks
})

const hoveredVisitorIndex = ref(null)
const hoveredVisitor = computed(() => {
    if (hoveredVisitorIndex.value === null) return null
    return visitorBars.value[hoveredVisitorIndex.value] || null
})

const tooltipPositionStyle = computed(() => {
    if (!hoveredVisitor.value) return {}
    const percentX = Math.max(10, Math.min(90, (hoveredVisitor.value.x + hoveredVisitor.value.barW / 2) / vChartW * 100))
    const percentY = (hoveredVisitor.value.y) / vChartH * 100
    return {
        left: `${percentX}%`,
        bottom: `${Math.max(15, 100 - percentY + 12)}%`,
        transform: 'translateX(-50%)',
    }
})

// ---- Donut (SVG circle stroke-dasharray) ----
const DONUT_COLORS = {
    Completed: '#2f9e74',
    Late: '#d49b47',
    EarlyLeave: '#e8925a',
    Absent: '#d44747',
    CheckedIn: '#47a3d4',
    ForgotCheckout: '#8b5cf6',
    Leave: '#9aa5b1',
}

const attendanceTotal = computed(() => attendanceStatus.value.reduce((s, x) => s + x.count, 0))

const attendanceSegments = computed(() => {
    const total = attendanceTotal.value || 1
    let acc = 0
    return attendanceStatus.value.map((x) => {
        const length = Math.max(0.5, (x.count / total) * 100)
        const offset = 25 - acc
        acc += length
        return {
            length,
            offset,
            color: statusColor(x.status),
        }
    })
})

function statusColor(status) {
    return DONUT_COLORS[status] || '#94a3b8'
}

function statusLabel(status) {
    switch (status) {
        case 'Completed': return 'Đúng giờ'
        case 'Late': return 'Đi trễ'
        case 'EarlyLeave': return 'Về sớm'
        case 'Absent': return 'Vắng mặt'
        case 'CheckedIn': return 'Đã check-in'
        case 'ForgotCheckout': return 'Quên check-out'
        case 'Leave': return 'Nghỉ phép'
        default: return status || 'Không rõ'
    }
}

function visitorStatusLabel(status) {
    switch (status) {
        case 'Completed': return 'Đã xong'
        case 'CheckedIn': return 'Đang trong'
        case 'Overstay': return 'Quá giờ'
        case 'CheckedOut': return 'Đã ra'
        case 'Approved': return 'Đã duyệt'
        case 'Pending': return 'Chờ duyệt'
        default: return status || 'Khác'
    }
}

// ---- Heatmap ----
const hourLabels = ['0', '4', '8', '12', '16', '20']

function heatClass(value, max) {
    if (value <= 0) return 'lvl-0'
    const ratio = value / max
    if (ratio <= 0.2) return 'lvl-1'
    if (ratio <= 0.4) return 'lvl-2'
    if (ratio <= 0.6) return 'lvl-3'
    if (ratio <= 0.8) return 'lvl-4'
    return 'lvl-5'
}

// ---- KPI tone ----
function kpiCardTone(value, tone) {
    return value > 0 ? tone : ''
}

function formatTime(value) {
    if (!value) return '—'
    return new Date(value).toLocaleString('vi-VN', {
        hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit',
    })
}

async function loadAll() {
    loading.value = true
    loadError.value = ''
    try {
        const { data } = await getDashboardReports()
        report.value = data
    } catch (error) {
        console.error('Dashboard load error:', error)
        loadError.value = 'Không thể tải dữ liệu báo cáo thống kê. Vui lòng thử lại.'
    } finally {
        loading.value = false
    }
}

onMounted(loadAll)
</script>

<style scoped>
.report-page {
    display: flex;
    flex-direction: column;
    gap: 24px;
    padding-bottom: 32px;
}

.dashboard-subtitle {
    margin-top: 10px;
    max-width: 76ch;
    line-height: 1.55;
}

.updated-at {
    color: var(--text-muted);
    font-size: 0.84rem;
    white-space: nowrap;
}

.header-actions {
    display: flex;
    align-items: center;
    gap: 14px;
}

/* KPI cards */
.kpi-grid {
    display: grid;
    grid-template-columns: repeat(5, minmax(0, 1fr));
    gap: 18px;
}

.kpi-card {
    display: flex;
    gap: 16px;
    align-items: flex-start;
    padding: 20px 22px;
    border-radius: 22px;
    border: 1px solid var(--border-soft);
    background: var(--surface);
    box-shadow: 0 4px 18px rgba(0, 0, 0, 0.03);
    transition: transform 0.18s ease, box-shadow 0.18s ease, border-color 0.18s ease;
}

.kpi-card:hover {
    transform: translateY(-2px);
    box-shadow: var(--shadow-md);
}

.kpi-card.warn {
    border-color: rgba(216, 155, 55, 0.35);
    background: linear-gradient(180deg, rgba(216, 155, 55, 0.07), var(--surface));
}

.kpi-card.danger {
    border-color: rgba(195, 81, 70, 0.4);
    background: linear-gradient(180deg, rgba(195, 81, 70, 0.08), var(--surface));
}

.kpi-icon {
    font-size: 1.6rem;
    line-height: 1;
    padding-top: 2px;
}

.kpi-body {
    min-width: 0;
    flex: 1;
}

.kpi-label {
    display: block;
    color: var(--text-secondary);
    font-size: 0.82rem;
    font-weight: 600;
}

.kpi-value {
    display: block;
    margin-top: 8px;
    font-family: var(--font-heading);
    font-size: 2.1rem;
    line-height: 1;
    color: var(--text-primary);
}

.kpi-note {
    display: block;
    margin-top: 8px;
    color: var(--text-muted);
    font-size: 0.78rem;
    line-height: 1.5;
}

/* Panel main (line chart) */
.panel-main {
    border: 1px solid var(--border-soft);
    border-radius: 22px;
    background: var(--surface);
    padding: 24px 28px;
    box-shadow: 0 4px 18px rgba(0, 0, 0, 0.03);
}

.panel-head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
    flex-wrap: wrap;
    margin-bottom: 20px;
}

.panel-kicker {
    font-size: 0.72rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-muted);
}

.panel-title {
    margin: 4px 0 0;
    font-size: 1.1rem;
    font-weight: 700;
}

.legend {
    display: flex;
    gap: 16px;
    align-items: center;
    flex-wrap: wrap;
}

.legend-item {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-size: 0.82rem;
    color: var(--text-secondary);
}

.dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    display: inline-block;
}

.dot.in { background: var(--accent-info); }
.dot.out { background: #d49b47; }
.dot.total { background: #8b5cf6; }

/* Line chart */
.line-chart {
    position: relative;
    min-height: 260px;
}

.line-svg {
    width: 100%;
    height: 240px;
    display: block;
}

.grid-line {
    stroke: rgba(24, 49, 77, 0.08);
    stroke-width: 1;
}

.line-path {
    fill: none;
    stroke-width: 2.4;
    stroke-linecap: round;
    stroke-linejoin: round;
}

.line-path.in { stroke: var(--accent-info); }
.line-path.out { stroke: #d49b47; }

.point-dot {
    stroke: #fff;
    stroke-width: 1.2;
}

.point-dot.in { fill: var(--accent-info); }
.point-dot.out { fill: #d49b47; }

.axis-x {
    display: flex;
    justify-content: space-between;
    margin-top: 8px;
    padding-inline: 6px;
}

.axis-x span {
    color: var(--text-muted);
    font-size: 0.74rem;
}

/* Mid grid: gate bars + donut */
.mid-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 22px;
}

.ops-grid.two {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 22px;
}

.ops-panel {
    border: 1px solid var(--border-soft);
    border-radius: 22px;
    background: var(--surface);
    padding: 24px 28px;
    box-shadow: 0 4px 18px rgba(0, 0, 0, 0.03);
}

/* Gate bars */
.gate-bars {
    display: grid;
    gap: 14px;
}

.gate-row {
    display: grid;
    grid-template-columns: 130px minmax(0, 1fr) 44px;
    gap: 14px;
    align-items: center;
}

.gate-name {
    font-size: 0.84rem;
    color: var(--text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.gate-track {
    height: 22px;
    background: var(--surface-muted);
    border-radius: 999px;
    overflow: hidden;
}

.gate-stack {
    display: flex;
    height: 100%;
}

.gate-bar {
    height: 100%;
    transition: width 0.4s ease;
}

.gate-bar.in { background: linear-gradient(90deg, var(--accent-primary), var(--accent-info)); }
.gate-bar.out { background: linear-gradient(90deg, #e6b56b, #c47d2d); }

.gate-total {
    font-family: var(--font-heading);
    font-weight: 700;
    color: var(--text-primary);
    text-align: right;
}

/* Donut */
.donut-wrap {
    display: flex;
    align-items: center;
    gap: 24px;
    flex-wrap: wrap;
}

.donut-holder {
    position: relative;
    width: 170px;
    height: 170px;
    flex-shrink: 0;
}

.donut-svg {
    width: 100%;
    height: 100%;
    transform: rotate(-90deg);
}

.donut-ring {
    stroke: var(--surface-muted);
    stroke-width: 4.5;
}

.donut-seg {
    stroke-width: 4.5;
    transition: stroke-dasharray 0.4s ease;
}

.donut-center {
    position: absolute;
    inset: 0;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    text-align: center;
}

.donut-center strong {
    font-family: var(--font-heading);
    font-size: 1.8rem;
    line-height: 1;
}

.donut-center span {
    margin-top: 4px;
    color: var(--text-muted);
    font-size: 0.74rem;
}

.donut-legend {
    list-style: none;
    margin: 0;
    padding: 0;
    display: grid;
    gap: 8px;
    flex: 1;
    min-width: 150px;
}

.donut-legend li {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 0.84rem;
}

.swatch {
    width: 12px;
    height: 12px;
    border-radius: 4px;
    flex-shrink: 0;
}

.legend-label {
    flex: 1;
    color: var(--text-secondary);
}

.legend-count {
    font-weight: 700;
    color: var(--text-primary);
}

/* Heatmap */
.heatmap {
    display: grid;
    gap: 8px;
}

.heatmap-row {
    display: grid;
    grid-template-columns: 48px repeat(24, minmax(0, 1fr));
    gap: 4px;
    align-items: center;
}

.heatmap-header {
    margin-bottom: 6px;
}

.heatmap-corner,
.heatmap-hour {
    color: var(--text-muted);
    font-size: 0.72rem;
    font-weight: 600;
    text-align: center;
}

.heatmap-cell {
    height: 20px;
    border-radius: 5px;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.heatmap-cell:hover {
    transform: scale(1.2);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
    z-index: 2;
}

.lvl-0 { background: var(--surface-muted); }
.lvl-1 { background: rgba(15, 124, 130, 0.18); }
.lvl-2 { background: rgba(15, 124, 130, 0.38); }
.lvl-3 { background: rgba(15, 124, 130, 0.6); }
.lvl-4 { background: rgba(15, 124, 130, 0.8); }
.lvl-5 { background: #0f7c82; }

/* Panel stats */
.panel-stats {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
}

.stat-pill {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    padding: 4px 10px;
    border-radius: 8px;
    background: var(--surface-muted);
    font-size: 0.76rem;
    color: var(--text-secondary);
    border: 1px solid var(--border-soft);
}

.stat-pill-label {
    color: var(--text-muted);
}

.stat-pill-value {
    font-weight: 700;
    color: var(--accent-primary);
}

/* Visitor chart */
.visitor-panel {
    display: flex;
    flex-direction: column;
    justify-content: space-between;
}

.visitor-chart-wrap {
    position: relative;
    width: 100%;
    margin-top: 4px;
}

.visitor-svg-container {
    position: relative;
    width: 100%;
}

.visitor-svg {
    width: 100%;
    height: 180px;
    display: block;
    overflow: visible;
}

.visitor-grid-line {
    stroke: rgba(24, 49, 77, 0.08);
    stroke-width: 1;
    stroke-dasharray: 3 3;
}

.visitor-axis-y-text {
    font-size: 10px;
    fill: var(--text-muted);
    font-family: inherit;
    font-weight: 500;
}

.visitor-axis-x-text {
    font-size: 10.5px;
    fill: var(--text-muted);
    font-family: inherit;
    font-weight: 500;
}

.visitor-slot-rect {
    fill: transparent;
    cursor: pointer;
    transition: fill 0.15s ease;
}

.visitor-slot-rect.active,
.visitor-slot-rect:hover {
    fill: rgba(15, 124, 130, 0.06);
}

.visitor-bar-bg {
    fill: var(--surface-muted);
    opacity: 0.7;
}

.visitor-bar-fill {
    transition: y 0.3s ease, height 0.3s ease, fill 0.2s ease;
    cursor: pointer;
}

.visitor-bar-fill.active {
    filter: drop-shadow(0 2px 6px rgba(15, 124, 130, 0.45));
}

.visitor-tooltip {
    position: absolute;
    pointer-events: none;
    background: var(--ink-900);
    color: #fff;
    padding: 6px 10px;
    border-radius: 8px;
    font-size: 0.75rem;
    white-space: nowrap;
    box-shadow: 0 6px 18px rgba(0, 0, 0, 0.25);
    z-index: 10;
    transition: left 0.1s ease, bottom 0.1s ease;
}

.visitor-tooltip::after {
    content: '';
    position: absolute;
    top: 100%;
    left: 50%;
    transform: translateX(-50%);
    border-width: 5px;
    border-style: solid;
    border-color: var(--ink-900) transparent transparent transparent;
}

.tooltip-date {
    font-size: 0.7rem;
    color: var(--mist-400);
    margin-bottom: 2px;
}

.tooltip-val strong {
    font-weight: 700;
    color: var(--glow-500);
    font-size: 0.85rem;
}

.fade-fast-enter-active,
.fade-fast-leave-active {
    transition: opacity 0.15s ease, transform 0.15s ease;
}

.fade-fast-enter-from,
.fade-fast-leave-to {
    opacity: 0;
    transform: translateX(-50%) translateY(4px);
}

/* Visitor Status Cards */
.visitor-status-cards {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    margin-top: 18px;
    padding-top: 14px;
    border-top: 1px solid var(--border-soft);
}

.visitor-status-pill {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 6px 14px;
    border-radius: 999px;
    font-size: 0.82rem;
    font-weight: 500;
    background: var(--surface-muted);
    border: 1px solid var(--border-soft);
    transition: all 0.2s ease;
}

.visitor-status-pill:hover {
    transform: translateY(-1px);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.visitor-status-pill .status-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: var(--text-muted);
}

.visitor-status-pill .status-name {
    color: var(--text-secondary);
}

.visitor-status-pill .status-val {
    font-weight: 700;
    color: var(--text-primary);
    font-family: var(--font-heading);
}

/* Status variants */
.status-checkedin {
    background: rgba(84, 196, 211, 0.12);
    border-color: rgba(84, 196, 211, 0.3);
}
.status-checkedin .status-dot { background: var(--glow-500); box-shadow: 0 0 6px var(--glow-500); }
.status-checkedin .status-name { color: #0d6d72; }

.status-overstay {
    background: rgba(195, 81, 70, 0.12);
    border-color: rgba(195, 81, 70, 0.3);
}
.status-overstay .status-dot { background: var(--accent-danger); box-shadow: 0 0 6px rgba(195, 81, 70, 0.6); }
.status-overstay .status-name { color: var(--accent-danger); font-weight: 600; }
.status-overstay .status-val { color: var(--accent-danger); }

.status-checkedout, .status-completed {
    background: rgba(20, 134, 109, 0.12);
    border-color: rgba(20, 134, 109, 0.3);
}
.status-checkedout .status-dot, .status-completed .status-dot { background: var(--accent-success); }
.status-checkedout .status-name, .status-completed .status-name { color: #0d6855; }

.status-approved {
    background: rgba(139, 92, 246, 0.12);
    border-color: rgba(139, 92, 246, 0.3);
}
.status-approved .status-dot { background: #8b5cf6; }
.status-approved .status-name { color: #6d28d9; }

.status-pending {
    background: rgba(216, 155, 55, 0.12);
    border-color: rgba(216, 155, 55, 0.3);
}
.status-pending .status-dot { background: var(--sun-500); }
.status-pending .status-name { color: #b86f21; }

/* Anomaly table */
.anomaly-table {
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.anomaly-row {
    display: grid;
    grid-template-columns: 140px minmax(120px, 1fr) 70px minmax(160px, 2fr);
    gap: 14px;
    align-items: center;
    padding: 12px 16px;
    border-radius: 12px;
    font-size: 0.86rem;
}

.anomaly-row:nth-child(odd) {
    background: var(--surface-muted);
}

.anomaly-head {
    background: transparent !important;
    color: var(--text-muted);
    font-weight: 700;
    font-size: 0.76rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    padding-bottom: 8px;
}

.anomaly-note {
    color: var(--text-secondary);
}

.badge {
    padding: 3px 10px;
    border-radius: 999px;
    font-size: 0.74rem;
    font-weight: 700;
}

.in-badge { background: rgba(15, 124, 130, 0.14); color: var(--accent-primary); }
.out-badge { background: rgba(196, 125, 45, 0.14); color: #c47d2d; }

.alert-danger-bar {
    padding: 16px 20px;
    border-radius: 16px;
    background: rgba(195, 81, 70, 0.1);
    color: var(--accent-danger);
    border: 1px solid rgba(195, 81, 70, 0.25);
    font-size: 0.9rem;
}

@media (max-width: 1180px) {
    .kpi-grid {
        grid-template-columns: repeat(3, minmax(0, 1fr));
    }
    .mid-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 820px) {
    .kpi-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
    .anomaly-row {
        grid-template-columns: 120px 1fr 60px;
    }
    .anomaly-row .anomaly-note {
        grid-column: 1 / -1;
    }
    .heatmap-row {
        grid-template-columns: 40px repeat(24, minmax(0, 1fr));
        gap: 2px;
    }
}

@media (max-width: 560px) {
    .kpi-grid {
        grid-template-columns: 1fr;
    }
    .gate-row {
        grid-template-columns: 96px minmax(0, 1fr) 40px;
    }
    .donut-wrap {
        flex-direction: column;
        align-items: center;
    }
}
</style>
