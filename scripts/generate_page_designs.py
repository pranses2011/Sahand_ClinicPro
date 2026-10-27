#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
════════════════════════════════════════════════════════════════════════════
 تولید شش «طرح جدید» برای صفحات هدف — همان سبک تصویربرداری تأییدشدهٔ مالک
════════════════════════════════════════════════════════════════════════════
پس از آنکه مالک گفت طرحهای قبلی شبیه طرحهای تأییدیاش نیستند و خواست
«دوباره یکییکی از اول طراحی» شود، این اسکریپت شش طرح تازه میسازد که همگی از
یک سیستم طراحی واحد پیروی میکنند (همان کارتهای آماری پنجگانه، چیپها، دکمههای
تخت، تبها و جداول صفحهای تصویربرداری که با اسکرینشات واقعی تأیید شد).

بوم هر طرح دقیقاً ۱۰۳۰×۷۷۴ پیکسل است — همان اندازهای که workflow ویندوزی
از صفحهٔ واقعی WPF اسکرینشات میگیرد — بنابراین مقایسهٔ «طرح / واقعیت»
تصویربهتصویر و تا حد امکان پیکسلبهپیکسل انجام میشود.

خروجی: docs/design/approved/*.html (و base.css کنارشان)
اجرا:   python3 scripts/generate_page_designs.py
"""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs" / "design" / "approved"
OUT.mkdir(parents=True, exist_ok=True)


def page(key: str, title: str, sub: str, icon: str, actions: str, body: str) -> str:
    return f"""<!DOCTYPE html>
<html lang="fa" dir="rtl"><head><meta charset="utf-8">
<title>{title} — طرح پیشنهادی</title>
<link rel="stylesheet" href="base.css">
</head><body data-page="{key}">
<div class="top">
  <div class="actions">{actions}</div>
  <div class="heading">
    <div><div class="ttl">{title}</div><div class="sub">{sub}</div></div>
    <div class="badge-ico">{icon}</div>
  </div>
</div>
{body}
<div class="foot-strip">سهند کلینیکپرو — طرح پیشنهادی {title} | بوم ۱۰۳۰×۷۷۴ | سبک تأییدشدهٔ تصویربرداری</div>
</body></html>
"""


def stats(items: list[tuple[str, str, str]]) -> str:
    """(رنگ, عدد, برچسب) — پنج کارت تمامعرض."""
    cells = "".join(
        f'<div class="stat {c}"><b>{v}</b><span>{t}</span></div>' for c, v, t in items
    )
    return f'<div class="stats">{cells}</div>'


def tabs(names: list[str], active: int = 0) -> str:
    return '<div class="tabs">' + "".join(
        f'<div class="tab {"on" if i == active else ""}">{n}</div>'
        for i, n in enumerate(names)
    ) + "</div>"


BTN_NEW = '<span class="btn primary">＋ نظرسنجی جدید</span>'

PAGES: dict[str, str] = {}

# ─────────────────────────────────────────────────────────────────────────────
# ۱) دندانپزشکی — چارت FDI، وضعیت دندانها، جزئیات و طرح درمان
# ─────────────────────────────────────────────────────────────────────────────
def teeth_row(numbers: list[int], colors: dict[int, str]) -> str:
    out = []
    for n in numbers:
        c = colors.get(n, "#FFFFFF")
        border = "#E53935" if c != "#FFFFFF" else "#C9D6E4"
        out.append(
            f'<div style="width:46px;height:46px;border:1.5px solid {border};background:{c};'
            f'border-radius:7px;display:flex;flex-direction:column;align-items:center;'
            f'justify-content:center;font-size:11px;font-weight:800;color:#22364F">'
            f'{str(n).translate(str.maketrans("0123456789","۰۱۲۳۴۵۶۷۸۹"))}'
            f'<span style="font-size:7.5px;font-weight:500;color:#6B7C93">پیشین</span></div>'
        )
    return '<div style="display:flex;gap:4px;justify-content:center">' + "".join(out) + "</div>"


upper1 = [18, 17, 16, 15, 14, 13, 12, 11]
upper2 = [21, 22, 23, 24, 25, 26, 27, 28]
lower1 = [48, 47, 46, 45, 44, 43, 42, 41]
lower2 = [31, 32, 33, 34, 35, 36, 37, 38]
colors = {16: "#E53935", 14: "#FDD835", 26: "#43A047", 36: "#1E88E5", 46: "#8E24AA"}

tooth_chart = f"""
<div class="panel" style="padding:8px 10px">
  <div class="row" style="justify-content:space-between;margin-bottom:6px">
    <div class="chips">
      <span class="chip on">همه</span><span class="chip">نیازمند درمان</span>
      <span class="chip sq">راهنما:</span>
      <span class="chip sq">🟥 پوسیدگی</span><span class="chip sq">🟨 ترمیمشده</span>
      <span class="chip sq">🟩 درمانشده</span><span class="chip sq">🟦 روکش</span>
    </div>
    <div class="note">چارت دندان — شمارهگذاری FDI</div>
  </div>
  <div style="display:flex;gap:8px;justify-content:center;margin:5px 0">
    <div style="flex:1;display:flex;flex-direction:column;gap:6px;align-items:center">
      <div class="note" style="font-weight:700;color:#31527A">فک بالا — راست</div>
      {teeth_row(upper1, colors)}{teeth_row(upper2, colors)}
    </div>
    <div style="width:1px;background:#E3EAF2"></div>
    <div style="flex:1;display:flex;flex-direction:column;gap:6px;align-items:center">
      <div class="note" style="font-weight:700;color:#31527A">فک پایین — چپ</div>
      {teeth_row(lower1, colors)}{teeth_row(lower2, colors)}
    </div>
  </div>
</div>"""

PAGES["dentistry"] = page(
    "dentistry", "پروندهٔ دندانپزشکی", "چارت دندان، طرح درمان و پرتونگاری پیوندخورده — یکپارچه", "🦷",
    '<span class="btn ghost">🖨️ چاپ چارت</span><span class="btn primary">＋ پرونده جدید</span>',
    stats([("g", "۱۲۴", "بیماران دارای پرونده"), ("r", "۱۸", "دندان نیازمند درمان"),
           ("p", "۷", "طرح درمان فعال"), ("o", "۵", "جلسات امروز"), ("b", "۴۸٫۵M", "درآمد ماه (ریال)")])
    + tabs(["چارت دندان", "طرح درمان", "پرتونگاری", "نسخه و رضایتنامه"])
    + '<div class="panel" style="padding:8px 10px;margin-bottom:8px">'
      '<div class="row"><div class="field ico col" style="max-width:300px">جستجوی بیمار (کد ملی / موبایل)</div>'
      '<div class="field col" style="max-width:190px">زهرا محمدی — کد ۰۰۱۲۳۴</div>'
      '<span class="btn primary sm">جستجو ⌕</span><div class="sp"></div>'
      '<span class="note">آخرین ویزیت: ۱۴۰۵/۰۸/۰۳</span></div></div>'
    + '<div style="display:flex;gap:8px">'
      f'<div style="flex:1.55">{tooth_chart}</div>'
      '<div style="flex:1;display:flex;flex-direction:column;gap:8px">'
        '<div class="panel"><h3>🔍 دندان انتخابی: ۱۶</h3>'
        '<div class="kv"><span>جنس دندان</span><b>آسیاب بزرگ اول · ۳ ریشه · ۴ کانال</b></div>'
        '<div class="kv"><span>وضعیت</span><b style="color:#D8514B">پوسیدگی عمیق</b></div>'
        '<div class="kv"><span>سطح درگیر</span><b>M ☐ · D ☐ · O ☑ · B ☐ · L ☐</b></div>'
        '<div class="field sm" style="margin-top:7px">انتخاب خدمت از کاتالوگ…</div>'
        '<div class="row tight" style="margin-top:7px"><span class="btn sm">ثبت وضعیت</span>'
        '<span class="btn dark sm">＋ افزودن به طرح درمان</span></div></div>'
        '<div class="panel"><h3>📋 طرح درمان <span class="tag b" style="margin-inline-start:auto">۳ قلم</span></h3>'
        '<table class="tbl"><tr><th>#</th><th>دندان</th><th>درمان</th><th>سطح</th><th>هزینه</th><th>وضعیت</th></tr>'
        '<tr><td>۱</td><td>۱۶</td><td>درمان ریشه (۴ کانال)</td><td>O</td><td>۴٬۵۰۰٬۰۰۰</td><td><span class="tag o">در انتظار</span></td></tr>'
        '<tr><td>۲</td><td>۱۴</td><td>ترمیم کامپوزیت</td><td>M</td><td>۱٬۲۰۰٬۰۰۰</td><td><span class="tag g">انجامشده</span></td></tr>'
        '<tr><td>۳</td><td>۳۶</td><td>روکش سرامیکی</td><td>—</td><td>۸٬۰۰۰٬۰۰۰</td><td><span class="tag n">بدون نوبت</span></td></tr></table>'
        '<div class="kv" style="margin-top:6px"><span>جمع کل طرح درمان</span><b>۱۳٬۷۰۰٬۰۰۰ ریال</b></div>'
        '<span class="btn primary wide" style="margin-top:7px">ثبت و چاپ طرح درمان</span></div>'
      '</div></div>'
)

# ─────────────────────────────────────────────────────────────────────────────
# ۲) چشمپزشکی — رفرکشن، فشار چشم، یافتهها و نسخه
# ─────────────────────────────────────────────────────────────────────────────
eye_card = lambda side, sph, cyl, ax, add, iop, warn=False: f"""
<div class="panel" style="flex:1">
  <h3>{side}</h3>
  <div class="row tight" style="margin-bottom:6px"><div class="col"><span class="lbl">Sphere</span><div class="field sm">{sph}</div></div>
  <div class="col"><span class="lbl">Cylinder</span><div class="field sm">{cyl}</div></div>
  <div class="col"><span class="lbl">Axis</span><div class="field sm">{ax}</div></div></div>
  <div class="row tight" style="margin-bottom:6px"><div class="col"><span class="lbl">Add</span><div class="field sm">{add}</div></div>
  <div class="col"><span class="lbl">دید بدون عینک</span><div class="field sm">۲۰/۴۰</div></div>
  <div class="col"><span class="lbl">دید با عینک</span><div class="field sm">۲۰/۲۰</div></div></div>
  <div class="lbl">فشار چشم (IOP) — mmHg</div>
  <div class="field sm" style="border-color:{'#D8514B' if warn else '#C7D5E3'};color:{'#D8514B' if warn else '#1B2A41'}">{iop} mm Hg</div>
  <div style="margin-top:6px"><div class="bar {'r' if warn else 'g'}"><i style="width:{80 if warn else 45}%"></i></div>
  <div class="note" style="color:{'#D8514B' if warn else '#2E9E5B'}">{'⚠️ بالاتر از ۲۱ — احتمال گلوکوم' if warn else '✓ در محدودهٔ طبیعی'}</div></div>
</div>"""

PAGES["ophthalmology"] = page(
    "ophthalmology", "معاینهٔ چشمپزشکی", "رفرکشن، فشار چشم، اسلیتلمپ و نسخهٔ عینک — پروندهٔ یکپارچه", "👁️",
    '<span class="btn ghost">🖨️ چاپ نسخه</span><span class="btn primary">💾 ذخیره معاینه</span>',
    stats([("g", "۸", "معاینات امروز"), ("r", "۲", "فشار چشم بالا"),
           ("p", "۶", "نسخهٔ عینک امروز"), ("o", "۴", "پیگیریهای هفته"), ("b", "۹۶", "بیماران پروندهدار")])
    + tabs(["رفرکشن و حدت بینایی", "اسلیتلمپ و فوندوس", "تستهای تخصصی", "تصویربرداری چشم", "تشخیص و درمان"])
    + '<div class="panel" style="padding:8px 10px;margin-bottom:8px"><div class="row">'
      '<div class="field ico col" style="max-width:300px">جستجوی بیمار (کد ملی / موبایل)</div>'
      '<div class="field col" style="max-width:190px">علی رضایی — کد ۰۰۲۱۰۵</div>'
      '<span class="btn primary sm">جستجو ⌕</span><div class="sp"></div>'
      '<span class="note">PD: ۶۲ mm · دید رنگ: طبیعی</span></div></div>'
    + '<div class="row" style="align-items:stretch;margin-bottom:8px">'
      + eye_card("👁️ چشم راست (OD)", "-۲٫۵۰", "-۰٫۷۵", "۱۸۰", "+۱٫۵۰", "۱۴")
      + eye_card("👁️ چشم چپ (OS)", "-۳٫۰۰", "-۱٫۰۰", "۱۷۵", "+۱٫۵۰", "۲۳", True)
      + '<div class="panel" style="flex:.8"><h3>👓 نسخهٔ عینک زنده</h3>'
        '<div class="panel" style="background:#F7FAFD;padding:9px;font-size:11px;line-height:2.1">'
        '<div class="kv"><span>OD</span><b>-۲٫۵۰ / -۰٫۷۵ × ۱۸۰</b></div>'
        '<div class="kv"><span>OS</span><b>-۳٫۰۰ / -۱٫۰۰ × ۱۷۵</b></div>'
        '<div class="kv"><span>Add</span><b>+۱٫۵۰</b></div>'
        '<div class="kv"><span>PD</span><b>۶۲ mm</b></div></div>'
        '<div class="note" style="margin-top:7px">نسخه از عددهای رفرکشن ساخته میشود.</div>'
        '<span class="btn primary wide" style="margin-top:7px">چاپ نسخهٔ عینک</span></div>'
    + '</div>'
    + '<div style="display:flex;gap:8px">'
      '<div class="panel" style="flex:1.3"><h3>🔬 یافتهها و تشخیص</h3>'
      '<div class="field" style="height:44px;align-items:flex-start;padding-top:6px">حدت بینایی دو چشم با عینک کامل؛ فشار چشم چپ بالاتر از حد طبیعی — پیگیری گلوکوم.</div>'
      '<div class="row tight" style="margin-top:7px"><div class="col"><span class="lbl">تشخیص</span><div class="field sm">آستیگماتیسم مرکب دوطرفه</div></div>'
      '<div class="col"><span class="lbl">برنامهٔ درمان</span><div class="field sm">OCT عصب بینایی</div></div></div></div>'
      '<div class="panel" style="flex:1"><h3>📈 روند فشار چشم</h3>'
      '<div style="display:flex;align-items:flex-end;gap:12px;height:74px;padding:4px 8px;border-bottom:1px solid #D6E0EA">'
      '<div style="flex:1;background:#8FC9E8;height:36px;border-radius:4px"></div>'
      '<div style="flex:1;background:#8FC9E8;height:40px;border-radius:4px"></div>'
      '<div style="flex:1;background:#D8514B;height:62px;border-radius:4px"></div></div>'
      '<div class="row" style="justify-content:space-between;margin-top:4px"><span class="mini">۱۴۰۴/۱۱</span>'
      '<span class="mini">۱۴۰۵/۰۵</span><span class="mini">۱۴۰۵/۰۸</span></div>'
      '<div class="note" style="margin-top:5px">آخرین معاینه: IOP چپ ۲۳ — نیازمند پیگیری</div></div>'
    + '</div>'
)

# ─────────────────────────────────────────────────────────────────────────────
# ۳) فیزیوتراپی — نقشهٔ بدن، جلسه، پرچمهای قرمز، روند
# ─────────────────────────────────────────────────────────────────────────────
body_map = """
<div class="panel" style="flex:.85;text-align:center">
  <h3 style="justify-content:center">🧍 نقشهٔ بدن — محل درد</h3>
  <div style="display:flex;gap:8px;justify-content:center">
    <div style="position:relative;width:86px;height:180px">
      <div style="position:absolute;top:0;right:31px;width:24px;height:24px;border:2px solid #8AA0B8;border-radius:50%"></div>
      <div style="position:absolute;top:26px;right:23px;width:40px;height:60px;border:2px solid #8AA0B8;border-radius:12px 12px 8px 8px"></div>
      <div style="position:absolute;top:60px;right:27px;width:32px;height:18px;background:#E58A2B;border-radius:5px"></div>
      <div style="position:absolute;top:28px;right:5px;width:16px;height:52px;border:2px solid #8AA0B8;border-radius:8px"></div>
      <div style="position:absolute;top:28px;right:65px;width:16px;height:52px;border:2px solid #8AA0B8;border-radius:8px"></div>
      <div style="position:absolute;top:88px;right:26px;width:16px;height:62px;border:2px solid #8AA0B8;border-radius:6px"></div>
      <div style="position:absolute;top:88px;right:44px;width:16px;height:62px;border:2px solid #8AA0B8;border-radius:6px"></div>
      <div class="mini" style="position:absolute;bottom:0;right:0;width:100%">نمای جلو</div>
    </div>
    <div style="position:relative;width:86px;height:180px">
      <div style="position:absolute;top:0;right:31px;width:24px;height:24px;border:2px solid #8AA0B8;border-radius:50%"></div>
      <div style="position:absolute;top:26px;right:23px;width:40px;height:60px;border:2px solid #8AA0B8;border-radius:12px 12px 8px 8px"></div>
      <div style="position:absolute;top:52px;right:27px;width:32px;height:26px;background:#D8514B;border-radius:5px"></div>
      <div style="position:absolute;top:28px;right:5px;width:16px;height:52px;border:2px solid #8AA0B8;border-radius:8px"></div>
      <div style="position:absolute;top:28px;right:65px;width:16px;height:52px;border:2px solid #8AA0B8;border-radius:8px"></div>
      <div style="position:absolute;top:88px;right:26px;width:16px;height:62px;border:2px solid #8AA0B8;border-radius:6px"></div>
      <div style="position:absolute;top:88px;right:44px;width:16px;height:62px;border:2px solid #8AA0B8;border-radius:6px"></div>
      <div class="mini" style="position:absolute;bottom:0;right:0;width:100%">نمای پشت</div>
    </div>
  </div>
  <div class="chips" style="justify-content:center;margin-top:7px">
    <span class="chip sq">🟩 بدون درد</span><span class="chip sq">🟨 متوسط</span><span class="chip sq">🟥 شدید</span></div>
  <div class="note" style="margin-top:6px">ناحیهٔ انتخابی: <b>کمر (لومبار)</b> — شدت ۴/۱۰</div>
</div>"""

PAGES["physiotherapy"] = page(
    "physiotherapy", "دورهٔ درمان فیزیوتراپی", "نقشهٔ بدن، جلسات درمان و روند بهبود — پروتکل ۱۲ جلسهای", "🏃",
    '<span class="btn ghost">📊 گزارش پیشرفت</span><span class="btn primary">＋ جلسهٔ جدید</span>',
    stats([("g", "۹", "جلسات امروز"), ("r", "۲", "پرچم قرمز"),
           ("p", "۲۴", "بیماران فعال"), ("o", "۴۱", "جلسات باقیمانده"), ("b", "۶۸٪", "میانگین بهبود")])
    + tabs(["جلسهٔ درمان", "نقشهٔ بدن", "ارزیابی و اهداف", "روند پیشرفت"])
    + '<div class="panel" style="padding:8px 10px;margin-bottom:8px"><div class="row">'
      '<div class="field ico col" style="max-width:290px">جستجوی بیمار (کد ملی / موبایل)</div>'
      '<div class="field col" style="max-width:170px">مریم کریمی — جلسهٔ ۵ از ۱۲</div>'
      '<div class="sp"></div><span class="note">پروتکل: کمردرد مزمن — هدف: کاهش درد به زیر ۳</span>'
      '<span class="btn primary sm">شروع جلسه</span></div></div>'
    + '<div class="row" style="align-items:stretch;margin-bottom:8px">'
      + body_map
      + '<div class="panel" style="flex:1.25"><h3>📝 ثبت جلسهٔ پنجم</h3>'
        '<div class="row tight" style="margin-bottom:6px"><div class="col"><span class="lbl">نمرهٔ درد (۰-۱۰)</span><div class="field sm">۴</div></div>'
        '<div class="col"><span class="lbl">مدت جلسه</span><div class="field sm">۴۵ دقیقه</div></div>'
        '<div class="col"><span class="lbl">تاریخ</span><div class="field sm">۱۴۰۵/۰۸/۰۵</div></div></div>'
        '<div class="lbl">تکنیکهای درمانی</div><div class="chips" style="margin-bottom:7px">'
        '<span class="chip on">☑ الکتروتراپی (TENS)</span><span class="chip on">☑ اولتراسوند</span>'
        '<span class="chip on">☑ تمرینات درمانی</span><span class="chip">☐ لیزر درمانی</span>'
        '<span class="chip">☐ ماساژ</span><span class="chip">☐ هیدروتراپی</span></div>'
        '<div class="lbl">یادداشت بالینی</div>'
        '<div class="field" style="height:44px;align-items:flex-start;padding-top:6px">بهبود قابل توجه در خمشدن به جلو؛ ادامهٔ تمرینات ثباتدهندهٔ مرکزی.</div>'
        '<span class="btn primary wide" style="margin-top:8px">ذخیرهٔ جلسه</span></div>'
      + '<div class="panel" style="flex:.8"><h3>🚩 غربالگری پرچمهای قرمز</h3>'
        '<div class="kv"><span>کاهش وزن بیعلت</span><b class="tag g">خیر</b></div>'
        '<div class="kv"><span>تب / عفونت</span><b class="tag g">خیر</b></div>'
        '<div class="kv"><span>بیاختیاری ادرار</span><b class="tag r">بله — ارجاع</b></div>'
        '<div class="kv"><span>سابقهٔ سرطان</span><b class="tag g">خیر</b></div>'
        '<div class="kv"><span>ترومای اخیر</span><b class="tag g">خیر</b></div>'
        '<div class="note" style="margin-top:8px;background:#FFF5E7;border:1px solid #F3C88F;border-radius:7px;padding:6px">⚠️ وجود پرچم قرمز: ارجاع به پزشک متخصص پیشنهاد میشود.</div>'
        '<span class="btn ghost wide" style="margin-top:7px">چاپ گزارش ارجاع</span></div>'
    + '</div>'
    + '<div class="panel"><h3>📈 روند درد و دامنهٔ حرکت (۵ جلسهٔ اخیر)</h3>'
      '<div style="display:flex;align-items:flex-end;gap:18px;height:55px;padding:3px 8px;border-bottom:1px solid #D6E0EA">'
      + "".join(f'<div style="flex:1;background:#2E9E5B;height:{h}px;border-radius:4px 4px 0 0"></div>'
                for h in (50, 43, 35, 28, 21))
      + '</div><div class="row" style="justify-content:space-between;margin-top:3px">'
      + "".join(f'<span class="mini">جلسهٔ {i}</span>' for i in "۱۲۳۴۵")
      + '</div></div>'
)

# ─────────────────────────────────────────────────────────────────────────────
# ۴) نظرسنجی — کارتها، جدول، توزیع ستارگان و میانگین پزشکان (همه یکجا)
# ─────────────────────────────────────────────────────────────────────────────
star_rows = "".join(
    f'<div class="row tight" style="margin-bottom:5px"><span style="width:56px;font-size:10.5px">{stars}</span>'
    f'<div class="bar g col"><i style="width:{w}%"></i></div>'
    f'<span class="mini" style="width:62px;text-align:left">{count} پاسخ ({pct}٪)</span></div>'
    for stars, count, pct, w in [("★★★★★", "۱۴۸", "۴۷", 100), ("★★★★", "۹۶", "۳۱", 65),
                                  ("★★★", "۴۱", "۱۳", 28), ("★★", "۱۷", "۵", 12), ("★", "۱۰", "۳", 7)]
)

doctor_rows = "".join(
    f'<div class="row tight" style="margin-bottom:6px"><span class="avatar">👨‍⚕️</span>'
    f'<div class="col"><div style="display:flex;justify-content:space-between"><b style="font-size:10.5px">{name}</b>'
    f'<span class="mini">{cnt} پاسخ</span></div><div class="bar"><i style="width:{w}%"></i></div></div>'
    f'<span class="chip sq" style="height:23px">{score}</span></div>'
    for name, cnt, score, w in [("دکتر رضایی", "۹۶", "۴٫۸", 96), ("دکتر کریمی", "۶۴", "۴٫۶", 92),
                                 ("دکتر بهادری", "۸۱", "۴٫۳", 86)]
)

PAGES["survey"] = page(
    "survey", "نظرسنجی بیماران", "سنجش رضایت بیماران پس از ویزیت — ارسال لینک پیامکی و تحلیل نتایج", "📝",
    '<span class="btn ghost">📊 نتایج</span><span class="btn ghost">📨 ارسال لینک به ویزیتهای امروز</span>'
    '<span class="btn ghost">✏️ ویرایش</span><span class="btn ghost">⏻ فعال/غیرفعال</span>'
    '<span class="btn primary">＋ نظرسنجی جدید</span>',
    stats([("g", "۴۲٪", "نرخ پاسخگویی"), ("o", "۴٫۲/۵", "میانگین امتیاز"),
           ("b", "۳۱۲", "پاسخ این ماه"), ("p", "۲", "نظرسنجیهای فعال"), ("r", "۱۸", "نظرات اخیر")])
    + tabs(["فهرست نظرسنجیها", "نتایج و آمار", "سازندهٔ سوالات"])
    + '<div class="panel" style="padding:8px 10px;margin-bottom:8px"><div class="row">'
      '<div class="field ico col" style="max-width:330px">جستجو…</div>'
      '<div class="chips"><span class="chip on">همه</span><span class="chip">فعال</span>'
      '<span class="chip">پایان یافته</span><span class="chip">پیشنویس</span></div>'
      '<div class="sp"></div><span class="btn ghost sm">🔄 تازهمسازی</span></div></div>'
    + '<div style="display:flex;gap:8px">'
      '<div style="flex:1.5"><div class="panel" style="padding:8px 10px">'
        '<table class="tbl"><tr><th>عنوان</th><th>توضیح</th><th>سوالات</th><th>پاسخها</th>'
        '<th>میانگین امتیاز</th><th>وضعیت</th></tr>'
        '<tr><td><b>رضایت از ویزیت پزشک</b></td><td>کیفیت ویزیت و برخورد</td><td>۴</td><td>۳۱۲</td><td>۴٫۲/۵</td><td><span class="tag g">فعال</span></td></tr>'
        '<tr><td><b>کیفیت پذیرش و برخورد</b></td><td>نظرسنجی کوتاه پذیرش</td><td>۴</td><td>۱۹۴</td><td>۴٫۵/۵</td><td><span class="tag g">فعال</span></td></tr>'
        '<tr><td><b>نظرسنجی فصلی پاییز</b></td><td>ویژهٔ بیماران مزمن</td><td>۶</td><td>۴۲۸</td><td>۴٫۳/۵</td><td><span class="tag n">پایان یافته</span></td></tr>'
        '<tr><td><b>ارزیابی خدمات آزمایشگاه</b></td><td>زمان انتظار و پاسخدهی</td><td>۵</td><td>۱۲۶</td><td>۳٫۹/۵</td><td><span class="tag o">پیشنویس</span></td></tr></table>'
        '<div class="note" style="margin-top:7px">۴ نظرسنجی · مجموع ۱٬۰۶۰ پاسخ</div></div></div>'
      '<div style="flex:1;display:flex;flex-direction:column;gap:8px">'
        '<div class="panel"><h3>⭐ توزیع امتیازها — رضایت از ویزیت</h3>' + star_rows + '</div>'
        '<div class="panel"><h3>👨‍⚕️ میانگین امتیاز به تفکیک پزشک</h3>' + doctor_rows + '</div>'
      '</div>'
    + '</div>'
    + '<div class="panel" style="margin-top:8px"><h3>💬 نظرات آزاد اخیر</h3>'
      '<div class="row tight"><div class="col field sm">«برخورد بسیار خوب و توضیحات کامل بود.» — زهرا محمدی · دکتر رضایی</div>'
      '<div class="col field sm">«زمان انتظار کمی طولانی بود.» — علی احمدی · دکتر کریمی</div></div></div>'
)

# طرح دوم نظرسنجی (تب نتایج و آمار) — همان زبان طراحی، تمرکز بر آمار
PAGES["survey-results"] = page(
    "survey-results", "نظرسنجی بیماران — نتایج و آمار", "تحلیل پاسخها، توزیع امتیازها و عملکرد پزشکان", "📊",
    '<span class="btn ghost">📤 خروجی اکسل</span><span class="btn primary">🔄 تازهمسازی</span>',
    stats([("g", "۴۲٪", "نرخ پاسخگویی"), ("o", "۴٫۲/۵", "میانگین امتیاز"),
           ("b", "۳۱۲", "پاسخ این ماه"), ("p", "۲", "نظرسنجیهای فعال"), ("r", "۱۸", "نظرات اخیر")])
    + tabs(["فهرست نظرسنجیها", "نتایج و آمار", "سازندهٔ سوالات"], 1)
    + '<div class="panel" style="padding:8px 10px;margin-bottom:8px"><div class="row">'
      '<div class="field col" style="max-width:290px">انتخاب نظرسنجی: رضایت از ویزیت پزشک</div>'
      '<div class="sp"></div><span class="note">بازه: ۳۰ روز اخیر</span>'
      '<span class="btn ghost sm">🔄 بارگذاری مجدد</span></div></div>'
    + '<div class="row" style="align-items:stretch;margin-bottom:8px">'
      '<div class="panel" style="flex:1"><h3>⭐ توزیع امتیازها</h3>' + star_rows
      + '<div class="divider"></div><div class="note">۳۱۲ پاسخ · میانگین ۴٫۲ از ۵</div></div>'
      '<div class="panel" style="flex:1"><h3>👨‍⚕️ میانگین امتیاز پزشکان</h3>' + doctor_rows
      + '<div class="divider"></div><div class="note">۳ پزشک · ۲۴۱ پاسخ ارزیابیشده</div></div>'
      '<div class="panel" style="flex:.8"><h3>📅 روند رضایت</h3>'
      '<div style="display:flex;align-items:flex-end;gap:9px;height:100px;padding:3px 6px;border-bottom:1px solid #D6E0EA">'
      + "".join(f'<div style="flex:1;background:{"#2E9E5B" if i>2 else "#8FC9E8"};height:{h}px;border-radius:4px 4px 0 0"></div>'
                for i, h in enumerate((42, 50, 47, 62, 70, 81)))
      + '</div><div class="row" style="justify-content:space-between;margin-top:4px">'
      + "".join(f'<span class="mini">{m}</span>' for m in ("فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور"))
      + '</div><div class="note" style="margin-top:8px">روند صعودی: +۰٫۴ امتیاز در ۶ ماه</div></div>'
    + '</div>'
    + '<div class="panel"><h3>💬 نظرات آزاد بیماران (۳۰ مورد اخیر)</h3>'
      '<div class="row tight" style="margin-bottom:6px">'
      '<div class="col panel" style="margin:0;background:#F7FAFD"><b>زهرا محمدی</b> · دکتر رضایی · <span class="tag g">۵ ستاره</span>'
      '<div class="note">برخورد بسیار خوب و توضیحات کامل بود. ممنونم.</div></div>'
      '<div class="col panel" style="margin:0;background:#F7FAFD"><b>علی احمدی</b> · دکتر کریمی · <span class="tag b">۴ ستاره</span>'
      '<div class="note">زمان انتظار کمی طولانی بود ولی ویزیت عالی بود.</div></div>'
      '<div class="col panel" style="margin:0;background:#F7FAFD"><b>مریم حسینی</b> · دکتر بهادری · <span class="tag g">۵ ستاره</span>'
      '<div class="note">محیط تمیز و پرسنل مؤدب.</div></div></div></div>'
)

# ─────────────────────────────────────────────────────────────────────────────
# ۵) قالب چاپ — شبکهٔ کارتها (همان زبان تصویربرداری) + طراح
# ─────────────────────────────────────────────────────────────────────────────
def template_card(title: str, kind: str, paper: str, accent: str, active: bool = False) -> str:
    return f"""<div class="panel" style="padding:9px;{('border-color:#1976D2;border-width:2px;' if active else '')}">
  <div class="row" style="justify-content:space-between;margin-bottom:7px">
    <span class="tag {'b' if active else 'n'}">{kind}</span>
    <b style="font-size:11.5px">{title}</b></div>
  <div style="height:78px;border:1px dashed #BDD0E4;border-radius:7px;background:#FAFCFE;padding:7px 9px">
    <div style="height:8px;background:{accent};border-radius:3px;width:60%;margin-bottom:6px"></div>
    <div style="height:3px;background:#DCE6F0;border-radius:2px;margin-bottom:4px"></div>
    <div style="height:3px;background:#DCE6F0;border-radius:2px;width:85%;margin-bottom:4px"></div>
    <div style="height:3px;background:#DCE6F0;border-radius:2px;width:70%;margin-bottom:8px"></div>
    <div style="height:5px;background:#E8EFF7;border-radius:2px"></div></div>
  <div class="row tight" style="margin-top:8px">
    <span class="btn sm {'primary' if active else 'ghost'}">{'✓ پیشفرض' if active else 'انتخاب'}</span>
    <span class="btn ghost sm">ویرایش</span><div class="sp"></div><span class="mini">{paper}</span></div>
</div>"""

cards = "".join([
    template_card("فاکتور — سربرگ A5", "فاکتور", "A5", "#16355E", True),
    template_card("نسخه — سربرگ پزشک", "نسخه", "A5", "#0D9488"),
    template_card("رسید پذیرش کوتاه", "پذیرش", "A6", "#E58A2B"),
    template_card("صورت حساب بیمه", "بیمه", "A4", "#8A5CD1"),
    template_card("نتیجهٔ آزمایش", "آزمایشگاه", "A4", "#2E9E5B"),
    template_card("گزارش تصویربرداری", "تصویربرداری", "A4", "#D8514B"),
])

PAGES["print-template"] = page(
    "print-template", "قالب چاپ و طب مکمل", "کتابخانهٔ قالبهای چاپ، طرحهای آماده و ویرایشگر HTML", "🖨️",
    '<span class="btn ghost">👁️ پیشنمایش</span><span class="btn ghost">⚙️ تنظیم پیشفرض</span>'
    '<span class="btn primary">＋ قالب جدید</span>',
    stats([("g", "۳", "قالبهای فعال"), ("b", "۶", "همهٔ قالبها"),
           ("p", "۲", "قالبهای پیشفرض"), ("o", "۹", "انواع سند"), ("t", "۱۴۰۵/۰۸/۰۳", "آخرین بروزرسانی")])
    + tabs(["کتابخانهٔ قالبها", "طراح HTML", "متغیرها", "همگامسازی داده"])
    + '<div class="panel" style="padding:8px 10px;margin-bottom:8px"><div class="row">'
      '<div class="chips"><span class="chip on">همه موجودیتها</span><span class="chip">فاکتور</span>'
      '<span class="chip">نسخه</span><span class="chip">پذیرش</span><span class="chip">بیمه</span>'
      '<span class="chip">آزمایشگاه</span><span class="chip">تصویربرداری</span></div>'
      '<div class="sp"></div><div class="field sm ico" style="width:210px">جستجوی قالب…</div></div></div>'
    + '<div style="display:grid;grid-template-columns:repeat(3,1fr);gap:8px">' + cards + '</div>'
    + '<div class="panel" style="margin-top:8px;padding:8px 10px"><div class="row">'
      '<span class="note">💡 قالب پیشفرض فعال هنگام چاپ همان سند اعمال میشود؛ با کلیک «انتخاب» میتوانید قالب دیگری را فعال کنید.</span>'
      '<div class="sp"></div><span class="btn ghost sm">کپی قالب</span><span class="btn dark sm">ذخیرهٔ تغییرات</span></div></div>'
)

# پنجرهٔ طراح — نسخهٔ دوم همان صفحه (تب طراح HTML)
PAGES["print-designer-window"] = page(
    "print-designer-window", "طراح قالب چاپ — فاکتور A5", "ویرایشگر HTML با متغیرها و پیشنمایش زنده", "🖨️",
    '<span class="btn ghost">🖨️ چاپ</span><span class="btn primary">💾 ذخیره</span>',
    stats([("b", "A5", "اندازهٔ کاغذ"), ("t", "۱۳", "اندازهٔ فونت"),
           ("p", "۹", "متغیرهای سند"), ("g", "فعال", "وضعیت قالب"), ("o", "۲", "طرحهای آماده")])
    + tabs(["طراح HTML", "متغیرها", "پیشنمایش", "طرحهای آماده"])
    + '<div style="display:flex;gap:8px">'
      '<div class="panel" style="flex:1"><h3>📄 پیشنمایش زنده — نمونه</h3>'
      '<div style="height:300px;border:1px solid #D6E0EA;border-radius:8px;background:#fff;padding:16px;text-align:center">'
      '<div style="font-size:15px;font-weight:800;color:#16355E;border-bottom:2px solid #16355E;padding-bottom:8px">درمانگاه شبانهروزی سهند</div>'
      '<div style="text-align:right;margin-top:14px;font-size:11px;line-height:2.2">'
      '<div class="kv"><span>شماره فاکتور</span><b>INV-۳۳۱۲</b></div>'
      '<div class="kv"><span>تاریخ</span><b>۱۴۰۵/۰۸/۰۵</b></div>'
      '<div class="kv"><span>بیمار</span><b>نظرا محمدی</b></div>'
      '<div class="kv"><span>کد ملی</span><b>۰۰۱۲۳۴۵۶۷۸</b></div></div>'
      '<div style="border-top:1px dashed #9AA9BA;margin-top:16px;padding-top:10px;font-size:12px">'
      '<b>جمع کل: ۴٬۹۵۰٬۰۰۰ ریال</b></div></div></div>'
      '<div class="panel" style="flex:1.35"><h3>⌨️ ویرایشگر HTML با متغیرها</h3>'
      '<div style="height:262px;background:#12202F;border-radius:8px;padding:12px;color:#9FE8C7;'
      'font-family:Consolas,monospace;font-size:11px;line-height:1.9;direction:ltr;text-align:left;overflow:hidden">'
      '&lt;div class="header"&gt;<br>'
      '&nbsp;&nbsp;{درمانگاه} — {شماره_فاکتور}<br>'
      '&lt;/div&gt;<br>'
      '&lt;table class="items"&gt;<br>'
      '&nbsp;&nbsp;{ردیفهای_خدمات}<br>'
      '&lt;/table&gt;<br>'
      '&lt;div class="total"&gt;جمع کل: {مبلغ_کل}&lt;/div&gt;'
      '</div>'
      '<div class="chips" style="margin-top:9px"><span class="chip sq">🗓 تاریخ شمسی</span>'
      '<span class="chip sq">👤 نام بیمار</span><span class="chip sq">🧾 شماره فاکتور</span>'
      '<span class="chip sq">💰 مبلغ کل</span><span class="chip sq">🏥 نام درمانگاه</span></div></div>'
      '<div class="panel" style="flex:.72"><h3>🎨 طرحهای آماده</h3>'
      '<div class="panel" style="margin:0 0 7px 0;background:#F7FAFD;border-color:#1976D2"><b>سربرگ کلاسیک</b>'
      '<div class="note">A5 · آبی نفتی · فعال</div></div>'
      '<div class="panel" style="margin:0 0 7px 0;background:#F7FAFD"><b>ساده و اقتصادی</b>'
      '<div class="note">A5 · سیاه و سفید</div></div>'
      '<div class="panel" style="margin:0;background:#F7FAFD"><b>رسید حرارتی</b>'
      '<div class="note">۸۰mm · بدون لوگو</div></div>'
      '<span class="btn primary wide" style="margin-top:9px">＋ قالب جدید</span></div>'
    + '</div>'
)

# ─────────────────────────────────────────────────────────────────────────────
# ۶) تنظیمات چاپگر و چاپ — مرکز چاپ
# ─────────────────────────────────────────────────────────────────────────────
print_rows = "".join(
    f'<tr><td><b>{title}</b></td><td class="mini">{desc}</td><td>{printer}</td>'
    f'<td>{paper}</td><td>{copies}</td><td><span class="tag {"g" if prev else "n"}">{"بله" if prev else "خیر"}</span></td>'
    f'<td><span class="tag {"b" if auto else "n"}">{"بله" if auto else "خیر"}</span></td></tr>'
    for title, desc, printer, paper, copies, prev, auto in [
        ("قبض پذیرش", "رسید پذیرش بیمار", "چاپگر حرارتی ۸۰mm", "A5", "۱", True, True),
        ("قبض نوبت", "رسید نوبتدهی", "چاپگر حرارتی ۸۰mm", "A5", "۱", True, True),
        ("فاکتور", "فاکتور خدمات", "HP LaserJet P1102", "A5", "۱", True, False),
        ("نسخهٔ پزشک", "نسخهٔ دارویی", "پیشفرض ویندوز", "A5", "۱", True, False),
        ("نتیجهٔ آزمایش", "جواب آزمایش", "HP LaserJet P1102", "A4", "۱", True, False),
        ("برچسب نمونه", "لیبل ظرف نمونه", "چاپگر لیبل", "۵۰×۲۵", "۱", False, True),
        ("گزارش تصویربرداری", "گزارش رادیولوژی", "پیشفرض ویندوز", "A4", "۱", True, False),
    ]
)

PAGES["print-settings"] = page(
    "print-settings", "تنظیمات چاپگر و چاپ", "چاپگر اختصاصی هر سند، کاغذ، نسخهها، پیشنمایش و چاپ خودکار", "⚙️",
    '<span class="btn ghost">🖨️ تست چاپ</span><span class="btn primary">💾 ذخیره تنظیمات</span>',
    stats([("g", "۱۲", "اسناد قابل چاپ"), ("b", "۴", "چاپگرهای نصبشده"),
           ("p", "۳", "چاپ خودکار"), ("o", "۹", "پیشنمایش فعال"), ("t", "۳۷", "چاپ امروز")])
    + tabs(["مرکز چاپ", "رفتار عمومی چاپ", "چاپگرها", "پوسته و ظاهر"])
    + '<div class="panel" style="padding:8px 10px;margin-bottom:8px"><div class="row">'
      '<div class="field ico col" style="max-width:300px">جستجوی سند…</div>'
      '<div class="chips"><span class="chip on">همهٔ اسناد</span><span class="chip">چاپ خودکار</span>'
      '<span class="chip">نیازمند پیشنمایش</span></div>'
      '<div class="sp"></div><span class="btn ghost sm">🔄 تازهمسازی چاپگرها</span></div></div>'
    + '<div style="display:flex;gap:8px">'
      '<div style="flex:1.6"><div class="panel" style="padding:8px 10px">'
        '<h3>🖨️ مرکز چاپ — چاپگر اختصاصی هر سند</h3>'
        '<table class="tbl"><tr><th>سند</th><th>توضیح</th><th>چاپگر</th><th>کاغذ</th>'
        '<th>نسخه</th><th>پیشنمایش</th><th>خودکار</th></tr>' + print_rows + '</table>'
        '<div class="note" style="margin-top:6px">چاپگر خالی = پیشفرض ویندوز. ۱۲ سند در ۴ چاپگر.</div>'
      '</div></div>'
      '<div style="flex:1;display:flex;flex-direction:column;gap:8px">'
        '<div class="panel"><h3>⚙️ رفتار عمومی چاپ (همهٔ سندها)</h3>'
        '<div class="kv"><span>نمایش پیشنمایش قبل از چاپ</span><b class="tag g">☑ فعال</b></div>'
        '<div class="kv"><span>چاپ خودکار بعد از ثبت فاکتور</span><b class="tag n">☐ غیرفعال</b></div>'
        '<div class="kv"><span>چاپ خودکار قبض نوبت</span><b class="tag g">☑ فعال</b></div>'
        '<div class="kv"><span>چاپ خودکار قبض پذیرش</span><b class="tag g">☑ فعال</b></div>'
        '<div class="lbl" style="margin-top:8px">چاپگر پیشفرض ویندوز</div>'
        '<div class="field sm">HP LaserJet P1102</div>'
        '<div class="row tight" style="margin-top:8px"><span class="btn primary sm">تست چاپ</span>'
        '<span class="btn ghost sm">ذخیره</span></div></div>'
        '<div class="panel"><h3>🖨️ چاپگرهای نصبشده</h3>'
        '<div class="kv"><span>HP LaserJet P1102</span><b class="tag g">آماده</b></div>'
        '<div class="kv"><span>EPSON LX-310</span><b class="tag g">آماده</b></div>'
        '<div class="kv"><span>چاپگر حرارتی ۸۰mm</span><b class="tag g">آماده</b></div>'
        '<div class="kv"><span>چاپگر لیبل</span><b class="tag o">در صف</b></div></div>'
      '</div>'
    + '</div>'
)


def main() -> None:
    for key, html in PAGES.items():
        (OUT / f"{key}.html").write_text(html, encoding="utf-8")
        print(f"✅ {key}.html — {len(html)} bytes")
    print(f"\nجمعاً {len(PAGES)} طرح در {OUT} ساخته شد.")


if __name__ == "__main__":
    main()
