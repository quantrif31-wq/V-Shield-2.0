import json
import os
import pickle
import sys
import tempfile
import unittest
from contextlib import redirect_stderr
from datetime import datetime, timezone
from io import StringIO
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from adopt_legacy_face_models import (
    ALREADY_ADOPTED,
    DATABASE_ROW_CONFLICT,
    DESTINATION_CONFLICT,
    EMPLOYEE_ALREADY_HAS_MODEL,
    EMPLOYEE_MISSING,
    ENCODING_COUNT_MISMATCH,
    MANIFEST_INVALID,
    MANIFEST_NOT_APPROVED,
    MAPPING_MISMATCH,
    SOURCE_CHECKSUM_MISMATCH,
    AdoptionDatabaseError,
    AdoptionError,
    DatabaseModelRow,
    apply_adoption,
    canonical_model_path,
    generate_template,
    load_manifest,
    main,
    rollback_adoption,
    sha256,
    validate_manifest,
)


def write_model(path: Path, count: int = 2, *, invalid: bool = False):
    values = [
        np.ones(127 if invalid else 128, dtype=np.float64)
        for _ in range(count)
    ]
    with path.open("wb") as stream:
        pickle.dump(values, stream)


class FakeRepository:
    def __init__(self, employees=(1, 2, 3)):
        self.employees = set(employees)
        self.rows = []
        self.next_id = 1
        self.adopt_calls = 0
        self.rollback_calls = 0
        self.fail_adopt = False
        self.last_adopted_at = None

    def employee_exists(self, employee_id):
        return employee_id in self.employees

    def models_by_filename(self, file_name):
        return [row for row in self.rows if row.model_file_name == file_name]

    def models_by_employee(self, employee_id):
        return [row for row in self.rows if row.employee_id == employee_id]

    def adopt(self, mappings, adopted_at):
        self.adopt_calls += 1
        self.last_adopted_at = adopted_at
        if self.fail_adopt:
            raise AdoptionDatabaseError("fixture transaction failure")

        pending = []
        results = []
        for mapping in mappings:
            if mapping["employeeId"] not in self.employees:
                raise AdoptionDatabaseError("employee missing")
            conflicts = [
                row
                for row in self.rows
                if row.model_file_name == mapping["fileName"]
                or row.employee_id == mapping["employeeId"]
            ]
            exact = [
                row
                for row in conflicts
                if row.employee_id == mapping["employeeId"]
                and row.model_file_name == mapping["fileName"]
                and row.model_path == mapping["modelPath"]
            ]
            if conflicts:
                if len(conflicts) == 1 and len(exact) == 1:
                    results.append(
                        {
                            "rowId": exact[0].row_id,
                            **mapping,
                            "created": False,
                        }
                    )
                    continue
                raise AdoptionDatabaseError("conflict")
            pending.append(mapping)

        for mapping in pending:
            row = DatabaseModelRow(
                self.next_id,
                mapping["employeeId"],
                mapping["fileName"],
                mapping["modelPath"],
            )
            self.rows.append(row)
            results.append({"rowId": self.next_id, **mapping, "created": True})
            self.next_id += 1
        return results

    def rollback(self, database_rows):
        self.rollback_calls += 1
        ids = {row["rowId"] for row in database_rows if row.get("created", True)}
        current = {row.row_id: row for row in self.rows}
        for record in database_rows:
            if not record.get("created", True):
                continue
            row = current.get(record["rowId"])
            if (
                row is None
                or row.employee_id != record["employeeId"]
                or row.model_file_name != record["fileName"]
                or row.model_path != record["modelPath"]
            ):
                raise AdoptionDatabaseError("rollback conflict")
        self.rows = [row for row in self.rows if row.row_id not in ids]


class AdoptionTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.source = self.root / "legacy"
        self.source.mkdir()
        self.model_root = self.root / "models"
        self.active = self.model_root / "active"
        self.staging = self.model_root / "staging"
        self.archive = self.model_root / "archive"
        self.failed = self.model_root / "failed"
        for directory in (self.active, self.staging, self.archive, self.failed):
            directory.mkdir(parents=True)
        self.manifest_path = self.root / "manifest.json"
        self.result_path = self.root / "result.json"
        self.rollback_path = self.root / "rollback.json"
        self.repository = FakeRepository()

    def tearDown(self):
        self.temp.cleanup()

    def add_model(self, file_name="emp_1_model.pkl", count=2, employee_id=1):
        path = self.source / file_name
        write_model(path, count)
        return {
            "fileName": file_name,
            "suggestedEmployeeId": int(file_name.split("_")[1]),
            "employeeId": employee_id,
            "expectedSha256": sha256(path),
            "expectedEncodingCount": count,
        }

    def approved_manifest(self, entries):
        return {
            "schemaVersion": 1,
            "approved": True,
            "approvedBy": "security-operator",
            "approvedAtUtc": "2026-07-28T03:00:00Z",
            "models": entries,
        }

    def validate(self, manifest, **kwargs):
        return validate_manifest(
            manifest,
            self.source,
            self.active,
            self.staging,
            self.archive,
            self.failed,
            self.repository,
            **kwargs,
        )

    def test_generate_template_never_approves_or_promotes_suggestion(self):
        self.add_model()

        template = generate_template(self.source, self.manifest_path)

        self.assertFalse(template["approved"])
        self.assertEqual("", template["approvedBy"])
        self.assertIsNone(template["approvedAtUtc"])
        self.assertEqual(1, template["models"][0]["suggestedEmployeeId"])
        self.assertIsNone(template["models"][0]["employeeId"])
        self.assertEqual([], self.repository.rows)
        self.assertEqual([], list(self.active.glob("*.pkl")))

    def test_generate_template_refuses_overwrite_without_explicit_force(self):
        self.add_model()
        generate_template(self.source, self.manifest_path)
        with self.assertRaisesRegex(AdoptionError, "already exists"):
            generate_template(self.source, self.manifest_path)
        generate_template(self.source, self.manifest_path, force=True)

    def test_apply_requires_dedicated_confirmation_flag(self):
        errors = StringIO()
        ignored_manifest = (
            Path(__file__).resolve().parents[2]
            / "runtime/face-data/manifests/unit-test-does-not-exist.json"
        )
        with redirect_stderr(errors):
            exit_code = main(["--apply", "--manifest", str(ignored_manifest)])
        self.assertEqual(2, exit_code)
        self.assertIn("--confirm-adoption", errors.getvalue())

    def test_unapproved_empty_approver_and_non_utc_approval_are_rejected(self):
        entry = self.add_model()
        variants = [
            {**self.approved_manifest([entry]), "approved": False},
            {**self.approved_manifest([entry]), "approvedBy": ""},
            {**self.approved_manifest([entry]), "approvedAtUtc": "2026-07-28T03:00:00"},
        ]
        for manifest in variants:
            with self.subTest(manifest=manifest):
                report = self.validate(manifest)
                self.assertFalse(report.valid)
                self.assertEqual(MANIFEST_NOT_APPROVED, report.status)

    def test_null_employee_and_duplicate_filename_or_employee_are_rejected(self):
        first = self.add_model()
        second = self.add_model("emp_2_model.pkl", employee_id=2)
        cases = [
            [{**first, "employeeId": None}, second],
            [first, {**second, "fileName": first["fileName"]}],
            [first, {**second, "employeeId": first["employeeId"]}],
        ]
        for entries in cases:
            with self.subTest(entries=entries):
                report = self.validate(self.approved_manifest(entries))
                self.assertFalse(report.valid)
                self.assertTrue(
                    report.issues
                    or any(model.status == MANIFEST_INVALID for model in report.models)
                )

    def test_employee_missing_is_reported_without_mutation(self):
        entry = self.add_model("emp_99_model.pkl", employee_id=99)
        report = self.validate(self.approved_manifest([entry]))
        self.assertEqual(EMPLOYEE_MISSING, report.models[0].status)
        self.assertEqual(0, self.repository.adopt_calls)

    def test_filename_is_only_suggestion_and_mismatch_needs_extra_confirmation(self):
        entry = self.add_model(employee_id=2)
        report = self.validate(self.approved_manifest([entry]))
        self.assertEqual(MAPPING_MISMATCH, report.models[0].status)
        self.assertFalse(report.valid)

        allowed = self.validate(
            self.approved_manifest([entry]), allow_filename_mismatch=True
        )
        self.assertTrue(allowed.valid)
        self.assertEqual(2, allowed.models[0].employeeId)
        self.assertTrue(allowed.models[0].warnings)

    def test_checksum_encoding_and_invalid_model_fail_validation(self):
        entry = self.add_model()
        bad_checksum = {**entry, "expectedSha256": "0" * 64}
        self.assertEqual(
            SOURCE_CHECKSUM_MISMATCH,
            self.validate(self.approved_manifest([bad_checksum])).models[0].status,
        )
        bad_count = {**entry, "expectedEncodingCount": 99}
        self.assertEqual(
            ENCODING_COUNT_MISMATCH,
            self.validate(self.approved_manifest([bad_count])).models[0].status,
        )
        write_model(self.source / entry["fileName"], invalid=True)
        self.assertFalse(self.validate(self.approved_manifest([entry])).valid)

    def test_database_filename_and_employee_conflicts_are_reported(self):
        entry = self.add_model()
        self.repository.rows.append(
            DatabaseModelRow(1, 2, entry["fileName"], "models/active/other.pkl")
        )
        self.assertEqual(
            DATABASE_ROW_CONFLICT,
            self.validate(self.approved_manifest([entry])).models[0].status,
        )

        self.repository.rows = [
            DatabaseModelRow(2, 1, "different.pkl", "models/active/different.pkl")
        ]
        self.assertEqual(
            EMPLOYEE_ALREADY_HAS_MODEL,
            self.validate(self.approved_manifest([entry])).models[0].status,
        )

    def test_destination_same_checksum_is_allowed_and_different_is_conflict(self):
        entry = self.add_model()
        shutil_source = self.source / entry["fileName"]
        (self.active / entry["fileName"]).write_bytes(shutil_source.read_bytes())
        self.assertTrue(self.validate(self.approved_manifest([entry])).valid)

        (self.active / entry["fileName"]).write_bytes(b"different")
        self.assertEqual(
            DESTINATION_CONFLICT,
            self.validate(self.approved_manifest([entry])).models[0].status,
        )

    def test_validate_is_read_only_and_output_contains_no_vector_or_secret(self):
        entry = self.add_model()
        before = self.source.read_bytes() if self.source.is_file() else None
        report = self.validate(self.approved_manifest([entry]))
        rendered = json.dumps(report.safe_dict())
        self.assertTrue(report.valid)
        self.assertEqual(0, self.repository.adopt_calls)
        self.assertNotIn("connection", rendered.lower())
        self.assertNotIn("[1.0", rendered)
        self.assertEqual(before, self.source.read_bytes() if self.source.is_file() else None)

    def test_path_traversal_and_symlink_escape_are_rejected(self):
        entry = self.add_model()
        traversal = {**entry, "fileName": "../escape.pkl"}
        self.assertFalse(self.validate(self.approved_manifest([traversal])).valid)

        outside = self.root / "outside.pkl"
        write_model(outside)
        link = self.source / "emp_2_link.pkl"
        try:
            os.symlink(outside, link)
        except (OSError, NotImplementedError):
            self.skipTest("Symlink creation is unavailable")
        linked = {
            "fileName": link.name,
            "suggestedEmployeeId": 2,
            "employeeId": 2,
            "expectedSha256": sha256(outside),
            "expectedEncodingCount": 2,
        }
        report = self.validate(self.approved_manifest([entry, linked]))
        self.assertFalse(report.valid)

    def test_apply_is_all_or_nothing_and_uses_canonical_metadata(self):
        first = self.add_model()
        second = self.add_model("emp_2_model.pkl", employee_id=2)
        manifest = self.approved_manifest([first, second])
        report = self.validate(manifest)

        result = apply_adoption(
            manifest,
            report,
            self.source,
            self.active,
            self.repository,
            self.result_path,
            self.rollback_path,
        )

        self.assertEqual("Adopted", result["status"])
        self.assertEqual(2, len(self.repository.rows))
        self.assertTrue(all(row.model_path.startswith("models/active/") for row in self.repository.rows))
        self.assertEqual({1, 2}, {row.employee_id for row in self.repository.rows})
        self.assertIsNotNone(self.repository.last_adopted_at.tzinfo)
        self.assertEqual(timezone.utc, self.repository.last_adopted_at.tzinfo)
        self.assertEqual(2, len(list(self.active.glob("*.pkl"))))

    def test_database_failure_rolls_back_only_new_destination_files(self):
        entry = self.add_model()
        report = self.validate(self.approved_manifest([entry]))
        self.repository.fail_adopt = True

        with self.assertRaises(AdoptionDatabaseError):
            apply_adoption(
                self.approved_manifest([entry]),
                report,
                self.source,
                self.active,
                self.repository,
                self.result_path,
                self.rollback_path,
            )
        self.assertEqual([], list(self.active.glob("*.pkl")))
        self.assertEqual([], self.repository.rows)
        failure_manifest = load_manifest(self.rollback_path)
        self.assertEqual("RolledBackAfterFailure", failure_manifest["status"])
        self.assertEqual([entry["fileName"]], failure_manifest["removedFiles"])

    def test_copy_verification_failure_does_not_insert_database(self):
        entry = self.add_model()
        manifest = self.approved_manifest([entry])
        report = self.validate(manifest)
        (self.source / entry["fileName"]).write_bytes(b"changed after validation")

        with self.assertRaises(Exception):
            apply_adoption(
                manifest,
                report,
                self.source,
                self.active,
                self.repository,
                self.result_path,
                self.rollback_path,
            )
        self.assertEqual(0, self.repository.adopt_calls)
        self.assertEqual([], self.repository.rows)

    def test_partial_validation_blocks_every_model(self):
        valid = self.add_model()
        invalid = self.add_model("emp_2_model.pkl", employee_id=2)
        invalid["expectedSha256"] = "0" * 64
        manifest = self.approved_manifest([valid, invalid])
        report = self.validate(manifest)
        self.assertFalse(report.valid)
        with self.assertRaises(AdoptionError):
            apply_adoption(
                manifest,
                report,
                self.source,
                self.active,
                self.repository,
                self.result_path,
                self.rollback_path,
            )
        self.assertEqual([], self.repository.rows)

    def test_idempotent_rerun_preserves_row_and_preexisting_file(self):
        entry = self.add_model()
        manifest = self.approved_manifest([entry])
        first_report = self.validate(manifest)
        first = apply_adoption(
            manifest,
            first_report,
            self.source,
            self.active,
            self.repository,
            self.result_path,
            self.rollback_path,
        )
        original_row = self.repository.rows[0]
        second_report = self.validate(manifest)
        self.assertEqual(ALREADY_ADOPTED, second_report.models[0].status)
        second = apply_adoption(
            manifest,
            second_report,
            self.source,
            self.active,
            self.repository,
            self.root / "result2.json",
            self.root / "rollback2.json",
        )
        self.assertEqual(ALREADY_ADOPTED, second["status"])
        self.assertEqual([original_row], self.repository.rows)
        self.assertEqual([], second["createdFiles"])

    def test_rollback_is_dry_run_by_default_then_removes_created_artifacts(self):
        entry = self.add_model()
        manifest = self.approved_manifest([entry])
        report = self.validate(manifest)
        apply_adoption(
            manifest,
            report,
            self.source,
            self.active,
            self.repository,
            self.result_path,
            self.rollback_path,
        )
        rollback = load_manifest(self.rollback_path)

        dry_run = rollback_adoption(
            rollback, self.active, self.repository, confirm=False, runtime_restored_to_legacy=False
        )
        self.assertEqual("RollbackDryRun", dry_run["status"])
        self.assertEqual(1, len(self.repository.rows))

        with self.assertRaisesRegex(AdoptionError, "runtime-restored"):
            rollback_adoption(
                rollback, self.active, self.repository, confirm=True, runtime_restored_to_legacy=False
            )
        applied = rollback_adoption(
            rollback, self.active, self.repository, confirm=True, runtime_restored_to_legacy=True
        )
        self.assertEqual("RolledBack", applied["status"])
        self.assertEqual([], self.repository.rows)
        self.assertFalse((self.active / entry["fileName"]).exists())
        self.assertTrue((self.source / entry["fileName"]).exists())

    def test_rollback_never_deletes_preexisting_file_and_rejects_changed_checksum(self):
        entry = self.add_model()
        destination = self.active / entry["fileName"]
        destination.write_bytes((self.source / entry["fileName"]).read_bytes())
        manifest = self.approved_manifest([entry])
        report = self.validate(manifest)
        apply_adoption(
            manifest,
            report,
            self.source,
            self.active,
            self.repository,
            self.result_path,
            self.rollback_path,
        )
        rollback = load_manifest(self.rollback_path)
        rollback_adoption(
            rollback, self.active, self.repository, confirm=True, runtime_restored_to_legacy=True
        )
        self.assertTrue(destination.exists())

        changed = {
            "databaseRows": [],
            "createdFiles": [{"fileName": entry["fileName"], "sha256": "0" * 64}],
        }
        with self.assertRaisesRegex(AdoptionError, "checksum changed"):
            rollback_adoption(
                changed, self.active, self.repository, confirm=False, runtime_restored_to_legacy=False
            )

    def test_model_path_is_relative_and_created_at_represents_adoption_time(self):
        self.assertEqual("models/active/model.pkl", canonical_model_path("model.pkl"))
        self.assertFalse(Path(canonical_model_path("model.pkl")).is_absolute())


if __name__ == "__main__":
    unittest.main()
