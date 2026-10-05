## چرخه سوم (v1.2) — جلسه ۲۰۲۶-۱۰-۰۵ (بخش دوم: فاز E تکمیل شد)

| فاز | گام‌ها | وضعیت |
|------|--------|--------|
| A — سرور لایسنس cPanel | L1-L5 | ✅ کامل |
| B — زیرساخت UI | U0-U2 | ✅ کامل |
| C — ماژول‌های اصلی | U3-U7 | ✅ کامل |
| D — مالی و پاراکلینیک | U8-U12 | ✅ کامل |
| E — اداری و تکمیلی | U13-U19 | ✅ کامل (۷ گام) |
| F — داده مرجع | S1-S3 | ✅ کامل |

- **پیشرفت چرخه سوم: ۲۸/۲۸ گام (۱۰۰٪)** | تگ **v1.2.0-alpha**
- شاخه feature/phase-17-ui-admin → ادغام به main
- وضعیت Solution: ۰ خطا / ۵۴ تست سبز

### خلاصه فاز E (گام‌های U13-U19):
- **U13** — کاربران: فهرست با فیلتر نقش/وضعیت + عملیات ردیف (ویرایش/ریست رمز با تولید رمز قوی و کپی/قفل ۳۰دقیقه/آزادسازی/غیرفعال) + فرم دوتبی با سنجه قدرت رمز ۵سطحی و بررسی یکتایی برخط + نقش‌ها: ماتریس دسترسی ۲۵ ماژول × ۶ عملیات با انتخاب گروهی و سراسری
- **U14** — پیامک ۵ تبی (ارسال تکی/دسته‌ای با مخاطبان هدفمند + تاریخچه با فیلتر و بازارسال + تمپلیت + پنل‌ها با تست اتصال + آمار ۳۰روزه با نمودار) + مرکز اعلان ۵نوع با شمارنده زنگ TopBar
- **U15** — تنظیمات ۶بخشی (درمانگاه/ماژول‌ها با وضعیت لایسنس/سامانه‌های دولتی/چاپگر/تم با پیش‌نمایش زنده/امنیت) + بکاپ (وضعیت + تاریخچه + خودکار ۳سطحی + بازیابی با تأیید) + لاگ عملیات (فیلتر + نشان ۷عملیات + جزئیات Old/New JSON قالب‌بندی‌شده)
- **U16** — گزارش‌ساز: کاتالوگ ۳۶ گزارش در ۶ دسته + ۱۰ گزارش واقعی سمت سرور + پارامتر بازه + کارت‌های خلاصه + جدول داینامیک + خروجی CSV فارسی
- **U17** — چندشعبه‌ای: BranchesController + هدر X-Branch-Id + فیلتر سراسری EF روی همه AuditableEntityها (سازگار با داده بدون شعبه) + انتخابگر TopBar
- **U18** — سپاس ۵ تبی (آمار/تراکنش با بازارسال/دریافتی‌ها/تنظیمات/لاگ API با Request-Response) + نسخه الکترونیک (آمار + پایش + بازارسال)
- **U19** — لایسنس (وضعیت + ماژول‌ها + فعال‌سازی برخط/برون‌خط .lic) + ۹ قالب چاپ + نظرسنجی با میانگین امتیاز + همگام‌سازی داده

---


## چرخه سوم (v1.2) — جلسه ۲۰۲۶-۱۰-۰۵ (بخش اول)

| فاز | گام‌ها | وضعیت |
|------|--------|--------|
| A — سرور لایسنس cPanel | L1-L5 | ✅ کامل + ZIP در download |
| B — زیرساخت UI | U0 | ✅ کامل |
| C — ماژول‌های اصلی | U1-U7 | ✅ کامل (۱۱ کامیت) |
| D — مالی و پاراکلینیک | U8-U12 | ✅ کامل (۵ کامیت) |
| F — داده مرجع | S1-S3 | ✅ کامل |
| E — اداری و تکمیلی | U13-U19 | ⏳ جلسه بعد — زیرساخت چرخه ۲ موجود |

- وضعیت Solution: ۰ خطا / ۰ هشدار / ۵۴ تست سبز
- شاخه main push شده

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


---

## 📊 گزارش چرخه دوم — فاز C+D: پنل لایسنس و یکپارچه‌سازی (2026-10-05)

### گام V9 — پنل لایسنس Blazor واقعی:
- چیدمان RTL فارسی با ستون کنار + ۴ صفحه: داشبورد آماری، مدیریت لایسنس‌ها، درخواست‌های فعال‌سازی، لاگ اعتبارسنجی
- سرویس LicenseAdminService: صدور با کلید تصادفی SCPRO-XXXXX-XXXXX-XXXXX + امضای RSA اختیاری + تمدید + ابطال (حذف نرم)
- کاتالوگ ۲۳ ماژول قابل انتخاب هنگام صدور

### گام V10 — استعلام برخط بیمه (فعال‌سازی هوک گام ۲۴):
- IInsuranceEligibilityService + پیاده‌سازی HTTP به ApiEndpoint بیمه
- سه تلاش با وقفه نمایی + کش ۳۰ دقیقه‌ای + سقوط برون‌خط به قرارداد محلی + ثبت ApiCallLog
- گرداننده ثبت استعلام به سرویس واقعی متصل شد (Response/ApprovalCode/ValidUntil ذخیره می‌شود)

### گام V11 — مهاجرت AutoMapper → Mapperly:
- PatientMapper + DoctorMapper تولیدشده در زمان کامپایل (Riok.Mapperly 3.6.0)
- هشدار امنیتی GHSA-rvv3-g6hj-g44x با حذف کامل AutoMapper بسته شد
- سرکوب‌های NU1903/NuGetAudit از Directory.Build.props حذف شدند
- API نسخه 3.6: `[MapperRequiredMapping(RequiredMappingStrategy.Target)]` روی متدها

### گام V12 — تست یکپارچه‌سازی جریان کامل + نهایی‌سازی:
- تست سرتاسری: ورود ادمین → ثبت بیمار → جستجو → نوبت → پذیرش → ویزیت SOAP → نسخه → فاکتور روی EF InMemory
- **کشف و رفع باگ تولیدی**: cast ریپازیتوری عمومی به IAppointmentRepository در ثبت نوبت (در محیط واقعی هم هرگز کار نمی‌کرد)
- تشخیص خودکار InMemory در سازنده DbContext از options.Extensions (دسترسی به Database.ProviderName مدل را پیش از مقداردهی پرچم می‌ساخت)
- یکسان‌سازی کارخانه‌های تست روی پایه InMemory (کش مدل EF per-type) + غیرفعال‌سازی موازی‌سازی مجموعه‌ها (انجماد Serilog)
- نتیجه نهایی: **۵۰/۵۰ تست سبز** | کل Solution بدون خطا

### وضعیت نهایی چرخه دوم:
- ۱۲/۱۲ گام TODO-v1.1 کامل | برچسب v1.0.0-alpha → v1.1.0-alpha
- کلاینت دسکتاپ: ۱۱ ماژول فعال | پنل لایسنس: کامل | بیمه برخط: متصل

## 💬 یادداشت‌ها:
- معماری: Clean Architecture + CQRS + Repository + Unit of Work
- نام‌گذاری: پروژه‌ها/فضای‌نام‌ها `Sahand.ClinicPro.*` — نام کلاس‌های مستندشده حفظ شد (مثل ClinicProDbContext)
- تداخل نام WPF: در Desktop همیشه `System.Windows.Application` (تداخل با فضای‌نام Application) — در remarks کد توضیح داده شد
- تمام کامنت‌های کد فارسی مفصل؛ نام‌گذاری کد انگلیسی استاندارد؛ فایل‌ها با BOM (سازگاری ویندوز)
- قواعد Commit: `<emoji> [Step N]: <توضیح>` + Push مکرر (حداکثر هر ۳۰ دقیقه)
- برای هر فاز یک Branch جداگانه: `feature/phase-N-...` سپس Merge به main در پایان فاز
- توکن GitHub هرگز در فایل‌های ریپازیتوری ذخیره نشود (تنها تنظیمات لوکال)

---

# 📊 چرخه سوم توسعه (v1.2) — بر اساس پرامپت‌های UI + سرور لایسنس cPanel

> شروع: 2026-10-05 | فایل مدیریت: TODO-v1.2.md | منبع: docs/prompts/prompt-ui-part1..5.html (مطالعه کامل ۵ فایل)

## فاز A: سرور لایسنس PHP برای cPanel — ✅ کامل (5/5)

### گام‌های L1-L3 — سرور PHP (شاخه feature/phase-13-license-php):
- ۱۲ فایل PHP خالص (۲۲۷۴ خط) بدون Composer/فریمورک — فقط pdo_mysql + openssl + json
- install.php سه‌مرحله‌ای → ۷ جدول MySQL (utf8mb4) + جفت‌کلید RSA-2048 + قفل نصب
- پنل RTL: داشبورد/صدور/تمدید/ابطال/انتقال/آفلاین .lic/لاگ/تنظیمات + CSRF + قفل ۵ خطا + حسابرسی
- api.php: activate (bind اولین اثر انگشت) + heartbeat + ping + Rate-Limit 60/10min
- قرارداد امضا: «کلید|انقضای yyyy-MM-dd|حداکثر کاربر» با SHA256+PKCS1 — بایت‌به‌بایت هم‌ارز دات‌نت

### گام L4 — اتصال واقعی کلاینت .NET:
- LicenseService بازنویسی شد: HttpClient typed + تحلیل پاسخ JSON + Upsert محدودیت‌ها/ماژول‌ها از سرور
- ImportPublicKey دوگانه: قالب XML (FromXmlString) و PEM (ImportFromPem)
- HeartbeatAsync + کار Hangfire روزانه ساعت ۳ بامداد + مهلت آفلاین ۳۰ روزه قابل تنظیم
- **رفع باگ**: میدلور لایسنس مسیر lab را به "Laboratory" نگاشت می‌کرد ولی کاتالوگ "Lab" است — اصلاح شد
- ۴ تست سازگاری امضا (بردار تولیدشده با cryptography پایتون = هم‌ارز openssl_sign)

### گام L5 — بسته‌بندی:
- download/Sahand-LicenseServer-cPanel-v1.0.zip (شامل ۳ فایل htaccess محافظ)
- README-FA.md: راهنمای گام‌به‌گام cPanel + نکات امنیتی + قرارداد API

### نتیجه فاز: کل Solution سبز (۰ هشدار) | ۵۴/۵۴ تست سبز


## فاز B: زیرساخت UI دسکتاپ — ✅ کامل (گام‌های U0-U2)

### گام U0 — پوسته و کنترل‌های مشترک:
- ۶ کنترل سفارشی: PersianCalendarControl + PersianDatePicker (+DateShamsiText دوطرفه) + TagInputControl + PaginationBar + StatCard + EmptyState
- ارتقای MainWindow: TopBar (مسیر/ساعت لحظه‌ای/زنگ/تم) + StatusBar + Snackbar سراسری + SnackbarService + ۵ مبدل
- رفع باگ طول ماه شمسی (PersianCalendar.GetDaysInMonth)

### گام U1 — ورود و داشبورد:
- LoginWindow ۵۰۰×۶۰۰ بدون حاشیه + گرادیان + چشم رمز + RememberMe (JSON محلی)
- DashboardView کامل + ۴ endpoint جدید (revenue-chart/appointments-today/recent-activities/alerts)
- رفع باگ تداخل نام RevenuePoint با ReportsViewModel

### گام U2 — ماژول بیمار:
- PatientsView: ۹ ستون + Badge بیمه + ContextMenu + دابل‌کلیک + PaginationBar + EmptyState
- PatientFormWindow: ۵ تب + اعتبارسنجی زنده کد ملی + بررسی خودکار یکتایی + سه TagInput
- PatientDetailWindow جدید: کارت سربرگ + هشدار قرمز آلرژی + تب ویزیت/نسخه متصل

## فاز F: داده‌های مرجع — ✅ کامل (گام‌های S1-S3)

- مولدهای پایتونی stepS1/stepS2_reference_data.py → ۶ فایل داده (۷۴۲۰ خط)
- ۱۱۵۳ ICD-10 | ۵۰۳۴ دارو | ۲۲۹ آزمایش | ۵۲۳ تداخل | ۵۷ تعرفه | ۳۱ استان/۶۳۵ شهر
- ReferenceDataSeeder (Order 45) با نگاشت کلاس‌های دارویی + تکرارپذیری کامل
- نتیجه چرخه تا اینجا: **۱۰/۲۸ گام** | ۵۴/۵۴ تست سبز | Solution بدون خطا

---

# 📊 چرخه چهارم توسعه (v1.3) — بلادرنگ، EMR پیشرفته، فیش حقوقی و DICOM

> شروع و پایان: 2026-10-05 | فایل مدیریت: TODO-v1.3.md | مبنای شروع: تگ v1.2.0-alpha
> شاخه: feature/phase-19-cycle4 — هر ۴ گام اجرایی + مستندات کامل شد

## گام R1 — اتصال بلادرنگ SignalR ✅
- قرارداد IRealtimeNotifier مستقل از فناوری در Application + پیاده‌سازی بی‌اثر پیش‌فرض (TryAdd در AddApplication)
- QueueHub گروه شعبه‌ای + NotificationHub گروه کاربری — هر دو [Authorize] با توکن از query string (رویداد OnMessageReceived در IdentitySetup)
- سیم‌کشی سه گرداننده صف + فرمان CreateNotification با پخش فوری + endpoint جدید POST notifications
- کلاینت WPF با دو HubConnection + تلاش مجدد خودکار (۰/۲/۵/۱۵/۳۰s) + رفرش زنده صف پذیرش/ویزیت + زنگ فوری TopBar + وضعیت StatusBar
- ۴ مجوز ویژه seeder شد (sms.bulksend که خلأ قبلی بود + notifications.manage + emr.advanced + payroll.manage)
- ۹ تست (۴ قرارداد + ۵ Moq) — جمع ۶۳/۶۳

## گام R2 — پرونده بالینی پیشرفته ✅
- ۳ موجودیت (GrowthMeasurement + FamilyHistory + ChronicCarePlan) + مهاجرت Cycle4EmrAdvanced (۳ جدول + ۵ ایندکس)
- داده مرجع WHO 2006 در Shared — ۴ شاخص × ۲ جنسیت + درون‌یابی خطی + z-score + طبقه‌بندی رنگی ۴سطحی
- EmrController با ۱۱ endpoint + نمای AdvancedEmrView سه‌تبی با نمودار رشد Canvas (میانه + ±۱/۲/۳ SD + نقاط رنگی بیمار)
- ۱۲ تست WHO — جمع ۷۵/۷۵

## گام R3 — فیش حقوقی ✅
- PayrollCalculator با قواعد ۱۴۰۴: اضافه‌کاری ۱/۴× (۱۹۲ ساعت) + بیمه ۷٪ + پلکان مالیات تجمعی (۲۴۰م/۳۶۰م/۶۰۰م) + کسر بیمه پیش از مالیات
- GenerateSalariesCommand idempotent از قرارداد + حضوروغیاب + SalariesController با ۴ endpoint
- PayrollView با انتخابگر دوره شمسی + ۳ کارت + جدول ۱۰ستونی + PayslipWindow با FlowDocument چاپی (۳ ناحیه امضا + PrintDialog)
- ۹ تست محاسباتی — جمع ۸۴/۸۴

## گام R4 — نمایشگر DICOM ✅
- تجزیه‌گر خالص DicomFileReader در Shared: P10 + Explicit/Implicit VR LE + خاکستری ۸/۱۶بیت + RGB + قطعه‌قطعه + دنباله تودرتو + Rescale + پنجره/سطح + خطای فارسی برای فشرده/BE
- DicomViewerWindow: اسلایدر پنجره/سطح + پنجره خودکار + بزرگ‌نمایی ۲۰-۸۰۰٪ + پنل متادیتای فارسی
- ۶ endpoint فایل تصویر (آپلود multipart + تشخیص جادوی DICM + دانلود + ...) + PostFileAsync/DownloadFileAsync در ApiClient + بخش DICOM در تب تصویربرداری
- ۹ تست با فایل مصنوعی — جمع نهایی **۹۳/۹۳ سبز**

## وضعیت نهایی چرخه چهارم:
- ۴/۴ گام + مستندات کامل | ۳۹ تست جدید (۵۴ → ۹۳) | برچسب v1.3.0-alpha
- کشف و رفع باگ: قرارداد بایت تگ DICOM (گروه در ۱۶ بیت پایین uint32) — با پروژه دیباگ ایزوله ریشه‌یابی شد
- ناوبری: ۲ آیتم جدید (پرونده بالینی پیشرفته + فیش حقوقی)
