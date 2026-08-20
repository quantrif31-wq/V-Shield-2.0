import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'

const { authState, setUser } = vi.hoisted(() => {
  const authState = { user: null }
  return {
    authState,
    setUser: (user) => { authState.user = user },
  }
})

vi.mock('../../stores/auth', () => ({ authState }))

vi.mock('../../services/dynamicQrApi', () => ({
  generateDynamicQr: vi.fn(),
}))

vi.mock('qrcode', () => ({
  __esModule: true,
  default: { toDataURL: vi.fn().mockResolvedValue('data:image/png;base64,QR') },
}))

import DynamicQrGenerator from '../DynamicQrGenerator.vue'
import { generateDynamicQr } from '../../services/dynamicQrApi'
import QRCode from 'qrcode'

const sampleQr = (overrides = {}) => ({
  success: true,
  message: 'OK',
  data: {
    employeeId: 7,
    employeeName: 'Nguyen A',
    qrPayload: 'EMP:7:ABC:123',
    timeStepSeconds: 30,
    generatedAtUtc: '2026-08-20T01:00:00Z',
    expiresAtUtc: new Date(Date.now() + 20000).toISOString(),
    ...overrides,
  },
})

beforeEach(() => {
  vi.clearAllMocks()
  generateDynamicQr.mockResolvedValue(sampleQr())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('DynamicQrGenerator.vue', () => {
  it('renders personal mode and issues QR for the authed employee', async () => {
    setUser({ role: 'User', employeeId: 5, fullName: 'Chi Le' })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    expect(wrapper.find('.mode-pill').text()).toContain('Cá nhân')
    expect(generateDynamicQr).toHaveBeenCalledWith(5)
    expect(wrapper.vm.qrData.employeeName).toBe('Nguyen A')
    expect(wrapper.find('img[alt="Dynamic QR"]').attributes('src')).toContain('data:image/png')
    expect(QRCode.toDataURL).toHaveBeenCalled()
    wrapper.unmount()
  })

  it('shows admin mode with the configured controls', async () => {
    setUser({ role: 'Admin', employeeId: null, fullName: 'Admin' })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    expect(wrapper.find('.mode-pill').text()).toContain('Admin')
    expect(generateDynamicQr).not.toHaveBeenCalled()
    expect(wrapper.find('input.qr-input').exists()).toBe(true)
  })

  it('requires a valid employee id before issuing', async () => {
    setUser({ role: 'Admin', employeeId: null })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    await wrapper.vm.issueRealtime()
    expect(wrapper.vm.errorMessage).toContain('Vui lòng nhập Employee ID hợp lệ')
  })

  it('shows the backend message when generation fails', async () => {
    setUser({ role: 'User', employeeId: 5, fullName: 'Chi Le' })
    generateDynamicQr.mockResolvedValue({ success: false, data: null, message: 'employee not found' })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    expect(wrapper.vm.errorMessage).toContain('employee not found')
    expect(wrapper.vm.isRealtimeActive).toBe(false)
  })

  it('rejects bad payloads returned from the backend', async () => {
    setUser({ role: 'User', employeeId: 5, fullName: 'Chi Le' })
    generateDynamicQr.mockResolvedValue({ success: true, data: { qrPayload: '' } })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    expect(wrapper.vm.errorMessage).toContain('Tạo QR thất bại')
  })

  it('pauses and resumes realtime', async () => {
    setUser({ role: 'User', employeeId: 5, fullName: 'Chi Le' })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    expect(wrapper.vm.isRealtimeActive).toBe(true)
    wrapper.vm.pauseRealtime()
    expect(wrapper.vm.isRealtimeActive).toBe(false)
    expect(wrapper.vm.successMessage).toContain('tạm dừng')
    await wrapper.vm.resumeRealtime()
    await flushPromises()
    expect(wrapper.vm.isRealtimeActive).toBe(true)
    expect(wrapper.vm.successMessage).toContain('Realtime đã được tiếp tục')
    wrapper.unmount()
  })

  it('resumes realtime with a fresh fetch when the qr is expired', async () => {
    setUser({ role: 'User', employeeId: 5, fullName: 'Chi Le' })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    wrapper.vm.pauseRealtime()
    wrapper.vm.qrData.expiresAtUtc = new Date(Date.now() - 5000).toISOString()
    generateDynamicQr.mockResolvedValue(
      sampleQr({ expiresAtUtc: new Date(Date.now() + 20000).toISOString() })
    )
    await wrapper.vm.resumeRealtime()
    await flushPromises()
    expect(generateDynamicQr).toHaveBeenCalledTimes(2)
    expect(wrapper.vm.isRealtimeActive).toBe(true)
    wrapper.unmount()
  })

  it('refreshes manually and keeps realtime state', async () => {
    setUser({ role: 'User', employeeId: 5, fullName: 'Chi Le' })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    await wrapper.vm.refreshOnce()
    await flushPromises()
    expect(generateDynamicQr).toHaveBeenCalledTimes(2)
    expect(wrapper.vm.isRealtimeActive).toBe(true)
    expect(wrapper.vm.successMessage).toContain('đồng bộ lại')
    wrapper.unmount()
  })

  it('refreshes manually with fresh expiry and pauses realtime when refreshed fails', async () => {
    setUser({ role: 'User', employeeId: 5, fullName: 'Chi Le' })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    generateDynamicQr.mockResolvedValue(sampleQr())
    await wrapper.vm.refreshOnce()
    expect(wrapper.vm.remainingSeconds).toBeGreaterThan(0)
    generateDynamicQr.mockResolvedValue({ success: false, data: null, message: 'x' })
    wrapper.vm.isRealtimeActive = false
    await wrapper.vm.refreshOnce()
    expect(wrapper.vm.isRealtimeActive).toBe(false)
    wrapper.unmount()
  })

  it('copies the payload through the clipboard api', async () => {
    setUser({ role: 'User', employeeId: 5, fullName: 'Chi Le' })
    const writeText = vi.fn().mockResolvedValue(undefined)
    Object.defineProperty(navigator, 'clipboard', {
      configurable: true,
      value: { writeText },
    })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    await wrapper.vm.copyPayload()
    await flushPromises()
    expect(writeText).toHaveBeenCalledWith('EMP:7:ABC:123')
    expect(wrapper.vm.copyMessage).toContain('Đã sao chép')
    wrapper.unmount()
  })

  it('reports when the clipboard api is unavailable', async () => {
    setUser({ role: 'User', employeeId: 5, fullName: 'Chi Le' })
    Object.defineProperty(navigator, 'clipboard', {
      configurable: true,
      value: undefined,
    })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    await wrapper.vm.copyPayload()
    await flushPromises()
    expect(wrapper.vm.copyMessage).toContain('Không thể sao chép')
    wrapper.unmount()
  })

  it('does not call copy when there is no payload', async () => {
    setUser({ role: 'User', employeeId: 5, fullName: 'Chi Le' })
    Object.defineProperty(navigator, 'clipboard', {
      configurable: true,
      value: { writeText: vi.fn() },
    })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    wrapper.vm.qrData = null
    await wrapper.vm.copyPayload()
    expect(wrapper.vm.copyMessage).toBe('')
    wrapper.unmount()
  })

  it('formats date and time helpers', () => {
    setUser({ role: 'Admin', employeeId: null })
    const wrapper = mount(DynamicQrGenerator)
    expect(wrapper.vm.formatDate('2026-08-20T01:02:03Z')).toContain('2026')
    expect(wrapper.vm.formatTimeOnly('2026-08-20T01:02:03Z')).toContain(':')
    expect(wrapper.vm.formatDate(null)).toBe('--')
    expect(wrapper.vm.formatTimeOnly(null)).toBe('--:--:--')
  })

  it('cleans up the ticker and copy timeouts on unmount', async () => {
    setUser({ role: 'Role', employeeId: 9, fullName: 'X' })
    const spy = vi.spyOn(global, 'clearTimeout')
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    wrapper.unmount()
    expect(spy).toHaveBeenCalled()
  })
})

async function flushPromises() {
  await new Promise((resolve) => setTimeout(resolve, 0))
  await new Promise((resolve) => setTimeout(resolve, 0))
}