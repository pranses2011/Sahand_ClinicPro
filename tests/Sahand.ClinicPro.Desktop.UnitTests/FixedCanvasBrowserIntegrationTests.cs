using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FluentAssertions;
using Sahand.ClinicPro.Application.Features.Reporting.PrintTemplates;
using Sahand.ClinicPro.Desktop.Services;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

/// <summary>آزمون DOM واقعی مرورگر ویندوز؛ بدون اتصال چاپگر یا ارسال کار چاپ.</summary>
public class FixedCanvasBrowserIntegrationTests
{
    [Fact]
    public void Browser_reports_fit_and_overflow_for_rendered_canvas()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { CheckInSta(); }
            catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(50)).Should().BeTrue("مرورگر باید در زمان محدود بارگذاری شود");
        if (failure is not null) throw new InvalidOperationException("آزمون مرورگر داخلی ناموفق بود.", failure);
    }

    private static void CheckInSta()
    {
        var browser = new WebBrowser();
        var window = new Window { Content = browser, Width = 900, Height = 700, ShowInTaskbar = false, ShowActivated = false, Left = -10000, Top = -10000 };
        try
        {
            window.Show();
            var doc = new FreeTemplateDocument { Width = 500, Height = 250, Elements = new() {
                new() { Kind = "variable", Key = "patient_name", Text = "بیمار:", X = 10, Y = 10, Width = 140, Height = 30 }
            } };
            var template = FreeTemplateDesign.Build(doc, new[] { "patient_name" });
            var shortHtml = Page(template, "Ali");
            Navigate(browser, shortHtml);
            FixedCanvasPrintGuard.InspectBrowserDocument(browser.Document).Should().Be(FixedCanvasPrintGuard.Result.Clear);
            var longHtml = Page(template, new string('A', 800));
            Navigate(browser, longHtml);
            FixedCanvasPrintGuard.InspectBrowserDocument(browser.Document).Should().Be(FixedCanvasPrintGuard.Result.Overflow);

            var tableDoc = new FreeTemplateDocument { Width = 500, Height = 250, Elements = new() {
                new() { Kind = "variable", Key = "items_html", X = 10, Y = 10, Width = 220, Height = 45 }
            } };
            var tableTemplate = FreeTemplateDesign.Build(tableDoc, new[] { "items_html" });
            var rows = string.Concat(Enumerable.Repeat("<tr><td>Test item</td><td>123</td></tr>", 20));
            var tableHtml = PrintTemplateRenderer.Render(tableTemplate, new Dictionary<string, object?> { ["items_html"] = "<table>" + rows + "</table>" });
            Navigate(browser, PrintTemplateRenderer.WrapDocument(tableHtml, "Clinic", null, null, null, null, 13, null, false, "A4"));
            FixedCanvasPrintGuard.InspectBrowserDocument(browser.Document).Should().Be(FixedCanvasPrintGuard.Result.Overflow);
        }
        finally { window.Close(); browser.Dispose(); Dispatcher.CurrentDispatcher.InvokeShutdown(); }
    }

    private static string Page(string template, string value)
        => PrintTemplateRenderer.WrapDocument(PrintTemplateRenderer.Render(template, new Dictionary<string, object?> { ["patient_name"] = value }),
            "Clinic", null, null, null, null, 13, null, false, "A4");

    private static void Navigate(WebBrowser browser, string html)
    {
        var frame = new DispatcherFrame();
        var loaded = false;
        System.Windows.Navigation.LoadCompletedEventHandler handler = (_, _) => { loaded = true; frame.Continue = false; };
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        browser.LoadCompleted += handler;
        try
        {
            timer.Start();
            browser.NavigateToString(html);
            Dispatcher.PushFrame(frame);
            loaded.Should().BeTrue("صفحه باید در زمان محدود بارگذاری شود");
        }
        finally { timer.Stop(); browser.LoadCompleted -= handler; }
    }
}
