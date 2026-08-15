package com.vshield.mobile.security

import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.provider.Settings
import androidx.biometric.BiometricManager
import androidx.biometric.BiometricPrompt
import androidx.core.content.ContextCompat
import androidx.fragment.app.FragmentActivity

enum class BiometricType {
    FINGERPRINT,
    FACE,
    GENERIC
}

data class BiometricCapability(
    val type: BiometricType,
    val label: String
)

class BiometricAuthManager(private val context: Context) {

    fun getSupportedBiometricCapabilities(): List<BiometricCapability> {
        val packageManager = context.packageManager
        val capabilities = buildList {
            if (packageManager.hasSystemFeature(PackageManager.FEATURE_FINGERPRINT)) {
                add(BiometricCapability(BiometricType.FINGERPRINT, "Van tay"))
            }

            if (supportsFaceUnlock(packageManager)) {
                add(BiometricCapability(BiometricType.FACE, "Khuon mat"))
            }

            if (isDeviceCredentialReady()) {
                add(BiometricCapability(BiometricType.GENERIC, "Pin / Mat khau"))
            }
        }

        if (capabilities.isNotEmpty()) {
            return capabilities
        }

        return if (isBiometricReady()) {
            listOf(BiometricCapability(BiometricType.GENERIC, "Khoa dien thoai"))
        } else {
            emptyList()
        }
    }

    fun isDeviceCredentialReady(): Boolean {
        val biometricManager = BiometricManager.from(context)
        return biometricManager.canAuthenticate(
            BiometricManager.Authenticators.DEVICE_CREDENTIAL
        ) == BiometricManager.BIOMETRIC_SUCCESS
    }

    fun isBiometricReady(): Boolean {
        val biometricManager = BiometricManager.from(context)
        val canWeak = biometricManager.canAuthenticate(BiometricManager.Authenticators.BIOMETRIC_WEAK)
        val canStrong = biometricManager.canAuthenticate(BiometricManager.Authenticators.BIOMETRIC_STRONG)
        val canDeviceCredential = biometricManager.canAuthenticate(
            BiometricManager.Authenticators.DEVICE_CREDENTIAL
        )
        return canWeak == BiometricManager.BIOMETRIC_SUCCESS ||
            canStrong == BiometricManager.BIOMETRIC_SUCCESS ||
            canDeviceCredential == BiometricManager.BIOMETRIC_SUCCESS
    }

    fun authenticate(
        activity: FragmentActivity,
        title: String = "Xac thuc de mo ung dung",
        subtitle: String = "Dung Pin, van tay hoac khuon mat de mo nhanh",
        onSuccess: () -> Unit,
        onError: (String) -> Unit
    ) {
        val executor = ContextCompat.getMainExecutor(context)

        val callback = object : BiometricPrompt.AuthenticationCallback() {
            override fun onAuthenticationSucceeded(result: BiometricPrompt.AuthenticationResult) {
                onSuccess()
            }

            override fun onAuthenticationError(errorCode: Int, errString: CharSequence) {
                if (errorCode != BiometricPrompt.ERROR_NEGATIVE_BUTTON &&
                    errorCode != BiometricPrompt.ERROR_USER_CANCELED
                ) {
                    onError(errString.toString())
                }
            }

            override fun onAuthenticationFailed() {
                onError("Xac thuc that bai, vui long thu lai")
            }
        }

        runCatching {
            val promptInfo = buildPromptInfo(title, subtitle)
            BiometricPrompt(activity, executor, callback)
                .authenticate(promptInfo)
        }.onFailure { ex ->
            onError(ex.message ?: "Khong mo duoc xac thuc sinh trac hoc tren thiet bi nay.")
        }
    }

    fun confirmEnrollment(
        activity: FragmentActivity,
        selectedTypes: Set<BiometricType>,
        onSuccess: () -> Unit,
        onError: (String) -> Unit
    ) {
        authenticate(
            activity = activity,
            title = "Bat dang nhap nhanh",
            subtitle = buildEnrollmentSubtitle(selectedTypes),
            onSuccess = onSuccess,
            onError = onError
        )
    }

    fun openEnrollmentSettings(activity: FragmentActivity): Boolean {
        val intent = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            Intent(Settings.ACTION_BIOMETRIC_ENROLL).apply {
                putExtra(
                    Settings.EXTRA_BIOMETRIC_AUTHENTICATORS_ALLOWED,
                    AUTHENTICATOR
                )
            }
        } else {
            Intent(Settings.ACTION_SECURITY_SETTINGS)
        }

        return runCatching {
            activity.startActivity(intent)
            true
        }.getOrDefault(false)
    }

    private fun supportsFaceUnlock(packageManager: PackageManager): Boolean {
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            packageManager.hasSystemFeature(PackageManager.FEATURE_FACE)
        } else {
            packageManager.hasSystemFeature("android.hardware.biometrics.face")
        }
    }

    private fun buildEnrollmentSubtitle(selectedTypes: Set<BiometricType>): String {
        val labels = selectedTypes.toDisplayText()
        return if (labels.isBlank()) {
            "Xac nhan sinh trac hoc de luu thiet bi nay"
        } else {
            "Xac nhan $labels de luu thiet bi nay"
        }
    }

    private fun buildPromptInfo(title: String, subtitle: String): BiometricPrompt.PromptInfo {
        val allowDeviceCredential = Build.VERSION.SDK_INT >= Build.VERSION_CODES.R && isDeviceCredentialReady()

        val builder = BiometricPrompt.PromptInfo.Builder()
            .setTitle(title)
            .setSubtitle(subtitle)

        if (!allowDeviceCredential) {
            builder.setNegativeButtonText("Huy")
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            val allowed = if (allowDeviceCredential) {
                BiometricManager.Authenticators.DEVICE_CREDENTIAL or
                    BiometricManager.Authenticators.BIOMETRIC_WEAK
            } else {
                BiometricManager.Authenticators.BIOMETRIC_WEAK
            }
            builder.setAllowedAuthenticators(allowed)
        }
        return builder.build()
    }

    companion object {
        private const val AUTHENTICATOR = BiometricManager.Authenticators.BIOMETRIC_WEAK
    }
}

fun Set<BiometricType>.toDisplayText(): String {
    if (isEmpty()) {
        return "sinh trac hoc"
    }

    val labels = mapNotNull {
        when (it) {
            BiometricType.FINGERPRINT -> "van tay"
            BiometricType.FACE -> "khuon mat"
            BiometricType.GENERIC -> "sinh trac hoc"
        }
    }.distinct()

    return when (labels.size) {
        0 -> "sinh trac hoc"
        1 -> labels.first()
        2 -> "${labels[0]} va ${labels[1]}"
        else -> labels.dropLast(1).joinToString(", ") + " va " + labels.last()
    }
}
