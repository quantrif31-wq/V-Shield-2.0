# Batch Y - Residual Naming and Encoding Audit

Date: 2026-05-23

## Scope
Performed targeted audit for residual naming debt and mojibake/encoding artifacts after Batch X.
No runtime behavior changes.

## Generated artifacts
- `analysis_mojibake_hits_2026-05-23.txt`
- `analysis_residual_naming_hits_2026-05-23.txt`

## Key findings
1. Legacy route aliases already removed in backend controller route attributes (`SetCam`, `QR_Dong`, `FaceID` route aliases).
2. Residual legacy-named symbols remain mostly as:
- DTO/class names such as `SetCamRequest`
- Runtime script/process identifiers like `QR_Dong.py`
- UI/internal labels/keys in some frontend files (e.g., `thonghanh`, `tao_qr_d`, `scan_qr_d`).
3. Mojibake strings still present in multiple files (comments, messages, summaries), especially in backend controllers and some frontend comments.

## Validation
- Backend build: PASS
- Frontend build: PASS

## Next step proposal
- Batch Y1: Encoding hygiene in comments/messages only (no identifier rename).
- Batch Y2: Internal symbol rename wave (safe private/local symbols first).
- Batch Y3: DTO/class rename planning for contract-sensitive symbols (compatibility-first).

