<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Evidence</span>
                <h1 class="page-title">Evidence Repository</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadItems">Refresh</button>
            </div>
        </div>
        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Items</span><h2 class="panel-title">Evidence Items</h2></div>
                    <div class="filter-row">
                        <select v-model="filters.evidenceType" class="form-select" @change="loadItems">
                            <option value="">All Types</option>
                            <option value="Document">Document</option>
                            <option value="Image">Image</option>
                            <option value="Video">Video</option>
                            <option value="Log">Log</option>
                            <option value="Report">Report</option>
                        </select>
                        <select v-model="filters.privacyLabel" class="form-select" @change="loadItems">
                            <option value="">All Privacy</option>
                            <option value="Internal">Internal</option>
                            <option value="Biometric">Biometric</option>
                            <option value="PersonalIdentity">Personal Identity</option>
                            <option value="VehicleIdentity">Vehicle Identity</option>
                            <option value="VisitorDocument">Visitor Document</option>
                            <option value="SensitiveSite">Sensitive Site</option>
                            <option value="Public">Public</option>
                        </select>
                        <select v-model="filters.isLegalHold" class="form-select" @change="loadItems">
                            <option value="">All Hold</option>
                            <option value="true">Legal Hold</option>
                            <option value="false">No Hold</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="items.length === 0" class="empty-card">No evidence items.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Type</th><th>Source</th><th>Privacy</th><th>Retention</th><th>Hash</th><th>Legal Hold</th><th>Created</th><th>Actions</th></tr></thead>
                        <tbody>
                            <tr v-for="item in items" :key="item.evidenceItemId">
                                <td>{{ item.evidenceItemId }}</td>
                                <td><span class="badge badge-info">{{ item.evidenceType }}</span></td>
                                <td class="table-sub">{{ item.sourceType }}:{{ (item.sourceReference || '').substring(0, 20) }}</td>
                                <td><span class="badge" :class="privacyClass(item.privacyLabel)">{{ item.privacyLabel }}</span></td>
                                <td>{{ item.retentionCategory }}</td>
                                <td class="table-sub">{{ item.hashSha256.substring(0, 12) }}...</td>
                                <td><span v-if="item.isLegalHold" class="badge badge-danger">Hold</span><span v-else class="table-sub">—</span></td>
                                <td class="table-sub">{{ new Date(item.createdAtUtc).toLocaleDateString() }}</td>
                                <td><button class="btn btn-secondary btn-sm" @click="viewDetail(item)">Detail</button></td>
                            </tr>
                        </tbody>
                    </table>
                    <div class="pagination-bar">
                        <span>Page {{ page }}/{{ totalPages }}</span>
                        <div class="page-buttons">
                            <button class="page-btn" :disabled="page <= 1" @click="page--; loadItems()">‹</button>
                            <button class="page-btn" :disabled="page >= totalPages" @click="page++; loadItems()">›</button>
                        </div>
                    </div>
                </div>
            </article>
        </section>
        <div v-if="detail" class="modal-overlay" @click.self="detail = null">
            <div class="modal-box wide-modal">
                <h3>Evidence #{{ detail.evidenceItemId }}</h3>
                <div class="detail-grid">
                    <div><strong>Type:</strong> {{ detail.evidenceType }}</div>
                    <div><strong>Source:</strong> {{ detail.sourceType }}</div>
                    <div><strong>Ref:</strong> {{ detail.sourceReference || '—' }}</div>
                    <div><strong>Storage:</strong> {{ detail.storageReference }}</div>
                    <div><strong>Hash:</strong> {{ detail.hashSha256 }}</div>
                    <div><strong>Privacy:</strong> {{ detail.privacyLabel }}</div>
                    <div><strong>Retention:</strong> {{ detail.retentionCategory }}</div>
                    <div><strong>Legal Hold:</strong> {{ detail.isLegalHold ? 'Yes' : 'No' }}</div>
                </div>
                <div class="panel-head" style="margin-top:1rem"><h3>Custody Timeline</h3></div>
                <div v-if="custody.length === 0" class="empty-card">No custody entries.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Action</th><th>Actor</th><th>From</th><th>To</th><th>Note</th><th>Time</th></tr></thead>
                        <tbody>
                            <tr v-for="c in custody" :key="c.chainOfCustodyEntryId">
                                <td><span class="badge badge-info">{{ c.action }}</span></td>
                                <td>{{ c.actorUserId || '—' }}</td>
                                <td class="table-sub">{{ c.fromCustodian || '—' }}</td>
                                <td class="table-sub">{{ c.toCustodian || '—' }}</td>
                                <td class="table-sub">{{ c.note || '—' }}</td>
                                <td class="table-sub">{{ new Date(c.createdAtUtc).toLocaleString() }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="detail = null">Close</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const items = ref([])
const detail = ref(null)
const custody = ref([])
const loading = ref(true)
const page = ref(1)
const totalPages = ref(1)
const filters = reactive({ evidenceType: '', privacyLabel: '', isLegalHold: '' })

async function loadItems() {
    loading.value = true
    try {
        const params = { page: page.value, pageSize: 50 }
        if (filters.evidenceType) params.evidenceType = filters.evidenceType
        if (filters.privacyLabel) params.privacyLabel = filters.privacyLabel
        if (filters.isLegalHold) params.isLegalHold = filters.isLegalHold === 'true'
        const res = await enterpriseApi.getEvidenceItems(params)
        items.value = res.data.items || []
        totalPages.value = Math.ceil((res.data.total || 0) / 50) || 1
    } catch { items.value = [] }
    finally { loading.value = false }
}

async function viewDetail(item) {
    detail.value = item
    try { const res = await enterpriseApi.getChainOfCustody(item.evidenceItemId); custody.value = Array.isArray(res.data) ? res.data : [] }
    catch { custody.value = [] }
}

function privacyClass(l) {
    if (l === 'Biometric' || l === 'PersonalIdentity') return 'badge-danger'
    if (l === 'SensitiveSite') return 'badge-warn'
    if (l === 'VisitorDocument') return 'badge-primary'
    return 'badge-info'
}

onMounted(loadItems)
</script>
