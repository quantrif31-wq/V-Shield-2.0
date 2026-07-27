# Current Face ID frontend contract

This document records the Flask response fields consumed by the current Vue
frontend. It is a characterization artifact, not a proposed API.

## `POST /api/camera/on`

Used fields:

- `success`
- `message`
- `ip`
- `stream_url`

## `POST /api/camera/off`

Used fields:

- `success`
- `message`

## `POST /api/camera/reset`

Used fields:

- `success`
- `message`

## `GET /api/camera/status`

Used fields:

- `success`
- `camera_enabled`
- `camera_connected`
- `ip`
- `tracking_active`
- `identity_confirmed`
- `face_match`
- `employee_id`
- `confirm_count`
- `distance`
- `last_seen`
- `bbox`
- `timeout`
- `alert`
- `scan_locked`
- `lock_reason`
- `fps`
- `models_loaded`
- `total_encodings`
- `message`
- `last_update`
- `stream_url`

## `GET /api/camera/result`

Used fields:

- `success`
- `camera_enabled`
- `camera_connected`
- `ip`
- `tracking_active`
- `identity_confirmed`
- `face_match`
- `employee_id`
- `confirm_count`
- `distance`
- `last_seen`
- `bbox`
- `timeout`
- `alert`
- `scan_locked`
- `lock_reason`
- `fps`
- `message`
- `last_update`
- `last_snapshot`
- `last_face_crop`
- `locked_snapshot`
- `locked_face_crop`

## `GET /api/camera/locked-images`

Used fields:

- `success`
- `identity_confirmed`
- `employee_id`
- `scan_locked`
- `lock_reason`
- `locked_snapshot`
- `locked_face_crop`

## Current error behavior preserved by tests

- Transport exceptions from the Python service become HTTP `503` and include
  the exception message.
- Python HTTP status codes and response bodies are proxied unchanged.
- The upstream response status, raw body, and content type are preserved.
- ASP.NET request cancellation is forwarded to the Python request and propagates
  as cancellation instead of being converted to HTTP `503`.

## ASP.NET Face Runtime typed client

`FaceCameraController` delegates all Face Runtime HTTP transport to the typed
`IFaceRecognitionClient`/`FaceRecognitionClient`. The client is registered with
`AddHttpClient`, owns method/path selection, forwards cancellation tokens, and
returns the upstream status, raw body, and content type as a
`FaceRuntimeResponse`. The controller preserves the upstream content type and
falls back to `application/json` only when the runtime omits it.

Backend-to-runtime route mapping:

| ASP.NET route | Python Face Runtime route |
| --- | --- |
| `POST /api/FaceCamera/camera/on` | `POST /api/cameras/default/start` |
| `POST /api/FaceCamera/camera/off` | `POST /api/cameras/default/stop` |
| `POST /api/FaceCamera/camera/reset` | `POST /api/cameras/default/reset` |
| `GET /api/FaceCamera/camera/status` | `GET /api/cameras/default/status` |
| `GET /api/FaceCamera/camera/result` | `GET /api/cameras/default/result` |
| `GET /api/FaceCamera/camera/locked-images` | `GET /api/cameras/default/locked-images` |
| `GET /api/FaceCamera/models` | `GET /api/models` |
| `POST /api/FaceCamera/models/reload` | `POST /api/models/reload` |

All ASP.NET routes above require authentication and the existing `monitoring`
operational permission. The reload proxy rejects a request body before calling
the runtime.

`AiServices:FaceCameraBaseUrl` is required and is validated as an absolute HTTP
or HTTPS URI at startup. A host-only value is normalized to an `/api/` base;
configured paths have one trailing slash. `AiServices:FaceCameraTimeoutSeconds`
must be greater than zero and defaults to 100 seconds, matching the previous
default `HttpClient` timeout. Connection/DNS/socket failures and client timeouts
become HTTP `503`; upstream HTTP 4xx/5xx responses remain proxy responses.

Commit 4 does not add retry or a circuit breaker because start, stop, reset, and
reload do not yet have an idempotency design. `FACE_SERVICE_TOKEN` remains
unenforced and no backend service-token header is prepared in this commit.

## Face Runtime environment configuration

The Flask runtime reads configuration from process environment variables.
Defaults preserve the pre-configuration behavior:

| Variable | Default | Unit |
| --- | --- | --- |
| `FACE_MODEL_DIR` | `API/API/API/wwwroot/uploads/VideoFace/FaceID` from repository root | path |
| `FACE_SNAPSHOT_DIR` | unset; snapshots remain Base64 in memory | path |
| `FACE_THRESHOLD` | `0.35` | face distance |
| `FACE_CONFIRM_FRAMES` | `5` | frames |
| `FACE_LOST_TIMEOUT` | `2.0` | seconds |
| `FACE_ENCODE_INTERVAL` | `0.7` | seconds |
| `FACE_FRAME_WIDTH` | `480` | pixels |
| `FACE_ROTATION` | `-90` | degrees/mode |
| `FACE_RECOGNIZE_TIMEOUT` | `5.0` | seconds |
| `FACE_ALERT_TIMEOUT` | `8.0` | seconds |
| `FACE_STREAM_WIDTH` | `640` | pixels |
| `FACE_STREAM_HEIGHT` | `360` | pixels |
| `FACE_JPEG_QUALITY` | `80` | OpenCV quality, 0–100 |
| `FACE_MAX_CAMERAS` | `2` | concurrently active sessions |
| `FACE_SERVICE_TOKEN` | unset; not enforced in Commit 2 | secret string |

`PORT` (`5001`) and `HEADLESS_MODE` (`true`) are existing runtime settings
which are now parsed by the same configuration module.

Relative model and snapshot paths resolve from repository root, determined from
the configuration module's file location. Resolution does not depend on the
current working directory or the repository directory name. A missing model
directory remains missing and produces the existing no-model warning; the
runtime does not create or overwrite it.

## Face model registry

The Flask runtime publishes an immutable `RegistrySnapshot` containing one
version of both subject IDs and encodings. Recognition reads the snapshot once
per comparison, so IDs and encodings cannot come from different reload
versions. Snapshot collections are tuples and encoding arrays use read-only
bytes-backed storage.

Startup remains tolerant: a missing, empty, or entirely invalid model
directory starts the service with zero models and reports sanitized warnings.
If some startup files are valid and others are invalid, the valid files are
available and the invalid files are recorded as errors.

Reload is strict. A candidate snapshot is built outside the swap lock. If any
candidate file has an error, the candidate is rejected and the active snapshot
and version remain unchanged. A successful candidate is swapped in a short
critical section. Concurrent readers continue using their complete old
snapshot until the swap. A second simultaneous reload receives
`RELOAD_IN_PROGRESS`.

### `GET /api/models`

Returns registry metadata:

- `version`
- `loadedAt` as UTC ISO 8601
- `modelDirectory` as the sanitized directory basename
- `successfulFileCount`
- `encodingCount`
- `errorCount`
- `models`, containing only `fileName`, `subjectId`, and `encodingCount`
- `errors`, containing only `fileName`, `errorCode`, and a sanitized `message`

The response does not expose encodings, image data, service tokens, or
per-model absolute paths.

### `POST /api/models/reload`

The endpoint accepts no request body:

- HTTP `200`: strict reload succeeded and the version increased.
- HTTP `400`: a request body was supplied.
- HTTP `409`: another reload is already running.
- HTTP `422`: model validation/loading failed; the old snapshot remains active.
- HTTP `500`: unexpected internal failure with a sanitized response.

`FACE_SERVICE_TOKEN` is still not enforced in Commit 3.

### Pickle safety

`.pkl` deserialization is not safe for untrusted input. Extension and path
validation do not make pickle safe. Model files must be generated by the
trusted system, and write access to `FACE_MODEL_DIR` must be restricted.
The reload API never accepts a filename, path, upload, or pickle content.
A future migration should replace pickle with a non-executable model format.

## Authenticated frontend routing

The Vue Face Camera service now uses the shared ASP.NET HTTP client. That client
uses `VITE_API_BASE_URL`, attaches the current JWT, and preserves the existing
single-flight refresh-token behavior. `faceApi.js` still returns `response.data`,
so camera response fields consumed by the components are unchanged.

Frontend calls use `/FaceCamera/...` because the shared client base URL already
ends in `/api`. ASP.NET then forwards the same operation to Python:

| Frontend service route | ASP.NET route | Python Face Runtime route |
| --- | --- | --- |
| `POST /FaceCamera/cameras/{cameraId}/start` | `POST /api/FaceCamera/cameras/{cameraId}/start` | `POST /api/cameras/{cameraId}/start` |
| `POST /FaceCamera/cameras/{cameraId}/stop` | `POST /api/FaceCamera/cameras/{cameraId}/stop` | `POST /api/cameras/{cameraId}/stop` |
| `POST /FaceCamera/cameras/{cameraId}/reset` | `POST /api/FaceCamera/cameras/{cameraId}/reset` | `POST /api/cameras/{cameraId}/reset` |
| `GET /FaceCamera/cameras/{cameraId}/status` | `GET /api/FaceCamera/cameras/{cameraId}/status` | `GET /api/cameras/{cameraId}/status` |
| `GET /FaceCamera/cameras/{cameraId}/result` | `GET /api/FaceCamera/cameras/{cameraId}/result` | `GET /api/cameras/{cameraId}/result` |
| `GET /FaceCamera/cameras/{cameraId}/locked-images` | `GET /api/FaceCamera/cameras/{cameraId}/locked-images` | `GET /api/cameras/{cameraId}/locked-images` |
| `GET /FaceCamera/models` | `GET /api/FaceCamera/models` | `GET /api/models` |
| `POST /FaceCamera/models/reload` | `POST /api/FaceCamera/models/reload` | `POST /api/models/reload` |

The frontend classifies `401` as an expired session and leaves refresh/login
handling to the shared client, `403` as missing monitoring permission, and
`503` as Face Runtime unavailable. Validation (`400`), reload conflict (`409`),
rejected models (`422`), server errors (`500`), backend connection failures, and
request cancellation remain distinct. Polling displays one current error state
instead of producing repeated alerts or stack-trace logs; successful polling
clears the state without a page reload.

`getModels()` and `reloadModels()` are available from the frontend service.
Reload sends no request body and accepts no path or filename.

The obsolete frontend Face Runtime base URL setting has been removed from
frontend environment files, Docker build arguments, and Compose. Nginx no
longer has a direct Face Runtime proxy. Face camera state is isolated per
validated camera ID.
`FACE_SERVICE_TOKEN` remains unenforced.

### Exposed Face Camera screen

The primary Face ID screen is `FaceCamera.vue`, exposed at
`/monitoring/face-camera` and from the `Nhận diện khuôn mặt` sidebar entry.
The route inherits the authenticated main layout, is limited to the existing
`Admin` and `BaoVe` roles, and uses the existing `monitoring` operational task.
The frontend guard controls navigation visibility and early routing only;
ASP.NET remains the final authorization boundary.

The camera source is entered by an authorized operator. It is not embedded in
the route, hard-coded in source, or newly persisted in browser storage.
`ThongHanh.vue` remains an unrouted legacy component while the active gate
transit workflow remains `GateTransitMonitor.vue`; its two face lanes now use
separate camera sessions.

RTSP URLs are not assigned directly to an HTML image element because browsers
cannot decode RTSP. The screen registers the source through the existing camera
runtime API and renders the go2rtc MJPEG preview URL. Preview load/error events
now represent the media request rather than merely loading a wrapper page, and
one delayed retry covers the short go2rtc restart window. A failed source keeps
the screen active and displays a camera/network/go2rtc diagnostic message.

Local integration verification confirmed:

- an unauthenticated visit redirects to
  `/login?redirect=/monitoring/face-camera`;
- an authorized guard can load the route;
- the browser requested `GET /api/FaceCamera/cameras/monitoring-face-camera/status` and
  `GET /api/FaceCamera/models`;
- model metadata returned HTTP `200`, a version, 5 model files, and 665
  encodings, without vectors, tokens, or absolute model paths;
- stopping Face Runtime produced HTTP `503` and the inline
  `Face ID unavailable` state without logout or repeated alerts;
- restarting Face Runtime allowed the next model request to succeed without a
  page reload;
- no Face Camera request used `/face-api`, port `5001`, or the
  `face-runtime` hostname.

The production build contains a separate `FaceCamera-*.js`/`.css` lazy chunk.

## Docker network isolation

Browser requests terminate at frontend/Nginx and Face Camera operations use
the authenticated ASP.NET `/api/FaceCamera/...` routes. Nginx no longer proxies
any path directly to Python, and the frontend bundle has no Face Runtime
hostname, port, or deployment variable.

ASP.NET and `face-runtime` share the deterministic `vshield-face-backend`
bridge. The backend uses `http://face-runtime:5001/api`. The runtime declares
container port `5001` with Compose `expose`, but neither the default nor VPS
Compose publishes that port to the host. The frontend is not attached to this
bridge.

The bridge deliberately does not use `internal: true`: Face Runtime must retain
outbound access to RTSP cameras on the LAN. Absence of a published port prevents
host/LAN inbound access while the normal bridge permits outbound camera
connections.

Docker checks runtime liveness from inside the container with Python standard
library code calling `GET /api/health`. This checks the Flask process/API only;
it neither contacts a camera nor reloads models. ASP.NET does not depend on the
runtime becoming healthy, so the main application can start and continue
returning the established `503` response while Face Runtime is unavailable.

The legacy FastAPI `FaceID.py`, `FaceRecognitionController`, Docker service
`faceid-runtime`, autostart configuration, and port `8000` have been removed.
The only supported Face ID runtime is Flask `nhandienface.py` in
`face-runtime`; `/api/FaceCamera/...` is the only ASP.NET Face ID surface.
The retired `/api/face-recognition/*` routes are no longer mapped.

`FACE_SERVICE_TOKEN` is still not enforced. Physical RTSP camera connectivity
has not been accepted as passing. Preview remains routed through go2rtc, and
this network change does not alter camera data, model files, or go2rtc
configuration.

The physical test source at the time of verification was not reachable from
the Docker host: ICMP, TCP, and direct FFmpeg connectivity to its RTSP endpoint
timed out, and both go2rtc and Face Runtime reported connection timeouts. The
host and camera addresses are in the same configured `/8` subnet and the host
can resolve the camera MAC address, so remaining checks are the camera app's
LAN/server setting, Wi-Fi client isolation, device firewall, single-client
limits, and the RTSP port/path. The UI now reports this condition accurately.

## Commit 8: isolated multi-camera sessions

Face Runtime now owns a thread-safe `CameraManager` containing independent
`CameraSession` instances keyed by validated `cameraId`. Each session owns its
stream URL and lane metadata, capture and recognition workers, stop event,
latest frame, recognition/confirmation state, lost-face timing, cooldown,
locked images, errors, locks, and generation token. Mutable camera state is not
shared. A generation change on stop, reset, or restart prevents an old worker
from publishing frames or recognition state into the new session generation.

The immutable Face model registry remains shared by all sessions. Each
recognition comparison reads exactly one complete registry snapshot. Model
reload does not restart camera sessions, and a rejected reload leaves the
previous snapshot active for every session.

`FACE_MAX_CAMERAS` is a positive integer with default `2`. It limits
concurrently enabled sessions; stopped sessions do not consume capacity.
Starting the same `cameraId` with the same URL is idempotent. Starting an active
ID with a different URL, or exceeding the limit, returns HTTP `409`. Invalid
IDs/body return `400`; unknown GET/stop/reset operations return `404`.

The camera-specific Python routes are:

- `GET /api/cameras`
- `POST /api/cameras/{cameraId}/start`
- `POST /api/cameras/{cameraId}/stop`
- `POST /api/cameras/{cameraId}/reset`
- `GET /api/cameras/{cameraId}/status`
- `GET /api/cameras/{cameraId}/result`
- `GET /api/cameras/{cameraId}/locked-images`

The ASP.NET gateway exposes the corresponding authenticated routes below
`/api/FaceCamera/cameras`, keeps the `monitoring` operational permission, and
uses the typed Face Runtime client. Status, body, and content type are proxied
without parsing. Transport failure remains HTTP `503`, while caller
cancellation still propagates.

The transitional Python `/api/camera/*` and ASP.NET
`/api/FaceCamera/camera/*` routes remain available and map to the same session
with `cameraId=default`; there is no second legacy state store. They will be
removed only after all consumers migrate.

`FaceCamera.vue` accepts stable `cameraId` and `laneId` props and defaults to
`monitoring-face-camera`. The legacy, currently unrouted `ThongHanh.vue` uses
the independent IDs `lane-1-face` and `lane-2-face`. All requests continue
through the authenticated ASP.NET client; neither component contacts Python
directly.

Session configuration and in-memory recognition state are not persisted across
a Face Runtime restart. Physical RTSP connectivity is not accepted as passing.
Liveness detection, enrollment, RecognitionEvent/database audit, VIP/appointment
logic, and gate-opening integration are not part of Commit 8.
