#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📦 ساخت بستهٔ آپدیت v2.4.0 → v2.4.1 — چرخهٔ بیست‌وهشتم.
فقط فایل‌های تغییرکردهٔ همین رلیز (تگ v2.4.0 تا HEAD) با همان ساختار
پوشه‌ای مخزن + مانیفست متنی + راهنمای فارسی HTML.

گاردهای انسجام: نشانه‌های سلامت تغییرات کلیدی این نسخه باید در سورس موجود
باشند وگرنه اسکریپت متوقف می‌شود.
"""
import pathlib
import subprocess
import zipfile

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
BASE = "v2.4.0"    # رلیز چرخهٔ بیست‌وهفتم
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.4.0-to-2.4.1.zip")
MANIFEST_NAME = "changed-files.txt"
GUIDE_NAME = "Update-Guide-2.4.1.html"

# ─── گارد انسجام مثبت — نشانه‌های سلامت تغییرات کلیدی این نسخه ───
COHERENCE_CHECKS = [
    # آیکون منو — خروجی enum مستقیم
    ("src/Sahand.ClinicPro.Desktop/Converters/IconSetKindConverter.cs",
     "return kind", "خروجی enum مبدل آیکون"),
    # بسته‌شدن انتخابگر ساعت
    ("src/Sahand.ClinicPro.Desktop/Controls/TimePicker.xaml.cs",
     "PickerPopup.Closed", "همگام‌سازی تاگل با Closed پاپ‌آپ ساعت"),
    # نقش‌های کاربر در ویرایش
    ("src/Sahand.ClinicPro.Desktop/ViewModels/UserFormViewModel.cs",
     "await LoadRolesAsync()", "انتظار برای نقش‌ها پیش از تیک‌زدن"),
    # اسلات‌های پر نوبت‌دهی — تداخل بازه‌ای + پذیرش
    ("src/Sahand.ClinicPro.Application/Features/Appointments/Queries/GetAvailableSlots/GetAvailableSlotsQuery.cs",
     "تداخل بازه‌ای", "الگوریتم تداخل بازه‌ای اسلات"),
    ("src/Sahand.ClinicPro.Application/Features/Appointments/Queries/GetAvailableSlots/GetAvailableSlotsQuery.cs",
     "Reception", "پوشش پذیرش‌های همان روز"),
    # پاراکلینیک بیمارمحور
    ("src/Sahand.ClinicPro.API/Controllers/ParaclinicalController.cs",
     "GetDentalChart", "اندپوینت بازیابی چارت دندان"),
    ("src/Sahand.ClinicPro.API/Controllers/ParaclinicalController.cs",
     "GetEyeExams", "اندپوینت سابقهٔ چشم"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ParaclinicalViewModel.cs",
     "SaveDentalChartAsync", "ذخیرهٔ چارت دندان در پروندهٔ بیمار"),
    ("src/Sahand.ClinicPro.Desktop/Views/ParaclinicalView.xaml",
     "DentalPatientSearch", "نوار بیمار تب دندانپزشکی"),
    # خروج با پشتیبان
    ("src/Sahand.ClinicPro.Desktop/Views/ThemedDialog.xaml.cs",
     "ShowExitConfirm", "دیالوگ تأیید خروج با چک‌باکس"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml.cs",
     "ConfirmExitAsync", "قلاب تأیید خروج پنجرهٔ اصلی"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml.cs",
     "CloseForLoginSwitch", "مسیر خروج از حساب بدون تأییدیه"),
    # تازه‌سازی فوری میزکار پزشک
    ("src/Sahand.ClinicPro.Desktop/ViewModels/MainWindowViewModel.cs",
     "DoctorVisitViewModel doctorVisit", "صف بلادرنگ میزکار پزشک"),
    # پارس فارسی مبالغ
    ("src/Sahand.ClinicPro.Desktop/Helpers/Money.cs",
     "TryParseRialDigits", "پارس امن ارقام فارسی"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/DoctorsViewModel.cs",
     "TryParseRialDigits", "رفع حق ویزیت عددی نامعتبر"),
    # نظرسنجی — طراح و DELETE
    ("src/Sahand.ClinicPro.API/Controllers/SurveysController.cs",
     "DeleteSurvey", "حذف نرم نظرسنجی"),
    ("src/Sahand.ClinicPro.API/Controllers/SurveysController.cs",
     "string? Questions)", "سوالات JSON در GET فهرست"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/SurveyViewModel.cs",
     "ParseList", "پارسر سوالات نظرسنجی"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/ComplementaryViewModel.cs",
     "OpenSurveyDesigner", "طراح سوالات تب نظرسنجی"),
    # رنگ بیمه
    ("src/Sahand.ClinicPro.Desktop/Converters/InsuranceBrushConverter.cs",
     "InsuranceBrushConverter", "مبدل رنگ متمایز بیمه"),
    # فونت کنسول API
    ("src/Sahand.ClinicPro.API/Program.cs",
     "TrySetPersianCapableConsoleFont", "فونت شکل‌دهندهٔ کنسول"),
    # عرض ستون‌های ساعت
    ("src/Sahand.ClinicPro.Desktop/Views/DoctorFormWindow.xaml",
     'Width="156"', "ستون‌های ۱۵۶px ساعت برنامهٔ کاری"),
    # نسخه
    ("Directory.Build.props",
     "2.4.1", "نسخهٔ 2.4.1"),
]

# نشانه‌های منفی — نباید وجود داشته باشند
NEGATIVE_CHECKS = [
    # الگوی قدیمی رشته‌ای مبدل آیکون حذف شده باشد
    ("src/Sahand.ClinicPro.Desktop/Converters/IconSetKindConverter.cs",
     'return map is not null && map.TryGetValue(baseIcon, out var mapped)\n            ? mapped', "خروجی رشته‌ای مبدل حذف"),
    # pageSize=1 حذف شده باشد (نشانگر = خود رشتهٔ پرس‌وجو، نه اشارهٔ کامنتی)
    ("src/Sahand.ClinicPro.Desktop/ViewModels/UserFormViewModel.cs",
     "pageSize=1&", "پرس‌وجوی pageSize=1 حذف"),
    # تساوی دقیق اسلات حذف شده باشد
    ("src/Sahand.ClinicPro.Application/Features/Appointments/Queries/GetAvailableSlots/GetAvailableSlotsQuery.cs",
     "bookedMap.ContainsKey", "الگوی تساوی دقیق اسلات حذف"),
]


def run(cmd: str) -> str:
    return subprocess.run(cmd, shell=True, cwd=REPO, capture_output=True, text=True).stdout.strip()


# ─── گاردها ───
errors = []
for rel, marker, desc in COHERENCE_CHECKS:
    p = REPO / rel
    if not p.exists() or marker not in p.read_text(encoding="utf-8"):
        errors.append(f"گارد مثبت شکست خورد — {desc}: {rel} ⊂ «{marker}»")
for rel, marker, desc in NEGATIVE_CHECKS:
    p = REPO / rel
    if p.exists() and marker in p.read_text(encoding="utf-8"):
        errors.append(f"گارد منفی شکست خورد — {desc}: {rel} ∌ «{marker}»")
if errors:
    for e in errors:
        print("⛔", e)
    raise SystemExit(1)
print("✅ همهٔ گاردهای انسجام پاس شد")

# ─── فهرست فایل‌های تغییرکرده ───
changed = run(f"git diff --name-only --diff-filter=ACMR {BASE}..HEAD -- src tests Directory.Build.props CHANGELOG.md")
files = [f for f in changed.splitlines() if f.strip()]
if not files:
    raise SystemExit("⛔ هیچ فایل تغییرکرده‌ای یافت نشد")
print(f"📦 {len(files)} فایل تغییرکرده")

# ─── راهنمای فارسی HTML ───
guide = """<!DOCTYPE html>
<html lang="fa" dir="rtl"><head><meta charset="utf-8">
<title>راهنمای به‌روزرسانی ۲.۴.۰ به ۲.۴.۱</title>
<style>body{font-family:Tahoma,'Segoe UI';direction:rtl;background:#F4F7FB;margin:0;padding:24px;color:#1A2C42}
.card{max-width:760px;margin:auto;background:#fff;border-radius:14px;padding:28px;box-shadow:0 4px 18px rgba(20,50,90,.12)}
h1{font-size:20px;color:#1565C0}ol{line-height:2.1}b{color:#2E7D32}
.warn{background:#FFF8E1;border-radius:10px;padding:12px 16px;border-right:4px solid #F9A825;margin:14px 0}
.ok{background:#E8F5E9;border-radius:10px;padding:12px 16px;border-right:4px solid #43A047;margin:14px 0}
code{background:#ECEFF1;border-radius:5px;padding:2px 6px;direction:ltr;display:inline-block}
ul{line-height:2}</style></head><body><div class="card">
<h1>📦 بستهٔ به‌روزرسانی سهند کلینیک‌پرو — ۲.۴.۰ به ۲.۴.۱</h1>
<div class="ok">این بسته فقط فایل‌های تغییرکرده را دارد؛ همه را با همان ساختار پوشه‌ای روی نسخهٔ نصب‌شدهٔ <b>۲.۴.۰</b> کپی و جایگزین کنید.</div>
<h3>مراحل نصب</h3>
<ol>
<li>برنامهٔ دسکتاپ و <b>سرور API</b> را کامل ببندید (مطمئن شوید پردازهٔ Sahand.ClinicPro.API در حال اجرا نیست)</li>
<li>از پوشهٔ نصب فعلی، یک نسخهٔ پشتیبان (کپی کامل) تهیه کنید</li>
<li>محتوای این بسته را با همان ساختار پوشه‌ها روی پوشهٔ نصب کپی و <b>جایگزین</b> کنید</li>
<li>سرور API را اجرا کنید — این نسخه مهاجرت دیتابیس جدیدی ندارد؛ اندپوینت‌های جدید (چارت دندان/سابقهٔ چشم/حذف نظرسنجی) هم‌زمان با نخستین اجرا فعال‌اند</li>
<li>برنامهٔ دسکتاپ را اجرا و از نسخهٔ ۲.۴.۱ در Splash مطمئن شوید</li>
</ol>
<div class="warn"><b>نکتهٔ مهم:</b> اگر ویژوال استودیو باز است، پیش از باز کردن راه‌حل آن را ببندید یا اسکریپت <code>Clean-VS-Cache.bat</code> را اجرا کنید تا کش طراح XAML قدیمی، قالب‌های تازه را نشان ندهد (تجربهٔ چرخهٔ ۲۵).</div>
<h3>خلاصهٔ تغییرات مهم این نسخه</h3>
<ul>
<li>🎨 <b>آیکون‌های منوی کناری</b> — ریشهٔ «همه با یک آیکون» رفع شد؛ هر آیتم آیکون واقعی خودش را می‌گیرد (هر سه مجموعهٔ کلاسیک/مدرن/مینیمال)</li>
<li>⏰ انتخابگر ساعت با دکمهٔ تأیید بسته می‌شود + Escape + ستون‌های ساعت برنامهٔ کاری پزشک کامل دیده می‌شوند (۱۵۶px)</li>
<li>👤 نقش‌های کاربر در تب ویرایش نمایش داده می‌شوند و ذخیره می‌شوند</li>
<li>📅 نوبت‌دهی: ساعت‌های پر (نوبت‌ها + پذیرش‌های همان روز همان پزشک) کم‌رنگ و غیرفعال نشان داده می‌شوند — با نام بیمار در تولتیپ</li>
<li>🦷👁 دندانپزشکی و چشم‌پزشکی بیمارمحور شدند: جستجوی بیمار + ذخیره/بازیابی چارت ۳۲ دندان در پرونده + سابقهٔ ۲۰ معاینهٔ چشم + ثبت کامل اندازه‌ها (IOP/PD/حدت بینایی)</li>
<li>🚪 خروج از برنامه با تأییدیهٔ فارسی + چک‌باکس «پشتیبان‌گیری قبل از خروج» (پیش‌فرض روشن)</li>
<li>💰 «حق ویزیت عددی نامعتبر است» در ویرایش پزشک رفع شد + یکدستی ریال/تومان در همهٔ فرم‌های مبلغی</li>
<li>📝 نظرسنجی‌ها واقعاً طراحی می‌شوند: سوالات موجود بارگذاری و ویرایش می‌شوند + کلیدهای سطری طراح/نتایج/فعال‌سازی/حذف</li>
<li>🏥 هر بیمه در لیست بیماران رنگ متمایز خودش را دارد (بیمه‌های سفارشی هم)</li>
<li>🩺 پذیرش جدید همان لحظه در صف میزکار پزشک می‌نشیند (نه با تأخیر ۳۰ ثانیه)</li>
<li>🖥 لاگ فارسی کنسول API خوانا شد (حروف متصل — فونت TrueType خودکار)</li>
</ul>
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
               f"بستهٔ به‌روزرسانی 2.4.0 → 2.4.1\nتعداد فایل: {len(manifest_lines)}\n\n" + "\n".join(manifest_lines))

print(f"✅ ساخته شد: {OUT} ({OUT.stat().st_size / 1024:.0f} کیلوبایت)")
print(f"— راهنما: {GUIDE_NAME} | مانیفست: {MANIFEST_NAME}")
