<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <h1>Quản lý tủ locker</h1>
            <button class="btn btn-primary" @click="openCreateCabinet">+ Thêm tủ</button>
        </div>

        <div v-if="loading" class="loading-spinner">Đang tải...</div>
        <section v-else class="ops-grid one">
            <article class="ops-panel" v-for="cabinet in cabinets" :key="cabinet.lockerCabinetId">
                <div class="ops-panel-header">
                    <h3>{{ cabinet.name }}</h3>
                    <span class="badge badge-info">{{ cabinet.location || 'Chưa có vị trí' }}</span>
                </div>
                <div class="compartment-grid">
                    <div v-for="comp in compartmentsByCabinet(cabinet.lockerCabinetId)" :key="comp.lockerCompartmentId"
                         :class="['compartment', comp.status.toLowerCase()]"
                         @click="selectCompartment(comp)">
                        <div class="comp-code">{{ comp.code }}</div>
                        <div class="comp-status">{{ comp.status === 'Empty' ? 'Trống' : 'Có đồ' }}</div>
                    </div>
                </div>
                <div class="form-actions" style="margin-top:0.75rem">
                    <button class="btn btn-sm btn-secondary" @click="showAddCompartments(cabinet)">+ Thêm ngăn</button>
                    <button class="btn btn-sm btn-secondary" @click="openEditCabinet(cabinet)">Sửa tủ</button>
                    <button class="btn btn-sm btn-danger" @click="deleteCabinet(cabinet)">Xóa tủ</button>
                    <router-link to="/locker-access-logs" class="btn btn-sm btn-info">Nhật ký</router-link>
                </div>
            </article>
        </section>

        <Teleport to="body">
            <div v-if="showCabinetForm" class="modal-overlay" @click.self="showCabinetForm = false">
                <div class="modal-panel">
                    <h3>{{ editingCabinet ? 'Sửa tủ locker' : 'Thêm tủ locker' }}</h3>
                    <div class="form-group"><label>Tên tủ *</label><input v-model="cabinetForm.name" class="form-control" /></div>
                    <div class="form-group"><label>Vị trí</label><input v-model="cabinetForm.location" class="form-control" /></div>
                    <div class="form-group"><label>Mô tả</label><textarea v-model="cabinetForm.description" class="form-control" rows="2"></textarea></div>
                    <div class="form-actions">
                        <button class="btn btn-primary" @click="submitCabinet" :disabled="submitting">{{ submitting ? 'Đang lưu...' : 'Lưu' }}</button>
                        <button class="btn btn-secondary" @click="showCabinetForm = false">Hủy</button>
                    </div>
                </div>
            </div>

            <div v-if="showCompartmentForm" class="modal-overlay" @click.self="showCompartmentForm = false">
                <div class="modal-panel">
                    <h3>Thêm ngăn cho tủ "{{ selectedCabinet?.name }}"</h3>
                    <div class="form-group">
                        <label>Mã ngăn (cách nhau bằng dấu phẩy, VD: A1,A2,B1,B2)</label>
                        <textarea v-model="compartmentCodes" class="form-control" rows="3" placeholder="A1,A2,B1,B2"></textarea>
                    </div>
                    <div class="form-actions">
                        <button class="btn btn-primary" @click="submitCompartments" :disabled="submitting">{{ submitting ? 'Đang lưu...' : 'Lưu' }}</button>
                        <button class="btn btn-secondary" @click="showCompartmentForm = false">Hủy</button>
                    </div>
                </div>
            </div>

            <div v-if="selectedComp" class="modal-overlay" @click.self="selectedComp = null">
                <div class="modal-panel">
                    <h3>Ngăn {{ selectedComp.code }}</h3>
                    <p>Trạng thái: <strong>{{ selectedComp.status === 'Empty' ? 'Trống' : 'Có đồ' }}</strong></p>
                    <p v-if="selectedComp.evidenceItem">Evidence ID: #{{ selectedComp.evidenceItem.evidenceItemId }}</p>
                    <div class="form-actions">
                        <button v-if="selectedComp.status === 'Occupied'" class="btn btn-danger" @click="releaseCompartment(selectedComp)">Lấy đồ ra</button>
                        <button class="btn btn-secondary" @click="selectedComp = null">Đóng</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { lostFoundApi } from '../services/enterpriseSecurityApi'

const cabinets = ref([])
const compartments = ref([])
const loading = ref(false)
const showCabinetForm = ref(false)
const showCompartmentForm = ref(false)
const selectedCabinet = ref(null)
const selectedComp = ref(null)
const compartmentCodes = ref('')
const submitting = ref(false)
const editingCabinet = ref(null)
const cabinetForm = reactive({ name: '', location: '', description: '' })

onMounted(loadData)

async function loadData() {
    loading.value = true
    try {
        const res = await lostFoundApi.getLockerCabinets()
        cabinets.value = res.data || []
        const ids = cabinets.value.map(c => c.lockerCabinetId)
        const all = await Promise.all(ids.map(id =>
            lostFoundApi.getLockerCabinetDetail(id).then(r => r.data.compartments || []).catch(() => [])
        ))
        compartments.value = all.flat()
    } catch (e) { console.error(e) }
    finally { loading.value = false }
}

function compartmentsByCabinet(cabinetId) {
    return compartments.value.filter(c => c.lockerCabinetId === cabinetId)
}

function openCreateCabinet() {
    editingCabinet.value = null
    cabinetForm.name = ''; cabinetForm.location = ''; cabinetForm.description = ''
    showCabinetForm.value = true
}

function openEditCabinet(cabinet) {
    editingCabinet.value = cabinet
    cabinetForm.name = cabinet.name
    cabinetForm.location = cabinet.location || ''
    cabinetForm.description = cabinet.description || ''
    showCabinetForm.value = true
}

async function submitCabinet() {
    if (!cabinetForm.name) { alert('Tên tủ là bắt buộc.'); return }
    submitting.value = true
    try {
        if (editingCabinet.value) {
            await lostFoundApi.updateLockerCabinet(editingCabinet.value.lockerCabinetId, {
                name: cabinetForm.name, location: cabinetForm.location || null, description: cabinetForm.description || null
            })
        } else {
            await lostFoundApi.createLockerCabinet({ name: cabinetForm.name, location: cabinetForm.location || null, description: cabinetForm.description || null })
        }
        showCabinetForm.value = false
        await loadData()
    } catch (e) { alert('Lỗi: ' + (e.response?.data?.message || e.message)) }
    finally { submitting.value = false }
}

async function deleteCabinet(cabinet) {
    if (!confirm(`Xóa tủ "${cabinet.name}"? Chỉ xóa được nếu tất cả ngăn đều trống.`)) return
    try {
        await lostFoundApi.deleteLockerCabinet(cabinet.lockerCabinetId)
        await loadData()
    } catch (e) { alert('Lỗi: ' + (e.response?.data?.message || e.message)) }
}

function showAddCompartments(cabinet) {
    selectedCabinet.value = cabinet
    compartmentCodes.value = ''
    showCompartmentForm.value = true
}

async function submitCompartments() {
    const codes = compartmentCodes.value.split(',').map(c => c.trim()).filter(Boolean)
    if (!codes.length) { alert('Nhập ít nhất một mã ngăn.'); return }
    submitting.value = true
    try {
        await lostFoundApi.createCompartments(selectedCabinet.value.lockerCabinetId, { codes })
        showCompartmentForm.value = false
        await loadData()
    } catch (e) { alert('Lỗi: ' + (e.response?.data?.message || e.message)) }
    finally { submitting.value = false }
}

function selectCompartment(comp) {
    selectedComp.value = comp
}

async function releaseCompartment(comp) {
    if (!confirm(`Xác nhận lấy đồ ra khỏi ngăn ${comp.code}?`)) return
    try {
        await lostFoundApi.releaseCompartment(comp.lockerCompartmentId)
        selectedComp.value = null
        await loadData()
    } catch (e) { alert('Lỗi: ' + (e.response?.data?.message || e.message)) }
}
</script>

<style scoped>
.compartment-grid { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-top: 0.5rem; }
.compartment { width: 80px; height: 80px; border: 2px solid var(--border); border-radius: 8px; display: flex; flex-direction: column; align-items: center; justify-content: center; cursor: pointer; transition: all 0.2s; }
.compartment:hover { transform: scale(1.05); }
.compartment.empty { border-color: var(--success); background: rgba(40,167,69,0.08); }
.compartment.occupied { border-color: var(--warning); background: rgba(255,193,7,0.08); }
.comp-code { font-weight: 700; font-size: 1.1rem; }
.comp-status { font-size: 0.7rem; color: var(--text-secondary); }
.loading-spinner { text-align: center; padding: 2rem; color: var(--text-secondary); }
</style>
