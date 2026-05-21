<template>
  <div class="page">
    <div class="topbar">
      <div>
        <h1>V-Shield QR Walk-in Monitor</h1>
        <p>Kiểm soát ra vào đi bộ - Quét QR và gọi API QrAccess Controller mới</p>
      </div>
    </div>

    <div class="lane-grid">
      <section
        v-for="term in terminals"
        :key="term.id"
        class="lane-card"
        :class="{ ready: term.sessionLocked }"
      >
        <div class="lane-head">
          <div>
            <h2>{{ term.name }}</h2>
            <p>{{ term.desc }}</p>
          </div>
          <div class="lane-final-status" :class="statusPillClass(term)">
            {{ statusPillText(term) }}
          </div>
        </div>

        <div class="lane-actions">
          <button class="btn btn-main" :disabled="term.loading || !term.cameraIp" @click="startScanner(term)">
            {{ term.loading ? "Đang xử lý..." : "Mở Camera & Quét" }}
          </button>
          <button class="btn btn-off" :disabled="term.loading || !term.previewRunning" @click="stopScanner(term)">
            Tắt Camera
          </button>
        </div>

        <div class="ip-row" style="grid-template-columns: 1fr 1fr;">
          <div class="ip-box">
            <label>Camera QR (ID: {{ term.cameraId || 'Trống' }})</label>
            <div class="search-box">
              <input v-model="cameraSearch[term.id]" placeholder="Tìm camera..." :disabled="term.loading" />
              <div class="dropdown" v-if="cameraSearch[term.id]">
                <div v-for="cam in filterCameras(cameraSearch[term.id])" :key="cam.cameraId" 
                     @click="selectCamera(cam, term)" class="dropdown-item">
                  {{ cam.cameraName }} (ID: {{ cam.cameraId }})
                </div>
              </div>
            </div>
          </div>
          <div class="ip-box">
            <label>Mật khẩu Xác thực Đổi Cam</label>
            <input type="password" v-model="term.userPassword" placeholder="Nhập pass tài khoản..." :disabled="term.loading" />
          </div>
        </div>

        <div class="summary-bar" style="grid-template-columns: 1fr 1fr;">
          <div class="summary-item">
            <span class="label">ID Người dùng</span>
            <span class="value strong">{{ term.verifiedId || "-----" }}</span>
          </div>
          <div class="summary-item">
            <span class="label">Trạng thái quyền</span>
            <span class="value" :class="term.alert ? 'danger-text' : 'ok-text'">
              {{ term.verifyMessage || "ĐANG CHỜ" }}
            </span>
          </div>
        </div>

        <div class="camera-stack">
          <div class="cam-block">
            <div class="cam-head">
              <span>Luồng Camera WebRTC</span>
            </div>

            <div class="cam-preview">
              <iframe
                v-if="term.previewRunning && term.viewUrl"
                :key="term.previewKey"
                :src="term.viewUrl"
                class="preview-image"
                style="border: none;"
                :ref="el => setVideoRef(term.id, el)"
              ></iframe>
              <div v-else class="cam-off">Offline</div>
              
              <canvas :ref="el => setCanvasRef(term.id, el)" style="display:none;"></canvas>
            </div>
            
            <div class="bottom-note">
              <span><b>Payload QR bắt được:</b> {{ shortText(term.qrPayload) }}</span>
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<script>
import jsQR from "jsqr";
import axios from "axios";
import { getCameras } from "../services/setcamAPI"; // Gọi API lấy cam như bản cũ

export default {
  name: "QrAccessMonitor",
  data() {
    return {
      cameras: [],
      cameraSearch: {},
      canvasRefs: {},
      // Thay vì Lane (biển số), ta gọi là Terminals (cổng quét bộ)
      terminals: [
        {
          id: "term1",
          name: "Chốt Đi Bộ 1",
          desc: "Quét QR kiểm tra quyền Access",
          loading: false,
          
          cameraIp: "",
          viewUrl: "",
          cameraId: null,
          userPassword: "", // Pass để gửi lên backend

          previewRunning: false,
          previewKey: 0,
          previewTimer: null,
          isDecoding: false,

          sessionLocked: false, // Khóa tạm khi đang call API tránh spam
          qrPayload: "",
          verifiedId: "",
          verifyMessage: "",
          alert: false
        }
      ]
    };
  },

  async mounted() {
    await this.loadCameraList();
  },

  beforeUnmount() {
    this.terminals.forEach(term => this.stopScanner(term));
  },

  methods: {
    setCanvasRef(id, el) { if (el) this.canvasRefs[id] = el; },
    setVideoRef(id, el) { /* Iframe không trích iframe.contentWindow được nếu khác domain, cần cấu hình CORS */ },

    async loadCameraList() {
      try {
        this.cameras = await getCameras();
      } catch (e) {
        console.error("loadCameraList error", e);
      }
    },

    filterCameras(keyword) {
      if (!keyword) return this.cameras;
      const key = keyword.toLowerCase();
      return this.cameras.filter(c => 
        String(c.cameraName || "").toLowerCase().includes(key) || 
        String(c.cameraId).includes(key)
      );
    },

    selectCamera(cam, term) {
      if (!cam.urlView) {
        alert("Camera chưa có UrlView.");
        return;
      }
      term.cameraIp = cam.streamUrl;
      term.viewUrl = cam.urlView;
      term.cameraId = cam.cameraId;
      this.cameraSearch[term.id] = cam.cameraName;
    },

    startScanner(term) {
      if (!term.cameraId || !term.userPassword) {
        alert("Vui lòng chọn Camera và nhập mật khẩu tài khoản.");
        return;
      }
      term.previewRunning = true;
      term.previewKey++;
      this.clearSession(term);
      
      // Bắt đầu vòng lặp chụp ảnh từ Iframe (Giả định Iframe cùng nguồn/CORS)
      this.startDecodingLoop(term);
    },

    stopScanner(term) {
      term.previewRunning = false;
      this.clearSession(term);
      if (term.previewTimer) {
        clearInterval(term.previewTimer);
        term.previewTimer = null;
      }
    },

    clearSession(term) {
      term.sessionLocked = false;
      term.qrPayload = "";
      term.verifiedId = "";
      term.verifyMessage = "";
      term.alert = false;
    },

    startDecodingLoop(term) {
      if (term.previewTimer) clearInterval(term.previewTimer);
      
      term.previewTimer = setInterval(async () => {
        if (!term.previewRunning || term.sessionLocked || term.isDecoding) return;
        await this.captureAndDecode(term);
      }, 500); // Mỗi nửa giây quét 1 lần
    },

    async captureAndDecode(term) {
      term.isDecoding = true;
      try {
        // LƯU Ý: Đoạn này giả định iframe chứa ảnh có thể trích xuất canvas.
        // Trên thực tế nếu dùng RTSP -> go2rtc -> iframe, bạn có thể phải dùng mjpeg img_src thay vì iframe để bắt canvas 
        // Code minh hoạ Logic:
        const canvas = this.canvasRefs[term.id];
        if (!canvas) return;

        // --- NẾU THAY IFRAME BẰNG THE <IMG> MJPEG STREAM ---
        // const img = document.querySelector(`#img-${term.id}`);
        // ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
        
        // GIẢ LẬP: Phát hiện được QR từ jsQR
        // const code = jsQR(imageData.data, w, h);
        const dummyCode = null; // Đặt đoạn jsQR của bạn vào đây

        if (dummyCode && dummyCode.data) {
          term.sessionLocked = true; // Khóa loop để ko bắn API liên tục
          term.qrPayload = dummyCode.data;
          await this.callApiScanAccess(term, term.qrPayload);
        }
      } catch (e) {
        // ignore
      } finally {
        term.isDecoding = false;
      }
    },

    async callApiScanAccess(term, payload) {
      term.loading = true;
      try {
        // Gọi thẳng vào API backend mới hoàn thiện
        const reqData = {
          QrPayload: payload,
          CameraId: term.cameraId,
          UserPassword: term.userPassword,
          LoggedInUserId: 1 // TODO: Thay bằng ID user đang đăng nhập thực tế (hoặc token)
        };

        const res = await axios.post("/api/QrAccess/scan-access", reqData);
        
        if (res.data.success) {
          term.alert = false;
          term.verifiedId = res.data.data.employeeId || res.data.data.visitorDetailId || "OK";
          term.verifyMessage = res.data.message;
        } else {
          term.alert = true;
          term.verifyMessage = res.data.message;
        }
      } catch (err) {
        term.alert = true;
        term.verifyMessage = err.response?.data?.message || err.message || "Lỗi kết nối";
      } finally {
        term.loading = false;
        // Tự động mở khóa scan sau 3 giây để người tiếp theo vào
        setTimeout(() => {
          this.clearSession(term);
        }, 3000);
      }
    },

    statusPillText(term) {
      if (!term.previewRunning) return "OFFLINE";
      if (term.sessionLocked) return term.alert ? "TỪ CHỐI" : "ĐÃ CHO QUA";
      return "ĐANG QUÉT MÃ";
    },

    statusPillClass(term) {
      if (!term.previewRunning) return "wait";
      if (term.sessionLocked) return term.alert ? "danger" : "ok";
      return "neutral";
    },

    shortText(val) {
      if (!val) return "-----";
      return val.length > 50 ? val.substring(0, 50) + "..." : val;
    }
  }
};
</script>

<style scoped>
/* Tái sử dụng CSS cũ */
.page { min-height: 100vh; background: #f3f6fb; padding: 20px; font-family: Inter, Arial, sans-serif; color: #0f172a; }
.topbar h1 { margin: 0; font-size: 28px; font-weight: 800; }
.topbar p { margin: 6px 0 0; color: #64748b; font-size: 14px; }
.lane-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 18px; }
.lane-card { background: #ffffff; border: 1px solid #e2e8f0; border-radius: 18px; padding: 16px; box-shadow: 0 8px 24px rgba(15, 23, 42, 0.06); }
.lane-card.ready { border-color: #93c5fd; }
.lane-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; }
.lane-head h2 { margin: 0; font-size: 22px; font-weight: 800; }
.lane-final-status { min-width: 150px; text-align: center; padding: 8px 12px; border-radius: 999px; font-size: 12px; font-weight: 900; }
.lane-final-status.ok { background: #dcfce7; color: #166534; }
.lane-final-status.wait { background: #fff7ed; color: #c2410c; }
.lane-final-status.danger { background: #fee2e2; color: #b91c1c; }
.lane-final-status.neutral { background: #e2e8f0; color: #1e293b; }
.lane-actions { display: flex; gap: 8px; margin-bottom: 14px; }
.btn { height: 40px; border: none; border-radius: 10px; padding: 0 14px; color: white; font-size: 13px; font-weight: 800; cursor: pointer; }
.btn-main { background: #2563eb; }
.btn-off { background: #dc2626; }
.btn:disabled { opacity: 0.6; cursor: not-allowed; }
.ip-row { display: grid; gap: 10px; margin-bottom: 14px; }
.ip-box label { display: block; font-size: 12px; font-weight: 700; margin-bottom: 6px; color: #334155; }
.ip-box input { width: 100%; height: 42px; border: 1px solid #cbd5e1; border-radius: 10px; padding: 0 12px; font-size: 14px; outline: none; }
.summary-bar { display: grid; gap: 10px; margin-bottom: 14px; }
.summary-item { background: #f8fafc; border: 1px solid #e9eef5; border-radius: 12px; padding: 10px; }
.summary-item .label { display: block; font-size: 11px; color: #64748b; margin-bottom: 6px; }
.summary-item .value { font-size: 15px; font-weight: 800; }
.ok-text { color: #15803d; }
.danger-text { color: #b91c1c; }
.cam-preview { width: 100%; aspect-ratio: 16/9; background: #0f172a; border-radius: 12px; overflow: hidden; margin-bottom: 10px; position: relative;}
.preview-image { width: 100%; height: 100%; object-fit: contain; display: block; }
.cam-off { width: 100%; height: 100%; display: flex; color: #cbd5e1; align-items: center; justify-content: center; font-size: 18px; font-weight: 700; }
.search-box { position: relative; }
.dropdown { position: absolute; background: white; border: 1px solid #ccc; width: 100%; max-height: 200px; overflow-y: auto; z-index: 9999; }
.dropdown-item { padding: 8px; cursor: pointer; }
.dropdown-item:hover { background: #eee; }
.bottom-note { font-size: 12px; color: #475569; margin-top: 10px;}
</style>