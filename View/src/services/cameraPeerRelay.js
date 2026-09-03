import * as signalR from '@microsoft/signalr'
import { API_ORIGIN } from '../config/api'

const AUTH_TOKEN_KEY = 'v_shield_token'
const RTC_CONFIG = { iceServers: [
  { urls: 'stun:stun.l.google.com:19302' },
  { urls: 'stun:stun1.l.google.com:19302' },
  { urls: 'stun:stun.cloudflare.com:3478' },
] }

let connection = null
let connectionPromise = null
const peers = new Map()

function token() { return sessionStorage.getItem(AUTH_TOKEN_KEY) || localStorage.getItem(AUTH_TOKEN_KEY) || '' }

// go2rtc sends its ICE payload as the raw SDP candidate line. Browsers accept
// an RTCIceCandidateInit, so retain JSON support too for relay/browser peers.
export function normalizeRemoteIceCandidate(value) {
  if (!value) return null
  if (typeof value === 'object') return value
  try {
    const parsed = JSON.parse(value)
    if (typeof parsed === 'string') return { candidate: parsed, sdpMLineIndex: 0 }
    if (parsed && typeof parsed === 'object') return parsed
  } catch { /* Native go2rtc candidate: "candidate:..." */ }
  return { candidate: String(value), sdpMLineIndex: 0 }
}

async function getConnection() {
  if (connection?.state === signalR.HubConnectionState.Connected) return connection
  if (connectionPromise) return connectionPromise
  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_ORIGIN}/hubs/camera-relay`, { accessTokenFactory: token })
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .configureLogging(signalR.LogLevel.Warning)
    .build()
  connection.on('CameraRelaySignal', async (sessionId, kind, value) => {
    const peer = peers.get(sessionId)
    if (!peer) return
    try {
      if (kind === 'answer') await peer.pc.setRemoteDescription({ type: 'answer', sdp: value })
      else if (kind === 'candidate' && value) {
        const candidate = normalizeRemoteIceCandidate(value)
        if (candidate) await peer.pc.addIceCandidate(candidate)
      }
      else if (kind === 'error') peer.onState?.('failed', value)
    } catch (error) { peer.onState?.('failed', error?.message) }
  })
  connectionPromise = connection.start().finally(() => { connectionPromise = null })
  return connectionPromise
}

export async function openCameraPeer({ nodeId, streamName, onStream, onState }) {
  const hub = await getConnection()
  const response = await hub.invoke('OpenStream', nodeId, streamName)
  const sessionId = response?.sessionId
  if (!sessionId) throw new Error('Máy chủ không tạo được phiên camera.')
  const pc = new RTCPeerConnection(RTC_CONFIG)
  const peer = { sessionId, pc, onState }
  peers.set(sessionId, peer)
  pc.ontrack = (event) => { if (event.streams?.[0]) onStream?.(event.streams[0]) }
  pc.onconnectionstatechange = () => {
    if (pc.connectionState === 'failed') onState?.('failed', 'Kết nối WebRTC tới camera local bị ngắt.')
  }
  pc.onicecandidate = (event) => {
    if (event.candidate) hub.invoke('Signal', sessionId, 'candidate', JSON.stringify(event.candidate.toJSON())).catch(() => {})
  }
  try {
    const offer = await pc.createOffer({ offerToReceiveVideo: true, offerToReceiveAudio: false })
    await pc.setLocalDescription(offer)
    await hub.invoke('Signal', sessionId, 'offer', offer.sdp)
  } catch (error) {
    closeCameraPeer(peer)
    throw error
  }
  return peer
}

export function closeCameraPeer(peer) {
  if (!peer) return
  peers.delete(peer.sessionId)
  try { peer.pc.close() } catch { /* closed */ }
  if (connection?.state === signalR.HubConnectionState.Connected)
    connection.invoke('CloseStream', peer.sessionId).catch(() => {})
}
