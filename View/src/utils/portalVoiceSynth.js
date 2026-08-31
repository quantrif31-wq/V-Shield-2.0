// Web Speech API Tactical AI Cockpit Voice Synthesizer

class TacticalVoiceEngine {
  constructor() {
    this.enabled = true
    this.synth = typeof window !== 'undefined' ? window.speechSynthesis : null
    this.voice = null
    this.initVoice()
  }

  initVoice() {
    if (!this.synth) return
    const setVoice = () => {
      const voices = this.synth.getVoices()
      // Prefer crisp female English or natural system voices
      this.voice = voices.find(v => v.lang.includes('en') && (v.name.includes('Google') || v.name.includes('Natural') || v.name.includes('Samantha') || v.name.includes('Zira'))) || voices[0]
    }

    if (this.synth.onvoiceschanged !== undefined) {
      this.synth.onvoiceschanged = setVoice
    }
    setVoice()
  }

  speak(text) {
    if (!this.enabled || !this.synth) return
    try {
      this.synth.cancel() // Cancel previous utterances
      const utterance = new SpeechSynthesisUtterance(text)
      if (this.voice) utterance.voice = this.voice
      utterance.pitch = 1.05
      utterance.rate = 1.12
      utterance.volume = 0.8
      this.synth.speak(utterance)
    } catch (_) {}
  }

  speakSystemBoot() {
    this.speak('V-Shield Quantum Defense Matrix Initialized. All sectors armed.')
  }

  speakOverdriveEngaged() {
    this.speak('Hyper Overdrive Mode Engaged. Energy output at maximum.')
  }

  speakTargetLocked(targetName = 'Target') {
    this.speak(`Sector target locked: ${targetName}`)
  }

  speakDownloadStarted() {
    this.speak('Mobile Client APK package transmission started.')
  }

  speakSignalTransmitted() {
    this.speak('Telemetry signal transmitted to Central Command.')
  }
}

export const tacticalVoice = new TacticalVoiceEngine()
