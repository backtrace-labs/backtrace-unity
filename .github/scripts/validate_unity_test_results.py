#!/usr/bin/env python3
"""Fail closed when Unity's NUnit result artifacts do not prove tests ran."""

import argparse
import re
import sys
import xml.etree.ElementTree as ElementTree
from pathlib import Path


COUNTER_ATTRIBUTES = (
    "testcasecount",
    "total",
    "passed",
    "failed",
    "inconclusive",
    "skipped",
)
INTEGER_PATTERN = re.compile(r"[0-9]+\Z")


class ValidationError(Exception):
    """Raised when a Unity test-result artifact violates the CI contract."""


def _parse_counter(root, attribute, result_file):
    raw_value = root.get(attribute)
    if raw_value is None:
        raise ValidationError(
            "{0}: <test-run> is missing the {1!r} counter".format(
                result_file, attribute
            )
        )

    if INTEGER_PATTERN.fullmatch(raw_value) is None:
        raise ValidationError(
            "{0}: <test-run> has a malformed {1!r} counter: {2!r}".format(
                result_file, attribute, raw_value
            )
        )

    return int(raw_value)


def validate_result_file(result_file, required_tests):
    """Validate one NUnit XML file without relying on any other result file."""
    try:
        root = ElementTree.parse(str(result_file)).getroot()
    except (OSError, ElementTree.ParseError) as error:
        raise ValidationError(
            "{0}: could not parse Unity test results ({1})".format(
                result_file, type(error).__name__
            )
        )

    if root.tag != "test-run":
        raise ValidationError(
            "{0}: expected a <test-run> root element, found <{1}>".format(
                result_file, root.tag
            )
        )

    counters = {
        attribute: _parse_counter(root, attribute, result_file)
        for attribute in COUNTER_ATTRIBUTES
    }

    if counters["total"] <= 0:
        raise ValidationError(
            "{0}: Unity completed without discovering any tests".format(result_file)
        )

    if counters["testcasecount"] <= 0:
        raise ValidationError(
            "{0}: Unity reported no test cases".format(result_file)
        )

    if counters["passed"] <= 0:
        raise ValidationError(
            "{0}: Unity reported no passed tests".format(result_file)
        )

    if counters["failed"] > 0:
        raise ValidationError(
            "{0}: Unity reported {1} failed test(s)".format(
                result_file, counters["failed"]
            )
        )

    if root.get("result") != "Passed":
        raise ValidationError(
            "{0}: Unity test-run result was not Passed: {1!r}".format(
                result_file, root.get("result")
            )
        )

    matching_results = {required_test: [] for required_test in required_tests}
    for test_case in root.iter("test-case"):
        full_name = test_case.get("fullname")
        if full_name in matching_results:
            matching_results[full_name].append(test_case.get("result"))

    for required_test, results in matching_results.items():
        if not results:
            raise ValidationError(
                "{0}: required test was not discovered: {1}".format(
                    result_file, required_test
                )
            )
        if any(result != "Passed" for result in results):
            raise ValidationError(
                "{0}: required test did not pass: {1} (results: {2})".format(
                    result_file,
                    required_test,
                    ", ".join(str(result) for result in results),
                )
            )

    return counters["total"]


def discover_result_files(results_root, mode):
    """Return only result files for the requested Unity test mode."""
    pattern = "{0}-results.xml".format(mode)
    return sorted(path for path in results_root.rglob(pattern) if path.is_file())


def build_argument_parser():
    parser = argparse.ArgumentParser(
        description="Validate Unity NUnit XML artifacts without aggregating away failures."
    )
    parser.add_argument(
        "--results-root",
        required=True,
        type=Path,
        help="Directory containing Unity test-result artifacts.",
    )
    parser.add_argument(
        "--mode",
        required=True,
        choices=("playmode", "editmode"),
        help="Unity test mode whose result files must be validated.",
    )
    parser.add_argument(
        "--require-test",
        action="append",
        default=[],
        help="Fully qualified test name that must be present and passed in every result file.",
    )
    return parser


def main(arguments=None):
    options = build_argument_parser().parse_args(arguments)
    result_files = discover_result_files(options.results_root, options.mode)

    if not result_files:
        print(
            "Error: no {0}-results.xml files found under {1}.".format(
                options.mode, options.results_root
            ),
            file=sys.stderr,
        )
        return 1

    errors = []
    validated = []
    for result_file in result_files:
        try:
            total = validate_result_file(result_file, options.require_test)
            validated.append((result_file, total))
        except ValidationError as error:
            errors.append(str(error))

    if errors:
        for error in errors:
            print("Error: {0}".format(error), file=sys.stderr)
        return 1

    for result_file, total in validated:
        print("Validated {0}: {1} test(s), 0 failed.".format(result_file, total))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
