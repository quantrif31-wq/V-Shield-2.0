import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ authState: { user: { employeeId: 5 } } }))

vi.mock('../../stores/auth', () => ({ authState: hoisted.authState }))
vi.mock('../../services/vehicleApi', () => ({ getByEmployeeId: vi.fn() }))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn() }))
vi.mock('../../services/vehicleDelegationApi', () => ({
  createDelegation: vi.fn(),
  getOutgoing: vi.fn(),
  getIncoming: vi.fn(),
  approveDelegation: vi.fn(),
  rejectDelegation: vi.fn(),
  revokeDelegation: vi.fn(),
}))

const vehicleApi = await import('../../services/vehicleApi')
const employeeApi = await import('../../services/employeeApi')
const delegationApi = await import('../../services/vehicleDelegationApi')
const VehicleTransfer = (await import('../VehicleTransfer.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  vi.useFakeTimers()
})
afterEach(() => {
  vi.runOnlyPendingTimers()
  vi.useRealTimers()
})

describe('VehicleTransfer', () => {
  it('loads incoming and outgoing delegations', async () => {
    vehicleApi.getByEmployeeId.mockResolvedValue({ data: [{ vehicleId: 1, licensePlate: '29A-1', parkingStatus: 'IN' }] })
    delegationApi.getIncoming.mockResolvedValue({ data: [{ vehicleDelegationId: 2, fromEmployeeName: 'A', licensePlate: '29A-2', status: 'Pending' }] })
    delegationApi.getOutgoing.mockResolvedValue({ data: [] })
    const wrapper = mount(VehicleTransfer)
    await flushPromises()

    await wrapper.find('[data-tab="incoming"]').trigger('click')
    await flushPromises()
    expect(wrapper.find('.request-list').text()).toContain('A')
    expect(wrapper.find('.request-list').text()).toContain('29A-2')
  })

  it('creates a delegation with a selected vehicle and employee', async () => {
    vehicleApi.getByEmployeeId.mockResolvedValue({ data: [{ vehicleId: 1, licensePlate: '29A-1', parkingStatus: 'IN' }] })
    delegationApi.getIncoming.mockResolvedValue({ data: [] })
    delegationApi.getOutgoing.mockResolvedValue({ data: [] })
    employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 9, fullName: 'Nguyễn B' }] })
    const wrapper = mount(VehicleTransfer)
    await flushPromises()

    await wrapper.find('select').setValue('1')
    await wrapper.find('input[type="text"]').trigger('focus')
    await wrapper.find('input[type="text"]').setValue('Nguyễn B')
    await flushPromises()
    vi.advanceTimersByTime(300)
    await flushPromises()
    await wrapper.find('.employee-option').trigger('mousedown')

    delegationApi.createDelegation.mockResolvedValue({})
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(delegationApi.createDelegation).toHaveBeenCalledWith(expect.objectContaining({ vehicleId: 1, toEmployeeId: 9 }))
  })

  it('approves an incoming delegation', async () => {
    vehicleApi.getByEmployeeId.mockResolvedValue({ data: [] })
    delegationApi.getIncoming.mockResolvedValue({ data: [{ vehicleDelegationId: 2, fromEmployeeName: 'A', licensePlate: '29A-2', status: 'Pending' }] })
    delegationApi.getOutgoing.mockResolvedValue({ data: [] })
    const wrapper = mount(VehicleTransfer)
    await flushPromises()
    await wrapper.find('[data-tab="incoming"]').trigger('click')
    await flushPromises()

    delegationApi.approveDelegation.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Đồng ý').trigger('click')
    await flushPromises()
    expect(delegationApi.approveDelegation).toHaveBeenCalledWith(2)
  })
})
