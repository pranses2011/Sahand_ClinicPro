#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🔧 وصلهٔ پس از انتشار v2.0.2 — تعمیر خطای CS0103 پنجره تغییر رمز (گزارش نهم مالک)
================================================================================
علت خطا روی دستگاه مالک: جفتِ ChangePasswordWindow.xaml/.xaml.cs ناهماهنگ شده
(.xaml.cs جدید + .xaml قدیمی). مخزن و بستهٔ آپدیت اصلی سالم‌اند (بیلد تمیز ۰ خطا).
این اسکریپت:
۱) بستهٔ کوچک SahandClinicPro-Hotfix-ChangePasswordWindow-2.0.2.zip می‌سازند —
   فقط ۲ فایل هماهنگ + یادداشت تعمیر فارسی HTML
۲) دارایی را به رلیز موجود v2.0.2 اضافه می‌کند (idempotent)
۳) متن رلیز را با بخش وصله به‌روز می‌کند (idempotent با نشانه)
⚠️ توکن از متغیر محیطی GH_TOKEN خوانده می‌شود — هرگز در سورس هاردکد نمی‌شود
"""
import json
import os
import pathlib
import sys
import urllib.parse
import urllib.request
import zipfile

REPO = pathlib.Path(__file__).resolve().parents[1]
REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
TAG = "v2.0.2"

HOTFIX_NAME = "SahandClinicPro-Hotfix-ChangePasswordWindow-2.0.2.zip"
PAIR = [
    "src/Sahand.ClinicPro.Desktop/Views/ChangePasswordWindow.xaml",
    "src/Sahand.ClinicPro.Desktop/Views/ChangePasswordWindow.xaml.cs",
]
NOTE_NAME = "Fix-Note-2.0.2.html"
OUT_DIR = pathlib.Path("/home/z/my-project/download")

TOKEN = os.environ.get("GH_TOKEN") or ""
if not TOKEN:
    raise SystemExit("متغیر محیطی GH_TOKEN تنظیم نشده است")


def api(url: str, method: str = "GET", data: bytes | None = None,
        content_type: str = "application/json", raw: bool = False):
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {TOKEN}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Content-Type", content_type)
    req.add_header("User-Agent", "SahandClinicPro-Hotfix-v202")
    with urllib.request.urlopen(req) as resp:
        payload = resp.read()
        if not payload:  # DELETE با 204 و بدنه خالی
            return None
        return payload if raw else json.loads(payload.decode())


# ─────────────────────────── یادداشت تعمیر (HTML فارسی RTL) ───────────────────────────

NOTE_CSS = """
body{margin:0;background:#f4f7f6;color:#1c2b33;font-family:"Vazirmatn","Segoe UI",Tahoma,sans-serif;
line-height:2;font-size:15px;direction:rtl}
.wrap{max-width:760px;margin:0 auto;padding:22px 18px 50px}
header{background:linear-gradient(135deg,#8a3418,#b3372f);color:#fff;border-radius:14px;padding:20px 22px}
header h1{margin:0 0 4px;font-size:20px}
header p{margin:0;opacity:.92;font-size:13px}
h2{font-size:17px;color:#7a2413;border-right:5px solid #b3372f;padding-right:10px;margin:28px 0 10px}
.card{background:#fff;border:1px solid #e2e8e6;border-radius:10px;padding:14px 18px;margin:10px 0}
code{font-family:Consolas,monospace;font-size:13px;background:#eef1f0;border-radius:5px;
padding:1px 6px;direction:ltr;display:inline-block}
pre{background:#2d1b17;color:#ffd9d0;border-radius:10px;padding:12px 14px;overflow-x:auto;
direction:ltr;text-align:left;font-family:Consolas,monospace;font-size:12.5px}
.ok{border-right:5px solid #0c7a4b;background:#eff8f3}
.warn{border-right:5px solid #9a6700;background:#fdf8ee}
ol{padding-right:22px;margin:8px 0}li{margin:6px 0}
.kbd{background:#e8ecec;border:1px solid #cbd5d9;border-bottom-width:3px;border-radius:6px;
padding:1px 8px;font-size:13px}
footer{margin-top:34px;color:#5a6b73;font-size:12px;text-align:center}
"""

NOTE_HTML = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="utf-8">
<title>یادداشت تعمیر — پنجره تغییر رمز ۲.۰.۲</title>
<style>""" + NOTE_CSS + """</style>
</head>
<body>
<div class="wrap">

<header>
<h1>🔧 وصلهٔ تعمیر — خطای CS0103 پنجره تغییر رمز</h1>
<p>سامانهٔ درمانگاه سهند — نسخهٔ ۲.۰.۲ | فقط ۲ فایل + این یادداشت</p>
</header>

<h2>این خطا چه بود؟</h2>
<div class="card">
<p>هنگام بیلد/اجرا این خطا داده شد:</p>
<pre>CS0103: The name 'CurrentPasswordRevealBox' does not exist in the current context
File: ChangePasswordWindow.xaml.cs — Line 71</pre>
<p><b>علت:</b> روی دستگاه شما دو فایل پنجرهٔ تغییر رمز با هم ناهماهنگ شده‌اند —
فایل <code>.xaml.cs</code> نسخهٔ جدید است (خط ۷۱ آن به کادرِ آشکارساز رمز فعلی ارجاع می‌دهد)
ولی فایل <code>.xaml</code> هنوز نسخهٔ قدیمی است (آن کادر را ندارد).
رایج‌ترین دلیل: هنگام جایگزینی فایل‌های بستهٔ آپدیت، <b>ویژوال استودیو باز بوده</b>،
فایل قدیمی XAML در حافظهٔ آن مانده و بعداً دوباره روی فایل جدید ذخیره شده —
یا آن فایل در مرحلهٔ «Replace» جا افتاده است.</p>
</div>

<div class="card ok">
<p>✅ خود بستهٔ آپدیت اصلی رلیز <b>سالم</b> است و این دو فایل را به‌صورت هماهنگ دارد؛
بررسی ما با بیلد کاملاً تمیز مخزن: ۰ خطا. این وصله فقط برای راحتی تعمیر محلی شماست.</p>
</div>

<h2>راه حل — ۴ قدم</h2>
<div class="card">
<ol>
<li><b>ویژوال استودیو را کاملاً ببندید</b> (File ← Exit — مهم‌ترین قدم!)</li>
<li>این بسته را باز کنید؛ هر دو فایل را با همان ساختار پوشه‌ای روی پوشهٔ سورس
(<code>E:\\Sahand_ClinicPro-2.0.1</code>) کپی و <b>Replace</b> کنید:
<div style="font-family:Consolas,monospace;font-size:12.5px;direction:ltr;text-align:left;color:#33474f">
src\\Sahand.ClinicPro.Desktop\\Views\\ChangePasswordWindow.xaml<br>
src\\Sahand.ClinicPro.Desktop\\Views\\ChangePasswordWindow.xaml.cs</div></li>
<li>سولوشن را باز کنید و <span class="kbd">Build</span> ← <span class="kbd">Rebuild Solution</span></li>
<li>اجرا — دیگر خطای CS0103 نخواهید داشت ✔</li>
</ol>
</div>

<h2>راستی‌آزمایی (اختیاری)</h2>
<div class="card">
پس از جایگزینی، فایل <code>ChangePasswordWindow.xaml</code> را در ویژوال استودیو باز کنید؛
حدود خط ۹۳ باید این را ببینید: <code>x:Name="CurrentPasswordRevealBox"</code> —
اگر بود، جفت فایل‌ها هماهنگ‌اند.</p>
</div>

<h2>پیشگیری برای آپدیت‌های بعدی</h2>
<div class="card warn">
<ol>
<li>هنگام جایگزینی فایل‌های هر بستهٔ آپدیت، <b>هیچ برنامه‌ای باز نباشد</b> (مخصوصاً ویژوال استودیو)</li>
<li>اگر بعد از کپی، ویژوال استودیو پرسید «the file has been changed outside… reload?»
روی <b>Yes to All</b> کلیک کنید — هرگز نسخهٔ قدیمیِ در حافظه را ذخیره نکنید</li>
<li>اگر باز هم خطای دیگری از جنس CS0103 در فایل دیگری دیدید، کل بستهٔ
<code>SahandClinicPro-Update-2.0.1-to-2.0.2.zip</code> را دوباره (با بستن ویژوال استودیو) کامل جایگزین کنید</li>
</ol>
</div>

<footer>سامانهٔ جامع مدیریت درمانگاه سهند — نسخهٔ ۲.۰.۲</footer>

</div>
</body>
</html>
"""

# ─────────────────────────── متن بخش وصله در بدنهٔ رلیز ───────────────────────────

BODY_SECTION = """
## 🔧 وصلهٔ پس از انتشار — تعمیر خطای CS0103 پنجرهٔ تغییر رمز (گزارش نهم مالک)

اگر پس از نصب بستهٔ آپدیت با این خطا مواجه شدید:

```
CS0103: The name 'CurrentPasswordRevealBox' does not exist in the current context
— ChangePasswordWindow.xaml.cs خط ۷۱
```

علت: روی دستگاه شما جفتِ فایل‌های پنجرهٔ تغییر رمز ناهماهنگ شده است (`.xaml.cs` جدید است ولی `.xaml` قدیمی مانده — معمولاً چون ویژوال استودیو هنگام جایگزینی فایل‌ها باز بوده و نسخهٔ قدیمی را دوباره روی فایل جدید ذخیره کرده است). بستهٔ اصلی این رلیز سالم است (بیلد کاملاً تمیز مخزن: ۰ خطا) و هر دو فایل را هماهنگ دارد؛ این وصله فقط برای تعمیر سریع محلی است:

| فایل | توضیح |
|:---|:---|
| `SahandClinicPro-Hotfix-ChangePasswordWindow-2.0.2.zip` | فقط ۲ فایل هماهنگ پنجرهٔ تغییر رمز + یادداشت تعمیر فارسی — ویژوال استودیو را ببندید، جایگزین کنید، `Rebuild Solution` |

"""


def build_zip() -> pathlib.Path:
    out = OUT_DIR / HOTFIX_NAME
    out.parent.mkdir(parents=True, exist_ok=True)
    if out.exists():
        out.unlink()
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        zf.writestr(NOTE_NAME, NOTE_HTML)
        for path in PAIR:
            src = REPO / path
            if not src.is_file():
                raise SystemExit("⛔ فایل در مخزن نیست: " + path)
            zf.write(src, path)
    # راستی‌آزمایی محتوا
    with zipfile.ZipFile(out) as zf:
        names = zf.namelist()
        for path in PAIR:
            if path not in names:
                raise SystemExit("⛔ فایل جا افتاد در ZIP: " + path)
        xaml = zf.read(PAIR[0]).decode("utf-8")
        if 'x:Name="CurrentPasswordRevealBox"' not in xaml:
            raise SystemExit("⛔ XAML داخل وصله، کادر آشکارساز را ندارد!")
    print("✔ بستهٔ وصله ساخته شد: " + str(out) + " (" + str(round(out.stat().st_size / 1024)) + " KB)")
    return out


def upload_asset(release: dict, zip_path: pathlib.Path) -> None:
    for asset in release.get("assets", []):
        if asset["name"] == HOTFIX_NAME:
            api(asset["url"], method="DELETE")
            print("  ↻ دارایی وصلهٔ قبلی حذف شد")
            break
    upload_base = release["upload_url"].split("{")[0]
    url = f"{upload_base}?name={urllib.parse.quote(HOTFIX_NAME)}"
    data = zip_path.read_bytes()
    api(url, method="POST", data=data, content_type="application/zip", raw=True)
    print(f"  ✔ {HOTFIX_NAME} آپلود شد ({len(data)/1024:.0f} KB)")


def patch_body(body: str) -> str:
    if HOTFIX_NAME in body:
        print("  ✔ متن رلیز از قبل بخش وصله را دارد")
        return body
    idx = body.rfind("</div>")
    if idx == -1:
        raise SystemExit("✖ نشانهٔ پایان div در متن رلیز یافت نشد")
    return body[:idx] + BODY_SECTION + "\n" + body[idx:]


def main() -> int:
    zip_path = build_zip()
    print("🏁 افزودن وصله به رلیز " + TAG)
    rel = api(f"{REPO_API}/releases/tags/{TAG}")
    print("  ✔ رلیز موجود — شناسه " + str(rel["id"]))

    upload_asset(rel, zip_path)

    new_body = patch_body(rel["body"])
    if new_body != rel["body"]:
        payload = json.dumps({"body": new_body}).encode()
        api(f"{REPO_API}/releases/{rel['id']}", method="PATCH", data=payload)
        print("  ✔ متن رلیز با بخش وصله به‌روز شد")
    print("🎉 وصله در رلیز v2.0.2 زنده است")
    return 0


if __name__ == "__main__":
    sys.exit(main())
