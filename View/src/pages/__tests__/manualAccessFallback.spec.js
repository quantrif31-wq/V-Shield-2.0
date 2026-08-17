import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/deviceManagementApi', () => ({ getGates: vi.fn() }))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn(), getProtectedFaceImage: vi.fn() }))
vi.mock('../../services/guestProfileApi', () => ({ getVisitorDirectory: vi.fn() }))
vi.mock('../../services/http', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn(), delete: vi.fn(), request: vi.fn() },
}))

const deviceManagementApi = await import('../../services/deviceManagementApi')
const employeeApi = await import('../../services/employeeApi')
const guestProfileApi = await import('../../services/guestProfileApi')
const http = (await import('../../services/http')).default
const ManualAccessFallback = (await import('../ManualAccessFallback.vue')).default

beforeEach(() => vi.clearAllMocks())

describe('ManualAccessFallback', () => {
  it('loads gates on mount and employees on search', async () => {
    deviceManagementApi.getGates.mockResolvedValue({ data: [{ gateId: 1, name: 'Cổng A' }] })
    employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'Nguyễn An' }] })
    guestProfileApi.getVisitorDirectory.mockResolvedValue({ data: { items: [] } })
    const wrapper = mount(ManualAccessFallback)
    await flushPromises()
    expect(deviceManagementApi.getGates).toHaveBeenCalled()

    await wrapper.findAll('.search-box input')[0].setValue('An')
    await flushPromises()
    expect(employeeApi.getAll).toHaveBeenCalledWith(expect.objectContaining({ name: 'An' }))
  })

  it('submits an allow decision for a subject', async () => {
    deviceManagementApi.getGates.mockResolvedValue({ data: [{ gateId: 1, name: 'Cổng A' }] })
    employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'Nguyễn An' }] })
    guestProfileApi.getVisitorDirectory.mockResolvedValue({ data: { items: [] } })
    http.post.mockResolvedValue({ data: {} })
    const wrapper = mount(ManualAccessFallback)
    await flushPromises()

    await wrapper.findAll('select')[0].setValue('1')
    await wrapper.findAll('.search-box input')[0].setValue('An')
    await flushPromises()
    await wrapper.find('.dropdown-item').trigger('click')
    await wrapper.find('.maf-btn-allow').trigger('click')
    await flushPromises()
    expect(http.post).toHaveBeenCalledWith('/QrAccess/manual-access', expect.objectContaining({ gateId: 1, employeeId: 7 }))
  })
})
