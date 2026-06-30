<template>
  <div class="indoor-viewer">
    <div v-if="loading" class="loading">Đang tải bản đồ trong nhà...</div>
    <div v-else-if="nodes.length === 0" class="empty">Chưa có dữ liệu bản đồ trong nhà cho tòa nhà này.</div>
    <div v-else class="floor-plan">
      <div class="floor-selector">
        <label>Tầng:</label>
        <select v-model="selectedFloor" @change="onFloorChange">
          <option v-for="floor in floors" :key="floor" :value="floor">{{ floor }}</option>
        </select>
      </div>
      <div class="floor-canvas" ref="floorCanvas">
        <div
          v-for="node in filteredNodes"
          :key="node.id"
          class="path-node"
          :class="getNodeClass(node)"
          :style="getNodeStyle(node)"
          :title="node.label"
        >
          <span class="node-label">{{ node.label }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import http from '../../services/http'

export default {
  name: 'IndoorPathViewer',
  props: {
    buildingId: { type: Number, default: null },
    targetLabel: { type: String, default: '' }
  },
  data() {
    return {
      nodes: [],
      floors: [],
      selectedFloor: '',
      loading: false
    }
  },
  computed: {
    filteredNodes() {
      const floorId = this.getFloorId(this.selectedFloor)
      return this.nodes.filter(n => n.facilityFloorId === floorId || !floorId)
    }
  },
  async mounted() {
    if (this.buildingId) await this.loadNodes()
  },
  methods: {
    async loadNodes() {
      this.loading = true
      try {
        const res = await http.get(`/indoor-map/nodes?buildingId=${this.buildingId}`)
        this.nodes = res.data?.data || []
        const floorSet = new Set(this.nodes.map(n => n.facilityFloorName).filter(Boolean))
        this.floors = [...floorSet].sort()
        if (this.floors.length > 0) this.selectedFloor = this.floors[0]
      } catch (e) {
        console.error('Failed to load indoor nodes', e)
      } finally {
        this.loading = false
      }
    },
    getFloorId(name) {
      const node = this.nodes.find(n => n.facilityFloorName === name)
      return node?.facilityFloorId || null
    },
    onFloorChange() {},
    getNodeClass(node) {
      return {
        'node-entrance': node.nodeType === 'Entrance',
        'node-stair': node.nodeType === 'Stair',
        'node-elevator': node.nodeType === 'Elevator',
        'node-room': node.nodeType === 'Room',
        'node-corridor': node.nodeType === 'Corridor',
        'node-target': this.targetLabel && node.label === this.targetLabel
      }
    },
    getNodeStyle(node) {
      const scale = 2
      return {
        left: `${node.x * scale + 50}px`,
        top: `${node.z * scale + 50}px`
      }
    }
  }
}
</script>

<style scoped>
.indoor-viewer {
  background: #f8f9fa;
  border-radius: 6px;
  padding: 12px;
  min-height: 300px;
}
.loading, .empty { text-align: center; color: #999; padding: 40px; }
.floor-selector {
  margin-bottom: 12px;
  display: flex;
  align-items: center;
  gap: 8px;
}
.floor-selector select {
  padding: 6px 10px;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  font-size: 13px;
}
.floor-canvas {
  position: relative;
  min-height: 280px;
  background: white;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  overflow: hidden;
}
.path-node {
  position: absolute;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 10px;
  white-space: nowrap;
  cursor: pointer;
  transform: translate(-50%, -50%);
}
.node-entrance { background: #22c55e; color: white; }
.node-stair { background: #f59e0b; color: white; }
.node-elevator { background: #6366f1; color: white; }
.node-room { background: #e2e8f0; color: #333; border: 1px solid #cbd5e1; }
.node-corridor { background: transparent; border: 1px dashed #94a3b8; }
.node-target { background: #dc2626 !important; color: white !important; animation: pulse 1.5s infinite; }
.node-label { pointer-events: none; }
@keyframes pulse {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.1); }
}
</style>
