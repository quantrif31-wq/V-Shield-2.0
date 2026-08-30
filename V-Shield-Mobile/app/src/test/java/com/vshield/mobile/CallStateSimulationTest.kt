package com.vshield.mobile

import com.google.gson.JsonObject
import com.google.gson.JsonParser
import com.vshield.mobile.data.model.ChatCallState
import com.vshield.mobile.data.model.SignalRCallInfo
import com.vshield.mobile.data.model.SignalRCallResponse
import org.junit.Assert.*
import org.junit.Test

class CallStateSimulationTest {

    @Test
    fun testAudioCallStateTransitions() {
        var state: ChatCallState = ChatCallState.Idle
        assertEquals(ChatCallState.Idle, state)

        // 1. Initiate audio call
        state = ChatCallState.Outgoing(
            toEmployeeId = 10,
            toFullName = "Nguyen Van A",
            conversationId = 1,
            callType = "audio"
        )
        assertTrue(state is ChatCallState.Outgoing)
        assertEquals("audio", (state as ChatCallState.Outgoing).callType)

        // 2. Remote accepts
        state = ChatCallState.Connected(
            withEmployeeId = 10,
            withFullName = "Nguyen Van A",
            callType = "audio"
        )
        assertTrue(state is ChatCallState.Connected)
        assertEquals("audio", (state as ChatCallState.Connected).callType)

        // 3. End call
        state = ChatCallState.Idle
        assertEquals(ChatCallState.Idle, state)
    }

    @Test
    fun testVideoCallStateTransitions() {
        var state: ChatCallState = ChatCallState.Idle

        // 1. Incoming video call with video m-line
        val sampleOfferSdp = "v=0\r\no=- 12345 2 IN IP4 127.0.0.1\r\ns=-\r\nm=audio 9 UDP/TLS/RTP/SAVPF 111\r\nm=video 9 UDP/TLS/RTP/SAVPF 96\r\n"
        val isVideo = sampleOfferSdp.contains("m=video")

        state = ChatCallState.Incoming(
            fromEmployeeId = 20,
            fromFullName = "Tran Thi B",
            conversationId = 2,
            offerSdp = sampleOfferSdp,
            callType = if (isVideo) "video" else "audio"
        )

        assertTrue(state is ChatCallState.Incoming)
        assertEquals("video", (state as ChatCallState.Incoming).callType)

        // 2. Accept
        state = ChatCallState.Connected(
            withEmployeeId = 20,
            withFullName = "Tran Thi B",
            callType = "video"
        )
        assertTrue(state is ChatCallState.Connected)
        assertEquals("video", (state as ChatCallState.Connected).callType)
    }

    @Test
    fun testIceCandidateJsonSerialization() {
        val sdpMid = "audio"
        val sdpMLineIndex = 0
        val sdp = "candidate:12345 1 udp 2122260223 192.168.1.100 50000 typ host"

        val json = JsonObject().apply {
            addProperty("sdpMid", sdpMid)
            addProperty("sdpMLineIndex", sdpMLineIndex)
            addProperty("candidate", sdp)
        }.toString()

        val parsed = JsonParser.parseString(json).asJsonObject
        assertEquals("audio", parsed.get("sdpMid").asString)
        assertEquals(0, parsed.get("sdpMLineIndex").asInt)
        assertEquals(sdp, parsed.get("candidate").asString)
    }
}
