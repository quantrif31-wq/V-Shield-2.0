<template>
    <section class="card realtime-panel">
        <div class="panel-head compact">
            <div>
                <span class="panel-kicker">Realtime</span>
                <h3 class="panel-title">Su kien gan day</h3>
            </div>
            <span class="updated-at">{{ updatedLabel }}</span>
        </div>

        <div v-if="error" class="empty-card">{{ error }}</div>
        <div v-else-if="!recentEvents.length" class="empty-card">Chua co hoat dong gan day.</div>
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
})

defineEmits(['focus-gate'])

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

const statusClass = (status) => {
    const normalized = String(status || '').toUpperCase()
    if (['APPROVED', 'SUCCESS', 'GRANTED', 'OK', 'MATCHED'].includes(normalized)) return 'ok'
    if (['DENIED', 'REJECTED', 'FAILED', 'BLOCKED'].includes(normalized)) return 'danger'
    return 'warn'
}
</script>

<style scoped>
.realtime-panel {
    padding: 18px;
}

.updated-at {
    color: var(--text-muted);
    font-size: 0.86rem;
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
