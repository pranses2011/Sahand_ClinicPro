# -*- coding: utf-8 -*-
"""
╔══════════════════════════════════════════════════════════════════════════╗
║  مولدِ وایرفریم‌های اختصاصی — سهند ClinicPro  (دور ۲)                     ║
║  هر فرم بر پایهٔ کهن‌الگوی خود رندر می‌شود؛ خروجی‌ها یکنواخت نیستند.        ║
╚══════════════════════════════════════════════════════════════════════════╝

اجرا:
    python3 redesign2/tools/generate2.py
"""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from spec_core import FORMS                      # noqa: E402
from render import (ic, e, kpi_row, toolbar, table, page_header,  # noqa: E402
                    btn, tabs, sections, field, app_frame, doc_parts,
                    sidebar)

ROOT = Path(__file__).resolve().parents[2]
R2 = ROOT / "redesign2"
DIR_HTML = R2 / "01-wireframes-html"
CSS = (R2 / "tools" / "wireframe.css").read_text(encoding="utf-8")

DIRTY = '<span class="l">تغییرات ذخیره نشده</span>'

ARCH_FA = {
    "dashboard": "پیشخوان", "list-detail": "فهرست + جزئیات",
    "list-dense": "جدول متراکم", "list-cards": "شبکه کارت‌ها",
    "kanban": "ستون‌های کانبان", "form-std": "فرم گروه‌بندی‌شده",
    "form-narrow": "فرم باریک", "form-wizard": "فرم مرحله‌ای",
    "form-lines": "فرم + اقلام", "detail-tabs": "جزئیات تب‌دار",
    "detail-timeline": "جزئیات خط‌زمان", "settings": "تنظیمات",
    "dialog": "گفتگو", "viewer": "نمایشگر", "report": "گزارش",
    "pos": "پایانه فروش", "split": "دوپنلی", "shell": "قاب اصلی",
    "splash": "صفحه آغازین",
}


# ════════════════════════════════════════════════════════════════════════
#  کهن‌الگوها
# ════════════════════════════════════════════════════════════════════════

def arch_dashboard(f):
    bars = [42, 58, 35, 71, 64, 88, 52, 76, 61, 93, 48, 82]
    labels = ["فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
              "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"]
    bar_html = "".join(
        f'<div class="bar" style="height:{v}%"><span class="bv num">{v}</span></div>'
        for v in bars)
    lbl_html = "".join(f"<span>{t}</span>" for t in labels)

    donut = ('<div class="donut" style="background:conic-gradient('
             '#0D9488 0 38%, #2563EB 38% 62%, #7C3AED 62% 78%, '
             '#D97706 78% 91%, #E11D48 91% 100%)">'
             '<div class="hole"><b class="num">۳۶</b><span>نوبت امروز</span></div></div>')
    legend = "".join(
        f'<div class="lg"><span class="dot" style="background:{c}"></span>'
        f'<span class="nm">{n}</span><span class="vl num">{v}</span></div>'
        for c, n, v in [("#0D9488", "ویزیت عمومی", "۱۴"), ("#2563EB", "تخصصی", "۹"),
                        ("#7C3AED", "پیگیری", "۶"), ("#D97706", "معاینه دوره‌ای", "۵"),
                        ("#E11D48", "اورژانس", "۲")])

    queue = [("۱۲", "سارا احمدی", "دکتر موسوی", "۱۸ دقیقه", "wn"),
             ("۱۳", "محمد رضایی", "دکتر رضایی", "۵ دقیقه", "in"),
             ("۱۴", "فاطمه کریمی", "دکتر موسوی", "۲ دقیقه", "wn")]
    qrows = "".join(
        f'<tr><td class="c num">{a}</td><td>{b}</td><td>{c}</td>'
        f'<td class="r num">{d}</td>'
        f'<td class="c"><span class="chip c-{t}"><span class="dt"></span>در صف</span></td></tr>'
        for a, b, c, d, t in queue)

    events = [("ok", "ویزیت ثبت شد", "امروز ۰۹:۴۲", "ویزیت سارا احمدی — دکتر موسوی"),
              ("wn", "هشدار موجودی", "امروز ۰۹:۱۲", "امپرازول ۲۰ به زیر حد سفارش رسید"),
              ("in", "پرداخت ثبت شد", "امروز ۰۸:۵۵", "فاکتور INV-۹۵۱۲ — ۷۵,۰۰۰ تومان"),
              ("dg", "سند نامتوازن", "دیروز ۱۸:۰۲", "سند JV-۱۲۴۳ نیازمند بررسی")]
    tl = "".join(f'<div class="tl-i {c}"><div class="tt">{e(t)}</div>'
                 f'<div class="tm num">{e(m)}</div><div class="td">{e(d)}</div></div>'
                 for c, t, m, d in events)

    quick = "".join(
        f'<span class="card" style="padding:13px;display:flex;gap:10px;align-items:center">'
        f'<span class="card-h" style="padding:0;border:0"><span class="ti" style="background:var(--c-50);'
        f'color:var(--c-700);width:34px;height:34px;border-radius:11px;display:grid;'
        f'place-items:center">{ic(i, "i-sm")}</span></span>'
        f'<span style="font-size:12.5px;font-weight:600">{e(t)}</span></span>'
        for i, t in [("user-plus", "پذیرش جدید"), ("calendar-plus", "نوبت جدید"),
                     ("file-text", "نسخه جدید"), ("flask-conical", "درخواست آزمایش"),
                     ("receipt", "فاکتور جدید"), ("package", "ورود کالا")])

    return (kpi_row(f["kpis"]) +
            '<div class="two mb14">'
            f'<div class="card"><div class="card-h"><span class="ti" style="background:var(--c-50);'
            f'color:var(--c-700)">{ic("chart-column", "i-sm")}</span><h3>درآمد ماهانه (میلیون تومان)</h3>'
            f'<div class="hs"><span class="chip c-cy">۱۴۰۴</span></div></div>'
            f'<div class="card-b"><div class="bars">{bar_html}</div>'
            f'<div class="xax">{lbl_html}</div></div></div>'
            f'<div class="card"><div class="card-h"><span class="ti" style="background:var(--pp-50);'
            f'color:var(--pp-700)">{ic("chart-pie", "i-sm")}</span><h3>توزیع ویزیت‌های امروز</h3></div>'
            f'<div class="card-b" style="display:flex;gap:18px;align-items:center;'
            f'flex-wrap:wrap">{donut}<div class="legend2">{legend}</div></div></div>'
            '</div>'
            '<div class="two mb14">'
            f'<div class="tblwrap"><div class="tblbar"><div class="tt">{ic("users", "i-sm")}'
            f'صف انتظار زنده<span class="cnt num">۳</span></div>'
            f'<div class="tr"><span class="btn btn-g btn-sm" style="color:var(--c-700)">مشاهده همه</span></div></div>'
            f'<table class="t compact"><thead><tr><th class="c">نوبت</th><th>بیمار</th>'
            f'<th>پزشک</th><th class="r">انتظار</th><th class="c">وضعیت</th></tr></thead>'
            f'<tbody>{qrows}</tbody></table></div>'
            f'<div class="card"><div class="card-h"><span class="ti" style="background:var(--in-50);'
            f'color:var(--in-700)">{ic("activity", "i-sm")}</span><h3>فعالیّت‌های اخیر</h3></div>'
            f'<div class="card-b"><div class="tl">{tl}</div></div></div>'
            '</div>'
            f'<div class="card"><div class="card-h"><span class="ti" style="background:var(--c-50);'
            f'color:var(--c-700)">{ic("zap", "i-sm")}</span><h3>اقدام‌های سریع</h3></div>'
            f'<div class="card-b four">{quick}</div></div>')


def arch_list_detail(f):
    aside = ""
    if f["aside"]:
        kvs = "".join(f'<div class="kv"><span class="k">{ic(f["aside"]["icon"], "i-sm")}'
                      f'{e(k)}</span><span class="v num">{e(v)}</span></div>'
                      for k, v in f["aside"]["fields"])
        aside = (f'<div class="detail"><div class="detail-h">'
                 f'<h4>{e(f["aside"]["title"])}</h4></div>'
                 f'<div class="detail-b"><div class="kv-list">{kvs}</div>'
                 f'<div style="display:flex;gap:7px;margin-top:13px" class="stack">'
                 f'<span class="btn btn-p" style="flex:1;justify-content:center">'
                 f'{ic("eye", "i-sm")}مشاهده پرونده</span>'
                 f'<span class="btn btn-s btn-ic">{ic("pencil", "i-sm")}</span></div>'
                 f'</div></div>')
    return (page_header(f) + kpi_row(f["kpis"]) + toolbar(f) +
            f'<div class="split">{table(f)}{aside}</div>')


def arch_list_dense(f):
    return page_header(f) + kpi_row(f["kpis"]) + toolbar(f) + table(f, compact=True)


def arch_list_cards(f):
    items = []
    for row in (f["rows"] or [{} for _ in range(6)]):
        title = row[1] if len(row) > 1 else "—"
        if isinstance(title, tuple):
            title = title[1]
        meta = row[2] if len(row) > 2 else ""
        if isinstance(meta, tuple):
            meta = meta[1]
        st = row[-1] if row else ""
        chip_html = ""
        if isinstance(st, tuple) and st[0] == "chip":
            from render import CHIP_TONE
            chip_html = f'<span class="chip {CHIP_TONE.get(st[2], "c-nu")}"><span class="dt"></span>{e(st[1])}</span>'
        items.append(
            f'<div class="card"><div class="card-b" style="display:flex;gap:12px;align-items:flex-start">'
            f'<div class="av lg" style="background:linear-gradient(135deg,var(--c-100),var(--c-50));'
            f'color:var(--c-700)">{ic(f["icon"], "i-lg")}</div>'
            f'<div style="flex:1;min-width:0"><div style="font-size:13.5px;font-weight:700">{e(title)}</div>'
            f'<div style="font-size:11.5px;color:var(--text-2);margin-top:2px">{e(meta)}</div>'
            f'<div style="margin-top:9px">{chip_html}</div></div>'
            f'<span class="iconbtn">{ic("chevron-left", "i-sm")}</span></div></div>')
    return (page_header(f) + toolbar(f) +
            f'<div class="three">{"".join(items)}</div>')


def arch_kanban(f):
    # گروه‌بندی ردیف‌ها بر اساس آخرین ستونِ نشان
    cols = []
    for row in f["rows"]:
        st = row[-1]
        name = st[1] if isinstance(st, tuple) else "سایر"
        tone = st[2] if isinstance(st, tuple) else "nu"
        if not cols or cols[-1][0] != name:
            cols.append([name, tone, []])
        cols[-1][2].append(row)
    palette = {"wn": "#F59E0B", "in": "#3B82F6", "ok": "#10B981", "dg": "#F43F5E", "cy": "#0D9488"}
    out = []
    for name, tone, rows in cols:
        cards = []
        for r in rows:
            who_ = r[1] if len(r) > 1 else ""
            who_html = _cell_html(who_)
            metas = []
            for c, v in zip(f["cols"], r):
                if c["k"] in ("who", "chip"):
                    continue
                metas.append(f'<span>{ic(c["ic"] or "hash", "i-sm")}{_cell_html(v)}</span>')
            cards.append(
                f'<div class="kb-c"><div class="r1">{who_html}</div>'
                f'<div class="mt">{"".join(metas)}</div>'
                f'<div class="ft">{ic("clock", "i-sm")}<span class="fhint">هم‌اکنون</span>'
                f'<span class="chip c-cy" style="margin-inline-start:auto">'
                f'{ic("check", "i-sm")}تأیید</span></div></div>')
        out.append(f'<div class="kb-col"><div class="kb-h">'
                   f'<span class="d" style="background:{palette.get(tone, "#94A3B8")}"></span>'
                   f'{e(name)}<span class="n num">{len(rows)}</span></div>'
                   f'<div class="kb-b">{"".join(cards)}</div></div>')
    return page_header(f) + kpi_row(f["kpis"]) + toolbar(f) + f'<div class="kb">{"".join(out)}</div>'


def _cell_html(v):
    from render import _cell
    return _cell(v, None)


def _window(f, body, size="w-lg", side=False, footer=True, hero=False):
    ft = ""
    if footer:
        l = '<span class="l">تغییرات ذخیره نشده</span>' if f["sections"] else ""
        ft = (f'<div class="win-f">{l}<div class="r">'
              f'{"".join(btn(a) for a in f["actions"])}</div></div>')
    hd = ""
    if hero:
        hd = (f'<div class="win-h"><div class="wi">{ic(f["icon"], "i-lg")}</div>'
              f'<div><h3>{e(f["title"])}</h3><div class="ws">{e(f["subtitle"])}</div></div>'
              f'<div class="wx"><span></span><span></span><span></span></div></div>')
    ft_main = "".join(btn(a) for a in f["actions"])
    return (f'<div class="win {size}">{hd}<div class="win-b">{body}</div>{ft}</div>')


def arch_form_std(f):
    return _window(f, sections(f), "w-lg", hero=True)


def arch_form_narrow(f):
    hero = ""
    if f.get("hero"):
        feats = "".join(f'<div><span class="fi">{ic(i, "i-sm")}</span>{e(t)}</div>'
                        for i, t in f["hero"].get("features", []))
        hero = ('<div class="login-l"><div class="lb"><span class="logo">'
                f'{ic("heart-pulse", "i-lg")}</span><span>سهند</span></div>'
                f'<h2 style="margin-top:26px">سامانه جامع<br>مدیریت درمانگاه</h2>'
                f'<p>ورود به حساب کاربری برای دسترسی به ماژول‌های درمانگاه</p>'
                f'<div class="feats">{feats}</div></div>')
    if hero:
        body = (f'<div class="login-r"><h3>{e(f["title"])}</h3>'
                f'<div class="ls">{e(f["subtitle"])}</div>{sections(f)}'
                f'<div style="display:flex;gap:8px;margin-top:18px">'
                f'{"".join(btn(a) for a in f["actions"])}</div></div>')
        return f'<div class="win" style="max-width:900px"><div class="login">{hero}{body}</div></div>'
    return _window(f, sections(f), "w-md", hero=True)


def arch_form_wizard(f):
    names = f.get("steps") or ["مرحله ۱", "مرحله ۲", "مرحله ۳"]
    parts = []
    for i, n in enumerate(names):
        cls = "done" if i == 0 else ("on" if i == 1 else "")
        mark = ic("check", "i-sm") if i == 0 else f'<span class="num">{i + 1}</span>'
        bar = "" if i == len(names) - 1 else '<span class="bar2"></span>'
        parts.append(f'<div class="step {cls}"><span class="sn">{mark}</span>'
                     f'<span class="st">{e(n)}</span>{bar}</div>')
    steps = f'<div class="steps">{"".join(parts)}</div>'
    return _window(f, steps + sections(f), "w-lg", hero=True)


def arch_form_lines(f):
    lines = [
        ("آموکسی‌سیلین ۵۰۰", "هر ۸ ساعت · ۷ روز", "۲۱ عدد", "۴۵,۰۰۰"),
        ("امپرازول ۲۰", "هر شب · ۱۴ روز", "۱۴ عدد", "۳۲,۰۰۰"),
        ("استامینوفن ۵۰۰", "در صورت درد", "۱۰ عدد", "۱۸,۰۰۰"),
    ]
    rows = "".join(
        f'<div class="li"><div class="lii">{ic("pill", "i-sm")}</div>'
        f'<div style="flex:1;min-width:0"><div class="ln">{e(a)}</div>'
        f'<div class="lm">{e(b)} · {e(c)}</div></div>'
        f'<div class="lp num">{e(d)}</div>'
        f'<span class="iconbtn">{ic("trash-2", "i-sm")}</span></div>'
        for a, b, c, d in lines)
    tot = f.get("total") or {}
    totals = "".join(
        f'<div class="tr2">{e(k)}<span class="v">{e(v)}</span></div>'
        for k, v in [("تعداد اقلام", tot.get("items", "—")),
                     ("جمع کل", tot.get("subtotal", "—")),
                     ("تخفیف", tot.get("discount", "—"))])
    totals += (f'<div class="tr2 big">قابل پرداخت<span class="v">{e(tot.get("total", "—"))}</span></div>')
    addrow = ('<div class="inp" style="border-style:dashed;margin-top:4px">'
              f'{ic("plus", "li")}<span class="ph">افزودن قلم…</span></div>')
    return _window(
        f,
        sections(f) +
        '<div class="two mt14">'
        f'<div class="cart"><div class="cart-h">{ic("list", "i-sm")}اقلام</div>'
        f'<div class="cart-b">{rows}{addrow}</div></div>'
        f'<div><div class="card"><div class="card-h">{ic("calculator", "i-sm")}'
        f'<h3>خلاصه</h3></div><div class="totals" style="border-top:0">{totals}'
        f'<div style="display:flex;gap:7px;margin-top:10px">'
        f'<span class="btn btn-s" style="flex:1;justify-content:center">'
        f'{ic("calculator", "i-sm")}محاسبه</span></div></div></div></div>'
        '</div>', "w-lg", hero=True)


def _hero(f):
    h = f.get("hero")
    if not h:
        return ""
    chips = "".join(f'<span class="chip c-{t}"><span class="dt"></span>{e(c)}</span>'
                    for c, t in h.get("chips", []))
    stats = "".join(f'<div style="text-align:center"><div style="font-size:17px;font-weight:800" '
                    f'class="num">{e(v)}</div><div style="font-size:10.5px;color:var(--text-3)">'
                    f'{e(k)}</div></div>' for k, v in h.get("stats", []))
    kvs = "".join(f'<div class="kv"><span class="k">{e(k)}</span>'
                  f'<span class="v num">{e(v)}</span></div>' for k, v in h.get("fields", []))
    return (f'<div class="card mb14"><div class="card-b" style="display:flex;gap:18px;'
            f'align-items:flex-start;flex-wrap:wrap">'
            f'<div class="av lg {h.get("tone", "")}">{e(h.get("av", ""))}</div>'
            f'<div style="flex:1;min-width:220px"><div style="display:flex;gap:8px;'
            f'align-items:center;flex-wrap:wrap"><h2 style="font-size:19px;font-weight:800">'
            f'{e(h.get("name", ""))}</h2>{chips}</div>'
            f'<div style="font-size:12px;color:var(--text-2);margin-top:3px">{e(h.get("sub", ""))}</div>'
            f'<div style="display:flex;gap:22px;margin-top:13px">{stats}</div></div>'
            f'<div style="flex:1;min-width:240px"><div class="kv-list">{kvs}</div></div>'
            f'</div></div>')


def arch_detail_tabs(f):
    return page_header(f) + _hero(f) + tabs(f) + sections(f) + table(f)


def arch_detail_timeline(f):
    h = f.get("hero") or {}
    events = [("in", "نوبت ثبت شد", "۱۴۰۴/۰۷/۲۰ ۰۹:۳۰", "نوبت برای دکتر موسوی — ساعت ۱۰:۳۰"),
              ("ok", "پذیرش انجام شد", "۱۴۰۴/۰۷/۱۲ ۰۹:۰۵", "پذیرش شماره ۱۲ — ویزیت عمومی"),
              ("ok", "ویزیت ثبت شد", "۱۴۰۴/۰۷/۱۲ ۰۹:۴۲", "تشخیص K21.9 — دکتر موسوی"),
              ("ok", "نسخه صادر شد", "۱۴۰۴/۰۷/۱۲ ۰۹:۴۵", "۳ قلم دارو — تحویل از داروخانه"),
              ("in", "آزمایش درخواست شد", "۱۴۰۴/۰۷/۱۲ ۰۹:۵۰", "CBC، قند ناشتا، چربی خون"),
              ("ok", "نتیجه آزمایش آماده", "۱۴۰۴/۰۷/۱۳ ۱۱:۲۰", "۱۸ پارامتر — ارسال پیامک به بیمار"),
              ("wn", "تصویربرداری", "۱۴۰۴/۰۷/۱۴ ۱۰:۱۵", "رادیوگرافی قفسه سینه — در انتظار گزارش"),
              ("ok", "صورتحساب صادر شد", "۱۴۰۴/۰۷/۱۴ ۱۰:۳۰", "فاکتور INV-۹۵۱۲ — ۲۵۰,۰۰۰ تومان")]
    tl = "".join(f'<div class="tl-i {c}"><div class="tt">{e(t)}</div>'
                 f'<div class="tm num">{e(m)}</div><div class="td">{e(d)}</div></div>'
                 for c, t, m, d in events)
    return (page_header(f) + _hero(f) + tabs(f) +
            f'<div class="card"><div class="card-b"><div class="tl">{tl}</div></div></div>')


def arch_settings(f):
    navs = [("عمومی", "sliders-horizontal", True), ("مالی", "wallet", False),
            ("ظاهر و زبان", "palette", False), ("اعلان‌ها", "bell", False),
            ("پشتیبان‌گیری", "database-backup", False), ("لایسنس", "shield-check", False),
            ("چاپ", "printer", False), ("سامانه‌ها", "link-2", False)]
    nav = "".join(f'<div class="i{" on" if on else ""}">{ic(i, "i-sm")}{e(t)}</div>'
                  for t, i, on in navs)
    return (page_header(f) +
            f'<div class="setwrap"><div class="setnav"><div class="g">دسته‌بندی</div>{nav}</div>'
            f'<div>{sections(f)}'
            f'<div class="win-f" style="border:1px solid var(--border);border-radius:var(--r-lg);'
            f'margin-top:6px">{DIRTY if f["sections"] else ""}'
            f'<div class="r">{"".join(btn(a) for a in f["actions"])}</div></div></div></div>')


def arch_dialog(f):
    body = (f'<div style="text-align:center;padding:8px 0 4px">'
            f'<div class="empty" style="padding:16px 0"><div class="ei" style="width:56px;'
            f'height:56px;border-radius:18px">{ic(f["icon"], "i-lg")}</div>'
            f'<h5>{e(f["title"])}</h5><p>{e(f["subtitle"])}</p></div>'
            f'{sections(f)}</div>')
    if f["notes"]:
        body += f'<div class="note n-in mt14">{ic("info", "i-sm")}<div>{e(f["notes"])}</div></div>'
    return _window(f, body, "w-sm", hero=True)


def arch_viewer(f):
    rail = ["move", "zoom-in", "hand", "contrast", "ruler", "pencil", "crop", "download"]
    vr = "".join(f'<span class="{"on" if i == 0 else ""}">{ic(n, "i-sm")}</span>'
                 for i, n in enumerate(rail))
    return (f'<div class="win w-lg" style="background:var(--n-900);border-color:#1E293B">'
            f'<div class="win-h" style="background:#0F172A;border-bottom:1px solid #1E293B">'
            f'<div class="wi">{ic(f["icon"], "i-sm")}</div><div><h3>{e(f["title"])}</h3>'
            f'<div class="ws">{e(f["subtitle"])}</div></div>'
            f'<div class="wx"><span></span><span></span><span></span></div></div>'
            f'<div class="viewer"><nav class="vrail">{vr}</nav>'
            f'<div class="vstage"><div class="vimg">'
            f'<div style="text-align:center;color:#475569">'
            f'{ic("scan-line", "i-lg", "width:44px;height:44px")}'
            f'<div style="font-size:11px;margin-top:8px">تصویر پزشکی</div></div></div>'
            f'<div class="vhud tl">سارا احمدی · P-۱۰۲۴<br>CR · قفسه سینه · PA</div>'
            f'<div class="vhud bl">W ۱۵۰۰ · L −۶۰۰<br>قاب ۲ از ۶</div>'
            f'<div class="vhud br">۱۴۰۴/۰۷/۱۸ ۱۰:۲۲<br>۱۰۲۴×۱۰۲۴ · ۱۶ بیت</div>'
            f'</div></div></div>')


def arch_report(f):
    steps = ""
    if f.get("steps"):
        parts = []
        for i, n in enumerate(f["steps"]):
            cls = "done" if i < 2 else ("on" if i == 2 else "")
            mark = ic("check", "i-sm") if i < 2 else f'<span class="num">{i + 1}</span>'
            bar = "" if i == len(f["steps"]) - 1 else '<span class="bar2"></span>'
            parts.append(f'<div class="step {cls}"><span class="sn">{mark}</span>'
                         f'<span class="st">{e(n)}</span>{bar}</div>')
        steps = f'<div class="steps">{"".join(parts)}</div>'
    paper = ('<div style="background:#fff;border:1px solid var(--border);border-radius:8px;'
             'box-shadow:var(--sh-3);padding:26px 30px;min-height:420px">'
             '<div style="text-align:center;border-bottom:2px solid var(--c-700);padding-bottom:12px;'
             'margin-bottom:16px"><div style="font-size:16px;font-weight:800">درمانگاه تخصصی سهند</div>'
             '<div style="font-size:11.5px;color:var(--text-2)">گزارش ویزیت‌ها — مهر ۱۴۰۴</div></div>'
             '<table style="width:100%;font-size:11.5px;border-collapse:collapse">'
             '<thead><tr style="background:var(--n-100)"><th style="padding:7px;text-align:right">پزشک</th>'
             '<th style="padding:7px;text-align:right">ویزیت</th>'
             '<th style="padding:7px;text-align:right">درآمد</th></tr></thead><tbody>'
             '<tr><td style="padding:6px;border-bottom:1px solid var(--border-soft)">دکتر موسوی</td>'
             '<td style="padding:6px;border-bottom:1px solid var(--border-soft)" class="num">۱۲۴</td>'
             '<td style="padding:6px;border-bottom:1px solid var(--border-soft)" class="num">۳۱,۰۰۰,۰۰۰</td></tr>'
             '<tr><td style="padding:6px;border-bottom:1px solid var(--border-soft)">دکتر رضایی</td>'
             '<td style="padding:6px;border-bottom:1px solid var(--border-soft)" class="num">۸۷</td>'
             '<td style="padding:6px;border-bottom:1px solid var(--border-soft)" class="num">۳۴,۸۰۰,۰۰۰</td></tr>'
             '<tr><td style="padding:6px;border-bottom:1px solid var(--border-soft)">دکتر کریمی</td>'
             '<td style="padding:6px;border-bottom:1px solid var(--border-soft)" class="num">۶۲</td>'
             '<td style="padding:6px;border-bottom:1px solid var(--border-soft)" class="num">۱۸,۴۰۰,۰۰۰</td></tr>'
             '</tbody></table>'
             '<div style="margin-top:18px;padding-top:12px;border-top:1px dashed var(--n-300);'
             'display:flex;font-size:12px;font-weight:700"><span>جمع کل</span>'
             '<span style="margin-inline-start:auto" class="num">۲۷۳ · ۸۴,۲۰۰,۰۰۰</span></div>'
             '</div>')
    side = (f'<div class="card"><div class="card-h">{ic("sliders-horizontal", "i-sm")}'
            f'<h3>پارامترها</h3></div><div class="card-b">{sections(f)}'
            f'<div style="display:flex;gap:7px" class="stack">'
            f'{"".join(btn(a) for a in f["actions"])}</div></div></div>')
    return page_header(f) + steps + f'<div class="split" style="grid-template-columns:320px 1fr">{side}{paper}</div>'


def arch_pos(f):
    lines = [("آموکسی‌سیلین ۵۰۰ کپسول", "بچ A-۲۲ · انقضا ۱۴۰۵/۰۲", "۲ بسته", "۹۰,۰۰۰"),
             ("امپرازول ۲۰ کپسول", "بچ B-۱۴ · انقضا ۱۴۰۴/۱۱", "۱ بسته", "۳۲,۰۰۰"),
             ("استامینوفن ۵۰۰", "بچ S-۰۵ · انقضا ۱۴۰۵/۰۹", "۲ بسته", "۳۶,۰۰۰")]
    rows = "".join(
        f'<div class="li"><div class="lii">{ic("pill", "i-sm")}</div>'
        f'<div style="flex:1;min-width:0"><div class="ln">{e(a)}</div>'
        f'<div class="lm">{e(b)}</div><div class="lm">{e(c)}</div></div>'
        f'<div class="lp num">{e(d)}</div>'
        f'<span class="iconbtn">{ic("trash-2", "i-sm")}</span></div>'
        for a, b, c, d in lines)
    tot = (f.get("total") or {})
    totals = "".join(
        f'<div class="tr2">{e(k)}<span class="v">{e(v)}</span></div>'
        for k, v in [("جمع اقلام", "۵"), ("جمع کل", "۱۵۸,۰۰۰"),
                     ("سهم بیمه", "۱۱۰,۶۰۰")])
    totals += '<div class="tr2 big">قابل پرداخت<span class="v">۴۷,۴۰۰</span></div>'
    return (page_header(f) + kpi_row(f["kpis"]) + toolbar(f) +
            '<div class="pos">'
            f'{table(f)}'
            f'<div class="cart"><div class="cart-h">{ic("shopping-cart", "i-sm")}سبد تحویل'
            f'<span class="chip c-cy" style="margin-inline-start:auto">FEFO</span></div>'
            f'<div class="cart-b">{rows}</div>'
            f'<div class="totals">{totals}'
            f'<div style="display:flex;gap:7px;margin-top:10px">'
            f'<span class="btn btn-p" style="flex:1;justify-content:center">'
            f'{ic("package-check", "i-sm")}تحویل نهایی</span>'
            f'<span class="btn btn-s btn-ic">{ic("printer", "i-sm")}</span></div></div></div>'
            '</div>')


def arch_split(f):
    aside = ""
    if f["aside"]:
        kvs = "".join(f'<div class="kv"><span class="k">{ic(f["aside"]["icon"], "i-sm")}'
                      f'{e(k)}</span><span class="v num">{e(v)}</span></div>'
                      for k, v in f["aside"]["fields"])
        aside = (f'<div class="card"><div class="card-h">'
                 f'<span class="ti" style="background:var(--c-50);color:var(--c-700)">'
                 f'{ic(f["aside"]["icon"], "i-sm")}</span><h3>{e(f["aside"]["title"])}</h3></div>'
                 f'<div class="card-b"><div class="kv-list">{kvs}</div></div></div>')
    return (page_header(f) + kpi_row(f["kpis"]) + toolbar(f) +
            f'<div class="split" style="grid-template-columns:1fr 300px">{table(f)}{aside}</div>')


def arch_shell(f):
    inner = (
        '<div class="phead"><div class="hi">' + ic("layout-dashboard", "i-lg") +
        '</div><div><h2>پیشخوان</h2><div class="sub">نمای کلی وضعیت امروز درمانگاه</div></div>'
        '<div class="acts"><span class="btn btn-p">' + ic("user-plus", "i-sm") +
        'پذیرش جدید</span></div></div>' +
        kpi_row([{"ic": "users", "l": "بیماران امروز", "v": "۴۸", "tr": "+۱۲٪", "dir": "up", "ac": "teal"},
                 {"ic": "calendar-check", "l": "نوبت‌های امروز", "v": "۳۶", "tr": "+۸٪", "dir": "up", "ac": "blue"},
                 {"ic": "banknote", "l": "درآمد امروز", "v": "۲۸,۴۵۰,۰۰۰", "tr": "+۲۳٪", "dir": "up", "ac": "green"},
                 {"ic": "timer", "l": "میانگین انتظار", "v": "۱۴ دقیقه", "tr": "−۶٪", "dir": "down", "ac": "amber"}]) +
        '<div class="note n-cy mt14">' + ic("layout-panel-left", "i-lg") +
        '<div><b>قاب اصلی برنامه: </b>نوار کناری با ۱۵ گروه و شمارندهٔ هر صفحه؛ '
        'نوار بالا با جستجوی یکپارچه (Ctrl+K)، انتخاب‌گر تم و زبان، اعلان‌ها و منوی کاربر. '
        'ناحیهٔ محتوا صفحهٔ جاری را نمایش می‌دهد.</div></div>')
    return app_frame(inner, FORMS, f["name"], side=True)


def arch_splash(f):
    return ('<div class="login" style="min-height:380px"><div class="login-l" '
            'style="padding:60px 40px;align-items:center;text-align:center">'
            '<div class="lb" style="justify-content:center"><span class="logo" '
            'style="width:64px;height:64px;border-radius:20px">'
            + ic("heart-pulse", "i-lg", "width:34px;height:34px") +
            '</span></div><h2 style="margin-top:26px;font-size:28px">سهند</h2>'
            '<p style="margin-top:6px">سامانه جامع مدیریت درمانگاه</p>'
            '<div style="margin-top:38px;width:220px">'
            '<div style="height:4px;border-radius:4px;background:rgba(255,255,255,.25);overflow:hidden">'
            '<div style="width:62%;height:100%;background:#fff;border-radius:4px"></div></div>'
            '<div style="margin-top:10px;font-size:11.5px;opacity:.85">در حال بارگذاری تنظیمات…</div>'
            '</div></div></div>')


ARCH = {
    "dashboard": arch_dashboard, "list-detail": arch_list_detail,
    "list-dense": arch_list_dense, "list-cards": arch_list_cards,
    "kanban": arch_kanban, "form-std": arch_form_std,
    "form-narrow": arch_form_narrow, "form-wizard": arch_form_wizard,
    "form-lines": arch_form_lines, "detail-tabs": arch_detail_tabs,
    "detail-timeline": arch_detail_timeline, "settings": arch_settings,
    "dialog": arch_dialog, "viewer": arch_viewer, "report": arch_report,
    "pos": arch_pos, "split": arch_split, "shell": arch_shell,
    "splash": arch_splash,
}

# کهن‌الگوهایی که در قابِ برنامه نمایش داده می‌شوند
IN_APP = {"dashboard", "list-detail", "list-dense", "list-cards", "kanban",
          "detail-tabs", "detail-timeline", "settings", "split", "pos",
          "report", "shell"}


# ════════════════════════════════════════════════════════════════════════
#  تولید سند HTML
# ════════════════════════════════════════════════════════════════════════

def build_html(f):
    arch = f["arch"]
    renderer = ARCH.get(arch, arch_list_dense)
    body = renderer(f)

    if arch in IN_APP:
        stage = app_frame(body, FORMS, f["name"], side=(arch != "shell"))
    else:
        stage = body

    tags = "".join(
        f'<span class="tag">{ic(n, "i-sm")}{e(v)}</span>'
        for n, v in [("file-text", f["name"]), ("layout-panel-left", ARCH_FA.get(arch, arch)),
                     ("folder", f["group"]), ("palette", "تم‌پذیر"),
                     ("languages", "دو زبانه")])

    return f"""<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>وایرفریم {e(f['title'])} — سهند ClinicPro</title>
<style>
{CSS}
</style>
</head>
<body>
<div class="doc">

  <div class="doc-head">
    <h1><span class="hi">{ic(f['icon'], 'i-lg')}</span>{e(f['title'])}</h1>
    <div class="sub">{e(f['subtitle'])}</div>
    <div class="meta">{tags}</div>
  </div>

  <div class="sheet">
    <div class="sheet-cap">
      <span class="t">{ic("eye", "i-sm")}نمای بصری پیشنهادی</span>
      <span class="hint">وایرفریم با داده‌های نمونه · کهن‌الگوی «{e(ARCH_FA.get(arch, arch))}»</span>
    </div>
    {stage}
  </div>

  {doc_parts(f)}

  <div class="doc-foot">📅 ۱۴۰۴/۰۷/۱۹ · 🔖 نسخه ۲ · مولد: redesign2/tools/generate2.py</div>

</div>
</body>
</html>
"""


def build_index():
    groups = {}
    for f in FORMS:
        groups.setdefault(f["group"], []).append(f)
    secs = []
    for g, forms in groups.items():
        cards = "".join(
            f'<a class="card" href="{e(x["name"])}.html">'
            f'<div class="ci">{ic(x["icon"], "i-lg")}</div>'
            f'<div class="ct">{e(x["title"])}</div>'
            f'<div class="cs">{e(x["subtitle"])}</div>'
            f'<div class="cm"><span class="chip c-cy">{e(ARCH_FA.get(x["arch"], x["arch"]))}</span>'
            f'<span class="cod">{e(x["name"])}</span></div></a>'
            for x in forms)
        secs.append(f'<section class="grp"><h2 class="gh">{ic("folder", "i-sm")}{e(g)}'
                    f'<span class="gc num">{len(forms)}</span></h2>'
                    f'<div class="grid">{cards}</div></section>')
    extra = """
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(258px,1fr));gap:14px}
.card{background:var(--surface);border:1px solid var(--border);border-radius:var(--r-lg);
      padding:16px;text-decoration:none;color:inherit;display:block;box-shadow:var(--sh-1);
      transition:transform .16s,box-shadow .16s,border-color .16s}
.card:hover{transform:translateY(-4px);box-shadow:var(--sh-3);border-color:var(--c-500)}
.ci{color:var(--c-700);margin-bottom:9px}
.ct{font-weight:700;font-size:14.5px}
.cs{font-size:12px;color:var(--text-2);min-height:36px;margin-top:3px;line-height:1.7}
.cm{display:flex;align-items:center;justify-content:space-between;gap:8px;margin-top:11px;
    padding-top:11px;border-top:1px solid var(--border-soft)}
.cod{font-size:10.5px;color:var(--text-3);font-family:ui-monospace,monospace;direction:ltr;
     overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.gh{font-size:18px;color:var(--c-800);margin-bottom:15px;display:flex;align-items:center;gap:8px;
    border-bottom:2px solid var(--c-100);padding-bottom:9px}
.gc{background:var(--c-100);color:var(--c-800);font-size:11px;padding:1px 10px;
    border-radius:999px;font-weight:700;margin-inline-start:auto}
.grp{margin-bottom:34px}
"""
    return f"""<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>گالری وایرفریم‌ها — سهند ClinicPro (نسخه ۲)</title>
<style>
{CSS}
{extra}
</style>
</head>
<body>
<div class="doc">
  <div class="doc-head">
    <h1><span class="hi">{ic("layout-dashboard", "i-lg")}</span>گالری وایرفریم‌ها — سهند ClinicPro</h1>
    <div class="sub">طراحیِ اختصاصی برای هر فرم · ۱۸ کهن‌الگو · داده‌های نمونه · آیکون‌های حرفه‌ای</div>
    <div class="meta">
      <span class="tag">{ic("file-text", "i-sm")}{len(FORMS)} وایرفریم</span>
      <span class="tag">{ic("folder", "i-sm")}{len(groups)} گروه</span>
      <span class="tag">{ic("layout-panel-left", "i-sm")}۱۸ کهن‌الگو</span>
      <span class="tag">{ic("palette", "i-sm")}تم روشن و تاریک</span>
      <span class="tag">{ic("languages", "i-sm")}fa-IR · en-US</span>
    </div>
  </div>
  {''.join(secs)}
  <div class="doc-foot">📅 ۱۴۰۴/۰۷/۱۹ · 🔖 نسخه ۲ · مولد: redesign2/tools/generate2.py</div>
</div>
</body>
</html>
"""


def main():
    DIR_HTML.mkdir(parents=True, exist_ok=True)
    print(f"🎨 در حال تولید {len(FORMS)} وایرفریم اختصاصی…")
    for f in FORMS:
        (DIR_HTML / f"{f['name']}.html").write_text(build_html(f), encoding="utf-8")
    (DIR_HTML / "index.html").write_text(build_index(), encoding="utf-8")
    print(f"✅ {len(FORMS)} وایرفریم + گالری در 01-wireframes-html/")


if __name__ == "__main__":
    main()
