<template>
    <div
        class="file-drop-zone"
        :class="{ 'is-dragover': isDragover, 'has-file': file }"
        @dragover.prevent="isDragover = true"
        @dragleave.prevent="isDragover = false"
        @drop.prevent="onDrop"
        @click="selectFile"
    >
        <input
            ref="fileInput"
            type="file"
            :accept="accept"
            style="display: none"
            @change="onFileChange"
        />
        <div v-if="!file" class="drop-content">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="upload-icon">
                <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4" />
                <polyline points="17 8 12 3 7 8" />
                <line x1="12" y1="3" x2="12" y2="15" />
            </svg>
            <p class="drop-title">Kéo thả file vào đây</p>
            <p class="drop-subtitle">hoặc Click để chọn file</p>
            <p class="drop-hint">Hỗ trợ: {{ supportedFormats }}</p>
        </div>
        <div v-else class="file-selected">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="file-icon">
                <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" />
                <polyline points="14 2 14 8 20 8" />
            </svg>
            <div class="file-info">
                <span class="file-name">{{ file.name }}</span>
                <span class="file-size">{{ formatSize(file.size) }}</span>
            </div>
            <button class="btn-icon remove-btn" @click.stop="removeFile" title="Xoá file">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="18" y1="6" x2="6" y2="18" />
                    <line x1="6" y1="6" x2="18" y2="18" />
                </svg>
            </button>
        </div>
    </div>
</template>

<script setup>
import { ref } from 'vue'

const props = defineProps({
    accept: { type: String, default: '.csv,.xlsx,.xls,.json,.xml' },
    supportedFormats: { type: String, default: 'CSV, Excel, JSON, XML' },
})

const emit = defineEmits(['file-selected', 'file-removed'])

const file = ref(null)
const isDragover = ref(false)
const fileInput = ref(null)

function selectFile() {
    fileInput.value?.click()
}

function onFileChange(e) {
    const f = e.target.files?.[0]
    if (f) setFile(f)
}

function onDrop(e) {
    isDragover.value = false
    const f = e.dataTransfer?.files?.[0]
    if (f) setFile(f)
}

function setFile(f) {
    file.value = f
    emit('file-selected', f)
}

function removeFile() {
    file.value = null
    if (fileInput.value) fileInput.value.value = ''
    emit('file-removed')
}

function formatSize(bytes) {
    if (bytes < 1024) return bytes + ' B'
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}
</script>

<style scoped>
.file-drop-zone {
    border: 2px dashed var(--border-color, #374151);
    border-radius: 12px;
    padding: 2rem;
    text-align: center;
    cursor: pointer;
    transition: all 0.2s;
    background: var(--surface-alt, rgba(255, 255, 255, 0.03));
}
.file-drop-zone:hover,
.file-drop-zone.is-dragover {
    border-color: var(--accent, #3b82f6);
    background: var(--accent-glass, rgba(59, 130, 246, 0.08));
}
.file-drop-zone.has-file {
    border-style: solid;
    border-color: var(--accent, #3b82f6);
}
.upload-icon {
    width: 48px;
    height: 48px;
    margin-bottom: 0.75rem;
    color: var(--text-secondary, #9ca3af);
}
.drop-title {
    font-size: 1rem;
    font-weight: 500;
    color: var(--text-primary, #f3f4f6);
    margin: 0 0 0.25rem;
}
.drop-subtitle {
    font-size: 0.875rem;
    color: var(--text-secondary, #9ca3af);
    margin: 0 0 0.5rem;
}
.drop-hint {
    font-size: 0.75rem;
    color: var(--text-tertiary, #6b7280);
    margin: 0;
}
.file-selected {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}
.file-icon {
    width: 32px;
    height: 32px;
    color: var(--accent, #3b82f6);
    flex-shrink: 0;
}
.file-info {
    flex: 1;
    text-align: left;
    min-width: 0;
}
.file-name {
    display: block;
    font-weight: 500;
    color: var(--text-primary, #f3f4f6);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.file-size {
    font-size: 0.75rem;
    color: var(--text-secondary, #9ca3af);
}
.remove-btn {
    background: none;
    border: none;
    color: var(--text-secondary, #9ca3af);
    cursor: pointer;
    padding: 4px;
    border-radius: 4px;
}
.remove-btn:hover {
    color: #ef4444;
    background: rgba(239, 68, 68, 0.1);
}
.remove-btn svg {
    width: 18px;
    height: 18px;
}
</style>
