<template>
    <section class="metric-grid">
        <article class="metric-tile">
            <span class="metric-label">Site trong mo hinh</span>
            <strong class="metric-value">{{ summary.siteCount || 0 }}</strong>
            <span class="metric-note">So khuon vien dang duoc mo phong trong canh 3D.</span>
        </article>
        <article class="metric-tile">
            <span class="metric-label">Doi tuong 3D</span>
            <strong class="metric-value">{{ summary.objectCount || 0 }}</strong>
            <span class="metric-note">Tong building, gate, path, parking va landmark.</span>
        </article>
        <article class="metric-tile">
            <span class="metric-label">Cong dang hoat dong</span>
            <strong class="metric-value">{{ summary.activeGateCount || 0 }}</strong>
            <span class="metric-note">Gate co activity moi trong 5 phut gan nhat.</span>
        </article>
        <article class="metric-tile">
            <span class="metric-label">Cong canh bao</span>
            <strong class="metric-value">{{ summary.warningGateCount || 0 }}</strong>
            <span class="metric-note">Co camera van de hoac su kien canh bao.</span>
        </article>
        <article class="metric-tile">
            <span class="metric-label">Camera offline</span>
            <strong class="metric-value">{{ summary.offlineCameraCount || 0 }}</strong>
            <span class="metric-note">Tinh theo camera thieu stream hoac URL xem.</span>
        </article>
        <article class="metric-tile">
            <span class="metric-label">Su kien 5 phut</span>
            <strong class="metric-value">{{ summary.recentEventCount || 0 }}</strong>
            <span class="metric-note">Lay truc tiep tu luong AccessLog realtime.</span>
        </article>
    </section>
</template>

<script setup>
defineProps({
    summary: {
        type: Object,
        default: () => ({
            siteCount: 0,
            objectCount: 0,
            activeGateCount: 0,
            warningGateCount: 0,
            offlineCameraCount: 0,
            recentEventCount: 0,
        }),
    },
})
</script>
