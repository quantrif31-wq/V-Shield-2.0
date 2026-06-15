<template>
  <div class="page-container policy-engine animate-in">
    <div class="page-header-bar">
      <div>
        <span class="panel-kicker">Access Policy Engine 2.0</span>
        <h1 class="page-title">Policy Designer &amp; Control</h1>
      </div>
      <div class="header-actions">
        <span v-if="activeEmergency" class="status-pill danger">LOCKDOWN ACTIVE</span>
        <button type="button" class="btn btn-sm btn-secondary" :disabled="loading" @click="loadAll">Refresh</button>
      </div>
    </div>

    <section class="metric-grid">
      <article class="metric-tile">
        <span class="metric-label">Policy Versions</span>
        <strong class="metric-value">{{ overview.policyVersions }}</strong>
        <span class="metric-note">{{ overview.activePolicyVersions }} active</span>
      </article>
      <article class="metric-tile">
        <span class="metric-label">Rules</span>
        <strong class="metric-value">{{ overview.accessRules }}</strong>
      </article>
      <article class="metric-tile">
        <span class="metric-label">Pending Approval</span>
        <strong class="metric-value">{{ overview.pendingApprovalPolicyVersions }}</strong>
      </article>
      <article class="metric-tile">
        <span class="metric-label">Emergency States</span>
        <strong class="metric-value">{{ overview.emergencyStates }}</strong>
        <span class="metric-note">active</span>
      </article>
      <article class="metric-tile">
        <span class="metric-label">Anti-Passback</span>
        <strong class="metric-value">{{ overview.antiPassbackStates }}</strong>
      </article>
      <article class="metric-tile">
        <span class="metric-label">Decisions</span>
        <strong class="metric-value">{{ overview.decisions }}</strong>
      </article>
    </section>

    <section class="workspace-tabs">
      <button type="button" :class="{ active: tab === 'versions' }" @click="tab = 'versions'; loadVersions()">Policy Versions</button>
      <button type="button" :class="{ active: tab === 'rules' }" @click="tab = 'rules'; loadRules()">Rules</button>
      <button type="button" :class="{ active: tab === 'simulator' }" @click="tab = 'simulator'">Simulator</button>
      <button type="button" :class="{ active: tab === 'emergency' }" @click="tab = 'emergency'; loadEmergencies()">Emergency</button>
      <button type="button" :class="{ active: tab === 'occupancy' }" @click="tab = 'occupancy'">Occupancy</button>
      <button type="button" :class="{ active: tab === 'duress' }" @click="tab = 'duress'; loadDuress()">Duress</button>
    </section>

    <section v-if="tab === 'versions'" class="soc-section">
      <div class="section-toolbar">
        <h2>Policy Version Lifecycle</h2>
        <button type="button" class="btn btn-primary btn-sm" @click="showCreateVersion = true">New Version</button>
      </div>
      <div v-if="versions.length === 0" class="empty-card">No policy versions yet.</div>
      <div v-else class="version-list">
        <div v-for="v in versions" :key="v.accessPolicyVersionId" class="version-row">
          <div class="version-info">
            <strong>{{ v.name }}</strong>
            <span class="version-meta">Status: {{ v.status }} &middot; Rules: {{ v.ruleCount || 0 }}</span>
          </div>
          <div class="version-badges">
            <span class="badge" :class="statusClass(v.status)">{{ v.status }}</span>
            <button v-if="v.status === 'Draft'" class="btn btn-xs btn-secondary" @click="submitVersion(v)">Submit</button>
            <button v-if="v.status === 'PendingApproval'" class="btn btn-xs btn-primary" @click="approveVersion(v)">Approve</button>
            <button v-if="v.status === 'Approved'" class="btn btn-xs btn-success" @click="activateVersion(v)">Activate</button>
            <button v-if="v.status === 'Active'" class="btn btn-xs btn-secondary" @click="retireVersion(v)">Retire</button>
          </div>
        </div>
      </div>
    </section>

    <section v-if="tab === 'rules'" class="soc-section">
      <div class="section-toolbar">
        <h2>Access Rules</h2>
        <button type="button" class="btn btn-primary btn-sm" @click="showAddRule = true">Add Rule</button>
      </div>
      <div v-if="rules.length === 0" class="empty-card">No rules. Create a policy version first.</div>
      <div v-else class="rule-list">
        <div v-for="r in rules" :key="r.accessRuleId" class="rule-row">
          <div class="rule-info">
            <strong>{{ r.allowAccess ? 'ALLOW' : 'DENY' }}</strong>
            <span class="rule-detail">{{ r.subjectType }}:{{ r.subjectId || '*' }} &middot; {{ r.credentialType }}</span>
          </div>
          <div class="rule-scope">
            <span v-if="r.siteId">Site {{ r.siteId }}</span>
            <span v-if="r.securityZoneId">Zone {{ r.securityZoneId }}</span>
            <span v-if="r.accessPointId">Point {{ r.accessPointId }}</span>
          </div>
          <span class="badge" :class="r.isActive ? 'badge-green' : 'badge-gray'">{{ r.isActive ? 'Active' : 'Inactive' }}</span>
        </div>
      </div>
    </section>

    <section v-if="tab === 'simulator'" class="soc-section">
      <div class="section-toolbar">
        <h2>Policy Simulator</h2>
      </div>
      <div class="simulator-grid">
        <div class="form-grid single">
          <label>Subject Type <select v-model="simForm.subjectType"><option>Employee</option><option>Visitor</option><option>Contractor</option></select></label>
          <label>Subject ID <input v-model.number="simForm.subjectId" type="number" min="1" /></label>
          <label>Site ID <input v-model.number="simForm.siteId" type="number" /></label>
          <label>Zone ID <input v-model.number="simForm.securityZoneId" type="number" /></label>
          <label>Access Point ID <input v-model.number="simForm.accessPointId" type="number" /></label>
          <label>Credential Type <select v-model="simForm.credentialType"><option>Any</option><option>QR</option><option>Bio</option><option>Card</option><option>EmergencyOverride</option></select></label>
          <label class="checkbox-label"><input v-model="simForm.allowHolidayAccess" type="checkbox" /> Allow Holiday Access</label>
          <button type="button" class="btn btn-primary btn-sm" :disabled="!simForm.subjectId" @click="runSimulation">Simulate</button>
        </div>
        <div v-if="simResult" class="sim-result">
          <div class="sim-decision" :class="simResult.result === 'Allow' ? 'sim-allow' : 'sim-deny'">
            <strong>{{ simResult.result }}</strong>
            <span>{{ simResult.reason }}</span>
          </div>
          <div class="sim-meta">
            <span>Mode: {{ simResult.decisionMode }}</span>
            <span v-if="simResult.accessPolicyVersionId">Version: {{ simResult.accessPolicyVersionId }}</span>
          </div>
        </div>
      </div>
    </section>

    <section v-if="tab === 'emergency'" class="soc-section">
      <div class="section-toolbar">
        <h2>Emergency &amp; Lockdown Control</h2>
        <button type="button" class="btn btn-danger btn-sm" @click="showEmergencyForm = true">New Emergency</button>
      </div>
      <div v-if="emergencies.length === 0" class="empty-card">No active emergencies.</div>
      <div v-else class="emergency-list">
        <div v-for="e in emergencies" :key="e.emergencyStateId" class="emergency-row" :class="e.state === 'FullLockdown' ? 'lockdown' : 'emergency'">
          <div class="emergency-info">
            <strong>{{ e.state }}</strong>
            <span class="emergency-meta">{{ e.reason }}</span>
          </div>
          <div class="emergency-scope">
            <span v-if="e.siteId">Site {{ e.siteId }}</span>
            <span v-if="e.securityZoneId">Zone {{ e.securityZoneId }}</span>
          </div>
          <span class="badge badge-red">ACTIVE</span>
        </div>
      </div>
    </section>

    <section v-if="tab === 'occupancy'" class="soc-section">
      <div class="section-toolbar">
        <h2>Occupancy Tracking</h2>
      </div>
      <div class="form-grid single" style="max-width:400px">
        <label>Site ID <input v-model.number="occForm.siteId" type="number" /></label>
        <label>Zone ID <input v-model.number="occForm.securityZoneId" type="number" /></label>
        <label>Count <input v-model.number="occForm.count" type="number" min="0" /></label>
        <label>Max Allowed <input v-model.number="occForm.maxAllowed" type="number" min="0" /></label>
        <button type="button" class="btn btn-primary btn-sm" @click="recordOccupancy">Record Occupancy</button>
      </div>
    </section>

    <section v-if="tab === 'duress'" class="soc-section">
      <div class="section-toolbar">
        <h2>Duress Events</h2>
        <div class="header-actions">
          <button type="button" class="btn btn-sm btn-secondary" @click="loadDuress(false)">All</button>
          <button type="button" class="btn btn-sm btn-warning" @click="loadDuress(true)">Unacknowledged</button>
        </div>
      </div>
      <div v-if="duressEvents.length === 0" class="empty-card">No duress events.</div>
      <div v-else class="duress-list">
        <div v-for="e in duressEvents" :key="e.duressEventId" class="duress-row">
          <div class="duress-info">
            <strong>Duress #{{ e.duressEventId }}</strong>
            <span class="duress-meta">{{ e.credentialType }} &middot; User {{ e.userId }}</span>
          </div>
          <div class="duress-detail">
            <span v-if="e.description">{{ e.description }}</span>
            <span v-if="e.isAcknowledged" class="ack-badge">Acknowledged</span>
            <span v-else class="unack-badge">PENDING</span>
          </div>
          <button v-if="!e.isAcknowledged" type="button" class="btn btn-xs btn-primary" @click="acknowledgeDuress(e)">Acknowledge</button>
        </div>
      </div>
    </section>

    <div v-if="showCreateVersion" class="modal-overlay" @click.self="showCreateVersion = false">
      <div class="modal-content">
        <div class="modal-header">
          <h2>New Policy Version</h2>
          <button type="button" class="btn-close" @click="showCreateVersion = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-grid single">
            <label>Name <input v-model="versionForm.name" required /></label>
            <label>Change Summary <textarea v-model="versionForm.changeSummary" rows="3"></textarea></label>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-primary" :disabled="!versionForm.name" @click="createVersion">Create</button>
          <button type="button" class="btn btn-secondary" @click="showCreateVersion = false">Cancel</button>
        </div>
      </div>
    </div>

    <div v-if="showAddRule" class="modal-overlay" @click.self="showAddRule = false">
      <div class="modal-content">
        <div class="modal-header">
          <h2>New Access Rule</h2>
          <button type="button" class="btn-close" @click="showAddRule = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-grid double">
            <label>Subject Type <select v-model="ruleForm.subjectType"><option>Employee</option><option>Visitor</option><option>Contractor</option></select></label>
            <label>Credential Type <select v-model="ruleForm.credentialType"><option>Any</option><option>QR</option><option>Bio</option><option>Card</option></select></label>
            <label>Subject ID <input v-model.number="ruleForm.subjectId" type="number" /></label>
            <label>Access Level ID <input v-model.number="ruleForm.accessLevelId" type="number" min="1" required /></label>
            <label>Site ID <input v-model.number="ruleForm.siteId" type="number" /></label>
            <label>Zone ID <input v-model.number="ruleForm.securityZoneId" type="number" /></label>
            <label>Point ID <input v-model.number="ruleForm.accessPointId" type="number" /></label>
            <label class="checkbox-label"><input v-model="ruleForm.allowAccess" type="checkbox" /> Allow Access</label>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-primary" :disabled="!ruleForm.accessLevelId" @click="createRule">Create</button>
          <button type="button" class="btn btn-secondary" @click="showAddRule = false">Cancel</button>
        </div>
      </div>
    </div>

    <div v-if="showEmergencyForm" class="modal-overlay" @click.self="showEmergencyForm = false">
      <div class="modal-content">
        <div class="modal-header">
          <h2>New Emergency State</h2>
          <button type="button" class="btn-close" @click="showEmergencyForm = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-grid single">
            <label>State <select v-model="emergencyForm.state"><option>FullLockdown</option><option>PartialLockdown</option><option>Evacuation</option><option>ShelterInPlace</option></select></label>
            <label>Site ID <input v-model.number="emergencyForm.siteId" type="number" /></label>
            <label>Zone ID <input v-model.number="emergencyForm.securityZoneId" type="number" /></label>
            <label>Reason <textarea v-model="emergencyForm.reason" rows="3" required></textarea></label>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-danger" :disabled="!emergencyForm.reason" @click="createEmergency">Activate</button>
          <button type="button" class="btn btn-secondary" @click="showEmergencyForm = false">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const loading = ref(false)
const tab = ref('versions')
const overview = reactive({ policyVersions: 0, accessRules: 0, pendingApprovalPolicyVersions: 0, emergencyStates: 0, antiPassbackStates: 0, decisions: 0, activePolicyVersions: 0 })
const versions = ref([])
const rules = ref([])
const emergencies = ref([])
const duressEvents = ref([])
const simResult = ref(null)
const showCreateVersion = ref(false)
const showAddRule = ref(false)
const showEmergencyForm = ref(false)

const versionForm = reactive({ name: '', changeSummary: '' })
const ruleForm = reactive({ subjectType: 'Employee', credentialType: 'Any', subjectId: null, accessLevelId: null, siteId: null, securityZoneId: null, accessPointId: null, allowAccess: true })
const simForm = reactive({ subjectType: 'Employee', subjectId: null, siteId: null, securityZoneId: null, accessPointId: null, credentialType: 'Any', allowHolidayAccess: false })
const occForm = reactive({ siteId: null, securityZoneId: null, count: 0, maxAllowed: null })
const emergencyForm = reactive({ state: 'FullLockdown', siteId: null, securityZoneId: null, reason: '' })

const activeEmergency = computed(() => emergencies.value.length > 0)

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
  try {
    const res = await enterpriseApi.getPolicyOverview()
    Object.assign(overview, res.data)
  } catch {}
}

async function loadVersions() {
  try {
    const res = await enterpriseApi.getPolicyVersions()
    versions.value = res.data || []
  } catch {}
}

async function loadRules() {
  try {
    const res = await enterpriseApi.getAccessRules()
    rules.value = res.data || []
  } catch {}
}

async function loadEmergencies() {
  try {
    const res = await enterpriseApi.getActiveEmergencies()
    emergencies.value = res.data || []
  } catch {}
}

async function loadDuress(unacknowledged) {
  try {
    const res = await enterpriseApi.getDuressEvents(unacknowledged)
    duressEvents.value = res.data || []
  } catch {}
}

async function createVersion() {
  if (!versionForm.name) return
  try {
    await enterpriseApi.createPolicyVersion({ name: versionForm.name, changeSummary: versionForm.changeSummary })
    showCreateVersion.value = false
    versionForm.name = ''; versionForm.changeSummary = ''
    await loadVersions()
  } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function submitVersion(v) {
  try { await enterpriseApi.submitPolicyVersion(v.accessPolicyVersionId); await loadVersions() }
  catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function approveVersion(v) {
  try {
    await enterpriseApi.approvePolicyVersion(v.accessPolicyVersionId, { note: 'Approved from UI' })
    await loadVersions()
  } catch (err) { alert(err.response?.data?.message || 'Approval failed — may require step-up') }
}

async function activateVersion(v) {
  try { await enterpriseApi.activatePolicyVersion(v.accessPolicyVersionId); await loadVersions() }
  catch (err) { alert(err.response?.data?.message || 'Activation failed') }
}

async function retireVersion(v) {
  try { await enterpriseApi.retirePolicyVersion(v.accessPolicyVersionId); await loadVersions() }
  catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function createRule() {
  if (!ruleForm.accessLevelId) return
  try {
    await enterpriseApi.createAccessRule({ ...ruleForm })
    showAddRule.value = false
    ruleForm.accessLevelId = null; ruleForm.subjectId = null
    await loadRules()
  } catch (err) { alert(err.response?.data?.message || 'Failed') }
}

async function runSimulation() {
  try {
    const res = await enterpriseApi.simulateAccess({ ...simForm, evaluatedAtUtc: null })
    simResult.value = res.data
  } catch (err) { alert(err.response?.data?.message || 'Simulation failed') }
}

async function createEmergency() {
  if (!emergencyForm.reason) return
  try {
    await enterpriseApi.createEmergencyState({ ...emergencyForm })
    showEmergencyForm.value = false
    emergencyForm.reason = ''
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
.version-list, .rule-list, .emergency-list, .duress-list { display: flex; flex-direction: column; gap: 6px; }
.version-row, .rule-row, .emergency-row, .duress-row { display: flex; align-items: center; justify-content: space-between; padding: 12px 14px; border-radius: 10px; border: 1px solid var(--border-soft); background: var(--surface); gap: 10px; }
.version-info, .rule-info, .emergency-info, .duress-info { display: flex; flex-direction: column; gap: 2px; flex: 1; }
.version-meta, .rule-detail, .emergency-meta, .duress-meta { font-size: 0.8rem; color: var(--text-muted); }
.version-badges { display: flex; gap: 6px; align-items: center; }
.rule-scope { display: flex; gap: 6px; font-size: 0.78rem; color: var(--text-secondary); }
.badge { font-size: 0.72rem; padding: 2px 8px; border-radius: 12px; font-weight: 500; }
.badge-green { background: rgba(34,197,94,.15); color: #22c55e; }
.badge-gray { background: rgba(100,116,139,.15); color: #64748b; }
.badge-yellow { background: rgba(234,179,8,.15); color: #eab308; }
.badge-blue { background: rgba(59,130,246,.15); color: #3b82f6; }
.badge-red { background: rgba(239,68,68,.15); color: #ef4444; }
.btn-xs { min-height: 28px; padding: 0 10px; font-size: 0.75rem; border-radius: 8px; }
.btn-success { background: #22c55e; color: #fff; }
.btn-danger { background: #ef4444; color: #fff; }
.simulator-grid { display: grid; grid-template-columns: 360px 1fr; gap: 20px; }
.sim-result { padding: 18px; border-radius: 14px; border: 1px solid var(--border-soft); }
.sim-decision { display: flex; flex-direction: column; gap: 6px; padding: 14px; border-radius: 10px; font-size: 1.2rem; font-weight: 700; margin-bottom: 10px; }
.sim-allow { background: rgba(34,197,94,.1); color: #22c55e; border: 1px solid rgba(34,197,94,.2); }
.sim-deny { background: rgba(239,68,68,.1); color: #ef4444; border: 1px solid rgba(239,68,68,.2); }
.sim-decision span { font-size: 0.85rem; font-weight: 400; }
.sim-meta { font-size: 0.78rem; color: var(--text-muted); display: flex; gap: 12px; }
.emergency-row.lockdown { border-color: rgba(239,68,68,.3); background: rgba(239,68,68,.04); }
.emergency-scope { font-size: 0.78rem; color: var(--text-secondary); display: flex; gap: 6px; }
.duress-detail { display: flex; align-items: center; gap: 8px; font-size: 0.82rem; }
.ack-badge { color: #22c55e; font-size: 0.72rem; font-weight: 600; }
.unack-badge { color: #ef4444; font-size: 0.72rem; font-weight: 600; }
.checkbox-label { display: flex !important; align-items: center; gap: 8px; cursor: pointer; flex-direction: row !important; }
.checkbox-label input[type="checkbox"] { width: 18px; height: 18px; }
.form-grid.double { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.form-grid.double label { display: flex; flex-direction: column; gap: 5px; font-size: 0.83rem; color: var(--text-secondary); }
.panel-kicker { display: inline-flex; align-items: center; align-self: flex-start; padding: 4px 10px; border-radius: 999px; background: rgba(15,124,130,.08); color: var(--accent-primary); font-size: 0.72rem; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; }
.status-pill.danger { background: rgba(239,68,68,.15); color: #ef4444; padding: 4px 12px; border-radius: 20px; font-size: 0.72rem; font-weight: 700; letter-spacing: 0.05em; }
</style>
