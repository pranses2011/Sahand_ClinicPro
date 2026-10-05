# 🔑 سرور لایسنس Sahand ClinicPro — نسخه cPanel / هاست اشتراکی

سرور لایسنس **PHP خالص + MySQL** برای سیستم مدیریت درمانگاه Sahand_ClinicPro — بدون هیچ پیش‌نیاز یا افزودنی خارجی (بدون Composer، بدون فریمورک، بدون Node). قابل نصب روی هر هاست اشتراکی cPanel معمولی.

## ✨ امکانات

- 🧙 **نصب‌کننده وب ۳ مرحله‌ای** — بررسی پیش‌نیاز، اتصال دیتابیس، ساخت خودکار جداول و جفت‌کلید RSA-2048
- 🖥️ **پنل مدیریت فارسی RTL** — داشبورد آماری، صدور/تمدید/ابطال لایسنس، فعال‌سازی آفلاین (فایل `.lic`)، لاگ اعتبارسنجی، تنظیمات
- 📡 **API عمومی سازگار با کلاینت .NET** — `activate` و `heartbeat` و `ping`
- 🔐 **امنیت** — امضای دیجیتال RSA-2048/SHA256/PKCS1 (یکسان با سمت دات‌نت)، PDO آماده‌سازی‌شده، CSRF، قفل ۵ خطا، محدودسازی نرخ، رمزهای hash شده
- 📴 **فعال‌سازی آفلاین** — مدیر فایل `.req` مشتری را می‌چسباند و فایل `.lic` امضاشده می‌گیرد
- 🗓️ **کار پاک‌سازی روزانه** — با Cron cPanel

## 📦 پیش‌نیازها (روی هر هاست cPanel استاندارد موجود است)

| نیاز | حداقل |
|------|-------|
| PHP | 7.4 (توصیه: 8.x) |
| افزونه‌ها | pdo_mysql، openssl، json، session (mbstring توصیه می‌شود) |
| دیتابیس | MySQL 5.7+ یا MariaDB 10.3+ |
| دسترسی | امکان نوشتن در پوشه `data/` (سطح 755) |

## 🚀 نصب روی cPanel — گام به گام

### ۱) ساخت دیتابیس
در cPanel وارد **MySQL® Databases** شوید:
1. یک دیتابیس بسازید (مثلاً `cpuser_licsrv`)
2. یک کاربر دیتابیس بسازید با رمز قوی (مثلاً `cpuser_licuser`)
3. کاربر را با **ALL PRIVILEGES** به دیتابیس اضافه کنید

### ۲) آپلود فایل‌ها
1. در **File Manager** پوشه‌ای بسازید (مثلاً `public_html/license`)
3. همه فایل‌های این بسته را در آن آپلود کنید (یا ZIP آپلود و Extract کنید)
4. مطمئن شوید پوشه `data` موجود است (در بسته هست) — اگر نبود با مجوز **755** بسازید

### ۳) اجرای نصب‌کننده
در مرورگر باز کنید: `https://your-domain.com/license/install.php`
- مرحله ۱: بررسی پیش‌نیازها (همه باید سبز باشد — در غیر این صورت از **Select PHP Version** افزونه را فعال کنید)
- مرحله ۲: اطلاعات دیتابیس (میزبان معمولاً `localhost`)
- مرحله ۳: حساب مدیر — نصب انجام می‌شود و **کلید عمومی RSA** در دو قالب PEM و XML نمایش داده می‌شود

> ⚠️ **الزاماً بعد از نصب فایل `install.php` را از هاست حذف کنید.**

### ۴) تنظیم کلاینت (.NET) — فایل `appsettings.json`
```json
"License": {
  "PortalUrl": "https://your-domain.com/license/api.php",
  "HardwareFingerprint": "SHA256-OF-CLIENT-HARDWARE",
  "RsaPublicKey": "<RSAKeyValue><Modulus>…</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>"
}
```
- `RsaPublicKey` را از صفحه پایان نصب یا پنل → تنظیمات کپی کنید (قالب XML یا PEM — هر دو پشتیبانی می‌شود)
- کلاینت با `POST` روی `api.php` فعال‌سازی و تپ قلب انجام می‌دهد

### ۵) (اختیاری) کار روزانه پاک‌سازی
در cPanel → **Cron Jobs** یک کار روزانه بسازید:
```
30 3 * * * php /home/CPUSER/public_html/license/cron.php YOUR_LS_CRON_SECRET
```
مقدار `LS_CRON_SECRET` در فایل `data/config.php` است.

## 📐 قرارداد API

### فعال‌سازی برخط — `POST api.php`
```json
{ "action": "activate", "licenseKey": "SCPRO-XXXXX-XXXXX-XXXXX",
  "fingerprint": "SHA256…", "clinicName": "درمانگاه سلامت", "appVersion": "1.0.0" }
```
پاسخ موفق:
```json
{
  "isValid": true, "status": "valid", "message": "لایسنس معتبر است.",
  "serverTime": "2026-10-05T10:00:00Z",
  "license": {
    "licenseKey": "SCPRO-…", "licenseType": "subscription",
    "issuedDate": "2026-10-05", "expiryDate": "2027-10-05",
    "maxUsers": 5, "maxDoctors": 10, "maxPatients": 5000, "maxBranches": 1,
    "enabledModules": ["Patients", "Appointments"],
    "daysRemaining": 365
  },
  "signature": "Base64(RSA-SHA256(\"SCPRO-…|2027-10-05|5\"))"
}
```
وضعیت‌های خطا: `not_found` | `expired` | `revoked` | `fingerprint_mismatch` | `rate_limited` | `bad_request`

### تپ قلب — `POST api.php`
همان فرمت با `"action": "heartbeat"` (اثر انگشت باید با دستگاه ثبت‌شده بخواند)

### بررسی سلامت — `GET api.php?action=ping`

## 📴 جریان فعال‌سازی آفلاین
1. کلاینت فایل درخواست `.req` می‌سازد (شامل کلید لایسنس + اثر انگشت سخت‌افزار)
2. مشتری فایل را برای فروشنده می‌فرستد
3. فروشنده در پنل → لایسنس → **فعال‌سازی آفلاین** محتوای `.req` را می‌چسباند
4. فایل `.lic` امضاشده دانلود و به مشتری داده می‌شود
5. کلاینت فایل `.lic` را بارگذاری و امضای RSA را تأیید می‌کند

## 🔒 نکات امنیتی هاست اشتراکی
- فایل `install.php` را بعد از نصب **حذف** کنید
- دسترسی وب به `data/` و `inc/` با `.htaccess` مسدود است — اگر وب‌سرور Nginx است قواعد معادل را از میزبان بخواهید
- HTTPS فعال کنید (cPanel → Let's Encrypt / AutoSSL) — بدون آن کوکی نشست امن ارسال نمی‌شود
- رمز مدیر را قوی انتخاب کنید و آن را دوره‌ای تغییر دهید (پنل → تنظیمات)
- در صورت لو رفتن کلید خصوصی، از پنل → تنظیمات کلیدها را بازتولید کنید و کلید عمومی جدید را در کلاینت‌ها به‌روز کنید

## 🗂️ ساختار فایل‌ها
```
├── install.php      ← نصب‌کننده ۳ مرحله‌ای (بعد از نصب حذف شود)
├── index.php        ← پنل مدیریت (ورود/داشبورد/لایسنس‌ها/لاگ/تنظیمات)
├── api.php          ← API عمومی برای کلاینت .NET
├── cron.php         ← پاک‌سازی روزانه (با کلید رمزی)
├── .htaccess        ← قواعد محافظت Apache
├── inc/
│   ├── functions.php ← هسته مشترک (دیتابیس، امضا، شمسی، امنیت)
│   ├── pages.inc.php ← صفحات پنل
│   └── schema.sql    ← ساختار جداول MySQL
├── assets/style.css ← سبک فارسی RTL (بدون وابستگی)
└── data/            ← تولیدی نصب: config.php + کلیدهای RSA + install.lock
```

## 🔗 اتصال به کلاینت دات‌نت
پروژه کلاینت (`Sahand.ClinicPro.Infrastructure` → `LicenseService`) با همین قرارداد کار می‌کند:
- `ActivateOnlineAsync(key)` → `POST api.php` با action=activate
- `ActivateOfflineAsync(licContent)` → تأیید امضای `Key|Expiry|MaxUsers`
- تأیید امضا با `RSA.FromXmlString` یا `RSA.ImportFromPem` (هر دو قالب)
