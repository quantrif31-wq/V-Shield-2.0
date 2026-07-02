package com.vshield.mobile.security

import android.content.Context
import android.content.SharedPreferences
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey

class SecureStorage(context: Context) {

    private val appContext = context.applicationContext
    private val prefs: SharedPreferences = createPreferences()

    private fun createPreferences(): SharedPreferences {
        return runCatching {
            val masterKey = MasterKey.Builder(appContext)
                .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
                .build()

            EncryptedSharedPreferences.create(
                appContext,
                ENCRYPTED_PREFS_NAME,
                masterKey,
                EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
                EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
            )
        }.getOrElse {
            // Keep the app usable on devices where the encrypted keystore gets invalidated.
            appContext.getSharedPreferences(FALLBACK_PREFS_NAME, Context.MODE_PRIVATE)
        }
    }

    fun enableBiometricForSession(username: String) {
        prefs.edit()
            .putBoolean(KEY_BIOMETRIC_ENABLED, true)
            .putString(KEY_LAST_USERNAME, username)
            .apply()
    }

    fun rememberLastUsername(username: String) {
        prefs.edit()
            .putString(KEY_LAST_USERNAME, username)
            .apply()
    }

    fun disableBiometric() {
        prefs.edit()
            .remove(KEY_BIOMETRIC_ENABLED)
            .apply()
    }

    fun isBiometricEnabled(): Boolean = prefs.getBoolean(KEY_BIOMETRIC_ENABLED, false)

    fun getLastUsername(): String? = prefs.getString(KEY_LAST_USERNAME, null)

    companion object {
        private const val ENCRYPTED_PREFS_NAME = "vshield_biometric_prefs"
        private const val FALLBACK_PREFS_NAME = "vshield_biometric_prefs_fallback"
        private const val KEY_BIOMETRIC_ENABLED = "biometric_enabled"
        private const val KEY_LAST_USERNAME = "biometric_last_username"
    }
}
