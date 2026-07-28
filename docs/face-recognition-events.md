# Face recognition events

Face Runtime emits an immutable event from the same state transition that locks a
completed recognition result. It does not emit per frame. `Recognized` and
`Unknown` are supported; unknown events remain available in the runtime buffer,
but the ASP.NET collector does not persist them by default.

Each event contains a random `eventId`, a per-camera monotonically increasing
`sequence`, `sessionGeneration`, camera/lane and subject identifiers, UTC time,
distance, and the model registry version, filename and 12-character checksum
prefix. Events never contain frames, Base64 images, encodings, full checksums,
model paths, RTSP URLs, credentials or tokens. The existing mutable result and
locked-image endpoints remain unchanged.

## Runtime buffer and API

Each `CameraSession` owns an isolated bounded deque. Defaults are
`FACE_EVENT_BUFFER_SIZE=500` and `FACE_EVENT_RETENTION_SECONDS=3600`; both must
be positive. The incremental endpoint is:

```http
GET /api/cameras/{cameraId}/events?afterSequence=0&sessionGeneration=1&limit=100
```

The limit is 1–200. Events are returned oldest-first and reads do not remove
them. `gapDetected=true` means the requested sequence has expired/been evicted
or the session generation changed. No synthetic event is created to fill a gap.

## Collector and persistence

`FaceRecognitionEventCollector` polls active runtime sessions using a durable
checkpoint per camera. Runtime event IDs are unique in the database, so replay
is idempotent across API restarts. Event insertion and checkpoint advancement
use one relational transaction. A generation reset restarts incremental reading
at sequence zero for the new generation. Gaps, generation resets and payload
conflicts are written as sync warnings.

Recognized subjects are reconciled against Employee, the active
EmployeeFaceModel (filename plus checksum prefix), and
FaceCameraConfiguration.RuntimeCameraId. The canonical database lane wins when
runtime and configuration differ. No missing employee, model or camera record is
created automatically.

Configuration:

```json
"FaceRecognitionEvents": {
  "CollectorEnabled": true,
  "PollIntervalMilliseconds": 1000,
  "BatchSize": 100,
  "MaxParallelCameras": 2,
  "StoreUnknownEvents": false,
  "RetentionDays": 90
}
```

Retention is documented and validated, but cleanup is intentionally deferred.
History endpoints are authenticated and require the `identity-mgmt` operational
task. They are read-only and redact biometric/model internals.

Commit 12 does not create AccessDecision or attendance records, open a gate,
process guests/VIPs, add liveness, publish model revocation, or store face
images/vectors. Physical RTSP camera verification remains a separate operational
test.
