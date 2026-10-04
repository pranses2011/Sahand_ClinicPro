# 🏥 سامانه جامع مدیریت درمانگاه سهند — Sahand ClinicPro

<div dir="rtl">

سامانه یکپارچه مدیریت درمانگاه برای محیط ایران: نوبت‌دهی، پذیرش، ویزیت با الگوی SOAP، نسخه الکترونیک، مالی با حسابداری دوبل، پاراکلینیک، داروخانه، پیامک و اتصال به سامانه‌های دولتی.

</div>

## 🧱 معماری

```
┌─────────────────────────────────────────────────────────┐
│  Clients:  WPF Desktop (MaterialDesign/RTL)  │  Blazor License Portal │
│                       ↓ REST /api/v1                    │
│  API: ASP.NET Core 8 — 24 controller groups             │
│                       ↓ MediatR (CQRS + 4 Behaviors)    │
│  Application: Features × 12 modules, Validators (FA)    │
│                       ↓                                 │
│  Domain: 87 entities, 89 enums, ValueObjects, Events    │
│                       ↑ IRepository/IUnitOfWork         │
│  Infrastructure: EF Core 8 (SQL Server), Identity (JWT),│
│  SMS chain, Government (Sepas/eRx), License (RSA)       │
└─────────────────────────────────────────────────────────┘
```

- **Clean Architecture** + CQRS (Commands/Queries per folder) + Repository/UnitOfWork
- **Soft delete** سراسری با Query Filter و اینترسپتورهای حسابرسی
- **SQL Server 2022** با ۸۷ جدول، ۱۵ Sequence (MRN/APT/RCP/VST/RX/INV/...)
- کد انگلیسی / کامنت XML فارسی در تمام لایه‌ها

## 🚀 راه‌اندازی سریع

### پیش‌نیازها
- .NET 8 SDK
- SQL Server 2022 (مستقیم یا Docker)
- (برای کلاینت) ویندوز 7+ با .NET 8 Desktop Runtime

### ۱) API

```bash
cd src/Sahand.ClinicPro.API
dotnet ef database update --project ../Sahand.ClinicPro.Infrastructure
dotnet run
```

- Swagger: `http://localhost:5000/swagger`
- سلامت: `http://localhost:5000/health`
- بذرهای خودکار: ۸ نقش، ۱۵۰ دسترسی، ادمین، ۱۴ تخصص، ۲۳ کد ICD-10، ۸ بیمه، ۲۳ ماژول

### ۲) ورود اول

<div dir="rtl">

| فیلد | مقدار |
|------|-------|
| نام کاربری | `admin` |
| رمز عبور | `Admin@123` |

⚠️ **الزامی**: تغییر رمز پس از نخستین ورود (MustChangePassword فعال است)

</div>

### ۳) کلاینت WPF

```bash
cd src/Sahand.ClinicPro.Desktop
dotnet build   # روی ویندوز: dotnet run
```

نشانی API در `appsettings.json` کلاینت → `ApiSettings:BaseUrl`

## ⚙️ پیکربندی کلیدی (appsettings.json)

| کلید | توضیح |
|------|-------|
| `ConnectionStrings:DefaultConnection` | رشته اتصال SQL Server |
| `Jwt:SecretKey` | ⚠️ در تولید از متغیر محیطی/KeyVault |
| `Jobs:Enabled` | فعال‌سازی Hangfire (نیازمند DB در دسترس) |
| `Sms:Kavenegar:ApiKey` | کلید پنل پیامک |
| `Government:Sepas` | نشانی و توکن سامانه سپاس |
| `License:RsaPublicKey` | کلید عمومی بررسی امضای لایسنس |

## 🧩 وضعیت ماژول‌ها

| ماژول | API | CQRS | نکات |
|-------|-----|------|------|
| احراز هویت | ✅ | ✅ | JWT + قفل ۵ تلاشی + چرخش RefreshToken |
| بیماران | ✅ | ✅ | کد ملی checksum + MRN خودکار |
| پزشکان | ✅ | ✅ | |
| نوبت‌دهی | ✅ | ✅ | ظرفیت/غیبت/تداخل |
| پذیرش | ✅ | ✅ | صف پویا + زمان انتظار |
| ویزیت (SOAP) | ✅ | ✅ | ICD-10 الزامی برای تکمیل |
| نسخه | ✅ | ✅ | رد تداخل ممنوع مصرف |
| فاکتور/پرداخت | ✅ | ✅ | سند حسابداری خودکار دوبل |
| حسابداری | ✅ | ✅ | چهارسطحی + قانون توازن |
| بیمه | ✅ | ✅ | مطالبه دوره‌ای + استعلام برخط با fallback برون‌خط |
| آزمایشگاه | ✅ | ✅ | جریان وضعیت + رویداد پیامک |
| داروخانه | ✅ | ✅ | تحویل FEFO + هشدار موجودی |
| پاراکلینیک | ✅ | ✅ | تصویربرداری/دندان/چشم/فیزیوتراپی |
| پیامک | ✅ | ✅ | زنجیره جایگزینی + صف Hangfire |
| سپاس + نسخه الکترونیک | ✅ | ✅ | لاگ کامل ApiCallLog |
| لایسنس | ✅ | ✅ | سه‌مرحله‌ای + میدلور ماژول |
| گزارش‌ها | ✅ | ✅ | درآمد روزانه + عملکرد پزشکان |
| منابع انسانی | ✅ | ✅ | پرسنل + مرخصی |
| تنظیمات/بکاپ/لاگ | ✅ | ✅ | |
| داشبورد | ✅ | — | آمار روزانه |
| WPF Client | 🖥️ | — | ۱۱ ماژول کامل: ورود، داشبورد، بیماران، نوبت‌دهی، پذیرش، ویزیت SOAP، نسخه، مالی، آزمایشگاه، داروخانه، گزارش‌ها، تنظیمات (چرخه v1.1) |
| پنل لایسنس Blazor | 🌐 | — | داشبورد + صدور/تمدید/ابطال + فعال‌سازی‌ها + لاگ اعتبارسنجی (چرخه v1.1) |
| استعلام برخط بیمه | ✅ | ✅ | سه تلاش + کش ۳۰ دقیقه + سقوط برون‌خط (چرخه v1.1) |

## 🧪 تست‌ها

```bash
dotnet test Sahand.ClinicPro.sln
```

۵۰ تست سبز در ۴ پروژه:
- تقویم شمسی و اعتبارسنج‌های فارسی (۳۷)
- گرداننده ثبت بیمار با EF InMemory (۶)
- یکپارچگی API شامل **جریان کامل بیمار**: ورود ادمین → ثبت بیمار → جستجو → نوبت → پذیرش → ویزیت SOAP → نسخه → فاکتور (۷)

## 📂 ساختار

```
src/
├── Sahand.ClinicPro.Domain          # ۸۷ موجودیت — بدون وابستگی
├── Sahand.ClinicPro.Application     # CQRS × ۱۲ ماژول + Behaviors
├── Sahand.ClinicPro.Infrastructure  # EF Core + Identity + SMS + Gov + License
├── Sahand.ClinicPro.API             # ۲۰+ کنترلر + ۵ میدلور
├── Sahand.ClinicPro.Desktop         # WPF (MVVM, RTL, MaterialDesign)
├── Sahand.ClinicPro.LicensePortal   # پنل وب لایسنس — Blazor RTL کامل
└── Sahand.ClinicPro.Shared          # قراردادهای مشترک
tests/                                # ۴ پروژه تست
docs/                                 # مستندات طراحی و راهنما
scripts/                              # مولدهای بازتولید کد (Python)
```

## 🔐 امنیت

- BCrypt ضریب ۱۲ + سیاست رمز + تاریخچه ۵ رمز
- قفل حساب ۳۰ دقیقه پس از ۵ تلاش ناموفق
- Rate limit: عمومی ۱۰۰/دقیقه، ورود ۵/دقیقه
- مجوزدهی پویا `PERMISSION:x` با نقش Admin همه‌دسترس
- رمزنگاری AES-256-GCM سرویس داده‌های حساس
- سرآیندهای امنیتی + لاگ عملیات با پاک‌سازی فیلدهای حساس
- میدلور لایسنس با کدهای ۴۰۳ استاندارد

## 📄 مجوز

MIT — جزئیات در [LICENSE](LICENSE)
