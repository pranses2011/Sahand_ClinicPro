#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""چرخهٔ چهل‌ویکم — ساخت بستهٔ آپدیت 2.5.1 → 2.6.0 (۲۶ فایل + راهنما + مانیفست)"""
import os
import zipfile

REPO = '/home/z/my-project/Sahand_ClinicPro'
OUT = '/home/z/my-project/download/SahandClinicPro-Update-2.5.1-to-2.6.0.zip'

# فایل‌های تغییرکرده از v2.5.1 تا HEAD (فقط فایل‌های کاربر — اسکریپت‌های انتشار داخل ریپو لازم نیست)
FILES = [
    'Directory.Build.props',
    'CHANGELOG.md',
    # ─── سرور API ───
    'src/Sahand.ClinicPro.API/Controllers/ConsumablesController.cs',
    'src/Sahand.ClinicPro.API/Controllers/UsersController.cs',
    'src/Sahand.ClinicPro.API/Middleware/LicenseValidationMiddleware.cs',
    # ─── Application ───
    'src/Sahand.ClinicPro.Application/Features/Pharmacy/ConsumablesFeatures.cs',
    'src/Sahand.ClinicPro.Application/Features/Pharmacy/PharmacyFeatures.cs',
    # ─── Domain / Infrastructure ───
    'src/Sahand.ClinicPro.Domain/Security/UserShortcut.cs',
    'src/Sahand.ClinicPro.Infrastructure/Persistence/ClinicProDbContext.cs',
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Security/UserShortcutConfiguration.cs',
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261011210000_Cycle41UserShortcuts.cs',
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/ClinicProDbContextModelSnapshot.cs',
    # ─── دسکتاپ: صفحهٔ جدید مصرفی ───
    'src/Sahand.ClinicPro.Desktop/Views/ConsumablesView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/ConsumablesView.xaml.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/ConsumablesViewModel.cs',
    # ─── دسکتاپ: سیم‌کشی صفحه ───
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/App.xaml.cs',
    'src/Sahand.ClinicPro.Desktop/Helpers/PageCatalog.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs',
    # ─── دسکتاپ: میانبرها در دیتابیس + پیش‌فرض ادمین ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs',
    # ─── دسکتاپ: پذیرش و فرم‌های مشترک ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml',
    'src/Sahand.ClinicPro.Desktop/ViewModels/StockInFormViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/PharmacyItemFormViewModel.cs',
    # ─── دسکتاپ: فیلتر نوع گزارش‌ها ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReportBrowserViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/ReportBrowserView.xaml',
]

# ─── گاردهای مثبت: نشانهٔ چرخهٔ چهل‌ویکم در هر فایل (بایت-قطعی) ───
GUARDS = {
    'Directory.Build.props': ['<Version>2.6.0</Version>'],
    'CHANGELOG.md': ['[v2.6.0]'],
    'src/Sahand.ClinicPro.API/Controllers/ConsumablesController.cs': [
        'Route("api/v1/consumables")',
        'MedicationItemKind.Consumable',
        'چرخهٔ چهل‌ویکم',
    ],
    'src/Sahand.ClinicPro.API/Controllers/UsersController.cs': [
        '[HttpGet("me/shortcuts")]',
        '[HttpPut("me/shortcuts")]',
        'IgnoreQueryFilters()',
    ],
    'src/Sahand.ClinicPro.API/Middleware/LicenseValidationMiddleware.cs': ['عمداً در این نقشه'],
    'src/Sahand.ClinicPro.Application/Features/Pharmacy/ConsumablesFeatures.cs': ['صفحهٔ «لوازم مصرفی»'],
    'src/Sahand.ClinicPro.Application/Features/Pharmacy/PharmacyFeatures.cs': [
        'i.SellPrice,',
        'i.PurchasePrice))',
    ],
    'src/Sahand.ClinicPro.Domain/Security/UserShortcut.cs': ['class UserShortcut : AuditableEntity'],
    'src/Sahand.ClinicPro.Infrastructure/Persistence/ClinicProDbContext.cs': ['DbSet<UserShortcut>'],
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Security/UserShortcutConfiguration.cs': [
        'IX_UserShortcuts_UserId',
        'HasFilter("[IsDeleted] = 0")',
    ],
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261011210000_Cycle41UserShortcuts.cs': [
        'Cycle41UserShortcuts',
        'CREATE TABLE [dbo].[UserShortcuts]',
    ],
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/ClinicProDbContextModelSnapshot.cs': [
        'Domain.Entities.Security.UserShortcut"',
        'HasDatabaseName("IX_UserShortcuts_UserId")',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/ConsumablesView.xaml': [
        'لوازم مصرفی درمانگاه',
        'VisibleBatches',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/ConsumablesView.xaml.cs': ['class ConsumablesView'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ConsumablesViewModel.cs': [
        '"consumables/inventory"',
        'CanManage',
    ],
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml': ['vm:ConsumablesViewModel'],
    'src/Sahand.ClinicPro.Desktop/App.xaml.cs': ['AddTransient<ConsumablesViewModel>'],
    'src/Sahand.ClinicPro.Desktop/Helpers/PageCatalog.cs': [
        '"consumables", "لوازم مصرفی درمانگاه", "Syringe", "receptions.view"',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs': [
        '["consumables"] = () => consumablesVm',
        'PageKey = "consumables"',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs': [
        'users/me/shortcuts',
        'DefaultAdminShortcutKeys',
        '_shortcutsSaveWonRace',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs': [
        '"consumables"',
        '"consumables/consume"',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml': ['صفحهٔ «لوازم مصرفی»'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/StockInFormViewModel.cs': [
        'consumables/inventory',
        'defaultKindIndex',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/PharmacyItemFormViewModel.cs': ['consumables/items'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReportBrowserViewModel.cs': [
        'ReportTypeFilters',
        'ApplyCatalogFilters',
        'IsChartReport',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/ReportBrowserView.xaml': [
        'ReportTypeFilters',
        'نوع گزارش',
    ],
}

# گاردهای منفی — نباید وجود داشته باشند (اگر بود یعنی فایل کهنه در بسته نشسته)
# درس چرخهٔ ۳۹/۴۰: گارد منفی با «کد دقیق» نه متن کامنت
NEG = {
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs': [
        '"pharmacy/consumables"',
        '"pharmacy/consume"',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/StockInFormViewModel.cs': ['"pharmacy/inventory"'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/PharmacyItemFormViewModel.cs': ['"lookups/medications"'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ConsumablesViewModel.cs': ['"pharmacy/'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs': ['private void LoadShortcuts()'],
}

errors = []
for rel in FILES:
    path = os.path.join(REPO, rel)
    if not os.path.isfile(path):
        errors.append(f'نبود فایل: {rel}')
        continue
    with open(path, encoding='utf-8-sig') as f:
        content = f.read()
    for g in GUARDS.get(rel, []):
        if g not in content:
            errors.append(f'گارد مثبت شکست در {rel}: {g!r}')
    for n in NEG.get(rel, []):
        if n in content:
            errors.append(f'گارد منفی شکست در {rel}: {n!r} هنوز هست!')

if errors:
    print('❌ گاردها شکست خوردند:')
    for e in errors:
        print('  -', e)
    raise SystemExit(1)
print(f'✅ همهٔ {sum(len(v) for v in GUARDS.values())} گارد مثبت و {sum(len(v) for v in NEG.values())} گارد منفی پاس شدند')

# ─── مانیفست ───
manifest = [
    'فایل‌های تغییرکردهٔ 2.5.1 → 2.6.0 (چرخهٔ چهل‌ویکم)',
    'پس از جایگزینی: Clean + Rebuild کامل سولوشن + publish/ری‌استارت سرور API',
    'یک مهاجرت دیتابیس (جدول UserShortcuts) در نخستین اجرای سرور خودکار اعمال می‌شود',
    '',
]
manifest += [f.replace('/', '\\') for f in FILES]

# ─── بسته ───
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with zipfile.ZipFile(OUT, 'w', zipfile.ZIP_DEFLATED) as z:
    for rel in FILES:
        z.write(os.path.join(REPO, rel), rel)
    z.writestr('changed-files.txt', '\n'.join(manifest) + '\n')
    z.writestr('Update-Guide-2.6.0.html', open('/home/z/my-project/scripts/guide_v260.html', encoding='utf-8').read())

size = os.path.getsize(OUT)
print(f'📦 بسته ساخته شد: {OUT} ({size:,} بایت، {len(FILES)} فایل + راهنما + مانیفست)')
