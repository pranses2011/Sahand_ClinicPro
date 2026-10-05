#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
✏️ به‌روزرسانی متن رلیز v1.4.0 — جایگزینی بخش «نصب» با فهرست ۹ پیوست همین رلیز
"""
import json
import pathlib
import sys
import urllib.request

TOKEN = "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"
REPO = "pranses2011/Sahand_ClinicPro"
TAG = "v1.4.0"


def api(url: str, method: str = "GET", data: bytes | None = None,
        content_type: str = "application/json"):
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {TOKEN}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Content-Type", content_type)
    req.add_header("User-Agent", "SahandClinicPro-Release-Editor")
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())


def main():
    rel = api(f"https://api.github.com/repos/{REPO}/releases/tags/{TAG}")
    body = rel["body"]

    # بخش قدیمی «نصب» (که به v1.3.0 ارجاع می‌داد) از انتهای متن
    old_start = body.find("## 📦 نصب")
    old_end = body.find("## 🔗 پیوندها")
    if old_start == -1 or old_end == -1:
        print("✖ بخش‌های موردنظر یافت نشد")
        return 1

    new_section = """## 📦 پیوست‌های دانلود (۹ مورد)

| # | پیوست | کاربرد | حجم |
|:---:|:---|:---|:---:|
| ۱ | `SahandClinicPro-Setup-v1.4.0.zip` | 📦 بسته خودکفای تولید نصب‌کننده — سورس کامل + اسکریپت‌های Inno Setup فارسی؛ روی یک رایانه ویندوزی `BUILD-INSTALLER.bat` را اجرا کنید تا `SahandClinicPro-Setup-v1.4.0.exe` ساخته شود | ~۲MB |
| ۲ | `Sahand-LicenseServer-cPanel-v1.1.zip` | 🔐 سرور لایسنس PHP خالص برای هاست cPanel — نصب ۳ مرحله‌ای، بدون Composer و بدون هیچ پیش‌نیازی | ~۴۰KB |
| ۳ | `SahandClinicPro-Install-Network-Guide-v1.4.pdf` | 📗 راهنمای کامل نصب و راه‌اندازی شبکه — ۱۲ فصل، ۱۷ صفحه: SQL Server، سرور API با سرویس ویندوز، کلاینت‌ها، سرور لایسنس، اتصال دستگاه حضور و غیاب، بکاپ خودکار | ~۱.۶MB |
| ۴ | `SahandClinicPro-User-Guide-v1.4.pdf` | 📘 راهنمای کامل کار با نرم‌افزار — ۱۷ فصل، ۱۸ صفحه با ۱۰ اسکرین‌شات واقعی؛ شامل فصل جدید «داروخانه و انبارگردانی» ⭐ | ~۳.۷MB |
| ۵ | `SahandClinicPro-Brochure-v1.4.pdf` | 🎀 کاتالوگ و بروشور بازاریابی — ۱۲ اسلاید تمام‌رنگی: مزیت‌ها، نقشه ۲۴ ماژول، مقایسه با رقبا، پلن‌های لایسنس | ~۲.۵MB |
| ۶ | `SahandClinicPro-Poster-Vertical-50x70-150dpi.png` | 🖼 پوستر چاپی عمودی ۵۰×۷۰ سانتی‌متر (۱۵۰DPI) | ~۳MB |
| ۷ | `SahandClinicPro-Poster-Horizontal-70x50-150dpi.png` | 🖼 پوستر چاپی افقی ۷۰×۵۰ سانتی‌متر (۱۵۰DPI) | ~۳.۵MB |
| ۸ | `SahandClinicPro-Banner-Vertical-1.5x3m.png` | 🏙 استند عمودی ایستاده ۱.۵×۳ متر (۷۲DPI) | ~۵.۲MB |
| ۹ | `SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png` | 🛢 بیلبورد شهری افقی ۳×۱.۵ متر (۷۲DPI) | ~۶.۹MB |

> 💡 **مسیر پیشنهادی نصب:** پیوست ۳ (راهنمای نصب) را باز کنید → با پیوست ۱ سرور و کلاینت‌ها را نصب کنید → با پیوست ۲ سرور لایسنس را روی هاست راه بیندازید → برای تیم فروش، پیوست‌های ۵ تا ۹ آماده چاپ‌اند.

"""
    body = body[:old_start] + new_section + body[old_end:]

    api(
        f"https://api.github.com/repos/{REPO}/releases/{rel['id']}",
        method="PATCH",
        data=json.dumps({"body": body}).encode(),
    )
    print("✅ متن رلیز v1.4.0 با فهرست ۹ پیوست به‌روزرسانی شد")
    return 0


if __name__ == "__main__":
    sys.exit(main())
