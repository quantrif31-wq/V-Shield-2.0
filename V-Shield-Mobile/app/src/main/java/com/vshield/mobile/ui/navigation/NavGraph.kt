package com.vshield.mobile.ui.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.navArgument
import com.vshield.mobile.ui.screen.*
import com.vshield.mobile.viewmodel.ChatViewModel
import com.vshield.mobile.viewmodel.NotificationViewModel

sealed class Screen(val route: String) {
    data object Login : Screen("login")
    data object Home : Screen("home")
    data object Transfer : Screen("transfer")
    data object Chat : Screen("chat")
    data object Conversation : Screen("conversation/{conversationId}") {
        fun createRoute(conversationId: Int) = "conversation/$conversationId"
    }
    data object Leave : Screen("leave")
    data object Profile : Screen("profile")
    data object Notifications : Screen("notifications")
    data object AlarmMap : Screen("alarmMap/{latitude}/{longitude}?label={label}") {
        fun createRoute(lat: Double, lng: Double, label: String? = null) =
            "alarmMap/$lat/$lng" + (label?.let { "?label=$it" } ?: "")
    }
}

@Composable
fun NavGraph(
    navController: NavHostController,
    chatViewModel: ChatViewModel,
    notificationViewModel: NotificationViewModel,
    startDestination: String,
    onSessionExpired: () -> Unit,
    onStartCall: (Int, String) -> Unit
) {
    NavHost(
        navController = navController,
        startDestination = startDestination
    ) {
        composable(Screen.Login.route) {
            LoginScreen(
                onLoginSuccess = {
                    navController.navigate(Screen.Home.route) {
                        popUpTo(Screen.Login.route) { inclusive = true }
                    }
                }
            )
        }
        composable(Screen.Home.route) {
            HomeScreen(onSessionExpired = onSessionExpired)
        }
        composable(Screen.Transfer.route) {
            TransferScreen(onSessionExpired = onSessionExpired)
        }
        composable(Screen.Chat.route) {
            ChatListScreen(
                chatViewModel = chatViewModel,
                onConversationClick = { conv ->
                    navController.navigate(Screen.Conversation.createRoute(conv.conversationId))
                },
                onBack = { navController.popBackStack() }
            )
        }
        composable(
            route = Screen.Conversation.route,
            arguments = listOf(navArgument("conversationId") { type = NavType.IntType })
        ) { backStackEntry ->
            val conversationId = backStackEntry.arguments?.getInt("conversationId") ?: return@composable
            ConversationScreen(
                conversationId = conversationId,
                chatViewModel = chatViewModel,
                onBack = { navController.popBackStack() },
                onStartCall = { targetId, targetName ->
                    onStartCall(targetId, targetName)
                }
            )
        }
        composable(Screen.Leave.route) {
            LeaveScreen(onSessionExpired = onSessionExpired)
        }
        composable(Screen.Profile.route) {
            ProfileScreen(onSessionExpired = onSessionExpired)
        }
        composable(Screen.Notifications.route) {
            NotificationScreen(
                notificationViewModel = notificationViewModel,
                onSessionExpired = onSessionExpired,
                onViewMap = { lat, lng, label ->
                    navController.navigate(Screen.AlarmMap.createRoute(lat, lng, label))
                }
            )
        }
        composable(
            route = Screen.AlarmMap.route,
            arguments = listOf(
                navArgument("latitude") { type = NavType.StringType },
                navArgument("longitude") { type = NavType.StringType },
                navArgument("label") { type = NavType.StringType; nullable = true; defaultValue = null }
            )
        ) { backStackEntry ->
            val lat = backStackEntry.arguments?.getString("latitude")?.toDoubleOrNull() ?: 0.0
            val lng = backStackEntry.arguments?.getString("longitude")?.toDoubleOrNull() ?: 0.0
            val label = backStackEntry.arguments?.getString("label")
            AlarmMapScreen(
                latitude = lat,
                longitude = lng,
                locationLabel = label,
                onBack = { navController.popBackStack() }
            )
        }
    }
}
