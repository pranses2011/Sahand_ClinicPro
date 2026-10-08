#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.0.9 → v2.1.0 — چرخه بیست‌وسوم (گزارش شانزدهم مالک).
فقط فایل‌های تغییرکردهٔ همین رلیز (تگ v2.0.9 تا HEAD) با همان ساختار پوشه‌ای
مخزن، به‌همراه راهنمای فارسی HTML و مانیفست متنی — الگوی چرخه‌های قبل.

گاردهای انسجام: نشانه‌های سلامت رفع‌های کلیدی این نسخه باید در سورس موجود
باشند و نشانه‌های باگ‌نباید باشند، وگرنه اسکریپت متوقف می‌شود.
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
TAG = "v2.0.9"
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.0.9-to-2.1.0.zip")
GUIDE_NAME = "Update-Guide-2.1.0.html"
MANIFEST_NAME = "changed-files.txt"

# ─── گارد انسجام مثبت — نشانه‌های سلامت رفع‌های این نسخه ───
COHERENCE_CHECKS = [
    # فونت سراسری واقعی
    ("src/Sahand.ClinicPro.Desktop/App.xaml.cs",
     "OnAnyWindowLoadedForFont", "کلاس‌هندلر فونت سراسری"),
    ("src/Sahand.ClinicPro.Desktop/App.xaml.cs",
     'SetResourceReference(Window.FontFamilyProperty, "App.FontFamily")', "اتصال پویای فونت"),
    # رنگ زندهٔ منو
    ("src/Sahand.ClinicPro.Desktop/Services/ThemeService.cs",
     '"App.SidebarBrush"', "قلم گرادیانی منو"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     '{DynamicResource App.SidebarBrush}', "مصرف پویای رنگ منو"),
    # رفع 429
    ("src/Sahand.ClinicPro.API/Program.cs",
     "PermitLimit = 600", "سقف نرخ ۶۰۰"),
    # تمپلیت نسخه — FK اختیاری
    ("src/Sahand.ClinicPro.Domain/Clinical/PrescriptionTemplate.cs",
     "public long? DoctorId", "DoctorId اختیاری"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261009000001_Cycle23TemplateDoctorNullable.cs",
     "Cycle23TemplateDoctorNullable", "مهاجرت FK اختیاری"),
    # بیمه آزاد
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     '"آزاد (بدون بیمه)"', "گزینهٔ آزاد پذیرش"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/BillingViewModel.cs",
     '"آزاد (بدون بیمه)"', "گزینهٔ آزاد صورتحساب"),
    # چاپ
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     "PrescriptionPrintStyle", "طرح‌های چاپ"),
    ("src/Sahand.ClinicPro.Desktop/Views/PrescriptionDetailWindow.xaml.cs",
     "OnReprintClick", "چاپ مجدد"),
    ("src/Sahand.ClinicPro.Application/DTOs/Prescriptions/PrescriptionDtos.cs",
     "string PatientName,\n    long DoctorId,", "نام بیمار/پزشک در DTO جزئیات"),
    # CRUD بیمه/تعرفه
    ("src/Sahand.ClinicPro.Application/Features/Lookups/InsuranceCompanyCommands.cs",
     "CreateInsuranceCompanyCommand", "فرمان ساخت بیمه"),
    ("src/Sahand.ClinicPro.Application/Features/Lookups/ServiceTariffCommands.cs",
     "CreateServiceTariffCommand", "فرمان ساخت تعرفه"),
    ("src/Sahand.ClinicPro.Desktop/Views/CompanyFormWindow.xaml",
     "CompanyFormWindow", "پنجرهٔ فرم بیمه"),
    ("src/Sahand.ClinicPro.Desktop/Views/TariffFormWindow.xaml",
     "TariffFormWindow", "پنجرهٔ فرم تعرفه"),
    # پرسنل مستقل
    ("src/Sahand.ClinicPro.Desktop/Views/PersonnelView.xaml",
     "PersonnelView", "نمای مستقل پرسنل"),
    # دارو/تداخل
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/DrugInteractionSeedDataSupplement2.cs",
     "Supplement2Rows", "مکمل دوم تداخل‌ها"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/MedicationSeedDataSupplement3.cs",
     "Supplement3Rows", "مکمل سوم داروها"),
    # نسخه
    ("Directory.Build.props",
     "<Version>2.1.0</Version>", "نسخهٔ 2.1.0"),
    ("CHANGELOG.md",
     "[v2.1.0]", "بخش CHANGELOG"),
]

# ─── گارد انسجام منفی — نشانه‌های باگ نباید باشند ───
ABSENCE_CHECKS = [
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "InsuranceCompanies.Add(new InsuranceOption(c.Id, c.Name));\n        if", "درج بدون آزاد"),
    ("src/Sahand.ClinicPro.Application/Features/Prescriptions/PrescriptionTemplates/PrescriptionTemplatesFeatures.cs",
     "DoctorId = request.DoctorId ?? 0", "مقدار صفر FK"),
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml",
     "MaterialDesign.Brush.TextField.HoverBackground", "کلید براش نامعتبر"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/MedicationSeedDataSupplement3.cs",
     '"9101', "کد IRC تصادمی با مکمل ۲"),
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
<title>راهنمای به‌روزرسانی ۲.۰.۹ → ۲.۱.۰ — درمانگاه سهند</title>
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
<h1>📦 به‌روزرسانی ۲.۰.۹ → ۲.۱.۰</h1>
<div>درمانگاه سهند — چرخه بیست‌وسوم (گزارش شانزدهم مالک)</div>
</header>

<div class="okbox">این نسخه شامل <b>رفع ~۳۹ مورد</b> از گزارش شماست: چاپ با دکمهٔ پرینتر و سه طرح، رفع خطای ذخیرهٔ تمپلیت (ریشهٔ کلید خارجی)، فونت و رنگ زندهٔ منو، بیمهٔ آزاد، CRUD بیمه/تعرفه، Enter در جستجوها، صفحهٔ مستقل پرسنل، ۱۱۰ تداخل و ۳۳ داروی جدید و…</div>

<h2>⚠️ پیش‌نیاز مهم — مهاجرت دیتابیس</h2>
<div class="dangerbox">این نسخه شامل مهاجرت <code>Cycle23TemplateDoctorNullable</code> است که هنگام اجرای سرور API به‌صورت خودکار اعمال می‌شود؛ فقط کافی است سرور <b>یک‌بار پس از جایگزینی فایل‌ها اجرا شود</b>. اگر سرور اجرا نبود، فایل‌ها را با خیال راحت جایگزین کنید.</div>

<h2>🔧 مراحل نصب</h2>
<div class="step"><div class="stepnum">۱</div><div><b>بستن برنامه و ویژوال استودیو</b> — مطمئن شوید برنامهٔ دسکتاپ و سرور (و VS) بسته‌اند. اگر فایل‌ها قفل بودند، در Task Manager همهٔ فرایندهای <code>Sahand.ClinicPro</code> را End Task کنید.</div></div>
<div class="step"><div class="stepnum">۲</div><div><b>استخراج ZIP</b> — فایل ZIP را باز کنید؛ ساختار پوشه‌ها همان ساختار مخزن است.</div></div>
<div class="step"><div class="stepnum">۳</div><div><b>جایگزینی فایل‌ها</b> — پوشهٔ <code>src</code> داخل ZIP را روی پوشهٔ <code>E:\\Sahand_ClinicPro\\src</code> کپی کنید (Replace). فقط فایل‌های تغییرکرده در ZIP هستند.</div></div>
<div class="step"><div class="stepnum">۴</div><div><b>حذف خروجی‌های قدیمی</b> — PowerShell در <code>E:\\Sahand_ClinicPro</code>:<br><code>Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force</code></div></div>
<div class="step"><div class="stepnum">۵</div><div><b>ساخت مجدد</b> — در Visual Studio: <b>Rebuild Solution</b> (Release یا Debug — مطابق روتین قبلی). بیلد باید با ۰ خطا و ۰ هشدار تمام شود.</div></div>
<div class="step"><div class="stepnum">۶</div><div><b>اجرای سرور و برنامه</b> — سرور API را اجرا کنید تا مهاجرت‌ها و بذرهای جدید (دارو/تداخل) اعمال شوند، سپس برنامهٔ دسکتاپ.</div></div>

<h2>✨ بعد از نصب — نکات استفاده</h2>
<div class="card">
<ul>
<li><b>چاپ نسخه:</b> در پنجرهٔ چاپ، طرح «کلاسیک/مدرن/فشرده» را از نوار بالا انتخاب کنید؛ دکمهٔ آبی «چاپ» صفحهٔ انتخاب پرینتر ویندوز را باز می‌کند. انتخاب شما برای دفعهٔ بعد یادآوری می‌شود. چاپ مجدد از پنجرهٔ جزئیات نسخه ممکن است.</li>
<li><b>بیمه/تعرفه:</b> در صفحهٔ بیمه، دکمه‌های «بیمهٔ جدید» و «ویرایش» (تب شرکت‌ها) و «تعرفهٔ جدید» و «ویرایش» (تب تعرفه‌ها). پذیرش و صورتحساب اکنون گزینهٔ «آزاد (بدون بیمه)» دارند.</li>
<li><b>داروها و تداخل‌ها:</b> در فرم نسخه، دکمه‌های «بروزرسانی فهرست داروها» و «بروزرسانی تداخل‌ها» را یک‌بار بزنید تا ۳۳ دارو و ~۱۱۰ تداخل جدید (شامل ضدانعاددهای جدید مثل ریواروکسابان) اضافه شوند.</li>
<li><b>فونت و رنگ منو:</b> تنظیمات → تم؛ اکنون بدون ری‌استارت زنده اعمال می‌شود.</li>
<li><b>گروه خونی بیمار:</b> فرم ثبت/ویرایش بیمار → تب «اطلاعات شخصی» → کادر «گروه خونی» کنار تاریخ تولد.</li>
</ul>
</div>

<h2>🗒️ تغییر فایل‌ها</h2>
<p>فهرست کامل فایل‌های این بسته در <code>changed-files.txt</code> کنار همین راهنماست.</p>

<footer>درمانگاه سهند — سامانه جامع مدیریت درمانگاه • نسخهٔ ۲.۱.۰ • مهر ۱۴۰۵</footer>
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
