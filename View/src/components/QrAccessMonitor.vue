<template>
  <div class="qrm-page">
    <div class="qrm-topbar">
      <div>
        <h1>V-Shield QR Walk-in Monitor</h1>
        <p>Kiem soat vao/ra bang QR cho luong di bo.</p>
      </div>
      <button class="qrm-settings-btn" type="button" @click="showSettings = true">Cai dat</button>
    </div>

    <section v-for="term in terminals" :key="term.id" class="qrm-card">
      <div class="qrm-card-head">
        <div>
          <div class="qrm-kicker">Station</div>
          <h2>{{ term.name }}</h2>
          <p>{{ term.desc }}</p>
        </div>
        <div class="qrm-status-pill" :class="statusPillClass(term)">{{ statusPillText(term) }}</div>
      </div>

      <div class="qrm-actions">
        <button class="qrm-btn qrm-btn-main" :disabled="term.loading || !term.cameraIp" @click="startScanner(term)">
          {{ term.loading ? "Dang xu ly..." : "Quet 1 lan" }}
        </button>
        <button class="qrm-btn qrm-btn-off" :disabled="term.loading || !term.previewRunning" @click="stopScanner(term)">Tat Camera</button>
      </div>

      <div class="qrm-form-grid">
        <div class="qrm-field">
          <label>Camera QR (ID: {{ term.cameraId || 'Trong' }})</label>
          <div class="search-box">
            <input v-model="cameraSearch[term.id]" placeholder="Tim camera..." :disabled="term.loading" />
            <div class="dropdown" v-if="cameraSearch[term.id]">
              <div
                v-for="cam in filterCameras(cameraSearch[term.id]).slice(0, 5)"
                :key="cam.cameraId"
                @click="onChooseCamera(cam, term)"
                class="dropdown-item"
              >
                {{ cam.cameraName }} (ID: {{ cam.cameraId }})
              </div>
            </div>
          </div>
          <div class="camera-verified" :class="term.cameraVerified ? 'ok' : 'warn'">
            {{ term.cameraVerified ? 'Camera da xac thuc' : 'Chua xac thuc camera' }}
          </div>
        </div>
      </div>

      <div class="qrm-preview-wrap">
        <div class="qrm-preview-head">
          <span>Camera Preview</span>
          <span class="preview-badge">{{ term.previewRunning ? "Preview ON" : "Preview OFF" }}</span>
        </div>

        <div class="cam-preview" :class="previewStateClass(term)">
          <iframe
            v-if="term.previewRunning && term.viewUrl"
            :key="term.previewKey"
            :src="term.viewUrl"
            class="preview-image"
            style="border: none;"
            :ref="el => setVideoRef(term.id, el)"
          ></iframe>
          <div v-else class="cam-off">QR Offline</div>

          <div v-if="term.identityLabel" class="id-overlay" :class="term.permissionState">
            {{ term.identityLabel }}
          </div>

          <div class="scan-overlay" v-if="term.previewRunning && term.permissionState === 'scanning'">
            Dang quet QR...
          </div>
        </div>
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

    <div v-if="authModal.open" class="auth-mask" @click="closeAuthModal">
      <div class="auth-dialog" @click.stop>
        <h3>Xac thuc doi camera</h3>
        <p>{{ authModal.cameraName }} (ID: {{ authModal.cameraId }})</p>
        <input
          v-model="authModal.password"
          type="password"
          placeholder="Nhap mat khau tai khoan..."
          @keyup.enter="confirmCameraAuth"
        />
        <div class="auth-error" v-if="authModal.error">{{ authModal.error }}</div>
        <div class="auth-actions">
          <button type="button" class="qrm-btn qrm-btn-off" :disabled="authModal.loading" @click="closeAuthModal">Huy</button>
          <button type="button" class="qrm-btn qrm-btn-main" :disabled="authModal.loading" @click="confirmCameraAuth">
            {{ authModal.loading ? 'Dang kiem tra...' : 'Xac nhan' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from "axios";
import { getCameras } from "../services/cameraRuntimeApi";
import { getRuntimeServices, startRuntimeService, stopRuntimeService, updateRuntimeService } from "../services/runtimeServiceApi";
import { startQrScanner, resetQrSession, stopQrScanner, getQrScanResult, scanQrOnce } from "../services/dynamicQrScannerApi";
import { authState } from "../stores/auth";

const SCAN_TIMEOUT_MS = 6500;
const DUPLICATE_SUPPRESS_MS = 1800;
const HOLD_RELEASE_EMPTY_POLLS = 4;
const HOLD_MAX_MS = 2200;
const IDLE_DOWNGRADE_MS = 25000;
const NORMAL_POLL_MS = 300;
const LOW_POLL_MS = 900;
const NORMAL_SESSION_DELAY_MS = 0;
const LOW_SESSION_DELAY_MS = 1400;

export default {
  name: "QrAccessMonitor",
  data() {
    return {
      cameras: [],
      cameraSearch: {},
      showSettings: false,
      runtimeServices: [],
      runtimeBusy: {},
      runtimeLoading: false,
      authModal: {
        open: false,
        termId: "",
        cameraId: null,
        cameraName: "",
        cameraIp: "",
        viewUrl: "",
        password: "",
        loading: false,
        error: ""
      },
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
          cameraVerified: false,
          previewRunning: false,
          previewKey: 0,
          resultTimer: null,
          sessionTimer: null,
          sessionLocked: false,
          scanSessionActive: false,
          qrPayload: "",
          verifiedId: "",
          verifiedName: "",
          verifiedType: "",
          verifyMessage: "",
          permissionState: "idle",
          identityLabel: "",
          resultResetTimer: null,
          traceCounter: 0,
          activeTraceId: 0,
          lastPayload: "",
          lastPayloadAt: 0,
          perfMode: "normal",
          lastDetectedAt: 0,
          holdLocked: false,
          emptyPollStreak: 0,
          holdPayload: "",
          holdStartedAt: 0
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
    setVideoRef() {},
    getCurrentUserId() {
      const fromStore = Number(
        authState?.user?.userId ||
        authState?.user?.UserId ||
        authState?.user?.id ||
        0
      );
      if (fromStore > 0) return fromStore;

      try {
        const raw = localStorage.getItem("v_shield_user");
        if (raw) {
          const parsed = JSON.parse(raw);
          const fromStorage = Number(parsed?.userId || parsed?.UserId || parsed?.id || 0);
          if (fromStorage > 0) return fromStorage;
        }
      } catch {
        // ignore and fallback JWT parse
      }

      try {
        const token = localStorage.getItem("v_shield_token") || "";
        const parts = String(token).split(".");
        if (parts.length !== 3) return 0;
        const payloadBase64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
        const normalized = payloadBase64.padEnd(payloadBase64.length + ((4 - (payloadBase64.length % 4)) % 4), "=");
        const json = atob(normalized);
        const payload = JSON.parse(json);
        const fromSub = Number(payload?.sub || payload?.nameid || 0);
        return fromSub > 0 ? fromSub : 0;
      } catch {
        return 0;
      }
    },

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
      return { on: !!isOn, pending: this.runtimeIsBusy(name) };
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
        if (this.runtimeRunning(name)) await stopRuntimeService(name);
        else await startRuntimeService(name);
        await this.fetchRuntimeServices();
      } catch (e) {
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
      const key = String(keyword || "").toLowerCase();
      return this.cameras.filter((c) =>
        String(c.cameraName || "").toLowerCase().includes(key) ||
        String(c.cameraId || "").includes(key)
      );
    },

    onChooseCamera(cam, term) {
      if (!cam?.urlView || !cam?.streamUrl) {
        alert("Camera chua co du URL stream/view.");
        return;
      }
      this.authModal = {
        open: true,
        termId: term.id,
        cameraId: cam.cameraId,
        cameraName: cam.cameraName || "Camera",
        cameraIp: cam.streamUrl,
        viewUrl: cam.urlView,
        password: "",
        loading: false,
        error: ""
      };
    },

    closeAuthModal() {
      if (this.authModal.loading) return;
      this.authModal.open = false;
      this.authModal.error = "";
    },

    async confirmCameraAuth() {
      if (!this.authModal.open || !this.authModal.termId) return;
      if (!this.authModal.password.trim()) {
        this.authModal.error = "Vui long nhap mat khau.";
        return;
      }

      const term = this.terminals.find((x) => x.id === this.authModal.termId);
      if (!term) return;

      const userId = this.getCurrentUserId();
      if (userId <= 0) {
        this.authModal.error = "Khong tim thay user dang dang nhap.";
        return;
      }

      this.authModal.loading = true;
      this.authModal.error = "";
      try {
        await axios.post("/api/QrAccess/verify-camera-auth", {
          CameraId: this.authModal.cameraId,
          UserPassword: this.authModal.password,
          LoggedInUserId: userId
        });

        if (term.previewRunning) {
          await this.stopScanner(term);
        }

        term.cameraIp = this.authModal.cameraIp;
        term.viewUrl = this.authModal.viewUrl;
        term.cameraId = this.authModal.cameraId;
        term.userPassword = this.authModal.password;
        term.cameraVerified = true;
        term.permissionState = "idle";
        term.identityLabel = "";
        this.cameraSearch[term.id] = this.authModal.cameraName;
        this.authModal.open = false;
        this.authModal.error = "";
      } catch (e) {
        this.authModal.error = e?.response?.data?.message || e?.message || "Xac thuc that bai.";
      } finally {
        this.authModal.loading = false;
      }
    },

    async startScanner(term) {
      if (!term.cameraId || !term.cameraVerified) {
        alert("Vui long chon camera va xac thuc mat khau truoc khi mo.");
        return;
      }
      if (!String(term.cameraIp || "").trim() || !String(term.viewUrl || "").trim()) {
        alert("Camera chua co URL stream/view hop le.");
        return;
      }

      try {
        term.loading = true;
        if (!this.runtimeRunning("python_qr")) {
          await startRuntimeService("python_qr");
          await this.fetchRuntimeServices();
        }

        if (!term.previewRunning) {
          await startQrScanner(term.cameraIp);
          term.previewRunning = true;
          term.previewKey += 1;
        }

        this.clearScanState(term);
        this.ensureNormalPerf(term);
        term.lastDetectedAt = Date.now();
        this.startResultPolling(term);
        await this.runManualScan(term);
      } catch (e) {
        alert(e?.message || "Khong the mo scanner Python.");
      } finally {
        term.loading = false;
      }
    },

    async stopScanner(term) {
      term.previewRunning = false;
      this.stopResultPolling(term);
      this.stopSessionTimer(term);
      this.stopResultResetTimer(term);
      this.clearScanState(term);
      try {
        await stopQrScanner();
      } catch (e) {
        console.warn("stopQrScanner warning:", e);
      }
    },

    clearScanState(term) {
      term.scanSessionActive = false;
      term.sessionLocked = false;
      term.qrPayload = "";
      term.verifiedId = "";
      term.verifiedName = "";
      term.verifiedType = "";
      term.verifyMessage = "";
      term.permissionState = "idle";
      term.identityLabel = "";
      term.activeTraceId = 0;
      term.holdLocked = false;
      term.emptyPollStreak = 0;
      term.holdPayload = "";
      term.holdStartedAt = 0;
    },

    ensureNormalPerf(term) {
      if (term.perfMode !== "normal") {
        term.perfMode = "normal";
        this.startResultPolling(term);
      }
    },

    ensureLowPerf(term) {
      if (term.perfMode !== "low") {
        term.perfMode = "low";
        this.startResultPolling(term);
      }
    },

    pollIntervalMs(term) {
      return term.perfMode === "low" ? LOW_POLL_MS : NORMAL_POLL_MS;
    },

    nextSessionDelayMs(term) {
      return term.perfMode === "low" ? LOW_SESSION_DELAY_MS : NORMAL_SESSION_DELAY_MS;
    },

    applyAdaptivePerf(term) {
      if (!term.previewRunning) return;
      const lastDetectedAt = Number(term.lastDetectedAt || 0);
      if (!lastDetectedAt) return;
      const idleMs = Date.now() - lastDetectedAt;
      if (idleMs >= IDLE_DOWNGRADE_MS) {
        this.ensureLowPerf(term);
      }
    },

    startResultPolling(term) {
      this.stopResultPolling(term);
      term.resultTimer = setInterval(async () => {
        if (!term.previewRunning || term.loading) return;
        this.applyAdaptivePerf(term);
        await this.pullQrResult(term);
      }, this.pollIntervalMs(term));
    },

    stopResultPolling(term) {
      if (term.resultTimer) {
        clearInterval(term.resultTimer);
        term.resultTimer = null;
      }
    },

    stopSessionTimer(term) {
      if (term.sessionTimer) {
        clearTimeout(term.sessionTimer);
        term.sessionTimer = null;
      }
    },

    stopResultResetTimer() {},

    async runManualScan(term) {
      if (!term.previewRunning || term.scanSessionActive) return;
      term.scanSessionActive = true;
      term.permissionState = "scanning";
      term.verifyMessage = "Dang quet QR...";
      term.holdLocked = false;
      term.emptyPollStreak = 0;
      term.holdPayload = "";
      term.holdStartedAt = 0;
      try {
        await resetQrSession();
        await scanQrOnce();
      } catch (e) {
        term.scanSessionActive = false;
        term.permissionState = "deny";
        term.verifyMessage = e?.message || "Loi khi bat dau quet.";
      }
    },

    async pullQrResult(term) {
      try {
        const res = await getQrScanResult();
        if (!res) return;

        if (term.holdLocked) return;

        if (res.locked && res.qr && term.scanSessionActive) {
          this.stopSessionTimer(term);
          term.scanSessionActive = false;
          term.sessionLocked = true;
          term.qrPayload = String(res.qr || "").trim();
          term.lastDetectedAt = Date.now();
          this.ensureNormalPerf(term);
          const now = Date.now();
          if (
            term.qrPayload &&
            term.qrPayload === term.lastPayload &&
            now - Number(term.lastPayloadAt || 0) < DUPLICATE_SUPPRESS_MS
          ) {
            term.scanSessionActive = false;
            return;
          }
          term.lastPayload = term.qrPayload;
          term.lastPayloadAt = now;
          term.traceCounter = Number(term.traceCounter || 0) + 1;
          term.activeTraceId = term.traceCounter;
          await this.callApiScanAccess(term, term.qrPayload);
          term.holdLocked = true;
          term.holdPayload = term.qrPayload;
          term.holdStartedAt = Date.now();
          term.emptyPollStreak = 0;
          term.scanSessionActive = false;
        }
      } catch {
        // keep loop alive
      }
    },

    async callApiScanAccess(term, payload) {
      term.loading = true;
      try {
        const userId = this.getCurrentUserId();
        const reqData = {
          QrPayload: payload,
          CameraId: term.cameraId,
          UserPassword: term.userPassword,
          LoggedInUserId: userId > 0 ? userId : null
        };

        const res = await axios.post("/api/QrAccess/scan-access", reqData);
        const data = res?.data?.data || {};

        term.verifiedId = String(data.employeeId || data.visitorDetailId || "");
        term.verifiedName = String(data.subjectName || "");
        term.verifiedType = data.employeeId ? "Nhan vien" : "Khach";
        term.identityLabel = this.buildIdentityLabel(term.activeTraceId, term.verifiedType, term.verifiedId, term.verifiedName);
        term.verifyMessage = res?.data?.message || "Cho phep";
        term.permissionState = "allow";
      } catch (err) {
        const status = Number(err?.response?.status || 0);
        const data = err?.response?.data?.data || {};
        const message = err?.response?.data?.message || err?.message || "Tu choi";

        term.verifiedId = String(data.employeeId || data.visitorDetailId || "");
        term.verifiedName = String(data.subjectName || "");
        term.verifiedType = data.employeeId ? "Nhan vien" : "Khach";
        term.identityLabel = this.buildIdentityLabel(term.activeTraceId, term.verifiedType, term.verifiedId, term.verifiedName);
        term.verifyMessage = status === 401 ? "Mat khau tai khoan khong chinh xac." : message;
        term.permissionState = "deny";
      } finally {
        term.loading = false;
      }
    },

    buildIdentityLabel(traceId, type, id, name) {
      const traceText = Number(traceId || 0) > 0 ? `#${traceId}` : "#-";
      const typeText = String(type || "").trim();
      const idText = String(id || "").trim();
      const nameText = String(name || "").trim();
      if (!typeText && !idText && !nameText) return "";
      const safeType = typeText || "Doi tuong";
      const safeId = idText || "N/A";
      const safeName = nameText || "Chua ro";
      return `${traceText} | ${safeType} | ID: ${safeId} | Ten: ${safeName}`;
    },

    previewStateClass(term) {
      if (term.permissionState === "allow") return "state-allow";
      if (term.permissionState === "deny") return "state-deny";
      return "state-idle";
    },

    statusPillText(term) {
      if (!term.previewRunning) return "OFFLINE";
      if (term.permissionState === "allow") return "CHO PHEP";
      if (term.permissionState === "deny") return "TU CHOI";
      if (term.permissionState === "scanning") return "DANG QUET";
      return "SAN SANG";
    },

    statusPillClass(term) {
      if (!term.previewRunning) return "wait";
      if (term.permissionState === "allow") return "ok";
      if (term.permissionState === "deny") return "danger";
      return "neutral";
    }
  }
};
</script>

<style scoped>
.qrm-page { min-height: calc(100vh - 20px); padding: 16px; }
.qrm-topbar { margin-top: 6px; margin-bottom: 10px; display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; }
.qrm-topbar h1 { margin: 0; font-size: clamp(30px, 4vw, 44px); font-weight: 900; line-height: 1.1; letter-spacing: -0.02em; }
.qrm-topbar p { margin: 8px 0 0; color: #64748b; font-size: 16px; }
.qrm-settings-btn { height: 40px; padding: 0 14px; border-radius: 10px; border: 1px solid #cbd5e1; background: #fff; color: #334155; font-weight: 800; cursor: pointer; }

.qrm-card { width: min(1280px, 100%); margin: 0 auto; background: #ffffff; border: 1px solid #dde6f0; border-radius: 20px; padding: 16px; box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08); }
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

.qrm-form-grid { display: grid; grid-template-columns: 1fr; gap: 12px; margin-bottom: 12px; }
.qrm-field label { display: block; font-size: 12px; font-weight: 800; margin-bottom: 6px; color: #2e4159; text-transform: uppercase; letter-spacing: .03em; }
.qrm-field input { width: 100%; height: 44px; border: 1px solid #c5d4e6; border-radius: 12px; padding: 0 12px; font-size: 15px; outline: none; background: #f8fbff; }
.camera-verified { margin-top: 8px; font-size: 12px; font-weight: 800; }
.camera-verified.ok { color: #15803d; }
.camera-verified.warn { color: #b45309; }

.qrm-preview-wrap { max-width: 820px; margin: 0 auto; }
.qrm-preview-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; font-weight: 800; color: #12233b; }
.preview-badge { background: #fff7ed; color: #c2410c; border-radius: 999px; padding: 6px 10px; font-size: 12px; }
.cam-preview { width: 100%; aspect-ratio: 16/10; background: #07163b; border-radius: 14px; border: 2px solid #27497f; overflow: hidden; position: relative; transition: border-color .18s ease, box-shadow .18s ease; }
.cam-preview.state-allow { border-color: #16a34a; box-shadow: 0 0 0 2px rgba(34, 197, 94, 0.28) inset; }
.cam-preview.state-deny { border-color: #dc2626; box-shadow: 0 0 0 2px rgba(239, 68, 68, 0.3) inset; }
.cam-preview.state-idle { border-color: #27497f; }
.preview-image { width: 100%; height: 100%; object-fit: contain; display: block; }
.cam-off { width: 100%; height: 100%; display: flex; color: #d4e2ff; align-items: center; justify-content: center; font-size: 30px; font-weight: 800; }

.id-overlay {
  position: absolute;
  left: 12px;
  bottom: 12px;
  padding: 8px 10px;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 800;
  color: #fff;
  background: rgba(15, 23, 42, 0.7);
  max-width: calc(100% - 24px);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.id-overlay.allow { background: rgba(22, 163, 74, 0.82); }
.id-overlay.deny { background: rgba(220, 38, 38, 0.82); }

.scan-overlay {
  position: absolute;
  right: 12px;
  top: 12px;
  background: rgba(2, 6, 23, 0.7);
  color: #e2e8f0;
  border: 1px solid rgba(148, 163, 184, 0.45);
  border-radius: 999px;
  font-size: 12px;
  font-weight: 800;
  padding: 6px 10px;
}

.search-box { position: relative; }
.dropdown { position: absolute; background: #fff; border: 1px solid #c6d5e8; border-radius: 10px; width: 100%; max-height: 220px; overflow-y: auto; z-index: 9999; box-shadow: 0 12px 24px rgba(15, 23, 42, 0.12); }
.dropdown-item { padding: 10px; cursor: pointer; font-size: 14px; }
.dropdown-item:hover { background: #edf4ff; }

.qrm-drawer-mask { position: fixed; inset: 0; background: rgba(15, 23, 42, 0.35); z-index: 200; display: flex; justify-content: flex-end; }
.qrm-drawer { width: min(420px, 92vw); height: 100%; background: #ffffff; box-shadow: -12px 0 30px rgba(15, 23, 42, 0.18); padding: 16px; }
.qrm-drawer-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
.qrm-drawer-head h3 { margin: 0; font-size: 20px; font-weight: 900; }
.qrm-drawer-close { border: 1px solid #cbd5e1; background: #fff; border-radius: 8px; padding: 6px 10px; font-weight: 700; cursor: pointer; }
.qrm-setting-row { display: flex; justify-content: space-between; align-items: center; gap: 12px; border: 1px solid #e2e8f0; border-radius: 12px; padding: 12px; margin-bottom: 10px; }
.qrm-setting-name { font-weight: 900; color: #0f172a; }
.qrm-setting-desc { margin-top: 4px; font-size: 13px; color: #64748b; }

.toggle-switch { position: relative; flex-shrink: 0; width: 50px; height: 28px; border-radius: 999px; border: 2px solid #cbd5e1; background: #e2e8f0; cursor: pointer; padding: 0; transition: background 0.2s ease, border-color 0.2s ease; }
.toggle-switch.on { background: #22c55e; border-color: #16a34a; }
.toggle-switch.pending { background: #facc15; border-color: #eab308; }
.toggle-switch:disabled { opacity: 0.55; cursor: not-allowed; }
.toggle-switch-knob { position: absolute; top: 2px; left: 2px; width: 20px; height: 20px; border-radius: 50%; background: #fff; box-shadow: 0 1px 4px rgba(15, 23, 42, 0.2); transition: transform 0.2s ease; }
.toggle-switch.on .toggle-switch-knob { transform: translateX(22px); }
.toggle-switch.pending .toggle-switch-knob { background: #fef08a; }
.auto-start-btn { min-height: 30px; padding: 0 10px; border-radius: 999px; border: 1px solid #cbd5e1; background: #f8fafc; color: #334155; font-size: 11px; font-weight: 700; cursor: pointer; }
.auto-start-btn:disabled { opacity: 0.55; cursor: not-allowed; }
.qrm-refresh-btn { width: 100%; height: 40px; border: 1px solid #cbd5e1; border-radius: 10px; background: #fff; font-weight: 800; cursor: pointer; }

.auth-mask { position: fixed; inset: 0; background: rgba(2, 6, 23, 0.45); z-index: 260; display: grid; place-items: center; }
.auth-dialog { width: min(460px, 92vw); background: #fff; border: 1px solid #dbe5f1; border-radius: 14px; padding: 16px; box-shadow: 0 18px 42px rgba(2, 6, 23, 0.24); }
.auth-dialog h3 { margin: 0 0 8px; font-size: 22px; }
.auth-dialog p { margin: 0 0 10px; color: #475569; font-weight: 700; }
.auth-dialog input { width: 100%; height: 44px; border: 1px solid #c5d4e6; border-radius: 10px; padding: 0 12px; font-size: 15px; }
.auth-error { margin-top: 8px; color: #b91c1c; font-size: 13px; font-weight: 700; }
.auth-actions { margin-top: 14px; display: flex; justify-content: flex-end; gap: 10px; }

@media (max-width: 900px) {
  .qrm-topbar { flex-direction: column; align-items: stretch; }
  .qrm-status-pill { min-width: 120px; }
}
</style>
