import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/importExportApi', () => ({
    importFile: vi.fn(),
    previewImport: vi.fn(),
    downloadTemplate: vi.fn(),
    aiAnalyze: vi.fn(),
    aiOcr: vi.fn(),
    aiNormalize: vi.fn(),
    aiConfirm: vi.fn(),
}))

const api = await import('../../services/importExportApi')
const ImportModal = (await import('../import-export/ImportModal.vue')).default

const stubs = {
    FileDropZone: {
        template: '<button class="fake-drop" @click="$emit(\'file-selected\', { name: \'data.csv\', size: 12 })">drop</button>',
    },
    AiOcrProgress: true,
    AiPreviewConfirm: true,
    transition: false,
}

const selectFile = async (wrapper) => {
    await wrapper.get('.fake-drop').trigger('click')
    await flushPromises()
}

beforeEach(() => {
    vi.clearAllMocks()
})

afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
})

describe('ImportModal', () => {
    it('renders the modal with entity title', () => {
        const wrapper = mount(ImportModal, { props: { entityType: 'employees', entityDisplayName: 'nhân viên' }, global: { stubs } })
        expect(wrapper.text()).toContain('Nhập nhân viên')
    })

    it('emits close on overlay click and on close button', async () => {
        const wrapper = mount(ImportModal, { props: { entityType: 'employees' }, global: { stubs } })
        await wrapper.get('.modal-overlay').trigger('click')
        expect(wrapper.emitted('close')).toBeTruthy()
        const wrapper2 = mount(ImportModal, { props: { entityType: 'employees' }, global: { stubs } })
        await wrapper2.get('.btn-icon').trigger('click')
        expect(wrapper2.emitted('close')).toBeTruthy()
    })

    it('shows import options after file selection', async () => {
        const wrapper = mount(ImportModal, { props: { entityType: 'employees' }, global: { stubs } })
        expect(wrapper.find('.import-options').exists()).toBe(false)
        await wrapper.get('.fake-drop').trigger('click')
        await flushPromises()
        expect(wrapper.find('.import-options').exists()).toBe(true)
        const dupe = wrapper.find('input[type="checkbox"]')
        await dupe.setValue(false)
        expect(wrapper.find('.import-options').exists()).toBe(true)
    })

    it('runs legacy import and emits import-complete on success', async () => {
        api.importFile.mockResolvedValue({ data: { status: 'Completed', successCount: 5, errorCount: 0, warningCount: 1, errors: [] } })
        const wrapper = mount(ImportModal, { props: { entityType: 'employees' }, global: { stubs } })
        await wrapper.get('.fake-drop').trigger('click')
        await flushPromises()
        const importBtn = wrapper.findAll('button').find((b) => b.text().includes('📥 Nhập'))
        await importBtn.trigger('click')
        await flushPromises()
        expect(api.importFile).toHaveBeenCalled()
        expect(wrapper.text()).toContain('Nhập thành công!')
        expect(wrapper.emitted('import-complete')).toBeTruthy()
        expect(wrapper.text()).toContain('5 thành công')
    })

    it('renders failed import with error rows', async () => {
        api.importFile.mockResolvedValue({
            data: { status: 'Failed', successCount: 0, errorCount: 2, warningCount: 0, errors: [{ row: 1, column: 'name', value: 'x', message: 'Thiếu trường' }] },
        })
        const wrapper = mount(ImportModal, { props: { entityType: 'employees' }, global: { stubs } })
        await selectFile(wrapper)
        const importBtn = wrapper.findAll('button').find((b) => b.text().includes('📥 Nhập'))
        await importBtn.trigger('click')
        await flushPromises()
        expect(wrapper.text()).toContain('Nhập thất bại')
        expect(wrapper.text()).toContain('Thiếu trường')
        expect(wrapper.emitted('import-complete')).toBeFalsy()
    })

    it('handles import API error', async () => {
        api.importFile.mockRejectedValue({ response: { data: { message: 'Server lỗi' } } })
        const wrapper = mount(ImportModal, { props: { entityType: 'employees' }, global: { stubs } })
        await selectFile(wrapper)
        const importBtn = wrapper.findAll('button').find((b) => b.text().includes('📥 Nhập'))
        await importBtn.trigger('click')
        await flushPromises()
        expect(wrapper.text()).toContain('Nhập thất bại')
        expect(wrapper.text()).toContain('Server lỗi')
        expect(wrapper.findAll('.error-table tbody tr').length).toBe(1)
    })

    it('runs AI preview flow for readable files', async () => {
        api.aiAnalyze.mockResolvedValue({ data: { isReadable: true, suggestedAction: 'normalize', detectedFormat: 'csv', sessionId: 's1', totalRows: 3 } })
        api.aiNormalize.mockResolvedValue({ data: { readyForImport: true, changeCount: 1, totalRows: 3, changes: [], validation: { errorCount: 0 } } })
        const wrapper = mount(ImportModal, { props: { entityType: 'employees' }, global: { stubs } })
        await selectFile(wrapper)
        const aiBtn = wrapper.findAll('button').find((b) => b.text().includes('Xem trước (AI)'))
        await aiBtn.trigger('click')
        await flushPromises()
        expect(api.aiAnalyze).toHaveBeenCalled()
        expect(api.aiNormalize).toHaveBeenCalled()
        expect(wrapper.text()).toContain('3 dòng, 1 thay đổi')
        const confirmBtn = wrapper.findAll('button').find((b) => b.text().includes('Xác nhận & Nhập'))
        expect(confirmBtn.exists()).toBe(true)
    })

    it('confirms AI import and emits import-complete', async () => {
        api.aiAnalyze.mockResolvedValue({ data: { isReadable: true, suggestedAction: 'normalize', detectedFormat: 'csv', sessionId: 's1', totalRows: 1 } })
        api.aiNormalize.mockResolvedValue({ data: { readyForImport: true, changeCount: 0, totalRows: 1, changes: [], validation: { errorCount: 0 } } })
        api.aiConfirm.mockResolvedValue({ data: { status: 'Completed', successCount: 1, errorCount: 0, warningCount: 0, errors: [] } })
        const wrapper = mount(ImportModal, { props: { entityType: 'employees' }, global: { stubs } })
        await selectFile(wrapper)
        const aiBtn = wrapper.findAll('button').find((b) => b.text().includes('Xem trước (AI)'))
        await aiBtn.trigger('click')
        await flushPromises()
        const confirmBtn = wrapper.findAll('button').find((b) => b.text().includes('Xác nhận & Nhập'))
        await confirmBtn.trigger('click')
        await flushPromises()
        expect(api.aiConfirm).toHaveBeenCalledWith('employees', 's1', { confirmNormalization: true, overrideConflicts: false })
        expect(wrapper.text()).toContain('Nhập thành công!')
    })

    it('runs OCR flow for unreadable files', async () => {
        vi.useFakeTimers()
        api.aiAnalyze.mockResolvedValue({ data: { isReadable: false, suggestedAction: 'ocr', sessionId: 's9', detectedFormat: 'pdf' } })
        api.aiOcr.mockResolvedValue({ data: { status: 'done', totalRows: 4, changeCount: 2, changes: [] } })
        api.aiNormalize.mockResolvedValue({ data: { readyForImport: true, changeCount: 2, totalRows: 4, changes: [], validation: { errorCount: 0 } } })
        const wrapper = mount(ImportModal, { props: { entityType: 'employees' }, global: { stubs } })
        await wrapper.get('.fake-drop').trigger('click')
        const aiBtn = wrapper.findAll('button').find((b) => b.text().includes('Xem trước (AI)'))
        await aiBtn.trigger('click')
        await vi.advanceTimersByTimeAsync(1500)
        await flushPromises()
        expect(api.aiOcr).toHaveBeenCalled()
        expect(api.aiNormalize).toHaveBeenCalled()
    })

    it('downloads a template file', async () => {
        const createObjectURL = vi.fn(() => 'blob:url')
        const revokeObjectURL = vi.fn()
        Object.defineProperty(window.URL, 'createObjectURL', { configurable: true, writable: true, value: createObjectURL })
        Object.defineProperty(window.URL, 'revokeObjectURL', { configurable: true, writable: true, value: revokeObjectURL })
        const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
        api.downloadTemplate.mockResolvedValue({ data: new Blob(['a']) })
        const wrapper = mount(ImportModal, { props: { entityType: 'employees' }, global: { stubs } })
        await selectFile(wrapper)
        const templateBtn = wrapper.findAll('button').find((b) => b.text().includes('Tải template'))
        await templateBtn.trigger('click')
        await flushPromises()
        expect(api.downloadTemplate).toHaveBeenCalledWith('employees', 'csv')
        expect(createObjectURL).toHaveBeenCalled()
        expect(clickSpy).toHaveBeenCalled()
        expect(revokeObjectURL).toHaveBeenCalled()
    })

    it('removes file and resets state', async () => {
        const wrapper = mount(ImportModal, {
            props: { entityType: 'employees' },
            global: {
                stubs: {
                    FileDropZone: {
                        template: '<button class="fake-drop" @click="$emit(\'file-selected\', { name: \'x.csv\' })">drop</button><button class="fake-remove" @click="$emit(\'file-removed\')">remove</button>',
                    },
                },
            },
        })
        await wrapper.get('.fake-drop').trigger('click')
        await flushPromises()
        expect(wrapper.find('.import-options').exists()).toBe(true)
        await wrapper.get('.fake-remove').trigger('click')
        await flushPromises()
        expect(wrapper.find('.import-options').exists()).toBe(false)
    })
})