#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🎨 رندر بنرها و پوسترهای چرخه دوازدهم (v1.9) — درمانگاه سهند
خروجی‌ها در /home/z/my-project/download/:
  ۱) SahandClinicPro-Poster-Vertical-50x70-150dpi.png   — پوستر عمودی ۲۹۵۳×۴۱۳۴
  ۲) SahandClinicPro-Poster-Horizontal-70x50-150dpi.png — پوستر افقی ۴۱۳۴×۲۹۵۳
  ۳) SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png — بیلبورد ۸۵۰۴×۴۲۵۲
  ۴) SahandClinicPro-Banner-Vertical-1.5x3m.png            — استند ۴۲۵۲×۸۵۰۴
+ پیش‌نمایش کوچک هرکدام برای بازبینی سریع (…-preview.png)
"""
import sys
import pathlib
from playwright.sync_api import sync_playwright

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
OUT = pathlib.Path("/home/z/my-project/download")
OUT.mkdir(parents=True, exist_ok=True)

PNG_JOBS = [
    ("docs/marketing/poster-vertical.html", "SahandClinicPro-Poster-Vertical-50x70-150dpi.png", 2953, 4134),
    ("docs/marketing/poster-horizontal.html", "SahandClinicPro-Poster-Horizontal-70x50-150dpi.png", 4134, 2953),
    ("docs/marketing/banner-horizontal.html", "SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png", 8504, 4252),
    ("docs/marketing/banner-vertical.html", "SahandClinicPro-Banner-Vertical-1.5x3m.png", 4252, 8504),
]


def load_second_time(page, uri: str):
    """بارگذاری دوباره تا فونت وزیرمتن قطعاً از کش اعمال شود"""
    page.goto(uri, wait_until="networkidle")
    page.reload(wait_until="networkidle")
    page.evaluate("() => document.fonts.ready.then(() => true)")
    page.wait_for_timeout(700)


def main() -> int:
    with sync_playwright() as p:
        browser = p.chromium.launch(args=["--force-color-profile=srgb", "--disable-lcd-text"])
        for rel, out_name, w, h in PNG_JOBS:
            src = (REPO / rel).resolve()
            dst = OUT / out_name
            page = browser.new_page(viewport={"width": w, "height": h}, device_scale_factor=1)
            load_second_time(page, src.as_uri())
            page.screenshot(path=str(dst), type="png")
            # پیش‌نمایش کوچک برای بازبینی سریع
            prev = OUT / out_name.replace(".png", "-preview.png")
            page.set_viewport_size({"width": 900, "height": int(900 * h / w)})
            load_second_time(page, src.as_uri())
            page.screenshot(path=str(prev), type="png", full_page=False)
            size_mb = dst.stat().st_size / (1024 * 1024)
            print(f"  ✔ {out_name}  ({size_mb:.1f} MB) + preview")
            page.close()
        browser.close()
    print("DONE-ALL")
    return 0


if __name__ == "__main__":
    sys.exit(main())
