<script setup>
import { ref, reactive, inject, onMounted } from 'vue'
import { portalApi } from '../../services/portalApi'

const communityUser = inject('communityUser', ref(null))
const feedbackForm = reactive({
  fullName: '',
  email: '',
  category: 'Feature',
  message: ''
})
const feedbackSubmitting = ref(false)
const feedbackToast = ref('')

const openFaqIndex = ref(null)

const faqs = [
  {
    q: 'V-Shield 2.0 hoạt động như thế nào khi trạm cục bộ bị mất mạng Internet?',
    a: 'Nhờ kiến trúc Offline-First Hybrid Sync, mọi dữ liệu nhân viên, mã QR động và khuôn mặt Face ID đã được đồng bộ sẵn cục bộ. Trạm vẫn tự động nhận diện và mở rào chắn bình thường. Khi có mạng trở lại, lịch sử quẹt thẻ sẽ tự động gửi ngược lên Cloud trung tâm.'
  },
  {
    q: 'Mã QR động trên app Mobile có thể chụp ảnh màn hình để gửi người khác không?',
    a: 'Không thể. Mã QR được tạo động dựa trên thuật toán TOTP SHA-256 thay đổi liên tục mỗi 30 giây. Mỗi mã quét chỉ có giá trị duy nhất 1 lần (Single-Use Token) và tự động hết hạn, vô hiệu hóa hoàn toàn việc dùng ảnh chụp màn hình.'
  },
  {
    q: 'Hệ thống có thể triển khai trên môi trường máy chủ nội bộ (On-Premises) không?',
    a: 'Có. Toàn bộ hệ thống V-Shield 2.0 đã được đóng gói 100% bằng Docker và Docker Compose, cho phép triển khai nhanh chóng trên cả máy chủ Cloud (VPS) hoặc máy tính cục bộ tại trạm bảo vệ chỉ với một câu lệnh.'
  },
  {
    q: 'Cuộc gọi Video Call VoIP có an toàn và bảo mật không?',
    a: 'Luồng cuộc gọi thoại & hình ảnh được mã hóa toàn trình (End-to-End Encryption) qua chuẩn WebRTC DTLS/SRTP, đảm bảo chỉ có người gọi và phòng bảo vệ mới giải mã được tín hiệu.'
  }
]

function toggleFaq(idx) {
  openFaqIndex.value = openFaqIndex.value === idx ? null : idx
}

function triggerSfx() {
  window.dispatchEvent(new CustomEvent('portal-click-sfx'))
}

onMounted(() => {
  if (communityUser.value) {
    feedbackForm.fullName = communityUser.value.fullName
    feedbackForm.email = communityUser.value.email
  }
})

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
      feedbackToast.value = '✨ Góp ý của bạn đã được chuyển tới nhóm phát triển!'
      feedbackForm.message = ''
      setTimeout(() => { feedbackToast.value = '' }, 5000)
    }
  } catch {
    feedbackToast.value = 'Không thể gửi góp ý lúc này, vui lòng thử lại.'
  } finally {
    feedbackSubmitting.value = false
  }
}
</script>

<template>
  <div class="py-12 lg:py-16">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-16">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 rounded-full border border-teal-500/30 bg-teal-950/40 px-3 py-1 text-xs font-bold text-teal-300 font-mono">
          <span>FEEDBACK & SUPPORT CHANNEL</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-extrabold tracking-tight text-slate-100 font-mono">
          Góp Ý & Liên Hệ Hỗ Trợ
        </h1>
        <p class="mx-auto max-w-2xl text-xs sm:text-sm text-slate-400 leading-relaxed">
          Chúng tôi luôn lắng nghe mọi phản hồi, ý kiến đóng góp tính năng và giải đáp thắc mắc từ hội đồng chấm đồ án, chuyên gia và người dùng.
        </p>
      </div>

      <!-- Main Contact Grid (Feedback Form + Contact Info) -->
      <div class="grid grid-cols-1 gap-10 lg:grid-cols-12 items-start">
        <!-- Left: Feedback Form (7 cols) -->
        <div class="rounded-3xl border border-cyan-500/30 bg-slate-900/80 p-8 shadow-[0_0_40px_rgba(0,240,255,0.15)] backdrop-blur-2xl space-y-6 lg:col-span-7">
          <h3 class="text-xl font-bold text-slate-100 font-mono">
            Hòm Thư Đóng Góp Ý Kiến
          </h3>

          <form @submit.prevent="submitFeedback" class="space-y-4">
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label class="block text-xs font-semibold text-slate-400 mb-1">Họ và Tên</label>
                <input
                  v-model="feedbackForm.fullName"
                  type="text"
                  placeholder="Nguyễn Văn A"
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2.5 text-xs text-slate-200 outline-none focus:border-cyan-400"
                />
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-400 mb-1">Địa Chỉ Email</label>
                <input
                  v-model="feedbackForm.email"
                  type="email"
                  placeholder="example@domain.com"
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2.5 text-xs text-slate-200 outline-none focus:border-cyan-400"
                />
              </div>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-400 mb-1">Chủ Đề Góp Ý</label>
              <select
                v-model="feedbackForm.category"
                class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2.5 text-xs text-slate-200 outline-none focus:border-cyan-400"
              >
                <option value="Feature">💡 Đề xuất tính năng mới</option>
                <option value="Bug">🐛 Báo cáo sự cố / Lỗi hệ thống</option>
                <option value="UI/UX">🎨 Góp ý giao diện & trải nghiệm người dùng</option>
                <option value="Partnership">🤝 Hợp tác nghiên cứu & triển khai</option>
                <option value="Other">📝 Ý kiến khác</option>
              </select>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-400 mb-1">Nội Dung Chi Tiết</label>
              <textarea
                v-model="feedbackForm.message"
                rows="4"
                placeholder="Mô tả cụ thể góp ý của bạn để giúp hệ thống V-Shield 2.0 hoàn thiện hơn..."
                class="w-full rounded-xl border border-slate-700 bg-slate-950/80 p-3.5 text-xs text-slate-200 outline-none focus:border-cyan-400"
              ></textarea>
            </div>

            <div class="flex items-center justify-between pt-2">
              <div v-if="feedbackToast" class="text-xs font-semibold text-cyan-300">
                {{ feedbackToast }}
              </div>
              <div v-else></div>

              <button
                type="submit"
                :disabled="feedbackSubmitting"
                class="rounded-xl bg-gradient-to-r from-cyan-500 via-teal-400 to-pink-500 px-6 py-3 text-xs font-bold uppercase tracking-wider text-slate-950 shadow-[0_0_20px_rgba(0,240,255,0.4)] transition hover:opacity-90 disabled:opacity-50"
              >
                {{ feedbackSubmitting ? 'Đang chuyển tiếp...' : 'Gửi Góp Ý' }}
              </button>
            </div>
          </form>
        </div>

        <!-- Right: Official Info & Thesis Lab (5 cols) -->
        <div class="space-y-6 lg:col-span-5">
          <div class="rounded-3xl border border-slate-800 bg-slate-900/80 p-8 space-y-6 backdrop-blur-xl">
            <h3 class="text-lg font-bold text-slate-100 font-mono">
              Thông Tin Liên Hệ Dự Án
            </h3>

            <div class="space-y-4 text-xs">
              <div class="flex items-start gap-3">
                <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-cyan-500/20 text-cyan-300 border border-cyan-400/30">
                  <span class="text-sm">📍</span>
                </div>
                <div>
                  <div class="font-bold text-slate-200 font-mono">Phòng Lab Nghiên Cứu An Ninh</div>
                  <div class="text-slate-400 mt-0.5">Khoa Công Nghệ Thông Tin & An Toàn Thông Tin</div>
                </div>
              </div>

              <div class="flex items-start gap-3">
                <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-pink-500/20 text-pink-300 border border-pink-400/30">
                  <span class="text-sm">✉️</span>
                </div>
                <div>
                  <div class="font-bold text-slate-200 font-mono">Email Hỗ Trợ Đề Tài</div>
                  <div class="text-slate-400 mt-0.5 font-mono">contact@v-shield.site</div>
                </div>
              </div>

              <div class="flex items-start gap-3">
                <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-teal-500/20 text-teal-300 border border-teal-400/30">
                  <span class="text-sm">🌐</span>
                </div>
                <div>
                  <div class="font-bold text-slate-200 font-mono">Trang Web Triển Khai Trực Tuyến</div>
                  <div class="text-slate-400 mt-0.5 font-mono">https://v-shield.site</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- FAQ Section -->
      <div class="space-y-6">
        <h3 class="text-2xl font-extrabold text-slate-100 font-mono text-center">
          Câu Hỏi Thường Gặp (FAQ)
        </h3>

        <div class="mx-auto max-w-3xl space-y-3">
          <div
            v-for="(faq, fIdx) in faqs"
            :key="fIdx"
            class="rounded-2xl border border-slate-800 bg-slate-900/60 backdrop-blur-xl transition hover:border-cyan-500/30"
          >
            <button
              type="button"
              @click="toggleFaq(fIdx); triggerSfx()"
              class="flex w-full items-center justify-between p-4 sm:p-5 text-left text-xs sm:text-sm font-bold text-slate-200"
            >
              <span>{{ faq.q }}</span>
              <span class="ml-4 font-mono text-cyan-400 text-lg">{{ openFaqIndex === fIdx ? '−' : '+' }}</span>
            </button>
            <div
              v-if="openFaqIndex === fIdx"
              class="border-t border-slate-800/80 p-4 sm:p-5 pt-3 text-xs text-slate-300 leading-relaxed bg-slate-950/40"
            >
              {{ faq.a }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
