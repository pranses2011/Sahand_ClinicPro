#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🚀 انتشار رلیز v2.13.0 — چرخهٔ ۴۹-۵۰ (فاز ۰ تثبیت اضطراری)."""
import json
import pathlib
import subprocess
import urllib.request

TOKEN = subprocess.run(
    ["git", "config", "--get", "remote.origin.url"],
    cwd="/home/z/my-project/Sahand_ClinicPro", capture_output=True, text=True
).stdout
# توکن از URL ریموت استخراج می‌شود (https://TOKEN@github.com/...)
TOKEN = TOKEN.split("//")[1].split("@")[0].strip() if "//" in TOKEN and "@" in TOKEN else ""
REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
TAG = "v2.13.0"
ZIP = pathlib.Path("/home/z/my-project/download/SahandClinicPro-Update-2.12.0-to-2.13.0.zip")

# ── ۱) تگ روی HEAD فعلی main ──
subprocess.run(f'git tag -f {TAG} main', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro", check=True)
subprocess.run(f'git push origin -f {TAG}', shell=True, cwd="/home/z/my-project/Sahand_ClinicPro",
               check=True, capture_output=True, text=True)
print("✅ تگ", TAG, "پوش شد")

BODY = """## 🔐 v2.13.0 — چرخهٔ ۴۹-۵۰: فاز ۰ تثبیت اضطراری (نخستین گام نقشه راه بازبینی مستقل)

> سه روزنهٔ امنیت/پایداری بسته شد: **کلید امضای JWT از مخزن حذف شد** (تا این نسخه هر کسی با دسترسی به مخزن می‌توانست توکن جعلی با نقش ادمین بسازد!)، دو **انسداد رشته‌ای** (ریسک بن‌بست سرور پشتیبان زیر بار همزمان + هنگ صفحهٔ گزارش‌ها در خطای شبکه) ریشه‌یابی و رفع شدند، و **اسکنر ۶ گیته الگوهای ممنوعه** برای همیشه جلوی بازگشت همهٔ این الگوها را می‌گیرد. دروازهٔ ۰ نقشه راه (docs/analysis/ai-review) با گواهی اسکنر سبز عبور شد.

### 🔑 حذف کلید JWT از مخزن (BUG-01)
- کلید هاردکد از `appsettings.json` حذف شد — در توسعه کلید خالی مجاز است (جایگزین داخلی)، در تولید **الزامی** از متغیر محیطی
- **گارد سه‌لایه تولید:** غایب ⟵ پیام فارسی · کوتاه‌تر از ۳۲ بایت ⟵ رد · **الگوی ناامن** (Development-Secret و…) ⟵ رد
- 🔍 کشف بازبینی: کلید پیشین مخزن از گارد قدیمی **رد می‌شد** (طولانی‌تر از حداقل و ناهم‌جنس با ثابت پیش‌فرض) و در تولید پذیرفته می‌شد — تست رگرسیون تا ابد مانع بازگشتش است
- تشخیص محیط از هر سه کلید `Environment` / `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT`
- **مرور کامل ۱۲ اندپوینت عمومی** با جدول دلیل/ریسک/کنترل + سقف نرخ ۲۰/دقیقه روی اندپوینت‌های لایسنس (تنها ریسک قرمز مرور مهار شد)
- راهنمای استقرار کامل فارسی: `docs/guides/deploy-jwt.md` (تولید کلید · User-Secrets · ۵ سناریوی سرور · چرخش · عیب‌یابی)

### 🔄 رفع انسداد رشته‌ای پشتیبان ابری (BUG-02)
- نخِ درخواست هنگام ساخت نخستین سرویس ابری به‌صورت هم‌گام منتظر دیتابیس می‌ماند — زیر بار همزمان ریسک بن‌بست واقعی
- الگوی استاندارد **async lazy با SemaphoreSlim** + دوباره‌سنجی پس از قفل؛ هر ۴ اندپوینت ابری اصلاح شد
- تست رگرسیون: ۵۰ درخواست هم‌زمان — همه باید زیر ۲۰ ثانیه پاسخ بگیرند

### 📊 رفع خواندن هم‌گام نتیجهٔ Taskها در گزارش‌ها (BUG-03)
- در خطای شبکه، استثنای درون‌پیچیده به رشتهٔ UI می‌رسید و برنامه هنگ می‌کرد؛ اکنون با `await` خوانده می‌شود و پیام فارسی تمیز نمایش می‌دهد
- تست واحد دسکتاپ با FakeApiClient (موفقیت + شکست پاسخ)

### 🛡 پیشگیری برای همیشه
- `scripts/security/scan_secrets.sh` — ۶ گیت: کلید در پیکربندی · کلید افشاشدهٔ تاریخی · رمز اتصال · انسداد هم‌گام API · خواندن هم‌گام دسکتاپ · async void — سازگار با Git Bash ویندوز

### ⚠️ گام الزامی نصب سرور (پیش از جایگزینی فایل‌ها)
```
# PowerShell — ساخت کلید تصادفی:
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
# تنظیم (پس از آن سرور را کامل ببندید و دوباره باز کنید):
setx Jwt__SecretKey "کلید-تولیدی-بالا"
```
بدون این گام، سرور در محیط تولید عمداً بالا نمی‌آید (پیام فارسی راهنما). توکن‌های فعال باطل‌اند — کاربران با یک ورود مجدد ادامه می‌دهند. راهنمای گام‌به‌گام: `Update-Guide-2.13.0.html` داخل بسته.

### 📦 محتوا و نصب
**۱۳ فایل** — دلتا از v2.12.0. هر دو پروژه (API + Desktop) تغییر دارند؛ تغییر دیتابیس **نداریم**. پس از جایگزینی: Clean + Rebuild + ری‌استارت سرور API. سنجهٔ نصب: لاگ «راه‌اندازی شد» بدون پیام ⛔ + صفحهٔ گزارش‌ها سالم."""

# ── ۲) ساخت رلیز ──
payload = {
    "tag_name": TAG,
    "name": "v2.13.0 — چرخهٔ ۴۹-۵۰: فاز ۰ تثبیت اضطراری — حذف کلید JWT + رفع دو انسداد رشته‌ای + اسکنر الگوهای ممنوعه",
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
print("\n🎉 رلیز v2.13.0 کامل شد:", release["html_url"])
