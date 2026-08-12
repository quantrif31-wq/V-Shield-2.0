<template>
  <section class="spatial-workspace">
    <div class="spatial-overview">
      <div>
        <span class="section-kicker">Hạ tầng không gian</span>
        <h2>Điều khiển hạ tầng 3D</h2>
        <p>
          Quản lý đối tượng 3D thực tế, bản đồ khu vực và vị trí thiết bị từ một không gian làm việc duy nhất,
          biến mô hình khuôn viên thành tài nguyên vận hành thay vì mô hình tĩnh trình diễn.
        </p>
      </div>
      <div class="spatial-actions">
        <select v-model.number="selectedSiteId" class="form-select">
          <option :value="0">Tất cả khu vực</option>
          <option v-for="site in siteOptions" :key="site.siteId" :value="site.siteId">{{ site.name }}</option>
        </select>
        <button type="button" class="btn btn-secondary" :disabled="loadingScene" @click="loadWorkspace">Làm mới dữ liệu không gian</button>
      </div>
    </div>

    <div class="spatial-stat-grid">
      <article class="spatial-stat-card">
        <span>Khu vực hiển thị</span>
        <strong>{{ filteredSceneSites.length }}</strong>
      </article>
      <article class="spatial-stat-card">
        <span>Đối tượng 3D</span>
        <strong>{{ filteredObjects.length }}</strong>
      </article>
      <article class="spatial-stat-card">
        <span>Bản đồ khu vực</span>
        <strong>{{ siteMaps.length }}</strong>
      </article>
      <article class="spatial-stat-card highlight">
        <span>Vị trí</span>
        <strong>{{ placements.length }}</strong>
      </article>
    </div>

    <div v-if="workspaceError" class="empty-card">{{ workspaceError }}</div>

    <div class="spatial-layout">
      <aside class="spatial-sidebar card-shell">
        <div class="panel-section-header compact">
          <div>
            <span class="section-kicker">Đối tượng</span>
            <h3>Danh mục hiện trường 3D</h3>
          </div>
          <button type="button" class="btn btn-xs btn-primary" @click="startNewObject()">+ Đối tượng mới</button>
        </div>

        <div class="sidebar-toolbar">
          <select v-model="objectTypeFilter" class="form-select">
            <option value="all">Tất cả loại đối tượng</option>
            <option v-for="option in objectTypeOptions" :key="option" :value="option">{{ option }}</option>
          </select>
          <input v-model="objectSearch" class="form-input" placeholder="Tìm nhãn đối tượng..." />
        </div>

        <div v-if="!filteredObjects.length" class="empty-card compact">Chưa có đối tượng 3D nào trong bộ lọc này.</div>
        <div v-else class="object-list">
          <button
            v-for="item in filteredObjects"
            :key="item.id"
            type="button"
            class="object-item"
            :class="{ active: selectedObjectId === item.id }"
            @click="selectObject(item)"
          >
            <div>
              <strong>{{ item.label }}</strong>
              <span>{{ item.type }} • Khu vực {{ resolveSiteName(item.siteId) }}</span>
            </div>
            <span class="soft-chip" :class="item.isActive ? 'success' : 'muted'">{{ item.isActive ? 'Đang hoạt động' : 'Ngừng' }}</span>
          </button>
        </div>
      </aside>

      <main class="spatial-main">
        <div class="canvas-shell card-shell">
          <div class="panel-section-header compact">
            <div>
              <span class="section-kicker">Xem trước</span>
              <h3>Mô hình 3D vận hành</h3>
            </div>
            <div class="toolbar-actions">
              <button type="button" class="btn btn-xs btn-secondary" @click="focusSelectedSite">Tập trung khu vực</button>
              <button type="button" class="btn btn-xs btn-secondary" @click="fitScene">Vừa khung</button>
            </div>
          </div>

          <Campus3DCanvas
            ref="canvasRef"
            :sites="filteredSceneSites"
            :gates="gateStatuses"
            :recent-events="recentEvents"
            :selected-gate-id="null"
            @inspect-object="handleInspectObject"
          />
        </div>

        <div class="editor-grid">
          <section class="card-shell form-card">
            <div class="panel-section-header compact">
              <div>
                <span class="section-kicker">Trình biên soạn</span>
                <h3>{{ objectForm.id ? 'Cập nhật đối tượng 3D' : 'Tạo đối tượng 3D' }}</h3>
              </div>
              <span class="soft-chip">{{ objectForm.objectType || 'Đối tượng' }}</span>
            </div>

            <div class="form-grid two">
              <label>
                Khu vực
                <select v-model.number="objectForm.siteId" class="form-select">
                  <option v-for="site in siteOptions" :key="site.siteId" :value="site.siteId">{{ site.name }}</option>
                </select>
              </label>
              <label>
                Loại
                <select v-model="objectForm.objectType" class="form-select">
                  <option v-for="option in objectTypeOptions" :key="option" :value="option">{{ option }}</option>
                </select>
              </label>
              <label>
                Nhãn
                <input v-model="objectForm.label" class="form-input" placeholder="Tòa nhà chính, cổng xe tải..." />
              </label>
              <label>
                Màu
                <input v-model="objectForm.color" class="form-input" placeholder="#0f766e" />
              </label>
              <label>
                Chiều rộng
                <input v-model.number="objectForm.width" type="number" step="0.1" class="form-input" />
              </label>
              <label>
                Chiều dài
                <input v-model.number="objectForm.length" type="number" step="0.1" class="form-input" />
              </label>
              <label>
                Chiều cao
                <input v-model.number="objectForm.height" type="number" step="0.1" class="form-input" />
              </label>
              <label>
                Số tầng
                <input v-model.number="objectForm.floors" type="number" min="0" class="form-input" />
              </label>
            </div>

            <div class="coordinate-grid">
              <label>
                X
                <input v-model.number="objectForm.positionX" type="number" step="0.1" class="form-input" />
              </label>
              <label>
                Z
                <input v-model.number="objectForm.positionZ" type="number" step="0.1" class="form-input" />
              </label>
              <label>
                Y
                <input v-model.number="objectForm.positionY" type="number" step="0.1" class="form-input" />
              </label>
              <label>
                Xoay
                <input v-model.number="objectForm.rotation" type="number" step="1" class="form-input" />
              </label>
            </div>

            <div class="nudge-toolbar">
              <button type="button" class="btn btn-xs btn-secondary" @click="nudgeObject('positionX', -2)">X -2</button>
              <button type="button" class="btn btn-xs btn-secondary" @click="nudgeObject('positionX', 2)">X +2</button>
              <button type="button" class="btn btn-xs btn-secondary" @click="nudgeObject('positionZ', -2)">Z -2</button>
              <button type="button" class="btn btn-xs btn-secondary" @click="nudgeObject('positionZ', 2)">Z +2</button>
              <button type="button" class="btn btn-xs btn-secondary" @click="nudgeObject('width', 1)">Rộng +1</button>
              <button type="button" class="btn btn-xs btn-secondary" @click="nudgeObject('length', 1)">Dài +1</button>
            </div>

            <div class="form-grid two">
              <label>
                Mã tòa nhà
                <input v-model="objectMeta.buildingId" class="form-input" placeholder="Không bắt buộc" />
              </label>
              <label>
                Mã khu
                <input v-model="objectMeta.zoneId" class="form-input" placeholder="Không bắt buộc" />
              </label>
              <label>
                Mã điểm truy cập
                <input v-model="objectMeta.accessPointId" class="form-input" placeholder="Không bắt buộc" />
              </label>
              <label>
                Mức bảo mật
                <select v-model="objectMeta.level" class="form-select">
                  <option value="">Không</option>
                  <option value="Normal">Bình thường</option>
                  <option value="Restricted">Hạn chế</option>
                  <option value="Critical">Nghiêm trọng</option>
                </select>
              </label>
              <label class="wide-field">
                Nhãn khu
                <input v-model="objectMeta.zone" class="form-input" placeholder="Khu văn phòng, logistics, SOC..." />
              </label>
              <label class="wide-field">
                Ghi chú
                <textarea v-model="objectMeta.note" class="form-input form-textarea" rows="3" placeholder="Ghi chú vận hành cho đối tượng này"></textarea>
              </label>
            </div>

            <label class="toggle-row">
              <input v-model="objectForm.isActive" type="checkbox" />
              <span>Đối tượng đang hoạt động trong hiện trường trực tiếp</span>
            </label>

            <div v-if="objectMessage" class="inline-message" :class="objectMessageType">{{ objectMessage }}</div>

            <div class="editor-actions">
              <button type="button" class="btn btn-primary" :disabled="savingObject" @click="saveObject">
                {{ savingObject ? 'Đang lưu...' : (objectForm.id ? 'Lưu đối tượng' : 'Tạo đối tượng') }}
              </button>
              <button type="button" class="btn btn-secondary" @click="startNewObject(objectForm.objectType)">Đặt lại biểu mẫu</button>
              <button v-if="objectForm.id" type="button" class="btn btn-danger" :disabled="deletingObject" @click="removeObject">
                {{ deletingObject ? 'Đang xóa...' : 'Xóa đối tượng' }}
              </button>
            </div>
          </section>

          <section class="card-shell form-card">
            <div class="panel-section-header compact">
              <div>
                <span class="section-kicker">Bản đồ</span>
                <h3>Bản đồ khu vực & vị trí</h3>
              </div>
              <button type="button" class="btn btn-xs btn-primary" @click="startNewMap">+ Bản đồ mới</button>
            </div>

            <div class="map-list">
              <button
                v-for="map in siteMaps"
                :key="map.siteMapId"
                type="button"
                class="map-item"
                :class="{ active: selectedMapId === map.siteMapId }"
                @click="selectMap(map)"
              >
                <div>
                  <strong>{{ map.name }}</strong>
                  <span>{{ map.coordinateSystem }} • {{ map.assetReference }}</span>
                </div>
                <span class="soft-chip" :class="map.isActive ? 'success' : 'muted'">{{ map.isActive ? 'Đang hoạt động' : 'Ngừng' }}</span>
              </button>
            </div>

            <div class="form-grid two compact-top">
              <label>
                Tên bản đồ
                <input v-model="mapForm.name" class="form-input" placeholder="Tầng trệt, bản đồ lối đi trong khuôn viên..." />
              </label>
              <label>
                Hệ tọa độ
                <select v-model="mapForm.coordinateSystem" class="form-select">
                  <option value="Normalized">Chuẩn hóa</option>
                  <option value="Absolute">Tuyệt đối</option>
                </select>
              </label>
              <label class="wide-field">
                Tham chiếu tài sản
                <input v-model="mapForm.assetReference" class="form-input" placeholder="Tham chiếu kho lưu trữ hoặc bản vẽ (không bắt buộc)" />
              </label>
            </div>

            <label class="toggle-row">
              <input v-model="mapForm.isActive" type="checkbox" />
              <span>Bản đồ đang hoạt động cho công tác vận hành</span>
            </label>

            <div class="editor-actions compact-top">
              <button type="button" class="btn btn-primary" :disabled="savingMap" @click="saveMap">
                {{ savingMap ? 'Đang lưu...' : (mapForm.id ? 'Lưu bản đồ' : 'Tạo bản đồ') }}
              </button>
              <button type="button" class="btn btn-secondary" @click="startNewMap">Đặt lại bản đồ</button>
              <button v-if="mapForm.id" type="button" class="btn btn-danger" :disabled="deletingMap" @click="removeMap">
                {{ deletingMap ? 'Đang xóa...' : 'Xóa bản đồ' }}
              </button>
            </div>

            <div class="panel-section-header compact compact-top">
              <div>
                <span class="section-kicker">Vị trí</span>
                <h3>{{ selectedMapId ? 'Gắn thiết bị vào bản đồ đã chọn' : 'Hãy chọn bản đồ trước' }}</h3>
              </div>
            </div>

            <div v-if="selectedMapId" class="form-grid two">
              <label>
                Thiết bị
                <select v-model.number="placementForm.securityDeviceId" class="form-select">
                  <option :value="null">Chọn thiết bị</option>
                  <option v-for="device in siteDevices" :key="device.securityDeviceId" :value="device.securityDeviceId">
                    {{ device.name }} • {{ device.deviceType }}
                  </option>
                </select>
              </label>
              <label>
                Loại biểu tượng
                <select v-model="placementForm.iconType" class="form-select">
                  <option value="Device">Thiết bị</option>
                  <option value="Camera">Camera</option>
                  <option value="Gate">Cổng</option>
                  <option value="Reader">Đầu đọc</option>
                </select>
              </label>
              <label>
                X
                <input v-model.number="placementForm.x" type="number" step="0.1" class="form-input" />
              </label>
              <label>
                Y
                <input v-model.number="placementForm.y" type="number" step="0.1" class="form-input" />
              </label>
            </div>

            <div v-if="selectedMapId" class="editor-actions compact-top">
              <button type="button" class="btn btn-primary" :disabled="savingPlacement" @click="savePlacement">
                {{ savingPlacement ? 'Đang lưu...' : (placementForm.id ? 'Lưu vị trí' : 'Thêm vị trí') }}
              </button>
              <button type="button" class="btn btn-secondary" @click="resetPlacement">Đặt lại vị trí</button>
              <button v-if="placementForm.id" type="button" class="btn btn-danger" :disabled="deletingPlacement" @click="removePlacement">
                {{ deletingPlacement ? 'Đang xóa...' : 'Xóa vị trí' }}
              </button>
            </div>

            <div v-if="placements.length" class="placement-list">
              <button
                v-for="placement in placements"
                :key="placement.mapDevicePlacementId"
                type="button"
                class="placement-item"
                :class="{ active: placementForm.id === placement.mapDevicePlacementId }"
                @click="selectPlacement(placement)"
              >
                <div>
                  <strong>{{ placement.securityDeviceName || placement.cameraName || `Vị trí #${placement.mapDevicePlacementId}` }}</strong>
                  <span>{{ placement.iconType }} • ({{ placement.x }}, {{ placement.y }})</span>
                </div>
              </button>
            </div>
            <div v-else-if="selectedMapId" class="empty-card compact">Chưa có vị trí nào trên bản đồ này.</div>
          </section>
        </div>
      </main>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import Campus3DCanvas from '../campus-map/Campus3DCanvas.vue'
import { createCampusSceneObject, deleteCampusSceneObject, getCampusMapRealtime, getCampusScene3D, updateCampusSceneObject } from '../../services/campusMapApi'
import { enterpriseApi } from '../../services/enterpriseSecurityApi'

const props = defineProps({
  siteOptions: {
    type: Array,
    default: () => [],
  },
  preferredSiteId: {
    type: Number,
    default: null,
  },
})

const canvasRef = ref(null)
const loadingScene = ref(false)
const workspaceError = ref('')
const selectedSiteId = ref(0)
const sceneSites = ref([])
const gateStatuses = ref([])
const recentEvents = ref([])
const objectSearch = ref('')
const objectTypeFilter = ref('all')
const selectedObjectId = ref(null)
const savingObject = ref(false)
const deletingObject = ref(false)
const objectMessage = ref('')
const objectMessageType = ref('info')
const siteMaps = ref([])
const selectedMapId = ref(null)
const placements = ref([])
const siteDevices = ref([])
const savingMap = ref(false)
const deletingMap = ref(false)
const savingPlacement = ref(false)
const deletingPlacement = ref(false)

const objectTypeOptions = ['Building', 'GateMarker', 'ParkingArea', 'Path', 'Landmark']

const objectForm = reactive({
  id: null,
  siteId: 0,
  objectType: 'Building',
  label: '',
  positionX: 0,
  positionY: 0,
  positionZ: 0,
  width: 24,
  length: 16,
  height: 8,
  floors: 1,
  rotation: 0,
  color: '#2563eb',
  isActive: true,
})

const objectMeta = reactive({
  buildingId: '',
  zoneId: '',
  accessPointId: '',
  zone: '',
  level: '',
  note: '',
})

const mapForm = reactive({
  id: null,
  name: '',
  coordinateSystem: 'Normalized',
  assetReference: '',
  isActive: true,
})

const placementForm = reactive({
  id: null,
  securityDeviceId: null,
  x: 50,
  y: 50,
  iconType: 'Device',
})

const filteredSceneSites = computed(() => {
  if (!selectedSiteId.value) return sceneSites.value
  return sceneSites.value.filter((site) => site.siteId === selectedSiteId.value)
})

const filteredObjects = computed(() => {
  const lower = objectSearch.value.trim().toLowerCase()
  return filteredSceneSites.value
    .flatMap((site) => (site.objects || []).map((item) => ({ ...item, siteId: site.siteId })))
    .filter((item) => objectTypeFilter.value === 'all' || item.type === objectTypeFilter.value)
    .filter((item) => !lower || item.label.toLowerCase().includes(lower))
})

watch(() => props.preferredSiteId, (value) => {
  if (value && !selectedSiteId.value) selectedSiteId.value = value
}, { immediate: true })

watch(selectedSiteId, async () => {
  if (!selectedSiteId.value && props.siteOptions.length === 1) {
    selectedSiteId.value = props.siteOptions[0].siteId
    return
  }
  objectForm.siteId = selectedSiteId.value || props.siteOptions[0]?.siteId || 0
  await Promise.all([loadSiteMaps(), loadDevices()])
  resetPlacement()
}, { immediate: true })

async function loadWorkspace() {
  loadingScene.value = true
  workspaceError.value = ''
  try {
    const [sceneRes, realtimeRes] = await Promise.all([getCampusScene3D(), getCampusMapRealtime()])
    sceneSites.value = Array.isArray(sceneRes.data?.sites) ? sceneRes.data.sites : []
    gateStatuses.value = Array.isArray(sceneRes.data?.gates) ? sceneRes.data.gates : []
    if (Array.isArray(realtimeRes.data?.gates)) gateStatuses.value = realtimeRes.data.gates
    recentEvents.value = Array.isArray(realtimeRes.data?.recentEvents) ? realtimeRes.data.recentEvents : []
    await Promise.all([loadSiteMaps(), loadDevices()])
  } catch (error) {
    workspaceError.value = error.response?.data?.message || 'Không thể tải dữ liệu hạ tầng không gian.'
  } finally {
    loadingScene.value = false
  }
}

async function loadSiteMaps() {
  try {
    const params = selectedSiteId.value ? { siteId: selectedSiteId.value } : undefined
    const res = await enterpriseApi.getSiteMaps(params)
    siteMaps.value = Array.isArray(res.data) ? res.data : []

    if (selectedMapId.value && !siteMaps.value.some((map) => map.siteMapId === selectedMapId.value)) {
      selectedMapId.value = null
      placements.value = []
      resetPlacement()
    }
    if (selectedMapId.value) await loadPlacements()
  } catch {
    siteMaps.value = []
  }
}

async function loadDevices() {
  try {
    const res = await enterpriseApi.getTopology()
    const rows = Array.isArray(res.data) ? res.data : []
    siteDevices.value = selectedSiteId.value
      ? rows.filter((item) => item.siteId === selectedSiteId.value)
      : rows
  } catch {
    siteDevices.value = []
  }
}

async function loadPlacements() {
  if (!selectedMapId.value) {
    placements.value = []
    return
  }
  try {
    const res = await enterpriseApi.getMapPlacements(selectedMapId.value)
    placements.value = Array.isArray(res.data) ? res.data : []
  } catch {
    placements.value = []
  }
}

function resolveSiteName(siteId) {
  return props.siteOptions.find((site) => site.siteId === siteId)?.name || siteId || 'Không xác định'
}

function focusSelectedSite() {
  if (selectedSiteId.value) {
    canvasRef.value?.focusSite?.(selectedSiteId.value)
  } else {
    canvasRef.value?.fitToContent?.()
  }
}

function fitScene() {
  canvasRef.value?.fitToContent?.()
}

function handleInspectObject(payload) {
  if (!payload?.label) return
  const match = filteredObjects.value.find((item) => item.label === payload.label && item.type === payload.objectType)
  if (match) selectObject(match)
}

function parseProperties(raw) {
  if (!raw) return {}
  if (typeof raw === 'object') return raw
  try {
    return JSON.parse(raw)
  } catch {
    return {}
  }
}

function applyObjectMeta(propsValue = {}) {
  objectMeta.buildingId = propsValue.buildingId ? String(propsValue.buildingId) : ''
  objectMeta.zoneId = propsValue.zoneId ? String(propsValue.zoneId) : ''
  objectMeta.accessPointId = propsValue.accessPointId ? String(propsValue.accessPointId) : ''
  objectMeta.zone = propsValue.zone || ''
  objectMeta.level = propsValue.level || ''
  objectMeta.note = propsValue.note || ''
}

function startNewObject(type = 'Building') {
  selectedObjectId.value = null
  objectForm.id = null
  objectForm.siteId = selectedSiteId.value || props.siteOptions[0]?.siteId || 0
  objectForm.objectType = type
  objectForm.label = ''
  objectForm.positionX = 0
  objectForm.positionY = 0
  objectForm.positionZ = 0
  objectForm.width = type === 'Path' ? 3 : 24
  objectForm.length = type === 'Path' ? 12 : 16
  objectForm.height = type === 'Path' ? 0.3 : 8
  objectForm.floors = type === 'Building' ? 1 : null
  objectForm.rotation = 0
  objectForm.color = type === 'GateMarker' ? '#0f766e' : '#2563eb'
  objectForm.isActive = true
  applyObjectMeta({})
  objectMessage.value = ''
}

function selectObject(item) {
  selectedObjectId.value = item.id
  objectForm.id = item.id
  objectForm.siteId = item.siteId
  objectForm.objectType = item.type
  objectForm.label = item.label
  objectForm.positionX = Number(item.posX || 0)
  objectForm.positionY = Number(item.posY || 0)
  objectForm.positionZ = Number(item.posZ || 0)
  objectForm.width = Number(item.width || 0)
  objectForm.length = Number(item.length || 0)
  objectForm.height = Number(item.height || 0)
  objectForm.floors = item.floors ?? null
  objectForm.rotation = Number(item.rotation || 0)
  objectForm.color = item.color || '#2563eb'
  objectForm.isActive = item.isActive !== false
  applyObjectMeta(parseProperties(item.properties))
  objectMessage.value = ''
}

function buildPropertiesJson() {
  const payload = {
    buildingId: objectMeta.buildingId || undefined,
    zoneId: objectMeta.zoneId || undefined,
    accessPointId: objectMeta.accessPointId || undefined,
    zone: objectMeta.zone || undefined,
    level: objectMeta.level || undefined,
    note: objectMeta.note || undefined,
  }
  const cleaned = Object.fromEntries(Object.entries(payload).filter(([, value]) => value !== undefined && value !== ''))
  return Object.keys(cleaned).length ? JSON.stringify(cleaned) : null
}

function nudgeObject(field, delta) {
  objectForm[field] = Number(objectForm[field] || 0) + delta
}

async function saveObject() {
  if (!objectForm.siteId || !objectForm.label.trim()) {
    objectMessage.value = 'Khu vực và nhãn đối tượng là bắt buộc.'
    objectMessageType.value = 'error'
    return
  }

  savingObject.value = true
  objectMessage.value = ''
  const payload = {
    siteId: objectForm.siteId,
    objectType: objectForm.objectType,
    label: objectForm.label.trim(),
    positionX: Number(objectForm.positionX || 0),
    positionY: Number(objectForm.positionY || 0),
    positionZ: Number(objectForm.positionZ || 0),
    width: Number(objectForm.width || 0),
    length: Number(objectForm.length || 0),
    height: Number(objectForm.height || 0),
    floors: objectForm.floors === '' ? null : objectForm.floors,
    rotation: Number(objectForm.rotation || 0),
    color: objectForm.color || null,
    propertiesJson: buildPropertiesJson(),
    isActive: objectForm.isActive,
  }

  try {
    let savedId = objectForm.id
    if (objectForm.id) {
      await updateCampusSceneObject(objectForm.id, payload)
    } else {
      const res = await createCampusSceneObject(payload)
      savedId = res.data?.id || null
    }
    await loadWorkspace()
    if (savedId) {
      const saved = filteredObjects.value.find((item) => item.id === savedId)
      if (saved) selectObject(saved)
    }
    objectMessage.value = objectForm.id ? 'Đã cập nhật đối tượng 3D.' : 'Đã tạo đối tượng 3D.'
    objectMessageType.value = 'success'
  } catch (error) {
    objectMessage.value = error.response?.data?.message || 'Không thể lưu đối tượng 3D.'
    objectMessageType.value = 'error'
  } finally {
    savingObject.value = false
  }
}

async function removeObject() {
  if (!objectForm.id) return
  deletingObject.value = true
  try {
    await deleteCampusSceneObject(objectForm.id)
    startNewObject(objectForm.objectType)
    await loadWorkspace()
    objectMessage.value = 'Đã xóa đối tượng 3D.'
    objectMessageType.value = 'success'
  } catch (error) {
    objectMessage.value = error.response?.data?.message || 'Không thể xóa đối tượng 3D.'
    objectMessageType.value = 'error'
  } finally {
    deletingObject.value = false
  }
}

function startNewMap() {
  selectedMapId.value = null
  mapForm.id = null
  mapForm.name = ''
  mapForm.coordinateSystem = 'Normalized'
  mapForm.assetReference = ''
  mapForm.isActive = true
  placements.value = []
  resetPlacement()
}

function selectMap(map) {
  selectedMapId.value = map.siteMapId
  mapForm.id = map.siteMapId
  mapForm.name = map.name
  mapForm.coordinateSystem = map.coordinateSystem || 'Normalized'
  mapForm.assetReference = map.assetReference || ''
  mapForm.isActive = map.isActive !== false
  loadPlacements()
  resetPlacement()
}

async function saveMap() {
  if (!mapForm.name.trim()) return
  savingMap.value = true
  const payload = {
    siteId: selectedSiteId.value || null,
    name: mapForm.name.trim(),
    assetReference: mapForm.assetReference.trim() || null,
    coordinateSystem: mapForm.coordinateSystem,
    isActive: mapForm.isActive,
  }
  try {
    if (mapForm.id) {
      await enterpriseApi.updateSiteMap(mapForm.id, payload)
    } else {
      const res = await enterpriseApi.createSiteMap(payload)
      selectedMapId.value = res.data?.siteMapId || null
    }
    await loadSiteMaps()
  } finally {
    savingMap.value = false
  }
}

async function removeMap() {
  if (!mapForm.id) return
  deletingMap.value = true
  try {
    await enterpriseApi.deleteSiteMap(mapForm.id)
    startNewMap()
    await loadSiteMaps()
  } finally {
    deletingMap.value = false
  }
}

function resetPlacement() {
  placementForm.id = null
  placementForm.securityDeviceId = null
  placementForm.x = 50
  placementForm.y = 50
  placementForm.iconType = 'Device'
}

function selectPlacement(placement) {
  placementForm.id = placement.mapDevicePlacementId
  placementForm.securityDeviceId = placement.securityDeviceId || null
  placementForm.x = Number(placement.x || 0)
  placementForm.y = Number(placement.y || 0)
  placementForm.iconType = placement.iconType || 'Device'
}

async function savePlacement() {
  if (!selectedMapId.value || !placementForm.securityDeviceId) return
  savingPlacement.value = true
  const payload = {
    securityDeviceId: placementForm.securityDeviceId,
    x: Number(placementForm.x || 0),
    y: Number(placementForm.y || 0),
    iconType: placementForm.iconType,
  }
  try {
    if (placementForm.id) {
      await enterpriseApi.updateMapPlacement(selectedMapId.value, placementForm.id, payload)
    } else {
      await enterpriseApi.addMapPlacement(selectedMapId.value, payload)
    }
    await loadPlacements()
    resetPlacement()
  } finally {
    savingPlacement.value = false
  }
}

async function removePlacement() {
  if (!selectedMapId.value || !placementForm.id) return
  deletingPlacement.value = true
  try {
    await enterpriseApi.deleteMapPlacement(selectedMapId.value, placementForm.id)
    await loadPlacements()
    resetPlacement()
  } finally {
    deletingPlacement.value = false
  }
}

onMounted(async () => {
  if (!selectedSiteId.value && props.siteOptions.length === 1) {
    selectedSiteId.value = props.siteOptions[0].siteId
  }
  startNewObject()
  await loadWorkspace()
})
</script>

<style scoped>
.spatial-workspace {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.spatial-overview,
.card-shell {
  border-radius: 24px;
  border: 1px solid var(--border-subtle);
  background: var(--surface-default);
  box-shadow: var(--shadow-md);
}

.spatial-overview {
  display: flex;
  justify-content: space-between;
  gap: 18px;
  padding: 22px;
}

.spatial-overview h2 {
  margin: 4px 0 8px;
  color: var(--text-primary);
}

.spatial-overview p {
  margin: 0;
  color: var(--text-secondary);
  line-height: 1.6;
  max-width: 760px;
}

.spatial-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: start;
}

.spatial-stat-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
}

.spatial-stat-card {
  padding: 16px 18px;
  border-radius: 20px;
  background: var(--surface-default);
  border: 1px solid var(--border-subtle);
  box-shadow: var(--shadow-sm);
}

.spatial-stat-card span {
  color: var(--text-muted);
  font-size: 0.82rem;
}

.spatial-stat-card strong {
  display: block;
  margin-top: 8px;
  color: var(--text-primary);
  font-size: 1.35rem;
}

.spatial-stat-card.highlight {
  background: linear-gradient(135deg, #0f766e 0%, #155e75 100%);
}

.spatial-stat-card.highlight span,
.spatial-stat-card.highlight strong {
  color: #f8fafc;
}

.spatial-layout {
  display: grid;
  grid-template-columns: 320px minmax(0, 1fr);
  gap: 18px;
}

.spatial-sidebar,
.spatial-main {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.card-shell {
  padding: 18px;
}

.sidebar-toolbar,
.toolbar-actions,
.editor-actions,
.nudge-toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.object-list,
.map-list,
.placement-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.object-item,
.map-item,
.placement-item {
  display: flex;
  justify-content: space-between;
  align-items: start;
  gap: 12px;
  padding: 12px 14px;
  border-radius: 16px;
  border: 1px solid var(--border-subtle);
  background: var(--surface-subtle);
  cursor: pointer;
  text-align: left;
  transition: border-color var(--transition-fast), background var(--transition-fast), box-shadow var(--transition-fast);
}

.object-item:hover,
.map-item:hover,
.placement-item:hover {
  border-color: var(--border-default);
  background: var(--surface-hover);
}

.object-item.active,
.map-item.active,
.placement-item.active {
  border-color: rgba(15, 118, 110, 0.26);
  background: linear-gradient(135deg, rgba(15, 118, 110, 0.12), rgba(21, 94, 117, 0.06));
}

.object-item strong,
.map-item strong,
.placement-item strong {
  display: block;
  color: var(--text-primary);
}

.object-item span,
.map-item span,
.placement-item span {
  color: var(--text-muted);
  font-size: 0.8rem;
}

.canvas-shell {
  padding-bottom: 12px;
}

.editor-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 18px;
}

.form-card {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.form-grid.two,
.coordinate-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.coordinate-grid {
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

label {
  display: grid;
  gap: 8px;
  color: var(--text-secondary);
  font-size: 0.88rem;
}

.wide-field {
  grid-column: 1 / -1;
}

.form-input,
.form-select,
.form-textarea {
  width: 100%;
  min-height: 42px;
  padding: 0 14px;
  border-radius: 14px;
  border: 1px solid var(--border-default);
  background: var(--surface-subtle);
  color: var(--text-primary);
}

.form-textarea {
  min-height: 96px;
  padding-top: 12px;
  resize: vertical;
}

.toggle-row {
  display: inline-flex;
  grid-template-columns: none;
  flex-direction: row;
  align-items: center;
  gap: 10px;
}

.toggle-row input {
  width: 18px;
  height: 18px;
}

.compact-top {
  margin-top: 8px;
}

.inline-message.success {
  color: var(--status-success-text);
}

.inline-message.error {
  color: var(--status-danger-text);
}

.empty-card.compact {
  min-height: 120px;
}

@media (max-width: 1280px) {
  .spatial-layout,
  .editor-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 820px) {
  .spatial-overview,
  .spatial-stat-grid,
  .form-grid.two,
  .coordinate-grid {
    grid-template-columns: 1fr;
  }

  .spatial-overview {
    flex-direction: column;
  }
}
</style>
