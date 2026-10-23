# بستهٔ نصب درمانگاه سهند — v2.23.0

<div dir="rtl">

## نصب نسخهٔ آمادهٔ درمانگاه

در GitHub Release نسخهٔ `v2.23.0` دو فایل ویندوزی منتشر می‌شود:

- `SahandClinicPro-Setup-v2.23.0.exe` — نصب خودبسندهٔ کلاینت؛ نیازی به نصب جداگانهٔ .NET Desktop Runtime نیست.
- `SahandClinicPro-Server-v2.23.0.zip` — API خودبسنده + نصب‌کنندهٔ سرویس ویندوز + راهنمای نصب.

### ترتیب نصب پیشنهادی

1. روی رایانهٔ مرکزی، SQL Server را نصب و راه‌اندازی کنید.
2. ZIP سرور را استخراج کنید و `Install-Server-Package.bat` را با **Run as administrator** اجرا کنید.
3. رشته اتصال SQL را وارد کنید. نصب‌کننده یک کلید JWT امن می‌سازد، دیتابیس را در اولین اجرا آماده می‌کند، تمام schema نسخهٔ فعلی را خودکار اعمال می‌کند و API را به شکل Windows Service راه می‌اندازد.
4. فایل EXE کلاینت را روی رایانه‌های لازم نصب کنید. در کلاینت‌های دیگر از `appsettings.json` کنار برنامه، نشانی `http://IP-سرور:5000/api/v1` را تنظیم کنید.
5. بررسی آمادگی کامل سرور (شامل اتصال دیتابیس): `http://localhost:5000/health/ready` باید `Healthy` نشان دهد. مسیر `/health` فقط زنده‌بودن API را بررسی می‌کند.

> درمانگاه نیازی به .NET SDK، ابزار `dotnet-ef`، Visual Studio یا اجرای دستی migration ندارد. دیتابیس ساخته و تغییرات schema هنگام نصب سرور خودکار اعمال می‌شوند. اگر SQL در دسترس نباشد یا مجوز ساخت دیتابیس نداشته باشد، نصب متوقف می‌شود تا سرویس با اسکیمای ناقص بالا نیاید.

## ساخت بسته‌ها از سورس

روی Windows 10/11 به .NET 8 SDK و Inno Setup 6 نیاز دارید. اجرای `BUILD-INSTALLER.bat` خروجی زیر را می‌سازد:

- `dist\SahandClinicPro-Setup-v2.23.0.exe`
- `dist\SahandClinicPro-Server-v2.23.0.zip`

ساخت Release رسمی نیز در `.github/workflows/release-installer.yml` با Windows runner انجام می‌شود.

## محتویات

| فایل | کاربرد |
|---|---|
| `BUILD-INSTALLER.bat` | انتشار خودبسندهٔ WPF و API، سپس بسته‌بندی EXE و ZIP سرور |
| `SahandClinicPro.iss` | تنظیمات Inno Setup، زبان فارسی و میانبرهای راهنما |
| `Install-Server-Package.bat/.ps1` | نصب سرور بسته‌بندی‌شده، ساخت خودکار DB و راه‌اندازی Windows Service |
| `INSTALL-SERVER.bat` | انتشار و نصب سرور از سورس برای تیم فنی |
| `appsettings.client.example.json` | نمونهٔ نشانی API برای کلاینت |
| `RELEASE-INSTALL-GUIDE.txt` | راهنمای کوتاه نصب برای درمانگاه |

</div>
