<template>
  <div class="page-container site-hierarchy animate-in">
    <div class="page-header-bar">
      <div>
        <span class="panel-kicker">Enterprise Foundation</span>
        <h1 class="page-title">Site Hierarchy &amp; Asset Mapping</h1>
      </div>
      <div class="header-actions">
        <button type="button" class="btn btn-sm btn-secondary" :disabled="loading" @click="loadAll">Refresh</button>
      </div>
    </div>

    <section class="metric-grid">
      <article class="metric-tile">
        <span class="metric-label">Companies</span>
        <strong class="metric-value">{{ overview.companies }}</strong>
      </article>
      <article class="metric-tile">
        <span class="metric-label">Sites</span>
        <strong class="metric-value">{{ overview.sites }}</strong>
      </article>
      <article class="metric-tile">
        <span class="metric-label">Buildings</span>
        <strong class="metric-value">{{ overview.buildings }}</strong>
      </article>
      <article class="metric-tile">
        <span class="metric-label">Zones</span>
        <strong class="metric-value">{{ overview.zones }}</strong>
      </article>
      <article class="metric-tile">
        <span class="metric-label">Access Points</span>
        <strong class="metric-value">{{ overview.accessPoints }}</strong>
      </article>
      <article class="metric-tile">
        <span class="metric-label">Doors / Lanes</span>
        <strong class="metric-value">{{ overview.doors }} / {{ overview.lanes }}</strong>
      </article>
    </section>

    <section class="workspace-tabs">
      <button type="button" :class="{ active: tab === 'tree' }" @click="tab = 'tree'">Hierarchy Tree</button>
      <button type="button" :class="{ active: tab === 'assets' }" @click="tab = 'assets'; loadAssetStatus()">Asset Map</button>
      <button type="button" :class="{ active: tab === 'backfill' }" @click="tab = 'backfill'">Backfill</button>
    </section>

    <section v-if="tab === 'tree'" class="soc-section">
      <div class="hierarchy-layout">
        <aside class="hierarchy-tree">
          <div class="tree-search">
            <input v-model="searchQuery" placeholder="Search by name or code..." @input="searchNodes" />
          </div>
          <div v-if="searchResults.length" class="search-results">
            <div v-for="(r, i) in searchResults" :key="i" class="search-hit" @click="selectSearchResult(r)">
              <strong>{{ r.name }}</strong>
              <span class="hit-type">{{ r.entityType }}</span>
              <span v-if="r.parentName" class="hit-parent">{{ r.parentName }}</span>
            </div>
          </div>
          <div v-if="!searchQuery && hierarchy.length" class="tree-root">
            <div v-for="company in hierarchy" :key="company.companyId" class="tree-node company-node">
              <div class="node-label" :class="{ active: selectedNode?.type === 'company' && selectedNode?.id === company.companyId }" @click="selectNode('company', company.companyId, company)">
                <span class="node-icon">&#9707;</span>
                <span class="node-name">{{ company.name }}</span>
                <span class="node-code">{{ company.code }}</span>
              </div>
              <div v-if="company.sites?.length" class="tree-children">
                <div v-for="site in company.sites" :key="site.siteId" class="tree-node site-node">
                  <div class="node-label" :class="{ active: selectedNode?.type === 'site' && selectedNode?.id === site.siteId }" @click="selectNode('site', site.siteId, site)">
                    <span class="node-icon">&#9962;</span>
                    <span class="node-name">{{ site.name }}</span>
                    <span class="node-code">{{ site.code }}</span>
                  </div>
                  <div class="tree-children">
                    <div v-for="building in (site.buildings || [])" :key="building.buildingId" class="tree-node building-node">
                      <div class="node-label" :class="{ active: selectedNode?.type === 'building' && selectedNode?.id === building.buildingId }" @click="selectNode('building', building.buildingId, building)">
                        <span class="node-icon">&#9602;</span>
                        <span class="node-name">{{ building.name }}</span>
                        <span class="node-code">{{ building.code }}</span>
                      </div>
                      <div v-if="building.floors?.length" class="tree-children">
                        <div v-for="floor in building.floors" :key="floor.facilityFloorId" class="tree-node floor-node">
                          <div class="node-label" :class="{ active: selectedNode?.type === 'floor' && selectedNode?.id === floor.facilityFloorId }" @click="selectNode('floor', floor.facilityFloorId, floor)">
                            <span class="node-icon">&#9636;</span>
                            <span class="node-name">{{ floor.name }}</span>
                          </div>
                        </div>
                      </div>
                    </div>
                    <div v-for="zone in (site.zones || [])" :key="zone.securityZoneId" class="tree-node zone-node">
                      <div class="node-label" :class="{ active: selectedNode?.type === 'zone' && selectedNode?.id === zone.securityZoneId }" @click="selectNode('zone', zone.securityZoneId, zone)">
                        <span class="node-icon">&#9915;</span>
                        <span class="node-name">{{ zone.name }}</span>
                        <span class="node-code">{{ zone.code }}</span>
                      </div>
                      <div v-if="zone.accessPoints?.length" class="tree-children">
                        <div v-for="ap in zone.accessPoints" :key="ap.accessPointId" class="tree-node ap-node">
                          <div class="node-label" :class="{ active: selectedNode?.type === 'accesspoint' && selectedNode?.id === ap.accessPointId }" @click="selectNode('accesspoint', ap.accessPointId, ap)">
                            <span class="node-icon">&#8600;</span>
                            <span class="node-name">{{ ap.name }}</span>
                            <span class="node-code">{{ ap.type }}</span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div v-if="!hierarchy.length && !searchQuery" class="empty-card">No hierarchy data. Run backfill first.</div>
        </aside>
        <main class="hierarchy-detail">
          <div v-if="!selectedNode" class="empty-card">Select a node to view details.</div>
          <div v-else class="detail-panel">
            <div class="detail-header">
              <h2>{{ selectedData?.name || 'Details' }}</h2>
              <div class="detail-actions">
                <button type="button" class="btn btn-sm btn-secondary" @click="openAddChild">Add Child</button>
              </div>
            </div>
            <div class="detail-grid">
              <div class="detail-field" v-for="(val, key) in detailFields" :key="key">
                <span class="detail-label">{{ key }}</span>
                <strong>{{ val ?? '---' }}</strong>
              </div>
            </div>
          </div>
        </main>
      </div>
    </section>

    <section v-if="tab === 'assets'" class="soc-section">
      <div class="section-toolbar">
        <h2>Asset Mapping Status</h2>
      </div>
      <div v-if="assetStatus" class="asset-status-grid">
        <article class="asset-card">
          <h3>Gates</h3>
          <div class="asset-bar">
            <div class="asset-bar-fill mapped" :style="{ width: assetStatus.totalGates ? (assetStatus.gatesMapped / assetStatus.totalGates * 100) + '%' : '0%' }"></div>
          </div>
          <div class="asset-numbers">
            <span class="mapped">{{ assetStatus.gatesMapped }} mapped</span>
            <span class="unmapped">{{ assetStatus.gatesUnmapped }} unmapped</span>
          </div>
        </article>
        <article class="asset-card">
          <h3>Cameras</h3>
          <div class="asset-bar">
            <div class="asset-bar-fill mapped" :style="{ width: assetStatus.totalCameras ? (assetStatus.camerasMapped / assetStatus.totalCameras * 100) + '%' : '0%' }"></div>
          </div>
          <div class="asset-numbers">
            <span class="mapped">{{ assetStatus.camerasMapped }} mapped</span>
            <span class="unmapped">{{ assetStatus.camerasUnmapped }} unmapped</span>
          </div>
        </article>
        <article class="asset-card">
          <h3>Vehicles</h3>
          <div class="asset-bar">
            <div class="asset-bar-fill mapped" :style="{ width: assetStatus.totalVehicles ? (assetStatus.vehiclesMapped / assetStatus.totalVehicles * 100) + '%' : '0%' }"></div>
          </div>
          <div class="asset-numbers">
            <span class="mapped">{{ assetStatus.vehiclesMapped }} mapped</span>
            <span class="unmapped">{{ assetStatus.vehiclesUnmapped }} unmapped</span>
          </div>
        </article>
      </div>
    </section>

    <section v-if="tab === 'backfill'" class="soc-section">
      <div class="section-toolbar">
        <h2>Legacy Asset Backfill</h2>
      </div>
      <div class="backfill-panel">
        <div class="form-grid single">
          <label>Company code <input v-model="backfillForm.companyCode" placeholder="VSHIELD" /></label>
          <label>Site code <input v-model="backfillForm.siteCode" placeholder="HQ" /></label>
          <label>Company name <input v-model="backfillForm.companyName" placeholder="V-Shield Company" /></label>
          <label>Site name <input v-model="backfillForm.siteName" placeholder="Headquarters" /></label>
        </div>
        <button type="button" class="btn btn-primary btn-sm" :disabled="busy.backfill" @click="runBackfill">Run Safe Backfill</button>
        <div v-if="backfillReport" class="backfill-result">
          <strong>Backfill Complete</strong>
          <div class="backfill-stats">
            <span>{{ backfillReport.gatesMapped }} gates</span>
            <span>{{ backfillReport.cameraDevicesCreated }} cameras</span>
            <span>{{ backfillReport.employeesMapped }} employees</span>
            <span>{{ backfillReport.vehiclesMapped }} vehicles</span>
            <span>{{ backfillReport.accessLogSnapshotsUpdated }} log snapshots</span>
          </div>
        </div>
      </div>
    </section>

    <div v-if="showAddModal" class="modal-overlay" @click.self="showAddModal = false">
      <div class="modal-content">
        <div class="modal-header">
          <h2>Add {{ childLabel }}</h2>
          <button type="button" class="btn-close" @click="showAddModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-grid single">
            <label>Name <input v-model="addForm.name" required /></label>
            <label v-if="childType !== 'floor' && childType !== 'accesspoint'">Code <input v-model="addForm.code" /></label>
            <label v-if="childType === 'floor'">Sort Order <input v-model.number="addForm.sortOrder" type="number" min="0" /></label>
            <label v-if="childType === 'zone'">Security Level
              <select v-model="addForm.securityLevel">
                <option value="Normal">Normal</option>
                <option value="Restricted">Restricted</option>
                <option value="HighSecurity">High Security</option>
              </select>
            </label>
            <label v-if="childType === 'zone'" class="checkbox-label">
              <input v-model="addForm.isRestricted" type="checkbox" /> Restricted Zone
            </label>
            <label v-if="childType === 'accesspoint'">Type
              <select v-model="addForm.type">
                <option value="Door">Door</option>
                <option value="Gate">Gate</option>
                <option value="Turnstile">Turnstile</option>
              </select>
            </label>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-primary" :disabled="!addForm.name" @click="saveChild">Create</button>
          <button type="button" class="btn btn-secondary" @click="showAddModal = false">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const loading = ref(false)
const tab = ref('tree')
const hierarchy = ref([])
const overview = reactive({ companies: 0, sites: 0, buildings: 0, floors: 0, zones: 0, accessPoints: 0, doors: 0, lanes: 0 })
const selectedNode = ref(null)
const selectedData = ref(null)
const searchQuery = ref('')
const searchResults = ref([])
const assetStatus = ref(null)

const busy = reactive({ backfill: false })
const backfillForm = reactive({ companyName: 'V-Shield Company', companyCode: 'VSHIELD', siteName: 'Headquarters', siteCode: 'HQ', timeZoneId: 'Asia/Ho_Chi_Minh' })
const backfillReport = ref(null)

const showAddModal = ref(false)
const childType = ref('site')
const childLabel = ref('Site')
const addForm = reactive({ name: '', code: '', sortOrder: 0, securityLevel: 'Normal', isRestricted: false, type: 'Door' })

const detailFields = computed(() => {
  if (!selectedData.value) return {}
  const d = selectedData.value
  const fields = {}
  fields.Name = d.name
  if (d.code) fields.Code = d.code
  if (d.address) fields.Address = d.address
  if (d.isActive !== undefined) fields.Active = d.isActive ? 'Yes' : 'No'
  if (d.type) fields.Type = d.type
  if (d.securityLevel) fields['Security Level'] = d.securityLevel
  if (d.isRestricted !== undefined) fields.Restricted = d.isRestricted ? 'Yes' : 'No'
  if (d.directionMode) fields.Direction = d.directionMode
  if (d.sortOrder !== undefined) fields['Sort Order'] = d.sortOrder
  return fields
})

async function loadAll() {
  loading.value = true
  await Promise.all([loadOverview(), loadHierarchy()])
  loading.value = false
}

async function loadOverview() {
  try {
    const res = await enterpriseApi.overview()
    if (res?.length && res[0]?.data) Object.assign(overview, res[0].data)
  } catch {}
}

async function loadHierarchy() {
  try {
    const res = await enterpriseApi.getHierarchy()
    hierarchy.value = res.data || []
  } catch {}
}

async function loadAssetStatus() {
  try {
    const res = await enterpriseApi.getBackfillStatus()
    assetStatus.value = res.data
  } catch {}
}

async function searchNodes() {
  const q = searchQuery.value.trim()
  if (!q || q.length < 2) { searchResults.value = []; return }
  const types = ['company', 'site', 'building', 'zone']
  const all = await Promise.all(types.map(t => enterpriseApi.searchHierarchy(t, q).catch(() => ({ data: [] }))))
  searchResults.value = all.flatMap(r => r.data || []).slice(0, 30)
}

function selectSearchResult(r) {
  const typeMap = { Company: 'company', Site: 'site', Building: 'building', Zone: 'zone', AccessPoint: 'accesspoint' }
  const type = typeMap[r.entityType] || 'site'
  const id = r.companyId || r.siteId || r.buildingId || r.securityZoneId || r.accessPointId
  selectNode(type, id, r)
  searchQuery.value = ''
  searchResults.value = []
}

function selectNode(type, id, data) {
  selectedNode.value = { type, id }
  selectedData.value = data
}

function openAddChild() {
  if (!selectedNode.value) return
  const map = { company: { child: 'site', label: 'Site' }, site: { child: 'building', label: 'Building' }, building: { child: 'floor', label: 'Floor' }, zone: { child: 'accesspoint', label: 'Access Point' } }
  const m = map[selectedNode.value.type] || { child: 'site', label: 'Site' }
  childType.value = m.child
  childLabel.value = m.label
  addForm.name = ''; addForm.code = ''; addForm.sortOrder = 0; addForm.securityLevel = 'Normal'; addForm.isRestricted = false; addForm.type = 'Door'
  showAddModal.value = true
}

async function saveChild() {
  const parent = selectedNode.value
  if (!parent || !addForm.name) return
  try {
    switch (childType.value) {
      case 'site':
        await enterpriseApi.createSite({ companyId: parent.id, name: addForm.name, code: addForm.code || addForm.name.substring(0, 3).toUpperCase() })
        break
      case 'building':
        await enterpriseApi.createBuilding({ siteId: parent.id, name: addForm.name, code: addForm.code || addForm.name.substring(0, 3).toUpperCase() })
        break
      case 'floor':
        await enterpriseApi.createFloor({ buildingId: parent.id, name: addForm.name, code: addForm.code || addForm.name.substring(0, 3).toUpperCase(), sortOrder: addForm.sortOrder })
        break
      case 'accesspoint':
        await enterpriseApi.createAccessPoint({ siteId: parent.id, securityZoneId: null, name: addForm.name, type: addForm.type, directionMode: 'Bidirectional' })
        break
      case 'zone':
        await enterpriseApi.createZone({ siteId: parent.id, buildingId: null, facilityFloorId: null, name: addForm.name, code: addForm.code || addForm.name.substring(0, 3).toUpperCase(), securityLevel: addForm.securityLevel, isRestricted: addForm.isRestricted })
        break
    }
    showAddModal.value = false
    await loadHierarchy()
  } catch (err) {
    alert(err.response?.data?.message || 'Failed to create')
  }
}

async function runBackfill() {
  busy.backfill = true
  try {
    const res = await enterpriseApi.backfillDefaultSite(backfillForm)
    backfillReport.value = res.data
    await loadAll()
  } catch (err) {
    alert(err.response?.data?.message || 'Backfill failed')
  } finally {
    busy.backfill = false
  }
}

onMounted(loadAll)
</script>

<style scoped>
.site-hierarchy { max-width: 1300px; }
.hierarchy-layout { display: grid; grid-template-columns: 360px 1fr; gap: 16px; min-height: 400px; }
.hierarchy-tree { border: 1px solid var(--border-soft); border-radius: 14px; padding: 14px; background: var(--surface); overflow-y: auto; max-height: 70vh; }
.tree-search { margin-bottom: 10px; }
.tree-search input { width: 100%; min-height: 36px; padding: 0 12px; border-radius: 10px; border: 1px solid var(--border-soft); background: var(--surface); color: var(--text-primary); }
.search-results { display: flex; flex-direction: column; gap: 4px; margin-bottom: 10px; }
.search-hit { display: flex; gap: 8px; align-items: center; padding: 8px 10px; border-radius: 8px; cursor: pointer; font-size: 0.85rem; }
.search-hit:hover { background: rgba(84,196,211,.08); }
.hit-type { font-size: 0.7rem; padding: 1px 6px; border-radius: 4px; background: var(--border-soft); color: var(--text-muted); text-transform: uppercase; }
.hit-parent { color: var(--text-muted); font-size: 0.78rem; }
.tree-root { display: flex; flex-direction: column; gap: 2px; }
.node-label { display: flex; align-items: center; gap: 6px; padding: 6px 8px; border-radius: 8px; cursor: pointer; font-size: 0.85rem; transition: background .1s; }
.node-label:hover { background: rgba(84,196,211,.06); }
.node-label.active { background: rgba(84,196,211,.14); color: var(--primary); }
.node-icon { width: 18px; text-align: center; font-size: 0.9rem; color: var(--text-muted); }
.node-name { font-weight: 500; }
.node-code { font-size: 0.72rem; color: var(--text-muted); margin-left: auto; }
.tree-children { padding-left: 20px; }
.hierarchy-detail { border: 1px solid var(--border-soft); border-radius: 14px; padding: 18px; background: var(--surface); }
.detail-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }
.detail-header h2 { font-size: 1.1rem; font-weight: 600; margin: 0; }
.asset-status-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; }
.asset-card { padding: 18px; border-radius: 14px; border: 1px solid var(--border-soft); background: var(--surface); }
.asset-card h3 { font-size: 0.95rem; font-weight: 600; margin: 0 0 10px; }
.asset-bar { height: 8px; border-radius: 4px; background: var(--border-soft); overflow: hidden; margin-bottom: 8px; }
.asset-bar-fill { height: 100%; border-radius: 4px; transition: width .4s; }
.asset-bar-fill.mapped { background: var(--accent-gradient); }
.asset-numbers { display: flex; justify-content: space-between; font-size: 0.82rem; }
.asset-numbers .mapped { color: var(--primary); }
.asset-numbers .unmapped { color: var(--text-muted); }
.backfill-panel { max-width: 500px; display: flex; flex-direction: column; gap: 12px; }
.backfill-result { padding: 14px; border-radius: 10px; background: rgba(34,197,94,.08); border: 1px solid rgba(34,197,94,.2); }
.backfill-stats { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 8px; font-size: 0.85rem; color: var(--text-secondary); }
.checkbox-label { display: flex !important; align-items: center; gap: 8px; cursor: pointer; flex-direction: row !important; }
.checkbox-label input[type="checkbox"] { width: 18px; height: 18px; }
</style>
