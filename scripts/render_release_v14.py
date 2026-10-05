#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🎨 اسکریپت تولید asset های رلیز v1.4.0 — درمانگاه سهند
=========================================================
خروجی‌ها (در /home/z/my-project/download/):
  ۱) SahandClinicPro-User-Guide-v1.4.pdf           — راهنمای کاربر (A4 عمودی)
  ۲) SahandClinicPro-Install-Network-Guide-v1.4.pdf — راهنمای نصب (A4 عمودی)
  ۳) SahandClinicPro-Brochure-v1.4.pdf              — بروشور بازاریابی (A4 افقی)
  ۴) SahandClinicPro-Poster-Vertical-50x70-150dpi.png   — پوستر عمودی ۲۹۵۳×۴۱۳۴
  ۵) SahandClinicPro-Poster-Horizontal-70x50-150dpi.png — پوستر افقی ۴۱۳۴×۲۹۵۳
  ۶) SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png — بیلبورد ۸۵۰۴×۴۲۵۲
  ۷) SahandClinicPro-Banner-Vertical-1.5x3m.png            — استند ۴۲۵۲×۸۵۰۴
نکته فنی: فایل‌ها با file:// لود می‌شوند تا فونت‌های نسبی Vazirmatn فعال شوند؛
پیش از رندر به fonts.ready و networkidle منتظر می‌مانیم.
"""
import sys
import pathlib
from playwright.sync_api import sync_playwright

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
OUT = pathlib.Path("/home/z/my-project/download")
OUT.mkdir(parents=True, exist_ok=True)

# ─── تعریف نگاشت‌ها: (فایل منبع، خروجی، نوع رندر، تنظیمات) ───
PDF_JOBS = [
    # (منبع نسبی، نام خروجی، landscape؟)
    ("docs/guides/user-guide.html", "SahandClinicPro-User-Guide-v1.4.pdf", False),
    ("docs/guides/install-guide.html", "SahandClinicPro-Install-Network-Guide-v1.4.pdf", False),
    ("docs/guides/brochure.html", "SahandClinicPro-Brochure-v1.4.pdf", True),
]

PNG_JOBS = [
    # (منبع نسبی، نام خروجی، عرض، ارتفاع)
    ("docs/marketing/poster-vertical.html", "SahandClinicPro-Poster-Vertical-50x70-150dpi.png", 2953, 4134),
    ("docs/marketing/poster-horizontal.html", "SahandClinicPro-Poster-Horizontal-70x50-150dpi.png", 4134, 2953),
    ("docs/marketing/banner-horizontal.html", "SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png", 8504, 4252),
    ("docs/marketing/banner-vertical.html", "SahandClinicPro-Banner-Vertical-1.5x3m.png", 4252, 8504),
]


def render_pdfs(page):
    """رندر سه PDF چاپی — A4 با پس‌زمینه و صفحه‌بندی CSS"""
    for rel, out_name, landscape in PDF_JOBS:
        src = (REPO / rel).resolve()
        dst = OUT / out_name
        page.goto(src.as_uri(), wait_until="networkidle")
        # اطمینان از بارگذاری کامل فونت‌های محلی Vazirmatn
        page.evaluate("() => document.fonts.ready.then(() => true)")
        page.wait_for_timeout(700)
        page.pdf(
            path=str(dst),
            format="A4",
            landscape=landscape,
            print_background=True,
            prefer_css_page_size=True,  # احترام به @page در HTML
        )
        size_kb = dst.stat().st_size // 1024
        print(f"  ✔ {out_name}  ({size_kb} KB)")


def render_pngs(browser):
    """رندر چهار خروجی چاپی PNG با ابعاد پیکسلی دقیق"""
    for rel, out_name, w, h in PNG_JOBS:
        src = (REPO / rel).resolve()
        dst = OUT / out_name
        context = browser.new_context(
            viewport={"width": w, "height": h},
            device_scale_factor=1,
        )
        page = context.new_page()
        page.goto(src.as_uri(), wait_until="networkidle")
        page.evaluate("() => document.fonts.ready.then(() => true)")
        page.wait_for_timeout(900)
        page.screenshot(path=str(dst), clip={"x": 0, "y": 0, "width": w, "height": h})
        size_mb = dst.stat().st_size // (1024 * 1024)
        print(f"  ✔ {out_name}  ({size_mb} MB)")
        context.close()


def main():
    print("🎨 رندر asset های رلیز v1.4.0 — آغاز")
    with sync_playwright() as p:
        browser = p.chromium.launch(args=["--force-color-profile=srgb", "--font-render-hinting=none"])
        # صفحه مشترک برای PDF ها
        pdf_page = browser.new_page(viewport={"width": 1400, "height": 1000})
        print("📄 PDF ها:")
        render_pdfs(pdf_page)
        pdf_page.close()
        print("🖼 PNG های چاپی:")
        render_pngs(browser)
        browser.close()
    print("✅ همه رندرها کامل شد — خروجی در:", OUT)


if __name__ == "__main__":
    sys.exit(main())
