# Face model lifecycle metadata

`EmployeeFaceModels` is the authoritative lifecycle catalog. Face Runtime reads
the canonical active files from `/data/face/models/active` and publishes an
immutable registry snapshot containing subject ID, filename, SHA-256 checksum,
encoding count, and registry version. It never publishes a full path, encoding
vector, pickle content, or service credential.

Lifecycle states are stored as strings: `Pending`, `Prepared`, `Activating`,
`Active`, `Archived`, `Revoked`, and `Failed`. The database enforces unique
filenames, unique `(EmployeeId, Version)`, no more than one `Active` model per
employee, and a unique non-null `SourceEnrollmentJobId`. `RowVersion` provides
optimistic concurrency. Enrollment job linkage remains nullable because
enrollment is not implemented in this commit.

The five adopted models were explicitly bootstrapped as Version 1 and Active.
`CreatedAt` remains the adoption timestamp. `ActivatedAtUtc` is initialized
from that timestamp because adoption made each model the official active model;
it is not a training timestamp. Checksum and encoding count come from the
canonical runtime registry rather than filename parsing or direct ASP.NET
filesystem access.

Bootstrap is never automatic at API startup:

```text
dotnet API.dll face-models bootstrap-metadata --dry-run
dotnet API.dll face-models bootstrap-metadata --apply --confirm-bootstrap
```

Apply requires both flags, validates again inside a serializable transaction,
updates all five rows, and uses rowversion concurrency. A matching rerun returns
`AlreadyBootstrapped`. Conflicting partial metadata is never overwritten.

Administrative read-only APIs use the `identity-mgmt` operational permission:

- `GET /api/FaceModels`
- `GET /api/FaceModels/health`
- `GET /api/Employees/{employeeId}/face-models`

They expose only a checksum prefix and never expose model paths or contents.
Reconciliation states are `Synced`, `MissingInRuntime`,
`UnexpectedInRuntime`, `ChecksumMismatch`, `EncodingCountMismatch`,
`SubjectMismatch`, `DatabaseMetadataMissing`, and `RuntimeUnavailable`.
Reads do not repair, activate, archive, or revoke models.

The legacy source remains unchanged for rollback. Commit 11 adds controlled,
video-only employee enrollment with manual activation, atomic runtime
promotion/rollback and crash-window reconciliation. Model revoke remains
hidden from ASP.NET/UI until a durable revoke request marker supports safe
crash recovery. See `docs/face-enrollment.md`. Physical RTSP camera
connectivity remains unverified.
