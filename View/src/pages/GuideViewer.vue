<template>
  <div class="guide-page animate-in">
    <!-- Hero -->
    <GuideHero
      :active-role="activeRole"
      :total-pages="filteredPages.length"
      @select-role="activeRole = $event"
    />

    <!-- Tabs -->
    <div class="guide-tabs-bar animate-in">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        class="guide-tab"
        :class="{ active: activeTab === tab.id }"
        @click="activeTab = tab.id"
      >
        <span class="tab-icon">{{ tab.icon }}</span>
        <span class="tab-label">{{ tab.label }}</span>
      </button>
    </div>

    <div class="guide-body">
      <!-- ========== TAB: TỔNG QUAN ========== -->
      <section v-if="activeTab === 'overview'" class="guide-section animate-in">
        <div class="overview-grid">
          <div class="ov-card">
            <h3>🔐 Hệ thống kiểm soát ra vào thông minh</h3>
            <p>V-Shield giúp kiểm soát ai được ra vào công ty bằng QR động và biển số xe, vào thời điểm nào, qua cổng nào. Mọi quyết định đều được ghi lại để truy vết.</p>
          </div>
          <div class="ov-card">
            <h3>👥 4 vai trò người dùng</h3>
            <p><strong>Admin</strong>: Làm được tất cả. <strong>Bảo vệ</strong>: Coi camera, check-in khách. <strong>Quản lý</strong>: Xem báo cáo. <strong>Nhân viên</strong>: Tạo QR, chấm công.</p>
          </div>
          <div class="ov-card">
            <h3>📱 Các phương thức xác thực</h3>
            <p>QR động chống dùng lại, biển số xe (ANPR), thẻ từ và mã xác thực hai lớp (MFA) cho thao tác quản trị.</p>
          </div>
          <div class="ov-card">
            <h3>📊 Hệ thống gồm {{ totalPageCount }} trang chức năng</h3>
            <p>Chia thành 8 nhóm: Giám sát, SOC, Khách thăm, AI, Giao thông, Evidence, Video, Thiết bị. Xem tab "Hướng dẫn từng trang" để biết chi tiết.</p>
          </div>
        </div>

        <!-- Quick Guide: Tôi cần làm gì? -->
        <div v-if="showQuickGuide" class="quick-guide-box">
          <div class="quick-guide-header">
            <h4>🎯 "Tôi cần làm gì?" — Gợi ý nhanh cho bạn</h4>
            <button class="quick-guide-close" @click="showQuickGuide = false" title="Đóng">✕</button>
          </div>
          <div class="quick-guide-roles">
            <div
              v-for="guide in filteredQuickGuides"
              :key="guide.role"
              class="quick-guide-group"
            >
              <span class="quick-guide-role-label">{{ guide.role }}</span>
              <div class="quick-guide-links">
                <button
                  v-for="task in guide.tasks"
                  :key="task.path"
                  class="quick-guide-btn"
                  :class="{ read: readPages.has(task.path) }"
                  @click="scrollToPage(task.path)"
                >
                  <span class="qg-icon">{{ task.icon }}</span>
                  <span class="qg-label">{{ task.label }}</span>
                  <span v-if="readPages.has(task.path)" class="qg-check">✓</span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="ov-info-box">
          <h4>💡 Bắt đầu từ đâu?</h4>
          <ol>
            <li><strong>Đăng nhập</strong> bằng tài khoản được cấp.</li>
            <li>Nếu bạn là <strong>Nhân viên</strong>: QR ra vào sẽ tự động hiện ra.</li>
            <li>Nếu bạn là <strong>Bảo vệ</strong>: Mở màn hình Giám sát để xem camera.</li>
            <li>Nếu bạn là <strong>Quản lý</strong>: Xem Dashboard để nắm tình hình.</li>
            <li>Nếu bạn là <strong>Admin</strong>: Vào Cài đặt để cấu hình hệ thống.</li>
          </ol>
        </div>
      </section>

      <!-- ========== TAB: HƯỚNG DẪN TỪNG TRANG ========== -->
      <section v-if="activeTab === 'pages'" class="guide-section animate-in">
        <!-- Filter bar -->
        <div class="filter-bar">
          <div class="search-box">
            <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"/><path d="M21 21l-4.35-4.35"/>
            </svg>
            <input
              v-model="searchQuery"
              type="text"
              placeholder="Tìm trang hướng dẫn..."
            />
          </div>
          <select v-model="groupFilter" class="filter-select">
            <option value="all">📂 Tất cả nhóm</option>
            <option v-for="g in groups" :key="g.id" :value="g.id">{{ g.icon }} {{ g.label }}</option>
          </select>
        </div>

        <!-- Empty state -->
        <div v-if="filteredPages.length === 0" class="empty-state">
          <p>Không tìm thấy trang phù hợp. Thử tìm từ khóa khác hoặc đổi vai trò.</p>
        </div>

        <!-- Pages by group -->
        <div v-for="group in groupedFiltered" :key="group.id" class="group-section">
          <h3 class="group-title">
            <span class="group-icon">{{ group.icon }}</span>
            {{ group.label }}
            <span class="group-count">{{ group.pages.length }} trang</span>
          </h3>
          <div class="page-card-list">
            <GuidePageCard
              v-for="page in group.pages"
              :key="page.path"
              :page="page"
              :data-page-path="page.path"
              :is-open="openPage === page.path"
              :is-read="readPages.has(page.path)"
              :group-color="group.color"
              @toggle="handleToggle(page.path)"
            />
          </div>
        </div>
      </section>

      <!-- ========== TAB: FAQ ========== -->
      <section v-if="activeTab === 'faq'" class="guide-section animate-in">
        <div class="faq-header">
          <h2>❓ Câu hỏi thường gặp</h2>
          <p>Những thắc mắc phổ biến khi sử dụng V-Shield.</p>
        </div>
        <div class="faq-list">
          <div
            v-for="(faq, idx) in faqs"
            :key="idx"
            class="faq-item"
            :class="{ open: openFaq === idx }"
          >
            <button class="faq-question" @click="openFaq = openFaq === idx ? -1 : idx">
              <span>{{ faq.q }}</span>
              <svg :class="{ rotated: openFaq === idx }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M6 9l6 6 6-6"/>
              </svg>
            </button>
            <div v-if="openFaq === idx" class="faq-answer">
              <p>{{ faq.a }}</p>
            </div>
          </div>
        </div>
        <div class="faq-footer">
          <p>Chưa tìm thấy câu trả lời? Hãy hỏi trợ lý AI ở góc dưới màn hình!</p>
        </div>
      </section>
    </div>

    <!-- Bottom -->
    <div class="guide-footer">
      <span>V-Shield Security Platform v2.0</span>
      <span>•</span>
      <span>{{ filteredPages.length }} trang hướng dẫn</span>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, nextTick } from 'vue'
import GuideHero from '../components/guide/GuideHero.vue'
import GuidePageCard from '../components/guide/GuidePageCard.vue'
import { pageData, groups, faqs, quickGuides } from '../data/guideData.js'

// State
const activeTab = ref('overview')
const activeRole = ref('all')
const searchQuery = ref('')
const groupFilter = ref('all')
const openPage = ref(null)
const openFaq = ref(-1)
const showQuickGuide = ref(true)

// Read tracking — lưu trang đã đọc vào localStorage
const STORAGE_KEY = 'vshield_guide_read'
const readPages = ref(new Set())

// Khôi phục từ localStorage
const initReadPages = () => {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) {
      readPages.value = new Set(JSON.parse(stored))
    }
  } catch {}
}
initReadPages()

const markRead = (path) => {
  if (!readPages.value.has(path)) {
    readPages.value.add(path)
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify([...readPages.value]))
    } catch {}
  }
}

// Tabs
const tabs = [
  { id: 'overview', icon: '📖', label: 'Tổng quan' },
  { id: 'pages', icon: '📚', label: 'Hướng dẫn từng trang' },
  { id: 'faq', icon: '❓', label: 'Câu hỏi thường gặp' },
]

// Filter pages
const filteredPages = computed(() => {
  return pageData.filter(p => {
    // Role filter
    if (activeRole.value !== 'all' && !p.roles.includes(activeRole.value) && !p.roles.includes('Tất cả')) return false
    // Search
    if (searchQuery.value) {
      const q = searchQuery.value.toLowerCase()
      const matchName = p.label.toLowerCase().includes(q)
      const matchDesc = p.mucDich.toLowerCase().includes(q)
      const matchSteps = p.steps?.some(s => s.title.toLowerCase().includes(q) || s.moTa.toLowerCase().includes(q))
      if (!matchName && !matchDesc && !matchSteps) return false
    }
    // Group filter
    if (groupFilter.value !== 'all') {
      const group = groups.find(g => g.id === groupFilter.value)
      if (group && p.group !== group.label) return false
    }
    return true
  })
})

const totalPageCount = computed(() => pageData.length)

// Group filtered pages
const groupedFiltered = computed(() => {
  const result = []
  for (const group of groups) {
    const pages = filteredPages.value.filter(p => p.group === group.label)
    if (pages.length > 0) {
      result.push({ ...group, pages })
    }
  }
  return result
})

// Filter quick guides theo vai trò đang chọn
const filteredQuickGuides = computed(() => {
  if (activeRole.value === 'all') return quickGuides
  return quickGuides.filter(g => g.role === activeRole.value)
})

const handleToggle = (path) => {
  if (openPage.value === path) {
    openPage.value = null
  } else {
    openPage.value = path
    markRead(path)
  }
}

const clearReadHistory = () => {
  readPages.value = new Set()
  localStorage.removeItem(STORAGE_KEY)
}

const scrollToPage = async (path) => {
  activeTab.value = 'pages'
  openPage.value = path
  markRead(path)
  await nextTick()
  const el = document.querySelector(`[data-page-path="${path}"]`)
  if (el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'center' })
    el.classList.add('highlight-flash')
    setTimeout(() => el.classList.remove('highlight-flash'), 1500)
  }
}
</script>

<style scoped>
.guide-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

/* Tabs */
.guide-tabs-bar {
  display: flex;
  gap: 4px;
  padding: 0 32px;
  margin-top: 20px;
  border-bottom: 1px solid var(--border-color);
}
.guide-tab {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 20px;
  border: none;
  background: transparent;
  color: var(--text-secondary);
  font-size: 0.92rem;
  font-weight: 600;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
  transition: all 0.2s ease;
}
.guide-tab:hover {
  color: var(--text-primary);
  background: rgba(15,124,130,0.04);
}
.guide-tab.active {
  color: var(--accent-primary);
  border-bottom-color: var(--accent-primary);
}
.tab-icon { font-size: 1.1rem; }
.tab-label { white-space: nowrap; }

/* Body */
.guide-body {
  flex: 1;
  padding: 24px 32px 48px;
  max-width: 1100px;
  margin: 0 auto;
  width: 100%;
}

/* Overview tab */
.overview-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 14px;
  margin-bottom: 24px;
}
.ov-card {
  padding: 20px;
  border-radius: 16px;
  border: 1px solid var(--border-color);
  background: var(--bg-card);
}
.ov-card h3 {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 8px;
}
.ov-card p {
  font-size: 0.88rem;
  color: var(--text-secondary);
  line-height: 1.55;
}
.ov-info-box {
  padding: 24px;
  border-radius: 16px;
  border: 1px solid var(--border-color);
  background: linear-gradient(135deg, rgba(16,185,129,0.04), rgba(15,124,130,0.04));
}
.ov-info-box h4 {
  font-size: 1.05rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 12px;
}
.ov-info-box ol {
  margin: 0;
  padding-left: 20px;
}
.ov-info-box li {
  font-size: 0.9rem;
  color: var(--text-secondary);
  line-height: 1.7;
}

/* Quick Guide */
.quick-guide-box {
  margin-bottom: 20px;
  padding: 20px 24px;
  border-radius: 16px;
  border: 1px solid rgba(84,196,211,0.25);
  background: linear-gradient(135deg, rgba(84,196,211,0.06), rgba(59,130,246,0.04));
  position: relative;
}
.quick-guide-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
}
.quick-guide-header h4 {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
}
.quick-guide-close {
  width: 28px;
  height: 28px;
  border-radius: 8px;
  border: none;
  background: rgba(0,0,0,0.04);
  color: var(--text-muted);
  cursor: pointer;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s;
}
.quick-guide-close:hover {
  background: rgba(0,0,0,0.08);
  color: var(--text-primary);
}
.quick-guide-roles {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.quick-guide-group {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}
.quick-guide-role-label {
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--text-muted);
  min-width: 70px;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
.quick-guide-links {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}
.quick-guide-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 7px 14px;
  border-radius: 999px;
  border: 1px solid var(--border-color);
  background: var(--bg-card);
  color: var(--text-secondary);
  font-size: 0.82rem;
  cursor: pointer;
  transition: all 0.15s ease;
}
.quick-guide-btn:hover {
  border-color: var(--accent-primary);
  color: var(--accent-primary);
  background: rgba(84,196,211,0.06);
}
.quick-guide-btn.read {
  opacity: 0.6;
}
.qg-icon { font-size: 0.9rem; }
.qg-check {
  font-size: 0.7rem;
  color: #10b981;
  font-weight: 700;
}

/* Read Progress */
.read-progress {
  margin-bottom: 20px;
}
.read-progress-info {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 6px;
  font-size: 0.82rem;
  color: var(--text-secondary);
}
.read-progress-info strong {
  color: var(--accent-primary);
}
.read-clear-btn {
  font-size: 0.78rem;
  padding: 4px 12px;
  border-radius: 8px;
  border: 1px solid var(--border-color);
  background: transparent;
  color: var(--text-muted);
  cursor: pointer;
  transition: all 0.15s;
}
.read-clear-btn:hover {
  border-color: #ef4444;
  color: #ef4444;
}
.read-progress-bar {
  width: 100%;
  height: 6px;
  border-radius: 999px;
  background: var(--border-color);
  overflow: hidden;
}
.read-progress-fill {
  height: 100%;
  border-radius: 999px;
  background: linear-gradient(90deg, var(--accent-primary), #10b981);
  transition: width 0.3s ease;
}

/* Highlight flash animation */
.highlight-flash {
  animation: flashHighlight 1.5s ease;
}
@keyframes flashHighlight {
  0%, 100% { box-shadow: 0 0 0 0 rgba(84,196,211,0); }
  20% { box-shadow: 0 0 0 4px rgba(84,196,211,0.3); }
  40% { box-shadow: 0 0 0 2px rgba(84,196,211,0.15); }
}

/* Filter bar */
.filter-bar {
  display: flex;
  gap: 12px;
  margin-bottom: 20px;
  flex-wrap: wrap;
}
.search-box {
  position: relative;
  flex: 1;
  min-width: 240px;
}
.search-icon {
  position: absolute;
  left: 14px;
  top: 50%;
  transform: translateY(-50%);
  width: 18px;
  height: 18px;
  color: var(--text-muted);
}
.search-box input {
  width: 100%;
  min-height: 44px;
  padding: 0 14px 0 42px;
  border-radius: 12px;
  border: 1px solid var(--border-color);
  background: var(--bg-input);
  color: var(--text-primary);
  font-size: 0.9rem;
  transition: border-color 0.2s;
}
.search-box input:focus {
  border-color: rgba(15,124,130,0.36);
  box-shadow: 0 0 0 3px rgba(84,196,211,0.15);
  outline: none;
}
.filter-select {
  min-height: 44px;
  padding: 0 36px 0 14px;
  border-radius: 12px;
  border: 1px solid var(--border-color);
  background: var(--bg-input);
  color: var(--text-primary);
  font-size: 0.88rem;
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='%236d8291'%3E%3Cpath d='M7 10l5 5 5-5z'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 10px center;
  background-size: 18px;
  cursor: pointer;
  min-width: 160px;
}

/* Groups */
.group-section {
  margin-bottom: 28px;
}
.group-title {
  display: flex;
  align-items: center;
  gap: 10px;
  font-family: var(--font-heading);
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 12px;
  padding-bottom: 10px;
  border-bottom: 1px solid var(--border-color);
}
.group-icon { font-size: 1.2rem; }
.group-count {
  font-size: 0.78rem;
  color: var(--text-muted);
  font-weight: 500;
  margin-left: auto;
}

.page-card-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

/* Empty state */
.empty-state {
  padding: 48px;
  text-align: center;
  color: var(--text-muted);
  border: 1px dashed var(--border-color);
  border-radius: 16px;
  font-size: 0.95rem;
}

/* FAQ */
.faq-header {
  margin-bottom: 20px;
}
.faq-header h2 {
  font-family: var(--font-heading);
  font-size: 1.4rem;
  color: var(--text-primary);
  margin-bottom: 6px;
}
.faq-header p {
  color: var(--text-secondary);
  font-size: 0.92rem;
}
.faq-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.faq-item {
  border-radius: 14px;
  border: 1px solid var(--border-color);
  background: var(--bg-card);
  overflow: hidden;
}
.faq-question {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  padding: 16px 20px;
  border: none;
  background: transparent;
  color: var(--text-primary);
  font-size: 0.94rem;
  font-weight: 600;
  cursor: pointer;
  text-align: left;
}
.faq-question:hover { background: rgba(15,124,130,0.03); }
.faq-question svg {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  color: var(--text-muted);
  transition: transform 0.2s ease;
}
.faq-question svg.rotated { transform: rotate(180deg); }
.faq-answer {
  padding: 0 20px 16px;
}
.faq-answer p {
  color: var(--text-secondary);
  font-size: 0.9rem;
  line-height: 1.65;
  white-space: pre-wrap;
}
.faq-footer {
  margin-top: 24px;
  padding: 20px;
  text-align: center;
  color: var(--text-muted);
  border-radius: 14px;
  border: 1px dashed var(--border-color);
}
.faq-footer p {
  font-size: 0.9rem;
}

/* Footer */
.guide-footer {
  padding: 20px;
  text-align: center;
  display: flex;
  gap: 10px;
  justify-content: center;
  color: var(--text-muted);
  font-size: 0.82rem;
  border-top: 1px solid var(--border-color);
}

/* Animations */
.animate-in {
  animation: fadeIn 0.3s ease;
}
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(8px); }
  to { opacity: 1; transform: translateY(0); }
}

/* Responsive */
@media (max-width: 768px) {
  .guide-tabs-bar {
    padding: 0 16px;
    overflow-x: auto;
    gap: 0;
  }
  .guide-tab {
    padding: 10px 14px;
    font-size: 0.84rem;
  }
  .guide-body {
    padding: 16px;
  }
  .filter-bar {
    flex-direction: column;
  }
  .search-box { min-width: 100%; }
  .filter-select { min-width: 100%; }
  .overview-grid { grid-template-columns: 1fr; }
}
</style>
