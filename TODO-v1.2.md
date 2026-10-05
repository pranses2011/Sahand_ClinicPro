# 📋 لیست کارهای چرخه سوم توسعه — Sahand_ClinicPro v1.2

> فایل مدیریت چرخه سوم — بر اساس ۵ فایل پرامپت UI (فرم‌های ۱ تا ۴۵ + Seed Data + جزئیات فنی) و درخواست ویژه سرور لایسنس cPanel
> پیش‌نیاز: تکمیل چرخه اول (TODO.md — v1.0.0-alpha) و چرخه دوم (TODO-v1.1.md — v1.1.0-alpha)
> منبع مشخصات: `docs/prompts/prompt-ui-part1..5.html` (خوانده‌شده و استخراج‌شده کامل)

## 🎯 پیشرفت کلی: 57% (16/28 گام)

---

## 🗂️ خلاصه منابع مطالعه‌شده (۵ فایل پرامپت UI)

| فایل | فرم‌ها | محتوای کلیدی |
|------|-------|--------------|
| prompt-ui-part1 | ۱-۷ | قوانین کلی فرم‌ها (TopBar/Toolbar/DataGrid/Pagination/StatusBar)، Login، Dashboard، PatientList/Form/Detail، DoctorList/Form |
| prompt-ui-part2 | ۸-۱۳ | AppointmentBook (PersianCalendar + Slot)، AppointmentList، Reception، VisitForm (SOAP + 8 تب)، Prescription (تداخل دارویی)، VisitQueue |
| prompt-ui-part3 | ۱۴-۲۳ | Invoice، CashRegister، Accounting (۵ تب)، Lab، Pharmacy (۵ تب)، Insurance (۴ تب)، Dental (چارت FDI)، Eye، Physio، Imaging |
| prompt-ui-part4 | ۲۴-۳۴ | Users (Strength Meter)، RolePermission (ماتریس)، SMS (۵ تب)، ReportBrowser (۴۰+ گزارش)، Settings، Backup، AuditLog، License، Employee، LicensePortal، Survey |
| prompt-ui-part5 | ۳۵-۴۵ | Branches، Notifications (SignalR)، Sepas (۵ تب)، Tariff، EPrescription، DICOM، PrintTemplate، DataSync، انتخاب شعبه، **Seed Data ۱۲گانه**، اینترسپتورها |

---

## 🌐 فاز A: سرور لایسنس PHP برای cPanel — درخواست ویژه کاربر (شاخه feature/phase-13-license-php)

> درخواست صریح کاربر: نسخه بهینه‌شده برای cPanel + MySQL **بدون هیچ پیش‌نیاز یا افزودنی** — قابل نصب روی هاست اشتراکی
> یعنی: PHP خالص (بدون Composer/فریمورک/Node)، فقط افزونه‌های استاندارد هاست (pdo_mysql، openssl، json، mbstring)

- [x] گام L1: هسته سرور لایسنس PHP ✅ 2026-10-05 — install.php سه‌مرحله‌ای + inc/functions.php (امضای RSA-2048 سازگار با دات‌نت + شمسی + امنیت) + schema.sql هفت‌جدولی — نصب‌کننده وب (`install.php`: بررسی پیش‌نیازها + ساخت جداول MySQL + تولید جفت‌کلید RSA-2048 + قفل نصب) + پیکربندی (`inc/config.php` تولیدی + محافظت `data/` با htaccess)
- [x] گام L2: پنل مدیریت PHP ✅ 2026-10-05 — داشبورد آماری + صدور با کاتالوگ ۲۳ ماژول + تمدید/ابطال/انتقال دستگاه + فعال‌سازی آفلاین (.lic) + لاگ + تنظیمات PEM/XML + CSRF و قفل ۵ خطا — ورود با قفل ۳۰ دقیقه‌ای + CSRF + داشبورد آماری + صدور (ویزارد محدودیت‌ها/ماژول‌ها) + تمدید + ابطال + فعال‌سازی آفلاین (تولید فایل .lic) + لاگ اعتبارسنجی + تنظیمات (نمایش کلید عمومی PEM و XML) — رابط فارسی RTL
- [x] گام L3: API عمومی `api.php` ✅ 2026-10-05 — activate (اتصال اثر انگشت در اولین فعال‌سازی) + heartbeat + ping + Rate-Limit 60/10min — `activate` (اتصال اثر انگشت سخت‌افزار در اولین فعال‌سازی) + `heartbeat` + Rate-Limit + قرارداد JSON سازگار با کلاینت C# (امضای `Key|Expiry|MaxUsers` با SHA256withRSA/PKCS1)
- [x] گام L4: اتصال واقعی کلاینت C# ✅ 2026-10-05 — LicenseService با HttpClient typed + تأیید امضا XML/PEM + تپ قلب روزانه Hangfire با مهلت آفلاین ۳۰روزه + رفع باگ ماژول Laboratory→Lab + ۴ تست سازگاری امضا سبز — `LicenseService.ActivateOnlineAsync` واقعی (فراخوانی HTTP + تحلیل پاسخ + ذخیره محدودیت‌ها/ماژول‌ها) + `ActivateOfflineAsync` با تأیید امضا از فایل .lic + پشتیبانی کلید عمومی PEM و XML + تنظیم `License:PortalUrl`
- [x] گام L5: مستندسازی و بسته‌بندی ✅ 2026-10-05 — README-FA.md راهنمای گام‌به‌گام cPanel + ZIP کامل با htaccess در download/Sahand-LicenseServer-cPanel-v1.0.zip — راهنمای گام‌به‌گام نصب روی cPanel (ساخت DB → آپلود → install.php → حذف فایل نصب) + نکات امنیتی هاست اشتراکی + فایل ZIP قابل آپلود در `download/`

## 🧱 فاز B: زیرساخت مشترک UI دسکتاپ (شاخه feature/phase-14-ui-shell)

> مطابق «قوانین کلی ساخت فرم‌های WPF» در prompt-ui-part1 — پایه همه فرم‌های بعدی

- [x] گام U0: پوسته و کنترل‌های مشترک ✅ 2026-10-05 — TopBar کامل + StatusBar + ۶ کنترل سفارشی (PersianCalendar/PersianDatePicker/TagInput/PaginationBar/StatCard/EmptyState) + SnackbarService + ۵ مبدل مشترک
  - ارتقای MainWindow: TopBar کامل (Breadcrumb + ساعت شمسی لحظه‌ای + زنگ اعلان با Badge + تغییر تم + نام/آواتار کاربر + انتخابگر شعبه وقتی ۲+ شعبه) + StatusBar پایین (وضعیت اتصال/لایسنس/کاربر/تاریخ)
  - کنترل‌های سفارشی: `PersianCalendarControl` (UniformGrid ۷×۷ + MarkedDates + DisabledDates + جمعه قرمز)، `PersianDatePicker` (متن شمسی ↔ تاریخ میلادی)، `TagInputControl` (تگ با رنگ + Enter افزودن + × حذف)، `PaginationBar`، `StatCard` (عدد درشت + نوار رنگی + مقایسه)، `StatusBadge`، `EmptyState`
  - سرویس‌ها: `SnackbarService` (سبز/قرمز پایین صفحه)، `ShortcutService` (F3/F5/Ctrl+F/Ctrl+S/Ctrl+Enter)
  - رفتارهای عمومی: Debounce جستجو ۳۰۰ms + ProgressRing + تأیید حذف با DialogHost

## 🖥️ فاز C: بازسازی UI ماژول‌های اصلی طبق مشخصات دقیق (شاخه feature/phase-15-ui-core)

- [x] گام U1: LoginView ✅ 2026-10-05 — ۵۰۰×۶۰۰ بدون حاشیه + گرادیان آبی-سبز + دکمه چشم نمایش رمز + RememberMe (ذخیره فقط نام کاربری در JSON محلی) + دکمه فعال‌سازی لایسنس + کشیدن پنجره بدون نوار عنوان
- [x] گام U1: DashboardView ✅ 2026-10-05 — نوار خوش‌آمد + ۴ کارت اصلی + ۳ کارت فرعی با StatCard + نمودار میله‌ای درآمد ۷ روز + نوبت‌های پیش‌رو با Badge وضعیت + جدول فعالیت‌های اخیر + Timer ۶۰ ثانیه + ۴ endpoint جدید داشبورد (revenue-chart / appointments-today / recent-activities / alerts)
- [x] گام U2: PatientListView دقیق ✅ 2026-10-05 (کامیت c188a9b) — (۹ ستون: انتخاب/پرونده/نام/کدملی/موبایل/بیمه Badge/سن/آخرین مراجعه شمسی/عملیات با مجوز + ContextMenu + Ctrl+A + خروجی Excel) + PatientFormView (۵ تب: اصلی/بیمه/سوابق/تماس/اسناد + بررسی خودکار کدملی ۱۰رقمی + آبشار استان↔شهر + TagInput آلرژی/مزمن/دارو + دکمه‌های ذخیره/ذخیره‌موقت/ذخیره‌ونوبت) + PatientDetailView (کارت سربرگ + هشدار آلرژی برجسته + ۶ تب سوابق + اقدامات سریع)
- [x] گام U3: DoctorListView ✅ 2026-10-05 — ۶ endpoint جدید (schedule/absences/performance) + lookups تخصص/اتاق/پرسنل + ۵ مبدل جدید + فرم ۵ تب کامل (ستون‌های تخصص/حق‌ویزیت/مدت/درصد سهم/امتیاز) + DoctorFormView (۵ تب: پایه/تنظیم ویزیت/امضا و مهر/برنامه کاری هفتگی با جدول شیفت + غیبت‌ها با بررسی نوبت‌های رزروشده)
- [x] گام U4: AppointmentBookView ✅ 2026-10-05 — تقویم شمسی با نشانگر + اسلات سبز/خط‌خورده + خلاصه و ثبت + فهرست ۱۲ ستونی با فیلتر ۵گانه + لغو دسته‌ای + SMS یادآوری (۵ endpoint جدید) (جستجوی سریع بیمار AutoComplete + PersianCalendar با نشانگر تعداد + پنل Slot های سبز/خط‌خورده + خلاصه انتخاب + نوع/اولویت/یادداشت + ثبت) + AppointmentListView (فیلتر تاریخ/پزشک/وضعیت/نوع/منبع + Badge های ۷ وضعیت + SMS دسته‌ای + لغو دسته‌ای)
- [x] گام U5: ReceptionView ✅ 2026-10-05 — ۴ کارت آماری + جستجوی هوشمند (کدملی/APT/موبایل) + استعلام بیمه با Timeout و سقوط آزاد + محاسبه ۳سطری هزینه + دو دکمه زنجیره‌ای پذیرش‌وفاکتور/پذیرش‌وپرداخت + صف رنگ‌بندی‌شده (استعلام نتیجه کامل + queue-stats) (کارت‌های آماری + جستجوی هوشمند کدملی/APT/موبایل + استعلام بیمه با Timeout + محاسبه خودکار هزینه + دکمه‌های «پذیرش و فاکتور»/«پذیرش و پرداخت نقدی» + صف زنده با رنگ‌بندی)
- [x] گام U6: VisitFormView ✅ 2026-10-05 — سایدبار ۲۸۰ (هویت/آلرژی قرمز/مزمن نارنجی/دارو آبی/سوابق) + ۸ تب (SOAP کامل ۶ فیلد/حیاتی ۸ کارت با Badge و هشدار/معاینه ۴ ناحیه/تشخیص ICD-10 AutoComplete + ثانویه + پیگیری/نسخه/آزمایش/تصویر/فایل) + AutoSave ۶۰s + Stopwatch + Ctrl+S/Ctrl+Enter + صف زنده ۳۰s + فراخوان Space (queue + call-next + فیلدهای ۷گانه SOAP + علائم حیاتی upsert) (سایدبار ۲۸۰px: عکس + اطلاعات + آلرژی قرمز + سوابق + ۸ تب SOAP/حیاتی/معاینه/تشخیص/نسخه/آزمایش/تصویر/فایل + AutoSave ۶۰s + Stopwatch + «بیمار بعدی») + VisitQueueView (بیمار فعلی + صف با زمان انتظار زنده + فراخوان + میانگین ویزیت)
- [x] گام U7: PrescriptionView ✅ 2026-10-05 — جستجوی داروی غنی (نام‌فا/ان/فرم/IRC/کشور) با Debounce + کارت اقلام با دوز/تعداد/مدت/مسیر/Quantity خودکار + پنل تداخل ۴سطحی خودکار (خفیف→ممنوع + آلرژی BLOCK قرمز تیره) + ارسال الکترونیک با کد رهگیری برجسته + کپی/SMS + تمپلیت ذخیره/بارگذاری (۶ endpoint جدید) (نوار تأیید بیمه + جستجوی دارو با Debounce + کارت اقلام با دوز/تعداد/مدت/مسیر/Quantity خودکار + پنل تداخل ۴سطحی (سبز/زرد/قرمز/آلرژی BLOCK) + ارسال الکترونیک با کد رهگیری + تمپلیت‌ها)

## 💰 فاز D: مالی، حسابداری و پاراکلینیک (شاخه feature/phase-16-ui-financial)

- [x] گام U8: InvoiceView ✅ 2026-10-05 — ۵ کارت صندوق + اقلام با کد K از تعرفه + تخفیف مبلغ↔درصد خودکار + سهم بیمه/مالیات/قابل پرداخت برجسته + ۴ روش پرداخت + زنجیره فاکتور‌وپرداخت + تراکنش‌های امروز + کاتالوگ خدمات endpoint (سربرگ صندوق + اقلام با کد K + پنل خلاصه با تخفیف مبلغ↔درصد + روش پرداخت ۵گانه + پرداخت ترکیبی + ثبت/چاپ) + CashRegisterView (وضعیت + بستن با شمارش وجه + تراکنش‌های روز)
- [ ] گام U9: AccountingView (۵ تب: دفتر روزنامه + فرم سند با AutoComplete حساب و قانون توازن + دفتر کل + درخت سرفصل ۴سطحی + تراز + سود و زیان ساختاریافته)
- [ ] گام U10: LabView (۴ تب + فرم درخواست با انتخاب تست و جمع هزینه + ثبت نتیجه با Flag خودکار H/L/HH/LL وابسته به جنسیت) + PharmacyView (۵ تب: تحویل با Badge موجودی + انبار با رنگ‌بندی + ورود کالا + سفارش + انبارگردانی)
- [ ] گام U11: InsuranceView (۴ تب: شرکت‌ها + تعرفه‌ها + مطالبات + استعلامات) + TariffManagementView (ویرایش Inline + کپی از سال قبل با درصد + افزایش سراسری)
- [ ] گام U12: DentalView (چارت ۳۲ دندانی FDI با رنگ + سطح M/D/O/B/L + طرح درمان) + EyeExamView (OD/OS با Sphere/Cyl/Axis/Add + IOP با هشدار گلوکوم) + PhysiotherapyView (جلسات + تکنیک‌ها + نمره درد) + ImagingView (درخواست + گزارش + آپلود)

## ⚙️ فاز E: مدیریت، یکپارچه‌سازی و تکمیلی (شاخه feature/phase-17-ui-admin)

- [ ] گام U13: UserListView + فرم کاربر (Strength Meter ۵سطحی + نقش‌ها + دسترسی سفارشی) + RolePermissionView (لیست نقش + ماتریس دسترسی ۲۴ گروه با انتخاب همه)
- [ ] گام U14: SmsView (۵ تب: ارسال تکی/دسته‌ای + تاریخچه + تمپلیت با متغیرها + پنل‌ها با تست اتصال + آمار) + NotificationListView (مرکز اعلان ۵ نوع + Toast + SignalR)
- [ ] گام U15: SettingsView کامل (اطلاعات درمانگاه + ماژول‌ها با وضعیت لایسنس + سامانه‌های دولتی + چاپگر + تم + امنیت) + BackupView (۳سطحی + بازیابی) + AuditLogView (فیلترها + Old/New JSON)
- [ ] گام U16: ReportBrowserView (سایدبار دسته‌ها + کاتالوگ ۴۰+ گزارش + پارامترهای داینامیک + خروجی)
- [ ] گام U17: BranchListView + چند-مستأجری (هدر X-Branch-Id + ICurrentUserService.BranchId + فیلتر سراسری EF + انتخابگر TopBar + نمایشگر وضعیت لایسنس فرم ۳۱)
- [ ] گام U18: SepasIntegrationView (۵ تب: وضعیت + تراکنش‌ها + دریافتی‌ها + تنظیمات + لاگ API با Request/Response) + EPrescriptionView (آمار + پایش + ارسال مجدد + Hangfire هر ۵ دقیقه)
- [ ] گام U19: فرم‌های تکمیلی — PrintTemplateView (۹ قالب + تنظیم چاپگر) + SurveyView + DataSyncView + LicenseView وضعیت (ProgressBar های محدودیت)

## 🌱 فاز F: تکمیل Seed Data طبق بخش ۴۴ پرامپت (شاخه feature/phase-18-seed)

- [x] گام S1: LocationSeeder ✅ 2026-10-05 — ۳۱ استان + ۶۳۵ شهر (داده استاتیک برای آبشار فرم بیمار) + ICD10Seeder با ۱۱۵۳ کد واقعی پرکاربرد و توضیح فارسی (۳۱ استان + شهرها از JSON) + ICD10Seeder (۱۰۰۰+ کد پرکاربرد فارسی)
- [x] گام S2: MedicationSeeder ✅ 2026-10-05 — ۵۰۳۴ قلم (مولکول×فرم×دوز×۶ سازنده ایرانی واقعی) + LabTestSeeder با ۲۲۹ تست و محدوده طبیعی وابسته به جنسیت (کاتالوگ دارویی ایران — IRC/ژنریک/برند/فرم/دوز/سازنده) + LabTestSeeder (۲۰۰+ تست با محدوده طبیعی وابسته به جنسیت)
- [x] گام S3: DrugInteractionSeeder ✅ 2026-10-05 — ۵۲۳ جفت تداخل واقعی با شدت چهارسطحی و مکانیزم و توصیه + نگاشت کلاس‌های دارویی (NSAID/ACEI/Statin/...) + ServiceTariffSeeder با ۵۷ خدمت پایه + تعرفه سال جاری (۵۰۰+ تداخل با شدت و مکانیزم) + ServiceTariffSeeder (تعرفه پایه خدمات) + SpecialtySeeder درختی کامل (پزشکی + دندان)

---

## 📌 قواعد اجرا
- کامیت‌ها: `<emoji> [Step <کد>]: <توضیح>` — ادغام هر فاز به main با کامیت 🔀 و برچسب پایان چرخه v1.2.0-alpha
- هر فاز: شاخه feature جداگانه → build سبز + تست سبز → merge به main → push
- پس از هر گام: به‌روزرسانی همین فایل + PROGRESS.md + CHANGELOG.md
- همه View ها: MVVM + Material Design + RTL + فونت Vazirmatn + کامنت فارسی
- API های لازم برای فرم‌های جدید (Notifications/Tariffs/EPrescriptions/Branches) در همان گام اضافه می‌شوند
- سرور لایسنس PHP: `src/LicenseServer.Php/` در ریپو — بدون هیچ وابستگی خارجی؛ خروجی ZIP در `download/`
