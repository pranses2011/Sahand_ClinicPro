#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.2.1 → v2.3.0 — چرخهٔ بیست‌وششم (گزارش هجدهم مالک).
فقط فایل‌های تغییرکردهٔ همین رلیز (کامیت 8b53a6b = v2.2.1 تا HEAD=4bb4c12) با
همان ساختار پوشه‌ای مخزن + مانیفست متنی.

گاردهای انسجام: نشانه‌های سلامت رفع‌های کلیدی این نسخه باید در سورس موجود
باشند وگرنه اسکریپت متوقف می‌شود.
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "8b53a6b"   # v2.2.1
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.2.1-to-2.3.0.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.3.0.html"

# ─── گارد انسجام مثبت — نشانه‌های سلامت رفع‌های کلیدی این نسخه ───
COHERENCE_CHECKS = [
    # قانون سراسری ایندکس‌های یکتای تهی‌پذیر
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/ClinicProDbContext.cs",
     "IS NOT NULL", "قانون فیلتر ایندکس‌های یکتای تهی‌پذیر"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261009114111_Cycle26NullableUniqueIndexes.cs",
     "IX_Employee_PersonnelCode", "مهاجرت ۷۲ ایندکس"),
    # لاگ فارسی سرور
    ("src/Sahand.ClinicPro.API/Program.cs",
     "Console.OutputEncoding", "کدپیج UTF-8 کنسول"),
    # مقادیر قبلی لاگ عملیات
    ("src/Sahand.ClinicPro.API/Middleware/AuditLogMiddleware.cs",
     "TryCaptureOldValues", "ثبت مقادیر قبلی"),
    # کرش گزارش‌ساز
    ("src/Sahand.ClinicPro.Desktop/Views/AdhocReportWindow.xaml.cs",
     "هرگز null ست نمی‌شود", "رفع FontFamily تهی"),
    # سرریز مبلغ
    ("src/Sahand.ClinicPro.Desktop/Helpers/Money.cs",
     "MaxRial", "سقف امن مبلغ"),
    # بکاپ خارجی
    ("src/Sahand.ClinicPro.API/Controllers/BackupController.cs",
     "upload-restore", "اندپوینت بارگذاری-و-بازیابی"),
    ("src/Sahand.ClinicPro.Infrastructure/Services/Backup/BackupManager.cs",
     "CanWriteToDirectory", "آزمون نوشتن پوشهٔ استخراج"),
    # زوم چاپ
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     "PART_ContentHost", "قالب مینیمال نمایشگر"),
    # ساعت عقربه‌ای
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml.cs",
     "رفع چرخش یک‌خانه‌ای صفحه", "چیدمان صحیح صفحهٔ ساعت"),
    # فونت ارقام فارسی
    ("src/Sahand.ClinicPro.Desktop/App.xaml",
     "App.CodeFont", "منبع فونت ارقام/کد"),
    # منوی کناری
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     "FFD54F", "سربرگ طلایی گروه‌ها"),
]

# نشانه‌های منفی — نباید وجود داشته باشند
NEGATIVE_CHECKS = [
    ("src/Sahand.ClinicPro.Desktop/Views/AdhocReportWindow.xaml.cs",
     "FontFamily = i == 0 ? null", "الگوی FontFamily تهی حذف شده"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "InsuranceCompanies[InsuranceIndex - 1]", "آف‌بای‌وان بیمهٔ پذیرش حذف"),
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
changed = run(f"git diff --name-only --diff-filter=ACMR {BASE}..HEAD -- src tests Directory.Build.props")
files = [f for f in changed.splitlines() if f.strip()]
if not files:
    raise SystemExit("⛔ هیچ فایل تغییرکرده‌ای یافت نشد")
print(f"📦 {len(files)} فایل تغییرکرده")

# ─── راهنمای فارسی HTML ───
guide = """<!DOCTYPE html>
<html lang="fa" dir="rtl"><head><meta charset="utf-8">
<title>راهنمای به‌روزرسانی ۲.۲.۱ به ۲.۳.۰</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:760px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}ol{line-height:2.1}b{color:#2E7D32}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
ul{line-height:2}</style></head><body><div class="card">
<h1>📦 بستهٔ به‌روزرسانی سهند کلینیک‌پرو — ۲.۲.۱ به ۲.۳.۰</h1>
<div class="ok">این بسته فقط فایل‌های تغییرکرده را دارد؛ همه را با همان ساختار پوشه‌ای روی نسخهٔ نصب‌شدهٔ <b>۲.۲.۱</b> کپی و جایگزین کنید.</div>
<h3>مراحل نصب</h3>
<ol>
<li>برنامهٔ دسکتاپ و <b>سرور API</b> را کامل ببندید (مطمئن شوید پردازهٔ Sahand.ClinicPro.API در حال اجرا نیست)</li>
<li>از پوشهٔ نصب فعلی، یک نسخهٔ پشتیبان (کپی کامل) تهیه کنید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی پوشهٔ نصب کپی و <b>جایگزین</b> کنید</li>
<li>سرور API را اجرا کنید — <b>مهاجرت دیتابیس (۷۲ ایندکس) خودکار در نخستین اجرا اعمال می‌شود</b></li>
<li>برنامهٔ دسکتاپ را اجرا و از نسخهٔ ۲.۳.۰ در Splash مطمئن شوید</li>
</ol>
<div class="warn"><b>نکتهٔ مهم:</b> پیش از اجرای سرور، SQL Server در حال اجرا باشد و کاربر سرویس به پوشهٔ پشتیبان‌ها (پیش‌فرض <code>C:\\Backups\\SahandClinicPro</code>) دسترسی کامل داشته باشد.</div>
<h3>خلاصهٔ تغییرات مهم این نسخه</h3>
<ul>
<li>🚨 رفع قطعی خطای «خطای غیرمنتظره در سرور» هنگام ثبت/ویرایش کاربر و پرسنل (ایندکس کد پرسنلی تهی) — نقش‌ها و دسترسی‌ها هم اکنون ذخیره می‌شوند</li>
<li>📋 لاگ عملیات: نمایش «مقادیر قبلی» در جزئیات تغییرات</li>
<li>🇮🇷 لاگ سرور API متن فارسی را درست نمایش می‌دهد + فایل لاگ UTF-8 روزانه در <code>Logs/</code></li>
<li>🧑‍⚕️ لیست پرسنل: ویرایش کامل، ویرایش حقوقی و حذف نرم + تاریخ استخدام با انتخابگر شمسی</li>
<li>🧾 بیمهٔ لیست بیماران درست نمایش داده می‌شود (تامین اجتماعی دیگر «آزاد» نشان داده نمی‌شود)</li>
<li>💰 تعرفهٔ آزاد همیشه ۱۰۰٪ سهم بیمار؛ تعرفهٔ پذیرش خدمت درمانی بر اساس بیمهٔ منتخب</li>
<li>💾 بازیابی پشتیبان از هارد/فلش + رفع خطای «access to the path is denied»</li>
<li>🖨 زوم چاپ نسخه یکدست شد و کد ملی بیمار در سربرگ چاپ می‌نشیند</li>
<li>⏰ صفحهٔ ساعت عقربه‌ای اصلاح شد (اعداد در جای درست)</li>
<li>🦷 رنگ وضعیت دندان‌ها بی‌درنگ تغییر می‌کند</li>
<li>🎨 منوهای اصلی دارای زیرمنو با رنگ طلایی متمایز + نسخهٔ برنامه در Splash</li>
<li>🛡 فرم‌های پرسنل/تعرفه پیش از بستن با تغییرات ذخیره‌نشده تأیید می‌گیرند</li>
<li>🔤 فونت روی ارقام فارسی اعمال می‌شود (موبایل/کد ملی/سن و…)</li>
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
               f"بستهٔ به‌روزرسانی 2.2.1 → 2.3.0\nتعداد فایل: {len(manifest_lines)}\n\n" + "\n".join(manifest_lines))

print(f"✅ ساخته شد: {OUT} ({OUT.stat().st_size / 1024:.0f} کیلوبایت)")
print(f"— راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME}")
