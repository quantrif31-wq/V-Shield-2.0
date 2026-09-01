import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import PortalAudioToggle from '../PortalAudioToggle.vue'

vi.mock('../../../utils/portalAudio', () => ({
  mechaAudio: {
    sfxEnabled: true,
    bgmEnabled: false,
    playClick: vi.fn(),
    playTargetLock: vi.fn(),
    toggleBgm: vi.fn().mockReturnValue(true)
  }
}))

vi.mock('../../../utils/portalVoiceSynth', () => ({
  tacticalVoice: {
    speak: vi.fn(),
    speakSystemBoot: vi.fn()
  }
}))

import { mechaAudio } from '../../../utils/portalAudio'
import { tacticalVoice } from '../../../utils/portalVoiceSynth'

describe('PortalAudioToggle', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('toggles SFX off', async () => {
    const wrapper = mount(PortalAudioToggle)
    const sfxBtn = wrapper.findAll('button').find(b => b.text().includes('SFX'))
    await sfxBtn.trigger('click')
    expect(sfxBtn.classes()).not.toContain('border-amber-500/50')
  })

  it('toggles SFX back on and plays sound', async () => {
    const wrapper = mount(PortalAudioToggle)
    const sfxBtn = wrapper.findAll('button').find(b => b.text().includes('SFX'))
    await sfxBtn.trigger('click')
    await sfxBtn.trigger('click')
    expect(mechaAudio.playClick).toHaveBeenCalled()
    expect(tacticalVoice.speak).toHaveBeenCalled()
  })

  it('toggles BGM and plays target lock', async () => {
    mechaAudio.toggleBgm.mockReturnValue(true)
    const wrapper = mount(PortalAudioToggle)
    const bgmBtn = wrapper.findAll('button').find(b => b.text().includes('BGM'))
    await bgmBtn.trigger('click')
    expect(mechaAudio.playTargetLock).toHaveBeenCalled()
    expect(tacticalVoice.speakSystemBoot).toHaveBeenCalled()
    expect(wrapper.findAll('button').find(b => b.text().includes('BGM')).classes()).toContain('border-orange-500/60')
  })

  it('handles BGM toggle returning false', async () => {
    mechaAudio.toggleBgm.mockReturnValue(false)
    const wrapper = mount(PortalAudioToggle)
    const bgmBtn = wrapper.findAll('button').find(b => b.text().includes('BGM'))
    await bgmBtn.trigger('click')
    expect(mechaAudio.playTargetLock).not.toHaveBeenCalled()
  })
})
