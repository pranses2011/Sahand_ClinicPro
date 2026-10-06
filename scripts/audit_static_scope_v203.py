#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
ممیزی scope-محور ارجاعات StaticResource — چرخه شانزدهم (گزارش دهم مالک)
ریشه: StaticResource در زمان اجرا فقط از (۱) منابع خودِ فایل (۲) App.xaml
حل می‌شود؛ تعریف کلید در فایل‌های دیگر (مثل BoolToVis در LoginWindow) برای
فایل بی‌خ است → XamlParseException هنگام InitializeComponent.
تست XamlResourceAuditTests فعلی کلیدها را «سراسری» جمع می‌کند و همین کورمندی
را دارد؛ این اسکریپت گزارش کامل همهٔ نقض‌های scope را می‌دهد (معیار تست جدید).
"""
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
XAML_DIR = REPO / "src" / "Sahand.ClinicPro.Desktop"
MANIFEST = REPO / "scripts" / "materialdesign-keys-5.3.2.txt"
SYSTEM_KEYS = {"FocusVisual"}

REF_RE = re.compile(r"(?:Static|Dynamic)Resource\s+([A-Za-z_][A-Za-z0-9_.\-]*)")
STATIC_RE = re.compile(r"StaticResource\s+([A-Za-z_][A-Za-z0-9_.\-]*)")
KEY_DEF_RE = re.compile(r'x:Key="([^"]+)"')


def main() -> int:
    pkg_keys = {
        line.strip()
        for line in MANIFEST.read_text(encoding="utf-8-sig").splitlines()
        if line.strip() and not line.strip().startswith("#")
    }

    xaml_files = sorted(XAML_DIR.rglob("*.xaml"))
    print(f"فایل‌های XAML: {len(xaml_files)}")

    # کلیدهای تعریف‌شده در هر فایل
    keys_per_file: dict[str, set[str]] = {}
    for f in xaml_files:
        text = f.read_text(encoding="utf-8-sig")
        keys_per_file[str(f)] = set(KEY_DEF_RE.findall(text))

    app_keys = keys_per_file.get(str(XAML_DIR / "App.xaml"), set())
    all_keys: set[str] = set().union(*keys_per_file.values()) if keys_per_file else set()

    violations: dict[str, list[str]] = {}
    for f in xaml_files:
        rel = f.relative_to(REPO)
        text = f.read_text(encoding="utf-8-sig")
        allowed = keys_per_file[str(f)] | app_keys | pkg_keys | SYSTEM_KEYS
        for m in STATIC_RE.finditer(text):
            key = m.group(1)
            if key not in allowed:
                violations.setdefault(key, []).append(str(rel))

    if not violations:
        print("✅ صفر نقض scope — همهٔ ارجاعات StaticResource در محدودهٔ فایل/App حل می‌شوند")
        return 0

    print(f"❌ {len(violations)} کلید بدون تعریف در محدودهٔ قابل‌حل:\n")
    for key in sorted(violations):
        files = violations[key]
        print(f"  {key}  ({len(files)} ارجاع در {len(set(files))} فایل)")
        for rel in sorted(set(files)):
            print(f"      - {rel}")
    return 1


if __name__ == "__main__":
    sys.exit(main())
