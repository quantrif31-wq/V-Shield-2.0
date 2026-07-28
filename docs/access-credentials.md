# Canonical employee credentials

`AccessCredential` is the canonical enterprise metadata record for a credential owned by
exactly one employee. It does not replace device authentication, visitor credentials, or
the secret storage used by `EmployeeDynamicQr`.

## Boundaries

- `DeviceCredential` belongs to a security device and remains device/API authentication.
- `VisitorCredential` belongs to a visit and remains a visitor subsystem record.
- `EmployeeDynamicQr` remains the source of the QR secret and current QR access behavior.
- `AccessCredential` stores ownership, type, lifecycle, an effective window, and policy
  context. It never stores a QR secret, card plaintext, face encoding, model, image, or video.
- Creating a credential does not create an `AccessRule`, `EmployeeAccessPermission`,
  `AccessDecision`, gate command, or attendance record.

Supported canonical types are `DynamicQr`, `Card`, and `FaceBiometric`. They are stored as
stable strings matching `AccessRule.CredentialType`. Unknown rule values remain untouched
and are reported by inventory.

Stored statuses are `Pending`, `Active`, `Inactive`, and terminal `Revoked`. Effective
status is calculated for the supplied UTC evaluation time. `EffectiveFromUtc` is inclusive;
`ExpiresAtUtc` is exclusive. Expiration is calculated and is not written back by a worker.

Card identifiers are normalized, domain-separated by credential type, and protected with
HMAC-SHA-256 using `AccessCredentials:IdentifierHmacKey` (environment variable
`AccessCredentials__IdentifierHmacKey`). Only a masked suffix is returned. The real key
must come from a secret store and is not committed.

## Dynamic QR migration strategy

1. **Foundation:** create the canonical table and credential-aware evaluator. Existing QR
   controllers remain independent; there is no backfill.
2. **Controlled linking:** inventory QR rows, obtain approval, then create `DynamicQr`
   metadata referencing `EmployeeDynamicQrId`. Never copy or hash `SecretKey`.
3. **Controlled cutover:** only after separate characterization, rollout, and rollback
   testing may QR evaluation resolve canonical metadata.

Run the read-only inventory with:

```text
dotnet run --project API/API/API -- access-credentials inventory
```

The ignored report is written to
`runtime/face-data/manifests/access-credential-inventory.json` and contains no secret or
raw card identifier.

## Enterprise evaluation

The credential-aware evaluator accepts an immutable `AccessCredentialContext`, rejects
ownership or lifecycle failures before policy matching, then uses the canonical type with
the existing rule precedence, schedules, temporary grants, timezone, and active policy
selection. The existing type-only overload remains compatible. Face comparison still
supplies no credential until the separate face-binding commit; old comparison snapshots
remain immutable.

This foundation does not select a canonical policy, bind face models, create access
decisions, open a gate, or write attendance. Physical RTSP camera verification remains
outside this change.

## Development migration verification

The SQL Server migration chain uses `NO ACTION` for credential ownership and
actor relationships, preventing parallel referential-action paths without
weakening ownership. Development inventory after migration contains zero
`AccessCredentials`; no QR row was backfilled. There are 175 active Dynamic QR
rows, no inactive/orphan/multiple-owner rows, and Employee IDs 1–5 each retain
one active QR row and zero canonical credentials. The QR flow remains
independent, Face binding is not implemented, and physical RTSP verification is
still pending.
