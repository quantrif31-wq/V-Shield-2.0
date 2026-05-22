# Backend Folder/File Standardization Plan (Safety-First)

## Scope and Safety Rules
- Project: `V-Shield-2.0` (must keep)
- Backend compile target analyzed: `C:/DoAnTotNghiep/V-Shield-2.0/API/API/API/API.csproj`
- This document focuses on backend folder/file/class naming and cross-file linkage safety.
- Protected names (must keep exactly, no rename in this plan):
- `AI_An_Ninh/test`
- `AI_An_Ninh/cam`

## Current Status Check
- `BienSoController` is no longer present in compiled backend source; current file/class is `LicensePlateController`.
- High-impact hardcoded folder dependency still exists for `AI_Project` in backend runtime path-building.

## Critical Hidden Linkages Found
### Hardcoded `AI_Project` path references
- `C:/DoAnTotNghiep/V-Shield-2.0/API/API/API/Controllers/DeviceManagementController.cs:483`
- `C:/DoAnTotNghiep/V-Shield-2.0/API/API/API/Controllers/FaceIDController.cs:96`
- `C:/DoAnTotNghiep/V-Shield-2.0/API/API/API/Controllers/FaceIDController.cs:97`
- `C:/DoAnTotNghiep/V-Shield-2.0/API/API/API/Controllers/FaceIDController.cs:98`
- `C:/DoAnTotNghiep/V-Shield-2.0/API/API/API/Program.cs:276`
- `C:/DoAnTotNghiep/V-Shield-2.0/API/API/API/Controllers/SetCamController.cs:207`
- `C:/DoAnTotNghiep/V-Shield-2.0/API/API/API/Controllers/SetCamController.cs:347`
- `C:/DoAnTotNghiep/V-Shield-2.0/API/API/API/Services/RuntimeOrchestrator.cs:192`
- `C:/DoAnTotNghiep/V-Shield-2.0/API/API/API/Services/RuntimeOrchestrator.cs:232`

## Folder Naming Standardization Map (Backend Runtime Layer)

### 1) Root AI runtime folder
- Current Name: `AI_Project`
- Type: Runtime folder root
- Suggested English Name: `AI_Runtime`
- Risk: `Critical`
- Why critical:
- Used by hardcoded runtime process launch paths (Python scripts, go2rtc binaries).
- Rename without code update will break runtime at startup/process launch.
- Safe strategy:
1. Introduce one shared config key (example: `RuntimePaths:AiRootFolderName`) and default to current value.
2. Replace all hardcoded `"AI_Project"` with config-based lookup in the 9 locations above.
3. Build and smoke-test runtime launch flows.
4. Rename physical folder to `AI_Runtime`.
5. Update config default value.
6. Rebuild + full smoke test.

### 2) Subfolder normalization candidates (inside AI runtime)
- Current: `doc_bien_gpu` -> Suggested: `license_plate_gpu`
- Current: `face_recognition` -> Suggested: `face_recognition` (keep, already clear)
- Current: `QR_Dong` -> Suggested: `dynamic_qr`
- Current: `cam` -> Suggested: `cam` (keep as-is to reduce risk and because existing runtime binary paths depend on it)
- Current: `test` -> Suggested: `test` (keep)
- Current: `AI_An_Ninh` -> Suggested: optional `ai_security` (only if needed)
- Constraint:
- Even if `AI_An_Ninh` is renamed, do not rename or touch protected subfolder names `test` and `cam` inside it.

## Backend File/Class Naming Standardization Map

### Priority A (high value, manageable risk)
1. Current: `SetCamController.cs` / `SetCamController`
- Suggested: `CameraRuntimeController.cs` / `CameraRuntimeController`
- Risk: `High`
- Dependencies to update:
- route attributes
- frontend API caller paths
- Swagger consumers
- logs/monitoring filters

2. Current: `QR_DongController.cs` / `QR_DongController`
- Suggested: `DynamicQrController.cs` / `DynamicQrController`
- Risk: `High`
- Dependencies to update:
- route paths and frontend calls
- DTO contract names if exposed
- any script path or command arg coupled to current name

3. Current: `FaceIDController.cs` / `FaceIDController`
- Suggested: `FaceRecognitionController.cs`
- Risk: `Medium-High`
- Dependencies to update:
- route + frontend API service imports
- any policy/permission strings keyed by controller name

### Priority B (cleanup-only)
1. Current: `QrAccessController.cs`
- Suggested: `QrAccessController.cs` (keep class, optionally file `QrAccessController.cs` already acceptable)
- Risk: `Safe`

2. Current: `PlateCameraController.cs`
- Suggested: keep (clear enough)
- Risk: `Safe`

## Route Naming Safety Note
- Many controllers still use `[Route("api/[controller]")]`.
- This is not inherently wrong, but route paths become coupled to class names.
- Recommendation for safe refactor:
1. Freeze explicit canonical routes first (e.g. `api/camera-runtime`, `api/dynamic-qr`).
2. Keep temporary compatibility route aliases for 1 release.
3. Update frontend calls.
4. Remove aliases in a later hard-cut.

## Execution Phases (No-break Plan)

### Phase 1: Decouple folder-name hardcode (required before folder rename)
1. Add config-backed AI root folder key.
2. Refactor 9 hardcoded path callsites to use config.
3. Build + runtime smoke test.

### Phase 2: Rename root folder safely
1. Rename `AI_Project` -> `AI_Runtime`.
2. Update config value only.
3. Validate go2rtc + python process launch paths.

### Phase 3: Controller/file standardization with compatibility
1. Rename `SetCamController` -> `CameraRuntimeController` with stable explicit route.
2. Rename `QR_DongController` -> `DynamicQrController` with stable explicit route.
3. Rename `FaceIDController` -> `FaceRecognitionController`.
4. Keep temporary route aliases.
5. Update frontend clients.

### Phase 4: Hard-cut compatibility cleanup
1. Remove temporary alias routes.
2. Remove old client endpoint usage.
3. Final integration test + regression checklist.

## Definition of Done for this backend naming layer
- No hardcoded `"AI_Project"` string remains in compiled backend.
- Backend runs correctly after physical folder rename.
- Target controller/file names are standardized to English.
- No 404 route regressions in frontend-backend integration.
- No runtime launch regressions for go2rtc/python services.
