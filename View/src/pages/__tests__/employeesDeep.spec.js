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
  getDepartments: vi.fn(),
  getPositions: vi.fn(),
}))
vi.mock('../../services/statisticsApi', () => ({ getSummary: vi.fn() }))

hoisted.route = reactive({ query: {} })

const employeeApi = await import('../../services/employeeApi')
const lookupApi = await import('../../services/lookupApi')
const statisticsApi = await import('../../services/statisticsApi')
const Employees = (await import('../Employees.vue')).default

const sharedStubs = {
  RouterLink: { template: '<a><slot /></a>' },
  ImportModal: { name: 'ImportModal', props: ['entityType', 'entityDisplayName'], emits: ['close', 'import-complete'], template: '<div class="import-stub">import-stub</div>' },
  ExportModal: { name: 'ExportModal', props: ['entityType', 'entityDisplayName', 'availableColumns'], emits: ['close'], template: '<div class="export-stub">export-stub</div>' },
}

let wrappers = []

function mountPage(data, stubs = {}) {
  if (data !== undefined) employeeApi.getAll.mockResolvedValue(data)
  const wrapper = mount(Employees, { global: { stubs: { ...sharedStubs, ...stubs } } })
  wrappers.push(wrapper)
  return wrapper
}

function makeEmployees(count, extra = {}) {
  return Array.from({ length: count }, (_, idx) => {
    const i = idx + 1
    return {
      employeeId: i,
      fullName: i === 4 ? '' : `Nguyen Van ${i}`,
      phone: i % 2 ? '0900000001' : '',
      email: i % 2 ? `e${i}@x.com` : '',
      departmentName: i % 2 ? 'An ninh' : '',
      positionName: i % 2 ? 'Bao ve' : '',
      status: i % 2 === 0,
      faceImageUrl: i === 3 ? '/faces/3.jpg' : i === 5 ? 'https://cdn.example/5.jpg' : '',
      ...extra[i],
    }
  })
}

beforeEach(() => {
  vi.clearAllMocks()
  hoisted.route.query = {}
  employeeApi.getAll.mockResolvedValue({ data: makeEmployees(1) })
  employeeApi.getProtectedFaceImage.mockResolvedValue({ data: new Blob(['x']) })
  employeeApi.create.mockResolvedValue({ data: { employeeId: 99 } })
  employeeApi.update.mockResolvedValue({ data: {} })
  employeeApi.deleteEmployee.mockResolvedValue({ data: {} })
  employeeApi.uploadFace.mockResolvedValue({ data: {} })
  lookupApi.getDepartments.mockResolvedValue({ data: [{ departmentId: 1, name: 'An Ninh' }] })
  lookupApi.getPositions.mockResolvedValue({ data: [{ positionId: 1, name: 'Bao ve' }] })
  statisticsApi.getSummary.mockResolvedValue({ totalEmployees: 3, activeEmployees: 2, inactiveEmployees: 1 })
})

URL.createObjectURL = vi.fn(() => 'blob:mock-face')
URL.revokeObjectURL = vi.fn()

afterEach(() => {
  wrappers.forEach((w) => w.unmount())
  wrappers = []
  document.body.innerHTML = ''
})

describe('Employees data & layout', () => {
  it('renders rows with avatars, protected images and summary', async () => {
    const wrapper = mountPage()
    await flushPromises()
    expect(wrapper.text()).toContain('Nguyen Van 1')
    expect(wrapper.find('.summary-grid').text()).toContain('3')

    employeeApi.getAll.mockResolvedValue({ data: makeEmployees(5) })
    employeeApi.getProtectedFaceImage.mockClear()
    const wrapper2 = mount(Employees, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(employeeApi.getProtectedFaceImage).toHaveBeenCalledWith(3)

    const blobImg = wrapper2.find('img[src="blob:mock-face"]')
    expect(blobImg.exists()).toBe(true)
    expect(wrapper2.find('img[src="https://cdn.example/5.jpg"]').exists()).toBe(true)
    expect(wrapper2.text()).toContain('?')

    await blobImg.trigger('error')
    await nextTick()
    expect(wrapper2.find('img[src="blob:mock-face"]').exists()).toBe(false)
  })

  it('falls back to local counts when the summary endpoint fails', async () => {
    statisticsApi.getSummary.mockRejectedValue(new Error('boom'))
    const wrapper = mountPage()
    await flushPromises()
    expect(wrapper.find('.summary-grid').text()).toContain('1')
  })

  it('skips protected face hydration when the image lookup fails', async () => {
    employeeApi.getAll.mockResolvedValue({ data: makeEmployees(4) })
    employeeApi.getProtectedFaceImage.mockRejectedValue(new Error('not found'))
    const wrapper = mountPage()
    await flushPromises()
    expect(wrapper.find('.avatar-fallback').exists()).toBe(true)
  })

  it('shows permission denied then generic load error and recovers via retry', async () => {
    employeeApi.getAll.mockRejectedValue({ response: { status: 403 } })
    const wrapper = mountPage()
    await flushPromises()
    expect(wrapper.text()).toContain('quyền')

    employeeApi.getAll.mockRejectedValue({ response: { data: { message: 'máy chủ lỗi' } } })
    await wrapper.findAll('button').find((b) => b.text() === 'Làm mới').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('máy chủ lỗi')

    employeeApi.getAll.mockRejectedValue(new Error('nope'))
    await wrapper.findAll('button').find((b) => b.text() === 'Làm mới').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Không thể tải danh sách nhân viên.')

    employeeApi.getAll.mockResolvedValue({ data: makeEmployees(1) })
    await wrapper.findAll('button').find((b) => b.text().includes('Thử lại')).trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Nguyen Van 1')
  })

  it('renders the empty state with a create shortcut', async () => {
    employeeApi.getAll.mockResolvedValue({ data: [] })
    const wrapper = mountPage()
    await flushPromises()
    expect(wrapper.text()).toContain('Không có nhân viên phù hợp')
    await wrapper.findAll('button').find((b) => b.text() === 'Thêm nhân viên').trigger('click')
    await flushPromises()
    expect(document.body.querySelector('#employee-form')).toBeTruthy()
  })
})

describe('Employees route query handling', () => {
  it('applies search/status/sort/page from the route', async () => {
    hoisted.route.query = { search: 'nguyen', status: 'true', page: 1, sort: 'status', direction: 'desc' }
    const wrapper = mountPage()
    await flushPromises()
    expect(employeeApi.getAll).toHaveBeenCalledWith(expect.objectContaining({ search: 'nguyen', status: true }))
    expect(wrapper.find('#employee-search').element.value).toBe('nguyen')
    expect(wrapper.find('#employee-status').element.value).toBe('true')
  })

  it('reacts to route.query changes through the watcher', async () => {
    const wrapper = mountPage()
    await flushPromises()
    employeeApi.getAll.mockClear()
    hoisted.route.query = { status: 'false' }
    await flushPromises()
    await new Promise((r) => setTimeout(r, 0))
    const ctx = { calls: employeeApi.getAll.mock.calls.map((c) => c[0]) }
    expect(employeeApi.getAll).toHaveBeenCalledWith(expect.objectContaining({ status: false }))
    expect(employeeApi.getAll.mock.calls.length).toBeGreaterThanOrEqual(1)
    expect(wrapper.find('#employee-status').element.value).toBe('false')
  })

  it('sorts by fullName and status in both directions', async () => {
    const wrapper = mountPage()
    await flushPromises()

    await wrapper.find('thead button').trigger('click')
    expect(hoisted.router.replace).toHaveBeenCalledWith({ query: expect.objectContaining({ direction: 'desc', sort: undefined }) })

    hoisted.route.query = { sort: 'status', direction: 'desc' }
    await flushPromises()
    const statusHeader = wrapper.findAll('thead button').find((h) => h.text().includes('Trạng thái'))
    await statusHeader.trigger('click')
    expect(hoisted.router.replace).toHaveBeenLastCalledWith({ query: expect.objectContaining({ sort: 'status', direction: undefined }) })
  })

  it('paginates and clamps the page when data shrinks', async () => {
    hoisted.route.query = { page: 5 }
    employeeApi.getAll.mockResolvedValue({ data: makeEmployees(12) })
    const wrapper = mountPage()
    await flushPromises()
    expect(hoisted.router.replace).toHaveBeenCalledWith({ query: expect.objectContaining({ page: 2 }) })

    hoisted.route.query = { page: 2 }
    await flushPromises()
    expect(wrapper.text()).toContain('11–12')

    await wrapper.findAll('button').find((b) => b.text() === 'Trang trước').trigger('click')
    expect(hoisted.router.replace).toHaveBeenLastCalledWith({ query: expect.objectContaining({ page: undefined }) })
  })

  it('debounces search commits and clears filters', async () => {
    const wrapper = mountPage()
    await flushPromises()

    vi.useFakeTimers()
    await wrapper.find('#employee-search').setValue('An')
    expect(hoisted.router.replace).not.toHaveBeenCalled()
    vi.advanceTimersByTime(400)
    await Promise.resolve()
    vi.useRealTimers()
    await flushPromises()

    expect(hoisted.router.replace).toHaveBeenCalledWith({ query: expect.objectContaining({ search: 'An', page: undefined }) })

    await wrapper.findAll('button').find((b) => b.text().includes('Xóa bộ lọc')).trigger('click')
    expect(hoisted.router.replace).toHaveBeenLastCalledWith({ query: expect.objectContaining({ search: undefined, status: undefined }) })
  })
})