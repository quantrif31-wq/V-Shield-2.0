<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Cấp phát thiết bị</span>
                <h1 class="page-title">Trình cấp phát thiết bị</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="showRequestForm = true">Yêu cầu mới</button>
                <button class="btn btn-secondary" @click="showCreateDevice = true">Tạo thiết bị</button>
                <button class="btn btn-secondary" @click="showRegisterController = true">Đăng ký bộ điều khiển</button>
            </div>
        </div>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Yêu cầu</span><h2 class="panel-title">Yêu cầu cấp phát</h2></div>
                    <div class="panel-actions">
                        <select v-model="statusFilter" @change="loadRequests" class="form-select">
                            <option value="">Tất cả</option>
                            <option value="Pending">Chờ duyệt</option>
                            <option value="Approved">Đã duyệt</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="requests.length === 0" class="empty-card">Chưa có yêu cầu cấp phát.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Thiết bị</th><th>Loại</th><th>Trạng thái</th><th>Ghi chú duyệt</th><th>Thao tác</th></tr></thead>
                        <tbody>
                            <tr v-for="r in requests" :key="r.deviceProvisioningRequestId">
                                <td>{{ r.requestedName }}</td>
                                <td>{{ r.deviceType }}</td>
                                <td><span class="badge" :class="r.status === 'Approved' ? 'badge-success' : 'badge-warn'">{{ r.status }}</span></td>
                                <td class="table-sub">{{ r.approvalNote || '—' }}</td>
                                <td>
                                    <button v-if="r.status === 'Pending'" class="btn btn-success btn-sm" @click="approve(r)">Duyệt</button>
                                    <button v-if="r.status === 'Approved'" class="btn btn-primary btn-sm" @click="finalizeProvisioning(r)">Hoàn tất</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Thiết bị</span><h2 class="panel-title">Thiết bị đã đăng ký</h2></div>
                </div>
                <div class="toolbar-shell">
                    <div class="search-bar">
                        <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                        </svg>
                        <input v-model="deviceSearch" type="text" placeholder="Tìm thiết bị..." />
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="filteredDevices.length === 0" class="empty-card">Chưa có thiết bị nào được đăng ký.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Tên</th><th>Loại</th><th>Hãng</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
                        <tbody>
                            <tr v-for="d in filteredDevices" :key="d.securityDeviceId" class="device-row" @click="selectedDevice = d">
                                <td>{{ d.name }}</td>
                                <td>{{ d.deviceType }}</td>
                                <td>{{ d.vendor || '—' }}</td>
                                <td><span class="status-dot" :class="statusClass(d.status)"></span>{{ d.status }}</td>
                                <td>
                                    <button class="btn btn-sm btn-ghost" @click.stop="selectedDevice = d">Chi tiết</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>

        <!-- New Request Modal -->
        <div v-if="showRequestForm" class="modal-overlay" @click.self="showRequestForm = false">
            <div class="modal-box">
                <h3>Yêu cầu cấp phát mới</h3>
                <div class="form-group">
                    <label>Tên thiết bị</label>
                    <input v-model="requestForm.requestedName" class="form-input" placeholder="vd. Contour-C2" />
                </div>
                <div class="form-group">
                    <label>Loại thiết bị</label>
                    <select v-model="requestForm.deviceType" class="form-select">
                        <option value="Controller">Bộ điều khiển</option>
                        <option value="Reader">Đầu đọc</option>
                        <option value="Camera">Camera</option>
                        <option value="Barrier">Barie</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Mã khu vực</label>
                    <input v-model.number="requestForm.siteId" type="number" class="form-input" placeholder="Không bắt buộc" />
                </div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showRequestForm = false">Hủy</button>
                    <button class="btn btn-primary" :disabled="busy || !requestForm.requestedName.trim()" @click="submitRequest">
                        {{ busy ? 'Đang gửi...' : 'Gửi' }}
                    </button>
                </div>
            </div>
        </div>

        <!-- Create Device Modal -->
        <div v-if="showCreateDevice" class="modal-overlay" @click.self="showCreateDevice = false">
            <div class="modal-box">
                <h3>Tạo thiết bị</h3>
                <div class="form-group">
                    <label>Tên thiết bị *</label>
                    <input v-model="createForm.name" class="form-input" placeholder="vd. Door-Reader-03" />
                </div>
                <div class="form-group">
                    <label>Loại thiết bị *</label>
                    <select v-model="createForm.deviceType" class="form-select">
                        <option value="Controller">Bộ điều khiển</option>
                        <option value="Reader">Đầu đọc</option>
                        <option value="Camera">Camera</option>
                        <option value="Barrier">Barie</option>
                        <option value="Sensor">Cảm biến</option>
                    </select>
                </div>
                <div class="form-row two">
                    <div class="form-group">
                        <label>Hãng</label>
                        <input v-model="createForm.vendor" class="form-input" placeholder="vd. HID" />
                    </div>
                    <div class="form-group">
                        <label>Model</label>
                        <input v-model="createForm.model" class="form-input" placeholder="vd. Signo-20" />
                    </div>
                </div>
                <div class="form-group">
                    <label>Địa chỉ IP</label>
                    <input v-model="createForm.ipAddress" class="form-input" placeholder="vd. 192.168.1.100" />
                </div>
                <div class="form-group">
                    <label>Mã khu vực</label>
                    <input v-model.number="createForm.siteId" type="number" class="form-input" placeholder="Không bắt buộc" />
                </div>
                <div v-if="createResult" class="success-card">{{ createResult }}</div>
                <div v-else-if="createError" class="alert alert-danger">{{ createError }}</div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showCreateDevice = false">Hủy</button>
                    <button class="btn btn-primary" :disabled="createBusy || !createForm.name.trim()" @click="submitCreateDevice">
                        {{ createBusy ? 'Đang tạo...' : 'Tạo' }}
                    </button>
                </div>
            </div>
        </div>

        <!-- Register Controller Modal -->
        <div v-if="showRegisterController" class="modal-overlay" @click.self="showRegisterController = false">
            <div class="modal-box">
                <h3>Đăng ký bộ điều khiển</h3>
                <div class="form-group">
                    <label>Mã thiết bị cha *</label>
                    <input v-model.number="regForm.deviceId" type="number" class="form-input" placeholder="Mã thiết bị để đăng ký bộ điều khiển" />
                </div>
                <div class="form-group">
                    <label>Giao thức</label>
                    <select v-model="regForm.protocol" class="form-select">
                        <option value="OSDP">OSDP</option>
                        <option value="Wiegand">Wiegand</option>
                        <option value="RS-485">RS-485</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Số thẻ tối đa</label>
                    <input v-model.number="regForm.maxCredentials" type="number" class="form-input" value="50000" />
                </div>
                <div v-if="regResult" class="success-card">{{ regResult }}</div>
                <div v-else-if="regError" class="alert alert-danger">{{ regError }}</div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showRegisterController = false">Hủy</button>
                    <button class="btn btn-primary" :disabled="regBusy || !regForm.deviceId" @click="submitRegisterController">
                        {{ regBusy ? 'Đang đăng ký...' : 'Đăng ký' }}
                    </button>
                </div>
            </div>
        </div>

        <!-- Device Detail Modal -->
        <div v-if="selectedDevice" class="modal-overlay" @click.self="selectedDevice = null">
            <div class="modal-box">
                <h3>Chi tiết thiết bị — {{ selectedDevice.name }}</h3>
                <div class="detail-grid" style="margin:12px 0;">
                    <div class="detail-row"><span class="detail-label">ID</span><span>{{ selectedDevice.securityDeviceId }}</span></div>
                    <div class="detail-row"><span class="detail-label">Loại</span><span>{{ selectedDevice.deviceType }}</span></div>
                    <div class="detail-row"><span class="detail-label">Trạng thái</span><span class="status-dot" :class="statusClass(selectedDevice.status)"></span>{{ selectedDevice.status }}</div>
                    <div class="detail-row"><span class="detail-label">Hãng</span><span>{{ selectedDevice.vendor || '—' }}</span></div>
                    <div class="detail-row"><span class="detail-label">Model</span><span>{{ selectedDevice.model || '—' }}</span></div>
                    <div class="detail-row"><span class="detail-label">Khu vực</span><span>{{ selectedDevice.siteId || '—' }}</span></div>
                </div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="selectedDevice = null">Đóng</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const requests = ref([])
const devices = ref([])
const loading = ref(true)
const busy = ref(false)
const showRequestForm = ref(false)
const statusFilter = ref('')
const deviceSearch = ref('')
const selectedDevice = ref(null)

// Create Device
const showCreateDevice = ref(false)
const createBusy = ref(false)
const createResult = ref('')
const createError = ref('')
const createForm = ref({
    name: '', deviceType: 'Controller', vendor: '',
    model: '', ipAddress: '', siteId: null,
})

// Register Controller
const showRegisterController = ref(false)
const regBusy = ref(false)
const regResult = ref('')
const regError = ref('')
const regForm = ref({ deviceId: null, protocol: 'OSDP', maxCredentials: 50000 })

// Request form
const requestForm = ref({ requestedName: '', deviceType: 'Controller', siteId: null })

const filteredDevices = computed(() => {
    if (!deviceSearch.value) return devices.value
    const q = deviceSearch.value.toLowerCase()
    return devices.value.filter(d => d.name.toLowerCase().includes(q))
})

async function loadRequests() {
    loading.value = true
    try {
        const [reqRes, devRes] = await Promise.all([
            enterpriseApi.getProvisioningRequests({ status: statusFilter.value || undefined }),
            enterpriseApi.getTopology(),
        ])
        requests.value = Array.isArray(reqRes.data) ? reqRes.data : []
        const topo = Array.isArray(devRes.data) ? devRes.data : []
        devices.value = topo
    } catch {
        requests.value = []
        devices.value = []
    } finally {
        loading.value = false
    }
}

async function submitRequest() {
    if (!requestForm.value.requestedName.trim()) return
    busy.value = true
    try {
        await enterpriseApi.createProvisioningRequest(requestForm.value)
        showRequestForm.value = false
        requestForm.value = { requestedName: '', deviceType: 'Controller', siteId: null }
        await loadRequests()
    } finally {
        busy.value = false
    }
}

async function approve(r) {
    if (!confirm(`Phê duyệt cấp phát thiết bị cho "${r.requestedName}"?`)) return
    try {
        await enterpriseApi.approveProvisioningRequest(r.deviceProvisioningRequestId, { approvalNote: 'Đã duyệt qua trình cấp phát' })
        await loadRequests()
    } catch {
        alert('Phê duyệt thất bại')
    }
}

async function finalizeProvisioning(r) {
    // After approval, auto-create the device
    if (!confirm(`Hoàn tất cấp phát cho "${r.requestedName}" bằng cách tạo thiết bị?`)) return
    busy.value = true
    try {
        await enterpriseApi.createDevice({
            name: r.requestedName,
            deviceType: r.deviceType,
            siteId: r.siteId || null,
        })
        alert(`Đã tạo thiết bị "${r.requestedName}" thành công!`)
        await loadRequests()
    } catch (e) {
        alert('Hoàn tất thất bại: ' + (e.response?.data?.message || e.message))
    } finally {
        busy.value = false
    }
}

async function submitCreateDevice() {
    if (!createForm.value.name.trim()) return
    createBusy.value = true
    createResult.value = ''
    createError.value = ''
    try {
        const res = await enterpriseApi.createDevice({
            name: createForm.value.name,
            deviceType: createForm.value.deviceType,
            vendor: createForm.value.vendor || null,
            model: createForm.value.model || null,
            ipAddress: createForm.value.ipAddress || null,
            siteId: createForm.value.siteId || null,
        })
        createResult.value = `Đã tạo thiết bị! ID: ${res.data?.securityDeviceId || res.data?.id}`
        createForm.value = { name: '', deviceType: 'Controller', vendor: '', model: '', ipAddress: '', siteId: null }
        await loadRequests()
    } catch (e) {
        createError.value = e.response?.data?.message || e.message
    } finally {
        createBusy.value = false
    }
}

async function submitRegisterController() {
    if (!regForm.value.deviceId) return
    regBusy.value = true
    regResult.value = ''
    regError.value = ''
    try {
        const res = await enterpriseApi.registerController(regForm.value.deviceId, {
            protocol: regForm.value.protocol,
            maxCredentials: regForm.value.maxCredentials,
        })
        regResult.value = `Đã đăng ký bộ điều khiển! ${res.data?.message || ''}`
        regForm.value = { deviceId: null, protocol: 'OSDP', maxCredentials: 50000 }
    } catch (e) {
        regError.value = e.response?.data?.message || e.message
    } finally {
        regBusy.value = false
    }
}

function statusClass(s) {
    if (s === 'Ok' || s === 'Online') return 'status-ok'
    if (s === 'Tamper' || s === 'Fault') return 'status-danger'
    return 'status-warn'
}

onMounted(loadRequests)
</script>

<style scoped>
.device-row { cursor: pointer; transition: background var(--transition-fast); }
.device-row:hover { background: var(--surface-hover); }
.toolbar-shell { margin-bottom: 8px; }
.search-bar { position: relative; }
.search-icon { position: absolute; left: 10px; top: 50%; transform: translateY(-50%); width: 16px; height: 16px; color: var(--text-muted); }
.search-bar input { width: 100%; padding: 8px 10px 8px 32px; border: 1px solid var(--border-subtle); border-radius: 8px; font-size: 13px; background: var(--surface-default); }
</style>
