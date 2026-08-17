package com.vshield.mobile.data.model

import com.google.gson.Gson
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class ModelSerializationTest {

    private val gson = Gson()

    @Test
    fun qrResponse_deserializesFields() {
        val json = """
            {
              "success": true,
              "message": "ok",
              "data": {
                "employeeId": 7,
                "employeeName": "Nguyen An",
                "qrPayload": "EMP:7|TS:10|OTP:123456",
                "timeStepSeconds": 30,
                "generatedAtUtc": "2026-01-01T00:00:00Z",
                "expiresAtUtc": "2026-01-01T00:00:30Z",
                "remainingSeconds": 30
              }
            }
        """.trimIndent()

        val response = gson.fromJson(json, QrResponse::class.java)
        assertEquals(true, response.success)
        assertEquals("ok", response.message)
        val data = response.data!!
        assertEquals(7, data.employeeId)
        assertEquals("Nguyen An", data.employeeName)
        assertEquals("EMP:7|TS:10|OTP:123456", data.qrPayload)
        assertEquals(30, data.timeStepSeconds)
        assertEquals(30, data.remainingSeconds)
    }

    @Test
    fun qrResponse_acceptsNullData() {
        val json = """{"success": false, "message": "failed", "data": null}"""
        val response = gson.fromJson(json, QrResponse::class.java)
        assertEquals(false, response.success)
        assertNull(response.data)
    }

    @Test
    fun offlineQrBootstrapResponse_deserializesFields() {
        val json = """
            {
              "success": true,
              "message": null,
              "data": {
                "employeeId": 3,
                "employeeName": "Bao Ve",
                "secretKey": "JBSWY3DPEHPK3PXP",
                "timeStepSeconds": 30,
                "digits": 6,
                "issuedAtUtc": "2026-01-01T00:00:00Z"
              }
            }
        """.trimIndent()

        val response = gson.fromJson(json, OfflineQrBootstrapResponse::class.java)
        assertEquals(true, response.success)
        val data = response.data!!
        assertEquals(3, data.employeeId)
        assertEquals("Bao Ve", data.employeeName)
        assertEquals("JBSWY3DPEHPK3PXP", data.secretKey)
        assertEquals(6, data.digits)
    }

    @Test
    fun loginData_deserializes() {
        val json = """
            {"token": "abc", "refreshToken": "xyz", "employeeId": 1, "role": "Admin", "username": "admin", "requiresMfa": false}
        """.trimIndent()
        val model = gson.fromJson(json, LoginData::class.java)
        assertEquals("abc", model.token)
        assertEquals("xyz", model.refreshToken)
        assertEquals(1, model.employeeId)
        assertEquals("Admin", model.role)
        assertEquals(false, model.requiresMfa)
    }
}
