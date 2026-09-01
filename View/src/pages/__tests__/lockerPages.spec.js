import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ route: { query: {} } }))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route }))

vi.mock('../../services/enterpriseSecurityApi', () => ({
  lostFoundApi: {
    getLockerCabinets: vi.fn(),
    getLockerCabinetDetail: vi.fn(),
    createLockerCabinet: vi.fn(),
    updateLockerCabinet: vi.fn(),
    deleteLockerCabinet: vi.fn(),
    createCompartments: vi.fn(),
    releaseCompartment: vi.fn(),
    getLockerAccessLogs: vi.fn(),
  },
}))

const lostFoundApi = (await import('../../services/enterpriseSecurityApi')).lostFoundApi

const LockerManager = (await import('../LockerManager.vue')).default
const LockerAccessLogs = (await import('../LockerAccessLogs.vue')).default

const routerLinkStub = { template: '<a><slot /></a>' }

beforeEach(() => vi.clearAllMocks())

describe('LockerManager', () => {
  it('loads cabinets with their compartments', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [{ lockerCabinetId: 1, name: 'Tủ A', location: 'Sảnh' }] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({
      data: { compartments: [
        { lockerCompartmentId: 11, lockerCabinetId: 1, code: 'A1', status: 'Empty' },
        { lockerCompartmentId: 12, lockerCabinetId: 1, code: 'A2', status: 'Occupied' },
      ] },
    })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    expect(wrapper.text()).toContain('Tủ A')
    expect(wrapper.text()).toContain('A1')
    expect(wrapper.text()).toContain('Có đồ')
  })

  it('deletes a cabinet after confirmation', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [{ lockerCabinetId: 1, name: 'Tủ A' }] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    lostFoundApi.deleteLockerCabinet.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa tủ').trigger('click')
    await flushPromises()
    expect(lostFoundApi.deleteLockerCabinet).toHaveBeenCalledWith(1)
    confirmSpy.mockRestore()
  })

  it('skips cabinet deletion when confirmation is cancelled', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [{ lockerCabinetId: 1, name: 'Tủ A' }] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false)
    await wrapper.vm.deleteCabinet({ lockerCabinetId: 1, name: 'Tủ A' })
    await flushPromises()
    expect(lostFoundApi.deleteLockerCabinet).not.toHaveBeenCalled()
    confirmSpy.mockRestore()
  })

  it('handles cabinet deletion error', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [{ lockerCabinetId: 1, name: 'Tủ A' }] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    lostFoundApi.deleteLockerCabinet.mockRejectedValue({ response: { data: { message: 'del' } } })
    await wrapper.vm.deleteCabinet({ lockerCabinetId: 1, name: 'Tủ A' })
    await flushPromises()
    expect(alertSpy).toHaveBeenCalledWith(expect.stringContaining('del'))
    confirmSpy.mockRestore()
    alertSpy.mockRestore()
  })

  it('opens create cabinet and submits it', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    wrapper.vm.openCreateCabinet()
    expect(wrapper.vm.showCabinetForm).toBe(true)
    expect(wrapper.vm.editingCabinet).toBeNull()
    wrapper.vm.cabinetForm.name = 'Tủ mới'
    lostFoundApi.createLockerCabinet.mockResolvedValue({})
    await wrapper.vm.submitCabinet()
    await flushPromises()
    expect(lostFoundApi.createLockerCabinet).toHaveBeenCalled()
    expect(wrapper.vm.showCabinetForm).toBe(false)
  })

  it('opens edit cabinet and submits update', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    wrapper.vm.openEditCabinet({ lockerCabinetId: 9, name: 'Tủ B', location: '', description: 'd' })
    expect(wrapper.vm.editingCabinet.lockerCabinetId).toBe(9)
    lostFoundApi.updateLockerCabinet.mockResolvedValue({})
    await wrapper.vm.submitCabinet()
    await flushPromises()
    expect(lostFoundApi.updateLockerCabinet).toHaveBeenCalledWith(9, expect.objectContaining({ name: 'Tủ B', description: 'd' }))
  })

  it('validates cabinet name before submit', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    wrapper.vm.openCreateCabinet()
    await wrapper.vm.submitCabinet()
    await flushPromises()
    expect(lostFoundApi.createLockerCabinet).not.toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('handles cabinet submit error', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    wrapper.vm.openCreateCabinet()
    wrapper.vm.cabinetForm.name = 'Tủ lỗi'
    lostFoundApi.createLockerCabinet.mockRejectedValue({ response: { data: { message: 'createfail' } } })
    await wrapper.vm.submitCabinet()
    await flushPromises()
    expect(alertSpy).toHaveBeenCalledWith(expect.stringContaining('createfail'))
    alertSpy.mockRestore()
  })

  it('submits new compartments', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    wrapper.vm.showAddCompartments({ lockerCabinetId: 3 })
    expect(wrapper.vm.showCompartmentForm).toBe(true)
    wrapper.vm.compartmentCodes = 'A1, A2'
    lostFoundApi.createCompartments.mockResolvedValue({})
    await wrapper.vm.submitCompartments()
    await flushPromises()
    expect(lostFoundApi.createCompartments).toHaveBeenCalledWith(3, { codes: ['A1', 'A2'] })
    expect(wrapper.vm.showCompartmentForm).toBe(false)
  })

  it('rejects empty compartment codes', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    wrapper.vm.showAddCompartments({ lockerCabinetId: 3 })
    wrapper.vm.compartmentCodes = ',,'
    await wrapper.vm.submitCompartments()
    await flushPromises()
    expect(lostFoundApi.createCompartments).not.toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('selects a compartment and releases it', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    wrapper.vm.selectCompartment({ lockerCompartmentId: 7, code: 'A1' })
    expect(wrapper.vm.selectedComp.lockerCompartmentId).toBe(7)
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    lostFoundApi.releaseCompartment.mockResolvedValue({})
    await wrapper.vm.releaseCompartment({ lockerCompartmentId: 7, code: 'A1' })
    await flushPromises()
    expect(lostFoundApi.releaseCompartment).toHaveBeenCalledWith(7)
    expect(wrapper.vm.selectedComp).toBeNull()
    confirmSpy.mockRestore()
  })

  it('skips release when confirmation is cancelled', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false)
    await wrapper.vm.releaseCompartment({ lockerCompartmentId: 7, code: 'A1' })
    await flushPromises()
    expect(lostFoundApi.releaseCompartment).not.toHaveBeenCalled()
    confirmSpy.mockRestore()
  })

  it('handles release error', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    lostFoundApi.releaseCompartment.mockRejectedValue({ response: { data: { message: 'relfail' } } })
    await wrapper.vm.releaseCompartment({ lockerCompartmentId: 7, code: 'A1' })
    await flushPromises()
    expect(alertSpy).toHaveBeenCalledWith(expect.stringContaining('relfail'))
    confirmSpy.mockRestore()
    alertSpy.mockRestore()
  })

  it('handles load error gracefully', async () => {
    lostFoundApi.getLockerCabinets.mockRejectedValue({})
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    expect(wrapper.vm.cabinets).toEqual([])
    expect(wrapper.vm.loading).toBe(false)
  })

  it('filters compartments by cabinet', async () => {
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    lostFoundApi.getLockerCabinetDetail.mockResolvedValue({ data: { compartments: [] } })
    const wrapper = mount(LockerManager, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    wrapper.vm.compartments = [
      { lockerCabinetId: 1, code: 'A' },
      { lockerCabinetId: 2, code: 'B' },
    ]
    expect(wrapper.vm.compartmentsByCabinet(1)).toEqual([{ lockerCabinetId: 1, code: 'A' }])
  })
})

describe('LockerAccessLogs', () => {
  it('loads access logs and renders actions', async () => {
    lostFoundApi.getLockerAccessLogs.mockResolvedValue({
      data: [{ lockerAccessLogId: 1, accessedAtUtc: '2026-08-01T00:00:00Z', lockerCompartmentId: 11, action: 'Assign', accessedByUserId: 'u1' }],
    })
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    const wrapper = mount(LockerAccessLogs, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    expect(wrapper.text()).toContain('Gán')
    expect(wrapper.text()).toContain('u1')
  })

  it('respects a compartment filter from the route query', async () => {
    hoisted.route.query = { compartmentId: '11' }
    lostFoundApi.getLockerAccessLogs.mockResolvedValue({ data: [] })
    lostFoundApi.getLockerCabinets.mockResolvedValue({ data: [] })
    const wrapper = mount(LockerAccessLogs, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    expect(lostFoundApi.getLockerAccessLogs).toHaveBeenCalledWith({ limit: 200, compartmentId: 11 })
  })
})
