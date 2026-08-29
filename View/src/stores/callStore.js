import { reactive, shallowRef, watch } from 'vue'
import { authState } from './auth'
import * as chatApi from '../services/chatApi'
import { playIncomingRingtone, playOutgoingDialTone, playCallEndTone, stopAllTones } from '../services/callAudio'

// Free Worldwide Google & Cloudflare STUN Servers
const RTC_CONFIG = {
  iceServers: [
    { urls: 'stun:stun.l.google.com:19302' },
    { urls: 'stun:stun1.l.google.com:19302' },
    { urls: 'stun:stun2.l.google.com:19302' },
    { urls: 'stun:stun3.l.google.com:19302' },
    { urls: 'stun:stun.cloudflare.com:3478' }
  ]
}

export const callState = reactive({
  state: 'idle', // 'idle' | 'calling' | 'incoming' | 'connected'
  callType: 'audio', // 'audio' | 'video'
  targetEmployeeId: null,
  targetFullName: '',
  fromEmployeeId: null,
  fromFullName: '',
  conversationId: null,
  offerSdp: null,

  isMuted: false,
  isVideoOff: false,
  isSpeakerMuted: false,
  isPip: false,
  callDuration: 0,
  errorMessage: '',
})

export const localMediaStream = shallowRef(null)
export const remoteMediaStream = shallowRef(null)

let peerConnection = null
let pendingIceCandidates = []
let durationTimer = null
let isListenerInitialized = false

export function initGlobalCallListener() {
  if (!isListenerInitialized) {
    isListenerInitialized = true

    chatApi.onIncomingCall((data) => {
      handleIncomingCall(data)
    })

    chatApi.onCallResponse((data) => {
      handleCallResponse(data)
    })

    chatApi.onCallEnded((data) => {
      handleCallEnded(data)
    })

    watch(() => authState.isAuthenticated, (isAuth) => {
      if (isAuth && authState.token) {
        void chatApi.connectChatHub(authState.token)
      }
    }, { immediate: true })
  }

  if (authState.isAuthenticated && authState.token) {
    void chatApi.connectChatHub(authState.token)
  }
}

async function initWebRTC(callType) {
  try {
    const constraints = {
      audio: {
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true,
      },
      video: callType === 'video'
        ? { width: { ideal: 1280 }, height: { ideal: 720 }, facingMode: 'user' }
        : false,
    }

    const stream = await navigator.mediaDevices.getUserMedia(constraints)
    localMediaStream.value = stream
    callState.isMuted = false
    callState.isVideoOff = false

    peerConnection = new RTCPeerConnection(RTC_CONFIG)

    stream.getTracks().forEach((track) => {
      peerConnection.addTrack(track, stream)
    })

    peerConnection.ontrack = (event) => {
      if (event.streams && event.streams[0]) {
        remoteMediaStream.value = event.streams[0]
      }
    }

    peerConnection.onconnectionstatechange = () => {
      if (['disconnected', 'failed', 'closed'].includes(peerConnection?.connectionState)) {
        cleanupCall()
      }
    }

    return stream
  } catch (err) {
    console.error('Failed to get user media for call', err)
    callState.errorMessage = 'Không thể truy cập Microphone/Camera: ' + (err.message || 'Bị từ chối')
    cleanupCall()
    throw err
  }
}

export async function startCall({ targetEmployeeId, targetFullName, type = 'audio', conversationId = null }) {
  if (!targetEmployeeId) return
  cleanupCall()

  callState.state = 'calling'
  callState.callType = type
  callState.targetEmployeeId = targetEmployeeId
  callState.targetFullName = targetFullName || 'Đồng nghiệp'
  callState.conversationId = conversationId
  callState.isPip = false

  playOutgoingDialTone()

  try {
    await initWebRTC(type)

    peerConnection.onicecandidate = (event) => {
      if (event.candidate) {
        chatApi.callUser(targetEmployeeId, 'ice', JSON.stringify({
          candidate: event.candidate.candidate,
          sdpMid: event.candidate.sdpMid,
          sdpMLineIndex: event.candidate.sdpMLineIndex,
        }), conversationId)
      }
    }

    const offer = await peerConnection.createOffer({
      offerToReceiveAudio: true,
      offerToReceiveVideo: type === 'video',
    })
    await peerConnection.setLocalDescription(offer)

    await chatApi.callUser(targetEmployeeId, 'offer', offer.sdp, conversationId)

    if (conversationId) {
      void chatApi.sendMessage(conversationId,
        `Bắt đầu cuộc gọi ${type === 'video' ? 'video' : 'thoại'}`,
        'CallOffer'
      )
    }
  } catch (e) {
    console.error('startCall failed', e)
    stopAllTones()
    cleanupCall()
  }
}

async function handleIncomingCall(data) {
  if (data.signalingType === 'offer' || data.signalingType === 'CallOffer') {
    const isVideo = (data.signalingData || '').includes('m=video') || data.signalingType === 'video'
    callState.state = 'incoming'
    callState.callType = isVideo ? 'video' : 'audio'
    callState.fromEmployeeId = data.fromEmployeeId
    callState.fromFullName = data.fromFullName || 'Đồng nghiệp'
    callState.conversationId = data.conversationId
    callState.offerSdp = data.signalingData
    callState.isPip = false

    playIncomingRingtone()
  } else if (data.signalingType === 'ice') {
    try {
      const cand = JSON.parse(data.signalingData)
      if (cand?.candidate && peerConnection && peerConnection.remoteDescription) {
        await peerConnection.addIceCandidate(new RTCIceCandidate(cand))
      } else if (cand?.candidate) {
        pendingIceCandidates.push(cand)
      }
    } catch (e) {}
  }
}

export async function acceptCall() {
  stopAllTones()
  const fromId = callState.fromEmployeeId
  const offerSdp = callState.offerSdp
  const callType = callState.callType

  callState.state = 'connected'
  startDurationTimer()

  try {
    await initWebRTC(callType)

    peerConnection.onicecandidate = (event) => {
      if (event.candidate) {
        chatApi.callResponse(fromId, 'ice', JSON.stringify({
          candidate: event.candidate.candidate,
          sdpMid: event.candidate.sdpMid,
          sdpMLineIndex: event.candidate.sdpMLineIndex,
        }))
      }
    }

    if (offerSdp) {
      await peerConnection.setRemoteDescription(new RTCSessionDescription({
        type: 'offer',
        sdp: offerSdp,
      }))

      for (const cand of pendingIceCandidates) {
        try {
          await peerConnection.addIceCandidate(new RTCIceCandidate(cand))
        } catch (e) {}
      }
      pendingIceCandidates = []

      const answer = await peerConnection.createAnswer()
      await peerConnection.setLocalDescription(answer)

      await chatApi.callResponse(fromId, 'accepted', null)
      await chatApi.callResponse(fromId, 'answer', answer.sdp)
    }
  } catch (e) {
    console.error('acceptCall error', e)
    cleanupCall()
  }
}

export async function rejectCall() {
  stopAllTones()
  const fromId = callState.fromEmployeeId
  if (fromId) {
    await chatApi.callResponse(fromId, 'reject', null)
  }
  cleanupCall()
}

async function handleCallResponse(data) {
  if (data.signalingType === 'accepted') {
    stopAllTones()
    callState.state = 'connected'
    startDurationTimer()
  } else if (data.signalingType === 'answer' || data.signalingType === 'CallAnswer') {
    stopAllTones()
    if (peerConnection && data.signalingData) {
      try {
        await peerConnection.setRemoteDescription(new RTCSessionDescription({
          type: 'answer',
          sdp: data.signalingData,
        }))
        for (const cand of pendingIceCandidates) {
          try {
            await peerConnection.addIceCandidate(new RTCIceCandidate(cand))
          } catch (e) {}
        }
        pendingIceCandidates = []
      } catch (e) {
        console.error('Set remote answer failed', e)
      }
    }
  } else if (data.signalingType === 'ice') {
    try {
      const cand = JSON.parse(data.signalingData)
      if (cand?.candidate && peerConnection && peerConnection.remoteDescription) {
        await peerConnection.addIceCandidate(new RTCIceCandidate(cand))
      } else if (cand?.candidate) {
        pendingIceCandidates.push(cand)
      }
    } catch (e) {}
  } else if (data.signalingType === 'reject') {
    playCallEndTone()
    cleanupCall()
  }
}

function handleCallEnded(data) {
  playCallEndTone()
  cleanupCall()
}

export async function endCall() {
  const targetId = callState.targetEmployeeId || callState.fromEmployeeId
  if (targetId) {
    await chatApi.endCall(targetId, callState.conversationId)
  }
  playCallEndTone()
  cleanupCall()
}

export function cleanupCall() {
  stopAllTones()
  stopDurationTimer()

  callState.state = 'idle'
  callState.targetEmployeeId = null
  callState.targetFullName = ''
  callState.fromEmployeeId = null
  callState.fromFullName = ''
  callState.offerSdp = null
  callState.conversationId = null
  callState.callDuration = 0
  callState.isPip = false

  if (localMediaStream.value) {
    localMediaStream.value.getTracks().forEach((t) => t.stop())
    localMediaStream.value = null
  }
  if (peerConnection) {
    peerConnection.close()
    peerConnection = null
  }
  remoteMediaStream.value = null
  pendingIceCandidates = []
}

export function toggleMic() {
  if (localMediaStream.value) {
    const audioTrack = localMediaStream.value.getAudioTracks()[0]
    if (audioTrack) {
      audioTrack.enabled = !audioTrack.enabled
      callState.isMuted = !audioTrack.enabled
    }
  }
}

export function toggleCamera() {
  if (localMediaStream.value) {
    const videoTrack = localMediaStream.value.getVideoTracks()[0]
    if (videoTrack) {
      videoTrack.enabled = !videoTrack.enabled
      callState.isVideoOff = !videoTrack.enabled
    }
  }
}

export function togglePip() {
  callState.isPip = !callState.isPip
}

export function toggleSpeaker() {
  callState.isSpeakerMuted = !callState.isSpeakerMuted
}

function startDurationTimer() {
  callState.callDuration = 0
  stopDurationTimer()
  durationTimer = setInterval(() => {
    callState.callDuration++
  }, 1000)
}

function stopDurationTimer() {
  if (durationTimer) {
    clearInterval(durationTimer)
    durationTimer = null
  }
}

export function formatCallDuration(seconds) {
  const m = Math.floor(seconds / 60).toString().padStart(2, '0')
  const s = (seconds % 60).toString().padStart(2, '0')
  return `${m}:${s}`
}
