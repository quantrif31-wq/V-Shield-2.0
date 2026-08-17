package com.vshield.mobile.security

import org.junit.Assert.assertEquals
import org.junit.Assert.assertThrows
import org.junit.Assert.assertTrue
import org.junit.Test

class OfflineQrGeneratorTest {

    private val baseConfig = OfflineQrConfig(
        employeeId = 42,
        employeeName = "Nguyen An",
        secretKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", // Base32 of "12345678901234567890"
        timeStepSeconds = 30,
        digits = 6
    )

    @Test
    fun generate_producesExpectedPayloadFormat() {
        val data = OfflineQrGenerator.generate(baseConfig, nowEpochSeconds = 0L)
        assertEquals("EMP:42|TS:0|OTP:755224", data.qrPayload)
        assertEquals(42, data.employeeId)
        assertEquals("Nguyen An", data.employeeName)
    }

    @Test
    fun generate_matchesRfc4226TotpVectors() {
        val counter1 = OfflineQrGenerator.generate(baseConfig, nowEpochSeconds = 30L)
        assertTrue(counter1.qrPayload.endsWith("OTP:287082"))
        val counter4 = OfflineQrGenerator.generate(baseConfig, nowEpochSeconds = 120L)
        assertTrue(counter4.qrPayload.endsWith("OTP:338314"))
    }

    @Test
    fun generate_isDeterministicForSameInput() {
        val first = OfflineQrGenerator.generate(baseConfig, nowEpochSeconds = 12345L)
        val second = OfflineQrGenerator.generate(baseConfig, nowEpochSeconds = 12345L)
        assertEquals(first.qrPayload, second.qrPayload)
        assertEquals(first.expiresAtUtc, second.expiresAtUtc)
    }

    @Test
    fun generate_coercesTimeStepToAtLeastFifteen() {
        val data = OfflineQrGenerator.generate(
            baseConfig.copy(timeStepSeconds = 5),
            nowEpochSeconds = 100L
        )
        assertEquals(15, data.timeStepSeconds)
    }

    @Test
    fun generate_computesRemainingSecondsUntilNextBoundary() {
        val data = OfflineQrGenerator.generate(baseConfig, nowEpochSeconds = 30L)
        assertEquals(30, data.remainingSeconds)
    }

    @Test
    fun generate_formatsTimestampsAsUtcIso() {
        val data = OfflineQrGenerator.generate(baseConfig, nowEpochSeconds = 0L)
        assertEquals("1970-01-01T00:00:00Z", data.generatedAtUtc)
        assertEquals("1970-01-01T00:00:30Z", data.expiresAtUtc)
    }

    @Test
    fun generate_rejectsInvalidBase32Secret() {
        assertThrows(IllegalArgumentException::class.java) {
            OfflineQrGenerator.generate(baseConfig.copy(secretKey = "NOT-VALID!!"), nowEpochSeconds = 0L)
        }
    }

    @Test
    fun generate_trimsBase32Padding() {
        val data = OfflineQrGenerator.generate(baseConfig.copy(secretKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ===="), nowEpochSeconds = 0L)
        assertEquals("EMP:42|TS:0|OTP:755224", data.qrPayload)
    }
}
