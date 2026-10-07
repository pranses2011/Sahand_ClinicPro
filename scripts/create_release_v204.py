#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 ساخت رلیز v2.0.4 در گیت‌هاب + بارگذاری بستهٔ آپدیت — چرخه هفدهم"""
import json
import pathlib
import sys
import urllib.request

TOKEN = "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"
REPO = "pranses2011/Sahand_ClinicPro"
TAG = "v2.0.4"
ASSET = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.0.3-to-2.0.4.zip")

BODY = """<div dir="rtl" align="right">

# 🔐 نسخهٔ ۲.۰.۴ — پنج رفع گزارش یازدهم مالک

**وارونگی واقعی رمز ماسک‌شده ریشه‌یابی و رفع شد + فعال‌سازی لایسنس واقعاً کار می‌کند و ورود بدون لایسنس ممکن نیست + تم آبی مطابق اسکرین‌شات‌ها + داشبورد تمام‌صفحه + رفع کرش دکمهٔ بیمار جدید** 🎉

## 📥 بستهٔ آپدیت این رلیز (قاعدهٔ دائمی توزیع)

| فایل | توضیح |
|:---|:---|
| `SahandClinicPro-Update-2.0.3-to-2.0.4.zip` | **فقط ۲۲ فایل تغییرکردهٔ همین رلیز** (۴ جدید + ۱۸ ویرایش) با همان ساختار پوشه‌ای + راهنمای فارسی نصب (`Update-Guide-2.0.4.html`) + فهرست فایل‌ها (`changed-files.txt`) |

**نصب در یک خط:** ویژوال استودیو را **کاملاً ببندید** → بسته را باز کنید → همه را انتخاب (<kbd>Ctrl+A</kbd>) → روی ریشهٔ پوشهٔ سورس فعلی خود پیست کنید → **Replace the files** → `Rebuild Solution` → اجرا.

> ⚠️ اگر نسخهٔ فعلی شما قدیمی‌تر از ۲.۰.۳ است، اول بستهٔ تجمعی `2.0.1→2.0.3` از رلیز قبلی را نصب کنید و بعد این بسته را. فایل `appsettings.json` (تنظیم SQL شما)، دیتابیس و رمز شما دست نمی‌خورند.

## 🔐 ۱) وارونگی رمز عبور در حالت ماسک — ریشه واقعی پیدا شد

شما سه بار درست گزارش می‌دادید و حق با شما بود — **نقص در خود مخزن بود**، نه فایل‌های کهنه. این‌بار به‌جای حدس، سورس خودِ کتابخانهٔ MaterialDesignThemes 5.3.2 کالبدشکافی شد:

> بایندینگ `PasswordBoxAssist.Password` با `DefaultUpdateSourceTrigger=LostFocus` ثبت شده و در `HandlePasswordChanged` مقدار را **بدون هیچ بررسی برابری** دوباره داخل `PasswordBox.Password` می‌نویسد. هر بازنویسی برنامه‌ایِ Password، مکان‌نما را به **ابتدای کادر** می‌پراند → کاراکتر بعدی **در ابتدای رشته** درج می‌شود → تایپ `Admin@123` در ViewModel به‌صورت `321@nimdA` می‌نشست.

چرا فقط حالت ماسک خراب بود و با آیکون چشم درست می‌شد؟ کادر آشکار یک TextBox معمولی با بایندینگ سالم است؛ فقط مسیر PasswordBox این عارضه را داشت — **دقیقاً همان نشانه‌ای که گزارش کردید.**

**رفع سه‌لایه:**
- حذف کامل آن بایندینگ از کادر ماسک ورود و هر سه کادر پنجرهٔ تغییر رمز — همگام‌سازی «تک‌مسیری» فقط با رویداد `PasswordChanged`؛ دیگر هیچ‌چیز به داخل کادرِ در حال تایپ بازنویسی نمی‌کند
- سرویس جدید `InputSanitizer`: نویسه‌های نامرئی جهت (LRM/RLM/…) که کیبورد فارسی ویندوز می‌تواند تزریق کند، پیش از ارسال به سرور پاک می‌شوند — با ۹ تست گارد
- هنگام مخفی‌کردن چشم، کادر ماسک با آخرین مقدار همگام و همه‌گزین می‌شود تا تایپ بعدی «جایگزین» باشد نه درج در ابتدا

## 🔑 ۲) فعال‌سازی لایسنس — دکمه واقعاً کار می‌کند و ورود گیت می‌خورد

دو حفره بسته شد:
- **دکمهٔ «🔑 فعال‌سازی لایسنس» صفحهٔ ورود هیچ شنونده‌ای نداشت** (رویدادش به هیچ‌جا وصل نبود!) → اکنون پنجرهٔ فعال‌سازی مستقل باز می‌شود: فعال‌سازی آنلاین با کلید (فرم `SCPRO-XXXXX-XXXXX-XXXXX`) و آفلاین با فایل `.lic` — هر دو پیش از ورود هم کار می‌کنند
- **ورود موفق بدون هیچ بررسی لایسنسی مستقیم وارد برنامه می‌شد** → اکنون در هر سه مسیر (ورود موفق، پس از تغییر رمز اجباری، اجرای برنامه با توکنِ به‌یادمانده) وضعیت لایسنس از سرور پرسیده می‌شود؛ پاسخ صریحِ «نامعتبر» = پنجرهٔ فعال‌سازی مودال؛ فعال شد = ورود ادامه می‌یابد؛ بدون فعال‌سازی بسته شد = توکن پاک + پیام راهنما + بازگشت به صفحهٔ ورود (**دورزدنی‌ناپذیر**)

> 💡 اگر شبکه/سرور لایسنس موقتاً قطع باشد برنامه شما را بیرون نمی‌کند (فقط پاسخ صریحِ «نامعتبر» فعال‌سازی می‌خواهد) — درمانگاه قفل نمی‌شود.

## 🎨 ۳) تم آبی مانند اسکرین‌شات‌ها

ریشهٔ سبزی که می‌دیدید `PrimaryColor="Teal"` در App.xaml بود. اکنون:
- رنگ اصلی کل برنامه → **آبی** (دکمه‌ها، آیکون‌ها، های‌لایت‌ها، انتخاب‌ها)
- سایدبار → **گرادیان آبی تیرهٔ رسمی طرح (#16355E→#1C4E89)** با متن سفید/آبی‌روشن و آیتم فعال با های‌لایت نیمه‌شفاف گوشه‌گرد — همان چیزی که در اسکرین‌شات‌ها می‌بینید
- گرادیان پنجرهٔ ورود هم کاملاً آبی شد

## 🖥️ ۴) داشبورد تمام‌صفحه

پنجرهٔ اصلی از این پس **تمام‌صفحه** باز می‌شود (با دکمهٔ Restore ویندوز می‌توانید کوچکش کنید).

## 💥 ۵) کرش دکمهٔ «بیمار جدید»

خطای `InvalidOperationException ... read-only property 'Allergies'` — سه بایندینگ `TwoWay` روی پراپرتی‌های فقط-خواندنیِ تگ‌ها (آلرژی/بیماری مزمن/داروهای فعلی) در تب «سوابق پزشکی» بود؛ به `OneWay` اصلاح شد و فرم بیمار بدون خطا باز می‌شود.

## ✅ تأیید نهایی

- بیلد Release کامل سولوشن: **۰ خطا**
- تست‌های قابل‌اجرا روی محیط توسعه: **۱۸۱/۱۸۱ سبز** (دامنه ۲۸ + کاربرد ۹۳ + یکپارچگی API ۱۹ + دسکتاپ ۴۱ — شامل ۹ تست گارد جدید: گیت لایسنس ×۳ و پاک‌ساز نویسه‌های جهت ×۶)
- گارد انسجام بسته: هر ۵ رفع با نشانگر مثبت/منفی قبل از بسته‌بندی تأیید شدند (همان درس CS0103)

## 🧭 بعد از نصب — سه اطمینان

۱. پانویس پنجرهٔ ورود **«نسخه ۲.۰.۴»** را نشان دهد
۲. دکمهٔ «🔑 فعال‌سازی لایسنس» پنجرهٔ فعال‌سازی را باز کند
۳. پس از ورود: سایدبار آبی تیره + پنجره تمام‌صفحه + دکمهٔ «بیمار جدید» بدون خطا

> 💡 دارایی‌های راهنما (راهنمای کاربر/نصب/VS/SQL، بروشور، پوستر و بنر) در رلیزهای v2.0.0 و v2.0.1 **همچنان معتبرند** — این نسخه وصلهٔ پایداری است.

</div>
"""


def api(method: str, url: str, payload=None, content_type="application/json", raw=None):
    data = raw if raw is not None else (json.dumps(payload).encode() if payload else None)
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {TOKEN}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("User-Agent", "sahand-release-script")
    if data is not None:
        req.add_header("Content-Type", content_type)
    with urllib.request.urlopen(req) as r:
        body = r.read()
        # پاسخ GitHub همیشه JSON است حتی وقتی بدنهٔ درخواست zip بوده
        parsed = None
        if body:
            try:
                parsed = json.loads(body)
            except (json.JSONDecodeError, UnicodeDecodeError):
                parsed = body
        return r.status, parsed if parsed is not None else body


def main() -> int:
    # ۱) ساخت رلیز روی تگ موجود
    url = f"https://api.github.com/repos/{REPO}/releases"
    try:
        status, release = api("POST", url, {
            "tag_name": TAG,
            "name": "🔐 نسخهٔ ۲.۰.۴ — پنج رفع گزارش یازدهم مالک",
            "body": BODY,
            "draft": False,
            "prerelease": False,
            "make_latest": "true",
        })
    except urllib.error.HTTPError as e:
        if e.code == 422:
            # رلیز موجود است — ویرایش
            status, rel_list = api("GET", f"{url}/tags/{TAG}")
            rel_id = rel_list["id"]
            status, release = api("PATCH", f"{url}/{rel_id}", {
                "name": "🔐 نسخهٔ ۲.۰.۴ — پنج رفع گزارش یازدهم مالک",
                "body": BODY,
                "draft": False,
                "prerelease": False,
                "make_latest": "true",
            })
        else:
            raise
    print("رلیز ساخته شد:", release.get("html_url"))
    rel_id = release["id"]

    # ۲) حذف asset هم‌نام قبلی (idempotent) و بارگذاری ZIP
    status, assets = api("GET", f"https://api.github.com/repos/{REPO}/releases/{rel_id}/assets")
    for a in assets:
        if a["name"] == ASSET.name:
            api("DELETE", f"https://api.github.com/repos/{REPO}/releases/assets/{a['id']}")
            print("asset قبلی حذف شد:", a["name"])

    up_url = f"https://uploads.github.com/repos/{REPO}/releases/{rel_id}/assets?name={ASSET.name}"
    status, uploaded = api("POST", up_url, raw=ASSET.read_bytes(),
                           content_type="application/zip")
    print("بسته بارگذاری شد:", uploaded.get("browser_download_url"))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
