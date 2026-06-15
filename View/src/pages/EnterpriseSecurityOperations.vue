<template>
    <div class="page-container enterprise-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Company-wide security</span>
                <h1 class="page-title">Enterprise Security Command</h1>
            </div>
            <div class="header-actions">
                <button type="button" class="btn btn-secondary" :disabled="loading" @click="loadOverview">
                    Refresh
                </button>
                <button type="button" class="btn btn-primary" @click="selectedWorkspace = 'soc'">
                    Open SOC
                </button>
            </div>
        </div>

        <section class="readiness-band">
            <div class="readiness-score">
                <span>Target</span>
                <strong>100%</strong>
            </div>
            <div class="readiness-copy">
                <h2>Renovation control surface</h2>
                <p>{{ statusMessage }}</p>
            </div>
            <div class="readiness-actions">
                <span class="status-pill" :class="{ danger: loadError }">
                    {{ loadError ? 'Needs attention' : 'Live' }}
                </span>
            </div>
        </section>

        <section class="metric-grid">
            <article v-for="metric in headlineMetrics" :key="metric.label" class="metric-tile">
                <span class="metric-label">{{ metric.label }}</span>
                <strong class="metric-value">{{ metric.value }}</strong>
                <span class="metric-note">{{ metric.note }}</span>
            </article>
        </section>

        <section class="workspace-tabs" aria-label="Enterprise workspaces">
            <button
                v-for="workspace in workspaces"
                :key="workspace.id"
                type="button"
                :class="{ active: selectedWorkspace === workspace.id }"
                @click="selectedWorkspace = workspace.id"
            >
                {{ workspace.label }}
            </button>
        </section>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">{{ activeWorkspace.kicker }}</span>
                        <h2 class="panel-title">{{ activeWorkspace.title }}</h2>
                    </div>
                    <span class="soft-chip">{{ activeWorkspace.badge }}</span>
                </div>

                <div class="workspace-summary">
                    <div v-for="item in activeWorkspace.metrics" :key="item.label" class="workspace-stat">
                        <strong>{{ item.value }}</strong>
                        <span>{{ item.label }}</span>
                    </div>
                </div>

                <div class="action-strip">
                    <button
                        v-for="action in activeWorkspace.actions"
                        :key="action"
                        type="button"
                        class="btn btn-secondary btn-sm"
                        @click="selectedAction = action"
                    >
                        {{ action }}
                    </button>
                </div>
            </article>

            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Step-up</span>
                        <h2 class="panel-title">Privileged action session</h2>
                    </div>
                    <span class="soft-chip" :class="{ success: stepUp.active }">
                        {{ stepUp.active ? 'Verified' : 'Required' }}
                    </span>
                </div>

                <form class="form-grid" @submit.prevent="verifyStepUp">
                    <label>
                        Action
                        <select v-model="stepUp.action">
                            <option value="AllPrivilegedActions">All privileged actions</option>
                            <option value="UserAdministration">User administration</option>
                            <option value="AccessPolicyEmergency">Emergency policy</option>
                            <option value="DeviceConfiguration">Device configuration</option>
                            <option value="EvidenceExportApproval">Evidence export</option>
                            <option value="EvidenceRetentionPurge">Evidence purge</option>
                            <option value="SiteHierarchyBackfill">Site hierarchy backfill</option>
                            <option value="ReleaseApproval">Release approval</option>
                        </select>
                    </label>
                    <label>
                        Password
                        <input v-model="stepUp.password" type="password" autocomplete="current-password" />
                    </label>
                    <label>
                        MFA code
                        <input v-model="stepUp.mfaCode" inputmode="numeric" autocomplete="one-time-code" />
                    </label>
                    <button type="submit" class="btn btn-primary" :disabled="busy.stepUp">
                        Verify
                    </button>
                </form>
                <p v-if="stepUp.message" class="inline-message">{{ stepUp.message }}</p>
            </article>
        </section>

        <section class="ops-grid three">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Production guard</span>
                        <h2 class="panel-title">Configuration health</h2>
                    </div>
                    <span class="soft-chip" :class="{ success: configHealth.status === 'Healthy', danger: configHealth.status === 'Blocked' }">
                        {{ configHealth.status || 'Unknown' }}
                    </span>
                </div>
                <div class="finding-list">
                    <div v-for="finding in visibleFindings" :key="finding.key" class="finding-row">
                        <strong>{{ finding.key }}</strong>
                        <span :class="finding.status.toLowerCase()">{{ finding.status }}</span>
                    </div>
                </div>
            </article>

            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Foundation</span>
                        <h2 class="panel-title">Legacy asset backfill</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="backfillDefaultSite">
                    <label>
                        Company code
                        <input v-model="backfillForm.companyCode" required />
                    </label>
                    <label>
                        Site code
                        <input v-model="backfillForm.siteCode" required />
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.backfill">
                        Run safe backfill
                    </button>
                </form>
                <div class="asset-map-summary">
                    <span>{{ assetMap.gates.length }} gates</span>
                    <span>{{ assetMap.cameras.length }} cameras</span>
                    <span>{{ assetMap.vehicles.length }} vehicles</span>
                </div>
            </article>

            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Policy</span>
                        <h2 class="panel-title">Decision simulator</h2>
                    </div>
                    <span v-if="policyResult.result" class="soft-chip" :class="{ danger: policyResult.result === 'Deny', success: policyResult.result === 'Allow' }">
                        {{ policyResult.result }}
                    </span>
                </div>
                <form class="form-grid single" @submit.prevent="simulatePolicy">
                    <label>
                        Subject ID
                        <input v-model.number="policyForm.subjectId" type="number" min="1" required />
                    </label>
                    <label>
                        Credential
                        <select v-model="policyForm.credentialType">
                            <option>QR</option>
                            <option>Badge</option>
                            <option>EmergencyOverride</option>
                        </select>
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.policy">
                        Simulate
                    </button>
                </form>
                <p v-if="policyResult.reason" class="inline-message">{{ policyResult.reason }}</p>
            </article>
        </section>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Identity</span>
                        <h2 class="panel-title">Provider and HR import</h2>
                    </div>
                </div>
                <form class="form-grid" @submit.prevent="saveProvider">
                    <label>
                        Provider name
                        <input v-model="providerForm.name" required />
                    </label>
                    <label>
                        Authority
                        <input v-model="providerForm.authority" required />
                    </label>
                    <label>
                        Client ID
                        <input v-model="providerForm.clientId" />
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.provider">
                        Save provider
                    </button>
                </form>
                <form class="form-grid stacked" @submit.prevent="importUser">
                    <label>
                        Provider ID
                        <input v-model.number="importForm.providerId" type="number" min="1" required />
                    </label>
                    <label>
                        External subject
                        <input v-model="importForm.externalSubject" required />
                    </label>
                    <label>
                        Username
                        <input v-model="importForm.username" required />
                    </label>
                    <label>
                        Full name
                        <input v-model="importForm.displayName" />
                    </label>
                    <label>
                        Email
                        <input v-model="importForm.email" type="email" />
                    </label>
                    <label>
                        Lifecycle
                        <select v-model="importForm.lifecycleStatus">
                            <option>Active</option>
                            <option>Suspended</option>
                            <option>Terminated</option>
                            <option>ContractorActive</option>
                            <option>ContractorExpired</option>
                        </select>
                    </label>
                    <button type="submit" class="btn btn-primary" :disabled="busy.importUser">
                        Import user
                    </button>
                </form>
            </article>

            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Devices</span>
                        <h2 class="panel-title">Simulator and fault drill</h2>
                    </div>
                </div>
                <form class="form-grid" @submit.prevent="createVirtualController">
                    <label>
                        Controller name
                        <input v-model="deviceForm.name" required />
                    </label>
                    <label>
                        Protocol
                        <select v-model="deviceForm.protocol">
                            <option>OSDP-Sim</option>
                            <option>ONVIF-Access-Sim</option>
                        </select>
                    </label>
                    <label>
                        Max credentials
                        <input v-model.number="deviceForm.maxCredentials" type="number" min="1" />
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.device">
                        Create simulator
                    </button>
                </form>
                <form class="form-grid stacked" @submit.prevent="injectFault">
                    <label>
                        Device ID
                        <input v-model.number="faultForm.securityDeviceId" type="number" min="1" required />
                    </label>
                    <label>
                        Fault
                        <select v-model="faultForm.status">
                            <option>Tamper</option>
                            <option>Offline</option>
                            <option>RelayFailure</option>
                            <option>BarrierStuck</option>
                        </select>
                    </label>
                    <label>
                        Severity
                        <select v-model="faultForm.severity">
                            <option>Medium</option>
                            <option>High</option>
                            <option>Critical</option>
                        </select>
                    </label>
                    <button type="submit" class="btn btn-primary" :disabled="busy.fault">
                        Inject fault
                    </button>
                </form>
            </article>
        </section>

        <section class="ops-grid three">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">SOC</span>
                        <h2 class="panel-title">Alarm intake</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="createAlarm">
                    <label>
                        Summary
                        <input v-model="alarmForm.summary" required />
                    </label>
                    <label>
                        Severity
                        <select v-model="alarmForm.severity">
                            <option>Medium</option>
                            <option>High</option>
                            <option>Critical</option>
                        </select>
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.alarm">
                        Create alarm
                    </button>
                </form>
                <div v-if="socIntel.summary" class="soc-intel-summary">
                    <span class="soft-chip" :class="riskChipClass">{{ riskLabel }}</span>
                    <p>{{ socIntel.summary }}</p>
                </div>
            </article>

            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Recovery</span>
                        <h2 class="panel-title">Backup drill</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="startBackup">
                    <label>
                        Profile
                        <select v-model="backupForm.profile">
                            <option>MediumCompany</option>
                            <option>LargeCompany</option>
                            <option>Production</option>
                        </select>
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.backup">
                        Start backup
                    </button>
                </form>
            </article>

            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Release</span>
                        <h2 class="panel-title">QA evidence</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="createQaRun">
                    <label>
                        Test type
                        <select v-model="qaForm.testType">
                            <option>E2E</option>
                            <option>LoadStressSoakChaos</option>
                            <option>HardwareSimulator</option>
                            <option>Migration</option>
                        </select>
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.qa">
                        Record QA run
                    </button>
                </form>
            </article>
        </section>

        <!-- Ops Workspace: Restore, Security Checks, Outbox -->
        <section v-if="selectedWorkspace === 'ops'" class="ops-grid three">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Restore</span>
                        <h2 class="panel-title">Restore Drill</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="startRestore">
                    <label>
                        Backup Run ID
                        <input v-model.number="restoreForm.backupRunId" type="number" required />
                    </label>
                    <label>
                        Target RTO (minutes)
                        <input v-model.number="restoreForm.targetRtoMinutes" type="number" />
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.restore">
                        {{ busy.restore ? 'Starting...' : 'Start Restore' }}
                    </button>
                </form>
                <div v-if="restoreResult" class="success-card" style="margin-top:8px;">{{ restoreResult }}</div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Security</span>
                        <h2 class="panel-title">Security Checks</h2>
                    </div>
                </div>
                <form class="form-grid single" @submit.prevent="recordSecurityCheck">
                    <label>
                        Check Type
                        <select v-model="securityForm.checkType">
                            <option value="PhysicalPatrol">Physical Patrol</option>
                            <option value="CameraReview">Camera Review</option>
                            <option value="DoorAudit">Door Audit</option>
                            <option value="PerimeterCheck">Perimeter Check</option>
                            <option value="ComplianceAudit">Compliance Audit</option>
                        </select>
                    </label>
                    <label>
                        Status
                        <select v-model="securityForm.status">
                            <option value="Pass">Pass</option>
                            <option value="Fail">Fail</option>
                            <option value="Degraded">Degraded</option>
                        </select>
                    </label>
                    <label>
                        Notes
                        <textarea v-model="securityForm.notes" class="form-input" rows="2"></textarea>
                    </label>
                    <button type="submit" class="btn btn-secondary" :disabled="busy.security">
                        {{ busy.security ? 'Recording...' : 'Record Check' }}
                    </button>
                </form>
                <div v-if="securityResult" class="success-card" style="margin-top:8px;">{{ securityResult }}</div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Outbox</span>
                        <h2 class="panel-title">Outbox & Webhooks</h2>
                    </div>
                </div>
                <div class="incident-brief-form" style="flex-wrap:wrap;">
                    <select v-model="outboxFilter" class="filter-select" style="flex:1;">
                        <option value="">All</option>
                        <option value="Pending">Pending</option>
                        <option value="Failed">Failed</option>
                        <option value="Delivered">Delivered</option>
                    </select>
                    <button class="btn btn-primary btn-sm" :disabled="outboxLoading" @click="loadOutboxEvents">
                        {{ outboxLoading ? 'Loading...' : 'Load' }}
                    </button>
                </div>
                <div v-if="outboxEvents.length === 0" class="empty-card">No outbox events.</div>
                <div v-else class="table-container" style="max-height:200px;overflow-y:auto;">
                    <table class="data-table">
                        <thead><tr><th>Type</th><th>Status</th><th>Retry</th></tr></thead>
                        <tbody>
                            <tr v-for="e in outboxEvents" :key="e.outboxEventId">
                                <td class="table-sub">{{ (e.eventType || '').substring(0, 20) }}</td>
                                <td><span class="soft-chip" :class="e.status === 'Failed' ? 'danger' : e.status === 'Delivered' ? 'success' : 'warn'">{{ e.status }}</span></td>
                                <td><span v-if="e.retryCount != null" class="text-muted">{{ e.retryCount }}</span></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>

        <!-- Ops backup list -->
        <section v-if="selectedWorkspace === 'ops'" class="ops-grid two" style="margin-top:0.5rem;">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Backups</span>
                        <h2 class="panel-title">Recent Backup Runs</h2>
                    </div>
                    <button class="btn btn-sm btn-secondary" :disabled="backupLoading" @click="loadBackupRuns">Refresh</button>
                </div>
                <div v-if="backupLoading" class="empty-card">Loading...</div>
                <div v-else-if="backupRuns.length === 0" class="empty-card">No backup runs.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Profile</th><th>Status</th><th>Started</th><th>RPO</th></tr></thead>
                        <tbody>
                            <tr v-for="b in backupRuns" :key="b.backupRunId">
                                <td>{{ b.profile || '—' }}</td>
                                <td><span class="soft-chip" :class="b.status === 'Completed' ? 'success' : b.status === 'Failed' ? 'danger' : 'warn'">{{ b.status }}</span></td>
                                <td class="table-sub">{{ new Date(b.startedAtUtc).toLocaleString() }}</td>
                                <td>{{ b.achievedRpoMinutes || '—' }}m</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Restore</span>
                        <h2 class="panel-title">Restore Drills</h2>
                    </div>
                    <button class="btn btn-sm btn-secondary" :disabled="restoreLoading" @click="loadRestoreDrills">Refresh</button>
                </div>
                <div v-if="restoreLoading" class="empty-card">Loading...</div>
                <div v-else-if="restoreDrills.length === 0" class="empty-card">No restore drills.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Status</th><th>RTO Target</th><th>Started</th></tr></thead>
                        <tbody>
                            <tr v-for="r in restoreDrills" :key="r.restoreDrillId">
                                <td>{{ r.restoreDrillId }}</td>
                                <td><span class="soft-chip" :class="r.status === 'Completed' ? 'success' : r.status === 'Failed' ? 'danger' : 'warn'">{{ r.status }}</span></td>
                                <td>{{ r.targetRtoMinutes || '—' }}m</td>
                                <td class="table-sub">{{ new Date(r.startedAtUtc).toLocaleString() }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>

        <section v-if="selectedWorkspace === 'soc'" class="ops-grid two">
            <!-- SOC content (same as before) -->
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI Intelligence</span>
                        <h2 class="panel-title">SOC Analytics</h2>
                    </div>
                    <button type="button" class="btn btn-sm btn-secondary" @click="loadSocIntel">Refresh</button>
                </div>
                <div class="soc-stats-grid">
                    <div class="soc-stat">
                        <strong>{{ socIntel.statistics.totalToday }}</strong>
                        <span>Alarm hom nay</span>
                        <span class="soc-change" :class="{ up: socIntel.statistics.changePercent > 0, down: socIntel.statistics.changePercent < 0 }">
                            {{ socIntel.statistics.changePercent > 0 ? '+' : '' }}{{ socIntel.statistics.changePercent }}%
                        </span>
                    </div>
                    <div class="soc-stat">
                        <strong class="text-danger">{{ socIntel.statistics.criticalOpenAlarms }}</strong>
                        <span>Critical dang mo</span>
                    </div>
                    <div class="soc-stat">
                        <strong>{{ socIntel.statistics.openAlarms }}</strong>
                        <span>Tong alarm mo</span>
                    </div>
                    <div class="soc-stat">
                        <strong>{{ socIntel.statistics.avgResolutionHours }}</strong>
                        <span>Gio xu ly TB</span>
                    </div>
                </div>
                <div v-if="Object.keys(socIntel.statistics.bySeverity).length" class="soc-severity-breakdown">
                    <h4>Phan bo theo muc do</h4>
                    <div v-for="(count, sev) in socIntel.statistics.bySeverity" :key="sev" class="severity-bar-row">
                        <span>{{ sev }}</span>
                        <div class="severity-bar-track">
                            <div class="severity-bar-fill" :class="'sev-' + sev.toLowerCase()" :style="{ width: (count / Math.max(...Object.values(socIntel.statistics.bySeverity)) * 100) + '%' }"></div>
                        </div>
                        <span>{{ count }}</span>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI Copilot</span>
                        <h2 class="panel-title">Incident Analysis</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="incidentBriefing.incidentId" type="number" min="1" placeholder="Incident ID" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="incidentBriefing.loading" @click="analyzeIncident">
                        {{ incidentBriefing.loading ? 'Dang phan tich...' : 'Phan tich bang AI' }}
                    </button>
                </div>
                <div v-if="incidentBriefing.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(incidentBriefing.result.severity)">
                            {{ incidentBriefing.result.severity || 'N/A' }}
                        </span>
                        <small>Provider: {{ incidentBriefing.result.provider || 'N/A' }}</small>
                    </div>
                    <p class="brief-summary">{{ incidentBriefing.result.summary }}</p>
                    <div v-if="incidentBriefing.result.reasoningSummary" class="rec-reasoning">
                        <strong>Phan tich:</strong>
                        <p>{{ incidentBriefing.result.reasoningSummary }}</p>
                    </div>
                    <div v-if="incidentBriefing.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(incidentBriefing.result.recommendationId)">Phe duyet</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(incidentBriefing.result.recommendationId)">Tu choi</button>
                    </div>
                </div>
            </article>
        </section>

        <section v-if="selectedWorkspace === 'reception'" class="ops-grid two">
            <!-- Reception content -->
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI Risk Screening</span>
                        <h2 class="panel-title">Visitor Screening</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="visitorScreening.visitId" type="number" min="1" placeholder="Visit ID" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="visitorScreening.loading" @click="screenVisitor">
                        {{ visitorScreening.loading ? 'Dang phan tich...' : 'Phan tich riu ro' }}
                    </button>
                </div>
                <div v-if="visitorScreening.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(visitorScreening.result.severity)">
                            {{ visitorScreening.result.severity || 'N/A' }}
                        </span>
                        <small>Provider: {{ visitorScreening.result.provider || 'N/A' }}</small>
                    </div>
                    <p class="brief-summary">{{ visitorScreening.result.summary }}</p>
                    <div v-if="visitorScreening.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(visitorScreening.result.recommendationId)">Phe duyet</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(visitorScreening.result.recommendationId)">Tu choi</button>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Quick actions</span>
                        <h2 class="panel-title">Screening log</h2>
                    </div>
                </div>
                <div class="empty-card">Cac ket qua phan tich riu ro khach tham se hien o day.</div>
            </article>
        </section>

        <section v-if="selectedWorkspace === 'gate'" class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI Risk Screening</span>
                        <h2 class="panel-title">Vehicle Screening</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="vehicleScreening.vehicleId" type="number" min="1" placeholder="Vehicle ID" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="vehicleScreening.loading" @click="screenVehicle">
                        {{ vehicleScreening.loading ? 'Dang phan tich...' : 'Phan tich riu ro' }}
                    </button>
                </div>
                <div v-if="vehicleScreening.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(vehicleScreening.result.severity)">
                            {{ vehicleScreening.result.severity || 'N/A' }}
                        </span>
                        <small>Provider: {{ vehicleScreening.result.provider || 'N/A' }}</small>
                    </div>
                    <p class="brief-summary">{{ vehicleScreening.result.summary }}</p>
                    <div v-if="vehicleScreening.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(vehicleScreening.result.recommendationId)">Phe duyet</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(vehicleScreening.result.recommendationId)">Tu choi</button>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Quick actions</span>
                        <h2 class="panel-title">Screening log</h2>
                    </div>
                </div>
                <div class="empty-card">Cac ket qua phan tich riu ro phuong tien se hien o day.</div>
            </article>
        </section>

        <section v-if="selectedWorkspace === 'auditor'" class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI Phan tich</span>
                        <h2 class="panel-title">Evidence Analysis</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="evidenceAnalysis.evidenceId" type="number" min="1" placeholder="Evidence Item ID" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="evidenceAnalysis.loading" @click="analyzeEvidence">
                        {{ evidenceAnalysis.loading ? 'Dang phan tich...' : 'Phan tich bang AI' }}
                    </button>
                </div>
                <div v-if="evidenceAnalysis.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(evidenceAnalysis.result.severity)">
                            {{ evidenceAnalysis.result.severity || 'N/A' }}
                        </span>
                        <small>Provider: {{ evidenceAnalysis.result.provider || 'N/A' }}</small>
                    </div>
                    <p class="brief-summary">{{ evidenceAnalysis.result.summary }}</p>
                    <div v-if="evidenceAnalysis.result.reasoningSummary" class="rec-reasoning">
                        <strong>Phan tich:</strong>
                        <p>{{ evidenceAnalysis.result.reasoningSummary }}</p>
                    </div>
                    <div v-if="evidenceAnalysis.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(evidenceAnalysis.result.recommendationId)">Phe duyet</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(evidenceAnalysis.result.recommendationId)">Tu choi</button>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI Kiem tra</span>
                        <h2 class="panel-title">Export Request Review</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="evidenceExport.exportId" type="number" min="1" placeholder="Export Request ID" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="evidenceExport.loading" @click="reviewExport">
                        {{ evidenceExport.loading ? 'Dang kiem tra...' : 'Kiem tra xuat' }}
                    </button>
                </div>
                <div v-if="evidenceExport.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(evidenceExport.result.severity)">
                            {{ evidenceExport.result.severity || 'N/A' }}
                        </span>
                        <small>Provider: {{ evidenceExport.result.provider || 'N/A' }}</small>
                    </div>
                    <p class="brief-summary">{{ evidenceExport.result.summary }}</p>
                    <div v-if="evidenceExport.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(evidenceExport.result.recommendationId)">Phe duyet</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(evidenceExport.result.recommendationId)">Tu choi</button>
                    </div>
                </div>
            </article>
        </section>

        <section v-if="selectedWorkspace === 'admin'" class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI Chinh sach</span>
                        <h2 class="panel-title">Policy Simulator</h2>
                    </div>
                </div>
                <div class="incident-brief-form">
                    <input v-model.number="policySimulation.policyId" type="number" min="1" placeholder="Policy Version ID" class="filter-input" />
                    <button class="btn btn-primary btn-sm" :disabled="policySimulation.loading" @click="simulateAiPolicy">
                        {{ policySimulation.loading ? 'Dang mo phong...' : 'Mo phong chinh sach' }}
                    </button>
                    <button class="btn btn-secondary btn-sm" :disabled="policySimulation.loading" @click="explainAiPolicy">
                        Giai thich
                    </button>
                </div>
                <div v-if="policySimulation.result" class="ai-brief-result">
                    <div class="rec-header">
                        <span class="soft-chip" :class="sevClass(policySimulation.result.severity)">
                            {{ policySimulation.result.severity || 'N/A' }}
                        </span>
                        <small>Provider: {{ policySimulation.result.provider || 'N/A' }}</small>
                    </div>
                    <p class="brief-summary">{{ policySimulation.result.summary }}</p>
                    <div v-if="policySimulation.result.reasoningSummary" class="rec-reasoning">
                        <strong>Phan tich:</strong>
                        <p>{{ policySimulation.result.reasoningSummary }}</p>
                    </div>
                    <div v-if="policySimulation.result.recommendationId" class="rec-actions">
                        <button class="btn btn-success btn-sm" @click="approveAi(policySimulation.result.recommendationId)">Phe duyet</button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAi(policySimulation.result.recommendationId)">Tu choi</button>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI Chinh sach</span>
                        <h2 class="panel-title">Policy Explanation</h2>
                    </div>
                </div>
                <div class="empty-card">Nhap Policy Version ID va chon "Giai thich" de xem phan tich chinh sach bang ngon ngu tu nhien. AI se giai thich muc dich, nguoi bi anh huong, va cac buoc tiep theo.</div>
            </article>
        </section>

        <section class="ops-panel audit-panel">
            <div class="panel-head compact">
                <div>
                    <span class="panel-kicker">Activity</span>
                    <h2 class="panel-title">Latest local actions</h2>
                </div>
            </div>
            <div v-if="activityLog.length" class="activity-list">
                <div v-for="item in activityLog" :key="item.id" class="activity-row">
                    <span>{{ item.time }}</span>
                    <strong>{{ item.title }}</strong>
                    <p>{{ item.detail }}</p>
                </div>
            </div>
            <div v-else class="empty-card">No local actions in this session.</div>
        </section>
    </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { enterpriseApi, socIntelApi } from '../services/enterpriseSecurityApi'
import { enterpriseAiApi } from '../services/enterpriseAiApi'

const loading = ref(false)
const loadError = ref('')
const selectedWorkspace = ref('admin')
const selectedAction = ref('')
const activityLog = ref([])

const overview = reactive({
    foundation: {},
    identity: {},
    policy: {},
    visitorVehicle: {},
    devices: {},
    soc: {},
    evidence: {},
    operations: {},
    release: {},
})
const configHealth = reactive({
    status: '',
    findings: [],
})
const socIntel = reactive({
    summary: '',
    overallRisk: 'thap',
    statistics: { totalToday: 0, totalYesterday: 0, changePercent: 0, openAlarms: 0, criticalOpenAlarms: 0, avgResolutionHours: 0, bySeverity: {}, byType: {}, byHour: {} },
    anomalies: [],
})

const incidentBriefing = reactive({ incidentId: null, loading: false, result: null })
const evidenceAnalysis = reactive({ evidenceId: null, loading: false, result: null })
const evidenceExport = reactive({ exportId: null, loading: false, result: null })
const deviceInsights = reactive({ loading: false, items: [] })
const visitorScreening = reactive({ visitId: null, loading: false, result: null })
const vehicleScreening = reactive({ vehicleId: null, loading: false, result: null })
const policySimulation = reactive({ policyId: null, loading: false, result: null })

const eventFeed = reactive({
    events: [], loading: false,
    filter: { sourceType: '', eventType: '', cameraId: null, subjectId: '', limit: 20 },
})

const nlQuery = reactive({ queryText: '', loading: false, result: null })

const assetMap = reactive({ gates: [], cameras: [], vehicles: [] })

const busy = reactive({
    stepUp: false, provider: false, importUser: false, device: false,
    fault: false, alarm: false, backup: false, qa: false, backfill: false,
    policy: false, ai: false, restore: false, security: false,
})

const stepUp = reactive({ action: 'AllPrivilegedActions', password: '', mfaCode: '', sessionId: null, active: false, message: '' })

const providerForm = reactive({ name: 'Corporate IdP', protocol: 'OIDC', authority: 'https://idp.company.local', clientId: 'v-shield', isEnabled: true })
const importForm = reactive({ providerId: 1, externalSubject: 'employee-001', username: 'employee.001', displayName: 'Employee 001', email: 'employee.001@company.local', phone: '', role: 'Staff', lifecycleStatus: 'Active', primarySiteId: null })
const deviceForm = reactive({ name: 'Virtual Controller 01', protocol: 'OSDP-Sim', direction: 'Entry', maxCredentials: 50000 })
const faultForm = reactive({ securityDeviceId: null, status: 'Tamper', severity: 'High', message: 'Operator drill' })
const alarmForm = reactive({ summary: 'Manual SOC drill alarm', severity: 'High' })
const backupForm = reactive({ profile: 'MediumCompany' })
const qaForm = reactive({ testType: 'LoadStressSoakChaos' })
const backfillForm = reactive({ companyName: 'V-Shield Company', companyCode: 'VSHIELD', siteName: 'Headquarters', siteCode: 'HQ', timeZoneId: 'Asia/Ho_Chi_Minh' })
const policyForm = reactive({ subjectType: 'Employee', subjectId: 1, siteId: null, securityZoneId: null, accessPointId: null, credentialType: 'QR', allowHolidayAccess: false, evaluatedAtUtc: null })
const policyResult = reactive({ result: '', reason: '', decisionMode: '' })

// Ops workspace state
const restoreForm = reactive({ backupRunId: null, targetRtoMinutes: 60 })
const restoreResult = ref('')
const securityForm = reactive({ checkType: 'PhysicalPatrol', status: 'Pass', notes: '' })
const securityResult = ref('')
const outboxFilter = ref('')
const outboxLoading = ref(false)
const outboxEvents = ref([])
const backupLoading = ref(false)
const backupRuns = ref([])
const restoreLoading = ref(false)
const restoreDrills = ref([])

const statusMessage = computed(() => {
    if (loadError.value) return loadError.value
    if (loading.value) return 'Refreshing enterprise security data.'
    return 'Operational views are connected to enterprise APIs and local acceptance gates.'
})

const headlineMetrics = computed(() => [
    { label: 'Sites', value: overview.foundation.sites || 0, note: `${overview.foundation.accessPoints || 0} access points` },
    { label: 'Open alarms', value: overview.soc.openAlarms || 0, note: `${overview.soc.criticalOpenAlarms || 0} critical` },
    { label: 'Devices', value: overview.devices.devices || 0, note: `${overview.devices.offlinePackages || 0} offline packages` },
    { label: 'Evidence', value: overview.evidence.evidenceItems || 0, note: `${overview.evidence.pendingExports || 0} pending exports` },
    { label: 'Outbox', value: overview.operations.pendingOutboxEvents || 0, note: `${overview.operations.failedOutboxEvents || 0} failed` },
    { label: 'Release gates', value: overview.release.pendingRequiredGates || 0, note: `${overview.release.approvedReleaseCandidates || 0} approved releases` },
])

const workspaces = computed(() => [
    {
        id: 'admin', label: 'Admin', kicker: 'Administration', title: 'Foundation and identity',
        badge: `${overview.identity.activeMappings || 0} active mappings`,
        metrics: [
            { label: 'Companies', value: overview.foundation.companies || 0 },
            { label: 'Identity providers', value: overview.identity.enabledProviders || 0 },
            { label: 'Terminated users', value: overview.identity.terminatedEmployees || 0 },
        ],
        actions: ['Provider', 'HR import', 'Recertification'],
    },
    {
        id: 'soc', label: 'SOC', kicker: 'Command center', title: 'Alarms and incidents',
        badge: `${overview.soc.openIncidents || 0} open incidents`,
        metrics: [
            { label: 'Open alarms', value: overview.soc.openAlarms || 0 },
            { label: 'Active SOPs', value: overview.soc.activeSops || 0 },
            { label: 'Dispatch tasks', value: overview.soc.openDispatchTasks || 0 },
        ],
        actions: ['Acknowledge', 'Dispatch', 'Handover'],
    },
    {
        id: 'reception', label: 'Reception', kicker: 'Visitor desk', title: 'Visits and watchlists',
        badge: `${overview.visitorVehicle.watchlistMatches || 0} matches`,
        metrics: [
            { label: 'Visits', value: overview.visitorVehicle.visits || 0 },
            { label: 'Credentials', value: overview.visitorVehicle.visitorCredentials || 0 },
            { label: 'Watchlist entries', value: overview.visitorVehicle.watchlistEntries || 0 },
        ],
        actions: ['Check-in', 'Forms', 'Overstay'],
    },
    {
        id: 'gate', label: 'Gate', kicker: 'Vehicle lanes', title: 'Parking and barrier review',
        badge: `${overview.visitorVehicle.barriers || 0} barriers`,
        metrics: [
            { label: 'Parking permits', value: overview.visitorVehicle.parkingPermits || 0 },
            { label: 'Lane events', value: overview.visitorVehicle.laneEvents || 0 },
            { label: 'Barrier commands', value: overview.visitorVehicle.barrierCommands || 0 },
        ],
        actions: ['Plate review', 'Open barrier', 'Exception'],
    },
    {
        id: 'auditor', label: 'Auditor', kicker: 'Governance', title: 'Evidence and compliance',
        badge: `${overview.evidence.activeLegalHolds || 0} legal holds`,
        metrics: [
            { label: 'Collections', value: overview.evidence.collections || 0 },
            { label: 'Access logs', value: overview.evidence.accessLogs || 0 },
            { label: 'Reports', value: overview.evidence.complianceReports || 0 },
        ],
        actions: ['Export review', 'Retention', 'Report'],
    },
    {
        id: 'ops', label: 'Ops', kicker: 'Resilience', title: 'Backup, restore & security',
        badge: `${overview.operations.degradedDependencies || 0} degraded`,
        metrics: [
            { label: 'Backups', value: overview.operations.backupRuns || 0 },
            { label: 'Restore drills', value: overview.operations.restoreDrills || 0 },
            { label: 'Security checks', value: overview.operations.securityChecks || 0 },
        ],
        actions: ['Outbox', 'Backup', 'Restore', 'Security'],
    },
])

const activeWorkspace = computed(() =>
    workspaces.value.find((workspace) => workspace.id === selectedWorkspace.value) || workspaces.value[0]
)

const visibleFindings = computed(() =>
    (configHealth.findings || []).filter((finding) => finding.status !== 'Pass').slice(0, 5)
)

const riskLabel = computed(() => {
    switch (socIntel.overallRisk) {
        case 'cao': return 'Rui ro cao'
        case 'trung_binh': return 'Rui ro TB'
        default: return 'Rui ro thap'
    }
})

const riskChipClass = computed(() => {
    switch (socIntel.overallRisk) {
        case 'cao': return 'danger'
        case 'trung_binh': return 'warning'
        default: return 'success'
    }
})

async function loadOverview() {
    loading.value = true
    loadError.value = ''
    try {
        const [foundation, identity, policy, visitorVehicle, devices, soc, evidence, operations, release] = await enterpriseApi.overview()
        Object.assign(overview.foundation, normalizeKeys(foundation.data))
        Object.assign(overview.identity, normalizeKeys(identity.data))
        Object.assign(overview.policy, normalizeKeys(policy.data))
        Object.assign(overview.visitorVehicle, normalizeKeys(visitorVehicle.data))
        Object.assign(overview.devices, normalizeKeys(devices.data))
        Object.assign(overview.soc, normalizeKeys(soc.data))
        Object.assign(overview.evidence, normalizeKeys(evidence.data))
        Object.assign(overview.operations, normalizeKeys(operations.data))
        Object.assign(overview.release, normalizeKeys(release.data))

        const [configResult, assetResult] = await Promise.allSettled([
            enterpriseApi.configHealth(),
            enterpriseApi.assetMap(),
        ])
        if (configResult.status === 'fulfilled') {
            const normalized = normalizeKeys(configResult.value.data)
            configHealth.status = normalized.status || ''
            configHealth.findings = normalized.findings || []
        }
        if (assetResult.status === 'fulfilled') {
            const normalized = normalizeKeys(assetResult.value.data)
            assetMap.gates = normalized.gates || []
            assetMap.cameras = normalized.cameras || []
            assetMap.vehicles = normalized.vehicles || []
        }
        loadSocIntel()
    } catch (error) {
        loadError.value = error.response?.data?.message || 'Cannot load enterprise security data.'
    } finally {
        loading.value = false
    }
}

async function loadSocIntel() {
    try {
        const intel = await socIntelApi.getIntelligence()
        Object.assign(socIntel, normalizeKeys(intel.data))
    } catch {}
}

const sevClass = (sev) => {
    switch ((sev || '').toLowerCase()) {
        case 'critical': return 'danger'
        case 'high': return 'danger'
        case 'medium': return 'warning'
        default: return 'success'
    }
}

async function analyzeIncident() {
    if (!incidentBriefing.incidentId) return
    incidentBriefing.loading = true
    incidentBriefing.result = null
    try {
        const { data } = await enterpriseAiApi.analyzeIncident(incidentBriefing.incidentId)
        incidentBriefing.result = data
        pushActivity('AI Incident Briefing', `Incident #${incidentBriefing.incidentId} analyzed`)
    } catch (error) {
        incidentBriefing.result = { severity: 'Medium', summary: 'Khong the phan tich: ' + (error.response?.data?.message || error.message), provider: 'Error', recommendationId: null }
    } finally { incidentBriefing.loading = false }
}

async function analyzeEvidence() {
    if (!evidenceAnalysis.evidenceId) return
    evidenceAnalysis.loading = true
    evidenceAnalysis.result = null
    try {
        const { data } = await enterpriseAiApi.analyzeEvidence(evidenceAnalysis.evidenceId)
        evidenceAnalysis.result = data
        pushActivity('AI Evidence Analysis', `Evidence #${evidenceAnalysis.evidenceId} analyzed`)
    } catch (error) {
        evidenceAnalysis.result = { severity: 'Medium', summary: 'Khong the phan tich: ' + (error.response?.data?.message || error.message), provider: 'Error', recommendationId: null }
    } finally { evidenceAnalysis.loading = false }
}

async function reviewExport() {
    if (!evidenceExport.exportId) return
    evidenceExport.loading = true
    evidenceExport.result = null
    try {
        const { data } = await enterpriseAiApi.reviewExportRequest(evidenceExport.exportId)
        evidenceExport.result = data
        pushActivity('AI Export Review', `Export #${evidenceExport.exportId} reviewed`)
    } catch (error) {
        evidenceExport.result = { severity: 'Medium', summary: 'Khong the kiem tra: ' + (error.response?.data?.message || error.message), provider: 'Error', recommendationId: null }
    } finally { evidenceExport.loading = false }
}

async function approveAi(id) { if (!id) return; try { await enterpriseAiApi.reviewRecommendation(id, 'Approved', 'Phe duyet sau khi xem xet'); pushActivity('AI Recommendation', `Approved #${id}`) } catch {} }
async function rejectAi(id) { if (!id) return; try { await enterpriseAiApi.reviewRecommendation(id, 'Rejected', 'Khong dong y'); pushActivity('AI Recommendation', `Rejected #${id}`) } catch {} }

async function screenVisitor() {
    if (!visitorScreening.visitId) return
    visitorScreening.loading = true; visitorScreening.result = null
    try { const { data } = await enterpriseAiApi.screenVisitor(visitorScreening.visitId); visitorScreening.result = data; pushActivity('AI Visitor Screening', `Visit #${visitorScreening.visitId} screened`) }
    catch (error) { visitorScreening.result = { severity: 'Medium', summary: 'Khong the phan tich: ' + (error.response?.data?.message || error.message), provider: 'Error', recommendationId: null } }
    finally { visitorScreening.loading = false }
}

async function screenVehicle() {
    if (!vehicleScreening.vehicleId) return
    vehicleScreening.loading = true; vehicleScreening.result = null
    try { const { data } = await enterpriseAiApi.screenVehicle(vehicleScreening.vehicleId); vehicleScreening.result = data; pushActivity('AI Vehicle Screening', `Vehicle #${vehicleScreening.vehicleId} screened`) }
    catch (error) { vehicleScreening.result = { severity: 'Medium', summary: 'Khong the phan tich: ' + (error.response?.data?.message || error.message), provider: 'Error', recommendationId: null } }
    finally { vehicleScreening.loading = false }
}

async function simulateAiPolicy() {
    if (!policySimulation.policyId) return
    policySimulation.loading = true; policySimulation.result = null
    try { const { data } = await enterpriseAiApi.simulatePolicy(policySimulation.policyId); policySimulation.result = data; pushActivity('AI Policy Simulate', `Policy #${policySimulation.policyId} simulated`) }
    catch (error) { policySimulation.result = { severity: 'Low', summary: 'Khong the mo phong: ' + (error.response?.data?.message || error.message), provider: 'Error', recommendationId: null } }
    finally { policySimulation.loading = false }
}

async function explainAiPolicy() {
    if (!policySimulation.policyId) return
    policySimulation.loading = true; policySimulation.result = null
    try { const { data } = await enterpriseAiApi.explainPolicy(policySimulation.policyId); policySimulation.result = data; pushActivity('AI Policy Explain', `Policy #${policySimulation.policyId} explained`) }
    catch (error) { policySimulation.result = { severity: 'Low', summary: 'Khong the giai thich: ' + (error.response?.data?.message || error.message), provider: 'Error', recommendationId: null } }
    finally { policySimulation.loading = false }
}

watch(selectedWorkspace, (ws) => {
    if (ws === 'soc') { loadSocIntel() }
    if (ws === 'ops') { loadBackupRuns(); loadRestoreDrills(); loadOutboxEvents() }
})

async function verifyStepUp() {
    busy.stepUp = true; stepUp.message = ''
    try {
        const start = await enterpriseApi.stepUpStart(stepUp.action, 'Operator console verification')
        const verified = await enterpriseApi.stepUpVerify(start.data.sessionId, stepUp.password, stepUp.mfaCode)
        stepUp.sessionId = verified.data.sessionId; stepUp.active = verified.data.active
        enterpriseApi.setStepUpSession(verified.data.sessionId)
        stepUp.message = 'Verified until ' + formatDateTime(verified.data.expiresAtUtc)
        pushActivity('Step-up verified', stepUp.action)
    } catch (error) {
        stepUp.active = false; stepUp.message = error.response?.data?.message || 'Verification failed.'
    } finally { busy.stepUp = false }
}

async function saveProvider() { await runAction('provider', 'Provider saved', () => enterpriseApi.upsertIdentityProvider(providerForm)) }
async function importUser() { const user = { ...importForm }; const pid = user.providerId; delete user.providerId; await runAction('importUser', 'User import recorded', () => enterpriseApi.importIdentityUsers(pid, [user])) }
async function createVirtualController() { await runAction('device', 'Virtual controller created', () => enterpriseApi.createVirtualController(deviceForm)) }
async function injectFault() { await runAction('fault', 'Simulator fault injected', () => enterpriseApi.injectSimulatorFault(faultForm)) }
async function createAlarm() { await runAction('alarm', 'Alarm created', () => enterpriseApi.createAlarm({ alarmType: 'ManualDrill', severity: alarmForm.severity, summary: alarmForm.summary })) }
async function startBackup() { await runAction('backup', 'Backup run started', () => enterpriseApi.startBackup({ profile: backupForm.profile, targetRpoMinutes: 15, targetRtoMinutes: 60, notes: 'Started from enterprise console' })) }
async function createQaRun() { await runAction('qa', 'QA run recorded', () => enterpriseApi.createQaRun({ testType: qaForm.testType, profile: 'MediumCompany', evidenceReference: '/qa/local-enterprise-console', notes: 'Recorded from enterprise console' })) }
async function backfillDefaultSite() { await runAction('backfill', 'Foundation backfill completed', () => enterpriseApi.backfillDefaultSite(backfillForm)) }

async function simulatePolicy() {
    await runAction('policy', 'Policy simulation completed', async () => {
        const response = await enterpriseApi.simulateAccessPolicy({ ...policyForm, evaluatedAtUtc: policyForm.evaluatedAtUtc || new Date().toISOString() })
        policyResult.result = response.data?.result || ''; policyResult.reason = response.data?.reason || ''; policyResult.decisionMode = response.data?.decisionMode || ''
        return response
    })
}

// --- Ops workspace actions ---
async function startRestore() {
    if (!restoreForm.backupRunId) return
    busy.restore = true; restoreResult.value = ''
    try {
        await enterpriseApi.startRestore({ backupRunId: restoreForm.backupRunId, targetRtoMinutes: restoreForm.targetRtoMinutes })
        restoreResult.value = 'Restore drill started!'
        restoreForm.backupRunId = null
        await loadRestoreDrills()
    } catch (e) { restoreResult.value = 'Failed: ' + (e.response?.data?.message || e.message) }
    finally { busy.restore = false }
}

async function recordSecurityCheck() {
    busy.security = true; securityResult.value = ''
    try {
        await enterpriseApi.recordSecurityCheck({
            checkType: securityForm.checkType,
            status: securityForm.status,
            notes: securityForm.notes || null,
        })
        securityResult.value = 'Security check recorded!'
        securityForm.notes = ''
    } catch (e) { securityResult.value = 'Failed: ' + (e.response?.data?.message || e.message) }
    finally { busy.security = false }
}

async function loadOutboxEvents() {
    outboxLoading.value = true; outboxEvents.value = []
    try {
        const params = { pageSize: 20 }
        if (outboxFilter.value) params.status = outboxFilter.value
        const res = await enterpriseApi.getOutboxEvents(params)
        outboxEvents.value = res.data?.items || []
    } catch { outboxEvents.value = [] }
    finally { outboxLoading.value = false }
}

async function loadBackupRuns() {
    backupLoading.value = true; backupRuns.value = []
    try {
        const res = await enterpriseApi.getBackupRuns({ pageSize: 10 })
        backupRuns.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
    } catch { backupRuns.value = [] }
    finally { backupLoading.value = false }
}

async function loadRestoreDrills() {
    restoreLoading.value = true; restoreDrills.value = []
    try {
        const res = await enterpriseApi.getRestoreDrills({ pageSize: 10 })
        restoreDrills.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
    } catch { restoreDrills.value = [] }
    finally { restoreLoading.value = false }
}

async function runAction(key, title, action) {
    busy[key] = true
    try {
        const response = await action()
        pushActivity(title, response.data?.message || JSON.stringify(response.data).slice(0, 140))
        await loadOverview()
    } catch (error) { pushActivity(title + ' failed', error.response?.data?.message || error.message) }
    finally { busy[key] = false }
}

function pushActivity(title, detail) {
    activityLog.value.unshift({ id: `${Date.now()}-${Math.random()}`, title, detail, time: new Date().toLocaleTimeString() })
    activityLog.value = activityLog.value.slice(0, 8)
}

function normalizeKeys(data) {
    return Object.fromEntries(Object.entries(data || {}).map(([key, value]) => [key.charAt(0).toLowerCase() + key.slice(1), value]))
}

function formatDateTime(value) { if (!value) return ''; return new Date(value).toLocaleString() }

onMounted(loadOverview)
</script>

<style scoped>
.enterprise-page { display: flex; flex-direction: column; gap: 22px; }
.readiness-band { display: grid; grid-template-columns: auto minmax(0, 1fr) auto; align-items: center; gap: 24px; padding: 22px; border-radius: 18px; border: 1px solid var(--border-soft); background: linear-gradient(135deg, rgba(18, 75, 91, 0.92), rgba(18, 36, 52, 0.96)); color: #f7fcff; box-shadow: var(--shadow-card); }
.readiness-score { width: 112px; height: 112px; border-radius: 999px; display: grid; place-content: center; text-align: center; border: 1px solid rgba(255, 255, 255, 0.24); background: rgba(255, 255, 255, 0.08); }
.readiness-score span { font-size: 0.74rem; text-transform: uppercase; color: rgba(247, 252, 255, 0.72); }
.readiness-score strong { font-size: 2rem; line-height: 1; }
.readiness-copy h2 { margin: 0 0 8px; font-size: 1.35rem; }
.readiness-copy p { margin: 0; color: rgba(247, 252, 255, 0.76); }
.status-pill { display: inline-flex; align-items: center; min-height: 36px; padding: 0 14px; border-radius: 999px; background: rgba(77, 216, 180, 0.16); color: #bbffe8; font-weight: 700; }
.status-pill.danger { background: rgba(236, 91, 91, 0.18); color: #ffd0d0; }
.workspace-tabs { display: flex; flex-wrap: wrap; gap: 10px; }
.workspace-tabs button { min-height: 40px; padding: 0 16px; border-radius: 999px; border: 1px solid var(--border-soft); background: var(--surface); color: var(--text-secondary); font-weight: 700; }
.workspace-tabs button.active { color: #05313b; background: #8ceaf4; border-color: #8ceaf4; }
.workspace-summary { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; }
.workspace-stat { min-height: 86px; padding: 14px; border-radius: 14px; border: 1px solid var(--border-soft); background: var(--surface-muted); }
.workspace-stat strong { display: block; font-size: 1.45rem; color: var(--text-primary); }
.workspace-stat span { color: var(--text-secondary); font-size: 0.86rem; }
.action-strip { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 18px; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
.form-grid.stacked { margin-top: 18px; }
.form-grid.single { grid-template-columns: 1fr; }
.form-grid label { display: flex; flex-direction: column; gap: 7px; color: var(--text-secondary); font-size: 0.82rem; font-weight: 700; }
.form-grid input, .form-grid select, .form-grid textarea { width: 100%; min-height: 42px; padding: 0 12px; border-radius: 12px; border: 1px solid var(--border-soft); background: var(--surface); color: var(--text-primary); }
.form-grid textarea { padding: 8px 12px; min-height: 60px; }
.form-grid button { align-self: end; }
.inline-message { margin: 12px 0 0; color: var(--text-secondary); }
.finding-list { display: grid; gap: 8px; }
.finding-row, .asset-map-summary { display: flex; align-items: center; justify-content: space-between; gap: 10px; min-height: 38px; padding: 8px 0; border-top: 1px solid var(--border-soft); }
.finding-row:first-child { border-top: none; }
.finding-row strong { min-width: 0; color: var(--text-primary); font-size: 0.88rem; overflow-wrap: anywhere; }
.finding-row span, .asset-map-summary span { flex: 0 0 auto; color: var(--text-secondary); font-size: 0.82rem; font-weight: 700; }
.finding-row span.fail { color: #d44747; }
.finding-row span.warn { color: #b7791f; }
.asset-map-summary { margin-top: 14px; justify-content: flex-start; flex-wrap: wrap; }
.audit-panel { width: 100%; }
.activity-list { display: grid; gap: 10px; }
.activity-row { display: grid; grid-template-columns: 90px 190px minmax(0, 1fr); gap: 12px; align-items: center; padding: 12px 0; border-top: 1px solid var(--border-soft); }
.activity-row:first-child { border-top: none; }
.activity-row span { color: var(--text-muted); font-size: 0.82rem; }
.activity-row strong { color: var(--text-primary); }
.activity-row p { margin: 0; color: var(--text-secondary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.soc-intel-summary { margin-top: 14px; padding: 12px; border-radius: 12px; background: var(--surface-muted); border: 1px solid var(--border-soft); display: flex; flex-direction: column; gap: 8px; }
.soc-intel-summary p { margin: 0; font-size: 0.85rem; color: var(--text-secondary); }
.soc-stats-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; margin-top: 14px; }
.soc-stat { padding: 14px; border-radius: 12px; background: var(--surface-muted); border: 1px solid var(--border-soft); }
.soc-stat strong { display: block; font-size: 1.5rem; color: var(--text-primary); }
.soc-stat span { font-size: 0.82rem; color: var(--text-secondary); }
.soc-change { font-size: 0.8rem; font-weight: 700; }
.soc-change.up { color: #d44747; }
.soc-change.down { color: #4db480; }
.text-danger { color: #d44747; }
.soc-severity-breakdown { margin-top: 16px; }
.soc-severity-breakdown h4 { margin: 0 0 8px; font-size: 0.82rem; color: var(--text-secondary); text-transform: uppercase; letter-spacing: 0.04em; }
.severity-bar-row { display: grid; grid-template-columns: 64px minmax(0, 1fr) 32px; align-items: center; gap: 8px; margin-bottom: 6px; font-size: 0.82rem; color: var(--text-secondary); }
.severity-bar-track { height: 18px; border-radius: 999px; background: var(--surface); overflow: hidden; }
.severity-bar-fill { height: 100%; border-radius: 999px; transition: width 0.6s ease; }
.severity-bar-fill.sev-critical { background: #d44747; }
.severity-bar-fill.sev-high { background: #d49b47; }
.severity-bar-fill.sev-medium { background: #47a3d4; }
.severity-bar-fill.sev-low { background: #74b47a; }
.anomaly-list { display: grid; gap: 8px; margin-top: 10px; }
.anomaly-item { padding: 12px; border-radius: 10px; border: 1px solid var(--border-soft); background: var(--surface-muted); }
.anomaly-item.sev-critical { border-left: 3px solid #d44747; }
.anomaly-item.sev-high { border-left: 3px solid #d49b47; }
.anomaly-item strong { display: block; font-size: 0.85rem; color: var(--text-primary); text-transform: capitalize; margin-bottom: 4px; }
.anomaly-item p { margin: 0 0 6px; font-size: 0.82rem; color: var(--text-secondary); }
.anomaly-metric { display: flex; gap: 10px; font-size: 0.78rem; color: var(--text-muted); }
.incident-brief-form { display: flex; gap: 8px; align-items: center; margin-top: 14px; }
.incident-brief-form input, .filter-input { width: 100%; min-height: 42px; padding: 0 12px; border-radius: 12px; border: 1px solid var(--border-soft); background: var(--surface); color: var(--text-primary); }
.filter-select { min-height: 42px; padding: 0 12px; border-radius: 12px; border: 1px solid var(--border-soft); background: var(--surface); color: var(--text-primary); }
.ai-brief-result { margin-top: 14px; padding: 14px; border-radius: 14px; background: var(--surface-muted); border: 1px solid var(--border-soft); display: flex; flex-direction: column; gap: 10px; }
.ai-brief-result .brief-summary { margin: 0; font-size: 0.85rem; color: var(--text-secondary); line-height: 1.6; }
.rec-header { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.rec-header small { color: var(--text-muted); font-size: 0.74rem; }
.rec-reasoning { padding: 10px; border-radius: 10px; background: rgba(24, 49, 77, 0.04); }
.rec-reasoning strong { display: block; font-size: 0.78rem; color: var(--text-secondary); margin-bottom: 6px; text-transform: uppercase; letter-spacing: 0.04em; }
.rec-reasoning p { margin: 0; font-size: 0.82rem; color: var(--text-secondary); white-space: pre-wrap; }
.rec-actions { display: flex; gap: 8px; }
.success-card { padding: 10px; border-radius: 8px; background: rgba(77, 180, 128, 0.12); color: #16a34a; font-size: 0.85rem; }
.empty-card { padding: 40px; text-align: center; color: var(--text-muted); border: 1px dashed var(--border-soft); border-radius: 12px; }
@media (max-width: 900px) {
    .readiness-band { grid-template-columns: 1fr; }
    .readiness-score { width: 92px; height: 92px; }
    .workspace-summary, .form-grid, .activity-row { grid-template-columns: 1fr; }
}
</style>
