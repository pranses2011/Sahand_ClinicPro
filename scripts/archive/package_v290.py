#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""چرخهٔ ۴۵ — بستهٔ آپدیت 2.8.0 → 2.9.0 (فقط دسکتاپ + نسخه/CHANGELOG؛ سرور دست‌نخورده)."""
import os
import zipfile

REPO = '/home/z/my-project/Sahand_ClinicPro'
OUT = '/home/z/my-project/download/SahandClinicPro-Update-2.8.0-to-2.9.0.zip'

FILES = [
    'Directory.Build.props',
    'CHANGELOG.md',
    # ─── دسکتاپ: داشبورد + توست ───
    'src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml',
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml',
    # ─── دسکتاپ: لیست مصرفی پذیرش ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs',
    # ─── دسکتاپ: ورود کالای دسته‌جمعی ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/StockInFormViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/StockInFormWindow.xaml',
    # ─── دسکتاپ: سربرگ‌های گرادیانی ───
    'src/Sahand.ClinicPro.Desktop/Views/PatientsView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/ConsumablesView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/VisitsView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/DoctorVisitView.xaml',
]

GUARDS = {
    'Directory.Build.props': ['<Version>2.9.0</Version>'],
    'CHANGELOG.md': ['## [v2.9.0]'],
    'src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml': ['MinHeight="96"', 'بازطراحی مقاوم چرخهٔ ۴۵', 'CornerRadius="18"'],
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml': ['<TranslateTransform x:Name="ToastShift" Y="0" />', 'FillBehavior="HoldEnd"'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs': ['RefreshConsumablesAsync', '_refreshingConsumables'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/StockInFormViewModel.cs': ['StockInBatchLine', 'AddLine', 'ثبت همه'],
    'src/Sahand.ClinicPro.Desktop/Views/StockInFormWindow.xaml': ['افزودن به فهرست', 'BatchLines'],
    'src/Sahand.ClinicPro.Desktop/Views/PatientsView.xaml': ['#5E35B1', 'چرخهٔ ۴۵'],
    'src/Sahand.ClinicPro.Desktop/Views/ConsumablesView.xaml': ['#00838F', 'چرخهٔ ۴۵'],
    'src/Sahand.ClinicPro.Desktop/Views/VisitsView.xaml': ['#00695C', 'چرخهٔ ۴۵'],
    'src/Sahand.ClinicPro.Desktop/Views/DoctorVisitView.xaml': ['#1565C0', 'چرخهٔ ۴۵'],
}

# گاردهای منفی — الگوهای کهنه باید حذف شده باشند
NEGATIVE_FILE = {
    'src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml': [
        'TileShadow',  # افکت و استوری‌بورد سایهٔ کاشی حذف شده
        '<DropShadowEffect x:Name="TileShadow"',
    ],
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml': [
        '<TranslateTransform x:Name="ToastShift" Y="14"',  # پایهٔ ناصفر ممنوع
    ],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs': [
        'if (ConsumableOptions.Count == 0)',  # کش یک‌بارهٔ قدیمی حذف شد
    ],
}

GUIDE = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<title>راهنمای آپدیت سهند کلینیک‌پرو ۲.۹.۰</title>
<style>
body{font-family:Vazirmatn,Tahoma,'Segoe UI',sans-serif;direction:rtl;background:#f4f7fb;margin:0;padding:24px;color:#1f2937}
.wrap{max-width:860px;margin:0 auto}
h1{color:#0d47a1;font-size:24px;border-bottom:3px solid #1e88e5;padding-bottom:10px}
h2{color:#1565c0;font-size:17px;margin-top:26px}
.card{background:#fff;border:1px solid #dce7f0;border-radius:12px;padding:16px 20px;margin:14px 0;box-shadow:0 1px 3px rgba(30,58,95,.06)}
ol,ul{line-height:2.1}
.tag{display:inline-block;background:#e3f2fd;color:#1565c0;border:1px solid #bbdefb;border-radius:8px;padding:2px 10px;font-size:12px;margin:2px}
.ok{color:#2e7d32;font-weight:700}
.note{background:#E8F5E9;border:1px solid #A5D6A7;border-radius:10px;padding:12px 16px}
code{background:#eef3f8;border-radius:5px;padding:1px 6px;font-size:13px}
</style>
</head>
<body><div class="wrap">
<h1>📦 بستهٔ آپدیت سهند کلینیک‌پرو — 2.8.0 ← 2.9.0</h1>

<div class="note"><b>✅ این آپدیت فقط پروژهٔ دسکتاپ را تغییر می‌دهد — سرور API و دیتابیس دست‌نخورده‌اند؛ بدون publish و ری‌استارت سرور.</b></div>

<div class="card">
<b>این نسخه چه می‌آورد؟</b> (چرخهٔ چهل‌وپنجم — شش گزارش شما)
<ol>
<li><b>کاشی‌های میانبر داشبورد سالم شدند</b> — آیکون کامل + عنوان همیشه دیده می‌شوند (ریشه: سازوکار برش DropShadowEffect؛ رفع با حذف افکت + حداقل ارتفاع تضمینی)</li>
<li><b>پیام توست کامل نمایش داده می‌شود</b> — لبهٔ پایین و گوشه‌ها دیگر بریده نمی‌شوند</li>
<li><b>دکمه‌های «میانبر جدید» و «ویرایش» داشبورد کاملاً گرد و فقط-آیکون شدند</b></li>
<li><b>لیست اقلام مصرفی در پذیرش</b> — با هر بار ورود به صفحهٔ پذیرش تازه می‌شود (قبلاً فقط یک‌بار در عمر برنامه بار می‌شد!)</li>
<li><b>ورود کالای دسته‌جمعی</b> — در اقلام مصرفی و داروخانه: همهٔ کالاها را به فهرست اضافه کنید و در آخر «ثبت همه» را بزنید</li>
<li><b>ظاهر حرفه‌ای‌تر</b> — سربرگ‌های گرادیانی جدید برای بیماران، لوازم مصرفی، ویزیت و معاینه و میزکار پزشک</li>
</ol>
</div>

<h2>🧩 محتویات بسته</h2>
<div class="card">
<span class="tag">۱۱ فایل</span> <span class="tag">همه دسکتاپ + نسخه</span> <span class="tag">changed-files.txt</span> <span class="tag">راهنما</span>
<p style="font-size:13px">هیچ فایل سروری در بسته نیست.</p>
</div>

<h2>⚙️ گام‌های نصب</h2>
<div class="card"><ol>
<li>فایل‌ها را روی ریشهٔ پروژه (<code>E:\\Sahand_ClinicPro</code>) کپی و <b>جایگزین</b> کنید.</li>
<li>ویژوال استودیو: پروژهٔ <b>Sahand.ClinicPro.Desktop</b> را <b>Rebuild</b> کنید (Clean ضروری است).</li>
<li>خروجی جدید دسکتاپ را به ایستگاه‌ها کپی کنید و برنامه را اجرا کنید.</li>
</ol></div>

<h2>✅ سنجه‌های موفقیت</h2>
<div class="card"><ul>
<li>نسخهٔ <b class="ok">2.9.0</b> زیر پنجرهٔ اصلی نمایش داده می‌شود</li>
<li>داشبورد: کاشی‌های میانبر با <b>آیکون کامل و عنوان</b> دیده می‌شوند (نه نیم‌دایره)؛ دکمه‌های + و مداد کاملاً <b>گرد</b>‌اند</li>
<li>هر پیام توستی کامل با گوشه‌های گرد پایین دیده می‌شود</li>
<li>پذیرش ← حالت «خدمت درمانی»: کمبوی اقلام مصرفی قلم‌ها را نشان می‌دهد؛ از صفحهٔ «لوازم مصرفی» یک ورود کالای جدید ثبت کنید و به پذیرش برگردید — بدون ری‌استارت در فهرست است</li>
<li>لوازم مصرفی یا داروخانه ← «ورود کالا»: چند قلم را با «افزودن به فهرست» جمع کنید و «ثبت همه» را بزنید — گزارش تک‌تک ردیف‌ها را می‌بینید</li>
<li>صفحات بیماران/مصرفی‌ها/ویزیت/میزکار پزشک سربرگ گرادیانی جدید دارند</li>
</ul></div>

<div class="note">💡 اگر باز هم موردی از کاشی‌ها یا توست بریده دیده شدید، اسکرین‌شات بفرستید — ریشه‌یابی این چرخه با تحلیل پیکسلی تصویر انجام شده و دقیق قابل پیگیری است.</div>
</div></body></html>
"""

# ─── گاردها ───
errors = []
for rel, markers in GUARDS.items():
    path = os.path.join(REPO, rel)
    if not os.path.exists(path):
        errors.append(f'نبود فایل: {rel}')
        continue
    text = open(path, encoding='utf-8').read()
    for m in markers:
        if m not in text:
            errors.append(f'گارد مثبت شکست در {rel}: «{m}»')

for rel, patterns in NEGATIVE_FILE.items():
    text = open(os.path.join(REPO, rel), encoding='utf-8').read()
    for p in patterns:
        if p in text:
            errors.append(f'گارد منفی شکست در {rel}: «{p}» هنوز هست')

if errors:
    print('✗ گاردها شکست:')
    for e in errors:
        print('  -', e)
    raise SystemExit(1)
print('✓ همهٔ گاردهای انسجام پاس شدند')

if os.path.exists(OUT):
    os.remove(OUT)
with zipfile.ZipFile(OUT, 'w', zipfile.ZIP_DEFLATED) as z:
    for rel in FILES:
        z.write(os.path.join(REPO, rel), rel)
    z.writestr('changed-files.txt', '\n'.join(sorted(FILES)) + '\n')
    z.writestr('Update-Guide-2.9.0.html', GUIDE)

# ─── چند-ساخت-و-بررسی ───
with zipfile.ZipFile(OUT) as z:
    names = set(z.namelist())
    missing = [f for f in FILES if f not in names]
    assert not missing, missing
    assert 'Update-Guide-2.9.0.html' in names
    t = z.read('src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml').decode('utf-8')
    assert 'MinHeight="96"' in t and 'TileShadow' not in t
    m = z.read('src/Sahand.ClinicPro.Desktop/MainWindow.xaml').decode('utf-8')
    assert 'ToastShift" Y="0"' in m
    s = z.read('src/Sahand.ClinicPro.Desktop/ViewModels/StockInFormViewModel.cs').decode('utf-8')
    assert 'BatchLines' in s

print(f'✓ بسته ساخته شد: {OUT} — {len(FILES)} فایل، {os.path.getsize(OUT)/1024:.0f} KB')
