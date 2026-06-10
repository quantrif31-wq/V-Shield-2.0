# No-Touch Zones

The following directories and files must NOT be modified:

## Directories (no internal logic changes)
- `AI_Runtime/**` - Python AI runtime (face recognition, QR, plate detection)
- `runtime/**` - Runtime infrastructure

## Scripts (no changes)
- `scripts/setup-public-domain.ps1`
- `scripts/uninstall-public-domain.ps1`
- `scripts/reset-public-domain-appsettings.ps1`
- `scripts/read-public-domain-appsettings.ps1`
- `scripts/update-public-domain-appsettings.ps1`

## Batch Files
- `setup-public-domain.bat`
- `uninstall-public-domain.bat`

## Config Backups
- `API/API/API/appsettings.json.bak.public-domain`

## Allowed Actions Around No-Touch Zones
- Wrap with API-layer controls
- Restrict access around them
- Add gateway validation before requests reach them
- Add observability around them
- Add timeout, retry, circuit-breaker, watchdog, or health wrappers
- Add compensating controls and segmentation
