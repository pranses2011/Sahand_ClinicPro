#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت → v2.13.0 — چرخهٔ ۴۹-۵۰ (فاز ۰ تثبیت اضطراری).

بستهٔ «فقط فایل‌های تغییرکرده از تگ v2.12.0» — دیف واقعی کد (بدون docs/analysis
که مستندات داخلی مخزن است؛ راهنمای استقرار deploy-jwt.md به‌عمد داخل بسته می‌آید
چون گام نصب سرور این چرخه بدون آن ناممکن است).

گاردهای انسجام این چرخه (همه باید در اولین اجرا پاس شوند):
  • مثبت: نشانگرهای سلامت هر رفع چرخهٔ ۴۹/۵۰ در فایل‌های بسته
  • منفی: کلید قدیمی حذف‌شده نباید در هیچ فایل بسته برگردد (به‌جز فایل‌های گارد/تست)
  • تطبیق دقیق فهرست بسته با دیف واقعی گیت v2.12.0..HEAD (پس از مستثنی‌ها)
  • ممیزی خودکار using های رسانه‌ای + اعتبارسنجی XML همهٔ XAMLها
"""
import pathlib
import subprocess
import sys
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.12.0"
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.12.0-to-2.13.0.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.13.0.html"
OLD_JWT_KEY = "SahandClinicPro-Development-Secret-Key-Change-In-Production-2026"

# ─── فهرست بسته = دیف واقعی v2.12.0..HEAD (مستثنی: تحلیل‌ها/اسکریپت‌های داخلی) ───
EXCLUDES = [
    '":(exclude)docs/analysis/*"',
    '":(exclude)docs/screenshots/*"',
    '":(exclude)docs/prompts/*"',
    '":(exclude)scripts/create_update_zip_*.py"',
    '":(exclude)scripts/publish_release_*.py"',
    '":(exclude)scripts/prepend_changelog_*.py"',
]
diff_cmd = f'git -C "{REPO}" diff --name-only {BASE}..HEAD -- . ' + " ".join(EXCLUDES)
diff = subprocess.run(diff_cmd, shell=True, capture_output=True, text=True, check=True).stdout.split()
CHANGED = sorted(set(diff))

# ─── گارد انسجام مثبت — نشانگرهای سلامت رفع‌های این بسته ───
COHERENCE_CHECKS = [
    # ═══ چرخهٔ ۴۹ — کلید JWT ═══
    ("src/Sahand.ClinicPro.API/appsettings.json",
     '"SecretKey": ""', "کلید امضا خالی شد (تزریق از محیط)"),
    ("src/Sahand.ClinicPro.Infrastructure/Identity/IdentitySetup.cs",
     "insecurePatterns", "گارد الگوی ناامن چرخهٔ ۴۹"),
    ("src/Sahand.ClinicPro.Infrastructure/Identity/IdentitySetup.cs",
     "DOTNET_ENVIRONMENT", "تشخیص محیط سه‌کلیده"),
    ("src/Sahand.ClinicPro.API/Program.cs",
     '"license"', "سیاست نرخ اندپوینت‌های لایسنس"),
    ("src/Sahand.ClinicPro.API/Controllers/LicenseController.cs",
     '[EnableRateLimiting("license")]', "اعمال سقف نرخ روی کنترلر لایسنس"),
    ("tests/Sahand.ClinicPro.Infrastructure.IntegrationTests/JwtKeyGuardTests.cs",
     "Production_WithOldRepoHardcodedKey_MustThrow", "تست رگرسیون کلید پیشین مخزن"),
    ("docs/guides/deploy-jwt.md",
     "Jwt__SecretKey", "راهنمای استقرار کلید"),
    # ═══ چرخهٔ ۵۰ — انسدادهای رشته‌ای ═══
    ("src/Sahand.ClinicPro.API/Controllers/BackupController.cs",
     "GetCloudServiceAsync", "الگوی async lazy سرویس ابری"),
    ("src/Sahand.ClinicPro.API/Controllers/BackupController.cs",
     "_cloudInitLock", "قفل SemaphoreSlim"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReportsViewModel.cs",
     "var revenue = await revenueTask;", "خواندن نتیجه با await"),
    ("tests/Sahand.ClinicPro.API.IntegrationTests/BackupCloudConcurrencyTests.cs",
     "Should_Not_Deadlock", "تست ۵۰ درخواست همزمان"),
    ("tests/Sahand.ClinicPro.Desktop.UnitTests/ReportsViewModelTests.cs",
     "ShouldShowPersianErrorNotCrash", "تست پیام فارسی به‌جای کرش"),
    ("scripts/security/scan_secrets.sh",
     "گیت ۶", "اسکنر الگوهای ممنوعه"),
    # ═══ نسخه ═══
    ("Directory.Build.props",
     "<Version>2.13.0</Version>", "نسخهٔ 2.13.0"),
    ("CHANGELOG.md",
     "[v2.13.0]", "سرچشمهٔ نسخهٔ جدید"),
]

# ─── گارد منفی — کلید قدیمی در فایل‌های بسته (به‌جز فایل‌های «آشکارساز»: گارد/تست/اسکنر) ───
NEGATIVE_WHITELIST = {
    "src/Sahand.ClinicPro.Infrastructure/Identity/IdentitySetup.cs",
    "tests/Sahand.ClinicPro.Infrastructure.IntegrationTests/JwtKeyGuardTests.cs",
    "scripts/security/scan_secrets.sh",  # اسکنر برای تشخیص، الگوی کلید را دارد (آشکارساز نه مصرف)
    "scripts/create_update_zip_v2130.py",
}

def fail(msg):
    print(f"⛔ {msg}")
    sys.exit(1)

# ۱) همهٔ فایل‌های گارد باید در دیف باشند
for path, _, _ in COHERENCE_CHECKS:
    if path not in CHANGED:
        fail(f"گارد مثبت: فایل {path} در دیف v{BASE}..HEAD نیست — فهرست بسته ناقص است")

# ۲) نشانگرهای مثبت
for path, marker, desc in COHERENCE_CHECKS:
    text = (REPO / path).read_text(encoding="utf-8")
    if marker not in text:
        fail(f"گارد مثبت [{desc}]: نشانگر «{marker}» در {path} پیدا نشد")

# ۳) گارد منفی — کلید افشاشده در هیچ فایل بسته نباشد (خارج از لیست‌سفید گارد)
for path in CHANGED:
    p = REPO / path
    if not p.is_file() or path in NEGATIVE_WHITELIST:
        continue
    body = p.read_text(encoding="utf-8", errors="ignore")
    if OLD_JWT_KEY in body:
        fail(f"گارد منفی: کلید قدیمی در {path} یافت شد — نباید در بسته باشد")

# ۴) ممیزی‌های سراسری
r = subprocess.run([sys.executable, "scripts/validate_xaml.py"], cwd=REPO,
                   capture_output=True, text=True)
if "نامعتبر: 0" not in r.stdout:
    fail(f"اعتبارسنجی XAML شکست خورد:\n{r.stdout[-500:]}")
r = subprocess.run([sys.executable, "scripts/audit_csharp_usings.py"], cwd=REPO,
                   capture_output=True, text=True)
if "سالم‌اند" not in r.stdout:
    fail(f"ممیزی using ها شکست خورد:\n{r.stdout[-500:]}")

# ۵) نسخه در Build.props == CHANGELOG == نام بسته
props = (REPO / "Directory.Build.props").read_text(encoding="utf-8")
if "<Version>2.13.0</Version>" not in props:
    fail("نسخهٔ Directory.Build.props هنوز 2.13.0 نیست")

# ═══ راهنمای HTML فارسی داخل بسته ═══
GUIDE_HTML = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="UTF-8">
<title>راهنمای آپدیت 2.13.0 — فاز ۰ تثبیت اضطراری</title>
<style>
  body { font-family: Vazirmatn, Tahoma, sans-serif; background:#f8fafc; color:#0f172a; margin:0; padding:24px; line-height:1.9; }
  .wrap { max-width: 860px; margin: 0 auto; }
  h1 { color:#0d9488; border-bottom:3px solid #0d9488; padding-bottom:12px; font-size:1.5rem; }
  h2 { color:#be123c; margin-top:32px; font-size:1.15rem; }
  .warn { background:#fff7ed; border:1px solid #fdba74; border-right:5px solid #ea580c; border-radius:8px; padding:16px 20px; margin:16px 0; }
  .ok { background:#f0fdf4; border:1px solid #86efac; border-right:5px solid #16a34a; border-radius:8px; padding:16px 20px; margin:16px 0; }
  code { background:#e2e8f0; border-radius:4px; padding:2px 8px; font-size:0.9em; direction:ltr; display:inline-block; }
  .cmd { background:#1e293b; color:#e2e8f0; border-radius:8px; padding:12px 16px; direction:ltr; text-align:left; font-family:Consolas,monospace; margin:8px 0; overflow-x:auto; }
  ol li, ul li { margin: 6px 0; }
  table { border-collapse:collapse; width:100%; margin:12px 0; }
  th, td { border:1px solid #cbd5e1; padding:8px 12px; text-align:right; font-size:0.92em; }
  th { background:#f1f5f9; }
</style>
</head>
<body><div class="wrap">
<h1>📦 آپدیت 2.12.0 → 2.13.0 — چرخهٔ ۴۹-۵۰ (فاز ۰ تثبیت اضطراری)</h1>

<p>این بسته <b>کلید امضای JWT را از مخزن حذف می‌کند</b> (رفع BUG-01 نقشه راه)، دو انسداد رشته‌ای خطرناک را برطرف می‌کند و اسکنر الگوهای ممنوعه را اضافه می‌کند.</p>

<div class="warn">
⚠️ <b>گام الزامی سرور — پیش از جایگزینی فایل‌ها:</b><br>
متغیر محیطی <code>Jwt__SecretKey</code> را با یک کلید تصادفی جدید (حداقل ۶۴ کاراکتر) تنظیم کنید. بدون آن، سرور API در محیط تولید <b>عمداً بالا نمی‌آید</b> و پیام فارسی راهنما نشان می‌دهد.
<div class="cmd">REM ساخت کلید جدید در PowerShell:
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])

REM تنظیم برای کل کاربر (پس از آن سرور را کامل ببندید و دوباره باز کنید):
setx Jwt__SecretKey "کلید-تولیدی-بالا"</div>
راهنمای کامل با ۵ سناریوی نصب (کنسول/سرویس/IIS/...): فایل <code>docs/guides/deploy-jwt.md</code> در همین بسته.
</div>

<h2>📋 ترتیب نصب (مهم — اول کلید، بعد فایل‌ها)</h2>
<ol>
  <li>کلید تصادفی بسازید و <code>Jwt__SecretKey</code> را تنظیم کنید (بالا)</li>
  <li>پوشهٔ <code>src</code> و فایل‌های این بسته را روی سورس پروژه جایگزین کنید (ساختار مسیرها رعایت شده)</li>
  <li>سولوشن را <b>Clean + Rebuild</b> کنید (هر دو پروژهٔ API و Desktop تغییر دارند — اگر Error List بعد از Rebuild هنوز پر بود، <code>Clean-VS-Cache.bat</code> را اجرا و دوباره Rebuild کنید)</li>
  <li>سرور API را ری‌استارت کنید — لاگ باید «✅ API درمانگاه سهند راه‌اندازی شد» بدهد</li>
  <li>نسخهٔ ۲.۱۳.۰ را در «درباره» یا خروجی بیلد ببینید</li>
  <li>چند کاربر تستی یک‌بار خارج و دوباره وارد شوند (توکن‌های قدیمی با کلید جدید باطل‌اند — کاملاً طبیعی)</li>
</ol>

<h2>✅ چک‌لیست پس از نصب</h2>
<table>
<tr><th>بررسی</th><th>انتظار</th></tr>
<tr><td>لاگ آغازین سرور API</td><td>«راه‌اندازی شد و در حال سرویس‌دهی است» — بدون پیام ⛔ کلید</td></tr>
<tr><td>صفحهٔ گزارش‌ها</td><td>بارگذاری نرمال؛ در قطع شبکه‌ی شبیه‌سازی‌شده پیام فارسی (نه هنگ برنامه)</td></tr>
<tr><td>پشتیبان ابری → وضعیت</td><td>پاسخ نرمال (رفع انسداد رشته‌ای چرخهٔ ۵۰)</td></tr>
<tr><td>فعال‌سازی لایسنس</td><td>همان‌طور کار می‌کند (سقف نرخ ۲۰/دقیقه فقط برای سوءاستفاده است)</td></tr>
<tr><td>اسکنر پیشگیری</td><td><code>bash scripts/security/scan_secrets.sh</code> در Git Bash → «همه گیت‌ها سبز است»</td></tr>
</table>

<h2>🧯 اگر سرور بالا نیامد</h2>
<ul>
  <li>پیام «کلید تنظیم نشده» ⟵ <code>Jwt__SecretKey</code> ست نشده یا نامش غلط تایپی است (دو خط زیر <code>__</code>)</li>
  <li>پیام «الگوی ناامن» ⟵ کلید شما هنوز حاوی عبارت‌هایی مثل Development-Secret است — کلید تصادفی خالص بگذارید</li>
  <li>پس از <code>setx</code> سرور را کامل ببندید و دوباره باز کنید (setx برای پروسه‌های قبلی اثر ندارد)</li>
  <li>در محیط توسعه (ویژوال استودیو) هیچ کاری لازم نیست — کلید خالی مجاز است</li>
</ul>

<div class="ok">
📌 <b>خلاصهٔ تغییرات این نسخه:</b> حذف کلید JWT از مخزن + گارد سه‌لایه تولید + سقف نرخ اندپوینت‌های لایسنس + رفع انسداد رشته‌ای پشتیبان ابری (SemaphoreSlim) + رفع خواندن هم‌گام نتیجهٔ Taskها در گزارش‌ها + اسکنر ۶ گیته الگوهای ممنوعه + ۷ تست رگرسیون جدید — جزئیات کامل در <code>CHANGELOG.md</code>.
</div>
</div></body></html>"""

# ═══ ساخت زیپ ═══
if OUT.exists():
    OUT.unlink()
with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED) as zf:
    for path in CHANGED:
        zf.write(REPO / path, path)
    zf.writestr(GUIDE_NAME, GUIDE_HTML)
    zf.writestr(MANIFEST_NAME,
                "فایل‌های تغییرکرده v2.12.0 → v2.13.0 (چرخهٔ ۴۹-۵۰ — فاز ۰)\n" +
                "=" * 60 + "\n" + "\n".join(CHANGED) +
                "\n" + GUIDE_NAME + "\n" + MANIFEST_NAME + "\n")

# ═══ گزارش ═══
print("✅ بستهٔ آپدیت ساخته شد:", OUT)
print(f"   تعداد فایل‌ها: {len(CHANGED)} + راهنما + مانیفست")
print(f"   حجم: {OUT.stat().st_size:,} بایت")
for path in CHANGED:
    print("   •", path)
