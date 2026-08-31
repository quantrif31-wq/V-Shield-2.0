<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { authState } from '../stores/auth'
import { portalApi } from '../services/portalApi'
import PortalParticlesCanvas from '../components/portal/PortalParticlesCanvas.vue'
import PortalAudioToggle from '../components/portal/PortalAudioToggle.vue'
import PortalAuthModal from '../components/portal/PortalAuthModal.vue'

const router = useRouter()

// ── State ──
const overview = ref({
  systemName: 'V-SHIELD 2.0',
  tagline: 'Hệ thống kiểm soát an ninh thông minh đa nền tảng & AI Realtime',
  version: '2.0.0',
  releaseDate: '2026-08-31',
  averageRating: 4.95,
  totalReviews: 1280,
  totalComments: 3450,
  serverStatus: 'Online',
  apkDownloadUrl: 'https://v-shield.site/downloads/VShield-Mobile-Latest.apk',
  apkSizeBytes: 61059982
})

const reviews = ref([])
const comments = ref([])
const showAuthModal = ref(false)
const communityUser = ref(null)

// ── Form States ──
const newReview = reactive({
  authorName: '',
  rating: 5,
  content: '',
  platform: 'Web'
})
const reviewSubmitting = ref(false)
const reviewToast = ref('')

const newComment = reactive({
  authorName: '',
  content: ''
})
const commentSubmitting = ref(false)
const commentToast = ref('')

const feedbackForm = reactive({
  fullName: '',
  email: '',
  category: 'Feature',
  message: ''
})
const feedbackSubmitting = ref(false)
const feedbackToast = ref('')

const newsletterEmail = ref('')
const newsletterSubmitting = ref(false)
const newsletterToast = ref('')

// ── Mascot Dialogues ──
const mascotQuotes = [
  'Xin chào Operator! Hệ thống phòng thủ V-Shield 2.0 đã trực tuyến.',
  'Đồng bộ dữ liệu Hybrid Sync đang hoạt động với độ trễ dưới 30ms!',
  'Mô hình AI Face ID đã sẵn sàng quét nhận diện sinh trắc học.',
  'Cổng liên lạc Video Call VoIP được mã hóa end-to-end sẵn sàng kết nối.'
]
const currentQuoteIndex = ref(0)

function nextQuote() {
  currentQuoteIndex.value = (currentQuoteIndex.value + 1) % mascotQuotes.length
}

// ── Lifecycle & Data Loading ──
onMounted(async () => {
  // Load community user from localStorage
  const savedUser = localStorage.getItem('vshield_community_user')
  if (savedUser) {
    try {
      communityUser.value = JSON.parse(savedUser)
      newReview.authorName = communityUser.value.fullName
      newComment.authorName = communityUser.value.fullName
      feedbackForm.fullName = communityUser.value.fullName
      feedbackForm.email = communityUser.value.email
    } catch {}
  }

  // Load API data
  try {
    const [ov, revs, cmts] = await Promise.all([
      portalApi.getOverview(),
      portalApi.getReviews(),
      portalApi.getComments()
    ])
    if (ov) overview.value = { ...overview.value, ...ov }
    if (revs) reviews.value = revs
    if (cmts) comments.value = cmts
  } catch (err) {
    console.error('Failed to load portal data:', err)
  }

  // Mascot quote rotation
  setInterval(nextQuote, 8000)
})

// ── Methods ──
function triggerSfx() {
  window.dispatchEvent(new CustomEvent('portal-click-sfx'))
}

function handleLoginSuccess(user) {
  communityUser.value = user
  newReview.authorName = user.fullName
  newComment.authorName = user.fullName
  feedbackForm.fullName = user.fullName
  feedbackForm.email = user.email
}

function handleLogout() {
  communityUser.value = null
}

async function submitReview() {
  if (!newReview.authorName.trim() || !newReview.content.trim()) {
    reviewToast.value = 'Vui lòng nhập họ tên và nội dung đánh giá!'
    return
  }
  triggerSfx()
  reviewSubmitting.value = true
  try {
    const res = await portalApi.createReview(newReview)
    if (res.success && res.data) {
      reviews.value.unshift(res.data)
      newReview.content = ''
      reviewToast.value = '✨ Cảm ơn bạn! Đánh giá đã được đăng thành công.'
      setTimeout(() => { reviewToast.value = '' }, 4000)
    }
  } catch {
    reviewToast.value = 'Không thể gửi đánh giá, vui lòng thử lại.'
  } finally {
    reviewSubmitting.value = false
  }
}

async function submitComment() {
  if (!newComment.authorName.trim() || !newComment.content.trim()) {
    commentToast.value = 'Vui lòng nhập tên và nội dung bình luận!'
    return
  }
  triggerSfx()
  commentSubmitting.value = true
  try {
    const res = await portalApi.createComment(newComment)
    if (res.success && res.data) {
      comments.value.unshift(res.data)
      newComment.content = ''
      commentToast.value = '💬 Bình luận của bạn đã được xuất bản!'
      setTimeout(() => { commentToast.value = '' }, 4000)
    }
  } catch {
    commentToast.value = 'Không thể đăng bình luận, vui lòng thử lại.'
  } finally {
    commentSubmitting.value = false
  }
}

async function reactComment(comment) {
  triggerSfx()
  comment.likesCount++
  await portalApi.reactComment(comment.id, 'like')
}

async function submitFeedback() {
  if (!feedbackForm.fullName.trim() || !feedbackForm.email.trim() || !feedbackForm.message.trim()) {
    feedbackToast.value = 'Vui lòng điền đầy đủ các trường thông tin!'
    return
  }
  triggerSfx()
  feedbackSubmitting.value = true
  try {
    const res = await portalApi.submitFeedback(feedbackForm)
    if (res.success) {
      feedbackToast.value = '🌟 Góp ý của bạn đã được chuyển tới nhóm phát triển!'
      feedbackForm.message = ''
      setTimeout(() => { feedbackToast.value = '' }, 5000)
    }
  } catch {
    feedbackToast.value = 'Lỗi kết nối khi gửi góp ý.'
  } finally {
    feedbackSubmitting.value = false
  }
}

async function submitNewsletter() {
  if (!newsletterEmail.value.trim() || !newsletterEmail.value.includes('@')) {
    newsletterToast.value = 'Vui lòng nhập địa chỉ email hợp lệ!'
    return
  }
  triggerSfx()
  newsletterSubmitting.value = true
  try {
    const res = await portalApi.subscribeNewsletter({ email: newsletterEmail.value })
    if (res.success) {
      newsletterToast.value = '✉️ Đăng ký nhận bản tin thành công!'
      newsletterEmail.value = ''
      setTimeout(() => { newsletterToast.value = '' }, 4000)
    }
  } catch {
    newsletterToast.value = 'Không thể đăng ký, vui lòng thử lại sau.'
  } finally {
    newsletterSubmitting.value = false
  }
}

function scrollToSection(id) {
  triggerSfx()
  const el = document.getElementById(id)
  if (el) {
    el.scrollIntoView({ behavior: 'smooth' })
  }
}

function downloadApk() {
  triggerSfx()
  window.open(overview.value.apkDownloadUrl, '_blank')
}

// ── Static Lore & Data ──
const featuresList = [
  {
    icon: '👁️',
    title: 'AI Face ID & Multi-Angle',
    tag: 'Edge AI 60 FPS',
    description: 'Nhận diện khuôn mặt thời gian thực kết hợp nhiều góc camera, đối soát sinh trắc học chống giả mạo Deepfake.',
    color: 'from-cyan-500/20 to-blue-600/20',
    border: 'border-cyan-500/40',
    glow: 'rgba(0,240,255,0.3)'
  },
  {
    icon: '🚗',
    title: 'Barie & Đọc Biển Số Tự Động',
    tag: 'ANPR Precision',
    description: 'Tự động đọc biển số xe tốc độ cao, kết hợp khoá chéo IN-OUT và điều khiển barie thông minh không điểm mù.',
    color: 'from-amber-500/20 to-orange-600/20',
    border: 'border-amber-500/40',
    glow: 'rgba(245,158,11,0.3)'
  },
  {
    icon: '📲',
    title: 'Dynamic QR Anti-Fraud',
    tag: 'TOTP 5s Cycle',
    description: 'Mã QR động làm mới liên tục mỗi vài giây, mã hoá bất đối xứng giúp loại bỏ hoàn toàn hành vi chụp màn hình chia sẻ mã.',
    color: 'from-emerald-500/20 to-teal-600/20',
    border: 'border-emerald-500/40',
    glow: 'rgba(16,185,129,0.3)'
  },
  {
    icon: '📞',
    title: 'VoIP & Video Call Thời Gian Thực',
    tag: 'WebRTC P2P',
    description: 'Đàm thoại âm thanh & video độ phân giải cao giữa nhân viên di động và trung tâm chỉ huy an ninh.',
    color: 'from-pink-500/20 to-rose-600/20',
    border: 'border-pink-500/40',
    glow: 'rgba(244,63,94,0.3)'
  },
  {
    icon: '🌐',
    title: 'Edge-Cloud Hybrid Sync',
    tag: 'Zero Latency',
    description: 'Cơ chế đồng bộ dữ liệu đa chiều thông minh, duy trì hoạt động độc lập kể cả khi mất kết nối mạng và tự phục hồi không nghẽn.',
    color: 'from-indigo-500/20 to-purple-600/20',
    border: 'border-indigo-500/40',
    glow: 'rgba(99,102,241,0.3)'
  },
  {
    icon: '🛡️',
    title: 'UEBA & Tình Báo Đe Dọa',
    tag: 'AI Anomaly',
    description: 'Phát hiện hành vi bất thường, cảnh báo xâm nhập sớm và tự động kích hoạt kịch bản phong toả an ninh tức thì.',
    color: 'from-purple-500/20 to-violet-600/20',
    border: 'border-purple-500/40',
    glow: 'rgba(168,85,247,0.3)'
  }
]

const roadmapMilestones = [
  {
    version: 'v1.0 (Khởi Nguyên)',
    time: 'Giai đoạn 1',
    status: 'Đã hoàn thành',
    badge: 'Cơ bản',
    title: 'Kiểm soát ra vào truyền thống & CSDL tập trung',
    desc: 'Thiết lập mô hình thẻ từ, ghi nhận nhật ký ra vào, giao diện bảng điều khiển quản lý nhân viên và phương tiện.'
  },
  {
    version: 'v1.5 (Tự Động Hóa)',
    time: 'Giai đoạn 2',
    status: 'Đã hoàn thành',
    badge: 'Nâng cấp',
    title: 'Tích hợp Barie tự động, QR Code tĩnh & Camera IP',
    desc: 'Kết nối thiết bị ngoại vi qua giao thức RTSP/ONVIF, điều khiển rào chắn tự động và chuẩn hóa luồng xử lý ngoại lệ.'
  },
  {
    version: 'v2.0 (Neural Realtime Defense)',
    time: 'Hiện tại (2026.08)',
    status: 'Phát hành chính thức',
    badge: 'Kỷ nguyên Mới',
    title: 'Kiến trúc AI Edge-Cloud, VoIP Call, Realtime Sync & 100% Docker',
    desc: 'Đột phá với nhận diện khuôn mặt YOLO/InsightFace, Video Call nội bộ, kháng nghẽn tải lớn và chuẩn hóa Docker Container toàn diện.'
  },
  {
    version: 'v2.5 (Tương Lai)',
    time: 'Lộ trình tiếp theo',
    status: 'Đang phát triển',
    badge: 'Tầm nhìn',
    title: 'Trợ lý AI Agent LLM, Bản đồ số 3D Digital Twin & Edge Chip TPU',
    desc: 'Tích hợp mô hình ngôn ngữ lớn phân tích tình huống tự động, bản đồ không gian 3D tương tác và chip tăng tốc AI biên.'
  }
]

const teamMembers = [
  {
    name: 'ThS. Phan Hoàng Khải',
    role: 'Giáo viên hướng dẫn',
    avatar: '/images/team/thaykhai.jpg',
    specialty: 'Cố vấn chuyên môn & Định hướng kiến trúc hệ thống',
    badge: 'Mentor'
  },
  {
    name: 'Phạm Văn Thành',
    role: 'Lead Developer / Core AI',
    avatar: '/images/team/thanh.jpg',
    specialty: 'Thiết kế kiến trúc hệ thống, tích hợp AI & Realtime Sync',
    badge: 'Core Lead'
  },
  {
    name: 'Hà Mạnh Hùng',
    role: 'Back-end API Architect',
    avatar: '/images/team/hung.jpg',
    specialty: 'Xây dựng RESTful API .NET, VoIP Call & CSDL tối ưu',
    badge: 'Backend'
  },
  {
    name: 'Phạm Ngọc Hoài Anh',
    role: 'Project Leader / DevOps',
    avatar: '/images/team/anh.jpg',
    specialty: 'Điều phối dự án, Docker hóa hệ thống & Triển khai VPS',
    badge: 'Leader'
  },
  {
    name: 'Vũ Tiến Đạt',
    role: 'Front-end Specialist',
    avatar: '/images/team/dat.jpg',
    specialty: 'Giao diện Cyber UI/UX Vue 3 & Trải nghiệm người dùng',
    badge: 'Frontend'
  },
  {
    name: 'Nguyễn Quốc Việt',
    role: 'Database & Security Admin',
    avatar: '/images/team/viet.jpg',
    specialty: 'Tối ưu hoá CSDL SQL Server & Chính sách an ninh RBAC',
    badge: 'Database'
  }
]
</script>

<template>
  <div class="relative min-h-screen bg-[#060913] text-slate-100 font-sans selection:bg-cyan-500 selection:text-slate-950 overflow-x-hidden">
    <!-- Dynamic Cyber Particles Canvas Background -->
    <PortalParticlesCanvas />

    <!-- Ambient Gradient Backdrops -->
    <div class="pointer-events-none fixed inset-0 z-0">
      <div class="absolute top-[-10%] left-[-10%] h-[500px] w-[500px] rounded-full bg-cyan-600/15 blur-[120px]"></div>
      <div class="absolute top-[30%] right-[-10%] h-[600px] w-[600px] rounded-full bg-pink-600/15 blur-[140px]"></div>
      <div class="absolute bottom-[-10%] left-[20%] h-[500px] w-[500px] rounded-full bg-indigo-600/15 blur-[130px]"></div>
    </div>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- NAVIGATION BAR                                           -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <header class="sticky top-0 z-40 w-full border-b border-cyan-500/20 bg-slate-950/75 backdrop-blur-xl transition-all duration-300">
      <div class="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6 lg:px-8">
        <!-- Logo & Status -->
        <div class="flex items-center gap-3 cursor-pointer" @click="scrollToSection('hero')">
          <div class="relative flex h-10 w-10 items-center justify-center rounded-xl border border-cyan-400/60 bg-gradient-to-br from-cyan-500/20 to-blue-600/30 shadow-[0_0_15px_rgba(0,240,255,0.4)]">
            <span class="text-xl">🛡️</span>
            <span class="absolute -bottom-1 -right-1 flex h-3 w-3">
              <span class="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75"></span>
              <span class="relative inline-flex h-3 w-3 rounded-full bg-emerald-500 border border-slate-950"></span>
            </span>
          </div>
          <div>
            <div class="flex items-center gap-2">
              <span class="text-lg font-black tracking-wider text-transparent bg-clip-text bg-gradient-to-r from-cyan-300 via-white to-pink-400">
                V-SHIELD 2.0
              </span>
              <span class="rounded bg-cyan-500/20 px-1.5 py-0.2 text-[10px] font-extrabold text-cyan-300 border border-cyan-400/30">
                NEURAL
              </span>
            </div>
            <p class="text-[10px] text-slate-400 tracking-tight">Access Control & Defense Platform</p>
          </div>
        </div>

        <!-- Desktop Nav Links -->
        <nav class="hidden lg:flex items-center gap-6 text-xs font-semibold tracking-wide text-slate-300">
          <button type="button" @click="scrollToSection('features')" class="hover:text-cyan-300 transition-colors">TÍNH NĂNG</button>
          <button type="button" @click="scrollToSection('roadmap')" class="hover:text-cyan-300 transition-colors">LỊCH SỬ & LỘ TRÌNH</button>
          <button type="button" @click="scrollToSection('download')" class="hover:text-cyan-300 transition-colors">TẢI APK</button>
          <button type="button" @click="scrollToSection('reviews')" class="hover:text-cyan-300 transition-colors">ĐÁNH GIÁ</button>
          <button type="button" @click="scrollToSection('feedback')" class="hover:text-cyan-300 transition-colors">BÌNH LUẬN & GÓP Ý</button>
          <button type="button" @click="scrollToSection('team')" class="hover:text-cyan-300 transition-colors">ĐỘI NGŨ</button>
        </nav>

        <!-- Action Items: Audio + Community Auth + Admin Access -->
        <div class="flex items-center gap-3">
          <!-- Audio Synthesizer Toggle -->
          <PortalAudioToggle />

          <!-- Community User Profile Button -->
          <button
            type="button"
            @click="showAuthModal = true; triggerSfx()"
            class="flex items-center gap-2 rounded-full border border-slate-700 bg-slate-900/80 px-3 py-1.5 text-xs font-semibold text-slate-200 hover:border-cyan-400 hover:text-cyan-300 transition-all shadow-sm"
          >
            <img
              v-if="communityUser"
              :src="communityUser.avatarUrl"
              :alt="communityUser.fullName"
              class="h-5 w-5 rounded-full border border-cyan-400 object-cover"
            />
            <span v-else class="text-sm">🌐</span>
            <span class="max-w-[100px] truncate hidden sm:inline">
              {{ communityUser ? communityUser.fullName : 'Cộng Đồng' }}
            </span>
          </button>

          <!-- Main Launch System Button -->
          <router-link
            :to="authState.user ? '/dashboard' : '/login'"
            @click="triggerSfx"
            class="group relative inline-flex items-center gap-2 overflow-hidden rounded-full border border-cyan-400/80 bg-gradient-to-r from-cyan-500 via-blue-600 to-indigo-600 px-4 py-1.5 text-xs font-bold text-slate-950 shadow-[0_0_20px_rgba(0,240,255,0.4)] transition-all hover:scale-105 hover:shadow-[0_0_30px_rgba(0,240,255,0.7)]"
          >
            <span class="relative z-10 text-white font-extrabold flex items-center gap-1.5">
              <span>⚡</span>
              <span>{{ authState.user ? 'Vào Dashboard' : 'Đăng Nhập Quản Trị' }}</span>
            </span>
          </router-link>
        </div>
      </div>
    </header>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- HERO SHOWCASE                                            -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <section id="hero" class="relative z-10 pt-16 pb-20 lg:pt-24 lg:pb-32 px-4 sm:px-6 lg:px-8">
      <div class="mx-auto max-w-7xl">
        <div class="grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
          
          <!-- Hero Text Content -->
          <div class="lg:col-span-7 space-y-6 text-center lg:text-left">
            <!-- Glowing Tagline Badge -->
            <div class="inline-flex items-center gap-2.5 rounded-full border border-cyan-400/40 bg-cyan-950/40 px-4 py-1.5 text-xs font-semibold text-cyan-300 shadow-[0_0_15px_rgba(0,240,255,0.2)] backdrop-blur-md">
              <span class="h-2 w-2 rounded-full bg-cyan-400 animate-pulse"></span>
              <span class="tracking-widest uppercase text-[11px]">KỶ NGUYÊN AN NINH SỐ ĐA NỀN TẢNG</span>
            </div>

            <!-- Massive Epic Headline -->
            <h1 class="text-4xl sm:text-5xl lg:text-6xl font-black tracking-tight leading-[1.15]">
              <span class="block text-white">HỆ THỐNG AN NINH</span>
              <span class="block text-transparent bg-clip-text bg-gradient-to-r from-cyan-300 via-pink-400 to-amber-300 drop-shadow-[0_0_25px_rgba(0,240,255,0.4)]">
                THỜI GIAN THỰC & AI
              </span>
            </h1>

            <p class="text-base sm:text-lg text-slate-300 max-w-2xl mx-auto lg:mx-0 font-normal leading-relaxed">
              Giải pháp kiểm soát ra vào toàn diện kết hợp nhận diện khuôn mặt <strong class="text-cyan-300">Edge AI 60 FPS</strong>, Barie thông minh, QR Code động chống gian lận, <strong class="text-pink-300">VoIP Video Call</strong> mã hóa và đồng bộ đám mây đa chiều chuẩn hóa 100% Docker.
            </p>

            <!-- Dual Action CTAs -->
            <div class="flex flex-wrap items-center justify-center lg:justify-start gap-4 pt-2">
              <router-link
                :to="authState.user ? '/dashboard' : '/login'"
                @click="triggerSfx"
                class="group relative inline-flex items-center gap-3 rounded-2xl border-2 border-cyan-400 bg-gradient-to-r from-cyan-400 via-teal-400 to-cyan-500 px-6 py-3.5 text-sm font-extrabold text-slate-950 shadow-[0_0_30px_rgba(0,240,255,0.5)] transition-all hover:scale-105 hover:shadow-[0_0_45px_rgba(0,240,255,0.8)]"
              >
                <span>🚀</span>
                <span>TRUY CẬP HỆ THỐNG QUẢN TRỊ</span>
              </router-link>

              <button
                type="button"
                @click="scrollToSection('download')"
                class="inline-flex items-center gap-3 rounded-2xl border border-slate-700 bg-slate-900/90 px-6 py-3.5 text-sm font-bold text-slate-200 shadow-lg backdrop-blur-md transition-all hover:border-pink-500 hover:text-pink-300 hover:shadow-[0_0_25px_rgba(244,63,94,0.3)]"
              >
                <span>📲</span>
                <span>TẢI MOBILE APK (v2.0)</span>
              </button>
            </div>

            <!-- Mascot Anime Quote Interactive Card -->
            <div
              @click="nextQuote(); triggerSfx()"
              class="group relative mt-6 cursor-pointer overflow-hidden rounded-2xl border border-cyan-500/30 bg-slate-900/60 p-4 backdrop-blur-md transition-all hover:border-cyan-400/60 hover:bg-slate-900/80 shadow-inner"
            >
              <div class="flex items-center gap-3.5">
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-cyan-500/30 to-pink-500/30 text-lg border border-cyan-400/40">
                  🤖
                </div>
                <div class="text-left">
                  <div class="flex items-center gap-2">
                    <span class="text-xs font-bold text-cyan-300">V-Shield AI Assistant</span>
                    <span class="text-[10px] text-slate-500">(Bấm để đổi câu thoại)</span>
                  </div>
                  <p class="text-xs text-slate-300 mt-0.5 italic">
                    "{{ mascotQuotes[currentQuoteIndex] }}"
                  </p>
                </div>
              </div>
            </div>

          </div>

          <!-- Hero Graphic / Interactive Holographic Terminal Card -->
          <div class="lg:col-span-5 relative flex justify-center">
            <div class="relative w-full max-w-md overflow-hidden rounded-3xl border border-cyan-400/40 bg-gradient-to-b from-slate-900/90 via-slate-950/95 to-slate-900/90 p-6 shadow-[0_0_50px_rgba(0,240,255,0.25)] backdrop-blur-xl">
              <!-- Holographic Corner Accents -->
              <div class="absolute top-0 right-0 h-16 w-16 bg-gradient-to-bl from-pink-500/30 to-transparent"></div>
              <div class="absolute bottom-0 left-0 h-16 w-16 bg-gradient-to-tr from-cyan-500/30 to-transparent"></div>

              <!-- Terminal Header -->
              <div class="flex items-center justify-between border-b border-slate-800 pb-4">
                <div class="flex items-center gap-2">
                  <span class="h-3 w-3 rounded-full bg-rose-500"></span>
                  <span class="h-3 w-3 rounded-full bg-amber-500"></span>
                  <span class="h-3 w-3 rounded-full bg-emerald-500"></span>
                  <span class="text-xs font-mono font-semibold text-slate-400 ml-2">TERMINAL // LIVE_METRICS</span>
                </div>
                <span class="rounded bg-emerald-500/20 px-2 py-0.5 text-[10px] font-bold text-emerald-400 border border-emerald-500/30">
                  SYSTEM READY
                </span>
              </div>

              <!-- Metrics Grid -->
              <div class="grid grid-cols-2 gap-4 py-5 font-mono">
                <div class="rounded-2xl border border-cyan-500/20 bg-slate-900/80 p-3.5">
                  <p class="text-[11px] text-slate-400 uppercase tracking-wider">Face ID Accuracy</p>
                  <p class="text-2xl font-black text-cyan-300 mt-1">99.98%</p>
                  <span class="text-[10px] text-cyan-500/80">Anti-Spoofing Active</span>
                </div>

                <div class="rounded-2xl border border-pink-500/20 bg-slate-900/80 p-3.5">
                  <p class="text-[11px] text-slate-400 uppercase tracking-wider">Sync Latency</p>
                  <p class="text-2xl font-black text-pink-300 mt-1">&lt; 30ms</p>
                  <span class="text-[10px] text-pink-500/80">Hybrid Realtime Relay</span>
                </div>

                <div class="rounded-2xl border border-amber-500/20 bg-slate-900/80 p-3.5">
                  <p class="text-[11px] text-slate-400 uppercase tracking-wider">Active Platforms</p>
                  <p class="text-2xl font-black text-amber-300 mt-1">3 / 3</p>
                  <span class="text-[10px] text-amber-500/80">Cloud • Docker • APK</span>
                </div>

                <div class="rounded-2xl border border-emerald-500/20 bg-slate-900/80 p-3.5">
                  <p class="text-[11px] text-slate-400 uppercase tracking-wider">Community Score</p>
                  <p class="text-2xl font-black text-emerald-300 mt-1">{{ overview.averageRating }} ★</p>
                  <span class="text-[10px] text-emerald-500/80">{{ overview.totalReviews }}+ Đánh giá</span>
                </div>
              </div>

              <!-- Live Stream Preview Simulation Graphic -->
              <div class="relative overflow-hidden rounded-2xl border border-cyan-500/30 bg-slate-950 p-3">
                <div class="flex items-center justify-between text-xs text-slate-400 font-mono mb-2">
                  <span class="flex items-center gap-1.5 text-cyan-400 font-bold">
                    <span class="h-2 w-2 rounded-full bg-cyan-400 animate-ping"></span>
                    CAM_GATE_01 [AI_ANPR_ACTIVE]
                  </span>
                  <span class="text-[10px] text-slate-500">60 FPS • 1080p</span>
                </div>
                <div class="h-24 w-full rounded-xl bg-gradient-to-r from-slate-900 via-slate-800 to-slate-900 flex items-center justify-center border border-slate-800 relative">
                  <div class="text-center">
                    <span class="text-2xl">⚡</span>
                    <p class="text-[11px] font-mono text-cyan-300 mt-1 font-semibold">NEURAL STREAM PROTOCOL CONNECTED</p>
                  </div>
                  <!-- Scanning Laser Line -->
                  <div class="absolute inset-x-0 h-0.5 bg-gradient-to-r from-transparent via-cyan-400 to-transparent shadow-[0_0_8px_#00f0ff] animate-pulse"></div>
                </div>
              </div>

            </div>
          </div>

        </div>
      </div>
    </section>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- SECTION: HỆ THỐNG TÍNH NĂNG (FEATURE MATRIX)            -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <section id="features" class="relative z-10 py-20 bg-slate-950/60 border-t border-b border-cyan-500/10 px-4 sm:px-6 lg:px-8">
      <div class="mx-auto max-w-7xl">
        <div class="text-center max-w-3xl mx-auto mb-16 space-y-3">
          <span class="text-xs font-extrabold tracking-widest text-cyan-400 uppercase">HỆ THỐNG VẬN HÀNH 6 TRỤ CỘT</span>
          <h2 class="text-3xl sm:text-4xl font-black text-transparent bg-clip-text bg-gradient-to-r from-white via-cyan-200 to-pink-300">
            CÔNG NGHỆ BẢO MẬT & ĐIỀU HÀNH ĐỘT PHÁ
          </h2>
          <p class="text-sm text-slate-400">
            Tất cả các mô-đun an ninh được tích hợp trên một nền tảng đồng nhất, xử lý cục bộ tại biên kết hợp đồng bộ hóa đám mây tức thời.
          </p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div
            v-for="(feat, idx) in featuresList"
            :key="idx"
            class="group relative rounded-3xl border bg-gradient-to-b p-7 backdrop-blur-md transition-all duration-300 hover:-translate-y-1.5 hover:shadow-2xl"
            :class="[feat.border, feat.color]"
          >
            <div class="flex items-center justify-between mb-4">
              <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-900/90 text-2xl border border-slate-700/60 shadow-md">
                {{ feat.icon }}
              </div>
              <span class="rounded-full bg-slate-900/80 px-2.5 py-1 text-[10px] font-bold text-cyan-300 border border-cyan-500/30">
                {{ feat.tag }}
              </span>
            </div>

            <h3 class="text-lg font-bold text-white group-hover:text-cyan-200 transition-colors">
              {{ feat.title }}
            </h3>

            <p class="mt-2.5 text-xs text-slate-300 leading-relaxed">
              {{ feat.description }}
            </p>
          </div>
        </div>
      </div>
    </section>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- SECTION: LỊCH SỬ PHÁT TRIỂN & LỘ TRÌNH (ROADMAP)          -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <section id="roadmap" class="relative z-10 py-20 px-4 sm:px-6 lg:px-8">
      <div class="mx-auto max-w-7xl">
        <div class="text-center max-w-3xl mx-auto mb-16 space-y-3">
          <span class="text-xs font-extrabold tracking-widest text-pink-400 uppercase">TIẾN HÓA CÔNG NGHỆ</span>
          <h2 class="text-3xl sm:text-4xl font-black text-transparent bg-clip-text bg-gradient-to-r from-pink-300 via-white to-cyan-300">
            LỊCH SỬ PHÁT TRIỂN & LỘ TRÌNH V-SHIELD
          </h2>
          <p class="text-sm text-slate-400">
            Hành trình từ một đồ án nghiên cứu kiểm soát ra vào tới nền tảng an ninh thông minh đa nền tảng hiện đại.
          </p>
        </div>

        <div class="relative border-l-2 border-cyan-500/30 ml-4 md:ml-32 space-y-12">
          <div
            v-for="(item, idx) in roadmapMilestones"
            :key="idx"
            class="relative pl-8 md:pl-12 group"
          >
            <!-- Timeline Dot -->
            <div class="absolute -left-[9px] top-1.5 h-4 w-4 rounded-full border-2 border-slate-950 bg-cyan-400 shadow-[0_0_12px_#00f0ff] group-hover:scale-125 transition-transform"></div>

            <div class="rounded-3xl border border-slate-800 bg-slate-900/70 p-6 backdrop-blur-md transition-all hover:border-cyan-400/40 hover:bg-slate-900/90 shadow-lg">
              <div class="flex flex-wrap items-center justify-between gap-2 mb-2">
                <span class="text-xs font-mono font-bold text-cyan-300 tracking-wider">
                  {{ item.version }} • {{ item.time }}
                </span>
                <span
                  class="rounded-full px-2.5 py-0.5 text-[10px] font-bold"
                  :class="idx === 2 ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-400/40 animate-pulse' : 'bg-slate-800 text-slate-400'"
                >
                  {{ item.status }}
                </span>
              </div>

              <h3 class="text-base font-bold text-white group-hover:text-cyan-200">
                {{ item.title }}
              </h3>

              <p class="mt-2 text-xs text-slate-300 leading-relaxed">
                {{ item.desc }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- SECTION: DOWNLOAD HUB (TẢI APK MOBILE)                    -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <section id="download" class="relative z-10 py-20 bg-slate-950/80 border-t border-b border-cyan-500/10 px-4 sm:px-6 lg:px-8">
      <div class="mx-auto max-w-7xl">
        <div class="rounded-3xl border border-cyan-400/40 bg-gradient-to-r from-slate-900 via-slate-950 to-slate-900 p-8 sm:p-12 shadow-[0_0_50px_rgba(0,240,255,0.2)]">
          <div class="grid grid-cols-1 lg:grid-cols-12 gap-8 items-center">
            
            <div class="lg:col-span-8 space-y-4">
              <span class="rounded-full bg-pink-500/20 px-3 py-1 text-xs font-bold text-pink-300 border border-pink-500/30">
                MOBILE APPLICATION (ANDROID)
              </span>
              <h2 class="text-3xl sm:text-4xl font-black text-white">
                TRẢI NGHIỆM V-SHIELD TRÊN DI ĐỘNG
              </h2>
              <p class="text-sm text-slate-300 max-w-2xl leading-relaxed">
                Ứng dụng di động dành cho nhân viên và quản lý: nhận diện điểm danh qua QR Code động, gọi đàm thoại Video Call với phòng trực an ninh, gửi đơn nghỉ phép và nhận thông báo khẩn cấp tức thời.
              </p>

              <!-- Release Specs -->
              <div class="flex flex-wrap gap-4 pt-2 text-xs font-mono text-slate-400">
                <span class="flex items-center gap-1.5"><strong class="text-cyan-300">Phiên bản:</strong> v{{ overview.version }}</span>
                <span class="flex items-center gap-1.5"><strong class="text-cyan-300">Kích thước:</strong> ~58.2 MB</span>
                <span class="flex items-center gap-1.5"><strong class="text-cyan-300">Yêu cầu:</strong> Android 9.0+</span>
                <span class="flex items-center gap-1.5"><strong class="text-cyan-300">Cập nhật:</strong> {{ overview.releaseDate }}</span>
              </div>

              <!-- Download Buttons -->
              <div class="flex flex-wrap gap-4 pt-4">
                <button
                  type="button"
                  @click="downloadApk"
                  class="inline-flex items-center gap-3 rounded-2xl border border-cyan-400 bg-gradient-to-r from-cyan-400 to-teal-500 px-6 py-3.5 text-sm font-extrabold text-slate-950 shadow-[0_0_25px_rgba(0,240,255,0.4)] transition-all hover:scale-105"
                >
                  <span class="text-lg">📥</span>
                  <span>TẢI VỀ FILE APK TRỰC TIẾP</span>
                </button>

                <a
                  href="https://v-shield.site"
                  target="_blank"
                  class="inline-flex items-center gap-3 rounded-2xl border border-slate-700 bg-slate-900/90 px-6 py-3.5 text-sm font-bold text-slate-200 transition-all hover:border-cyan-400 hover:text-cyan-300"
                >
                  <span>🌐</span>
                  <span>TRUY CẬP BẢN WEB CLOUD</span>
                </a>
              </div>
            </div>

            <!-- APK QR Scan Box -->
            <div class="lg:col-span-4 flex flex-col items-center justify-center p-6 rounded-2xl border border-cyan-500/30 bg-slate-900/90 text-center">
              <div class="p-3 bg-white rounded-xl shadow-[0_0_20px_rgba(0,240,255,0.4)]">
                <!-- Clean QR Code Visual -->
                <img
                  :src="`https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${encodeURIComponent('https://v-shield.site/downloads/VShield-Mobile-Latest.apk')}`"
                  alt="Scan to download APK"
                  class="h-32 w-32 object-contain"
                />
              </div>
              <p class="mt-3 text-xs font-bold text-cyan-200">QUÉT QR ĐỂ TẢI TRÊN ĐIỆN THOẠI</p>
              <p class="text-[11px] text-slate-400 mt-0.5">Hỗ trợ mọi dòng máy Android</p>
            </div>

          </div>
        </div>
      </div>
    </section>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- SECTION: ĐÁNH GIÁ & XẾP HẠNG (REVIEWS & RATINGS)          -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <section id="reviews" class="relative z-10 py-20 px-4 sm:px-6 lg:px-8">
      <div class="mx-auto max-w-7xl">
        <div class="text-center max-w-3xl mx-auto mb-16 space-y-3">
          <span class="text-xs font-extrabold tracking-widest text-amber-400 uppercase">CỘNG ĐỒNG & CHUYÊN GIA</span>
          <h2 class="text-3xl sm:text-4xl font-black text-transparent bg-clip-text bg-gradient-to-r from-amber-300 via-white to-cyan-300">
            ĐÁNH GIÁ TỪ NGƯỜI DÙNG & DOANH NGHIỆP
          </h2>
          <div class="flex items-center justify-center gap-2 pt-1">
            <span class="text-2xl font-black text-amber-400">{{ overview.averageRating }}</span>
            <span class="text-amber-400 text-lg">★★★★★</span>
            <span class="text-xs text-slate-400">({{ overview.totalReviews }}+ lượt đánh giá)</span>
          </div>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-12 gap-8">
          
          <!-- Submit Review Box -->
          <div class="lg:col-span-5 rounded-3xl border border-amber-500/30 bg-slate-900/80 p-6 backdrop-blur-md">
            <h3 class="text-lg font-bold text-white mb-1 flex items-center gap-2">
              <span>⭐</span>
              <span>Gửi Đánh Giá Của Bạn</span>
            </h3>
            <p class="text-xs text-slate-400 mb-4">Chia sẻ cảm nhận và trải nghiệm sử dụng hệ thống V-Shield 2.0</p>

            <form @submit.prevent="submitReview" class="space-y-3.5">
              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1">Họ tên của bạn</label>
                <input
                  v-model="newReview.authorName"
                  type="text"
                  placeholder="VD: TS. Nguyễn Văn A, Kỹ sư..."
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2.5 text-sm text-slate-100 placeholder-slate-500 focus:border-amber-400 focus:outline-none"
                  required
                />
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1">Mức độ hài lòng</label>
                <div class="flex items-center gap-2">
                  <button
                    v-for="star in 5"
                    :key="star"
                    type="button"
                    @click="newReview.rating = star; triggerSfx()"
                    class="text-2xl transition-transform hover:scale-125 focus:outline-none"
                  >
                    <span :class="star <= newReview.rating ? 'text-amber-400' : 'text-slate-600'">★</span>
                  </button>
                  <span class="text-xs font-bold text-amber-300 ml-2 font-mono">{{ newReview.rating }} / 5 Sao</span>
                </div>
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1">Nền tảng đánh giá</label>
                <select
                  v-model="newReview.platform"
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2 text-sm text-slate-100 focus:border-amber-400 focus:outline-none"
                >
                  <option value="Web Cloud">Web Cloud (VPS)</option>
                  <option value="Docker Local">Docker Local</option>
                  <option value="Mobile Android">Mobile Android (APK)</option>
                </select>
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1">Nội dung nhận xét</label>
                <textarea
                  v-model="newReview.content"
                  rows="3"
                  placeholder="Nhập cảm nhận của bạn về hiệu năng, giao diện hoặc tính năng..."
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2 text-sm text-slate-100 placeholder-slate-500 focus:border-amber-400 focus:outline-none"
                  required
                ></textarea>
              </div>

              <div v-if="reviewToast" class="rounded-xl border border-amber-500/40 bg-amber-950/40 p-2.5 text-xs text-amber-300">
                {{ reviewToast }}
              </div>

              <button
                type="submit"
                :disabled="reviewSubmitting"
                class="w-full rounded-xl bg-gradient-to-r from-amber-400 via-orange-400 to-amber-500 py-2.5 text-sm font-bold text-slate-950 shadow-md hover:brightness-110 transition-all disabled:opacity-50"
              >
                {{ reviewSubmitting ? 'Đang gửi...' : 'GỬI ĐÁNH GIÁ' }}
              </button>
            </form>
          </div>

          <!-- Reviews Feed -->
          <div class="lg:col-span-7 space-y-4">
            <div
              v-for="rev in reviews"
              :key="rev.id"
              class="rounded-3xl border border-slate-800 bg-slate-900/60 p-5 backdrop-blur-md transition-all hover:border-amber-400/40 hover:bg-slate-900/80 shadow-md"
            >
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <img
                    :src="rev.avatarUrl || 'https://api.dicebear.com/7.x/adventurer/svg?seed=' + rev.authorName"
                    :alt="rev.authorName"
                    class="h-11 w-11 rounded-full border border-amber-400/50 object-cover"
                  />
                  <div>
                    <div class="flex items-center gap-2">
                      <h4 class="font-bold text-white text-sm">{{ rev.authorName }}</h4>
                      <span v-if="rev.isVerified" class="text-[10px] text-cyan-400 font-semibold">✓ Đã xác thực</span>
                    </div>
                    <p class="text-[11px] text-slate-400">{{ rev.authorRole || 'Thành viên' }} • Nền tảng: <span class="text-cyan-300">{{ rev.platform }}</span></p>
                  </div>
                </div>

                <!-- Stars -->
                <div class="text-amber-400 text-sm">
                  {{ '★'.repeat(rev.rating) }}{{ '☆'.repeat(5 - rev.rating) }}
                </div>
              </div>

              <p class="mt-3 text-xs text-slate-300 leading-relaxed">
                "{{ rev.content }}"
              </p>

              <div class="mt-3 flex items-center justify-between text-[11px] text-slate-500 pt-2 border-t border-slate-800/60">
                <span>{{ new Date(rev.createdAt).toLocaleDateString('vi-VN') }}</span>
                <span class="text-cyan-400/80 font-mono">♥ {{ rev.likesCount }} Hữu ích</span>
              </div>
            </div>
          </div>

        </div>
      </div>
    </section>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- SECTION: BÌNH LUẬN & GÓP Ý (FEEDBACK & COMMENTS)           -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <section id="feedback" class="relative z-10 py-20 bg-slate-950/70 border-t border-b border-cyan-500/10 px-4 sm:px-6 lg:px-8">
      <div class="mx-auto max-w-7xl">
        <div class="grid grid-cols-1 lg:grid-cols-12 gap-12">
          
          <!-- Direct Feedback Form -->
          <div class="lg:col-span-5 space-y-4">
            <span class="text-xs font-extrabold tracking-widest text-cyan-400 uppercase">HÒM THƯ GÓP Ý TRỰC TIẾP</span>
            <h2 class="text-2xl sm:text-3xl font-black text-white">GỬI GÓP Ý & BÁO LỖI</h2>
            <p class="text-xs text-slate-400 leading-relaxed">
              Mọi ý kiến đóng góp của bạn đều giúp hoàn thiện hệ thống V-Shield 2.0 ngày càng thông minh và mạnh mẽ hơn.
            </p>

            <form @submit.prevent="submitFeedback" class="rounded-3xl border border-cyan-500/30 bg-slate-900/80 p-6 space-y-3.5 backdrop-blur-md">
              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1">Họ tên của bạn</label>
                <input
                  v-model="feedbackForm.fullName"
                  type="text"
                  placeholder="Tên của bạn..."
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2 text-sm text-slate-100 focus:border-cyan-400 focus:outline-none"
                  required
                />
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1">Email liên hệ</label>
                <input
                  v-model="feedbackForm.email"
                  type="email"
                  placeholder="email@domain.com"
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2 text-sm text-slate-100 focus:border-cyan-400 focus:outline-none"
                  required
                />
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1">Phân loại</label>
                <select
                  v-model="feedbackForm.category"
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2 text-sm text-slate-100 focus:border-cyan-400 focus:outline-none"
                >
                  <option value="Feature">💡 Đề xuất tính năng mới</option>
                  <option value="Bug">🐛 Báo lỗi hệ thống (Bug Report)</option>
                  <option value="UX">🌟 Trải nghiệm giao diện UI/UX</option>
                  <option value="Partnership">🤝 Liên hệ hợp tác / Triển khai</option>
                </select>
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1">Nội dung chi tiết</label>
                <textarea
                  v-model="feedbackForm.message"
                  rows="3"
                  placeholder="Mô tả chi tiết ý kiến hoặc lỗi bạn gặp phải..."
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2 text-sm text-slate-100 focus:border-cyan-400 focus:outline-none"
                  required
                ></textarea>
              </div>

              <div v-if="feedbackToast" class="rounded-xl border border-cyan-500/40 bg-cyan-950/40 p-2.5 text-xs text-cyan-300">
                {{ feedbackToast }}
              </div>

              <button
                type="submit"
                :disabled="feedbackSubmitting"
                class="w-full rounded-xl bg-gradient-to-r from-cyan-400 to-blue-500 py-2.5 text-sm font-bold text-slate-950 shadow-md hover:brightness-110 transition-all disabled:opacity-50"
              >
                {{ feedbackSubmitting ? 'Đang gửi...' : 'GỬI PHẢN HỒI' }}
              </button>
            </form>
          </div>

          <!-- Community Comments Thread -->
          <div class="lg:col-span-7 space-y-4">
            <div class="flex items-center justify-between">
              <h3 class="text-lg font-bold text-white flex items-center gap-2">
                <span>💬</span>
                <span>Thảo Luận Cộng Đồng</span>
              </h3>
              <span class="text-xs text-cyan-400 font-mono">{{ comments.length }} Bình luận</span>
            </div>

            <!-- Comment Input Box -->
            <form @submit.prevent="submitComment" class="rounded-3xl border border-slate-800 bg-slate-900/90 p-4 backdrop-blur-md space-y-3">
              <div class="flex items-center gap-3">
                <input
                  v-model="newComment.authorName"
                  type="text"
                  placeholder="Tên của bạn..."
                  class="w-1/3 rounded-xl border border-slate-700 bg-slate-950 px-3 py-1.5 text-xs text-slate-100 focus:border-cyan-400 focus:outline-none"
                  required
                />
                <input
                  v-model="newComment.content"
                  type="text"
                  placeholder="Để lại bình luận hoặc câu hỏi của bạn..."
                  class="w-2/3 rounded-xl border border-slate-700 bg-slate-950 px-3 py-1.5 text-xs text-slate-100 focus:border-cyan-400 focus:outline-none"
                  required
                />
              </div>

              <div class="flex items-center justify-between pt-1">
                <span v-if="commentToast" class="text-xs text-cyan-300 font-semibold">{{ commentToast }}</span>
                <span v-else class="text-[11px] text-slate-500">Mọi bình luận đều hiển thị công khai</span>

                <button
                  type="submit"
                  :disabled="commentSubmitting"
                  class="rounded-xl bg-cyan-500 px-4 py-1.5 text-xs font-bold text-slate-950 hover:bg-cyan-400 transition-all disabled:opacity-50"
                >
                  Đăng
                </button>
              </div>
            </form>

            <!-- Comments List -->
            <div class="space-y-3 max-h-[500px] overflow-y-auto pr-1">
              <div
                v-for="cmt in comments"
                :key="cmt.id"
                class="rounded-2xl border border-slate-800/80 bg-slate-900/50 p-4 transition-all hover:border-slate-700"
              >
                <div class="flex items-start justify-between">
                  <div class="flex items-center gap-2.5">
                    <img
                      :src="cmt.avatarUrl"
                      :alt="cmt.authorName"
                      class="h-8 w-8 rounded-full border border-cyan-400/40 object-cover"
                    />
                    <div>
                      <div class="flex items-center gap-1.5">
                        <span class="text-xs font-bold text-white">{{ cmt.authorName }}</span>
                        <span class="rounded bg-cyan-500/10 px-1.5 py-0.2 text-[9px] font-semibold text-cyan-300 border border-cyan-500/20">
                          {{ cmt.badge || 'Operator' }}
                        </span>
                      </div>
                      <span class="text-[10px] text-slate-500">{{ new Date(cmt.createdAt).toLocaleString('vi-VN') }}</span>
                    </div>
                  </div>

                  <!-- Like Button -->
                  <button
                    type="button"
                    @click="reactComment(cmt)"
                    class="inline-flex items-center gap-1 rounded-full bg-slate-800/80 px-2.5 py-1 text-[10px] font-bold text-pink-300 hover:bg-pink-500/20 transition-all"
                  >
                    <span>♥</span>
                    <span>{{ cmt.likesCount }}</span>
                  </button>
                </div>

                <p class="mt-2 text-xs text-slate-300 pl-10">
                  {{ cmt.content }}
                </p>

                <!-- Replies -->
                <div v-if="cmt.replies && cmt.replies.length > 0" class="mt-3 pl-10 space-y-2 border-t border-slate-800/50 pt-2">
                  <div
                    v-for="rep in cmt.replies"
                    :key="rep.id"
                    class="rounded-xl border border-cyan-500/20 bg-cyan-950/20 p-2.5"
                  >
                    <div class="flex items-center gap-2">
                      <img :src="rep.avatarUrl" :alt="rep.authorName" class="h-5 w-5 rounded-full object-cover" />
                      <span class="text-[11px] font-bold text-cyan-300">{{ rep.authorName }}</span>
                      <span class="text-[9px] bg-cyan-500/20 text-cyan-200 px-1 rounded">{{ rep.badge }}</span>
                    </div>
                    <p class="text-xs text-slate-300 mt-1 pl-7">{{ rep.content }}</p>
                  </div>
                </div>
              </div>
            </div>

          </div>

        </div>
      </div>
    </section>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- SECTION: ĐĂNG KÝ BẢN TIN (NEWSLETTER)                     -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <section class="relative z-10 py-16 px-4 sm:px-6 lg:px-8">
      <div class="mx-auto max-w-5xl rounded-3xl border border-cyan-400/40 bg-gradient-to-r from-slate-900 via-slate-950 to-slate-900 p-8 sm:p-12 text-center shadow-[0_0_40px_rgba(0,240,255,0.2)]">
        <span class="text-2xl mb-2 inline-block">✉️</span>
        <h2 class="text-2xl sm:text-3xl font-black text-white">ĐĂNG KÝ NHẬN BẢN TIN & BẢN VÁ AN NINH</h2>
        <p class="mt-2 text-xs sm:text-sm text-slate-300 max-w-xl mx-auto">
          Nhận thông báo cập nhật phiên bản, thông số kỹ thuật và báo cáo bảo mật định kỳ từ đội ngũ V-Shield.
        </p>

        <form @submit.prevent="submitNewsletter" class="mt-6 flex flex-col sm:flex-row max-w-md mx-auto gap-3">
          <input
            v-model="newsletterEmail"
            type="email"
            placeholder="Nhập email của bạn..."
            class="flex-1 rounded-2xl border border-slate-700 bg-slate-950 px-4 py-3 text-sm text-slate-100 placeholder-slate-500 focus:border-cyan-400 focus:outline-none"
            required
          />
          <button
            type="submit"
            :disabled="newsletterSubmitting"
            class="rounded-2xl bg-gradient-to-r from-cyan-400 to-teal-400 px-6 py-3 text-sm font-extrabold text-slate-950 shadow-md hover:scale-105 transition-all disabled:opacity-50"
          >
            ĐĂNG KÝ
          </button>
        </form>
        <p v-if="newsletterToast" class="mt-3 text-xs text-cyan-300 font-semibold">{{ newsletterToast }}</p>
      </div>
    </section>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- SECTION: ĐỘI NGŨ PHÁT TRIỂN & CỐ VẤN (CREATORS LORE)     -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <section id="team" class="relative z-10 py-20 bg-slate-950/70 border-t border-cyan-500/10 px-4 sm:px-6 lg:px-8">
      <div class="mx-auto max-w-7xl">
        <div class="text-center max-w-3xl mx-auto mb-16 space-y-3">
          <span class="text-xs font-extrabold tracking-widest text-indigo-400 uppercase">ĐỒ ÁN TỐT NGHIỆP CÔNG NGHỆ</span>
          <h2 class="text-3xl sm:text-4xl font-black text-transparent bg-clip-text bg-gradient-to-r from-indigo-300 via-white to-pink-300">
            ĐỘI NGŨ PHÁT TRIỂN & CỐ VẤN
          </h2>
          <p class="text-sm text-slate-400">
            Dự án được nghiên cứu và phát triển bởi sinh viên ngành Công nghệ Thông tin dưới sự hướng dẫn chuyên môn tận tâm.
          </p>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          <div
            v-for="(member, idx) in teamMembers"
            :key="idx"
            class="group relative overflow-hidden rounded-3xl border border-slate-800 bg-gradient-to-b from-slate-900/90 to-slate-950 p-6 backdrop-blur-md transition-all hover:border-cyan-400/50 hover:shadow-[0_0_30px_rgba(0,240,255,0.2)]"
          >
            <div class="flex items-center gap-4 mb-4">
              <img
                :src="member.avatar"
                :alt="member.name"
                class="h-16 w-16 rounded-2xl border-2 border-cyan-400/60 object-cover shadow-[0_0_15px_rgba(0,240,255,0.3)] group-hover:scale-105 transition-transform"
              />
              <div>
                <span class="rounded-full bg-cyan-500/20 px-2 py-0.5 text-[9px] font-bold text-cyan-300 border border-cyan-500/30">
                  {{ member.badge }}
                </span>
                <h3 class="text-base font-bold text-white mt-1">{{ member.name }}</h3>
                <p class="text-xs text-pink-300 font-medium">{{ member.role }}</p>
              </div>
            </div>

            <p class="text-xs text-slate-300 leading-relaxed border-t border-slate-800/80 pt-3">
              {{ member.specialty }}
            </p>
          </div>
        </div>
      </div>
    </section>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- FOOTER                                                    -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <footer class="relative z-10 border-t border-slate-800 bg-slate-950 py-12 px-4 sm:px-6 lg:px-8 text-slate-400 text-xs">
      <div class="mx-auto max-w-7xl flex flex-col md:flex-row items-center justify-between gap-6">
        <div class="flex items-center gap-3">
          <span class="text-2xl">🛡️</span>
          <div>
            <p class="font-bold text-white text-sm">V-SHIELD 2.0 • NEURAL DEFENSE PLATFORM</p>
            <p class="text-[11px] text-slate-500">© 2026 Đồ Án Tốt Nghiệp Công Nghệ Thông Tin. All rights reserved.</p>
          </div>
        </div>

        <div class="flex flex-wrap items-center gap-6 font-semibold">
          <router-link to="/about-project" class="hover:text-cyan-300 transition-colors">Về Dự Án</router-link>
          <button type="button" @click="scrollToSection('download')" class="hover:text-cyan-300 transition-colors">Tải Ứng Dụng</button>
          <router-link to="/login" class="hover:text-cyan-300 transition-colors">Quản Trị Viên</router-link>
          <span class="text-emerald-400 flex items-center gap-1.5 font-mono">
            <span class="h-2 w-2 rounded-full bg-emerald-400 animate-pulse"></span>
            CENTRAL_SERVER: ONLINE
          </span>
        </div>
      </div>
    </footer>

    <!-- Community Auth Modal -->
    <PortalAuthModal
      :show="showAuthModal"
      :current-user="communityUser"
      @close="showAuthModal = false"
      @login-success="handleLoginSuccess"
      @logout="handleLogout"
    />
  </div>
</template>

<style scoped>
/* Custom animations & polish */
@keyframes float {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-8px); }
}

.animate-float {
  animation: float 4s ease-in-out infinite;
}
</style>
