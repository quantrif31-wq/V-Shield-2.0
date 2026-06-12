<template>
    <div class="modal-overlay" @click.self="$emit('close')">
        <div class="modal-container import-modal">
            <header class="modal-header">
                <h2>📥 Import {{ entityDisplayName }}</h2>
                <button class="btn-icon" @click="$emit('close')">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </header>

            <div class="modal-body">
                <FileDropZone
                    :ref="dropZoneRef"
                    :supported-formats="supportedFormatsStr"
                    @file-selected="onFileSelected"
                    @file-removed="onFileRemoved"
                />

                <div v-if="selectedFile" class="import-options">
                    <label class="checkbox-label">
                        <input type="checkbox" v-model="skipDuplicates" />
                        <span>Bỏ qua dữ liệu trùng lặp</span>
                    </label>
                    <label class="checkbox-label">
                        <input type="checkbox" v-model="updateExisting" />
                        <span>Cập nhật dữ liệu đã tồn tại</span>
                    </label>
                </div>

                <div v-if="previewResult" class="preview-section">
                    <h3>Kết quả preview</h3>
                    <div class="preview-stats">
                        <div class="stat-badge" :class="previewResult.errorCount > 0 ? 'error' : 'success'">
                            {{ previewResult.totalRows }} dòng · {{ previewResult.errorCount }} lỗi
                        </div>
                    </div>
                    <div v-if="previewResult.errors?.length" class="error-list">
                        <div v-for="(err, i) in previewResult.errors.slice(0, 10)" :key="i" class="error-item">
                            <span class="error-row">Dòng {{ err.row }}:</span>
                            <span>{{ err.message }}</span>
                        </div>
                    </div>
                </div>

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
                        <h4>Chi tiết lỗi</h4>
                        <div class="error-table-wrap">
                            <table class="error-table">
                                <thead>
                                    <tr><th>Dòng</th><th>Cột</th><th>Lỗi</th></tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(err, i) in importResult.errors.slice(0, 50)" :key="i">
                                        <td>{{ err.row }}</td>
                                        <td>{{ err.column || '—' }}</td>
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
                        v-if="selectedFile && !previewResult"
                        class="btn btn-outline"
                        :disabled="previewLoading"
                        @click="doPreview"
                    >
                        {{ previewLoading ? 'Đang xử lý...' : 'Xem trước' }}
                    </button>
                    <button
                        v-if="selectedFile"
                        class="btn btn-primary"
                        :disabled="importLoading"
                        @click="format && downloadTemplateFile(format)"
                    >
                        📄 Tải template
                    </button>
                    <button
                        v-if="selectedFile"
                        class="btn btn-accent"
                        :disabled="importLoading"
                        @click="doImport"
                    >
                        {{ importLoading ? 'Đang Import...' : '📥 Import' }}
                    </button>
                </div>
            </footer>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import FileDropZone from './FileDropZone.vue'
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
    if (importResult.value.errorCount === 0) return 'Import thành công!'
    if (importResult.value.successCount > 0) return 'Import hoàn tất (có lỗi)'
    return 'Import thất bại'
})

function onFileSelected(file) {
    selectedFile.value = file
    previewResult.value = null
    importResult.value = null
}

function onFileRemoved() {
    selectedFile.value = null
    previewResult.value = null
    importResult.value = null
}

async function doPreview() {
    if (!selectedFile.value) return
    previewLoading.value = true
    try {
        const res = await importExportApi.previewImport(props.entityType, selectedFile.value)
        previewResult.value = res.data
    } catch (err) {
        previewResult.value = { totalRows: 0, errorCount: 1, errors: [{ row: 0, message: err.message }] }
    } finally {
        previewLoading.value = false
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
    margin-top: 1rem;
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
