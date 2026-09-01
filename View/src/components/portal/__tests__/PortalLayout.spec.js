import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalLayout from '../PortalLayout.vue'

vi.mock('../../../utils/portalAudio', () => ({
  mechaAudio: {
    playHeavyImpactDrop: vi.fn(),
    playEngage: vi.fn(),
    playClick: vi.fn(),
    playHover: vi.fn()
  }
}))

import { mechaAudio } from '../../../utils/portalAudio'

const childStub = { template: '<div class="stub"><slot /></div>' }
const authModalStub = {
  template: '<div class="auth-stub"><button class="auth-close" @click="$emit(\'close\')">close</button><button class="auth-success" @click="$emit(\'login-success\', { fullName: \'Emitted User\' })">login</button></div>'
}

describe('PortalLayout', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  function mountLayout() {
    return mount(PortalLayout, {
      global: {
        stubs: {
          PortalTacticalCursor: childStub,
          PortalLaserSparksCanvas: childStub,
          PortalScreenHudOverlay: childStub,
          PortalGlobalThreeCanvas: childStub,
          PortalParticlesCanvas: childStub,
          PortalNavbar: childStub,
          PortalFooter: childStub,
          PortalLiveTelemetryFeed: childStub,
          PortalAuthModal: authModalStub,
          'router-view': childStub,
          'router-link': childStub
        }
      }
    })
  }

  it('handles auth modal close event', async () => {
    const wrapper = mountLayout()
    wrapper.vm.handleOpenAuth()
    await wrapper.vm.$nextTick()
    await wrapper.find('.auth-close').trigger('click')
    expect(wrapper.vm.showAuthModal).toBe(false)
  })

  it('handles auth modal login-success event', async () => {
    const wrapper = mountLayout()
    wrapper.vm.handleOpenAuth()
    await wrapper.vm.$nextTick()
    await wrapper.find('.auth-success').trigger('click')
    expect(wrapper.vm.communityUser).toEqual({ fullName: 'Emitted User' })
    expect(wrapper.vm.showAuthModal).toBe(false)
    expect(mechaAudio.playEngage).toHaveBeenCalled()
  })

  it('mounts and attaches global listeners', () => {
    const clickSpy = vi.spyOn(window, 'addEventListener')
    const wrapper = mountLayout()
    expect(clickSpy).toHaveBeenCalled()
    wrapper.unmount()
  })

  it('loads saved community user from localStorage on mount', () => {
    localStorage.setItem('vshield_community_user', JSON.stringify({ fullName: 'Saved' }))
    const wrapper = mountLayout()
    expect(wrapper.vm.communityUser).toEqual({ fullName: 'Saved' })
  })

  it('handles invalid saved user JSON gracefully', () => {
    localStorage.setItem('vshield_community_user', 'not-json{')
    const wrapper = mountLayout()
    expect(wrapper.vm.communityUser).toBeNull()
  })

  it('opens auth modal', async () => {
    const wrapper = mountLayout()
    wrapper.vm.handleOpenAuth()
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.showAuthModal).toBe(true)
    expect(wrapper.find('.stub').exists()).toBe(true)
    expect(mechaAudio.playHeavyImpactDrop).toHaveBeenCalled()
  })

  it('handles login success', () => {
    const wrapper = mountLayout()
    wrapper.vm.handleOpenAuth()
    wrapper.vm.handleLoginSuccess({ fullName: 'New User' })
    expect(wrapper.vm.communityUser).toEqual({ fullName: 'New User' })
    expect(wrapper.vm.showAuthModal).toBe(false)
    expect(mechaAudio.playEngage).toHaveBeenCalled()
  })

  it('handles community logout', () => {
    localStorage.setItem('vshield_community_user', JSON.stringify({ fullName: 'New User' }))
    const wrapper = mountLayout()
    wrapper.vm.handleOpenAuth()
    wrapper.vm.handleLoginSuccess({ fullName: 'New User' })
    wrapper.vm.handleLogoutCommunity()
    expect(wrapper.vm.communityUser).toBeNull()
    expect(localStorage.getItem('vshield_community_user')).toBeNull()
    expect(mechaAudio.playClick).toHaveBeenCalled()
  })

  it('handles global click on mecha-btn-hazard element', () => {
    const wrapper = mountLayout()
    const btn = document.createElement('button')
    btn.classList.add('mecha-btn-hazard')
    const evt = new MouseEvent('click', { bubbles: true })
    Object.defineProperty(evt, 'target', { value: btn })
    window.dispatchEvent(evt)
    expect(mechaAudio.playHeavyImpactDrop).toHaveBeenCalled()
  })

  it('handles global click on non-hazard interactive element', () => {
    const wrapper = mountLayout()
    const a = document.createElement('a')
    const evt = new MouseEvent('click', { bubbles: true })
    Object.defineProperty(evt, 'target', { value: a })
    window.dispatchEvent(evt)
    expect(mechaAudio.playClick).toHaveBeenCalled()
  })

  it('ignores global click on non-interactive element', () => {
    const wrapper = mountLayout()
    const div = document.createElement('div')
    const evt = new MouseEvent('click', { bubbles: true })
    Object.defineProperty(evt, 'target', { value: div })
    window.dispatchEvent(evt)
    expect(mechaAudio.playClick).not.toHaveBeenCalled()
  })

  it('handles global hover on interactive element', () => {
    const wrapper = mountLayout()
    const btn = document.createElement('button')
    const evt = new MouseEvent('mouseover', { bubbles: true })
    Object.defineProperty(evt, 'target', { value: btn })
    window.dispatchEvent(evt)
    expect(mechaAudio.playHover).toHaveBeenCalled()
  })
})
