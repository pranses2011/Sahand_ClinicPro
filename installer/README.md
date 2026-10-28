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

### مجوز «بازیابی اطلاعات از فایل پشتیبان» کاملاً خودکار شد

`RESTORE DATABASE` مجوز **سطح سرور** میخواهد (نقش `dbcreator` یا `sysadmin`) و مالک بودن روی خود دیتابیس کافی نیست. نصاب سرور — بدون نیاز به اینکه شما بدانید لاگین SQL چیست یا رمز `sa` را داشته باشید — چهار لایه را پشتسرهم امتحان میکند:

1. اتصال با همان شناسههای رشتهٔ اتصال API و تشخیص لاگین دقیق با `SUSER_SNAME()`؛ در حالت ویندوزی، حساب سرویس نصب قبلی، `NT AUTHORITY\SYSTEM`، حساب جاری، حساب ماشین (`DOMAIN\PC$`) و حساب مجازی سرویس پوشش داده میشوند؛
2. اتصال با احراز هویت ویندوزی حساب جاری (همان حسابی که معمولاً SQL Express را نصب کرده است)؛
3. پرسش نام/رمز یک حساب مدیر SQL (مثلاً `sa`) — فقط اگر دو لایهٔ اول جواب نداد؛
4. **لایهٔ نهایی کاملاً بدون رمز:** اگر SQL Server روی همان رایانه باشد، سرویس `MSSQLSERVER`/`MSSQL$INSTANCE` بهصورت موقت در حالت تککاربره (`net start <service> /m`) بالا میآید؛ طبق مستندات مایکروسافت، هر عضو `Administrators` محلی در این حالت `sysadmin` محسوب میشود، پس نصاب خودش نقش `dbcreator` را میدهد. سپس در بلوک `finally` بدون استثنا SQL Server به حالت عادی چندکاربره برگردانده و `SERVERPROPERTY('IsSingleUser')` راستیآزمایی میشود. پارامتر `/m` فقط همان استارت را تحت تأثیر قرار میدهد و در رجیستری ذخیره نمیشود؛ با یک ریاستارت ساده همیشه وضع عادی برمیگردد. فایل نجات `SQL-RECOVERY-Restart-Normal.bat` هم کنار برنامه ساخته میشود.

همهٔ نتایج (لاگین هدف، مسیر موفق، آخرین خطا) در `C:\SahandClinicPro\Server\restore-permission.status.txt` ثبت میشود. اگر بههر دلیلی هیچ مسیری موفق نشد، فایل `Grant-RestorePermission.sql` ساخته میشود تا یکبار در SSMS با حساب `sysadmin` اجرا شود؛ نصب سرور در این حالت متوقف نمیشود.

**تعمیر نصبهای قدیمی بدون نصب دوباره:** فایل `Enable-RestorePermission.bat` را در پوشهٔ سرور با **Run as administrator** اجرا کنید. همان چهار لایه روی نصب موجود اعمال میشود و نیازی به بستهٔ کامل سرور نیست.

سوئیچها و متغیرها: `-NoSqlSingleUserElevation` (غیرفعالکردن ریاستارت موقت)، `-NoPrompt` (نصب بیتعامل)، `-SqlAdminUser`/`-SqlAdminPassword` یا متغیرهای محیطی `CLINICPRO_SQL_ADMIN_USER` / `CLINICPRO_SQL_ADMIN_PASSWORD` (دادن مستقیم حساب مدیر SQL).

> ⚠️ لایهٔ چهارم SQL Server محلی را حدود ۳۰ ثانیه موقتاً ریاستارت میکند. اگر SQL Server شما پایگاه دادههای حساس دیگری هم سرویس میدهد، میتوانید با سوئیچ `-NoSqlSingleUserElevation` این لایه را خاموش کنید و بهجای آن حساب مدیر SQL را بدهید.

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
| `Enable-RestorePermission.bat/.ps1` | برقراری خودکار چهارلایهٔ نقش `dbcreator` — در نصب تازه و همچنین تعمیر مستقل نصب قدیمی |
| `INSTALL-SERVER.bat` | انتشار و نصب سرور از سورس برای تیم فنی |
| `appsettings.client.example.json` | نمونهٔ نشانی API برای کلاینت |
| `RELEASE-INSTALL-GUIDE.txt` | راهنمای کوتاه نصب برای درمانگاه |

</div>
