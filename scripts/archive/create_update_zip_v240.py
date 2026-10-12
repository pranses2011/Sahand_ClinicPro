#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.3.0 → v2.4.0 — چرخهٔ بیست‌وهفتم (موارد منتقل‌شدهٔ گزارش هجدهم مالک).
فقط فایل‌های تغییرکردهٔ همین رلیز (کامیت 1524997 = v2.3.0 تا HEAD) با
همان ساختار پوشه‌ای مخزن + مانیفست متنی + راهنمای فارسی HTML.

گاردهای انسجام: نشانه‌های سلامت تغییرات کلیدی این نسخه باید در سورس موجود
باشند وگرنه اسکریپت متوقف می‌شود.
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "1524997"   # v2.3.0 (رلیز چرخه بیست‌وششم)
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.3.0-to-2.4.0.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.0.html"

# ─── گارد انسجام مثبت — نشانه‌های سلامت تغییرات کلیدی این نسخه ───
COHERENCE_CHECKS = [
    # آف‌بای‌وان کشوی بیمهٔ فاکتور
    ("src/Sahand.ClinicPro.Desktop/ViewModels/BillingViewModel.cs",
     "بازبینی الگوی کشوی بیمه", "رفع آف‌بای‌وان بیمهٔ فاکتور"),
    # واحد پول پویا — فیش حقوقی
    ("src/Sahand.ClinicPro.Desktop/Views/PayslipWindow.xaml.cs",
     "Money.FormatWithUnit", "فیش حقوقی با واحد پویا"),
    # پیام‌های سرور لایسنس در برنامه
    ("src/Sahand.ClinicPro.Desktop/Services/LicenseGate.cs",
     "CheckDetailedAsync", "بررسی تفصیلی لایسنس با پیام"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     "LoadLicenseStatusAsync", "نوار وضعیت پویای لایسنس"),
    # فیلدهای پنل PHP
    ("src/LicenseServer.Php/inc/functions.php",
     "clinicEconomicCode", "کد اقتصادی در پاسخ پنل"),
    ("src/LicenseServer.Php/inc/functions.php",
     "economic_code", "ستون کد اقتصادی دیتابیس پنل"),
    # users/me + میزکار پزشک
    ("src/Sahand.ClinicPro.API/Controllers/UsersController.cs",
     "GetCurrentUser", "نقطهٔ پایانی users/me"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/DoctorVisitViewModel.cs",
     "میزکار پزشک", "صفحهٔ ویزیت پزشک"),
    # دارو/تداخل
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/MedicationSeedDataSupplement5.cs",
     "Supplement5Rows", "مکمل پنجم داروها"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/DrugInteractionSeedDataSupplement4.cs",
     "Supplement4Rows", "مکمل چهارم تداخل‌ها"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/MedicationSeedDataSupplement.cs",
     "Supplement5Rows", "اتصال مکمل پنجم به AllRows"),
    # تب‌بندی تنظیمات
    ("src/Sahand.ClinicPro.Desktop/Views/SettingsView.xaml",
     "TabControl", "تب‌بندی صفحهٔ تنظیمات"),
    # قالب‌های آمادهٔ نظرسنجی
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs",
     "SurveyTemplates", "قالب‌های آمادهٔ نظرسنجی"),
    # مجموعهٔ آیکون‌ها
    ("src/Sahand.ClinicPro.Desktop/Converters/IconSetKindConverter.cs",
     "IconSetKindConverter", "مبدل مجموعهٔ آیکون"),
    ("src/Sahand.ClinicPro.Desktop/Services/ThemeService.cs",
     "App.IconSetName", "منبع پویای مجموعهٔ آیکون"),
    # هشدار CA2013
    ("src/Sahand.ClinicPro.Desktop/App.xaml.cs",
     "is not true", "الگوی صحیح بررسی پرچم مجوز بستن"),
    # نسخه
    ("Directory.Build.props",
     "2.4.0", "نسخهٔ 2.4.0"),
]

# نشانه‌های منفی — نباید وجود داشته باشند
NEGATIVE_CHECKS = [
    # الگوی قدیمی آف‌بای‌وان بیمه باید کاملاً حذف شده باشد
    ("src/Sahand.ClinicPro.Desktop/ViewModels/BillingViewModel.cs",
     "InsuranceCompanies[InsuranceIndex - 1]", "آف‌بای‌وان بیمهٔ فاکتور حذف"),
    # ReferenceEquals روی bool حذف شده باشد
    ("src/Sahand.ClinicPro.Desktop/App.xaml.cs",
     "ReferenceEquals(window.GetValue", "الگوی CA2013 حذف"),
]


def run(cmd: str) -> str:
    return subprocess.run(cmd, shell=True, cwd=REPO, capture_output=True, text=True).stdout.strip()


# ─── گاردها ───
errors = []
for rel, marker, desc in COHERENCE_CHECKS:
    p = REPO / rel
    if not p.exists() or marker not in p.read_text(encoding="utf-8"):
        errors.append(f"گارد مثبت شکست خورد — {desc}: {rel} ⊂ «{marker}»")
for rel, marker, desc in NEGATIVE_CHECKS:
    p = REPO / rel
    if p.exists() and marker in p.read_text(encoding="utf-8"):
        errors.append(f"گارد منفی شکست خورد — {desc}: {rel} ∌ «{marker}»")
if errors:
    for e in errors:
        print("⛔", e)
    raise SystemExit(1)
print("✅ همهٔ گاردهای انسجام پاس شد")

# ─── فهرست فایل‌های تغییرکرده ───
changed = run(f"git diff --name-only --diff-filter=ACMR {BASE}..HEAD -- src tests Directory.Build.props CHANGELOG.md")
files = [f for f in changed.splitlines() if f.strip()]
if not files:
    raise SystemExit("⛔ هیچ فایل تغییرکرده‌ای یافت نشد")
print(f"📦 {len(files)} فایل تغییرکرده")

# ─── راهنمای فارسی HTML ───
guide = """<!DOCTYPE html>
<html lang="fa" dir="rtl"><head><meta charset="utf-8">
<title>راهنمای به‌روزرسانی ۲.۳.۰ به ۲.۴.۰</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:760px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}ol{line-height:2.1}b{color:#2E7D32}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
ul{line-height:2}</style></head><body><div class="card">
<h1>📦 بستهٔ به‌روزرسانی سهند کلینیک‌پرو — ۲.۳.۰ به ۲.۴.۰</h1>
<div class="ok">این بسته فقط فایل‌های تغییرکرده را دارد؛ همه را با همان ساختار پوشه‌ای روی نسخهٔ نصب‌شدهٔ <b>۲.۳.۰</b> کپی و جایگزین کنید.</div>
<h3>مراحل نصب</h3>
<ol>
<li>برنامهٔ دسکتاپ و <b>سرور API</b> را کامل ببندید (مطمئن شوید پردازهٔ Sahand.ClinicPro.API در حال اجرا نیست)</li>
<li>از پوشهٔ نصب فعلی، یک نسخهٔ پشتیبان (کپی کامل) تهیه کنید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی پوشهٔ نصب کپی و <b>جایگزین</b> کنید</li>
<li>سرور API را اجرا کنید — این نسخه مهاجرت دیتابیس جدیدی ندارد</li>
<li>برنامهٔ دسکتاپ را اجرا و از نسخهٔ ۲.۴.۰ در Splash مطمئن شوید</li>
</ol>
<div class="warn"><b>نکتهٔ مهم (پنل لایسنس PHP):</b> اگر سرور پنل لایسنس را خودتان میزبانی می‌کنید، فایل‌های <code>LicenseServer.Php</code> این بسته را هم روی هاست به‌روزرسانی کنید تا فیلدهای «کد اقتصادی» و «شناسهٔ ملی» درمانگاه فعال شوند؛ ستون‌های جدید دیتابیس در نخستین بازدید خودکار ساخته می‌شوند (نصب مجدد لازم نیست).</div>
<h3>خلاصهٔ تغییرات مهم این نسخه</h3>
<ul>
<li>🩺 <b>میزکار پزشک</b> — صفحهٔ جدید در گروه «پذیرش و درمان»: خودکار می‌شناسد کدام پزشک وارد شده (اتصال حساب به پرسنل/پزشک) و فقط صف و آمار خودش را نشان می‌دهد؛ فراخوان بیمار بعدی با Space</li>
<li>🏛 پیام‌های سرور لایسنس در برنامه نمایش می‌یابد + نوار وضعیت لایسنس واقعی است (روزهای باقیمانده) + فیلدهای کد اقتصادی/شناسهٔ ملی/شهر از پنل لایسنس همگام می‌شود</li>
<li>🐛 صدور فاکتور: بیمهٔ انتخابی درست ثبت می‌شود (بیمهٔ قبلی دیگر جایگزین نمی‌شود)</li>
<li>💰 واحد پول (ریال/تومان) در همه‌جا اثر می‌کند — از جمله فیش حقوقی چاپی</li>
<li>💊 ۷۰ داروی مهم جدید + ۵۵ تداخل بالینی جدید — با دکمه‌های «بروزرسانی فهرست داروها/تداخل‌ها» در فرم نسخه دریافت کنید</li>
<li>⚙️ صفحهٔ تنظیمات تب‌بندی شد (۶ تب) + ۴ قالب آمادهٔ نظرسنجی با یک کلیک در «قالب چاپ و نظرسنجی»</li>
<li>🎨 مجموعهٔ آیکون‌های منو قابل تعویض است: کلاسیک / خط‌نازک مدرن / مینیمال (تب «تم و ظاهر» تنظیمات — بدون ری‌استارت)</li>
</ul>
</div></body></html>"""

# ─── ساخت ZIP ───
OUT.parent.mkdir(parents=True, exist_ok=True)
if OUT.exists():
    OUT.unlink()

manifest_lines = []
with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    z.writestr(GUIDE_NAME, guide)
    for rel in files:
        full = REPO / rel
        if not full.exists():
            print(f"⚠️ موجود نیست (رد شد): {rel}")
            continue
        z.write(full, rel)
        manifest_lines.append(rel)
    z.writestr(MANIFEST_NAME,
               f"بستهٔ به‌روزرسانی 2.3.0 → 2.4.0\nتعداد فایل: {len(manifest_lines)}\n\n" + "\n".join(manifest_lines))

print(f"✅ ساخته شد: {OUT} ({OUT.stat().st_size / 1024:.0f} کیلوبایت)")
print(f"— راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME}")
