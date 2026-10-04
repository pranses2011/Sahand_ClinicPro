# Changelog — Sahand_ClinicPro

تمام تغییرات مهم این پروژه در این فایل ثبت می‌شود.
قالب بر اساس [Keep a Changelog](https://keepachangelog.com/fa/1.1.0/) و نسخه‌بندی [SemVer](https://semver.org/lang/fa/) است.

## [Unreleased]

### 🏗️ Added - 2026-10-05 (گام ۱: Solution Setup)
- ایجاد `Sahand.ClinicPro.sln` با **۱۱ پروژه** مطابق Clean Architecture (شاخه `feature/phase-01-foundation`):
  - `src/`: Domain، Application، Infrastructure، Shared، API، Desktop (WPF)، LicensePortal (Blazor Server)
  - `tests/`: Domain.UnitTests، Application.UnitTests، Infrastructure.IntegrationTests، API.IntegrationTests
- مدیریت مرکزی نسخه پکیج‌ها با `Directory.Packages.props` (CPM + پین انتقالی) — پکیج‌های مایکروسافتی هم‌نسخه 8.0.x (EF Core 8.0.31)
- پیکربندی مشترک ساخت با `Directory.Build.props` (C# 12، Nullable، تولید مستندات XML فارسی، `EnableWindowsTargeting`)
- `nuget.config` (فقط منبع رسمی) و `.editorconfig` (سبک کد + نام‌گذاری)
- اسکلت تزریق وابستگی:
  - `AddApplication` — ثبت خودکار MediatR + AutoMapper + FluentValidation از اسمبلی
  - `AddInfrastructure` — اسکلت با نقشه راه گام‌های ۳/۴/۲۳/۲۴/۲۵ در کامنت‌های فارسی
- `Program.cs` لایه API: Serilog دومرحله‌ای + Swagger با توضیحات فارسی + نقطه بررسی سلامت `/health`
- پروژه WPF دست‌نویس (قالب در لینوکس موجود نبود): Material Design 3، RTL کامل، تم فیروزه‌ای/نیلی
- پنل لایسنس Blazor Server (تعامل سراسری) با صفحه خوش‌آمد فارسی
- ۴ فایل تست دودی واقعی (۶ تست): صحت DI دو لایه + اجرای درون‌حافظه‌ای API و پاسخ `/health` — **همه سبز**
- اسکریپت `scripts/step01-create-solution.sh` برای بازتولید ساختار قالبی
- انتخاب نسخه‌های رایگان نهایی: MediatR 12.5.0، FluentValidation 11.12.0، AutoMapper 13.0.1، FluentAssertions 6.12.2 (جزئیات در TODO.md)

### Security - 2026-10-05
- مستندسازی ریسک AutoMapper 13.0.1 (هشدار GHSA-rvv3-g6hj-g44x / NU1903) و سرکوب دقیق همراه با توجیه کامل؛ بازبینی در گام ۲۹ (امنیت)

### 📦 Added - 2026-10-05 (فاز صفر: راه‌اندازی ریپازیتوری)
- ایجاد ریپازیتوری GitHub با نام `Sahand_ClinicPro` (Private)
- افزودن مستندات طراحی (۶ فایل HTML) در `docs/design/`:
  - `01-overview.html` — تحلیل نیازمندی‌ها و معماری
  - `02-database.html` — طراحی دیتابیس (۹۰+ جدول)
  - `03-flowcharts.html` — فلوچارت‌ها و نمودار ارتباطات
  - `04-wireframes.html` — وایرفریم ۱۶ صفحه اصلی
  - `05-security-timeline.html` — امنیت ۱۴ لایه + زمان‌بندی ۱۲ ماهه
  - `06-license-integration.html` — سامانه لایسنس + یکپارچه‌سازی + الگوریتم‌ها
- افزودن پرامپت‌های پیاده‌سازی (۳ فایل HTML) در `docs/prompts/`:
  - `prompt-part1.html` — معرفی + معماری + دیتابیس
  - `prompt-part2.html` — ماژول‌ها + API + WPF
  - `prompt-part3.html` — امنیت + لایسنس + دستورات نهایی (۳۱ گام)
- ایجاد `README.md` — معرفی جامع پروژه و پشته فناوری
- ایجاد `TODO.md` — چک‌لیست ۳۱ گام پیاده‌سازی در ۹ فاز
- ایجاد `PROGRESS.md` — ردیابی وضعیت لحظه‌ای پروژه
- ایجاد `.gitignore` — قالب Visual Studio / .NET با قواعد امنیتی پروژه
- ایجاد ساختار اولیه پوشه‌ها: `docs/api/`، `docs/guides/`، `scripts/`، `.github/workflows/`
- افزودن `ci.yml` — جایگاه GitHub Actions (فعال‌سازی در گام‌های بعدی)
- LICENSE (MIT) ایجاد شد

[Unreleased]: https://github.com/pranses2011/Sahand_ClinicPro/compare/main...HEAD
