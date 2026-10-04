# 📊 وضعیت پیشرفت پروژه

> این فایل وضعیت لحظه‌ای پروژه را نشان می‌دهد — پس از هر تغییر به‌روزرسانی می‌شود.

## 🎯 پروژه کامل شد — هر ۳۱ گام از ۹ فاز ✅ | نسخه v1.0.0-alpha
## 📅 آخرین به‌روزرسانی: 2026-10-05
## 🔄 آخرین Commit: 🧬 Step 2: Domain Layer (شاخه feature/phase-02-domain)

## ✅ انجام شده:
- مطالعه کامل ۹ فایل مستندات (۶ فایل طراحی + ۳ فایل پرامپت پیاده‌سازی)
- ایجاد ریپازیتوری GitHub (Private): `pranses2011/Sahand_ClinicPro`
- استخراج مستندات در `docs/design/` و `docs/prompts/`
- ایجاد فایل‌های مدیریتی: README.md، TODO.md، PROGRESS.md، CHANGELOG.md، .gitignore
- LICENSE (MIT) توسط GitHub ایجاد شد
- اولین Commit و Push انجام شد

### گام ۱ — Solution Setup (شاخه feature/phase-01-foundation):
- ایجاد `Sahand.ClinicPro.sln` با ۱۱ پروژه (۷ در src + ۴ در tests) با نام‌گذاری `Sahand.ClinicPro.*`
- پیکربندی مرکزی: `Directory.Build.props` (C# 12، Nullable، تولید مستند XML، EnableWindowsTargeting) + `Directory.Packages.props` (CPM با پین نسخه‌ها) + `nuget.config` + `.editorconfig`
- اسکلت DI: `AddApplication` (MediatR + AutoMapper + FluentValidation) و `AddInfrastructure` (نقشه گام‌های ۳/۴/۲۳/۲۴/۲۵ به‌صورت کامنت)
- `Program.cs` کامل API: Serilog دو مرحله‌ای + Swagger با عنوان فارسی + نقطه /health
- پروژه WPF دست‌نویس (قالب لینوکس نبود): Material Design 3 + RTL + فونت وزیرمتن‌خور
- پنل Blazor License Portal با تعامل سراسری سرور
- ۶ تست دودی واقعی (DI + /health) — همه سبز
- Build کامل: ۰ خطا، ۰ هشدار
- تصمیم امنیتی مستند: AutoMapper 13.0.1 + NuGetAuditSuppress/NoWarn NU1903 (جزئیات در TODO.md)

## 🔨 در حال انجام:
- (در انتظار تأیید شروع گام ۲ — Domain Layer)

## 📋 برنامه بعدی:
- گام ۲: Domain Layer — BaseEntity + AuditableEntity + ISoftDeletable + Result + همه Entities (۴۵+) + Enums (۳۰+) + Value Objects + Interfaces + Exceptions
- سپس گام ۳: Infrastructure - Persistence (ClinicProDbContext + Configurations + Migrations + Seeders)
- Merge شاخه feature/phase-01-foundation به main پس از تکمیل گام‌های ۱ تا ۴ (پایان فاز ۱)


---

## 📊 گزارش چرخه دوم — فاز A+B: تکمیل کلاینت دسکتاپ (2026-10-05)

### گام V0 — پیش‌نیازهای API:
- `LookupsController` با چهار endpoint مرجع: بیمه‌ها / تشخیص‌های ICD-10 / داروها / آزمایش‌ها (فقط احراز هویت — تصمیم معماری مستند)
- `GET /api/v1/lab/orders/{id}` — جزئیات درخواست آزمایش با اقلام (برای ثبت نتیجه)
- `GET /api/v1/prescriptions` — فهرست صفحه‌بندی‌شده نسخه‌ها (فیلتر بیمار/وضعیت + جستجو)

### گام‌های V1-V8 — نُه ماژول دسکتاپ با الگوی مستقر (VM + ApiClient + DataTemplate):
- نوبت‌دهی: فهرست روزانه با فیلتر پزشک/وضعیت + پنجره ثبت نوبت با جستجوی بیمار + چهار عملیات وضعیت
- پذیرش: صف امروز با زمان انتظار + فرم پذیرش حضوری/نوبتی + جریان کامل وضعیت + بیمه‌های فرم بیمار تکمیل شد
- ویزیت: ویرایشگر SOAP چهاربخشی + جستجوی تشخیص ICD-10 + ذخیره پیش‌نویس/تکمیل
- نسخه: فهرست صفحه‌بندی‌شده + ویرایشگر اقلام دارویی با جستجوی دارو + نمایش جزئیات
- مالی: فاکتورها با اقلام قابل ویرایش و جمع زنده + ثبت پرداخت با روش‌های پنج‌گانه
- آزمایشگاه: درخواست با انتخاب تست‌های چک‌باکسی + ثبت نتیجه قلم‌به‌قلم با بازه طبیعی
- داروخانه: موجودی با فیلتر کمبود + ورود بچ دارو + تحویل نسخه FEFO با پیش‌نمایش اقلام
- گزارش‌ها: درآمد روزانه و عملکرد پزشکان با نمودار ستونی WPF + چهار کارت جمع
- تنظیمات: ویرایشگر تنظیمات + لاگ حسابرسی با فیلتر و صفحه‌بندی + پرسنل + پشتیبان

### تعمیر نقص مستمر از چرخه اول:
- پروژه Infrastructure.IntegrationTests از گام ۲۹/۳۰ کامپایل نمی‌شد (پکیج EF InMemory مفقود + ارجاع فضای‌نام)
- پرچم `IsInMemory` در DbContext + مولد مقدار MRN سمت کاربر — بدون تغییر رفتار SQL Server
- **نتیجه: ۴۹/۴۹ تست سبز در کل Solution**

### وضعیت فنی:
- همه ۱۱ آیتم ناوبری دسکتاپ فعال شد
- شاخه feature/phase-10-desktop — ۱۰ کامیت

## 💬 یادداشت‌ها:
- معماری: Clean Architecture + CQRS + Repository + Unit of Work
- نام‌گذاری: پروژه‌ها/فضای‌نام‌ها `Sahand.ClinicPro.*` — نام کلاس‌های مستندشده حفظ شد (مثل ClinicProDbContext)
- تداخل نام WPF: در Desktop همیشه `System.Windows.Application` (تداخل با فضای‌نام Application) — در remarks کد توضیح داده شد
- تمام کامنت‌های کد فارسی مفصل؛ نام‌گذاری کد انگلیسی استاندارد؛ فایل‌ها با BOM (سازگاری ویندوز)
- قواعد Commit: `<emoji> [Step N]: <توضیح>` + Push مکرر (حداکثر هر ۳۰ دقیقه)
- برای هر فاز یک Branch جداگانه: `feature/phase-N-...` سپس Merge به main در پایان فاز
- توکن GitHub هرگز در فایل‌های ریپازیتوری ذخیره نشود (تنها تنظیمات لوکال)
