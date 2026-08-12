<template>
    <div class="ai-preview">
        <div v-if="aiResult" class="ai-result-section">
            <div class="preview-header">
                <h3>Kết quả xem trước AI</h3>
                <div class="preview-stats">
                    <span class="stat stat-rows">{{ totalRows }} hàng</span>
                    <span class="stat" :class="changeCount > 0 ? 'stat-changes' : 'stat-ok'">
                        {{ changeCount }} thay đổi
                    </span>
                    <span v-if="validation" class="stat" :class="validation.isValid ? 'stat-ok' : 'stat-errors'">
                        {{ validation.isValid ? 'Hợp lệ' : `${validation.errorCount} lỗi` }}
                    </span>
                </div>
            </div>

            <AiNormalizationReport v-if="changes?.length" :changes="changes" />
            <AiSuggestionPopup
                :visible="synonymIssues.length > 0"
                :issues="synonymIssues"
                @close="synonymIssues = []"
            />
        </div>

        <div class="import-options">
            <label class="checkbox-label">
                <input type="checkbox" v-model="confirmNormalization" />
                <span>Áp dụng chuẩn hóa AI trước khi import</span>
            </label>
            <label class="checkbox-label">
                <input type="checkbox" v-model="overrideConflicts" />
                <span>Ghi đè dữ liệu trùng lặp</span>
            </label>
        </div>
    </div>
</template>

<script setup>
import { ref } from 'vue'
import AiNormalizationReport from './AiNormalizationReport.vue'
import AiSuggestionPopup from './AiSuggestionPopup.vue'

const props = defineProps({
    aiResult: { type: Object, default: null },
    totalRows: { type: Number, default: 0 },
    changeCount: { type: Number, default: 0 },
    changes: { type: Array, default: () => [] },
    validation: { type: Object, default: null },
    synonymIssues: { type: Array, default: () => [] },
})

const confirmNormalization = ref(true)
const overrideConflicts = ref(false)

defineExpose({ confirmNormalization, overrideConflicts })
</script>

<style scoped>
.ai-preview {
    margin-top: 1rem;
}
.ai-result-section {
    margin-bottom: 1rem;
}
.preview-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 0.75rem;
}
.preview-header h3 {
    font-size: 0.875rem;
    font-weight: 600;
    margin: 0;
    color: var(--text-primary, #f3f4f6);
}
.preview-stats {
    display: flex;
    gap: 0.5rem;
}
.stat {
    font-size: 0.7rem;
    padding: 0.2rem 0.5rem;
    border-radius: 999px;
    font-weight: 500;
    background: var(--border-color, #374151);
    color: var(--text-secondary, #9ca3af);
}
.stat-rows { background: rgba(59, 130, 246, 0.15); color: #60a5fa; }
.stat-changes { background: rgba(234, 179, 8, 0.15); color: #eab308; }
.stat-ok { background: rgba(34, 197, 94, 0.15); color: #22c55e; }
.stat-errors { background: rgba(239, 68, 68, 0.15); color: #ef4444; }
.import-options {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding-top: 0.75rem;
    border-top: 1px solid var(--border-color, #374151);
}
.checkbox-label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.8rem;
    color: var(--text-secondary, #d1d5db);
    cursor: pointer;
}
.checkbox-label input { accent-color: var(--accent, #3b82f6); }
</style>
