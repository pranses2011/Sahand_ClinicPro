#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📄 تفکیک CHANGELOG — چرخهٔ ۵۵ (BUG-11).
فایل ۳۹۷KB/۴۷ سرچشمه‌ای به دو بخش شکسته می‌شود:
  CHANGELOG.md            — سرچشمه‌های اخیر (v2.10.0 به بعد) + پیوند بایگانی
  docs/changelog-archive  — ۴۱ سرچشمهٔ تاریخی (v2.8.0 و قدیمی‌تر)
روش: شناسایی خطوط «## [vX.Y.Z]» و برش بر مبنای خط آغازِ نسخهٔ مرزی.
idempotent — دوباره اجرا شود چیزی خراب نمی‌کند (اگر بایگانی موجود باشد رد می‌شود)."""
import pathlib
import re
import sys

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
CHANGELOG = REPO / "CHANGELOG.md"
ARCHIVE = REPO / "docs" / "changelog-archive.md"
KEEP_FROM = "v2.9.0"  # این نسخه و جدیدترها در CHANGELOG می‌مانند

text = CHANGELOG.read_text(encoding="utf-8")
heads = [(m.start(), m.group(1)) for m in re.finditer(r"^## \[(v[\d.]+)\]", text, re.M)]
if not heads:
    sys.exit("⛔ هیچ سرچشمهٔ نسخه‌ای پیدا نشد")

keep_idx = next((i for i, (_, ver) in enumerate(heads) if ver == KEEP_FROM), None)
if keep_idx is None:
    sys.exit(f"⛔ نسخهٔ مرزی {KEEP_FROM} در CHANGELOG نیست")

cut_pos = heads[keep_idx][0]
head_part = text[:cut_pos].rstrip() + "\n"
archive_part = text[cut_pos:]

if ARCHIVE.exists():
    sys.exit("⛔ بایگانی از قبل وجود دارد — برای جلوگیری از ازدواج، ابتدا آن را بازبینی/حذف کنید")

ARCHIVE.parent.mkdir(parents=True, exist_ok=True)
archive_header = (
    "<div dir=\"rtl\">\n\n"
    "# 📚 بایگانی سرچشمهٔ نسخه‌ها — v2.8.0 و قدیمی‌تر\n\n"
    "> این فایل بخش تاریخی CHANGELOG است که در چرخهٔ ۵۵ (BUG-11) از فایل اصلی "
    "جدا شد تا سرچشمهٔ نسخه‌های تازه کوتاه و خوانا بماند. سرچشمهٔ نسخه‌های "
    "جدید همیشه در `CHANGELOG.md` ریشهٔ پروژه است.\n\n"
    "</div>\n\n---\n\n"
)
ARCHIVE.write_text(archive_header + archive_part, encoding="utf-8")

pointer = (
    "\n---\n\n"
    "> 📚 **بایگانی نسخه‌های قدیمی‌تر (v2.8.0 به قبل):** "
    f"[docs/changelog-archive.md](docs/changelog-archive.md) — {len(heads) - keep_idx} سرچشمهٔ تاریخی\n"
)
CHANGELOG.write_text(head_part + pointer, encoding="utf-8")

import subprocess
print("✅ تفکیک انجام شد:")
print("   CHANGELOG.md:", f"{CHANGELOG.stat().st_size:,}", "بایت —", keep_idx, "سرچشمهٔ اخیر (از", KEEP_FROM, ")")
print("   docs/changelog-archive.md:", f"{ARCHIVE.stat().st_size:,}", "بایت —", len(heads) - keep_idx, "سرچشمهٔ تاریخی")
