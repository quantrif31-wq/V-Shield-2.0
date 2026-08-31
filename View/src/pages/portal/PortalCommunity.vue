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
      reviewToast.value = '✨ Cảm ơn bạn! Đánh giá đã được gửi thành công.'
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
</script>

<template>
  <div class="py-12 lg:py-16">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-12">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 rounded-full border border-pink-500/30 bg-pink-950/40 px-3 py-1 text-xs font-bold text-pink-300 font-mono">
          <span>COMMUNITY FEEDBACK & REVIEWS</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-extrabold tracking-tight text-slate-100 font-mono">
          Đánh Giá & Cộng Đồng
        </h1>
        <p class="mx-auto max-w-2xl text-xs sm:text-sm text-slate-400 leading-relaxed">
          Không gian trao đổi kỹ thuật, đánh giá chất lượng sản phẩm và đóng góp ý kiến trực tiếp từ các kỹ sư, chuyên gia an ninh và người dùng.
        </p>
      </div>

      <!-- Navigation Tabs -->
      <div class="flex justify-center border-b border-slate-800">
        <div class="flex gap-4">
          <button
            type="button"
            @click="activeTab = 'reviews'; triggerSfx()"
            class="relative pb-3 text-sm font-bold uppercase tracking-wider transition"
            :class="[
              activeTab === 'reviews'
                ? 'text-cyan-300 drop-shadow-[0_0_10px_rgba(0,240,255,0.6)]'
                : 'text-slate-400 hover:text-slate-200'
            ]"
          >
            <span>★ Đánh Giá Chuyên Gia & Người Dùng ({{ reviews.length }})</span>
            <span v-if="activeTab === 'reviews'" class="absolute bottom-0 left-0 right-0 h-0.5 bg-gradient-to-r from-cyan-400 to-pink-400"></span>
          </button>

          <button
            type="button"
            @click="activeTab = 'comments'; triggerSfx()"
            class="relative pb-3 text-sm font-bold uppercase tracking-wider transition"
            :class="[
              activeTab === 'comments'
                ? 'text-pink-300 drop-shadow-[0_0_10px_rgba(255,42,133,0.6)]'
                : 'text-slate-400 hover:text-slate-200'
            ]"
          >
            <span>💬 Diễn Đàn Thảo Luận Kỹ Thuật ({{ comments.length }})</span>
            <span v-if="activeTab === 'comments'" class="absolute bottom-0 left-0 right-0 h-0.5 bg-gradient-to-r from-pink-400 to-purple-400"></span>
          </button>
        </div>
      </div>

      <!-- ── TAB 1: REVIEWS ── -->
      <div v-if="activeTab === 'reviews'" class="space-y-10">
        <!-- Review Submission Card -->
        <div class="rounded-3xl border border-cyan-500/30 bg-slate-900/80 p-6 sm:p-8 backdrop-blur-2xl space-y-4">
          <div class="flex items-center justify-between">
            <h3 class="text-lg font-bold text-slate-100 font-mono">
              Gửi Đánh Giá Của Bạn
            </h3>
            <button
              v-if="!communityUser"
              type="button"
              @click="openAuthModal"
              class="text-xs font-semibold text-pink-400 hover:underline"
            >
              Đăng nhập Google SSO để hiển thị avatar
            </button>
          </div>

          <form @submit.prevent="submitReview" class="space-y-4">
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <div>
                <label class="block text-xs font-semibold text-slate-400 mb-1">Họ và Tên</label>
                <input
                  v-model="newReview.authorName"
                  type="text"
                  placeholder="Nguyễn Văn A"
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2 text-xs text-slate-200 placeholder-slate-500 outline-none focus:border-cyan-400"
                />
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-400 mb-1">Điểm Đánh Giá</label>
                <select
                  v-model.number="newReview.rating"
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2 text-xs text-amber-400 outline-none focus:border-cyan-400"
                >
                  <option :value="5">★★★★★ (5/5 - Xuất sắc)</option>
                  <option :value="4">★★★★☆ (4/5 - Rất tốt)</option>
                  <option :value="3">★★★☆☆ (3/5 - Bình thường)</option>
                  <option :value="2">★★☆☆☆ (2/5 - Cần cải thiện)</option>
                  <option :value="1">★☆☆☆☆ (1/5 - Kém)</option>
                </select>
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-400 mb-1">Nền Tảng Trải Nghiệm</label>
                <select
                  v-model="newReview.platform"
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2 text-xs text-slate-200 outline-none focus:border-cyan-400"
                >
                  <option value="Web Cloud">Web Cloud (VPS)</option>
                  <option value="Docker Local">Docker Local</option>
                  <option value="Mobile Android">Mobile Android (APK)</option>
                </select>
              </div>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-400 mb-1">Nội Dung Nhận Xét</label>
              <textarea
                v-model="newReview.content"
                rows="3"
                placeholder="Chia sẻ cảm nhận về tốc độ nhận diện Face ID, barie tự động, độ trễ đồng bộ..."
                class="w-full rounded-xl border border-slate-700 bg-slate-950/80 p-3.5 text-xs text-slate-200 placeholder-slate-500 outline-none focus:border-cyan-400"
              ></textarea>
            </div>

            <div class="flex items-center justify-between">
              <div v-if="reviewToast" class="text-xs font-semibold text-cyan-300">
                {{ reviewToast }}
              </div>
              <div v-else></div>

              <button
                type="submit"
                :disabled="reviewSubmitting"
                class="rounded-xl bg-gradient-to-r from-cyan-500 to-pink-500 px-6 py-2.5 text-xs font-bold uppercase tracking-wider text-slate-950 shadow-[0_0_20px_rgba(0,240,255,0.4)] transition hover:opacity-90 disabled:opacity-50"
              >
                {{ reviewSubmitting ? 'Đang gửi...' : 'Đăng Đánh Giá' }}
              </button>
            </div>
          </form>
        </div>

        <!-- Review Cards List -->
        <div class="grid grid-cols-1 gap-6 md:grid-cols-3">
          <div
            v-for="rev in reviews"
            :key="rev.id"
            class="rounded-2xl border border-slate-800 bg-slate-900/60 p-6 backdrop-blur-xl space-y-4 hover:border-cyan-500/40 transition"
          >
            <div class="flex items-center gap-3">
              <img :src="rev.avatarUrl" :alt="rev.authorName" class="h-11 w-11 rounded-full border border-cyan-400/40 bg-slate-800" />
              <div>
                <h4 class="text-xs font-bold text-slate-100 font-mono">{{ rev.authorName }}</h4>
                <div class="text-[10px] text-cyan-400 font-medium">{{ rev.authorRole || rev.platform }}</div>
              </div>
            </div>

            <!-- Star Rating -->
            <div class="flex items-center gap-1 text-amber-400 text-sm">
              <span v-for="s in rev.rating" :key="s">★</span>
            </div>

            <p class="text-xs text-slate-300 leading-relaxed">
              "{{ rev.content }}"
            </p>

            <div class="flex items-center justify-between text-[10px] text-slate-500 border-t border-slate-800/80 pt-3 font-mono">
              <span>{{ rev.platform }}</span>
              <span>♥ {{ rev.likesCount }} hữu ích</span>
            </div>
          </div>
        </div>
      </div>

      <!-- ── TAB 2: COMMENTS & DISCUSSION ── -->
      <div v-else class="space-y-8">
        <!-- New Comment Input -->
        <div class="rounded-3xl border border-pink-500/30 bg-slate-900/80 p-6 sm:p-8 backdrop-blur-2xl space-y-4">
          <h3 class="text-lg font-bold text-slate-100 font-mono">
            Đặt Câu Hỏi / Thảo Luận Kỹ Thuật
          </h3>

          <form @submit.prevent="submitComment" class="space-y-3">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <input
                  v-model="newComment.authorName"
                  type="text"
                  placeholder="Tên / Biệt danh của bạn"
                  class="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-3.5 py-2 text-xs text-slate-200 outline-none focus:border-pink-400"
                />
              </div>
            </div>

            <div>
              <textarea
                v-model="newComment.content"
                rows="3"
                placeholder="Nhập nội dung thảo luận, thắc mắc về triển khai Docker, AI Model hoặc thuật toán TOTP..."
                class="w-full rounded-xl border border-slate-700 bg-slate-950/80 p-3.5 text-xs text-slate-200 outline-none focus:border-pink-400"
              ></textarea>
            </div>

            <div class="flex items-center justify-between">
              <div v-if="commentToast" class="text-xs font-semibold text-pink-300">
                {{ commentToast }}
              </div>
              <div v-else></div>

              <button
                type="submit"
                :disabled="commentSubmitting"
                class="rounded-xl bg-gradient-to-r from-pink-500 to-purple-600 px-6 py-2 text-xs font-bold uppercase text-white shadow-[0_0_20px_rgba(255,42,133,0.4)] transition hover:opacity-90 disabled:opacity-50"
              >
                {{ commentSubmitting ? 'Đang gửi...' : 'Gửi Thảo Luận' }}
              </button>
            </div>
          </form>
        </div>

        <!-- Comment Thread List -->
        <div class="space-y-4">
          <div
            v-for="cmt in comments"
            :key="cmt.id"
            class="rounded-2xl border border-slate-800 bg-slate-900/60 p-6 backdrop-blur-xl space-y-3"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-3">
                <img :src="cmt.avatarUrl" :alt="cmt.authorName" class="h-9 w-9 rounded-full border border-pink-400/40 bg-slate-800" />
                <div>
                  <div class="flex items-center gap-2">
                    <span class="text-xs font-bold text-slate-200 font-mono">{{ cmt.authorName }}</span>
                    <span v-if="cmt.badge" class="rounded bg-pink-950 px-1.5 py-0.2 text-[9px] font-bold text-pink-300 border border-pink-500/30">
                      {{ cmt.badge }}
                    </span>
                  </div>
                  <span class="text-[10px] text-slate-500 font-mono">{{ new Date(cmt.createdAt).toLocaleString('vi-VN') }}</span>
                </div>
              </div>

              <button
                type="button"
                @click="reactComment(cmt)"
                class="flex items-center gap-1.5 rounded-full border border-slate-700 bg-slate-950/60 px-3 py-1 text-xs text-slate-300 hover:border-pink-500/50 hover:text-pink-300 transition"
              >
                <span>♥</span>
                <span>{{ cmt.likesCount }}</span>
              </button>
            </div>

            <p class="text-xs text-slate-300 leading-relaxed pl-12">
              {{ cmt.content }}
            </p>

            <!-- Nested Replies -->
            <div v-if="cmt.replies && cmt.replies.length > 0" class="pl-12 pt-2 space-y-2">
              <div
                v-for="rep in cmt.replies"
                :key="rep.id"
                class="rounded-xl border border-slate-800 bg-slate-950/60 p-3 space-y-1.5"
              >
                <div class="flex items-center gap-2">
                  <img :src="rep.avatarUrl" :alt="rep.authorName" class="h-6 w-6 rounded-full border border-cyan-400" />
                  <span class="text-xs font-bold text-cyan-300 font-mono">{{ rep.authorName }}</span>
                  <span class="rounded bg-cyan-950 px-1.5 py-0.2 text-[9px] font-bold text-cyan-300">
                    {{ rep.badge || 'Developer' }}
                  </span>
                </div>
                <p class="text-xs text-slate-300 pl-8">
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
