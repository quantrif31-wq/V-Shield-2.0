import http from './http'

export const socApi = {
    overview() {
        return http.get('/enterprise/soc/overview')
    },
    getAlarms(params) {
        return http.get('/enterprise/soc/alarms', { params })
    },
    getAlarm(id) {
        return http.get(`/enterprise/soc/alarms/${id}`)
    },
    getAlarmComments(id) {
        return http.get(`/enterprise/soc/alarms/${id}/comments`)
    },
    createAlarm(payload) {
        return http.post('/enterprise/soc/alarms', payload)
    },
    acknowledgeAlarm(id) {
        return http.patch(`/enterprise/soc/alarms/${id}/acknowledge`)
    },
    assignAlarm(id, payload) {
        return http.patch(`/enterprise/soc/alarms/${id}/assign`, payload)
    },
    closeAlarm(id, payload) {
        return http.patch(`/enterprise/soc/alarms/${id}/close`, payload)
    },
    addComment(alarmId, payload) {
        return http.post(`/enterprise/soc/alarms/${alarmId}/comments`, payload)
    },
    classifyAlarm(id) {
        return http.get(`/enterprise/soc/alarms/${id}/classify`)
    },
    recommendSop(id) {
        return http.get(`/enterprise/soc/alarms/${id}/recommend-sop`)
    },
    escalationRisk(id) {
        return http.get(`/enterprise/soc/alarms/${id}/escalation-risk`)
    },
    getIncidents(params) {
        return http.get('/enterprise/soc/incidents', { params })
    },
    getIncident(id) {
        return http.get(`/enterprise/soc/incidents/${id}`)
    },
    getIncidentTimeline(id) {
        return http.get(`/enterprise/soc/incidents/${id}/items`)
    },
    createIncident(payload) {
        return http.post('/enterprise/soc/incidents', payload)
    },
    closeIncident(id, payload) {
        return http.patch(`/enterprise/soc/incidents/${id}/close`, payload)
    },
    addIncidentTimelineItem(incidentId, payload) {
        return http.post(`/enterprise/soc/incidents/${incidentId}/timeline`, payload)
    },
    getSopTemplates(activeOnly) {
        return http.get('/enterprise/soc/sop-templates', { params: { activeOnly } })
    },
    getSopExecutions(params) {
        return http.get('/enterprise/soc/sop-executions', { params })
    },
    startSopExecution(payload) {
        return http.post('/enterprise/soc/sop-executions', payload)
    },
    completeSopExecution(id, payload) {
        return http.patch(`/enterprise/soc/sop-executions/${id}/complete`, payload)
    },
    getDispatchTasks(params) {
        return http.get('/enterprise/soc/dispatch-tasks', { params })
    },
    createDispatchTask(payload) {
        return http.post('/enterprise/soc/dispatch-tasks', payload)
    },
    completeDispatchTask(id, payload) {
        return http.patch(`/enterprise/soc/dispatch-tasks/${id}/complete`, payload)
    },
    getAnomalies() {
        return http.get('/enterprise/soc/anomalies')
    },
    getIntelligence() {
        return http.get('/enterprise/soc/intelligence')
    },
}
