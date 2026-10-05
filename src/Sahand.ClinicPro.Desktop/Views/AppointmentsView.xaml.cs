using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت نوبت‌دهی دو-تبی — گام U4
/// </summary>
public partial class AppointmentsView : UserControl
{
    /// <summary>سازنده</summary>
    public AppointmentsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>اتصال رویداد انتخاب تاریخ تقویم به فرمان ViewModel</summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is AppointmentsViewModel vm)
        {
            vm.SelectDateCommand.Execute(null);
        }
    }

    /// <summary>رویداد انتخاب تاریخ از تقویم شمسی</summary>
    private void OnCalendarDateSelected(object? sender, DateTime? e)
    {
        if (DataContext is AppointmentsViewModel vm)
            vm.SelectDateCommand.Execute(e);
    }
}
