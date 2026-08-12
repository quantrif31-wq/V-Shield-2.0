<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <h1>Duyệt và trao trả đồ thất lạc</h1>
        </div>

        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="ops-panel-header">
                    <h3>Hồ sơ nhận lại đồ</h3>
                    <div class="filter-row">
                        <select v-model="filter" class="form-control" @change="loadClaims">
                            <option value="">Tất cả</option>
                            <option value="Pending">Chờ duyệt</option>
                            <option value="Approved">Đã duyệt</option>
                            <option value="Completed">Hoàn tất</option>
                            <option value="Rejected">Từ chối</option>
                            <option value="Cancelled">Đã hủy</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="loading-spinner">Đang tải...</div>
                <table class="data-table" v-else>
                    <thead>
                        <tr>
                            <th>Người nhận</th>
                            <th>CCCD/CMND</th>
                            <th>SDT</th>
                            <th>Đồ vật</th>
                            <th>Hồ sơ</th>
                            <th>Trạng thái</th>
                            <th>Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="c in claims" :key="c.claimRequestId">
                            <td>{{ c.claimantName }}</td>
                            <td>{{ c.claimantIdNumber }}</td>
                            <td>{{ c.claimantPhone || '---' }}</td>
                            <td style="max-width:220px" class="text-truncate">{{ c.foundItem?.itemDescription || '---' }}</td>
                            <td>
                                <span class="badge badge-success" v-if="c.claimantPhotoUrl && c.itemPhotoUrl">Đầy đủ</span>
                                <span class="badge badge-warning" v-else>Cần bổ sung</span>
                            </td>
                            <td><span :class="'badge badge-' + statusClass(c.status)">{{ statusLabel(c.status) }}</span></td>
                            <td class="action-cell">
                                <button class="btn btn-sm" @click="openEvidence(c)">Xem hồ sơ</button>
                                <button v-if="canEdit(c)" class="btn btn-sm btn-secondary" @click="openEdit(c)">Sửa</button>
                                <button v-if="canCancel(c)" class="btn btn-sm btn-danger" @click="cancelClaim(c)">Hủy</button>
                                <button v-if="c.status === 'Pending'" class="btn btn-sm btn-success" @click="approve(c)">Duyệt</button>
                                <button v-if="c.status === 'Pending'" class="btn btn-sm btn-danger" @click="reject(c)">Từ chối</button>
                                <button v-if="c.status === 'Approved'" class="btn btn-sm btn-primary" @click="openComplete(c)">Trả đồ</button>
                                <button v-if="c.status === 'Completed'" class="btn btn-sm btn-primary" @click="printReceipt(c)">In biên bản</button>
                            </td>
                        </tr>
                        <tr v-if="!claims.length"><td colspan="7" class="empty-state">Chưa có yêu cầu nào.</td></tr>
                    </tbody>
                </table>
            </article>
        </section>

        <Teleport to="body">
            <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
                <div class="modal-panel wide-modal">
                    <h3>Cập nhật hồ sơ nhận lại đồ</h3>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Người nhận *</label>
                            <input v-model="editForm.claimantName" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>CCCD/CMND *</label>
                            <input v-model="editForm.claimantIdNumber" class="form-control" />
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Số điện thoại</label>
                            <input v-model="editForm.claimantPhone" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Hồ sơ báo mất liên quan</label>
                            <select v-model="editForm.lostItemReportId" class="form-control">
                                <option :value="null">-- Không liên kết --</option>
                                <option v-for="lost in lostItemOptions" :key="lost.lostItemReportId" :value="lost.lostItemReportId">
                                    {{ lost.reporterName }} - {{ lost.itemDescription }}
                                </option>
                            </select>
                        </div>
                    </div>

                    <div class="form-group">
                        <label>Đường dẫn giấy tờ minh chứng</label>
                        <input v-model="editForm.proofDocumentUrl" class="form-control" placeholder="Nếu có file scan/đường dẫn nội bộ" />
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Ảnh người nhận *</label>
                            <input type="file" accept="image/*" class="form-control" @change="onEditFileChange($event, 'claimant')" />
                            <img v-if="editForm.claimantPhotoPreview" :src="editForm.claimantPhotoPreview" alt="claimant" class="photo-preview" />
                        </div>
                        <div class="form-group">
                            <label>Ảnh vật đối chiếu *</label>
                            <input type="file" accept="image/*" class="form-control" @change="onEditFileChange($event, 'item')" />
                            <img v-if="editForm.itemPhotoPreview" :src="editForm.itemPhotoPreview" alt="claim-item" class="photo-preview" />
                        </div>
                    </div>

                    <div class="form-actions">
                        <button class="btn btn-primary" @click="submitEdit" :disabled="submittingEdit">{{ submittingEdit ? 'Đang lưu...' : 'Lưu thay đổi' }}</button>
                        <button class="btn btn-secondary" @click="showEditModal = false">Hủy</button>
                    </div>
                </div>
            </div>

            <div v-if="showCompleteModal" class="modal-overlay" @click.self="showCompleteModal = false">
                <div class="modal-panel wide-modal">
                    <h3>Hoàn tất trao trả</h3>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Ảnh người nhận</label>
                            <input type="file" accept="image/*" class="form-control" @change="onCompleteFileChange($event, 'claimant')" />
                            <img v-if="completeForm.claimantPhotoPreview" :src="completeForm.claimantPhotoPreview" alt="claimant" class="photo-preview" />
                        </div>
                        <div class="form-group">
                            <label>Ảnh bàn giao đồ vật *</label>
                            <input type="file" accept="image/*" class="form-control" @change="onCompleteFileChange($event, 'return')" />
                            <img v-if="completeForm.returnPhotoPreview" :src="completeForm.returnPhotoPreview" alt="return" class="photo-preview" />
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Người chứng kiến</label>
                            <input v-model="completeForm.witnessName" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Ghi chú bàn giao *</label>
                            <textarea v-model="completeForm.handoverNote" class="form-control" rows="4"></textarea>
                        </div>
                    </div>

                    <div class="form-actions">
                        <button class="btn btn-primary" @click="complete" :disabled="submittingComplete">{{ submittingComplete ? 'Đang lưu...' : 'Xác nhận đã trả đồ' }}</button>
                        <button class="btn btn-secondary" @click="showCompleteModal = false">Hủy</button>
                    </div>
                </div>
            </div>

            <div v-if="showEvidenceModal" class="modal-overlay" @click.self="showEvidenceModal = false">
                <div class="modal-panel wide-modal">
                    <h3>Hồ sơ đối chiếu nhận lại đồ</h3>
                    <div class="evidence-grid three">
                        <div class="evidence-card">
                            <div class="evidence-title">Người nhận</div>
                            <img v-if="evidencePreview.claimantPhotoUrl" :src="evidencePreview.claimantPhotoUrl" alt="claimant" class="photo-preview" />
                            <div v-else class="empty-state compact">Chưa có ảnh</div>
                        </div>
                        <div class="evidence-card">
                            <div class="evidence-title">Vật đối chiếu</div>
                            <img v-if="evidencePreview.itemPhotoUrl" :src="evidencePreview.itemPhotoUrl" alt="claim-item" class="photo-preview" />
                            <div v-else class="empty-state compact">Chưa có ảnh</div>
                        </div>
                        <div class="evidence-card">
                            <div class="evidence-title">Ảnh trao trả</div>
                            <img v-if="evidencePreview.returnPhotoUrl" :src="evidencePreview.returnPhotoUrl" alt="return" class="photo-preview" />
                            <div v-else class="empty-state compact">Chưa có ảnh</div>
                        </div>
                    </div>
                    <div class="evidence-meta">
                        <div><strong>Giấy tờ:</strong> {{ evidencePreview.proofDocumentUrl || '---' }}</div>
                        <div><strong>Ghi chú duyệt:</strong> {{ evidencePreview.reviewNote || '---' }}</div>
                        <div><strong>Lý do từ chối:</strong> {{ evidencePreview.rejectionReason || '---' }}</div>
                        <div><strong>Người chứng kiến:</strong> {{ evidencePreview.witnessName || '---' }}</div>
                        <div><strong>Bàn giao:</strong> {{ evidencePreview.handoverNote || '---' }}</div>
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
import { reactive, ref, onMounted } from 'vue'
import { lostFoundApi } from '../services/enterpriseSecurityApi'

const claims = ref([])
const lostItemOptions = ref([])
const filter = ref('')
const loading = ref(false)
const showEditModal = ref(false)
const showCompleteModal = ref(false)
const showEvidenceModal = ref(false)
const submittingEdit = ref(false)
const submittingComplete = ref(false)
const selectedClaim = ref(null)

const editForm = reactive({
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

const completeForm = reactive({
    claimantPhotoUrl: null,
    claimantPhotoBase64: null,
    claimantPhotoPreview: '',
    returnPhotoUrl: null,
    returnPhotoBase64: null,
    returnPhotoPreview: '',
    witnessName: '',
    handoverNote: ''
})
const evidencePreview = reactive({
    claimantPhotoUrl: '',
    itemPhotoUrl: '',
    returnPhotoUrl: '',
    proofDocumentUrl: '',
    reviewNote: '',
    rejectionReason: '',
    witnessName: '',
    handoverNote: ''
})

onMounted(async () => {
    await Promise.all([loadClaims(), loadLostItemOptions()])
})

async function loadClaims() {
    loading.value = true
    try {
        const res = await lostFoundApi.getClaimRequests({ status: filter.value || undefined })
        claims.value = res.data || []
    } catch (e) {
        console.error(e)
    } finally {
        loading.value = false
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

function canEdit(claim) {
    return claim.status === 'Pending' || claim.status === 'Rejected'
}

function canCancel(claim) {
    return claim.status === 'Pending' || claim.status === 'Rejected' || claim.status === 'Approved'
}

async function approve(claim) {
    const note = prompt(`Ghi chú duyệt cho "${claim.claimantName}" (có thể bỏ trống):`) || ''
    try {
        await lostFoundApi.approveClaimRequest(claim.claimRequestId, { note: note || null })
        await loadClaims()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    }
}

async function reject(claim) {
    const reason = prompt(`Lý do từ chối yêu cầu của "${claim.claimantName}"?`) || ''
    if (!reason.trim()) return
    try {
        await lostFoundApi.rejectClaimRequest(claim.claimRequestId, { reason })
        await loadClaims()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    }
}

async function cancelClaim(claim) {
    const confirmed = confirm(`Hủy hồ sơ nhận lại đồ của "${claim.claimantName}"?`)
    if (!confirmed) return
    try {
        await lostFoundApi.deleteClaimRequest(claim.claimRequestId)
        await loadClaims()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    }
}

function openEvidence(claim) {
    evidencePreview.claimantPhotoUrl = claim.claimantPhotoUrl || ''
    evidencePreview.itemPhotoUrl = claim.itemPhotoUrl || ''
    evidencePreview.returnPhotoUrl = claim.returnPhotoUrl || ''
    evidencePreview.proofDocumentUrl = claim.proofDocumentUrl || ''
    evidencePreview.reviewNote = claim.reviewNote || ''
    evidencePreview.rejectionReason = claim.rejectionReason || ''
    evidencePreview.witnessName = claim.witnessName || ''
    evidencePreview.handoverNote = claim.handoverNote || ''
    showEvidenceModal.value = true
}

function printReceipt(claim) {
    const printWindow = window.open('', '_blank', 'width=960,height=1200')
    if (!printWindow) {
        alert('Không mở được cửa sổ in. Vui lòng kiểm tra trình duyệt.')
        return
    }

    const completedAt = formatDateTime(claim.completedAtUtc)
    const foundAt = formatDateTime(claim.foundItem?.foundAtUtc)
    const note = escapeHtml(claim.handoverNote || '---')
    const witness = escapeHtml(claim.witnessName || '---')
    const proof = escapeHtml(claim.proofDocumentUrl || '---')
    const itemDescription = escapeHtml(claim.foundItem?.itemDescription || '---')
    const foundLocation = escapeHtml(claim.foundItem?.foundLocation || '---')
    const storageLocation = escapeHtml(claim.foundItem?.storageLocation || '---')
    const claimantName = escapeHtml(claim.claimantName || '---')
    const claimantId = escapeHtml(claim.claimantIdNumber || '---')
    const claimantPhone = escapeHtml(claim.claimantPhone || '---')
    const reviewNote = escapeHtml(claim.reviewNote || '---')

    const html = `<!doctype html>
<html lang="vi">
<head>
<meta charset="utf-8" />
<title>Biên bản trao trả đồ thất lạc</title>
<style>
  body { font-family: "Times New Roman", serif; margin: 32px; color: #111827; }
  .sheet { max-width: 860px; margin: 0 auto; }
  .center { text-align: center; }
  .title { font-size: 28px; font-weight: 700; text-transform: uppercase; margin: 20px 0 8px; }
  .subtitle { font-size: 15px; margin-bottom: 24px; }
  .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 18px; margin: 18px 0; }
  .card { border: 1px solid #cbd5e1; border-radius: 10px; padding: 14px; }
  .label { font-weight: 700; margin-bottom: 8px; }
  .photo { width: 100%; max-height: 240px; object-fit: contain; border: 1px solid #e5e7eb; border-radius: 8px; background: #f8fafc; }
  .meta { margin: 10px 0; line-height: 1.65; }
  .meta strong { display: inline-block; min-width: 180px; }
  .note { margin-top: 12px; padding: 12px; border: 1px dashed #94a3b8; border-radius: 8px; white-space: pre-wrap; }
  .signatures { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 24px; margin-top: 36px; text-align: center; }
  .sign-box { min-height: 140px; }
  .sign-title { font-weight: 700; margin-bottom: 12px; }
  .muted { color: #475569; }
  @media print { body { margin: 16px; } .sheet { max-width: none; } }
</style>
</head>
<body>
  <div class="sheet">
    <div class="center">
      <div class="title">Biên Bản Trao Trả Đồ Thất Lạc</div>
      <div class="subtitle">Lap luc: ${completedAt}</div>
    </div>

    <div class="meta">
      <div><strong>Người nhận lại:</strong> ${claimantName}</div>
      <div><strong>CCCD/CMND:</strong> ${claimantId}</div>
      <div><strong>Số điện thoại:</strong> ${claimantPhone}</div>
      <div><strong>Đồ vật:</strong> ${itemDescription}</div>
      <div><strong>Nơi tìm thấy:</strong> ${foundLocation}</div>
      <div><strong>Vị trí lưu trữ:</strong> ${storageLocation}</div>
      <div><strong>Thời điểm nhặt được:</strong> ${foundAt}</div>
      <div><strong>Giấy tờ minh chứng:</strong> ${proof}</div>
      <div><strong>Ghi chú duyệt:</strong> ${reviewNote}</div>
      <div><strong>Người chứng kiến:</strong> ${witness}</div>
    </div>

    <div class="grid">
      <div class="card">
        <div class="label">Ảnh người nhận</div>
        ${renderImage(claim.claimantPhotoUrl, 'Ảnh người nhận')}
      </div>
      <div class="card">
        <div class="label">Ảnh vật đối chiếu</div>
        ${renderImage(claim.itemPhotoUrl, 'Ảnh vật đối chiếu')}
      </div>
      <div class="card">
        <div class="label">Ảnh bàn giao</div>
        ${renderImage(claim.returnPhotoUrl, 'Ảnh bàn giao')}
      </div>
      <div class="card">
        <div class="label">Ghi chú bàn giao</div>
        <div class="note">${note}</div>
      </div>
    </div>

    <div class="signatures">
      <div class="sign-box">
        <div class="sign-title">Người giao</div>
        <div class="muted">(Ký và ghi rõ họ tên)</div>
      </div>
      <div class="sign-box">
        <div class="sign-title">Người nhận</div>
        <div class="muted">(Ký và ghi rõ họ tên)</div>
      </div>
      <div class="sign-box">
        <div class="sign-title">Người chứng kiến</div>
        <div class="muted">(Ký và ghi rõ họ tên)</div>
      </div>
    </div>
  </div>
</body>
</html>`

    printWindow.document.open()
    printWindow.document.write(html)
    printWindow.document.close()
    printWindow.focus()
    setTimeout(() => printWindow.print(), 300)
}

function openEdit(claim) {
    selectedClaim.value = claim
    editForm.claimantName = claim.claimantName || ''
    editForm.claimantIdNumber = claim.claimantIdNumber || ''
    editForm.claimantPhone = claim.claimantPhone || ''
    editForm.proofDocumentUrl = claim.proofDocumentUrl || ''
    editForm.lostItemReportId = claim.lostItemReportId || null
    editForm.claimantPhotoUrl = claim.claimantPhotoUrl || null
    editForm.claimantPhotoBase64 = null
    editForm.claimantPhotoPreview = claim.claimantPhotoUrl || ''
    editForm.itemPhotoUrl = claim.itemPhotoUrl || null
    editForm.itemPhotoBase64 = null
    editForm.itemPhotoPreview = claim.itemPhotoUrl || ''
    showEditModal.value = true
}

async function submitEdit() {
    if (!selectedClaim.value) return
    if (!editForm.claimantName.trim() || !editForm.claimantIdNumber.trim()) {
        alert('Cần nhập người nhận và CCCD/CMND.')
        return
    }
    if (!editForm.claimantPhotoBase64 && !editForm.claimantPhotoUrl) {
        alert('Cần có ảnh người nhận.')
        return
    }
    if (!editForm.itemPhotoBase64 && !editForm.itemPhotoUrl) {
        alert('Cần có ảnh vật đối chiếu.')
        return
    }

    submittingEdit.value = true
    try {
        await lostFoundApi.updateClaimRequest(selectedClaim.value.claimRequestId, {
            foundItemReportId: selectedClaim.value.foundItemReportId,
            lostItemReportId: editForm.lostItemReportId,
            claimantName: editForm.claimantName,
            claimantIdNumber: editForm.claimantIdNumber,
            claimantPhone: editForm.claimantPhone || null,
            proofDocumentUrl: editForm.proofDocumentUrl || null,
            claimantPhotoUrl: editForm.claimantPhotoBase64 ? null : editForm.claimantPhotoUrl,
            claimantPhotoBase64: editForm.claimantPhotoBase64,
            itemPhotoUrl: editForm.itemPhotoBase64 ? null : editForm.itemPhotoUrl,
            itemPhotoBase64: editForm.itemPhotoBase64
        })
        showEditModal.value = false
        await loadClaims()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    } finally {
        submittingEdit.value = false
    }
}

function openComplete(claim) {
    selectedClaim.value = claim
    completeForm.claimantPhotoUrl = claim.claimantPhotoUrl || null
    completeForm.claimantPhotoBase64 = null
    completeForm.claimantPhotoPreview = claim.claimantPhotoUrl || ''
    completeForm.returnPhotoUrl = claim.returnPhotoUrl || null
    completeForm.returnPhotoBase64 = null
    completeForm.returnPhotoPreview = claim.returnPhotoUrl || ''
    completeForm.witnessName = claim.witnessName || ''
    completeForm.handoverNote = claim.handoverNote || ''
    showCompleteModal.value = true
}

async function onEditFileChange(event, kind) {
    const file = event.target.files?.[0]
    if (!file) return
    const dataUrl = await readFileAsDataUrl(file)
    if (kind === 'claimant') {
        editForm.claimantPhotoBase64 = dataUrl
        editForm.claimantPhotoPreview = dataUrl
    } else {
        editForm.itemPhotoBase64 = dataUrl
        editForm.itemPhotoPreview = dataUrl
    }
}

async function onCompleteFileChange(event, kind) {
    const file = event.target.files?.[0]
    if (!file) return
    const dataUrl = await readFileAsDataUrl(file)
    if (kind === 'claimant') {
        completeForm.claimantPhotoBase64 = dataUrl
        completeForm.claimantPhotoPreview = dataUrl
    } else {
        completeForm.returnPhotoBase64 = dataUrl
        completeForm.returnPhotoPreview = dataUrl
    }
}

async function complete() {
    if (!selectedClaim.value) return
    if (!completeForm.handoverNote.trim()) {
        alert('Cần ghi rõ nội dung bàn giao.')
        return
    }
    if (!completeForm.returnPhotoBase64 && !completeForm.returnPhotoUrl) {
        alert('Cần có ảnh lúc trao trả.')
        return
    }

    submittingComplete.value = true
    try {
        await lostFoundApi.completeClaimRequest(selectedClaim.value.claimRequestId, {
            claimantPhotoUrl: completeForm.claimantPhotoBase64 ? null : completeForm.claimantPhotoUrl,
            claimantPhotoBase64: completeForm.claimantPhotoBase64,
            returnPhotoUrl: completeForm.returnPhotoBase64 ? null : completeForm.returnPhotoUrl,
            returnPhotoBase64: completeForm.returnPhotoBase64,
            witnessName: completeForm.witnessName || null,
            handoverNote: completeForm.handoverNote
        })
        showCompleteModal.value = false
        await loadClaims()
    } catch (e) {
        alert('Lỗi: ' + (e.response?.data?.message || e.message))
    } finally {
        submittingComplete.value = false
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

function renderImage(url, alt) {
    return url
        ? `<img src="${escapeAttribute(url)}" alt="${escapeAttribute(alt)}" class="photo" />`
        : '<div class="muted">Chưa có ảnh</div>'
}

function escapeHtml(value) {
    return String(value)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;')
}

function escapeAttribute(value) {
    return escapeHtml(value)
}

function formatDateTime(value) {
    if (!value) return '---'
    return new Date(value).toLocaleString('vi-VN')
}

function statusClass(status) {
    const map = { Pending: 'warning', Approved: 'primary', Completed: 'success', Rejected: 'danger', Cancelled: 'secondary' }
    return map[status] || 'secondary'
}

function statusLabel(status) {
    const map = { Pending: 'Chờ duyệt', Approved: 'Đã duyệt', Completed: 'Hoàn tất', Rejected: 'Từ chối', Cancelled: 'Đã hủy' }
    return map[status] || status
}
</script>

<style scoped>
.text-truncate { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.filter-row { display: flex; gap: 0.5rem; align-items: center; }
.filter-row .form-control { width: 180px; }
.loading-spinner { text-align: center; padding: 2rem; color: var(--text-secondary); }
.wide-modal { width: min(920px, calc(100vw - 2rem)); }
.photo-preview { width: 100%; max-height: 180px; object-fit: cover; border-radius: 0.75rem; margin-top: 0.75rem; border: 1px solid var(--border-color); }
.action-cell { display: flex; flex-wrap: wrap; gap: 0.5rem; }
.evidence-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; }
.evidence-grid.three { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.evidence-card { border: 1px solid var(--border-color); border-radius: 0.9rem; padding: 1rem; background: var(--surface-1, var(--surface-default)); transition: border-color var(--transition-fast), box-shadow var(--transition-fast), transform var(--transition-fast); }

.evidence-card:hover { border-color: var(--border-color-hover); box-shadow: var(--shadow-sm); transform: translateY(-1px); }
.evidence-title { font-weight: 700; margin-bottom: 0.5rem; }
.compact { padding: 1rem; }
.evidence-meta { display: grid; gap: 0.75rem; margin-top: 1rem; }
</style>
