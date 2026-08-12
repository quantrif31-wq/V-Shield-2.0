<template>
    <div v-if="changes.length > 0" class="norm-report">
        <div class="report-header">
            <div class="report-title-row">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="18" height="18" class="report-icon">
                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
                </svg>
                <span class="report-title">Báo cáo chuẩn hóa AI</span>
                <span class="report-badge">{{ changes.length }} thay đổi</span>
            </div>
            <p class="report-desc">
                AI đã tự động chuẩn hóa {{ changes.length }} giá trị để đảm bảo dữ liệu nhất quán.
            </p>
        </div>

        <div class="change-list">
            <div v-for="(ch, i) in previewChanges" :key="i" class="change-row">
                <div class="change-num">{{ i + 1 }}</div>
                <div class="change-details">
                    <div class="change-meta">
                        <span class="meta-badge" :class="ch.reason">{{ reasonLabel(ch.reason) }}</span>
                        <span class="meta-position">Hàng {{ ch.row }} · {{ ch.column }}</span>
                    </div>
                    <div class="change-values">
                        <span class="old-value">{{ ch.originalValue }}</span>
                        <span class="arrow">→</span>
                        <span class="new-value">{{ ch.normalizedValue }}</span>
                    </div>
                </div>
            </div>
        </div>

        <div v-if="changes.length > 10" class="show-more">
            <button class="btn btn-link" @click="showAll = !showAll">
                {{ showAll ? 'Thu gọn' : `Xem thêm ${changes.length - 10} thay đổi` }}
            </button>
        </div>
    </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
    changes: { type: Array, default: () => [] },
})

const showAll = ref(false)

const previewChanges = computed(() =>
    showAll.value ? props.changes : props.changes.slice(0, 10)
)

const reasonLabels = {
    column_name: 'Column Map',
    synonym: 'Synonym',
    case: 'Case',
    boolean_synonym: 'Boolean',
    boolean_format: 'Bool Format',
    fk_synonym: 'FK Match',
}

function reasonLabel(cat) {
    return reasonLabels[cat] || cat || 'Normalized'
}
</script>

<style scoped>
.norm-report {
    margin-top: 1rem;
    border: 1px solid var(--border-color, #374151);
    border-radius: 12px;
    background: var(--surface-alt, rgba(255,255,255,0.03));
    overflow: hidden;
}
.report-header {
    padding: 0.75rem 1rem;
    border-bottom: 1px solid var(--border-color, #374151);
}
.report-title-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--text-primary, #f3f4f6);
}
.report-icon { color: var(--accent, #3b82f6); }
.report-badge {
    font-size: 0.65rem;
    font-weight: 600;
    padding: 0.15rem 0.5rem;
    border-radius: 999px;
    background: rgba(234, 179, 8, 0.15);
    color: #eab308;
}
.report-desc {
    margin: 0.25rem 0 0;
    font-size: 0.75rem;
    color: var(--text-secondary, #9ca3af);
}
.change-list { display: flex; flex-direction: column; }
.change-row {
    display: flex;
    align-items: flex-start;
    gap: 0.75rem;
    padding: 0.5rem 1rem;
    border-bottom: 1px solid rgba(255,255,255,0.03);
    font-size: 0.8rem;
}
.change-row:last-child { border-bottom: none; }
.change-num {
    width: 20px;
    height: 20px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 4px;
    background: var(--border-color, #374151);
    color: var(--text-secondary, #9ca3af);
    font-size: 0.65rem;
    font-weight: 700;
    flex-shrink: 0;
    margin-top: 0.15rem;
}
.change-details {
    flex: 1;
    min-width: 0;
}
.change-meta {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.25rem;
}
.meta-badge {
    font-size: 0.6rem;
    font-weight: 600;
    padding: 0.1rem 0.4rem;
    border-radius: 4px;
    text-transform: uppercase;
    letter-spacing: 0.03em;
}
.meta-badge.column_name { background: rgba(59, 130, 246, 0.15); color: #60a5fa; }
.meta-badge.synonym { background: rgba(234, 179, 8, 0.15); color: #eab308; }
.meta-badge.case { background: rgba(139, 92, 246, 0.15); color: #a78bfa; }
.meta-badge.boolean_synonym, .meta-badge.boolean_format { background: rgba(34, 197, 94, 0.15); color: #22c55e; }
.meta-badge.fk_synonym { background: rgba(249, 115, 22, 0.15); color: #f97316; }
.meta-position { font-size: 0.7rem; color: var(--text-tertiary, #6b7280); }
.change-values {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.8rem;
}
.old-value {
    color: #fca5a5;
    text-decoration: line-through;
    word-break: break-all;
}
.arrow { color: var(--text-tertiary, #6b7280); font-size: 0.75rem; }
.new-value {
    color: #22c55e;
    font-weight: 500;
    word-break: break-all;
}
.show-more {
    padding: 0.5rem;
    text-align: center;
    border-top: 1px solid var(--border-color, #374151);
}
.btn-link {
    background: none;
    border: none;
    color: var(--accent, #3b82f6);
    cursor: pointer;
    font-size: 0.8rem;
}
.btn-link:hover { text-decoration: underline; }
</style>
