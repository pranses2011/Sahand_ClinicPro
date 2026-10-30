#!/usr/bin/env python3
"""Expose test failures in check annotations without requiring artifact access.

GitHub limits annotations per step. Group failures into a few annotations so
large test suites do not silently hide all but the first ten failures.
"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def trx_failures(directory: Path) -> list[str]:
    failures = []
    for path in sorted(directory.rglob("*.trx")):
        try:
            root = ET.parse(path).getroot()
        except (OSError, ET.ParseError):
            continue
        for result in root.iter():
            if result.tag.rsplit("}", 1)[-1] != "UnitTestResult" or result.get("outcome") != "Failed":
                continue
            message = next((element.text or "" for element in result.iter()
                            if element.tag.rsplit("}", 1)[-1] == "Message"), "")
            name = result.get("testName", "Unnamed test")
            compact = " ".join(message.split())
            # FluentAssertions may print a large HTML/JSON value before the
            # actual mismatch, which is usually at the end of the message.
            if len(compact) > 1200:
                compact = compact[:400] + " … [truncated] … " + compact[-800:]
            failures.append(f"{name}: {compact}")
    return list(dict.fromkeys(failures))


def log_failures(path: Path) -> list[str]:
    if not path.exists():
        return []
    lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    failures = []
    for index, line in enumerate(lines):
        if not re.match(r"^\s*(Failed |\[FAIL\])", line, re.I):
            continue
        detail = []
        for following in lines[index + 1 : index + 18]:
            if re.match(r"^\s*(Stack Trace:|Failed |\[FAIL\]|Standard Output Messages:)", following, re.I):
                break
            if following.strip() and not re.match(r"^\s*Error Message:", following, re.I):
                detail.append(following.strip())
        failures.append(" | ".join([line.strip(), *detail])[:1200])
    return failures


def annotate(message: str) -> None:
    escaped = message.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
    print("::error::" + escaped)


def main() -> None:
    log = Path(sys.argv[1])
    results = Path(sys.argv[2]) if len(sys.argv) > 2 else Path("TestResults")
    failures = trx_failures(results) if results.exists() else []
    if not failures:
        failures = log_failures(log)
    if failures:
        batch_size, max_batches = 15, 9
        for start in range(0, min(len(failures), batch_size * max_batches), batch_size):
            batch = failures[start : start + batch_size]
            annotate(f"Failed tests {start + 1}–{start + len(batch)} of {len(failures)}:\n" + "\n".join(batch))
        if len(failures) > batch_size * max_batches:
            print(f"{len(failures) - batch_size * max_batches} additional failures are in the test-results artifact.")
    elif log.exists():
        # Test-host/restore failures may not have a named test.
        tail = log.read_text(encoding="utf-8", errors="replace").splitlines()[-12:]
        annotate("Test host or runner failed:\n" + "\n".join(line for line in tail if line.strip()))
    else:
        annotate("Test log and test results are unavailable; inspect the preceding step.")


if __name__ == "__main__":
    main()
