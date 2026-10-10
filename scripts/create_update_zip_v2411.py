#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت → v2.4.11 — چرخهٔ سی‌وهشتم (گزارش دهم مالک).

بستهٔ «تجمعی از v2.4.8» — طبق درس چرخهٔ سی‌وپنجم: این بسته همهٔ فایل‌های
تغییرکرده از تگ v2.4.8 تا HEAD را دارد؛ یعنی وصلهٔ v2.4.9 + چرخهٔ v2.4.10 +
هر ۱۱ اصلاح این چرخه را پوشش می‌دهد. ایستگاه روی هر یک از ۲.۴.۸/۲.۴.۹/
۲.۴.۱۰ با همین یک بسته به ۲.۴.۱۱ می‌رسد (کپی فایل تکراری بی‌ضرر است).

گاردهای انسجام این چرخه (همه باید در اولین اجرا پاس شوند):
  • نشانگرهای سلامت هر ۱۱ رفع چرخهٔ ۳۸ + میراث وصلهٔ 2.4.9 و چرخهٔ 2.4.10
  • ممیزی خودکار using های رسانه‌ای + اعتبارسنجی XML همهٔ XAMLها
  • تطبیق دقیق فهرست بسته با دیف واقعی گیت v2.4.8..HEAD
"""
import pathlib
import subprocess
import sys
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.8"    # تجمعی — پوشش ایستگاه‌های ۲.۴.۸ / ۲.۴.۹ / ۲.۴.۱۰
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.8-to-2.4.11.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.11.html"

# مستثنی‌ها: اسکریپت‌های بسته‌بندی/انتشار/ممیزی داخلی و پوشهٔ docs — به ایستگاه مالک ربطی ندارند
EXCLUDES = [
    '":(exclude)docs/*"',
    '":(exclude)scripts/create_update_zip_*.py"',
    '":(exclude)scripts/publish_release_*.py"',
    '":(exclude)scripts/audit_csharp_usings.py"',
    '":(exclude)scripts/prepend_changelog_38.py"',
]

# ─── فهرست بسته = دیف واقعی v2.4.8..HEAD ───
diff_cmd = f'git -C "{REPO}" diff --name-only {BASE}..HEAD -- . ' + " ".join(EXCLUDES)
diff = subprocess.run(diff_cmd, shell=True, capture_output=True, text=True, check=True).stdout.split()
CHANGED = sorted(set(diff))

# ─── گارد انسجام مثبت — نشانگرهای سلامت رفع‌های این بسته ───
COHERENCE_CHECKS = [
    # ═══ میراث v2.4.9 (بسته تجمعی است) ═══
    ("src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs",
     "using System.Windows.Media;", "using رسانه‌ای HexBrush (v2.4.9)"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs",
     "جابه‌جا نکنید", "هشدار ترتیب استاتیک (v2.4.9)"),
    # ═══ میراث v2.4.10 ═══
    ("src/Sahand.ClinicPro.Desktop/App.xaml",
     'x:Key="App.PlainToggleButton"', "سپر استایل سوییچ MD3 (۲.۴.۱۰)"),
    ("src/Sahand.ClinicPro.Application/Features/Lookups/ServiceTariffCommands.cs",
     "TariffFallbackShares", "کمک‌کنندهٔ سهم fallback تعرفه (۲.۴.۱۰)"),
    ("src/Sahand.ClinicPro.Desktop/Services/PhotoTools.cs",
     "DoctorThumbnailKey", "کلید کش بندانگشتی (۲.۴.۱۰)"),
    # ═══ ۱) تور آموزشی — لایهٔ درون‌پنجره‌ای ═══
    ("src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml",
     '<UserControl x:Class="Sahand.ClinicPro.Desktop.Views.GuidedTourWindow"',
     "تور کنترل کاربر شد (نه پنجرهٔ جدا)"),
    ("src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml.cs",
     "TranslatePoint(new Point(0, 0), (IInputElement)this)",
     "اندازه‌گیری در همان درخت بصری"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'x:Name="TourLayer"', "لایهٔ میزبان تور در پوسته"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml.cs",
     "ShowTourOverlay", "متد میزبانی تور"),
    # ═══ ۲) همزمانی لایسنس ═══
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "SemaphoreSlim UpsertGate", "گیت صف‌کنندهٔ جای‌گذاری لایسنس"),
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "DbUpdateConcurrencyException", "پوشش تلاش مجدد همزمانی"),
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "SaveSettingsWithRetryAsync", "تلاش مجدد تنظیمات لایسنس"),
    # ═══ ۳) سطح دسترسی ═══
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     "PagePermissionMap", "نقشهٔ صفحه ← دسترسی"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     "ApplyPermissionFilter", "پالایش منو با دسترسی"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     "FirstAllowedPageKey", "ناوبری اولیهٔ مجاز"),
    ("src/Sahand.ClinicPro.API/Controllers/UsersController.cs",
     "permissions = User.Claims", "دسترسی‌های زندهٔ users/me"),
    # ═══ ۴) پروفایل کاربر ═══
    ("src/Sahand.ClinicPro.Desktop/Services/TokenManager.cs",
     "UpdateProfile", "به‌روزرسانی پروفایل کنار توکن"),
    ("src/Sahand.ClinicPro.Desktop/Services/ApiClient.cs",
     "envelope.Data.DisplayName", "نگاشت پروفایل پاسخ ورود"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'ImageSource="{Binding CurrentUserPhoto}"', "عکس کاربر در پوسته"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     "CurrentUserPhoto", "پراپرتی عکس کاربر"),
    # ═══ ۵) گزارش‌ها و چاپ ═══
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReportBrowserViewModel.cs",
     "private void PrintReport()", "فرمان چاپ گزارش"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReportBrowserViewModel.cs",
     "ResultNotice", "پیام درون‌صفحه‌ای گزارش‌ساز"),
    ("src/Sahand.ClinicPro.Desktop/Views/ReportBrowserView.xaml",
     'Command="{Binding PrintCommand}"', "اتصال دکمهٔ چاپ"),
    ("src/Sahand.ClinicPro.Desktop/Views/PatientDetailWindow.xaml.cs",
     "OnPrintCardClick", "چاپ کارت بیمار"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/AccountingViewModel.cs",
     "ReportsNotice", "پیام شکست تراز/سودوزیان"),
    ("src/Sahand.ClinicPro.Desktop/Views/AccountingView.xaml",
     'Binding ReportsNotice', "بنر خطای درون تب حسابداری"),
    # ═══ ۶) وسط‌چین سربرگ ═══
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     "ستون ۱ (میانه): مسیر صفحه + نام صفحه + بج ساعت", "گروه میانه وسط‌چین"),
    # ═══ ۷) لاگ رنگی API ═══
    ("src/Sahand.ClinicPro.API/Logging/PersianConsoleTextFormatter.cs",
     "WriteLevelBadge", "بج رنگی سطح"),
    ("src/Sahand.ClinicPro.API/Logging/PersianConsoleTextFormatter.cs",
     "TryRenderHttpRequest", "خط ویژهٔ درخواست HTTP"),
    ("src/Sahand.ClinicPro.API/Program.cs",
     "UseSerilogRequestLogging", "لاگ ساخت‌یافتهٔ درخواست‌ها"),
    # ═══ ۸) توست ═══
    ("src/Sahand.ClinicPro.Desktop/Services/SnackbarService.cs",
     "enum ToastKind", "نوع چهارگانهٔ توست"),
    ("src/Sahand.ClinicPro.Desktop/Services/SnackbarService.cs",
     "record ToastMessage", "مدل پیام شناور"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'Command="{Binding DataContext.DismissToastCommand', "بستن توست با کلیک"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'x:Name="IconCircle"', "دایرهٔ آیکون کپسول توست"),
    # ═══ ۹) اسپلش ═══
    ("src/Sahand.ClinicPro.Desktop/Views/SplashWindow.xaml",
     'Background="#D9132B50" CornerRadius="18"', "کادر گوشه‌گرد زیر نوشته‌ها"),
    # ═══ ۱۰) نسخه ═══
    ("Directory.Build.props", "<Version>2.4.11</Version>", "نسخهٔ 2.4.11"),
    ("CHANGELOG.md", "[v2.4.11]", "ثبت چرخهٔ سی‌وهشتم در CHANGELOG"),
    # ═══ تست واحد هماهنگ ═══
    ("tests/Sahand.ClinicPro.Desktop.UnitTests/MoneyFlowViewModelTests.cs",
     "ToastKind.Success", "FakeSnackbar با قرارداد نو"),
]

# ─── نشانگرهای منفی — الگوهای معیوب باید حذف شده باشند ───
NEGATIVE_CHECKS = [
    # نسخهٔ کهنه
    ("Directory.Build.props", "<Version>2.4.10</Version>", "نسخهٔ کهنه حذف شد"),
    # پوشش خاکستری پشت دیالوگ‌ها (گزارش مالک: نمی‌خواهم باشه)
    ("src/Sahand.ClinicPro.Desktop/Views/ThemedDialog.xaml",
     "#66000000", "پوشش تیرهٔ دیالوگ حذف شد"),
    ("src/Sahand.ClinicPro.Desktop/Views/ColorPickerWindow.xaml",
     "#66000000", "پوشش تیرهٔ انتخاب‌گر رنگ حذف شد"),
    # سایهٔ لوگوی اسپلش (پشت تصویر باید کاملاً شفاف باشد)
    ("src/Sahand.ClinicPro.Desktop/Views/SplashWindow.xaml",
     'BlurRadius="22" ShadowDepth="6"', "سایهٔ پشت لوگوی اسپلش حذف شد"),
    # Snackbar متریال از پوسته حذف شد (جایش کپسول‌های توست نشسته)
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     "materialDesign:Snackbar", "اسنک‌بار متریال از پوسته حذف شد"),
    # آواتار قدیمی سایدبار/سربرگ (جایش عکس واقعی + آدمک سه‌لایه نشسته)
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'Kind="AccountCircle"', "آیکون آواتار قدیمی حذف شد"),
    # پیشوندهای ایموجی از پیام‌های پوسته
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     '🔔 {args.Title}', "پیشوند ایموجی زنگ حذف شد"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     '🏛 سرور لایسنس', "پیشوند ایموجی پیام لایسنس حذف شد"),
]

print("■ اجرای گاردهای انسجام چرخهٔ ۳۸…")
for rel, needle, desc in COHERENCE_CHECKS:
    assert needle in (REPO / rel).read_text(encoding="utf-8-sig"), f"❌ نشانگر سلامت یافت نشد [{desc}] در {rel}"
    print(f"  ✅ {desc}")
for rel, needle, desc in NEGATIVE_CHECKS:
    assert needle not in (REPO / rel).read_text(encoding="utf-8-sig"), f"❌ الگوی معیوب هنوز هست [{desc}] در {rel}"
    print(f"  ✅ {desc}")

# ─── گارد ترتیب استاتیک (میراث v2.4.9) ───
vm_txt = (REPO / "src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs").read_text(encoding="utf-8-sig")
i_types = vm_txt.find("public static TemplateTypeOption[] TemplateTypes")
i_filter = vm_txt.find("TemplateFilterOptions { get; }")
assert 0 < i_types < i_filter, \
    f"❌ ترتیب استاتیک شکسته است! TemplateTypes ({i_types}) باید قبل از TemplateFilterOptions ({i_filter}) باشد"
print("  ✅ گارد ترتیب استاتیک ComplementaryViewModel")

# ─── گارد قطعه‌ای ستون‌های تعرفه (میراث ۲.۴.۱۰) ───
ins_txt = (REPO / "src/Sahand.ClinicPro.Desktop/Views/InsuranceView.xaml").read_text(encoding="utf-8-sig")
seg_start = ins_txt.find('Header="قیمت تعرفه"')
seg_end = ins_txt.find('Header="وضعیت"', seg_start)
assert 0 < seg_start < seg_end, "❌ قطعهٔ ستون‌های تعرفه پیدا نشد"
seg = ins_txt[seg_start:seg_end]
assert seg.count('Value="Center"') == 3, "❌ ستون‌های تعرفه باید ۳ Center داشته باشند"
print("  ✅ ستون‌های عددی تعرفه وسط‌چین (میراث ۲.۴.۱۰)")

# ─── ممیزی خودکار using های رسانه‌ای کل سورس ───
res = subprocess.run([sys.executable, str(REPO / "scripts" / "audit_csharp_usings.py")],
                     capture_output=True, text=True)
assert res.returncode == 0, f"❌ ممیزی using های رسانه‌ای ناموفق:\n{res.stdout}{res.stderr}"
print(f"  ✅ {res.stdout.strip().splitlines()[-1]}")

# ─── گارد نهایی XML ───
res = subprocess.run([sys.executable, str(REPO / "scripts" / "validate_xaml.py")],
                     capture_output=True, text=True)
assert res.returncode == 0, f"❌ اعتبارسنجی XAML ناموفق:\n{res.stdout}"
print("  ✅ همهٔ فایل‌های XAML معتبرند")

# ─── تطبیق فهرست بسته با دیف واقعی + فایل‌های کلیدی این چرخه ───
assert set(CHANGED) == set(diff), "❌ عدم تطابق فهرست بسته با دیف گیت"
MUST_INCLUDE = {
    # چرخهٔ ۳۸ — فایل‌های ستون اصلی
    "src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml.cs",
    "src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/MainWindow.xaml.cs",
    "src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
    "src/Sahand.ClinicPro.Desktop/Services/SnackbarService.cs",
    "src/Sahand.ClinicPro.Desktop/Services/TokenManager.cs",
    "src/Sahand.ClinicPro.Desktop/Services/ITokenManager.cs",
    "src/Sahand.ClinicPro.Desktop/Services/ApiClient.cs",
    "src/Sahand.ClinicPro.Desktop/ViewModels/ReportBrowserViewModel.cs",
    "src/Sahand.ClinicPro.Desktop/Views/ReportBrowserView.xaml",
    "src/Sahand.ClinicPro.Desktop/ViewModels/AccountingViewModel.cs",
    "src/Sahand.ClinicPro.Desktop/Views/AccountingView.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/PatientDetailWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/PatientDetailWindow.xaml.cs",
    "src/Sahand.ClinicPro.Desktop/Views/ThemedDialog.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/ThemedDialog.xaml.cs",
    "src/Sahand.ClinicPro.Desktop/Views/ColorPickerWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/ColorPickerWindow.xaml.cs",
    "src/Sahand.ClinicPro.Desktop/Views/SplashWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/LicenseActivationWindow.xaml.cs",
    "src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
    "src/Sahand.ClinicPro.API/Controllers/UsersController.cs",
    "src/Sahand.ClinicPro.API/Logging/PersianConsoleTextFormatter.cs",
    "src/Sahand.ClinicPro.API/Program.cs",
    "tests/Sahand.ClinicPro.Desktop.UnitTests/MoneyFlowViewModelTests.cs",
}
missing = MUST_INCLUDE - set(CHANGED)
assert not missing, f"❌ فایل‌های کلیدی چرخهٔ ۳۸ در دیف نیستند: {missing}"
print(f"  ✅ بستهٔ {BASE}→HEAD دقیقاً {len(CHANGED)} فایل دارد (تجمعی: 2.4.9 + 2.4.10 + 2.4.11)")
print(f"  ✅ هر ۲۶ فایل کلیدی چرخهٔ ۳۸ در بسته حاضرند")

# ─── راهنمای HTML فارسی ───
GUIDE_HTML = """<!DOCTYPE html>
<html lang="fa" dir="rtl"><head><meta charset="utf-8">
<title>راهنمای به‌روزرسانی به ۲.۴.۱۱</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:880px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}h3{color:#1565C0;margin-top:22px}ol,ul{line-height:2.1}b{color:#2E7D32}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
.important{background:#FFEBEE;border-radius:10px;padding:14px 18px;border-right:4px solid #C62828;margin:14px 0}
.info{background:#E3F2FD;border-radius:10px;padding:12px 16px;border-right:4px solid #1E88E5;margin:14px 0}</style></head><body><div class="card">
<h1>📦 به‌روزرسانی سهند کلینیک‌پرو — نسخهٔ ۲.۴.۱۱ (چرخهٔ سی‌وهشتم)</h1>

<div class="ok">محتوا: <b>__COUNT__ فایل</b> — این بسته <b>«تجمعی از ۲.۴.۸»</b> است؛ وصلهٔ ۲.۴.۹ و چرخهٔ ۲.۴.۱۰ و هر ۱۱ اصلاح این چرخه را با هم دارد. اگر الان روی ۲.۴.۸ یا ۲.۴.۹ یا ۲.۴.۱۰ هستید — فقط همین یک بسته را نصب کنید. همه را با همان ساختار پوشه‌ای روی <code>E:\\Sahand_ClinicPro</code> کپی و <b>جایگزین</b> کنید.</div>

<div class="warn"><b>⭐ سه پروژه باید بازبیلد شوند:</b> این چرخه در <b>دسکتاپ</b> و <b>سرور API</b> (و Application/Infrastructure) تغییر دارد. پس از کپی: <code>BUILD ← Clean Solution</code> سپس <code>Rebuild Solution</code> و سرور API را هم با فایل‌های جدید publish و <b>ری‌استارت</b> کنید.</div>

<h3>۱۱ اصلاح این نسخه</h3>
<ul>
<li>🎓 <b>تور آموزشی اولین ورود</b> — هایلایت‌ها آینه‌ای جای اشتباه می‌افتادند (ریشه: رفتار RTL ویندوز در مختصات بین‌پنجره‌ای). تور به لایهٔ درون‌پنجره‌ای منتقل شد؛ برای دیدن دوباره: منوی «🎓 مرکز آموزش ← اجرای تور معرفی».</li>
<li>🔒 <b>خطای سرور API در تازه‌سازی لایسنس</b> — تصادم همزمانی (DbUpdateConcurrencyException) تپ‌قلب/تازه‌سازی‌های هم‌زمان روی رکورد لایسنس؛ با صف‌کردن و تلاش مجدد رفع شد. تازه‌سازی لایسنس از چند ایستگاه هم‌زمان حالا بی‌خطاست.</li>
<li>🛂 <b>سطح دسترسی</b> — منوی کناری فقط صفحاتی را نشان می‌دهد که کاربر دسترسی دارد (همان کلیدهای سرور). کاربران بدون دسترسی دیگر منوی خالی‌الوعد نمی‌بینند.</li>
<li>👤 <b>نام و تصویر کاربر</b> — پایین سایدبار و بالای صفحات: نام کامل در صورت وجود (وگرنه نام کاربری) + تصویر دایره‌ای کاربر (اگر برای پرسنلش عکس ثبت شده باشد).</li>
<li>🖨️ <b>چاپ گزارش‌ها</b> — دکمهٔ «چاپ» گزارش‌ساز فعال شد: جدول کامل با سربرگ و تاریخ شمسی مستقیم به چاپگر. شکست اجرای گزارش هم حالا با بنر قرمز داخل صفحه اعلام می‌شود (نه فقط یک پیام گذرا).</li>
<li>🖨️ <b>چاپ کارت بیمار</b> — دکمهٔ چاپ بالای پروندهٔ بیمار فعال شد: کارت شناسایی دوستونی با مشخصات و خط یادداشت.</li>
<li>📊 <b>گزارش‌های مالی حسابداری</b> — «به‌روزرسانی» تراز/سود و زیان حالا نتیجهٔ واقعی را اعلام می‌کند و علت خطا داخل خود تب نمایش داده می‌شود (پیش‌تر حتی روی شکست پیام موفقیت می‌داد).</li>
<li>🎯 <b>وسط‌چین سربرگ</b> — نام صفحه و بج ساعت در نوار بالای صفحات دقیقاً وسط‌چین شدند.</li>
<li>🎨 <b>لاگ سرور API</b> — کنسول حالا رنگی و قالب‌بندی‌شده است: بج رنگی سطح + خط ویژهٔ درخواست‌های HTTP با رنگ کد وضعیت (سبز/کهربایی/قرمز).</li>
<li>🔔 <b>پیام‌های توست</b> — مطابق تصویر نمونه: کپسول گوشه‌گرد با دایرهٔ رنگی نوع پیام (سبز تیک/قرمز ضربدر/کهربایی تعجب/آبی i)، حذف خودکار زمان‌بندی‌شده، کلیک = بستن.</li>
<li>✨ <b>دیالوگ‌ها و اسپلش</b> — کادر خاکستری پشت دیالوگ‌های تأیید حذف شد؛ در اسپلش پشت لوگو کاملاً شفاف است و نوشته‌ها داخل کادر گوشه‌گرد نشسته‌اند.</li>
</ul>

<h3>مراحل نصب</h3>
<ol>
<li>ویژوال استودیو، برنامهٔ دسکتاپ و سرور API را کامل ببندید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی <code>E:\\Sahand_ClinicPro</code> کپی و <b>جایگزین</b> کنید (__COUNT__ فایل)</li>
<li>در ویژوال استودیو: <b>BUILD ← Clean Solution</b> و سپس <b>Rebuild Solution</b></li>
<li>سرور API را با فایل‌های جدید publish کرده و سرویسش را ری‌استارت کنید (سرویس لایسنس + اندپوینت users/me + لاگ رنگی تغییر کرده‌اند)</li>
</ol>

<h3>بعد از نصب بررسی کنید (چک‌لیست ۹ موردی)</h3>
<ul>
<li>✅ زیر دکمهٔ «خروج از حساب» <b>«نسخه ۲.۴.۱۱»</b> دیده شود — اگر نبود فایل‌ها جایگزین نشده‌اند</li>
<li>✅ تور معرفی (مرکز آموزش ← اجرای تور) هایلایت‌ها را دقیقاً دور همان کنترل‌ها می‌کشد</li>
<li>✅ با کاربری غیر ادمین (نقش محدود) وارد شوید — منو فقط صفحات مجاز را نشان می‌دهد و نام کامل/عکس کاربر بالای صفحه و پایین سایدبار است</li>
<li>✅ صفحهٔ گزارش‌ها: یک گزارش را اجرا و دکمهٔ «چاپ» را بگیرید — پنجرهٔ چاپگر با جدول کامل باز می‌شود</li>
<li>✅ پروندهٔ بیمار ← دکمهٔ چاپگر بالای صفحه ← کارت بیمار چاپ می‌شود</li>
<li>✅ حسابداری ← تب تراز ← «به‌روزرسانی»: نتیجهٔ واقعی اعلام می‌شود (در صورت خطای سرور، بنر قرمز داخل تب)</li>
<li>✅ پیام‌های برنامه (مثلاً «گزارش اجرا شد») به‌شکل کپسول گوشه‌گرد با آیکون رنگی ظاهر می‌شوند؛ کلیک روی آن می‌بنددش</li>
<li>✅ دیالوگ‌های تأیید (مثلاً خروج از برنامه) دیگر قاب خاکستری دور کارت ندارند</li>
<li>✅ کنسول سرور API: خطوط لاگ رنگی با بج سطح و خطوط درخواست HTTP «← GET /api/... → 200 · 45ms»</li>
</ul>

<div class="info"><b>نکتهٔ سطح دسترسی:</b> فیلتر منو از همان کلیدهای مجوزی خوانده می‌شود که سرور اجرا می‌کند. اگر صفحه‌ای برای کاربری باید نمایان باشد، از «مدیریت سامانه ← نقش‌ها» مجوز مشاهدهٔ ماژول مربوطه را به نقشش اضافه کنید (مثلاً patients.view برای بیماران). نقش Admin همیشه همه‌چیز را می‌بیند.</div>
</div></body></html>
""".replace("__COUNT__", str(len(CHANGED)))

# ─── ساخت زیپ ───
OUT.parent.mkdir(parents=True, exist_ok=True)
if OUT.exists():
    OUT.unlink()

with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED) as z:
    z.writestr(GUIDE_NAME, GUIDE_HTML)
    for rel in CHANGED:
        z.write(REPO / rel, rel)
    z.writestr(MANIFEST_NAME,
               "بستهٔ تجمعی v2.4.11 — همهٔ تغییرات v2.4.8 تا v2.4.11 (شامل وصلهٔ 2.4.9 و چرخهٔ 2.4.10)\n"
               "پس از کپی: Clean Solution سپس Rebuild Solution + بازبیلد/ری‌استارت سرور API\n"
               "چک‌لیست ۹ موردی داخل راهنمای HTML\n"
               + "-" * 70 + "\n" + "\n".join(CHANGED) + "\n")

print(f"\n✅ بستهٔ آپدیت ساخته شد: {OUT}")
print(f"   حجم: {OUT.stat().st_size:,} بایت | فایل‌ها: {len(CHANGED)}")
with zipfile.ZipFile(OUT) as z:
    names = z.namelist()
    print(f"   • راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME} | سورس: {len(names)-2} فایل")
