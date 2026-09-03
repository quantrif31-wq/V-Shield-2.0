import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'

vi.mock('../GateTransitMonitor.vue', () => ({
  default: {
    name: 'GateTransitMonitor',
    props: { credentialMode: String },
    template: '<div class="gate-monitor">{{ credentialMode }}</div>'
  }
}))

import FacePlateTransitMonitor from '../ThongHanh.vue'

describe('FacePlateTransitMonitor', () => {
  it('reuses the transit monitor with FaceID as its credential', () => {
    const wrapper = mount(FacePlateTransitMonitor)
    expect(wrapper.find('.gate-monitor').text()).toBe('FACEID')
  })
})
