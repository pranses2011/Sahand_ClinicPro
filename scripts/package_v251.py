# -*- coding: utf-8 -*-
"""چرخهٔ چهلم — ساخت بستهٔ آپدیت 2.5.0 → 2.5.1 (وصلهٔ بیلد: ۹ فایل + راهنما + مانیفست)"""
import os
import zipfile

REPO = '/home/z/my-project/Sahand_ClinicPro'
OUT = '/home/z/my-project/download/SahandClinicPro-Update-2.5.0-to-2.5.1.zip'

# فایل‌های تغییرکرده از v2.5.0 تا HEAD (فقط فایل‌های کاربر — اسکریپت انتشار داخل ریپو لازم نیست)
FILES = [
    'Directory.Build.props',
    'CHANGELOG.md',
    'src/Sahand.ClinicPro.API/Logging/PersianConsoleTextFormatter.cs',
    'src/Sahand.ClinicPro.Application/Interfaces/ILicenseService.cs',
    'src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs',
    'src/Sahand.ClinicPro.Desktop/Views/AccountingView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml.cs',
]

# ─── گاردهای انسجام: هر فایل باید存在 وجود داشته باشد و نشانهٔ چرخهٔ چهلم را داشته باشد ───
GUARDS = {
    'src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml': [
        'http://materialdesigninxaml.net/winfx/xaml/themes',   # xmlns اصلاح‌شده
        'چرخهٔ چهلم',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml.cs': [
        'using System.Windows.Media;',
        '(UIElement)this',
    ],
    'src/Sahand.ClinicPro.Desktop/Views/AccountingView.xaml': ['چرخهٔ چهلم', '<StackPanel>'],
    'src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml': ['چرخهٔ چهلم', '</StackPanel>'],
    'src/Sahand.ClinicPro.Desktop/Converters/CommonConverters.cs': ['رفع CS8510'],
    'src/Sahand.ClinicPro.API/Logging/PersianConsoleTextFormatter.cs': ['رفع CS0165'],
    'src/Sahand.ClinicPro.Application/Interfaces/ILicenseService.cs': ['return raw;'],
    'Directory.Build.props': ['<Version>2.5.1</Version>'],
    'CHANGELOG.md': ['[v2.5.1]'],
}
# گاردهای منفی — نباید وجود داشته باشند (اگر بود یعنی فایل کهنه در بسته نشسته)
# توجه: مقادیر حتماً «فهرست» باشند تا حلقه روی رشته نچرخد
NEG = {
    'src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml': [
        'http://materialdesigninxaml.net/xaml/themes"',   # URI بی‌winfx با کوتیشن انتها — کامنتِ توضیحی خطای مثبت ندهد
    ],
    'src/Sahand.ClinicPro.API/Logging/PersianConsoleTextFormatter.cs': ['if (status is { } sc)'],
    'src/Sahand.ClinicPro.Application/Interfaces/ILicenseService.cs': ['return Date;'],
    'src/Sahand.ClinicPro.Desktop/Views/GuidedTourWindow.xaml.cs': ['(IInputElement)this'],
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
print(f'✅ همهٔ {len(GUARDS)} گارد مثبت و {len(NEG)} گارد منفی پاس شدند')

# ─── مانیفست ───
manifest = ['فایل‌های تغییرکردهٔ 2.5.0 → 2.5.1 (چرخهٔ چهلم — وصلهٔ بیلد)',
            'پس از جایگزینی: Clean + Rebuild کامل سولوشن (دسکتاپ + سرور API)', '']
manifest += [f.replace('/', '\\') for f in FILES]

# ─── بسته ───
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with zipfile.ZipFile(OUT, 'w', zipfile.ZIP_DEFLATED) as z:
    for rel in FILES:
        z.write(os.path.join(REPO, rel), rel)
    z.writestr('changed-files.txt', '\n'.join(manifest) + '\n')
    z.writestr('Update-Guide-2.5.1.html', open('/home/z/my-project/scripts/guide_v251.html', encoding='utf-8').read())

size = os.path.getsize(OUT)
print(f'📦 بسته ساخته شد: {OUT} ({size:,} بایت، {len(FILES)} فایل + راهنما + مانیفست)')
