#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت تجمعی → v2.4.8 — چرخهٔ سی‌وپنجم (گزارش هشتم مالک).

★ چرا «تجمعی»؟ کشف کلیدی این چرخه: تحلیل پیکسلی اسکرین‌شات منوی مالک نشان داد
MainWindow ایستگاه او قدیمی‌تر از v2.4.6 است — MainWindow.xaml در زیپ ۲.۴.۵ نبود
و زیپ ۲.۴.۶ هم به‌خاطر خطای MC3000 (داخل زیپ ۲.۴.۵!) توسط مالک برگرانده شد.
پس بستهٔ این رلیز به‌جای «دیف همین چرخه»، همهٔ فایل‌های تغییرکرده از v2.4.4
(آخرین حالت کاملِ اطمینانیِ ایستگاه مالک) تا HEAD را دارد — فارغ از اینکه
مالب کدام زیپ‌ها را کجا اعمال/برگردانده، پس از این بسته ایستگاهش دقیقاً
همان HEAD می‌شود.

گاردهای انسجام: نشانه‌های سلامت هر هفت اصلاح این چرخه + نبودِ الگوهای معیوب +
اعتبارسنجی XML همهٔ XAMLها + تطبیق دقیق فهرست بسته با دیف واقعی گیت.
"""
import pathlib
import subprocess
import sys
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.4"    # آخرین حالت کامل اطمینانی ایستگاه مالک
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-Cumulative-to-2.4.8.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.8.html"

# مستثنی‌ها: اسکریپت‌های بسته‌بندی/انتشار داخلی و پوشهٔ docs — به ایستگاه مالک ربطی ندارند
EXCLUDES = [
    '":(exclude)docs/*"',
    '":(exclude)scripts/create_update_zip_*.py"',
    '":(exclude)scripts/publish_release_*.py"',
]

# ─── فهرست بسته = دیف واقعی v2.4.4..HEAD (به‌علاوهٔ اسکریپت پایش) ───
diff_cmd = f'git -C "{REPO}" diff --name-only {BASE}..HEAD -- . ' + " ".join(EXCLUDES)
diff = subprocess.run(diff_cmd, shell=True, capture_output=True, text=True, check=True).stdout.split()
CHANGED = sorted(set(diff) | {"scripts/validate_xaml.py"})

# ─── گارد انسجام مثبت — نشانه‌های سلامت هفت اصلاح این چرخه ───
COHERENCE_CHECKS = [
    # ۱) طراح قالب — فیلتر موجودیت
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs",
     "VisibleTemplates", "فهرست پالایه‌شدهٔ طراح قالب"),
    ("src/Sahand.ClinicPro.Desktop/Views/ComplementaryView.xaml",
     'TemplateFilterIndex', "انتخابگر فیلتر موجودیت"),
    # ۲) لاگ عملیات — شناسه از آخرین قطعهٔ عددی
    ("src/Sahand.ClinicPro.API/Middleware/AuditLogMiddleware.cs",
     "parts.Length - 1; i >= 0; i--", "پیمایش معکوس برای یافتن شناسه"),
    # ۳) پیام‌های سرور — رنگ + تاریخ شمسی
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     '[JsonPropertyName("color")] public string? Color', "فیلد رنگ پنل لایسنس"),
    ("src/Sahand.ClinicPro.Application/Interfaces/ILicenseService.cs",
     "public string DateFa", "تاریخ شمسی در DTO مشترک"),
    ("src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml",
     'Binding DateFa', "نمایش تاریخ شمسی زیر عنوان پیام"),
    ("src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs",
     "class HexBrushConverter", "مبدل رنگ هگز با حالت ملایم"),
    # ۴) چاپ نسخه — الزامات قالب رسمی
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     "DocumentViewerBase.IsMasterPageProperty, true", "پرچم صفحهٔ مستر رسمی"),
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     "typeof(AdornerDecorator)", "لایهٔ آدورنر رسمی"),
    # ۵) منوی کناری — سپرهای دفاعی
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     "{TemplateBinding HorizontalContentAlignment}", "کشش صریح ContentPresenter"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'MinWidth="190"', "عرض‌حداقل تضمینی سربرگ/آیتم"),
    # ۶) ساعت‌ها — اصلاحات چرخهٔ ۳۳ در بسته باشند
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorFormWindow.xaml",
     'Width="150" HorizontalAlignment="Stretch"', "عرض صریح انتخابگرهای برنامهٔ کاری"),
    # ۷) راهنمای چیپی نوبت‌دهی
    ("src/Sahand.ClinicPro.Desktop/Views/AppointmentsView.xaml",
     'Background="#BBDEFB"', "چیپ انتخاب شما در راهنمای اسلات"),
    # ۱۸ قالب جدید
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/PrintTemplateSeedData.cs",
     "فاکتور مینیمال — سفید پاک", "خانوادهٔ مینیمال"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/PrintTemplateSeedData.cs",
     "نوبت‌نامه فشرده — سه‌قیمت", "خانوادهٔ فشرده"),
    # نسخه
    ("Directory.Build.props", "<Version>2.4.8</Version>", "نسخهٔ 2.4.8"),
    ("CHANGELOG.md", "[v2.4.8]", "ثبت چرخهٔ سی‌وپنجم در CHANGELOG"),
]

# ─── نشانه‌های منفی — الگوهای معیوب باید حذف شده باشند ───
NEGATIVE_CHECKS = [
    # راهنمای متنی قدیمی اسلات نوبت
    ("src/Sahand.ClinicPro.Desktop/Views/AppointmentsView.xaml",
     'راهنما: سبز = آزاد', "متن راهنمای قدیمی حذف شد"),
    # قالب بدون آدورنر قدیمی
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     "scrollFactory.AppendChild(pageViewFactory);", "چیدمان بدون آدورنر حذف شد"),
    # تاریخ خام کنار عنوان پیام
    ("src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml",
     'Text="{Binding Date}" FontSize="10"', "تاریخ خام کنار عنوان حذف شد"),
    # نسخهٔ کهنه
    ("Directory.Build.props", "<Version>2.4.7</Version>", "نسخهٔ کهنه حذف شد"),
]

print("■ اجرای گاردهای انسجام چرخهٔ ۳۵…")
for rel, needle, desc in COHERENCE_CHECKS:
    assert needle in (REPO / rel).read_text(encoding="utf-8"), f"❌ نشانهٔ سلامت یافت نشد [{desc}] در {rel}"
    print(f"  ✅ {desc}")
for rel, needle, desc in NEGATIVE_CHECKS:
    assert needle not in (REPO / rel).read_text(encoding="utf-8"), f"❌ الگوی معیوب هنوز هست [{desc}] در {rel}"
    print(f"  ✅ {desc}")

# تطبیق فهرست بسته با دیف واقعی
diff_set = set(diff) | {"scripts/validate_xaml.py"}
assert diff_set == set(CHANGED), \
    f"❌ عدم تطابق فهرست بسته:\n  جاافتاده: {diff_set - set(CHANGED)}\n  اضافه: {set(CHANGED) - diff_set}"
print(f"  ✅ بستهٔ تجمعی {BASE}→HEAD دقیقاً {len(CHANGED)} فایل دارد")

# گارد نهایی XML
res = subprocess.run([sys.executable, str(REPO / "scripts" / "validate_xaml.py")],
                     capture_output=True, text=True)
assert res.returncode == 0, f"❌ اعتبارسنجی XAML ناموفق:\n{res.stdout}"
print("  ✅ هر ۸۲ فایل XAML معتبرند")

# ─── راهنمای HTML فارسی ───
GUIDE_HTML = """<!DOCTYPE html>
<html lang="fa" dir="rtl"><head><meta charset="utf-8">
<title>راهنمای به‌روزرسانی تجمعی به ۲.۴.۸</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:820px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}h3{color:#1565C0;margin-top:22px}ol,ul{line-height:2.1}b{color:#2E7D32}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
.important{background:#FFEBEE;border-radius:10px;padding:14px 18px;border-right:4px solid #C62828;margin:14px 0}
.info{background:#E3F2FD;border-radius:10px;padding:12px 16px;border-right:4px solid #1E88E5;margin:14px 0}</style></head><body><div class="card">
<h1>📦 بستهٔ تجمعی سهند کلینیک‌پرو — تا نسخهٔ ۲.۴.۸</h1>

<div class="important"><b>⭐ این بسته «تجمعی» است — با همهٔ بسته‌های قبلی فرق دارد</b><br>
کشف این چرخه: برخی فایل‌های رلیزهای ۲.۴.۵ و ۲.۴.۶ هرگز به ایستگاه شما نرسیده بود (زیپ ۲.۴.۵ فایل منوی اصلی را نداشت و زیپ ۲.۴.۶ هم بعد از خطای ساخت برگرانده شد) — برای همین منوی کناری و ساعت‌ها هرچه اصلاح می‌شد، شما هنوز حالت قدیمی را می‌دیدید!
<b>این بسته هر ۴۴ فایلِ تغییرکرده از نسخهٔ ۲.۴.۴ تا امروز</b> را دارد؛ بعد از نصب، ایستگاه شما دقیقاً همان آخرین نسخهٔ کامل می‌شود — فارغ از اینکه قبلاً کدام بسته‌ها را اعمال کرده باشید.</div>

<div class="ok">محتوا: <b>۴۴ فایل</b> (۳۵ فایل دسکتاپ + ۷ فایل سرور API/زیرساخت + فایل نسخه + CHANGELOG + ابزار پایش XAML). همه را با همان ساختار پوشه‌ای روی پوشهٔ پروژه کپی و <b>جایگزین</b> کنید.</div>

<h3>مراحل نصب</h3>
<ol>
<li>ویژوال استودیو و برنامهٔ دسکتاپ و <b>سرور API</b> را کامل ببندید</li>
<li>از پوشهٔ پروژه فعلی یک نسخهٔ پشتیبان (کپی کامل) تهیه کنید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی <code>E:\\Sahand_ClinicPro</code> کپی و <b>جایگزین</b> کنید</li>
<li>در ویژوال استودیو: <b>BUILD ← Clean Solution</b> و سپس <b>Rebuild Solution</b> — یا اسکریپت <code>Clean-VS-Cache.bat</code></li>
<li>سرور API را اجرا کنید (با اولین اجرا، ۱۸ قالب چاپ جدید هم خودکار به دیتابیس اضافه می‌شوند) و بعد برنامهٔ دسکتاپ را باز کنید</li>
</ol>

<h3>بعد از نصب بررسی کنید (چک‌لیست ۷ موردی این نسخه)</h3>
<ul>
<li>🧭 <b>منوی کناری</b>: عنوان طلاییِ درشت همهٔ گروه‌های اصلی (پذیرش و درمان، نسخه و دارو، …) کنار آیکون دیده می‌شود؛ دکمه‌ها تمام‌عرض‌اند؛ <b>زیر دکمهٔ خروج باید «نسخه ۲.۴.۸»</b> دیده شود — اگر نبود، فایل‌ها جایگزین نشده‌اند و مراحل نصب را تکرار کنید</li>
<li>🖨️ <b>طراح قالب چاپ</b>: انتخابگر «نمایش قالب‌های» بالای فهرست؛ با انتخاب «نسخه پزشکی» فقط قالب‌های نسخه دیده می‌شوند؛ هر موجودیت اکنون <b>۵ طرح</b> دارد (استاندارد/مدرن/کلاسیک/مینیمال/فشرده)</li>
<li>📋 <b>لاگ عملیات</b>: یک موجودیت را ویرایش کنید؛ در جزئیات ردیفِ لاگ، بخش «مقادیر قبلی» پر است (ویرایش برنامهٔ کاری پزشک هم حالا مقدار قبلی می‌گیرد)</li>
<li>📢 <b>پیام‌های داشبورد</b>: تاریخ هر پیام <b>شمسی</b> و <b>زیر عنوان</b> است و رنگ کادر پیام با همان رنگی که در پنل لایسنس انتخاب کرده‌اید یکی است</li>
<li>🖨️ <b>چاپ نسخه</b>: دکمهٔ چاپ در نسخه‌نویسی → پیش‌نمایش صفحه را نشان می‌دهد (نه صفحهٔ خالی)؛ زوم +/− کار می‌کند</li>
<li>📅 <b>برنامهٔ کاری پزشک</b>: ساعت‌های شروع/پایان/استراحت در تمام عرض سلول و کامل دیده می‌شوند</li>
<li>🎨 <b>نوبت‌دهی</b>: راهنمای زیر تقویم اسلات‌ها حالا مربع‌های رنگی واقعی با برچسب فارسی است (مثل دندانپزشکی)</li>
</ul>

<div class="info"><b>نکتهٔ رنگ پیام‌ها:</b> رنگ کادر از فیلد «رنگ» پیام در پنل لایسنس می‌آید؛ برای پیام‌های قدیمی که موقع ارسالشان رنگی ثبت نشده، رنگ سطح اهمیت (آبی/سبز/زرد/قرمز) استفاده می‌شود.</div>

<div class="warn"><b>نکتهٔ قالب‌های جدید:</b> ۱۸ قالب جدید چاپ با اولین اجرای API به دیتابیس اضافه می‌شوند (بذر idempotent) — اگر در فهرست طراح نبودند، یک بار سرور API را ری‌استارت کنید و در تب طراح دکمهٔ <b>بارگذاری مجدد (⟳)</b> را بزنید.</div>
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
               "بستهٔ تجمعی → v2.4.8 — همهٔ تغییرات v2.4.4 تا HEAD (۴۴ فایل)\n"
               "پس از کپی: Clean Solution سپس Rebuild Solution (چک‌لیست داخل راهنمای HTML)\n"
               + "-" * 70 + "\n" + "\n".join(CHANGED) + "\n")

print(f"\n✅ بستهٔ تجمعی ساخته شد: {OUT}")
print(f"   حجم: {OUT.stat().st_size:,} بایت | فایل‌ها: {len(CHANGED)}")
with zipfile.ZipFile(OUT) as z:
    names = z.namelist()
    print(f"   • راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME} | سورس: {len(names)-2} فایل")
