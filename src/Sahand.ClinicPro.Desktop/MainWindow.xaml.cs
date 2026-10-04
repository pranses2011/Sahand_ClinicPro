using System.Windows;

namespace Sahand.ClinicPro.Desktop;

/// <summary>
/// کد پشت پنجره اصلی کلاینت دسکتاپ — اسکلت گام ۱.
/// در گام ۱۰ این پنجره به پوسته اصلی ناوبری (Sidebar + هدر)
/// تبدیل می‌شود و منطق آن به MainWindowViewModel منتقل خواهد شد.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// سازنده پیش‌فرض پنجره اصلی.
    /// </summary>
    public MainWindow()
    {
        // راه‌اندازی اجزای XAML — همیشه باید اولین دستور سازنده باشد
        InitializeComponent();
    }
}
