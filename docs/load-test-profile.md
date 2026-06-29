# Load Test Profile

## Muc tieu

Bo load test trong `API/API/API.Tests/LoadTesting` da co the bat chay co chu dich, thay vi bi `Skip` cung trong ma.

## Cach chay nhanh

1. Khoi dong API that va dam bao co seed data phu hop.
2. Chay script:

```powershell
.\scripts\run-load-tests.ps1 -Suite all -BaseUrl http://localhost:5107
```

## Cac suite co san

- `all`: toan bo load test
- `auth`: nhom dang nhap, refresh token, health
- `access`: nhom cong, QR, barrier
- `enterprise`: nhom SOC, evidence, operations
- `chaos`: nhom stress, soak, chaos

## Bien moi truong ho tro

- `ENABLE_LOAD_TESTS=true`
- `LOAD_TEST_URL=http://localhost:5107`
- `LOAD_TEST_ADMIN_TOKEN=...`
- `LOAD_TEST_REFRESH_TOKEN=...`
- `LOAD_TEST_DURATION_SECONDS=30`
- `LOAD_TEST_CONCURRENCY=10`
- `LOAD_TEST_WARMUP_SECONDS=3`

## Luu y van con lai

- Cac test nay van can API dang chay that, du lieu seed va token phu hop.
- `RefreshToken_LoadTest` can them `LOAD_TEST_REFRESH_TOKEN`.
- Bai `Chaos_DatabaseRestart` van can tac dong ha tang ben ngoai neu muon kiem thu day du kha nang phuc hoi DB.
