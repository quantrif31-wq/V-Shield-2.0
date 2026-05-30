<template>
    <section class="card properties-panel">
        <div v-if="!item" class="empty-card">Chon mot Gate tren ban do de xem chi tiet.</div>
        <template v-else>
            <div class="panel-head compact">
                <div>
                    <span class="panel-kicker">Gate details</span>
                    <h3 class="panel-title">{{ item.gateName }}</h3>
                </div>
            </div>

            <div class="surface-list">
                <div class="surface-item">
                    <div class="inline-stat">
                        <strong>{{ item.location || 'Chua co location' }}</strong>
                        <span>Vi tri</span>
                    </div>
                </div>
                <div class="surface-item">
                    <div class="inline-stat">
                        <strong>{{ statusLabel(item.status) }}</strong>
                        <span>Trang thai realtime</span>
                    </div>
                </div>
                <div class="surface-item">
                    <div class="inline-stat">
                        <strong>{{ item.stats?.cameraCount || 0 }} / {{ item.stats?.offlineCameraCount || 0 }}</strong>
                        <span>Camera / offline</span>
                    </div>
                </div>
                <div class="surface-item">
                    <div class="inline-stat">
                        <strong>{{ item.stats?.recentAccessCount || 0 }}</strong>
                        <span>Su kien 5 phut</span>
                    </div>
                </div>
            </div>

            <form v-if="editable" class="layout-form" @submit.prevent>
                <h4>Chinh layout</h4>
                <div class="form-row">
                    <div class="form-group">
                        <label>X</label>
                        <input :value="item.layout.x" type="number" step="1" @change="patchNumber('x', $event.target.value)" />
                    </div>
                    <div class="form-group">
                        <label>Y</label>
                        <input :value="item.layout.y" type="number" step="1" @change="patchNumber('y', $event.target.value)" />
                    </div>
                </div>
                <div class="form-row">
                    <div class="form-group">
                        <label>W</label>
                        <input :value="item.layout.w" type="number" min="120" step="1" @change="patchNumber('w', $event.target.value)" />
                    </div>
                    <div class="form-group">
                        <label>H</label>
                        <input :value="item.layout.h" type="number" min="70" step="1" @change="patchNumber('h', $event.target.value)" />
                    </div>
                </div>
                <div class="form-row">
                    <div class="form-group">
                        <label>Z-Index</label>
                        <input :value="item.layout.zIndex" type="number" step="1" @change="patchNumber('zIndex', $event.target.value)" />
                    </div>
                    <div class="form-group">
                        <label>Icon</label>
                        <input :value="item.layout.icon || ''" type="text" @change="patchText('icon', $event.target.value)" />
                    </div>
                </div>
                <div class="form-group">
                    <label>Mau</label>
                    <input :value="item.layout.color || '#0f766e'" type="color" @input="patchText('color', $event.target.value)" />
                </div>
                <div class="form-row">
                    <label class="switch-row">
                        <input
                            type="checkbox"
                            :checked="item.layout.isVisible"
                            @change="$emit('patch-layout', { gateId: item.gateId, isVisible: $event.target.checked })"
                        />
                        Hien thi
                    </label>
                    <label class="switch-row">
                        <input
                            type="checkbox"
                            :checked="item.layout.isLocked"
                            @change="$emit('patch-layout', { gateId: item.gateId, isLocked: $event.target.checked })"
                        />
                        Khoa item
                    </label>
                </div>
            </form>
        </template>
    </section>
</template>

<script setup>
defineProps({
    item: { type: Object, default: null },
    editable: { type: Boolean, default: false },
})

const emit = defineEmits(['patch-layout'])

const statusLabel = (status) => {
    const map = {
        Offline: 'Offline',
        Warning: 'Canh bao',
        Active: 'Dang hoat dong',
        Normal: 'Binh thuong',
    }
    return map[status] || status || 'Binh thuong'
}

const patchNumber = (field, rawValue) => {
    const numberValue = Number(rawValue)
    if (Number.isNaN(numberValue)) return
    emit('patch-layout', { [field]: numberValue })
}

const patchText = (field, rawValue) => {
    emit('patch-layout', { [field]: rawValue })
}
</script>

<style scoped>
.properties-panel {
    padding: 18px;
}

.layout-form {
    margin-top: 16px;
    padding-top: 14px;
    border-top: 1px solid var(--border-color);
}

.layout-form h4 {
    font-family: var(--font-heading);
    font-size: 1rem;
    margin-bottom: 12px;
}

.switch-row {
    min-height: 46px;
    display: inline-flex;
    align-items: center;
    gap: 8px;
    font-size: 0.9rem;
    color: var(--text-secondary);
}

.switch-row input {
    width: 16px;
    height: 16px;
}
</style>
