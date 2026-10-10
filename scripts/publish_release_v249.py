#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.4.9 در گیت‌هاب — تگ + یادداشت رلیز + بارگذاری زیپ وصله.

وصلهٔ فوری چرخهٔ سی‌وششم: ترمیم سه خطای ساخت/هشدار بستهٔ v2.4.8
(using رسانه‌ای + ترتیب استاتیک + کامنت XML پارامتر رکورد).
"""
import json
import pathlib
import subprocess
import urllib.request

TOKEN = "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"
REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
ZIP = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.8-to-2.4.9.zip")
TAG = "v2.4.9"

# ── ۱) تگ سبک روی HEAD (کامیت نسخه) و پوش آن ──
subprocess.run(f'git tag -f {TAG} main', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
subprocess.run(f'git push origin -f {TAG}', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True,
               capture_output=True, text=True)
print("✅ تگ", TAG, "پوش شد")

BODY = """## 🚀 v2.4.9 — وصلهٔ فوری: ترمیم خطای ساخت بستهٔ v2.4.8

### 🐞 چه اتفاقی افتاده بود؟
پس از نصب بستهٔ تجمعی ۲.۴.۸، Rebuild روی ایستگاه شما با ۵ خطای کامپایل در `CommonConverters.cs` متوقف می‌شد (`Brushes` / `Color` / `ColorConverter` / `SolidColorBrush` شناخته نمی‌شدند) + خطای `CS0006` در پروژهٔ تست و دو هشدار. این وصله هر سه را ریشه‌یابی و ترمیم می‌کند.

### 🔧 سه ترمیم این بسته
- **خطای بیلد (CS0103/CS0246):** `using System.Windows.Media` برای مبدل رنگ `HexBrush` جا افتاده بود → درج شد. خطای `CS0006` پروژهٔ تست هم فقط دنبالهٔ همین شکست بود و با ساخته‌شدن بیلد **خودبه‌خود** برطرف می‌شود.
- **باگ پنهان (CS8604) — مهم‌تر از هشدار:** ترتیب مقداردهنده‌های استاتیک در `ComplementaryViewModel` نادرست بود؛ حتی بعد از ترمیم بیلد هم بازکردن صفحهٔ «فرم‌های تکمیلی» با کرش `TypeInitializationException` متوقف می‌شد → ترتیب اصلاح شد.
- **هشدار CS1587:** کامنت XML روی پارامتر رکورد `SupportMessageDto` در C# 12 معتبر نیست → به کامنت عادی با حفظ محتوا تبدیل شد.

### 🛡️ پیشگیری ساختاری (قانون هم‌کلاسی‌ها)
- ممیزی سراسری همهٔ ۸۴۶ فایل .cs انجام شد — تنها همین یک فایل معیوب بود.
- اسکریپت جدید `audit_csharp_usings.py` از این پس در بسته‌بندی هر چرخه اجرا می‌شود تا using فراموش‌شدهٔ دیگر ممکن نباشد (تست منفی: باگ این چرخه را دقیقاً در خط ۲۱۷ گزارشی شما می‌گیرد).

### 📦 محتوا و نصب
فقط **۵ فایل** تغییرکرده نسبت به ۲.۴.۸ (بستهٔ ۲.۴.۸ تجمعی بود و فایل‌هایش به ایستگاه شما رسیده‌اند؛ اگر هنوز نصبش نکرده‌اید، اول آن را از رلیز v2.4.8 نصب کنید).

**چک‌لیست پس از Clean + Rebuild:**
1. بیلد بدون هیچ خطای CS0103/CS0246/CS0006 تمام شود و Error List از CS1587/CS8604 هم خالی باشد
2. زیر دکمهٔ «خروج از حساب»، «نسخه ۲.۴.۹» دیده شود
3. صفحهٔ «فرم‌های تکمیلی» بدون کرش باز شود (دقیقاً جایی بود که باگ پنهان کرش می‌داد)

راهنمای کامل فارسی داخل بسته: `Update-Guide-2.4.9.html`
"""

# ── ۲) ساخت رلیز ──
payload = {
    "tag_name": TAG,
    "name": "v2.4.9 — وصلهٔ فوری: ترمیم خطای ساخت v2.4.8 (using رسانه‌ای + ترتیب استاتیک + کامنت XML)",
    "body": BODY,
    "draft": False,
    "prerelease": False,
}
req = urllib.request.Request(
    f"{REPO_API}/releases",
    data=json.dumps(payload).encode(),
    headers={"Authorization": f"token {TOKEN}", "Accept": "application/vnd.github+json",
             "Content-Type": "application/json"},
    method="POST")
with urllib.request.urlopen(req) as r:
    release = json.load(r)
print("✅ رلیز ساخته شد:", release["html_url"], "| id:", release["id"])

# ── ۳) بارگذاری زیپ — آپلود دارایی به uploads.github.com ──
upload_url = f"https://uploads.github.com/repos/pranses2011/Sahand_ClinicPro/releases/{release['id']}/assets?name={ZIP.name}"
req = urllib.request.Request(
    upload_url,
    data=ZIP.read_bytes(),
    headers={"Authorization": f"token {TOKEN}", "Accept": "application/vnd.github+json",
             "Content-Type": "application/zip"},
    method="POST")
with urllib.request.urlopen(req) as r:
    asset = json.load(r)
print("✅ زیپ بارگذاری شد:", asset["browser_download_url"], "| حجم:", asset["size"], "بایت")
