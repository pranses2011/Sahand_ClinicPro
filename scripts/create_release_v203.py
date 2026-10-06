#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🏁 ساخت رلیز گیت‌هاب v2.0.3 + بارگذاری بستهٔ آپدیت تجمعی — چرخه شانزدهم
==============================================================================
۱) ساخت رلیز v2.0.3 با یادداشت انتشار فارسی RTL (خودکار: Source code zip/tar.gz)
۲) بارگذاری دارایی «SahandClinicPro-Update-2.0.1-to-2.0.3.zip» در همین رلیز
   — بستهٔ تجمعی: همهٔ فایل‌های تغییرکرده از 2.0.1 به بعد (قاعدهٔ دائمی مالک:
   «برای تغییرات، فایل زیپ آپدیت بساز و در رلیز بزار تا فقط فایل‌هایی که
   تغییر کرده‌اند رو دانلود و جایگزین کنم»)؛ تجمعی‌شدن به این دلیل که سه
   گزارش پیاپی نشان داد جایگزینی بخشی از فایل‌ها روی دستگاه مالک خطاساز است
⚠️ توکن از متغیر محیطی GH_TOKEN خوانده می‌شود — هرگز در سورس هاردکد نمی‌شود
"""
import json
import os
import pathlib
import sys
import urllib.parse
import urllib.request

REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
TAG = "v2.0.3"
RELEASE_NAME = "🛡️ نسخهٔ ۲.۰.۳ — رفع کرش داشبورد + بستهٔ آپدیت تجمعی"
ASSET_PATH = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.0.1-to-2.0.3.zip")
ASSET_NAME = ASSET_PATH.name

TOKEN = os.environ.get("GH_TOKEN") or ""
if not TOKEN:
    raise SystemExit("متغیر محیطی GH_TOKEN تنظیم نشده است — توکن در سورس هاردکد نمی‌شود")

BODY = """<div dir="rtl" align="right">

# 🛡️ نسخهٔ ۲.۰.۳ — رفع کرش داشبورد + بستهٔ آپدیت تجمعی

**نخستین باز شدن داشبورد دیگر با XamlParseException نمی‌ریزد** + برای قطع‌شدن چرخهٔ «فایل‌های نیمه‌جایگزین‌شده»، بستهٔ این رلیز **تجمعی** است: همهٔ فایل‌های تغییرکرده از ۲.۰.۱ تا امروز در یک ZIP 🎉

## 📥 مهم‌ترین بخش این رلیز — بستهٔ آپدیت تجمعی

| فایل | توضیح |
|:---|:---|
| `SahandClinicPro-Update-2.0.1-to-2.0.3.zip` | **همهٔ فایل‌های تغییرکرده از نسخهٔ ۲.۰.۱ تا ۲.۰.۳** با همان ساختار پوشه‌ای + راهنمای فارسی نصب (`Update-Guide-2.0.3.html`) + فهرست فایل‌ها (`changed-files.txt`) — جایگزین کامل بستهٔ ۲.۰.۲ و وصلهٔ کوچک قبلی |

**نصب در یک خط:** ویژوال استودیو را ببندید → بسته را باز کنید → همه را انتخاب (<kbd>Ctrl+A</kbd>) → روی پوشهٔ سورس فعلی خود پیست کنید → **Replace the files** → `Rebuild Solution` → اجرا.

> ⚠️ **این بار همهٔ فایل‌ها را با هم جایگزین کنید، نه بخشی را.** خطاهای اخیر (CS0103 و دوباره‌دیده‌شدن نشانه‌های رفع‌شده) ناشی از جایگزینی ناقص بود؛ این بسته تجمعی است تا یک Replace کامل همهٔ فایل‌ها را یکدست کند. فایل `appsettings.json` (تنظیم SQL شما)، دیتابیس و رمزِ تغییرکردهٔ شما دست نمی‌خورند.

## 📊 چه اتفاقی افتاده بود؟ (گزارش دهم مالک)

پس از ثبت موفق رمز جدید، باز شدن داشبورد با خطای `XamlParseException: Cannot find resource named 'BoolToVis'` در `DashboardView.xaml` خط ۱۱۱ می‌ریخت. ریشه: **نقص واقعی مخزن که از چرخه چهاردهم به جا مانده بود** — داشبورد در ۹ نقطه به مبدل `BoolToVis` ارجاع می‌داد اما این کلید نه در خود فایل تعریف شده بود و نه در App.xaml؛ `StaticResource` فقط از منابع «همان فایل» یا App.xaml حل می‌شود و تعریف در ۱۳ فایل دیگر پروژه برای داشبورد بی‌اثر بود. چرا بیلد ۰ خطا و ۲۶۹ تست سبز آن را نگرفتند؟ اعتبارسنجی StaticResource اصلاً در بیلد وجود ندارد و تست ممیزی XAML کلیدها را «سراسری» جمع می‌کرد. این بمب ساعتی پنهان مانده بود چون داشبورد تا امروز هیچ‌وقت باز نشده بود.

**رفع این نسخه:**
- 📊 مبدل گمشده `BoolToVis` به داشبورد اضافه شد (همان الگوی ۱۳ فایل دیگر پروژه)
- 🛡️ **گارد تستی جدید**: هر فایل XAML جداگانه در برابر فقط منابع خودش + App.xaml چک می‌شود — این کلاس خطا دیگر از چشم تست‌ها فرار نمی‌کند
- 🔖 نسخه به 2.0.3 ارتقا یافت — **پانویس پنجرهٔ ورود باید «نسخه ۲.۰.۳» را نشان دهد** = سریع‌ترین راه تشخیص نصب موفق

## 🔐 دربارهٔ «نامعتبر بودن رمز»

طبیعی است! رمز شما در همان نشست قبلی (پیش از کرش داشبورد) با موفقیت عوض شد — `Admin@123` دیگر معتبر نیست؛ **با رمز جدیدی که انتخاب کردید وارد شوید.** اگر در آزمایش‌ها ۵ بار اشتباه زده‌اید و حساب قفل شده:
```sql
UPDATE Users SET FailedLoginAttempts = 0, LockoutEnd = NULL WHERE Username = 'admin';
```

## 📋 همهٔ رفع‌های جمع‌شده در این بسته (از ۲.۰.۱ تاکنون)

| گزارش مالک | رفع |
|:---|:---|
| دهم | **کرش باز شدن داشبورد (BoolToVis گمشده)** + گارد تستی scope-محور StaticResource + ارتقای نسخه |
| دومی تا چهارم | راهنمای SQL تقویت‌شده + فیلتر ایندکس 10735 + پورت ثابت 5000 سرور |
| پنجم | خطای 404 کل ارتباط + باز شدن پاکت پاسخ API + نمایش درست رمز (LTR/وسط‌چین/چشم بیرون کادر) + جریان تغییر رمز اجباری |
| ششم | خطای ۵۰۰ لاگین — ترکیب ناسازگار «تلاش مجدد خودکار EF + تراکنش دستی» |
| هفتم | خطای «اطلاعات ارسالی نامعتبر است» — payload سه‌فیلدی + چشم هر سه کادر |
| هشتم | فونت وزیرمتن بومی (جاسازی داخل exe) |
| نهم | وصلهٔ جفت فایل پنجرهٔ تغییر رمز (در این بسته تجمعی هم گنجانده شده) |

## ✅ تأیید نهایی

- بیلد Release کامل سولوشن: **۰ خطا**
- کل تست‌ها: **۲۷۰/۲۷۰ سبز** (شامل گارد جدید scope-محور)
- ممیزی scope-محور هر ۷۱ فایل XAML: **صفر ارجاع بی‌تعریف**

> 💡 **دارایی‌های راهنما** (راهنمای کاربر/نصب/VS/SQL، بروشور، پوستر و بنر) در رلیزهای v2.0.0 و v2.0.1 **همچنان معتبرند** — نسخهٔ ۲.۰.۳ وصلهٔ پایداری است و قابلیت جدیدی به مستندات اضافه نکرده است.

</div>
"""


def api(url: str, method: str = "GET", data: bytes | None = None,
        content_type: str = "application/json", raw: bool = False):
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {TOKEN}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Content-Type", content_type)
    req.add_header("User-Agent", "SahandClinicPro-Release-v203")
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
        raise SystemExit(f"⛔ بستهٔ آپدیت پیدا نشد: {ASSET_PATH} — ابتدا create_update_zip_v203.py را اجرا کنید")
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
    print("🎉 رلیز v2.0.3 کامل شد (بستهٔ تجمعی + سورس خودکار)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
