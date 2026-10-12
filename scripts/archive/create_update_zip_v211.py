#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت تجمعی v2.6.1 → v2.11.0 — چرخهٔ چهل‌وهفتم (گزارش مالک).

درس قطعی چرخه‌های ۳۵/۴۵/۴۶ («فایل جاافتاده = باگ رفع‌نشده در ایستگاه» و
تحلیل پیکسلی تصاویر مالک که نشان داد ایستگاه با XAML های پیش از چرخهٔ ۴۵
کار می‌کند هرچند نسخه را به‌روز می‌داند): بسته از ابتدا «تجمعی» است — همهٔ
فایل‌های تغییرکرده از تگ v2.6.1 تا HEAD — تا هر ایستگاهی با یک بسته به
وضعیت همگنِ کامل برسد؛ حتی اگر بسته‌های میانی را نگرفته باشد.

گاردهای انسجام: نشانه‌های سلامت رفع‌های کلیدی این چرخه باید در سورس موجود
باشند و نشانه‌های باگ‌ها نباشند، وگرنه اسکریپت متوقف می‌شود.
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
TAG = "v2.6.1"          # تجمعی — از آخرین بستر پایدار
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-Cumulative-2.6.1-to-2.11.0.zip")
GUIDE_NAME = "Update-Guide-2.11.0.html"
MANIFEST_NAME = "changed-files.txt"

# ─── گارد انسجام مثبت — نشانه‌های سلامت رفع‌های چرخهٔ ۴۷ ───
COHERENCE_CHECKS = [
    # مبدل ارقام فارسی دوسویه (ظرفیت + مبالغ سند)
    ("src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs",
     "Money.PersianDigitsToLatin(s)", "برگشت دوسویهٔ ارقام فارسی"),
    # سود و زیان — بدون علامت منفی
    ("src/Sahand.ClinicPro.Application/Features/Accounting/AccountingFeatures.cs",
     "return natureSign * accountLines.Sum(l => l.Debit - l.Credit);", "فرمول درست ماندهٔ سود و زیان"),
    # اندپوینت جزئیات سند
    ("src/Sahand.ClinicPro.API/Controllers/AccountingController.cs",
     'HttpGet("entries/{entryId:long}")', "اندپوینت جزئیات سند"),
    # زیرمنبع لاگ عملیات
    ("src/Sahand.ClinicPro.API/Middleware/AuditLogMiddleware.cs",
     "TryCaptureSubResourceOldValues", "مقادیر قبلی زیرمنبع"),
    # کمبوباکس با فوکوس باز می‌شود
    ("src/Sahand.ClinicPro.Desktop/Controls/ComboBoxSearch.cs",
     "OnComboGotFocus", "گشایش با فوکوس"),
    # صفحهٔ مستقل شیفت‌ها
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ShiftsViewModel.cs",
     "class ShiftsViewModel", "ViewModel مستقل شیفت‌ها"),
    ("src/Sahand.ClinicPro.Desktop/Views/ShiftsView.xaml",
     "ShiftsView", "نمای مستقل شیفت‌ها"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'DataType="{x:Type vm:ShiftsViewModel}"', "قالب نگاشت شیفت‌ها"),
    ("src/Sahand.ClinicPro.Desktop/Helpers/PageCatalog.cs",
     '"shifts", "مدیریت شیفت‌ها"', "کاتالوگ شیفت‌ها"),
    # حضور و غیاب بدون تب شیفت (گارد مثبتش در EXTRA_CHECKS — کامنت انتقال)
    ("src/Sahand.ClinicPro.Desktop/Views/AttendanceView.xaml",
     'Header="📝 اصلاح کارت"', "کادر اصلاح کارت پس از حذف تب شیفت"),
    # توست در ردیف میزکار
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'ItemsSource="{Binding Toasts}"', "میزبان توست موجود"),
    # تولتیپ قطعی
    ("src/Sahand.ClinicPro.Desktop/App.xaml",
     'Background="#263238"', "زمینهٔ قطعی تولتیپ"),
    # دکمهٔ فراخوان با کف ارتفاع
    ("src/Sahand.ClinicPro.Desktop/Views/VisitsView.xaml",
     'MinWidth="230" MinHeight="46"', "دکمهٔ فراخوان مقاوم"),
    # کاشی ضدگلوله + نشان نسخه
    ("src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml",
     'Width="120" Height="116"', "ارتفاع قطعی کاشی میانبر"),
    ("src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml",
     "AppInfo.VersionText", "نشان نسخهٔ داشبورد"),
    # بیمهٔ پذیرش دیدنی
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "بیمهٔ پروندهٔ بیمار روی", "توست تأیید بیمهٔ پرونده"),
    # دابل‌کلیک سند
    ("src/Sahand.ClinicPro.Desktop/Views/AccountingView.xaml",
     "ShowEntryDetailsCommand", "اتصال دابل‌کلیک جزئیات سند"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/AccountingViewModel.cs",
     "OnLinePropertyChanged", "اتصال جمع‌های سند به ویرایش سطرها"),
    # نسخه و CHANGELOG
    ("Directory.Build.props",
     "<Version>2.11.0</Version>", "نسخهٔ 2.11.0"),
    ("CHANGELOG.md",
     "[v2.11.0]", "بخش CHANGELOG"),
]

# ─── گارد انسجام منفی — نشانه‌های باگ نباید باشند ───
ABSENCE_CHECKS = [
    # مبدل معیوب (برگشت بی‌اثر) — الگوی دقیق کلاس
    ("src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs",
     "/// <summary>برگشت بی‌اثر — ورودی‌ها جداگانه ارقام فارسی را می‌پذیرند</summary>", "کامنت مبدل تک‌جهتهٔ قدیمی"),
    # سود و زیان معکوس
    ("src/Sahand.ClinicPro.Application/Features/Accounting/AccountingFeatures.cs",
     "return -natureSign *", "علامت معکوس مانده"),
    # پیام خطای بدون زنجیره در ثبت شیفت
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ShiftsViewModel.cs",
     'response?.Message ?? "ثبت شیفت ناموفق بود"', "پیام عمومی بدون LastError"),
    # تولتیپ تابع تم (ریشهٔ ناخوانایی)
    ("src/Sahand.ClinicPro.Desktop/App.xaml",
     "MaterialDesign.Brush.ToolTip.Background", "زمینهٔ تولتیپ از قلم تم"),
    # توست در گرید ریشه با حاشیهٔ قدیمی
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'Grid.Column="1"\n                      VerticalAlignment="Bottom"\n                      HorizontalAlignment="Center"\n                      Margin="0,0,0,64"', "چیدمان قدیمی توست"),
    # رادیوی ماهیت با مبدل غلط
    ("src/Sahand.ClinicPro.Desktop/Views/AccountingView.xaml",
     "IsChecked=\"{Binding NewNatureIsCredit, Converter={StaticResource BoolToVisibility}}\"", "رادیوی ماهیت با مبدل غلط"),
    # تب شیفت باید از حضور و غیاب حذف شده باشد
    ("src/Sahand.ClinicPro.Desktop/Views/AttendanceView.xaml",
     'Header="⏰ شیفت‌ها"', "تب شیفت در حضور و غیاب (منتقل شد)"),
]

# یافتن نشانهٔ مثبتِ «تب شیفت حذف شد» (کامنت جایگزین) — گارد ویژه
EXTRA_CHECKS = [
    ("src/Sahand.ClinicPro.Desktop/Views/AttendanceView.xaml",
     "تب «شیفت‌ها» از این صفحه به صفحهٔ مستقل «مدیریت شیفت‌ها»", "کامنت انتقال تب شیفت"),
]


def verify():
    errors = []
    for path, needle, why in COHERENCE_CHECKS + EXTRA_CHECKS:
        text = (REPO / path).read_text(encoding="utf-8")
        if needle not in text:
            errors.append(f"✗ گارد مثبت شکست خورد: {path} ← «{needle}» ({why})")
    for path, needle, why in ABSENCE_CHECKS:
        text = (REPO / path).read_text(encoding="utf-8")
        if needle in text:
            errors.append(f"✗ گارد منفی شکست خورد: {path} ← «{needle}» نباید باشد ({why})")
    if errors:
        print("\n".join(errors))
        raise SystemExit(1)
    print(f"✓ همهٔ {len(COHERENCE_CHECKS) + len(EXTRA_CHECKS)} گارد مثبت و {len(ABSENCE_CHECKS)} گارد منفی پاس شدند")


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
        status, rel = parts[0], parts[-1]
        # فقط فایل‌های سورس — خروجی‌های بیلد و دارایی‌های کش‌شده نه
        if not rel.startswith(("src/", "Directory.Build.props", "CHANGELOG.md", "scripts/")):
            continue
        files.append((status, rel))
    return files


GUIDE = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>راهنمای به‌روزرسانی تجمعی → ۲.۱۱.۰ — درمانگاه سهند</title>
<style>
:root{--ink:#1c2b3a;--mut:#5a6b7d;--line:#dfe7ee;--brand:#1565C0;--brand2:#0D47A1;--bg:#f4f7fb;--card:#fff}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--ink);font-family:"Vazirmatn","Segoe UI",Tahoma,Arial,sans-serif;line-height:1.95;font-size:15px}
.wrap{max-width:880px;margin:0 auto;padding:24px 20px 60px}
header.hero{background:linear-gradient(135deg,#1565C0 0%,#0D47A1 100%);color:#fff;border-radius:16px;padding:26px;margin-bottom:24px}
header.hero h1{margin:0 0 6px;font-size:23px}
h2{font-size:18px;color:var(--brand2);border-right:5px solid var(--brand);padding-right:12px;margin:30px 0 10px}
.card{background:var(--card);border:1px solid var(--line);border-radius:12px;padding:16px 18px;margin:12px 0}
table{width:100%;border-collapse:collapse;font-size:13.5px}
th,td{border:1px solid var(--line);padding:7px 10px;text-align:right;vertical-align:top}
th{background:#eaf1fa;color:var(--brand2)}
ul{padding-right:22px;margin:8px 0}
li{margin:5px 0}
code{font-family:Consolas,monospace;background:#eef2f7;border-radius:5px;padding:1px 7px;direction:ltr;display:inline-block;font-size:12.5px}
.dangerbox{border-right:5px solid #b3372f;background:#fdf0ef;border-radius:10px;padding:12px 16px;margin:14px 0}
.okbox{border-right:5px solid #2E7D32;background:#eff8f3;border-radius:10px;padding:12px 16px;margin:14px 0}
.warnbox{border-right:5px solid #D97706;background:#fdf6ec;border-radius:10px;padding:12px 16px;margin:14px 0}
.step{display:flex;gap:12px;margin:13px 0;align-items:flex-start}
.stepnum{flex:0 0 32px;height:32px;border-radius:50%;background:var(--brand);color:#fff;display:flex;align-items:center;justify-content:center;font-weight:700}
footer{margin-top:36px;color:var(--mut);font-size:12.5px;text-align:center}
</style>
</head>
<body>
<div class="wrap">
<header class="hero">
<h1>📦 به‌روزرسانی تجمعی → ۲.۱۱.۰</h1>
<div>درمانگاه سهند — چرخهٔ چهل‌وهفتم (۱۲ گزارش مالک)</div>
</header>

<div class="okbox">این بسته <b>تجمعی</b> است: همهٔ فایل‌های تغییرکرده از نسخهٔ ۲.۶.۱ تا ۲.۱۱.۰ را دارد. حتی اگر بسته‌های میانی (۲.۷ تا ۲.۱۰) را نصب نکرده‌اید، همین یک بسته شما را به آخرین وضعیت می‌رساند. چرخهٔ ۴۷ شامل: صفحهٔ مستقل مدیریت شیفت‌ها، بازطراحی حرفه‌ای حسابداری، ریفعظرفیت برنامهٔ کاری، سود و زیان، لاگ عملیات، توست، تولتیپ، کمبوباکس‌ها و دکمهٔ فراخوان است.</div>

<div class="warnbox"><b>⭐ مهم‌ترین قدم این نصب — پاک‌سازی کش بیلد:</b> تحلیل تصاویر شما نشان داد رندر ایستگاه شما با فایل‌های قدیمی (پیش از نسخهٔ ۲.۹) است، هرچند شمارهٔ نسخه به‌روز است — یعنی فایل‌های جدید به بیلد نرسیده‌اند (کش BAML ویژوال استودیو). این بار حتماً مرحلهٔ ۴ (حذف bin/obj) را انجام دهید.</div>

<h2>🔧 مراحل نصب</h2>
<div class="step"><div class="stepnum">۱</div><div><b>بستن برنامه و ویژوال استودیو</b> — و اگر فایلی قفل بود، همهٔ فرایندهای <code>Sahand.ClinicPro</code> را در Task Manager پایان دهید.</div></div>
<div class="step"><div class="stepnum">۲</div><div><b>استخراج ZIP</b> — ساختار پوشه‌ها همان ساختار مخزن است.</div></div>
<div class="step"><div class="stepnum">۳</div><div><b>جایگزینی فایل‌ها</b> — پوشهٔ <code>src</code> و فایل‌های ریشه داخل ZIP را روی <code>E:\\Sahand_ClinicPro</code> کپی کنید (Replace).</div></div>
<div class="step"><div class="stepnum">۴</div><div><b>حذف خروجی‌های قدیمی (حیاتی!)</b> — PowerShell در پوشهٔ مخزن:<br><code>Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force</code><br>یا اجرای <code>Clean-VS-Cache.bat</code> از ریشهٔ مخزن.</div></div>
<div class="step"><div class="stepnum">۵</div><div><b>Rebuild Solution</b> — در Visual Studio با پیکربندی همیشگی. بیلد باید با <b>۰ خطا و ۰ هشدار</b> تمام شود (همین الان روی سورس همین بسته ساخته و آزموده شده).</div></div>
<div class="step"><div class="stepnum">۶</div><div><b>اجرای سرور API سپس برنامهٔ دسکتاپ</b> — این نسخه مهاجرت دیتابیس جدید ندارد؛ فقط سرور باید با فایل‌های جدید ری‌بیلد و ری‌استارت شود (تغییرات حسابداری و لاگ عملیات سمت سرور است).</div></div>

<h2>✅ چک‌لیست پس از نصب — مطمئن شوید بیلد تازه است</h2>
<div class="card">
<ul>
<li><b>نشان نسخه روی داشبورد:</b> در پنل «میانبرهای سریع من»، کنار عنوان، یک نشان آبی «نسخه ۲.۱۱.۰» باید دیده شود. <b>اگر این نشان نبود، بیلد شما کهنه است</b> — به مرحلهٔ ۴ برگردید.</li>
<li><b>کاشی‌های میانبر:</b> آیکون + عنوان کامل و خوانا (کارت‌های بلند سفید).</li>
<li><b>توست پیام‌ها:</b> کپسول سفید با سایه که کامل (بدون بریدگی پایین) بالای نوار وضعیت دیده می‌شود.</li>
<li><b>منوی کناری ← کادر درمان و پرسنل ← «مدیریت شیفت‌ها»:</b> صفحهٔ مستقل با کارت‌های شیفت و تخصیص‌ها. اگر خطای ثبت شیفت گرفتید، حالا پیام واقعی علت (مثلاً نبود مجوز attendance.shifts) نمایش داده می‌شود.</li>
<li><b>حسابداری ← سود/زیان:</b> صورت سود و زیان ساختاریافته؛ سود خالص سبز و زیان خالص قرمز.</li>
<li><b>حسابداری ← دفتر روزنامه:</b> دابل‌کلیک روی هر سند = پنجرهٔ جزئیات کامل با سطرها.</li>
<li><b>فرم پزشک ← برنامهٔ کاری:</b> در ستون ظرفیت عدد بنویسید و سلول را ترک کنید — مقدار باید بماند (با ارقام فارسی هم).</li>
<li><b>کمبوباکس‌ها:</b> کلیک یا فوکوس روی هر کمبوی جستجوشونده، فهرست را باز می‌کند.</li>
<li><b>پذیرش:</b> با انتخاب بیمار، توست تأیید بیمهٔ پرونده او را می‌بینید و کشوی بیمه خودکار تنظیم می‌شود.</li>
<li><b>لاگ عملیات:</b> ویرایش دسترسی‌های نقش ← مقدار قبلی = فهرست مجوزهای پیشین (نه آی‌دی/نام نقش).</li>
</ul>
</div>

<h2>🩺 اگر باز هم چیزی درست نشد</h2>
<div class="dangerbox">اگر پس از نصب، نشان نسخهٔ ۲.۱۱.۰ روی داشبورد <b>نبود</b>، بیلد ایستگاه از کش قدیمی می‌آید: ویژوال استودیو را ببندید، <code>bin</code> و <code>obj</code> همهٔ پروژه‌ها را دستی پاک کنید، سولوشن را باز کنید و Rebuild بزنید. اگر باز هم مشکل بود، اسکرین‌شات همان صفحه را بفرستید.</div>

<h2>🗒️ تغییر فایل‌ها</h2>
<p>فهرست کامل در <code>changed-files.txt</code> کنار همین راهنماست.</p>

<footer>درمانگاه سهند — سامانه جامع مدیریت درمانگاه • نسخهٔ ۲.۱۱.۰ • مهر ۱۴۰۵</footer>
</div>
</body>
</html>
"""


def main():
    verify()
    files = changed_files()
    if not files:
        raise SystemExit("هیچ فایل تغییرکرده‌ای یافت نشد!")
    OUT.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        manifest = []
        for status, rel in files:
            src = REPO / rel
            if not src.is_file():
                print(f"  ⚠ رد شد (وجود ندارد): {rel}")
                continue
            z.write(src, rel)
            manifest.append(f"{status}\t{rel}")
        z.writestr(GUIDE_NAME, GUIDE)
        z.writestr(MANIFEST_NAME, "\n".join(manifest) + "\n")
    size_kb = OUT.stat().st_size // 1024
    print(f"✓ بستهٔ تجمعی ساخته شد: {OUT}  ({size_kb} KB، {len(files)} فایل)")


if __name__ == "__main__":
    main()
