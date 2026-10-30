#!/usr/bin/env python3
"""Expose test names and failure messages as GitHub Actions annotations."""
import re
import sys
from pathlib import Path

path = Path(sys.argv[1])
if not path.exists():
    print("Test log is unavailable; inspect the preceding step.")
    raise SystemExit(0)
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
    failures.append(" | ".join([line.strip(), *detail]))
for failure in failures[:30]:
    escaped = failure.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
    print("::error::" + escaped)
if not failures:
    # Test-host/restore failures may not have a named test; expose the last
    # lines so they are still diagnosable without artifact download access.
    for line in lines[-12:]:
        if line.strip():
            print("::error::" + line.replace("%", "%25"))
