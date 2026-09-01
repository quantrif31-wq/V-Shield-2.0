import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalMechaWarrior3DStage from '../PortalMechaWarrior3DStage.vue'

vi.mock('../../../utils/portalAudio', () => ({
  mechaAudio: {
    playTargetLock: vi.fn(),
    playHeavyImpactDrop: vi.fn(),
    playEngage: vi.fn(),
    playClick: vi.fn(),
    playHover: vi.fn()
  }
}))

import { mechaAudio } from '../../../utils/portalAudio'

describe('PortalMechaWarrior3DStage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('mounts and renders cockpit', () => {
    const wrapper = mount(PortalMechaWarrior3DStage)
    expect(wrapper.text()).toContain('FLAGSHIP SHADOW BRIDGE')
  })

  it('handles mousemove and mouseleave for parallax', async () => {
    const wrapper = mount(PortalMechaWarrior3DStage)
    await wrapper.trigger('mousemove', { clientX: 100, clientY: 100 })
    await wrapper.trigger('mouseleave')
    expect(wrapper.vm.mouseX).toBe(0)
  })

  it('emits selectPilot when a quick-dot is clicked', async () => {
    const wrapper = mount(PortalMechaWarrior3DStage)
    const quickDots = wrapper.findAll('button[title]')
    expect(quickDots.length).toBe(5)
    await quickDots[1].trigger('click')
    expect(wrapper.emitted('selectPilot')).toBeTruthy()
    expect(wrapper.emitted('selectPilot')[0][0]).toBe(1)
  })

  it('emits selectPilot when a carousel pilot card is clicked', async () => {
    const wrapper = mount(PortalMechaWarrior3DStage)
    const cards = wrapper.findAll('div.cursor-pointer')
    expect(cards.length).toBeGreaterThanOrEqual(5)
    await cards[2].trigger('click')
    expect(wrapper.emitted('selectPilot')).toBeTruthy()
  })

  it('triggers overdrive and resets after timeout', async () => {
    const wrapper = mount(PortalMechaWarrior3DStage)
    const odBtn = wrapper.findAll('button').find(b => b.text().includes('QUÁ TẢI'))
    await odBtn.trigger('click')
    expect(wrapper.vm.isOverdriveActive).toBe(true)
    expect(mechaAudio.playTargetLock).toHaveBeenCalled()
    expect(mechaAudio.playHeavyImpactDrop).toHaveBeenCalled()
    vi.advanceTimersByTime(1501)
    expect(wrapper.vm.isOverdriveActive).toBe(false)
  })

  it('toggles shield on and off', async () => {
    const wrapper = mount(PortalMechaWarrior3DStage)
    const shieldBtn = wrapper.findAll('button').find(b => b.text().includes('KHIÊN'))
    await shieldBtn.trigger('click')
    expect(wrapper.vm.isShieldActive).toBe(true)
    expect(mechaAudio.playEngage).toHaveBeenCalled()
    await shieldBtn.trigger('click')
    expect(wrapper.vm.isShieldActive).toBe(false)
    expect(mechaAudio.playClick).toHaveBeenCalled()
  })

  it('toggles lock-on on and off', async () => {
    const wrapper = mount(PortalMechaWarrior3DStage)
    const lockBtn = wrapper.findAll('button').find(b => b.text().includes('KHÓA'))
    await lockBtn.trigger('click')
    expect(wrapper.vm.isLockOnActive).toBe(true)
    expect(mechaAudio.playTargetLock).toHaveBeenCalled()
    await lockBtn.trigger('click')
    expect(wrapper.vm.isLockOnActive).toBe(false)
    expect(mechaAudio.playHover).toHaveBeenCalled()
  })

  it('rotates to a different pilot based on activeIndex prop', () => {
    const wrapper = mount(PortalMechaWarrior3DStage, { props: { activeIndex: 2 } })
    expect(wrapper.text()).toContain('CRIMSON ARSENAL COCKPIT')
  })

  it('displays auto-rotate status', () => {
    const wrapper = mount(PortalMechaWarrior3DStage, { props: { isAutoRotating: false } })
    expect(wrapper.text()).toContain('TẠM DỪNG')
  })
})
