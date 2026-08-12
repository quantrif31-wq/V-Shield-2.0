<template>
    <div class="page-container identity-management animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Danh tính doanh nghiệp</span>
                <h1 class="page-title">Quản lý danh tính</h1>
            </div>
            <div class="header-actions">
                <button type="button" class="btn btn-sm btn-warning" @click="showOffboard = true">Ngừng cấp phát nhân viên</button>
                <button type="button" class="btn btn-primary" :disabled="loading" @click="loadAll">Làm mới</button>
            </div>
        </div>

        <section class="metric-grid">
            <article class="metric-tile">
                <span class="metric-label">Nhà cung cấp IdP</span>
                <strong class="metric-value">{{ overview.providers }}</strong>
                <span class="metric-note">{{ overview.enabledProviders }} đang bật</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Ánh xạ danh tính</span>
                <strong class="metric-value">{{ overview.mappings }}</strong>
                <span class="metric-note">{{ overview.activeMappings }} đang hoạt động</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Nhân viên đang hoạt động</span>
                <strong class="metric-value">{{ overview.activeEmployees }}</strong>
                <span class="metric-note">{{ overview.suspendedEmployees }} bị tạm khóa</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Đã thôi việc</span>
                <strong class="metric-value">{{ overview.terminatedEmployees }}</strong>
                <span class="metric-note">{{ overview.recertificationCampaigns }} chiến dịch</span>
            </article>
        </section>

        <section class="workspace-tabs">
            <button type="button" :class="{ active: tab === 'providers' }" @click="tab = 'providers'">Nhà cung cấp danh tính</button>
            <button type="button" :class="{ active: tab === 'import' }" @click="tab = 'import'">Nhập người dùng / Nhóm</button>
            <button type="button" :class="{ active: tab === 'mappings' }" @click="tab = 'mappings'; loadMappings()">Ánh xạ</button>
        </section>

        <section v-if="tab === 'providers'" class="soc-section">
            <div class="section-toolbar">
                <h2>Nhà cung cấp danh tính bên ngoài</h2>
                <button type="button" class="btn btn-primary btn-sm" @click="openAddProvider">Thêm nhà cung cấp</button>
            </div>
            <div v-if="loading" class="empty-card">Đang tải nhà cung cấp...</div>
            <div v-else-if="!providers.length" class="empty-card">Chưa có nhà cung cấp danh tính nào được cấu hình.</div>
            <div v-else class="provider-list">
                <div v-for="p in providers" :key="p.externalIdentityProviderId" class="provider-row" @click="editProvider(p)">
                    <div class="provider-info">
                        <strong>{{ p.name }}</strong>
                        <span class="provider-meta">{{ p.protocol }} &middot; {{ p.authority }}</span>
                    </div>
                    <div class="provider-badges">
                        <span class="badge" :class="p.isEnabled ? 'badge-green' : 'badge-gray'">{{ p.isEnabled ? 'Đã bật' : 'Đã tắt' }}</span>
                        <span v-if="p.clientId" class="badge badge-outline">Client: {{ p.clientId }}</span>
                    </div>
                </div>
            </div>
        </section>

        <section v-if="tab === 'import'" class="soc-section">
            <div class="import-grid">
                <div class="import-panel">
                    <div class="section-toolbar">
                        <h2>Nhập người dùng</h2>
                    </div>
                    <div class="form-grid single">
                        <label>
                            Nhà cung cấp
                            <select v-model="importUser.providerId">
                                <option value="">-- Chọn nhà cung cấp --</option>
                                <option v-for="p in providers.filter(x => x.isEnabled)" :key="p.externalIdentityProviderId" :value="p.externalIdentityProviderId">{{ p.name }}</option>
                            </select>
                        </label>
                        <label>
                            Dòng dữ liệu người dùng (mỗi dòng: ExternalSubject, Username, DisplayName, Email, Role)
                            <textarea v-model="importUser.raw" rows="6" placeholder="sub-001,john.doe,John Doe,john@example.com,LeTan"></textarea>
                        </label>
                        <button type="button" class="btn btn-primary btn-sm" :disabled="!importUser.providerId || !importUser.raw" @click="doImportUsers">Nhập người dùng</button>
                    </div>
                    <div v-if="importResult.length" class="import-result">
                        <div v-for="r in importResult" :key="r.externalSubject" class="import-row" :class="r.status === 'Imported' ? 'import-ok' : 'import-skip'">
                            <strong>{{ r.externalSubject }}</strong> &rarr; {{ r.username }} &mdash; {{ r.status }}
                        </div>
                    </div>
                </div>
                <div class="import-panel">
                    <div class="section-toolbar">
                        <h2>Nhập nhóm</h2>
                    </div>
                    <div class="form-grid single">
                        <label>
                            Dòng dữ liệu nhóm (mỗi dòng: Code, Name)
                            <textarea v-model="importGroup.raw" rows="6" placeholder="SEC-GUARD, Security Guard Team"></textarea>
                        </label>
                        <button type="button" class="btn btn-primary btn-sm" :disabled="!importGroup.raw" @click="doImportGroups">Nhập nhóm</button>
                    </div>
                    <div v-if="groupResult.length" class="import-result">
                        <div v-for="r in groupResult" :key="r.code" class="import-row import-ok">
                            <strong>{{ r.code }}</strong> &rarr; {{ r.name }}
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <section v-if="tab === 'mappings'" class="soc-section">
            <div class="section-toolbar">
                <h2>Ánh xạ danh tính</h2>
            </div>
            <div v-if="mappings.length === 0" class="empty-card">Không tìm thấy ánh xạ nào.</div>
            <div v-else class="mapping-list">
                <div v-for="m in mappings" :key="m.externalIdentityMappingId" class="mapping-row">
                    <div class="mapping-info">
                        <strong>{{ m.externalUsername || m.externalSubject }}</strong>
                        <span class="mapping-meta">Nhà cung cấp: {{ m.externalIdentityProviderId }} &middot; Sub: {{ m.externalSubject }}</span>
                    </div>
                    <span class="badge" :class="m.isActive ? 'badge-green' : 'badge-gray'">{{ m.isActive ? 'Hoạt động' : 'Không hoạt động' }}</span>
                </div>
            </div>
        </section>

        <div v-if="showEditor" class="modal-overlay" @click.self="showEditor = false">
            <div class="modal-content modal-lg">
                <div class="modal-header">
                    <h2>{{ editingProvider ? 'Sửa nhà cung cấp' : 'Thêm nhà cung cấp' }}</h2>
                    <button type="button" class="btn-close" @click="showEditor = false">&times;</button>
                </div>
                <div class="modal-body">
                    <div class="form-grid double">
                        <label>
                            Tên *
                            <input v-model="form.name" placeholder="Azure AD" required />
                        </label>
                        <label>
                            Giao thức
                            <select v-model="form.protocol">
                                <option value="OIDC">OIDC</option>
                                <option value="SAML">SAML</option>
                                <option value="LDAP">LDAP</option>
                            </select>
                        </label>
                        <label class="span-2">
                            Authority *
                            <input v-model="form.authority" placeholder="https://login.microsoftonline.com/..." />
                        </label>
                        <label>
                            Client ID
                            <input v-model="form.clientId" placeholder="00000000-0000-0000-0000-000000000000" />
                        </label>
                        <label>
                            Client Secret
                            <input v-model="form.clientSecret" type="password" placeholder="Secret..." />
                        </label>
                        <label>
                            Redirect URL
                            <input v-model="form.redirectUrl" placeholder="https://localhost:5173/login" />
                        </label>
                        <label class="span-2">
                            Scopes
                            <input v-model="form.scopes" placeholder="openid profile email" />
                        </label>
                        <label class="span-2">
                            <label class="checkbox-label">
                                <input v-model="form.isEnabled" type="checkbox" />
                                Bật
                            </label>
                        </label>
                    </div>
                    <div v-if="challengeUrl" class="challenge-box">
                        <strong>URL Thử thách OIDC</strong>
                        <code class="challenge-url">{{ challengeUrl }}</code>
                        <button type="button" class="btn btn-sm btn-secondary" @click="copyChallenge">Sao chép</button>
                    </div>
                </div>
                <div class="modal-footer">
                    <button v-if="editingProvider && form.protocol === 'OIDC' && form.clientId" type="button" class="btn btn-secondary" @click="buildChallenge">Kiểm tra Thử thách OIDC</button>
                    <button type="button" class="btn btn-primary" :disabled="!form.name || !form.authority" @click="saveProvider">{{ editingProvider ? 'Cập nhật' : 'Tạo mới' }}</button>
                    <button type="button" class="btn btn-secondary" @click="showEditor = false">Hủy</button>
                </div>
            </div>
        </div>

        <div v-if="showOffboard" class="modal-overlay" @click.self="showOffboard = false">
            <div class="modal-content">
                <div class="modal-header">
                    <h2>Ngừng cấp phát nhân viên</h2>
                    <button type="button" class="btn-close" @click="showOffboard = false">&times;</button>
                </div>
                <div class="modal-body">
                    <div class="form-grid single">
                        <label>
                            Mã nhân viên
                            <input v-model.number="offboardForm.employeeId" type="number" min="1" required />
                        </label>
                        <label>
                            Lý do
                            <textarea v-model="offboardForm.reason" rows="3" placeholder="Lý do thôi việc..."></textarea>
                        </label>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-warning" :disabled="!offboardForm.employeeId" @click="doOffboard">Xác nhận ngừng cấp phát</button>
                    <button type="button" class="btn btn-secondary" @click="showOffboard = false">Hủy</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { identityApi } from '../services/identityApi'

const loading = ref(false)
const tab = ref('providers')
const providers = ref([])
const mappings = ref([])
const overview = reactive({
    providers: 0, enabledProviders: 0,
    mappings: 0, activeMappings: 0,
    activeEmployees: 0, suspendedEmployees: 0,
    terminatedEmployees: 0, recertificationCampaigns: 0,
})
const showEditor = ref(false)
const showOffboard = ref(false)
const editingProvider = ref(null)
const challengeUrl = ref('')

const form = reactive({
    name: '', protocol: 'OIDC', authority: '',
    clientId: '', clientSecret: '', redirectUrl: '', scopes: 'openid profile email',
    isEnabled: true,
})

const importUser = reactive({ providerId: '', raw: '' })
const importGroup = reactive({ raw: '' })
const importResult = ref([])
const groupResult = ref([])

const offboardForm = reactive({ employeeId: null, reason: '' })

async function loadAll() {
    loading.value = true
    await Promise.all([loadOverview(), loadProviders()])
    loading.value = false
}

async function loadOverview() {
    try {
        const res = await identityApi.getOverview()
        Object.assign(overview, res.data)
    } catch {}
}

async function loadProviders() {
    try {
        const res = await identityApi.getProviders()
        providers.value = res.data
    } catch {}
}

async function loadMappings() {
    try {
        const res = await identityApi.getOverview()
        overview.mappings = res.data.mappings
        overview.activeMappings = res.data.activeMappings
    } catch {}
}

function openAddProvider() {
    editingProvider.value = null
    form.name = ''; form.protocol = 'OIDC'; form.authority = ''
    form.clientId = ''; form.clientSecret = ''; form.redirectUrl = ''
    form.scopes = 'openid profile email'; form.isEnabled = true
    challengeUrl.value = ''
    showEditor.value = true
}

function editProvider(p) {
    editingProvider.value = p
    form.name = p.name; form.protocol = p.protocol; form.authority = p.authority
    form.clientId = p.clientId || ''; form.clientSecret = ''; form.redirectUrl = p.redirectUrl || ''
    form.scopes = p.scopes || 'openid profile email'; form.isEnabled = p.isEnabled
    challengeUrl.value = ''
    showEditor.value = true
}

async function saveProvider() {
    try {
        const res = await identityApi.upsertProvider({ ...form })
        await loadProviders()
        showEditor.value = false
    } catch (err) {
        alert(err.response?.data?.message || 'Không lưu được nhà cung cấp')
    }
}

async function buildChallenge() {
    if (!editingProvider.value) return
    try {
        const res = await identityApi.oidcChallenge(
            editingProvider.value.externalIdentityProviderId,
            form.redirectUrl || 'https://localhost:5173/login',
            null
        )
        challengeUrl.value = res.data.challengeUrl
    } catch (err) {
        alert(err.response?.data?.message || 'Không tạo được thử thách')
    }
}

function copyChallenge() {
    navigator.clipboard.writeText(challengeUrl.value)
}

async function doImportUsers() {
    if (!importUser.providerId || !importUser.raw) return
    const lines = importUser.raw.trim().split('\n').filter(Boolean)
    const users = lines.map(line => {
        const parts = line.split(',').map(s => s.trim())
        return {
            externalSubject: parts[0],
            username: parts[1],
            displayName: parts[2],
            email: parts[3],
            role: parts[4],
            lifecycleStatus: parts[5],
        }
    })
    try {
        const res = await identityApi.importUsers(Number(importUser.providerId), users)
        importResult.value = res.data.results || []
    } catch (err) {
        alert(err.response?.data?.message || 'Nhập dữ liệu thất bại')
    }
}

async function doImportGroups() {
    if (!importGroup.raw) return
    const lines = importGroup.raw.trim().split('\n').filter(Boolean)
    const groups = lines.map(line => {
        const parts = line.split(',').map(s => s.trim())
        return { code: parts[0], name: parts[1] }
    })
    try {
        const res = await identityApi.importGroups(groups)
        groupResult.value = res.data.results || []
    } catch (err) {
        alert(err.response?.data?.message || 'Nhập dữ liệu thất bại')
    }
}

async function doOffboard() {
    if (!offboardForm.employeeId) return
    try {
        const res = await identityApi.offboardEmployee(offboardForm.employeeId, offboardForm.reason)
        alert(`Đã ngừng cấp phát nhân viên ${offboardForm.employeeId}. Phiên bản token: ${res.data.tokenVersion}`)
        showOffboard.value = false
        offboardForm.employeeId = null
        offboardForm.reason = ''
        await loadOverview()
    } catch (err) {
        alert(err.response?.data?.message || 'Ngừng cấp phát thất bại')
    }
}

onMounted(loadAll)
</script>

<style scoped>
.page-container { max-width: 1200px; }
.section-toolbar { display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }
.section-toolbar h2 { font-size: 1rem; font-weight: 600; color: var(--text-primary); margin: 0; }
.provider-list, .mapping-list { display: flex; flex-direction: column; gap: 8px; }
.provider-row { display: flex; align-items: center; justify-content: space-between; padding: 14px 16px; border-radius: 12px; border: 1px solid var(--border-soft); background: var(--surface); cursor: pointer; transition: border-color .15s, background-color .15s, box-shadow .15s; }
.provider-row:hover { border-color: var(--accent-primary); background: var(--surface-hover); box-shadow: var(--shadow-xs); }
.provider-info { display: flex; flex-direction: column; gap: 3px; }
.provider-meta { font-size: 0.8rem; color: var(--text-muted); }
.provider-badges { display: flex; gap: 6px; align-items: center; }
.badge { font-size: 0.75rem; padding: 3px 10px; border-radius: 20px; font-weight: 500; }
.badge-green { background: var(--status-success-bg); color: var(--status-success-text); }
.badge-gray { background: var(--status-neutral-bg); color: var(--status-neutral-text); }
.badge-outline { border: 1px solid var(--border-soft); color: var(--text-secondary); background: transparent; }
.mapping-row { display: flex; align-items: center; justify-content: space-between; padding: 10px 14px; border-radius: 10px; border: 1px solid var(--border-soft); background: var(--surface); }
.mapping-info { display: flex; flex-direction: column; gap: 2px; }
.mapping-meta { font-size: 0.78rem; color: var(--text-muted); }
.import-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
.import-panel { display: flex; flex-direction: column; gap: 12px; }
.import-result { display: flex; flex-direction: column; gap: 4px; max-height: 200px; overflow-y: auto; }
.import-row { font-size: 0.85rem; padding: 4px 8px; border-radius: 6px; }
.import-ok { color: var(--status-success-text); background: var(--status-success-bg); }
.import-skip { color: var(--status-warning-text); background: var(--status-warning-bg); }
.form-grid.double { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.form-grid.double .span-2 { grid-column: span 2; }
.form-grid.double label { display: flex; flex-direction: column; gap: 5px; font-size: 0.83rem; color: var(--text-secondary); }
.form-grid.double input, .form-grid.double select { min-height: 40px; padding: 0 12px; border-radius: 10px; border: 1px solid var(--border-soft); background: var(--surface); color: var(--text-primary); }
.checkbox-label { display: flex !important; flex-direction: row !important; align-items: center; gap: 8px; cursor: pointer; }
.checkbox-label input[type="checkbox"] { width: 18px; height: 18px; }
.challenge-box { margin-top: 12px; padding: 14px; background: var(--status-info-bg); border-radius: 10px; display: flex; flex-direction: column; gap: 8px; }
.challenge-url { font-size: 0.75rem; word-break: break-all; padding: 8px; background: var(--bg-code, #1e1e2e); border-radius: 6px; }
</style>
