# V-Shield 2.0 UI RC2 release manifest

## Release identity

| Field | Value |
|---|---|
| Product | V-Shield 2.0 |
| Release | UI RC2 |
| Branch | `release/v-shield-2.0-ui-rc1` |
| Release commit | `3a357e084344f2fabdc7fb7c3f1f11d92d203a9b` |
| Annotated tag | `v-shield-2.0-ui-rc2` |
| Artifact | `View/release-artifacts/v-shield-2.0-ui-rc2.zip` |
| Artifact SHA-256 | `627b62e7a69e60337d65d8db7443f414498264605700af727ac49d38424fb5c9` |
| Lockfile SHA-256 | `93121d1320e4bd591308d61dc409558e678f1340330ff5f9e8a926df671d5291` |
| Content manifest SHA-256 | `f510614cda0f9f4f40f26ecf54c34613fc01d9924344e6beeb56d8c2e8535bd5` |
| GitHub Actions | `31036497935` — PASS |
| Application marker | `2.0.0-rc2` |

The content manifest digest is SHA-256 over UTF-8 lines sorted by archive path,
formatted as `<file-sha256><two spaces>dist/<path>\n`.

## Artifact evidence

```text
Production files: 196
Uncompressed bytes: 3,945,178
ZIP bytes: 1,219,568
Deterministic timestamp: 2026-08-05T00:00:00Z
Independent archive builds: 2
Byte-for-byte reproducible: YES
Production artifact scan: PASS — 196 files
Sensitive artifact scan: PASS — 198 files
```

Largest production bundles:

| Bundle | Bytes |
|---|---:|
| `maplibre-vendor-DBcK9Xb9.js` | 1,052,971 |
| `three-vendor-CxEsojCc.js` | 543,149 |
| `index-B6YHx1cA.js` | 195,158 |
| `qr-scanner-vendor-BYg87YmP.js` | 130,248 |

## Release gates

```text
Design check: PASS
Unit: PASS — 26/26
Playwright: PASS — 195/195
Functional: PASS — 75/75
Visual Linux: PASS — 120/120
Accessibility: PASS — 50/50
Build: PASS — Vite 7.3.6, 458 modules
Security scans: PASS
npm audit: PASS — 0 vulnerabilities
```

## RC1 preservation

RC1 tag `v-shield-2.0-ui-rc1` remains at
`929c77f257f3490ffe7692a4e59d1e47b5b2ac20`. Its artifact remains immutable.
RC1 is superseded for UAT because its GitHub run did not contain Linux visual
baselines; this does not invalidate the RC1 artifact.

## Readiness

```text
RC2 PACKAGED
GITHUB RELEASE GATES PASS
UAT ADMIN BLOCKED
PRODUCTION NOT READY
```
