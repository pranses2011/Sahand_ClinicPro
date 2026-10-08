using System.Globalization;
using System.Windows.Data;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل نام متغیر → برچسب فارسی قابل‌فهم — چرخه بیست‌وچهارم (گزارش هفدهم
/// مالک): «دکمه‌هایی که برای متغیرها ظاهر می‌شود نامفهوم هستند و هیچ آیکون
/// و متنی ندارند» — پیش‌تر دکمه‌های پالت متغیر «بدون هیچ Content» رندر
/// می‌شدند (خالی!). اکنون هر دکمه آیکون 🧩 + نام فارسی متغیر را نشان می‌دهد
/// و راهنمای اصلی (توکن خام) در ToolTip دیده می‌شود.
/// </summary>
public sealed class VariableLabelConverter : IValueConverter
{
    /// <summary>نگاشت توکن → برچسب فارسی — همهٔ متغیرهای ۹ نوع سند</summary>
    private static readonly Dictionary<string, string> Fa = new()
    {
        // مشترک
        ["patient_name"] = "نام بیمار",
        ["patient_age"] = "سن بیمار",
        ["patient_national_code"] = "کد ملی بیمار",
        ["doctor_name"] = "نام پزشک",
        ["insurance_name"] = "نام بیمه",
        ["items_html"] = "جدول اقلام",
        ["notes"] = "توضیحات",
        // فاکتور
        ["invoice_code"] = "شماره فاکتور",
        ["invoice_date"] = "تاریخ فاکتور",
        ["subtotal"] = "جمع کل",
        ["discount"] = "تخفیف",
        ["insurance_share"] = "سهم بیمه",
        ["patient_share"] = "قابل پرداخت بیمار",
        ["total"] = "مبلغ نهایی",
        // نسخه
        ["prescription_code"] = "شماره نسخه",
        ["visit_date"] = "تاریخ ویزیت",
        // آزمایش
        ["order_number"] = "شماره درخواست",
        ["order_date"] = "تاریخ درخواست",
        ["tests_html"] = "فهرست آزمون‌ها",
        ["clinical_info"] = "اطلاعات بالینی",
        ["result_date"] = "تاریخ نتیجه",
        ["results_html"] = "جدول نتایج",
        ["verified_by"] = "تأییدکننده",
        // تصویربرداری
        ["modality"] = "نوع تصویربرداری",
        // نوبت
        ["appointment_date"] = "تاریخ نوبت",
        ["appointment_time"] = "ساعت نوبت",
        ["queue_number"] = "شماره نوبت",
        ["tracking_code"] = "کد رهگیری",
        // پذیرش
        ["reception_code"] = "شماره رسید",
        ["reception_date"] = "تاریخ پذیرش",
        ["service_name"] = "نام خدمت",
        ["amount"] = "مبلغ",
        ["payment_method"] = "روش پرداخت",
        // گواهی
        ["issue_date"] = "تاریخ صدور",
        ["content"] = "متن گواهی",
        ["duration"] = "مدت",
        // فیش حقوقی
        ["employee_name"] = "نام پرسنل",
        ["period"] = "دوره",
        ["base_salary"] = "حقوق پایه",
        ["overtime_pay"] = "اضافه‌کاری",
        ["insurance"] = "بیمه",
        ["tax"] = "مالیات",
        ["net_pay"] = "خالص پرداختی",
    };

    /// <summary>توکن خام → «🧩 نام فارسی»؛ توکن ناشناخته خام می‌ماند</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var token = value?.ToString() ?? string.Empty;
        return Fa.TryGetValue(token, out var fa)
            ? $"🧩 {fa}"
            : $"🧩 {token}";
    }

    /// <summary>بدون پشتیبانی</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
