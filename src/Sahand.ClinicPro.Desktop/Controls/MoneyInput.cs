using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// رفتار کادر مبلغ — چرخه بیستم (گزارش چهاردهم مالک):
/// «مبالغ در کادرهای متنیِ ورود هم با جداکنندهٔ ۳ رقمی باشند».
/// با فعال‌کردن این رفتار روی یک TextBox، متنِ در حال تایپ بی‌درنگ
/// با جداکنندهٔ هزارگان قالب‌بندی می‌شود («۱۲۳۴۵۶۷» → «1,234,567»)،
/// ارقام فارسی هم پذیرفته و نرمال می‌شوند و مقدار عددی تمیز در
/// AttachedProperty خروجی (NumericValue) برای اتصال TwoWay در دسترس است.
/// کاربرد: <code>TextBox behaviors:MoneyInput.IsEnabled="True"</code>
/// </summary>
public static class MoneyInput
{
    #region IsEnabled — فعال‌سازی رفتار

    /// <summary>AttachedProperty فعال‌سازی قالب‌بندی زندهٔ مبلغ</summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(MoneyInput),
            new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>گرفتن وضعیت فعال</summary>
    public static bool GetIsEnabled(DependencyObject obj)
        => (bool)obj.GetValue(IsEnabledProperty);

    /// <summary>گذاشتن وضعیت فعال — با true، رفتار به TextBox وصل می‌شود</summary>
    public static void SetIsEnabled(DependencyObject obj, bool value)
        => obj.SetValue(IsEnabledProperty, value);

    /// <summary>وصله‌کردن TextChanged هنگام فعال‌سازی</summary>
    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box)
            return;

        if (e.NewValue is true)
            box.TextChanged += OnTextChanged;
        else
            box.TextChanged -= OnTextChanged;
    }

    #endregion

    #region NumericValue — مقدار عددی تمیز برای Binding

    /// <summary>
    /// مقدار عددی تمیز (بدون جداکننده، به‌رسم نمایشی) — اتصال TwoWay واقعی به VM
    /// </summary>
    public static readonly DependencyProperty NumericValueProperty =
        DependencyProperty.RegisterAttached(
            "NumericValue", typeof(decimal), typeof(MoneyInput),
            new FrameworkPropertyMetadata(0m,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnNumericValueChanged));

    /// <summary>گرفتن مقدار عددی</summary>
    public static decimal GetNumericValue(DependencyObject obj)
        => (decimal)obj.GetValue(NumericValueProperty);

    /// <summary>گذاشتن مقدار عددی — متن کادر هم قالب‌بندی می‌شود</summary>
    public static void SetNumericValue(DependencyObject obj, decimal value)
        => obj.SetValue(NumericValueProperty, value);

    /// <summary>مقدار عددی از بیرون تغییر کرد → بازنویسی متن قالب‌بندی‌شده</summary>
    private static void OnNumericValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box)
            return;

        // مقدار نمایشی → رشتهٔ قالب‌بندی‌شدهٔ هزارگان
        var text = ((decimal)e.NewValue) == 0
            ? string.Empty
            : ((decimal)e.NewValue).ToString("#,0", System.Globalization.CultureInfo.InvariantCulture);

        // جلوگیری از حلقهٔ بازگشتی — فقط اگر متن واقعاً متفاوت است
        if (!string.Equals(box.Text, text, StringComparison.Ordinal))
        {
            var caret = box.CaretIndex;
            box.Text = text;
            // مکان‌نما را انتهای متن نگه می‌داریم (رفتار طبیعی تایپ انتهای عدد)
            box.CaretIndex = box.Text.Length;
            _ = caret; // موقعیت قبلی فقط برای تشخیص تایپ میانی است — ساده نگه داشتیم
        }
    }

    #endregion

    /// <summary>
    /// منطق اصلی: متن جدید → پارس (با ارقام فارسی و بدون جداکننده) →
    /// قالب‌بندی با جداکنندهٔ ۳ رقمی → بازنویسی متن + همگام‌سازی NumericValue.
    /// کاراکترهای غیرعددی بی‌صدا حذف می‌شوند.
    /// </summary>
    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox box)
            return;

        var raw = box.Text;
        if (string.IsNullOrEmpty(raw))
        {
            box.SetValue(NumericValueProperty, 0m);
            return;
        }

        // نرمال‌سازی: ارقام فارسی/عربی → لاتین؛ فقط رقم و جداکننده باقی بماند
        var latin = Money.PersianDigitsToLatin(raw);
        var digitsOnly = new System.Text.StringBuilder(latin.Length);
        foreach (var ch in latin)
        {
            if (char.IsDigit(ch))
                digitsOnly.Append(ch);
            // جداکننده‌های موجود نادیده گرفته می‌شوند — دوباره قالب‌بندی می‌کنیم
        }

        if (digitsOnly.Length == 0)
        {
            box.Text = string.Empty;
            box.SetValue(NumericValueProperty, 0m);
            return;
        }

        // مقدار عددی تمیز — به رسم نمایشی (تومان/ریال از دید کاربر)
        var value = decimal.Parse(digitsOnly.ToString(), System.Globalization.CultureInfo.InvariantCulture);
        box.SetValue(NumericValueProperty, value);

        // قالب‌بندی هزارگان — اگر متن کاربر همین است، بازنویسی نکن (حلقه/پرش مکان‌نما)
        var formatted = value.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture);
        if (!string.Equals(raw, formatted, StringComparison.Ordinal))
        {
            box.Text = formatted;
            box.CaretIndex = box.Text.Length;
        }
    }
}
