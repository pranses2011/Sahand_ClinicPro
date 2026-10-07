#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📦 ساخت بستهٔ آپدیت v2.0.4 → v2.0.5 — چرخه هجدهم (گزارش دوازدهم مالک)
فقط فایل‌های تغییرکردهٔ همین رلیز (قاعدهٔ دائمی توزیع از P9/P10)، با همان
ساختار پوشه‌ای مخزن، به‌همراه راهنمای فارسی HTML و مانیفست متنی فایل‌ها.

رفع‌های این بسته (هر ۹ مورد گزارش دوازدهم):
  ۱) تور آموزشی — هایلایت دقیق (رفع DPI + نام‌گذاری TopBar/PageHost + ناوبری به صفحه)
  ۲) خطای «نشانی پنل لایسنس تنظیم نشده است — License:PortalUrl» — پیش‌فرض رسمی در کد
  ۳+۷) سرور لایسنس پیش‌فرض https://clinic-license.ea-fixer.ir + قابل ویرایش از صفحهٔ فعال‌سازی
  ۴) دریافت فایل درخواست فعال‌سازی آفلاین (.req)
  ۵) اطلاعات پشتیبانی پیش‌فرض + تازه‌سازی از سرور لایسنس
  ۶) چیپس ماژول‌ها با نام + رنگ سبز در حالت فعال
  ۸) ساعت/تاریخ داشبورد و نوار بالا با ارقام فارسی (+ موارد مشابه)
  ۹) عکس پرسنل و دکترها — انتخاب/نمایش/بزرگ‌نمایی + مسیر در دیتابیس + فایل در Photos/Employees

نحوهٔ استفاده:
    python scripts/create_update_zip_v205.py [مسیر-خروجی.zip]

پیش‌فرض خروجی: ریشهٔ مخزن / SahandClinicPro-Update-2.0.4-to-2.0.5.zip

ایمنی‌ها:
- توقف اگر فایل حذف‌شده (D) در محدوده باشد
- توقف اگر appsettings در فهرست باشد (تنظیم SQL مالک هرگز بازنویسی نشود)
- گارد انسجام مثبت: نشانه‌های سلامت رفع‌های همین نسخه باید در HEAD موجود باشند
- گارد انسجام منفی: پیام خطای «نشانی پنل لایسنس تنظیم نشده» باید از LicenseService
  «حذف شده» باشد (رفع خطای PortalUrl)
⚠️ درس چرخه ۱۵: HTML حاوی آکولاد است — بدون f-string، فقط الحاق ساده و .replace()
"""
import pathlib
import subprocess
import sys
import zipfile

REPO = pathlib.Path(__file__).resolve().parents[1]
TAG = "v2.0.4"
OUT_NAME = "SahandClinicPro-Update-2.0.4-to-2.0.5.zip"
GUIDE_NAME = "Update-Guide-2.0.5.html"
MANIFEST_NAME = "changed-files.txt"

# ─── گارد انسجام مثبت — نشانه‌های سلامت که باید در محتوای فعلی HEAD موجود باشند ───
COHERENCE_CHECKS = [
    # ۱) تور آموزشی
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'x:Name="TopBar"', "نام‌گذاری نوار بالا برای هایلایت تور"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'x:Name="PageHost"', "نام‌گذاری میزکار صفحات برای هایلایت تور"),
    ("src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml.cs",
     "TransformFromDevice", "رفع DPI هایلایت تور"),
    ("src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml.cs",
     "ApplyCenteredMask", "سوراخ مرکزی دور حباب خوش‌آمد"),
    # ۲+۳+۷) لایسنس — پیش‌فرض و تنظیم‌پذیری نشانی
    ("src/Sahand.ClinicPro.Application/Common/Helpers/LicensePortalHelper.cs",
     "clinic-license.ea-fixer.ir", "نشانی رسمی سرور لایسنس"),
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "ResolvePortalUrlAsync", "زنجیرهٔ حل نشانی سرور لایسنس"),
    ("src/Sahand.ClinicPro.API/Controllers/LicenseController.cs",
     "activation-request", "endpoint فایل .req"),
    ("src/Sahand.ClinicPro.API/Controllers/LicenseController.cs",
     "SaveLicenseSettingsRequest", "ذخیرهٔ نشانی سرور لایسنس"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/LicenseStatusViewModel.cs",
     "DownloadRequestFileAsync", "دکمهٔ دریافت .req"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/LicenseStatusViewModel.cs",
     "SavePortalUrlAsync", "ذخیرهٔ نشانی سرور لایسنس در UI"),
    ("src/Sahand.ClinicPro.Desktop/Views/LicenseStatusView.xaml",
     'Command="{Binding DownloadRequestFileCommand}"', "دکمهٔ .req در XAML"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/LicenseStatusViewModel.cs",
     "ChipBackground", "چیپس ماژول با رنگ وضعیت"),
    # ۵) پشتیبانی
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "DefaultSupportInfo", "پیش‌فرض اطلاعات پشتیبانی"),
    ("src/LicenseServer.Php/api.php",
     "$action === 'support'", "اکشن عمومی support پنل PHP"),
    # ۸) ارقام فارسی
    ("src/Sahand.ClinicPro.Application/Common/Helpers/PersianDateHelper.cs",
     "ToShamsiFa", "تاریخ شمسی با ارقام فارسی"),
    ("src/Sahand.ClinicPro.Application/Common/Helpers/PersianDateHelper.cs",
     "ToClockFa", "ساعت با ارقام فارسی"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     "ToClockFa(DateTime.Now)", "ساعت نوار بالا فارسی‌رقم"),
    # ۹) عکس‌ها
    ("src/Sahand.ClinicPro.API/Controllers/PeoplePhotosController.cs",
     "Photos/Employees", "ذخیرهٔ فایل عکس در پوشهٔ برنامه"),
    ("src/Sahand.ClinicPro.API/Controllers/PeoplePhotosController.cs",
     "ProfilePicture", "ذخیرهٔ مسیر عکس در دیتابیس"),
    ("src/Sahand.ClinicPro.Desktop/Views/PhotoViewerWindow.xaml.cs",
     "PhotoViewerWindow", "نمایشگر بزرگ عکس"),
    ("src/Sahand.ClinicPro.Desktop/Services/PhotoTools.cs",
     "UploadEmployeePhotoAsync", "آپلود عکس پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/Views/AdminView.xaml",
     "ViewPhotoCommand", "ستون عکس فهرست پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorsView.xaml",
     "ViewDoctorPhotoCommand", "ستون عکس فهرست پزشکان"),
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorFormWindow.xaml",
     "ChangePhotoCommand", "کارت عکس فرم پزشک"),
    ("src/Sahand.ClinicPro.Desktop/Views/EmployeeFormWindow.xaml",
     "PickPhotoCommand", "کارت عکس فرم پرسنل"),
    # نسخه
    ("Directory.Build.props",
     "<Version>2.0.5</Version>", "نسخهٔ 2.0.5"),
]

# ─── گارد انسجام منفی — الگوی واقعی خطای رفع‌شده باید «نباشد» ───
# (کامنت‌های مستندات که رفع را شرح می‌دهند مجازند — نشانگر با پسوند تنظیم کی است)
ABSENCE_CHECKS = [
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "تنظیم نشده است — License:PortalUrl",
     "مسیر خطای تنظیم‌نشدن PortalUrl باید حذف شده باشد"),
]


def changed_files():
    """فهرست (وضعیت، مسیر) همهٔ فایل‌های تغییرکرده از تگ تا HEAD"""
    out = subprocess.run(
        ["git", "diff", "--name-status", TAG + "..HEAD"],
        cwd=REPO, capture_output=True, text=True, check=True,
    ).stdout
    files = []
    for line in out.splitlines():
        line = line.rstrip("\n")
        if not line.strip():
            continue
        parts = line.split("\t")
        status, path = parts[0], parts[-1]
        files.append((status, path))
    return files


def group_title(path: str) -> str:
    """گروه‌بندی فارسی برای نمایش در راهنما"""
    p = path.replace("\\", "/")
    if p.startswith("src/Sahand.ClinicPro.Desktop/Views/"):
        return "پنجره‌ها و نماها (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/Controls/"):
        return "کنترل‌های مشترک (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/ViewModels/"):
        return "ViewModel ها (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/Services/"):
        return "سرویس‌ها (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/"):
        return "هستهٔ برنامهٔ دسکتاپ"
    if p.startswith("src/Sahand.ClinicPro.API/"):
        return "لایهٔ API (سرور محلی)"
    if p.startswith("src/Sahand.ClinicPro.Application/"):
        return "لایهٔ کاربرد (Application)"
    if p.startswith("src/Sahand.ClinicPro.Infrastructure/"):
        return "لایهٔ زیرساخت (Infrastructure)"
    if p.startswith("src/LicenseServer.Php/"):
        return "سرور لایسنس PHP"
    if p.startswith("tests/"):
        return "پروژه‌های تست"
    if p.startswith("docs/"):
        return "مستندات و راهنماها"
    return "فایل‌های ریشهٔ مخزن"


# پالت آبی — هماهنگ با تم محصول
CSS = """
:root{--ink:#1c2b3a;--mut:#5a6b7d;--line:#dfe7ee;--brand:#1565C0;--brand2:#0D47A1;
--bg:#f4f7fb;--card:#ffffff;--ok:#2E7D32;--warn:#9a6700;--bad:#b3372f}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--ink);
font-family:"Vazirmatn","Segoe UI",Tahoma,Arial,sans-serif;line-height:1.9;font-size:15px}
.wrap{max-width:880px;margin:0 auto;padding:24px 20px 64px}
header.hero{background:linear-gradient(135deg,var(--brand) 0%,var(--brand2) 100%);
color:#fff;border-radius:16px;padding:28px 26px;margin-bottom:26px}
header.hero h1{margin:0 0 6px;font-size:24px}
header.hero p{margin:0;opacity:.92;font-size:14px}
.badge{display:inline-block;background:rgba(255,255,255,.16);border:1px solid rgba(255,255,255,.35);
border-radius:999px;padding:2px 14px;font-size:13px;margin-top:10px;margin-inline-end:6px}
h2{font-size:19px;color:var(--brand2);border-right:5px solid var(--brand);
padding-right:12px;margin:34px 0 12px}
h3{font-size:16px;margin:20px 0 8px;color:var(--brand2)}
.card{background:var(--card);border:1px solid var(--line);border-radius:12px;
padding:18px 20px;margin:12px 0}
table{width:100%;border-collapse:collapse;font-size:13.5px}
th,td{border:1px solid var(--line);padding:7px 10px;text-align:right;vertical-align:top}
th{background:#eaf1fa;color:var(--brand2)}
ul,ol{padding-right:22px;padding-left:0;margin:8px 0}
li{margin:5px 0}
code,.mono{font-family:Consolas,"Courier New",monospace;font-size:13px;
background:#eef2f7;border-radius:5px;padding:1px 6px;direction:ltr;display:inline-block}
.note{border-right:5px solid #b98a2f;background:#fdf8ee}
.okbox{border-right:5px solid var(--ok);background:#eff8f3}
.dangerbox{border-right:5px solid var(--bad);background:#fdf0ef}
.step{display:flex;gap:12px;margin:14px 0;align-items:flex-start}
.stepnum{flex:0 0 34px;height:34px;border-radius:50%;background:var(--brand);color:#fff;
display:flex;align-items:center;justify-content:center;font-weight:700;font-size:16px}
.stepbody{flex:1}
.stepbody b{color:var(--brand2)}
.flist{font-family:Consolas,monospace;font-size:12.5px;direction:ltr;text-align:left;
color:#33475f;margin:6px 0}
.flist div{padding:1px 4px;border-bottom:1px dashed #e7ecf2}
.tagA{color:var(--ok);font-weight:700}
.tagM{color:#b98a2f;font-weight:700}
footer{margin-top:40px;color:var(--mut);font-size:12.5px;text-align:center}
.kbd{background:#e8edf2;border:1px solid #cbd5e1;border-bottom-width:3px;border-radius:6px;
padding:1px 8px;font-size:13px;font-family:inherit}
"""

GUIDE_HEAD = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>راهنمای بستهٔ آپدیت — سامانهٔ درمانگاه سهند ۲.۰.۵</title>
<style>""" + CSS + """</style>
</head>
<body>
<div class="wrap">

<header class="hero">
<h1>📦 بستهٔ آپدیت نسخهٔ ۲.۰.۴ ← ۲.۰.۵</h1>
<p>سامانهٔ جامع درمانگاه سهند — فقط فایل‌های تغییرکردهٔ این رلیز، با همان ساختار پوشه‌ها</p>
<span class="badge">__TOTAL__ فایل در این بسته</span>
<span class="badge">تاریخ انتشار: ۷ اکتبر ۲۰۲۶</span>
</header>
"""

SECTION_WHAT = """
<h2>۱) این بسته چیست؟</h2>
<div class="card">
<p>این بسته فقط <b>فایل‌های تغییرکرده از نسخهٔ ۲.۰.۴ تا ۲.۰.۵</b> را دارد — با همان ساختار پوشه‌ای مخزن.
اگر نسخهٔ فعلی شما ۲.۰.۴ است (پانویس پنجرهٔ ورود «نسخه ۲.۰.۴» را نشان می‌دهد)، همین بسته کافی است؛
و اگر نسخهٔ قدیمی‌تری دارید، اول بسته‌های قبلی را به ترتیب نصب کنید و بعد این بسته را.</p>
</div>

<div class="card note">
<p>💡 مثل همیشه <b>همهٔ فایل‌های داخل بسته را با هم</b> جایگزین کنید (۶ قدم پایین) و ویژوال استودیو را قبل از جایگزینی <b>کاملاً ببندید</b>.
جایگزینی جفت‌ناهماهنگ فایل‌های وابسته (XAML و کدِ پشتِ آن) علت خطاهای قبلی بوده است.</p>
</div>
"""

SECTION_FIXES = """
<h2>۲) چه چیزهایی رفع شده؟ (هر ۹ مورد گزارش شما)</h2>

<div class="card">
<table>
<tr><th style="width:34%">موضوع</th><th>توضیح</th></tr>
<tr><td>🎓 <b>تور آموزشی</b></td>
<td>هایلایت‌های اشتباه/خالی ریشهٔ فنی داشت: در نمایشگرهای با مقیاس ۱۲۵٪/۱۵۰٪ مختصات سوراخ هایلایت جابه‌جا می‌شد + نوار بالا نام‌گذاری نشده بود + گام‌های صفحات، آیتم ریزِ منو را روشن می‌کردند نه خود صفحه. اکنون: هایلایت دقیق روی هر عنصر، ناوبری خودکار به صفحهٔ هر گام و روشن‌شدن «هم آیتم منو و هم محتوای صفحه» با هم — نوشتهٔ هر حباب حالا با تصویر روی صفحه می‌خواند</td></tr>
<tr><td>🔑 <b>خطای «نشانی پنل لایسنس تنظیم نشده است»</b></td>
<td>نشانی سرور لایسنس در کد پیش‌فرض گرفت: <code>https://clinic-license.ea-fixer.ir/api.php</code> — این خطا دیگر در هیچ شرایطی رخ نمی‌دهد (حتی اگر تنظیم خالی باشد)</td></tr>
<tr><td>🔧 <b>سرور لایسنس پیش‌فرض + قابل تغییر</b></td>
<td>در صفحهٔ فعال‌سازی لایسنس فیلد «سرور لایسنس» اضافه شد؛ پیش‌فرض همان نشانی رسمی است و می‌توانید عوضش کنید (حتی قبل از ورود به برنامه). فقط ریشهٔ نشانی را بدهید کافی است — خودکار به <code>api.php</code> کامل می‌شود</td></tr>
<tr><td>📁 <b>فایل درخواست آفلاین (.req)</b></td>
<td>دکمهٔ «📥 دریافت فایل درخواست (.req)» در بخش فعال‌سازی آفلاین اضافه شد — فایل را ذخیره کنید و برای فروشنده بفرستید تا فایل <code>.lic</code> بگیرید و با دکمهٔ بارگذاری فعال کنید</td></tr>
<tr><td>🎧 <b>اطلاعات پشتیبانی</b></td>
<td>کارت پشتیبانی از همین اول با اطلاعات موجود نمایش داده می‌شود و با هر تماس موفق با سرور لایسنس (فعال‌سازی/تپ‌قلب روزانه) خودکار تازه می‌شود</td></tr>
<tr><td>🧩 <b>چیپس ماژول‌های فعال</b></td>
<td>قبلاً همه فقط «غیرفعال» خاکستری نشان می‌دادند؛ اکنون هر چیپس «نام ماژول • وضعیت» را نشان می‌دهد و ماژول‌های فعال <b style="color:#2E7D32">سبز</b> می‌شوند</td></tr>
<tr><td>🔢 <b>ساعت و تاریخ فارسی‌رقم</b></td>
<td>ساعت لحظه‌ای و تاریخ نوار بالا + کارت تاریخ و پیام خوش‌آمد داشبورد همه با <b>ارقام فارسی</b> نمایش داده می‌شوند؛ ساعت‌های حضور و غیاب، تاریخ پشتیبان، تاریخ صدور فیش حقوقی و موارد مشابه هم اصلاح شد</td></tr>
<tr><td>🖼 <b>عکس پرسنل و دکترها</b></td>
<td>در فرم ثبت پرسنل و فرم پزشک «کارت عکس» اضافه شد (انتخاب عکس JPG/PNG تا ۵ مگابایت + پیش‌نمایش). در فهرست پرسنل ستون «عکس» با دکمه‌های نمایش بزرگ / تعویض / حذف و در فهرست پزشکان با نمایش بزرگ / تعویض. با کلیک روی عکس، <b>تمام‌صفحه بزرگ</b> نمایش داده می‌شود (بستن با کلیک یا Esc). طبق قاعدهٔ شما: <b>مسیر فایل در دیتابیس</b> ذخیره می‌شود و <b>خود فایل در پوشهٔ Photos/Employees برنامه</b> قرار می‌گیرد</td></tr>
</table>
</div>
"""

SECTION_NOTES = """
<h2>۳) سه نکتهٔ مهم این نصب</h2>

<div class="card okbox">
<p><b>۱) سرور لایسنس PHP:</b> فایل <code>src/LicenseServer.Php/api.php</code> هم در بسته است — اگر پنل لایسنس را روی هاست جداگانه دارا هستید، این فایل را هم آنجا جایگزین کنید تا «اطلاعات پشتیبانی پیش‌فرض از سرور» و اکشن عمومی <code>support</code> کار کند. اگر پنل را خودتان مستقر نکرده‌اید، نیازی به کار خاصی نیست — پیش‌فرض‌ها از خود برنامه می‌آید.</p>
<p><b>۲) پوشهٔ عکس‌ها:</b> عکس‌های پرسنل/پزشکان در زیرپوشهٔ <code>Photos\\Employees</code> کنار برنامهٔ API ذخیره می‌شوند و به‌صورت خودکار ساخته می‌شود — نیازی به ساخت دستی نیست. اگر آنتی‌ویروس پوشهٔ برنامه را می‌بندد، پوشهٔ Photos را در استثنائات قرار دهید.</p>
<p><b>۳) لایسنس بدون تغییر دستی:</b> دیگر لازم نیست چیزی در <code>appsettings.json</code> تنظیم کنید — نشانی سرور لایسنس از خود برنامه (فیلد سرور لایسنس) قابل تغییر است و در دیتابیس ذخیره می‌شود.</p>
</div>
"""

SECTION_STEPS = """
<h2>۴) نصب در ۶ قدم (حدود ۲ دقیقه)</h2>

<div class="step"><div class="stepnum">۱</div><div class="stepbody">
<b>ویژوال استودیو را کاملاً ببندید</b> (بستن کامل، نه کمینه‌کردن) — این مهم‌ترین قدم است.
اگر VS باز بماند ممکن است فایل قدیمی را از حافظهٔ خودش دوباره روی فایل جدید ذخیره کند.
</div></div>

<div class="step"><div class="stepnum">۲</div><div class="stepbody">
<b>پوشهٔ سورس فعلی خود را باز کنید</b> — همان پوشه‌ای که سولوشن در آن است (مثلاً <code>E:\\Sahand_ClinicPro-2.0.1</code> یا هر نامی که دارد).
</div></div>

<div class="step"><div class="stepnum">۳</div><div class="stepbody">
فایل ZIP این بسته را باز کنید و <b>همهٔ محتوایش را انتخاب کنید</b> (<span class="kbd">Ctrl+A</span>) —
هم پوشه‌های <code>src</code> و <code>tests</code> را و هم فایل‌های ریشه (<code>Directory.Build.props</code>، <code>CHANGELOG.md</code>، <code>README.md</code>).
</div></div>

<div class="step"><div class="stepnum">۴</div><div class="stepbody">
همه را روی <b>ریشهٔ پوشهٔ سورس</b> پیست کنید و در پنجرهٔ ویندوز <b>Replace the files</b> را بزنید
(اگر دربارهٔ ادغام پوشه‌ها پرسید: بله/برای همه).
</div></div>

<div class="step"><div class="stepnum">۵</div><div class="stepbody">
ویژوال استودیو را باز کنید، سولوشن را باز کنید و از منوی <b>Build → Rebuild Solution</b> بزنید
(نه Build معمولی — Rebuild). باید <b>صفر خطا</b> بگیرید.
</div></div>

<div class="step"><div class="stepnum">۶</div><div class="stepbody">
برنامه را اجرا کنید و پنج اطمینان پایین را چک کنید.
</div></div>

<div class="card dangerbox">
<p>⚠️ اگر بیلد خطا داد، فایل‌ها ناهماهنگ کپی شده‌اند — قدم ۱ را جدی بگیرید (بستن VS)،
دوباره همهٔ بسته را Replace کنید و Rebuild بزنید. فایل <code>appsettings.json</code> (تنظیم SQL شما) و دیتابیس و رمز شما در این بسته نیست و دست نمی‌خورد.</p>
</div>
"""

SECTION_CONFIRM = """
<h2>۵) پنج اطمینان پس از نصب</h2>
<div class="card okbox">
<ol>
<li>پانویس پنجرهٔ ورود باید <b>«نسخه ۲.۰.۵»</b> را نشان دهد ← یعنی بسته درست نصب شده</li>
<li>دکمهٔ «🔑 فعال‌سازی لایسنس» را بزنید ← باید <b>فیلد «سرور لایسنس»</b> با نشانی پیش‌فرض پر باشد، دکمهٔ <b>«📥 دریافت فایل درخواست (.req)»</b> دیده شود و کارت <b>پشتیبانی</b> اطلاعات داشته باشد</li>
<li>وارد برنامه شوید ← ساعت نوار بالا باید <b>ارقام فارسی</b> داشته باشد (۱۴:۰۵:۳۰ مثل)</li>
<li>اگر تور آموزشی را دیده باشید: از منوی «تنظیمات → مرکز آموزش» دوباره ببینید ← هایلایت‌ها باید دقیقاً دور عنصر مربوطه باشند</li>
<li>«تنظیمات → پرسنل» ← ستون <b>عکس</b> جدید با دکمه‌های نمایش/تعویض/حذف؛ یک عکس انتخاب کنید و روی آیکون نمایش بزنید ← تمام‌صفحه بزرگ نشان داده می‌شود</li>
</ol>
</div>
"""

SECTION_FILES = """
<h2>۶) فهرست فایل‌های این بسته</h2>
<div class="card">
<p class="flist">__FILELIST__</p>
<p style="font-size:13px;color:var(--mut)">نشانه‌گذاری: <span class="tagA">A = فایل جدید</span> | <span class="tagM">M = ویرایش‌شده</span> — بدون هیچ فایل حذف‌شده</p>
</div>
"""

GUIDE_FOOT = """
<footer>
ساخته‌شده در چرخه هجدهم — گیت کلون <span class="mono">pranses2011/Sahand_ClinicPro</span> · رلیز v2.0.5
</footer>
</div>
</body>
</html>
"""


def build_guide(total: int, files: list) -> str:
    """مونتاژ راهنمای HTML — بدون f-string (درس چرخه ۱۵)"""
    groups = {}
    for status, path in files:
        groups.setdefault(group_title(path), []).append((status, path))

    lines = []
    for group in dict.fromkeys(group_title(p) for _, p in files):
        lines.append("<div><b style='color:#0D47A1'>" + group + "</b></div>")
        for status, path in files:
            if group_title(path) == group:
                tag = '<span class="tagA">A</span>' if status == "A" else '<span class="tagM">M</span>'
                lines.append("<div>" + tag + " " + path.replace("\\", "/") + "</div>")

    html = (GUIDE_HEAD
            + SECTION_WHAT
            + SECTION_FIXES
            + SECTION_NOTES
            + SECTION_STEPS
            + SECTION_CONFIRM
            + SECTION_FILES
            + GUIDE_FOOT)
    html = html.replace("__TOTAL__", str(total))
    html = html.replace("__FILELIST__", "".join(lines))
    return html


def main() -> int:
    files = changed_files()

    # ── گارد ۱: بدون فایل حذف‌شده ──
    deleted = [p for s, p in files if s.startswith("D")]
    if deleted:
        print("⛔ توقف — فایل حذف‌شده در محدوده (نیازمند بازبینی):", deleted)
        return 1

    # ── گارد ۲: appsettings هرگز در بسته نیست ──
    unsafe = [p for _, p in files if "appsettings" in p.lower()]
    if unsafe:
        print("⛔ توقف — فایل تنظیمات در بسته نباید باشد:", unsafe)
        return 1

    # ── گارد ۳: نشانه‌های سلامت در HEAD ──
    for path, marker, desc in COHERENCE_CHECKS:
        full = REPO / path
        if not full.exists():
            print("⛔ توقف — فایل کلیدی یافت نشد:", path)
            return 1
        content = full.read_text(encoding="utf-8-sig", errors="replace")
        if marker not in content:
            print("⛔ توقف — نشانهٔ سلامت یافت نشد:", desc, "—", path)
            return 1

    # ── گارد ۴: پیام خطای رفع‌شده باید حذف شده باشد ──
    for path, marker, desc in ABSENCE_CHECKS:
        full = REPO / path
        content = full.read_text(encoding="utf-8-sig", errors="replace")
        if marker in content:
            print("⛔ توقف — نشانهٔ ممنوع هنوز موجود است:", desc, "—", path)
            return 1

    out = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else (REPO / OUT_NAME)
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for status, path in files:
            zf.write(REPO / path, path.replace("\\", "/"))
        zf.writestr(GUIDE_NAME, build_guide(len(files), files))
        manifest = ("# فایل‌های تغییرکرده v2.0.4 → v2.0.5 (چرخه هجدهم — گزارش دوازدهم مالک)\n"
                    "# A = جدید | M = ویرایش | بدون حذف\n")
        manifest += "\n".join(f"{s}\t{p.replace(chr(92), '/')}" for s, p in files)
        manifest += f"\n\n# جمع: {len(files)} فایل ({sum(1 for s, _ in files if s == 'A')} جدید + {sum(1 for s, _ in files if s == 'M')} ویرایش)\n"
        zf.writestr(MANIFEST_NAME, manifest)

    size_kb = out.stat().st_size / 1024
    print(f"✅ بسته ساخته شد: {out}  ({size_kb:.1f} KB)")
    print(f"   {len(files)} فایل ({sum(1 for s, _ in files if s == 'A')} جدید + {sum(1 for s, _ in files if s == 'M')} ویرایش) + راهنما + مانیفست")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
