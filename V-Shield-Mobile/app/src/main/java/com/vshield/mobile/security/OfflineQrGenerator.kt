package com.vshield.mobile.security

import com.vshield.mobile.data.model.QrData
import java.nio.ByteBuffer
import java.time.Instant
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec
import kotlin.math.max

data class OfflineQrConfig(
    val employeeId: Int,
    val employeeName: String,
    val secretKey: String,
    val timeStepSeconds: Int,
    val digits: Int
)

object OfflineQrGenerator {
    private val isoFormatter = DateTimeFormatter.ISO_INSTANT.withZone(ZoneOffset.UTC)

    fun generate(config: OfflineQrConfig, nowEpochSeconds: Long = Instant.now().epochSecond): QrData {
        val timeStep = config.timeStepSeconds.coerceAtLeast(15)
        val counter = nowEpochSeconds / timeStep
        val nextBoundary = (counter + 1L) * timeStep
        val otp = generateTotp(config.secretKey, counter, config.digits)

        return QrData(
            employeeId = config.employeeId,
            employeeName = config.employeeName,
            qrPayload = "EMP:${config.employeeId}|TS:$counter|OTP:$otp",
            timeStepSeconds = timeStep,
            generatedAtUtc = isoFormatter.format(Instant.ofEpochSecond(nowEpochSeconds)),
            expiresAtUtc = isoFormatter.format(Instant.ofEpochSecond(nextBoundary)),
            remainingSeconds = max(0, (nextBoundary - nowEpochSeconds).toInt())
        )
    }

    private fun generateTotp(base32Secret: String, counter: Long, digits: Int): String {
        val keyBytes = base32Decode(base32Secret)
        val counterBytes = ByteBuffer.allocate(8).putLong(counter).array()

        val mac = Mac.getInstance("HmacSHA1")
        mac.init(SecretKeySpec(keyBytes, "HmacSHA1"))
        val hash = mac.doFinal(counterBytes)

        val offset = hash.last().toInt() and 0x0F
        val binaryCode =
            ((hash[offset].toInt() and 0x7F) shl 24) or
                ((hash[offset + 1].toInt() and 0xFF) shl 16) or
                ((hash[offset + 2].toInt() and 0xFF) shl 8) or
                (hash[offset + 3].toInt() and 0xFF)

        val modulo = Math.pow(10.0, digits.toDouble()).toInt().coerceAtLeast(1)
        return (binaryCode % modulo).toString().padStart(digits, '0')
    }

    private fun base32Decode(input: String): ByteArray {
        val alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"
        val normalized = input.trim().trimEnd('=').uppercase()
        val output = ArrayList<Byte>()

        var bitBuffer = 0
        var bitsLeft = 0

        for (char in normalized) {
            val value = alphabet.indexOf(char)
            require(value >= 0) { "SecretKey Base32 khong hop le." }

            bitBuffer = (bitBuffer shl 5) or (value and 0x1F)
            bitsLeft += 5

            if (bitsLeft >= 8) {
                output.add(((bitBuffer shr (bitsLeft - 8)) and 0xFF).toByte())
                bitsLeft -= 8
            }
        }

        return output.toByteArray()
    }
}
