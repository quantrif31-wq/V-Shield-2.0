package com.vshield.mobile.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CallEnd
import androidx.compose.material.icons.filled.Mic
import androidx.compose.material.icons.filled.MicOff
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PhotoCamera
import androidx.compose.material.icons.filled.VideocamOff
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
import com.vshield.mobile.data.model.ChatCallState
import com.vshield.mobile.viewmodel.ChatViewModel
import org.webrtc.SurfaceViewRenderer
import org.webrtc.VideoTrack

@Composable
fun IncomingCallDialog(
    callState: ChatCallState.Incoming,
    onAccept: () -> Unit,
    onReject: () -> Unit,
    onDismiss: () -> Unit
) {
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
                            Icons.Default.Person,
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
                    text = "Đang gọi...",
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
                    Icon(Icons.Default.CallEnd, contentDescription = "Từ chối")
                    Spacer(modifier = Modifier.width(4.dp))
                    Text("Từ chối", color = MaterialTheme.colorScheme.onError)
                }
                FilledTonalButton(
                    onClick = onAccept,
                    colors = ButtonDefaults.filledTonalButtonColors(
                        containerColor = MaterialTheme.colorScheme.primary
                    )
                ) {
                    Icon(Icons.Default.PhotoCamera, contentDescription = "Chấp nhận")
                    Spacer(modifier = Modifier.width(4.dp))
                    Text("Chấp nhận")
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
                onEnd = { chatViewModel.endCall() },
                localVideoTrack = uiState.localVideoTrack
            )
        }

        is ChatCallState.Connected -> {
            ConnectedCallScreen(
                fullName = call.withFullName,
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
    AndroidView(
        modifier = modifier,
        factory = { ctx ->
            SurfaceViewRenderer(ctx).apply {
                setMirror(isMirror)
                setZOrderMediaOverlay(true)
                init(sharedEglBase.eglBaseContext, null)
                videoTrack?.addSink(this)
            }
        },
        update = { view ->
            videoTrack?.let { track ->
                track.removeSink(view)
                track.addSink(view)
            }
        },
        onRelease = { view ->
            videoTrack?.removeSink(view)
            view.release()
        }
    )
}

private val sharedEglBase: org.webrtc.EglBase by lazy {
    org.webrtc.EglBase.create()
}

@Composable
private fun OutgoingCallScreen(
    fullName: String,
    onEnd: () -> Unit,
    localVideoTrack: VideoTrack?
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF1A1A2E)),
        contentAlignment = Alignment.Center
    ) {
        if (localVideoTrack != null) {
            VideoSurfaceView(
                videoTrack = localVideoTrack,
                isMirror = true,
                modifier = Modifier.fillMaxSize()
            )
        }

        Column(
            modifier = Modifier.fillMaxSize(),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(modifier = Modifier.height(120.dp))
            Surface(
                modifier = Modifier.size(80.dp),
                shape = CircleShape,
                color = MaterialTheme.colorScheme.primaryContainer
            ) {
                Box(contentAlignment = Alignment.Center) {
                    Icon(
                        Icons.Default.Person,
                        contentDescription = null,
                        modifier = Modifier.size(40.dp),
                        tint = MaterialTheme.colorScheme.onPrimaryContainer
                    )
                }
            }
            Spacer(modifier = Modifier.height(16.dp))
            Text(text = fullName, fontWeight = FontWeight.Bold, fontSize = 24.sp, color = Color.White)
            Spacer(modifier = Modifier.height(8.dp))
            Text(text = "Đang gọi...", color = Color.White.copy(alpha = 0.8f))
            Spacer(modifier = Modifier.weight(1f))
            FilledIconButton(
                onClick = onEnd,
                modifier = Modifier.size(64.dp).padding(bottom = 40.dp),
                colors = IconButtonDefaults.filledIconButtonColors(
                    containerColor = MaterialTheme.colorScheme.error
                )
            ) {
                Icon(
                    Icons.Default.CallEnd,
                    contentDescription = "Kết thúc",
                    modifier = Modifier.size(32.dp)
                )
            }
        }
    }
}

@Composable
private fun ConnectedCallScreen(
    fullName: String,
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
            .background(Color(0xFF1A1A2E))
    ) {
        if (remoteVideoTrack != null) {
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
            }
        }

        if (localVideoTrack != null && !isCameraOff) {
            Box(
                modifier = Modifier
                    .align(Alignment.TopEnd)
                    .padding(16.dp)
                    .width(110.dp)
                    .height(160.dp)
                    .clip(RoundedCornerShape(12.dp))
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
            if (remoteVideoTrack == null) {
                Text(
                    text = fullName,
                    fontWeight = FontWeight.Bold,
                    fontSize = 22.sp,
                    color = Color.White,
                    textAlign = TextAlign.Center
                )
            }
            Spacer(modifier = Modifier.weight(1f))

            Row(
                horizontalArrangement = Arrangement.spacedBy(24.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                FilledIconButton(
                    onClick = onToggleMic,
                    modifier = Modifier.size(56.dp),
                    colors = IconButtonDefaults.filledIconButtonColors(
                        containerColor = if (isMicMuted) Color(0xFFCC0000) else Color(0xFF37474F)
                    )
                ) {
                    Icon(
                        if (isMicMuted) Icons.Default.MicOff else Icons.Default.Mic,
                        contentDescription = if (isMicMuted) "Bật mic" else "Tắt mic",
                        modifier = Modifier.size(28.dp)
                    )
                }

                FilledIconButton(
                    onClick = onEnd,
                    modifier = Modifier.size(68.dp),
                    colors = IconButtonDefaults.filledIconButtonColors(
                        containerColor = MaterialTheme.colorScheme.error
                    )
                ) {
                    Icon(
                        Icons.Default.CallEnd,
                        contentDescription = "Kết thúc",
                        modifier = Modifier.size(32.dp)
                    )
                }

                FilledIconButton(
                    onClick = onToggleCamera,
                    modifier = Modifier.size(56.dp),
                    colors = IconButtonDefaults.filledIconButtonColors(
                        containerColor = if (isCameraOff) Color(0xFFCC0000) else Color(0xFF37474F)
                    )
                ) {
                    Icon(
                        Icons.Default.VideocamOff,
                        contentDescription = if (isCameraOff) "Bật camera" else "Tắt camera",
                        modifier = Modifier.size(28.dp)
                    )
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Text(
                text = "Đang trong cuộc gọi",
                color = Color.White.copy(alpha = 0.7f),
                fontSize = 13.sp
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
