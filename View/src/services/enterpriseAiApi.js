import http from './http'

export const enterpriseAiApi = {
    // Generic AI analysis
    analyze(domain, entityType, entityId, inputData) {
        return http.post('/enterprise/ai/analyze', {
            domain,
            entityType,
            entityId,
            jobType: `${domain}-${entityType}-analysis`,
            inputData: inputData || {},
        })
    },

    // Get recommendations
    getRecommendations(domain, entityType, entityId, limit = 10) {
        return http.get('/enterprise/ai/recommendations', {
            params: { domain, entityType, entityId, limit },
        })
    },

    // Review recommendation (approve/reject)
    reviewRecommendation(id, status, comment) {
        return http.patch(`/enterprise/ai/recommendations/${id}/review`, { status, comment })
    },

    // Submit feedback on recommendation
    submitFeedback(id, feedbackType, comment) {
        return http.post(`/enterprise/ai/recommendations/${id}/feedback`, {
            feedbackType,
            comment: comment || '',
        })
    },

    // SOC Incident Copilot
    analyzeIncident(incidentId) {
        return http.post(`/enterprise/soc/incidents/${incidentId}/ai-briefing`)
    },

    // Evidence AI
    analyzeEvidence(itemId) {
        return http.post(`/enterprise/evidence/items/${itemId}/ai-analyze`)
    },
    reviewExportRequest(exportId) {
        return http.post(`/enterprise/evidence/export-requests/${exportId}/ai-review`)
    },

    // Device Health AI
    getDeviceHealthInsights() {
        return http.get('/enterprise/devices/health-insights')
    },
    diagnoseDevice(deviceId) {
        return http.post(`/enterprise/devices/${deviceId}/ai-diagnose`)
    },

    // UEBA Risk Graph
    explainEmployeeRisk(employeeId) {
        return http.post(`/ueba/employees/${employeeId}/risk-explanation`)
    },

    // Visitor/Vehicle Risk Screening
    screenVisitor(visitId) {
        return http.post(`/enterprise/visitor-vehicle/visitors/${visitId}/screen`)
    },
    screenVehicle(vehicleId) {
        return http.post(`/enterprise/visitor-vehicle/vehicles/${vehicleId}/screen`)
    },

    // Policy Simulation
    simulatePolicy(policyVersionId) {
        return http.post(`/enterprise/ai/policies/${policyVersionId}/simulate`)
    },
    explainPolicy(policyVersionId) {
        return http.post(`/enterprise/ai/policies/${policyVersionId}/explain`)
    },

    // Event Metadata
    ingestEventMetadata(payload) {
        return http.post('/enterprise/ai/event-metadata/ingest', payload)
    },
    searchEventMetadata(params = {}) {
        return http.get('/enterprise/ai/event-metadata/search', { params })
    },

    // Natural Language Query
    naturalLanguageQuery(query) {
        return http.post('/enterprise/ai/query', { query })
    },
}
