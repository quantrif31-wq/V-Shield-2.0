import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ route: { params: {} } }))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route }))
vi.mock('../../services/preRegistrationApi', () => ({ validateToken: vi.fn(), submitRegistration: vi.fn() }))
vi.mock('../../utils/nameValidator', () => ({ validateVietnameseName: vi.fn(), normalizeVietnameseName: vi.fn() }))

const preRegistrationApi = await import('../../services/preRegistrationApi')
const nameValidator = await import('../../utils/nameValidator')

let GuestRegister

beforeEach(async () => {
  vi.clearAllMocks()
  hoisted.route.params = { token: 'tok-1' }
  globalThis.alert = vi.fn()
  try {
    Object.defineProperty(window, 'close', { value: vi.fn(), configurable: true })
  } catch {}
  window.open = vi.fn()
  try {
    Object.defineProperty(window, 'closed', { value: true, configurable: true })
  } catch {}
  vi.spyOn(console, 'error').mockImplementation(() => {})
  preRegistrationApi.validateToken.mockResolvedValue({
    data: {
      hostEmployeeName: 'Nguyen Van A',
      hostEmployeePhone: '0901234567',
      hostEmployeeEmail: 'a@vshield.vn',
      hostDepartmentName: 'An ninh',
      hostPositionName: 'Trưởng',
      hostFaceImageUrl: null,
      expiredAt: '2026-01-31T23:59:59Z',
    },
  })
  nameValidator.validateVietnameseName.mockReturnValue({ isValid: true, error: '', normalizedName: 'Nguyen Van A' })
  nameValidator.normalizeVietnameseName.mockImplementation((n) => (n || '').trim())
  if (!GuestRegister) {
    GuestRegister = (await import('../GuestRegister.vue')).default
  }
})

afterEach(() => {
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('GuestRegister', () => {
  it('validates the registration token on mount and fills host info', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    expect(preRegistrationApi.validateToken).toHaveBeenCalledWith('tok-1')
    expect(wrapper.vm.isValidating).toBe(false)
    expect(wrapper.vm.hostInfo.hostEmployeeName).toBe('Nguyen Van A')
    expect(wrapper.text()).toContain('Nguyen Van A')
    expect(wrapper.text()).toContain('Đăng ký khách thăm quan')
  })

  it('shows error card for invalid token with server message', async () => {
    preRegistrationApi.validateToken.mockRejectedValue({ response: { data: { message: 'Token không hợp lệ' } } })
    const wrapper = mount(GuestRegister)
    await flushPromises()
    expect(wrapper.vm.errorState).toBe(true)
    expect(wrapper.text()).toContain('Token không hợp lệ')
    expect(wrapper.text()).toContain('Không thể truy cập')
  })

  it('shows default error message when token rejection has no server message', async () => {
    preRegistrationApi.validateToken.mockRejectedValue(new Error('net'))
    const wrapper = mount(GuestRegister)
    await flushPromises()
    expect(wrapper.vm.errorState).toBe(true)
    expect(wrapper.text()).toContain('Link không hợp lệ hoặc đã hết hạn')
  })

  it('renders validating state while token is pending', async () => {
    let resolveFn
    preRegistrationApi.validateToken.mockReturnValue(new Promise((r) => { resolveFn = r }))
    const wrapper = mount(GuestRegister)
    await flushPromises()
    expect(wrapper.vm.isValidating).toBe(true)
    expect(wrapper.text()).toContain('Đang xác thực link đăng ký')
    resolveFn({ data: { hostEmployeeName: 'Nguyen Van A' } })
    await flushPromises()
    expect(wrapper.vm.isValidating).toBe(false)
  })

  it('getInitials computes initials from name', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    expect(wrapper.vm.getInitials('Nguyen Van A')).toBe('VA')
    expect(wrapper.vm.getInitials('Nguyen Van')).toBe('NV')
    expect(wrapper.vm.getInitials('')).toBe('??')
    expect(wrapper.vm.getInitials(null)).toBe('??')
  })

  it('formatDateTime formats dates and handles empty input', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    expect(wrapper.vm.formatDateTime(null)).toBe('—')
    expect(wrapper.vm.formatDateTime('')).toBe('—')
    expect(wrapper.vm.formatDateTime('2026-01-05T08:30:00Z')).toMatch(/\d{2}:\d{2} \d{2}\/\d{2}\/\d{4}/)
  })

  it('runNameValidation resets state for empty name', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = '   '
    wrapper.vm.runNameValidation()
    expect(wrapper.vm.nameValidation.touched).toBe(false)
    expect(wrapper.vm.nameValidation.isValid).toBe(false)
  })

  it('runNameValidation marks valid name', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = 'Nguyen Van A'
    wrapper.vm.runNameValidation()
    expect(wrapper.vm.nameValidation.touched).toBe(true)
    expect(wrapper.vm.nameValidation.isValid).toBe(true)
    expect(wrapper.vm.nameValidation.error).toBe('')
  })

  it('runNameValidation marks invalid name with error', async () => {
    nameValidator.validateVietnameseName.mockReturnValue({ isValid: false, error: 'Họ tên không hợp lệ', normalizedName: '' })
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = 'A'
    wrapper.vm.runNameValidation()
    expect(wrapper.vm.nameValidation.touched).toBe(true)
    expect(wrapper.vm.nameValidation.isValid).toBe(false)
    expect(wrapper.vm.nameValidation.error).toBe('Họ tên không hợp lệ')
  })

  it('runVisitorNameValidation handles missing, empty, valid and invalid visitors', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.runVisitorNameValidation(9)
    const visitor = wrapper.vm.form.visitors[0]
    visitor.fullName = ''
    wrapper.vm.runVisitorNameValidation(0)
    expect(visitor._nameError).toBe('')
    visitor.fullName = 'Nguyen Van An'
    wrapper.vm.runVisitorNameValidation(0)
    expect(visitor._nameError).toBe('')
    nameValidator.validateVietnameseName.mockReturnValue({ isValid: false, error: 'Họ tên không hợp lệ', normalizedName: '' })
    visitor.fullName = 'A'
    wrapper.vm.runVisitorNameValidation(0)
    expect(visitor._nameError).toBe('Họ tên không hợp lệ')
  })

  it('goToStep2 alerts when name is invalid', async () => {
    nameValidator.validateVietnameseName.mockReturnValue({ isValid: false, error: 'Họ tên không hợp lệ', normalizedName: '' })
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = 'A'
    await wrapper.vm.goToStep2()
    expect(globalThis.alert).toHaveBeenCalledWith('Họ tên không hợp lệ')
    expect(wrapper.vm.currentStep).toBe(1)
  })

  it('goToStep2 alerts with fallback message when name error is empty', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = ''
    await wrapper.vm.goToStep2()
    expect(globalThis.alert).toHaveBeenCalledWith('Họ và tên không hợp lệ')
    expect(wrapper.vm.currentStep).toBe(1)
  })

  it('goToStep2 alerts when time out is not after time in', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = 'Nguyen Van A'
    wrapper.vm.form.expectedTimeIn = '2026-01-05T08:00'
    wrapper.vm.form.expectedTimeOut = '2026-01-05T07:00'
    await wrapper.vm.goToStep2()
    expect(globalThis.alert).toHaveBeenCalledWith('Thời gian ra phải sau thời gian vào!')
    expect(wrapper.vm.currentStep).toBe(1)
  })

  it('goToStep2 normalizes name and moves to step 2 adding visitors', async () => {
    nameValidator.normalizeVietnameseName.mockReturnValue('Nguyen Van A')
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = ' nguyen van a '
    wrapper.vm.form.expectedTimeIn = '2026-01-05T08:00'
    wrapper.vm.form.expectedTimeOut = '2026-01-05T17:00'
    wrapper.vm.form.numberOfVisitors = 3
    await wrapper.vm.goToStep2()
    expect(wrapper.vm.form.fullName).toBe('Nguyen Van A')
    expect(wrapper.vm.form.visitors.length).toBe(3)
    expect(wrapper.vm.currentStep).toBe(2)
    expect(wrapper.findAll('.visitor-form-card').length).toBe(3)
  })

  it('goToStep2 removes excess visitors when number decreased', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = 'Nguyen Van A'
    wrapper.vm.form.expectedTimeIn = '2026-01-05T08:00'
    wrapper.vm.form.expectedTimeOut = '2026-01-05T17:00'
    wrapper.vm.form.visitors = [{ fullName: 'A A', idCardNumber: '', expectedFaceImage: null, _nameError: '' }, { fullName: 'B B', idCardNumber: '', expectedFaceImage: null, _nameError: '' }]
    wrapper.vm.form.numberOfVisitors = 1
    await wrapper.vm.goToStep2()
    expect(wrapper.vm.form.visitors.length).toBe(1)
    expect(wrapper.vm.currentStep).toBe(2)
  })

  it('addVisitor and removeVisitor manage visitors array', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.addVisitor()
    expect(wrapper.vm.form.visitors.length).toBe(2)
    wrapper.vm.removeVisitor(0)
    expect(wrapper.vm.form.visitors.length).toBe(1)
  })

  it('handleSubmit alerts when no visitor has a name', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.expectedTimeIn = '2026-01-05T08:00'
    wrapper.vm.form.expectedTimeOut = '2026-01-05T17:00'
    await wrapper.vm.handleSubmit()
    expect(globalThis.alert).toHaveBeenCalledWith('Vui lòng điền tên ít nhất 1 khách trong đoàn')
    expect(preRegistrationApi.submitRegistration).not.toHaveBeenCalled()
  })

  it('handleSubmit alerts when a visitor name is invalid', async () => {
    nameValidator.validateVietnameseName.mockReturnValue({ isValid: false, error: 'Họ tên không hợp lệ', normalizedName: '' })
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = 'Nguyen Van A'
    wrapper.vm.form.expectedTimeIn = '2026-01-05T08:00'
    wrapper.vm.form.expectedTimeOut = '2026-01-05T17:00'
    wrapper.vm.form.visitors[0].fullName = 'X'
    await wrapper.vm.handleSubmit()
    expect(globalThis.alert).toHaveBeenCalledWith('Vui lòng kiểm tra lại tên các khách trong đoàn')
    expect(preRegistrationApi.submitRegistration).not.toHaveBeenCalled()
  })

  it('handleSubmit succeeds, shows success card and auto-closes after countdown', async () => {
    vi.useFakeTimers()
    preRegistrationApi.submitRegistration.mockResolvedValue({ data: { registrationId: 99 } })
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = 'Nguyen Van A'
    wrapper.vm.form.phone = '0901234567'
    wrapper.vm.form.expectedTimeIn = '2026-01-05T08:00'
    wrapper.vm.form.expectedTimeOut = '2026-01-05T17:00'
    wrapper.vm.form.visitors[0].fullName = 'Nguyen Van An'
    wrapper.vm.form.visitors[0].idCardNumber = '012345'
    await wrapper.vm.handleSubmit()
    await flushPromises()
    expect(preRegistrationApi.submitRegistration).toHaveBeenCalledWith('tok-1', expect.objectContaining({
      fullName: 'Nguyen Van A',
      phone: '0901234567',
      numberOfVisitors: 1,
      visitors: [{ fullName: 'Nguyen Van A', idCardNumber: '012345', expectedFaceImage: null }],
    }))
    expect(wrapper.vm.isSubmitted).toBe(true)
    expect(wrapper.vm.submittedId).toBe(99)
    expect(wrapper.vm.countdown).toBe(5)
    expect(wrapper.vm.isSubmitting).toBe(false)
    expect(wrapper.text()).toContain('Đăng ký thành công!')
    expect(wrapper.text()).toContain('#99')
    await vi.advanceTimersByTimeAsync(5000)
    expect(wrapper.vm.countdown).toBe(0)
    expect(window.close).toHaveBeenCalled()
  })

  it('handleSubmit alerts server error message on failure', async () => {
    preRegistrationApi.submitRegistration.mockRejectedValue({ response: { data: { message: 'Link hết hạn' } } })
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = 'Nguyen Van A'
    wrapper.vm.form.expectedTimeIn = '2026-01-05T08:00'
    wrapper.vm.form.expectedTimeOut = '2026-01-05T17:00'
    wrapper.vm.form.visitors[0].fullName = 'Nguyen Van An'
    await wrapper.vm.handleSubmit()
    await flushPromises()
    expect(globalThis.alert).toHaveBeenCalledWith('Link hết hạn')
    expect(console.error).toHaveBeenCalled()
    expect(wrapper.vm.isSubmitting).toBe(false)
    expect(wrapper.vm.isSubmitted).toBe(false)
  })

  it('handleSubmit alerts generic error when no server message', async () => {
    preRegistrationApi.submitRegistration.mockRejectedValue(new Error('boom'))
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = 'Nguyen Van A'
    wrapper.vm.form.expectedTimeIn = '2026-01-05T08:00'
    wrapper.vm.form.expectedTimeOut = '2026-01-05T17:00'
    wrapper.vm.form.visitors[0].fullName = 'Nguyen Van An'
    await wrapper.vm.handleSubmit()
    await flushPromises()
    expect(globalThis.alert).toHaveBeenCalledWith('Có lỗi xảy ra, vui lòng thử lại')
    expect(wrapper.vm.isSubmitting).toBe(false)
  })

  it('closeCurrentPage closes window directly when it closes', async () => {
    vi.useFakeTimers()
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.closeCurrentPage()
    expect(window.close).toHaveBeenCalled()
  })

  it('closeCurrentPage falls back to open+close when window stays open', async () => {
    vi.useFakeTimers()
    Object.defineProperty(window, 'closed', { value: false, configurable: true })
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.closeCurrentPage()
    await vi.advanceTimersByTimeAsync(120)
    expect(window.open).toHaveBeenCalledWith('', '_self')
    expect(window.close).toHaveBeenCalled()
  })

  it('resetForm clears submission and form state', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.isSubmitted = true
    wrapper.vm.submittedId = 5
    wrapper.vm.currentStep = 2
    wrapper.vm.form.fullName = 'Nguyen Van A'
    wrapper.vm.form.phone = '0901'
    wrapper.vm.form.numberOfVisitors = 3
    wrapper.vm.form.visitors = [{ fullName: 'B B', idCardNumber: '', expectedFaceImage: null, _nameError: '' }, { fullName: 'C C', idCardNumber: '', expectedFaceImage: null, _nameError: '' }]
    wrapper.vm.resetForm()
    expect(wrapper.vm.isSubmitted).toBe(false)
    expect(wrapper.vm.currentStep).toBe(1)
    expect(wrapper.vm.form.fullName).toBe('')
    expect(wrapper.vm.form.phone).toBe('')
    expect(wrapper.vm.form.expectedTimeIn).toBe('')
    expect(wrapper.vm.form.expectedTimeOut).toBe('')
    expect(wrapper.vm.form.numberOfVisitors).toBe(1)
    expect(wrapper.vm.form.visitors.length).toBe(1)
  })

  it('renders success details host name and can submit through DOM flow', async () => {
    preRegistrationApi.submitRegistration.mockResolvedValue({ data: { registrationId: 7 } })
    nameValidator.normalizeVietnameseName.mockReturnValue('Nguyen Van A')
    const wrapper = mount(GuestRegister)
    await flushPromises()
    await wrapper.find('input[type="text"]').setValue('Nguyen Van A')
    const inputs = wrapper.findAll('input[type="datetime-local"]')
    await inputs.at(0).setValue('2026-01-05T08:00')
    await inputs.at(1).setValue('2026-01-05T17:00')
    const nextBtn = wrapper.findAll('button').find((b) => b.text().includes('Tiếp theo'))
    await nextBtn.trigger('click')
    expect(wrapper.vm.currentStep).toBe(2)
    const visitorInput = wrapper.findAll('input[placeholder="Họ tên khách"]').at(0)
    await visitorInput.setValue('Nguyen Van An')
    const submitBtn = wrapper.findAll('button').find((b) => b.text().includes('Gửi đăng ký'))
    await submitBtn.trigger('click')
    await flushPromises()
    expect(wrapper.vm.isSubmitted).toBe(true)
    expect(wrapper.text()).toContain('#7')
    expect(wrapper.text()).toContain('Nguyen Van A')
  })

  it('goes back to step 1 via Quay lại button', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.currentStep = 2
    await nextTick()
    const backBtn = wrapper.findAll('button').find((b) => b.text().includes('Quay lại'))
    await backBtn.trigger('click')
    expect(wrapper.vm.currentStep).toBe(1)
  })

  it('validates name via DOM input and blur events', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    const nameInput = wrapper.find('input[type="text"]')
    await nameInput.setValue('Nguyen Van A')
    await nameInput.trigger('blur')
    expect(wrapper.vm.nameValidation.touched).toBe(true)
    expect(wrapper.text()).toContain('Hợp lệ')
    nameValidator.validateVietnameseName.mockReturnValue({ isValid: false, error: 'Họ tên không hợp lệ', normalizedName: '' })
    await nameInput.setValue('A')
    await nameInput.trigger('blur')
    expect(wrapper.vm.nameValidation.isValid).toBe(false)
    expect(wrapper.vm.nameValidation.error).toBe('Họ tên không hợp lệ')
  })

  it('updates phone and visitors number inputs via DOM', async () => {
    const wrapper = mount(GuestRegister)
    await flushPromises()
    await wrapper.find('input[type="tel"]').setValue('0912000000')
    expect(wrapper.vm.form.phone).toBe('0912000000')
    await wrapper.find('input[type="number"]').setValue('4')
    expect(wrapper.vm.form.numberOfVisitors).toBe(4)
  })

  it('adds and removes visitors through DOM and validates visitor name on blur', async () => {
    nameValidator.normalizeVietnameseName.mockReturnValue('Nguyen Van A')
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.form.fullName = 'Nguyen Van A'
    wrapper.vm.form.expectedTimeIn = '2026-01-05T08:00'
    wrapper.vm.form.expectedTimeOut = '2026-01-05T17:00'
    wrapper.vm.form.numberOfVisitors = 2
    await wrapper.vm.goToStep2()
    await nextTick()
    expect(wrapper.findAll('.visitor-form-card').length).toBe(2)
    const visitorName = wrapper.findAll('input[placeholder="Họ tên khách"]').at(0)
    await visitorName.setValue('Nguyen Van An')
    await visitorName.trigger('blur')
    expect(wrapper.vm.form.visitors[0]._nameError).toBe('')
    const idCard = wrapper.findAll('input[placeholder="012345678901"]').at(0)
    await idCard.setValue('987654')
    expect(wrapper.vm.form.visitors[0].idCardNumber).toBe('987654')
    wrapper.vm.form.numberOfVisitors = 5
    await nextTick()
    const addBtn = wrapper.findAll('button').find((b) => b.text().includes('Thêm khách'))
    await addBtn.trigger('click')
    expect(wrapper.vm.form.visitors.length).toBe(3)
    const removeBtn = wrapper.findAll('.btn-remove').at(0)
    await removeBtn.trigger('click')
    expect(wrapper.vm.form.visitors.length).toBe(2)
  })

  it('closeCurrentPage catches when window.close throws', async () => {
    vi.useFakeTimers()
    window.close.mockImplementation(() => { throw new Error('denied') })
    Object.defineProperty(window, 'closed', { value: false, configurable: true })
    const wrapper = mount(GuestRegister)
    await flushPromises()
    wrapper.vm.closeCurrentPage()
    await vi.advanceTimersByTimeAsync(120)
    expect(wrapper.exists()).toBe(true)
  })
})