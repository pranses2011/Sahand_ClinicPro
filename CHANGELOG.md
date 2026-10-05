# Changelog — Sahand_ClinicPro

تمام تغییرات مهم این پروژه در این فایل ثبت می‌شود.
قالب بر اساس [Keep a Changelog](https://keepachangelog.com/fa/1.1.0/) و نسخه‌بندی [SemVer](https://semver.org/lang/fa/) است.

## [Unreleased] — چرخه سوم (v1.2 در حال توسعه)

### 🌐 سرور لایسنس PHP برای cPanel — فاز A (گام‌های L1-L5)

#### Added
- **سرور لایسنس PHP خالص + MySQL** بدون هیچ وابستگی خارجی — قابل نصب روی هاست اشتراکی cPanel (`src/LicenseServer.Php/`):
  - نصب‌کننده وب ۳ مرحله‌ای: بررسی پیش‌نیاز ← اطلاعات دیتابیس ← حساب مدیر + تولید جفت‌کلید RSA-2048 + قفل نصب خودکار
  - پنل مدیریت فارسی RTL: داشبورد آماری، صدور (کاتالوگ ۲۳ ماژول)، تمدید، ابطال، انتقال به دستگاه جدید، فعال‌سازی آفلاین (تولید فایل .lic)، لاگ اعتبارسنجی، تنظیمات با نمایش کلید عمومی PEM/XML، حسابرسی مدیران، CSRF + قفل ۵ خطا/۳۰ دقیقه
  - API عمومی `api.php`: activate (اتصال اثر انگشت در اولین فعال‌سازی) + heartbeat + ping با Rate-Limit 60/10min و قرارداد JSON امضاشده
  - کار روزانه پاک‌سازی با کلید رمزی (cron.php) + بسته ZIP آماده آپلود در `download/`
- **اتصال واقعی کلاینت .NET**: `ActivateOnlineAsync` با HttpClient typed واقعی + تأیید امضای RSA (کلید قالب XML یا PEM) + `HeartbeatAsync` روزانه Hangfire (ساعت ۳ بامداد) با مهلت آفلاین قابل تنظیم (پیش‌فرض ۳۰ روز) + تنظیمات جدید `ClinicName`/`OfflineGraceDays`
- ۴ تست سازگاری امضای بین-پلتفرمی (openssl_sign ↔ RSA.VerifyData) شامل لایسنس دائمی با انقضای تهی و رد payload دستکاری‌شده — ۵۴/۵۴ تست سبز

#### Fixed
- باگ میدلور لایسنس: نگاشت مسیر `api/v1/lab` از ماژول "Laboratory" به "Lab" اصلاح شد (قبلاً ماژول آزمایشگاه همیشه رد می‌شد)

## [1.1.0-alpha] — 2026-10-05 (چرخه دوم توسعه — ۱۲ گام در ۴ فاز)

### 🖥️ Added — تکمیل کلاینت دسکتاپ (فاز A+B)
- **Lookups API**: چهار endpoint مرجع (بیمه‌ها/تشخیص‌های ICD-10/داروها/آزمایش‌ها) + جزئیات درخواست آزمایش + فهرست صفحه‌بندی‌شده نسخه‌ها
- **۹ ماژول WPF** با الگوی MVVM مستقر: نوبت‌دهی (فیلتر + چهار عملیات وضعیت)، پذیرش (صف + زمان انتظار)، ویزیت (ویرایشگر SOAP + جستجوی ICD-10)، نسخه (ویرایشگر اقلام دارویی)، مالی (فاکتور با اقلام + پرداخت)، آزمایشگاه (انتخاب تست + ثبت نتیجه قلم‌به‌قلم)، داروخانه (موجودی + ورود بچ + تحویل FEFO)، گزارش‌ها (نمودار ستونی WPF)، تنظیمات (چهار زبانه اداری)
- بیمه‌های فرم بیمار از داده مرجع بارگذاری شد (بستن بدهی گام ۱۲ چرخه اول)

### 🔑 Added — پنل لایسنس Blazor (فاز C)
- چیدمان RTL فارسی + داشبورد آماری + صدور/تمدید/ابطال لایسنس با کلید تصادفی و امضای RSA اختیاری + فعال‌سازی‌ها + لاگ اعتبارسنجی

### 🔗 Added — یکپارچه‌سازی و پایداری (فاز D)
- **استعلام برخط بیمه**: HTTP با سه تلاش + کش ۳۰ دقیقه + سقوط برون‌خط + ApiCallLog
- **مهاجرت AutoMapper → Mapperly 3.6.0**: هشدار امنیتی GHSA-rvv3-g6hj-g44x بسته شد؛ سرکوب‌های ممیزی NuGet حذف شدند
- **تست جریان کامل بیمار** (ورود→بیمار→نوبت→پذیرش→ویزیت→نسخه→فاکتور) روی EF InMemory

### 🔧 Fixed
- باگ تولیدی ثبت نوبت: cast ریپازیتوری عمومی به IAppointmentRepository هرگز معتبر نبود
- پروژه Infrastructure.IntegrationTasks از گام ۲۹/۳۰ کامپایل نمی‌شد (پکیج EF InMemory مفقود + ارجاع فضای‌نام) — تعمیر کامل
- تشخیص خودکار InMemory در سازنده DbContext (بدون ساخت زودهنگام مدل)

### 🧪 Tests
- ۴۳ → **۵۰ تست سبز** (+۷ شامل جریان سرتاسری)

### 🏁 Added — 2026-10-05 (فاز ۹: گام‌های ۲۹-۳۱ — نهایی‌سازی)
- **Security**: AuditLogMiddleware با پاک‌سازی خودکار فیلدهای حساس (password/token) از لاگ‌ها، سرآیندهای امنیتی استاندارد، فشرده‌سازی پاسخ، داشبورد Hangfire فقط ادمین
- **Tests**: ۴۳+ تست سبز — Money/NationalCode/PhoneNumber، گرداننده ثبت بیمار با EF InMemory و AutoMapper واقعی، اعتبارسنج‌های فارسی، ۶ تست یکپارچگی API
- **Docs**: README جامع (راه‌اندازی/پیکربندی/وضعیت ماژول‌ها)، راهنمای استقرار سرور، چک‌لیست تست دستی WPF
- **CI**: GitHub Actions — ساخت + تست خودکار روی همه شاخه‌ها


### 📡 Added — 2026-10-05 (فاز ۷: گام‌های ۲۳-۲۵ — ارتباطات و لایسنس)
- **SMS**: اینترفیس دوگانه سرویس/پنل + کارخانه زنجیره جایگزینی + پنل کاوه‌نگار (REST کامل) + سرویس قالب با جایگزینی متغیر + پردازشگر صف Hangfire با سه تلاش + ثبت کامل پیامک‌ها
- **دولتی**: سپاس ثبت ویزیت با لاگ تراکنش و ApiCallLog؛ نسخه الکترونیک با payload اقلام IRC و کد رهگیری
- **لایسنس**: اعتبارسنجی سه‌مرحله‌ای (انگشت/انقضا/امضا RSA-2048)، فعال‌سازی برخط و برون‌خط، میدلور کنترل ماژول با کدهای استاندارد ۴۰۳، کنترلر وضعیت
- **Hangfire**: سرور + داشبورد /jobs + شغل تکراری صف پیامک — پشت پرچم Jobs:Enabled


### 💰 Added — 2026-10-05 (فاز ۵: گام‌های ۱۷-۱۹ — مالی و حسابداری)
- **Invoices**: صدور با محاسبه سهم بیمه بر اساس قرارداد، تخفیف اقلام، مالیات، سهم پزشک + سند حسابداری خودکار (بدهکار مطالبات بیمار / بستانکار درآمد درمانی)
- **Payments**: ثبت دریافتی با کنترل پرداخت اضافی، گذار وضعیت فاکتور به تسویه، رویداد تسویه، سند دریافتی خودکار
- **Accounting**: درخت سرفصل چهارسطحی، سند دستی با الزام توازن (قانون طلایی)، ثبت فقط روی حساب تفصیلی
- **Insurance**: مطالبه دوره‌ای با جمع خودکار سهم بیمه‌ای فاکتورها + ثبت استعلام


### 🏥 Added — 2026-10-05 (فاز ۴: گام‌های ۱۳-۱۶ — زنجیره درمانی)
- **Appointments**: رزرو با کنترل ظرفیت شیفت، غیبت پزشک، تداخل زمان بیمار + رویداد پیامک + شماره صف
- **Receptions**: پذیرش از نوبت یا حضوری، تغییر خودکار وضعیت نوبت، جریان صف تا ترخیص با محاسبه زمان انتظار
- **Visits**: ذخیره تدریجی الگوی SOAP، تکمیل با تشخیص ICD-10 الزامی، محاسبه مدت، به‌روزرسانی آخرین مراجعه
- **Prescriptions**: رد صدور در تداخل ممنوع مصرف، اعتبار مخدر ۳ روزه، رویداد آماده ارسال نسخه الکترونیک


### 🖥️ Added — 2026-10-05 (فاز ۳: گام‌های ۱۰ تا ۱۲ — کلاینت دسکتاپ)
- سرویس‌های کلاینت: `ApiClient` (پیوست خودکار توکن + تمدید خودکار روی ۴۰۱ + یک تلاش مجدد)، `TokenManager`، `NavigationService`، `ThemeService`
- پوسته اصلی: ناوبری کنار با ۱۱ ماژول، هدر با کاربر و تاریخ شمسی و کلید تم، DataTemplates مدل‌نما
- پنجره ورود: کارت Material + Enter برای ورود + پیام خطای فارسی
- داشبورد: چهار کارت آمار امروز + وضعیت اتصال + `DashboardController` با پرس‌وجوهای روزانه
- ماژول بیماران: جدول داده + جستجو + صفحه‌بندی سروری + فرم ثبت/ویرایش با اعتبارسنجی


### 🎯 Added — 2026-10-05 (فاز ۲: گام‌های ۶ تا ۹ — ماژول‌های هسته‌ای)
- **Auth**: چهار فرمان CQRS ورود/تمدید/خروج/تغییر رمز با اعتبارسنجی فارسی و محدودیت ۵ درخواست در دقیقه
- **Patients**: CRUD کامل + جستجوی صفحه‌بندی‌شده + اعتبارسنجی رقم کنترلی کد ملی + یکتایی غیرهمگام + سکانس MRN + رویداد دامنه پیامک خوش‌آمدگویی
- **Doctors**: CRUD + فیلتر تخصص + جستجو با نام پرسنل
- **API**: ExceptionHandlingMiddleware فارسی، HasPermission پویا (PERMISSION:x)، RateLimiter سراسری ۱۰۰/دقیقه و ورود ۵/دقیقه، Swagger با پشتیبانی JWT، پاسخ استاندارد برای خطاهای ModelState، سه کنترلر /api/v1
- **تست‌ها**: ۲۸ تست سبز (۱۷ تقویم + ۶ API + ۲ زیرساخت + ۲ دامنه + ۱ سلامت)


### ⚙️ Added — 2026-10-05 (گام ۵: Application.Common)
- چهار Behavior خط لوله MediatR: Logging، Performance (آستانه ۵۰۰ms)، Validation، Transaction (فقط برای Commandها)
- `ApiResponse<T>` و `PagedResponse<T>` و `PagedRequest` — فرمت استاندارد همه endpointها
- `PersianDateHelper` — تبدیل دوطرفه جلالی/میلادی بدون وابستگی + سن + ارقام فارسی (۱۷ تست سبز)
- `AppValidationException` با دیکشنری فیلد→پیام فارسی
- اینترفیس‌های `ITransactionManager`، `IEncryptionService`، `IDateTimeService`
- پیاده‌سازی زیرساخت: `TransactionManager`، `EncryptionService` (AES-256-GCM)، `DateTimeService` (منطقه ایران)


### 🔐 Added — 2026-10-05 (گام ۴: Infrastructure.Identity)
- `IdentityService`: احراز هویت با نام کاربری/ایمیل، قفل حساب ۳۰ دقیقه بعد از ۵ تلاش ناموفق، ثبت نشست کاربر
- چرخش توکن تازه‌سازی (Rotation) هنگام تمدید + ابطال هنگام خروج
- تغییر رمز عبور با سیاست امنیتی و جلوگیری از تکرار ۵ رمز اخیر
- `JwtTokenGenerator`: توکن دسترسی با ادعاهای نقش و دسترسی + توکن تازه‌سازی ۲۵۶ بیتی تصادفی
- `PasswordHasher`: BCrypt ضریب ۱۲ + سیاست حداقل ۸ کاراکتر با حرف و رقم
- `IdentitySetup`: تنظیم JWT Bearer با پاسخ فارسی توکن منقضی
- بخش Jwt در appsettings.json


### 🗄️ Added — 2026-10-05 (گام ۳: Infrastructure.Persistence)
- `ClinicProDbContext` با ۸۷ DbSet، فیلتر پرس‌وجوی سراسری حذف نرم، رفتار NoAction همه کلیدهای خارجی، RowVersion همزمانی، ۱۵ سکانس SQL Server
- ۸۷ فایل Configuration با کامنت فارسی جدول و ستون + ایندکس‌های یکتای فیلترشده + دقت اعشار + مقادیر پیش‌فرض سکانسی (MRN/APT/RCP/VST/RX/INV/PAY/JE)
- Migration اولیه (۵۰۱۷ خط — ۸۷ جدول) با dotnet-ef 8.0.11
- `Repository<T>` عمومی + ریپازیتوری‌های تخصصی بیمار/پزشک/نوبت/لایسنس
- `UnitOfWork` با کش ریپازیتوری و انتشار رویدادهای دامنه از طریق MediatR
- اینترسپتورهای `AuditableEntityInterceptor` و `SoftDeleteInterceptor`
- ۶ بذر داده اولیه: نقش‌ها (۸)، دسترسی‌ها (۱۵۰=۲۵ ماژول×۶ عملیات)، ادمین (admin/Admin@123)، تخصص‌ها (۱۴)، ICD-10 (۲۳)، بیمه‌ها (۸)، ماژول‌ها (۲۳)، قالب پیامک (۵)، سرفصل‌های حسابداری
- `CurrentUserService` برای HttpContext و `ICurrentUserService` در Application


### 🧬 Added — 2026-10-05 (گام ۲: Domain Layer)
- کلاس‌های پایه: `BaseEntity` (کلید + رویدادهای دامنه)، `AuditableEntity` (حسابرسی + حذف نرم + BranchId + RowVersion)، `Result<T>`، `Error`، `ValueObject`
- ۸۰ موجودیت دامنه در ۱۵ پوشه ماژولار (People/Scheduling/Clinical/Billing/Accounting/Paraclinical/Pharmacy/Personnel/Security/Communication/Infrastructure/Licensing/Integration/Reporting)
- ۸۹ شمارشی (enum) با مقادیر مستند در مستندات دیتابیس
- اشیاء ارزشی: `NationalCode` (اعتبارسنجی رقم کنترلی)، `PhoneNumber`، `Money`، `Address`
- استثناهای دامنه: `DomainException`، `BusinessRuleException`، `NotFoundException`، `ConflictException`، `UnbalancedJournalEntryException` (قانون طلایی حسابداری دوبل)
- اینترفیس‌های ریپازیتوری: `IRepository<T>`، `IUnitOfWork`، `IPatientRepository`، `IDoctorRepository`، `IAppointmentRepository`، `ILicenseRepository`
- ۱۲ رویداد دامنه (ثبت بیمار، رزرو/لغو نوبت، تکمیل ویزیت، صدور نسخه، فاکتور، پرداخت، هشدار موجودی، لایسنس، نتیجه آزمایش)
- مولد پایتونی `scripts/step02_*.py` برای بازتولید یکسان فایل‌ها


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

## [1.0.0-alpha] — 2026-10-05

اولین نسخه کامل معماری — هر ۳۱ گام پیاده‌سازی شد:
- هسته: Solution یازده‌پروژه‌ای + Domain کامل (۸۷ موجودیت) + Persistence (Migration اولیه) + Identity (JWT/BCrypt)
- درمانی: نوبت ← پذیرش ← ویزیت SOAP ← نسخه با تداخل‌سنجی
- مالی: فاکتور/پرداخت با سند دوبل خودکار + حسابداری چهارسطحی + بیمه
- پاراکلینیک: آزمایشگاه/تصویربرداری/دندان/چشم/فیزیوتراپی + داروخانه FEFO
- یکپارچه‌سازی: پیامک (کاوه‌نگار + زنجیره جایگزینی) + سپاس + نسخه الکترونیک + لایسنس RSA
- کلاینت: WPF (ورود/داشبورد/بیماران) + API کامل ۲۰+ کنترلر
