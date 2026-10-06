#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🔍 ممیزی ارجاعات منبع XAML دسکتاپ در برابر کلیدهای واقعی پکیج MaterialDesignThemes
اجرا از ریشه مخزن:  python3 scripts/audit_xaml_resources.py
- همه ارجاعات StaticResource/DynamicResource فایل‌های XAML دسکتاپ استخراج می‌شود
- هر کلید باید یا در XAML پروژه تعریف شده باشد (x:Key) یا در مانیفست پکیج موجود باشد
- ریشه یادداشت: چون XAML هنگام بیلد ارجاعات StaticResource را اعتبارسنجی نمی‌کند،
  این ممیزی تنها گارد پیش از اجراست (تا کرش‌های شبیه چرخه ۱۴ رخ ندهد)
"""
import pathlib
import re
import sys
from collections import defaultdict

ROOT = pathlib.Path(__file__).resolve().parent.parent
XAML_DIR = ROOT / "src" / "Sahand.ClinicPro.Desktop"
MANIFEST = ROOT / "scripts" / "materialdesign-keys-5.3.2.txt"

pkg_keys = {ln.strip() for ln in MANIFEST.read_text(encoding="utf-8").splitlines()
            if ln.strip() and not ln.startswith("#")}
local_keys: set[str] = set()
xaml_files = sorted(XAML_DIR.rglob("*.xaml"))
for f in xaml_files:
    local_keys.update(re.findall(r'x:Key="([^"]+)"',
                                 f.read_text(encoding="utf-8-sig", errors="replace")))

SYSTEM_KEYS = {"FocusVisual"}
REF = re.compile(r'(?:Static|Dynamic)Resource\s+([A-Za-z_][A-Za-z0-9_.-]*)')

missing: dict[str, list[str]] = defaultdict(list)
for f in xaml_files:
    rel = str(f.relative_to(ROOT))
    txt = f.read_text(encoding="utf-8-sig", errors="replace")
    for m in REF.finditer(txt):
        key = m.group(1)
        if key in local_keys or key in pkg_keys or key in SYSTEM_KEYS:
            continue
        missing[key].append(rel)

if missing:
    print("⛔ ارجاعات منبع بدون تعریف (در زمان اجرا کرش یا بی‌رنگ می‌شوند):")
    for key, files in sorted(missing.items()):
        print(f"  ▸ {key}  ({len(files)} فایل)")
    sys.exit(1)
print(f"✅ {len(xaml_files)} فایل XAML — همه ارجاعات منبع معتبرند "
      f"({len(pkg_keys)} کلید پکیج + {len(local_keys)} کلید محلی)")
