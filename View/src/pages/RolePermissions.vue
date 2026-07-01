<template>
    <div class="page-container animate-in">
        <header class="page-header bento-header">
            <div class="greeting">
                <h1 class="page-title">Quyền Theo Vai Trò</h1>
                <p class="page-subtitle">Xem trước mỗi vai trò được vào những trang nào trước khi gán tài khoản cụ thể.</p>
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
            <div class="bento-card intro-card">
                <p class="intro-copy">
                    Vai trò là lớp quyền gốc của hệ thống. Khi gán vai trò cho tài khoản, tài khoản đó sẽ tự có các trang bên dưới.
                    Nếu cần cộng thêm hoặc rút bớt riêng cho từng người, hãy chỉnh ở trang quản lý tài khoản trong phần phạm vi công việc.
                </p>
            </div>

            <div class="role-grid">
                <section v-for="card in roleAccessCards" :key="card.role" class="bento-card role-card">
                    <div class="role-card-top">
                        <span class="badge-role" :class="getRoleBadgeClass(card.role)">{{ card.label }}</span>
                        <span class="text-muted">{{ card.tasks.length }} trang</span>
                    </div>

                    <div v-if="card.tasks.length === 0" class="empty-mini">Chưa có trang mặc định.</div>

                    <div v-else class="permission-list">
                        <article v-for="task in card.tasks" :key="task.taskKey" class="permission-item">
                            <strong>{{ task.label }}</strong>
                            <span class="route-line">{{ task.routes.join(', ') }}</span>
                        </article>
                    </div>
                </section>
            </div>

            <div class="bento-card matrix-card">
                <div class="section-head">
                    <div>
                        <h2 class="section-title">Ma Trận Vai Trò</h2>
                        <p class="section-subtitle">Đối chiếu nhanh vai trò nào đang có quyền vào từng trang.</p>
                    </div>
                </div>

                <div class="table-wrap">
                    <table class="sleek-table">
                        <thead>
                            <tr>
                                <th>Trang / chức năng</th>
                                <th>Route</th>
                                <th>Admin</th>
                                <th>Quản lý</th>
                                <th>Bảo vệ</th>
                                <th>Lễ tân</th>
                                <th>Nhân sự</th>
                                <th>Nhân viên</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="task in taskCatalog" :key="task.taskKey">
                                <td>
                                    <strong>{{ task.label }}</strong>
                                </td>
                                <td class="text-muted">{{ task.routes.join(', ') }}</td>
                                <td v-for="role in roleOrder" :key="role" class="text-center">
                                    <span class="matrix-pill" :class="task.defaultRoles.includes(role) ? 'allowed' : 'denied'">
                                        {{ task.defaultRoles.includes(role) ? 'Có' : 'Không' }}
                                    </span>
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
import { getOperationalScopeReference } from '../services/userApi'

const loading = ref(true)
const loadError = ref('')
const taskCatalog = ref([])
const tasksByRole = ref({})
const roleOrder = ['Admin', 'QuanLy', 'BaoVe', 'LeTan', 'NhanSu', 'NhanVien']

const roleMeta = [
    { role: 'Admin', label: 'Admin' },
    { role: 'QuanLy', label: 'Quản lý' },
    { role: 'BaoVe', label: 'Bảo vệ' },
    { role: 'LeTan', label: 'Lễ tân' },
    { role: 'NhanSu', label: 'Nhân sự' },
    { role: 'NhanVien', label: 'Nhân viên' },
]

const taskMap = computed(() =>
    Object.fromEntries(taskCatalog.value.map(task => [task.taskKey, task]))
)

const roleAccessCards = computed(() =>
    roleMeta.map(item => ({
        ...item,
        tasks: (tasksByRole.value?.[item.role] || []).map(taskKey => taskMap.value[taskKey]).filter(Boolean),
    }))
)

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

async function fetchReference() {
    loading.value = true
    loadError.value = ''
    try {
        const res = await getOperationalScopeReference()
        taskCatalog.value = res.data?.taskCatalog || []
        tasksByRole.value = res.data?.tasksByRole || {}
    } catch (error) {
        loadError.value = error.response?.data?.message || 'Không thể tải dữ liệu quyền theo vai trò'
    } finally {
        loading.value = false
    }
}

onMounted(fetchReference)
</script>

<style scoped>
.bento-header { margin-bottom: 24px; padding: 0 4px; display: flex; justify-content: space-between; align-items: center; }
.bento-header .greeting h1 { font-size: 1.8rem; font-weight: 700; color: var(--text-primary); }
.bento-header .greeting p { color: var(--text-secondary); font-size: 0.95rem; }
.bento-card { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: var(--border-radius-lg); padding: 24px; }
.intro-card { margin-bottom: 24px; }
.intro-copy { margin: 0; color: var(--text-secondary); line-height: 1.65; }
.role-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 16px; margin-bottom: 24px; }
.role-card-top { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: 12px; }
.permission-list { display: flex; flex-direction: column; gap: 10px; }
.permission-item { display: flex; flex-direction: column; gap: 4px; padding: 12px; border-radius: 12px; background: var(--bg-input); }
.route-line { color: var(--text-muted); font-size: 0.88rem; }
.empty-mini { color: var(--text-muted); font-size: 0.9rem; }
.matrix-card { overflow: hidden; }
.section-head { margin-bottom: 16px; }
.section-title { margin: 0; font-size: 1.2rem; color: var(--text-primary); }
.section-subtitle { margin: 6px 0 0; color: var(--text-secondary); font-size: 0.92rem; }
.table-wrap { overflow-x: auto; }
.text-center { text-align: center; }
.text-muted { color: var(--text-muted); }
.error-text { color: var(--accent-danger); }
.matrix-pill { display: inline-flex; align-items: center; justify-content: center; min-width: 44px; padding: 6px 10px; border-radius: 999px; font-size: 0.82rem; font-weight: 600; }
.matrix-pill.allowed { background: rgba(16, 185, 129, 0.12); color: var(--accent-success); }
.matrix-pill.denied { background: rgba(239, 68, 68, 0.1); color: var(--accent-danger); }
@media (max-width: 768px) {
    .bento-card { padding: 18px; }
}
</style>
