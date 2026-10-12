#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🏁 ساخت رلیز گیت‌هاب v2.0.1 با یادداشت انتشار فارسی RTL — چرخه چهاردهم (وصله)
تغییرات فقط در XAML دسکتاپ است؛ دارایی‌های بازاریابی v2.0.0 همچنان معتبرند.
⚠️ توکن از متغیر محیطی GH_TOKEN خوانده می‌شود — هرگز در سورس هاردکد نمی‌شود
"""
import json
import pathlib
import sys
import urllib.parse
import urllib.request

REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
TAG = "v2.0.1"

import os
TOKEN = os.environ.get("GH_TOKEN") or ""
if not TOKEN:
    raise SystemExit("متغیر محیطی GH_TOKEN تنظیم نشده است — توکن دیگر در سورس هاردکد نمی‌شود")

BODY = """<div dir="rtl" align="right">

# 🔧 نسخه ۲.۰.۱ — وصله رفع کرش اجرای MaterialDesign

**رفع کامل کرش زمان اجرای برنامه در Visual Studio + کشف و رفع ۹ کلید شبح‌مانند دیگر در همان کلاس خطا + گارد بازگشتی ضد تکرار** 🎉

## 🐞 چه اتفاقی افتاده بود؟

اجرای برنامه با خطای زیر متوقف می‌شد:

```
XamlParseException: Cannot find resource named 'MaterialDesignFilledButton'
```

**ریشه:** کلید استایل به سبک MD3 خام (`MaterialDesignFilledButton`) در پکیج `MaterialDesignThemes 5.3.2` اصلاً وجود ندارد. چون ارجاعات `StaticResource` در XAML هنگام **بیلد اعتبارسنجی نمی‌شوند** و فقط در **زمان اجرا** فرو می‌ریزند، بیلد سبز و ۲۴۹ تست سبز این خطا را نشان نداده بودند.

## 🔍 ممیزی جامع — نه فقط همین یک خطا

با کالبدشکافی DLL پکیج و استخراج **فهرست ۸۷۲ کلید واقعی**، همه ۷۰ فایل XAML ممیزی شد: **۱۱۰ ارجاع معیوب در ۹ کلید شبح‌مانند و ۴۱ فایل** شناسایی و با نگاشت معنایی اصلاح شد — بدون این ممیزی، برنامه پس از رفع هر مورد روی مورد بعدی کرش می‌کرد:

| کلید نامعتبر | تعداد | معادل صحیح |
|:---|:---:|:---|
| `MaterialDesignFilledButton` | ۱۸ | `MaterialDesignRaisedButton` |
| `MaterialDesign.Brush.Background.Variant` | ۴۳ | `MaterialDesign.Brush.Separator.Background` |
| `MaterialDesign.Card.Background` | ۲۰ | `MaterialDesign.Brush.Card.Background` |
| `MaterialDesign.Brush.Error` | ۱۵ | `MaterialDesign.Brush.ValidationError` |
| `BooleanToVisibilityConverter` (کلید تایپی) | ۹ | `BoolToVis` |
| سه استایل تکی + یک تایپوی محلی | ۵ | Outlined / FlatMidBg / Flat / InvertBool |

## ✨ بهبودهای همراه

- **نسخه دینامیک** — پانویس پنجره ورود از زمان v1.2.0 هاردکد و کهنه بود («نسخه ۱.۲.۰»!)؛ حالا هر دو نمایش (ورود + نوار وضعیت اصلی) با کلاس `Helpers/AppInfo` مستقیم از نسخه اسمبلی با ارقام فارسی خوانده می‌شوند
- **گارد بازگشتی `XamlResourceAuditTests`** — از این پس هر ارجاع منبع نامعتبر جدید در XAML، تست را قرمز می‌کند (۲۵۰/۲۵۰ تست سبز)
- **ابزار ممیزی در مخزن** — `scripts/audit_xaml_resources.py` + مانیفست کلیدهای پکیج + اسکریپت بازتولید برای ارتقاهای آینده

## 📥 نحوه دریافت

سورس اصلاح‌شده را از **Source code (zip)** همین رلیز دانلود کنید و در Visual Studio باز کنید — بدون هیچ پیکربندی اضافه اجرا می‌شود.

> 💡 **دارایی‌های نسخه ۲.۰.۰** (راهنمای کاربر، راهنمای نصب، راهنمای VS، بروشور، پوستر/بنر و ZIP سرور لایسنس) **همچنان معتبرند** — این وصله فقط XAML دسکتاپ را اصلاح می‌کند و هیچ قابلیت یا پیکربندی‌ای تغییر نکرده است.

</div>
"""


def api(url: str, method: str = "GET", data: bytes | None = None,
        content_type: str = "application/json", token: str = "") -> dict:
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {token}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Content-Type", content_type)
    req.add_header("User-Agent", "SahandClinicPro-Release-v201")
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())


def main() -> int:
    token = TOKEN
    print(f"🏁 ساخت رلیز {TAG}")

    # idempotency — حذف رلیز/تگ قبلی
    try:
        rel = api(f"{REPO_API}/releases/tags/{TAG}", token=token)
        api(f"{REPO_API}/releases/{rel['id']}", method="DELETE", token=token)
        print("  ↻ رلیز قبلی حذف شد")
    except Exception:
        print("  ✔ رلیز قبلی وجود نداشت")
    try:
        api(f"{REPO_API}/git/refs/tags/{TAG}", method="DELETE", token=token)
    except Exception:
        pass

    main_sha = api(f"{REPO_API}/git/ref/heads/main", token=token)["object"]["sha"]
    payload = json.dumps({
        "tag_name": TAG,
        "target_commitish": main_sha,
        "name": "🔧 نسخه ۲.۰.۱ — وصله رفع کرش اجرای MaterialDesign",
        "body": BODY,
        "draft": False,
        "prerelease": False,
    }).encode()
    rel = api(f"{REPO_API}/releases", method="POST", data=payload, token=token)
    print(f"  ✔ رلیز ساخته شد — شناسه {rel['id']}")
    print(f"  🔗 {rel['html_url']}")
    print("🎉 رلیز وصله کامل شد (سورس به‌صورت خودکار پیوست می‌شود)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
