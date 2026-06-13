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
