import { onMounted, onBeforeUnmount, ref } from 'vue'
import { onEntityChanged } from '../services/notificationApi'

/**
 * Hook to automatically trigger a data refresh when real-time entity changes occur.
 * 
 * @param {string|string[]} entityTypes - Entity name(s) e.g. 'Employee', ['Vehicle', 'VehicleType']
 * @param {Function} refreshCallback - Function to call when an update is received
 * @param {Object} [options]
 * @param {number} [options.debounceMs=250] - Debounce window in ms for rapid bursts
 * @param {boolean} [options.immediate=false] - Whether to call refreshCallback immediately on mount
 */
export function useRealtimeSync(entityTypes, refreshCallback, options = {}) {
  const { debounceMs = 250, immediate = false } = options
  const isUpdating = ref(false)
  const lastSyncAt = ref(null)
  let timer = null
  let unsubscribe = null

  const triggerRefresh = (event) => {
    lastSyncAt.value = event?.occurredAtUtc || new Date().toISOString()
    if (debounceMs <= 0) {
      isUpdating.value = true
      Promise.resolve(refreshCallback(event)).finally(() => {
        isUpdating.value = false
      })
      return
    }

    if (timer) clearTimeout(timer)
    timer = setTimeout(() => {
      isUpdating.value = true
      Promise.resolve(refreshCallback(event)).finally(() => {
        isUpdating.value = false
      })
    }, debounceMs)
  }

  onMounted(() => {
    if (immediate) {
      triggerRefresh({ aggregateType: 'Initial', action: 'Mount' })
    }
    unsubscribe = onEntityChanged(entityTypes, (event) => {
      triggerRefresh(event)
    })
  })

  onBeforeUnmount(() => {
    if (timer) {
      clearTimeout(timer)
      timer = null
    }
    if (unsubscribe) {
      unsubscribe()
      unsubscribe = null
    }
  })

  return {
    isUpdating,
    lastSyncAt,
    refreshNow: () => triggerRefresh({ aggregateType: 'Manual', action: 'Trigger' })
  }
}
