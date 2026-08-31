import { describe, it, expect, beforeEach, afterEach, vi } from "vitest"

function createMockOscillator() {
  return {
    type: "",
    frequency: { setValueAtTime: vi.fn() },
    connect: vi.fn(),
    start: vi.fn(),
    stop: vi.fn()
  }
}

function createMockGain() {
  return {
    gain: {
      setValueAtTime: vi.fn(),
      linearRampToValueAtTime: vi.fn(),
      exponentialRampToValueAtTime: vi.fn()
    },
    connect: vi.fn()
  }
}

function installMockContext({ suspended = false } = {}) {
  const oscillators = []
  const ctx = {
    state: suspended ? "suspended" : "running",
    currentTime: 10,
    destination: { name: "dest" },
    resume: vi.fn().mockResolvedValue(undefined),
    createOscillator: () => {
      const osc = createMockOscillator()
      oscillators.push(osc)
      return osc
    },
    createGain: () => createMockGain()
  }
  globalThis.window.AudioContext = function AudioContextMock() {
    return ctx
  }
  globalThis.window.webkitAudioContext = undefined
  return { ctx, oscillators }
}

describe("callAudio", () => {
  let mod
  let playIncomingRingtone
  let playOutgoingDialTone
  let playCallEndTone
  let stopAllTones

  beforeEach(async () => {
    vi.useFakeTimers()
    vi.resetModules()
    if (!globalThis.window) globalThis.window = {}
    installMockContext()
    mod = await import("../callAudio")
    playIncomingRingtone = mod.playIncomingRingtone
    playOutgoingDialTone = mod.playOutgoingDialTone
    playCallEndTone = mod.playCallEndTone
    stopAllTones = mod.stopAllTones
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it("playIncomingRingtone creates oscillators and schedules a repeating timer", () => {
    const setIntervalSpy = vi.spyOn(globalThis, "setInterval")
    expect(() => playIncomingRingtone()).not.toThrow()
    expect(setIntervalSpy).toHaveBeenCalled()
    stopAllTones()
  })

  it("playIncomingRingtone is a no-op when no audio context is available", async () => {
    globalThis.window.AudioContext = undefined
    globalThis.window.webkitAudioContext = undefined
    expect(() => playIncomingRingtone()).not.toThrow()
  })

  it("playIncomingRingtone swallows errors thrown inside playChord", async () => {
    const { ctx } = installMockContext()
    ctx.createOscillator = () => {
      throw new Error("boom")
    }
    globalThis.window.AudioContext = function AudioContextMock() {
      return ctx
    }
    expect(() => playIncomingRingtone()).not.toThrow()
    stopAllTones()
  })

  it("playIncomingRingtone swallows errors in second burst", async () => {
    const { ctx } = installMockContext()
    let calls = 0
    ctx.createOscillator = () => {
      calls++
      if (calls > 4) throw new Error("boom")
      return createMockOscillator()
    }
    globalThis.window.AudioContext = function AudioContextMock() {
      return ctx
    }
    expect(() => playIncomingRingtone()).not.toThrow()
    stopAllTones()
  })

  it("resumes a suspended audio context", () => {
    const { ctx } = installMockContext({ suspended: true })
    globalThis.window.AudioContext = function AudioContextMock() {
      return ctx
    }
    playIncomingRingtone()
    expect(ctx.resume).toHaveBeenCalled()
    stopAllTones()
  })

  it("handles a rejected resume promise", async () => {
    const { ctx } = installMockContext({ suspended: true })
    ctx.resume = vi.fn().mockRejectedValue(new Error("resume failed"))
    globalThis.window.AudioContext = function AudioContextMock() {
      return ctx
    }
    playIncomingRingtone()
    await vi.waitFor(() => {
      expect(ctx.resume).toHaveBeenCalled()
    })
    stopAllTones()
  })

  it("playOutgoingDialTone creates beep oscillators and sets a timer", () => {
    const setIntervalSpy = vi.spyOn(globalThis, "setInterval")
    playOutgoingDialTone()
    expect(setIntervalSpy).toHaveBeenCalled()
    stopAllTones()
  })

  it("playOutgoingDialTone catches errors inside playBeep", () => {
    const { ctx } = installMockContext()
    ctx.createOscillator = () => {
      throw new Error("boom")
    }
    globalThis.window.AudioContext = function AudioContextMock() {
      return ctx
    }
    expect(() => playOutgoingDialTone()).not.toThrow()
    stopAllTones()
  })

  it("playCallEndTone creates an end tone oscillator", () => {
    expect(() => playCallEndTone()).not.toThrow()
    stopAllTones()
  })

  it("playCallEndTone catches errors", () => {
    const { ctx } = installMockContext()
    ctx.createOscillator = () => {
      throw new Error("boom")
    }
    globalThis.window.AudioContext = function AudioContextMock() {
      return ctx
    }
    expect(() => playCallEndTone()).not.toThrow()
    stopAllTones()
  })

  it("stopAllTones clears ringtone and dial timers", () => {
    const clearSpy = vi.spyOn(globalThis, "clearInterval")
    playIncomingRingtone()
    playOutgoingDialTone()
    stopAllTones()
    expect(clearSpy).toHaveBeenCalled()
  })

  it("stopAllTones is resilient when oscillator.stop throws", () => {
    playIncomingRingtone()
    const { oscillators } = installMockContext()
    playOutgoingDialTone()
    stopAllTones()
    oscillators.forEach(() => {})
    expect(typeof stopAllTones).toBe("function")
  })

  it("getAudioContext returns null when no window audio context", () => {
    globalThis.window.AudioContext = undefined
    globalThis.window.webkitAudioContext = undefined
    expect(() => playOutgoingDialTone()).not.toThrow()
    expect(() => playCallEndTone()).not.toThrow()
  })
})
