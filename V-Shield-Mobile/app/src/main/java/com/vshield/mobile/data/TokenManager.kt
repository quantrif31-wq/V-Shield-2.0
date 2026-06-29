package com.vshield.mobile.data

import android.content.Context
import android.content.SharedPreferences
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKeys

class TokenManager(context: Context) {

    private val masterKeyAlias = MasterKeys.getOrCreate(MasterKeys.AES256_GCM_SPEC)

    private val sharedPreferences: SharedPreferences = EncryptedSharedPreferences.create(
        "vshield_secure_prefs",
        masterKeyAlias,
        context,
        EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
        EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
    )

    fun saveToken(token: String) {
        sharedPreferences.edit().putString(KEY_TOKEN, token).apply()
    }

    fun getToken(): String? = sharedPreferences.getString(KEY_TOKEN, null)

    fun saveRefreshToken(refreshToken: String) {
        sharedPreferences.edit().putString(KEY_REFRESH_TOKEN, refreshToken).apply()
    }

    fun getRefreshToken(): String? = sharedPreferences.getString(KEY_REFRESH_TOKEN, null)

    fun saveEmployeeId(employeeId: Int) {
        sharedPreferences.edit().putInt(KEY_EMPLOYEE_ID, employeeId).apply()
    }

    fun getEmployeeId(): Int = sharedPreferences.getInt(KEY_EMPLOYEE_ID, 0)

    fun saveRoles(roles: Set<String>) {
        sharedPreferences.edit().putStringSet(KEY_ROLES, roles).apply()
    }

    fun getRoles(): Set<String> = sharedPreferences.getStringSet(KEY_ROLES, emptySet()) ?: emptySet()

    fun isLoggedIn(): Boolean = getToken() != null

    fun clear() {
        sharedPreferences.edit().clear().apply()
    }

    companion object {
        private const val KEY_TOKEN = "auth_token"
        private const val KEY_REFRESH_TOKEN = "refresh_token"
        private const val KEY_EMPLOYEE_ID = "employee_id"
        private const val KEY_ROLES = "user_roles"
    }
}
