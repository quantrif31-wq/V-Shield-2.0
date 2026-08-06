import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import BaseButton from '../BaseButton.vue'
import StatusBadge from '../StatusBadge.vue'
import DataTable from '../DataTable.vue'

describe('BaseButton', () => {
  it('exposes loading and disabled state without duplicate activation', async () => {
    const wrapper = mount(BaseButton, { props: { loading: true }, slots: { default: 'Lưu thay đổi' } })
    expect(wrapper.attributes('aria-busy')).toBe('true')
    expect(wrapper.attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Lưu thay đổi')
  })

  it('requires a supplied accessible label for icon-only usage', () => {
    const wrapper = mount(BaseButton, { props: { iconOnly: true, ariaLabel: 'Đóng' }, slots: { icon: '×' } })
    expect(wrapper.attributes('aria-label')).toBe('Đóng')
  })
})

describe('StatusBadge', () => {
  it('maps operational status to semantic text and label', () => {
    const wrapper = mount(StatusBadge, { props: { status: 'disconnected', label: 'Mất kết nối', dot: true } })
    expect(wrapper.classes()).toContain('is-danger')
    expect(wrapper.attributes('aria-label')).toBe('Mất kết nối')
    expect(wrapper.text()).toContain('Mất kết nối')
  })
})

describe('DataTable', () => {
  it('uses aria-sort for the active sortable column', () => {
    const wrapper = mount(DataTable, { props: { columns: [{ key: 'name', label: 'Tên', sortable: true }], rows: [{ id: 1, name: 'Camera A' }], sortKey: 'name', sortDirection: 'desc' } })
    expect(wrapper.get('th').attributes('aria-sort')).toBe('descending')
  })
})
