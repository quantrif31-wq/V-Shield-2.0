<template>
    <div class="page-container ops-page animate-in">
        <header class="page-header">
            <div>
                <h1 class="page-title">Ban do khuon vien realtime</h1>
                <p class="page-subtitle">
                    Mo phong cac Gate tren so do cong ty. Admin/Manager co the keo tha va luu layout hien thi.
                </p>
            </div>
        </header>

        <MapMiniStats :summary="summary" />

        <CampusMapToolbar
            :mode="mode"
            :can-edit="canEdit"
            :dirty="dirty"
            :saving="saving"
            :refreshing="refreshing"
            @change-mode="changeMode"
            @auto-arrange="autoArrange"
            @reset-layout="resetLayout"
            @fit-screen="fitToScreen"
            @refresh="refreshAll"
            @save="saveLayout"
        />

        <div v-if="loading" class="empty-card">Dang tai campus map...</div>
        <div v-else-if="error" class="empty-card">{{ error }}</div>
        <section v-else class="campus-layout">
            <CampusMapCanvas
                ref="canvasRef"
                class="canvas-col"
                :items="items"
                :selected-gate-id="selectedGateId"
                :editable="mode === 'edit' && canEdit"
                @select="selectedGateId = $event"
                @update-layout="applyLocalLayoutPatch"
            />
            <div class="panel-col">
                <ElementPropertiesPanel
                    :item="selectedItem"
                    :editable="mode === 'edit' && canEdit"
                    @patch-layout="applyLocalLayoutPatch"
                />
                <RealtimeStatusPanel :updated-at="updatedAt" :recent-events="recentEvents" :error="realtimeError" />
            </div>
        </section>

        <transition name="toast">
            <div v-if="toast" class="toast-card" :class="toast.type">{{ toast.message }}</div>
        </transition>
    </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { onBeforeRouteLeave } from 'vue-router'
import { authState } from '../stores/auth'
import CampusMapToolbar from '../components/campus-map/CampusMapToolbar.vue'
import CampusMapCanvas from '../components/campus-map/CampusMapCanvas.vue'
import ElementPropertiesPanel from '../components/campus-map/ElementPropertiesPanel.vue'
import RealtimeStatusPanel from '../components/campus-map/RealtimeStatusPanel.vue'
import MapMiniStats from '../components/campus-map/MapMiniStats.vue'
import { getCampusMapLayout, getCampusMapRealtime, saveCampusMapLayout } from '../services/campusMapApi'

const DEFAULT_W = 220
const DEFAULT_H = 120
const DEFAULT_GAP = 24
const DEFAULT_COLS = 4
const DEFAULT_X = 24
const DEFAULT_Y = 24

const mode = ref('view')
const items = ref([])
const selectedGateId = ref(null)
const summary = ref({
    activeGateCount: 0,
    warningGateCount: 0,
    offlineCameraCount: 0,
    recentEventCount: 0,
})
const recentEvents = ref([])
const updatedAt = ref(null)
const loading = ref(true)
const refreshing = ref(false)
const saving = ref(false)
const error = ref('')
const realtimeError = ref('')
const dirty = ref(false)
const canvasRef = ref(null)
const toast = ref(null)

let toastTimer = null
let realtimeTimer = null

const role = computed(() => authState.user?.role || '')
const canEdit = computed(() => role.value === 'Admin' || role.value === 'Staff')
const selectedItem = computed(() => items.value.find((x) => x.gateId === selectedGateId.value) || null)

const showToast = (message, type = 'success') => {
    if (toastTimer) clearTimeout(toastTimer)
    toast.value = { message, type }
    toastTimer = setTimeout(() => {
        toast.value = null
    }, 2600)
}

const normalizeItem = (raw, index) => {
    const fallback = defaultLayout(index)
    return {
        gateId: raw.gateId,
        gateName: raw.gateName,
        location: raw.location,
        layout: {
            x: Number(raw.layout?.x ?? fallback.x),
            y: Number(raw.layout?.y ?? fallback.y),
            w: Number(raw.layout?.w ?? fallback.w),
            h: Number(raw.layout?.h ?? fallback.h),
            zIndex: Number(raw.layout?.zIndex ?? 1),
            color: raw.layout?.color || '#0f766e',
            icon: raw.layout?.icon || 'gate',
            isVisible: raw.layout?.isVisible ?? true,
            isLocked: raw.layout?.isLocked ?? false,
        },
        stats: raw.stats || {
            cameraCount: 0,
            onlineCameraCount: 0,
            offlineCameraCount: 0,
            lastAccessAt: null,
            recentAccessCount: 0,
        },
        status: raw.status || 'Normal',
    }
}

const defaultLayout = (index) => {
    const col = index % DEFAULT_COLS
    const row = Math.floor(index / DEFAULT_COLS)
    return {
        x: DEFAULT_X + col * (DEFAULT_W + DEFAULT_GAP),
        y: DEFAULT_Y + row * (DEFAULT_H + DEFAULT_GAP),
        w: DEFAULT_W,
        h: DEFAULT_H,
        zIndex: 1,
    }
}

const loadLayout = async () => {
    const { data } = await getCampusMapLayout()
    const list = Array.isArray(data?.items) ? data.items : []
    items.value = list.map((item, index) => normalizeItem(item, index))
    selectedGateId.value = items.value[0]?.gateId ?? null
    dirty.value = false
}

const applyRealtimeSnapshot = (data) => {
    summary.value = data?.summary || summary.value
    updatedAt.value = data?.updatedAt || null
    recentEvents.value = data?.recentEvents || []

    const byGate = new Map((data?.gates || []).map((gate) => [gate.gateId, gate]))
    items.value = items.value.map((item) => {
        const current = byGate.get(item.gateId)
        if (!current) return item
        return {
            ...item,
            status: current.status || item.status,
            stats: {
                ...item.stats,
                cameraCount: current.cameraCount ?? item.stats.cameraCount,
                offlineCameraCount: current.offlineCameraCount ?? item.stats.offlineCameraCount,
                recentAccessCount: current.recentAccessCount ?? item.stats.recentAccessCount,
                lastAccessAt: current.lastAccessAt ?? item.stats.lastAccessAt,
            },
        }
    })
}

const loadRealtime = async () => {
    try {
        const { data } = await getCampusMapRealtime()
        applyRealtimeSnapshot(data)
        realtimeError.value = ''
    } catch (err) {
        realtimeError.value = err?.response?.data?.message || 'Khong tai duoc realtime, se thu lai tu dong.'
    }
}

const refreshAll = async () => {
    refreshing.value = true
    try {
        await loadLayout()
        await loadRealtime()
    } finally {
        refreshing.value = false
    }
}

const applyLocalLayoutPatch = (payload) => {
    const gateId = payload.gateId || selectedGateId.value
    if (!gateId) return

    const index = items.value.findIndex((x) => x.gateId === gateId)
    if (index < 0) return

    const target = items.value[index]
    if (target.layout.isLocked) return

    const nextLayout = {
        ...target.layout,
        ...payload,
    }

    if (nextLayout.w < 120) nextLayout.w = 120
    if (nextLayout.h < 70) nextLayout.h = 70
    if (nextLayout.x < 0) nextLayout.x = 0
    if (nextLayout.y < 0) nextLayout.y = 0

    items.value[index] = {
        ...target,
        layout: nextLayout,
    }
    dirty.value = true
}

const autoArrange = () => {
    if (mode.value !== 'edit' || !canEdit.value) return
    const ordered = [...items.value].sort((a, b) => a.gateId - b.gateId)
    ordered.forEach((item, index) => {
        const pos = defaultLayout(index)
        item.layout = { ...item.layout, ...pos }
    })
    items.value = ordered
    dirty.value = true
}

const resetLayout = () => {
    if (mode.value !== 'edit' || !canEdit.value) return
    const ok = window.confirm('Reset layout ve grid mac dinh?')
    if (!ok) return
    autoArrange()
}

const changeMode = (targetMode) => {
    if (targetMode === 'edit' && !canEdit.value) {
        showToast('Ban khong co quyen vao Edit mode.', 'error')
        return
    }
    mode.value = targetMode
}

const saveLayout = async () => {
    if (!canEdit.value || mode.value !== 'edit' || !dirty.value) return
    saving.value = true
    try {
        await saveCampusMapLayout({
            items: items.value.map((item) => ({
                gateId: item.gateId,
                x: Number(item.layout.x),
                y: Number(item.layout.y),
                w: Number(item.layout.w),
                h: Number(item.layout.h),
                zIndex: Number(item.layout.zIndex || 1),
                color: item.layout.color || null,
                icon: item.layout.icon || null,
                isVisible: !!item.layout.isVisible,
                isLocked: !!item.layout.isLocked,
            })),
        })
        dirty.value = false
        showToast('Da luu layout campus map.')
    } catch (err) {
        showToast(err?.response?.data?.message || 'Luu layout that bai.', 'error')
    } finally {
        saving.value = false
    }
}

const fitToScreen = () => {
    canvasRef.value?.fitToContent()
}

const handleBeforeUnload = (event) => {
    if (!dirty.value) return
    event.preventDefault()
    event.returnValue = ''
}

onBeforeRouteLeave((to, from, next) => {
    if (!dirty.value) {
        next()
        return
    }
    const ok = window.confirm('Ban co thay doi chua luu. Roi trang se mat thay doi, tiep tuc?')
    next(ok)
})

onMounted(async () => {
    loading.value = true
    error.value = ''
    try {
        await loadLayout()
        await loadRealtime()
        realtimeTimer = setInterval(loadRealtime, 5000)
        window.addEventListener('beforeunload', handleBeforeUnload)
    } catch (err) {
        error.value = err?.response?.data?.message || 'Khong tai duoc du lieu campus map.'
    } finally {
        loading.value = false
    }
})

onBeforeUnmount(() => {
    if (realtimeTimer) clearInterval(realtimeTimer)
    if (toastTimer) clearTimeout(toastTimer)
    window.removeEventListener('beforeunload', handleBeforeUnload)
})
</script>

<style scoped>
.campus-layout {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 360px;
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
