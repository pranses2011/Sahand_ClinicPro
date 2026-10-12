#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.2.0 در گیت‌هاب — تگ + Release + آپلود بستهٔ ZIP.
نکتهٔ حیاتی (چرخه ۲۲): آپلود asset باید به uploads.github.com برود،
نه api.github.com (وگرنه 404 می‌دهد).
"""
import json
import pathlib
import urllib.request

TOKEN = "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"
REPO = "pranses2011/Sahand_ClinicPro"
TAG = "v2.2.0"
ASSET = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.1.0-to-2.2.0.zip")

BODY = """## 📦 نسخهٔ ۲.۲.۰ — چرخه بیست‌وچهارم (گزارش هفدهم مالک — ۴۵+ مورد)

### 🛠️ رفع‌های مهم (ریشه‌یابی و تعمیر قطعی)
- 🛡️ **آبشار خطای سرور «StatusCode cannot be set because the response has already started»** — گارد پاسخ آغازشده در میدلور خطا + چالش ۴۰۱ JWT با پاسخ فارسی
- 🐛 **کرش گزارش‌ها** ('' is not a valid FontFamily) + **کرش تصویربرداری/دندانپزشکی** (Binding روی Value دتاتریگر) + **پروندهٔ بیمار** («پاسخ سرور قابل تفسیر نبود» — هم‌نام‌سازی DTO)
- 🕐 **انتخابگر ساعت وارونه** — ردیف دیجیتال و بوم عقربه‌ای در RTL آینه می‌شدند؛ LTR شدند + رفع نمایش ناقص پس از تأیید
- 🖨️ **چاپ نسخه: زوم بی‌اثر + ستون ردیف تمام‌عرض** — زوم LayoutTransformی + عرض ثابت ستون‌های جدول RTL
- 📋 **لاگ عملیات خالی** — پاسخ تک‌لایه + DTO غنی‌تر (IP/مقادیر) + فیلتر عملیات مستقل
- 👤 **نام کامل کاربر + ذخیرهٔ نقش‌ها** — فیلد نام کامل با اتصال خودکار به پرسنل + ریشهٔ ذخیره‌نشدن نقش‌ها (تداخل کلید مرکب UserRole → همگام‌سازی تفاوتی)
- 🔢 **ارقام فارسی بدون استثنا** — زبان fa-IR روی همهٔ پنجره‌ها (رندر موتور WPF) + پارسر ارقام فارسی
- 💰 **تعمیر قطعی جداکنندهٔ ۳ رقمی زنده** — ریشه: SetValue اتصال داطمی را حذف می‌کرد → SetCurrentValue + حفظ مکان‌نما + وصل به حق ویزیت پزشک
- 🎨 **فونت وزیرمتن** — کارخانهٔ FontAssets با سازندهٔ رسمی دوآرگومانی + برچسب متمایز از فونت‌های ویندوز
- 🖥 **اندازهٔ فونت کل برنامه** — زوم پیوسته روی محتوای پنجرهٔ اصلی
- 💲 **~۳۰ عنوان هاردکد «(ریال)»** → برچسب پویای واحد پول
- 💾 **«Cannot open backup device»** — استخراج ZIP در پوشهٔ پشتیبان خودِ SQL Server (دسترسی حساب سرویس)
- 🩸 فیلد گروه خونی از ستون فاصله‌انداز ۱۲px به ستون اصلی + عرض ستون‌های ساعت برنامهٔ کاری

### ✨ قابلیت‌های جدید
- 💉 **پذیرش خدمت درمانی** — تزریقات/پانسمان/آزمایش/تصویربرداری بدون پزشک از تعرفه‌ها + انجام‌دهندهٔ پرسنل + فاکتور خودکار
- 💲 **تعرفه به‌تفکیک بیمه + تعرفهٔ آزاد** — انتخابگر هدف در تب تعرفه‌ها + اعمال خودکار قیمت بیمه در فاکتور
- 👔 **سمت/دپارتمان پرسنل** (قابل تایپ مقدار جدید + ساخت خودکار) + کد پرسنلی/تاریخ استخدام/تحصیلات/تماس اضطراری
- 🖨 **چاپگر نوبت‌دهی + قبض فیش حرارتی ۸۰ میلی‌متری** با چاپ خودکار پس از ثبت نوبت + فهرست چاپگرهای نصب‌شده در ComboBoxها
- 🎨 **شخصی‌سازی حرفه‌ای تم** — رنگ اصلی سفارشی + حالت آیکون‌های رنگی منو (بدون ری‌استارت) + **پریست‌های شخصی** (ذخیره/اعمال/حذف)
- ✨ **لوگوی رسمی بزرگ ClinicPro.png** در سایدبار/لاگین + **Splash انیمیشنی شفاف** با نوار پیشرفت درصد فارسی
- ⌨️ **ESC سراسری** برای بستن همهٔ فرم‌ها/دیالوگ‌ها
- 💾 **انتخاب فرمت پشتیبان** (ZIP/BAK) + تشخیص خودکار در بازیابی + **حذف دستی فایل‌های قدیمی**
- 🖨 **۱۱ قالب چاپ پیش‌فرض جدید** (طرح مدرن برای هر ۹ نوع سند + فیش حرارتی نوبت/رسید) + **۸ اندازهٔ کاغذ جدید** (حرارتی ۸۰/۵۸ میلی‌متر + سفارشی) + **برچسب فارسی متغیرها** با آیکون 🧩
- 🏛 **اطلاعات درمانگاه از سرور لایسنس** — همگام‌سازی خودکار با هر بررسی + بخش فقط‌خواندنی؛ تغییر تنها از پنل لایسنس
- 💊 **۲۰ داروی جدید** (اولانزاپین/آریپیپرازول/کوئتیاپین/لوتیراستام/پرگابالین/سیتاگلیپتین/امپاگلیفلوزین/لیراگلوتاید/آمیودارون و…) + **۵۰ تداخل بالینی جدید** (۸ گروه شامل گل راعی و مکمل‌های گیاهی)
- 📄 **سند بررسی تفاوت فرم‌ها با اسکرین‌شات‌ها** — docs/forms-vs-screenshots-report.md
- 🏠 بازگرداندن منوی «مدیریت سامانه (تب‌بندی کامل)»

### ⚠️ نکتهٔ نصب
شامل مهاجرت `Cycle24ReceptionServices` (idempotent) است که با اجرای سرور API خودکار اعمال می‌شود. سرور لایسنس PHP را هم به‌روز کنید (ستون clinic_phone خودکار ساخته می‌شود). راهنمای کامل فارسی داخل ZIP است.

**بیلد:** ۰ خطا / ۰ هشدار — تست‌های قابل‌اجرا (دامنه + کاربرد): ۱۴۲ پاس
"""

HEADERS = {
    "Authorization": f"token {TOKEN}",
    "Accept": "application/vnd.github+json",
    "User-Agent": "sahand-release-script",
}


def api(url, data=None, method="GET", raw=None, extra_headers=None):
    headers = dict(HEADERS)
    if extra_headers:
        headers.update(extra_headers)
    req = urllib.request.Request(url, data=(raw if raw is not None else
                                            (json.dumps(data).encode() if data else None)),
                                 headers=headers, method=method)
    with urllib.request.urlopen(req) as resp:
        body = resp.read()
        return json.loads(body) if body else {}


def main():
    # ۱) ساخت تگ روی HEAD فعلی
    import subprocess
    subprocess.run(["git", "tag", "-f", TAG], cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
    subprocess.run(["git", "push", "-f", "origin", TAG], cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
    print("✓ تگ v2.2.0 منتشر شد")

    # ۲) ساخت رلیز
    rel = api(
        f"https://api.github.com/repos/{REPO}/releases",
        data={"tag_name": TAG, "name": f"نسخهٔ ۲.۲.۰ — چرخه بیست‌وچهارم (گزارش هفدهم مالک)",
              "body": BODY, "draft": False, "prerelease": False},
        method="POST")
    print(f"✓ رلیز ساخته شد: {rel['html_url']}")

    # ۳) آپلود بستهٔ ZIP — به uploads.github.com (نه api!)
    upload = api(
        f"https://uploads.github.com/repos/{REPO}/releases/{rel['id']}/assets?name={ASSET.name}",
        raw=ASSET.read_bytes(), method="POST",
        extra_headers={"Content-Type": "application/zip"})
    print(f"✓ بستهٔ ZIP آپلود شد: {upload['browser_download_url']}")


if __name__ == "__main__":
    main()
