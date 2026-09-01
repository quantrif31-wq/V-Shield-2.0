import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const EnterpriseActionDrawer = (await import('../EnterpriseActionDrawer.vue')).default

beforeEach(() => {
  document.body.innerHTML = ''
})

afterEach(() => {
  document.body.innerHTML = ''
})

const openDrawer = (opts = {}) =>
  mount(EnterpriseActionDrawer, {
    props: { open: true, title: 'Xóa bằng chứng', ...(opts.props || {}) },
    slots: { default: '<p class="drawer-body">Nội dung</p>', ...(opts.slots || {}) },
    attachTo: document.body,
  })

describe('EnterpriseActionDrawer', () => {
  it('renders nothing when not open', () => {
    mount(EnterpriseActionDrawer, { props: { open: false, title: 'T' }, attachTo: document.body })
    expect(document.body.querySelector('.action-drawer')).toBeNull()
  })

  it('renders title, eyebrow and body when open', () => {
    openDrawer()
    expect(document.body.querySelector('.action-drawer').getAttribute('aria-label')).toBe('Xóa bằng chứng')
    expect(document.body.textContent).toContain('Xóa bằng chứng')
    expect(document.body.textContent).toContain('Thao tác có kiểm soát')
    expect(document.body.textContent).toContain('Nội dung')
  })

  it('uses a custom eyebrow when provided', () => {
    openDrawer({ props: { eyebrow: 'Kiểm soát đặc biệt' } })
    expect(document.body.textContent).toContain('Kiểm soát đặc biệt')
  })

  it('emits close when the close button is clicked', () => {
    const wrapper = openDrawer()
    document.body.querySelector('header button').click()
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('emits close when the overlay backdrop is clicked', () => {
    const wrapper = openDrawer()
    document.body.querySelector('.action-drawer-overlay').dispatchEvent(new MouseEvent('click', { bubbles: true }))
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('does not emit close when clicking inside the drawer', async () => {
    const wrapper = openDrawer()
    document.body.querySelector('.action-drawer').dispatchEvent(new MouseEvent('click', { bubbles: true }))
    expect(wrapper.emitted('close')).toBeFalsy()
  })

  it('emits close when Escape is pressed while open', () => {
    const wrapper = openDrawer()
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('does not emit close on Escape when closed', () => {
    const wrapper = mount(EnterpriseActionDrawer, { props: { open: false, title: 'T' }, attachTo: document.body })
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    expect(wrapper.emitted('close')).toBeFalsy()
  })

  it('renders the footer slot when provided', () => {
    openDrawer({ slots: { default: '<p>Body</p>', footer: '<button class="drawer-footer-btn">Xác nhận</button>' } })
    expect(document.body.querySelector('.drawer-footer-btn')).toBeTruthy()
  })

  it('removes the keydown listener on unmount', () => {
    const spy = vi.spyOn(window, 'removeEventListener')
    const wrapper = openDrawer()
    wrapper.unmount()
    expect(spy).toHaveBeenCalledWith('keydown', expect.any(Function))
    spy.mockRestore()
  })
})
