#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📸 مولد اسکرین‌شات ۵۲ فرم برنامه دسکتاپ درمانگاه سهند — نسخه ۱.۷.۰
==============================================================
هر فرم به‌صورت موکاپ HTML وفادار به طراحی WPF/Material تولید و در ابعاد
1600×1000 اسکرین‌شات می‌شود تا در بروشور «تور تصویری کامل» استفاده شود.

خروجی: docs/screenshots/forms/NN-name.png  (۵۲ فایل)
"""
import sys
import pathlib
from playwright.sync_api import sync_playwright

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
OUT_DIR = REPO / "docs" / "screenshots" / "forms"
OUT_DIR.mkdir(parents=True, exist_ok=True)
TMP_DIR = REPO / "scripts" / "_mockups"
TMP_DIR.mkdir(parents=True, exist_ok=True)

W, H = 1600, 1000

# ═══════════════════════════  قالب مشترک  ═══════════════════════════

BASE_CSS = """
@font-face { font-family:'Vazirmatn'; src:url('../../docs/assets/fonts/Vazirmatn-Regular.ttf') format('truetype'); font-weight:400; }
@font-face { font-family:'Vazirmatn'; src:url('../../docs/assets/fonts/Vazirmatn-Medium.ttf') format('truetype'); font-weight:500; }
@font-face { font-family:'Vazirmatn'; src:url('../../docs/assets/fonts/Vazirmatn-SemiBold.ttf') format('truetype'); font-weight:600; }
@font-face { font-family:'Vazirmatn'; src:url('../../docs/assets/fonts/Vazirmatn-Bold.ttf') format('truetype'); font-weight:700; }
@font-face { font-family:'Vazirmatn'; src:url('../../docs/assets/fonts/Vazirmatn-ExtraBold.ttf') format('truetype'); font-weight:800; }
* { margin:0; padding:0; box-sizing:border-box; }
html, body { width:1600px; height:1000px; overflow:hidden; }
body { font-family:'Vazirmatn','Noto Color Emoji',sans-serif; direction:rtl;
  background:#EFF3F9; color:#1F2A3D; }

/* ─── اسکلت اصلی برنامه (شبیه پوسته WPF) ─── */
.app { display:flex; height:100%; }
.side { width:236px; background:linear-gradient(180deg,#16355E 0%,#1C4E89 100%); color:#DCE9FA;
  display:flex; flex-direction:column; padding:14px 10px; flex:none; }
.side .brand { display:flex; align-items:center; gap:10px; padding:8px 10px 14px; }
.side .brand .lg { width:42px; height:42px; border-radius:11px; background:#fff; overflow:hidden; flex:none;
  display:flex; align-items:center; justify-content:center; font-size:22px; }
.side .brand .lg img { width:100%; height:100%; object-fit:contain; display:block; }
.side .brand b { font-size:15px; font-weight:800; color:#fff; line-height:1.2; }
.side .brand small { font-size:9.5px; opacity:.65; letter-spacing:.5px; }
.nav { display:flex; flex-direction:column; gap:2px; overflow:hidden; }
.nav .it { display:flex; align-items:center; gap:9px; padding:8.5px 11px; border-radius:9px;
  font-size:12.5px; font-weight:600; opacity:.82; }
.nav .it .em { width:19px; text-align:center; font-size:14px; }
.nav .it.on { background:rgba(255,255,255,.16); opacity:1; box-shadow:inset 0 0 0 1.5px rgba(255,255,255,.22); }
.side .foot { margin-top:auto; padding:9px 11px; border-top:1px solid rgba(255,255,255,.14);
  font-size:10.5px; opacity:.7; }

.main { flex:1; display:flex; flex-direction:column; min-width:0; }
.topbar { height:56px; background:#fff; border-bottom:1px solid #E1E8F2; display:flex;
  align-items:center; gap:14px; padding:0 20px; flex:none; }
.topbar .tt { font-size:16.5px; font-weight:800; color:#16355E; }
.topbar .crumb { font-size:11px; color:#8296B3; }
.topbar .sp { flex:1; }
.topbar .who { display:flex; align-items:center; gap:8px; font-size:11.5px; font-weight:700; color:#40536E; }
.topbar .av { width:32px; height:32px; border-radius:50%; background:linear-gradient(135deg,#2E86C1,#16355E);
  color:#fff; display:flex; align-items:center; justify-content:center; font-size:13px; font-weight:800; }
.topbar .sch { width:250px; height:34px; border-radius:9px; background:#F1F5FA; border:1px solid #E1E8F2;
  display:flex; align-items:center; padding:0 12px; font-size:11px; color:#8296B3; gap:7px; }

.content { flex:1; padding:16px 20px 18px; overflow:hidden; display:flex; flex-direction:column; gap:12px; }

/* ─── اجزای مشترک ─── */
.toolbar { display:flex; align-items:center; gap:9px; }
.btn { display:inline-flex; align-items:center; gap:7px; border-radius:9px; padding:8.5px 16px;
  font-size:12px; font-weight:700; font-family:inherit; }
.btn.pri { background:linear-gradient(135deg,#1F6FB2,#16355E); color:#fff;
  box-shadow:0 3px 9px rgba(22,53,94,.32); }
.btn.sec { background:#fff; color:#2E5C96; border:1.5px solid #CBD9EA; }
.btn.warn { background:#FFF4E0; color:#9A6A12; border:1.5px solid #F3D9A4; }
.toolbar .inp { height:36px; border-radius:9px; border:1.5px solid #D8E2EF; background:#fff;
  padding:0 13px; font-size:12px; color:#8296B3; }
.toolbar .sp { flex:1; }

.cards { display:grid; grid-template-columns:repeat(auto-fit,minmax(150px,1fr)); gap:12px; }
.stat { background:#fff; border-radius:14px; padding:14px 16px; border:1px solid #E4EBF4;
  box-shadow:0 2px 7px rgba(23,50,94,.05); }
.stat .lb { font-size:11px; color:#7C90AC; display:flex; align-items:center; gap:6px; }
.stat .vl { font-size:23px; font-weight:800; color:#16355E; margin-top:5px; }
.stat .tr { font-size:10.5px; font-weight:700; margin-top:4px; }
.stat .tr.up { color:#0E9F6E; } .stat .tr.dn { color:#C0392B; }
.stat.c1 { border-top:3.5px solid #2E86C1; } .stat.c2 { border-top:3.5px solid #17A589; }
.stat.c3 { border-top:3.5px solid #E67E22; } .stat.c4 { border-top:3.5px solid #8E44AD; }
.stat.c5 { border-top:3.5px solid #C0392B; } .stat.c6 { border-top:3.5px solid #27AE60; }

.panel { background:#fff; border-radius:14px; border:1px solid #E4EBF4;
  box-shadow:0 2px 7px rgba(23,50,94,.05); padding:15px 16px; }
.panel .ph { display:flex; align-items:center; justify-content:space-between; margin-bottom:11px; }
.panel .ph b { font-size:13.5px; font-weight:800; color:#16355E; }
.panel .ph small { font-size:10.5px; color:#8296B3; }

table.tbl { width:100%; border-collapse:collapse; font-size:11.8px; }
.tbl th { text-align:right; color:#6E82A0; font-weight:700; font-size:10.8px;
  padding:8px 9px; border-bottom:1.5px solid #E4EBF4; white-space:nowrap; }
.tbl td { padding:8.5px 9px; border-bottom:1px solid #F0F4F9; color:#2B3B52; white-space:nowrap; }
.tbl tr:nth-child(even) td { background:#F8FAFD; }
.badge { display:inline-block; border-radius:20px; padding:2.5px 10px; font-size:10.2px; font-weight:800; }
.b-green { background:#E1F7EE; color:#0B7A55; } .b-amber { background:#FDF1DC; color:#9A6A12; }
.b-red { background:#FCE8E6; color:#B03A2E; } .b-blue { background:#E3EFFC; color:#1F5E9E; }
.b-gray { background:#EDF1F6; color:#5D6F88; } .b-purple { background:#F3EAFB; color:#7D3BA4; }

.bars { display:flex; align-items:flex-end; gap:14px; height:150px; padding:6px 4px 0; }
.bar { flex:1; border-radius:6px 6px 3px 3px; background:linear-gradient(180deg,#5DA6DC,#2E86C1); position:relative; }
.bar.hi { background:linear-gradient(180deg,#43C59E,#17A589); }
.bar span { position:absolute; top:-19px; width:100%; text-align:center; font-size:9.8px; color:#6E82A0; }
.bar i { position:absolute; bottom:-19px; width:100%; text-align:center; font-size:10px; font-style:normal; color:#8296B3; }

.progress { height:8px; border-radius:6px; background:#EDF1F6; overflow:hidden; }
.progress > div { height:100%; border-radius:6px; background:linear-gradient(90deg,#2E86C1,#5DA6DC); }

.chips { display:flex; flex-wrap:wrap; gap:8px; }
.chip { background:#EDF3FB; color:#2E5C96; border:1px solid #D8E2EF; border-radius:20px;
  padding:5px 13px; font-size:11.5px; font-weight:700; }

.formgrid { display:grid; grid-template-columns:1fr 1fr; gap:12px 18px; }
.fld { display:flex; flex-direction:column; gap:5px; }
.fld label { font-size:11px; font-weight:700; color:#5D6F88; }
.fld .box { height:38px; border-radius:9px; border:1.5px solid #D8E2EF; background:#fff;
  display:flex; align-items:center; padding:0 12px; font-size:12.5px; color:#2B3B52; gap:8px; }
.fld .box.hint { color:#A5B4C8; }
.fld select.box { justify-content:space-between; }
.fld .box .num { margin-right:auto; color:#A5B4C8; font-size:10.5px; direction:ltr; }

.qrow { display:flex; align-items:center; gap:11px; background:#F8FAFD; border:1px solid #EDF1F6;
  border-radius:12px; padding:10.5px 13px; margin-bottom:8px; }
.qrow .n { width:36px; height:36px; border-radius:50%; background:#16355E; color:#fff; flex:none;
  display:flex; align-items:center; justify-content:center; font-size:13.5px; font-weight:800; }
.qrow .nm { font-size:13px; font-weight:700; color:#22334D; }
.qrow .in { font-size:10.8px; color:#8296B3; margin-top:2px; }
.qrow .st { margin-right:auto; }

.grid2 { display:grid; grid-template-columns:1fr 1fr; gap:12px; }
.grid3 { display:grid; grid-template-columns:repeat(3,1fr); gap:12px; }
.grow { flex:1; min-height:0; }

/* ─── پنجره‌های مودال (فرم‌ها) ─── */
.desk { position:absolute; inset:0; background:linear-gradient(150deg,#243B5E,#101E33); }
.desk::before { content:''; position:absolute; inset:0; opacity:.14;
  background-image:linear-gradient(rgba(255,255,255,.4) 1px,transparent 1px),
    linear-gradient(90deg,rgba(255,255,255,.4) 1px,transparent 1px); background-size:42px 42px; }
.win { position:absolute; top:50%; left:50%; transform:translate(-50%,-50%);
  width:1080px; max-height:900px; background:#F6F9FD; border-radius:16px; overflow:hidden;
  box-shadow:0 34px 90px rgba(4,12,28,.6), 0 0 0 1px rgba(255,255,255,.14); display:flex; flex-direction:column; }
.win .tbar { height:52px; background:linear-gradient(135deg,#1C4E89,#16355E); color:#fff;
  display:flex; align-items:center; gap:11px; padding:0 18px; flex:none; }
.win .tbar .em { font-size:17px; }
.win .tbar b { font-size:14.5px; font-weight:800; }
.win .tbar small { font-size:10.5px; opacity:.7; }
.win .tbar .sp { flex:1; }
.win .tbar .x { width:30px; height:30px; border-radius:8px; background:rgba(255,255,255,.14);
  display:flex; align-items:center; justify-content:center; font-size:13px; }
.win .body { padding:18px 20px; overflow:hidden; display:flex; flex-direction:column; gap:13px; }
.win .acts { display:flex; gap:9px; justify-content:flex-start; border-top:1px solid #E4EBF4;
  padding-top:13px; margin-top:2px; }
"""

NAV_ITEMS = [
    ("📊", "داشبورد", False), ("🧑‍🤝‍🧑", "بیماران", False), ("🩺", "پزشکان", False),
    ("🗓", "نوبت‌دهی", False), ("🛎", "پذیرش", False), ("📋", "ویزیت و معاینه", False),
    ("💊", "نسخه‌نویسی", False), ("❤️", "پرونده بالینی", False), ("💵", "مالی و صندوق", False),
    ("📒", "حسابداری", False), ("🧾", "فیش حقوقی", False), ("🕐", "حضور و غیاب", False),
    ("🛡", "بیمه", False), ("🧪", "پاراکلینیک", False), ("🔬", "آزمایشگاه", False),
    ("💊", "داروخانه", False), ("📦", "انبارگردانی", False), ("📈", "گزارش‌ها", False),
    ("👤", "کاربران", False), ("🔑", "نقش‌ها", False), ("✉️", "پیامک", False),
    ("📝", "نظرسنجی بیماران", False), ("👔", "پرسنل", False), ("⚙️", "تنظیمات", False),
    ("🏦", "سپاس", False), ("☁️", "نسخه الکترونیک", False), ("🏢", "شعبه‌ها", False),
    ("💾", "بکاپ و بازیابی", False), ("🧾", "لاگ عملیات", False), ("🔑", "لایسنس", False),
    ("🎨", "قالب چاپ", False),
]


def sidebar(active_title: str) -> str:
    items = []
    for em, title, _ in NAV_ITEMS:
        on = ' class="on"' if title == active_title else ""
        items.append(f'<div class="it"{on}><span class="em">{em}</span>{title}</div>')
    # لوگوی رسمی محصول (ClinicPro.png آپلودی مالک) به‌جای ایموجی
    return ('<aside class="side"><div class="brand"><div class="lg">'
            '<img src="../../docs/assets/ClinicPro.png" alt="لوگو"></div>'
            '<div><b>درمانگاه سهند</b><br><small>SAHAND CLINICPRO</small></div></div>'
            f'<nav class="nav">{"".join(items)}</nav>'
            '<div class="foot">👤 دکتر رضایی · مدیر سیستم<br>۱۴۰۵/۰۷/۱۳ — ۱۰:۲۴</div></aside>')


def shell(title: str, crumb: str, active_nav: str, body_html: str, icon: str) -> str:
    return f"""<!DOCTYPE html><html lang="fa" dir="rtl"><head><meta charset="UTF-8">
<title>{title}</title><style>{BASE_CSS}</style></head><body>
<div class="app">{sidebar(active_nav)}
<div class="main">
  <div class="topbar"><span style="font-size:18px">{icon}</span>
    <div><div class="tt">{title}</div><div class="crumb">{crumb}</div></div>
    <div class="sp"></div>
    <div class="sch">🔎 جستجوی سراسری… (Ctrl+K)</div>
    <div class="who"><div class="av">ر</div> دکتر رضایی</div>
  </div>
  <div class="content">{body_html}</div>
</div></div></body></html>"""


def window_shell(title: str, sub: str, icon: str, body_html: str) -> str:
    return f"""<!DOCTYPE html><html lang="fa" dir="rtl"><head><meta charset="UTF-8">
<title>{title}</title><style>{BASE_CSS}</style></head><body>
<div class="desk"></div>
<div class="win">
  <div class="tbar"><span class="em">{icon}</span><div><b>{title}</b><br><small>{sub}</small></div>
  <div class="sp"></div><div class="x">✕</div></div>
  <div class="body">{body_html}</div>
</div></body></html>"""


# ═══════════════════════════  سازنده‌های اجزا  ═══════════════════════════

def stat_row(items):
    """items: [(label, value, trend, cls, icon)]"""
    cells = "".join(
        f'<div class="stat {c}"><div class="lb">{i} {l}</div><div class="vl">{v}</div>'
        f'<div class="tr {"up" if t.startswith("▲") else "dn"}">{t}</div></div>'
        for (l, v, t, c, i) in items)
    return f'<div class="cards">{cells}</div>'


def table_panel(title, cols, rows, small=""):
    head = "".join(f"<th>{c}</th>" for c in cols)
    body = ""
    for r in rows:
        body += "<tr>" + "".join(f"<td>{c}</td>" for c in r) + "</tr>"
    return (f'<div class="panel grow"><div class="ph"><b>{title}</b><small>{small}</small></div>'
            f'<table class="tbl"><tr>{head}</tr>{body}</table></div>')


def chart_panel(title, data, labels, hi_index=None):
    bars = ""
    for i, (v, lb) in enumerate(zip(data, labels)):
        cls = "bar hi" if (hi_index is not None and i in (hi_index if isinstance(hi_index, list) else [hi_index])) else "bar"
        bars += f'<div class="{cls}" style="height:{v}%"><span>{v}</span><i>{lb}</i></div>'
    return (f'<div class="panel"><div class="ph"><b>{title}</b><small>هفته جاری</small></div>'
            f'<div class="bars" style="margin-bottom:22px">{bars}</div></div>')


def form_fields(fields):
    """fields: [(label, value, icon?, hint?)]"""
    out = []
    for f in fields:
        lb, val = f[0], f[1]
        em = f[2] if len(f) > 2 and f[2] else "✏️"
        hint = f[3] if len(f) > 3 and f[3] else ""
        cls = "box hint" if not val or hint else "box"
        out.append(f'<div class="fld"><label>{lb}</label><div class="{cls}">'
                   f'<span>{em}</span>{val if val else hint}</div></div>')
    return f'<div class="formgrid">{"".join(out)}</div>'


def queue_rows(rows):
    """rows: [(no, name, info, status_html)]"""
    out = "".join(
        f'<div class="qrow"><div class="n">{n}</div><div><div class="nm">{nm}</div>'
        f'<div class="in">{inf}</div></div><div class="st">{st}</div></div>'
        for (n, nm, inf, st) in rows)
    return out


def toolbar(buttons, search="جستجو…"):
    btns = "".join(f'<button class="btn {c}">{i} {t}</button>' for (t, i, c) in buttons)
    return (f'<div class="toolbar">{btns}<div class="sp"></div>'
            f'<input class="inp" style="width:210px" placeholder="🔎 {search}"></div>')


def actions_row():
    return ('<div class="acts"><button class="btn pri">💾 ذخیره</button>'
            '<button class="btn sec">🗑 حذف</button>'
            '<button class="btn sec">🖨 چاپ</button>'
            '<div class="sp" style="flex:1"></div>'
            '<button class="btn sec">✕ انصراف</button></div>')


# ═══════════════════════════  تعریف ۵۲ فرم  ═══════════════════════════

B = '<span class="badge b-{}">{}</span>'

SPECS = []

def view(num, key, title, icon, nav, body):
    SPECS.append(dict(num=num, key=key, title=title, icon=icon, kind="view", nav=nav, body=body))

def win(num, key, title, icon, sub, body):
    SPECS.append(dict(num=num, key=key, title=title, icon=icon, kind="window", sub=sub, body=body))

# ─── ۰۱ داشبورد ───
view(1, "dashboard", "داشبورد مدیریتی", "📊", "داشبورد",
     stat_row([("بیماران امروز", "۱۲۸", "▲ ۱۲٪ دیروز", "c1", "🧑‍🤝‍🧑"),
               ("در صف انتظار", "۹", "▲ فراخوان زنده", "c2", "🛎"),
               ("درآمد امروز", "۴۸.۵م ت", "▲ ۸٪ هفته", "c3", "💵"),
               ("نوبت‌های امروز", "۶۷", "▲ ۱۵ ویزیت باقی", "c4", "🗓"),
               ("غیبت پرسنل", "۲", "▼ ۱ نفر کمتر", "c6", "👔")])
     + '<div class="grid2 grow" style="grid-template-columns:1.5fr 1fr">'
     + chart_panel("مراجعات هفته جاری", [38, 56, 47, 88, 64, 96, 30], ["ش", "ی", "د", "س", "چ", "پ", "ج"], hi_index=[3, 5])
     + '<div class="panel"><div class="ph"><b>🛎 صف زنده سالن</b><small>بلادرنگ</small></div>'
     + queue_rows([("۴۲", "مریم احمدی", "دکتر کریمی — داخلی", B.format("green", "در حال ویزیت")),
                   ("۴۳", "علی رضایی", "دکتر نجفی — عمومی", B.format("blue", "فراخوان")),
                   ("۴۴", "نگار موسوی", "دکتر احمدی — قلب", B.format("amber", "در انتظار")),
                   ("۴۵", "حسین کاظمی", "دکتر رضایی — اطفال", B.format("gray", "در انتظار"))])
     + '</div></div>'
     + '<div class="panel"><div class="ph"><b>📌 خلاصه امروز</b><small>۱۴۰۵/۰۷/۱۳</small></div>'
       '<div class="chips"><span class="chip">💰 صندوق: ۱۲.۸م ت بدهی</span><span class="chip">🧪 ۸ آزمایش در انتظار نتیجه</span>'
       '<span class="chip">💊 ۳ نسخه در تداخل‌سنجی</span><span class="chip">📅 فردا: ۵۴ نوبت</span>'
       '<span class="chip">⚠️ موجودی ۲ دارو زیر حد بحرانی</span></div></div>')

# ─── ۰۲ بیماران ───
view(2, "patients", "مدیریت بیماران", "🧑‍🤝‍🧑", "بیماران",
     toolbar([("بیمار جدید", "➕", "pri"), ("خروجی اکسل", "📗", "sec"), ("چاپ لیست", "🖨", "sec")], "نام، کد ملی یا شماره پرونده…")
     + stat_row([("کل بیماران", "۳٬۴۵۲", "▲ ۴۲ این ماه", "c1", "👥"), ("ثبت امروز", "۱۸", "▲ ۶ زن / ۱۲ مرد", "c2", "➕"),
                 ("بیماران VIP", "۹۶", "▲ ۳ جدید", "c3", "⭐"), ("بدون بیمه", "۴۱۲", "▼ ۲٪", "c5", "🛡")])
     + table_panel("فهرست بیماران", ["شماره پرونده", "نام و نام خانوادگی", "کد ملی", "موبایل", "بیمه", "سن", "آخرین مراجعه", "وضعیت"],
        [("MR-1042", "مریم احمدی", "۰۰۷۶۴۰۹۶۲۷", "۰۹۱۴…", B.format("blue", "تأمین اجتماعی"), "۳۴", "۱۴۰۵/۰۷/۱۱", B.format("green", "فعال")),
         ("MR-1041", "علی رضایی", "۰۰۱۲۳۴۵۶۷۹", "۰۹۱۴…", B.format("purple", "آزاد"), "۴۱", "۱۴۰۵/۰۷/۱۳", B.format("green", "فعال")),
         ("MR-1039", "نگار موسوی", "۰۰۴۵۶۷۸۹۱۲", "۰۹۳۵…", B.format("blue", "تأمین اجتماعی"), "۲۸", "۱۴۰۵/۰۷/۰۹", B.format("amber", "غیرفعال")),
         ("MR-1038", "حسین کاظمی", "۰۰۲۳۴۵۶۷۸۱", "۰۹۱۲…", B.format("green", "نیروهای مسلح"), "۵۳", "۱۴۰۵/۰۷/۱۳", B.format("green", "فعال")),
         ("MR-1036", "سارا محمدی", "۰۰۶۷۸۹۱۲۳۴", "۰۹۱۴…", B.format("blue", "تأمین اجتماعی"), "۷", "۱۴۰۵/۰۷/۰۸", B.format("green", "فعال")),
         ("MR-1033", "رضا حسینی", "۰۰۹۰۱۲۳۴۵۶", "۰۹۱۴…", B.format("gray", "بدون بیمه"), "۶۲", "۱۴۰۵/۰۶/۲۹", B.format("gray", "غیرفعال"))],
         "نمایش ۶ از ۳٬۴۵۲ — کد ملی‌ها با رقم کنترلی اعتبارسنجی می‌شوند"))

# ─── ۰۳ پزشکان ───
view(3, "doctors", "پزشکان و تخصص‌ها", "🩺", "پزشکان",
     toolbar([("پزشک جدید", "➕", "pri"), ("برنامه شیفت", "🗓", "sec"), ("عملکرد", "📈", "sec")])
     + stat_row([("کل پزشکان", "۲۴", "▲ ۲ متخصص جدید", "c1", "🩺"), ("ویزیت امروز", "۶۷", "▲ ۹ پزشک حاضر", "c2", "📋"),
                 ("میانگین ویزیت روزانه", "۱۸", "▲ ۱۲ دقیقه", "c3", "⏱"), ("رضایت بیماران", "۴.۶/۵", "▲ از نظرسنجی", "c4", "⭐")])
     + table_panel("پزشکان فعال", ["نام", "تخصص", "شماره نظام پزشکی", "شیفت امروز", "نوبت‌های امروز", "حق‌الویزیت", "وضعیت"],
        [("دکتر کریمی", "داخلی", "۱۲۳۴۵", "۸–۱۴", "۱۴", "۳۵۰٬۰۰۰", B.format("green", "در درمانگاه")),
         ("دکتر نجفی", "عمومی", "۲۳۴۵۶", "۱۴–۲۰", "۲۱", "۲۵۰٬۰۰۰", B.format("green", "در درمانگاه")),
         ("دکتر احمدی", "قلب و عروق", "۳۴۵۶۷", "—", "۰", "۵۰۰٬۰۰۰", B.format("gray", "مرخصی")),
         ("دکتر رضایی", "اطفال", "۴۵۶۷۸", "۸–۱۴", "۱۸", "۴۰۰٬۰۰۰", B.format("green", "در درمانگاه")),
         ("دکتر صادقی", "ارتوپدی", "۵۶۷۸۹", "۱۶–۲۰", "۱۴", "۴۵۰٬۰۰۰", B.format("blue", "نوبت‌دهی"))], "۲۴ پزشک ثبت‌شده"))

# ─── ۰۴ نوبت‌دهی ───
view(4, "appointments", "نوبت‌دهی هوشمند", "🗓", "نوبت‌دهی",
     toolbar([("نوبت جدید", "➕", "pri"), ("نوبت‌های امروز", "📆", "sec"), ("لیست انتظار", "⏳", "sec"), ("پیامک یادآوری", "✉️", "sec")])
     + stat_row([("نوبت‌های امروز", "۶۷", "▲ ۱۵ باقی‌مانده", "c1", "🗓"), ("نوبت‌های فردا", "۵۴", "▲ ۸٪", "c2", "📆"),
                 ("لغو شده امروز", "۴", "▼ ۲٪", "c5", "✖"), ("میانگین انتظار", "۹ دقیقه", "▼ ۲ دقیقه بهتر", "c6", "⏱")])
     + table_panel("برنامه نوبت‌های امروز — ۱۴۰۵/۰۷/۱۳", ["ساعت", "بیمار", "پزشک", "نوع خدمت", "کانال ثبت", "وضعیت"],
        [("۰۸:۳۰", "مریم احمدی", "دکتر کریمی", "ویزیت داخلی", B.format("blue", "حضوری"), B.format("green", "انجام شد")),
         ("۰۹:۰۰", "علی رضایی", "دکتر نجفی", "ویزیت عمومی", B.format("purple", "تلفنی"), B.format("green", "انجام شد")),
         ("۰۹:۳۰", "نگار موسوی", "دکتر احمدی", "کنترل قلب", B.format("green", "اینترنت"), B.format("blue", "در راه")),
         ("۱۰:۰۰", "حسین کاظمی", "دکتر رضایی", "ویزیت اطفال", B.format("blue", "حضوری"), B.format("amber", "تأیید نشده")),
         ("۱۰:۳۰", "سارا محمدی", "دکتر رضایی", "واکسیناسیون", B.format("green", "اینترنت"), B.format("amber", "تأیید نشده")),
         ("۱۱:۰۰", "رضا حسینی", "دکتر صادقی", "ویزیت ارتوپدی", B.format("blue", "حضوری"), B.format("gray", "در انتظار"))]))

# ─── ۰۵ پذیرش ───
view(5, "receptions", "پذیرش و صف زنده", "🛎", "پذیرش",
     toolbar([("پذیرش جدید", "➕", "pri"), ("فراخوان بعدی", "📢", "pri"), ("نمایش شماره روی تلویزیون", "📺", "sec")])
     + '<div class="grid2" style="grid-template-columns:1.2fr 1fr">'
     + '<div class="panel"><div class="ph"><b>🛎 صف زنده سالن — بلادرنگ SignalR</b><small>به‌روزرسانی خودکار</small></div>'
     + queue_rows([("۴۲", "مریم احمدی", "پذیرش ۰۸:۴۲ — دکتر کریمی", B.format("green", "در حال ویزیت")),
                   ("۴۳", "علی رضایی", "پذیرش ۰۸:۵۵ — دکتر نجفی", B.format("blue", "فراخوان شد")),
                   ("۴۴", "نگار موسوی", "پذیرش ۰۹:۱۰ — دکتر احمدی", B.format("amber", "در انتظار")),
                   ("۴۵", "حسین کاظمی", "پذیرش ۰۹:۲۲ — دکتر رضایی", B.format("amber", "در انتظار")),
                   ("۴۶", "سارا محمدی", "پذیرش ۰۹:۳۱ — دکتر رضایی", B.format("gray", "آماده‌سازی"))])
     + '</div><div class="panel"><div class="ph"><b>📈 آمار صف امروز</b></div>'
     + stat_row([("پذیرش امروز", "۵۸", "▲ ۱۲٪", "c1", "🛎"), ("میانگین انتظار", "۹ دقیقه", "▼ بهتر", "c2", "⏱")])
     + '<div style="margin-top:12px"><div class="ph"><b>توزیع صف بر اساس خدمت</b></div>'
     + '<div style="display:flex;flex-direction:column;gap:9px">'
     + '<div><div style="display:flex;justify-content:space-between;font-size:11px;margin-bottom:4px"><span>ویزیت سرپایی</span><b>۳۴ نفر</b></div><div class="progress"><div style="width:58%"></div></div></div>'
     + '<div><div style="display:flex;justify-content:space-between;font-size:11px;margin-bottom:4px"><span>خدمات پاراکلینیک</span><b>۱۵ نفر</b></div><div class="progress"><div style="width:26%"></div></div></div>'
     + '<div><div style="display:flex;justify-content:space-between;font-size:11px;margin-bottom:4px"><span>ویزیت متخصص</span><b>۹ نفر</b></div><div class="progress"><div style="width:16%"></div></div></div>'
     + '</div></div></div></div>')

# ─── ۰۶ ویزیت ───
view(6, "visits", "ویزیت و معاینه", "📋", "ویزیت و معاینه",
     toolbar([("ویزیت جدید", "➕", "pri"), ("تکمیل‌نشده‌ها", "📝", "sec"), ("ارسال به نسخه", "💊", "sec")])
     + table_panel("ویزیت‌های امروز", ["شماره", "بیمار", "پزشک", "علت مراجعه", "فشار خون", "نبض", "دما", "تشخیص初步", "وضعیت"],
        [("#V-881", "مریم احمدی", "دکتر کریمی", "درد شکم", "۱۲۰/۸۰", "۷۸", "۳۶.۸", "گاستریت", B.format("green", "تکمیل")),
         ("#V-880", "علی رضایی", "دکتر نجفی", "سرماخوردگی", "۱۱۵/۷۵", "۷۲", "۳۷.۲", "ویروس تنفسی", B.format("green", "تکمیل")),
         ("#V-879", "نگار موسوی", "دکتر احمدی", "کنترل فشار", "۱۴۵/۹۵", "۸۴", "۳۶.۶", "هایپرتانسیون", B.format("blue", "در معاینه")),
         ("#V-878", "حسین کاظمی", "دکتر رضایی", "تب کودک", "—", "—", "۳۸.۴", "—", B.format("amber", "در انتظار")),
         ("#V-877", "رضا حسینی", "دکتر صادقی", "درد زانو", "۱۳۰/۸۵", "۷۶", "۳۶.۷", "آرتروز", B.format("green", "تکمیل"))])
     + '<div class="panel"><div class="ph"><b>🔎 علائم حیاتی — مریم احمدی</b><small>پرونده MR-1042</small></div>'
     + '<div class="grid3">'
     + '<div class="stat c1"><div class="lb">📈 فشار خون</div><div class="vl">۱۲۰/۸۰</div><div class="tr up">نرمال</div></div>'
     + '<div class="stat c2"><div class="lb">❤️ نبض</div><div class="vl">۷۸</div><div class="tr up">نرمال</div></div>'
     + '<div class="stat c3"><div class="lb">🌡 دما</div><div class="vl">۳۶.۸°</div><div class="tr up">نرمال</div></div>'
     + '</div></div>')

# ─── ۰۷ نسخه‌نویسی ───
view(7, "prescriptions", "نسخه‌نویسی هوشمند", "💊", "نسخه‌نویسی",
     toolbar([("نسخه جدید", "➕", "pri"), ("تداخل‌سنجی", "⚠️", "sec"), ("نسخه الکترونیک", "☁️", "sec")])
     + stat_row([("نسخه امروز", "۲۳", "▲ ۴ دارو‌ی جدید", "c1", "💊"), ("هشدار تداخل", "۳", "▼ ۱ کمتر", "c5", "⚠️"),
                 ("حساسیت‌های فعال", "۷", "▲ پایش خودکار", "c3", "🚨"), ("ارسال به نسخه الکترونیک", "۱۹", "▲ ۸۳٪", "c2", "☁️")])
     + table_panel("نسخه‌های اخیر", ["شماره", "بیمار", "پزشک", "تعداد قلم", "تداخل", "هشدار حساسیت", "قابل چاپ", "وضعیت"],
        [("#P-1204", "مریم احمدی", "دکتر کریمی", "۳", B.format("green", "ندارد"), B.format("green", "ندارد"), B.format("green", "بله"), B.format("green", "صادر شد")),
         ("#P-1203", "علی رضایی", "دکتر نجفی", "۲", B.format("green", "ندارد"), B.format("green", "ندارد"), B.format("green", "بله"), B.format("green", "صادر شد")),
         ("#P-1202", "نگار موسوی", "دکتر احمدی", "۴", B.format("red", "وارفارین+آسپرین"), B.format("amber", "پنی‌سیلین"), B.format("red", "بازبینی"), B.format("amber", "در بررسی")),
         ("#P-1201", "حسین کاظمی", "دکتر رضایی", "۲", B.format("green", "ندارد"), B.format("green", "ندارد"), B.format("green", "بله"), B.format("blue", "در داروخانه"))]))

# ─── ۰۸ پرونده بالینی پیشرفته ───
view(8, "emr-advanced", "پرونده بالینی پیشرفته", "❤️", "پرونده بالینی",
     toolbar([("نمودار رشد WHO", "📈", "pri"), ("سابقه خانوادگی", "👥", "sec"), ("طرح مراقبت مزمن", "🩺", "sec"), ("DICOM", "🩻", "sec")])
     + '<div class="grid2 grow" style="grid-template-columns:1fr 1.4fr">'
     + '<div class="panel"><div class="ph"><b>📈 نمودار رشد WHO 2006 — سارا محمدی</b><small>۷ ساله</small></div>'
     + '<div class="bars" style="height:190px;margin-bottom:20px">'
     + "".join(f'<div class="bar{" hi" if i in (2,5) else ""}" style="height:{v}%"><span>{lb}</span><i>سنة {i+2}</i></div>'
               for i, (v, lb) in enumerate(zip([55, 62, 72, 66, 78, 88, 74, 82, 79], ["۱۶", "۱۸", "۲۰", "۲۱", "۲۳", "۲۶", "۲۵", "۲۷", "۲۶"])))
     + '</div><div class="chips"><span class="chip">قد: ۱۲۲ سانتی‌متر</span><span class="chip">وزن: ۲۳.۵ کیلوگرم</span>'
       '<span class="chip">BMI: ۱۵.۸ — نرمال</span><span class="chip">z-score: ۰.۴</span></div></div>'
     + '<div class="panel"><div class="ph"><b>🗂 خلاصه پرونده</b><small>MR-1036</small></div>'
     + table_panel("", ["تاریخ", "رویداد", "پزشک", "جزئیات"],
        [("۱۴۰۵/۰۷/۰۸", "ویزیت اطفال", "دکتر رضایی", " control واکسیناسیون"),
         ("۱۴۰۵/۰۴/۲۲", "تب ۳۹ درجه", "دکتر رضایی", "آزمایش CBC — نرمال"),
         ("۱۴۰۵/۰۲/۱۰", "ویزیت دوره‌ای", "دکتر نجفی", "معاینه کامل — سالم"),
         ("۱۴۰۴/۱۱/۰۵", "برونشیت", "دکتر کریمی", "درمان سرپایی ۷ روز")], "").replace('class="panel grow"', 'class=""')
     + '<div class="chips" style="margin-top:10px"><span class="chip">🚨 حساسیت: هیچ</span>'
       '<span class="chip">🏥 بیماری زمینه‌ای: هیچ</span><span class="chip">💉 واکسن: کامل</span></div></div>'
     + '</div>')

# ─── ۰۹ مالی و صندوق ───
view(9, "billing", "مالی و صندوق", "💵", "مالی و صندوق",
     toolbar([("فاکتور جدید", "➕", "pri"), ("دریافت وجه", "💰", "pri"), ("بستن صندوق", "🔒", "sec"), ("گزارش روزانه", "📊", "sec")])
     + stat_row([("درآمد امروز", "۴۸.۵م ت", "▲ ۸٪ هفته", "c1", "💵"), ("صندوق باز", "۱۲.۸م ت", "▲ ۳ کاربر فعال", "c2", "🏦"),
                 ("بدهی بیماران", "۲۳.۴م ت", "▼ ۵٪", "c3", "📉"), ("سهم بیمه امروز", "۱۸.۲م ت", "▲ ارسال به سپاس", "c4", "🏛")])
     + table_panel("فاکتورهای امروز", ["شماره", "بیمار", "خدمات", "مبلغ کل", "سهم بیمه", "پرداختی", "روش پرداخت", "وضعیت"],
        [("#INV-3312", "مریم احمدی", "ویزیت + آزمایش", "۸۹۰٬۰۰۰", "۵۳۴٬۰۰۰", "۳۵۶٬۰۰۰", B.format("blue", "کارتخوان"), B.format("green", "تسویه")),
         ("#INV-3311", "علی رضایی", "ویزیت عمومی", "۲۵۰٬۰۰۰", "۰", "۲۵۰٬۰۰۰", B.format("green", "نقدی"), B.format("green", "تسویه")),
         ("#INV-3310", "نگار موسوی", "کنترل قلب + نوار قلب", "۱٬۲۰۰٬۰۰۰", "۷۲۰٬۰۰۰", "۴۸۰٬۰۰۰", B.format("blue", "کارتخوان"), B.format("amber", "در انتظار")),
         ("#INV-3309", "حسین کاظمی", "ویزیت اطفال", "۴۰۰٬۰۰۰", "۲۴۰٬۰۰۰", "۰", B.format("gray", "بدهی"), B.format("red", "بدهکار"))]))

# ─── ۱۰ حسابداری ───
view(10, "accounting", "حسابداری دوبل", "📒", "حسابداری",
     toolbar([("سند جدید", "➕", "pri"), ("تراز آزمایشی", "⚖️", "sec"), ("دفتر کل", "📖", "sec"), ("دوره مالی", "📅", "sec")])
     + stat_row([("اسناد این ماه", "۸۴۲", "▲ ۱۲٪", "c1", "📒"), ("تراز آزمایشی", "متوازن ✔", "▲ ۱۰۰٪", "c2", "⚖️"),
                 ("سند خودکار از فاکتور", "۷۹۶", "▲ ۹۴.۵٪", "c3", "⚡"), ("هزینه‌های ماه", "۱۴۲م ت", "▲ ۶٪", "c4", "📉")])
     + table_panel("اسناد حسابداری — دوره ۱۴۰۵/۰۷", ["شماره سند", "تاریخ", "شرح", "بدهکار", "بستانکار", "منشأ", "وضعیت"],
        [("#JV-1042", "۱۴۰۵/۰۷/۱۳", "درآمد ویزیت — فاکتور INV-3312", "۸۹۰٬۰۰۰", "—", B.format("blue", "خودکار فاکتور"), B.format("green", "تأیید شده")),
         ("#JV-1041", "۱۴۰۵/۰۷/۱۳", "دریافت نقدی صندوق ۱", "—", "۳۵۶٬۰۰۰", B.format("blue", "خودکار صندوق"), B.format("green", "تأیید شده")),
         ("#JV-1040", "۱۴۰۵/۰۷/۱۳", "سهم بیمه — مطالبات سپاس", "۵۳۴٬۰۰۰", "—", B.format("blue", "خودکار بیمه"), B.format("green", "تأیید شده")),
         ("#JV-1039", "۱۴۰۵/۰۷/۱۲", "خرید ملزومات مصرفی", "۳٬۲۰۰٬۰۰۰", "—", B.format("gray", "دستی"), B.format("amber", "در انتظار تأیید"))]))

# ─── ۱۱ فیش حقوقی ───
view(11, "payroll", "فیش حقوقی پرسنل", "🧾", "فیش حقوقی",
     toolbar([("صدور فیش ماه", "➕", "pri"), ("پیش‌نمایش چاپ", "🖨", "sec"), ("قواعد ۱۴۰۴", "📜", "sec")])
     + stat_row([("پرسنل حقوق‌بگیر", "۳۸", "▲ ۲ استخدام", "c1", "👔"), ("جمع حقوق مهر", "۸۹۲م ت", "▲ ۴.۲٪", "c2", "💰"),
                 ("حقوق درصدی از سود", "۶۱.۳م ت", "▲ از سود خدمات خود پرسنل", "c3", "📈"), ("کسورات بیمه/مالیات", "۱۰۴م ت", "▲ محاسبه خودکار", "c4", "📉")])
     + table_panel("فیش‌های حقوقی مهر ۱۴۰۵", ["پرسنل", "سمت", "حقوق پایه", "٪ از سود", "سود خدمات", "حقوق درصدی", "اضافه‌کاری", "بیمه ۷٪", "خالص پرداختی", "وضعیت"],
        [("رضا محمدنژاد", "پرستار", "۲۲م", "۳۰٪", "۴۸.۲م", "۱۴.۵م", "۲.۴م", "۲.۷م", "۳۵.۱م", B.format("green", "صادر شد")),
         ("فاطمه اکبری", "منشی پذیرش", "۱۲م", "—", "—", "—", "۰.۸م", "۰.۹م", "۱۱.۳م", B.format("green", "صادر شد")),
         ("مهدی قلی‌زاده", "آزمایشگاه", "۱۵م", "۲۵٪", "۶۲.۹م", "۱۵.۷م", "۳.۱م", "۲.۴م", "۲۵.۸م", B.format("blue", "در بررسی")),
         ("زهرا رحیمی", "کارمند مالی", "۱۲م", "۱۰٪", "۱۹.۴م", "۱.۹م", "—", "۱م", "۱۲.۲م", B.format("green", "صادر شد"))]))

# ─── ۱۲ حضور و غیاب ───
view(12, "attendance", "حضور و غیاب", "🕐", "حضور و غیاب",
     toolbar([("گرفتن ضربه‌ها", "🔄", "pri"), ("درخواست اصلاح کارت", "📝", "sec"), ("گزارش ماهانه", "📊", "sec")])
     + stat_row([("حاضر امروز", "۳۶/۳۸", "▲ ۹۴.۷٪", "c1", "✅"), ("در تأخیر", "۴", "▼ ۱ کمتر", "c3", "⏰"),
                 ("اضافه‌کاری دیروز", "۲۳ ساعت", "▲ ۵ ساعت", "c2", "🕐"), ("دستگاه متصل", "۲", "▲ ZKTeco + Hikvision", "c4", "🔌")])
     + table_panel("ضربه‌های امروز — ۱۴۰۵/۰۷/۱۳ (دریافت خودکار از دستگاه)", ["پرسنل", "ورود", "خروج", "شیفت", "تأخیر", "اضافه‌کاری", "وضعیت تطبیق"],
        [("رضا محمدنژاد", "۰۷:۵۲", "—", "صبح", "—", "—", B.format("green", "در شیفت")),
         ("فاطمه اکبری", "۰۸:۱۴", "—", "صبح", "۱۴ دقیقه", "—", B.format("amber", "تأخیر مجاز")),
         ("مهدی قلی‌زاده", "۰۷:۴۸", "—", "صبح", "—", "—", B.format("green", "در شیفت")),
         ("زهرا رحیمی", "۱۶:۰۲", "—", "عصر", "—", "—", B.format("green", "در شیفت")),
         ("علی نوروزی", "—", "—", "صبح", "—", "—", B.format("red", "غیبت — در حال بررسی"))]))

# ─── ۱۳ بیمه ───
view(13, "insurance", "بیمه و مطالبات", "🛡", "بیمه",
     toolbar([("شرکت بیمه جدید", "➕", "pri"), ("ارسال تراکنش", "📤", "sec"), ("پیگیری مطالبات", "📞", "sec")])
     + stat_row([("تراکنش‌های ارسالی ماه", "۸۱۲", "▲ ۱۴٪", "c1", "📤"), ("مطالبات وصول‌نشده", "۶۴.۲م ت", "▼ ۸٪", "c3", "⏳"),
                 ("نرخ پذیرش اولیه", "۹۶.۸٪", "▲ ۱.۲٪", "c2", "✅"), ("درآمد بیمه امروز", "۱۸.۲م ت", "▲ ۳۷٪", "c4", "💰")])
     + table_panel("مطالبات دوره‌ای از بیمه‌ها", ["بیمه", "دوره", "تعداد تراکنش", "مبلغ مطالبه", "وصول‌شده", "تاریخ ارسال", "وضعیت"],
        [("تأمین اجتماعی", "مهر ۱۴۰۵", "۴۲۴", "۱۴۸.۶م", "۸۴.۲م", "۱۴۰۵/۰۷/۱۰", B.format("blue", "در پیگیری")),
         ("نیروهای مسلح", "مهر ۱۴۰۵", "۱۸۶", "۶۲.۴م", "۳۱.۰م", "۱۴۰۵/۰۷/۱۰", B.format("blue", "در پیگیری")),
         ("سلامت", "شهریور ۱۴۰۵", "۲۰۲", "۵۸.۹م", "۵۸.۹م", "۱۴۰۵/۰۶/۲۸", B.format("green", "وصول کامل")),
         ("آزاد (نقدی)", "—", "—", "—", "—", "—", B.format("gray", "بدون مطالبه"))]))

# ─── ۱۴ پاراکلینیک ───
view(14, "paraclinical", "مرکز پاراکلینیک", "🧪", "پاراکلینیک",
     toolbar([("سفارش جدید", "➕", "pri"), ("نمایشگر DICOM", "🩻", "sec"), ("نتایج امروز", "📋", "sec")])
     + stat_row([("سفارش‌های فعال", "۲۴", "▲ ۸ آزمایش", "c1", "🧪"), ("تصویربرداری امروز", "۱۱", "▲ ۴ رادیولوژی", "c2", "🩻"),
                 ("نتایج آماده تحویل", "۹", "▲ پیامک خودکار", "c3", "✅"), ("میانگین زمان پاسخ", "۴۲ دقیقه", "▼ ۶ دقیقه", "c4", "⏱")])
     + table_panel("سفارش‌های پاراکلینیک امروز", ["شماره", "بیمار", "نوع", "شرح", "پزشک درخواستگر", "پاسخ‌دهنده", "وضعیت"],
        [("#PC-521", "مریم احمدی", B.format("purple", "آزمایش"), "CBC + FBS", "دکتر کریمی", "آزمایشگاه", B.format("blue", "در حال انجام")),
         ("#PC-520", "حسین کاظمی", B.format("blue", "تصویربرداری"), "سونوگرافی شکم", "دکتر رضایی", "رادیولوژی", B.format("amber", "رزرو شده")),
         ("#PC-519", "نگار موسوی", B.format("purple", "آزمایش"), "آزمایش قلب + تیروئید", "دکتر احمدی", "آزمایشگاه", B.format("green", "آماده تحویل")),
         ("#PC-518", "رضا حسینی", B.format("blue", "تصویربرداری"), "رادیوگرافی زانو", "دکتر صادقی", "رادیولوژی", B.format("green", "گزارش شد")),
         ("#PC-517", "سارا محمدی", B.format("purple", "آزمایش"), "آزمایش ادرار", "دکتر رضایی", "آزمایشگاه", B.format("green", "آماده تحویل"))]))

# ─── ۱۵ آزمایشگاه ───
view(15, "lab", "آزمایشگاه", "🔬", "آزمایشگاه",
     toolbar([("ثبت نتیجه", "✏️", "pri"), ("تأیید آزمایش‌شناس", "✅", "sec"), ("ارسال پیامک نتیجه", "✉️", "sec")])
     + stat_row([("آزمایش امروز", "۱۸", "▲ ۵ فوری", "c1", "🔬"), ("در انتظار نتیجه", "۶", "▼ ۲ کمتر", "c3", "⏳"),
                 ("تأیید‌شده امروز", "۱۲", "▲ ۹۸٪ دقیق", "c2", "✅"), ("پانل‌های تعریفی", "۴۲", "▲ سفارشی", "c4", "🧫")])
     + table_panel("نمونه‌های در جریان", ["کد نمونه", "بیمار", "پانل", "دریافت", "FPR", "نتیجه", "وضعیت"],
        [("S-8842", "مریم احمدی", "CBC + FBS", "۰۹:۱۵", "۲ ساعت", "—", B.format("blue", "در حال آنالیز")),
         ("S-8841", "نگار موسوی", "آزمایش قلب", "۰۸:۵۰", "۴ ساعت", "LDL: ۱۴۲ ↑", B.format("amber", "در بازبینی")),
         ("S-8840", "رضا حسینی", "CRP", "۰۸:۳۲", "۱ ساعت", "منفی", B.format("green", "تأیید شد")),
         ("S-8839", "سارا محمدی", "آزمایش ادرار", "۰۸:۱۰", "۲ ساعت", "طبیعی", B.format("green", "تأیید شد")),
         ("S-8838", "علی رضایی", "ویتامین D", "۰۸:۰۵", "۶ ساعت", "—", B.format("gray", "ارسال به مرجع"))]))

# ─── ۱۶ داروخانه ───
view(16, "pharmacy", "داروخانه و انبار", "💊", "داروخانه",
     toolbar([("رسید انبار", "📦", "pri"), ("خروج دارو", "📤", "sec"), ("زیر حد بحرانی", "⚠️", "sec")])
     + stat_row([("اقلام فعال", "۴۸۶", "▲ ۱۲ قلم جدید", "c1", "💊"), ("امروز صادر شد", "۳۷ قلم", "▲ ۸ نسخه", "c2", "📤"),
                 ("زیر حد بحرانی", "۲", "▲ سفارش خرید", "c5", "⚠️"), ("نرخ گردش انبار", "۴.۲", "▲ ۰.۳", "c4", "🔄")])
     + table_panel("موجودی داروها (گرفتی از انبارگردانی)", ["کد", "دارو", "دسته", "موجودی", "حد بحرانی", "واحد", "انقضا", "وضعیت"],
        [("D-1001", "استامینوفن کدئین", "مسکن", "۴۲۰", "۱۰۰", "قرص", "۱۴۰۶/۰۳", B.format("green", "نرمال")),
         ("D-1002", "آموکسی‌سیلین ۵۰۰", "آنتی‌بیوتیک", "۸۵", "۱۵۰", "کپسول", "۱۴۰۵/۱۲", B.format("red", "زیر حد بحرانی")),
         ("D-1003", "متفرمین ۵۰۰", "دیابت", "۱٬۲۴۰", "۲۰۰", "قرص", "۱۴۰۶/۰۸", B.format("green", "نرمال")),
         ("D-1004", "وارفارین ۵", "ضد انعقاد", "۱۶۰", "۸۰", "قرص", "۱۴۰۵/۱۰", B.format("amber", "انقضا نزدیک")),
         ("D-1005", "سرم رینگر", "سرم", "۶۴", "۵۰", "بطری", "۱۴۰۶/۰۱", B.format("green", "نرمال"))]))

# ─── ۱۷ انبارگردانی ───
view(17, "stocktaking", "انبارگردانی", "📦", "انبارگردانی",
     toolbar([("شمارش جدید", "➕", "pri"), ("شمارش جاری", "🔢", "sec"), ("اختلاف‌ها", "⚖️", "sec")])
     + stat_row([("شمارش‌های امسال", "۶", "▲ ماهانه", "c1", "📦"), ("اقلام شمارش‌شده", "۴۶۸/۴۸۶", "▲ ۹۶.۳٪", "c2", "🔢"),
                 ("اختلاف امروز", "۷ قلم", "▲ ۳ کمبود / ۴ اضافه", "c3", "⚖️"), ("ارزش اختلاف", "۲.۱م ت", "▼ ۰.۴٪ انبار", "c4", "💰")])
     + table_panel("شمارش جاری — دسته دارویی", ["کد", "کالا", "موجودی سیستمی", "شمارش‌شده", "اختلاف", "شمارنده", "وضعیت"],
        [("D-1001", "استامینوفن کدئین", "۴۲۰", "۴۱۸", "-۲", "زهرا رحیمی", B.format("amber", "اختلاف کمبود")),
         ("D-1003", "متفرمین ۵۰۰", "۱٬۲۴۰", "۱٬۲۴۰", "۰", "زهرا رحیمی", B.format("green", "مطابق")),
         ("D-1005", "سرم رینگر", "۶۴", "۶۶", "+۲", "مهدی قلی‌زاده", B.format("blue", "اختلاف اضافه")),
         ("D-1009", "روغن سیر", "۳۲", "۳۲", "۰", "مهدی قلی‌زاده", B.format("green", "مطابق"))]))

# ─── ۱۸ گزارش‌ها ───
view(18, "reports", "گزارش‌ساز پیشرفته", "📈", "گزارش‌ها",
     toolbar([("گزارش جدید", "➕", "pri"), ("خروجی اکسل", "📗", "sec"), ("زمان‌بندی ارسال", "⏰", "sec")])
     + '<div class="cards" style="grid-template-columns:repeat(3,1fr)">'
     + "".join(f'<div class="panel"><div class="ph"><b>{i} {t}</b><small>{c}</small></div>'
               f'<div style="display:flex;align-items:center;justify-content:space-between;margin-top:6px">'
               f'<span style="font-size:11px;color:#8296B3">{d}</span>'
               f'<button class="btn sec" style="padding:5px 12px;font-size:10.5px">اجرا ▶</button></div></div>'
               for (i, t, c, d) in [
                   ("💰", "سود و زیان دوره", "مالی", "درآمد - هزینه - خالص"),
                   ("📊", "آمار مراجعات", "مدیریتی", "روند ماهانه + الگوی ساعتی"),
                   ("🏥", "بیماری‌های مزمن ICD-10", "بالینی", "توزیع کدهای تشخیص"),
                   ("👨‍⚕️", "عملکرد پزشکان", "مدیریتی", "ویزیت × رضایت × درآمد"),
                   ("🏦", "صورت‌حساب بیمه", "بیمه", "تفکیک شرکت و دوره"),
                   ("🕐", "کارکرد پرسنل", "منابع انسانی", "حضور + اضافه‌کاری"),
                   ("🧪", "پرتکرارترین آزمایش‌ها", "پاراکلینیک", "رتبه‌بندی پانل‌ها"),
                   ("💊", "مصرف دارو", "داروخانه", "پرفروش‌ها + منقضی‌شونده"),
                   ("👤", "بدهکاران برتر", "مالی", "بیماران بدهکار")])
     + '</div>')

# ─── ۱۹ کاربران ───
view(19, "users", "مدیریت کاربران", "👤", "کاربران",
     toolbar([("کاربر جدید", "➕", "pri"), ("بازنشانی رمز", "🔑", "sec"), ("قفل کاربر", "🔒", "sec")])
     + stat_row([("کاربران فعال", "۱۹", "▲ ۲ آنلاین", "c1", "👤"), ("نقش‌های تعریفی", "۸", "▲ ۱۶۰ دسترسی ریزبین", "c2", "🔑"),
                 ("قفل موقت امروز", "۱", "▼ پس از ۵ تلاش", "c3", "🔒"), ("ورودهای موفق امروز", "۲۳", "▲ همه رمز قوی", "c4", "✅")])
     + table_panel("کاربران سیستم", ["نام کاربری", "نام", "نقش", "شعبه", "آخرین ورود", "وضعیت"],
        [("a.karimi", "آقای کریمی", "مدیر سیستم", "مرکزی", "۱۴۰۵/۰۷/۱۳ ۰۸:۰۲", B.format("green", "فعال")),
         ("f.akbari", "فاطمه اکبری", "پذیرش", "مرکزی", "۱۴۰۵/۰۷/۱۳ ۰۸:۱۴", B.format("green", "فعال")),
         ("m.gholi", "مهدی قلی‌زاده", "آزمایشگاه", "مرکزی", "۱۴۰۵/۰۷/۱۳ ۰۷:۴۸", B.format("green", "فعال")),
         ("s.rezaei", "دکتر رضایی", "پزشک", "مرکزی", "۱۴۰۵/۰۷/۱۳ ۰۸:۰۰", B.format("green", "فعال")),
         ("h.nasim", "حسین نسیمی", "حسابداری", "شعبه ۲", "۱۴۰۵/۰۷/۱۲ ۱۶:۲۰", B.format("gray", "غیرفعال"))]))

# ─── ۲۰ نقش‌ها ───
view(20, "roles", "نقش‌ها و دسترسی‌ها", "🔑", "نقش‌ها",
     toolbar([("نقش جدید", "➕", "pri"), ("کپی از نقش", "📑", "sec")])
     + '<div class="grid2 grow" style="grid-template-columns:1fr 1.6fr">'
     + '<div class="panel"><div class="ph"><b>نقش‌های سیستم</b><small>۸ نقش</small></div>'
     + queue_rows([("👨‍💼", "مدیر سیستم", "دسترسی کامل", B.format("blue", "۱۶۰/۱۶۰")),
                   ("🩺", "پزشک", "پرونده + نسخه", B.format("green", "۴۸/۱۶۰")),
                   ("🛎", "پذیرش", "بیمار + نوبت + صندوق", B.format("green", "۳۶/۱۶۰")),
                   ("🔬", "آزمایشگاه", "سفارش + نتیجه", B.format("green", "۲۴/۱۶۰")),
                   ("💰", "حسابداری", "اسناد + گزارش مالی", B.format("green", "۴۲/۱۶۰"))])
     + '</div>'
     + '<div class="panel"><div class="ph"><b>ماتریس دسترسی نقش «پذیرش»</b><small>۳۶ دسترسی فعال</small></div>'
     + '<div class="grid3" style="gap:8px">'
     + "".join(f'<div style="background:{"#E1F7EE" if a else "#F4F6F9"};color:{"#0B7A55" if a else "#9AA8BC"};'
               f'border-radius:9px;padding:8px 11px;font-size:10.8px;font-weight:700">{("✅ " if a else "✕ ")}{t}</div>'
               for (t, a) in [("مشاهده بیماران", True), ("ثبت بیمار", True), ("ویرایش بیمار", True),
                              ("حذف بیمار", False), ("ثبت نوبت", True), ("لغو نوبت", True),
                              ("پذیرش حضوری", True), ("بستن صندوق", True), ("ابطال فاکتور", False),
                              ("مشاهده پرونده", True), ("نسخه‌نویسی", False), ("ثبت نتیجه آزمایش", False),
                              ("مشاهده گزارش مالی", False), ("مدیریت کاربران", False), ("تنظیمات سیستم", False),
                              ("چاپ فاکتور", True), ("ارسال پیامک", True), ("خروجی اکسل", True)])
     + '</div></div></div>')

# ─── ۲۱ پیامک ───
view(21, "sms", "پیامک هوشمند", "✉️", "پیامک",
     toolbar([("ارسال گروهی", "📢", "pri"), ("قالب جدید", "📝", "sec"), ("گزارش تحویل", "📊", "sec")])
     + stat_row([("ارسال این ماه", "۱٬۲۴۸", "▲ ۱۴٪", "c1", "✉️"), ("نرخ تحویل", "۹۸.۴٪", "▲ ۰.۶٪", "c2", "✅"),
                 ("یادآوری نوبت امروز", "۶۷", "▲ ۶۵ تأیید", "c3", "⏰"), ("لینک نظرسنجی امروز", "۵۸", "▲ ۱۲ پاسخ", "c4", "📝")])
     + '<div class="grid2 grow"><div class="panel"><div class="ph"><b>✏️ ارسال سریع</b></div>'
     + form_fields([("گیرندگان", "۶۷ بیمار ویزیت امروز", "👥"), ("قالب پیام", "یادآوری نوبت", "📄")])
     + '<div class="fld" style="margin-top:12px"><label>متن پیام (۷۰ - ۱۰۶ کاراکتر فارسی)</label>'
       '<div style="min-height:110px;border:1.5px solid #D8E2EF;border-radius:9px;padding:11px 13px;font-size:12px;color:#2B3B52;background:#fff">'
       '[بیمار گرامی] نوبت شما فردا ساعت [ساعت] نزد دکتر [پزشک] در درمانگاه سهند ثبت شد. لغو: ۰۴۱-۳۴۴۳۶۱۴۱</div></div>'
     + '<div style="display:flex;gap:9px;margin-top:12px"><button class="btn pri">📤 ارسال هم‌اکنون</button>'
       '<button class="btn sec">⏰ ارسال زمان‌بندی‌شده</button></div></div>'
     + table_panel("خروجی امروز", ["زمان", "گیرنده", "نوع", "متن", "اپراتور", "وضعیت"],
        [("۱۰:۱۲", "۰۹۱۴…۱۲۳", "نظرسنجی", "رضایت از ویزیت؟ /s/8x2…", "همراه اول", B.format("green", "مستقیم")),
         ("۱۰:۱۰", "۰۹۳۵…۴۵۶", "یادآوری", "نوبت فردا ۰۹:۳۰ — دکتر احمدی", "ایرانسل", B.format("green", "مستقیم")),
         ("۱۰:۰۵", "۰۹۱۲…۷۸۹", "نتیجه آزمایش", "نتیجه آماده است — کد پیگیری ۸۸۴۲", "همراه اول", B.format("green", "رسیده"),
         ), ("۰۹:۵۸", "۰۹۱۴…۳۲۱", "بدهکار", "یادآوری بدهی ۲۴۰٬۰۰۰ تومان", "ایرانسل", B.format("amber", "در صف ارسال"))])
     + '</div>')

# ─── ۲۲ نظرسنجی ───
view(22, "surveys", "نظرسنجی بیماران", "📝", "نظرسنجی بیماران",
     toolbar([("نظرسنجی جدید", "➕", "pri"), ("ارسال لینک به ویزیت‌های امروز", "📤", "pri"), ("نتایج", "📊", "sec")])
     + stat_row([("نظرسنجی‌های فعال", "۲", "▲ سازنده پویا", "c1", "📝"), ("پاسخ این ماه", "۳۱۲", "▲ ۲۴٪", "c2", "🗳"),
                 ("میانگین رضایت", "۴.۴/۵", "▲ ۰.۲", "c3", "⭐"), ("نرخ پاسخ‌گویی", "۴۸٪", "▲ +۶٪", "c4", "📈")])
     + '<div class="grid2 grow" style="grid-template-columns:1.4fr 1fr">'
     + table_panel("نظرسنجی‌ها", ["عنوان", "سوالات", "پاسخ‌ها", "میانگین", "وضعیت"],
        [("رضایت از ویزیت پزشک", "۴", "۳۱۲", "۴.۴/۵", B.format("green", "فعال")),
         ("کیفیت پذیرش و برخورد", "۳", "۱۹۶", "۴.۶/۵", B.format("green", "فعال")),
         ("نظرسنجی فصلی تابستان", "۶", "۴۲۸", "۴.۲/۵", B.format("gray", "پایان‌یافته"))])
     + '<div class="panel"><div class="ph"><b>⭐ توزیع امتیاز — رضایت از ویزیت</b><small>۳۱۲ پاسخ</small></div>'
     + '<div style="display:flex;flex-direction:column;gap:9px;margin-top:4px">'
     + "".join(f'<div><div style="display:flex;justify-content:space-between;font-size:11.5px;margin-bottom:4px"><span>{"⭐" * n}</span><b>{c} پاسخ ({p}%)</b></div><div class="progress"><div style="width:{p}%;{"background:linear-gradient(90deg,#17A589,#43C59E)" if n >= 4 else ""}"></div></div></div>'
               for (n, c, p) in [(5, 148, 47), (4, 96, 31), (3, 41, 13), (2, 17, 5), (1, 10, 3)])
     + '</div><div class="chips" style="margin-top:14px"><span class="chip">🩺 دکتر رضایی: ۴.۸</span>'
       '<span class="chip">🩺 دکتر کریمی: ۴.۶</span><span class="chip">🩺 دکتر نجفی: ۴.۲</span></div></div></div>')

# ─── ۲۳ پرسنل ───
view(23, "personnel", "مدیریت پرسنل", "👔", "پرسنل",
     toolbar([("پرسنل جدید", "➕", "pri"), ("قرارداد", "📜", "sec"), ("سابقه", "🗂", "sec")])
     + stat_row([("کل پرسنل", "۳۸", "▲ ۲ استخدام ماه", "c1", "👔"), ("قرارداد فعال", "۳۶", "▲ ۲ در تمدید", "c2", "📜"),
                 ("پرسنل شعبه ۲", "۹", "▲ ۲۳.۷٪", "c3", "🏢"), ("میانگین سابقه", "۴.۲ سال", "▲ وفاداری بالا", "c4", "⏳")])
     + table_panel("فهرست پرسنل", ["کد پرسنلی", "نام", "سمت", "شعبه", "حقوق پایه", "٪ از سود خدمات", "نوع قرارداد", "وضعیت"],
        [("P-201", "رضا محمدنژاد", "پرستار", "مرکزی", "۲۲٬۰۰۰٬۰۰۰", "۳۰٪", B.format("green", "رسمی"), B.format("green", "شاغل")),
         ("P-202", "فاطمه اکبری", "منشی پذیرش", "مرکزی", "۱۲٬۰۰۰٬۰۰۰", "—", B.format("blue", "پروژه‌ای"), B.format("green", "شاغل")),
         ("P-203", "مهدی قلی‌زاده", "کارشناس آزمایشگاه", "مرکزی", "۱۵٬۰۰۰٬۰۰۰", "۲۵٪", B.format("green", "رسمی"), B.format("green", "شاغل")),
         ("P-208", "زهرا رحیمی", "کارمند مالی", "شعبه ۲", "۱۲٬۰۰۰٬۰۰۰", "۱۰٪", B.format("amber", "آزمایشی"), B.format("green", "شاغل")),
         ("P-210", "علی نوروزی", "خدمات", "شعبه ۲", "۸٬۵۰۰٬۰۰۰", "—", B.format("gray", "ساعتی"), B.format("amber", "مرخصی"))]))

# ─── ۲۴ تنظیمات ───
view(24, "settings", "تنظیمات سیستم", "⚙️", "تنظیمات",
     '<div class="chips"><span class="chip">🏥 اطلاعات درمانگاه</span><span class="chip">🖨 چاپ و سربرگ</span>'
     '<span class="chip">✉️ پیامک</span><span class="chip">🗄 پشتیبان‌گیری</span><span class="chip">🌐 پشتیبانی و لایسنس</span>'
     '<span class="chip">🎨 ظاهر برنامه</span></div>'
     + form_fields([("نام درمانگاه", "درمانگاه سهند", "🏥"), ("کد اقتصادی", "۴۱۱۲۳۴۵۶۷۸۹۰", "🔢"),
                    ("شناسه ملی", "۱۰۸۶۱۲۳۴۵۶۷", "🔢"), ("تلفن", "۰۴۱-۳۳۴۵۶۷۸۹", "📞"),
                    ("ایمیل", "info@my-clinic.ir", "✉️"), ("آدرس", "تبریز — خیابان …", "📍")])
     + '<div class="panel"><div class="ph"><b>🧑‍💻 اطلاعات پشتیبانی محصول</b><small>دریافت خودکار از سرور لایسنس — قابل ویرایش از پنل</small></div>'
     + form_fields([("نام پشتیبان", "سعید نجفی", "👤"), ("شرکت", "سهند سرویس", "🏢"),
                    ("تلفن پشتیبانی", "۰۴۱-۳۴۴۳۶۱۴۱", "📞"), ("موبایل", "۰۹۱۴۴۲۲۰۴۳۰", "📱"),
                    ("ایمیل پشتیبانی", "info@ea-fixer.ir", "✉️"), ("وب‌سایت", "ea-fixer.ir", "🌐")])
     + '</div>')

# ─── ۲۵ سپاس ───
view(25, "sepas", "سامانه سپاس", "🏦", "سپاس",
     toolbar([("ارسال تراکنش‌های امروز", "📤", "pri"), ("استعلام وضعیت", "🔄", "sec"), ("بازارسال خطادار", "🔁", "sec")])
     + stat_row([("ارسال امروز", "۶۷", "▲ ۸۳٪ از کل", "c1", "📤"), ("تأیید‌شده", "۵۹", "▲ ۸۸٪", "c2", "✅"),
                 ("خطا / بازگشتی", "۳", "▼ اصلاح خودکار", "c5", "⚠️"), ("در صف ارسال", "۵", "▲ تلاش مجدد ۳۰ دقیقه", "c3", "⏳")])
     + table_panel("تراکنش‌های سپاس امروز", ["شماره پیگیری", "بیمار", "خدمت", "مبلغ", "نوع تراکنش", "وضعیت", "پاسخ سامانه"],
        [("SP-88123", "مریم احمدی", "ویزیت داخلی", "۸۹۰٬۰۰۰", "OPD", B.format("green", "تأیید"), "کد رهگیری: ۹۲۳۴۵"),
         ("SP-88122", "نگار موسوی", "نوار قلب", "۷۲۰٬۰۰۰", "OPD", B.format("green", "تأیید"), "کد رهگیری: ۹۲۳۴۴"),
         ("SP-88121", "حسین کاظمی", "ویزیت اطفال", "۴۰۰٬۰۰۰", "OPD", B.format("amber", "در انتظار"), "—"),
         ("SP-88120", "رضا حسینی", "رادیوگرافی", "۹۵۰٬۰۰۰", "IMG", B.format("red", "خطای اعتبارسنجی"), "نسخه نامعتبر — بازارسال")], "اتصال رسمی + حالت شبیه‌ساز"))

# ─── ۲۶ نسخه الکترونیک ───
view(26, "erx", "نسخه الکترونیک", "☁️", "نسخه الکترونیک",
     toolbar([("ارسال نسخه به سامانه", "☁️", "pri"), ("استعلام داروخانه", "🔍", "sec"), ("لینک رهگیری", "🔗", "sec")])
     + stat_row([("نسخه الکترونیک امروز", "۱۹", "▲ ۸۳٪ نسخه‌ها", "c1", "☁️"), ("تأیید Samat", "۱۷", "▲ ۸۹٪", "c2", "✅"),
                 ("ارسال نهایی به داروخانه", "۱۴", "▲ ۷۴٪", "c3", "💊"), ("خطا", "۰", "▲ بدون خطا", "c6", "✅")])
     + table_panel("نسخه‌های الکترونیک امروز", ["شماره نسخه", "بیمار", "پزشک", "تعداد قلم", "داروخانه مقصد", "وضعیت"],
        [("eRx-4412", "مریم احمدی", "دکتر کریمی", "۳", "داروخانه سهند", B.format("green", "تحویل شد")),
         ("eRx-4411", "علی رضایی", "دکتر نجفی", "۲", "داروخانه آتی‌دارو", B.format("green", "تحویل شد")),
         ("eRx-4410", "نگار موسوی", "دکتر احمدی", "۴", "داروخانه سهند", B.format("blue", "در سامانه Samat")),
         ("eRx-4409", "سارا محمدی", "دکتر رضایی", "۲", "داروخانه شهر", B.format("amber", "امضای دیجیتال در انتظار"))]))

# ─── ۲۷ شعبه‌ها ───
view(27, "branches", "مدیریت شعبه‌ها", "🏢", "شعبه‌ها",
     toolbar([("شعبه جدید", "➕", "pri"), ("مقایسه عملکرد", "📊", "sec")])
     + '<div class="grid2">'
     + '<div class="panel"><div class="ph"><b>🏥 شعبه مرکزی — تبریز</b><small>اصلی</small></div>'
     + '<div class="cards" style="grid-template-columns:1fr 1fr">'
     + '<div class="stat c1"><div class="lb">بیماران امروز</div><div class="vl">۱۰۹</div></div>'
     + '<div class="stat c2"><div class="lb">درآمد امروز</div><div class="vl">۴۱.۲م</div></div>'
     + '<div class="stat c3"><div class="lb">پرسنل</div><div class="vl">۲۹</div></div>'
     + '<div class="stat c4"><div class="lb">کاربران فعال</div><div class="vl">۱۵</div></div></div></div>'
     + '<div class="panel"><div class="ph"><b>🏢 شعبه ۲ — آذرشهر</b><small>از ۱۴۰۳</small></div>'
     + '<div class="cards" style="grid-template-columns:1fr 1fr">'
     + '<div class="stat c1"><div class="lb">بیماران امروز</div><div class="vl">۱۹</div></div>'
     + '<div class="stat c2"><div class="lb">درآمد امروز</div><div class="vl">۷.۳م</div></div>'
     + '<div class="stat c3"><div class="lb">پرسنل</div><div class="vl">۹</div></div>'
     + '<div class="stat c4"><div class="lb">کاربران فعال</div><div class="vl">۴</div></div></div></div>'
     + '</div>'
     + table_panel("خلاصه عملکرد شعبه‌ها (هفته جاری)", ["شعبه", "بیماران", "درآمد", "سهم", "نرخ رضایت", "وضعیت"],
        [("مرکزی — تبریز", "۷۱۲", "۲۸۴م", "۸۵٪", "۴.۵/۵", B.format("green", "فعال")),
         ("۲ — آذرشهر", "۱۲۶", "۴۹م", "۱۵٪", "۴.۳/۵", B.format("green", "فعال"))]))

# ─── ۲۸ بکاپ ───
view(28, "backup", "بکاپ و بازیابی", "💾", "بکاپ و بازیابی",
     toolbar([("تهیه بکاپ فوری", "💾", "pri"), ("بازیابی از فایل", "♻️", "sec"), ("تنظیم زمان‌بندی", "⏰", "sec")])
     + stat_row([("آخرین بکاپ", "امروز ۰۳:۰۰", "▲ موفق", "c2", "✅"), ("حجم آخرین بکاپ", "۸۴۲ مگابایت", "▲ فشرده", "c1", "💾"),
                 ("بکاپ‌های نگهداری‌شده", "۳۰", "▲ چرخش خودکار", "c3", "🗂"), ("زمان‌بندی", "روزانه ۰۳:۰۰", "▲ فعال", "c4", "⏰")])
     + table_panel("تاریخچه پشتیبان‌گیری", ["زمان", "نوع", "حجم", "مدت", "محل نگهداری", "وضعیت"],
        [("۱۴۰۵/۰۷/۱۳ ۰۳:۰۰", B.format("blue", "کامل"), "۸۴۲ MB", "۴ دقیقه", "درایو محلی E:", B.format("green", "موفق")),
         ("۱۴۰۵/۰۷/۱۲ ۰۳:۰۰", B.format("blue", "کامل"), "۸۳۸ MB", "۴ دقیقه", "درایو محلی E:", B.format("green", "موفق")),
         ("۱۴۰۵/۰۷/۱۲ ۱۲:۰۰", B.format("purple", "افتراقی"), "۳۸ MB", "۴۱ ثانیه", "درایو محلی E:", B.format("green", "موفق")),
         ("۱۴۰۵/۰۷/۱۱ ۰۳:۰۰", B.format("blue", "کامل"), "۸۳۱ MB", "۴ دقیقه", "درایو محلی E:", B.format("green", "موفق"))]))

# ─── ۲۹ لاگ عملیات ───
view(29, "audit", "لاگ عملیات (ممیزی)", "🧾", "لاگ عملیات",
     toolbar([("فیلتر پیشرفته", "🔎", "sec"), ("خروجی لاگ", "📗", "sec"), ("پاک‌سازی قدیمی", "🧹", "sec")], "کاربر، جدول یا عملیات…")
     + stat_row([("رویدادهای امروز", "۲٬۴۸۴", "▲ ۸٪", "c1", "🧾"), ("ورودهای موفق", "۲۳", "▲ همه مجاز", "c2", "✅"),
                 ("تغییرات حساس", "۳۱", "▲ فیلدها ماسک شد", "c3", "🔐"), ("خطاهای امنیتی", "۰", "▲ بدون رخداد", "c6", "🛡")])
     + table_panel("رویدادهای اخیر — با مقدار قبلی/جدید", ["زمان", "کاربر", "عملیات", "موجودیت", "قبلی", "جدید", "نتیجه"],
        [("۱۰:۲۲", "f.akbari", "ویرایش", "بیمار MR-1039", "موبایل: ۰۹۱۲…", "موبایل: ۰۹۳۵…", B.format("green", "موفق")),
         ("۱۰:۱۸", "s.rezaei", "ایجاد", "نسخه #P-1204", "—", "۳ قلم دارو", B.format("green", "موفق")),
         ("۱۰:۰۵", "a.karimi", "تغییر نقش", "کاربر m.gholi", "آزمایشگاه", "کارشناس ارشد", B.format("green", "موفق")),
         ("۰۹:۵۶", "ناشناس ×۳", "ورود ناموفق", "کاربر h.nasim", "—", "قفل موقت ۱۵ دقیقه", B.format("red", "قفل شد")),
         ("۰۹:۴۱", "f.akbari", "چاپ", "فاکتور INV-3312", "—", "۲ نسخه", B.format("green", "موفق"))]))

# ─── ۳۰ لایسنس ───
view(30, "license", "وضعیت لایسنس", "🔑", "لایسنس",
     '<div class="panel"><div class="ph"><b>🔑 لایسنس درمانگاه</b>'
     '<span class="badge b-green">فعال — ۲۴۳ روز باقیمانده</span></div>'
     + '<div class="grid3">'
     + '<div class="stat c1"><div class="lb">کلید لایسنس</div><div class="vl" style="font-size:15px;direction:ltr">SCPRO-K7M2P-9X4RT-WQ3BN</div></div>'
     + '<div class="stat c2"><div class="lb">نوع اشتراک</div><div class="vl">یک‌ساله</div><div class="tr up">تا ۱۴۰۶/۰۷/۱۰</div></div>'
     + '<div class="stat c3"><div class="lb">فعال‌سازی</div><div class="vl">برخط</div><div class="tr up">۱۴۰۵/۰۴/۱۸</div></div>'
     + '</div><div style="display:flex;gap:8px;margin-top:12px;flex-wrap:wrap">'
     + "".join(f'<span class="badge b-{"green" if a else "gray"}">{"✅" if a else "▫️"} {t}</span>'
               for (t, a) in [("بیماران", True), ("نوبت‌دهی", True), ("پذیرش", True), ("نسخه‌نویسی", True),
                              ("صندوق", True), ("آزمایشگاه", True), ("داروخانه", True), ("حسابداری", True),
                              ("حضور و غیاب", True), ("نظرسنجی", True), ("فیش حقوقی", True), ("پاراکلینیک", True),
                              ("سامانه دولتی", True), ("پیامک", True), ("دندانپزشکی", False), ("فیزیوتراپی", False)])
     + '</div></div>'
     + '<div class="panel"><div class="ph"><b>🧑‍💻 پشتیبانی محصول</b><small>دریافت خودکار از سرور لایسنس هنگام فعال‌سازی</small></div>'
     + '<div class="grid3">'
     + '<div class="stat c1"><div class="lb">👤 پشتیبان</div><div class="vl" style="font-size:16px">سعید نجفی</div><div class="tr up">سهند سرویس</div></div>'
     + '<div class="stat c2"><div class="lb">📞 تلفن</div><div class="vl" style="font-size:16px;direction:ltr">041-34436141</div><div class="tr up" style="direction:ltr">09144220430</div></div>'
     + '<div class="stat c3"><div class="lb">✉️ ایمیل و وب</div><div class="vl" style="font-size:13px;direction:ltr">info@ea-fixer.ir</div><div class="tr up" style="direction:ltr">ea-fixer.ir</div></div>'
     + '</div></div>')

# ─── ۳۱ قالب چاپ و طب مکمل ───
view(31, "complementary", "قالب چاپ و طب مکمل", "🎨", "قالب چاپ",
     toolbar([("قالب جدید", "➕", "pri"), ("پیش‌نمایش", "👁", "sec"), ("تنظیم پیش‌فرض", "✔️", "sec")])
     + '<div class="grid3">'
     + "".join(f'<div class="panel"><div class="ph"><b>{i} {t}</b><small>{c}</small></div>'
               f'<div style="border:1.5px dashed #CBD9EA;border-radius:10px;height:120px;margin-top:6px;'
               f'display:flex;align-items:center;justify-content:center;font-size:11px;color:#8296B3">{d}</div>'
               f'<div style="display:flex;gap:6px;margin-top:9px"><button class="btn sec" style="padding:5px 11px;font-size:10.5px;flex:1">ویرایش</button>'
               f'<button class="btn {"pri" if df else "sec"}" style="padding:5px 11px;font-size:10.5px;flex:1">{"پیش‌فرض ✔" if df else "انتخاب"}</button></div></div>'
               for (i, t, c, d, df) in [
                   ("📄", "فاکتور A5", "مالی", "پیش‌نمایش سربرگ دارو", True),
                   ("🧾", "فیش حقوقی", "منابع انسانی", "جدول مزایا/کسورات", True),
                   ("💊", "نسخه الکترونیک", "درمان", "قالب رسمی وزارت بهداشت", True),
                   ("📋", "نتیجه آزمایش", "آزمایشگاه", "A4 با بازه نرمال", True),
                   ("🧑‍🤝‍🧑", "شناسنامه بیمار", "پذیرش", "کارت پرسنلی A6", False),
                   ("🌿", "برگه طب مکمل", "درمان", "فرم طب سنتی", False)])
     + '</div>')

# ─── ۳۲ اعلان‌ها ───
view(32, "notifications", "اعلان‌ها و یادآورها", "🔔", "داشبورد",
     toolbar([("علامت‌گذاری همه خوانده‌شده", "✔️", "sec"), ("تنظیم اعلان‌ها", "⚙️", "sec")])
     + stat_row([("خوانده‌نشده", "۵", "▲ بلادرنگ", "c3", "🔔"), ("اعلان امروز", "۲۱", "▲ ۴ خودکار", "c1", "📣"),
                 ("یادآور موعد دارو", "۳", "▲ برای بیماران", "c2", "💊"), ("هشدار موجودی", "۲", "▲ زیر بحرانی", "c5", "⚠️")])
     + queue_rows([("🔔", "نوبت جدید دکتر رضایی", "بیمار: حسین کاظمی — ۱۰:۰۰", B.format("blue", "جدید")),
                   ("⚠️", "موجودی آموکسی‌سیلین زیر حد بحرانی", "۸۵ کپسول باقی‌مانده — سفارش خرید", B.format("red", "هشدار")),
                   ("🧪", "نتیجه آزمایش S-8841 آماده بازبینی", "بیمار: نگار موسوی — LDL بالا", B.format("amber", "در انتظار")),
                   ("💰", "فاکتور INV-3309 بدهکار ماند", "مبلغ ۴۰۰٬۰۰۰ تومان — پیامک یادآوری ارسال شد", B.format("amber", "اقدام لازم")),
                   ("🔁", "۳ تراکنش سپاس خطا خورد", "بازارسال خودکار ۱۰:۳۰", B.format("blue", "خودکار")),
                   ("✅", "بکاپ روزانه موفق", "۸۴۲ MB — ۰۳:۰۴ بامداد", B.format("green", "اطلاع"))])
     )

# ─── ۳۳ مرورگر گزارش ───
view(33, "report-browser", "مرورگر گزارش", "📑", "گزارش‌ها",
     '<div class="grid2 grow" style="grid-template-columns:240px 1fr">'
     + '<div class="panel"><div class="ph"><b>📂 دسته‌ها</b></div>'
     + queue_rows([("💰", "مالی", "۹ گزارش", B.format("gray", "")),
                   ("🏥", "بالینی", "۷ گزارش", B.format("gray", "")),
                   ("👔", "منابع انسانی", "۶ گزارش", B.format("gray", "")),
                   ("📈", "مدیریتی", "۵ گزارش", B.format("blue", "باز")),
                   ("🧪", "پاراکلینیک", "۴ گزارش", B.format("gray", ""))])
     + '</div>'
     + '<div class="panel"><div class="ph"><b>📈 آمار مراجعات ماهانه</b><small>مهر ۱۴۰۵ — پیش‌نمایش</small></div>'
     + '<div class="bars" style="height:230px;margin-bottom:22px">'
     + "".join(f'<div class="bar{" hi" if i in (2, 5) else ""}" style="height:{v}%"><span>{n}</span><i>{d}</i></div>'
               for i, (v, n, d) in enumerate(zip([62, 74, 88, 71, 80, 96, 58, 84], ["۱٬۲۴۰", "۱٬۴۸۰", "۱٬۷۶۰", "۱٬۴۲۰", "۱٬۶۰۰", "۱٬۹۲۰", "۱٬۱۶۰", "۱٬۶۸۰"], ["فرو", "ورد", "خرد", "تیر", "مرد", "شهر", "مهر", "آبا"])))
     + '</div><div class="chips"><span class="chip">بازه: ۶ ماه اخیر</span><span class="chip">تفکیک: شعبه</span>'
       '<span class="chip">خروجی: اکسل فارسی</span><span class="chip">طرح: ستونی گروهی</span></div></div></div>')

# ═══════════════════  ۱۹ پنجره فرم (مودال)  ═══════════════════

win(34, "patient-form", "ثبت بیمار جدید", "🧑‍🤝‍🧑", "پذیرش — پرونده جدید",
     form_fields([("نام *", "مریم", "👤"), ("نام خانوادگی *", "احمدی", "👤"),
                 ("کد ملی * — اعتبارسنجی رقم کنترلی", "۰۰۷۶۴۰۹۶۲۷", "🪪"),
                 ("موبایل * — الگوی 09XXXXXXXXX", "۰۹۱۴۱۲۳۴۵۶۷", "📱"),
                 ("تاریخ تولد شمسی", "۱۳۷۰/۰۳/۱۲", "📅"), ("جنسیت", "خانم", "⚧"),
                 ("گروه خونی", "A+", "🩸"), ("بیمه اصلی", "تأمین اجتماعی", "🛡"),
                 ("شماره دفترچه", "۴۴–۱۲۳–۶۷", "📕"), ("بیمه تکمیلی", "ندارد", "➕"),
                 ("شهر", "تبریز", "🏙"), ("آدرس", "خیابان آبرسان — پلاک ۱۲", "📍")])
     + '<div class="panel" style="background:#FDF6E7;border-color:#F3D9A4"><b style="font-size:12px;color:#9A6A12">🪪 کد ملی با الگوریتم رسمی رقم کنترلی اعتبارسنجی می‌شود — کد جعلی پذیرفته نمی‌شود و تکراری‌ها رد می‌شوند.</b></div>'
     + actions_row())

win(35, "patient-detail", "پرونده بیمار — مریم احمدی", "🗂", "MR-1042 — ۳۴ ساله — تأمین اجتماعی",
     '<div class="chips"><span class="chip">🪪 ۰۰۷۶۴۰۹۶۲۷</span><span class="chip">📱 ۰۹۱۴۱۲۳۴۵۶۷</span>'
     '<span class="chip">🩸 A+</span><span class="chip">🛡 تأمین اجتماعی</span><span class="chip">⚠️ حساسیت: پنی‌سیلین</span></div>'
     + table_panel("سابقه مراجعات", ["تاریخ", "پزشک", "علت", "تشخیص ICD-10", "نسخه", "صورتحساب"],
        [("۱۴۰۵/۰۷/۱۳", "دکتر کریمی", "درد شکم", "K29.7 گاستریت", "#P-1204", "#INV-3312"),
         ("۱۴۰۵/۰۴/۰۲", "دکتر کریمی", "کنترل", "Z00.0 معاینه دوره‌ای", "#P-098۷", "#INV-2910"),
         ("۱۴۰۵/۰۱/۱۵", "دکتر احمدی", "تپش قلب", "R00.2 تپش", "#P-0751", "#INV-2411")])
     + actions_row())

win(36, "appointment-form", "ثبت نوبت جدید", "🗓", "نوبت‌دهی — تقویم هوشمند",
     form_fields([("بیمار", "حسین کاظمی — MR-1038", "🧑‍🤝‍🧑"), ("پزشک", "دکتر رضایی — اطفال", "🩺"),
                 ("تخصص خدمت", "ویزیت اطفال", "📋"), ("تاریخ شمسی", "۱۴۰۵/۰۷/۱۴", "📅"),
                 ("بازه ساعت", "۰۸:۳۰ – ۰۹:۰۰", "⏰"), ("کانال ثبت", "حضوری", "🖥")])
     + '<div class="panel"><div class="ph"><b>🗓 ظرفیت دکتر رضایی — ۱۴۰۵/۰۷/۱۴</b><small>۱۴ نوبت آزاد از ۲۰</small></div>'
     + '<div class="chips">' + "".join(f'<span class="chip" style="{"opacity:.35" if i in (3, 6) else ""}">{"✅ " if i not in (3, 6) else "✕ "}۰۸:{str(i * 30).zfill(2) if i < 2 else f"0{i - 1}:{(i % 2) * 30:02d}"}</span>' for i in range(8))
     + '<span class="chip">…</span></div></div>'
     + actions_row())

win(37, "reception-form", "پذیرش حضوری", "🛎", "صف زنده — شماره ۴۶",
     form_fields([("بیمار", "سارا محمدی — MR-1036", "🧑‍🤝‍🧑"), ("شماره صف", "۴۶", "🔢"),
                 ("پزشک مقصد", "دکتر رضایی — اطفال", "🩺"), ("نوع خدمت", "ویزیت", "📋"),
                 ("بیعانه", "۴۰۰٬۰۰۰ تومان", "💰"), ("روش پرداخت", "کارتخوان", "💳")])
     + '<div class="panel" style="background:#E8F6F0;border-color:#BCE6D6"><b style="font-size:12px;color:#0B7A55">📢 با ثبت پذیرش، شماره ۴۶ بلافاصله روی تلویزیون سالن و همه رایانه‌ها ظاهر می‌شود (SignalR).</b></div>'
     + actions_row())

win(38, "visit-window", "ثبت ویزیت و معاینه", "📋", "ویزیت — مریم احمدی (MR-1042)",
     form_fields([("علت مراجعه", "درد شکم از دیروز", "📝"), ("شرح حال", "درد سوزشی اپی‌گاستر…", "📖"),
                 ("فشار خون", "۱۲۰/۸۰ mmHg", "📈"), ("نبض", "۷۸ bpm", "❤️"),
                 ("دما", "۳۶.۸ °C", "🌡"), ("وزن", "۶۴ kg", "⚖️"),
                 ("تشخیص ICD-10", "K29.7 — گاستریت", "🔎"), ("بیماری‌های زمینه‌ای", "فشار خون — کنترل‌شده", "🏥")])
     + '<div class="panel" style="background:#FCE8E6;border-color:#F2C4C0"><b style="font-size:12px;color:#B03A2E">🚨 هشدار حساسیت: بیمار به پنی‌سیلین حساسیت دارد — در نسخه‌نویسی پایش می‌شود.</b></div>'
     + actions_row())

win(39, "prescription-form", "نسخه‌نویسی", "💊", "دکتر کریمی — مریم احمدی",
     form_fields([("مدت درمان", "۷ روز", "📅"), ("توضیح عمومی", "با غذا مصرف شود", "📝")])
     + table_panel("اقلام نسخه", ["#", "دارو", "دوز", "بارداری/شیردهی", "تداخل", "قیمت"],
        [("۱", "امپرازول ۲۰mg", "۱ قرص صبح ناشتا", B.format("green", "امن"), B.format("green", "ندارد"), "۴۲٬۰۰۰"),
         ("۲", "سیمتیکون ۸۰mg", "۱ قرص بعد غذا", B.format("green", "امن"), B.format("green", "ندارد"), "۳۸٬۰۰۰"),
         ("۳", "سوکرالفات ژل", "۱۰ml قبل خواب", B.format("amber", "احتیاط"), B.format("green", "ندارد"), "۵۶٬۰۰۰")])
     + '<div class="panel" style="background:#E8F6F0;border-color:#BCE6D6"><b style="font-size:12px;color:#0B7A55">✅ تداخل‌سنجی خودکار انجام شد — ۳ قلم بدون تداخل و مطابق حساسیت‌های بیمار.</b></div>'
     + actions_row())

win(40, "prescription-detail", "مشاهده نسخه #P-1204", "💊", "صادره ۱۴۰۵/۰۷/۱۳ — دکتر کریمی",
     '<div class="chips"><span class="chip">🧑‍🤝‍🧑 مریم احمدی</span><span class="chip">📅 ۱۴۰۵/۰۷/۱۳</span>'
     '<span class="chip">🧾 ۳ قلم دارو</span><span class="chip">💰 جمع: ۱۳۶٬۰۰۰ تومان</span></div>'
     + table_panel("اقلام", ["دارو", "دوز و زمان", "مدت", "توضیح"],
        [("امپرازول ۲۰mg", "۱ قرص صبح ناشتا", "۷ روز", "قبل از صبحانه"),
         ("سیمتیکون ۸۰mg", "۱ قرص بعد از غذا", "۷ روز", "در صورت نفخ"),
         ("سوکرالفات ژل", "۱۰ml قبل از خواب", "۷ روز", "—")])
     + '<div class="chips"><span class="chip">☁️ ارسال به نسخه الکترونیک: موفق</span><span class="chip">🖨 چاپ: انجام شد</span></div>'
     + actions_row())

win(41, "doctor-form", "ثبت پزشک جدید", "🩺", "پزشکان — پرونده پزشکی",
     form_fields([("نام", "دکتر سارا", "👤"), ("نام خانوادگی", "موسوی", "👤"),
                 ("تخصص", "پوست و مو", "🎓"), ("شماره نظام پزشکی", "۶۷۸۹۰", "🔢"),
                 ("موبایل", "۰۹۱۴۷۶۵۴۳۲۱", "📱"), ("شیفت", "عصر ۱۶–۲۰", "🕐"),
                 ("حق‌الویزیت", "۵۵۰٬۰۰۰ تومان", "💰"), ("اتاق معاینه", "اتاق ۵", "🚪")])
     + actions_row())

win(42, "employee-form", "ثبت پرسنل جدید", "👔", "منابع انسانی — پرونده پرسنلی",
     form_fields([("نام", "زهرا", "👤"), ("نام خانوادگی", "رحیمی", "👤"),
                 ("کد ملی", "۰۰۴۵۶۷۸۹۱", "🪪"), ("سمت", "کارمند مالی", "💼"),
                 ("شعبه", "شعبه ۲ — آذرشهر", "🏢"), ("تاریخ استخدام", "۱۴۰۲/۰۶/۰۱", "📅"),
                 ("نوع قرارداد", "آزمایشی ۳ ماهه", "📜"), ("حقوق پایه", "۱۸٬۵۰۰٬۰۰۰ تومان", "💰"),
                 ("درصد حقوق از سود خدمات", "۱۰٪ — فقط از سود خدمات انجامی خود پرسنل", "📈"), ("شیفت", "اداری ۸–۱۶", "🕐")])
     + actions_row())

win(43, "invoice-form", "صدور فاکتور", "💵", "صندوق ۱ — مریم احمدی",
     form_fields([("بیمار", "مریم احمدی — MR-1042", "🧑‍🤝‍🧑"), ("بیمه", "تأمین اجتماعی — ۶۰٪", "🛡")])
     + table_panel("اقلام فاکتور", ["خدمت", "تعرفه", "تعداد", "مبلغ", "سهم بیمه", "بیمار"],
        [("ویزیت داخلی", "۳۵۰٬۰۰۰", "۱", "۳۵۰٬۰۰۰", "۲۱۰٬۰۰۰", "۱۴۰٬۰۰۰"),
         ("آزمایش CBC", "۲۴۰٬۰۰۰", "۱", "۲۴۰٬۰۰۰", "۱۴۴٬۰۰۰", "۹۶٬۰۰۰"),
         ("آزمایش FBS", "۳۰۰٬۰۰۰", "۱", "۳۰۰٬۰۰۰", "۱۸۰٬۰۰۰", "۱۲۰٬۰۰۰")])
     + stat_row([("جمع کل", "۸۹۰٬۰۰۰", "▲ ۳ قلم", "c1", "💰"), ("سهم بیمه", "۵۳۴٬۰۰۰", "▲ ارسال به سپاس", "c4", "🏛"),
                 ("پرداختی بیمار", "۳۵۶٬۰۰۰", "▲ کارتخوان", "c2", "💳")])
     + actions_row())

win(44, "payment-window", "دریافت وجه", "💰", "صندوق — تسویه بدهی",
     form_fields([("بیمار", "حسین کاظمی — MR-1038", "🧑‍🤝‍🧑"), ("بدهی قبلی", "۴۰۰٬۰۰۰ تومان", "📉"),
                 ("مبلغ دریافتی", "۴۰۰٬۰۰۰ تومان", "💰"), ("روش پرداخت", "کارتخوان — بانک ملت", "💳"),
                 ("شماره پیگیری", "۸۳۴۵۲۱", "🔢"), ("تاریخ شمسی", "۱۴۰۵/۰۷/۱۳", "📅")])
     + '<div class="panel" style="background:#E8F6F0;border-color:#BCE6D6"><b style="font-size:12px;color:#0B7A55">⚖️ با ثبت این پرداخت، سند حسابداری دوبل به‌طور خودکار صادر و بدهی بیمار صفر می‌شود.</b></div>'
     + actions_row())

win(45, "payslip-window", "فیش حقوقی مهر ۱۴۰۵", "🧾", "رضا محمدنژاد — پرستار — ۱۷۶ ساعت کارکرد",
     '<div class="grid2"><div class="panel"><div class="ph"><b>➕ مزایا</b></div>'
     + table_panel("", ["عنوان", "مبلغ"], [("حقوق پایه", "۲۲٬۰۰۰٬۰۰۰"), ("حقوق درصدی از سود خدمات ۳۰٪", "۱۴٬۴۶۰٬۰۰۰"), ("اضافه‌کاری ۱۴ ساعت × ۱.۴", "۲٬۴۵۰٬۰۰۰"), ("بن کارگری", "۹٬۰۰۰٬۰۰۰")], "").replace('class="panel grow"', 'class=""')
     + '</div><div class="panel"><div class="ph"><b>➖ کسورات</b></div>'
     + table_panel("", ["عنوان", "مبلغ"], [("بیمه سهم کارگر ۷٪", "۳٬۳۸۳٬۰۰۰"), ("مالیات پلکانی", "۱٬۷۶۰٬۰۰۰")], "").replace('class="panel grow"', 'class=""')
     + '</div></div>'
     + stat_row([("مبنای سود خدمات", "۴۸٬۲۰۰٬۰۰۰", "▲ سود = مبلغ − هزینه", "c3", "📈"), ("جمع کسورات", "۵٬۱۴۳٬۰۰۰", "▼", "c5", "➖"),
                 ("خالص پرداختی", "۳۹٬۵۲۴٬۰۰۰", "▲ واریز به کارت", "c1", "💰")])
     + actions_row())

win(46, "lab-order-form", "سفارش آزمایش", "🧪", "دکتر کریمی — مریم احمدی",
     form_fields([("نوع نمونه", "خون وریدی", "🩸"), ("فوریت", "عادی — FPR ۲ ساعت", "⏱")])
     + '<div class="panel"><div class="ph"><b>🧫 انتخاب پانل‌ها</b><small>۴۲ پانل تعریفی</small></div>'
     + '<div class="chips">' + "".join(f'<span class="chip" style="{"background:#DCEBFB;border-color:#9CC3EC" if s else ""}">{"✅ " if s else ""}{t}</span>'
          for (t, s) in [("CBC — کامل خون", True), ("FBS — قند ناشتا", True), ("Lipid Profile", False),
                         ("آزمایش کبد", False), ("آزمایش تیروئید", False), ("CRP", False), ("ویتامین D", False)])
     + '</div></div>'
     + actions_row())

win(47, "lab-result-window", "ثبت نتیجه آزمایش", "🔬", "نمونه S-8841 — آزمایش قلب",
     table_panel("نتایج — بازه نرمال خودکار", ["شاخص", "نتیجه", "بازه نرمال", "واحد", "وضعیت"],
        [("کلسترول کل", "۲۱۸", "تا ۲۰۰", "mg/dL", B.format("red", "بالا ↑")),
         ("LDL", "۱۴۲", "تا ۱۳۰", "mg/dL", B.format("red", "بالا ↑")),
         ("HDL", "۵۲", "بیش از ۴۰", "mg/dL", B.format("green", "نرمال")),
         ("تری‌گلیسیرید", "۱۶۸", "تا ۱۵۰", "mg/dL", B.format("amber", "مرزی"))])
     + '<div class="panel" style="background:#FDF1DC;border-color:#F3D9A4"><b style="font-size:12px;color:#9A6A12">✉️ پس از تأیید آزمایش‌شناس، پیامک «نتیجه آماده است» با کد پیگیری برای بیمار ارسال می‌شود.</b></div>'
     + actions_row())

win(48, "stockin-form", "رسید ورود انبار", "📦", "داروخانه — تحویل از پخش الوند",
     form_fields([("تأمین‌کننده", "پخش الوند دارو", "🚚"), ("شماره فاکتور خرید", "۱۴۰۵-۸۸۲", "🔢"),
                 ("تاریخ شمسی", "۱۴۰۵/۰۷/۱۳", "📅"), ("انبار مقصد", "داروخانه مرکزی", "🏬")])
     + table_panel("اقلام رسیده", ["کد", "دارو", "تعداد", "قیمت واحد", "انقضا", "باتچ‌نامبر"],
        [("D-1002", "آموکسی‌سیلین ۵۰۰", "۲٬۰۰۰", "۱۲٬۵۰۰", "۱۴۰۶/۰۶", "AMX-88231"),
         ("D-1003", "متفرمین ۵۰۰", "۵٬۰۰۰", "۸٬۲۰۰", "۱۴۰۷/۰۲", "MET-71204"),
         ("D-1005", "سرم رینگر", "۳۰۰", "۹۸٬۰۰۰", "۱۴۰۶/۰۹", "RL-33412")])
     + actions_row())

win(49, "user-form", "ثبت کاربر جدید", "👤", "کاربران — کنترل دسترسی",
     form_fields([("نام کاربری", "n.sadeghi", "👤"), ("نام کامل", "نگار صادقی", "📛"),
                 ("نقش", "پذیرش", "🔑"), ("شعبه", "مرکزی — تبریز", "🏢"),
                 ("رمز عبور", "••••••••••", "🔒"), ("تکرار رمز", "••••••••••", "🔒")])
     + '<div class="panel" style="background:#E8F0FB;border-color:#BFD5EE"><b style="font-size:12px;color:#1F5E9E">🔐 سیاست رمز: حداقل ۸ کاراکتر، حرف بزرگ + عدد + نماد؛ BCrypt با ضریب ۱۲ ذخیره می‌شود؛ قفل خودکار پس از ۵ تلاش ناموفق.</b></div>'
     + actions_row())

win(50, "dicom-viewer", "نمایشگر DICOM", "🩻", "رادیوگرافی زانو — رضا حسینی",
     '<div class="grid2 grow" style="grid-template-columns:1.5fr 1fr">'
     + '<div class="panel" style="background:#0B1526;border-color:#22334D;display:flex;flex-direction:column">'
     + '<div class="ph" style="color:#DCE9FA"><b>🩻 رادیوگرافی زانو — نمای جانبی</b><small style="color:#8296B3">پنجره: 400/40 · سطح: 12</small></div>'
     + '<div style="flex:1;border-radius:10px;background:radial-gradient(ellipse at 50% 45%,#5E7590 0%,#2C3E52 40%,#101C2C 75%);position:relative;overflow:hidden">'
     + '<div style="position:absolute;inset:0;background:linear-gradient(105deg,transparent 42%,rgba(255,255,255,.16) 50%,transparent 58%)"></div>'
     + '<div style="position:absolute;top:18%;left:28%;width:44%;height:52%;border-radius:50% 50% 42% 42%;background:linear-gradient(160deg,#8FA6BF,#46617E);opacity:.8;filter:blur(2px)"></div>'
     + '<div style="position:absolute;bottom:8%;right:30%;width:12%;height:26%;border-radius:46%;background:linear-gradient(180deg,#9FB4CC,#5E7590);opacity:.7;filter:blur(1px)"></div>'
     + '<div style="position:absolute;top:8px;left:10px;color:#9FB4CC;font-size:10.5px;direction:ltr">PA: ۱۴۰۵۰۷۱۳ · MR-1041</div>'
     + '</div></div>'
     + '<div class="panel"><div class="ph"><b>⚙️ ابزار و متادیتا</b></div>'
     + form_fields([("پنجره (Window)", "W: 400 / L: 40", "🪟"), ("بزرگنمایی", "۱۲۰٪", "🔍")])
     + '<div style="margin-top:11px">' + table_panel("متادیتا", ["فیلد", "مقدار"],
        [("نام بیمار", "رضا حسینی"), ("تاریخ شمسی", "۱۴۰۵/۰۷/۱۳"), ("نمای تصویر", "جانبی"),
         ("ضخامت برش", "—"), ("دستگاه", "SVG دیجیتال")], "").replace('class="panel grow"', 'class=""') + '</div></div></div>')

win(51, "doctor-performance", "عملکرد پزشک — دکتر رضایی", "📈", "مهر ۱۴۰۵ — تحلیل جامع",
     stat_row([("ویزیت ماه", "۳۴۸", "▲ ۱۴٪", "c1", "📋"), ("میانگین رضایت", "۴.۸/۵", "▲ نظرسنجی", "c2", "⭐"),
               ("درآمد ماه", "۸۴.۶م ت", "▲ ۱۱٪", "c3", "💰"), ("میانگین زمان ویزیت", "۱۱ دقیقه", "▼ بهتر", "c6", "⏱")])
     + chart_panel("روند ویزیت هفتگی دکتر رضایی", [52, 61, 58, 71, 64, 88, 34], ["ش", "ی", "د", "س", "چ", "پ", "ج"], hi_index=[5])
     + '<div class="chips"><span class="chip">🏆 رتبه ۱ رضایت بیمار</span><span class="chip">🔁 نرخ بازگشت: ۳۴٪</span>'
       '<span class="chip">🧾 نسخه الکترونیک: ۹۱٪</span><span class="chip">⭐ شکایت: ۰</span></div>')

win(52, "login", "ورود به سیستم", "🔐", "درمانگاه سهند — نسخه ۱.۷.۰",
     '<div style="display:flex;flex-direction:column;align-items:center;gap:14px;padding:22px 0">'
     + '<div style="width:84px;height:84px;border-radius:22px;background:radial-gradient(circle at 40% 25%,#D8F1FF,#BFE4FB 70%);display:flex;align-items:center;justify-content:center;font-size:42px;box-shadow:0 10px 26px rgba(22,53,94,.25)">🏥</div>'
     + '<div style="text-align:center"><div style="font-size:20px;font-weight:800;color:#16355E">درمانگاه سهند</div>'
     + '<div style="font-size:11px;color:#8296B3;letter-spacing:2px;direction:ltr">SAHAND CLINICPRO v1.6</div></div>'
     + '<div style="width:88%">' + form_fields([("نام کاربری", "a.karimi", "👤"), ("رمز عبور", "••••••••••", "🔒")])[0] + '</div>'
     + '<div style="width:88%">' + form_fields([("", "••••••••••", "🔒")])[0] + '</div>'
     + '<div style="width:88%;display:flex;justify-content:space-between;align-items:center;font-size:11px;color:#5D6F88">'
     + '<span style="display:flex;align-items:center;gap:6px"><span style="width:15px;height:15px;border-radius:4px;border:1.5px solid #2E86C1;display:inline-block"></span> مرا به خاطر بسپار</span>'
     + '<span style="color:#2E86C1;font-weight:700">رمز را فراموش کردم؟</span></div>'
     + '<button class="btn pri" style="width:88%;justify-content:center;font-size:14px;padding:12px">🔐 ورود</button>'
     + '<div style="font-size:10px;color:#A5B4C8">🧑‍💻 پشتیبانی: سهند سرویس — ۰۴۱-۳۴۴۳۶۱۴۱</div></div>')

# ═══════════════════════════  رندر  ═══════════════════════════

def main():
    only = set(int(x) for x in sys.argv[1:]) if len(sys.argv) > 1 else None
    with sync_playwright() as p:
        browser = p.chromium.launch(args=["--force-color-profile=srgb"])
        page = browser.new_page(viewport={"width": W, "height": H}, device_scale_factor=1)
        made = 0
        for spec in SPECS:
            if only and spec["num"] not in only:
                continue
            if spec["kind"] == "view":
                html = shell(spec["title"], "درمانگاه سهند — " + spec["title"], spec["nav"], spec["body"], spec["icon"])
                name = f"{spec['num']:02d}-{spec['key']}.png"
            else:
                html = window_shell(spec["title"], spec.get("sub", ""), spec["icon"], spec["body"])
                name = f"{spec['num']:02d}-{spec['key']}-win.png"
            src = TMP_DIR / f"{spec['num']:02d}.html"
            src.write_text(html, encoding="utf-8")
            # ═══ ریشه‌یابی باگ فونت (چرخه نهم) ═══
            # مسیر قدیمی ../../assets/fonts غلط بود و فونت هرگز بارگذاری نمی‌شد.
            # حالا: (۱) goto اول — بارگذاری فونت‌ها؛ (۲) reload = ظاهر «بار دوم»
            # مثل برنامه واقعی که پس از کش شدن فونت یکدست می‌شود؛ (۳) انتظار
            # صریح document.fonts.ready؛ (۴) راستی‌آزمایی Vazirmatn واقعاً فعال است.
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
    print(f"\n📸 {made} اسکرین‌شات در {OUT_DIR} تولید شد")


if __name__ == "__main__":
    sys.exit(main())

