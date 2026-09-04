import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/deviceManagementApi', () => ({ getGates: vi.fn() }))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn(), getProtectedFaceImage: vi.fn() }))
vi.mock('../../services/guestProfileApi', () => ({ getVisitorDirectory: vi.fn() }))
vi.mock('../../services/gateTransitApi', () => ({ getManualSubject: vi.fn() }))
vi.mock('../../services/http', () => ({ default: { post: vi.fn() } }))

const devices = await import('../../services/deviceManagementApi')
const employees = await import('../../services/employeeApi')
const visitors = await import('../../services/guestProfileApi')
const gateTransitApi = await import('../../services/gateTransitApi')
const http = (await import('../../services/http')).default
const ManualAccessFallback = (await import('../ManualAccessFallback.vue')).default

beforeEach(() => vi.clearAllMocks())

async function mountPage() {
  devices.getGates.mockResolvedValue({ data: [{ gateId: 1, gateName: 'Cổng A' }] })
  const wrapper = mount(ManualAccessFallback)
  await flushPromises()
  return wrapper
}

describe('ManualAccessFallback', () => {
  it('uses one lookup box to search employees and approved visitors', async () => {
    employees.getAll.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'Nguyễn An' }] })
    visitors.getVisitorDirectory.mockResolvedValue({ data: { items: [{ visitorDetailId: 9, fullName: 'Khách An' }] } })
    const wrapper = await mountPage()

    await wrapper.find('.search-box input').setValue('An')
    await flushPromises()

    expect(employees.getAll).toHaveBeenCalledWith({ name: 'An', pageSize: 10 })
    expect(visitors.getVisitorDirectory).toHaveBeenCalledWith({ query: 'An', pageSize: 10, registrationStatus: 'Approved' })
    expect(wrapper.findAll('.dropdown-item')).toHaveLength(2)
    expect(wrapper.text()).toContain('Nhân viên')
    expect(wrapper.text()).toContain('Khách')
  })

  it('does not query either directory for a one-character name', async () => {
    const wrapper = await mountPage()
    await wrapper.find('.search-box input').setValue('A')
    await flushPromises()
    expect(employees.getAll).not.toHaveBeenCalled()
    expect(visitors.getVisitorDirectory).not.toHaveBeenCalled()
  })

  it('resolves a one-digit employee code directly', async () => {
    gateTransitApi.getManualSubject.mockResolvedValue({
      data: { success: true, data: { subjectType: 'employee', subjectId: 1, fullName: 'Nhân viên Một' } },
    })
    const wrapper = await mountPage()
    await wrapper.find('.search-box input').setValue('1')
    await flushPromises()

    expect(gateTransitApi.getManualSubject).toHaveBeenCalledWith('1')
    expect(wrapper.findAll('.dropdown-item')).toHaveLength(1)
    expect(wrapper.text()).toContain('Nhân viên Một')
  })

  it('selects an employee without a manual type switch', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.pickSubject({
      subjectType: 'employee', id: 7, fullName: 'Nguyễn An', detail: 'Mã NV: 7',
      raw: { employeeId: 7, fullName: 'Nguyễn An', department: 'Vận hành' },
    })
    expect(wrapper.vm.subject.subjectType).toBe('employee')
    expect(wrapper.vm.idLabel).toBe('Mã NV')
    expect(wrapper.find('.maf-photo-card').exists()).toBe(true)
  })

  it('selects a visitor without a manual type switch', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.pickSubject({
      subjectType: 'visitor', id: 9, fullName: 'Khách A', detail: 'Khách',
      raw: { visitorDetailId: 9, fullName: 'Khách A', guestPhone: '090', hostEmployeeName: 'Host' },
    })
    expect(wrapper.vm.subject.subjectType).toBe('visitor')
    expect(wrapper.vm.idLabel).toBe('Mã KH')
    expect(wrapper.vm.extraInfo).toContain('SĐT')
  })

  it('submits an allow decision using the identified employee id', async () => {
    http.post.mockResolvedValue({ data: {} })
    const wrapper = await mountPage()
    wrapper.vm.gateId = '1'
    await wrapper.vm.pickSubject({ subjectType: 'employee', id: 7, fullName: 'Nguyễn An', detail: '', raw: { employeeId: 7, fullName: 'Nguyễn An' } })
    await wrapper.vm.submitDecision(true)
    expect(http.post).toHaveBeenCalledWith('/QrAccess/manual-access', expect.objectContaining({ gateId: 1, employeeId: 7, isDenied: false }))
  })

  it('submits an allow decision using the identified visitor id', async () => {
    http.post.mockResolvedValue({ data: {} })
    const wrapper = await mountPage()
    wrapper.vm.gateId = '1'
    await wrapper.vm.pickSubject({ subjectType: 'visitor', id: 9, fullName: 'Khách A', detail: '', raw: { visitorDetailId: 9, fullName: 'Khách A' } })
    await wrapper.vm.submitDecision(true)
    expect(http.post).toHaveBeenCalledWith('/QrAccess/manual-access', expect.objectContaining({ gateId: 1, visitorDetailId: 9, isDenied: false }))
  })
})
