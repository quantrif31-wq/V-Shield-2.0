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
import com.vshield.mobile.service.NotificationHelper
import com.vshield.mobile.service.VShieldBackgroundService

class MainActivity : FragmentActivity() {

    private var pendingIntentAction: ((ChatViewModel, androidx.navigation.NavHostController) -> Unit)? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        handleIntent(intent)
        setContent {
            VShieldTheme {
                VShieldMainScreen(
                    pendingAction = pendingIntentAction,
                    onClearPendingAction = { pendingIntentAction = null }
                )
            }
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        handleIntent(intent)
    }

    private fun handleIntent(intent: Intent?) {
        if (intent == null) return
        val callAction = intent.getStringExtra(NotificationHelper.EXTRA_CALL_ACTION)
        val navigateTo = intent.getStringExtra(NotificationHelper.EXTRA_NAVIGATE_TO)
        val convId = intent.getIntExtra(NotificationHelper.EXTRA_CONVERSATION_ID, 0)

        android.util.Log.i("MainActivity", "handleIntent: callAction=$callAction, navigateTo=$navigateTo, convId=$convId")

        val fromEmpId = intent.getIntExtra("EXTRA_FROM_EMPLOYEE_ID", 0)
        val fromName = intent.getStringExtra("EXTRA_FROM_FULL_NAME") ?: "Cuộc gọi đến"
        val callType = intent.getStringExtra("EXTRA_CALL_TYPE") ?: "audio"

        if (callAction == NotificationHelper.ACTION_ACCEPT_CALL) {
            NotificationHelper.cancelIncomingCallNotification(this)
        }

        pendingIntentAction = { chatVm, nav ->
            if (callAction == NotificationHelper.ACTION_ACCEPT_CALL) {
                android.util.Log.i("MainActivity", "pendingIntentAction: accepting call from $fromEmpId ($fromName)...")
                chatVm.acceptCall(
                    fromEmployeeId = fromEmpId,
                    fromFullName = fromName,
                    conversationId = convId,
                    callType = callType
                )
            } else if (callAction == NotificationHelper.ACTION_REJECT_CALL) {
                chatVm.rejectCall()
            } else if (navigateTo == "call") {
                val bgCall = com.vshield.mobile.service.VShieldBackgroundService.lastIncomingCall
                if (bgCall != null && chatVm.uiState.value.callState is ChatCallState.Idle) {
                    val isVideo = (bgCall.signalingData ?: "").contains("m=video")
                    val cType = if (isVideo) "video" else "audio"
                    chatVm.restoreIncomingCallState(
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
                    nav.navigate(Screen.Conversation.createRoute(convId)) {
                        popUpTo(Screen.Home.route)
                        launchSingleTop = true
                    }
                } else {
                    nav.navigate(Screen.Chat.route) {
                        popUpTo(Screen.Home.route)
                        launchSingleTop = true
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalComposeUiApi::class)
@Composable
fun VShieldMainScreen(
    pendingAction: ((ChatViewModel, androidx.navigation.NavHostController) -> Unit)? = null,
    onClearPendingAction: () -> Unit = {}
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

    DisposableEffect(lifecycleOwner) {
        val observer = LifecycleEventObserver { _, event ->
            if (event == Lifecycle.Event.ON_RESUME) {
                VShieldBackgroundService.isAppInForeground = true
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

    val context = androidx.compose.ui.platform.LocalContext.current

    // Request permissions (Notifications, Audio, Camera) immediately on app launch
    val multiplePermissionsLauncher = androidx.activity.compose.rememberLauncherForActivityResult(
        contract = androidx.activity.result.contract.ActivityResultContracts.RequestMultiplePermissions()
    ) {}

    LaunchedEffect(Unit) {
        val permissions = mutableListOf<String>()
        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.TIRAMISU) {
            permissions.add(android.Manifest.permission.POST_NOTIFICATIONS)
        }
        permissions.add(android.Manifest.permission.RECORD_AUDIO)
        permissions.add(android.Manifest.permission.CAMERA)
        multiplePermissionsLauncher.launch(permissions.toTypedArray())
    }

    LaunchedEffect(isLoggedIn, pendingAction) {
        if (isLoggedIn) {
            notificationViewModel.initialize()
            chatViewModel.initialize()
            authViewModel.recordUserActivity()

            if (pendingAction != null) {
                pendingAction(chatViewModel, navController)
                onClearPendingAction()
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
        }
    }
}
