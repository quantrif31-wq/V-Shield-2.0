// Web Audio API Procedural Synthesizer for Mecha Tactical SFX & Ambient Synth BGM

class MechaSoundEngine {
  constructor() {
    this.ctx = null
    this.sfxEnabled = true
    this.bgmEnabled = false
    this.bgmNodes = []
    this.initFromStorage()
  }

  initFromStorage() {
    try {
      const savedSfx = localStorage.getItem('vshield_portal_audio')
      this.sfxEnabled = savedSfx !== null ? savedSfx === 'true' : true
      const savedBgm = localStorage.getItem('vshield_portal_bgm')
      this.bgmEnabled = savedBgm === 'true'
    } catch {
      this.sfxEnabled = true
      this.bgmEnabled = false
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

  // --- AMBIENT CYBER SYNTH BGM ---
  startBgm() {
    const ctx = this.getContext()
    if (!ctx || this.bgmNodes.length > 0) return

    try {
      this.bgmEnabled = true
      localStorage.setItem('vshield_portal_bgm', 'true')

      const masterGain = ctx.createGain()
      masterGain.gain.setValueAtTime(0.04, ctx.currentTime)
      masterGain.connect(ctx.destination)

      // 1. Sub Drone
      const osc1 = ctx.createOscillator()
      osc1.type = 'sawtooth'
      osc1.frequency.setValueAtTime(55, ctx.currentTime) // A1 note

      const filter1 = ctx.createBiquadFilter()
      filter1.type = 'lowpass'
      filter1.frequency.setValueAtTime(220, ctx.currentTime)

      osc1.connect(filter1)
      filter1.connect(masterGain)
      osc1.start()

      // 2. Harmonic Fifth Drone
      const osc2 = ctx.createOscillator()
      osc2.type = 'sine'
      osc2.frequency.setValueAtTime(82.4, ctx.currentTime) // E2 note

      const gain2 = ctx.createGain()
      gain2.gain.setValueAtTime(0.6, ctx.currentTime)

      osc2.connect(gain2)
      gain2.connect(masterGain)
      osc2.start()

      // 3. Modulating LFO for slow breathing cyber pulse
      const lfo = ctx.createOscillator()
      lfo.frequency.setValueAtTime(0.12, ctx.currentTime)
      const lfoGain = ctx.createGain()
      lfoGain.gain.setValueAtTime(80, ctx.currentTime)
      lfo.connect(lfoGain)
      lfoGain.connect(filter1.frequency)
      lfo.start()

      this.bgmNodes = [osc1, osc2, lfo, masterGain]
    } catch (_) {}
  }

  stopBgm() {
    this.bgmEnabled = false
    try {
      localStorage.setItem('vshield_portal_bgm', 'false')
      this.bgmNodes.forEach(node => {
        try {
          if (node.stop) node.stop()
          if (node.disconnect) node.disconnect()
        } catch (_) {}
      })
      this.bgmNodes = []
    } catch (_) {}
  }

  toggleBgm() {
    if (this.bgmEnabled) {
      this.stopBgm()
      return false
    } else {
      this.startBgm()
      return true
    }
  }

  // --- INTERACTIVE SFX ---
  playHover() {
    if (!this.sfxEnabled) return
    const ctx = this.getContext()
    if (!ctx) return

    try {
      const now = ctx.currentTime
      const osc = ctx.createOscillator()
      const gain = ctx.createGain()

      osc.type = 'sine'
      osc.frequency.setValueAtTime(750, now)
      osc.frequency.exponentialRampToValueAtTime(1300, now + 0.04)

      gain.gain.setValueAtTime(0.025, now)
      gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.04)

      osc.connect(gain)
      gain.connect(ctx.destination)

      osc.start(now)
      osc.stop(now + 0.04)
    } catch (_) {}
  }

  playClick() {
    if (!this.sfxEnabled) return
    const ctx = this.getContext()
    if (!ctx) return

    try {
      const now = ctx.currentTime
      const osc = ctx.createOscillator()
      const gain = ctx.createGain()

      osc.type = 'triangle'
      osc.frequency.setValueAtTime(1200, now)
      osc.frequency.exponentialRampToValueAtTime(350, now + 0.08)

      gain.gain.setValueAtTime(0.08, now)
      gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.08)

      osc.connect(gain)
      gain.connect(ctx.destination)

      osc.start(now)
      osc.stop(now + 0.08)
    } catch (_) {}
  }

  playTargetLock() {
    if (!this.sfxEnabled) return
    const ctx = this.getContext()
    if (!ctx) return

    try {
      const now = ctx.currentTime
      const osc = ctx.createOscillator()
      const gain = ctx.createGain()

      osc.type = 'sawtooth'
      osc.frequency.setValueAtTime(1600, now)
      osc.frequency.setValueAtTime(2200, now + 0.03)

      gain.gain.setValueAtTime(0.035, now)
      gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.07)

      osc.connect(gain)
      gain.connect(ctx.destination)

      osc.start(now)
      osc.stop(now + 0.07)
    } catch (_) {}
  }

  playEngage() {
    if (!this.sfxEnabled) return
    const ctx = this.getContext()
    if (!ctx) return

    try {
      const now = ctx.currentTime
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

  playMagneticSnap() {
    if (!this.sfxEnabled) return
    const ctx = this.getContext()
    if (!ctx) return

    try {
      const now = ctx.currentTime
      const osc = ctx.createOscillator()
      const gain = ctx.createGain()
      osc.type = 'sine'
      osc.frequency.setValueAtTime(400, now)
      osc.frequency.exponentialRampToValueAtTime(900, now + 0.03)
      gain.gain.setValueAtTime(0.02, now)
      gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.03)
      osc.connect(gain)
      gain.connect(ctx.destination)
      osc.start(now)
      osc.stop(now + 0.03)
    } catch (_) {}
  }
}

export const mechaAudio = new MechaSoundEngine()
