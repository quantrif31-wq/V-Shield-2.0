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
