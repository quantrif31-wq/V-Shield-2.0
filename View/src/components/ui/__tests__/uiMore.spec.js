import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import BaseSelect from '../BaseSelect.vue'
import BaseSwitch from '../BaseSwitch.vue'
import BaseTabs from '../BaseTabs.vue'
import EmptyState from '../EmptyState.vue'
import PageHeader from '../PageHeader.vue'
import StatusBadge from '../StatusBadge.vue'

describe('StatusBadge', () => {
  it.each([
    ['online', 'is-success'],
    ['active', 'is-success'],
    ['approved', 'is-success'],
    ['offline', 'is-neutral'],
    ['pending', 'is-warning'],
    ['stale', 'is-warning'],
    ['disconnected', 'is-danger'],
    ['rejected', 'is-danger'],
    ['critical', 'is-danger'],
    ['info', 'is-info'],
  ])('maps status %s to %s', (status, expectedClass) => {
    const wrapper = mount(StatusBadge, { props: { status } })
    expect(wrapper.classes()).toContain(expectedClass)
  })

  it('falls back to the raw status and the label for accessibility', () => {
    const wrapper = mount(StatusBadge, { props: { status: 'custom', label: 'Tuỳ chọn' } })
    expect(wrapper.classes()).toContain('is-custom')
    expect(wrapper.attributes('aria-label')).toBe('Tuỳ chọn')
  })

  it('uses srLabel when provided and renders the default label text', () => {
    const wrapper = mount(StatusBadge, { props: { status: 'active', label: 'Đang chạy', srLabel: 'Trạng thái máy quét' } })
    expect(wrapper.attributes('aria-label')).toBe('Trạng thái máy quét')
    expect(wrapper.text()).toContain('Đang chạy')
  })

  it('renders dot and icon slots', () => {
    const wrapper = mount(StatusBadge, { props: { status: 'active', dot: true }, slots: { icon: '<i>i</i>' } })
    expect(wrapper.find('.vs-status__dot').exists()).toBe(true)
    expect(wrapper.find('.vs-status__icon').exists()).toBe(true)
  })
})

describe('EmptyState', () => {
  it('renders title and description with an empty role', () => {
    const wrapper = mount(EmptyState, { props: { title: 'Trống', description: 'Chưa có dữ liệu' } })
    expect(wrapper.find('h3').text()).toBe('Trống')
    expect(wrapper.find('p').text()).toBe('Chưa có dữ liệu')
    expect(wrapper.attributes('role')).toBeUndefined()
  })

  it('marks error states as alerts', () => {
    const wrapper = mount(EmptyState, { props: { title: 'Lỗi', description: 'x', kind: 'error' } })
    expect(wrapper.classes()).toContain('is-error')
    expect(wrapper.attributes('role')).toBe('alert')
  })

  it('renders icon and action slots', () => {
    const wrapper = mount(EmptyState, {
      props: { title: 'Trống', description: 'd' },
      slots: { icon: '<i>i</i>', actions: '<button>Thử lại</button>' },
    })
    expect(wrapper.find('.vs-empty__icon').exists()).toBe(true)
    expect(wrapper.find('.vs-empty__actions button').text()).toBe('Thử lại')
  })
})

describe('PageHeader', () => {
  it('renders title, description and breadcrumbs', () => {
    const wrapper = mount(PageHeader, {
      props: { title: 'Quản lý nhân sự', description: 'Danh sách', breadcrumbs: [{ label: 'Trang chủ', to: '/' }, { label: 'Nhân sự' }] },
      global: { stubs: { RouterLink: { template: '<a><slot /></a>' } } },
    })
    expect(wrapper.find('h1').text()).toBe('Quản lý nhân sự')
    expect(wrapper.find('nav').exists()).toBe(true)
    expect(wrapper.find('[aria-current="page"]').text()).toBe('Nhân sự')
  })

  it('renders action and status slots', () => {
    const wrapper = mount(PageHeader, {
      props: { title: 'T', description: 'D' },
      slots: { actions: '<button>Xuất</button>', status: '<span>ok</span>' },
    })
    expect(wrapper.find('.vs-page-header__actions button').text()).toBe('Xuất')
    expect(wrapper.find('.vs-page-header__title span').text()).toBe('ok')
  })
})

describe('BaseTabs', () => {
  it('renders tabs and emits on click', () => {
    const wrapper = mount(BaseTabs, {
      props: { items: [{ value: 'a', label: 'A' }, { value: 'b', label: 'B', count: 3 }], modelValue: 'a' },
    })
    expect(wrapper.findAll('button')).toHaveLength(2)
    expect(wrapper.find('#tabs-tab-b .vs-tabs__count').text()).toBe('3')
    wrapper.get('#tabs-tab-b').trigger('click')
    expect(wrapper.emitted('update:modelValue')[0]).toEqual(['b'])
  })

  it('supports arrow-key navigation', async () => {
    const wrapper = mount(BaseTabs, {
      props: { items: [{ value: 'a', label: 'A' }, { value: 'b', label: 'B' }, { value: 'c', label: 'C' }], modelValue: 'a' },
    })
    wrapper.get('#tabs-tab-a').trigger('keydown', { key: 'ArrowRight' })
    await new Promise((resolve) => setTimeout(resolve, 0))
    expect(wrapper.emitted('update:modelValue')[0]).toEqual(['b'])
    wrapper.get('#tabs-tab-b').trigger('keydown', { key: 'Home' })
    await new Promise((resolve) => setTimeout(resolve, 0))
    expect(wrapper.emitted('update:modelValue')[1]).toEqual(['a'])
  })
})

describe('BaseSwitch', () => {
  it('renders label and emits toggles', () => {
    const wrapper = mount(BaseSwitch, { props: { modelValue: false, label: 'Bật ghi hình' } })
    expect(wrapper.text()).toContain('Bật ghi hình')
    wrapper.get('input').setValue(true)
    expect(wrapper.emitted('update:modelValue')[0]).toEqual([true])
  })

  it('respects the disabled prop', () => {
    const wrapper = mount(BaseSwitch, { props: { modelValue: false, label: 'X', disabled: true } })
    expect(wrapper.get('input').attributes('disabled')).toBeDefined()
  })

  it('renders the description when provided', () => {
    const wrapper = mount(BaseSwitch, { props: { modelValue: true, label: 'X', description: 'ghi chú' } })
    expect(wrapper.text()).toContain('ghi chú')
  })
})

describe('BaseSelect', () => {
  it('renders a loading placeholder', () => {
    const wrapper = mount(BaseSelect, {
      props: { id: 'sel', modelValue: '', placeholder: 'Chọn...', loading: true },
      slots: { default: '<option value="1">Một</option>' },
    })
    expect(wrapper.find('option').text()).toBe('Đang tải...')
    expect(wrapper.get('select').attributes('disabled')).toBeDefined()
  })

  it('emits update on change and marks invalid state', () => {
    const wrapper = mount(BaseSelect, {
      props: { id: 'sel', modelValue: '', invalid: true },
      slots: { default: '<option value="1">Một</option>' },
    })
    expect(wrapper.get('select').attributes('aria-invalid')).toBe('true')
    wrapper.get('select').setValue('1')
    expect(wrapper.emitted('update:modelValue')[0]).toEqual(['1'])
  })

  it('renders a disabled placeholder option', () => {
    const wrapper = mount(BaseSelect, {
      props: { id: 'sel', modelValue: '', placeholder: 'Chọn phòng ban' },
      slots: { default: '<option value="1">An Ninh</option>' },
    })
    expect(wrapper.find('option').text()).toBe('Chọn phòng ban')
    expect(wrapper.find('option').attributes('disabled')).toBeDefined()
  })
})
