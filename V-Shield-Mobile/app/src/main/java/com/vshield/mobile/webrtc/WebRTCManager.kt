package com.vshield.mobile.webrtc

import android.content.Context
import android.media.AudioManager
import android.os.Handler
import android.os.Looper
import android.util.Log
import org.webrtc.*
import org.webrtc.audio.AudioDeviceModule
import org.webrtc.audio.JavaAudioDeviceModule
import java.util.concurrent.CopyOnWriteArrayList

class WebRTCManager(private val context: Context) {

    companion object {
        private const val TAG = "WebRTCManager"

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

        private var isFactoryInitialized = false

        @Synchronized
        fun ensureFactoryInitialized(appContext: Context) {
            if (!isFactoryInitialized) {
                try {
                    val initOptions = PeerConnectionFactory.InitializationOptions.builder(appContext.applicationContext)
                        .setEnableInternalTracer(false)
                        .createInitializationOptions()
                    PeerConnectionFactory.initialize(initOptions)
                    isFactoryInitialized = true
                    Log.i(TAG, "PeerConnectionFactory initialized successfully")
                } catch (e: Throwable) {
                    Log.e(TAG, "Failed to initialize PeerConnectionFactory: ${e.message}", e)
                }
            }
        }
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

    private val mainHandler = Handler(Looper.getMainLooper())
    private val audioManager: AudioManager? by lazy {
        context.applicationContext.getSystemService(Context.AUDIO_SERVICE) as? AudioManager
    }

    private var peerConnectionFactory: PeerConnectionFactory? = null
    private var peerConnection: PeerConnection? = null
    private var localVideoSource: VideoSource? = null
    private var localVideoTrack: VideoTrack? = null
    private var localAudioSource: AudioSource? = null
    private var localAudioTrack: AudioTrack? = null
    private var videoCapturer: CameraVideoCapturer? = null
    private var surfaceTextureHelper: SurfaceTextureHelper? = null
    private var audioDeviceModule: AudioDeviceModule? = null

    private val pendingIceCandidates = CopyOnWriteArrayList<IceCandidate>()
    private var isRemoteDescriptionSet = false

    private val iceServers = listOf(
        PeerConnection.IceServer.builder("stun:stun.l.google.com:19302").createIceServer(),
        PeerConnection.IceServer.builder("stun:stun1.l.google.com:19302").createIceServer(),
        PeerConnection.IceServer.builder("stun:stun2.l.google.com:19302").createIceServer(),
        PeerConnection.IceServer.builder("stun:stun.cloudflare.com:3478").createIceServer()
    )

    fun initialize() {
        if (peerConnectionFactory != null) return

        try {
            ensureFactoryInitialized(context.applicationContext)

            val adm = createSafeAudioDeviceModule()
            audioDeviceModule = adm

            val encoderFactory = DefaultVideoEncoderFactory(eglBaseContext, true, true)
            val decoderFactory = DefaultVideoDecoderFactory(eglBaseContext)

            val options = PeerConnectionFactory.Options()

            val builder = PeerConnectionFactory.builder()
                .setOptions(options)
                .setVideoEncoderFactory(encoderFactory)
                .setVideoDecoderFactory(decoderFactory)

            if (adm != null) {
                builder.setAudioDeviceModule(adm)
            }

            peerConnectionFactory = builder.createPeerConnectionFactory()
            Log.i(TAG, "PeerConnectionFactory instance created")
        } catch (e: Throwable) {
            Log.e(TAG, "WebRTC initialize error: ${e.message}", e)
            listener?.onError("Khởi tạo thoại thất bại: ${e.message}")
        }
    }

    private fun createSafeAudioDeviceModule(): AudioDeviceModule? {
        return try {
            JavaAudioDeviceModule.builder(context.applicationContext)
                .setUseHardwareAcousticEchoCanceler(false)
                .setUseHardwareNoiseSuppressor(false)
                .setAudioRecordErrorCallback(object : JavaAudioDeviceModule.AudioRecordErrorCallback {
                    override fun onWebRtcAudioRecordInitError(msg: String?) {
                        Log.w(TAG, "AudioRecord init error: $msg")
                    }
                    override fun onWebRtcAudioRecordStartError(code: JavaAudioDeviceModule.AudioRecordStartErrorCode?, msg: String?) {
                        Log.w(TAG, "AudioRecord start error [$code]: $msg")
                    }
                    override fun onWebRtcAudioRecordError(msg: String?) {
                        Log.w(TAG, "AudioRecord error: $msg")
                    }
                })
                .setAudioTrackErrorCallback(object : JavaAudioDeviceModule.AudioTrackErrorCallback {
                    override fun onWebRtcAudioTrackInitError(msg: String?) {
                        Log.w(TAG, "AudioTrack init error: $msg")
                    }
                    override fun onWebRtcAudioTrackStartError(code: JavaAudioDeviceModule.AudioTrackStartErrorCode?, msg: String?) {
                        Log.w(TAG, "AudioTrack start error [$code]: $msg")
                    }
                    override fun onWebRtcAudioTrackError(msg: String?) {
                        Log.w(TAG, "AudioTrack error: $msg")
                    }
                })
                .createAudioDeviceModule()
        } catch (e: Throwable) {
            Log.w(TAG, "createSafeAudioDeviceModule fallback: ${e.message}")
            null
        }
    }

    fun hasPeerConnection(): Boolean = peerConnection != null

    fun createPeerConnection(): Boolean {
        if (peerConnection != null) return true
        val factory = peerConnectionFactory ?: return false

        try {
            val rtcConfig = PeerConnection.RTCConfiguration(iceServers).apply {
                sdpSemantics = PeerConnection.SdpSemantics.UNIFIED_PLAN
                continualGatheringPolicy = PeerConnection.ContinualGatheringPolicy.GATHER_CONTINUALLY
                tcpCandidatePolicy = PeerConnection.TcpCandidatePolicy.ENABLED
                bundlePolicy = PeerConnection.BundlePolicy.MAXBUNDLE
                rtcpMuxPolicy = PeerConnection.RtcpMuxPolicy.REQUIRE
            }

            val pc = factory.createPeerConnection(rtcConfig, object : PeerConnection.Observer {
                override fun onIceCandidate(candidate: IceCandidate) {
                    mainHandler.post {
                        try {
                            listener?.onIceCandidate(candidate)
                        } catch (e: Throwable) {
                            Log.w(TAG, "onIceCandidate callback: ${e.message}")
                        }
                    }
                }

                override fun onIceCandidatesRemoved(candidates: Array<out IceCandidate>) {}
                override fun onSignalingChange(state: PeerConnection.SignalingState?) {}
                override fun onIceConnectionChange(state: PeerConnection.IceConnectionState?) {
                    val stateStr = state?.toString() ?: "unknown"
                    mainHandler.post {
                        try {
                            listener?.onConnectionStateChanged(stateStr)
                        } catch (e: Throwable) {
                            Log.w(TAG, "onIceConnectionChange: ${e.message}")
                        }
                    }
                }
                override fun onIceConnectionReceivingChange(receiving: Boolean) {}
                override fun onIceGatheringChange(state: PeerConnection.IceGatheringState?) {}
                override fun onAddStream(stream: MediaStream?) {
                    mainHandler.post {
                        try {
                            stream?.videoTracks?.firstOrNull()?.let { listener?.onRemoteVideo(it) }
                        } catch (e: Throwable) {
                            Log.w(TAG, "onAddStream: ${e.message}")
                        }
                    }
                }
                override fun onRemoveStream(stream: MediaStream?) {}
                override fun onDataChannel(channel: DataChannel?) {}
                override fun onRenegotiationNeeded() {}
                override fun onAddTrack(receiver: RtpReceiver?, streams: Array<out MediaStream>?) {
                    mainHandler.post {
                        try {
                            val track = receiver?.track()
                            if (track is VideoTrack) {
                                listener?.onRemoteVideo(track)
                            }
                        } catch (e: Throwable) {
                            Log.w(TAG, "onAddTrack: ${e.message}")
                        }
                    }
                }
            })

            if (pc == null) {
                Log.e(TAG, "factory.createPeerConnection returned null")
                return false
            }

            peerConnection = pc
            isRemoteDescriptionSet = false
            return true
        } catch (e: Throwable) {
            Log.e(TAG, "createPeerConnection exception: ${e.message}", e)
            return false
        }
    }

    fun setupLocalMedia(enableVideo: Boolean = false): Boolean {
        val factory = peerConnectionFactory ?: return false
        val pc = peerConnection ?: return false

        try {
            // Set audio mode for clear phone call
            try {
                audioManager?.mode = AudioManager.MODE_IN_COMMUNICATION
                audioManager?.isSpeakerphoneOn = enableVideo // Speaker on for video, earpiece for audio
            } catch (e: Throwable) {
                Log.w(TAG, "AudioManager mode setup: ${e.message}")
            }

            val audioConstraints = MediaConstraints().apply {
                mandatory.add(MediaConstraints.KeyValuePair("googEchoCancellation", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googNoiseSuppression", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googAutoGainControl", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googHighpassFilter", "true"))
            }

            val aSource = factory.createAudioSource(audioConstraints)
            localAudioSource = aSource
            val aTrack = factory.createAudioTrack("audio0", aSource)
            aTrack.setEnabled(true)
            localAudioTrack = aTrack
            pc.addTrack(aTrack, listOf("vshield_stream"))

            if (enableVideo) {
                try {
                    val vSource = factory.createVideoSource(false)
                    localVideoSource = vSource
                    val capturer = createCameraCapturer()
                    videoCapturer = capturer
                    if (capturer != null) {
                        val helper = SurfaceTextureHelper.create("VideoCaptureThread", eglBaseContext)
                        surfaceTextureHelper = helper
                        capturer.initialize(helper, context.applicationContext, vSource.capturerObserver)
                        capturer.startCapture(1280, 720, 30)

                        val vTrack = factory.createVideoTrack("video0", vSource)
                        vTrack.setEnabled(true)
                        localVideoTrack = vTrack
                        pc.addTrack(vTrack, listOf("vshield_stream"))
                        mainHandler.post {
                            listener?.onLocalVideo(vTrack)
                        }
                    }
                } catch (e: Throwable) {
                    Log.w(TAG, "Camera init fallback to audio only: ${e.message}")
                }
            }

            return true
        } catch (e: Throwable) {
            Log.e(TAG, "setupLocalMedia failed: ${e.message}", e)
            listener?.onError("Không thể bật micro: ${e.message}")
            return false
        }
    }

    private fun createCameraCapturer(): CameraVideoCapturer? {
        return try {
            val enumerator = Camera2Enumerator(context.applicationContext)
            val deviceNames = enumerator.deviceNames
            val front = deviceNames.firstOrNull { enumerator.isFrontFacing(it) }
            val back = deviceNames.firstOrNull { enumerator.isBackFacing(it) }
            (front ?: back)?.let { enumerator.createCapturer(it, null) }
        } catch (e: Throwable) {
            Log.w(TAG, "Camera2Enumerator failed: ${e.message}")
            null
        }
    }

    fun createOffer() {
        val pc = peerConnection ?: run {
            listener?.onError("PeerConnection chưa sẵn sàng")
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
                    override fun onSetSuccess() {
                        Log.i(TAG, "Local offer description set successfully")
                        mainHandler.post {
                            listener?.onOfferCreated(desc.description)
                        }
                    }
                    override fun onCreateFailure(p0: String?) {}
                    override fun onSetFailure(error: String?) {
                        Log.e(TAG, "setLocalDescription failure: $error")
                        mainHandler.post {
                            listener?.onError("Không thể thiết lập cấu hình cuộc gọi: $error")
                        }
                    }
                }, desc)
            }

            override fun onSetSuccess() {}
            override fun onCreateFailure(error: String?) {
                Log.e(TAG, "createOffer failure: $error")
                mainHandler.post {
                    listener?.onError("Không thể tạo kết nối gọi: $error")
                }
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
                Log.i(TAG, "Remote offer description set successfully")
                isRemoteDescriptionSet = true
                drainPendingIceCandidates()
                createAnswer()
            }
            override fun onCreateFailure(p0: String?) {}
            override fun onSetFailure(error: String?) {
                Log.e(TAG, "setRemoteDescription (offer) failed: $error")
                mainHandler.post {
                    listener?.onError("Không thể tiếp nhận offer từ đối phương")
                }
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
                    override fun onSetSuccess() {
                        Log.i(TAG, "Local answer description set successfully")
                        mainHandler.post {
                            listener?.onAnswerCreated(desc.description)
                        }
                    }
                    override fun onCreateFailure(p0: String?) {}
                    override fun onSetFailure(error: String?) {
                        Log.e(TAG, "setLocalDescription (answer) failed: $error")
                        mainHandler.post {
                            listener?.onError("Không thể hoàn tất trả lời cuộc gọi: $error")
                        }
                    }
                }, desc)
            }

            override fun onSetSuccess() {}
            override fun onCreateFailure(error: String?) {
                Log.e(TAG, "createAnswer failure: $error")
                mainHandler.post {
                    listener?.onError("Không thể tạo tín hiệu trả lời: $error")
                }
            }
            override fun onSetFailure(p0: String?) {}
        }, constraints)
    }

    fun handleRemoteAnswer(answerSdp: String) {
        val pc = peerConnection ?: return
        val desc = SessionDescription(SessionDescription.Type.ANSWER, answerSdp)
        pc.setRemoteDescription(object : SdpObserver {
            override fun onCreateSuccess(p0: SessionDescription?) {}
            override fun onSetSuccess() {
                Log.i(TAG, "Remote answer description set successfully")
                isRemoteDescriptionSet = true
                drainPendingIceCandidates()
            }
            override fun onCreateFailure(p0: String?) {}
            override fun onSetFailure(error: String?) {
                Log.e(TAG, "setRemoteDescription (answer) failed: $error")
            }
        }, desc)
    }

    fun addIceCandidate(candidate: IceCandidate) {
        val pc = peerConnection
        if (pc == null || !isRemoteDescriptionSet) {
            pendingIceCandidates.add(candidate)
            return
        }
        try {
            pc.addIceCandidate(candidate)
        } catch (e: Throwable) {
            Log.w(TAG, "addIceCandidate failed: ${e.message}")
        }
    }

    private fun drainPendingIceCandidates() {
        val pc = peerConnection ?: return
        for (cand in pendingIceCandidates) {
            try {
                pc.addIceCandidate(cand)
            } catch (e: Throwable) {
                Log.w(TAG, "drain candidate error: ${e.message}")
            }
        }
        pendingIceCandidates.clear()
    }

    fun setAudioEnabled(enabled: Boolean) {
        try {
            localAudioTrack?.setEnabled(enabled)
        } catch (e: Throwable) {
            Log.w(TAG, "setAudioEnabled error: ${e.message}")
        }
    }

    fun setVideoEnabled(enabled: Boolean) {
        try {
            localVideoTrack?.setEnabled(enabled)
        } catch (e: Throwable) {
            Log.w(TAG, "setVideoEnabled error: ${e.message}")
        }
    }

    fun switchCamera() {
        try {
            videoCapturer?.switchCamera(null)
        } catch (e: Throwable) {
            Log.w(TAG, "switchCamera error: ${e.message}")
        }
    }

    fun close() {
        try {
            audioManager?.mode = AudioManager.MODE_NORMAL
            audioManager?.isSpeakerphoneOn = false
        } catch (e: Throwable) {
            Log.w(TAG, "AudioManager reset: ${e.message}")
        }

        try {
            videoCapturer?.stopCapture()
        } catch (_: Throwable) {}
        try {
            videoCapturer?.dispose()
        } catch (_: Throwable) {}
        videoCapturer = null

        try {
            surfaceTextureHelper?.dispose()
        } catch (_: Throwable) {}
        surfaceTextureHelper = null

        try {
            localVideoTrack?.dispose()
        } catch (_: Throwable) {}
        localVideoTrack = null

        try {
            localVideoSource?.dispose()
        } catch (_: Throwable) {}
        localVideoSource = null

        try {
            localAudioTrack?.dispose()
        } catch (_: Throwable) {}
        localAudioTrack = null

        try {
            localAudioSource?.dispose()
        } catch (_: Throwable) {}
        localAudioSource = null

        try {
            peerConnection?.close()
            peerConnection?.dispose()
        } catch (_: Throwable) {}
        peerConnection = null

        try {
            peerConnectionFactory?.dispose()
        } catch (_: Throwable) {}
        peerConnectionFactory = null

        try {
            audioDeviceModule?.release()
        } catch (_: Throwable) {}
        audioDeviceModule = null

        pendingIceCandidates.clear()
        isRemoteDescriptionSet = false
        Log.i(TAG, "WebRTCManager closed and cleaned up")
    }
}
