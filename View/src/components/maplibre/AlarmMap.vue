<template>
  <div class="alarm-map-container">
    <div ref="mapContainer" class="map-container"></div>
    <div v-if="alarm" class="alarm-info-panel">
      <h4>{{ alarm.title || 'Vị trí báo động' }}</h4>
      <p v-if="alarm.locationLabel" class="location-label">{{ alarm.locationLabel }}</p>
      <p v-if="alarm.body" class="alarm-body">{{ alarm.body }}</p>
      <div class="alarm-coords" v-if="alarm.latitude">
        <span>{{ alarm.latitude.toFixed(6) }}, {{ alarm.longitude.toFixed(6) }}</span>
      </div>
      <div class="route-info" v-if="routeData">
        <p><strong>Khoảng cách:</strong> {{ formatDistance(routeData.totalDistanceMeters) }}</p>
        <p><strong>Thời gian:</strong> {{ formatDuration(routeData.totalDurationSeconds) }}</p>
        <p v-if="routeData.targetBuildingName"><strong>Tòa nhà:</strong> {{ routeData.targetBuildingName }}</p>
        <p v-if="routeData.targetFloorLabel"><strong>Tầng:</strong> {{ routeData.targetFloorLabel }}</p>
      </div>
      <div class="alarm-actions">
        <button v-if="!routeData" @click="showRoute" class="btn-route" :disabled="loading">
          {{ loading ? 'Đang tính...' : 'Chỉ đường' }}
        </button>
        <button v-if="routeData && alarm.latitude" @click="openInMaps" class="btn-maps">
          Mở Google Maps
        </button>
      </div>
    </div>
  </div>
</template>

<script>
import maplibregl from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import { routingApi } from '../../services/routingApi'
import { captureError, recordMetric } from '../../services/observability'

export default {
  name: 'AlarmMap',
  props: {
    alarm: { type: Object, default: null },
    fromLat: { type: Number, default: null },
    fromLng: { type: Number, default: null },
    buildingId: { type: Number, default: null },
    targetNodeId: { type: Number, default: null }
  },
  data() {
    return {
      map: null,
      marker: null,
      routeLayer: null,
      routeData: null,
      loading: false
    }
  },
  mounted() {
    this.initMap()
  },
  beforeUnmount() {
    if (this.map) this.map.remove()
  },
  methods: {
    initMap() {
      if (!this.alarm?.latitude) return
      const startedAt = performance.now()
      try {
        this.map = new maplibregl.Map({
          container: this.$refs.mapContainer,
          style: 'https://basemaps.cartocdn.com/gl/positron-gl-style/style.json',
          center: [this.alarm.longitude, this.alarm.latitude],
          zoom: 16,
          attributionControl: false
        })
      } catch (error) {
        captureError(error, 'map_initialization_failure', { component: 'AlarmMap' })
        throw error
      }

      this.map.addControl(new maplibregl.NavigationControl(), 'top-right')

      this.map.on('load', () => {
        recordMetric('map_initialization', performance.now() - startedAt, { component: 'AlarmMap' })
        this.addAlarmMarker()
        this.fitBounds()
      })
      this.map.on('error', event => captureError(event?.error || 'Map error', 'map_initialization_failure', { component: 'AlarmMap' }))
    },
    addAlarmMarker() {
      if (!this.map || !this.alarm?.latitude) return

      const el = document.createElement('div')
      el.className = 'alarm-marker'
      el.innerHTML = '<svg width="32" height="32" viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="10" fill="#dc2626" stroke="white" stroke-width="2"/><path d="M12 8v4M12 16h0" stroke="white" stroke-width="2" stroke-linecap="round"/></svg>'
      el.style.width = '32px'
      el.style.height = '32px'
      el.style.cursor = 'pointer'

      this.marker = new maplibregl.Marker({ element: el })
        .setLngLat([this.alarm.longitude, this.alarm.latitude])
        .setPopup(new maplibregl.Popup({ offset: 25 }).setHTML(`
          <div style="font-weight:600;font-size:14px;">${this.alarm.title || 'Báo động'}</div>
          <div style="font-size:12px;color:#666;">${this.alarm.locationLabel || ''}</div>
        `))
        .addTo(this.map)
    },
    fitBounds() {
      if (!this.map || !this.alarm?.latitude) return

      const bounds = new maplibregl.LngLatBounds()
      bounds.extend([this.alarm.longitude, this.alarm.latitude])
      if (this.fromLat && this.fromLng) {
        bounds.extend([this.fromLng, this.fromLat])
      }
      this.map.fitBounds(bounds, { padding: 80, maxZoom: 16 })
    },
    async showRoute() {
      this.loading = true
      try {
        const res = await routingApi.getRoute({
          fromLat: this.fromLat || 21.0285,
          fromLng: this.fromLng || 105.8048,
          toLat: this.alarm.latitude,
          toLng: this.alarm.longitude,
          buildingId: this.buildingId,
          targetNodeId: this.targetNodeId
        })
        this.routeData = res.data?.data

        if (this.routeData?.outdoorGeoJson) {
          this.addRouteLine(this.routeData.outdoorGeoJson)
        }
      } catch (e) {
        console.error('Route failed', e)
      } finally {
        this.loading = false
      }
    },
    addRouteLine(geoJson) {
      if (!this.map) return

      if (this.routeLayer) {
        this.map.removeLayer('route-line')
        this.map.removeSource('route-source')
      }

      const geojson = typeof geoJson === 'string' ? JSON.parse(geoJson) : geoJson

      this.map.addSource('route-source', {
        type: 'geojson',
        data: {
          type: 'Feature',
          properties: {},
          geometry: geojson
        }
      })

      this.map.addLayer({
        id: 'route-line',
        type: 'line',
        source: 'route-source',
        paint: {
          'line-color': '#2563eb',
          'line-width': 4,
          'line-opacity': 0.8
        }
      })

      this.fitBounds()
    },
    openInMaps() {
      if (!this.alarm?.latitude) return
      const url = `https://www.google.com/maps/dir/?api=1&destination=${this.alarm.latitude},${this.alarm.longitude}`
      window.open(url, '_blank')
    },
    formatDistance(m) {
      if (!m) return ''
      return m >= 1000 ? `${(m / 1000).toFixed(1)} km` : `${Math.round(m)} m`
    },
    formatDuration(s) {
      if (!s) return ''
      const min = Math.round(s / 60)
      if (min >= 60) return `${Math.floor(min / 60)}h ${min % 60}ph`
      return `${min} phút`
    }
  }
}
</script>

<style scoped>
.alarm-map-container {
  position: relative;
  width: 100%;
  height: 100%;
  min-height: 400px;
  border-radius: 8px;
  overflow: hidden;
}
.map-container {
  width: 100%;
  height: 100%;
  min-height: 400px;
}
.alarm-info-panel {
  position: absolute;
  top: 12px;
  left: 12px;
  background: white;
  padding: 14px 18px;
  border-radius: 10px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.15);
  max-width: 280px;
  z-index: 10;
}
.alarm-info-panel h4 {
  margin: 0 0 4px;
  font-size: 15px;
  color: #dc2626;
}
.location-label {
  font-size: 12px;
  color: #666;
  margin: 2px 0;
}
.alarm-body {
  font-size: 13px;
  color: #333;
  margin: 4px 0;
}
.alarm-coords {
  font-size: 11px;
  color: #999;
  margin: 4px 0;
}
.route-info {
  background: #f0f7ff;
  border-radius: 6px;
  padding: 8px 10px;
  margin: 8px 0;
  font-size: 12px;
}
.route-info p {
  margin: 2px 0;
}
.alarm-actions {
  display: flex;
  gap: 8px;
  margin-top: 8px;
}
.btn-route, .btn-maps {
  flex: 1;
  padding: 8px 12px;
  border: none;
  border-radius: 6px;
  font-size: 13px;
  cursor: pointer;
  font-weight: 500;
  transition: all 0.2s;
}
.btn-route {
  background: var(--interactive-primary);
  color: var(--text-on-interactive);
}
.btn-route:hover { background: var(--interactive-primary-hover); }
.btn-route:disabled { background: var(--interactive-disabled); cursor: not-allowed; }
.btn-maps {
  background: var(--surface-subtle);
  color: var(--text-primary);
  border: 1px solid var(--border-subtle);
}
.btn-maps:hover { background: var(--surface-hover); }
</style>
