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
```

تولید کلید JWT: `[Convert]::ToBase64String((1..64 | %{Get-Random -Max 256} | %{[byte]$_}))`

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
