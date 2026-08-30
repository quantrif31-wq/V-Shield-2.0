package com.vshield.mobile.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Send
import androidx.compose.material.icons.filled.DoneAll
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material.icons.filled.Videocam
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalSoftwareKeyboardController
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import android.content.pm.PackageManager
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.core.content.ContextCompat
import com.vshield.mobile.data.model.ChatMessageInfo
import com.vshield.mobile.data.model.ConversationInfo
import com.vshield.mobile.viewmodel.ChatViewModel
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ConversationScreen(
    conversationId: Int,
    chatViewModel: ChatViewModel,
    onBack: () -> Unit,
    onStartCall: (targetEmployeeId: Int, targetFullName: String, type: String) -> Unit
) {
    val uiState by chatViewModel.uiState.collectAsState()
    val listState = rememberLazyListState()
    val keyboardController = LocalSoftwareKeyboardController.current
    val context = LocalContext.current

    val conversation = remember(conversationId, uiState.conversations) {
        uiState.conversations.find { it.conversationId == conversationId }
    }

    val otherParticipants = remember(conversation, uiState.myEmployeeId) {
        conversation?.participants?.filter { it.employeeId != uiState.myEmployeeId } ?: emptyList()
    }
    val fallbackSenderName = remember(uiState.messages, uiState.myEmployeeId) {
        uiState.messages.firstOrNull { it.senderId != uiState.myEmployeeId }?.senderName
    }
    val displayName = conversation?.title ?: (if (otherParticipants.isNotEmpty()) otherParticipants.joinToString(", ") { it.fullName } else (fallbackSenderName ?: "Cuộc trò chuyện"))

    val audioPermissions = remember {
        buildList {
            add(android.Manifest.permission.RECORD_AUDIO)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                add(android.Manifest.permission.BLUETOOTH_CONNECT)
            }
        }.toTypedArray()
    }

    val videoPermissions = remember {
        buildList {
            add(android.Manifest.permission.RECORD_AUDIO)
            add(android.Manifest.permission.CAMERA)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                add(android.Manifest.permission.BLUETOOTH_CONNECT)
            }
        }.toTypedArray()
    }

    fun hasPermissions(perms: Array<String>): Boolean {
        return perms.all {
            ContextCompat.checkSelfPermission(context, it) == PackageManager.PERMISSION_GRANTED
        }
    }

    val audioPermissionLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.RequestMultiplePermissions()
    ) { map ->
        val target = otherParticipants.firstOrNull() ?: return@rememberLauncherForActivityResult
        val audioGranted = map[android.Manifest.permission.RECORD_AUDIO] == true ||
                ContextCompat.checkSelfPermission(context, android.Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED
        if (audioGranted) {
            onStartCall(target.employeeId, target.fullName, "audio")
        }
    }

    val videoPermissionLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.RequestMultiplePermissions()
    ) { map ->
        val target = otherParticipants.firstOrNull() ?: return@rememberLauncherForActivityResult
        val audioGranted = map[android.Manifest.permission.RECORD_AUDIO] == true ||
                ContextCompat.checkSelfPermission(context, android.Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED
        val cameraGranted = map[android.Manifest.permission.CAMERA] == true ||
                ContextCompat.checkSelfPermission(context, android.Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED
        if (audioGranted && cameraGranted) {
            onStartCall(target.employeeId, target.fullName, "video")
        }
    }

    var messageText by remember { mutableStateOf("") }

    LaunchedEffect(conversationId) {
        chatViewModel.openConversation(conversationId)
    }

    LaunchedEffect(uiState.messages.size) {
        if (uiState.messages.isNotEmpty()) {
            listState.animateScrollToItem(uiState.messages.size - 1)
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(
                            text = displayName,
                            maxLines = 1,
                            overflow = TextOverflow.Ellipsis,
                            fontSize = 16.sp
                        )
                        if (uiState.typingUser != null) {
                            Text(
                                text = "${uiState.typingUser} đang nhập...",
                                fontSize = 12.sp,
                                color = MaterialTheme.colorScheme.primary
                            )
                        }
                    }
                },
                navigationIcon = {
                    IconButton(onClick = {
                        chatViewModel.setCurrentConversation(null)
                        onBack()
                    }) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Quay lại")
                    }
                },
                actions = {
                    if (otherParticipants.size == 1) {
                        val target = otherParticipants.first()
                        IconButton(onClick = {
                            if (hasPermissions(audioPermissions)) {
                                onStartCall(target.employeeId, target.fullName, "audio")
                            } else {
                                audioPermissionLauncher.launch(audioPermissions)
                            }
                        }) {
                            Icon(Icons.Filled.Phone, contentDescription = "Gọi thoại")
                        }
                        IconButton(onClick = {
                            if (hasPermissions(videoPermissions)) {
                                onStartCall(target.employeeId, target.fullName, "video")
                            } else {
                                videoPermissionLauncher.launch(videoPermissions)
                            }
                        }) {
                            Icon(Icons.Filled.Videocam, contentDescription = "Gọi video")
                        }
                    }
                }
            )
        }
    ) { padding ->
        Column(modifier = Modifier.fillMaxSize().padding(padding)) {
            if (uiState.isLoadingMessages) {
                Box(
                    modifier = Modifier.weight(1f).fillMaxWidth(),
                    contentAlignment = Alignment.Center
                ) {
                    CircularProgressIndicator()
                }
            } else if (uiState.messages.isEmpty()) {
                Box(
                    modifier = Modifier.weight(1f).fillMaxWidth(),
                    contentAlignment = Alignment.Center
                ) {
                    Text("Chưa có tin nhắn nào", color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            } else {
                LazyColumn(
                    modifier = Modifier.weight(1f).fillMaxWidth(),
                    state = listState,
                    contentPadding = PaddingValues(horizontal = 8.dp, vertical = 8.dp)
                ) {
                    items(uiState.messages, key = { it.messageId }) { msg ->
                        MessageBubble(
                            message = msg,
                            isMine = msg.senderId == uiState.myEmployeeId,
                            showSender = otherParticipants.size > 1
                        )
                    }
                }
            }

            HorizontalDivider(thickness = 0.5.dp)

            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(8.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                OutlinedTextField(
                    value = messageText,
                    onValueChange = {
                        messageText = it
                        if (it.isNotBlank()) chatViewModel.sendTyping()
                    },
                    modifier = Modifier.weight(1f),
                    placeholder = { Text("Nhập tin nhắn...") },
                    singleLine = true,
                    keyboardOptions = KeyboardOptions(imeAction = ImeAction.Send),
                    keyboardActions = KeyboardActions(
                        onSend = {
                            if (messageText.isNotBlank()) {
                                chatViewModel.sendMessage(messageText)
                                messageText = ""
                                keyboardController?.hide()
                            }
                        }
                    )
                )
                Spacer(modifier = Modifier.width(8.dp))
                FilledIconButton(
                    onClick = {
                        if (messageText.isNotBlank()) {
                            chatViewModel.sendMessage(messageText)
                            messageText = ""
                            keyboardController?.hide()
                        }
                    },
                    enabled = messageText.isNotBlank()
                ) {
                    Icon(Icons.AutoMirrored.Filled.Send, contentDescription = "Gửi")
                }
            }
        }
    }
}

@Composable
private fun MessageBubble(
    message: ChatMessageInfo,
    isMine: Boolean,
    showSender: Boolean
) {
    val bubbleColor = if (isMine) MaterialTheme.colorScheme.primary
        else MaterialTheme.colorScheme.surfaceVariant
    val textColor = if (isMine) MaterialTheme.colorScheme.onPrimary
        else MaterialTheme.colorScheme.onSurfaceVariant
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 2.dp),
        horizontalAlignment = if (isMine) Alignment.End else Alignment.Start
    ) {
        if (showSender && !isMine) {
            Text(
                text = message.senderName ?: "",
                fontSize = 11.sp,
                color = MaterialTheme.colorScheme.primary,
                fontWeight = FontWeight.Medium,
                modifier = Modifier.padding(start = 8.dp, bottom = 2.dp)
            )
        }

        Row(verticalAlignment = Alignment.Bottom) {
            if (isMine) {
                if (message.isRead) {
                    Icon(
                        Icons.Default.DoneAll,
                        contentDescription = "Đã đọc",
                        modifier = Modifier.size(14.dp).padding(end = 2.dp),
                        tint = MaterialTheme.colorScheme.primary
                    )
                }
            }

            Surface(
                shape = RoundedCornerShape(
                    topStart = 16.dp,
                    topEnd = 16.dp,
                    bottomStart = if (isMine) 16.dp else 4.dp,
                    bottomEnd = if (isMine) 4.dp else 16.dp
                ),
                color = bubbleColor
            ) {
                Column(modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp)) {
                    Text(
                        text = message.content ?: "",
                        color = if (isMine) MaterialTheme.colorScheme.onPrimary
                            else MaterialTheme.colorScheme.onSurface,
                        fontSize = 15.sp
                    )
                    Text(
                        text = formatMessageTime(message.sentAt ?: ""),
                        fontSize = 10.sp,
                        color = textColor,
                        modifier = Modifier.align(Alignment.End)
                    )
                }
            }
        }
    }
}

private fun formatMessageTime(isoString: String): String {
    return try {
        val instant = Instant.parse(isoString)
        val local = instant.atZone(ZoneId.systemDefault()).toLocalDateTime()
        local.format(DateTimeFormatter.ofPattern("HH:mm", Locale.getDefault()))
    } catch (_: Exception) { "" }
}
