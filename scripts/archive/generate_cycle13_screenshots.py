#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📸 مولد اسکرین‌شات قابلیت‌های جدید چرخه سیزدهم — نسخه ۲.۰.۰
============================================================
۱۱ فرم جدید (۵۳ تا ۶۳) مطابق ظاهر واقعی WPF/Material با بازی‌استفاده
از سازنده‌های اسکریپت ۵۲ فرم موجود.

خروجی: docs/screenshots/forms/53..63-*.png
"""
import sys
import pathlib

# بازی‌استفاده سازنده‌ها و قالب‌های اسکریپت اصلی
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import generate_form_screenshots as base  # noqa: E402

from playwright.sync_api import sync_playwright  # noqa: E402

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
OUT_DIR = REPO / "docs" / "screenshots" / "forms"
OUT_DIR.mkdir(parents=True, exist_ok=True)
TMP_DIR = REPO / "scripts" / "_mockups13"
TMP_DIR.mkdir(parents=True, exist_ok=True)

W, H = 1600, 1000
B = base.B

# استایل‌های تکمیلی چرخه ۱۳ — به بدنه هر فرم افزوده می‌شود
EXTRA_CSS = """
<style>
/* چیدمان دوستونه پنل‌ها — چرخه ۱۳ */
.panels2 { display:flex; gap:12px; align-items:stretch; min-height:0; }
.panels2 .panel { min-width:0; }
/* کارت وضعیت ابری */
.cloudcard { background:#fff; border:1.5px solid #CFE4FB; border-radius:13px; padding:16px 18px; margin-bottom:10px; }
.cloudgrid { display:flex; gap:20px; align-items:center; margin-top:10px; }
.cloudgrid .ci { flex:1; } .cloudgrid .ci .l { font-size:11px; color:#8296B3; }
.cloudgrid .ci .v { font-size:15.5px; font-weight:800; color:#16355E; margin-top:4px; }
.pill { display:inline-flex; align-items:center; gap:6px; border-radius:20px; padding:5px 14px;
  font-size:11.5px; font-weight:800; color:#fff; }
.pill.ok { background:#2E7D32; } .pill.off { background:#9E9E9E; } .pill.enc { background:#1565C0; }
/* گام‌های شماره‌دار گزارش‌ساز */
.stepcard { background:#fff; border:1.5px solid #E1E8F2; border-radius:13px; padding:14px 16px; margin-bottom:10px; }
.stephead { display:flex; align-items:center; gap:9px; margin-bottom:10px; }
.stepnum { width:26px; height:26px; border-radius:50%; background:#1565C0; color:#fff; font-weight:800;
  display:flex; align-items:center; justify-content:center; font-size:13px; flex:none; }
.stephead b { font-size:13.5px; color:#16355E; }
.chk { display:inline-flex; align-items:center; gap:7px; background:#F4F8FD; border:1px solid #E1E8F2;
  border-radius:8px; padding:6px 11px; margin:3px; font-size:11.5px; font-weight:600; }
.chk.on { background:#E3F2FD; border-color:#90CAF9; color:#0D47A1; }
.frow { background:#F4F8FD; border:1px solid #E1E8F2; border-radius:9px; padding:9px 11px; margin-bottom:6px;
  display:flex; gap:8px; align-items:center; }
.tag { border:1px solid #CFE4FB; border-radius:7px; background:#fff; padding:5px 10px; font-size:11px; font-weight:700; color:#16355E; }
/* تور هدایت‌شده */
.tourbg { position:absolute; inset:0; }
.tourhole { position:absolute; border-radius:12px; box-shadow:0 0 0 4000px rgba(10,22,40,.78); border:2.5px solid #64B5F6; }
.tourbubble { position:absolute; background:#fff; border-radius:15px; border:1.5px solid #BBDEFB; padding:19px 22px; width:440px;
  box-shadow:0 14px 42px rgba(8,25,50,.35); }
.tbtitle { font-size:16.5px; font-weight:800; color:#1565C0; display:flex; align-items:center; gap:8px; }
.tbmsg { font-size:12.5px; line-height:2.05; color:#37474F; margin-top:9px; }
.tourbar { height:5px; border-radius:3px; background:#ECEFF1; margin:13px 0 11px; overflow:hidden; }
.tourbar i { display:block; height:100%; background:#1565C0; border-radius:3px; }
/* خط زمان */
.tl { position:relative; padding-right:26px; }
.tl::before { content:''; position:absolute; right:9px; top:6px; bottom:6px; width:3px; background:#E1E8F2; border-radius:2px; }
.tli { position:relative; padding:8px 0 14px; }
.tli::before { content:''; position:absolute; right:-24px; top:13px; width:13px; height:13px; border-radius:50%;
  border:3px solid #fff; box-shadow:0 0 0 1.5px currentColor; background:currentColor; }
.tli .d { font-size:10.5px; color:#8296B3; font-weight:700; }
.tli .t { font-size:13px; font-weight:800; color:#16355E; margin-top:2px; }
.tli .s { font-size:11.5px; color:#5B7089; margin-top:3px; line-height:1.8; }
/* آیتم درس آموزش */
.lsn { display:flex; align-items:center; gap:11px; background:#fff; border:1px solid #E1E8F2;
  border-radius:11px; padding:11px 13px; margin-bottom:7px; }
.lsn .ic { font-size:20px; } .lsn b { font-size:12.5px; color:#16355E; display:block; }
.lsn small { font-size:10.5px; color:#8296B3; }
.done { background:#2E7D32; color:#fff; border-radius:14px; font-size:10px; padding:3px 10px; font-weight:800; margin-right:auto; }
.pending { background:#ECEFF1; color:#78909C; border-radius:14px; font-size:10px; padding:3px 10px; font-weight:800; margin-right:auto; }
</style>
"""

def shot_shell(title, crumb, nav, body, icon=""):
    """پوسته نما با استایل تکمیلی"""
    return base.shell(title, crumb, nav, EXTRA_CSS + body, icon)

def shot_window(title, sub, icon, body):
    """پوستر پنجره با استایل تکمیلی"""
    return base.window_shell(title, sub, icon, EXTRA_CSS + body)

SPECS = []

def add(key, title, html, kind="view"):
    SPECS.append(dict(key=key, title=title, html=html, kind=kind))

# ═══════════════ ۵۳ — تور هدایت‌شده اولین اجرا (پنجره) ═══════════════
add("guided-tour", "تور معرفی سامانه", shot_window(
    "تور معرفی سامانه", "چرخه سیزدهم — ۸ ایستگاه، ۵ دقیقه", "🎓",
    """
<div style="position:relative;height:770px;background:linear-gradient(135deg,#EFF3F9,#E3ECF7);border-radius:8px;overflow:hidden">
  <div style="position:absolute;top:34px;right:26px;width:214px;height:58px;border:1.5px solid #D5DFEC;background:#fff;border-radius:9px;display:flex;align-items:center;gap:9px;padding:0 13px;font-size:12px;font-weight:800;color:#16355E">📊 داشبورد</div>
  <div style="position:absolute;top:110px;right:26px;width:214px;height:58px;border:1.5px solid #D5DFEC;background:#fff;border-radius:9px;display:flex;align-items:center;gap:9px;padding:0 13px;font-size:12px;font-weight:700;color:#40536E">🧑‍🤝‍🧑 بیماران</div>
  <div style="position:absolute;top:186px;right:26px;width:214px;height:58px;border:2.5px solid #64B5F6;box-shadow:0 0 0 4000px rgba(10,22,40,.78);border-radius:9px;background:#fff;display:flex;align-items:center;gap:9px;padding:0 13px;font-size:12px;font-weight:800;color:#0D47A1">🗓 نوبت‌دهی</div>
  <div class="tourbubble" style="top:300px;right:70px">
    <div style="display:flex;justify-content:space-between;align-items:center">
      <div class="tbtitle">🗓 نوبت‌دهی</div><div style="font-size:10.5px;color:#8296B3;font-weight:700">۳ از ۸</div>
    </div>
    <div class="tbmsg">رزرو حضوری و تلفنی با تقویم شمسی، لغو با پیشنهاد خودکار به لیست انتظار و یادآوری پیامکی.<br>نوار وضعیت رنگی، شلوغی هر روز را یک‌نگاه نشان می‌دهد.</div>
    <div class="tourbar"><i style="width:37.5%"></i></div>
    <div style="display:flex;gap:8px;align-items:center">
      <button style="border:none;background:none;color:#E53935;font-weight:800;font-size:11.5px;cursor:pointer">رد شدن از تور</button>
      <div style="flex:1"></div>
      <button style="border:1.5px solid #CFE4FB;background:#fff;color:#1565C0;border-radius:8px;padding:7px 18px;font-weight:800;font-size:12px">قبلی</button>
      <button style="border:none;background:#1565C0;color:#fff;border-radius:8px;padding:7px 20px;font-weight:800;font-size:12.5px">بعدی ▶</button>
    </div>
  </div>
  <div style="position:absolute;bottom:22px;right:50%;transform:translateX(50%);color:#B9C9DD;font-size:12px;font-weight:700">هایلایت نقطه‌ای + حباب راهنما — نمایش خودکار در نخستین ورود هر کاربر</div>
</div>"""), kind="win")

# ═══════════════ ۵۴ — مرکز آموزش (نما) ═══════════════
add("training-center", "مرکز آموزش", shot_shell(
    "مرکز آموزش", "درمانگاه سهند — مرکز آموزش", "🎓 آموزش",
    base.toolbar([("بازنشانی پیشرفت", "♻️", "sec")], "جستجوی درس…")
    + '<div class="panels2">'
    + '<div class="panel" style="flex:0 0 335px">'
    + '<div class="ph"><b>فهرست درس‌ها</b><small>۱۰ درس تعاملی گام‌به‌گام</small></div>'
    + '<div style="padding:4px 8px 10px">'
    + '<div style="background:#fff;border:1px solid #E1E8F2;border-radius:11px;padding:12px 13px;margin-bottom:10px">'
    + '<div style="display:flex;justify-content:space-between;font-size:12.5px"><b style="color:#16355E">پیشرفت یادگیری شما</b><b style="color:#1565C0">۳ از ۱۰</b></div>'
    + '<div style="height:8px;border-radius:4px;background:#ECEFF1;margin-top:8px"><i style="display:block;height:100%;width:30%;background:#1565C0;border-radius:4px"></i></div></div>'
    + '<div class="lsn"><span class="ic">👤</span><div><b>پذیرش بیمار جدید</b><small>ثبت پرونده با کد ملی معتبر • ۴ دقیقه</small></div><span class="done">✓ تکمیل</span></div>'
    + '<div class="lsn" style="border-color:#90CAF9;background:#F4F9FF"><span class="ic">📅</span><div><b>ثبت نوبت</b><small>رزرو با تقویم شمسی • ۴ دقیقه</small></div><span class="done">✓ تکمیل</span></div>'
    + '<div class="lsn" style="border-color:#90CAF9;background:#F4F9FF"><span class="ic">🛎</span><div><b>پذیرش حضوری روز مراجعه</b><small>صف انتظار و شماره نوبت • ۳ دقیقه</small></div><span class="done">✓ تکمیل</span></div>'
    + '<div class="lsn"><span class="ic">🩺</span><div><b>ویزیت و معاینه</b><small>ساختار SOAP • ۵ دقیقه</small></div><span class="pending">در انتظار</span></div>'
    + '<div class="lsn"><span class="ic">💊</span><div><b>نسخه‌نویسی و ایمنی دارویی</b><small>هشدار تداخل هوشمند • ۵ دقیقه</small></div><span class="pending">در انتظار</span></div>'
    + '<div class="lsn"><span class="ic">🧾</span><div><b>صورتحساب و فاکتور</b><small>تسویه بیمه • ۴ دقیقه</small></div><span class="pending">در انتظار</span></div>'
    + '</div></div>'
    + '<div class="panel grow"><div class="ph"><b>💊 درس ۵ از ۱۰ — نسخه‌نویسی و ایمنی دارویی</b><small>گام‌به‌گام با نکات کارگاهی</small></div>'
    + '<div style="padding:10px 16px;font-size:12.5px;line-height:2.1;color:#37474F">'
    + '<div style="background:#F1F8FF;border:1px solid #CFE4FB;border-radius:10px;padding:11px 14px;margin-bottom:9px"><b style="color:#1565C0">گام ۱ —</b> «نسخه‌نویسی» → نسخه جدید برای ویزیت فعال.</div>'
    + '<div style="background:#F1F8FF;border:1px solid #CFE4FB;border-radius:10px;padding:11px 14px;margin-bottom:9px"><b style="color:#1565C0">گام ۲ —</b> داروها را جستجو و با دوز و مدت به نسخه اضافه کنید.</div>'
    + '<div style="background:#F1F8FF;border:1px solid #CFE4FB;border-radius:10px;padding:11px 14px;margin-bottom:9px"><b style="color:#1565C0">گام ۳ —</b> پنل هشدار زنده را ببینید: تداخل دارو-دارو، حساسیت، بارداری، سنی و کلیوی.</div>'
    + '<div style="background:#FFF8E1;border-right:4px solid #FFB300;border-radius:8px;padding:10px 13px;margin:12px 0"><b>⚠️ هشدار:</b> تیک «باردار/شیرده» را هنگام ویزیت بانوان فعال کنید.</div>'
    + '<div style="background:#E8F5E9;border-right:4px solid #43A047;border-radius:8px;padding:10px 13px"><b>💡 نکته:</b> نسخه الکترونیک با کد پیگیری در همان فرم ارسال می‌شود.</div>'
    + '</div></div></div>'
    + base.actions_row().replace('💾 ذخیره', '✅ این درس را تکمیل کردم').replace('🗑 حذف', '').replace('🖨 چاپ', '')))

# ═══════════════ ۵۵ — گزارش‌ساز رایگانه (پنجره) ═══════════════
add("adhoc-report", "گزارش‌ساز رایگانه", shot_window(
    "گزارش‌ساز رایگانه — کوئری‌ساز گرافیکی", "بدون نیاز به SQL — سه گام: موجودیت ← فیلتر ← گروه‌بندی", "🧮",
    '<div class="panels2">'
    + '<div style="flex:0 0 465px">'
    + '<div class="stepcard"><div class="stephead"><span class="stepnum">۱</span><b>موجودیت و ستون‌ها</b></div>'
    + '<div class="tag" style="background:#E3F2FD;border-color:#90CAF9;margin-bottom:8px;display:inline-block;padding:7px 14px">🧾 فاکتورها</div>'
    + '<div><span class="chk on">شماره فاکتور</span><span class="chk on">نام بیمار</span><span class="chk on">نوع فاکتور</span>'
    + '<span class="chk on">مبلغ نهایی</span><span class="chk on">وضعیت پرداخت</span><span class="chk">تاریخ فاکتور</span></div></div>'
    + '<div class="stepcard"><div class="stephead"><span class="stepnum">۲</span><b>فیلترها (اختیاری)</b>'
    + '<button style="margin-right:auto;border:1.5px solid #CFE4FB;background:#fff;color:#1565C0;border-radius:8px;padding:5px 12px;font-weight:800;font-size:11px">＋ افزودن</button></div>'
    + '<div class="frow"><span class="tag">وضعیت پرداخت</span><span class="tag">مساوی</span>'
    + '<span class="tag" style="background:#E3F2FD;border-color:#90CAF9;color:#0D47A1">تسویه‌نشده</span></div>'
    + '<div class="frow"><span class="tag">مبلغ نهایی</span><span class="tag">بزرگ‌تر مساوی</span><span class="tag" style="background:#E3F2FD;border-color:#90CAF9;color:#0D47A1">۵۰۰٬۰۰۰</span></div></div>'
    + '<div class="stepcard"><div class="stephead"><span class="stepnum">۳</span><b>گروه‌بندی و تجمیع</b></div>'
    + '<div style="display:flex;gap:8px"><span class="tag" style="flex:1;text-align:center;background:#E3F2FD;border-color:#90CAF9;color:#0D47A1;padding:8px">گروه: نوع فاکتور</span>'
    + '<span class="tag" style="flex:1;text-align:center;background:#E3F2FD;border-color:#90CAF9;color:#0D47A1;padding:8px">تجمیع: جمع مبلغ نهایی</span></div></div>'
    + '<div class="stepcard"><div class="stephead"><span class="stepnum">⚙</span><b>پارامترها و ذخیره</b></div>'
    + '<div style="display:flex;gap:8px;align-items:center">'
    + '<span class="tag">سقف ردیف: ۱۰۰</span><span class="tag">نمودار: ستونی</span>'
    + '<button style="border:none;background:#1565C0;color:#fff;border-radius:8px;padding:7px 16px;font-weight:800;font-size:11.5px;margin-right:auto">💾 ذخیره تعریف</button></div>'
    + '<div style="margin-top:9px"><span class="tag" style="background:#F1F8FF">درس‌های ذخیره‌شده: «مانده بیماران بیمه‌دار» ▶</span></div></div>'
    + '</div>'
    + '<div class="panel grow"><div class="ph"><b>نتایج — جمع مبلغ نهایی به تفکیک نوع فاکتور</b><small>لیست سفید امن — بدون هیچ SQL خام</small></div>'
    + '<div class="cards">'
    + '<div class="stat c1"><div class="lb">🗂 تعداد گروه</div><div class="vl">۶</div><div class="tr up">▲ کامل</div></div>'
    + '<div class="stat c2"><div class="lb">💰 جمع کل (ریال)</div><div class="vl">۸۴٬۲۰۰٬۰۰۰</div><div class="tr up">▲ ۱۲٪</div></div>'
    + '<div class="stat c3"><div class="lb">🏆 بیشترین گروه</div><div class="vl">ویزیت</div><div class="tr up">۴۱٪ کل</div></div>'
    + '</div>'
    + base.chart_panel("جمع مبلغ به تفکیک نوع فاکتور (ستونی دوقلو)", [82, 58, 41, 27, 14, 8],
                       ["ویزیت", "آزمایش", "تصویربرداری", "داروخانه", "دندانپزشکی", "سایر"], hi_index=0)
    + '</div></div>'), kind="win")

# ═══════════════ ۵۶ — لیست انتظار نوبت (نما) ═══════════════
add("waitlist", "لیست انتظار نوبت", shot_shell(
    "نوبت‌دهی — لیست انتظار", "درمانگاه سهند — لیست انتظار", "🗓 نوبت‌دهی",
    '<div class="chips" style="margin-bottom:9px"><span class="chip on">📅 برنامه هفته</span>'
    + '<span class="chip">🧑‍🤝‍🧑 نوبت‌های امروز</span><span class="chip on" style="background:#1565C0;color:#fff">⏳ لیست انتظار</span>'
    + '<span class="chip">📊 آمار نوبت‌ها</span></div>'
    + base.toolbar([("ثبت در لیست انتظار", "➕", "pri"), ("پیشنهاد جای خالی", "🔁", "sec"), ("ارسال پیامک پیشنهاد", "✉️", "sec")], "نام بیمار…")
    + base.stat_row([
        ("منتظران فعلی", "۱۷", "▲ ۴ امروز اضافه شد", "c1", "⏳"),
        ("پیشنهاد ارسال‌شده", "۹", "▲ ۶ پذیرفت", "c2", "✉️"),
        ("نرخ تبدیل پیشنهاد", "۶۷٪", "▲ بالاتر از میانگین", "c3", "📈"),
        ("اوشوان صف (اورژانس)", "۲", "▲ پیگیری فوری", "c6", "🚨")])
    + base.table_panel("لیست انتظار — با لغو هر نوبت، متناظرترین منتظرِ همان پزشک پیامک پیشنهاد می‌گیرد",
        ["بیمار", "پزشک دلخواه", "بازه دلخواه", "اولویت", "وضعیت", "ثبت"],
        [("زهرا محمدی — MR-1042", "دکتر کریمی", "شنبه تا چهارشنبه، صبح", B.format("red", "🚨 اورژانس"), B.format("blue", "منتظر"), "۱۴۰۵/۰۷/۱۰"),
         ("علی رضایی — MR-1078", "دکتر موسوی", "هر روز، عصر", B.format("amber", "⚡ فوری"), B.format("purple", "پیشنهاد رفت"), "۱۴۰۵/۰۷/۱۱"),
         ("مریم احمدی — MR-1103", "دکتر کریمی", "فقط پنجشنبه", B.format("green", "عادی"), B.format("blue", "منتظر"), "۱۴۰۵/۰۷/۱۲"),
         ("حسین کاظمی — MR-1121", "هر پزشک", "شنبه/دوشنبه، صبح", B.format("green", "عادی"), B.format("green", "نوبت گرفت ✓"), "۱۴۰۵/۰۷/۱۳")],
        small="اولویت: اورژانس ← فوری ← عادی — هم‌ترازها به ترتیب قدیمی‌ترین")))

# ═══════════════ ۵۷ — بستن شیفت صندوق (پنجره) ═══════════════
add("shift-close", "بستن شیفت صندوق", shot_window(
    "بستن شیفت صندوق", "شنبه ۱۴۰۵/۰۷/۱۳ — صندوقدار: فاطمه اکبری", "🌙",
    '<div class="panels2">'
    + '<div class="panel grow"><div class="ph"><b>شمارش فیزیکی نقد و کارت</b><small>جمع سامانه ↔ دستی</small></div>'
    + base.table_panel("مقایسه", ["روش", "جمع سامانه", "شمارش دستی", "اختلاف", "وضعیت"],
        [("💵 نقدی", "۱۲٬۴۵۰٬۰۰۰", "۱۲٬۴۰۰٬۰۰۰", "−۵۰٬۰۰۰", B.format("amber", "کم‌شده ⚠")),
         ("💳 کارت‌خوان", "۲۱٬۸۰۰٬۰۰۰", "۲۱٬۸۰۰٬۰۰۰", "۰", B.format("green", "مطابقت ✓")),
         ("🏦 آنلاین", "۶٬۲۰۰٬۰۰۰", "—", "—", B.format("blue", "سیستمی")),
         ("جمع کل", "۴۰٬۴۵۰٬۰۰۰", "۴۰٬۴۰۰٬۰۰۰", "−۵۰٬۰۰۰", B.format("amber", "کم ۰٫۱۲٪"))],
        small="اختلاف‌سنجی خودکار با رنگ‌بندی کم/اضافه/مطابقت")
    + '<div class="formgrid">'
    + '<div class="fld"><label>تحویل‌گیرنده شیفت بعد</label><div class="box"><span>👤</span>سارا نادری — صندوقدار عصر</div></div>'
    + '<div class="fld"><label>یادداشت</label><div class="box"><span>📝</span>کاهش ۵۰ هزار ریالی — خوانده شد</div></div></div></div>'
    + '<div style="flex:0 0 415px">'
    + '<div class="panel" style="background:#16355E;border-color:#16355E;color:#fff;margin-bottom:10px">'
    + '<div class="ph"><b style="color:#fff">🧾 رسید تحویل شیفت</b><small style="color:#9FB6D4">شماره ترتیبی SHR-0087</small></div>'
    + '<div style="padding:2px 14px 12px;font-size:12.5px;line-height:2.3;color:#DCE9FA">'
    + 'شنبه ۱۴۰۵/۰۷/۱۳ — شیفت صبح<br>نقد شمرده‌شده: <b style="color:#fff">۱۲٬۴۰۰٬۰۰۰ ریال</b><br>'
    + 'کارت تأییدشده: <b style="color:#fff">۲۱٬۸۰۰٬۰۰۰ ریال</b><br>اختلاف نقد: <b style="color:#FFCC80">−۵۰٬۰۰۰ (کم)</b><br>'
    + 'تحویل به: سارا نادری<br><span style="font-size:10.5px;color:#9FB6D4">امضای تحویل‌دهنده / تحویل‌گیرنده</span></div></div>'
    + '<div class="panel"><div class="ph"><b>شیفت‌های بسته‌شده اخیر</b><small>تاریخچه تحویل</small></div>'
    + base.table_panel("", ["تاریخ", "صندوقدار", "جمع (ریال)", "اختلاف"],
        [("۱۴۰۵/۰۷/۱۲", "ف. اکبری", "۳۸٬۹۰۰٬۰۰۰", B.format("green", "مطابقت")),
         ("۱۴۰۵/۰۷/۱۱", "س. نادری", "۴۱٬۲۰۰٬۰۰۰", B.format("green", "مطابقت")),
         ("۱۴۰۵/۰۷/۱۰", "ف. اکبری", "۳۹٬۷۵۰٬۰۰۰", B.format("amber", "+۱۲٬۰۰۰"))])
    + '</div></div></div>'
    + base.actions_row().replace('💾 ذخیره', '🌙 بستن شیفت و چاپ رسید').replace('🗑 حذف', '').replace('🖨 چاپ', '')), kind="win")

# ═══════════════ ۵۸ — تسویه حساب بیمه (نما) ═══════════════
add("insurance-settlement", "تسویه حساب بیمه", shot_shell(
    "بیمه — تسویه حساب", "درمانگاه سهند — تسویه بیمه (Fractionment)", "🛡 بیمه",
    '<div class="chips" style="margin-bottom:9px"><span class="chip">🏥 بیمه‌گران</span>'
    + '<span class="chip">📋 تعرفه‌ها</span><span class="chip on" style="background:#1565C0;color:#fff">⚖️ تسویه بیمه</span>'
    + '<span class="chip">📄 صورتحساب بیمه</span></div>'
    + base.toolbar([("محاسبه صورتحساب", "🧮", "pri"), ("مدیریت قواعد تسویه", "⚙️", "sec"), ("خروجی EXCEL بیمه", "📗", "sec")], "بیمه‌گر…")
    + base.stat_row([
        ("سهم بیمه‌گر (ماه)", "۲۸۴٬۶۰۰٬۰۰۰", "▲ ۸۲ فاکتور", "c1", "🏥"),
        ("سهم بیماران", "۹۱٬۳۰۰٬۰۰۰", "▲ فرانشیز اعمال شد", "c3", "🧑‍🤝‍🧑"),
        ("قواعد فعال", "۱۴", "▲ ۴ سطح اولویت", "c2", "⚖️"),
        ("صورتحساب صادره", "۹", "▲ ارسال به بیمه‌گر", "c4", "📄")])
    + base.table_panel("تفکیک سهم بیمه/بیمار به‌ازای هر قلم — اولویت ۴سطحی (قاعده اختصاصی ← عمومی ← تعرفه ← قرارداد)",
        ["فاکتور", "خدمت", "مبلغ کل", "سهم بیمه‌گر", "فرانشیز", "سهم بیمار", "قاعده اعمالی"],
        [("INV-3312", "ویزیت متخصص", "۳۵۰٬۰۰۰", "۲۸۰٬۰۰۰", "—", "۷۰٬۰۰۰", B.format("blue", "۸۰٪ عمومی")),
         ("INV-3313", "آزمایش کامل", "۱٬۲۰۰٬۰۰۰", "۹۶۰٬۰۰۰", "۴۸٬۰۰۰", "۱۹۲٬۰۰۰", B.format("purple", "۹۰٪ اختصاصی")),
         ("INV-3318", "سونوگرافی", "۹۵۰٬۰۰۰", "۶۶۵٬۰۰۰", "۹۵٬۰۰۰", "۱۹۰٬۰۰۰", B.format("amber", "۷۰٪ تعرفه")),
         ("INV-3321", "فیزیوتراپی", "۴۸۰٬۰۰۰", "۳۳۶٬۰۰۰", "—", "۱۴۴٬۰۰۰", B.format("green", "۷۰٪ قرارداد"))],
        small="سهم‌ها بر اساس قواعد قراردادی + فرانشیز + سقف قلم محاسبه می‌شوند")))

# ═══════════════ ۵۹ — خط زمان پرونده بیمار (پنجره) ═══════════════
add("patient-timeline", "خط زمان پرونده بیمار", shot_window(
    "پرونده بیمار — زهرا محمدی", "MR-1042 · ۳۴ ساله · خط زمان رویدادهای ۷ نوع", "🕐",
    '<div class="chips" style="margin-bottom:9px"><span class="chip">📋 خلاصه</span><span class="chip">🩺 ویزیت‌ها</span>'
    + '<span class="chip">💊 نسخه‌ها</span><span class="chip">🧪 آزمایش‌ها</span><span class="chip">🧾 فاکتورها</span>'
    + '<span class="chip on" style="background:#1565C0;color:#fff">🕐 خط زمان</span></div>'
    + '<div style="background:#FFEBEE;border:1.5px solid #FFCDD2;border-radius:10px;padding:10px 14px;margin-bottom:10px;display:flex;gap:10px;align-items:center">'
    + '<span style="font-size:17px">🚨</span><div style="font-size:12px;line-height:1.8"><b style="color:#C62828">نوار نتایج غیرعادی پاراکلینیک:</b> '
    + 'TSH <b>۹٫۴ mIU/L</b> (نرمال ۰٫۴–۴٫۰) <span style="background:#FFCDD2;color:#B71C1C;border-radius:8px;padding:1px 8px;font-weight:800">H</span> — '
    + 'ویتامین D <b>۱۱ ng/mL</b> <span style="background:#FFCDD2;color:#B71C1C;border-radius:8px;padding:1px 8px;font-weight:800">L</span> — ۱۴۰۵/۰۶/۲۸</div></div>'
    + '<div class="chips" style="margin-bottom:8px"><span class="chip on">همه رویدادها</span><span class="chip">ویزیت</span>'
    + '<span class="chip">آزمایش</span><span class="chip">تصویر</span><span class="chip">نسخه</span><span class="chip">فاکتور</span><span class="chip">سال ۱۴۰۵</span></div>'
    + '<div class="panel"><div class="ph"><b>🕐 خط زمان — ادغام یکپارچه ۷ نوع رویداد با برچسب شمسی</b><small>فیلتر نوع و سال</small></div>'
    + '<div style="display:flex;gap:26px;padding:6px 16px 12px">'
    + '<div class="tl" style="flex:1">'
    + '<div class="tli" style="color:#1565C0"><div class="d">۱۴۰۵/۰۷/۱۲ — 🩺 ویزیت</div><div class="t">ویزیت پیگیری — دکتر کریمی</div>'
    + '<div class="s">شکایت: کنترل تیروئید · تشخیص: E07.9 · نسخه صادر شد</div></div>'
    + '<div class="tli" style="color:#00897B"><div class="d">۱۴۰۵/۰۷/۱۰ — 💊 نسخه</div><div class="t">نسخه #P-1204 — دارویی</div>'
    + '<div class="s">لووتیروکسین ۵۰mcg روزانه · بدون هشدار تداخل</div></div>'
    + '<div class="tli" style="color:#F9A825"><div class="d">۱۴۰۵/۰۶/۲۸ — 🧪 آزمایش</div><div class="t">پنل تیروئید — ۲ یافته خارج از محدوده</div>'
    + '<div class="s">TSH بالا (H) · ویتامین D پایین (L) — نمونه‌گیری صبح</div></div>'
    + '<div class="tli" style="color:#6A1B9A"><div class="d">۱۴۰۵/۰۶/۲۸ — 🧾 فاکتور</div><div class="t">INV-3212 — ۴۵۰٬۰۰۰ ریال</div>'
    + '<div class="s">سهم بیمه ۸۰٪ · تسویه‌شده</div></div>'
    + '<div class="tli" style="color:#43A047"><div class="d">۱۴۰۵/۰۵/۱۴ — 💳 پرداخت</div><div class="t">۹۰٬۰۰۰ ریال — کارت‌خوان</div><div class="s">رسید PAY-8871</div></div>'
    + '</div>'
    + '<div class="panel" style="flex:0 0 415px;margin:0"><div class="ph"><b>خلاصه آماری</b><small>۱۸ ماه اخیر</small></div>'
    + '<div style="padding:0 14px 12px;font-size:12.5px;line-height:2.4">'
    + '<div style="display:flex;justify-content:space-between"><span>🩺 ویزیت‌ها</span><b>۹</b></div>'
    + '<div style="display:flex;justify-content:space-between"><span>💊 نسخه‌ها</span><b>۱۲</b></div>'
    + '<div style="display:flex;justify-content:space-between"><span>🧪 آزمایش‌ها</span><b>۷</b></div>'
    + '<div style="display:flex;justify-content:space-between"><span>🧾 فاکتورها</span><b>۱۱</b></div>'
    + '<div style="display:flex;justify-content:space-between"><span>💳 پرداخت‌ها</span><b>۱۰</b></div>'
    + '<div style="display:flex;justify-content:space-between"><span>🚨 نتایج غیرعادی باز</span><b style="color:#C62828">۲</b></div>'
    + '</div></div></div></div>'), kind="win")

# ═══════════════ ۶۰ — طراح قالب چاپ (پنجره) ═══════════════
add("print-designer", "طراح قالب چاپ سفارشی", shot_window(
    "طراح قالب چاپ", "قالب فاکتور A5 — پیش‌فرض درمانگاه", "🖨",
    '<div class="panels2">'
    + '<div class="panel" style="flex:0 0 350px"><div class="ph"><b>قالب‌ها</b><small>۹ نوع سند</small></div>'
    + '<div style="padding:0 10px 12px">'
    + '<div class="lsn" style="border-color:#90CAF9;background:#F4F9FF"><span class="ic">🧾</span><div><b>فاکتور — رسمی A5</b><small>فعال · پیش‌فرض</small></div><span class="done">فعال</span></div>'
    + '<div class="lsn"><span class="ic">💊</span><div><b>نسخه — سربرگ پزشک</b><small>A5</small></div><span class="pending">غیرفعال</span></div>'
    + '<div class="lsn"><span class="ic">🧾</span><div><b>رسید پرداخت — کوتاه</b><small>A6</small></div><span class="pending">غیرفعال</span></div>'
    + '<div class="lsn"><span class="ic">📄</span><div><b>صورتحساب بیمه</b><small>A4</small></div><span class="pending">غیرفعال</span></div></div>'
    + '<div class="toolbar" style="padding:0 10px 4px"><button class="btn pri">➕ قالب جدید</button>'
    + '<button class="btn sec">📋 کپی</button></div></div>'
    + '<div class="panel" style="flex:0 0 430px"><div class="ph"><b>ویرایشگر HTML با پالت متغیر</b><small>{{نام_درمانگاه}} … {{مبلغ_کل}}</small></div>'
    + '<div style="background:#0F1E33;color:#D7E4F5;border-radius:10px;padding:13px 15px;font-family:Consolas,monospace;font-size:11.5px;line-height:2;direction:ltr;text-align:left">'
    + '&lt;div class=<span style="color:#FFCC80">"header"</span>&gt;<br>'
    + '&nbsp;&nbsp;{{<span style="color:#80CBC4">نام_درمانگاه</span>}} — {{<span style="color:#80CBC4">تلفن</span>}}<br>'
    + '&lt;/div&gt;<br>&lt;table&gt;<br>'
    + '&nbsp;&nbsp;{{<span style="color:#80CBC4">ردیف‌های_خدمات</span>}}<br>'
    + '&lt;/table&gt;<br>&lt;div class=<span style="color:#FFCC80">"total"</span>&gt;'
    + '{{<span style="color:#80CBC4">مبلغ_کل</span>}} ریال&lt;/div&gt;</div>'
    + '<div class="chips" style="margin-top:9px"><span class="chip">{{نام_بیمار}}</span><span class="chip">{{شماره_فاکتور}}</span>'
    + '<span class="chip">{{تاریخ_شمسی}}</span><span class="chip">{{مبلغ_کل}}</span></div></div>'
    + '<div class="panel grow"><div class="ph"><b>پیش‌نمایش زنده با داده نمونه</b><small>A5 — سربرگ رسمی</small></div>'
    + '<div style="background:#fff;border:1px solid #E1E8F2;border-radius:9px;padding:16px 18px;margin:0 14px 14px;font-size:12px;line-height:2">'
    + '<div style="display:flex;justify-content:space-between;border-bottom:2px solid #16355E;padding-bottom:8px">'
    + '<b style="color:#16355E;font-size:14px">درمانگاه تخصصی سهند</b><span style="font-size:10.5px;color:#8296B3">تلفن: ۳۴۴۳۶۱۴۱-۰۴۱</span></div>'
    + '<div style="text-align:center;margin:8px 0;color:#1565C0;font-weight:800">صورتحساب بیمار</div>'
    + '<div style="font-size:11px">زهرا محمدی — INV-3312 — ۱۴۰۵/۰۷/۱۲</div>'
    + '<div style="border-top:1px dashed #B0BEC5;margin-top:8px;padding-top:8px;display:flex;justify-content:space-between">'
    + '<span>ویزیت متخصص</span><b>۳۵۰٬۰۰۰ ریال</b></div>'
    + '<div style="display:flex;justify-content:space-between;margin-top:6px"><span>آزمایش کامل</span><b>۱٬۲۰۰٬۰۰۰ ریال</b></div>'
    + '<div style="display:flex;justify-content:space-between;border-top:2px solid #16355E;margin-top:9px;padding-top:7px">'
    + '<b>جمع کل</b><b style="color:#1565C0">۱٬۵۵۰٬۰۰۰ ریال</b></div></div></div></div>'
    + base.actions_row().replace('🗑 حذف', '👁 پیش‌نمایش مرورگر')), kind="win")

# ═══════════════ ۶۱ — آرشیو سالانه (نما) ═══════════════
add("archive-yearly", "آرشیو سالانه", shot_shell(
    "تنظیمات — آرشیو سالانه", "درمانگاه سهند — آرشیو سالانه", "⚙️ تنظیمات",
    '<div class="chips" style="margin-bottom:9px"><span class="chip on" style="background:#1565C0;color:#fff">🗄 آرشیو سالانه</span>'
    + '<span class="chip">🏢 تنظیمات کلینیک</span><span class="chip">🧾 لاگ حسابرسی</span><span class="chip">👔 پرسنل</span></div>'
    + '<div class="toolbar">'
    + '<span style="font-size:12.5px;font-weight:700;align-self:center">سال شمسی آرشیو (قدیمی‌تر از):</span>'
    + '<span class="tag" style="padding:8px 16px;background:#E3F2FD;border-color:#90CAF9;color:#0D47A1">۱۴۰۳</span>'
    + '<button class="btn sec">👀 پیش‌نمایش شمارشی</button><button class="btn pri">▶ اجرای آرشیو</button>'
    + '<span class="chk" style="margin-right:8px">حذف فیزیکی (پس از ثبت نسخه آرشیو)</span></div>'
    + '<div style="background:#F1F8FF;border:1.5px solid #CFE4FB;border-radius:11px;padding:11px 15px;margin-bottom:10px;font-size:12px;line-height:2.1;color:#37474F">'
    + '<b style="color:#1565C0">پیش‌نمایش سال ۱۴۰۳</b> — برش از ۱۴۰۳/۰۱/۰۱ (۲۰۲۴-۰۳-۲۰):<br>'
    + '• نوبت‌ها: ۴٬۲۱۲ ردیف قابل انتقال (۳۱ مسدود به‌دلیل ارجاع جدیدتر) · پذیرش‌ها: ۳٬۸۹۰ (۱۲ مسدود)<br>'
    + '• ویزیت‌ها: ۳٬۷۵۵ (۸ مسدود) · فاکتورها: ۲٬۱۰۴ (۴۷ مسدود) · پرداخت‌ها: ۱٬۹۸۸ · نسخه‌ها: ۲٬۶۷۱ · آزمایش‌ها: ۱٬۴۰۹<br>'
    + '<b>مجموع: ۲۰٬۰۲۹ قابل انتقال، ۹۸ مسدود</b> — ردیف‌های مسدود به داده جدیدتر ارجاع دارند و هرگز منتقل نمی‌شوند</div>'
    + base.table_panel("مرورگر آرشیو — جستجوی پرونده قدیمی بیمار",
        ["مقصد", "بیمار", "شرح رکورد", "تاریخ", "سال"],
        [("نوبت", "علی رضایی", "نوبت APT-2201 — تکمیل شده", "۱۴۰۲/۱۱/۰۴", "۱۴۰۲"),
         ("ویزیت", "مریم احمدی", "ویزیت — کمردرد", "۱۴۰۲/۰۹/۱۸", "۱۴۰۲"),
         ("فاکتور", "حسین کاظمی", "فاکتور INV-1187 — ۸۵۰٬۰۰۰ ریال", "۱۴۰۲/۰۸/۰۲", "۱۴۰۲"),
         ("آزمایش", "زهرا محمدی", "آزمایش LAB-556 — تأییدشده", "۱۴۰۲/۰۶/۲۳", "۱۴۰۲"),
         ("نسخه", "علی رضایی", "نسخه P-0912 — دارویی", "۱۴۰۲/۰۵/۱۰", "۱۴۰۲")],
        small="تصویر کامل JSON هر ردیف در ArchiveRecords با حفظ ارجاع بیمار — بانک عملیاتی سبک می‌ماند")))

# ═══════════════ ۶۲ — بکاپ ابری (نما) ═══════════════
add("cloud-backup", "پشتیبان ابری", shot_shell(
    "بکاپ و بازیابی — پشتیبان ابری", "درمانگاه سهند — بکاپ ابری رمزنگاری‌شده", "💾 بکاپ و بازیابی",
    base.toolbar([("بکاپ فوری", "💾", "pri"), ("بازیابی از فایل", "♻️", "sec"), ("تنظیم زمان‌بندی", "⏰", "sec")])
    + base.stat_row([
        ("آخرین بکاپ کامل", "امروز ۰۳:۰۰", "▲ موفق", "c2", "✅"),
        ("حجم آخرین بکاپ", "۸۴۲ مگابایت", "▲ فشرده", "c1", "💾"),
        ("بکاپ‌های نگهداری‌شده", "۳۰", "▲ چرخش خودکار", "c3", "🗂"),
        ("زمان‌بندی", "روزانه ۰۳:۰۰", "▲ فعال", "c4", "⏰")])
    + '<div class="cloudcard"><div style="display:flex;align-items:center;gap:10px">'
    + '<span style="font-size:21px">☁️</span><b style="font-size:14.5px;color:#16355E">پشتیبان ابری</b>'
    + '<span class="pill ok">فعال • رمزنگاری‌شده</span>'
    + '<div style="flex:1"></div>'
    + '<button class="btn sec" style="padding:6px 14px">🔌 تست اتصال</button>'
    + '<button class="btn sec" style="padding:6px 14px">☁️ بارگذاری آخرین نسخه</button></div>'
    + '<div class="cloudgrid">'
    + '<div class="ci"><div class="l">آخرین بارگذاری ابری</div><div class="v">۱۴۰۵/۰۷/۱۳ ۰۳:۰۹</div></div>'
    + '<div class="ci"><div class="l">نسخه‌های ابری</div><div class="v">۱۴ نسخه</div></div>'
    + '<div class="ci"><div class="l">حجم کل ابری</div><div class="v">۱۱٫۲ GB</div></div>'
    + '<div class="ci"><div class="l">مقصد</div><div class="v">آروان Object Storage</div></div>'
    + '<div class="ci"><div class="l">رمزنگاری</div><div class="v" style="color:#2E7D32">AES-256-GCM ✓</div></div>'
    + '</div>'
    + '<div style="background:#E8F5E9;border-right:4px solid #43A047;border-radius:8px;padding:9px 13px;margin-top:11px;font-size:11.5px">'
    + '<b>✅ تست رمزگشایی آخرین نسخه موفق بود</b> — ۸۸۴ مگابایت سالم (۱۴۰۵/۰۷/۱۲ ۱۰:۱۵) — بدون دست‌زدن به بانک اصلی</div></div>'
    + base.table_panel("تاریخچه پشتیبان‌گیری", ["زمان", "نوع", "حجم", "محل نگهداری", "رمزنگاری", "وضعیت"],
        [("۱۴۰۵/۰۷/۱۳ ۰۳:۰۰", B.format("blue", "کامل"), "۸۴۲ MB", B.format("purple", "☁ ابر + محلی"), "AES-256", B.format("green", "موفق")),
         ("۱۴۰۵/۰۷/۱۲ ۰۳:۰۰", B.format("blue", "کامل"), "۸۳۸ MB", B.format("purple", "☁ ابر + محلی"), "AES-256", B.format("green", "موفق")),
         ("۱۴۰۵/۰۷/۱۲ ۱۲:۰۰", B.format("purple", "افتراقی"), "۳۸ MB", "درایو محلی E:", "—", B.format("green", "موفق"))],
        small="بارگذاری خودکار شبانه + پاک‌سازی نسخه‌های فراتر از سهمیه")))

# ═══════════════ ۶۳ — ایمنی دارویی هنگام نسخه‌نویسی (پنجره) ═══════════════
add("drug-safety", "ایمنی دارویی — هشدارهای زنده", shot_window(
    "نسخه‌نویسی — مریم احمدی", "موتور ۷بُعدی ایمنی بالینی — بررسی زنده هنگام ثبت اقلام", "🛡",
    base.form_fields([("مدت درمان", "۷ روز", "📅"), ("توضیح عمومی", "با غذا مصرف شود", "📝")])
    + base.table_panel("اقلام نسخه", ["#", "دارو", "دوز", "بارداری/شیردهی", "تداخل", "قیمت"],
        [("۱", "وارفارین ۵mg", "۱ قرص شب", B.format("amber", "احتیاط"), B.format("red", "بحرانی"), "۳۸٬۰۰۰"),
         ("۲", "آسپرین ۸۰mg", "۱ قرص صبح", B.format("green", "امن"), B.format("red", "بحرانی"), "۱۲٬۰۰۰"),
         ("۳", "امپرازول ۲۰mg", "۱ قرص صبح ناشتا", B.format("green", "امن"), B.format("amber", "توجه"), "۴۲٬۰۰۰")])
    + '<div class="panel" style="background:#FFEBEE;border-color:#FFCDD2;margin-bottom:8px">'
    + '<div class="ph"><b style="color:#C62828">🚨 پنل هشدار ایمنی دارویی — ۲ هشدار بحرانی نیازمند رسیدگی</b></div>'
    + '<div style="padding:0 14px 12px;font-size:12px;line-height:2.15">'
    + '<div style="background:#fff;border:1.5px solid #FFCDD2;border-radius:9px;padding:9px 13px;margin-bottom:7px">'
    + '<b style="color:#C62828">⚠️ تداخل شدید — وارفارین × آسپرین (سطح ۴)</b><br>'
    + 'خطر خونریزی گوارشی و افزایش قابل توجه INR — توصیه: پرهیز از مصرف هم‌زمان یا کاهش دوز با پایش لابراتواری</div>'
    + '<div style="background:#fff;border:1.5px solid #FFE0B2;border-radius:9px;padding:9px 13px">'
    + '<b style="color:#E65100">🩸 احتیاط سنی — سالمند (Beers)</b><br>'
    + 'آسپرین预防ی برای بیمار ۷۴ ساله با سابقه زخم معده: ارزیابی ریسک/فایده لازم است</div>'
    + '<div style="display:flex;gap:8px;align-items:center;margin-top:9px">'
    + '<span style="font-size:11.5px;font-weight:800;color:#C62828">دلیل کتبی برای ادامه با وجود هشدار:</span>'
    + '<input class="inp" style="flex:1" placeholder="مثلاً: با پایش هفتگی INR و پوشش معده ادامه می‌دهم…">'
    + '<button class="btn pri" style="background:#C62828">ثبت دلیل و ادامه</button></div></div></div>'
    + '<div class="chips"><span class="chip">۵۲۳ جفت تداخل مرجع</span><span class="chip">حساسیت متقاطع ATC</span>'
    + '<span class="chip">رده بارداری FDA</span><span class="chip">کلیوی/کبدی</span><span class="chip">مسمومیت سنی</span></div>'
    + base.actions_row()), kind="win")


# ═══════════════ ۶۴ — تحلیل RFM بیماران (پنجره) ═══════════════
add("rfm-analysis", "تحلیل RFM بیماران", shot_window(
    "تحلیل RFM بیماران", "رده‌بندی ۸گانه — امتیاز پنجکی تازگی × تکرار × مبلغ (۵×۵×۵)", "🎯",
    '<div class="panels2">'
    + '<div class="panel" style="flex:0 0 415px">'
    + base.donut_panel("ترکیب رده‌های بیماران", [
        ("قهرمان", 412, ""), ("وفادار", 486, ""), ("بالقوه", 355, ""), ("تازه‌وارد", 268, ""),
        ("در معرض خطر", 197, ""), ("در حال از دست دادن", 142, ""), ("بیهوش", 88, ""), ("از دست رفته", 63, "")],
        unit="بیمار فعال", center="۲٬۰۱۱", sub="از مجموع ۳٬۴۵۲ پرونده")
    + '</div>'
    + '<div class="panel grow"><div class="ph"><b>🏅 قهرمانان — تازگی و تکرار و مبلغ هر سه ۵</b><small>هدف پیامک وفاداری</small></div>'
    + base.table_panel("", ["بیمار", "آخرین مراجعه", "تکرار", "مبلغ کل", "امتیاز RFM"],
        [("مریم احمدی", "۱۴۰۵/۰۷/۱۳", "۲۴", "۱۸.۴م", B.format("green", "۵۵۵")),
         ("حسین کاظمی", "۱۴۰۵/۰۷/۱۰", "۱۹", "۱۴.۱م", B.format("green", "۵۵۵")),
         ("نگار موسوی", "۱۴۰۵/۰۷/۰۹", "۱۶", "۱۱.۸م", B.format("green", "۵۴۵"))])
    + '<div class="ph" style="margin-top:8px"><b>⚠️ در معرض خطر — بیش از ۴ ماه غیبت</b><small>هدف پیامک بازگشت</small></div>'
    + base.table_panel("", ["بیمار", "آخرین مراجعه", "تکرار", "مبلغ کل", "امتیاز RFM"],
        [("رضا حسینی", "۱۴۰۵/۰۲/۱۸", "۹", "۶.۲م", B.format("amber", "۲۴۵")),
         ("سارا محمدی", "۱۴۰۵/۰۱/۰۲", "۵", "۳.۹م", B.format("amber", "۲۳۵"))])
    + '<div class="chips"><span class="chip">📤 ارسال پیامک به قهرمانان</span>'
    + '<span class="chip">📤 پیامک بازگشت به در معرض خطر</span><span class="chip">📗 خروجی اکسل رده‌ها</span></div>'
    + '</div></div>'
    + '<div class="chips"><span class="chip">تازگی: ۵ پنجکی وارونه</span><span class="chip">تکرار: ۵ پنجکی</span>'
    + '<span class="chip">مبلغ: ۵ پنجکی</span><span class="chip">رده ۸گانه قاعده‌محور</span></div>'), kind="win")


def main():
    only = {x for x in sys.argv[1:]} if len(sys.argv) > 1 else None
    with sync_playwright() as p:
        browser = p.chromium.launch(args=["--force-color-profile=srgb"])
        page = browser.new_page(viewport={"width": W, "height": H}, device_scale_factor=1)
        made = 0
        for i, spec in enumerate(SPECS, start=53):
            key = spec["key"]
            if only and key not in only:
                continue
            if spec["kind"] == "view":
                html = spec["html"]
                name = f"{i:02d}-{key}.png"
            else:
                html = spec["html"]
                name = f"{i:02d}-{key}-win.png"
            src = TMP_DIR / f"{i:02d}.html"
            src.write_text(html, encoding="utf-8")
            page.goto(src.resolve().as_uri(), wait_until="networkidle")
            page.reload(wait_until="networkidle")
            page.evaluate("() => document.fonts.ready.then(() => true)")
            page.wait_for_timeout(250)
            fonts_ok = page.evaluate("() => document.fonts.check('12px Vazirmatn')")
            if not fonts_ok:
                print(f"  ⚠ هشدار: فونت وزیرمتن در {name} فعال نشد!")
            out = OUT_DIR / name
            page.screenshot(path=str(out), clip={"x": 0, "y": 0, "width": W, "height": H})
            made += 1
            print(f"  ✔ {name}  ({out.stat().st_size // 1024} KB){'' if fonts_ok else ' — بدون فونت!'}")
        browser.close()
    print(f"\n📸 {made} اسکرین‌شات چرخه سیزدهم در {OUT_DIR} تولید شد")


if __name__ == "__main__":
    sys.exit(main())
