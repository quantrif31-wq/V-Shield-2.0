<template>
    <section class="card canvas-card">
        <div v-if="!items.length" class="empty-card">Chua co Gate nao de hien thi tren ban do.</div>
        <div v-else ref="viewportRef" class="canvas-viewport">
            <div class="canvas-grid" :style="canvasStyle">
                <CampusMapElement
                    v-for="item in sortedItems"
                    :key="item.gateId"
                    :item="item"
                    :selected="item.gateId === selectedGateId"
                    :editable="editable"
                    @select="$emit('select', $event)"
                    @drag-start="startDrag"
                    @resize-start="startResize"
                />
            </div>
        </div>
    </section>
</template>

<script setup>
import { computed, onBeforeUnmount, ref } from 'vue'
import CampusMapElement from './CampusMapElement.vue'

const MIN_WIDTH = 120
const MIN_HEIGHT = 70

const props = defineProps({
    items: { type: Array, default: () => [] },
    selectedGateId: { type: Number, default: null },
    editable: { type: Boolean, default: false },
})

const emit = defineEmits(['select', 'update-layout'])

const viewportRef = ref(null)
const interaction = ref(null)

const sortedItems = computed(() =>
    [...props.items].sort((a, b) => Number(a.layout.zIndex || 0) - Number(b.layout.zIndex || 0))
)

const canvasStyle = computed(() => {
    const visibleItems = props.items.filter((x) => x.layout.isVisible !== false)
    const maxRight = Math.max(
        1400,
        ...visibleItems.map((item) => Number(item.layout.x || 0) + Number(item.layout.w || 220) + 120)
    )
    const maxBottom = Math.max(
        860,
        ...visibleItems.map((item) => Number(item.layout.y || 0) + Number(item.layout.h || 120) + 120)
    )

    return {
        width: `${Math.ceil(maxRight)}px`,
        height: `${Math.ceil(maxBottom)}px`,
    }
})

const startDrag = ({ gateId, event }) => {
    if (!props.editable) return
    const item = props.items.find((x) => x.gateId === gateId)
    if (!item || item.layout.isLocked) return

    interaction.value = {
        type: 'drag',
        gateId,
        startPointerX: event.clientX,
        startPointerY: event.clientY,
        startX: Number(item.layout.x || 0),
        startY: Number(item.layout.y || 0),
        startW: Number(item.layout.w || 220),
        startH: Number(item.layout.h || 120),
    }

    bindPointerEvents()
}

const startResize = ({ gateId, event }) => {
    if (!props.editable) return
    const item = props.items.find((x) => x.gateId === gateId)
    if (!item || item.layout.isLocked) return

    interaction.value = {
        type: 'resize',
        gateId,
        startPointerX: event.clientX,
        startPointerY: event.clientY,
        startX: Number(item.layout.x || 0),
        startY: Number(item.layout.y || 0),
        startW: Number(item.layout.w || 220),
        startH: Number(item.layout.h || 120),
    }

    bindPointerEvents()
}

const onPointerMove = (event) => {
    if (!interaction.value) return

    const dx = event.clientX - interaction.value.startPointerX
    const dy = event.clientY - interaction.value.startPointerY

    if (interaction.value.type === 'drag') {
        emit('update-layout', {
            gateId: interaction.value.gateId,
            x: Math.max(0, interaction.value.startX + dx),
            y: Math.max(0, interaction.value.startY + dy),
        })
        return
    }

    emit('update-layout', {
        gateId: interaction.value.gateId,
        w: Math.max(MIN_WIDTH, interaction.value.startW + dx),
        h: Math.max(MIN_HEIGHT, interaction.value.startH + dy),
    })
}

const onPointerUp = () => {
    interaction.value = null
    unbindPointerEvents()
}

const bindPointerEvents = () => {
    window.addEventListener('pointermove', onPointerMove)
    window.addEventListener('pointerup', onPointerUp, { once: true })
}

const unbindPointerEvents = () => {
    window.removeEventListener('pointermove', onPointerMove)
    window.removeEventListener('pointerup', onPointerUp)
}

const fitToContent = () => {
    if (!viewportRef.value || !props.items.length) return

    const minX = Math.min(...props.items.map((x) => Number(x.layout.x || 0)))
    const minY = Math.min(...props.items.map((x) => Number(x.layout.y || 0)))
    const maxX = Math.max(...props.items.map((x) => Number(x.layout.x || 0) + Number(x.layout.w || 220)))
    const maxY = Math.max(...props.items.map((x) => Number(x.layout.y || 0) + Number(x.layout.h || 120)))

    const boxW = maxX - minX
    const boxH = maxY - minY
    const targetX = Math.max(0, minX - (viewportRef.value.clientWidth - boxW) / 2)
    const targetY = Math.max(0, minY - (viewportRef.value.clientHeight - boxH) / 2)

    viewportRef.value.scrollTo({
        left: targetX,
        top: targetY,
        behavior: 'smooth',
    })
}

onBeforeUnmount(() => {
    unbindPointerEvents()
})

defineExpose({
    fitToContent,
})
</script>

<style scoped>
.canvas-card {
    padding: 12px;
    min-height: 540px;
}

.canvas-viewport {
    width: 100%;
    min-height: 520px;
    max-height: 70vh;
    overflow: auto;
    border-radius: 14px;
    border: 1px solid var(--border-color);
    background: linear-gradient(0deg, rgba(9, 27, 37, 0.96), rgba(15, 38, 52, 0.96));
}

.canvas-grid {
    position: relative;
    background-image:
        linear-gradient(rgba(174, 237, 245, 0.08) 1px, transparent 1px),
        linear-gradient(90deg, rgba(174, 237, 245, 0.08) 1px, transparent 1px);
    background-size: 32px 32px;
}
</style>
