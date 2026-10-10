#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.6.1 در گیت‌هاب — تگ + یادداشت رلیز + بارگذاری زیپ آپدیت.

چرخهٔ چهل‌ودوم: رفع خطای خروج از حساب (Show روی پنجرهٔ بسته) + حذف Breadcrumb
از هدر + بذر سمت/دپارتمان درمانگاهی + رفع پاک‌شدن سمت/دپارتمان در ویرایش پرسنل
+ بازطراحی حرفه‌ای انتخابگر میانبرهای داشبورد → بیلد تمیز کامل سولوشن ۱۱ پروژه
بدون هیچ خطا و هشدار (warnaserror).
"""
import json
import pathlib
import subprocess
import urllib.request

TOKEN = "[REDACTED:github_token]"
REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
ZIP = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.6.0-to-2.6.1.zip")
TAG = "v2.6.1"

# ── ۱) تگ روی HEAD (کامیت نسخه 9cecdf2) و پوش آن ──
subprocess.run(f'git tag -f {TAG} main', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
subprocess.run(f'git push origin -f {TAG}', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True,
               capture_output=True, text=True)
print("✅ تگ", TAG, "پوش شد")

BODY = """## 🧰 v2.6.1 — چرخهٔ چهل‌ودوم: رفع خطای خروج از حساب + هدر تمیز + سمت/دپارتمان پرسنل + بازطراحی انتخابگر میانبرها

> چهار گزارش مالک با دو درس معماری مشترک: **«پنجرهٔ WPF بسته‌شده هرگز Show نمی‌شود»** (DI تک‌نمونه، رکوردِ مرده تحویل می‌داد) و **«فیلد بدون مقدار اولیهٔ نمایشی = پاک‌شدن بی‌صدا در ذخیرهٔ بعدی»**. هر دو با «شناختن مقدار فعلی و حفظ آن» رفع شدند نه با وصلهٔ محلی. بیلد کامل سولوشن ۱۱ پروژه **بدون هیچ خطا و هشدار** (با گسترش warnaserror) تأیید شد.

### 🚪 رفع خطای «خروج از حساب کاربری»
- **گزارش مالک:** خروج با خطای `InvalidOperationException: Cannot call Show… after a Window has closed` می‌افتاد
- **ریشه:** LoginWindow/MainWindow در DI تک‌نمونه بودند؛ LoginWindow پس از لاگین واقعاً Close می‌شود و «خروج از حساب» همان نمونهٔ بسته را Show می‌کرد — MainWindow هم در لاگین دوم همین وضعیت را داشت
- **رفع «نمونهٔ زنده یا نمونهٔ نو»:** DI پنجره‌ها Transient + متدهای کمکی `App.ShowLoginWindow/ShowMainWindow` (نخست پنجرهٔ زنده از Application.Current.Windows — تعویض نشست فقط فوکوس می‌گیرد — و فقط در نبودش نمونهٔ نو)؛ مهاجرت هر ۶ نقطهٔ ساخت پنجره — خروج/ورود هر تعداد بار، بدون خطا

### 🎨 هدر صفحات — وسط فقط برای نام صفحه و ساعت
- به درخواست مالک، مسیر (Breadcrumb «خانه › …») و آیکون خانه از نوار بالا حذف شد
- گروه میانه به لایهٔ همپوش `ColumnSpan=3 + Center` منتقل شد تا وسطِ واقعیِ پنجره تضمین شود حتی با عرض نابرابر ستون‌های کاربر/شعبه (تعمیق درس چرخهٔ ۳۸)

### 👔 سمت و دپارتمان پرسنل
- **فهرست‌های آمادهٔ درمانگاهی:** بذر ۳۰ سمت استاندارد (مدیر درمانگاه، پزشک متخصص، پرستار، منشی پذیرش، مسئول صندوق، داروساز و…) + ۱۷ دپارتمان (پذیرش، اورژانس، درمانگاه تخصصی، اتاق عمل، آزمایشگاه و…) — مهاجرت idempotent گاردشده؛ نام‌های موجود شما تکراری نمی‌شوند
- **رفع «ویرایش اصلاح نمی‌کند»:** در نبودِ مقدار در فهرست مرجع، هر ذخیرهٔ ویرایش سمت/دپارتمان را بی‌صدا پاک می‌کرد؛ اکنون مقدار فعلی در متن کامبو نمایش و هنگام ذخیره حفظ می‌شود (EmployeeDetailDto نام‌ها را هم برمی‌گرداند + زنجیرهٔ حل پنج‌مرحله‌ای: انتخاب فهرستی/تهی آگاهانه/حفظ فعلی/تطبیق کلایندی/ساخت سروری با پیام فارسی در شکست)

### ✨ بازطراحی حرفه‌ای فرم انتخاب میانبرهای داشبورد
- سربرگ گرادیانی برند + چیپ شمار صفحات؛ گروه‌بندی صفحات بر اساس ۱۰ دستهٔ منو با رنگ اختصاصی هر دسته روی لبهٔ کارت، دایرهٔ آیکون و سرصفحهٔ گروه
- **انتخاب چندگانه:** تیک چند کارت و افزودن همه با هم؛ جستجوی زنده روی عنوان و نام دسته با شمار نتیجه و حالت «یافت نشد»؛ دکمهٔ افزودن هوشمند

### 🧾 وضعیت ۱۱+۸ مورد جلسات قبل
- هر ۱۱ گزارش چرخهٔ ۳۸ در **v2.4.11** و هر ۸ خطای بیلد چرخهٔ ۴۰ در **v2.5.1** رفع و رلیز شده‌اند (ممیزی این چرخه حضور همه را در کد گواهی کرد) — اگر هنوز دیده می‌شوند، ایستگاه شما بیلد کهنه دارد؛ سنجهٔ نصب این بسته: «نسخهٔ ۲.۶.۱» زیر دکمهٔ خروج

### 📦 محتوا و نصب
**۱۵ فایل** — دلتا از v2.6.0. ⚠️ **هر دو پروژه (Desktop + API) تغییر دارند** + **یک مهاجرت دیتابیس** (بذر سمت/دپارتمان — فقط درج ردیف تازه، بدون تغییر دادهٔ موجود) در نخستین اجرای سرور خودکار اعمال می‌شود.
> پس از جایگزینی: **Clean + Rebuild** سولوشن + **publish/ری‌استارت سرور API**.
> 🔎 سنجهٔ نصب موفق: «نسخهٔ ۲.۶.۱» زیر خروج + خروج از حساب بدون خطا + فهرست پرِ سمت/دپارتمان در فرم پرسنل + ظاهر جدید انتخابگر میانبرها.
چک‌لیست کامل در `Update-Guide-2.6.1.html` داخل بسته.
"""

# ── ۲) ساخت رلیز ──
payload = {
    "tag_name": TAG,
    "name": "v2.6.1 — چرخهٔ چهل‌ودوم: رفع خطای خروج از حساب + هدر تمیز + سمت/دپارتمان پرسنل + بازطراحی انتخابگر میانبرها",
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
print("\n🎉 رلیز v2.6.1 کامل شد:", release["html_url"])
