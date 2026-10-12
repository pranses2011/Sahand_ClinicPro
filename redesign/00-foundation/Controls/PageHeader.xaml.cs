using System.Windows;
using System.Windows.Controls;

namespace Sahand.ClinicPro.Desktop.Controls
{
    /// <summary>
    /// هدر یکپارچه‌ی صفحات — عنوان، زیرعنوان، آیکون و ناحیه‌ی اقدام‌ها
    /// </summary>
    public partial class PageHeader : UserControl
    {
        public PageHeader()
        {
            InitializeComponent();
        }

        /// <summary>عنوان صفحه</summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(PageHeader),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>زیرعنوان یا توضیح کوتاه</summary>
        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(PageHeader),
                new PropertyMetadata(null));

        public string? Subtitle
        {
            get => (string?)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        /// <summary>شکلک یا گلیف نمایش‌داده‌شده در سمت راست</summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(PageHeader),
                new PropertyMetadata(null));

        public string? Icon
        {
            get => (string?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>محتوای دکمه‌ی اقدامِ اصلی</summary>
        public static readonly DependencyProperty PrimaryActionProperty =
            DependencyProperty.Register(nameof(PrimaryAction), typeof(object), typeof(PageHeader),
                new PropertyMetadata(null));

        public object? PrimaryAction
        {
            get => GetValue(PrimaryActionProperty);
            set => SetValue(PrimaryActionProperty, value);
        }

        /// <summary>محتوای اقدام‌های ثانویه</summary>
        public static readonly DependencyProperty SecondaryActionsProperty =
            DependencyProperty.Register(nameof(SecondaryActions), typeof(object), typeof(PageHeader),
                new PropertyMetadata(null));

        public object? SecondaryActions
        {
            get => GetValue(SecondaryActionsProperty);
            set => SetValue(SecondaryActionsProperty, value);
        }
    }
}
