<template>
    <div class="page-container animate-in">
        <header class="page-header bento-header">
            <div class="greeting">
                <h1 class="page-title">Chuyển nhượng xe</h1>
                <p class="page-subtitle">Ủy quyền xe cho nhân viên khác</p>
            </div>
        </header>

        <div class="bento-tabs" style="display: flex; gap: 4px; background: var(--bg-surface); padding: 4px; border-radius: 14px; margin-bottom: 20px; max-width: 500px;">
            <button v-for="tab in tabs" :key="tab.key" class="tab-btn" :class="{ active: activeTab === tab.key }" @click="activeTab = tab.key">
                {{ tab.label }}
            </button>
        </div>

        <!-- Tab: Ủy quyền xe -->
        <div v-if="activeTab === 'grant'" class="bento-card">
            <h3 style="margin: 0 0 16px;">Ủy quyền xe của bạn</h3>
            <div v-if="myVehicles.length === 0" class="empty-layout">
                <p>Bạn không có xe nào trong bãi để ủy quyền.</p>
            </div>
            <form v-else @submit.prevent="submitDelegation" class="modal-form-grid">
                <div class="input-pane">
                    <label>Chọn xe</label>
                    <select v-model="delegationForm.vehicleId" class="sleek-select" required>
                        <option value="" disabled>-- Chọn xe --</option>
                        <option v-for="v in myVehicles" :key="v.vehicleId" :value="v.vehicleId">
                            {{ v.licensePlate }} {{ v.description ? '- ' + v.description : '' }}
                        </option>
                    </select>
                </div>
                <div class="input-pane">
                    <label>Nhân viên nhận xe</label>
                    <div class="combo-box-wrapper">
                        <input v-model="employeeSearch" type="text" class="sleek-input" placeholder="Tìm theo tên..." @input="searchEmployees" @focus="showEmpDropdown = true" />
                        <transition name="dropdown">
                            <div v-if="showEmpDropdown && employeeResults.length > 0" class="combo-dropdown">
                                <div v-for="emp in employeeResults" :key="emp.employeeId" class="combo-option" @mousedown.prevent="selectEmployee(emp)">
                                    <span>{{ emp.fullName }} <span class="text-muted">({{ emp.departmentName || '—' }})</span></span>
                                </div>
                            </div>
                        </transition>
                    </div>
                    <div v-if="selectedEmployee" class="selected-employee">
                        Đã chọn: <strong>{{ selectedEmployee.fullName }}</strong>
                    </div>
                </div>
                <div class="input-pane">
                    <label>Lý do</label>
                    <textarea v-model="delegationForm.reason" class="sleek-input" rows="2" placeholder="Lý do ủy quyền..."></textarea>
                </div>
                <div v-if="grantError" class="error-box"><span>{{ grantError }}</span></div>
                <div v-if="grantSuccess" class="success-box"><span>{{ grantSuccess }}</span></div>
                <button type="submit" class="btn btn-primary" :disabled="grantSaving">
                    <span v-if="grantSaving" class="spinner-sm"></span> Gửi yêu cầu
                </button>
            </form>
        </div>

        <!-- Tab: Yêu cầu đến -->
        <div v-if="activeTab === 'incoming'" class="bento-card">
            <h3 style="margin: 0 0 16px;">Yêu cầu ủy quyền đến bạn</h3>
            <div v-if="incomingLoading" class="empty-layout"><div class="spinner-lg"></div></div>
            <div v-else-if="incomingList.length === 0" class="empty-layout"><p>Không có yêu cầu nào.</p></div>
            <div v-else class="sleek-table-container">
                <table class="sleek-table">
                    <thead><tr><th>Người gửi</th><th>Biển số</th><th>Lý do</th><th>Ngày gửi</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
                    <tbody>
                        <tr v-for="d in incomingList" :key="d.vehicleDelegationId" class="table-row">
                            <td>{{ d.fromEmployeeName }}</td>
                            <td><strong>{{ d.licensePlate }}</strong></td>
                            <td class="text-muted">{{ d.reason || '—' }}</td>
                            <td class="text-muted">{{ formatDate(d.requestedAtUtc) }}</td>
                            <td>{{ statusLabel(d.status) }}</td>
                            <td>
                                <div v-if="d.status === 'Pending'" style="display: flex; gap: 8px;">
                                    <button class="btn btn-sm btn-primary" @click="doApprove(d.vehicleDelegationId)">Đồng ý</button>
                                    <button class="btn btn-sm btn-danger" @click="doReject(d.vehicleDelegationId)">Từ chối</button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- Tab: Yêu cầu đi -->
        <div v-if="activeTab === 'outgoing'" class="bento-card">
            <h3 style="margin: 0 0 16px;">Yêu cầu bạn đã gửi</h3>
            <div v-if="outgoingLoading" class="empty-layout"><div class="spinner-lg"></div></div>
            <div v-else-if="outgoingList.length === 0" class="empty-layout"><p>Bạn chưa gửi yêu cầu nào.</p></div>
            <div v-else class="sleek-table-container">
                <table class="sleek-table">
                    <thead><tr><th>Người nhận</th><th>Biển số</th><th>Lý do</th><th>Ngày gửi</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
                    <tbody>
                        <tr v-for="d in outgoingList" :key="d.vehicleDelegationId" class="table-row">
                            <td>{{ d.toEmployeeName }}</td>
                            <td><strong>{{ d.licensePlate }}</strong></td>
                            <td class="text-muted">{{ d.reason || '—' }}</td>
                            <td class="text-muted">{{ formatDate(d.requestedAtUtc) }}</td>
                            <td>{{ statusLabel(d.status) }}</td>
                            <td>
                                <button v-if="d.status === 'Pending'" class="btn btn-sm btn-secondary" @click="doRevoke(d.vehicleDelegationId)">Hủy</button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { authState } from '../stores/auth'
import { getByEmployeeId } from '../services/vehicleApi'
import { getAll as getAllEmployees } from '../services/employeeApi'
import { createDelegation, getOutgoing, getIncoming, approveDelegation, rejectDelegation, revokeDelegation } from '../services/vehicleDelegationApi'

const tabs = [
    { key: 'grant', label: 'Ủy quyền xe' },
    { key: 'incoming', label: 'Yêu cầu đến' },
    { key: 'outgoing', label: 'Yêu cầu đi' },
]
const activeTab = ref('grant')

const myVehicles = ref([])
const delegationForm = ref({ vehicleId: '', reason: '' })
const employeeSearch = ref('')
const employeeResults = ref([])
const showEmpDropdown = ref(false)
const selectedEmployee = ref(null)
const grantError = ref('')
const grantSuccess = ref('')
const grantSaving = ref(false)

const incomingList = ref([])
const incomingLoading = ref(false)
const outgoingList = ref([])
const outgoingLoading = ref(false)

let empSearchTimer = null

function formatDate(d) {
    if (!d) return '—'
    return new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function statusLabel(s) {
    const map = { Pending: 'Chờ duyệt', Approved: 'Đã duyệt', Rejected: 'Đã từ chối', Revoked: 'Đã hủy' }
    return map[s] || s
}

function selectEmployee(emp) {
    selectedEmployee.value = emp
    employeeSearch.value = emp.fullName
    showEmpDropdown.value = false
}

async function searchEmployees() {
    if (empSearchTimer) clearTimeout(empSearchTimer)
    if (!employeeSearch.value.trim()) { employeeResults.value = []; return }
    empSearchTimer = setTimeout(async () => {
        try {
            const res = await getAllEmployees({ search: employeeSearch.value.trim() })
            employeeResults.value = (res.data || []).filter(e => e.employeeId !== authState.user?.employeeId)
        } catch { employeeResults.value = [] }
    }, 300)
}

async function submitDelegation() {
    if (!selectedEmployee.value) { grantError.value = 'Vui lòng chọn nhân viên nhận xe.'; return }
    grantError.value = ''
    grantSuccess.value = ''
    grantSaving.value = true
    try {
        await createDelegation({
            vehicleId: parseInt(delegationForm.value.vehicleId),
            toEmployeeId: selectedEmployee.value.employeeId,
            reason: delegationForm.value.reason || null,
        })
        grantSuccess.value = 'Đã gửi yêu cầu ủy quyền.'
        delegationForm.value = { vehicleId: '', reason: '' }
        selectedEmployee.value = null
        employeeSearch.value = ''
        await loadVehicles()
        await loadOutgoing()
    } catch (e) {
        grantError.value = e.response?.data?.message || 'Không thể gửi yêu cầu.'
    } finally {
        grantSaving.value = false
    }
}

async function loadVehicles() {
    try {
        const res = await getByEmployeeId(authState.user?.employeeId)
        myVehicles.value = (res.data || []).filter(v => v.parkingStatus === 'IN')
    } catch { myVehicles.value = [] }
}

async function loadIncoming() {
    incomingLoading.value = true
    try {
        const res = await getIncoming()
        incomingList.value = res.data || []
    } catch { incomingList.value = [] }
    finally { incomingLoading.value = false }
}

async function loadOutgoing() {
    outgoingLoading.value = true
    try {
        const res = await getOutgoing()
        outgoingList.value = res.data || []
    } catch { outgoingList.value = [] }
    finally { outgoingLoading.value = false }
}

async function doApprove(id) {
    try {
        await approveDelegation(id)
        await loadIncoming()
        await loadOutgoing()
    } catch (e) {
        alert(e.response?.data?.message || 'Lỗi khi duyệt.')
    }
}

async function doReject(id) {
    if (!confirm('Từ chối yêu cầu này?')) return
    try {
        await rejectDelegation(id, { reason: null })
        await loadIncoming()
    } catch (e) {
        alert(e.response?.data?.message || 'Lỗi khi từ chối.')
    }
}

async function doRevoke(id) {
    if (!confirm('Hủy yêu cầu này?')) return
    try {
        await revokeDelegation(id)
        await loadOutgoing()
    } catch (e) {
        alert(e.response?.data?.message || 'Lỗi khi hủy.')
    }
}

onMounted(() => {
    loadVehicles()
    loadIncoming()
    loadOutgoing()
})
</script>

<style scoped>
.bento-tabs .tab-btn {
    flex: 1; padding: 10px 16px; border-radius: 12px; border: none; background: transparent;
    color: var(--text-secondary); font-size: 0.9rem; font-weight: 500; cursor: pointer; transition: all 0.2s;
}
.bento-tabs .tab-btn.active {
    background: var(--bg-surface-raised); color: var(--text-primary); box-shadow: 0 2px 8px rgba(0,0,0,0.12);
}
.bento-tabs .tab-btn:hover { color: var(--text-primary); }
.combo-box-wrapper { position: relative; }
.combo-dropdown {
    position: absolute; top: 100%; left: 0; right: 0; z-index: 10;
    background: var(--bg-surface-raised); border: 1px solid var(--border-color);
    border-radius: 12px; box-shadow: 0 8px 24px rgba(0,0,0,0.15); max-height: 200px; overflow-y: auto;
}
.combo-option { padding: 10px 14px; cursor: pointer; font-size: 0.9rem; }
.combo-option:hover { background: rgba(84,196,211,0.08); }
.selected-employee { margin-top: 8px; font-size: 0.85rem; color: var(--text-secondary); }
.success-box { background: rgba(34,197,94,0.1); border: 1px solid rgba(34,197,94,0.3); border-radius: 10px; padding: 10px 14px; color: #22c55e; font-size: 0.85rem; }
.btn-sm { padding: 6px 14px; font-size: 0.8rem; border-radius: 8px; }
</style>
