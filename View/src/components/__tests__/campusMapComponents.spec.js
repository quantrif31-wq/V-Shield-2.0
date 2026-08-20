import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'

vi.mock('../../services/http', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() }
}))

const CampusMapToolbar = (await import('../campus-map/CampusMapToolbar.vue')).default
const RealtimeStatusPanel = (await import('../campus-map/RealtimeStatusPanel.vue')).default
const IndoorPathViewer = (await import('../campus-map/IndoorPathViewer.vue')).default
const ElementPropertiesPanel = (await import('../campus-map/ElementPropertiesPanel.vue')).default

describe('CampusMapToolbar', () => {
  it('renders mode buttons and emits change-mode', async () => {
    const wrapper = mount(CampusMapToolbar, { props: { mode: 'view', canEdit: true } })
    expect(wrapper.text()).toContain('Chế độ xem')
    expect(wrapper.find('.btn-primary').text()).toContain('Chế độ xem')
    await wrapper.findAll('button')[1].trigger('click')
    expect(wrapper.emitted('change-mode')[0][0]).toBe('edit')
  })

  it('disables edit actions without permission', () => {
    const wrapper = mount(CampusMapToolbar, { props: { mode: 'view', canEdit: false } })
    const buttons = wrapper.findAll('button')
    expect(buttons[1].attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Lưu bố cục')
  })

  it('shows saving/refreshing labels and emits actions', async () => {
    const wrapper = mount(CampusMapToolbar, { props: { mode: 'edit', canEdit: true, dirty: true, saving: true, refreshing: true } })
    expect(wrapper.text()).toContain('Đang lưu...')
    expect(wrapper.text()).toContain('Đang tải...')
    const buttons = wrapper.findAll('button')
    await buttons[buttons.length - 3].trigger('click')
    expect(wrapper.emitted('fit-screen')).toBeTruthy()
  })
})

describe('RealtimeStatusPanel', () => {
  const events = [
    { logId: 1, gateId: 7, gateName: 'Cổng A', timestamp: '2026-08-20T10:00:00Z', direction: 'Vào', resultStatus: 'APPROVED', actorName: 'An', cameraName: 'CAM-1' },
    { logId: 2, gateId: 8, gateName: 'Cổng B', timestamp: '2026-08-20T10:01:00Z', direction: 'Ra', resultStatus: 'DENIED', actorName: 'Bình', capturedLicensePlate: '30A-123.45' }
  ]

  it('shows empty state without events', () => {
    const wrapper = mount(RealtimeStatusPanel)
    expect(wrapper.text()).toContain('Chưa có hoạt động gần đây.')
    expect(wrapper.text()).toContain('Chưa cập nhật')
  })

  it('renders error message when present', () => {
    const wrapper = mount(RealtimeStatusPanel, { props: { error: 'Không tải được' } })
    expect(wrapper.text()).toContain('Không tải được')
  })

  it('renders event list in compact=false and emits focus-gate', async () => {
    const wrapper = mount(RealtimeStatusPanel, { props: { recentEvents: events, updatedAt: '2026-08-20T10:02:00Z' } })
    expect(wrapper.findAll('.event-card').length).toBe(2)
    expect(wrapper.find('.updated-at').text()).toContain('Cập nhật:')
    await wrapper.findAll('.event-card')[0].trigger('click')
    expect(wrapper.emitted('focus-gate')[0][0]).toBe(7)
    expect(wrapper.find('.event-status').classes()).toContain('ok')
  })

  it('renders compact orbit dots with statuses', async () => {
    const wrapper = mount(RealtimeStatusPanel, { props: { recentEvents: events, compact: true } })
    expect(wrapper.findAll('.event-dot').length).toBe(2)
    expect(wrapper.find('.event-dot').classes()).toContain('ok')
    await wrapper.findAll('.event-dot')[1].trigger('click')
    expect(wrapper.emitted('focus-gate')[0][0]).toBe(8)
  })
})

describe('IndoorPathViewer', () => {
  it('loads nodes on mount and filters by floor', async () => {
    const http = (await import('../../services/http')).default
    http.get.mockResolvedValue({
      data: {
        data: [
          { id: 1, label: 'Cổng 1', nodeType: 'Entrance', x: 10, z: 20, facilityFloorId: 1, facilityFloorName: 'Tầng 1' },
          { id: 2, label: 'Phòng họp', nodeType: 'Room', x: 30, z: 40, facilityFloorId: 2, facilityFloorName: 'Tầng 2' }
        ]
      }
    })
    const wrapper = mount(IndoorPathViewer, { props: { buildingId: 5, targetLabel: 'Phòng họp' } })
    await flushPromises()
    expect(http.get).toHaveBeenCalledWith('/indoor-map/nodes?buildingId=5')
    expect(wrapper.findAll('.path-node').length).toBe(1)
    const select = wrapper.find('select')
    expect((select.element).value).toBe('Tầng 1')
    await select.setValue('Tầng 2')
    expect(wrapper.findAll('.path-node').length).toBe(1)
    expect(wrapper.find('.node-target').exists()).toBe(true)
    await select.setValue('Tầng 1')
    expect(wrapper.findAll('.path-node')[0].classes()).toContain('node-entrance')
  })

  it('shows empty state when no nodes and handles load error', async () => {
    const http = (await import('../../services/http')).default
    http.get.mockRejectedValue(new Error('boom'))
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    const wrapper = mount(IndoorPathViewer, { props: { buildingId: 3 } })
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa có dữ liệu bản đồ trong nhà')
    spy.mockRestore()
  })

  it('shows loading state and empty without buildingId', () => {
    const wrapper = mount(IndoorPathViewer)
    expect(wrapper.text()).toContain('Chưa có dữ liệu')
  })
})

describe('ElementPropertiesPanel', () => {
  const item = {
    gateId: 9,
    gateName: 'Cổng B1',
    location: 'Tầng 1 - Sảnh',
    status: 'Active',
    stats: { cameraCount: 2, offlineCameraCount: 1, recentAccessCount: 5 },
    layout: { x: 10, y: 20, w: 120, h: 70, zIndex: 3, icon: '🚪', color: '#0f766e', isVisible: true, isLocked: false }
  }

  it('renders empty state without item', () => {
    const wrapper = mount(ElementPropertiesPanel)
    expect(wrapper.text()).toContain('Chọn một cổng trên bản đồ')
  })

  it('renders details and status label mapping', () => {
    const wrapper = mount(ElementPropertiesPanel, { props: { item } })
    expect(wrapper.find('.panel-title').text()).toBe('Cổng B1')
    expect(wrapper.text()).toContain('2 / 1')
    expect(wrapper.text()).toContain('Đang hoạt động')
  })

  it('emits patch-layout for number/text/checkbox changes when editable', async () => {
    const wrapper = mount(ElementPropertiesPanel, { props: { item, editable: true } })
    const xInput = wrapper.findAll('input[type="number"]')[0]
    xInput.element.value = '55'
    await xInput.trigger('change')
    expect(wrapper.emitted('patch-layout')[0][0]).toEqual({ x: 55 })
    const iconInput = wrapper.findAll('input[type="text"]')[0]
    iconInput.element.value = '🏛️'
    await iconInput.trigger('change')
    expect(wrapper.emitted('patch-layout')[1][0]).toEqual({ icon: '🏛️' })
    const visibleCheckbox = wrapper.findAll('input[type="checkbox"]')[0]
    visibleCheckbox.element.checked = false
    await visibleCheckbox.trigger('change')
    expect(wrapper.emitted('patch-layout')[2][0]).toEqual({ gateId: 9, isVisible: false })
  })
})