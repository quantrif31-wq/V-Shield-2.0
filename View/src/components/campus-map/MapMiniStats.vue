<template>
    <section class="metric-grid">
        <article class="metric-tile">
            <span class="metric-label">Khu vuc dang hoat dong</span>
            <strong class="metric-value">{{ summary.activeGateCount || 0 }}</strong>
            <span class="metric-note">Gate co activity moi trong 5 phut.</span>
        </article>
        <article class="metric-tile">
            <span class="metric-label">Khu vuc canh bao</span>
            <strong class="metric-value">{{ summary.warningGateCount || 0 }}</strong>
            <span class="metric-note">Gate co camera van de hoac su kien canh bao.</span>
        </article>
        <article class="metric-tile">
            <span class="metric-label">Camera offline</span>
            <strong class="metric-value">{{ summary.offlineCameraCount || 0 }}</strong>
            <span class="metric-note">Tinh theo camera thieu stream/view URL.</span>
        </article>
        <article class="metric-tile">
            <span class="metric-label">Su kien 5 phut gan nhat</span>
            <strong class="metric-value">{{ summary.recentEventCount || 0 }}</strong>
            <span class="metric-note">Lay truc tiep tu AccessLog.</span>
        </article>
    </section>
</template>

<script setup>
defineProps({
    summary: {
        type: Object,
        default: () => ({
            activeGateCount: 0,
            warningGateCount: 0,
            offlineCameraCount: 0,
            recentEventCount: 0,
        }),
    },
})
</script>
