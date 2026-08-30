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

            val encoderFactory = try {
                DefaultVideoEncoderFactory(eglBaseContext, true, true)
            } catch (e: Throwable) {
                Log.w(TAG, "DefaultVideoEncoderFactory fallback: ${e.message}")
                SoftwareVideoEncoderFactory()
            }
            val decoderFactory = try {
                DefaultVideoDecoderFactory(eglBaseContext)
            } catch (e: Throwable) {
                Log.w(TAG, "DefaultVideoDecoderFactory fallback: ${e.message}")
                SoftwareVideoDecoderFactory()
            }

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
            val useHardwareAEC = try {
                JavaAudioDeviceModule.isBuiltInAcousticEchoCancelerSupported()
            } catch (_: Throwable) { false }

            val useHardwareNS = try {
                JavaAudioDeviceModule.isBuiltInNoiseSuppressorSupported()
            } catch (_: Throwable) { false }

            JavaAudioDeviceModule.builder(context.applicationContext)
                .setUseHardwareAcousticEchoCanceler(useHardwareAEC)
                .setUseHardwareNoiseSuppressor(useHardwareNS)
                .setAudioSource(android.media.MediaRecorder.AudioSource.VOICE_COMMUNICATION)
                .setAudioAttributes(
                    android.media.AudioAttributes.Builder()
                        .setUsage(android.media.AudioAttributes.USAGE_VOICE_COMMUNICATION)
                        .setContentType(android.media.AudioAttributes.CONTENT_TYPE_SPEECH)
                        .build()
                )
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

    fun setSpeakerphoneOn(on: Boolean) {
        try {
            audioManager?.mode = AudioManager.MODE_IN_COMMUNICATION
            audioManager?.isSpeakerphoneOn = on
            if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.S) {
                if (on) {
                    val speakerDevice = audioManager?.availableCommunicationDevices?.firstOrNull {
                        it.type == android.media.AudioDeviceInfo.TYPE_BUILTIN_SPEAKER
                    }
                    if (speakerDevice != null) {
                        audioManager?.setCommunicationDevice(speakerDevice)
                    }
                } else {
                    val earpieceDevice = audioManager?.availableCommunicationDevices?.firstOrNull {
                        it.type == android.media.AudioDeviceInfo.TYPE_BUILTIN_EARPIECE
                    }
                    if (earpieceDevice != null) {
                        audioManager?.setCommunicationDevice(earpieceDevice)
                    } else {
                        audioManager?.clearCommunicationDevice()
                    }
                }
            }
            Log.i(TAG, "Speakerphone set to: $on")
        } catch (e: Throwable) {
            Log.w(TAG, "setSpeakerphoneOn error: ${e.message}")
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
                            stream?.audioTracks?.forEach {
                                it.setEnabled(true)
                                it.setVolume(1.0)
                                Log.i(TAG, "onAddStream: AudioTrack enabled with full volume")
                            }
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
                            } else if (track is org.webrtc.AudioTrack) {
                                track.setEnabled(true)
                                track.setVolume(1.0)
                                Log.i(TAG, "onAddTrack: Remote AudioTrack enabled with full volume")
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
            // Set audio mode for clear, loud VoIP phone call
            try {
                audioManager?.mode = AudioManager.MODE_IN_COMMUNICATION
                setSpeakerphoneOn(true) // Default to loudspeaker so audio is loud and clear

                // Ensure in-call volume is raised to comfortable level
                val maxVol = audioManager?.getStreamMaxVolume(AudioManager.STREAM_VOICE_CALL) ?: 0
                val currentVol = audioManager?.getStreamVolume(AudioManager.STREAM_VOICE_CALL) ?: 0
                if (maxVol > 0 && currentVol < (maxVol * 0.7).toInt()) {
                    audioManager?.setStreamVolume(AudioManager.STREAM_VOICE_CALL, (maxVol * 0.85).toInt(), 0)
                }
            } catch (e: Throwable) {
                Log.w(TAG, "AudioManager mode setup: ${e.message}")
            }

            val audioConstraints = MediaConstraints().apply {
                mandatory.add(MediaConstraints.KeyValuePair("googEchoCancellation", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googNoiseSuppression", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googAutoGainControl", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googHighpassFilter", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googTypingNoiseDetection", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googAudioMirroring", "false"))
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
                        try {
                            capturer.startCapture(640, 480, 30)
                        } catch (_: Throwable) {
                            try {
                                capturer.startCapture(320, 240, 15)
                            } catch (_: Throwable) {}
                        }

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
            val enumerator = if (Camera2Enumerator.isSupported(context.applicationContext)) {
                Camera2Enumerator(context.applicationContext)
            } else {
                Camera1Enumerator(true)
            }
            val deviceNames = enumerator.deviceNames
            val front = deviceNames.firstOrNull { enumerator.isFrontFacing(it) }
            val back = deviceNames.firstOrNull { enumerator.isBackFacing(it) }
            val selected = front ?: back ?: deviceNames.firstOrNull()
            selected?.let { enumerator.createCapturer(it, null) }
        } catch (e: Throwable) {
            try {
                val fallback = Camera1Enumerator(true)
                val deviceNames = fallback.deviceNames
                val front = deviceNames.firstOrNull { fallback.isFrontFacing(it) }
                val back = deviceNames.firstOrNull { fallback.isBackFacing(it) }
                val selected = front ?: back ?: deviceNames.firstOrNull()
                selected?.let { fallback.createCapturer(it, null) }
            } catch (e2: Throwable) {
                Log.w(TAG, "createCameraCapturer failed: ${e2.message}")
                null
            }
        }
    }

    fun createOffer(enableVideo: Boolean = false) {
        val pc = peerConnection ?: run {
            listener?.onError("PeerConnection chưa sẵn sàng")
            return
        }

        val constraints = MediaConstraints().apply {
            mandatory.add(MediaConstraints.KeyValuePair("OfferToReceiveAudio", "true"))
            mandatory.add(MediaConstraints.KeyValuePair("OfferToReceiveVideo", if (enableVideo) "true" else "false"))
        }

        pc.createOffer(object : SdpObserver {
            override fun onCreateSuccess(desc: SessionDescription) {
                pc.setLocalDescription(object : SdpObserver {
                    override fun onCreateSuccess(p0: SessionDescription?) {}
                    override fun onSetSuccess() {
                        Log.i(TAG, "Local offer description set successfully (enableVideo=$enableVideo)")
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

    private fun normalizeSdp(sdp: String): String {
        return sdp.replace("\r\n", "\n")
            .replace("\r", "\n")
            .split("\n")
            .filter { it.isNotBlank() }
            .joinToString("\r\n", postfix = "\r\n")
    }

    fun handleRemoteOffer(offerSdp: String, enableVideo: Boolean = false) {
        val pc = peerConnection ?: return
        val normalized = normalizeSdp(offerSdp)
        val hasVideo = enableVideo || normalized.contains("m=video")
        val desc = SessionDescription(SessionDescription.Type.OFFER, normalized)
        pc.setRemoteDescription(object : SdpObserver {
            override fun onCreateSuccess(p0: SessionDescription?) {}
            override fun onSetSuccess() {
                Log.i(TAG, "Remote offer description set successfully (hasVideo=$hasVideo)")
                isRemoteDescriptionSet = true
                drainPendingIceCandidates()
                createAnswer(hasVideo)
            }
            override fun onCreateFailure(p0: String?) {}
            override fun onSetFailure(error: String?) {
                Log.e(TAG, "setRemoteDescription (offer) failed: $error")
            }
        }, desc)
    }

    private fun createAnswer(enableVideo: Boolean = false) {
        val pc = peerConnection ?: return
        val constraints = MediaConstraints().apply {
            mandatory.add(MediaConstraints.KeyValuePair("OfferToReceiveAudio", "true"))
            mandatory.add(MediaConstraints.KeyValuePair("OfferToReceiveVideo", if (enableVideo) "true" else "false"))
        }

        pc.createAnswer(object : SdpObserver {
            override fun onCreateSuccess(desc: SessionDescription) {
                pc.setLocalDescription(object : SdpObserver {
                    override fun onCreateSuccess(p0: SessionDescription?) {}
                    override fun onSetSuccess() {
                        Log.i(TAG, "Local answer description set successfully (enableVideo=$enableVideo)")
                        mainHandler.post {
                            listener?.onAnswerCreated(desc.description)
                        }
                    }
                    override fun onCreateFailure(p0: String?) {}
                    override fun onSetFailure(error: String?) {
                        Log.e(TAG, "setLocalDescription (answer) failed: $error")
                    }
                }, desc)
            }

            override fun onSetSuccess() {}
            override fun onCreateFailure(error: String?) {
                Log.e(TAG, "createAnswer failure: $error")
            }
            override fun onSetFailure(p0: String?) {}
        }, constraints)
    }

    fun handleRemoteAnswer(answerSdp: String) {
        val pc = peerConnection ?: return
        val normalized = normalizeSdp(answerSdp)
        val desc = SessionDescription(SessionDescription.Type.ANSWER, normalized)
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
            setSpeakerphoneOn(false)
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
