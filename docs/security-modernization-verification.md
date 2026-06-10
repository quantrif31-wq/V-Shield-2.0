# V-Shield 2.0 Security Modernization Verification

Last checked: 2026-06-10

## Required Local Checks

Run from `C:\DoAnTotNghiep\V-Shield-2.0`:

```powershell
dotnet test API\API\API\API.sln --no-restore --verbosity minimal
```

Expected result:

- `22/22` API tests pass
- `0` warnings
- `0` errors

Run from `C:\DoAnTotNghiep\V-Shield-2.0\View`:

```powershell
npm run build
```

Expected result:

- Vite production build succeeds
- routes are split into lazy-loaded chunks
- no previous oversized single entry-bundle warning

## Database Migration

The modernization adds migration:

- `20260610021424_AddAuthSessionsMfaAndAuditFields`

It adds:

- refresh-token storage with hashed tokens
- MFA fields on `AppUsers`
- access-token versioning fields on `AppUsers`
- structured audit fields on `SystemAuditLogs`

## Production Runtime Requirements

- Set `VSHIELD_JWT_SECRET` to a high-entropy value of at least 32 characters.
- Set `VSHIELD_SEED_ADMIN_USERNAME` and `VSHIELD_SEED_ADMIN_PASSWORD` before first production bootstrap.
- Admin and BaoVe users must complete TOTP MFA setup on first valid login.
- Keep Python and public-domain internals untouched; use API/runtime wrapper controls for future hardening.
