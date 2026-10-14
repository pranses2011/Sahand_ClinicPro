using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجرهٔ «گروه منوی جدید» — چرخهٔ ۷۴.
/// عنوان دلخواه + آیکون از مجموعهٔ پیشنهادی؛ Enter = تأیید.
/// </summary>
public partial class NavGroupPromptWindow : Window
{
    /// <summary>عنوان انتخاب‌شدهٔ کاربر</summary>
    public string GroupTitle => TitleBox.Text.Trim();

    /// <summary>آیکون انتخاب‌شده (نام PackIcon) — پیش‌فرض پوشه</summary>
    public string SelectedIcon => IconList.SelectedItem as string ?? "Folder";

    /// <summary>سازنده — پر کردن فهرست آیکون‌ها و تمرکز روی عنوان</summary>
    public NavGroupPromptWindow()
    {
        InitializeComponent();
        IconList.ItemsSource = new[]
        {
            "Folder", "Hospital", "Prescription", "TestTube", "AccountTie",
            "CashMultiple", "MessageTextOutline", "Cog", "Star", "HeartPulse",
            "School", "ViewDashboard", "Bookmark", "Pin", "Archive",
        };
        IconList.SelectedIndex = 0;
        Loaded += (_, _) => { TitleBox.Focus(); };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = true;
                e.Handled = true;
            }
        };
    }

    /// <summary>ایجاد گروه — عنوان خالی پذیرفته نمی‌شود</summary>
    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            TitleBox.Focus();
            return;
        }
        DialogResult = true;
    }

    /// <summary>انصراف</summary>
    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
