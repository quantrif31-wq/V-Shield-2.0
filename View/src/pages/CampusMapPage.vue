<template>
    <div class="page-container ops-page animate-in">
        <header class="page-header">
            <div>
                <h1 class="page-title">Ban do 3D khuon vien realtime</h1>
                <p class="page-subtitle">
                    Mo hinh 3D toan canh cong ty. Keo tha de xoay view, hover de xem thong tin, click vao cong de chi tiet.
                </p>
            </div>
        </header>

        <MapMiniStats :summary="summary" />

        <CampusMapToolbar
            :mode="'view'"
            :can-edit="false"
            :dirty="false"
            :saving="false"
            :refreshing="refreshing"
            @refresh="refreshAll"
            @fit-screen="fitToScreen"
        />

        <div v-if="loading" class="empty-card">Dang tai du lieu 3D khuon vien...</div>
        <div v-else-if="error" class="empty-card">{{ error }}</div>
        <section v-else class="campus-layout">
            <Campus3DCanvas
                ref="canvasRef"
                :sites="sites"
                :gates="gateStatuses"
                :recent-events="recentEvents"
                :selected-gate-id="selectedGateId"
                @select-gate="onSelectGate"
                @inspect-object="onInspectObject"
            />
            <div class="panel-col">
                <CampusAssetInspector :selected-asset="selectedAsset" :summary="summary" :updated-at="updatedAt" />
                <RealtimeStatusPanel
                    :updated-at="updatedAt"
                    :recent-events="recentEvents"
                    :error="realtimeError"
                    @focus-gate="onFocusGate"
                />
            </div>
        </section>

        <transition name="toast">
            <div v-if="toast" class="toast-card" :class="toast.type">{{ toast.message }}</div>
        </transition>
    </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { authState } from '../stores/auth'
import CampusMapToolbar from '../components/campus-map/CampusMapToolbar.vue'
import Campus3DCanvas from '../components/campus-map/Campus3DCanvas.vue'
import CampusAssetInspector from '../components/campus-map/CampusAssetInspector.vue'
import RealtimeStatusPanel from '../components/campus-map/RealtimeStatusPanel.vue'
import MapMiniStats from '../components/campus-map/MapMiniStats.vue'
import { getCampusScene3D, getCampusMapRealtime } from '../services/campusMapApi'

const sites = ref([])
const gateStatuses = ref([])
const summary = ref({
    siteCount: 0,
    objectCount: 0,
    activeGateCount: 0,
    warningGateCount: 0,
    offlineCameraCount: 0,
    recentEventCount: 0,
})
const recentEvents = ref([])
const updatedAt = ref(null)
const selectedGateId = ref(null)
const selectedAsset = ref(null)
const loading = ref(true)
const refreshing = ref(false)
const error = ref('')
const realtimeError = ref('')
const canvasRef = ref(null)
const toast = ref(null)

let toastTimer = null
let realtimeTimer = null

const role = computed(() => authState.user?.role || '')

const showToast = (message, type = 'success') => {
    if (toastTimer) clearTimeout(toastTimer)
    toast.value = { message, type }
    toastTimer = setTimeout(() => { toast.value = null }, 2600)
}

const loadScene = async () => {
    const { data } = await getCampusScene3D()
    sites.value = data?.sites || []
    gateStatuses.value = data?.gates || []
    summary.value = {
        ...summary.value,
        siteCount: data?.summary?.siteCount || 0,
        objectCount: data?.summary?.objectCount || 0,
        activeGateCount: data?.summary?.onlineGates || 0,
        warningGateCount: data?.summary?.warningGates || 0,
        offlineCameraCount: data?.summary?.offlineCameras || 0,
    }
    updatedAt.value = data?.updatedAt || null
}

const loadRealtime = async () => {
    try {
        const { data } = await getCampusMapRealtime()
        gateStatuses.value = (data?.gates || []).map(g => ({
            gateId: g.gateId,
            gateName: g.gateName || '',
            status: g.status || 'Normal',
            cameraCount: g.cameraCount || 0,
            offlineCameraCount: g.offlineCameraCount || 0,
            lastAccessAt: g.lastAccessAt,
            recentAccessCount: g.recentAccessCount,
        }))
        summary.value = {
            ...summary.value,
            activeGateCount: data?.summary?.activeGateCount || 0,
            warningGateCount: data?.summary?.warningGateCount || 0,
            offlineCameraCount: data?.summary?.offlineCameraCount || 0,
            recentEventCount: data?.summary?.recentEventCount || 0,
        }
        updatedAt.value = data?.updatedAt || null
        recentEvents.value = data?.recentEvents || []
        realtimeError.value = ''
    } catch (err) {
        realtimeError.value = err?.response?.data?.message || 'Khong tai duoc realtime, se thu lai tu dong.'
    }
}

const refreshAll = async () => {
    refreshing.value = true
    try {
        await loadScene()
        await loadRealtime()
    } finally {
        refreshing.value = false
    }
}

const onSelectGate = (gateId) => {
    selectedGateId.value = gateId
}

const onInspectObject = (asset) => {
    selectedAsset.value = asset
}

const onFocusGate = (gateId) => {
    if (!gateId) return
    selectedGateId.value = gateId
    canvasRef.value?.focusGate?.(gateId)
}

const fitToScreen = () => {
    canvasRef.value?.fitToContent?.()
}

onMounted(async () => {
    loading.value = true
    error.value = ''
    try {
        await loadScene()
        await loadRealtime()
        realtimeTimer = setInterval(loadRealtime, 5000)
    } catch (err) {
        error.value = err?.response?.data?.message || 'Khong tai duoc du lieu campus 3D.'
    } finally {
        loading.value = false
    }
})

onBeforeUnmount(() => {
    if (realtimeTimer) clearInterval(realtimeTimer)
    if (toastTimer) clearTimeout(toastTimer)
})
</script>

<style scoped>
.campus-layout {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 320px;
    gap: 18px;
}

.panel-col {
    display: grid;
    align-content: start;
    gap: 18px;
}

.toast-card {
    position: fixed;
    right: 24px;
    bottom: 24px;
    z-index: 1200;
    padding: 12px 18px;
    border-radius: 12px;
    background: var(--accent-success);
    color: #fff;
    box-shadow: var(--shadow-lg);
}

.toast-card.error {
    background: var(--accent-danger);
}

.toast-enter-active,
.toast-leave-active {
    transition: all 0.24s ease;
}

.toast-enter-from,
.toast-leave-to {
    opacity: 0;
    transform: translateY(12px);
}

@media (max-width: 1200px) {
    .campus-layout {
        grid-template-columns: 1fr;
    }
}
</style>
