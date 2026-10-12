#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.5.1 در گیت‌هاب — تگ + یادداشت رلیز + بارگذاری زیپ آپدیت.

چرخهٔ چهلم: وصلهٔ بیلد — ریشه‌یابی کامل خطاهای Rebuild مالک
(MC3074 xmlns بی‌winfx تور + MC3089 فرزند دوتایی ScrollViewer حسابداری و
Border داشبورد + CS0165 متغیر الگوی sc + CS8603 DateFa + سه خطای ماسک‌شده)
→ بیلد تمیز کامل سولوشن بدون هیچ خطا و هشدار.
"""
import json
import pathlib
import subprocess
import urllib.request

TOKEN = "[REDACTED:github_token]"
REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
ZIP = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.5.0-to-2.5.1.zip")
TAG = "v2.5.1"

# ── ۱) تگ روی HEAD (کامیت نسخه 6490e02) و پوش آن ──
subprocess.run(f'git tag -f {TAG} main', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
subprocess.run(f'git push origin -f {TAG}', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True,
               capture_output=True, text=True)
print("✅ تگ", TAG, "پوش شد")

BODY = """## 🩹 v2.5.1 — چرخهٔ چهلم: وصلهٔ بیلد (رفع کامل خطاهای Rebuild)

> گزارش مالک: شش خطای Rebuild + دو CS0006 آبشاری. **سولوشن کامل ۱۱ پروژه پس از این وصله در بیلد تمیز از صفر بدون هیچ خطا و هشداری تأیید شد.** بدون تغییر دیتابیس، بدون تغییر رفتار برنامه — فقط اصلاح سورس.

### 🔎 ریشه‌یابی‌ها (کشف‌های کلیدی)
- **خطاهای بیلد پیازی‌اند:** تا خطای XAML رفع نشود، کامپایلر به خطاهای کدِ پشت نمی‌رسد — فهرست اولیهٔ شما فقط لایهٔ بیرونی را نشان می‌داد؛ سه خطای ماسک‌شدهٔ دیگر نیز یافت و رفع شد
- **MC3074 تور آموزشی:** این فایلِ یکتا از زمان ساخت (چرخهٔ ۱۳) xmlns متریال‌دیزاین را **بدون «winfx»** داشت — تنها فایل پروژه در برابر ۸۰ فایلِ دارای winfx؛ کتابخانه فقط URI دارای winfx را به اسمبلی نگاشت می‌کند ⇒ PackIcon حل‌نشدنی بود
- **MC3089 حسابداری:** در تب «سود/زیان» بنر خطا و کارت صورت، دو فرزندِ مستقیم ScrollViewer بودند (ScrollViewer فقط یک فرزند می‌پذیرد)
- **MC3089 داشبورد (پنهان):** در کارت پیام‌های پشتیبانی، سربرگ و متن دو فرزندِ مستقیم Border بودند — در فهرست شما نبود و فقط در بازتولید بیلد آشکار شد
- **CS0165 لاگ API:** متغیر الگوی `is { } sc` فقط درون شاخهٔ true قطعی است؛ تحلیل جریان کامپایلر بازگشتِ زودهنگامِ null را به آن وصل نمی‌کند
- **CS8603 هشدار DTO لایسنس:** بازگشتِ پراپرتی رکوردِ دارای پارامتر اختیاری تهی، «شاید تهی» شمرده می‌شد؛ متن Trim شدهٔ قطعاً ناتهی بازگردانده می‌شود (محتوا یکسان)
- **CS0006 ×۲:** دنبالهٔ شکست بیلد Desktop/API — خودبه‌خود رفع شد
- **ماسک‌شده‌ها:** نبود `using System.Windows.Media` در کد پشت تور (CS0246/CS0103) + cast نادرست TranslatePoint (CS1503) + بازوی غیرقابل‌دسترس switch در مبدل EmptyListToVisibility (CS8510)

### 📦 محتوا و نصب
**۹ فایل** — دلتا از v2.5.0 (اگر قدیمی‌تر هستید، نخست بستهٔ 2.4.11-to-2.5.0 را نصب کنید).

> ⚠️ پس از جایگزینی فایل‌ها: **Clean + Rebuild** کامل سولوشن؛ اگر سرور API جداگانه publish می‌شود، بازنشر و ری‌استارت آن هم لازم است (تغییر API جزئی اما الزامی).
> 🔎 سنجهٔ نصب موفق: «نسخهٔ ۲.۵.۱» زیر دکمهٔ خروج منوی کناری + Error List کاملاً خالی (خطا و هشدار).

چک‌لیست پس از نصب در `Update-Guide-2.5.1.html` داخل بسته.
"""

# ── ۲) ساخت رلیز ──
payload = {
    "tag_name": TAG,
    "name": "v2.5.1 — چرخهٔ چهلم: وصلهٔ بیلد — ریشه‌یابی کامل خطاهای Rebuild (MC3074/MC3089/CS0165/CS8603 + سه خطای ماسک‌شده)",
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

# ── ۳) بارگذاری زیپ ──
upload_url = release["upload_url"].replace("{?name,label}", f"?name={ZIP.name}")
req = urllib.request.Request(
    upload_url,
    data=ZIP.read_bytes(),
    headers={"Authorization": f"token {TOKEN}", "Accept": "application/vnd.github+json",
             "Content-Type": "application/zip"},
    method="POST")
with urllib.request.urlopen(req) as r:
    asset = json.load(r)
print("✅ زیپ بارگذاری شد:", asset["name"], "|", asset["size"] // 1024, "KB")
print("\n🎉 رلیز v2.5.1 کامل شد:", release["html_url"])
