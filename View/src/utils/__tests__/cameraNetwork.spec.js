import { describe, expect, it } from 'vitest'
import {
  buildCameraHealthProbeUrl,
  extractCameraDisplayParts,
  isBrowserVideoCameraUrl,
  isHlsCameraUrl,
  isHttpCameraUrl,
  isKnownStreamPreviewUrl,
  isRtspCameraUrl,
  looksLikeHostInput,
  normalizeCameraUrl,
  resolveCameraPreviewUrl,
  resolveCameraSourceUrl,
  shouldAppendPreviewCacheBust,
} from '../cameraNetwork'

describe('cameraNetwork URL classifiers', () => {
  it('detects http/rtsp camera urls', () => {
    expect(isHttpCameraUrl('http://10.0.0.1/video')).toBe(true)
    expect(isHttpCameraUrl('https://cam.example/live')).toBe(true)
    expect(isHttpCameraUrl('rtsp://10.0.0.1/stream')).toBe(false)
    expect(isRtspCameraUrl('rtsp://10.0.0.1/stream')).toBe(true)
    expect(isRtspCameraUrl('http://x')).toBe(false)
  })

  it('detects hls and browser video urls only over http(s)', () => {
    expect(isHlsCameraUrl('http://10.0.0.1/hls/stream.m3u8')).toBe(true)
    expect(isHlsCameraUrl('http://10.0.0.1/index.m3u8?x=1')).toBe(true)
    expect(isHlsCameraUrl('rtsp://x/stream.m3u8')).toBe(false)
    expect(isBrowserVideoCameraUrl('http://10.0.0.1/clip.mp4')).toBe(true)
    expect(isBrowserVideoCameraUrl('http://10.0.0.1/clip.webm')).toBe(true)
    expect(isBrowserVideoCameraUrl('http://10.0.0.1/clip.ogg')).toBe(true)
    expect(isBrowserVideoCameraUrl('http://10.0.0.1/feed')).toBe(false)
  })

  it('recognizes host-like text input', () => {
    expect(looksLikeHostInput('192.168.1.10')).toBe(true)
    expect(looksLikeHostInput('camera-01:8080/video')).toBe(true)
    expect(looksLikeHostInput('rtsp://x/y')).toBe(false)
  })
})

describe('cameraNetwork normalizeCameraUrl', () => {
  it('normalizes protocol-less host input to http', () => {
    expect(normalizeCameraUrl('192.168.1.10')).toBe('http://192.168.1.10/')
  })

  it('maps known preview ports to standard paths', () => {
    expect(normalizeCameraUrl('http://10.0.0.5:8081')).toBe('http://10.0.0.5:8081/video')
    expect(normalizeCameraUrl('http://10.0.0.5:8080')).toBe('http://10.0.0.5:8080/videofeed')
  })

  it('keeps unknown ports and non-http schemes untouched', () => {
    expect(normalizeCameraUrl('http://10.0.0.5:554/live')).toBe('http://10.0.0.5:554/live')
    expect(normalizeCameraUrl('rtsp://user:pass@host/stream')).toBe('rtsp://user:pass@host/stream')
  })

  it('returns empty for blank input and raw value for unparseable', () => {
    expect(normalizeCameraUrl('')).toBe('')
    expect(normalizeCameraUrl('   ')).toBe('')
    expect(normalizeCameraUrl('http://')).toBe('http://')
  })
})

describe('cameraNetwork preview helpers', () => {
  it('detects known stream preview paths', () => {
    expect(isKnownStreamPreviewUrl('http://10.0.0.5/video')).toBe(true)
    expect(isKnownStreamPreviewUrl('http://10.0.0.5/videofeed')).toBe(true)
    expect(isKnownStreamPreviewUrl('http://10.0.0.5/feed')).toBe(false)
    expect(isKnownStreamPreviewUrl('http://')).toBe(false)
    expect(isKnownStreamPreviewUrl('rtsp://x/y')).toBe(false)
  })

  it('builds a health probe url by stripping preview paths', () => {
    expect(buildCameraHealthProbeUrl('http://10.0.0.5/video')).toBe('http://10.0.0.5/')
    expect(buildCameraHealthProbeUrl('http://10.0.0.5/videofeed')).toBe('http://10.0.0.5/')
    expect(buildCameraHealthProbeUrl('http://10.0.0.5/misc')).toBe('http://10.0.0.5/misc')
    expect(buildCameraHealthProbeUrl('rtsp://x/y')).toBe('')
    expect(buildCameraHealthProbeUrl('http://')).toBe('http://')
  })

  it('appends cache-bust only for http urls outside known preview paths', () => {
    expect(shouldAppendPreviewCacheBust('http://10.0.0.5/misc')).toBe(true)
    expect(shouldAppendPreviewCacheBust('http://10.0.0.5/video')).toBe(false)
    expect(shouldAppendPreviewCacheBust('rtsp://x/y')).toBe(false)
  })

  it('resolves preview/source urls with defined fallback order', () => {
    expect(resolveCameraPreviewUrl({ previewUrl: '  http://a  ', url: 'http://b' })).toBe('http://a')
    expect(resolveCameraPreviewUrl({ url: 'http://b' })).toBe('http://b')
    expect(resolveCameraPreviewUrl(null)).toBe('')
    expect(resolveCameraSourceUrl({ url: 'http://b', previewUrl: 'http://a' })).toBe('http://b')
    expect(resolveCameraSourceUrl({ previewUrl: 'http://a' })).toBe('http://a')
  })

  it('extracts display parts from object or string input', () => {
    expect(extractCameraDisplayParts({ name: 'CAM-01', label: 'Cổng A' })).toEqual({
      slotName: 'CAM-01',
      sourceName: 'Cổng A',
    })
    expect(extractCameraDisplayParts('CAM-02', 2)).toEqual({ slotName: 'CAM-02', sourceName: '' })
    expect(extractCameraDisplayParts(null, 3)).toEqual({ slotName: 'CAM-03', sourceName: '' })
  })
})
