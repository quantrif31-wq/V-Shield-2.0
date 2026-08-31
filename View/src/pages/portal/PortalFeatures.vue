<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const activeCategory = ref('all')

const categories = [
  { id: 'all', name: 'Tất Cả Công Nghệ' },
  { id: 'ai', name: 'AI & Sinh Trắc Học' },
  { id: 'barrier', name: 'Rào Chắn & Giao Thông' },
  { id: 'sync', name: 'Mạng & Đồng Bộ Lai' },
  { id: 'security', name: 'Bảo Mật & UEBA' }
]

const features = [
  {
    id: 1,
    category: 'ai',
    title: 'AI Face ID Realtime 60 FPS',
    tag: 'BIOMETRIC ENGINE',
    accent: 'cyan',
    desc: 'Hệ thống nhận diện khuôn mặt xử lý tốc độ cao với thuật toán YOLOv11 + ArcFace, hỗ trợ nhận diện đa góc độ (lên đến 45 độ), thích ứng điều kiện ánh sáng yếu và chống giả mạo hình ảnh/video 2D (Anti-Spoofing).',
    specs: [
      { label: 'Tốc độ nhận diện', value: '< 45ms' },
      { label: 'Độ chính xác', value: '99.98%' },
      { label: 'Góc nhận diện', value: '±45° Yaw/Pitch' },
      { label: 'Chống giả mạo', value: 'Live 3D Depth' }
    ]
  },
  {
    id: 2,
    category: 'barrier',
    title: 'Virtual Smart Barrier & Đối Soát Biển Số',
    tag: 'TRAFFIC CONTROL',
    accent: 'pink',
    desc: 'Cơ chế điều khiển rào chắn tự động kết hợp camera nhận diện biển số xe (ANPR OCR). Tự động đối chiếu thông tin chủ xe và người lái qua thẻ từ / khuôn mặt để phát hiện gian lận đổi biển số.',
    specs: [
      { label: 'Thời gian mở barie', value: '0.6s' },
      { label: 'Độ chính xác OCR', value: '99.5%' },
      { label: 'Đồng bộ làn xe', value: 'Đa luồng Lane 1/2' },
      { label: 'Cảnh báo đối soát', value: 'Realtime SOC Alert' }
    ]
  },
  {
    id: 3,
    category: 'security',
    title: 'Mã QR Động TOTP Chống Gian Lận',
    tag: 'ANTI-CLONING PASS',
    accent: 'teal',
    desc: 'Thẻ thông hành điện tử sinh mã QR động thay đổi liên tục mỗi 30 giây dựa trên thuật toán TOTP SHA-256 mã hóa khóa bí mật phần cứng, vô hiệu hóa hoàn toàn hành vi chụp màn hình chia sẻ cho người khác.',
    specs: [
      { label: 'Thời gian xoay mã', value: '30s / token' },
      { label: 'Thuật toán mã hóa', value: 'HMAC-SHA256' },
      { label: 'Khả năng chống sao chép', value: '100% Tuyệt đối' },
      { label: 'Hỗ trợ offline', value: 'Quét tại trạm không mạng' }
    ]
  },
  {
    id: 4,
    category: 'barrier',
    title: 'VoIP Intercom & Video Call WebRTC',
    tag: 'HD COMMUNICATION',
    accent: 'purple',
    desc: 'Hệ thống liên lạc thoại & video đa điểm độ trễ cực thấp giữa bảo vệ tại cổng và người dùng qua ứng dụng di động. Hỗ trợ xác nhận danh tính khách vãng lai và phê duyệt mở barie khẩn cấp từ xa.',
    specs: [
      { label: 'Độ trễ truyền hình', value: '< 100ms WebRTC' },
      { label: 'Chất lượng video', value: '1080p 60FPS' },
      { label: 'Mã hóa luồng gọi', value: 'DTLS / SRTP' },
      { label: 'Tích hợp mở cổng', value: 'Nút ấn 1-chạm trong cuộc gọi' }
    ]
  },
  {
    id: 5,
    category: 'sync',
    title: 'Hybrid Sync Protocol (Offline-First)',
    tag: 'DISTRIBUTED ARCHITECTURE',
    accent: 'cyan',
    desc: 'Kiến trúc đồng bộ hai chiều phân tán giữa đám mây trung tâm (Cloud Central) và các trạm kiểm soát cục bộ (Local Stations). Trạm cục bộ vẫn vận hành độc lập hoàn toàn khi đứt cáp quang và tự động đối chiếu khi có mạng.',
    specs: [
      { label: 'Độ trễ đồng bộ', value: 'Sub-30ms' },
      { label: 'Xung đột dữ liệu', value: 'Vector Clocks CRDT' },
      { label: 'Thời gian offline tối đa', value: 'Vô hạn (Tự cache)' },
      { label: 'Băng thông tối ưu', value: 'Nén Gzip/Protobuf' }
    ]
  },
  {
    id: 6,
    category: 'security',
    title: 'UEBA - Phân Tích Hành Vi Người Dùng & AI Anomaly',
    tag: 'SECURITY INTELLIGENCE',
    accent: 'pink',
    desc: 'Hệ thống học máy theo dõi hồ sơ di chuyển của nhân viên và khách để phát hiện các dấu hiệu đột nhập: ra vào ngoài giờ làm việc, quẹt thẻ ở 2 vị trí địa lý bất khả thi (Impossible Travel) hoặc đi theo sau (Tailgating).',
    specs: [
      { label: 'Mô hình AI phát hiện', value: 'Isolation Forest + UEBA' },
      { label: 'Cấp độ cảnh báo', value: 'Info / Warning / Critical' },
      { label: 'Phản ứng tự động', value: 'Khóa cửa & Báo động SOC' },
      { label: 'Xuất báo cáo', value: 'SIEM & Audit Trail' }
    ]
  }
]

function filteredFeatures() {
  if (activeCategory.value === 'all') return features
  return features.filter(f => f.category === activeCategory.value)
}

function triggerSfx() {
  window.dispatchEvent(new CustomEvent('portal-click-sfx'))
}
</script>

<template>
  <div class="py-12 lg:py-16">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-12">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 rounded-full border border-cyan-500/30 bg-cyan-950/40 px-3 py-1 text-xs font-bold text-cyan-300 font-mono">
          <span>⚡ HỆ SINH THÁI CÔNG NGHỆ 6 TRỤ CỘT</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-extrabold tracking-tight text-slate-100 font-mono">
          Tính Năng & Kiến Trúc An Ninh
        </h1>
        <p class="mx-auto max-w-2xl text-xs sm:text-sm text-slate-400 leading-relaxed">
          Được thiết kế theo tiêu chuẩn an ninh cấp doanh nghiệp, V-Shield 2.0 cung cấp khả năng tự động hóa phòng thủ toàn diện từ rìa (Edge IoT) đến đám mây (Cloud).
        </p>
      </div>

      <!-- Category Filter Tabs -->
      <div class="flex flex-wrap items-center justify-center gap-2">
        <button
          v-for="cat in categories"
          :key="cat.id"
          type="button"
          @click="activeCategory = cat.id; triggerSfx()"
          class="rounded-xl px-4 py-2 text-xs font-bold uppercase tracking-wider transition-all"
          :class="[
            activeCategory === cat.id
              ? 'bg-gradient-to-r from-cyan-500 to-pink-500 text-slate-950 shadow-[0_0_20px_rgba(0,240,255,0.4)]'
              : 'border border-slate-800 bg-slate-900/60 text-slate-400 hover:border-cyan-500/40 hover:text-cyan-300'
          ]"
        >
          {{ cat.name }}
        </button>
      </div>

      <!-- Feature Grid -->
      <div class="grid grid-cols-1 gap-8 md:grid-cols-2">
        <div
          v-for="item in filteredFeatures()"
          :key="item.id"
          class="relative rounded-3xl border border-slate-800 bg-slate-900/70 p-6 sm:p-8 backdrop-blur-xl transition-all duration-300 hover:border-cyan-500/50 hover:shadow-[0_0_30px_rgba(0,240,255,0.2)]"
        >
          <!-- Tag -->
          <div class="flex items-center justify-between">
            <span class="rounded-lg border border-cyan-500/30 bg-cyan-950/60 px-2.5 py-1 text-[10px] font-black uppercase tracking-wider text-cyan-300 font-mono">
              {{ item.tag }}
            </span>
            <span class="font-mono text-xs font-bold text-slate-500">
              FEATURE #0{{ item.id }}
            </span>
          </div>

          <!-- Title -->
          <h3 class="mt-4 text-xl font-extrabold text-slate-100 font-mono">
            {{ item.title }}
          </h3>

          <!-- Description -->
          <p class="mt-3 text-xs leading-relaxed text-slate-300">
            {{ item.desc }}
          </p>

          <!-- Tech Specs Breakdown Grid -->
          <div class="mt-6 grid grid-cols-2 gap-3 border-t border-slate-800/80 pt-5">
            <div
              v-for="(spec, sIdx) in item.specs"
              :key="sIdx"
              class="rounded-xl border border-slate-800/60 bg-slate-950/50 p-2.5"
            >
              <div class="text-[10px] text-slate-400">{{ spec.label }}</div>
              <div class="mt-0.5 font-mono text-xs font-bold text-cyan-300">{{ spec.value }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
