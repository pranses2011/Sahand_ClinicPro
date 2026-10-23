# -*- coding: utf-8 -*-
"""
╔══════════════════════════════════════════════════════════════════════════╗
║  مولدِ مجموعه‌ی بازطراحیِ رابط کاربر — Sahand ClinicPro                    ║
╚══════════════════════════════════════════════════════════════════════════╝

برای هر فرمِ ثبت‌شده در forms_spec.py چهار خروجی تولید می‌کند:

    01-wireframes-html/{Name}.html     وایرفریم گرافیکی (CSS حرفه‌ای)
    02-wireframes-md/{Name}.md         وایرفریم متنی + مستند المان‌ها
    03-views/{Name}.xaml               View با تم یکپارچه و اتصال به زبان
    04-viewmodels/{Name}ViewModel.cs   ViewModel با دستورها و اعتبارسنجی

هم‌چنین دو فایل زبان را در 00-foundation/Localization می‌سازد:
    Strings.resx      (فارسی — پیش‌فرض)
    Strings.en.resx   (انگلیسی)

اجرا:
    python3 redesign/tools/generate.py
"""

from __future__ import annotations

import html
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from forms_spec import FORMS  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
REDESIGN = ROOT / "redesign"

DIR_HTML = REDESIGN / "01-wireframes-html"
DIR_MD = REDESIGN / "02-wireframes-md"
DIR_VIEW = REDESIGN / "03-views"
DIR_VM = REDESIGN / "04-viewmodels"
DIR_RES = REDESIGN / "00-foundation" / "Localization"

NS_BASE = "Sahand.ClinicPro.Desktop.Base"
NS_CTRL = "Sahand.ClinicPro.Desktop.Controls"
NS_LOC = "Sahand.ClinicPro.Desktop.Localization"
NS_VM = "Sahand.ClinicPro.Desktop.ViewModels.Redesign"


# ══════════════════════════════════════════════════════════════════════
#  ابزارهای کمکی
# ══════════════════════════════════════════════════════════════════════

def key_of(name: str) -> str:
    """نام فرم بدون پسوند View/Window (مانند PatientsView → Patients)."""
    for suffix in ("View", "Window", "Dialog"):
        if name.endswith(suffix) and len(name) > len(suffix):
            return name[: -len(suffix)]
    return name


def is_window(kind: str) -> bool:
    return kind in ("form", "dialog", "login", "splash", "viewer", "shell")


def parse_field(spec: str):
    """تجزیه‌ی «prop:type:label[:*]» به (prop, type, label, required)."""
    parts = spec.split(":")
    prop = parts[0]
    ftype = parts[1] if len(parts) > 1 else "text"
    label = parts[2] if len(parts) > 2 else prop
    required = len(parts) > 3 and parts[3] == "*"
    return prop, ftype, label, required


def cs_type(prop: str, ftype: str) -> str:
    """تعیین نوع C# برای یک فیلد."""
    if ftype in ("number", "money"):
        return "decimal"
    if ftype == "check":
        return "bool"
    if ftype == "date":
        return "DateTime?"
    if ftype == "time":
        return "TimeSpan?"
    if ftype == "lookup":
        return "Guid?"
    if ftype == "combo":
        return "Guid?" if prop.endswith("Id") else "string"
    return "string"


def cs_default(prop: str, ftype: str) -> str:
    if ftype in ("number", "money"):
        return "0m"
    if ftype == "check":
        return "false"
    if ftype in ("date", "time"):
        return "null"
    if ftype in ("lookup", "combo"):
        return "null" if cs_type(prop, ftype).endswith("?") else "string.Empty"
    return "string.Empty"


def xaml_control(prop: str, ftype: str) -> str:
    """تولید عنصر XAML متناظر با نوع فیلد."""
    two_way = "{Binding %s, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" % prop
    if ftype == "textarea":
        return ('<TextBox Style="{DynamicResource Input.TextBox}" Text="%s" '
                'AcceptsReturn="True" TextWrapping="Wrap" MinHeight="88" '
                'VerticalContentAlignment="Top" />' % two_way)
    if ftype == "combo":
        return ('<ComboBox Style="{DynamicResource Input.ComboBox}" '
                'SelectedValue="{Binding %s, Mode=TwoWay}" />' % prop)
    if ftype == "lookup":
        return ('<ctrl:SearchBox Text="{Binding %sText, Mode=TwoWay}" '
                'Placeholder="%s" />' % (prop, "جستجو و انتخاب…"))
    if ftype == "check":
        return ('<CheckBox Style="{DynamicResource Input.CheckBox}" '
                'IsChecked="{Binding %s, Mode=TwoWay}" />' % prop)
    if ftype == "date":
        return ('<DatePicker Style="{DynamicResource Input.Date}" '
                'SelectedDate="{Binding %s, Mode=TwoWay}" />' % prop)
    if ftype == "password":
        return ('<PasswordBox Style="{DynamicResource Input.Password}" '
                'ctrl:PasswordHelper.Attach="True" '
                'ctrl:PasswordHelper.Password="{Binding %s, Mode=TwoWay}" />' % prop)
    if ftype in ("number", "money"):
        return ('<TextBox Style="{DynamicResource Input.TextBox}" Text="%s" '
                'FlowDirection="LeftToRight" HorizontalContentAlignment="Right" />' % two_way)
    return ('<TextBox Style="{DynamicResource Input.TextBox}" Text="%s" />' % two_way)


def esc(text: str) -> str:
    return html.escape(str(text), quote=True)


# ══════════════════════════════════════════════════════════════════════
#  گردآوری کلیدهای زبان
# ══════════════════════════════════════════════════════════════════════

COMMON_STRINGS = {
    "Common.Save": ("ذخیره", "Save"),
    "Common.Cancel": ("انصراف", "Cancel"),
    "Common.Close": ("بستن", "Close"),
    "Common.Edit": ("ویرایش", "Edit"),
    "Common.Delete": ("حذف", "Delete"),
    "Common.New": ("جدید", "New"),
    "Common.Refresh": ("تازه‌سازی", "Refresh"),
    "Common.Search": ("جستجو", "Search"),
    "Common.Print": ("چاپ", "Print"),
    "Common.Export": ("خروجی", "Export"),
    "Common.Loading": ("در حال بارگذاری…", "Loading…"),
    "Common.NoData": ("موردی یافت نشد", "No records found"),
    "Common.Total": ("جمع کل", "Total"),
    "Common.Status": ("وضعیت", "Status"),
    "Common.Date": ("تاریخ", "Date"),
    "Common.Description": ("توضیح", "Description"),
    "Common.Amount": ("مبلغ", "Amount"),
    "Common.ConfirmDelete": ("آیا از حذف این مورد اطمینان دارید؟", "Are you sure you want to delete this item?"),
    "Common.ValidationError": ("لطفاً خطاهای فرم را اصلاح کنید", "Please correct the form errors"),
    "Common.SaveSuccess": ("اطلاعات با موفقیت ذخیره شد", "Saved successfully"),
    "Common.Required": ("این فیلد الزامی است", "This field is required"),
    "Common.Previous": ("‹ قبلی", "‹ Previous"),
    "Common.Next": ("بعدی ›", "Next ›"),
    "Common.ToggleTheme": ("تغییر تم", "Toggle theme"),
    "Common.ToggleLanguage": ("تغییر زبان", "Toggle language"),
    "Common.AppName": ("سهند", "Sahand"),
    "Common.AppTagline": ("نرم‌افزار جامع مدیریت درمانگاه", "Comprehensive clinic management"),
}


def collect_strings():
    """گردآوری همه‌ی رشته‌های مورد نیاز از مشخصات فرم‌ها."""
    strings = dict(COMMON_STRINGS)

    for form in FORMS:
        k = key_of(form["name"])
        strings[f"{k}.Title"] = (form["title"], form["title"])
        if form["subtitle"]:
            strings[f"{k}.Subtitle"] = (form["subtitle"], form["subtitle"])

        for i, (icon, label, _value) in enumerate(form["kpis"], start=1):
            strings[f"{k}.Kpi{i}"] = (label, label)

        for i, (sec_title, _sec_icon, _fields) in enumerate(form["sections"], start=1):
            strings[f"{k}.Sec{i}"] = (sec_title, sec_title)

        for _sec_title, _sec_icon, fields in form["sections"]:
            for spec in fields:
                prop, _t, label, _r = parse_field(spec)
                strings[f"{k}.Fld_{prop}"] = (label, label)

        for name, label, _c in form["filters"]:
            strings[f"{k}.Filter_{name}"] = (label, label)

        for binding, header, _w in form["cols"]:
            strings[f"{k}.Col_{binding}"] = (header, header)

        for command, label, _s in form["actions"]:
            strings[f"{k}.Act_{command}"] = (label, label)

        for i, (tab_title, _tab_icon) in enumerate(form["tabs"], start=1):
            strings[f"{k}.Tab{i}"] = (tab_title, tab_title)

        if form["notes"]:
            strings[f"{k}.Notes"] = (form["notes"], form["notes"])

    return strings


# ══════════════════════════════════════════════════════════════════════
#  ۱) وایرفریم HTML
# ══════════════════════════════════════════════════════════════════════

CSS = """
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:"Vazirmatn","Segoe UI",Tahoma,sans-serif;background:#F1F5F9;
     color:#1E293B;direction:rtl;padding:32px;line-height:1.7}
.wrap{max-width:1280px;margin:0 auto}
.doc-head{background:#0D9488;color:#fff;padding:28px 32px;border-radius:16px 16px 0 0}
.doc-head h1{font-size:26px;font-weight:700;margin-bottom:6px}
.doc-head p{opacity:.9;font-size:14px}
.meta{display:flex;gap:10px;flex-wrap:wrap;margin-top:14px}
.tag{background:rgba(255,255,255,.18);padding:4px 12px;border-radius:999px;font-size:12px}
.body{background:#fff;padding:32px;border-radius:0 0 16px 16px;
      box-shadow:0 8px 28px rgba(15,23,42,.10)}
.frame{border:2px dashed #94A3B8;border-radius:14px;background:#F8FAFC;
       padding:20px;margin-bottom:26px}
.cap{font-size:12px;color:#64748B;margin-bottom:10px;font-weight:600;
     letter-spacing:.3px;text-transform:uppercase}
.app{background:#EFF3F8;border:1px solid #CBD5E1;border-radius:12px;
     overflow:hidden;background:#fff}
.topbar{background:#0F766E;color:#fff;padding:10px 16px;display:flex;
        align-items:center;gap:14px;font-size:13px}
.topbar .brand{font-weight:700}
.topbar .sp{flex:1}
.topbar .pill{background:rgba(255,255,255,.16);padding:3px 10px;border-radius:999px}
.shell{display:flex;min-height:430px}
.side{width:190px;background:#0F766E;color:#CFE9E5;padding:16px 12px;font-size:13px}
.side .item{padding:8px 12px;border-radius:8px;margin-bottom:4px;display:flex;gap:8px}
.side .item.on{background:rgba(255,255,255,.16);color:#fff;font-weight:600}
.main{flex:1;padding:18px;background:#F8FAFC;overflow:hidden}
.hdr{display:flex;align-items:center;gap:12px;margin-bottom:14px}
.hdr .ico{width:44px;height:44px;border-radius:12px;background:#CCFBF1;
          display:flex;align-items:center;justify-content:center;font-size:22px}
.hdr h2{font-size:19px}
.hdr .sub{font-size:12px;color:#64748B}
.kpis{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));
      gap:12px;margin-bottom:14px}
.kpi{background:#fff;border:1px solid #E2E8F0;border-radius:12px;padding:12px 14px}
.kpi .lb{font-size:11px;color:#64748B}
.kpi .vl{font-size:22px;font-weight:700;color:#0F766E}
.kpi .tr{font-size:11px;color:#15803D}
.bar{display:flex;gap:8px;flex-wrap:wrap;margin-bottom:12px;align-items:center}
.fld{background:#fff;border:1px solid #CBD5E1;border-radius:8px;padding:7px 12px;
     font-size:12px;min-width:130px;color:#64748B}
.fld.wide{flex:1;min-width:200px}
.btn{padding:8px 16px;border-radius:8px;font-size:12px;font-weight:600}
.btn.p{background:#0D9488;color:#fff}
.btn.s{background:#fff;color:#334155;border:1px solid #CBD5E1}
.btn.d{background:#BE123C;color:#fff}
.btn.g{background:#15803D;color:#fff}
.tbl{width:100%;border-collapse:collapse;font-size:12px;background:#fff;
     border-radius:10px;overflow:hidden}
.tbl th{background:#F1F5F9;text-align:right;padding:9px 12px;color:#64748B;
        font-weight:600;border-bottom:1px solid #E2E8F0}
.tbl td{padding:9px 12px;border-bottom:1px solid #E2E8F0}
.tbl tr:nth-child(even) td{background:#F8FAFC}
.badge{display:inline-block;padding:2px 10px;border-radius:999px;font-size:11px;font-weight:600}
.b.ok{background:#DCFCE7;color:#15803D}
.b.warn{background:#FEF3C7;color:#B45309}
.b.err{background:#FFE4E6;color:#BE123C}
.b.info{background:#E0F2FE;color:#0369A1}
.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:12px}
.card{background:#fff;border:1px solid #E2E8F0;border-radius:12px;padding:14px}
.card h4{font-size:13px;margin-bottom:10px;color:#0F766E;
         border-bottom:1px solid #E2E8F0;padding-bottom:6px}
.row{display:flex;flex-direction:column;gap:3px;margin-bottom:9px}
.row label{font-size:11px;color:#64748B}
.row .in{border:1px solid #CBD5E1;border-radius:8px;padding:7px 11px;
         background:#fff;font-size:12px;color:#1E293B}
.row .in.ta{min-height:56px}
.tabs{display:flex;gap:4px;border-bottom:2px solid #E2E8F0;margin-bottom:12px;flex-wrap:wrap}
.tabs .t{padding:8px 15px;font-size:12px;color:#64748B;border-radius:8px 8px 0 0}
.tabs .t.on{color:#0D9488;font-weight:700;border-bottom:2px solid #0D9488;
            margin-bottom:-2px;background:#CCFBF1}
.foot{display:flex;justify-content:space-between;align-items:center;
      margin-top:12px;padding-top:12px;border-top:1px solid #E2E8F0;font-size:12px}
.acts{display:flex;gap:8px}
.empty{text-align:center;padding:34px;color:#94A3B8;font-size:13px}
.empty .ic{font-size:44px;display:block;margin-bottom:8px;opacity:.6}
.pager{display:flex;gap:6px;align-items:center;font-size:12px;color:#64748B}
.pager .pg{border:1px solid #CBD5E1;border-radius:6px;padding:3px 9px;background:#fff}
.pager .pg.on{background:#0D9488;color:#fff;border-color:#0D9488}
.legend{margin-top:26px;background:#F8FAFC;border:1px solid #E2E8F0;
        border-radius:12px;padding:18px 22px}
.legend h3{font-size:15px;margin-bottom:10px;color:#0F766E}
.legend table{width:100%;border-collapse:collapse;font-size:12.5px}
.legend th{text-align:right;padding:6px 8px;border-bottom:1px solid #E2E8F0;color:#64748B}
.legend td{padding:6px 8px;border-bottom:1px solid #F1F5F9}
.notes{background:#FFFBEB;border:1px solid #FDE68A;border-radius:10px;
       padding:12px 16px;font-size:13px;color:#78350F;margin-top:14px}
.foot-doc{text-align:center;color:#94A3B8;font-size:12px;margin-top:22px}
@media print{body{background:#fff;padding:0}.frame{border-style:solid}}
"""


def nav_items(current: str):
    groups = {}
    for f in FORMS:
        if f["kind"] in ("login", "splash", "shell", "dialog"):
            continue
        groups.setdefault(f["group"], []).append(f)
    items = []
    for g, forms in groups.items():
        for f in forms:
            on = " on" if f["name"] == current else ""
            items.append(f'<div class="item{on}"><span>{esc(f["icon"])}</span>'
                         f'<span>{esc(f["title"])}</span></div>')
    return "\n".join(items[:14])


def body_content(form):
    k = key_of(form["name"])
    kind = form["kind"]
    out = []

    # هدر
    out.append(
        f'<div class="hdr">'
        f'<div class="ico">{esc(form["icon"])}</div>'
        f'<div><h2>{esc(form["title"])}</h2>'
        f'<div class="sub">{esc(form["subtitle"])}</div></div></div>'
    )

    # کارت‌های شاخص
    if form["kpis"]:
        out.append('<div class="kpis">')
        for icon, label, value in form["kpis"]:
            out.append(f'<div class="kpi"><div class="lb">{esc(icon)} {esc(label)}</div>'
                       f'<div class="vl">{esc(value)}</div>'
                       f'<div class="tr">▲ ۱۲٪</div></div>')
        out.append('</div>')

    # تب‌ها
    if form["tabs"]:
        out.append('<div class="tabs">')
        for i, (title, icon) in enumerate(form["tabs"], start=1):
            on = " on" if i == 1 else ""
            out.append(f'<div class="t{on}">{esc(icon)} {esc(title)}</div>')
        out.append('</div>')

    # نوار فیلتر
    if form["filters"] or form["actions"]:
        out.append('<div class="bar">')
        for name, label, control in form["filters"]:
            wide = " wide" if control == "search" else ""
            out.append(f'<div class="fld{wide}">'
                       f'{"🔍 " if control == "search" else "▾ "}{esc(label)}</div>')
        out.append('<div style="flex:1"></div>')
        for cmd, label, style in form["actions"][:3]:
            cls = {"Primary": "p", "Secondary": "s", "Danger": "d",
                   "Success": "g", "Ghost": "s"}.get(style, "s")
            out.append(f'<div class="btn {cls}">{esc(label)}</div>')
        out.append('</div>')

    # محتوا
    if form["sections"]:
        out.append('<div class="cards">')
        for i, (sec_title, sec_icon, fields) in enumerate(form["sections"], start=1):
            out.append(f'<div class="card"><h4>{esc(sec_icon)} {esc(sec_title)}</h4>')
            for spec in fields:
                prop, ftype, label, required = parse_field(spec)
                star = ' <span style="color:#BE123C">*</span>' if required else ""
                if ftype == "textarea":
                    body = f'<div class="in ta">{esc(label)}…</div>'
                elif ftype == "combo":
                    body = f'<div class="in">▾ {esc(label)}</div>'
                elif ftype == "check":
                    body = f'<div class="in">☐ {esc(label)}</div>'
                elif ftype == "date":
                    body = f'<div class="in">📅 ۱۴۰۴/۰۷/۱۹</div>'
                else:
                    body = f'<div class="in">{esc(label)}…</div>'
                out.append(f'<div class="row"><label>{esc(label)}{star}</label>{body}</div>')
            out.append('</div>')
        out.append('</div>')

    if form["cols"]:
        out.append('<table class="tbl"><thead><tr>')
        for binding, header, width in form["cols"]:
            out.append(f'<th>{esc(header)}</th>')
        out.append('</tr></thead><tbody>')
        for r in range(4):
            out.append('<tr>')
            for idx, (binding, header, width) in enumerate(form["cols"]):
                if idx == 0:
                    out.append(f'<td><strong>#{r + 1:03d}</strong></td>')
                elif "Status" in binding:
                    out.append('<td><span class="badge b ok">تأیید شده</span></td>')
                elif "Amount" in binding or "Balance" in binding:
                    out.append('<td style="text-align:left">۱۲۵٬۰۰۰</td>')
                else:
                    out.append('<td>—</td>')
            out.append('</tr>')
        out.append('</tbody></table>')

    if not form["sections"] and not form["cols"] and not form["kpis"]:
        out.append('<div class="empty"><span class="ic">🗒</span>'
                   'محتوای این صفحه بر اساس نوع فرم تکمیل می‌شود</div>')

    # پانویس
    if form["cols"]:
        out.append('<div class="foot">'
                   '<div class="pager">'
                   '<span class="pg">‹</span><span class="pg on">۱</span>'
                   '<span class="pg">۲</span><span class="pg">۳</span>'
                   '<span class="pg">›</span>'
                   '<span style="margin-right:10px">نمایش ۱ تا ۲۵ از ۱۲۴۵</span>'
                   '</div></div>')

    if form["actions"]:
        out.append('<div class="foot"><div></div><div class="acts">')
        for cmd, label, style in form["actions"]:
            cls = {"Primary": "p", "Secondary": "s", "Danger": "d",
                   "Success": "g", "Ghost": "s"}.get(style, "s")
            out.append(f'<div class="btn {cls}">{esc(label)}</div>')
        out.append('</div></div>')

    return "\n".join(out)


def legend_table(form):
    rows = []
    if form["filters"]:
        for name, label, control in form["filters"]:
            rows.append((esc(label), control, "فیلتر", "فیلتر کردن فهرست بر اساس این معیار"))
    if form["cols"]:
        for binding, header, width in form["cols"]:
            rows.append((esc(header), binding, "ستون", "نمایش مقدار در جدول"))
    for _st, _si, fields in form["sections"]:
        for spec in fields:
            prop, ftype, label, required = parse_field(spec)
            rows.append((esc(label), ftype, "فیلد" + (" (الزامی)" if required else ""),
                         "ورودیِ کاربر در بخش مربوط"))
    if form["actions"]:
        for cmd, label, style in form["actions"]:
            rows.append((esc(label), cmd, "اقدام", f"دکمه‌ی {style} — اجرای فرمان"))

    if not rows:
        return '<p style="font-size:13px;color:#64748B">این صفحه المانِ تعاملیِ خاصی ندارد.</p>'

    body = "\n".join(
        f"<tr><td>{r[0]}</td><td><code>{r[1]}</code></td><td>{r[2]}</td><td>{r[3]}</td></tr>"
        for r in rows
    )
    return (f'<table><thead><tr><th>برچسب</th><th>نام/نوع</th>'
            f'<th>المان</th><th>نقش</th></tr></thead><tbody>{body}</tbody></table>')


def gen_html(form):
    k = key_of(form["name"])
    kind_label = {
        "list": "صفحه‌ی فهرستی", "form": "فرم ورود داده", "detail": "صفحه‌ی جزئیات",
        "dashboard": "پیشخوان", "dialog": "پنجره‌ی گفتگو", "login": "صفحه‌ی ورود",
        "splash": "صفحه‌ی آغازین", "viewer": "نمایشگر", "settings": "تنظیمات",
        "shell": "قاب اصلی",
    }.get(form["kind"], form["kind"])

    content = body_content(form)
    if form["kind"] in ("login", "splash"):
        frame = f'<div class="app"><div style="display:flex;align-items:center;'
        frame += f'justify-content:center;min-height:430px;background:#F8FAFC">{content}</div></div>'
    else:
        frame = (f'<div class="app"><div class="topbar"><span class="brand">🏥 سهند</span>'
                 f'<span class="pill">🔍 جستجو</span><span class="sp"></span>'
                 f'<span class="pill">🔔 ۳</span><span class="pill">👤 کاربر</span>'
                 f'<span class="pill">🌙</span></div>'
                 f'<div class="shell"><div class="side">{nav_items(form["name"])}</div>'
                 f'<div class="main">{content}</div></div></div>')

    notes = (f'<div class="notes">💡 <strong>نکته طراحی:</strong> {esc(form["notes"])}</div>'
             if form["notes"] else "")

    return f"""<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>وایرفریم {esc(form['title'])} — سهند</title>
<style>{CSS}</style>
</head>
<body>
<div class="wrap">

  <div class="doc-head">
    <h1>{esc(form['icon'])} {esc(form['title'])}</h1>
    <p>{esc(form['subtitle'])}</p>
    <div class="meta">
      <span class="tag">📄 {esc(form['name'])}</span>
      <span class="tag">🧩 {esc(kind_label)}</span>
      <span class="tag">🗂 {esc(form['group'])}</span>
      <span class="tag">🔑 {esc(k)}</span>
      <span class="tag">🎨 تم‌پذیر · دو زبانه</span>
    </div>
  </div>

  <div class="body">

    <div class="frame">
      <div class="cap">نمای بصری پیشنهادی</div>
      {frame}
    </div>

    {notes}

    <div class="legend">
      <h3>📐 المان‌ها و مشخصات</h3>
      {legend_table(form)}
    </div>

    <div class="foot-doc">
      📅 ۱۴۰۴/۰۷/۱۹ · 🔖 v1.0 · بازطراحی رابط کاربر سهند
    </div>

  </div>
</div>
</body>
</html>
"""


# ══════════════════════════════════════════════════════════════════════
#  ۲) وایرفریم متنی (Markdown)
# ══════════════════════════════════════════════════════════════════════

def ascii_frame(form):
    W = 78
    def line(text=""):
        pad = W - 2 - len(text)
        return "│" + text + " " * max(pad, 0) + "│"

    out = ["┌" + "─" * (W - 2) + "┐"]

    # نوار بالا
    out.append(line(f" 🏥 سهند      🔍 جستجو                    🔔(۳)  👤  🌙  ⚙"))
    out.append("├" + "─" * (W - 2) + "┤")

    # هدر صفحه
    out.append(line(f" {form['icon']}  {form['title']}"))
    if form["subtitle"]:
        out.append(line(f"    {form['subtitle']}"))
    out.append("├" + "─" * (W - 2) + "┤")

    # کارت‌های شاخص
    if form["kpis"]:
        cells = [f"{ic} {lb}: {vl}" for ic, lb, vl in form["kpis"][:4]]
        row = "  ".join(f"{c:<18}" for c in cells)
        out.append(line(" " + row[:W - 4]))
        out.append("├" + "─" * (W - 2) + "┤")

    # تب‌ها
    if form["tabs"]:
        tabs = "   ".join(f"{'▓' if i == 1 else '░'}{ic}{t}"
                          for i, (t, ic) in enumerate(form["tabs"], start=1))
        out.append(line(" " + tabs[:W - 4]))
        out.append("├" + "─" * (W - 2) + "┤")

    # نوار فیلتر
    if form["filters"]:
        fl = "  ".join(f"[{lb}]" for _n, lb, _c in form["filters"][:4])
        out.append(line(" " + fl[:W - 4]))
        out.append("├" + "─" * (W - 2) + "┤")

    # بخش‌های فرم
    for sec_title, sec_icon, fields in form["sections"]:
        out.append(line(f" ┌ {sec_icon} {sec_title} " + "─" * max(W - 10 - len(sec_title), 4)))
        for spec in fields:
            prop, ftype, label, required = parse_field(spec)
            star = "*" if required else " "
            if ftype == "textarea":
                box = "[" + " " * 44 + "]\n" + " " * 8 + "[" + " " * 44 + "]"
            elif ftype == "combo":
                box = "[▾ " + " " * 42 + "]"
            elif ftype == "check":
                box = "[☐]"
            elif ftype == "date":
                box = "[📅 ۱۴۰۴/۰۷/۱۹" + " " * 30 + "]"
            else:
                box = "[" + " " * 44 + "]"
            out.append(line(f" │ {label}{star}"))
            for bl in box.split("\n"):
                out.append(line(" │   " + bl))
        out.append(line(" └" + "─" * (W - 6)))

    # ستون‌ها
    if form["cols"]:
        headers = [h for _b, h, _w in form["cols"]]
        widths = [min(max(len(h) + 2, 12), 22) for h in headers]
        hline = "".join(f"{h:<{w}}" for h, w in zip(headers, widths))
        out.append(line(" " + hline[:W - 4]))
        out.append("├" + "─" * (W - 2) + "┤")
        for r in range(1, 5):
            rline = "".join(f"{('#%03d' % r) if i == 0 else '—':<{w}}"
                            for i, w in enumerate(widths))
            out.append(line(" " + rline[:W - 4]))
        out.append("├" + "─" * (W - 2) + "┤")
        out.append(line(" ‹ قبلی   ۱  ۲  ۳  بعدی ›                    نمایش ۱ تا ۲۵ از ۱۲۴۵"))

    if not form["sections"] and not form["cols"] and not form["kpis"]:
        out.append(line())
        out.append(line("          🗒  محتوای این صفحه بر اساس نوع فرم تکمیل می‌شود"))
        out.append(line())

    # دکمه‌ها
    if form["actions"]:
        out.append("├" + "─" * (W - 2) + "┤")
        acts = "   ".join(
            f"{'▓' if s == 'Primary' else '▒' if s == 'Success' else '░'}{lb}"
            for _c, lb, s in form["actions"])
        out.append(line(" " + acts[:W - 4]))

    out.append("└" + "─" * (W - 2) + "┘")
    return "\n".join(out)


def gen_md(form):
    k = key_of(form["name"])
    kind_label = {
        "list": "صفحه‌ی فهرستی", "form": "فرم ورود داده", "detail": "صفحه‌ی جزئیات",
        "dashboard": "پیشخوان", "dialog": "پنجره‌ی گفتگو", "login": "صفحه‌ی ورود",
        "splash": "صفحه‌ی آغازین", "viewer": "نمایشگر", "settings": "تنظیمات",
        "shell": "قاب اصلی",
    }.get(form["kind"], form["kind"])

    md = []
    md.append(f"# {form['icon']} وایرفریم — {form['title']}")
    md.append("")
    md.append(f"> **{kind_label}** · فایل `{form['name']}` · گروه «{form['group']}»")
    md.append("")
    if form["subtitle"]:
        md.append(f"_{form['subtitle']}_")
        md.append("")

    md.append("---")
    md.append("")
    md.append("## 🖼 نمای کلی")
    md.append("")
    md.append("```")
    md.append(ascii_frame(form))
    md.append("```")
    md.append("")

    if form["kpis"]:
        md.append("## 📊 شاخص‌های کلیدی")
        md.append("")
        md.append("| شکلک | شاخص | مقدار نمونه |")
        md.append("|:--:|:---|:--:|")
        for ic, lb, vl in form["kpis"]:
            md.append(f"| {ic} | {lb} | {vl} |")
        md.append("")

    if form["tabs"]:
        md.append("## 📑 تب‌ها")
        md.append("")
        md.append("| # | عنوان | شکلک |")
        md.append("|:--:|:---|:--:|")
        for i, (t, ic) in enumerate(form["tabs"], start=1):
            md.append(f"| {i} | {t} | {ic} |")
        md.append("")

    if form["filters"]:
        md.append("## 🔍 فیلترها")
        md.append("")
        md.append("| نام | برچسب | کنترل |")
        md.append("|:---|:---|:--:|")
        for n, lb, c in form["filters"]:
            md.append(f"| `{n}` | {lb} | {c} |")
        md.append("")

    if form["sections"]:
        md.append("## 📝 بخش‌ها و فیلدها")
        md.append("")
        for i, (sec_title, sec_icon, fields) in enumerate(form["sections"], start=1):
            md.append(f"### {sec_icon} {i}. {sec_title}")
            md.append("")
            md.append("| ویژگی | برچسب | نوع کنترل | الزامی | نوع C# |")
            md.append("|:---|:---|:--:|:--:|:---|")
            for spec in fields:
                prop, ftype, label, required = parse_field(spec)
                md.append(f"| `{prop}` | {label} | {ftype} | "
                          f"{'✅' if required else '—'} | `{cs_type(prop, ftype)}` |")
            md.append("")

    if form["cols"]:
        md.append("## 📋 ستون‌های جدول")
        md.append("")
        md.append("| اتصال (Binding) | سرستون | عرض |")
        md.append("|:---|:---|:--:|")
        for b, h, w in form["cols"]:
            md.append(f"| `{b}` | {h} | {w} |")
        md.append("")

    if form["actions"]:
        md.append("## ⚡ اقدام‌ها")
        md.append("")
        md.append("| فرمان | برچسب | سبک دکمه |")
        md.append("|:---|:---|:--:|")
        for c, lb, s in form["actions"]:
            md.append(f"| `{c}Command` | {lb} | `{s}` |")
        md.append("")

    if form["notes"]:
        md.append("## 💡 نکته طراحی")
        md.append("")
        md.append(form["notes"])
        md.append("")

    md.append("## 🔗 فایل‌های مرتبط")
    md.append("")
    md.append(f"| فایل | مسیر |")
    md.append("|:---|:---|")
    md.append(f"| وایرفریم گرافیکی | `01-wireframes-html/{form['name']}.html` |")
    md.append(f"| View | `03-views/{form['name']}.xaml` |")
    md.append(f"| ViewModel | `04-viewmodels/{form['name']}ViewModel.cs` |")
    md.append("")
    md.append("---")
    md.append("")
    md.append("<div align=\"center\">📅 ۱۴۰۴/۰۷/۱۹ · 🔖 v1.0</div>")
    md.append("")
    return "\n".join(md)


# ══════════════════════════════════════════════════════════════════════
#  ۳) View (XAML)
# ══════════════════════════════════════════════════════════════════════

def vm_class(form):
    """نام کلاس ViewModel — هم‌نام با فایل خروجی."""
    # MainWindow استثناست: قاب اصلی برنامه نام کامل را نگه می‌دارد
    if form["name"] == "MainWindow":
        return "MainWindowViewModel"
    return f"{key_of(form['name'])}ViewModel"


def xaml_root(form):
    k = key_of(form["name"])
    vm = vm_class(form)
    return f"""    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ctrl="clr-namespace:{NS_CTRL};assembly=Sahand.ClinicPro.Desktop"
    xmlns:loc="clr-namespace:{NS_LOC};assembly=Sahand.ClinicPro.Desktop"
    xmlns:vm="clr-namespace:{NS_VM};assembly=Sahand.ClinicPro.Desktop"
    mc:Ignorable="d"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    d:DataContext="{{d:DesignInstance Type=vm:{vm}}}"
    SnapsToDevicePixels="True\""""


def xaml_kpis(form):
    if not form["kpis"]:
        return ""
    k = key_of(form["name"])
    items = []
    for i, (icon, label, _v) in enumerate(form["kpis"], start=1):
        items.append(f"""                <ctrl:StatCard Grid.Column="{i - 1}"
                               Icon="{icon}"
                               Title="{{loc:Loc {k}.Kpi{i}}}"
                               Value="{{Binding Kpi{i}Value}}"
                               Trend="{{Binding Kpi{i}Trend}}"
                               Margin="0,0,12,0" />""")
    cols = "".join('<ColumnDefinition Width="*" />' for _ in form["kpis"])
    return f"""
        <!-- 📊 شاخص‌های کلیدی -->
        <Grid Grid.Row="1" Margin="0,0,0,16">
            <Grid.ColumnDefinitions>{cols}</Grid.ColumnDefinitions>
{chr(10).join(items)}
        </Grid>"""


def xaml_filters(form):
    if not form["filters"]:
        return ""
    k = key_of(form["name"])
    parts = []
    for name, label, control in form["filters"]:
        if control == "search":
            parts.append(f"""                    <ctrl:SearchBox Width="260"
                                    Text="{{Binding SearchText, Mode=TwoWay}}"
                                    Placeholder="{{loc:Loc {k}.Filter_{name}}}"
                                    Margin="0,0,8,0" />""")
        elif control == "date":
            parts.append(f"""                    <DatePicker Style="{{DynamicResource Input.Date}}"
                                Width="160" Margin="0,0,8,0"
                                SelectedDate="{{Binding Filter{name.capitalize()}, Mode=TwoWay}}" />""")
        elif control == "check":
            parts.append(f"""                    <CheckBox Style="{{DynamicResource Input.CheckBox}}"
                              Content="{{loc:Loc {k}.Filter_{name}}}"
                              IsChecked="{{Binding Filter{name.capitalize()}, Mode=TwoWay}}"
                              VerticalAlignment="Center" Margin="0,0,8,0" />""")
        else:
            parts.append(f"""                    <ComboBox Style="{{DynamicResource Input.ComboBox}}"
                              Width="170" Margin="0,0,8,0"
                              SelectedValue="{{Binding Filter{name.capitalize()}, Mode=TwoWay}}" />""")
    return f"""
            <!-- 🔍 نوار فیلتر -->
            <Border Grid.Row="2" Style="{{DynamicResource Card.Flat}}"
                    Padding="12" Margin="0,0,0,12">
                <WrapPanel Orientation="Horizontal" VerticalAlignment="Center">
{chr(10).join(parts)}
                </WrapPanel>
            </Border>"""


def xaml_grid(form):
    if not form["cols"]:
        return ""
    k = key_of(form["name"])
    cols = []
    for binding, header, width in form["cols"]:
        w = "*" if width == "*" else width
        if binding in ("Status", "IsCritical", "IsActive"):
            cols.append(f"""                    <DataGridTemplateColumn Header="{{loc:Loc {k}.Col_{binding}}}"
                                            Width="{w}">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <ctrl:StatusBadge Text="{{Binding {binding}Text}}"
                                                  Level="{{Binding {binding}Level}}" />
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>""")
        elif "Amount" in binding or "Balance" in binding or "Salary" in binding:
            cols.append(f"""                    <DataGridTextColumn Header="{{loc:Loc {k}.Col_{binding}}}"
                                        Binding="{{Binding {binding}, StringFormat=N0}}"
                                        Width="{w}">
                        <DataGridTextColumn.ElementStyle>
                            <Style TargetType="TextBlock">
                                <Setter Property="TextAlignment" Value="Right" />
                                <Setter Property="FlowDirection" Value="LeftToRight" />
                            </Style>
                        </DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>""")
        elif "Date" in binding or "At" in binding or binding == "Time":
            cols.append(f"""                    <DataGridTextColumn Header="{{loc:Loc {k}.Col_{binding}}}"
                                        Binding="{{Binding {binding}, Converter={{StaticResource PersianDate}}}}"
                                        Width="{w}" />""")
        else:
            cols.append(f"""                    <DataGridTextColumn Header="{{loc:Loc {k}.Col_{binding}}}"
                                        Binding="{{Binding {binding}}}"
                                        Width="{w}" />""")

    return f"""
            <!-- 📋 جدول داده‌ها -->
            <Border Grid.Row="3" Style="{{DynamicResource Card}}"
                    Padding="0" ClipToBounds="True">
                <Grid>
                    <DataGrid Style="{{DynamicResource Grid.Modern}}"
                              ColumnHeaderStyle="{{DynamicResource Grid.Header}}"
                              RowStyle="{{DynamicResource Grid.Row}}"
                              CellStyle="{{DynamicResource Grid.Cell}}"
                              ItemsSource="{{Binding Items}}"
                              SelectedItem="{{Binding SelectedItem, Mode=TwoWay}}"
                              BorderThickness="0"
                              HeadersVisibility="Column">
                        <DataGrid.Columns>
{chr(10).join(cols)}
                        </DataGrid.Columns>
                    </DataGrid>

                    <!-- وضعیت تهی -->
                    <ctrl:EmptyState Icon="🗒"
                                     Title="{{loc:Loc Common.NoData}}"
                                     Visibility="{{Binding IsEmpty, Converter={{StaticResource BoolToVisibility}}}}" />
                </Grid>
            </Border>"""


def xaml_pager(form):
    if not form["cols"]:
        return ""
    return """
            <!-- 📄 صفحه‌بندی -->
            <Border Grid.Row="4" Padding="12,10,12,0">
                <Grid>
                    <TextBlock Text="{Binding PageSummary}"
                               Style="{DynamicResource Txt.Caption}"
                               VerticalAlignment="Center" />
                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Left">
                        <Button Style="{DynamicResource Btn.Secondary}"
                                Content="{loc:Loc Common.Previous}"
                                Command="{Binding PreviousPageCommand}" />
                        <Button Style="{DynamicResource Btn.Secondary}"
                                Content="{loc:Loc Common.Next}"
                                Command="{Binding NextPageCommand}" />
                    </StackPanel>
                </Grid>
            </Border>"""


def xaml_sections(form):
    if not form["sections"]:
        return ""
    k = key_of(form["name"])
    blocks = []
    for i, (sec_title, sec_icon, fields) in enumerate(form["sections"], start=1):
        rows = []
        for spec in fields:
            prop, ftype, label, required = parse_field(spec)
            star = ' <Run Text="*" Foreground="{DynamicResource Brush.Danger}" />' if required else ""
            ctrl = xaml_control(prop, ftype)
            rows.append(f"""                            <StackPanel Grid.Column="{len(rows) % 2}"
                                        Margin="0,0,12,10">
                                <TextBlock Style="{{DynamicResource Input.Label}}">
                                    <Run Text="{{loc:Loc {k}.Fld_{prop}}}"/>{star}
                                </TextBlock>
                                {ctrl}
                            </StackPanel>""")
        body = chr(10).join(rows)
        blocks.append(f"""
                    <!-- {sec_icon} {sec_title} -->
                    <Border Style="{{DynamicResource Card}}" Margin="0,0,0,14">
                        <StackPanel>
                            <TextBlock Text="{{loc:Loc {k}.Sec{i}}}"
                                       Style="{{DynamicResource Txt.Subtitle}}"
                                       Margin="0,0,0,12" />
                            <UniformGrid Columns="2">
{body}
                            </UniformGrid>
                        </StackPanel>
                    </Border>""")
    return chr(10).join(blocks)


def xaml_tabs(form):
    if not form["tabs"]:
        return ""
    k = key_of(form["name"])
    items = []
    for i, (title, icon) in enumerate(form["tabs"], start=1):
        body = ("<!-- محتوای این تب در ViewModel مربوطه بارگذاری می‌شود -->\n"
                "                            "
                f"<TextBlock Text=\"{{loc:Loc {k}.Tab{i}}}\" "
                "Style=\"{DynamicResource Txt.Secondary}\" />")
        items.append(f"""                        <TabItem Style="{{DynamicResource Tab.Item}}">
                            <TabItem.Header>
                                <StackPanel Orientation="Horizontal">
                                    <TextBlock Text="{icon}" Margin="0,0,6,0" />
                                    <TextBlock Text="{{loc:Loc {k}.Tab{i}}}" />
                                </StackPanel>
                            </TabItem.Header>
                            <Border Padding="0,16,0,0">
                                {body}
                            </Border>
                        </TabItem>""")
    return f"""
            <!-- 📑 تب‌ها -->
            <TabControl Grid.Row="2" Style="{{DynamicResource Tab.Modern}}">
{chr(10).join(items)}
            </TabControl>"""


def xaml_actions(form, indent=16):
    if not form["actions"]:
        return ""
    k = key_of(form["name"])
    pad = " " * indent
    items = []
    for cmd, label, style in form["actions"]:
        st = {"Primary": "Btn.Primary", "Secondary": "Btn.Secondary",
              "Danger": "Btn.Danger", "Success": "Btn.Success",
              "Ghost": "Btn.Ghost"}.get(style, "Btn.Secondary")
        items.append(f'{pad}<Button Style="{{DynamicResource {st}}}"\n'
                     f'{pad}        Content="{{loc:Loc {k}.Act_{cmd}}}"\n'
                     f'{pad}        Command="{{Binding {cmd}Command}}"\n'
                     f'{pad}        MinHeight="{{DynamicResource Size.ButtonHeight}}" />')
    return chr(10).join(items)


def gen_view(form):
    k = key_of(form["name"])
    kind = form["kind"]
    win = is_window(kind)

    head = ("<Window x:Class=\"Sahand.ClinicPro.Desktop.Views.%s\"" % form["name"]) if win else \
           ("<UserControl x:Class=\"Sahand.ClinicPro.Desktop.Views.%s\"" % form["name"])

    title_attr = f'\n    Title="{{loc:Loc {k}.Title}}"' if win else ""

    # بدنه بر اساس نوع
    if kind == "login":
        body = f"""    <Grid Background="{{DynamicResource Brush.Canvas}}">
        <Border Style="{{DynamicResource Card}}"
                Width="400" VerticalAlignment="Center" HorizontalAlignment="Center"
                Padding="32">
            <StackPanel>
                <TextBlock Text="🏥" FontSize="44" HorizontalAlignment="Center" />
                <TextBlock Text="{{loc:Loc {k}.Title}}"
                           Style="{{DynamicResource Txt.Headline}}"
                           HorizontalAlignment="Center" Margin="0,8,0,24" />
{xaml_sections(form)}

                <!-- پیام خطا -->
                <TextBlock Text="{{Binding ErrorMessage}}"
                           Style="{{DynamicResource Input.Error}}"
                           Visibility="{{Binding ErrorMessage, Converter={{StaticResource NullToCollapsed}}}}"
                           Margin="0,8" />

                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center"
                            Margin="0,16,0,0">
{xaml_actions(form, 16)}
                </StackPanel>
            </StackPanel>
        </Border>
    </Grid>"""
    elif kind == "splash":
        body = f"""    <Grid Background="{{DynamicResource Brush.Primary}}">
        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
            <TextBlock Text="🏥" FontSize="72" HorizontalAlignment="Center" />
            <TextBlock Text="{{loc:Loc Common.AppName}}"
                       FontSize="32" FontWeight="Bold" Foreground="White"
                       HorizontalAlignment="Center" Margin="0,16,0,4" />
            <TextBlock Text="{{loc:Loc {k}.Subtitle}}"
                       Foreground="White" Opacity="0.85"
                       HorizontalAlignment="Center" />
        </StackPanel>
    </Grid>"""
    elif kind == "shell":
        body = f"""    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- 🧭 نوار کناری -->
        <Border Grid.Row="0" Grid.RowSpan="2" Grid.Column="0"
                Width="{{DynamicResource Size.SidebarWidth}}"
                Background="{{DynamicResource Brush.Sidebar}}">
            <StackPanel Margin="12">
                <TextBlock Foreground="White" FontSize="20"
                           Text="{{loc:Loc Common.AppName}}"
                           FontWeight="Bold" Margin="0,8,0,20" />
                <ItemsControl ItemsSource="{{Binding NavItems}}" />
            </StackPanel>
        </Border>

        <!-- 🔝 نوار بالا -->
        <Border Grid.Row="0" Grid.Column="1"
                Height="{{DynamicResource Size.HeaderHeight}}"
                Background="{{DynamicResource Brush.Surface}}"
                BorderBrush="{{DynamicResource Brush.Border}}"
                BorderThickness="0,0,0,1" Padding="16,0">
            <Grid>
                <ctrl:SearchBox Width="320" HorizontalAlignment="Right"
                                Text="{{Binding GlobalSearchText, Mode=TwoWay}}" />
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Left">
                    <Button Style="{{DynamicResource Btn.Icon}}" Content="🌙"
                            Command="{{Binding ToggleThemeCommand}}"
                            ToolTip="{{loc:Loc Common.ToggleTheme}}" />
                    <Button Style="{{DynamicResource Btn.Icon}}" Content="🌐"
                            Command="{{Binding ToggleLanguageCommand}}"
                            ToolTip="{{loc:Loc Common.ToggleLanguage}}" />
                    <Button Style="{{DynamicResource Btn.Icon}}" Content="🔔"
                            Command="{{Binding OpenNotificationsCommand}}" />
                </StackPanel>
            </Grid>
        </Border>

        <!-- 📄 ناحیه‌ی محتوا -->
        <ContentControl Grid.Row="1" Grid.Column="1"
                        Content="{{Binding CurrentPage}}" />
    </Grid>"""
    elif kind in ("form", "dialog"):
        rows = ["<RowDefinition Height=\"Auto\" />", "<RowDefinition Height=\"*\" />",
                "<RowDefinition Height=\"Auto\" />"]
        body = f"""    <Grid Background="{{DynamicResource Brush.Canvas}}">
        <Grid.RowDefinitions>
            {rows[0]}
            {rows[1]}
            {rows[2]}
        </Grid.RowDefinitions>

        <!-- 🧭 هدر -->
        <Border Grid.Row="0" Background="{{DynamicResource Brush.Surface}}"
                BorderBrush="{{DynamicResource Brush.Border}}"
                BorderThickness="0,0,0,1"
                Padding="{'20,14'}">
            <Grid>
                <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock Text="{form['icon']}" FontSize="24" Margin="0,0,10,0" />
                    <StackPanel>
                        <TextBlock Text="{{Binding WindowTitle}}"
                                   Style="{{DynamicResource Txt.Headline}}" />
                        <TextBlock Text="{{loc:Loc {k}.Subtitle}}"
                                   Style="{{DynamicResource Txt.Caption}}" />
                    </StackPanel>
                </StackPanel>
            </Grid>
        </Border>

        <!-- 📝 محتوا -->
        <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto"
                      Padding="{'20,16'}">
            <StackPanel MaxWidth="960">
{xaml_sections(form) if form['sections'] else f'                <TextBlock Text="{{{{loc:Loc {k}.Subtitle}}}}" Style="{{{{DynamicResource Txt.Secondary}}}}" />'}
{xaml_tabs(form)}
{xaml_grid(form) if form['cols'] else ''}
            </StackPanel>
        </ScrollViewer>

        <!-- ⚡ نوار اقدام -->
        <Border Grid.Row="2" Background="{{DynamicResource Brush.Surface}}"
                BorderBrush="{{DynamicResource Brush.Border}}"
                BorderThickness="0,1,0,0" Padding="{'20,12'}">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left">
{xaml_actions(form, 16)}
            </StackPanel>
        </Border>

        <!-- نشانگر بارگذاری -->
        <Border Grid.Row="0" Grid.RowSpan="3"
                Background="{{DynamicResource Brush.Canvas}}" Opacity="0.6"
                Visibility="{{Binding IsBusy, Converter={{StaticResource BoolToVisibility}}}}">
            <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
                <TextBlock Text="⏳" FontSize="32" HorizontalAlignment="Center" />
                <TextBlock Text="{{Binding BusyMessage}}"
                           Style="{{DynamicResource Txt.Body}}" Margin="0,8,0,0" />
            </StackPanel>
        </Border>
    </Grid>"""
    elif kind == "viewer":
        body = f"""    <Grid Background="{{DynamicResource Brush.Canvas}}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- 🛠 نوار ابزار -->
        <Border Grid.Row="0" Background="{{DynamicResource Brush.Surface}}"
                BorderBrush="{{DynamicResource Brush.Border}}"
                BorderThickness="0,0,0,1" Padding="12">
            <StackPanel Orientation="Horizontal">
{xaml_actions(form, 16)}
            </StackPanel>
        </Border>

        <!-- 🖼 ناحیه‌ی تصویر -->
        <Border Grid.Row="1" Margin="12" Style="{{DynamicResource Card}}"
                ClipToBounds="True">
            <Image Source="{{Binding CurrentImage}}" Stretch="Uniform" />
        </Border>
    </Grid>"""
    else:
        # list / detail / dashboard / settings
        has_grid = bool(form["cols"])
        rows = ["<RowDefinition Height=\"Auto\" />"]
        if form["kpis"]:
            rows.append("<RowDefinition Height=\"Auto\" />")
        rows.append("<RowDefinition Height=\"Auto\" />")
        if form["tabs"]:
            rows.append("<RowDefinition Height=\"*\" />")
        if has_grid:
            rows.append("<RowDefinition Height=\"*\" />")
            rows.append("<RowDefinition Height=\"Auto\" />")
        else:
            rows.append("<RowDefinition Height=\"*\" />")

        grid_row = 3 if form["tabs"] else 2
        pager_row = grid_row + 1
        content_row = grid_row if not has_grid else grid_row

        header_row_content = f"""        <!-- 🧭 هدر صفحه -->
        <ctrl:PageHeader Grid.Row="0"
                         Icon="{form['icon']}"
                         Title="{{loc:Loc {k}.Title}}"
                         Subtitle="{{loc:Loc {k}.Subtitle}}">
            <ctrl:PageHeader.PrimaryAction>
                <StackPanel Orientation="Horizontal">
{xaml_actions(form, 20)}
                </StackPanel>
            </ctrl:PageHeader.PrimaryAction>
        </ctrl:PageHeader>"""

        filter_row = 2
        kpis_block = xaml_kpis(form) if form["kpis"] else ""

        blocks_filter = ""
        if form["filters"]:
            blocks_filter = xaml_filters(form).replace(
                'Grid.Row="2"', 'Grid.Row="%d"' % filter_row)

        blocks_tabs = ""
        if form["tabs"]:
            blocks_tabs = xaml_tabs(form).replace(
                'Grid.Row="2"', 'Grid.Row="%d"' % filter_row)

        SV = '<ScrollViewer Grid.Row="%d" VerticalScrollBarVisibility="Auto" Padding="20,0,20,16">' % grid_row
        SV_END = "</ScrollViewer>"
        need_sv = (not has_grid) and (not form["tabs"])
        open_sv = SV if need_sv else ""
        close_sv = SV_END if need_sv else ""

        blocks_sections = ""
        if form["sections"] and not is_window(form["kind"]):
            blocks_sections = xaml_sections(form)

        blocks_grid = ""
        if has_grid:
            blocks_grid = xaml_grid(form).replace(
                'Grid.Row="3"', 'Grid.Row="%d"' % grid_row)

        blocks_pager = ""
        if has_grid:
            blocks_pager = xaml_pager(form).replace(
                'Grid.Row="4"', 'Grid.Row="%d"' % pager_row)

        rows_joined = chr(10).join("            " + r for r in rows)

        body = f"""    <Grid Background="{{DynamicResource Brush.Canvas}}">
        <Grid.RowDefinitions>
{rows_joined}
        </Grid.RowDefinitions>

{header_row_content}
{kpis_block}
{blocks_filter}
{blocks_tabs}
{open_sv}
{blocks_sections}
{close_sv}
{blocks_grid}
{blocks_pager}

        <!-- ⏳ نشانگر بارگذاری -->
        <Border Grid.Row="0" Grid.RowSpan="{len(rows)}"
                Background="{{DynamicResource Brush.Canvas}}" Opacity="0.6"
                Visibility="{{Binding IsBusy, Converter={{StaticResource BoolToVisibility}}}}">
            <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
                <TextBlock Text="⏳" FontSize="32" HorizontalAlignment="Center" />
                <TextBlock Text="{{Binding BusyMessage}}"
                           Style="{{DynamicResource Txt.Body}}" Margin="0,8,0,0" />
            </StackPanel>
        </Border>
    </Grid>"""

    root_style = "Window.Modern" if win else "Page.Modern"
    style_attr = f'\n    Style="{{DynamicResource {root_style}}}"'

    return f"""{head}
{xaml_root(form)}{title_attr}{style_attr}
    FlowDirection="RightToLeft">

    <!--
    ═══════════════════════════════════════════════════════════════════════
      {form['icon']} {form['title']}  —  بازطراحی شده
    ═══════════════════════════════════════════════════════════════════════
      • تمام رنگ‌ها از Tokens.xaml و با DynamicResource دریافت می‌شوند
        → تعویض تمِ روشن/تاریک در لحظه بدون تغییر این فایل
      • تمام متن‌ها از Strings.resx از طریق {{loc:Loc}} دریافت می‌شوند
        → افزودن زبان جدید بدون تغییر این فایل
      • وایرفریم مرجع:  02-wireframes-md/{form['name']}.md
        وایرفریم گرافیکی: 01-wireframes-html/{form['name']}.html
    ═══════════════════════════════════════════════════════════════════════
    -->

{body}

</{'Window' if win else 'UserControl'}>
"""


# ══════════════════════════════════════════════════════════════════════
#  ۴) ViewModel (C#)
# ══════════════════════════════════════════════════════════════════════

def vm_properties(form):
    props = []
    for _st, _si, fields in form["sections"]:
        for spec in fields:
            prop, ftype, label, required = parse_field(spec)
            t = cs_type(prop, ftype)
            props.append((prop, t, required, label))
    return props


def gen_viewmodel(form):
    k = key_of(form["name"])
    kind = form["kind"]
    cls = vm_class(form)

    props = vm_properties(form)
    kpi_count = len(form["kpis"])

    # درخت ناوبری برای قاب اصلی (از روی فهرست فرم‌ها ساخته می‌شود)
    nav_entries = []
    if kind == "shell":
        from forms_spec import FORMS as ALL_FORMS
        for _f in ALL_FORMS:
            if _f["kind"] in ("login", "splash", "shell", "dialog"):
                continue
            nav_entries.append(
                '            if (!groups.ContainsKey("%s"))\n'
                '                groups["%s"] = new List<NavItemViewModel>();\n'
                '\n'
                '            groups["%s"].Add(new NavItemViewModel\n'
                '            {\n'
                '                Title = LocalizationManager.Get("%s.Title"),\n'
                '                Icon = "%s",\n'
                '                TargetKey = "%s",\n'
                '            });' % (_f["group"], _f["group"], _f["group"],
                                      key_of(_f["name"]), _f["icon"], key_of(_f["name"]))
            )


    # کلاس پایه: فرم‌ها از FormViewModelBase ارث می‌برند و MarkDirty دارند
    if kind == "list":
        base_cls = "PageViewModelBase<%sItem>" % k
    elif kind in ("form", "dialog"):
        base_cls = "FormViewModelBase"
    else:
        base_cls = "ViewModelBase"


    # ویژگی‌های شاخص
    kpi_props = ""
    if kpi_count:
        parts = []
        for i in range(1, kpi_count + 1):
            parts.append(f"""        private string _kpi{i}Value = "—";
        private double? _kpi{i}Trend;

        /// <summary>مقدار شاخص {i} — «{form['kpis'][i-1][1]}»</summary>
        public string Kpi{i}Value
        {{
            get => _kpi{i}Value;
            set => SetProperty(ref _kpi{i}Value, value);
        }}

        /// <summary>روند شاخص {i} بر حسب درصد</summary>
        public double? Kpi{i}Trend
        {{
            get => _kpi{i}Trend;
            set => SetProperty(ref _kpi{i}Trend, value);
        }}
""")
        kpi_props = "\n".join(parts)

    # ویژگی‌های فیلدها
    field_props = ""
    if props:
        parts = []
        for prop, t, required, label in props:
            tp = t.rstrip("?")
            nullable = t.endswith("?")
            default = cs_default(prop, "combo" if t == "Guid?" else ("text" if t == "string" else
                                 ("money" if t == "decimal" else ("check" if t == "bool" else
                                  ("date" if t == "DateTime?" else "text")))))
            if t == "Guid?":
                default = "null"
            dirty = "\n                    MarkDirty();" if base_cls == "FormViewModelBase" else ""
            parts.append(f"""        private {t} _{prop[0].lower() + prop[1:]} = {default};

        /// <summary>{label}{' (الزامی)' if required else ''}</summary>
        public {t} {prop}
        {{
            get => _{prop[0].lower() + prop[1:]};
            set
            {{
                if (SetProperty(ref _{prop[0].lower() + prop[1:]}, value))
                {{
                    ClearErrors();{dirty}
                }}
            }}
        }}
""")
        field_props = "\n".join(parts)

    # ویژگی‌های فیلتر
    filter_props = ""
    if form["filters"]:
        parts = []
        for name, label, control in form["filters"]:
            if control == "search":
                continue
            t = "DateTime?" if control == "date" else ("bool" if control == "check" else "string")
            default = "null" if t == "DateTime?" else ("false" if t == "bool" else "null")
            parts.append(f"""        private {t} _filter{name.capitalize()} = {default};

        /// <summary>فیلتر: {label}</summary>
        public {t} Filter{name.capitalize()}
        {{
            get => _filter{name.capitalize()};
            set
            {{
                if (SetProperty(ref _filter{name.capitalize()}, value))
                    _ = LoadAsync();
            }}
        }}
""")
        filter_props = "\n".join(parts)

    # فرمان‌ها
    cmd_props = ""
    if form["actions"]:
        parts = []
        for cmd, label, style in form["actions"]:
            if cmd in ("Save", "Cancel"):
                continue
            parts.append(f"""        private IAsyncRelayCommand? _{cmd[0].lower() + cmd[1:]}Command;

        /// <summary>{label}</summary>
        public IAsyncRelayCommand {cmd}Command =>
            _{cmd[0].lower() + cmd[1:]}Command ??= new AsyncRelayCommand({cmd}Async);
""")
        cmd_props = "\n".join(parts)

    # متدهای فرمان
    cmd_methods = ""
    if form["actions"]:
        parts = []
        for cmd, label, style in form["actions"]:
            if cmd in ("Save", "Cancel"):
                continue
            method = cmd[0].lower() + cmd[1:]
            if cmd in ("New", "NewItem", "NewOrder", "NewInvoice", "NewReception",
                       "NewVisit", "NewCount", "NewSession", "NewSurvey", "NewVoucher",
                       "NewClaim", "NewRole", "NewCount"):
                body = f"""            // TODO: باز کردن فرم ایجاد از طریق IDialogService
            await Task.CompletedTask;"""
            elif cmd in ("Print", "PrintReport", "PrintRecord", "PrintPayslips", "PrintReceipt"):
                body = f"""            // TODO: تولید خروجی چاپ با استفاده از قالب‌های چاپ
            await Task.CompletedTask;"""
            elif cmd in ("Export", "ExportExcel", "ExportPdf"):
                body = f"""            // TODO: تولید خروجی و ذخیره در مسیر انتخابی کاربر
            await Task.CompletedTask;"""
            else:
                body = f"""            var ok = await RunAsync(async ct =>
            {{
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }}, "{label}");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");"""
            parts.append(f"""        /// <summary>
        /// {label}
        /// </summary>
        private async Task {method}Async()
        {{
{body}
        }}
""")
        cmd_methods = "\n".join(parts)

    # اعتبارسنجی
    validate_body = ""
    if props:
        checks = []
        for prop, t, required, label in props:
            if not required:
                continue
            if t == "string":
                checks.append(f"""            if (string.IsNullOrWhiteSpace({prop}))
                AddError("{label} الزامی است", nameof({prop}));""")
            elif t == "Guid?":
                checks.append(f"""            if ({prop} is null)
                AddError("{label} الزامی است", nameof({prop}));""")
            elif t == "decimal":
                checks.append(f"""            if ({prop} <= 0)
                AddError("{label} باید بزرگ‌تر از صفر باشد", nameof({prop}));""")
            elif t == "DateTime?":
                checks.append(f"""            if ({prop} is null)
                AddError("{label} الزامی است", nameof({prop}));""")
        if checks:
            validate_body = "\n".join(checks) + "\n\n            return !HasErrors;"

    if not validate_body:
        validate_body = "            return true;"

    # تعیین کلاس پایه
    if kind == "list":
        base = "PageViewModelBase<%sItem>" % k
        usings = f"using {NS_BASE};\n"
        extra = f"""        /// <inheritdoc />
        protected override async Task<PagedResult<{k}Item>?> FetchAsync(CancellationToken ct)
        {{
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<{k}Item>>(
            //       "api/v1/{k.lower}s?page={{PageNumber}}&size={{PageSize}}", ct);
            //   return new PagedResult<{k}Item>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<{k}Item>();
            return new PagedResult<{k}Item>(items, 0);
        }}
"""
        row_props = chr(10).join(
            "        /// <summary>%s</summary>" % h + chr(10) +
            "        public string %s { get; set; } = string.Empty;" % b + chr(10)
            for b, h, _w in form["cols"]
        ) or ("        /// <summary>شناسه</summary>" + chr(10) +
              "        public Guid Id { get; set; }" + chr(10))

        item_record = """

    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «%s»
    /// </summary>
    public sealed class %sItem
    {
%s
        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
""" % (form["title"], k, row_props)
    elif kind in ("form", "dialog"):
        base = "FormViewModelBase"
        usings = f"using {NS_BASE};\n"
        extra = f"""        /// <inheritdoc />
        protected override bool Validate()
        {{
            ClearAllErrors();
{validate_body}
        }}

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {{
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new {{ ... }};
            //   await _apiClient.PostAsync<{k}Dto>("api/v1/{k.lower}s", payload, ct);

            await Task.Delay(200, ct);
        }}
"""
        item_record = ""
    elif kind == "shell":
        base = "ViewModelBase"
        usings = f"using {NS_BASE};\n"
        extra = f"""        /// <summary>آیتم‌های نوار کناری</summary>
        public ObservableCollection<NavItemViewModel> NavItems {{ get; }} = new();

        private string _globalSearchText = string.Empty;

        /// <summary>متنِ جستجوی یکپارچه در نوار بالا</summary>
        public string GlobalSearchText
        {{
            get => _globalSearchText;
            set => SetProperty(ref _globalSearchText, value);
        }}

        private object? _currentPage;

        /// <summary>صفحه‌ی در حال نمایش در ناحیه‌ی محتوا</summary>
        public object? CurrentPage
        {{
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }}

        /// <summary>تغییر تم میان روشن و تاریک</summary>
        public IRelayCommand ToggleThemeCommand => new RelayCommand(ToggleTheme);

        /// <summary>تغییر زبان میان فارسی و انگلیسی</summary>
        public IRelayCommand ToggleLanguageCommand => new RelayCommand(ToggleLanguage);

        /// <summary>نمایش پنل اعلان‌ها</summary>
        public IAsyncRelayCommand OpenNotificationsCommand =>
            new AsyncRelayCommand(OpenNotificationsAsync);

        private void ToggleTheme()
        {{
            ThemeManager.Toggle();
            ShowSuccess("تم با موفقیت تغییر کرد");
        }}

        private void ToggleLanguage()
        {{
            var next = LocalizationManager.IsRightToLeft ? "en-US" : "fa-IR";
            LocalizationManager.SetCulture(next);
            BuildNavigation();
        }}

        private async Task OpenNotificationsAsync()
        {{
            await RunAsync(async ct => await Task.Delay(120, ct), "دریافت اعلان‌ها…");
        }}

        /// <summary>
        /// ساختِ درخت ناوبریِ نوار کناری بر اساس گروه‌بندی فرم‌ها.
        /// </summary>
        /// <remarks>
        /// برای افزودن یک صفحه‌ی جدید کافی است آن را در
        /// <c>tools/forms_spec.py</c> ثبت کنید تا در این‌جا هم پدیدار شود.
        /// </remarks>
        private void BuildNavigation()
        {{
            NavItems.Clear();
            var groups = new Dictionary<string, List<NavItemViewModel>>();

{chr(10).join(nav_entries)}

            foreach (var kv in groups)
            {{
                var group = new NavItemViewModel
                {{
                    Title = kv.Key,
                    Icon = "🗂",
                    IsGroup = true,
                }};

                foreach (var child in kv.Value)
                    group.Children.Add(child);

                NavItems.Add(group);
            }}

            if (NavItems.Count > 0)
                NavItems[0].IsExpanded = true;
        }}

        /// <inheritdoc />
        public override async Task LoadAsync()
        {{
            BuildNavigation();

            await RunAsync(async ct =>
            {{
                // TODO: بارگذاریِ تنظیمات کاربر، مجوزها و اعلان‌های خوانده‌نشده
                await Task.Delay(200, ct);
            }}, "در حال آماده‌سازی…");
        }}
"""
        item_record = f"""

    /// <summary>
    /// یک آیتم در نوار کناریِ برنامه
    /// </summary>
    public sealed class NavItemViewModel : ViewModelBase
    {{
        /// <summary>عنوان نمایشی</summary>
        public string Title {{ get; set; }} = string.Empty;

        /// <summary>شکلکِ نمایشی</summary>
        public string Icon {{ get; set; }} = string.Empty;

        /// <summary>آیا این آیتم یک گروه است؟</summary>
        public bool IsGroup {{ get; set; }}

        private bool _isExpanded;

        /// <summary>آیا گروه باز است؟</summary>
        public bool IsExpanded
        {{
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }}

        /// <summary>نامِ نوعِ صفحه برای ناوبری (در صورت نیاز)</summary>
        public string? TargetKey {{ get; set; }}

        /// <summary>زیرآیتم‌ها</summary>
        public ObservableCollection<NavItemViewModel> Children {{ get; }} = new();
    }}
"""
    else:
        base = "ViewModelBase"
        usings = f"using {NS_BASE};\n"
        extra = f"""        /// <summary>
        /// بارگذاری داده‌های اولیه
        /// </summary>
        public async Task LoadAsync()
        {{
            await RunAsync(async ct =>
            {{
                // TODO: بارگذاری داده‌ها از سرویس مربوط
                await Task.Delay(200, ct);
            }}, "در حال بارگذاری…");
        }}
"""
        item_record = ""

    # سازنده
    ctor_params = "ILogger? logger = null"
    if kind == "list":
        ctor_body = ""
    else:
        ctor_body = ""

    return f"""using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
{usings}using {NS_LOC};\nusing Sahand.ClinicPro.Desktop.Themes;
namespace {NS_VM}
{{
    /// <summary>
    /// ViewModel صفحه‌ی «{form['title']}» — {form['subtitle']}
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/{form['name']}.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/{form['name']}.html</c>
    /// 🎨 View متناظر:     <c>03-views/{form['name']}.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{{loc:Loc}}</c> تأمین می‌شوند.
    /// </remarks>
    public class {cls} : {base}
    {{
        public {cls}({ctor_params}) : base(logger)
        {{
{ctor_body}            _ = LoadAsync();
        }}

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════

{kpi_props}
{field_props}
{filter_props}
        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

{cmd_props}
        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

{extra}
        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

{cmd_methods}
    }}
{item_record}}}
"""


# ══════════════════════════════════════════════════════════════════════
#  ۵) فایل‌های زبان (resx)
# ══════════════════════════════════════════════════════════════════════

RESX_HEADER = """<?xml version="1.0" encoding="utf-8"?>
<root>
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:import namespace="http://www.w3.org/XML/1998/namespace" />
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="metadata">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" />
              </xsd:sequence>
              <xsd:attribute name="name" use="required" type="xsd:string" />
              <xsd:attribute name="type" type="xsd:string" />
              <xsd:attribute name="mimetype" type="xsd:string" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="assembly">
            <xsd:complexType>
              <xsd:attribute name="alias" type="xsd:string" />
              <xsd:attribute name="name" type="xsd:string" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
              <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
              <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
"""


def gen_resx(strings, index):
    parts = [RESX_HEADER]
    for key in sorted(strings):
        value = strings[key][index]
        escaped = (str(value).replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;"))
        parts.append(f'  <data name="{key}" xml:space="preserve">\n'
                     f'    <value>{escaped}</value>\n'
                     f'  </data>')
    parts.append("</root>")
    return "\n".join(parts) + "\n"


# ══════════════════════════════════════════════════════════════════════
#  اجرا
# ══════════════════════════════════════════════════════════════════════

def main():
    for d in (DIR_HTML, DIR_MD, DIR_VIEW, DIR_VM, DIR_RES):
        d.mkdir(parents=True, exist_ok=True)

    print(f"🎨 در حال تولید {len(FORMS)} فرم…")

    for form in FORMS:
        name = form["name"]

        (DIR_HTML / f"{name}.html").write_text(gen_html(form), encoding="utf-8")
        (DIR_MD / f"{name}.md").write_text(gen_md(form), encoding="utf-8")
        (DIR_VIEW / f"{name}.xaml").write_text(gen_view(form), encoding="utf-8")
        (DIR_VM / f"{vm_class(form)}.cs").write_text(gen_viewmodel(form), encoding="utf-8")

    strings = collect_strings()
    (DIR_RES / "Strings.resx").write_text(gen_resx(strings, 0), encoding="utf-8")
    (DIR_RES / "Strings.en.resx").write_text(gen_resx(strings, 1), encoding="utf-8")

    print(f"✅ {len(FORMS)} وایرفریم HTML")
    print(f"✅ {len(FORMS)} وایرفریم Markdown")
    print(f"✅ {len(FORMS)} View (XAML)")
    print(f"✅ {len(FORMS)} ViewModel (C#)")
    print(f"✅ {len(strings)} کلید زبان در Strings.resx و Strings.en.resx")


if __name__ == "__main__":
    main()
