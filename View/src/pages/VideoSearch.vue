<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Video & bằng chứng</span>
                <h1 class="page-title">Tìm kiếm video & Đánh dấu</h1>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Đánh dấu</span><h2 class="panel-title">Video đã đánh dấu</h2></div>
                    <div class="panel-actions">
                        <button class="btn btn-secondary btn-sm" @click="showBmForm = true">+ Thêm đánh dấu</button>
                    </div>
                </div>
                <div v-if="loadingBm" class="empty-card">Đang tải...</div>
                <div v-else-if="bookmarks.length === 0" class="empty-card">Chưa có đánh dấu nào.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Mã sự kiện</th><th>Mã camera</th><th>Bắt đầu</th><th>Kết thúc</th><th>Thao tác</th></tr></thead>
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
                    <div><span class="panel-kicker">Video cắt</span><h2 class="panel-title">Yêu cầu cắt video</h2></div>
                    <div class="panel-actions">
                        <select v-model="clipFilter" @change="loadClips" class="form-select">
                            <option value="">Tất cả</option>
                            <option value="Pending">Chờ duyệt</option>
                            <option value="Approved">Đã duyệt</option>
                            <option value="Exported">Đã xuất</option>
                        </select>
                        <button class="btn btn-secondary btn-sm" @click="showClipForm = true">+ Yêu cầu cắt</button>
                    </div>
                </div>
                <div v-if="loadingClip" class="empty-card">Đang tải...</div>
                <div v-else-if="clips.length === 0" class="empty-card">Chưa có yêu cầu nào.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Mã</th><th>Camera</th><th>Trạng thái</th><th>Lưu trữ</th><th>Xuất</th><th>Thao tác</th></tr></thead>
                        <tbody>
                            <tr v-for="c in clips" :key="c.clipRequestId">
                                <td>{{ c.clipRequestId }}</td>
                                <td>{{ c.cameraId || '—' }}</td>
                                <td><span class="badge" :class="c.status === 'Exported' ? 'badge-success' : c.status === 'Approved' ? 'badge-primary' : 'badge-warn'">{{ statusLabel[c.status] || c.status }}</span></td>
                                <td>{{ c.retentionCategory || '—' }}</td>
                                <td class="table-sub">{{ c.exportReference || '—' }}</td>
                                <td>
                                    <button v-if="c.status === 'Pending'" class="btn btn-success btn-sm" @click="approveClip(c)">Duyệt</button>
                                    <button v-if="c.status === 'Approved'" class="btn btn-primary btn-sm" @click="exportClip(c)">Xuất</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>
        <div v-if="showBmForm" class="modal-overlay" @click.self="showBmForm = false">
            <div class="modal-box">
                <h3>Tạo đánh dấu video</h3>
                <div class="form-group"><label>Mã sự kiện</label><input v-model.number="bmForm.securityEventId" type="number" class="form-input" /></div>
                <div class="form-group"><label>Mã camera</label><input v-model.number="bmForm.cameraId" type="number" class="form-input" /></div>
                <div class="form-group"><label>Bắt đầu (UTC)</label><input v-model="bmForm.startUtc" type="datetime-local" class="form-input" /></div>
                <div class="form-group"><label>Kết thúc (UTC)</label><input v-model="bmForm.endUtc" type="datetime-local" class="form-input" /></div>
                <div class="form-group"><label>Ghi chú</label><input v-model="bmForm.note" class="form-input" /></div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showBmForm = false">Hủy</button>
                    <button class="btn btn-primary" :disabled="busy" @click="submitBm">{{ busy ? 'Đang lưu...' : 'Lưu' }}</button>
                </div>
            </div>
        </div>
        <div v-if="showClipForm" class="modal-overlay" @click.self="showClipForm = false">
            <div class="modal-box">
                <h3>Yêu cầu cắt video</h3>
                <div class="form-group"><label>Mã camera</label><input v-model.number="clipForm.cameraId" type="number" class="form-input" /></div>
                <div class="form-group"><label>Mã sự kiện</label><input v-model.number="clipForm.securityEventId" type="number" class="form-input" /></div>
                <div class="form-group"><label>Bắt đầu</label><input v-model="clipForm.startUtc" type="datetime-local" class="form-input" /></div>
                <div class="form-group"><label>Kết thúc</label><input v-model="clipForm.endUtc" type="datetime-local" class="form-input" /></div>
                <div class="form-group"><label>Lưu trữ</label><select v-model="clipForm.retentionCategory" class="form-select"><option value="">Không</option><option value="Evidence">Bằng chứng</option><option value="Training">Đào tạo</option><option value="Compliance">Tuân thủ</option></select></div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showClipForm = false">Hủy</button>
                    <button class="btn btn-primary" :disabled="busy" @click="submitClip">Gửi yêu cầu</button>
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
const statusLabel = {
    Pending: 'Chờ duyệt',
    Approved: 'Đã duyệt',
    Exported: 'Đã xuất',
    Evidence: 'Bằng chứng',
    Training: 'Đào tạo',
    Compliance: 'Tuân thủ'
}
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
    if (!confirm('Xóa đánh dấu này?')) return
    try { await enterpriseApi.deleteVideoBookmark(id); await loadBookmarks() }
    catch { alert('Xóa thất bại') }
}

async function approveClip(c) {
    const cat = prompt('Loại lưu trữ (Bằng chứng/Đào tạo/Tuân thủ):', c.retentionCategory || 'Evidence')
    if (!cat) return
    try { await enterpriseApi.approveClipRequest(c.clipRequestId, { retentionCategory: cat }); await loadClips() }
    catch { alert('Duyệt thất bại') }
}

async function exportClip(c) {
    const ref = prompt('Tham chiếu xuất (ví dụ: đường dẫn tệp hoặc URL):')
    if (!ref) return
    try { await enterpriseApi.exportClipRequest(c.clipRequestId, { exportReference: ref }); await loadClips() }
    catch { alert('Xuất thất bại') }
}

onMounted(() => { loadBookmarks(); loadClips() })
</script>

<style scoped>
.form-select {
    transition: border-color var(--transition-fast), box-shadow var(--transition-fast), background var(--transition-fast);
}
.form-select:hover {
    border-color: var(--border-strong);
}
</style>
