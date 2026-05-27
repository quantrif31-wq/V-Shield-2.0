<template>
  <div class="visitor-pass-page">
    <div class="card">
      <h1>Thẻ QR khách mời</h1>
      <p v-if="visitorName" class="sub">{{ visitorName }} <span v-if="visitorId">- ID {{ visitorId }}</span></p>

      <div v-if="loading" class="state">Đang tải QR...</div>
      <div v-else-if="error" class="state error">{{ error }}</div>
      <div v-else class="qr-wrap">
        <canvas ref="qrCanvas" width="280" height="280"></canvas>
        <p class="hint">QR động tự làm mới, vui lòng đưa mã này cho camera quét.</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onBeforeUnmount, onMounted, ref, nextTick } from 'vue'
import { useRoute } from 'vue-router'
import QRCode from 'qrcode'
import { getVisitorPass } from '../services/preRegistrationApi'

const route = useRoute()
const qrCanvas = ref(null)
const loading = ref(true)
const error = ref('')
const visitorName = ref('')
const visitorId = ref(null)
let timer = null

async function drawQr(payload) {
  if (!qrCanvas.value || !payload) return
  await QRCode.toCanvas(qrCanvas.value, payload, { width: 280, margin: 2 })
}

async function fetchPass() {
  try {
    const token = String(route.params.token || '')
    const res = await getVisitorPass(token)
    const data = res?.data || {}
    visitorName.value = data?.Visitor?.FullName || data?.visitor?.fullName || ''
    visitorId.value = data?.Visitor?.VisitorDetailId || data?.visitor?.visitorDetailId || null
    const payload = data?.DynamicQr?.QrPayload || data?.dynamicQr?.qrPayload || ''
    error.value = ''
    await nextTick()
    await drawQr(payload)
  } catch (e) {
    error.value = e?.response?.data?.Message || e?.response?.data?.message || 'Không thể tải thẻ QR.'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await fetchPass()
  timer = setInterval(fetchPass, 5000)
})

onBeforeUnmount(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped>
.visitor-pass-page { min-height: 100vh; display: grid; place-items: center; padding: 24px; background: #eef4f7; }
.card { width: min(92vw, 520px); background: #fff; border: 1px solid #dbe7ef; border-radius: 18px; padding: 24px; text-align: center; box-shadow: 0 14px 40px rgba(15, 23, 42, 0.08); }
h1 { margin: 0 0 8px; font-size: 28px; color: #0f2747; }
.sub { margin: 0 0 16px; color: #4a647b; }
.qr-wrap { display: grid; justify-items: center; gap: 12px; }
canvas { border-radius: 12px; background: #fff; border: 1px solid #d6e2ec; }
.hint { margin: 0; color: #5d748a; font-size: 14px; }
.state { padding: 12px; color: #27445f; }
.state.error { color: #b42318; background: #fff1f0; border: 1px solid #f4c7c3; border-radius: 10px; }
</style>
