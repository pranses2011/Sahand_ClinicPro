#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📦 ساخت بستهٔ آپدیت v2.0.3 → v2.0.4 — چرخه هفدهم (گزارش یازدهم مالک)
فقط فایل‌های تغییرکردهٔ همین رلیز (قاعدهٔ دائمی توزیع از P9/P10)، با همان
ساختار پوشه‌ای مخزن، به‌همراه راهنمای فارسی HTML و مانیفست متنی فایل‌ها.

رفع‌های این بسته (هر ۵ مورد گزارش یازدهم):
  ۱) وارونگی رمز ماسک‌شده — حذف بایندینگ PasswordBoxAssist + InputSanitizer
  ۲) فعال‌سازی لایسنس — LicenseGate + پنجرهٔ LicenseActivationWindow + گیت سه‌گانه
  ۳) تم آبی — PrimaryColor=Blue + سایدبار آبی تیره
  ۴) داشبورد تمام‌صفحه — WindowState=Maximized
  ۵) کرش دکمهٔ بیمار جدید — Mode=OneWay روی پراپرتی‌های فقط-خواندنی تگ‌ها

نحوهٔ استفاده:
    python scripts/create_update_zip_v204.py [مسیر-خروجی.zip]

پیش‌فرض خروجی: ریشهٔ مخزن / SahandClinicPro-Update-2.0.3-to-2.0.4.zip

ایمنی‌ها:
- توقف اگر فایل حذف‌شده (D) در محدوده باشد
- توقف اگر appsettings در فهرست باشد (تنظیم SQL مالک هرگز بازنویسی نشود)
- گارد انسجام مثبت: نشانه‌های سلامت رفع‌های همین نسخه باید در HEAD موجود باشند
- گارد انسجام منفی: بایندینگ PasswordBoxAssist باید از پنجره‌های ورود/تغییر رمز
  «حذف شده» باشد (رفع وارونگی رمز — درس CS0103: جفت فایل‌ها همیشه هماهنگ خارج شوند)
⚠️ درس چرخه ۱۵: HTML حاوی آکولاد است — بدون f-string، فقط الحاق ساده و .replace()
"""
import pathlib
import subprocess
import sys
import zipfile

REPO = pathlib.Path(__file__).resolve().parents[1]
TAG = "v2.0.3"
OUT_NAME = "SahandClinicPro-Update-2.0.3-to-2.0.4.zip"
GUIDE_NAME = "Update-Guide-2.0.4.html"
MANIFEST_NAME = "changed-files.txt"

# ─── گارد انسجام مثبت — نشانه‌های سلامت که باید در محتوای فعلی HEAD موجود باشند ───
COHERENCE_CHECKS = [
    ("src/Sahand.ClinicPro.Desktop/Views/PatientFormWindow.xaml",
     'Tags="{Binding Allergies, Mode=OneWay}"', "رفع کرش بیمار جدید (OneWay)"),
    ("src/Sahand.ClinicPro.Desktop/Views/LoginWindow.xaml",
     'KeyDown="OnPasswordKeyDown"', "جفت XAML/کد-پشت پنجرهٔ ورود"),
    ("src/Sahand.ClinicPro.Desktop/Views/ChangePasswordWindow.xaml.cs",
     "CurrentPasswordBox.PasswordChanged", "همگام‌سازی تک‌مسیری کادرهای ماسک تغییر رمز"),
    ("src/Sahand.ClinicPro.Desktop/Services/InputSanitizer.cs",
     "StripDirectionMarks", "پاک‌ساز نویسه‌های نامرئی جهت"),
    ("src/Sahand.ClinicPro.Desktop/Services/LicenseGate.cs",
     "public static class LicenseGate", "گیت لایسنس"),
    ("src/Sahand.ClinicPro.Desktop/Views/LicenseActivationWindow.xaml.cs",
     "ActivatedSuccessfully", "پنجرهٔ فعال‌سازی مستقل لایسنس"),
    ("src/Sahand.ClinicPro.Desktop/ViewModels/LoginViewModel.cs",
     "LicenseActivationRequired?.Invoke", "گیت لایسنس در جریان ورود"),
    ("src/Sahand.ClinicPro.Desktop/App.xaml",
     'PrimaryColor="Blue"', "تم آبی"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     'WindowState="Maximized"', "باز شدن تمام‌صفحهٔ داشبورد"),
    ("src/Sahand.ClinicPro.Desktop/MainWindow.xaml",
     "#16355E", "سایدبار آبی تیره مطابق اسکرین‌شات‌ها"),
    ("Directory.Build.props",
     "<Version>2.0.4</Version>", "نسخهٔ 2.0.4"),
]

# ─── گارد انسجام منفی — ریشهٔ وارونگی رمز باید «نباشد» ───
# (نشانگر، الگوی کاملِ بایندینگ است نه صرفِ نام — کامنت‌های توضیحی مجازند)
ABSENCE_CHECKS = [
    ("src/Sahand.ClinicPro.Desktop/Views/LoginWindow.xaml",
     'PasswordBoxAssist.Password="{Binding', "بایندینگ مشکل‌ساز رمز پنجرهٔ ورود"),
    ("src/Sahand.ClinicPro.Desktop/Views/ChangePasswordWindow.xaml",
     'PasswordBoxAssist.Password="{Binding', "بایندینگ مشکل‌ساز رمز پنجرهٔ تغییر رمز"),
]


def changed_files():
    """فهرست (وضعیت، مسیر) همهٔ فایل‌های تغییرکرده از تگ تا HEAD"""
    out = subprocess.run(
        ["git", "diff", "--name-status", TAG + "..HEAD"],
        cwd=REPO, capture_output=True, text=True, check=True,
    ).stdout
    files = []
    for line in out.splitlines():
        line = line.rstrip("\n")
        if not line.strip():
            continue
        parts = line.split("\t")
        status, path = parts[0], parts[-1]
        files.append((status, path))
    return files


def group_title(path: str) -> str:
    """گروه‌بندی فارسی برای نمایش در راهنما"""
    p = path.replace("\\", "/")
    if p.startswith("src/Sahand.ClinicPro.Desktop/Views/"):
        return "پنجره‌ها و نماها (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/Controls/"):
        return "کنترل‌های مشترک (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/ViewModels/"):
        return "ViewModel ها (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/Services/"):
        return "سرویس‌ها (دسکتاپ)"
    if p.startswith("src/Sahand.ClinicPro.Desktop/"):
        return "هستهٔ برنامهٔ دسکتاپ"
    if p.startswith("tests/"):
        return "پروژه‌های تست"
    if p.startswith("docs/"):
        return "مستندات و راهنماها"
    return "فایل‌های ریشهٔ مخزن"


# پالت آبی — هماهنگ با تم جدید محصول (چرخه هفدهم)
CSS = """
:root{--ink:#1c2b3a;--mut:#5a6b7d;--line:#dfe7ee;--brand:#1565C0;--brand2:#0D47A1;
--bg:#f4f7fb;--card:#ffffff;--ok:#2E7D32;--warn:#9a6700;--bad:#b3372f}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--ink);
font-family:"Vazirmatn","Segoe UI",Tahoma,Arial,sans-serif;line-height:1.9;font-size:15px}
.wrap{max-width:880px;margin:0 auto;padding:24px 20px 64px}
header.hero{background:linear-gradient(135deg,var(--brand) 0%,var(--brand2) 100%);
color:#fff;border-radius:16px;padding:28px 26px;margin-bottom:26px}
header.hero h1{margin:0 0 6px;font-size:24px}
header.hero p{margin:0;opacity:.92;font-size:14px}
.badge{display:inline-block;background:rgba(255,255,255,.16);border:1px solid rgba(255,255,255,.35);
border-radius:999px;padding:2px 14px;font-size:13px;margin-top:10px;margin-inline-end:6px}
h2{font-size:19px;color:var(--brand2);border-right:5px solid var(--brand);
padding-right:12px;margin:34px 0 12px}
h3{font-size:16px;margin:20px 0 8px;color:var(--brand2)}
.card{background:var(--card);border:1px solid var(--line);border-radius:12px;
padding:18px 20px;margin:12px 0}
table{width:100%;border-collapse:collapse;font-size:13.5px}
th,td{border:1px solid var(--line);padding:7px 10px;text-align:right;vertical-align:top}
th{background:#eaf1fa;color:var(--brand2)}
ul,ol{padding-right:22px;padding-left:0;margin:8px 0}
li{margin:5px 0}
code,.mono{font-family:Consolas,"Courier New",monospace;font-size:13px;
background:#eef2f7;border-radius:5px;padding:1px 6px;direction:ltr;display:inline-block}
pre{background:#12233d;color:#d7e4f4;border-radius:10px;padding:14px 16px;
overflow-x:auto;direction:ltr;text-align:left;font-family:Consolas,monospace;font-size:13px}
.note{border-right:5px solid #b98a2f;background:#fdf8ee}
.okbox{border-right:5px solid var(--ok);background:#eff8f3}
.dangerbox{border-right:5px solid var(--bad);background:#fdf0ef}
.step{display:flex;gap:12px;margin:14px 0;align-items:flex-start}
.stepnum{flex:0 0 34px;height:34px;border-radius:50%;background:var(--brand);color:#fff;
display:flex;align-items:center;justify-content:center;font-weight:700;font-size:16px}
.stepbody{flex:1}
.stepbody b{color:var(--brand2)}
details{background:var(--card);border:1px solid var(--line);border-radius:10px;
padding:10px 16px;margin:10px 0}
summary{cursor:pointer;font-weight:700;color:var(--brand2)}
.flist{font-family:Consolas,monospace;font-size:12.5px;direction:ltr;text-align:left;
color:#33475f;margin:6px 0}
.flist div{padding:1px 4px;border-bottom:1px dashed #e7ecf2}
.tagA{color:var(--ok);font-weight:700}
.tagM{color:#b98a2f;font-weight:700}
footer{margin-top:40px;color:var(--mut);font-size:12.5px;text-align:center}
.kbd{background:#e8edf2;border:1px solid #cbd5e1;border-bottom-width:3px;border-radius:6px;
padding:1px 8px;font-size:13px;font-family:inherit}
"""

GUIDE_HEAD = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>راهنمای بستهٔ آپدیت — سامانهٔ درمانگاه سهند ۲.۰.۴</title>
<style>""" + CSS + """</style>
</head>
<body>
<div class="wrap">

<header class="hero">
<h1>📦 بستهٔ آپدیت نسخهٔ ۲.۰.۳ ← ۲.۰.۴</h1>
<p>سامانهٔ جامع درمانگاه سهند — فقط فایل‌های تغییرکردهٔ این رلیز، با همان ساختار پوشه‌ها</p>
<span class="badge">__TOTAL__ فایل در این بسته</span>
<span class="badge">تاریخ انتشار: ۷ اکتبر ۲۰۲۶</span>
</header>
"""

SECTION_WHAT = """
<h2>۱) این بسته چیست؟</h2>
<div class="card">
<p>این بسته فقط <b>فایل‌های تغییرکرده از نسخهٔ ۲.۰.۳ تا ۲.۰.۴</b> را دارد — با همان ساختار پوشه‌ای مخزن.
اگر نسخهٔ فعلی شما ۲.۰.۳ است (پانویس پنجرهٔ ورود «نسخه ۲.۰.۳» را نشان می‌دهد)، همین بسته کافی است؛
و اگر نسخهٔ قدیمی‌تری دارید، اول <b>بستهٔ تجمعی 2.0.1→2.0.3</b> از رلیز قبلی و بعد این بسته را نصب کنید.</p>
</div>

<div class="card note">
<p>💡 <b>مهم‌ترین نکتهٔ این نصب:</b> همهٔ فایل‌های داخل بسته را <b>با هم</b> جایگزین کنید (۶ قدم پایین).
جایگزینیِ فقط چند فایلِ منتخب، علت بروز خطاهای قبلی بوده است — چون جفتِ فایل‌های وابسته به هم
(مثل XAML و کدِ پشتِ آن) باید همیشه با هم عوض شوند. در این بسته ۴ فایل <b>جدید</b> هم هست
(پنجرهٔ فعال‌سازی لایسنس + دو سرویس جدید) که باید دقیقاً در همان مسیرها کپی شوند.</p>
</div>
"""

SECTION_FIXES = """
<h2>۲) چه چیزهایی رفع شده؟ (هر ۵ مورد گزارش شما)</h2>

<div class="card">
<table>
<tr><th style="width:34%">موضوع</th><th>توضیح</th></tr>
<tr><td>🔐 <b>وارونگی رمز در حالت ماسک</b></td>
<td>ریشه واقعی پیدا شد: بایندینگ کتابخانهٔ MaterialDesign مقدار رمز را پشت سر هم دوباره داخل کادر ماسک می‌نوشت و مکان‌نما به ابتدای کادر می‌پرید؛ نتیجه تایپ <code>Admin@123</code> ارسال <code>321@nimdA</code> بود! آن بایندینگ کامل حذف شد + نویسه‌های نامرئی جهت (که کیبورد فارسی تزریق می‌کند) پیش از ارسال پاک می‌شوند. از این پس رمز ماسک‌شده هم بدون نیاز به آیکون چشم درست ارسال می‌شود</td></tr>
<tr><td>🔑 <b>فعال‌سازی لایسنس</b></td>
<td>دکمهٔ «فعال‌سازی لایسنس» صفحهٔ ورود پیش‌تر به هیچ کاری وصل نبود و ورود هم بدون لایسنس ممکن بود. اکنون: (۱) دکمه، پنجرهٔ فعال‌سازی مستقل را باز می‌کند (فعال‌سازی آنلاین با کلید و آفلاین با فایل <code>.lic</code>)؛ (۲) اگر لایسنس فعال نباشد، پس از وارد کردن رمز درست هم پنجرهٔ فعال‌سازی باز می‌شود و تا فعال‌سازی وارد برنامه نمی‌شوید؛ (۳) اجرای دوبارهٔ برنامه با توکنِ به‌یادمانده هم از همین گیت می‌گذرد</td></tr>
<tr><td>🎨 <b>تم آبی</b></td>
<td>رنگ اصلی برنامه از فیروزه‌ای‌سبز به <b>آبی</b> تغییر کرد — سایدبار هم مانند اسکرین‌شات‌ها آبی تیره شد. همهٔ دکمه‌ها، آیکون‌ها و های‌لایت‌ها هماهنگ آبی می‌شوند</td></tr>
<tr><td>🖥 <b>داشبورد تمام‌صفحه</b></td>
<td>پنجرهٔ اصلی از این پس <b>تمام‌صفحه</b> باز می‌شود (با دکمهٔ بازگردانی ویندوز می‌توانید کوچکش کنید)</td></tr>
<tr><td>💥 <b>کرش دکمهٔ «بیمار جدید»</b></td>
<td>خطای <code>InvalidOperationException ... read-only property 'Allergies'</code> رفع شد — بایندینگ نادرست روی پراپرتی‌های فقط-خواندنی تگ‌ها اصلاح شد؛ فرم بیمار (هر ۵ تب: اصلی، بیمه، سوابق پزشکی، تماس اضطراری، اسناد) بدون خطا باز می‌شود</td></tr>
</table>
</div>
"""

SECTION_LICENSE_NOTE = """
<h2>۳) دربارهٔ لایسنس — رفتار جدید</h2>
<div class="card okbox">
<p>اگر روی سرور شما لایسنس فعال <b>نباشد</b>، پس از ورود موفق، پنجرهٔ «فعال‌سازی لایسنس» باز می‌شود:</p>
<ul>
<li><b>فعال‌سازی آنلاین:</b> کلید لایسنس را در همان پنجره وارد کنید و دکمه را بزنید — پس از موفقیت، پیام سبز می‌بینید، پنجره خودکار بسته می‌شود و ورود ادامه می‌یابد</li>
<li><b>فعال‌سازی آفلاین:</b> فایل <code>.lic</code> را از دکمهٔ بارگذاری انتخاب کنید</li>
<li>اگر پنجره را بدون فعال‌سازی ببندید، به صفحهٔ ورود برمی‌گردید و پیام «برای ورود به برنامه ابتدا لایسنس را فعال کنید» می‌بینید — برنامه بدون لایسنس فعال باز نمی‌شود</li>
<li>کلید لایسنس را ندارید؟ با پشتیبان محصول تماس بگیرید؛ فرم <b>کلید SCPRO-XXXXX-XXXXX-XXXXX</b> است</li>
</ul>
<p>💡 اگر شبکه یا سرور لایسنس موقتاً قطع باشد، برنامه شما را بیرون نمی‌کند (وضعیت «نامعلوم» مانع کار درمانگاه نمی‌شود) — فقط وقتی سرور <b>صریحاً</b> بگوید لایسنس نامعتبر است، فعال‌سازی خواسته می‌شود.</p>
</div>
"""

SECTION_STEPS = """
<h2>۴) نصب در ۶ قدم (حدود ۲ دقیقه)</h2>

<div class="step"><div class="stepnum">۱</div><div class="stepbody">
<b>ویژوال استودیو را کاملاً ببندید</b> (بستن کامل، نه کمینه‌کردن) — این مهم‌ترین قدم است.
اگر VS باز بماند ممکن است فایل قدیمی را از حافظهٔ خودش دوباره روی فایل جدید ذخیره کند و همان خطاهای جفت‌ناهماهنگ قبلی برگردد.
</div></div>

<div class="step"><div class="stepnum">۲</div><div class="stepbody">
<b>پوشهٔ سورس فعلی خود را باز کنید</b> — همان پوشه‌ای که سولوشن در آن است (مثلاً <code>E:\\Sahand_ClinicPro-2.0.1</code> یا هر نامی که دارد).
</div></div>

<div class="step"><div class="stepnum">۳</div><div class="stepbody">
فایل ZIP این بسته را باز کنید و <b>همهٔ محتوایش را انتخاب کنید</b> (<span class="kbd">Ctrl+A</span>) —
هم پوشه‌های <code>src</code> و <code>tests</code> را و هم فایل‌های ریشه (<code>Directory.Build.props</code>، <code>CHANGELOG.md</code>، <code>README.md</code>، <code>TODO-v2.0.1.md</code>).
</div></div>

<div class="step"><div class="stepnum">۴</div><div class="stepbody">
همه را روی <b>ریشهٔ پوشهٔ سورس</b> پیست کنید و در پنجرهٔ ویندوز <b>Replace the files</b> را بزنید
(اگر دربارهٔ ادغام پوشه‌ها پرسید: بله/برای همه).
</div></div>

<div class="step"><div class="stepnum">۵</div><div class="stepbody">
ویژوال استودیو را باز کنید، سولوشن را باز کنید و از منوی <b>Build → Rebuild Solution</b> بزنید
(نه Build معمولی — Rebuild). باید <b>صفر خطا</b> بگیرید.
</div></div>

<div class="step"><div class="stepnum">۶</div><div class="stepbody">
برنامه را اجرا کنید و سه اطمینان پایین را چک کنید.
</div></div>

<div class="card dangerbox">
<p>⚠️ اگر بیلد خطا داد، فایل‌ها ناهماهنگ کپی شده‌اند — قدم ۱ را جدی بگیرید (بستن VS)،
دوباره همهٔ بسته را Replace کنید و Rebuild بزنید. فایل <code>appsettings.json</code> (تنظیم SQL شما) و دیتابیس و رمز شما در این بسته نیست و دست نمی‌خورد.</p>
</div>
"""

SECTION_CONFIRM = """
<h2>۵) سه اطمینان پس از نصب</h2>
<div class="card okbox">
<ol>
<li>پانویس پنجرهٔ ورود باید <b>«نسخه ۲.۰.۴»</b> را نشان دهد ← یعنی بسته درست نصب شده</li>
<li>دکمهٔ «🔑 فعال‌سازی لایسنس» زیر دکمهٔ ورود را بزنید ← باید <b>پنجرهٔ فعال‌سازی</b> باز شود (نه هیچ واکنشی نباشد)</li>
<li>پس از ورود: سایدبار باید <b>آبی تیره</b> باشد و پنجره <b>تمام‌صفحه</b> باز شود؛ از منوی «بیماران» دکمهٔ «بیمار جدید» را بزنید ← فرم بدون خطا باز می‌شود</li>
</ol>
</div>
"""

SECTION_FILES = """
<h2>۶) فهرست فایل‌های این بسته</h2>
<div class="card">
<p class="flist">__FILELIST__</p>
<p style="font-size:13px;color:var(--mut)">نشانه‌گذاری: <span class="tagA">A = فایل جدید</span> | <span class="tagM">M = ویرایش‌شده</span> — بدون هیچ فایل حذف‌شده</p>
</div>
"""

GUIDE_FOOT = """
<footer>
ساخته‌شده در چرخه هفدهم — گیت کلون <span class="mono">pranses2011/Sahand_ClinicPro</span> · رلیز v2.0.4
</footer>
</div>
</body>
</html>
"""


def build_guide(total: int, files: list) -> str:
    """مونتاژ راهنمای HTML — بدون f-string (درس چرخه ۱۵)"""
    groups = {}
    for status, path in files:
        groups.setdefault(group_title(path), []).append((status, path))

    lines = []
    for group in groups:
        lines.append("<div><b style='color:#0D47A1'>" + group + "</b></div>")
        for status, path in files:
            if group_title(path) == group:
                tag = '<span class="tagA">A</span>' if status == "A" else '<span class="tagM">M</span>'
                lines.append("<div>" + tag + " " + path.replace("\\", "/") + "</div>")

    html = (GUIDE_HEAD
            + SECTION_WHAT
            + SECTION_FIXES
            + SECTION_LICENSE_NOTE
            + SECTION_STEPS
            + SECTION_CONFIRM
            + SECTION_FILES
            + GUIDE_FOOT)
    html = html.replace("__TOTAL__", str(total))
    html = html.replace("__FILELIST__", "".join(lines))
    return html


def main() -> int:
    files = changed_files()

    # ── گارد ۱: بدون فایل حذف‌شده ──
    deleted = [p for s, p in files if s.startswith("D")]
    if deleted:
        print("⛔ توقف — فایل حذف‌شده در محدوده (نیازمند بازبینی):", deleted)
        return 1

    # ── گارد ۲: appsettings هرگز در بسته نیست ──
    unsafe = [p for _, p in files if "appsettings" in p.lower()]
    if unsafe:
        print("⛔ توقف — فایل تنظیمات در بسته نباید باشد:", unsafe)
        return 1

    # ── گارد ۳: نشانه‌های سلامت در HEAD ──
    for path, marker, desc in COHERENCE_CHECKS:
        full = REPO / path
        if not full.exists():
            print("⛔ توقف — فایل کلیدی یافت نشد:", path)
            return 1
        content = full.read_text(encoding="utf-8-sig", errors="replace")
        if marker not in content:
            print("⛔ توقف — نشانهٔ سلامت یافت نشد:", desc, "—", path)
            return 1

    # ── گارد ۴: ریشهٔ وارونگی رمز باید حذف شده باشد ──
    for path, marker, desc in ABSENCE_CHECKS:
        full = REPO / path
        content = full.read_text(encoding="utf-8-sig", errors="replace")
        if marker in content:
            print("⛔ توقف — نشانهٔ ممنوع هنوز موجود است:", desc, "—", path)
            return 1

    out = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else (REPO / OUT_NAME)
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for status, path in files:
            zf.write(REPO / path, path.replace("\\", "/"))
        zf.writestr(GUIDE_NAME, build_guide(len(files), files))
        manifest = ("# فایل‌های تغییرکرده v2.0.3 → v2.0.4 (چرخه هفدهم — گزارش یازدهم مالک)\n"
                    "# A = جدید | M = ویرایش | بدون حذف\n")
        manifest += "\n".join(f"{s}\t{p.replace(chr(92), '/')}" for s, p in files)
        manifest += f"\n\n# جمع: {len(files)} فایل ({sum(1 for s, _ in files if s == 'A')} جدید + {sum(1 for s, _ in files if s == 'M')} ویرایش)\n"
        zf.writestr(MANIFEST_NAME, manifest)

    size_kb = out.stat().st_size / 1024
    print(f"✅ بسته ساخته شد: {out}  ({size_kb:.1f} KB)")
    print(f"   {len(files)} فایل ({sum(1 for s, _ in files if s == 'A')} جدید + {sum(1 for s, _ in files if s == 'M')} ویرایش) + راهنما + مانیفست")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
