#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🏁 ساخت رلیز گیت‌هاب v1.7.0 با یادداشت انتشار فارسی RTL + آپلود ۱۰ پیوست
"""
import json
import pathlib
import sys
import time
import urllib.parse
import urllib.request

REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
TAG = "v1.7.0"
OUT = pathlib.Path("/home/z/my-project/download")

BODY = """<div dir="rtl" align="right">

# 🏁 نسخه ۱.۷.۰ — چرخه نهم توسعه

**حقوق درصدی پرسنل از سود خدمات + آیکون رسمی محصول + رفع باگ فونت اسکرین‌شات‌ها** 🎉

| شاخص | مقدار |
|:---|:---|
| 💼 حقوق درصدی | درصدی از **سود خدمات انجام‌شده توسط خودِ هر پرسنل** — نه کل فروش و نه کل دریافتی درمانگاه |
| 🧾 فیش شفاف | سه ستون جدید: درصد + مبنای سود دوره + مبلغ حقوق درصدی (با ردیف چاپی و یادداشت مبنا) |
| 🏷 انجام‌دهنده خدمت | ستون «انجام‌دهنده» در اقلام فاکتور — انتساب هر خدمت به پرسنل با عکس‌فوری هزینه |
| 🎨 آیکون رسمی | ClinicPro.png روی فایل exe، Taskbar و نوار عنوان هر ۵۲+ پنجره + نصب‌کننده Inno Setup |
| 📸 فونت اسکرین‌شات‌ها | ریشه‌یابی و رفع کامل باگ اعمال‌نشدن وزیرمتن + ظاهر یکدست «بار دوم» |
| 🖥 فرم‌های دسکتاپ | **۵۲ فرم** WPF با Material Design RTL |
| 🧪 تست‌های سبز | ۱۳۷ ← **۱۴۳** (۶ تست جدید حقوق درصدی) |
| ✅ وضعیت بیلد | ۰ خطا / ۰ هشدار |

---

## 💼 حقوق درصدی پرسنل از سود خدمات (نه کل دریافتی)

در تعریف پرسنل‌ها علاوه بر **حقوق پایه**، **درصد حقوق از سود خدمات** هم لحاظ می‌شود. قاعده مالک در هسته مدل داده رعایت شده است:

| مفهوم | پیاده‌سازی |
|:---|:---|
| **مبنا = سود نه فروش** | سود هر قلم فاکتور = مبلغ نهایی قلم − (هزینه تمام‌شده هر واحد × تعداد) |
| **فقط خدمات خود پرسنل** | هر قلم فاکتور با ستون `PerformedById` به پرسنل انجام‌دهنده منتسب می‌شود؛ فقط سود همین اقلام در فیش او لحاظ می‌شود |
| **عکس‌فوری هزینه** | هزینه هر خدمت در لحظه صدور فاکتور ثبت می‌شود — تغییر آتی هزینه، سوابق و فیش‌های گذشته را خراب نمی‌کند |
| **فیلتر فاکتور معتبر** | فقط فاکتورهای صادرشده/پرداخت‌جزئی/پرداخت‌شده (بدون پیش‌نویس، ابطال و برگشت هزینه) |
| **سود منفی → صفر** | ضرر دوره بر عهده کارفرماست؛ پرسنل متضرر نمی‌شود |
| **مشمول بیمه و مالیات** | پورسانت جزو حقوق و مزایای مشمول بیمه ۷٪ و مالیات پلکانی است (ماده ۲۸ تأمین اجتماعی) |
| **شفافیت فیش** | عکس‌فوری درصد و مبنای سود در لحظه محاسبه — قابل مقایسه تاریخی |

**زنجیره کامل در ۶ لایه**: دامنه (۴ موجودیت توسعه یافت) → مایگریشن `Cycle9CommissionPayroll` (۷ ستون + ایندکس + FK با پیش‌فرض صفر = بدون شکستن داده‌های موجود) → محاسبه‌گر → فرمان تولید حقوق دوره → API (شامل endpoint جدید `PUT hr/employees/{id}/salary`) → دسکتاپ (فرم پرسنل با کارت اطلاعات حقوقی + نمای حقوق با ستون‌ها و کارت جمع + فیش چاپی با ردیف و یادداشت مبنا + ستون انجام‌دهنده در فاکتور).

## 🎨 آیکون رسمی محصول

- لوگوی ClinicPro به آیکون شش‌مقیاسی ویندوزی (۲۵۶/۱۲۸/۶۴/۴۸/۳۲/۱۶) تبدیل شد
- روی فایل exe (`<ApplicationIcon>`)، Taskbar و نوار عنوان **هر ۵۲+ پنجره** (Setter سراسری — بدون دستکاری تک‌به‌تک)
- **رفع غیبت `installer/appicon.ico`** که `SetupIconFile` در نصب‌کننده Inno Setup ارجاع می‌داد و نبودش کامپایل را می‌شکست

## 📸 ریشه‌یابی و رفع باگ فونت اسکرین‌شات‌ها

مسیر `@font-face` در مولد اسکرین‌شات‌ها اشتباه بود و فونت وزیرمتن هرگز بارگذاری نمی‌شد — متن‌ها با فونت جایگزین زشت رندر می‌شدند:

- اصلاح مسیر + سازوکار «بار دوم» (goto + reload برای کش فونت — همان ظاهری که در برنامه واقعی پس از کش شدن یکدست می‌شود)
- راستی‌آزمایی خودکار `document.fonts.check` به‌عنوان گارد بازگشت باگ
- **لوگوی رسمی ClinicPro در نوار کناری** هر ۳۳ نمای اصلی به‌جای ایموجی
- بازتولید هر ۵۲ اسکرین‌شات (بدون هیچ هشدار فونت) + تأیید چشمی هوش بصری + بازسازی بروشور

---

## 📦 پیوست‌های این رلیز (۱۰ فایل)

| # | فایل | شرح |
|:---:|:---|:---|
| ۱ | `SahandClinicPro-Setup-v1.7.0.zip` | بسته خودکفای تولید نصب‌کننده — سورس کامل + installer (با appicon.ico 🆕) + راهنماها (۸۲۱ فایل) |
| ۲ | `SahandClinicPro-LicenseServer-cPanel-v1.2.zip` | سرور لایسنس PHP v1.2 (بدون تغییر در این چرخه) |
| ۳ | `SahandClinicPro-User-Guide-v1.7.pdf` | راهنمای کاربر v3.1 — ۱۸ فصل |
| ۴ | `SahandClinicPro-Install-Network-Guide-v1.7.pdf` | راهنمای نصب و شبکه — مبتدی‌پسند |
| ۵ | `SahandClinicPro-VS-Guide-v1.7.pdf` | راهنمای دانلود پروژه + ویژوال استودیو + خروجی exe |
| ۶ | `SahandClinicPro-Brochure-v1.7.pdf` | کاتالوگ ۳۳ صفحه‌ای با تور تصویری ۵۲ فرم — فونت‌درست 🆕 |
| ۷ | `SahandClinicPro-Poster-Vertical-50x70-150dpi.png` | پوستر عمودی ۵۰×۷۰ — سه‌بعدی ۲۹۵۳×۴۱۳۴ |
| ۸ | `SahandClinicPro-Poster-Horizontal-70x50-150dpi.png` | پوستر افقی ۷۰×۵۰ — سه‌بعدی ۴۱۳۴×۲۹۵۳ |
| ۹ | `SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png` | بنر بیلبورد ۳×۱/۵م — سه‌بعدی ۸۵۰۴×۴۲۵۲ |
| ۱۰ | `SahandClinicPro-Banner-Vertical-1.5x3m.png` | بنر استند ۱/۵×۳م — سه‌بعدی ۴۲۵۲×۸۵۰۴ |

### 🔁 مسیر پیشنهادی نصب

۱) `Setup` را دانلود و طبق `شروع-سریع-READ-ME.txt` نصب‌کننده بسازید → ۲) `LicenseServer` را روی هاست cPanel بالا بیاورید (یا ارتقا دهید) → ۳) از پنل لایسنس، کلید صادر و در برنامه فعال کنید → ۴) در تعریف هر پرسنل، حقوق پایه و درصد از سود خدمات را ثبت کنید → ۵) در فاکتورها، انجام‌دهنده هر خدمت را انتخاب و در پایان ماه حقوق دوره را تولید کنید.

---

## 📊 آمار چرخه نهم

| سنجه | مقدار |
|:---|:---|
| کامیت‌های این چرخه | ۲۵+ |
| تست‌ها | ۱۴۳ سبز (۲۸ + ۷۳ + ۲۶ + ۱۶) |
| هشدار بیلد | ۰ |
| ستون دیتابیس جدید | ۷ (مایگریشن Cycle9CommissionPayroll) |
| endpoint جدید | ۱ (`PUT hr/employees/{id}/salary`) |
| اسکرین‌شات بازتولیدشده | ۵۲ فرم |

---

## 🧑‍💻 پشتیبانی — سهند سرویس

**سعید نجفی — سهند سرویس**
📞 `041-34436141` · 📱 `09144220430` · ✉️ `info@ea-fixer.ir` · 🌐 `https://ea-fixer.ir`

> 💡 همین اطلاعات درون خود نرم‌افزار (صفحه لایسنس → کارت پشتیبانی) از سرور لایسنس دریافت و نمایش داده می‌شود و از پنل مدیریت قابل ویرایش است.

</div>
"""

ASSETS = [
    "SahandClinicPro-Setup-v1.7.0.zip",
    "SahandClinicPro-LicenseServer-cPanel-v1.2.zip",
    "SahandClinicPro-User-Guide-v1.7.pdf",
    "SahandClinicPro-Install-Network-Guide-v1.7.pdf",
    "SahandClinicPro-VS-Guide-v1.7.pdf",
    "SahandClinicPro-Brochure-v1.7.pdf",
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
    # https://TOKEN@github.com/...
    return url.split("//")[1].split("@")[0]


def api(url: str, method: str = "GET", data: bytes | None = None,
        content_type: str = "application/json", token: str = "") -> dict:
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {token}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Content-Type", content_type)
    req.add_header("User-Agent", "SahandClinicPro-Release-v17")
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())


def main():
    token = get_token()
    print(f"🏁 ساخت رلیز {TAG}")

    # حذف رلیز و تگ قبلی در صورت وجود (idempotency)
    try:
        rel = api(f"{REPO_API}/releases/tags/{TAG}", token=token)
        api(f"{REPO_API}/releases/{rel['id']}", method="DELETE", token=token)
        print("  ↻ رلیز قبلی حذف شد")
    except Exception:
        print("  ✔ رلیزی قبلی وجود نداشت")
    try:
        api(f"{REPO_API}/git/refs/tags/{TAG}", method="DELETE", token=token)
        print("  ↻ تگ قبلی حذف شد")
    except Exception:
        pass

    # ساخت رلیز جدید روی HEAD
    main_sha = api(f"{REPO_API}/git/ref/heads/main", token=token)["object"]["sha"]
    payload = json.dumps({
        "tag_name": TAG,
        "target_commitish": main_sha,
        "name": f"🏁 نسخه ۱.۷.۰ — حقوق درصدی پرسنل از سود خدمات + آیکون رسمی + رفع باگ فونت اسکرین‌شات‌ها",
        "body": BODY,
        "draft": False,
        "prerelease": False,
    }).encode()
    rel = api(f"{REPO_API}/releases", method="POST", data=payload, token=token)
    release_id = rel["id"]
    print(f"  ✔ رلیز ساخته شد — شناسه {release_id}")

    # آپلود asset ها
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
            url = f"https://uploads.github.com/repos/pranses2011/Sahand_ClinicPro/releases/{release_id}/assets?name={urllib.parse.quote(name)}"
            api(url, method="POST", data=path.read_bytes(),
                content_type="application/octet-stream", token=token)
            ok.append(name)
        except Exception as exc:  # noqa: BLE001
            print(f"  ✖ خطا: {exc}")
            failed.append(name)
        time.sleep(1.0)

    print("════════════════════════════════")
    print(f"✅ رلیز {TAG} با {len(ok)}/{len(ASSETS)} پیوست منتشر شد")
    print(f"🔗 https://github.com/pranses2011/Sahand_ClinicPro/releases/tag/{TAG}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
