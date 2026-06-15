<template>
  <div class="page-container policy-engine animate-in">
    <div class="page-header-bar">
      <div>
        <span class="panel-kicker">Access Policy Engine 2.0</span>
        <h1 class="page-title">Policy Designer &amp; Control</h1>
      </div>
      <div class="header-actions">
        <span v-if="activeEmergency" class="status-pill danger">LOCKDOWN ACTIVE</span>
        <span class="role-badge" :class="`role--${currentRole.toLowerCase()}`">{{ currentRole }}</span>
        <button type="button" class="btn btn-sm btn-secondary" :disabled="loading" @click="loadAll">Refresh</button>
      </div>
    </div>

    <!-- Layer Navigation -->
    <div class="workspace-tabs layer-tabs">
      <button type="button" :class="{ active: layer === 'design' }" @click="layer = 'design'">
        <span class="layer-icon">&#9997;</span>
        <span>Thiết kế Policy</span>
        <span class="layer-role">Admin</span>
      </button>
      <button type="button" :class="{ active: layer === 'operations' }" @click="layer = 'operations'">
        <span class="layer-icon">&#9881;</span>
        <span>Điều hành Access</span>
        <span class="layer-role">Admin / QuanLy</span>
      </button>
      <button type="button" :class="{ active: layer === 'emergency' }" @click="layer = 'emergency'">
        <span class="layer-icon">&#9888;</span>
        <span>Tình trạng khẩn cấp</span>
        <span class="layer-role">Admin</span>
      </button>
      <button type="button" :class="{ active: layer === 'monitor' }" @click="layer = 'monitor'; loadDuress(); loadEmergencies()">
        <span class="layer-icon">&#128200;</span>
        <span>Giám sát &amp; Hậu kiểm</span>
        <span class="layer-role">Admin / QuanLy / BaoVe</span>
      </button>
    </div>

    <!-- LAYER 1: Policy Design -->
    <section v-if="layer === 'design'" class="soc-section">
      <div class="section-toolbar">
        <h2>Policy Design</h2>
        <span class="soft-chip danger">Chỉ Admin</span>
      </div>
      <div class="sub-tabs">
        <button :class="{ active: designTab === 'versions' }" @click="designTab = 'versions'; loadVersions()">Versions</button>
        <button :class="{ active: designTab === 'rules' }" @click="designTab = 'rules'; loadRules()">Rules</button>
        <button :class="{ active: designTab === 'access-levels' }" @click="designTab = 'access-levels'">Access Levels</button>
        <button :class="{ active: designTab === 'groups' }" @click="designTab = 'groups'">Access Groups</button>
        <button :class="{ active: designTab === 'schedules' }" @click="designTab = 'schedules'">Schedules</button>
        <button :class="{ active: designTab === 'simulator' }" @click="designTab = 'simulator'">Simulator</button>
      </div>

      <!-- Versions (existing) -->
      <div v-if="designTab === 'versions'" class="section-content">
        <div class="section-toolbar"><h3>Policy Version Lifecycle</h3><button class="btn btn-primary btn-sm" @click="showCreateVersion = true">New Version</button></div>
        <div v-if="versions.length === 0" class="empty-card">No policy versions yet.</div>
        <div v-else class="version-list">
          <div v-for="v in versions" :key="v.accessPolicyVersionId" class="version-row">
            <div class="version-info"><strong>{{ v.name }}</strong><span class="version-meta">Status: {{ v.status }} &middot; Rules: {{ v.ruleCount || 0 }}</span></div>
            <div class="version-badges">
              <span class="badge" :class="statusClass(v.status)">{{ v.status }}</span>
              <button v-if="v.status === 'Draft'" class="btn btn-xs btn-secondary" @click="submitVersion(v)">Submit</button>
              <button v-if="v.status === 'PendingApproval'" class="btn btn-xs btn-primary" @click="approveVersion(v)">Approve</button>
              <button v-if="v.status === 'Approved'" class="btn btn-xs btn-success" @click="activateVersion(v)">Activate</button>
              <button v-if="v.status === 'Active'" class="btn btn-xs btn-secondary" @click="retireVersion(v)">Retire</button>
            </div>
          </div>
        </div>
      </div>

      <!-- Rules (existing) -->
      <div v-if="designTab === 'rules'" class="section-content">
        <div class="section-toolbar"><h3>Access Rules</h3><button class="btn btn-primary btn-sm" @click="showAddRule = true">Add Rule</button></div>
        <div v-if="rules.length === 0" class="empty-card">No rules.</div>
        <div v-else class="rule-list">
          <div v-for="r in rules" :key="r.accessRuleId" class="rule-row">
            <div class="rule-info"><strong>{{ r.allowAccess ? 'ALLOW' : 'DENY' }}</strong><span class="rule-detail">{{ r.subjectType }}:{{ r.subjectId || '*' }} &middot; {{ r.credentialType }}</span></div>
            <div class="rule-scope"><span v-if="r.siteId">Site {{ r.siteId }}</span><span v-if="r.securityZoneId">Zone {{ r.securityZoneId }}</span></div>
            <span class="badge" :class="r.isActive ? 'badge-green' : 'badge-gray'">{{ r.isActive ? 'Active' : 'Inactive' }}</span>
          </div>
        </div>
      </div>

      <!-- Access Levels -->
      <div v-if="designTab === 'access-levels'" class="section-content">
        <div class="section-toolbar"><h3>Access Levels</h3><button class="btn btn-primary btn-sm" @click="showAccessLevelForm = true">Create Level</button></div>
        <div class="form-inline" style="margin-bottom: 12px;">
          <input v-model="accessLevelForm.name" placeholder="Level name (e.g. VIP, Employee, Contractor)" class="form-control" style="flex:1;" />
          <input v-model.number="accessLevelForm.priority" type="number" placeholder="Priority" class="form-control" style="width:100px;" />
          <button class="btn btn-primary btn-sm" :disabled="!accessLevelForm.name" @click="createAccessLevel">Create</button>
        </div>
        <div v-if="accessLevels.length === 0" class="empty-card">No access levels. Create one above.</div>
        <div v-else class="compact-list">
          <div v-for="lvl in accessLevels" :key="lvl.accessLevelId" class="compact-row">
            <span class="compact-label">{{ lvl.name }}</span>
            <span class="compact-meta">Priority: {{ lvl.priority || 0 }}</span>
          </div>
        </div>
      </div>

      <!-- Access Groups -->
      <div v-if="designTab === 'groups'" class="section-content">
        <div class="section-toolbar"><h3>Access Groups</h3><button class="btn btn-primary btn-sm" @click="showAccessGroupForm = true">Create Group</button></div>
        <div class="form-inline" style="margin-bottom: 12px;">
          <input v-model="accessGroupForm.name" placeholder="Group name" class="form-control" style="flex:1;" />
          <input v-model.number="accessGroupForm.accessLevelId" type="number" placeholder="Access Level ID" class="form-control" style="width:140px;" />
          <button class="btn btn-primary btn-sm" :disabled="!accessGroupForm.name" @click="createAccessGroup">Create</button>
        </div>
        <div v-if="accessGroups.length === 0" class="empty-card">No access groups.</div>
        <div v-else class="compact-list">
          <div v-for="g in accessGroups" :key="g.accessGroupId" class="compact-row">
            <span class="compact-label">{{ g.name }}</span>
            <span v-if="g.accessLevelId" class="compact-meta">Level: {{ g.accessLevelId }}</span>
          </div>
        </div>
      </div>

      <!-- Schedules -->
      <div v-if="designTab === 'schedules'" class="section-content">
        <div class="section-toolbar"><h3>Schedules</h3><button class="btn btn-primary btn-sm" @click="showScheduleForm = true">Create Schedule</button></div>
        <div class="form-inline" style="margin-bottom: 12px;">
          <input v-model="scheduleForm.name" placeholder="Schedule name" class="form-control" style="flex:1;" />
          <input v-model="scheduleForm.startTime" type="time" class="form-control" style="width:140px;" />
          <input v-model="scheduleForm.endTime" type="time" class="form-control" style="width:140px;" />
          <button class="btn btn-primary btn-sm" :disabled="!scheduleForm.name" @click="createSchedule">Create</button>
        </div>
        <div v-if="schedules.length === 0" class="empty-card">No schedules.</div>
        <div v-else class="compact-list">
          <div v-for="s in schedules" :key="s.scheduleId" class="compact-row">
            <span class="compact-label">{{ s.name }}</span>
            <span class="compact-meta">{{ s.startTime || '' }} - {{ s.endTime || '' }}</span>
          </div>
        </div>
      </div>

      <!-- Simulator (existing, moved) -->
      <div v-if="designTab === 'simulator'" class="section-content">
        <div class="section-toolbar"><h3>Policy Simulator</h3></div>
        <div class="simulator-grid">
          <div class="form-grid single">
            <label>Subject Type <select v-model="simForm.subjectType"><option>Employee</option><option>Visitor</option><option>Contractor</option></select></label>
            <label>Subject ID <input v-model.number="simForm.subjectId" type="number" min="1" /></label>
            <label>Site ID <input v-model.number="simForm.siteId" type="number" /></label>
            <label>Zone ID <input v-model.number="simForm.securityZoneId" type="number" /></label>
            <label>Access Point ID <input v-model.number="simForm.accessPointId" type="number" /></label>
            <label>Credential Type <select v-model="simForm.credentialType"><option>Any</option><option>QR</option><option>Bio</option><option>Card</option><option>EmergencyOverride</option></select></label>
            <label class="checkbox-label"><input v-model="simForm.allowHolidayAccess" type="checkbox" /> Allow Holiday Access</label>
            <button class="btn btn-primary btn-sm" :disabled="!simForm.subjectId" @click="runSimulation">Simulate</button>
          </div>
          <div v-if="simResult" class="sim-result">
            <div class="sim-decision" :class="simResult.result === 'Allow' ? 'sim-allow' : 'sim-deny'"><strong>{{ simResult.result }}</strong><span>{{ simResult.reason }}</span></div>
            <div class="sim-meta"><span>Mode: {{ simResult.decisionMode }}</span><span v-if="simResult.accessPolicyVersionId">Version: {{ simResult.accessPolicyVersionId }}</span></div>
          </div>
        </div>
        <!-- Shadow Compare -->
        <div class="section-toolbar" style="margin-top:20px;"><h3>Shadow Compare</h3></div>
        <div class="form-inline">
          <input v-model.number="shadowForm.subjectId" type="number" placeholder="Subject ID" class="form-control" style="width:120px;" />
          <select v-model="shadowForm.subjectType" class="form-control" style="width:140px;"><option>Employee</option><option>Visitor</option></select>
          <button class="btn btn-secondary btn-sm" :disabled="!shadowForm.subjectId" @click="runShadowCompare">Compare</button>
        </div>
        <div v-if="shadowResult" class="shadow-result">
          <div class="shadow-row"><span>New Policy:</span><span :class="shadowResult.newResult === 'Allow' ? 'text-green' : 'text-red'">{{ shadowResult.newResult }}</span></div>
          <div class="shadow-row"><span>Old Policy:</span><span :class="shadowResult.oldResult === 'Allow' ? 'text-green' : 'text-red'">{{ shadowResult.oldResult }}</span></div>
          <div class="shadow-row"><span>Difference:</span><span :class="shadowResult.isDifferent ? 'text-orange' : 'text-green'">{{ shadowResult.isDifferent ? 'CHANGED' : 'No change' }}</span></div>
        </div>
      </div>
    </section>

    <!-- LAYER 2: Operations (Abnormal Access) -->
    <section v-if="layer === 'operations'" class="soc-section">
      <div class="section-toolbar">
        <h2>Điều hành Access bất thường</h2>
        <span class="soft-chip warn">{{ isAdmin ? 'Full access' : isQuanLy ? 'Review only' : 'Request only' }}</span>
      </div>
      <div class="ops-cards">
        <!-- Temporary Grant -->
        <div class="ops-card">
          <div class="ops-card-head">
            <h3>Temporary Grant</h3>
            <span class="soft-chip danger">Chỉ Admin</span>
          </div>
          <p class="ops-card-desc">Cấp quyền tạm thời cho đối tượng không có trong policy.</p>
          <div class="ops-card-form" v-if="isAdmin">
            <input v-model.number="tempGrantForm.subjectId" type="number" placeholder="Subject ID" class="form-control" />
            <input v-model="tempGrantForm.duration" placeholder="Duration (e.g. 2h, 24h)" class="form-control" />
            <textarea v-model="tempGrantForm.reason" rows="2" placeholder="Reason *" class="form-control"></textarea>
            <button class="btn btn-danger btn-sm" :disabled="!tempGrantForm.subjectId || !tempGrantForm.reason" @click="issueTemporaryGrant">Issue Grant</button>
          </div>
          <div v-else class="ops-card-request">
            <p class="text-muted">Bạn không có quyền cấp temporary grant trực tiếp.</p>
            <button class="btn btn-sm btn-secondary" @click="alert('Yêu cầu đã được gửi đến Admin.')">Gửi yêu cầu lên Admin</button>
          </div>
        </div>

        <!-- Anti-Passback Reset -->
        <div class="ops-card">
          <div class="ops-card-head">
            <h3>Anti-Passback Reset</h3>
            <span class="soft-chip danger">Chỉ Admin</span>
          </div>
          <p class="ops-card-desc">Reset trạng thái anti-passback cho đối tượng hoặc điểm truy cập.</p>
          <div class="ops-card-form" v-if="isAdmin">
            <input v-model.number="apResetForm.subjectId" type="number" placeholder="Subject ID" class="form-control" />
            <input v-model.number="apResetForm.accessPointId" type="number" placeholder="Access Point ID" class="form-control" />
            <textarea v-model="apResetForm.reason" rows="2" placeholder="Reason *" class="form-control"></textarea>
            <button class="btn btn-warning btn-sm" :disabled="!apResetForm.reason" @click="resetAntiPassback">Reset</button>
          </div>
          <div v-else class="ops-card-request">
            <p class="text-muted">Bạn không có quyền reset anti-passback trực tiếp.</p>
            <button class="btn btn-sm btn-secondary" @click="alert('Yêu cầu reset anti-passback đã được gửi đến Admin.')">Gửi yêu cầu lên Admin</button>
          </div>
        </div>

        <!-- Occupancy -->
        <div class="ops-card">
          <div class="ops-card-head">
            <h3>Occupancy</h3>
            <span v-if="isAdmin" class="soft-chip">Admin</span>
            <span v-else class="soft-chip muted">Read only</span>
          </div>
          <div v-if="isAdmin" class="ops-card-form">
            <input v-model.number="occForm.siteId" type="number" placeholder="Site ID" class="form-control" />
            <input v-model.number="occForm.securityZoneId" type="number" placeholder="Zone ID" class="form-control" />
            <input v-model.number="occForm.count" type="number" min="0" placeholder="Count" class="form-control" />
            <input v-model.number="occForm.maxAllowed" type="number" min="0" placeholder="Max Allowed" class="form-control" />
            <button class="btn btn-primary btn-sm" @click="recordOccupancy">Record</button>
          </div>
          <div v-else class="ops-card-request">
            <p class="text-muted">Xem số liệu occupancy trong Dashboard.</p>
            <router-link to="/dashboard" class="btn btn-sm btn-secondary">Go to Dashboard</router-link>
          </div>
        </div>
      </div>
    </section>

    <!-- LAYER 3: Emergency -->
    <section v-if="layer === 'emergency'" class="soc-section">
      <div class="section-toolbar">
        <h2>Emergency &amp; Lockdown</h2>
        <span class="soft-chip danger">Chỉ Admin</span>
      </div>
      <div v-if="!isAdmin" class="empty-card">
        <p>Chỉ Admin mới có quyền thực hiện action khẩn cấp.</p>
        <button class="btn btn-sm btn-secondary" @click="alert('Yêu cầu đã được gửi đến Admin.')">Gửi yêu cầu khẩn cấp</button>
      </div>
      <div v-else>
        <div class="section-toolbar">
          <button class="btn btn-danger" @click="showEmergencyForm = true">New Emergency State</button>
        </div>
        <div v-if="emergencies.length === 0" class="empty-card">No active emergencies.</div>
        <div v-else class="emergency-list">
          <div v-for="e in emergencies" :key="e.emergencyStateId" class="emergency-row" :class="e.state === 'FullLockdown' ? 'lockdown' : 'emergency'">
            <div class="emergency-info"><strong>{{ e.state }}</strong><span class="emergency-meta">{{ e.reason }}</span></div>
            <div class="emergency-scope"><span v-if="e.siteId">Site {{ e.siteId }}</span><span v-if="e.securityZoneId">Zone {{ e.securityZoneId }}</span></div>
            <span class="badge badge-red">ACTIVE</span>
          </div>
        </div>
      </div>
    </section>

    <!-- LAYER 4: Monitor -->
    <section v-if="layer === 'monitor'" class="soc-section">
      <div class="section-toolbar">
        <h2>Giám sát &amp; Hậu kiểm</h2>
        <div class="header-actions">
          <span class="soft-chip" :class="currentRole === 'Admin' ? 'danger' : currentRole === 'QuanLy' ? 'warn' : 'muted'">{{ currentRole }}</span>
        </div>
      </div>
      <div class="sub-tabs">
        <button :class="{ active: monitorTab === 'overview' }" @click="monitorTab = 'overview'">Overview</button>
        <button :class="{ active: monitorTab === 'duress' }" @click="monitorTab = 'duress'; loadDuress()">Duress</button>
        <button :class="{ active: monitorTab === 'emergencies' }" @click="monitorTab = 'emergencies'; loadEmergencies()">Emergencies</button>
      </div>

      <div v-if="monitorTab === 'overview'" class="metric-grid">
        <article class="metric-tile"><span class="metric-label">Active Emergencies</span><strong class="metric-value">{{ emergencies.length }}</strong></article>
        <article class="metric-tile"><span class="metric-label">Unacknowledged Duress</span><strong class="metric-value">{{ unacknowledgedDuressCount }}</strong></article>
        <article class="metric-tile"><span class="metric-label">Anti-Passback States</span><strong class="metric-value">{{ overview.antiPassbackStates }}</strong></article>
        <article class="metric-tile"><span class="metric-label">Decisions Today</span><strong class="metric-value">{{ overview.decisions }}</strong></article>
      </div>

      <div v-if="monitorTab === 'duress'" class="section-content">
        <div class="section-toolbar">
          <h3>Duress Events</h3>
          <div><button class="btn btn-sm btn-secondary" @click="loadDuress(false)">All</button><button class="btn btn-sm btn-warning" @click="loadDuress(true)">Unacknowledged</button></div>
        </div>
        <div v-if="duressEvents.length === 0" class="empty-card">No duress events.</div>
        <div v-else class="duress-list">
          <div v-for="e in duressEvents" :key="e.duressEventId" class="duress-row">
            <div class="duress-info"><strong>Duress #{{ e.duressEventId }}</strong><span class="duress-meta">{{ e.credentialType }} &middot; User {{ e.userId }}</span></div>
            <div class="duress-detail">
              <span v-if="e.description">{{ e.description }}</span>
              <span v-if="e.isAcknowledged" class="ack-badge">Acknowledged</span>
              <span v-else class="unack-badge">PENDING</span>
            </div>
            <button v-if="!e.isAcknowledged && isAdmin" class="btn btn-xs btn-primary" @click="acknowledgeDuress(e)">Acknowledge</button>
          </div>
        </div>
      </div>

      <div v-if="monitorTab === 'emergencies'" class="section-content">
        <div class="section-toolbar"><h3>Active Emergencies</h3></div>
        <div v-if="emergencies.length === 0" class="empty-card">No active emergencies.</div>
        <div v-else class="emergency-list">
          <div v-for="e in emergencies" :key="e.emergencyStateId" class="emergency-row">
            <div class="emergency-info"><strong>{{ e.state }}</strong><span class="emergency-meta">{{ e.reason }}</span></div>
            <span class="badge badge-red">ACTIVE</span>
          </div>
        </div>
      </div>
    </section>

    <!-- Modals (existing) -->
    <div v-if="showCreateVersion" class="modal-overlay" @click.self="showCreateVersion = false">
      <div class="modal-content"><div class="modal-header"><h2>New Policy Version</h2><button class="btn-close" @click="showCreateVersion = false">&times;</button></div>
        <div class="modal-body"><div class="form-grid single"><label>Name <input v-model="versionForm.name" required /></label><label>Change Summary <textarea v-model="versionForm.changeSummary" rows="3"></textarea></label></div></div>
        <div class="modal-footer"><button class="btn btn-primary" :disabled="!versionForm.name" @click="createVersion">Create</button><button class="btn btn-secondary" @click="showCreateVersion = false">Cancel</button></div>
      </div>
    </div>

    <div v-if="showAddRule" class="modal-overlay" @click.self="showAddRule = false">
      <div class="modal-content"><div class="modal-header"><h2>New Access Rule</h2><button class="btn-close" @click="showAddRule = false">&times;</button></div>
        <div class="modal-body"><div class="form-grid double">
          <label>Subject Type <select v-model="ruleForm.subjectType"><option>Employee</option><option>Visitor</option><option>Contractor</option></select></label>
          <label>Credential Type <select v-model="ruleForm.credentialType"><option>Any</option><option>QR</option><option>Bio</option><option>Card</option></select></label>
          <label>Subject ID <input v-model.number="ruleForm.subjectId" type="number" /></label>
          <label>Access Level ID <input v-model.number="ruleForm.accessLevelId" type="number" min="1" required /></label>
          <label>Site ID <input v-model.number="ruleForm.siteId" type="number" /></label>
          <label>Zone ID <input v-model.number="ruleForm.securityZoneId" type="number" /></label>
          <label>Point ID <input v-model.number="ruleForm.accessPointId" type="number" /></label>
          <label class="checkbox-label"><input v-model="ruleForm.allowAccess" type="checkbox" /> Allow Access</label>
        </div></div>
        <div class="modal-footer"><button class="btn btn-primary" :disabled="!ruleForm.accessLevelId" @click="createRule">Create</button><button class="btn btn-secondary" @click="showAddRule = false">Cancel</button></div>
      </div>
    </div>

    <div v-if="showEmergencyForm" class="modal-overlay" @click.self="showEmergencyForm = false">
      <div class="modal-content"><div class="modal-header"><h2>New Emergency State</h2><button class="btn-close" @click="showEmergencyForm = false">&times;</button></div>
        <div class="modal-body"><div class="form-grid single">
          <label>State <select v-model="emergencyForm.state"><option>FullLockdown</option><option>PartialLockdown</option><option>Evacuation</option><option>ShelterInPlace</option></select></label>
          <label>Site ID <input v-model.number="emergencyForm.siteId" type="number" /></label>
          <label>Zone ID <input v-model.number="emergencyForm.securityZoneId" type="number" /></label>
          <label>Reason <textarea v-model="emergencyForm.reason" rows="3" required></textarea></label>
        </div></div>
        <div class="modal-footer"><button class="btn btn-danger" :disabled="!emergencyForm.reason" @click="createEmergency">Activate</button><button class="btn btn-secondary" @click="showEmergencyForm = false">Cancel</button></div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'
import { authState } from '../stores/auth'

const loading = ref(false)
const layer = ref('design')
const designTab = ref('versions')
const monitorTab = ref('overview')

// Auth
const currentRole = computed(() => authState.user?.role || 'BaoVe')
const isAdmin = computed(() => currentRole.value === 'Admin')
const isQuanLy = computed(() => currentRole.value === 'QuanLy')

const overview = reactive({ policyVersions: 0, accessRules: 0, pendingApprovalPolicyVersions: 0, emergencyStates: 0, antiPassbackStates: 0, decisions: 0, activePolicyVersions: 0 })
const versions = ref([])
const rules = ref([])
const emergencies = ref([])
const duressEvents = ref([])
const accessLevels = ref([])
const accessGroups = ref([])
const schedules = ref([])
const simResult = ref(null)
const shadowResult = ref(null)

const showCreateVersion = ref(false)
const showAddRule = ref(false)
const showEmergencyForm = ref(false)
const showAccessLevelForm = ref(false)

const versionForm = reactive({ name: '', changeSummary: '' })
const ruleForm = reactive({ subjectType: 'Employee', credentialType: 'Any', subjectId: null, accessLevelId: null, siteId: null, securityZoneId: null, accessPointId: null, allowAccess: true })
const simForm = reactive({ subjectType: 'Employee', subjectId: null, siteId: null, securityZoneId: null, accessPointId: null, credentialType: 'Any', allowHolidayAccess: false })
const occForm = reactive({ siteId: null, securityZoneId: null, count: 0, maxAllowed: null })
const emergencyForm = reactive({ state: 'FullLockdown', siteId: null, securityZoneId: null, reason: '' })
const accessLevelForm = reactive({ name: '', priority: 0 })
const accessGroupForm = reactive({ name: '', accessLevelId: null })
const scheduleForm = reactive({ name: '', startTime: '', endTime: '' })
const tempGrantForm = reactive({ subjectId: null, duration: '24h', reason: '' })
const apResetForm = reactive({ subjectId: null, accessPointId: null, reason: '' })
const shadowForm = reactive({ subjectId: null, subjectType: 'Employee' })

const activeEmergency = computed(() => emergencies.value.length > 0)
const unacknowledgedDuressCount = computed(() => duressEvents.value.filter(e => !e.isAcknowledged).length)

function statusClass(s) {
  const map = { Draft: 'badge-gray', PendingApproval: 'badge-yellow', Approved: 'badge-blue', Active: 'badge-green', Retired: 'badge-gray' }
  return map[s] || 'badge-gray'
}

async function loadAll() {
  loading.value = true
  await Promise.all([loadOverview(), loadVersions(), loadEmergencies()])
  loading.value = false
}

async function loadOverview() {
  try { const res = await enterpriseApi.getPolicyOverview(); Object.assign(overview, res.data) } catch {}
}

async function loadVersions() {
  try { const res = await enterpriseApi.getPolicyVersions(); versions.value = res.data || [] } catch {}
}

async function loadRules() {
  try { const res = await enterpriseApi.getAccessRules(); rules.value = res.data || [] } catch {}
}

async function loadEmergencies() {
  try { const res = await enterpriseApi.getActiveEmergencies(); emergencies.value = res.data || [] } catch {}
}

async function loadDuress(unacknowledged) {
  try { const res = await enterpriseApi.getDuressEvents(unacknowledged); duressEvents.value = res.data || [] } catch {}
}

async function createVersion() {
  if (!versionForm.name) return
  try {
    await enterpriseApi.createPolicyVersion({ name: versionForm.name, changeSummary: versionForm.changeSummary })
    showCreateVersion.value = false; versionForm.name = ''; versionForm.changeSummary = ''
    await loadVersions()
  } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function submitVersion(v) {
  try { await enterpriseApi.submitPolicyVersion(v.accessPolicyVersionId); await loadVersions() } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function approveVersion(v) {
  try {
    await enterpriseApi.approvePolicyVersion(v.accessPolicyVersionId, { note: 'Approved from UI' })
    await loadVersions()
  } catch (err) { alert(err.response?.data?.message || 'Approval failed') }
}

async function activateVersion(v) {
  try { await enterpriseApi.activatePolicyVersion(v.accessPolicyVersionId); await loadVersions() } catch (err) { alert(err.response?.data?.message || 'Activation failed') }
}

async function retireVersion(v) {
  try { await enterpriseApi.retirePolicyVersion(v.accessPolicyVersionId); await loadVersions() } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function createRule() {
  if (!ruleForm.accessLevelId) return
  try {
    await enterpriseApi.createAccessRule({ ...ruleForm })
    showAddRule.value = false; ruleForm.accessLevelId = null; ruleForm.subjectId = null
    await loadRules()
  } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function createAccessLevel() {
  if (!accessLevelForm.name) return
  const name = accessLevelForm.name.trim()
  const priority = accessLevelForm.priority || 0
  try {
    await enterpriseApi.createAccessLevel({ name, priority })
    accessLevels.value = [...accessLevels.value, { accessLevelId: Date.now(), name, priority }]
    accessLevelForm.name = ''; accessLevelForm.priority = 0
    alert('Access level created.')
  } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function createAccessGroup() {
  if (!accessGroupForm.name) return
  try {
    await enterpriseApi.createAccessGroup({ name: accessGroupForm.name, accessLevelId: accessGroupForm.accessLevelId || null })
    accessGroupForm.name = ''; accessGroupForm.accessLevelId = null
    alert('Access group created.')
  } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function createSchedule() {
  if (!scheduleForm.name) return
  try {
    await enterpriseApi.createSchedule({ name: scheduleForm.name, startTime: scheduleForm.startTime || null, endTime: scheduleForm.endTime || null })
    scheduleForm.name = ''; scheduleForm.startTime = ''; scheduleForm.endTime = ''
    alert('Schedule created.')
  } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function runSimulation() {
  try {
    const res = await enterpriseApi.simulateAccess({ ...simForm, evaluatedAtUtc: null })
    simResult.value = res.data
  } catch (err) { alert(err.response?.data?.message || 'Simulation failed') }
}

async function runShadowCompare() {
  if (!shadowForm.subjectId) return
  try {
    const res = await enterpriseApi.shadowCompare({ subjectId: shadowForm.subjectId, subjectType: shadowForm.subjectType })
    shadowResult.value = res.data
  } catch (err) { alert(err.response?.data?.message || 'Shadow compare failed') }
}

async function issueTemporaryGrant() {
  if (!tempGrantForm.subjectId || !tempGrantForm.reason) return
  try {
    await enterpriseApi.createTemporaryGrant({ subjectId: tempGrantForm.subjectId, duration: tempGrantForm.duration || '24h', reason: tempGrantForm.reason })
    tempGrantForm.subjectId = null; tempGrantForm.reason = ''
    alert('Temporary grant issued successfully.')
  } catch (err) { alert(err.response?.data?.message || 'Failed to issue grant') }
}

async function resetAntiPassback() {
  if (!apResetForm.reason) return
  try {
    await enterpriseApi.resetAntiPassback({ subjectId: apResetForm.subjectId || null, accessPointId: apResetForm.accessPointId || null, reason: apResetForm.reason })
    apResetForm.subjectId = null; apResetForm.accessPointId = null; apResetForm.reason = ''
    alert('Anti-passback reset successfully.')
  } catch (err) { alert(err.response?.data?.message || 'Failed to reset') }
}

async function createEmergency() {
  if (!emergencyForm.reason) return
  try {
    await enterpriseApi.createEmergencyState({ ...emergencyForm })
    showEmergencyForm.value = false; emergencyForm.reason = ''
    await loadEmergencies()
  } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function recordOccupancy() {
  try {
    await enterpriseApi.recordOccupancy({ ...occForm })
    alert('Occupancy recorded')
  } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function acknowledgeDuress(e) {
  try {
    await enterpriseApi.acknowledgeDuressEvent(e.duressEventId)
    e.isAcknowledged = true
  } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

onMounted(loadAll)
</script>

<style scoped>
.policy-engine { max-width: 1300px; }
.layer-tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
  border-bottom: 1px solid var(--border-soft, #e2e8f0);
  padding-bottom: 8px;
}
.layer-tabs button {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  border: none;
  background: none;
  font-size: 14px;
  font-weight: 600;
  color: #64748b;
  cursor: pointer;
  border-radius: 10px 10px 0 0;
  border-bottom: 2px solid transparent;
  margin-bottom: -9px;
}
.layer-tabs button.active {
  color: #2563eb;
  border-bottom-color: #2563eb;
  background: #eff6ff;
}
.layer-icon { font-size: 16px; }
.layer-role {
  font-size: 10px;
  font-weight: 700;
  color: #94a3b8;
  padding: 1px 6px;
  border-radius: 4px;
  background: #f1f5f9;
}
.role-badge {
  padding: 2px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 700;
}
.role--admin { background: #fee2e2; color: #991b1b; }
.role--baove { background: #dbeafe; color: #1e40af; }
.role--quanly { background: #fef3c7; color: #92400e; }
.role--staff { background: #f3f4f6; color: #374151; }
.sub-tabs {
  display: flex;
  gap: 4px;
  margin-bottom: 12px;
  border-bottom: 1px solid #e2e8f0;
}
.sub-tabs button {
  padding: 6px 14px;
  border: none;
  background: none;
  font-size: 13px;
  font-weight: 600;
  color: #64748b;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
}
.sub-tabs button.active {
  color: #2563eb;
  border-bottom-color: #2563eb;
}
.section-content { min-height: 200px; }
.ops-cards {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
  gap: 14px;
}
.ops-card {
  padding: 16px;
  border-radius: 14px;
  border: 1px solid var(--border-soft, #e9eef5);
  background: var(--surface, #fff);
}
.ops-card-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}
.ops-card-head h3 { margin: 0; font-size: 16px; font-weight: 800; }
.ops-card-desc { font-size: 13px; color: #64748b; margin-bottom: 12px; }
.ops-card-form {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.ops-card-request {
  padding: 10px;
  background: #fffbeb;
  border: 1px solid #fde68a;
  border-radius: 10px;
  text-align: center;
}
.form-inline {
  display: flex;
  gap: 8px;
  align-items: flex-end;
  flex-wrap: wrap;
}
.compact-list { display: flex; flex-direction: column; gap: 4px; }
.compact-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  border-radius: 8px;
  background: #f8fafc;
  border: 1px solid #f1f5f9;
}
.compact-label { font-weight: 700; font-size: 14px; flex: 1; }
.compact-meta { font-size: 12px; color: #64748b; }
.shadow-result {
  margin-top: 12px;
  padding: 14px;
  background: #f8fafc;
  border-radius: 10px;
  border: 1px solid #e9eef5;
}
.shadow-row { display: flex; gap: 10px; font-size: 14px; padding: 4px 0; }
.text-green { color: #22c55e; font-weight: 700; }
.text-red { color: #ef4444; font-weight: 700; }
.text-orange { color: #f97316; font-weight: 700; }
.text-muted { color: #64748b; font-size: 13px; }
.form-grid.double { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.form-grid.double label { display: flex; flex-direction: column; gap: 5px; font-size: 0.83rem; color: var(--text-secondary); }
.simulator-grid { display: grid; grid-template-columns: 360px 1fr; gap: 20px; }
.sim-result { padding: 18px; border-radius: 14px; border: 1px solid var(--border-soft); }
.sim-decision { display: flex; flex-direction: column; gap: 6px; padding: 14px; border-radius: 10px; font-size: 1.2rem; font-weight: 700; margin-bottom: 10px; }
.sim-allow { background: rgba(34,197,94,.1); color: #22c55e; border: 1px solid rgba(34,197,94,.2); }
.sim-deny { background: rgba(239,68,68,.1); color: #ef4444; border: 1px solid rgba(239,68,68,.2); }
.sim-decision span { font-size: 0.85rem; font-weight: 400; }
.sim-meta { font-size: 0.78rem; color: var(--text-muted); display: flex; gap: 12px; }
.version-list, .rule-list, .emergency-list, .duress-list { display: flex; flex-direction: column; gap: 6px; }
.version-row, .rule-row, .emergency-row, .duress-row { display: flex; align-items: center; justify-content: space-between; padding: 12px 14px; border-radius: 10px; border: 1px solid var(--border-soft); background: var(--surface); gap: 10px; }
.version-info, .rule-info, .emergency-info, .duress-info { display: flex; flex-direction: column; gap: 2px; flex: 1; }
.version-meta, .rule-detail, .emergency-meta, .duress-meta { font-size: 0.8rem; color: var(--text-muted); }
.version-badges { display: flex; gap: 6px; align-items: center; }
.rule-scope { display: flex; gap: 6px; font-size: 0.78rem; color: var(--text-secondary); }
.emergency-row.lockdown { border-color: rgba(239,68,68,.3); background: rgba(239,68,68,.04); }
.emergency-scope { font-size: 0.78rem; color: var(--text-secondary); display: flex; gap: 6px; }
.duress-detail { display: flex; align-items: center; gap: 8px; font-size: 0.82rem; }
.ack-badge { color: #22c55e; font-size: 0.72rem; font-weight: 600; }
.unack-badge { color: #ef4444; font-size: 0.72rem; font-weight: 600; }
.badge { font-size: 0.72rem; padding: 2px 8px; border-radius: 12px; font-weight: 500; }
.badge-green { background: rgba(34,197,94,.15); color: #22c55e; }
.badge-gray { background: rgba(100,116,139,.15); color: #64748b; }
.badge-yellow { background: rgba(234,179,8,.15); color: #eab308; }
.badge-blue { background: rgba(59,130,246,.15); color: #3b82f6; }
.badge-red { background: rgba(239,68,68,.15); color: #ef4444; }
.btn-xs { min-height: 28px; padding: 0 10px; font-size: 0.75rem; border-radius: 8px; }
.btn-success { background: #22c55e; color: #fff; }
.btn-danger { background: #ef4444; color: #fff; }
.btn-warning { background: #f97316; color: #fff; }
.form-grid.single { display: grid; grid-template-columns: 1fr; gap: 14px; }
.form-grid.single label { display: flex; flex-direction: column; gap: 5px; font-size: 0.83rem; color: var(--text-secondary); }
.panel-kicker {
  display: inline-flex; align-items: center; align-self: flex-start; padding: 4px 10px; border-radius: 999px;
  background: rgba(15,124,130,.08); color: var(--accent-primary); font-size: 0.72rem; font-weight: 700; letter-spacing: .08em; text-transform: uppercase;
}
.status-pill.danger { background: rgba(239,68,68,.15); color: #ef4444; padding: 4px 12px; border-radius: 20px; font-size: 0.72rem; font-weight: 700; letter-spacing: 0.05em; }
.checkbox-label { display: flex !important; align-items: center; gap: 8px; cursor: pointer; flex-direction: row !important; }
.checkbox-label input[type="checkbox"] { width: 18px; height: 18px; }
.form-control {
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  padding: 8px 12px;
  font-size: 13px;
  outline: none;
  background: #fff;
  font-family: inherit;
}
.form-control:focus {
  border-color: #60a5fa;
  box-shadow: 0 0 0 3px rgba(37,99,235,0.08);
}
textarea.form-control {
  resize: vertical;
  min-height: 48px;
}
</style>
