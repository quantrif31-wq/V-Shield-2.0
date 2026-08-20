import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'

vi.mock('maplibre-gl', () => {
  const noop = () => {}
  class FakeMap {
    static Mock = true
    on = vi.fn()
    addControl = vi.fn()
    addLayer = vi.fn()
    removeLayer = vi.fn()
    addSource = vi.fn()
    removeSource = vi.fn()
    fitBounds = vi.fn()
    remove = vi.fn()
  }
  return {
    default: {
      Map: FakeMap,
      Marker: class { setLngLat() { return this } setPopup() { return this } addTo() { return this } },
      Popup: class { setHTML() { return this } },
      NavigationControl: class {},
      LngLatBounds: class { extend() { return this } }
    }
  }
})

vi.mock('../../services/routingApi', () => ({ routingApi: { getRoute: vi.fn() } }))
vi.mock('../../services/observability', () => ({ captureError: vi.fn(), recordMetric: vi.fn() }))

const AlarmMap = (await import('../maplibre/AlarmMap.vue')).default
const AiOcrProgress = (await import('../import-export/ai/AiOcrProgress.vue')).default
const AiSuggestionPopup = (await import('../import-export/ai/AiSuggestionPopup.vue')).default
const AiPreviewConfirm = (await import('../import-export/ai/AiPreviewConfirm.vue')).default

describe('AlarmMap', () => {
  it('does not init map without alarm latitude, shows no panel', () => {
    const wrapper = mount(AlarmMap, { props: { alarm: null } })
    expect(wrapper.find('.alarm-info-panel').exists()).toBe(false)
  })

  it('renders alarm info panel with coords and formats route data', async () => {
    const { routingApi } = await import('../../services/routingApi')
    const alarm = { title: 'Cảnh báo A', locationLabel: 'Tòa B', body: 'Cháy', latitude: 21.0285, longitude: 105.8048 }
    routingApi.getRoute.mockResolvedValue({
      data: {
        data: { totalDistanceMeters: 1500, totalDurationSeconds: 3900, targetBuildingName: 'Tòa B', targetFloorLabel: 'Tầng 2', outdoorGeoJson: { type: 'LineString', coordinates: [] } }
      }
    })
    const wrapper = mount(AlarmMap, { props: { alarm, buildingId: 1, targetNodeId: 2 } })
    expect(wrapper.find('.alarm-info-panel').exists()).toBe(true)
    expect(wrapper.text()).toContain('21.028500, 105.804800')
    expect(wrapper.find('.btn-route').exists()).toBe(true)

    await wrapper.find('.btn-route').trigger('click')
    await new Promise((r) => setTimeout(r, 0))
    expect(routingApi.getRoute).toHaveBeenCalled()
    expect(wrapper.text()).toContain('1.5 km')
    expect(wrapper.text()).toContain('1h 5ph')
    expect(wrapper.find('.btn-maps').exists()).toBe(true)
  })

  it('handles route failure gracefully', async () => {
    const { routingApi } = await import('../../services/routingApi')
    routingApi.getRoute.mockRejectedValue(new Error('no route'))
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    const alarm = { title: 'A', latitude: 10.5, longitude: 106.5 }
    const wrapper = mount(AlarmMap, { props: { alarm } })
    await wrapper.find('.btn-route').trigger('click')
    await new Promise((r) => setTimeout(r, 0))
    expect(wrapper.vm.loading).toBe(false)
    spy.mockRestore()
  })
})

describe('AiOcrProgress', () => {
  it('renders default pending steps when idle', () => {
    const wrapper = mount(AiOcrProgress)
    expect(wrapper.findAll('.step').length).toBe(4)
    expect(wrapper.text()).toContain('Phân tích file')
    expect(wrapper.text()).toContain('Đề xuất chuẩn hóa')
  })

  it('shows spinner while processing and success badge when done', () => {
    const processing = mount(AiOcrProgress, { props: { status: 'processing', currentStep: 1 } })
    expect(processing.find('.spinner').exists()).toBe(true)
    expect(processing.findAll('.step')[1].classes()).toContain('active')
    const done = mount(AiOcrProgress, { props: { status: 'done', currentStep: 3 } })
    expect(done.text()).toContain('Hoàn tất')
    expect(done.findAll('.step')[0].classes()).toContain('done')
  })

  it('renders error and detected format', () => {
    const wrapper = mount(AiOcrProgress, { props: { status: 'error', error: 'Lỗi OCR', detectedFormat: 'csv' } })
    expect(wrapper.find('.ocr-error').text()).toContain('Lỗi OCR')
    expect(wrapper.text()).toContain('CSV')
    expect(wrapper.findAll('.step')[0].classes()).not.toContain('done')
  })
})

describe('AiSuggestionPopup', () => {
  const issues = [
    { row: 2, column: 'HoVaTen', originalValue: 'Nguyen Van A', suggestedValue: 'Nguyễn Văn A', confidence: 0.95, category: 'synonym' },
    { row: null, column: 'GioiTinh', originalValue: 'nam', suggestedValue: 'Nam', confidence: 0.5, category: 'case' },
    { row: 3, column: 'Khoa', originalValue: 'CNTT', suggestedValue: 'Công nghệ thông tin', confidence: 0.75, category: 'column_name' }
  ]

  it('renders nothing when hidden', () => {
    const wrapper = mount(AiSuggestionPopup, { props: { visible: false, issues } })
    expect(wrapper.find('.suggestion-popup').exists()).toBe(false)
  })

  it('shows empty state when no issues', () => {
    const wrapper = mount(AiSuggestionPopup, { props: { visible: true, issues: [] } })
    expect(wrapper.text()).toContain('Không có vấn đề về synonym')
  })

  it('groups issues by category, limits to 20 and emits close', async () => {
    const wrapper = mount(AiSuggestionPopup, { props: { visible: true, issues } })
    expect(wrapper.findAll('.issue-group').length).toBe(3)
    expect(wrapper.findAll('.issue-row').length).toBe(3)
    expect(wrapper.find('.group-badge.synonym').text()).toBe('Synonym')
    expect(wrapper.find('.confidence.high').exists()).toBe(true)
    await wrapper.find('.btn-icon').trigger('click')
    expect(wrapper.emitted('close')).toBeTruthy()
  })
})

describe('AiPreviewConfirm', () => {
  it('renders options without ai result', () => {
    const wrapper = mount(AiPreviewConfirm)
    expect(wrapper.text()).toContain('Áp dụng chuẩn hóa AI trước khi import')
    expect(wrapper.text()).toContain('Ghi đè dữ liệu trùng lặp')
  })

  it('renders stats and validation when aiResult present', async () => {
    const change = { field: 'ten', original: 'a', suggested: 'b' }
    const wrapper = mount(AiPreviewConfirm, {
      props: {
        aiResult: { ok: true },
        totalRows: 10,
        changeCount: 3,
        changes: [change],
        validation: { isValid: true, errorCount: 0 },
        synonymIssues: []
      }
    })
    expect(wrapper.text()).toContain('10 hàng')
    expect(wrapper.text()).toContain('3 thay đổi')
    expect(wrapper.text()).toContain('Hợp lệ')
    const cb = wrapper.findAll('input[type="checkbox"]')
    expect(cb[0].element.checked).toBe(true)
    expect(cb[1].element.checked).toBe(false)
  })

  it('shows invalid validation as error count', () => {
    const wrapper = mount(AiPreviewConfirm, { props: { aiResult: {}, totalRows: 0, changeCount: 0, validation: { isValid: false, errorCount: 5 } } })
    expect(wrapper.text()).toContain('5 lỗi')
  })
})