import { reactive } from 'vue'
import { enterpriseApi } from './enterpriseSecurityApi'

export const securityAlertState = reactive({
  items: [],
  criticalCount: 0,
  loading: false,
  lastUpdatedAt: null,
  error: '',
})

let timer = null

export async function refreshSecurityAlerts() {
  securityAlertState.loading = true
  try {
    const response = await enterpriseApi.getActiveSecurityAlerts()
    securityAlertState.items = response.data?.items || []
    securityAlertState.criticalCount = response.data?.criticalCount || 0
    securityAlertState.lastUpdatedAt = response.data?.generatedAtUtc || new Date().toISOString()
    securityAlertState.error = ''
  } catch (error) {
    securityAlertState.error = error?.response?.data?.message || 'Không thể cập nhật cảnh báo an ninh.'
  } finally {
    securityAlertState.loading = false
  }
}

export function startSecurityAlertPolling(intervalMs = 10000) {
  stopSecurityAlertPolling()
  refreshSecurityAlerts()
  timer = window.setInterval(refreshSecurityAlerts, intervalMs)
}

export function stopSecurityAlertPolling() {
  if (timer) window.clearInterval(timer)
  timer = null
}
