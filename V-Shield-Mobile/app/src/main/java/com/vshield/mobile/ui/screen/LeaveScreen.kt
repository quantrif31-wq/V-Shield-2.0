package com.vshield.mobile.ui.screen

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.vshield.mobile.ui.component.ErrorDialog
import com.vshield.mobile.ui.component.LoadingIndicator
import com.vshield.mobile.ui.theme.*
import com.vshield.mobile.viewmodel.LeaveViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
@Suppress("UNUSED_PARAMETER")
fun LeaveScreen(
    onSessionExpired: () -> Unit,
    leaveViewModel: LeaveViewModel = viewModel()
) {
    val uiState by leaveViewModel.uiState.collectAsState()
    var showCreateDialog by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        leaveViewModel.loadData()
    }

    if (uiState.error != null) {
        ErrorDialog(
            title = "Lỗi",
            message = uiState.error!!,
            onDismiss = { leaveViewModel.clearMessages() }
        )
    }

    if (uiState.successMessage != null) {
        AlertDialog(
            onDismissRequest = { leaveViewModel.clearMessages() },
            title = { Text("Thành công") },
            text = { Text(uiState.successMessage!!) },
            confirmButton = {
                TextButton(onClick = { leaveViewModel.clearMessages() }) {
                    Text("Đóng")
                }
            }
        )
    }

    if (showCreateDialog) {
        CreateLeaveDialog(
            leaveTypes = uiState.leaveTypes,
            onDismiss = { showCreateDialog = false },
            onSubmit = { typeId, start, end, reason ->
                leaveViewModel.createLeaveRequest(typeId, start, end, reason)
                showCreateDialog = false
            }
        )
    }

    Column(modifier = Modifier.fillMaxSize()) {
        Surface(
            modifier = Modifier.fillMaxWidth(),
            color = Blue700
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 16.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "Nghỉ phép",
                    style = MaterialTheme.typography.titleLarge,
                    color = MaterialTheme.colorScheme.onPrimary,
                    fontWeight = FontWeight.Bold
                )
                FilledTonalButton(
                    onClick = { showCreateDialog = true },
                    shape = RoundedCornerShape(12.dp)
                ) {
                    Icon(Icons.Filled.Add, contentDescription = null, modifier = Modifier.size(18.dp))
                    Spacer(modifier = Modifier.width(4.dp))
                    Text("Tạo đơn")
                }
            }
        }

        if (uiState.isLoading) {
            LoadingIndicator()
        } else {
            LazyColumn(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                if (uiState.leaveRequests.isEmpty()) {
                    item {
                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            shape = RoundedCornerShape(12.dp)
                        ) {
                            Column(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(32.dp),
                                horizontalAlignment = Alignment.CenterHorizontally
                            ) {
                                Icon(
                                    Icons.Filled.CalendarMonth,
                                    contentDescription = null,
                                    modifier = Modifier.size(48.dp),
                                    tint = Gray400
                                )
                                Spacer(modifier = Modifier.height(12.dp))
                                Text(
                                    text = "Chưa có đơn nghỉ phép nào",
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = Gray600
                                )
                            }
                        }
                    }
                }

                items(uiState.leaveRequests) { request ->
                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(12.dp)
                    ) {
                        Column(modifier = Modifier.padding(16.dp)) {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = formatLeaveType(request.leaveType),
                                    style = MaterialTheme.typography.titleMedium,
                                    fontWeight = FontWeight.SemiBold
                                )
                                val statusColor = when (request.status) {
                                    "Pending" -> Orange600
                                    "Approved" -> Green600
                                    "Rejected" -> Red600
                                    else -> Gray600
                                }
                                Surface(
                                    shape = RoundedCornerShape(8.dp),
                                    color = statusColor.copy(alpha = 0.1f)
                                ) {
                                    Text(
                                        text = when (request.status) {
                                            "Pending" -> "Chờ duyệt"
                                            "Approved" -> "Đã duyệt"
                                            "Rejected" -> "Từ chối"
                                            else -> request.status
                                        },
                                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 4.dp),
                                        style = MaterialTheme.typography.labelSmall,
                                        color = statusColor
                                    )
                                }
                            }

                            Spacer(modifier = Modifier.height(8.dp))

                            Text(
                                text = "${request.startDate} → ${request.endDate}",
                                style = MaterialTheme.typography.bodyMedium
                            )

                            request.reason?.let {
                                Text(
                                    text = "Lý do: $it",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = Gray600
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

private fun formatLeaveType(type: String?): String {
    return when (type?.lowercase()?.trim()) {
        "annualleave", "annual_leave", "annual" -> "Nghỉ phép năm"
        "sickleave", "sick_leave", "sick" -> "Nghỉ ốm / Bệnh"
        "unpaidleave", "unpaid_leave", "unpaid" -> "Nghỉ không lương"
        "personalleave", "personal_leave", "personal" -> "Nghỉ việc riêng"
        "maternityleave", "maternity_leave", "maternity" -> "Nghỉ thai sản"
        "other" -> "Lý do khác"
        null -> "Nghỉ phép"
        else -> type
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CreateLeaveDialog(
    leaveTypes: List<com.vshield.mobile.data.model.LeaveType>,
    onDismiss: () -> Unit,
    onSubmit: (leaveTypeId: Int, startDate: String, endDate: String, reason: String) -> Unit
) {
    var selectedTypeId by remember { mutableIntStateOf(0) }
    var startDate by remember { mutableStateOf("") }
    var endDate by remember { mutableStateOf("") }
    var reason by remember { mutableStateOf("") }
    var expanded by remember { mutableStateOf(false) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Tạo đơn nghỉ phép") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                ExposedDropdownMenuBox(
                    expanded = expanded,
                    onExpandedChange = { expanded = it }
                ) {
                    OutlinedTextField(
                        value = leaveTypes.find { it.leaveTypeId == selectedTypeId }?.typeName
                            ?: "Chọn loại nghỉ",
                        onValueChange = {},
                        readOnly = true,
                        label = { Text("Loại nghỉ") },
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
                        modifier = Modifier
                            .fillMaxWidth()
                            .menuAnchor(MenuAnchorType.PrimaryNotEditable, true),
                        shape = RoundedCornerShape(12.dp)
                    )
                    ExposedDropdownMenu(
                        expanded = expanded,
                        onDismissRequest = { expanded = false }
                    ) {
                        leaveTypes.forEach { type ->
                            DropdownMenuItem(
                                text = { Text(type.typeName) },
                                onClick = {
                                    selectedTypeId = type.leaveTypeId
                                    expanded = false
                                }
                            )
                        }
                    }
                }

                OutlinedTextField(
                    value = startDate,
                    onValueChange = { startDate = it },
                    label = { Text("Ngày bắt đầu (yyyy-MM-dd)") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    shape = RoundedCornerShape(12.dp)
                )

                OutlinedTextField(
                    value = endDate,
                    onValueChange = { endDate = it },
                    label = { Text("Ngày kết thúc (yyyy-MM-dd)") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    shape = RoundedCornerShape(12.dp)
                )

                OutlinedTextField(
                    value = reason,
                    onValueChange = { reason = it },
                    label = { Text("Lý do") },
                    modifier = Modifier.fillMaxWidth(),
                    maxLines = 3,
                    shape = RoundedCornerShape(12.dp)
                )
            }
        },
        confirmButton = {
            TextButton(
                onClick = {
                    if (selectedTypeId > 0 && startDate.isNotBlank() && endDate.isNotBlank()) {
                        onSubmit(selectedTypeId, startDate, endDate, reason)
                    }
                },
                enabled = selectedTypeId > 0 && startDate.isNotBlank() && endDate.isNotBlank()
            ) {
                Text("Gửi đơn")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("Hủy bỏ")
            }
        }
    )
}
