import { mount, flushPromises } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalDownload from '../PortalDownload.vue'

const { mockGetOverview, mockToDataURL } = vi.hoisted(() => ({
  mockGetOverview: vi.fn(),
  mockToDataURL: vi.fn()
}))

vi.mock('../../../services/portalApi', () => ({
  portalApi: { getOverview: (...args) => mockGetOverview(...args) }
}))

vi.mock('qrcode', () => ({
  default: { toDataURL: (...args) => mockToDataURL(...args) }
}))

describe('PortalDownload', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockGetOverview.mockReset()
    mockToDataURL.mockReset()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('mounts and generates QR code with fallback', async () => {
    mockGetOverview.mockResolvedValue({ apkDownloadUrl: 'https://v-shield.site/custom.apk' })
    mockToDataURL.mockResolvedValue('data:image/png;base64,QR')
    const wrapper = mount(PortalDownload)
    await flushPromises()
    expect(wrapper.vm.apkDownloadUrl).toBe('https://v-shield.site/custom.apk')
    expect(wrapper.vm.qrDataUrl).toBe('data:image/png;base64,QR')
    expect(wrapper.find('img').attributes('src')).toBe('data:image/png;base64,QR')
  })

  it('shows loading state when no QR yet', async () => {
    mockGetOverview.mockResolvedValue({})
    let resolveQr
    mockToDataURL.mockImplementation(() => new Promise(r => { resolveQr = r }))
    const wrapper = mount(PortalDownload)
    await flushPromises()
    expect(wrapper.text()).toContain('ĐANG TẠO MÃ QR...')
    resolveQr('data:image/png;base64,QR')
    return Promise.resolve()
  })

  it('handles QR generation error', async () => {
    mockGetOverview.mockResolvedValue({})
    mockToDataURL.mockRejectedValue(new Error('qr fail'))
    const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
    const wrapper = mount(PortalDownload)
    await flushPromises()
    expect(wrapper.vm.qrDataUrl).toBe('')
    errorSpy.mockRestore()
  })

  it('handles getOverview failure gracefully', async () => {
    mockGetOverview.mockRejectedValue(new Error('fail'))
    mockToDataURL.mockResolvedValue('data:image/png;base64,QR')
    const wrapper = mount(PortalDownload)
    await flushPromises()
    expect(wrapper.vm.apkDownloadUrl).toBe('https://v-shield.site/downloads/VShield-Mobile-Latest.apk')
    expect(wrapper.vm.qrDataUrl).toBe('data:image/png;base64,QR')
  })
})
