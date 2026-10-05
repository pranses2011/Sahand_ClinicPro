#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🎨 تولید PDFهای ریلیز v1.9.0 — درمانگاه سهند
راهنمای کاربر + راهنمای نصب/شبکه + راهنمای VS + بروشور (همه به‌روزشده چرخه ۱۲)
"""
import sys
import pathlib
from playwright.sync_api import sync_playwright

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
OUT = pathlib.Path("/home/z/my-project/download")
OUT.mkdir(parents=True, exist_ok=True)

PDF_JOBS = [
    ("docs/guides/user-guide.html", "SahandClinicPro-User-Guide-v1.9.pdf", False),
    ("docs/guides/install-guide.html", "SahandClinicPro-Install-Network-Guide-v1.9.pdf", False),
    ("docs/guides/vs-guide.html", "SahandClinicPro-VS-Guide-v1.9.pdf", False),
    ("docs/guides/brochure.html", "SahandClinicPro-Brochure-v1.9.pdf", True),
]


def load_second_time(page, uri: str):
    """بارگذاری دوباره برای اطمینان از کش فونت وزیرمتن"""
    page.goto(uri, wait_until="networkidle")
    page.reload(wait_until="networkidle")
    page.evaluate("() => document.fonts.ready.then(() => true)")
    page.wait_for_timeout(700)


def main() -> int:
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
    print("PDFS-DONE")
    return 0


if __name__ == "__main__":
    sys.exit(main())
