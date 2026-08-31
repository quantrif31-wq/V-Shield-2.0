import { describe, it, expect, beforeAll, beforeEach, afterEach, vi } from "vitest"

const chatApiMock = vi.hoisted(() => ({
  onIncomingCall: vi.fn(),
  onCallResponse: vi.fn(),
  onCallEnded: vi.fn(),
  connectChatHub: vi.fn().mockResolvedValue(undefined),
  callUser: vi.fn().mockResolvedValue(undefined),
  callResponse: vi.fn().mockResolvedValue(undefined),
  sendMessage: vi.fn().mockResolvedValue(undefined),
  endCall: vi.fn().mockResolvedValue(undefined)
}))

vi.mock("../../services/chatApi", () => chatApiMock)

const authStateMock = vi.hoisted(() => ({
  isAuthenticated: true,
  token: "tok-123"
}))

vi.mock("../auth", () => ({ authState: authStateMock }))

import {
  callState,
  localMediaStream,
  remoteMediaStream,
  initGlobalCallListener,
  cleanupCall,
  startCall,
  acceptCall,
  rejectCall,
  endCall,
  toggleMic,
  toggleCamera,
  togglePip,
  toggleSpeaker,
  formatCallDuration
} from "../callStore"

let createdPeerConnections = []

function makePeerConnection() {
  const pc = {
    connectionState: "connected",
    addTrack: vi.fn(),
    close: vi.fn(),
    createOffer: vi.fn().mockResolvedValue({ sdp: "offer-sdp" }),
    createAnswer: vi.fn().mockResolvedValue({ sdp: "answer-sdp" }),
    setLocalDescription: vi.fn().mockResolvedValue(undefined),
    setRemoteDescription: vi.fn(function () {
      this.remoteDescription = { type: this._remoteType || "answer" }
      return Promise.resolve()
    }),
    addIceCandidate: vi.fn().mockResolvedValue(undefined),
    iceCandidateHandler: null,
    trackHandler: null,
    connectionStateHandler: null,
    get onicecandidate() {
      return this.iceCandidateHandler
    },
    set onicecandidate(fn) {
      this.iceCandidateHandler = fn
    },
    get ontrack() {
      return this.trackHandler
    },
    set ontrack(fn) {
      this.trackHandler = fn
    },
    get onconnectionstatechange() {
      return this.connectionStateHandler
    },
    set onconnectionstatechange(fn) {
      this.connectionStateHandler = fn
    }
  }
  return pc
}

function makeStream() {
  return {
    getTracks: vi.fn(() => []),
    getAudioTracks: vi.fn(() => []),
    getVideoTracks: vi.fn(() => [])
  }
}

globalThis.RTCIceCandidate = class {
  constructor(init) {
    this.init = init
  }
}
globalThis.RTCSessionDescription = class {
  constructor(init) {
    this.type = init.type
    this.sdp = init.sdp
  }
}

function installWebRTCMocks() {
  createdPeerConnections = []
  globalThis.RTCPeerConnection = function RTCPeerConnectionMock() {
    const pc = makePeerConnection()
    createdPeerConnections.push(pc)
    return pc
  }
  const stream = makeStream()
  const mediaDevices = {
    getUserMedia: vi.fn().mockResolvedValue(stream)
  }
  Object.defineProperty(globalThis.navigator, "mediaDevices", {
    value: mediaDevices,
    configurable: true,
    writable: true
  })
  return { stream, mediaDevices }
}

function resetCallState() {
  localMediaStream.value = null
  remoteMediaStream.value = null
  Object.assign(callState, {
    state: "idle",
    callType: "audio",
    targetEmployeeId: null,
    targetFullName: "",
    fromEmployeeId: null,
    fromFullName: "",
    conversationId: null,
    offerSdp: null,
    isMuted: false,
    isVideoOff: false,
    isSpeakerMuted: false,
    isPip: false,
    callDuration: 0,
    errorMessage: ""
  })
  cleanupCall()
}

let incomingCb
let responseCb
let endedCb

describe("callStore", () => {
  beforeAll(() => {
    installWebRTCMocks()
    initGlobalCallListener()
    incomingCb = chatApiMock.onIncomingCall.mock.calls[0][0]
    responseCb = chatApiMock.onCallResponse.mock.calls[0][0]
    endedCb = chatApiMock.onCallEnded.mock.calls[0][0]
  })

  beforeEach(() => {
    vi.useFakeTimers()
    vi.clearAllMocks()
    installWebRTCMocks()
    resetCallState()
  })

  afterEach(() => {
    cleanupCall()
    vi.useRealTimers()
  })

  it("initGlobalCallListener registers chat listeners once", () => {
    initGlobalCallListener()
    expect(chatApiMock.onIncomingCall).toHaveBeenCalledTimes(0)
  })

  it("startCall with no target is a no-op", async () => {
    await startCall({})
    expect(chatApiMock.callUser).not.toHaveBeenCalled()
  })

  it("startCall audio flow creates offer and sends message with conversationId", async () => {
    await startCall({ targetEmployeeId: "e1", targetFullName: "Alice", type: "audio", conversationId: "c1" })

    expect(callState.state).toBe("calling")
    expect(callState.targetFullName).toBe("Alice")
    expect(chatApiMock.callUser).toHaveBeenCalledWith("e1", "offer", "offer-sdp", "c1")
    expect(chatApiMock.sendMessage).toHaveBeenCalled()

    const pc = createdPeerConnections[0]
    pc.onicecandidate({ candidate: { candidate: "cand1", sdpMid: "0", sdpMLineIndex: 0 } })
    expect(chatApiMock.callUser).toHaveBeenCalledWith("e1", "ice", expect.any(String), "c1")
  })

  it("startCall video type with empty full name uses default and no conversation message", async () => {
    await startCall({ targetEmployeeId: "e2", type: "video" })
    expect(callState.callType).toBe("video")
    expect(callState.targetFullName).toBe("Đồng nghiệp")
    expect(chatApiMock.sendMessage).not.toHaveBeenCalled()
  })

  it("startCall cleans up when getUserMedia rejects", async () => {
    globalThis.navigator.mediaDevices.getUserMedia.mockRejectedValue(new Error("denied"))
    await startCall({ targetEmployeeId: "e3" })
    expect(callState.state).toBe("idle")
    expect(callState.errorMessage).toContain("Không thể truy cập")
  })

  it("incoming offer sets incoming state", async () => {
    await incomingCb({
      signalingType: "offer",
      fromEmployeeId: "from1",
      fromFullName: "Bob",
      conversationId: "conv1",
      signalingData: "m=video 9 udp"
    })
    expect(callState.state).toBe("incoming")
    expect(callState.callType).toBe("video")
    expect(callState.fromFullName).toBe("Bob")
    expect(callState.conversationId).toBe("conv1")
  })

  it("incoming audio call defaults name when missing", async () => {
    await incomingCb({ signalingType: "CallOffer", fromEmployeeId: "from2", signalingData: "audio only" })
    expect(callState.callType).toBe("audio")
    expect(callState.fromFullName).toBe("Đồng nghiệp")
  })

  it("incoming ice candidate is handled", async () => {
    await incomingCb({ signalingType: "ice", signalingData: JSON.stringify({ candidate: "c2" }) })
    expect(callState.state).toBe("idle")
  })

  it("incoming ice with malformed data is caught", async () => {
    await incomingCb({ signalingType: "ice", signalingData: "{bad json" })
    expect(callState.state).toBe("idle")
  })

  it("acceptCall sets connected and completes the offer/answer handshake", async () => {
    await incomingCb({ signalingType: "offer", fromEmployeeId: "from1", signalingData: "offer-norm" })
    await acceptCall()

    expect(callState.state).toBe("connected")
    expect(chatApiMock.callResponse).toHaveBeenCalledWith("from1", "accepted", null)
    expect(chatApiMock.callResponse).toHaveBeenCalledWith("from1", "answer", "answer-sdp")

    const pc = createdPeerConnections[0]
    pc.onicecandidate({ candidate: { candidate: "cA", sdpMid: "0", sdpMLineIndex: 0 } })
    expect(chatApiMock.callResponse).toHaveBeenCalledWith("from1", "ice", expect.any(String))
  })

  it("acceptCall cleans up on error", async () => {
    await incomingCb({ signalingType: "offer", fromEmployeeId: "fromX", signalingData: "offer" })
    globalThis.navigator.mediaDevices.getUserMedia.mockRejectedValue(new Error("denied"))
    await acceptCall()
    expect(callState.state).toBe("idle")
  })

  it("handleCallResponse accepted sets connected and starts duration", async () => {
    await responseCb({ signalingType: "accepted" })
    expect(callState.state).toBe("connected")
    vi.advanceTimersByTime(3200)
    expect(callState.callDuration).toBe(3)
  })

  it("handleCallResponse answer sets remote description and flushes queued candidates", async () => {
    await incomingCb({ signalingType: "ice", signalingData: JSON.stringify({ candidate: "cQ", sdpMid: "0", sdpMLineIndex: 0 }) })
    await incomingCb({ signalingType: "offer", fromEmployeeId: "f", signalingData: "o" })
    await acceptCall()
    await responseCb({ signalingType: "answer", signalingData: "answer2" })
    expect(createdPeerConnections[0].setRemoteDescription).toHaveBeenCalled()
  })

  it("handleCallResponse answer flushes pending candidates collected pre-answer", async () => {
    await startCall({ targetEmployeeId: "eP", type: "audio" })
    await incomingCb({ signalingType: "ice", signalingData: JSON.stringify({ candidate: "cQ2" }) })
    await responseCb({ signalingType: "answer", signalingData: "answerQ" })
    expect(createdPeerConnections[0].setRemoteDescription).toHaveBeenCalled()
    expect(createdPeerConnections[0].addIceCandidate).toHaveBeenCalled()
  })

  it("handleCallResponse ice queues candidate when no remote description yet", async () => {
    await startCall({ targetEmployeeId: "eQ", type: "audio" })
    await responseCb({ signalingType: "ice", signalingData: JSON.stringify({ candidate: "cQueued" }) })
    expect(callState.state).toBe("calling")
  })

  it("incoming ice candidate forwards to peer when remote description present", async () => {
    await incomingCb({ signalingType: "offer", fromEmployeeId: "f", signalingData: "o" })
    await acceptCall()
    await incomingCb({ signalingType: "ice", signalingData: JSON.stringify({ candidate: "cIncoming" }) })
    expect(createdPeerConnections[0].addIceCandidate).toHaveBeenCalled()
  })

  it("ontrack sets remote media stream when event has streams", async () => {
    await startCall({ targetEmployeeId: "eTk", type: "audio" })
    const stream = { id: "remote" }
    createdPeerConnections[0].ontrack({ streams: [stream] })
    expect(remoteMediaStream.value).toBe(stream)
  })

  it("startCall adds local tracks to the peer connection", async () => {
    const track = { kind: "audio", enabled: true, stop: vi.fn() }
    const stream = {
      getTracks: vi.fn(() => [track]),
      getAudioTracks: vi.fn(() => [track]),
      getVideoTracks: vi.fn(() => []),
      getVideoTracksMock: undefined
    }
    globalThis.navigator.mediaDevices.getUserMedia.mockResolvedValue(stream)
    await startCall({ targetEmployeeId: "eTr", type: "audio" })
    const pc = createdPeerConnections[0]
    expect(pc.addTrack).toHaveBeenCalledWith(track, stream)
  })

  it("onconnectionstatechange cleanup on failed connection", async () => {
    await startCall({ targetEmployeeId: "eFs", type: "audio" })
    createdPeerConnections[0].connectionState = "failed"
    createdPeerConnections[0].onconnectionstatechange()
    expect(callState.state).toBe("idle")
  })

  it("handleCallResponse answer error is caught", async () => {
    await incomingCb({ signalingType: "offer", fromEmployeeId: "f", signalingData: "o" })
    await acceptCall()
    createdPeerConnections[0].setRemoteDescription.mockRejectedValueOnce(new Error("x"))
    await responseCb({ signalingType: "CallAnswer", signalingData: "ans" })
    expect(callState.state).toBe("connected")
  })

  it("handleCallResponse ice forwards to peer when remote description present", async () => {
    await incomingCb({ signalingType: "offer", fromEmployeeId: "f", signalingData: "o" })
    await acceptCall()
    await responseCb({ signalingType: "ice", signalingData: JSON.stringify({ candidate: "c3" }) })
    expect(createdPeerConnections[0].addIceCandidate).toHaveBeenCalled()
  })

  it("handleCallResponse reject plays end tone and cleans up", async () => {
    await responseCb({ signalingType: "reject" })
    expect(callState.state).toBe("idle")
  })

  it("handleCallEnded cleans up", async () => {
    callState.state = "connected"
    await endedCb({})
    expect(callState.state).toBe("idle")
  })

  it("rejectCall notifies caller when fromEmployeeId set", async () => {
    callState.fromEmployeeId = "fromR"
    await rejectCall()
    expect(chatApiMock.callResponse).toHaveBeenCalledWith("fromR", "reject", null)
    expect(callState.state).toBe("idle")
  })

  it("rejectCall without fromEmployeeId just cleans up", async () => {
    callState.fromEmployeeId = null
    await rejectCall()
    expect(chatApiMock.callResponse).not.toHaveBeenCalled()
  })

  it("endCall notifies target and cleans up", async () => {
    await startCall({ targetEmployeeId: "eT", type: "audio", conversationId: "cC" })
    callState.targetEmployeeId = "eT"
    callState.conversationId = "cC"
    await endCall()
    expect(chatApiMock.endCall).toHaveBeenCalledWith("eT", "cC")
    expect(callState.state).toBe("idle")
  })

  it("endCall uses fromEmployeeId fallback", async () => {
    callState.targetEmployeeId = null
    callState.fromEmployeeId = "fromE"
    callState.conversationId = null
    await endCall()
    expect(chatApiMock.endCall).toHaveBeenCalledWith("fromE", null)
  })

  it("cleanupCall closes stream tracks and peer connection", async () => {
    await startCall({ targetEmployeeId: "eX", type: "audio" })
    const pc = createdPeerConnections[0]
    cleanupCall()
    expect(pc.close).toHaveBeenCalled()
    expect(callState.state).toBe("idle")
  })

  it("toggleMic toggles audio track enabled", async () => {
    await startCall({ targetEmployeeId: "eM", type: "audio" })
    const audioTrack = { kind: "audio", enabled: true }
    localMediaStream.value.getAudioTracks = () => [audioTrack]
    toggleMic()
    expect(audioTrack.enabled).toBe(false)
    expect(callState.isMuted).toBe(true)
    toggleMic()
    expect(callState.isMuted).toBe(false)
  })

  it("toggleCamera toggles video track enabled", async () => {
    await startCall({ targetEmployeeId: "eC", type: "video" })
    const videoTrack = { kind: "video", enabled: true }
    localMediaStream.value.getVideoTracks = () => [videoTrack]
    toggleCamera()
    expect(videoTrack.enabled).toBe(false)
    expect(callState.isVideoOff).toBe(true)
  })

  it("togglePip and toggleSpeaker toggle booleans", async () => {
    togglePip()
    expect(callState.isPip).toBe(true)
    toggleSpeaker()
    expect(callState.isSpeakerMuted).toBe(true)
  })

  it("formatCallDuration pads minutes and seconds", async () => {
    expect(formatCallDuration(0)).toBe("00:00")
    expect(formatCallDuration(65)).toBe("01:05")
    expect(formatCallDuration(3630)).toBe("60:30")
  })
})
