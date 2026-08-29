<template>
  <div v-if="callState.state !== 'idle'" class="global-call-root">
    <!-- 1. INCOMING CALL BANNER (Zalo-Style Non-Intrusive Floating Card) -->
    <transition name="zalo-slide-down">
      <div v-if="callState.state === 'incoming' && !isIncomingBannerDismissed" class="zalo-incoming-banner-wrapper">
        <div class="zalo-incoming-card shadow-elevated">
          <div class="banner-avatar-col">
            <div class="avatar-ring-pulse"></div>
            <div class="banner-avatar" :style="{ background: getAvatarColor(callState.fromFullName) }">
              {{ (callState.fromFullName || 'V').charAt(0).toUpperCase() }}
            </div>
          </div>

          <div class="banner-info-col">
            <div class="banner-eyebrow">
              <span class="pulse-indicator-dot"></span>
              <span>Cuộc gọi đến...</span>
            </div>
            <h3 class="banner-caller-name">{{ callState.fromFullName }}</h3>
            <div class="banner-call-type">
              <svg v-if="callState.callType === 'video'" class="type-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                <polygon points="23 7 16 12 23 17 23 7"></polygon>
                <rect x="1" y="5" width="15" height="14" rx="2" ry="2"></rect>
              </svg>
              <svg v-else class="type-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                <path d="M22 16.92v3a2 2 0 01-2.18 2 19.79 19.79 0 01-8.63-3.07 19.5 19.5 0 01-6-6 19.79 19.79 0 01-3.07-8.67A2 2 0 014.11 2h3a2 2 0 012 1.72 12.84 12.84 0 00.7 2.81 2 2 0 01-.45 2.11L8.09 9.91a16 16 0 006 6l1.27-1.27a2 2 0 012.11-.45 12.84 12.84 0 002.81.7A2 2 0 0122 16.92z"></path>
              </svg>
              <span>{{ callState.callType === 'video' ? 'Cuộc gọi Video Face-to-Face' : 'Cuộc gọi Thoại HD' }}</span>
            </div>
          </div>

          <div class="banner-actions-col">
            <button class="zalo-btn btn-decline" @click="rejectCall" title="Từ chối cuộc gọi">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" width="18" height="18">
                <line x1="1" y1="1" x2="23" y2="23"></line>
                <path d="M16.5 16.5l-1.27 1.27a16 16 0 01-6-6L10.5 10.5"></path>
                <path d="M10.6 5.3A2 2 0 0112 5h7a2 2 0 012 2v3a2 2 0 01-2 2h-1"></path>
                <path d="M2 4.27a2 2 0 012-1.27h3a2 2 0 012 1.72 12.84 12.84 0 00.7 2.81 2 2 0 01-.45 2.11L8.09 9.91"></path>
              </svg>
            </button>
            <button class="zalo-btn btn-answer" @click="handleAccept" title="Trả lời ngay">
              <svg v-if="callState.callType === 'video'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" width="18" height="18">
                <polygon points="23 7 16 12 23 17 23 7"></polygon>
                <rect x="1" y="5" width="15" height="14" rx="2" ry="2"></rect>
              </svg>
              <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" width="18" height="18">
                <path d="M22 16.92v3a2 2 0 01-2.18 2 19.79 19.79 0 01-8.63-3.07 19.5 19.5 0 01-6-6 19.79 19.79 0 01-3.07-8.67A2 2 0 014.11 2h3a2 2 0 012 1.72 12.84 12.84 0 00.7 2.81 2 2 0 01-.45 2.11L8.09 9.91a16 16 0 006 6l1.27-1.27a2 2 0 012.11-.45 12.84 12.84 0 002.81.7A2 2 0 0122 16.92z"></path>
              </svg>
            </button>
            <button class="zalo-btn btn-ignore" @click="dismissBanner" title="Tiếp tục làm việc (Ẩn thông báo)">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16">
                <line x1="18" y1="6" x2="6" y2="18"></line>
                <line x1="6" y1="6" x2="18" y2="18"></line>
              </svg>
            </button>
          </div>
        </div>
      </div>
    </transition>

    <!-- 2. OUTGOING CALLING DIALOG -->
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
            <span class="spinner-ring"></span>
            <span>Đang đổ chuông...</span>
          </div>

          <div v-if="callState.callType === 'video'" class="outgoing-video-preview">
            <video ref="outgoingPreviewRef" autoplay playsinline muted></video>
          </div>

          <div class="call-buttons-row">
            <button class="action-btn btn-decline" @click="endCall" title="Hủy cuộc gọi">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" width="22" height="22">
                <line x1="1" y1="1" x2="23" y2="23"></line>
                <path d="M16.5 16.5l-1.27 1.27a16 16 0 01-6-6L10.5 10.5"></path>
                <path d="M10.6 5.3A2 2 0 0112 5h7a2 2 0 012 2v3a2 2 0 01-2 2h-1"></path>
                <path d="M2 4.27a2 2 0 012-1.27h3a2 2 0 012 1.72 12.84 12.84 0 00.7 2.81 2 2 0 01-.45 2.11L8.09 9.91"></path>
              </svg>
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
              <button class="icon-btn-ghost" @click="togglePip" title="Thu nhỏ góc màn hình để tiếp tục làm việc">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="18" height="18">
                  <polyline points="4 14 10 14 10 20"></polyline>
                  <polyline points="20 10 14 10 14 4"></polyline>
                  <line x1="14" y1="10" x2="21" y2="3"></line>
                  <line x1="3" y1="21" x2="10" y2="14"></line>
                </svg>
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
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="28" height="28">
                  <line x1="1" y1="1" x2="23" y2="23"></line>
                  <path d="M21 21l-3.34-3.34L16 16.5 7.5 8 3 3.5"></path>
                  <polygon points="23 7 16 12 23 17 23 7"></polygon>
                  <rect x="1" y="5" width="15" height="14" rx="2" ry="2"></rect>
                </svg>
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
              <svg v-if="callState.isMuted" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="22" height="22">
                <line x1="1" y1="1" x2="23" y2="23"></line>
                <path d="M9 9v3a3 3 0 005.12 2.12M15 9.34V4a3 3 0 00-5.94-.6"></path>
                <path d="M17 16.95A7 7 0 015 12v-2m14 0v2a7 7 0 01-.11 1.23"></path>
                <line x1="12" y1="19" x2="12" y2="23"></line>
                <line x1="8" y1="23" x2="16" y2="23"></line>
              </svg>
              <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="22" height="22">
                <path d="M12 1a3 3 0 00-3 3v8a3 3 0 006 0V4a3 3 0 00-3-3z"></path>
                <path d="M19 10v2a7 7 0 01-14 0v-2"></path>
                <line x1="12" y1="19" x2="12" y2="23"></line>
                <line x1="8" y1="23" x2="16" y2="23"></line>
              </svg>
            </button>

            <button v-if="callState.callType === 'video'" class="ctrl-btn" :class="{ 'is-active': callState.isVideoOff }" @click="toggleCamera" :title="callState.isVideoOff ? 'Bật Camera' : 'Tắt Camera'">
              <svg v-if="callState.isVideoOff" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="22" height="22">
                <line x1="1" y1="1" x2="23" y2="23"></line>
                <path d="M21 21l-3.34-3.34L16 16.5 7.5 8 3 3.5"></path>
                <polygon points="23 7 16 12 23 17 23 7"></polygon>
                <rect x="1" y="5" width="15" height="14" rx="2" ry="2"></rect>
              </svg>
              <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="22" height="22">
                <polygon points="23 7 16 12 23 17 23 7"></polygon>
                <rect x="1" y="5" width="15" height="14" rx="2" ry="2"></rect>
              </svg>
            </button>

            <button class="ctrl-btn" :class="{ 'is-active': callState.isSpeakerMuted }" @click="toggleSpeaker" :title="callState.isSpeakerMuted ? 'Bật Loa' : 'Tắt Loa'">
              <svg v-if="callState.isSpeakerMuted" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="22" height="22">
                <polygon points="11 5 6 9 2 9 2 15 6 15 11 19 11 5"></polygon>
                <line x1="23" y1="9" x2="17" y2="15"></line>
                <line x1="17" y1="9" x2="23" y2="15"></line>
              </svg>
              <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="22" height="22">
                <polygon points="11 5 6 9 2 9 2 15 6 15 11 19 11 5"></polygon>
                <path d="M19.07 4.93a10 10 0 010 14.14M15.54 8.46a5 5 0 010 7.07"></path>
              </svg>
            </button>

            <button class="ctrl-btn btn-hangup-large" @click="endCall" title="Kết thúc cuộc gọi">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" width="26" height="26">
                <line x1="1" y1="1" x2="23" y2="23"></line>
                <path d="M16.5 16.5l-1.27 1.27a16 16 0 01-6-6L10.5 10.5"></path>
                <path d="M10.6 5.3A2 2 0 0112 5h7a2 2 0 012 2v3a2 2 0 01-2 2h-1"></path>
                <path d="M2 4.27a2 2 0 012-1.27h3a2 2 0 012 1.72 12.84 12.84 0 00.7 2.81 2 2 0 01-.45 2.11L8.09 9.91"></path>
              </svg>
            </button>
          </div>
        </div>
      </div>
    </transition>

    <!-- 4. FLOATING MINI PiP BUBBLE (When Minimized) -->
    <div v-if="callState.state === 'connected' && callState.isPip" class="floating-pip-bubble animate-in">
      <div class="pip-media-container" @click="togglePip" title="Nhấp để mở rộng Face-to-Face">
        <video v-if="callState.callType === 'video'" ref="remoteVideoPipRef" class="pip-video-preview" autoplay playsinline></video>
        <div v-else class="pip-audio-preview">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="28" height="28">
            <path d="M22 16.92v3a2 2 0 01-2.18 2 19.79 19.79 0 01-8.63-3.07 19.5 19.5 0 01-6-6 19.79 19.79 0 01-3.07-8.67A2 2 0 014.11 2h3a2 2 0 012 1.72 12.84 12.84 0 00.7 2.81 2 2 0 01-.45 2.11L8.09 9.91a16 16 0 006 6l1.27-1.27a2 2 0 012.11-.45 12.84 12.84 0 002.81.7A2 2 0 0122 16.92z"></path>
          </svg>
        </div>
        <div class="pip-info-badge">
          <span class="dot"></span> {{ formatCallDuration(callState.callDuration) }}
        </div>
      </div>
      <div class="pip-controls">
        <button class="pip-btn" :class="{ muted: callState.isMuted }" @click.stop="toggleMic" :title="callState.isMuted ? 'Bật mic' : 'Tắt mic'">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="13" height="13">
            <path d="M12 1a3 3 0 00-3 3v8a3 3 0 006 0V4a3 3 0 00-3-3z"></path>
          </svg>
        </button>
        <button class="pip-btn pip-btn-expand" @click.stop="togglePip" title="Mở rộng Face-to-Face">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="13" height="13">
            <polyline points="15 3 21 3 21 9"></polyline>
            <polyline points="9 21 3 21 3 15"></polyline>
            <line x1="21" y1="3" x2="14" y2="10"></line>
            <line x1="3" y1="21" x2="10" y2="14"></line>
          </svg>
        </button>
        <button class="pip-btn pip-btn-end" @click.stop="endCall" title="Ngắt kết nối">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" width="13" height="13">
            <line x1="1" y1="1" x2="23" y2="23"></line>
            <path d="M16.5 16.5l-1.27 1.27a16 16 0 01-6-6L10.5 10.5"></path>
          </svg>
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
const isIncomingBannerDismissed = ref(false)

onMounted(() => {
  initGlobalCallListener()
})

watch(() => callState.state, (newState) => {
  if (newState === 'incoming') {
    isIncomingBannerDismissed.value = false
  }
})

function dismissBanner() {
  isIncomingBannerDismissed.value = true
}

function handleAccept() {
  acceptCall()
}

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

/* ========================================= */
/* ZALO-STYLE NON-BLOCKING INCOMING BANNER   */
/* ========================================= */
.zalo-incoming-banner-wrapper {
  position: fixed;
  top: 18px;
  right: 22px;
  z-index: 10005;
  pointer-events: none;
}

.zalo-incoming-card {
  pointer-events: auto;
  width: 380px;
  background: var(--surface-raised, #ffffff);
  border: 1px solid var(--border-focus, rgba(15, 124, 130, 0.35));
  border-radius: 20px;
  padding: 14px 18px;
  display: flex;
  align-items: center;
  gap: 14px;
  box-shadow: 0 16px 40px rgba(0, 0, 0, 0.28), 0 0 0 1px rgba(15, 124, 130, 0.15);
  backdrop-filter: blur(12px);
  animation: zalo-bounce 0.35s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}

.banner-avatar-col {
  position: relative;
  width: 50px;
  height: 50px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.banner-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  color: #ffffff;
  font-weight: 700;
  font-size: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.2);
}

.avatar-ring-pulse {
  position: absolute;
  inset: -4px;
  border-radius: 50%;
  border: 2px solid #10b981;
  opacity: 0.8;
  animation: ring-pulse 1.8s ease-out infinite;
}

.banner-info-col {
  flex: 1;
  min-width: 0;
}

.banner-eyebrow {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  font-weight: 600;
  color: #10b981;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.pulse-indicator-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #10b981;
  box-shadow: 0 0 6px #10b981;
}

.banner-caller-name {
  margin: 2px 0 2px;
  font-size: 15px;
  font-weight: 700;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.banner-call-type {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  color: var(--text-muted);
}

.banner-actions-col {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.zalo-btn {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform 0.15s ease, filter 0.15s ease;
}

.zalo-btn:hover {
  transform: scale(1.08);
  filter: brightness(1.1);
}

.zalo-btn.btn-decline {
  background: #ef4444;
  color: #ffffff;
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.35);
}

.zalo-btn.btn-answer {
  background: #10b981;
  color: #ffffff;
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.35);
}

.zalo-btn.btn-ignore {
  background: var(--surface-subtle, #f1f5f9);
  color: var(--text-muted);
  width: 32px;
  height: 32px;
  border: 1px solid var(--border-subtle);
}

.zalo-btn.btn-ignore:hover {
  color: var(--text-primary);
  background: var(--surface-hover);
}

/* ========================================= */
/* OUTGOING CALL BACKDROP & CARD             */
/* ========================================= */
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

.outgoing-call-box {
  background: var(--surface-raised, #ffffff);
  color: var(--text-primary);
  border: 1px solid var(--border-subtle);
  border-radius: 28px;
  padding: 40px 32px;
  width: 90%;
  max-width: 400px;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.45);
  animation: call-scale-up 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.caller-avatar-wrapper {
  position: relative;
  width: 88px;
  height: 88px;
  margin-bottom: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.caller-avatar {
  width: 88px;
  height: 88px;
  border-radius: 50%;
  color: #ffffff;
  font-size: 36px;
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

.caller-name {
  margin: 0 0 6px;
  font-size: 22px;
  font-weight: 700;
}

.call-type-tag {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  color: var(--text-muted);
  margin-bottom: 28px;
}

.spinner-ring {
  width: 14px;
  height: 14px;
  border: 2px solid var(--border-subtle);
  border-top-color: var(--accent-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
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
  gap: 6px;
  padding: 14px 28px;
  border-radius: 20px;
  border: none;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: transform 0.2s, filter 0.2s;
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
  transform: scale(1.05);
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
  padding: 18px 24px;
  background: rgba(15, 23, 42, 0.95);
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 22px;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
  z-index: 10;
}

.ctrl-btn {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  border: none;
  background: rgba(255, 255, 255, 0.12);
  color: #ffffff;
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
  width: 58px;
  height: 58px;
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

@keyframes zalo-bounce {
  from { transform: translateY(-30px); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}

@keyframes ring-pulse {
  0% { transform: scale(1); opacity: 0.9; }
  100% { transform: scale(1.6); opacity: 0; }
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

@keyframes call-scale-up {
  from { opacity: 0; transform: scale(0.92); }
  to { opacity: 1; transform: scale(1); }
}
</style>
