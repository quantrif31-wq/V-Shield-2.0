<template>
    <div class="ocr-progress">
        <div class="ocr-header">
            <div class="ocr-title-row">
                <span class="ocr-icon">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="20" height="20">
                        <circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/>
                    </svg>
                </span>
                <span>Xử lý OCR bằng AI</span>
                <span v-if="status === 'processing'" class="spinner"></span>
                <span v-else-if="status === 'done'" class="badge badge-success">Hoàn tất</span>
                <span v-else-if="status === 'error'" class="badge badge-error">Thất bại</span>
            </div>
        </div>

        <div class="steps">
            <div class="step" :class="stepClass(0)">
                <div class="step-indicator">{{ stepIcon(0) }}</div>
                <div class="step-info">
                    <span class="step-label">Phân tích file</span>
                    <span class="step-desc">{{ steps[0] }}</span>
                </div>
            </div>
            <div class="step" :class="stepClass(1)">
                <div class="step-indicator">{{ stepIcon(1) }}</div>
                <div class="step-info">
                    <span class="step-label">Trích xuất văn bản</span>
                    <span class="step-desc">{{ steps[1] }}</span>
                </div>
            </div>
            <div class="step" :class="stepClass(2)">
                <div class="step-indicator">{{ stepIcon(2) }}</div>
                <div class="step-info">
                    <span class="step-label">Phân tích ngữ nghĩa</span>
                    <span class="step-desc">{{ steps[2] }}</span>
                </div>
            </div>
            <div class="step" :class="stepClass(3)">
                <div class="step-indicator">{{ stepIcon(3) }}</div>
                <div class="step-info">
                    <span class="step-label">Đề xuất chuẩn hóa</span>
                    <span class="step-desc">{{ steps[3] }}</span>
                </div>
            </div>
        </div>

        <div v-if="error" class="ocr-error">{{ error }}</div>

        <div v-if="detectedFormat" class="ocr-meta">
            <span class="meta-label">Đã nhận dạng:</span>
            <span class="meta-value">{{ detectedFormat.toUpperCase() }}</span>
        </div>
    </div>
</template>

<script setup>
const props = defineProps({
    status: { type: String, default: 'idle' },
    steps: { type: Array, default: () => ['Queue', 'Queue', 'Queue', 'Queue'] },
    error: { type: String, default: '' },
    detectedFormat: { type: String, default: '' },
    currentStep: { type: Number, default: -1 },
})

function stepClass(idx) {
    if (props.currentStep > idx) return 'done'
    if (props.currentStep === idx) return 'active'
    if (props.status === 'error' && props.currentStep < idx) return ''
    return 'pending'
}

function stepIcon(idx) {
    if (props.currentStep > idx) return '✓'
    if (props.currentStep === idx) return '○'
    return '○'
}
</script>

<style scoped>
.ocr-progress {
    background: var(--surface-alt, rgba(255,255,255,0.03));
    border-radius: 12px;
    padding: 1rem;
    margin-top: 1rem;
    border: 1px solid var(--border-color, #374151);
}
.ocr-header {
    margin-bottom: 0.75rem;
}
.ocr-title-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--text-primary, #f3f4f6);
}
.ocr-icon {
    display: flex;
    align-items: center;
    color: var(--accent, #3b82f6);
}
.spinner {
    width: 16px; height: 16px;
    border: 2px solid var(--border-color, #374151);
    border-top-color: var(--accent, #3b82f6);
    border-radius: 50%;
    animation: spin 0.6s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
.badge {
    font-size: 0.7rem;
    padding: 0.15rem 0.5rem;
    border-radius: 999px;
    font-weight: 500;
}
.badge-success { background: rgba(34, 197, 94, 0.15); color: #22c55e; }
.badge-error { background: rgba(239, 68, 68, 0.15); color: #ef4444; }

.steps { display: flex; flex-direction: column; gap: 0.5rem; }
.step {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.4rem 0.5rem;
    border-radius: 8px;
    transition: all 0.2s;
}
.step.active { background: rgba(59, 130, 246, 0.08); }
.step.done { opacity: 0.8; }
.step-indicator {
    width: 24px; height: 24px;
    display: flex; align-items: center; justify-content: center;
    border-radius: 50%;
    font-size: 0.7rem;
    font-weight: 700;
    flex-shrink: 0;
}
.step.pending .step-indicator {
    background: var(--border-color, #374151);
    color: var(--text-tertiary, #6b7280);
}
.step.active .step-indicator {
    background: var(--accent, #3b82f6);
    color: #fff;
}
.step.done .step-indicator {
    background: rgba(34, 197, 94, 0.15);
    color: #22c55e;
}
.step-info {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
}
.step-label {
    font-size: 0.8rem;
    font-weight: 500;
    color: var(--text-primary, #f3f4f6);
}
.step-desc {
    font-size: 0.7rem;
    color: var(--text-secondary, #9ca3af);
}
.ocr-error {
    margin-top: 0.5rem;
    padding: 0.5rem;
    background: rgba(239, 68, 68, 0.08);
    border-radius: 6px;
    color: #fca5a5;
    font-size: 0.8rem;
}
.ocr-meta {
    margin-top: 0.5rem;
    display: flex;
    gap: 0.5rem;
    font-size: 0.75rem;
}
.meta-label { color: var(--text-secondary, #9ca3af); }
.meta-value { color: var(--accent, #3b82f6); font-weight: 500; }
</style>
