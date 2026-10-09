#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.4.3 → v2.4.4 — چرخهٔ سی‌ویکم (گزارش کامل مالک).
فقط فایل‌های تغییرکردهٔ همین رلیز (تگ v2.4.3 تا HEAD) با همان ساختار
پوشه‌ای مخزن + مانیفست متنی + راهنمای فارسی HTML.

گاردهای انسجام: نشانه‌های سلامت تغییرات کلیدی این نسخه باید در سورس موجود
باشند و نشانه‌های الگوی معیوب حذف شده باشند وگرنه اسکریپت متوقف می‌شود.
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.3"    # رلیز چرخهٔ سی‌امین
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.3-to-2.4.4.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.4.html"

# ─── گارد انسجام مثبت — نشانه‌های سلامت تغییرات کلیدی این نسخه ───
COHERENCE_CHECKS = [
    # ۱) کرش صفحهٔ پرسنل
    ("src/Sahand.ClinicPro.Desktop/Views/PersonnelView.xaml",
     'Style="{StaticResource App.RowIconButton}"\n                                        ToolTip="ویرایش اطلاعات حقوقی',
     "آکولاد باز در استایل دکمهٔ حقوقی پرسنل"),
    # ۲) نقش‌های کاربر در فرم کاربر
    ("src/Sahand.ClinicPro.Desktop/Views/UserFormWindow.xaml",
     'IsChecked="{Binding IsChecked, Mode=TwoWay}"',
     "بایندینگ TwoWay چک‌باکس نقش‌ها"),
    # ۳) سیدر قالب چاپ — ذخیره و پیش‌فرض تکی
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/PrintTemplateSeeder.cs",
     "if (addedAny)\n            await _context.SaveChangesAsync(cancellationToken);",
     "ذخیرهٔ مستقل سیدر قالب‌ها"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/PrintTemplateSeeder.cs",
     "newlyDefaultedTypes",
     "پیش‌فرض تک‌نمونه‌ای برای هر نوع"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/SeedData/PrintTemplateSeedData.cs",
     "فاکتور کلاسیک — خطوط ظریف",
     "طرح کلاسیک فاکتور"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Reporting/PrintTemplateConfiguration.cs",
     "HasMaxLength(20)",
     "گسترش ستون PaperSize به ۲۰ نویسه"),
    ("src/Sahand.ClinicPro.API/Controllers/PrintTemplatesController.cs",
     "IsValidPaperSize",
     "اعتبارسنجی کامل اندازه‌های کاغذ"),
    ("src/Sahand.ClinicPro.Application/Features/Reporting/PrintTemplates/PrintTemplateRenderer.cs",
     "ResolvePaperWidthPx",
     "عرض پیکسلی همهٔ اندازه‌های کاغذ"),
    # ۴) فیلترهای EF
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/ClinicProDbContext.cs",
     "HasQueryFilter(ur => !ur.User!.IsDeleted && !ur.Role!.IsDeleted)",
     "فیلتر منطبق UserRole"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/ClinicProDbContext.cs",
     "HasQueryFilter(rt => !rt.User!.IsDeleted)",
     "فیلتر منطبق RefreshToken"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Billing/InsuranceCompanyConfiguration.cs",
     'DefaultCoveragePercent).HasPrecision(5, 2)',
     "دقت اعشار درصد پوشش"),
    # ۵) همگام‌سازی لایسنس
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "await SyncClinicInfoFromLicenseAsync(result.License, ct);",
     "همگام‌سازی در فعال‌سازی برخط"),
    ("src/Sahand.ClinicPro.Infrastructure/Services/License/LicenseService.cs",
     "await SyncClinicInfoFromLicenseAsync(lic, ct);",
     "همگام‌سازی در فعال‌سازی برون‌خط"),
    ("src/Sahand.ClinicPro.API/Program.cs",
     "تپ قلب یکبارهٔ استارتاپ",
     "تپ قلب استارتاپ"),
    ("src/Sahand.ClinicPro.API/Program.cs",
     "TimeSpan.FromSeconds(45)",
     "تأخیر ۴۵ ثانیه‌ای تپ قلب"),
    # ۶) مالی — پذیرش
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     'description = $"ویزیت {SelectedDoctor!.Name}"',
     "نام صحیح قلم ویزیت در فاکتور"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "invoiceFailed",
     "گزارش صادقانهٔ شکست فاکتور"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "Views.AppDialog.Error",
     "دیالوگ فارسی به‌جای بلع خطا"),
    # ۷) مالی — فرم دستی صندوق
    ("src/Sahand.ClinicPro.Desktop/ViewModels/BillingViewModel.cs",
     "description = i.Name",
     "نام صحیح اقلام در فاکتور دستی"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/BillingViewModel.cs",
     "paymentOk",
     "بررسی نتیجهٔ پرداخت فاکتور دستی"),
    # ۸) کدینگ حسابداری
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/AccountingAccountSeeder.cs",
     '("1101", "صندوق"',
     "حساب تفصیلی صندوق"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/AccountingAccountSeeder.cs",
     '("4101", "درآمد خدمات درمانی"',
     "حساب درآمد خدمات"),
    # ۹) ستون‌های مالی پذیرش
    ("src/Sahand.ClinicPro.Application/DTOs/Receptions/ReceptionDtos.cs",
     "decimal InvoiceTotal",
     "فیلد جمع فاکتور در DTO پذیرش"),
    ("src/Sahand.ClinicPro.Application/Features/Receptions/GetTodayReceptions/GetTodayReceptionsQuery.cs",
     "invoiceTotals.TryGetValue",
     "پیوست مبالغ مالی به پذیرش‌های روز"),
    ("src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml",
     "وضعیت مالی",
     "ستون وضعیت مالی در جدول پذیرش"),
    # ۱۰) منو
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'TextAlignment="Center"',
     "وسط‌چین‌سازی نام برنامه"),
    # نسخه
    ("Directory.Build.props",
     "<Version>2.4.4</Version>", "نسخهٔ 2.4.4"),
    ("CHANGELOG.md",
     "[v2.4.4]", "ثبت چرخهٔ سی‌ویکم در CHANGELOG"),
    # مهاجرت‌های دیتابیس
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261009200901_Cycle31PaperSizeLength.cs",
     "nvarchar(20)",
     "مهاجرت گسترش PaperSize"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261009201526_Cycle31PrintTemplatePaperAndFilters.cs",
     "decimal(5,2)",
     "مهاجرت دقت DefaultCoveragePercent"),
]

# نشانه‌های منفی — الگوهای معیوب باید حذف شده باشند
NEGATIVE_CHECKS = [
    ("src/Sahand.ClinicPro.Desktop/Views/PersonnelView.xaml",
     'Style="StaticResource',
     "استایل رشته‌ای بدون آکولاد حذف شد"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     'serviceName = $"ویزیت',
     "فیلد ناهمخوان serviceName در فاکتور پذیرش حذف شد"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs",
     "serviceName = SelectedService.Name, quantity = 1, unitPrice = SelectedService.Price, discount = 0m",
     "فیلد ناهمخوان سرویس در فاکتور پذیرش حذف شد"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/BillingViewModel.cs",
     "serviceName = i.Name",
     "فیلد ناهمخوان در فاکتور دستی حذف شد"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/BillingViewModel.cs",
     "discount = 0m,",
     "فیلد discount ناهمخوان در فاکتور دستی حذف شد"),
    ("src/Sahand.ClinicPro.Infrastructure/Persistence/Seeders/AccountingAccountSeeder.cs",
     "if (await _context.AccountingAccounts.AnyAsync(cancellationToken)) return;",
     "الگوی ردشدن کامل بذر حسابداری حذف شد"),
    ("src/Sahand.ClinicPro.API/Controllers/PrintTemplatesController.cs",
     "return \"اندازه کاغذ باید A4 یا A5 یا A6 باشد\";",
     "اعتبارسنجی سه‌گانهٔ قدیمی کاغذ حذف شد"),
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
<title>راهنمای به‌روزرسانی ۲.۴.۳ به ۲.۴.۴</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:780px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}ol{line-height:2.1}b{color:#2E7D32}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
.info{background:#E3F2FD;border-radius:10px;padding:12px 16px;border-right:4px solid #1E88E5;margin:14px 0}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
ul{line-height:2}</style></head><body><div class="card">
<h1>📦 بستهٔ به‌روزرسانی سهند کلینیک‌پرو — ۲.۴.۳ به ۲.۴.۴</h1>
<div class="ok">این بسته فقط فایل‌های تغییرکرده را دارد؛ همه را با همان ساختار پوشه‌ای روی نسخهٔ ۲.۴.۳ کپی و جایگزین کنید و سپس Solution را دوباره بیلد کنید.</div>
<h3>مراحل نصب</h3>
<ol>
<li>برنامهٔ دسکتاپ و <b>سرور API</b> را کامل ببندید (مطمئن شوید پردازهٔ Sahand.ClinicPro.API در حال اجرا نیست)</li>
<li>از پوشهٔ پروژهٔ فعلی، یک نسخهٔ پشتیبان (کپی کامل) تهیه کنید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی پوشهٔ پروژه کپی و <b>جایگزین</b> کنید — چهار فایل مهاجرت جدید در <code>Persistence/Migrations</code> هم داخل بسته هستند</li>
<li>Solution را بیلد و سرور API را اجرا کنید</li>
<li>در نخستین اجرای API، <b>دو مهاجرت دیتابیس خودکار اعمال می‌شود</b> (گسترش ستون PaperSize قالب چاپ + دقت اعشار درصد پوشش بیمه) — بذرافشان‌ها هم قالب‌های چاپ تازه (۲۰ قالب) و کدینگ حسابداری تفصیلی (۲۱ حساب) را اضافه می‌کنند</li>
</ol>
<div class="info"><b>بعد از اجرا بررسی کنید:</b> صفحهٔ «فرم‌های تکمیلی ← طراح قالب چاپ» باید برای هر نوع سند دست‌کم ۳ قالب آماده نشان دهد (استاندارد/مدرن/کلاسیک) — اگر فقط ۹ قالب می‌بینید، یعنی بذر اجرا نشده و لاگ سرور را ببینید. حدود یک دقیقه پس از استارتاپ سرور، اطلاعات درمانگاه از سرور لایسنس همگام و در «تنظیمات و مدیریت سامانه ← اطلاعات درمانگاه» ظاهر می‌شود.</div>
<h3>خلاصهٔ تغییرات مهم این نسخه</h3>
<ul>
<li>🖥️ <b>رفع کرش صفحهٔ پرسنل</b> (Unable to cast String to Style) — یک آکولاد باز جاافتاده در XAML</li>
<li>👤 <b>تب نقش‌ها و دسترسی‌ها در فرم کاربر</b> — چک‌باکس نقش‌ها بایندینگ نداشت؛ حالا نقش‌های فعلی تیک می‌خورند و انتخاب جدید ذخیره می‌شود</li>
<li>🖨️ <b>قالب‌های چاپ پیش‌فرض</b> — ریشهٔ دیده‌نشدن: سیدر هیچ‌وقت ذخیره نمی‌کرد! حالا ۲۹ قالب (هر نوع ≥۳ طرح) + پشتیبانی کامل کاغذ حرارتی/سفارشی</li>
<li>🔑 <b>اطلاعات درمانگاه از لایسنس</b> — همگام‌سازی در لحظهٔ فعال‌سازی + تپ قلب یکبارهٔ استارتاپ (قبلاً فقط ۳ بامداد)</li>
<li>💰 <b>زنجیرهٔ مالی پذیرش</b> — سه ریشه‌یابی: نام اقلام فاکتور گم می‌شد (ناهمخوانی فیلدها) + شکست بی‌صدا با پیام موفقیت + اسناد حسابداری هرگز تولید نمی‌شدند (کدینگ تفصیلی غایب بود). حالا: نام‌ها سالم، خطاها با دیالوگ فارسی، ۲۱ حساب تفصیلی، و فهرست پذیرش روز سه ستون مالی (مبلغ فاکتور/پرداخت‌شده/وضعیت مالی رنگی) دارد</li>
<li>🗄️ <b>هر ۴ هشدار EF سرور رفع شد</b> (سه فیلتر سراسری + دقت Decimal)</li>
<li>🎨 <b>لوگو و نام برنامه در منوی کناری وسط‌چین شد</b></li>
</ul>
<div class="warn"><b>نکته:</b> اسناد حسابداری خودکار از این پس برای فاکتورها/پرداخت‌های <b>جدید</b> تولید می‌شود؛ رکوردهای قبلی بدون سند مانده‌اند و در صورت نیاز باید دستی در ماژول حسابداری ثبت شوند.</div>
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
               f"بستهٔ به‌روزرسانی 2.4.3 → 2.4.4\nتعداد فایل: {len(manifest_lines)}\n\n" + "\n".join(manifest_lines))

print(f"✅ ساخته شد: {OUT} ({OUT.stat().st_size / 1024:.0f} کیلوبایت)")
print(f"— راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME}")
