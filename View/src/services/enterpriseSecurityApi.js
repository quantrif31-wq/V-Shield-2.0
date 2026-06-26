import http from './http'

export const socIntelApi = {
    getIntelligence() {
        return http.get('/enterprise/soc/intelligence')
    },
    classifyAlarm(alarmId) {
        return http.get(`/enterprise/soc/alarms/${alarmId}/classify`)
    },
    recommendSop(alarmId) {
        return http.get(`/enterprise/soc/alarms/${alarmId}/recommend-sop`)
    },
    predictEscalationRisk(alarmId) {
        return http.get(`/enterprise/soc/alarms/${alarmId}/escalation-risk`)
    },
    getAnomalies() {
        return http.get('/enterprise/soc/anomalies')
    },
}

export const enterpriseApi = {
    setStepUpSession(sessionId) {
        if (sessionId) {
            http.defaults.headers.common['X-Step-Up-Session-Id'] = String(sessionId)
        } else {
            delete http.defaults.headers.common['X-Step-Up-Session-Id']
        }
    },
    overview() {
        const requests = [
            http.get('/enterprise/foundation/overview'),
            http.get('/enterprise/identity/overview'),
            http.get('/enterprise/access-policy/overview'),
            http.get('/enterprise/visitor-vehicle/overview'),
            http.get('/enterprise/devices/overview'),
            http.get('/enterprise/soc/overview'),
            http.get('/enterprise/evidence/overview'),
            http.get('/enterprise/operations/overview'),
            http.get('/enterprise/release-readiness/overview'),
        ]

        return Promise.allSettled(requests).then((results) =>
            results.map((result) => (result.status === 'fulfilled' ? result.value : { data: {} }))
        )
    },
    configHealth() {
        return http.get('/enterprise/operations/config-health')
    },
    assetMap() {
        return http.get('/enterprise/foundation/asset-map')
    },
    backfillDefaultSite(payload) {
        return http.post('/enterprise/foundation/backfill/default-site', payload)
    },
    simulateAccessPolicy(payload) {
        return http.post('/enterprise/access-policy/simulate', payload)
    },
    stepUpStart(action, reason) {
        return http.post('/Auth/step-up/start', { action, reason })
    },
    stepUpVerify(sessionId, password, mfaCode) {
        return http.post('/Auth/step-up/verify', { sessionId, password, mfaCode })
    },
    upsertIdentityProvider(payload) {
        return http.post('/enterprise/identity/providers', payload)
    },
    importIdentityUsers(providerId, users) {
        return http.post('/enterprise/identity/import/users', { providerId, users })
    },
    createVirtualController(payload) {
        return http.post('/enterprise/devices/simulator/virtual-controller', payload)
    },
    injectSimulatorFault(payload) {
        return http.post('/enterprise/devices/simulator/fault', payload)
    },
    searchHierarchy(type, q) {
        return http.get('/enterprise/foundation/hierarchy/search', { params: { type, q } })
    },
    getBackfillStatus() {
        return http.get('/enterprise/foundation/backfill/status')
    },
    getHierarchy() {
        return http.get('/enterprise/foundation/hierarchy')
    },
    createCompany(payload) { return http.post('/enterprise/foundation/companies', payload) },
    createSite(payload) { return http.post('/enterprise/foundation/sites', payload) },
    createBuilding(payload) { return http.post('/enterprise/foundation/buildings', payload) },
    createFloor(payload) { return http.post('/enterprise/foundation/floors', payload) },
    createZone(payload) { return http.post('/enterprise/foundation/zones', payload) },
    createAccessPoint(payload) { return http.post('/enterprise/foundation/access-points', payload) },
    createDoor(payload) { return http.post('/enterprise/foundation/doors', payload) },
    createLane(payload) { return http.post('/enterprise/foundation/lanes', payload) },
    getPolicyOverview() { return http.get('/enterprise/access-policy/overview') },
    getPolicyVersions() { return http.get('/enterprise/access-policy/policy-versions') },
    createPolicyVersion(payload) { return http.post('/enterprise/access-policy/policy-versions', payload) },
    submitPolicyVersion(id) { return http.patch(`/enterprise/access-policy/policy-versions/${id}/submit`) },
    approvePolicyVersion(id, payload) { return http.patch(`/enterprise/access-policy/policy-versions/${id}/approve`, payload || {}) },
    activatePolicyVersion(id) { return http.patch(`/enterprise/access-policy/policy-versions/${id}/activate`) },
    retirePolicyVersion(id) { return http.patch(`/enterprise/access-policy/policy-versions/${id}/retire`) },
    getAccessRules() { return http.get('/enterprise/access-policy/rules') },
    createAccessRule(payload) { return http.post('/enterprise/access-policy/rules', payload) },
    createAccessLevel(payload) { return http.post('/enterprise/access-policy/access-levels', payload) },
    createAccessGroup(payload) { return http.post('/enterprise/access-policy/access-groups', payload) },
    createSchedule(payload) { return http.post('/enterprise/access-policy/schedules', payload) },
    evaluateAccess(payload) { return http.post('/enterprise/access-policy/evaluate', payload) },
    simulateAccess(payload) { return http.post('/enterprise/access-policy/simulate', payload) },
    shadowCompare(payload) { return http.post('/enterprise/access-policy/shadow-compare', payload) },
    createTemporaryGrant(payload) { return http.post('/enterprise/access-policy/temporary-grants', payload) },
    getEmergencyPasses(active = true) { return http.get('/enterprise/access-policy/emergency-passes', { params: { active } }) },
    createEmergencyPass(payload) { return http.post('/enterprise/access-policy/emergency-passes', payload) },
    createEmergencyState(payload) { return http.post('/enterprise/access-policy/emergency-states', payload) },
    getActiveEmergencies() { return http.get('/enterprise/access-policy/emergency-states?active=true') },
    recordOccupancy(payload) { return http.post('/enterprise/access-policy/occupancy', payload) },
    resetAntiPassback(payload) { return http.post('/enterprise/access-policy/anti-passback/reset', payload) },
    getDuressEvents(unacknowledged) { return http.get('/enterprise/access-policy/duress-events', { params: { unacknowledged } }) },
    recordDuressEvent(payload) { return http.post('/enterprise/access-policy/duress-events', payload) },
    acknowledgeDuressEvent(eventId) { return http.post(`/enterprise/access-policy/duress-events/${eventId}/acknowledge`) },
    createAlarm(payload) {
        return http.post('/enterprise/soc/alarms', payload)
    },
    startBackup(payload) {
        return http.post('/enterprise/operations/backup-runs', payload)
    },
    createQaRun(payload) {
        return http.post('/enterprise/release-readiness/qa-test-runs', payload)
    },
    // Visitor & Vehicle
    getVisits(params) { return http.get('/enterprise/visitor-vehicle/visits', { params }) },
    getVisitDetail(visitId) { return http.get(`/enterprise/visitor-vehicle/visits/${visitId}`) },
    getOverstays() { return http.get('/enterprise/visitor-vehicle/visits/overstays') },
    createVisit(payload) { return http.post('/enterprise/visitor-vehicle/visits', payload) },
    checkInVisit(visitId, payload) { return http.post(`/enterprise/visitor-vehicle/visits/${visitId}/check-in`, payload) },
    checkOutVisit(visitId) { return http.post(`/enterprise/visitor-vehicle/visits/${visitId}/check-out`) },
    issueVisitorCredential(visitId, payload) { return http.post(`/enterprise/visitor-vehicle/visits/${visitId}/credentials`, payload) },
    // Watchlist
    getWatchlistEntries(params) { return http.get('/enterprise/visitor-vehicle/watchlist-entries', { params }) },
    getWatchlistMatches(params) { return http.get('/enterprise/visitor-vehicle/watchlist-matches', { params }) },
    createWatchlistEntry(payload) { return http.post('/enterprise/visitor-vehicle/watchlist', payload) },
    reviewWatchlistMatch(matchId, payload) { return http.patch(`/enterprise/visitor-vehicle/watchlist-matches/${matchId}/review`, payload) },
    // Forms
    getFormTemplates(params) { return http.get('/enterprise/visitor-vehicle/forms', { params }) },
    createFormTemplate(payload) { return http.post('/enterprise/visitor-vehicle/forms', payload) },
    acceptForm(visitId, payload) { return http.post(`/enterprise/visitor-vehicle/visits/${visitId}/form-acceptances`, payload) },
    // Contractors
    getContractors(params) { return http.get('/enterprise/visitor-vehicle/contractors', { params }) },
    getContractorDetail(contractorId) { return http.get(`/enterprise/visitor-vehicle/contractors/${contractorId}`) },
    createContractor(payload) { return http.post('/enterprise/visitor-vehicle/contractors', payload) },
    revokeContractor(contractorId, payload) { return http.patch(`/enterprise/visitor-vehicle/contractors/${contractorId}/revoke`, payload) },
    // Parking & Barriers & Lane
    getParkingAreas(params) { return http.get('/enterprise/visitor-vehicle/parking-areas', { params }) },
    getParkingPermits(params) { return http.get('/enterprise/visitor-vehicle/parking-permits', { params }) },
    createParkingArea(payload) { return http.post('/enterprise/visitor-vehicle/parking-areas', payload) },
    createParkingPermit(payload) { return http.post('/enterprise/visitor-vehicle/parking-permits', payload) },
    getBarriers(params) { return http.get('/enterprise/visitor-vehicle/barriers', { params }) },
    createBarrier(payload) { return http.post('/enterprise/visitor-vehicle/barriers', payload) },
    recordBarrierCommand(barrierId, payload) { return http.post(`/enterprise/visitor-vehicle/barriers/${barrierId}/commands`, payload) },
    getBarrierCommands(barrierId, params) { return http.get(`/enterprise/visitor-vehicle/barriers/${barrierId}/commands`, { params }) },
    simulateBarrierCommand(barrierId, payload) { return http.post(`/enterprise/visitor-vehicle/barriers/${barrierId}/simulate`, payload) },
    getLaneEvents(params) { return http.get('/enterprise/visitor-vehicle/lane-events', { params }) },
    recordLaneEvent(payload) { return http.post('/enterprise/visitor-vehicle/lane-events', payload) },
    getLaneHealth() { return http.get('/enterprise/visitor-vehicle/lane-health') },
    // Adjudication (plate review)
    getAdjudications(params) { return http.get('/enterprise/visitor-vehicle/adjudications', { params }) },
    reviewAdjudication(adjudicationId, payload) { return http.patch(`/enterprise/visitor-vehicle/adjudications/${adjudicationId}/review`, payload) },
    // Device Topology
    getTopology() { return http.get('/enterprise/devices/topology') },
    getDevice(deviceId) { return http.get(`/enterprise/devices/${deviceId}`) },
    getDeviceReaders(deviceId) { return http.get(`/enterprise/devices/${deviceId}/readers`) },
    getDeviceRelays(deviceId) { return http.get(`/enterprise/devices/${deviceId}/relays`) },
    getDeviceSensors(deviceId) { return http.get(`/enterprise/devices/${deviceId}/sensors`) },
    getDeviceHealthHistory(deviceId, params) { return http.get(`/enterprise/devices/${deviceId}/health`, { params }) },
    getDeviceConfigurations(deviceId) { return http.get(`/enterprise/devices/${deviceId}/configuration-versions`) },
    // Provisioning
    getProvisioningRequests(params) { return http.get('/enterprise/devices/provisioning-requests', { params }) },
    createProvisioningRequest(payload) { return http.post('/enterprise/devices/provisioning-requests', payload) },
    approveProvisioningRequest(requestId, payload) { return http.patch(`/enterprise/devices/provisioning-requests/${requestId}/approve`, payload) },
    // Offline Packages
    getOfflinePolicyPackages(params) { return http.get('/enterprise/devices/offline-policy-packages', { params }) },
    createOfflinePolicyPackage(payload) { return http.post('/enterprise/devices/offline-policy-packages', payload) },
    // Device Adapters
    getAdapters() { return http.get('/enterprise/devices/connectors/adapters') },
    getConnectorStatus() { return http.get('/enterprise/devices/connectors/status') },
    // Simulator
    simulateOfflineScan(payload) { return http.post('/enterprise/devices/simulator/offline-scan', payload) },
    createDevice(payload) { return http.post('/enterprise/devices', payload) },
    registerController(deviceId, payload) { return http.post(`/enterprise/devices/${deviceId}/controllers`, payload) },
    recordHealth(deviceId, payload) { return http.post(`/enterprise/devices/${deviceId}/health`, payload) },
    getHealthInsights() { return http.get('/enterprise/devices/health-insights') },
    diagnoseDevice(deviceId) { return http.post(`/enterprise/devices/${deviceId}/ai-diagnose`) },
    // Situational Awareness
    getEvents(params) { return http.get('/enterprise/situational-awareness/events', { params }) },
    getEvent(eventId) { return http.get(`/enterprise/situational-awareness/events/${eventId}`) },
    deleteEvent(eventId) { return http.delete(`/enterprise/situational-awareness/events/${eventId}`) },
    createEvent(payload) { return http.post('/enterprise/situational-awareness/events', payload) },
    getVideoBookmarks(params) { return http.get('/enterprise/situational-awareness/video-bookmarks', { params }) },
    deleteVideoBookmark(bookmarkId) { return http.delete(`/enterprise/situational-awareness/video-bookmarks/${bookmarkId}`) },
    createVideoBookmark(payload) { return http.post('/enterprise/situational-awareness/video-bookmarks', payload) },
    getClipRequests(params) { return http.get('/enterprise/situational-awareness/clip-requests', { params }) },
    createClipRequest(payload) { return http.post('/enterprise/situational-awareness/clip-requests', payload) },
    approveClipRequest(clipId, payload) { return http.patch(`/enterprise/situational-awareness/clip-requests/${clipId}/approve`, payload) },
    exportClipRequest(clipId, payload) { return http.patch(`/enterprise/situational-awareness/clip-requests/${clipId}/export`, payload) },
    getAiAdjudications(params) { return http.get('/enterprise/situational-awareness/ai-adjudications', { params }) },
    createAiAdjudication(payload) { return http.post('/enterprise/situational-awareness/ai-adjudications', payload) },
    reviewAiAdjudication(itemId, payload) { return http.patch(`/enterprise/situational-awareness/ai-adjudications/${itemId}/review`, payload) },
    getAiMetrics(params) { return http.get('/enterprise/situational-awareness/ai-metrics', { params }) },
    getAiMetricsSummary() { return http.get('/enterprise/situational-awareness/ai-metrics/summary') },
    recordAiMetric(payload) { return http.post('/enterprise/situational-awareness/ai-metrics', payload) },
    getCorrelations(params) { return http.get('/enterprise/situational-awareness/correlations', { params }) },
    getCorrelationDetail(correlationId) { return http.get(`/enterprise/situational-awareness/correlations/${correlationId}`) },
    runCorrelation(payload) { return http.post('/enterprise/situational-awareness/correlations/run', payload) },
    getSiteMaps() { return http.get('/enterprise/situational-awareness/maps') },
    getMapPlacements(mapId) { return http.get(`/enterprise/situational-awareness/maps/${mapId}/placements`) },
    createSiteMap(payload) { return http.post('/enterprise/situational-awareness/maps', payload) },
    addMapPlacement(mapId, payload) { return http.post(`/enterprise/situational-awareness/maps/${mapId}/placements`, payload) },
    // Evidence
    getEvidenceItems(params) { return http.get('/enterprise/evidence/items', { params }) },
    createEvidenceItem(payload) { return http.post('/enterprise/evidence/items', payload) },
    getEvidenceItem(itemId) { return http.get(`/enterprise/evidence/items/${itemId}`) },
    verifyEvidenceHash(itemId, payload) { return http.post(`/enterprise/evidence/items/${itemId}/verify-hash`, payload) },
    getChainOfCustody(itemId) { return http.get(`/enterprise/evidence/items/${itemId}/custody`) },
    getEvidenceAccessLogs(itemId) { return http.get(`/enterprise/evidence/items/${itemId}/access-logs`) },
    // Collections
    getEvidenceCollections(params) { return http.get('/enterprise/evidence/collections', { params }) },
    getEvidenceCollectionDetail(collectionId) { return http.get(`/enterprise/evidence/collections/${collectionId}`) },
    createEvidenceCollection(payload) { return http.post('/enterprise/evidence/collections', payload) },
    addEvidenceCollectionItem(collectionId, payload) { return http.post(`/enterprise/evidence/collections/${collectionId}/items`, payload) },
    closeEvidenceCollection(collectionId, payload) { return http.patch(`/enterprise/evidence/collections/${collectionId}/close`, payload) },
    addCustodyEntry(itemId, payload) { return http.post(`/enterprise/evidence/items/${itemId}/custody`, payload) },
    // Export Requests
    getExportRequests(params) { return http.get('/enterprise/evidence/export-requests', { params }) },
    createExportRequest(payload) { return http.post('/enterprise/evidence/export-requests', payload) },
    approveExportRequest(exportId, payload) { return http.patch(`/enterprise/evidence/export-requests/${exportId}/approve`, payload) },
    // Redaction
    getRedactionRequests(params) { return http.get('/enterprise/evidence/redaction-requests', { params }) },
    createRedactionRequest(payload) { return http.post('/enterprise/evidence/redaction-requests', payload) },
    approveRedaction(redactId, payload) { return http.patch(`/enterprise/evidence/redaction-requests/${redactId}/approve`, payload) },
    performRedaction(redactId, payload) { return http.patch(`/enterprise/evidence/redaction-requests/${redactId}/perform`, payload) },
    verifyRedaction(redactId, payload) { return http.patch(`/enterprise/evidence/redaction-requests/${redactId}/verify`, payload) },
    // Legal Holds
    getLegalHolds(params) { return http.get('/enterprise/evidence/legal-holds', { params }) },
    createLegalHold(payload) { return http.post('/enterprise/evidence/legal-holds', payload) },
    releaseLegalHold(legalHoldId, payload) { return http.patch(`/enterprise/evidence/legal-holds/${legalHoldId}/release`, payload) },
    // Retention
    getRetentionPolicies(params) { return http.get('/enterprise/evidence/retention-policies', { params }) },
    getRetentionPolicy(policyId) { return http.get(`/enterprise/evidence/retention-policies/${policyId}`) },
    createRetentionPolicy(payload) { return http.post('/enterprise/evidence/retention-policies', payload) },
    updateRetentionPolicy(policyId, payload) { return http.patch(`/enterprise/evidence/retention-policies/${policyId}`, payload) },
    dryRunRetention(payload) { return http.post('/enterprise/evidence/retention/dry-run', payload) },
    purgeEvidence(payload) { return http.post('/enterprise/evidence/retention/purge', payload) },
    // Compliance Reports
     getComplianceReports(params) { return http.get('/enterprise/evidence/compliance-reports', { params }) },
     runComplianceReport(payload) { return http.post('/enterprise/evidence/compliance-reports', payload) },
     // Operations
     getOutboxEvents(params) { return http.get('/enterprise/operations/outbox-events', { params }) },
     getWebhookSubscriptions(params) { return http.get('/enterprise/operations/webhook-subscriptions', { params }) },
     createWebhookSubscription(payload) { return http.post('/enterprise/operations/webhook-subscriptions', payload) },
     getWebhookDeliveries(params) { return http.get('/enterprise/operations/webhook-deliveries', { params }) },
     getSiemExports(params) { return http.get('/enterprise/operations/siem-exports', { params }) },
     // Backup
     getBackupRuns(params) { return http.get('/enterprise/operations/backup-runs', { params }) },
     startBackup(payload) { return http.post('/enterprise/operations/backup-runs', payload) },
     // Restore
     getRestoreDrills(params) { return http.get('/enterprise/operations/restore-drills', { params }) },
     startRestore(payload) { return http.post('/enterprise/operations/restore-drills', payload) },
     // Security
     getSecurityChecks(params) { return http.get('/enterprise/operations/security-checks', { params }) },
     recordSecurityCheck(payload) { return http.post('/enterprise/operations/security-checks', payload) },
     // Config Health
     getConfigHealth() { return http.get('/enterprise/operations/config-health') },
     // Health summary
     getHealthSummary() { return http.get('/enterprise/operations/health-summary') },
     // Webhook operations
     dispatchEvent(eventId) { return http.post(`/enterprise/operations/outbox-events/${eventId}/dispatch`) },
     // SIEM operations
     getSiemExports(params) { return http.get('/enterprise/operations/siem-exports', { params }) },
     // ================= Phase G: Intervention Requests =================
     getInterventionOverview() { return http.get('/enterprise/intervention/overview') },
     getInterventionRequests(params) { return http.get('/enterprise/intervention/requests', { params }) },
     getInterventionRequestDetail(requestId) { return http.get(`/enterprise/intervention/requests/${requestId}`) },
     createInterventionRequest(payload) { return http.post('/enterprise/intervention/requests', payload) },
     acceptInterventionRequest(requestId, payload) { return http.patch(`/enterprise/intervention/requests/${requestId}/accept`, payload) },
     rejectInterventionRequest(requestId, payload) { return http.patch(`/enterprise/intervention/requests/${requestId}/reject`, payload) },
     executeInterventionRequest(requestId, payload) { return http.patch(`/enterprise/intervention/requests/${requestId}/execute`, payload) },
     expireInterventionRequests() { return http.post('/enterprise/intervention/requests/expire') },
      getActiveSecurityAlerts() { return http.get('/security-alerts/active') },
      resetDemoScenarios() { return http.post('/demo-control/reset') },
      // ================= Kiosk Check-in =================
      getVisits(params) { return http.get('/enterprise/visitor-vehicle/visits', { params }) },
      checkInVisit(visitId, payload) { return http.post(`/enterprise/visitor-vehicle/visits/${visitId}/check-in`, payload) },
      acceptForm(visitId, payload) { return http.post(`/enterprise/visitor-vehicle/visits/${visitId}/form-acceptances`, payload) },
}
