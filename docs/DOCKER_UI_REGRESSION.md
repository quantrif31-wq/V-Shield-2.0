# Docker UI Regression (Manual)

Ngay cap nhat: 2026-05-29

## Muc tieu
Xac minh frontend chay trong Docker giu nguyen hanh vi nghiep vu quan trong.

## Dieu kien truoc khi test
1. Chay stack:
   - `docker compose up -d`
   - `docker compose --profile ai up -d`
   - `docker compose --profile ai-heavy up -d`
2. Mo: `http://localhost:5173`
3. Dang nhap tai khoan admin.

## Checklist theo man hinh

### 1) Dang nhap / Dang xuat
- [ ] Dang nhap thanh cong, vao dashboard.
- [ ] Dang xuat thanh cong, quay ve login.
- [ ] Dang nhap lai, session moi duoc tao.

### 2) Pre-registrations
- [ ] Mo trang `/pre-registrations`.
- [ ] Tao moi 1 dang ky (co it nhat 1 guest).
- [ ] Duyet/tu choi 1 ban ghi.
- [ ] Mo link guest, xac nhan hanh vi dung (link con/het han theo trang thai).

### 3) Access Permission Manager
- [ ] Mo trang `/access-permission-manager`.
- [ ] Tab nhan vien: tim kiem, cap quyen, xoa quyen.
- [ ] Tab khach moi: tim kiem, cap quyen, xoa quyen.
- [ ] Kiem tra bo loc ket hop nhan vien/khu vuc hoat dong dung.

### 4) System Audit Logs
- [ ] Mo trang `/system-audit-logs`.
- [ ] Su dung o tim kiem + bo loc hanh dong/ket qua.
- [ ] Click 1 dong de mo panel chi tiet ben phai.
- [ ] Kiem tra co du lieu: user, hanh dong, doi tuong, ket qua, gia tri truoc/sau, thiet bi, vi tri/IP.

### 5) Camera flows
- [ ] Trang QR monitor mo duoc stream qua runtime.
- [ ] Trang gate monitor mo duoc stream QR/plate.
- [ ] Khi runtime chua co URL camera, hien canh bao dung UX.
- [ ] Luong bat/tat camera va scan 1 lan khong vo view.

## Cach ghi nhan ket qua
- Neu pass: danh dau [x] vao muc tuong ung.
- Neu fail: ghi ro:
  - Trang/route
  - Buoc tai hien
  - Ket qua thuc te
  - Ket qua ky vong
  - Anh/chup log lien quan

## Ket luan
- Khi tat ca muc deu [x], co the xem la Docker parity UI da dat muc release noi bo.
