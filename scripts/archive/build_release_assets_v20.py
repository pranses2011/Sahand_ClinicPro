#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📦 ساخت دارایی‌های ریلیز v2.0.0 — درمانگاه سهند (چرخه سیزدهم)
==============================================================
خروجی‌ها در /home/z/my-project/download/:
  ۱) SahandClinicPro-LicenseServer-v2.0.zip — سرور لایسنس PHP کامل (با ۹ ماژول جدید کاتالوگ)
  ۲) ۴ PDF: راهنمای کاربر + نصب/شبکه + VS + بروشور ۴۱ صفحه‌ای
  ۳) ۴ PNG چاپی: پوستر عمودی/افقی + بیلبورد + استند (+ پیش‌نمایش)
"""
import sys
import zipfile
import pathlib
from playwright.sync_api import sync_playwright

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
OUT = pathlib.Path("/home/z/my-project/download")
OUT.mkdir(parents=True, exist_ok=True)

EXCLUDE_DIRS = {"bin", "obj", "publish", "dist", ".git", ".vs", "TestResults"}


def add_tree(zf: zipfile.ZipFile, base: pathlib.Path, arc_root: str) -> int:
    """افزودن بازگشتی یک پوشه به ZIP — خروجی: تعداد فایل"""
    count = 0
    for path in sorted(base.rglob("*")):
        if not path.is_file():
            continue
        rel = path.relative_to(base)
        if any(part in EXCLUDE_DIRS for part in rel.parts):
            continue
        zf.write(path, f"{arc_root}/{rel.as_posix()}")
        count += 1
    return count


def build_license_zip():
    """بسته سرور لایسنس PHP v2.0 برای cPanel — شامل ۹ ماژول جدید کاتالوگ"""
    dst = OUT / "SahandClinicPro-LicenseServer-v2.0.zip"
    base = REPO / "src" / "LicenseServer.Php"
    total = 0
    with zipfile.ZipFile(dst, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        total += add_tree(zf, base, "LicenseServer")
        zf.write(base / "README-FA.md", "README-FA.md")
        total += 1
    size_mb = dst.stat().st_size / (1024 * 1024)
    print(f"  ✔ SahandClinicPro-LicenseServer-v2.0.zip  ({total} فایل، {size_mb:.1f} MB)")
    return dst


PDF_JOBS = [
    ("docs/guides/user-guide.html", "SahandClinicPro-User-Guide-v2.0.pdf", False),
    ("docs/guides/install-guide.html", "SahandClinicPro-Install-Network-Guide-v2.0.pdf", False),
    ("docs/guides/vs-guide.html", "SahandClinicPro-VS-Guide-v2.0.pdf", False),
    ("docs/guides/brochure.html", "SahandClinicPro-Brochure-v2.0.pdf", True),
]

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


def build_pdfs():
    with sync_playwright() as p:
        browser = p.chromium.launch()
        for rel, out_name, landscape in PDF_JOBS:
            src = (REPO / rel).resolve()
            dst = OUT / out_name
            page = browser.new_page()
            load_second_time(page, src.as_uri())
            page.pdf(path=str(dst), format="A4", landscape=landscape,
                     print_background=True, prefer_css_page_size=True)
            size_mb = dst.stat().st_size / (1024 * 1024)
            print(f"  ✔ {out_name}  ({size_mb:.1f} MB)")
            page.close()
        browser.close()


def build_pngs():
    with sync_playwright() as p:
        browser = p.chromium.launch(args=["--force-color-profile=srgb", "--disable-lcd-text"])
        for rel, out_name, w, h in PNG_JOBS:
            src = (REPO / rel).resolve()
            dst = OUT / out_name
            page = browser.new_page(viewport={"width": w, "height": h}, device_scale_factor=1)
            load_second_time(page, src.as_uri())
            page.screenshot(path=str(dst), type="png")
            prev = OUT / out_name.replace(".png", "-preview.png")
            page.set_viewport_size({"width": 900, "height": int(900 * h / w)})
            load_second_time(page, src.as_uri())
            page.screenshot(path=str(prev), type="png", full_page=False)
            size_mb = dst.stat().st_size / (1024 * 1024)
            print(f"  ✔ {out_name}  ({size_mb:.1f} MB) + preview")
            page.close()
        browser.close()


def main() -> int:
    print("📦 ۱/۳ — ZIP سرور لایسنس…")
    build_license_zip()
    print("📄 ۲/۳ — PDFها…")
    build_pdfs()
    print("🖼 ۳/۳ — پوسترها و بنرها…")
    build_pngs()
    print("\nDONE-ALL — همه دارایی‌های v2.0.0 آماده شد")
    return 0


if __name__ == "__main__":
    sys.exit(main())
