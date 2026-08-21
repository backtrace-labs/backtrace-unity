import contextlib
import io
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ElementTree
from pathlib import Path


SCRIPT_DIRECTORY = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SCRIPT_DIRECTORY))

import validate_unity_test_results as validator


REQUIRED_TESTS = (
    "Backtrace.Unity.Tests.Runtime.AndroidNativeInitializationTests."
    "RejectedBeforeNativeBridgeDoesNotRollback",
    "Backtrace.Unity.Tests.Runtime.AndroidNativeInitializationTests."
    "NativeBridgeFalseResultRollsBackPartialState",
)


class UnityTestResultValidatorTests(unittest.TestCase):
    def setUp(self):
        self._temporary_directory = tempfile.TemporaryDirectory()
        self.results_root = Path(self._temporary_directory.name)

    def tearDown(self):
        self._temporary_directory.cleanup()

    def _write_result(
        self,
        relative_path="playmode-results.xml",
        total=None,
        failed=0,
        tests=REQUIRED_TESTS,
        counter_overrides=None,
        root_result="Passed",
    ):
        if total is None:
            total = len(tests)

        counters = {
            "testcasecount": str(total),
            "total": str(total),
            "passed": str(max(total - failed, 0)),
            "failed": str(failed),
            "inconclusive": "0",
            "skipped": "0",
            "result": root_result,
        }
        if counter_overrides:
            for counter, value in counter_overrides.items():
                if value is None:
                    counters.pop(counter, None)
                else:
                    counters[counter] = value

        root = ElementTree.Element("test-run", counters)
        suite = ElementTree.SubElement(root, "test-suite")
        for test in tests:
            if isinstance(test, tuple):
                full_name, result = test
            else:
                full_name, result = test, "Passed"
            ElementTree.SubElement(
                suite,
                "test-case",
                {"fullname": full_name, "result": result},
            )

        result_file = self.results_root / relative_path
        result_file.parent.mkdir(parents=True, exist_ok=True)
        ElementTree.ElementTree(root).write(
            str(result_file), encoding="utf-8", xml_declaration=True
        )
        return result_file

    def _run(self, mode="playmode", required_tests=REQUIRED_TESTS):
        arguments = [
            "--results-root",
            str(self.results_root),
            "--mode",
            mode,
        ]
        for required_test in required_tests:
            arguments.extend(("--require-test", required_test))

        stdout = io.StringIO()
        stderr = io.StringIO()
        with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            result = validator.main(arguments)
        return result, stdout.getvalue(), stderr.getvalue()

    def test_valid_result_passes(self):
        self._write_result()

        result, stdout, stderr = self._run()

        self.assertEqual(0, result)
        self.assertIn("2 test(s), 0 failed", stdout)
        self.assertEqual("", stderr)

    def test_no_result_files_fails(self):
        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertIn("no playmode-results.xml files", stderr)

    def test_malformed_xml_fails(self):
        result_file = self.results_root / "playmode-results.xml"
        result_file.write_text("<test-run>", encoding="utf-8")

        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertIn("could not parse Unity test results", stderr)

    def test_wrong_root_element_fails(self):
        result_file = self.results_root / "playmode-results.xml"
        result_file.write_text("<tests />", encoding="utf-8")

        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertIn("expected a <test-run> root element", stderr)

    def test_missing_or_malformed_counters_fail(self):
        invalid_values = (None, "", "1.5", "+1", "-1", " 1")
        for counter in validator.COUNTER_ATTRIBUTES:
            for invalid_value in invalid_values:
                with self.subTest(counter=counter, invalid_value=invalid_value):
                    overrides = {counter: invalid_value}
                    self._write_result(counter_overrides=overrides)

                    result, _, stderr = self._run()

                    self.assertEqual(1, result)
                    self.assertIn("{0!r} counter".format(counter), stderr)

    def test_zero_total_is_not_masked_by_another_file(self):
        self._write_result("valid/playmode-results.xml")
        self._write_result("empty/playmode-results.xml", total=0, tests=())

        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertIn("without discovering any tests", stderr)

    def test_failed_result_is_not_masked_by_another_file(self):
        self._write_result("valid/playmode-results.xml")
        self._write_result(
            "failed/playmode-results.xml",
            total=3,
            failed=1,
            tests=REQUIRED_TESTS + (("Example.FailedTest", "Failed"),),
        )

        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertIn("reported 1 failed test", stderr)

    def test_nonpassed_root_result_fails(self):
        self._write_result(root_result="Failed")

        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertIn("test-run result was not Passed", stderr)

    def test_zero_testcasecount_fails(self):
        self._write_result(counter_overrides={"testcasecount": "0"})

        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertIn("reported no test cases", stderr)

    def test_zero_passed_count_fails(self):
        self._write_result(counter_overrides={"passed": "0"})

        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertIn("reported no passed tests", stderr)

    def test_missing_required_test_fails(self):
        self._write_result(tests=REQUIRED_TESTS[:1])

        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertIn("required test was not discovered", stderr)
        self.assertIn(REQUIRED_TESTS[1], stderr)

    def test_skipped_required_test_fails(self):
        self._write_result(
            tests=(REQUIRED_TESTS[0], (REQUIRED_TESTS[1], "Skipped"))
        )

        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertIn("required test did not pass", stderr)

    def test_nonpassed_duplicate_required_test_fails(self):
        self._write_result(
            total=3,
            tests=(
                REQUIRED_TESTS[0],
                REQUIRED_TESTS[1],
                (REQUIRED_TESTS[1], "Skipped"),
            ),
        )

        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertIn("required test did not pass", stderr)
        self.assertIn("Passed, Skipped", stderr)

    def test_required_tests_cannot_be_combined_across_result_files(self):
        self._write_result("first/playmode-results.xml", tests=REQUIRED_TESTS[:1])
        self._write_result("second/playmode-results.xml", tests=REQUIRED_TESTS[1:])

        result, _, stderr = self._run()

        self.assertEqual(1, result)
        self.assertEqual(2, stderr.count("required test was not discovered"))

    def test_multiple_complete_result_files_pass(self):
        self._write_result("first/playmode-results.xml")
        self._write_result("second/playmode-results.xml")

        result, stdout, stderr = self._run()

        self.assertEqual(0, result)
        self.assertEqual(2, stdout.count("Validated"))
        self.assertEqual("", stderr)

    def test_modes_are_validated_independently(self):
        self._write_result("playmode-results.xml")
        self._write_result("editmode-results.xml", total=0, tests=())

        playmode_result, _, playmode_stderr = self._run("playmode")
        editmode_result, _, editmode_stderr = self._run(
            "editmode", required_tests=()
        )

        self.assertEqual(0, playmode_result)
        self.assertEqual("", playmode_stderr)
        self.assertEqual(1, editmode_result)
        self.assertIn("without discovering any tests", editmode_stderr)


if __name__ == "__main__":
    unittest.main()
