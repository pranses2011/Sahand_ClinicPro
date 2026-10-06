#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🔧 فاز A چرخه ۱۴ (v2.0.1) — تعویض دقیق ۱۰۱ ارجاع منبع نامعتبر XAML با کلیدهای واقعی
پکیج MaterialDesignThemes 5.3.2 + دو کلید محلی تایپی‌شده
همه جایگزینی‌ها با مرز واژه (negative lookahead) تا پیشوند کلیدهای بلندتر آسیب نبیند.
"""
import re
import pathlib
from collections import defaultdict

SRC = pathlib.Path("/home/z/my-project/Sahand_ClinicPro/src")

# (الگو، جانشین، شرح) — الگوها با lookahead منفی برای مرز کلید
REPLACEMENTS = [
    (r'MaterialDesignFilledButton(?![\w.-])', 'MaterialDesignRaisedButton',
     'دکمه پررنگ MD3 → معادل کلاسیک Raised'),
    (r'MaterialDesign\.Brush\.Error(?![\w.-])', 'MaterialDesign.Brush.ValidationError',
     'براش خطا → ValidationError موجود'),
    (r'MaterialDesign\.Brush\.Background\.Variant(?![\w.-])', 'MaterialDesign.Brush.Separator.Background',
     'سطح/حاشیه واریانت → براش جداکننده'),
    (r'MaterialDesign\.Brush\.Primary\.Container(?![\w.-])', 'MaterialDesign.Brush.Primary.Light',
     'کانتینر اصلی → روشنِ رنگ اصلی'),
    (r'MaterialDesign\.Card\.Background(?![\w.-])', 'MaterialDesign.Brush.Card.Background',
     'براش کارت → کلید صحیح نقطه‌دار'),
    (r'MaterialDesignChipOutlineButton(?![\w.-])', 'MaterialDesignOutlinedButton',
     'چیپ طرح‌دار → دکمه Outlined موجود'),
    (r'MaterialDesignFlatMidButton(?![\w.-])', 'MaterialDesignFlatMidBgButton',
     'فلت میانی → FlatMidBgButton موجود (استایل روزهای تقویم خود پکیج)'),
    (r'MaterialDesignFlatPrimaryButton(?![\w.-])', 'MaterialDesignFlatButton',
     'فلت اصلی → دکمه فلت (متن با رنگ اصلی)'),
    (r'\{StaticResource BooleanToVisibilityConverter\}', '{StaticResource BoolToVis}',
     'کلید تایپی مبدل دیدپذیری → BoolToVis تعریف‌شده در App.xaml'),
    (r'\{StaticResource InverseBool\}', '{StaticResource InvertBool}',
     'کلید تایپی مبدل معکوس → InvertBool تعریف‌شده در App.xaml'),
]

changed = defaultdict(int)
total = 0
for f in sorted(SRC.rglob("*.xaml")):
    txt = orig = f.read_text(encoding="utf-8-sig")
    n_file = 0
    for pat, rep, _ in REPLACEMENTS:
        txt, n = re.subn(pat, rep, txt)
        n_file += n
    if n_file:
        # حفظ BOM در صورت وجود
        bom = orig.startswith('\ufeff')
        body = ('\ufeff' + txt) if bom and not txt.startswith('\ufeff') else txt
        f.write_text(body, encoding="utf-8")
        changed[str(f.relative_to(SRC))] = n_file
        total += n_file

print(f"✅ {total} ارجاع در {len(changed)} فایل اصلاح شد:")
for f, n in sorted(changed.items(), key=lambda kv: -kv[1]):
    print(f"   {n:3d}× {f}")

# ── تأیید: هیچ ارجاع شبح‌مانندی نمانده باشد ──
PHANTOM = [r'MaterialDesignFilledButton', r'MaterialDesign\.Brush\.Error(?![\w.-])',
           r'MaterialDesign\.Brush\.Background\.Variant', r'MaterialDesign\.Brush\.Primary\.Container',
           r'MaterialDesign\.Card\.Background(?![\w.-])', r'MaterialDesignChipOutlineButton',
           r'MaterialDesignFlatMidButton(?![\w.-])', r'MaterialDesignFlatPrimaryButton(?![\w.-])',
           r'\{StaticResource BooleanToVisibilityConverter\}', r'\{StaticResource InverseBool\}']
left = sum(len(re.findall(p, f.read_text(encoding="utf-8-sig", errors="ignore")))
           for p in PHANTOM for f in SRC.rglob("*.xaml"))
print(f"\n{'🎉 هیچ ارجاع معیوبی باقی نماند' if left == 0 else f'⛔ {left} ارجاع باقی است!'}")
raise SystemExit(0 if left == 0 else 1)
