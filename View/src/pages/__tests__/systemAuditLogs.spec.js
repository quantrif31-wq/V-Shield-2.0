import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/accessLogApi', () => ({ getSystemAuditLogs: vi.fn() }))

const accessLogApi = await import('../../services/accessLogApi')
const SystemAuditLogs = (await import('../SystemAuditLogs.vue')).default

const requestRow = {
  id: 10,
  actionType: 'REQUEST',
  username: 'admin',
  timestampUtc: '2026-08-01T10:00:00Z',
  path: '/api/auth/login',
  isSuccess: true,
  newValuesJson: JSON.stringify({ device: 'Chrome', ip: '1.2.3.4', city: 'Hà Nội', country: 'VN' }),
}
const createRow = {
  id: 11,
  actionType: 'CREATE',
  username: 'admin',
  entityName: 'Employee',
  timestampUtc: '2026-08-01T10:00:05Z',
  isSuccess: true,
  oldValuesJson: null,
  newValuesJson: JSON.stringify({ fullName: 'An' }),
}

beforeEach(() => {
  vi.useFakeTimers()
  vi.clearAllMocks()
})
afterEach(() => {
  vi.runOnlyPendingTimers()
  vi.useRealTimers()
})

describe('SystemAuditLogs', () => {
  it('renders audit rows and opens the detail drawer', async () => {
    accessLogApi.getSystemAuditLogs.mockResolvedValue({ data: { items: [requestRow, createRow] } })
    const wrapper = mount(SystemAuditLogs)
    await flushPromises()
    expect(wrapper.text()).toContain('Đăng nhập')
    expect(wrapper.text()).toContain('Employee')

    await wrapper.findAll('.audit-row')[1].trigger('click')
    await flushPromises()
    expect(wrapper.find('.audit-drawer').exists()).toBe(true)
    expect(wrapper.text()).toContain('Tạo mới dữ liệu')
  })

  it('derives request metadata for a non-request row', async () => {
    const nonRequest = {
      id: 12,
      actionType: 'UPDATE',
      username: 'admin',
      entityName: 'User',
      timestampUtc: '2026-08-01T10:00:02Z',
      isSuccess: false,
      failureReason: 'denied',
      newValuesJson: JSON.stringify({ enabled: true }),
    }
    accessLogApi.getSystemAuditLogs.mockResolvedValue({ data: { items: [requestRow, nonRequest] } })
    const wrapper = mount(SystemAuditLogs)
    await flushPromises()
    await wrapper.findAll('.audit-row')[1].trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('denied')
    expect(wrapper.text()).toContain('Chrome')
    expect(wrapper.text()).toContain('Hà Nội, VN')
  })

  it('debounces filter changes and resets filters', async () => {
    accessLogApi.getSystemAuditLogs.mockResolvedValue({ data: { items: [createRow] } })
    const wrapper = mount(SystemAuditLogs)
    await flushPromises()
    expect(accessLogApi.getSystemAuditLogs).toHaveBeenCalledTimes(1)

    await wrapper.find('input[type="text"]').setValue('An')
    await flushPromises()
    vi.advanceTimersByTime(300)
    await flushPromises()
    expect(accessLogApi.getSystemAuditLogs).toHaveBeenCalledWith(expect.objectContaining({ query: 'An' }))

    await wrapper.findAll('button').find((b) => b.text() === 'Đặt lại').trigger('click')
    await flushPromises()
    expect(accessLogApi.getSystemAuditLogs).toHaveBeenLastCalledWith(expect.objectContaining({ query: undefined }))
  })

  it('shows an error message when the API fails', async () => {
    accessLogApi.getSystemAuditLogs.mockRejectedValue(new Error('x'))
    const wrapper = mount(SystemAuditLogs)
    await flushPromises()
    expect(wrapper.text()).toContain('Không tải được nhật ký hệ thống.')
  })
})
