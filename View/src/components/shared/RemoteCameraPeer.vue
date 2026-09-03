<template>
  <div class="remote-camera-peer">
    <video ref="video" class="remote-camera-peer__video" autoplay muted playsinline></video>
    <div v-if="state !== 'live'" class="remote-camera-peer__state">
      {{ state === 'failed' ? message : 'Đang kết nối camera local…' }}
    </div>
  </div>
</template>

<script>
import { closeCameraPeer, openCameraPeer } from '../../services/cameraPeerRelay'

export default {
  name: 'RemoteCameraPeer',
  emits: ['state-change'],
  props: {
    nodeId: { type: String, required: true },
    streamName: { type: String, required: true },
  },
  data: () => ({ state: 'connecting', message: '', peer: null }),
  methods: {
    setState(state, message = '') {
      this.state = state
      this.message = message
      this.$emit('state-change', { state, message })
    },
  },
  async mounted() {
    try {
      this.peer = await openCameraPeer({
        nodeId: this.nodeId,
        streamName: this.streamName,
        onStream: (stream) => {
          if (this.$refs.video) this.$refs.video.srcObject = stream
          this.setState('live')
        },
        onState: (next, detail) => {
          if (next === 'failed') this.setState(next, detail || 'Không thể kết nối camera local.')
        },
      })
    } catch (error) {
      this.setState('failed', error?.message || 'Không thể kết nối camera local.')
    }
  },
  beforeUnmount() { if (this.peer) closeCameraPeer(this.peer) },
}
</script>

<style scoped>
.remote-camera-peer { width: 100%; height: 100%; position: relative; background: #05080d; }
.remote-camera-peer__video { display: block; width: 100%; height: 100%; object-fit: cover; background: #05080d; }
.remote-camera-peer__state { position: absolute; inset: 0; display: grid; place-items: center; padding: 16px; color: #d6e4ed; font-weight: 600; text-align: center; }
</style>
