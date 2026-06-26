<template>
    <section class="card inspector-card" :class="{ compact }">
        <div class="inspector-topline">
            <span class="panel-kicker">{{ selectedAsset ? 'Focus' : 'Scene' }}</span>
            <span class="asset-type">{{ selectedAsset ? typeLabel : 'Tong quan 3D' }}</span>
        </div>

        <div v-if="selectedAsset" class="asset-hero">
            <div class="asset-orb" :class="orbClass">
                <span></span>
            </div>
            <div class="asset-copy">
                <strong>{{ selectedAsset.label || 'Asset' }}</strong>
                <span>{{ selectedAsset.siteCode }} - {{ selectedAsset.siteName }}</span>
            </div>
        </div>

        <div v-else class="summary-orbit">
            <div class="orbit-node primary">
                <strong>{{ summary.siteCount || 0 }}</strong>
                <span>site</span>
            </div>
            <div class="orbit-node">
                <strong>{{ summary.objectCount || 0 }}</strong>
                <span>object</span>
            </div>
            <div class="orbit-node">
                <strong>{{ summary.activeGateCount || 0 }}</strong>
                <span>active</span>
            </div>
            <div class="orbit-node warn">
                <strong>{{ summary.warningGateCount || 0 }}</strong>
                <span>warn</span>
            </div>
        </div>

        <div v-if="selectedAsset" class="signal-row">
            <div v-if="selectedAsset.metrics" class="signal-pill">
                <strong>{{ selectedAsset.metrics.buildings || 0 }}</strong>
                <span>toa nha</span>
            </div>
            <div v-if="selectedAsset.metrics" class="signal-pill">
                <strong>{{ selectedAsset.metrics.gates || 0 }}</strong>
                <span>cong</span>
            </div>
            <div v-if="selectedAsset.floors" class="signal-pill">
                <strong>{{ selectedAsset.floors }}</strong>
                <span>tang</span>
            </div>
            <div v-if="selectedAsset.gate" class="signal-pill">
                <strong :style="{ color: statusColor(selectedAsset.gate.status) }">{{ selectedAsset.gate.status }}</strong>
                <span>gate</span>
            </div>
        </div>

        <div class="detail-lines">
            <div v-if="selectedAsset?.dimensions" class="detail-line">
                <span>Khoi tich</span>
                <strong>{{ formatDimensions(selectedAsset.dimensions) }}</strong>
            </div>
            <div v-if="selectedAsset?.properties?.zone" class="detail-line">
                <span>Zone</span>
                <strong>{{ selectedAsset.properties.zone }}</strong>
            </div>
            <div v-if="selectedAsset?.properties?.level" class="detail-line">
                <span>Security</span>
                <strong>{{ selectedAsset.properties.level }}</strong>
            </div>
            <div v-if="selectedAsset?.gate" class="detail-line">
                <span>Camera</span>
                <strong>{{ selectedAsset.gate.cameraCount || 0 }} / {{ selectedAsset.gate.offlineCameraCount || 0 }} offline</strong>
            </div>
            <div v-if="selectedAsset?.gate" class="detail-line">
                <span>Access 5p</span>
                <strong>{{ selectedAsset.gate.recentAccessCount || 0 }}</strong>
            </div>
            <div v-if="selectedAsset?.gate?.lastAccessAt" class="detail-line">
                <span>Cuoi cung</span>
                <strong>{{ formatDateTime(selectedAsset.gate.lastAccessAt) }}</strong>
            </div>
            <div v-if="!selectedAsset" class="detail-line">
                <span>Offline cam</span>
                <strong>{{ summary.offlineCameraCount || 0 }}</strong>
            </div>
            <div v-if="!selectedAsset" class="detail-line">
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
    compact: { type: Boolean, default: false },
})

const typeMap = {
    Site: 'Site',
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

const orbClass = computed(() => {
    const type = props.selectedAsset?.objectType
    if (type === 'GateMarker') return 'gate'
    if (type === 'Site') return 'site'
    if (type === 'Building') return 'building'
    return 'generic'
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
    padding: 16px;
    background: rgba(5, 14, 24, 0.78);
    border: 1px solid rgba(125, 211, 252, 0.14);
    backdrop-filter: blur(16px);
}

.compact {
    box-shadow: 0 20px 50px rgba(2, 8, 23, 0.34);
}

.inspector-topline {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
}

.asset-type {
    color: var(--text-muted);
    font-size: 0.82rem;
}

.asset-hero {
    display: flex;
    align-items: center;
    gap: 14px;
    margin-top: 14px;
}

.asset-orb {
    position: relative;
    width: 52px;
    height: 52px;
    border-radius: 999px;
    background: radial-gradient(circle at 35% 35%, rgba(255, 255, 255, 0.95), rgba(56, 189, 248, 0.18));
    border: 1px solid rgba(125, 211, 252, 0.26);
    box-shadow: 0 0 24px rgba(56, 189, 248, 0.24);
    flex-shrink: 0;
}

.asset-orb span {
    position: absolute;
    inset: 11px;
    border-radius: inherit;
    border: 1px solid rgba(255, 255, 255, 0.25);
}

.asset-orb.gate {
    box-shadow: 0 0 26px rgba(15, 118, 110, 0.3);
}

.asset-orb.site {
    box-shadow: 0 0 26px rgba(59, 130, 246, 0.3);
}

.asset-orb.building {
    box-shadow: 0 0 26px rgba(14, 165, 233, 0.28);
}

.asset-copy {
    display: grid;
    gap: 4px;
}

.asset-copy strong {
    color: #f8fafc;
    font-size: 0.96rem;
}

.asset-copy span {
    color: #7dd3fc;
    font-size: 0.8rem;
}

.summary-orbit {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px;
    margin-top: 14px;
}

.orbit-node {
    display: grid;
    gap: 2px;
    padding: 12px;
    border-radius: 14px;
    background: radial-gradient(circle at top, rgba(56, 189, 248, 0.1), rgba(15, 23, 42, 0.64));
    border: 1px solid rgba(148, 163, 184, 0.12);
}

.orbit-node.primary {
    background: radial-gradient(circle at top, rgba(56, 189, 248, 0.18), rgba(15, 23, 42, 0.74));
}

.orbit-node.warn {
    background: radial-gradient(circle at top, rgba(245, 158, 11, 0.16), rgba(15, 23, 42, 0.72));
}

.orbit-node strong {
    color: #f8fafc;
    font-size: 1.1rem;
}

.orbit-node span {
    color: #94a3b8;
    font-size: 0.74rem;
    text-transform: uppercase;
    letter-spacing: 0.08em;
}

.signal-row {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-top: 14px;
}

.signal-pill {
    display: grid;
    gap: 2px;
    min-width: 74px;
    padding: 9px 10px;
    border-radius: 999px;
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid rgba(148, 163, 184, 0.14);
    text-align: center;
}

.signal-pill strong {
    color: #f8fafc;
    font-size: 0.84rem;
}

.signal-pill span {
    color: #94a3b8;
    font-size: 0.68rem;
    text-transform: uppercase;
    letter-spacing: 0.08em;
}

.detail-lines {
    display: grid;
    gap: 8px;
    margin-top: 14px;
}

.detail-line {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 10px 12px;
    border-radius: 12px;
    background: rgba(15, 23, 42, 0.42);
    border: 1px solid rgba(148, 163, 184, 0.1);
}

.detail-line span {
    color: #94a3b8;
    font-size: 0.76rem;
}

.detail-line strong {
    color: #e2e8f0;
    font-size: 0.82rem;
    text-align: right;
}

@media (max-width: 900px) {
    .inspector-card.compact {
        padding: 14px;
    }
}
</style>
