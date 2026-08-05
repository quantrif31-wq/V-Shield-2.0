import axe from 'axe-core'
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import BaseButton from '../BaseButton.vue'
import StatusBadge from '../StatusBadge.vue'

async function expectNoViolations(wrapper) {
  document.body.innerHTML = ''
  const main = document.createElement('main')
  main.appendChild(wrapper.element)
  document.body.appendChild(main)
  const result = await axe.run(document.body, { rules: { 'color-contrast': { enabled: false } } })
  expect(result.violations.map((item) => item.id)).toEqual([])
}

describe('shared UI accessibility', () => {
  it('keeps an icon button named', async () => {
    await expectNoViolations(mount(BaseButton, { props: { iconOnly: true, ariaLabel: 'Đóng hộp thoại' }, slots: { icon: '×' } }))
  })

  it('announces an operational status with text', async () => {
    await expectNoViolations(mount(StatusBadge, { props: { status: 'stale', label: 'Dữ liệu cũ', dot: true } }))
  })
})
