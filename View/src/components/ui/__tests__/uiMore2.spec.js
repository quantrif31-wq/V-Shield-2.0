import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it } from 'vitest'
import { nextTick } from 'vue'
import BaseButton from '../BaseButton.vue'
import BaseCard from '../BaseCard.vue'
import BaseCheckbox from '../BaseCheckbox.vue'
import BaseField from '../BaseField.vue'
import BaseInput from '../BaseInput.vue'
import BaseModal from '../BaseModal.vue'
import BaseRadio from '../BaseRadio.vue'
import BaseTextarea from '../BaseTextarea.vue'
import ConfirmDialog from '../ConfirmDialog.vue'
import LoadingSkeleton from '../LoadingSkeleton.vue'
import RouteErrorBoundary from '../RouteErrorBoundary.vue'
import ToastProvider from '../ToastProvider.vue'

afterEach(() => {
  document.body.innerHTML = ''
})

describe('BaseInput', () => {
  it('renders and emits the value on input', async () => {
    const wrapper = mount(BaseInput, { props: { id: 'name', modelValue: '' } })
    await wrapper.find('input').setValue('An')
    expect(wrapper.emitted('update:modelValue')[0]).toEqual(['An'])
    expect(wrapper.emitted('input')).toBeTruthy()
  })

  it('respects disabled and invalid states', () => {
    const wrapper = mount(BaseInput, { props: { id: 'x', modelValue: '', disabled: true, invalid: true } })
    expect(wrapper.find('input').attributes('disabled')).toBeDefined()
    expect(wrapper.find('input').attributes('aria-invalid')).toBe('true')
  })
})

describe('BaseField', () => {
  it('renders the label and associates it with the field', () => {
    const wrapper = mount(BaseField, {
      props: { label: 'Họ tên', required: true, forId: 'name' },
      slots: { default: '<input id="name">' },
    })
    expect(wrapper.text()).toContain('Họ tên')
    expect(wrapper.find('label').attributes('for')).toBe('name')
    expect(wrapper.find('input').exists()).toBe(true)
  })
})

describe('BaseCheckbox', () => {
  it('emits the checked state', async () => {
    const wrapper = mount(BaseCheckbox, { props: { id: 'c1', modelValue: false, label: 'Đồng ý' } })
    await wrapper.find('input').setValue(true)
    expect(wrapper.emitted('update:modelValue')[0]).toEqual([true])
  })

  it('respects disabled', () => {
    const wrapper = mount(BaseCheckbox, { props: { id: 'c1', modelValue: false, label: 'X', disabled: true } })
    expect(wrapper.find('input').attributes('disabled')).toBeDefined()
  })
})

describe('BaseRadio', () => {
  it('emits its value when selected', async () => {
    const wrapper = mount(BaseRadio, { props: { modelValue: '', value: 'A', name: 'grp', label: 'Mục A' } })
    await wrapper.find('input').setValue(true)
    expect(wrapper.emitted('update:modelValue')[0]).toEqual(['A'])
  })

  it('is checked when it matches the model', () => {
    const wrapper = mount(BaseRadio, { props: { modelValue: 'A', value: 'A', name: 'grp', label: 'Mục A' } })
    expect(wrapper.find('input').element.checked).toBe(true)
  })
})

describe('BaseTextarea', () => {
  it('emits the value on input', async () => {
    const wrapper = mount(BaseTextarea, { props: { id: 'note', modelValue: '' } })
    await wrapper.find('textarea').setValue('Ghi chú')
    expect(wrapper.emitted('update:modelValue')[0]).toEqual(['Ghi chú'])
  })
})

describe('BaseCard', () => {
  it('renders slot content and variant class', () => {
    const wrapper = mount(BaseCard, { props: { variant: 'kpi' }, slots: { default: 'Nội dung' } })
    expect(wrapper.text()).toContain('Nội dung')
    expect(wrapper.classes()).toContain('is-kpi')
  })
})

describe('BaseModal', () => {
  it('renders when open and emits close on overlay click', async () => {
    const wrapper = mount(BaseModal, { props: { open: true, title: 'Hộp thoại' }, slots: { default: 'body' } })
    await nextTick()
    expect(document.body.querySelector('.vs-modal')).toBeTruthy()
    document.body.querySelector('.vs-modal-layer').dispatchEvent(new MouseEvent('mousedown', { bubbles: true }))
    await nextTick()
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('does not render when closed', async () => {
    const wrapper = mount(BaseModal, { props: { open: false, title: 'X' } })
    await flushPromises()
    expect(document.body.querySelector('.vs-modal-layer')).toBeNull()
  })
})

describe('ConfirmDialog', () => {
  it('emits confirm and cancel', async () => {
    const wrapper = mount(ConfirmDialog, {
      props: { open: true, title: 'Xác nhận', confirmLabel: 'Đồng ý', cancelLabel: 'Hủy' },
      global: { stubs: { BaseButton, BaseModal: { props: ['open'], template: '<div v-if="open" class="confirm-wrap"><slot /><slot name="footer" /></div>' } } },
    })
    await wrapper.findAll('button').find((b) => b.text() === 'Đồng ý').trigger('click')
    expect(wrapper.emitted('confirm')).toBeTruthy()
    await wrapper.findAll('button').find((b) => b.text() === 'Hủy').trigger('click')
    expect(wrapper.emitted('cancel')).toBeTruthy()
  })
})

describe('LoadingSkeleton', () => {
  it('renders the skeleton', () => {
    const wrapper = mount(LoadingSkeleton, { props: { variant: 'table', lines: 3 } })
    expect(wrapper.exists()).toBe(true)
  })
})

describe('RouteErrorBoundary', () => {
  it('renders the default slot without errors', () => {
    const wrapper = mount(RouteErrorBoundary, { slots: { default: '<div>nội dung</div>' } })
    expect(wrapper.text()).toContain('nội dung')
  })
})

describe('ToastProvider', () => {
  it('renders the toast container', () => {
    const wrapper = mount(ToastProvider)
    expect(wrapper.find('.vs-toasts').exists()).toBe(true)
  })
})
