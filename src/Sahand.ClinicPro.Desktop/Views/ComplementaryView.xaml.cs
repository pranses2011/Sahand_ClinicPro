using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.ViewModels;
using Sahand.ClinicPro.Desktop.Services;

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
        // انتخابگر «نوع قالب جدید» — جفت‌مرتبه‌های (کلید، فارسی) از ViewModel
        TypeCombo.ItemsSource = ComplementaryViewModel.TemplateTypes;
        // چرخهٔ سی‌وپنجم: انتخابگر «فیلتر موجودیت» — «همه» + ۹ نوع سند؛
        // آیتم‌ها از TemplateFilterOptions می‌آیند تا با فهرست نوع‌ها همیشه همگام بماند
        TemplateFilterCombo.ItemsSource = ComplementaryViewModel.TemplateFilterOptions;
        DataContextChanged += OnDataContextChangedAsync;
    }

    /// <summary>اشتراک در تغییر HTML پیش‌نمایش</summary>
    private void OnDataContextChangedAsync(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ComplementaryViewModel old)
        {
            old.PropertyChanged -= OnViewModelPropertyChanged;
            old.OriginalReceiptPreviewRequested -= OnOriginalReceiptPreviewRequested;
        }
        if (e.NewValue is ComplementaryViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            vm.OriginalReceiptPreviewRequested += OnOriginalReceiptPreviewRequested;
        }
    }

    private void OnOriginalReceiptPreviewRequested()
    {
        var preview = new PrintPreviewWindow(ReceptionReceiptPrinter.CreateSamplePreviewDocument(),
            "قبض اصلی — داده نمونه، فاقد اعتبار", null, allowPrint: false);
        var owner = Window.GetWindow(this);
        if (owner is not null) preview.Owner = owner;
        preview.ShowDialog();
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
                PreviewPlaceholder.Visibility = Visibility.Visible;
                PreviewBrowser.Visibility = Visibility.Collapsed;
                return;
            }
            PreviewPlaceholder.Visibility = Visibility.Collapsed;
            PreviewBrowser.Visibility = Visibility.Visible;
            PreviewBrowser.NavigateToString(vm.PreviewHtml);
        });
    }
}
