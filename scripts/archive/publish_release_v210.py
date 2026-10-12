#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.1.0 در گیت‌هاب — تگ + Release + آپلود بستهٔ ZIP.
نکتهٔ حیاتی (چرخه ۲۲): آپلود asset باید به uploads.github.com برود،
نه api.github.com (وگرنه 404 می‌دهد).
"""
import json
import os
import pathlib
import urllib.request

TOKEN = "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"
REPO = "pranses2011/Sahand_ClinicPro"
TAG = "v2.1.0"
ASSET = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.0.9-to-2.1.0.zip")

BODY = """## 📦 نسخهٔ ۲.۱.۰ — چرخه بیست‌وسوم (گزارش شانزدهم مالک — رفع ~۳۹ مورد)

### ✨ قابلیت‌های جدید
- 🖨️ **سه طرح چاپ نسخه** (کلاسیک/مدرن/فشرده) با انتخابگر زنده در پیش‌نمایش + یادآوری انتخاب
- 🏥 **تعریف/ویرایش بیمه و تعرفه** در صفحهٔ بیمه (فرم‌های جدید + ستون نوع/وضعیت)
- 💊 **~۱۱۰ تداخل دارویی جدید** (۱۰ گروه درمانی: DOAC/استاتین/دیابت/QT/…) + **۳۳ داروی جدید** (ریواروکسابان، تاکرولیموس، لینزولید و…) — با دکمه‌های «بروزرسانی» قابل دریافت
- 👔 **صفحهٔ مستقل پرسنل** + منوی گروهی کامل (عنوان والد همیشه نمایان + رنگ متمایز زیرمنوها + لوگوی برداری)
- ⌨️ کلید **Enter = جستجو** در ۷ کادر جستجوی برنامه

### 🛠️ رفع‌های مهم
- 💊 **خطای «غیرمنتظره در سرور» ذخیرهٔ تمپلیت نسخه** — ریشهٔ واقعی: کلید خارجی الزامی DoctorId؛ اختیاری شد + مهاجرت تضمینی
- 🖨️ صفحهٔ چاپ **دکمهٔ پرینتر ندارد + قالب بهم‌ریخته** → پنجرهٔ اختصاصی با دکمهٔ چاپ + LineBreak واقعی + خط‌کشی سلول‌ها + ارقام فارسی + **دکمهٔ چاپ مجدد** در جزئیات نسخه
- 🎨 **فونت/اندازه اعمال نمی‌شد** — استایل ضمنی Window به پنجره‌های مشتق‌شده نمی‌رسید! کلاس‌هندلر سراسری با DynamicResource (زنده، بدون ری‌استارت)
- 🎨 **رنگ منو تا ری‌استارت** — قلم‌های XAML فریزشده بودند؛ قلم گرادیانی نو با DynamicResource
- 🚫 **خطای 429** — سقف نرخ API از ۱۰۰ به ۶۰۰ در دقیقه
- 🏥 **بیمهٔ آزاد** — گزینهٔ «آزاد (بدون بیمه)» در پذیرش/صورتحساب؛ رفع ثبت آسیا به‌عنوان آزاد
- 🔢 ارقام فارسی سراسری (۳۴۵ ستون ۳۶ جدول) + واحد پول تومان/ریال بی‌ری‌استارت
- 🕐 انتخابگر ساعت عقربه‌ای طرح Material + انتخابگر تاریخ در همهٔ فیلدها
- 🩺 ستون‌های سن/آخرین مراجعه + تب‌های واقعی پروندهٔ بیمار + جستجوی جزئی پذیرش + رفع قفل پروندهٔ بالینی
- 💾 پشتیبان واقعی + نام فایل شمسی + ZIP + بازیابی + انتخاب دستی
- 🧹 همهٔ هشدارهای بیلد (۰ خطا / ۰ هشدار) + رفع رگرسیون PDF گزارش‌ها

### ⚠️ نکتهٔ نصب
شامل مهاجرت `Cycle23TemplateDoctorNullable` است که با اجرای سرور API خودکار اعمال می‌شود. راهنمای کامل فارسی داخل ZIP است.

**بیلد:** ۰ خطا / ۰ هشدار — تست‌ها: ۱۱۴ اپلیکیشن + ۱۰۲ زیرساخت + ۴۴ دسکتاپ پاس
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
    subprocess_run = __import__("subprocess").run
    subprocess_run(["git", "tag", "-f", TAG], cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
    subprocess_run(["git", "push", "origin", TAG, "--force"], cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
    print(f"✓ تگ {TAG} پوش شد")

    # ۲) ساخت Release
    status, body = api(
        f"https://api.github.com/repos/{REPO}/releases",
        data={"tag_name": TAG, "name": f"نسخهٔ ۲.۱.۰ — چرخه بیست‌وسوم (گزارش شانزدهم مالک)",
              "body": BODY, "draft": False, "prerelease": False},
        method="POST")
    release = json.loads(body if isinstance(body, (bytes, bytearray)) else json.dumps(body))
    if status not in (200, 201):
        # شاید رلیز با این تگ از قبل هست — ویرایش می‌کنیم
        status, body = api(f"https://api.github.com/repos/{REPO}/releases/tags/{TAG}")
        rel = json.loads(body)
        rid = rel["id"]
        status, _ = api(f"https://api.github.com/repos/{REPO}/releases/{rid}",
                        data={"body": BODY}, method="PATCH")
        release = {"id": rid}
        print(f"~ رلیز موجود به‌روزرسانی شد (id={rid})")
    rid = release["id"]
    print(f"✓ Release ساخته شد (id={rid})")

    # ۳) آپلود بستهٔ ZIP — به uploads.github.com (نه api!)
    upload_url = f"https://uploads.github.com/repos/{REPO}/releases/{rid}/assets?name={ASSET.name}"
    status, body = api(upload_url, method="POST", raw=ASSET.read_bytes(),
                       extra_headers={"Content-Type": "application/zip"})
    print(f"✓ ZIP آپلود شد: status={status}")

    # ۴) لینک رلیز
    print(f"\n🚀 رلیز منتشر شد: https://github.com/{REPO}/releases/tag/{TAG}")


if __name__ == "__main__":
    main()
