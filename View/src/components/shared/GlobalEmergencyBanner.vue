<template>
  <section v-if="currentAlert" class="emergency-banner" role="alert" aria-live="assertive">
    <div class="emergency-pulse" aria-hidden="true"></div>
    <div class="emergency-copy">
      <strong>{{ currentAlert.title }}</strong>
      <span>{{ currentAlert.message }}</span>
    </div>
    <span v-if="remainingCount" class="emergency-count">+{{ remainingCount }}</span>
    <button type="button" class="emergency-action" @click="openDetails">Xem chi tiết</button>
    <button type="button" class="emergency-dismiss" aria-label="Ẩn cảnh báo tạm thời" @click="dismissedId = currentAlert.id">×</button>
  </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { securityAlertState } from '../../services/securityAlertBus'

const router = useRouter()
const dismissedId = ref('')
const currentAlert = computed(() => securityAlertState.items.find(item => item.id !== dismissedId.value) || null)
const remainingCount = computed(() => Math.max(0, securityAlertState.items.length - 1))

watch(() => securityAlertState.items[0]?.id, id => {
  if (id && id !== dismissedId.value) dismissedId.value = ''
})

function openDetails() {
  if (currentAlert.value?.route) router.push(currentAlert.value.route)
}
</script>

<style scoped>
.emergency-banner { position: fixed; top: var(--header-height); left: var(--sidebar-width); right: 0; z-index: 75; min-height: 54px; display: grid; grid-template-columns: auto minmax(0, 1fr) auto auto auto; align-items: center; gap: 12px; padding: 9px 18px; color: #fff; background: #a61b1b; border-bottom: 1px solid #7f1d1d; box-shadow: 0 8px 20px rgba(127,29,29,.22); }
.emergency-pulse { width: 10px; height: 10px; border-radius: 50%; background: #fff; box-shadow: 0 0 0 5px rgba(255,255,255,.2); animation: pulse 1.5s infinite; }
.emergency-copy { display: flex; min-width: 0; gap: 10px; align-items: baseline; }
.emergency-copy strong { white-space: nowrap; }
.emergency-copy span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: #fee2e2; }
.emergency-count { font-weight: 800; }
.emergency-action,.emergency-dismiss { border: 1px solid rgba(255,255,255,.55); background: transparent; color: #fff; min-height: 34px; cursor: pointer; }
.emergency-action { padding: 0 12px; font-weight: 700; }
.emergency-dismiss { width: 34px; font-size: 22px; }
@keyframes pulse { 50% { opacity: .45; transform: scale(.85); } }
@media (max-width: 1023px) { .emergency-banner { left: 0; grid-template-columns: auto minmax(0,1fr) auto auto; } .emergency-count { display:none; } }
@media (max-width: 680px) { .emergency-copy span { display:none; } .emergency-action { font-size: 12px; } }
</style>
