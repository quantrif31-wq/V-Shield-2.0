<template>
  <div v-if="callState.state !== 'idle'" class="global-call-root">
    <!-- 1. INCOMING CALL MODAL (Zalo Style) -->
    <transition name="call-pop">
      <div v-if="callState.state === 'incoming'" class="call-overlay-backdrop">
        <div class="incoming-call-box">
          <div class="caller-avatar-wrapper">
            <div class="pulse-ring ring-1"></div>
            <div class="pulse-ring ring-2"></div>
            <div class="caller-avatar" :style="{ background: getAvatarColor(callState.fromFullName) }">
              {{ (callState.fromFullName || 'V').charAt(0).toUpperCase() }}
            </div>
          </div>
          <h2 class="caller-name">{{ callState.fromFullName }}</h2>
          <div class="call-type-tag">
            <i :class="callState.callType === 'video' ? 'fas fa-video' : 'fas fa-phone-alt'"></i>
            <span>{{ callState.callType === 'video' ? 'Cuộc gọi Video Face-to-Face...' : 'Cuộc gọi thoại HD...' }}</span>
          </div>

          <div class="call-buttons-row">
            <button class="action-btn btn-decline" @click="rejectCall" title="Từ chối cuộc gọi">
              <i class="fas fa-phone-slash"></i>
              <span>Từ chối</span>
            </button>
            <button class="action-btn btn-answer" @click="acceptCall" title="Trả lời">
              <i :class="callState.callType === 'video' ? 'fas fa-video' : 'fas fa-phone'"></i>
              <span>Trả lời</span>
            </button>
          </div>
        </div>
      </div>
    </transition>

    <!-- 2. OUTGOING CALL MODAL -->
    <transition name="call-pop">
      <div v-if="callState.state === 'calling'" class="call-overlay-backdrop">
        <div class="outgoing-call-box">
          <div class="caller-avatar-wrapper">
            <div class="pulse-ring ring-1"></div>
            <div class="caller-avatar" :style="{ background: getAvatarColor(callState.targetFullName) }">
              {{ (callState.targetFullName || 'V').charAt(0).toUpperCase() }}
            </div>
          </div>
          <h2 class="caller-name">{{ callState.targetFullName }}</h2>
          <div class="call-type-tag">
            <i class="fas fa-spinner fa-spin"></i>
            <span>Đang đổ chuông...</span>
          </div>

          <!-- Video preview if video call -->
          <div v-if="callState.callType === 'video'" class="outgoing-video-preview">
            <video ref="outgoingPreviewRef" autoplay playsinline muted></video>
          </div>

          <div class="call-buttons-row">
            <button class="action-btn btn-decline" @click="endCall" title="Hủy cuộc gọi">
              <i class="fas fa-phone-slash"></i>
              <span>Hủy</span>
            </button>
          </div>
        </div>
      </div>
    </transition>

    <!-- 3. CONNECTED FACE-TO-FACE VIDEO / AUDIO CALL WINDOW -->
    <transition name="call-pop">
      <div v-if="callState.state === 'connected' && !callState.isPip" class="call-overlay-backdrop">
        <div class="face-to-face-modal" :class="{ 'audio-only-modal': callState.callType === 'audio' }">
          <!-- Top Bar -->
          <div class="call-top-bar">
            <div class="call-peer-info">
              <span class="live-pulse"></span>
              <h4>{{ callState.targetFullName || callState.fromFullName }}</h4>
              <span class="call-duration-text">{{ formatCallDuration(callState.callDuration) }}</span>
            </div>
            <div class="call-top-actions">
              <button class="icon-btn-ghost" @click="togglePip" title="Thu nhỏ góc màn hình">
                <i class="fas fa-compress-alt"></i>
              </button>
            </div>
          </div>

          <!-- Media Viewport -->
          <div class="call-viewport">
            <template v-if="callState.callType === 'video'">
              <video ref="remoteVideoRef" class="remote-video-screen" autoplay playsinline></video>
              <div v-if="!callState.isVideoOff" class="local-pip-box">
                <video ref="localVideoRef" class="local-pip-screen" autoplay playsinline muted></video>
              </div>
              <div v-else class="local-pip-box camera-off-placeholder">
                <i class="fas fa-video-slash"></i>
                <span>Camera đã tắt</span>
              </div>
            </template>

            <!-- Audio Call Display -->
            <template v-else>
              <div class="voice-call-stage">
                <div class="voice-avatar" :style="{ background: getAvatarColor(callState.targetFullName || callState.fromFullName) }">
                  {{ ((callState.targetFullName || callState.fromFullName) || 'V').charAt(0).toUpperCase() }}
                </div>
                <div class="voice-sound-waves">
                  <span v-for="n in 7" :key="n" :style="{ animationDelay: `${n * 0.12}s` }"></span>
                </div>
                <p class="voice-status-text">Đang trong cuộc gọi thoại HD</p>
              </div>
            </template>

            <audio ref="remoteAudioRef" autoplay :muted="callState.isSpeakerMuted"></audio>
          </div>

          <!-- Controls Bar -->
          <div class="call-controls-island">
            <button class="ctrl-btn" :class="{ 'is-active': callState.isMuted }" @click="toggleMic" :title="callState.isMuted ? 'Bật Mic' : 'Tắt Mic'">
              <i :class="callState.isMuted ? 'fas fa-microphone-slash' : 'fas fa-microphone'"></i>
            </button>

            <button v-if="callState.callType === 'video'" class="ctrl-btn" :class="{ 'is-active': callState.isVideoOff }" @click="toggleCamera" :title="callState.isVideoOff ? 'Bật Camera' : 'Tắt Camera'">
              <i :class="callState.isVideoOff ? 'fas fa-video-slash' : 'fas fa-video'"></i>
            </button>

            <button class="ctrl-btn" :class="{ 'is-active': callState.isSpeakerMuted }" @click="toggleSpeaker" :title="callState.isSpeakerMuted ? 'Bật Loa' : 'Tắt Loa'">
              <i :class="callState.isSpeakerMuted ? 'fas fa-volume-mute' : 'fas fa-volume-up'"></i>
            </button>

            <button class="ctrl-btn btn-hangup-large" @click="endCall" title="Kết thúc cuộc gọi">
              <i class="fas fa-phone-slash"></i>
            </button>
          </div>
        </div>
      </div>
    </transition>

    <!-- 4. FLOATING MINI PiP BUBBLE (When Minimized) -->
    <div v-if="callState.state === 'connected' && callState.isPip" class="floating-pip-bubble animate-in">
      <div class="pip-media-container" @click="togglePip">
        <video v-if="callState.callType === 'video'" ref="remoteVideoPipRef" class="pip-video-preview" autoplay playsinline></video>
        <div v-else class="pip-audio-preview">
          <i class="fas fa-phone-alt"></i>
        </div>
        <div class="pip-info-badge">
          <span class="dot"></span> {{ formatCallDuration(callState.callDuration) }}
        </div>
      </div>
      <div class="pip-controls">
        <button class="pip-btn" :class="{ muted: callState.isMuted }" @click.stop="toggleMic">
          <i :class="callState.isMuted ? 'fas fa-microphone-slash' : 'fas fa-microphone'"></i>
        </button>
        <button class="pip-btn pip-btn-expand" @click.stop="togglePip" title="Mở rộng">
          <i class="fas fa-expand-alt"></i>
        </button>
        <button class="pip-btn pip-btn-end" @click.stop="endCall" title="Ngắt kết nối">
          <i class="fas fa-phone-slash"></i>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, nextTick, onMounted } from 'vue'
import {
  callState,
  localMediaStream,
  remoteMediaStream,
  initGlobalCallListener,
  acceptCall,
  rejectCall,
  endCall,
  toggleMic,
  toggleCamera,
  togglePip,
  toggleSpeaker,
  formatCallDuration
} from '../../stores/callStore'

const remoteVideoRef = ref(null)
const localVideoRef = ref(null)
const remoteAudioRef = ref(null)
const outgoingPreviewRef = ref(null)
const remoteVideoPipRef = ref(null)

onMounted(() => {
  initGlobalCallListener()
})

watch(localMediaStream, (stream) => {
  nextTick(() => {
    if (localVideoRef.value && stream) {
      localVideoRef.value.srcObject = stream
    }
    if (outgoingPreviewRef.value && stream) {
      outgoingPreviewRef.value.srcObject = stream
    }
  })
})

watch(remoteMediaStream, (stream) => {
  nextTick(() => {
    if (remoteVideoRef.value && stream) {
      remoteVideoRef.value.srcObject = stream
    }
    if (remoteAudioRef.value && stream) {
      remoteAudioRef.value.srcObject = stream
    }
    if (remoteVideoPipRef.value && stream) {
      remoteVideoPipRef.value.srcObject = stream
    }
  })
})

watch(() => callState.isPip, (isPip) => {
  if (isPip) {
    nextTick(() => {
      if (remoteVideoPipRef.value && remoteMediaStream.value) {
        remoteVideoPipRef.value.srcObject = remoteMediaStream.value
      }
    })
  } else {
    nextTick(() => {
      if (remoteVideoRef.value && remoteMediaStream.value) {
        remoteVideoRef.value.srcObject = remoteMediaStream.value
      }
      if (localVideoRef.value && localMediaStream.value) {
        localVideoRef.value.srcObject = localMediaStream.value
      }
    })
  }
})

function getAvatarColor(name) {
  const colors = ['#1976D2','#388E3C','#D32F2F','#F57C00','#7B1FA2','#00796B','#5C6BC0','#E64A19','#C2185B','#303F9F']
  let hash = 0
  for (let i = 0; i < (name || '').length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash)
  return colors[Math.abs(hash) % colors.length]
}
</script>

<style scoped>
.global-call-root {
  position: relative;
  z-index: 10000;
}

.call-overlay-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(10, 15, 29, 0.78);
  backdrop-filter: blur(12px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10001;
}

/* ========================================= */
/* INCOMING & OUTGOING CARDS                 */
/* ========================================= */
.incoming-call-box,
.outgoing-call-box {
  background: var(--surface-raised, #ffffff);
  color: var(--text-primary);
  border: 1px solid var(--border-subtle);
  border-radius: 28px;
  padding: 40px 32px;
  width: 90%;
  max-width: 420px;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.45);
  animation: call-scale-up 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.caller-avatar-wrapper {
  position: relative;
  width: 96px;
  height: 96px;
  margin-bottom: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.caller-avatar {
  width: 96px;
  height: 96px;
  border-radius: 50%;
  color: #ffffff;
  font-size: 40px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.25);
  z-index: 2;
}

.pulse-ring {
  position: absolute;
  inset: 0;
  border-radius: 50%;
  border: 2px solid var(--accent-primary, #0f7c82);
  opacity: 0.8;
  animation: ring-pulse 2s cubic-bezier(0.215, 0.61, 0.355, 1) infinite;
}

.ring-2 {
  animation-delay: 0.7s;
}

@keyframes ring-pulse {
  0% { transform: scale(1); opacity: 0.9; }
  100% { transform: scale(1.6); opacity: 0; }
}

.caller-name {
  margin: 0 0 6px;
  font-size: 24px;
  font-weight: 700;
}

.call-type-tag {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  color: var(--text-muted);
  margin-bottom: 32px;
}

.outgoing-video-preview {
  width: 140px;
  height: 95px;
  border-radius: 12px;
  overflow: hidden;
  margin-bottom: 24px;
  border: 2px solid var(--border-subtle);
  background: #000;
}

.outgoing-video-preview video {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transform: scaleX(-1);
}

.call-buttons-row {
  display: flex;
  gap: 32px;
  justify-content: center;
}

.action-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 16px 26px;
  border-radius: 20px;
  border: none;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: transform 0.2s, filter 0.2s;
}

.action-btn i {
  font-size: 26px;
}

.action-btn:hover {
  transform: translateY(-2px);
  filter: brightness(1.12);
}

.btn-decline {
  background: #ef4444;
  color: #ffffff;
  box-shadow: 0 8px 20px rgba(239, 68, 68, 0.35);
}

.btn-answer {
  background: #10b981;
  color: #ffffff;
  box-shadow: 0 8px 20px rgba(16, 185, 129, 0.35);
}

/* ========================================= */
/* CONNECTED FACE-TO-FACE WINDOW             */
/* ========================================= */
.face-to-face-modal {
  background: #0b1120;
  color: #ffffff;
  border-radius: 28px;
  overflow: hidden;
  width: 92%;
  max-width: 900px;
  height: 86vh;
  max-height: 680px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 30px 70px rgba(0, 0, 0, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.12);
  position: relative;
}

.audio-only-modal {
  max-width: 500px;
  max-height: 520px;
}

.call-top-bar {
  padding: 16px 24px;
  background: rgba(15, 23, 42, 0.85);
  backdrop-filter: blur(8px);
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  z-index: 10;
}

.call-peer-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.call-peer-info h4 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}

.live-pulse {
  width: 9px;
  height: 9px;
  border-radius: 50%;
  background: #10b981;
  box-shadow: 0 0 10px #10b981;
}

.call-duration-text {
  font-size: 13px;
  color: #94a3b8;
  padding: 2px 8px;
  background: rgba(255, 255, 255, 0.08);
  border-radius: 6px;
}

.icon-btn-ghost {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  border: 1px solid rgba(255, 255, 255, 0.15);
  background: rgba(255, 255, 255, 0.06);
  color: #ffffff;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.icon-btn-ghost:hover {
  background: rgba(255, 255, 255, 0.18);
}

.call-viewport {
  flex: 1;
  position: relative;
  background: #020617;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.remote-video-screen {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.local-pip-box {
  position: absolute;
  bottom: 24px;
  right: 24px;
  width: 180px;
  height: 125px;
  border-radius: 16px;
  overflow: hidden;
  border: 2px solid rgba(255, 255, 255, 0.35);
  box-shadow: 0 14px 28px rgba(0, 0, 0, 0.55);
  background: #1e293b;
  z-index: 5;
}

.local-pip-screen {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transform: scaleX(-1);
}

.camera-off-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  color: #94a3b8;
  font-size: 11px;
}

.voice-call-stage {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 20px;
  color: #94a3b8;
}

.voice-avatar {
  width: 120px;
  height: 120px;
  border-radius: 50%;
  color: #ffffff;
  font-size: 52px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 14px 36px rgba(0, 0, 0, 0.5);
}

.voice-sound-waves {
  display: flex;
  align-items: center;
  gap: 6px;
  height: 28px;
}

.voice-sound-waves span {
  width: 4px;
  background: #38bdf8;
  border-radius: 3px;
  animation: sound-wave 1.2s ease-in-out infinite;
}

@keyframes sound-wave {
  0%, 100% { transform: scaleY(0.3); height: 8px; }
  50% { transform: scaleY(1.4); height: 26px; }
}

.voice-status-text {
  font-size: 14px;
  color: #cbd5e1;
}

.call-controls-island {
  padding: 20px 24px;
  background: rgba(15, 23, 42, 0.95);
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 22px;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
  z-index: 10;
}

.ctrl-btn {
  width: 54px;
  height: 54px;
  border-radius: 50%;
  border: none;
  background: rgba(255, 255, 255, 0.12);
  color: #ffffff;
  font-size: 20px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.ctrl-btn:hover {
  background: rgba(255, 255, 255, 0.22);
  transform: scale(1.06);
}

.ctrl-btn.is-active {
  background: #ef4444;
  color: #ffffff;
}

.btn-hangup-large {
  background: #ef4444;
  color: #ffffff;
  width: 60px;
  height: 60px;
  box-shadow: 0 10px 24px rgba(239, 68, 68, 0.4);
}

.btn-hangup-large:hover {
  background: #dc2626;
  transform: scale(1.1);
}

/* ========================================= */
/* FLOATING PiP BUBBLE (MINIMIZED)           */
/* ========================================= */
.floating-pip-bubble {
  position: fixed;
  bottom: 24px;
  right: 24px;
  width: 220px;
  height: 160px;
  background: #0f172a;
  border-radius: 18px;
  border: 2px solid rgba(255, 255, 255, 0.25);
  box-shadow: 0 16px 36px rgba(0, 0, 0, 0.6);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  z-index: 10002;
  cursor: pointer;
}

.pip-media-container {
  flex: 1;
  position: relative;
  background: #020617;
  overflow: hidden;
}

.pip-video-preview {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.pip-audio-preview {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #38bdf8;
  font-size: 32px;
}

.pip-info-badge {
  position: absolute;
  top: 8px;
  left: 8px;
  background: rgba(0, 0, 0, 0.65);
  padding: 2px 6px;
  border-radius: 6px;
  font-size: 11px;
  color: #ffffff;
  display: flex;
  align-items: center;
  gap: 4px;
}

.pip-info-badge .dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #10b981;
}

.pip-controls {
  padding: 6px 10px;
  background: rgba(15, 23, 42, 0.95);
  display: flex;
  justify-content: space-around;
  align-items: center;
}

.pip-btn {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: none;
  background: rgba(255, 255, 255, 0.12);
  color: #ffffff;
  font-size: 12px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}

.pip-btn.muted {
  background: #ef4444;
}

.pip-btn-end {
  background: #ef4444;
}

@keyframes call-scale-up {
  from { opacity: 0; transform: scale(0.92); }
  to { opacity: 1; transform: scale(1); }
}
</style>
