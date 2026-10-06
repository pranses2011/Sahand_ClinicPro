# 📋 TODO نسخه ۲.۰.۱ — چرخه چهاردهم (وصله): «رفع کرش زمان اجرای MaterialDesign در ویژوال استودیو»

> 🔴 **گزارش مالک**: اجرای برنامه در Visual Studio با خطای
> `XamlParseException: Cannot find resource named 'MaterialDesignFilledButton'`
> در `LoginWindow.xaml` (خط ۱۳۶) متوقف می‌شود.
>
> 🔍 **ریشه‌یابی**: کلیدهای استایل به سبک MD3 خام (FilledButton و...) در پکیج `MaterialDesignThemes 5.3.2` **اصلاً وجود ندارند** — ارجاعات StaticResource در XAML هنگام بیلد اعتبارسنجی نمی‌شوند و فقط در زمان اجرا فرو می‌ریزند؛ به همین دلیل بیلد سبز و ۲۴۹ تست سبز این خطا را نشان ندادند.
>
> ⚠️ **ممیزی جامع انجام شد**: علاوه بر مورد گزارش‌شده، در مجموع **۱۰۱ ارجاع معیوب در ۹ کلید شبح‌مانند** در ۳۱ فایل XAML شناسایی شد — بدون این ممیزی، برنامه پس از رفع هر مورد روی مورد بعدی کرش می‌کرد.

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
| A9 | `BooleanToVisibilityConverter` (نام کلید، نه کلاس) در DashboardView | ۹ | `BoolToVis` (تعریف App.xaml) | 💥 کرش استاتیک |
| A10 | `InverseBool` در InsuranceView | ۱ | `InvertBool` (تعریف App.xaml) | 💥 کرش استاتیک |
| — | `MaterialDesign3.Defaults.xaml` | ۱ | ✅ موجود است (مثبت کاذب ممیزی) | بدون مشکل |

**نکته کلیدی**: `MaterialDesign3.Defaults.xaml` در پکیج موجود و ادغام App.xaml صحیح است — اشتباه فقط در «نام کلید استایل‌ها» بوده، نه فایل تم. استایل‌های MD3 دکمه در این پکیج همچنان با کلیدهای کلاسیک (RaisedButton و...) تعریف می‌شوند.

## 🔍 فاز B — ابزار ممیزی و گاردهای آینده

- [ ] B1 — اسکریپت ممیزی `scripts/audit_xaml_resources.py` در مخزن (استخراج کلیدهای واقعی از DLL پکیج + مقایسه با ارجاعات XAML) + فهرست کلیدها `scripts/materialdesign-keys-5.3.2.txt`
- [ ] B2 — تست بازگشتی `XamlResourceAuditTests` در پروژه تست دسکتاپ — اگر کسی ارجاع نامعتبر جدید اضافه کند، تست قرمز می‌شود (چون XAML در بیلد اعتبارسنجی نمی‌شود، این تنها گارد است)

## ✅ فاز C — تأیید و انتشار

- [ ] C1 — بیلد Release کامل ۰ خطا + کل تست‌ها سبز
- [ ] C2 — ارتقای نسخه به 2.0.1 در Directory.Build.props + برچسب نسخه پنجره ورود خودکار به‌روز می‌شود
- [ ] C3 — CHANGELOG (بخش 2.0.1) + PROGRESS (چرخه چهاردهم) + تاریخچه نسخه README
- [ ] C4 — رلیز v2.0.1 با الگوی قبلی (یادداشت فارسی RTL) — دارایی‌های بازاریابی v2.0.0 معتبر می‌مانند (تغییر فقط در XAML دسکتاپ است)

---

## 📌 قواعد ثابت این چرخه

1. هر واحد منطقی → کامیت + پوش قبل از رفتن به واحد بعدی (دستور صریح مالک)
2. هیچ کلید منبعی بدون تأیید وجود در فهرست ۸۷۲ کلیدی پکیج نوشته نشود
3. جایگزینی‌ها معنایی هستند: Filled→Raised (دکمه پررنگ)، Error→ValidationError (قرمز خطا)، Background.Variant→Separator (بند/حاشیه خنثی)، Container→Primary.Light (سطح روشن رنگ اصلی)
4. متن‌های UI فارسی RTL حفظ شوند — این چرخه فقط تعویض کلید منبع است، هیچ متن/چیدمانی تغییر نمی‌کند
