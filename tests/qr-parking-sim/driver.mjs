#!/usr/bin/env node
/**
 * Driver test tự động "Gửi xe QR" — V-Shield 2.0
 *
 * Mô phỏng đúng luồng auto-mode của frontend (GateTransitMonitor.autoDecideSession):
 *  1. sinh QR hợp lệ (POST /api/dynamic-qr/generate)
 *  2. đẩy QR vào camera giả lập (sim /control/set)
 *  3. QR runtime nhận dạng thật (pyzbar) -> poll /qr/result lấy payload
 *  4. xác thực (POST /api/dynamic-qr/verify)
 *  5. đẩy biển số vào camera giả lập -> plate runtime nhận dạng thật (YOLO+PaddleOCR)
 *  6. xác nhận thông hành (POST /api/gate-transit/scan)
 *  Lặp 20 IN + 20 OUT, đo thời gian từng bước, đếm lỗi, xuất báo cáo.
 *
 * KHÔNG sửa logic app; chỉ gọi đúng endpoint như UI.
 */

const env = (k, d) => process.env[k] || d;

const CFG = {
  api: env("AK_API", "http://localhost:5107"),
  qr1: env("AK_QR1", "http://localhost:8001"),
  qr2: env("AK_QR2", "http://localhost:8002"),
  plate: env("AK_PLATE", "http://localhost:5002"),
  sim: env("AK_SIM", "http://localhost:9400"),
  simInternal: env("AK_SIM_INTERNAL", "http://vshield-qr-sim:9400"),
  user: env("AK_USER", "admin"),
  pass: env("AK_PASS", "AdminLocal@2026"),
  gateId: Number(env("AK_GATE", "1")),
  laneIn: Number(env("AK_LANE_IN", "1")),
  laneOut: Number(env("AK_LANE_OUT", "2")),
  cameraId: Number(env("AK_CAMERA", "1")),
  pairs: Number(env("AK_PAIRS", "20")), // 20 IN + 20 OUT = 40 lượt
  employeeStart: Number(env("AK_EMP_START", "1")),
  paceMs: Number(env("AK_PACE_MS", "0")), // delay giữa các lượt (tránh rate-limit ops 60/phút)
  pollQrMs: 400,
  pollPlateMs: 700,
  timeoutQrMs: 20000,
  timeoutPlateMs: 30000,
  scannerDevice: "sim-driver-vshield"
};

const delay = (ms) => new Promise((r) => setTimeout(r, ms));

async function http(method, url, body, token) {
  const headers = { "Content-Type": "application/json" };
  if (token) headers.Authorization = `Bearer ${token}`;
  let res;
  try {
    res = await fetch(url, { method, headers, body: body ? JSON.stringify(body) : undefined });
  } catch (e) {
    throw new Error(`KHONG GOI DUOC ${url}: ${e.message}`);
  }
  let data = null;
  try { data = await res.json(); } catch { data = null; }
  if (!res.ok) {
    const err = new Error(data?.message || data?.detail || `HTTP ${res.status}`);
    err.status = res.status;
    err.data = data;
    throw err;
  }
  return data;
}

const now = () => Date.now();

// ---------- từng bước ----------
async function login() {
  const r = await http("POST", `${CFG.api}/api/auth/login`, { username: CFG.user, password: CFG.pass });
  const token = r.token || r.Token || r.accessToken || r.data?.token;
  if (!token) throw new Error("Login khong tra token");
  return token;
}

async function genQr(token, employeeId) {
  const r = await http("POST", `${CFG.api}/api/dynamic-qr/generate`, { employeeId }, token);
  return r.data?.qrPayload;
}

async function verifyQr(token, payload) {
  const r = await http("POST", `${CFG.api}/api/dynamic-qr/verify`, {
    qrPayload: payload, scannerDevice: CFG.scannerDevice
  }, token);
  return { success: !!r.success, employeeId: r.data?.employeeId, message: r.message };
}

async function scanGate(token, p) {
  const r = await http("POST", `${CFG.api}/api/gate-transit/scan`, p, token);
  return { success: !!r.success, message: r.message, data: r.data };
}

// QR runtime
async function qrStart(base, stream) {
  // stop trước để camera_worker reopen (cùng URL cũng phải mở lại)
  try { await http("POST", `${base}/qr/stop`, null); } catch {}
  await http("POST", `${base}/qr/start`, { rtsp: `${CFG.simInternal}/mjpeg/${stream}` });
  await delay(800);
}
async function qrArm(base) { await http("POST", `${base}/qr/scan`, null); }
async function qrReset(base) { await http("POST", `${base}/qr/reset`, null); }
async function qrState(base) { return http("GET", `${base}/qr/result`); }

async function waitQrLock(base, expectedPayload) {
  const deadline = now() + CFG.timeoutQrMs;
  let last = null;
  while (now() < deadline) {
    const st = await qrState(base);
    last = st;
    if (st.locked_payload) {
      if (st.locked_payload === expectedPayload) return { ok: true, payload: st.locked_payload };
      // khóa payload khác (lạ) -> đợi
    }
    await delay(CFG.pollQrMs);
  }
  return { ok: false, error: `QR timeout (expected ${expectedPayload}, last=${last?.locked_payload || "none"})` };
}

// plate runtime
async function plateOn(ip) {
  const r = await http("POST", `${CFG.plate}/api/camera/on`, { ip });
  return r;
}
async function plateReset() { await http("POST", `${CFG.plate}/api/camera/reset`, null); }
async function plateResult() { return http("GET", `${CFG.plate}/api/camera/result`); }

async function waitPlateConfirm() {
  const deadline = now() + CFG.timeoutPlateMs;
  let last = null;
  while (now() < deadline) {
    const r = await plateResult();
    last = r;
    if (r.confirmed_plate) return { ok: true, plate: r.confirmed_plate, snap: r.locked_snapshot, crop: r.locked_plate_crop };
    await delay(CFG.pollPlateMs);
  }
  return { ok: false, error: `Plate timeout (last=${last?.last_raw_plate || "none"}, bbox=${last?.bbox ? "yes" : "no"}, conn=${last?.camera_connected})` };
}

async function setSimFrame(cam, type, payload, plate) {
  await http("POST", `${CFG.sim}/control/set`, { cam, type, payload: payload || "", plate: plate || "" });
}

// ---------- thực thi 1 lượt ----------
async function runEvent({ token, qrBase, qrCam, plateCam, laneId, direction, employeeId, plateText, index }) {
  const t = { start: now() };
  const out = { index, direction, employeeId, plateFed: plateText, steps: {}, ok: false };
  try {
    // 1. sinh QR hợp lệ
    const payload = await genQr(token, employeeId);
    t.gen = now();
    out.steps.generateMs = t.gen - t.start;
    out.payload = payload;

    // 2. đẩy QR vào cam
    await setSimFrame(qrCam, "qr", payload, "");
    // arm scan trước khi đẩy frame để bắt ngay
    await qrArm(qrBase);

    // 3. chờ QR runtime nhận dạng (retry 1 lần nếu timeout)
    let qrRes = await waitQrLock(qrBase, payload);
    if (!qrRes.ok) {
      // reconnect + thử lại
      await qrStart(qrBase, qrCam);
      await setSimFrame(qrCam, "qr", payload, "");
      await qrArm(qrBase);
      qrRes = await waitQrLock(qrBase, payload);
    }
    t.qr = now();
    out.steps.qrMs = t.qr - t.gen;
    if (!qrRes.ok) throw new Error(qrRes.error);

    // 4. verify
    const v = await verifyQr(token, payload);
    t.verify = now();
    out.steps.verifyMs = t.verify - t.qr;
    if (!v.success) throw new Error(`Verify that bai: ${v.message}`);
    out.verifiedEmployeeId = v.employeeId;

    // 5. đẩy biển số vào cam plate
    await plateOn(`${CFG.simInternal}/mjpeg/${plateCam}`);
    await plateReset();
    await setSimFrame(plateCam, "plate", "", plateText);

    // 6. chờ plate runtime nhận dạng (retry 1 lần nếu timeout)
    let pl = await waitPlateConfirm();
    if (!pl.ok) {
      await plateOn(`${CFG.simInternal}/mjpeg/${plateCam}`);
      await plateReset();
      await setSimFrame(plateCam, "plate", "", plateText);
      pl = await waitPlateConfirm();
    }
    t.plate = now();
    out.steps.plateMs = t.plate - t.verify;
    if (!pl.ok) throw new Error(pl.error);
    out.plateRead = pl.plate;

    // 7. scan
    const scanPayload = {
      LicensePlate: pl.plate,
      GateId: CFG.gateId,
      LaneId: laneId,
      Direction: direction,
      CameraId: CFG.cameraId,
      CredentialType: "QR",
      EmployeeId: v.employeeId,
      VehicleTypeId: 1,
      PlateSnapshotBase64: pl.snap || null,
      PlateCropBase64: pl.crop || null,
      QrSnapshotBase64: null
    };
    const sc = await scanGate(token, scanPayload);
    t.scan = now();
    out.steps.scanMs = t.scan - t.plate;
    if (!sc.success) throw new Error(`Scan that bai: ${sc.message}`);
    out.logId = sc.data?.logId;
    out.ok = true;
    out.message = sc.message;
  } catch (e) {
    out.ok = false;
    out.error = e.message;
    out.status = e.status || 0;
  } finally {
    t.end = now();
    out.steps.totalMs = t.end - t.start;
    // reset trạng thái cho lượt sau
    try { await qrReset(qrBase); } catch {}
    try { await plateReset(); } catch {}
    // trả frame về neutral
    try { await setSimFrame(qrCam, "neutral", "", ""); } catch {}
    try { await setSimFrame(plateCam, "neutral", "", ""); } catch {}
  }
  return out;
}

// ---------- chạy ----------
async function main() {
  console.log("=== AK Dịch xe QR — DRIVER ===");
  console.log(JSON.stringify(CFG, null, 2));
  const token = await login();
  console.log("Login OK.");

  // warm-up: khởi động QR runtimes + 1 lượt IN thử.
  // Dùng nhân viên KHÁC (employeeStart+pairs) để tránh trùng payload TOTP 30s
  // với lượt IN đầu (cùng employee trong 30s sinh QR y hệt nhau).
  await qrStart(CFG.qr1, "qr1");
  await qrStart(CFG.qr2, "qr2");
  console.log("QR runtimes started (qr1/qr2). Warm-up...");
  const warmEmp = CFG.employeeStart + CFG.pairs;
  const warm = await runEvent({
    token, qrBase: CFG.qr1, qrCam: "qr1", plateCam: "plate1",
    laneId: CFG.laneIn, direction: "IN", employeeId: warmEmp,
    plateText: `30A${1000 + warmEmp}`, index: "warm"
  });
  if (!warm.ok) {
    console.log("WARM-UP FAIL:", JSON.stringify(warm, null, 2));
    console.log("Dung lai — kiem tra QR/plate runtime va sim.");
    process.exit(1);
  }
  console.log(`Warm-up OK (qr=${warm.steps.qrMs}ms plate=${warm.steps.plateMs}ms scan=${warm.steps.scanMs}ms plateRead=${warm.plateRead})`);

  const results = [];
  const t0 = now();
  const N = CFG.pairs;

  for (let i = 0; i < N; i++) {
    const emp = CFG.employeeStart + i;
    const plateText = `30A${1000 + emp}`;
    // 20 IN (làn vào)
    const rIn = await runEvent({
      token, qrBase: CFG.qr1, qrCam: "qr1", plateCam: "plate1",
      laneId: CFG.laneIn, direction: "IN", employeeId: emp, plateText, index: `IN-${i + 1}`
    });
    results.push(rIn);
    if (CFG.paceMs) await delay(CFG.paceMs);
    const pct = ((i + 1) / N) * 50;
    process.stdout.write(`\rIN ${i + 1}/${N}  ${rIn.ok ? "OK " : "LOI"} ${rIn.ok ? rIn.steps.totalMs + "ms" : rIn.error}  (${Math.round(pct)}%)   `);
    if (!rIn.ok) console.log("");
  }
  console.log("\n=== XONG 20 IN ===");

  for (let i = 0; i < N; i++) {
    const emp = CFG.employeeStart + i;
    const plateText = `30A${1000 + emp}`;
    const rOut = await runEvent({
      token, qrBase: CFG.qr2, qrCam: "qr2", plateCam: "plate2",
      laneId: CFG.laneOut, direction: "OUT", employeeId: emp, plateText, index: `OUT-${i + 1}`
    });
    results.push(rOut);
    if (CFG.paceMs) await delay(CFG.paceMs);
    const pct = 50 + ((i + 1) / N) * 50;
    process.stdout.write(`\rOUT ${i + 1}/${N}  ${rOut.ok ? "OK " : "LOI"} ${rOut.ok ? rOut.steps.totalMs + "ms" : rOut.error}  (${Math.round(pct)}%)   `);
    if (!rOut.ok) console.log("");
  }
  console.log("\n=== XONG 20 OUT ===");

  const t1 = now();
  const totalMs = t1 - t0;

  // ---------- báo cáo ----------
  const ok = results.filter((r) => r.ok);
  const bad = results.filter((r) => !r.ok);
  const avg = ok.length ? ok.reduce((s, r) => s + r.steps.totalMs, 0) / ok.length : 0;
  const avgIn = results.filter((r) => r.direction === "IN" && r.ok);
  const avgOut = results.filter((r) => r.direction === "OUT" && r.ok);
  const avgInMs = avgIn.length ? avgIn.reduce((s, r) => s + r.steps.totalMs, 0) / avgIn.length : 0;
  const avgOutMs = avgOut.length ? avgOut.reduce((s, r) => s + r.steps.totalMs, 0) / avgOut.length : 0;
  const avgQr = ok.length ? ok.reduce((s, r) => s + (r.steps.qrMs || 0), 0) / ok.length : 0;
  const avgVerify = ok.length ? ok.reduce((s, r) => s + (r.steps.verifyMs || 0), 0) / ok.length : 0;
  const avgPlate = ok.length ? ok.reduce((s, r) => s + (r.steps.plateMs || 0), 0) / ok.length : 0;
  const avgScan = ok.length ? ok.reduce((s, r) => s + (r.steps.scanMs || 0), 0) / ok.length : 0;
  const plateMismatch = results.filter((r) => r.ok && r.plateRead !== r.plateFed);
  const plateMismatchIn = results.filter((r) => r.ok && r.plateRead !== r.plateFed && r.direction === "IN");

  console.log("\n\n===============================================");
  console.log("          BÁO CÁO TEST GỬI XE QR");
  console.log("===============================================");
  console.log(`Tổng số lượt: ${results.length} (20 IN + 20 OUT)`);
  console.log(`Thành công  : ${ok.length}`);
  console.log(`Lỗi        : ${bad.length}`);
  if (bad.length) {
    console.log("--- Chi tiết lỗi ---");
    bad.forEach((b) => console.log(`  [${b.index}] ${b.direction} emp=${b.employeeId}: ${b.error} (HTTP ${b.status})`));
  }
  console.log("\n--- Thời gian (chỉ tính lượt thành công) ---");
  console.log(`Trung bình / lượt : ${(avg / 1000).toFixed(2)}s`);
  console.log(`  IN  (vào)       : ${(avgInMs / 1000).toFixed(2)}s`);
  console.log(`  OUT (ra)        : ${(avgOutMs / 1000).toFixed(2)}s`);
  console.log(`Phân rã từng bước (avg):`);
  console.log(`  QR detect       : ${(avgQr / 1000).toFixed(2)}s`);
  console.log(`  Verify          : ${(avgVerify / 1000).toFixed(2)}s`);
  console.log(`  Plate detect    : ${(avgPlate / 1000).toFixed(2)}s`);
  console.log(`  Scan API        : ${(avgScan / 1000).toFixed(2)}s`);
  console.log(`Tổng thời gian 40 lượt: ${(totalMs / 1000).toFixed(1)}s (${(totalMs / 60000).toFixed(2)} phút)`);
  console.log(`Throughput: ${(ok.length / (totalMs / 1000)).toFixed(2)} lượt/giây`);
  console.log(`\nBiển số OCR đọc khác biển số phát (mismatch): ${plateMismatch.length}`);
  plateMismatch.slice(0, 10).forEach((r) => console.log(`  [${r.index}] phát=${r.plateFed} doc=${r.plateRead}`));

  // ghi file report
  const fs = await import("node:fs");
  const report = {
    generatedAt: new Date().toISOString(),
    config: CFG,
    total: results.length, success: ok.length, failed: bad.length,
    avgMsPerPerson: Math.round(avg),
    avgMsIn: Math.round(avgInMs), avgMsOut: Math.round(avgOutMs),
    avgQrMs: Math.round(avgQr), avgVerifyMs: Math.round(avgVerify),
    avgPlateMs: Math.round(avgPlate), avgScanMs: Math.round(avgScan),
    totalMs, throughputPerSec: +(ok.length / (totalMs / 1000)).toFixed(3),
    plateMismatchCount: plateMismatch.length,
    errors: bad.map((b) => ({ index: b.index, direction: b.direction, employeeId: b.employeeId, error: b.error, status: b.status })),
    events: results
  };
  fs.writeFileSync(new URL("./qr-test-report.json", import.meta.url), JSON.stringify(report, null, 2));
  console.log("\nReport: tests/qr-parking-sim/qr-test-report.json");
  process.exit(bad.length ? 2 : 0);
}

main().catch((e) => {
  console.error("FATAL:", e);
  process.exit(1);
});