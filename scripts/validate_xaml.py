#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
════════════════════════════════════════════════════════════════════════════
 پایش سلامت XML همهٔ فایل‌های XAML پروژهٔ سهند کلینیک‌پرو
════════════════════════════════════════════════════════════════════════════
 پشتوانهٔ ایجاد (چرخهٔ v2.4.7): خطای «MC3000: '{' is an unexpected token» در
 InsuranceView.xaml خط ۱۲۱ که بیلد کل دسکتاپ را متوقف کرده بود — کوتیشنِ
 آغازینِ مقدار Converter در کامیت dff31cd جا افتاده بود.

 چرا تجزیه‌گر XML و نه Regex؟
   کامپایلر XAML (خطاهای MC30xx) دقیقاً همان چیزی را می‌بیند که تجزیه‌گر
   XML می‌بیند. الگوهای Regex برای یافتن «= { بی‌کوتیشن» مثبت کاذب فراوان
   می‌دهند چون Markup Extensionهای تودرتو (Converter داخل Binding) کاملاً
   مجازند و خودشان داخل کوتیشن صفت بیرونی‌اند.

 روش اجرا:  python3 scripts/validate_xaml.py
 خروجی:    فهرست فایل‌های نامعتبر + محل خطا — کد خروج ۰ یعنی همه سالم؛
            مناسب برای قرارگیری در CI یا اجرای پیش از هر پوش.
"""
import os
import sys
import xml.etree.ElementTree as ET

# ریشهٔ سورس — نسبت به محل این اسکریپت (پوشهٔ scripts ریپو) محاسبه می‌شود
# تا روی هر ایستگاهی (لینوکس/ویندوز) بدون تغییر کار کند.
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "src"))

bad_files = []   # فایل‌هایی که XML معتبر ندارند (همان‌هایی که MC3000 می‌گیرند)
total_xaml = 0   # شمارندهٔ کل فایل‌های بررسی‌شده

for dirpath, _dirs, files in os.walk(ROOT):
    for name in files:
        if not name.lower().endswith(".xaml"):
            continue
        total_xaml += 1
        path = os.path.join(dirpath, name)
        rel = os.path.relpath(path, ROOT)
        try:
            with open(path, "r", encoding="utf-8") as f:
                text = f.read()
            # تجزیهٔ کامل — هر خطای ساختاری (توکن نامعتبر، تگ باز/بسته‌ناموزن،
            # کوتیشن جاافتاده و…) دقیقاً همان لحظه‌ای که کامپایلر XAML می‌گیرد
            ET.fromstring(text)
        except ET.ParseError as e:
            bad_files.append((rel, str(e)))
        except Exception as e:  # خطای خواندن/انکودینگ — باید جدی گرفته شود
            bad_files.append((rel, f"خطای دیگر: {e}"))

print(f"تعداد کل فایل‌های XAML بررسی‌شده: {total_xaml}")
print(f"فایل‌های XML نامعتبر: {len(bad_files)}")
print("═" * 70)
for rel, err in bad_files:
    print(f"  ✗ {rel}")
    print(f"      {err}")

# کد خروجی: ۰ = همه سالم (سبز برای CI) | ۱ = مشکل هست، پوش نکن
sys.exit(1 if bad_files else 0)
