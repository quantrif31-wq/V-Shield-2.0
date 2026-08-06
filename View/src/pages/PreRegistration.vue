<template>
  <div class="page-container visitor-page">
    <PageHeader
      title="Đăng ký khách trước"
      description="Theo dõi yêu cầu, phê duyệt khách và phát hành đường dẫn đăng ký an toàn."
      :breadcrumbs="[{ label: 'Khách' }, { label: 'Đăng ký trước' }]"
    >
      <template #actions>
        <BaseButton variant="secondary" :loading="isLoading" @click="refreshPage">Làm mới</BaseButton>
        <BaseButton @click="openCreateLink">Tạo link đăng ký</BaseButton>
      </template>
    </PageHeader>

    <section class="summary-grid" aria-label="Tổng quan đăng ký khách">
      <BaseCard variant="kpi"><span>Tổng yêu cầu</span><strong>{{ stats.total }}</strong><small>Tất cả đăng ký đã nhận</small></BaseCard>
      <BaseCard variant="kpi"><span>Chờ duyệt</span><strong>{{ stats.pending }}</strong><small>Cần xử lý bởi lễ tân</small></BaseCard>
      <BaseCard variant="kpi"><span>Đã duyệt</span><strong>{{ stats.approved }}</strong><small>Sẵn sàng tiếp đón</small></BaseCard>
      <BaseCard variant="kpi"><span>Đã từ chối</span><strong>{{ stats.rejected }}</strong><small>Không được cấp quyền</small></BaseCard>
    </section>

    <section class="list-panel" aria-labelledby="visitor-list-title">
      <div class="list-toolbar">
        <div><h2 id="visitor-list-title">Danh sách yêu cầu</h2><p>Hiển thị {{ registrations.length }} trong {{ totalItems }} yêu cầu</p></div>
        <div class="filter-bar" role="search">
          <BaseInput id="visitor-search" v-model="searchQuery" type="search" aria-label="Tìm khách" placeholder="Tên khách hoặc số điện thoại" @input="debouncedCommitFilters" />
          <BaseSelect id="visitor-status" v-model="filterStatus" aria-label="Lọc trạng thái" @update:model-value="commitFilters">
            <option value="">Tất cả trạng thái</option><option value="Pending">Chờ duyệt</option><option value="Approved">Đã duyệt</option><option value="Rejected">Đã từ chối</option>
          </BaseSelect>
          <BaseInput id="visitor-date" v-model="filterDate" type="date" aria-label="Lọc theo ngày" @input="commitFilters" />
          <BaseButton v-if="hasActiveFilters" variant="ghost" @click="clearFilters">Xóa lọc</BaseButton>
        </div>
      </div>

      <DataTable
        :columns="columns"
        :rows="registrations"
        row-key="registrationId"
        :loading="isLoading"
        :error="loadError"
        :permission-denied="permissionDenied"
        empty-title="Chưa có yêu cầu đăng ký"
        empty-description="Tạo đường dẫn và gửi cho khách để bắt đầu quy trình đăng ký."
      >
        <template #retry><BaseButton variant="secondary" @click="refreshPage">Thử lại</BaseButton></template>
        <template #empty-actions><BaseButton @click="openCreateLink">Tạo link đăng ký</BaseButton></template>
        <template #cell-guest="{ row }"><div class="identity-cell"><span class="avatar" :class="`tone-${tone(row.guestFullName)}`" aria-hidden="true">{{ getInitials(row.guestFullName) }}</span><span><strong>{{ row.guestFullName || 'Chưa cung cấp tên' }}</strong><small>{{ row.guestPhone || 'Chưa có số điện thoại' }}</small></span></div></template>
        <template #cell-schedule="{ row }"><div class="stacked-cell"><strong>{{ row.hostEmployeeName || 'Chưa chọn host' }}</strong><small>{{ formatDateTime(row.expectedTimeIn) }} – {{ formatDateTime(row.expectedTimeOut) }}</small></div></template>
        <template #cell-numberOfVisitors="{ value }">{{ value || 1 }} khách</template>
        <template #cell-status="{ row }"><StatusBadge :status="statusSemantic(row.status)" :label="getStatusLabel(row.status)" dot /></template>
        <template #actions="{ row }"><div class="row-actions"><BaseButton variant="ghost" size="small" @click="viewDetail(row.registrationId)">Chi tiết</BaseButton><BaseButton v-if="row.status === 'Pending'" variant="ghost" size="small" @click="handleUpdateStatus(row.registrationId, 'Approved')">Duyệt</BaseButton><BaseButton v-if="row.status === 'Pending'" variant="ghost" size="small" @click="requestReject(row)">Từ chối</BaseButton></div></template>
      </DataTable>

      <footer v-if="!isLoading && !loadError && registrations.length" class="pagination-bar">
        <span>Trang {{ currentPage }} / {{ totalPages }}</span>
        <div class="pagination-actions" aria-label="Phân trang"><BaseButton variant="secondary" size="small" :disabled="currentPage <= 1" @click="setPage(currentPage - 1)">Trang trước</BaseButton><BaseButton variant="secondary" size="small" :disabled="currentPage >= totalPages" @click="setPage(currentPage + 1)">Trang sau</BaseButton></div>
      </footer>
    </section>

    <BaseModal :open="showDetailModal" :title="`Chi tiết đăng ký #${detail?.registrationId || ''}`" description="Thông tin khách, đoàn đi cùng và lịch sử kiểm soát." @close="closeDetail">
      <LoadingSkeleton v-if="isLoadingDetail" variant="card" :lines="5" />
      <EmptyState v-else-if="detailError" kind="error" title="Không thể tải chi tiết" :description="detailError"><template #actions><BaseButton variant="secondary" @click="viewDetail(detailId)">Thử lại</BaseButton></template></EmptyState>
      <div v-else-if="detail" class="detail-content">
        <dl class="detail-grid">
          <div><dt>Khách đại diện</dt><dd>{{ detail.guestFullName || '—' }}</dd></div><div><dt>Liên hệ</dt><dd>{{ detail.guestPhone || '—' }}</dd></div>
          <div><dt>Nhân sự host</dt><dd>{{ detail.hostEmployeeName || '—' }}</dd></div><div><dt>Trạng thái</dt><dd><StatusBadge :status="statusSemantic(detail.status)" :label="getStatusLabel(detail.status)" dot /></dd></div>
          <div class="wide"><dt>Thời gian dự kiến</dt><dd>{{ formatDateTime(detail.expectedTimeIn) }} – {{ formatDateTime(detail.expectedTimeOut) }}</dd></div>
        </dl>

        <section v-if="detail.visitors?.length" class="detail-section"><h3>Đoàn khách đi cùng ({{ detail.visitors.length }})</h3><article v-for="(visitor, index) in detail.visitors" :key="visitor.visitorId || index" :ref="element => setQrCardRef(element, index, visitor.visitorPortalUrl || visitor.qrCodeData)" class="visitor-card"><div class="identity-cell"><span class="avatar" :class="`tone-${tone(visitor.fullName)}`">{{ getInitials(visitor.fullName) }}</span><span><strong>{{ visitor.fullName }}</strong><small>CCCD: {{ visitor.idCardNumber || '—' }}</small></span></div><div v-if="visitor.visitorPortalUrl || visitor.qrCodeData" class="qr-area"><canvas width="96" height="96" :aria-label="`QR của ${visitor.fullName}`"></canvas><div><small>QR truy cập động</small><div class="row-actions"><BaseButton variant="secondary" size="small" @click="downloadVisitorQr(index, visitor)">Tải QR</BaseButton><BaseButton variant="ghost" size="small" @click="copyText(visitor.visitorPortalUrl || visitor.qrCodeData)">Sao chép</BaseButton></div></div></div></article></section>

        <section v-if="detail.accessLogs?.length" class="detail-section"><h3>Lịch sử ra vào</h3><ol class="timeline"><li v-for="log in detail.accessLogs" :key="log.logId"><StatusBadge :status="log.direction === 'IN' ? 'success' : 'info'" :label="log.direction === 'IN' ? 'Vào' : 'Ra'" /><span>{{ formatDateTime(log.timestamp) }}</span><small v-if="log.capturedLicensePlate">{{ log.capturedLicensePlate }}</small></li></ol></section>
      </div>
      <template v-if="detail?.status === 'Pending'" #footer><BaseButton variant="secondary" :disabled="statusSaving" @click="requestReject(detail)">Từ chối</BaseButton><BaseButton :loading="statusSaving" @click="handleUpdateStatus(detail.registrationId, 'Approved', true)">Duyệt đăng ký</BaseButton></template>
    </BaseModal>

    <BaseModal :open="showCreateLinkModal" title="Tạo link đăng ký" description="Chọn host chịu trách nhiệm và thời hạn sử dụng của đường dẫn." @close="requestCloseCreateLink">
      <form v-if="!createdLink" id="visitor-link-form" class="link-form" @submit.prevent="handleCreateLink">
        <BaseField for-id="visitor-host" label="Nhân sự host" required :error="linkSubmitted && !linkForm.hostEmployeeId ? 'Vui lòng chọn nhân sự host.' : ''" v-slot="field">
          <BaseSelect id="visitor-host" v-model="linkForm.hostEmployeeId" :describedby="field.describedby" :invalid="field.invalid" :loading="employeesLoading"><option value="">Chọn nhân sự</option><option v-for="employee in employees" :key="employee.employeeId" :value="employee.employeeId">{{ employee.fullName }}{{ employee.departmentName ? ` · ${employee.departmentName}` : '' }}</option></BaseSelect>
        </BaseField>
        <BaseField for-id="visitor-expiry" label="Thời gian hiệu lực (giờ)" required hint="Từ 1 đến 168 giờ." :error="expiryError" v-slot="field"><BaseInput id="visitor-expiry" v-model="linkForm.expiryHours" type="number" min="1" max="168" :describedby="field.describedby" :invalid="field.invalid" /></BaseField>
        <p v-if="linkError" class="form-error" role="alert">{{ linkError }}</p>
      </form>
      <div v-else class="success-state" role="status"><StatusBadge status="approved" label="Tạo đường dẫn thành công" dot /><p>Hiệu lực đến {{ formatDateTime(createdLink.expiredAt) }}</p><BaseField for-id="created-registration-link" label="Đường dẫn đăng ký" v-slot="field"><BaseInput id="created-registration-link" :model-value="createdLink.registrationUrl" readonly :describedby="field.describedby" /></BaseField><div class="created-actions"><BaseButton @click="copyText(createdLink.registrationUrl)">{{ copied ? 'Đã sao chép' : 'Sao chép link' }}</BaseButton><BaseButton variant="secondary" @click="openCreatedLink">Mở tab mới</BaseButton></div></div>
      <template #footer><template v-if="!createdLink"><BaseButton variant="secondary" :disabled="isCreatingLink" @click="requestCloseCreateLink">Hủy</BaseButton><BaseButton type="submit" form="visitor-link-form" :loading="isCreatingLink">Tạo link</BaseButton></template><BaseButton v-else variant="secondary" @click="closeCreateLink(true)">Đóng</BaseButton></template>
    </BaseModal>

    <ConfirmDialog :open="showRejectDialog" kind="destructive" title="Từ chối đăng ký?" :description="`Yêu cầu của ${rejectTarget?.guestFullName || 'khách'} sẽ bị từ chối và không được cấp quyền truy cập.`" confirm-label="Từ chối yêu cầu" :loading="statusSaving" @cancel="showRejectDialog = false" @confirm="confirmReject" />
    <ConfirmDialog :open="showDiscardDialog" title="Bỏ thông tin đang nhập?" description="Host và thời hạn link chưa được lưu sẽ bị mất." confirm-label="Bỏ thay đổi" @cancel="showDiscardDialog = false" @confirm="closeCreateLink(true)" />
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import QRCode from 'qrcode'
import { createLink, getAll, getDetail, updateStatus } from '../services/preRegistrationApi'
import { getAll as getAllEmployees } from '../services/employeeApi'
import BaseButton from '../components/ui/BaseButton.vue'
import BaseCard from '../components/ui/BaseCard.vue'
import BaseField from '../components/ui/BaseField.vue'
import BaseInput from '../components/ui/BaseInput.vue'
import BaseModal from '../components/ui/BaseModal.vue'
import BaseSelect from '../components/ui/BaseSelect.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'
import DataTable from '../components/ui/DataTable.vue'
import EmptyState from '../components/ui/EmptyState.vue'
import LoadingSkeleton from '../components/ui/LoadingSkeleton.vue'
import PageHeader from '../components/ui/PageHeader.vue'
import StatusBadge from '../components/ui/StatusBadge.vue'
import { useToasts } from '../composables/useToasts'

const route = useRoute(); const router = useRouter(); const { success, error: showError } = useToasts()
const registrations = ref([]); const totalItems = ref(0); const currentPage = ref(1); const pageSize = 10; const totalPages = ref(1)
const isLoading = ref(true); const loadError = ref(''); const permissionDenied = ref(false); const searchQuery = ref(''); const filterStatus = ref(''); const filterDate = ref('')
const stats = reactive({ total: 0, pending: 0, approved: 0, rejected: 0 }); const detail = ref(null); const detailId = ref(null); const isLoadingDetail = ref(false); const detailError = ref(''); const showDetailModal = ref(false)
const employees = ref([]); const employeesLoading = ref(false); const showCreateLinkModal = ref(false); const createdLink = ref(null); const isCreatingLink = ref(false); const linkError = ref(''); const linkSubmitted = ref(false); const copied = ref(false); const linkForm = reactive({ hostEmployeeId: '', expiryHours: 24 }); const formBaseline = ref('')
const showRejectDialog = ref(false); const rejectTarget = ref(null); const statusSaving = ref(false); const showDiscardDialog = ref(false); const qrCardRefs = ref([]); let searchTimer = null; let copiedTimer = null
const columns = [{ key: 'guest', label: 'Khách' }, { key: 'schedule', label: 'Host / Thời gian' }, { key: 'numberOfVisitors', label: 'Quy mô' }, { key: 'status', label: 'Trạng thái' }]
const hasActiveFilters = computed(() => Boolean(searchQuery.value || filterStatus.value || filterDate.value)); const expiryError = computed(() => { const value=Number(linkForm.expiryHours); return linkSubmitted.value && (!Number.isFinite(value)||value<1||value>168) ? 'Thời hạn phải từ 1 đến 168 giờ.' : '' }); const formState = computed(() => JSON.stringify(linkForm)); const formDirty = computed(() => showCreateLinkModal.value && !createdLink.value && formState.value !== formBaseline.value)

function applyQuery(){searchQuery.value=String(route.query.search||'');filterStatus.value=['Pending','Approved','Rejected'].includes(String(route.query.status))?String(route.query.status):'';filterDate.value=String(route.query.date||'');currentPage.value=Math.max(1,Number(route.query.page)||1)}
function commitFilters(){router.replace({query:{...route.query,search:searchQuery.value.trim()||undefined,status:filterStatus.value||undefined,date:filterDate.value||undefined,page:undefined}})}
function debouncedCommitFilters(){clearTimeout(searchTimer);searchTimer=setTimeout(commitFilters,350)}
function clearFilters(){searchQuery.value='';filterStatus.value='';filterDate.value='';commitFilters()}
function setPage(page){router.replace({query:{...route.query,page:page>1?page:undefined}})}
async function fetchRegistrations(){isLoading.value=true;loadError.value='';permissionDenied.value=false;try{const params={page:currentPage.value,pageSize};if(filterStatus.value)params.status=filterStatus.value;if(filterDate.value)params.date=filterDate.value;const {data}=await getAll(params);let items=data.items||[];if(searchQuery.value.trim()){const q=searchQuery.value.trim().toLocaleLowerCase('vi');items=items.filter(item=>String(item.guestFullName||'').toLocaleLowerCase('vi').includes(q)||String(item.guestPhone||'').includes(q))}registrations.value=items;totalItems.value=data.total||0;totalPages.value=Math.max(1,Math.ceil(totalItems.value/pageSize));if(currentPage.value>totalPages.value)setPage(totalPages.value)}catch(err){registrations.value=[];if(err.response?.status===403)permissionDenied.value=true;else loadError.value=err.response?.data?.message||'Không thể tải danh sách đăng ký khách.'}finally{isLoading.value=false}}
async function fetchStats(){try{const responses=await Promise.all([getAll({pageSize:1}),getAll({status:'Pending',pageSize:1}),getAll({status:'Approved',pageSize:1}),getAll({status:'Rejected',pageSize:1})]);Object.assign(stats,{total:responses[0].data.total||0,pending:responses[1].data.total||0,approved:responses[2].data.total||0,rejected:responses[3].data.total||0})}catch{Object.assign(stats,{total:totalItems.value,pending:0,approved:0,rejected:0})}}
async function refreshPage(){await Promise.all([fetchRegistrations(),fetchStats()])}
async function fetchEmployees(){employeesLoading.value=true;try{employees.value=(await getAllEmployees()).data||[]}catch{showError('Không thể tải danh sách host','Bạn vẫn có thể đóng hộp thoại và thử lại.')}finally{employeesLoading.value=false}}
async function viewDetail(id){detailId.value=id;showDetailModal.value=true;isLoadingDetail.value=true;detailError.value='';detail.value=null;try{detail.value=(await getDetail(id)).data}catch(err){detailError.value=err.response?.status===403?'Bạn không có quyền xem chi tiết đăng ký này.':err.response?.data?.message||'Máy chủ không trả về dữ liệu chi tiết.'}finally{isLoadingDetail.value=false}}
function closeDetail(){showDetailModal.value=false;detail.value=null;detailError.value='';qrCardRefs.value=[]}
function requestReject(item){rejectTarget.value=item;showRejectDialog.value=true}
async function confirmReject(){if(!rejectTarget.value)return;const id=rejectTarget.value.registrationId;showRejectDialog.value=false;await handleUpdateStatus(id,'Rejected',detail.value?.registrationId===id)}
async function handleUpdateStatus(id,status,closeAfter=false){statusSaving.value=true;try{await updateStatus(id,status);success(status==='Approved'?'Đã duyệt đăng ký':'Đã từ chối đăng ký');if(closeAfter)closeDetail();await refreshPage()}catch(err){showError('Không thể cập nhật trạng thái',err.response?.data?.message||'Yêu cầu chưa được thay đổi.')}finally{statusSaving.value=false}}
function openCreateLink(){showCreateLinkModal.value=true;createdLink.value=null;linkError.value='';linkSubmitted.value=false;Object.assign(linkForm,{hostEmployeeId:'',expiryHours:24});queueMicrotask(()=>{formBaseline.value=formState.value});if(!employees.value.length)fetchEmployees()}
function requestCloseCreateLink(){if(formDirty.value){showDiscardDialog.value=true;return}closeCreateLink(true)}
function closeCreateLink(force=false){if(!force&&formDirty.value)return requestCloseCreateLink();showCreateLinkModal.value=false;showDiscardDialog.value=false;createdLink.value=null;linkError.value='';copied.value=false;Object.assign(linkForm,{hostEmployeeId:'',expiryHours:24})}
async function handleCreateLink(){linkSubmitted.value=true;const expiry=Number(linkForm.expiryHours);if(!linkForm.hostEmployeeId||!Number.isFinite(expiry)||expiry<1||expiry>168)return;isCreatingLink.value=true;linkError.value='';try{createdLink.value=(await createLink({hostEmployeeId:Number(linkForm.hostEmployeeId),expiryHours:expiry})).data;success('Đã tạo link đăng ký')}catch(err){linkError.value=err.response?.data?.message||'Không thể tạo link. Thông tin bạn nhập vẫn được giữ lại.'}finally{isCreatingLink.value=false}}
async function copyText(value){try{await navigator.clipboard.writeText(value);copied.value=true;success('Đã sao chép vào clipboard');clearTimeout(copiedTimer);copiedTimer=setTimeout(()=>{copied.value=false},2000)}catch{showError('Không thể sao chép','Hãy chọn và sao chép đường dẫn thủ công.')}}
function openCreatedLink(){if(createdLink.value?.registrationUrl)window.open(createdLink.value.registrationUrl,'_blank','noopener,noreferrer')}
function getInitials(name){return String(name||'?').trim().split(/\s+/).filter(Boolean).slice(-2).map(word=>word[0]).join('').toUpperCase()}
function tone(value){return [...String(value||'')].reduce((sum,char)=>sum+char.charCodeAt(0),0)%5}
function formatDateTime(value){if(!value)return '—';const date=new Date(value);return Number.isNaN(date.getTime())?'—':new Intl.DateTimeFormat('vi-VN',{dateStyle:'short',timeStyle:'short'}).format(date)}
function getStatusLabel(status){return {Pending:'Chờ duyệt',Approved:'Đã duyệt',Rejected:'Đã từ chối'}[status]||status||'Không xác định'}
function statusSemantic(status){return {Pending:'pending',Approved:'approved',Rejected:'rejected'}[status]||'neutral'}
async function renderQr(text,canvas){if(canvas&&text)try{await QRCode.toCanvas(canvas,text,{width:96,margin:2})}catch{showError('Không thể tạo QR','Dữ liệu QR không hợp lệ.')}}
function setQrCardRef(element,index,text){if(!element)return;qrCardRefs.value[index]=element;renderQr(text,element.querySelector('canvas'))}
function safeFileName(value){return String(value||'visitor').replace(/[\\/:*?"<>|]+/g,'_').replace(/\s+/g,'_').slice(0,80)}
function downloadVisitorQr(index,visitor){const canvas=qrCardRefs.value[index]?.querySelector('canvas');if(!canvas)return showError('Không thể tải QR','Mã QR chưa được tạo.');const anchor=document.createElement('a');anchor.href=canvas.toDataURL('image/png');anchor.download=`${safeFileName(visitor.fullName)}_QR.png`;anchor.click()}
function beforeUnload(event){if(!formDirty.value)return;event.preventDefault();event.returnValue=''}
onMounted(async()=>{applyQuery();window.addEventListener('beforeunload',beforeUnload);await Promise.all([refreshPage(),fetchEmployees()])})
onBeforeUnmount(()=>{clearTimeout(searchTimer);clearTimeout(copiedTimer);window.removeEventListener('beforeunload',beforeUnload)})
watch(()=>route.query,async()=>{applyQuery();await fetchRegistrations()},{deep:true})
</script>

<style scoped>
.visitor-page{display:grid;gap:var(--space-6)}
.summary-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:var(--space-4)}
.summary-grid span,.summary-grid small{display:block;color:var(--text-secondary);font-size:var(--type-caption-size);line-height:var(--type-caption-line)}.summary-grid strong{display:block;margin-block:var(--space-2);font-size:var(--type-h2-size);line-height:var(--type-h2-line)}
.list-panel{display:grid;gap:var(--space-4)}.list-toolbar{display:flex;align-items:flex-end;justify-content:space-between;gap:var(--space-4)}.list-toolbar h2{font-size:var(--type-h2-size);line-height:var(--type-h2-line)}.list-toolbar p{color:var(--text-muted);font-size:var(--type-body-size)}
.filter-bar{display:grid;grid-template-columns:minmax(220px,1fr) minmax(160px,200px) minmax(150px,180px) auto;gap:var(--space-2);width:min(100%,820px)}
.identity-cell{display:flex;align-items:center;gap:var(--space-3)}.identity-cell>span:last-child,.stacked-cell{display:grid;gap:var(--space-1)}.identity-cell small,.stacked-cell small{color:var(--text-muted)}.avatar{width:40px;height:40px;flex:0 0 40px;display:grid;place-items:center;border-radius:var(--radius-pill);background:var(--interactive-primary);color:var(--text-on-interactive);font-weight:800}.tone-1{background:var(--interactive-secondary)}.tone-2{background:var(--status-success-text)}.tone-3{background:var(--status-warning-text)}.tone-4{background:var(--status-danger-text)}
.row-actions,.pagination-actions,.created-actions{display:flex;align-items:center;gap:var(--space-2);white-space:nowrap}.pagination-bar{display:flex;align-items:center;justify-content:space-between;gap:var(--space-4);color:var(--text-secondary);font-size:var(--type-body-size)}
.detail-content,.link-form,.success-state{display:grid;gap:var(--space-5)}.detail-grid{display:grid;grid-template-columns:1fr 1fr;gap:var(--space-4);margin:0}.detail-grid div{display:grid;gap:var(--space-1);padding:var(--space-3);border:1px solid var(--border-subtle);border-radius:var(--radius-control);background:var(--surface-subtle)}.detail-grid .wide{grid-column:1/-1}.detail-grid dt{color:var(--text-muted);font-size:var(--type-caption-size)}.detail-grid dd{margin:0;font-weight:700}
.detail-section{display:grid;gap:var(--space-3);padding-top:var(--space-4);border-top:1px solid var(--border-subtle)}.detail-section h3{font-size:var(--type-h3-size);line-height:var(--type-h3-line)}.visitor-card{display:flex;align-items:center;justify-content:space-between;gap:var(--space-4);padding:var(--space-3);border:1px solid var(--border-subtle);border-radius:var(--radius-card)}.qr-area{display:flex;align-items:center;gap:var(--space-3)}.qr-area canvas{width:96px;height:96px;padding:var(--space-1);background:var(--surface-default);border:1px solid var(--border-subtle);border-radius:var(--radius-control)}.qr-area>div{display:grid;gap:var(--space-2)}.timeline{display:grid;gap:var(--space-2);margin:0;padding:0;list-style:none}.timeline li{display:grid;grid-template-columns:auto 1fr auto;align-items:center;gap:var(--space-3);padding:var(--space-2) 0}.timeline small{color:var(--text-muted)}
.form-error{padding:var(--space-3);border:1px solid var(--status-danger-border);border-radius:var(--radius-control);background:var(--status-danger-bg);color:var(--status-danger-text);font-size:var(--type-body-size)}.success-state>p{color:var(--text-secondary)}
@media(max-width:1100px){.summary-grid{grid-template-columns:repeat(2,1fr)}.list-toolbar{align-items:stretch;display:grid}.filter-bar{width:100%}}
@media(max-width:768px){.summary-grid,.filter-bar,.detail-grid{grid-template-columns:1fr}.pagination-bar,.visitor-card,.qr-area{align-items:flex-start;display:grid}.row-actions,.created-actions{flex-wrap:wrap}.detail-grid .wide{grid-column:auto}.qr-area canvas{width:80px;height:80px}}
</style>
