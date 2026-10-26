using System.Windows;

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
        Title = title;
        TitleText.Text = title;
        Browser.NavigateToString(html);
    }

    /// <summary>چاپ HTML — دیالوگ چاپ داخلی مرورگر روی همان سند</summary>
    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
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
