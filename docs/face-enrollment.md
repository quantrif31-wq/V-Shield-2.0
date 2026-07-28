# Controlled employee face enrollment

Commit 11 replaces manual `train_face.py` execution with a video-only,
stateful enrollment pipeline. The public API accepts only an employee ID and an
existing `EmployeeFaceVideo` ID. ASP.NET verifies ownership and constructs the
managed `video_notok/<file>` reference; clients cannot provide paths, model
filenames, checksums, vectors, URLs, RTSP sources, Base64 video, or pickle data.

Jobs move through `Pending`, `Processing`, `Prepared`, `Activating`,
`Completed`, `Failed`, `Cancelled`, or `RecoveryRequired`. A filtered unique
index permits only one non-terminal job per employee. The worker claims a
pending row with a conditional database update and retries runtime transport
failures up to the configured maximum. Quality failures are terminal.

Prepare samples at the configured interval, caps processed frames, accepts only
frames containing exactly one face, validates finite 128-value encodings, and
never deletes the source video. `qualityScore = usableFrameCount /
processedFrameCount`; this deterministic internal quality metric is not
recognition accuracy or an authentication probability. A candidate matching
another active subject below the duplicate threshold is rejected; matching the
same subject is allowed for re-enrollment.

Candidates use a temporary file, flush/fsync and atomic rename under canonical
staging. Candidate references are opaque. Prepare does not reload the registry.
Activation uses deterministic `emp_<employeeId>_v<version>_<job-prefix>.pkl`
names, atomically archives the old model, promotes the candidate and performs
strict reload. Reload failure restores the old model and registry. Camera
sessions are not recreated.

ASP.NET commits `Activating` job/model markers before the runtime call.
Recovery compares expected filename and checksum with the immutable registry:
a confirmed activation finalizes database archive/active states; ambiguous
state becomes `RecoveryRequired` without creating another version.

Prepared jobs require explicit administrator activation. Pending and Prepared
jobs can be cancelled; retry is limited to retryable runtime failures. Model
revoke is deliberately not exposed through ASP.NET/UI in this commit because a
durable revoke request marker is not yet available for safe crash recovery.

Canonical roots are `/data/face/input`, `/data/face/models/staging`,
`/data/face/models/active`, `/data/face/models/archive`, and
`/data/face/models/failed`. The runtime endpoints remain internal behind the
authenticated ASP.NET gateway. This commit does not implement liveness,
guests/VIP, multi-image enrollment, RecognitionEvent, access decisions, or
gate opening. Physical RTSP camera acceptance remains unverified.
