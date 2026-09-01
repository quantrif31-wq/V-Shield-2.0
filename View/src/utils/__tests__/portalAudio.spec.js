import { describe, it, expect, beforeEach, afterEach, vi } from "vitest"

function createMockOscillator() {
  return {
    type: "",
    frequency: {
      setValueAtTime: vi.fn(),
      exponentialRampToValueAtTime: vi.fn()
    },
    connect: vi.fn(),
    start: vi.fn(),
    stop: vi.fn(),
    disconnect: vi.fn()
  }
}

function createMockGain() {
  return {
    gain: {
      setValueAtTime: vi.fn(),
      exponentialRampToValueAtTime: vi.fn()
    },
    connect: vi.fn(),
    disconnect: vi.fn()
  }
}

function createMockFilter() {
  return {
    type: "",
    frequency: {
      setValueAtTime: vi.fn()
    },
    connect: vi.fn(),
    disconnect: vi.fn()
  }
}

function installMockContext({ suspended = false } = {}) {
  globalThis.window.AudioContext = function AudioContextMock() {
    return {
      state: suspended ? "suspended" : "running",
      currentTime: 10,
      destination: { name: "dest" },
      resume: vi.fn().mockResolvedValue(undefined),
      createOscillator: () => createMockOscillator(),
      createGain: () => createMockGain(),
      createBiquadFilter: () => createMockFilter()
    }
  }
  globalThis.window.webkitAudioContext = undefined
}

describe("portalAudio", () => {
  let mechaAudio

  beforeEach(async () => {
    vi.resetModules()
    if (!globalThis.window) globalThis.window = {}
    globalThis.localStorage = {
      getItem: vi.fn(() => null),
      setItem: vi.fn()
    }
    const mod = await import("../portalAudio")
    mechaAudio = mod.mechaAudio
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it("exports a mechaAudio singleton", () => {
    expect(mechaAudio).toBeDefined()
    expect(mechaAudio.sfxEnabled).toBe(true)
    expect(mechaAudio.bgmEnabled).toBe(false)
  })

  it("initFromStorage reads persisted sfx setting", async () => {
    globalThis.localStorage.getItem = vi.fn(key => (key === "vshield_portal_audio" ? "false" : null))
    vi.resetModules()
    const mod = await import("../portalAudio")
    expect(mod.mechaAudio.sfxEnabled).toBe(false)
    expect(mod.mechaAudio.bgmEnabled).toBe(false)
  })

  it("initFromStorage reads persisted bgm setting as true", async () => {
    globalThis.localStorage.getItem = vi.fn(key => (key === "vshield_portal_bgm" ? "true" : null))
    vi.resetModules()
    const mod = await import("../portalAudio")
    expect(mod.mechaAudio.bgmEnabled).toBe(true)
  })

  it("initFromStorage falls back to defaults when storage throws", async () => {
    globalThis.localStorage.getItem = vi.fn(() => {
      throw new Error("denied")
    })
    vi.resetModules()
    const mod = await import("../portalAudio")
    expect(mod.mechaAudio.sfxEnabled).toBe(true)
    expect(mod.mechaAudio.bgmEnabled).toBe(false)
  })

  it("getContext returns context and resumes when suspended", () => {
    installMockContext({ suspended: true })
    const ctx = mechaAudio.getContext()
    expect(ctx).toBeTruthy()
    expect(ctx.resume).toHaveBeenCalled()
  })

  it("getContext returns null when no AudioContext available", () => {
    globalThis.window.AudioContext = undefined
    globalThis.window.webkitAudioContext = undefined
    expect(mechaAudio.getContext()).toBeNull()
  })

  it("getContext creates context via webkitAudioContext fallback", () => {
    window.AudioContext = undefined
    window.webkitAudioContext = function WebkitMock() {
      return { state: "running", resume: vi.fn() }
    }
    const ctx = mechaAudio.getContext()
    expect(ctx).toBeTruthy()
  })

  it("startBgm builds a BGM graph and persists flag", () => {
    installMockContext()
    mechaAudio.bgmNodes = []
    mechaAudio.startBgm()
    expect(mechaAudio.bgmEnabled).toBe(true)
    expect(mechaAudio.bgmNodes.length).toBeGreaterThan(0)
    expect(globalThis.localStorage.setItem).toHaveBeenCalledWith("vshield_portal_bgm", "true")
  })

  it("startBgm is no-op when context missing", () => {
    globalThis.window.AudioContext = undefined
    globalThis.window.webkitAudioContext = undefined
    mechaAudio.bgmNodes = []
    mechaAudio.startBgm()
    expect(mechaAudio.bgmNodes).toEqual([])
  })

  it("startBgm does not duplicate when already playing", () => {
    installMockContext()
    mechaAudio.bgmNodes = [{}]
    const before = mechaAudio.bgmNodes.length
    mechaAudio.startBgm()
    expect(mechaAudio.bgmNodes.length).toBe(before)
  })

  it("startBgm swallows errors from graph construction", () => {
    installMockContext()
    window.AudioContext = function AudioContextMock() {
      return {
        state: "running",
        currentTime: 0,
        destination: {},
        resume: vi.fn(),
        createOscillator: () => {
          throw new Error("boom")
        },
        createGain: () => createMockGain(),
        createBiquadFilter: () => createMockFilter()
      }
    }
    mechaAudio.bgmNodes = []
    expect(() => mechaAudio.startBgm()).not.toThrow()
  })

  it("stopBgm disables bgm, persists false, and cleans nodes", () => {
    installMockContext()
    mechaAudio.bgmNodes = [{ stop: vi.fn(), disconnect: vi.fn() }]
    mechaAudio.bgmEnabled = true
    mechaAudio.stopBgm()
    expect(mechaAudio.bgmEnabled).toBe(false)
    expect(mechaAudio.bgmNodes).toEqual([])
    expect(globalThis.localStorage.setItem).toHaveBeenCalledWith("vshield_portal_bgm", "false")
  })

  it("stopBgm swallows node errors", () => {
    installMockContext()
    mechaAudio.bgmNodes = [{ stop: vi.fn(() => { throw new Error("x") }), disconnect: vi.fn() }]
    expect(() => mechaAudio.stopBgm()).not.toThrow()
  })

  it("toggleBgm starts when off and stops when on", () => {
    installMockContext()
    mechaAudio.bgmEnabled = false
    mechaAudio.bgmNodes = []
    const started = mechaAudio.toggleBgm()
    expect(started).toBe(true)
    expect(mechaAudio.bgmEnabled).toBe(true)

    const stopped = mechaAudio.toggleBgm()
    expect(stopped).toBe(false)
    expect(mechaAudio.bgmEnabled).toBe(false)
  })

  it("playHover does nothing when sfx disabled", () => {
    installMockContext()
    mechaAudio.sfxEnabled = false
    expect(() => mechaAudio.playHover()).not.toThrow()
  })

  it("playHover plays sound", () => {
    installMockContext()
    mechaAudio.sfxEnabled = true
    expect(() => mechaAudio.playHover()).not.toThrow()
  })

  it("playClick plays sound", () => {
    installMockContext()
    mechaAudio.sfxEnabled = true
    expect(() => mechaAudio.playClick()).not.toThrow()
  })

  it("playTargetLock plays sound", () => {
    installMockContext()
    expect(() => mechaAudio.playTargetLock()).not.toThrow()
  })

  it("playEngage plays two-tone sound", () => {
    installMockContext()
    expect(() => mechaAudio.playEngage()).not.toThrow()
  })

  it("playMagneticSnap plays sound", () => {
    installMockContext()
    expect(() => mechaAudio.playMagneticSnap()).not.toThrow()
  })

  it("playHeavyImpactDrop plays sound", () => {
    installMockContext()
    expect(() => mechaAudio.playHeavyImpactDrop()).not.toThrow()
  })

  it("sfx methods swallow context construction errors", () => {
    installMockContext()
    mechaAudio.sfxEnabled = true
    window.AudioContext = function AudioContextMock() {
      return {
        state: "running",
        currentTime: 0,
        destination: {},
        resume: vi.fn(),
        createOscillator: () => {
          throw new Error("boom")
        },
        createGain: () => createMockGain(),
        createBiquadFilter: () => createMockFilter()
      }
    }
    expect(() => mechaAudio.playClick()).not.toThrow()
    expect(() => mechaAudio.playHover()).not.toThrow()
    expect(() => mechaAudio.playTargetLock()).not.toThrow()
    expect(() => mechaAudio.playEngage()).not.toThrow()
    expect(() => mechaAudio.playMagneticSnap()).not.toThrow()
    expect(() => mechaAudio.playHeavyImpactDrop()).not.toThrow()
  })

  it("sfx methods are no-ops with no context", () => {
    globalThis.window.AudioContext = undefined
    window.webkitAudioContext = undefined
    mechaAudio.sfxEnabled = true
    expect(() => mechaAudio.playHover()).not.toThrow()
    expect(() => mechaAudio.playClick()).not.toThrow()
    expect(() => mechaAudio.playTargetLock()).not.toThrow()
    expect(() => mechaAudio.playEngage()).not.toThrow()
    expect(() => mechaAudio.playMagneticSnap()).not.toThrow()
    expect(() => mechaAudio.playHeavyImpactDrop()).not.toThrow()
  })
})
