package com.vshield.mobile.webrtc

import android.content.Context
import android.os.Handler
import android.os.HandlerThread
import android.util.Log
import org.webrtc.*
import org.webrtc.audio.AudioDeviceModule
import org.webrtc.audio.JavaAudioDeviceModule

class WebRTCManager(private val context: Context) {

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
        PeerConnection.IceServer.builder("stun:stun1.l.google.com:19302").createIceServer()
    )

    private lateinit var rootEglBase: EglBase

    fun initialize() {
        if (isInitialized) return
        try {
            try {
                System.loadLibrary("jingle_peerconnection_so")
            } catch (e: Throwable) {
                Log.w("WebRTCManager", "loadLibrary jingle_peerconnection_so failed, trying alternative", e)
                try {
                    System.loadLibrary("jingle_peerconnection")
                } catch (e2: Throwable) {
                    listener?.onError("Không load được thư viện WebRTC native")
                    return
                }
            }

            rootEglBase = EglBase.create()
            val encoderFactory = DefaultVideoEncoderFactory(rootEglBase.eglBaseContext, true, true)
            val decoderFactory = DefaultVideoDecoderFactory(rootEglBase.eglBaseContext)

            val initOptions = PeerConnectionFactory.InitializationOptions.builder(context)
                .setEnableInternalTracer(false)
                .createInitializationOptions()
            PeerConnectionFactory.initialize(initOptions)

            peerConnectionFactory = PeerConnectionFactory.builder()
                .setVideoEncoderFactory(encoderFactory)
                .setVideoDecoderFactory(decoderFactory)
                .setAudioDeviceModule(createAudioDeviceModule())
                .createPeerConnectionFactory()

            isInitialized = true
        } catch (e: Exception) {
            Log.e("WebRTCManager", "init failed", e)
            listener?.onError("Không thể khởi tạo WebRTC: ${e.message}")
        }
    }

    private fun createAudioDeviceModule(): AudioDeviceModule {
        audioDeviceModule = JavaAudioDeviceModule.builder(context)
            .setUseHardwareAcousticEchoCanceler(true)
            .setUseHardwareNoiseSuppressor(true)
            .createAudioDeviceModule()
        return audioDeviceModule!!
    }

    fun hasPeerConnection(): Boolean = peerConnection != null

    fun createPeerConnection(): Boolean {
        if (peerConnection != null) return true
        val factory = peerConnectionFactory ?: return false

        val config = PeerConnection.RTCConfiguration(iceServers)
        config.sdpSemantics = PeerConnection.SdpSemantics.UNIFIED_PLAN
        config.continualGatheringPolicy = PeerConnection.ContinualGatheringPolicy.GATHER_CONTINUALLY

        val pc = factory.createPeerConnection(config, object : PeerConnection.Observer {
            override fun onIceCandidate(candidate: IceCandidate) {
                listener?.onIceCandidate(candidate)
            }

            override fun onIceCandidatesRemoved(candidates: Array<out IceCandidate>) {}
            override fun onSignalingChange(p0: PeerConnection.SignalingState?) {}
            override fun onIceConnectionChange(state: PeerConnection.IceConnectionState?) {
                listener?.onConnectionStateChanged(state?.toString() ?: "unknown")
            }
            override fun onIceConnectionReceivingChange(p0: Boolean) {}
            override fun onIceGatheringChange(p0: PeerConnection.IceGatheringState?) {}
            override fun onAddStream(p0: MediaStream?) {
                p0?.videoTracks?.firstOrNull()?.let { listener?.onRemoteVideo(it) }
            }
            override fun onRemoveStream(p0: MediaStream?) {}
            override fun onDataChannel(p0: DataChannel?) {}
            override fun onRenegotiationNeeded() {}
            override fun onAddTrack(receiver: RtpReceiver?, streams: Array<out MediaStream>?) {
                val track = receiver?.track()
                if (track is VideoTrack) {
                    listener?.onRemoteVideo(track)
                }
            }
        })

        if (pc == null) return false
        peerConnection = pc
        return true
    }

    fun setupLocalMedia(): Boolean {
        val factory = peerConnectionFactory ?: return false
        val pc = peerConnection ?: return false

        try {
            val audioConstraints = MediaConstraints().apply {
                mandatory.add(MediaConstraints.KeyValuePair("googEchoCancellation", "true"))
                mandatory.add(MediaConstraints.KeyValuePair("googNoiseSuppression", "true"))
            }

            val audioSource = factory.createAudioSource(audioConstraints)
            localAudioTrack = factory.createAudioTrack("audio0", audioSource)

            localVideoSource = factory.createVideoSource(false)
            videoCapturer = createCameraCapturer()
            videoCapturer?.initialize(
                SurfaceTextureHelper.create("VideoCaptureThread", rootEglBase.eglBaseContext),
                context.applicationContext,
                localVideoSource?.capturerObserver
            )
            videoCapturer?.startCapture(1280, 720, 30)
            localVideoTrack = factory.createVideoTrack("video0", localVideoSource)
            localVideoTrack?.setEnabled(true)

            pc.addTrack(localAudioTrack, emptyList())
            pc.addTrack(localVideoTrack, emptyList())

            listener?.onLocalVideo(localVideoTrack!!)
            return true
        } catch (e: Exception) {
            Log.e("WebRTCManager", "setupLocalMedia failed", e)
            listener?.onError("Không thể bật camera/mic: ${e.message}")
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
        } catch (e: Exception) {
            Log.e("WebRTCManager", "camera enumerator failed", e)
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
                listener?.onError("Không thể tạo offer")
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
                listener?.onError("Không thể nhận offer")
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
                listener?.onError("Không thể tạo answer")
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
                listener?.onError("Không thể nhận answer")
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
            } catch (_: Exception) {}
            videoCapturer?.dispose()
            videoCapturer = null
            localVideoTrack = null
            localAudioTrack = null
            localVideoSource?.dispose()
            localVideoSource = null
            peerConnection?.close()
            peerConnection = null
            audioDeviceModule?.release()
            audioDeviceModule = null
        }
    }
}
