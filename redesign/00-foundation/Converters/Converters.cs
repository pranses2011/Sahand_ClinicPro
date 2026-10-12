using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Sahand.ClinicPro.Desktop.Converters
{
    /// <summary>
    /// تبدیل مقدار تهی به <see cref="Visibility.Collapsed"/>
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>معکوس کردن نتیجه</summary>
        public bool Invert { get; set; }

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isNull = value is null || (value is string s && string.IsNullOrWhiteSpace(s));
            if (Invert) isNull = !isNull;
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// تبدیل مقدار منطقی به <see cref="Visibility"/>
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>معکوس کردن نتیجه</summary>
        public bool Invert { get; set; }

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var flag = value is true;
            if (Invert) flag = !flag;
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is Visibility.Visible;
    }

    /// <summary>
    /// تبدیل رشته‌ی خالی یا مجموعه‌ی تهی به <see cref="Visibility.Collapsed"/>
    /// </summary>
    public class EmptyToVisibilityConverter : IValueConverter
    {
        /// <summary>معکوس کردن نتیجه</summary>
        public bool Invert { get; set; }

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isEmpty = value switch
            {
                null => true,
                string s => string.IsNullOrWhiteSpace(s),
                System.Collections.ICollection c => c.Count == 0,
                _ => false
            };

            if (Invert) isEmpty = !isEmpty;
            return isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// تبدیل تاریخ میلادی به رشته‌ی شمسی
    /// </summary>
    public class PersianDateConverter : IValueConverter
    {
        /// <summary>الگوی خروجی؛ پیش‌فرض «yyyy/MM/dd»</summary>
        public string Format { get; set; } = "yyyy/MM/dd";

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not DateTime dt)
                return string.Empty;

            var pc = new PersianCalendar();
            return string.Format("{0:0000}/{1:00}/{2:00}",
                pc.GetYear(dt), pc.GetMonth(dt), pc.GetDay(dt));
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// قالب‌بندی مبالغ با جداکننده‌ی هزارگان و نماد «تومان»
    /// </summary>
    public class MoneyConverter : IValueConverter
    {
        /// <summary>آیا نماد ارز نمایش داده شود</summary>
        public bool ShowSymbol { get; set; } = true;

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not decimal and not double and not int and not long)
                return string.Empty;

            var amount = System.Convert.ToDecimal(value);
            var formatted = amount.ToString("N0", CultureInfo.CurrentCulture);
            return ShowSymbol ? $"{formatted} تومان" : formatted;
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// نمایش توضیح فارسیِ یک عضوِ Enum از طریق ویژگی
    /// <see cref="System.ComponentModel.DescriptionAttribute"/>
    /// </summary>
    public class EnumDescriptionConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return string.Empty;

            var type = value.GetType();
            var name = value.ToString();
            if (name is null) return string.Empty;

            var field = type.GetField(name);
            if (field is null) return name;

            var attribute = field
                .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                .OfType<System.ComponentModel.DescriptionAttribute>()
                .FirstOrDefault();

            return attribute?.Description ?? name;
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
