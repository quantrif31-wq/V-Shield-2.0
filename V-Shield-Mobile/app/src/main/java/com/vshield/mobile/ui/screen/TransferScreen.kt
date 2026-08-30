package com.vshield.mobile.ui.screen

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.DirectionsCar
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.vshield.mobile.data.model.DelegationInfo
import com.vshield.mobile.ui.component.ErrorDialog
import com.vshield.mobile.ui.component.LoadingIndicator
import com.vshield.mobile.ui.theme.*
import com.vshield.mobile.viewmodel.TransferViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
@Suppress("UNUSED_PARAMETER")
fun TransferScreen(
    onSessionExpired: () -> Unit,
    transferViewModel: TransferViewModel = viewModel()
) {
    val uiState by transferViewModel.uiState.collectAsState()

    var showCreateDialog by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        transferViewModel.loadData()
    }

    if (uiState.error != null) {
        ErrorDialog(
            title = "Lỗi",
            message = uiState.error!!,
            onDismiss = { transferViewModel.clearMessages() }
        )
    }

    if (uiState.successMessage != null) {
        AlertDialog(
            onDismissRequest = { transferViewModel.clearMessages() },
            title = { Text("Thành công") },
            text = { Text(uiState.successMessage!!) },
            confirmButton = {
                TextButton(onClick = { transferViewModel.clearMessages() }) {
                    Text("Đóng")
                }
            }
        )
    }

    if (showCreateDialog) {
        CreateDelegationDialog(
            vehicles = uiState.vehicles,
            onDismiss = { showCreateDialog = false },
            onLookup = { transferViewModel.lookupEmployee(it) },
            employeeLookup = uiState.employeeLookup,
            onSubmit = { vehicleId, employeeId, reason ->
                transferViewModel.createDelegation(vehicleId, employeeId, reason)
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
                    text = "Ủy quyền & Bàn giao xe",
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
                    Text("Tạo yêu cầu")
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
                item {
                    Text(
                        text = "Yêu cầu đến",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        modifier = Modifier.padding(bottom = 8.dp)
                    )
                }

                if (uiState.incomingDelegations.isEmpty()) {
                    item {
                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            shape = RoundedCornerShape(12.dp)
                        ) {
                            Text(
                                text = "Chưa có yêu cầu ủy quyền nào",
                                modifier = Modifier.padding(16.dp),
                                style = MaterialTheme.typography.bodyMedium,
                                color = Gray600
                            )
                        }
                    }
                }

                items(uiState.incomingDelegations) { delegation ->
                    DelegationCard(
                        delegation = delegation,
                        isIncoming = true,
                        onApprove = { transferViewModel.approveDelegation(delegation.delegationId) },
                        onReject = { transferViewModel.rejectDelegation(delegation.delegationId) }
                    )
                }

                item {
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = "Yêu cầu đi",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        modifier = Modifier.padding(bottom = 8.dp)
                    )
                }

                if (uiState.outgoingDelegations.isEmpty()) {
                    item {
                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            shape = RoundedCornerShape(12.dp)
                        ) {
                            Text(
                                text = "Chưa có yêu cầu ủy quyền nào",
                                modifier = Modifier.padding(16.dp),
                                style = MaterialTheme.typography.bodyMedium,
                                color = Gray600
                            )
                        }
                    }
                }

                items(uiState.outgoingDelegations) { delegation ->
                    DelegationCard(
                        delegation = delegation,
                        isIncoming = false,
                        onApprove = null,
                        onReject = null
                    )
                }
            }
        }
    }
}

@Composable
private fun DelegationCard(
    delegation: DelegationInfo,
    isIncoming: Boolean,
    onApprove: (() -> Unit)?,
    onReject: (() -> Unit)?
) {
    val statusColor = when (delegation.status) {
        "Pending" -> Orange600
        "Approved" -> Green600
        "Rejected" -> Red600
        "Revoked" -> Gray600
        else -> Gray600
    }

    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(12.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    Icons.Filled.DirectionsCar,
                    contentDescription = null,
                    tint = Blue600,
                    modifier = Modifier.size(20.dp)
                )
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = delegation.licensePlate ?: "Xe #${delegation.vehicleId}",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.SemiBold
                )
            }

            Spacer(modifier = Modifier.height(8.dp))

            Text(
                text = if (isIncoming) "Từ: ${delegation.fromEmployeeName ?: "NV#${delegation.fromEmployeeId}"}"
                else "Đến: ${delegation.toEmployeeName ?: "NV#${delegation.toEmployeeId}"}",
                style = MaterialTheme.typography.bodyMedium
            )

            delegation.reason?.let {
                Text(
                    text = "Lý do: $it",
                    style = MaterialTheme.typography.bodySmall,
                    color = Gray600
                )
            }

            Spacer(modifier = Modifier.height(8.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Surface(
                    shape = RoundedCornerShape(8.dp),
                    color = statusColor.copy(alpha = 0.1f)
                ) {
                    Text(
                        text = when (delegation.status) {
                            "Pending" -> "Chờ duyệt"
                            "Approved" -> "Đã duyệt"
                            "Rejected" -> "Từ chối"
                            "Revoked" -> "Đã thu hồi"
                            else -> delegation.status
                        },
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 4.dp),
                        style = MaterialTheme.typography.labelSmall,
                        color = statusColor
                    )
                }

                if (isIncoming && delegation.status == "Pending" && onApprove != null && onReject != null) {
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        IconButton(onClick = onApprove) {
                            Icon(
                                Icons.Filled.Check,
                                contentDescription = "Duyệt",
                                tint = Green600
                            )
                        }
                        IconButton(onClick = onReject) {
                            Icon(
                                Icons.Filled.Close,
                                contentDescription = "Từ chối",
                                tint = Red600
                            )
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CreateDelegationDialog(
    vehicles: List<com.vshield.mobile.data.model.VehicleInfo>,
    onDismiss: () -> Unit,
    onLookup: (String) -> Unit,
    employeeLookup: List<com.vshield.mobile.data.model.EmployeeInfo>,
    onSubmit: (vehicleId: Int, toEmployeeId: Int, reason: String) -> Unit
) {
    var selectedVehicleId by remember { mutableIntStateOf(0) }
    var searchQuery by remember { mutableStateOf("") }
    var selectedEmployeeId by remember { mutableIntStateOf(0) }
    var selectedEmployeeName by remember { mutableStateOf("") }
    var reason by remember { mutableStateOf("") }
    var showVehiclePicker by remember { mutableStateOf(false) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Ủy quyền xe") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                if (showVehiclePicker) {
                    LazyColumn(modifier = Modifier.height(200.dp)) {
                        items(vehicles) { vehicle ->
                            TextButton(
                                onClick = {
                                    selectedVehicleId = vehicle.vehicleId
                                    showVehiclePicker = false
                                },
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Text("${vehicle.licensePlate} (${vehicle.vehicleTypeName ?: "Không rõ"})")
                            }
                        }
                    }
                } else {
                    OutlinedButton(
                        onClick = { showVehiclePicker = true },
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(12.dp)
                    ) {
                        Text(
                            if (selectedVehicleId > 0) {
                                vehicles.find { it.vehicleId == selectedVehicleId }?.licensePlate
                                    ?: "Xe #$selectedVehicleId"
                            } else "Chọn xe"
                        )
                    }
                }

                OutlinedTextField(
                    value = searchQuery,
                    onValueChange = {
                        searchQuery = it
                        if (it.length >= 2) onLookup(it)
                    },
                    label = { Text("Tìm nhân viên") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    shape = RoundedCornerShape(12.dp)
                )

                if (selectedEmployeeId > 0) {
                    Text(
                        text = "Đã chọn: $selectedEmployeeName",
                        style = MaterialTheme.typography.bodySmall,
                        color = Green600
                    )
                }

                if (employeeLookup.isNotEmpty() && selectedEmployeeId == 0) {
                    LazyColumn(modifier = Modifier.height(120.dp)) {
                        items(employeeLookup) { emp ->
                            TextButton(
                                onClick = {
                                    selectedEmployeeId = emp.employeeId
                                    selectedEmployeeName = emp.fullName
                                },
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Text("${emp.fullName} - ${emp.departmentName ?: ""}")
                            }
                        }
                    }
                }

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
                    if (selectedVehicleId > 0 && selectedEmployeeId > 0) {
                        onSubmit(selectedVehicleId, selectedEmployeeId, reason)
                    }
                },
                enabled = selectedVehicleId > 0 && selectedEmployeeId > 0
            ) {
                Text("Gửi yêu cầu")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("Hủy bỏ")
            }
        }
    )
}
