<template>
  <div class="qrm-page">
    <div class="qrm-topbar">
      <div>
        <h1>V-Shield QR Walk-in Monitor</h1>
        <p>Kiem soat vao/ra bang QR cho luong di bo.</p>
      </div>
      <button class="qrm-settings-btn" type="button" @click="showSettings = true">
        Cai dat
      </button>
    </div>

    <section v-for="term in terminals" :key="term.id" class="qrm-card" :class="{ ready: term.sessionLocked }">
      <div class="qrm-card-head">
        <div>
          <div class="qrm-kicker">Station</div>
          <h2>{{ term.name }}</h2>
          <p>{{ term.desc }}</p>
        </div>
        <div class="qrm-status-pill" :class="statusPillClass(term)">
          {{ statusPillText(term) }}
        </div>
      </div>

      <div class="qrm-actions">
        <button class="qrm-btn qrm-btn-main" :disabled="term.loading || !term.cameraIp" @click="startScanner(term)">
          {{ term.loading ? "Dang xu ly..." : "Mo Camera & Quet" }}
        </button>
        <button class="qrm-btn qrm-btn-off" :disabled="term.loading || !term.previewRunning" @click="stopScanner(term)">
          Tat Camera
        </button>
      </div>

      <div class="qrm-form-grid">
        <div class="qrm-field">
          <label>Camera QR (ID: {{ term.cameraId || 'Trong' }})</label>
          <div class="search-box">
            <input v-model="cameraSearch[term.id]" placeholder="Tim camera..." :disabled="term.loading" />
            <div class="dropdown" v-if="cameraSearch[term.id]">
              <div
                v-for="cam in filterCameras(cameraSearch[term.id])"
                :key="cam.cameraId"
                @click="selectCamera(cam, term)"
                class="dropdown-item"
              >
                {{ cam.cameraName }} (ID: {{ cam.cameraId }})
              </div>
            </div>
          </div>
        </div>
        <div class="qrm-field">
          <label>Mat khau xac thuc doi cam</label>
          <input type="password" v-model="term.userPassword" placeholder="Nhap pass tai khoan..." :disabled="term.loading" />
        </div>
      </div>

      <div class="qrm-summary-grid">
        <div class="summary-item">
          <span class="label">ID nguoi dung</span>
          <span class="value strong">{{ term.verifiedId || "-----" }}</span>
        </div>
        <div class="summary-item">
          <span class="label">Trang thai quyen</span>
          <span class="value" :class="term.alert ? 'danger-text' : 'ok-text'">
            {{ term.verifyMessage || "DANG CHO" }}
          </span>
        </div>
      </div>

      <div class="qrm-preview-wrap">
        <div class="qrm-preview-head">
          <span>Camera Preview</span>
          <span class="preview-badge">{{ term.previewRunning ? "Preview ON" : "Preview OFF" }}</span>
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
          <div v-else class="cam-off">QR Offline</div>
          <canvas :ref="el => setCanvasRef(term.id, el)" style="display:none;"></canvas>
        </div>
      </div>

      <div class="bottom-note">
        <span><b>Payload QR:</b> {{ shortText(term.qrPayload) }}</span>
      </div>
    </section>

    <div v-if="showSettings" class="qrm-drawer-mask" @click="showSettings = false">
      <aside class="qrm-drawer" @click.stop>
        <div class="qrm-drawer-head">
          <h3>Cai dat</h3>
          <button type="button" class="qrm-drawer-close" @click="showSettings = false">Dong</button>
        </div>

        <div class="qrm-setting-row">
          <div class="qrm-setting-copy">
            <div class="qrm-setting-name">Python doc QR</div>
            <div class="qrm-setting-desc">Bat/tat service quet QR backend</div>
          </div>
          <button
            type="button"
            class="toggle-switch"
            :class="toggleSwitchClass('python_qr', runtimeRunning('python_qr'))"
            role="switch"
            :aria-checked="runtimeRunning('python_qr')"
            :disabled="runtimeIsBusy('python_qr')"
            @click="toggleRuntime('python_qr')"
          >
            <span class="toggle-switch-knob" aria-hidden="true"></span>
          </button>
        </div>

        <div class="qrm-setting-row">
          <div class="qrm-setting-copy">
            <div class="qrm-setting-name">AutoStart Python QR</div>
            <div class="qrm-setting-desc">Tu dong bat khi he thong khoi dong</div>
          </div>
          <button
            type="button"
            class="auto-start-btn"
            :disabled="runtimeIsBusy('python_qr') || !runtimeEnabled('python_qr')"
            @click="toggleRuntimeAutoStart('python_qr')"
          >
            AutoStart: {{ runtimeAutoStart('python_qr') ? 'ON' : 'OFF' }}
          </button>
        </div>

        <button type="button" class="qrm-refresh-btn" :disabled="runtimeLoading" @click="fetchRuntimeServices">
          {{ runtimeLoading ? 'Dang tai...' : 'Lam moi trang thai' }}
        </button>
      </aside>
    </div>
  </div>
</template>

<script>
import axios from "axios";
import { getCameras } from "../services/cameraRuntimeApi";
import { getRuntimeServices, startRuntimeService, stopRuntimeService, updateRuntimeService } from "../services/runtimeServiceApi";
import { startQrScanner, resetQrSession, stopQrScanner, getQrScanResult, scanQrOnce } from "../services/dynamicQrScannerApi";

export default {
  name: "QrAccessMonitor",
  data() {
    return {
      cameras: [],
      cameraSearch: {},
      canvasRefs: {},
      showSettings: false,
      runtimeServices: [],
      runtimeBusy: {},
      runtimeLoading: false,
      terminals: [
        {
          id: "term1",
          name: "Chot di bo 1",
          desc: "Quet QR kiem tra quyen Access",
          loading: false,
          cameraIp: "",
          viewUrl: "",
          cameraId: null,
          userPassword: "",
          previewRunning: false,
          previewKey: 0,
          previewTimer: null,
          resultTimer: null,
          isDecoding: false,
          sessionLocked: false,
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
    await this.fetchRuntimeServices();
  },

  beforeUnmount() {
    this.terminals.forEach((term) => {
      this.stopScanner(term);
    });
  },

  methods: {
    setCanvasRef(id, el) {
      if (el) this.canvasRefs[id] = el;
    },
    setVideoRef() {},

    runtimeState(name) {
      return (this.runtimeServices || []).find((item) => item?.name === name) || null;
    },

    runtimeRunning(name) {
      return !!this.runtimeState(name)?.running;
    },

    runtimeAutoStart(name) {
      return !!this.runtimeState(name)?.autoStart;
    },

    runtimeEnabled(name) {
      const state = this.runtimeState(name);
      return state ? state.enabled !== false : false;
    },

    runtimeIsBusy(name) {
      return !!this.runtimeBusy[name];
    },

    toggleSwitchClass(name, isOn) {
      return {
        on: !!isOn,
        pending: this.runtimeIsBusy(name)
      };
    },

    async fetchRuntimeServices() {
      this.runtimeLoading = true;
      try {
        this.runtimeServices = await getRuntimeServices();
      } catch (e) {
        console.error("fetchRuntimeServices error", e);
      } finally {
        this.runtimeLoading = false;
      }
    },

    async toggleRuntime(name) {
      if (this.runtimeIsBusy(name)) return;
      this.runtimeBusy = { ...this.runtimeBusy, [name]: true };
      try {
        if (this.runtimeRunning(name)) {
          await stopRuntimeService(name);
        } else {
          await startRuntimeService(name);
        }
        await this.fetchRuntimeServices();
      } catch (e) {
        console.error("toggleRuntime error", e);
        alert(e?.response?.data?.message || e?.message || "Khong the bat/tat runtime.");
      } finally {
        this.runtimeBusy = { ...this.runtimeBusy, [name]: false };
      }
    },

    async toggleRuntimeAutoStart(name) {
      if (this.runtimeIsBusy(name)) return;
      this.runtimeBusy = { ...this.runtimeBusy, [name]: true };
      try {
        await updateRuntimeService(name, { autoStart: !this.runtimeAutoStart(name) });
        await this.fetchRuntimeServices();
      } catch (e) {
        console.error("toggleRuntimeAutoStart error", e);
        alert(e?.response?.data?.message || e?.message || "Khong the doi AutoStart.");
      } finally {
        this.runtimeBusy = { ...this.runtimeBusy, [name]: false };
      }
    },

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
      return this.cameras.filter((c) =>
        String(c.cameraName || "").toLowerCase().includes(key) ||
        String(c.cameraId).includes(key)
      );
    },

    selectCamera(cam, term) {
      if (!cam.urlView) {
        alert("Camera chua co UrlView.");
        return;
      }
      term.cameraIp = cam.streamUrl;
      term.viewUrl = cam.urlView;
      term.cameraId = cam.cameraId;
      this.cameraSearch[term.id] = cam.cameraName;
    },

    async startScanner(term) {
      if (!term.cameraId || !term.userPassword) {
        alert("Vui long chon camera va nhap mat khau tai khoan.");
        return;
      }
      if (!String(term.cameraIp || "").trim() || !String(term.viewUrl || "").trim()) {
        alert("Camera chua co URL stream/view. Hay chon lai camera co du RTSP va UrlView.");
        return;
      }
      try {
        term.loading = true;
        if (!this.runtimeRunning("python_qr")) {
          await startRuntimeService("python_qr");
          await this.fetchRuntimeServices();
        }

        await startQrScanner(term.cameraIp);
        await resetQrSession();
        await scanQrOnce();

        term.previewRunning = true;
        term.previewKey++;
        this.clearSession(term);
        this.startResultPolling(term);
      } catch (e) {
        alert(e?.message || "Khong the mo scanner Python. Kiem tra python_qr trong Cai dat va cong API.");
      } finally {
        term.loading = false;
      }
    },

    async stopScanner(term) {
      term.previewRunning = false;
      this.clearSession(term);
      this.stopResultPolling(term);
      try {
        await stopQrScanner();
      } catch (e) {
        console.warn("stopQrScanner warning:", e);
      }
    },

    clearSession(term) {
      term.sessionLocked = false;
      term.qrPayload = "";
      term.verifiedId = "";
      term.verifyMessage = "";
      term.alert = false;
    },

    startResultPolling(term) {
      this.stopResultPolling(term);
      term.resultTimer = setInterval(async () => {
        if (!term.previewRunning || term.loading) return;
        await this.pullQrResult(term);
      }, 350);
    },

    stopResultPolling(term) {
      if (term.resultTimer) {
        clearInterval(term.resultTimer);
        term.resultTimer = null;
      }
    },

    async pullQrResult(term) {
      try {
        const res = await getQrScanResult();
        if (!res) return;

        if (res.locked && res.qr) {
          term.sessionLocked = true;
          term.qrPayload = String(res.qr || "");
          this.stopResultPolling(term);
          await this.callApiScanAccess(term, term.qrPayload);
          return;
        }
      } catch (e) {
        console.warn("getQrScanResult warning:", e?.message || e);
      }
    },

    async callApiScanAccess(term, payload) {
      term.loading = true;
      try {
        const reqData = {
          QrPayload: payload,
          CameraId: term.cameraId,
          UserPassword: term.userPassword,
          LoggedInUserId: 1
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
        term.verifyMessage = err.response?.data?.message || err.message || "Loi ket noi";
      } finally {
        term.loading = false;
        setTimeout(() => {
          this.clearSession(term);
          if (term.previewRunning) {
            this.startResultPolling(term);
            scanQrOnce().catch(() => {});
          }
        }, 3000);
      }
    },

    statusPillText(term) {
      if (!term.previewRunning) return "OFFLINE";
      if (term.sessionLocked) return term.alert ? "TU CHOI" : "DA CHO QUA";
      return "DANG QUET MA";
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
.qrm-page { min-height: calc(100vh - 20px); padding: 16px; }
.qrm-topbar { margin-top: 6px; margin-bottom: 10px; }
.qrm-topbar { display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; }
.qrm-topbar h1 { margin: 0; font-size: clamp(30px, 4vw, 44px); font-weight: 900; line-height: 1.1; letter-spacing: -0.02em; }
.qrm-topbar p { margin: 8px 0 0; color: #64748b; font-size: 16px; }
.qrm-settings-btn {
  height: 40px;
  padding: 0 14px;
  border-radius: 10px;
  border: 1px solid #cbd5e1;
  background: #fff;
  color: #334155;
  font-weight: 800;
  cursor: pointer;
}

.qrm-card {
  width: min(1280px, 100%);
  margin: 0 auto;
  background: #ffffff;
  border: 1px solid #dde6f0;
  border-radius: 20px;
  padding: 16px;
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08);
}
.qrm-card.ready { border-color: #9ac8ff; }

.qrm-card-head { display: flex; justify-content: space-between; align-items: center; gap: 10px; margin-bottom: 12px; }
.qrm-kicker { font-size: 12px; font-weight: 800; letter-spacing: .08em; color: #6b7f96; text-transform: uppercase; }
.qrm-card-head h2 { margin: 4px 0 0; font-size: clamp(24px, 3vw, 32px); font-weight: 900; }
.qrm-card-head p { margin: 4px 0 0; color: #5f7188; font-size: 15px; }

.qrm-status-pill { min-width: 170px; text-align: center; padding: 10px 14px; border-radius: 999px; font-size: 13px; font-weight: 900; }
.qrm-status-pill.ok { background: #dcfce7; color: #166534; }
.qrm-status-pill.wait { background: #fff7ed; color: #c2410c; }
.qrm-status-pill.danger { background: #fee2e2; color: #b91c1c; }
.qrm-status-pill.neutral { background: #e2e8f0; color: #1e293b; }

.qrm-actions { display: flex; gap: 10px; margin-bottom: 12px; }
.qrm-btn { height: 44px; border: none; border-radius: 12px; padding: 0 16px; color: white; font-size: 14px; font-weight: 800; cursor: pointer; }
.qrm-btn-main { background: #6a8fe8; }
.qrm-btn-off { background: #e57f7f; }
.qrm-btn:disabled { opacity: 0.6; cursor: not-allowed; }

.qrm-form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; margin-bottom: 12px; }
.qrm-field label { display: block; font-size: 12px; font-weight: 800; margin-bottom: 6px; color: #2e4159; text-transform: uppercase; letter-spacing: .03em; }
.qrm-field input {
  width: 100%;
  height: 44px;
  border: 1px solid #c5d4e6;
  border-radius: 12px;
  padding: 0 12px;
  font-size: 15px;
  outline: none;
  background: #f8fbff;
}

.qrm-summary-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; margin-bottom: 12px; }
.summary-item { background: #f7fafd; border: 1px solid #e3edf7; border-radius: 12px; padding: 12px; }
.summary-item .label { display: block; font-size: 11px; color: #728399; margin-bottom: 6px; text-transform: uppercase; font-weight: 700; letter-spacing: .04em; }
.summary-item .value { font-size: 24px; font-weight: 900; }
.ok-text { color: #15803d; }
.danger-text { color: #b91c1c; }

.qrm-preview-wrap { max-width: 860px; margin: 0 auto; }
.qrm-preview-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; font-weight: 800; color: #12233b; }
.preview-badge { background: #fff7ed; color: #c2410c; border-radius: 999px; padding: 6px 10px; font-size: 12px; }
.cam-preview {
  width: 100%;
  aspect-ratio: 16/10;
  background: #07163b;
  border-radius: 14px;
  border: 2px solid #27497f;
  overflow: hidden;
  position: relative;
}
.preview-image { width: 100%; height: 100%; object-fit: contain; display: block; }
.cam-off { width: 100%; height: 100%; display: flex; color: #d4e2ff; align-items: center; justify-content: center; font-size: 30px; font-weight: 800; }

.search-box { position: relative; }
.dropdown {
  position: absolute;
  background: #fff;
  border: 1px solid #c6d5e8;
  border-radius: 10px;
  width: 100%;
  max-height: 220px;
  overflow-y: auto;
  z-index: 9999;
  box-shadow: 0 12px 24px rgba(15, 23, 42, 0.12);
}
.dropdown-item { padding: 10px; cursor: pointer; font-size: 14px; }
.dropdown-item:hover { background: #edf4ff; }

.bottom-note {
  margin-top: 10px;
  padding: 10px 12px;
  border-radius: 10px;
  background: #f8fbff;
  border: 1px solid #e2ebf6;
  font-size: 13px;
  color: #475569;
}

.qrm-drawer-mask {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.35);
  z-index: 200;
  display: flex;
  justify-content: flex-end;
}
.qrm-drawer {
  width: min(420px, 92vw);
  height: 100%;
  background: #ffffff;
  box-shadow: -12px 0 30px rgba(15, 23, 42, 0.18);
  padding: 16px;
}
.qrm-drawer-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}
.qrm-drawer-head h3 {
  margin: 0;
  font-size: 20px;
  font-weight: 900;
}
.qrm-drawer-close {
  border: 1px solid #cbd5e1;
  background: #fff;
  border-radius: 8px;
  padding: 6px 10px;
  font-weight: 700;
  cursor: pointer;
}
.qrm-setting-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 12px;
  margin-bottom: 10px;
}
.qrm-setting-name {
  font-weight: 900;
  color: #0f172a;
}
.qrm-setting-desc {
  margin-top: 4px;
  font-size: 13px;
  color: #64748b;
}
.toggle-switch {
  position: relative;
  flex-shrink: 0;
  width: 50px;
  height: 28px;
  border-radius: 999px;
  border: 2px solid #cbd5e1;
  background: #e2e8f0;
  cursor: pointer;
  padding: 0;
  transition: background 0.2s ease, border-color 0.2s ease;
}
.toggle-switch.on {
  background: #22c55e;
  border-color: #16a34a;
}
.toggle-switch.pending {
  background: #facc15;
  border-color: #eab308;
}
.toggle-switch:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
.toggle-switch-knob {
  position: absolute;
  top: 2px;
  left: 2px;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: #fff;
  box-shadow: 0 1px 4px rgba(15, 23, 42, 0.2);
  transition: transform 0.2s ease;
}
.toggle-switch.on .toggle-switch-knob {
  transform: translateX(22px);
}
.toggle-switch.pending .toggle-switch-knob {
  background: #fef08a;
}
.auto-start-btn {
  min-height: 30px;
  padding: 0 10px;
  border-radius: 999px;
  border: 1px solid #cbd5e1;
  background: #f8fafc;
  color: #334155;
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
}
.auto-start-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
.qrm-refresh-btn {
  width: 100%;
  height: 40px;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  background: #fff;
  font-weight: 800;
  cursor: pointer;
}

@media (max-width: 900px) {
  .qrm-form-grid, .qrm-summary-grid { grid-template-columns: 1fr; }
  .qrm-status-pill { min-width: 120px; }
  .qrm-topbar { flex-direction: column; align-items: stretch; }
}
</style>
