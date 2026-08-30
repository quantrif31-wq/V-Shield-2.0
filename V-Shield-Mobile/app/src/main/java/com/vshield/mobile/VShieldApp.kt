package com.vshield.mobile

import android.app.Application
import android.util.Log
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.TokenManager
import org.osmdroid.config.Configuration

class VShieldApp : Application() {

    lateinit var tokenManager: TokenManager
        private set

    override fun onCreate() {
        super.onCreate()

        // Set up global uncaught exception handler to catch native/background thread crashes
        val defaultHandler = Thread.getDefaultUncaughtExceptionHandler()
        Thread.setDefaultUncaughtExceptionHandler { thread, throwable ->
            Log.e("VShieldApp", "UNCAUGHT EXCEPTION on thread [${thread.name}]: ${throwable.message}", throwable)
            // Let the default handler proceed (shows crash dialog / kills process)
            defaultHandler?.uncaughtException(thread, throwable)
        }

        Configuration.getInstance().apply {
            userAgentValue = packageName
            osmdroidTileCache = cacheDir.resolve("tiles")
        }
        tokenManager = TokenManager(this)
        RetrofitClient.init(tokenManager)
        restoreSession()
        com.vshield.mobile.service.NotificationHelper.createNotificationChannels(this)
        try {
            com.vshield.mobile.webrtc.WebRTCManager.ensureFactoryInitialized(this)
        } catch (e: Throwable) {
            Log.e("VShieldApp", "WebRTC init failed: ${e.message}", e)
        }
    }

    private fun restoreSession() {
        val token = tokenManager.getToken()
        if (token != null) {
            RetrofitClient.setToken(token)
            com.vshield.mobile.service.VShieldBackgroundService.start(this)
        }
    }
}
