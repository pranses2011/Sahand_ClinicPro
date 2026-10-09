#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.4.4 → v2.4.5 — چرخهٔ سی‌ودوم (گزارش ششم مالک).
فقط فایل‌های تغییرکردهٔ همین رلیز (تگ v2.4.4 تا HEAD) با همان ساختار
پوشه‌ای مخزن + مانیفست متنی + راهنمای فارسی HTML.

گاردهای انسجام: نشانه‌های سلامت تغییرات کلیدی این نسخه باید در سورس موجود
باشند و نشانه‌های الگوی معیوب حذف شده باشند وگرنه اسکریپت متوقف می‌شود.
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.4"    # رلیز چرخهٔ سی‌ویکم
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.4-to-2.4.5.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.5.html"

# ─── گارد انسجام مثبت — نشانه‌های سلامت تغییرات کلیدی این نسخه ───
COHERENCE_CHECKS = [
    # ۱) دقت ستون‌های سهم اقلام فاکتور (بحرانی‌ترین ریشهٔ مالی)
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Billing/InvoiceItemConfiguration.cs",
     '(x => x.InsuranceShare).HasPrecision(18, 2)',
     "دقت ریالی سهم بیمهٔ قلم فاکتور"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Billing/InvoiceItemConfiguration.cs",
     '(x => x.PatientShare).HasPrecision(18, 2)',
     "دقت ریالی سهم بیمار قلم فاکتور"),
    # ۲) مهاجرت سی‌ودوم — دقت + بازتنظیم تعرفه‌های آزاد
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261010100000_Cycle32FreeTariffAndSharePrecision.cs",
     "UPDATE Tariffs SET InsuranceShare = 0, PatientShare = 100",
     "بازتنظیم دادهٔ تعرفه‌های آزاد"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261010100000_Cycle32FreeTariffAndSharePrecision.cs",
     'decimal(18,2)', "ALTER دقت سهم‌ها"),
    # ۳) تعرفهٔ آزاد — بذر ۰/۱۰۰
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/ReferenceDataSeeder.cs",
     "InsuranceShare = 0m,",
     "بذر تعرفهٔ آزاد بدون سهم بیمه"),
    # ۴) تعرفهٔ آزاد — دفاع در عمق سرور
    ("src/Sahand.ClinicPro.Application/Features/Lookups/LookupQueries.cs",
     "var isFreeTarget = request.InsuranceCompanyId is null;",
     "کوئری پذیرش: آزاد همیشه ۰/۱۰۰"),
    ("src/Sahand.ClinicPro.Application/Features/Lookups/ServiceTariffCommands.cs",
     "isFreeTarget ? 0m : request.InsuranceShare",
     "فرمان‌های تعرفه: آزاد ۰/۱۰۰ اجباری"),
    # ۵) پذیرش دسکتاپ — حالت آزاد
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "HasInsurance = !isFree;",
     "پرچم بیمهٔ واقعی در پذیرش"),
    ("src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml",
     "پذیرش آزاد (بدون بیمه) — کل مبلغ تعرفه توسط بیمار پرداخت می‌شود",
     "راهنمای حالت آزاد در فرم پذیرش"),
    # ۶) قرارداد رشته‌ای قالب چاپ
    ("src/Sahand.ClinicPro.API/Controllers/PrintTemplatesController.cs",
     "TryParseTemplateType",
     "تحلیل امن نوع رشته‌ای قالب"),
    ("src/Sahand.ClinicPro.API/Controllers/PrintTemplatesController.cs",
     "string Type,",
     "DTO رشته‌ای قالب چاپ"),
    # ۷) تپ قلب لایسنس — اثر انگشت سه‌مرحله‌ای
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "ComputeLocalFingerprint();",
     "fallback اثر انگشت محلی در تپ قلب"),
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "string.IsNullOrWhiteSpace(license.HardwareFingerprint)",
     "بررسی خالی‌نبودن اثر انگشت رکورد"),
    # ۸) اندپوینت تازه‌سازی لایسنس
    ("src/Sahand.ClinicPro.API/Controllers/LicenseController.cs",
     '[HttpPost("refresh")]',
     "اندپوینت license/refresh"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/SettingsViewModel.cs",
     '"license/refresh"',
     "تازه‌سازی لایسنس در تنظیمات"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/LicenseStatusViewModel.cs",
     "PostAnonymousAsync(\"license/refresh\"",
     "بررسی آنلاین واقعی صفحهٔ لایسنس"),
    # ۹) پیام‌های لایسنس + داشبورد
    ("src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs",
     'TryGetProperty("data", out var data)',
     "بازکردن پاکت پیام‌های پشتیبانی"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs",
     "UnwrapData",
     "بازکردن پاکت پنج بخش داشبورد"),
    # ۱۰) عنوان شناور سراسری
    ("src/Sahand.ClinicPro.Desktop/App.xaml",
     'Property="materialDesign:HintAssist.IsFloating" Value="True"',
     "عنوان شناور سراسری فیلدها"),
    # ۱۱) فونت‌های Consolas
    ("src/Sahand.ClinicPro.Desktop/Views/InsuranceView.xaml",
     '٪ سهم بیمه',
     "هدر درصدی لیست تعرفه"),
    # نسخه
    ("Directory.Build.props",
     "<Version>2.4.5</Version>", "نسخهٔ 2.4.5"),
    ("CHANGELOG.md",
     "[v2.4.5]", "ثبت چرخهٔ سی‌ودوم در CHANGELOG"),
]

# نشانه‌های منفی — الگوهای معیوب باید حذف شده باشند
NEGATIVE_CHECKS = [
    # دقت قدیمی درصدی روی مبالغ ریالی
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Billing/InvoiceItemConfiguration.cs",
     '(x => x.InsuranceShare).HasPrecision(5, 2)',
     "دقت معیوب سهم بیمه حذف شد"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Billing/InvoiceItemConfiguration.cs",
     '(x => x.PatientShare).HasPrecision(5, 2)',
     "دقت معیوب سهم بیمار حذف شد"),
    # بذر قدیمی ۷۰/۳۰ روی آزاد
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/ReferenceDataSeeder.cs",
     "InsuranceShare = row.InsurancePercent,",
     "بذر قدیمی درصد بیمه‌ای روی تعرفهٔ آزاد حذف شد"),
    # تپ قلب قدیمی با ?? بی‌اثر
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     '?? license.HardwareFingerprint ?? ""',
     "الگوی ?? بی‌اثر اثر انگشت حذف شد"),
    # DTO enum قدیمی قالب چاپ
    ("src/Sahand.ClinicPro.API/Controllers/PrintTemplatesController.cs",
     "PrintTemplateType Type,",
     "DTO enum قدیمی قالب چاپ حذف شد"),
    # پاکت‌خوانی قدیمی پیام‌ها
    ("src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs",
     'var response = await _api.GetAsync<JsonElement>("license/messages");\n            if (response.ValueKind == JsonValueKind.Array)',
     "الگوی آرایهٔ مستقیم پیام‌ها حذف شد"),
    # فونت Consolas در لیست تعرفه
    ("src/Sahand.ClinicPro.Desktop/Views/InsuranceView.xaml",
     'Value="Consolas"',
     "فونت Consolas لیست تعرفه حذف شد"),
    ("src/Sahand.ClinicPro.Desktop/Views/BillingView.xaml",
     'Value="Consolas"',
     "فونت Consolas صندوق حذف شد"),
]


def run(cmd: str) -> str:
    return subprocess.run(cmd, shell=True, cwd=REPO, capture_output=True, text=True).stdout.strip()


# ─── گاردها ───
errors = []
for rel, marker, desc in COHERENCE_CHECKS:
    p = REPO / rel
    if not p.exists() or marker not in p.read_text(encoding="utf-8-sig"):
        errors.append(f"گارد مثبت شکست خورد — {desc}: {rel} ⊂ «{marker}»")
for rel, marker, desc in NEGATIVE_CHECKS:
    p = REPO / rel
    if p.exists() and marker in p.read_text(encoding="utf-8-sig"):
        errors.append(f"گارد منفی شکست خورد — {desc}: {rel} ∌ «{marker}»")
if errors:
    for e in errors:
        print("⛔", e)
    raise SystemExit(1)
print("✅ همهٔ گاردهای انسجام پاس شد")

# ─── فهرست فایل‌های تغییرکرده ───
changed = run(f"git diff --name-only --diff-filter=ACMR {BASE}..HEAD -- src tests scripts Directory.Build.props CHANGELOG.md")
files = [f for f in changed.splitlines() if f.strip()]
# اسکریپت‌های بسته‌بندی نسخه‌های قبل فقط کار مخزن‌اند — در بستهٔ آپدیت کاربر جایی ندارند
files = [f for f in files if not f.startswith("scripts/create_update_zip_v2")
         and not f.startswith("scripts/publish_release_v2")]
if not files:
    raise SystemExit("⛔ هیچ فایل تغییرکرده‌ای یافت نشد")
print(f"📦 {len(files)} فایل تغییرکرده")
for f in files:
    print("   •", f)

# ─── راهنمای فارسی HTML ───
guide = """<!DOCTYPE html>
<html lang="fa" dir="rtl"><head><meta charset="utf-8">
<title>راهنمای به‌روزرسانی ۲.۴.۴ به ۲.۴.۵</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:780px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}ol{line-height:2.1}b{color:#2E7D32}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
.info{background:#E3F2FD;border-radius:10px;padding:12px 16px;border-right:4px solid #1E88E5;margin:14px 0}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
ul{line-height:2}</style></head><body><div class="card">
<h1>📦 بستهٔ به‌روزرسانی سهند کلینیک‌پرو — ۲.۴.۴ به ۲.۴.۵</h1>
<div class="ok">این بسته فقط فایل‌های تغییرکرده را دارد؛ همه را با همان ساختار پوشه‌ای روی نسخهٔ ۲.۴.۴ کپی و جایگزین کنید و سپس Solution را دوباره بیلد کنید.</div>
<h3>مراحل نصب</h3>
<ol>
<li>برنامهٔ دسکتاپ و <b>سرور API</b> را کامل ببندید (مطمئن شوید پردازهٔ Sahand.ClinicPro.API در حال اجرا نیست)</li>
<li>از پوشهٔ پروژهٔ فعلی، یک نسخهٔ پشتیبان (کپی کامل) تهیه کنید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی پوشهٔ پروژه کپی و <b>جایگزین</b> کنید — سه فایل مهاجرت جدید در <code>Persistence/Migrations</code> هم داخل بسته هستند</li>
<li>Solution را بیلد و سرور API را اجرا کنید</li>
<li>در نخستین اجرای API، <b>مهاجرت دیتابیس خودکار اعمال می‌شود</b> (دقت ستون‌های مبلغ سهم اقلام فاکتور + بازتنظیم دادهٔ تعرفه‌های آزاد به ۰٪ بیمه / ۱۰۰٪ بیمار)</li>
</ol>
<div class="info"><b>بعد از اجرا بررسی کنید:</b>
۱) در پذیرش، دکمهٔ «پذیرش و پرداخت نقدی» باید فاکتور و پرداخت را واقعاً ثبت کند و پیام موفق بدهد؛
۲) صفحهٔ «فرم‌های تکمیلی ← طراح قالب چاپ» باید قالب‌های آمادهٔ همهٔ انواع سند را نشان دهد (اگر خالی است سرور API قدیمی در حال اجراست — بیلد و ری‌استارت را فراموش نکنید)؛
۳) «تنظیمات ← اطلاعات درمانگاه» با ورود به بخش، از سرور لایسنس تازه‌سازی می‌کند؛ برای دیدن ماژول تازه‌فعال‌شده وارد بخش «ماژول‌ها» شوید یا در صفحهٔ لایسنس دکمهٔ «بررسی آنلاین» را بزنید.</div>
<h3>خلاصهٔ تغییرات مهم این نسخه</h3>
<ul>
<li>💰 <b>بحرانی — صدور فاکتور</b>: ریشهٔ «صدور فاکتور شکست خورد / خطای غیرمنتظره» ستون‌های مبلغ سهم بیمه/بیمار با سقف ۹۹۹٫۹۹ ریال بود! اکنون فاکتور و پرداخت واقعاً ثبت می‌شوند و در گزارش‌ها دیده می‌شوند</li>
<li>🆓 <b>بیمهٔ آزاد</b>: تعرفه‌های آزاد همیشه ۱۰۰٪ سهم بیمار؛ در پذیرش آزاد، ستون سهم بیمه حذف و کل مبلغ «قابل پرداخت بیمار» نمایش می‌یابد؛ فونت و هدرهای ستون‌های لیست تعرفه هم اصلاح شد</li>
<li>🖨️ <b>قالب‌های چاپ</b>: ریشهٔ دیده‌نشدن ناهمخوانی عدد/رشته بین سرور و کلاینت بود — نه لیست می‌آمد نه ذخیره ممکن بود؛ اکنون هر ۹ نوع سند قالب‌های آماده دارند و ساخت/ویرایش/کپی کار می‌کند</li>
<li>🔑 <b>لایسنس</b>: تپ قلب با اثر انگشت خالی ارسال می‌شد و سرور ردش می‌کرد — ماژول‌ها و اطلاعات درمانگاه هرگز تازه نمی‌شدند؛ حالا تازه‌سازی لحظه‌ای با ورود به تنظیمات و دکمهٔ بررسی آنلاین</li>
<li>📢 <b>پیام‌های پشتیبانی</b> روی داشبورد نمایش داده می‌شوند (پاکت پاسخ باز نمی‌شد)</li>
<li>📊 <b>کشف مهم: کل داشبورد خالی بود!</b> آمار امروز، هشدارها، نمودار درآمد، نوبت‌های امروز و فعالیت‌های اخیر — همه اکنون واقعاً پر می‌شوند</li>
<li>📝 <b>عنوان شناور همهٔ فیلدها</b> — مانند کادر کد ملی: عنوان هر فیلد هنگام پر شدن به بالای کادر می‌رود؛ در همهٔ فرم‌ها و پنجره‌ها</li>
</ul>
<div class="warn"><b>نکته:</b> اگر فاکتورهایی که قبلاً «ثبت شد» می‌گفتند در هیچ گزارشی نبودید، طبیعی است — به‌خاطر همین باگ هیچ‌وقت واقعاً ثبت نشده بودند و باید دوباره صادر شوند؛ پذیرش‌های ثبت‌شده از ماژول «صندوق و فاکتور» قابل تکمیل‌اند.</div>
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
               f"بستهٔ به‌روزرسانی 2.4.4 → 2.4.5\nتعداد فایل: {len(manifest_lines)}\n\n" + "\n".join(manifest_lines))

print(f"✅ ساخته شد: {OUT} ({OUT.stat().st_size / 1024:.0f} کیلوبایت)")
print(f"— راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME}")
