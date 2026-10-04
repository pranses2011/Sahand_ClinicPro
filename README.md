# 🏥 Sahand ClinicPro — سامانه جامع مدیریت درمانگاه

> **Sahand_ClinicPro** — سامانه یکپارچه و جامع مدیریت درمانگاه با معماری **Clean Architecture**، تحت شبکه Client-Server با API مرکزی، آماده توسعه به چند شعبه و موبایل (.NET MAUI).

<div dir="rtl">

## 📊 وضعیت پروژه

![Status](https://img.shields.io/badge/وضعیت-فاز%20صفر%20(راه‌اندازی)-orange)
![Progress](https://img.shields.io/badge/پیشرفت-0%25%20(0%2F31%20گام)-lightgrey)
![.NET](https://img.shields.io/badge/.NET-8.0%20LTS-blueviolet)
![C#](https://img.shields.io/badge/C%23-12-blue)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-red)
![License](https://img.shields.io/badge/License-MIT-green)

| شاخص | مقدار |
|---|---|
| 🧩 ماژول‌ها | ۳۵+ ماژول کامل |
| 🗄️ جداول دیتابیس | ۹۰+ جدول در ۱۲ گروه |
| 🔐 لایه‌های امنیتی | ۱۴ لایه (HIPAA-Oriented) |
| 📋 گام‌های پیاده‌سازی | ۳۱ گام در ۹ فاز |
| 👥 ظرفیت هدف | ۱۰ کاربر همزمان، ۵۰۰ بیمار/روز |
| 🔗 یکپارچه‌سازی | سپاس، سیب، ناب، سینا، نسخه الکترونیک، ۱۰+ پنل پیامک، بیمه‌ها |

---

## 🛠️ پشته فناوری (Tech Stack)

| بخش | تکنولوژی | نسخه / توضیح |
|---|---|---|
| زبان برنامه‌نویسی | C# | C# 12 |
| Backend API | ASP.NET Core Web API | .NET 8 LTS (REST) |
| Desktop Client | WPF + Material Design in XAML | الگوی MVVM، RTL کامل |
| License Portal | Blazor Server | .NET 8 |
| Mobile (آینده) | .NET MAUI | Cross-platform |
| Database | SQL Server 2022 | Standard Edition |
| ORM | Entity Framework Core 8 | Code-First + Fluent API |
| احراز هویت | JWT + Refresh Token | Access ۱۵min / Refresh 7day |
| هش رمز | BCrypt.Net-Next | Work Factor 12 |
| رمزنگاری داده حساس | AES-256-GCM | فیلدهای پزشکی حساس |
| Validation | FluentValidation | با پیام‌های فارسی |
| Object Mapping | AutoMapper | MappingProfile مرکزی |
| Reporting | Stimulsoft Reports | خروجی PDF/Excel/Word |
| Logging | Serilog + Seq | Structured Logging |
| Background Jobs | Hangfire | پیامک، بکاپ، Heartbeat |
| Real-time | SignalR | صف انتظار و نوتیفیکیشن |
| Testing | xUnit + Moq + FluentAssertions | Unit + Integration |
| CI/CD | GitHub Actions | Pipeline خودکار |

---

## 🏗️ معماری (Clean Architecture)

```
┌─────────────────────────────────────────────────┐
│  🖥️ Presentation: WPF (MVVM) + Blazor Portal    │
└──────────────────────┬──────────────────────────┘
           HTTP/HTTPS REST API
┌──────────────────────▼──────────────────────────┐
│  🌐 API Layer: ASP.NET Core 8 Web API           │
│  Controllers + Middleware + JWT + RateLimit     │
└──────────────────────┬──────────────────────────┘
┌──────────────────────▼──────────────────────────┐
│  ⚙️ Application Layer: CQRS + MediatR           │
│  Use Cases + DTOs + Behaviors + Validators      │
└──────────────────────┬──────────────────────────┘
┌──────────────────────▼──────────────────────────┐
│  🧬 Domain Layer (هسته - بدون وابستگی)           │
│  Entities + Value Objects + Domain Events       │
└──────────────────────┬──────────────────────────┘
┌──────────────────────▼──────────────────────────┐
│  🔧 Infrastructure: EF Core + Repositories      │
│  UoW + SMS Providers + Sepas + License + ...    │
└─────────────────────────────────────────────────┘
```

**الگوهای طراحی:** Clean Architecture، CQRS + MediatR، Repository + Unit of Work، Strategy، Factory، Adapter، Decorator

**قواعد طلایی معماری:**
- ✅ Domain هیچ وابستگی خارجی ندارد
- ✅ Application فقط به Domain وابسته است
- ✅ Infrastructure به Application و Domain وابسته است
- ✅ API به Application وابسته است (نه مستقیم به Infrastructure)
- ✅ تمام کامنت‌ها و XML Summaryها به زبان فارسی مفصل
- ✅ تمام کدها به انگلیسی استاندارد (PascalCase/camelCase)

---

## 🧩 ماژول‌های اصلی

- **🏥 هسته‌ای:** بیماران، نوبت‌دهی (تقویم شمسی)، پذیرش، ویزیت (SOAP)، نسخه‌نویسی (داخلی + الکترونیک)
- **💰 مالی:** صندوق و فاکتور، بیمه (استعلام آنلاین)، حسابداری دوبل (چهارسطحی ایرانی)، تعرفه‌ها، کارانه پزشکان
- **🔬 پاراکلینیک:** آزمایشگاه، تصویربرداری، دندانپزشکی (چارت FDI)، چشم‌پزشکی، فیزیوتراپی
- **💊 داروخانه:** موجودی بچ‌محور، تحویل با اسکن نسخه، تاریخ انقضا، انبارگردانی، هشدار موجودی
- **📞 ارتباطات:** ۱۰+ پنل پیامک (Kavenegar، ملی‌پیامک، ...)، صف ارسال، یادآوری خودکار نوبت
- **👥 اداری:** پرسنل، حضور و غیاب، حقوق، کاربران و RBAC گرانولار
- **📊 گزارش:** داشبورد مدیریتی، گزارش‌ساز Stimulsoft، ۱۰+ گزارش آماده
- **⚙️ زیرساخت:** بکاپ ۳سطحی خودکار، Audit Log کامل، تنظیمات، مدیریت اسناد

---

## 🔗 یکپارچه‌سازی سامانه‌های دولتی

| سامانه | کاربرد | پروتکل |
|---|---|---|
| 🏥 **سپاس** (ihio.gov.ir) | پرونده الکترونیک سلامت، ارسال ویزیت | REST + OAuth2 + HL7 FHIR |
| 💊 **نسخه الکترونیک سلامت** | ارسال نسخه، کد رهگیری، استعلام تداخل | SOAP + REST |
| 💊 **نسخه الکترونیک تأمین اجتماعی** | ثبت و تایید نسخه | REST + OAuth2 |
| 🩺 **سیب** | مکانیزاسیون مراقبت‌های سطح یک | REST |
| 🏥 **ناب / سینا** | اتوماسیون بهداشتی، نظام ارجاع | REST |
| 🏦 **بیمه‌ها (۸+)** | استعلام، تایید، تسویه، مطالبات | REST/SOAP + Plugin |

---

## 🔐 سامانه لایسنس اختصاصی

- **License Portal (Blazor Server):** پنل فروشنده با دیتابیس مجزا — صدور آنلاین/آفلاین، کنترل ماژول و ظرفیت
- **License Client (در نرم‌افزار):** فعال‌سازی با Hardware Fingerprint، اعتبارسنجی امضای RSA (SHA256withRSA)، Heartbeat روزانه
- **انواع لایسنس:** اشتراکی (ماهانه تا سالانه) و دائمی
- **مدیریت ماژولار:** ۷ ماژول پایه رایگان + ۱۶+ ماژول اختیاری قابل فعال‌سازی

---

## 📁 ساختار ریپازیتوری

```
Sahand_ClinicPro/
├── 📄 README.md                    # این فایل - معرفی پروژه
├── 📄 TODO.md                      # چک‌لیست ۳۱ گام پیاده‌سازی
├── 📄 PROGRESS.md                  # وضعیت لحظه‌ای پروژه
├── 📄 CHANGELOG.md                 # تاریخچه تغییرات
├── 📄 .gitignore                   # قواعد Visual Studio / .NET
├── 📄 LICENSE                      # MIT
│
├── 📁 docs/
│   ├── 📁 design/                  # مستندات طراحی (۶ فایل HTML)
│   │   ├── 01-overview.html        # تحلیل نیازمندی‌ها و معماری
│   │   ├── 02-database.html        # طراحی دیتابیس (۹۰+ جدول)
│   │   ├── 03-flowcharts.html      # فلوچارت‌ها و نمودارها
│   │   ├── 04-wireframes.html      # وایرفریم ۱۶ صفحه
│   │   ├── 05-security-timeline.html  # امنیت ۱۴ لایه + زمان‌بندی
│   │   └── 06-license-integration.html # لایسنس + یکپارچه‌سازی
│   ├── 📁 prompts/                 # پرامپت‌های پیاده‌سازی AI
│   │   ├── prompt-part1.html       # معرفی + معماری + دیتابیس
│   │   ├── prompt-part2.html       # ماژول‌ها + API + WPF
│   │   └── prompt-part3.html       # امنیت + لایسنس + دستورات نهایی
│   ├── 📁 api/                     # مستندات API (پس از پیاده‌سازی)
│   └── 📁 guides/                  # راهنماهای کاربری
│
├── 📁 src/                         # کدهای اصلی (از گام ۱)
├── 📁 tests/                       # تست‌ها (از گام ۳۰)
├── 📁 scripts/                     # اسکریپت‌ها (setup-db.sql، deploy.ps1)
└── 📁 .github/workflows/           # GitHub Actions (CI)
```

---

## 📋 مدیریت پروژه

- **`TODO.md`** — چک‌لیست کامل ۳۱ گام در ۹ فاز (به‌روزرسانی پس از هر گام)
- **`PROGRESS.md`** — وضعیت لحظه‌ای، گام فعلی، آخرین Commit
- **`CHANGELOG.md`** — تاریخچه تغییرات هر نسخه

### 🔄 گردش کار Git

```bash
# برای هر فاز یک Branch و در پایان Merge به main
git checkout -b feature/phase-1-infrastructure
# ... کدنویسی + Commit مکرر ...
git checkout main && git merge feature/phase-1-infrastructure
```

### 📝 قالب Commit Message

```
<emoji> [Step N]: <توضیح>

✨ feat: ویژگی جدید      🐛 fix: رفع باگ
📝 docs: مستندات         ♻️ refactor: بازنویسی
✅ test: تست             🔧 config: تنظیمات
🗄️ db: دیتابیس           🔐 security: امنیت
🚀 deploy: استقرار       📦 build: ساخت
```

**نمونه:** `✨ [Step 7]: افزودن موجودیت بیمار با کامنت‌های فارسی`

---

## 🚀 نقشه راه (۹ فاز / ۳۱ گام)

| فاز | عنوان | گام‌ها | وضعیت |
|---|---|---|---|
| ۰ | راه‌اندازی ریپازیتوری | — | ✅ کامل |
| ۱ | راه‌اندازی و زیرساخت | ۱–۴ | ⬜ |
| ۲ | ماژول‌های هسته‌ای | ۵–۹ | ⬜ |
| ۳ | Desktop Client | ۱۰–۱۲ | ⬜ |
| ۴ | ماژول‌های درمانی | ۱۳–۱۶ | ⬜ |
| ۵ | مالی و حسابداری | ۱۷–۱۹ | ⬜ |
| ۶ | پاراکلینیک و داروخانه | ۲۰–۲۲ | ⬜ |
| ۷ | ارتباطات و یکپارچه‌سازی | ۲۳–۲۵ | ⬜ |
| ۸ | گزارش و اداری | ۲۶–۲۸ | ⬜ |
| ۹ | نهایی‌سازی | ۲۹–۳۱ | ⬜ |

جزئیات کامل هر گام در فایل [`TODO.md`](./TODO.md) و دستورات اجرایی در `docs/prompts/prompt-part3.html`.

---

## 📄 مجوز

این پروژه تحت مجوز **MIT** منتشر می‌شود — جهت مشاهده جزئیات به فایل [LICENSE](./LICENSE) مراجعه کنید.

</div>
