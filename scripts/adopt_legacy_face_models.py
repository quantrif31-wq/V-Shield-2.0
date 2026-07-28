#!/usr/bin/env python3
"""Explicit, operator-approved adoption of trusted legacy face models."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import sys
import tempfile
import uuid
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable, Protocol

from face_model_inventory import DatabaseModelRow, inspect_model, write_json_atomic


SCHEMA_VERSION = 1
CANONICAL_MODEL_PREFIX = "models/active"
SHA256_PATTERN = re.compile(r"^[0-9a-f]{64}$")
FILENAME_EMPLOYEE_PATTERN = re.compile(r"^emp_(\d+)_", re.IGNORECASE)
VALIDATED = "Validated"
ALREADY_ADOPTED = "AlreadyAdopted"
MANIFEST_NOT_APPROVED = "ManifestNotApproved"
EMPLOYEE_MISSING = "EmployeeMissing"
EMPLOYEE_ALREADY_HAS_MODEL = "EmployeeAlreadyHasModel"
DATABASE_ROW_CONFLICT = "DatabaseRowConflict"
SOURCE_CHECKSUM_MISMATCH = "SourceChecksumMismatch"
ENCODING_COUNT_MISMATCH = "EncodingCountMismatch"
INVALID_MODEL = "InvalidModel"
DESTINATION_CONFLICT = "DestinationConflict"
MANIFEST_INVALID = "ManifestInvalid"
MAPPING_MISMATCH = "MappingMismatch"


class AdoptionError(RuntimeError):
    pass


class AdoptionDatabaseError(AdoptionError):
    pass


class AdoptionRepository(Protocol):
    def employee_exists(self, employee_id: int) -> bool: ...
    def models_by_filename(self, file_name: str) -> list[DatabaseModelRow]: ...
    def models_by_employee(self, employee_id: int) -> list[DatabaseModelRow]: ...
    def adopt(self, mappings: list[dict], adopted_at: datetime) -> list[dict]: ...
    def rollback(self, database_rows: list[dict]) -> None: ...


class SqlServerAdoptionRepository:
    """SQL Server repository with all adoption mutations transaction-scoped."""

    def __init__(self, connection_string: str):
        if not connection_string.strip():
            raise ValueError("A database connection string is required")
        self._connection_string = connection_string

    def _connect(self):
        try:
            import pyodbc

            return pyodbc.connect(self._connection_string, autocommit=False)
        except Exception as exc:
            raise AdoptionDatabaseError(
                "Could not connect to the configured adoption database."
            ) from exc

    @staticmethod
    def _rows(cursor) -> list[DatabaseModelRow]:
        return [
            DatabaseModelRow(
                row_id=int(row[0]),
                employee_id=int(row[1]),
                model_file_name=str(row[2]),
                model_path=str(row[3]),
            )
            for row in cursor.fetchall()
        ]

    def employee_exists(self, employee_id: int) -> bool:
        try:
            with self._connect() as connection:
                cursor = connection.cursor()
                cursor.execute(
                    "SELECT COUNT(*) FROM Employee WHERE EmployeeId = ?",
                    employee_id,
                )
                return int(cursor.fetchone()[0]) == 1
        except AdoptionDatabaseError:
            raise
        except Exception as exc:
            raise AdoptionDatabaseError(
                "Database schema mismatch: Employee must expose EmployeeId."
            ) from exc

    def models_by_filename(self, file_name: str) -> list[DatabaseModelRow]:
        return self._find_models("ModelFileName", file_name)

    def models_by_employee(self, employee_id: int) -> list[DatabaseModelRow]:
        return self._find_models("EmployeeId", employee_id)

    def _find_models(self, column: str, value: object) -> list[DatabaseModelRow]:
        if column not in {"ModelFileName", "EmployeeId"}:
            raise ValueError("Unsupported model lookup")
        try:
            with self._connect() as connection:
                cursor = connection.cursor()
                cursor.execute(
                    f"SELECT Id, EmployeeId, ModelFileName, ModelPath "
                    f"FROM EmployeeFaceModels WHERE {column} = ?",
                    value,
                )
                return self._rows(cursor)
        except AdoptionDatabaseError:
            raise
        except Exception as exc:
            raise AdoptionDatabaseError(
                "Database schema mismatch: EmployeeFaceModels must expose Id, "
                "EmployeeId, ModelFileName, ModelPath, and CreatedAt."
            ) from exc

    def adopt(self, mappings: list[dict], adopted_at: datetime) -> list[dict]:
        connection = self._connect()
        try:
            cursor = connection.cursor()
            results: list[dict] = []
            adoption_value = adopted_at.astimezone(timezone.utc).replace(tzinfo=None)
            for mapping in mappings:
                employee_id = mapping["employeeId"]
                file_name = mapping["fileName"]
                model_path = mapping["modelPath"]
                cursor.execute(
                    "SELECT COUNT(*) FROM Employee WHERE EmployeeId = ?",
                    employee_id,
                )
                if int(cursor.fetchone()[0]) != 1:
                    raise AdoptionDatabaseError("Employee no longer exists.")

                cursor.execute(
                    "SELECT Id, EmployeeId, ModelFileName, ModelPath, CreatedAt "
                    "FROM EmployeeFaceModels "
                    "WHERE ModelFileName = ? OR EmployeeId = ?",
                    file_name,
                    employee_id,
                )
                existing = cursor.fetchall()
                if existing:
                    exact = [
                        row
                        for row in existing
                        if int(row[1]) == employee_id
                        and str(row[2]) == file_name
                        and str(row[3]) == model_path
                    ]
                    if len(existing) == 1 and len(exact) == 1:
                        results.append(
                            {
                                "rowId": int(exact[0][0]),
                                "employeeId": employee_id,
                                "fileName": file_name,
                                "modelPath": model_path,
                                "created": False,
                            }
                        )
                        continue
                    raise AdoptionDatabaseError(
                        "Database state changed and now conflicts with the manifest."
                    )

                cursor.execute(
                    "INSERT INTO EmployeeFaceModels "
                    "(EmployeeId, ModelFileName, ModelPath, CreatedAt) "
                    "OUTPUT INSERTED.Id VALUES (?, ?, ?, ?)",
                    employee_id,
                    file_name,
                    model_path,
                    adoption_value,
                )
                results.append(
                    {
                        "rowId": int(cursor.fetchone()[0]),
                        "employeeId": employee_id,
                        "fileName": file_name,
                        "modelPath": model_path,
                        "created": True,
                    }
                )
            connection.commit()
            return results
        except Exception:
            connection.rollback()
            raise
        finally:
            connection.close()

    def rollback(self, database_rows: list[dict]) -> None:
        connection = self._connect()
        try:
            cursor = connection.cursor()
            for record in database_rows:
                cursor.execute(
                    "SELECT EmployeeId, ModelFileName, ModelPath "
                    "FROM EmployeeFaceModels WHERE Id = ?",
                    record["rowId"],
                )
                row = cursor.fetchone()
                if (
                    row is None
                    or int(row[0]) != record["employeeId"]
                    or str(row[1]) != record["fileName"]
                    or str(row[2]) != record["modelPath"]
                ):
                    raise AdoptionDatabaseError(
                        "Rollback refused because an adoption row changed or is missing."
                    )
            for record in database_rows:
                if record.get("created", True):
                    cursor.execute(
                        "DELETE FROM EmployeeFaceModels WHERE Id = ?",
                        record["rowId"],
                    )
            connection.commit()
        except Exception:
            connection.rollback()
            raise
        finally:
            connection.close()


@dataclass
class ModelValidation:
    fileName: str
    employeeId: int | None
    status: str
    checksum: str | None = None
    encodingCount: int = 0
    modelPath: str | None = None
    issues: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)


@dataclass
class ValidationReport:
    valid: bool
    status: str
    models: list[ModelValidation]
    issues: list[str] = field(default_factory=list)

    def safe_dict(self) -> dict:
        return {
            "valid": self.valid,
            "status": self.status,
            "issues": self.issues,
            "models": [asdict(model) for model in self.models],
        }


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def suggested_employee_id(file_name: str) -> int | None:
    match = FILENAME_EMPLOYEE_PATTERN.match(file_name)
    return int(match.group(1)) if match else None


def canonical_model_path(file_name: str) -> str:
    return f"{CANONICAL_MODEL_PREFIX}/{file_name}"


def generate_template(source_root: Path, manifest_path: Path, *, force: bool = False) -> dict:
    source_root = source_root.resolve()
    if manifest_path.exists() and not force:
        raise AdoptionError("Adoption manifest already exists; use --force-template to replace it.")

    models = []
    for source in sorted(source_root.glob("*.pkl"), key=lambda path: path.name):
        if source.resolve().parent != source_root:
            raise AdoptionError("Template generation refused a model path outside the legacy root.")
        checksum, encoding_count = inspect_model(source)
        models.append(
            {
                "fileName": source.name,
                "suggestedEmployeeId": suggested_employee_id(source.name),
                "employeeId": None,
                "expectedSha256": checksum,
                "expectedEncodingCount": encoding_count,
            }
        )
    template = {
        "schemaVersion": SCHEMA_VERSION,
        "approved": False,
        "approvedBy": "",
        "approvedAtUtc": None,
        "models": models,
    }
    write_json_atomic(manifest_path.resolve(), template)
    return template


def load_manifest(path: Path) -> dict:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        raise AdoptionError("Adoption manifest is not valid JSON.") from exc
    if not isinstance(payload, dict):
        raise AdoptionError("Adoption manifest root must be an object.")
    return payload


def _valid_approval_timestamp(value: object) -> bool:
    if not isinstance(value, str) or not value.strip():
        return False
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        return False
    return parsed.tzinfo is not None and parsed.utcoffset() == timezone.utc.utcoffset(parsed)


def _storage_layout_valid(
    active_root: Path,
    staging_root: Path,
    archive_root: Path,
    failed_root: Path,
) -> bool:
    directories = (active_root, staging_root, archive_root, failed_root)
    if not all(path.is_dir() for path in directories):
        return False
    return len({path.stat().st_dev for path in directories}) == 1


def validate_manifest(
    manifest: dict,
    source_root: Path,
    active_root: Path,
    staging_root: Path,
    archive_root: Path,
    failed_root: Path,
    repository: AdoptionRepository,
    *,
    allow_filename_mismatch: bool = False,
) -> ValidationReport:
    source_root = source_root.resolve()
    active_root = active_root.resolve()
    issues: list[str] = []
    entries = manifest.get("models")
    if manifest.get("schemaVersion") != SCHEMA_VERSION or not isinstance(entries, list):
        return ValidationReport(False, MANIFEST_INVALID, [], ["Unsupported manifest schema."])
    if (
        manifest.get("approved") is not True
        or not str(manifest.get("approvedBy") or "").strip()
        or not _valid_approval_timestamp(manifest.get("approvedAtUtc"))
    ):
        return ValidationReport(
            False,
            MANIFEST_NOT_APPROVED,
            [],
            ["Manifest requires approved=true, approvedBy, and a UTC approvedAtUtc."],
        )
    if not _storage_layout_valid(active_root, staging_root, archive_root, failed_root):
        return ValidationReport(
            False,
            MANIFEST_INVALID,
            [],
            ["Canonical active/staging/archive/failed directories must exist on one filesystem."],
        )

    source_names = {
        path.name for path in source_root.glob("*.pkl") if path.is_file()
    }
    entry_names = [entry.get("fileName") for entry in entries if isinstance(entry, dict)]
    employee_ids = [entry.get("employeeId") for entry in entries if isinstance(entry, dict)]
    duplicate_names = {name for name in entry_names if entry_names.count(name) > 1}
    duplicate_employees = {
        employee_id
        for employee_id in employee_ids
        if employee_id is not None and employee_ids.count(employee_id) > 1
    }
    if set(entry_names) != source_names or len(entry_names) != len(source_names):
        issues.append("Manifest must contain exactly one entry for every legacy model.")

    models: list[ModelValidation] = []
    for entry in entries:
        if not isinstance(entry, dict):
            models.append(ModelValidation("", None, MANIFEST_INVALID, issues=["Entry must be an object."]))
            continue
        file_name = entry.get("fileName")
        employee_id = entry.get("employeeId")
        model = ModelValidation(str(file_name or ""), employee_id, VALIDATED)
        if (
            not isinstance(file_name, str)
            or file_name != Path(file_name).name
            or not file_name.lower().endswith(".pkl")
            or file_name in duplicate_names
        ):
            model.status = MANIFEST_INVALID
            model.issues.append("Filename is unsafe, invalid, or duplicated.")
            models.append(model)
            continue
        if not isinstance(employee_id, int) or isinstance(employee_id, bool) or employee_id <= 0:
            model.status = MANIFEST_INVALID
            model.issues.append("employeeId must be explicitly set to a positive integer.")
            models.append(model)
            continue
        if employee_id in duplicate_employees:
            model.status = MANIFEST_INVALID
            model.issues.append("employeeId is duplicated in the manifest.")
            models.append(model)
            continue
        expected_checksum = str(entry.get("expectedSha256") or "").lower()
        expected_count = entry.get("expectedEncodingCount")
        if not SHA256_PATTERN.fullmatch(expected_checksum) or not isinstance(expected_count, int) or expected_count <= 0:
            model.status = MANIFEST_INVALID
            model.issues.append("Expected checksum or encoding count is invalid.")
            models.append(model)
            continue

        source = (source_root / file_name).resolve()
        if source.parent != source_root or not source.is_file():
            model.status = INVALID_MODEL
            model.issues.append("Source path is missing or escapes the legacy source root.")
            models.append(model)
            continue
        try:
            actual_checksum, actual_count = inspect_model(source)
        except (OSError, ValueError):
            model.status = INVALID_MODEL
            model.issues.append("Source model is unreadable or contains invalid encodings.")
            models.append(model)
            continue
        model.checksum = actual_checksum
        model.encodingCount = actual_count
        model.modelPath = canonical_model_path(file_name)
        if actual_checksum != expected_checksum:
            model.status = SOURCE_CHECKSUM_MISMATCH
            model.issues.append("Source checksum differs from the approved manifest.")
        elif actual_count != expected_count:
            model.status = ENCODING_COUNT_MISMATCH
            model.issues.append("Encoding count differs from the approved manifest.")

        suggestion = suggested_employee_id(file_name)
        if entry.get("suggestedEmployeeId") != suggestion:
            model.status = MANIFEST_INVALID
            model.issues.append(
                "suggestedEmployeeId must match the non-authoritative filename suggestion."
            )
        if suggestion is not None and suggestion != employee_id:
            model.warnings.append("Explicit employeeId differs from the filename suggestion.")
            if not allow_filename_mismatch and model.status == VALIDATED:
                model.status = MAPPING_MISMATCH
                model.issues.append(
                    "Mapping mismatch requires --allow-filename-mismatch after operator review."
                )

        if model.status == VALIDATED and not repository.employee_exists(employee_id):
            model.status = EMPLOYEE_MISSING
            model.issues.append("Approved employee does not exist.")

        filename_rows = repository.models_by_filename(file_name)
        employee_rows = repository.models_by_employee(employee_id)
        destination = active_root / file_name
        destination_same = destination.is_file() and sha256(destination) == actual_checksum
        exact_rows = [
            row
            for row in filename_rows
            if row.employee_id == employee_id and row.model_path == model.modelPath
        ]
        if model.status == VALIDATED and filename_rows:
            if (
                len(filename_rows) == 1
                and len(employee_rows) == 1
                and len(exact_rows) == 1
                and destination_same
            ):
                model.status = ALREADY_ADOPTED
            else:
                model.status = DATABASE_ROW_CONFLICT
                model.issues.append("A database row with this filename conflicts with the manifest.")
        elif model.status == VALIDATED and employee_rows:
            model.status = EMPLOYEE_ALREADY_HAS_MODEL
            model.issues.append("Approved employee already has a different face model.")

        if destination.exists():
            if not destination.is_file() or sha256(destination) != actual_checksum:
                model.status = DESTINATION_CONFLICT
                model.issues.append("Canonical destination exists with different content.")

        models.append(model)

    valid_statuses = {VALIDATED, ALREADY_ADOPTED}
    valid = not issues and bool(models) and all(model.status in valid_statuses for model in models)
    return ValidationReport(valid, VALIDATED if valid else "ValidationFailed", models, issues)


def _safe_result_path(manifest_path: Path, prefix: str, run_id: str) -> Path:
    return manifest_path.parent / f"{prefix}-{run_id}.json"


def apply_adoption(
    manifest: dict,
    report: ValidationReport,
    source_root: Path,
    active_root: Path,
    repository: AdoptionRepository,
    result_path: Path,
    rollback_path: Path,
    *,
    run_id: str | None = None,
) -> dict:
    if not report.valid:
        raise AdoptionError("Apply refused because manifest validation failed.")
    run_id = run_id or uuid.uuid4().hex
    started = utc_now()
    source_root = source_root.resolve()
    active_root = active_root.resolve()
    staged: list[tuple[Path, Path, ModelValidation]] = []
    created_files: list[dict] = []
    pre_existing_files: list[dict] = []
    database_rows: list[dict] = []
    try:
        for model in report.models:
            destination = active_root / model.fileName
            if destination.exists():
                pre_existing_files.append(
                    {"fileName": model.fileName, "sha256": model.checksum}
                )
                continue
            descriptor, temporary_name = tempfile.mkstemp(
                prefix=f".{model.fileName}.", suffix=".adoption.tmp", dir=active_root
            )
            temporary = Path(temporary_name)
            with os.fdopen(descriptor, "wb") as output, (source_root / model.fileName).open("rb") as source:
                shutil.copyfileobj(source, output)
                output.flush()
                os.fsync(output.fileno())
            checksum, count = inspect_model(temporary)
            if checksum != model.checksum or count != model.encodingCount:
                raise AdoptionError("Prepared model failed checksum or encoding verification.")
            staged.append((temporary, destination, model))

        for temporary, destination, model in staged:
            if destination.exists():
                raise AdoptionError("Destination changed after validation.")
            os.replace(temporary, destination)
            created_files.append({"fileName": model.fileName, "sha256": model.checksum})

        mappings = [
            {
                "employeeId": model.employeeId,
                "fileName": model.fileName,
                "modelPath": model.modelPath,
            }
            for model in report.models
        ]
        adoption_time = utc_now()
        database_rows = repository.adopt(mappings, adoption_time)
        rollback = {
            "schemaVersion": SCHEMA_VERSION,
            "runId": run_id,
            "runtimeRestoredToLegacyRequired": True,
            "databaseRows": database_rows,
            "createdFiles": created_files,
            "preExistingFiles": pre_existing_files,
            "activeRoot": "canonical-model-active",
        }
        write_json_atomic(rollback_path, rollback)
        result = {
            "runId": run_id,
            "startedAtUtc": started.isoformat(),
            "completedAtUtc": utc_now().isoformat(),
            "approvedBy": manifest["approvedBy"],
            "sourceFiles": [model.fileName for model in report.models],
            "destinationFiles": [model.fileName for model in report.models],
            "checksums": {model.fileName: model.checksum for model in report.models},
            "encodingCounts": {model.fileName: model.encodingCount for model in report.models},
            "employeeMappings": {
                model.fileName: model.employeeId for model in report.models
            },
            "databaseRowIds": [row["rowId"] for row in database_rows],
            "createdFiles": [item["fileName"] for item in created_files],
            "preExistingFiles": [item["fileName"] for item in pre_existing_files],
            "status": (
                ALREADY_ADOPTED
                if all(not row.get("created") for row in database_rows)
                else "Adopted"
            ),
            "rollbackManifest": rollback_path.name,
        }
        write_json_atomic(result_path, result)
        return result
    except Exception as exc:
        for temporary, _, _ in staged:
            temporary.unlink(missing_ok=True)
        removed_files: list[str] = []
        for item in created_files:
            destination = active_root / item["fileName"]
            if destination.is_file() and sha256(destination) == item["sha256"]:
                destination.unlink()
                removed_files.append(item["fileName"])
        if database_rows:
            try:
                repository.rollback(database_rows)
            except Exception:
                pass
        try:
            write_json_atomic(
                rollback_path,
                {
                    "schemaVersion": SCHEMA_VERSION,
                    "runId": run_id,
                    "status": "RolledBackAfterFailure",
                    "failureType": type(exc).__name__,
                    "databaseRows": database_rows,
                    "createdFiles": created_files,
                    "removedFiles": removed_files,
                    "preExistingFiles": pre_existing_files,
                    "activeRoot": "canonical-model-active",
                },
            )
        except Exception:
            pass
        raise


def rollback_adoption(
    rollback_manifest: dict,
    active_root: Path,
    repository: AdoptionRepository,
    *,
    confirm: bool,
    runtime_restored_to_legacy: bool,
) -> dict:
    created_files = rollback_manifest.get("createdFiles", [])
    database_rows = rollback_manifest.get("databaseRows", [])
    for item in created_files:
        destination = active_root / item["fileName"]
        if not destination.is_file() or sha256(destination) != item["sha256"]:
            raise AdoptionError("Rollback refused because a destination checksum changed.")
    result = {
        "status": "RollbackDryRun",
        "databaseRows": len([row for row in database_rows if row.get("created", True)]),
        "files": len(created_files),
    }
    if not confirm:
        return result
    if not runtime_restored_to_legacy:
        raise AdoptionError(
            "Rollback requires --runtime-restored-to-legacy confirmation."
        )
    repository.rollback(database_rows)
    for item in created_files:
        destination = active_root / item["fileName"]
        if destination.is_file() and sha256(destination) == item["sha256"]:
            destination.unlink()
    result["status"] = "RolledBack"
    return result


def repository_root() -> Path:
    return Path(__file__).resolve().parents[1]


def require_ignored_manifest_path(path: Path) -> None:
    allowed_root = (
        repository_root() / "runtime" / "face-data" / "manifests"
    ).resolve()
    if path.resolve().parent != allowed_root:
        raise AdoptionError(
            "Operational manifests must stay in runtime/face-data/manifests."
        )


def parse_args(argv: Iterable[str] | None = None) -> argparse.Namespace:
    root = repository_root()
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument("--generate-template", action="store_true")
    modes.add_argument("--validate", action="store_true")
    modes.add_argument("--apply", action="store_true")
    modes.add_argument("--rollback", type=Path)
    parser.add_argument(
        "--manifest",
        type=Path,
        default=root / "runtime/face-data/manifests/legacy-face-model-adoption.json",
    )
    parser.add_argument(
        "--source",
        type=Path,
        default=root / "API/API/API/wwwroot/uploads/VideoFace/FaceID",
    )
    parser.add_argument(
        "--active-root",
        type=Path,
        default=root / "runtime/face-data/models/active",
    )
    parser.add_argument(
        "--staging-root",
        type=Path,
        default=root / "runtime/face-data/models/staging",
    )
    parser.add_argument(
        "--archive-root",
        type=Path,
        default=root / "runtime/face-data/models/archive",
    )
    parser.add_argument(
        "--failed-root",
        type=Path,
        default=root / "runtime/face-data/models/failed",
    )
    parser.add_argument("--connection-string")
    parser.add_argument("--force-template", action="store_true")
    parser.add_argument("--allow-filename-mismatch", action="store_true")
    parser.add_argument("--confirm-adoption", action="store_true")
    parser.add_argument("--confirm-rollback", action="store_true")
    parser.add_argument("--runtime-restored-to-legacy", action="store_true")
    return parser.parse_args(argv)


def main(argv: Iterable[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        require_ignored_manifest_path(args.rollback or args.manifest)
        if args.generate_template:
            template = generate_template(args.source, args.manifest, force=args.force_template)
            print(f"Generated unapproved template for {len(template['models'])} model(s).")
            return 0
        if args.apply and not args.confirm_adoption:
            raise AdoptionError("--apply requires --confirm-adoption.")

        connection_string = (
            args.connection_string
            or os.environ.get("VSHIELD_FACE_ADOPTION_CONNECTION_STRING")
            or os.environ.get("ConnectionStrings__DefaultConnection")
        )
        if not connection_string:
            raise AdoptionError(
                "Database connection is required via --connection-string or "
                "VSHIELD_FACE_ADOPTION_CONNECTION_STRING."
            )
        repository = SqlServerAdoptionRepository(connection_string)
        if args.rollback:
            rollback_payload = load_manifest(args.rollback)
            result = rollback_adoption(
                rollback_payload,
                args.active_root.resolve(),
                repository,
                confirm=args.confirm_rollback,
                runtime_restored_to_legacy=args.runtime_restored_to_legacy,
            )
            print(json.dumps(result, ensure_ascii=False))
            return 0

        manifest = load_manifest(args.manifest)
        report = validate_manifest(
            manifest,
            args.source,
            args.active_root,
            args.staging_root,
            args.archive_root,
            args.failed_root,
            repository,
            allow_filename_mismatch=args.allow_filename_mismatch,
        )
        print(json.dumps(report.safe_dict(), ensure_ascii=False))
        if args.apply:
            if not report.valid:
                raise AdoptionError("Adoption validation failed; no changes were made.")
            run_id = uuid.uuid4().hex
            result_path = _safe_result_path(args.manifest, "legacy-face-adoption-result", run_id)
            rollback_path = _safe_result_path(args.manifest, "legacy-face-adoption-rollback", run_id)
            print(
                json.dumps(
                    {
                        "models": len(report.models),
                        "employeeIds": [model.employeeId for model in report.models],
                        "source": "legacy-face-model-directory",
                        "destination": "canonical-model-active",
                        "database": "configured-sql-server-database",
                        "rowsToInsert": sum(
                            model.status == VALIDATED for model in report.models
                        ),
                    }
                )
            )
            result = apply_adoption(
                manifest,
                report,
                args.source,
                args.active_root,
                repository,
                result_path,
                rollback_path,
                run_id=run_id,
            )
            print(json.dumps(result, ensure_ascii=False))
        return 0 if report.valid else 3
    except AdoptionError as exc:
        print(str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
