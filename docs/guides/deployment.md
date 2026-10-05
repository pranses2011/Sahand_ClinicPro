# 🚀 راهنمای استقرار — Sahand ClinicPro

## ۱. پیش‌نیازهای سرور

| مورد | حداقل | توصیه |
|------|-------|-------|
| OS | Windows Server 2016 / Ubuntu 22.04 | Windows Server 2022 |
| CPU | 4 هسته | 8 هسته |
| RAM | 8 GB | 16 GB |
| SQL Server | 2022 Express | 2022 Standard |
| .NET | 8 Runtime (Hosting Bundle) | 8 SDK برای به‌روزرسانی |

## ۲. دیتابیس

```powershell
# ساخت دیتابیس با Migration
cd src/Sahand.ClinicPro.API
dotnet ef database update --project ../Sahand.ClinicPro.Infrastructure --startup-project .

# یا با SQL (خروجی اسکریپت)
dotnet ef migrations script --idempotent `
  --project ../Sahand.ClinicPro.Infrastructure --startup-project . `
  -o ../../deploy/sahand-clinicpro-db.sql
```

نکته: کاربر سرویس SQL باید دسترسی `db_datareader` + `db_datawriter` + `db_ddladmin` داشته باشد.

## ۳. پیکربندی تولید

هرگز در appsettings.json ذخیره نکنید — از متغیرهای محیطی:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=...;Database=SahandClinicProDb;..."
$env:Jwt__SecretKey="<کلید ۲۵۶ بیتی تصادفی>"
$env:Jobs__Enabled="true"
$env:Sms__Kavenegar__ApiKey="<کلید پنل>"
# اختیاری — داشبورد لاگ ساختاریافته Seq (بدون تنظیم، سینک Seq فعال نمی‌شود — چرخه ۱۲)
$env:SEQ_SERVER_URL="http://your-seq-server:5341"
# اختیاری — سهمیه نگهداشت نسخه‌های پشتیبان خودکار (پیش‌فرض ۱۴ — چرخه ۱۲)
$env:Backup__RetentionCount="30"
$env:Backup__Directory="D:/Backups/SahandClinicPro"
```

تولید کلید JWT: `[Convert]::ToBase64String((1..64 | %{Get-Random -Max 256} | %{[byte]$_}))`

> ⚠️ **چرخه ۱۲ — نگهبان کلید JWT:** بدون `Jwt__SecretKey` معتبر (حداقل ۳۲ بایت)، سرور در محیط
> تولید با پیام خطای فارسی بالا نمی‌آید. این رفتار عمدی است تا استقرار ناامن ممکن نباشد.

### ۳-۱. کارهای زمان‌بندی‌شده (Hangfire) — چه چیزی با `Jobs:Enabled` فعال می‌شود؟

`Jobs__Enabled="true"` چهار کار پس‌زمینه را ثبت می‌کند؛ بدون آن هیچ‌کدام اجرا نمی‌شوند:

| شناسه جاب | زمان‌بندی | کارکرد | پیامد غیرفعال‌بودن |
|:---|:---|:---|:---|
| `sms-queue` | هر دقیقه | ارسال صف پیامک‌های زمان‌دار (یادآوری نوبت و…) | پیامک‌ها در صف می‌مانند و ارسال نمی‌شوند |
| `license-heartbeat` | روزانه ۳ بامداد | تپ‌قلب به سرور لایسنس + دریافت پیام‌های پشتیبانی و تازه‌سازی ماژول‌ها | مهلت آفلاین ۳۰ روزه شروع به شمارش معکوس می‌کند؛ پیام‌های پشتیبانی دریافت نمی‌شوند |
| `attendance-sync` | هر دقیقه | کشیدن ضربه‌ها از دستگاه‌های حضور و غیاب (ZKTeco) | ضربه‌های جدید از دستگاه خوانده نمی‌شوند |
| `backup-nightly` | روزانه ۲ بامداد | ثبت پشتیبان خودکار + پاک‌سازی نسخه‌های فراتر از `Backup:RetentionCount` | پشتیبان‌گیری خودکار انجام نمی‌شود (فقط دستی) |

داشبورد پایش جاب‌ها: با فعال‌بودن `Jobs:Enabled` مسیر `/jobs` (Hangfire Dashboard) در دسترس
مدیران سیستم قرار می‌گیرد — سوابق اجرا، خطاها و زمان‌بندی هر جاب قابل مشاهده است.

## ۴. سرویس ویندوز (Windows Service)

```xml
<!-- در csproj API موجود است -->
<PropertyGroup>
  <SelfContained>false</SelfContained>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>
```

```powershell
sc create SahandClinicProApi binPath= "C:\Apps\SahandClinicPro\Sahand.ClinicPro.API.exe"
sc start SahandClinicProApi
```

## ۵. کلاینت WPF

1. انتشار: `dotnet publish src/Sahand.ClinicPro.Desktop -c Release -r win-x64`
2. فونت وزیرمتن را در پوشه `Fonts` کنار exe قرار دهید
3. `appsettings.json` کلاینت: نشانی سرور + پورت
4. تست دسترسی: منوی «تنظیمات» ← «بررسی اتصال»

## ۶. پشتیبان‌گیری

- برنامه‌ریزی سه‌سطحی: کامل روزانه + افزایشی ۶ساعته + لاگ
- مسیر پیش‌فرض: `Backup:Directory` در تنظیمات (خروجی از سرور قابل تغییر)
- نگهداری: حداقل ۳۰ نسخه کامل + انتقال هفتگی به اشتراک شبکه

## ۷. چک‌لیست راه‌اندازی اول

- [ ] دیتابیس ساخته شد و Migration اجرا شد
- [ ] ورود ادمین موفق و رمز تغییر یافت
- [ ] اطلاع درمانگاه (Settings) تکمیل شد
- [ ] پنل پیامک فعال و اعتبار بررسی شد
- [ ] کاربران نقش‌ها دریافت کردند
- [ ] تست چاپی فاکتور و نسخه
- [ ] بکاپ خودکار زمان‌بندی و تست بازیابی شد
- [ ] لایسنس فعال شد (برخط یا برون‌خط)
