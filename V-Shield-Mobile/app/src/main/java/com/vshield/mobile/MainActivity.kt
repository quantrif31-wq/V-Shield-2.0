package com.vshield.mobile

import android.os.Bundle
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.ui.ExperimentalComposeUiApi
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.input.pointer.pointerInteropFilter
import androidx.fragment.app.FragmentActivity
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import com.vshield.mobile.data.model.ChatCallState
import com.vshield.mobile.ui.navigation.BottomNavBar
import com.vshield.mobile.ui.navigation.NavGraph
import com.vshield.mobile.ui.navigation.Screen
import com.vshield.mobile.ui.screen.CallOverlay
import com.vshield.mobile.ui.theme.VShieldTheme
import com.vshield.mobile.viewmodel.AuthViewModel
import com.vshield.mobile.viewmodel.ChatViewModel
import com.vshield.mobile.viewmodel.NotificationViewModel

import android.content.Intent
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import kotlinx.coroutines.flow.collectLatest
import com.vshield.mobile.security.PermissionManager
import com.vshield.mobile.security.PermissionStatus
import com.vshield.mobile.service.AutoStartHelper
import com.vshield.mobile.service.NotificationHelper
import com.vshield.mobile.service.VShieldBackgroundService
import com.vshield.mobile.ui.component.PermissionSetupDialog

class MainActivity : FragmentActivity() {

    private val intentActionFlow = kotlinx.coroutines.flow.MutableSharedFlow<Intent>(
        replay = 1,
        extraBufferCapacity = 1,
        onBufferOverflow = kotlinx.coroutines.channels.BufferOverflow.DROP_OLDEST
    )

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        intent?.let { intentActionFlow.tryEmit(it) }
        setContent {
            VShieldTheme {
                VShieldMainScreen(intentFlow = intentActionFlow)
            }
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        intentActionFlow.tryEmit(intent)
    }
}

@OptIn(ExperimentalComposeUiApi::class)
@Composable
fun VShieldMainScreen(
    intentFlow: kotlinx.coroutines.flow.Flow<Intent>? = null
) {
    val navController = rememberNavController()
    val authViewModel: AuthViewModel = viewModel()
    val chatViewModel: ChatViewModel = viewModel()
    val notificationViewModel: NotificationViewModel = viewModel()
    val authState by authViewModel.uiState.collectAsState()
    val chatState by chatViewModel.uiState.collectAsState()
    val notifState by notificationViewModel.uiState.collectAsState()
    val navBackStackEntry by navController.currentBackStackEntryAsState()
    val currentRoute = navBackStackEntry?.destination?.route
    val lifecycleOwner = LocalLifecycleOwner.current

    val context = androidx.compose.ui.platform.LocalContext.current
    var permissionStatus by remember { mutableStateOf(PermissionManager.checkPermissionStatus(context)) }
    var showPermissionDialog by remember { mutableStateOf(false) }

    DisposableEffect(lifecycleOwner) {
        val observer = LifecycleEventObserver { _, event ->
            if (event == Lifecycle.Event.ON_RESUME) {
                VShieldBackgroundService.isAppInForeground = true
                permissionStatus = PermissionManager.checkPermissionStatus(context)
            } else if (event == Lifecycle.Event.ON_PAUSE || event == Lifecycle.Event.ON_STOP) {
                VShieldBackgroundService.isAppInForeground = false
            }
        }
        lifecycleOwner.lifecycle.addObserver(observer)
        onDispose {
            lifecycleOwner.lifecycle.removeObserver(observer)
            VShieldBackgroundService.isAppInForeground = false
        }
    }

    val isLoggedIn = authState.isLoggedIn
    val mainRoutes = listOf(
        Screen.Home.route,
        Screen.Transfer.route,
        Screen.Chat.route,
        Screen.Notifications.route,
        Screen.Leave.route,
        Screen.Profile.route
    )

    // Request all essential permissions immediately on launch and check special permissions
    val multiplePermissionsLauncher = androidx.activity.compose.rememberLauncherForActivityResult(
        contract = androidx.activity.result.contract.ActivityResultContracts.RequestMultiplePermissions()
    ) {
        permissionStatus = PermissionManager.checkPermissionStatus(context)
        if (!permissionStatus.isAllGranted) {
            showPermissionDialog = true
        }
    }

    LaunchedEffect(Unit) {
        permissionStatus = PermissionManager.checkPermissionStatus(context)
        val requiredPermissions = PermissionManager.getRequiredRuntimePermissions()
        val missingRuntime = requiredPermissions.filter {
            androidx.core.content.ContextCompat.checkSelfPermission(context, it) != android.content.pm.PackageManager.PERMISSION_GRANTED
        }
        if (missingRuntime.isNotEmpty()) {
            multiplePermissionsLauncher.launch(requiredPermissions.toTypedArray())
        } else if (!permissionStatus.isAllGranted) {
            showPermissionDialog = true
        }
    }

    LaunchedEffect(isLoggedIn) {
        if (isLoggedIn) {
            notificationViewModel.initialize()
            chatViewModel.initialize()
            authViewModel.recordUserActivity()

            intentFlow?.collectLatest { receivedIntent ->
                val callAction = receivedIntent.getStringExtra(NotificationHelper.EXTRA_CALL_ACTION)
                val navigateTo = receivedIntent.getStringExtra(NotificationHelper.EXTRA_NAVIGATE_TO)
                val convId = receivedIntent.getIntExtra(NotificationHelper.EXTRA_CONVERSATION_ID, 0)
                val fromEmpId = receivedIntent.getIntExtra("EXTRA_FROM_EMPLOYEE_ID", 0)
                val fromName = receivedIntent.getStringExtra("EXTRA_FROM_FULL_NAME") ?: "Cuộc gọi đến"
                val callType = receivedIntent.getStringExtra("EXTRA_CALL_TYPE") ?: "audio"

                android.util.Log.i("MainActivity", "intentFlow collected: callAction=$callAction, navigateTo=$navigateTo, convId=$convId")

                if (callAction == NotificationHelper.ACTION_ACCEPT_CALL) {
                    NotificationHelper.cancelIncomingCallNotification(context)
                    chatViewModel.acceptCall(
                        fromEmployeeId = fromEmpId,
                        fromFullName = fromName,
                        conversationId = convId,
                        callType = callType
                    )
                } else if (callAction == NotificationHelper.ACTION_REJECT_CALL) {
                    chatViewModel.rejectCall()
                } else if (navigateTo == "call") {
                    val bgCall = com.vshield.mobile.service.VShieldBackgroundService.lastIncomingCall
                    if (bgCall != null && chatViewModel.uiState.value.callState is ChatCallState.Idle) {
                        val isVideo = (bgCall.signalingData ?: "").contains("m=video")
                        val cType = if (isVideo) "video" else "audio"
                        chatViewModel.restoreIncomingCallState(
                            fromEmployeeId = bgCall.fromEmployeeId,
                            fromFullName = bgCall.fromFullName ?: "Cuộc gọi đến",
                            conversationId = bgCall.conversationId,
                            offerSdp = bgCall.signalingData,
                            callType = cType
                        )
                    }
                } else if (navigateTo == NotificationHelper.ACTION_OPEN_CHAT) {
                    if (convId > 0) {
                        android.util.Log.i("MainActivity", "Navigating directly to conversation $convId")
                        navController.navigate(Screen.Conversation.createRoute(convId)) {
                            popUpTo(Screen.Home.route)
                            launchSingleTop = true
                        }
                    } else {
                        navController.navigate(Screen.Chat.route) {
                            popUpTo(Screen.Home.route)
                            launchSingleTop = true
                        }
                    }
                }
            }
        } else if (currentRoute != Screen.Login.route) {
            navController.navigate(Screen.Login.route) {
                popUpTo(0) { inclusive = true }
                launchSingleTop = true
            }
        }
    }

    val isInCall = chatState.callState !is ChatCallState.Idle
    val showBottomBar = isLoggedIn && currentRoute in mainRoutes
    val showCallOverlay = isInCall

    Scaffold(
        modifier = Modifier.fillMaxSize(),
        bottomBar = {
            if (showBottomBar && !showCallOverlay) {
                BottomNavBar(
                    currentRoute = currentRoute,
                    chatUnreadCount = chatViewModel.totalUnreadCount(),
                    notificationUnreadCount = notifState.unreadCount,
                    onItemClick = { item ->
                        if (currentRoute != item.route) {
                            navController.navigate(item.route) {
                                popUpTo(Screen.Home.route) { saveState = true }
                                launchSingleTop = true
                                restoreState = true
                            }
                        }
                    }
                )
            }
        }
    ) { innerPadding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .pointerInteropFilter {
                    if (isLoggedIn) {
                        authViewModel.recordUserActivity()
                    }
                    false
                }
        ) {
            NavGraph(
                navController = navController,
                chatViewModel = chatViewModel,
                notificationViewModel = notificationViewModel,
                startDestination = if (isLoggedIn) Screen.Home.route else Screen.Login.route,
                onSessionExpired = {
                    authViewModel.logout()
                    navController.navigate(Screen.Login.route) {
                        popUpTo(0) { inclusive = true }
                    }
                },
                onStartCall = { targetId, targetName, type ->
                    val conv = chatState.currentConversation
                    chatViewModel.startCall(targetId, targetName, conv?.conversationId, type)
                }
            )

            if (showCallOverlay) {
                CallOverlay(chatViewModel = chatViewModel)
            }

            if (showPermissionDialog) {
                PermissionSetupDialog(
                    status = permissionStatus,
                    onRefreshStatus = {
                        permissionStatus = PermissionManager.checkPermissionStatus(context)
                    },
                    onDismiss = {
                        showPermissionDialog = false
                    }
                )
            }
        }
    }
}
