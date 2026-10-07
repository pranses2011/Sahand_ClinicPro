#!/usr/bin/env python3
"""Find all invalid PackIconKind values in XAML files using extracted enum."""
import re
import sys
from pathlib import Path

REPO = Path("/home/z/my-project/Sahand_ClinicPro")
enum_members = set(__import__("json").loads(Path("/tmp/packiconkinds.json").read_text()))

# 2) Scan all XAML files for PackIcon Kind="..."
pattern = re.compile(r'Kind="([A-Za-z0-9_]+)"')
invalid = {}
valid_count = 0
for xaml in REPO.rglob("*.xaml"):
    if "obj" in xaml.parts or "bin" in xaml.parts:
        continue
    try:
        text = xaml.read_text(encoding="utf-8", errors="ignore")
    except Exception:
        continue
    for i, line in enumerate(text.splitlines(), 1):
        if 'PackIcon' not in line:
            continue
        for m in pattern.finditer(line):
            kind = m.group(1)
            if kind in enum_members:
                valid_count += 1
            else:
                invalid.setdefault(kind, []).append(f"{xaml.relative_to(REPO)}:{i}")

print(f"Valid icon kinds used: {valid_count}", file=sys.stderr)
if invalid:
    print("INVALID PackIconKind VALUES FOUND:")
    for kind, locs in sorted(invalid.items()):
        print(f"\n  {kind}  ({len(locs)} uses)")
        for loc in locs[:8]:
            print(f"    - {loc}")
        if len(locs) > 8:
            print(f"    ... and {len(locs)-8} more")
else:
    print("No invalid icon kinds found.")
