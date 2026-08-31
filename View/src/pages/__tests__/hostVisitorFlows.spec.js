import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getVisits: vi.fn(),
    createVisit: vi.fn(),
    getSites: vi.fn(),
    getFormTemplates: vi.fn(),
    issueVisitorCredential: vi.fn(),
    getParkingAreas: vi.fn(),
    createParkingPermit: vi.fn(),
    acceptForm: vi.fn(),
  },
}))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn() }))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const HostVisitorPage = (await import('../HostVisitorPage.vue')).default

const alertMock = vi.fn()
const consoleErrorMock = vi.fn()

beforeEach(() => {
  vi.clearAllMocks()
  alertMock.mockClear()
  consoleErrorMock.mockClear()
  window.alert = alertMock
  console.error = consoleErrorMock
  sessionStorage.setItem('v_shield_user', JSON.stringify({ employeeId: 5, fullName: 'Host A' }))
  enterpriseApi.getVisits.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getFormTemplates.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getParkingAreas.mockResolvedValue({ data: { items: [] } })
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('HostVisitorPage flows', () => {
  it('filters invitations by search query and renders table', async () => {
    enterpriseApi.getVisits.mockResolvedValue({
      data: { items: [
        { visitId: 1, visitorName: 'Anh Tuấn', visitorPhone: '091', status: 'CheckedIn', expectedInUtc: '2026-08-10T00:00:00Z' },
        { visitId: 2, visitorName: 'Chị Lan', status: 'Overstay' },
      ] },
    })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    expect(wrapper.vm.filteredInvitations.length).toBe(2)
    wrapper.vm.searchQuery = 'tuấn'
    expect(wrapper.vm.filteredInvitations.length).toBe(1)
    expect(wrapper.text()).toContain('Đỗ xe')
  })

  it('maps status classes and formats dates', () => {
    const wrapper = mount(HostVisitorPage)
    expect(wrapper.vm.statusClass('CheckedIn')).toBe('success')
    expect(wrapper.vm.statusClass('Overstay')).toBe('danger')
    expect(wrapper.vm.statusClass('Approved')).toBe('info')
    expect(wrapper.vm.statusClass('Invited')).toBe('')
    expect(wrapper.vm.formatDate(null)).toBe('—')
    expect(wrapper.vm.formatDate('2026-08-10T00:00:00Z')).toBeTruthy()
  })

  it('requires a guest name on submit', async () => {
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    wrapper.vm.showForm = true
    await wrapper.vm.submitInvitation()
    await flushPromises()
    expect(wrapper.vm.formError).toBe('Tên khách là bắt buộc.')
    expect(enterpriseApi.createVisit).not.toHaveBeenCalled()
  })

  it('requires expected times on submit', async () => {
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    wrapper.vm.form = { ...wrapper.vm.form, name: 'Khách', expectedIn: '', expectedOut: '' }
    await wrapper.vm.submitInvitation()
    await flushPromises()
    expect(wrapper.vm.formError).toBe('Thời gian dự kiến là bắt buộc.')
  })

  it('submits an invitation with parking and NDA', async () => {
    enterpriseApi.createVisit.mockResolvedValue({ data: { visitId: 99 } })
    enterpriseApi.createParkingPermit.mockResolvedValue({})
    enterpriseApi.acceptForm.mockResolvedValue({})
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    wrapper.vm.form = {
      name: 'Khách Mới', phone: '090', email: 'k@x.com',
      expectedIn: '2026-08-10T09:00', expectedOut: '2026-08-10T17:00',
      siteId: 3, ndaRequired: true, escortRequired: true, safetyBriefingRequired: true,
      parkingRequired: true, plateNumber: '29A-123',
    }
    wrapper.vm.formExtra = { selectedNdaTemplateId: 7, ndaTemplate: true }
    await wrapper.vm.submitInvitation()
    await flushPromises()
    expect(enterpriseApi.createVisit).toHaveBeenCalledWith(expect.objectContaining({
      visitorName: 'Khách Mới', hostEmployeeId: 5, siteId: 3, ndaRequired: true, escortRequired: true,
    }))
    expect(enterpriseApi.createParkingPermit).toHaveBeenCalledWith(expect.objectContaining({ visitId: 99, plateNumber: '29A-123' }))
    expect(enterpriseApi.acceptForm).toHaveBeenCalledWith(99, { formTemplateId: 7 })
    expect(wrapper.vm.formSuccess).toContain('Khách Mới')
    expect(wrapper.vm.showForm).toBe(false)
  })

  it('surfaces an invitation submit error', async () => {
    enterpriseApi.createVisit.mockRejectedValue({ response: { data: { message: 'Máy chủ lỗi' } } })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    wrapper.vm.form = { ...wrapper.vm.form, name: 'Khách', expectedIn: '2026-08-10T09:00', expectedOut: '2026-08-10T17:00' }
    await wrapper.vm.submitInvitation()
    await flushPromises()
    expect(wrapper.vm.formError).toBe('Máy chủ lỗi')
  })

  it('shows a guest detail alert', () => {
    const wrapper = mount(HostVisitorPage)
    wrapper.vm.viewDetail({ visitorName: 'Khách A', status: 'Approved', expectedInUtc: '2026-08-10T00:00:00Z', expectedOutUtc: '2026-08-10T00:00:00Z' })
    expect(window.alert).toHaveBeenCalledWith(expect.stringContaining('Khách A'))
  })

  it('issues a QR credential with defaults', async () => {
    enterpriseApi.issueVisitorCredential.mockResolvedValue({ data: { credentialReference: 'REF-1' } })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    wrapper.vm.issueCredential({ visitId: 10, visitorName: 'Khách B' })
    expect(wrapper.vm.credentialVisit.visitId).toBe(10)
    await wrapper.vm.submitCredential()
    await flushPromises()
    expect(enterpriseApi.issueVisitorCredential).toHaveBeenCalledWith(10, expect.objectContaining({ credentialType: 'QR' }))
    expect(wrapper.vm.credSuccess).toBe('REF-1')
  })

  it('issues credential with explicit time range', async () => {
    enterpriseApi.issueVisitorCredential.mockResolvedValue({})
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    wrapper.vm.issueCredential({ visitId: 11, visitorName: 'Khách C' })
    wrapper.vm.credFrom = '2026-08-10T08:00'
    wrapper.vm.credTo = '2026-08-12T08:00'
    await wrapper.vm.submitCredential()
    await flushPromises()
    expect(enterpriseApi.issueVisitorCredential).toHaveBeenCalledWith(11, expect.objectContaining({
      validFromUtc: expect.stringContaining('2026-08-10T'),
      validToUtc: expect.stringContaining('2026-08-12T'),
    }))
  })

  it('alerts on credential failure', async () => {
    enterpriseApi.issueVisitorCredential.mockRejectedValue({ response: { data: { message: 'không hợp lệ' } } })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    wrapper.vm.issueCredential({ visitId: 12, visitorName: 'Khách D' })
    await wrapper.vm.submitCredential()
    await flushPromises()
    expect(window.alert).toHaveBeenCalledWith(expect.stringContaining('không hợp lệ'))
  })

  it('does nothing on submitCredential without a target', async () => {
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    await wrapper.vm.submitCredential()
    expect(enterpriseApi.issueVisitorCredential).not.toHaveBeenCalled()
  })

  it('opens the parking form and loads parking areas', async () => {
    enterpriseApi.getParkingAreas.mockResolvedValue({ data: { items: [{ parkingAreaId: 1, name: 'Bãi A' }] } })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    await wrapper.vm.openParkingForm({ visitId: 20, visitorName: 'Khách E' })
    await flushPromises()
    expect(wrapper.vm.parkingTarget.visitId).toBe(20)
    expect(wrapper.vm.parkingAreas.length).toBe(1)
  })

  it('submits a parking permit', async () => {
    enterpriseApi.createParkingPermit.mockResolvedValue({})
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    await wrapper.vm.openParkingForm({ visitId: 21, visitorName: 'Khách F' })
    wrapper.vm.parkingForm = { areaId: 1, from: '2026-08-10T09:00', to: '2026-08-10T17:00', plate: '29A-455' }
    await wrapper.vm.submitParking()
    await flushPromises()
    expect(enterpriseApi.createParkingPermit).toHaveBeenCalledWith(expect.objectContaining({ visitId: 21, parkingAreaId: 1, plateNumber: '29A-455' }))
    expect(wrapper.vm.parkingDone).toContain('Đã cấp')
  })

  it('surfaces a parking submit error', async () => {
    enterpriseApi.createParkingPermit.mockRejectedValue({ message: 'hết chỗ' })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    await wrapper.vm.openParkingForm({ visitId: 22, visitorName: 'Khách G' })
    wrapper.vm.parkingForm = { areaId: 1, from: '', to: '', plate: '' }
    await wrapper.vm.submitParking()
    await flushPromises()
    expect(wrapper.vm.parkingError).toBe('hết chỗ')
  })

  it('does nothing on submitParking without area', async () => {
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    await wrapper.vm.openParkingForm({ visitId: 23, visitorName: 'Khách H' })
    wrapper.vm.parkingForm = { areaId: null, from: '', to: '', plate: '' }
    await wrapper.vm.submitParking()
    expect(enterpriseApi.createParkingPermit).not.toHaveBeenCalled()
  })

  it('logs errors when loading parking areas fails', async () => {
    enterpriseApi.getParkingAreas.mockRejectedValue(new Error('nope'))
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    await wrapper.vm.openParkingForm({ visitId: 24, visitorName: 'Khách I' })
    await flushPromises()
    expect(wrapper.vm.parkingAreas.length).toBe(0)
  })

  it('renders form templates modal and empty state', async () => {
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    wrapper.vm.showFormTemplates = true
    await flushPromises()
    expect(wrapper.find('.modal-overlay').exists() || document.body.textContent.includes('Chưa có biểu mẫu')).toBe(true)
  })

  it('renders template cards when templates exist', async () => {
    enterpriseApi.getFormTemplates.mockResolvedValue({ data: { items: [{ formTemplateId: 3, templateName: 'NDA mấu', description: 'pháp lý' }] } })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    expect(wrapper.vm.formTemplates.length).toBe(1)
    wrapper.vm.showFormTemplates = true
    await flushPromises()
  })

  it('opens the form-templates modal from the header button', async () => {
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Biểu mẫu').trigger('click')
    await flushPromises()
    expect(wrapper.vm.showFormTemplates).toBe(true)
    expect(document.body.textContent).toContain('Chưa có biểu mẫu')
    const closeBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim() === 'Đóng')
    closeBtn.click()
    await flushPromises()
    expect(wrapper.vm.showFormTemplates).toBe(false)
  })

  it('issues a credential through the QR button and modal', async () => {
    enterpriseApi.getVisits.mockResolvedValue({ data: { items: [{ visitId: 30, visitorName: 'Khách QR', status: 'Invited', expectedInUtc: 'x' }] } })
    enterpriseApi.issueVisitorCredential.mockResolvedValue({ data: { credentialReference: 'QR-99' } })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    const qrBtn = wrapper.findAll('button').find((b) => b.text() === 'Cấp QR')
    await qrBtn.trigger('click')
    await nextTick()
    expect(wrapper.vm.credentialVisit.visitId).toBe(30)
    const fromInput = document.body.querySelectorAll('.modal-panel input[type="datetime-local"]')[0]
    fromInput.value = '2026-08-10T08:00'
    fromInput.dispatchEvent(new Event('input'))
    const submitBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim() === 'Cấp thẻ')
    submitBtn.click()
    await flushPromises()
    expect(enterpriseApi.issueVisitorCredential).toHaveBeenCalledWith(30, expect.objectContaining({ credentialType: 'QR' }))
    expect(wrapper.vm.credSuccess).toBe('QR-99')
    const closeBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim() === 'Đóng')
    closeBtn.click()
    await flushPromises()
    expect(wrapper.vm.credentialVisit).toBe(null)
  })

  it('opens and closes the parking modal via the button', async () => {
    enterpriseApi.getVisits.mockResolvedValue({ data: { items: [{ visitId: 31, visitorName: 'Khách P', status: 'Approved', expectedInUtc: 'x' }] } })
    enterpriseApi.getParkingAreas.mockResolvedValue({ data: { items: [{ parkingAreaId: 5, name: 'Bãi C' }] } })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    const parkBtn = wrapper.findAll('button').find((b) => b.text() === 'Đỗ xe')
    await parkBtn.trigger('click')
    await flushPromises()
    expect(wrapper.vm.parkingTarget.visitId).toBe(31)
    expect(document.body.textContent).toContain('Bãi C')
    const closeBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim() === 'Đóng')
    closeBtn.click()
    await flushPromises()
    expect(wrapper.vm.parkingTarget).toBe(null)
  })

  it('shows a detail alert from the row button', async () => {
    enterpriseApi.getVisits.mockResolvedValue({ data: { items: [{ visitId: 32, visitorName: 'Khách Dt', status: 'Approved' }] } })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Chi tiết').trigger('click')
    expect(window.alert).toHaveBeenCalledWith(expect.stringContaining('Khách Dt'))
  })
})
