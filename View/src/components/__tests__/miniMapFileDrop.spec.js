import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

const MapMiniStats = (await import('../campus-map/MapMiniStats.vue')).default
const FileDropZone = (await import('../import-export/FileDropZone.vue')).default

describe('MapMiniStats', () => {
  it('renders default zero metrics', () => {
    const wrapper = mount(MapMiniStats)
    expect(wrapper.findAll('.metric-tile').length).toBe(6)
    expect(wrapper.text()).toContain('Site trong mô hình')
    expect(wrapper.text()).toContain('Camera offline')
  })

  it('renders provided summary values', () => {
    const summary = { siteCount: 3, objectCount: 42, activeGateCount: 5, warningGateCount: 2, offlineCameraCount: 1, recentEventCount: 99 }
    const wrapper = mount(MapMiniStats, { props: { summary } })
    expect(wrapper.find('.metric-tile').html()).toContain('3')
    expect(wrapper.text()).toContain('42')
    expect(wrapper.text()).toContain('99')
  })
})

describe('FileDropZone', () => {
  const makeFile = (name = 'report.csv', size = 2048) => new File(['x'.repeat(size)], name, { type: 'text/csv' })

  it('renders drop hint with default formats', () => {
    const wrapper = mount(FileDropZone)
    expect(wrapper.find('.drop-title').text()).toContain('Kéo thả file')
    expect(wrapper.find('.drop-hint').text()).toContain('CSV, Excel, JSON, XML')
  })

  it('selects a file via change event and emits file-selected', async () => {
    const wrapper = mount(FileDropZone)
    const input = wrapper.find('input[type="file"]')
    Object.defineProperty(input.element, 'files', { value: [makeFile('data.xlsx', 204800)], configurable: true })
    await input.trigger('change')
    expect(wrapper.find('.file-name').text()).toBe('data.xlsx')
    expect(wrapper.find('.file-size').text()).toBe('200.0 KB')
    expect(wrapper.emitted('file-selected')).toBeTruthy()
    expect(wrapper.emitted('file-selected')[0][0].name).toBe('data.xlsx')
  })

  it('switches to dragover styling and selects on drop', async () => {
    const wrapper = mount(FileDropZone)
    await wrapper.trigger('dragover')
    expect(wrapper.classes()).toContain('is-dragover')
    const dt = { files: [makeFile('data.json', 500) ] }
    await wrapper.trigger('drop', { dataTransfer: dt })
    expect(wrapper.classes()).not.toContain('is-dragover')
    expect(wrapper.find('.file-name').text()).toBe('data.json')
    expect(wrapper.find('.file-size').text()).toBe('500 B')
  })

  it('removes file and emits file-removed', async () => {
    const wrapper = mount(FileDropZone)
    const input = wrapper.find('input[type="file"]')
    Object.defineProperty(input.element, 'files', { value: [makeFile('data.csv')], configurable: true })
    await input.trigger('change')
    expect(wrapper.find('.file-selected').exists()).toBe(true)
    await wrapper.find('.remove-btn').trigger('click')
    expect(wrapper.find('.file-selected').exists()).toBe(false)
    expect(wrapper.emitted('file-removed')).toBeTruthy()
  })
})