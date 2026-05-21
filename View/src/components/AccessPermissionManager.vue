<template>
  <div class="page">
    <div class="topbar">
      <div>
        <h1>V-Shield Access Permission</h1>
        <p>Phân quyền truy cập khu vực cho Nhân viên và Khách</p>
      </div>
    </div>

    <div class="lane-grid" style="grid-template-columns: 1fr; max-width: 600px;">
      <section class="lane-card">
        <div class="lane-head">
          <div>
            <h2>Cài đặt quyền</h2>
            <p>Gán quyền vào Gate cho đối tượng tương ứng</p>
          </div>
        </div>

        <div class="ip-row">
          <div class="ip-box" style="grid-column: span 2;">
            <label>Loại đối tượng</label>
            <div style="display: flex; gap: 20px; margin-top: 10px;">
              <label>
                <input type="radio" value="employee" v-model="form.targetType" /> Nhân viên
              </label>
              <label>
                <input type="radio" value="visitor" v-model="form.targetType" /> Khách (Visitor)
              </label>
            </div>
          </div>

          <div class="ip-box">
            <label>{{ form.targetType === 'employee' ? 'Employee ID' : 'Visitor Detail ID' }}</label>
            <input type="number" v-model.number="form.targetId" placeholder="Nhập ID..." />
          </div>

          <div class="ip-box">
            <label>Gate ID (Khu vực)</label>
            <input type="number" v-model.number="form.gateId" placeholder="Nhập ID Gate..." />
          </div>

          <div class="ip-box" style="grid-column: span 2;">
            <label>Quyền truy cập</label>
            <div style="display: flex; gap: 20px; margin-top: 10px;">
              <label style="color: #15803d; font-weight: bold;">
                <input type="radio" :value="true" v-model="form.isAllowed" /> CHO PHÉP (IN)
              </label>
              <label style="color: #b91c1c; font-weight: bold;">
                <input type="radio" :value="false" v-model="form.isAllowed" /> TỪ CHỐI (OUT)
              </label>
            </div>
          </div>
        </div>

        <div class="lane-actions" style="margin-top: 20px;">
          <button 
            class="btn btn-main" 
            style="width: 100%; height: 48px;" 
            :disabled="loading" 
            @click="submitPermission"
          >
            {{ loading ? "Đang xử lý..." : "LƯU QUYỀN TRUY CẬP" }}
          </button>
        </div>

        <div class="bottom-note" v-if="message" :class="isSuccess ? 'ok-text' : 'danger-text'">
          <b>Kết quả:</b> {{ message }}
        </div>
      </section>
    </div>
  </div>
</template>

<script>
// Giả lập import axios, bạn thay bằng đường dẫn axios config của project
import axios from "axios";

export default {
  name: "AccessPermissionManager",
  data() {
    return {
      loading: false,
      message: "",
      isSuccess: false,
      form: {
        targetType: "employee", // 'employee' | 'visitor'
        targetId: null,
        gateId: null,
        isAllowed: true
      }
    };
  },
  methods: {
    async submitPermission() {
      if (!this.form.targetId || !this.form.gateId) {
        alert("Vui lòng nhập đầy đủ ID đối tượng và Gate ID");
        return;
      }

      this.loading = true;
      this.message = "";

      const payload = {
        GateId: this.form.gateId,
        IsAllowed: this.form.isAllowed
      };

      if (this.form.targetType === "employee") {
        payload.EmployeeId = this.form.targetId;
      } else {
        payload.VisitorDetailId = this.form.targetId;
      }

      try {
        // Thay URL thành URL thực tế của backend
        const res = await axios.post("/api/AccessPermission/set-permission", payload);
        if (res.data.success) {
          this.isSuccess = true;
          this.message = res.data.message || "Cập nhật thành công!";
        } else {
          this.isSuccess = false;
          this.message = res.data.message || "Lỗi cập nhật.";
        }
      } catch (err) {
        this.isSuccess = false;
        this.message = err.response?.data?.message || err.message || "Không thể kết nối Server.";
      } finally {
        this.loading = false;
      }
    }
  }
};
</script>

<style scoped>
/* Copy toàn bộ CSS từ Vue gốc của bạn bỏ vào đây. Để gọn mình xin phép rút gọn khối style, bạn có thể copy từ bản gốc */
.page { min-height: 100vh; background: #f3f6fb; padding: 20px; font-family: Inter, Arial, sans-serif; color: #0f172a; }
.topbar h1 { margin: 0; font-size: 28px; font-weight: 800; }
.lane-grid { display: grid; gap: 18px; }
.lane-card { background: #ffffff; border: 1px solid #e2e8f0; border-radius: 18px; padding: 16px; box-shadow: 0 8px 24px rgba(15, 23, 42, 0.06); }
.lane-head h2 { margin: 0; font-size: 22px; font-weight: 800; }
.btn { height: 40px; border: none; border-radius: 10px; padding: 0 14px; color: white; font-size: 13px; font-weight: 800; cursor: pointer; }
.btn-main { background: #2563eb; }
.btn:disabled { opacity: 0.6; cursor: not-allowed; }
.ip-row { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; margin-bottom: 14px; }
.ip-box label { display: block; font-size: 12px; font-weight: 700; margin-bottom: 6px; color: #334155; }
.ip-box input { width: 100%; height: 42px; border: 1px solid #cbd5e1; border-radius: 10px; padding: 0 12px; font-size: 14px; outline: none; }
.ok-text { color: #15803d; }
.danger-text { color: #b91c1c; }
</style>