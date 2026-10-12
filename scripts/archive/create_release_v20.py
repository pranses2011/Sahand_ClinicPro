#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🏁 ساخت رلیز گیت‌هاب v2.0.0 با یادداشت انتشار فارسی RTL + آپلود ۹ پیوست
چرخه سیزدهم: ۱۶ قابلیت منتخب سند ایده‌ها در ۶ محور — بزرگ‌ترین ارتقای یک‌باره محصول
⚠️ توکن از متغیر محیطی GH_TOKEN خوانده می‌شود — هرگز در سورس هاردکد نمی‌شود
"""
import json
import pathlib
import sys
import urllib.parse
import urllib.request

REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
TAG = "v2.0.0"
OUT = pathlib.Path("/home/z/my-project/download")

import os
TOKEN = os.environ.get("GH_TOKEN") or ""
if not TOKEN:
    raise SystemExit("متغیر محیطی GH_TOKEN تنظیم نشده است — توکن دیگر در سورس هاردکد نمی‌شود")

BODY = """<div dir="rtl" align="right">

# 🎯 نسخه ۲.۰.۰ — چرخه سیزدهم «۱۶ قابلیت منتخب سند ایده‌ها»

**بزرگ‌ترین ارتقای یک‌باره در تاریخ محصول — اجرای کامل ۱۶ قابلیت انتخاب‌شده مالک از سند قابلیت‌های پیشنهادی در ۶ محور** 🎉

| شاخص | مقدار |
|:---|:---|
| 🩺 ایمنی بالینی | موتور **۷بُعدی** ایمنی دارویی — ۵۲۳ جفت تداخل + حساسیت متقاطع ATC + بارداری FDA + Beers سنی + کلیوی/کبدی |
| 📊 هوش تجاری | **۲۵ گزارش نموداری** + تحلیل RFM + پیش‌بینی درآمد + گزارش‌ساز رایگانه بدون SQL |
| 🖥 فرم‌های دسکتاپ | **۶۴ فرم WPF** (۱۲ فرم جدید) با Material Design RTL |
| 🧩 ماژول‌های لایسنس | **۳۹ ماژول** (۹ ماژول جدید ۲.۰) |
| ☁️ زیرساخت | بکاپ ابری S3-سازگار (آروان/ابستور) با AES-256-GCM + آرشیو سالانه |
| 🧪 تست‌ها | **۲۴۹ تست دات‌نت** + ۳۴ E2E لایسنس — همه سبز |
| ✅ وضعیت بیلد | ۰ خطا — Release |

---

## 🩺 محور بالینی (درخواست‌های ۱-۱، ۱-۴، ۱-۵، ۱-۶)

- **الگوریتم خطر تشخیصی جامع** — با افزودن هر دارو، ۷ بُعد همان لحظه بررسی می‌شود: تداخل دارو↔دارو (۵۲۳ جفت، ۴ سطح شدت)، حساسیت مستقیم و متقاطع خانواده ATC، رده بارداری FDA، شیردهی، احتیاط سنی کودک/سالمند (Beers)، نارسایی کلیه/کبد و تعارض تشخیص زمینه‌ای — **گیت منع مطلق** با دلیل کتبی پزشک
- **نوار نتایج غیرعادی پاراکلینیک** — بالای پرونده بیمار، آخرین مقادیر خارج از محدوده با رنگ H/HH/L/LL + ۱۴ کلیدواژه بحرانی گزارش رادیولوژیست
- **خط زمان پرونده (Timeline)** — ادغام ۷ نوع رویداد با برچسب شمسی و فیلتر نوع/سال
- **طراح قالب چاپ سفارشی** — ۹ نوع سند، ویرایشگر HTML با متغیرهای جایگاه‌دار، پیش‌نمایش زنده، ۹ قالب آماده

## 💰 محور مالی (۲-۱، ۲-۲)

- **تسویه حساب بیمه (Fractionment)** — موتور قواعد ۴سطحی به‌ازای هر قلم (اختصاصی خدمت ← عمومی بیمه ← تعرفه ← قرارداد) با فرانشیز و سقف + صورتحساب طبقه‌بندی‌شده
- **صندوق روزانه و بستن شیفت** — شمارش فیزیکی ↔ سیستم با اختلاف‌سنجی رنگی + تحویل‌گیرنده شیفت بعد + رسید ترتیبی SHR قابل چاپ

## ⏳ محور نوبت‌دهی (۳-۴)

- **لیست انتظار هوشمند** — با لغو هر نوبت، متناظرترین منتظر همان پزشک (اولویت اورژانس→فوری→عادی) خودکار پیامک پیشنهاد جای خالی می‌گیرد

## 📊 محور هوش تجاری (۴-۱ تا ۴-۶)

| قابلیت | شرح |
|:---|:---|
| رشد MoM + مقایسه YoY | نمودار دوقلو + درصد رشد + جهت صعود/نزول |
| داشبورد شخصی‌سازی‌شده | چیدمان اختصاصی هر کاربر — نمایش/پنهان و جابه‌جایی کارت‌ها، ذخیره روی سرور |
| گزارش‌ساز رایگانه | سه گام گرافیکی بدون SQL — لیست سفید ۷ موجودیت، ~۹۰ فیلد فارسی، ۱۳ عملگر + نمودار و CSV |
| تحلیل RFM | پنجکی ۵×۵×۵ و رده ۸گانه از «قهرمان» تا «از دست رفته» + پیامک هدفمند |
| پیش‌بینی روند درآمد | رگرسیون حداقل مربعات + بازه اطمینان ۸۰٪ |
| خروجی PDF | هر گزارش جدولی با سربرگ درمانگاه، فونت وزیرمتن جاسازی‌شده و محل امضا/مهر |

## 🔧 محور زیرساخت (۷-۳، ۷-۷)

- **بکاپ ابری** — S3-سازگار (آروان/ابستور/AWS) با REST امضادار AWS Signature V4 خالص + رمزنگاری AES-256-GCM + بارگذاری خودکار شبانه + تست رمزگشایی بدون دست‌زدن به بانک
- **آرشیو سالانه** — جابه‌جایی محافظت‌شده ۷ جدولی (فرزند-به-والد) با پیش‌نمایش شمارشی + مرورگر پرونده قدیمی بیمار — بانک عملیاتی همیشه سبک

## 🎓 محور آموزش (۸-۶)

- **تور هدایت‌شده اولین اجرا** — ۸ ایستگاه با هایلایت نقطه‌ای سوراخ‌دار و حباب راهنما، خودکار در نخستین ورود هر کاربر
- **مرکز آموزش** — ۱۰ درس تعاملی HTML راست‌به‌چپ گام‌به‌گام داخلی + پیشرفت یادگیری هر کاربر روی سرور

## 📎 پیوست‌ها و خروجی‌ها

| فایل | شرح |
|:---|:---|
| `SahandClinicPro-LicenseServer-v2.0.zip` | سرور لایسنس PHP کامل برای آپلود روی cPanel — دارای ۹ ماژول جدید کاتالوگ |
| `SahandClinicPro-User-Guide-v2.0.pdf` | راهنمای کاربر — با فصل جدید قابلیت‌های ۲.۰ |
| `SahandClinicPro-Install-Network-Guide-v2.0.pdf` | راهنمای نصب و شبکه مبتدی‌پسند |
| `SahandClinicPro-VS-Guide-v2.0.pdf` | دانلود پروژه + Visual Studio + خروجی exe |
| `SahandClinicPro-Brochure-v2.0.pdf` | کاتالوگ ۴۱ صفحه‌ای — با تور ۶۴ فرم و اسلاید ۱۶ قابلیت |
| `SahandClinicPro-Poster-*.png` (۲) | پوستر ۵۰×۷۰ عمودی/افقی — محتوای ۲.۰ |
| `SahandClinicPro-Banner-*.png` (۲) | بیلبورد ۳×۱.۵ و استند ۱.۵×۳ — خوانا از دور |

> 💡 **به‌روزرسانی سرور لایسنس موجود:** محتوای ZIP را جایگزین کنید — مهاجرت‌های خودکار ستون‌ها و جدول‌ها هنگام نخستین ورود اجرا می‌شوند.

</div>
"""

ASSETS = [
    "SahandClinicPro-LicenseServer-v2.0.zip",
    "SahandClinicPro-User-Guide-v2.0.pdf",
    "SahandClinicPro-Install-Network-Guide-v2.0.pdf",
    "SahandClinicPro-VS-Guide-v2.0.pdf",
    "SahandClinicPro-Brochure-v2.0.pdf",
    "SahandClinicPro-Poster-Vertical-50x70-150dpi.png",
    "SahandClinicPro-Poster-Horizontal-70x50-150dpi.png",
    "SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png",
    "SahandClinicPro-Banner-Vertical-1.5x3m.png",
]


def api(url: str, method: str = "GET", data: bytes | None = None,
        content_type: str = "application/json", token: str = "") -> dict:
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {token}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Content-Type", content_type)
    req.add_header("User-Agent", "SahandClinicPro-Release-v20")
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
        "name": "🎯 نسخه ۲.۰.۰ — ۱۶ قابلیت منتخب سند ایده‌ها در ۶ محور",
        "body": BODY,
        "draft": False,
        "prerelease": False,
    }).encode()
    rel = api(f"{REPO_API}/releases", method="POST", data=payload, token=token)
    release_id = rel["id"]
    print(f"  ✔ رلیز ساخته شد — شناسه {release_id}")

    ok, failed = [], []
    for name in ASSETS:
        path = OUT / name
        if not path.exists():
            print(f"  ✖ یافت نشد: {path}")
            failed.append(name)
            continue
        size_mb = path.stat().st_size / (1024 * 1024)
        print(f"  … {name} ({size_mb:.1f} MB)")
        try:
            url = (f"https://uploads.github.com/repos/pranses2011/Sahand_ClinicPro/"
                   f"releases/{release_id}/assets?name={urllib.parse.quote(name)}")
            api(url, method="POST", data=path.read_bytes(),
                content_type="application/octet-stream", token=token)
            ok.append(name)
        except Exception as e:
            print(f"  ✖ خطا: {e}")
            failed.append(name)

    print(f"\n📊 نتیجه: {len(ok)} موفق، {len(failed)} ناموفق")
    if failed:
        for f in failed:
            print(f"   ✖ {f}")
        return 1
    print("🎉 رلیز کامل شد")
    return 0


if __name__ == "__main__":
    sys.exit(main())
