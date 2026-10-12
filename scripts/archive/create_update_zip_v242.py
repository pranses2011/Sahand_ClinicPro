#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.4.1 → v2.4.2 — چرخهٔ بیست‌ونهم.
فقط فایل‌های تغییرکردهٔ همین رلیز (تگ v2.4.1 تا HEAD) با همان ساختار
پوشه‌ای مخزن + مانیفست متنی + راهنمای فارسی HTML.

گاردهای انسجام: نشانه‌های سلامت تغییرات کلیدی این نسخه باید در سورس موجود
باشند وگرنه اسکریپت متوقف می‌شود.
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.1"    # رلیز چرخهٔ بیست‌وهشتم
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.1-to-2.4.2.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.2.html"

# ─── گارد انسجام مثبت — نشانه‌های سلامت تغییرات کلیدی این نسخه ───
COHERENCE_CHECKS = [
    # کلید مشخصات کاربری در لیست پزشکان/پرسنل
    ("src/Sahand.ClinicPro.API/Controllers/UsersController.cs",
     "GetUserByEmployee", "اندپوینت حساب متصل به پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/Services/AccountTools.cs",
     "OpenForEmployeeAsync", "سرویس مشترک گشودن حساب پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/UserFormViewModel.cs",
     "PrepareForEmployee", "پیش‌پر فرم ثبت حساب برای پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/UserFormViewModel.cs",
     "employeeId = LinkedEmployeeId", "ارسال شناسهٔ دقیق پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/Views/PersonnelView.xaml",
     'Kind="AccountKey"', "کلید حساب کاربری در ردیف پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorsView.xaml",
     'Kind="AccountKey"', "کلید حساب کاربری در ردیف پزشک"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/PersonnelViewModel.cs",
     "ManageAccountAsync", "فرمان مشخصات کاربری پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/DoctorsViewModel.cs",
     "ManageAccountAsync", "فرمان مشخصات کاربری پزشک"),
    # انتخاب‌گر رنگ حرفه‌ای
    ("src/Sahand.ClinicPro.Desktop/Views/ColorPickerWindow.xaml",
     "PaletteRows", "پنجرهٔ انتخاب‌گر رنگ با پالت"),
    ("src/Sahand.ClinicPro.Desktop/Views/ColorPickerWindow.xaml.cs",
     "public static string? Pick(", "نقطهٔ ورود استاتیک انتخاب‌گر"),
    ("src/Sahand.ClinicPro.Desktop/Converters/HexToBrushConverter.cs",
     "HexToBrushConverter", "مبدل کد رنگ به قلم‌وی"),
    ("src/Sahand.ClinicPro.Desktop/App.xaml",
     'HexToBrushConverter x:Key="HexToBrush"', "ثبت سراسری مبدل رنگ"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/SettingsViewModel.cs",
     "🎨 رنگ دلخواه…", "گزینهٔ رنگ دلخواه در کشوها"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/SettingsViewModel.cs",
     "EffectiveSidebarHex", "رنگ مؤثر منو با پشتیبانی دلخواه"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/SettingsViewModel.cs",
     "PrimaryCustomIndex", "گزینهٔ دلخواه رنگ اصلی"),
    ("src/Sahand.ClinicPro.Desktop/Views/SettingsView.xaml",
     "PickSidebarColorCommand", "دکمهٔ انتخاب‌گر رنگ منو"),
    ("src/Sahand.ClinicPro.Desktop/Views/SettingsView.xaml",
     "PickPrimaryColorCommand", "دکمهٔ انتخاب‌گر رنگ اصلی"),
    # کاتالوگ دارو/تداخل
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/MedicationSeedDataSupplement6.cs",
     "Supplement6Rows", "مکمل ششم داروها"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/DrugInteractionSeedDataSupplement5.cs",
     "Supplement5Rows", "مکمل پنجم تداخل‌ها"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/MedicationSeedDataSupplement.cs",
     "Concat(Supplement6Rows)", "تجمیع مکمل ششم در AllRows"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/DrugInteractionSeedDataSupplement.cs",
     "Concat(Supplement5Rows)", "تجمیع مکمل پنجم در AllInteractions"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/ReferenceDataSeeder.cs",
     '"Valproate" or "Valproic Acid"', "نگاشت مستعار والپروات"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/ReferenceDataSeeder.cs",
     '"Beta blockers" or "Beta-blocker"', "نگاشت کلاس بتابلاکرها"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/DrugInteractionSeedDataSupplement5.cs",
     '"Trimethoprim/Sulfamethoxazole", "Warfarin"', "تداخل پرخطر کو-تریموکسازول"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/DrugInteractionSeedDataSupplement5.cs",
     '"Tamoxifen", "Paroxetine"', "تداخل CYP2D6 تاموکسیفن"),
    # پاک‌سازی هشدار
    ("src/Sahand.ClinicPro.Desktop/ViewModels/SurveyViewModel.cs",
     "// QuestionsJson: متن خام JSON سوالات", "کامنت درون‌خطی جایگزین XML نامعتبر"),
    # نسخه
    ("Directory.Build.props",
     "2.4.2", "نسخهٔ 2.4.2"),
]

# نشانه‌های منفی — نباید وجود داشته باشند
NEGATIVE_CHECKS = [
    # رشتهٔ پالت قدیمی بدون گزینهٔ دلخواه حذف شده باشد
    ("src/Sahand.ClinicPro.Desktop/ViewModels/SettingsViewModel.cs",
     '"همگام با تم", "آبی تیره رسمی", "سبز تیره", "خاکستری گرافیت",\n        "زرشکی تیره", "فیروزه‌ای تیره", "بنفش تیره", "قهوه‌ای گرم", "مشکی مات"\n    };',
     "فهرست پالت منو بدون گزینهٔ دلخواه حذف"),
    # سقوط ناموفق پریست به Math.Max(0, Array.IndexOf قدیمی حذف شود — نشانهٔ مسیر رنگ اصلی
    ("src/Sahand.ClinicPro.Desktop/ViewModels/SettingsViewModel.cs",
     "CustomPrimaryIndex = Math.Max(0, Array.IndexOf(CustomPrimaryHexes, data.CustomPrimaryHex));",
     "سقوط پریست به همگام با تم حذف"),
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
<title>راهنمای به‌روزرسانی ۲.۴.۱ به ۲.۴.۲</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:760px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}ol{line-height:2.1}b{color:#2E7D32}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
ul{line-height:2}</style></head><body><div class="card">
<h1>📦 بستهٔ به‌روزرسانی سهند کلینیک‌پرو — ۲.۴.۱ به ۲.۴.۲</h1>
<div class="ok">این بسته فقط فایل‌های تغییرکرده را دارد؛ همه را با همان ساختار پوشه‌ای روی نسخهٔ نصب‌شدهٔ <b>۲.۴.۱</b> کپی و جایگزین کنید.</div>
<h3>مراحل نصب</h3>
<ol>
<li>برنامهٔ دسکتاپ و <b>سرور API</b> را کامل ببندید (مطمئن شوید پردازهٔ Sahand.ClinicPro.API در حال اجرا نیست)</li>
<li>از پوشهٔ نصب فعلی، یک نسخهٔ پشتیبان (کپی کامل) تهیه کنید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی پوشهٔ نصب کپی و <b>جایگزین</b> کنید</li>
<li>سرور API را اجرا کنید — این نسخه مهاجرت دیتابیس ندارد؛ اندپوینت جدید حساب کاربری پرسنل هم‌زمان با نخستین اجرا فعال است</li>
<li>برنامهٔ دسکتاپ را اجرا و از نسخهٔ ۲.۴.۲ در Splash مطمئن شوید</li>
</ol>
<div class="warn"><b>نکتهٔ دارو/تداخل (فقط یک‌بار پس از نصب):</b> در <b>فرم تجویز نسخه</b> ابتدا دکمهٔ <b>«بروزرسانی فهرست داروها»</b> (کنار جستجوی دارو) را بزنید تا ۲۱ داروی مرجع جدید و ۱۷۰+ جفت تداخلی تازه به دیتابیس سرور اضافه شود؛ سپس دکمهٔ <b>«بروزرسانی تداخل‌ها»</b> (کنار بخش بررسی خطر تشخیصی) را بزنید تا کش محلی برنامه از سرور تازه شود.</div>
<h3>خلاصهٔ تغییرات مهم این نسخه</h3>
<ul>
<li>🔑 <b>کلید «مشخصات کاربری»</b> در لیست پزشکان و پرسنل: حساب ورودِ متصل به همان رکورد را ویرایش می‌کند یا فرم ثبتِ حساب جدیدِ متصل به همان پرسنل (با نام/موبایل پیش‌پر) باز می‌کند</li>
<li>🎨 <b>انتخاب‌گر رنگ حرفه‌ای</b> در تنظیمات تم: برای رنگ منوی کناری و رنگ اصلی، هر رنگی قابل انتخاب است — پالت متریال ۱۹×۵ + اسلایدر RGB + ورودی کد HEX + پیش‌نمایش زنده؛ رنگ دلخواه در پریست‌ها هم دیگر گم نمی‌شود</li>
<li>💊 <b>کاتالوگ دارو/تداخل</b>: ۲۱ داروی مرجع جدید (کدئین، کو-تریموکسازول، موکسی‌فلوکساسین، فنتانیل، آدرنالین و…) + ۵۰ تداخل بالینی جدید + <b>فعال‌سازی ۱۲۲ جفت تداخلی که به دلیل عدم تطبیق نام‌ها هرگز اعمال نمی‌شدند</b> (والپروات، لیتیم، بتتابلاکرها، ACEI و…)</li>
<li>🧹 بیلد کامل اکنون ۰ خطا/۰ هشدار</li>
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
               f"بستهٔ به‌روزرسانی 2.4.1 → 2.4.2\nتعداد فایل: {len(manifest_lines)}\n\n" + "\n".join(manifest_lines))

print(f"✅ ساخته شد: {OUT} ({OUT.stat().st_size / 1024:.0f} کیلوبایت)")
print(f"— راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME}")
