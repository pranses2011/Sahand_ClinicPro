#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🌐 درج @font-face فونت وزیرمتن (woff2 پایه‌۶۴) در قالب HTML مرکز آموزش — v2.0.2
================================================================================
مرکز آموزش درس‌های HTML را با WebView2 (کرومیوم) رندر می‌کند؛ کرومیوم فقط
فونت‌های «نصب‌شدهٔ ویندوز» یا @font-face را می‌بیند. با جاسازی woff2 به‌صورت
base64 داخل قالب، درس‌ها روی هر دستگاهی — بدون نصب فونت و بدون اینترنت —
با وزیرمتن رندر می‌شوند (هم‌راستا با فونت جاسازی‌شدهٔ فرم‌های WPF).

ورودی‌ها:  docs/assets/webfonts/Vazirmatn-{Regular,Bold}.woff2
خروجی:    وصلهٔ src/Sahand.ClinicPro.Desktop/Services/TrainingCatalog.cs
دوباره‌سازی: python3 scripts/embed_webfonts_v202.py  (idempotent نیست —
            قبل از اجرای مجدد، بلوک قبلی @font-face را دستی حذف کنید)
"""
import base64
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parent.parent
WF = REPO / "docs" / "assets" / "webfonts"
TARGET = REPO / "src" / "Sahand.ClinicPro.Desktop" / "Services" / "TrainingCatalog.cs"


def main() -> int:
    faces = []
    for weight, fname in ((400, "Vazirmatn-Regular.woff2"), (700, "Vazirmatn-Bold.woff2")):
        path = WF / fname
        if not path.exists():
            print(f"✖ {path} یافت نشد")
            return 1
        data = path.read_bytes()
        b64 = base64.b64encode(data).decode("ascii")
        # ⚠️ رشتهٔ سازنده فقط با الحاق ساده ساخته می‌شود (نه f-string):
        # در f-string پایتون «}}» به «}» تکی تبدیل می‌شود و در رشتهٔ interpolate
        # شدهٔ C# باید «}}» بماند — همین تداخل یک‌بار خطای CS8086 ساخت.
        face = ("  @font-face {{ font-family: 'Vazirmatn'; "
                "src: url(data:font/woff2;base64," + b64 + ") format('woff2'); "
                "font-weight: " + str(weight) + "; font-style: normal; }}")
        faces.append(face)
        print(f"  ✔ {fname}: {len(data)} بایت → {len(b64)} کاراکتر base64 (وزن {weight})")

    text = TARGET.read_text(encoding="utf-8")
    if "@font-face" in text:
        print("✖ از قبل @font-face در TrainingCatalog.cs هست — اجرای مجدد بدون حذف قبلی مجاز نیست")
        return 1
    marker = "<style>\n"
    if marker not in text:
        print("✖ نشانهٔ <style> در قالب یافت نشد")
        return 1
    text = text.replace(marker, marker + "\n".join(faces) + "\n", 1)
    TARGET.write_text(text, encoding="utf-8")
    print(f"✔ TrainingCatalog.cs وصله شد ({len(text)} کاراکتر)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
