<template>
    <article
        v-if="item.layout.isVisible"
        class="map-element"
        :class="[
            `status-${(item.status || 'Normal').toLowerCase()}`,
            { selected, locked: item.layout.isLocked, editable },
        ]"
        :style="elementStyle"
        @pointerdown="onDragStart"
        @click.stop="$emit('select', item.gateId)"
    >
        <header class="element-head">
            <span class="element-icon">{{ iconGlyph }}</span>
            <strong>{{ item.gateName }}</strong>
        </header>
        <p class="element-location">{{ item.location || 'Chưa có vị trí' }}</p>
        <div class="element-foot">
            <span>{{ item.stats?.cameraCount || 0 }} camera</span>
            <span>{{ statusLabel }}</span>
        </div>
        <button
            v-if="editable && !item.layout.isLocked"
            type="button"
            class="resize-handle"
            aria-label="Kéo giãn"
            @pointerdown.stop="onResizeStart"
        />
    </article>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
    item: { type: Object, required: true },
    selected: { type: Boolean, default: false },
    editable: { type: Boolean, default: false },
})

const emit = defineEmits(['select', 'drag-start', 'resize-start'])

const elementStyle = computed(() => ({
    left: `${Number(props.item.layout.x || 0)}px`,
    top: `${Number(props.item.layout.y || 0)}px`,
    width: `${Number(props.item.layout.w || 220)}px`,
    height: `${Number(props.item.layout.h || 120)}px`,
    zIndex: Number(props.item.layout.zIndex || 1),
    background: props.item.layout.color || '#0f766e',
}))

const statusLabel = computed(() => {
    const map = {
        Offline: 'Offline',
        Warning: 'Cảnh báo',
        Active: 'Đang hoạt động',
        Normal: 'Bình thường',
    }
    return map[props.item.status] || props.item.status || 'Bình thường'
})

const iconGlyph = computed(() => {
    const icon = (props.item.layout.icon || 'gate').toLowerCase()
    if (icon.includes('camera')) return 'CAM'
    if (icon.includes('door')) return 'DOOR'
    return 'GATE'
})

const onDragStart = (event) => {
    if (!props.editable || props.item.layout.isLocked) return
    emit('drag-start', { gateId: props.item.gateId, event })
}

const onResizeStart = (event) => {
    if (!props.editable || props.item.layout.isLocked) return
    emit('resize-start', { gateId: props.item.gateId, event })
}
</script>

<style scoped>
.map-element {
    position: absolute;
    border-radius: 16px;
    padding: 10px 12px;
    color: #ecfeff;
    box-shadow: 0 12px 30px rgba(7, 16, 27, 0.28);
    border: 2px solid transparent;
    display: flex;
    flex-direction: column;
    gap: 6px;
    user-select: none;
}

.map-element.editable {
    cursor: grab;
}

.map-element.editable:active {
    cursor: grabbing;
}

.map-element.selected {
    border-color: #dffefb;
}

.map-element.locked {
    opacity: 0.75;
}

.element-head {
    display: flex;
    align-items: center;
    gap: 8px;
}

.element-head strong {
    font-size: 0.95rem;
    line-height: 1.2;
}

.element-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 40px;
    height: 24px;
    padding: 0 8px;
    border-radius: 999px;
    font-size: 0.66rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    background: rgba(255, 255, 255, 0.2);
}

.element-location {
    color: rgba(255, 255, 255, 0.9);
    font-size: 0.82rem;
}

.element-foot {
    margin-top: auto;
    display: flex;
    justify-content: space-between;
    gap: 8px;
    font-size: 0.76rem;
    color: rgba(255, 255, 255, 0.92);
}

.resize-handle {
    position: absolute;
    right: 4px;
    bottom: 4px;
    width: 14px;
    height: 14px;
    border-radius: 3px;
    border: 1px solid rgba(255, 255, 255, 0.9);
    background: rgba(255, 255, 255, 0.22);
    cursor: nwse-resize;
}

.status-offline {
    box-shadow: 0 0 0 2px rgba(255, 196, 196, 0.45), 0 12px 30px rgba(7, 16, 27, 0.28);
}

.status-warning {
    box-shadow: 0 0 0 2px rgba(255, 226, 154, 0.46), 0 12px 30px rgba(7, 16, 27, 0.28);
}

.status-active {
    box-shadow: 0 0 0 2px rgba(155, 255, 203, 0.45), 0 12px 30px rgba(7, 16, 27, 0.28);
}
</style>
