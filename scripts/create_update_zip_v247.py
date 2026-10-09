#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.4.6 → v2.4.7 — وصلهٔ فوری خطای ساخت MC3000.

محتوا: فقط فایل‌های تغییرکردهٔ همین رلیز (تگ v2.4.6 تا HEAD) با همان
ساختار پوشه‌ای مخزن + مانیفست متنی changed-files.txt + راهنمای فارسی HTML.

گاردهای انسجام: نشانهٔ سلامتِ اصلاح (کوتیشن کامل Converter) باید در سورس
موجود باشد، الگوی معیوب باید حذف شده باشد، نسخه ۲.۴.۷ ثبت شده باشد و
تجزیهٔ XML هر ۸۲ فایل XAML سبز باشد — وگرنه اسکریپت متوقف می‌شود تا
هیچ‌گاه بستهٔ معیوب به دست مالک نرسد.
"""
import pathlib
import subprocess
import sys
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.6"    # رلیز چرخهٔ سی‌وسوم
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.6-to-2.4.7.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.7.html"

# ─── فایل‌های این رلیز — دقیقاً همان دیف تگ پایه تا HEAD ───
CHANGED = [
    "src/Sahand.ClinicPro.Desktop/Views/InsuranceView.xaml",
    "Directory.Build.props",
    "CHANGELOG.md",
    "scripts/validate_xaml.py",
]

# ─── گارد انسجام مثبت — نشانه‌های سلامت این رلیز ───
COHERENCE_CHECKS = [
    # ۱) رفع اصلی — کوتیشن کامل دور مقدار Converter
    ("src/Sahand.ClinicPro.Desktop/Views/InsuranceView.xaml",
     'Converter="{StaticResource CurrencyRial}"',
     "کوتیشن کامل مبدل ارز ستون قیمت تعرفه"),
    # ۲) کامنت ریشه‌یابی در محل خطا
    ("src/Sahand.ClinicPro.Desktop/Views/InsuranceView.xaml",
     "اصلاح خطای MC3000",
     "کامنت توضیحی ریشهٔ خطا در محل"),
    # ۳) نسخه
    ("Directory.Build.props",
     "<Version>2.4.7</Version>", "نسخهٔ 2.4.7"),
    # ۴) ثبت چرخه در CHANGELOG
    ("CHANGELOG.md",
     "[v2.4.7]", "ثبت وصله در CHANGELOG"),
]

# ─── نشانه‌های منفی — الگوی معیوب باید حذف شده باشد ───
NEGATIVE_CHECKS = [
    # الگوی دقیق بیلد‌شکننده: «=» بی‌کوتیشن و «}"» یتیم پس از آن
    ("src/Sahand.ClinicPro.Desktop/Views/InsuranceView.xaml",
     'Converter={StaticResource CurrencyRial}"',
     "الگوی MC3000 حذف شد"),
    # نسخهٔ کهنه
    ("Directory.Build.props",
     "<Version>2.4.6</Version>", "نسخهٔ کهنه حذف شد"),
]

# ─── ۰) گاردها پیش از هر کاری اجرا می‌شوند ───
print("■ اجرای گاردهای انسجام…")
for rel, needle, desc in COHERENCE_CHECKS:
    text = (REPO / rel).read_text(encoding="utf-8")
    assert needle in text, f"❌ نشانهٔ سلامت یافت نشد [{desc}] در {rel}"
    print(f"  ✅ {desc}")
for rel, needle, desc in NEGATIVE_CHECKS:
    text = (REPO / rel).read_text(encoding="utf-8")
    assert needle not in text, f"❌ الگوی معیوب هنوز هست [{desc}] در {rel}"
    print(f"  ✅ {desc}")

# تطبیق فهرست فایل‌ها با دیف واقعی گیت (تا هیچ فایلی جا نیفتد)
diff = subprocess.run(
    f'git -C "{REPO}" diff --name-only {BASE}..HEAD -- . ":(exclude)docs/*"',
    shell=True, capture_output=True, text=True, check=True).stdout.split()
diff_set = set(diff)
changed_set = set(CHANGED)
assert diff_set == changed_set, \
    f"❌ دیف واقعی با فهرست بسته یکی نیست:\n  فقط در دیف: {diff_set - changed_set}\n  فقط در فهرست: {changed_set - diff_set}"
print(f"  ✅ دیف {BASE}..HEAD دقیقاً {len(CHANGED)} فایل است — تطبیق کامل")

# گارد نهایی: تجزیهٔ XML همهٔ XAMLها باید سبز باشد
res = subprocess.run([sys.executable, str(REPO / "scripts" / "validate_xaml.py")],
                     capture_output=True, text=True)
assert res.returncode == 0, f"❌ اعتبارسنجی XAML ناموفق:\n{res.stdout}"
print("  ✅ هر ۸۲ فایل XAML معتبرند")

# ─── ۱) راهنمای HTML فارسی ───
GUIDE_HTML = """<!DOCTYPE html>
<html lang="fa" dir="rtl"><head><meta charset="utf-8">
<title>راهنمای به‌روزرسانی ۲.۴.۶ به ۲.۴.۷</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:780px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}h3{color:#1565C0;margin-top:22px}ol{line-height:2.1}b{color:#2E7D32}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
.err{background:#FFEBEE;border-radius:10px;padding:12px 16px;border-right:4px solid #C62828;margin:14px 0}
.info{background:#E3F2FD;border-radius:10px;padding:12px 16px;border-right:4px solid #1E88E5;margin:14px 0}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
ul{line-height:2}</style></head><body><div class="card">
<h1>📦 بستهٔ به‌روزرسانی سهند کلینیک‌پرو — ۲.۴.۶ به ۲.۴.۷ (وصلهٔ فوری خطای ساخت)</h1>
<div class="ok">این بسته <b>وصلهٔ فوری</b> است و فقط <b>۴ فایل</b> تغییرکرده را دارد؛ همه را با همان ساختار پوشه‌ای روی نسخهٔ ۲.۴.۶ کپی و جایگزین کنید و سپس Solution را <b>Clean و Rebuild</b> کنید.</div>

<div class="err"><b>🐞 خطایی که این بسته رفع می‌کند</b><br>
ساخت پروژهٔ دسکتاپ با خطای
<code>MC3000: '{' is an unexpected token. The expected token is '"' or ''' — InsuranceView.xaml, Line 121</code>
متوقف می‌شد و برنامه اصلاً بیلد نمی‌شد.<br>
<b>ریشه:</b> در بروزرسانی قبلی (تعرفه‌های بیمه)، کوتیشنِ آغازینِ مقدار <code>Converter</code> ستون «قیمت تعرفه» جا افتاده بود و XML فایل بیمه را نامعتبر می‌کرد. الان کامل و درست شده است.</div>

<h3>مراحل نصب</h3>
<ol>
<li>ویژوال استودیو را ببندید (پنجره‌های برنامهٔ دسکتاپ و سرور API هم بسته باشند)</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی پوشهٔ پروژه (<code>E:\\Sahand_ClinicPro</code>) کپی و <b>جایگزین</b> کنید — ۴ فایل:</li>
<ul>
<li><code>src\\Sahand.ClinicPro.Desktop\\Views\\InsuranceView.xaml</code> ← اصلاح اصلی</li>
<li><code>Directory.Build.props</code> ← شمارهٔ نسخه ۲.۴.۷</li>
<li><code>CHANGELOG.md</code> ← ثبت توضیحات</li>
<li><code>scripts\\validate_xaml.py</code> ← ابزار پایش سلامت XAML (جدید)</li>
</ul>
<li>در ویژوال استودیو: <b>BUILD ← Clean Solution</b> و سپس <b>Rebuild Solution</b> — یا اسکریپت <code>Clean-VS-Cache.bat</code> ریشهٔ پروژه را اجرا کنید</li>
<li>سرور API را اجرا و برنامهٔ دسکتاپ را باز کنید</li>
</ol>

<h3>بعد از نصب بررسی کنید</h3>
<ul>
<li>✅ <b>بیلد بدون خطا</b>: دیگر هیچ خطای MC3000 در پنجرهٔ Error List نیست و Rebuild موفق می‌شود</li>
<li>✅ <b>صفحهٔ بیمه</b>: تب «تعرفه‌ها» باز می‌شود و ستون «قیمت تعرفه» مبالغ را با جداکنندهٔ هزارگان و ارقام فارسی نشان می‌دهد</li>
<li>✅ <b>نسخهٔ درون منو</b>: زیر دکمهٔ «خروج از حساب» در منوی کناری باید <b>۲.۴.۷</b> دیده شود — اگر ۲.۴.۶ بود، یعنی فایل‌ها جایگزین نشده‌اند یا بیلد تازه اجرا نشده و مراحل بالا را تکرار کنید</li>
</ul>

<div class="info"><b>🛡️ ابزار پیشگیرانهٔ جدید — validate_xaml.py</b><br>
از این پس پوشهٔ <code>scripts</code> پروژه، ابزار <code>validate_xaml.py</code> را دارد که سلامت XML هر ۸۲ فایل XAML را یک‌جا می‌سنجد (همان چیزی که کامپایلر می‌بیند) تا این‌گونه خطاهای بیلد‌شکننده هرگز دوباره رخ ندهد. اجرای آن اختیاری است:
<code>python scripts\\validate_xaml.py</code> — خروجی «۰ فایل نامعتبر» یعنی همه‌چیز سالم است.</div>

<div class="warn"><b>⭐ یادآوری قانون آپدیت</b><br>
همهٔ ۸۲ فایل XAML پروژه با تجزیه‌گر XML بازرسی شدند و <b>تنها همین یک فایل</b> مشکل داشت — جای دیگری از برنامه این خطا وجود ندارد. اگر پس از نصب، خطای دیگری از همین خانواده (MC30xx) دیدید، بدانید فایل‌های بسته به‌درستی جایگزین نشده‌اند.</div>
</div></body></html>
"""

# ─── ۲) ساخت زیپ ───
OUT.parent.mkdir(parents=True, exist_ok=True)
if OUT.exists():
    OUT.unlink()

with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED) as z:
    # راهنمای فارسی — محتوای درون‌برنامه‌ای، نه فایل مخزن
    z.writestr(GUIDE_NAME, GUIDE_HTML)
    # فایل‌های تغییرکرده با همان ساختار پوشه‌ای مخزن
    for rel in CHANGED:
        z.write(REPO / rel, rel)
    # مانیفست متنی — فهرست فایل‌ها برای بازبینی سریع مالک
    z.writestr(MANIFEST_NAME,
               "فایل‌های تغییرکردهٔ v2.4.6 → v2.4.7 (وصلهٔ MC3000)\n"
               "پس از کپی: Clean Solution سپس Rebuild Solution\n"
               + "-" * 60 + "\n" + "\n".join(CHANGED) + "\n")

print(f"\n✅ بسته ساخته شد: {OUT}")
print(f"   حجم: {OUT.stat().st_size:,} بایت")
with zipfile.ZipFile(OUT) as z:
    for n in z.namelist():
        print("   •", n)
