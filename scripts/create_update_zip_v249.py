#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت → v2.4.9 — وصلهٔ فوری چرخهٔ سی‌وششم (ترمیم خطای ساخت v2.4.8).

بستهٔ حداقلیِ دقیق: فقط ۵ فایلی که نسبت به v2.4.8 تغییر کرده‌اند. مبنای این
انتخاب: بستهٔ v2.4.8 «تجمعی» بود و خطاهای بیلد گزارش‌شدهٔ مالک دقیقاً از
فایل‌های جدید همان نسخه است ⇒ فایل‌های v2.4.8 به ایستگاه او رسیده و این
وصلهٔ کوچک برای رساندن او به HEAD کافی است. (اگر ایستگاهی هنوز بستهٔ تجمعی
۲.۴.۸ را نگرفته باشد، باید اول آن را از رلیز v2.4.8 نصب کند.)

گاردهای انسجام این چرخه:
  • نشانه‌های سلامت هر سه رفع (using رسانه‌ای / کامنت عادی / نسخهٔ ۲.۴.۹)
  • گارد ترتیب استاتیک (جدید): TemplateTypes باید در متنِ ComplementaryViewModel
    «قبل از» TemplateFilterOptions تعریف شده باشد — همان ریشهٔ CS8604 و کرش
    پنهان TypeInitializationException
  • گارد یکتایی: بدنهٔ آرایهٔ ۹تایی و using رسانه‌ای فقط یک‌بار باشند
  • ممیزی خودکار using های رسانه‌ای کل سورس (audit_csharp_usings.py — جدید)
  • اعتبارسنجی XML همهٔ XAMLها (validate_xaml.py)
  • تطبیق دقیق فهرست بسته با دیف واقعی گیت v2.4.8..HEAD
"""
import pathlib
import subprocess
import sys
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.8"    # بستهٔ حداقلی نسبت به نسخهٔ پیش — برخلاف v2.4.8 که تجمعی بود
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.8-to-2.4.9.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.9.html"

# مستثنی‌ها: اسکریپت‌های بسته‌بندی/انتشار/ممیزی داخلی و پوشهٔ docs — به ایستگاه مالک ربطی ندارند
# (نکته برای چرخه‌های بعد: scripts/audit_csharp_usings.py از این پس در دیف‌ها هست — همین مستثنی را نگه دارید)
EXCLUDES = [
    '":(exclude)docs/*"',
    '":(exclude)scripts/create_update_zip_*.py"',
    '":(exclude)scripts/publish_release_*.py"',
    '":(exclude)scripts/audit_csharp_usings.py"',
]

# ─── فهرست بسته = دیف واقعی v2.4.8..HEAD ───
diff_cmd = f'git -C "{REPO}" diff --name-only {BASE}..HEAD -- . ' + " ".join(EXCLUDES)
diff = subprocess.run(diff_cmd, shell=True, capture_output=True, text=True, check=True).stdout.split()
CHANGED = sorted(set(diff))

# ─── گارد انسجام مثبت — نشانه‌های سلامت سه رفع این چرخه ───
COHERENCE_CHECKS = [
    # ۱) خطای بیلد CS0103/CS0246 — using رسانه‌ای مبدل HexBrush
    ("src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs",
     "using System.Windows.Media;", "using رسانه‌ای برای HexBrushConverter"),
    ("src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs",
     "class HexBrushConverter", "خود مبدل هگز (دست‌نخورده)"),
    # ۲) هشدار CS1587 — کامنت عادی به‌جای XML روی پارامتر رکورد
    ("src/Sahand.ClinicPro.Application/Interfaces/ILicenseService.cs",
     "// چرخهٔ سی‌وپنجم: رنگ انتخابی فروشنده", "کامنت عادیِ پارامتر Color"),
    # ۳) باگ استاتیک — تعریف منتقل‌شده + کامنت هشدار
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs",
     "public static (string key, string fa)[] TemplateTypes", "تک‌منبع انواع سند"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs",
     "⚠️ این ترتیب را جابه‌جا نکنید", "کامنت هشدار ترتیب استاتیک"),
    # نسخه و CHANGELOG
    ("Directory.Build.props", "<Version>2.4.9</Version>", "نسخهٔ 2.4.9"),
    ("CHANGELOG.md", "[v2.4.9]", "ثبت وصلهٔ سی‌وششم در CHANGELOG"),
]

# ─── نشانه‌های منفی — الگوهای معیوب باید حذف شده باشند ───
NEGATIVE_CHECKS = [
    # کامنت XML روی پارامتر رکورد (CS1587)
    ("src/Sahand.ClinicPro.Application/Interfaces/ILicenseService.cs",
     "/// <summary>چرخهٔ سی‌وپنجم: رنگ انتخابی", "کامنت XML نامعتبر حذف شد"),
    # نسخهٔ کهنه
    ("Directory.Build.props", "<Version>2.4.8</Version>", "نسخهٔ کهنه حذف شد"),
    # متن راهنمای قدیمی تعریف TemplateTypes در جای نادرست (کامنت قبلی بدون هشدار ترتیب)
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs",
     "/// <summary>گزینه‌های نوع سند</summary>\n    public static", "تعریف قدیمیِ دیرهنگام حذف شد"),
]

print("■ اجرای گاردهای انسجام چرخهٔ ۳۶…")
for rel, needle, desc in COHERENCE_CHECKS:
    assert needle in (REPO / rel).read_text(encoding="utf-8-sig"), f"❌ نشانهٔ سلامت یافت نشد [{desc}] در {rel}"
    print(f"  ✅ {desc}")
for rel, needle, desc in NEGATIVE_CHECKS:
    assert needle not in (REPO / rel).read_text(encoding="utf-8-sig"), f"❌ الگوی معیوب هنوز هست [{desc}] در {rel}"
    print(f"  ✅ {desc}")

# ─── گارد ترتیب استاتیک (قلب رفع CS8604) — تعریف باید قبل از مصرف‌کننده باشد ───
vm_txt = (REPO / "src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs").read_text(encoding="utf-8-sig")
i_types = vm_txt.find("public static (string key, string fa)[] TemplateTypes")
i_filter = vm_txt.find("TemplateFilterOptions { get; }")
assert 0 < i_types < i_filter, \
    f"❌ ترتیب استاتیک شکسته است! TemplateTypes (موقعیت {i_types}) باید قبل از TemplateFilterOptions (موقعیت {i_filter}) باشد"
print("  ✅ گارد ترتیب استاتیک: TemplateTypes قبل از TemplateFilterOptions تعریف شده")
assert vm_txt.count("TemplateTypes { get; }") == 1, "❌ تعریف TemplateTypes باید دقیقاً یک‌باشد"
print("  ✅ تک‌تعریفی بودن TemplateTypes")

# گارد یکتایی using رسانه‌ای (ضد تکرار احتمالی در چرخه‌های بعد)
cc_txt = (REPO / "src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs").read_text(encoding="utf-8-sig")
assert cc_txt.count("using System.Windows.Media;") == 1, "❌ using System.Windows.Media باید دقیقاً یک‌بار باشد"
print("  ✅ using رسانه‌ای دقیقاً یک‌بار درج شده")

# ─── ممیزی خودکار using های رسانه‌ای کل سورس (جدید این چرخه) ───
res = subprocess.run([sys.executable, str(REPO / "scripts" / "audit_csharp_usings.py")],
                     capture_output=True, text=True)
assert res.returncode == 0, f"❌ ممیزی using های رسانه‌ای ناموفق:\n{res.stdout}{res.stderr}"
print(f"  ✅ {res.stdout.strip().splitlines()[-1]}")

# ─── گارد نهایی XML ───
res = subprocess.run([sys.executable, str(REPO / "scripts" / "validate_xaml.py")],
                     capture_output=True, text=True)
assert res.returncode == 0, f"❌ اعتبارسنجی XAML ناموفق:\n{res.stdout}"
print("  ✅ هر ۸۲ فایل XAML معتبرند")

# ─── تطبیق فهرست بسته با دیف واقعی ───
assert set(CHANGED) == set(diff), "❌ عدم تطابق فهرست بسته با دیف گیت"
EXPECTED = {
    "CHANGELOG.md",
    "Directory.Build.props",
    "src/Sahand.ClinicPro.Application/Interfaces/ILicenseService.cs",
    "src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs",
    "src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs",
}
assert set(CHANGED) == EXPECTED, \
    f"❌ فهرست بسته با انتظار این وصله فرق دارد:\n  جاافتاده: {EXPECTED - set(CHANGED)}\n  اضافه: {set(CHANGED) - EXPECTED}"
print(f"  ✅ بستهٔ {BASE}→HEAD دقیقاً {len(CHANGED)} فایل دارد (مطابق انتظار وصله)")

# ─── راهنمای HTML فارسی ───
GUIDE_HTML = """<!DOCTYPE html>
<html lang="fa" dir="rtl"><head><meta charset="utf-8">
<title>راهنمای به‌روزرسانی به ۲.۴.۹</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:820px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}h3{color:#1565C0;margin-top:22px}ol,ul{line-height:2.1}b{color:#2E7D32}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
.important{background:#FFEBEE;border-radius:10px;padding:14px 18px;border-right:4px solid #C62828;margin:14px 0}
.info{background:#E3F2FD;border-radius:10px;padding:12px 16px;border-right:4px solid #1E88E5;margin:14px 0}</style></head><body><div class="card">
<h1>📦 وصلهٔ فوری سهند کلینیک‌پرو — نسخهٔ ۲.۴.۹ (ترمیم خطای ساخت ۲.۴.۸)</h1>

<div class="important"><b>⭐ این بسته «وصلهٔ ۵ فایلی» است و بر مبنای ۲.۴.۸ ساخته شده</b><br>
این نسخه خطاهای Rebuild بستهٔ ۲.۴.۸ را ترمیم می‌کند. اگر بستهٔ تجمعی ۲.۴.۸ را هنوز نصب نکرده‌اید، <b>اول</b> آن را از رلیز v2.4.8 نصب کنید و بعد این وصله را.</div>

<div class="ok">محتوا: <b>۵ فایل</b> — سه فایل ترمیم‌شدهٔ خطا (مبدل‌ها، قرارداد لایسنس، ViewModel فرم‌های تکمیلی) + فایل نسخه + CHANGELOG. همه را با همان ساختار پوشه‌ای روی <code>E:\\Sahand_ClinicPro</code> کپی و <b>جایگزین</b> کنید.</div>

<h3>سه خطایی که این وصله ترمیم می‌کند</h3>
<ul>
<li>🐞 <b>خطای بیلد (CS0103/CS0246)</b> در <code>CommonConverters.cs</code> — <code>using System.Windows.Media</code> برای مبدل رنگ پیام‌های لایسنس جا افتاده بود. خطای <code>CS0006</code> پروژهٔ تست هم فقط دنبالهٔ همین خطاست و با ساخته‌شدن بیلد <b>خودبه‌خود</b> برطرف می‌شود — فایل دیگری برایش عوض نکنید.</li>
<li>📋 <b>باگ پنهان (CS8604)</b> در <code>ComplementaryViewModel.cs</code> — مهم‌تر از هشدار! ترتیب مقداردهنده‌های استاتیک نادرست بود و حتی بعد از موفقیت بیلد هم بازکردن صفحهٔ «فرم‌های تکمیلی» کرش می‌کرد؛ ترتیب اصلاح شد.</li>
<li>📝 <b>هشدار CS1587</b> در <code>ILicenseService.cs</code> — کامنت XML روی پارامتر رکورد در C# 12 معتبر نیست؛ به کامنت عادی تبدیل شد.</li>
</ul>

<h3>مراحل نصب</h3>
<ol>
<li>ویژوال استودیو و برنامهٔ دسکتاپ را کامل ببندید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی <code>E:\\Sahand_ClinicPro</code> کپی و <b>جایگزین</b> کنید (۵ فایل)</li>
<li>در ویژوال استودیو: <b>BUILD ← Clean Solution</b> و سپس <b>Rebuild Solution</b></li>
<li>سرور API تغییری در این بسته ندارد (دو فایل تغییرکردهٔ غیر دسکتاپی صرفاً کامنت/قراردادند و بازبیلد سرور هم بی‌ضرر است)</li>
</ol>

<h3>بعد از نصب بررسی کنید</h3>
<ul>
<li>✅ <b>Rebuild بدون هیچ خطای CS0103/CS0246/CS0006</b> تمام می‌شود و Error List هم از CS1587 و CS8604 خالی است</li>
<li>✅ زیر دکمهٔ «خروج از حساب» در منوی کناری باید <b>«نسخه ۲.۴.۹»</b> دیده شود — اگر نبود، فایل‌ها جایگزین نشده‌اند و مراحل را تکرار کنید</li>
<li>✅ صفحهٔ <b>«فرم‌های تکمیلی»</b> بدون هیچ کرشی باز می‌شود و تب طراح قالب چاپ با فیلتر موجودیت کار می‌کند (این صفحه دقیقاً جایی بود که باگ پنهان کرش می‌داد)</li>
<li>✅ چک‌لیست ۷ موردی نسخهٔ ۲.۴.۸ (منو/چاپ/پیام‌ها/لاگ/…) را هم اگر هنوز مرور نکرده‌اید، از راهنمای همان نسخه ببینید</li>
</ul>

<div class="info"><b>چرا این بسته کوچک است؟</b> بستهٔ ۲.۴.۸ تجمعی بود (۴۴ فایل از ۲.۴.۴) و خطاهای بیلد شما دقیقاً از فایل‌های همان نسخه گزارش شد؛ یعنی فایل‌های ۲.۴.۸ به ایستگاه شما رسیده‌اند. بنابراین فقط همین ۵ فایل ترمیمی لازم است.</div>
</div></body></html>
"""

# ─── ساخت زیپ ───
OUT.parent.mkdir(parents=True, exist_ok=True)
if OUT.exists():
    OUT.unlink()

with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED) as z:
    z.writestr(GUIDE_NAME, GUIDE_HTML)
    for rel in CHANGED:
        z.write(REPO / rel, rel)
    z.writestr(MANIFEST_NAME,
               "وصلهٔ v2.4.9 — ترمیم خطای ساخت بستهٔ v2.4.8 (۵ فایل تغییرکرده نسبت به v2.4.8)\n"
               "پیش‌نیاز: بستهٔ تجمعی v2.4.8 قبلاً نصب باشد\n"
               "پس از کپی: Clean Solution سپس Rebuild Solution (چک‌لیست داخل راهنمای HTML)\n"
               + "-" * 70 + "\n" + "\n".join(CHANGED) + "\n")

print(f"\n✅ بستهٔ آپدیت ساخته شد: {OUT}")
print(f"   حجم: {OUT.stat().st_size:,} بایت | فایل‌ها: {len(CHANGED)}")
with zipfile.ZipFile(OUT) as z:
    names = z.namelist()
    print(f"   • راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME} | سورس: {len(names)-2} فایل")
