#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.11.0 در گیت‌هاب — تگ + Release + آپلود بستهٔ ZIP تجمعی.
نکتهٔ حیاتی (چرخهٔ ۲۲): آپلود asset باید به uploads.github.com برود،
نه api.github.com (وگرنه 404 می‌دهد).
"""
import json
import pathlib
import subprocess
import urllib.request

TOKEN = "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"
REPO = "pranses2011/Sahand_ClinicPro"
TAG = "v2.11.0"
ASSET = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-Cumulative-2.6.1-to-2.11.0.zip")

BODY = """## 📦 نسخهٔ ۲.۱۱.۰ — چرخهٔ چهل‌وهفتم (۱۲ گزارش مالک)

> **بستهٔ تجمعی ۲.۶.۱ → ۲.۱۱.۰** — همهٔ فایل‌های تغییرکرده از ۲.۶.۱؛ حتی اگر بسته‌های میانی را نصب نکرده باشید همین یک بسته کافی است. در راهنمای داخل بسته ⭐ مرحلهٔ «حذف bin/obj» حیاتی است.

### 🆕 صفحهٔ مستقل «مدیریت شیفت‌ها»
- کل منطق شیفت‌ها/تخصیص‌ها از تب سوم حضور و غیاب به صفحهٔ مستقل خودش منتقل شد (منوی کناری ← کادر درمان و پرسنل)
- خطای «ثبت شیفت ناموفق بود»: حالا پیام واقعی علت (مجوز attendance.shifts / اعتبارسنجی) نمایش داده می‌شود

### 🛠️ رفع‌های حسابداری (سود و زیان همیشه زیان بود!)
- **فرمول مانده معکوس بود** — درآمد و هزینه هر دو منفی می‌شدند و «سود خالص = هزینه − درآمد»؛ اصلاح شد
- **مبالغ سطرهای سند ذخیره نمی‌شدند** — ریشهٔ مشترک با «ظرفیت برنامهٔ کاری پاک می‌شد»: مبدل ارقام فارسی در مسیر برگشت بی‌اثر بود؛ دوسویه شد
- **دکمهٔ ثبت سند پس از تایپ مبالغ غیرفعال می‌ماند** — جمع‌ها/توازن فقط با حذف سطر بازمحاسبه می‌شدند؛ اکنون زنده‌اند
- **دابل‌کلیک روی سند دفتر روزنامه** = پنجرهٔ جزئیات کامل (سطرها با کد و عنوان حساب + جمع‌ها + وضعیت)
- رادیوهای ماهیت حساب (بدهکار/بستانکار) هیچ‌وقت کار نمی‌کردند — رفع شد
- بازطراحی حرفه‌ای کامل صفحه با زبان طراحی مشترک برنامه + صورت سود و زیان ساختاریافته (سود سبز / زیان قرمز)

### 🛠️ رفع‌های دیگر گزارش‌شده
- **ظرفیت برنامهٔ کاری پزشک:** مقدار با ترک سلول پاک می‌شد (مبدل دوسویه) + وسط‌چین سرستون‌ها و سلول‌ها
- **لاگ عملیات:** تغییر دسترسی‌های نقش حالا «فهرست مجوزهای قبلی» را ثبت می‌کند (نه آی‌دی/نام نقش) — به‌همراه برنامهٔ کاری پزشک در ویرایش‌ها
- **توست:** جابه‌جایی ساختاری به ردیف میزکار — دیگر هرگز زیر نوار وضعیت نمی‌رود + کارت سفید قطعی (روی هر تمی پیدا)
- **تولتیپ‌ها:** جفت رنگ قطعی سرمه‌ای/سفید مستقل از تم
- **دکمهٔ فراخوان بیمار بعدی** (ویزیت + میزکار پزشک): کف ارتفاع ۴۶px — دیگر له نمی‌شود
- **بیمهٔ پرونده در پذیرش:** با انتخاب بیمار خودکار تنظیم می‌شود + توست تأیید دیدنی
- **کمبوباکس‌های جستجوشونده:** با فوکوس/کلیک، فهرست باز می‌شود

### 🧱 کاشی میانبرهای داشبورد — ضدگلوله + تشخیص بیلد
- تحلیل پیکسلی تصاویر نشان داد رندر ایستگاه شما با فایل‌های پیش از ۲.۹ است هرچند نسخه به‌روز اعلام شده (کش بیلد)؛ کاشی با ارتفاع قطعی مقاوم‌سازی شد
- **نشان «نسخه ۲.۱۱.۰» روی داشبورد اضافه شد** — اگر پس از نصب این نشان نبود، بیلد شما کهنه است؛ راهنمای داخل بسته را دقیق اجرا کنید (حذف bin/obj + Rebuild)

**بیلد:** ۰ خطا / ۰ هشدار — تست‌ها: ۲۷۷/۲۷۷ پاس (۱۱۴ کاربرد + ۲۸ دامنه + ۱۱۱ یکپارچگی + ۲۴ API)
"""

HEADERS = {
    "Authorization": f"token {TOKEN}",
    "Accept": "application/vnd.github+json",
    "User-Agent": "sahand-release-script",
}


def api(url, data=None, method="GET", raw=None, extra_headers=None):
    headers = dict(HEADERS)
    if extra_headers:
        headers.update(extra_headers)
    req = urllib.request.Request(url, data=(raw if raw is not None else
                                            (json.dumps(data).encode() if data else None)),
                                 headers=headers, method=method)
    with urllib.request.urlopen(req) as resp:
        body = resp.read()
        return resp.status, json.loads(body) if body and not raw else (resp.status, body)


def main():
    # ۱) ساخت تگ سبک (lightweight) روی HEAD فعلی
    subprocess.run(["git", "tag", "-f", TAG], cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
    subprocess.run(["git", "push", "origin", TAG, "--force"], cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
    print(f"✓ تگ {TAG} پوش شد")

    # ۲) ساخت Release
    status, body = api(
        f"https://api.github.com/repos/{REPO}/releases",
        data={"tag_name": TAG, "name": "نسخهٔ ۲.۱۱.۰ — چرخهٔ چهل‌وهفتم (۱۲ گزارش مالک)",
              "body": BODY, "draft": False, "prerelease": False},
        method="POST")
    if status not in (200, 201):
        # شاید رلیز با این تگ از قبل هست — ویرایش می‌کنیم
        status, body = api(f"https://api.github.com/repos/{REPO}/releases/tags/{TAG}")
        rel = json.loads(body)
        rid = rel["id"]
        status, _ = api(f"https://api.github.com/repos/{REPO}/releases/{rid}",
                        data={"body": BODY}, method="PATCH")
        release = {"id": rid}
        print(f"~ رلیز موجود به‌روزرسانی شد (id={rid})")
    else:
        release = json.loads(body if isinstance(body, (bytes, bytearray)) else json.dumps(body))
    rid = release["id"]
    print(f"✓ Release ساخته شد (id={rid})")

    # ۳) آپلود بستهٔ ZIP — به uploads.github.com (نه api!)
    upload_url = f"https://uploads.github.com/repos/{REPO}/releases/{rid}/assets?name={ASSET.name}"
    status, body = api(upload_url, method="POST", raw=ASSET.read_bytes(),
                       extra_headers={"Content-Type": "application/zip"})
    print(f"✓ ZIP آپلود شد: status={status}")

    # ۴) لینک رلیز
    print(f"\\n🚀 رلیز منتشر شد: https://github.com/{REPO}/releases/tag/{TAG}")


if __name__ == "__main__":
    main()
