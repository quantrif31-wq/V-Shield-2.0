<template>
    <div class="page-container soc-console animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Security Operations Center</span>
                <h1 class="page-title">SOC Alarm Console</h1>
            </div>
            <div class="header-actions">
                <span class="status-pill" :class="riskClass">{{ riskLabel }}</span>
                <button type="button" class="btn btn-secondary" :disabled="loading" @click="loadAll">Refresh</button>
            </div>
        </div>

        <section class="metric-grid">
            <article class="metric-tile">
                <span class="metric-label">Open Alarms</span>
                <strong class="metric-value">{{ overview.openAlarms }}</strong>
                <span class="metric-note">{{ overview.criticalOpenAlarms }} critical</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Active SOPs</span>
                <strong class="metric-value">{{ overview.activeSops }}</strong>
                <span class="metric-note">In progress</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Open Incidents</span>
                <strong class="metric-value">{{ overview.openIncidents }}</strong>
                <span class="metric-note">{{ overview.openDispatchTasks }} dispatch tasks</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Oldest Alarm</span>
                <strong class="metric-value">{{ overview.oldestOpenAlarmAgeMinutes }}</strong>
                <span class="metric-note">minutes ago</span>
            </article>
        </section>

        <section class="workspace-tabs" aria-label="SOC workspaces">
            <button type="button" :class="{ active: tab === 'alarms' }" @click="tab = 'alarms'">Alarms ({{ alarms.total }})</button>
            <button type="button" :class="{ active: tab === 'incidents' }" @click="tab = 'incidents'">Incidents ({{ incidents.total }})</button>
            <button type="button" :class="{ active: tab === 'sops' }" @click="tab = 'sops'">SOPs ({{ sopTotal }})</button>
            <button type="button" :class="{ active: tab === 'dispatch' }" @click="tab = 'dispatch'">Dispatch ({{ dispatchTotal }})</button>
            <button type="button" :class="{ active: tab === 'timeline' }" @click="tab = 'timeline'">Timeline</button>
            <button type="button" :class="{ active: tab === 'intel' }" @click="tab = 'intel'">AI Intel</button>
        </section>

        <!-- ALARMS TAB -->
        <section v-if="tab === 'alarms'" class="soc-section">
            <div class="filter-bar">
                <select v-model="alarmFilter.state">
                    <option value="">All states</option>
                    <option>New</option>
                    <option>Acknowledged</option>
                    <option>Assigned</option>
                    <option>Escalated</option>
                    <option>Closed</option>
                </select>
                <select v-model="alarmFilter.severity">
                    <option value="">All severities</option>
                    <option>Critical</option>
                    <option>High</option>
                    <option>Medium</option>
                    <option>Low</option>
                </select>
                <button type="button" class="btn btn-primary btn-sm" @click="loadAlarms">Filter</button>
            </div>
            <div v-if="alarms.items.length" class="alarm-list">
                <div v-for="alarm in alarms.items" :key="alarm.alarmId"
                    class="alarm-row" :class="'sev-' + alarm.severity.toLowerCase()"
                    :style="{ borderLeftColor: severityColor(alarm.severity) }"
                    @click="selectAlarm(alarm)">
                    <div class="alarm-meta">
                        <span class="alarm-id">#{{ alarm.alarmId }}</span>
                        <span class="alarm-state" :class="stateClass(alarm.state)">{{ alarm.state }}</span>
                        <span class="alarm-severity">{{ alarm.severity }}</span>
                    </div>
                    <div class="alarm-body">
                        <strong>{{ alarm.alarmType }}</strong>
                        <p>{{ alarm.summary }}</p>
                    </div>
                    <div class="alarm-actions">
                        <span class="alarm-time">{{ formatTime(alarm.createdAtUtc) }}</span>
                        <router-link v-if="alarm.latitude" :to="'/incident-map/' + alarm.alarmId" class="btn-map-icon" title="Xem trên bản đồ" @click.stop>
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><circle cx="12" cy="10" r="3"/><path d="M12 21s-8-4-8-10a8 8 0 0116 0c0 6-8 10-8 10z"/></svg>
                        </router-link>
                    </div>
                </div>
            </div>
            <div v-else class="empty-card">No alarms found.</div>
            <div v-if="alarms.total > alarms.pageSize" class="pagination">
                <button :disabled="alarms.page <= 1" @click="alarmPage--; loadAlarms()">Previous</button>
                <span>Page {{ alarms.page }} of {{ Math.ceil(alarms.total / alarms.pageSize) }}</span>
                <button :disabled="alarms.page * alarms.pageSize >= alarms.total" @click="alarmPage++; loadAlarms()">Next</button>
            </div>
        </section>

        <!-- INCIDENTS TAB -->
        <section v-if="tab === 'incidents'" class="soc-section">
            <div class="filter-bar">
                <button type="button" class="btn btn-primary btn-sm" @click="showIncidentForm = true">New Incident</button>
            </div>
            <div v-if="incidents.items.length" class="incident-list">
                <div v-for="inc in incidents.items" :key="inc.incidentId"
                    class="alarm-row" :class="'sev-' + inc.severity.toLowerCase()"
                    @click="selectIncident(inc)">
                    <div class="alarm-meta">
                        <span class="alarm-id">#{{ inc.incidentId }}</span>
                        <span class="alarm-state">{{ inc.status }}</span>
                        <span class="alarm-severity">{{ inc.severity }}</span>
                    </div>
                    <div class="alarm-body">
                        <strong>{{ inc.title }}</strong>
                        <p v-if="inc.outcome">Outcome: {{ inc.outcome }}</p>
                    </div>
                    <div class="alarm-time">{{ formatTime(inc.openedAtUtc) }}</div>
                </div>
            </div>
            <div v-else class="empty-card">No incidents found.</div>
        </section>

        <!-- SOPs TAB -->
        <section v-if="tab === 'sops'" class="soc-section">
            <div v-if="sopExecutions.length" class="incident-list">
                <div v-for="sop in sopExecutions" :key="sop.sopExecutionId" class="alarm-row">
                    <div class="alarm-meta">
                        <span class="alarm-id">#{{ sop.sopExecutionId }}</span>
                        <span class="alarm-state">{{ sop.status }}</span>
                    </div>
                    <div class="alarm-body">
                        <strong>SOP Template #{{ sop.sopTemplateId }}</strong>
                        <p>Started: {{ formatTime(sop.startedAtUtc) }}</p>
                    </div>
                </div>
            </div>
            <div v-else class="empty-card">No SOP executions found.</div>
        </section>

        <!-- DISPATCH TAB -->
        <section v-if="tab === 'dispatch'" class="soc-section">
            <div v-if="dispatchTasks.length" class="incident-list">
                <div v-for="task in dispatchTasks" :key="task.dispatchTaskId" class="alarm-row" :style="{ borderLeftColor: priorityColor(task.priority) }">
                    <div class="alarm-meta">
                        <span class="alarm-id">#{{ task.dispatchTaskId }}</span>
                        <span class="alarm-state">{{ task.status }}</span>
                        <span class="alarm-severity">{{ task.priority }}</span>
                    </div>
                    <div class="alarm-body">
                        <strong>{{ task.locationText }}</strong>
                        <p>{{ task.instructions }}</p>
                    </div>
                    <div class="alarm-time">{{ formatTime(task.createdAtUtc) }}</div>
                </div>
            </div>
            <div v-else class="empty-card">No dispatch tasks found.</div>
        </section>

        <section v-if="tab === 'timeline'" class="soc-section">
            <EventTimeline :embedded="true" />
        </section>

        <!-- AI INTEL TAB -->
        <section v-if="tab === 'intel'" class="soc-section">
            <div class="ops-grid two">
                <article class="ops-panel">
                    <div class="panel-head compact">
                        <div>
                            <span class="panel-kicker">AI Intelligence</span>
                            <h2 class="panel-title">SOC Analytics</h2>
                        </div>
                        <button type="button" class="btn btn-sm btn-secondary" @click="loadIntel">Refresh</button>
                    </div>
                    <div v-if="intel.summary" class="intel-summary">
                        <p>{{ intel.summary }}</p>
                    </div>
                    <div class="soc-stats-grid">
                        <div class="soc-stat">
                            <strong>{{ intel.statistics?.totalToday || 0 }}</strong>
                            <span>Alarm hom nay</span>
                            <span class="soc-change" :class="{ up: (intel.statistics?.changePercent || 0) > 0, down: (intel.statistics?.changePercent || 0) < 0 }">
                                {{ intel.statistics?.changePercent || 0 }}%
                            </span>
                        </div>
                        <div class="soc-stat">
                            <strong class="text-danger">{{ intel.statistics?.criticalOpenAlarms || 0 }}</strong>
                            <span>Critical mo</span>
                        </div>
                        <div class="soc-stat">
                            <strong>{{ intel.statistics?.avgResolutionHours || 0 }}</strong>
                            <span>Gio xu ly TB</span>
                        </div>
                    </div>
                </article>
                <article class="ops-panel">
                    <div class="panel-head compact">
                        <div>
                            <span class="panel-kicker">AI Phat hien</span>
                            <h2 class="panel-title">Bat thuong</h2>
                        </div>
                    </div>
                    <div v-if="intel.anomalies && intel.anomalies.length" class="anomaly-list">
                        <div v-for="(anomaly, idx) in intel.anomalies" :key="idx" class="anomaly-item" :class="'sev-' + (anomaly.severity || 'medium').toLowerCase()">
                            <strong>{{ anomaly.type }}</strong>
                            <p>{{ anomaly.detail }}</p>
                            <div v-if="anomaly.currentCount != null" class="anomaly-metric">
                                <span>{{ anomaly.currentCount }} hiện tại</span>
                                <span v-if="anomaly.expectedCount">| {{ anomaly.expectedCount }} TB</span>
                                <span v-if="anomaly.deviation">| +{{ anomaly.deviation }}%</span>
                            </div>
                        </div>
                    </div>
                    <div v-else class="empty-card">Không phát hiện bất thường.</div>
                </article>
            </div>
        </section>

        <!-- ALARM DETAIL MODAL -->
        <div v-if="selectedAlarm" class="modal-overlay" @click.self="selectedAlarm = null">
            <div class="modal-content modal-lg">
                <div class="modal-header">
                    <h2>Alarm #{{ selectedAlarm.alarmId }}</h2>
                    <button type="button" class="btn-close" @click="selectedAlarm = null">&times;</button>
                </div>
                <div class="modal-body">
                    <div class="detail-grid">
                        <div class="detail-field">
                            <span class="detail-label">Type</span>
                            <strong>{{ selectedAlarm.alarmType }}</strong>
                        </div>
                        <div class="detail-field">
                            <span class="detail-label">Severity</span>
                            <strong :style="{ color: severityColor(selectedAlarm.severity) }">{{ selectedAlarm.severity }}</strong>
                        </div>
                        <div class="detail-field">
                            <span class="detail-label">State</span>
                            <span class="alarm-state" :class="stateClass(selectedAlarm.state)">{{ selectedAlarm.state }}</span>
                        </div>
                        <div class="detail-field">
                            <span class="detail-label">Created</span>
                            <strong>{{ formatTime(selectedAlarm.createdAtUtc) }}</strong>
                        </div>
                    </div>
                    <div class="detail-summary">
                        <span class="detail-label">Summary</span>
                        <p>{{ selectedAlarm.summary }}</p>
                    </div>

                    <div v-if="classification" class="ai-insight">
                        <h4>AI Classification</h4>
                        <div class="detail-grid">
                            <div class="detail-field">
                                <span class="detail-label">Predicted Severity</span>
                                <strong>{{ classification.predictedSeverity }}</strong>
                            </div>
                            <div class="detail-field">
                                <span class="detail-label">Predicted Type</span>
                                <strong>{{ classification.predictedAlarmType }}</strong>
                            </div>
                            <div class="detail-field">
                                <span class="detail-label">Confidence</span>
                                <strong>{{ classification.confidence }}</strong>
                            </div>
                        </div>
                        <div v-if="classification.matchedKeywords?.length" class="keyword-list">
                            <span v-for="kw in classification.matchedKeywords" :key="kw" class="keyword-chip">{{ kw }}</span>
                        </div>
                    </div>

                    <div v-if="escalation" class="ai-insight">
                        <h4>Escalation Risk</h4>
                        <div class="risk-score-bar">
                            <div class="risk-score-fill" :style="{ width: escalation.riskScore + '%', background: escalation.riskScore >= 60 ? '#d44747' : escalation.riskScore >= 40 ? '#d49b47' : '#74b47a' }"></div>
                        </div>
                        <p class="risk-text">{{ escalation.riskScore }}/100 - {{ escalation.recommendation }}</p>
                        <ul v-if="escalation.factors?.length" class="factor-list">
                            <li v-for="(f, i) in escalation.factors" :key="i">{{ f }}</li>
                        </ul>
                    </div>

                    <div v-if="sopRecommendations?.length" class="ai-insight">
                        <h4>Recommended SOPs</h4>
                        <div v-for="sop in sopRecommendations" :key="sop.sopTemplateId" class="sop-rec-row">
                            <strong>{{ sop.name }}</strong>
                            <span class="rec-score">Score: {{ sop.relevanceScore }}</span>
                            <span>{{ sop.stepCount }} steps</span>
                            <p>{{ sop.reason }}</p>
                            <button class="btn btn-sm btn-primary" @click="startSop(sop.sopTemplateId)">Run SOP</button>
                        </div>
                    </div>

                    <div class="comments-section">
                        <h4>Comments ({{ comments.length }})</h4>
                        <div v-if="comments.length" class="comment-list">
                            <div v-for="c in comments" :key="c.alarmCommentId" class="comment-item">
                                <strong>User #{{ c.userId || 'system' }}</strong>
                                <p>{{ c.comment }}</p>
                                <span class="comment-time">{{ formatTime(c.createdAtUtc) }}</span>
                            </div>
                        </div>
                        <div v-else class="empty-card">No comments.</div>
                        <div class="comment-form">
                            <input v-model="newComment" placeholder="Add a comment..." @keyup.enter="addComment" />
                            <button class="btn btn-sm btn-secondary" :disabled="!newComment.trim()" @click="addComment">Send</button>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button v-if="selectedAlarm.state === 'New'" class="btn btn-primary" @click="acknowledgeAlarm">Acknowledge</button>
                    <button v-if="selectedAlarm.state !== 'Closed'" class="btn btn-secondary" @click="showAssign = true">Assign</button>
                    <button v-if="selectedAlarm.state !== 'Closed'" class="btn btn-secondary" @click="showCloseAlarm = true">Close</button>
                </div>
            </div>
        </div>

        <!-- INCIDENT DETAIL MODAL -->
        <div v-if="selectedIncident" class="modal-overlay" @click.self="selectedIncident = null">
            <div class="modal-content modal-lg">
                <div class="modal-header">
                    <h2>Incident #{{ selectedIncident.incidentId }}</h2>
                    <button type="button" class="btn-close" @click="selectedIncident = null">&times;</button>
                </div>
                <div class="modal-body">
                    <div class="detail-grid">
                        <div class="detail-field">
                            <span class="detail-label">Title</span>
                            <strong>{{ selectedIncident.title }}</strong>
                        </div>
                        <div class="detail-field">
                            <span class="detail-label">Severity</span>
                            <strong>{{ selectedIncident.severity }}</strong>
                        </div>
                        <div class="detail-field">
                            <span class="detail-label">Status</span>
                            <span class="alarm-state">{{ selectedIncident.status }}</span>
                        </div>
                        <div class="detail-field">
                            <span class="detail-label">Opened</span>
                            <strong>{{ formatTime(selectedIncident.openedAtUtc) }}</strong>
                        </div>
                    </div>
                    <div v-if="selectedIncident.outcome" class="detail-summary">
                        <span class="detail-label">Outcome</span>
                        <p>{{ selectedIncident.outcome }}</p>
                    </div>

                    <div class="comments-section">
                        <h4>Timeline ({{ timelineItems.length }})</h4>
                        <div v-if="timelineItems.length" class="comment-list">
                            <div v-for="item in timelineItems" :key="item.incidentTimelineItemId" class="comment-item">
                                <strong>{{ item.itemType }}</strong>
                                <p>{{ item.text }}</p>
                                <span class="comment-time">{{ formatTime(item.createdAtUtc) }}</span>
                            </div>
                        </div>
                        <div v-else class="empty-card">No timeline items.</div>
                        <div class="comment-form">
                            <input v-model="newTimelineText" placeholder="Add timeline note..." @keyup.enter="addTimelineItem" />
                            <button class="btn btn-sm btn-secondary" :disabled="!newTimelineText.trim()" @click="addTimelineItem">Add</button>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button v-if="selectedIncident.status !== 'Closed'" class="btn btn-primary" @click="showCloseIncident = true">Close Incident</button>
                </div>
            </div>
        </div>

        <!-- ASSIGN MODAL -->
        <div v-if="showAssign && selectedAlarm" class="modal-overlay" @click.self="showAssign = false">
            <div class="modal-content">
                <div class="modal-header">
                    <h2>Assign Alarm #{{ selectedAlarm.alarmId }}</h2>
                    <button type="button" class="btn-close" @click="showAssign = false">&times;</button>
                </div>
                <div class="modal-body">
                    <div class="form-grid single">
                        <label>
                            User ID
                            <input v-model.number="assignForm.userId" type="number" min="1" required />
                        </label>
                        <label>
                            Note
                            <textarea v-model="assignForm.note" rows="3"></textarea>
                        </label>
                    </div>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-primary" @click="assignAlarm">Assign</button>
                    <button class="btn btn-secondary" @click="showAssign = false">Cancel</button>
                </div>
            </div>
        </div>

        <!-- CLOSE ALARM MODAL -->
        <div v-if="showCloseAlarm && selectedAlarm" class="modal-overlay" @click.self="showCloseAlarm = false">
            <div class="modal-content">
                <div class="modal-header">
                    <h2>Close Alarm #{{ selectedAlarm.alarmId }}</h2>
                    <button type="button" class="btn-close" @click="showCloseAlarm = false">&times;</button>
                </div>
                <div class="modal-body">
                    <div class="form-grid single">
                        <label>
                            Resolution Note
                            <textarea v-model="closeForm.note" rows="3" required></textarea>
                        </label>
                    </div>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-primary" @click="closeAlarmAction">Close</button>
                    <button class="btn btn-secondary" @click="showCloseAlarm = false">Cancel</button>
                </div>
            </div>
        </div>

        <!-- CLOSE INCIDENT MODAL -->
        <div v-if="showCloseIncident && selectedIncident" class="modal-overlay" @click.self="showCloseIncident = false">
            <div class="modal-content">
                <div class="modal-header">
                    <h2>Close Incident #{{ selectedIncident.incidentId }}</h2>
                    <button type="button" class="btn-close" @click="showCloseIncident = false">&times;</button>
                </div>
                <div class="modal-body">
                    <div class="form-grid single">
                        <label>
                            Outcome Note (required)
                            <textarea v-model="closeIncidentForm.note" rows="3" required></textarea>
                        </label>
                    </div>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-primary" :disabled="!closeIncidentForm.note.trim()" @click="closeIncidentAction">Close Incident</button>
                    <button class="btn btn-secondary" @click="showCloseIncident = false">Cancel</button>
                </div>
            </div>
        </div>

        <!-- NEW INCIDENT MODAL -->
        <div v-if="showIncidentForm" class="modal-overlay" @click.self="showIncidentForm = false">
            <div class="modal-content">
                <div class="modal-header">
                    <h2>New Incident</h2>
                    <button type="button" class="btn-close" @click="showIncidentForm = false">&times;</button>
                </div>
                <div class="modal-body">
                    <div class="form-grid single">
                        <label>
                            Title
                            <input v-model="incidentForm.title" required />
                        </label>
                        <label>
                            Severity
                            <select v-model="incidentForm.severity">
                                <option>Medium</option>
                                <option>High</option>
                                <option>Critical</option>
                            </select>
                        </label>
                    </div>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-primary" :disabled="!incidentForm.title.trim()" @click="createIncidentAction">Create</button>
                    <button class="btn btn-secondary" @click="showIncidentForm = false">Cancel</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import EventTimeline from './EventTimeline.vue'
import { socApi } from '../services/socApi'

const route = useRoute()
const loading = ref(false)
const tab = ref('alarms')
const intel = reactive({ summary: '', statistics: {}, anomalies: [] })

const overview = reactive({
    openAlarms: 0, criticalOpenAlarms: 0, activeSops: 0,
    openIncidents: 0, openDispatchTasks: 0, oldestOpenAlarmAgeMinutes: 0,
})

const alarms = reactive({ total: 0, page: 1, pageSize: 20, items: [] })
const alarmFilter = reactive({ state: '', severity: '' })
const alarmPage = ref(1)

const incidents = reactive({ total: 0, page: 1, pageSize: 20, items: [] })
const sopExecutions = ref([])
const dispatchTasks = ref([])

const selectedAlarm = ref(null)
const selectedIncident = ref(null)
const classification = ref(null)
const escalation = ref(null)
const sopRecommendations = ref([])
const comments = ref([])
const timelineItems = ref([])
const newComment = ref('')
const newTimelineText = ref('')
const showAssign = ref(false)
const showCloseAlarm = ref(false)
const showCloseIncident = ref(false)
const showIncidentForm = ref(false)

const assignForm = reactive({ userId: null, note: '' })
const closeForm = reactive({ note: '' })
const closeIncidentForm = reactive({ note: '' })
const incidentForm = reactive({ title: '', severity: 'Medium' })

const sopTotal = computed(() => sopExecutions.value.length)
const dispatchTotal = computed(() => dispatchTasks.value.length)

const riskLabel = computed(() => {
    if (overview.criticalOpenAlarms > 0) return 'Critical alarms open'
    if (overview.openAlarms > 10) return 'Multiple open alarms'
    return 'Stable'
})

const riskClass = computed(() => {
    if (overview.criticalOpenAlarms > 0) return 'danger'
    if (overview.openAlarms > 10) return 'warn'
    return 'success'
})

const syncTabFromRoute = () => {
    const candidate = typeof route.query.tab === 'string' ? route.query.tab : ''
    if (['alarms', 'incidents', 'sops', 'dispatch', 'timeline', 'intel'].includes(candidate)) {
        tab.value = candidate
    }
}

async function loadAll() {
    loading.value = true
    await Promise.all([
        loadOverview(),
        loadAlarms(),
        loadIncidents(),
        loadSops(),
        loadDispatch(),
        loadIntel(),
    ])
    loading.value = false
}

async function loadOverview() {
    try {
        const res = await socApi.overview()
        Object.assign(overview, res.data)
    } catch {}
}

async function loadAlarms() {
    try {
        const res = await socApi.getAlarms({ state: alarmFilter.state || undefined, severity: alarmFilter.severity || undefined, page: alarmPage.value, pageSize: alarms.pageSize })
        Object.assign(alarms, res.data)
    } catch {}
}

async function loadIncidents() {
    try {
        const res = await socApi.getIncidents({ page: 1, pageSize: 20 })
        Object.assign(incidents, res.data)
    } catch {}
}

async function loadSops() {
    try {
        const res = await socApi.getSopExecutions({ page: 1, pageSize: 20 })
        sopExecutions.value = res.data.items || []
    } catch {}
}

async function loadDispatch() {
    try {
        const res = await socApi.getDispatchTasks({ page: 1, pageSize: 20 })
        dispatchTasks.value = res.data.items || []
    } catch {}
}

async function loadIntel() {
    try {
        const res = await socApi.getIntelligence()
        Object.assign(intel, res.data)
    } catch {}
}

async function selectAlarm(alarm) {
    selectedAlarm.value = alarm
    classification.value = null
    escalation.value = null
    sopRecommendations.value = []
    comments.value = []
    try {
        const [cls, esc, sop, cmt] = await Promise.all([
            socApi.classifyAlarm(alarm.alarmId),
            socApi.escalationRisk(alarm.alarmId),
            socApi.recommendSop(alarm.alarmId),
            socApi.getAlarmComments(alarm.alarmId),
        ])
        classification.value = cls.data
        escalation.value = esc.data
        sopRecommendations.value = sop.data
        comments.value = cmt.data
    } catch {}
}

async function selectIncident(inc) {
    selectedIncident.value = inc
    timelineItems.value = []
    try {
        const res = await socApi.getIncidentTimeline(inc.incidentId)
        timelineItems.value = res.data
    } catch {}
}

async function acknowledgeAlarm() {
    if (!selectedAlarm.value) return
    try {
        await socApi.acknowledgeAlarm(selectedAlarm.value.alarmId)
        selectedAlarm.value.state = 'Acknowledged'
        await loadAlarms()
    } catch {}
}

async function assignAlarm() {
    if (!selectedAlarm.value || !assignForm.userId) return
    try {
        await socApi.assignAlarm(selectedAlarm.value.alarmId, { assignedToUserId: assignForm.userId, note: assignForm.note })
        selectedAlarm.value.state = 'Assigned'
        showAssign.value = false
        await loadAlarms()
    } catch {}
}

async function closeAlarmAction() {
    if (!selectedAlarm.value) return
    try {
        await socApi.closeAlarm(selectedAlarm.value.alarmId, { note: closeForm.note })
        selectedAlarm.value.state = 'Closed'
        showCloseAlarm.value = false
        await loadAlarms()
    } catch {}
}

async function addComment() {
    if (!selectedAlarm.value || !newComment.value.trim()) return
    try {
        await socApi.addComment(selectedAlarm.value.alarmId, { comment: newComment.value.trim() })
        newComment.value = ''
        const res = await socApi.getAlarmComments(selectedAlarm.value.alarmId)
        comments.value = res.data
    } catch {}
}

async function addTimelineItem() {
    if (!selectedIncident.value || !newTimelineText.value.trim()) return
    try {
        await socApi.addIncidentTimelineItem(selectedIncident.value.incidentId, { text: newTimelineText.value.trim() })
        newTimelineText.value = ''
        const res = await socApi.getIncidentTimeline(selectedIncident.value.incidentId)
        timelineItems.value = res.data
    } catch {}
}

async function startSop(templateId) {
    if (!selectedAlarm.value) return
    try {
        await socApi.startSopExecution({ alarmId: selectedAlarm.value.alarmId, sopTemplateId: templateId })
        await loadSops()
        alert('SOP execution started.')
    } catch {}
}

async function createIncidentAction() {
    try {
        await socApi.createIncident({ title: incidentForm.title, severity: incidentForm.severity })
        showIncidentForm.value = false
        incidentForm.title = ''
        await loadIncidents()
    } catch {}
}

async function closeIncidentAction() {
    if (!selectedIncident.value) return
    try {
        await socApi.closeIncident(selectedIncident.value.incidentId, { note: closeIncidentForm.note })
        selectedIncident.value.status = 'Closed'
        showCloseIncident.value = false
        await loadIncidents()
    } catch {}
}

function severityColor(severity) {
    switch (severity) {
        case 'Critical': return '#d44747'
        case 'High': return '#d49b47'
        case 'Medium': return '#47a3d4'
        default: return '#74b47a'
    }
}

function priorityColor(priority) {
    switch (priority) {
        case 'High': case 'Critical': return '#d44747'
        case 'Medium': return '#d49b47'
        default: return '#74b47a'
    }
}

function stateClass(state) {
    switch (state) {
        case 'New': return 'state-new'
        case 'Acknowledged': return 'state-ack'
        case 'Assigned': return 'state-assigned'
        case 'Escalated': return 'state-escalated'
        case 'Closed': return 'state-closed'
        default: return ''
    }
}

function formatTime(val) {
    if (!val) return ''
    return new Date(val).toLocaleString()
}

watch(() => route.query.tab, syncTabFromRoute)

onMounted(async () => {
    syncTabFromRoute()
    await loadAll()
})
</script>

<style scoped>
.soc-console {
    display: flex;
    flex-direction: column;
    gap: 18px;
}

.status-pill {
    display: inline-flex;
    align-items: center;
    min-height: 36px;
    padding: 0 14px;
    border-radius: 999px;
    font-weight: 700;
    font-size: 0.85rem;
}

.status-pill.success { background: rgba(77, 180, 128, 0.16); color: #aaffd0; }
.status-pill.warn { background: rgba(212, 155, 71, 0.16); color: #ffd89a; }
.status-pill.danger { background: rgba(212, 71, 71, 0.16); color: #ffb0b0; }

.workspace-tabs {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
}

.workspace-tabs button {
    min-height: 40px;
    padding: 0 16px;
    border-radius: 999px;
    border: 1px solid var(--border-soft);
    background: var(--surface);
    color: var(--text-secondary);
    font-weight: 700;
    cursor: pointer;
}

.workspace-tabs button.active {
    color: #05313b;
    background: #8ceaf4;
    border-color: #8ceaf4;
}

.soc-section {
    display: flex;
    flex-direction: column;
    gap: 14px;
}

.filter-bar {
    display: flex;
    gap: 10px;
    align-items: center;
}

.filter-bar select {
    min-height: 40px;
    padding: 0 12px;
    border-radius: 10px;
    border: 1px solid var(--border-soft);
    background: var(--surface);
    color: var(--text-primary);
}

.alarm-list, .incident-list {
    display: grid;
    gap: 6px;
}

.alarm-row {
    display: grid;
    grid-template-columns: 160px minmax(0, 1fr) auto;
    gap: 14px;
    align-items: center;
    padding: 12px 16px;
    border-radius: 12px;
    border: 1px solid var(--border-soft);
    background: var(--surface);
    cursor: pointer;
    transition: background 0.15s;
    border-left: 3px solid transparent;
}

.alarm-row:hover {
    background: var(--surface-muted);
}

.alarm-row.sev-critical { border-left-color: #d44747; }
.alarm-row.sev-high { border-left-color: #d49b47; }
.alarm-row.sev-medium { border-left-color: #47a3d4; }

.alarm-meta {
    display: flex;
    gap: 8px;
    align-items: center;
    flex-wrap: wrap;
}

.alarm-id {
    font-size: 0.82rem;
    color: var(--text-muted);
    font-weight: 700;
}

.alarm-state {
    font-size: 0.75rem;
    padding: 2px 8px;
    border-radius: 999px;
    font-weight: 700;
}

.state-new { background: rgba(71, 163, 212, 0.16); color: #8cd4ff; }
.state-ack { background: rgba(212, 155, 71, 0.16); color: #ffd89a; }
.state-assigned { background: rgba(77, 180, 128, 0.16); color: #aaffd0; }
.state-escalated { background: rgba(212, 71, 71, 0.16); color: #ffb0b0; }
.state-closed { background: rgba(128, 128, 128, 0.16); color: #ccc; }

.alarm-severity {
    font-size: 0.75rem;
    font-weight: 700;
    color: var(--text-secondary);
}

.alarm-body {
    min-width: 0;
}

.alarm-body strong {
    display: block;
    font-size: 0.88rem;
    color: var(--text-primary);
}

.alarm-body p {
    margin: 2px 0 0;
    font-size: 0.82rem;
    color: var(--text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.alarm-time {
    font-size: 0.78rem;
    color: var(--text-muted);
    white-space: nowrap;
}

.alarm-actions {
    display: flex;
    align-items: center;
    gap: 8px;
    white-space: nowrap;
}

.btn-map-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 28px;
    height: 28px;
    border-radius: 8px;
    border: 1px solid var(--border-soft);
    color: var(--text-secondary);
    text-decoration: none;
    transition: all 0.15s;
}

.btn-map-icon:hover {
    background: rgba(71, 163, 212, 0.12);
    color: #8cd4ff;
    border-color: #8cd4ff;
}

.pagination {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 12px;
    margin-top: 12px;
}

.modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0,0,0,0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
}

.modal-content {
    background: var(--surface);
    border-radius: 18px;
    border: 1px solid var(--border-soft);
    box-shadow: var(--shadow-popup);
    width: 90%;
    max-width: 720px;
    max-height: 85vh;
    display: flex;
    flex-direction: column;
}

.modal-lg {
    max-width: 900px;
}

.modal-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 18px 22px;
    border-bottom: 1px solid var(--border-soft);
}

.modal-header h2 {
    margin: 0;
    font-size: 1.1rem;
}

.btn-close {
    background: none;
    border: none;
    font-size: 1.5rem;
    color: var(--text-secondary);
    cursor: pointer;
}

.modal-body {
    padding: 18px 22px;
    overflow-y: auto;
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.modal-footer {
    display: flex;
    gap: 10px;
    padding: 14px 22px;
    border-top: 1px solid var(--border-soft);
}

.detail-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 12px;
}

.detail-field {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.detail-label {
    font-size: 0.78rem;
    color: var(--text-muted);
    text-transform: uppercase;
    letter-spacing: 0.03em;
}

.detail-summary {
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.detail-summary p {
    margin: 0;
    color: var(--text-primary);
}

.ai-insight {
    padding: 14px;
    border-radius: 12px;
    background: rgba(18, 75, 91, 0.06);
    border: 1px solid var(--border-soft);
}

.ai-insight h4 {
    margin: 0 0 10px;
    font-size: 0.85rem;
    color: var(--text-primary);
}

.risk-score-bar {
    height: 8px;
    border-radius: 999px;
    background: var(--border-soft);
    overflow: hidden;
    margin-bottom: 6px;
}

.risk-score-fill {
    height: 100%;
    border-radius: 999px;
    transition: width 0.6s ease;
}

.risk-text {
    margin: 6px 0;
    font-size: 0.82rem;
    color: var(--text-secondary);
}

.factor-list {
    margin: 6px 0 0;
    padding-left: 16px;
}

.factor-list li {
    font-size: 0.8rem;
    color: var(--text-muted);
}

.keyword-list {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    margin-top: 8px;
}

.keyword-chip {
    padding: 2px 10px;
    border-radius: 999px;
    font-size: 0.75rem;
    background: rgba(71, 163, 212, 0.12);
    color: #8cd4ff;
}

.sop-rec-row {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 6px;
    padding: 10px 0;
    border-top: 1px solid var(--border-soft);
    align-items: center;
}

.sop-rec-row:first-child { border-top: none; }

.sop-rec-row strong { font-size: 0.85rem; }

.sop-rec-row span { font-size: 0.78rem; color: var(--text-muted); }

.sop-rec-row p {
    grid-column: 1 / -1;
    margin: 0;
    font-size: 0.8rem;
    color: var(--text-secondary);
}

.sop-rec-row button {
    justify-self: end;
}

.comments-section h4 {
    margin: 0 0 10px;
    font-size: 0.88rem;
}

.comment-list {
    display: grid;
    gap: 8px;
    margin-bottom: 10px;
}

.comment-item {
    padding: 10px;
    border-radius: 10px;
    background: var(--surface-muted);
}

.comment-item strong {
    font-size: 0.8rem;
}

.comment-item p {
    margin: 4px 0;
    font-size: 0.84rem;
}

.comment-time {
    font-size: 0.74rem;
    color: var(--text-muted);
}

.comment-form {
    display: flex;
    gap: 8px;
}

.comment-form input {
    flex: 1;
    min-height: 40px;
    padding: 0 12px;
    border-radius: 10px;
    border: 1px solid var(--border-soft);
    background: var(--surface);
    color: var(--text-primary);
}

.intel-summary {
    padding: 12px;
    border-radius: 12px;
    background: var(--surface-muted);
    margin-bottom: 10px;
}

.intel-summary p { margin: 0; font-size: 0.85rem; color: var(--text-secondary); }

.soc-stats-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 10px;
}

.soc-stat {
    padding: 14px;
    border-radius: 12px;
    background: var(--surface-muted);
    border: 1px solid var(--border-soft);
}

.soc-stat strong { display: block; font-size: 1.5rem; color: var(--text-primary); }
.soc-stat span { font-size: 0.78rem; color: var(--text-secondary); }

.soc-change { font-size: 0.8rem; font-weight: 700; }
.soc-change.up { color: #d44747; }
.soc-change.down { color: #4db480; }

.text-danger { color: #d44747; }

.anomaly-list { display: grid; gap: 8px; }

.anomaly-item {
    padding: 12px;
    border-radius: 10px;
    border: 1px solid var(--border-soft);
    background: var(--surface-muted);
}

.anomaly-item.sev-critical { border-left: 3px solid #d44747; }
.anomaly-item.sev-high { border-left: 3px solid #d49b47; }

.anomaly-item strong { display: block; font-size: 0.85rem; color: var(--text-primary); text-transform: capitalize; margin-bottom: 4px; }
.anomaly-item p { margin: 0 0 6px; font-size: 0.82rem; color: var(--text-secondary); }
.anomaly-metric { display: flex; gap: 10px; font-size: 0.78rem; color: var(--text-muted); }

.empty-card {
    padding: 40px;
    text-align: center;
    color: var(--text-muted);
    border: 1px dashed var(--border-soft);
    border-radius: 12px;
}

.ops-grid.two {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 16px;
}

.ops-panel {
    border: 1px solid var(--border-soft);
    border-radius: 14px;
    background: var(--surface);
    padding: 16px;
}

.panel-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
}

.panel-head.compact { margin-bottom: 8px; }

.panel-kicker {
    font-size: 0.72rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-muted);
}

.panel-title {
    margin: 0;
    font-size: 0.95rem;
}

@media (max-width: 700px) {
    .alarm-row { grid-template-columns: 1fr; }
    .alarm-actions { flex-direction: row; }
    .detail-grid { grid-template-columns: 1fr; }
    .soc-stats-grid { grid-template-columns: 1fr; }
    .ops-grid.two { grid-template-columns: 1fr; }
}
</style>
