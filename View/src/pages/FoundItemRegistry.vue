<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <h1>Quản lý đồ tìm thấy</h1>
            <button class="btn btn-primary" @click="openCreate">+ Nhận đồ tìm thấy</button>
        </div>

        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="ops-panel-header">
                    <h3>Hồ sơ lưu kho và người nhặt được</h3>
                    <div class="filter-row">
                        <select v-model="filter" class="form-control" @change="loadItems">
                            <option value="">Tất cả</option>
                            <option value="Unclaimed">Chưa trả</option>
                            <option value="ClaimPending">Chờ duyệt</option>
                            <option value="Returned">Đã trả</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="loading-spinner">Đang tải...</div>
                <table class="data-table" v-else>
                    <thead>
                        <tr>
                            <th>Người nhặt được</th>
                            <th>CCCD/CMND</th>
                            <th>Mô tả</th>
                            <th>Nơi tìm thấy</th>
                            <th>Lưu kho</th>
                            <th>Ảnh</th>
                            <th>Trạng thái</th>
                            <th>Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in items" :key="item.foundItemReportId">
                            <td>{{ item.foundByName }}</td>
                            <td>{{ item.foundByIdNumber || '---' }}</td>
                            <td style="max-width:260px" class="text-truncate">{{ item.itemDescription }}</td>
                            <td>{{ item.foundLocation }}</td>
                            <td>{{ storageLabel(item) }}</td>
                            <td>
                                <span class="badge badge-success" v-if="item.finderPhotoUrl && item.photoUrl">Đủ hồ sơ</span>
                                <span class="badge badge-warning" v-else>Thiếu ảnh</span>
                            </td>
                            <td><span :class="'badge badge-' + statusClass(item.status)">{{ statusLabel(item.status) }}</span></td>
                            <td class="action-cell">
                                <button class="btn btn-sm" @click="openEvidence(item)">Xem hồ sơ</button>
                                <button v-if="item.status === 'Unclaimed'" class="btn btn-sm btn-primary" @click="openClaim(item)">Tạo yêu cầu</button>
                                <button class="btn btn-sm btn-secondary" @click="openEdit(item)">Sửa</button>
                                <button class="btn btn-sm btn-danger" @click="removeItem(item)">Xóa</button>
                            </td>
                        </tr>
                        <tr v-if="!items.length"><td colspan="8" class="empty-state">Chưa có dữ liệu.</td></tr>
                    </tbody>
                </table>
            </article>
        </section>

        <Teleport to="body">
            <div v-if="showForm" class="modal-overlay" @click.self="showForm = false">
                <div class="modal-panel wide-modal">
                    <h3>{{ editing ? 'Cập nhật đồ tìm thấy' : 'Nhập đồ tìm thấy' }}</h3>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Người nhặt được *</label>
                            <input v-model="form.foundByName" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>CCCD/CMND *</label>
                            <input v-model="form.foundByIdNumber" class="form-control" />
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Số điện thoại *</label>
                            <input v-model="form.foundByPhone" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Nơi tìm thấy *</label>
                            <input v-model="form.foundLocation" class="form-control" />
                        </div>
                    </div>

                    <div class="form-group">
                        <label>Mô tả đồ vật *</label>
                        <textarea v-model="form.itemDescription" class="form-control" rows="3"></textarea>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Vị trí lưu trữ</label>
                            <input v-model="form.storageLocation" class="form-control" placeholder="Ví dụ: Tủ A, ngăn B2" />
                        </div>
                        <div class="form-group">
                            <label>Ngăn locker</label>
                            <select v-model="form.lockerCompartmentId" class="form-control">
                                <option :value="null">-- Không chọn --</option>
                                <option v-for="c in compartments" :key="c.lockerCompartmentId" :value="c.lockerCompartmentId">
                                    {{ cabinetName(c) }} - {{ c.code }}
                                </option>
                            </select>
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Ảnh người nhặt được *</label>
                            <input type="file" accept="image/*" class="form-control" @change="onFileChange($event, 'finder')" />
                            <img v-if="form.finderPhotoPreview" :src="form.finderPhotoPreview" alt="finder" class="photo-preview" />
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

            <div v-if="showClaimForm" class="modal-overlay" @click.self="showClaimForm = false">
                <div class="modal-panel wide-modal">
                    <h3>Tạo yêu cầu nhận lại đồ</h3>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Người nhận *</label>
                            <input v-model="claimForm.claimantName" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>CCCD/CMND *</label>
                            <input v-model="claimForm.claimantIdNumber" class="form-control" />
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Số điện thoại</label>
                            <input v-model="claimForm.claimantPhone" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Hồ sơ báo mất liên quan</label>
                            <select v-model="claimForm.lostItemReportId" class="form-control">
                                <option :value="null">-- Không liên kết --</option>
                                <option v-for="lost in lostItemOptions" :key="lost.lostItemReportId" :value="lost.lostItemReportId">
                                    {{ lost.reporterName }} - {{ lost.itemDescription }}
                                </option>
                            </select>
                        </div>
                    </div>

                    <div class="form-group">
                        <label>Đường dẫn giấy tờ minh chứng</label>
                        <input v-model="claimForm.proofDocumentUrl" class="form-control" placeholder="Nếu có file scan/đường dẫn nội bộ" />
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Ảnh người nhận *</label>
                            <input type="file" accept="image/*" class="form-control" @change="onClaimFileChange($event, 'claimant')" />
                            <img v-if="claimForm.claimantPhotoPreview" :src="claimForm.claimantPhotoPreview" alt="claimant" class="photo-preview" />
                        </div>
                        <div class="form-group">
                            <label>Ảnh người nhận cầm/vật đối chiếu *</label>
                            <input type="file" accept="image/*" class="form-control" @change="onClaimFileChange($event, 'item')" />
                            <img v-if="claimForm.itemPhotoPreview" :src="claimForm.itemPhotoPreview" alt="claim-item" class="photo-preview" />
                        </div>
                    </div>

                    <div class="form-actions">
                        <button class="btn btn-primary" @click="submitClaim" :disabled="submittingClaim">{{ submittingClaim ? 'Đang lưu...' : 'Tạo yêu cầu' }}</button>
                        <button class="btn btn-secondary" @click="showClaimForm = false">Hủy</button>
                    </div>
                </div>
            </div>

            <div v-if="showEvidenceModal" class="modal-overlay" @click.self="showEvidenceModal = false">
                <div class="modal-panel wide-modal">
                    <h3>Hồ sơ đối chiếu đồ tìm thấy</h3>
                    <div class="evidence-grid">
                        <div class="evidence-card">
                            <div class="evidence-title">Người nhặt được</div>
                            <img v-if="evidencePreview.finderPhotoUrl" :src="evidencePreview.finderPhotoUrl" alt="finder" class="photo-preview" />
                            <div v-else class="empty-state compact">Chưa có ảnh</div>
                        </div>
                        <div class="evidence-card">
                            <div class="evidence-title">Đồ vật</div>
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
const showClaimForm = ref(false)
const submittingClaim = ref(false)
const showEvidenceModal = ref(false)
const compartments = ref([])
const editing = ref(null)
const claimTarget = ref(null)
const lostItemOptions = ref([])

const form = reactive({
    foundByName: '',
    foundByPhone: '',
    foundByIdNumber: '',
    foundLocation: '',
    itemDescription: '',
    storageLocation: '',
    lockerCompartmentId: null,
    finderPhotoUrl: null,
    finderPhotoBase64: null,
    finderPhotoPreview: '',
    itemPhotoUrl: null,
    itemPhotoBase64: null,
    itemPhotoPreview: ''
})

const claimForm = reactive({
    claimantName: '',
    claimantIdNumber: '',
    claimantPhone: '',
    proofDocumentUrl: '',
    lostItemReportId: null,
    claimantPhotoUrl: null,
    claimantPhotoBase64: null,
    claimantPhotoPreview: '',
    itemPhotoUrl: null,
    itemPhotoBase64: null,
    itemPhotoPreview: ''
})
const evidencePreview = reactive({
    finderPhotoUrl: '',
    itemPhotoUrl: ''
})

onMounted(async () => {
    await Promise.all([loadItems(), loadCompartments()])
})

async function loadItems() {
    loading.value = true
    try {
        const res = await lostFoundApi.getFoundItems({ status: filter.value || undefined, page: 1, pageSize: 100 })
        items.value = res.data.items || []
        total.value = res.data.total || 0
    } catch (e) {
        console.error(e)
    } finally {
        loading.value = false
    }
}

async function loadCompartments() {
    try {
        const res = await lostFoundApi.getAvailableCompartments()
        compartments.value = res.data || []
    } catch (e) {
        console.error(e)
    }
}

async function loadLostItemOptions() {
    try {
        const res = await lostFoundApi.getLostItems({ page: 1, pageSize: 200 })
        lostItemOptions.value = res.data.items || []
    } catch (e) {
        console.error(e)
    }
}

function cabinetName(c) {
    return c.cabinet?.name || `Tu #${c.lockerCabinetId}`
}

function storageLabel(item) {
    const locker = item.lockerCompartment?.cabinet?.name && item.lockerCompartment?.code
        ? `${item.lockerCompartment.cabinet.name} - ${item.lockerCompartment.code}`
        : ''
    return item.storageLocation || locker || '---'
}

function resetForm() {
    editing.value = null
    form.foundByName = ''
    form.foundByPhone = ''
    form.foundByIdNumber = ''
    form.foundLocation = ''
    form.itemDescription = ''
    form.storageLocation = ''
    form.lockerCompartmentId = null
    form.finderPhotoUrl = null
    form.finderPhotoBase64 = null
    form.finderPhotoPreview = ''
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
    form.foundByName = item.foundByName || ''
    form.foundByPhone = item.foundByPhone || ''
    form.foundByIdNumber = item.foundByIdNumber || ''
    form.foundLocation = item.foundLocation || ''
    form.itemDescription = item.itemDescription || ''
    form.storageLocation = item.storageLocation || ''
    form.lockerCompartmentId = item.lockerCompartmentId ?? null
    form.finderPhotoUrl = item.finderPhotoUrl || null
    form.finderPhotoBase64 = null
    form.finderPhotoPreview = item.finderPhotoUrl || ''
    form.itemPhotoUrl = item.photoUrl || null
    form.itemPhotoBase64 = null
    form.itemPhotoPreview = item.photoUrl || ''
    showForm.value = true
}

async function openClaim(item) {
    claimTarget.value = item
    claimForm.claimantName = ''
    claimForm.claimantIdNumber = ''
    claimForm.claimantPhone = ''
    claimForm.proofDocumentUrl = ''
    claimForm.lostItemReportId = null
    claimForm.claimantPhotoUrl = null
    claimForm.claimantPhotoBase64 = null
    claimForm.claimantPhotoPreview = ''
    claimForm.itemPhotoUrl = null
    claimForm.itemPhotoBase64 = null
    claimForm.itemPhotoPreview = ''
    await loadLostItemOptions()
    showClaimForm.value = true
}

function openEvidence(item) {
    evidencePreview.finderPhotoUrl = item.finderPhotoUrl || ''
    evidencePreview.itemPhotoUrl = item.photoUrl || ''
    showEvidenceModal.value = true
}

async function onFileChange(event, kind) {
    const file = event.target.files?.[0]
    if (!file) return
    const dataUrl = await readFileAsDataUrl(file)
    if (kind === 'finder') {
        form.finderPhotoBase64 = dataUrl
        form.finderPhotoPreview = dataUrl
    } else {
        form.itemPhotoBase64 = dataUrl
        form.itemPhotoPreview = dataUrl
    }
}

async function onClaimFileChange(event, kind) {
    const file = event.target.files?.[0]
    if (!file) return
    const dataUrl = await readFileAsDataUrl(file)
    if (kind === 'claimant') {
        claimForm.claimantPhotoBase64 = dataUrl
        claimForm.claimantPhotoPreview = dataUrl
    } else {
        claimForm.itemPhotoBase64 = dataUrl
        claimForm.itemPhotoPreview = dataUrl
    }
}

async function submit() {
    if (!form.foundByName || !form.foundByPhone || !form.foundByIdNumber || !form.foundLocation || !form.itemDescription) {
        alert('Vui lòng nhập đầy đủ thông tin bắt buộc.')
        return
    }

    if (!form.finderPhotoBase64 && !form.finderPhotoUrl) {
        alert('Cần có ảnh người nhặt được.')
        return
    }

    if (!form.itemPhotoBase64 && !form.itemPhotoUrl) {
        alert('Cần có ảnh đồ vật.')
        return
    }

    submitting.value = true
    try {
        const payload = {
            foundByName: form.foundByName,
            foundByPhone: form.foundByPhone,
            foundByIdNumber: form.foundByIdNumber,
            finderPhotoUrl: form.finderPhotoBase64 ? null : form.finderPhotoUrl,
            finderPhotoBase64: form.finderPhotoBase64,
            foundLocation: form.foundLocation,
            foundAtUtc: editing.value?.foundAtUtc || new Date().toISOString(),
            itemDescription: form.itemDescription,
            photoUrl: form.itemPhotoBase64 ? null : form.itemPhotoUrl,
            photoBase64: form.itemPhotoBase64,
            storageLocation: form.storageLocation || null,
            lockerCompartmentId: form.lockerCompartmentId
        }

        if (editing.value) {
            await lostFoundApi.updateFoundItem(editing.value.foundItemReportId, payload)
        } else {
            await lostFoundApi.createFoundItem(payload)
        }

        showForm.value = false
        await Promise.all([loadItems(), loadCompartments()])
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    } finally {
        submitting.value = false
    }
}

async function removeItem(item) {
    if (!confirm(`Xóa hồ sơ "${item.itemDescription}"? Hành động này không thể hoàn tác.`)) return
    try {
        await lostFoundApi.deleteFoundItem(item.foundItemReportId)
        await Promise.all([loadItems(), loadCompartments()])
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    }
}

async function submitClaim() {
    if (!claimTarget.value) return
    if (!claimForm.claimantName || !claimForm.claimantIdNumber) {
        alert('Cần nhập tên và CCCD/CMND người nhận.')
        return
    }
    if (!claimForm.claimantPhotoBase64 && !claimForm.claimantPhotoUrl) {
        alert('Cần có ảnh người nhận.')
        return
    }
    if (!claimForm.itemPhotoBase64 && !claimForm.itemPhotoUrl) {
        alert('Cần có ảnh đối chiếu với đồ vật.')
        return
    }

    submittingClaim.value = true
    try {
        await lostFoundApi.createClaimRequest({
            foundItemReportId: claimTarget.value.foundItemReportId,
            lostItemReportId: claimForm.lostItemReportId,
            claimantName: claimForm.claimantName,
            claimantIdNumber: claimForm.claimantIdNumber,
            claimantPhone: claimForm.claimantPhone || null,
            proofDocumentUrl: claimForm.proofDocumentUrl || null,
            claimantPhotoUrl: claimForm.claimantPhotoBase64 ? null : claimForm.claimantPhotoUrl,
            claimantPhotoBase64: claimForm.claimantPhotoBase64,
            itemPhotoUrl: claimForm.itemPhotoBase64 ? null : claimForm.itemPhotoUrl,
            itemPhotoBase64: claimForm.itemPhotoBase64
        })
        showClaimForm.value = false
        await loadItems()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    } finally {
        submittingClaim.value = false
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

function statusClass(s) { const m = { Unclaimed: 'info', ClaimPending: 'warning', Returned: 'success' }; return m[s] || 'secondary' }
function statusLabel(s) { const m = { Unclaimed: 'Chưa trả', ClaimPending: 'Chờ duyệt', Returned: 'Đã trả' }; return m[s] || s }
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
