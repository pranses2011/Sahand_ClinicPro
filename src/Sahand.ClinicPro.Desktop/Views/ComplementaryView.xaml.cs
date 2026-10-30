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

    private void OnFreeTemplateEditor(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ComplementaryViewModel vm || !vm.IsTemplateReady || vm.SelectedTemplate is not { IsHtmlTemplate: true } template)
        { AppDialog.Error("طراحی قالب", "ابتدا یک قالب قابل ویرایش را انتخاب و بارگذاری کنید."); return; }
        var before = template.Body;
        var beforePaper = template.PaperSize;
        var beforeOrientation = template.Orientation;
        if (!string.Equals(beforeOrientation, "Portrait", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(beforeOrientation, "Landscape", StringComparison.OrdinalIgnoreCase))
        { AppDialog.Error("طراحی قالب", "جهت ذخیره‌شده این قالب نامعتبر است؛ برای جلوگیری از بازنویسی با پشتیبانی بررسی کنید."); return; }
        var variables = vm.ActiveVariables.ToArray();
        var keys = variables.Select(v => v.Key).ToArray();
        if (!FreeTemplateDesign.TryRead(before, keys, out var document))
        {
            if (AppDialog.Confirm("شروع طراح آزاد", "این قالب با طراح آزاد ساخته نشده یا تغییر دستی داشته است. اعمال طرح جدید بدنه فعلی را جایگزین می‌کند؛ تا زمان ذخیره نسخه سرور تغییر نمی‌کند. ادامه می‌دهید؟", AppDialogIcon.Warning) != true) return;
            var width = Math.Min(600, FreeTemplateDesign.AvailableBodyWidth(beforePaper, beforeOrientation));
            if (width < 100) { AppDialog.Error("طراحی قالب", "کاغذ برای بوم آزاد بیش از حد باریک است؛ ابتدا اندازه کاغذ را تغییر دهید."); return; }
            document = new FreeTemplateDocument { Width = width, Elements = new() { new FreeTemplateElement { Kind = "text", Text = "عنوان سند", X = 0, Y = 20, Width = Math.Min(200, width) } } };
        }
        var window = new FreeTemplateEditorWindow(document!, variables, beforePaper, beforeOrientation) { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() != true || window.ResultHtml is null || window.ResultPaperSize is null || window.ResultOrientation is null) return;
        if (!ReferenceEquals(vm.SelectedTemplate, template) || template.Body != before || template.PaperSize != beforePaper || template.Orientation != beforeOrientation || !vm.IsTemplateReady)
        { AppDialog.Error("طراحی قالب", "قالب هنگام ویرایش تغییر کرد؛ طرح اعمال نشد."); return; }
        template.Body = window.ResultHtml;
        template.PaperSize = window.ResultPaperSize;
        template.Orientation = window.ResultOrientation;
    }

    private void OnVisualTemplateEditor(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ComplementaryViewModel vm || !vm.IsTemplateReady || vm.SelectedTemplate is not { IsHtmlTemplate: true } template)
        { AppDialog.Error("طراحی قالب", "ابتدا یک قالب قابل ویرایش را انتخاب و بارگذاری کنید."); return; }
        var before = template.Body;
        var beforePaper = template.PaperSize;
        var variables = vm.ActiveVariables.ToArray();
        var keys = variables.Select(v => v.Key).ToArray();
        if (!VisualTemplateDesign.TryRead(before, keys, out var rows))
        {
            if (AppDialog.Confirm("شروع طراحی بصری", "این قالب قبلاً با طراح بصری ساخته نشده است. با اعمال طرح جدید، بدنه فعلی جایگزین می‌شود؛ تا زمان ذخیره، نسخه سرور تغییر نمی‌کند. ادامه می‌دهید؟", AppDialogIcon.Warning) != true) return;
            rows = new() { new VisualTemplateBlock { Kind = "text", Text = "عنوان سند" } };
        }
        var window = new VisualTemplateEditorWindow(rows, variables, beforePaper) { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() != true || window.ResultHtml is null || window.ResultPaperSize is null) return;
        if (!ReferenceEquals(vm.SelectedTemplate, template) || template.Body != before || template.PaperSize != beforePaper || !vm.IsTemplateReady)
        { AppDialog.Error("طراحی قالب", "قالب هنگام ویرایش تغییر کرد؛ طرح اعمال نشد."); return; }
        template.Body = window.ResultHtml;
        template.PaperSize = window.ResultPaperSize;
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
