import { describe, expect, it } from 'vitest'
import { normalizeRemoteIceCandidate } from '../cameraPeerRelay'

describe('cameraPeerRelay ICE normalization', () => {
  it('accepts the raw candidate line emitted by go2rtc', () => {
    expect(normalizeRemoteIceCandidate('candidate:1 1 UDP 2122260223 192.168.1.10 5000 typ host')).toEqual({
      candidate: 'candidate:1 1 UDP 2122260223 192.168.1.10 5000 typ host',
      sdpMLineIndex: 0,
    })
  })

  it('preserves a browser-style JSON candidate', () => {
    expect(normalizeRemoteIceCandidate('{"candidate":"candidate:2","sdpMid":"0","sdpMLineIndex":0}')).toEqual({
      candidate: 'candidate:2', sdpMid: '0', sdpMLineIndex: 0,
    })
  })
})
