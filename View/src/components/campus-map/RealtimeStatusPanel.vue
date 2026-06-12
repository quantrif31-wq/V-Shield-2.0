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
            <article v-for="event in recentEvents.slice(0, 8)" :key="event.logId" class="surface-item">
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

.scroll-zone {
    max-height: 360px;
    overflow-y: auto;
}
</style>
