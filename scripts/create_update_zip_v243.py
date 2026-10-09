#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.4.2 → v2.4.3 — چرخهٔ سی‌امین (پایداری سرور API + فارسی کنسول).
فقط فایل‌های تغییرکردهٔ همین رلیز (تگ v2.4.2 تا HEAD) با همان ساختار
پوشه‌ای مخزن + مانیفست متنی + راهنمای فارسی HTML.

گاردهای انسجام: نشانه‌های سلامت تغییرات کلیدی این نسخه باید در سورس موجود
باشند و نشانه‌های الگوی معیوب حذف شده باشند وگرنه اسکریپت متوقف می‌شود.
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.2"    # رلیز چرخهٔ بیست‌ونهم
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.2-to-2.4.3.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.3.html"

# ─── گارد انسجام مثبت — نشانه‌های سلامت تغییرات کلیدی این نسخه ───
COHERENCE_CHECKS = [
    # ۱) رفع کرش کلید تکراری بذرافشان مرجع
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/ReferenceDataSeeder.cs",
     "var medIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);",
     "دیکشنری امن نام داروها بدون حساسیت حروف"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/ReferenceDataSeeder.cs",
     "Isosorbide Dinitrate",
     "کامنت ریشه‌یابی کرش ایزوسورباید"),
    ("src/Sahand.ClinicPro.Infrastructure/Services/Backup/AutomaticBackupWorker.cs",
     "var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);",
     "سخت‌سازی تنظیمات کارگر پشتیبان"),
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "var existing = new Dictionary<string, SystemSetting>(StringComparer.OrdinalIgnoreCase);",
     "سخت‌سازی upsert اطلاعات پشتیبانی"),
    # ۲) رفع No route matches
    ("src/Sahand.ClinicPro.API/Controllers/ApiBaseController.cs",
     "var location = Url?.Action(actionName, routeValues);",
     "ساخت زودهنگام نشانی در CreatedResponse"),
    ("src/Sahand.ClinicPro.API/Controllers/ParaclinicalController.cs",
     'CreatedResponse(result, nameof(GetTodayOrders), new { }, "طرح درمان ثبت شد")',
     "اصلاح نام اکشن ثبت طرح درمان دندان"),
    ("src/Sahand.ClinicPro.API/Controllers/PaymentsController.cs",
     'CreatedResponse(result, nameof(GetTodayTransactions), new { }, "پرداخت با موفقیت ثبت شد")',
     "اصلاح نام اکشن ثبت پرداخت"),
    # ۳) مدیریت قطع اتصال کلاینت
    ("src/Sahand.ClinicPro.API/Middleware/AuditLogMiddleware.cs",
     "when (ex is BadHttpRequestException or IOException",
     "گارد خواندن بدنهٔ ناقص"),
    ("src/Sahand.ClinicPro.API/Middleware/ExceptionHandlingMiddleware.cs",
     "catch (Microsoft.AspNetCore.Http.BadHttpRequestException)",
     "بازپرتاب استثنای زیرساختی Kestrel"),
    # ۴) دو هشدار مزمن
    ("src/Sahand.ClinicPro.Infrastructure/DependencyInjection.cs",
     "SavepointsDisabledBecauseOfMARS",
     "سرکوب مستند هشدار MARS/Savepoints"),
    ("src/Sahand.ClinicPro.API/Program.cs",
     "var hasHttpsEndpoint = app.Urls.Any(u => u.StartsWith(\"https://\"",
     "ریدایرکت شرطی https"),
    # ۵) شکل‌دهندهٔ RTL کنسول
    ("src/Sahand.ClinicPro.API/Logging/RtlConsoleShaper.cs",
     "LamAlefForm",
     "لیگاتور لام‌الف در موتور شکل‌دهی"),
    ("src/Sahand.ClinicPro.API/Logging/RtlConsoleShaper.cs",
     "ReverseRtlGraphemes",
     "وارونه‌سازی گرافمی قطعه‌های RTL"),
    ("src/Sahand.ClinicPro.API/Logging/PersianConsoleTextFormatter.cs",
     "class PersianConsoleTextFormatter : ITextFormatter",
     "قالب‌بند کنسول سازگار با فارسی"),
    ("src/Sahand.ClinicPro.API/Logging/PersianConsoleTextFormatter.cs",
     "WT_SESSION",
     "تشخیص ترمینال مدرن در قالب‌بند"),
    ("src/Sahand.ClinicPro.API/appsettings.json",
     "Sahand.ClinicPro.API.Logging.PersianConsoleTextFormatter, Sahand.ClinicPro.API",
     "اتصال قالب‌بند به سینک کنسول در تنظیمات"),
    ("src/Sahand.ClinicPro.API/Program.cs",
     "new Sahand.ClinicPro.API.Logging.PersianConsoleTextFormatter()",
     "قالب‌بند روی لاگر بوت‌استرپ"),
    # نسخه
    ("Directory.Build.props",
     "2.4.3", "نسخهٔ 2.4.3"),
    ("CHANGELOG.md",
     "[v2.4.3]", "ثبت چرخهٔ سی‌امین در CHANGELOG"),
]

# نشانه‌های منفی — الگوهای معیوب باید حذف شده باشند
NEGATIVE_CHECKS = [
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/ReferenceDataSeeder.cs",
     ".GroupBy(m => m.GenericName)",
     "الگوی شکنندهٔ GroupBy+ToDictionaryAsync حذف شد"),
    ("src/Sahand.ClinicPro.Infrastructure/Services/Backup/AutomaticBackupWorker.cs",
     ".ToDictionaryAsync(s => s.Key, s => s.Value, ct)",
     "ToDictionaryAsync مستقیم کارگر پشتیبان حذف شد"),
    ("src/Sahand.ClinicPro.API/Controllers/ParaclinicalController.cs",
     '"GetOrders"',
     'رشتهٔ نام اکشن ناموجود «GetOrders» حذف شد'),
    ("src/Sahand.ClinicPro.API/Controllers/PaymentsController.cs",
     '"GetAll"',
     'رشتهٔ نام اکشن ناموجود «GetAll» حذف شد'),
    ("src/Sahand.ClinicPro.API/Program.cs",
     "    app.UseHttpsRedirection();",
     "ریدایرکت بی‌قیدوشرط https حذف شد"),
    ("src/Sahand.ClinicPro.API/Program.cs",
     ".WriteTo.Console()",
     "سینک کنسول بدون قالب‌بند فارسی حذف شد"),
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
changed = run(f"git diff --name-only --diff-filter=ACMR {BASE}..HEAD -- src tests Directory.Build.props CHANGELOG.md")
files = [f for f in changed.splitlines() if f.strip()]
if not files:
    raise SystemExit("⛔ هیچ فایل تغییرکرده‌ای یافت نشد")
print(f"📦 {len(files)} فایل تغییرکرده")
for f in files:
    print("   •", f)

# ─── راهنمای فارسی HTML ───
guide = """<!DOCTYPE html>
<html lang="fa" dir="rtl"><head><meta charset="utf-8">
<title>راهنمای به‌روزرسانی ۲.۴.۲ به ۲.۴.۳</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:760px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}ol{line-height:2.1}b{color:#2E7D32}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
.info{background:#E3F2FD;border-radius:10px;padding:12px 16px;border-right:4px solid #1E88E5;margin:14px 0}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
ul{line-height:2}</style></head><body><div class="card">
<h1>📦 بستهٔ به‌روزرسانی سهند کلینیک‌پرو — ۲.۴.۲ به ۲.۴.۳</h1>
<div class="ok">این بسته فقط فایل‌های تغییرکرده را دارد؛ همه را با همان ساختار پوشه‌ای روی نسخهٔ ۲.۴.۲ کپی و جایگزین کنید و سپس Solution را دوباره بیلد کنید.</div>
<h3>مراحل نصب</h3>
<ol>
<li>برنامهٔ دسکتاپ و <b>سرور API</b> را کامل ببندید (مطمئن شوید پردازهٔ Sahand.ClinicPro.API در حال اجرا نیست)</li>
<li>از پوشهٔ پروژهٔ فعلی، یک نسخهٔ پشتیبان (کپی کامل) تهیه کنید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی پوشهٔ پروژه کپی و <b>جایگزین</b> کنید — دو فایل جدید <code>Logging/RtlConsoleShaper.cs</code> و <code>Logging/PersianConsoleTextFormatter.cs</code> هم داخل بسته هستند</li>
<li>اگر فایل <code>appsettings.json</code> را روی سیستم خودتان شخصی‌سازی کرده‌اید (مثلاً رشتهٔ اتصال SQL)، به‌جای جایگزین‌کردن کل فایل فقط بخش <code>Serilog ← WriteTo ← Console ← Args ← formatter</code> را مطابق فایل همین بسته اضافه کنید</li>
<li>Solution را بیلد و سرور API را اجرا کنید — این نسخه <b>مهاجرت دیتابیس جدیدی ندارد</b></li>
<li>در نخستین اجرای API، بذرافشان داده‌های مرجع این‌بار کامل و بدون خطا اجرا می‌شود (قبلاً وسط کار کرش می‌کرد) — پیام‌های موفقیت همهٔ بذرها را در کنسول ببینید</li>
</ol>
<div class="info"><b>نمایش فارسی کنسول:</b> از این پس پیام‌های فارسی لاگ سرور در کنسول ویندوز با <b>حروف متصل و ترتیب درست</b> نمایش داده می‌شوند («راه‌اندازی» دیگر «ی ز ا د ن ا ه ا ر» دیده نمی‌شود). این اصلاح خودکار فقط در کنسول قدیمی (conhost) فعال می‌شود؛ در Windows Terminal و فایل‌های لاگ <code>Logs/api-*.log</code> متن همیشه به‌صورت استاندارد و سالم ثبت می‌شود.</div>
<h3>خلاصهٔ تغییرات مهم این نسخه</h3>
<ul>
<li>💊 <b>رفع کرش «کلید تکراری Isosorbide Dinitrate»</b> در بذرافشان داده‌های مرجع — ریشه: داروهای هم‌نام با دوز متفاوت در کاتالوگ + الگوی شکنندهٔ GroupBy+ToDictionaryAsync؛ همین خطا در کارگر پشتیبان‌گیری و اطلاعات پشتیبانی لایسنس هم پیشگیرانه رفع شد</li>
<li>🛣️ <b>رفع خطای ۵۰۰ پس از ثبت موفق</b> (No route matches the supplied values) — ثبت طرح درمان دندانپزشکی/تصویربرداری/چشم/فیزیوتراپی و <b>ثبت پرداخت</b> داده را ذخیره می‌کردند اما به کلاینت خطای «خطای غیرمنتظره» برمی‌گرداندند و کاربر گمان می‌کرد ثبت نشده است</li>
<li>🔌 <b>قطع اتصال کلاینت دیگر خطای ERR نیست</b> — «Unexpected end of request content» استثنای زیرساختی Kestrel است و از این پس بی‌سروصدا مدیریت می‌شود</li>
<li>🔇 حذف دو هشدار مزمن لاگ: <code>Savepoints are disabled because MARS</code> و <code>Failed to determine the https port</code></li>
<li>📖 <b>نمایش صحیح فارسی کنسول API</b> — موتور شکل‌دهی اشکال نمایشی + چینش بصری راست‌به‌چپ با حفظ واژه‌های لاتین و ارقام</li>
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
               f"بستهٔ به‌روزرسانی 2.4.2 → 2.4.3\nتعداد فایل: {len(manifest_lines)}\n\n" + "\n".join(manifest_lines))

print(f"✅ ساخته شد: {OUT} ({OUT.stat().st_size / 1024:.0f} کیلوبایت)")
print(f"— راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME}")
