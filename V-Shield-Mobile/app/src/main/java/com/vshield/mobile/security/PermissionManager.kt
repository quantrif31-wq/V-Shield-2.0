package com.vshield.mobile.security

import android.Manifest
import android.app.Activity
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.os.PowerManager
import android.provider.Settings
import android.util.Log
import androidx.core.content.ContextCompat
import com.vshield.mobile.service.AutoStartHelper

data class PermissionStatus(
    val hasCamera: Boolean,
    val hasAudio: Boolean,
    val hasNotifications: Boolean,
    val hasLocation: Boolean,
    val hasBluetooth: Boolean,
    val hasOverlay: Boolean,
    val isIgnoringBattery: Boolean
) {
    val isAllGranted: Boolean
        get() = hasCamera && hasAudio && hasNotifications && hasLocation && hasOverlay && isIgnoringBattery

    val missingCount: Int
        get() = listOf(hasCamera, hasAudio, hasNotifications, hasLocation, hasOverlay, isIgnoringBattery).count { !it }
}

object PermissionManager {
    private const val TAG = "PermissionManager"

    fun getRequiredRuntimePermissions(): List<String> {
        val list = mutableListOf(
            Manifest.permission.CAMERA,
            Manifest.permission.RECORD_AUDIO,
            Manifest.permission.ACCESS_FINE_LOCATION,
            Manifest.permission.ACCESS_COARSE_LOCATION
        )
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            list.add(Manifest.permission.POST_NOTIFICATIONS)
            list.add(Manifest.permission.READ_MEDIA_IMAGES)
        } else {
            list.add(Manifest.permission.READ_EXTERNAL_STORAGE)
        }
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            list.add(Manifest.permission.BLUETOOTH_CONNECT)
        }
        return list
    }

    fun checkPermissionStatus(context: Context): PermissionStatus {
        val hasCamera = ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED
        val hasAudio = ContextCompat.checkSelfPermission(context, Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED
        val hasLocFine = ContextCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED
        val hasLocCoarse = ContextCompat.checkSelfPermission(context, Manifest.permission.ACCESS_COARSE_LOCATION) == PackageManager.PERMISSION_GRANTED
        val hasLocation = hasLocFine || hasLocCoarse

        val hasNotifications = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) == PackageManager.PERMISSION_GRANTED
        } else {
            true
        }

        val hasBluetooth = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            ContextCompat.checkSelfPermission(context, Manifest.permission.BLUETOOTH_CONNECT) == PackageManager.PERMISSION_GRANTED
        } else {
            true
        }

        val hasOverlay = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            Settings.canDrawOverlays(context)
        } else {
            true
        }

        val isIgnoringBattery = AutoStartHelper.isIgnoringBatteryOptimizations(context)

        return PermissionStatus(
            hasCamera = hasCamera,
            hasAudio = hasAudio,
            hasNotifications = hasNotifications,
            hasLocation = hasLocation,
            hasBluetooth = hasBluetooth,
            hasOverlay = hasOverlay,
            isIgnoringBattery = isIgnoringBattery
        )
    }

    fun requestOverlayPermission(context: Context) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            try {
                val intent = Intent(
                    Settings.ACTION_MANAGE_OVERLAY_PERMISSION,
                    Uri.parse("package:${context.packageName}")
                ).apply {
                    addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                }
                context.startActivity(intent)
            } catch (e: Exception) {
                Log.e(TAG, "Failed to open overlay permission settings", e)
                AutoStartHelper.openAppDetailsSettings(context)
            }
        }
    }

    fun requestBatteryOptimization(activity: Activity) {
        AutoStartHelper.requestIgnoreBatteryOptimizations(activity)
    }

    fun openAutostartSettings(context: Context) {
        AutoStartHelper.openAutoStartPermissionSettings(context)
    }
}
