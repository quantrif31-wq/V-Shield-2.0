import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalFoundersTurntable from '../PortalFoundersTurntable.vue'

vi.mock('../../../utils/portalAudio', () => ({
  mechaAudio: {
    playTargetLock: vi.fn(),
    playHeavyImpactDrop: vi.fn(),
    playClick: vi.fn()
  }
}))

vi.mock('../../../utils/portalVoiceSynth', () => ({
  tacticalVoice: { speakTargetLocked: vi.fn() }
}))

import { mechaAudio } from '../../../utils/portalAudio'
import { tacticalVoice } from '../../../utils/portalVoiceSynth'

const stageStub = {
  template: '<div class="stage-stub"><slot /></div>'
}

describe('PortalFoundersTurntable', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('mounts and renders current champion', () => {
    const wrapper = mount(PortalFoundersTurntable, {
      global: { stubs: { PortalMechaWarrior3DStage: stageStub } }
    })
    expect(wrapper.text()).toContain('Phạm Văn Thành')
  })

  it('navigates to next champion', async () => {
    const wrapper = mount(PortalFoundersTurntable, {
      global: { stubs: { PortalMechaWarrior3DStage: stageStub } }
    })
    const nextBtn = wrapper.findAll('button').find(b => b.text().includes('TIẾP'))
    await nextBtn.trigger('click')
    expect(wrapper.vm.activeIndex).toBe(1)
    expect(mechaAudio.playTargetLock).toHaveBeenCalled()
    expect(tacticalVoice.speakTargetLocked).toHaveBeenCalled()
  })

  it('navigates to previous champion with wrap-around', async () => {
    const wrapper = mount(PortalFoundersTurntable, {
      global: { stubs: { PortalMechaWarrior3DStage: stageStub } }
    })
    const prevBtn = wrapper.findAll('button').find(b => b.text().includes('TRƯỚC'))
    await prevBtn.trigger('click')
    expect(wrapper.vm.activeIndex).toBe(4)
  })

  it('selects a champion via quick switcher', async () => {
    const wrapper = mount(PortalFoundersTurntable, {
      global: { stubs: { PortalMechaWarrior3DStage: stageStub } }
    })
    const champBtn = wrapper.findAll('button').find(b => {
      const t = b.text()
      return !t.includes('TRƯỚC') && !t.includes('TIẾP') && !t.includes('TỰ XOAY')
    })
    await champBtn.trigger('click')
    expect(mechaAudio.playTargetLock).toHaveBeenCalled()
    expect(tacticalVoice.speakTargetLocked).toHaveBeenCalled()
  })

  it('toggles auto rotate off and on', async () => {
    const wrapper = mount(PortalFoundersTurntable, {
      global: { stubs: { PortalMechaWarrior3DStage: stageStub } }
    })
    const toggleBtn = wrapper.findAll('button').find(b => b.text().includes('TỰ XOAY'))
    await toggleBtn.trigger('click')
    expect(wrapper.vm.isAutoRotating).toBe(false)
    await toggleBtn.trigger('click')
    expect(wrapper.vm.isAutoRotating).toBe(true)
  })

  it('auto-rotates via setInterval every 6.5 seconds', () => {
    const wrapper = mount(PortalFoundersTurntable, {
      global: { stubs: { PortalMechaWarrior3DStage: stageStub } }
    })
    const initial = wrapper.vm.activeIndex
    vi.advanceTimersByTime(6501)
    expect(wrapper.vm.activeIndex).toBe((initial + 1) % 5)
  })

  it('stops auto-rotate interval on unmount', () => {
    const wrapper = mount(PortalFoundersTurntable, {
      global: { stubs: { PortalMechaWarrior3DStage: stageStub } }
    })
    wrapper.unmount()
    vi.advanceTimersByTime(60000)
  })
})
