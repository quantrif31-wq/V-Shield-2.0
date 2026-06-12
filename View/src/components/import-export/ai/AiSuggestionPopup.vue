<template>
    <div v-if="visible" class="suggestion-popup">
        <div class="popup-header">
            <span>Synonyms & Suggestions</span>
            <button class="btn-icon" @click="$emit('close')">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16">
                    <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
                </svg>
            </button>
        </div>

        <div v-if="issues.length === 0" class="popup-empty">
            Không có vấn đề về synonym nào được phát hiện.
        </div>

        <div v-for="(grp, gi) in groupedIssues" :key="gi" class="issue-group">
            <div class="group-header">
                <span class="group-badge" :class="grp.category">{{ grp.categoryLabel }}</span>
                <span class="group-count">{{ grp.items.length }} issues</span>
            </div>
            <div v-for="(item, ii) in grp.items.slice(0, 20)" :key="ii" class="issue-row">
                <div class="issue-cell">
                    <span class="cell-label">Row</span>
                    <span>{{ item.row || '—' }}</span>
                </div>
                <div class="issue-cell">
                    <span class="cell-label">Column</span>
                    <span>{{ item.column }}</span>
                </div>
                <div class="issue-cell">
                    <span class="cell-label">Original</span>
                    <span class="original">{{ item.originalValue }}</span>
                </div>
                <div class="issue-cell">
                    <span class="cell-label">Suggest</span>
                    <span class="suggested">{{ item.suggestedValue }}</span>
                </div>
                <div class="issue-cell">
                    <span class="cell-label">Conf</span>
                    <span class="confidence" :class="confidenceClass(item.confidence)">
                        {{ (item.confidence * 100).toFixed(0) }}%
                    </span>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
    visible: { type: Boolean, default: false },
    issues: { type: Array, default: () => [] },
})

defineEmits(['close'])

const categoryLabels = {
    column_name: 'Column Mapping',
    synonym: 'Synonym',
    case: 'Case',
    boolean_synonym: 'Boolean',
    boolean_format: 'Boolean Format',
    fk_synonym: 'Foreign Key',
}

const groupedIssues = computed(() => {
    const groups = {}
    for (const item of props.issues) {
        const cat = item.category || 'other'
        if (!groups[cat]) groups[cat] = { category: cat, categoryLabel: categoryLabels[cat] || cat, items: [] }
        groups[cat].items.push(item)
    }
    return Object.values(groups)
})

function confidenceClass(val) {
    if (val >= 0.9) return 'high'
    if (val >= 0.7) return 'medium'
    return 'low'
}
</script>

<style scoped>
.suggestion-popup {
    background: var(--surface, #1f2937);
    border: 1px solid var(--border-color, #374151);
    border-radius: 12px;
    margin-top: 1rem;
    max-height: 400px;
    overflow-y: auto;
}
.popup-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.75rem 1rem;
    border-bottom: 1px solid var(--border-color, #374151);
    font-size: 0.8rem;
    font-weight: 600;
    color: var(--text-primary, #f3f4f6);
    position: sticky;
    top: 0;
    background: var(--surface, #1f2937);
    z-index: 1;
}
.btn-icon {
    background: none;
    border: none;
    color: var(--text-secondary, #9ca3af);
    cursor: pointer;
    padding: 4px;
    border-radius: 4px;
}
.btn-icon:hover { color: var(--text-primary, #f3f4f6); background: rgba(255,255,255,0.05); }
.popup-empty {
    padding: 2rem;
    text-align: center;
    color: var(--text-secondary, #9ca3af);
    font-size: 0.8rem;
}
.issue-group { border-bottom: 1px solid var(--border-color, #374151); }
.issue-group:last-child { border-bottom: none; }
.group-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.5rem 1rem;
    background: var(--surface-alt, rgba(255,255,255,0.03));
}
.group-badge {
    font-size: 0.7rem;
    font-weight: 600;
    padding: 0.15rem 0.5rem;
    border-radius: 999px;
}
.group-badge.column_name { background: rgba(59, 130, 246, 0.15); color: #60a5fa; }
.group-badge.synonym { background: rgba(234, 179, 8, 0.15); color: #eab308; }
.group-badge.case { background: rgba(139, 92, 246, 0.15); color: #a78bfa; }
.group-badge.boolean_synonym, .group-badge.boolean_format { background: rgba(34, 197, 94, 0.15); color: #22c55e; }
.group-badge.fk_synonym { background: rgba(249, 115, 22, 0.15); color: #f97316; }
.group-count { font-size: 0.7rem; color: var(--text-tertiary, #6b7280); }
.issue-row {
    display: flex;
    gap: 0.5rem;
    padding: 0.4rem 1rem;
    font-size: 0.75rem;
    border-top: 1px solid rgba(255,255,255,0.03);
}
.issue-cell {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
    min-width: 0;
}
.issue-cell:nth-child(1) { width: 36px; }
.issue-cell:nth-child(2) { width: 80px; }
.issue-cell:nth-child(3) { flex: 1; }
.issue-cell:nth-child(4) { flex: 1; }
.issue-cell:nth-child(5) { width: 50px; }
.cell-label {
    font-size: 0.6rem;
    color: var(--text-tertiary, #6b7280);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}
.original { color: #fca5a5; word-break: break-all; }
.suggested { color: #22c55e; word-break: break-all; }
.confidence { font-weight: 600; }
.confidence.high { color: #22c55e; }
.confidence.medium { color: #eab308; }
.confidence.low { color: #f97316; }
</style>
