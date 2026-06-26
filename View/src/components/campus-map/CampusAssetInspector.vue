<template>
    <section class="card inspector-card">
        <div class="panel-head compact">
            <div>
                <span class="panel-kicker">Inspector</span>
                <h3 class="panel-title">{{ selectedAsset?.label || 'Tong quan mo hinh' }}</h3>
            </div>
            <span class="asset-type">{{ selectedAsset ? typeLabel : 'Scene 3D' }}</span>
        </div>

        <div v-if="selectedAsset" class="inspector-grid">
            <div class="inspector-item">
                <span>Site</span>
                <strong>{{ selectedAsset.siteCode }} • {{ selectedAsset.siteName }}</strong>
            </div>
            <div class="inspector-item" v-if="selectedAsset.floors">
                <span>Tang</span>
                <strong>{{ selectedAsset.floors }}</strong>
            </div>
            <div class="inspector-item" v-if="selectedAsset.dimensions">
                <span>Kich thuoc</span>
                <strong>{{ formatDimensions(selectedAsset.dimensions) }}</strong>
            </div>
            <div class="inspector-item" v-if="selectedAsset.properties?.zone">
                <span>Zone</span>
                <strong>{{ selectedAsset.properties.zone }}</strong>
            </div>
            <div class="inspector-item" v-if="selectedAsset.properties?.level">
                <span>Security level</span>
                <strong>{{ selectedAsset.properties.level }}</strong>
            </div>
            <div class="inspector-item" v-if="selectedAsset.gate">
                <span>Trang thai cong</span>
                <strong :style="{ color: statusColor(selectedAsset.gate.status) }">{{ selectedAsset.gate.status }}</strong>
            </div>
            <div class="inspector-item" v-if="selectedAsset.gate">
                <span>Camera</span>
                <strong>{{ selectedAsset.gate.cameraCount || 0 }} tong • {{ selectedAsset.gate.offlineCameraCount || 0 }} offline</strong>
            </div>
            <div class="inspector-item" v-if="selectedAsset.gate">
                <span>Access 5 phut</span>
                <strong>{{ selectedAsset.gate.recentAccessCount || 0 }} su kien</strong>
            </div>
            <div class="inspector-item" v-if="selectedAsset.gate?.lastAccessAt">
                <span>Truy cap cuoi</span>
                <strong>{{ formatDateTime(selectedAsset.gate.lastAccessAt) }}</strong>
            </div>
        </div>

        <div v-else class="inspector-grid">
            <div class="inspector-item">
                <span>Site</span>
                <strong>{{ summary.siteCount || 0 }}</strong>
            </div>
            <div class="inspector-item">
                <span>3D object</span>
                <strong>{{ summary.objectCount || 0 }}</strong>
            </div>
            <div class="inspector-item">
                <span>Cong active</span>
                <strong>{{ summary.activeGateCount || 0 }}</strong>
            </div>
            <div class="inspector-item">
                <span>Cong canh bao</span>
                <strong>{{ summary.warningGateCount || 0 }}</strong>
            </div>
            <div class="inspector-item">
                <span>Camera offline</span>
                <strong>{{ summary.offlineCameraCount || 0 }}</strong>
            </div>
            <div class="inspector-item">
                <span>Cap nhat</span>
                <strong>{{ updatedAt ? formatDateTime(updatedAt) : 'Chua co' }}</strong>
            </div>
        </div>
    </section>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
    selectedAsset: { type: Object, default: null },
    summary: { type: Object, default: () => ({}) },
    updatedAt: { type: [String, Date], default: null },
})

const typeMap = {
    Building: 'Toa nha',
    GateMarker: 'Cong / lane',
    ParkingArea: 'Bai do xe',
    Path: 'Tuyen ket noi',
    Landmark: 'Canh quan',
}

const typeLabel = computed(() => {
    if (!props.selectedAsset) return 'Scene 3D'
    return typeMap[props.selectedAsset.objectType] || props.selectedAsset.objectType
})

const formatDimensions = (dimensions) => {
    if (!dimensions) return '--'
    return `${Math.round(dimensions.width || 0)}m x ${Math.round(dimensions.length || 0)}m x ${Math.round(dimensions.height || 0)}m`
}

const formatDateTime = (value) => {
    if (!value) return '--'
    return new Date(value).toLocaleString('vi-VN', {
        hour12: false,
        day: '2-digit',
        month: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
    })
}

const statusColor = (status) => {
    if (status === 'Warning') return '#f59e0b'
    if (status === 'Offline') return '#94a3b8'
    if (status === 'Active') return '#38bdf8'
    return '#22c55e'
}
</script>

<style scoped>
.inspector-card {
    padding: 18px;
}

.asset-type {
    color: var(--text-muted);
    font-size: 0.82rem;
}

.inspector-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 12px;
}

.inspector-item {
    display: grid;
    gap: 4px;
    padding: 12px;
    border-radius: 12px;
    background: rgba(15, 23, 42, 0.42);
    border: 1px solid rgba(148, 163, 184, 0.12);
}

.inspector-item span {
    color: var(--text-muted);
    font-size: 0.78rem;
}

.inspector-item strong {
    color: var(--text-primary);
    font-size: 0.92rem;
}
</style>
