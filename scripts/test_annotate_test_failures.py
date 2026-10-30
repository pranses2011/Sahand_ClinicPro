#!/usr/bin/env python3
"""Regression tests for grouped GitHub failure annotations."""
import contextlib
import io
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import annotate_test_failures as annotations


class AnnotationTests(unittest.TestCase):
    def test_trx_results_are_grouped_and_escaped(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            tests = "".join(
                f'<UnitTestResult testName="Case {i}" outcome="Failed">'
                '<Output><ErrorInfo><Message>50% failure</Message></ErrorInfo></Output>'
                '</UnitTestResult>' for i in range(32)
            )
            (root / "results.trx").write_text(
                f'<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>{tests}</Results></TestRun>',
                encoding="utf-8",
            )
            output = io.StringIO()
            with patch("sys.argv", ["annotations", str(root / "missing.log"), str(root)]), contextlib.redirect_stdout(output):
                annotations.main()
            lines = output.getvalue().splitlines()
            self.assertEqual(3, len(lines))
            self.assertTrue(all(line.startswith("::error::") for line in lines))
            self.assertIn("Case 0: 50%25 failure", lines[0])
            self.assertIn("Case 31", lines[-1])
            self.assertIn("%0A", lines[0])

    def test_missing_trx_falls_back_to_test_log(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            log = root / "test.log"
            log.write_text("Failed Demo.Test [1 ms]\nError Message:\n  detail\nStack Trace:\n", encoding="utf-8")
            self.assertEqual(["Failed Demo.Test [1 ms] | detail"], annotations.log_failures(log))


if __name__ == "__main__":
    unittest.main()
