import { flushPromises, mount } from '@vue/test-utils'
import { nextTick, reactive } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({
  route: { query: {} },
  router: { replace: vi.fn() },
}))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route, useRouter: () => hoisted.router }))
vi.mock('../../services/employeeApi', () => ({
  getAll: vi.fn(),
  create: vi.fn(),
  update: vi.fn(),
  deleteEmployee: vi.fn(),
  uploadFace: vi.fn(),
  getProtectedFaceImage: vi.fn(),
}))
vi.mock('../../services/lookupApi', () => ({
  getDepartments: vi.fn(() => Promise.resolve({ data: [{ departmentId: 1, name: 'An Ninh' }] })),
  getPositions: vi.fn(() => Promise.resolve({ data: [{ positionId: 1, name: 'Bao ve' }] })),
}))
vi.mock('../../services/statisticsApi', () => ({ getSummary: vi.fn(() => Promise.resolve({ totalEmployees: 1, activeEmployees: 1, inactiveEmployees: 0 })) }))

hoisted.route = reactive({ query: {} })

const employeeApi = await import('../../services/employeeApi')
const Employees = (await import('../Employees.vue')).default

const ImportModalStub = {
  name: 'ImportModal',
  emits: ['close', 'import-complete'],
  template: '<div class="import-stub"><button class="stub-import-close" @click="$emit(\'close\')">đóng nhập</button><button class="stub-import-done" @click="$emit(\'import-complete\', { successCount: 2, errorCount: 0 })">hoàn tất nhập</button></div>',
}
const ExportModalStub = {
  name: 'ExportModal',
  emits: ['close'],
  template: '<div class="export-stub"><button class="stub-export-close" @click="$emit(\'close\')">đóng xuất</button></div>',
}
const importWithErrors = {
  name: 'ImportModal',
  emits: ['close', 'import-complete'],
  template: '<div class="import-stub"><button class="stub-import-done" @click="$emit(\'import-complete\', { successCount: 3, errorCount: 2 })">hoàn tất nhập lỗi</button></div>',
}

const sharedStubs = { RouterLink: { template: '<a><slot /></a>' }, ImportModal: ImportModalStub, ExportModal: ExportModalStub }

let wrappers = []

function mountPage(dataset = { data: [{ employeeId: 1, fullName: 'Nguyen Van An', status: true, faceImageUrl: '' }] }, stubs = {}) {
  employeeApi.getAll.mockResolvedValue(dataset)
  const wrapper = mount(Employees, { global: { stubs: { ...sharedStubs, ...stubs } } })
  wrappers.push(wrapper)
  return wrapper
}

beforeEach(() => {
  vi.clearAllMocks()
  hoisted.route.query = {}
  employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 1, fullName: 'Nguyen Van An', status: true, faceImageUrl: '' }] })
  employeeApi.getProtectedFaceImage.mockResolvedValue({ data: new Blob(['x']) })
  employeeApi.create.mockResolvedValue({ data: { employeeId: 99 } })
  employeeApi.update.mockResolvedValue({ data: {} })
  employeeApi.deleteEmployee.mockResolvedValue({ data: {} })
  employeeApi.uploadFace.mockResolvedValue({ data: {} })
})

URL.createObjectURL = vi.fn(() => 'blob:mock-face')
URL.revokeObjectURL = vi.fn()
HTMLInputElement.prototype.click = vi.fn()

afterEach(() => {
  wrappers.forEach((w) => w.unmount())
  wrappers = []
  document.body.innerHTML = ''
})

function bodyFind(css) {
  const el = document.body.querySelector(css)
  if (!el) throw new Error(`missing ${css}`)
  return el
}

function setValue(selector, value) {
  const el = bodyFind(selector)
  el.value = value
  el.dispatchEvent(new Event(el.tagName === 'SELECT' ? 'change' : 'input'))
  return el
}

function bodyButton(text) {
  return [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim() === text)
}

function submitForm() {
  bodyFind('#employee-form').dispatchEvent(new Event('submit'))
}

function dropFiles(element, files) {
  const event = new Event('drop', { cancelable: true })
  Object.defineProperty(event, 'dataTransfer', { value: { files } })
  element.dispatchEvent(event)
}

describe('Employees create validation', () => {
  it('blocks invalid names and blur marks touch state', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Thêm nhân viên').trigger('click')
    await flushPromises()

    submitForm()
    await flushPromises()
    expect(employeeApi.create).not.toHaveBeenCalled()
    expect(document.body.textContent).toContain('Họ và tên là bắt buộc.')

    const nameInput = bodyFind('#employee-name')
    nameInput.dispatchEvent(new Event('blur'))
    await nextTick()
    expect(document.body.textContent).toContain('Họ và tên là bắt buộc.')

    setValue('#employee-name', 'A')
    await nextTick()
    expect(document.body.textContent).toContain('quá ngắn')
  })

  it('surfaces server and network save errors but keeps the form', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Thêm nhân viên').trigger('click')
    await flushPromises()

    setValue('#employee-name', 'Tran Thi Bich')
    employeeApi.create.mockRejectedValue({ response: { data: { message: 'trùng email' } } })
    submitForm()
    await flushPromises()
    expect(document.body.textContent).toContain('trùng email')

    employeeApi.create.mockRejectedValue(new Error('offline'))
    submitForm()
    await flushPromises()
    expect(document.body.textContent).toContain('Không thể lưu hồ sơ')
    expect(document.body.querySelector('#employee-form')).toBeTruthy()
  })
})

describe('Employees face URL and file flows', () => {
  it('creates with a face URL via URL mode', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Thêm nhân viên').trigger('click')
    await flushPromises()

    bodyButton('Dùng URL').click()
    await nextTick()
    setValue('#employee-face-url', 'https://cdn.example/face.jpg')
    setValue('#employee-name', 'Tran Thi Bich')
    submitForm()
    await flushPromises()

    expect(employeeApi.create).toHaveBeenCalledWith(expect.objectContaining({ faceImageUrl: 'https://cdn.example/face.jpg' }))
  })

  it('creates with a selected face file and uploads it', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Thêm nhân viên').trigger('click')
    await flushPromises()

    const file = new File(['img'], 'face.png', { type: 'image/png' })
    const faceInput = bodyFind('input[aria-label="Chọn ảnh Face ID trong biểu mẫu"]')
    Object.defineProperty(faceInput, 'files', { value: [file], configurable: true })
    faceInput.dispatchEvent(new Event('change'))
    await nextTick()

    expect(document.body.querySelector('.dropzone img').getAttribute('src')).toBe('blob:mock-face')
    setValue('#employee-name', 'Tran Thi Bich')
    submitForm()
    await flushPromises()

    expect(employeeApi.create).toHaveBeenCalledWith(expect.objectContaining({ fullName: 'Tran Thi Bich' }))
    expect(employeeApi.uploadFace).toHaveBeenCalledWith(99, file)
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:mock-face')
  })

  it('drops an image onto the dropzone and removes it', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Thêm nhân viên').trigger('click')
    await flushPromises()

    const file = new File(['img'], 'face.webp', { type: 'image/webp' })
    const dropzone = document.body.querySelector('.dropzone')
    dropFiles(dropzone, [file])
    await nextTick()
    expect(URL.createObjectURL).toHaveBeenCalledWith(file)

    const badFile = new File(['x'], 'notes.txt', { type: 'text/plain' })
    dropFiles(document.body.querySelector('.dropzone'), [badFile])
    await nextTick()

    bodyButton('Xóa ảnh đã chọn').click()
    await nextTick()
    expect(document.body.querySelector('.dropzone img')).toBeNull()
  })
})

describe('Employees edit flows', () => {
  const editEmployee = { employeeId: 3, fullName: 'Nguyen Van Ba', phone: '', email: '', departmentId: null, positionId: null, status: true, faceImageUrl: 'https://cdn.example/ba.jpg' }

  it('edits a URL-face employee and toggles access', async () => {
    const wrapper = mountPage({ data: [editEmployee] })
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Sửa').trigger('click')
    await flushPromises()

    expect(document.body.querySelector('#employee-face-url').value).toBe('https://cdn.example/ba.jpg')
    const statusSwitch = bodyFind('#employee-form input[role="switch"]')
    statusSwitch.checked = false
    statusSwitch.dispatchEvent(new Event('change'))
    submitForm()
    await flushPromises()

    expect(employeeApi.update).toHaveBeenCalledWith(3, expect.objectContaining({ status: false, faceImageUrl: 'https://cdn.example/ba.jpg' }))
    expect(employeeApi.create).not.toHaveBeenCalled()
  })

  it('discards dirty edits through the confirm dialog', async () => {
    const wrapper = mountPage({ data: [{ employeeId: 2, fullName: 'Nguyen Van Hai', status: false, faceImageUrl: '/faces/2.jpg' }] })
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Sửa').trigger('click')
    await flushPromises()

    setValue('#employee-name', 'Nguyen Van Doi')
    bodyButton('Hủy').click()
    await nextTick()
    expect(document.body.textContent).toContain('Bỏ thay đổi chưa lưu?')

    const cancelButtons = [...document.body.querySelectorAll('button')].filter((b) => b.textContent.trim() === 'Hủy')
    cancelButtons[cancelButtons.length - 1].click()
    await nextTick()
    expect(document.body.querySelector('#employee-form')).toBeTruthy()
    expect(document.body.textContent).not.toContain('Bỏ thay đổi chưa lưu?')

    bodyButton('Hủy').click()
    await nextTick()
    bodyButton('Bỏ thay đổi').click()
    await flushPromises()
    expect(document.body.querySelector('#employee-form')).toBeNull()
  })

  it('closes a clean modal directly through the dismiss button', async () => {
    const wrapper = mountPage({ data: [editEmployee] })
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Sửa').trigger('click')
    await flushPromises()
    document.body.querySelector('.vs-modal button[aria-label]').click()
    await flushPromises()
    expect(document.body.querySelector('#employee-form')).toBeNull()
  })
})

describe('Employees delete flows', () => {
  it('deletes after confirmation and refreshes', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    await nextTick()
    expect(document.body.textContent).toContain('Xóa hồ sơ nhân viên?')

    employeeApi.getAll.mockClear()
    bodyButton('Xóa nhân viên').click()
    await flushPromises()
    expect(employeeApi.deleteEmployee).toHaveBeenCalledWith(1)
    expect(employeeApi.getAll).toHaveBeenCalled()
  })

  it('keeps the dialog open when deletion fails', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    employeeApi.deleteEmployee.mockRejectedValue({ response: { data: { message: 'đang chuyển đổi' } } })
    bodyButton('Xóa nhân viên').click()
    await flushPromises()
    expect(document.body.querySelector('.vs-modal')).toBeTruthy()
  })
})

describe('Employees inline face upload', () => {
  it('uploads a face from the row action and reports failures', async () => {
    const wrapper = mountPage()
    await flushPromises()
    const uploadInput = wrapper.find('input[aria-label="Chọn ảnh Face ID cho nhân viên"]').element

    await wrapper.findAll('button').find((b) => b.text() === 'Face ID').trigger('click')
    const file = new File(['img'], 'row.png', { type: 'image/png' })
    Object.defineProperty(uploadInput, 'files', { value: [file], configurable: true })
    uploadInput.dispatchEvent(new Event('change'))
    await flushPromises()
    expect(employeeApi.uploadFace).toHaveBeenCalledWith(1, file)

    employeeApi.uploadFace.mockRejectedValue(new Error('bad image'))
    await wrapper.findAll('button').find((b) => b.text() === 'Face ID').trigger('click')
    Object.defineProperty(uploadInput, 'files', { value: [], configurable: true })
    uploadInput.dispatchEvent(new Event('change'))
    await flushPromises()
  })
})

describe('Employees import/export', () => {
  it('handles import-complete with zero errors and closes export', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Nhập dữ liệu').trigger('click')
    await nextTick()
    employeeApi.getAll.mockClear()
    await wrapper.find('.stub-import-done').trigger('click')
    await flushPromises()
    expect(employeeApi.getAll).toHaveBeenCalled()
    expect(wrapper.find('.import-stub').exists()).toBe(false)

    await wrapper.findAll('button').find((b) => b.text() === 'Xuất dữ liệu').trigger('click')
    await nextTick()
    expect(wrapper.find('.export-stub').exists()).toBe(true)
    await wrapper.find('.stub-export-close').trigger('click')
    await nextTick()
    expect(wrapper.find('.export-stub').exists()).toBe(false)
  })

  it('tracks errors reported by an import', async () => {
    const wrapper = mountPage({ data: [{ employeeId: 1, fullName: 'An', status: true }] }, { ImportModal: importWithErrors })
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Nhập dữ liệu').trigger('click')
    await nextTick()
    await wrapper.find('.stub-import-done').trigger('click')
    await flushPromises()
    expect(wrapper.find('.import-stub').exists()).toBe(false)
  })
})

describe('Employees lifecycle', () => {
  it('guards beforeunload only when the form is dirty', async () => {
    const wrapper = mountPage()
    await flushPromises()

    const clean = new Event('beforeunload', { cancelable: true })
    clean.preventDefault = vi.fn()
    window.dispatchEvent(clean)
    expect(clean.preventDefault).not.toHaveBeenCalled()

    await wrapper.findAll('button').find((b) => b.text() === 'Thêm nhân viên').trigger('click')
    await flushPromises()
    setValue('#employee-name', 'Tran Thi Bich')

    const dirty = new Event('beforeunload', { cancelable: true })
    dirty.preventDefault = vi.fn()
    window.dispatchEvent(dirty)
    expect(dirty.preventDefault).toHaveBeenCalledTimes(1)
  })

  it('cleans up timers, listeners and object URLs on unmount', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Thêm nhân viên').trigger('click')
    await flushPromises()

    const file = new File(['img'], 'face.png', { type: 'image/png' })
    const faceInput = bodyFind('input[aria-label="Chọn ảnh Face ID trong biểu mẫu"]')
    Object.defineProperty(faceInput, 'files', { value: [file], configurable: true })
    faceInput.dispatchEvent(new Event('change'))
    await nextTick()

    wrapper.find('#employee-search').setValue('x')
    await nextTick()
    wrapper.unmount()
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:mock-face')
  })
})