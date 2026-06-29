# Inventory Chua Hoan Tat - 2026-06-29

## Da hoan thanh trong dot nay

- Web frontend build pass sau khi bo import sai trong `IncidentMapPage.vue`.
- Android `assembleDebug` build pass sau khi canh chinh AGP, Kotlin va JDK 17 trong `V-Shield-Mobile`.
- Backend `dotnet build` dat `0 Warning(s), 0 Error(s)`.
- Attendance records da co xuat Excel truc tiep tu giao dien.
- Attendance work schedules da co tao lich hang loat theo nhieu nhan vien va khoang ngay.
- Notification calls trong backend da duoc `await` dung cach o cac luong leave request va vehicle delegation.
- `CameraRecordingService` khong con goi cleanup theo kieu tha noi.

## Con mo sau khi da xac minh build/test

### 1. Load test van dang bi skip

Tinh trang:
- `API/API/API.Tests/LoadTesting/AuthLoadTests.cs`
- `API/API/API.Tests/LoadTesting/AccessGatewayLoadTests.cs`
- `API/API/API.Tests/LoadTesting/EnterpriseLoadTests.cs`
- `API/API/API.Tests/LoadTesting/StressSoakChaosTests.cs`

Ly do:
- Can moi truong API dang chay, du lieu seed va token/mau phu hop.
- Chua duoc dua vao quy trinh kiem tra thuong xuyen.

Huong xu ly tiep:
- Tao script chuan de dung local stack va nap bien moi truong `LOAD_TEST_*`.
- Tach mot profile test rieng de co the bat/tat load test co chu dich.

### 2. Frontend con can toi uu chunk lon

Tinh trang:
- `npm run build` van canh bao chunk lon hon 500 kB.
- Cum chunk lon nhat hien tai nam quanh `IncidentMapPage`, `campusMapApi`, `GateTransitMonitor`.

Anh huong:
- Khong chan build.
- Co the lam tang thoi gian tai trang dau tien hoac route nang.

Huong xu ly tiep:
- Tach them dynamic import cho cac map/workspace nang.
- Can nhac `manualChunks` trong Vite config cho map, signalr va module enterprise lon.

### 3. Android con warning deprecated/unused

Tinh trang:
- `V-Shield-Mobile/app/src/main/java/com/vshield/mobile/data/TokenManager.kt`
- `V-Shield-Mobile/app/src/main/java/com/vshield/mobile/security/SecureStorage.kt`
- `V-Shield-Mobile/app/src/main/java/com/vshield/mobile/ui/navigation/BottomNavBar.kt`
- `V-Shield-Mobile/app/src/main/java/com/vshield/mobile/ui/screen/*`

Anh huong:
- Build van thanh cong.
- Day la no ky thuat, chua phai loi chuc nang.

Huong xu ly tiep:
- Chuyen tu `MasterKeys` sang API hien tai cua `androidx.security`.
- Doi icon deprecated sang `Icons.AutoMirrored.*`.
- Don cac parameter/variable khong dung.

### 4. Load test solution-level chua tu dong chay

Tinh trang:
- `API/API/API.slnx` hien chi chua `API/API.csproj`.
- Test project van chay duoc khi goi truc tiep `API.Tests.csproj`, nhung chua nam trong solution file.

Anh huong:
- De gay hieu nham rang da test day du khi chi build/test o muc solution.

Huong xu ly tiep:
- Can nhac them `API.Tests` vao `API.slnx` neu muon solution-level check bao phu hon.

## Ket qua xac minh moi nhat

- Backend: `dotnet build API/API/API/API.csproj --nologo` pass sach.
- Backend tests: `dotnet test API/API/API.Tests/API.Tests.csproj --nologo` pass `114`, skip `27`.
- Frontend: `npm run build` pass.
- Android: `V-Shield-Mobile\\gradlew.bat assembleDebug` pass.

## Uu tien tiep theo de dat muc "san pham sach hon"

1. Giam chunk frontend lon o cac route map va workspace nang.
2. Don warning deprecated trong Android de tranh no ky thuat tang them.
3. Chuan hoa va kich hoat duoc load-test profile thay vi de `Skip` hoan toan.
4. Dua test project vao `API.slnx` neu muon quy trinh kiem tra dong bo hon.
