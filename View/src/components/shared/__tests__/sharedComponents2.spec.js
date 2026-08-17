import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../PrivilegedActionReasonForm.vue', () => ({
  default: { name: 'PrivilegedActionReasonForm', template: '<div class="parf">REASON</div>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'PrivilegedActionReasonForm',
  __name: 'PrivilegedActionReasonForm',
}))

const DecisionDrawer = (await import('../DecisionDrawer.vue')).default
const EnterpriseActionDrawer = (await import('../EnterpriseActionDrawer.vue')).default
const ExceptionCaseTimeline = (await import('../ExceptionCaseTimeline.vue')).default
const EnterpriseDataTable = (await import('../EnterpriseDataTable.vue')).default

beforeEach(() => vi.clearAllMocks())
afterEach(() => {
  document.body.innerHTML = ''
})

describe('DecisionDrawer', () => {
  it('renders when visible and emits allow on action', async () => {
    const wrapper = mount(DecisionDrawer, {
      props: { visible: true, laneName: 'Cổng A', subjectName: 'An', canAllow: true },
      global: { stubs: { PrivilegedActionReasonForm: true } },
    })
    await flushPromises()
    expect(document.body.textContent).toContain('Cổng A')
    const allowBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Cho qua'))
    allowBtn.click()
    expect(wrapper.emitted('action')).toBeTruthy()
  })

  it('emits close when the close button is pressed', async () => {
    const wrapper = mount(DecisionDrawer, {
      props: { visible: true, laneName: 'Cổng A' },
      global: { stubs: { PrivilegedActionReasonForm: true } },
    })
    await flushPromises()
    document.body.querySelector('.dd-close').click()
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('does not render when hidden', () => {
    const wrapper = mount(DecisionDrawer, {
      props: { visible: false },
      global: { stubs: { PrivilegedActionReasonForm: true } },
    })
    expect(document.body.querySelector('.dd-root')).toBeNull()
  })
})

describe('EnterpriseActionDrawer', () => {
  it('renders when open and emits close on overlay click', async () => {
    const wrapper = mount(EnterpriseActionDrawer, { props: { open: true, title: 'Cấp quyền' } })
    await flushPromises()
    expect(document.body.textContent).toContain('Cấp quyền')
    document.body.querySelector('.action-drawer-overlay').click()
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('does not render when closed', () => {
    const wrapper = mount(EnterpriseActionDrawer, { props: { open: false, title: 'X' } })
    expect(document.body.querySelector('.action-drawer-overlay')).toBeNull()
  })
})

describe('ExceptionCaseTimeline', () => {
  it('renders timeline entries sorted by time', () => {
    const wrapper = mount(ExceptionCaseTimeline, {
      props: {
        items: [
          { id: 2, timestamp: '2026-08-02T00:00:00Z', type: 'event', title: 'Sau' },
          { id: 1, timestamp: '2026-08-01T00:00:00Z', type: 'event', title: 'Trước' },
        ],
      },
    })
    expect(wrapper.find('.ect-root').exists()).toBe(true)
    const text = wrapper.text()
    expect(text.indexOf('Trước')).toBeLessThan(text.indexOf('Sau'))
  })

  it('shows an empty state without items', () => {
    const wrapper = mount(ExceptionCaseTimeline, { props: { items: [] } })
    expect(wrapper.find('.ect-empty').exists()).toBe(true)
  })
})

describe('EnterpriseDataTable', () => {
  const columns = [{ key: 'name', label: 'Tên' }]
  it('renders rows', () => {
    const wrapper = mount(EnterpriseDataTable, {
      props: { columns, rows: [{ id: 1, name: 'Cổng A' }], rowKey: 'id' },
    })
    expect(wrapper.find('tbody').text()).toContain('Cổng A')
  })

  it('shows the loading state', () => {
    const wrapper = mount(EnterpriseDataTable, {
      props: { columns, rows: [], loading: true },
    })
    expect(wrapper.text()).toContain('Đang tải')
  })

  it('shows the error state', () => {
    const wrapper = mount(EnterpriseDataTable, {
      props: { columns, rows: [], error: 'Lỗi kết nối' },
    })
    expect(wrapper.text()).toContain('Lỗi kết nối')
  })

  it('shows the empty state', () => {
    const wrapper = mount(EnterpriseDataTable, {
      props: { columns, rows: [], emptyTitle: 'Không có dữ liệu' },
    })
    expect(wrapper.text()).toContain('Không có dữ liệu')
  })
})
