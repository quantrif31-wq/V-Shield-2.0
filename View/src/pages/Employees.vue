<template>
  <div class="page-container employees-page">
    <PageHeader
      title="Hồ sơ nhân viên"
      description="Quản lý hồ sơ, trạng thái truy cập và dữ liệu nhận diện của nhân sự nội bộ."
      :breadcrumbs="[{ label: 'Nhân sự' }, { label: 'Hồ sơ nhân viên' }]"
    >
      <template #actions>
        <BaseButton variant="secondary" @click="showImportModal = true">Nhập dữ liệu</BaseButton>
        <BaseButton variant="secondary" @click="showExportModal = true">Xuất dữ liệu</BaseButton>
        <BaseButton @click="openCreateModal">Thêm nhân viên</BaseButton>
      </template>
    </PageHeader>

    <section class="summary-grid" aria-label="Tổng quan nhân sự">
      <BaseCard variant="kpi"><span>Tổng nhân sự</span><strong>{{ employeeSummary.totalEmployees || employees.length }}</strong><small>Hồ sơ trong hệ thống</small></BaseCard>
      <BaseCard variant="kpi"><span>Đang hoạt động</span><strong>{{ activeCount }}</strong><small>Có quyền truy cập</small></BaseCard>
      <BaseCard variant="kpi"><span>Đã khóa</span><strong>{{ inactiveCount }}</strong><small>Không thể sử dụng cổng</small></BaseCard>
    </section>

    <section class="list-panel" aria-labelledby="employee-list-title">
      <div class="list-toolbar">
        <div><h2 id="employee-list-title">Danh sách nhân viên</h2><p>{{ employees.length }} kết quả phù hợp</p></div>
        <div class="filter-bar" role="search">
          <BaseInput id="employee-search" v-model="searchQuery" type="search" placeholder="Tìm theo tên, email hoặc số điện thoại" aria-label="Tìm nhân viên" @input="debouncedCommitFilters" />
          <BaseSelect id="employee-status" v-model="filterStatus" aria-label="Lọc theo trạng thái" @update:model-value="commitFilters">
            <option value="">Tất cả trạng thái</option><option value="true">Đang hoạt động</option><option value="false">Đã khóa</option>
          </BaseSelect>
          <BaseButton v-if="hasActiveFilters" variant="ghost" @click="clearFilters">Xóa bộ lọc</BaseButton>
          <BaseButton variant="secondary" :loading="loading" @click="fetchEmployees">Làm mới</BaseButton>
        </div>
      </div>

      <DataTable
        :columns="columns"
        :rows="paginatedEmployees"
        row-key="employeeId"
        :loading="loading"
        :error="loadError"
        :permission-denied="permissionDenied"
        :sort-key="sortKey"
        :sort-direction="sortDirection"
        empty-title="Không có nhân viên phù hợp"
        empty-description="Thử thay đổi bộ lọc hoặc tạo hồ sơ nhân viên mới."
        @sort="handleSort"
      >
        <template #retry><BaseButton variant="secondary" @click="fetchEmployees">Thử lại</BaseButton></template>
        <template #empty-actions><BaseButton @click="openCreateModal">Thêm nhân viên</BaseButton></template>
        <template #cell-fullName="{ row }">
          <div class="identity-cell">
            <img v-if="getEmployeeAvatarSrc(row)" :src="getEmployeeAvatarSrc(row)" class="avatar" alt="" @error="markEmployeeAvatarBroken(row.employeeId, $event)" />
            <span v-else class="avatar avatar-fallback" :class="`tone-${row.employeeId % 5}`" aria-hidden="true">{{ getInitials(row.fullName) }}</span>
            <span><strong>{{ row.fullName }}</strong><small>ID {{ row.employeeId }}</small></span>
          </div>
        </template>
        <template #cell-contact="{ row }"><div class="stacked-cell"><span>{{ row.phone || 'Chưa có số điện thoại' }}</span><small>{{ row.email || 'Chưa có email' }}</small></div></template>
        <template #cell-organization="{ row }"><div class="stacked-cell"><span>{{ row.departmentName || 'Chưa xếp phòng' }}</span><small>{{ row.positionName || 'Chưa có chức vụ' }}</small></div></template>
        <template #cell-status="{ row }"><StatusBadge :status="row.status ? 'active' : 'inactive'" :label="row.status ? 'Hoạt động' : 'Đã khóa'" dot /></template>
        <template #cell-faceId="{ row }">
          <StatusBadge :status="row.hasFaceId ? 'active' : 'inactive'" :label="row.hasFaceId ? 'Đã đăng ký' : 'Chưa đăng ký'" dot />
        </template>
        <template #actions="{ row }">
          <div class="row-actions">
            <BaseButton variant="ghost" size="small" @click="openEditModal(row)">Sửa</BaseButton>
            <BaseButton variant="ghost" size="small" @click="requestFaceUpload(row)">Face ID</BaseButton>
            <BaseButton variant="ghost" size="small" @click="confirmDelete(row)">Xóa</BaseButton>
          </div>
        </template>
      </DataTable>

      <footer v-if="!loading && !loadError && employees.length" class="pagination-bar">
        <span>Hiển thị {{ pagStart }}–{{ pagEnd }} trong {{ employees.length }}</span>
        <div class="pagination-actions" aria-label="Phân trang">
          <BaseButton variant="secondary" size="small" :disabled="currentPage <= 1" @click="setPage(currentPage - 1)">Trang trước</BaseButton>
          <span aria-current="page">Trang {{ currentPage }} / {{ totalPages }}</span>
          <BaseButton variant="secondary" size="small" :disabled="currentPage >= totalPages" @click="setPage(currentPage + 1)">Trang sau</BaseButton>
        </div>
      </footer>
    </section>

    <input ref="faceUploadInput" class="sr-only" type="file" accept="image/*" aria-label="Chọn ảnh Face ID cho nhân viên" @change="handleFaceUpload" />

    <BaseModal :open="showModal" :title="isEditing ? 'Cập nhật nhân viên' : 'Thêm nhân viên'" description="Thông tin được sử dụng trong các luồng kiểm soát ra vào." @close="requestCloseModal">
      <form id="employee-form" class="employee-form" @submit.prevent="handleSubmit">
        <BaseField for-id="employee-name" label="Họ và tên" required :error="nameError" :success="empNameValidation.isValid ? 'Tên hợp lệ' : ''" v-slot="field">
          <BaseInput id="employee-name" v-model="modalForm.fullName" :describedby="field.describedby" :invalid="field.invalid" autocomplete="name" placeholder="Ví dụ: Nguyễn Văn An" @input="runNameValidation" @blur="empNameValidation.touched = true; runNameValidation()" />
        </BaseField>
        <div class="form-grid">
          <BaseField for-id="employee-phone" label="Điện thoại" v-slot="field"><BaseInput id="employee-phone" v-model="modalForm.phone" type="tel" :describedby="field.describedby" autocomplete="tel" placeholder="09xx xxx xxx" /></BaseField>
          <BaseField for-id="employee-email" label="Email" v-slot="field"><BaseInput id="employee-email" v-model="modalForm.email" type="email" :describedby="field.describedby" autocomplete="email" placeholder="mail@example.com" /></BaseField>
          <BaseField for-id="employee-department" label="Phòng ban" v-slot="field"><BaseSelect id="employee-department" v-model="modalForm.departmentId" :describedby="field.describedby"><option :value="null">Chọn phòng ban</option><option v-for="item in departments" :key="item.departmentId" :value="item.departmentId">{{ item.name }}</option></BaseSelect></BaseField>
          <BaseField for-id="employee-position" label="Chức vụ" v-slot="field"><BaseSelect id="employee-position" v-model="modalForm.positionId" :describedby="field.describedby"><option :value="null">Chọn chức vụ</option><option v-for="item in positions" :key="item.positionId" :value="item.positionId">{{ item.name }}</option></BaseSelect></BaseField>
        </div>
        <BaseSwitch v-if="isEditing" v-model="modalForm.status" label="Cho phép truy cập" description="Tắt để khóa quyền sử dụng cổng nhưng vẫn giữ hồ sơ và lịch sử." />
        <fieldset class="face-section"><legend>Dữ liệu nhận diện Face ID</legend><div class="mode-actions"><BaseButton :variant="uploadMode === 'file' ? 'primary' : 'secondary'" size="small" @click="uploadMode = 'file'">Tải tệp</BaseButton><BaseButton :variant="uploadMode === 'url' ? 'primary' : 'secondary'" size="small" @click="uploadMode = 'url'">Dùng URL</BaseButton></div>
          <div v-if="uploadMode === 'file'" class="face-upload"><BaseButton variant="secondary" class="dropzone" @click="$refs.faceInput.click()" @dragover.prevent @drop.prevent="handleDrop"><img v-if="facePreview" :src="facePreview" alt="Ảnh Face ID đang chọn"/><span v-else>Chọn hoặc kéo ảnh JPG/PNG/WebP vào đây</span></BaseButton><input ref="faceInput" class="sr-only" type="file" accept="image/*" aria-label="Chọn ảnh Face ID trong biểu mẫu" @change="handleFaceSelect"/><BaseButton v-if="facePreview" variant="link" @click="removeFace">Xóa ảnh đã chọn</BaseButton></div>
          <div v-else class="face-upload"><BaseField for-id="employee-face-url" label="URL ảnh" :error="urlError ? 'Không thể tải ảnh từ URL này.' : ''" v-slot="field"><BaseInput id="employee-face-url" v-model="modalForm.faceImageUrl" type="url" :describedby="field.describedby" :invalid="field.invalid" placeholder="https://example.com/face.jpg" @input="urlError = false"/><img v-if="modalForm.faceImageUrl && !urlError" :src="modalForm.faceImageUrl" class="url-preview" alt="Xem trước ảnh Face ID" @error="urlError = true"/></BaseField><BaseButton v-if="modalForm.faceImageUrl" variant="link" @click="modalForm.faceImageUrl = ''; urlError = false">Xóa URL ảnh</BaseButton></div>
        </fieldset>
        <p v-if="modalError" class="form-error" role="alert">{{ modalError }}</p>
      </form>
      <template #footer><BaseButton variant="secondary" :disabled="saving" @click="requestCloseModal">Hủy</BaseButton><BaseButton type="submit" form="employee-form" :loading="saving" :disabled="Boolean(nameError)">{{ isEditing ? 'Lưu thay đổi' : 'Tạo nhân viên' }}</BaseButton></template>
    </BaseModal>

    <ConfirmDialog :open="showDeleteModal" kind="destructive" title="Xóa hồ sơ nhân viên?" :description="`Hồ sơ ${deleteTarget?.fullName || ''} và dữ liệu Face ID liên quan sẽ bị xóa. Hành động này không thể hoàn tác.`" confirm-label="Xóa nhân viên" :loading="saving" @cancel="showDeleteModal = false" @confirm="handleDelete" />
    <ConfirmDialog :open="showDiscardDialog" title="Bỏ thay đổi chưa lưu?" description="Các thông tin bạn vừa nhập sẽ bị mất." confirm-label="Bỏ thay đổi" @cancel="showDiscardDialog = false" @confirm="closeModal(true)" />

    <ImportModal v-if="showImportModal" entity-type="Employee" entity-display-name="Nhân viên" @close="showImportModal = false" @import-complete="onImportComplete" />
    <ExportModal v-if="showExportModal" entity-type="Employee" entity-display-name="Nhân viên" :available-columns="['EmployeeId','FullName','Email','Phone','DepartmentName','PositionName','Status']" @close="showExportModal = false" />
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { create, deleteEmployee, getAll, getProtectedFaceImage, update, uploadFace } from '../services/employeeApi'
import { getDepartments, getPositions } from '../services/lookupApi'
import { getSummary as getEmployeeSummary } from '../services/statisticsApi'
import { normalizeVietnameseName, validateVietnameseName } from '../utils/nameValidator'
import BaseButton from '../components/ui/BaseButton.vue'
import BaseCard from '../components/ui/BaseCard.vue'
import BaseField from '../components/ui/BaseField.vue'
import BaseInput from '../components/ui/BaseInput.vue'
import BaseModal from '../components/ui/BaseModal.vue'
import BaseSelect from '../components/ui/BaseSelect.vue'
import BaseSwitch from '../components/ui/BaseSwitch.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'
import DataTable from '../components/ui/DataTable.vue'
import PageHeader from '../components/ui/PageHeader.vue'
import StatusBadge from '../components/ui/StatusBadge.vue'
import ImportModal from '../components/import-export/ImportModal.vue'
import ExportModal from '../components/import-export/ExportModal.vue'
import { useToasts } from '../composables/useToasts'

const route = useRoute(); const router = useRouter(); const { success, error: showError } = useToasts()
const employees = ref([]); const departments = ref([]); const positions = ref([]); const loading = ref(true); const loadError = ref(''); const permissionDenied = ref(false)
const searchQuery = ref(''); const filterStatus = ref(''); const currentPage = ref(1); const pageSize = 10; const sortKey = ref('fullName'); const sortDirection = ref('asc')
const employeeSummary = ref({ totalEmployees: 0, activeEmployees: 0, inactiveEmployees: 0 }); const protectedAvatarUrls = ref({}); const brokenEmployeeAvatarIds = ref({})
const showModal = ref(false); const showDeleteModal = ref(false); const showDiscardDialog = ref(false); const showImportModal = ref(false); const showExportModal = ref(false)
const isEditing = ref(false); const editingId = ref(null); const saving = ref(false); const modalError = ref(''); const deleteTarget = ref(null); const formBaseline = ref('')
const modalForm = reactive({ fullName: '', phone: '', email: '', departmentId: null, positionId: null, status: true, faceImageUrl: '' }); const faceFile = ref(null); const facePreview = ref(''); const uploadMode = ref('file'); const urlError = ref(false)
const faceUploadInput = ref(null); const faceUploadTarget = ref(null); let searchTimer = null
const empNameValidation = reactive({ touched: false, isValid: false, error: '' })
const columns = [{ key: 'fullName', label: 'Nhân viên', sortable: true }, { key: 'contact', label: 'Liên hệ' }, { key: 'organization', label: 'Đơn vị' }, { key: 'faceId', label: 'Face ID' }, { key: 'status', label: 'Trạng thái', sortable: true }]
const sortedEmployees = computed(() => [...employees.value].sort((a,b) => { const av=sortKey.value==='status'?Number(a.status):String(a.fullName||''); const bv=sortKey.value==='status'?Number(b.status):String(b.fullName||''); return (typeof av==='string'?av.localeCompare(bv,'vi'):av-bv)*(sortDirection.value==='asc'?1:-1) }))
const totalPages = computed(() => Math.max(1, Math.ceil(sortedEmployees.value.length / pageSize))); const paginatedEmployees = computed(() => sortedEmployees.value.slice((currentPage.value-1)*pageSize,currentPage.value*pageSize)); const pagStart = computed(() => employees.value.length ? (currentPage.value-1)*pageSize+1 : 0); const pagEnd = computed(() => Math.min(currentPage.value*pageSize,employees.value.length))
const activeCount = computed(() => employeeSummary.value.activeEmployees || employees.value.filter(item=>item.status).length); const inactiveCount = computed(() => employeeSummary.value.inactiveEmployees || employees.value.filter(item=>!item.status).length); const hasActiveFilters = computed(() => Boolean(searchQuery.value || filterStatus.value)); const nameError = computed(() => empNameValidation.touched && !empNameValidation.isValid ? empNameValidation.error : '')
const formState = computed(() => JSON.stringify({ ...modalForm, uploadMode: uploadMode.value, face: faceFile.value?.name || facePreview.value })); const formDirty = computed(() => showModal.value && formState.value !== formBaseline.value)

function applyQuery(){searchQuery.value=String(route.query.search||'');filterStatus.value=['true','false'].includes(String(route.query.status))?String(route.query.status):'';currentPage.value=Math.max(1,Number(route.query.page)||1);sortKey.value=['fullName','status'].includes(String(route.query.sort))?String(route.query.sort):'fullName';sortDirection.value=route.query.direction==='desc'?'desc':'asc'}
function commitFilters(){router.replace({query:{...route.query,search:searchQuery.value.trim()||undefined,status:filterStatus.value||undefined,page:undefined}})}
function debouncedCommitFilters(){clearTimeout(searchTimer);searchTimer=setTimeout(commitFilters,350)}
function clearFilters(){searchQuery.value='';filterStatus.value='';commitFilters()}
function setPage(page){router.replace({query:{...route.query,page:page>1?page:undefined}})}
function handleSort(key){const direction=sortKey.value===key&&sortDirection.value==='asc'?'desc':'asc';router.replace({query:{...route.query,sort:key==='fullName'?undefined:key,direction:direction==='asc'?undefined:direction}})}
async function fetchEmployees(){loading.value=true;loadError.value='';permissionDenied.value=false;try{const params={};if(searchQuery.value.trim())params.search=searchQuery.value.trim();if(filterStatus.value!=='')params.status=filterStatus.value==='true';const response=await getAll(params);employees.value=response.data||[];await hydrateEmployeeAvatars(employees.value);if(currentPage.value>totalPages.value)setPage(totalPages.value)}catch(err){if(err.response?.status===403)permissionDenied.value=true;else loadError.value=err.response?.data?.message||'Không thể tải danh sách nhân viên.'}finally{loading.value=false}}
async function fetchEmployeeSummary(){try{const data=await getEmployeeSummary();employeeSummary.value={totalEmployees:data?.totalEmployees||0,activeEmployees:data?.activeEmployees||0,inactiveEmployees:data?.inactiveEmployees||0}}catch{employeeSummary.value={totalEmployees:employees.value.length,activeEmployees:employees.value.filter(item=>item.status).length,inactiveEmployees:employees.value.filter(item=>!item.status).length}}}
function resetValidation(){Object.assign(empNameValidation,{touched:false,isValid:false,error:''})}
function runNameValidation(){const value=modalForm.fullName?.trim();if(!value){empNameValidation.isValid=false;empNameValidation.error='Họ và tên là bắt buộc.';return}const result=validateVietnameseName(value);empNameValidation.isValid=result.isValid;empNameValidation.error=result.error}
function snapshotForm(){formBaseline.value=formState.value}
function openCreateModal(){isEditing.value=false;editingId.value=null;modalError.value='';faceFile.value=null;facePreview.value='';uploadMode.value='file';urlError.value=false;resetValidation();Object.assign(modalForm,{fullName:'',phone:'',email:'',departmentId:null,positionId:null,status:true,faceImageUrl:''});showModal.value=true;queueMicrotask(snapshotForm)}
function openEditModal(emp){isEditing.value=true;editingId.value=emp.employeeId;modalError.value='';faceFile.value=null;urlError.value=false;const isUrl=/^https?:\/\//.test(emp.faceImageUrl||'');uploadMode.value=isUrl?'url':'file';facePreview.value=emp.faceImageUrl&&!isUrl?getEmployeeAvatarSrc(emp):'';Object.assign(modalForm,{fullName:emp.fullName,phone:emp.phone||'',email:emp.email||'',departmentId:emp.departmentId||null,positionId:emp.positionId||null,status:emp.status??true,faceImageUrl:isUrl?emp.faceImageUrl:''});resetValidation();runNameValidation();showModal.value=true;queueMicrotask(snapshotForm)}
function requestCloseModal(){if(formDirty.value){showDiscardDialog.value=true;return}closeModal(true)}
function closeModal(force=false){if(!force&&formDirty.value)return requestCloseModal();showModal.value=false;showDiscardDialog.value=false;modalError.value='';faceFile.value=null;if(facePreview.value?.startsWith('blob:'))URL.revokeObjectURL(facePreview.value);facePreview.value=''}
async function handleSubmit(){empNameValidation.touched=true;runNameValidation();if(!empNameValidation.isValid)return;saving.value=true;modalError.value='';try{modalForm.fullName=normalizeVietnameseName(modalForm.fullName);const data={fullName:modalForm.fullName,phone:modalForm.phone||null,email:modalForm.email||null,departmentId:modalForm.departmentId||null,positionId:modalForm.positionId||null};if(uploadMode.value==='url')data.faceImageUrl=modalForm.faceImageUrl||'';else if(!faceFile.value&&!facePreview.value)data.faceImageUrl='';let id=editingId.value;if(isEditing.value){data.status=modalForm.status;await update(id,data)}else{data.status=true;id=(await create(data)).data.employeeId}if(uploadMode.value==='file'&&faceFile.value&&id)await uploadFace(id,faceFile.value);closeModal(true);success(isEditing.value?'Đã lưu thay đổi':'Đã tạo hồ sơ nhân viên');await Promise.all([fetchEmployees(),fetchEmployeeSummary()])}catch(err){modalError.value=err.response?.data?.message||'Không thể lưu hồ sơ. Dữ liệu bạn nhập vẫn được giữ lại.'}finally{saving.value=false}}
function handleFaceSelect(event){const file=event.target.files?.[0];event.target.value='';if(!file)return;if(facePreview.value?.startsWith('blob:'))URL.revokeObjectURL(facePreview.value);faceFile.value=file;facePreview.value=URL.createObjectURL(file)}
function handleDrop(event){const file=event.dataTransfer.files?.[0];if(file?.type.startsWith('image/'))handleFaceSelect({target:{files:[file],value:''}})}
function removeFace(){faceFile.value=null;if(facePreview.value?.startsWith('blob:'))URL.revokeObjectURL(facePreview.value);facePreview.value=''}
function requestFaceUpload(employee){faceUploadTarget.value=employee;faceUploadInput.value?.click()}
async function handleFaceUpload(event){const file=event.target.files?.[0];event.target.value='';if(!file||!faceUploadTarget.value)return;try{await uploadFace(faceUploadTarget.value.employeeId,file);success('Đã cập nhật Face ID');await fetchEmployees()}catch{showError('Không thể cập nhật Face ID','Tệp ảnh chưa được lưu. Vui lòng thử lại.')}finally{faceUploadTarget.value=null}}
function confirmDelete(employee){deleteTarget.value=employee;showDeleteModal.value=true}
async function handleDelete(){if(!deleteTarget.value)return;saving.value=true;try{await deleteEmployee(deleteTarget.value.employeeId);showDeleteModal.value=false;success('Đã xóa hồ sơ nhân viên');await Promise.all([fetchEmployees(),fetchEmployeeSummary()])}catch(err){showError('Không thể xóa nhân viên',err.response?.data?.message||'Máy chủ không xử lý được yêu cầu.')}finally{saving.value=false}}
function onImportComplete(result){showImportModal.value=false;Promise.all([fetchEmployees(),fetchEmployeeSummary()]);const message=`${result.successCount} bản ghi thành công${result.errorCount?`, ${result.errorCount} lỗi`:''}`;result.errorCount?showError('Import hoàn tất có lỗi',message):success('Import hoàn tất',message)}
function getInitials(name){return name?name.split(' ').filter(Boolean).slice(-2).map(word=>word[0]).join('').toUpperCase():'?'}
async function hydrateEmployeeAvatars(list){releaseProtectedAvatars();const entries=await Promise.all((list||[]).map(async employee=>{if(!employee?.employeeId||!employee.faceImageUrl||/^https?:\/\//.test(employee.faceImageUrl))return[employee?.employeeId,''];try{return[employee.employeeId,URL.createObjectURL((await getProtectedFaceImage(employee.employeeId)).data)]}catch{return[employee.employeeId,'']}}));protectedAvatarUrls.value=Object.fromEntries(entries.filter(([id])=>id))}
function getEmployeeAvatarSrc(employee){if(!employee?.faceImageUrl||brokenEmployeeAvatarIds.value[employee.employeeId])return'';return /^https?:\/\//.test(employee.faceImageUrl)?employee.faceImageUrl:protectedAvatarUrls.value[employee.employeeId]||''}
function markEmployeeAvatarBroken(id,event){if(event?.target)event.target.hidden=true;brokenEmployeeAvatarIds.value={...brokenEmployeeAvatarIds.value,[id]:true}}
function releaseProtectedAvatars(){Object.values(protectedAvatarUrls.value).forEach(url=>url&&URL.revokeObjectURL(url));protectedAvatarUrls.value={}}
function handleBeforeUnload(event){if(!formDirty.value)return;event.preventDefault();event.returnValue=''}
onMounted(async()=>{applyQuery();window.addEventListener('beforeunload',handleBeforeUnload);try{const[d,p]=await Promise.all([getDepartments(),getPositions()]);departments.value=d.data;positions.value=p.data}catch{}await Promise.all([fetchEmployees(),fetchEmployeeSummary()])})
onBeforeUnmount(()=>{clearTimeout(searchTimer);window.removeEventListener('beforeunload',handleBeforeUnload);releaseProtectedAvatars();if(facePreview.value?.startsWith('blob:'))URL.revokeObjectURL(facePreview.value)})
watch(()=>route.query,async()=>{applyQuery();await fetchEmployees()},{deep:true})
</script>

<style scoped>
.employees-page{display:grid;gap:var(--space-6)}
.summary-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:var(--space-4)}
.summary-grid span,.summary-grid small{display:block;color:var(--text-secondary);font-size:var(--type-caption-size);line-height:var(--type-caption-line)}
.summary-grid strong{display:block;margin-block:var(--space-2);font-size:var(--type-h2-size);line-height:var(--type-h2-line)}
.list-panel{display:grid;gap:var(--space-4)}
.list-toolbar{display:flex;align-items:flex-end;justify-content:space-between;gap:var(--space-4)}
.list-toolbar h2{font-size:var(--type-h2-size);line-height:var(--type-h2-line)}.list-toolbar p{color:var(--text-muted);font-size:var(--type-body-size)}
.filter-bar{display:grid;grid-template-columns:minmax(240px,1fr) minmax(170px,220px) auto auto;gap:var(--space-2);width:min(100%,760px)}
.identity-cell{display:flex;align-items:center;gap:var(--space-3)}.identity-cell>span:last-child,.stacked-cell{display:grid;gap:var(--space-1)}.identity-cell small,.stacked-cell small{color:var(--text-muted)}
.avatar{width:40px;height:40px;flex:0 0 40px;border-radius:var(--radius-pill);object-fit:cover}.avatar-fallback{display:grid;place-items:center;color:var(--text-on-interactive);font-weight:800;background:var(--interactive-primary)}.tone-1{background:var(--interactive-secondary)}.tone-2{background:var(--status-success-text)}.tone-3{background:var(--status-warning-text)}.tone-4{background:var(--status-danger-text)}
.row-actions,.pagination-actions,.mode-actions{display:flex;align-items:center;gap:var(--space-2);white-space:nowrap}.pagination-bar{display:flex;align-items:center;justify-content:space-between;gap:var(--space-4);color:var(--text-secondary);font-size:var(--type-body-size)}
.employee-form{display:grid;gap:var(--space-5)}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:var(--space-4)}
.face-section{display:grid;gap:var(--space-3);padding:var(--space-4);border:1px solid var(--border-subtle);border-radius:var(--radius-card)}.face-section legend{padding-inline:var(--space-2);font-weight:700}
.face-upload{display:grid;gap:var(--space-2)}.dropzone{min-height:140px;display:grid;place-items:center;padding:var(--space-4);border:1px dashed var(--border-strong);border-radius:var(--radius-control);background:var(--surface-subtle);color:var(--text-secondary)}.dropzone:hover{background:var(--surface-hover)}.dropzone img,.url-preview{max-height:180px;margin-inline:auto;border-radius:var(--radius-control);object-fit:contain}
.form-error{padding:var(--space-3);border:1px solid var(--status-danger-border);border-radius:var(--radius-control);background:var(--status-danger-bg);color:var(--status-danger-text);font-size:var(--type-body-size)}
@media(max-width:1024px){.list-toolbar{align-items:stretch;display:grid}.filter-bar{width:100%}}
@media(max-width:768px){.summary-grid{grid-template-columns:1fr}.filter-bar,.form-grid{grid-template-columns:1fr}.pagination-bar{align-items:flex-start;display:grid}.pagination-actions{flex-wrap:wrap}.row-actions{flex-wrap:wrap}}
</style>
