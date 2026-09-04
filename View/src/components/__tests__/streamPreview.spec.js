import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest'
import { mount, shallowMount } from '@vue/test-utils'

import StreamPreview from '../StreamPreview.vue'

function mockLocation() {
  const fullscreenElement = { onchange: null }
  const req = vi.fn()
  const exit = vi.fn()
  Object.defineProperty(document, 'fullscreenElement', { configurable: true, get: () => fullscreenElement.value })
  document.exitFullscreen = exit
  return { fullscreenElement, req, exit }
}

beforeEach(() => {
  vi.restoreAllMocks()
  document.body.innerHTML = ''
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('StreamPreview.vue', () => {
  it('renders the empty message when no url is given', () => {
    const wrapper = shallowMount(StreamPreview, { props: { url: '' } })
    expect(wrapper.find('.preview-message').text()).toContain('Chưa có preview')
  })

  it('renders rtsp message for rtsp urls', () => {
    const wrapper = shallowMount(StreamPreview, { props: { url: 'rtsp://192.168.1.10:554/stream' } })
    expect(wrapper.find('.preview-message').text()).toContain('RTSP đã sẵn sàng cho AI')
  })

  it('renders unsupported message for unknown protocols', () => {
    const wrapper = shallowMount(StreamPreview, { props: { url: 'ftp://files' } })
    expect(wrapper.find('.preview-message').text()).toContain('Không hỗ trợ xem trực tiếp')
  })

  it('renders an image for http urls and reports ready', async () => {
    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/snapshot.jpg' } })
    const img = wrapper.find('img.stream-media')
    expect(img.exists()).toBe(true)
    expect(img.attributes('alt')).toBe('Camera preview')
    await img.trigger('load')
    expect(wrapper.emitted('ready')).toBeTruthy()
    expect(wrapper.vm.errorMessage).toBe('')
  })

  it('reports an error when the image fails to load', async () => {
    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/snapshot.jpg' } })
    await wrapper.find('img.stream-media').trigger('error')
    expect(wrapper.emitted('error')).toBeTruthy()
    expect(wrapper.vm.errorMessage).toContain('Không tải được ảnh preview')
  })

  it('attaches a browser video and plays it', async () => {
    global.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined)
    global.HTMLMediaElement.prototype.pause = vi.fn()
    global.HTMLMediaElement.prototype.load = vi.fn()
    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/feed.mp4' } })
    await flushPromises()
    const video = wrapper.find('video.stream-media')
    expect(video.exists()).toBe(true)
    expect(video.attributes('src')).toContain('feed.mp4')
    await video.trigger('loadeddata')
    expect(wrapper.emitted('ready')).toBeTruthy()
  })

  it('re-attaches a browser video when it ends', async () => {
    global.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined)
    global.HTMLMediaElement.prototype.pause = vi.fn()
    global.HTMLMediaElement.prototype.load = vi.fn()
    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/feed.mp4' } })
    await flushPromises()
    await wrapper.find('video.stream-media').trigger('ended')
    expect(wrapper.vm.errorMessage).toBe('')
    expect(global.HTMLMediaElement.prototype.play).toHaveBeenCalled()
  })

  it('reports a video error from the native element', async () => {
    global.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined)
    global.HTMLMediaElement.prototype.pause = vi.fn()
    global.HTMLMediaElement.prototype.load = vi.fn()
    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/feed.mp4' } })
    await flushPromises()
    await wrapper.find('video.stream-media').trigger('error')
    expect(wrapper.emitted('error')).toBeTruthy()
  })

  it('does nothing on dblclick when the stream is not ready', async () => {
    const el = { requestFullscreen: vi.fn() }
    const wrapper = mount(StreamPreview, {
      props: { url: 'http://camera.local/snapshot.jpg' },
    })
    wrapper.vm.errorMessage = 'boom'
    wrapper.vm.containerRef = el
    await wrapper.trigger('dblclick')
    expect(el.requestFullscreen).not.toHaveBeenCalled()
  })

  it('enters and exits fullscreen on dblclick', async () => {
    const el = { requestFullscreen: vi.fn().mockResolvedValue(undefined) }
    const exit = vi.fn().mockResolvedValue(undefined)
    document.exitFullscreen = exit
    let inFullscreen = false
    Object.defineProperty(document, 'fullscreenElement', {
      configurable: true,
      get: () => (inFullscreen ? {} : null),
    })
    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/snapshot.jpg' } })
    await wrapper.find('img.stream-media').trigger('load')
    wrapper.vm.containerRef = el
    await wrapper.trigger('dblclick')
    expect(el.requestFullscreen).toHaveBeenCalled()
    inFullscreen = true
    await wrapper.trigger('dblclick')
    expect(exit).toHaveBeenCalled()
  })

  it('prevents context menu and exits fullscreen on right click', async () => {
    const exit = vi.fn().mockResolvedValue(undefined)
    document.exitFullscreen = exit
    Object.defineProperty(document, 'fullscreenElement', {
      configurable: true,
      get: () => ({}),
    })
    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/snapshot.jpg' } })
    const evt = { preventDefault: vi.fn() }
    await wrapper.trigger('contextmenu', evt)
    expect(evt.preventDefault).toHaveBeenCalled()
    expect(exit).toHaveBeenCalled()
  })

  it('leaves the context menu alone when not in fullscreen', async () => {
    Object.defineProperty(document, 'fullscreenElement', {
      configurable: true,
      get: () => null,
    })
    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/snapshot.jpg' } })
    const evt = { preventDefault: vi.fn() }
    await wrapper.trigger('contextmenu', evt)
    expect(evt.preventDefault).not.toHaveBeenCalled()
  })

  it('loads hls.js dynamically for hls urls', async () => {
    global.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined)
    global.HTMLMediaElement.prototype.pause = vi.fn()
    global.HTMLMediaElement.prototype.load = vi.fn()
    const HlsMock = vi.fn().mockImplementation(function () {
      const inst = { on: vi.fn(), loadSource: vi.fn(), attachMedia: vi.fn(), destroy: vi.fn() }
      return inst
    })
    HlsMock.isSupported = () => true
    HlsMock.Events = { ERROR: 'error', MANIFEST_PARSED: 'manifest' }
    global.window.Hls = HlsMock
    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/live.m3u8' } })
    await flushPromises()
    expect(HlsMock).toHaveBeenCalled()
    const inst = HlsMock.mock.results[0].value
    const errCb = inst.on.mock.calls.find(([name]) => name === 'error')
    errCb[1]({}, { fatal: true })
    expect(wrapper.vm.errorMessage).toContain('HLS')
  })

  it('always shows a seek bar for DVR and derives its range from the HLS manifest', async () => {
    global.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined)
    global.HTMLMediaElement.prototype.pause = vi.fn()
    global.HTMLMediaElement.prototype.load = vi.fn()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      text: vi.fn().mockResolvedValue('#EXTM3U\n#EXTINF:4.0,\nsegment-1.m4s\n#EXTINF:4.0,\nsegment-2.m4s'),
    }))
    const HlsMock = vi.fn().mockImplementation(function () {
      return { on: vi.fn(), loadSource: vi.fn(), attachMedia: vi.fn(), destroy: vi.fn() }
    })
    HlsMock.isSupported = () => true
    HlsMock.Events = { ERROR: 'error', MANIFEST_PARSED: 'manifest' }
    global.window.Hls = HlsMock

    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/dvr/index.m3u8', dvrMode: true } })
    await flushPromises()
    await flushPromises()

    expect(wrapper.find('.dvr-transport').exists()).toBe(true)
    expect(wrapper.find('.dvr-scrubber').attributes('disabled')).toBeUndefined()
    expect(wrapper.find('.dvr-transport').text()).toContain('00:08')
  })

  it('fails gracefully when hls.js is unsupported', async () => {
    global.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined)
    global.HTMLMediaElement.prototype.pause = vi.fn()
    global.HTMLMediaElement.prototype.load = vi.fn()
    global.window.Hls = { isSupported: () => false }
    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/live.m3u8' } })
    await flushPromises()
    expect(wrapper.emitted('error')).toBeTruthy()
  })

  it('pauses video on deactivate and resumes on activate via keep-alive', async () => {
    global.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined)
    global.HTMLMediaElement.prototype.pause = vi.fn()
    global.HTMLMediaElement.prototype.load = vi.fn()
    const Host = {
      components: { StreamPreview },
      data: () => ({ show: true }),
      template: '<KeepAlive><StreamPreview v-if="show" url="http://camera.local/feed.mp4" /></KeepAlive>',
    }
    const wrapper = mount(Host)
    await flushPromises()
    expect(global.HTMLMediaElement.prototype.play).toHaveBeenCalled()
    wrapper.vm.show = false
    await wrapper.vm.$nextTick()
    expect(global.HTMLMediaElement.prototype.pause).toHaveBeenCalled()
    wrapper.vm.show = true
    await wrapper.vm.$nextTick()
    expect(global.HTMLMediaElement.prototype.play).toHaveBeenCalled()
    await wrapper.unmount()
  })

  it('cleans up on unmount', async () => {
    global.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined)
    global.HTMLMediaElement.prototype.pause = vi.fn()
    global.HTMLMediaElement.prototype.load = vi.fn()
    const wrapper = mount(StreamPreview, { props: { url: 'http://camera.local/feed.mp4' } })
    await flushPromises()
    await wrapper.unmount()
    expect(global.HTMLMediaElement.prototype.pause).toHaveBeenCalled()
  })
})

async function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0))
}
