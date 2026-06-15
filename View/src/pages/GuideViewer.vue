<template>
  <div class="guide-page animate-in">
    <!-- Hero Header -->
    <div class="guide-hero">
      <div class="guide-hero-bg" aria-hidden="true">
        <div class="hero-orb a"></div>
        <div class="hero-orb b"></div>
      </div>
      <div class="guide-hero-content">
        <span class="guide-kicker">📖 Hướng dẫn sử dụng</span>
        <h1 class="guide-title">V-Shield Security Platform</h1>
        <p class="guide-subtitle">
          Tài liệu hướng dẫn toàn diện — bao gồm tất cả màn hình, chức năng và luồng nghiệp vụ
          dành cho mọi vai trò: <strong>Admin</strong>, <strong>Bảo vệ</strong>, <strong>Quản lý</strong> và <strong>Nhân viên</strong>.
        </p>
        <div class="guide-meta">
          <span class="guide-meta-chip">Phiên bản 2.0</span>
          <span class="guide-meta-chip">Cập nhật 06/2026</span>
          <span class="guide-meta-chip">{{ totalPages }} trang chức năng</span>
        </div>
      </div>
    </div>

    <div class="guide-layout">
      <!-- Sidebar navigation -->
      <aside class="guide-sidebar">
        <div class="guide-sidebar-sticky">
          <div class="sidebar-search">
            <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"/><path d="M21 21l-4.35-4.35"/>
            </svg>
            <input v-model="searchQuery" type="text" placeholder="Tìm kiếm chức năng..." />
          </div>

          <nav class="sidebar-nav">
            <button
              v-for="section in filteredSections"
              :key="section.id"
              class="sidebar-section-btn"
              :class="{ active: activeSection === section.id }"
              @click="activeSection = section.id"
            >
              <span class="section-icon" v-html="section.icon"></span>
              <span class="section-label">{{ section.label }}</span>
              <span class="section-count">{{ section.count }}</span>
            </button>
          </nav>

          <div class="sidebar-roles">
            <span class="roles-label">Chọn vai trò:</span>
            <div class="roles-chips">
              <button
                v-for="role in roles"
                :key="role.id"
                class="role-chip"
                :class="{ active: activeRole === role.id }"
                @click="activeRole = role.id"
              >
                <span class="role-dot" :style="{ background: role.color }"></span>
                {{ role.label }}
              </button>
            </div>
          </div>
        </div>
      </aside>

      <!-- Main Content -->
      <main class="guide-main">
        <!-- Section: Overview -->
        <section v-if="activeSection === 'overview'" class="guide-section">
          <div class="section-header">
            <h2>Tổng quan hệ thống</h2>
            <p>V-Shield là nền tảng kiểm soát ra vào thông minh, tích hợp nhận diện khuôn mặt (Face ID), nhận diện biển số (ANPR), QR động và giám sát camera tập trung.</p>
          </div>

          <div class="feature-grid">
            <div class="feature-card">
              <div class="feature-icon blue">🔐</div>
              <h3>Xác thực đa tầng</h3>
              <p>Face ID + QR động + Biển số xe + MFA, đảm bảo an ninh nhiều lớp.</p>
            </div>
            <div class="feature-card">
              <div class="feature-icon green">📹</div>
              <h3>Giám sát trực tiếp</h3>
              <p>Theo dõi camera realtime, nhận diện khuôn mặt và biển số tức thời.</p>
            </div>
            <div class="feature-card">
              <div class="feature-icon purple">📊</div>
              <h3>Báo cáo & Analytics</h3>
              <p>Dashboard tổng quan, UEBA phân tích hành vi, AI Intelligence dự báo.</p>
            </div>
            <div class="feature-card">
              <div class="feature-icon orange">👥</div>
              <h3>Quản lý khách & Nhân sự</h3>
              <p>Pre-registration, check-in kiosk, danh sách khách, watchlist, contractor.</p>
            </div>
            <div class="feature-card">
              <div class="feature-icon red">🚗</div>
              <h3>Giao thông & Bãi đỗ</h3>
              <p>Lane dashboard, barrier control, plate review, parking management.</p>
            </div>
            <div class="feature-card">
              <div class="feature-icon teal">⚙️</div>
              <h3>Enterprise & Compliance</h3>
              <p>SOC Alarm, Policy Engine, Evidence Repository, Retention & Legal Hold.</p>
            </div>
          </div>

          <!-- Role info cards -->
          <div class="role-overview">
            <h3>Phân quyền theo vai trò</h3>
            <div class="role-grid">
              <div class="role-card" v-for="r in roles" :key="r.id" :style="{ borderLeftColor: r.color }">
                <div class="role-card-head">
                  <span class="role-dot" :style="{ background: r.color }"></span>
                  <strong>{{ r.label }}</strong>
                </div>
                <p>{{ r.description }}</p>
                <div class="role-stats">
                  <span>{{ r.pageCount }} trang</span>
                  <span>{{ r.featureCount }} chức năng</span>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- Section: Role Workflow -->
        <section v-if="activeSection === 'workflow'" class="guide-section">
          <div class="section-header">
            <h2>Luồng hoạt động theo vai trò</h2>
            <p>Hướng dẫn từng bước cho mỗi vai trò khi sử dụng hệ thống V-Shield.</p>
          </div>

          <!-- Workflow Admin -->
          <div v-if="activeRole === 'admin' || activeRole === 'all'" class="workflow-block">
            <h3 class="workflow-role-title">
              <span class="role-dot" style="background: #3b82f6"></span>
              Luồng cho Quản trị viên (Admin)
            </h3>
            <div class="workflow-steps">
              <div class="wf-step">
                <div class="wf-step-num">1</div>
                <div class="wf-step-body">
                  <h4>Đăng nhập & Xác thực</h4>
                  <p>Truy cập <router-link to="/login">/login</router-link> với tài khoản Admin. Có thể yêu cầu MFA (Authenticator).</p>
                </div>
              </div>
              <div class="wf-step">
                <div class="wf-step-num">2</div>
                <div class="wf-step-body">
                  <h4>Dashboard & Giám sát</h4>
                  <p>Xem tổng quan tại <router-link to="/dashboard">Dashboard</router-link>, theo dõi camera tại <router-link to="/monitoring">Giám sát</router-link>, kiểm tra <router-link to="/access-logs">Lịch sử ra vào</router-link>.</p>
                </div>
              </div>
              <div class="wf-step">
                <div class="wf-step-num">3</div>
                <div class="wf-step-body">
                  <h4>Quản trị Nhân sự & Tài khoản</h4>
                  <p>Quản lý <router-link to="/employees">Nhân viên</router-link>, <router-link to="/users">Tài khoản hệ thống</router-link>, <router-link to="/settings">Cài đặt</router-link>, <router-link to="/policy-engine">Policy Engine</router-link>.</p>
                </div>
              </div>
              <div class="wf-step">
                <div class="wf-step-num">4</div>
                <div class="wf-step-body">
                  <h4>Quản lý Khách & Thiết bị</h4>
                  <p>Xử lý <router-link to="/pre-registrations">Hẹn trước</router-link>, <router-link to="/guest-profiles">Hồ sơ khách</router-link>, cấu hình <router-link to="/device-management">Camera & cổng</router-link>.</p>
                </div>
              </div>
              <div class="wf-step">
                <div class="wf-step-num">5</div>
                <div class="wf-step-body">
                  <h4>Bảo mật & Tuân thủ</h4>
                  <p>Giám sát <router-link to="/soc-console">SOC Alarm</router-link>, quản lý <router-link to="/evidence-repository">Evidence</router-link>, <router-link to="/compliance-reports">Báo cáo tuân thủ</router-link>.</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Workflow Bảo vệ -->
          <div v-if="activeRole === 'baove' || activeRole === 'all'" class="workflow-block">
            <h3 class="workflow-role-title">
              <span class="role-dot" style="background: #10b981"></span>
              Luồng cho Bảo vệ (BaoVe)
            </h3>
            <div class="workflow-steps">
              <div class="wf-step">
                <div class="wf-step-num">1</div>
                <div class="wf-step-body">
                  <h4>Đăng nhập & Dashboard</h4>
                  <p>Đăng nhập, xem Dashboard để nắm tình hình: xe trong bãi, khách hẹn, ngoại lệ.</p>
                </div>
              </div>
              <div class="wf-step">
                <div class="wf-step-num">2</div>
                <div class="wf-step-body">
                  <h4>Giám sát Camera</h4>
                  <p>Mở <router-link to="/monitoring">Giám sát trực tiếp</router-link> để theo dõi 4 luồng camera cùng lúc.</p>
                </div>
              </div>
              <div class="wf-step">
                <div class="wf-step-num">3</div>
                <div class="wf-step-body">
                  <h4>Xác thực tại cổng</h4>
                  <p>Dùng <router-link to="/gate-transit-monitor">Điều phối thông hành</router-link> hoặc <router-link to="/face-id-security">Face ID</router-link> và <router-link to="/license-plate-security">Biển số</router-link> để xác thực.</p>
                </div>
              </div>
              <div class="wf-step">
                <div class="wf-step-num">4</div>
                <div class="wf-step-body">
                  <h4>Xử lý khách & Ngoại lệ</h4>
                  <p>Check-in khách tại <router-link to="/reception">Reception</router-link>, xử lý <router-link to="/exceptions">Ngoại lệ</router-link>, kiểm tra <router-link to="/watchlist">Watchlist</router-link>.</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Workflow Quản lý -->
          <div v-if="activeRole === 'quanly' || activeRole === 'all'" class="workflow-block">
            <h3 class="workflow-role-title">
              <span class="role-dot" style="background: #8b5cf6"></span>
              Luồng cho Quản lý (QuanLy)
            </h3>
            <div class="workflow-steps">
              <div class="wf-step">
                <div class="wf-step-num">1</div>
                <div class="wf-step-body">
                  <h4>Dashboard & Báo cáo</h4>
                  <p>Dashboard tổng quan, <router-link to="/attendance/reports">Báo cáo công</router-link>, <router-link to="/access-logs">Lịch sử ra vào</router-link>.</p>
                </div>
              </div>
              <div class="wf-step">
                <div class="wf-step-num">2</div>
                <div class="wf-step-body">
                  <h4>Quản lý Nhân sự & Xe</h4>
                  <p><router-link to="/vehicles">Phương tiện</router-link>, <router-link to="/system-catalog">Danh mục hệ thống</router-link>, <router-link to="/exceptions">Xử lý ngoại lệ</router-link>.</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Workflow Nhân viên -->
          <div v-if="activeRole === 'staff' || activeRole === 'all'" class="workflow-block">
            <h3 class="workflow-role-title">
              <span class="role-dot" style="background: #f59e0b"></span>
              Luồng cho Nhân viên (Staff)
            </h3>
            <div class="workflow-steps">
              <div class="wf-step">
                <div class="wf-step-num">1</div>
                <div class="wf-step-body">
                  <h4>Đăng nhập & QR</h4>
                  <p>Đăng nhập → tự động chuyển đến <router-link to="/dynamic-qr-generator">Tạo QR động</router-link>. Giữ màn hình để quét tại cổng.</p>
                </div>
              </div>
              <div class="wf-step">
                <div class="wf-step-num">2</div>
                <div class="wf-step-body">
                  <h4>Chấm công & Nghỉ phép</h4>
                  <p>Xem <router-link to="/attendance/records">Bảng chấm công</router-link>, gửi <router-link to="/attendance/leave-requests">Đơn xin nghỉ</router-link>, xem <router-link to="/attendance/work-schedules">Lịch làm việc</router-link>.</p>
                </div>
              </div>
              <div class="wf-step">
                <div class="wf-step-num">3</div>
                <div class="wf-step-body">
                  <h4>Mời khách & Bản đồ</h4>
                  <p><router-link to="/host-visitor">Mời khách</router-link>, xem <router-link to="/campus-map">Bản đồ khuôn viên</router-link>.</p>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- Section: Pages Directory -->
        <section v-if="activeSection === 'pages'" class="guide-section">
          <div class="section-header">
            <h2>Danh mục trang chức năng</h2>
            <p>Tất cả {{ totalPages }} trang trong hệ thống, được tổ chức theo nhóm và vai trò truy cập.</p>
            <div class="filter-bar">
              <select v-model="pageFilter" class="guide-select">
                <option value="all">Tất cả nhóm</option>
                <option v-for="g in pageGroups" :key="g.label" :value="g.label">{{ g.label }}</option>
              </select>
              <select v-model="pageRoleFilter" class="guide-select">
                <option value="all">Tất cả vai trò</option>
                <option value="Admin">Admin</option>
                <option value="BaoVe">Bảo vệ</option>
                <option value="QuanLy">Quản lý</option>
                <option value="Staff">Nhân viên</option>
              </select>
            </div>
          </div>

          <div v-for="group in filteredPageGroups" :key="group.label" class="page-group">
            <h3 class="page-group-title">{{ group.label }}</h3>
            <div class="page-grid">
              <router-link
                v-for="page in group.pages"
                :key="page.path"
                :to="page.path"
                class="page-card"
                :class="{ restricted: !page.allRoles.includes(activeRole) && activeRole !== 'all' }"
              >
                <div class="page-card-head">
                  <span class="page-icon" v-html="page.icon"></span>
                  <div>
                    <strong>{{ page.label }}</strong>
                    <p>{{ page.desc }}</p>
                  </div>
                </div>
                <div class="page-card-meta">
                  <span class="page-badge">{{ page.group }}</span>
                  <div class="page-roles">
                    <span v-for="role in page.roles" :key="role" class="mini-role-chip" :class="role.toLowerCase()">{{ role }}</span>
                  </div>
                </div>
              </router-link>
            </div>
          </div>
        </section>

        <!-- Section: Features by Page -->
        <section v-if="activeSection === 'features'" class="guide-section">
          <div class="section-header">
            <h2>Chi tiết chức năng từng trang</h2>
            <p>Mô tả tất cả button, input, bảng và thao tác trên mỗi trang.</p>
            <div class="filter-bar">
              <select v-model="featurePage" class="guide-select">
                <option value="">Chọn trang để xem chi tiết...</option>
                <option v-for="p in allPages" :key="p.path" :value="p.path">{{ p.label }}</option>
              </select>
            </div>
          </div>

          <div v-if="featurePage" class="feature-detail">
            <div class="feature-detail-header">
              <span class="feature-detail-icon" v-html="getPageInfo(featurePage)?.icon || ''"></span>
              <div>
                <h3>{{ getPageInfo(featurePage)?.label || featurePage }}</h3>
                <p class="feature-detail-path">{{ featurePage }}</p>
              </div>
            </div>

            <div class="feature-detail-body">
              <div v-for="(detail, idx) in getPageDetails(featurePage)" :key="idx" class="detail-item">
                <span class="detail-tag" :class="detail.type">{{ detail.type === 'button' ? 'Nút' : detail.type === 'input' ? 'Ô nhập' : detail.type === 'table' ? 'Bảng' : detail.type === 'select' ? 'Chọn' : 'Tính năng' }}</span>
                <div class="detail-content">
                  <strong>{{ detail.name }}</strong>
                  <p>{{ detail.desc }}</p>
                </div>
              </div>
            </div>
          </div>
          <div v-else class="feature-empty">
            <p>Chọn một trang từ danh sách trên để xem chi tiết các chức năng.</p>
          </div>
        </section>

        <!-- Section: FAQ -->
        <section v-if="activeSection === 'faq'" class="guide-section">
          <div class="section-header">
            <h2>Câu hỏi thường gặp</h2>
            <p>Giải đáp nhanh các thắc mắc phổ biến khi sử dụng V-Shield.</p>
          </div>

          <div class="faq-list">
            <div v-for="(faq, idx) in faqs" :key="idx" class="faq-item" :class="{ open: openFaq === idx }">
              <button class="faq-question" @click="openFaq = openFaq === idx ? -1 : idx">
                <span>{{ faq.q }}</span>
                <svg :class="{ rotated: openFaq === idx }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M6 9l6 6 6-6"/>
                </svg>
              </button>
              <div class="faq-answer" v-if="openFaq === idx">
                <p v-for="(line, li) in faq.a.split('\n')" :key="li">{{ line }}</p>
              </div>
            </div>
          </div>
        </section>

        <!-- Bottom nav -->
        <div class="guide-bottom-nav">
          <span>V-Shield Security Platform v2.0</span>
          <span>•</span>
          <span>Hướng dẫn đầy đủ {{ totalPages }} trang chức năng</span>
        </div>
      </main>
    </div>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'

const searchQuery = ref('')
const activeSection = ref('overview')
const activeRole = ref('all')
const pageFilter = ref('all')
const pageRoleFilter = ref('all')
const featurePage = ref('')
const openFaq = ref(-1)

const roles = [
  { id: 'all', label: 'Tất cả', color: '#6b7280', description: 'Xem tất cả nội dung phù hợp với vai trò của bạn.', pageCount: '~65', featureCount: '200+' },
  { id: 'admin', label: 'Admin', color: '#3b82f6', description: 'Toàn quyền quản trị hệ thống, cấu hình, bảo mật, enterprise.', pageCount: '65', featureCount: '200+' },
  { id: 'baove', label: 'Bảo vệ', color: '#10b981', description: 'Giám sát camera, xác thực cổng, check-in khách, xử lý ngoại lệ.', pageCount: '~40', featureCount: '120+' },
  { id: 'quanly', label: 'Quản lý', color: '#8b5cf6', description: 'Báo cáo, duyệt đơn, danh mục, giám sát tổng quan.', pageCount: '~25', featureCount: '80+' },
  { id: 'staff', label: 'Nhân viên', color: '#f59e0b', description: 'QR động, chấm công, đơn nghỉ, mời khách, bản đồ.', pageCount: '~10', featureCount: '30+' },
]

const totalPages = computed(() => allPages.length)

const sections = [
  { id: 'overview', label: 'Tổng quan', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></svg>', count: 0 },
  { id: 'workflow', label: 'Luồng hoạt động', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>', count: 0 },
  { id: 'pages', label: 'Danh mục trang', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>', count: totalPages },
  { id: 'features', label: 'Chi tiết chức năng', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="9"/><path d="M12 8v4l3 3"/></svg>', count: 0 },
  { id: 'faq', label: 'FAQ', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="9"/><path d="M12 16v-4"/><path d="M12 8h.01"/></svg>', count: 0 },
]

const filteredSections = computed(() => sections.map(s => ({
  ...s,
  count: s.id === 'pages' ? filteredPageGroups.value.reduce((acc, g) => acc + g.pages.length, 0) : s.count
})))

const pageGroups = [
  {
    label: 'Tổng quan & Giám sát',
    pages: [
      { path: '/dashboard', label: 'Dashboard', desc: 'Bảng điều phối tổng quan, metric, biểu đồ traffic', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></svg>', roles: ['Admin','BaoVe','QuanLy'], group: 'Tổng quan' },
      { path: '/monitoring', label: 'Giám sát trực tiếp', desc: 'Theo dõi camera realtime tối đa 4 luồng', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M23 19a2 2 0 01-2 2H3a2 2 0 01-2-2V8a2 2 0 012-2h4l2-3h6l2 3h4a2 2 0 012 2z"/><circle cx="12" cy="13" r="4"/></svg>', roles: ['Admin','BaoVe','QuanLy'], group: 'Giám sát' },
      { path: '/access-logs', label: 'Tra cứu vào/ra', desc: 'Lịch sử ra vào với bộ lọc đa chiều', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 8v4l3 3"/><circle cx="12" cy="12" r="9"/></svg>', roles: ['Admin','BaoVe','QuanLy'], group: 'Giám sát' },
      { path: '/ueba', label: 'UEBA', desc: 'Phân tích hành vi bất thường người dùng', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="9"/><path d="M12 8v4l3 3"/><path d="M5 3l4 4"/></svg>', roles: ['Admin','BaoVe','QuanLy'], group: 'Phân tích' },
    ]
  },
  {
    label: 'SOC & Enterprise',
    pages: [
      { path: '/soc-console', label: 'SOC Alarm Console', desc: 'Alarm queue, incident command, security events', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/><path d="M12 8v4"/><path d="M12 16h.01"/></svg>', roles: ['Admin','BaoVe'], group: 'Enterprise' },
      { path: '/identity-management', label: 'Identity Management', desc: 'IdP, SSO, OIDC, auto offboarding', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="8.5" cy="7" r="4"/><path d="M20 8v6"/><path d="M23 11h-6"/></svg>', roles: ['Admin','BaoVe'], group: 'Enterprise' },
      { path: '/site-hierarchy', label: 'Site Hierarchy', desc: 'Tree structure, assets, backfill UI', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 2L2 7l10 5 10-5-10-5z"/><path d="M2 17l10 5 10-5"/><path d="M2 12l10 5 10-5"/></svg>', roles: ['Admin','BaoVe'], group: 'Enterprise' },
      { path: '/policy-engine', label: 'Policy Engine', desc: 'Design, simulate, lockdown security policies', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="11" width="18" height="10" rx="2"/><path d="M7 11V7a5 5 0 0110 0v4"/><circle cx="12" cy="16" r="1"/></svg>', roles: ['Admin'], group: 'Enterprise' },
      { path: '/enterprise-security', label: 'Enterprise Console', desc: 'Workspaces, setup, security overview', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8"/><path d="M12 17v4"/></svg>', roles: ['Admin','BaoVe'], group: 'Enterprise' },
    ]
  },
  {
    label: 'Quản lý Khách thăm',
    pages: [
      { path: '/pre-registrations', label: 'Danh sách hẹn trước', desc: 'Duyệt, theo dõi đăng ký trước của khách', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M16 4h2a2 2 0 012 2v14a2 2 0 01-2 2H6a2 2 0 01-2-2V6a2 2 0 012-2h2"/><rect x="8" y="2" width="8" height="4" rx="1.5"/><path d="M9 14l2 2 4-4"/></svg>', roles: ['Admin'], group: 'Khách thăm' },
      { path: '/registration-links', label: 'Link đăng ký tự động', desc: 'Tạo token, URL gửi khách đăng ký', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M10 13a5 5 0 007.54.54l3-3a5 5 0 00-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 00-7.54-.54l-3 3a5 5 0 007.07 7.07l1.71-1.71"/></svg>', roles: ['Admin'], group: 'Khách thăm' },
      { path: '/guest-profiles', label: 'Hồ sơ khách', desc: 'Danh bạ khách quen, tái sử dụng nhanh', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M16 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/><circle cx="10" cy="7" r="4"/><path d="M20 8v6"/><path d="M17 11h6"/></svg>', roles: ['Admin','BaoVe'], group: 'Khách thăm' },
      { path: '/reception', label: 'Reception', desc: 'Check-in, walk-in, overstays, visitor management', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8"/><path d="M12 17v4"/><circle cx="12" cy="10" r="2"/></svg>', roles: ['Admin','BaoVe'], group: 'Khách thăm' },
      { path: '/kiosk', label: 'Kiosk Check-in', desc: 'Tự check-in tại quầy, in visitor pass', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="4" y="2" width="16" height="20" rx="2"/><path d="M9 6h6"/><path d="M12 10v4"/><path d="M10 12h4"/></svg>', roles: ['Admin','BaoVe'], group: 'Khách thăm' },
      { path: '/host-visitor', label: 'Mời khách', desc: 'Tạo lời mời gửi email/SMS cho khách', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M16 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/><circle cx="10" cy="7" r="4"/><path d="M22 12h-6"/><path d="M19 9v6"/></svg>', roles: ['Admin','Staff','BaoVe'], group: 'Khách thăm' },
      { path: '/watchlist', label: 'Watchlist', desc: 'Rà soát đối sánh, danh sách theo dõi', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="10"/><path d="M12 8v4l3 3"/></svg>', roles: ['Admin','BaoVe'], group: 'Khách thăm' },
    ]
  },
  {
    label: 'AI & Thiết bị',
    pages: [
      { path: '/face-id-security', label: 'Face ID', desc: 'Nhận diện khuôn mặt realtime, check-in bằng khuôn mặt', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M8 3H6a3 3 0 00-3 3v2"/><path d="M16 3h2a3 3 0 013 3v2"/><path d="M8 21H6a3 3 0 01-3-3v-2"/><path d="M16 21h2a3 3 0 003-3v-2"/><path d="M9 10a3 3 0 016 0v4a3 3 0 01-6 0z"/></svg>', roles: ['Admin','BaoVe'], group: 'AI' },
      { path: '/license-plate-security', label: 'Nhận diện biển số', desc: 'ANPR, OCR biển số, fuzzy match', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="6" width="18" height="12" rx="2"/><path d="M7 10h10"/><path d="M7 14h4"/></svg>', roles: ['Admin','BaoVe'], group: 'AI' },
      { path: '/face-video-monitor', label: 'Video khuôn mặt', desc: 'Đối soát Face ID qua video stream', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="5" width="15" height="14" rx="2"/><path d="M18 10l3-2v8l-3-2"/><path d="M8 10a2 2 0 114 0v4a2 2 0 11-4 0z"/></svg>', roles: ['Admin','BaoVe'], group: 'AI' },
      { path: '/gate-transit-monitor', label: 'Gate Transit', desc: 'Điều phối thông hành: QR + biển số + face theo làn', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 7h18"/><path d="M6 7v10"/><path d="M18 7v10"/><path d="M9 11h6"/><path d="M9 15h6"/><path d="M12 7v10"/></svg>', roles: ['Admin','BaoVe'], group: 'AI' },
      { path: '/dynamic-qr-generator', label: 'Tạo QR động', desc: 'Sinh mã QR realtime, tự làm mới theo chu kỳ', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 4h6v6H4z"/><path d="M14 4h6v6h-6z"/><path d="M4 14h6v6H4z"/><path d="M15 15h2"/><path d="M19 15v5"/><path d="M14 19h5"/></svg>', roles: ['Admin','Staff','BaoVe'], group: 'AI' },
      { path: '/dynamic-qr-scanner', label: 'Quét QR động', desc: 'Quét và xác thực QR động tại cổng', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 4h5v5H4z"/><path d="M15 4h5v5h-5z"/><path d="M4 15h5v5H4z"/><path d="M16 16h1"/><path d="M19 16h1"/><path d="M16 19h4"/><path d="M12 7h1"/><path d="M12 12h1"/><path d="M7 12h5"/></svg>', roles: ['Admin','BaoVe'], group: 'AI' },
    ]
  },
  {
    label: 'Thiết bị & Hạ tầng',
    pages: [
      { path: '/device-management', label: 'Camera & cổng', desc: 'Cấu hình camera, cổng truy cập, go2rtc', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 7h16v10H4z"/><path d="M9 7V4h6v3"/><path d="M8 17h8"/><path d="M7 21h10"/></svg>', roles: ['Admin','BaoVe'], group: 'Thiết bị' },
      { path: '/device-topology', label: 'Device Topology', desc: 'Sơ đồ thiết bị enterprise, network graph', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="12" cy="12" r="3"/><path d="M12 9V6"/><path d="M12 18v-3"/><path d="M9 12H6"/><path d="M18 12h-3"/></svg>', roles: ['Admin','BaoVe'], group: 'Thiết bị' },
      { path: '/device-health', label: 'Device Health', desc: 'Sức khỏe thiết bị, AI diagnosis, metrics', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>', roles: ['Admin','BaoVe'], group: 'Thiết bị' },
      { path: '/provisioning-wizard', label: 'Provisioning', desc: 'Cấp phát thiết bị mới, bulk registration', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 2l8 4v6c0 5-3.5 8.5-8 10-4.5-1.5-8-5-8-10V6l8-4z"/><path d="M9 12l2 2 4-4"/></svg>', roles: ['Admin'], group: 'Thiết bị' },
      { path: '/offline-packages', label: 'Offline Packages', desc: 'Policy cho offline resilience, sync packages', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 002 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/></svg>', roles: ['Admin','BaoVe'], group: 'Thiết bị' },
      { path: '/simulator-panel', label: 'Simulator', desc: 'Virtual controller, fault injection, testing', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>', roles: ['Admin','BaoVe'], group: 'Thiết bị' },
      { path: '/biometrics', label: 'Biometrics', desc: 'Dữ liệu nhận diện, model khuôn mặt, độ phủ AI', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 15a3 3 0 100-6 3 3 0 000 6z"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"/></svg>', roles: ['Admin'], group: 'Thiết bị' },
    ]
  },
  {
    label: 'Giao thông & Bãi đỗ',
    pages: [
      { path: '/lane-dashboard', label: 'Lane Dashboard', desc: 'Sức khỏe làn, barrier, traffic flow', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="4" y="4" width="16" height="16" rx="2"/><path d="M4 12h16"/><path d="M12 4v16"/></svg>', roles: ['Admin','BaoVe'], group: 'Giao thông' },
      { path: '/barrier-panel', label: 'Barrier Control', desc: 'Điều khiển barrier, parking management', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 12H3"/><path d="M12 3v18"/><path d="M7 8l5-5 5 5"/><path d="M7 16l5 5 5-5"/></svg>', roles: ['Admin','BaoVe'], group: 'Giao thông' },
      { path: '/plate-review', label: 'Plate Review', desc: 'Duyệt ảnh biển số AI, xác nhận kết quả', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="2" y="6" width="20" height="12" rx="2"/><path d="M6 10h4"/><path d="M14 10h4"/></svg>', roles: ['Admin','BaoVe'], group: 'Giao thông' },
    ]
  },
  {
    label: 'Evidence & Compliance',
    pages: [
      { path: '/evidence-repository', label: 'Evidence Repository', desc: 'Quản lý vật chứng, chain of custody', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><path d="M16 13H8"/><path d="M16 17H8"/><path d="M10 9H8"/></svg>', roles: ['Admin'], group: 'Compliance' },
      { path: '/export-approval-queue', label: 'Export Approval', desc: 'Phê duyệt xuất evidence, audit trail', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>', roles: ['Admin'], group: 'Compliance' },
      { path: '/redaction-queue', label: 'Redaction Queue', desc: 'Xóa thông tin nhạy cảm khỏi evidence', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20 5H9l-7 7 7 7h11a2 2 0 002-2V7a2 2 0 00-2-2z"/><line x1="18" y1="9" x2="12" y2="15"/><line x1="12" y1="9" x2="18" y2="15"/></svg>', roles: ['Admin'], group: 'Compliance' },
      { path: '/retention-dashboard', label: 'Retention & Legal Hold', desc: 'Chính sách lưu giữ, niêm phong pháp lý', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0110 0v4"/></svg>', roles: ['Admin'], group: 'Compliance' },
      { path: '/compliance-reports', label: 'Compliance Reports', desc: 'Báo cáo tuân thủ, audit logs', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2"/><rect x="9" y="3" width="6" height="4" rx="1"/><path d="M9 14l2 2 4-4"/></svg>', roles: ['Admin'], group: 'Compliance' },
    ]
  },
  {
    label: 'Video & AI Review',
    pages: [
      { path: '/event-timeline', label: 'Event Timeline', desc: 'Chuỗi sự kiện an ninh theo thời gian', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="9"/><path d="M12 8v4l3 3"/><path d="M12 2v2"/><path d="M12 20v2"/><path d="M2 12h2"/><path d="M20 12h2"/></svg>', roles: ['Admin','BaoVe'], group: 'Video' },
      { path: '/video-search', label: 'Video Search', desc: 'Tìm kiếm video, bookmark, annotation', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="10" cy="10" r="8"/><path d="M16 16l5 5"/><rect x="3" y="12" width="14" height="4" rx="1"/></svg>', roles: ['Admin','BaoVe'], group: 'Video' },
      { path: '/ai-review-queue', label: 'AI Review Queue', desc: 'Chất lượng và đánh giá AI, human-in-the-loop', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><path d="M9 13l2 2 4-4"/></svg>', roles: ['Admin','BaoVe'], group: 'Video' },
      { path: '/correlation-view', label: 'Correlation', desc: 'Tương quan đa tín hiệu: face + plate + event', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/><circle cx="12" cy="12" r="2"/></svg>', roles: ['Admin','BaoVe'], group: 'Video' },
    ]
  },
]

const allPages = computed(() => pageGroups.flatMap(g => g.pages.map(p => ({ ...p, allRoles: p.roles }))))

const filteredPageGroups = computed(() => {
  return pageGroups
    .map(g => ({
      ...g,
      pages: g.pages.filter(p => {
        if (pageFilter.value !== 'all' && p.group !== pageFilter.value) return false
        if (pageRoleFilter.value !== 'all' && !p.roles.includes(pageRoleFilter.value)) return false
        if (searchQuery.value) {
          const q = searchQuery.value.toLowerCase()
          return p.label.toLowerCase().includes(q) || p.desc.toLowerCase().includes(q)
        }
        return true
      })
    }))
    .filter(g => g.pages.length > 0)
})

const getPageInfo = (path) => allPages.value.find(p => p.path === path)

const getPageDetails = (path) => {
  const details = {
    '/dashboard': [
      { type: 'feature', name: 'Metric tiles', desc: 'Xe trong bãi, khách dự kiến, ngoại lệ, chấm công...' },
      { type: 'button', name: 'Mở giám sát trực tiếp', desc: 'Chuyển đến trang Monitoring' },
      { type: 'button', name: 'Tra cứu vào/ra', desc: 'Chuyển đến Access Logs' },
      { type: 'button', name: 'Xem khách hẹn trước', desc: 'Chuyển đến Pre-registrations' },
      { type: 'table', name: 'Hoạt động mới nhất', desc: 'Danh sách các hoạt động gần đây (actor, gate, status)' },
      { type: 'feature', name: 'Biểu đồ traffic', desc: 'Lưu lượng vào/ra theo ngày trong tuần dạng cột' },
      { type: 'feature', name: 'AI Intelligence', desc: 'Phân tích thông minh, insights, dự báo tuần tới' },
    ],
    '/monitoring': [
      { type: 'input', name: 'Camera URL', desc: 'Nhập URL stream camera để xem trực tiếp' },
      { type: 'button', name: 'Bật/Tắt camera', desc: 'Bật hoặc tắt preview camera' },
      { type: 'feature', name: 'Camera grid', desc: 'Hiển thị tối đa 4 camera dạng lưới 2x2' },
      { type: 'select', name: 'Chọn camera', desc: 'Chọn camera từ danh sách đã cấu hình' },
      { type: 'button', name: 'Cài đặt', desc: 'Mở panel cấu hình camera' },
    ],
    '/access-logs': [
      { type: 'input', name: 'Từ khóa', desc: 'Tìm theo tên, biển số, ghi chú' },
      { type: 'select', name: 'Chiều di chuyển', desc: 'Lọc theo chiều Vào/Ra' },
      { type: 'select', name: 'Cổng', desc: 'Chọn cổng cụ thể' },
      { type: 'select', name: 'Trạng thái', desc: 'Lọc theo trạng thái (APPROVED, DENIED...)' },
      { type: 'input', name: 'Từ ngày - Đến ngày', desc: 'Khoảng thời gian cần tra cứu' },
      { type: 'button', name: 'Áp dụng lọc', desc: 'Kích hoạt bộ lọc và tải lại dữ liệu' },
      { type: 'button', name: 'Đặt lại', desc: 'Xóa tất cả bộ lọc về mặc định' },
      { type: 'table', name: 'Bảng lịch sử', desc: 'Hiển thị: Thời gian, Đối tượng, Chiều, Cổng, Biển số, Phương thức, Trạng thái' },
    ],
    '/employees': [
      { type: 'button', name: 'Thêm nhân viên', desc: 'Mở modal tạo mới nhân sự' },
      { type: 'button', name: 'Import', desc: 'Import danh sách nhân viên từ file Excel/CSV' },
      { type: 'button', name: 'Export', desc: 'Xuất danh sách nhân viên ra file' },
      { type: 'input', name: 'Tìm kiếm', desc: 'Tìm theo tên, SĐT, Email' },
      { type: 'select', name: 'Lọc trạng thái', desc: 'Tất cả / Đang hoạt động / Ngừng hoạt động' },
      { type: 'table', name: 'Danh sách nhân viên', desc: 'Avatar, Họ tên, Liên hệ, Phòng ban, Chức vụ, Trạng thái, Hành động' },
      { type: 'button', name: 'Sửa / Xóa', desc: 'Icon button để sửa hoặc xóa nhân viên' },
    ],
    '/vehicles': [
      { type: 'button', name: 'Đăng ký phương tiện', desc: 'Mở modal đăng ký xe mới' },
      { type: 'input', name: 'Tìm biển số', desc: 'Tìm theo biển số, chủ xe' },
      { type: 'select', name: 'Lọc loại xe', desc: 'Ô tô, xe máy, xe tải...' },
      { type: 'input', name: 'Biển số (form)', desc: 'Nhập biển số, tự động validate và nhận diện loại xe' },
      { type: 'select', name: 'Chủ sở hữu', desc: 'Combobox tìm kiếm nhân viên' },
    ],
    '/dynamic-qr-generator': [
      { type: 'input', name: 'Employee ID', desc: 'Nhập ID nhân sự cần cấp QR (Admin)' },
      { type: 'button', name: 'Phát QR realtime', desc: 'Tạo và tự động làm mới QR theo chu kỳ' },
      { type: 'button', name: 'Tạm dừng realtime', desc: 'Dừng tự động làm mới' },
      { type: 'button', name: 'Làm mới ngay', desc: 'Refresh QR thủ công' },
      { type: 'feature', name: 'QR display', desc: 'Mã QR lớn, rõ, kèm countdown thời gian sống' },
    ],
    '/face-id-security': [
      { type: 'input', name: 'Camera URL', desc: 'Nhập URL camera stream cho nhận diện khuôn mặt' },
      { type: 'button', name: 'Bật preview', desc: 'Mở preview camera' },
      { type: 'button', name: 'Khởi tạo phiên', desc: 'Bắt đầu phiên nhận diện khuôn mặt' },
      { type: 'button', name: 'Tắt camera', desc: 'Tắt camera và kết thúc phiên' },
      { type: 'feature', name: 'Kết quả nhận diện', desc: 'Employee ID, trạng thái, bounding box, confidence' },
    ],
    '/settings': [
      { type: 'tab', name: 'Cài đặt chung', desc: 'Tên công ty, giờ mở/đóng cổng, ngôn ngữ, múi giờ' },
      { type: 'tab', name: 'Mạng lưới Camera', desc: 'Thêm, sửa, xóa camera, quét camera LAN' },
      { type: 'tab', name: 'Hệ thống AI', desc: 'Bật Face ID, Anti-spoofing, LPR, ngưỡng nhận diện' },
      { type: 'tab', name: 'Cảnh báo tự động', desc: 'Cảnh báo người lạ, xe chưa đăng ký, camera offline' },
    ],
  }
  return details[path] || [
    { type: 'feature', name: 'Trang chức năng', desc: 'Vào trang để khám phá các tính năng chi tiết.' },
  ]
}

const faqs = [
  { q: 'Làm thế nào để đăng nhập vào V-Shield?', a: 'Truy cập đường dẫn /login. Nhập tên đăng nhập và mật khẩu được cấp bởi Admin. Nếu tài khoản yêu cầu MFA, nhập mã 6 số từ ứng dụng Authenticator.' },
  { q: 'Tôi quên mật khẩu, phải làm sao?', a: 'Liên hệ Admin hệ thống để được reset mật khẩu. Tính năng quên mật khẩu tự động đang được phát triển.' },
  { q: 'Làm sao để tạo QR động?', a: 'Admin: vào Tạo QR động, nhập Employee ID, bấm "Phát QR realtime". Nhân viên: đăng nhập tự động vào trang QR với QR của chính mình.' },
  { q: 'Sự khác nhau giữa các vai trò?', a: 'Admin: toàn quyền. Bảo vệ: giám sát, xác thực cổng, check-in. Quản lý: báo cáo, duyệt đơn. Nhân viên: QR, chấm công, đơn nghỉ.' },
  { q: 'Tại sao camera không hiển thị?', a: 'Kiểm tra: 1) Camera đã được cấu hình trong Settings, 2) URL stream đúng, 3) go2rtc đang chạy, 4) Trình duyệt không chặn mixed content.' },
  { q: 'Làm sao để thêm nhân viên mới?', a: 'Vào Hồ sơ nhân viên > bấm "Thêm nhân viên". Điền đầy đủ thông tin: Họ tên, SĐT, Email, Phòng ban, Chức vụ. Có thể upload ảnh khuôn mặt để train Face ID.' },
  { q: 'Làm sao để xử lý ngoại lệ?', a: 'Vào Xử lý ngoại lệ, xem danh sách các trường hợp bypass hoặc lỗi nhận diện, kiểm tra chi tiết và xác nhận xử lý.' },
  { q: 'Làm sao để check-in khách?', a: 'Vào Reception, tìm kiếm khách theo tên hoặc số điện thoại, chọn check-in. Hoặc dùng Kiosk Check-in để khách tự làm.' },
]

const roleMap = { all: 'all', admin: 'Admin', baove: 'BaoVe', quanly: 'QuanLy', staff: 'Staff' }
</script>

<style scoped>
.guide-page {
  min-height: 100vh;
}
.guide-hero {
  position: relative;
  padding: 48px 32px 40px;
  overflow: hidden;
  background: linear-gradient(180deg, rgba(16,32,51,0.97), rgba(24,49,77,0.94));
  color: #eefbfc;
}
.guide-hero-bg {
  position: absolute;
  inset: 0;
  pointer-events: none;
}
.hero-orb {
  position: absolute;
  border-radius: 999px;
  filter: blur(80px);
  opacity: 0.3;
}
.hero-orb.a { width: 400px; height: 400px; top: -100px; right: -60px; background: rgba(84,196,211,0.4); }
.hero-orb.b { width: 300px; height: 300px; bottom: -80px; left: 20%; background: rgba(216,155,55,0.2); }
.guide-hero-content {
  position: relative;
  z-index: 1;
  max-width: 900px;
  margin: 0 auto;
}
.guide-kicker {
  display: inline-flex;
  padding: 8px 16px;
  border-radius: 999px;
  background: rgba(84,196,211,0.12);
  color: #b8f7ff;
  font-size: 0.85rem;
  font-weight: 700;
  letter-spacing: 0.08em;
}
.guide-title {
  margin-top: 20px;
  font-family: var(--font-heading);
  font-size: clamp(2.2rem, 4vw, 3.4rem);
  font-weight: 700;
  line-height: 1.02;
  letter-spacing: -0.04em;
}
.guide-subtitle {
  margin-top: 14px;
  font-size: 1.05rem;
  line-height: 1.65;
  color: rgba(222,241,246,0.82);
  max-width: 64ch;
}
.guide-meta { margin-top: 20px; display: flex; flex-wrap: wrap; gap: 10px; }
.guide-meta-chip {
  padding: 7px 14px;
  border-radius: 999px;
  background: rgba(255,255,255,0.08);
  border: 1px solid rgba(255,255,255,0.1);
  color: rgba(222,241,246,0.78);
  font-size: 0.82rem;
  font-weight: 600;
}

.guide-layout {
  display: grid;
  grid-template-columns: 240px 1fr;
  gap: 0;
  min-height: calc(100vh - 200px);
}
.guide-sidebar {
  position: sticky;
  top: calc(var(--header-height) + 16px);
  align-self: start;
  padding: 24px 16px 24px 0;
}
.guide-sidebar-sticky {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.sidebar-search {
  position: relative;
}
.sidebar-search input {
  width: 100%;
  min-height: 42px;
  padding: 0 14px 0 38px;
  border-radius: 12px;
  border: 1px solid var(--border-color);
  background: var(--bg-input);
  color: var(--text-primary);
  font-size: 0.88rem;
}
.sidebar-search input:focus {
  border-color: rgba(15,124,130,0.36);
  box-shadow: 0 0 0 3px rgba(84,196,211,0.15);
}
.search-icon { position: absolute; left: 12px; top: 50%; transform: translateY(-50%); width: 16px; height: 16px; color: var(--text-muted); }
.sidebar-nav {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.sidebar-section-btn {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-radius: 12px;
  border: none;
  background: transparent;
  color: var(--text-secondary);
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  text-align: left;
  transition: all var(--transition-fast);
}
.sidebar-section-btn:hover { background: rgba(15,124,130,0.06); color: var(--text-primary); }
.sidebar-section-btn.active { background: rgba(15,124,130,0.1); color: var(--accent-primary); }
.section-icon { width: 20px; height: 20px; flex-shrink: 0; display: flex; align-items: center; justify-content: center; }
.section-icon :deep(svg) { width: 18px; height: 18px; }
.section-label { flex: 1; }
.section-count {
  padding: 2px 8px;
  border-radius: 999px;
  background: rgba(15,124,130,0.08);
  color: var(--accent-primary);
  font-size: 0.75rem;
  font-weight: 700;
}
.sidebar-roles {
  padding-top: 12px;
  border-top: 1px solid var(--border-color);
}
.roles-label { display: block; margin-bottom: 10px; color: var(--text-muted); font-size: 0.78rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.08em; }
.roles-chips { display: flex; flex-direction: column; gap: 6px; }
.role-chip {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  border-radius: 10px;
  border: 1px solid transparent;
  background: transparent;
  color: var(--text-secondary);
  font-size: 0.84rem;
  font-weight: 600;
  cursor: pointer;
  transition: all var(--transition-fast);
}
.role-chip:hover { background: var(--bg-input); }
.role-chip.active { border-color: var(--border-color-hover); background: rgba(15,124,130,0.06); color: var(--text-primary); }
.role-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }

.guide-main { padding: 32px 28px 48px; min-width: 0; }
.guide-section {
  animation: fadeIn 0.3s ease, slideUp 0.3s ease;
}
.section-header { margin-bottom: 28px; }
.section-header h2 { font-family: var(--font-heading); font-size: 1.8rem; color: var(--text-primary); margin-bottom: 8px; }
.section-header p { color: var(--text-secondary); font-size: 1rem; line-height: 1.6; max-width: 62ch; }

.feature-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; margin-bottom: 32px; }
.feature-card { padding: 22px; border-radius: 20px; border: 1px solid var(--border-color); background: var(--bg-card); transition: all var(--transition-normal); }
.feature-card:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); border-color: var(--border-color-hover); }
.feature-icon { width: 48px; height: 48px; border-radius: 14px; display: flex; align-items: center; justify-content: center; font-size: 1.4rem; margin-bottom: 12px; }
.feature-icon.blue { background: rgba(59,130,246,0.1); }
.feature-icon.green { background: rgba(16,185,129,0.1); }
.feature-icon.purple { background: rgba(139,92,246,0.1); }
.feature-icon.orange { background: rgba(245,158,11,0.1); }
.feature-icon.red { background: rgba(239,68,68,0.1); }
.feature-icon.teal { background: rgba(15,124,130,0.1); }
.feature-card h3 { font-size: 1.08rem; color: var(--text-primary); margin-bottom: 6px; }
.feature-card p { color: var(--text-secondary); font-size: 0.88rem; line-height: 1.55; }

.role-overview h3 { font-family: var(--font-heading); font-size: 1.3rem; margin-bottom: 16px; color: var(--text-primary); }
.role-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 14px; }
.role-card { padding: 18px; border-radius: 16px; border: 1px solid var(--border-color); border-left: 4px solid; background: var(--bg-card); }
.role-card-head { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
.role-card-head strong { font-size: 1rem; color: var(--text-primary); }
.role-card p { color: var(--text-secondary); font-size: 0.86rem; line-height: 1.5; }
.role-stats { margin-top: 12px; display: flex; gap: 12px; }
.role-stats span { font-size: 0.78rem; color: var(--text-muted); padding: 4px 10px; border-radius: 999px; background: var(--bg-input); }

.workflow-block { margin-bottom: 28px; padding: 24px; border-radius: 20px; border: 1px solid var(--border-color); background: var(--bg-card); }
.workflow-role-title { display: flex; align-items: center; gap: 10px; font-family: var(--font-heading); font-size: 1.15rem; margin-bottom: 18px; color: var(--text-primary); }
.workflow-steps { display: grid; gap: 14px; }
.wf-step { display: grid; grid-template-columns: 40px 1fr; gap: 14px; }
.wf-step-num { width: 40px; height: 40px; border-radius: 12px; background: linear-gradient(135deg, var(--teal-500), var(--steel-500)); color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 700; }
.wf-step-body h4 { font-size: 0.98rem; font-weight: 600; color: var(--text-primary); margin-bottom: 4px; }
.wf-step-body p { font-size: 0.88rem; color: var(--text-secondary); line-height: 1.5; }
.wf-step-body :deep(a) { color: var(--accent-primary); font-weight: 600; }
.wf-step-body :deep(a:hover) { text-decoration: underline; }

.page-group { margin-bottom: 28px; }
.page-group-title { font-family: var(--font-heading); font-size: 1.15rem; color: var(--text-primary); margin-bottom: 14px; padding-bottom: 10px; border-bottom: 1px solid var(--border-color); }
.page-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 12px; }
.page-card { display: flex; flex-direction: column; gap: 10px; padding: 16px; border-radius: 16px; border: 1px solid var(--border-color); background: var(--bg-card); text-decoration: none; transition: all var(--transition-normal); }
.page-card:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); border-color: var(--border-color-hover); }
.page-card.restricted { opacity: 0.5; }
.page-card-head { display: flex; align-items: flex-start; gap: 12px; }
.page-icon { width: 36px; height: 36px; flex-shrink: 0; display: flex; align-items: center; justify-content: center; color: var(--accent-primary); }
.page-icon :deep(svg) { width: 20px; height: 20px; }
.page-card-head div { min-width: 0; }
.page-card-head strong { display: block; font-size: 0.94rem; color: var(--text-primary); }
.page-card-head p { font-size: 0.82rem; color: var(--text-secondary); line-height: 1.45; margin-top: 4px; }
.page-card-meta { display: flex; align-items: center; justify-content: space-between; gap: 8px; flex-wrap: wrap; }
.page-badge { padding: 4px 10px; border-radius: 999px; background: rgba(15,124,130,0.08); color: var(--accent-primary); font-size: 0.72rem; font-weight: 700; }
.page-roles { display: flex; gap: 4px; flex-wrap: wrap; }
.mini-role-chip { padding: 2px 8px; border-radius: 999px; font-size: 0.68rem; font-weight: 700; background: rgba(15,124,130,0.08); color: var(--accent-primary); }
.mini-role-chip.admin { background: rgba(59,130,246,0.1); color: #3b82f6; }
.mini-role-chip.baove { background: rgba(16,185,129,0.1); color: #10b981; }
.mini-role-chip.quanly { background: rgba(139,92,246,0.1); color: #8b5cf6; }
.mini-role-chip.staff { background: rgba(245,158,11,0.1); color: #f59e0b; }

.filter-bar { display: flex; gap: 12px; margin-top: 16px; flex-wrap: wrap; }
.guide-select { min-height: 42px; padding: 0 36px 0 14px; border-radius: 12px; border: 1px solid var(--border-color); background: var(--bg-input); color: var(--text-primary); font-size: 0.88rem; appearance: none; background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='%236d8291'%3E%3Cpath d='M7 10l5 5 5-5z'/%3E%3C/svg%3E"); background-repeat: no-repeat; background-position: right 10px center; background-size: 18px; cursor: pointer; }

.feature-detail { padding: 22px; border-radius: 20px; border: 1px solid var(--border-color); background: var(--bg-card); }
.feature-detail-header { display: flex; align-items: flex-start; gap: 12px; margin-bottom: 20px; }
.feature-detail-icon { width: 40px; height: 40px; flex-shrink: 0; display: flex; align-items: center; justify-content: center; color: var(--accent-primary); }
.feature-detail-icon :deep(svg) { width: 22px; height: 22px; }
.feature-detail-header h3 { font-family: var(--font-heading); font-size: 1.2rem; color: var(--text-primary); }
.feature-detail-path { font-size: 0.82rem; color: var(--text-muted); font-family: monospace; margin-top: 4px; }
.feature-detail-body { display: grid; gap: 10px; }
.detail-item { display: flex; align-items: flex-start; gap: 12px; padding: 12px; border-radius: 12px; background: var(--bg-input); }
.detail-tag { flex-shrink: 0; padding: 4px 10px; border-radius: 8px; font-size: 0.72rem; font-weight: 700; text-transform: uppercase; }
.detail-tag.button { background: rgba(59,130,246,0.12); color: #3b82f6; }
.detail-tag.input { background: rgba(16,185,129,0.12); color: #10b981; }
.detail-tag.table { background: rgba(139,92,246,0.12); color: #8b5cf6; }
.detail-tag.select { background: rgba(245,158,11,0.12); color: #f59e0b; }
.detail-tag.feature { background: rgba(15,124,130,0.12); color: var(--accent-primary); }
.detail-content strong { display: block; font-size: 0.9rem; color: var(--text-primary); margin-bottom: 4px; }
.detail-content p { font-size: 0.84rem; color: var(--text-secondary); line-height: 1.45; }
.feature-empty { padding: 40px; text-align: center; color: var(--text-muted); background: var(--bg-card); border-radius: 16px; border: 1px dashed var(--border-color); }

.faq-list { display: grid; gap: 10px; }
.faq-item { border-radius: 16px; border: 1px solid var(--border-color); background: var(--bg-card); overflow: hidden; }
.faq-question { width: 100%; display: flex; justify-content: space-between; align-items: center; gap: 12px; padding: 16px 20px; border: none; background: transparent; color: var(--text-primary); font-size: 0.96rem; font-weight: 600; cursor: pointer; text-align: left; }
.faq-question:hover { background: rgba(15,124,130,0.04); }
.faq-question svg { width: 18px; height: 18px; flex-shrink: 0; color: var(--text-muted); transition: transform 0.2s ease; }
.faq-question svg.rotated { transform: rotate(180deg); }
.faq-answer { padding: 0 20px 16px; }
.faq-answer p { color: var(--text-secondary); font-size: 0.9rem; line-height: 1.6; margin-bottom: 6px; }

.guide-bottom-nav { margin-top: 40px; padding-top: 20px; border-top: 1px solid var(--border-color); display: flex; gap: 10px; justify-content: center; color: var(--text-muted); font-size: 0.84rem; }

@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
@keyframes slideUp { from { opacity: 0; transform: translateY(12px); } to { opacity: 1; transform: translateY(0); } }

@media (max-width: 1024px) {
  .guide-layout { grid-template-columns: 1fr; }
  .guide-sidebar { position: static; padding: 20px 16px; }
  .sidebar-roles .roles-chips { flex-direction: row; flex-wrap: wrap; }
}
@media (max-width: 768px) {
  .guide-hero { padding: 32px 20px 28px; }
  .guide-main { padding: 20px 16px 32px; }
  .page-grid { grid-template-columns: 1fr; }
  .feature-grid { grid-template-columns: 1fr; }
}
</style>
