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
- Invalid JSON and empty bodies are still labeled `application/json`.
- ASP.NET request cancellation is forwarded to the Python request and propagates
  as cancellation instead of being converted to HTTP `503`.

## ASP.NET Face Runtime typed client

`FaceCameraController` delegates all Face Runtime HTTP transport to the typed
`IFaceRecognitionClient`/`FaceRecognitionClient`. The client is registered with
`AddHttpClient`, owns method/path selection, forwards cancellation tokens, and
returns the upstream status, raw body, and content type as a
`FaceRuntimeResponse`. The controller continues labeling proxied responses as
`application/json` to preserve the existing public API contract, including
invalid JSON and empty bodies.

Backend-to-runtime route mapping:

| ASP.NET route | Python Face Runtime route |
| --- | --- |
| `POST /api/FaceCamera/camera/on` | `POST /api/camera/on` |
| `POST /api/FaceCamera/camera/off` | `POST /api/camera/off` |
| `POST /api/FaceCamera/camera/reset` | `POST /api/camera/reset` |
| `GET /api/FaceCamera/camera/status` | `GET /api/camera/status` |
| `GET /api/FaceCamera/camera/result` | `GET /api/camera/result` |
| `GET /api/FaceCamera/camera/locked-images` | `GET /api/camera/locked-images` |
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
