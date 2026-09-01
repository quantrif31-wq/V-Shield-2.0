import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/dashboardApi', () => ({ getDashboardReports: vi.fn() }))

const dashboardApi = await import('../../services/dashboardApi')
const Dashboard = (await import('../Dashboard.vue')).default

const report = {
  generatedAt: '2026-08-01T08:00:00Z',
  kpis: {
    todayTotal: 10,
    todayCheckIn: 6,
    todayCheckOut: 4,
    checkedInVisitors: 3,
    todayVisitors: 7,
    todayAnomalies: 2,
    openAlarms: 3,
    criticalAlarms: 1,
    offlineDevices: 1,
    degradedDevices: 2,
  },
  trafficByDay: [
    { label: 'T2', total: 5, checkIn: 3, checkOut: 2 },
    { label: 'T3', total: 7, checkIn: 4, checkOut: 3 },
    { label: 'T4', total: 6, checkIn: 3, checkOut: 3 },
  ],
  trafficByGate: [{ gateName: 'Cổng A', checkIn: 4, checkOut: 2, total: 6 }],
  hourlyByWeekday: [],
  attendanceStatus: [],
  attendanceTrend: [],
  visitorTrend: [],
  visitorStatus: [],
  alarmBySeverity: [],
  alarmByState: [],
  deviceByStatus: [],
  anomalies: [],
}

beforeEach(() => vi.clearAllMocks())

describe('Dashboard', () => {
  it('loads the dashboard report and renders KPIs', async () => {
    dashboardApi.getDashboardReports.mockResolvedValue({ data: report })
    const wrapper = mount(Dashboard)
    await flushPromises()
    expect(dashboardApi.getDashboardReports).toHaveBeenCalled()
    expect(wrapper.text()).toContain('10')
    expect(wrapper.text()).toContain('6')
  })

  it('renders the traffic line chart from daily data', async () => {
    dashboardApi.getDashboardReports.mockResolvedValue({ data: report })
    const wrapper = mount(Dashboard)
    await flushPromises()
    const linePath = wrapper.find('.line-path.in')
    expect(linePath.exists()).toBe(true)
    expect(linePath.attributes('d')).toContain('M ')
  })

  it('renders the visitor SVG bar chart and summary stats when visitorTrend is available', async () => {
    const reportWithVisitors = {
      ...report,
      visitorTrend: [
        { date: '2026-08-01', label: '01/08', total: 12 },
        { date: '2026-08-02', label: '02/08', total: 8 },
        { date: '2026-08-03', label: '03/08', total: 15 },
      ],
      visitorStatus: [
        { status: 'Overstay', count: 2 },
        { status: 'CheckedOut', count: 5 },
      ],
    }
    dashboardApi.getDashboardReports.mockResolvedValue({ data: reportWithVisitors })
    const wrapper = mount(Dashboard)
    await flushPromises()
    expect(wrapper.find('.visitor-svg').exists()).toBe(true)
    expect(wrapper.findAll('.visitor-bar-fill').length).toBe(3)
    expect(wrapper.find('.status-overstay').exists()).toBe(true)
    expect(wrapper.text()).toContain('Quá giờ')
    expect(wrapper.text()).toContain('35') // total: 12+8+15 = 35
  })

  it('handles a missing report gracefully', async () => {
    dashboardApi.getDashboardReports.mockResolvedValue({ data: null })
    const wrapper = mount(Dashboard)
    await flushPromises()
    expect(wrapper.exists()).toBe(true)
  })
})
