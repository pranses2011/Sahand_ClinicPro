using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sahand.ClinicPro.Desktop.Controls
{
    /// <summary>
    /// جعبه‌ی جستجو با دکمه‌ی پاک‌کردن و پشتیبانی از میانبرها
    /// </summary>
    public partial class SearchBox : UserControl
    {
        public SearchBox()
        {
            InitializeComponent();
            Loaded += (_, _) => Keyboard.Focus(InputBox);
        }

        /// <summary>متن جستجو</summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(SearchBox),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>متن راهنما</summary>
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchBox),
                new PropertyMetadata("جستجو…"));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            Text = string.Empty;
            Keyboard.Focus(InputBox);
        }
    }
}
