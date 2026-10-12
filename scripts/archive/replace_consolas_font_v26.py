#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — حذف فونت هاردکد Consolas از همهٔ نماها.

ریشهٔ «اعدادی که فارسی شده‌اند، فونت روی آن‌ها اعمال نمی‌شود و با فونت
پیش‌فرض نمایش می‌دهند (مثلاً شمارهٔ تلفن و کد ملی و سن در لیست بیماران)»:
فونت Consolas فقط لاتین است و ارقام فارسی ندارد؛ WPF برای نویسه‌های فارسی
به فونت جایگزینِ سیستم fallback می‌کند → ارقام فارسی با فونتی متفاوت از
فونت انتخابی کاربر (وزیرمتن) رندر می‌شوند و ناهماهنگ دیده می‌شوند.

راه‌حل: همهٔ «FontFamily="Consolas"»ها به منبع پویای App.CodeFont تبدیل
می‌شوند که در ThemeService به همان فونت انتخابی کاربر (پیش‌فرض وزیرمتن)
مقداردهی می‌شود — ارقام فارسی هم با فونت برنامه رسم می‌شوند.
"""
import re
from pathlib import Path

DESKTOP = Path("/home/z/my-project/Sahand_ClinicPro/src/Sahand.ClinicPro.Desktop")

# فقط فایل‌های منبع — نه خروجی بیلد
xaml_files = list((DESKTOP / "Views").glob("*.xaml"))
xaml_files.append(DESKTOP / "MainWindow.xaml")
xaml_files.append(DESKTOP / "App.xaml")

total = 0
for path in xaml_files:
    if not path.exists():
        continue
    text = path.read_text(encoding="utf-8")
    # الگوی دقیق مقداردهی فونت Consolas (با هر فاصله‌گذاری)
    new_text, n = re.subn(
        r'FontFamily\s*=\s*"Consolas"',
        'FontFamily="{DynamicResource App.CodeFont}"',
        text,
    )
    if n:
        path.write_text(new_text, encoding="utf-8")
        print(f"{path.name}: {n} مورد")
        total += n

print(f"\nمجموع: {total} مورد در XAML تبدیل شد")
