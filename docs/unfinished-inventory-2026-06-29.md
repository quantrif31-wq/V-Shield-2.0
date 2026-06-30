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

### 1. Load test da co profile chay, nhung van can moi truong that

Tinh trang:
- `API/API/API.Tests/LoadTesting/AuthLoadTests.cs`
- `API/API/API.Tests/LoadTesting/AccessGatewayLoadTests.cs`
- `API/API/API.Tests/LoadTesting/EnterpriseLoadTests.cs`
- `API/API/API.Tests/LoadTesting/StressSoakChaosTests.cs`

Tinh trang hien tai:
- Cac load test da duoc doi sang co che bat bang `ENABLE_LOAD_TESTS=true`.
- Da co script `scripts/run-load-tests.ps1` va tai lieu `docs/load-test-profile.md`.
- Van can moi truong API dang chay, du lieu seed va token/mau phu hop neu muon chay that.

Huong xu ly tiep:
- Dua profile nay vao pipeline rieng neu muon chay dinh ky.
- Chuan hoa seed data va token mau de ket qua giua cac lan chay on dinh hon.

### 2. Con can theo doi mot canh bao build tu thu vien ben thu ba

Tinh trang:
- Frontend da duoc tach chunk cho cac man hinh map nang va khong con canh bao chunk vuot nguong mac dinh.
- `npm run build` van hien mot canh bao Rollup lien quan toi comment `/*#__PURE__*/` trong goi `@microsoft/signalr`.

Anh huong:
- Khong chan build.
- Day la canh bao tu dependency, khong phai loi logic cua ung dung.

Huong xu ly tiep:
- Theo doi khi nang cap `@microsoft/signalr`.
- Chi can can thiep them neu muon build log sach hon hoan toan.

## Cac muc da dong trong dot nay

- `API.Tests` da duoc dua vao `API/API/API.slnx` de chay `dotnet test` o muc solution.
- Frontend da tach them chunk cho `IncidentMapPage` va cau hinh `manualChunks` trong `View/vite.config.js`.
- Android da duoc don cac warning deprecated chinh quanh secure storage, SignalR request body va icon auto-mirrored.
- Load test da khong con bi khoa bang `Skip` cung; da co co che opt-in va script chay rieng.
- Backend startup da qua duoc 2 migration loi `multiple cascade paths` o `UserOperationalScopes` va `VehicleDelegations`.

## Ket qua xac minh moi nhat

- Backend: `dotnet build API/API/API/API.csproj --nologo` pass sach.
- Backend tests: `dotnet test API/API/API.slnx --nologo` pass `116`, skip `27`.
- Backend startup thuc te: `GET /health/live` tra `200` sau khi migrate local SQL Server.
- Load-test smoke thuc te: `HealthEndpoint_HighConcurrency` pass khi bat `ENABLE_LOAD_TESTS=true`.
- Frontend: `npm run build` pass.
- Android: `V-Shield-Mobile\\gradlew.bat assembleDebug` pass.

## Uu tien tiep theo de dat muc "san pham sach hon"

1. Dua load-test profile vao pipeline hoac moi truong staging co seed data on dinh.
2. Theo doi canh bao build tu `@microsoft/signalr` khi nang cap dependency.
