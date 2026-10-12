#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.0.6 → v2.0.7 — چرخه بیستم (گزارش چهاردهم مالک).
فقط فایل‌های تغییرکردهٔ همین رلیز با همان ساختار پوشه‌ای مخزن،
به‌همراه راهنمای فارسی HTML و مانیفست متنی — الگوی build_release_assets_v20.

رفع‌های این بسته (گزارش چهاردهم مالک — ۱۸ مورد):
  ۱) خطای 404 تم/تنظیمات (۱۸ مسیر الحاقی خراب در ۳ کنترلر)
  ۲) ارجاع مشخصات بیمار به فرم ویرایش/نوبت/پرونده + دکمه‌های ردیف
  ۳) منوی پرسنل که صفحهٔ تنظیمات را باز می‌کرد
  ۴) کرش صفحهٔ نقش‌ها (ToggleButton)
  ۵) فونت و اندازهٔ فونت که کار نمی‌کرد (منابع پویا + حذف فونت هاردکدشده)
  ۶) تم فقط رنگ نباشد — گوشهٔ کارت‌ها + چگالی در هر پوسته
  ۷) RTL هشت پنجرهٔ فرم (ثبت پرسنل و…)
  ۸) تعداد کل خودکار نسخه از مدت/مصرف/دوز
  ۹) خطای غیرمنتظرهٔ ذخیره تمپلیت نسخه (ستون ۲۰۰ نویسه‌ای)
  ۱۰) تداخل‌های دارویی داخل برنامه + دکمهٔ بروزرسانی
  ۱۱) فهرست کامل داروها + دکمهٔ بروزرسانی لیست داروها
  ۱۲) اعتبارسنجی زندهٔ کد ملی با متن زیر فیلد
  ۱۳) ذخیره‌نشدن تنظیمات بکاپ خودکار (همان 404) + مسیر دابل‌بک‌اسلش
  ۱۴) فعال‌سازی و تنظیم پشتیبان ابری از داخل برنامه
  ۱۵) جداکنندهٔ ۳ رقمی همهٔ مبالغ + کادرهای ورود زنده
  ۱۶) واحد پولی ریال/تومان فقط نمایشی
  ۱۷) لیست پزشکان در فرم نوبت از صفحهٔ بیماران
  ۱۸) دکمه‌های عملیاتی ناقص لیست پزشکان
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
TAG = "v2.0.6"
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.0.6-to-2.0.7.zip")
GUIDE_NAME = "Update-Guide-2.0.7.html"
MANIFEST_NAME = "changed-files.txt"

# ─── گارد انسجام مثبت — نشانه‌های سلامت رفع‌های این نسخه ───
COHERENCE_CHECKS = [
    # ۱) مسیرهای مطلق 404
    ("src/Sahand.ClinicPro.API/Controllers/AdminController.cs",
     '[HttpPut("~/api/v1/settings")]', "مسیر مطلق ذخیرهٔ تنظیمات"),
    ("src/Sahand.ClinicPro.API/Controllers/ParaclinicalController.cs",
     '[HttpPost("~/api/v1/imaging/orders")]', "مسیر مطلق پاراکلینیک"),
    ("src/Sahand.ClinicPro.API/Controllers/ArchiveController.cs",
     '[HttpGet("~/api/v1/admin/archive/preview")]', "مسیر مطلق آرشیو"),
    # ۲) پاکت پاسخ بیمار
    ("src/Sahand.ClinicPro.Desktop/ViewModels/PatientDetailViewModel.cs",
     "PatientDetailEnvelope", "پاکت استاندارد جزئیات بیمار"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/PatientsViewModel.cs",
     "EditPatient(object? parameter)", "پارامتر ردیف ویرایش بیمار"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/AppointmentFormViewModel.cs",
     "preselectedPatient", "بیمار از پیش انتخاب‌شدهٔ فرم نوبت"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/AppointmentFormViewModel.cs",
     "LoadDoctorsAsync", "بارگذاری خودکار پزشکان فرم نوبت"),
    # ۳) پرسنل
    ("src/Sahand.ClinicPro.Desktop/ViewModels/PersonnelViewModel.cs",
     'Title => "مدیریت پرسنل"', "ViewModel مستقل پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     '["personnel"] = () => personnelVm', "نگاشت منوی پرسنل"),
    # ۴) نقش‌ها
    ("src/Sahand.ClinicPro.Desktop/Views/RolePermissionView.xaml",
     "<ToggleButton Grid.Row=\"0\" Grid.Column=\"1\"", "ToggleButton صفحهٔ نقش‌ها"),
    # ۵+۶) تم
    ("src/Sahand.ClinicPro.Desktop/Services/ThemeService.cs",
     "App.CardRadius", "گوشهٔ کارت‌ها از تم"),
    ("src/Sahand.ClinicPro.Desktop/App.xaml",
     "{DynamicResource App.FontFamily}", "فونت پویا در سبک Window"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/SettingsViewModel.cs",
     "OnCurrencyUnitIndexChanged", "تعویض واحد پول از تنظیمات"),
    ("src/Sahand.ClinicPro.Desktop/Helpers/Money.cs",
     "FromDisplayInput", "کمکی مبلغ با واحد نمایشی"),
    # ۷) RTL
    ("src/Sahand.ClinicPro.Desktop/Views/EmployeeFormWindow.xaml",
     'FlowDirection="RightToLeft"', "RTL فرم پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/Views/ReceptionFormWindow.xaml",
     'FlowDirection="RightToLeft"', "RTL فرم پذیرش"),
    # ۸) تعداد کل زنده
    ("src/Sahand.ClinicPro.Desktop/ViewModels/PrescriptionFormViewModel.cs",
     "partial void OnDosageChanged(string? value) => RecalculateTotal()", "بازمحاسبهٔ زندهٔ تعداد کل"),
    # ۹) تمپلیت
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Clinical/PrescriptionTemplateConfiguration.cs",
     'HasColumnType("nvarchar(max)")', "ستون JSON تمپلیت بدون سقف"),
    ("src/Sahand.ClinicPro.Infrastructure/DependencyInjection.cs",
     "GetPendingMigrations", "مهاجرت خودکار در راه‌اندازی"),
    # ۱۰+۱۱) دارو و تداخل
    ("src/Sahand.ClinicPro.Desktop/Services/DrugInteractionCache.cs",
     "UpdateAsync", "کش تداخل محلی + بروزرسانی"),
    ("src/Sahand.ClinicPro.API/Controllers/PrescriptionsController.cs",
     "medications/refresh-catalog", "endpoint بروزرسانی فهرست داروها"),
    ("src/Sahand.ClinicPro.API/Controllers/PrescriptionsController.cs",
     "GetDrugInteractionsQuery", "endpoint دانلود تداخل‌ها"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/MedicationSeedDataSupplement.cs",
     "Aceclofenac", "داروهای مکمل کاتالوگ"),
    # ۱۲) کد ملی زنده
    ("src/Sahand.ClinicPro.Desktop/ViewModels/EmployeeFormViewModel.cs",
     "NationalCodeValidationText", "متن اعتبارسنجی زندهٔ فرم پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/PatientsViewModel.cs",
     "NationalCodeValidationText", "متن اعتبارسنجی زندهٔ فرم بیمار"),
    # ۱۴) ابر
    ("src/Sahand.ClinicPro.API/Controllers/BackupController.cs",
     "cloud/config", "endpoint تنظیم پشتیبان ابری"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/BackupViewModel.cs",
     "SaveCloudConfigAsync", "ذخیرهٔ تنظیمات ابری در دسکتاپ"),
    # ۱۵) مبالغ
    ("src/Sahand.ClinicPro.Desktop/Controls/MoneyInput.cs",
     "NumericValueProperty", "رفتار کادر مبلغ زنده"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/BillingViewModel.cs",
     "Money.Format", "قالب مبلغ صندوق"),
    # ۱۸) پزشکان
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorsView.xaml",
     'Header="عملیات" Width="218"', "عرض کامل ستون عملیات پزشکان"),
    # نسخه
    ("Directory.Build.props",
     "<Version>2.0.7</Version>", "نسخهٔ 2.0.7"),
]

ABSENCE_CHECKS = [
    ("src/Sahand.ClinicPro.API/Controllers/AdminController.cs",
     '[HttpPut("api/v1/settings")]', "قالب مسیر الحاقی قدیمی نباید باشد"),
    ("src/Sahand.ClinicPro.Desktop/Views/RolePermissionView.xaml",
     '<Button Grid.Row="0" Grid.Column="1"', "Button با استایل ToggleButton نباید باشد"),
    ("src/Sahand.ClinicPro.Desktop/Views/EmployeeFormWindow.xaml",
     'FontFamily="pack://application:,,,/Assets/Fonts/#Vazirmatn"', "فونت هاردکدشده نباید باشد"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Clinical/PrescriptionTemplateConfiguration.cs",
     'x.Items).HasMaxLength(200)', "سقف ۲۰۰ نویسه‌ای ستون Items نباید باشد"),
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
    if p.startswith("src/Sahand.ClinicPro.Desktop/Helpers/") or p.startswith("src/Sahand.ClinicPro.Desktop/Converters/"):
        return "راهنماها و مبدل‌ها (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/"):
        return "هستهٔ برنامهٔ دسکتاپ"
    if p.startswith("src/Sahand.ClinicPro.API/"):
        return "لایهٔ API (سرور محلی)"
    if p.startswith("src/Sahand.ClinicPro.Application/"):
        return "لایهٔ کاربرد (Application)"
    if p.startswith("src/Sahand.ClinicPro.Infrastructure/"):
        return "لایهٔ زیرساخت (Infrastructure)"
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
<title>راهنمای بستهٔ آپدیت — سامانهٔ درمانگاه سهند ۲.۰.۷</title>
<style>""" + CSS + """</style>
</head>
<body>
<div class="wrap">

<header class="hero">
<h1>📦 بستهٔ آپدیت نسخهٔ ۲.۰.۶ ← ۲.۰.۷</h1>
<p>سامانهٔ جامع درمانگاه سهند — فقط فایل‌های تغییرکردهٔ این رلیز، با همان ساختار پوشه‌ها</p>
<span class="badge">__TOTAL__ فایل در این بسته</span>
<span class="badge">تاریخ انتشار: ۸ اکتبر ۲۰۲۶</span>
</header>

<h2>۱) این بسته چیست؟</h2>
<div class="card">
<p>این بسته فقط <b>فایل‌های تغییرکرده از نسخهٔ ۲.۰.۶ تا ۲.۰.۷</b> را دارد — با همان ساختار پوشه‌ای مخزن.
اگر نسخهٔ فعلی شما ۲.۰.۶ است (پانویس پنجرهٔ ورود «نسخه ۲.۰.۶» را نشان می‌دهد)، همین بسته کافی است؛
برای نسخه‌های قدیمی‌تر اول بسته‌های قبلی را به ترتیب نصب کنید.</p>
</div>

<div class="card note">
<p>💡 مثل همیشه <b>همهٔ فایل‌های داخل بسته را با هم</b> جایگزین کنید (۶ قدم پایین) و ویژوال استودیو و سرور را قبل از جایگزینی <b>کاملاً ببندید</b>.</p>
</div>

<h2>۲) چه چیزهایی رفع شد؟ (گزارش چهاردهم شما — همهٔ ۱۸ مورد)</h2>
<div class="card">
<table>
<tr><th style="width:34%">موضوع</th><th>توضیح</th></tr>
<tr><td>🔌 <b>خطای 404 روی کلیک تم</b></td>
<td>ریشهٔ فنی پیدا شد: ۱۸ مسیر API در سه کنترلر (تنظیمات/آرشیو/پاراکلینیک) به‌جای مسیر مطلق، به مسیر پایهٔ کنترلر «الحاق» می‌شدند (آدرس واقعی مثل <code>api/v1/Admin/api/v1/settings</code> می‌شد!). همین باگ باعث خطای «خطای سرور - کد 404» هنگام انتخاب تم، «ذخیرهٔ برخی تنظیمات ناموفق بود» در بکاپ خودکار و کارنکردن عملکردهای پاراکلینیک (تصویربرداری/دندان/چشم/فیزیوتراپی) بود — همه یکجا رفع شد</td></tr>
<tr><td>👥 <b>مشخصات بیمار به فرم‌ها نمی‌رسید</b></td>
<td>سه ریشهٔ هم‌زمان: (۱) پاسخ <code>patients/{id}</code> در پاکت استاندارد پیچیده می‌شود و فرم‌ها مستقیم روی DTO نگاشت می‌شدند → مشخصات خالی؛ (۲) کلیک روی دکمهٔ داخل سطر جدول در WPF لزوماً ردیف را انتخاب نمی‌کند؛ (۳) فرم نوبت با فهرست پزشکان خالی باز می‌شد. حالا ویرایش پر باز می‌شود، فرم نوبت با بیمار و پزشکان از پیش‌آماده باز می‌شود و پروندهٔ بیمار مشخصات را نشان می‌دهد</td></tr>
<tr><td>🧑‍⚕️ <b>منوی پرسنل صفحهٔ تنظیمات را باز می‌کرد</b></td>
<td>صفحهٔ مستقل «مدیریت پرسنل» ساخته شد — آیتم منوی پرسنل مستقیم زبانهٔ پرسنل را با همان عنوان باز می‌کند؛ تنظیمات هم آیتم منوی خودش را دارد</td></tr>
<tr><td>💥 <b>کرش صفحهٔ نقش‌ها</b></td>
<td>دو دکمه با استایل مخصوص ToggleButton ساخته شده بودند که نوعشان Button بود → خطای XamlParseException. درست شد و کل برنامه برای الگوی مشابه اسکن شد</td></tr>
<tr><td>🎨 <b>فونت و اندازهٔ فونت کار نمی‌کرد</b></td>
<td>فونت در ۶۸ فایل رابط کاربری «هاردکد» شده بود؛ حذف شد و به منبع پویای سراسری وصل شد — حالا تعویض فونت (وزیرمتن/Tahoma/Segoe UI) و سایز ۱۱ تا ۱۸ بی‌درنگ روی همهٔ پنجره‌ها اثر می‌کند و در اجرای بعدی هم برقرار است</td></tr>
<tr><td>✨ <b>تم فقط تعویض رنگ نباشد</b></td>
<td>هر پوسته علاوه بر رنگ، شعاع گوشهٔ کارت‌ها (۴ تا ۱۸) و چگالی (استاندارد/فشرده) اختصاصی دارد — تعویض تم چهرهٔ همهٔ عناصر (کارت‌ها/پنل‌ها/دکمه‌ها) را عوض می‌کند. روی کارت هر تم در تنظیمات، مشخصاتش نوشته شده</td></tr>
<tr><td>🔀 <b>راست‌چین‌شدن فرم ثبت پرسنل</b></td>
<td>هشت پنجرهٔ فرم بدون راست‌چین‌بودن پیدا شد (پرسنل/نوبت/سفارش آزمایش/نتیجهٔ آزمایش/پرداخت/جزئیات نسخه/پذیرش/ورود کالا) — همه راست‌چین شدند</td></tr>
<tr><td>💊 <b>تعداد کل خودکار در نسخه</b></td>
<td>با هر تغییر دوز/تعداد مصرف/مدت، «تعداد کل» همان لحظه از روی فرمول دوز × مصرف روزانه × مدت بازمحاسبه می‌شود</td></tr>
<tr><td>💾 <b>خطای ذخیره تمپلیت نسخه</b></td>
<td>ستون JSON اقلام در دیتابیس فقط ۲۰۰ نویسه بود و JSON نسخه سرریز می‌شد → «خطای غیرمنتظره در سرور». ستون به نامحدود ارتقا یافت؛ <b>مهاجرت دیتابیس حالا هنگام راه‌اندازی سرور خودکار اعمال می‌شود</b> — فقط فایل‌ها را جایگزین کنید و سرور را روشن کنید</td></tr>
<tr><td>⚗️ <b>تداخل‌ها داخل برنامه</b></td>
<td>کاتالوگ کامل تداخل‌های دارویی داخل برنامه کش می‌شود (فایل محلی در %AppData%) و بررسی اقلام حتی در قطعی شبکه از همین کش انجام می‌شود؛ دکمهٔ «بروزرسانی تداخل‌ها» کاتالوگ سرور را دانلود و تداخل‌های جدید را اضافه می‌کند</td></tr>
<tr><td>📋 <b>فهرست کامل داروها + بروزرسانی</b></td>
<td>دکمهٔ «بروزرسانی فهرست داروها» داروهای جدید کاتالوگ مرجع را به دیتابیس سرور می‌افزاید (پیش‌تر فقط روی دیتابیس خالی کار می‌کرد) + ۹۰ ردیف جدید (۷۰ مولکول پرکاربرد غایب مثل آسکلوفناک، اتوریکوکسیب، تریمتازیدین، ممانتین، ویلداگلیپتین، آلندرونات و…) به کاتالوگ اضافه شد</td></tr>
<tr><td>🪪 <b>اعتبارسنجی زندهٔ کد ملی</b></td>
<td>هنگام تایپ، زیر فیلد بازخورد مرحله‌ای نمایش داده می‌شود: «X رقم دیگر» (زرد) → «فقط رقم» (قرمز) → «معتبر» (سبز) یا «رقم کنترلی غلط» (قرمز) — در فرم بیمار و فرم پرسنل؛ ارقام فارسی هم پذیرفته می‌شود</td></tr>
<tr><td>⚙️ <b>تنظیمات بکاپ خودکار ذخیره نمی‌شد</b></td>
<td>همان خطای 404 مسیرها بود که رفع شد؛ به‌علاوه باگ مسیر پیش‌فرض بکاپ (دابل‌بک‌اسلش) هم درست شد</td></tr>
<tr><td>☁️ <b>جای فعال‌سازی پشتیبان ابری نبود</b></td>
<td>فرم کامل «فعال‌سازی و تنظیم پشتیبان ابری» در صفحهٔ بکاپ اضافه شد: انتخاب ارائه‌دهنده (سازگار S3 مثل آبجکت‌استوریج آروان/آبرار یا پوشهٔ محلی/شبکه) + آدرس و سطل و کلیدها + رمزنگاری AES-256 + بارگذاری خودکار + دکمهٔ ذخیره و تست اتصال. کلیدهای رمزی هرگز به برنامه برنمی‌گردند — کادر خالی یعنی حفظ مقدار قبلی</td></tr>
<tr><td>💰 <b>جداکنندهٔ ۳ رقمی مبالغ</b></td>
<td>همهٔ نمایش‌های مبلغ (۱۱ صفحه از صندوق و پذیرش تا حقوق و دستمزد و گزارش‌ها) با جداکنندهٔ هزارگان یکدست شد؛ کادرهای ورود مبلغ (تخفیف/دریافتی فاکتور/حقوق پایه/پرداخت) هم هنگام تایپ زنده جداکننده می‌گیرند و ارقام فارسی را هم می‌پذیرند</td></tr>
<tr><td>🪙 <b>واحد پولی ریال/تومان</b></td>
<td>در تنظیمات ← تم و ظاهر قابل تعویض است؛ فقط جنبهٔ نمایشی دارد (در حالت تومان مبالغ بر ۱۰ تقسیم و واحد «تومان» نوشته می‌شود) و محاسبات و ذخیره‌سازی همه‌جا ریالی می‌مانند</td></tr>
<tr><td>👨‍⚕️ <b>لیست پزشکان نمایش داده نمی‌شد</b></td>
<td>فرم نوبت که از صفحهٔ بیماران باز می‌شد، فهرست پزشکان را خودش نمی‌گرفت و خالی می‌ماند — حالا خودش بارگذاری می‌کند؛ مشخصات بیمار هم از همان ردیف تزریق می‌شود</td></tr>
<tr><td>🩺 <b>دکمه‌های عملیاتی ناقص پزشکان</b></td>
<td>عرض ستون عملیات زیاد شد — چهار دکمهٔ ویرایش/آمار عملکرد/برنامه کاری/کارانه دیگر بریده نمی‌شوند؛ دستورها هم مستقیم ردیف خودشان را می‌گیرند</td></tr>
</table>
</div>

<h2>۳) قبل از شروع — دو نکتهٔ مهم</h2>
<div class="card dangerbox">
<p>⚠️ <b>این رلیز هم فایل‌های سرور دارد</b> (پوشه‌های <code>src/Sahand.ClinicPro.API</code> و <code>Application</code> و <code>Infrastructure</code>). اگر سرور محلی را با سورس اجرا می‌کنید، بعد از جایگزینی، سرور را یک‌بار ری‌استارت کنید تا <b>مهاجرت دیتابیس به‌صورت خودکار اعمال شود</b> (ارتقای ستون تمپلیت نسخه) و داروهای جدید کاتالوگ هم به‌صورت خودکار بذر شوند.</p>
</div>

<h2>۴) روش نصب — ۶ قدم ساده</h2>
<div class="step"><div class="stepnum">۱</div><div class="stepbody">
<b>بستن برنامه:</b> سامانه دسکتاپ، ویژوال استودیو و سرور محلی (اگر اجراست) را کاملاً ببندید تا فایل‌ها قفل نباشند.
</div></div>
<div class="step"><div class="stepnum">۲</div><div class="stepbody">
<b>پشتیبان‌گیری:</b> از پوشهٔ کامل پروژه و دیتابیس یک نسخهٔ پشتیبان تهیه کنید (همیشه عادت خوبی است).
</div></div>
<div class="step"><div class="stepnum">۳</div><div class="stepbody">
<b>بازکردن بسته:</b> فایل ZIP را باز کنید — داخلش همان ساختار پوشه‌های <code>src/…</code> است.
</div></div>
<div class="step"><div class="stepnum">۴</div><div class="stepbody">
<b>جایگزینی:</b> <b>همهٔ پوشه‌ها را با هم</b> روی ریشهٔ مخزن پروژه کپی و Replace کنید. فایل‌های راهنما (<code>Update-Guide-2.0.7.html</code> و <code>changed-files.txt</code>) را کپی نکنید.
</div></div>
<div class="step"><div class="stepnum">۵</div><div class="stepbody">
<b>بازسازی:</b> راه‌حل را در ویژوال استودیو باز و <span class="kbd">Rebuild Solution</span> بزنید.
</div></div>
<div class="step"><div class="stepnum">۶</div><div class="stepbody">
<b>اجرا:</b> سرور را روشن کنید (مهاجرت‌ها خودکار اعمال می‌شوند) بعد برنامهٔ دسکتاپ را اجرا کنید — پانویس ورود باید «نسخه ۲.۰.۷» را نشان دهد.
</div></div>

<h2>۵) بعد از نصب — تنظیمات پیشنهادی</h2>
<div class="card okbox">
<ul>
<li>🎨 <b>تم و ظاهر:</b> تنظیمات ← تم و ظاهر — فونت، سایز پایه، پوسته (گوشه/چگالی هر پوسته روی کارتش نوشته شده) و واحد پول نمایشی (ریال/تومان) را انتخاب کنید؛ همه‌چیز بی‌درنگ اعمال و ذخیره می‌شود</li>
<li>☁️ <b>پشتیبان ابری:</b> بکاپ و بازیابی ← بخش «فعال‌سازی و تنظیم پشتیبان ابری» — ارائه‌دهنده و کلیدها را وارد و ذخیره کنید، بعد «تست اتصال» بزنید</li>
<li>📋 <b>فهرست داروها:</b> نسخه‌نویسی ← «بروزرسانی فهرست داروها» (داروهای جدید اضافه می‌شوند) و «بروزرسانی تداخل‌ها» (کاتالوگ تداخل داخل برنامه نو می‌شود)</li>
</ul>
</div>

<h2>۶) فهرست کامل فایل‌های این بسته</h2>
<div class="card">
<div class="flist">__FILES__
</div>
<p style="margin-top:8px">راهنمای A = فایل جدید، M = فایل ویرایش‌شده.</p>
</div>

<footer>سامانهٔ جامع مدیریت درمانگاه سهند — بستهٔ آپدیت ۲.۰.۶ به ۲.۰.۷ • ۸ اکتبر ۲۰۲۶</footer>
</div>
</body>
</html>
"""
    html = html.replace("__TOTAL__", str(total))
    html = html.replace("__FILES__", "\n".join(groups_lines))
    return html


def main():
    # ۱) گارد انسجام مثبت
    errors = []
    for path, needle, desc in COHERENCE_CHECKS:
        text = (REPO / path).read_text(encoding="utf-8-sig")
        if needle not in text:
            errors.append(f"MISSING in {path}: {needle} ({desc})")
    for path, needle, desc in ABSENCE_CHECKS:
        text = (REPO / path).read_text(encoding="utf-8-sig")
        if needle in text:
            errors.append(f"PRESENT in {path}: {needle} ({desc})")
    if errors:
        print("❌ COHERENCE GUARD FAILED:")
        for e in errors:
            print("  -", e)
        raise SystemExit(1)
    print("✅ گارد انسجام مثبت/منفی پاس شد")

    # ۲) فهرست فایل‌ها
    files = changed_files()
    total = len(files)
    print(f"changed files: {total}")

    # ۳) راهنما + مانیفست
    OUT.parent.mkdir(parents=True, exist_ok=True)
    guide = build_guide(total, files).encode("utf-8")
    manifest = "\n".join(
        f"{status}\t{path}" for status, path in files
    ).encode("utf-8")

    # ۴) ساخت ZIP
    with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED) as zf:
        for status, path in files:
            zf.write(REPO / path, path)
        zf.writestr(GUIDE_NAME, guide)
        zf.writestr(MANIFEST_NAME, manifest)

    size_kb = OUT.stat().st_size // 1024
    print(f"✅ بستهٔ آپدیت ساخته شد: {OUT} ({size_kb} KB — {total} فایل + راهنما + مانیفست)")


if __name__ == "__main__":
    main()
