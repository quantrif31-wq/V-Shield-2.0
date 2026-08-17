import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/importExportApi', () => ({
  getHistory: vi.fn(),
  downloadResult: vi.fn(),
}))

const importExportApi = await import('../../services/importExportApi')
const ImportExportHistory = (await import('../ImportExportHistory.vue')).default

const historyRows = [
  { id: 1, operationType: 'Import', entityType: 'Employee', fileName: 'employees.csv', fileSize: 2048, status: 'Completed', successCount: 10, errorCount: 0, performedByName: 'admin', performedAt: '2026-08-01T00:00:00Z' },
  { id: 2, operationType: 'Export', entityType: 'Vehicle', fileName: 'vehicles.xlsx', fileSize: 3 * 1024 * 1024, status: 'Failed', successCount: 0, errorCount: 4, performedByName: 'baove', performedAt: '2026-08-01T00:00:00Z' },
]

beforeEach(() => vi.clearAllMocks())
afterEach(() => {
  vi.unstubAllGlobals()
})

describe('ImportExportHistory', () => {
  it('lists import/export history with badges and sizes', async () => {
    importExportApi.getHistory.mockResolvedValue({ data: historyRows })
    const wrapper = mount(ImportExportHistory)
    await flushPromises()
    expect(importExportApi.getHistory).toHaveBeenCalledWith({ operationType: undefined, entityType: undefined })
    expect(wrapper.text()).toContain('employees.csv')
    expect(wrapper.text()).toContain('2.0 KB')
    expect(wrapper.text()).toContain('3.0 MB')
    expect(wrapper.text()).toContain('Nhân viên')
    expect(wrapper.text()).toContain('✅ Hoàn tất')
  })

  it('refetches when filters change', async () => {
    importExportApi.getHistory.mockResolvedValue({ data: historyRows })
    const wrapper = mount(ImportExportHistory)
    await flushPromises()
    await wrapper.findAll('select')[0].setValue('Export')
    await flushPromises()
    expect(importExportApi.getHistory).toHaveBeenLastCalledWith({ operationType: 'Export', entityType: undefined })
  })

  it('downloads an export file via the API', async () => {
    importExportApi.getHistory.mockResolvedValue({ data: historyRows })
    importExportApi.downloadResult.mockResolvedValue({ data: new Blob(['x']) })
    vi.stubGlobal('URL', { ...window.URL, createObjectURL: vi.fn(() => 'blob:test'), revokeObjectURL: vi.fn() })
    const wrapper = mount(ImportExportHistory)
    await flushPromises()
    const downloadButton = wrapper.findAll('button').find((b) => b.text() === 'Tải về')
    await downloadButton.trigger('click')
    await flushPromises()
    expect(importExportApi.downloadResult).toHaveBeenCalledWith(2)
  })

  it('paginates long histories', async () => {
    const many = Array.from({ length: 45 }, (_, i) => ({ id: i + 1, operationType: 'Import', entityType: 'Employee', fileName: `f${i}.csv`, status: 'Completed', successCount: 1, errorCount: 0 }))
    importExportApi.getHistory.mockResolvedValue({ data: many })
    const wrapper = mount(ImportExportHistory)
    await flushPromises()
    expect(wrapper.text()).toContain('Trang 1 / 4')
    await wrapper.findAll('button').find((b) => b.text() === 'Sau').trigger('click')
    expect(wrapper.text()).toContain('Trang 2 / 4')
  })
})
