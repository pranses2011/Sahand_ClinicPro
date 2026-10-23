# -*- coding: utf-8 -*-
"""
╔══════════════════════════════════════════════════════════════════════════╗
║  svg2geom.py — تبدیل SVG به دادهٔ مسیرِ WPF (StreamGeometry)                ║
║                                                                          ║
║  دو کار انجام می‌دهد:                                                     ║
║    ۱. تبدیل عناصرِ هندسیِ SVG (circle / ellipse / rect / line /           ║
║       polyline / polygon) به معادلِ دستوراتِ مسیر.                        ║
║    ۲. «مطلق‌سازی» همهٔ دستوراتِ مسیر — گامی حیاتی برای مجموعه‌های        ║
║       چند‌مسیره مانند Lucide.                                             ║
║                                                                          ║
║  چرا مطلق‌سازی ضروری است؟                                                 ║
║      یک آیکونِ Lucide معمولاً چند عنصر <path> دارد. اگر دادهٔ آن‌ها را      ║
║      ساده‌سرهم کنیم، دستورِ نسبیِ ابتدایِ مسیرِ دوم (مثلاً m12 5) از       ║
║      نقطهٔ پایانِ مسیرِ قبلی محاسبه می‌شود و آیکون کج می‌شود. در هر        ║
║      عنصرِ <path> نقطهٔ جاری از (0,0) شروع می‌شود؛ بنابراین هر عنصر را     ║
║      جداگانه مطلق می‌کنیم و سپس به‌هم می‌چسبانیم.                          ║
╚══════════════════════════════════════════════════════════════════════════╝
"""

from __future__ import annotations

import re

# تعدادِ پارامترهای هر دستور
ARGC = {
    "M": 2, "L": 2, "T": 2,
    "H": 1, "V": 1,
    "C": 6, "S": 4, "Q": 4,
    "A": 7,
    "Z": 0,
}

NUM = re.compile(r"[-+]?(?:\d*\.\d+|\d+\.?)(?:[eE][-+]?\d+)?")
CMD = re.compile(r"[MmLlHhVvCcSsQqTtAaZz]")
ATTR = re.compile(r"([\w:-]+)=\"([^\"]*)\"")


def _f(v: float) -> str:
    """قالب‌بندی عدد: حذف صفرهای اضافی و حفظ خوانایی."""
    s = f"{v:.4f}".rstrip("0").rstrip(".")
    return s if s not in ("", "-0", "-0.") else "0"


# ══════════════════════════════════════════════════════════════════════════
# ۱. مطلق‌سازی دادهٔ مسیر
# ══════════════════════════════════════════════════════════════════════════

def normalize_path(d: str) -> str:
    """همهٔ دستوراتِ یک دادهٔ مسیرِ SVG را به معادلِ مطلق تبدیل می‌کند."""
    tokens = CMD.findall(d)
    if not tokens:
        return ""

    chunks = CMD.split(d)
    # chunks[0] پیشوندِ خالی است (داده با دستور شروع می‌شود)
    raw_nums = [NUM.findall(c) for c in chunks[1:]]

    cur_x = cur_y = 0.0        # نقطهٔ جاریِ مطلق
    sub_x = sub_y = 0.0        # نقطهٔ آغازِ زیرمسیر (برای Z)
    out: list[str] = []

    for cmd, nums_str in zip(tokens, raw_nums):
        rel = cmd.islower()
        up = cmd.upper()
        argc = ARGC[up]
        nums = [float(n) for n in nums_str]

        if up == "Z":
            out.append("Z")
            cur_x, cur_y = sub_x, sub_y
            continue

        # گروه‌بندی پارامترها
        groups = [nums[i:i + argc] for i in range(0, len(nums) - argc + 1, argc)]
        if not groups:
            continue

        for gi, g in enumerate(groups):
            # در SVG، تکرارِ پارامترهای M به معنای L است
            eff = up
            if up == "M" and gi > 0:
                eff = "L"

            if eff == "M":
                x, y = g
                if rel:
                    x += cur_x
                    y += cur_y
                out.append(f"M{_f(x)},{_f(y)}")
                cur_x, cur_y = x, y
                sub_x, sub_y = x, y

            elif eff == "L" or eff == "T":
                x, y = g
                if rel:
                    x += cur_x
                    y += cur_y
                out.append(f"{eff}{_f(x)},{_f(y)}")
                cur_x, cur_y = x, y

            elif eff == "H":
                x = g[0] + (cur_x if rel else 0.0)
                out.append(f"H{_f(x)}")
                cur_x = x

            elif eff == "V":
                y = g[0] + (cur_y if rel else 0.0)
                out.append(f"V{_f(y)}")
                cur_y = y

            elif eff in ("C", "S", "Q"):
                pts = list(g)
                # در این دستورات زوجِ پایانی همیشه نقطهٔ پایان است
                for i in range(0, len(pts), 2):
                    if rel:
                        pts[i] += cur_x
                        pts[i + 1] += cur_y
                out.append(eff + ",".join(_f(p) for p in pts))
                cur_x, cur_y = pts[-2], pts[-1]

            elif eff == "A":
                rx, ry, rot, laf, sf, x, y = g
                if rel:
                    x += cur_x
                    y += cur_y
                out.append(
                    f"A{_f(rx)},{_f(ry)} {_f(rot)} {int(laf)} {int(sf)} {_f(x)},{_f(y)}"
                )
                cur_x, cur_y = x, y

    return "".join(out)


# ══════════════════════════════════════════════════════════════════════════
# ۲. عناصرِ هندسی → مسیر
# ══════════════════════════════════════════════════════════════════════════

def circle(attrs: dict[str, str]) -> str:
    cx, cy = float(attrs.get("cx", 0)), float(attrs.get("cy", 0))
    r = float(attrs.get("r", 0))
    return (f"M{_f(cx - r)},{_f(cy)}"
            f"a{_f(r)},{_f(r)} 0 1 0 {_f(r * 2)},0"
            f"a{_f(r)},{_f(r)} 0 1 0 {_f(-r * 2)},0Z")


def ellipse(attrs: dict[str, str]) -> str:
    cx, cy = float(attrs.get("cx", 0)), float(attrs.get("cy", 0))
    rx, ry = float(attrs.get("rx", 0)), float(attrs.get("ry", 0))
    return (f"M{_f(cx - rx)},{_f(cy)}"
            f"a{_f(rx)},{_f(ry)} 0 1 0 {_f(rx * 2)},0"
            f"a{_f(rx)},{_f(ry)} 0 1 0 {_f(-rx * 2)},0Z")


def rect(attrs: dict[str, str]) -> str:
    x, y = float(attrs.get("x", 0)), float(attrs.get("y", 0))
    w, h = float(attrs.get("width", 0)), float(attrs.get("height", 0))
    r = float(attrs.get("rx") or attrs.get("ry") or 0)
    if r <= 0:
        return f"M{_f(x)},{_f(y)}h{_f(w)}v{_f(h)}h{_f(-w)}Z"
    r = min(r, w / 2, h / 2)
    return (f"M{_f(x + r)},{_f(y)}"
            f"h{_f(w - 2 * r)}a{_f(r)},{_f(r)} 0 0 1 {_f(r)},{_f(r)}"
            f"v{_f(h - 2 * r)}a{_f(r)},{_f(r)} 0 0 1 {_f(-r)},{_f(r)}"
            f"h{_f(-(w - 2 * r))}a{_f(r)},{_f(r)} 0 0 1 {_f(-r)},{_f(-r)}"
            f"v{_f(-(h - 2 * r))}a{_f(r)},{_f(r)} 0 0 1 {_f(r)},{_f(-r)}Z")


def line(attrs: dict[str, str]) -> str:
    x1, y1 = float(attrs.get("x1", 0)), float(attrs.get("y1", 0))
    x2, y2 = float(attrs.get("x2", 0)), float(attrs.get("y2", 0))
    return f"M{_f(x1)},{_f(y1)}L{_f(x2)},{_f(y2)}"


def _points(attrs: dict[str, str], close: bool) -> str:
    pts = re.findall(r"(-?[\d.]+)[,\s]+(-?[\d.]+)", attrs.get("points", ""))
    if not pts:
        return ""
    out = ["M" + _f(float(pts[0][0])) + "," + _f(float(pts[0][1]))]
    for p in pts[1:]:
        out.append("L" + _f(float(p[0])) + "," + _f(float(p[1])))
    if close:
        out.append("Z")
    return "".join(out)


def polyline(attrs: dict[str, str]) -> str:
    return _points(attrs, False)


def polygon(attrs: dict[str, str]) -> str:
    return _points(attrs, True)


HANDLERS = {"circle": circle, "ellipse": ellipse, "rect": rect,
            "line": line, "polyline": polyline, "polygon": polygon}

TAG_RE = re.compile(r"<(path|circle|ellipse|rect|line|polyline|polygon)\s([^>]*?)/?>")


def to_path(inner_svg: str) -> str:
    """
    تبدیل محتوای درونی یک <svg> به یک رشتهٔ مسیرِ «سراسر مطلق» و یکپارچه.

    خروجی مستقیماً در WPF به‌صورت
        <StreamGeometry x:Key="Icon.X">…</StreamGeometry>
    قابل استفاده است و با هر ترکیبی از عناصرِ SVG درست ترسیم می‌شود.
    """
    parts: list[str] = []
    for tag, attr_str in TAG_RE.findall(inner_svg):
        attrs = dict(ATTR.findall(attr_str))
        if tag == "path":
            d = attrs.get("d", "").strip()
        else:
            fn = HANDLERS.get(tag)
            d = fn(attrs) if fn else ""
        if d:
            parts.append(normalize_path(d))
    return " ".join(p for p in parts if p)


# ══════════════════════════════════════════════════════════════════════════
# خودآزمایی
# ══════════════════════════════════════════════════════════════════════════

if __name__ == "__main__":
    # آیکون arrow-right از Lucide: دو عنصر <path>، دومی با m نسبی
    sample = ('<path d="M5 12h14"/><path d="m12 5 7 7-7 7"/>')
    print("ورودی :", sample)
    print("خروجی :", to_path(sample))
    assert to_path(sample) == "M5,12H19 M12,5L19,12L12,19", to_path(sample)

    print(to_path('<circle cx="12" cy="12" r="10"/>'))
    print("✅ خودآزمایی موفق")
