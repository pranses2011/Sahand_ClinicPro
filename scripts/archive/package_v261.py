#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""چرخهٔ چهل‌ودوم — ساخت بستهٔ آپدیت 2.6.0 → 2.6.1 (۱۵ فایل + راهنما + مانیفست)

چهار رفع مالک: ① خطای خروج از حساب (Show روی پنجرهٔ بسته) ② حذف Breadcrumb
از هدر ③ بذر سمت/دپارتمان + رفع پاک‌شدن در ویرایش پرسنل ④ بازطراحی انتخابگر
میانبرهای داشبورد. گاردهای انسجام بایت-قطعی (درس چرخهٔ ۳۹/۴۰/۴۱: گارد منفی
با «کد دقیق حذف‌شده» نه متن کامنت)."""
import os
import zipfile

REPO = '/home/z/my-project/Sahand_ClinicPro'
OUT = '/home/z/my-project/download/SahandClinicPro-Update-2.6.0-to-2.6.1.zip'

# فایل‌های تغییرکرده از v2.6.0 تا HEAD (فقط فایل‌های کاربر — اسکریپت‌های انتشار داخل ریپو لازم نیست)
FILES = [
    'Directory.Build.props',
    'CHANGELOG.md',
    # ─── Application (سرور): نام‌های سمت/دپارتمان در جزئیات پرسنل ───
    'src/Sahand.ClinicPro.Application/Features/HR/HRFeatures.cs',
    # ─── Infrastructure: مهاجرت بذر سمت/دپارتمان ───
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261012060000_Cycle42SeedPositionsDepartments.cs',
    # ─── دسکتاپ: رفع خطای خروج از حساب (نمونهٔ زنده یا نمونهٔ نو) ───
    'src/Sahand.ClinicPro.Desktop/App.xaml.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/LoginWindow.xaml.cs',
    'src/Sahand.ClinicPro.Desktop/Views/ChangePasswordWindow.xaml.cs',
    # ─── دسکتاپ: هدر بدون Breadcrumb ───
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/ViewModels/PersonnelViewModel.cs',
    # ─── دسکتاپ: رفع ویرایش سمت/دپارتمان پرسنل ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/EmployeeFormViewModel.cs',
    # ─── دسکتاپ: بازطراحی انتخابگر میانبرها ───
    'src/Sahand.ClinicPro.Desktop/Helpers/PageCatalog.cs',
    'src/Sahand.ClinicPro.Desktop/Views/ShortcutPickerWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/ShortcutPickerWindow.xaml.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs',
]

# ─── گاردهای مثبت: نشانهٔ چرخهٔ چهل‌ودوم در هر فایل (بایت-قطعی) ───
GUARDS = {
    'Directory.Build.props': ['<Version>2.6.1</Version>'],
    'CHANGELOG.md': ['[v2.6.1]'],
    'src/Sahand.ClinicPro.Application/Features/HR/HRFeatures.cs': [
        'string? PositionName, string? DepartmentName',
        'e.Position != null ? e.Position.Title : null',
        'e.Department != null ? e.Department.Name : null',
        'چرخهٔ چهل‌ودوم',
    ],
    'src/Sahand.ClinicPro.Infrastructure/Persistence/Migrations/20261012060000_Cycle42SeedPositionsDepartments.cs': [
        'Cycle42SeedPositionsDepartments',
        'NOT EXISTS (SELECT 1 FROM Departments d WHERE d.Name = s.Name)',
        'NOT EXISTS (SELECT 1 FROM Positions p WHERE p.Title = s.Title)',
        "N'منشی پذیرش'",
        "N'تکنسین رادیولوژی'",
        "N'تصویربرداری و رادیولوژی'",
        "NOT EXISTS (SELECT 1 FROM Employees e WHERE e.DepartmentId = d.Id)",
        "NOT EXISTS (SELECT 1 FROM Employees e WHERE e.PositionId = p.Id)",
    ],
    'src/Sahand.ClinicPro.Desktop/App.xaml.cs': [
        'services.AddTransient<LoginWindow>();',
        'services.AddTransient<MainWindow>();',
        'public static Views.LoginWindow ShowLoginWindow()',
        'public static MainWindow ShowMainWindow()',
        'Current.Windows.OfType<Views.LoginWindow>().FirstOrDefault()',
        'Current.Windows.OfType<MainWindow>().FirstOrDefault()',
        'چرخهٔ چهل‌ودوم',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs': [
        'App.ShowLoginWindow();',
        'پراپرتی Breadcrumb حذف شد',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/LoginWindow.xaml.cs': [
        'App.ShowMainWindow();',
        'چرخهٔ چهل‌ودوم',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/ChangePasswordWindow.xaml.cs': [
        'App.ShowMainWindow();',
        'چرخهٔ چهل‌ودوم',
    ],
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml': [
        'Grid.Column="0" Grid.ColumnSpan="3"',
        'MaxWidth="420" TextTrimming="CharacterEllipsis"',
        'IsHitTestVisible="False"',
        'Text="{Binding CurrentTitle}"',
        'چرخهٔ چهل‌ودوم',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/PersonnelViewModel.cs': [
        'Breadcrumb حذف شد',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/EmployeeFormViewModel.cs': [
        '_originalPositionId',
        '_originalDepartmentId',
        'originalId is not null',
        'PositionText = d.PositionName;',
        'DepartmentText = d.DepartmentName;',
        '(long? Id, string? Error)',
        'چرخهٔ چهل‌ودوم',
    ],
    'src/Sahand.ClinicPro.Desktop/Helpers/PageCatalog.cs': [
        'GroupColors',
        'Group: "جریان درمان"',
        'Group: "مدیریت سامانه"',
        'ColorOfGroup',
        'چرخهٔ چهل‌ودوم',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/ShortcutPickerWindow.xaml': [
        'SelectionMode="Multiple"',
        'GroupNameToColor',
        'ShortcutCardStyle',
        'چرخهٔ چهل‌ودوم',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/ShortcutPickerWindow.xaml.cs': [
        'GroupNameToColorConverter',
        'ListCollectionView',
        'SelectedKeys',
        'چرخهٔ ۴۲',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs': [
        'GroupColor',
        'SelectedKeys is not { Count: > 0 }',
        'ColorOfGroup',
    ],
}

# گاردهای منفی — نباید وجود داشته باشند (اگر بود یعنی فایل کهنه در بسته نشسته)
# درس چرخهٔ ۳۹/۴۰/۴۱: گارد منفی با «کد دقیقِ حذف‌شده» نه متن کامنت
NEG = {
    'src/Sahand.ClinicPro.Desktop/App.xaml.cs': [
        'services.AddSingleton<LoginWindow>()',
        'services.AddSingleton<MainWindow>()',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs': [
        'GetRequiredService(typeof(LoginWindow))',
        'Breadcrumb = $"',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/LoginWindow.xaml.cs': [
        'GetRequiredService(typeof(MainWindow))',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/ChangePasswordWindow.xaml.cs': [
        'GetRequiredService(typeof(MainWindow))',
    ],
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml': [
        'Text="{Binding Breadcrumb}"',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/EmployeeFormViewModel.cs': [
        # فراخوانی قدیمی تک‌آرگومانی (کد جدید ادامهٔ آرگومان‌ها را با کاما می‌آورد)
        'PositionText, "lookups/positions");',
        'DepartmentText, "lookups/departments");',
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs': [
        'picker.SelectedKey is',
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

if errors:
    print('❌ گاردها شکست خوردند:')
    for e in errors:
        print('  -', e)
    raise SystemExit(1)
print(f'✅ همهٔ {sum(len(v) for v in GUARDS.values())} گارد مثبت و {sum(len(v) for v in NEG.values())} گارد منفی پاس شدند')

# ─── مانیفست ───
manifest = [
    'فایل‌های تغییرکردهٔ 2.6.0 → 2.6.1 (چرخهٔ چهل‌ودوم)',
    'پس از جایگزینی: Clean + Rebuild کامل سولوشن + publish/ری‌استارت سرور API',
    'یک مهاجرت دیتابیس (بذر سمت/دپارتمان) در نخستین اجرای سرور خودکار اعمال می‌شود — فقط درج ردیف تازه؛ بدون تغییر دادهٔ موجود',
    '',
]
manifest += [f.replace('/', '\\') for f in FILES]

# ─── بسته ───
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with zipfile.ZipFile(OUT, 'w', zipfile.ZIP_DEFLATED) as z:
    for rel in FILES:
        z.write(os.path.join(REPO, rel), rel)
    z.writestr('changed-files.txt', '\n'.join(manifest) + '\n')
    z.writestr('Update-Guide-2.6.1.html', open('/home/z/my-project/scripts/guide_v261.html', encoding='utf-8').read())

size = os.path.getsize(OUT)
print(f'📦 بسته ساخته شد: {OUT} ({size:,} بایت، {len(FILES)} فایل + راهنما + مانیفست)')
