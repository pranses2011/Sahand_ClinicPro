#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.6.0 در گیت‌هاب — تگ + یادداشت رلیز + بارگذاری زیپ آپدیت.

چرخهٔ چهل‌ویکم: لوازم مصرفی مستقل از ماژول داروخانه + میانبرهای داشبورد در
دیتابیس + میانبرهای پیش‌فرض ادمین + فیلتر نوع چاپ/گزارش‌ها + رفع جعبهٔ
جستجوی مردهٔ گزارش‌ساز → بیلد تمیز کامل سولوشن بدون هیچ خطا و هشدار.
"""
import json
import pathlib
import subprocess
import urllib.request

TOKEN = "[REDACTED:github_token]"
REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
ZIP = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.5.1-to-2.6.0.zip")
TAG = "v2.6.0"

# ── ۱) تگ روی HEAD (کامیت نسخه 9483122) و پوش آن ──
subprocess.run(f'git tag -f {TAG} main', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
subprocess.run(f'git push origin -f {TAG}', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True,
               capture_output=True, text=True)
print("✅ تگ", TAG, "پوش شد")

BODY = """## 🧰 v2.6.0 — چرخهٔ چهل‌ویکم: لوازم مصرفی مستقل از داروخانه + میانبرهای داشبورد در دیتابیس

> سه درخواست مالک با یک ریشهٔ مشترکِ معماری: **وابستگی‌های ناخواسته**. این چرخه هر سه گره را باز کرد. سولوشن کامل ۱۱ پروژه در بیلد تمیز از صفر **بدون هیچ خطا و هشداری** تأیید شد.

### 🧰 لوازم مصرفی مستقل از ماژول داروخانه (مهم‌ترین)
- **ریشه:** میدلور لایسنس پیشوند `/api/v1/pharmacy` را به ماژول Pharmacy گره زده بود ⇒ درمانگاه بدون آن لایسنس حتی برای سرنگ و گاز ۴۰۳ می‌گرفت
- **کنترلر مستقل `api/v1/consumables`** خارج از نقشهٔ ماژول لایسنس — با بازاستفادهٔ کامل Handlerهای چرخهٔ ۳۹ (هیچ منطق انباری دومی؛ فهرست بچ‌ها سمت سرور به مصرفی قفل است)
- **مجوزها:** فهرست/مصرف با `receptions.view/create` (مصرف سرنگ حین پذیرش خدمتی است — دیگر مجوز pharmacy.* به نقش پذیرش ندهید!)؛ تعریف قلم/ورود کالا (عملیات خرید) با نقش Admin/Manager
- **صفحهٔ جدید «لوازم مصرفی»** در منوی «پذیرش و درمان»: چیپ‌های خلاصه (اقلام/کمبود/انقضا/ارزش انبار) + جدول بچ‌ها با قیمت خرید/فروش + جستجوی زنده + سوییچ کمبود؛ دکمه‌های مدیریتی فقط برای Admin/Manager
- **پذیرش خدمتی و فرم‌های قلم/ورود کالا** به مسیر مستقل منتقل شدند — داروخانهٔ اصلی روی مسیر خودش ماند (یک انبار مشترک، دو منظر)

### 💾 میانبرهای داشبورد در دیتابیس
- جدول `UserShortcuts` + ایندکس یکتای فیلترشده + مهاجرت idempotent + اندپوینت‌های `GET/PUT users/me/shortcuts`
- اولویت بارگذاری: رکورد دیتابیس → مهاجرت خودکار کش محلی (چیدمان قبلی شما در نخستین ورود منتقل می‌شود) → **میانبرهای پیش‌فرض ادمین** (پذیرش/نوبت/بیماران/ویزیت/صندوق/گزارش‌ها) → جانشینی کش محلی در قطعی سرور
- گارد مسابقهٔ ذخیره/بارگذاری + لیست خالی = انتخاب آگاهانهٔ کاربر (پیش‌فرض برنمی‌گردد)

### 📊 فیلتر نوع چاپ/گزارش‌ها
- کمبوباکس «نوع گزارش»: همه / جدولی (چاپ/اکسل/PDF) / نموداری — پالایش کلایندی روی کاتالوگ کش‌شده
- **رفع جعبهٔ جستجوی مرده:** جستجوی گزارش‌ساز از ابتدا بایند بود ولی هیچ پالایشی انجام نمی‌داد — اکنون زنده است

### 📦 محتوا و نصب
**۲۶ فایل** — دلتا از v2.5.1. ⚠️ **هر دو پروژه (Desktop + API) تغییر دارند** + **یک مهاجرت دیتابیس** (جدول میانبرها) در نخستین اجرای سرور خودکار اعمال می‌شود.
> پس از جایگزینی: **Clean + Rebuild** سولوشن + **publish/ری‌استارت سرور API**.
> 🔎 سنجهٔ نصب موفق: «نسخهٔ ۲.۶.۰» زیر خروج + آیتم «لوازم مصرفی» در منو + فیلتر نوع در گزارش‌ها + میانبرهای پیش‌فرض ادمین.
چک‌لیست کامل در `Update-Guide-2.6.0.html` داخل بسته.
"""

# ── ۲) ساخت رلیز ──
payload = {
    "tag_name": TAG,
    "name": "v2.6.0 — چرخهٔ چهل‌ویکم: لوازم مصرفی مستقل از داروخانه + میانبرهای داشبورد در دیتابیس + فیلتر نوع گزارش‌ها",
    "body": BODY,
    "draft": False,
    "prerelease": False,
}
req = urllib.request.Request(
    f"{REPO_API}/releases",
    data=json.dumps(payload).encode(),
    headers={"Authorization": f"token {TOKEN}", "Accept": "application/vnd.github+json",
             "Content-Type": "application/json"},
    method="POST")
with urllib.request.urlopen(req) as r:
    release = json.load(r)
print("✅ رلیز ساخته شد:", release["html_url"], "| id:", release["id"])

# ── ۳) بارگذاری زیپ ──
upload_url = release["upload_url"].replace("{?name,label}", f"?name={ZIP.name}")
req = urllib.request.Request(
    upload_url,
    data=ZIP.read_bytes(),
    headers={"Authorization": f"token {TOKEN}", "Accept": "application/vnd.github+json",
             "Content-Type": "application/zip"},
    method="POST")
with urllib.request.urlopen(req) as r:
    asset = json.load(r)
print("✅ زیپ بارگذاری شد:", asset["name"], "|", asset["size"] // 1024, "KB")
print("\n🎉 رلیز v2.6.0 کامل شد:", release["html_url"])
