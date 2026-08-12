<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Khả năng phục hồi ngoại tuyến</span>
                <h1 class="page-title">Gói chính sách ngoại tuyến</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="showForm = true">Tạo gói</button>
            </div>
        </div>
        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Gói</span><h2 class="panel-title">Tất cả gói ngoại tuyến</h2></div>
                    <div class="panel-actions">
                        <select v-model="statusFilter" @change="loadPackages" class="form-select">
                            <option value="">Tất cả</option>
                            <option value="Draft">Bản nháp</option>
                            <option value="Published">Đã xuất bản</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="packages.length === 0" class="empty-card">Chưa có gói ngoại tuyến nào.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>ID thiết bị</th><th>Phiên bản</th><th>Trạng thái</th><th>Băm</th><th>Xuất bản lúc</th></tr></thead>
                        <tbody>
                            <tr v-for="p in packages" :key="p.offlinePolicyPackageId">
                                <td>{{ p.offlinePolicyPackageId }}</td>
                                <td>{{ p.securityDeviceId }}</td>
                                <td>{{ p.packageVersion }}</td>
                                <td><span class="badge" :class="p.status === 'Published' ? 'badge-success' : 'badge-warn'">{{ statusLabel(p.status) }}</span></td>
                                <td class="table-sub">{{ p.payloadHash ? p.payloadHash.substring(0, 12) + '...' : '—' }}</td>
                                <td>{{ p.publishedAtUtc ? new Date(p.publishedAtUtc).toLocaleString() : '—' }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>
        <div v-if="showForm" class="modal-overlay" @click.self="showForm = false">
            <div class="modal-box">
                <h3>Tạo gói chính sách ngoại tuyến</h3>
                <div class="form-group">
                    <label>ID thiết bị</label>
                    <input v-model.number="form.securityDeviceId" type="number" class="form-input" />
                </div>
                <div class="form-group">
                    <label>Phiên bản gói</label>
                    <input v-model="form.packageVersion" class="form-input" placeholder="VD: 1.0.0" />
                </div>
                <div class="form-group">
                    <label>Payload JSON</label>
                    <textarea v-model="form.payloadJson" class="form-input" rows="4" placeholder='{"allowAll":true}'></textarea>
                </div>
                <div class="form-group">
                    <label>Trạng thái</label>
                    <select v-model="form.status" class="form-select">
                        <option value="Draft">Bản nháp</option>
                        <option value="Published">Đã xuất bản</option>
                    </select>
                </div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showForm = false">Hủy</button>
                    <button class="btn btn-primary" :disabled="busy" @click="submitPackage">{{ busy ? 'Đang tạo...' : 'Tạo' }}</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const statusLabelMap = { Draft: 'Bản nháp', Published: 'Đã xuất bản' }
function statusLabel(value) { return statusLabelMap[value] || value }

const packages = ref([])
const loading = ref(true)
const busy = ref(false)
const showForm = ref(false)
const statusFilter = ref('')
const form = ref({ securityDeviceId: null, packageVersion: '', payloadJson: '{"allowAll":true}', payloadHash: '', status: 'Draft' })

async function loadPackages() {
    loading.value = true
    try {
        const res = await enterpriseApi.getOfflinePolicyPackages({ status: statusFilter.value || undefined })
        packages.value = Array.isArray(res.data) ? res.data : []
    } catch { packages.value = [] }
    finally { loading.value = false }
}

async function submitPackage() {
    if (!form.value.securityDeviceId) return
    busy.value = true
    try {
        await enterpriseApi.createOfflinePolicyPackage(form.value)
        showForm.value = false
        form.value = { securityDeviceId: null, packageVersion: '', payloadJson: '{"allowAll":true}', payloadHash: '', status: 'Draft' }
        await loadPackages()
    } finally { busy.value = false }
}

onMounted(loadPackages)
</script>
