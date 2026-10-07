using System.Windows;
using System.Windows.Input;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// نمایشگر بزرگ عکس پروفایل — چرخه هجدهم (گزارش دوازدهم مالک):
/// «با کلیک روی عکس اون رو بزرگ نشون بده» — تمام‌صفحهٔ تیره با تصویر
/// سفارشی‌شده؛ بستن با کلیک روی هر نقطه یا Esc.
/// </summary>
public partial class PhotoViewerWindow : Window
{
    /// <summary>سازنده با تصویر و عنوان اختیاری</summary>
    public PhotoViewerWindow(System.Windows.Media.ImageSource source, string? title = null)
    {
        InitializeComponent();
        PhotoImage.Source = source;
        if (!string.IsNullOrWhiteSpace(title))
            ViewerTitle.Text = title;

        MouseLeftButtonUp += (_, _) => Close();
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };
    }
}
