#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_bindings.py — بررسیِ یکپارچگیِ View ↔ ViewModel

هر اتصالِ {Binding X} در فایل‌های XAML را برمی‌دارد و بررسی می‌کند که در
ViewModelِ متناظر (یا در مدل‌های مشترک یا پایه‌ها) عضوی با همان نام وجود داشته
باشد. خروجیِ این ابزار معیارِ پذیرشِ «هیچ اتصالِ یتیمی باقی نمانده» است.
"""

from __future__ import annotations

import os
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from gen_views import vm_name  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
BIND = re.compile(r"\{Binding ([A-Za-z_]\w*)")

# اعضای فراهم‌شده توسط پایه‌ها (نیازی به تعریفِ دوباره در فرزند نیست)
BASE = set("""IsBusy IsNotBusy BusyMessage ErrorMessage SuccessMessage HasErrors Items SelectedItem
HasSelection CanEdit CanDelete IsEmpty SearchText PageNumber PageSize TotalCount TotalPages
PageSummary IsDirty IsReadOnly IsEditable IsEditMode WindowTitle CloseAction LoadCommand
CreateCommand EditCommand DeleteCommand RefreshCommand NextPageCommand PreviousPageCommand
SaveCommand CancelCommand""".split())


def declared_members(text: str) -> set[str]:
    """نامِ همه‌ی اعضای عمومیِ یک متنِ C#"""
    found = set(re.findall(r"public (?:[\w?<>,\s\.\[\]]+?) (\w+)", text))
    found |= set(re.findall(r"public (?:sealed |partial )*(?:class|record) (\w+)", text))
    # پارامترهای موضعیِ recordها (مانند RelationItem(string From, string To, string Why))
    for rec in re.finditer(r"public sealed record \w+\(([^)]*)\)", text):
        for part in rec.group(1).split(","):
            part = part.strip()
            if not part:
                continue
            bits = part.split()
            if len(bits) >= 2:
                found.add(bits[-1].rstrip("?"))
            elif bits:
                found.add(bits[-1].rstrip("?"))
    return found


def main() -> None:
    shared = (ROOT / "04-viewmodels" / "_Shared.cs").read_text(encoding="utf-8")
    shared_members = declared_members(shared)

    total = 0
    missing: dict[str, list[str]] = {}

    for fp in sorted((ROOT / "03-views").glob("*.xaml")):
        name = fp.stem
        binds = set(BIND.findall(fp.read_text(encoding="utf-8")))
        total += len(binds)

        vm_path = ROOT / "04-viewmodels" / f"{vm_name({'name': name})}.cs"
        if not vm_path.exists():
            missing.setdefault(name, []).append("«فایل ViewModel یافت نشد»")
            continue

        members = declared_members(vm_path.read_text(encoding="utf-8")) | shared_members
        for b in sorted(binds):
            if b not in members and b not in BASE:
                missing.setdefault(name, []).append(b)

    print(f"اتصال‌های یکتای بررسی‌شده : {total}")
    print(f"صفحات دارای اتصالِ یتیم  : {len(missing)}")
    if missing:
        for k in sorted(missing):
            print(f"  ⚠ {k}: {missing[k][:10]}")
    else:
        print("  ✅ هیچ اتصالِ یتیمی نیست")


if __name__ == "__main__":
    main()
