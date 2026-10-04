# 📋 لیست کارهای Sahand_ClinicPro

> فایل مدیریت پروژه — چک‌لیست ۳۱ گام پیاده‌سازی در ۹ فاز
> منبع: `docs/prompts/prompt-part3.html` (بخش ۸: ترتیب پیاده‌سازی)

## 🎯 پیشرفت کلی: 29% (9/31 گام)

---

## 📦 فاز ۱: راه‌اندازی و زیرساخت
- [x] گام ۱: Solution Setup (ایجاد پروژه‌ها + NuGet Packages + DI Configuration) ✅ 2026-10-05
- [x] گام ۲: Domain Layer (BaseEntity + ۸۰ Entity + ۸۹ Enum + Value Objects + Interfaces) ✅ 2026-10-05
- [x] گام ۳: Infrastructure.Persistence — DbContext + ۸۷ Configuration + Migration اولیه (۵۰۱۷ خط) + ۶ Seeder + Repository/UnitOfWork/Interceptors ✅ 2026-10-05
- [x] گام ۴: Infrastructure.Identity — JWT + BCrypt + Refresh Token چرخشی + قفل ۵ تلاشی + RBAC ✅ 2026-10-05

## 🎯 فاز ۲: ماژول‌های هسته‌ای
- [x] گام ۵: Application.Common — ۴ Behavior + ApiResponse/PagedResponse + PersianDateHelper (تست‌شده با ۱۷ تست) + IEncryption/IDateTime/ITransaction ✅ 2026-10-05
- [x] گام ۶: Auth Module (Login + Refresh + Logout + ChangePassword) ✅ 2026-10-05
- [x] گام ۷: Patients Module (Create/Update/Delete/GetAll/Search + کد ملی اعتبارسنجی) ✅ 2026-10-05
- [x] گام ۸: Doctors Module (CRUD + فیلتر تخصص + جستجو) ✅ 2026-10-05
- [x] گام ۹: API Layer (میدلور خطا + HasPermission + RateLimit 100/5 + Swagger JWT + سه کنترلر + ۶ تست) ✅ 2026-10-05

## 🖥️ فاز ۳: Desktop Client
- [ ] گام ۱۰: WPF Setup (App.xaml + MainWindow + Theme + Fonts + Navigation)
- [ ] گام ۱۱: WPF Login + Dashboard
- [ ] گام ۱۲: WPF Patient Module (List + Form + Detail)

## 🏥 فاز ۴: ماژول‌های درمانی
- [ ] گام ۱۳: Appointments Module (Application + API + WPF)
- [ ] گام ۱۴: Reception Module (Application + API + WPF)
- [ ] گام ۱۵: Visit Module (Application + API + WPF)
- [ ] گام ۱۶: Prescription Module (Application + API + WPF)

## 💰 فاز ۵: مالی و حسابداری
- [ ] گام ۱۷: Invoice + Payment Module (Application + API + WPF)
- [ ] گام ۱۸: Accounting Module (Application + API + WPF)
- [ ] گام ۱۹: Insurance Module (Application + API + WPF)

## 🔬 فاز ۶: پاراکلینیک و داروخانه
- [ ] گام ۲۰: Lab Module (Application + API + WPF)
- [ ] گام ۲۱: Pharmacy Module (Application + API + WPF)
- [ ] گام ۲۲: Paraclinical Modules (Imaging, Dental, Eye, Physio)

## 📱 فاز ۷: ارتباطات و یکپارچه‌سازی
- [ ] گام ۲۳: SMS Module (10 providers + Templates + Queue)
- [ ] گام ۲۴: Government Integration (Sepas, Sib, Nab, Sina, E-Prescription)
- [ ] گام ۲۵: License System (Portal + Client + Activation + Heartbeat)

## 📊 فاز ۸: گزارش و اداری
- [ ] گام ۲۶: Reports Module (Stimulsoft + Dashboard Charts)
- [ ] گام ۲۷: HR Module (Employees + Attendance + Salary)
- [ ] گام ۲۸: Backup + Settings + Audit Log

## 🚀 فاز ۹: نهایی‌سازی
- [ ] گام ۲۹: Security Hardening + Performance Optimization
- [ ] گام ۳۰: Unit Tests + Integration Tests
- [ ] گام ۳۱: Documentation + Deployment Package

---

## ✅ خارج از ۳۱ گام (تکمیل شده):
- [x] فاز صفر: ایجاد ریپازیتوری GitHub
- [x] فاز صفر: استخراج مستندات در docs/
- [x] فاز صفر: ایجاد TODO.md و PROGRESS.md و README.md و CHANGELOG.md

---

## 📝 لاگ تغییرات اخیر:
- **2026-10-05**: گام‌های ۶ تا ۹ کامل شد — فاز ۲ بسته شد: ماژول Auth (۴ فرمان CQRS)، ماژول بیماران (CRUD کامل + جستجوی صفحه‌بندی‌شده + سکانس MRN + رویداد پیامک)، ماژول پزشکان، لایه API (میدلور خطای فارسی، مجوزدهی پویا PERMISSION:x، RateLimiter دو‌سیاستی، Swagger با Bearer، ۶ تست یکپارچگی). ۲۸ تست کل سبز.
- **2026-10-05**: گام ۵ کامل شد — چهار رفتار خط لوله MediatR (لاگ/عملکرد/اعتبارسنجی/تراکنش) با ترتیب صحیح ثبت؛ ApiResponse و PagedResponse و PagedRequest استاندارد؛ PersianDateHelper با الگوریتم جلالی بدون وابستگی (۱۷ تست شامل تاریخ‌های مرجع رسمی و کبیسه و رفت‌وبرگشت ۱۰ ساله)؛ پیاده‌سازی AES-256-GCM و DateTimeService ایران و TransactionManager در زیرساخت.
- **2026-10-05**: گام ۴ کامل شد — IdentityService کامل (ورود با قفل ۳۰ دقیقه‌ای بعد ۵ تلاش، چرخش توکن تازه‌سازی، تغییر رمز با تاریخچه ۵ رمز)، JwtTokenGenerator (HMAC-SHA256 با ادعاهای نقش/دسترسی)، PasswordHasher (BCrypt ضریب ۱۲ + سیاست رمز)، تنظیم JWT Bearer با پیام فارسی. فاز ۱ کامل شد و feature/phase-01-foundation به main ادغام شد.
- **2026-10-05**: گام ۳ کامل شد — ClinicProDbContext با ۸۷ DbSet و فیلتر حذف نرم سراسری و ۱۵ SQL Sequence؛ ۸۷ فایل Configuration تولیدشده با کامنت فارسی جدول/ستون + ایندکس‌های فیلترشده؛ Migration اولیه (۵۰۱۷ خط، ۸۷ جدول)؛ Repository عمومی + ۴ تخصصی؛ UnitOfWork با انتشار رویدادهای دامنه؛ اینترسپتورهای حسابرسی/حذف نرم؛ ۶ بذر (نقش‌ها/دسترسی‌ها/ادمین/داده پایه/سرفصل حسابداری). ابزار dotnet-ef 8.0.11 محلی شد.
- **2026-10-05**: گام ۲ کامل شد — لایه دامنه کامل: ۸ موجودیت پایه (BaseEntity/AuditableEntity/Result/ValueObject)، ۸۰ موجودیت در ۱۵ پوشه ماژولار، ۸۹ شمارشی، ۴ شیء ارزشی (کد ملی/موبایل/پول/آدرس)، ۶ استثنا، ۶ اینترفیس ریپازیتوری، ۱۲ رویداد دامنه. Build سبز + تست‌ها سبز.
- **2026-10-05**: گام ۱ کامل شد — Solution یازده‌پروژه‌ای با نام‌گذاری Sahand.ClinicPro.*، مدیریت مرکزی پکیج‌ها (CPM)، اسکلت DI دو لایه، Program.cs با Serilog/Swagger فارسی، پروژه WPF دست‌نویس + ۶ تست دودی سبز. شاخه: feature/phase-01-foundation
- **2026-10-05**: راه‌اندازی ریپازیتوری، استخراج ۹ فایل مستندات (۶ طراحی + ۳ پرامپت)، ایجاد فایل‌های مدیریتی. آماده شروع گام ۱.

## ⚠️ موارد نیاز به توجه:
- [Resolved 2026-10-05] نام‌گذاری نهایی پروژه‌ها: `Sahand.ClinicPro.*` (تصمیم کاربر) — کلاس‌های داخلی مطابق مستندات باقی می‌مانند (مثل ClinicProDbContext)
- [Resolved 2026-10-05] نسخه پکیج‌ها: پین‌شدن مرکزی در `Directory.Packages.props` (CPM) + پین انتقالی
- [Open] ⚠️ امنیتی: AutoMapper 13.0.1 دارای هشدار GHSA-rvv3-g6hj-g44x (DoS نگاشت گراف عمیق) — رفع فقط در نسخه‌های تجاری 15.1.1+. ریسک پذیرفته‌شده با مستندسازی کامل (Directory.Build.props)؛ بازبینی در گام ۲۹: مهاجرت به Mapperly یا خرید لایسنس
- [Info] قالب WPF در محیط لینوکسی موجود نیست؛ پروژه Desktop دست‌نویس شد و EnableWindowsTargeting در Directory.Build.props فعال است (ساخت در CI لینوکس ممکن است)
- [Info] نسخه‌های رایگان نهایی انتخاب شدند: MediatR 12.5.0 (آخرین Apache-2.0)، FluentValidation 11.12.0، AutoMapper 13.0.1 (آخرین MIT)، FluentAssertions 6.12.2
- [Info] توکن دسترسی GitHub فقط در Local نگهداری شود و هرگز در فایل‌های ریپازیتوری قرار نگیرد.
