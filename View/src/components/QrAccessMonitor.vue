<template>
  <div class="qrm-page">
    <div class="qrm-topbar">
      <div>
        <h1>Giám sát lối đi bộ bằng QR</h1>
        <p>Kiểm soát vào/ra bằng QR cho luồng đi bộ.</p>
      </div>
      <button class="qrm-settings-btn" type="button" @click="showSettings = true">Cài đặt</button>
    </div>

    <section v-for="term in terminals" :key="term.id" class="qrm-card">
      <div class="qrm-card-head">
        <div>
          <div class="qrm-kicker">Trạm</div>
          <h2>{{ term.name }}</h2>
          <p>{{ term.desc }}</p>
        </div>
        <div class="qrm-status-pill" :class="statusPillClass(term)">{{ statusPillText(term) }}</div>
      </div>

      <div class="qrm-gate-row">
        <div class="qrm-field">
          <label>Cổng / khu vực triển khai</label>
          <select
            v-model="term.gateId"
            class="qrm-select"
            :disabled="term.loading || gateLoading"
            @change="onSelectGate(term)"
          >
            <option :value="null" disabled>{{ gateLoading ? 'Đang tải cổng...' : '-- Chọn cổng triển khai --' }}</option>
            <option v-for="g in gates" :key="g.gateId" :value="g.gateId">
              {{ g.gateName }} (ID: {{ g.gateId }}){{ g.location ? ' · ' + g.location : '' }}
            </option>
          </select>
        </div>
        <div class="qrm-gate-badge" :class="term.appliedGateId ? 'ok' : 'warn'">
          <span class="qrm-gate-badge-label">Đang quản lý</span>
          <strong>{{ term.appliedGateId ? (term.gateName || gateNameById(term.appliedGateId)) + ' (ID: ' + term.appliedGateId + ')' : 'Chưa chọn cổng' }}</strong>
        </div>
      </div>

      <div class="qrm-actions">
        <button
          class="qrm-btn qrm-btn-main"
          :disabled="term.loading || !term.appliedGateId || !term.cameraIp"
          @click="term.continuousActive ? stopScanner(term) : startScanner(term)"
        >
          {{ term.loading ? "Đang xử lý..." : (term.continuousActive ? "Dừng quét" : "Bắt đầu quét") }}
        </button>
      </div>

      <div class="qrm-form-grid">
        <div class="qrm-field">
          <label>Camera QR của cổng {{ term.appliedGateId ? gateNameById(term.appliedGateId) : '' }} (ID: {{ term.cameraId || 'Trống' }})</label>
          <div class="search-box">
            <input
              v-model="cameraSearch[term.id]"
              placeholder="Tìm camera..."
              :disabled="term.loading || !term.appliedGateId"
              @focus="cameraOpen[term.id] = true"
              @blur="cameraOpen[term.id] = false"
            />
            <div class="dropdown" v-if="isDropVisible(term)">
              <div
                v-for="cam in dropdownMatches(term)"
                :key="cam.cameraId"
                class="dropdown-item"
                @mousedown.prevent
                @click="onChooseCamera(cam, term)"
              >
                {{ cam.cameraName }} (ID: {{ cam.cameraId }}){{ cam.gateName ? ' · ' + cam.gateName : '' }}
              </div>
            </div>
          </div>
          <div class="camera-verified" :class="term.cameraVerified ? 'ok' : 'warn'">
            {{ !term.appliedGateId ? 'Hãy chọn cổng triển khai trước' : (term.cameraVerified ? 'Camera đã xác thực' : 'Chưa xác thực camera') }}
          </div>
        </div>
      </div>

      <div class="qrm-preview-wrap">
        <div class="qrm-preview-head">
          <span>Xem trước camera</span>
          <span class="preview-badge">{{ term.previewRunning ? "Đang xem" : "Đã tắt xem" }}</span>
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
          <div v-else class="cam-off">QR ngoại tuyến</div>

          <div v-if="term.identityLabel" class="id-overlay" :class="term.permissionState">
            {{ term.identityLabel }}
          </div>

          <div class="scan-overlay" v-if="term.previewRunning && term.permissionState === 'scanning'">
            Đang quét QR...
          </div>
          <div class="scan-overlay" v-else-if="term.previewRunning && term.sessionActive">
            Đã quét — chờ mã mới
          </div>
          <div class="scan-overlay" v-else-if="term.previewRunning && term.continuousActive && !term.identityLabel && !term.scanSessionActive">
            Sẵn sàng quét...
          </div>
        </div>
      </div>
    </section>

    <div v-if="showSettings" class="qrm-drawer-mask" @click="showSettings = false">
      <aside class="qrm-drawer" @click.stop>
        <div class="qrm-drawer-head">
          <h3>Cài đặt</h3>
          <button type="button" class="qrm-drawer-close" @click="showSettings = false">Đóng</button>
        </div>

        <div class="qrm-setting-row">
          <div class="qrm-setting-copy">
            <div class="qrm-setting-name">Python đọc QR</div>
            <div class="qrm-setting-desc">Bật/tắt dịch vụ quét QR ở backend</div>
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
            <div class="qrm-setting-name">Tự khởi động Python QR</div>
            <div class="qrm-setting-desc">Tự động bật khi hệ thống khởi động</div>
          </div>
          <button
            type="button"
            class="auto-start-btn"
            :disabled="runtimeIsBusy('python_qr') || !runtimeEnabled('python_qr')"
            @click="toggleRuntimeAutoStart('python_qr')"
          >
            Tự khởi động: {{ runtimeAutoStart('python_qr') ? 'BẬT' : 'TẮT' }}
          </button>
        </div>

        <button type="button" class="qrm-refresh-btn" :disabled="runtimeLoading" @click="fetchRuntimeServices">
          {{ runtimeLoading ? 'Đang tải...' : 'Làm mới trạng thái' }}
        </button>
      </aside>
    </div>

    <div v-if="authModal.open" class="auth-mask" @click="closeAuthModal">
      <div class="auth-dialog" @click.stop>
        <h3>Xác thực đổi camera</h3>
        <p>{{ authModal.cameraName }} (ID: {{ authModal.cameraId }})</p>
        <p class="auth-hint">Sử dụng phiên đăng nhập hiện tại để xác thực camera.</p>
        <div class="auth-error" v-if="authModal.error">{{ authModal.error }}</div>
        <div class="auth-actions">
          <button type="button" class="qrm-btn qrm-btn-off" :disabled="authModal.loading" @click="closeAuthModal">Hủy</button>
          <button type="button" class="qrm-btn qrm-btn-main" :disabled="authModal.loading" @click="confirmCameraAuth">
            {{ authModal.loading ? 'Đang kiểm tra...' : 'Xác nhận' }}
          </button>
        </div>
      </div>
    </div>

    <div v-if="gateLockModal.open" class="gate-lock-mask" @click.self="cancelGateLock">
      <div class="gate-lock-dialog" @click.stop>
        <h3>Xác thực đổi cổng triển khai</h3>
        <p class="gate-lock-hint">
          Chuyển sang <strong>{{ gateLockModal.targetGateName }}</strong> (ID: {{ gateLockModal.targetGateId }}).
          Nhập lại thông tin bảo mật của tài khoản hiện tại để xác nhận.
        </p>
        <div class="gate-lock-field">
          <label>Mật khẩu</label>
          <input
            v-model="gateLockModal.password"
            type="password"
            placeholder="Nhập lại mật khẩu"
            :disabled="gateLockModal.loading"
            @keyup.enter="confirmGateLock"
          />
        </div>
        <div class="gate-lock-field" v-if="gateLockModal.mfaRequired">
          <label>Mã xác thực hai bước (6 số)</label>
          <input
            v-model="gateLockModal.mfaCode"
            type="text"
            placeholder="Mã từ ứng dụng xác thực"
            :disabled="gateLockModal.loading"
            @keyup.enter="confirmGateLock"
          />
        </div>
        <div class="gate-lock-error" v-if="gateLockModal.error">{{ gateLockModal.error }}</div>
        <div class="gate-lock-actions">
          <button type="button" class="qrm-btn qrm-btn-off" :disabled="gateLockModal.loading" @click="cancelGateLock">Hủy</button>
          <button type="button" class="qrm-btn qrm-btn-main" :disabled="gateLockModal.loading" @click="confirmGateLock">
            {{ gateLockModal.loading ? 'Đang xác thực...' : 'Xác nhận' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import http from "../services/http";
import { getCameras } from "../services/cameraRuntimeApi";
import { getManualGates } from "../services/gateTransitApi";
import { getRuntimeServices, startRuntimeService, stopRuntimeService, updateRuntimeService } from "../services/runtimeServiceApi";
import { startQrScanner, resetQrSession, stopQrScanner, getQrScanResult, scanQrOnce } from "../services/dynamicQrScannerApi";
import { loadViewPrefs, saveViewPrefs } from "../services/viewPrefs";

const VIEW_PREFS_KEY = "QrAccessMonitor";

const SCAN_TIMEOUT_MS = 6500;
const IDLE_DOWNGRADE_MS = 25000;
const NORMAL_POLL_MS = 300;
const LOW_POLL_MS = 900;

export default {
  name: "QrAccessMonitor",
  data() {
    return {
      cameras: [],
      gates: [],
      gateLoading: false,
      cameraSearch: {},
      cameraOpen: {},
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
        loading: false,
        error: ""
      },
      gateLockModal: {
        open: false,
        term: null,
        targetGateId: null,
        targetGateName: "",
        prevGateId: null,
        password: "",
        mfaCode: "",
        mfaRequired: false,
        loading: false,
        error: ""
      },
      terminals: [
        {
          id: "term1",
          name: "Chốt đi bộ 1",
          desc: "Quét QR kiểm tra quyền truy cập",
          loading: false,
          gateId: null,
          gateName: "",
          appliedGateId: null,
          cameraIp: "",
          viewUrl: "",
          cameraId: null,
          cameraName: "",
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
          lastResolvedPermissionState: "idle",
          lastResolvedIdentityLabel: "",
          lastResolvedVerifyMessage: "",
          lastPayload: "",
          lastPayloadAt: 0,
          perfMode: "normal",
          lastDetectedAt: 0,
          holdLocked: false,
          emptyPollStreak: 0,
          holdPayload: "",
          holdStartedAt: 0,
          continuousActive: false,
          sessionActive: false,
          lastLockedAt: 0
        }
      ]
    };
  },

  async mounted() {
    await this.loadCameraList();
    await this.loadGates();
    await this.fetchRuntimeServices();
    this.restoreTermSetup(this.terminals[0]);
  },

  beforeUnmount() {
    this.terminals.forEach((term) => {
      this.stopScanner(term);
    });
  },

  methods: {
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
        alert(e?.response?.data?.message || e?.message || "Không thể bật/tắt runtime.");
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
        alert(e?.response?.data?.message || e?.message || "Không thể đổi thiết lập tự khởi động.");
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

    async loadGates() {
      this.gateLoading = true;
      try {
        const res = await getManualGates();
        this.gates = res.data?.data && Array.isArray(res.data.data) ? res.data.data : [];
      } catch (e) {
        console.error("loadGates error", e);
        this.gates = [];
      } finally {
        this.gateLoading = false;
      }
    },

    gateNameById(gateId) {
      if (!gateId) return "";
      const gate = this.gates.find((g) => Number(g.gateId) === Number(gateId));
      return gate ? gate.gateName : "";
    },

    // ===== Bộ nhớ setup theo user + view =====

    persistTermSetup(term) {
      saveViewPrefs(VIEW_PREFS_KEY, {
        appliedGateId: term.appliedGateId || null,
        gateName: term.gateName || this.gateNameById(term.appliedGateId) || "",
        cameraId: term.cameraId || null,
        cameraName: term.cameraName || "",
        cameraIp: term.cameraIp || "",
        viewUrl: term.viewUrl || "",
        cameraVerified: !!term.cameraVerified
      });
    },

    restoreTermSetup(term) {
      const prefs = loadViewPrefs(VIEW_PREFS_KEY);
      if (!prefs) return;

      const gateId = Number(prefs.appliedGateId || 0);
      const gate = gateId ? this.gates.find((g) => Number(g.gateId) === gateId) : null;
      if (!gate) return;

      term.appliedGateId = gateId;
      term.gateId = gateId;
      term.gateName = gate.gateName || "";

      const cameraId = Number(prefs.cameraId || 0);
      const cam = cameraId ? this.cameras.find((c) => Number(c.cameraId) === cameraId) : null;
      if (cam && Number(cam.gateId || 0) === gateId) {
        term.cameraId = cam.cameraId;
        term.cameraName = cam.cameraName || "";
        term.cameraIp = cam.streamUrl || prefs.cameraIp || "";
        term.viewUrl = cam.urlView || prefs.viewUrl || "";
        term.cameraVerified = true;
        this.cameraSearch[term.id] = cam.cameraName || "";
      } else if (prefs.cameraVerified) {
        term.cameraId = null;
        term.cameraName = "";
        term.cameraIp = "";
        term.viewUrl = "";
        term.cameraVerified = false;
        this.cameraSearch[term.id] = "";
      }
    },

    onSelectGate(term) {
      const target = Number(term.gateId || 0);
      const applied = Number(term.appliedGateId || 0);
      if (!target) {
        term.gateId = applied || null;
        return;
      }
      if (target === applied) return;

      const gate = this.gates.find((g) => Number(g.gateId) === target);
      this.gateLockModal = {
        open: true,
        term,
        targetGateId: target,
        targetGateName: gate ? gate.gateName : "",
        prevGateId: applied || null,
        password: "",
        mfaCode: "",
        mfaRequired: false,
        loading: false,
        error: ""
      };
      this.fetchGateLockMfaRequired();
    },

    async fetchGateLockMfaRequired() {
      try {
        const res = await http.get("/Auth/me");
        this.gateLockModal.mfaRequired = !!res?.data?.mfaRequired;
      } catch (e) {
        this.gateLockModal.error = "Không lấy được trạng thái bảo mật của tài khoản.";
      }
    },

    async confirmGateLock() {
      const modal = this.gateLockModal;
      if (modal.loading) return;
      if (!String(modal.password || "").trim()) {
        modal.error = "Vui lòng nhập lại mật khẩu.";
        return;
      }
      if (modal.mfaRequired && !String(modal.mfaCode || "").trim()) {
        modal.error = "Vui lòng nhập mã xác thực hai bước.";
        return;
      }
      modal.loading = true;
      modal.error = "";
      try {
        const startRes = await http.post("/Auth/step-up/start", {
          action: "GateSelection",
          reason: `Đổi cổng triển khai sang ${modal.targetGateName}`
        });
        const sessionId = startRes?.data?.sessionId;
        if (!sessionId) throw new Error("Không tạo được phiên xác thực.");
        await http.post("/Auth/step-up/verify", {
          sessionId,
          password: modal.password,
          mfaCode: modal.mfaCode || null
        });
        this.applyGateChange(modal.term, modal.targetGateId, modal.targetGateName);
        modal.open = false;
      } catch (e) {
        const status = Number(e?.response?.status || 0);
        modal.error = status === 401 || status === 400
          ? "Mật khẩu hoặc mã xác thực hai bước không đúng."
          : (e?.response?.data?.message || e?.message || "Xác thực thất bại.");
        modal.password = "";
        modal.mfaCode = "";
      } finally {
        modal.loading = false;
      }
    },

    applyGateChange(term, gateId, gateName) {
      term.appliedGateId = gateId;
      term.gateId = gateId;
      term.gateName = gateName || this.gateNameById(gateId) || "";
      if (term.previewRunning) {
        this.stopScanner(term);
      }
      term.cameraId = null;
      term.cameraVerified = false;
      term.cameraIp = "";
      term.viewUrl = "";
      this.cameraSearch[term.id] = "";
      this.clearScanState(term);
      this.persistTermSetup(term);
    },

    cancelGateLock() {
      const modal = this.gateLockModal;
      if (modal.loading) return;
      const term = modal.term;
      if (term) term.gateId = modal.prevGateId;
      modal.open = false;
      modal.error = "";
    },

    filterCameras(keyword, term) {
      const gateId = Number(term?.appliedGateId || 0);
      const list = this.cameras.filter((c) => {
        const cGate = Number(c.gateId || 0);
        if (!gateId) return cGate === 0;
        return cGate === gateId;
      });
      if (!keyword) return list;
      const key = String(keyword || "").toLowerCase();
      return list.filter((c) =>
        String(c.cameraName || "").toLowerCase().includes(key) ||
        String(c.cameraId || "").includes(key)
      );
    },

    dropdownMatches(term) {
      return this.filterCameras(this.cameraSearch[term.id], term).slice(0, 5);
    },

    isDropVisible(term) {
      if (!this.cameraOpen[term.id]) return false;
      const list = this.dropdownMatches(term);
      if (!list.length) return false;
      if (
        term.cameraVerified &&
        term.cameraId &&
        list.length === 1 &&
        Number(list[0].cameraId) === Number(term.cameraId)
      ) {
        return false;
      }
      return true;
    },

    onChooseCamera(cam, term) {
      if (!cam?.urlView || !cam?.streamUrl) {
        alert("Camera chưa có đủ URL stream/view.");
        return;
      }
      this.authModal = {
        open: true,
        termId: term.id,
        cameraId: cam.cameraId,
        cameraName: cam.cameraName || "Camera",
        cameraIp: cam.streamUrl,
        viewUrl: cam.urlView,
        gateId: term.appliedGateId || null,
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

      const term = this.terminals.find((x) => x.id === this.authModal.termId);
      if (!term) return;

      this.authModal.loading = true;
      this.authModal.error = "";
      try {
        await http.post("/QrAccess/verify-camera-auth", {
          CameraId: this.authModal.cameraId,
          GateId: this.authModal.gateId || null
        });

        if (term.previewRunning) {
          await this.stopScanner(term);
        }

        term.cameraIp = this.authModal.cameraIp;
        term.viewUrl = this.authModal.viewUrl;
        term.cameraId = this.authModal.cameraId;
        term.cameraName = this.authModal.cameraName || "";
        term.cameraVerified = true;
        term.permissionState = "idle";
        term.identityLabel = "";
        this.cameraSearch[term.id] = this.authModal.cameraName;
        this.authModal.open = false;
        this.authModal.error = "";
        this.persistTermSetup(term);
      } catch (e) {
        this.authModal.error = e?.response?.data?.message || e?.message || "Xác thực thất bại.";
      } finally {
        this.authModal.loading = false;
      }
    },

    async startScanner(term) {
      if (!term.appliedGateId) {
        alert("Vui lòng chọn cổng / khu vực triển khai trước khi mở.");
        return;
      }
      if (!term.cameraId || !term.cameraVerified) {
        alert("Vui lòng chọn camera và xác thực mật khẩu trước khi mở.");
        return;
      }
      if (!String(term.cameraIp || "").trim() || !String(term.viewUrl || "").trim()) {
        alert("Camera chưa có URL stream/view hợp lệ.");
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
        term.continuousActive = true;
        this.ensureNormalPerf(term);
        term.lastDetectedAt = Date.now();
        this.startResultPolling(term);
        await this.runManualScan(term);
      } catch (e) {
        alert(e?.message || "Không thể mở scanner Python.");
      } finally {
        term.loading = false;
      }
    },

    async stopScanner(term) {
      term.previewRunning = false;
      term.continuousActive = false;
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
      this.stopResultResetTimer(term);
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
      term.lastResolvedPermissionState = "idle";
      term.lastResolvedIdentityLabel = "";
      term.lastResolvedVerifyMessage = "";
      term.lastPayload = "";
      term.lastPayloadAt = 0;
      term.holdLocked = false;
      term.emptyPollStreak = 0;
      term.holdPayload = "";
      term.holdStartedAt = 0;
      term.sessionActive = false;
      term.lastLockedAt = 0;
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

    stopResultResetTimer(term) {
      if (term?.resultResetTimer) {
        clearTimeout(term.resultResetTimer);
        term.resultResetTimer = null;
      }
    },

    async runManualScan(term) {
      if (!term.previewRunning || term.scanSessionActive) return;
      this.stopResultResetTimer(term);
      term.scanSessionActive = true;
      term.permissionState = "scanning";
      term.verifyMessage = "Đang quét QR...";
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
        term.verifyMessage = e?.message || "Lỗi khi bắt đầu quét.";
      }
    },

    async pullQrResult(term) {
      try {
        const res = await getQrScanResult();
        if (!res) return;

        const inSession = !!res.session_active;
        if (!inSession && term.sessionActive) {
          this.resetResultDisplay(term);
        }
        term.sessionActive = inSession;

        if (res.cooldown_payload) {
          this.stopSessionTimer(term);
          term.scanSessionActive = false;
          return;
        }

        if (res.locked && res.qr) {
          const lockedAt = Number(res.locked_at || 0);
          const isNewLock = lockedAt !== Number(term.lastLockedAt || 0);
          term.lastLockedAt = lockedAt;
          if (!isNewLock) return;

          this.stopSessionTimer(term);
          term.scanSessionActive = false;
          term.sessionLocked = true;
          term.qrPayload = String(res.qr || "").trim();
          term.lastDetectedAt = Date.now();
          this.ensureNormalPerf(term);
          term.traceCounter = Number(term.traceCounter || 0) + 1;
          term.activeTraceId = term.traceCounter;
          await this.callApiScanAccess(term, term.qrPayload);
        }
      } catch {
        // keep loop alive
      }
    },

    resetResultDisplay(term) {
      this.stopSessionTimer(term);
      this.stopResultResetTimer(term);
      term.scanSessionActive = false;
      term.sessionLocked = false;
      term.qrPayload = "";
      term.verifiedId = "";
      term.verifiedName = "";
      term.verifiedType = "";
      term.verifyMessage = "";
      term.permissionState = "idle";
      term.identityLabel = "";
      term.lastResolvedPermissionState = "idle";
      term.lastResolvedIdentityLabel = "";
      term.lastResolvedVerifyMessage = "";
    },

    async callApiScanAccess(term, payload) {
      term.loading = true;
      try {
        const reqData = {
          QrPayload: payload,
          CameraId: term.cameraId,
          GateId: term.appliedGateId || null
        };

        const res = await http.post("/QrAccess/scan-access", reqData);
        const data = res?.data?.data || {};

        term.verifiedId = String(data.employeeId || data.visitorDetailId || "");
        term.verifiedName = String(data.subjectName || "");
        term.verifiedType = data.employeeId ? "Nhân viên" : "Khách";
        term.identityLabel = this.buildIdentityLabel(term.activeTraceId, term.verifiedType, term.verifiedId, term.verifiedName);
        term.verifyMessage = res?.data?.message || "Cho phép";
        term.permissionState = "allow";
        term.lastResolvedPermissionState = term.permissionState;
        term.lastResolvedIdentityLabel = term.identityLabel;
        term.lastResolvedVerifyMessage = term.verifyMessage;
      } catch (err) {
        const status = Number(err?.response?.status || 0);
        const data = err?.response?.data?.data || {};
        const message = err?.response?.data?.message || err?.message || "Tu choi";

        term.verifiedId = String(data.employeeId || data.visitorDetailId || "");
        term.verifiedName = String(data.subjectName || "");
        term.verifiedType = data.employeeId ? "Nhân viên" : "Khách";
        term.identityLabel = this.buildIdentityLabel(term.activeTraceId, term.verifiedType, term.verifiedId, term.verifiedName);
        term.verifyMessage = status === 401 ? "Phiên đăng nhập không hợp lệ hoặc đã hết quyền." : message;
        term.permissionState = "deny";
        term.lastResolvedPermissionState = term.permissionState;
        term.lastResolvedIdentityLabel = term.identityLabel;
        term.lastResolvedVerifyMessage = term.verifyMessage;
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
      const safeType = typeText || "Đối tượng";
      const safeId = idText || "N/A";
      const safeName = nameText || "Chưa rõ";
      return `${traceText} | ${safeType} | ID: ${safeId} | Tên: ${safeName}`;
    },

    previewStateClass(term) {
      if (term.permissionState === "allow") return "state-allow";
      if (term.permissionState === "deny") return "state-deny";
      return "state-idle";
    },

    statusPillText(term) {
      if (!term.previewRunning) return "OFFLINE";
      if (term.permissionState === "allow") return "CHO PHEP";
      if (term.permissionState === "deny") return "TỪ CHỐI";
      if (term.permissionState === "scanning") return "ĐANG QUÉT";
      if (term.continuousActive) return "ĐANG CHẠY";
      return "SẴN SÀNG";
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
.qrm-topbar h1 { margin: 0; font-size: clamp(30px, 4vw, 44px); font-weight: 900; line-height: 1.1; letter-spacing: -0.02em; color: var(--text-primary); }
.qrm-topbar p { margin: 8px 0 0; color: var(--text-muted); font-size: 16px; }
.qrm-settings-btn { height: 40px; padding: 0 14px; border-radius: 10px; border: 1px solid var(--border-subtle); background: var(--surface-default); color: var(--text-secondary); font-weight: 800; cursor: pointer; }
.qrm-settings-btn:hover { background: var(--surface-hover); }

.qrm-card { width: min(1280px, 100%); margin: 0 auto; background: var(--surface-default); border: 1px solid var(--border-subtle); border-radius: 20px; padding: 16px; box-shadow: var(--shadow-sm); }
.qrm-card-head { display: flex; justify-content: space-between; align-items: center; gap: 10px; margin-bottom: 12px; }
.qrm-kicker { font-size: 12px; font-weight: 800; letter-spacing: .08em; color: var(--text-muted); text-transform: uppercase; }
.qrm-card-head h2 { margin: 4px 0 0; font-size: clamp(24px, 3vw, 32px); font-weight: 900; color: var(--text-primary); }
.qrm-card-head p { margin: 4px 0 0; color: var(--text-secondary); font-size: 15px; }

.qrm-status-pill { min-width: 170px; text-align: center; padding: 10px 14px; border-radius: 999px; font-size: 13px; font-weight: 900; }
.qrm-status-pill.ok { background: var(--status-success-bg); color: var(--status-success-text); }
.qrm-status-pill.wait { background: var(--status-warning-bg); color: var(--status-warning-text); }
.qrm-status-pill.danger { background: var(--status-danger-bg); color: var(--status-danger-text); }
.qrm-status-pill.neutral { background: var(--status-neutral-bg); color: var(--status-neutral-text); }

.qrm-actions { display: flex; gap: 10px; margin-bottom: 12px; }
.qrm-btn { height: 44px; border: none; border-radius: 12px; padding: 0 16px; color: white; font-size: 14px; font-weight: 800; cursor: pointer; }
.qrm-btn-main { background: var(--accent-gradient); }
.qrm-btn-off { background: var(--accent-danger); }
.qrm-btn:disabled { opacity: 0.6; cursor: not-allowed; }

.qrm-form-grid { display: grid; grid-template-columns: 1fr; gap: 12px; margin-bottom: 12px; }
.qrm-gate-row { display: grid; grid-template-columns: 1fr auto; align-items: end; gap: 12px; margin-bottom: 12px; }
.qrm-select { width: 100%; height: 44px; border: 1px solid var(--border-subtle); border-radius: 12px; padding: 0 12px; font-size: 15px; outline: none; background: var(--surface-subtle); color: var(--text-primary); }
.qrm-gate-badge { display: grid; gap: 2px; justify-items: start; padding: 8px 14px; border-radius: 12px; font-size: 14px; }
.qrm-gate-badge .qrm-gate-badge-label { font-size: 11px; font-weight: 800; letter-spacing: .06em; text-transform: uppercase; opacity: .7; }
.qrm-gate-badge.ok { background: var(--status-success-bg); color: var(--status-success-text); border: 1px solid var(--status-success-border); }
.qrm-gate-badge.warn { background: var(--status-warning-bg); color: var(--status-warning-text); border: 1px solid var(--status-warning-border); }
.qrm-field label { display: block; font-size: 12px; font-weight: 800; margin-bottom: 6px; color: var(--text-secondary); text-transform: uppercase; letter-spacing: .03em; }
.qrm-field input { width: 100%; height: 44px; border: 1px solid var(--border-subtle); border-radius: 12px; padding: 0 12px; font-size: 15px; outline: none; background: var(--surface-subtle); color: var(--text-primary); }
.camera-verified { margin-top: 8px; font-size: 12px; font-weight: 800; }
.camera-verified.ok { color: var(--accent-success); }
.camera-verified.warn { color: var(--accent-warning); }

.qrm-preview-wrap { max-width: 820px; margin: 0 auto; }
.qrm-preview-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; font-weight: 800; color: var(--text-primary); }
.preview-badge { background: var(--status-warning-bg); color: var(--status-warning-text); border-radius: 999px; padding: 6px 10px; font-size: 12px; }
.cam-preview { width: 100%; aspect-ratio: 16/10; background: #07163b; border-radius: 14px; border: 2px solid #27497f; overflow: hidden; position: relative; transition: border-color .18s ease, box-shadow .18s ease; }
.cam-preview.state-allow { border-color: #16a34a; box-shadow: 0 0 0 2px rgba(34, 197, 94, 0.28) inset; }
.cam-preview.state-deny { border-color: #dc2626; box-shadow: 0 0 0 2px rgba(239, 68, 68, 0.3) inset; }
.cam-preview.state-idle { border-color: #27497f; }
.preview-image { width: 100%; height: 100%; object-fit: cover; display: block; }
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
.dropdown { background: var(--surface-default); border: 1px solid var(--border-subtle); border-radius: 10px; width: 100%; max-height: 220px; overflow-y: auto; margin-top: 4px; box-shadow: var(--shadow-md); color: var(--text-primary); }
.dropdown-item { padding: 10px; cursor: pointer; font-size: 14px; }
.dropdown-item:hover { background: var(--surface-hover); }

.qrm-drawer-mask { position: fixed; inset: 0; background: var(--surface-overlay); z-index: 200; display: flex; justify-content: flex-end; }
.qrm-drawer { width: min(420px, 92vw); height: 100%; background: var(--surface-default); color: var(--text-primary); box-shadow: var(--shadow-overlay); padding: 16px; }
.qrm-drawer-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
.qrm-drawer-head h3 { margin: 0; font-size: 20px; font-weight: 900; color: var(--text-primary); }
.qrm-drawer-close { border: 1px solid var(--border-subtle); background: var(--surface-subtle); color: var(--text-secondary); border-radius: 8px; padding: 6px 10px; font-weight: 700; cursor: pointer; }
.qrm-drawer-close:hover { background: var(--surface-hover); }
.qrm-setting-row { display: flex; justify-content: space-between; align-items: center; gap: 12px; border: 1px solid var(--border-subtle); border-radius: 12px; padding: 12px; margin-bottom: 10px; background: var(--surface-subtle); }
.qrm-setting-name { font-weight: 900; color: var(--text-primary); }
.qrm-setting-desc { margin-top: 4px; font-size: 13px; color: var(--text-muted); }

.toggle-switch { position: relative; flex-shrink: 0; width: 50px; height: 28px; border-radius: 999px; border: 2px solid var(--border-subtle); background: var(--surface-subtle); cursor: pointer; padding: 0; transition: background 0.2s ease, border-color 0.2s ease; }
.toggle-switch.on { background: #22c55e; border-color: #16a34a; }
.toggle-switch.pending { background: #facc15; border-color: #eab308; }
.toggle-switch:disabled { opacity: 0.55; cursor: not-allowed; }
.toggle-switch-knob { position: absolute; top: 2px; left: 2px; width: 20px; height: 20px; border-radius: 50%; background: #fff; box-shadow: 0 1px 4px rgba(15, 23, 42, 0.2); transition: transform 0.2s ease; }
.toggle-switch.on .toggle-switch-knob { transform: translateX(22px); }
.toggle-switch.pending .toggle-switch-knob { background: #fef08a; }
.auto-start-btn { min-height: 30px; padding: 0 10px; border-radius: 999px; border: 1px solid var(--border-subtle); background: var(--surface-subtle); color: var(--text-secondary); font-size: 11px; font-weight: 700; cursor: pointer; }
.auto-start-btn:disabled { opacity: 0.55; cursor: not-allowed; }
.qrm-refresh-btn { width: 100%; height: 40px; border: 1px solid var(--border-subtle); border-radius: 10px; background: var(--surface-default); color: var(--text-primary); font-weight: 800; cursor: pointer; }
.qrm-refresh-btn:hover { background: var(--surface-hover); }

.auth-mask { position: fixed; inset: 0; background: var(--surface-overlay); z-index: 260; display: grid; place-items: center; }
.auth-dialog { width: min(460px, 92vw); background: var(--surface-default); color: var(--text-primary); border: 1px solid var(--border-subtle); border-radius: 14px; padding: 16px; box-shadow: var(--shadow-overlay); }
.auth-dialog h3 { margin: 0 0 8px; font-size: 22px; color: var(--text-primary); }
.auth-dialog p { margin: 0 0 10px; color: var(--text-muted); font-weight: 700; }
.auth-dialog input { width: 100%; height: 44px; border: 1px solid var(--border-subtle); border-radius: 10px; padding: 0 12px; font-size: 15px; background: var(--surface-subtle); color: var(--text-primary); }
.auth-error { margin-top: 8px; color: var(--status-danger-text); font-size: 13px; font-weight: 700; }
.auth-actions { margin-top: 14px; display: flex; justify-content: flex-end; gap: 10px; }

.gate-lock-mask { position: fixed; inset: 0; background: var(--surface-overlay); z-index: 270; display: grid; place-items: center; }
.gate-lock-dialog { width: min(440px, 92vw); background: var(--surface-default); color: var(--text-primary); border: 1px solid var(--border-subtle); border-radius: 14px; padding: 18px; box-shadow: var(--shadow-overlay); }
.gate-lock-dialog h3 { margin: 0 0 8px; font-size: 21px; color: var(--text-primary); }
.gate-lock-hint { margin: 0 0 14px; color: var(--text-muted); font-size: 13px; line-height: 1.5; }
.gate-lock-field { margin-bottom: 12px; }
.gate-lock-field label { display: block; margin-bottom: 5px; font-size: 13px; font-weight: 700; color: var(--text-secondary); }
.gate-lock-field input { width: 100%; height: 44px; border: 1px solid var(--border-subtle); border-radius: 10px; padding: 0 12px; font-size: 15px; box-sizing: border-box; background: var(--surface-subtle); color: var(--text-primary); }
.gate-lock-actions { margin-top: 16px; display: flex; justify-content: flex-end; gap: 10px; }

@media (max-width: 900px) {
  .qrm-topbar { flex-direction: column; align-items: stretch; }
  .qrm-status-pill { min-width: 120px; }
  .qrm-gate-row { grid-template-columns: 1fr; align-items: stretch; }
  .qrm-gate-badge { justify-items: stretch; }
}
</style>
