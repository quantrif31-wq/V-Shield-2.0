package com.vshield.mobile.ui.screen

import android.util.Log
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import com.google.accompanist.permissions.ExperimentalPermissionsApi
import com.google.accompanist.permissions.rememberMultiplePermissionsState
import com.vshield.mobile.data.model.ChatCallState
import com.vshield.mobile.viewmodel.ChatViewModel
import com.vshield.mobile.webrtc.WebRTCManager
import org.webrtc.RendererCommon
import org.webrtc.SurfaceViewRenderer
import org.webrtc.VideoTrack

@OptIn(ExperimentalPermissionsApi::class)
@Composable
fun IncomingCallDialog(
    callState: ChatCallState.Incoming,
    onAccept: () -> Unit,
    onReject: () -> Unit,
    onDismiss: () -> Unit
) {
    val isVideo = callState.callType == "video"
    val permissions = if (isVideo) {
        listOf(android.Manifest.permission.RECORD_AUDIO, android.Manifest.permission.CAMERA)
    } else {
        listOf(android.Manifest.permission.RECORD_AUDIO)
    }
    val callPermissionsState = rememberMultiplePermissionsState(permissions)

    AlertDialog(
        onDismissRequest = onDismiss,
        title = {
            Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.fillMaxWidth()) {
                Surface(
                    modifier = Modifier.size(64.dp),
                    shape = CircleShape,
                    color = MaterialTheme.colorScheme.primaryContainer
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Icon(
                            if (isVideo) Icons.Default.Videocam else Icons.Default.Person,
                            contentDescription = null,
                            modifier = Modifier.size(32.dp),
                            tint = MaterialTheme.colorScheme.onPrimaryContainer
                        )
                    }
                }
                Spacer(modifier = Modifier.height(12.dp))
                Text(
                    text = callState.fromFullName,
                    fontWeight = FontWeight.Bold,
                    fontSize = 20.sp
                )
                Text(
                    text = if (isVideo) "Cuộc gọi Video Face-to-Face..." else "Cuộc gọi thoại HD đến...",
                    fontSize = 14.sp,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        },
        text = {},
        confirmButton = {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                FilledTonalButton(
                    onClick = onReject,
                    colors = ButtonDefaults.filledTonalButtonColors(
                        containerColor = MaterialTheme.colorScheme.error
                    )
                ) {
                    Icon(Icons.Default.CallEnd, contentDescription = "Từ chối", tint = MaterialTheme.colorScheme.onError)
                    Spacer(modifier = Modifier.width(4.dp))
                    Text("Từ chối", color = MaterialTheme.colorScheme.onError)
                }
                FilledTonalButton(
                    onClick = {
                        if (callPermissionsState.allPermissionsGranted) {
                            onAccept()
                        } else {
                            callPermissionsState.launchMultiplePermissionRequest()
                            onAccept()
                        }
                    },
                    colors = ButtonDefaults.filledTonalButtonColors(
                        containerColor = Color(0xFF10B981)
                    )
                ) {
                    Icon(Icons.Default.Call, contentDescription = "Trả lời", tint = Color.White)
                    Spacer(modifier = Modifier.width(4.dp))
                    Text("Trả lời", color = Color.White)
                }
            }
        }
    )
}

@Composable
fun CallOverlay(chatViewModel: ChatViewModel) {
    val uiState by chatViewModel.uiState.collectAsState()

    when (val call = uiState.callState) {
        is ChatCallState.Incoming -> {
            IncomingCallDialog(
                callState = call,
                onAccept = { chatViewModel.acceptCall() },
                onReject = { chatViewModel.rejectCall() },
                onDismiss = { chatViewModel.rejectCall() }
            )
        }

        is ChatCallState.Outgoing -> {
            OutgoingCallScreen(
                fullName = call.toFullName,
                callType = call.callType,
                onEnd = { chatViewModel.endCall() },
                localVideoTrack = uiState.localVideoTrack
            )
        }

        is ChatCallState.Connected -> {
            ConnectedCallScreen(
                fullName = call.withFullName,
                callType = call.callType,
                onEnd = { chatViewModel.endCall() },
                onToggleMic = { chatViewModel.toggleMic() },
                onToggleCamera = { chatViewModel.toggleCamera() },
                onSwitchCamera = { chatViewModel.switchCamera() },
                isMicMuted = uiState.isMicMuted,
                isCameraOff = uiState.isCameraOff,
                localVideoTrack = uiState.localVideoTrack,
                remoteVideoTrack = uiState.remoteVideoTrack,
                callError = uiState.callError,
                onDismissError = { chatViewModel.clearCallError() }
            )
        }

        ChatCallState.Idle -> {}
    }
}

@Composable
private fun VideoSurfaceView(videoTrack: VideoTrack?, isMirror: Boolean, modifier: Modifier = Modifier) {
    if (videoTrack == null) return
    AndroidView(
        modifier = modifier,
        factory = { ctx ->
            SurfaceViewRenderer(ctx).apply {
                setMirror(isMirror)
                setZOrderMediaOverlay(true)
                try {
                    init(WebRTCManager.eglBaseContext, null)
                    setScalingType(RendererCommon.ScalingType.SCALE_ASPECT_FILL)
                    setEnableHardwareScaler(true)
                } catch (e: Throwable) {
                    Log.w("VideoSurfaceView", "SurfaceViewRenderer init: ${e.message}")
                }
                try {
                    videoTrack.addSink(this)
                    tag = videoTrack
                } catch (e: Throwable) {
                    Log.w("VideoSurfaceView", "Initial addSink: ${e.message}")
                }
            }
        },
        update = { view ->
            if (view.tag != videoTrack) {
                try {
                    (view.tag as? VideoTrack)?.removeSink(view)
                } catch (_: Throwable) {}
                try {
                    videoTrack.addSink(view)
                    view.tag = videoTrack
                } catch (e: Throwable) {
                    Log.w("VideoSurfaceView", "update sink: ${e.message}")
                }
            }
        },
        onRelease = { view ->
            try {
                (view.tag as? VideoTrack)?.removeSink(view)
                view.tag = null
                view.release()
            } catch (e: Throwable) {
                Log.w("VideoSurfaceView", "onRelease: ${e.message}")
            }
        }
    )
}

@Composable
private fun OutgoingCallScreen(
    fullName: String,
    callType: String,
    onEnd: () -> Unit,
    localVideoTrack: VideoTrack?
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF0F172A)),
        contentAlignment = Alignment.Center
    ) {
        if (callType == "video" && localVideoTrack != null) {
            VideoSurfaceView(
                videoTrack = localVideoTrack,
                isMirror = true,
                modifier = Modifier.fillMaxSize()
            )
        }

        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            modifier = Modifier
                .fillMaxSize()
                .padding(32.dp)
        ) {
            Spacer(modifier = Modifier.height(48.dp))
            Surface(
                modifier = Modifier.size(96.dp),
                shape = CircleShape,
                color = MaterialTheme.colorScheme.primaryContainer
            ) {
                Box(contentAlignment = Alignment.Center) {
                    Icon(
                        if (callType == "video") Icons.Default.Videocam else Icons.Default.Person,
                        contentDescription = null,
                        modifier = Modifier.size(48.dp),
                        tint = MaterialTheme.colorScheme.onPrimaryContainer
                    )
                }
            }
            Spacer(modifier = Modifier.height(16.dp))
            Text(
                text = fullName,
                fontWeight = FontWeight.Bold,
                fontSize = 24.sp,
                color = Color.White
            )
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = if (callType == "video") "Đang gọi Video Face-to-Face..." else "Đang gọi thoại HD...",
                fontSize = 15.sp,
                color = Color.White.copy(alpha = 0.8f)
            )

            Spacer(modifier = Modifier.weight(1f))

            FilledIconButton(
                onClick = onEnd,
                modifier = Modifier.size(68.dp),
                colors = IconButtonDefaults.filledIconButtonColors(
                    containerColor = MaterialTheme.colorScheme.error
                )
            ) {
                Icon(
                    Icons.Default.CallEnd,
                    contentDescription = "Hủy cuộc gọi",
                    tint = Color.White,
                    modifier = Modifier.size(32.dp)
                )
            }
            Spacer(modifier = Modifier.height(32.dp))
        }
    }
}

@Composable
private fun ConnectedCallScreen(
    fullName: String,
    callType: String,
    onEnd: () -> Unit,
    onToggleMic: () -> Unit,
    onToggleCamera: () -> Unit,
    onSwitchCamera: () -> Unit,
    isMicMuted: Boolean,
    isCameraOff: Boolean,
    localVideoTrack: VideoTrack?,
    remoteVideoTrack: VideoTrack?,
    callError: String?,
    onDismissError: () -> Unit
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF0F172A))
    ) {
        if (callType == "video" && remoteVideoTrack != null) {
            VideoSurfaceView(
                videoTrack = remoteVideoTrack,
                isMirror = false,
                modifier = Modifier.fillMaxSize()
            )
        } else {
            Column(
                modifier = Modifier.fillMaxSize(),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
                Surface(
                    modifier = Modifier.size(96.dp),
                    shape = CircleShape,
                    color = MaterialTheme.colorScheme.primaryContainer
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Icon(
                            Icons.Default.Person,
                            contentDescription = null,
                            modifier = Modifier.size(48.dp),
                            tint = MaterialTheme.colorScheme.onPrimaryContainer
                        )
                    }
                }
                Spacer(modifier = Modifier.height(16.dp))
                Text(text = fullName, fontWeight = FontWeight.Bold, fontSize = 22.sp, color = Color.White)
                Spacer(modifier = Modifier.height(8.dp))
                Text(
                    text = if (callType == "video") "Đang kết nối Video Face-to-Face..." else "Đang trong cuộc gọi thoại HD",
                    fontSize = 14.sp,
                    color = Color(0xFF38BDF8)
                )
            }
        }

        if (callType == "video" && localVideoTrack != null && !isCameraOff) {
            Box(
                modifier = Modifier
                    .align(Alignment.TopEnd)
                    .padding(16.dp)
                    .width(110.dp)
                    .height(160.dp)
                    .clip(RoundedCornerShape(14.dp))
                    .background(Color(0xFF1E293B))
            ) {
                VideoSurfaceView(
                    videoTrack = localVideoTrack,
                    isMirror = true,
                    modifier = Modifier.fillMaxSize()
                )
            }
        }

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(modifier = Modifier.height(32.dp))
            if (remoteVideoTrack != null) {
                Text(
                    text = fullName,
                    fontWeight = FontWeight.Bold,
                    fontSize = 18.sp,
                    color = Color.White,
                    textAlign = TextAlign.Center
                )
            }
            Spacer(modifier = Modifier.weight(1f))

            Row(
                horizontalArrangement = Arrangement.spacedBy(16.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                FilledIconButton(
                    onClick = onToggleMic,
                    modifier = Modifier.size(52.dp),
                    colors = IconButtonDefaults.filledIconButtonColors(
                        containerColor = if (isMicMuted) Color(0xFFEF4444) else Color(0xFF334155)
                    )
                ) {
                    Icon(
                        if (isMicMuted) Icons.Default.MicOff else Icons.Default.Mic,
                        contentDescription = if (isMicMuted) "Bật mic" else "Tắt mic",
                        tint = Color.White,
                        modifier = Modifier.size(24.dp)
                    )
                }

                if (callType == "video") {
                    FilledIconButton(
                        onClick = onSwitchCamera,
                        modifier = Modifier.size(52.dp),
                        colors = IconButtonDefaults.filledIconButtonColors(
                            containerColor = Color(0xFF334155)
                        )
                    ) {
                        Icon(
                            Icons.Default.FlipCameraAndroid,
                            contentDescription = "Đổi camera",
                            tint = Color.White,
                            modifier = Modifier.size(24.dp)
                        )
                    }

                    FilledIconButton(
                        onClick = onToggleCamera,
                        modifier = Modifier.size(52.dp),
                        colors = IconButtonDefaults.filledIconButtonColors(
                            containerColor = if (isCameraOff) Color(0xFFEF4444) else Color(0xFF334155)
                        )
                    ) {
                        Icon(
                            if (isCameraOff) Icons.Default.VideocamOff else Icons.Default.Videocam,
                            contentDescription = if (isCameraOff) "Bật camera" else "Tắt camera",
                            tint = Color.White,
                            modifier = Modifier.size(24.dp)
                        )
                    }
                }

                FilledIconButton(
                    onClick = onEnd,
                    modifier = Modifier.size(64.dp),
                    colors = IconButtonDefaults.filledIconButtonColors(
                        containerColor = MaterialTheme.colorScheme.error
                    )
                ) {
                    Icon(
                        Icons.Default.CallEnd,
                        contentDescription = "Kết thúc",
                        tint = Color.White,
                        modifier = Modifier.size(30.dp)
                    )
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Text(
                text = "V-Shield Real-time Call",
                color = Color.White.copy(alpha = 0.6f),
                fontSize = 12.sp
            )
        }
    }

    if (callError != null) {
        AlertDialog(
            onDismissRequest = onDismissError,
            title = { Text("Lỗi cuộc gọi") },
            text = { Text(callError) },
            confirmButton = {
                TextButton(onClick = onDismissError) { Text("Đóng") }
            }
        )
    }
}
