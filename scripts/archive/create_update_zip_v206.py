#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📦 ساخت بستهٔ آپدیت v2.0.5 → v2.0.6 — چرخه نوزدهم (گزارش سیزدهم مالک)
فقط فایل‌های تغییرکردهٔ همین رلیز با همان ساختار پوشه‌ای مخزن،
به‌همراه راهنمای فارسی HTML و مانیفست متنی.

رفع‌های این بسته (هر ۱۱ مورد گزارش سیزدهم):
  ۱) فهرست بیماران خالی — پاسخ دوبل صفحه‌بندی (data.data)
  ۲) کرش آیکون AccountLarge در دکمهٔ پزشک جدید
  ۳) کرش آیکون Reception در منوی پذیرش
  ۴) کرش آیکون DatabaseBackup در منوی بکاپ
  ۵) استان/شهر کشویی با هر ۳۱ استان و شهرستان‌های ایران
  ۶) فیلدهای نامرئی شهر و شغل در فرم بیمار
  ۷) ارقام فارسی تقویم + نمایش کامل روزهای دورقمی
  ۸) اعتبارسنجی کامل کد ملی (رقم کنترلی) + فرم پرسنل
  ۹) چیپس ماژول‌های لایسنس (Billing به‌جای Invoices)
  ۱۰) چیدمان تمام‌صفحه صفحهٔ لایسنس + رفع موارد مشابه RTL
  ۱۱) هشت پوستهٔ رنگی قابل انتخاب از تنظیمات
"""
import pathlib
import subprocess
import sys
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
TAG = "v2.0.5"
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.0.5-to-2.0.6.zip")
GUIDE_NAME = "Update-Guide-2.0.6.html"
MANIFEST_NAME = "changed-files.txt"

# ─── گارد انسجام مثبت — نشانه‌های سلامت رفع‌های این نسخه ───
COHERENCE_CHECKS = [
    # ۱) فهرست‌ها
    ("src/Sahand.ClinicPro.API/Controllers/ApiBaseController.cs",
     "ToPagedResponse", "متد تک‌لایهٔ پاسخ صفحه‌بندی"),
    ("src/Sahand.ClinicPro.API/Controllers/PatientsController.cs",
     "ToPagedResponse(result)", "کنترلر بیماران تک‌لایه"),
    # ۲) آیکون‌ها
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorFormWindow.xaml",
     'Kind="AccountCircle"', "آیکون جایگزین فرم پزشک"),
    ("src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml",
     'Kind="ClipboardAccount"', "آیکون جایگزین پذیرش"),
    ("src/Sahand.ClinicPro.Desktop/Views/BackupView.xaml",
     'Kind="DatabaseSync"', "آیکون جایگزین بکاپ"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     'Icon = "ClipboardAccount"', "آیکون منوی پذیرش"),
    # ۵+۶) فرم بیمار
    ("src/Sahand.ClinicPro.Desktop/Helpers/IranGeoData.cs",
     "خراسان رضوی", "دادهٔ کامل استان‌های ایران"),
    ("src/Sahand.ClinicPro.Desktop/Views/PatientFormWindow.xaml",
     'ItemsSource="{Binding CityOptions}"', "کشویی شهر وابسته به استان"),
    # ۷) تقویم
    ("src/Sahand.ClinicPro.Desktop/Controls/PersianCalendarControl.xaml.cs",
     "ToPersianDigits(day.ToString())", "روزهای تقویم با ارقام فارسی"),
    ("src/Sahand.ClinicPro.Desktop/Controls/PersianDatePicker.xaml.cs",
     "ToShamsiFa(date)", "نمایش فارسی انتخابگر تاریخ"),
    # ۸) کد ملی
    ("src/Sahand.ClinicPro.Desktop/Helpers/Validators.cs",
     "IsValidNationalCode", "اعتبارسنج مشترک کد ملی"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/EmployeeFormViewModel.cs",
     "رقم کنترلی درست", "اعتبارسنجی کد ملی فرم پرسنل"),
    # ۹) لایسنس
    ("src/Sahand.ClinicPro.Desktop/ViewModels/LicenseStatusViewModel.cs",
     '["Billing"] = "صندوق و فاکتور"', "کلید درست ماژول صندوق/فاکتور"),
    # ۱۰) چیدمان
    ("src/Sahand.ClinicPro.Desktop/Views/LicenseStatusView.xaml",
     'MaxWidth="1200" HorizontalAlignment="Stretch"', "تمام‌صفحه شدن صفحهٔ لایسنس"),
    # ۱۱) پوسته‌ها
    ("src/Sahand.ClinicPro.Desktop/Services/ThemeService.cs",
     "فیروزه‌ای", "کاتالوگ هشت پوستهٔ رنگی"),
    ("src/Sahand.ClinicPro.Desktop/Views/SettingsView.xaml",
     "SelectThemeCommand", "کارت‌های انتخاب پوسته در تنظیمات"),
    ("src/Sahand.ClinicPro.Desktop/App.xaml.cs",
     "ThemeKey", "اعمال پوستهٔ ذخیره‌شده در راه‌اندازی"),
    # نسخه
    ("Directory.Build.props",
     "<Version>2.0.6</Version>", "نسخهٔ 2.0.6"),
]

ABSENCE_CHECKS = [
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorFormWindow.xaml",
     'Kind="AccountLarge"', "آیکون نامعتبر نباید باشد"),
    ("src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml",
     'Kind="Reception"', "آیکون نامعتبر نباید باشد"),
    ("src/Sahand.ClinicPro.Desktop/Views/BackupView.xaml",
     'Kind="DatabaseBackup"', "آیکون نامعتبر نباید باشد"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/LicenseStatusViewModel.cs",
     '["Invoices"]', "کلید قدیمی Invoices نباید باشد"),
]

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


def changed_files():
    out = subprocess.run(
        ["git", "diff", "--name-status", TAG + "..HEAD"],
        cwd=REPO, capture_output=True, text=True, check=True,
    ).stdout
    files = []
    for line in out.splitlines():
        if not line.strip():
            continue
        parts = line.split("\t")
        files.append((parts[0], parts[-1]))
    return files


def group_title(path):
    p = path.replace("\\", "/")
    if p.startswith("src/Sahand.ClinicPro.Desktop/Views/"):
        return "پنجره‌ها و نماها (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/Controls/"):
        return "کنترل‌های مشترک (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/ViewModels/"):
        return "ViewModel ها (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/Services/"):
        return "سرویس‌ها (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/Helpers/"):
        return "راهنماها و داده‌های مرجع (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/"):
        return "هستهٔ برنامهٔ دسکتاپ"
    if p.startswith("src/Sahand.ClinicPro.API/"):
        return "لایهٔ API (سرور محلی)"
    if p.startswith("src/Sahand.ClinicPro.Application/"):
        return "لایهٔ کاربرد (Application)"
    if p.startswith("tests/"):
        return "پروژه‌های تست"
    return "فایل‌های ریشهٔ مخزن"


def build_guide(total, files):
    groups_lines = []
    for group in dict.fromkeys(group_title(p) for _, p in files):
        groups_lines.append("<div><b style='color:#0D47A1'>" + group + "</b></div>")
        for status, path in files:
            if group_title(path) == group:
                tag = '<span class="tagA">A</span>' if status == "A" else '<span class="tagM">M</span>'
                groups_lines.append("<div>" + tag + " " + path.replace("\\", "/") + "</div>")

    html = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>راهنمای بستهٔ آپدیت — سامانهٔ درمانگاه سهند ۲.۰.۶</title>
<style>""" + CSS + """</style>
</head>
<body>
<div class="wrap">

<header class="hero">
<h1>📦 بستهٔ آپدیت نسخهٔ ۲.۰.۵ ← ۲.۰.۶</h1>
<p>سامانهٔ جامع درمانگاه سهند — فقط فایل‌های تغییرکردهٔ این رلیز، با همان ساختار پوشه‌ها</p>
<span class="badge">__TOTAL__ فایل در این بسته</span>
<span class="badge">تاریخ انتشار: ۸ اکتبر ۲۰۲۶</span>
</header>

<h2>۱) این بسته چیست؟</h2>
<div class="card">
<p>این بسته فقط <b>فایل‌های تغییرکرده از نسخهٔ ۲.۰.۵ تا ۲.۰.۶</b> را دارد — با همان ساختار پوشه‌ای مخزن.
اگر نسخهٔ فعلی شما ۲.۰.۵ است (پانویس پنجرهٔ ورود «نسخه ۲.۰.۵» را نشان می‌دهد)، همین بسته کافی است؛
برای نسخه‌های قدیمی‌تر اول بسته‌های قبلی را به ترتیب نصب کنید.</p>
</div>

<div class="card note">
<p>💡 مثل همیشه <b>همهٔ فایل‌های داخل بسته را با هم</b> جایگزین کنید (۶ قدم پایین) و ویژوال استودیو را قبل از جایگزینی <b>کاملاً ببندید</b>.</p>
</div>

<h2>۲) چه چیزهایی رفع شده؟ (هر ۱۱ مورد گزارش شما)</h2>
<div class="card">
<table>
<tr><th style="width:34%">موضوع</th><th>توضیح</th></tr>
<tr><td>👥 <b>فهرست بیماران خالی</b></td>
<td>ریشهٔ فنی پیدا شد: پاسخ endpointهای صفحه‌بندی‌شده دوبار در پاکت استاندارد پیچیده می‌شد (<code>data.data</code>) و دسکتاپ <code>data</code> را در ریشه انتظار داشت. با متد تک‌لایهٔ جدید <code>ToPagedResponse</code> در ۵ کنترلر (بیماران/پزشکان/نسخه‌ها/فاکتورها/جستجوی نوبت‌ها) هم‌زمان رفع شد — یعنی فهرست پزشکان، نسخه‌ها و فاکتورها و کشوی پزشکان در همهٔ صفحات هم که همین مشکل را داشتند درست شدند</td></tr>
<tr><td>💥 <b>خطای دکمهٔ پزشک جدید</b></td>
<td>آیکون <code>AccountLarge</code> در کتابخانهٔ MaterialDesign نسخهٔ ۵.۳.۲ وجود ندارد → با <code>AccountCircle</code> جایگزین شد (همین مشکل در فرم پرسنل هم بود و رفع شد)</td></tr>
<tr><td>🩺 <b>خطای منوی پذیرش</b></td>
<td>آیکون <code>Reception</code> نامعتبر بود → با <code>ClipboardAccount</code> جایگزین شد (هم در صفحهٔ پذیرش و هم در منوی کناری)</td></tr>
<tr><td>💾 <b>خطای منوی بکاپ</b></td>
<td>آیکون <code>DatabaseBackup</code> نامعتبر بود → با <code>DatabaseSync</code> جایگزین شد (صفحه + منو)</td></tr>
<tr><td>🔎 <b>بقیهٔ آیکون‌های نامعتبر</b></td>
<td>کل ۷۳ فایل رابط کاربری با فهرست واقعی ۱۳٬۶۴۷ آیکون معتبر کتابخانه اسکن شد — ۵ مورد دیگر هم پیدا و رفع شد (گزارش‌ها، انبارگردانی، نظرسنجی، پیامک دسته‌ای)</td></tr>
<tr><td>🗺 <b>استان و شهر کشویی</b></td>
<td>هر ۳۱ استان ایران + همهٔ شهرستان‌های هر استان، به‌صورت پیش‌فرض داخل برنامه (بدون نیاز به اینترنت). با انتخاب استان، فهرست شهرهای همان استان باز می‌شود؛ در ویرایش بیمار مقدار قبلی حفظ می‌شود</td></tr>
<tr><td>👁 <b>فیلدهای نامرئی</b></td>
<td>ریشهٔ «دیده نشدن فیلد جلوی استان (شهر) و جلوی کدپستی (شغل)»: هر دو در ستون فاصله‌دار ۱۲ پیکسلی نشسته بودند! به ستون درست منتقل شدند و کل برنامه هم برای الگوی مشابه اسکن شد</td></tr>
<tr><td>📅 <b>ارقام فارسی تقویم</b></td>
<td>همهٔ اعداد تقویم انتخابگر تاریخ (روزها، عنوان ماه/سال، نشانگر تعداد نوبت، راهنمای ماوس) فارسی شدند + سلول‌ها از بریدن روزهای دورقمی محافظت می‌شوند + یک باگ پنهان: انتخاب تاریخ از تقویم به فرم منتقل نمی‌شد که همین حالا رفع شد</td></tr>
<tr><td>🪪 <b>کد ملی</b></td>
<td>صحت کد ملی با الگوریتم رسمی رقم کنترلی سنجیده می‌شود (نه فقط تعداد رقم) — فرم پرسنل که اصلاً اعتبارسنجی نداشت هم به همین قانون مجهز شد + فرمت موبایل</td></tr>
<tr><td>🧩 <b>ماژول‌های لایسنس</b></td>
<td>ریشهٔ «غیرفعال نشان دادن صندوق و فاکتور»: برنامه دنبال کلید <code>Invoices</code> می‌گشت در حالی که سرور لایسنس <code>Billing</code> می‌فرستد! کلیدها با کاتالوگ رسمی سرور هماهنگ شد و ۹ ماژول غایب (داشبورد/ویزیت/پرسنل/گزارش‌ها/سامانه دولتی/تنظیمات/بکاپ/لاگ/کاربران) هم به چیپس‌ها اضافه شدند</td></tr>
<tr><td>📐 <b>صفحهٔ لایسنس نصفه</b></td>
<td>در حالت راست‌به‌چپ، مقدار «تراز راست» یعنی سمت چپِ صفحه! ستون محتوا به کل عرض گسترش یافت — همین سنجاق‌شدن به چپ در ۷ صفحهٔ دیگر (بکاپ، تنظیمات، پیامک، نظرسنجی، نوبت‌دهی، ویزیت) هم رفع شد</td></tr>
<tr><td>🎨 <b>هشت پوستهٔ رنگی</b></td>
<td>آبی 💙 فیروزه‌ای 🩵 سبز 💚 بنفش 💜 نیلی 🫐 زرشکی ❤️ نارنجی 🧡 صورتی 💗 — از «تنظیمات ← تم و ظاهر» با یک کلیک بلافاصله اعمال می‌شود؛ انتخاب شما ذخیره می‌شود و دفعهٔ بعد برنامه از همان لحظهٔ اول با همان پوسته باز می‌شود</td></tr>
</table>
</div>

<h2>۳) نکتهٔ مهم این نصب</h2>
<div class="card okbox">
<p><b>این نسخه هم فایل‌های سرور (API) دارد و هم دسکتاپ</b> — فهرست بیماران و ماژول‌های لایسنس سمت سرور اصلاح شدند؛ پس از جایگزینی فایل‌ها حتماً <b>Rebuild Solution</b> بزنید (نه Build معمولی). فایل <code>appsettings.json</code> و دیتابیس و رمز شما در بسته نیست و دست نمی‌خورد.</p>
</div>

<h2>۴) نصب در ۶ قدم (حدود ۲ دقیقه)</h2>
<div class="step"><div class="stepnum">۱</div><div class="stepbody"><b>ویژوال استودیو را کاملاً ببندید</b> — بستن کامل، نه کمینه‌کردن.</div></div>
<div class="step"><div class="stepnum">۲</div><div class="stepbody"><b>پوشهٔ سورس فعلی خود را باز کنید</b> — همان پوشه‌ای که سولوشن در آن است.</div></div>
<div class="step"><div class="stepnum">۳</div><div class="stepbody">فایل ZIP این بسته را باز کنید و <b>همهٔ محتوایش را انتخاب کنید</b> (<span class="kbd">Ctrl+A</span>) — هم پوشهٔ <code>src</code> و <code>tests</code> و هم فایل‌های ریشه.</div></div>
<div class="step"><div class="stepnum">۴</div><div class="stepbody">همه را روی <b>ریشهٔ پوشهٔ سورس</b> پیست کنید و <b>Replace the files</b> را بزنید (اگر پرسید: بله/برای همه).</div></div>
<div class="step"><div class="stepnum">۵</div><div class="stepbody">ویژوال استودیو را باز کنید و از منوی <b>Build → Rebuild Solution</b> بزنید. باید <b>صفر خطا</b> بگیرید.</div></div>
<div class="step"><div class="stepnum">۶</div><div class="stepbody">برنامه را اجرا کنید و شش اطمینان پایین را چک کنید.</div></div>
<div class="card dangerbox">
<p>⚠️ اگر بیلد خطا داد، فایل‌ها ناهماهنگ کپی شده‌اند — بسته را دوباره کامل Replace کنید و Rebuild بزنید.</p>
</div>

<h2>۵) شش اطمینان پس از نصب</h2>
<div class="card okbox">
<ol>
<li>پانویس پنجرهٔ ورود باید <b>«نسخه ۲.۰.۶»</b> را نشان دهد</li>
<li>صفحهٔ بیماران ← فهرست باید نمایش داده شود (و کشوی پزشکان صفحات دیگر هم پر باشد)</li>
<li>منوی کناری ← <b>پذیرش</b> و <b>بکاپ و بازیابی</b> بدون خطا باز شوند؛ پزشکان ← دکمهٔ پزشک جدید بدون خطا</li>
<li>فرم بیمار جدید ← <b>استان</b> و <b>شهر</b> کشویی با فهرست کامل + فیلد <b>شغل</b> کنار کدپستی دیده شود؛ انتخابگر تاریخ ← اعداد فارسی</li>
<li>لایسنس ← چیپس «صندوق و فاکتور» در لایسنس فعال <b style="color:#2E7D32">سبز</b> باشد و اطلاعات تمام‌عرض نمایش داده شود</li>
<li>تنظیمات ← تم و ظاهر ← هشت کارت رنگی؛ یکی را انتخاب کنید ← کل برنامه همان‌جا رنگ عوض کند؛ ببندید و دوباره باز کنید ← همان پوسته باشد</li>
</ol>
</div>

<h2>۶) فهرست فایل‌های این بسته</h2>
<div class="card">
<p class="flist">__FILELIST__</p>
<p style="font-size:13px;color:var(--mut)">نشانه‌گذاری: <span class="tagA">A = فایل جدید</span> | <span class="tagM">M = ویرایش‌شده</span> — بدون هیچ فایل حذف‌شده</p>
</div>

<footer>
ساخته‌شده در چرخه نوزدهم — گیت کلون <span class="mono">pranses2011/Sahand_ClinicPro</span> · رلیز v2.0.6
</footer>
</div>
</body>
</html>
"""
    html = html.replace("__TOTAL__", str(total))
    html = html.replace("__FILELIST__", "".join(groups_lines))
    return html


def main() -> int:
    files = changed_files()

    deleted = [p for s, p in files if s.startswith("D")]
    if deleted:
        print("⛔ فایل حذف‌شده در محدوده:", deleted)
        return 1
    unsafe = [p for _, p in files if "appsettings" in p.lower()]
    if unsafe:
        print("⛔ فایل تنظیمات در بسته نباید باشد:", unsafe)
        return 1

    for path, marker, desc in COHERENCE_CHECKS:
        full = REPO / path
        if not full.exists():
            print("⛔ فایل کلیدی یافت نشد:", path)
            return 1
        content = full.read_text(encoding="utf-8-sig", errors="replace")
        if marker not in content:
            print("⛔ نشانهٔ سلامت یافت نشد:", desc, "—", path)
            return 1

    for path, marker, desc in ABSENCE_CHECKS:
        full = REPO / path
        content = full.read_text(encoding="utf-8-sig", errors="replace")
        if marker in content:
            print("⛔ نشانهٔ ممنوع هنوز موجود است:", desc, "—", path)
            return 1

    OUT.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for status, path in files:
            zf.write(REPO / path, path.replace("\\", "/"))
        zf.writestr(GUIDE_NAME, build_guide(len(files), files))
        manifest = ("# فایل‌های تغییرکرده v2.0.5 → v2.0.6 (چرخه نوزدهم — گزارش سیزدهم مالک)\n"
                    "# A = جدید | M = ویرایش | بدون حذف\n")
        manifest += "\n".join(f"{s}\t{p.replace(chr(92), '/')}" for s, p in files)
        manifest += f"\n\n# جمع: {len(files)} فایل ({sum(1 for s, _ in files if s == 'A')} جدید + {sum(1 for s, _ in files if s == 'M')} ویرایش)\n"
        zf.writestr(MANIFEST_NAME, manifest)

    size_kb = OUT.stat().st_size / 1024
    print(f"✅ بسته ساخته شد: {OUT}  ({size_kb:.1f} KB)")
    print(f"   {len(files)} فایل ({sum(1 for s, _ in files if s == 'A')} جدید + {sum(1 for s, _ in files if s == 'M')} ویرایش) + راهنما + مانیفست")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
