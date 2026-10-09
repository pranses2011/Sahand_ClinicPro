#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.4.6 در گیت‌هاب — تگ + یادداشت رلیز + بارگذاری زیپ آپدیت."""
import json
import pathlib
import subprocess
import urllib.request

TOKEN = "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"
REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
ZIP = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.5-to-2.4.6.zip")
TAG = "v2.4.6"

# ── ۱) ساخت تگ سبک روی آخرین کامیت main و پوش آن ──
subprocess.run(f'git tag -f {TAG} main', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
subprocess.run(f'git push origin -f {TAG}', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True,
               capture_output=True, text=True)
print("✅ تگ", TAG, "پوش شد")

BODY = """## چرخهٔ سی‌وسوم — گزارش هفتم مالک (v2.4.6)

شش اصلاح رابط کاربری با ریشه‌یابی کامل؛ چهار مورد سومین/دومین گزارشِ همان علامت بودند و این بار ریشهٔ ساختاری/رندری واقعی هرکدام پیدا و قطعی شد:

### 🎨 اسپلش — بزرگ‌تر + زمینهٔ کاملاً شفاف
عامل ظاهر «خاکستری» ظرف نیمه‌مات تیرهٔ تمام‌عیار (#F2091A33) دور محتوا بود؛ حذف شد. پنجره ۴۸۰×۴۲۰ → ۶۴۰×۵۶۰، لوگو ۱۹۰ → ۲۸۰px، خوانایی با سایهٔ نرم روی متن‌ها.

### 🦷 راهنمای رنگ چارت دندان — چیپ رنگی واقعی
ایموجی‌های ⬜🟥🟨🟩🟦⬛🟪 در فونت فارسی وزیرمتن گلیف ندارند و مربع تهی/نمای جایگزین می‌افتادند («راهنما مشخص نبود») — چیپ‌های Border با همان کدهای رنگ ToothVM.Color + برچسب + تولتیپ؛ راهنمای نمرهٔ درد فیزیوتراپی هم همین الگو شد (قانون هم‌کلاسی‌ها).

### ⏰ انتخابگر ساعت — بستن تضمینی با تأیید (دومین گزارش)
`ClosePopupForcibly` با دو مسیر مستقل (IsChecked تاگل + IsOpen=false صریح) از هر سه دکمه + گارد `_popupWasOpenOnMouseDown` (الگوی اثبات‌شدهٔ PersianDatePicker) که فشردن تاگلِ باز را از بازشدن‌مجدد بی‌درنگ بازمی‌دارد + حذف `PopupAnimation=Fade` (عامل پاپ‌آپ گیرکرده) از هر دو انتخابگر.

### 📅 برنامهٔ کاری — ساعت‌ها کامل دیده شوند (سومین گزارش)
سه لایه: تاگل انتخابگر صریحاً Stretch و محتوا مرکزچین و حذف MinWidth غیرواقعی ۵۶px + عرض صریح ۱۵۰px روی هر ۴ انتخابگر ستون‌های شروع/پایان/استراحت + `HorizontalContentAlignment=Stretch` روی استایل ضمنی DataGridCell (همهٔ جدول‌های برنامه).

### 🧭 منوی کناری — عنوان گروه‌ها (سومین گزارش، مهم)
بازطراحی ساختاری: StackPanel افقی → گرید چه‌ارستونه [نوار][آیکون][**عنوان ***][فلش] — ستون عنوان `*` یعنی فضای تضمینی؛ «فقط آیکون دیده شدن» ساختاراً ناممکن می‌شود. سربرگ تمام‌عرض + زیرمنوها تورفتگی راست ۲۰px + آیتم تکی/زیرمنو هم گرید شدند (عنوان‌های بلند دیگر بیرون نمی‌زنند). **نشان نسخهٔ درون منو زیر دکمهٔ خروج اضافه شد** — چون این مورد دو بار «رفع» شده ولی باز گزارش شده بود؛ اگر پس از آپدیت عدد تازه نبود، بیلد ایستگاه کهنه است.

### 🖨️ چاپ نسخه — زوم کار کند + دکمه‌های +/− نمایان
قالب مینیمال، ScrollViewer قالب رسمی را حذف کرده بود → صفحهٔ زوم‌شده از لبه‌ها بیرون می‌زد بی‌آنکه بتوان لبه‌ها را دید؛ چیدمان رسمی بازگشت. دکمه‌های متنی «+»/«−» (نامرئی در فونت فارسی) → PackIcon وکتوری؛ خوشهٔ زوم LTR با ترتیب استاندارد [−] [٪] [+].

---
**نصب:** فایل‌های زیپ را با همان ساختار پوشه‌ای جایگزین کنید، سپس در ویژوال استودیو **Clean Solution و Rebuild** (یا `Clean-VS-Cache.bat`) — و مطمئن شوید زیر دکمهٔ خروج منو، نسخهٔ **۲.۴.۶** نمایان است. جزئیات کامل در `Update-Guide-2.4.6.html` داخل بسته.
"""

# ── ۲) ساخت رلیز ──
payload = {
    "tag_name": TAG,
    "name": "v2.4.6 — چرخهٔ سی‌وسوم: اسپلش شفاف/چارت دندان/انتخابگر ساعت/برنامهٔ کاری/منوی کناری/چاپ نسخه",
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
