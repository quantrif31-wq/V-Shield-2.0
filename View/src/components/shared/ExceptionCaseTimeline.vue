<template>
  <div class="ect-root">
    <div v-if="!items || items.length === 0" class="ect-empty">
      <p>Chưa có sự kiện nào cho case này.</p>
    </div>

    <div v-else class="ect-timeline">
      <div
        v-for="(item, index) in sortedItems"
        :key="item.id || index"
        class="ect-entry"
        :class="`ect-entry--${item.type}`"
      >
        <div class="ect-dot" :class="`ect-dot--${item.type}`"></div>
        <div v-if="index < sortedItems.length - 1" class="ect-line"></div>

        <div class="ect-card" :class="`ect-card--${item.type}`">
          <div class="ect-card-head">
            <span class="ect-badge" :class="`ect-badge--${item.type}`">{{ badgeText(item.type) }}</span>
            <span class="ect-time">{{ formatTime(item.timestamp) }}</span>
          </div>

          <div class="ect-card-body">
            <p class="ect-title">{{ item.title }}</p>
            <p v-if="item.description" class="ect-desc">{{ item.description }}</p>

            <!-- Actor info -->
            <div v-if="item.actor" class="ect-meta">
              <span class="ect-meta-label">Người thực hiện:</span>
              <span class="ect-meta-value">{{ item.actor }}</span>
            </div>

            <!-- Reason -->
            <div v-if="item.reason" class="ect-meta">
              <span class="ect-meta-label">Lý do:</span>
              <span class="ect-meta-value">{{ item.reason }}</span>
            </div>

            <!-- Receipt -->
            <div v-if="item.receiptId" class="ect-receipt">
              <code>{{ item.receiptId }}</code>
            </div>

            <!-- Extra data -->
            <div v-if="item.details && item.details.length > 0" class="ect-details">
              <div v-for="(d, i) in item.details" :key="i" class="ect-detail-row">
                <span class="ect-detail-label">{{ d.label }}:</span>
                <span class="ect-detail-value">{{ d.value }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'ExceptionCaseTimeline',
  props: {
    items: {
      type: Array,
      default: () => [],
      validator: (items) =>
        items.every(
          (item) =>
            item &&
            item.type &&
            item.title &&
            ['scan', 'allow', 'deny', 'manual', 'override', 'duress', 'escalate', 'approve', 'reject', 'close', 'review', 'system'].includes(item.type)
        ),
    },
  },
  computed: {
    sortedItems() {
      if (!this.items) return []
      return [...this.items].sort((a, b) => {
        const tA = new Date(a.timestamp || 0).getTime()
        const tB = new Date(b.timestamp || 0).getTime()
        return tA - tB
      })
    },
  },
  methods: {
    badgeText(type) {
      const map = {
        scan: 'Quét',
        allow: 'Cho qua',
        deny: 'Từ chối',
        manual: 'Thủ công',
        override: 'Override',
        duress: 'Duress',
        escalate: 'Yêu cầu',
        approve: 'Phê duyệt',
        reject: 'Từ chối duyệt',
        close: 'Đóng case',
        review: 'Hậu kiểm',
        system: 'Hệ thống',
      }
      return map[type] || type
    },
    formatTime(ts) {
      if (!ts) return ''
      try {
        return new Date(ts).toLocaleString('vi-VN', {
          year: 'numeric',
          month: '2-digit',
          day: '2-digit',
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
        })
      } catch {
        return String(ts)
      }
    },
  },
}
</script>

<style scoped>
.ect-root {
  width: 100%;
}
.ect-empty {
  padding: 24px;
  text-align: center;
  color: #64748b;
  font-size: 14px;
  background: #f8fafc;
  border-radius: 10px;
  border: 1px dashed #cbd5e1;
}
.ect-timeline {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 8px 0;
}
.ect-entry {
  position: relative;
  display: flex;
  align-items: flex-start;
  gap: 14px;
  padding: 6px 0 6px 20px;
}
.ect-dot {
  position: absolute;
  left: 4px;
  top: 16px;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  z-index: 2;
  flex-shrink: 0;
  border: 2px solid #fff;
}
.ect-dot--scan { background: #64748b; }
.ect-dot--allow { background: #22c55e; }
.ect-dot--deny { background: #ef4444; }
.ect-dot--manual { background: #f59e0b; }
.ect-dot--override { background: #f97316; }
.ect-dot--duress { background: #ec4899; }
.ect-dot--escalate { background: #3b82f6; }
.ect-dot--approve { background: #10b981; }
.ect-dot--reject { background: #ef4444; }
.ect-dot--close { background: #6b7280; }
.ect-dot--review { background: #8b5cf6; }
.ect-dot--system { background: #94a3b8; }
.ect-line {
  position: absolute;
  left: 9px;
  top: 28px;
  bottom: -6px;
  width: 2px;
  background: #e2e8f0;
  z-index: 1;
}
.ect-card {
  flex: 1;
  min-width: 0;
  background: #ffffff;
  border: 1px solid #e9eef5;
  border-radius: 10px;
  overflow: hidden;
  transition: box-shadow 0.15s ease;
}
.ect-card:hover {
  box-shadow: 0 2px 12px rgba(15, 23, 42, 0.06);
}
.ect-card--allow { border-left: 3px solid #22c55e; }
.ect-card--deny { border-left: 3px solid #ef4444; }
.ect-card--manual { border-left: 3px solid #f59e0b; }
.ect-card--override { border-left: 3px solid #f97316; }
.ect-card--duress { border-left: 3px solid #ec4899; }
.ect-card--escalate { border-left: 3px solid #3b82f6; }
.ect-card--approve { border-left: 3px solid #10b981; }
.ect-card--reject { border-left: 3px solid #ef4444; }
.ect-card--close { border-left: 3px solid #6b7280; }
.ect-card--review { border-left: 3px solid #8b5cf6; }
.ect-card--scan { border-left: 3px solid #64748b; }
.ect-card--system { border-left: 3px solid #94a3b8; }
.ect-card-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  background: #f8fafc;
  border-bottom: 1px solid #f1f5f9;
}
.ect-badge {
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 700;
}
.ect-badge--scan { background: #f1f5f9; color: #475569; }
.ect-badge--allow { background: #dcfce7; color: #166534; }
.ect-badge--deny { background: #fee2e2; color: #991b1b; }
.ect-badge--manual { background: #fef3c7; color: #92400e; }
.ect-badge--override { background: #fff7ed; color: #c2410c; }
.ect-badge--duress { background: #fce7f3; color: #9d174d; }
.ect-badge--escalate { background: #dbeafe; color: #1e40af; }
.ect-badge--approve { background: #d1fae5; color: #065f46; }
.ect-badge--reject { background: #fee2e2; color: #991b1b; }
.ect-badge--close { background: #f3f4f6; color: #374151; }
.ect-badge--review { background: #ede9fe; color: #5b21b6; }
.ect-badge--system { background: #f8fafc; color: #64748b; }
.ect-time {
  font-size: 11px;
  color: #94a3b8;
  font-weight: 600;
}
.ect-card-body {
  padding: 10px 12px;
}
.ect-title {
  margin: 0 0 4px;
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
}
.ect-desc {
  margin: 0 0 8px;
  font-size: 13px;
  color: #475569;
  line-height: 1.4;
}
.ect-meta {
  display: flex;
  gap: 6px;
  font-size: 12px;
  margin-top: 4px;
}
.ect-meta-label {
  color: #64748b;
  font-weight: 600;
  flex-shrink: 0;
}
.ect-meta-value {
  color: #334155;
  font-weight: 600;
}
.ect-receipt {
  margin-top: 6px;
}
.ect-receipt code {
  font-size: 11px;
  font-family: 'JetBrains Mono', 'SF Mono', monospace;
  background: #f1f5f9;
  padding: 2px 6px;
  border-radius: 4px;
  color: #475569;
  word-break: break-all;
}
.ect-details {
  margin-top: 8px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.ect-detail-row {
  display: flex;
  gap: 8px;
  font-size: 12px;
}
.ect-detail-label {
  color: #64748b;
  font-weight: 600;
  min-width: 100px;
}
.ect-detail-value {
  color: #0f172a;
  font-weight: 500;
}
</style>
