<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Retention</span>
                <h1 class="page-title">Retention & Legal Hold Dashboard</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadAll">Refresh</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Policies</span><h2 class="panel-title">Retention Policies</h2></div>
                    <button class="btn btn-secondary btn-sm" @click="dryRun">Dry Run</button>
                </div>
                <div v-if="loadingPolicies" class="empty-card">Loading...</div>
                <div v-else-if="policies.length === 0" class="empty-card">No policies. Create one via API.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Name</th><th>Type</th><th>Category</th><th>Days</th><th>Purge Mode</th><th>Active</th><th>Actions</th></tr></thead>
                        <tbody>
                            <tr v-for="p in policies" :key="p.retentionPolicyId">
                                <td>{{ p.name }}</td>
                                <td>{{ p.evidenceType }}</td>
                                <td>{{ p.retentionCategory }}</td>
                                <td>{{ p.retentionDays }}</td>
                                <td><span class="badge badge-info">{{ p.purgeMode }}</span></td>
                                <td><span class="badge" :class="p.isActive ? 'badge-success' : 'badge-secondary'">{{ p.isActive ? 'Active' : 'Inactive' }}</span></td>
                                <td>
                                    <button v-if="p.isActive" class="btn btn-warning btn-sm" @click="toggleActive(p, false)">Deactivate</button>
                                    <button v-else class="btn btn-success btn-sm" @click="toggleActive(p, true)">Activate</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Legal Holds</span><h2 class="panel-title">Active Legal Holds</h2></div>
                </div>
                <div v-if="loadingHolds" class="empty-card">Loading...</div>
                <div v-else-if="holds.length === 0" class="empty-card">No legal holds.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Evidence ID</th><th>Collection ID</th><th>Reason</th><th>Status</th><th>Applied</th></tr></thead>
                        <tbody>
                            <tr v-for="h in holds" :key="h.legalHoldId">
                                <td>{{ h.legalHoldId }}</td>
                                <td>{{ h.evidenceItemId || '—' }}</td>
                                <td>{{ h.evidenceCollectionId || '—' }}</td>
                                <td class="table-sub">{{ h.reason.substring(0, 40) }}</td>
                                <td><span class="badge" :class="h.status === 'Active' ? 'badge-danger' : 'badge-info'">{{ h.status }}</span></td>
                                <td class="table-sub">{{ new Date(h.appliedAtUtc).toLocaleString() }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>
        <div v-if="dryRunResult" class="modal-overlay" @click.self="dryRunResult = null">
            <div class="modal-box wide-modal">
                <h3>Dry Run Results</h3>
                <pre class="dry-run-output">{{ JSON.stringify(dryRunResult, null, 2) }}</pre>
                <div class="modal-actions">
                    <button class="btn btn-danger" :disabled="purgeBusy" @click="confirmPurge">Purge Listed Items</button>
                    <button class="btn btn-secondary" @click="dryRunResult = null">Close</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const policies = ref([])
const holds = ref([])
const loadingPolicies = ref(true)
const loadingHolds = ref(true)
const dryRunResult = ref(null)
const purgeBusy = ref(false)

async function loadAll() { await Promise.all([loadPolicies(), loadHolds()]) }

async function loadPolicies() {
    loadingPolicies.value = true
    try { const res = await enterpriseApi.getRetentionPolicies({ isActive: true }); policies.value = Array.isArray(res.data) ? res.data : [] }
    catch { policies.value = [] }
    finally { loadingPolicies.value = false }
}

async function loadHolds() {
    loadingHolds.value = true
    try { const res = await enterpriseApi.getLegalHolds({ status: 'Active' }); holds.value = Array.isArray(res.data) ? res.data : [] }
    catch { holds.value = [] }
    finally { loadingHolds.value = false }
}

async function toggleActive(p, active) {
    try { await enterpriseApi.updateRetentionPolicy(p.retentionPolicyId, { isActive: active }); await loadPolicies() }
    catch { alert('Update failed') }
}

async function dryRun() {
    try {
        const res = await enterpriseApi.dryRunRetention({ asOfUtc: new Date().toISOString(), limit: 100 })
        dryRunResult.value = res.data
    } catch { alert('Dry run failed') }
}

async function confirmPurge() {
    if (!confirm('This will purge evidence items. This action requires step-up MFA. Continue?')) return
    purgeBusy.value = true
    try {
        const ids = dryRunResult.value?.candidates?.map(c => c.evidenceItemId) || []
        await enterpriseApi.purgeEvidence({ evidenceItemIds: ids, reason: 'Retention policy purge' })
        dryRunResult.value = null
        alert('Purge completed')
    } catch { alert('Purge failed') }
    finally { purgeBusy.value = false }
}

onMounted(loadAll)
</script>
