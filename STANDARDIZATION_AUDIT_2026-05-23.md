# Standardization Audit - 2026-05-23

## Completed in this batch
- Pushed latest code to `origin/main` (including plan doc).
- Renamed backend DTO:
  - `SetCamRequest` -> `CameraUpsertRequest`
  - File: `API/API/API/DTOs/CameraUpsertRequest.cs`
  - References updated in `CameraRuntimeController`.
- Renamed local frontend alias in settings page:
  - `getSetCamList` -> `getCameraList`.
- Replaced legacy route path in sidebar:
  - `/setcam` -> `/settings?tab=camera`.

## Validation results
- Backend build: PASS (`dotnet build`), warnings only (pre-existing nullable warnings).
- Frontend build: PASS (`npm run build`), chunk-size warning only.
- Global scan for `setcam|SetCam`: no remaining matches.

## Remaining standardization targets (next safe steps)
1. Runtime keys still in Vietnamese-style naming
- `python_cam_gia_lap`
- Methods and state around `CamGiaLap` in:
  - `API/API/API/Controllers/CameraRuntimeController.cs`
  - `API/API/API/Services/RuntimeOrchestrator.cs`
  - `View/src/components/GateTransitMonitor.vue`

2. Non-English or temporary labels/comments in code identifiers/notes
- Example marker in `View/src/components/ThongHanh.vue`:
  - `Ð?I TÊN FILE SERVICE ? ÐÂY LÀ XONG`
- Several internal variable names/messages without consistent naming policy (need controlled pass).

3. Display text policy (Vietnamese UI)
- Keep Vietnamese UI strings as product language.
- Only standardize code identifiers/paths/events to English.
- Continue manual, line-by-line edits for any VN text correction; no bulk auto transform.

## Safe execution order (proposed next)
1. Rename runtime key family `python_cam_gia_lap` to `python_cam_simulator` with compatibility mapping to avoid breakage.
2. Update API endpoints/method names containing `CamGiaLap` with backward-compatible aliases.
3. Update frontend monitor bindings to new runtime keys.
4. Run backend + frontend builds after each sub-step.
5. Remove compatibility aliases only after one full green pass.
