#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
╔══════════════════════════════════════════════════════════════════════════╗
║  resx_sync.py — همگام‌سازی فایل‌های زبان                                    ║
║                                                                          ║
║  وظیفه:                                                                  ║
║    ۱. افزودن کلیدهای جدید به Strings.resx (فارسی) و Strings.en.resx      ║
║    ۲. گزارشِ کلیدهایی که در یک زبان هستند و در دیگری نیستند              ║
║    ۳. مرتب‌سازی کلیدها بر پایه‌ی نام (برای خوانایی و دیفِ تمیز)            ║
║                                                                          ║
║  استفاده:                                                                ║
║    python3 tools/resx_sync.py                                            ║
╚══════════════════════════════════════════════════════════════════════════╝
"""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LOC = ROOT / "00-foundation" / "Localization"
FA = LOC / "Strings.resx"
EN = LOC / "Strings.en.resx"

DATA_RE = re.compile(
    r'[ \t]*<data name="(?P<name>[^"]+)"[^>]*>\s*'
    r'<value>(?P<value>.*?)</value>'
    r'(?:\s*<comment>(?P<comment>.*?)</comment>)?\s*</data>\s*\n?',
    re.S,
)


def load(path: Path) -> tuple[str, dict[str, tuple[str, str]]]:
    """فایل resx را به (قالب بدون داده‌ها، نگاشتِ نام ← (مقدار، توضیح)) تجزیه می‌کند."""
    text = path.read_text(encoding="utf-8")
    entries: dict[str, tuple[str, str]] = {}

    def take(m: re.Match[str]) -> str:
        entries[m.group("name")] = (m.group("value") or "", m.group("comment") or "")
        return ""

    skeleton = DATA_RE.sub(take, text)
    return skeleton, entries


def render(path: Path, skeleton: str, entries: dict[str, tuple[str, str]]) -> None:
    """داده‌ها را مرتب و در انتهای قالب می‌نویسد."""
    body = []
    for name in sorted(entries, key=lambda k: (k.split(".")[0].lower(), k.lower())):
        value, comment = entries[name]
        block = f'  <data name="{name}" xml:space="preserve">\n    <value>{value}</value>\n'
        if comment:
            block += f"    <comment>{comment}</comment>\n"
        block += "  </data>\n"
        body.append(block)

    out = skeleton.rstrip()
    out = re.sub(r"\s*</root>\s*$", "\n", out)
    out += "\n" + "".join(body) + "</root>\n"
    path.write_text(out, encoding="utf-8")


# ══════════════════════════════════════════════════════════════════════════
# کلیدهای مشترکِ لایه‌ی UI که در کنترل‌های پایه مصرف می‌شوند
# ══════════════════════════════════════════════════════════════════════════
NEW_COMMON: dict[str, tuple[str, str]] = {
    "Common.Actions":        ("عملیات", "Actions"),
    "Common.Active":         ("فعال", "Active"),
    "Common.All":            ("همه", "All"),
    "Common.Back":           ("بازگشت", "Back"),
    "Common.Clear":          ("پاک کردن", "Clear"),
    "Common.Code":           ("کد", "Code"),
    "Common.Details":        ("جزئیات", "Details"),
    "Common.EmptyMessage":   ("هنوز داده‌ای برای نمایش وجود ندارد.", "There is no data to display yet."),
    "Common.EmptyTitle":     ("موردی یافت نشد", "Nothing found"),
    "Common.Exit":           ("خروج", "Exit"),
    "Common.Filter":         ("فیلتر", "Filter"),
    "Common.Help":           ("راهنما", "Help"),
    "Common.Inactive":       ("غیرفعال", "Inactive"),
    "Common.Name":           ("نام", "Name"),
    "Common.No":             ("خیر", "No"),
    "Common.Notes":          ("توضیحات", "Notes"),
    "Common.Of":             ("از", "of"),
    "Common.PageX":          ("صفحه", "Page"),
    "Common.Relations":      ("ارتباطات", "Relationships"),
    "Common.Reset":          ("بازنشانی", "Reset"),
    "Common.RowsPerPage":    ("سطر در هر صفحه", "Rows per page"),
    "Common.Settings":       ("تنظیمات", "Settings"),
    "Common.Showing":        ("نمایش", "Showing"),
    "Common.Submit":         ("ثبت", "Submit"),
    "Common.Yes":            ("بله", "Yes"),
}


def main() -> None:
    skel_fa, fa = load(FA)
    skel_en, en = load(EN)

    added = 0
    for key, (persian, english) in NEW_COMMON.items():
        if key not in fa:
            fa[key] = (persian, "")
            added += 1
        if key not in en:
            en[key] = (english, "")
            added += 1

    render(FA, skel_fa, fa)
    render(EN, skel_en, en)

    only_fa = sorted(set(fa) - set(en))
    only_en = sorted(set(en) - set(fa))

    print(f"✅ کلیدهای فارسی : {len(fa)}")
    print(f"✅ کلیدهای انگلیسی: {len(en)}")
    print(f"➕ افزوده‌شده     : {added}")
    if only_fa:
        print(f"⚠ فقط در فارسی   : {len(only_fa)} → {only_fa[:8]}")
    if only_en:
        print(f"⚠ فقط در انگلیسی : {len(only_en)} → {only_en[:8]}")
    if not only_fa and not only_en:
        print("✅ دو زبان کاملاً هم‌تراز هستند")


if __name__ == "__main__":
    main()
