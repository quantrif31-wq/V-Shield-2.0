<template>
    <section class="card toolbar-card">
        <div class="toolbar-left">
            <button
                class="btn"
                :class="mode === 'view' ? 'btn-primary' : 'btn-secondary'"
                @click="$emit('change-mode', 'view')"
            >
                Chế độ xem
            </button>
            <button
                class="btn"
                :class="mode === 'edit' ? 'btn-primary' : 'btn-secondary'"
                :disabled="!canEdit"
                @click="$emit('change-mode', 'edit')"
            >
                Chế độ chỉnh sửa
            </button>
        </div>

        <div class="toolbar-right">
            <button class="btn btn-secondary" :disabled="!canEdit || mode !== 'edit'" @click="$emit('auto-arrange')">
                Tự dàn đều
            </button>
            <button class="btn btn-secondary" :disabled="!canEdit || mode !== 'edit'" @click="$emit('reset-layout')">
                Đặt lại bố cục
            </button>
            <button class="btn btn-secondary" @click="$emit('fit-screen')">Vừa màn hình</button>
            <button class="btn btn-secondary" :disabled="refreshing" @click="$emit('refresh')">
                {{ refreshing ? 'Đang tải...' : 'Làm mới' }}
            </button>
            <button class="btn btn-primary" :disabled="!canEdit || mode !== 'edit' || !dirty || saving" @click="$emit('save')">
                {{ saving ? 'Đang lưu...' : 'Lưu bố cục' }}
            </button>
        </div>
    </section>
</template>

<script setup>
defineProps({
    mode: { type: String, default: 'view' },
    canEdit: { type: Boolean, default: false },
    dirty: { type: Boolean, default: false },
    saving: { type: Boolean, default: false },
    refreshing: { type: Boolean, default: false },
})

defineEmits(['change-mode', 'auto-arrange', 'reset-layout', 'fit-screen', 'refresh', 'save'])
</script>

<style scoped>
.toolbar-card {
    padding: 14px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    flex-wrap: wrap;
}

.toolbar-left,
.toolbar-right {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
}
</style>
