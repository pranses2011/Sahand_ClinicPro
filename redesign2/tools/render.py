# -*- coding: utf-8 -*-
"""
کتابخانهٔ رندر — تولید نشانه‌گذاری برای وایرفریم‌ها
هر کهن‌الگو تابعِ رندرِ خود را دارد تا خروجی‌ها یکنواخت نباشند.
"""

import json
import html
from pathlib import Path

REQ = '<span class="rq">*</span>'

ICONS = json.loads((Path(__file__).parent / "icons.json").read_text(encoding="utf-8"))
SVG_ALL = ICONS["all"]

# رنگ‌های شاخص
ACCENT = {
    "teal":   ("#0D9488", "#CCFBF1"),
    "green":  ("#059669", "#D1FAE5"),
    "amber":  ("#D97706", "#FEF3C7"),
    "rose":   ("#E11D48", "#FFE4E6"),
    "blue":   ("#2563EB", "#DBEAFE"),
    "violet": ("#7C3AED", "#EDE9FE"),
}

CHIP_TONE = {
    "ok": "c-ok", "wn": "c-wn", "dg": "c-dg", "in": "c-in",
    "pp": "c-pp", "nu": "c-nu", "cy": "c-cy",
}


# ───────────────────────────────────────────────────────────────────────
#  آیکون
# ───────────────────────────────────────────────────────────────────────

def ic(name, cls="i", style=""):
    """تولید آیکونِ SVG درون‌خطی از مجموعهٔ Lucide."""
    inner = SVG_ALL.get(name)
    if inner is None:
        inner = SVG_ALL.get("circle", "")
    s = f' style="{style}"' if style else ""
    return (f'<svg class="{cls}" viewBox="0 0 24 24" fill="none" stroke="currentColor" '
            f'stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"'
            f' aria-hidden="true"{s}>{inner}</svg>')


def e(t):
    return html.escape(str(t), quote=True)


# ───────────────────────────────────────────────────────────────────────
#  اجزای مشترک
# ───────────────────────────────────────────────────────────────────────

def kpi_row(kpis):
    if not kpis:
        return ""
    cells = []
    for k in kpis:
        fg, bg = ACCENT.get(k["ac"], ACCENT["teal"])
        trend = ""
        if k["tr"]:
            arrow = "trending-up" if k["dir"] == "up" else ("trending-down" if k["dir"] == "down" else "minus")
            cls = "up" if k["dir"] == "up" else ("down" if k["dir"] == "down" else "flat")
            trend = f'<div class="kt {cls}">{ic(arrow, "i-sm")}{e(k["tr"])}</div>'
        cells.append(
            f'<div class="kpi" style="--ac:{fg};--acbg:{bg}">'
            f'<div class="ki">{ic(k["ic"], "i-lg")}</div>'
            f'<div class="kv"><div class="kl">{e(k["l"])}</div>'
            f'<div class="kn num">{e(k["v"])}</div>{trend}</div></div>'
        )
    n = max(1, len(kpis))
    return f'<div class="kpis" style="grid-template-columns:repeat({n},1fr)">{"".join(cells)}</div>'


def toolbar(form):
    if not form["toolbar"]:
        return ""
    ctls = []
    for tb in form["toolbar"]:
        k, w = tb["k"], tb["w"]
        cls = f"ctl {w}" if w else "ctl"
        val = tb["v"] or ""
        if k == "search":
            ctls.append(f'<div class="fld"><label>{e(tb["l"])}</label>'
                        f'<div class="{cls} search">{ic("search")}<span class="ph">{e(val)}</span></div></div>')
        elif k == "combo":
            ctls.append(f'<div class="fld"><label>{e(tb["l"])}</label>'
                        f'<div class="{cls}">{ic("list-filter")}<span class="val">{e(val)}</span>'
                        f'{ic("chevron-down", "i", "width:14px;height:14px")}</div></div>')
        elif k == "date":
            ctls.append(f'<div class="fld"><label>{e(tb["l"])}</label>'
                        f'<div class="{cls} date">{ic("calendar-days")}<span class="val">{e(val)}</span></div></div>')
        elif k == "check":
            on = " on" if val else ""
            ctls.append(f'<div class="fld"><label>&nbsp;</label>'
                        f'<div class="{cls}" style="border:none;background:transparent;padding:0">'
                        f'<span class="box{on}">{ic("check") if val else ""}</span>'
                        f'<span style="font-size:12px">{e(tb["l"])}</span></div></div>')
        elif k == "seg":
            opts = "".join(f'<span class="{"on" if o == val else ""}">{e(o)}</span>'
                           for o in (tb["opts"] or []))
            ctls.append(f'<div class="fld"><label>{e(tb["l"])}</label><div class="seg">{opts}</div></div>')
        else:
            ctls.append(f'<div class="fld"><label>{e(tb["l"])}</label>'
                        f'<div class="{cls}"><span class="val">{e(val)}</span></div></div>')
    return f'<div class="toolbar">{"".join(ctls)}<div class="spacer"></div></div>'


def _cell(v, col):
    """رندر یک سلولِ جدول بر اساس نوع داده."""
    if isinstance(v, tuple):
        kind = v[0]
        if kind == "chip":
            tone = CHIP_TONE.get(v[2], "c-nu")
            dot = '<span class="dt"></span>' if (len(v) < 4 or v[3]) else ""
            return f'<span class="chip {tone}">{dot}{e(v[1])}</span>'
        if kind == "who":
            av = v[3] or v[1][:1]
            tone = v[4] or ""
            return (f'<div class="who"><div class="av {tone}">{e(av)}</div>'
                    f'<div><div class="nm">{e(v[1])}</div><div class="sb">{e(v[2])}</div></div></div>')
        if kind == "num":
            return f'<span class="num">{e(v[1])}{e(v[2])}</span>'
        if kind == "date":
            return f'<span class="num" style="font-size:11.5px">{e(v[1])}</span>'
        if kind == "icon":
            return ic(v[1], "i-sm")
        return e(str(v[1]))
    return e(str(v))


def table(form, compact=False, show_bar=True, title=None):
    if not form["cols"]:
        return ""
    heads = []
    for c in form["cols"]:
        al = {"r": "", "l": "r", "c": "c"}.get(c["al"], "")
        icn = ic(c["ic"], "i-sm") + " " if c["ic"] else ""
        w = f' style="width:{c["w"]}"' if c["w"] else ""
        heads.append(f'<th class="{al}"{w}>{icn}{e(c["h"])}</th>')

    body = []
    for i, row in enumerate(form["rows"]):
        tds = []
        sel = " sel" if i == 0 else ""
        for col, val in zip(form["cols"], row):
            al = {"r": "", "l": "r", "c": "c"}.get(col["al"], "")
            tds.append(f'<td class="{al}">{_cell(val, col)}</td>')
        acts = ('<div class="rowacts">'
                f'<span class="iconbtn">{ic("eye", "i-sm")}</span>'
                f'<span class="iconbtn">{ic("pencil", "i-sm")}</span>'
                f'<span class="iconbtn">{ic("trash-2", "i-sm")}</span></div>')
        body.append(f'<tr class="{sel.strip()}">{"".join(tds)}<td class="c">{acts}</td></tr>')

    bar = ""
    if show_bar:
        bar = (f'<div class="tblbar"><div class="tt">{ic("list", "i-sm")}'
               f'{e(title or form["title"])}'
               f'<span class="cnt num">{len(form["rows"])}</span></div>'
               f'<div class="tr"><span class="btn btn-g btn-sm">{ic("list-filter", "i-sm")}ستون‌ها</span>'
               f'<span class="btn btn-g btn-sm">{ic("refresh-cw", "i-sm")}</span></div></div>')

    comp = " compact" if compact else ""
    pager = ""
    if len(form["rows"]) >= 3:
        pager = ('<div class="pager"><span class="pg dis">›</span>'
                 '<span class="pg on">۱</span><span class="pg">۲</span>'
                 '<span class="pg">۳</span><span class="pg">‹</span>'
                 '<span class="sum">نمایش ۱ تا ۲۵ از ۱,۲۴۸ مورد · ۵۰ صفحه</span></div>')

    return (f'<div class="tblwrap">{bar}'
            f'<table class="t{comp}"><thead><tr>{"".join(heads)}<th class="c"></th></tr></thead>'
            f'<tbody>{"".join(body)}</tbody></table>{pager}</div>')


def page_header(form, acts=True):
    a = ""
    if acts and form["actions"]:
        a = f'<div class="phead-acts">{"".join(btn(x) for x in form["actions"][:4])}</div>'
    return (f'<div class="phead"><div class="hi">{ic(form["icon"], "i-lg")}</div>'
            f'<div><h2>{e(form["title"])}</h2><div class="sub">{e(form["subtitle"])}</div></div>'
            f'<div class="acts">{a}</div></div>')


def btn(a):
    cls = {"p": "btn-p", "s": "btn-s", "g": "btn-g", "d": "btn-d", "ok": "btn-ok"}.get(a["st"], "btn-s")
    icon = ic(a["ic"], "i-sm") if a["ic"] else ""
    return f'<span class="btn {cls}">{icon}{e(a["l"])}</span>'


def tabs(form):
    if not form["tabs"]:
        return ""
    items = []
    for i, (t, icon, cnt) in enumerate(form["tabs"]):
        on = " on" if i == 0 else ""
        n = f'<span class="n num">{cnt}</span>' if cnt else ""
        items.append(f'<span class="tab{on}">{ic(icon, "i-sm")}{e(t)}{n}</span>')
    return f'<div class="tabs">{"".join(items)}</div>'


# ───────────────────────────────────────────────────────────────────────
#  فیلدها
# ───────────────────────────────────────────────────────────────────────

FIELD_ICON = {
    "date": "calendar-days", "time": "clock", "money": "banknote",
    "number": "calculator", "combo": "chevron-down", "lookup": "search",
    "password": "eye", "national": "fingerprint", "phone": "smartphone",
    "barcode": "barcode", "file": "paperclip", "textarea": "align-right",
}


def field(fd):
    t, v = fd["t"], fd["v"]
    lic = fd["ic"] or ""
    req = REQ if fd["req"] else ""
    lab = f'<label>{ic(lic, "i-sm") if lic else ""}{e(fd["l"])}{req}</label>'
    sp = fd.get("s", 6)

    inner = ""
    if t == "textarea":
        inner = f'<div class="inp ta"><span class="v">{e(v)}</span></div>'
    elif t == "combo":
        inner = (f'<div class="inp sel">{ic(lic or "list", "i-sm") if lic else ""}'
                 f'<span class="v">{e(v)}</span>{ic("chevron-down", "rt")}</div>')
    elif t == "lookup":
        inner = (f'<div class="inp" style="background:var(--n-50)">{ic("search", "li")}'
                 f'<span class="v">{e(v)}</span>{ic("chevron-down", "rt")}</div>')
    elif t == "check":
        on = " on" if v else ""
        inner = (f'<div class="inp chk"><span class="box{on}">{ic("check") if v else ""}</span>'
                 f'<span style="font-size:12px">{e(v if isinstance(v, str) and v else "")}</span></div>')
    elif t == "password":
        inner = f'<div class="inp"><span class="v">{e(v)}</span>{ic("eye", "rt")}</div>'
    elif t == "date":
        inner = (f'<div class="inp" style="direction:rtl">{ic("calendar-days", "li")}'
                 f'<span class="v num">{e(v)}</span></div>')
    elif t == "time":
        inner = (f'<div class="inp" style="direction:rtl">{ic("clock", "li")}'
                 f'<span class="v num">{e(v)}</span></div>')
    elif t in ("number", "money"):
        extra = " money" if t == "money" else ""
        inner = f'<div class="inp num{extra}"><span class="v">{e(v)}</span></div>'
    elif t == "file":
        inner = (f'<div class="inp" style="border-style:dashed">{ic("paperclip", "li")}'
                 f'<span class="v">{e(v)}</span>{ic("upload", "rt")}</div>')
    else:
        inner = (f'<div class="inp">{ic(lic, "li") if lic else ""}'
                 f'<span class="v{"" if v else " ph"}">{e(v) if v else "—"}</span></div>')

    hint = f'<div class="fhint">{e(fd["hint"])}</div>' if fd.get("hint") else ""
    return f'<div class="f" style="grid-column:span {sp}">{lab}{inner}{hint}</div>'


def sections(form):
    if not form["sections"]:
        return ""
    out = []
    for sec in form["sections"]:
        fl = "".join(field(fd) for fd in sec["fields"])
        cols = sec.get("cols") or 12
        note = f'<div class="note n-in mb14">{ic("info", "i-sm")}<div>{e(sec["note"])}</div></div>' if sec.get("note") else ""
        out.append(
            f'<div class="sec"><div class="sec-h"><div class="si">{ic(sec["ic"], "i-sm")}</div>'
            f'<h4>{e(sec["t"])}</h4></div>'
            f'{note}<div class="grid12" style="grid-template-columns:repeat({cols},1fr)">{fl}</div></div>'
        )
    return "".join(out)


# ───────────────────────────────────────────────────────────────────────
#  نوار کناری و قاب برنامه
# ───────────────────────────────────────────────────────────────────────

def sidebar(all_forms, current):
    groups = {}
    for f in all_forms:
        if f["arch"] in ("shell", "splash") or f["name"] == "LoginWindow":
            continue
        groups.setdefault(f["group"], []).append(f)
    parts = []
    for g, forms in groups.items():
        items = []
        for f in forms:
            on = " on" if f["name"] == current else ""
            items.append(f'<div class="side-item{on}">{ic(f["icon"])}{e(f["title"])}</div>')
        parts.append(f'<div class="side-grp">{e(g)}</div>{"".join(items)}')
    body = "".join(parts)
    return (f'<aside class="side"><div class="side-scroll">{body}</div>'
            f'<div class="side-foot">نسخه ۲.۱۲.۰ · سعید نجفی</div></aside>')


def topbar(with_side=True):
    br = ""
    if not with_side:
        br = (f'<div class="brand"><span class="logo">{ic("heart-pulse")}</span>'
              f'<span>سهند</span></div>')
    return (f'<header class="topbar">{br}'
            f'<div class="gsearch">{ic("search")}<span>جستجوی یکپارچه…</span>'
            f'<span class="kbd" style="margin-inline-start:auto;font-size:10px;'
            f'border:1px solid var(--border);padding:1px 5px;border-radius:4px">Ctrl+K</span></div>'
            f'<div class="spacer"></div>'
            f'<span class="iconbtn">{ic("sun", "i-lg")}</span>'
            f'<span class="iconbtn">{ic("languages", "i-lg")}</span>'
            f'<span class="iconbtn">{ic("bell", "i-lg")}<i class="dot"></i></span>'
            f'<div class="me"><div class="av" style="width:26px;height:26px;font-size:10px">سن</div>'
            f'<div><div class="nm">سعید نجفی</div><div class="rl">مدیر سیستم</div></div></div>'
            f'</header>')


def app_frame(inner, all_forms, current, side=True):
    sb = sidebar(all_forms, current) if side else ""
    return (f'<div class="app">{topbar(side)}'
            f'<div class="appbody">{sb}'
            f'<div class="main"><div class="page">{inner}</div></div></div></div>')


# ───────────────────────────────────────────────────────────────────────
#  پانویس مستند (ارتباطات + المان‌ها + نکته)
# ───────────────────────────────────────────────────────────────────────

def doc_parts(form):
    out = []

    # ارتباطات
    if form["rel"]:
        chips = []
        for a, b, lbl in form["rel"]:
            chips.append(f'<div class="rel"><span class="nd">{ic("layout-panel-left", "i-sm")}{e(a)}</span>'
                         f'<span class="ar">{ic("arrow-left", "i-sm")}</span>'
                         f'<span class="nd">{ic("file-text", "i-sm")}{e(b)}</span>'
                         f'<span class="fhint">— {e(lbl)}</span></div>')
        out.append(f'<div class="doc2"><h3>{ic("git-fork", "i-sm")}ارتباطات این صفحه</h3>'
                   f'<div style="padding:14px 20px" class="mini-tbl">{"".join(chips)}</div></div>')

    # المان‌ها
    rows = []
    for c in form["cols"]:
        rows.append(f'<tr><td>{e(c["h"])}</td><td><code>{e(c["b"])}</code></td>'
                    f'<td>{e(c["k"])}</td><td>{"راست‌چین" if c["al"] == "r" else ("چپ‌چین" if c["al"] == "l" else "میان‌چین")}'
                    f'{(" · عرض " + e(c["w"])) if c["w"] else ""}</td></tr>')
    for sec in form["sections"]:
        for fd in sec["fields"]:
            rows.append(f'<tr><td>{e(fd["l"])}</td><td><code>{e(fd["p"])}</code></td>'
                        f'<td>{e(fd["t"])}</td><td>{fd.get("s", 6)} از ۱۲ ستون'
                        f'{" · الزامی" if fd["req"] else ""}</td></tr>')
    for tb in form["toolbar"]:
        rows.append(f'<tr><td>{e(tb["l"])}</td><td><code>{e(tb["k"])}</code></td>'
                    f'<td>فیلتر</td><td>در نوار ابزار</td></tr>')
    for a in form["actions"]:
        rows.append(f'<tr><td>{e(a["l"])}</td><td><code>{e(a["c"])}Command</code></td>'
                    f'<td>دکمه</td><td>سبک {e(a["st"])}</td></tr>')

    if rows:
        out.append(f'<div class="doc2"><h3>{ic("list", "i-sm")}المان‌ها و اندازه‌ها</h3>'
                   f'<table class="d"><thead><tr><th>برچسب</th><th>نام/اتصال</th>'
                   f'<th>نوع</th><th>اندازه و نقش</th></tr></thead>'
                   f'<tbody>{"".join(rows)}</tbody></table></div>')

    if form["notes"]:
        out.append(f'<div class="note n-cy" style="margin-bottom:22px">'
                   f'{ic("lightbulb", "i-lg")}<div><b>نکتهٔ طراحی: </b>{e(form["notes"])}</div></div>')

    return "".join(out)
