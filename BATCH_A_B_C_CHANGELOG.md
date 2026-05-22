# BATCH_A_B_CHANGELOG

## Scope
- Phase completed: Batch A + Batch B
- Strategy: rename internal symbols only, avoid API/JSON/DB/config contract changes
- Protected invariants preserved:
  - Project name `V-Shield-2.0`
  - Folder names `AI_Project/test`, `AI_Project/cam`

## Validation Summary
- Backend build: `dotnet build API/API/API/API.csproj` passed after each backend batch
- Frontend build: `npm run build` in `View` passed after each frontend batch
- No route path/name change and no endpoint URL change in this phase

## Batch A (Backend, Safe)

### API Controllers

#### AccessLogsController
- File: `API/API/API/Controllers/AccessLogsController.cs`
- `BuildLogProjectionQuery` -> `BuildAccessLogProjectionQuery`
- `MapLogItem` -> `MapAccessLogItem`
- `IsExceptionLog` -> `IsExceptionAccessLog`
- `GetMethod` -> `GetDetectionMethod`
- Risk: Safe (private/internal helper rename)

#### Program
- File: `API/API/API/Program.cs`
- `EnsureSeedAdmin` -> `EnsureSeedAdminUser`
- `EnsureGo2RtcRunning` -> `EnsureGo2RtcProcessRunning`
- `NormalizeUsername` -> `NormalizeUsernameInvariant`
- Risk: Safe (private static helper rename)

#### UsersController
- File: `API/API/API/Controllers/UsersController.cs`
- `NormalizeUsername` -> `NormalizeUsernameInvariant`
- Risk: Safe

#### DashboardController
- File: `API/API/API/Controllers/DashboardController.cs`
- `StartOfWeek` -> `GetStartOfWeek`
- `GetWeekdayLabel` -> `GetVietnameseWeekdayLabel`
- parameter fix: `GetStartOfWeek` (param) -> `startOfWeek`
- Risk: Safe

#### FaceCameraController
- File: `API/API/API/Controllers/FaceCameraController.cs`
- `ResolveServiceBaseUrl` -> `ResolveNormalizedServiceBaseUrl`
- `ProxyGetAsync` -> `ProxyGetFromServiceAsync`
- `ProxyPostAsync` -> `ProxyPostToServiceAsync`
- `ProxyPostJsonAsync` -> `ProxyPostJsonToServiceAsync`
- `BuildServiceUrl` -> `BuildServiceEndpointUrl`
- Risk: Safe

#### PlateCameraController
- File: `API/API/API/Controllers/PlateCameraController.cs`
- `ResolveServiceBaseUrl` -> `ResolveNormalizedServiceBaseUrl`
- `ProxyGetAsync` -> `ProxyGetFromServiceAsync`
- `ProxyPostAsync` -> `ProxyPostToServiceAsync`
- `ProxyPostJsonAsync` -> `ProxyPostJsonToServiceAsync`
- `BuildServiceUrl` -> `BuildServiceEndpointUrl`
- Risk: Safe

#### SetCamController
- File: `API/API/API/Controllers/SetCamController.cs`
- `EnsureCloudflaredConfig` -> `EnsureCloudflaredTunnelConfig`
- `StartCloudflared` -> `StartCloudflaredTunnel`
- `GetLocalIPAddress` -> `GetLocalIpv4Address`
- `StartPythonScript` -> `StartPythonWorkerScript`
- `StopPythonScript` -> `StopPythonWorkerScript`
- `IsPythonScriptRunning` -> `IsPythonWorkerScriptRunning`
- Risk: Safe

#### FaceIDController
- File: `API/API/API/Controllers/FaceIDController.cs`
- `EnsurePythonRunning` -> `EnsureFaceIdServiceRunningAsync`
- `IsPythonServiceAvailable` -> `IsFaceIdServiceAvailableAsync`
- `BuildPythonUrl` -> `BuildFaceIdServiceUrl`
- `ResolveServiceBaseUrl` -> `ResolveNormalizedServiceBaseUrl`
- `ResolvePythonFolder` -> `ResolveFaceIdPythonFolder`
- `ResolvePythonExecutable` -> `ResolvePythonExecutablePath`
- `StartPythonServer` -> `StartFaceIdPythonServer`
- `StopPythonServer` -> `StopFaceIdPythonServer`
- `ResolveUvicornHost` -> `ResolveUvicornBindHost`
- access modifier normalization for renamed helpers -> `private`
- Risk: Safe

#### DeviceManagementController
- File: `API/API/API/Controllers/DeviceManagementController.cs`
- `BuildCameraQuery` -> `BuildCameraProjectionQuery`
- `NormalizeOptional` -> `NormalizeOptionalText`
- `NormalizeCameraUrl` -> `NormalizeCameraStreamUrl`
- `IsDirectWebStream` -> `IsHttpOrRelativeStreamUrl`
- `ShouldProxyViaGo2Rtc` -> `ShouldProxyStreamViaGo2Rtc`
- `BuildCameraViewUrl` -> `BuildCameraWebViewUrl`
- `ResolveGo2RtcPublicBaseUrl` -> `ResolveGo2RtcPublicBaseEndpoint`
- `ResolvePublicAppBaseUrl` -> `ResolvePublicApplicationBaseUrl`
- `NormalizeBaseUrl` -> `NormalizeUrlBase`
- `TryReloadGo2RtcAsync` -> `TryReloadGo2RtcRuntimeAsync`
- Risk: Safe

## Batch B (Frontend, Medium internal)

### Services

#### thonghanhAPI
- File: `View/src/services/thonghanhAPI.js`
- `api` -> `gateApiClient`
- Risk: Medium (file-local identifier)

#### setcamAPI
- File: `View/src/services/setcamAPI.js`
- `api` -> `setCamApiClient`
- Risk: Medium

#### biensoApi
- File: `View/src/services/biensoApi.js`
- `request` -> `requestWithBaseUrlFallback`
- Risk: Medium

#### authApi
- File: `View/src/services/authApi.js`
- `api` -> `authApiClient`
- Risk: Medium

#### userApi
- File: `View/src/services/userApi.js`
- `api` -> `userApiClient`
- Risk: Medium

#### vehicleApi
- File: `View/src/services/vehicleApi.js`
- `api` -> `vehicleApiClient`
- Risk: Medium

#### employeeApi
- File: `View/src/services/employeeApi.js`
- `api` -> `employeeApiClient`
- Risk: Medium

### Router
- File: `View/src/router/index.js`
- import alias only (no route contract change):
  - `FaceID` (import var) -> `FaceIdSecurity`
  - `bienso` -> `LicensePlateSecurity`
  - `ThongHanh` -> `GatePassageMonitor`
  - `Tao_QR_D` -> `DynamicQrGenerator`
  - `Scan_QR_D` -> `DynamicQrScanner`
- guard readability:
  - lambda arg `r` -> `routeRecord`
  - variable `role` -> `currentUserRole`
  - unused redirect arg removed: `redirect: to =>` -> `redirect: () =>`
- Risk: Medium (internal readability, no path/name change)

### Sidebar
- File: `View/src/components/Layout/Sidebar.vue`
- `toggleGroup` -> `toggleNavGroup`
- `canSeeItem` -> `canAccessNavItem`
- `handleNavClick` -> `handleSidebarNavClick`
- `debouncedSearch` -> `debouncedQuickSearch`
- `searchTimeout` -> `quickSearchTimeout`
- `handleResultClick` -> `handleSearchResultClick`
- `handleClickOutside` -> `handleSearchOutsideClick`
- Risk: Medium (template/script synchronized)

## Deferred for next phases
- High/Critical items intentionally not touched in Batch A/B:
  - controller/resource bilingual names in public contract
  - route `name` and path normalization
  - DTO/JSON key normalization
  - EF/DB object rename
  - config key rename

## Next Gate Before Batch C
1. Snapshot current branch state/tag for rollback
2. Re-run core smoke tests manually (login, sidebar navigation, camera runtime actions)
3. Start compatibility-first plan for any contract-impacting rename

## Batch C (High, cross-layer internal)

### Vehicle domain service abstraction rename
- Files:
  - `API/API/API/Services/VehicleService.cs`
  - `API/API/API/Controllers/VehiclesController.cs`
  - `API/API/API/Program.cs`
- Renames:
  - `IVehicleService` -> `IVehicleManagementService`
  - `VehicleService` -> `VehicleManagementService`
- DI updated:
  - `AddScoped<IVehicleManagementService, VehicleManagementService>()`
- Constructor naming cleanup:
  - `VehiclesController(IVehicleManagementService vehicleManagementService)`
- Risk: High (cross-file + DI + controller coupling)
- Contract impact: None (route/payload unchanged)

### Authentication service abstraction rename
- Files:
  - `API/API/API/Services/IAuthService.cs`
  - `API/API/API/Services/AuthService.cs`
  - `API/API/API/Controllers/AuthController.cs`
  - `API/API/API/Program.cs`
- Renames:
  - `IAuthService` -> `IAuthenticationService`
  - `AuthService` -> `AuthenticationService`
- DI updated:
  - `AddScoped<IAuthenticationService, AuthenticationService>()`
- Constructor naming cleanup:
  - `AuthController(Services.IAuthenticationService authenticationService, ...)`
- Risk: High
- Contract impact: None (`api/Auth/*`, JWT payload unchanged)

### LAN discovery service abstraction rename
- Files:
  - `API/API/API/Services/LanCameraDiscoveryService.cs`
  - `API/API/API/Controllers/FaceIDController.cs`
  - `API/API/API/Program.cs`
- Renames:
  - `ILanCameraDiscoveryService` -> `ILocalNetworkCameraDiscoveryService`
  - `LanCameraDiscoveryService` -> `LocalNetworkCameraDiscoveryService`
- DI updated:
  - `AddScoped<ILocalNetworkCameraDiscoveryService, LocalNetworkCameraDiscoveryService>()`
- Constructor naming cleanup:
  - `FaceIDController(ILocalNetworkCameraDiscoveryService localNetworkCameraDiscoveryService, ...)`
- Risk: High
- Contract impact: None (`api/FaceID/*` unchanged)

## Validation after Batch C
- Backend build: `dotnet build API/API/API/API.csproj` passed (0 errors)
- Existing nullable/runtime warnings remain (baseline), no new compile break introduced by rename batch

## Next boundary
- Next step is `Critical` batch only with compatibility-first strategy:
  - route aliases (old+new)
  - DTO key dual-read/write window
  - config fallback keys
  - DB migration-safe rename script
