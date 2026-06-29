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

## Ket qua xac minh moi nhat

- Backend: `dotnet build API/API/API/API.csproj --nologo` pass sach.
- Backend tests: `dotnet test API/API/API.Tests/API.Tests.csproj --nologo` pass `114`, skip `27`.
- Frontend: `npm run build` pass.
- Android: `V-Shield-Mobile\\gradlew.bat assembleDebug` pass.

## Uu tien tiep theo de dat muc "san pham sach hon"

1. Chuan hoa va kich hoat duoc load-test profile thay vi de `Skip` hoan toan.
2. Theo doi canh bao build tu `@microsoft/signalr` khi nang cap dependency.
