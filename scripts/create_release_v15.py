#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🏁 ساخت رلیز گیت‌هاب v1.5.0 با یادداشت انتشار فارسی RTL
"""
import json
import sys
import urllib.request

TOKEN = "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"
REPO = "pranses2011/Sahand_ClinicPro"

BODY = """<div dir="rtl" align="right">

# 🏁 نسخه ۱.۵.۰ — چرخه هفتم توسعه

**ماژول نظرسنجی بیماران — تکمیل چرخه ۳۴ فرمی پرامپت‌های UI (۳۴/۳۴)** 🎉

| شاخص | مقدار |
|:---|:---|
| 📝 ماژول جدید | نظرسنجی بیماران (فرم ۳۴) — نمای دسکتاپ + ۳ endpoint جدید |
| 🔌 endpoint های نظرسنجی | ۴ ← **۷** |
| 🖥 صفحات دسکتاپ | ۲۹ ← **۳۰** |
| 🧪 تست‌های سبز | ۱۲۹ ← **۱۳۳** |
| 🔐 مجوز جدید | surveys.manage + surveys.view |
| ✅ وضعیت بیلد | ۰ خطا / ۰ هشدار |

---

## 📝 ماژول نظرسنجی بیماران

### نمای دسکتاپ SurveyView — دو تب مطابق پرامپت فرم ۳۴:

**📋 لیست نظرسنجی‌ها:**
- جدول ۵ستونی (عنوان / نوع / وضعیت / تعداد پاسخ / میانگین امتیاز)
- فرم ایجاد و ویرایش با **سازنده پویا سوالات** — سه نوع سوال: امتیاز ۱-۵ / متن آزاد / چندگزینه‌ای
- گزینه‌های چندگزینه‌ای با جداکننده «|» — فیلد فقط برای نوع چندگزینه‌ای ظاهر می‌شود
- دکمه فعال/غیرفعال‌سازی و ویرایش سریع

**📊 نتایج و آمار:**
- ۳ کارت آماری: کل پاسخ‌ها / میانگین کل / پاسخ ۳۰ روز اخیر
- **توزیع ستاره‌های ۵ تا ۱** با نوار مقایسه‌ای
- **میانگین امتیاز به تفکیک پزشک** با نوار نسبی و نام کامل پزشک
- ۲۰ نظر آزاد اخیر با نام بیمار و تاریخ شمسی

### API نظرسنجی (۴ → ۷ endpoint):

| endpoint | شرح |
|:---|:---|
| `GET /surveys/{id}/results` 🆕 | کارت‌های خلاصه + توزیع ستاره + میانگین هر پزشک + نظرات اخیر با تاریخ شمسی |
| `PUT /surveys/{id}` 🆕 | ویرایش کامل با اعتبارسنجی — پاسخ‌های ثبت‌شده حفظ می‌شوند |
| `POST /surveys/{id}/send-links` 🆕 | ارسال لینک پیامکی به بیماران ویزیت‌شده امروز از طریق صف Hangfire |

### 💬 ارسال لینک پیامکی:
دکمه «📤 ارسال لینک به ویزیت‌های امروز» برای همه بیماران ویزیت‌شده امروزِ موبایل‌دار یک پیامک شخصی‌سازی‌شده (با نام بیمار و لینک نظرسنجی) در **صف پیامک** قرار می‌دهد که پردازشگر پس‌زمینه ارسال می‌کند — بدون بلاک‌شدن رابط کاربری.

### 🔐 مجوزهای ویژه:
- `surveys.manage` — ایجاد / ویرایش / فعال‌سازی / ارسال لینک
- `surveys.view` — مشاهده فهرست و نتایج

## 🧹 رفع باگ جانبی
- هشدار nullable در ارسال یادآوری نوبت — بیمار بدون موبایل معتبر از ارسال skip می‌شود

## 🧪 تست‌ها
- ۴ تست یکپارچگی جریان نظرسنجی: ایجاد/ویرایش + صحت محاسبات چندپزشکه (میانگین ۳ پاسخ ≈ ۳.۳۳؛ پزشک الف ۴.۵ / پزشک ب ۱.۰) + رد پاسخ روی غیرفعال + صف پیامک ارسال لینک
- مجموع ۱۲۹ ← **۱۳۳ تست سبز** در ۴ پروژه

---

## 📦 پیوست‌های دانلود (۹ مورد)

| # | پیوست | کاربرد | حجم |
|:---:|:---|:---|:---:|
| ۱ | `SahandClinicPro-Setup-v1.5.0.zip` | 📦 بسته خودکفای تولید نصب‌کننده — سورس کامل v1.5.0 + اسکریپت‌های Inno Setup فارسی؛ روی ویندوز `BUILD-INSTALLER.bat` → `SahandClinicPro-Setup-v1.5.0.exe` | ~۱.۸MB |
| ۲ | `Sahand-LicenseServer-cPanel-v1.1.zip` | 🔐 سرور لایسنس PHP خالص برای هاست cPanel — نصب ۳ مرحله‌ای بدون هیچ پیش‌نیاز | ~۴۰KB |
| ۳ | `SahandClinicPro-Install-Network-Guide-v1.5.pdf` | 📗 راهنمای کامل نصب و شبکه — ۱۲ فصل، ۱۷ صفحه | ~۱.۶MB |
| ۴ | `SahandClinicPro-User-Guide-v1.5.pdf` | 📘 راهنمای کار با نرم‌افزار — ۱۸ فصل (شامل فصل جدید نظرسنجی ⭐)، ۱۹ صفحه با ۱۰ اسکرین‌شات | ~۳.۸MB |
| ۵ | `SahandClinicPro-Brochure-v1.5.pdf` | 🎀 کاتالوگ بازاریابی ۱۲ اسلایدی — آمار به‌روز ۱۳۳ تست | ~۲.۵MB |
| ۶ | `SahandClinicPro-Poster-Vertical-50x70-150dpi.png` | 🖼 پوستر چاپی عمودی ۵۰×۷۰ سانتی‌متر (۱۵۰DPI) | ~۳MB |
| ۷ | `SahandClinicPro-Poster-Horizontal-70x50-150dpi.png` | 🖼 پوستر چاپی افقی ۷۰×۵۰ سانتی‌متر (۱۵۰DPI) | ~۳.۵MB |
| ۸ | `SahandClinicPro-Banner-Vertical-1.5x3m.png` | 🏙 استند عمودی ایستاده ۱.۵×۳ متر (۷۲DPI) | ~۵.۲MB |
| ۹ | `SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png` | 🛢 بیلبورد شهری افقی ۳×۱.۵ متر (۷۲DPI) | ~۶.۹MB |

> 💡 **مسیر پیشنهادی نصب:** پیوست ۳ (راهنمای نصب) → پیوست ۱ (سرور و کلاینت‌ها) → پیوست ۲ (سرور لایسنس روی هاست) → پیوست‌های ۵ تا ۹ برای تیم فروش آماده چاپ‌اند.

## 🔗 پیوندها

- [📖 CHANGELOG کامل](https://github.com/pranses2011/Sahand_ClinicPro/blob/main/CHANGELOG.md)
- [📋 فایل مدیریت چرخه](https://github.com/pranses2011/Sahand_ClinicPro/blob/main/TODO-v1.5.md)

</div>
"""


def api(url, method="GET", data=None, content_type="application/json"):
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {TOKEN}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Content-Type", content_type)
    req.add_header("User-Agent", "SahandClinicPro-Release-Creator")
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())


def main():
    # یافتن رلیز موجود v1.5.0 و به‌روزرسانی متن آن (ساخت قبلاً انجام شده)
    rel = api(f"https://api.github.com/repos/{REPO}/releases/tags/v1.5.0")
    payload = {
        "body": BODY,
    }
    result = api(
        f"https://api.github.com/repos/{REPO}/releases/{rel['id']}",
        method="PATCH",
        data=json.dumps(payload).encode(),
    )
    print(f"✅ متن رلیز v1.5.0 با جدول ۹ پیوست به‌روزرسانی شد: {result['html_url']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
