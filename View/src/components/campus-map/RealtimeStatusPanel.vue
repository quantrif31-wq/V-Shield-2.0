<template>
    <section class="card realtime-panel" :class="{ compact }">
        <div class="panel-head compact-head">
            <div>
                <span class="panel-kicker">Realtime</span>
                <h3 class="panel-title">Dong su kien</h3>
            </div>
            <span class="updated-at">{{ updatedLabel }}</span>
        </div>

        <div v-if="error" class="empty-card">{{ error }}</div>
        <div v-else-if="!recentEvents.length" class="empty-card">Chua co hoat dong gan day.</div>
        <div v-else-if="compact" class="event-orbit">
            <button
                v-for="event in compactEvents"
                :key="event.logId"
                class="event-dot"
                :class="statusClass(event.resultStatus)"
                :title="eventTitle(event)"
                @click="$emit('focus-gate', event.gateId)"
            >
                <span class="dot-core"></span>
                <span class="dot-time">{{ formatTime(event.timestamp) }}</span>
            </button>
        </div>
        <div v-else class="surface-list scroll-zone">
            <article
                v-for="event in recentEvents.slice(0, 8)"
                :key="event.logId"
                class="surface-item event-card"
                @click="$emit('focus-gate', event.gateId)"
            >
                <strong>{{ event.gateName || 'Gate khong xac dinh' }}</strong>
                <p class="event-meta">
                    {{ formatDateTime(event.timestamp) }} - {{ event.direction || 'N/A' }}
                    <span v-if="event.cameraName">- {{ event.cameraName }}</span>
                </p>
                <p class="event-sub">
                    <span v-if="event.actorName">{{ event.actorName }}</span>
                    <span v-if="event.capturedLicensePlate">- {{ event.capturedLicensePlate }}</span>
                    <span v-if="event.resultStatus">- {{ event.resultStatus }}</span>
                </p>
                <span class="event-status" :class="statusClass(event.resultStatus)">{{ event.resultStatus || 'Unknown' }}</span>
            </article>
        </div>
    </section>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
    updatedAt: { type: [String, Date], default: null },
    recentEvents: { type: Array, default: () => [] },
    error: { type: String, default: '' },
    compact: { type: Boolean, default: false },
})

defineEmits(['focus-gate'])

const compactEvents = computed(() => props.recentEvents.slice(0, 6))

const updatedLabel = computed(() => {
    if (!props.updatedAt) return 'Chua cap nhat'
    return `Cap nhat: ${formatDateTime(props.updatedAt)}`
})

const formatDateTime = (value) => {
    if (!value) return '--'
    return new Date(value).toLocaleString('vi-VN', {
        hour12: false,
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        day: '2-digit',
        month: '2-digit',
    })
}

const formatTime = (value) => {
    if (!value) return '--'
    return new Date(value).toLocaleTimeString('vi-VN', {
        hour12: false,
        hour: '2-digit',
        minute: '2-digit',
    })
}

const eventTitle = (event) => {
    const gate = event.gateName || 'Gate'
    const actor = event.actorName ? ` - ${event.actorName}` : ''
    const result = event.resultStatus ? ` - ${event.resultStatus}` : ''
    return `${gate}${actor}${result}`
}

const statusClass = (status) => {
    const normalized = String(status || '').toUpperCase()
    if (['APPROVED', 'SUCCESS', 'GRANTED', 'OK', 'MATCHED'].includes(normalized)) return 'ok'
    if (['DENIED', 'REJECTED', 'FAILED', 'BLOCKED'].includes(normalized)) return 'danger'
    return 'warn'
}
</script>

<style scoped>
.realtime-panel {
    padding: 16px;
    background: rgba(5, 14, 24, 0.78);
    border: 1px solid rgba(125, 211, 252, 0.14);
    backdrop-filter: blur(16px);
}

.compact {
    box-shadow: 0 20px 50px rgba(2, 8, 23, 0.34);
}

.compact-head {
    align-items: center;
}

.updated-at {
    color: var(--text-muted);
    font-size: 0.8rem;
}

.event-orbit {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-top: 14px;
    overflow-x: auto;
    padding-bottom: 4px;
}

.event-dot {
    position: relative;
    display: grid;
    place-items: center;
    width: 58px;
    min-width: 58px;
    height: 58px;
    border-radius: 999px;
    background: rgba(15, 23, 42, 0.72);
    border: 1px solid rgba(148, 163, 184, 0.14);
    cursor: pointer;
    transition: transform 0.18s ease, border-color 0.18s ease, box-shadow 0.18s ease;
}

.event-dot:hover {
    transform: translateY(-2px) scale(1.03);
}

.dot-core {
    width: 18px;
    height: 18px;
    border-radius: 999px;
}

.dot-time {
    position: absolute;
    bottom: -18px;
    color: #94a3b8;
    font-size: 0.68rem;
}

.event-dot.ok .dot-core {
    background: #22c55e;
    box-shadow: 0 0 18px rgba(34, 197, 94, 0.42);
}

.event-dot.warn .dot-core {
    background: #f59e0b;
    box-shadow: 0 0 18px rgba(245, 158, 11, 0.42);
}

.event-dot.danger .dot-core {
    background: #ef4444;
    box-shadow: 0 0 18px rgba(239, 68, 68, 0.42);
}

.event-dot.ok:hover {
    border-color: rgba(34, 197, 94, 0.38);
}

.event-dot.warn:hover {
    border-color: rgba(245, 158, 11, 0.38);
}

.event-dot.danger:hover {
    border-color: rgba(239, 68, 68, 0.38);
}

.event-meta {
    margin-top: 6px;
    color: var(--text-secondary);
    font-size: 0.86rem;
}

.event-sub {
    margin-top: 4px;
    color: var(--text-muted);
    font-size: 0.82rem;
}

.event-card {
    position: relative;
    cursor: pointer;
    transition: transform 0.18s ease, border-color 0.18s ease;
}

.event-card:hover {
    transform: translateY(-1px);
    border-color: rgba(56, 189, 248, 0.28);
}

.event-status {
    display: inline-flex;
    margin-top: 8px;
    padding: 4px 8px;
    border-radius: 999px;
    font-size: 0.74rem;
    font-weight: 700;
    width: fit-content;
}

.event-status.ok {
    background: rgba(34, 197, 94, 0.14);
    color: #22c55e;
}

.event-status.warn {
    background: rgba(245, 158, 11, 0.14);
    color: #f59e0b;
}

.event-status.danger {
    background: rgba(239, 68, 68, 0.14);
    color: #ef4444;
}

.scroll-zone {
    max-height: 360px;
    overflow-y: auto;
}
</style>
