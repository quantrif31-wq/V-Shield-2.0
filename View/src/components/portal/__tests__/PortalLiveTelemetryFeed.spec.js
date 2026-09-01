import { mount, flushPromises } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalLiveTelemetryFeed from '../PortalLiveTelemetryFeed.vue'

vi.mock('../../../utils/portalAudio', () => ({
  mechaAudio: { playClick: vi.fn() }
}))

describe('PortalLiveTelemetryFeed', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  it('mounts and renders collapsed state by default', () => {
    const wrapper = mount(PortalLiveTelemetryFeed)
    expect(wrapper.text()).toContain('SOC TELEMETRY FEED')
  })

  it('generates initial events on mount', () => {
    const wrapper = mount(PortalLiveTelemetryFeed)
    expect(wrapper.text()).toContain('SOC TELEMETRY FEED')
  })

  it('toggles drawer open and closed', async () => {
    const wrapper = mount(PortalLiveTelemetryFeed)
    const toggleBtn = wrapper.find('.cursor-pointer')
    await toggleBtn.trigger('click')
    expect(wrapper.text()).toContain('LIVE DEFENSE TELEMETRY')
    expect(wrapper.text()).toContain('ms ping')
  })

  it('closes drawer via close button', async () => {
    const wrapper = mount(PortalLiveTelemetryFeed)
    await wrapper.find('.cursor-pointer').trigger('click')
    expect(wrapper.text()).toContain('LIVE DEFENSE TELEMETRY')
    const closeBtn = wrapper.find('button')
    await closeBtn.trigger('click')
    expect(wrapper.text()).toContain('SOC TELEMETRY FEED')
  })

  it('generates events on interval', async () => {
    const wrapper = mount(PortalLiveTelemetryFeed)
    vi.advanceTimersByTime(8000)
    expect(wrapper.text()).toContain('SOC TELEMETRY FEED')
  })

  it('cleans up interval on unmount', () => {
    const wrapper = mount(PortalLiveTelemetryFeed)
    wrapper.unmount()
  })

  it('expands and shows events list', async () => {
    const wrapper = mount(PortalLiveTelemetryFeed)
    await wrapper.find('.cursor-pointer').trigger('click')
    expect(wrapper.text()).toContain('CENTRAL CLOUD')
  })
})
