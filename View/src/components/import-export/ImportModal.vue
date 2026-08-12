<template>
    <div class="modal-overlay" @click.self="$emit('close')">
        <div class="modal-container import-modal">
            <header class="modal-header">
                <h2>📥 Nhập {{ entityDisplayName }}</h2>
                <button class="btn-icon" @click="$emit('close')">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </header>

            <div class="modal-body">
                <FileDropZone
                    :ref="dropZoneRef"
                    :supported-formats="aiStatusAvailable ? `${supportedFormatsStr}, PDF, Hình ảnh` : supportedFormatsStr"
                    @file-selected="onFileSelected"
                    @file-removed="onFileRemoved"
                />

                <div v-if="selectedFile && !importResult" class="import-options">
                    <label class="checkbox-label">
                        <input type="checkbox" v-model="skipDuplicates" />
                        <span>Bỏ qua dữ liệu trùng lặp</span>
                    </label>
                    <label class="checkbox-label">
                        <input type="checkbox" v-model="updateExisting" />
                        <span>Cập nhật dữ liệu đã tồn tại</span>
                    </label>
                </div>

                <!-- AI OCR Progress -->
                <AiOcrProgress
                    v-if="aiOcrVisible"
                    :status="ocrStatus"
                    :steps="ocrSteps"
                    :error="ocrError"
                    :detected-format="detectedFormat"
                    :current-step="ocrCurrentStep"
                />

                <!-- AI Analysis Message -->
                <div v-if="aiMessage" class="ai-message" :class="aiMessageType">
                    {{ aiMessage }}
                </div>

                <!-- AI Preview + Normalization Report -->
                <AiPreviewConfirm
                    v-if="aiPreviewVisible"
                    ref="aiPreviewRef"
                    :ai-result="aiNormalizeResult"
                    :total-rows="aiTotalRows"
                    :change-count="aiChangeCount"
                    :changes="aiChanges"
                    :validation="aiValidation"
                    :synonym-issues="synonymIssues"
                />

                <!-- Legacy Preview (fallback when AI not available) -->
                <div v-if="previewResult && !aiSessionId" class="preview-section">
                    <h3>Kết quả preview</h3>
                    <div class="preview-stats">
                        <div class="stat-badge" :class="previewResult.errorCount > 0 ? 'error' : 'success'">
                            {{ previewResult.totalRows }} dòng · {{ previewResult.errorCount }} lỗi
                        </div>
                    </div>
                    <div v-if="previewResult.errors?.length" class="error-list">
                        <div v-for="(err, i) in previewResult.errors" :key="i" class="error-item">
                            <span class="error-row">Dòng {{ err.row }}:</span>
                            <span>{{ err.message }}</span>
                        </div>
                    </div>
                </div>

                <!-- Import Result -->
                <div v-if="importResult" class="result-section">
                    <div class="result-card" :class="resultClass">
                        <div class="result-icon">{{ resultIcon }}</div>
                        <div class="result-details">
                            <p class="result-title">{{ resultTitle }}</p>
                            <p class="result-stats">
                                ✅ {{ importResult.successCount }} thành công ·
                                ❌ {{ importResult.errorCount }} lỗi ·
                                ⚠️ {{ importResult.warningCount }} cảnh báo
                            </p>
                        </div>
                    </div>
                    <div v-if="importResult.errors?.length" class="error-detail-section">
                        <h4>Chi tiết lỗi theo dòng ({{ importResult.errors.length }})</h4>
                        <div class="error-table-wrap">
                            <table class="error-table">
                                <thead>
                                    <tr><th>Dòng</th><th>Cột</th><th>Giá trị</th><th>Lỗi</th></tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(err, i) in importResult.errors" :key="i">
                                        <td>{{ err.row }}</td>
                                        <td>{{ err.column || '—' }}</td>
                                        <td>{{ err.value || '—' }}</td>
                                        <td>{{ err.message }}</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>

            <footer class="modal-footer">
                <button class="btn btn-secondary" @click="$emit('close')">Đóng</button>
                <div v-if="!importResult" class="footer-right">
                    <button
                        v-if="selectedFile && !aiSessionId && !aiPreviewVisible"
                        class="btn btn-outline"
                        :disabled="loading"
                        @click="startAiFlow"
                    >
                        {{ loading ? 'Đang xử lý...' : '🔍 Xem trước (AI)' }}
                    </button>
                    <button
                        v-if="selectedFile"
                        class="btn btn-primary"
                        :disabled="loading || importLoading"
                        @click="format && downloadTemplateFile(format)"
                    >
                        📄 Tải template
                    </button>
                    <button
                        v-if="selectedFile && !aiSessionId && !aiPreviewVisible"
                        class="btn btn-accent"
                        :disabled="loading || importLoading"
                        @click="doImport"
                    >
                        {{ importLoading ? 'Đang nhập...' : '📥 Nhập' }}
                    </button>
                    <button
                        v-if="aiPreviewVisible"
                        class="btn btn-accent"
                        :disabled="importLoading"
                        @click="doAiImport"
                    >
                        {{ importLoading ? 'Đang nhập...' : '✅ Xác nhận & Nhập' }}
                    </button>
                </div>
            </footer>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, nextTick } from 'vue'
import FileDropZone from './FileDropZone.vue'
import AiOcrProgress from './ai/AiOcrProgress.vue'
import AiPreviewConfirm from './ai/AiPreviewConfirm.vue'
import * as importExportApi from '../../services/importExportApi'

const props = defineProps({
    entityType: { type: String, required: true },
    entityDisplayName: { type: String, default: '' },
    format: { type: String, default: 'csv' },
})

const emit = defineEmits(['close', 'import-complete'])

const selectedFile = ref(null)
const skipDuplicates = ref(true)
const updateExisting = ref(false)
const previewResult = ref(null)
const importResult = ref(null)
const previewLoading = ref(false)
const importLoading = ref(false)

// AI state
const aiStatusAvailable = ref(true)
const aiSessionId = ref(null)
const aiNormalizeResult = ref(null)
const aiChanges = ref([])
const aiChangeCount = ref(0)
const aiTotalRows = ref(0)
const aiValidation = ref(null)
const aiPreviewVisible = ref(false)
const synonymIssues = ref([])
const aiMessage = ref('')
const aiMessageType = ref('info')
const detectedFormat = ref('')
const loading = ref(false)

// OCR state
const aiOcrVisible = ref(false)
const ocrStatus = ref('idle')
const ocrSteps = ref(['Queued', 'Queued', 'Queued', 'Queued'])
const ocrError = ref('')
const ocrCurrentStep = ref(-1)

const supportedFormatsStr = 'CSV, Excel, JSON, XML'

const resultClass = computed(() => {
    if (!importResult.value) return ''
    if (importResult.value.errorCount === 0) return 'success'
    if (importResult.value.successCount > 0) return 'partial'
    return 'failed'
})

const resultIcon = computed(() => {
    if (!importResult.value) return ''
    if (importResult.value.errorCount === 0) return '✅'
    if (importResult.value.successCount > 0) return '⚠️'
    return '❌'
})

const resultTitle = computed(() => {
    if (!importResult.value) return ''
    if (importResult.value.errorCount === 0) return 'Nhập thành công!'
    if (importResult.value.successCount > 0) return 'Nhập hoàn tất (có lỗi)'
    return 'Nhập thất bại'
})

function onFileSelected(file) {
    selectedFile.value = file
    previewResult.value = null
    importResult.value = null
    resetAiState()
}

function onFileRemoved() {
    selectedFile.value = null
    previewResult.value = null
    importResult.value = null
    resetAiState()
}

function resetAiState() {
    aiSessionId.value = null
    aiNormalizeResult.value = null
    aiChanges.value = []
    aiChangeCount.value = 0
    aiTotalRows.value = 0
    aiValidation.value = null
    aiPreviewVisible.value = false
    synonymIssues.value = []
    aiMessage.value = ''
    aiMessageType.value = 'info'
    detectedFormat.value = ''
    aiOcrVisible.value = false
    ocrStatus.value = 'idle'
    ocrSteps.value = ['Queued', 'Queued', 'Queued', 'Queued']
    ocrError.value = ''
    ocrCurrentStep.value = -1
}

async function startAiFlow() {
    if (!selectedFile.value) return
    loading.value = true
    resetAiState()

    try {
        // Step 1: Analyze
        aiMessage.value = 'Đang phân tích file...'
        aiMessageType.value = 'info'
        const analyzeRes = await importExportApi.aiAnalyze(props.entityType, selectedFile.value)
        const analysis = analyzeRes.data
        aiSessionId.value = analysis.sessionId
        detectedFormat.value = analysis.detectedFormat

        if (!analysis.isReadable) {
            // Step 2: OCR
            await runOcrFlow(analysis)
        } else if (analysis.suggestedAction === 'normalize' || analysis.suggestedAction === 'import') {
            aiMessage.value = `File ${analysis.detectedFormat.toUpperCase()} đã được đọc thành công (${analysis.totalRows} dòng)`
            aiMessageType.value = 'success'

            // Step 3: Normalize
            await runNormalizeFlow()
        } else {
            aiPreviewVisible.value = true
            aiNormalizeResult.value = { ready: true }
            aiTotalRows.value = analysis.totalRows || 0
        }
    } catch (err) {
        aiMessage.value = err.response?.data?.message || err.message || 'Lỗi khi phân tích file'
        aiMessageType.value = 'error'
        // Fallback to legacy preview
        try {
            const fallbackRes = await importExportApi.previewImport(props.entityType, selectedFile.value)
            previewResult.value = fallbackRes.data
        } catch (fallbackErr) {
            previewResult.value = { totalRows: 0, errorCount: 1, errors: [{ row: 0, message: fallbackErr.message }] }
        }
    } finally {
        loading.value = false
    }
}

async function runOcrFlow(analysis) {
    aiOcrVisible.value = true
    ocrStatus.value = 'processing'

    // Step animation
    ocrSteps.value = ['✓ Phân tích', 'Đang trích xuất...', 'Đang chờ xử lý', 'Đang chờ xử lý']
    ocrCurrentStep.value = 1
    await sleep(500)

    ocrSteps.value = ['✓ Phân tích', 'Đang trích xuất...', 'Đang phân tích ngữ nghĩa...', 'Đang chờ xử lý']
    ocrCurrentStep.value = 2
    await sleep(300)

    try {
        const ocrRes = await importExportApi.aiOcr(props.entityType, selectedFile.value)
        const ocrData = ocrRes.data

        ocrSteps.value = ['✓ Phân tích', '✓ Trích xuất', '✓ Phân tích ngữ nghĩa', 'Đang chuẩn hóa...']
        ocrCurrentStep.value = 3
        await sleep(200)

        if (ocrData.status === 'failed') {
            ocrStatus.value = 'error'
            ocrError.value = ocrData.errorMessage || 'Xử lý OCR thất bại'
            return
        }

        aiChanges.value = ocrData.changes || []
        aiChangeCount.value = ocrData.changeCount || 0
        aiTotalRows.value = ocrData.totalRows || 0

        ocrSteps.value = [
            `✓ File ${analysis.detectedFormat?.toUpperCase() || 'không xác định'}`,
            `✓ Đã trích xuất ${ocrData.totalRows || 0} dòng`,
            `✓ Phát hiện ${ocrData.changes?.length || 0} từ đồng nghĩa`,
            'Đang gửi normalization...',
        ]
        ocrCurrentStep.value = 3
        ocrStatus.value = 'done'
        await sleep(300)

        // Step 3: Normalize after OCR
        await runNormalizeFlow()
    } catch (err) {
        ocrStatus.value = 'error'
        ocrError.value = err.response?.data?.message || err.message || 'Xử lý OCR thất bại'
    }
}

async function runNormalizeFlow() {
    if (!aiSessionId.value) return

    aiMessage.value = 'Đang chuẩn hóa dữ liệu với AI...'
    aiMessageType.value = 'info'

    try {
        const normRes = await importExportApi.aiNormalize(props.entityType, aiSessionId.value)
        const normData = normRes.data

        aiChanges.value = normData.changes || []
        aiChangeCount.value = normData.changeCount || 0
        aiTotalRows.value = normData.totalRows || 0
        aiValidation.value = normData.validation || null
        aiPreviewVisible.value = true

        const errorCount = normData.validation?.errorCount || 0
        if (normData.readyForImport) {
            aiMessage.value = `Dữ liệu đã sẵn sàng để nhập (${aiTotalRows.value} dòng, ${aiChangeCount.value} thay đổi)`
            aiMessageType.value = 'success'
        } else {
            aiMessage.value = `Dữ liệu có ${errorCount} lỗi cần xem xét trước khi nhập`
            aiMessageType.value = 'warning'
        }
    } catch (err) {
        aiMessage.value = err.response?.data?.message || err.message || 'Lỗi khi chuẩn hóa dữ liệu'
        aiMessageType.value = 'error'
    }
}

async function doAiImport() {
    if (!aiSessionId.value) return
    importLoading.value = true

    try {
        const previewRef = await nextTick()
        const opts = {
            confirmNormalization: true,
            overrideConflicts: false,
        }

        const res = await importExportApi.aiConfirm(props.entityType, aiSessionId.value, opts)
        importResult.value = res.data
        if (res.data.status === 'Completed' || res.data.status === 'PartialSuccess') {
            emit('import-complete', res.data)
        }
    } catch (err) {
        importResult.value = {
            successCount: 0,
            errorCount: 1,
            warningCount: 0,
            status: 'Failed',
            errors: [{ row: 0, message: err.response?.data?.message || err.message }],
        }
    } finally {
        importLoading.value = false
    }
}

async function doImport() {
    if (!selectedFile.value) return
    importLoading.value = true
    try {
        const res = await importExportApi.importFile(props.entityType, selectedFile.value, {
            skipDuplicates: skipDuplicates.value,
            updateExisting: updateExisting.value,
        })
        importResult.value = res.data
        if (res.data.status === 'Completed' || res.data.status === 'PartialSuccess') {
            emit('import-complete', res.data)
        }
    } catch (err) {
        importResult.value = {
            successCount: 0,
            errorCount: 1,
            warningCount: 0,
            status: 'Failed',
            errors: [{ row: 0, message: err.response?.data?.message || err.message }],
        }
    } finally {
        importLoading.value = false
    }
}

async function downloadTemplateFile(format) {
    try {
        const res = await importExportApi.downloadTemplate(props.entityType, format)
        const url = window.URL.createObjectURL(new Blob([res.data]))
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', `template_${props.entityType}.${format}`)
        document.body.appendChild(link)
        link.click()
        document.body.removeChild(link)
        window.URL.revokeObjectURL(url)
    } catch (err) {
        console.error('Template download failed', err)
    }
}

function sleep(ms) {
    return new Promise(r => setTimeout(r, ms))
}
</script>

<style scoped>
.modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.6);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    backdrop-filter: blur(4px);
}
.modal-container {
    background: var(--surface, #1f2937);
    border-radius: 16px;
    width: 90%;
    max-width: 640px;
    max-height: 90vh;
    display: flex;
    flex-direction: column;
    box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
}
.modal-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1.25rem 1.5rem;
    border-bottom: 1px solid var(--border-color, #374151);
}
.modal-header h2 {
    margin: 0;
    font-size: 1.125rem;
    font-weight: 600;
}
.btn-icon {
    background: none;
    border: none;
    color: var(--text-secondary, #9ca3af);
    cursor: pointer;
    padding: 4px;
    border-radius: 6px;
}
.btn-icon:hover { color: var(--text-primary, #f3f4f6); background: rgba(255,255,255,0.05); }
.btn-icon svg { width: 20px; height: 20px; }
.modal-body {
    padding: 1.5rem;
    overflow-y: auto;
    flex: 1;
}
.modal-footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1rem 1.5rem;
    border-top: 1px solid var(--border-color, #374151);
}
.footer-right { display: flex; gap: 0.5rem; }

.import-options {
    margin-top: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}
.checkbox-label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.875rem;
    color: var(--text-secondary, #d1d5db);
    cursor: pointer;
}
.checkbox-label input { accent-color: var(--accent, #3b82f6); }

.ai-message {
    margin-top: 0.75rem;
    padding: 0.5rem 0.75rem;
    border-radius: 8px;
    font-size: 0.8rem;
    display: flex;
    align-items: center;
    gap: 0.5rem;
}
.ai-message.info { background: rgba(59, 130, 246, 0.1); color: #60a5fa; border: 1px solid rgba(59, 130, 246, 0.2); }
.ai-message.success { background: rgba(34, 197, 94, 0.1); color: #22c55e; border: 1px solid rgba(34, 197, 94, 0.2); }
.ai-message.warning { background: rgba(234, 179, 8, 0.1); color: #eab308; border: 1px solid rgba(234, 179, 8, 0.2); }
.ai-message.error { background: rgba(239, 68, 68, 0.1); color: #fca5a5; border: 1px solid rgba(239, 68, 68, 0.2); }

.preview-section, .result-section { margin-top: 1rem; }
.preview-section h3, .error-detail-section h4 {
    font-size: 0.875rem;
    font-weight: 600;
    margin: 0 0 0.5rem;
    color: var(--text-primary, #f3f4f6);
}
.preview-stats { margin-bottom: 0.75rem; }
.stat-badge {
    display: inline-block;
    padding: 0.25rem 0.75rem;
    border-radius: 999px;
    font-size: 0.8rem;
    font-weight: 500;
}
.stat-badge.success { background: rgba(34, 197, 94, 0.15); color: #22c55e; }
.stat-badge.error { background: rgba(239, 68, 68, 0.15); color: #ef4444; }

.error-list { display: flex; flex-direction: column; gap: 0.25rem; }
.error-item {
    font-size: 0.8rem;
    color: #fca5a5;
    padding: 0.25rem 0.5rem;
    background: rgba(239, 68, 68, 0.08);
    border-radius: 4px;
}
.error-row { font-weight: 600; margin-right: 0.25rem; }

.result-card {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 1rem;
    border-radius: 12px;
    margin-bottom: 0.75rem;
}
.result-card.success { background: rgba(34, 197, 94, 0.1); border: 1px solid rgba(34, 197, 94, 0.3); }
.result-card.partial { background: rgba(234, 179, 8, 0.1); border: 1px solid rgba(234, 179, 8, 0.3); }
.result-card.failed { background: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.3); }
.result-icon { font-size: 2rem; }
.result-title { font-weight: 600; margin: 0; color: var(--text-primary, #f3f4f6); }
.result-stats { font-size: 0.8rem; margin: 0.25rem 0 0; color: var(--text-secondary, #9ca3af); }
.error-table-wrap { max-height: 200px; overflow-y: auto; border-radius: 8px; border: 1px solid var(--border-color, #374151); }
.error-table { width: 100%; border-collapse: collapse; font-size: 0.8rem; }
.error-table th {
    background: var(--surface-alt, rgba(255,255,255,0.05));
    padding: 0.5rem;
    text-align: left;
    color: var(--text-secondary, #9ca3af);
    font-weight: 500;
    position: sticky;
    top: 0;
}
.error-table td { padding: 0.4rem 0.5rem; border-top: 1px solid var(--border-color, #374151); color: #fca5a5; }
</style>
