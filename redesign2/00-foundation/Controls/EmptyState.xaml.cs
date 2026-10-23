using System.Windows;
using System.Windows.Controls;

namespace Sahand.ClinicPro.Desktop.Controls
{
    /// <summary>
    /// نمایش وضعیت تهی برای زمانی که داده‌ای برای نمایش وجود ندارد
    /// </summary>
    public partial class EmptyState : UserControl
    {
        public EmptyState()
        {
            InitializeComponent();
        }

        /// <summary>آیکون بزرگ (StreamGeometry از Icons.xaml)</summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(EmptyState),
                new PropertyMetadata(null));

        public Geometry? Icon
        {
            get => (Geometry?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>عنوان وضعیت تهی</summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyState),
                new PropertyMetadata("موردی یافت نشد"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>توضیح تکمیلی</summary>
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(EmptyState),
                new PropertyMetadata(null));

        public string? Message
        {
            get => (string?)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        /// <summary>دکمه یا محتوای اقدام</summary>
        public static readonly DependencyProperty ActionProperty =
            DependencyProperty.Register(nameof(Action), typeof(object), typeof(EmptyState),
                new PropertyMetadata(null));

        public object? Action
        {
            get => GetValue(ActionProperty);
            set => SetValue(ActionProperty, value);
        }
    }
}
