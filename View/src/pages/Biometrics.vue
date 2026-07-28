<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Biometrics</span>
                <h1 class="page-title">Dữ liệu sinh trắc học</h1>
            </div>
            <div class="header-actions">
                <router-link to="/employees" class="btn btn-primary">Mở hồ sơ nhân sự</router-link>
                <router-link to="/monitoring" class="btn btn-secondary">Quay lại giám sát</router-link>
            </div>
        </div>

        <section class="metric-grid">
            <article class="metric-tile">
                <span class="metric-label">Tổng nhân sự</span>
                <strong class="metric-value">{{ summary.totalEmployees }}</strong>
                <span class="metric-note">Số nhân sự nội bộ hiện đang có trong hệ thống.</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">File model</span>
                <strong class="metric-value">{{ summary.totalModelFiles }}</strong>
                <span class="metric-note">Tất cả model khuôn mặt đang lưu.</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">File video</span>
                <strong class="metric-value">{{ summary.totalVideoFiles }}</strong>
                <span class="metric-note">Video huấn luyện đang có trong kho dữ liệu.</span>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Thiếu video</span>
                <strong class="metric-value">{{ summary.employeesMissingVideos }}</strong>
                <span class="metric-note">Nhân sự chưa có video để huấn luyện thêm.</span>
            </article>
        </section>

        <section class="ops-panel">
            <div class="toolbar-shell">
                <div class="search-bar">
                    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="11" cy="11" r="8" />
                        <path d="M21 21l-4.35-4.35" />
                    </svg>
                    <input v-model="query" type="text" placeholder="Tìm nhân sự, phòng ban, email..." />
                </div>
            </div>

            <div v-if="isLoading" class="empty-card">Đang tải dữ liệu biometrics...</div>
            <div v-else-if="employees.length === 0" class="empty-card">Không có nhân sự nào khớp với bộ lọc hiện tại.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nhân sự</th>
                            <th>Phòng ban</th>
                            <th>Model</th>
                            <th>Video</th>
                            <th>Cập nhật gần nhất</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="employee in paginatedEmployees" :key="employee.employeeId">
                            <td>
                                <div class="table-main">{{ employee.fullName }}</div>
                                <div class="table-sub">{{ employee.positionName || 'Chưa có chức vụ' }}</div>
                            </td>
                            <td>{{ employee.departmentName || 'Chưa gán phòng ban' }}</td>
                            <td>
                                <div class="chip-row">
                                    <span class="soft-chip" :class="employee.modelCount > 0 ? 'success' : 'danger'">
                                        {{ employee.modelCount }} model
                                    </span>
                                </div>
                            </td>
                            <td>
                                <div class="chip-row">
                                    <span class="soft-chip" :class="employee.videoCount > 0 ? 'success' : 'warn'">
                                        {{ employee.videoCount }} video
                                    </span>
                                </div>
                            </td>
                            <td>{{ latestRecordLabel(employee) }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <div v-if="!isLoading && employees.length > 0" class="pagination-bar">
                <span>Hiển thị {{ bPagStart }}–{{ bPagEnd }} / {{ employees.length }}</span>
                <div class="page-buttons">
                    <button class="page-btn" :disabled="bCurrentPage <= 1" @click="bCurrentPage--">‹</button>
                    <button v-for="p in bTotalPages" :key="p" class="page-btn" :class="{ active: p === bCurrentPage }" @click="bCurrentPage = p">{{ p }}</button>
                    <button class="page-btn" :disabled="bCurrentPage >= bTotalPages" @click="bCurrentPage++">›</button>
                </div>
            </div>
        </section>

        <section class="ops-panel">
            <div class="panel-head">
                <div>
                    <span class="panel-kicker">Enterprise credentials</span>
                    <h2 class="panel-title">Credential nhân viên canonical</h2>
                </div>
                <span class="soft-chip">{{ accessCredentials.length }} credential</span>
            </div>
            <div v-if="accessCredentials.length" class="table-container">
                <table class="data-table">
                    <thead><tr><th>Nhân viên</th><th>Loại</th><th>Stored / effective</th><th>Identifier</th><th>Hiệu lực</th></tr></thead>
                    <tbody>
                        <tr v-for="credential in accessCredentials" :key="credential.id">
                            <td>{{ credential.employeeName }}<div class="table-sub">ID {{ credential.employeeId }}</div></td>
                            <td>{{ credential.credentialType }}</td>
                            <td>{{ credential.storedStatus }} / {{ credential.effectiveStatus }}</td>
                            <td><code>{{ credential.maskedIdentifier || 'Không lưu identifier' }}</code></td>
                            <td>{{ formatDateTime(credential.effectiveFromUtc) }} → {{ formatDateTime(credential.expiresAtUtc) }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div v-else class="empty-card">Chưa có credential nhân viên canonical. QR hiện hữu vẫn hoạt động độc lập.</div>
        </section>

        <section class="ops-panel">
            <div class="panel-head">
                <div>
                    <span class="panel-kicker">Face credential binding</span>
                    <h2 class="panel-title">Liên kết danh tính khuôn mặt với enterprise credential</h2>
                </div>
                <span class="soft-chip">{{ faceCredentialBindings.length }} binding</span>
            </div>
            <div v-if="faceCredentialBindings.length" class="table-container">
                <table class="data-table">
                    <thead><tr><th>Nhân viên</th><th>Credential</th><th>Trạng thái</th><th>Identifier</th><th>Thời gian</th></tr></thead>
                    <tbody>
                        <tr v-for="binding in faceCredentialBindings" :key="binding.id">
                            <td>{{ binding.employeeName }}<div class="table-sub">ID {{ binding.employeeId }}</div></td>
                            <td>{{ binding.credentialType }} #{{ binding.accessCredentialId }}</td>
                            <td>{{ binding.bindingStatus }} · {{ binding.credentialEffectiveStatus }}</td>
                            <td><code>{{ binding.maskedIdentifier || 'Không lưu identifier' }}</code></td>
                            <td>{{ formatDateTime(binding.activatedAtUtc) }} → {{ formatDateTime(binding.revokedAtUtc) }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div v-else class="empty-card">
                Chưa có binding được phê duyệt. Hệ thống không tự chọn credential và không tự bind nhân viên.
            </div>
        </section>

        <section class="ops-panel">
            <div class="panel-head">
                <div>
                    <span class="panel-kicker">Controlled enrollment</span>
                    <h2 class="panel-title">Tạo model từ video đã quản lý</h2>
                </div>
            </div>
            <div class="enrollment-controls">
                <select v-model="selectedEmployeeId" @change="loadEmployeeVideos">
                    <option value="">Chọn nhân viên</option>
                    <option v-for="employee in employees" :key="employee.employeeId" :value="employee.employeeId">
                        {{ employee.fullName }}
                    </option>
                </select>
                <select v-model="selectedVideoId" :disabled="!selectedEmployeeId">
                    <option value="">Chọn video enrollment</option>
                    <option v-for="video in employeeVideos" :key="video.id" :value="video.id">
                        {{ video.fileName }} · {{ formatDateTime(video.createdAt) }}
                    </option>
                </select>
                <button class="btn btn-primary" :disabled="!selectedVideoId || enrollmentBusy" @click="createEnrollment">
                    Tạo enrollment job
                </button>
            </div>
            <div v-if="selectedEmployeeId" class="chip-row">
                <span class="soft-chip" :class="bindingReadiness === 'Ready' ? 'success' : 'warn'">
                    Enterprise readiness: {{ bindingReadiness }}
                </span>
                <span v-if="selectedEmployeeBinding" class="soft-chip">
                    Binding #{{ selectedEmployeeBinding.id }} · {{ selectedEmployeeBinding.credentialEffectiveStatus }}
                </span>
                <span v-for="candidate in selectedEmployeeCandidates" :key="candidate.accessCredentialId" class="soft-chip">
                    {{ candidate.credentialType }} #{{ candidate.accessCredentialId }} · {{ candidate.candidateClassification }}
                </span>
            </div>
            <div v-if="enrollmentJobs.length" class="table-container">
                <table class="data-table">
                    <thead><tr><th>Nhân viên</th><th>Trạng thái</th><th>Chất lượng</th><th>Kết quả</th><th>Thao tác</th></tr></thead>
                    <tbody>
                        <tr v-for="job in enrollmentJobs" :key="job.jobId">
                            <td>{{ job.employeeName }}<div class="table-sub">{{ formatDateTime(job.createdAtUtc) }}</div></td>
                            <td><span class="soft-chip">{{ job.status }}</span><div class="table-sub">Attempt {{ job.attemptCount }}</div></td>
                            <td>{{ job.usableFrameCount ?? '--' }} frame · {{ job.encodingCount ?? '--' }} encoding<div class="table-sub">Quality {{ job.qualityScore ?? '--' }} (metric nội bộ)</div></td>
                            <td>
                                <span v-if="job.duplicateSubjectId" class="soft-chip danger">Trùng subject {{ job.duplicateSubjectId }}</span>
                                <div v-if="job.failureMessage" class="table-sub">{{ job.failureCode }}: {{ job.failureMessage }}</div>
                            </td>
                            <td class="chip-row">
                                <button v-if="job.canActivate" class="btn btn-primary" @click="runJobAction(job, 'activate')">Activate</button>
                                <button v-if="job.canCancel" class="btn btn-secondary" @click="runJobAction(job, 'cancel')">Cancel</button>
                                <button v-if="job.canRetry" class="btn btn-secondary" @click="runJobAction(job, 'retry')">Retry</button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div v-else class="empty-card">Chưa có enrollment job.</div>
        </section>

        <section class="ops-panel">
            <div class="panel-head">
                <div>
                    <span class="panel-kicker">Model lifecycle</span>
                    <h2 class="panel-title">Trạng thái model khuôn mặt</h2>
                </div>
                <span class="soft-chip" :class="modelRuntimeUnavailable ? 'danger' : 'success'">
                    {{ modelRuntimeUnavailable ? 'Runtime unavailable' : `Registry v${registryVersion || '--'}` }}
                </span>
            </div>
            <div v-if="faceModels.length" class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nhân viên</th>
                            <th>Version / trạng thái</th>
                            <th>Encoding</th>
                            <th>Checksum</th>
                            <th>Activated</th>
                            <th>Registry</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="model in faceModels" :key="model.id">
                            <td>{{ model.employeeName }}<div class="table-sub">{{ model.modelFileName }}</div></td>
                            <td>v{{ model.version || '--' }} · {{ model.status || 'Metadata missing' }}</td>
                            <td>{{ model.encodingCount || '--' }}</td>
                            <td><code>{{ model.checksumPrefix || '--' }}</code></td>
                            <td>{{ formatDateTime(model.activatedAtUtc) }}</td>
                            <td><span class="soft-chip" :class="model.registrySyncState === 'Synced' ? 'success' : 'warn'">{{ model.registrySyncState }}</span></td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div v-else class="empty-card">Chưa có metadata vòng đời model.</div>
        </section>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Recent models</span>
                        <h2 class="panel-title">Model mới nhất</h2>
                    </div>
                </div>

                <div v-if="recentModels.length" class="surface-list">
                    <article v-for="model in recentModels" :key="model.id" class="surface-item">
                        <div class="surface-item-title">{{ model.employeeName }}</div>
                        <div class="surface-item-sub">{{ model.modelFileName }}</div>
                        <div class="chip-row">
                            <span class="soft-chip">{{ formatDateTime(model.createdAt) }}</span>
                            <span class="soft-chip success">Protected artifact</span>
                        </div>
                    </article>
                </div>
                <div v-else class="empty-card">Chưa có model nào trong hệ thống.</div>
            </article>

            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Recent videos</span>
                        <h2 class="panel-title">Video mới nhất</h2>
                    </div>
                </div>

                <div v-if="recentVideos.length" class="surface-list">
                    <article v-for="video in recentVideos" :key="video.id" class="surface-item">
                        <div class="surface-item-title">{{ video.employeeName }}</div>
                        <div class="surface-item-sub">{{ video.fileName }} - {{ formatFileSize(video.fileSize) }}</div>
                        <div class="chip-row">
                            <span class="soft-chip">{{ formatDateTime(video.createdAt) }}</span>
                            <span class="soft-chip success">Protected artifact</span>
                        </div>
                    </article>
                </div>
                <div v-else class="empty-card">Chưa có video nào trong hệ thống.</div>
            </article>
        </section>
    </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import {
    getBiometricOverview, getFaceModelHealth, getFaceEnrollmentJobs,
    createFaceEnrollmentJob, cancelFaceEnrollmentJob,
    retryFaceEnrollmentJob, activateFaceEnrollmentJob,
    getAccessCredentials, getFaceCredentialBindings,
    getEmployeeFaceCredentialBinding, getEmployeeFaceCredentialCandidates,
} from '../services/biometricApi'
import { getEmployeeVideos } from '../services/faceVideoApi'

const isLoading = ref(true)
const query = ref('')
const summary = ref({
    totalEmployees: 0,
    trainedEmployees: 0,
    employeesWithVideos: 0,
    employeesMissingModels: 0,
    employeesMissingVideos: 0,
    totalModelFiles: 0,
    totalVideoFiles: 0,
})
const employees = ref([])
const recentModels = ref([])
const recentVideos = ref([])
const faceModels = ref([])
const registryVersion = ref(null)
const enrollmentJobs = ref([])
const accessCredentials = ref([])
const faceCredentialBindings = ref([])
const selectedEmployeeBinding = ref(null)
const selectedEmployeeCandidates = ref([])
const selectedEmployeeId = ref('')
const selectedVideoId = ref('')
const employeeVideos = ref([])
const enrollmentBusy = ref(false)
const modelRuntimeUnavailable = computed(() =>
    faceModels.value.some(model => model.registrySyncState === 'RuntimeUnavailable'))

const bCurrentPage = ref(1)
const bPageSize = 10
const bTotalPages = computed(() => Math.max(1, Math.ceil(employees.value.length / bPageSize)))
const paginatedEmployees = computed(() => {
    const start = (bCurrentPage.value - 1) * bPageSize
    return employees.value.slice(start, start + bPageSize)
})
const bPagStart = computed(() => employees.value.length === 0 ? 0 : (bCurrentPage.value - 1) * bPageSize + 1)
const bPagEnd = computed(() => Math.min(bCurrentPage.value * bPageSize, employees.value.length))

const formatDateTime = (value) => {
    if (!value) return '--'
    return new Date(value).toLocaleString('vi-VN', {
        hour: '2-digit',
        minute: '2-digit',
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
    })
}

const formatFileSize = (value) => {
    if (!value) return '0 B'
    if (value < 1024) return `${value} B`
    if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`
    return `${(value / (1024 * 1024)).toFixed(1)} MB`
}

const latestRecordLabel = (employee) => {
    const latest = employee.latestModelAt || employee.latestVideoAt
    if (!latest) return 'Chưa có dữ liệu'
    return formatDateTime(latest)
}

const fetchOverview = async () => {
    isLoading.value = true
    bCurrentPage.value = 1
    try {
        const [{ data }, modelHealth, jobs, credentials] = await Promise.all([
            getBiometricOverview({ query: query.value || undefined }),
            getFaceModelHealth(),
            getFaceEnrollmentJobs(),
            getAccessCredentials(),
            getFaceCredentialBindings(),
        ])
        summary.value = { ...summary.value, ...(data.summary || {}) }
        employees.value = data.employees || []
        recentModels.value = data.recentModels || []
        recentVideos.value = data.recentVideos || []
        faceModels.value = modelHealth.data?.models || []
        registryVersion.value = modelHealth.data?.registryVersion || null
        enrollmentJobs.value = jobs.data || []
        accessCredentials.value = credentials.data || []
        faceCredentialBindings.value = bindings.data || []
    } catch (error) {
        console.error('Biometric overview error:', error)
        employees.value = []
        recentModels.value = []
        recentVideos.value = []
        faceModels.value = []
        registryVersion.value = null
        accessCredentials.value = []
        faceCredentialBindings.value = []
    } finally {
        isLoading.value = false
    }
}

const loadEmployeeVideos = async () => {
    selectedVideoId.value = ''
    employeeVideos.value = []
    selectedEmployeeBinding.value = null
    selectedEmployeeCandidates.value = []
    if (!selectedEmployeeId.value) return
    const [videos, binding, candidates] = await Promise.allSettled([
        getEmployeeVideos(selectedEmployeeId.value),
        getEmployeeFaceCredentialBinding(selectedEmployeeId.value),
        getEmployeeFaceCredentialCandidates(selectedEmployeeId.value),
    ])
    employeeVideos.value = videos.status === 'fulfilled' ? (videos.value.data || []) : []
    selectedEmployeeBinding.value = binding.status === 'fulfilled' ? binding.value.data : null
    selectedEmployeeCandidates.value = candidates.status === 'fulfilled' ? (candidates.value.data || []) : []
}

const bindingReadiness = computed(() => {
    if (selectedEmployeeBinding.value?.bindingStatus === 'Active') return 'Ready'
    if (!selectedEmployeeId.value) return '--'
    if (selectedEmployeeCandidates.value.length === 0) return 'NoFaceCredential'
    const ready = selectedEmployeeCandidates.value.filter(item => item.candidateClassification === 'Ready')
    if (ready.length > 1) return 'MultipleCandidates'
    if (ready.length === 1) return 'BindingMissing'
    const blocking = selectedEmployeeCandidates.value[0]?.blockingReasonCode
    return blocking || 'NoFaceCredential'
})

const createEnrollment = async () => {
    enrollmentBusy.value = true
    try {
        await createFaceEnrollmentJob(Number(selectedEmployeeId.value), Number(selectedVideoId.value))
        await fetchOverview()
    } finally {
        enrollmentBusy.value = false
    }
}

const runJobAction = async (job, action) => {
    const actions = {
        activate: activateFaceEnrollmentJob,
        cancel: cancelFaceEnrollmentJob,
        retry: retryFaceEnrollmentJob,
    }
    await actions[action](job.jobId)
    await fetchOverview()
}

let queryTimer = null
watch(query, () => {
    clearTimeout(queryTimer)
    queryTimer = setTimeout(fetchOverview, 260)
})

let enrollmentTimer = null
onMounted(() => {
    fetchOverview()
    enrollmentTimer = setInterval(() => {
    if (enrollmentJobs.value.some(job => ['Pending', 'Processing', 'Activating'].includes(job.status)))
        fetchOverview()
    }, 5000)
})
onUnmounted(() => clearInterval(enrollmentTimer))
</script>

<style scoped>
.table-main {
    color: var(--text-primary);
    font-weight: 600;
}

.table-sub {
    margin-top: 4px;
    color: var(--text-muted);
    font-size: 0.8rem;
}

.surface-list {
    max-height: 400px;
    overflow-y: auto;
}
.enrollment-controls {
    display: grid;
    grid-template-columns: 1fr 1fr auto;
    gap: 12px;
    margin-bottom: 18px;
}

@media (max-width: 1180px) {
    }
</style>
