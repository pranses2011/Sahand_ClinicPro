#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت → v2.4.10 — چرخهٔ سی‌وهفتم (گزارش نهم مالک).

بستهٔ «تجمعی از v2.4.8» — طبق درس چرخهٔ سی‌وپنجم (فایل جاافتاده = باگ «رفع‌نشده»
در ایستگاه مالک): این بسته همهٔ فایل‌های تغییرکرده از تگ v2.4.8 تا HEAD را
دارد؛ یعنی هم وصلهٔ ۵فایلیِ v2.4.9 (ترمیم بیلد) را هم هر ۷ رفع چرخهٔ سی‌وهفتم
را پوشش می‌دهد. ایستگاهی که روی ۲.۴.۸ است یا روی ۲.۴.۹ — هر دو با همین یک
بسته به ۲.۴.۱۰ می‌رسند (کپی فایل تکراری بی‌ضرر است؛ محتوا همان یا جدیدتر است).

گاردهای انسجام این چرخه (همه باید در اولین اجرا پاس شوند):
  • نشانگرهای سلامت هر ۸ رفع چرخهٔ ۳۷ + وصلهٔ v2.4.9 (چون بسته تجمعی است)
  • گارد قطعه‌ای ستون‌های تعرفه: بین «قیمت تعرفه» تا «وضعیت» فقط Center
  • گارد ترتیب استاتیک ComplementaryViewModel (میراث v2.4.9)
  • ممیزی خودکار using های رسانه‌ای + اعتبارسنجی XML همهٔ XAMLها
  • تطبیق دقیق فهرست بسته با دیف واقعی گیت v2.4.8..HEAD
"""
import pathlib
import subprocess
import sys
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.8"    # تجمعی — پوشش هر دو ایستگاه ۲.۴.۸ و ۲.۴.۹
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.8-to-2.4.10.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.10.html"

# مستثنی‌ها: اسکریپت‌های بسته‌بندی/انتشار/ممیزی داخلی و پوشهٔ docs — به ایستگاه مالک ربطی ندارند
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

# ─── گارد انسجام مثبت — نشانگرهای سلامت رفع‌های این بسته ───
COHERENCE_CHECKS = [
    # ═══ وصلهٔ v2.4.9 (بسته تجمعی است — نشانگرهایش هم باید سالم بمانند) ═══
    ("src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs",
     "using System.Windows.Media;", "using رسانه‌ای HexBrush (v2.4.9)"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs",
     "⚠️ این ترتیب را جابه‌جا نکنید", "هشدار ترتیب استاتیک (v2.4.9)"),
    ("src/Sahand.ClinicPro.Application/Interfaces/ILicenseService.cs",
     "// چرخهٔ سی‌وپنجم: رنگ انتخابی فروشنده", "کامنت عادی پارامتر Color (v2.4.9)"),
    # ═══ ۱) ریشهٔ مشترک منو + ساعت — سپر استایل سوییچ MD3 ═══
    ("src/Sahand.ClinicPro.Desktop/App.xaml",
     'x:Key="App.PlainToggleButton"', "استایل سپر خالی در App.xaml"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     "App.PlainToggleButton", "سپر روی سربرگ گروه‌های سایدبار"),
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml",
     'Style="{StaticResource App.PlainToggleButton}"', "سپر روی تاگل انتخابگر ساعت"),
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml",
     'MinHeight="34"', "حداقل ارتفاع لمسی قالب ساعت"),
    # ═══ ۲) کمبوی «نمایش قالب‌های» — پراپرتی واقعی به‌جای فیلد تاپل ═══
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs",
     "record TemplateTypeOption", "رکورد گزینهٔ نوع قالب"),
    ("src/Sahand.ClinicPro.Desktop/Views/ComplementaryView.xaml",
     'DisplayMemberPath="Fa"', "اتصال کمبو به پراپرتی Fa"),
    # ═══ ۳) پیام‌های سرور — تاریخ شمسی + بج + رنگ روشن ═══
    ("src/Sahand.ClinicPro.Application/Interfaces/ILicenseService.cs",
     "DateFa", "تاریخ شمسی در DTO مشترک پیام"),
    ("src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml",
     "DateFa", "بج تاریخ شمسی در کارت پیام"),
    # ═══ ۴) نسخه‌نویسی — JSON صریح + قالب رسمی نمایشگر چاپ ═══
    ("src/Sahand.ClinicPro.Desktop/ViewModels/PrescriptionFormViewModel.cs",
     "JsonPropertyName", "نام‌گذاری JSON صریح اقلام تمپلیت"),
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     "AdornerDecorator", "ساختار رسمی نمایشگر چاپ"),
    ("src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
     "IsMasterPage", "پرچم صفحهٔ اصلی DocumentPageView"),
    # ═══ ۵) فوق‌تخصص + تب برنامهٔ کاری ═══
    ("src/Sahand.ClinicPro.Desktop/ViewModels/DoctorsViewModel.cs",
     "مقدار فعلی را نگه دار", "نگه‌داشت مقدار فوق‌تخصص پیش از Clear"),
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorFormWindow.xaml.cs",
     "InitialTabIndex", "بازکردن فرم روی تب درخواستی"),
    # ═══ ۶) تعرفه — fallback از تنظیمات بیمه + وسط‌چین ═══
    ("src/Sahand.ClinicPro.Application/Features/Lookups/ServiceTariffCommands.cs",
     "TariffFallbackShares", "کمک‌کنندهٔ سهم fallback (لیست مدیریت)"),
    ("src/Sahand.ClinicPro.Application/Features/Lookups/LookupQueries.cs",
     "TariffFallbackShares", "کمک‌کنندهٔ سهم fallback (پذیرش/صندوق)"),
    ("src/Sahand.ClinicPro.Application/Features/Lookups/ServiceTariffCommands.cs",
     "DefaultCoveragePercent", "الگوی اولویت پوشش بیمه"),
    # ═══ ۷) بندانگشتی پزشکان/پرسنل ═══
    ("src/Sahand.ClinicPro.Desktop/Services/PhotoTools.cs",
     "DecodePixelWidth", "بندانگشتی‌ساز کم‌حافظه"),
    ("src/Sahand.ClinicPro.Desktop/Services/PhotoTools.cs",
     "DoctorThumbnailKey", "کلید کش بندانگشتی پزشک"),
    ("src/Sahand.ClinicPro.Desktop/Services/PhotoTools.cs",
     "InvalidateThumbnail", "ابطال کش پس از تعویض/حذف"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/DoctorsViewModel.cs",
     "LoadDoctorThumbnailsAsync", "بارگذاری بندانگشتی‌های پزشکان"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/AdminViewModel.cs",
     "LoadEmployeeThumbnailsAsync", "بارگذاری بندانگشتی‌های پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/EmployeeFormViewModel.cs",
     "InvalidateThumbnail", "ابطال کش از فرم پرسنل"),
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorsView.xaml",
     'ImageBrush ImageSource="{Binding Photo}"', "بندانگشتی دایره‌ای لیست پزشکان"),
    ("src/Sahand.ClinicPro.Desktop/Views/PersonnelView.xaml",
     'ImageBrush ImageSource="{Binding Photo}"', "بندانگشتی دایره‌ای لیست پرسنل"),
    # ═══ ۸) نسخه و CHANGELOG ═══
    ("Directory.Build.props", "<Version>2.4.10</Version>", "نسخهٔ 2.4.10"),
    ("CHANGELOG.md", "[v2.4.10]", "ثبت چرخهٔ سی‌وهفتم در CHANGELOG"),
]

# ─── نشانگرهای منفی — الگوهای معیوب باید حذف شده باشند ───
NEGATIVE_CHECKS = [
    # نسخهٔ کهنه
    ("Directory.Build.props", "<Version>2.4.9</Version>", "نسخهٔ کهنه حذف شد"),
    # کمبوی قالب — فیلد تاپل به‌جای پراپرتی (۹ گزینهٔ سفید)
    ("src/Sahand.ClinicPro.Desktop/Views/ComplementaryView.xaml",
     'DisplayMemberPath="Item2"', "اتصال فیلد تاپل حذف شد"),
    # آیکون‌های «نمایش بزرگ» قدیمی در دو لیست — جایشان بندانگشتی واقعی نشسته
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorsView.xaml",
     'PackIcon Kind="Image" Width="18"', "آیکون قدیمی عکس پزشکان حذف شد"),
    ("src/Sahand.ClinicPro.Desktop/Views/PersonnelView.xaml",
     'PackIcon Kind="Image" Width="18"', "آیکون قدیمی عکس پرسنل حذف شد"),
    # کامنت XML نامعتبر روی پارامتر رکورد (CS1587 — میراث v2.4.9)
    ("src/Sahand.ClinicPro.Application/Interfaces/ILicenseService.cs",
     "/// <summary>چرخهٔ سی‌وپنجم: رنگ انتخابی", "کامنت XML نامعتبر حذف شد"),
]

print("■ اجرای گاردهای انسجام چرخهٔ ۳۷…")
for rel, needle, desc in COHERENCE_CHECKS:
    assert needle in (REPO / rel).read_text(encoding="utf-8-sig"), f"❌ نشانگر سلامت یافت نشد [{desc}] در {rel}"
    print(f"  ✅ {desc}")
for rel, needle, desc in NEGATIVE_CHECKS:
    assert needle not in (REPO / rel).read_text(encoding="utf-8-sig"), f"❌ الگوی معیوب هنوز هست [{desc}] در {rel}"
    print(f"  ✅ {desc}")

# ─── گارد قطعه‌ای ستون‌های تعرفه — فقط Center بین «قیمت تعرفه» تا «وضعیت» ───
ins_txt = (REPO / "src/Sahand.ClinicPro.Desktop/Views/InsuranceView.xaml").read_text(encoding="utf-8-sig")
seg_start = ins_txt.find('Header="قیمت تعرفه"')
seg_end = ins_txt.find('Header="وضعیت"', seg_start)
assert 0 < seg_start < seg_end, "❌ قطعهٔ ستون‌های تعرفه در InsuranceView پیدا نشد"
seg = ins_txt[seg_start:seg_end]
assert seg.count('Value="Center"') == 3, f"❌ ستون‌های تعرفه باید ۳ ستون Center داشته باشند (الان {seg.count(chr(34).join(['Value=', 'Center', '']))})"
assert 'Value="Right"' not in seg, "❌ هنوز ستون عددی راست‌چین در قطعهٔ تعرفه هست"
print("  ✅ هر سه ستون عددی تعرفه وسط‌چین‌اند")

# ─── گارد ترتیب استاتیک (میراث v2.4.9 — قلب رفع CS8604؛ چرخهٔ ۳۷ نوع را
#     از تاپل به رکورد TemplateTypeOption ارتقا داد، ترتیب باید حفظ شود) ───
vm_txt = (REPO / "src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs").read_text(encoding="utf-8-sig")
i_types = vm_txt.find("public static TemplateTypeOption[] TemplateTypes")
i_filter = vm_txt.find("TemplateFilterOptions { get; }")
assert 0 < i_types < i_filter, \
    f"❌ ترتیب استاتیک شکسته است! TemplateTypes (موقعیت {i_types}) باید قبل از TemplateFilterOptions (موقعیت {i_filter}) باشد"
assert vm_txt.count("TemplateTypeOption[] TemplateTypes { get; }") == 1, "❌ تعریف TemplateTypes باید دقیقاً یک‌باشد"
print("  ✅ گارد ترتیب استاتیک: TemplateTypes قبل از TemplateFilterOptions + تک‌تعریفی")

# ─── ممیزی خودکار using های رسانه‌ای کل سورس ───
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
    "src/Sahand.ClinicPro.Application/Features/Lookups/LookupQueries.cs",
    "src/Sahand.ClinicPro.Application/Features/Lookups/ServiceTariffCommands.cs",
    "src/Sahand.ClinicPro.Application/Interfaces/ILicenseService.cs",
    "src/Sahand.ClinicPro.Desktop/App.xaml",
    "src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml",
    "src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs",
    "src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/Services/PhotoTools.cs",
    "src/Sahand.ClinicPro.Desktop/Services/PrescriptionPrintHelper.cs",
    "src/Sahand.ClinicPro.Desktop/ViewModels/AdminViewModel.cs",
    "src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs",
    "src/Sahand.ClinicPro.Desktop/ViewModels/DoctorsViewModel.cs",
    "src/Sahand.ClinicPro.Desktop/ViewModels/EmployeeFormViewModel.cs",
    "src/Sahand.ClinicPro.Desktop/ViewModels/PrescriptionFormViewModel.cs",
    "src/Sahand.ClinicPro.Desktop/Views/ComplementaryView.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/DoctorFormWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/DoctorFormWindow.xaml.cs",
    "src/Sahand.ClinicPro.Desktop/Views/DoctorsView.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/InsuranceView.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/PersonnelView.xaml",
    "src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
}
assert set(CHANGED) == EXPECTED, \
    f"❌ فهرست بسته با انتظار این چرخه فرق دارد:\n  جاافتاده: {EXPECTED - set(CHANGED)}\n  اضافه: {set(CHANGED) - EXPECTED}"
print(f"  ✅ بستهٔ {BASE}→HEAD دقیقاً {len(CHANGED)} فایل دارد (تجمعی: وصلهٔ 2.4.9 + چرخهٔ 3.7)")

# ─── راهنمای HTML فارسی ───
GUIDE_HTML = """<!DOCTYPE html>
<html lang="fa" dir="rtl"><head><meta charset="utf-8">
<title>راهنمای به‌روزرسانی به ۲.۴.۱۰</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:860px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}h3{color:#1565C0;margin-top:22px}ol,ul{line-height:2.1}b{color:#2E7D32}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
.important{background:#FFEBEE;border-radius:10px;padding:14px 18px;border-right:4px solid #C62828;margin:14px 0}
.info{background:#E3F2FD;border-radius:10px;padding:12px 16px;border-right:4px solid #1E88E5;margin:14px 0}</style></head><body><div class="card">
<h1>📦 به‌روزرسانی سهند کلینیک‌پرو — نسخهٔ ۲.۴.۱۰ (چرخهٔ سی‌وهفتم)</h1>

<div class="ok">محتوا: <b>۲۴ فایل</b> — این بسته <b>«تجمعی از ۲.۴.۸»</b> است؛ یعنی هم وصلهٔ ترمیم بیلد ۲.۴.۹ را هم هر ۸ اصلاح این چرخه را دارد. اگر الان روی ۲.۴.۸ هستید <b>یا</b> روی ۲.۴.۹ — در هر دو حالت فقط همین یک بسته را نصب کنید (کپی فایل تکراری بی‌ضرر است). همه را با همان ساختار پوشه‌ای روی <code>E:\\Sahand_ClinicPro</code> کپی و <b>جایگزین</b> کنید.</div>

<div class="warn"><b>⭐ دو پروژه باید بازبیلد شوند:</b> این چرخه هم در <b>دسکتاپ</b> تغییر دارد هم در <b>سرور API</b> (کوئری‌های تعرفه در Application + سرویس لایسنس). پس از کپی: <code>BUILD ← Clean Solution</code> سپس <code>Rebuild Solution</code> و سرور API را هم با فایل‌های جدید publish و ری‌استارت کنید.</div>

<h3>۸ اصلاح این نسخه</h3>
<ul>
<li>🧭 <b>منوی کناری + ساعت‌های برنامهٔ کاری (ریشهٔ مشترک قطعی)</b> — استایل ضمنیِ سوییچ MD3 همهٔ دکمه‌های تاگلِ بدون استایل را در قاب 52×32 زندانی می‌کرد؛ عنوان گروه‌های منو زیر خط می‌ماند و ساعت «10:30» به‌صورت «10:3» با آیکون نصف‌بریده دیده می‌شد. اکنون استایل سپر خالی + ابعاد خودکار.</li>
<li>🖨️ <b>کمبوی «نمایش قالب‌های» در طراح قالب چاپ</b> — ۹ گزینهٔ سفیدِ بی‌متن بود؛ ریشه: اتصال به «فیلد» تاپل که WPF نمی‌خواند → رکورد با پراپرتی واقعی.</li>
<li>📢 <b>پیام‌های سرور لایسنس</b> — تاریخ شمسی به‌صورت بج در گوشهٔ بالا-چپ کادر + پس‌زمینهٔ روشن‌تر (رنگ حاشیه = رنگ انتخابی پنل).</li>
<li>💊 <b>نسخه‌نویسی</b> — بارگذاری تمپلیت اقلام را با نام/مصرف می‌آورد (نام‌گذاری JSON صریح) + پیش‌نمایش چاپ دیگر خالی نیست (قالب رسمی نمایشگر WPF بازسازی شد).</li>
<li>🩺 <b>فرم پزشک</b> — فوق‌تخصص هنگام ذخیره تهی نمی‌شود + دکمهٔ «برنامهٔ کاری» لیست، فرم را مستقیم روی همان تب باز می‌کند.</li>
<li>🧮 <b>تعرفه‌ها</b> — ریشهٔ «همهٔ بیمه‌ها ۱۰۰٪ سهم بیمار شدن»: در نبود تعرفهٔ اختصاصی، سهم‌ها از ردیف آزاد (۰/۱۰۰) خوانده می‌شد؛ اکنون از <b>درصد پوشش خودِ بیمه</b> (فیلد «درصد پوشش» در تعریف بیمه — پیش‌فرض ۷۰). برای هر بیمه‌ای که تعرفهٔ اختصاصی دارید همان معتبر می‌ماند. + سه ستون عددی لیست تعرفه وسط‌چین شد.</li>
<li>🖼️ <b>بندانگشتی عکس</b> — لیست پزشکان و لیست پرسنل حالا تصویر دایره‌ای واقعی نشان می‌دهند؛ کلیک روی عکس = بزرگ‌نمایی تمام‌صفحه (عکس را از همان دکمهٔ کناری می‌توانید عوض کنید).</li>
<li>🐞 <b>ترمیم‌های ۲.۴.۹</b> — اگر هنوز وصلهٔ ۲.۴.۹ را نصب نکرده‌اید، نگران نباشید؛ فایل‌هایش داخل همین بسته است.</li>
</ul>

<h3>مراحل نصب</h3>
<ol>
<li>ویژوال استودیو، برنامهٔ دسکتاپ و سرور API را کامل ببندید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی <code>E:\\Sahand_ClinicPro</code> کپی و <b>جایگزین</b> کنید (۲۴ فایل)</li>
<li>در ویژوال استودیو: <b>BUILD ← Clean Solution</b> و سپس <b>Rebuild Solution</b></li>
<li>سرور API را با فایل‌های جدید publish کرده و سرویسش را ری‌استارت کنید (دو کوئری تعرفه + سرویس لایسنس تغییر کرده‌اند)</li>
</ol>

<h3>بعد از نصب بررسی کنید (چک‌لیست ۸ موردی)</h3>
<ul>
<li>✅ زیر دکمهٔ «خروج از حساب» منوی کناری <b>«نسخه ۲.۴.۱۰»</b> دیده شود — اگر نبود فایل‌ها جایگزین نشده‌اند</li>
<li>✅ در منوی کناری، عنوان گروه‌های دارای زیرمنو (پروندهٔ الکترونیک، پشتیبانی و…) خوانا و کامل است</li>
<li>✅ در «تعریف پزشک ← برنامهٔ کاری» ساعت انتخابی (مثلاً 10:30) کامل و با آیکون سالم دیده می‌شود</li>
<li>✅ «قالب چاپ و نظرسنجی ← طراح قالب چاپ» کمبوی «نمایش قالب‌های:» نام فارسی گزینه‌ها را نشان می‌دهد</li>
<li>✅ در داشبورد، پیام‌های سرور لایسنس تاریخ <b>شمسی</b> در بج گوشهٔ بالا-چپ دارند و کادرشان روشن است</li>
<li>✅ در نسخه‌نویسی: بارگذاری تمپلیت اقلام را با نام و مشخصات می‌آورد + دکمهٔ چاپ پیش‌نمایش پر نشان می‌دهد</li>
<li>✅ صفحهٔ بیمه ← تب تعرفه‌ها: انتخاب هر بیمه، سهم‌ها را از درصد پوشش همان بیمه نشان می‌دهد (پیش‌فرض ۷۰/۳۰) و آزاد ۰/۱۰۰؛ ستون‌های عددی وسط‌چین</li>
<li>✅ لیست پزشکان و لیست پرسنل: عکس بندانگشتی دایره‌ای دیده می‌شود و کلیک روی آن بزرگش می‌کند</li>
</ul>

<div class="info"><b>نکتهٔ تعرفه‌ها:</b> اگر می‌خواهید درصدی غیر از پیش‌فرض برای یک بیمه باشد، دو راه دارید: ① در تعریف همان بیمه فیلد «درصد پوشش» را تغییر دهید (روی همهٔ خدمت‌های بدون تعرفهٔ اختصاصی اثر می‌کند) یا ② در تب تعرفه‌ها آن بیمه را انتخاب و برای هر خدمت «تعرفهٔ اختصاصی» ثبت کنید (فقط همان خدمت). ردیف‌های اختصاصی همیشه مقدم‌اند.</div>
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
               "بستهٔ تجمعی v2.4.10 — همهٔ تغییرات v2.4.8 تا v2.4.10 (شامل وصلهٔ 2.4.9)\n"
               "پس از کپی: Clean Solution سپس Rebuild Solution + بازبیلد/ری‌استارت سرور API\n"
               "چک‌لیست ۸ موردی داخل راهنمای HTML\n"
               + "-" * 70 + "\n" + "\n".join(CHANGED) + "\n")

print(f"\n✅ بستهٔ آپدیت ساخته شد: {OUT}")
print(f"   حجم: {OUT.stat().st_size:,} بایت | فایل‌ها: {len(CHANGED)}")
with zipfile.ZipFile(OUT) as z:
    names = z.namelist()
    print(f"   • راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME} | سورس: {len(names)-2} فایل")
