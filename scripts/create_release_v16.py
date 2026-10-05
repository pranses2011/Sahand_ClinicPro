#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🏁 ساخت رلیز گیت‌هاب v1.6.0 با یادداشت انتشار فارسی RTL + آپلود ۱۰ پیوست
"""
import json
import pathlib
import sys
import time
import urllib.parse
import urllib.request

REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
TAG = "v1.6.0"
OUT = pathlib.Path("/home/z/my-project/download")

BODY = """<div dir="rtl" align="right">

# 🏁 نسخه ۱.۶.۰ — چرخه هشتم توسعه

**پشتیبانی زنده یکپارچه + تکمیل لایسنس ۳۰ ماژولی + بازطراحی سه‌بعدی خروجی‌های چاپی + تور تصویری ۵۲ فرم** 🎉

| شاخص | مقدار |
|:---|:---|
| 🧑‍💻 پشتیبانی زنده | اطلاعات تماس از پنل لایسنس قابل ویرایش → ارسال خودکار به کلاینت هنگام فعال‌سازی → نمایش سراسر برنامه (صفر هاردکد) |
| 🧩 ماژول‌های لایسنس | ۲۴ ← **۳۰** (نظرسنجی، فیش حقوقی، پاراکلینیک، پرونده پیشرفته، طب مکمل، شعبه‌ها) |
| 🪪 ضدجعل کد ملی | اعتبارسنجی الگوریتمی در ثبت **و ویرایش** + یکتایی به‌استثنای خود بیمار |
| 🖼 خروجی‌های چاپی | پوستر و بنر بازطراحی سه‌بعدی — رفع کامل ایراد «لبه ناقص/سفید» |
| 📸 اسکرین‌شات فرم‌ها | ۱۴ ← **۵۲** (هر فرم برنامه، با محتوای واقعی‌نما) |
| 📘 کاتالوگ | ۳۳ صفحه A4 با تور تصویری کامل ۵۲ فرم در ۸ حوزه |
| 📖 راهنمای جدید | دانلود پروژه + ویژوال استودیو + خروجی exe (مبتدی‌پسند) |
| 🖥 فرم‌های دسکتاپ | **۵۲ فرم** WPF با Material Design RTL |
| 🧪 تست‌های سبز | ۱۳۳ ← **۱۳۷** |
| ✅ وضعیت بیلد | ۰ خطا / ۰ هشدار |

---

## 🧑‍💻 پشتیبانی زنده یکپارچه (گام W1)

اطلاعات تماس پشتیبانی (نام پشتیبان، شرکت، تلفن، موبایل، ایمیل، وب‌سایت) دیگر در هیچ‌جای سورس هاردکد نشده است:

| لایه | تغییر |
|:---|:---|
| **سرور لایسنس PHP** | ۶ کلید تنظیم `support_*` با بذر اولیه + فرم ویرایش در پنل مدیریت + افزودن آبجکت `support` به پاسخ activate/heartbeat |
| **زیرساخت دات‌نت** | `SupportInfoDto` + دریافت `support` از پاسخ سرور + ذخیره خودکار در جدول SystemSettings هنگام فعال‌سازی/تپ‌قلب + `GetSupportInfoAsync` |
| **API** | endpoint جدید `GET license/support` + افزودن `supportInfo` به `license/status` |
| **دسکتاپ** | کارت پشتیبانی در صفحه لایسنس (نام/شرکت/تلفن/موبایل/ایمیل/وب) — پیش از فعال‌سازی پیام جایگزین |
| **پرتال Blazor** | فرم ویرایش اطلاعات پشتیبانی در داشبورد |

## 🧩 کاتالوگ ۳۰ ماژولی لایسنس (گام W2)

۶ ماژول جدید همزمان به هر سه کاتالوگ (سرور PHP + پرتال Blazor + دسکتاپ) اضافه شد:
`Survey` نظرسنجی بیماران · `Payroll` فیش حقوقی · `Paraclinical` پاراکلینیک · `EMR` پرونده بالینی پیشرفته · `Complementary` طب مکمل و قالب چاپ · `Branches` مدیریت شعبه‌ها

> ⚖️ قاعده پذیرفته‌شده: از این پس هر ماژول جدید همزمان به کاتالوگ لایسنس اضافه می‌شود.

## 🪪 تکمیل اعتبارسنجی کد ملی (گام W3)

- `NationalCode` به `UpdatePatientDto` اضافه و در هندلر اعمال می‌شود
- قواعد کامل در `UpdatePatientCommandValidator`: الگوریتم رقم کنترلی + یکتایی با استثنای خود بیمار
- متد جدید `ExistsByNationalCodeAsync(code, excludePatientId)` در `IPatientRepository`
- ۴ تست یکپارچگی: کد جعلی رد / کد تکراری بیمار دیگر رد / حفظ کد خود بیمار مجاز / اعمال تغییر در هندلر

## 🖼 پوسترها و بنرهای سه‌بعدی (گام W4/W5)

- **ریشه‌یابی و رفع باگ «سمت چپ سفید / سمت راست ناقص»**: حذف عناصر سرریزدهنده + قاب `.bg` با overflow:hidden + overflow دوگانه html/body
- بازطراحی کامل با **موکاپ سه‌بعدی داشبورد** (perspective + rotateX/Y)، چیپ‌های شناور چندلایه، کره‌های نورانی و عنوان‌های سه‌بعدی سایه‌لایه
- نمایش قابلیت‌های نرم‌افزار + **اطلاعات تماس واقعی سهند سرویس** + لوگوی رسمی محصول
- اسکریپت راستی‌آزمایی پیکسلی `verify_print_png.py` — نمونه‌گیری ۲۰۰ نقطه از هر لبه + بررسی scrollWidth — **همه PASS**

## 📘 کاتالوگ با تور تصویری ۵۲ فرم (گام W6/W7)

- مولد داده‌محور `generate_form_screenshots.py` — ۳۳ نمای اصلی + ۱۹ پنجره فرم با اسکلت وفادار به پوسته WPF/Material
- بروشور جدید: جلد با لوگو + ۶ کارت مزیت + نقشه ۸ حوزه + **۲۹ صفحه تور تصویری** (هر فرم با اسکرین‌شات ۱۶۰۰×۱۰۰۰ و توضیح کاربرد) + جدول ۳۰ ماژول + مقایسه + پلن‌ها و تماس

## 📖 راهنماهای مبتدی‌پسند (گام W8/W9)

- **راهنمای نصب و شبکه — بازنویسی کامل**: زبان ساده، هر کلیک گام جدا با نام دقیق دکمه، کادرهای «چه انتظاری داشته باشید»، ۴ تصویر جدید (نصب SQL، کنسول سرور، فایروال، نقشه شبکه)، جدول رفع اشکال ۸ردیفی، چک‌لیست خرید و چک‌لیست نهایی
- **راهنمای جدید VS و خروجی exe**: دانلود از گیت‌هاب (ZIP و Git) + باز کردن Solution + Restore/Build + اجرا + سه روش Publish (تک‌فایل self-contained / خط فرمان / Inno Setup) + آموزش تغییر عناصر فرم + جدول ۸ خطای رایج + پرسش‌های متکرار

---

## 📦 پیوست‌های این رلیز (۱۰ فایل)

| # | فایل | شرح |
|:---:|:---|:---|
| ۱ | `SahandClinicPro-Setup-v1.6.0.zip` | بسته خودکفای تولید نصب‌کننده — سورس کامل + installer + راهنماها (۸۱۷ فایل) |
| ۲ | `SahandClinicPro-LicenseServer-cPanel-v1.2.zip` | سرور لایسنس PHP به‌روزشده — با تنظیمات پشتیبانی پویا + ۳۰ ماژول + یادداشت ارتقا |
| ۳ | `SahandClinicPro-User-Guide-v1.6.pdf` | راهنمای کاربر v3.1 — ۱۸ فصل |
| ۴ | `SahandClinicPro-Install-Network-Guide-v1.6.pdf` | راهنمای نصب و شبکه — مبتدی‌پسند با ۱۲ تصویر |
| ۵ | `SahandClinicPro-VS-Guide-v1.6.pdf` | 🆕 راهنمای دانلود پروژه + ویژوال استودیو + خروجی exe |
| ۶ | `SahandClinicPro-Brochure-v1.6.pdf` | کاتالوگ ۳۳ صفحه‌ای با تور تصویری ۵۲ فرم |
| ۷ | `SahandClinicPro-Poster-Vertical-50x70-150dpi.png` | پوستر عمودی ۵۰×۷۰ — سه‌بعدی ۲۹۵۳×۴۱۳۴ |
| ۸ | `SahandClinicPro-Poster-Horizontal-70x50-150dpi.png` | پوستر افقی ۷۰×۵۰ — سه‌بعدی ۴۱۳۴×۲۹۵۳ |
| ۹ | `SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png` | بنر بیلبورد ۳×۱.۵م — سه‌بعدی ۸۵۰۴×۴۲۵۲ |
| ۱۰ | `SahandClinicPro-Banner-Vertical-1.5x3m.png` | بنر استند ۱.۵×۳م — سه‌بعدی ۴۲۵۲×۸۵۰۴ |

### 🔁 مسیر پیشنهادی نصب

۱) `Setup` را دانلود و طبق `شروع-سریع-READ-ME.txt` نصب‌کننده بسازید → ۲) `LicenseServer` را روی هاست cPanel بالا بیاورید (یا ارتقا دهید) → ۳) از پنل لایسنس، کلید صادر و در برنامه فعال کنید — کارت پشتیبانی خودکار ظاهر می‌شود → ۴) برای شخصی‌سازی و خروجی exe، راهنمای VS را ببینید.

---

## 📊 آمار چرخه هشتم

| سنجه | مقدار |
|:---|:---|
| کامیت‌های این چرخه | ۲۰+ |
| تست‌ها | ۱۳۷ سبز (۲۸ + ۶۷ + ۲۶ + ۱۶) |
| هشدار بیلد | ۰ (رفع ۵ هشدار nullable遗留ی) |
| ماژول لایسنس | ۳۰ |
| اسکرین‌شات تولیدی | ۵۲ فرم + ۴ تصویر راهنمای نصب |

---

## 🧑‍💻 پشتیبانی — سهند سرویس

**سعید نجفی — سهند سرویس**
📞 `041-34436141` · 📱 `09144220430` · ✉️ `info@ea-fixer.ir` · 🌐 `https://ea-fixer.ir`

> 💡 همین اطلاعات درون خود نرم‌افزار (صفحه لایسنس → کارت پشتیبانی) از سرور لایسنس دریافت و نمایش داده می‌شود و از پنل مدیریت قابل ویرایش است.

</div>
"""

ASSETS = [
    "SahandClinicPro-Setup-v1.6.0.zip",
    "SahandClinicPro-LicenseServer-cPanel-v1.2.zip",
    "SahandClinicPro-User-Guide-v1.6.pdf",
    "SahandClinicPro-Install-Network-Guide-v1.6.pdf",
    "SahandClinicPro-VS-Guide-v1.6.pdf",
    "SahandClinicPro-Brochure-v1.6.pdf",
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
    req.add_header("User-Agent", "SahandClinicPro-Release-v16")
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
        "name": f"🏁 نسخه ۱.۶.۰ — پشتیبانی زنده + ۳۰ ماژول لایسنس + خروجی‌های سه‌بعدی + تور ۵۲ فرم",
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
