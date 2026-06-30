<template>
  <div class="page-container site-hierarchy-page animate-in">
    <section class="hero-panel">
      <div class="hero-copy">
        <span class="panel-kicker">Enterprise Foundation</span>
        <h1 class="page-title">Site Hierarchy &amp; Asset Mapping</h1>
        <p class="hero-text">
          Organize physical locations, security zones, and access points in one workspace, then verify how
          gates, cameras, and vehicles are mapped across the hierarchy.
        </p>
        <div class="hero-actions">
          <button type="button" class="btn btn-primary" :disabled="loading" @click="loadAll">Refresh workspace</button>
          <button type="button" class="btn btn-secondary" @click="focusStructureTab">Open structure view</button>
        </div>
      </div>
      <div class="hero-side">
        <div class="hero-stat">
          <strong>{{ overview.sites }}</strong>
          <span>active sites in scope</span>
        </div>
        <div class="hero-stat">
          <strong>{{ overview.accessPoints }}</strong>
          <span>access points mapped</span>
        </div>
        <div class="hero-stat highlight">
          <strong>{{ mappedAssetSummary }}</strong>
          <span>asset coverage snapshot</span>
        </div>
      </div>
    </section>

    <section class="metric-strip">
      <article v-for="metric in topMetrics" :key="metric.label" class="metric-card">
        <span class="metric-label">{{ metric.label }}</span>
        <strong class="metric-value">{{ metric.value }}</strong>
        <span class="metric-note">{{ metric.note }}</span>
      </article>
    </section>

    <section class="workspace-tabs">
      <button type="button" :class="{ active: tab === 'tree' }" @click="tab = 'tree'">Structure Workspace</button>
      <button type="button" :class="{ active: tab === 'assets' }" @click="openAssetTab">Asset Coverage</button>
      <button type="button" :class="{ active: tab === 'spatial' }" @click="openSpatialTab">Spatial Ops</button>
    </section>

    <section v-if="tab === 'tree'" class="workspace-shell">
      <aside class="navigator-panel">
        <div class="panel-section-header">
          <div>
            <span class="section-kicker">Navigator</span>
            <h2>Hierarchy explorer</h2>
          </div>
          <div class="toolbar-actions">
            <span class="soft-chip">{{ hierarchy.length }} companies</span>
            <button type="button" class="btn btn-xs btn-primary" @click="openCreateCompany">+ Company</button>
          </div>
        </div>

        <div class="navigator-toolbar">
          <label class="attention-toggle">
            <input v-model="showNeedsAttentionOnly" type="checkbox" />
            <span>Show only nodes needing setup</span>
          </label>
          <div class="toolbar-actions">
            <button type="button" class="btn btn-xs btn-secondary" @click="expandAll">Expand all</button>
            <button type="button" class="btn btn-xs btn-secondary" @click="collapseAll">Collapse all</button>
          </div>
        </div>

        <label class="search-box">
          <span>Search node</span>
          <input v-model="searchQuery" placeholder="Search by name or code..." @input="searchNodes" />
        </label>

        <div v-if="searchQuery && !searchResults.length" class="empty-inline">
          No quick matches yet. Try at least 2 characters or switch to the tree below.
        </div>

        <div v-if="searchResults.length" class="search-results">
          <button
            v-for="(result, index) in searchResults"
            :key="`${result.entityType}-${index}`"
            type="button"
            class="search-hit"
            @click="selectSearchResult(result)"
          >
            <strong>{{ result.name }}</strong>
            <span class="hit-meta">{{ result.entityType }}</span>
            <span v-if="result.parentName" class="hit-parent">{{ result.parentName }}</span>
          </button>
        </div>

        <div v-if="!searchQuery && hierarchy.length" class="tree-root">
          <div v-for="company in hierarchy" :key="company.companyId" class="tree-branch">
            <div class="node-stack">
              <button
                v-if="company.sites?.length"
                type="button"
                class="expand-btn"
                @click.stop="toggleExpanded('company', company.companyId)"
              >
                {{ isExpanded('company', company.companyId) ? '−' : '+' }}
              </button>
              <button
                type="button"
                class="node-row company-row"
                :class="{ active: isSelected('company', company.companyId), attention: needsAttention(company, 'company') }"
                @click="selectNode('company', company.companyId, company)"
              >
                <span class="node-icon">◎</span>
                <span class="node-main">
                  <strong>{{ company.name }}</strong>
                  <small>{{ company.code }}</small>
                </span>
                <span v-if="needsAttention(company, 'company')" class="node-badge">Needs setup</span>
                <span class="node-count">{{ company.sites?.length || 0 }} sites</span>
              </button>
            </div>

            <div v-if="company.sites?.length && isExpanded('company', company.companyId)" class="tree-children">
              <div v-for="site in filteredSites(company.sites || [])" :key="site.siteId" class="tree-branch">
                <div class="node-stack">
                  <button
                    v-if="siteHasVisibleChildren(site)"
                    type="button"
                    class="expand-btn"
                    @click.stop="toggleExpanded('site', site.siteId)"
                  >
                    {{ isExpanded('site', site.siteId) ? '−' : '+' }}
                  </button>
                <button
                  type="button"
                  class="node-row site-row"
                  :class="{ active: isSelected('site', site.siteId), attention: needsAttention(site, 'site') }"
                  @click="selectNode('site', site.siteId, site)"
                >
                  <span class="node-icon">⌂</span>
                  <span class="node-main">
                    <strong>{{ site.name }}</strong>
                    <small>{{ site.code }}</small>
                  </span>
                  <span v-if="needsAttention(site, 'site')" class="node-badge">Needs setup</span>
                  <span class="node-count">{{ (site.buildings?.length || 0) + (site.zones?.length || 0) }} units</span>
                </button>
                </div>

                <div v-if="isExpanded('site', site.siteId)" class="tree-children">
                  <div v-for="building in filteredBuildings(site.buildings || [])" :key="building.buildingId" class="tree-branch">
                    <div class="node-stack">
                      <button
                        v-if="building.floors?.length"
                        type="button"
                        class="expand-btn"
                        @click.stop="toggleExpanded('building', building.buildingId)"
                      >
                        {{ isExpanded('building', building.buildingId) ? '−' : '+' }}
                      </button>
                    <button
                      type="button"
                      class="node-row building-row"
                      :class="{ active: isSelected('building', building.buildingId), attention: needsAttention(building, 'building') }"
                      @click="selectNode('building', building.buildingId, building)"
                    >
                      <span class="node-icon">▣</span>
                      <span class="node-main">
                        <strong>{{ building.name }}</strong>
                        <small>{{ building.code }}</small>
                      </span>
                      <span v-if="needsAttention(building, 'building')" class="node-badge">Needs setup</span>
                      <span class="node-count">{{ building.floors?.length || 0 }} floors</span>
                    </button>
                    </div>

                    <div v-if="building.floors?.length && isExpanded('building', building.buildingId)" class="tree-children">
                      <button
                        v-for="floor in filteredFloors(building.floors || [])"
                        :key="floor.facilityFloorId"
                        type="button"
                        class="node-row floor-row"
                        :class="{ active: isSelected('floor', floor.facilityFloorId) }"
                        @click="selectNode('floor', floor.facilityFloorId, floor)"
                      >
                        <span class="node-icon">□</span>
                        <span class="node-main">
                          <strong>{{ floor.name }}</strong>
                          <small>Sort {{ floor.sortOrder ?? 0 }}</small>
                        </span>
                        <span v-if="needsAttention(floor, 'floor')" class="node-badge">Review</span>
                      </button>
                    </div>
                  </div>

                  <div v-for="zone in filteredZones(site.zones || [])" :key="zone.securityZoneId" class="tree-branch">
                    <div class="node-stack">
                      <button
                        v-if="zone.accessPoints?.length"
                        type="button"
                        class="expand-btn"
                        @click.stop="toggleExpanded('zone', zone.securityZoneId)"
                      >
                        {{ isExpanded('zone', zone.securityZoneId) ? '−' : '+' }}
                      </button>
                    <button
                      type="button"
                      class="node-row zone-row"
                      :class="{ active: isSelected('zone', zone.securityZoneId), attention: needsAttention(zone, 'zone') }"
                      @click="selectNode('zone', zone.securityZoneId, zone)"
                    >
                      <span class="node-icon">◈</span>
                      <span class="node-main">
                        <strong>{{ zone.name }}</strong>
                        <small>{{ zone.code }}</small>
                      </span>
                      <span v-if="needsAttention(zone, 'zone')" class="node-badge">Needs setup</span>
                      <span class="node-count">{{ zone.accessPoints?.length || 0 }} points</span>
                    </button>
                    </div>

                    <div v-if="zone.accessPoints?.length && isExpanded('zone', zone.securityZoneId)" class="tree-children">
                      <button
                        v-for="accessPoint in filteredAccessPoints(zone.accessPoints || [])"
                        :key="accessPoint.accessPointId"
                        type="button"
                        class="node-row access-row"
                        :class="{ active: isSelected('accesspoint', accessPoint.accessPointId), attention: needsAttention(accessPoint, 'accesspoint') }"
                        @click="selectNode('accesspoint', accessPoint.accessPointId, accessPoint)"
                      >
                        <span class="node-icon">↘</span>
                        <span class="node-main">
                          <strong>{{ accessPoint.name }}</strong>
                          <small>{{ accessPoint.type }}</small>
                        </span>
                        <span v-if="needsAttention(accessPoint, 'accesspoint')" class="node-badge">Review</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div v-if="!hierarchy.length && !searchQuery" class="empty-card">
          No hierarchy data yet. Use the advanced tools to backfill a default site foundation first.
        </div>
      </aside>

      <main class="detail-panel-shell">
        <div v-if="selectedNode && selectedData" class="detail-panel">
          <div class="detail-hero">
            <div>
              <div class="detail-type-row">
                <span class="soft-chip success">{{ selectedNodeLabel }}</span>
                <span v-if="selectedData.code" class="soft-chip">{{ selectedData.code }}</span>
                <span v-if="selectedData.isActive !== undefined" class="soft-chip" :class="selectedData.isActive ? 'success' : 'muted'">
                  {{ selectedData.isActive ? 'Active' : 'Inactive' }}
                </span>
              </div>
              <h2>{{ selectedData.name }}</h2>
              <p class="detail-description">
                {{ selectedNodeDescription }}
              </p>
            </div>
            <div class="detail-hero-actions">
              <button
                type="button"
                class="btn btn-primary btn-sm"
                @click="openEditNode"
              >
                Edit
              </button>
              <button
                type="button"
                class="btn btn-sm"
                :class="selectedData.isActive === false ? 'btn-secondary' : 'btn-danger'"
                @click="deleteSelectedNode"
              >
                {{ selectedNodeLifecycleAction }}
              </button>
              <button
                v-for="option in childOptions"
                :key="option.type"
                type="button"
                class="btn btn-secondary btn-sm"
                @click="openAddChild(option.type)"
              >
                + {{ option.label }}
              </button>
            </div>
          </div>

          <section class="detail-stat-grid">
            <article v-for="stat in selectedNodeStats" :key="stat.label" class="detail-stat-card">
              <span>{{ stat.label }}</span>
              <strong>{{ stat.value }}</strong>
            </article>
          </section>

          <section class="detail-content-grid">
            <article class="detail-card primary">
              <div class="panel-section-header compact">
                <div>
                  <span class="section-kicker">Overview</span>
                  <h3>Node details</h3>
                </div>
              </div>
              <div class="detail-grid">
                <div v-for="(value, key) in detailFields" :key="key" class="detail-field">
                  <span class="detail-label">{{ key }}</span>
                  <strong>{{ value ?? '---' }}</strong>
                </div>
              </div>
            </article>

            <article class="detail-card">
              <div class="panel-section-header compact">
                <div>
                  <span class="section-kicker">Activity</span>
                  <h3>Recent changes</h3>
                </div>
              </div>
              <div v-if="historyLoading" class="history-empty">
                Loading change history...
              </div>
              <div v-else-if="historyCards.length" class="history-list">
                <div v-for="item in historyCards" :key="item.id" class="history-item">
                  <div class="history-item-top">
                    <strong>{{ item.action }}</strong>
                    <span class="soft-chip" :class="item.status === 'Success' ? 'success' : 'muted'">{{ item.status }}</span>
                  </div>
                  <p>{{ item.actor }} • {{ item.timestamp }}</p>
                  <small v-if="item.path">{{ item.path }}</small>
                  <small v-if="item.reason" class="history-reason">{{ item.reason }}</small>
                </div>
              </div>
              <div v-else class="history-empty">
                No recorded changes yet for this node.
              </div>
            </article>
          </section>
        </div>

        <div v-else class="empty-state-card">
          <span class="section-kicker">Ready</span>
          <h2>Select a node to start</h2>
          <p>
            Choose a company, site, building, zone, or access point from the navigator to inspect details and continue structuring the environment.
          </p>
          <button v-if="hierarchy.length" type="button" class="btn btn-primary" @click="selectNode('company', hierarchy[0].companyId, hierarchy[0])">
            Open first company
          </button>
        </div>
      </main>
    </section>

    <section v-else-if="tab === 'assets'" class="asset-workspace">
      <div class="asset-overview-card">
        <div>
          <span class="section-kicker">Coverage</span>
          <h2>Asset mapping status</h2>
          <p>
            Review how completely operational assets are attached to the enterprise structure before running automation or governance checks.
          </p>
        </div>
        <button type="button" class="btn btn-secondary btn-sm" @click="toggleAdvancedTools">
          {{ showAdvancedTools ? 'Hide advanced tools' : 'Show advanced tools' }}
        </button>
      </div>

      <div class="asset-status-grid">
        <article v-for="card in assetCards" :key="card.label" class="asset-card">
          <div class="asset-card-top">
            <h3>{{ card.label }}</h3>
            <span class="soft-chip" :class="card.unmapped === 0 ? 'success' : 'warning'">{{ card.coverageLabel }}</span>
          </div>
          <div class="asset-bar">
            <div class="asset-bar-fill mapped" :style="{ width: `${card.percent}%` }"></div>
          </div>
          <div class="asset-numbers">
            <span class="mapped">{{ card.mapped }} mapped</span>
            <span class="unmapped">{{ card.unmapped }} unmapped</span>
          </div>
          <p class="asset-note">{{ card.note }}</p>
        </article>
      </div>

      <div class="asset-insight-strip">
        <div class="insight-card">
          <strong>{{ mappedAssetSummary }}</strong>
          <span>overall mapped assets</span>
        </div>
        <div class="insight-card">
          <strong>{{ overallCoveragePercent }}%</strong>
          <span>overall coverage</span>
        </div>
        <div class="insight-card">
          <strong>{{ riskiestAssetLabel }}</strong>
          <span>needs the most attention</span>
        </div>
      </div>

      <section class="unmapped-section">
        <div class="panel-section-header">
          <div>
            <span class="section-kicker">Action queue</span>
            <h2>Unmapped assets</h2>
          </div>
          <div class="toolbar-actions">
            <button
              v-for="option in assetTypeOptions"
              :key="option.value"
              type="button"
              class="btn btn-xs"
              :class="selectedAssetFilter === option.value ? 'btn-primary' : 'btn-secondary'"
              @click="selectedAssetFilter = option.value"
            >
              {{ option.label }}
            </button>
          </div>
        </div>

        <div v-if="!assetMapLoaded" class="empty-card">Loading unmapped asset details...</div>
        <div v-else-if="!filteredUnmappedAssets.length" class="empty-card">All assets in this filter are already mapped.</div>
        <div v-else class="unmapped-grid">
          <article v-for="asset in filteredUnmappedAssets" :key="asset.key" class="unmapped-card">
            <div class="unmapped-top">
              <strong>{{ asset.title }}</strong>
              <span class="soft-chip warning">{{ asset.typeLabel }}</span>
            </div>
            <p class="asset-note">{{ asset.subtitle }}</p>
            <div class="unmapped-meta">
              <span v-for="tag in asset.tags" :key="tag" class="soft-chip muted">{{ tag }}</span>
            </div>
          </article>
      </div>
    </section>

    <SpatialInfrastructureWorkspace
      v-if="tab === 'spatial'"
      :site-options="siteOptions"
      :preferred-site-id="preferredSpatialSiteId"
    />

      <section v-if="showAdvancedTools" class="advanced-tools-panel">
        <div class="panel-section-header">
          <div>
            <span class="section-kicker">Advanced</span>
            <h2>Legacy asset backfill</h2>
          </div>
          <span class="soft-chip warning">Privileged action</span>
        </div>
        <p class="advanced-copy">
          Use this only when bootstrapping or repairing older deployments. It creates a safe default company/site structure and remaps legacy assets into that structure.
        </p>

        <div class="backfill-grid">
          <label>Company code <input v-model="backfillForm.companyCode" placeholder="VSHIELD" /></label>
          <label>Site code <input v-model="backfillForm.siteCode" placeholder="HQ" /></label>
          <label>Company name <input v-model="backfillForm.companyName" placeholder="V-Shield Company" /></label>
          <label>Site name <input v-model="backfillForm.siteName" placeholder="Headquarters" /></label>
        </div>

        <div class="advanced-actions">
          <button type="button" class="btn btn-primary btn-sm" :disabled="busy.backfill" @click="runBackfill">
            {{ busy.backfill ? 'Running backfill...' : 'Run safe backfill' }}
          </button>
        </div>

        <div v-if="backfillReport" class="backfill-result">
          <strong>Backfill complete</strong>
          <div class="backfill-stats">
            <span>{{ backfillReport.gatesMapped }} gates</span>
            <span>{{ backfillReport.cameraDevicesCreated }} cameras</span>
            <span>{{ backfillReport.employeesMapped }} employees</span>
            <span>{{ backfillReport.vehiclesMapped }} vehicles</span>
            <span>{{ backfillReport.accessLogSnapshotsUpdated }} log snapshots</span>
          </div>
        </div>
      </section>
    </section>

    <div v-if="showAddModal" class="modal-overlay" @click.self="showAddModal = false">
      <div class="modal-content modern-modal">
        <div class="modal-header">
          <div>
            <span class="section-kicker">Create child node</span>
            <h2>Add {{ childLabel }}</h2>
          </div>
          <button type="button" class="btn-close" @click="showAddModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <p class="modal-copy">
            <template v-if="childType === 'company'">
              Create a new top-level company workspace to start a fresh hierarchy.
            </template>
            <template v-else>
              This new {{ childLabel.toLowerCase() }} will be attached under <strong>{{ selectedData?.name }}</strong>.
            </template>
          </p>
          <div class="form-grid single">
            <label>Name <input v-model="addForm.name" required /></label>
            <label v-if="childType !== 'accesspoint'">Code <input v-model="addForm.code" placeholder="Optional short code" /></label>
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

    <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
      <div class="modal-content modern-modal">
        <div class="modal-header">
          <div>
            <span class="section-kicker">Edit node</span>
            <h2>{{ editTitle }}</h2>
          </div>
          <button type="button" class="btn-close" @click="showEditModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-grid single">
            <label>Name <input v-model="editForm.name" required /></label>
            <label v-if="editNodeType !== 'accesspoint'">Code <input v-model="editForm.code" /></label>
            <label v-if="editNodeType === 'site'">Address <input v-model="editForm.address" /></label>
            <label v-if="editNodeType === 'site'">Time Zone <input v-model="editForm.timeZoneId" /></label>
            <label v-if="editNodeType === 'floor'">Sort Order <input v-model.number="editForm.sortOrder" type="number" min="0" /></label>
            <label v-if="editNodeType === 'zone'">Security Level
              <select v-model="editForm.securityLevel">
                <option value="Normal">Normal</option>
                <option value="Restricted">Restricted</option>
                <option value="HighSecurity">High Security</option>
              </select>
            </label>
            <label v-if="editNodeType === 'zone'" class="checkbox-label">
              <input v-model="editForm.isRestricted" type="checkbox" /> Restricted Zone
            </label>
            <label v-if="editNodeType === 'accesspoint'">Type
              <select v-model="editForm.type">
                <option value="Door">Door</option>
                <option value="Gate">Gate</option>
                <option value="Turnstile">Turnstile</option>
              </select>
            </label>
            <label v-if="editNodeType === 'accesspoint'">Direction
              <select v-model="editForm.directionMode">
                <option value="Bidirectional">Bidirectional</option>
                <option value="EntryOnly">Entry Only</option>
                <option value="ExitOnly">Exit Only</option>
              </select>
            </label>
            <label class="checkbox-label">
              <input v-model="editForm.isActive" type="checkbox" /> Active
            </label>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-primary" :disabled="!editForm.name" @click="saveEditNode">Save</button>
          <button type="button" class="btn btn-secondary" @click="showEditModal = false">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import SpatialInfrastructureWorkspace from '../components/site-hierarchy/SpatialInfrastructureWorkspace.vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'
import { getSystemAuditLogs } from '../services/accessLogApi'

const loading = ref(false)
const tab = ref('tree')
const hierarchy = ref([])
const overview = reactive({ companies: 0, sites: 0, buildings: 0, floors: 0, zones: 0, accessPoints: 0, doors: 0, lanes: 0 })
const selectedNode = ref(null)
const selectedData = ref(null)
const searchQuery = ref('')
const searchResults = ref([])
const assetStatus = ref(null)
const assetMap = ref(null)
const assetMapLoaded = ref(false)
const historyLoading = ref(false)
const selectedHistory = ref([])
const showAdvancedTools = ref(false)
const showNeedsAttentionOnly = ref(false)
const selectedAssetFilter = ref('all')
const expandedNodes = ref({
  company: new Set(),
  site: new Set(),
  building: new Set(),
  zone: new Set(),
})

const busy = reactive({ backfill: false })
const backfillForm = reactive({ companyName: 'V-Shield Company', companyCode: 'VSHIELD', siteName: 'Headquarters', siteCode: 'HQ', timeZoneId: 'Asia/Ho_Chi_Minh' })
const backfillReport = ref(null)

const showAddModal = ref(false)
const childType = ref('site')
const childLabel = ref('Site')
const addForm = reactive({ name: '', code: '', sortOrder: 0, securityLevel: 'Normal', isRestricted: false, type: 'Door' })
const showEditModal = ref(false)
const editNodeType = ref('company')
const editTitle = ref('Edit node')
const editForm = reactive({
  name: '',
  code: '',
  address: '',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  sortOrder: 0,
  securityLevel: 'Normal',
  isRestricted: false,
  type: 'Door',
  directionMode: 'Bidirectional',
  isActive: true,
})

const topMetrics = computed(() => ([
  { label: 'Companies', value: overview.companies, note: 'top-level org units' },
  { label: 'Sites', value: overview.sites, note: 'physical campuses' },
  { label: 'Buildings', value: overview.buildings, note: 'mapped structures' },
  { label: 'Zones', value: overview.zones, note: 'security perimeters' },
  { label: 'Access Points', value: overview.accessPoints, note: 'doors, gates, turnstiles' },
  { label: 'Doors / Lanes', value: `${overview.doors} / ${overview.lanes}`, note: 'operational endpoints' },
]))

const selectedNodeLabel = computed(() => {
  const map = {
    company: 'Company',
    site: 'Site',
    building: 'Building',
    floor: 'Floor',
    zone: 'Security Zone',
    accesspoint: 'Access Point',
  }
  return selectedNode.value ? map[selectedNode.value.type] || 'Node' : 'Node'
})

const selectedNodeDescription = computed(() => {
  if (!selectedNode.value) return ''
  const map = {
    company: 'Company-level container for all downstream sites and operational assets.',
    site: 'Site-level workspace that groups buildings, zones, and local operational assets.',
    building: 'Building record used to organize floors and physical layout context.',
    floor: 'Floor-level node for ordering and orienting site facilities.',
    zone: 'Security zone used to group risk, access control, and entry points.',
    accesspoint: 'Operational endpoint where access decisions and device mapping converge.',
  }
  return map[selectedNode.value.type] || 'Hierarchy node details.'
})

const childOptions = computed(() => {
  if (!selectedNode.value) return []
  const map = {
    company: [{ type: 'site', label: 'Site' }],
    site: [{ type: 'building', label: 'Building' }, { type: 'zone', label: 'Security Zone' }],
    building: [{ type: 'floor', label: 'Floor' }],
    zone: [{ type: 'accesspoint', label: 'Access Point' }],
  }
  return map[selectedNode.value.type] || []
})

const selectedNodeLifecycleAction = computed(() => (selectedData.value?.isActive === false ? 'Restore' : 'Deactivate'))

const selectedNodeStats = computed(() => {
  if (!selectedData.value || !selectedNode.value) return []
  const data = selectedData.value
  const type = selectedNode.value.type

  if (type === 'company') {
    return [
      { label: 'Sites', value: data.sites?.length || 0 },
      { label: 'Buildings', value: (data.sites || []).reduce((sum, site) => sum + (site.buildings?.length || 0), 0) },
      { label: 'Zones', value: (data.sites || []).reduce((sum, site) => sum + (site.zones?.length || 0), 0) },
    ]
  }
  if (type === 'site') {
    return [
      { label: 'Buildings', value: data.buildings?.length || 0 },
      { label: 'Zones', value: data.zones?.length || 0 },
      { label: 'Addressed', value: data.address ? 'Yes' : 'No' },
    ]
  }
  if (type === 'building') {
    return [
      { label: 'Floors', value: data.floors?.length || 0 },
      { label: 'Active', value: data.isActive ? 'Yes' : 'No' },
      { label: 'Code', value: data.code || '---' },
    ]
  }
  if (type === 'zone') {
    return [
      { label: 'Access Points', value: data.accessPoints?.length || 0 },
      { label: 'Security Level', value: data.securityLevel || 'Normal' },
      { label: 'Restricted', value: data.isRestricted ? 'Yes' : 'No' },
    ]
  }
  if (type === 'accesspoint') {
    return [
      { label: 'Type', value: data.type || '---' },
      { label: 'Direction', value: data.directionMode || '---' },
      { label: 'Active', value: data.isActive ? 'Yes' : 'No' },
    ]
  }
  return [
    { label: 'Code', value: data.code || '---' },
    { label: 'Sort Order', value: data.sortOrder ?? '---' },
    { label: 'Active', value: data.isActive ? 'Yes' : 'No' },
  ]
})

const historyCards = computed(() =>
  (selectedHistory.value || []).map((item) => ({
    id: item.id,
    action: item.actionType || 'UPDATE',
    actor: item.username || 'System',
    status: item.isSuccess ? 'Success' : 'Failed',
    reason: item.failureReason || null,
    path: item.path || null,
    timestamp: formatAuditTimestamp(item.timestampUtc),
  }))
)

const detailFields = computed(() => {
  if (!selectedData.value) return {}
  const data = selectedData.value
  const fields = {}
  fields.Name = data.name
  if (data.code) fields.Code = data.code
  if (data.address) fields.Address = data.address
  if (data.timeZoneId) fields['Time Zone'] = data.timeZoneId
  if (data.isActive !== undefined) fields.Active = data.isActive ? 'Yes' : 'No'
  if (data.type) fields.Type = data.type
  if (data.securityLevel) fields['Security Level'] = data.securityLevel
  if (data.isRestricted !== undefined) fields.Restricted = data.isRestricted ? 'Yes' : 'No'
  if (data.directionMode) fields.Direction = data.directionMode
  if (data.sortOrder !== undefined) fields['Sort Order'] = data.sortOrder
  return fields
})

const assetCards = computed(() => {
  const status = assetStatus.value
  if (!status) return []
  return [
    {
      label: 'Gates',
      mapped: status.gatesMapped || 0,
      unmapped: status.gatesUnmapped || 0,
      total: status.totalGates || 0,
      percent: percent(status.gatesMapped, status.totalGates),
      coverageLabel: `${percent(status.gatesMapped, status.totalGates)}% coverage`,
      note: 'Gate assets should be linked before barrier and lane automation.',
    },
    {
      label: 'Cameras',
      mapped: status.camerasMapped || 0,
      unmapped: status.camerasUnmapped || 0,
      total: status.totalCameras || 0,
      percent: percent(status.camerasMapped, status.totalCameras),
      coverageLabel: `${percent(status.camerasMapped, status.totalCameras)}% coverage`,
      note: 'Camera mapping improves incident correlation and site-level review.',
    },
    {
      label: 'Vehicles',
      mapped: status.vehiclesMapped || 0,
      unmapped: status.vehiclesUnmapped || 0,
      total: status.totalVehicles || 0,
      percent: percent(status.vehiclesMapped, status.totalVehicles),
      coverageLabel: `${percent(status.vehiclesMapped, status.totalVehicles)}% coverage`,
      note: 'Vehicle mapping keeps parking and access enforcement tied to the right site.',
    },
  ]
})

const mappedAssetSummary = computed(() => {
  if (!assetStatus.value) return 'Not loaded'
  const mapped = (assetStatus.value.gatesMapped || 0) + (assetStatus.value.camerasMapped || 0) + (assetStatus.value.vehiclesMapped || 0)
  const total = (assetStatus.value.totalGates || 0) + (assetStatus.value.totalCameras || 0) + (assetStatus.value.totalVehicles || 0)
  return `${mapped}/${total || 0}`
})

const assetTypeOptions = [
  { label: 'All', value: 'all' },
  { label: 'Gates', value: 'gate' },
  { label: 'Cameras', value: 'camera' },
  { label: 'Vehicles', value: 'vehicle' },
]

const overallCoveragePercent = computed(() => {
  if (!assetStatus.value) return 0
  const mapped = (assetStatus.value.gatesMapped || 0) + (assetStatus.value.camerasMapped || 0) + (assetStatus.value.vehiclesMapped || 0)
  const total = (assetStatus.value.totalGates || 0) + (assetStatus.value.totalCameras || 0) + (assetStatus.value.totalVehicles || 0)
  return percent(mapped, total)
})

const riskiestAssetLabel = computed(() => {
  if (!assetCards.value.length) return 'Awaiting data'
  const sorted = [...assetCards.value].sort((a, b) => b.unmapped - a.unmapped)
  return sorted[0]?.label || 'Awaiting data'
})

const unmappedAssets = computed(() => {
  if (!assetMap.value) return []
  const gates = (assetMap.value.gates || [])
    .filter((item) => !item.siteId)
    .map((item) => ({
      key: `gate-${item.gateId}`,
      type: 'gate',
      typeLabel: 'Gate',
      title: item.gateName,
      subtitle: item.location || 'No mapped site or lane yet.',
      tags: [`Gate #${item.gateId}`],
    }))
  const cameras = (assetMap.value.cameras || [])
    .filter((item) => !item.siteId)
    .map((item) => ({
      key: `camera-${item.cameraId}`,
      type: 'camera',
      typeLabel: 'Camera',
      title: item.cameraName,
      subtitle: item.cameraType || 'No mapped site or device link yet.',
      tags: [`Camera #${item.cameraId}`, item.gateId ? `Gate ${item.gateId}` : 'No gate link'],
    }))
  const vehicles = (assetMap.value.vehicles || [])
    .filter((item) => !item.siteId)
    .map((item) => ({
      key: `vehicle-${item.vehicleId}`,
      type: 'vehicle',
      typeLabel: 'Vehicle',
      title: item.licensePlate,
      subtitle: item.employeeName || 'No employee owner attached.',
      tags: [`Vehicle #${item.vehicleId}`, item.parkingStatus || 'Unknown parking state'],
    }))
  return [...gates, ...cameras, ...vehicles]
})

const filteredUnmappedAssets = computed(() => {
  if (selectedAssetFilter.value === 'all') return unmappedAssets.value
  return unmappedAssets.value.filter((item) => item.type === selectedAssetFilter.value)
})

const siteOptions = computed(() =>
  hierarchy.value.flatMap((company) =>
    (company.sites || []).map((site) => ({
      siteId: site.siteId,
      name: site.name,
      code: site.code,
    }))
  )
)

const preferredSpatialSiteId = computed(() => {
  if (!selectedNode.value || !selectedData.value) return null
  if (selectedNode.value.type === 'site') return selectedData.value.siteId
  if (selectedNode.value.type === 'building') return selectedData.value.siteId
  if (selectedNode.value.type === 'floor') return selectedData.value.siteId
  if (selectedNode.value.type === 'zone') return selectedData.value.siteId
  if (selectedNode.value.type === 'accesspoint') return selectedData.value.siteId
  return null
})

async function loadAll() {
  loading.value = true
  await Promise.all([loadOverview(), loadHierarchy(), loadAssetStatus(), loadAssetMap()])
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
    hierarchy.value = normalizeHierarchy(res.data || [])
    if (!selectedNode.value && hierarchy.value.length) {
      selectNode('company', hierarchy.value[0].companyId, hierarchy.value[0])
    } else if (selectedNode.value) {
      const refreshed = findNodeRecord(selectedNode.value.type, selectedNode.value.id)
      if (refreshed) selectedData.value = refreshed
    }
  } catch {
    hierarchy.value = []
  }
}

async function loadAssetStatus() {
  try {
    const res = await enterpriseApi.getBackfillStatus()
    assetStatus.value = res.data
  } catch {}
}

async function loadAssetMap() {
  try {
    const res = await enterpriseApi.assetMap()
    assetMap.value = normalizeAssetMap(res.data)
  } catch {
    assetMap.value = { gates: [], cameras: [], vehicles: [] }
  } finally {
    assetMapLoaded.value = true
  }
}

async function searchNodes() {
  const query = searchQuery.value.trim()
  if (!query || query.length < 2) {
    searchResults.value = []
    return
  }
  const types = ['company', 'site', 'building', 'zone', 'accesspoint']
  const all = await Promise.all(types.map((type) => enterpriseApi.searchHierarchy(type, query).catch(() => ({ data: [] }))))
  searchResults.value = all.flatMap((result) => result.data || []).slice(0, 30)
}

function selectSearchResult(result) {
  const typeMap = { Company: 'company', Site: 'site', Building: 'building', Zone: 'zone', AccessPoint: 'accesspoint' }
  const type = typeMap[result.entityType] || 'site'
  const id = result.companyId || result.siteId || result.buildingId || result.securityZoneId || result.accessPointId
  const record = findNodeRecord(type, id)
  selectNode(type, id, record || result)
  searchQuery.value = ''
  searchResults.value = []
}

function selectNode(type, id, data) {
  selectedNode.value = { type, id }
  selectedData.value = data
  loadSelectedNodeHistory()
}

function isSelected(type, id) {
  return selectedNode.value?.type === type && selectedNode.value?.id === id
}

function focusStructureTab() {
  tab.value = 'tree'
}

async function openAssetTab() {
  tab.value = 'assets'
  if (!assetStatus.value) await loadAssetStatus()
  if (!assetMapLoaded.value) await loadAssetMap()
}

function toggleAdvancedTools() {
  showAdvancedTools.value = !showAdvancedTools.value
}

function toggleExpanded(type, id) {
  const current = expandedNodes.value[type]
  if (!current) return
  if (current.has(id)) current.delete(id)
  else current.add(id)
}

function isExpanded(type, id) {
  return expandedNodes.value[type]?.has(id)
}

function expandAll() {
  expandedNodes.value.company = new Set(hierarchy.value.map((company) => company.companyId))
  expandedNodes.value.site = new Set(hierarchy.value.flatMap((company) => (company.sites || []).map((site) => site.siteId)))
  expandedNodes.value.building = new Set(hierarchy.value.flatMap((company) => (company.sites || []).flatMap((site) => (site.buildings || []).map((building) => building.buildingId))))
  expandedNodes.value.zone = new Set(hierarchy.value.flatMap((company) => (company.sites || []).flatMap((site) => (site.zones || []).map((zone) => zone.securityZoneId))))
}

function collapseAll() {
  expandedNodes.value.company = new Set()
  expandedNodes.value.site = new Set()
  expandedNodes.value.building = new Set()
  expandedNodes.value.zone = new Set()
}

function openAddChild(nextType) {
  if (!selectedNode.value) return
  const option = childOptions.value.find((item) => item.type === nextType) || childOptions.value[0]
  if (!option) return
  childType.value = option.type
  childLabel.value = option.label
  addForm.name = ''
  addForm.code = ''
  addForm.sortOrder = 0
  addForm.securityLevel = 'Normal'
  addForm.isRestricted = false
  addForm.type = 'Door'
  showAddModal.value = true
}

function openSpatialTab() {
  tab.value = 'spatial'
}

function openCreateCompany() {
  selectedNode.value = null
  selectedData.value = null
  childType.value = 'company'
  childLabel.value = 'Company'
  addForm.name = ''
  addForm.code = ''
  addForm.sortOrder = 0
  addForm.securityLevel = 'Normal'
  addForm.isRestricted = false
  addForm.type = 'Door'
  showAddModal.value = true
}

function openEditNode() {
  if (!selectedNode.value || !selectedData.value) return
  editNodeType.value = selectedNode.value.type
  editTitle.value = `Edit ${selectedNodeLabel.value}`
  editForm.name = selectedData.value.name || ''
  editForm.code = selectedData.value.code || ''
  editForm.address = selectedData.value.address || ''
  editForm.timeZoneId = selectedData.value.timeZoneId || 'Asia/Ho_Chi_Minh'
  editForm.sortOrder = selectedData.value.sortOrder ?? 0
  editForm.securityLevel = selectedData.value.securityLevel || 'Normal'
  editForm.isRestricted = !!selectedData.value.isRestricted
  editForm.type = selectedData.value.type || 'Door'
  editForm.directionMode = selectedData.value.directionMode || 'Bidirectional'
  editForm.isActive = selectedData.value.isActive !== false
  showEditModal.value = true
}

async function saveChild() {
  const parent = selectedNode.value
  const parentData = selectedData.value
  if (childType.value !== 'company' && (!parent || !parentData)) return
  if (!addForm.name) return

  try {
    switch (childType.value) {
      case 'company':
        await enterpriseApi.createCompany({
          name: addForm.name,
          code: addForm.code || addForm.name.substring(0, 3).toUpperCase(),
        })
        break
      case 'site':
        await enterpriseApi.createSite({
          companyId: parent.id,
          name: addForm.name,
          code: addForm.code || addForm.name.substring(0, 3).toUpperCase(),
        })
        break
      case 'building':
        await enterpriseApi.createBuilding({
          siteId: parent.id,
          name: addForm.name,
          code: addForm.code || addForm.name.substring(0, 3).toUpperCase(),
        })
        break
      case 'floor':
        await enterpriseApi.createFloor({
          buildingId: parent.id,
          name: addForm.name,
          code: addForm.code || addForm.name.substring(0, 3).toUpperCase(),
          sortOrder: addForm.sortOrder,
        })
        break
      case 'zone':
        await enterpriseApi.createZone({
          siteId: parent.id,
          buildingId: null,
          facilityFloorId: null,
          name: addForm.name,
          code: addForm.code || addForm.name.substring(0, 3).toUpperCase(),
          securityLevel: addForm.securityLevel,
          isRestricted: addForm.isRestricted,
        })
        break
      case 'accesspoint':
        await enterpriseApi.createAccessPoint({
          siteId: parentData.siteId,
          securityZoneId: parent.id,
          name: addForm.name,
          type: addForm.type,
          directionMode: 'Bidirectional',
        })
        break
    }
    showAddModal.value = false
    await loadHierarchy()
  } catch (err) {
    alert(err.response?.data?.message || 'Failed to create')
  }
}

async function saveEditNode() {
  if (!selectedNode.value || !selectedData.value || !editForm.name) return

  try {
    switch (editNodeType.value) {
      case 'company':
        await enterpriseApi.updateCompany(selectedNode.value.id, {
          name: editForm.name,
          code: editForm.code || editForm.name.substring(0, 3).toUpperCase(),
          isActive: editForm.isActive,
        })
        break
      case 'site':
        await enterpriseApi.updateSite(selectedNode.value.id, {
          name: editForm.name,
          code: editForm.code || editForm.name.substring(0, 3).toUpperCase(),
          address: editForm.address || null,
          timeZoneId: editForm.timeZoneId || 'Asia/Ho_Chi_Minh',
          isActive: editForm.isActive,
        })
        break
      case 'building':
        await enterpriseApi.updateBuilding(selectedNode.value.id, {
          name: editForm.name,
          code: editForm.code || editForm.name.substring(0, 3).toUpperCase(),
          isActive: editForm.isActive,
        })
        break
      case 'floor':
        await enterpriseApi.updateFloor(selectedNode.value.id, {
          name: editForm.name,
          code: editForm.code || editForm.name.substring(0, 3).toUpperCase(),
          sortOrder: editForm.sortOrder,
          isActive: editForm.isActive,
        })
        break
      case 'zone':
        await enterpriseApi.updateZone(selectedNode.value.id, {
          name: editForm.name,
          code: editForm.code || editForm.name.substring(0, 3).toUpperCase(),
          siteId: selectedData.value.siteId,
          buildingId: selectedData.value.buildingId || null,
          facilityFloorId: selectedData.value.facilityFloorId || null,
          securityLevel: editForm.securityLevel,
          isRestricted: editForm.isRestricted,
          isActive: editForm.isActive,
        })
        break
      case 'accesspoint':
        await enterpriseApi.updateAccessPoint(selectedNode.value.id, {
          name: editForm.name,
          siteId: selectedData.value.siteId,
          securityZoneId: selectedData.value.securityZoneId || null,
          type: editForm.type,
          directionMode: editForm.directionMode,
          isActive: editForm.isActive,
        })
        break
    }
    showEditModal.value = false
    await loadHierarchy()
    await loadSelectedNodeHistory()
  } catch (err) {
    alert(err.response?.data?.message || 'Failed to save changes')
  }
}

async function deleteSelectedNode() {
  if (!selectedNode.value || !selectedData.value) return
  const actionLabel = selectedData.value.isActive === false ? 'restore' : 'deactivate'
  const ok = confirm(`${actionLabel[0].toUpperCase()}${actionLabel.slice(1)} ${selectedNodeLabel.value.toLowerCase()} "${selectedData.value.name}"?`)
  if (!ok) return

  try {
    if (selectedData.value.isActive === false) {
      switch (selectedNode.value.type) {
        case 'company':
          await enterpriseApi.restoreCompany(selectedNode.value.id)
          break
        case 'site':
          await enterpriseApi.restoreSite(selectedNode.value.id)
          break
        case 'building':
          await enterpriseApi.restoreBuilding(selectedNode.value.id)
          break
        case 'floor':
          await enterpriseApi.restoreFloor(selectedNode.value.id)
          break
        case 'zone':
          await enterpriseApi.restoreZone(selectedNode.value.id)
          break
        case 'accesspoint':
          await enterpriseApi.restoreAccessPoint(selectedNode.value.id)
          break
        default:
          return
      }
    } else {
      switch (selectedNode.value.type) {
        case 'company':
          await enterpriseApi.deleteCompany(selectedNode.value.id)
          break
        case 'site':
          await enterpriseApi.deleteSite(selectedNode.value.id)
          break
        case 'building':
          await enterpriseApi.deleteBuilding(selectedNode.value.id)
          break
        case 'floor':
          await enterpriseApi.deleteFloor(selectedNode.value.id)
          break
        case 'zone':
          await enterpriseApi.deleteZone(selectedNode.value.id)
          break
        case 'accesspoint':
          await enterpriseApi.deleteAccessPoint(selectedNode.value.id)
          break
        default:
          return
      }
    }
    await loadHierarchy()
    await loadSelectedNodeHistory()
  } catch (err) {
    alert(err.response?.data?.message || `Failed to ${actionLabel} node`)
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

function normalizeHierarchy(companies) {
  const normalized = (companies || []).map((company) => ({
    ...company,
    sites: (company.sites || []).map((site) => ({
      ...site,
      companyId: company.companyId,
      companyName: company.name,
      buildings: (site.buildings || []).map((building) => ({
        ...building,
        siteId: site.siteId,
        siteName: site.name,
        floors: (building.floors || []).map((floor) => ({
          ...floor,
          buildingId: building.buildingId,
          buildingName: building.name,
          siteId: site.siteId,
          siteName: site.name,
        })),
      })),
      zones: (site.zones || []).map((zone) => ({
        ...zone,
        siteId: site.siteId,
        siteName: site.name,
        accessPoints: (zone.accessPoints || []).map((accessPoint) => ({
          ...accessPoint,
          siteId: site.siteId,
          siteName: site.name,
          securityZoneId: zone.securityZoneId,
          securityZoneName: zone.name,
        })),
      })),
    })),
  }))
  if (!expandedNodes.value.company.size && normalized.length) {
    expandedNodes.value.company = new Set(normalized.map((company) => company.companyId))
  }
  return normalized
}

function findNodeRecord(type, id) {
  for (const company of hierarchy.value) {
    if (type === 'company' && company.companyId === id) return company
    for (const site of company.sites || []) {
      if (type === 'site' && site.siteId === id) return site
      for (const building of site.buildings || []) {
        if (type === 'building' && building.buildingId === id) return building
        for (const floor of building.floors || []) {
          if (type === 'floor' && floor.facilityFloorId === id) return floor
        }
      }
      for (const zone of site.zones || []) {
        if (type === 'zone' && zone.securityZoneId === id) return zone
        for (const accessPoint of zone.accessPoints || []) {
          if (type === 'accesspoint' && accessPoint.accessPointId === id) return accessPoint
        }
      }
    }
  }
  return null
}

function percent(value, total) {
  if (!total) return 0
  return Math.round((value / total) * 100)
}

function normalizeAssetMap(data) {
  return {
    gates: data?.gates || [],
    cameras: data?.cameras || [],
    vehicles: data?.vehicles || [],
  }
}

function getAuditEntityMeta(type, id) {
  const entityNames = {
    company: 'Company',
    site: 'Site',
    building: 'Building',
    floor: 'FacilityFloor',
    zone: 'SecurityZone',
    accesspoint: 'AccessPoint',
  }
  return {
    entityName: entityNames[type] || null,
    entityId: id != null ? String(id) : null,
  }
}

async function loadSelectedNodeHistory() {
  if (!selectedNode.value?.type || selectedNode.value?.id == null) {
    selectedHistory.value = []
    return
  }

  const { entityName, entityId } = getAuditEntityMeta(selectedNode.value.type, selectedNode.value.id)
  if (!entityName || !entityId) {
    selectedHistory.value = []
    return
  }

  historyLoading.value = true
  try {
    const res = await getSystemAuditLogs({
      page: 1,
      pageSize: 8,
      entityName,
      entityId,
    })
    selectedHistory.value = res.data?.items || []
  } catch {
    selectedHistory.value = []
  } finally {
    historyLoading.value = false
  }
}

function formatAuditTimestamp(value) {
  if (!value) return 'Unknown time'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return 'Unknown time'
  return date.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function needsAttention(node, type) {
  if (!showNeedsAttentionOnly.value) {
    if (type === 'company') return !node.sites?.length
    if (type === 'site') return !node.buildings?.length || !node.zones?.length
    if (type === 'building') return !node.floors?.length
    if (type === 'zone') return !node.accessPoints?.length
    if (type === 'floor') return !node.code
    if (type === 'accesspoint') return node.isActive === false
    return false
  }
  if (type === 'company') return !node.sites?.length || filteredSites(node.sites || []).length > 0
  if (type === 'site') return !node.buildings?.length || !node.zones?.length
  if (type === 'building') return !node.floors?.length
  if (type === 'zone') return !node.accessPoints?.length
  if (type === 'floor') return !node.code
  if (type === 'accesspoint') return node.isActive === false
  return false
}

function filteredSites(items) {
  return showNeedsAttentionOnly.value ? items.filter((item) => needsAttention(item, 'site') || filteredBuildings(item.buildings || []).length > 0 || filteredZones(item.zones || []).length > 0) : items
}

function filteredBuildings(items) {
  return showNeedsAttentionOnly.value ? items.filter((item) => needsAttention(item, 'building') || filteredFloors(item.floors || []).length > 0) : items
}

function filteredFloors(items) {
  return showNeedsAttentionOnly.value ? items.filter((item) => needsAttention(item, 'floor')) : items
}

function filteredZones(items) {
  return showNeedsAttentionOnly.value ? items.filter((item) => needsAttention(item, 'zone') || filteredAccessPoints(item.accessPoints || []).length > 0) : items
}

function filteredAccessPoints(items) {
  return showNeedsAttentionOnly.value ? items.filter((item) => needsAttention(item, 'accesspoint')) : items
}

function siteHasVisibleChildren(site) {
  return filteredBuildings(site.buildings || []).length > 0 || filteredZones(site.zones || []).length > 0
}

onMounted(loadAll)
</script>

<style scoped>
.site-hierarchy-page {
  max-width: 1380px;
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.hero-panel {
  display: grid;
  grid-template-columns: minmax(0, 1.6fr) minmax(260px, 0.8fr);
  gap: 18px;
  padding: 24px 26px;
  border-radius: 28px;
  background:
    radial-gradient(circle at top right, rgba(15, 118, 110, 0.16), transparent 30%),
    linear-gradient(135deg, #f8fbfd 0%, #eef7f6 52%, #f6f8fb 100%);
  border: 1px solid rgba(148, 163, 184, 0.22);
  box-shadow: 0 18px 45px rgba(15, 23, 42, 0.06);
}

.hero-copy {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.hero-text {
  max-width: 760px;
  margin: 0;
  color: #526277;
  line-height: 1.65;
}

.hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.hero-side {
  display: grid;
  gap: 12px;
  align-content: start;
}

.hero-stat {
  padding: 16px 18px;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.88);
  border: 1px solid rgba(148, 163, 184, 0.18);
}

.hero-stat strong {
  display: block;
  font-size: 1.5rem;
  color: #0f172a;
}

.hero-stat span {
  display: block;
  margin-top: 4px;
  color: #64748b;
  font-size: 0.88rem;
}

.hero-stat.highlight {
  background: linear-gradient(135deg, #0f766e 0%, #0f766e 14%, #155e75 100%);
}

.hero-stat.highlight strong,
.hero-stat.highlight span {
  color: #f8fafc;
}

.metric-strip {
  display: grid;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  gap: 14px;
}

.metric-card {
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(148, 163, 184, 0.16);
  background: #fff;
  box-shadow: 0 12px 32px rgba(15, 23, 42, 0.05);
}

.metric-label {
  display: block;
  color: #64748b;
  font-size: 0.78rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.metric-value {
  display: block;
  margin-top: 8px;
  font-size: 1.35rem;
  color: #0f172a;
}

.metric-note {
  display: block;
  margin-top: 6px;
  color: #94a3b8;
  font-size: 0.8rem;
}

.workspace-shell {
  display: grid;
  grid-template-columns: minmax(320px, 380px) minmax(0, 1fr);
  gap: 18px;
}

.navigator-panel,
.detail-panel,
.empty-state-card,
.asset-overview-card,
.advanced-tools-panel {
  border-radius: 24px;
  border: 1px solid rgba(148, 163, 184, 0.16);
  background: #fff;
  box-shadow: 0 18px 45px rgba(15, 23, 42, 0.05);
}

.navigator-panel {
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 720px;
}

.navigator-toolbar,
.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  justify-content: space-between;
  flex-wrap: wrap;
}

.attention-toggle {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  color: #475569;
  font-size: 0.84rem;
}

.attention-toggle input {
  width: 16px;
  height: 16px;
}

.panel-section-header {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 12px;
}

.panel-section-header h2,
.panel-section-header h3 {
  margin: 2px 0 0;
  font-size: 1.05rem;
  color: #0f172a;
}

.panel-section-header.compact {
  margin-bottom: 14px;
}

.section-kicker {
  color: #0f766e;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.search-box {
  display: grid;
  gap: 8px;
  color: #475569;
  font-size: 0.88rem;
}

.search-box input,
.backfill-grid input,
.form-grid input,
.form-grid select {
  width: 100%;
  min-height: 42px;
  padding: 0 14px;
  border-radius: 14px;
  border: 1px solid #d8e1ea;
  background: #f8fafc;
  color: #0f172a;
}

.search-results {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.search-hit {
  display: grid;
  gap: 4px;
  padding: 12px 14px;
  text-align: left;
  border: 1px solid #dbe6ef;
  border-radius: 14px;
  background: linear-gradient(180deg, #fcfefe 0%, #f5fbfb 100%);
  cursor: pointer;
}

.search-hit strong {
  color: #0f172a;
}

.hit-meta,
.hit-parent,
.empty-inline {
  color: #64748b;
  font-size: 0.82rem;
}

.tree-root {
  display: flex;
  flex-direction: column;
  gap: 10px;
  overflow: auto;
  padding-right: 4px;
}

.tree-branch {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.node-stack {
  display: flex;
  align-items: stretch;
  gap: 8px;
}

.expand-btn {
  width: 32px;
  border: 1px solid #dbe6ef;
  border-radius: 12px;
  background: #fff;
  color: #0f766e;
  font-weight: 700;
  cursor: pointer;
  flex-shrink: 0;
}

.tree-children {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-left: 18px;
  border-left: 1px dashed #d7e3ec;
  margin-left: 10px;
}

.node-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 11px 12px;
  border-radius: 16px;
  border: 1px solid transparent;
  background: #f8fafc;
  color: #0f172a;
  cursor: pointer;
  transition: all 0.18s ease;
}

.node-row:hover {
  border-color: rgba(15, 118, 110, 0.16);
  background: #f3fbfa;
  transform: translateX(2px);
}

.node-row.active {
  background: linear-gradient(135deg, rgba(15, 118, 110, 0.13), rgba(21, 94, 117, 0.08));
  border-color: rgba(15, 118, 110, 0.28);
}

.node-row.attention {
  border-color: rgba(245, 158, 11, 0.32);
  background: linear-gradient(135deg, rgba(245, 158, 11, 0.08), rgba(251, 191, 36, 0.04));
}

.node-icon {
  display: inline-flex;
  width: 28px;
  height: 28px;
  align-items: center;
  justify-content: center;
  border-radius: 10px;
  background: #ffffff;
  color: #0f766e;
  flex-shrink: 0;
}

.node-main {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.node-main strong {
  font-size: 0.92rem;
}

.node-main small,
.node-count {
  color: #64748b;
  font-size: 0.78rem;
}

.node-count {
  margin-left: auto;
  white-space: nowrap;
}

.node-badge {
  padding: 3px 8px;
  border-radius: 999px;
  background: rgba(245, 158, 11, 0.12);
  color: #b45309;
  font-size: 0.72rem;
  font-weight: 700;
}

.detail-panel-shell {
  min-height: 720px;
}

.detail-panel,
.empty-state-card {
  height: 100%;
  padding: 22px;
}

.detail-panel {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.detail-hero {
  display: flex;
  justify-content: space-between;
  align-items: start;
  gap: 18px;
  padding: 20px;
  border-radius: 22px;
  background: linear-gradient(135deg, #f8fbff 0%, #f5faf9 100%);
  border: 1px solid #e3ebf2;
}

.detail-type-row,
.detail-hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.detail-hero h2 {
  margin: 10px 0 6px;
  font-size: 1.55rem;
  color: #0f172a;
}

.detail-description,
.modal-copy,
.advanced-copy,
.empty-state-card p,
.asset-overview-card p,
.asset-note {
  margin: 0;
  color: #5a6b80;
  line-height: 1.6;
}

.detail-stat-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.detail-stat-card,
.insight-card {
  padding: 16px 18px;
  border-radius: 18px;
  border: 1px solid #e2e8f0;
  background: #fff;
}

.detail-stat-card span,
.insight-card span {
  display: block;
  color: #64748b;
  font-size: 0.82rem;
}

.detail-stat-card strong,
.insight-card strong {
  display: block;
  margin-top: 8px;
  color: #0f172a;
  font-size: 1.2rem;
}

.detail-content-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.25fr) minmax(280px, 0.75fr);
  gap: 16px;
}

.detail-card {
  padding: 20px;
  border-radius: 22px;
  border: 1px solid #e2e8f0;
  background: #fff;
}

.detail-card.primary {
  background: linear-gradient(180deg, #ffffff 0%, #fbfdff 100%);
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.detail-field {
  padding: 14px;
  border-radius: 16px;
  background: #f8fafc;
  border: 1px solid #ebf0f5;
}

.detail-label {
  display: block;
  color: #64748b;
  font-size: 0.8rem;
  margin-bottom: 6px;
}

.action-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.action-tip {
  display: grid;
  gap: 6px;
  padding: 14px;
  border-radius: 16px;
  background: #f8fafc;
  border: 1px solid #e6edf3;
}

.action-tip strong {
  color: #0f172a;
}

.action-tip span {
  color: #64748b;
  line-height: 1.55;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.history-item {
  display: grid;
  gap: 6px;
  padding: 14px;
  border-radius: 16px;
  background: #f8fafc;
  border: 1px solid #e6edf3;
}

.history-item-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.history-item p,
.history-item small,
.history-empty {
  margin: 0;
  color: #64748b;
  line-height: 1.55;
}

.history-reason {
  color: #b45309;
}

.empty-state-card {
  display: grid;
  align-content: center;
  justify-items: start;
  gap: 10px;
  background: linear-gradient(135deg, #ffffff 0%, #f8fbfd 100%);
}

.empty-state-card h2 {
  margin: 0;
  color: #0f172a;
}

.asset-workspace {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.asset-overview-card,
.advanced-tools-panel {
  padding: 22px;
}

.asset-overview-card {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: start;
}

.asset-overview-card h2 {
  margin: 4px 0 8px;
  color: #0f172a;
}

.asset-status-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}

.asset-card {
  padding: 20px;
  border-radius: 22px;
  border: 1px solid rgba(148, 163, 184, 0.16);
  background: #fff;
  box-shadow: 0 16px 40px rgba(15, 23, 42, 0.05);
}

.asset-card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 14px;
}

.asset-card h3 {
  margin: 0;
  color: #0f172a;
}

.asset-bar {
  height: 10px;
  border-radius: 999px;
  background: #e2e8f0;
  overflow: hidden;
}

.asset-bar-fill {
  height: 100%;
  border-radius: 999px;
  transition: width 0.35s ease;
}

.asset-bar-fill.mapped {
  background: linear-gradient(90deg, #0f766e 0%, #22c55e 100%);
}

.asset-numbers {
  display: flex;
  justify-content: space-between;
  gap: 10px;
  margin-top: 10px;
  color: #64748b;
  font-size: 0.84rem;
}

.asset-numbers .mapped {
  color: #0f766e;
}

.asset-insight-strip {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
}

.unmapped-section {
  padding: 22px;
  border-radius: 24px;
  border: 1px solid rgba(148, 163, 184, 0.16);
  background: #fff;
  box-shadow: 0 18px 45px rgba(15, 23, 42, 0.05);
}

.unmapped-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
  margin-top: 16px;
}

.unmapped-card {
  padding: 16px;
  border-radius: 18px;
  border: 1px solid #e2e8f0;
  background: linear-gradient(180deg, #fffdf8 0%, #ffffff 100%);
}

.unmapped-top {
  display: flex;
  justify-content: space-between;
  gap: 10px;
  align-items: center;
  margin-bottom: 8px;
}

.unmapped-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 12px;
}

.backfill-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
  margin-top: 16px;
}

.backfill-grid label,
.form-grid label {
  display: grid;
  gap: 8px;
  color: #475569;
  font-size: 0.88rem;
}

.advanced-actions {
  margin-top: 16px;
}

.backfill-result {
  margin-top: 16px;
  padding: 16px;
  border-radius: 18px;
  background: rgba(34, 197, 94, 0.08);
  border: 1px solid rgba(34, 197, 94, 0.2);
}

.backfill-stats {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 8px;
  color: #4b5563;
  font-size: 0.84rem;
}

.modern-modal {
  max-width: 560px;
  border-radius: 24px;
}

.soft-chip.warning {
  background: rgba(245, 158, 11, 0.12);
  color: #b45309;
}

.checkbox-label {
  display: flex !important;
  flex-direction: row !important;
  align-items: center;
  gap: 10px;
}

.checkbox-label input[type="checkbox"] {
  width: 18px;
  height: 18px;
}

@media (max-width: 1180px) {
  .metric-strip,
  .asset-status-grid,
  .asset-insight-strip,
  .detail-stat-grid,
  .unmapped-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .detail-content-grid,
  .hero-panel,
  .workspace-shell {
    grid-template-columns: 1fr;
  }

  .navigator-panel,
  .detail-panel-shell {
    min-height: auto;
  }
}

@media (max-width: 760px) {
  .site-hierarchy-page {
    gap: 14px;
  }

  .hero-panel,
  .asset-overview-card,
  .detail-hero {
    padding: 18px;
  }

  .metric-strip,
  .asset-status-grid,
  .asset-insight-strip,
  .detail-stat-grid,
  .detail-grid,
  .backfill-grid,
  .unmapped-grid {
    grid-template-columns: 1fr;
  }

  .detail-hero,
  .asset-overview-card {
    flex-direction: column;
  }

  .workspace-shell {
    gap: 14px;
  }

  .navigator-panel,
  .detail-panel,
  .empty-state-card,
  .advanced-tools-panel {
    padding: 16px;
  }
}
</style>
