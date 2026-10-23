using System.Windows;
using System.Windows.Controls;

namespace Sahand.ClinicPro.Desktop.Controls
{
    /// <summary>
    /// پیوندِ امنِ کلمه عبور به ViewModel
    /// </summary>
    /// <remarks>
    /// <see cref="PasswordBox.Password"/> یک ویژگی وابستگی نیست و قابل
    /// اتصال (Binding) نیست. این کلاس با یک ویژگیِ پیوسته، مقدار را هم‌گام
    /// نگه می‌دارد و توسط <see cref="IsUpdating"/> از حلقه‌ی بی‌پایان جلوگیری
    /// می‌کند.
    ///
    /// نمونه استفاده:
    /// <code>
    /// &lt;PasswordBox Style="{DynamicResource Input.Password}"
    ///               ctrl:PasswordHelper.Attach="True"
    ///               ctrl:PasswordHelper.Password="{Binding Password, Mode=TwoWay}" /&gt;
    /// </code>
    /// </remarks>
    public static class PasswordHelper
    {
        private static bool _isUpdating;

        // ══════════════════════════════════════════════════════════════════
        //  Attach
        // ══════════════════════════════════════════════════════════════════

        /// <summary>ویژگی پیوسته برای فعال‌سازی همگام‌سازی</summary>
        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached(
                "Attach", typeof(bool), typeof(PasswordHelper),
                new PropertyMetadata(false, OnAttachChanged));

        /// <summary>دریافت مقدار Attach</summary>
        public static bool GetAttach(DependencyObject d)
            => (bool)d.GetValue(AttachProperty);

        /// <summary>تنظیم مقدار Attach</summary>
        public static void SetAttach(DependencyObject d, bool value)
            => d.SetValue(AttachProperty, value);

        private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox box) return;

            box.PasswordChanged -= OnPasswordChanged;

            if ((bool)e.NewValue)
                box.PasswordChanged += OnPasswordChanged;
        }

        // ══════════════════════════════════════════════════════════════════
        //  Password
        // ══════════════════════════════════════════════════════════════════

        /// <summary>ویژگی پیوسته‌ی مقدار کلمه عبور</summary>
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached(
                "Password", typeof(string), typeof(PasswordHelper),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnPasswordPropertyChanged));

        /// <summary>دریافت مقدار کلمه عبور</summary>
        public static string GetPassword(DependencyObject d)
            => (string)d.GetValue(PasswordProperty);

        /// <summary>تنظیم مقدار کلمه عبور</summary>
        public static void SetPassword(DependencyObject d, string value)
            => d.SetValue(PasswordProperty, value);

        private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (_isUpdating) return;
            if (d is not PasswordBox box) return;

            _isUpdating = true;
            box.Password = e.NewValue as string ?? string.Empty;
            _isUpdating = false;
        }

        private static void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdating) return;
            if (sender is not PasswordBox box) return;

            _isUpdating = true;
            SetPassword(box, box.Password);
            _isUpdating = false;
        }
    }
}
