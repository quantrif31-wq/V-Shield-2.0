#!/usr/bin/env python3
"""Inventory and copy trusted legacy face models without changing the database."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import pickle
import re
import shutil
import sys
import tempfile
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable, Protocol

import numpy as np


READY = "Ready"
ORPHANED = "Orphaned"
MISSING_FILE = "MissingFile"
CONFLICT = "Conflict"
DUPLICATE_DATABASE_ROWS = "DuplicateDatabaseRows"
DUPLICATE_EMPLOYEE_MODELS = "DuplicateEmployeeModels"
INVALID_MODEL = "InvalidModel"
EMPLOYEE_MISSING = "EmployeeMissing"
BLOCKING_COPY_STATUSES = {
    CONFLICT,
    DUPLICATE_DATABASE_ROWS,
    DUPLICATE_EMPLOYEE_MODELS,
    INVALID_MODEL,
}
EMPLOYEE_FILE_PATTERN = re.compile(r"^emp_(\d+)_", re.IGNORECASE)


@dataclass(frozen=True)
class DatabaseModelRow:
    row_id: int
    employee_id: int
    model_file_name: str
    model_path: str | None = None

    @property
    def basename(self) -> str:
        value = self.model_file_name or self.model_path or ""
        return Path(value.replace("\\", "/")).name


class InventoryRepository(Protocol):
    def model_rows(self) -> list[DatabaseModelRow]: ...
    def employee_ids(self) -> set[int]: ...


class SqlServerInventoryRepository:
    def __init__(self, connection_string: str):
        if not connection_string.strip():
            raise ValueError("A database connection string is required")
        self._connection_string = connection_string

    def _connect(self):
        import pyodbc

        return pyodbc.connect(self._connection_string)

    def model_rows(self) -> list[DatabaseModelRow]:
        with self._connect() as connection:
            cursor = connection.cursor()
            cursor.execute(
                "SELECT Id, EmployeeId, ModelFileName, ModelPath FROM EmployeeFaceModels"
            )
            return [
                DatabaseModelRow(
                    row_id=int(row[0]),
                    employee_id=int(row[1]),
                    model_file_name=str(row[2] or ""),
                    model_path=str(row[3]) if row[3] is not None else None,
                )
                for row in cursor.fetchall()
            ]

    def employee_ids(self) -> set[int]:
        with self._connect() as connection:
            cursor = connection.cursor()
            cursor.execute("SELECT EmployeeId FROM Employees")
            return {int(row[0]) for row in cursor.fetchall()}


@dataclass
class InventoryItem:
    fileName: str
    sourcePathSanitized: str
    sha256: str | None
    encodingCount: int
    filenameEmployeeId: int | None
    databaseRowId: int | None
    databaseEmployeeId: int | None
    employeeExists: bool
    status: str
    issues: list[str] = field(default_factory=list)


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _filename_employee_id(file_name: str) -> int | None:
    match = EMPLOYEE_FILE_PATTERN.match(file_name)
    return int(match.group(1)) if match else None


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def inspect_model(path: Path) -> tuple[str, int]:
    checksum = _sha256(path)
    try:
        with path.open("rb") as stream:
            encodings = pickle.load(stream)
    except Exception as exc:
        raise ValueError("Model cannot be deserialized") from exc

    if not isinstance(encodings, (list, tuple)):
        raise ValueError("Model must contain a list or tuple of encodings")

    for encoding in encodings:
        array = np.asarray(encoding)
        if (
            array.ndim != 1
            or array.shape[0] != 128
            or not np.issubdtype(array.dtype, np.number)
            or not np.all(np.isfinite(array))
        ):
            raise ValueError("Model contains an invalid face encoding")
    return checksum, len(encodings)


class FaceModelInventory:
    def __init__(self, source_root: Path, repository: InventoryRepository):
        self.source_root = source_root.resolve()
        self.repository = repository

    def build(self) -> dict:
        rows = self.repository.model_rows()
        employees = self.repository.employee_ids()
        files = sorted(self.source_root.glob("*.pkl"), key=lambda path: path.name)
        rows_by_basename: dict[str, list[DatabaseModelRow]] = {}
        for row in rows:
            rows_by_basename.setdefault(row.basename.casefold(), []).append(row)

        mapped_files_by_employee: dict[int, set[str]] = {}
        for model_file in files:
            matches = rows_by_basename.get(model_file.name.casefold(), [])
            if len(matches) == 1:
                mapped_files_by_employee.setdefault(matches[0].employee_id, set()).add(
                    model_file.name.casefold()
                )

        items = [
            self._inventory_file(
                model_file,
                rows_by_basename.get(model_file.name.casefold(), []),
                employees,
                mapped_files_by_employee,
            )
            for model_file in files
        ]

        existing_names = {path.name.casefold() for path in files}
        for row in rows:
            if row.basename.casefold() not in existing_names:
                items.append(
                    InventoryItem(
                        fileName=row.basename,
                        sourcePathSanitized=f"legacy/{row.basename}",
                        sha256=None,
                        encodingCount=0,
                        filenameEmployeeId=_filename_employee_id(row.basename),
                        databaseRowId=row.row_id,
                        databaseEmployeeId=row.employee_id,
                        employeeExists=row.employee_id in employees,
                        status=MISSING_FILE,
                        issues=["Database row references a model file that is not present."],
                    )
                )

        status_counts: dict[str, int] = {}
        for item in items:
            status_counts[item.status] = status_counts.get(item.status, 0) + 1
        return {
            "generatedAtUtc": _utc_now(),
            "source": "legacy-face-model-directory",
            "dryRun": True,
            "summary": {
                "fileCount": len(files),
                "databaseRowCount": len(rows),
                "encodingCount": sum(item.encodingCount for item in items if item.sha256),
                "statusCounts": status_counts,
            },
            "models": [asdict(item) for item in items],
        }

    def _inventory_file(
        self,
        model_file: Path,
        matches: list[DatabaseModelRow],
        employees: set[int],
        mapped_files_by_employee: dict[int, set[str]],
    ) -> InventoryItem:
        filename_employee_id = _filename_employee_id(model_file.name)
        checksum: str | None = None
        encoding_count = 0
        issues: list[str] = []
        try:
            checksum, encoding_count = inspect_model(model_file)
        except ValueError as exc:
            try:
                checksum = _sha256(model_file)
            except OSError:
                checksum = None
            issues.append(str(exc))
            return InventoryItem(
                model_file.name,
                f"legacy/{model_file.name}",
                checksum,
                encoding_count,
                filename_employee_id,
                matches[0].row_id if len(matches) == 1 else None,
                matches[0].employee_id if len(matches) == 1 else None,
                bool(len(matches) == 1 and matches[0].employee_id in employees),
                INVALID_MODEL,
                issues,
            )

        if not matches:
            issues.append("No EmployeeFaceModels row matches this basename.")
            status = ORPHANED
            row = None
        elif len(matches) > 1:
            issues.append("Multiple EmployeeFaceModels rows match this basename.")
            status = DUPLICATE_DATABASE_ROWS
            row = None
        else:
            row = matches[0]
            if row.employee_id not in employees:
                issues.append("The referenced employee does not exist.")
                status = EMPLOYEE_MISSING
            elif (
                filename_employee_id is not None
                and filename_employee_id != row.employee_id
            ):
                issues.append("Filename employee ID conflicts with the database employee ID.")
                status = CONFLICT
            elif len(mapped_files_by_employee.get(row.employee_id, set())) > 1:
                issues.append("Multiple model files map to the same employee.")
                status = DUPLICATE_EMPLOYEE_MODELS
            else:
                status = READY

        return InventoryItem(
            fileName=model_file.name,
            sourcePathSanitized=f"legacy/{model_file.name}",
            sha256=checksum,
            encodingCount=encoding_count,
            filenameEmployeeId=filename_employee_id,
            databaseRowId=row.row_id if row else None,
            databaseEmployeeId=row.employee_id if row else None,
            employeeExists=bool(row and row.employee_id in employees),
            status=status,
            issues=issues,
        )


def write_json_atomic(path: Path, payload: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.{os.getpid()}.tmp")
    with temporary.open("x", encoding="utf-8") as stream:
        json.dump(payload, stream, indent=2, ensure_ascii=False)
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temporary, path)


def copy_ready_models(
    inventory: dict,
    source_root: Path,
    active_root: Path,
    rollback_manifest_path: Path,
) -> dict:
    statuses = {item["status"] for item in inventory["models"]}
    blockers = sorted(statuses & BLOCKING_COPY_STATUSES)
    if blockers:
        raise RuntimeError(
            "Copy refused because inventory contains blocking statuses: "
            + ", ".join(blockers)
        )

    active_root.mkdir(parents=True, exist_ok=True)
    rollback = {
        "createdAtUtc": _utc_now(),
        "activeRoot": "canonical-model-active",
        "createdFiles": [],
        "idempotentFiles": [],
    }
    write_json_atomic(rollback_manifest_path, rollback)
    for item in inventory["models"]:
        if item["status"] != READY:
            continue
        source = (source_root / item["fileName"]).resolve()
        if source.parent != source_root.resolve():
            raise RuntimeError("Source model path escapes the legacy model root")
        destination = active_root / item["fileName"]
        if destination.exists():
            if _sha256(destination) == item["sha256"]:
                rollback["idempotentFiles"].append(item["fileName"])
                write_json_atomic(rollback_manifest_path, rollback)
                continue
            raise RuntimeError(f"Destination checksum conflict: {item['fileName']}")

        descriptor, temporary_name = tempfile.mkstemp(
            prefix=f".{item['fileName']}.", suffix=".tmp", dir=active_root
        )
        temporary = Path(temporary_name)
        try:
            with os.fdopen(descriptor, "wb") as output, source.open("rb") as input_stream:
                shutil.copyfileobj(input_stream, output)
                output.flush()
                os.fsync(output.fileno())
            if _sha256(temporary) != item["sha256"]:
                raise RuntimeError(f"Checksum verification failed: {item['fileName']}")
            _, copied_count = inspect_model(temporary)
            if copied_count != item["encodingCount"]:
                raise RuntimeError(f"Encoding count verification failed: {item['fileName']}")
            os.replace(temporary, destination)
            rollback["createdFiles"].append(
                {"fileName": item["fileName"], "sha256": item["sha256"]}
            )
            write_json_atomic(rollback_manifest_path, rollback)
        finally:
            temporary.unlink(missing_ok=True)

    return rollback


def rollback_import(active_root: Path, rollback_manifest_path: Path) -> list[str]:
    payload = json.loads(rollback_manifest_path.read_text(encoding="utf-8"))
    removed: list[str] = []
    for item in payload.get("createdFiles", []):
        file_name = item.get("fileName", "")
        if file_name != Path(file_name).name:
            raise RuntimeError("Rollback manifest contains an unsafe filename")
        destination = active_root / file_name
        if destination.is_file() and _sha256(destination) == item.get("sha256"):
            destination.unlink()
            removed.append(file_name)
    return removed


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[1]


def _parse_args(argv: Iterable[str] | None = None) -> argparse.Namespace:
    root = _repo_root()
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--source",
        type=Path,
        default=root / "API" / "API" / "API" / "wwwroot" / "uploads" / "VideoFace" / "FaceID",
    )
    parser.add_argument(
        "--manifest",
        type=Path,
        default=root / "runtime" / "face-data" / "manifests" / "legacy-face-model-inventory.json",
    )
    parser.add_argument(
        "--active-root",
        type=Path,
        default=root / "runtime" / "face-data" / "models" / "active",
    )
    parser.add_argument("--connection-string")
    parser.add_argument("--copy-ready-models", action="store_true")
    parser.add_argument("--rollback-manifest", type=Path)
    return parser.parse_args(argv)


def main(argv: Iterable[str] | None = None) -> int:
    args = _parse_args(argv)
    if args.rollback_manifest:
        removed = rollback_import(args.active_root.resolve(), args.rollback_manifest.resolve())
        print(f"Rolled back {len(removed)} imported model file(s).")
        return 0

    connection_string = (
        args.connection_string
        or os.environ.get("VSHIELD_FACE_INVENTORY_CONNECTION_STRING")
        or os.environ.get("ConnectionStrings__DefaultConnection")
    )
    if not connection_string:
        print(
            "Database connection is required via --connection-string or "
            "VSHIELD_FACE_INVENTORY_CONNECTION_STRING.",
            file=sys.stderr,
        )
        return 2

    repository = SqlServerInventoryRepository(connection_string)
    inventory = FaceModelInventory(args.source, repository).build()
    write_json_atomic(args.manifest.resolve(), inventory)
    print(json.dumps(inventory["summary"], ensure_ascii=False))
    if args.copy_ready_models:
        rollback_path = args.manifest.with_name(
            f"face-model-import-rollback-{datetime.now(timezone.utc):%Y%m%dT%H%M%SZ}.json"
        )
        copy_ready_models(
            inventory,
            args.source.resolve(),
            args.active_root.resolve(),
            rollback_path.resolve(),
        )
        print(f"Rollback manifest: {rollback_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
