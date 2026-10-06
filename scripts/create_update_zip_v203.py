#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📦 ساخت بستهٔ آپدیت تجمعی v2.0.1 → v2.0.3 — چرخه شانزدهم (گزارش دهم مالک)
همهٔ فایل‌های تغییرکرده از تگ v2.0.1 تا HEAD (شامل هرچه در بستهٔ 2.0.2 و
وصلهٔ CS0103 بود + رفع کرش داشبورد)، با همان ساختار پوشه‌ای مخزن، به‌همراه
راهنمای فارسی HTML و مانیفست متنی فایل‌ها.

چرا تجمعی؟ سه گزارش پیاپی مالک نشانهٔ «جایگزینی ناقص فایل‌ها روی دستگاه» بود
(CS0103 جفت ناهماهنگ + دیدن دوبارهٔ نشانه‌های رفع‌شده) → این بسته یک‌جا همهٔ
فایل‌های لازم از 2.0.1 را می‌دهد تا یک Replace کامل، دستگاه را سازگار کند.

نحوهٔ استفاده:
    python scripts/create_update_zip_v203.py [مسیر-خروجی.zip]

پیش‌فرض خروجی: ریشهٔ مخزن / SahandClinicPro-Update-2.0.1-to-2.0.3.zip

ایمنی‌ها:
- توقف اگر فایل حذف‌شده (D) در محدوده باشد
- توقف اگر appsettings در فهرست باشد (تنظیم SQL مالک هرگز بازنویسی نشود)
- توقف اگر نشانه‌های سلامت فایل‌های کلیدی در محتوای فعلی نباشد (درس CS0103:
  جفت‌های XAML/کد-پشت و رفع‌های همین نسخه باید همیشه هماهنگ از مخزن خارج شوند)
⚠️ درس چرخه ۱۵: HTML حاوی آکولاد است — بدون f-string، فقط الحاق ساده و .replace()
"""
import pathlib
import subprocess
import sys
import zipfile

REPO = pathlib.Path(__file__).resolve().parents[1]
TAG = "v2.0.1"
OUT_NAME = "SahandClinicPro-Update-2.0.1-to-2.0.3.zip"
GUIDE_NAME = "Update-Guide-2.0.3.html"
MANIFEST_NAME = "changed-files.txt"

# گارد انسجام — نشانه‌های سلامت که باید در محتوای فعلی HEAD موجود باشند
COHERENCE_CHECKS = [
    ("src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml",
     'x:Key="BoolToVis"', "رفع کرش داشبورد (مبدل BoolToVis)"),
    ("src/Sahand.ClinicPro.Desktop/Views/LoginWindow.xaml",
     'FlowDirection="LeftToRight"', "کادر چپ‌به‌راست پنجرهٔ ورود"),
    ("src/Sahand.ClinicPro.Desktop/Views/ChangePasswordWindow.xaml",
     "CurrentPasswordRevealBox", "جفت XAML/کد-پشت پنجرهٔ تغییر رمز"),
    ("Directory.Build.props",
     "<Version>2.0.3</Version>", "نسخهٔ 2.0.3"),
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


def fa_digits(text: str) -> str:
    """تبدیل ارقام ASCII به فارسی برای هماهنگی متن فارسی راهنما"""
    table = str.maketrans("0123456789", "۰۱۲۳۴۵۶۷۸۹")
    return text.translate(table)


def group_title(path: str) -> str:
    """گروه‌بندی فارسی برای نمایش در راهنما"""
    p = path.replace("\\", "/")
    if p.startswith("src/Sahand.ClinicPro.Desktop/Assets/Fonts/"):
        return "فونت‌های جاسازی‌شدهٔ وزیرمتن (TTF)"
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
    if p.startswith("src/Sahand.ClinicPro.API/"):
        return "سمت سرور (API)"
    if p.startswith("src/Sahand.ClinicPro.Infrastructure/"):
        return "زیرساخت و مهاجرت‌های دیتابیس"
    if p.startswith("tests/"):
        return "پروژه‌های تست"
    if p.startswith("docs/"):
        return "مستندات و راهنماها"
    if p.startswith("scripts/"):
        return "اسکریپت‌های پشتیبانی"
    return "فایل‌های ریشهٔ مخزن"


CSS = """
:root{--ink:#1c2b33;--mut:#5a6b73;--line:#dfe6e9;--brand:#0d5c46;--brand2:#0a4636;
--gold:#b98a2f;--bg:#f6f8f7;--card:#ffffff;--ok:#0c7a4b;--warn:#9a6700;--bad:#b3372f}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--ink);
font-family:"Vazirmatn","Segoe UI",Tahoma,Arial,sans-serif;line-height:1.9;font-size:15px}
.wrap{max-width:880px;margin:0 auto;padding:24px 20px 64px}
header.hero{background:linear-gradient(135deg,var(--brand) 0%,var(--brand2) 100%);
color:#fff;border-radius:16px;padding:28px 26px;margin-bottom:26px}
header.hero h1{margin:0 0 6px;font-size:24px}
header.hero p{margin:0;opacity:.92;font-size:14px}
.badge{display:inline-block;background:rgba(255,255,255,.16);border:1px solid rgba(255,255,255,.35);
border-radius:999px;padding:2px 14px;font-size:13px;margin-top:10px}
h2{font-size:19px;color:var(--brand2);border-right:5px solid var(--brand);
padding-right:12px;margin:34px 0 12px}
h3{font-size:16px;margin:20px 0 8px;color:var(--brand2)}
.card{background:var(--card);border:1px solid var(--line);border-radius:12px;
padding:18px 20px;margin:12px 0}
table{width:100%;border-collapse:collapse;font-size:13.5px}
th,td{border:1px solid var(--line);padding:7px 10px;text-align:right;vertical-align:top}
th{background:#eef3f1;color:var(--brand2)}
ul,ol{padding-right:22px;padding-left:0;margin:8px 0}
li{margin:5px 0}
code,.mono{font-family:Consolas,"Courier New",monospace;font-size:13px;
background:#eef1f0;border-radius:5px;padding:1px 6px;direction:ltr;display:inline-block}
pre{background:#12312a;color:#d7ece4;border-radius:10px;padding:14px 16px;
overflow-x:auto;direction:ltr;text-align:left;font-family:Consolas,monospace;font-size:13px}
.note{border-right:5px solid var(--gold);background:#fdf8ee}
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
color:#33474f;margin:6px 0}
.flist div{padding:1px 4px;border-bottom:1px dashed #e7ecee}
.tagA{color:var(--ok);font-weight:700}
.tagM{color:var(--gold);font-weight:700}
footer{margin-top:40px;color:var(--mut);font-size:12.5px;text-align:center}
.big{font-size:17px}
.center{text-align:center}
.kbd{background:#e8ecec;border:1px solid #cbd5d9;border-bottom-width:3px;border-radius:6px;
padding:1px 8px;font-size:13px;font-family:inherit}
"""

GUIDE_HEAD = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>راهنمای بستهٔ آپدیت — سامانهٔ درمانگاه سهند ۲.۰.۳</title>
<style>""" + CSS + """</style>
</head>
<body>
<div class="wrap">

<header class="hero">
<h1>📦 بستهٔ آپدیت تجمعی نسخهٔ ۲.۰.۱ ← ۲.۰.۳</h1>
<p>سامانهٔ جامع درمانگاه سهند — همهٔ فایل‌های تغییرکرده در یک بسته، با همان ساختار پوشه‌ها</p>
<span class="badge">__TOTAL__ فایل در این بسته</span>
<span class="badge">تاریخ انتشار: ۷ اکتبر ۲۰۲۶</span>
</header>
"""

SECTION_WHAT = """
<h2>۱) این بسته چیست؟</h2>
<div class="card">
<p>این بستهٔ آپدیت <b>تجمعی</b> است: <b>همهٔ فایل‌هایی که از نسخهٔ ۲.۰.۱ تا ۲.۰.۳ تغییر کرده‌اند</b> — با همان ساختار پوشه‌ای مخزن.
هرچه در بستهٔ قبلی (۲.۰.۲)، وصلهٔ کوچک پنجرهٔ تغییر رمز و رفع امروز بود، همه یک‌جا در همین بسته جمع شده‌اند.</p>
<p>اگر هنگام نصب بسته‌های قبلی فایلی جا افتاده یا ویژوال استودیو فایلی را به‌حالت قدیمی برگردانده بود،
<b>این بسته آن را هم درست می‌کند</b> — کافی است <b>کل محتوای بسته</b> را جایگزین کنید، نه بخشی از آن را.</p>
</div>

<div class="card note">
<p>💡 <b>مهم‌ترین نکتهٔ این نصب:</b> همهٔ فایل‌های داخل بسته را <b>با هم</b> جایگزین کنید (۶ قدم پایین).
جایگزینیِ فقط چند فایلِ منتخب، علت بروز خطاهای اخیر بوده است — چون جفتِ فایل‌های وابسته به هم
(مثل XAML و کدِ پشتِ آن) باید همیشه با هم عوض شوند.</p>
</div>
"""

SECTION_FIXES = """
<h2>۲) چه چیزهایی رفع شده؟</h2>

<div class="card">
<table>
<tr><th style="width:34%">موضوع</th><th>توضیح</th></tr>
<tr><td>📊 <b>کرش باز شدن داشبورد (جدید این نسخه)</b></td>
<td>پس از تغییر رمز، باز شدن داشبورد با خطای <code>Cannot find resource named 'BoolToVis'</code> می‌ریخت — مبدل گمشده به داشبورد اضافه شد و از این پس داشبورد بدون خطا باز می‌شود — گزارش دهم</td></tr>
<tr><td>🔐 <b>رمز عبور شما عوض شده است</b></td>
<td>اگر در دفعهٔ قبل پنجرهٔ تغییر رمز با موفقیت ثبت شد، رمز قدیمی <code>Admin@123</code> دیگر معتبر نیست — با <b>رمز جدید</b> وارد شوید — گزارش دهم</td></tr>
<tr><td>👁 <b>نمایش درست رمز عبور</b></td>
<td>کادرهای نام کاربری/رمز چپ‌به‌راست و وسط‌چین شدند؛ آیکون چشم برای هر ۴ کادر (ورود + سه کادر تغییر رمز) در ستون کناریِ <b>بیرون از کادر</b> است — گزارش‌های پنجم و هفتم</td></tr>
<tr><td>🔁 <b>جریان تغییر رمز اجباری</b></td>
<td>پس از اولین ورود، پنجرهٔ تغییر رمز باز می‌شود و بدون ثبت موفق قابل دورزدن نیست — گزارش پنجم</td></tr>
<tr><td>💥 <b>خطای ۵۰۰ لاگین</b></td>
<td>ترکیب ناسازگار «تلاش مجدد خودکار + تراکنش دستی» در EF Core حذف شد — گزارش ششم</td></tr>
<tr><td>✋ <b>«اطلاعات ارسالی نامعتبر است»</b></td>
<td>payload سه‌فیلدی (رمز فعلی + جدید + تکرار) ارسال می‌شود + ریشهٔ واقعی خطا نمایش داده می‌شود — گزارش هفتم</td></tr>
<tr><td>🔍 <b>خطای 404 کل ارتباط</b></td>
<td>آدرس‌دهی پایهٔ HttpClient درست شد + پاکت استاندارد پاسخ API باز می‌شود — گزارش پنجم</td></tr>
<tr><td>🔤 <b>فونت وزیرمتن بومی</b></td>
<td>۴ وزن فونت داخل exe جاسازی شد + هر ۶۷ ارجاع فرم‌ها وصل شد + مرکز آموزش وب‌فونت گرفت — گزارش هشتم</td></tr>
<tr><td>🗄 <b>رفع‌های دیتابیس و سرور</b></td>
<td>فیلتر ایندکس داروخانه (Error 10735) + پورت ثابت 5000 سرور + راهنمای SQL تقویت‌شده — گزارش‌های دوم تا چهارم</td></tr>
</table>
</div>
"""

SECTION_INSTALL = """
<h2>۳) نصب آپدیت — ۶ قدم</h2>

<div class="step"><div class="stepnum">۱</div><div class="stepbody">
<b>ویژوال استودیو را کاملاً ببندید</b> (و اگر سرور API یا برنامه در حال اجراست، آن‌ها را هم ببندید) تا فایل‌ها قفل نباشند و بافر قدیمی روی فایل جدید ذخیره نشود.</div></div>

<div class="step"><div class="stepnum">۲</div><div class="stepbody">
<b>فایل ZIP را باز کنید</b> (دابل‌کلیک در ویندوز) — داخل آن، فایل‌ها با ساختار پوشه‌ای
<code>src\\...</code> دیده می‌شوند. فایل راهنما (<code>__GUIDE__</code>) و فهرست فایل‌ها (<code>__MANIFEST__</code>) هم آن‌جا هستند.</div></div>

<div class="step"><div class="stepnum">۳</div><div class="stepbody">
<b>همه را انتخاب و کپی کنید</b> (<span class="kbd">Ctrl+A</span> سپس <span class="kbd">Ctrl+C</span>) و در پوشهٔ سورس خود
<code>E:\\Sahand_ClinicPro-2.0.1</code> پیست کنید (<span class="kbd">Ctrl+V</span>) — همان‌جایی که فایل
<code>Sahand.ClinicPro.sln</code> قرار دارد.</div></div>

<div class="step"><div class="stepnum">۴</div><div class="stepbody">
<b>پیام جایگزینی را تأیید کنید</b> — ویندوز می‌پرسد «Replace or Skip Files?»؛ روی
<b>«Replace the files in the destination»</b> کلیک کنید (برای پوشه‌ها هم Merge بزنید).
هیچ فایلی را از کپی‌شدن حذف نکنید.</div></div>

<div class="step"><div class="stepnum">۵</div><div class="stepbody">
<b>سولوشن را باز و Rebuild کنید</b> — منوی <span class="kbd">Build</span> ←
<span class="kbd">Rebuild Solution</span>. دفعهٔ اول کمی طولانی‌تر می‌کشد؛ عادی است.</div></div>

<div class="step"><div class="stepnum">۶</div><div class="stepbody">
<b>اجرا و ورود</b> — ابتدا سرور API را اجرا کنید (باید بنویسد <code>Now listening on: http://localhost:5000</code>)،
سپس پروژهٔ دسکتاپ را اجرا کنید و با <code>admin</code> و <b>رمز جدیدتان</b> (اگر هنوز عوضش نکرده‌اید: <code>Admin@123</code>) وارد شوید ← داشبورد.</div></div>
"""

SECTION_VERIFY = """
<h2>۴) پس از نصب — سه اطمینان</h2>

<div class="card okbox">
<p>✅ <b>پانویس پنجرهٔ ورود باید «نسخه ۲.۰.۳» را نشان دهد.</b>
این سریع‌ترین راه تشخیص نصب موفق است؛ اگر عدد قدیمی‌تر دیدید، یعنی فایل‌ها هنوز کامل جایگزین نشده‌اند — بسته را دوباره و کامل نصب کنید.</p></div>

<div class="card okbox">
<p>✅ <b>داشبورد باید بدون هیچ خطایی باز شود</b> — خطای <code>BoolToVis</code> ریشه‌کن شده است.
اگر باز هم دیدید، یعنی فایل <code>DashboardView.xaml</code> روی دستگاه شما قدیمی است.</p></div>

<div class="card okbox">
<p>✅ <b>کادرهای رمز باید چپ‌به‌راست و وسط‌چین باشند</b> و آیکون چشم همهٔ کادرها بیرون از کادر، کنار آن باشد.
اگر رمز را وارونه دیدید (مثل <code>321@nimdA</code>) یعنی فایل‌های قدیمی روی دستگاه مانده‌اند — بسته را دوباره و کامل نصب کنید.</p></div>
"""

SECTION_REASSURE = """
<h2>۵) نگران نباشید — سه نکتهٔ مهم</h2>

<div class="card okbox">
<p>✅ <b>تنظیمات SQL شما دست نمی‌خورد.</b> فایل <code>appsettings.json</code> سرور در این بسته نیست؛
نام نمونهٔ SQL Server که خودتان تنظیم کرده‌اید، همان می‌ماند.</p></div>

<div class="card okbox">
<p>✅ <b>دیتابیس ساخته‌شده و رمز تغییرکردهٔ شما معتبر می‌مانند.</b> هیچ مهاجرت جدیدی نسبت به نسخهٔ ۲.۰.۲ اضافه نشده؛
جداول، داده‌ها و رمز جدیدِ ثبت‌شدهٔ شما سر جای خود هستند.</p></div>

<div class="card okbox">
<p>✅ <b>اگر قبلاً فایلی را دستی ویرایش کرده بودید</b> (مثل فیلتر ایندکس یا <code>launchSettings.json</code>)،
جایگزینی مشکلی ایجاد نمی‌کند — این بسته همان رفع‌ها را در نسخهٔ نهایی خودش دارد.</p></div>
"""

SECTION_LOCK = """
<h2>۶) رمز عبور و قفل حساب</h2>

<div class="card note">
<p>💡 <b>رمز فعلی شما چیست؟</b> اگر در دفعهٔ قبل پنجرهٔ تغییر رمز با موفقیت ثبت شد (و بعد داشبورد خطا داد)،
رمز حساب شما همان <b>رمز جدید</b> است — ورود با <code>Admin@123</code> پیام «نامعتبر بودن رمز» می‌دهد که طبیعی است.</p></div>

<div class="card dangerbox">
<p>🔒 پنج تلاش ناموفق پیاپی، حساب را ۳۰ دقیقه قفل می‌کند. اگر موقع آزمایش‌های قبلی حساب قفل شده،
در SSMS این دستور را روی دیتابیس برنامه اجرا کنید:</p>
<pre>UPDATE Users SET FailedLoginAttempts = 0, LockoutEnd = NULL WHERE Username = 'admin';</pre>
<p>سپس بدون انتظار دوباره ورود را امتحان کنید.</p></div>

<div class="card note">
<p>💡 <b>نکتهٔ تایپ رمز:</b> نمایش رمز در نسخهٔ جدید درست است (چپ‌به‌راست). رمز را <b>معمولی و همان‌طور که هست</b> تایپ کنید؛
هرگز برای «درست‌کردن» نمایش، وارونه تایپ نکنید.</p></div>
"""

SECTION_FILES_HEAD = """
<h2>۷) فهرست کامل فایل‌های این بسته</h2>
<p class="big">در مجموع <b>__TOTAL__ فایل</b>: <span class="tagA">__NADD__ فایل جدید</span> و
<span class="tagM">__NMOD__ فایل ویرایش‌شده</span>. فهرست کامل با وضعیت هر فایل در <code>__MANIFEST__</code> هم موجود است.</p>
"""

SECTION_FILES_TAIL = """
<footer>
سامانهٔ جامع مدیریت درمانگاه سهند — نسخهٔ ۲.۰.۳<br>
این راهنما داخل بستهٔ آپدیت قرار دارد؛ برای تاریخچهٔ کامل تغییرات، فایل <code>CHANGELOG.md</code> مخزن را ببینید.
</footer>

</div>
</body>
</html>
"""


def build_guide(files, counts):
    """سرهم‌کردن راهنمای HTML — بدون f-string (درس تداخل آکولاد)"""
    parts = [GUIDE_HEAD, SECTION_WHAT, SECTION_FIXES, SECTION_INSTALL,
             SECTION_VERIFY, SECTION_REASSURE, SECTION_LOCK]

    head = SECTION_FILES_HEAD
    head = head.replace("__TOTAL__", fa_digits(str(counts["total"])))
    head = head.replace("__NADD__", fa_digits(str(counts["added"])))
    head = head.replace("__NMOD__", fa_digits(str(counts["modified"])))
    head = head.replace("__MANIFEST__", MANIFEST_NAME)
    parts.append(head)

    # گروه‌بندی فارسی
    groups = {}
    for status, path in files:
        groups.setdefault(group_title(path), []).append((status, path))
    for title in groups:
        items = groups[title]
        block = ['<details open><summary>' + title + " — " + fa_digits(str(len(items))) + " فایل</summary>", '<div class="flist">']
        for status, path in items:
            cls = "tagA" if status == "A" else "tagM"
            mark = "جدید" if status == "A" else "ویرایش"
            block.append('<div><span class="' + cls + '">[' + mark + ']</span> ' + path + "</div>")
        block.append("</div></details>")
        parts.append("\n".join(block))
    parts.append(SECTION_FILES_TAIL)

    html = "\n".join(parts)
    html = html.replace("__TOTAL__", fa_digits(str(counts["total"])))
    html = html.replace("__NADD__", fa_digits(str(counts["added"])))
    html = html.replace("__NMOD__", fa_digits(str(counts["modified"])))
    html = html.replace("__GUIDE__", GUIDE_NAME)
    html = html.replace("__MANIFEST__", MANIFEST_NAME)
    return html


def build_manifest(files, counts):
    lines = [
        "SahandClinicPro — Cumulative Update 2.0.1 -> 2.0.3",
        "===================================================",
        "Supersedes: Update-2.0.1-to-2.0.2.zip + Hotfix-ChangePasswordWindow-2.0.2.zip",
        "Total: " + str(counts["total"]) + " files (" + str(counts["added"]) + " added, "
        + str(counts["modified"]) + " modified, " + str(counts["deleted"]) + " deleted)",
        "",
        "Legend: A = new file, M = modified file",
        "",
    ]
    for status, path in files:
        mark = "A" if status == "A" else "M"
        lines.append("[" + mark + "] " + path)
    lines.append("")
    return "\n".join(lines)


def main() -> int:
    # گارد انسجام — قبل از بسته‌بندی، نشانه‌های سلامت در محتوای فعلی بررسی شوند
    for path, marker, desc in COHERENCE_CHECKS:
        src = REPO / path
        if not src.is_file():
            print("⛔ فایل کلیدی پیدا نشد: " + path)
            return 1
        if marker not in src.read_text(encoding="utf-8-sig"):
            print("⛔ نشانهٔ سلامت یافت نشد در " + path + " → " + desc)
            print("   مورد جستجو: " + marker)
            return 1
        print("✔ انسجام: " + desc)

    files = changed_files()
    added = [p for s, p in files if s == "A"]
    modified = [p for s, p in files if s == "M"]
    deleted = [p for s, p in files if s == "D"]
    if deleted:
        print("⛔ فایل حذف‌شده در محدوده هست — بازبینی دستی لازم است:")
        for p in deleted:
            print("   D " + p)
        return 1
    forbidden = [p for _, p in files if "appsettings" in p.lower()]
    if forbidden:
        print("⛔ appsettings در بسته نباید باشد:", forbidden)
        return 1

    counts = {"total": len(files), "added": len(added), "modified": len(modified), "deleted": len(deleted)}
    print("📦 بستهٔ آپدیت تجمعی: " + str(counts["total"]) + " فایل (" + str(counts["added"]) + " جدید + "
          + str(counts["modified"]) + " ویرایش)")

    out_path = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else (REPO / OUT_NAME)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    guide_html = build_guide(files, counts)
    manifest_txt = build_manifest(files, counts)

    with zipfile.ZipFile(out_path, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        # ابتدا راهنما و مانیفست در ریشهٔ بسته
        zf.writestr(GUIDE_NAME, guide_html)
        zf.writestr(MANIFEST_NAME, manifest_txt)
        # سپس فایل‌های تغییرکرده با ساختار مخزن
        for status, path in files:
            src = REPO / path
            if not src.is_file():
                print("⛔ فایل در درخت کار نیست: " + path)
                return 1
            zf.write(src, path)

    # راستی‌آزمایی
    with zipfile.ZipFile(out_path) as zf:
        names = zf.namelist()
        bad = zf.testzip()
        if bad is not None:
            print("⛔ خرابی CRC در: " + bad)
            return 1
        missing = [p for _, p in files if p not in names]
        if missing:
            print("⛔ فایل جاافتاده در ZIP:", missing[:5])
            return 1
        # نشانهٔ رفع داشبورد باید داخل خود بسته هم باشد (درس جفت ناهماهنگ CS0103)
        dash = zf.read("src/Sahand.ClinicPro.Desktop/Views/DashboardView.xaml").decode("utf-8-sig")
        if 'x:Key="BoolToVis"' not in dash:
            print("⛔ رفع داشبورد داخل بسته نیست!")
            return 1
        total_size = sum(i.file_size for i in zf.infolist())
        print("✔ راستی‌آزمایی: " + str(len(names)) + " ورودی (۲ فایل راهنما/مانیفست + " + str(counts["total"]) + " فایل)")
        print("  حجم باز شده: " + str(round(total_size / 1024)) + " KB")
    print("💾 خروجی: " + str(out_path) + " (" + str(round(out_path.stat().st_size / 1024)) + " KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
