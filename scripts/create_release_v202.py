#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🏁 ساخت رلیز گیت‌هاب v2.0.2 + بارگذاری بستهٔ آپدیت — چرخه پانزدهم
==============================================================================
۱) ساخت رلیز v2.0.2 با یادداشت انتشار فارسی RTL (خودکار: Source code zip/tar.gz)
۲) بارگذاری دارایی «SahandClinicPro-Update-2.0.1-to-2.0.2.zip» در همین رلیز
   — بستهٔ آپدیت فقط فایل‌های تغییرکرده از 2.0.1 به بعد را دارد (درخواست مالک:
   «برای تغییرات، فایل زیپ آپدیت بساز و در رلیز بزار تا فقط فایل‌هایی که
   تغییر کرده‌اند رو دانلود و جایگزین کنم»)
⚠️ توکن از متغیر محیطی GH_TOKEN خوانده می‌شود — هرگز در سورس هاردکد نمی‌شود
"""
import json
import os
import pathlib
import sys
import urllib.parse
import urllib.request

REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
TAG = "v2.0.2"
RELEASE_NAME = "🔤 نسخهٔ ۲.۰.۲ — فونت وزیرمتن بومی + نخستین بستهٔ آپدیت"
ASSET_PATH = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.0.1-to-2.0.2.zip")
ASSET_NAME = ASSET_PATH.name

TOKEN = os.environ.get("GH_TOKEN") or ""
if not TOKEN:
    raise SystemExit("متغیر محیطی GH_TOKEN تنظیم نشده است — توکن در سورس هاردکد نمی‌شود")

BODY = """<div dir="rtl" align="right">

# 🔤 نسخهٔ ۲.۰.۲ — فونت وزیرمتن بومی + نخستین بستهٔ آپدیت

**فونت وزیرمتن از این نسخه داخل خود exe جاسازی شده — دیگر هیچ دستگاهی به نصب فونت نیاز ندارد** + همهٔ رفع‌های پس از v2.0.1 در یک بستهٔ آپدیت جمع شدند 🎉

## 📥 مهم‌ترین بخش این رلیز — بستهٔ آپدیت (درخواست مالک)

| فایل | توضیح |
|:---|:---|
| `SahandClinicPro-Update-2.0.1-to-2.0.2.zip` | **همهٔ ۱۲۳ فایل تغییرکرده از نسخهٔ ۲.۰.۱ به بعد** با همان ساختار پوشه‌ای + راهنمای فارسی نصب (`Update-Guide-2.0.2.html`) + فهرست فایل‌ها (`changed-files.txt`) |

**نصب در یک خط:** بسته را باز کنید → همه را انتخاب (<kbd>Ctrl+A</kbd>) → روی پوشهٔ سورس فعلی خود پیست کنید → Replace the files → `Rebuild Solution` → اجرا. فایل `appsettings.json` (تنظیم SQL شما) و دیتابیس ساخته‌شده دست نمی‌خورند.

از این پس **هر رلیز جدید، بستهٔ آپدیت فقط با فایل‌های تغییرکردهٔ همان رلیز** خواهد داشت — لازم نیست کل سورس را دوباره دانلود کنید.

## 🔤 چه اتفاقی افتاده بود؟ (گزارش هشتم مالک)

فونت وزیر در **هیچ‌کدام از فرم‌ها** اعمال نمی‌شد. ریشه: هر ۶۷ ارجاع فونت در فرم‌ها با «نام خانواده» کار می‌کرد (`FontFamily="Vazirmatn, ..."`) و WPF نام ساده را **فقط در میان فونت‌های نصب‌شدهٔ ویندوز** جستجو می‌کند — دستگاه‌های درمانگاه وزیرمتن ندارند → همه‌جا سقوط به Segoe UI/Tahoma. این کمبود از ابتدای پروژه وجود داشت اما تا اجرای واقعی روی ویندوز مالک پنهان مانده بود.

**رفع سه‌لایه:**
- **جاسازی در برنامه** — ۴ وزن TTF وزیرمتن (Regular/SemiBold/Bold/Black — مجوز آزاد OFL) به‌عنوان `Resource` داخل اسمبلی دسکتاپ (~۴۸۰KB)
- **اتصال همهٔ ارجاع‌ها** — سبک سراسری `App.xaml` + هر ۶۷ ارجاع در ۶۸ فایل به `pack://application:,,,/Assets/Fonts/#Vazirmatn` تبدیل شد؛ WPF حالا فونت را از داخل خود exe می‌خواند
- **مرکز آموزش** — WebView2 (کرومیوم) فقط فونت نصب‌شده یا `@font-face` می‌بیند؛ دو بلوک `@font-face` با woff2 پایه‌۶۴ (وزن ۴۰۰/۷۰۰) داخل قالب HTML درس‌ها جاسازی شد

## 📋 همهٔ رفع‌های جمع‌شده در این بسته (از ۲.۰.۱ تاکنون)

| گزارش مالک | رفع |
|:---|:---|
| دومی تا چهارم | راهنمای SQL تقویت‌شده (`dotnet tool restore` + خطای SEQUENCE + فیلتر ایندکس 10735) + پورت ثابت 5000 سرور (`launchSettings.json`) + حذف نویزهای گمراه‌کننده کنسول |
| پنجم | خطای 404 کل ارتباط برنامه↔سرور (نرمال‌سازی آدرس پایه) + باز شدن پاکت پاسخ API + نمایش درست رمز (LTR + وسط‌چین + چشم بیرون کادر) + جریان کامل تغییر رمز اجباری |
| ششم | خطای ۵۰۰ لاگین — ترکیب ناسازگار «تلاش مجدد خودکار EF + تراکنش دستی» حذف شد |
| هفتم | خطای «اطلاعات ارسالی نامعتبر است» — payload سه‌فیلدی تغییر رمز + چشم برای هر سه کادر + نمایش ریشهٔ واقعی خطا |
| هشتم | فونت وزیرمتن بومی (این نسخه) |

## ✅ تأیید نهایی

- بیلد Release کامل سولوشن: **۰ خطا**
- کل تست‌ها: **۲۶۹/۲۶۹ سبز** (شامل گارد `XamlResourceAuditTests` روی هر ۷۰ فایل XAML)
- بستهٔ آپدیت: ۱۲۳ فایل (۲۲ جدید + ۱۰۱ ویرایش، بدون حذف) — با گارد ایمنی که `appsettings.json` هرگز در بسته نباشد

## 🧭 بعد از نصب

ورود با `admin` / `Admin@123` ← پنجرهٔ تغییر رمز اجباری (هر سه کادر با آیکون چشمِ **بیرون از کادر**) ← ثبت ← داشبورد. اگر در آزمایش‌های قبلی ۵ بار رمز اشتباه زده‌اید و حساب قفل شده:
```sql
UPDATE Users SET FailedLoginAttempts = 0, LockoutEnd = NULL WHERE Username = 'admin';
```

> 💡 **دارایی‌های راهنما** (راهنمای کاربر/نصب/VS/SQL، بروشور، پوستر، بنر و ZIP سرور لایسنس) در رلیزهای v2.0.0 و v2.0.1 **همچنان معتبرند** — این نسخه فونت و رفع‌های اتصال را شامل می‌شود و قابلیت جدیدی به مستندات اضافه نکرده است.

</div>
"""


def api(url: str, method: str = "GET", data: bytes | None = None,
        content_type: str = "application/json", raw: bool = False):
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {TOKEN}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Content-Type", content_type)
    req.add_header("User-Agent", "SahandClinicPro-Release-v202")
    with urllib.request.urlopen(req) as resp:
        payload = resp.read()
        if not payload:  # DELETE با 204 و بدنه خالی
            return None
        return payload if raw else json.loads(payload.decode())


def upload_asset(release: dict) -> None:
    # idempotent — حذف دارایی هم‌نام قبلی
    for asset in release.get("assets", []):
        if asset["name"] == ASSET_NAME:
            api(asset["url"], method="DELETE")
            print(f"  ↻ دارایی قبلی {ASSET_NAME} حذف شد")
            break
    upload_base = release["upload_url"].split("{")[0]
    url = f"{upload_base}?name={urllib.parse.quote(ASSET_NAME)}"
    data = ASSET_PATH.read_bytes()
    api(url, method="POST", data=data, content_type="application/zip", raw=True)
    print(f"  ✔ {ASSET_NAME} آپلود شد ({len(data)/1024:.0f} KB)")


def main() -> int:
    if not ASSET_PATH.is_file():
        raise SystemExit(f"⛔ بستهٔ آپدیت پیدا نشد: {ASSET_PATH} — ابتدا create_update_zip_v202.py را اجرا کنید")
    print(f"🏁 ساخت رلیز {TAG}")

    # idempotency — حذف رلیز/تگ قبلی اگر موجود باشد
    try:
        rel = api(f"{REPO_API}/releases/tags/{TAG}")
        api(f"{REPO_API}/releases/{rel['id']}", method="DELETE")
        print("  ↻ رلیز قبلی حذف شد")
    except Exception:
        print("  ✔ رلیز قبلی وجود نداشت")
    try:
        api(f"{REPO_API}/git/refs/tags/{TAG}", method="DELETE")
        print("  ↻ تگ قبلی حذف شد")
    except Exception:
        pass

    main_sha = api(f"{REPO_API}/git/ref/heads/main")["object"]["sha"]
    print(f"  ✔ main روی {main_sha[:7]}")
    payload = json.dumps({
        "tag_name": TAG,
        "target_commitish": main_sha,
        "name": RELEASE_NAME,
        "body": BODY,
        "draft": False,
        "prerelease": False,
    }).encode()
    rel = api(f"{REPO_API}/releases", method="POST", data=payload)
    print(f"  ✔ رلیز ساخته شد — شناسه {rel['id']}")
    print(f"  🔗 {rel['html_url']}")

    upload_asset(rel)
    print("🎉 رلیز v2.0.2 کامل شد (بستهٔ آپدیت + سورس خودکار)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
