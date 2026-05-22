# RENAME_EXECUTION_PLAN_v1 - V-Shield-2.0

## 1) Objective
Chuẩn hóa naming tiếng Anh theo batch an toàn, không đổi tên project `V-Shield-2.0`, không đổi tên thư mục `AI_Project/test` và `AI_Project/cam`.

## 2) Global Guardrails
- Không rename một lượt toàn bộ.
- Mỗi batch phải pass test gate rồi mới qua batch tiếp theo.
- Với `Critical`, bắt buộc chiến lược tương thích tạm (compatibility window).
- Mọi rename đều có rollback point.

## 3) Execution Order
1. Batch A: Internal `Safe`
2. Batch B: Internal `Medium`
3. Batch C: Cross-module `High Risk`
4. Batch D: Contract `Critical` (API/JSON/DB/config/runtime string)

## 4) Test Gate (bắt buộc sau mỗi batch)
- API compile: `dotnet build API/API/API/API.csproj`
- Frontend build: `npm run build` trong `View`
- API smoke:
  - `GET /health`
  - `GET /hubs/employee-stats` handshake
  - route checklist từ `analysis_api_routes.txt`
- Frontend smoke:
  - login
  - điều hướng các route chính trong `View/src/router/index.js`
  - gọi API từ `View/src/services/*`
- Runtime smoke:
  - Face/Plate/QR service endpoint ping theo `AiServices` config
  - SetCam runtime actions (start/stop/status)
- DB smoke:
  - chạy truy vấn CRUD các entity chính qua API

## 5) Batch Definition

## Batch A - Safe (internal low fan-out)
Scope:
- local variable/method private, helper names, không nằm trong route/DTO/DbSet/JSON key.

Action:
- Rename bằng IDE symbol rename theo file/module nhỏ.
- Không đổi public API contract.

Rollback:
- Revert commit batch nếu fail gate.

Exit Criteria:
- Build API + FE pass.
- Smoke pass.

## Batch B - Medium (internal medium fan-out)
Scope:
- method/property/class nội bộ có tham chiếu vừa phải.
- component/service name không ảnh hưởng endpoint contract trực tiếp.

Action:
- Rename theo domain: Employee, Vehicle, AccessLog, Camera, Registration.
- Chuẩn hóa casing (PascalCase/camelCase/snake_case theo ngôn ngữ).

Rollback:
- Revert toàn batch hoặc từng domain commit.

Exit Criteria:
- Tất cả gate pass + không regression UI route.

## Batch C - High Risk (cross-layer)
Scope:
- controller/class/service namespace liên quan nhiều file.
- ví dụ: `BienSo*`, `ThongHanh*`, `FaceID*`, `QR_Dong*`.

Action:
- Rename code symbol trước, giữ nguyên route cũ tạm thời.
- Tạo alias layer trong FE service nếu cần.
- Cập nhật import/ref toàn cục theo `analysis_symbol_references_clean.csv`.

Rollback:
- Revert commit domain tương ứng.

Exit Criteria:
- API/FE/runtime smoke pass, không đứt navigation.

## Batch D - Critical (contract)
Scope:
- API route, JSON key, config key, DB object mapping, hardcoded runtime string/path.

Action bắt buộc:
- API route: hỗ trợ dual route (old + new) trong 1 giai đoạn.
- JSON field: DTO tương thích ngược (đọc cả old/new trong migration window).
- Config key: fallback đọc key cũ nếu key mới chưa có.
- DB: EF migration có script up/down + data validation.
- Runtime string/path: chuyển về constants/options; không giữ string rải rác.

Rollback:
- Route switchback về old contract.
- Rollback migration (down script) nếu lỗi.

Exit Criteria:
- Dual-contract hoạt động.
- Sau window ổn định mới remove contract cũ.

## 6) Naming Standard Target
- C#: PascalCase type/member, camelCase local/param, interface `I*`, async suffix `Async`.
- JS/Vue: camelCase function/var, PascalCase component, route `name` dạng lowerCamelCase.
- Python: snake_case, class PascalCase.
- Tránh mixed-language trong code symbol; giữ tiếng Việt cho nội dung hiển thị.

## 7) Candidate Migration Map (v1 ưu tiên)
1. `BienSoController` -> `LicensePlateController` (High->Critical nếu đổi route)
2. `ThongHanhController` -> `GateTransitController` (High->Critical)
3. `QR_DongController` -> `DynamicQrController` (High->Critical)
4. FE route name `tao_qr_d` -> `createDynamicQr` (Critical)
5. FE route name `scan_qr_d` -> `scanDynamicQr` (Critical)
6. `FaceIDController` -> `FaceIdController` (High)
7. `setcamAPI.js` -> `cameraRuntimeApi.js` (Medium/High)

## 8) Sprintized Rollout Suggestion
1. Sprint 1: Batch A + một phần Batch B (domain Users/Departments/Positions)
2. Sprint 2: phần còn lại Batch B + Batch C (controller/service bilingual group)
3. Sprint 3: Batch D với compatibility window
4. Sprint 4: remove deprecated names/contracts, cleanup

## 9) Required Artifacts Before Touching Code
- `MASTER_RENAME_MAP_VSHIELD.md`
- `analysis_name_declarations.csv`
- `analysis_symbol_references_clean.csv`
- `analysis_master_map_seed.csv`
- `analysis_api_routes.txt`
- `analysis_frontend_routes_services.txt`

## 10) Go/No-Go Rule
Chỉ Go batch tiếp theo khi batch hiện tại pass toàn bộ gate. Nếu fail bất kỳ gate nào: dừng, rollback, cập nhật map, chạy lại.
