package com.vshield.mobile.ui.screen

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vshield.mobile.data.model.ConversationInfo
import com.vshield.mobile.data.model.ContactInfo
import com.vshield.mobile.viewmodel.ChatViewModel
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ChatListScreen(
    chatViewModel: ChatViewModel,
    onConversationClick: (ConversationInfo) -> Unit,
    onBack: () -> Unit
) {
    val uiState by chatViewModel.uiState.collectAsState()

    LaunchedEffect(Unit) {
        chatViewModel.initialize()
    }

    var showContactPicker by remember { mutableStateOf(false) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Liên lạc") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Quay lại")
                    }
                }
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { showContactPicker = true }) {
                Icon(Icons.Default.Add, contentDescription = "Tạo hội thoại mới")
            }
        }
    ) { padding ->
        if (uiState.isLoadingConversations && uiState.conversations.isEmpty()) {
            Box(
                modifier = Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator()
            }
        } else if (uiState.conversations.isEmpty()) {
            Box(
                modifier = Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) {
                Text("Chưa có hội thoại nào", style = MaterialTheme.typography.bodyLarge)
            }
        } else {
            LazyColumn(modifier = Modifier.fillMaxSize().padding(padding)) {
                items(uiState.conversations, key = { it.conversationId }) { conv ->
                    ConversationItem(
                        conversation = conv,
                        myEmployeeId = uiState.myEmployeeId,
                        onClick = { onConversationClick(conv) }
                    )
                }
            }
        }
    }

    if (showContactPicker) {
        ContactPickerDialog(
            contacts = uiState.contacts,
            onContactClick = { contact ->
                chatViewModel.createConversation(contact.employeeId)
                showContactPicker = false
            },
            onDismiss = { showContactPicker = false }
        )
    }
}

@Composable
private fun ConversationItem(
    conversation: ConversationInfo,
    myEmployeeId: Int,
    onClick: () -> Unit
) {
    val otherParticipants = conversation.participants.filter { it.employeeId != myEmployeeId }
    val displayName = if (conversation.title != null) conversation.title
        else otherParticipants.joinToString(", ") { it.fullName }

    val lastMsg = conversation.lastMessage
    val timeText = lastMsg?.sentAt?.let { formatTime(it) } ?: ""

    Surface(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
    ) {
        Row(
            modifier = Modifier
                .padding(horizontal = 16.dp, vertical = 12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Surface(
                modifier = Modifier.size(48.dp),
                shape = CircleShape,
                color = MaterialTheme.colorScheme.primaryContainer
            ) {
                Box(contentAlignment = Alignment.Center) {
                    Text(
                        text = displayName.take(2).uppercase(),
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.onPrimaryContainer
                    )
                }
            }
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        text = displayName,
                        fontWeight = FontWeight.SemiBold,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                        modifier = Modifier.weight(1f)
                    )
                    Text(
                        text = timeText,
                        fontSize = 12.sp,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                Spacer(modifier = Modifier.height(4.dp))
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        text = lastMsg?.let {
                            if (it.senderId == myEmployeeId) "Bạn: " else ""
                        } + (lastMsg?.content ?: "Chưa có tin nhắn"),
                        fontSize = 14.sp,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                        modifier = Modifier.weight(1f)
                    )
                    if (conversation.unreadCount > 0) {
                        Spacer(modifier = Modifier.width(8.dp))
                        Badge(containerColor = MaterialTheme.colorScheme.primary) {
                            Text(
                                text = if (conversation.unreadCount > 99) "99+" else conversation.unreadCount.toString(),
                                fontSize = 11.sp,
                                color = MaterialTheme.colorScheme.onPrimary
                            )
                        }
                    }
                }
            }
        }
    }
    HorizontalDivider(modifier = Modifier.padding(start = 72.dp))
}

@Composable
private fun ContactPickerDialog(
    contacts: List<ContactInfo>,
    onContactClick: (ContactInfo) -> Unit,
    onDismiss: () -> Unit
) {
    var searchQuery by remember { mutableStateOf("") }
    val filtered = remember(contacts, searchQuery) {
        if (searchQuery.isBlank()) contacts
        else contacts.filter { it.fullName.contains(searchQuery, ignoreCase = true) }
    }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Chọn người để nhắn tin") },
        text = {
            Column {
                OutlinedTextField(
                    value = searchQuery,
                    onValueChange = { searchQuery = it },
                    placeholder = { Text("Tìm kiếm...") },
                    leadingIcon = { Icon(Icons.Default.Search, contentDescription = null) },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                Spacer(modifier = Modifier.height(8.dp))
                LazyColumn(modifier = Modifier.heightIn(max = 400.dp)) {
                    items(filtered, key = { it.employeeId }) { contact ->
                        Surface(
                            modifier = Modifier
                                .fillMaxWidth()
                                .clickable { onContactClick(contact) }
                        ) {
                            Row(
                                modifier = Modifier.padding(vertical = 8.dp, horizontal = 4.dp),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Surface(
                                    modifier = Modifier.size(40.dp),
                                    shape = CircleShape,
                                    color = MaterialTheme.colorScheme.secondaryContainer
                                ) {
                                    Box(contentAlignment = Alignment.Center) {
                                        Text(
                                            text = contact.fullName.take(1).uppercase(),
                                            fontWeight = FontWeight.Bold
                                        )
                                    }
                                }
                                Spacer(modifier = Modifier.width(12.dp))
                                Column {
                                    Text(contact.fullName, fontWeight = FontWeight.Medium)
                                    if (contact.positionName != null) {
                                        Text(
                                            text = contact.positionName,
                                            fontSize = 12.sp,
                                            color = MaterialTheme.colorScheme.onSurfaceVariant
                                        )
                                    }
                                }
                            }
                        }
                    }
                    if (filtered.isEmpty()) {
                        item {
                            Text(
                                "Không tìm thấy",
                                modifier = Modifier.padding(8.dp),
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    }
                }
            }
        },
        confirmButton = {},
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("Đóng") }
        }
    )
}

private fun formatTime(isoString: String): String {
    return try {
        val instant = Instant.parse(isoString)
        val local = instant.atZone(ZoneId.systemDefault()).toLocalDateTime()
        val now = java.time.LocalDateTime.now()
        val formatter = if (local.toLocalDate() == now.toLocalDate())
            DateTimeFormatter.ofPattern("HH:mm", Locale.getDefault())
        else if (local.year == now.year)
            DateTimeFormatter.ofPattern("dd/MM", Locale.getDefault())
        else
            DateTimeFormatter.ofPattern("dd/MM/yyyy", Locale.getDefault())
        local.format(formatter)
    } catch (_: Exception) { "" }
}
