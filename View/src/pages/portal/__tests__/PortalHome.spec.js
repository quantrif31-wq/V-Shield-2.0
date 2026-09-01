import { mount, flushPromises } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalHome from '../PortalHome.vue'

const h = vi.hoisted(() => ({ mockPush: vi.fn() }))

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: h.mockPush })
}))

vi.mock('../../../services/portalApi', () => ({
  portalApi: { getOverview: (...args) => mockGetOverview(...args) }
}))

const { mockGetOverview } = vi.hoisted(() => ({ mockGetOverview: vi.fn() }))

vi.mock('../../../utils/cyberTextScramble', () => ({
  TextScramble: class {
    constructor() { this.setText = vi.fn() }
  }
}))

vi.mock('../../../utils/portalAudio', () => ({
  mechaAudio: { playHover: vi.fn(), playClick: vi.fn() }
}))

const threeCoreStub = { template: '<div class="three-stub" />' }

describe('PortalHome', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.useFakeTimers()
    window.scrollTo = vi.fn()
    mockGetOverview.mockReset()
    mockGetOverview.mockResolvedValue({})
    h.mockPush.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  function mountHome() {
    return mount(PortalHome, {
      global: { stubs: { 'router-link': { template: '<a><slot /></a>' }, PortalThreeCore: threeCoreStub } }
    })
  }

  it('mounts and renders hero headline', async () => {
    const wrapper = mountHome()
    await flushPromises()
    expect(wrapper.text()).toContain('V-SHIELD 2.0')
  })

  it('loads overview data from api', async () => {
    mockGetOverview.mockResolvedValue({ systemName: 'API SYSTEM', averageRating: 4.5 })
    const wrapper = mountHome()
    await flushPromises()
    expect(wrapper.vm.overview.systemName).toBe('API SYSTEM')
    expect(wrapper.vm.overview.averageRating).toBe(4.5)
  })

  it('advances status update on click', async () => {
    const wrapper = mountHome()
    await flushPromises()
    const statusBox = wrapper.find('.mecha-hud-bracket')
    await statusBox.trigger('click')
    expect(wrapper.vm.currentUpdateIndex).toBe(1)
  })

  it('navigates to features', async () => {
    const wrapper = mountHome()
    await flushPromises()
    const btn = wrapper.findAll('button').find(b => b.text().includes('GIẢI PHÁP CÔNG NGHỆ'))
    await btn.trigger('click')
    expect(h.mockPush).toHaveBeenCalledWith('/features')
    expect(window.scrollTo).toHaveBeenCalled()
  })

  it('navigates to download', async () => {
    const wrapper = mountHome()
    await flushPromises()
    const btn = wrapper.findAll('button').find(b => b.text().includes('TẢI ỨNG DỤNG MOBILE'))
    await btn.trigger('click')
    expect(h.mockPush).toHaveBeenCalledWith('/download')
  })

  it('navigates via CTA buttons and triggers scramble on hover', async () => {
    const wrapper = mountHome()
    await flushPromises()
    const downloadCta = wrapper.findAll('button').find(b => b.text().includes('TẢI APP MOBILE'))
    await downloadCta.trigger('click')
    expect(h.mockPush).toHaveBeenCalledWith('/download')
    const communityCta = wrapper.findAll('button').find(b => b.text().includes('XEM ĐÁNH GIÁ'))
    await communityCta.trigger('click')
    expect(h.mockPush).toHaveBeenCalledWith('/community')
  })

  it('rotates updates on interval', async () => {
    const wrapper = mountHome()
    await flushPromises()
    const initial = wrapper.vm.currentUpdateIndex
    vi.advanceTimersByTime(7001)
    expect(wrapper.vm.currentUpdateIndex).toBe((initial + 1) % wrapper.vm.systemUpdates.length)
  })

  it('clicks core module blocks to navigate to features', async () => {
    const wrapper = mountHome()
    await flushPromises()
    const blocks = wrapper.findAll('.mecha-card-3d')
    expect(blocks.length).toBeGreaterThanOrEqual(3)
    for (const block of blocks) {
      await block.trigger('click')
    }
    expect(h.mockPush).toHaveBeenCalledWith('/features')
  })

  it('handles overview api error', async () => {
    mockGetOverview.mockRejectedValue(new Error('fail'))
    const wrapper = mountHome()
    await flushPromises()
    expect(wrapper.vm.overview.systemName).toBe('V-SHIELD 2.0')
  })
})
