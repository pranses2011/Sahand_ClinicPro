using System.Windows;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// پنجرهٔ چاپ قالبی — نمایش HTML رندرشده با قالب پیش‌فرض سرور و چاپ آن؛
/// وقتی برای سندی قالب فعال باشد، چاپ برنامه از همین مسیر می‌گذرد
/// (قرارداد html_first تأییدشدهٔ مالک) وگرنه چاپ داخلی FlowDocument.
/// </summary>
public partial class HtmlPrintWindow : Window
{
    private readonly string _html;

    /// <summary>ساخت پنجره با HTML آماده و عنوان سند</summary>
    public HtmlPrintWindow(string html, string title)
    {
        InitializeComponent();
        _html = html;
        CanvasWarning.Visibility = html.Contains("<!--clinicpro-canvas-v1:", StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;
        Title = title;
        TitleText.Text = title;
        Browser.NavigateToString(html);
    }

    private FixedCanvasPrintGuard.Result CheckFixedCanvas()
        => !_html.Contains("<!--clinicpro-canvas-v1:", StringComparison.Ordinal)
            ? FixedCanvasPrintGuard.Result.Clear : FixedCanvasPrintGuard.InspectBrowserDocument(Browser.Document);

    /// <summary>چاپ HTML — دیالوگ چاپ داخلی مرورگر روی همان سند</summary>
    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        var check = CheckFixedCanvas();
        if (check != FixedCanvasPrintGuard.Result.Clear)
        {
            AppDialog.Error("چاپ قالبی", check == FixedCanvasPrintGuard.Result.Overflow
                ? "بخشی از محتوای سند در کادر ثابت جا نمی‌شود و ممکن است حذف شود. چاپ متوقف شد؛ اندازه کادر یا قالب را اصلاح کنید."
                : "بررسی کادرهای قالب ممکن نیست یا صفحه هنوز کامل بارگذاری نشده است؛ چاپ متوقف شد. دوباره تلاش کنید یا قالب دیگری برگزینید.");
            return;
        }
        try
        {
            Browser.InvokeScript("execScript", new object[] { "window.print();", "JavaScript" });
        }
        catch
        {
            AppDialog.Error("چاپ قالبی", "باز کردن دیالوگ چاپ ناموفق بود؛ دوباره تلاش کنید.");
        }
    }

    /// <summary>بستن پنجره</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
