<template>
  <div class="incident-map-page">
    <div class="page-header">
      <h2>Bản đồ sự cố</h2>
      <div class="header-actions">
        <button v-if="alarmId" @click="reload" class="btn-reload"><i class="fas fa-sync-alt"></i> Tải lại</button>
      </div>
    </div>

    <div v-if="!alarmId" class="no-alarm">
      <p>Chọn một báo động từ danh sách để xem bản đồ.</p>
      <router-link to="/soc-console" class="btn-link">Đi đến SOC Console</router-link>
    </div>

    <div v-else-if="loading" class="loading-state">
      <i class="fas fa-spinner fa-spin"></i>
      <p>Đang tải thông tin sự cố...</p>
    </div>

    <div v-else-if="alarm" class="map-section">
      <AlarmMap
        :alarm="mapAlarm"
        :from-lat="userLat"
        :from-lng="userLng"
        :building-id="buildingId"
      />
      <div class="indoor-section" v-if="buildingId">
        <h3>Bản đồ trong nhà</h3>
        <IndoorPathViewer
          :building-id="buildingId"
          :target-label="alarm.locationLabel"
        />
      </div>
    </div>

    <div v-else class="error-state">
      <p>Không tìm thấy thông tin sự cố.</p>
    </div>
  </div>
</template>

<script>
import { socApi } from '../services/socApi'
import { notificationApi } from '../services/notificationApi'
import AlarmMap from '../components/maplibre/AlarmMap.vue'
import IndoorPathViewer from '../components/campus-map/IndoorPathViewer.vue'

export default {
  name: 'IncidentMapPage',
  components: { AlarmMap, IndoorPathViewer },
  data() {
    return {
      alarmId: null,
      alarm: null,
      loading: false,
      userLat: 21.0285,
      userLng: 105.8048,
      buildingId: null
    }
  },
  computed: {
    mapAlarm() {
      if (!this.alarm) return null
      return {
        title: `Báo động: ${this.alarm.alarmType}`,
        body: this.alarm.summary,
        latitude: this.alarm.latitude,
        longitude: this.alarm.longitude,
        locationLabel: this.alarm.locationLabel || ''
      }
    }
  },
  async mounted() {
    this.alarmId = this.$route.params.alarmId || this.$route.query.alarmId
    if (this.alarmId) await this.loadAlarm()
  },
  methods: {
    async loadAlarm() {
      this.loading = true
      try {
        const res = await socApi.getAlarm(this.alarmId)
        this.alarm = res.data?.data || res.data
        if (this.alarm?.siteId) {
          this.buildingId = this.alarm.siteId
        }
      } catch (e) {
        console.error('Failed to load alarm', e)
      } finally {
        this.loading = false
      }
    },
    reload() {
      if (this.alarmId) this.loadAlarm()
    }
  }
}
</script>

<style scoped>
.incident-map-page {
  padding: 20px;
  height: calc(100vh - 100px);
  display: flex;
  flex-direction: column;
}
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}
.page-header h2 { margin: 0; font-size: 20px; color: #333; }
.btn-reload {
  padding: 6px 14px;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  background: white;
  cursor: pointer;
  font-size: 13px;
}
.btn-reload:hover { background: #f5f5f5; }
.map-section {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-height: 0;
}
.indoor-section {
  background: white;
  border-radius: 8px;
  padding: 16px;
  box-shadow: 0 1px 4px rgba(0,0,0,0.1);
}
.indoor-section h3 { margin: 0 0 12px; font-size: 16px; color: #333; }
.no-alarm, .loading-state, .error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  flex: 1;
  color: #999;
  gap: 12px;
}
.loading-state i { font-size: 32px; }
.btn-link {
  color: #1976D2;
  text-decoration: underline;
  cursor: pointer;
}
</style>
