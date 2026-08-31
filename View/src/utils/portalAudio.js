// Web Audio API Procedural Synthesizer for Mecha Tactical SFX

class MechaSoundEngine {
  constructor() {
    this.ctx = null
    this.enabled = true
    this.initFromStorage()
  }

  initFromStorage() {
    try {
      const saved = localStorage.getItem('vshield_portal_audio')
      this.enabled = saved !== null ? saved === 'true' : true
    } catch {
      this.enabled = true
    }
  }

  getContext() {
    if (!this.ctx && typeof window !== 'undefined') {
      const AudioContext = window.AudioContext || window.webkitAudioContext
      if (AudioContext) {
        this.ctx = new AudioContext()
      }
    }
    if (this.ctx && this.ctx.state === 'suspended') {
      this.ctx.resume()
    }
    return this.ctx
  }

  playHover() {
    if (!this.enabled) return
    const ctx = this.getContext()
    if (!ctx) return

    try {
      const now = ctx.currentTime
      const osc = ctx.createOscillator()
      const gain = ctx.createGain()

      osc.type = 'sine'
      osc.frequency.setValueAtTime(800, now)
      osc.frequency.exponentialRampToValueAtTime(1400, now + 0.04)

      gain.gain.setValueAtTime(0.03, now)
      gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.04)

      osc.connect(gain)
      gain.connect(ctx.destination)

      osc.start(now)
      osc.stop(now + 0.04)
    } catch (_) {}
  }

  playClick() {
    if (!this.enabled) return
    const ctx = this.getContext()
    if (!ctx) return

    try {
      const now = ctx.currentTime
      const osc = ctx.createOscillator()
      const gain = ctx.createGain()

      osc.type = 'triangle'
      osc.frequency.setValueAtTime(1200, now)
      osc.frequency.exponentialRampToValueAtTime(400, now + 0.08)

      gain.gain.setValueAtTime(0.08, now)
      gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.08)

      osc.connect(gain)
      gain.connect(ctx.destination)

      osc.start(now)
      osc.stop(now + 0.08)
    } catch (_) {}
  }

  playTargetLock() {
    if (!this.enabled) return
    const ctx = this.getContext()
    if (!ctx) return

    try {
      const now = ctx.currentTime
      const osc = ctx.createOscillator()
      const gain = ctx.createGain()

      osc.type = 'sawtooth'
      osc.frequency.setValueAtTime(1800, now)
      osc.frequency.setValueAtTime(2400, now + 0.03)

      gain.gain.setValueAtTime(0.04, now)
      gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.07)

      osc.connect(gain)
      gain.connect(ctx.destination)

      osc.start(now)
      osc.stop(now + 0.07)
    } catch (_) {}
  }

  playEngage() {
    if (!this.enabled) return
    const ctx = this.getContext()
    if (!ctx) return

    try {
      const now = ctx.currentTime
      // Low sub bass thump
      const oscSub = ctx.createOscillator()
      const gainSub = ctx.createGain()
      oscSub.type = 'sine'
      oscSub.frequency.setValueAtTime(180, now)
      oscSub.frequency.exponentialRampToValueAtTime(40, now + 0.25)
      gainSub.gain.setValueAtTime(0.12, now)
      gainSub.gain.exponentialRampToValueAtTime(0.0001, now + 0.25)
      oscSub.connect(gainSub)
      gainSub.connect(ctx.destination)
      oscSub.start(now)
      oscSub.stop(now + 0.25)

      // High cyber sweep
      const oscHi = ctx.createOscillator()
      const gainHi = ctx.createGain()
      oscHi.type = 'sine'
      oscHi.frequency.setValueAtTime(600, now)
      oscHi.frequency.exponentialRampToValueAtTime(1600, now + 0.15)
      gainHi.gain.setValueAtTime(0.06, now)
      gainHi.gain.exponentialRampToValueAtTime(0.0001, now + 0.15)
      oscHi.connect(gainHi)
      gainHi.connect(ctx.destination)
      oscHi.start(now)
      oscHi.stop(now + 0.15)
    } catch (_) {}
  }
}

export const mechaAudio = new MechaSoundEngine()
