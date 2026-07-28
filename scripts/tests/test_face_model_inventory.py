import json
import pickle
import sys
import tempfile
import unittest
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from face_model_inventory import (
    CONFLICT,
    DUPLICATE_DATABASE_ROWS,
    DUPLICATE_EMPLOYEE_MODELS,
    EMPLOYEE_MISSING,
    INVALID_MODEL,
    MISSING_FILE,
    ORPHANED,
    READY,
    DatabaseModelRow,
    FaceModelInventory,
    copy_ready_models,
    rollback_import,
)


class FakeRepository:
    def __init__(self, rows=(), employees=()):
        self.rows = list(rows)
        self.employees = set(employees)

    def model_rows(self):
        return self.rows

    def employee_ids(self):
        return self.employees


def write_model(path: Path, count: int = 2, invalid_encoding: bool = False):
    values = [
        np.ones(127 if invalid_encoding else 128, dtype=np.float64)
        for _ in range(count)
    ]
    with path.open("wb") as stream:
        pickle.dump(values, stream)


class FaceModelInventoryTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.source = self.root / "legacy"
        self.source.mkdir()

    def tearDown(self):
        self.temp.cleanup()

    def build(self, rows=(), employees=()):
        return FaceModelInventory(
            self.source, FakeRepository(rows, employees)
        ).build()

    def one(self, inventory):
        return inventory["models"][0]

    def test_exact_mapping_is_ready(self):
        write_model(self.source / "emp_1_model.pkl")
        item = self.one(
            self.build([DatabaseModelRow(10, 1, "emp_1_model.pkl")], [1])
        )
        self.assertEqual(READY, item["status"])
        self.assertEqual(2, item["encodingCount"])
        self.assertEqual(64, len(item["sha256"]))

    def test_file_without_row_is_orphaned(self):
        write_model(self.source / "emp_1_model.pkl")
        self.assertEqual(ORPHANED, self.one(self.build())["status"])

    def test_row_without_file_is_missing_file(self):
        item = self.one(
            self.build([DatabaseModelRow(10, 1, "emp_1_missing.pkl")], [1])
        )
        self.assertEqual(MISSING_FILE, item["status"])
        self.assertIsNone(item["sha256"])

    def test_filename_database_mismatch_is_conflict(self):
        write_model(self.source / "emp_2_model.pkl")
        item = self.one(
            self.build([DatabaseModelRow(10, 1, "emp_2_model.pkl")], [1, 2])
        )
        self.assertEqual(CONFLICT, item["status"])

    def test_duplicate_database_rows_are_reported(self):
        write_model(self.source / "emp_1_model.pkl")
        rows = [
            DatabaseModelRow(10, 1, "emp_1_model.pkl"),
            DatabaseModelRow(11, 1, "emp_1_model.pkl"),
        ]
        self.assertEqual(DUPLICATE_DATABASE_ROWS, self.one(self.build(rows, [1]))["status"])

    def test_duplicate_employee_models_are_reported(self):
        write_model(self.source / "emp_1_first.pkl")
        write_model(self.source / "emp_1_second.pkl")
        rows = [
            DatabaseModelRow(10, 1, "emp_1_first.pkl"),
            DatabaseModelRow(11, 1, "emp_1_second.pkl"),
        ]
        statuses = {item["status"] for item in self.build(rows, [1])["models"]}
        self.assertEqual({DUPLICATE_EMPLOYEE_MODELS}, statuses)

    def test_missing_employee_is_reported(self):
        write_model(self.source / "emp_9_model.pkl")
        item = self.one(
            self.build([DatabaseModelRow(10, 9, "emp_9_model.pkl")], [])
        )
        self.assertEqual(EMPLOYEE_MISSING, item["status"])
        self.assertFalse(item["employeeExists"])

    def test_invalid_pickle_and_encoding_are_invalid_model(self):
        (self.source / "emp_1_broken.pkl").write_bytes(b"not-pickle")
        write_model(self.source / "emp_2_bad.pkl", invalid_encoding=True)
        rows = [
            DatabaseModelRow(10, 1, "emp_1_broken.pkl"),
            DatabaseModelRow(11, 2, "emp_2_bad.pkl"),
        ]
        statuses = [item["status"] for item in self.build(rows, [1, 2])["models"]]
        self.assertEqual([INVALID_MODEL, INVALID_MODEL], statuses)

    def test_manifest_contains_no_vectors_or_secrets(self):
        write_model(self.source / "emp_1_model.pkl")
        manifest = self.build([DatabaseModelRow(10, 1, "emp_1_model.pkl")], [1])
        rendered = json.dumps(manifest)
        self.assertNotIn("connection", rendered.lower())
        self.assertNotIn("[1.0", rendered)
        self.assertEqual("legacy/emp_1_model.pkl", self.one(manifest)["sourcePathSanitized"])

    def test_dry_run_does_not_write_destination(self):
        write_model(self.source / "emp_1_model.pkl")
        active = self.root / "active"
        self.build([DatabaseModelRow(10, 1, "emp_1_model.pkl")], [1])
        self.assertFalse(active.exists())

    def test_explicit_copy_is_atomic_idempotent_and_rollback_safe(self):
        source_file = self.source / "emp_1_model.pkl"
        write_model(source_file)
        inventory = self.build([DatabaseModelRow(10, 1, source_file.name)], [1])
        active = self.root / "active"
        rollback_path = self.root / "rollback.json"

        first = copy_ready_models(inventory, self.source, active, rollback_path)
        self.assertEqual([source_file.name], [item["fileName"] for item in first["createdFiles"]])
        self.assertEqual(source_file.read_bytes(), (active / source_file.name).read_bytes())
        second = copy_ready_models(inventory, self.source, active, self.root / "rollback-2.json")
        self.assertEqual([source_file.name], second["idempotentFiles"])

        (active / source_file.name).write_bytes(b"changed")
        self.assertEqual([], rollback_import(active, rollback_path))
        self.assertTrue((active / source_file.name).exists())

    def test_destination_checksum_conflict_and_blocking_inventory_refuse_copy(self):
        source_file = self.source / "emp_1_model.pkl"
        write_model(source_file)
        inventory = self.build([DatabaseModelRow(10, 1, source_file.name)], [1])
        active = self.root / "active"
        active.mkdir()
        (active / source_file.name).write_bytes(b"different")
        with self.assertRaisesRegex(RuntimeError, "checksum conflict"):
            copy_ready_models(inventory, self.source, active, self.root / "rollback.json")

        inventory["models"][0]["status"] = CONFLICT
        with self.assertRaisesRegex(RuntimeError, "blocking statuses"):
            copy_ready_models(inventory, self.source, active, self.root / "rollback.json")

    def test_rollback_removes_only_unchanged_files_created_by_import(self):
        source_file = self.source / "emp_1_model.pkl"
        write_model(source_file)
        inventory = self.build([DatabaseModelRow(10, 1, source_file.name)], [1])
        active = self.root / "active"
        rollback_path = self.root / "rollback.json"
        copy_ready_models(inventory, self.source, active, rollback_path)

        removed = rollback_import(active, rollback_path)

        self.assertEqual([source_file.name], removed)
        self.assertFalse((active / source_file.name).exists())


if __name__ == "__main__":
    unittest.main()
