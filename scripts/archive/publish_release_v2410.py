#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.4.10 در گیت‌هاب — تگ + یادداشت رلیز + بارگذاری زیپ تجمعی.

چرخهٔ سی‌وهفتم: ۸ گزارش مالک (منو/ساعت/کمبو/پیام‌ها/نسخه‌نویسی/تعرفه/
فوق‌تخصص/بندانگشتی) — بستهٔ تجمعی از v2.4.8 (شامل وصلهٔ 2.4.9).
"""
import json
import pathlib
import subprocess
import urllib.request

TOKEN = "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"
REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
ZIP = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.8-to-2.4.10.zip")
TAG = "v2.4.10"

# ── ۱) تگ سبک روی HEAD (کامیت نسخه) و پوش آن ──
subprocess.run(f'git tag -f {TAG} main', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
subprocess.run(f'git push origin -f {TAG}', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True,
               capture_output=True, text=True)
print("✅ تگ", TAG, "پوش شد")

BODY = """## 🚀 v2.4.10 — چرخهٔ سی‌وهفتم: ۸ اصلاح (منو/ساعت/کمبو/پیام‌ها/نسخه‌نویسی/تعرفه/فوق‌تخصص/بندانگشتی)

### 🧭 دو کشف کلیدی این چرخه
1. **ریشهٔ مشترک منو + ساعت:** استایل ضمنیِ سوییچ MaterialDesign3 همهٔ `ToggleButton`های بدون استایل را در قاب 52×32 زندانی می‌کرد — عنوان گروه‌های منوی کناری زیر خط می‌ماند (پنجمین گزارش) و ساعت «10:30» به‌صورت «10:3» با آیکون نصف‌بریده دیده می‌شد (چهارمین گزارش). رفع با استایل سپر خالی `App.PlainToggleButton` + ابعاد خودکار.
2. **ریشهٔ «همهٔ بیمه‌ها ۱۰۰٪ سهم بیمار شدن»:** در نبود ردیف اختصاصی برای بیمهٔ هدف، سهم‌ها از ردیف آزاد خوانده می‌شد که پس از قاعدهٔ درستِ چرخهٔ ۳۲ (آزاد = ۰/۱۰۰) برای هر بیمه‌ای بی‌تعرفهٔ اختصاصی ۱۰۰٪ بیمار می‌ساخت. اکنون سهم‌های ردیف «پایه» از **درصد پوشش خودِ بیمه** (الگوی `FractionmentCalculator`: `DefaultCoveragePercent` → `DefaultInsuranceShare` → ۷۰) محاسبه می‌شود — هم در لیست مدیریت تعرفه‌ها هم در پذیرش/صدور فاکتور.

### 🔧 ۸ اصلاح
- 🧭 **منوی کناری + ساعت‌های برنامهٔ کاری** — ریشهٔ مشترک قطعی (استایل سوییچ MD3)
- 🖨️ **کمبوی «نمایش قالب‌های»** — ۹ گزینهٔ سفیدِ بی‌متن؛ اتصال به فیلدِ تاپل که WPF نمی‌خواند → رکورد با پراپرتی واقعی
- 📢 **پیام‌های سرور لایسنس** — تاریخ شمسی به‌صورت بج در گوشهٔ بالا-چپ کادر + پس‌زمینهٔ روشن‌تر (حاشیه = رنگ انتخابی پنل)
- 💊 **نسخه‌نویسی** — بارگذاری تمپلیت اقلام را با نام/مصرف می‌آورد (`JsonPropertyName` صریح — حساسیت حروف camelCase/PascalCase) + پیش‌نمایش چاپ دیگر خالی نیست (بازسازی قالب رسمی نمایشگر WPF)
- 🩺 **فرم پزشک** — فوق‌تخصص دیگر هنگام ذخیره تهی نمی‌شود (Clear مجموعه با اتصال TwoWay مقدار واقعی را می‌کشت) + دکمهٔ «برنامهٔ کاری» فرم را مستقیم روی همان تب باز می‌کند
- 🧮 **تعرفه‌ها** — fallback سهم از تنظیمات بیمه (دو هندلر) + وسط‌چین سه ستون عددی لیست
- 🖼️ **بندانگشتی عکس** — لیست پزشکان و پرسنل تصویر دایره‌ای واقعیِ کلیک‌شونده دارند (بزرگ‌نمایی تمام‌صفحه)؛ بندانگشتی کم‌حافظه + کش ایستا + ابطال در همهٔ مسیرهای تعویض/حذف
- 🐞 **ترمیم‌های 2.4.9** داخل همین بسته (تجمعی)

### 📦 محتوا و نصب
**۲۴ فایل** — بستهٔ تجمعی از v2.4.8: اگر روی ۲.۴.۸ **یا** ۲.۴.۹ هستید، فقط همین یک بسته را نصب کنید.

> ⚠️ این چرخه هم دسکتاپ هم **سرور API** تغییر دارد (کوئری‌های تعرفه + سرویس لایسنس) — پس از کپی، Clean + Rebuild و سرور را هم publish/ری‌استارت کنید.

**چک‌لیست پس از نصب:**
1. زیر دکمهٔ «خروج از حساب»، «نسخه ۲.۴.۱۰» دیده شود
2. عنوان گروه‌های منوی کناری خوانا؛ ساعت 10:30 کامل با آیکون سالم
3. کمبوی «نمایش قالب‌های» نام فارسی گزینه‌ها را نشان می‌دهد
4. پیام‌های داشبورد: تاریخ شمسی در بج + کادر روشن
5. نسخه‌نویسی: تمپلیت با نام اقلام + پیش‌نمایش چاپ پر
6. تعرفه‌ها: هر بیمه درصد پوشش خودش را نشان می‌دهد (پیش‌فرض ۷۰/۳۰؛ آزاد ۰/۱۰۰) + ستون‌ها وسط‌چین
7. لیست پزشکان/پرسنل: بندانگشتی دایره‌ای، کلیک = بزرگ‌نمایی

راهنمای کامل فارسی داخل بسته: `Update-Guide-2.4.10.html`
"""

# ── ۲) ساخت رلیز ──
payload = {
    "tag_name": TAG,
    "name": "v2.4.10 — چرخهٔ سی‌وهفتم: منو/ساعت (ریشهٔ مشترک MD3) + تعرفه (fallback پوشش بیمه) + بندانگشتی عکس + ۵ اصلاح دیگر",
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
