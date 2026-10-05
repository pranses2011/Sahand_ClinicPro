using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پذیرش — گام U5: رنگ‌بندی ردیف‌های صف بر اساس وضعیت
/// </summary>
public partial class ReceptionsView : UserControl
{
    /// <summary>رنگ وضعیت‌های صف — مطابق فرم ۱۰</summary>
    private static readonly SolidColorBrush[] StatusBrushes =
    {
        new(Color.FromRgb(0xFF, 0xFF, 0xFF)), // پذیرش‌شده
        new(Color.FromRgb(0xFF, 0xF8, 0xE1)), // در صف — زرد روشن
        new(Color.FromRgb(0xE3, 0xF2, 0xFD)), // در ویزیت — آبی روشن
        new(Color.FromRgb(0xE8, 0xF5, 0xE9)), // ویزیت‌شده — سبز روشن
        new(Color.FromRgb(0xEC, 0xEF, 0xF1)), // ترخیص — خاکستری
        new(Color.FromRgb(0xFF, 0xEB, 0xEE)), // لغو — قرمز روشن
    };

    /// <summary>سازنده</summary>
    public ReceptionsView()
    {
        InitializeComponent();
    }

    /// <summary>رنگ پس‌زمینه ردیف صف بر اساس نمایه وضعیت</summary>
    private void OnQueueRowLoading(object sender, System.Windows.Controls.DataGridRowEventArgs e)
    {
        if (sender is DataGridRow row && row.Item is Sahand.ClinicPro.Desktop.ViewModels.ReceptionRow reception)
        {
            var index = System.Math.Clamp(reception.StatusId - 1, 0, StatusBrushes.Length - 1);
            row.Background = StatusBrushes[index];
            if (reception.StatusId is 4 or 5)
                row.Opacity = 0.7; // تمام‌شده‌ها کم‌رنگ
        }
    }
}
