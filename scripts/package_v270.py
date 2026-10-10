#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""چرخهٔ چهل‌وسوم — ساخت بستهٔ آپدیت 2.6.1 → 2.7.0 (۶۵ فایل + راهنما + مانیفست)

چهارده گزارش مالک در یک چرخه: هشدار EF Sentinel، تکراری کاذب اقلام، ورود
کالا + فهرست‌های قلم‌محور، اتاق‌ها، میانبرهای مردهٔ داشبورد، جستجوی تایپی
کمبوباکس‌ها، عنوان شناور فیلدها، کاشی/توست، بستن شیفت کامل، مستقل‌سازی
مصرفی/داروخانه، انبارگردانی دامنه‌دار و چهار صفحهٔ مستقل پاراکلینیک.
گاردهای انسجام بایت-قطعی (درس چرخهٔ ۳۹/۴۰/۴۱/۴۲: گارد منفی با کد دقیق)."""
import os
import zipfile

REPO = '/home/z/my-project/Sahand_ClinicPro'
OUT = '/home/z/my-project/download/SahandClinicPro-Update-2.6.1-to-2.7.0.zip'

# فایل‌های تغییرکرده از v2.6.1 تا HEAD — به‌علاوهٔ سه حذف‌شده که در مانیفست اعلان می‌شوند
FILES = [
    'Directory.Build.props',
    'CHANGELOG.md',
    # ─── سرور — رفع‌های این چرخه ───
    'src/Sahand.ClinicPro.API/Controllers/ConsumablesController.cs',
    'src/Sahand.ClinicPro.API/Controllers/PharmacyController.cs',
    'src/Sahand.ClinicPro.API/Controllers/StocktakingsController.cs',
    'src/Sahand.ClinicPro.API/Middleware/ExceptionHandlingMiddleware.cs',
    'src/Sahand.ClinicPro.Application/Features/Lookups/MedicationCommands.cs',
    'src/Sahand.ClinicPro.Application/Features/Pharmacy/PharmacyFeatures.cs',
    'src/Sahand.ClinicPro.Application/Features/Pharmacy/StocktakingFeatures.cs',
    'src/Sahand.ClinicPro.Domain/Pharmacy/StockTaking.cs',
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Clinical/MedicationConfiguration.cs',
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Pharmacy/StockTakingConfiguration.cs',
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261010230054_Cycle43StocktakingScope.cs',
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261010230054_Cycle43StocktakingScope.Designer.cs',
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/ClinicProDbContextModelSnapshot.cs',
    # ─── تست گارد چرخه ───
    'tests/Sahand.ClinicPro.Infrastructure.IntegrationTests/Cycle43StockInDiagnostics.cs',
    # ─── دسکتاپ — هسته ───
    'src/Sahand.ClinicPro.Desktop/App.xaml',
    'src/Sahand.ClinicPro.Desktop/App.xaml.cs',
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Controls/ComboBoxSearch.cs',
    'src/Sahand.ClinicPro.Desktop/Helpers/PageCatalog.cs',
    'src/Sahand.ClinicPro.Desktop/Helpers/PatientQuickSearch.cs',
    # ─── دسکتاپ — پاراکلینیک مستقل ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/DentistryViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/OphthalmologyViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/PhysiotherapyViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/ImagingViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/DentistryView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/DentistryView.xaml.cs',
    'src/Sahand.ClinicPro.Desktop/Views/OphthalmologyView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/OphthalmologyView.xaml.cs',
    'src/Sahand.ClinicPro.Desktop/Views/PhysiotherapyView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/PhysiotherapyView.xaml.cs',
    'src/Sahand.ClinicPro.Desktop/Views/ImagingView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/ImagingView.xaml.cs',
    # ─── دسکتاپ — مصرفی/داروخانه/انبارگردانی ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/ConsumablesViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/ConsumablesView.xaml',
    'src/Sahand.ClinicPro.Desktop/ViewModels/PharmacyViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/PharmacyView.xaml',
    'src/Sahand.ClinicPro.Desktop/ViewModels/PharmacyItemFormViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/PharmacyItemFormWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/ViewModels/StockInFormViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/StockInFormWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/ViewModels/StocktakingViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/StocktakingView.xaml',
    # ─── دسکتاپ — بستن شیفت ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/ShiftCloseViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/ShiftCloseWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/Services/ShiftReceiptPrinter.cs',
    'src/Sahand.ClinicPro.Desktop/Views/ShiftReceiptPreviewWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/ShiftReceiptPreviewWindow.xaml.cs',
    # ─── دسکتاپ — داشبورد و پذیرش ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml',
    # ─── دسکتاپ — عنوان فیلدها (پچ Hint) ───
    'src/Sahand.ClinicPro.Desktop/Views/AccountingView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/AdvancedEmrView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/AppointmentsView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/AuditLogView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/BackupView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/ComplementaryView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/EmployeeFormWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/InvoiceFormWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/PatientDetailWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/ReportBrowserView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/ReportsView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/SettingsView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/SmsView.xaml',
]

# فایل‌های حذف‌شده — در بسته نیستند؛ مانیفست و راهنما حذفشان را الزامی کرده‌اند
DELETED = [
    'src/Sahand.ClinicPro.Desktop/ViewModels/ParaclinicalViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/ParaclinicalView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/ParaclinicalView.xaml.cs',
]

# ─── گاردهای مثبت: نشانه‌های چرخهٔ چهل‌وسوم (بایت-قطعی) ───
GUARDS = {
    'Directory.Build.props': ['<Version>2.7.0</Version>'],
    'CHANGELOG.md': ['[v2.7.0]'],
    'src/Sahand.ClinicPro.API/Controllers/ConsumablesController.cs': [
        'ExpectedItemKind = MedicationItemKind.Consumable',
    ],
    'src/Sahand.ClinicPro.API/Controllers/PharmacyController.cs': [
        'ExpectedItemKind = Sahand.ClinicPro.Domain.Enums.MedicationItemKind.Drug',
    ],
    'src/Sahand.ClinicPro.API/Controllers/StocktakingsController.cs': [
        'StartStocktakingCommand(request.Notes, request.ItemKind)',
    ],
    'src/Sahand.ClinicPro.API/Middleware/ExceptionHandlingMiddleware.cs': [
        'exception.GetType().Name}: {exception.Message}',
    ],
    'src/Sahand.ClinicPro.Application/Features/Lookups/MedicationCommands.cs': [
        'internal static string NormalizeFa',
        'MinimumLength(2)',
        'rawNameFa',
    ],
    'src/Sahand.ClinicPro.Application/Features/Pharmacy/PharmacyFeatures.cs': [
        'record InventoryItemDto(',
        'ExpectedItemKind',
        'Inventory = inventory',
        'Error.NotFound("قلم انبار"',
    ],
    'src/Sahand.ClinicPro.Application/Features/Pharmacy/StocktakingFeatures.cs': [
        'ScopeFa(MedicationItemKind? kind)',
        's.ItemKind == request.ItemKind',
        'request.ItemKind == null || inv.Medication != null',
    ],
    'src/Sahand.ClinicPro.Domain/Pharmacy/StockTaking.cs': [
        'public MedicationItemKind? ItemKind { get; set; }',
    ],
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Clinical/MedicationConfiguration.cs': [
        '.HasSentinel(MedicationItemKind.Drug)',
    ],
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Configurations/Pharmacy/StockTakingConfiguration.cs': [
        'builder.Property(x => x.ItemKind)',
    ],
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261010230054_Cycle43StocktakingScope.cs': [
        "UPDATE [UserShortcuts] SET [KeysJson] = '[]' WHERE [KeysJson] IS NULL",
        'AddColumn<byte>(',
    ],
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/ClinicProDbContextModelSnapshot.cs': [
        # Sentinel در اسنپ‌شات ثبت نمی‌شود (رفتار EF 8 — مدل زنده ملاک است و
        # تست ItemKind_Has_Sentinel آن را می‌پوشاند)؛ گارد اسنپ‌شات روی ستون دامنه
        'HasComment("دامنهٔ جلسه انبارگردانی',
    ],
    'tests/Sahand.ClinicPro.Infrastructure.IntegrationTests/Cycle43StockInDiagnostics.cs': [
        'ItemKind_Has_Sentinel_In_Runtime_Model',
        'savedTx.InventoryId.Should().Be(savedBatch.Id',
    ],
    'src/Sahand.ClinicPro.Desktop/App.xaml': [
        'MaterialDesignOutlinedTextBox',
        'MaterialDesignOutlinedComboBox',
        'MaterialDesignOutlinedPasswordBox',
        'controls:ComboBoxSearch.IsEnabled" Value="True"',
    ],
    'src/Sahand.ClinicPro.Desktop/App.xaml.cs': [
        'services.AddTransient<DentistryViewModel>()',
        'services.AddTransient<ImagingViewModel>()',
    ],
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml': [
        'Margin="0,0,0,64"',
        'x:Name="AccentStrip"',
        'views:DentistryView',
        'views:ImagingView',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs': [
        '_navigation.NavigationRequested += (_, e) => NavigateToPage(e.PageKey)',
        'DentistryViewModel dentistryVm',
        '["paraclinical"] = () => imagingVm',
    ],
    'src/Sahand.ClinicPro.Desktop/Controls/ComboBoxSearch.cs': [
        'public static class ComboBoxSearch',
        "'\\u200c' => ' '",
        'StaysOpenOnEdit = true',
    ],
    'src/Sahand.ClinicPro.Desktop/Helpers/PageCatalog.cs': [
        '("dentistry", "دندانپزشکی"',
        '("ophthalmology", "چشم‌پزشکی"',
        '("physiotherapy", "فیزیوتراپی"',
        '("imaging", "تصویربرداری"',
    ],
    'src/Sahand.ClinicPro.Desktop/Helpers/PatientQuickSearch.cs': [
        'public static class PatientQuickSearch',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/DentistryViewModel.cs': [
        'public partial class DentistryViewModel',
        'PatientQuickSearch.FindAsync',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/OphthalmologyViewModel.cs': [
        'public partial class OphthalmologyViewModel',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/PhysiotherapyViewModel.cs': [
        'public partial class PhysiotherapyViewModel',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ImagingViewModel.cs': [
        'public partial class ImagingViewModel',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/DentistryView.xaml': ['x:Class="Sahand.ClinicPro.Desktop.Views.DentistryView"'],
    'src/Sahand.ClinicPro.Desktop/Views/OphthalmologyView.xaml': ['x:Class="Sahand.ClinicPro.Desktop.Views.OphthalmologyView"'],
    'src/Sahand.ClinicPro.Desktop/Views/PhysiotherapyView.xaml': ['x:Class="Sahand.ClinicPro.Desktop.Views.PhysiotherapyView"'],
    'src/Sahand.ClinicPro.Desktop/Views/ImagingView.xaml': ['x:Class="Sahand.ClinicPro.Desktop.Views.ImagingView"'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ConsumablesViewModel.cs': [
        'dto.ItemId',
        'dto.BatchCount',
        'isConsumableContext: true',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/PharmacyViewModel.cs': [
        '"itemKind=1"',
        'i.ItemId',
        'isConsumableContext: false',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/PharmacyItemFormViewModel.cs': [
        '_genericNameIsAuto',
        'isConsumableContext = true',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/StockInFormViewModel.cs': [
        'MedicationsLookupResponse',
        'isConsumableContext = true',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/StocktakingViewModel.cs': [
        'NewSessionScopes',
        'itemKind = NewSessionScopeKind',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ShiftCloseViewModel.cs': [
        'Money.Format(SystemCash)',
        'new Services.ShiftReceipt(',
        'ShiftReceiptPreviewWindow(receipt)',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/ShiftCloseWindow.xaml': [
        'Width="1220" Height="820"',
        'behaviors:MoneyInput.NumericValue="{Binding CountedCash',
    ],
    'src/Sahand.ClinicPro.Desktop/Services/ShiftReceiptPrinter.cs': [
        'public static class ShiftReceiptPrinter',
        'Printer.Receipt',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs': [
        'public string? DisplayName { get; set; }',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml': [
        'TileShadow',
        'WrapPanel HorizontalAlignment="Center"',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/AccountingView.xaml': ['HintAssist.Hint="حساب دفتر کل"'],
    'src/Sahand.ClinicPro.Desktop/Views/InvoiceFormWindow.xaml': ['HintAssist.Hint="مبلغ دریافتی"'],
    'src/Sahand.ClinicPro.Desktop/Views/ReportsView.xaml': ['HintAssist.Hint="بازهٔ گزارش"'],
}

# ─── گاردهای منفی — کد دقیقِ حذف‌شده (درس چرخهٔ ۳۹+) ───
NEG = {
    'src/Sahand.ClinicPro.Desktop/App.xaml.cs': [
        'services.AddTransient<ParaclinicalViewModel>()',
    ],
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml': [
        'views:ParaclinicalView',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs': [
        '= () => paraclinicalVm',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/PharmacyViewModel.cs': [
        'InventoryKindFilters',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/PharmacyView.xaml': [
        'InventoryKindFilters',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/PharmacyItemFormViewModel.cs': [
        'KindOptions',
        'if (string.IsNullOrWhiteSpace(GenericName))\n            GenericName = value;',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/StockInFormViewModel.cs': [
        'SearchKindFilters',
        'defaultKindIndex',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/StockInFormWindow.xaml': [
        'SearchKindFilters',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ConsumablesViewModel.cs': [
        'public string? BatchNumber { get; set; }',
        'dto.BatchNumber',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs': [
        'room.RoomName',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ShiftCloseViewModel.cs': [
        '{SystemCash:N0}',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml': [
        'Text="میانبر جدید" Margin="6,0,0,0"',
    ],
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

# گارد حذف: فایل‌های قدیمی نباید در ریپو باشند
for rel in DELETED:
    if os.path.exists(os.path.join(REPO, rel)):
        errors.append(f'فایل حذف‌شده هنوز موجود است: {rel}')

if errors:
    print('❌ گاردها شکست خوردند:')
    for e in errors:
        print('  -', e)
    raise SystemExit(1)
print(f'✅ همهٔ {sum(len(v) for v in GUARDS.values())} گارد مثبت و {sum(len(v) for v in NEG.values())} گارد منفی و {len(DELETED)} گارد حذف پاس شدند')

# ─── مانیفست ───
manifest = [
    'فایل‌های تغییرکردهٔ 2.6.1 → 2.7.0 (چرخهٔ چهل‌وسوم — چهارده گزارش مالک)',
    '⚠️ پیش از باز کردن زیپ، این سه فایلِ حذف‌شده را از پوشهٔ کد پاک کنید (وگرنه بیلد خطا می‌دهد):',
    '    src\\Sahand.ClinicPro.Desktop\\ViewModels\\ParaclinicalViewModel.cs',
    '    src\\Sahand.ClinicPro.Desktop\\Views\\ParaclinicalView.xaml',
    '    src\\Sahand.ClinicPro.Desktop\\Views\\ParaclinicalView.xaml.cs',
    'پس از جایگزینی: Clean + Rebuild کامل سولوشن + publish/ری‌استارت سرور API',
    'یک مهاجرت دیتابیس (دامنهٔ انبارگردانی + نال‌پاکسازی میانبرها) در نخستین اجرای سرور خودکار اعمال می‌شود',
    '',
    '── فایل‌های این بسته ──',
]
manifest += [f.replace('/', '\\') for f in FILES]

# ─── بسته ───
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with zipfile.ZipFile(OUT, 'w', zipfile.ZIP_DEFLATED) as z:
    for rel in FILES:
        z.write(os.path.join(REPO, rel), rel)
    z.writestr('changed-files.txt', '\n'.join(manifest) + '\n')
    z.writestr('Update-Guide-2.7.0.html', open('/home/z/my-project/scripts/guide_v270.html', encoding='utf-8').read())

size = os.path.getsize(OUT)
print(f'📦 بسته ساخته شد: {OUT} ({size:,} بایت، {len(FILES)} فایل + راهنما + مانیفست)')
