#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.4.7 در گیت‌هاب — تگ + یادداشت رلیز + بارگذاری زیپ آپدیت.
وصلهٔ فوری خطای ساخت MC3000 در InsuranceView.xaml (بیلد‌شکننده)."""
import json
import pathlib
import subprocess
import urllib.request

TOKEN = "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"
REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
ZIP = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.6-to-2.4.7.zip")
TAG = "v2.4.7"

# ── ۱) ساخت تگ سبک روی آخرین کامیت main و پوش آن ──
subprocess.run(f'git tag -f {TAG} main', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
subprocess.run(f'git push origin -f {TAG}', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True,
               capture_output=True, text=True)
print("✅ تگ", TAG, "پوش شد")

BODY = """## 🐞 وصلهٔ فوری v2.4.7 — تعمیر خطای ساخت `MC3000` در صفحهٔ بیمه

بیلد دسکتاپ روی ایستگاه شما با خطای
```
MC3000: '{' is an unexpected token. The expected token is '"' or '''.
        InsuranceView.xaml — Line 121, position 93
```
متوقف شده بود و برنامه اصلاً کامپایل نمی‌شد.

### ریشه
در بروزرسانی قبلی (تعرفه‌های بیمهٔ چرخهٔ v2.4.5)، هنگام تبدیل ستون **«قیمت تعرفه»** از `StringFormat` به مبدل ارز ریالی، **کوتیشنِ آغازینِ مقدار صفت `Converter`** جا افتاده بود — XML فایل از همان نقطه نامعتبر می‌شد و کامپایلر XAML همان‌جا متوقف می‌شد.

### رفع
- `Converter="{StaticResource CurrencyRial}"` — مقدار داخل جفت‌کوتیشن کامل؛ همان مقصود کامیت اصلی و هم‌الگو با ۳ استفادهٔ دیگر همین مبدل در همان صفحه
- **بازرسی سراسری (قانون هم‌کلاسی‌ها):** هر ۸۲ فایل XAML پروژه با تجزیه‌گر XML — دقیقاً همان چیزی که کامپایلر می‌بیند — اعتبارسنجی شد؛ **تنها همین یک فایل** مشکل داشت
- **پیشگیری:** ابزار `scripts/validate_xaml.py` به پروژه اضافه شد — سلامت XML همهٔ XAMLها را یک‌جا می‌سنجد تا این‌گونه خطاهای بیلد‌شکننده دیگر تکرار نشود
- نسخه ۲.۴.۷ — پس از نصب، زیر دکمهٔ «خروج از حساب» در منوی کناری باید **۲.۴.۷** دیده شود

---
**نصب:** فقط ۴ فایل در زیپ است؛ با همان ساختار پوشه‌ای جایگزین کنید و سپس **Clean + Rebuild**. جزئیات در `Update-Guide-2.4.7.html` داخل بسته.
"""

# ── ۲) ساخت رلیز ──
payload = {
    "tag_name": TAG,
    "name": "v2.4.7 — وصلهٔ فوری: تعمیر خطای ساخت MC3000 در InsuranceView.xaml",
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

# ── ۳) بارگذاری زیپ — نکتهٔ مهم: آپلود دارایی‌های رلیز به uploads.github.com
# می‌رود نه api.github.com (وگرنه ۴۰۴ می‌گیرد) ──
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
