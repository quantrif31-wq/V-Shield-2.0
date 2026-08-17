import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/gateTransitApi', () => ({
  getManualSubject: vi.fn(),
  scanGate: vi.fn(),
  scanGuest: vi.fn(),
  getManualGates: vi.fn(),
}))
vi.mock('../../services/employeeApi', () => ({ getProtectedFaceImage: vi.fn() }))

const gateTransitApi = await import('../../services/gateTransitApi')
const ManualParkingConsole = (await import('../ManualParkingConsole.vue')).default

beforeEach(() => vi.clearAllMocks())

describe('ManualParkingConsole', () => {
  it('loads the available gates', async () => {
    gateTransitApi.getManualGates.mockResolvedValue({ data: [{ gateId: 1, name: 'Cổng A' }] })
    const wrapper = mount(ManualParkingConsole)
    await flushPromises()
    expect(gateTransitApi.getManualGates).toHaveBeenCalled()
  })

  it('looks up a subject by code', async () => {
    gateTransitApi.getManualGates.mockResolvedValue({ data: [{ gateId: 1, name: 'Cổng A' }] })
    gateTransitApi.getManualSubject.mockResolvedValue({
      data: { subjectId: 5734, subjectType: 'employee', displayName: 'Nguyễn Văn An' },
    })
    const wrapper = mount(ManualParkingConsole)
    await flushPromises()

    await wrapper.find('select').setValue('1')
    await wrapper.find('.code-input').setValue('EMP:5734')
    await wrapper.findAll('button').find((b) => b.text().includes('Nhận dạng')).trigger('click')
    await flushPromises()
    expect(gateTransitApi.getManualSubject).toHaveBeenCalledWith('EMP:5734')
  })

  it('shows an error when the subject cannot be found', async () => {
    gateTransitApi.getManualGates.mockResolvedValue({ data: [{ gateId: 1, name: 'Cổng A' }] })
    gateTransitApi.getManualSubject.mockRejectedValue({ response: { data: { message: 'Không tìm thấy' } } })
    const wrapper = mount(ManualParkingConsole)
    await flushPromises()

    await wrapper.find('.code-input').setValue('XYZ')
    await wrapper.findAll('button').find((b) => b.text().includes('Nhận dạng')).trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Không tìm thấy')
  })
})
