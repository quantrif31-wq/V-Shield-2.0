package com.vshield.mobile.security

import android.content.Context
import android.content.SharedPreferences
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey

data class OfflineUserSession(
    val username: String,
    val fullName: String,
    val employeeId: Int,
    val role: String?
)

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
            appContext.getSharedPreferences(FALLBACK_PREFS_NAME, Context.MODE_PRIVATE)
        }
    }

    fun enableBiometricForSession(username: String, types: Set<BiometricType>) {
        prefs.edit()
            .putBoolean(KEY_BIOMETRIC_ENABLED, true)
            .putString(KEY_LAST_USERNAME, username)
            .putStringSet(KEY_BIOMETRIC_TYPES, types.map { it.name }.toSet())
            .apply()
    }

    fun rememberLastUsername(username: String) {
        prefs.edit()
            .putString(KEY_LAST_USERNAME, username)
            .apply()
    }

    fun saveOfflineUserSession(
        username: String,
        fullName: String,
        employeeId: Int,
        role: String?
    ) {
        prefs.edit()
            .putString(KEY_LAST_USERNAME, username)
            .putString(KEY_OFFLINE_USERNAME, username)
            .putString(KEY_OFFLINE_FULL_NAME, fullName)
            .putInt(KEY_OFFLINE_EMPLOYEE_ID, employeeId)
            .putString(KEY_OFFLINE_ROLE, role)
            .apply()
    }

    fun saveOfflineQrConfig(config: OfflineQrConfig) {
        prefs.edit()
            .putInt(KEY_OFFLINE_QR_EMPLOYEE_ID, config.employeeId)
            .putString(KEY_OFFLINE_QR_EMPLOYEE_NAME, config.employeeName)
            .putString(KEY_OFFLINE_QR_SECRET, config.secretKey)
            .putInt(KEY_OFFLINE_QR_TIME_STEP, config.timeStepSeconds)
            .putInt(KEY_OFFLINE_QR_DIGITS, config.digits)
            .apply()
    }

    fun getOfflineUserSession(): OfflineUserSession? {
        val username = prefs.getString(KEY_OFFLINE_USERNAME, null)
        val fullName = prefs.getString(KEY_OFFLINE_FULL_NAME, null)
        val employeeId = prefs.getInt(KEY_OFFLINE_EMPLOYEE_ID, 0)

        if (username.isNullOrBlank() || fullName.isNullOrBlank() || employeeId <= 0) {
            return null
        }

        return OfflineUserSession(
            username = username,
            fullName = fullName,
            employeeId = employeeId,
            role = prefs.getString(KEY_OFFLINE_ROLE, null)
        )
    }

    fun getOfflineQrConfig(): OfflineQrConfig? {
        val employeeId = prefs.getInt(KEY_OFFLINE_QR_EMPLOYEE_ID, 0)
        val employeeName = prefs.getString(KEY_OFFLINE_QR_EMPLOYEE_NAME, null)
        val secretKey = prefs.getString(KEY_OFFLINE_QR_SECRET, null)
        val timeStepSeconds = prefs.getInt(KEY_OFFLINE_QR_TIME_STEP, 0)
        val digits = prefs.getInt(KEY_OFFLINE_QR_DIGITS, 0)

        if (employeeId <= 0 ||
            employeeName.isNullOrBlank() ||
            secretKey.isNullOrBlank() ||
            timeStepSeconds <= 0 ||
            digits <= 0
        ) {
            return null
        }

        return OfflineQrConfig(
            employeeId = employeeId,
            employeeName = employeeName,
            secretKey = secretKey,
            timeStepSeconds = timeStepSeconds,
            digits = digits
        )
    }

    fun hasOfflineSession(): Boolean = getOfflineUserSession() != null

    fun clearOfflineSession() {
        prefs.edit()
            .remove(KEY_OFFLINE_USERNAME)
            .remove(KEY_OFFLINE_FULL_NAME)
            .remove(KEY_OFFLINE_EMPLOYEE_ID)
            .remove(KEY_OFFLINE_ROLE)
            .remove(KEY_OFFLINE_QR_EMPLOYEE_ID)
            .remove(KEY_OFFLINE_QR_EMPLOYEE_NAME)
            .remove(KEY_OFFLINE_QR_SECRET)
            .remove(KEY_OFFLINE_QR_TIME_STEP)
            .remove(KEY_OFFLINE_QR_DIGITS)
            .apply()
    }

    fun disableBiometric() {
        prefs.edit()
            .remove(KEY_BIOMETRIC_ENABLED)
            .remove(KEY_BIOMETRIC_TYPES)
            .apply()
    }

    fun isBiometricEnabled(): Boolean = prefs.getBoolean(KEY_BIOMETRIC_ENABLED, false)

    fun getEnabledBiometricTypes(): Set<BiometricType> {
        val rawValues = prefs.getStringSet(KEY_BIOMETRIC_TYPES, emptySet()).orEmpty()
        return rawValues.mapNotNull { value ->
            runCatching { BiometricType.valueOf(value) }.getOrNull()
        }.toSet()
    }

    fun getLastUsername(): String? = prefs.getString(KEY_LAST_USERNAME, null)

    companion object {
        private const val ENCRYPTED_PREFS_NAME = "vshield_biometric_prefs"
        private const val FALLBACK_PREFS_NAME = "vshield_biometric_prefs_fallback"
        private const val KEY_BIOMETRIC_ENABLED = "biometric_enabled"
        private const val KEY_BIOMETRIC_TYPES = "biometric_types"
        private const val KEY_LAST_USERNAME = "biometric_last_username"
        private const val KEY_OFFLINE_USERNAME = "offline_username"
        private const val KEY_OFFLINE_FULL_NAME = "offline_full_name"
        private const val KEY_OFFLINE_EMPLOYEE_ID = "offline_employee_id"
        private const val KEY_OFFLINE_ROLE = "offline_role"
        private const val KEY_OFFLINE_QR_EMPLOYEE_ID = "offline_qr_employee_id"
        private const val KEY_OFFLINE_QR_EMPLOYEE_NAME = "offline_qr_employee_name"
        private const val KEY_OFFLINE_QR_SECRET = "offline_qr_secret"
        private const val KEY_OFFLINE_QR_TIME_STEP = "offline_qr_time_step"
        private const val KEY_OFFLINE_QR_DIGITS = "offline_qr_digits"
    }
}
