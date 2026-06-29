package com.vshield.mobile

import android.app.Application
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.TokenManager
import org.osmdroid.config.Configuration

class VShieldApp : Application() {

    lateinit var tokenManager: TokenManager
        private set

    override fun onCreate() {
        super.onCreate()
        Configuration.getInstance().apply {
            userAgentValue = packageName
            osmdroidTileCache = cacheDir.resolve("tiles")
        }
        tokenManager = TokenManager(this)
        restoreSession()
    }

    private fun restoreSession() {
        val token = tokenManager.getToken()
        if (token != null) {
            RetrofitClient.setToken(token)
        }
    }
}
