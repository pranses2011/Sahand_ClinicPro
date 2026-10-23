using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Controls
{
    /// <summary>
    /// کارت نمایش یک شاخص کلیدی (KPI) همراه با روند تغییرات
    /// </summary>
    public partial class StatCard : UserControl
    {
        public StatCard()
        {
            InitializeComponent();
        }

        /// <summary>عنوان شاخص</summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(StatCard),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>مقدار شاخص</summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(StatCard),
                new PropertyMetadata("—"));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>شکلک نمایش‌داده‌شده</summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(StatCard),
                new PropertyMetadata("📊"));

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>درصد تغییر نسبت به دوره‌ی قبل</summary>
        public static readonly DependencyProperty TrendProperty =
            DependencyProperty.Register(nameof(Trend), typeof(double?), typeof(StatCard),
                new PropertyMetadata(null, OnTrendChanged));

        public double? Trend
        {
            get => (double?)GetValue(TrendProperty);
            set => SetValue(TrendProperty, value);
        }

        /// <summary>توضیح کوچک زیر مقدار</summary>
        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(nameof(Hint), typeof(string), typeof(StatCard),
                new PropertyMetadata(null));

        public string? Hint
        {
            get => (string?)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        /// <summary>نام رنگ تاکیدی: Primary | Success | Warning | Danger | Info</summary>
        public static readonly DependencyProperty AccentProperty =
            DependencyProperty.Register(nameof(Accent), typeof(string), typeof(StatCard),
                new PropertyMetadata("Primary"));

        public string Accent
        {
            get => (string)GetValue(AccentProperty);
            set => SetValue(AccentProperty, value);
        }

        // ══════════ ویژگی‌های محاسباتی ══════════

        /// <summary>متن نمایشیِ روند</summary>
        public static readonly DependencyPropertyKey TrendTextPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(TrendText), typeof(string), typeof(StatCard),
                new PropertyMetadata(null));

        public static readonly DependencyProperty TrendTextProperty =
            TrendTextPropertyKey.DependencyProperty;

        public string? TrendText => (string?)GetValue(TrendTextProperty);

        /// <summary>رنگِ متنِ روند</summary>
        public static readonly DependencyPropertyKey TrendBrushPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(TrendBrush), typeof(Brush), typeof(StatCard),
                new PropertyMetadata(null));

        public static readonly DependencyProperty TrendBrushProperty =
            TrendBrushPropertyKey.DependencyProperty;

        public Brush? TrendBrush => (Brush?)GetValue(TrendBrushProperty);

        private static void OnTrendChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not StatCard card) return;

            if (e.NewValue is not double trend)
            {
                card.SetValue(TrendTextPropertyKey, null);
                return;
            }

            var arrow = trend > 0 ? "▲" : trend < 0 ? "▼" : "■";
            card.SetValue(TrendTextPropertyKey,
                string.Format(CultureInfo.CurrentCulture, "{0} {1:0.#}%", arrow, Math.Abs(trend)));

            var resourceKey = trend > 0 ? "Brush.Success" : trend < 0 ? "Brush.Danger" : "Brush.TextMuted";
            card.SetValue(TrendBrushPropertyKey,
                Application.Current.TryFindResource(resourceKey) as Brush);
        }
    }
}
