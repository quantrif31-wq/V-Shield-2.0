<template>
    <div class="modal-overlay" @click.self="$emit('close')">
        <div class="modal-container export-modal">
            <header class="modal-header">
                <h2>📤 Xuất {{ entityDisplayName }}</h2>
                <button class="btn-icon" @click="$emit('close')">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </header>

            <div class="modal-body">
                <div class="form-group">
                    <label class="form-label">Định dạng xuất</label>
                    <div class="format-grid">
                        <button
                            v-for="fmt in formats"
                            :key="fmt.format"
                            class="format-btn"
                            :class="{ active: selectedFormat === fmt.format }"
                            @click="selectedFormat = fmt.format"
                        >
                            <span class="format-ext">.{{ fmt.format }}</span>
                            <span class="format-name">{{ fmt.displayName }}</span>
                        </button>
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label">Chọn cột xuất</label>
                    <div class="column-grid">
                        <label v-for="col in availableColumns" :key="col" class="column-checkbox">
                            <input type="checkbox" :value="col" v-model="selectedColumns" />
                            <span>{{ columnDisplayName(col) }}</span>
                        </label>
                    </div>
                    <div class="column-actions">
                        <button class="btn-link" @click="selectAllColumns">Chọn tất cả</button>
                        <button class="btn-link" @click="selectedColumns = []">Bỏ chọn</button>
                    </div>
                </div>

                <div v-if="exportResult" class="result-section">
                    <div class="result-card success">
                        <div class="result-icon">✅</div>
                        <div class="result-details">
                            <p class="result-title">Xuất thành công!</p>
                            <p class="result-stats">{{ exportResult.totalRows }} dòng · {{ exportResult.fileFormat.toUpperCase() }} · {{ formatSize(exportResult.fileSize) }}</p>
                        </div>
                    </div>
                </div>
            </div>

            <footer class="modal-footer">
                <button class="btn btn-secondary" @click="$emit('close')">Đóng</button>
                <div class="footer-right">
                    <button class="btn btn-outline" @click="downloadTemplateFile" :disabled="exportLoading">
                        📄 Tải template
                    </button>
                    <button class="btn btn-accent" :disabled="exportLoading" @click="doExport">
                        {{ exportLoading ? 'Đang xuất...' : '📤 Xuất' }}
                    </button>
                </div>
            </footer>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import * as importExportApi from '../../services/importExportApi'

const props = defineProps({
    entityType: { type: String, required: true },
    entityDisplayName: { type: String, default: '' },
    availableColumns: { type: Array, default: () => [] },
})

const emit = defineEmits(['close'])

const formats = ref([])
const selectedFormat = ref('csv')
const selectedColumns = ref([])
const exportResult = ref(null)
const exportLoading = ref(false)

onMounted(async () => {
    try {
        const res = await importExportApi.getFormats()
        formats.value = res.data.filter(f => f.supportsExport)
    } catch { }
    if (props.availableColumns.length) {
        selectedColumns.value = [...props.availableColumns]
    }
})

function columnDisplayName(col) {
    return col.replace(/([A-Z])/g, ' $1').replace(/^./, s => s.toUpperCase()).trim()
}

function selectAllColumns() {
    selectedColumns.value = [...props.availableColumns]
}

function formatSize(bytes) {
    if (!bytes) return '0 B'
    if (bytes < 1024) return bytes + ' B'
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

async function doExport() {
    exportLoading.value = true
    try {
        const params = {
            format: selectedFormat.value,
            includeHeaders: true,
            columns: selectedColumns.value,
        }
        const res = await importExportApi.exportData(props.entityType, params)
        exportResult.value = res.data

        if (res.data.downloadUrl) {
            const dlRes = await importExportApi.downloadResult(res.data.historyId)
            const url = window.URL.createObjectURL(new Blob([dlRes.data]))
            const link = document.createElement('a')
            link.href = url
            link.setAttribute('download', res.data.fileName)
            document.body.appendChild(link)
            link.click()
            document.body.removeChild(link)
            window.URL.revokeObjectURL(url)
        }
    } catch (err) {
        console.error('Export failed', err)
    } finally {
        exportLoading.value = false
    }
}

async function downloadTemplateFile() {
    try {
        const res = await importExportApi.downloadTemplate(props.entityType, selectedFormat.value)
        const url = window.URL.createObjectURL(new Blob([res.data]))
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', `template_${props.entityType}.${selectedFormat.value}`)
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
.modal-header h2 { margin: 0; font-size: 1.125rem; font-weight: 600; }
.btn-icon {
    background: none; border: none; color: var(--text-secondary, #9ca3af);
    cursor: pointer; padding: 4px; border-radius: 6px;
}
.btn-icon:hover { color: var(--text-primary, #f3f4f6); background: rgba(255,255,255,0.05); }
.btn-icon svg { width: 20px; height: 20px; }
.modal-body { padding: 1.5rem; overflow-y: auto; flex: 1; }
.modal-footer {
    display: flex; align-items: center; justify-content: space-between;
    padding: 1rem 1.5rem; border-top: 1px solid var(--border-color, #374151);
}
.footer-right { display: flex; gap: 0.5rem; }

.form-group { margin-bottom: 1.25rem; }
.form-label {
    display: block; font-size: 0.875rem; font-weight: 500;
    color: var(--text-secondary, #d1d5db); margin-bottom: 0.5rem;
}

.format-grid { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.format-btn {
    display: flex; flex-direction: column; align-items: center;
    padding: 0.75rem 1rem; border: 1px solid var(--border-color, #374151);
    border-radius: 10px; background: var(--surface-alt, rgba(255,255,255,0.03));
    cursor: pointer; transition: all 0.15s; min-width: 80px;
}
.format-btn:hover { border-color: var(--accent, #3b82f6); }
.format-btn.active {
    border-color: var(--accent, #3b82f6);
    background: var(--accent-glass, rgba(59, 130, 246, 0.12));
}
.format-ext { font-size: 0.9rem; font-weight: 600; color: var(--text-primary, #f3f4f6); }
.format-name { font-size: 0.7rem; color: var(--text-secondary, #9ca3af); }

.column-grid {
    display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: 0.35rem; max-height: 200px; overflow-y: auto;
    padding: 0.5rem; border: 1px solid var(--border-color, #374151);
    border-radius: 8px; background: var(--surface-alt, rgba(255,255,255,0.02));
}
.column-checkbox {
    display: flex; align-items: center; gap: 0.4rem;
    font-size: 0.8rem; color: var(--text-primary, #f3f4f6); cursor: pointer;
}
.column-checkbox input { accent-color: var(--accent, #3b82f6); }
.column-actions { display: flex; gap: 0.75rem; margin-top: 0.35rem; }
.btn-link {
    background: none; border: none; color: var(--accent, #3b82f6);
    font-size: 0.8rem; cursor: pointer; padding: 0;
}
.btn-link:hover { text-decoration: underline; }

.result-section { margin-top: 0.5rem; }
.result-card {
    display: flex; align-items: center; gap: 1rem;
    padding: 1rem; border-radius: 12px;
    background: rgba(34, 197, 94, 0.1);
    border: 1px solid rgba(34, 197, 94, 0.3);
}
.result-icon { font-size: 2rem; }
.result-title { font-weight: 600; margin: 0; color: var(--text-primary, #f3f4f6); }
.result-stats { font-size: 0.8rem; margin: 0.25rem 0 0; color: var(--text-secondary, #9ca3af); }
</style>
