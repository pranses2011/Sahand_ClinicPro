#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت → v2.5.0 — چرخهٔ سی‌ونه‌م (گزارش مالک).

بستهٔ «دلتا از v2.4.11» — ایستگاه روی ۲.۴.۱۱ با همین یک بسته به ۲.۵.۰ می‌رسد.

گاردهای انسجام این چرخه (همه باید در اولین اجرا پاس شوند):
  • نشانگرهای سلامت هر ۶ گزارش مالک چرخهٔ ۳۹
  • گاردهای منفی — الگوهای معیوب باید حذف شده باشند
  • ممیزی خودکار using + اعتبارسنجی XML + ممیزی بایتی آیکون‌های جدید از DLL
  • تطبیق دقیق فهرست بسته با دیف واقعی گیت v2.4.11..HEAD
"""
import pathlib
import subprocess
import sys
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.11"
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.11-to-2.5.0.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.5.0.html"

EXCLUDES = [
    '":(exclude)docs/*"',
    '":(exclude)scripts/*"',
]

# ─── فهرست بسته = دیف واقعی v2.4.11..HEAD ───
diff_cmd = f'git -C "{REPO}" diff --name-only {BASE}..HEAD -- . ' + " ".join(EXCLUDES)
diff = subprocess.run(diff_cmd, shell=True, capture_output=True, text=True, check=True).stdout.split()
CHANGED = sorted(set(diff))
print(f"■ فهرست بسته: {len(CHANGED)} فایل از {BASE}..HEAD")

# ─── گارد انسجام مثبت — نشانگرهای سلامت چرخهٔ ۳۹ ───
COHERENCE_CHECKS = [
    # ═══ ۱) بج تاریخ پیام سرور لایسنس → گوشه بالا-چپ ═══
    ("src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml",
     "چرخهٔ سی‌ونه‌م (گزارش مالک) — جای بج تاریخ", "کامنت ریشه‌یابی بج تاریخ"),
    ("src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml",
     'Text="{Binding DateFa, Mode=OneWay}"', "بج تاریخ شمسی"),
    # ═══ ۲) دکمه‌های ضدفشردگی ═══
    ("src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml",
     'MinWidth="195" MinHeight="46"', "ضدفشردگی دکمهٔ پذیرش و فاکتور"),
    ("src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml",
     'MinWidth="215" MinHeight="46"', "ضدفشردگی دکمهٔ پذیرش و پرداخت"),
    # ═══ ۳) بک‌اند اتاق ═══
    ("src/Sahand.ClinicPro.Domain/Scheduling/Reception.cs",
     "public long? RoomId { get; set; }", "ستون RoomId موجودیت پذیرش"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261011120000_Cycle39ReceptionRoom.cs",
     'Migration("20261011120000_Cycle39ReceptionRoom")', "مهاجرت اتاق"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261011120000_Cycle39ReceptionRoom.cs",
     "IX_Receptions_RoomId", "ایندکس اتاق"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/ClinicProDbContextModelSnapshot.cs",
     'b.HasIndex("RoomId");', "اسنپ‌شات RoomId"),
    # ═══ ۴) بک‌اند لوازم مصرفی ═══
    ("src/Sahand.ClinicPro.Domain/Enums/MedicationItemKind.cs",
     "Consumable = 2", "enum نوع قلم انبار"),
    ("src/Sahand.ClinicPro.Domain/Enums/PharmacyReferenceType.cs",
     "Reception = 5", "منشأ تراکنش انبار: پذیرش"),
    ("src/Sahand.ClinicPro.Application/Features/Pharmacy/ConsumablesFeatures.cs",
     "record ConsumeStockCommand", "فرمان مصرف FEFO"),
    ("src/Sahand.ClinicPro.Application/Features/Pharmacy/ConsumablesFeatures.cs",
     "PharmacyReferenceType.Reception", "تراکنش با منشأ پذیرش"),
    ("src/Sahand.ClinicPro.Application/Features/Lookups/MedicationCommands.cs",
     "record CreatePharmacyItemCommand", "فرمان ساخت قلم انبار"),
    ("src/Sahand.ClinicPro.API/Controllers/PharmacyController.cs",
     '[HttpPost("consume")]', "اندپوینت مصرف"),
    ("src/Sahand.ClinicPro.API/Controllers/PharmacyController.cs",
     '[HttpGet("consumables")]', "اندپوینت فهرست مصرفی‌ها"),
    ("src/Sahand.ClinicPro.API/Controllers/LookupsController.cs",
     '[HttpPost("medications")]', "اندپوینت ساخت قلم"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261011130000_Cycle39MedicationItemKind.cs",
     'Migration("20261011130000_Cycle39MedicationItemKind")', "مهاجرت ItemKind"),
    ("src/Sahand.ClinicPro.Application/Features/Billing/CreateInvoice/CreateInvoiceCommand.cs",
     "IsInsuranceExempt", "معافیت بیمه‌ای اقلام فاکتور"),
    # ═══ ۵) دسکتاپ اتاق‌ها ═══
    ("src/Sahand.ClinicPro.Desktop/Views/RoomsView.xaml",
     'x:Class="Sahand.ClinicPro.Desktop.Views.RoomsView"', "صفحهٔ مدیریت اتاق‌ها"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     '"مدیریت اتاق‌ها", PageKey = "rooms"', "آیتم منوی اتاق‌ها"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "roomId = isServiceMode ? SelectedRoom?.Id", "ارسال اتاق پذیرش خدمتی"),
    # ═══ ۶) دسکتاپ مصرفی‌ها ═══
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "pharmacy/consume", "مصرف در زنجیرهٔ پذیرش"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "isInsuranceExempt = true", "اقلام معاف از بیمه در فاکتور"),
    ("src/Sahand.ClinicPro.Desktop/Views/PharmacyItemFormWindow.xaml",
     'x:Class="Sahand.ClinicPro.Desktop.Views.PharmacyItemFormWindow"', "فرم قلم جدید"),
    ("src/Sahand.ClinicPro.Desktop/Views/PharmacyView.xaml",
     'Command="{Binding AddItemCommand}"', "دکمهٔ افزودن قلم داروخانه"),
    ("src/Sahand.ClinicPro.Desktop/Views/PharmacyView.xaml",
     'Header="نوع"', "ستون نوع انبار"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/PharmacyViewModel.cs",
     "InventoryKindIndex", "فیلتر نوع انبار"),
    # ═══ ۷) کارت‌خوان پذیرش ═══
    ("src/Sahand.ClinicPro.Desktop/Views/ReceptionPayWindow.xaml",
     'x:Class="Sahand.ClinicPro.Desktop.Views.ReceptionPayWindow"', "پنجرهٔ پرداخت پذیرش"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionPayViewModel.cs",
     "record ReceptionPaymentInfo", "مدل نتیجهٔ پرداخت"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "method = payment.Method", "روش پرداخت از پنجره در payments"),
    # ═══ ۸) بستن شیفت ═══
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ShiftCloseViewModel.cs",
     "payments/today-summary", "خواندن مبالغ سیستم شیفت"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ShiftCloseViewModel.cs",
     "public decimal LiveNetDiff", "مغایرت خالص زنده"),
    ("src/Sahand.ClinicPro.Desktop/Views/ShiftCloseWindow.xaml",
     "مغایرت زنده", "جدول مغایرت‌گیری شیفت"),
    # ═══ ۹) میانبرهای داشبورد ═══
    ("src/Sahand.ClinicPro.Desktop/Helpers/PageCatalog.cs",
     "public static class PageCatalog", "کاتالوگ یکپارچهٔ صفحات"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs",
     "shortcuts.{_tokens.Username", "ذخیرهٔ per-user میانبرها"),
    ("src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml",
     "میانبرهای سریع من", "پنل میانبرهای داشبورد"),
    ("src/Sahand.ClinicPro.Desktop/Views/ShortcutPickerWindow.xaml",
     'x:Class="Sahand.ClinicPro.Desktop.Views.ShortcutPickerWindow"', "انتخابگر میانبر"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     "Helpers.PageCatalog.PermissionMap", "منوی کناری از کاتالوگ می‌خواند"),
    # ═══ ۱۰) مبدل EmptyList ═══
    ("src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs",
     "class EmptyListToVisibilityConverter", "مبدل فهرست خالی"),
    ("src/Sahand.ClinicPro.Desktop/App.xaml",
     'x:Key="EmptyListToVisibility"', "ثبت مبدل EmptyList"),
    # ═══ ۱۱) نسخه ═══
    ("Directory.Build.props", "<Version>2.5.0</Version>", "نسخهٔ 2.5.0"),
    ("CHANGELOG.md", "[v2.5.0]", "ثبت چرخهٔ سی‌ونه‌م در CHANGELOG"),
]

# ─── نشانگرهای منفی — الگوهای معیوب باید حذف شده باشند ───
NEGATIVE_CHECKS = [
    # نسخهٔ کهنه
    ("Directory.Build.props", "<Version>2.4.11</Version>", "نسخهٔ کهنه حذف شد"),
    # پرداخت hardcode نقدی در پذیرش (جایش پنجرهٔ روش پرداخت نشسته)
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "withPayment", "پارامتر بولی پرداخت حذف شد"),
    # متن کهنهٔ دکمهٔ نقدی (نقل‌قول داخل کامنت‌ها مجاز است — فقط متن دکمه)
    ("src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml",
     'Text="💵 پذیرش و پرداخت نقدی"', "متن کهنهٔ دکمهٔ پرداخت حذف شد"),
    # دیکشنری کپی‌شدهٔ نقشهٔ مجوز (جایش کاتالوگ نشسته)
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     '["dashboard"] = "dashboard.view"', "نقشهٔ مجوز تکثیرشده حذف شد"),
    # کمبوباکس هم‌پرانتز با /// (درس CS1587)
    ("src/Sahand.ClinicPro.Application/Features/Pharmacy/ConsumablesFeatures.cs",
     '/// <summary>شناسهٔ پذیرشِ', "کامنت XML داخل پرانتز رکورد حذف شد"),
]

print("■ اجرای گاردهای انسجام چرخهٔ ۳۹…")
for rel, needle, desc in COHERENCE_CHECKS:
    assert needle in (REPO / rel).read_text(encoding="utf-8-sig"), f"❌ نشانگر سلامت یافت نشد [{desc}] در {rel}"
    print(f"  ✅ {desc}")
for rel, needle, desc in NEGATIVE_CHECKS:
    assert needle not in (REPO / rel).read_text(encoding="utf-8-sig"), f"❌ الگوی معیوب هنوز هست [{desc}] در {rel}"
    print(f"  ✅ {desc}")

# ─── گارد میراث: اسنپ‌شات باید هر سه نشانگر اتاق را داشته باشد ───
snap = (REPO / "src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/ClinicProDbContextModelSnapshot.cs").read_text(encoding="utf-8-sig")
ent = 'modelBuilder.Entity("Sahand.ClinicPro.Domain.Entities.Scheduling.Reception", b =>'
first = snap.find(ent); second = snap.find(ent, first + 10)
block = snap[first:second]
assert 'b.Property<long?>("RoomId")' in block, "❌ RoomId در بلوک Reception اسنپ‌شات نیست"
assert 'b.HasIndex("RoomId");' in block, "❌ ایندکس RoomId در بلوک Reception اسنپ‌شات نیست"
print("  ✅ اسنپ‌شات: پراپرتی + ایندکس RoomId در بلوک Reception")

# ─── گارد میراث: ItemKind در بلوک Medication ───
ment = 'modelBuilder.Entity("Sahand.ClinicPro.Domain.Entities.Clinical.Medication", b =>'
mfirst = snap.find(ment); msecond = snap.find(ment, mfirst + 10)
mblock = snap[mfirst:msecond]
assert 'b.Property<byte>("ItemKind")' in mblock, "❌ ItemKind در بلوک Medication اسنپ‌شات نیست"
print("  ✅ اسنپ‌شات: ItemKind در بلوک Medication")

# ─── ممیزی خودکار using های رسانه‌ای ───
res = subprocess.run([sys.executable, str(REPO / "scripts" / "audit_csharp_usings.py")],
                     capture_output=True, text=True)
assert res.returncode == 0, f"❌ ممیزی using ناموفق:\n{res.stdout}{res.stderr}"
print(f"  ✅ {res.stdout.strip().splitlines()[-1]}")

# ─── اعتبارسنجی XML همهٔ XAMLها ───
import xml.etree.ElementTree as ET
xaml_count = 0
for path in (REPO / "src" / "Sahand.ClinicPro.Desktop").rglob("*.xaml"):
    if any(p in ("obj", "bin") for p in path.parts):
        continue
    ET.parse(path)
    xaml_count += 1
print(f"  ✅ {xaml_count} فایل XAML معتبر")

# ─── ممیزی بایتی آیکون‌های جدید از DLL متریال‌دیزاین ───
import io as _io, zipfile as _zf
dll = _zf.ZipFile("/home/z/.nuget/packages/materialdesignthemes/5.3.2/materialdesignthemes.5.3.2.nupkg") \
    .read("lib/net8.0-windows7.0/MaterialDesignThemes.Wpf.dll")
for icon in ["Syringe", "PlusBox", "DoorOpen", "DoorClosed", "Door", "LightningBolt", "Pencil"]:
    assert dll.find(icon.encode() + b"\x00") != -1, f"❌ آیکون {icon} در DLL نیست"
print("  ✅ آیکون‌های جدید چرخه (Syringe/PlusBox/Door*/LightningBolt/Pencil) در DLL تأیید")

# ─── فایل‌های کلیدی این چرخه باید در بسته باشند ───
MUST_INCLUDE = {
    "src/Sahand.ClinicPro.Desktop/Views/RoomsView.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/PharmacyItemFormWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/ReceptionPayWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/ShortcutPickerWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/Helpers/PageCatalog.cs",
    "src/Sahand.ClinicPro.Application/Features/Pharmacy/ConsumablesFeatures.cs",
    "src/Sahand.ClinicPro.Application/Features/Lookups/MedicationCommands.cs",
    "src/Sahand.ClinicPro.Domain/Enums/MedicationItemKind.cs",
    "src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261011120000_Cycle39ReceptionRoom.cs",
    "src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261011130000_Cycle39MedicationItemKind.cs",
    "src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/ClinicProDbContextModelSnapshot.cs",
    "src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/ShiftCloseWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/PharmacyView.xaml",
    "src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
    "src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs",
    "src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/App.xaml",
    "Directory.Build.props",
    "CHANGELOG.md",
}
missing = MUST_INCLUDE - set(CHANGED)
assert not missing, f"❌ فایل‌های کلیدی در دیف نیستند: {missing}"
print(f"  ✅ همهٔ {len(MUST_INCLUDE)} فایل کلیدی چرخه در بسته هستند")

# ═══════════════ راهنمای فارسی آپدیت ═══════════════
GUIDE_HTML = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8" />
<title>راهنمای آپدیت ۲.۴.۱۱ → ۲.۵.۰</title>
<style>
  body { font-family: Vazirmatn, Tahoma, sans-serif; background:#F5F7FB; color:#1F2937; margin:0; padding:24px; line-height:1.9; }
  .card { max-width:860px; margin:0 auto 16px; background:#fff; border:1px solid #E0E0E0; border-radius:14px; padding:22px 26px; box-shadow:0 1px 4px rgba(0,0,0,.05); }
  h1 { font-size:21px; color:#1565C0; margin:0 0 6px; }
  h2 { font-size:16px; color:#0D47A1; margin:22px 0 8px; border-right:4px solid #1565C0; padding-right:10px; }
  .warn { background:#FFF8E1; border:1px solid #FFE082; border-radius:10px; padding:12px 16px; margin:12px 0; }
  ol, ul { padding-right:22px; } li { margin:6px 0; }
  code { background:#ECEFF1; border-radius:6px; padding:2px 7px; font-family:Consolas, monospace; font-size:12.5px; direction:ltr; display:inline-block; }
  .tag { display:inline-block; background:#E3F2FD; color:#0D47A1; border-radius:8px; padding:2px 10px; font-size:12px; margin:0 3px; }
  table { border-collapse:collapse; width:100%; margin:10px 0; } th,td { border:1px solid #E0E0E0; padding:8px 10px; font-size:13px; text-align:right; } th { background:#E3F2FD; }
</style>
</head>
<body>
<div class="card">
  <h1>📦 آپدیت درمانگاه سهند — نسخهٔ ۲.۵.۰ (چرخهٔ سی‌ونه‌م)</h1>
  <p>این بسته از نسخهٔ <b>۲.۴.۱۱</b> به <b>۲.۵.۰</b> ارتقا می‌دهد — <b>۴۹ فایل</b> تغییرکرده (دسکتاپ + سرور API + دیتابیس). اگر روی نسخهٔ قدیمی‌تر از ۲.۴.۱۱ هستید، نخست بستهٔ تجمعی 2.4.8-to-2.4.11 را نصب کنید.</p>
  <div class="warn">⚠️ <b>این چرخه هم دسکتاپ و هم سرور API تغییر دارد:</b> پس از جایگزینی فایل‌ها حتماً <b>Clean + Rebuild</b> کامل بزنید و سرور API را <b>publish و ری‌استارت</b> کنید. دو مهاجرت دیتابیس (ستون اتاق پذیرش + نوع قلم انبار) در نخستین اجرای سرور خودکار اعمال می‌شوند.</div>
</div>

<div class="card">
  <h2>✨ چه چیزهایی جدید است؟</h2>
  <table>
    <tr><th>قابلیت</th><th>توضیح</th></tr>
    <tr><td>🧰 <b>لوازم مصرفی درمانگاه</b></td><td>سرنگ/سرم/گاز و… وقتی بیمار همراه ندارد: در پذیرش خدمتی انتخاب و هزینه‌اش به فاکتور بیمار اضافه می‌شود؛ خرید و انبار و قیمت از ماژول داروخانه</td></tr>
    <tr><td>💳 <b>کارت‌خوان در پذیرش</b></td><td>دکمهٔ «پذیرش و پرداخت» اکنون روش (نقدی/کارتخوان + مرجع + چهار رقم کارت) را می‌پرسد</td></tr>
    <tr><td>🚪 <b>اتاق خدمات درمانی + مدیریت اتاق‌ها</b></td><td>انتخابگر اتاق در پذیرش خدمتی + صفحهٔ «مدیریت اتاق‌ها» در منوی مدیریت سامانه</td></tr>
    <tr><td>⚡ <b>میانبرهای سریع داشبورد</b></td><td>کاشی میانبر شخصی برای هر صفحه — مختص هر کاربر جدا ذخیره می‌شود</td></tr>
    <tr><td>🌙 <b>بستن شیفت هوشمند</b></td><td>مبالغ سیستم خودکار می‌آید؛ شمارش را بنویسید تا مغایرت زنده رنگی نشان داده شود</td></tr>
    <tr><td>📅 <b>بج تاریخ پیام‌های سرور</b></td><td>به گوشهٔ بالا-چپ کادر پیام رفت (رفع رفتار RTL ویندوز)</td></tr>
    <tr><td>🔘 <b>دکمه‌های له‌شده</b></td><td>دکمه‌های «پذیرش و فاکتور / پذیرش و پرداخت» با سه‌لایهٔ ضدفشردگی اصلاح شدند</td></tr>
  </table>
</div>

<div class="card">
  <h2>🧰 راه‌اندازی ماژول لوازم مصرفی (نخستین بار)</h2>
  <ol>
    <li><b>داروخانه ← «افزودن قلم»</b>: نوع را «لوازم مصرفی» بگذارید و سرنگ ۵ سی‌سی، سرم، گاز استریل و… را بسازید (نام انگلیسی می‌تواند همان فارسی باشد).</li>
    <li><b>داروخانه ← «ورود کالا»</b>: نوع را «فقط لوازم مصرفی» بگذارید، قلم را انتخاب و بچ/تعداد/قیمت خرید و فروش را ثبت کنید.</li>
    <li><b>پذیرش ← حالت «خدمت درمانی»</b>: بخش آبی «لوازم مصرفی درمانگاه» نمایان می‌شود؛ قلم و تعداد را اضافه کنید — جمع همان لحظه به «قابل پرداخت بیمار» می‌چسبد و با ثبت، موجودی انبار کم و به فاکتور می‌نشیند (۱۰۰٪ بر عهدهٔ بیمار، بدون سهم بیمه).</li>
  </ol>
  <div class="warn">🔐 <b>مجوز:</b> برای ثبت مصرف لوازم در پذیرش، نقش کاربرِ پذیرش به مجوز <code>pharmacy.create</code> نیاز دارد (ادمین همه‌چیز را دارد)؛ از منوی نقش‌ها اضافه کنید.</div>
</div>

<div class="card">
  <h2>⚡ میانبرهای شخصی داشبورد</h2>
  <ul>
    <li>روی داشبورد، پنل «میانبرهای سریع من» ← دکمهٔ نارنجی <b>«میانبر جدید»</b> ← صفحه را جستجو و انتخاب کنید.</li>
    <li>برای حذف: دکمهٔ <b>«ویرایش»</b> را روشن کنید، ✖ گوشهٔ کاشی را بزنید؛ با خاموش‌کردن ویرایش، خودکار ذخیره می‌شود.</li>
    <li>میانبرها برای هر کاربر جدا (در ایستگاه شما) ذخیره می‌شوند — دو کاربر یک دستگاه، دو پنل مستقل.</li>
  </ul>
</div>

<div class="card">
  <h2>✅ چک‌لیست پس از نصب</h2>
  <ol>
    <li>زیر دکمهٔ «خروج از حساب»، <b>«نسخه ۲.۵.۰»</b> دیده شود.</li>
    <li>کنسول سرور API بدون خطا بالا بیاید (مهاجرت‌های <code>Cycle39ReceptionRoom</code> و <code>Cycle39MedicationItemKind</code> اعمال شدند).</li>
    <li>داشبورد: پنل «میانبرهای سریع من» + افزودن یک میانبر + کلیک روی کاشی → صفحه باز شود.</li>
    <li>داروخانه: «افزودن قلم» با نوع «لوازم مصرفی» + فیلتر نوع تب موجودی (همه/دارو/مصرفی).</li>
    <li>پذیرش خدمتی: انتخابگر «اتاق انجام خدمت» + بخش لوازم مصرفی + «قابل پرداخت بیمار» شامل مصرفی‌ها.</li>
    <li>«پذیرش و پرداخت»: پنجرهٔ روش پرداخت (نقدی/کارتخوان) باز شود و ثبت با روش منتخب انجام شود.</li>
    <li>مالی و صندوق ← «بستن شیفت»: مبالغ سیستم در ستون «مبلغ سیستم» آمده و با تایپ شمارش، مغایرت زنده رنگی شود.</li>
    <li>منوی «مدیریت سامانه» ← «مدیریت اتاق‌ها» صفحه‌ی جدید باز شود.</li>
    <li>پیام‌های سرور لایسنس: بج تاریخ در <b>گوشهٔ بالا-چپ</b> کادر پیام.</li>
  </ol>
</div>

<div class="card">
  <h2>📋 فهرست فایل‌ها</h2>
  <p>فهرست کامل ۴۹ فایل در <code>changed-files.txt</code> داخل همین بسته است؛ همه را با حفظ ساختار پوشه‌ها در ریشهٔ سورس کپی کنید. <b>توجه:</b> فایل‌های سرور (پوشه‌های API / Application / Domain / Infrastructure) پس از کپی، نیازمند publish مجدد سرور هستند.</p>
</div>
</body>
</html>
"""

# ═══════════════ ساخت زیپ ═══════════════
OUT.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
    for rel in CHANGED:
        zf.write(REPO / rel, rel)

    # مانیفست فهرست تغییرات
    manifest = "\n".join(CHANGED) + "\n"
    zf.writestr(MANIFEST_NAME, manifest)

    # راهنمای فارسی
    zf.writestr(GUIDE_NAME, GUIDE_HTML)

size_kb = OUT.stat().st_size // 1024
print(f"\n✅ بسته ساخته شد: {OUT.name} — {len(CHANGED)} فایل + راهنما + مانیفست — {size_kb} KB")
