#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📘 مولد بروشور v1.7.0 — درمانگاه سهند
تور تصویری کامل هر ۵۲ فرم + معرفی قابلیت‌ها + جدول ماژول‌ها + تماس واقعی
خروجی: docs/guides/brochure.html  (A4 افقی — اسلایدی)
"""
import pathlib

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
OUT = REPO / "docs" / "guides" / "brochure.html"

# ═══════════ ۵۲ فرم: (شماره، فایل، آیکون، عنوان، توضیح، بخش) ═══════════
FORMS = [
    # ── بخش ۱: پذیرش و بیمار ──
    (1,  "01-dashboard",       "📊", "داشبورد مدیریتی",      "نمای ۳۶۰ درجه امروز درمانگاه: بیماران، صف زنده، درآمد و هشدارها در یک نگاه", "پذیرش و بیمار"),
    (2,  "02-patients",        "🧑‍🤝‍🧑", "مدیریت بیماران",         "پرونده الکترونیک با کد ملی اعتبارسنجی‌شده (ضد جعل)، بیمه و سابقه کامل مراجعات", "پذیرش و بیمار"),
    (3,  "04-appointments",    "🗓", "نوبت‌دهی هوشمند",       "تقویم با ظرفیت لحظه‌ای پزشکان و کانال‌های ثبت حضوری، تلفنی و اینترنتی", "پذیرش و بیمار"),
    (4,  "05-receptions",      "🛎", "پذیرش و صف زنده",       "فراخوان بلادرنگ بیمار روی همه رایانه‌ها و تلویزیون سالن — با SignalR", "پذیرش و بیمار"),
    (5,  "34-patient-form-win",     "📝", "فرم ثبت بیمار",    "فرم ثبت با اعتبارسنجی الگوریتمی کد ملی و الگوی موبایل — کد جعلی پذیرفته نمی‌شود", "پذیرش و بیمار"),
    (6,  "35-patient-detail-win",   "🗂", "پرونده بیمار",     "سابقه کامل مراجعات، نسخه‌ها، آزمایش‌ها و صورتحساب‌های هر بیمار در یک پنجره", "پذیرش و بیمار"),
    (7,  "36-appointment-form-win", "🗓", "فرم ثبت نوبت",     "انتخاب پزشک و بازه ساعت با نمایش ظرفیت آزاد لحظه‌ای", "پذیرش و بیمار"),
    (8,  "37-reception-form-win",   "🛎", "پذیرش حضوری",      "شماره صف خودکار، بیعانه و روش پرداخت — بلافاصله روی صف سالن ظاهر می‌شود", "پذیرش و بیمار"),
    # ── بخش ۲: درمان و نسخه ──
    (9,  "06-visits",          "📋", "ویزیت و معاینه",        "ثبت علائم حیاتی، شرح حال و تشخیص استاندارد ICD-10 برای هر ویزیت", "درمان و نسخه"),
    (10, "07-prescriptions",   "💊", "نسخه‌نویسی هوشمند",     "تداخل‌سنجی خودکار داروها و پایش حساسیت‌های بیمار هنگام تجویز", "درمان و نسخه"),
    (11, "08-emr-advanced",    "❤️", "پرونده بالینی پیشرفته", "نمودار رشد WHO با z-score، سابقه خانوادگی ۱۲ خویشاوندی و طرح مراقبت مزمن", "درمان و نسخه"),
    (12, "26-erx",             "☁️", "نسخه الکترونیک",        "امضای دیجیتال و ارسال مستقیم نسخه به سامانه ملی و داروخانه‌ها", "درمان و نسخه"),
    (13, "38-visit-window-win",     "📋", "پنجره ثبت ویزیت",  "فرم معاینه با هشدار خودکار حساسیت‌های دارویی بیمار", "درمان و نسخه"),
    (14, "39-prescription-form-win", "💊", "پنجره نسخه‌نویسی", "اقلام نسخه با بررسی لحظه‌ای تداخل و منع مصرف در بارداری", "درمان و نسخه"),
    (15, "40-prescription-detail-win", "🔍", "مشاهده نسخه",   "جزئیات کامل نسخه صادره + وضعیت ارسال الکترونیک و چاپ", "درمان و نسخه"),
    (16, "03-doctors",         "🩺", "پزشکان و تخصص‌ها",      "مدیریت پزشکان، شیفت‌ها، حق‌الویزیت و وضعیت حضور", "درمان و نسخه"),
    (17, "41-doctor-form-win", "🩺", "فرم ثبت پزشک",          "ثبت تخصص، شماره نظام پزشکی، شیفت و حق‌الویزیت", "درمان و نسخه"),
    (18, "51-doctor-performance-win", "📈", "عملکرد پزشک",    "تحلیل ویزیت، درآمد، رضایت بیمار و روند هفتگی هر پزشک", "درمان و نسخه"),
    # ── بخش ۳: پاراکلینیک و داروخانه ──
    (19, "14-paraclinical",    "🧪", "مرکز پاراکلینیک",       "فرمان واحد آزمایشگاه و تصویربرداری: سفارش، رزرو و تحویل نتیجه", "پاراکلینیک و داروخانه"),
    (20, "15-lab",             "🔬", "آزمایشگاه",             "پانل‌های آزمایش با زمان پاسخ (FPR) و پیامک خودکار آماده‌شدن نتیجه", "پاراکلینیک و داروخانه"),
    (21, "16-pharmacy",        "💊", "داروخانه و انبار",      "مدیریت موجودی با حد بحرانی، هشدار انقضا و گردش انبار", "پاراکلینیک و داروخانه"),
    (22, "17-stocktaking",     "📦", "انبارگردانی",           "شمارش دوره‌ای با ثبت اختلاف کمبود/اضافه و ارزش‌گذاری", "پاراکلینیک و داروخانه"),
    (23, "46-lab-order-form-win", "🧫", "فرم سفارش آزمایش",  "انتخاب پانل‌ها و فوریت نمونه‌برداری", "پاراکلینیک و داروخانه"),
    (24, "47-lab-result-window-win", "🔬", "ثبت نتیجه آزمایش", "درج نتایج با بازه نرمال و نشان‌گذاری خودکار بالا/پایین", "پاراکلینیک و داروخانه"),
    (25, "48-stockin-form-win", "📦", "رسید ورود انبار",      "ثبت محموله با باتچ‌نامبر و تاریخ انقضا", "پاراکلینیک و داروخانه"),
    (26, "50-dicom-viewer-win", "🩻", "نمایشگر DICOM",        "مشاهده تصاویر رادیولوژی بدون نرم‌افزار جانبی — پنجره/سطح تعاملی", "پاراکلینیک و داروخانه"),
    # ── بخش ۴: مالی و حسابداری ──
    (27, "09-billing",         "💵", "مالی و صندوق",          "فاکتور با محاسبه خودکار سهم بیمه، کارتخوان و بستن روزانه صندوق", "مالی و حسابداری"),
    (28, "10-accounting",      "📒", "حسابداری دوبل",         "هر فاکتور سند حسابداری خودکار صادر می‌کند — تراز آزمایشی همیشه بسته", "مالی و حسابداری"),
    (29, "11-payroll",         "🧾", "فیش حقوقی پرسنل",       "قواعد ۱۴۰۴ + حقوق درصدی از سود خدمات انجامی خود پرسنل (نه کل فروش) با بیمه ۷٪ و مالیات پلکانی", "مالی و حسابداری"),
    (30, "13-insurance",       "🛡", "بیمه و مطالبات",        "ارسال تراکنش به بیمه‌ها و پیگیری دوره‌ای مطالبات با بازارسال خودکار", "مالی و حسابداری"),
    (31, "43-invoice-form-win", "💵", "صدور فاکتور",          "اقلام خدمت با تعرفه و تفکیک سهم بیمه/بیمار", "مالی و حسابداری"),
    (32, "44-payment-window-win", "💰", "دریافت وجه",         "تسویه بدهی با صدور خودکار سند دوبل", "مالی و حسابداری"),
    (33, "45-payslip-window-win", "🧾", "فیش حقوقی",          "جدول مزایا (شامل حقوق درصدی از سود) و کسورات با خالص پرداختی — آماده چاپ امضادار", "مالی و حسابداری"),
    # ── بخش ۵: اداری و منابع انسانی ──
    (34, "23-personnel",       "👔", "مدیریت پرسنل",          "پرونده کامل پرسنل: قرارداد، سابقه، شعبه و وضعیت اشتغال", "اداری و منابع انسانی"),
    (35, "12-attendance",      "🕐", "حضور و غیاب",           "دریافت خودکار ضربه‌ها از دستگاه ZKTeco/Hikvision و تطبیق با شیفت", "اداری و منابع انسانی"),
    (36, "42-employee-form-win", "👔", "فرم ثبت پرسنل",       "کد ملی، قرارداد، بیمه + حقوق پایه و درصد حقوق از سود خدمات انجامی خود پرسنل", "اداری و منابع انسانی"),
    (37, "27-branches",        "🏢", "مدیریت شعبه‌ها",        "مقایسه عملکرد شعبه مرکزی و نمایندگی‌ها با آمار زنده", "اداری و منابع انسانی"),
    (38, "25-sepas",           "🏦", "سامانه سپاس",           "ارسال تراکنش، استعلام کد رهگیری و بازارسال خودکار خطاها", "اداری و منابع انسانی"),
    # ── بخش ۶: ارتباط با بیمار ──
    (39, "21-sms",             "✉️", "پیامک هوشمند",          "یادآوری نوبت، اطلاع نتیجه آزمایش و لینک نظرسنجی با قالب‌های پویا", "ارتباط با بیمار"),
    (40, "22-surveys",         "📝", "نظرسنجی بیماران",       "سازنده پویا نظرسنجی، ارسال لینک پیامکی و تحلیل توزیع ستاره + میانگین پزشکان", "ارتباط با بیمار"),
    (41, "32-notifications",   "🔔", "اعلان‌ها و یادآورها",   "یادآور موعد دارو، هشدار موجودی و رویدادهای سیستم به‌صورت بلادرنگ", "ارتباط با بیمار"),
    (42, "31-complementary",   "🎨", "قالب چاپ و طب مکمل",    "۶ قالب چاپی آماده (فاکتور، فیش، نسخه، نتیجه آزمایش و…) + فرم‌های طب مکمل", "ارتباط با بیمار"),
    # ── بخش ۷: امنیت و سیستم ──
    (43, "19-users",           "👤", "مدیریت کاربران",        "سیاست رمز قوی، قفل خودکار پس از ۵ تلاش و ذخیره‌سازی BCrypt", "امنیت و سیستم"),
    (44, "20-roles",           "🔑", "نقش‌ها و دسترسی‌ها",    "۱۶۰ دسترسی ریزبین با ماتریس قابل تنظیم برای هر نقش", "امنیت و سیستم"),
    (45, "29-audit",           "🧾", "لاگ عملیات (ممیزی)",   "هر تغییر با مقدار قبلی/جدیدی و نام کاربر ثبت و فیلدهای حساس ماسک می‌شوند", "امنیت و سیستم"),
    (46, "28-backup",          "💾", "بکاپ و بازیابی",        "زمان‌بندی روزانه، چرخش خودکار نسخه‌ها و بازیابی مطمئن", "امنیت و سیستم"),
    (47, "30-license",         "🔑", "وضعیت لایسنس",          "ماژول‌های فعال + کارت پشتیبانی زنده دریافتی از سرور لایسنس", "امنیت و سیستم"),
    (48, "24-settings",        "⚙️", "تنظیمات سیستم",         "مشخصات درمانگاه، چاپ، پیامک و اطلاعات پشتیبانی (از سرور لایسنس)", "امنیت و سیستم"),
    (49, "49-user-form-win",   "👤", "فرم ثبت کاربر",         "ثبت کاربر با نقش، شعبه و سیاست رمز عبور", "امنیت و سیستم"),
    (50, "52-login-win",       "🔐", "صفحه ورود",             "ورود امن با نمایش شماره پشتیبانی محصول", "امنیت و سیستم"),
    # ── بخش ۸: گزارش‌ها ──
    (51, "18-reports",         "📈", "گزارش‌ساز پیشرفته",     "۳۱+ گزارش آماده در ۵ حوزه با خروجی اکسل فارسی و زمان‌بندی ارسال", "گزارش‌ها"),
    (52, "33-report-browser",  "📑", "مرورگر گزارش",          "مرور دسته‌ای گزارش‌ها با پیش‌نمایش نموداری قبل از اجرا", "گزارش‌ها"),
]

SECTION_ORDER = ["پذیرش و بیمار", "درمان و نسخه", "پاراکلینیک و داروخانه",
                 "مالی و حسابداری", "اداری و منابع انسانی", "ارتباط با بیمار",
                 "امنیت و سیستم", "گزارش‌ها"]
SECTION_ICONS = {"پذیرش و بیمار": "🛎", "درمان و نسخه": "🩺", "پاراکلینیک و داروخانه": "🧪",
                 "مالی و حسابداری": "💵", "اداری و منابع انسانی": "👔", "ارتباط با بیمار": "✉️",
                 "امنیت و سیستم": "🛡", "گزارش‌ها": "📈"}

HEAD = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="UTF-8">
<title>کاتالوگ معرفی — درمانگاه سهند</title>
<style>
@font-face { font-family:'Vazirmatn'; src:url('../assets/fonts/Vazirmatn-Regular.ttf') format('truetype'); font-weight:400; }
@font-face { font-family:'Vazirmatn'; src:url('../assets/fonts/Vazirmatn-Medium.ttf') format('truetype'); font-weight:500; }
@font-face { font-family:'Vazirmatn'; src:url('../assets/fonts/Vazirmatn-SemiBold.ttf') format('truetype'); font-weight:600; }
@font-face { font-family:'Vazirmatn'; src:url('../assets/fonts/Vazirmatn-Bold.ttf') format('truetype'); font-weight:700; }
@font-face { font-family:'Vazirmatn'; src:url('../assets/fonts/Vazirmatn-ExtraBold.ttf') format('truetype'); font-weight:800; }
@font-face { font-family:'Vazirmatn'; src:url('../assets/fonts/Vazirmatn-Black.ttf') format('truetype'); font-weight:900; }
@page { size:A4 landscape; margin:0; }
* { margin:0; padding:0; box-sizing:border-box; }
body { font-family:'Vazirmatn','Noto Color Emoji',sans-serif; color:#1A237E; }
.slide { width:297mm; height:209mm; padding:11mm 14mm 9mm; page-break-after:always;
  position:relative; overflow:hidden; background:#fff; }
.slide:last-child { page-break-after:auto; }

/* جلد */
.cover { background:linear-gradient(135deg,#0D47A1 0%,#1565C0 55%,#00897B 100%); color:#fff; text-align:center; }
.cover .logo-wrap { width:44mm; height:44mm; margin:6mm auto 0; border-radius:11mm;
  background:radial-gradient(circle at 50% 25%,#D8F1FF,#BFE4FB 75%);
  box-shadow:0 6mm 14mm rgba(0,20,50,.4), 0 0 0 2mm rgba(255,255,255,.28);
  display:flex; align-items:center; justify-content:center; overflow:hidden; }
.cover .logo-wrap img { width:37mm; height:37mm; object-fit:contain; border-radius:9mm; }
.cover h1 { font-size:42px; font-weight:900; margin-top:7mm; }
.cover h2 { font-size:21px; font-weight:500; opacity:.92; margin-top:3.5mm; }
.cover .tagline { display:inline-block; border:1.5px solid rgba(255,255,255,.5); border-radius:40px;
  padding:6px 30px; font-size:14px; margin-top:8mm; backdrop-filter:blur(4px); }
.cover .stats { display:flex; justify-content:center; gap:13mm; margin-top:11mm; }
.cover .stat b { display:block; font-size:32px; font-weight:900; }
.cover .stat span { font-size:11.5px; opacity:.85; }
.cover .footer-line { position:absolute; bottom:10mm; right:0; left:0; font-size:10.5px; opacity:.8; }

/* عنوان اسلاید */
.hdr { display:flex; align-items:center; gap:4.5mm; margin-bottom:5.5mm; }
.hdr .ic { width:13mm; height:13mm; border-radius:4.5mm; background:linear-gradient(135deg,#1565C0,#00897B);
  color:#fff; display:flex; align-items:center; justify-content:center; font-size:8mm; }
.hdr h1 { font-size:21px; font-weight:800; color:#0D47A1; }
.hdr p { font-size:11px; color:#546E7A; margin-top:0.8mm; }
.hdr .page-no { margin-right:auto; font-size:10.5px; color:#90A4AE; }

/* کارت‌ها */
.grid { display:grid; gap:4.5mm; }
.card { background:#F8FAFC; border:1px solid #E0E7F0; border-radius:4.5mm; padding:4.5mm 5.5mm; }
.card .c-ic { font-size:8mm; }
.card h3 { font-size:13.5px; font-weight:700; color:#0D47A1; margin:1.8mm 0 1.4mm; }
.card p { font-size:10px; color:#455A64; line-height:1.75; text-align:right; }

.big-num { font-size:46px; font-weight:900; color:#1565C0; line-height:1; }
.big-num small { font-size:13px; font-weight:600; color:#546E7A; }

table.cmp { width:100%; border-collapse:collapse; font-size:10.5px; }
table.cmp th { background:#0D47A1; color:#fff; padding:3mm 3.5mm; }
table.cmp td { padding:2.6mm 3.5mm; border-bottom:1px solid #E0E7F0; text-align:center; }
table.cmp td:first-child { text-align:right; font-weight:600; }
.yes { color:#2E7D32; font-weight:800; } .no { color:#C62828; font-weight:800; }

/* کارت تور تصویری — دو فرم در هر صفحه */
.tour { display:grid; grid-template-columns:1fr 1fr; gap:6mm; height:158mm; }
.form-card { display:flex; flex-direction:column; gap:2.8mm; }
.form-card .shot-frame { border:1px solid #CFD8DC; border-radius:3.5mm; overflow:hidden;
  box-shadow:0 3mm 7mm rgba(13,71,161,.13); background:#EFF3F9; }
.form-card .shot-frame img { width:100%; display:block; }
.form-card .cap { background:#F8FAFC; border:1px solid #E0E7F0; border-radius:3mm; padding:3mm 4mm; }
.form-card .cap b { display:block; font-size:12.5px; font-weight:800; color:#0D47A1; }
.form-card .cap span { display:block; font-size:10px; color:#455A64; line-height:1.75; margin-top:1mm; }
.form-card .cap .em { font-size:13px; margin-left:1.5mm; }

.modchip { display:flex; align-items:center; gap:2.2mm; background:#F5F8FA; border:1px solid #E0E7F0;
  border-radius:3mm; padding:2.2mm 3.6mm; font-size:10px; }
.modchip .m { font-size:5mm; }
.plan { border-radius:5mm; padding:5.5mm; text-align:center; background:#F8FAFC; border:1.5px solid #E0E7F0; }
.plan.hot { background:linear-gradient(160deg,#0D47A1,#1565C0); color:#fff; border:none; transform:scale(1.04); }
.plan h3 { font-size:15px; font-weight:800; }
.plan .price { font-size:12.5px; margin:1.8mm 0; opacity:.9; }
.plan ul { list-style:none; text-align:right; font-size:10px; line-height:2; }
.cta { background:linear-gradient(90deg,#00897B,#43A047); color:#fff; border-radius:40px;
  padding:3.5mm 11mm; font-size:14px; font-weight:700; display:inline-block; }

/* نوار تماس واقعی */
.contact-band { display:flex; align-items:center; justify-content:space-between; gap:8mm;
  background:linear-gradient(115deg,#0E7C86,#0E9F6E); color:#fff; border-radius:5mm; padding:6mm 10mm;
  box-shadow:0 4mm 10mm rgba(4,90,80,.35); }
.contact-band .who b { font-size:16px; font-weight:800; display:block; }
.contact-band .who span { font-size:11px; opacity:.9; }
.contact-band .rows { display:flex; gap:9mm; font-size:12.5px; font-weight:700; direction:ltr; }
.contact-band .rows .em { font-size:12px; }
</style>
</head>
<body>
"""

def slide_header(icon, title, sub, page):
    return (f'<div class="hdr"><div class="ic">{icon}</div>'
            f'<div><h1>{title}</h1><p>{sub}</p></div>'
            f'<div class="page-no">{page}</div></div>')

def form_card(num, fname, icon, title, desc):
    return (f'<div class="form-card">'
            f'<div class="shot-frame"><img src="../screenshots/forms/{fname}.png" alt="{title}"></div>'
            f'<div class="cap"><b><span class="em">{icon}</span>{num}. {title}</b>'
            f'<span>{desc}</span></div></div>')

def main():
    parts = [HEAD]
    page = 1

    # ═══ جلد ═══
    parts.append(f"""
<!-- ═══ اسلاید {page}: جلد ═══ -->
<div class="slide cover">
  <div class="logo-wrap"><img src="../assets/ClinicPro.png" alt="لوگوی درمانگاه سهند"></div>
  <h1>سامانه جامع مدیریت درمانگاه سهند</h1>
  <h2>Sahand ClinicPro — نسخه ۱.۷.۰</h2>
  <div class="tagline">از پذیرش بیمار تا فیش حقوقی پرسنل — همه‌چیز در یک نرم‌افزار فارسی و جلالی</div>
  <div class="stats">
    <div class="stat"><b>۳۰+</b><span>ماژول یکپارچه</span></div>
    <div class="stat"><b>۵۲</b><span>فرم آماده کار</span></div>
    <div class="stat"><b>۹۴</b><span>جدول دیتابیس</span></div>
    <div class="stat"><b>۱۴۳</b><span>تست خودکار سبز</span></div>
    <div class="stat"><b>۱۰۰٪</b><span>فارسی و جلالی</span></div>
  </div>
  <div class="footer-line">کاتالوگ معرفی و بازاریابی — پاییز ۱۴۰۵ | ⭐ تور تصویری کامل هر ۵۲ فرم + حقوق درصدی پرسنل از سود خدمات + آیکون رسمی روی همه پنجره‌ها + پشتیبانی یکپارچه سهند سرویس</div>
</div>""")
    page += 1

    # ═══ چرا درمانگاه سهند ═══
    parts.append(f"""
<!-- ═══ اسلاید {page}: چرا سهند ═══ -->
<div class="slide">
  {slide_header("⚡", "چرا درمانگاه سهند؟", "شش دلیل کلی برای انتخاب — جزئیات کامل در تور تصویری", page)}
  <div class="grid" style="grid-template-columns:1fr 1fr 1fr; grid-auto-rows:1fr;">
    <div class="card"><div class="c-ic">🛎</div><h3>صف انتظار زنده</h3><p>فراخوان بیمار روی همه رایانه‌ها همان لحظه ظاهر می‌شود؛ نوبت‌سنج دیگر با فریاد و راه‌رویدن نمی‌چرخد. فناوری SignalR مایکروسافت.</p></div>
    <div class="card"><div class="c-ic">💊</div><h3>نسخه هوشمند و ایمن</h3><p>تداخل‌سنجی خودکار دارو، پایش حساسیت‌ها و ارسال مستقیم به سامانه نسخه الکترونیک — با امضای دیجیتال پزشک.</p></div>
    <div class="card"><div class="c-ic">🪪</div><h3>ضد جعل از لحظه ثبت</h3><p>کد ملی بیماران با الگوریتم رسمی رقم کنترلی اعتبارسنجی می‌شود؛ کد جعلی یا تکراری هرگز پذیرفته نمی‌شود.</p></div>
    <div class="card"><div class="c-ic">💵</div><h3>مالی بی‌خطا</h3><p>هر فاکتور خودکار سند دوبل صادر می‌کند؛ تراز آزمایشی همیشه بسته. فیش حقوقی با قواعد ۱۴۰۴ + حقوق درصدی هر پرسنل فقط از «سود خدمات انجامی خودش» نه کل فروش.</p></div>
    <div class="card"><div class="c-ic">🏛</div><h3>مچ با سامانه‌های دولتی</h3><p>سپاس، سیب، ناب، سینا و نسخه الکترونیک — ادغام بومی با حالت شبیه‌ساز تا روز فعال‌سازی رسمی.</p></div>
    <div class="card"><div class="c-ic">🧑‍💻</div><h3>پشتیبانی زنده یکپارچه</h3><p>اطلاعات تماس پشتیبانی از سرور لایسنس دریافت و در سراسر برنامه نمایش داده می‌شود — همیشه به‌روز و قابل ویرایش از پنل.</p></div>
  </div>
</div>""")
    page += 1

    # ═══ فهرست تور ═══
    toc_rows = ""
    for i, sec in enumerate(SECTION_ORDER, 1):
        count = sum(1 for f in FORMS if f[5] == sec)
        nums = [str(f[0]) for f in FORMS if f[5] == sec]
        rng = f"{nums[0]}–{nums[-1]}" if len(nums) > 1 else nums[0]
        toc_rows += (f'<div class="modchip" style="font-size:11.5px;padding:3.5mm 5mm">'
                     f'<span class="m">{SECTION_ICONS[sec]}</span><b>{i}. {sec}</b>'
                     f'<span style="margin-right:auto;color:#546E7A">فرم‌های {rng} — {count} فرم</span></div>')
    parts.append(f"""
<!-- ═══ اسلاید {page}: نقشه تور تصویری ═══ -->
<div class="slide">
  {slide_header("🗺", "تور تصویری کامل — ۵۲ فرم برنامه", "در ۸ حوزه کاری؛ هر فرم با تصویر واقعی و توضیح کاربرد", page)}
  <div class="grid" style="grid-template-columns:1fr 1fr;">{toc_rows}</div>
  <div style="margin-top:6mm" class="card"><p style="font-size:11px">
  📌 تمام تصاویر این کاتالوگ از رابط کاربری واقعی نرم‌افزار (نسخه دسکتاپ ویندوزی، فارسی و راست‌چین) تهیه شده‌اند.
  برای بزرگ‌نمایی هر تصویر، فایل PDF را با زوم ۱۵۰٪ یا بیشتر ببینید؛ متن‌ها در چاپ اصل نیز کاملاً خوانا هستند.</p></div>
</div>""")
    page += 1

    # ═══ تور تصویری — دو فرم در هر صفحه ═══
    tour_start_page = page
    tour_pages_count = (len(FORMS) + 1) // 2
    for sec_idx, sec in enumerate(SECTION_ORDER):
        forms = [f for f in FORMS if f[5] == sec]
        for i in range(0, len(forms), 2):
            pair = forms[i:i + 2]
            cards = "".join(form_card(*f[:5]) for f in pair)
            sec_title = f"{SECTION_ICONS[sec]} حوزه {sec_idx + 1}: {sec}" if i == 0 else f"{SECTION_ICONS[sec]} {sec} (ادامه)"
            parts.append(f"""
<!-- ═══ اسلاید {page}: {sec} — فرم {pair[0][0]}{f" و {pair[1][0]}" if len(pair) > 1 else ""} ═══ -->
<div class="slide">
  {slide_header(SECTION_ICONS[sec], sec_title, "تور تصویری کامل — تصویر واقعی رابط کاربری + کاربرد", page)}
  <div class="tour">{cards}</div>
</div>""")
            page += 1

    # ═══ جدول ماژول‌های لایسنس ═══
    modules = [
        ("Dashboard", "داشبورد"), ("Patients", "بیماران"), ("Appointments", "نوبت‌دهی"), ("Receptions", "پذیرش"),
        ("Visits", "ویزیت"), ("Prescriptions", "نسخه‌نویسی"), ("Billing", "صندوق و فاکتور"), ("Lab", "آزمایشگاه"),
        ("Pharmacy", "داروخانه"), ("Imaging", "تصویربرداری"), ("Dental", "دندانپزشکی"), ("Eye", "چشم‌پزشکی"),
        ("Physiotherapy", "فیزیوتراپی"), ("Accounting", "حسابداری"), ("HR", "پرسنل"), ("Attendance", "حضور و غیاب"),
        ("Reports", "گزارش‌ها"), ("Sms", "پیامک"), ("Insurance", "بیمه"), ("Government", "سامانه‌های دولتی"),
        ("Settings", "تنظیمات"), ("Backup", "بکاپ"), ("Audit", "لاگ عملیات"), ("Users", "کاربران"),
        ("Survey", "نظرسنجی بیماران 🆕"), ("Payroll", "فیش حقوقی 🆕"), ("Paraclinical", "پاراکلینیک 🆕"),
        ("EMR", "پرونده بالینی پیشرفته 🆕"), ("Complementary", "طب مکمل و قالب چاپ 🆕"), ("Branches", "مدیریت شعبه‌ها 🆕"),
    ]
    chip_rows = ""
    for i in range(0, len(modules), 5):
        row = "".join(f'<div class="modchip"><span class="m">{ic}</span><b>{fa}</b></div>'
                      for (en, fa) in [(m[0], m[1]) for m in modules[i:i + 5]]
                      for ic in ["✅"])
        chip_rows += f'<div class="grid" style="grid-template-columns:repeat(5,1fr)">{row}</div>'
    parts.append(f"""
<!-- ═══ اسلاید {page}: ماژول‌های لایسنس ═══ -->
<div class="slide">
  {slide_header("🧩", "۳۰ ماژول قابل فعال‌سازی جداگانه", "لایسنس انعطاف‌پذیر: هر درمانگاه فقط ماژول‌های موردنیاز را فعال می‌کند — همه ماژول‌های جدید از این پس همزمان به این فهرست اضافه می‌شوند", page)}
  {chip_rows}
  <div class="grid" style="grid-template-columns:1fr 1fr 1fr; margin-top:5mm">
    <div class="card" style="text-align:center"><div class="big-num">🔑 <small>قفل سخت‌افزاری</small></div><p>لایسنس به دستگاه شما قفل می‌شود؛ سرقت کلید بی‌فایده است.</p></div>
    <div class="card" style="text-align:center"><div class="big-num">⏱ <small>تپ‌قلب روزانه</small></div><p>اعتبارسنجی روزانه با مهلت آفلاین ۳۰ روزه در قطعی اینترنت.</p></div>
    <div class="card" style="text-align:center"><div class="big-num">🧑‍💻 <small>پنل مستقل</small></div><p>مدیریت لایسنس‌ها و اطلاعات پشتیبانی روی هاست اختصاصی شما.</p></div>
  </div>
</div>""")
    page += 1

    # ═══ مقایسه با روش سنتی ═══
    parts.append(f"""
<!-- ═══ اسلاید {page}: مقایسه ═══ -->
<div class="slide">
  {slide_header("⚖️", "مقایسه با روش سنتی", "چرا ادامه کار با کاغذ، اکسل و نرم‌افزارهای تکه‌تکه توجیه‌پذیر نیست", page)}
  <table class="cmp">
    <tr><th>شاخص</th><th>روش سنتی (کاغذ + اکسل)</th><th>نرم‌افزارهای عمومی</th><th>درمانگاه سهند</th></tr>
    <tr><td>صف انتظار و فراخوان بیمار</td><td class="no">فریاد و راه‌روی</td><td class="no">ندارد</td><td class="yes">صف زنده بلادرنگ 🛎</td></tr>
    <tr><td>اعتبارسنجی کد ملی بیمار</td><td class="no">چشمی و خطاخیز</td><td class="no">معمولاً بدون رقم کنترلی</td><td class="yes">الگوریتم رسمی ضد جعل 🪪</td></tr>
    <tr><td>تداخل دارویی هنگام تجویز</td><td class="no">به حافظه پزشک</td><td class="no">ندارد</td><td class="yes">تداخل‌سنجی خودکار 💊</td></tr>
    <tr><td>اتصال به سپاس و نسخه الکترونیک</td><td class="no">دستی و پرخطا</td><td class="no">نیازمند واسطه</td><td class="yes">ادغام بومی 🏛</td></tr>
    <tr><td>حضور و غیاب و فیش حقوقی</td><td class="no">دفترچه و ماشین‌حساب</td><td class="no">جدا از درمانگاه</td><td class="yes">زنجیره یکپارچه 🧾</td></tr>
    <tr><td>حسابداری دوبل از فاکتور</td><td class="no">ورود دوباره‌کار</td><td class="no">ماژول جدا و پرهزینه</td><td class="yes">سند خودکار ⚖️</td></tr>
    <tr><td>پشتیبانی و به‌روزرسانی</td><td class="no">وابسته به فرد</td><td class="no">تیکت عمومی</td><td class="yes">سهند سرویس — تماس مستقیم 🧑‍💻</td></tr>
  </table>
</div>""")
    page += 1

    # ═══ اسلاید پایانی — تماس واقعی ═══
    parts.append(f"""
<!-- ═══ اسلاید {page}: پایانی و تماس ═══ -->
<div class="slide">
  {slide_header("🎁", "همین امروز شروع کنید", "۱۵ روز دموی رایگان — نصب و آموزش توسط سهند سرویس", page)}
  <div class="grid" style="grid-template-columns:1fr 1fr 1fr;">
    <div class="plan"><h3>🥉 پایه</h3><div class="price">مناسب مطب‌های تک‌پزشک</div>
      <ul><li>✔️ بیماران + نوبت + پذیرش</li><li>✔️ نسخه‌نویسی + پیامک</li><li>✔️ ۵ کاربر</li></ul></div>
    <div class="plan hot"><h3>🥇 حرفه‌ای</h3><div class="price">پرطرفدارترین انتخاب</div>
      <ul><li>✔️ همه ماژول‌های درمان و مالی</li><li>✔️ حضور و غیاب + فیش حقوقی</li><li>✔️ نظرسنجی بیماران + سپاس</li><li>✔️ ۱۵ کاربر + پشتیبانی مستقیم</li></ul></div>
    <div class="plan"><h3>🏢 سازمانی</h3><div class="price">چندشعبه‌ای‌ها</div>
      <ul><li>✔️ همه ماژول‌ها + شعبه‌ها</li><li>✔️ اتصال بیمه و سامانه‌های دولتی</li><li>✔️ کاربر نامحدود</li></ul></div>
  </div>
  <div style="text-align:center; margin:8mm 0 6mm"><span class="cta">📞 برای دموی رایگان همین حالا تماس بگیرید</span></div>
  <div class="contact-band">
    <div class="who"><b>سعید نجفی</b><span>سهند سرویس — پشتیبانی و فروش</span></div>
    <div class="rows">
      <div><span class="em">📞</span> 041-34436141</div>
      <div><span class="em">📱</span> 09144220430</div>
      <div><span class="em">✉️</span> info@ea-fixer.ir</div>
      <div><span class="em">🌐</span> ea-fixer.ir</div>
    </div>
  </div>
</div>
</body>
</html>""")

    OUT.write_text("".join(parts), encoding="utf-8")
    tour_pages = page - tour_start_page
    print(f"📘 بروشور ساخته شد: {OUT}")
    print(f"   صفحات کل: {page} (تور تصویری: {tour_pages} صفحه — {len(FORMS)} فرم)")


if __name__ == "__main__":
    main()
