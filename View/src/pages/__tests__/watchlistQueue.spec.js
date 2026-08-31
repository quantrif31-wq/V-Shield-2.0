import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({
  route: null,
  router: { replace: vi.fn() },
}))

vi.mock('vue-router', () => {
  const { reactive } = require('vue')
  const route = reactive({ query: {} })
  hoisted.route = route
  return { useRoute: () => route, useRouter: () => hoisted.router }
})
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getWatchlistMatches: vi.fn(),
    getWatchlistEntries: vi.fn(),
    reviewWatchlistMatch: vi.fn(),
    createWatchlistEntry: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const WatchlistQueue = (await import('../WatchlistQueue.vue')).default

const matchA = {
  watchlistMatchId: 11,
  watchlistEntry: { displayName: 'Nguyễn Văn A', reason: 'Biển cấm', severity: 'Critical' },
  visit: { visitorName: 'Khách K', visitorPhone: '0901', status: 'CheckedIn', hostEmployee: { fullName: 'An' } },
  matchedAtUtc: '2024-01-01T00:00:00Z',
  status: 'Pending',
}
const matchB = {
  watchlistMatchId: 12,
  watchlistEntry: { displayName: '', reason: null, severity: 'Low' },
  visit: null,
  matchedAtUtc: null,
  status: 'Confirmed',
}
const matchC = {
  watchlistMatchId: 13,
  watchlistEntry: { displayName: 'Phương tiện X', reason: 'Trốn thuế', severity: 'Medium' },
  visit: { visitorName: 'Nam', visitorPhone: '0902', status: 'Approved', hostEmployee: { fullName: 'Bình' } },
  matchedAtUtc: '2024-06-01T00:00:00Z',
  status: 'FalsePositive',
}
const matchD = {
  watchlistMatchId: 14,
  watchlistEntry: { displayName: 'Khách Q', reason: 'Danh sách', severity: 'High' },
  visit: { visitorName: 'Q', visitorPhone: '0903', status: 'Overstay', hostEmployee: { fullName: 'C' } },
  matchedAtUtc: '2024-06-02T00:00:00Z',
  status: 'Escalated',
}

const entries = [
  { watchlistEntryId: 1, displayName: 'Nguyễn Văn A', entityType: 'Person', identifier: 'MB-01', severity: 'Critical', isActive: true, reason: 'Biển cấm' },
  { watchlistEntryId: 2, displayName: 'Xe 01', entityType: 'Vehicle', identifier: '51A-1', severity: 'Low', isActive: false, reason: '' },
]

const mountOptions = {
  global: {
    stubs: { teleport: true, RouterLink: { template: '<a><slot /></a>' } },
  },
}

let mountedWrappers = []
const mountQ = (component, options) => {
  const wrapper = mount(component, options)
  mountedWrappers.push(wrapper)
  return wrapper
}

beforeEach(() => {
  mountedWrappers = []
  vi.clearAllMocks()
  hoisted.route.query = {}
  enterpriseApi.getWatchlistMatches.mockResolvedValue({ data: { items: [matchA, matchB, matchC, matchD], total: 50 } })
  enterpriseApi.getWatchlistEntries.mockResolvedValue({ data: entries })
  enterpriseApi.reviewWatchlistMatch.mockResolvedValue({ data: {} })
  enterpriseApi.createWatchlistEntry.mockResolvedValue({ data: {} })
})

afterEach(async () => {
  for (const wrapper of mountedWrappers) wrapper.unmount()
  mountedWrappers = []
})

const openEntryModal = async (wrapper) => {
  await wrapper.findAll('header button')[1].trigger('click')
}
const clickFooterByText = async (wrapper, text) => {
  const target = wrapper.findAll('footer button').find((b) => b.text().includes(text))
  expect(target, `missing footer button "${text}"`).toBeTruthy()
  await target.trigger('click')
}

describe('WatchlistQueue', () => {
  it('loads matches on mount and renders the matches table fully', async () => {
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()

    expect(enterpriseApi.getWatchlistMatches).toHaveBeenCalledWith({ page: 1, pageSize: 25, severity: undefined })
    expect(wrapper.find('#watchlist-panel-matches').exists()).toBe(true)
    expect(wrapper.findAll('tbody tr').length).toBe(4)
    expect(wrapper.text()).toContain('1 / 2')
    expect(wrapper.text()).toContain('Nguyễn Văn A')
    expect(wrapper.text()).toContain('Chờ xử lý')
    expect(wrapper.text()).toContain('Chưa xác định')
    expect(wrapper.text()).toContain('—')
  })

  it('drives status search + severity filter, then reloads and pages', async () => {
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()

    await wrapper.find('#watchlist-status-search').setValue('conf')
    expect(wrapper.findAll('tbody tr').length).toBe(1)

    await wrapper.find('#watchlist-status-search').setValue('')
    expect(wrapper.findAll('tbody tr').length).toBe(4)

    await wrapper.find('#watchlist-severity').setValue('Low')
    expect(wrapper.findAll('tbody tr').length).toBe(1)
    expect(hoisted.router.replace).toHaveBeenCalledWith({ query: { severity: 'Low', page: undefined } })

    await wrapper.find('#watchlist-severity').setValue('')
    expect(wrapper.findAll('tbody tr').length).toBe(4)
    expect(hoisted.router.replace).toHaveBeenCalledWith({ query: { severity: undefined, page: undefined } })

    await wrapper.find('button.is-secondary').trigger('click')
    await flushPromises()
    expect(enterpriseApi.getWatchlistMatches).toHaveBeenCalledTimes(2)

    await wrapper.findAll('.pagination button')[1].trigger('click')
    await flushPromises()
    expect(hoisted.router.replace).toHaveBeenLastCalledWith({ query: { page: 2 } })

    hoisted.route.query = { page: '2' }
    await flushPromises()
    await wrapper.findAll('.pagination button')[0].trigger('click')
    await flushPromises()
    expect(hoisted.router.replace).toHaveBeenLastCalledWith({ query: { page: undefined } })
  })

  it('opens visitor details and reviews a pending match', async () => {
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()

    await wrapper.findAll('tbody tr td:nth-child(3) button')[0].trigger('click')
    expect(wrapper.text()).toContain('Khách K')
    await clickFooterByText(wrapper, 'Đóng')

    await wrapper.findAll('tbody tr td:nth-child(3) button')[0].trigger('click')
    await wrapper.find('button[aria-label="Đóng hộp thoại"]').trigger('click')
    await flushPromises()
    expect(wrapper.text()).not.toContain('Chi tiết khách')

    const reviewButtons = wrapper.findAll('tbody tr td:nth-child(6) button')
    await reviewButtons[0].trigger('click')
    expect(wrapper.text()).toContain('Nguyễn Văn A')
    await wrapper.find('#watchlist-decision').setValue('Escalated')
    await wrapper.find('#watchlist-note').setValue('Ghi chú kiểm duyệt')

    await wrapper.find('#watchlist-review-form').trigger('submit')
    await flushPromises()
    expect(enterpriseApi.reviewWatchlistMatch).toHaveBeenCalledWith(11, { status: 'Escalated', reviewNote: 'Ghi chú kiểm duyệt' })
    expect(enterpriseApi.getWatchlistMatches).toHaveBeenCalledTimes(2)
    expect(wrapper.find('#watchlist-review-form').exists()).toBe(false)
  })

  it('keeps note when review fails and reviews non-pending statuses', async () => {
    enterpriseApi.reviewWatchlistMatch.mockRejectedValue({ response: { data: { message: 'Máy chủ lỗi' } } })
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()

    const reviewButtons = wrapper.findAll('tbody tr td:nth-child(6) button')
    await reviewButtons[1].trigger('click')
    expect(wrapper.find('#watchlist-decision').element.value).toBe('Confirmed')

    await wrapper.find('#watchlist-decision').setValue('Closed')
    await wrapper.find('#watchlist-review-form').trigger('submit')
    await flushPromises()
    expect(enterpriseApi.reviewWatchlistMatch).toHaveBeenCalledWith(12, { status: 'Closed', reviewNote: null })
    expect(wrapper.find('.form-error').text()).toContain('Máy chủ lỗi')
    expect(wrapper.find('#watchlist-review-form').exists()).toBe(true)
  })

  it('cancels review from the footer and the header close button', async () => {
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()

    const reviewButtons = wrapper.findAll('tbody tr td:nth-child(6) button')
    await reviewButtons[0].trigger('click')
    await clickFooterByText(wrapper, 'Hủy')
    expect(wrapper.find('#watchlist-review-form').exists()).toBe(false)

    await reviewButtons[0].trigger('click')
    await wrapper.find('button[aria-label="Đóng hộp thoại"]').trigger('click')
    expect(wrapper.find('#watchlist-review-form').exists()).toBe(false)
  })

  it('loads entries when entries tab is active and renders the entries table', async () => {
    hoisted.route.query = { tab: 'entries' }
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()

    expect(enterpriseApi.getWatchlistEntries).toHaveBeenCalledWith({ active: true })
    expect(wrapper.find('#watchlist-panel-entries').exists()).toBe(true)
    expect(wrapper.findAll('tbody tr').length).toBe(2)

    await wrapper.find('#watchlist-entry-search').setValue('51A')
    expect(wrapper.findAll('tbody tr').length).toBe(1)

    await wrapper.find('#watchlist-entry-search').setValue('')
    expect(wrapper.findAll('tbody tr').length).toBe(2)

    await wrapper.find('#watchlist-tab-matches').trigger('click')
    await flushPromises()
    expect(hoisted.router.replace).toHaveBeenCalledWith({
      query: { tab: undefined, page: undefined, severity: undefined },
    })
    expect(wrapper.find('#watchlist-panel-matches').exists()).toBe(true)
  })

  it('adds an entry from the header (empty name blocked, then valid submit)', async () => {
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()

    await openEntryModal(wrapper)
    expect(wrapper.find('#watchlist-entry-form').exists()).toBe(true)

    await wrapper.find('#watchlist-entry-form').trigger('submit')
    await flushPromises()
    expect(enterpriseApi.createWatchlistEntry).not.toHaveBeenCalled()

    await wrapper.find('#watchlist-name').setValue('Tân đối tượng')
    await wrapper.find('#watchlist-identifier').setValue('P-1')
    await wrapper.find('#watchlist-reason').setValue('Theo dõi')
    await wrapper.find('#watchlist-new-severity').setValue('High')
    await wrapper.find('#watchlist-entry-form').trigger('submit')
    await flushPromises()

    expect(enterpriseApi.createWatchlistEntry).toHaveBeenCalledWith({
      entityType: 'Person',
      displayName: 'Tân đối tượng',
      identifier: 'P-1',
      severity: 'High',
      reason: 'Theo dõi',
    })
    expect(wrapper.find('#watchlist-entry-form').exists()).toBe(false)
  })

  it('keeps entry info when save fails (response and plain errors)', async () => {
    enterpriseApi.createWatchlistEntry.mockRejectedValue({ response: { data: { message: 'Trùng mã' } } })
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    await openEntryModal(wrapper)
    await wrapper.find('#watchlist-name').setValue('Kẻ khả nghi')
    await wrapper.find('#watchlist-entry-form').trigger('submit')
    await flushPromises()
    expect(wrapper.find('.form-error').text()).toContain('Trùng mã')
    expect(wrapper.find('#watchlist-entry-form').exists()).toBe(true)
    wrapper.unmount()

    enterpriseApi.createWatchlistEntry.mockRejectedValue(new Error('hỏng'))
    const wrapper2 = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    await openEntryModal(wrapper2)
    await wrapper2.find('#watchlist-name').setValue('Kẻ khả nghi')
    await wrapper2.find('#watchlist-entry-form').trigger('submit')
    await flushPromises()
    expect(wrapper2.find('.form-error').text()).toContain('Không thể thêm đối tượng')
  })

  it('discards dirty entry via ConfirmDialog and closes clean entry directly', async () => {
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()

    await openEntryModal(wrapper)
    await wrapper.find('#watchlist-name').setValue('Bẩn')
    await clickFooterByText(wrapper, 'Hủy')
    expect(wrapper.text()).toContain('Bỏ đối tượng đang nhập?')
    await clickFooterByText(wrapper, 'Bỏ thay đổi')
    await flushPromises()
    expect(wrapper.find('#watchlist-entry-form').exists()).toBe(false)

    await openEntryModal(wrapper)
    await clickFooterByText(wrapper, 'Hủy')
    expect(wrapper.find('#watchlist-entry-form').exists()).toBe(false)
  })

  it('cancels the discard dialog without closing the entry modal', async () => {
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()

    await openEntryModal(wrapper)
    await wrapper.find('#watchlist-name').setValue('X')
    await clickFooterByText(wrapper, 'Hủy')
    const buttons = wrapper.findAll('footer button')
    await buttons[buttons.length - 2].trigger('click')
    expect(wrapper.find('#watchlist-entry-form').exists()).toBe(true)
  })

  it('adds an entry while on the entries tab and reloads entries', async () => {
    hoisted.route.query = { tab: 'entries' }
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()

    await openEntryModal(wrapper)
    await wrapper.find('#watchlist-name').setValue('Xe nghi vấn')
    await wrapper.find('#watchlist-type').setValue('Vehicle')
    await wrapper.find('#watchlist-entry-form').trigger('submit')
    await flushPromises()

    expect(enterpriseApi.createWatchlistEntry).toHaveBeenCalledWith(expect.objectContaining({ entityType: 'Vehicle' }))
    expect(enterpriseApi.getWatchlistEntries).toHaveBeenCalledTimes(2)
  })

  it('shows permission denied and load errors for matches and entries', async () => {
    enterpriseApi.getWatchlistMatches.mockRejectedValue({ response: { status: 403 } })
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    expect(enterpriseApi.getWatchlistEntries).not.toHaveBeenCalled()
    wrapper.unmount()

    enterpriseApi.getWatchlistMatches.mockRejectedValue({ response: { data: { message: 'mất mạng' } } })
    const wrapper2 = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    expect(wrapper2.text()).toContain('mất mạng')
    wrapper2.unmount()

    enterpriseApi.getWatchlistMatches.mockRejectedValue(new Error('nope'))
    const wrapper3 = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    expect(wrapper3.text()).toContain('Không thể tải cảnh báo watchlist.')
    wrapper3.unmount()

    hoisted.route.query = { tab: 'entries' }
    enterpriseApi.getWatchlistEntries.mockRejectedValue({ response: { status: 403 } })
    const wrapper4 = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    expect(enterpriseApi.getWatchlistEntries).toHaveBeenCalled()
    wrapper4.unmount()

    enterpriseApi.getWatchlistEntries.mockRejectedValue(new Error('nope'))
    const wrapper5 = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    expect(wrapper5.text()).toContain('Không thể tải danh mục watchlist.')
  })

  it('covers retry actions and the empty-actions button', async () => {
    enterpriseApi.getWatchlistMatches.mockRejectedValueOnce(new Error('first'))
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    expect(wrapper.text()).toContain('Không thể tải cảnh báo watchlist.')
    await wrapper.find('#watchlist-panel-matches button.is-secondary').trigger('click')
    await flushPromises()
    expect(wrapper.findAll('tbody tr').length).toBe(4)

    hoisted.route.query = { tab: 'entries' }
    enterpriseApi.getWatchlistEntries.mockRejectedValueOnce(new Error('second'))
    hoisted.route.query = { tab: 'entries' }
    const wrapper2 = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    expect(wrapper2.text()).toContain('Không thể tải danh mục watchlist.')
    await wrapper2.find('#watchlist-panel-entries button.is-secondary').trigger('click')
    await flushPromises()
    expect(wrapper2.findAll('tbody tr').length).toBe(2)

    hoisted.route.query = { tab: 'entries' }
    enterpriseApi.getWatchlistEntries.mockResolvedValue({ data: [] })
    const wrapper3 = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    await wrapper3.find('#watchlist-panel-entries button.is-primary').trigger('click')
    expect(wrapper3.find('#watchlist-entry-form').exists()).toBe(true)
  })

  it('closeEntry without force keeps the modal and requests the discard dialog', async () => {
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    await openEntryModal(wrapper)
    await wrapper.find('#watchlist-name').setValue('X')
    wrapper.vm.closeEntry()
    await flushPromises()
    expect(wrapper.text()).toContain('Bỏ đối tượng đang nhập?')
  })

  it('reacts to route query changes through the query watcher', async () => {
    const wrapper = mountQ(WatchlistQueue, mountOptions)
    await flushPromises()
    expect(enterpriseApi.getWatchlistMatches).toHaveBeenCalledTimes(1)

    hoisted.route.query = { tab: 'entries', page: '3', severity: 'High' }
    await flushPromises()

    expect(enterpriseApi.getWatchlistEntries).toHaveBeenCalledTimes(1)
    expect(wrapper.find('#watchlist-panel-entries').exists()).toBe(true)
    expect(wrapper.find('#watchlist-panel-matches').exists()).toBe(false)

    hoisted.route.query = { tab: 'matches' }
    await flushPromises()
    expect(enterpriseApi.getWatchlistMatches).toHaveBeenCalledTimes(2)
    expect(wrapper.find('#watchlist-panel-matches').exists()).toBe(true)
  })
})