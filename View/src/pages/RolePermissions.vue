<template>
    <div class="page-container animate-in">
        <header class="page-header bento-header">
            <div class="greeting">
                <h1 class="page-title">Quyen Theo Vai Tro</h1>
                <p class="page-subtitle">Bat/tat quyen truy cap mac dinh cho tung vai tro ngay tren ma tran ben duoi.</p>
            </div>
            <div v-if="!loading && !loadError" class="header-actions">
                <button class="btn btn-secondary" @click="reloadDraft" :disabled="saving">
                    Hoan tac
                </button>
                <button class="btn btn-secondary danger-soft" @click="resetToDefaults" :disabled="saving">
                    Khoi phuc mac dinh
                </button>
                <button class="btn btn-primary" @click="savePermissions" :disabled="saving">
                    <span v-if="saving" class="spinner-sm"></span>
                    Luu thay doi
                </button>
            </div>
        </header>

        <div v-if="loading" class="bento-card empty-layout">
            <div class="spinner-lg"></div>
            <p>Đang tải ma trận quyền...</p>
        </div>

        <div v-else-if="loadError" class="bento-card empty-layout">
            <p class="error-text">{{ loadError }}</p>
            <button class="btn btn-primary" @click="fetchReference">Thu lai</button>
        </div>

        <template v-else>
            <div class="bento-card intro-card">
                <p class="intro-copy">
                    Moi o trong bang la quyen mac dinh cua mot vai tro doi voi mot nhom trang/chuc nang. Tick de cap quyen, bo tick de thu hoi quyen.
                    Sau khi lưu, menu và route của những tài khoản đang mang vai trò đó sẽ đi theo ma trận mới.
                </p>
            </div>

            <div v-if="feedbackMessage" class="bento-card feedback-card" :class="feedbackTone">
                {{ feedbackMessage }}
            </div>

            <div class="bento-card matrix-card">
                <div class="section-head">
                    <div>
                        <h2 class="section-title">Ma Tran Vai Tro</h2>
                        <p class="section-subtitle">Tick bat/tat quyen mac dinh theo vai tro, sau do bam luu de ap dung.</p>
                    </div>
                </div>

                <div class="table-wrap">
                    <table class="sleek-table">
                        <thead>
                            <tr>
                                <th>Trang / chuc nang</th>
                                <th>Route</th>
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
    </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { fetchUser } from '../stores/auth'
import { getOperationalScopeReference, replaceRolePermissions } from '../services/userApi'

const loading = ref(true)
const saving = ref(false)
const loadError = ref('')
const feedbackMessage = ref('')
const feedbackTone = ref('success')
const taskCatalog = ref([])
const tasksByRole = ref({})
const draftPermissions = ref({})
const roleOrder = ['Admin', 'QuanLy', 'BaoVe', 'LeTan', 'NhanSu', 'NhanVien']

const roleMeta = [
    { role: 'Admin', label: 'Admin' },
    { role: 'QuanLy', label: 'Quan ly' },
    { role: 'BaoVe', label: 'Bao ve' },
    { role: 'LeTan', label: 'Le tan' },
    { role: 'NhanSu', label: 'Nhan su' },
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

function reloadDraft() {
    draftPermissions.value = createDraftPermissions(tasksByRole.value)
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

async function savePermissions() {
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
        feedbackMessage.value = 'Da luu ma tran quyen theo vai tro.'
    } catch (error) {
        feedbackTone.value = 'error'
        feedbackMessage.value = error.response?.data?.message || 'Không thể lưu ma trận quyền'
    } finally {
        saving.value = false
    }
}

async function resetToDefaults() {
    const confirmed = window.confirm('Khôi phục toàn bộ ma trận quyền về mặc định hệ thống?')
    if (!confirmed) return

    saving.value = true
    feedbackMessage.value = ''
    try {
        await replaceRolePermissions([])
        await fetchReference()
        await fetchUser()
        feedbackTone.value = 'success'
        feedbackMessage.value = 'Da khoi phuc ma tran quyen mac dinh.'
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
