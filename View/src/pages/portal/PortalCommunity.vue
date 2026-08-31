<script setup>
import { ref, reactive, onMounted, inject } from 'vue'
import { portalApi } from '../../services/portalApi'

const communityUser = inject('communityUser', ref(null))
const openAuthModal = inject('openAuthModal', () => {})

const reviews = ref([])
const comments = ref([])
const activeTab = ref('reviews')

// Review Form
const newReview = reactive({
  authorName: '',
  rating: 5,
  content: '',
  platform: 'Web Cloud'
})
const reviewSubmitting = ref(false)
const reviewToast = ref('')

// Comment Form
const newComment = reactive({
  authorName: '',
  content: ''
})
const commentSubmitting = ref(false)
const commentToast = ref('')

function triggerSfx() {
  window.dispatchEvent(new CustomEvent('portal-click-sfx'))
}

onMounted(async () => {
  if (communityUser.value) {
    newReview.authorName = communityUser.value.fullName
    newComment.authorName = communityUser.value.fullName
  }

  try {
    const [revs, cmts] = await Promise.all([
      portalApi.getReviews(),
      portalApi.getComments()
    ])
    if (revs) reviews.value = revs
    if (cmts) comments.value = cmts
  } catch (err) {
    console.error('Failed to load community data:', err)
  }
})

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
      reviewToast.value = '✨ Đánh giá tác chiến đã được ghi nhận thành công!'
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
      commentToast.value = '💬 Tín hiệu trao đổi kỹ thuật đã được xuất bản!'
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
</script>

<template>
  <div class="py-10 lg:py-16 font-mono">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-12">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 border border-amber-500/40 bg-[#121620] px-3.5 py-1 text-xs font-black text-amber-400 mecha-cut-tr">
          <span>// OPERATOR COMBAT LOGS & DISCUSSIONS</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-black uppercase text-slate-100">
          NHẬT KÝ ĐÁNH GIÁ & CỘNG ĐỒNG
        </h1>
        <p class="mx-auto max-w-2xl font-sans text-xs sm:text-sm text-slate-400 leading-relaxed">
          Không gian trao đổi kỹ thuật, báo cáo thực chiến và đánh giá độ tin cậy của hệ thống V-Shield MK-II từ các chuyên gia an ninh.
        </p>
      </div>

      <!-- Navigation Tabs -->
      <div class="flex justify-center border-b border-slate-800">
        <div class="flex gap-4">
          <button
            type="button"
            @click="activeTab = 'reviews'; triggerSfx()"
            class="relative pb-3 text-xs sm:text-sm font-black uppercase tracking-wider transition"
            :class="[
              activeTab === 'reviews'
                ? 'text-amber-400 border-b-2 border-amber-400'
                : 'text-slate-500 hover:text-slate-300'
            ]"
          >
            <span>★ ĐÁNH GIÁ CHUYÊN GIA ({{ reviews.length }})</span>
          </button>

          <button
            type="button"
            @click="activeTab = 'comments'; triggerSfx()"
            class="relative pb-3 text-xs sm:text-sm font-black uppercase tracking-wider transition"
            :class="[
              activeTab === 'comments'
                ? 'text-orange-400 border-b-2 border-orange-400'
                : 'text-slate-500 hover:text-slate-300'
            ]"
          >
            <span>💬 DIỄN ĐÀN KỸ THUẬT ({{ comments.length }})</span>
          </button>
        </div>
      </div>

      <!-- ── TAB 1: REVIEWS ── -->
      <div v-if="activeTab === 'reviews'" class="space-y-10">
        <!-- Review Form -->
        <div class="mecha-hud-bracket border-2 border-amber-500/40 bg-[#0c0f15] p-6 sm:p-8 mecha-cut-corners shadow-[0_0_40px_rgba(255,204,0,0.1)] space-y-4">
          <div class="flex items-center justify-between border-b border-slate-800 pb-3">
            <h3 class="text-base font-black text-slate-100 uppercase">
              GỬI NHẬT KÝ ĐÁNH GIÁ CỦA BẠN
            </h3>
            <button
              v-if="!communityUser"
              type="button"
              @click="openAuthModal"
              class="text-xs font-bold text-amber-400 hover:underline"
            >
              [ ĐĂNG NHẬP PILOT SSO ]
            </button>
          </div>

          <form @submit.prevent="submitReview" class="space-y-4 font-sans">
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-3 font-mono">
              <div>
                <label class="block text-xs font-bold text-slate-400 mb-1">HỌ VÀ TÊN // CALLSIGN</label>
                <input
                  v-model="newReview.authorName"
                  type="text"
                  placeholder="Commander Alex"
                  class="w-full border border-slate-700 bg-[#07080b] px-3.5 py-2 text-xs text-slate-200 outline-none focus:border-amber-400 mecha-cut-tr"
                />
              </div>

              <div>
                <label class="block text-xs font-bold text-slate-400 mb-1">ĐIỂM ĐÁNH GIÁ TÁC CHIẾN</label>
                <select
                  v-model.number="newReview.rating"
                  class="w-full border border-slate-700 bg-[#07080b] px-3.5 py-2 text-xs text-amber-400 outline-none focus:border-amber-400 mecha-cut-tr"
                >
                  <option :value="5">★★★★★ (5/5 - Cực kỳ tin cậy)</option>
                  <option :value="4">★★★★☆ (4/5 - Rất tốt)</option>
                  <option :value="3">★★★☆☆ (3/5 - Chuẩn danh định)</option>
                  <option :value="2">★★☆☆☆ (2/5 - Cần tối ưu)</option>
                  <option :value="1">★☆☆☆☆ (1/5 - Không đạt)</option>
                </select>
              </div>

              <div>
                <label class="block text-xs font-bold text-slate-400 mb-1">NỀN TẢNG THỬ NGHIỆM</label>
                <select
                  v-model="newReview.platform"
                  class="w-full border border-slate-700 bg-[#07080b] px-3.5 py-2 text-xs text-slate-200 outline-none focus:border-amber-400 mecha-cut-tr"
                >
                  <option value="Web Cloud">Web Cloud (VPS)</option>
                  <option value="Docker Local">Docker Local Station</option>
                  <option value="Mobile Android">Mobile Field App (APK)</option>
                </select>
              </div>
            </div>

            <div>
              <label class="block text-xs font-mono font-bold text-slate-400 mb-1">NỘI DUNG NHẬT KÝ THỰC CHIẾN</label>
              <textarea
                v-model="newReview.content"
                rows="3"
                placeholder="Đánh giá về thời gian phản hồi Face ID, cơ chế mở barie, tính ổn định Offline-First..."
                class="w-full border border-slate-700 bg-[#07080b] p-3.5 text-xs text-slate-200 outline-none focus:border-amber-400 mecha-cut-tr"
              ></textarea>
            </div>

            <div class="flex items-center justify-between font-mono">
              <div v-if="reviewToast" class="text-xs font-bold text-amber-400">
                {{ reviewToast }}
              </div>
              <div v-else></div>

              <button
                type="submit"
                :disabled="reviewSubmitting"
                class="mecha-btn-hazard px-6 py-2.5 text-xs font-black uppercase mecha-cut-btn disabled:opacity-50"
              >
                {{ reviewSubmitting ? 'ĐANG NẠP...' : 'XUẤT BẢN ĐÁNH GIÁ' }}
              </button>
            </div>
          </form>
        </div>

        <!-- Review Cards List -->
        <div class="grid grid-cols-1 gap-6 md:grid-cols-3">
          <div
            v-for="rev in reviews"
            :key="rev.id"
            class="mecha-hud-bracket border border-slate-800 bg-[#0c0f15] p-6 mecha-cut-tr space-y-4 hover:border-amber-400 transition"
          >
            <div class="flex items-center gap-3">
              <img :src="rev.avatarUrl" :alt="rev.authorName" class="h-11 w-11 border-2 border-amber-400 bg-slate-900" />
              <div>
                <h4 class="text-xs font-black text-slate-100 uppercase">{{ rev.authorName }}</h4>
                <div class="text-[10px] text-amber-400 font-bold">{{ rev.authorRole || rev.platform }}</div>
              </div>
            </div>

            <div class="flex items-center gap-1 text-amber-400 text-sm">
              <span v-for="s in rev.rating" :key="s">★</span>
            </div>

            <p class="font-sans text-xs text-slate-300 leading-relaxed">
              "{{ rev.content }}"
            </p>

            <div class="flex items-center justify-between text-[10px] text-slate-500 border-t border-slate-800 pt-3">
              <span class="text-amber-400/80 font-bold">[ {{ rev.platform }} ]</span>
              <span>♥ {{ rev.likesCount }} CONFIRMED</span>
            </div>
          </div>
        </div>
      </div>

      <!-- ── TAB 2: COMMENTS ── -->
      <div v-else class="space-y-8">
        <!-- New Comment Form -->
        <div class="mecha-hud-bracket border-2 border-orange-500/40 bg-[#0c0f15] p-6 sm:p-8 mecha-cut-corners shadow-[0_0_40px_rgba(255,85,0,0.1)] space-y-4">
          <h3 class="text-base font-black text-slate-100 uppercase">
            TRUYỀN PHÁT TÍN HIỆU THẢO LUẬN // TECH COMMS
          </h3>

          <form @submit.prevent="submitComment" class="space-y-3 font-sans">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 font-mono">
              <div>
                <input
                  v-model="newComment.authorName"
                  type="text"
                  placeholder="Pilot Callsign / Tên"
                  class="w-full border border-slate-700 bg-[#07080b] px-3.5 py-2 text-xs text-slate-200 outline-none focus:border-orange-400 mecha-cut-tr"
                />
              </div>
            </div>

            <div>
              <textarea
                v-model="newComment.content"
                rows="3"
                placeholder="Nhập nội dung thảo luận về giải pháp mô hình AI, kiến trúc Docker hoặc giao thức TOTP..."
                class="w-full border border-slate-700 bg-[#07080b] p-3.5 text-xs text-slate-200 outline-none focus:border-orange-400 mecha-cut-tr"
              ></textarea>
            </div>

            <div class="flex items-center justify-between font-mono">
              <div v-if="commentToast" class="text-xs font-bold text-orange-400">
                {{ commentToast }}
              </div>
              <div v-else></div>

              <button
                type="submit"
                :disabled="commentSubmitting"
                class="mecha-btn-hazard px-6 py-2 text-xs font-black uppercase mecha-cut-btn disabled:opacity-50"
              >
                {{ commentSubmitting ? 'ĐANG PHÁT...' : 'GỬI TÍN HIỆU' }}
              </button>
            </div>
          </form>
        </div>

        <!-- Comments Stream -->
        <div class="space-y-4">
          <div
            v-for="cmt in comments"
            :key="cmt.id"
            class="mecha-hud-bracket border border-slate-800 bg-[#0c0f15] p-6 mecha-cut-tr space-y-3"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-3">
                <img :src="cmt.avatarUrl" :alt="cmt.authorName" class="h-9 w-9 border border-orange-400 bg-slate-900" />
                <div>
                  <div class="flex items-center gap-2">
                    <span class="text-xs font-black text-slate-200 uppercase">{{ cmt.authorName }}</span>
                    <span v-if="cmt.badge" class="bg-orange-950 px-1.5 py-0.2 text-[9px] font-bold text-orange-300 border border-orange-500/30">
                      {{ cmt.badge }}
                    </span>
                  </div>
                  <span class="text-[10px] text-slate-500">{{ new Date(cmt.createdAt).toLocaleString('vi-VN') }}</span>
                </div>
              </div>

              <button
                type="button"
                @click="reactComment(cmt)"
                class="flex items-center gap-1.5 border border-slate-700 bg-[#07080b] px-3 py-1 text-xs text-slate-300 hover:border-orange-400 hover:text-orange-300 transition"
              >
                <span>♥</span>
                <span>{{ cmt.likesCount }}</span>
              </button>
            </div>

            <p class="font-sans text-xs text-slate-300 leading-relaxed pl-12">
              {{ cmt.content }}
            </p>

            <!-- Nested Replies -->
            <div v-if="cmt.replies && cmt.replies.length > 0" class="pl-12 pt-2 space-y-2">
              <div
                v-for="rep in cmt.replies"
                :key="rep.id"
                class="border border-slate-800 bg-[#07080b] p-3 space-y-1.5 mecha-cut-tr"
              >
                <div class="flex items-center gap-2">
                  <img :src="rep.avatarUrl" :alt="rep.authorName" class="h-6 w-6 border border-amber-400" />
                  <span class="text-xs font-bold text-amber-400">{{ rep.authorName }}</span>
                  <span class="bg-amber-950 px-1.5 py-0.2 text-[8px] font-bold text-amber-300">
                    {{ rep.badge || 'DEVELOPER' }}
                  </span>
                </div>
                <p class="font-sans text-xs text-slate-300 pl-8">
                  {{ rep.content }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
