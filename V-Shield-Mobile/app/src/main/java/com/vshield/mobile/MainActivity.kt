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
import com.vshield.mobile.ui.screen.AlarmOverlay
import com.vshield.mobile.ui.screen.CallOverlay
import com.vshield.mobile.ui.theme.VShieldTheme
import com.vshield.mobile.viewmodel.AuthViewModel
import com.vshield.mobile.viewmodel.ChatViewModel
import com.vshield.mobile.viewmodel.NotificationViewModel

class MainActivity : FragmentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            VShieldTheme {
                VShieldMainScreen()
            }
        }
    }
}

@OptIn(ExperimentalComposeUiApi::class)
@Composable
fun VShieldMainScreen() {
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

    val isLoggedIn = authState.isLoggedIn
    val shouldKeepSessionDuringSystemAuth =
        authState.isBiometricPromptActive || authState.awaitingBiometricEnrollment
    val mainRoutes = listOf(
        Screen.Home.route,
        Screen.Transfer.route,
        Screen.Chat.route,
        Screen.Notifications.route,
        Screen.Leave.route,
        Screen.Profile.route
    )

    LaunchedEffect(isLoggedIn) {
        if (isLoggedIn) {
            notificationViewModel.initialize()
            authViewModel.recordUserActivity()
        } else if (currentRoute != Screen.Login.route) {
            navController.navigate(Screen.Login.route) {
                popUpTo(0) { inclusive = true }
                launchSingleTop = true
            }
        }
    }

    DisposableEffect(lifecycleOwner, isLoggedIn, shouldKeepSessionDuringSystemAuth) {
        val observer = LifecycleEventObserver { _, event ->
            if (isLoggedIn &&
                event == Lifecycle.Event.ON_STOP &&
                !shouldKeepSessionDuringSystemAuth
            ) {
                authViewModel.lockSessionForInactivity()
            }
        }

        lifecycleOwner.lifecycle.addObserver(observer)
        onDispose { lifecycleOwner.lifecycle.removeObserver(observer) }
    }

    val showBottomBar = isLoggedIn && currentRoute in mainRoutes
    val isInCall = chatState.callState !is ChatCallState.Idle
    val showCallOverlay = isInCall
    val activeAlarm = notifState.activeAlarm

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
                onStartCall = { targetId, targetName ->
                    val conv = chatState.currentConversation
                    chatViewModel.startCall(targetId, targetName, conv?.conversationId)
                }
            )

            if (activeAlarm != null) {
                AlarmOverlay(
                    alarm = activeAlarm,
                    onAcknowledge = { notificationViewModel.acknowledgeAlarm() },
                    onViewMap = if (activeAlarm.latitude != null && activeAlarm.longitude != null) {
                        {
                            navController.navigate(
                                Screen.AlarmMap.createRoute(
                                    activeAlarm.latitude,
                                    activeAlarm.longitude,
                                    activeAlarm.locationLabel
                                )
                            )
                        }
                    } else {
                        null
                    }
                )
            } else if (showCallOverlay) {
                CallOverlay(chatViewModel = chatViewModel)
            }
        }
    }
}
