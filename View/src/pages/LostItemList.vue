<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <h1>Quản lý báo mất đồ</h1>
            <button class="btn btn-primary" @click="openCreate">+ Báo mất đồ</button>
        </div>

        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="ops-panel-header">
                    <h3>Hồ sơ người đang tìm đồ</h3>
                    <div class="filter-row">
                        <select v-model="filter" class="form-control" @change="loadItems">
                            <option value="">Tất cả</option>
                            <option value="Pending">Chờ xử lý</option>
                            <option value="MatchFound">Đã ghép</option>
                            <option value="Claimed">Đã nhận lại</option>
                            <option value="Closed">Đã đóng</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="loading-spinner">Đang tải...</div>
                <table class="data-table" v-else>
                    <thead>
                        <tr>
                            <th>Người báo</th>
                            <th>CCCD/CMND</th>
                            <th>SDT</th>
                            <th>Mô tả</th>
                            <th>Ảnh</th>
                            <th>Ngày mất</th>
                            <th>Trạng thái</th>
                            <th>Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in items" :key="item.lostItemReportId">
                            <td>{{ item.reporterName }}</td>
                            <td>{{ item.reporterIdNumber || '---' }}</td>
                            <td>{{ item.reporterPhone }}</td>
                            <td style="max-width:260px" class="text-truncate">{{ item.itemDescription }}</td>
                            <td>
                                <span class="badge badge-success" v-if="item.reporterPhotoUrl && item.photoUrl">Đủ hồ sơ</span>
                                <span class="badge badge-warning" v-else>Thiếu ảnh</span>
                            </td>
                            <td>{{ formatDate(item.lostAtUtc) }}</td>
                            <td><span :class="'badge badge-' + statusClass(item.status)">{{ statusLabel(item.status) }}</span></td>
                            <td class="action-cell">
                                <button class="btn btn-sm" @click="openEvidence(item)">Xem hồ sơ</button>
                                <button class="btn btn-sm btn-secondary" @click="openEdit(item)">Sửa</button>
                                <button v-if="item.status === 'Pending' || item.status === 'MatchFound'" class="btn btn-sm btn-warning" @click="closeItem(item)">Đóng</button>
                                <button class="btn btn-sm btn-danger" @click="removeItem(item)">Xóa</button>
                            </td>
                        </tr>
                        <tr v-if="!items.length"><td colspan="8" class="empty-state">Chưa có dữ liệu.</td></tr>
                    </tbody>
                </table>
                <div class="pagination-bar"><span>Tong: {{ total }}</span></div>
            </article>
        </section>

        <Teleport to="body">
            <div v-if="showForm" class="modal-overlay" @click.self="showForm = false">
                <div class="modal-panel wide-modal">
                    <h3>{{ editing ? 'Cập nhật hồ sơ mất đồ' : 'Tạo hồ sơ mất đồ' }}</h3>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Người báo *</label>
                            <input v-model="form.reporterName" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>CCCD/CMND *</label>
                            <input v-model="form.reporterIdNumber" class="form-control" />
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Số điện thoại *</label>
                            <input v-model="form.reporterPhone" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Email</label>
                            <input v-model="form.reporterEmail" class="form-control" />
                        </div>
                    </div>

                    <div class="form-group">
                        <label>Mô tả đồ vật *</label>
                        <textarea v-model="form.itemDescription" class="form-control" rows="3"></textarea>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Nơi mất gần nhất</label>
                            <input v-model="form.lastSeenLocation" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Thời gian mất *</label>
                            <input type="datetime-local" v-model="form.lostAtUtc" class="form-control" />
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Ảnh người báo *</label>
                            <input type="file" accept="image/*" class="form-control" @change="onFileChange($event, 'reporter')" />
                            <img v-if="form.reporterPhotoPreview" :src="form.reporterPhotoPreview" alt="reporter" class="photo-preview" />
                        </div>
                        <div class="form-group">
                            <label>Ảnh đồ vật *</label>
                            <input type="file" accept="image/*" class="form-control" @change="onFileChange($event, 'item')" />
                            <img v-if="form.itemPhotoPreview" :src="form.itemPhotoPreview" alt="item" class="photo-preview" />
                        </div>
                    </div>

                    <div class="form-actions">
                        <button class="btn btn-primary" @click="submit" :disabled="submitting">{{ submitting ? 'Đang lưu...' : 'Lưu' }}</button>
                        <button class="btn btn-secondary" @click="showForm = false">Hủy</button>
                    </div>
                </div>
            </div>

            <div v-if="showEvidenceModal" class="modal-overlay" @click.self="showEvidenceModal = false">
                <div class="modal-panel wide-modal">
                    <h3>Hồ sơ đối chiếu báo mất đồ</h3>
                    <div class="evidence-grid">
                        <div class="evidence-card">
                            <div class="evidence-title">Người báo mất</div>
                            <img v-if="evidencePreview.reporterPhotoUrl" :src="evidencePreview.reporterPhotoUrl" alt="reporter" class="photo-preview" />
                            <div v-else class="empty-state compact">Chưa có ảnh</div>
                        </div>
                        <div class="evidence-card">
                            <div class="evidence-title">Đồ vật báo mất</div>
                            <img v-if="evidencePreview.itemPhotoUrl" :src="evidencePreview.itemPhotoUrl" alt="item" class="photo-preview" />
                            <div v-else class="empty-state compact">Chưa có ảnh</div>
                        </div>
                    </div>
                    <div class="form-actions">
                        <button class="btn btn-secondary" @click="showEvidenceModal = false">Đóng</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { lostFoundApi } from '../services/enterpriseSecurityApi'

const items = ref([])
const total = ref(0)
const filter = ref('')
const loading = ref(false)
const showForm = ref(false)
const submitting = ref(false)
const showEvidenceModal = ref(false)
const editing = ref(null)

const form = reactive({
    reporterName: '',
    reporterPhone: '',
    reporterEmail: '',
    reporterIdNumber: '',
    itemDescription: '',
    lastSeenLocation: '',
    lostAtUtc: '',
    reporterPhotoUrl: null,
    reporterPhotoBase64: null,
    reporterPhotoPreview: '',
    itemPhotoUrl: null,
    itemPhotoBase64: null,
    itemPhotoPreview: ''
})
const evidencePreview = reactive({
    reporterPhotoUrl: '',
    itemPhotoUrl: ''
})

onMounted(loadItems)

async function loadItems() {
    loading.value = true
    try {
        const res = await lostFoundApi.getLostItems({ status: filter.value || undefined, page: 1, pageSize: 100 })
        items.value = res.data.items || []
        total.value = res.data.total || 0
    } catch (e) {
        console.error(e)
    } finally {
        loading.value = false
    }
}

function resetForm() {
    editing.value = null
    form.reporterName = ''
    form.reporterPhone = ''
    form.reporterEmail = ''
    form.reporterIdNumber = ''
    form.itemDescription = ''
    form.lastSeenLocation = ''
    form.lostAtUtc = ''
    form.reporterPhotoUrl = null
    form.reporterPhotoBase64 = null
    form.reporterPhotoPreview = ''
    form.itemPhotoUrl = null
    form.itemPhotoBase64 = null
    form.itemPhotoPreview = ''
}

function openCreate() {
    resetForm()
    showForm.value = true
}

function openEdit(item) {
    editing.value = item
    form.reporterName = item.reporterName || ''
    form.reporterPhone = item.reporterPhone || ''
    form.reporterEmail = item.reporterEmail || ''
    form.reporterIdNumber = item.reporterIdNumber || ''
    form.itemDescription = item.itemDescription || ''
    form.lastSeenLocation = item.lastSeenLocation || ''
    form.lostAtUtc = toDatetimeLocal(item.lostAtUtc)
    form.reporterPhotoUrl = item.reporterPhotoUrl || null
    form.reporterPhotoBase64 = null
    form.reporterPhotoPreview = item.reporterPhotoUrl || ''
    form.itemPhotoUrl = item.photoUrl || null
    form.itemPhotoBase64 = null
    form.itemPhotoPreview = item.photoUrl || ''
    showForm.value = true
}

function openEvidence(item) {
    evidencePreview.reporterPhotoUrl = item.reporterPhotoUrl || ''
    evidencePreview.itemPhotoUrl = item.photoUrl || ''
    showEvidenceModal.value = true
}

async function onFileChange(event, kind) {
    const file = event.target.files?.[0]
    if (!file) return
    const dataUrl = await readFileAsDataUrl(file)
    if (kind === 'reporter') {
        form.reporterPhotoBase64 = dataUrl
        form.reporterPhotoPreview = dataUrl
    } else {
        form.itemPhotoBase64 = dataUrl
        form.itemPhotoPreview = dataUrl
    }
}

async function submit() {
    if (!form.reporterName || !form.reporterPhone || !form.reporterIdNumber || !form.itemDescription || !form.lostAtUtc) {
        alert('Vui lòng nhập đầy đủ thông tin bắt buộc.')
        return
    }

    if (!form.reporterPhotoBase64 && !form.reporterPhotoUrl) {
        alert('Cần có ảnh người báo.')
        return
    }

    if (!form.itemPhotoBase64 && !form.itemPhotoUrl) {
        alert('Cần có ảnh đồ vật.')
        return
    }

    submitting.value = true
    try {
        const payload = {
            reporterName: form.reporterName,
            reporterPhone: form.reporterPhone,
            reporterEmail: form.reporterEmail || null,
            reporterIdNumber: form.reporterIdNumber,
            reporterPhotoUrl: form.reporterPhotoBase64 ? null : form.reporterPhotoUrl,
            reporterPhotoBase64: form.reporterPhotoBase64,
            itemDescription: form.itemDescription,
            lastSeenLocation: form.lastSeenLocation || null,
            lostAtUtc: new Date(form.lostAtUtc).toISOString(),
            photoUrl: form.itemPhotoBase64 ? null : form.itemPhotoUrl,
            itemPhotoBase64: form.itemPhotoBase64
        }

        if (editing.value) {
            await lostFoundApi.updateLostItem(editing.value.lostItemReportId, payload)
        } else {
            await lostFoundApi.createLostItem(payload)
        }

        showForm.value = false
        await loadItems()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    } finally {
        submitting.value = false
    }
}

async function closeItem(item) {
    if (!confirm(`Đóng hồ sơ "${item.itemDescription}"?`)) return
    try {
        await lostFoundApi.closeLostItem(item.lostItemReportId)
        await loadItems()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    }
}

async function removeItem(item) {
    if (!confirm(`Xóa hồ sơ "${item.itemDescription}"? Hành động này không thể hoàn tác.`)) return
    try {
        await lostFoundApi.deleteLostItem(item.lostItemReportId)
        await loadItems()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    }
}

function readFileAsDataUrl(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader()
        reader.onload = () => resolve(reader.result)
        reader.onerror = reject
        reader.readAsDataURL(file)
    })
}

function toDatetimeLocal(value) {
    if (!value) return ''
    const date = new Date(value)
    const offset = date.getTimezoneOffset()
    const local = new Date(date.getTime() - offset * 60000)
    return local.toISOString().slice(0, 16)
}

function formatDate(d) { return d ? new Date(d).toLocaleDateString('vi-VN') : '' }
function statusClass(s) { const m = { Pending: 'warning', MatchFound: 'primary', Claimed: 'success', Closed: 'secondary' }; return m[s] || 'secondary' }
function statusLabel(s) { const m = { Pending: 'Chờ xử lý', MatchFound: 'Đã ghép', Claimed: 'Đã nhận lại', Closed: 'Đã đóng' }; return m[s] || s }
</script>

<style scoped>
.text-truncate { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.filter-row { display: flex; gap: 0.5rem; align-items: center; }
.filter-row .form-control { width: 180px; }
.loading-spinner { text-align: center; padding: 2rem; color: var(--text-secondary); }
.wide-modal { width: min(920px, calc(100vw - 2rem)); }
.photo-preview { width: 100%; max-height: 180px; object-fit: cover; border-radius: 0.75rem; margin-top: 0.75rem; border: 1px solid var(--border-color, #d7dce5); }
.action-cell { display: flex; flex-wrap: wrap; gap: 0.5rem; }
.evidence-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; }
.evidence-card { border: 1px solid var(--border-color, #d7dce5); border-radius: 0.9rem; padding: 1rem; background: var(--surface-1, #fff); }
.evidence-title { font-weight: 700; margin-bottom: 0.5rem; }
.compact { padding: 1rem; }
</style>
