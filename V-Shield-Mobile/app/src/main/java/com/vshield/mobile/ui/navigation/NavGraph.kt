package com.vshield.mobile.ui.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.navArgument
import com.vshield.mobile.ui.screen.*

sealed class Screen(val route: String) {
    data object Login : Screen("login")
    data object Home : Screen("home")
    data object Transfer : Screen("transfer")
    data object Leave : Screen("leave")
    data object Profile : Screen("profile")
}

@Composable
fun NavGraph(
    navController: NavHostController,
    startDestination: String,
    onSessionExpired: () -> Unit
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
        composable(Screen.Leave.route) {
            LeaveScreen(onSessionExpired = onSessionExpired)
        }
        composable(Screen.Profile.route) {
            ProfileScreen(onSessionExpired = onSessionExpired)
        }
    }
}
