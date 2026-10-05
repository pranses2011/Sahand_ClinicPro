#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🔍 راستی‌آزمایی پیکسلی خروجی‌های چاپی — ریشه‌یابی خرابی «چپ سفید / راست ناقص»
برای هر فایل HTML چاپی:
  ۱) رندر در ابعاد دقیق پیکسلی
  ۲) نمونه‌گیری از گوشه‌ها و لبه‌ها (۲۰۰ نقطه در هر لبه)
  ۳) عدم وجود نوار سفید/تک‌رنگ در هیچ لبه
  ۴) ابعاد دقیق خروجی
خروجی: گزارش PASS/FAIL برای هر فایل
"""
import sys
import pathlib
from playwright.sync_api import sync_playwright

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
TMP = pathlib.Path("/home/z/my-project/scripts/_print_check")
TMP.mkdir(parents=True, exist_ok=True)

JOBS = [
    ("docs/marketing/poster-vertical.html", 2953, 4134),
    ("docs/marketing/poster-horizontal.html", 4134, 2953),
    ("docs/marketing/banner-horizontal.html", 8504, 4252),
    ("docs/marketing/banner-vertical.html", 4252, 8504),
]


def edge_white_ratio(png_path, w, h):
    """نسبت پیکسل‌های تقریباً سفید در لبه‌ها — با Pillow"""
    from PIL import Image
    img = Image.open(png_path).convert("RGB")
    assert img.size == (w, h), f"ابعاد نادرست: {img.size} به‌جای {(w, h)}"
    px = img.load()

    def is_white(c):
        return c[0] > 242 and c[1] > 242 and c[2] > 242

    edges = {
        "top": [(x, 0) for x in range(0, w, max(1, w // 200))],
        "bottom": [(x, h - 1) for x in range(0, w, max(1, w // 200))],
        "left": [(0, y) for y in range(0, h, max(1, h // 200))],
        "right": [(w - 1, y) for y in range(0, h, max(1, h // 200))],
        "corners": [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)],
    }
    result = {}
    for name, points in edges.items():
        whites = sum(1 for p in points if is_white(px[p[0], p[1]]))
        result[name] = whites / len(points)
    return result


def main():
    targets = sys.argv[1:] if len(sys.argv) > 1 else [j[0] for j in JOBS]
    with sync_playwright() as p:
        browser = p.chromium.launch(args=["--force-color-profile=srgb"])
        all_ok = True
        for rel, w, h in JOBS:
            if rel not in targets:
                continue
            src = (REPO / rel).resolve()
            if not src.exists():
                print(f"⏭ {rel} — وجود ندارد، رد شد")
                continue
            out = TMP / (pathlib.Path(rel).stem + ".png")
            ctx = browser.new_context(viewport={"width": w, "height": h}, device_scale_factor=1)
            page = ctx.new_page()
            page.goto(src.as_uri(), wait_until="networkidle")
            page.evaluate("() => document.fonts.ready.then(() => true)")
            page.wait_for_timeout(900)
            # برش دقیقاً در مرز صفحه — چون هیچ سرریزی وجود ندارد، نتیجه همان کلیدی است
            page.screenshot(path=str(out), clip={"x": 0, "y": 0, "width": w, "height": h})

            # بررسی سرریز افقی سند — ریشه‌یابی باگ نسخه قبل
            overflow = page.evaluate(
                "() => ({ sw: document.documentElement.scrollWidth,"
                " sh: document.documentElement.scrollHeight })")
            ctx.close()

            ratios = edge_white_ratio(out, w, h)
            bad = {k: v for k, v in ratios.items() if v > 0.02}
            size_ok = overflow["sw"] <= w and overflow["sh"] <= h
            status = "✅ PASS" if (not bad and size_ok) else "❌ FAIL"
            if bad or not size_ok:
                all_ok = False
            print(f"{status} {rel}")
            print(f"    scrollW×H = {overflow['sw']}×{overflow['sh']} (مجاز: {w}×{h})"
                  + ("" if size_ok else "  ⚠ سرریز سند!"))
            for name, r in ratios.items():
                flag = "  ⚠ نوار سفید!" if r > 0.02 else ""
                print(f"    سفیدی {name:7s} = {r:6.1%}{flag}")
        browser.close()
        print("\n" + ("🎉 همه خروجی‌ها سالم هستند" if all_ok else "⚠️ خروجی معیوب — اصلاح لازم است"))
        return 0 if all_ok else 1


if __name__ == "__main__":
    sys.exit(main())
