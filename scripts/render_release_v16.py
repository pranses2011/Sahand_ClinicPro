#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🎨 اسکریپت تولید asset های رلیز v1.6.0 — درمانگاه سهند
=========================================================
خروجی‌ها (در /home/z/my-project/download/):
  ۱) SahandClinicPro-User-Guide-v1.6.pdf            — راهنمای کاربر v3.1 (A4 عمودی)
  ۲) SahandClinicPro-Install-Network-Guide-v1.6.pdf — راهنمای نصب مبتدی‌پسند (A4 عمودی)
  ۳) SahandClinicPro-VS-Guide-v1.6.pdf              — راهنمای VS و خروجی exe (A4 عمودی) 🆕
  ۴) SahandClinicPro-Brochure-v1.6.pdf              — کاتالوگ با تور ۵۲ فرم (A4 افقی)
  ۵) SahandClinicPro-Poster-Vertical-50x70-150dpi.png   — پوستر عمودی ۲۹۵۳×۴۱۳۴ (سه‌بعدی)
  ۶) SahandClinicPro-Poster-Horizontal-70x50-150dpi.png — پوستر افقی ۴۱۳۴×۲۹۵۳ (سه‌بعدی)
  ۷) SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png — بیلبورد ۸۵۰۴×۴۲۵۲ (سه‌بعدی)
  ۸) SahandClinicPro-Banner-Vertical-1.5x3m.png            — استند ۴۲۵۲×۸۵۰۴ (سه‌بعدی)
"""
import sys
import pathlib
from playwright.sync_api import sync_playwright

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
OUT = pathlib.Path("/home/z/my-project/download")
OUT.mkdir(parents=True, exist_ok=True)

PDF_JOBS = [
    ("docs/guides/user-guide.html", "SahandClinicPro-User-Guide-v1.6.pdf", False),
    ("docs/guides/install-guide.html", "SahandClinicPro-Install-Network-Guide-v1.6.pdf", False),
    ("docs/guides/vs-guide.html", "SahandClinicPro-VS-Guide-v1.6.pdf", False),
    ("docs/guides/brochure.html", "SahandClinicPro-Brochure-v1.6.pdf", True),
]

PNG_JOBS = [
    ("docs/marketing/poster-vertical.html", "SahandClinicPro-Poster-Vertical-50x70-150dpi.png", 2953, 4134),
    ("docs/marketing/poster-horizontal.html", "SahandClinicPro-Poster-Horizontal-70x50-150dpi.png", 4134, 2953),
    ("docs/marketing/banner-horizontal.html", "SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png", 8504, 4252),
    ("docs/marketing/banner-vertical.html", "SahandClinicPro-Banner-Vertical-1.5x3m.png", 4252, 8504),
]


def render_pdfs(page):
    for rel, out_name, landscape in PDF_JOBS:
        src = (REPO / rel).resolve()
        dst = OUT / out_name
        page.goto(src.as_uri(), wait_until="networkidle")
        page.evaluate("() => document.fonts.ready.then(() => true)")
        page.wait_for_timeout(800)
        page.pdf(path=str(dst), format="A4", landscape=landscape,
                 print_background=True, prefer_css_page_size=True)
        size_mb = dst.stat().st_size / (1024 * 1024)
        print(f"  ✔ {out_name}  ({size_mb:.1f} MB)")


def render_pngs(browser):
    for rel, out_name, w, h in PNG_JOBS:
        src = (REPO / rel).resolve()
        dst = OUT / out_name
        context = browser.new_context(viewport={"width": w, "height": h}, device_scale_factor=1)
        page = context.new_page()
        page.goto(src.as_uri(), wait_until="networkidle")
        page.evaluate("() => document.fonts.ready.then(() => true)")
        page.wait_for_timeout(900)
        page.screenshot(path=str(dst), clip={"x": 0, "y": 0, "width": w, "height": h})
        size_mb = dst.stat().st_size / (1024 * 1024)
        print(f"  ✔ {out_name}  ({size_mb:.1f} MB)")
        context.close()


def main():
    print("🎨 رندر asset های رلیز v1.6.0 — آغاز")
    with sync_playwright() as p:
        browser = p.chromium.launch(args=["--force-color-profile=srgb", "--font-render-hinting=none"])
        pdf_page = browser.new_page(viewport={"width": 1400, "height": 1000})
        print("📄 PDF ها (۵ سند):")
        render_pdfs(pdf_page)
        pdf_page.close()
        print("🖼 PNG های چاپی (سه‌بعدی):")
        render_pngs(browser)
        browser.close()
    print("✅ همه رندرها کامل شد — خروجی در:", OUT)
    return 0


if __name__ == "__main__":
    sys.exit(main())
