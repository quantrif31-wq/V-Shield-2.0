package com.vshield.mobile.webrtc

import android.content.Context
import android.os.Handler
import android.os.HandlerThread
import android.util.Log
import org.webrtc.*
import org.webrtc.audio.AudioDeviceModule
import org.webrtc.audio.JavaAudioDeviceModule

class WebRTCManager(private val context: Context) {

    companion object {
        private var _eglBase: EglBase? = null
        val eglBase: EglBase
            @Synchronized
            get() {
                if (_eglBase == null) {
                    _eglBase = EglBase.create()
                }
                return _eglBase!!
            }
        val eglBaseContext: EglBase.Context get() = eglBase.eglBaseContext
    }

    interface Listener {
        fun onOfferCreated(sdp: String)
        fun onAnswerCreated(sdp: String)
        fun onIceCandidate(candidate: IceCandidate)
        fun onLocalVideo(videoTrack: VideoTrack)
        fun onRemoteVideo(videoTrack: VideoTrack)
        fun onConnectionStateChanged(state: String)
        fun onError(message: String)
    }

    var listener: Listener? = null

    private var peerConnectionFactory: PeerConnectionFactory? = null
    private var peerConnection: PeerConnection? = null
    private var localVideoSource: VideoSource? = null
    private var localVideoTrack: VideoTrack? = null
    private var localAudioTrack: AudioTrack? = null
    private var videoCapturer: CameraVideoCapturer? = null
    private var audioDeviceModule: AudioDeviceModule? = null
    private var isInitialized = false

    private val signalingThreadHandler: Handler by lazy {
        val thread = HandlerThread("WebRTC-Signaling").apply { start() }
        Handler(thread.looper)
    }

    private val iceServers = listOf(
        PeerConnection.IceServer.builder("stun:stun.l.google.com:19302").createIceServer(),
        PeerConnection.IceServer.builder("stun:stun1.l.google.com:19302").createIceServer(),
        PeerConnection.IceServer.builder("stun:stun.cloudflare.com:3478").createIceServer()
    )

    fun initialize() {
        if (isInitialized && peerConnectionFactory != null) return
        try {
            val initOptions = PeerConnectionFactory.InitializationOptions.builder(context.applicationContext)
                .setEnableInternalTracer(false)
                .createInitializationOptions()
            PeerConnectionFactory.initialize(initOptions)

            val encoderFactory = DefaultVideoEncoderFactory(eglBaseContext, true, true)
            val decoderFactory = DefaultVideoDecoderFactory(eglBaseContext)

            val builder = PeerConnectionFactory.builder()
                .setVideoEncoderFactory(encoderFactory)
                .setVideoDecoderFactory(decoderFactory)

            createAudioDeviceModule()?.let {
                audioDeviceModule = it
                builder.setAudioDeviceModule(it)
            }

            peerConnectionFactory = builder.createPeerConnectionFactory()
            isInitialized = true
        } catch (e: Throwable) {
            Log.e("WebRTCManager", "WebRTC initialize failed: ${e.message}", e)
            listener?.onError("Không thể khởi tạo WebRTC: ${e.message}")
        }
    }

    private fun createAudioDeviceModule(): AudioDeviceModule? {
        return try {
            JavaAudioDeviceModule.builder(context.applicationContext)
                .setUseHardwareAcousticEchoCanceler(false)
                .setUseHardwareNoiseSuppressor(false)
                .createAudioDeviceModule()
        } catch (e: Throwable) {
            Log.w("WebRTCManager", "createAudioDeviceModule warning: ${e.message}")
            null
        }
    }

    fun hasPeerConnection(): Boolean = peerConnection != null

    fun createPeerConnection(): Boolean {
        if (peerConnection != null) return true
        val factory = peerConnectionFactory ?: return false

        try {
            val config = PeerConnection.RTCConfiguration(iceServers).apply {
                sdpSemantics = PeerConnection.SdpSemantics.UNIFIED_PLAN
                continualGatheringPolicy = PeerConnection.ContinualGatheringPolicy.GATHER_CONTINUALLY
                tcpCandidatePolicy = PeerConnection.TcpCandidatePolicy.ENABLED
            }

            val pc = factory.createPeerConnection(config, object : PeerConnection.Observer {
                override fun onIceCandidate(candidate: IceCandidate) {
                    try {
                        listener?.onIceCandidate(candidate)
                    } catch (e: Throwable) {
                        Log.w("WebRTCManager", "onIceCandidate callback warning: ${e.message}")
                    }
                }

                override fun onIceCandidatesRemoved(candidates: Array<out IceCandidate>) {}
                override fun onSignalingChange(p0: PeerConnection.SignalingState?) {}
                override fun onIceConnectionChange(state: PeerConnection.IceConnectionState?) {
                    try {
                        listener?.onConnectionStateChanged(state?.toString() ?: "unknown")
                    } catch (e: Throwable) {
                        Log.w("WebRTCManager", "onIceConnectionChange warning: ${e.message}")
                    }
                }
                override fun onIceConnectionReceivingChange(p0: Boolean) {}
                override fun onIceGatheringChange(p0: PeerConnection.IceGatheringState?) {}
                override fun onAddStream(p0: MediaStream?) {
                    try {
                        p0?.videoTracks?.firstOrNull()?.let { listener?.onRemoteVideo(it) }
                    } catch (e: Throwable) {
                        Log.w("WebRTCManager", "onAddStream warning: ${e.message}")
                    }
                }
                override fun onRemoveStream(p0: MediaStream?) {}
                override fun onDataChannel(p0: DataChannel?) {}
                override fun onRenegotiationNeeded() {}
                override fun onAddTrack(receiver: RtpReceiver?, streams: Array<out MediaStream>?) {
                    try {
                        val track = receiver?.track()
                        if (track is VideoTrack) {
                            listener?.onRemoteVideo(track)
                        }
                    } catch (e: Throwable) {
                        Log.w("WebRTCManager", "onAddTrack warning: ${e.message}")
                    }
                }
            })

            if (pc == null) return false
            peerConnection = pc
            return true
        } catch (e: Throwable) {
            Log.e("WebRTCManager", "createPeerConnection failed: ${e.message}", e)
            return false
        }
    }

    fun setupLocalMedia(enableVideo: Boolean = false): Boolean {
        val factory = peerConnectionFactory ?: return false
        val pc = peerConnection ?: return false

        try {
            val audioConstraints = MediaConstraints().apply {
                mandatory.add(MediaConstraints.KeyValuePair("googEchoCancellation", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googNoiseSuppression", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googAutoGainControl", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googHighpassFilter", "true"))
            }

            val audioSource = factory.createAudioSource(audioConstraints)
            localAudioTrack = factory.createAudioTrack("audio0", audioSource)
            localAudioTrack?.setEnabled(true)
            pc.addTrack(localAudioTrack, listOf("vshield_stream"))

            if (enableVideo) {
                try {
                    localVideoSource = factory.createVideoSource(false)
                    videoCapturer = createCameraCapturer()
                    if (videoCapturer != null) {
                        val surfaceTextureHelper = SurfaceTextureHelper.create("VideoCaptureThread", eglBaseContext)
                        videoCapturer?.initialize(
                            surfaceTextureHelper,
                            context.applicationContext,
                            localVideoSource?.capturerObserver
                        )
                        videoCapturer?.startCapture(1280, 720, 30)
                        localVideoTrack = factory.createVideoTrack("video0", localVideoSource)
                        localVideoTrack?.setEnabled(true)
                        pc.addTrack(localVideoTrack, listOf("vshield_stream"))
                        localVideoTrack?.let { listener?.onLocalVideo(it) }
                    }
                } catch (e: Throwable) {
                    Log.w("WebRTCManager", "Camera setup failed, proceeding audio-only: ${e.message}")
                }
            }

            return true
        } catch (e: Throwable) {
            Log.e("WebRTCManager", "setupLocalMedia failed: ${e.message}", e)
            listener?.onError("Không thể bật thiết bị âm thanh: ${e.message}")
            return false
        }
    }

    private fun createCameraCapturer(): CameraVideoCapturer? {
        return try {
            val enumerator = Camera2Enumerator(context)
            val deviceNames = enumerator.deviceNames
            val front = deviceNames.firstOrNull { enumerator.isFrontFacing(it) }
            val back = deviceNames.firstOrNull { enumerator.isBackFacing(it) }
            (front ?: back)?.let { enumerator.createCapturer(it, null) }
        } catch (e: Throwable) {
            Log.w("WebRTCManager", "camera enumerator failed: ${e.message}")
            null
        }
    }

    fun createOffer() {
        val pc = peerConnection ?: run {
            listener?.onError("Peer chưa sẵn sàng")
            return
        }
        val constraints = MediaConstraints().apply {
            mandatory.add(MediaConstraints.KeyValuePair("OfferToReceiveAudio", "true"))
            mandatory.add(MediaConstraints.KeyValuePair("OfferToReceiveVideo", "true"))
        }
        pc.createOffer(object : SdpObserver {
            override fun onCreateSuccess(desc: SessionDescription) {
                pc.setLocalDescription(object : SdpObserver {
                    override fun onCreateSuccess(p0: SessionDescription?) {}
                    override fun onSetSuccess() {}
                    override fun onCreateFailure(p0: String?) {}
                    override fun onSetFailure(p0: String?) {
                        listener?.onError("Không thể thiết lập offer")
                    }
                }, desc)
                listener?.onOfferCreated(desc.description)
            }

            override fun onSetSuccess() {}
            override fun onCreateFailure(p0: String?) {
                listener?.onError("Không thể tạo offer: $p0")
            }
            override fun onSetFailure(p0: String?) {}
        }, constraints)
    }

    fun handleRemoteOffer(offerSdp: String) {
        val pc = peerConnection ?: return
        val desc = SessionDescription(SessionDescription.Type.OFFER, offerSdp)
        pc.setRemoteDescription(object : SdpObserver {
            override fun onCreateSuccess(p0: SessionDescription?) {}
            override fun onSetSuccess() {
                createAnswer()
            }
            override fun onCreateFailure(p0: String?) {}
            override fun onSetFailure(p0: String?) {
                listener?.onError("Không thể nhận offer: $p0")
            }
        }, desc)
    }

    private fun createAnswer() {
        val pc = peerConnection ?: return
        val constraints = MediaConstraints().apply {
            mandatory.add(MediaConstraints.KeyValuePair("OfferToReceiveAudio", "true"))
            mandatory.add(MediaConstraints.KeyValuePair("OfferToReceiveVideo", "true"))
        }
        pc.createAnswer(object : SdpObserver {
            override fun onCreateSuccess(desc: SessionDescription) {
                pc.setLocalDescription(object : SdpObserver {
                    override fun onCreateSuccess(p0: SessionDescription?) {}
                    override fun onSetSuccess() {}
                    override fun onCreateFailure(p0: String?) {}
                    override fun onSetFailure(p0: String?) {
                        listener?.onError("Không thể thiết lập answer")
                    }
                }, desc)
                listener?.onAnswerCreated(desc.description)
            }

            override fun onSetSuccess() {}
            override fun onCreateFailure(p0: String?) {
                listener?.onError("Không thể tạo answer: $p0")
            }
            override fun onSetFailure(p0: String?) {}
        }, constraints)
    }

    fun handleRemoteAnswer(answerSdp: String) {
        val pc = peerConnection ?: return
        val desc = SessionDescription(SessionDescription.Type.ANSWER, answerSdp)
        pc.setRemoteDescription(object : SdpObserver {
            override fun onCreateSuccess(p0: SessionDescription?) {}
            override fun onSetSuccess() {}
            override fun onCreateFailure(p0: String?) {}
            override fun onSetFailure(p0: String?) {
                listener?.onError("Không thể nhận answer: $p0")
            }
        }, desc)
    }

    fun addIceCandidate(candidate: IceCandidate) {
        val pc = peerConnection ?: return
        pc.addIceCandidate(candidate)
    }

    fun setAudioEnabled(enabled: Boolean) {
        localAudioTrack?.setEnabled(enabled)
    }

    fun setVideoEnabled(enabled: Boolean) {
        localVideoTrack?.setEnabled(enabled)
    }

    fun switchCamera() {
        (videoCapturer as? CameraVideoCapturer)?.switchCamera(null)
    }

    fun close() {
        signalingThreadHandler.post {
            try {
                videoCapturer?.stopCapture()
            } catch (_: Throwable) {}
            try {
                videoCapturer?.dispose()
            } catch (_: Throwable) {}
            videoCapturer = null

            try {
                localVideoTrack?.dispose()
            } catch (_: Throwable) {}
            localVideoTrack = null

            try {
                localAudioTrack?.dispose()
            } catch (_: Throwable) {}
            localAudioTrack = null

            try {
                localVideoSource?.dispose()
            } catch (_: Throwable) {}
            localVideoSource = null

            try {
                peerConnection?.close()
            } catch (_: Throwable) {}
            peerConnection = null

            try {
                audioDeviceModule?.release()
            } catch (_: Throwable) {}
            audioDeviceModule = null
        }
    }
}
