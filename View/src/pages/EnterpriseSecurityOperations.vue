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

        <section v-if="selectedWorkspace === 'soc'" class="ops-grid two">
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

                <div class="panel-head compact" style="margin-top:18px">
                    <div>
                        <span class="panel-kicker">AI Copilot</span>
                        <h3 class="panel-title">Incident Analysis</h3>
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

            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI Phan tich</span>
                        <h2 class="panel-title">Bat thuong & Canh bao</h2>
                    </div>
                </div>
                <div v-if="socIntel.anomalies && socIntel.anomalies.length" class="anomaly-list">
                    <div v-for="(anomaly, idx) in socIntel.anomalies" :key="idx" class="anomaly-item" :class="'sev-' + anomaly.severity.toLowerCase()">
                        <strong>{{ anomaly.type }}</strong>
                        <p>{{ anomaly.detail }}</p>
                        <div v-if="anomaly.currentCount != null" class="anomaly-metric">
                            <span>{{ anomaly.currentCount }} hien tai</span>
                            <span v-if="anomaly.expectedCount">| {{ anomaly.expectedCount }} TB</span>
                            <span v-if="anomaly.deviation">| +{{ anomaly.deviation }}%</span>
                        </div>
                    </div>
                </div>
                <div v-else class="empty-card">Khong phat hien bat thuong nao.</div>

                <div class="panel-head compact" style="margin-top:18px">
                    <div>
                        <span class="panel-kicker">Event Feed</span>
                        <h3 class="panel-title">AI Event Metadata</h3>
                    </div>
                </div>
                <div class="incident-brief-form" style="flex-wrap:wrap">
                    <select v-model="eventFeed.filter.sourceType" class="filter-select" style="flex:1;min-width:80px">
                        <option value="">All sources</option>
                        <option value="FaceRecognition">Face</option>
                        <option value="LicensePlate">Plate</option>
                        <option value="CameraAnalytics">Camera</option>
                        <option value="AccessControl">Access</option>
                        <option value="DeviceHealth">Device</option>
                    </select>
                    <input v-model.number="eventFeed.filter.limit" type="number" min="5" max="100" placeholder="Limit" class="filter-input" style="width:60px" />
                    <button class="btn btn-primary btn-sm" :disabled="eventFeed.loading" @click="searchEventFeed">
                        {{ eventFeed.loading ? 'Dang tai...' : 'Tim su kien' }}
                    </button>
                </div>
                <div v-if="eventFeed.events.length" class="event-feed-list">
                    <div v-for="evt in eventFeed.events.slice(0, 8)" :key="evt.id" class="event-feed-item">
                        <div class="event-feed-head">
                            <span class="soft-chip" style="font-size:0.68rem">{{ evt.eventType }}</span>
                            <small>{{ evt.sourceType }}:{{ evt.sourceId }}</small>
                        </div>
                        <p v-if="evt.label" class="small-meta">{{ evt.label }}</p>
                        <div class="event-feed-meta">
                            <span v-if="evt.confidence">Conf: {{ evt.confidence }}</span>
                            <span v-if="evt.cameraId">Cam {{ evt.cameraId }}</span>
                            <span v-if="evt.gateId">Gate {{ evt.gateId }}</span>
                            <small v-if="evt.occurredAtUtc">{{ formatDate(evt.occurredAtUtc) }}</small>
                        </div>
                    </div>
                </div>
                <div v-else-if="!eventFeed.loading" class="empty-card">Chua co su kien. Nhap filter va tim kiem.</div>

                <div class="panel-head compact" style="margin-top:18px">
                    <div>
                        <span class="panel-kicker">Device Health AI</span>
                        <h3 class="panel-title">Thiet bi insight</h3>
                    </div>
                </div>
                <div v-if="deviceInsights.loading" class="empty-card">Dang tai...</div>
                <div v-else-if="deviceInsights.items.length" class="device-insight-list">
                    <div v-for="di in deviceInsights.items.slice(0, 5)" :key="di.deviceId" class="device-insight-item" :class="'pred-' + (di.predictedStatus || '').toLowerCase()">
                        <strong>{{ di.deviceName }}</strong>
                        <span class="soft-chip" :class="di.predictedStatus === 'Online' ? 'success' : di.predictedStatus === 'AtRisk' ? 'warning' : 'danger'">
                            {{ di.predictedStatus }}
                        </span>
                        <p v-if="di.insight" class="small-meta">{{ di.insight }}</p>
                    </div>
                </div>
                <div v-else class="empty-card">Khong co thiet bi.</div>

                <div class="panel-head compact" style="margin-top:18px">
                    <div>
                        <span class="panel-kicker">AI Truy van</span>
                        <h3 class="panel-title">Natural Language Query</h3>
                    </div>
                </div>
                <div class="nl-query-form">
                    <textarea v-model="nlQuery.queryText" rows="2" class="filter-input" placeholder="Vi du: Ai vao cong sau 22h trong 7 ngay qua? Camera nao dang stale? Co bao nhieu alarm critical chua xu ly?"></textarea>
                    <button class="btn btn-primary btn-sm" :disabled="nlQuery.loading || !nlQuery.queryText.trim()" @click="executeNlQuery" style="align-self:flex-start">
                        {{ nlQuery.loading ? 'Dang truy van...' : 'Truy van' }}
                    </button>
                </div>

                <div v-if="nlQuery.result" class="nl-query-result">
                    <div class="nl-query-intent">
                        <span class="soft-chip">{{ nlQuery.result.intent }}</span>
                        <small v-if="nlQuery.result.totalCount != null">{{ nlQuery.result.totalCount }} ket qua</small>
                        <span v-if="nlQuery.result.isActionable" class="soft-chip warning">Can phe duyet</span>
                    </div>
                    <p class="brief-summary">{{ nlQuery.result.summary }}</p>

                    <div v-if="nlQuery.result.draftRecommendation" class="rec-reasoning">
                        <strong>De xuat:</strong>
                        <p>{{ nlQuery.result.draftRecommendation }}</p>
                    </div>

                    <div v-if="nlQuery.result.warnings && nlQuery.result.warnings.length" class="nl-query-warnings">
                        <div v-for="(w, i) in nlQuery.result.warnings" :key="i" class="warning-item">
                            <small>⚠ {{ w }}</small>
                        </div>
                    </div>

                    <div v-if="nlQuery.result.results && nlQuery.result.results.length" class="nl-result-list">
                        <div v-for="(row, i) in nlQuery.result.results.slice(0, 10)" :key="i" class="nl-result-row" :class="'sev-' + (row.severity || 'low').toLowerCase()">
                            <div class="nl-result-head">
                                <span class="soft-chip" style="font-size:0.64rem;min-height:20px">{{ row.source }}</span>
                                <strong>{{ row.label }}</strong>
                                <span v-if="row.severity" class="nl-severity-badge" :class="row.severity.toLowerCase()">{{ row.severity }}</span>
                            </div>
                            <p class="small-meta">{{ row.detail }}</p>
                            <div class="nl-result-meta">
                                <small v-if="row.timestamp">{{ formatDate(row.timestamp) }}</small>
                                <small v-if="row.link" class="nl-link">{{ row.link }}</small>
                            </div>
                        </div>
                    </div>
                    <div v-else-if="!nlQuery.result.draftRecommendation" class="empty-card">
                        Khong tim thay ket qua phu hop.
                    </div>
                </div>
            </article>
        </section>

        <section v-if="selectedWorkspace === 'reception'" class="ops-grid two">
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

        <section v-if="selectedWorkspace === 'auditor'" class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">AI Phan tich</span>
                        <h2 class="panel-title">Retention & Legal Hold Review</h2>
                    </div>
                </div>
                <div class="empty-card">Chuc nang phan tich retention va legal hold se duoc bo sung. Su dung AI de kiem tra rui ro xoa bang chung va canh bao legal hold sap het han.</div>
            </article>

            <article class="ops-panel">
                <div class="panel-head compact">
                    <div>
                        <span class="panel-kicker">Quick actions</span>
                        <h2 class="panel-title">Phan tich gan day</h2>
                    </div>
                </div>
                <div class="empty-card">Cac ket qua phan tich AI bang chung se hien o day.</div>
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

const incidentBriefing = reactive({
    incidentId: null,
    loading: false,
    result: null,
})

const evidenceAnalysis = reactive({
    evidenceId: null,
    loading: false,
    result: null,
})

const evidenceExport = reactive({
    exportId: null,
    loading: false,
    result: null,
})

const deviceInsights = reactive({
    loading: false,
    items: [],
})

const visitorScreening = reactive({
    visitId: null,
    loading: false,
    result: null,
})

const vehicleScreening = reactive({
    vehicleId: null,
    loading: false,
    result: null,
})

const policySimulation = reactive({
    policyId: null,
    loading: false,
    result: null,
})

const eventFeed = reactive({
    events: [],
    loading: false,
    filter: {
        sourceType: '',
        eventType: '',
        cameraId: null,
        subjectId: '',
        limit: 20,
    },
})

const nlQuery = reactive({
    queryText: '',
    loading: false,
    result: null,
})
const assetMap = reactive({
    gates: [],
    cameras: [],
    vehicles: [],
})

const busy = reactive({
    stepUp: false,
    provider: false,
    importUser: false,
    device: false,
    fault: false,
    alarm: false,
    backup: false,
    qa: false,
    backfill: false,
    policy: false,
    ai: false,
})

const stepUp = reactive({
    action: 'AllPrivilegedActions',
    password: '',
    mfaCode: '',
    sessionId: null,
    active: false,
    message: '',
})

const providerForm = reactive({
    name: 'Corporate IdP',
    protocol: 'OIDC',
    authority: 'https://idp.company.local',
    clientId: 'v-shield',
    isEnabled: true,
})

const importForm = reactive({
    providerId: 1,
    externalSubject: 'employee-001',
    username: 'employee.001',
    displayName: 'Employee 001',
    email: 'employee.001@company.local',
    phone: '',
    role: 'Staff',
    lifecycleStatus: 'Active',
    primarySiteId: null,
})

const deviceForm = reactive({
    name: 'Virtual Controller 01',
    protocol: 'OSDP-Sim',
    direction: 'Entry',
    maxCredentials: 50000,
})

const faultForm = reactive({
    securityDeviceId: null,
    status: 'Tamper',
    severity: 'High',
    message: 'Operator drill',
})

const alarmForm = reactive({
    summary: 'Manual SOC drill alarm',
    severity: 'High',
})

const backupForm = reactive({
    profile: 'MediumCompany',
})

const qaForm = reactive({
    testType: 'LoadStressSoakChaos',
})

const backfillForm = reactive({
    companyName: 'V-Shield Company',
    companyCode: 'VSHIELD',
    siteName: 'Headquarters',
    siteCode: 'HQ',
    timeZoneId: 'Asia/Ho_Chi_Minh',
})

const policyForm = reactive({
    subjectType: 'Employee',
    subjectId: 1,
    siteId: null,
    securityZoneId: null,
    accessPointId: null,
    credentialType: 'QR',
    allowHolidayAccess: false,
    evaluatedAtUtc: null,
})

const policyResult = reactive({
    result: '',
    reason: '',
    decisionMode: '',
})

const statusMessage = computed(() => {
    if (loadError.value) return loadError.value
    if (loading.value) return 'Refreshing enterprise security data.'
    return 'Operational views are connected to enterprise APIs and local acceptance gates.'
})

const headlineMetrics = computed(() => [
    {
        label: 'Sites',
        value: overview.foundation.sites || 0,
        note: `${overview.foundation.accessPoints || 0} access points`,
    },
    {
        label: 'Open alarms',
        value: overview.soc.openAlarms || 0,
        note: `${overview.soc.criticalOpenAlarms || 0} critical`,
    },
    {
        label: 'Devices',
        value: overview.devices.devices || 0,
        note: `${overview.devices.offlinePackages || 0} offline packages`,
    },
    {
        label: 'Evidence',
        value: overview.evidence.evidenceItems || 0,
        note: `${overview.evidence.pendingExports || 0} pending exports`,
    },
    {
        label: 'Outbox',
        value: overview.operations.pendingOutboxEvents || 0,
        note: `${overview.operations.failedOutboxEvents || 0} failed`,
    },
    {
        label: 'Release gates',
        value: overview.release.pendingRequiredGates || 0,
        note: `${overview.release.approvedReleaseCandidates || 0} approved releases`,
    },
])

const workspaces = computed(() => [
    {
        id: 'admin',
        label: 'Admin',
        kicker: 'Administration',
        title: 'Foundation and identity',
        badge: `${overview.identity.activeMappings || 0} active mappings`,
        metrics: [
            { label: 'Companies', value: overview.foundation.companies || 0 },
            { label: 'Identity providers', value: overview.identity.enabledProviders || 0 },
            { label: 'Terminated users', value: overview.identity.terminatedEmployees || 0 },
        ],
        actions: ['Provider', 'HR import', 'Recertification'],
    },
    {
        id: 'soc',
        label: 'SOC',
        kicker: 'Command center',
        title: 'Alarms and incidents',
        badge: `${overview.soc.openIncidents || 0} open incidents`,
        metrics: [
            { label: 'Open alarms', value: overview.soc.openAlarms || 0 },
            { label: 'Active SOPs', value: overview.soc.activeSops || 0 },
            { label: 'Dispatch tasks', value: overview.soc.openDispatchTasks || 0 },
        ],
        actions: ['Acknowledge', 'Dispatch', 'Handover'],
    },
    {
        id: 'reception',
        label: 'Reception',
        kicker: 'Visitor desk',
        title: 'Visits and watchlists',
        badge: `${overview.visitorVehicle.watchlistMatches || 0} matches`,
        metrics: [
            { label: 'Visits', value: overview.visitorVehicle.visits || 0 },
            { label: 'Credentials', value: overview.visitorVehicle.visitorCredentials || 0 },
            { label: 'Watchlist entries', value: overview.visitorVehicle.watchlistEntries || 0 },
        ],
        actions: ['Check-in', 'Forms', 'Overstay'],
    },
    {
        id: 'gate',
        label: 'Gate',
        kicker: 'Vehicle lanes',
        title: 'Parking and barrier review',
        badge: `${overview.visitorVehicle.barriers || 0} barriers`,
        metrics: [
            { label: 'Parking permits', value: overview.visitorVehicle.parkingPermits || 0 },
            { label: 'Lane events', value: overview.visitorVehicle.laneEvents || 0 },
            { label: 'Barrier commands', value: overview.visitorVehicle.barrierCommands || 0 },
        ],
        actions: ['Plate review', 'Open barrier', 'Exception'],
    },
    {
        id: 'auditor',
        label: 'Auditor',
        kicker: 'Governance',
        title: 'Evidence and compliance',
        badge: `${overview.evidence.activeLegalHolds || 0} legal holds`,
        metrics: [
            { label: 'Collections', value: overview.evidence.collections || 0 },
            { label: 'Access logs', value: overview.evidence.accessLogs || 0 },
            { label: 'Reports', value: overview.evidence.complianceReports || 0 },
        ],
        actions: ['Export review', 'Retention', 'Report'],
    },
    {
        id: 'ops',
        label: 'Ops',
        kicker: 'Resilience',
        title: 'Workers, backup and release',
        badge: `${overview.operations.degradedDependencies || 0} degraded`,
        metrics: [
            { label: 'Backups', value: overview.operations.backupRuns || 0 },
            { label: 'Restore drills', value: overview.operations.restoreDrills || 0 },
            { label: 'QA runs', value: overview.release.qaTestRuns || 0 },
        ],
        actions: ['Outbox', 'SIEM', 'Backup'],
    },
])

const activeWorkspace = computed(() =>
    workspaces.value.find((workspace) => workspace.id === selectedWorkspace.value) || workspaces.value[0]
)

const visibleFindings = computed(() =>
    (configHealth.findings || [])
        .filter((finding) => finding.status !== 'Pass')
        .slice(0, 5)
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
        const [
            foundation,
            identity,
            policy,
            visitorVehicle,
            devices,
            soc,
            evidence,
            operations,
            release,
        ] = await enterpriseApi.overview()

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
    } catch {
    }
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
        incidentBriefing.result = {
            severity: 'Medium',
            summary: 'Khong the phan tich: ' + (error.response?.data?.message || error.message),
            provider: 'Error',
            recommendationId: null,
        }
    } finally {
        incidentBriefing.loading = false
    }
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
        evidenceAnalysis.result = {
            severity: 'Medium',
            summary: 'Khong the phan tich: ' + (error.response?.data?.message || error.message),
            provider: 'Error',
            recommendationId: null,
        }
    } finally {
        evidenceAnalysis.loading = false
    }
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
        evidenceExport.result = {
            severity: 'Medium',
            summary: 'Khong the kiem tra: ' + (error.response?.data?.message || error.message),
            provider: 'Error',
            recommendationId: null,
        }
    } finally {
        evidenceExport.loading = false
    }
}

async function approveAi(id) {
    if (!id) return
    try {
        await enterpriseAiApi.reviewRecommendation(id, 'Approved', 'Phe duyet sau khi xem xet')
        pushActivity('AI Recommendation', `Approved #${id}`)
    } catch { /* ignore */ }
}

async function rejectAi(id) {
    if (!id) return
    try {
        await enterpriseAiApi.reviewRecommendation(id, 'Rejected', 'Khong dong y')
        pushActivity('AI Recommendation', `Rejected #${id}`)
    } catch { /* ignore */ }
}

async function loadDeviceInsights() {
    deviceInsights.loading = true
    try {
        const { data } = await enterpriseAiApi.getDeviceHealthInsights()
        deviceInsights.items = Array.isArray(data) ? data : []
    } catch {
        deviceInsights.items = []
    } finally {
        deviceInsights.loading = false
    }
}

async function screenVisitor() {
    if (!visitorScreening.visitId) return
    visitorScreening.loading = true
    visitorScreening.result = null
    try {
        const { data } = await enterpriseAiApi.screenVisitor(visitorScreening.visitId)
        visitorScreening.result = data
        pushActivity('AI Visitor Screening', `Visit #${visitorScreening.visitId} screened`)
    } catch (error) {
        visitorScreening.result = {
            severity: 'Medium',
            summary: 'Khong the phan tich: ' + (error.response?.data?.message || error.message),
            provider: 'Error',
            recommendationId: null,
        }
    } finally {
        visitorScreening.loading = false
    }
}

async function screenVehicle() {
    if (!vehicleScreening.vehicleId) return
    vehicleScreening.loading = true
    vehicleScreening.result = null
    try {
        const { data } = await enterpriseAiApi.screenVehicle(vehicleScreening.vehicleId)
        vehicleScreening.result = data
        pushActivity('AI Vehicle Screening', `Vehicle #${vehicleScreening.vehicleId} screened`)
    } catch (error) {
        vehicleScreening.result = {
            severity: 'Medium',
            summary: 'Khong the phan tich: ' + (error.response?.data?.message || error.message),
            provider: 'Error',
            recommendationId: null,
        }
    } finally {
        vehicleScreening.loading = false
    }
}

async function simulateAiPolicy() {
    if (!policySimulation.policyId) return
    policySimulation.loading = true
    policySimulation.result = null
    try {
        const { data } = await enterpriseAiApi.simulatePolicy(policySimulation.policyId)
        policySimulation.result = data
        pushActivity('AI Policy Simulate', `Policy #${policySimulation.policyId} simulated`)
    } catch (error) {
        policySimulation.result = {
            severity: 'Low',
            summary: 'Khong the mo phong: ' + (error.response?.data?.message || error.message),
            provider: 'Error',
            recommendationId: null,
        }
    } finally {
        policySimulation.loading = false
    }
}

async function explainAiPolicy() {
    if (!policySimulation.policyId) return
    policySimulation.loading = true
    policySimulation.result = null
    try {
        const { data } = await enterpriseAiApi.explainPolicy(policySimulation.policyId)
        policySimulation.result = data
        pushActivity('AI Policy Explain', `Policy #${policySimulation.policyId} explained`)
    } catch (error) {
        policySimulation.result = {
            severity: 'Low',
            summary: 'Khong the giai thich: ' + (error.response?.data?.message || error.message),
            provider: 'Error',
            recommendationId: null,
        }
    } finally {
        policySimulation.loading = false
    }
}

async function executeNlQuery() {
    if (!nlQuery.queryText.trim()) return
    nlQuery.loading = true
    nlQuery.result = null
    try {
        const { data } = await enterpriseAiApi.naturalLanguageQuery(nlQuery.queryText)
        nlQuery.result = data
        pushActivity('NL Query', data.intent + ': ' + data.totalCount + ' results')
    } catch (error) {
        nlQuery.result = {
            intent: 'error',
            summary: 'Loi truy van: ' + (error.response?.data?.message || error.message),
            results: [],
            totalCount: 0,
            isActionable: false,
        }
    } finally {
        nlQuery.loading = false
    }
}

async function searchEventFeed() {
    eventFeed.loading = true
    try {
        const { data } = await enterpriseAiApi.searchEventMetadata(eventFeed.filter)
        eventFeed.events = Array.isArray(data) ? data : []
        pushActivity('Event Feed', `Found ${eventFeed.events.length} events`)
    } catch {
        eventFeed.events = []
    } finally {
        eventFeed.loading = false
    }
}

watch(selectedWorkspace, (ws) => {
    if (ws === 'soc') {
        loadSocIntel()
        loadDeviceInsights()
    }
})

async function verifyStepUp() {
    busy.stepUp = true
    stepUp.message = ''
    try {
        const start = await enterpriseApi.stepUpStart(stepUp.action, 'Operator console verification')
        const verified = await enterpriseApi.stepUpVerify(start.data.sessionId, stepUp.password, stepUp.mfaCode)
        stepUp.sessionId = verified.data.sessionId
        stepUp.active = verified.data.active
        enterpriseApi.setStepUpSession(verified.data.sessionId)
        stepUp.message = 'Verified until ' + formatDateTime(verified.data.expiresAtUtc)
        pushActivity('Step-up verified', stepUp.action)
    } catch (error) {
        stepUp.active = false
        stepUp.message = error.response?.data?.message || 'Verification failed.'
    } finally {
        busy.stepUp = false
    }
}

async function saveProvider() {
    await runAction('provider', 'Provider saved', () => enterpriseApi.upsertIdentityProvider(providerForm))
}

async function importUser() {
    const user = { ...importForm }
    const providerId = user.providerId
    delete user.providerId
    await runAction('importUser', 'User import recorded', () => enterpriseApi.importIdentityUsers(providerId, [user]))
}

async function createVirtualController() {
    await runAction('device', 'Virtual controller created', () => enterpriseApi.createVirtualController(deviceForm))
}

async function injectFault() {
    await runAction('fault', 'Simulator fault injected', () => enterpriseApi.injectSimulatorFault(faultForm))
}

async function createAlarm() {
    await runAction('alarm', 'Alarm created', () =>
        enterpriseApi.createAlarm({
            alarmType: 'ManualDrill',
            severity: alarmForm.severity,
            summary: alarmForm.summary,
        })
    )
}

async function startBackup() {
    await runAction('backup', 'Backup run started', () =>
        enterpriseApi.startBackup({
            profile: backupForm.profile,
            targetRpoMinutes: 15,
            targetRtoMinutes: 60,
            notes: 'Started from enterprise console',
        })
    )
}

async function createQaRun() {
    await runAction('qa', 'QA run recorded', () =>
        enterpriseApi.createQaRun({
            testType: qaForm.testType,
            profile: 'MediumCompany',
            evidenceReference: '/qa/local-enterprise-console',
            notes: 'Recorded from enterprise console',
        })
    )
}

async function backfillDefaultSite() {
    await runAction('backfill', 'Foundation backfill completed', () => enterpriseApi.backfillDefaultSite(backfillForm))
}

async function simulatePolicy() {
    await runAction('policy', 'Policy simulation completed', async () => {
        const response = await enterpriseApi.simulateAccessPolicy({
            ...policyForm,
            evaluatedAtUtc: policyForm.evaluatedAtUtc || new Date().toISOString(),
        })
        policyResult.result = response.data?.result || ''
        policyResult.reason = response.data?.reason || ''
        policyResult.decisionMode = response.data?.decisionMode || ''
        return response
    })
}

async function runAction(key, title, action) {
    busy[key] = true
    try {
        const response = await action()
        pushActivity(title, response.data?.message || JSON.stringify(response.data).slice(0, 140))
        await loadOverview()
    } catch (error) {
        pushActivity(title + ' failed', error.response?.data?.message || error.message)
    } finally {
        busy[key] = false
    }
}

function pushActivity(title, detail) {
    activityLog.value.unshift({
        id: `${Date.now()}-${Math.random()}`,
        title,
        detail,
        time: new Date().toLocaleTimeString(),
    })
    activityLog.value = activityLog.value.slice(0, 8)
}

function normalizeKeys(data) {
    return Object.fromEntries(
        Object.entries(data || {}).map(([key, value]) => [
            key.charAt(0).toLowerCase() + key.slice(1),
            value,
        ])
    )
}

function formatDateTime(value) {
    if (!value) return ''
    return new Date(value).toLocaleString()
}

function formatDate(value) {
    if (!value) return ''
    return new Date(value).toLocaleString('vi-VN', {
        hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit',
    })
}

onMounted(loadOverview)
</script>

<style scoped>
.enterprise-page {
    display: flex;
    flex-direction: column;
    gap: 22px;
}

.readiness-band {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    align-items: center;
    gap: 24px;
    padding: 22px;
    border-radius: 18px;
    border: 1px solid var(--border-soft);
    background: linear-gradient(135deg, rgba(18, 75, 91, 0.92), rgba(18, 36, 52, 0.96));
    color: #f7fcff;
    box-shadow: var(--shadow-card);
}

.readiness-score {
    width: 112px;
    height: 112px;
    border-radius: 999px;
    display: grid;
    place-content: center;
    text-align: center;
    border: 1px solid rgba(255, 255, 255, 0.24);
    background: rgba(255, 255, 255, 0.08);
}

.readiness-score span {
    font-size: 0.74rem;
    text-transform: uppercase;
    color: rgba(247, 252, 255, 0.72);
}

.readiness-score strong {
    font-size: 2rem;
    line-height: 1;
}

.readiness-copy h2 {
    margin: 0 0 8px;
    font-size: 1.35rem;
}

.readiness-copy p {
    margin: 0;
    color: rgba(247, 252, 255, 0.76);
}

.status-pill {
    display: inline-flex;
    align-items: center;
    min-height: 36px;
    padding: 0 14px;
    border-radius: 999px;
    background: rgba(77, 216, 180, 0.16);
    color: #bbffe8;
    font-weight: 700;
}

.status-pill.danger {
    background: rgba(236, 91, 91, 0.18);
    color: #ffd0d0;
}

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
}

.workspace-tabs button.active {
    color: #05313b;
    background: #8ceaf4;
    border-color: #8ceaf4;
}

.workspace-summary {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 12px;
}

.workspace-stat {
    min-height: 86px;
    padding: 14px;
    border-radius: 14px;
    border: 1px solid var(--border-soft);
    background: var(--surface-muted);
}

.workspace-stat strong {
    display: block;
    font-size: 1.45rem;
    color: var(--text-primary);
}

.workspace-stat span {
    color: var(--text-secondary);
    font-size: 0.86rem;
}

.action-strip {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    margin-top: 18px;
}

.form-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 12px;
}

.form-grid.stacked {
    margin-top: 18px;
}

.form-grid.single {
    grid-template-columns: 1fr;
}

.form-grid label {
    display: flex;
    flex-direction: column;
    gap: 7px;
    color: var(--text-secondary);
    font-size: 0.82rem;
    font-weight: 700;
}

.form-grid input,
.form-grid select {
    width: 100%;
    min-height: 42px;
    padding: 0 12px;
    border-radius: 12px;
    border: 1px solid var(--border-soft);
    background: var(--surface);
    color: var(--text-primary);
}

.form-grid button {
    align-self: end;
}

.inline-message {
    margin: 12px 0 0;
    color: var(--text-secondary);
}

.finding-list {
    display: grid;
    gap: 8px;
}

.finding-row,
.asset-map-summary {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
    min-height: 38px;
    padding: 8px 0;
    border-top: 1px solid var(--border-soft);
}

.finding-row:first-child {
    border-top: none;
}

.finding-row strong {
    min-width: 0;
    color: var(--text-primary);
    font-size: 0.88rem;
    overflow-wrap: anywhere;
}

.finding-row span,
.asset-map-summary span {
    flex: 0 0 auto;
    color: var(--text-secondary);
    font-size: 0.82rem;
    font-weight: 700;
}

.finding-row span.fail {
    color: #d44747;
}

.finding-row span.warn {
    color: #b7791f;
}

.asset-map-summary {
    margin-top: 14px;
    justify-content: flex-start;
    flex-wrap: wrap;
}

.audit-panel {
    width: 100%;
}

.activity-list {
    display: grid;
    gap: 10px;
}

.activity-row {
    display: grid;
    grid-template-columns: 90px 190px minmax(0, 1fr);
    gap: 12px;
    align-items: center;
    padding: 12px 0;
    border-top: 1px solid var(--border-soft);
}

.activity-row:first-child {
    border-top: none;
}

.activity-row span {
    color: var(--text-muted);
    font-size: 0.82rem;
}

.activity-row strong {
    color: var(--text-primary);
}

.activity-row p {
    margin: 0;
    color: var(--text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.soc-intel-summary {
    margin-top: 14px;
    padding: 12px;
    border-radius: 12px;
    background: var(--surface-muted);
    border: 1px solid var(--border-soft);
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.soc-intel-summary p {
    margin: 0;
    font-size: 0.85rem;
    color: var(--text-secondary);
}

.soc-stats-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px;
    margin-top: 14px;
}

.soc-stat {
    padding: 14px;
    border-radius: 12px;
    background: var(--surface-muted);
    border: 1px solid var(--border-soft);
}

.soc-stat strong {
    display: block;
    font-size: 1.5rem;
    color: var(--text-primary);
}

.soc-stat span {
    font-size: 0.82rem;
    color: var(--text-secondary);
}

.soc-change {
    font-size: 0.8rem;
    font-weight: 700;
}

.soc-change.up {
    color: #d44747;
}

.soc-change.down {
    color: #4db480;
}

.text-danger {
    color: #d44747;
}

.soc-severity-breakdown {
    margin-top: 16px;
}

.soc-severity-breakdown h4 {
    margin: 0 0 8px;
    font-size: 0.82rem;
    color: var(--text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.severity-bar-row {
    display: grid;
    grid-template-columns: 64px minmax(0, 1fr) 32px;
    align-items: center;
    gap: 8px;
    margin-bottom: 6px;
    font-size: 0.82rem;
    color: var(--text-secondary);
}

.severity-bar-track {
    height: 18px;
    border-radius: 999px;
    background: var(--surface);
    overflow: hidden;
}

.severity-bar-fill {
    height: 100%;
    border-radius: 999px;
    transition: width 0.6s ease;
}

.severity-bar-fill.sev-critical { background: #d44747; }
.severity-bar-fill.sev-high { background: #d49b47; }
.severity-bar-fill.sev-medium { background: #47a3d4; }
.severity-bar-fill.sev-low { background: #74b47a; }

.anomaly-list {
    display: grid;
    gap: 8px;
    margin-top: 10px;
}

.anomaly-item {
    padding: 12px;
    border-radius: 10px;
    border: 1px solid var(--border-soft);
    background: var(--surface-muted);
}

.anomaly-item.sev-critical {
    border-left: 3px solid #d44747;
}

.anomaly-item.sev-high {
    border-left: 3px solid #d49b47;
}

.anomaly-item strong {
    display: block;
    font-size: 0.85rem;
    color: var(--text-primary);
    text-transform: capitalize;
    margin-bottom: 4px;
}

.anomaly-item p {
    margin: 0 0 6px;
    font-size: 0.82rem;
    color: var(--text-secondary);
}

.anomaly-metric {
    display: flex;
    gap: 10px;
    font-size: 0.78rem;
    color: var(--text-muted);
}

.soft-chip.warning {
    background: rgba(212, 155, 71, 0.16);
    color: #ffd89a;
}

.soft-chip.danger {
    background: rgba(212, 71, 71, 0.16);
    color: #ffb0b0;
}

.soft-chip.success {
    background: rgba(77, 180, 128, 0.16);
    color: #aaffd0;
}

.incident-brief-form {
    display: flex;
    gap: 8px;
    align-items: center;
    margin-top: 14px;
}

.incident-brief-form input {
    width: 100%;
    min-height: 42px;
    padding: 0 12px;
    border-radius: 12px;
    border: 1px solid var(--border-soft);
    background: var(--surface);
    color: var(--text-primary);
}

.ai-brief-result {
    margin-top: 14px;
    padding: 14px;
    border-radius: 14px;
    background: var(--surface-muted);
    border: 1px solid var(--border-soft);
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.ai-brief-result .brief-summary {
    margin: 0;
    font-size: 0.85rem;
    color: var(--text-secondary);
    line-height: 1.6;
}

.rec-header {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
}

.rec-header small {
    color: var(--text-muted);
    font-size: 0.74rem;
}

.rec-reasoning {
    padding: 10px;
    border-radius: 10px;
    background: rgba(24, 49, 77, 0.04);
}

.rec-reasoning strong {
    display: block;
    font-size: 0.78rem;
    color: var(--text-secondary);
    margin-bottom: 6px;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.rec-reasoning p {
    margin: 0;
    font-size: 0.82rem;
    color: var(--text-secondary);
    white-space: pre-wrap;
}

.rec-actions {
    display: flex;
    gap: 8px;
}

.btn-success {
    background: rgba(77, 180, 128, 0.16);
    color: #4db480;
    border: 1px solid rgba(77, 180, 128, 0.3);
}

.device-insight-list {
    display: grid;
    gap: 8px;
    margin-top: 10px;
}

.device-insight-item {
    padding: 10px 12px;
    border-radius: 10px;
    border: 1px solid var(--border-soft);
    background: var(--surface-muted);
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.device-insight-item strong {
    font-size: 0.85rem;
    color: var(--text-primary);
}

.device-insight-item .small-meta {
    margin: 0;
    font-size: 0.78rem;
    color: var(--text-muted);
}

.device-insight-item.pred-degraded {
    border-left: 3px solid #d49b47;
}

.device-insight-item.pred-stale {
    border-left: 3px solid #d44747;
}

.device-insight-item.pred-offline {
    border-left: 3px solid #d44747;
}

.device-insight-item.pred-atrisk {
    border-left: 3px solid #d49b47;
}

.device-insight-item.pred-online {
    border-left: 3px solid #4db480;
}

.risk-explanation {
    margin-top: 12px;
    padding: 14px;
    border-radius: 14px;
    background: var(--surface-muted);
    border: 1px solid var(--border-soft);
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.risk-explanation p {
    margin: 0;
    font-size: 0.85rem;
    color: var(--text-secondary);
    line-height: 1.6;
}

@media (max-width: 900px) {
    .readiness-band {
        grid-template-columns: 1fr;
    }

    .readiness-score {
        width: 92px;
        height: 92px;
    }

    .workspace-summary,
    .form-grid,
    .activity-row {
        grid-template-columns: 1fr;
    }
}
</style>
