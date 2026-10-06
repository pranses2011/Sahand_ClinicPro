# 📋 TODO نسخه ۲.۰.۱ — چرخه چهاردهم (وصله): «رفع کرش زمان اجرای MaterialDesign در ویژوال استودیو»

> ✅ **وضعیت: چرخه چهاردهم کامل شد — ۱۱۰ ارجاع در ۴۱ فایل اصلاح + گارد بازگشتی + بیلد ۰ خطا + ۲۵۰/۲۵۰ تست سبز + رلیز v2.0.1**

## 🎯 هدف چرخه

تبدیل همه ارجاعات منبع نامعتبر XAML به کلیدهای واقعی موجود در پکیج `MaterialDesignThemes 5.3.2`
(استخراج‌شده مستقیم از DLL پکیج — فهرست ۸۷۲ کلیدی) + بیلد و تست سبز + رلیز وصله v2.0.1

---

## 🔧 فاز A — نگاشت ارجاعات معیوب (۱۰۱ مورد در ۹ کلید)

| # | کلید شبح‌مانند | تعداد | معادل معتبر | نوع خطا |
|---|:---|:---:|:---|:---|
| A1 | `MaterialDesignFilledButton` | ۱۸ | `MaterialDesignRaisedButton` | 💥 کرش استاتیک |
| A2 | `MaterialDesign.Brush.Error` | ۱۵ | `MaterialDesign.Brush.ValidationError` | بی‌رنگ (داینامیک) |
| A3 | `MaterialDesign.Brush.Background.Variant` | ۴۳ | `MaterialDesign.Brush.Separator.Background` | بی‌رنگ (داینامیک) |
| A4 | `MaterialDesign.Brush.Primary.Container` | ۱ | `MaterialDesign.Brush.Primary.Light` | بی‌رنگ (داینامیک) |
| A5 | `MaterialDesign.Card.Background` | ۲۰ | `MaterialDesign.Brush.Card.Background` | بی‌رنگ (داینامیک) |
| A6 | `MaterialDesignChipOutlineButton` | ۱ | `MaterialDesignOutlinedButton` | 💥 کرش استاتیک |
| A7 | `MaterialDesignFlatMidButton` | ۱ | `MaterialDesignFlatMidBgButton` | 💥 کرش استاتیک |
| A8 | `MaterialDesignFlatPrimaryButton` | ۱ | `MaterialDesignFlatButton` | 💥 کرش استاتیک |
| A9 | `BooleanToVisibilityConverter` (نام کلید، نه کلاس) در DashboardView | ۹ | `BoolToVis` (تعریف App.xaml) | 💥 کرش استاتیک — ✅ شد |
| A10 | `InverseBool` در InsuranceView | ۱ | `InvertBool` (تعریف App.xaml) | 💥 کرش استاتیک — ✅ شد |
| — | `MaterialDesign3.Defaults.xaml` | ۱ | ✅ موجود است (مثبت کاذب ممیزی) | بدون مشکل |

**نکته کلیدی**: `MaterialDesign3.Defaults.xaml` در پکیج موجود و ادغام App.xaml صحیح است — اشتباه فقط در «نام کلید استایل‌ها» بوده، نه فایل تم. استایل‌های MD3 دکمه در این پکیج همچنان با کلیدهای کلاسیک (RaisedButton و...) تعریف می‌شوند.

## 🔍 فاز B — ابزار ممیزی و گاردهای آینده

- [x] B1 — اسکریپت ممیزی `scripts/audit_xaml_resources.py` در مخزن (استخراج کلیدهای واقعی از DLL پکیج + مقایسه با ارجاعات XAML) + فهرست کلیدها `scripts/materialdesign-keys-5.3.2.txt`
- [x] B2 — تست بازگشتی `XamlResourceAuditTests` در پروژه تست دسکتاپ — اگر کسی ارجاع نامعتبر جدید اضافه کند، تست قرمز می‌شود (چون XAML در بیلد اعتبارسنجی نمی‌شود، این تنها گارد است)

## ✅ فاز C — تأیید و انتشار

- [x] C1 — بیلد Release کامل ۰ خطا + کل تست‌ها سبز (۲۵۰/۲۵۰)
- [x] C2 — ارتقای نسخه به 2.0.1 در Directory.Build.props + برچسب نسخه پنجره ورود خودکار به‌روز می‌شود (کلاس Helpers/AppInfo — پانویس ورود از v1.2.0 کهنه بود و هاردکد شد به دینامیک)
- [x] C3 — CHANGELOG (بخش 2.0.1) + PROGRESS (چرخه چهاردهم) + تاریخچه نسخه README
- [x] C4 — رلیز v2.0.1 با الگوی قبلی (یادداشت فارسی RTL) — دارایی‌های بازاریابی v2.0.0 معتبر می‌مانند (تغییر فقط در XAML دسکتاپ است)

## 📗 پیوست پس از انتشار — راهنمای SQL و رفع نقص آن

- [x] P1 — راهنمای PDF «اتصال به SQL Server و ساخت دیتابیس در اولین نصب و اجرا» (۱۸ صفحه مبتدی‌پسند) + انتشار در هر دو رلیز v2.0.0 و v2.0.1 + رفع رگرسیون چاپ RTL کرومیوم (`pre-wrap`) + ثبت در CHANGELOG
- [x] P2 — **رفع شکست قدم ۵ راهنما (گزارش مالک)**: دستور `dotnet ef database update` با خطای `Run "dotnet tool restore" to make the "dotnet-ef" command available` متوقف می‌شد — ریشه: manifest ابزار محلی `.config/dotnet-tools.json` همراه پروژه است و بر نصب سراسری اولویت دارد؛ راهنمای قبلی می‌گفت `dotnet tool install --global dotnet-ef` که برای این پروژه کافی نیست. اصلاح: قدم ۵ مرحله ۳ → `dotnet tool restore` + ردیف اختصاصی در جدول رفع اشکال ۱ + اصلاح ردیف `No executable found` + تکمیل `deployment.md` + بازرندر PDF و جایگزینی در هر دو رلیز (کامیت‌های `7aa233b` و `a59ee35`)
- [x] P3 — **رفع خطای SEQUENCE و نویزهای قدم ۵ (گزارش دوم مالک)**: `dotnet ef database update` این‌بار به `Unknown object type 'SEQUENCE'` (Error 343) خورد — ریشه: اتصال به نمونه SQL قدیمی‌تر از ۲۰۱۲ (مهاجرت InitialCreate با ۱۵ عدد CreateSequence شروع می‌شود؛ نشانه دیگر: هشدارهای زرد TLS 1.0). کارها: (۱) گارد `HostAbortedException` در Program.cs برای حذف نویز «💥 خطای مهلک» طراحی‌زمان (`a01bda9`)؛ (۲) هم‌نسخی manifest dotnet-ef با runtime: 8.0.11→8.0.31 (`3749659`)؛ (۳) تقویت راهنما: ردیف SEQUENCE + ردیف نویز + جدول تشخیص نسخه نمونه در قدم ۲ + جعبه «دو نوشته که نگران‌تان نکند» (`e7d90ec`) + بازرندر PDF و جایگزینی در هر دو رلیز

---

## 📌 قواعد ثابت این چرخه

1. هر واحد منطقی → کامیت + پوش قبل از رفتن به واحد بعدی (دستور صریح مالک)
2. هیچ کلید منبعی بدون تأیید وجود در فهرست ۸۷۲ کلیدی پکیج نوشته نشود
3. جایگزینی‌ها معنایی هستند: Filled→Raised (دکمه پررنگ)، Error→ValidationError (قرمز خطا)، Background.Variant→Separator (بند/حاشیه خنثی)، Container→Primary.Light (سطح روشن رنگ اصلی)
4. متن‌های UI فارسی RTL حفظ شوند — این چرخه فقط تعویض کلید منبع است، هیچ متن/چیدمانی تغییر نمی‌کند
