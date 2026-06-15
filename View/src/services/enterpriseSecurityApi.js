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
}
