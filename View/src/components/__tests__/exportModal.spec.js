import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/importExportApi', () => ({
    getFormats: vi.fn(),
    exportData: vi.fn(),
    downloadResult: vi.fn(),
    downloadTemplate: vi.fn(),
}))

const api = await import('../../services/importExportApi')
const ExportModal = (await import('../import-export/ExportModal.vue')).default
const formatList = [
    { format: 'csv', displayName: 'CSV', supportsExport: true },
    { format: 'json', displayName: 'JSON', supportsExport: true },
    { format: 'xml', displayName: 'XML', supportsExport: false },
]

beforeEach(() => {
    vi.clearAllMocks()
    api.getFormats.mockResolvedValue({ data: formatList })
})

afterEach(() => {
    vi.restoreAllMocks()
})

describe('ExportModal', () => {
    it('renders title and loads export formats', async () => {
        const wrapper = mount(ExportModal, { props: { entityType: 'employees', entityDisplayName: 'nhân viên' } })
        await flushPromises()
        expect(wrapper.text()).toContain('Xuất nhân viên')
        expect(api.getFormats).toHaveBeenCalled()
        expect(wrapper.findAll('.format-btn').length).toBe(2)
        expect(wrapper.text()).toContain('CSV')
    })

    it('emits close via overlay and close button', async () => {
        const wrapper = mount(ExportModal, { props: { entityType: 'employees' } })
        await flushPromises()
        await wrapper.get('.modal-overlay').trigger('click')
        expect(wrapper.emitted('close')).toBeTruthy()
    })

    it('pre-selects provided columns and renders display names', async () => {
        const wrapper = mount(ExportModal, {
            props: { entityType: 'employees', availableColumns: ['fullName', 'roleName'] },
        })
        await flushPromises()
        expect(wrapper.text()).toContain('Full Name')
        expect(wrapper.text()).toContain('Role Name')
        const checked = wrapper.findAll('.column-checkbox input').filter((i) => i.element.checked)
        expect(checked.length).toBe(2)
    })

    it('switches selected format', async () => {
        const wrapper = mount(ExportModal, { props: { entityType: 'employees' } })
        await flushPromises()
        const jsonBtn = wrapper.findAll('.format-btn').find((b) => b.text().includes('.json'))
        await jsonBtn.trigger('click')
        expect(jsonBtn.classes()).toContain('active')
    })

    it('selects and clears all columns', async () => {
        const wrapper = mount(ExportModal, {
            props: { entityType: 'employees', availableColumns: ['fullName', 'roleName', 'phone'] },
        })
        await flushPromises()
        const clearBtn = wrapper.findAll('.btn-link').find((b) => b.text().includes('Bỏ chọn'))
        await clearBtn.trigger('click')
        expect(wrapper.findAll('.column-checkbox input').filter((i) => i.element.checked).length).toBe(0)
        const selectAll = wrapper.findAll('.btn-link').find((b) => b.text().includes('Chọn tất cả'))
        await selectAll.trigger('click')
        expect(wrapper.findAll('.column-checkbox input').filter((i) => i.element.checked).length).toBe(3)
    })

    it('formats result sizes for KB and MB', async () => {
        const wrapper = mount(ExportModal, { props: { entityType: 'employees' } })
        await flushPromises()
        api.exportData.mockResolvedValue({ data: { totalRows: 10, fileFormat: 'csv', fileSize: 2048, downloadUrl: null, fileName: 'x.csv' } })
        const exportBtn = wrapper.findAll('button').find((b) => b.text().includes('📤 Xuất'))
        await exportBtn.trigger('click')
        await flushPromises()
        expect(api.exportData).toHaveBeenCalledWith('employees', { format: 'csv', includeHeaders: true, columns: [] })
        expect(wrapper.text()).toContain('2.0 KB')
        wrapper.unmount()

        const wrapper2 = mount(ExportModal, { props: { entityType: 'employees', availableColumns: ['a'] } })
        await flushPromises()
        api.exportData.mockResolvedValue({ data: { totalRows: 1, fileFormat: 'xlsx', fileSize: 3 * 1024 * 1024, downloadUrl: null, fileName: 'x.xlsx' } })
        const exportBtn2 = wrapper2.findAll('button').find((b) => b.text().includes('📤 Xuất'))
        await exportBtn2.trigger('click')
        await flushPromises()
        expect(wrapper2.text()).toContain('3.0 MB')
    })

    it('triggers download when result has a downloadUrl', async () => {
        const createObjectURL = vi.fn(() => 'blob:dl')
        const revokeObjectURL = vi.fn()
        Object.defineProperty(window.URL, 'createObjectURL', { configurable: true, writable: true, value: createObjectURL })
        Object.defineProperty(window.URL, 'revokeObjectURL', { configurable: true, writable: true, value: revokeObjectURL })
        const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
        api.exportData.mockResolvedValue({ data: { totalRows: 5, fileFormat: 'csv', fileSize: 100, downloadUrl: '/download/1', historyId: 1, fileName: 'report.csv' } })
        api.downloadResult.mockResolvedValue({ data: new Blob(['csv']) })
        const wrapper = mount(ExportModal, { props: { entityType: 'employees', availableColumns: ['a'] } })
        await flushPromises()
        const exportBtn = wrapper.findAll('button').find((b) => b.text().includes('📤 Xuất'))
        await exportBtn.trigger('click')
        await flushPromises()
        expect(api.downloadResult).toHaveBeenCalledWith(1)
        expect(createObjectURL).toHaveBeenCalled()
        expect(clickSpy).toHaveBeenCalled()
        expect(revokeObjectURL).toHaveBeenCalled()
    })

    it('downloads a template file', async () => {
        const revokeObjectURL = vi.fn()
        Object.defineProperty(window.URL, 'revokeObjectURL', { configurable: true, writable: true, value: revokeObjectURL })
        const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
        api.downloadTemplate.mockResolvedValue({ data: new Blob(['t']) })
        const wrapper = mount(ExportModal, { props: { entityType: 'employees' } })
        await flushPromises()
        const templateBtn = wrapper.findAll('button').find((b) => b.text().includes('Tải template'))
        await templateBtn.trigger('click')
        await flushPromises()
        expect(api.downloadTemplate).toHaveBeenCalledWith('employees', 'csv')
        expect(clickSpy).toHaveBeenCalled()
    })

    it('does not crash when getFormats fails', async () => {
        api.getFormats.mockRejectedValue(new Error('network'))
        const wrapper = mount(ExportModal, { props: { entityType: 'employees' } })
        await flushPromises()
        expect(wrapper.find('.format-grid').exists()).toBe(true)
    })
})