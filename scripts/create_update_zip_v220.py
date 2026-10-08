#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.1.0 → v2.2.0 — چرخه بیست‌وچهارم (گزارش هفدهم مالک).
فقط فایل‌های تغییرکردهٔ همین رلیز (تگ v2.1.0 تا HEAD) با همان ساختار پوشه‌ای
مخزن، به‌همراه راهنمای فارسی HTML و مانیفست متنی — الگوی چرخه‌های قبل.

گاردهای انسجام: نشانه‌های سلامت رفع‌های کلیدی این نسخه باید در سورس موجود
باشند و نشانه‌های باگ نباید باشند، وگرنه اسکریپت متوقف می‌شود.
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
TAG = "v2.1.0"
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.1.0-to-2.2.0.zip")
GUIDE_NAME = "Update-Guide-2.2.0.html"
MANIFEST_NAME = "changed-files.txt"

# ─── گارد انسجام مثبت — نشانه‌های سلامت رفع‌های این نسخه ───
COHERENCE_CHECKS = [
    # میدلور API — گارد پاسخ آغازشده + چالش JWT
    ("src/Sahand.ClinicPro.API/Middleware/ExceptionHandlingMiddleware.cs",
     "context.Response.HasStarted", "گارد پاسخ آغازشده"),
    ("src/Sahand.ClinicPro.Infrastructure/Identity/IdentitySetup.cs",
     "OnChallenge", "چالش ۴۰۱ فارسی"),
    # کرش گزارش‌ها
    ("src/Sahand.ClinicPro.Desktop/Views/ReportBrowserView.xaml.cs",
     "رفع کرش گزارش‌ها", "رفع FontFamily خالی"),
    # تصویربرداری — DataTrigger
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ParaclinicalViewModel.cs",
     "OnSelectedToothChanged", "پرچم IsSelected دندان"),
    ("src/Sahand.ClinicPro.Desktop/Views/ParaclinicalView.xaml",
     'DataTrigger Binding="{Binding IsSelected}"', "دتاتریگر ساده"),
    # TimePicker — جهت LTR
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml",
     'StackPanel Orientation="Horizontal" FlowDirection="LeftToRight"', "ردیف دیجیتال LTR"),
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml",
     'Border Grid.Column="2" FlowDirection="LeftToRight"', "بوم عقربه‌ای LTR"),
    # پروندهٔ بیمار — DTO هم‌نام سرور
    ("src/Sahand.ClinicPro.Desktop/ViewModels/PatientDetailViewModel.cs",
     "PrescriptionStatusText", "وضعیت عددی نسخه"),
    # لاگ عملیات
    ("src/Sahand.ClinicPro.Desktop/ViewModels/AuditLogViewModel.cs",
     "action=", "پارامتر فیلتر عملیات"),
    ("src/Sahand.ClinicPro.Application/Features/Settings/SettingsFeatures.cs",
     "string? Action = null", "فیلتر action سرور"),
    # کاربران — نام کامل + همگام‌سازی تفاوتی نقش‌ها
    ("src/Sahand.ClinicPro.Desktop/Views/UserFormWindow.xaml",
     "نام کامل *", "فیلد نام کامل"),
    ("src/Sahand.ClinicPro.API/Controllers/UsersController.cs",
     "SyncRolesAsync", "همگام‌سازی نقش‌ها"),
    ("src/Sahand.ClinicPro.API/Controllers/UsersController.cs",
     "ResolveEmployeeIdAsync", "اتصال پرسنل"),
    # پرسنل — سمت/دپارتمان
    ("src/Sahand.ClinicPro.Desktop/Views/EmployeeFormWindow.xaml",
     "سمت (قابل تایپ مقدار جدید)", "کمبوی سمت"),
    ("src/Sahand.ClinicPro.API/Controllers/LookupsController.cs",
     'HttpGet("positions")', "اندپوینت سمت‌ها"),
    # تعرفه به‌تفکیک بیمه
    ("src/Sahand.ClinicPro.Desktop/ViewModels/InsuranceViewModel.cs",
     "TariffTargetNames", "انتخابگر هدف تعرفه"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/BillingViewModel.cs",
     "OnInsuranceIndexChanged", "بارگذاری مجدد خدمات با بیمه"),
    # چاپگرها + قبض نوبت
    ("src/Sahand.ClinicPro.Desktop/Services/AppointmentTicketPrinter.cs",
     "AppointmentTicketPrinter", "چاپ قبض نوبت"),
    ("src/Sahand.ClinicPro.Desktop/Views/SettingsView.xaml",
     "چاپگر نوبت‌دهی", "چاپگر نوبت"),
    # پذیرش خدمت درمانی
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "ReceptionModes", "حالت‌های پذیرش"),
    ("src/Sahand.ClinicPro.Domain/Scheduling/Reception.cs",
     "public long? DoctorId", "پزشک اختیاری"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261009180000_Cycle24ReceptionServices.cs",
     "Cycle24ReceptionServices", "مهاجرت پذیرش خدمتی"),
    # ارقام فارسی fa-IR
    ("src/Sahand.ClinicPro.Desktop/App.xaml.cs",
     'XmlLanguage.GetLanguage("fa-IR")', "زبان fa-IR سراسری"),
    # جداکنندهٔ زنده — SetCurrentValue
    ("src/Sahand.ClinicPro.Desktop/Controls/MoneyInput.cs",
     "SetCurrentValue(NumericValueProperty", "حفظ اتصال داطمی"),
    # فونت وزیرمتن — FontAssets
    ("src/Sahand.ClinicPro.Desktop/Helpers/FontAssets.cs",
     "new Uri(\"pack://application:,,,/\")", "سازندهٔ دوآرگومانی فونت"),
    # زوم سراسری
    ("src/Sahand.ClinicPro.Desktop/App.xaml.cs",
     "App.FontScaleTransform", "اتصال مقیاس زوم"),
    # پریست تم + آیکون رنگی
    ("src/Sahand.ClinicPro.Desktop/ViewModels/SettingsViewModel.cs",
     "SaveThemePreset", "ذخیرهٔ پریست"),
    ("src/Sahand.ClinicPro.Desktop/Converters/IconTintConverter.cs",
     "IconTintConverter", "مبدل رنگ آیکون"),
    # لوگو + اسپلش
    ("src/Sahand.ClinicPro.Desktop/Views/SplashWindow.xaml",
     "SplashWindow", "پنجرهٔ اسپلش"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     "ClinicPro-256.png", "لوگوی سایدبار"),
    ("src/Sahand.ClinicPro.Desktop/Views/LoginWindow.xaml",
     "ClinicPro-512.png", "لوگوی لاگین"),
    # ESC سراسری
    ("src/Sahand.ClinicPro.Desktop/App.xaml.cs",
     "Key.Escape", "کلید ESC"),
    # پشتیبان — استخراج امن + فرمت + حذف
    ("src/Sahand.ClinicPro.Infrastructure/Services/Backup/BackupManager.cs",
     "TryGetSqlServerBackupDirectoryAsync", "پوشهٔ امن SQL"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/BackupViewModel.cs",
     "BackupFormats", "انتخاب فرمت پشتیبان"),
    ("src/Sahand.ClinicPro.API/Controllers/BackupController.cs",
     "delete-file", "حذف فایل پشتیبان"),
    # قالب چاپ — اندازه‌ها + متغیرها + بذر
    ("src/Sahand.ClinicPro.Desktop/Views/ComplementaryView.xaml",
     "فیش حرارتی ۸۰ میلی‌متر", "کاغذ حرارتی"),
    ("src/Sahand.ClinicPro.Desktop/Converters/VariableLabelConverter.cs",
     "VariableLabelConverter", "برچسب فارسی متغیرها"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/PrintTemplateSeedData.cs",
     "فاکتور مدرن — نوار رنگی", "قالب‌های مدرن"),
    # لایسنس — اطلاعات درمانگاه
    ("src/LicenseServer.Php/inc/functions.php",
     "'clinicName'", "اطلاعات درمانگاه در پاسخ پورتال"),
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "SyncClinicInfoFromLicenseAsync", "همگام‌سازی اطلاعات درمانگاه"),
    # دارو/تداخل
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/MedicationSeedDataSupplement4.cs",
     "Supplement4Rows", "مکمل چهارم داروها"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/DrugInteractionSeedDataSupplement3.cs",
     "Supplement3Rows", "مکمل سوم تداخل‌ها"),
    # سند بررسی فرم‌ها
    ("docs/forms-vs-screenshots-report.md",
     "هم‌ترازی بصری", "سند بررسی فرم‌ها"),
    # نسخه
    ("Directory.Build.props",
     "<Version>2.2.0</Version>", "نسخهٔ 2.2.0"),
    ("CHANGELOG.md",
     "[v2.2.0]", "بخش CHANGELOG"),
]

# ─── گارد انسجام منفی — نشانه‌های باگ نباید باشند ───
ABSENCE_CHECKS = [
    ("src/Sahand.ClinicPro.Desktop/Views/ReportBrowserView.xaml.cs",
     "FontFamily = i == 0 ? null", "مقدار null فونت ستون"),
    ("src/Sahand.ClinicPro.Desktop/Views/ParaclinicalView.xaml",
     'Value="{Binding DataContext.SelectedTooth', "Binding روی Value دتاتریگر"),
    ("src/Sahand.ClinicPro.Desktop/Controls/MoneyInput.cs",
     "box.SetValue(NumericValueProperty", "SetValue مخرب اتصال"),
    ("src/Sahand.ClinicPro.Desktop/Views/PatientFormWindow.xaml",
     '<ComboBox Grid.Column="1"\n                                          materialDesign:HintAssist.Hint="گروه خونی"', "کمبوی خون در ستون فاصله"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/UserFormViewModel.cs",
     "email = Email, phone = Phone, roleIds,", "بدون fullName"),
]


def verify():
    errors = []
    for path, needle, why in COHERENCE_CHECKS:
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
    print(f"✓ همهٔ {len(COHERENCE_CHECKS)} گارد مثبت و {len(ABSENCE_CHECKS)} گارد منفی پاس شدند")


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


GUIDE = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>راهنمای به‌روزرسانی ۲.۱.۰ → ۲.۲.۰ — درمانگاه سهند</title>
<style>
:root{--ink:#1c2b3a;--mut:#5a6b7d;--line:#dfe7ee;--brand:#1565C0;--brand2:#0D47A1;--bg:#f4f7fb;--card:#fff}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--ink);font-family:"Vazirmatn","Segoe UI",Tahoma,Arial,sans-serif;line-height:1.95;font-size:15px}
.wrap{max-width:860px;margin:0 auto;padding:24px 20px 60px}
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
.step{display:flex;gap:12px;margin:13px 0;align-items:flex-start}
.stepnum{flex:0 0 32px;height:32px;border-radius:50%;background:var(--brand);color:#fff;display:flex;align-items:center;justify-content:center;font-weight:700}
.flist{font-family:Consolas,monospace;font-size:12px;direction:ltr;text-align:left;color:#33475f;background:#f7fafd;border:1px solid var(--line);border-radius:8px;padding:10px;max-height:340px;overflow:auto}
footer{margin-top:36px;color:var(--mut);font-size:12.5px;text-align:center}
</style>
</head>
<body>
<div class="wrap">
<header class="hero">
<h1>📦 به‌روزرسانی ۲.۱.۰ → ۲.۲.۰</h1>
<div>درمانگاه سهند — چرخه بیست‌وچهارم (گزارش هفدهم مالک)</div>
</header>

<div class="okbox">این نسخه شامل <b>۴۵+ مورد</b> از گزارش شماست: رفع آبشار خطای سرور، کرش گزارش‌ها/تصویربرداری/پروندهٔ بیمار، انتخابگر ساعت وارونه، زوم و چیدمان چاپ، لاگ عملیات، نام کامل کاربر و ذخیرهٔ نقش‌ها، سمت/دپارتمان پرسنل، تعرفه به‌تفکیک بیمه، چاپگر نوبت‌دهی با قبض فیش حرارتی، <b>پذیرش خدمت درمانی (تزریقات/پانسمان/آزمایش/عکس)</b>، ارقام فارسی کامل، تعمیر قطعی جداکنندهٔ زندهٔ مبالغ، فونت وزیرمتن، زوم سراسری اندازهٔ فونت، پریست‌های شخصی تم، آیکون‌های رنگی، لوگوی رسمی بزرگ، Splash انیمیشنی، ESC فرم‌ها، پشتیبان ZIP/BAK با حذف دستی، ۱۱ قالب چاپ جدید با کاغذ حرارتی، داروها و تداخل‌های جدید، اطلاعات درمانگاه از سرور لایسنس و <b>سند بررسی تفاوت فرم‌ها با اسکرین‌شات‌ها</b>.</div>

<h2>⚠️ پیش‌نیاز مهم — مهاجرت دیتابیس</h2>
<div class="dangerbox">این نسخه شامل مهاجرت <code>Cycle24ReceptionServices</code> است (پزشک پذیرش اختیاری + ستون‌های خدمت درمانی) که هنگام اجرای سرور API <b>خودکار و idempotent</b> اعمال می‌شود؛ فقط کافی است سرور یک‌بار پس از جایگزینی فایل‌ها اجرا شود. همچنین سرور لایسنس (پوشهٔ LicenseServer.Php روی هاست) را هم به‌روزرسانی کنید تا اطلاعات درمانگاه از پنل لایسنس بیاید.</div>

<h2>🔧 مراحل نصب</h2>
<div class="step"><div class="stepnum">۱</div><div><b>بستن برنامه و ویژوال استودیو</b> — مطمئن شوید برنامهٔ دسکتاپ و سرور (و VS) بسته‌اند. اگر فایل‌ها قفل بودند، در Task Manager همهٔ فرایندهای <code>Sahand.ClinicPro</code> را End Task کنید.</div></div>
<div class="step"><div class="stepnum">۲</div><div><b>استخراج ZIP</b> — فایل ZIP را باز کنید؛ ساختار پوشه‌ها همان ساختار مخزن است.</div></div>
<div class="step"><div class="stepnum">۳</div><div><b>جایگزینی فایل‌ها</b> — پوشهٔ <code>src</code> و <code>docs</code> داخل ZIP را روی همان پوشه‌های <code>E:\\Sahand_ClinicPro</code> کپی کنید (Replace). فقط فایل‌های تغییرکرده در ZIP هستند.</div></div>
<div class="step"><div class="stepnum">۴</div><div><b>به‌روزرسانی سرور لایسنس</b> — اگر سرور لایسنس PHP روی هاست دارید، فایل <code>inc/functions.php</code> جدید را هم آپلود کنید (ستون clinic_phone خودکار ساخته می‌شود).</div></div>
<div class="step"><div class="stepnum">۵</div><div><b>حذف خروجی‌های قدیمی</b> — PowerShell در <code>E:\\Sahand_ClinicPro</code>:<br><code>Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force</code></div></div>
<div class="step"><div class="stepnum">۶</div><div><b>ساخت مجدد</b> — در Visual Studio: <b>Rebuild Solution</b>. بیلد باید با ۰ خطا و ۰ هشدار تمام شود.</div></div>
<div class="step"><div class="stepnum">۷</div><div><b>اجرای سرور و برنامه</b> — سرور API را اجرا کنید تا مهاجرت و بذرهای جدید (قالب‌های چاپ مدرن، ۲۰ دارو، ۵۰ تداخل) اعمال شوند، سپس برنامهٔ دسکتاپ. هنگام اولین بررسی لایسنس، اطلاعات درمانگاه از پنل لایسنس در تنظیمات قرار می‌گیرد.</div></div>

<h2>✨ بعد از نصب — نکات استفاده</h2>
<div class="card">
<ul>
<li><b>پذیرش خدمت درمانی:</b> صفحهٔ پذیرش → «نوع پذیرش» → «💉 خدمت درمانی» → خدمت را از تعرفه‌ها انتخاب کنید (تزریقات/پانسمان/آزمایش/تصویربرداری و…)؛ انجام‌دهندهٔ پرسنل اختیاری است؛ فاکتور با همان خدمت خودکار صادر می‌شود.</li>
<li><b>تعرفه به‌تفکیک بیمه:</b> صفحهٔ بیمه → تب تعرفه‌ها → «هدف تعرفه» را روی بیمهٔ موردنظر بگذارید و قیمت اختصاصی همان بیمه را ثبت کنید؛ صدور فاکتور با بیمهٔ بیمار، قیمت همان بیمه را می‌گیرد (در نبود، تعرفهٔ آزاد).</li>
<li><b>قبض نوبت:</b> تنظیمات → چاپگر → «چاپگر نوبت‌دهی» را انتخاب و «چاپ خودکار قبض نوبت» را فعال کنید؛ با هر ثبت نوبت، قبض فیشی ۸۰ میلی‌متری مستقیم چاپ می‌شود.</li>
<li><b>آیکون‌های رنگی و پریست تم:</b> تنظیمات → تم و ظاهر → «آیکون‌های منوی کناری: رنگی» و بخش «پریست‌های شخصی» برای ذخیره/بازیابی ترکیب کامل.</li>
<li><b>ارقام فارسی:</b> زبان همهٔ پنجره‌ها fa-IR شد؛ همهٔ اعداد حتی بدون مبدل، فارسی رندر می‌شوند. اگر فونت پیش‌فرض ویندوز مدنظرتان بود، از همان تنظیمات تم انتخاب کنید («وزیرمتن (فونت برنامه)» جداگانه مشخص است).</li>
<li><b>داروها و تداخل‌ها:</b> در فرم نسخه، دکمه‌های «بروزرسانی فهرست داروها» و «بروزرسانی تداخل‌ها» را یک‌بار بزنید تا ۲۰ دارو و ۵۰ تداخل جدید اضافه شوند.</li>
<li><b>پشتیبان:</b> صفحهٔ پشتیبان‌گیری → فرمت ZIP یا BAK را انتخاب کنید؛ بازیابی هر دو را خودکار تشخیص می‌دهد؛ فایل‌های قدیمی را با دکمهٔ «🗑 حذف فایل انتخاب‌شده» پاک کنید (حذف خودکار شبانه مطابق سهمیهٔ نگهداشت قبلی فعال است).</li>
<li><b>تفاوت فرم‌ها با اسکرین‌شات‌ها:</b> گزارش کامل در فایل <code>docs/forms-vs-screenshots-report.md</code> آماده شد.</li>
</ul>
</div>

<h2>🗒️ تغییر فایل‌ها</h2>
<p>فهرست کامل فایل‌های این بسته در <code>changed-files.txt</code> کنار همین راهنماست.</p>

<footer>درمانگاه سهند — سامانه جامع مدیریت درمانگاه • نسخهٔ ۲.۲.۰ • مهر ۱۴۰۵</footer>
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
    print(f"✓ بسته ساخته شد: {OUT}  ({size_kb} KB، {len(files)} فایل)")


if __name__ == "__main__":
    main()
