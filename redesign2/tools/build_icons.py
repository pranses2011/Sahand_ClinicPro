#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
╔══════════════════════════════════════════════════════════════════════════╗
║  build_icons.py — ساختِ Assets/Icons.xaml از مجموعهٔ Lucide              ║
║                                                                          ║
║  ورودی:  tools/icons.json      (۱۵۴۴ آیکونِ Lucide 0.469، مجوز MIT)     ║
║          tools/icons_used.json (فهرستِ ۱۷۰ آیکونِ مصرفیِ پروژه)           ║
║  خروجی:  00-foundation/Assets/Icons.xaml                                 ║
║                                                                          ║
║  نکتهٔ فنیِ مهم:                                                          ║
║      Lucide یک مجموعهٔ «خطی» (stroke) است، نه «پرشونده» (fill). بنابراین ║
║      سبک‌های Icon.* به‌جای Fill از Stroke استفاده می‌کنند و ضخامتِ خط    ║
║      متناسب با اندازه تنظیم می‌شود تا وزنِ بصری در همهٔ اندازه‌ها ثابت    ║
║      بماند (ضخامتِ هدف ≈ ۱.۷۵ پیکسل روی صفحه).                           ║
╚══════════════════════════════════════════════════════════════════════════╝
"""

from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from svg2geom import to_path  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "tools" / "icons.json"
USED = ROOT / "tools" / "icons_used.json"
OUT = ROOT / "00-foundation" / "Assets" / "Icons.xaml"

# شبکهٔ طراحیِ Lucide
GRID = 24
# ضخامتِ خط در شبکهٔ Lucide
BASE_STROKE = 2.0
# ضخامتِ هدفِ روی صفحه (پیکسل) — وزنِ بصریِ ثابت در همهٔ اندازه‌ها
TARGET_PX = 1.75

SIZES = [
    ("XSmall", 12, "Brush.TextSecondary", "آیکونِ همراهِ متنِ ریز"),
    ("Small", 15, "Brush.Text", "آیکونِ درون‌خطی (دکمه‌ها، برچسب‌ها)"),
    ("Medium", 18, "Brush.Text", "آیکونِ استانداردِ منو و تب"),
    ("Large", 22, "Brush.Text", "آیکونِ شاخص و سربرگ"),
    ("Display", 28, "Brush.OnPrimary", "آیکونِ نمایشی روی زمینهٔ رنگی"),
]


# استثناهای نام‌گذاری: کلیدهایی که نامِ Lucide آن‌ها از قاعدهٔ
# کباب‌سازی پیروی نمی‌کند (ترکیبِ حرف و رقم).
ALIAS = {
    "Building2": "building-2",
    "Columns3": "columns-3",
    "Grid3x3": "grid-3x3",
    "Link2": "link-2",
    "Settings2": "settings-2",
    "Trash2": "trash-2",
    "Undo2": "undo-2",
}


def kebab(key: str) -> str:
    """Icon.HeartPulse → heart-pulse"""
    if key in ALIAS:
        return ALIAS[key]
    return re.sub(r"(?<!^)(?=[A-Z])", "-", key).lower()


def main() -> None:
    data = json.loads(SRC.read_text(encoding="utf-8"))
    all_icons: dict[str, str] = data["all"]
    used: dict[str, str] = json.loads(USED.read_text(encoding="utf-8"))

    # کلیدهای موردنیاز: نامِ PascalCase (بدون پیشوندِ Icon.)
    wanted = [k.split(".")[-1] for k in used]

    lines: list[str] = []
    lines.append('<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"')
    lines.append('                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">')
    lines.append("")
    lines.append("    <!--")
    lines.append("    ╔══════════════════════════════════════════════════════════════════════╗")
    lines.append("    ║  کتابخانهٔ آیکون — آیکون‌های خطیِ حرفه‌ای                              ║")
    lines.append("    ║  منبع: Lucide 0.469 (مجوز MIT) · قالب: StreamGeometry                ║")
    lines.append("    ╠══════════════════════════════════════════════════════════════════════╣")
    lines.append("    ║  نحوهٔ استفاده:                                                       ║")
    lines.append("    ║      <Path Style=\"{DynamicResource Icon.Small}\"                       ║")
    lines.append("    ║            Data=\"{DynamicResource Icon.HeartPulse}\" />               ║")
    lines.append("    ║                                                                       ║")
    lines.append("    ║  چرا Stroke و نه Fill؟                                                ║")
    lines.append("    ║      Lucide یک مجموعهٔ خطی است (fill:none; stroke:currentColor).      ║")
    lines.append("    ║      ترسیم آن با Fill شکلِ درستی نمی‌سازد؛ بنابراین سبک‌های Icon.*   ║")
    lines.append("    ║      از Stroke با سرِ گرد و اتصالِ گرد استفاده می‌کنند.                ║")
    lines.append("    ║                                                                       ║")
    lines.append("    ║  رنگ: همهٔ سبک‌ها از قلم‌موهای معناییِ تم استفاده می‌کنند؛              ║")
    lines.append("    ║  تعویضِ تمِ روشن/تاریک نیازی به تغییر این فایل ندارد.                  ║")
    lines.append("    ║                                                                       ║")
    lines.append("    ║  این فایل به‌صورت خودکار توسط tools/build_icons.py ساخته شده است؛     ║")
    lines.append("    ║  آن را دستی ویرایش نکنید.                                             ║")
    lines.append("    ╚══════════════════════════════════════════════════════════════════════╝")
    lines.append("    -->")
    lines.append("")
    lines.append("    <!-- ══════════════════════════════════════════════════════════════════")
    lines.append("         اندازه‌های استانداردِ آیکون")
    lines.append("")
    lines.append("         ضخامتِ خط به‌صورت «ضخامتِ هدف ÷ مقیاس» محاسبه می‌شود تا وزنِ")
    lines.append("         بصریِ آیکون در همهٔ اندازه‌ها یکسان بماند:")
    lines.append(f"             مقیاس = اندازه ÷ {GRID}  →  ضخامت = {TARGET_PX} ÷ مقیاس")
    lines.append("    ══════════════════════════════════════════════════════════════════ -->")

    for name, size, brush, note in SIZES:
        thickness = round(TARGET_PX * GRID / size, 2)
        lines.append(f"")
        lines.append(f"    <!-- {note} -->")
        lines.append(f'    <Style x:Key="Icon.{name}" TargetType="Path">')
        lines.append(f'        <Setter Property="Width" Value="{size}" />')
        lines.append(f'        <Setter Property="Height" Value="{size}" />')
        lines.append(f'        <Setter Property="Stretch" Value="Uniform" />')
        lines.append(f'        <Setter Property="Stroke" Value="{{DynamicResource {brush}}}" />')
        lines.append(f'        <Setter Property="StrokeThickness" Value="{thickness}" />')
        lines.append(f'        <Setter Property="StrokeStartLineCap" Value="Round" />')
        lines.append(f'        <Setter Property="StrokeEndLineCap" Value="Round" />')
        lines.append(f'        <Setter Property="StrokeLineJoin" Value="Round" />')
        lines.append(f'        <Setter Property="StrokeMiterLimit" Value="10" />')
        lines.append(f'    </Style>')

    lines.append("")
    lines.append("    <!-- ══════════════════════════════════════════════════════════════════")
    lines.append("         مسیرهای آیکون (همگی مطلق‌سازی شده‌اند)")
    lines.append("    ══════════════════════════════════════════════════════════════════ -->")

    ok, failed = 0, []
    for key in sorted(wanted):
        lucide = kebab(key)
        markup = all_icons.get(lucide)
        if markup is None:
            failed.append(key)
            continue
        d = to_path(markup)
        if not d:
            failed.append(key)
            continue
        # پاک‌سازیِ نهایی: کاراکترهای غیرمجاز در دادهٔ مسیر
        if re.search(r"[^MmLlHhVvCcSsQqTtAaZz0-9,.\-+\s]", d):
            failed.append(f"{key} (کاراکتر غیرمجاز)")
            continue
        lines.append(f'    <StreamGeometry x:Key="Icon.{key}">{d}</StreamGeometry>')
        ok += 1

    lines.append("")
    lines.append("</ResourceDictionary>")

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")

    # اعتبارسنجی
    ET.parse(OUT)
    print(f"✅ {OUT.relative_to(ROOT)}")
    print(f"   {ok} آیکون · {len(SIZES)} اندازه · {OUT.stat().st_size // 1024} کیلوبایت")
    if failed:
        print(f"   ❌ ناموفق ({len(failed)}): {failed}")
    else:
        print("   ✅ همهٔ آیکون‌ها با موفقیت تبدیل شدند")


if __name__ == "__main__":
    main()
