using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم‌های تکمیلی — گام U19 + طراح قالب چرخه ۱۳:
/// اتصال پیش‌نمایش HTML قالب به مرورگر داخلی
/// </summary>
public partial class ComplementaryView : UserControl
{
    /// <summary>سازنده</summary>
    public ComplementaryView()
    {
        InitializeComponent();
        TypeCombo.ItemsSource = ComplementaryViewModel.TemplateTypes;
        DataContextChanged += OnDataContextChangedAsync;
    }

    /// <summary>اشتراک در تغییر HTML پیش‌نمایش</summary>
    private void OnDataContextChangedAsync(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ComplementaryViewModel old)
            old.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is ComplementaryViewModel vm)
            vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>نمایش HTML جدید در مرورگر</summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ComplementaryViewModel.PreviewHtml)) return;
        if (sender is not ComplementaryViewModel vm) return;

        Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrEmpty(vm.PreviewHtml))
            {
                PreviewBrowser.Visibility = Visibility.Collapsed;
                return;
            }
            PreviewBrowser.Visibility = Visibility.Visible;
            PreviewBrowser.NavigateToString(vm.PreviewHtml);
        });
    }
}
