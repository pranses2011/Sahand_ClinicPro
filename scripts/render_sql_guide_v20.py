#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📗 رندر PDF راهنمای اتصال به SQL Server و ساخت دیتابیس — Sahand ClinicPro v2.0
==============================================================================
ورودی:   docs/guides/sql-setup-guide.html  (فارسی RTL — فونت وزیرمتن — A4)
خروجی:   /home/z/my-project/download/SahandClinicPro-SQL-Setup-Guide-v2.0.pdf

همان خط لوله اثبات‌شده build_release_assets_v20.py (خانواده راهنماهای v2.0):
بارگذاری دوباره صفحه تا فونت وزیرمتن قطعاً از کش اعمال شود، سپس page.pdf
با prefer_css_page_size تا @page { size:A4 } خود CSS ملاک باشد.
"""
import pathlib
import sys

from playwright.sync_api import sync_playwright

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
SRC = REPO / "docs" / "guides" / "sql-setup-guide.html"
OUT = pathlib.Path("/home/z/my-project/download/SahandClinicPro-SQL-Setup-Guide-v2.0.pdf")


def load_second_time(page, uri: str) -> None:
    """بارگذاری دوباره تا فونت وزیرمتن قطعاً از کش اعمال شود"""
    page.goto(uri, wait_until="networkidle")
    page.reload(wait_until="networkidle")
    page.evaluate("() => document.fonts.ready.then(() => true)")
    page.wait_for_timeout(700)


def main() -> int:
    if not SRC.exists():
        print(f"✖ ورودی یافت نشد: {SRC}")
        return 1
    OUT.parent.mkdir(parents=True, exist_ok=True)

    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page()
        load_second_time(page, SRC.resolve().as_uri())
        page.pdf(
            path=str(OUT),
            format="A4",
            print_background=True,
            prefer_css_page_size=True,
        )
        page.close()
        browser.close()

    size_mb = OUT.stat().st_size / (1024 * 1024)
    print(f"✔ SahandClinicPro-SQL-Setup-Guide-v2.0.pdf ساخته شد ({size_mb:.2f} MB)")
    print(f"  مسیر: {OUT}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
