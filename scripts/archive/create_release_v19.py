#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🏁 ساخت رلیز گیت‌هاب v1.9.0 با یادداشت انتشار فارسی RTL + آپلود ۹ پیوست
چرخه دوازدهم: پنل لایسنس حرفه‌ای + پیام به داشبورد + ۲۱ گزارش نموداری + امنیت + تست دسکتاپ
"""
import json
import pathlib
import sys
import urllib.parse
import urllib.request

REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
TAG = "v1.9.0"
OUT = pathlib.Path("/home/z/my-project/download")

BODY = """<div dir="rtl" align="right">

# 🏁 نسخه ۱.۹.۰ — چرخه دوازدهم «استحکام + ارتقای تجربه»

**سامانه لایسنس PHP حرفه‌ای با پیام مستقیم به داشبورد درمانگاه + ۲۱ گزارش نموداری + سه گام امنیتی + تولد تست دسکتاپ** 🎉

| شاخص | مقدار |
|:---|:---|
| 🔐 پنل لایسنس | اطلاعات کامل درمانگاه + ویرایش + حذف امن دومرحله‌ای + داشبورد نموداری + ۵ گزارش + انیمیشن حرفه‌ای |
| 📨 پیام پشتیبانی | ارسال مستقیم از پنل روی داشبورد درمانگاه (۴ سطح اهمیت، تحویل تک‌بار با تپ‌قلب) |
| 📈 گزارش‌های نموداری | **۲۱ گزارش** — روند (سطحی)، مقایسه (ستونی)، سهم (دونات)، رتبه (میله‌ای افقی) |
| 🛡 امنیت | نگهبان کلید JWT در تولید + بکاپ خودکار شبانه + ممیزی ۱۹۱ endpoint |
| 🧪 تست‌ها | **۱۷۲ دات‌نت (۵ پروژه) + ۳۴ E2E لایسنس** — همه سبز |
| 🧪 تست دسکتاپ | پروژه جدید با FakeApiClient + ۶ تست جریان‌های پول‌ساز |
| 🎨 بنر و پوستر | بازطراحی مدرن ۴ فایل با عمق لایه‌ای واقعی (بدون کج‌کردن تصویر) |
| 💡 سند قابلیت‌ها | `docs/FEATURE-IDEAS.md` — ۴۳ پیشنهاد در ۸ محور برای بررسی مالک |
| 🖥 فرم‌های دسکتاپ | ۵۲ فرم WPF با Material Design RTL |
| ✅ وضعیت بیلد | ۰ خطا — سورس بدون هشدار |

---

## 🔐 سامانه لایسنس PHP — حرفه‌ای شد (درخواست مالک)

- **اطلاعات کامل درمانگاه** در هر لایسنس: نام مدیر، ایمیل، شهر، نشانی + فرم مشترک صدور/ویرایش
- **ویرایش کامل لایسنس**: درمانگاه/سقف‌ها/ماژول‌ها — اعمال خودکار با تپ‌قلب بعدی
- **حذف قطعی** با حفاظت دومرحله‌ای (تایپ دقیق کلید) + CASCADE + حسابرسی
- **داشبورد نموداری**: ۴ نمودار SVG خالص — روند ۱۴روزه اعتبارسنجی، دونات وضعیت، ستونی صدور شمسی، میله‌ای ماژول‌ها
- **صفحه گزارش‌ها**: نمای کلی درمانگاه‌ها، پوشش فعال‌سازی، لایسنس‌های خاموش، فعال‌ترین‌ها، توزیع نتیجه + CSV
- **انیمیشن حرفه‌ای**: شمارنده عددی فارسی، حلقه پیشرفت، ترسیم خط نمودار، قطاع پلکانی دونات، موج لمس، کره‌های شناور ورود

## 📨 پیام مستقیم به داشبورد درمانگاه

جریان کامل: پنل PHP → جدول `ls_messages` → تپ‌قلب روزانه کلاینت → ذخیره در تنظیمات محلی →
کارت «پیام‌های پشتیبانی» رنگ‌بندی‌شده (آبی/سبز/نارنجی/قرمز) بالای داشبورد دسکتاپ.
تأییدشده با ۳۴ تست E2E واقعی شامل تحویل تک‌بار.

## 📈 گزارش‌های نموداری — هر شاخص، نمودار درست خودش

| ماهیت داده | نوع نمودار | نمونه‌ها |
|:---|:---|:---|
| روند زمانی | سطحی (Area) | درآمد روزانه، بیمار جدید ماهانه، صدور نسخه |
| مقایسه | ستونی | ۱۲ ماه شمسی، شعبه‌ها، سن بدهی، رده سنی |
| سهم از کل | دونات | پزشکان، بیمه‌گران، روش پرداخت، جنسیت، وضعیت نوبت، حقوق درصدی پرسنل |
| رتبه‌بندی | میله‌ای افقی | خدمات پردرآمد، بدهکاران، آزمون‌ها، داروها |

## 🛡 سه گام امنیتی (فاز ۱ سند تحلیل)

1. **نگهبان کلید JWT** — در تولید، کلید پیش‌فرض/کوتاه باعث بالا نیامدن سرور با پیام فارسی می‌شود
2. **بکاپ خودکار** — جاب Hangfire ساعت ۲ بامداد + پاک‌سازی نسخه‌های فراتر از `Backup:RetentionCount`
3. **ممیزی ۱۹۱ endpoint** — ۱۸ مورد حساس فاقد مجوز پوشانده شد + ۱۲ خودخدمت با کامنت عمدت

## 📎 پیوست‌ها و خروجی‌ها

| فایل | شرح |
|:---|:---|
| `SahandClinicPro-LicenseServer-v1.9.zip` | سرور لایسنس PHP کامل برای آپلود روی cPanel (۱۷ فایل — دارای مهاجرت خودکار) |
| `SahandClinicPro-User-Guide-v1.9.pdf` | راهنمای کاربر — با فصل‌های جدید گزارش نموداری و پیام پشتیبانی |
| `SahandClinicPro-Install-Network-Guide-v1.9.pdf` | راهنمای نصب و شبکه مبتدی‌پسند |
| `SahandClinicPro-VS-Guide-v1.9.pdf` | دانلود پروژه + Visual Studio + خروجی exe |
| `SahandClinicPro-Brochure-v1.9.pdf` | کاتالوگ ۵۲ فرم — با اسلاید جدید ۲۱ گزارش نموداری |
| `SahandClinicPro-Poster-*.png` (۲) | پوستر ۵۰×۷۰ عمودی/افقی — طراحی مدرن لایه‌ای |
| `SahandClinicPro-Banner-*.png` (۲) | بیلبورد ۳×۱.۵ و استند ۱.۵×۳ — خوانا از دور |

> 💡 **به‌روزرسانی سرور لایسنس موجود:** محتوای ZIP را جایگزین کنید و یک‌بار در پنل وارد شوید —
> مهاجرت خودکار ستون‌ها و جدول `ls_messages` را می‌سازد؛ نیازی به دست‌زدن به داده‌ها نیست.

</div>
"""

ASSETS = [
    "SahandClinicPro-LicenseServer-v1.9.zip",
    "SahandClinicPro-User-Guide-v1.9.pdf",
    "SahandClinicPro-Install-Network-Guide-v1.9.pdf",
    "SahandClinicPro-VS-Guide-v1.9.pdf",
    "SahandClinicPro-Brochure-v1.9.pdf",
    "SahandClinicPro-Poster-Vertical-50x70-150dpi.png",
    "SahandClinicPro-Poster-Horizontal-70x50-150dpi.png",
    "SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png",
    "SahandClinicPro-Banner-Vertical-1.5x3m.png",
]


def get_token() -> str:
    import subprocess
    url = subprocess.run(
        ["git", "-C", "/home/z/my-project/Sahand_ClinicPro", "remote", "get-url", "origin"],
        capture_output=True, text=True, check=True).stdout.strip()
    return url.split("//")[1].split("@")[0]


def api(url: str, method: str = "GET", data: bytes | None = None,
        content_type: str = "application/json", token: str = "") -> dict:
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {token}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Content-Type", content_type)
    req.add_header("User-Agent", "SahandClinicPro-Release-v19")
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())


def main() -> int:
    token = get_token()
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
        "name": "🏁 نسخه ۱.۹.۰ — پنل لایسنس حرفه‌ای + پیام به داشبورد + ۲۱ گزارش نموداری + امنیت",
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
