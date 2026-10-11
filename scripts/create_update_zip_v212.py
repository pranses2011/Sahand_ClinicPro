# -*- coding: utf-8 -*-
"""چرخهٔ ۴۸ — بستهٔ آپدیت 2.11.0 → 2.12.0
فقط فایل‌های تغییر یافته + راهنمای فارسی HTML + مانیفست؛ طبق قانون مالک.
گاردهای انسجام: هر فایل باید در دیف تگ باشد و در زیپ حضور داشته باشد.
"""
import io, os, zipfile, subprocess, hashlib

REPO = "/home/z/my-project/Sahand_ClinicPro"
OUT = "/home/z/my-project/download/SahandClinicPro-Update-2.11.0-to-2.12.0.zip"
BASE_TAG = "3295e05"  # v2.11.0

# فایل‌های استقرار (بدون تست‌ها — تست‌ها آرتیفکت توسعه‌اند نه نصب)
DEPLOY_EXCLUDE_PREFIX = ("tests/",)

changed = subprocess.run(
    ["git", "diff", "--name-only", f"{BASE_TAG}..HEAD"],
    cwd=REPO, capture_output=True, text=True, check=True).stdout.splitlines()

deploy_files = [f for f in changed if not f.startswith(DEPLOY_EXCLUDE_PREFIX)]
assert len(deploy_files) >= 26, f"شمار فایل‌ها کمتر از انتظار است: {len(deploy_files)}"

# ═══ راهنمای فارسی HTML ═══
guide = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head><meta charset="utf-8"><title>راهنمای به‌روزرسانی ۲.۱۱.۰ → ۲.۱۲.۰</title>
<style>body{font-family:Tahoma,'Vazirmatn',sans-serif;direction:rtl;background:#f4f7f9;margin:0;padding:24px;color:#1f2937}
.card{max-width:860px;margin:auto;background:#fff;border-radius:14px;padding:24px 28px;box-shadow:0 2px 10px rgba(0,0,0,.07)}
h1{font-size:19px;color:#00695C}h2{font-size:15px;margin-top:22px;color:#1a237e}
code{background:#eef2f7;border-radius:5px;padding:1px 6px;font-family:Consolas,monospace;direction:ltr;display:inline-block}
li{margin:5px 0;line-height:1.9}.warn{background:#fff8e1;border:1px solid #ffe082;border-radius:10px;padding:10px 14px;margin-top:14px}
.ok{background:#e8f5e9;border:1px solid #a5d6a7;border-radius:10px;padding:10px 14px;margin-top:14px}
table{border-collapse:collapse;width:100%;margin-top:8px}td,th{border:1px solid #d5dde5;padding:6px 10px;font-size:12px;text-align:right}</style></head>
<body><div class="card">
<h1>📦 بستهٔ به‌روزرسانی Sahand ClinicPro — نسخهٔ ۲.۱۲.۰ (چرخهٔ چهل‌وهشتم)</h1>
<p>این بسته فقط <b>فایل‌های تغییر یافته</b> از نسخهٔ ۲.۱۱.۰ را دارد؛ فایل‌ها را در همان مسیرهای نسبی جایگزین کنید.</p>

<h2>۱) مراحل نصب</h2>
<ol>
<li>از پروژه فعلی (<code>src</code> و بیلد فعلی) نسخهٔ پشتیبان تهیه کنید.</li>
<li>محتویات پوشهٔ <code>src</code> این بسته را دقیقاً روی مسیر متناظر پروژهٔ خود کپی و جایگزین کنید (ساختار پوشه‌ها عیناً رعایت شده است).</li>
<li>در Visual Studio گزینهٔ <b>Rebuild Solution</b> را در پیکربندی <b>Release</b> اجرا کنید (پیشنهاد: ابتدا <code>Clean Solution</code>؛ برای اطمینان از نبودِ کش BAML قدیمی می‌توانید <code>Clean-VS-Cache.bat</code> ریشهٔ ریپو را هم اجرا کنید).</li>
<li><b>پروژهٔ API را هم Rebuild و سرور را ری‌استارت کنید</b> — این چرخه یک فایل در لایهٔ Application دارد (خط زمان پروندهٔ بیمار)؛ بدون ری‌بیلد سرور، تب‌های تازهٔ پاراکلینیک پرونده و ارجاع‌های ویزیت کار نمی‌کنند.</li>
<li>نرم‌افزار را اجرا کنید — باید شمارهٔ نسخهٔ <b>۲.۱۲.۰</b> را ببینید.</li>
</ol>

<h2>۲) چه چیزهایی درست شد؟ (خلاصهٔ ۱۰ گزارش)</h2>
<table>
<tr><th>موضوع</th><th>شرح</th></tr>
<tr><td>ورود اقلام مصرفی</td><td>خطای «تاریخ انقضای شمسی الزامی است» با تاریخ تکراری (پاک‌شدن کامل انتخابگر) + شمار کهنهٔ دکمهٔ «ثبت همه» + تازه‌نشدن لیست و کمبوی پذیرش</td></tr>
<tr><td>ثبت نتیجهٔ آزمایش</td><td>جدول اقلام همیشه خالی بود — پاکت پاسخ سرور باز نمی‌شد (سه باگ هم‌کلاس دیگر هم رفع شد: پرداخت با patientId=0، تحویل نسخه، زمینهٔ بیمار ویزیت)</td></tr>
<tr><td>تصویربرداری / فیزیوتراپی / چشم</td><td>نام‌های فیلد با DTO سرور ناهم‌جنس بود — درخواست تصویربرداری هرگز ثبت نمی‌شد، تکنیک‌های فیزیو و عدسی/استوانهٔ چشم بی‌صدا دور ریخته می‌شد</td></tr>
<tr><td>میزکار پزشک</td><td>ورود مستقیم پزشک به میز کار خودش + تب‌های ارجاع عملیاتی در فرم ویزیت (آزمایشگاه/تصویربرداری/فیزیوتراپی/نسخه‌نویسی با بیمار پیش‌پرشده و تداخل‌سنجی خودکار)</td></tr>
<tr><td>پروندهٔ بیمار</td><td>چهار تب تازه (تصویربرداری/دندانپزشکی/چشم‌پزشکی/فیزیوتراپی) + دابل‌کلیک جزئیات کامل روی همهٔ ردیف‌ها</td></tr>
<tr><td>دندانپزشکی</td><td>نشانگرهای دندان‌شکل با چهار هندسهٔ واقعی (آسیا/آسیای کوچک/نیش/پیش‌دندان)، چرخش فک بالا، علامت × کشیده‌شده</td></tr>
<tr><td>گزارش‌ها</td><td>گارد خودکار ۵۵ گزارش (همه سالم) + نمایش علت شکست کاتالوگ درون صفحه + خروجی تصویری با ۶ فرمت (PNG/JPEG×۲/BMP/GIF/TIFF)</td></tr>
<tr><td>چشم‌پزشکی</td><td>فارسی‌سازی برچسب‌های تخصصی: عدسی (Sphere)، استوانه (Cylinder)، محور (Axis)، افزودهٔ نزدیک‌بین (Add)، حدت بینایی، فشار داخل چشم، فاصلهٔ مردمک، دید رنگی</td></tr>
<tr><td>ظاهر</td><td>سربرگ‌های گرادیانی جدید برای صفحات آزمایشگاه و نسخه‌نویسی (هم‌الگوی سایر صفحات)</td></tr>
</table>

<h2>۳) پس از نصب چک کنید</h2>
<ul>
<li>در «ورود کالا»، دو قلم با تاریخ انقضای یکسان پشت‌سرهم به فهرست اضافه شود — نباید خطای «الزامی» بدهد و دکمه باید «ثبت همه (۲ قلم)» نشان دهد.</li>
<li>پس از ثبت، لیست لوازم مصرفی و کمبوی اقلام پذیرش (با یک بار رفت‌وبرگشت به صفحهٔ پذیرش) تازه باشند.</li>
<li>در آزمایشگاه، ثبت نتیجه برای یک درخواست — جدول اقلام باید پر باشد و ثبت موفق شود.</li>
<li>ورود با کاربرِ دارای نقش «پزشک» — مستقیم میز کار پزشک باز شود؛ در فرم ویزیت، تب آزمایش/تصویربرداری/فیزیوتراپی دکمهٔ ارسال واقعی داشته باشند.</li>
<li>پروندهٔ بیمار — چهار تب تازهٔ پاراکلینیک؛ دابل‌کلیک روی ویزیت/آزمایش/نسخه پنجرهٔ جزئیات را باز کند.</li>
<li>چارت دندانپزشکی — نشانگرها دندان‌شکل باشند.</li>
</ul>

<div class="warn">⚠️ اگر چیزی از موارد بالا کار نکرد و شمارهٔ نسخه را نمی‌بینید: بیلد ایستگاه شما کهنه است — <code>bin</code> و <code>obj</code> پروژه را حذف و دوباره Rebuild کنید. پروژهٔ API هم حتماً Rebuild و ری‌استارت شود.</div>
<div class="ok">✅ بدون تغییر دیتابیس — این چرخه مهاجرت جدیدی ندارد؛ فقط کد.</div>
</div></body></html>"""

os.makedirs(os.path.dirname(OUT), exist_ok=True)
if os.path.exists(OUT):
    os.remove(OUT)

with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED) as z:
    for rel in deploy_files:
        full = os.path.join(REPO, rel)
        assert os.path.exists(full), f"فایل یافت نشد: {rel}"
        z.write(full, rel)
    z.writestr("Update-Guide-2.12.0.html", guide)
    # مانیفست با SHA-256 هر فایل
    lines = ["# مانیفست بستهٔ آپدیت 2.11.0 → 2.12.0 — چرخهٔ چهل‌وهشتم",
             "# هر خط: sha256  مسیر", ""]
    for rel in deploy_files:
        full = os.path.join(REPO, rel)
        digest = hashlib.sha256(open(full, "rb").read()).hexdigest()
        lines.append(f"{digest}  {rel}")
    z.writestr("MANIFEST.sha256", "\n".join(lines))

# گارد: بازخوانی زیپ و تطبیق شمار/نام‌ها
with zipfile.ZipFile(OUT) as z:
    names = [n for n in z.namelist() if not n.endswith(".html") and not n.endswith(".sha256")]
    missing = set(deploy_files) - set(names)
    assert not missing, f"جاافتاده در زیپ: {missing}"
    print(f"✅ {len(names)} فایل استقرار + راهنما + مانیفست — همه حاضرند")

print(f"📦 بسته ساخته شد: {OUT} ({os.path.getsize(OUT)//1024} KB)")
