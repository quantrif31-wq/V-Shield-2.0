import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it } from 'vitest'
import { routeProgress } from '../../../services/routeLoading'
import RouteProgress from '../RouteProgress.vue'
import RouteErrorBoundary from '../RouteErrorBoundary.vue'

afterEach(() => {
  routeProgress.active = false
  routeProgress.progress = 0
})

describe('RouteProgress', () => {
  it('renders nothing while idle', () => {
    const wrapper = mount(RouteProgress)
    expect(wrapper.find('.route-progress').exists()).toBe(false)
  })

  it('renders the progress bar when active', () => {
    routeProgress.active = true
    routeProgress.progress = 42
    const wrapper = mount(RouteProgress)
    expect(wrapper.find('.route-progress').exists()).toBe(true)
    expect(wrapper.find('.route-progress__fill').attributes('style')).toContain('42%')
  })
})

describe('RouteErrorBoundary', () => {
  it('renders the child slot when there is no error', () => {
    const wrapper = mount(RouteErrorBoundary, { slots: { default: '<div class="ok">Nội dung</div>' } })
    expect(wrapper.find('.ok').text()).toBe('Nội dung')
  })
})
