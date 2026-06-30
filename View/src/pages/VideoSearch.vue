<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Video & evidence</span>
                <h1 class="page-title">Video Search & Bookmark</h1>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Bookmarks</span><h2 class="panel-title">Video Bookmarks</h2></div>
                    <div class="panel-actions">
                        <button class="btn btn-secondary btn-sm" @click="showBmForm = true">+ Bookmark</button>
                    </div>
                </div>
                <div v-if="loadingBm" class="empty-card">Loading...</div>
                <div v-else-if="bookmarks.length === 0" class="empty-card">No bookmarks.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Event ID</th><th>Camera ID</th><th>Start</th><th>End</th><th>Actions</th></tr></thead>
                        <tbody>
                            <tr v-for="b in bookmarks" :key="b.videoBookmarkId">
                                <td>{{ b.securityEventId || '—' }}</td>
                                <td>{{ b.cameraId || '—' }}</td>
                                <td class="table-sub">{{ new Date(b.startUtc).toLocaleString() }}</td>
                                <td class="table-sub">{{ new Date(b.endUtc).toLocaleString() }}</td>
                                <td><button class="btn btn-danger btn-sm" @click="deleteBm(b.videoBookmarkId)">Xóa</button></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Clips</span><h2 class="panel-title">Clip Requests</h2></div>
                    <div class="panel-actions">
                        <select v-model="clipFilter" @change="loadClips" class="form-select">
                            <option value="">All</option>
                            <option value="Pending">Pending</option>
                            <option value="Approved">Approved</option>
                            <option value="Exported">Exported</option>
                        </select>
                        <button class="btn btn-secondary btn-sm" @click="showClipForm = true">+ Request Clip</button>
                    </div>
                </div>
                <div v-if="loadingClip" class="empty-card">Loading...</div>
                <div v-else-if="clips.length === 0" class="empty-card">No clip requests.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Camera</th><th>Status</th><th>Retention</th><th>Export</th><th>Actions</th></tr></thead>
                        <tbody>
                            <tr v-for="c in clips" :key="c.clipRequestId">
                                <td>{{ c.clipRequestId }}</td>
                                <td>{{ c.cameraId || '—' }}</td>
                                <td><span class="badge" :class="c.status === 'Exported' ? 'badge-success' : c.status === 'Approved' ? 'badge-primary' : 'badge-warn'">{{ c.status }}</span></td>
                                <td>{{ c.retentionCategory || '—' }}</td>
                                <td class="table-sub">{{ c.exportReference || '—' }}</td>
                                <td>
                                    <button v-if="c.status === 'Pending'" class="btn btn-success btn-sm" @click="approveClip(c)">Approve</button>
                                    <button v-if="c.status === 'Approved'" class="btn btn-primary btn-sm" @click="exportClip(c)">Export</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>
        <div v-if="showBmForm" class="modal-overlay" @click.self="showBmForm = false">
            <div class="modal-box">
                <h3>Create Video Bookmark</h3>
                <div class="form-group"><label>Event ID</label><input v-model.number="bmForm.securityEventId" type="number" class="form-input" /></div>
                <div class="form-group"><label>Camera ID</label><input v-model.number="bmForm.cameraId" type="number" class="form-input" /></div>
                <div class="form-group"><label>Start UTC</label><input v-model="bmForm.startUtc" type="datetime-local" class="form-input" /></div>
                <div class="form-group"><label>End UTC</label><input v-model="bmForm.endUtc" type="datetime-local" class="form-input" /></div>
                <div class="form-group"><label>Note</label><input v-model="bmForm.note" class="form-input" /></div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showBmForm = false">Cancel</button>
                    <button class="btn btn-primary" :disabled="busy" @click="submitBm">{{ busy ? 'Saving...' : 'Save' }}</button>
                </div>
            </div>
        </div>
        <div v-if="showClipForm" class="modal-overlay" @click.self="showClipForm = false">
            <div class="modal-box">
                <h3>Request Clip</h3>
                <div class="form-group"><label>Camera ID</label><input v-model.number="clipForm.cameraId" type="number" class="form-input" /></div>
                <div class="form-group"><label>Event ID</label><input v-model.number="clipForm.securityEventId" type="number" class="form-input" /></div>
                <div class="form-group"><label>Start</label><input v-model="clipForm.startUtc" type="datetime-local" class="form-input" /></div>
                <div class="form-group"><label>End</label><input v-model="clipForm.endUtc" type="datetime-local" class="form-input" /></div>
                <div class="form-group"><label>Retention</label><select v-model="clipForm.retentionCategory" class="form-select"><option value="">None</option><option value="Evidence">Evidence</option><option value="Training">Training</option><option value="Compliance">Compliance</option></select></div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showClipForm = false">Cancel</button>
                    <button class="btn btn-primary" :disabled="busy" @click="submitClip">Request</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const bookmarks = ref([])
const clips = ref([])
const loadingBm = ref(true)
const loadingClip = ref(true)
const busy = ref(false)
const showBmForm = ref(false)
const showClipForm = ref(false)
const clipFilter = ref('')
const bmForm = reactive({ securityEventId: null, cameraId: null, startUtc: '', endUtc: '', note: '' })
const clipForm = reactive({ cameraId: null, securityEventId: null, startUtc: '', endUtc: '', retentionCategory: '' })

async function loadBookmarks() {
    loadingBm.value = true
    try { const res = await enterpriseApi.getVideoBookmarks(); bookmarks.value = Array.isArray(res.data) ? res.data : [] }
    catch { bookmarks.value = [] }
    finally { loadingBm.value = false }
}

async function loadClips() {
    loadingClip.value = true
    try { const res = await enterpriseApi.getClipRequests({ status: clipFilter.value || undefined }); clips.value = Array.isArray(res.data) ? res.data : [] }
    catch { clips.value = [] }
    finally { loadingClip.value = false }
}

async function submitBm() {
    busy.value = true
    try {
        await enterpriseApi.createVideoBookmark({
            securityEventId: bmForm.securityEventId || null,
            cameraId: bmForm.cameraId || null,
            startUtc: bmForm.startUtc ? new Date(bmForm.startUtc).toISOString() : new Date().toISOString(),
            endUtc: bmForm.endUtc ? new Date(bmForm.endUtc).toISOString() : new Date().toISOString(),
            note: bmForm.note || undefined
        })
        showBmForm.value = false
        await loadBookmarks()
    } finally { busy.value = false }
}

async function submitClip() {
    busy.value = true
    try {
        await enterpriseApi.createClipRequest({
            cameraId: clipForm.cameraId || null,
            securityEventId: clipForm.securityEventId || null,
            startUtc: clipForm.startUtc ? new Date(clipForm.startUtc).toISOString() : new Date().toISOString(),
            endUtc: clipForm.endUtc ? new Date(clipForm.endUtc).toISOString() : new Date().toISOString(),
            retentionCategory: clipForm.retentionCategory || undefined
        })
        showClipForm.value = false
        await loadClips()
    } finally { busy.value = false }
}

async function deleteBm(id) {
    if (!confirm('Delete this bookmark?')) return
    try { await enterpriseApi.deleteVideoBookmark(id); await loadBookmarks() }
    catch { alert('Delete failed') }
}

async function approveClip(c) {
    const cat = prompt('Retention category (Evidence/Training/Compliance):', c.retentionCategory || 'Evidence')
    if (!cat) return
    try { await enterpriseApi.approveClipRequest(c.clipRequestId, { retentionCategory: cat }); await loadClips() }
    catch { alert('Approve failed') }
}

async function exportClip(c) {
    const ref = prompt('Export reference (e.g. file path or URL):')
    if (!ref) return
    try { await enterpriseApi.exportClipRequest(c.clipRequestId, { exportReference: ref }); await loadClips() }
    catch { alert('Export failed') }
}

onMounted(() => { loadBookmarks(); loadClips() })
</script>
