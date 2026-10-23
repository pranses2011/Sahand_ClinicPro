#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
ساختِ یک صفحهٔ نمایشِ HTML برای کتابخانهٔ آیکون.

چرا؟  محیطِ توسعه فاقدِ SDK دات‌نت است، بنابراین خروجیِ WPF قابلِ کامپایل
      نیست. این صفحه همان دادهٔ مسیر (StreamGeometry) را با SVGِ خطی ترسیم
      می‌کند تا صحتِ تبدیل به‌صورت چشمی بررسی شود.

خروجی: 00-foundation/Assets/Icons-Preview.html
"""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "00-foundation" / "Assets" / "Icons.xaml"
OUT = ROOT / "00-foundation" / "Assets" / "Icons-Preview.html"

XK = "{http://schemas.microsoft.com/winfx/2006/xaml}Key"

SIZES = [("Display", 28), ("Large", 22), ("Medium", 18), ("Small", 15), ("XSmall", 12)]


def parse(path: Path):
    import xml.etree.ElementTree as ET

    root = ET.parse(path).getroot()
    icons = []
    sizes = []
    for e in root:
        key = e.get(XK) or ""
        if e.tag.endswith("StreamGeometry"):
            icons.append((key, (e.text or "").strip()))
        elif e.tag.endswith("Style") and key.startswith("Icon."):
            body = "".join(ET.tostring(c, encoding="unicode") for c in e)
            w = re.search(r'Width"\s+Value="([\d.]+)"', body)
            t = re.search(r'StrokeThickness"\s+Value="([\d.]+)"', body)
            if w and t:
                sizes.append((key.split(".", 1)[1], float(w.group(1)), float(t.group(1))))
    return icons, sizes


def main() -> None:
    icons, sizes = parse(SRC)

    # تبدیلِ دادهٔ WPF به SVG: کمان‌ها در هر دو یکسان‌اند، فقط جداکننده‌ها فرق دارد
    def to_svg(d: str) -> str:
        return d.strip()

    cards = []
    for key, d in icons:
        name = key.split(".", 1)[1]
        row = "".join(
            f'<span class="sz"><svg width="{w}" height="{w}" viewBox="0 0 24 24" '
            f'fill="none" stroke="currentColor" stroke-width="{t}" '
            f'stroke-linecap="round" stroke-linejoin="round"><path d="{to_svg(d)}"/></svg>'
            f"<i>{w:g}</i></span>"
            for _, w, t in sizes
        )
        cards.append(
            f'<div class="card" title="{key}">{row}<b>{name}</b></div>'
        )

    html = f"""<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>کتابخانهٔ آیکون — {len(icons)} آیکون</title>
<style>
  :root {{
    --bg:#F8FAFC; --card:#FFFFFF; --line:#E2E8F0;
    --ink:#0F172A; --muted:#64748B; --accent:#0F766E;
  }}
  *{{box-sizing:border-box}}
  body{{margin:0;padding:32px;background:var(--bg);color:var(--ink);
        font-family:"Vazirmatn","Segoe UI",Tahoma,sans-serif}}
  header{{max-width:1400px;margin:0 auto 24px}}
  h1{{font-size:22px;margin:0 0 6px}}
  p{{margin:0;color:var(--muted);font-size:13px;line-height:1.9}}
  code{{background:#EEF2F6;padding:1px 6px;border-radius:4px;font-size:12px}}
  .grid{{max-width:1400px;margin:0 auto;display:grid;
         grid-template-columns:repeat(auto-fill,minmax(190px,1fr));gap:10px}}
  .card{{background:var(--card);border:1px solid var(--line);border-radius:10px;
         padding:12px;display:flex;align-items:center;gap:10px}}
  .card b{{font-weight:500;font-size:11.5px;color:var(--muted);
           margin-inline-start:auto;direction:ltr}}
  .sz{{display:inline-flex;flex-direction:column;align-items:center;gap:3px}}
  .sz svg{{color:var(--accent)}}
  .sz i{{font-style:normal;font-size:9px;color:#94A3B8}}
</style>
</head>
<body>
<header>
  <h1>کتابخانهٔ آیکون — {len(icons)} آیکونِ خطی</h1>
  <p>
    منبع: Lucide ۰.۴۶۹ (MIT) · خروجی: <code>Assets/Icons.xaml</code> ·
    هر آیکون در اندازه‌های {", ".join(str(int(w)) for _, w, _ in sizes)} پیکسل نمایش داده شده است.<br>
    نحوهٔ استفاده در WPF:
    <code>&lt;Path Style="{{DynamicResource Icon.Small}}" Data="{{DynamicResource Icon.HeartPulse}}" /&gt;</code>
  </p>
</header>
<div class="grid">
{chr(10).join(cards)}
</div>
</body>
</html>
"""
    OUT.write_text(html, encoding="utf-8")
    print(f"✅ {OUT.relative_to(ROOT)} — {len(icons)} آیکون × {len(sizes)} اندازه")


if __name__ == "__main__":
    main()
