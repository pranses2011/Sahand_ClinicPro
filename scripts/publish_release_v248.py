#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.4.8 در گیت‌هاب — تگ + یادداشت رلیز + بارگذاری زیپ تجمعی."""
import json
import pathlib
import subprocess
import urllib.request

TOKEN = "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"
REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
ZIP = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-Cumulative-to-2.4.8.zip")
TAG = "v2.4.8"

# ── ۱) تگ سبک روی HEAD و پوش آن ──
subprocess.run(f'git tag -f {TAG} main', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
subprocess.run(f'git push origin -f {TAG}', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True,
               capture_output=True, text=True)
print("✅ تگ", TAG, "پوش شد")

BODY = """## 🚀 v2.4.8 — چرخهٔ سی‌وپنجم: ۷ اصلاح + بستهٔ تجمعی

### ⭐ مهم‌ترین تغییر این رلیز: بستهٔ «تجمعی»
کشف کلیدی این چرخه با تحلیل پیکسلی اسکرین‌شات شما: **فایل‌های رلیزهای ۲.۴.۵ و ۲.۴.۶ هرگز به ایستگاه شما نرسیده بود** (زیپ ۲.۴.۵ فایل `MainWindow.xaml` را نداشت و زیپ ۲.۴.۶ هم بعد از خطای ساخت MC3000 برگرانده شد) — برای همین منوی کناری و ساعت‌ها هرچقدر اصلاح می‌شد، شما هنوز حالت قدیمی را می‌دیدید!
این بسته **هر ۴۴ فایلِ تغییرکرده از ۲.۴.۴ تا امروز** را دارد؛ بعد از نصب، ایستگاه شما دقیقاً آخرین نسخهٔ کامل می‌شود.

### 🖨️ طراح قالب چاپ — فیلتر موجودیت + ۲ طرح تازه برای هر موجودیت
- انتخابگر «نمایش قالب‌های» بالای فهرست — فقط قالب‌های موجودیتِ منتخب دیده می‌شوند
- هر ۹ موجودیت اکنون **۵ طرح** دارد: استاندارد / مدرن / کلاسیک / **مینیمال** (سفید پاک) / **فشرده** (کاغذ A6) — ۱۸ قالب جدید با اولین اجرای API خودکار ثبت می‌شوند

### 📋 لاگ عملیات — مقدار قبلی برای مسیرهای زیرمنبعی
ویرایش‌هایی مثل برنامهٔ کاری پزشک (`PUT /doctors/5/schedule`) قطعهٔ آخر مسیرشان غیرعددی است و هرگز مقدار قبلی ثبت نمی‌کردند — اکنون شناسه از آخرین قطعهٔ عددی مسیر خوانده می‌شود.

### 📢 پیام‌های سرور لایسنس — تاریخ شمسی زیر عنوان + رنگ پنل شما
- تاریخ هر پیام **شمسی/ارقام‌فارسی** و در **ردیفی مستقل زیر عنوان** است (قبلا میلادی خام کنار عنوان بود)
- رنگ کادر = همان رنگی که در پنل لایسنس برای پیام انتخاب کرده‌اید (حاشیه توپر + پس‌زمینه ملایم همان رنگ)؛ برای پیام‌های قدیمیِ بی‌رنگ، رنگ سطح اهمیت

### 🖨️ چاپ نسخه — پیشنمایش خالی رفع شد
ریشه از سورس رسمی WPF پیدا شد: قالب رسمی `FlowDocumentPageViewer` صفحه را داخل `AdornerDecorator` می‌گذارد و `DocumentPageView` باید `PageNumber=0` + `IsMasterPage=True` بگیرد — هیچ‌کدام در قالب‌های سفارشی قبلی نبود. قالب فعلی رونوشت رسمی + پیمایش زوم است.

### 🧭 منوی کناری + 📅 ساعت‌های برنامهٔ کاری (دو مورد تکراری)
اصلاحات چرخهٔ ۳۳ در ریپو درست بودند اما به ایستگاه شما نرسیده بودند — با بستهٔ تجمعی می‌رسند + دو سپر دفاعی تازه: کشش صریح `TemplateBinding` و عرض‌حداقل ۱۹۰px (دکمه‌ها هرگز به فقط‌آیکونیِ ۵۵px فرو نمی‌ریزند).

### 🎨 راهنماهای رنگی — چیپ واقعی در سراسر برنامه
راهنمای اسلات‌های نوبت‌دهی (آخرین راهنمای متنیِ رنگی باقی‌مانده) با همان الگوی چیپی دندانپزشکی بازنویسی شد.

---
**نصب:** همهٔ فایل‌ها را با همان ساختار پوشه‌ای جایگزین کنید و **Clean + Rebuild** بزنید. پس از اجرا زیر دکمهٔ «خروج از حساب» باید **نسخه ۲.۴.۸** دیده شود. چک‌لیست کامل ۷ موردی در `Update-Guide-2.4.8.html` داخل بسته.
"""

# ── ۲) ساخت رلیز ──
payload = {
    "tag_name": TAG,
    "name": "v2.4.8 — چرخهٔ سی‌وپنجم: طراح قالب/لاگ/پیام‌ها/چاپ/منو/ساعت/راهنما + بستهٔ تجمعی",
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
