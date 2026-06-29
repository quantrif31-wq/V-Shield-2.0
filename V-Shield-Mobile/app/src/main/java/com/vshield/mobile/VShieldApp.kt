package com.vshield.mobile

import android.app.Application
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.TokenManager

class VShieldApp : Application() {

    lateinit var tokenManager: TokenManager
        private set

    override fun onCreate() {
        super.onCreate()
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
