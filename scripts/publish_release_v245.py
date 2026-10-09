#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.4.5 در گیت‌هاب — تگ + یادداشت رلیز + بارگذاری زیپ آپدیت."""
import json
import pathlib
import subprocess
import urllib.request

TOKEN = "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"
REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
ZIP = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.4-to-2.4.5.zip")
TAG = "v2.4.5"

# ── ۱) ساخت تگ سبک روی آخرین کامیت main و پوش آن ──
subprocess.run(f'git tag -f {TAG} main', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
subprocess.run(f'git push origin -f {TAG}', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True,
               capture_output=True, text=True)
print("✅ تگ", TAG, "پوش شد")

BODY = """## چرخهٔ سی‌ودوم — گزارش ششم مالک (v2.4.5)

شش ریشه‌یابی کامل + دو کشف پنهان هم‌خانواده:

### 💰 بحرانی — صدور فاکتور («پذیرش ثبت شد و صدور فاکتور شکست خورد»)
ستون‌های مبلغ ریالی سهم بیمه/بیمار اقلام فاکتور با دقت (5,2) یعنی سقف ۹۹۹٫۹۹ ریال بود → Arithmetic Overflow روی هر فاکتور واقعی → ۵۰۰. اکنون (18,2) + مهاجرت خودکار. **این همان ریشهٔ پنهان «هیچ دادهٔ مالی ذخیره نمی‌شود» چرخهٔ قبل بود.**

### 🆓 بیمهٔ آزاد — ۱۰۰٪ سهم بیمار
بذر قدیمی درصد بیمه (۷۰/۳۰) روی ردیف‌های آزاد می‌ذاشت؛ حالا ۰/۱۰۰ در بذر + مهاجرت + دفاع در عمق در ۴ نقطهٔ سرور + در پذیرش آزاد ستون سهم بیمه حذف و «قابل پرداخت بیمار» برجسته می‌شود. هدرهای لیست تعرفه «٪» شد و ۱۳ فونت هاردکد Consolas اصلاح شد.

### 🖨️ قالب‌های چاپ — سرانجام دیده می‌شوند
ناهمخوانی عدد/رشته: سرور enum را عدد می‌فرستاد، DTO دسکتاپ string داشت → JsonException → لیست همیشه خالی؛ ذخیره هم ۴۰۰ می‌گرفت. قرارداد رشته‌ای شد.

### 🔑 لایسنس — تپ قلب با اثر انگشت خالی رد می‌شد
`config ?? ...` با رشتهٔ خالی config هرگز fallback نمی‌رسید → سرور fingerprint_mismatch → ماژول‌ها/اطلاعات درمانگاه/پیام‌ها هرگز تازه نمی‌شدند. حل سه‌مرحله‌ای + اندپوینت `license/refresh` + تازه‌سازی با ورود به تنظیمات + «بررسی آنلاین» واقعی.

### 📢 پیام‌های پشتیبانی + 📊 کشف: کل داشبورد خالی بود!
پاکت ApiResponse در کارت پیام‌ها و هر پنج بخش داشبورد (آمار/هشدار/نمودار درآمد/نوبت‌ها/فعالیت‌ها) باز نمی‌شد — همه اکنون واقعاً پر می‌شوند.

### 📝 عنوان شناور همهٔ فیلدها
مانند کادر کد ملی: عنوان هر فیلد هنگام فوکوس/پر شدن به بالای کادر می‌رود — `HintAssist.IsFloating` ارث‌بری روی استایل Window، همهٔ فرم‌ها/پنجره‌ها.

---
**نصب:** بستهٔ زیپ را باز کنید و فایل‌ها را با همان ساختار پوشه‌ای جایگزین کنید، سپس Solution را بیلد و سرور API را اجرا کنید (مهاجرت دیتابیس خودکار اعمال می‌شود). جزئیات کامل در `Update-Guide-2.4.5.html` داخل بسته.
"""

# ── ۲) ساخت رلیز ──
payload = {
    "tag_name": TAG,
    "name": "v2.4.5 — چرخهٔ سی‌ودوم: فاکتور/تعرفهٔ آزاد/قالب چاپ/لایسنس/پیام‌ها و داشبورد/عنوان شناور",
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
