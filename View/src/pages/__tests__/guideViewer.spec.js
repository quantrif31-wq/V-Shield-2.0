import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'

vi.mock('../../components/guide/GuideHero.vue', () => ({
  default: { name: 'GuideHero', template: '<div class="guide-hero">HERO</div>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'GuideHero',
  __name: 'GuideHero',
}))
vi.mock('../../components/guide/GuidePageCard.vue', () => ({
  default: { name: 'GuidePageCard', props: ['page'], template: '<article class="guide-card">{{ page.title }}</article>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'GuidePageCard',
  __name: 'GuidePageCard',
}))

const GuideViewer = (await import('../GuideViewer.vue')).default

describe('GuideViewer', () => {
  it('renders the guide hero', async () => {
    const wrapper = mount(GuideViewer)
    await flushPromises()
    expect(wrapper.find('.guide-hero').exists()).toBe(true)
    expect(wrapper.exists()).toBe(true)
  })
})
