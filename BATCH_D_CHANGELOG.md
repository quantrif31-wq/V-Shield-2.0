# Batch D - Naming Alignment + Compatibility Guards

Date: 2026-05-22

## Changes
- Renamed gate transit controller symbols to clearer English names:
  - `GateController` -> `GateTransitController`
  - `GateScanRequest` -> `GateTransitScanRequest`
  - `GateApiResponse` -> `GateTransitApiResponse`
- Added explicit legacy route to preserve old clients:
  - `[Route("api/Gate")]`
- Kept backward-compatible alias class `GateApiResponse` to avoid cross-controller breakage during migration.
- Normalized controller dependencies to new response type:
  - `AccessPermissionController` now uses `GateTransitApiResponse`
  - `QrAccessController` now uses `GateTransitApiResponse`
- Renamed files to match current type names:
  - `Controllers/ThongHanhController.cs` -> `Controllers/GateTransitController.cs`
  - `Services/IAuthService.cs` -> `Services/IAuthenticationService.cs`
  - `Services/VehicleService.cs` -> `Services/VehicleManagementService.cs`
  - `Services/LanCameraDiscoveryService.cs` -> `Services/LocalNetworkCameraDiscoveryService.cs`

## Verification
- `dotnet build API.sln`: succeeded (0 errors)
- `npm run build` (View): succeeded

## Risk
- Runtime/API risk: Low (legacy routes + response alias preserved)
- Refactor safety: Improved by removing file/type name drift
