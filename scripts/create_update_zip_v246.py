#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.4.5 → v2.4.6 — چرخهٔ سی‌وسوم (گزارش هفتم مالک).
فقط فایل‌های تغییرکردهٔ همین رلیز (تگ v2.4.5 تا HEAD) با همان ساختار
پوشه‌ای مخزن + مانیفست متنی + راهنمای فارسی HTML.

گاردهای انسجام: نشانه‌های سلامت تغییرات کلیدی این نسخه باید در سورس موجود
باشند و نشانه‌های الگوی معیوب حذف شده باشند وگرنه اسکریپت متوقف می‌شود.
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.5"    # رلیز چرخهٔ سی‌ودوم
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.5-to-2.4.6.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.6.html"

# ─── گارد انسجام مثبت — نشانه‌های سلامت تغییرات کلیدی این نسخه ───
COHERENCE_CHECKS = [
    # ۱) اسپلش — بزرگ‌تر + شفاف (بدون ظرف تیره)
    ("src/Sahand.ClinicPro.Desktop/Views/SplashWindow.xaml",
     'Width="640" Height="560"',
     "پنجرهٔ اسپلش بزرگ‌تر ۶۴۰×۵۶۰"),
    ("src/Sahand.ClinicPro.Desktop/Views/SplashWindow.xaml",
     'Width="280" Height="280"',
     "لوگوی بزرگ ۲۸۰ پیکسلی"),
    # ۲) راهنمای رنگ دندانپزشکی — چیپ رنگی واقعی
    ("src/Sahand.ClinicPro.Desktop/Views/ParaclinicalView.xaml",
     'LegendChip',
     "چیپ‌های رنگی راهنمای چارت دندان"),
    ("src/Sahand.ClinicPro.Desktop/Views/ParaclinicalView.xaml",
     'Background="#8E24AA"',
     "چیپ ایمپلنت در راهنما"),
    # ۳) انتخابگر ساعت — بستن تضمینی + گارد بازشدن‌مجدد
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml.cs",
     "ClosePopupForcibly();",
     "بستن تضمینی پاپ‌آپ از هر سه دکمه"),
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml.cs",
     "_popupWasOpenOnMouseDown = PickerPopup.IsOpen;",
     "گارد بازشدن‌مجدد تاگل"),
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml.cs",
     "Dispatcher.BeginInvoke(() => _popupWasOpenOnMouseDown = false,",
     "ریست گارد پس از چرخهٔ ورودی"),
    # ۴) برنامهٔ کاری — کشش و مرکزچینی
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml",
     'HorizontalAlignment="Center"',
     "محتوای مرکزچین انتخابگر ساعت"),
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorFormWindow.xaml",
     'Width="150" HorizontalAlignment="Stretch"',
     "عرض صریح ۱۵۰px انتخابگرهای برنامهٔ کاری"),
    ("src/Sahand.ClinicPro.Desktop/App.xaml",
     '<Setter Property="HorizontalContentAlignment" Value="Stretch" />',
     "کشش افقی محتوای سلول همهٔ جدول‌ها"),
    # ۵) منوی کناری — گرید تضمینی عنوان گروه
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     '<!-- عنوان — تمام عرض باقی‌مانده؛ تضمینی -->',
     "ستون * عنوان سربرگ گروه"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'Margin="8,2,20,4"',
     "تورفتگی راست ۲۰px زیرمنوها"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     '{x:Static helpers:AppInfo.VersionText}',
     "نشان نسخه در پانوشت منو"),
    # ۶) چاپ نسخه — اسکرول + آیکون‌های زوم
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     "scrollFactory.AppendChild(pageViewFactory);",
     "بازگرداندن ScrollViewer قالب نمایشگر"),
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     "PackIconKind.Plus",
     "آیکون وکتوری بزرگ‌نمایی"),
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     "PackIconKind.Minus",
     "آیکون وکتوری کوچک‌نمایی"),
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     "FlowDirection = FlowDirection.LeftToRight,",
     "خوشهٔ زوم LTR"),
    # ۷) انتخابگر تاریخ — حذف Fade
    ("src/Sahand.ClinicPro.Desktop/Controls/PersianDatePicker.xaml",
     "AllowsTransparency=\"True\">",
     "پاپ‌آپ تقویم بدون انیمیشن Fade"),
    # نسخه
    ("Directory.Build.props",
     "<Version>2.4.6</Version>", "نسخهٔ 2.4.6"),
    ("CHANGELOG.md",
     "[v2.4.6]", "ثبت چرخهٔ سی‌وسوم در CHANGELOG"),
]

# نشانه‌های منفی — الگوهای معیوب باید حذف شده باشند
NEGATIVE_CHECKS = [
    # اسپلش قدیمی کوچک با ظرف تیره
    ("src/Sahand.ClinicPro.Desktop/Views/SplashWindow.xaml",
     'Width="480" Height="420"',
     "پنجرهٔ کوچک قدیمی حذف شد"),
    ("src/Sahand.ClinicPro.Desktop/Views/SplashWindow.xaml",
     'Background="#F2091A33"',
     "ظرف تیرهٔ خاکستری‌کننده حذف شد"),
    # ایموجی رندرنشدهٔ راهنمای دندان
    ("src/Sahand.ClinicPro.Desktop/Views/ParaclinicalView.xaml",
     'Text="راهنما: ⬜',
     "متن ایموجی‌دار راهنمای دندان حذف شد"),
    # بستن قدیمی بدون مسیر دوم
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml.cs",
     '_valueAtOpen = TimeText;\n        PickerPopup.IsOpen = false;',
     "الگوی بستن تک‌مسیرهٔ تأیید حذف شد"),
    # Fade از هر دو انتخابگر
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml",
     'PopupAnimation="Fade"',
     "انیمیشن Fade انتخابگر ساعت حذف شد"),
    ("src/Sahand.ClinicPro.Desktop/Controls/PersianDatePicker.xaml",
     'PopupAnimation="Fade"',
     "انیمیشن Fade تقویم حذف شد"),
    # MinWidth قدیمی متن ساعت
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml",
     'MinWidth="56"',
     "MinWidth غیرواقعی متن ساعت حذف شد"),
    # دکمه‌های زوم متنی قدیمی
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     'Content = "+", Width = 34',
     "دکمهٔ متنی + قدیمی حذف شد"),
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     'Content = "-", Width = 34',
     "دکمهٔ متنی − قدیمی حذف شد"),
    # قالب برهنهٔ بدون اسکرول قدیمی
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     'bareTemplate.VisualTree = pageViewFactory;',
     "قالب برهنهٔ بدون پیمایش حذف شد"),
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
<title>راهنمای به‌روزرسانی ۲.۴.۵ به ۲.۴.۶</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:780px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}ol{line-height:2.1}b{color:#2E7D32}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
.info{background:#E3F2FD;border-radius:10px;padding:12px 16px;border-right:4px solid #1E88E5;margin:14px 0}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
ul{line-height:2}</style></head><body><div class="card">
<h1>📦 بستهٔ به‌روزرسانی سهند کلینیک‌پرو — ۲.۴.۵ به ۲.۴.۶</h1>
<div class="ok">این بسته فقط فایل‌های تغییرکرده را دارد؛ همه را با همان ساختار پوشه‌ای روی نسخهٔ ۲.۴.۵ کپی و جایگزین کنید و سپس Solution را <b>Clean و Rebuild</b> کنید (توضیح پایین را حتما بخوانید).</div>
<h3>مراحل نصب</h3>
<ol>
<li>برنامهٔ دسکتاپ و <b>سرور API</b> را کامل ببندید (مطمئن شوید پردازهٔ Sahand.ClinicPro.API در حال اجرا نیست)</li>
<li>از پوشهٔ پروژهٔ فعلی، یک نسخهٔ پشتیبان (کپی کامل) تهیه کنید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی پوشهٔ پروژه کپی و <b>جایگزین</b> کنید</li>
<li>در ویژوال استودیو: <b>BUILD ← Clean Solution</b> و سپس <b>Rebuild Solution</b> — یا اسکریپت <code>Clean-VS-Cache.bat</code> ریشهٔ پروژه را اجرا کنید</li>
<li>سرور API را اجرا و برنامهٔ دسکتاپ را باز کنید</li>
</ol>
<div class="warn"><b>⭐ مهم — چرا Clean و Rebuild؟</b><br>
چند مورد این نسخه (عنوان گروه‌های منو) در رلیزهای قبل هم «رفع» شده بود ولی شما همچنان حالت قدیمی را می‌دیدید؛ محتمل‌ترین علت، ماندن بایلد کهنه (BAML کش‌شده) روی ایستگاه شماست.
برای اطمینان، از این نسخه <b>شمارهٔ نسخهٔ در حال اجرا زیر دکمهٔ «خروج از حساب» در منوی کناری</b> همیشه دیده می‌شود:
اگر پس از این آپدیت عدد <b>۲.۴.۶</b> را ندیدید، یعنی فایل‌ها جایگزین نشده‌اند یا بیلد تازه اجرا نشده — مراحل بالا را دوباره و کامل انجام دهید.</div>
<h3>بعد از اجرا بررسی کنید</h3>
<ul>
<li>🎨 <b>اسپلش</b>: پنجرهٔ بارگذاری بزرگ‌تر شده و دیگر تختهٔ تیره/خاکستری دورش نیست — لوگو و متن‌ها مستقیم روی دسکتاپ با سایهٔ نرم</li>
<li>🦷 <b>دندانپزشکی</b>: راهنمای رنگ‌های زیر چارت فک بالا/پایین اکنون مربع‌های رنگی واقعی با برچسب فارسی است (قبلا علامت‌های نامفهوم بود)؛ راهنمای نمرهٔ درد فیزیوتراپی هم همین‌طور</li>
<li>⏰ <b>انتخابگر ساعت</b>: با انتخاب زمان و زدن «تأیید» کادر بی‌درنگ بسته می‌شود (و کلیک روی خود کادر باز، دیگر آن را باز نگه نمی‌دارد)</li>
<li>📅 <b>برنامهٔ کاری پزشک</b>: ساعت‌های شروع/پایان/استراحت در تمام عرض سلول و کامل دیده می‌شوند</li>
<li>🧭 <b>منوی کناری</b>: عنوان همهٔ گروه‌های اصلی (پذیرش و درمان، نسخه و دارو، …) درشت طلایی کنار آیکون دیده می‌شود؛ سربرگ‌ها تمام‌عرض‌اند و زیرمنوها از راست تورفتگی دارند؛ زیر دکمهٔ خروج، نسخهٔ ۲.۴.۶ نمایان است</li>
<li>🖨️ <b>چاپ نسخه</b>: دکمه‌های زوم آیکون + و − واضح دارند؛ بزرگ‌نمایی کار می‌کند و صفحهٔ زوم‌شده با نوار پیمایش قابل جابه‌جایی است</li>
</ul>
<h3>خلاصهٔ تغییرات این نسخه</h3>
<ul>
<li>🎨 <b>اسپلش</b>: بزرگ‌تر (۶۴۰×۵۶۰، لوگو ۲۸۰px) + زمینهٔ کاملاً شفاف — عامل ظاهر خاکستری (ظرف تیرهٔ قبلی) حذف شد</li>
<li>🦷 <b>راهنمای رنگ چارت دندان</b>: ایموجی‌های رندرنشده در فونت فارسی → چیپ‌های رنگی واقعی با همان رنگ دکمه‌ها</li>
<li>⏰ <b>انتخابگر ساعت</b>: بستن تضمینی با تأیید/انصراف/پاک‌کردن (دو مسیر مستقل + گارد بازشدن‌مجدد + حذف انیمیشن معیوب Fade)</li>
<li>📅 <b>برنامهٔ کاری</b>: کنترل ساعت تمام‌عرضِ سلول با متن مرکزچین + کشش افقی سراسری محتوای همهٔ جدول‌های برنامه</li>
<li>🧭 <b>منوی کناری</b>: بازطراحی ساختاری سربرگ گروه‌ها — فضای عنوان تضمینی + تمام‌عرض + تورفتگی راست زیرمنوها + نشان نسخهٔ درون منو</li>
<li>🖨️ <b>چاپ نسخه</b>: نوار پیمایش برای صفحهٔ زوم‌شده بازگشت + دکمه‌های زوم با آیکون همیشه-نمایان و ترتیب استاندارد [−] [٪] [+]</li>
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
               f"بستهٔ به‌روزرسانی 2.4.5 → 2.4.6\nتعداد فایل: {len(manifest_lines)}\n\n" + "\n".join(manifest_lines))

print(f"✅ ساخته شد: {OUT} ({OUT.stat().st_size / 1024:.0f} کیلوبایت)")
print(f"— راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME}")
