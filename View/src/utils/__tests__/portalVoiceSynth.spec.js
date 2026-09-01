import { describe, it, expect, beforeEach, afterEach, vi } from "vitest"

describe("portalVoiceSynth", () => {
  let mod
  let tacticalVoice

  beforeEach(async () => {
    vi.resetModules()
    if (!globalThis.window) globalThis.window = {}
    const speakers = []
    globalThis.window.speechSynthesis = {
      cancel: vi.fn(),
      speak: vi.fn(u => speakers.push(u)),
      getVoices: vi.fn(() => [
        { lang: "en-US", name: "Google US English" },
        { lang: "vi-VN", name: "Vietnamese Female" }
      ]),
      onvoiceschanged: undefined
    }
    globalThis.window.SpeechSynthesisUtterance = function SpeechSynthesisUtteranceMock(text) {
      this.text = text
      this.voice = null
      this.pitch = 1
      this.rate = 1
      this.volume = 1
    }
    const imported = await import("../portalVoiceSynth")
    mod = imported
    tacticalVoice = imported.tacticalVoice
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it("exports a tacticalVoice singleton", () => {
    expect(tacticalVoice).toBeDefined()
    expect(tacticalVoice.enabled).toBe(true)
  })

  it("initVoice prefers an english natural voice", () => {
    expect(tacticalVoice.voice).toBeTruthy()
    expect(tacticalVoice.voice.name).toBe("Google US English")
  })

  it("speak cancels and creates utterance with pitch/rate/volume", () => {
    tacticalVoice.speak("hello")
    expect(window.speechSynthesis.cancel).toHaveBeenCalled()
    expect(window.speechSynthesis.speak).toHaveBeenCalled()
  })

  it("speak does nothing when disabled", () => {
    tacticalVoice.enabled = false
    tacticalVoice.speak("hello")
    expect(window.speechSynthesis.speak).not.toHaveBeenCalled()
  })

  it("speak swallows errors", () => {
    window.speechSynthesis.speak = vi.fn(() => {
      throw new Error("boom")
    })
    const utt = window.SpeechSynthesisUtterance
    expect(() => tacticalVoice.speak("hello")).not.toThrow()
    window.SpeechSynthesisUtterance = utt
  })

  it("speakSystemBoot sends boot message", () => {
    tacticalVoice.speak = vi.fn()
    tacticalVoice.speakSystemBoot()
    expect(tacticalVoice.speak).toHaveBeenCalled()
  })

  it("speakOverdriveEngaged sends overdrive message", () => {
    tacticalVoice.speak = vi.fn()
    tacticalVoice.speakOverdriveEngaged()
    expect(tacticalVoice.speak).toHaveBeenCalled()
  })

  it("speakTargetLocked uses provided name", () => {
    tacticalVoice.speak = vi.fn()
    tacticalVoice.speakTargetLocked("Zone A")
    expect(tacticalVoice.speak).toHaveBeenCalledWith(expect.stringContaining("Zone A"))
  })

  it("speakTargetLocked uses default target", () => {
    tacticalVoice.speak = vi.fn()
    tacticalVoice.speakTargetLocked()
    expect(tacticalVoice.speak).toHaveBeenCalled()
  })

  it("speakDownloadStarted sends download message", () => {
    tacticalVoice.speak = vi.fn()
    tacticalVoice.speakDownloadStarted()
    expect(tacticalVoice.speak).toHaveBeenCalled()
  })

  it("speakSignalTransmitted sends signal message", () => {
    tacticalVoice.speak = vi.fn()
    tacticalVoice.speakSignalTransmitted()
    expect(tacticalVoice.speak).toHaveBeenCalled()
  })

  it("speak no-ops when synth undefined", async () => {
    vi.resetModules()
    delete globalThis.window.speechSynthesis
    const imported = await import("../portalVoiceSynth")
    expect(() => imported.tacticalVoice.speak("hi")).not.toThrow()
  })

  it("initVoice handles onvoiceschanged callback", async () => {
    vi.resetModules()
    let handler
    globalThis.window.speechSynthesis = {
      cancel: vi.fn(),
      speak: vi.fn(),
      getVoices: vi.fn(() => [{ lang: "en-US", name: "Samantha" }]),
      onvoiceschanged: null
    }
    Object.defineProperty(window.speechSynthesis, "onvoiceschanged", {
      set(v) {
        handler = v
      },
      get() {
        return handler
      }
    })
    const imported = await import("../portalVoiceSynth")
    expect(imported.tacticalVoice.voice).toBeTruthy()
    if (handler) handler()
  })
})
