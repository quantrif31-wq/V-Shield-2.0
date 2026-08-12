<template>
    <div class="page-container animate-in">
        <header class="page-header bento-header">
            <div class="greeting">
                <h1 class="page-title">Quyền theo vai trò</h1>
                <p class="page-subtitle">Bật/tắt quyền truy cập mặc định cho từng vai trò ngay trên ma trận bên dưới.</p>
            </div>
            <div v-if="!loading && !loadError" class="header-actions">
                <button class="btn btn-secondary" @click="reloadDraft" :disabled="saving">
                    Hoàn tác
                </button>
                <button class="btn btn-secondary danger-soft" @click="resetToDefaults" :disabled="saving">
                    Khôi phục mặc định
                </button>
                <button class="btn btn-primary" @click="savePermissions" :disabled="saving">
                    <span v-if="saving" class="spinner-sm"></span>
                    Lưu thay đổi
                </button>
            </div>
        </header>

        <div v-if="loading" class="bento-card empty-layout">
            <div class="spinner-lg"></div>
            <p>Đang tải ma trận quyền...</p>
        </div>

        <div v-else-if="loadError" class="bento-card empty-layout">
            <p class="error-text">{{ loadError }}</p>
            <button class="btn btn-primary" @click="fetchReference">Thử lại</button>
        </div>

        <template v-else>
            <div class="bento-tabs" style="display: flex; gap: 4px; background: var(--bg-surface); padding: 4px; border-radius: 14px; margin-bottom: 20px; max-width: 560px;">
                <button type="button" class="tab-btn" :class="{ active: activeTab === 'tasks' }" @click="switchTab('tasks')">Trang / chức năng</button>
                <button type="button" class="tab-btn" :class="{ active: activeTab === 'gates' }" @click="switchTab('gates')">Cổng</button>
            </div>

            <template v-if="activeTab === 'tasks'">
                <div class="bento-card intro-card">
                    <p class="intro-copy">
                        Mỗi ô trong bảng là quyền mặc định của một vai trò đối với một nhóm trang/chức năng. Tick để cấp quyền, bỏ tick để thu hồi quyền.
                        Sau khi lưu, menu và route của những tài khoản đang mang vai trò đó sẽ đi theo ma trận mới.
                    </p>
                </div>

                <div v-if="feedbackMessage" class="bento-card feedback-card" :class="feedbackTone">
                    {{ feedbackMessage }}
                </div>

                <div class="bento-card matrix-card">
                    <div class="section-head">
                        <div>
                            <h2 class="section-title">Ma trận vai trò</h2>
                            <p class="section-subtitle">Tick bật/tắt quyền mặc định theo vai trò, sau đó bấm lưu để áp dụng.</p>
                        </div>
                    </div>

                    <div class="table-wrap">
                        <table class="sleek-table">
                            <thead>
                                <tr>
                                    <th>Trang / chức năng</th>
                                    <th>Đường dẫn</th>
                                    <th v-for="role in roleOrder" :key="role">{{ getRoleLabel(role) }}</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="task in taskCatalog" :key="task.taskKey">
                                    <td>
                                        <strong>{{ task.label }}</strong>
                                    </td>
                                    <td class="text-muted">{{ task.routes.join(', ') }}</td>
                                    <td v-for="role in roleOrder" :key="role" class="text-center">
                                        <label class="matrix-toggle">
                                            <input
                                                v-model="draftPermissions[role][task.taskKey]"
                                                type="checkbox"
                                                :disabled="saving"
                                                @change="clearFeedback"
                                            />
                                            <span class="matrix-pill" :class="draftPermissions[role][task.taskKey] ? 'allowed' : 'denied'">
                                                {{ draftPermissions[role][task.taskKey] ? 'Có' : 'Không' }}
                                            </span>
                                        </label>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </template>

            <template v-else>
                <div class="bento-card intro-card">
                    <p class="intro-copy">
                        Mỗi ô là quyền mặc định của một vai trò đối với một cổng truy cập. Tick để vai trò đó được qua cổng, bỏ tick để chặn.
                        Tài khoản thuộc vai trò sẽ kế thừa quyền này, trừ khi được gán quyền riêng ở màn hình quản lý tài khoản.
                        Quyền này được áp dụng ngay tại cổng khi quét QR vào cổng.
                    </p>
                </div>

                <div v-if="feedbackMessage" class="bento-card feedback-card" :class="feedbackTone">
                    {{ feedbackMessage }}
                </div>

                <div v-if="gateLoading" class="bento-card empty-layout">
                    <div class="spinner-lg"></div>
                    <p>Đang tải quyền qua cổng...</p>
                </div>
                <div v-else-if="gateError" class="bento-card empty-layout">
                    <p class="error-text">{{ gateError }}</p>
                    <button class="btn btn-primary" @click="fetchGateReference">Thử lại</button>
                </div>
                <div v-else class="bento-card matrix-card">
                    <div class="section-head">
                        <div>
                            <h2 class="section-title">Ma trận cổng theo vai trò</h2>
                            <p class="section-subtitle">Tick bật/tắt quyền qua từng cổng theo vai trò, sau đó bấm lưu để áp dụng.</p>
                        </div>
                    </div>

                    <div v-if="gateGates.length === 0" class="empty-layout">
                        <p>Chưa có cổng nào. Hãy tạo cổng trước.</p>
                    </div>
                    <div v-else class="table-wrap">
                        <table class="sleek-table">
                            <thead>
                                <tr>
                                    <th>Cổng</th>
                                    <th>Vị trí</th>
                                    <th v-for="role in roleOrder" :key="role">{{ getRoleLabel(role) }}</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="gate in gateGates" :key="gate.gateId">
                                    <td><strong>{{ gate.gateName }}</strong></td>
                                    <td class="text-muted">{{ gate.location || '-' }}</td>
                                    <td v-for="role in roleOrder" :key="role" class="text-center">
                                        <label class="matrix-toggle">
                                            <input
                                                v-model="gateDraftPermissions[role][gate.gateId]"
                                                type="checkbox"
                                                :disabled="saving"
                                                @change="clearFeedback"
                                            />
                                            <span class="matrix-pill" :class="gateDraftPermissions[role][gate.gateId] ? 'allowed' : 'denied'">
                                                {{ gateDraftPermissions[role][gate.gateId] ? 'Có' : 'Không' }}
                                            </span>
                                        </label>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </template>
        </template>

        <StepUpModal
            :visible="stepUpVisible"
            action-label="Cập nhật quyền theo vai trò"
            action="UserAdministration"
            :action-description="pendingAction === 'save' ? 'Lưu ma trận quyền mặc định theo vai trò' : 'Khôi phục ma trận quyền mặc định hệ thống'"
            severity="high"
            @cancel="onStepUpCancelled"
            @confirmed="onStepUpConfirmed"
        />
    </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { fetchUser } from '../stores/auth'
import { getOperationalScopeReference, replaceRolePermissions, getGateAccessReference, replaceRoleGatePermissions } from '../services/userApi'
import { enterpriseApi } from '../services/enterpriseSecurityApi'
import StepUpModal from '../components/shared/StepUpModal.vue'

const loading = ref(true)
const saving = ref(false)
const loadError = ref('')
const feedbackMessage = ref('')
const feedbackTone = ref('success')
const taskCatalog = ref([])
const tasksByRole = ref({})
const draftPermissions = ref({})
const activeTab = ref('tasks')
const gateLoading = ref(false)
const gateError = ref('')
const gateLoaded = ref(false)
const gateGates = ref([])
const gatesByRole = ref({})
const gateDraftPermissions = ref({})
const roleOrder = ['Admin', 'QuanLy', 'BaoVe', 'LeTan', 'NhanSu', 'NhanVien']
const stepUpVisible = ref(false)
const pendingAction = ref('save')

const roleMeta = [
    { role: 'Admin', label: 'Admin' },
    { role: 'QuanLy', label: 'Quản lý' },
    { role: 'BaoVe', label: 'Bảo vệ' },
    { role: 'LeTan', label: 'Lễ tân' },
    { role: 'NhanSu', label: 'Nhân sự' },
    { role: 'NhanVien', label: 'Nhân viên' },
]

function createDraftPermissions(sourceTasksByRole) {
    return roleOrder.reduce((acc, role) => {
        const allowed = new Set(sourceTasksByRole?.[role] || [])
        acc[role] = taskCatalog.value.reduce((taskAcc, task) => {
            taskAcc[task.taskKey] = allowed.has(task.taskKey)
            return taskAcc
        }, {})
        return acc
    }, {})
}

function getRoleBadgeClass(role) {
    const map = {
        Admin: 'admin',
        QuanLy: 'manager',
        BaoVe: 'guard',
        LeTan: 'reception',
        NhanSu: 'staff',
        NhanVien: 'staff',
    }
    return map[role] || 'staff'
}

function getRoleLabel(role) {
    return roleMeta.find(item => item.role === role)?.label || role
}

function clearFeedback() {
    feedbackMessage.value = ''
}

function switchTab(tab) {
    activeTab.value = tab
    clearFeedback()
    if (tab === 'gates' && !gateLoaded.value && !gateLoading.value) {
        fetchGateReference()
    }
}

function reloadDraft() {
    if (activeTab.value === 'tasks') {
        draftPermissions.value = createDraftPermissions(tasksByRole.value)
    } else {
        gateDraftPermissions.value = createDraftGatePermissions(gatesByRole.value)
    }
    clearFeedback()
}

async function fetchReference() {
    loading.value = true
    loadError.value = ''
    feedbackMessage.value = ''
    try {
        const res = await getOperationalScopeReference()
        taskCatalog.value = res.data?.taskCatalog || []
        tasksByRole.value = res.data?.tasksByRole || {}
        draftPermissions.value = createDraftPermissions(tasksByRole.value)
    } catch (error) {
        loadError.value = error.response?.data?.message || 'Không thể tải dữ liệu quyền theo vai trò'
    } finally {
        loading.value = false
    }
}

function createDraftGatePermissions(sourceGatesByRole) {
    return roleOrder.reduce((acc, role) => {
        const allowed = new Set(sourceGatesByRole?.[role] || [])
        acc[role] = gateGates.value.reduce((gateAcc, gate) => {
            gateAcc[gate.gateId] = allowed.has(gate.gateId)
            return gateAcc
        }, {})
        return acc
    }, {})
}

async function fetchGateReference() {
    gateLoading.value = true
    gateError.value = ''
    try {
        const res = await getGateAccessReference()
        gateGates.value = res.data?.gates || []
        gatesByRole.value = res.data?.gatesByRole || {}
        gateDraftPermissions.value = createDraftGatePermissions(gatesByRole.value)
        gateLoaded.value = true
    } catch (error) {
        gateError.value = error.response?.data?.message || 'Không thể tải dữ liệu quyền qua cổng'
    } finally {
        gateLoading.value = false
    }
}

async function savePermissions() {
    pendingAction.value = 'save'
    stepUpVisible.value = true
}

function onStepUpCancelled() {
    stepUpVisible.value = false
}

async function onStepUpConfirmed(result) {
    stepUpVisible.value = false
    if (result?.sessionId) {
        enterpriseApi.setStepUpSession(result.sessionId)
    }
    try {
        if (pendingAction.value === 'save') {
            await performSavePermissions()
        } else {
            await performResetPermissions()
        }
    } finally {
        enterpriseApi.setStepUpSession(null)
    }
}

async function performSavePermissions() {
    if (activeTab.value === 'gates') return performSaveGatePermissions()

    saving.value = true
    feedbackMessage.value = ''
    try {
        const payload = roleOrder.flatMap(role =>
            taskCatalog.value.map(task => ({
                role,
                taskKey: task.taskKey,
                isAllowed: !!draftPermissions.value?.[role]?.[task.taskKey],
            }))
        )

        await replaceRolePermissions(payload)
        await fetchReference()
        await fetchUser()
        feedbackTone.value = 'success'
        feedbackMessage.value = 'Đã lưu ma trận quyền theo vai trò.'
    } catch (error) {
        feedbackTone.value = 'error'
        feedbackMessage.value = error.response?.data?.message || 'Không thể lưu ma trận quyền'
    } finally {
        saving.value = false
    }
}

async function performSaveGatePermissions() {
    saving.value = true
    feedbackMessage.value = ''
    try {
        const payload = roleOrder.flatMap(role =>
            gateGates.value.map(gate => ({
                role,
                gateId: gate.gateId,
                isAllowed: !!gateDraftPermissions.value?.[role]?.[gate.gateId],
            }))
        )

        await replaceRoleGatePermissions(payload)
        await fetchGateReference()
        feedbackTone.value = 'success'
        feedbackMessage.value = 'Đã lưu ma trận quyền qua cổng theo vai trò.'
    } catch (error) {
        feedbackTone.value = 'error'
        feedbackMessage.value = error.response?.data?.message || 'Không thể lưu ma trận quyền qua cổng'
    } finally {
        saving.value = false
    }
}

async function resetToDefaults() {
    if (activeTab.value === 'gates') {
        const confirmed = window.confirm('Khôi phục toàn bộ ma trận quyền qua cổng về mặc định hệ thống?')
        if (!confirmed) return
    } else {
        const confirmed = window.confirm('Khôi phục toàn bộ ma trận quyền về mặc định hệ thống?')
        if (!confirmed) return
    }

    pendingAction.value = 'reset'
    stepUpVisible.value = true
}

async function performResetPermissions() {
    if (activeTab.value === 'gates') return performResetGateDefaults()

    saving.value = true
    feedbackMessage.value = ''
    try {
        await replaceRolePermissions([])
        await fetchReference()
        await fetchUser()
        feedbackTone.value = 'success'
        feedbackMessage.value = 'Đã khôi phục ma trận quyền mặc định.'
    } catch (error) {
        feedbackTone.value = 'error'
        feedbackMessage.value = error.response?.data?.message || 'Không thể khôi phục ma trận mặc định'
    } finally {
        saving.value = false
    }
}

async function performResetGateDefaults() {
    saving.value = true
    feedbackMessage.value = ''
    try {
        await replaceRoleGatePermissions([])
        await fetchGateReference()
        feedbackTone.value = 'success'
        feedbackMessage.value = 'Đã khôi phục ma trận quyền qua cổng mặc định.'
    } catch (error) {
        feedbackTone.value = 'error'
        feedbackMessage.value = error.response?.data?.message || 'Không thể khôi phục ma trận mặc định'
    } finally {
        saving.value = false
    }
}

onMounted(fetchReference)
</script>

<style scoped>
.bento-header { margin-bottom: 24px; padding: 0 4px; display: flex; justify-content: space-between; align-items: center; gap: 16px; }
.bento-header .greeting h1 { font-size: 1.8rem; font-weight: 700; color: var(--text-primary); }
.bento-header .greeting p { color: var(--text-secondary); font-size: 0.95rem; }
.header-actions { display: flex; gap: 12px; flex-wrap: wrap; }
.bento-card { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius-lg); padding: 24px; }
.intro-card, .feedback-card { margin-bottom: 24px; }
.intro-copy { margin: 0; color: var(--text-secondary); line-height: 1.65; }
.feedback-card.success { border-color: rgba(16, 185, 129, 0.28); color: var(--accent-success); }
.feedback-card.error { border-color: rgba(239, 68, 68, 0.28); color: var(--accent-danger); }
.matrix-card { overflow: hidden; }
.section-head { margin-bottom: 16px; }
.section-title { margin: 0; font-size: 1.2rem; color: var(--text-primary); }
.section-subtitle { margin: 6px 0 0; color: var(--text-secondary); font-size: 0.92rem; }
.table-wrap { overflow-x: auto; }
.text-center { text-align: center; }
.text-muted { color: var(--text-muted); }
.error-text { color: var(--accent-danger); }
.matrix-toggle { display: inline-flex; align-items: center; justify-content: center; cursor: pointer; }
.matrix-toggle input { position: absolute; opacity: 0; pointer-events: none; }
.matrix-pill { display: inline-flex; align-items: center; justify-content: center; min-width: 52px; padding: 6px 10px; border-radius: 999px; font-size: 0.82rem; font-weight: 600; border: 1px solid transparent; transition: transform 0.15s ease, box-shadow 0.15s ease; }
.matrix-toggle:hover .matrix-pill { transform: translateY(-1px); box-shadow: var(--shadow-sm); }
.matrix-pill.allowed { background: rgba(16, 185, 129, 0.12); color: var(--accent-success); border-color: rgba(16, 185, 129, 0.2); }
.matrix-pill.denied { background: rgba(239, 68, 68, 0.1); color: var(--accent-danger); border-color: rgba(239, 68, 68, 0.18); }
.danger-soft { color: var(--accent-danger); }
.bento-tabs .tab-btn {
    flex: 1; padding: 10px 16px; border-radius: 12px; border: none; background: transparent;
    color: var(--text-secondary); font-size: 0.9rem; font-weight: 500; cursor: pointer; transition: all 0.2s;
}
.bento-tabs .tab-btn.active {
    background: var(--bg-surface-raised); color: var(--text-primary); box-shadow: 0 2px 8px rgba(0,0,0,0.12);
}
.bento-tabs .tab-btn:hover { color: var(--text-primary); }
.spinner-lg { width: 36px; height: 36px; border: 3px solid var(--border-color); border-top-color: var(--accent-primary); border-radius: 50%; animation: spin 0.8s linear infinite; }
.spinner-sm { width: 16px; height: 16px; border: 2px solid rgba(255,255,255,0.3); border-top-color: #fff; border-radius: 50%; animation: spin 0.6s linear infinite; display: inline-block; margin-right: 6px; }
.empty-layout { padding: 60px; text-align: center; color: var(--text-muted); display: flex; flex-direction: column; align-items: center; gap: 16px; }
@keyframes spin { to { transform: rotate(360deg); } }
@media (max-width: 900px) {
    .bento-header { align-items: flex-start; flex-direction: column; }
}
@media (max-width: 768px) {
    .bento-card { padding: 18px; }
    .header-actions { width: 100%; }
}
</style>
