// Web Audio API Ringtone and Dial Tone Synthesizer
// 100% Free, zero external files, zero API keys

let audioCtx = null
let ringtoneTimer = null
let dialTimer = null
let activeOscillators = []

function getAudioContext() {
  if (!audioCtx) {
    const AudioContextClass = window.AudioContext || window.webkitAudioContext
    if (AudioContextClass) {
      audioCtx = new AudioContextClass()
    }
  }
  if (audioCtx && audioCtx.state === 'suspended') {
    audioCtx.resume().catch(() => {})
  }
  return audioCtx
}

export function playIncomingRingtone() {
  stopAllTones()
  const ctx = getAudioContext()
  if (!ctx) return

  const playChord = () => {
    try {
      const now = ctx.currentTime
      const notes = [523.25, 659.25, 783.99, 1046.5] // C5, E5, G5, C6 (Pleasing melodic ringtone)

      notes.forEach((freq, idx) => {
        const osc = ctx.createOscillator()
        const gain = ctx.createGain()

        osc.type = 'sine'
        osc.frequency.setValueAtTime(freq, now + idx * 0.08)

        gain.gain.setValueAtTime(0, now + idx * 0.08)
        gain.gain.linearRampToValueAtTime(0.12, now + idx * 0.08 + 0.04)
        gain.gain.exponentialRampToValueAtTime(0.001, now + idx * 0.08 + 0.5)

        osc.connect(gain)
        gain.connect(ctx.destination)

        osc.start(now + idx * 0.08)
        osc.stop(now + idx * 0.08 + 0.55)
        activeOscillators.push(osc)
      })

      // Second melodic burst after 0.5s
      const secondBurst = [659.25, 783.99, 1046.5, 1318.5]
      secondBurst.forEach((freq, idx) => {
        const osc = ctx.createOscillator()
        const gain = ctx.createGain()

        osc.type = 'sine'
        osc.frequency.setValueAtTime(freq, now + 0.5 + idx * 0.08)

        gain.gain.setValueAtTime(0, now + 0.5 + idx * 0.08)
        gain.gain.linearRampToValueAtTime(0.12, now + 0.5 + idx * 0.08 + 0.04)
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.5 + idx * 0.08 + 0.6)

        osc.connect(gain)
        gain.connect(ctx.destination)

        osc.start(now + 0.5 + idx * 0.08)
        osc.stop(now + 0.5 + idx * 0.08 + 0.65)
        activeOscillators.push(osc)
      })
    } catch (e) {
      console.warn('Audio play error', e)
    }
  }

  playChord()
  ringtoneTimer = setInterval(playChord, 2200)
}

export function playOutgoingDialTone() {
  stopAllTones()
  const ctx = getAudioContext()
  if (!ctx) return

  const playBeep = () => {
    try {
      const now = ctx.currentTime
      const freqs = [440, 480] // Standard dial tone pair

      freqs.forEach(freq => {
        const osc = ctx.createOscillator()
        const gain = ctx.createGain()

        osc.type = 'sine'
        osc.frequency.setValueAtTime(freq, now)

        gain.gain.setValueAtTime(0, now)
        gain.gain.linearRampToValueAtTime(0.08, now + 0.05)
        gain.gain.setValueAtTime(0.08, now + 0.95)
        gain.gain.linearRampToValueAtTime(0.001, now + 1.0)

        osc.connect(gain)
        gain.connect(ctx.destination)

        osc.start(now)
        osc.stop(now + 1.05)
        activeOscillators.push(osc)
      })
    } catch (e) {}
  }

  playBeep()
  dialTimer = setInterval(playBeep, 2500)
}

export function playCallEndTone() {
  stopAllTones()
  const ctx = getAudioContext()
  if (!ctx) return

  try {
    const now = ctx.currentTime
    const osc = ctx.createOscillator()
    const gain = ctx.createGain()

    osc.type = 'sine'
    osc.frequency.setValueAtTime(425, now)

    gain.gain.setValueAtTime(0.15, now)
    gain.gain.setValueAtTime(0, now + 0.15)
    gain.gain.setValueAtTime(0.15, now + 0.25)
    gain.gain.setValueAtTime(0, now + 0.4)
    gain.gain.setValueAtTime(0.15, now + 0.5)
    gain.gain.setValueAtTime(0, now + 0.65)

    osc.connect(gain)
    gain.connect(ctx.destination)

    osc.start(now)
    osc.stop(now + 0.7)
  } catch (e) {}
}

export function stopAllTones() {
  if (ringtoneTimer) {
    clearInterval(ringtoneTimer)
    ringtoneTimer = null
  }
  if (dialTimer) {
    clearInterval(dialTimer)
    dialTimer = null
  }
  activeOscillators.forEach(osc => {
    try { osc.stop() } catch (e) {}
  })
  activeOscillators = []
}
