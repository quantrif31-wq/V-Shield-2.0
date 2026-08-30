package com.vshield.mobile.ui.screen

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Badge
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vshield.mobile.data.model.ContactInfo
import com.vshield.mobile.data.model.ConversationInfo
import com.vshield.mobile.viewmodel.ChatViewModel
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale
import kotlin.math.abs

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
                title = { Text("Tin nhắn & Trò chuyện") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Quay lại")
                    }
                }
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { showContactPicker = true }) {
                Icon(Icons.Default.Add, contentDescription = "Tạo cuộc trò chuyện mới")
            }
        }
    ) { padding ->
        if (uiState.isLoadingConversations && uiState.conversations.isEmpty()) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator()
            }
        } else if (uiState.conversations.isEmpty()) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding),
                contentAlignment = Alignment.Center
            ) {
                Text("Chưa có cuộc trò chuyện nào", style = MaterialTheme.typography.bodyLarge)
            }
        } else {
            LazyColumn(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
            ) {
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
            modifier = Modifier.padding(horizontal = 16.dp, vertical = 12.dp),
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
    var filterDept by remember { mutableStateOf("") }

    val departments = remember(contacts) {
        contacts.map { it.departmentName }.filterNotNull().distinct().sorted()
    }

    val filtered = remember(contacts, searchQuery, filterDept) {
        var result = contacts
        if (searchQuery.isNotBlank()) {
            val q = searchQuery.lowercase()
            result = result.filter {
                it.fullName.lowercase().contains(q) ||
                    (it.departmentName?.lowercase()?.contains(q) == true) ||
                    (it.positionName?.lowercase()?.contains(q) == true) ||
                    (it.email?.lowercase()?.contains(q) == true)
            }
        }
        if (filterDept.isNotBlank()) {
            result = result.filter { it.departmentName == filterDept }
        }
        result
    }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Chọn người để nhắn tin") },
        text = {
            Column {
                OutlinedTextField(
                    value = searchQuery,
                    onValueChange = { searchQuery = it },
                    placeholder = { Text("Tên, phòng ban, chức vụ...") },
                    leadingIcon = { Icon(Icons.Default.Search, contentDescription = null) },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                Spacer(modifier = Modifier.height(6.dp))
                if (departments.isNotEmpty()) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(4.dp)
                    ) {
                        FilterChip(
                            selected = filterDept == "",
                            onClick = { filterDept = "" },
                            label = { Text("Tất cả", fontSize = 11.sp) }
                        )
                        departments.take(5).forEach { dept ->
                            FilterChip(
                                selected = filterDept == dept,
                                onClick = { filterDept = if (filterDept == dept) "" else dept },
                                label = { Text(dept, fontSize = 11.sp) }
                            )
                        }
                    }
                    Spacer(modifier = Modifier.height(8.dp))
                }
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
                                    color = avatarColor(contact.fullName)
                                ) {
                                    Box(contentAlignment = Alignment.Center) {
                                        Text(
                                            text = contact.fullName.take(1).uppercase(),
                                            fontWeight = FontWeight.Bold,
                                            color = Color.White
                                        )
                                    }
                                }
                                Spacer(modifier = Modifier.width(12.dp))
                                Column(modifier = Modifier.weight(1f)) {
                                    Text(contact.fullName, fontWeight = FontWeight.Medium, fontSize = 14.sp)
                                    Row(
                                        horizontalArrangement = Arrangement.spacedBy(6.dp),
                                        verticalAlignment = Alignment.CenterVertically
                                    ) {
                                        if (contact.positionName != null) {
                                            Surface(
                                                shape = RoundedCornerShape(4.dp),
                                                color = MaterialTheme.colorScheme.primaryContainer
                                            ) {
                                                Text(
                                                    text = contact.positionName,
                                                    fontSize = 10.sp,
                                                    modifier = Modifier.padding(horizontal = 5.dp, vertical = 1.dp),
                                                    color = MaterialTheme.colorScheme.onPrimaryContainer
                                                )
                                            }
                                        }
                                        if (contact.departmentName != null) {
                                            Text(
                                                text = contact.departmentName,
                                                fontSize = 11.sp,
                                                color = MaterialTheme.colorScheme.onSurfaceVariant
                                            )
                                        }
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

private val avatarColors = listOf(
    0xFF1976D2, 0xFF388E3C, 0xFFD32F2F, 0xFFF57C00, 0xFF7B1FA2,
    0xFF00796B, 0xFF5C6BC0, 0xFFE64A19, 0xFFC2185B, 0xFF303F9F
)

private fun avatarColor(name: String): Color {
    var hash = 0
    for (c in name) hash = c.code + (hash shl 5) - hash
    return Color(avatarColors[abs(hash) % avatarColors.size])
}

private fun formatTime(isoString: String): String {
    return try {
        val instant = Instant.parse(isoString)
        val local = instant.atZone(ZoneId.systemDefault()).toLocalDateTime()
        val now = java.time.LocalDateTime.now()
        val formatter = if (local.toLocalDate() == now.toLocalDate()) {
            DateTimeFormatter.ofPattern("HH:mm", Locale.getDefault())
        } else if (local.year == now.year) {
            DateTimeFormatter.ofPattern("dd/MM", Locale.getDefault())
        } else {
            DateTimeFormatter.ofPattern("dd/MM/yyyy", Locale.getDefault())
        }
        local.format(formatter)
    } catch (_: Exception) {
        ""
    }
}
