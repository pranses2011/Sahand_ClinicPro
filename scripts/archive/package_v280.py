#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""چرخهٔ چهل‌وچهارم — ساخت بستهٔ آپدیت 2.7.0 → 2.8.0 (۲۵ فایل سورس + راهنما + مانیفست)

گاردهای انسجام (درس چرخه‌های ۴۱-۴۳ — صرفاً بسته‌بندی نیستند؛ چند-ساخت-و-بررسی
در پایان اسکریپت): هر فایل باید نشان چرخهٔ ۴۴ را بایت‌قطعی داشته باشد و
الگوهای کهنه هم باید از کل مخزن حذف شده باشند."""
import os
import zipfile

REPO = '/home/z/my-project/Sahand_ClinicPro'
OUT = '/home/z/my-project/download/SahandClinicPro-Update-2.7.0-to-2.8.0.zip'

# ─── فایل‌های دلتای کاربر (بدون اسکریپت‌های داخلی انتشار) ───
FILES = [
    'Directory.Build.props',
    'CHANGELOG.md',
    # ─── سرور API ───
    'src/Sahand.ClinicPro.API/Controllers/RolesController.cs',
    'src/Sahand.ClinicPro.API/Middleware/AuditLogMiddleware.cs',
    'src/Sahand.ClinicPro.API/Sahand.ClinicPro.API.csproj',
    # ─── تست (قفل الگوی لاگ عملیات) ───
    'tests/Sahand.ClinicPro.API.IntegrationTests/AuditLogEntityNameExtractionTests.cs',
    # ─── دسکتاپ: پوسته و نشست ───
    'src/Sahand.ClinicPro.Desktop/App.xaml',
    'src/Sahand.ClinicPro.Desktop/App.xaml.cs',
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Helpers/PageCatalog.cs',
    # ─── دسکتاپ: کنترل‌ها و فرم پزشک ───
    'src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/DoctorFormWindow.xaml',
    # ─── دسکتاپ: پذیرش/نوبت‌دهی/بیمهٔ خودکار ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionFormViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/AppointmentFormViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/ViewModels/AppointmentsViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml',
    'src/Sahand.ClinicPro.Desktop/Views/AppointmentsView.xaml',
    # ─── دسکتاپ: حضور و غیاب — بازسازی شیفت‌ها ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/AttendanceViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/AttendanceView.xaml',
    # ─── دسکتاپ: نقش‌ها و دسترسی‌ها ───
    'src/Sahand.ClinicPro.Desktop/ViewModels/RolesViewModel.cs',
    'src/Sahand.ClinicPro.Desktop/Views/RolePermissionView.xaml',
    # ─── دسکتاپ: داشبورد ───
    'src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml',
]

# ─── گاردهای مثبت: نشان چرخهٔ ۴۴ در هر فایل (بایت-قطعی) ───
GUARDS = {
    'Directory.Build.props': ['<Version>2.8.0</Version>'],
    'CHANGELOG.md': ['## [v2.8.0]'],
    'src/Sahand.ClinicPro.API/Controllers/RolesController.cs': ['["attendance"] = "حضور و غیاب"', 'چرخهٔ چهل‌وچهارم'],
    'src/Sahand.ClinicPro.API/Middleware/AuditLogMiddleware.cs': ['FindTargetTable', 'NormalizeName'],
    'src/Sahand.ClinicPro.API/Sahand.ClinicPro.API.csproj': ['InternalsVisibleTo Include="Sahand.ClinicPro.API.IntegrationTests"'],
    'tests/Sahand.ClinicPro.API.IntegrationTests/AuditLogEntityNameExtractionTests.cs': ['Sub_Resource_Route_Returns_Segment_Near_Id'],
    'src/Sahand.ClinicPro.Desktop/App.xaml': ['TargetType="ToolTip"', 'ToolTipService.InitialShowDelay'],
    'src/Sahand.ClinicPro.Desktop/App.xaml.cs': ['StartSessionAsync'],
    'src/Sahand.ClinicPro.Desktop/MainWindow.xaml.cs': ['BeginInvokeClose', '_exitConfirmInProgress'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs': ['public async Task StartSessionAsync()', 'BuildNavMenu', 'RebuildNavItems'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/DashboardViewModel.cs': ['OnSessionStartedAsync', '_suppressEditExitSave'],
    'src/Sahand.ClinicPro.Desktop/Helpers/PageCatalog.cs': ['ShowInPicker: false, Group: "جریان درمان"'],
    'src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml': ['ClickMode="Press"', 'Focusable="False"'],
    'src/Sahand.ClinicPro.Desktop/Views/DoctorFormWindow.xaml': ['Width="106"'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionsViewModel.cs': ['ApplyPatientInsurance', 'OnFoundPatientChanged'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/ReceptionFormViewModel.cs': ['ApplyPatientInsurance'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/AppointmentFormViewModel.cs': ['InsuranceCompanyId = null'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/AppointmentsViewModel.cs': ['InsuranceCompanyId = null'],
    'src/Sahand.ClinicPro.Desktop/Views/ReceptionsView.xaml': ['جستجو ← انتخاب بیمار ← پذیرش', 'چرخهٔ ۴۴'],
    'src/Sahand.ClinicPro.Desktop/Views/AppointmentsView.xaml': ['بیمار ← پزشک ← زمان آزاد ← ثبت'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/AttendanceViewModel.cs': ['EditingShiftId', 'EndAssignmentAsync', 'EmployeeOption('],
    'src/Sahand.ClinicPro.Desktop/Views/AttendanceView.xaml': ['بازسازی کامل چرخهٔ ۴۴', 'EndAssignmentCommand'],
    'src/Sahand.ClinicPro.Desktop/ViewModels/RolesViewModel.cs': ['PermissionSearch', 'HasUnsavedChanges'],
    'src/Sahand.ClinicPro.Desktop/Views/RolePermissionView.xaml': ['جستجوی مجوز یا ماژول', 'ذخیره نشده!'],
    'src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml': ['حالت مدیریت چیدمان'],
}

# ─── گاردهای منفی: الگوهای کهنه باید از کل مخزن حذف شده باشند ───
NEGATIVE_GLOBAL = {
    # تخصیص شیفت با تایپ شناسهٔ عددی — حذف کامل
    'AssignEmployeeId': [],
    # ساخت مستقیم منو در سازنده — باید منتقل شده باشد
    'NavItems = new ObservableCollection<NavItem>': [],
    # دکمه‌های فوکوس‌پذیر پاپ‌آپ ساعت — باید Focusable=False گرفته باشند
    'x:Name="OkButton" Click="OnAcceptClick"\n                                Content="تأیید" FontSize="11.5"\n': [],
}


def check_guards():
    """همهٔ گاردها پیش از ساخت بسته سنجیده می‌شوند — شکست = توقف"""
    errors = []

    # گاردهای مثبت — فایل‌به‌فایل
    for rel, markers in GUARDS.items():
        path = os.path.join(REPO, rel)
        if not os.path.exists(path):
            errors.append(f'نبود فایل: {rel}')
            continue
        text = open(path, encoding='utf-8').read()
        for m in markers:
            if m not in text:
                errors.append(f'گارد مثبت شکست در {rel}: «{m}» یافت نشد')

    # گاردهای منفی — سراسری روی فایل‌های دسکتاپ/سرور (بدون Migrations و bin/obj)
    import pathlib
    for pattern in NEGATIVE_GLOBAL:
        for p in pathlib.Path(REPO).rglob('*'):
            s = str(p)
            if any(x in s for x in ('/bin/', '/obj/', '/Migrations/', '.git/', 'download')):
                continue
            if p.suffix in ('.cs', '.xaml') and '/Sahand_ClinicPro/' in s:
                try:
                    if pattern in p.read_text(encoding='utf-8'):
                        errors.append(f'گارد منفی شکست: «{pattern}» هنوز در {p.relative_to(REPO)} هست')
                        break
                except Exception:
                    pass

    return errors


GUIDE = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<title>راهنمای آپدیت سهند کلینیک‌پرو ۲.۸.۰</title>
<style>
body{font-family:Vazirmatn,Tahoma,'Segoe UI',sans-serif;direction:rtl;background:#f4f7fb;margin:0;padding:24px;color:#1f2937}
.wrap{max-width:860px;margin:0 auto}
h1{color:#0d47a1;font-size:24px;border-bottom:3px solid #1e88e5;padding-bottom:10px}
h2{color:#1565c0;font-size:17px;margin-top:26px}
.card{background:#fff;border:1px solid #dce7f0;border-radius:12px;padding:16px 20px;margin:14px 0;box-shadow:0 1px 3px rgba(30,58,95,.06)}
ol,.steps{line-height:2.1}
.tag{display:inline-block;background:#e3f2fd;color:#1565c0;border:1px solid #bbdefb;border-radius:8px;padding:2px 10px;font-size:12px;margin:2px}
.warn{background:#fff8e1;border:1px solid #ffe082;border-radius:10px;padding:12px 16px}
.ok{color:#2e7d32;font-weight:700}
ul{line-height:2}
code{background:#eef3f8;border-radius:5px;padding:1px 6px;font-size:13px}
</style>
</head>
<body><div class="wrap">
<h1>📦 بستهٔ آپدیت سهند کلینیک‌پرو — 2.7.0 ← 2.8.0</h1>
<div class="card">
<b>این نسخه چه می‌آورد؟</b> (چرخهٔ چهل‌وچهارم — یازده گزارش شما)
<div class="steps">
۱. <b>لاگ عملیات — ریشهٔ قطعی «مقادیر قبلی»:</b> ویرایش پرسنل/بیمه/اتاق/خدمات و همهٔ مسیرهای زیرمنبعی اکنون مقادیر قبلی واقعی را نشان می‌دهند<br>
۲. <b>خروج از حساب:</b> ورود با کاربر دوم، همه‌چیز را واقعاً نو می‌کند (نام/عکس/منو/میانبرها/اعلان‌ها/اتصال بلادرنگ)<br>
۳. <b>خطای خروج از برنامه</b> (Cannot call Close while a Window is closing) برطرف شد<br>
۴. <b>پذیرش:</b> با انتخاب بیمار، نوع بیمه از پرونده‌اش خودکار انتخاب می‌شود<br>
۵. <b>تعریف شیفت‌ها و تخصیص به پرسنل</b> کاملاً بازسازی شد (انتخابگر ساعت و تاریخ شمسی + جستجوی پرسنل + ویرایش/حذف + پایان تخصیص)<br>
۶. <b>برنامهٔ کاری پزشک:</b> دکمه‌های تأیید/انصراف/صبح/عصرِ انتخابگر ساعت کار می‌کنند + ستون‌های باریک‌تر بدون اسکرول افقی<br>
۷. <b>تولتیپ‌ها</b> چسبیده به نشانگر ماوس + ظاهر یکدست در کل برنامه<br>
۸. <b>داشبورد از لیست «افزودن میانبر» حذف شد</b> (ایراد ابهام)<br>
۹. <b>نقش‌ها و دسترسی‌ها:</b> ۵ ماژول جدید عنوان فارسی گرفت + جستجوی زندهٔ مجوزها + نشان «ذخیره‌نشده» + بازطراحی کامل<br>
۱۰. <b>صفحات پذیرش/نوبت‌دهی/داشبورد</b> با سربرگ‌های گرادیانی حرفه‌ای شدند
</div>
</div>

<h2>🧩 محتویات بسته</h2>
<div class="card">
<span class="tag">۲۵ فایل سورس تغییرکرده</span> <span class="tag">changed-files.txt</span> <span class="tag">این راهنما</span>
<p style="font-size:13px">هر دو پروژهٔ <b>سرور API</b> و <b>دسکتاپ</b> در این نسخه تغییر دارند؛
<b>هیچ مهاجرت دیتابیس جدیدی وجود ندارد</b> — جداول شیفت/تخصیص از قبل بوده‌اند.
تغییرات csproj سرور فقط «InternalsVisibleTo برای تست» است و روی انتشار بی‌اثر است.</p>
</div>

<h2>⚙️ گام‌های نصب</h2>
<div class="card"><ol>
<li>از پوشهٔ فعلی پروژه و دیتابیس <b>پشتیبان بگیرید</b>.</li>
<li>محتوای این زیپ را روی ریشهٔ پروژه (<code>E:\\Sahand_ClinicPro</code>) کپی و <b>جایگزین</b> کنید (۲۵ فایل).</li>
<li>ویژوال استودیو: روی سولوشن <b>Clean</b> سپس <b>Rebuild Solution</b> (هر دو پروژهٔ API و Desktop).</li>
<li>سرور API را دوباره <b>Publish</b> و روی IIS/سرویس جایگزین کنید، سپس ری‌استارت.</li>
<li>خروجی جدید دسکتاپ را به ایستگاه‌ها کپی کنید و برنامه را اجرا کنید.</li>
</ol></div>

<h2>✅ سنجه‌های موفقیت (پس از نصب بررسی کنید)</h2>
<div class="card"><ul>
<li>زیر پنجرهٔ اصلی، نسخهٔ <b class="ok">2.8.0</b> نمایش داده می‌شود</li>
<li><b>لاگ عملیات:</b> یک پرسنل را ویرایش کنید → ردیف لاگ جدید → ستون موجودیت «employees» و بخش «مقادیر قبلی» پر است</li>
<li><b>خروج از حساب:</b> با کاربر دیگری وارد شوید → نام/عکس بالای صفحه و میانبرهای داشبورد همان کاربرِ تازه است</li>
<li><b>خروج از برنامه</b> (X) → دیالوگ تأیید → تأیید کنید → بدون هیچ خطا بسته می‌شود (حتی بدون انتخاب پشتیبان‌گیری)</li>
<li><b>پذیرش:</b> بیماری با بیمهٔ ثبت‌شده را جستجو و انتخاب کنید → کشوی «نوع بیمه» خودکار بیمهٔ او را می‌گیرد</li>
<li><b>حضور و غیاب ← تب شیفت‌ها:</b> فرم تازه با انتخابگر ساعت/تاریخ؛ «جستجوی پرسنل» با تایپ نام پالایش می‌کند؛ ثبت تخصیص و «پایان» آن کار می‌کند</li>
<li><b>برنامهٔ کاری پزشک:</b> انتخابگر ساعت باز → دکمه‌های صبح/عصر/تأیید/انصراف پاسخ می‌دهند؛ جدول بدون اسکرول افقی جا شده است</li>
<li><b>نقش‌ها:</b> گروه‌های «حضور و غیاب/حقوق و فیش حقوقی/نظرسنجی/اعلان‌ها/پرونده بالینی» فارسی‌اند؛ جستجوی مجوزها کار می‌کند؛ تغییر چک‌باکس → چیپ «ذخیره نشده!»</li>
<li><b>تولتیپ:</b> ماوس روی کاشی میانبر داشبورد → تولتیپ درست کنار نشانگر، نه دور</li>
</ul></div>

<div class="warn">⚠️ اگر پیش از این میانبر «داشبورد» را خودتان پین کرده بودید، آن کاشی پس از این نسخه از پنل کنده می‌شود — این رفتار خواستهٔ شماست (ایراد ابهام).</div>

<h2>🧯 بازگشت</h2>
<div class="card">در صورت مشکل: بستهٔ قبلی 2.7.0 را بازنصب کنید؛ بدون مهاجرت دیتابیس، بازگشت بی‌دردسر است.</div>
</div></body></html>
"""

# ─── ساخت بسته ───
errs = check_guards()
if errs:
    print('✗ گاردهای انسجام شکست:')
    for e in errs:
        print('  -', e)
    raise SystemExit(1)
print('✓ همهٔ گاردهای انسجام پاس شدند')

if os.path.exists(OUT):
    os.remove(OUT)

with zipfile.ZipFile(OUT, 'w', zipfile.ZIP_DEFLATED) as z:
    for rel in FILES:
        z.write(os.path.join(REPO, rel), rel)
    # مانیفست
    manifest = '\n'.join(sorted(FILES)) + '\n'
    z.writestr('changed-files.txt', manifest)
    z.writestr('Update-Guide-2.8.0.html', GUIDE)

# ─── چند-ساخت-و-بررسی: بازخوانی بستهٔ ساخته‌شده ───
with zipfile.ZipFile(OUT) as z:
    names = set(z.namelist())
    missing = [f for f in FILES if f not in names]
    assert not missing, f'فایل‌های جاافتاده در بسته: {missing}'
    assert 'Update-Guide-2.8.0.html' in names and 'changed-files.txt' in names
    # گارد نمونه‌ای از محتوای زیپ (خواندن بایت‌ها از خود بسته)
    sample = z.read('src/Sahand.ClinicPro.API/Middleware/AuditLogMiddleware.cs').decode('utf-8')
    assert 'FindTargetTable' in sample
    sample2 = z.read('src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs').decode('utf-8')
    assert 'StartSessionAsync' in sample2

size = os.path.getsize(OUT)
print(f'✓ بسته ساخته شد: {OUT}')
print(f'  {len(FILES)} فایل سورس + مانیفست + راهنما — {size/1024:.0f} KB')
