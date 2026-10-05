#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🔍 ممیزی مجوز endpointهای API — چرخه دوازدهم (A3، بند ۴-۱۰)"""
import pathlib
import re

API = pathlib.Path("/home/z/my-project/Sahand_ClinicPro/src/Sahand.ClinicPro.API/Controllers")

rows = []
for f in sorted(API.glob("*.cs")):
    src = f.read_text(encoding="utf-8")
    # هر اکشن: بلوک ویژگی‌ها + امضا — با جداکننده‌ی خط‌محور
    for m in re.finditer(r'((?:\s*\[[^\]\n]+\]\s*)+)\s+public\s+(?:async\s+)?[^\s(]+\s+(\w+)\s*\(', src):
        attrs, method = m.group(1), m.group(2)
        vm = re.search(r'\[Http(Get|Post|Put|Delete|Patch)(?:\(\s*"([^"]*)")?', attrs)
        if not vm:
            continue
        verb = vm.group(1).upper()
        route = vm.group(2) or ""
        rows.append((f.stem, verb, route, method,
                     "HasPermission(" in attrs,
                     "AllowAnonymous" in attrs,
                     "[Authorize]" in attrs))

total = len(rows)
guarded = sum(1 for r in rows if r[4])
anon = sum(1 for r in rows if r[5])
authonly = [r for r in rows if not r[4] and not r[5]]
print(f"TOTAL={total}  HasPermission={guarded}  AllowAnonymous={anon}  Authorize-فقط={len(authonly)}")
print()
print("── بدون HasPermission و بدون AllowAnonymous ──")
for r in sorted(authonly):
    print(f"  {r[0]:32s} {r[1]:6s} /{r[2]:32s} {r[3]}")
print()
print("── AllowAnonymous (باید فقط auth/health/ping لایسنس باشند) ──")
for r in sorted(x for x in rows if x[5]):
    print(f"  {r[0]:32s} {r[1]:6s} /{r[2]:32s} {r[3]}")
