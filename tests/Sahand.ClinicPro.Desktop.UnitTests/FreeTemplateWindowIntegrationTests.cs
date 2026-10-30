using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Sahand.ClinicPro.Desktop.Views;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

[CollectionDefinition("Isolated WPF application", DisableParallelization = true)]
public sealed class IsolatedWpfCollection { }

/// <summary>آزمون کنترل‌های واقعی WPF؛ بدون شبیه‌سازی ماوس یا چاپگر.</summary>
[Collection("Isolated WPF application")]
public class FreeTemplateWindowIntegrationTests
{
    [Fact]
    public void Color_preset_updates_selected_element_only_after_apply()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { CheckInSta(); }
            catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(30)).Should().BeTrue("آزمون پنجره باید در زمان محدود تمام شود");
        if (failure is not null) throw new InvalidOperationException("آزمون کنترل‌های طراح ناموفق بود.", failure);
    }

    private static void CheckInSta()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        // فقط استایل‌های پایه لازم برای پنجره؛ وابسته به تم نصب‌شده کاربر نیست.
        app.Resources.Add(typeof(TextBox), new Style(typeof(TextBox)));
        app.Resources.Add(typeof(ComboBox), new Style(typeof(ComboBox)));
        var source = new FreeTemplateDocument { Elements = new() { new() { Text = "نمونه" } } };
        var window = new FreeTemplateEditorWindow(source, Array.Empty<ComplementaryViewModel.TemplateVariable>(), "A4", "Portrait");
        try
        {
            var canvas = (Canvas)window.FindName("PageCanvas");
            canvas.Children.OfType<Thumb>().Should().HaveCount(8);
            var field = (TextBox)window.FindName("ColorBox");
            var red = (Button)window.FindName("RedPresetButton");
            var apply = (Button)window.FindName("ApplyPropertiesButton");
            var current = (FreeTemplateDocument)typeof(FreeTemplateEditorWindow)
                .GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            red.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            field.Text.Should().Be("#EB4C53");
            current.Elements[0].Color.Should().Be("#173657");
            apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            current.Elements[0].Color.Should().Be("#EB4C53");
            source.Elements[0].Color.Should().Be("#173657", "ویرایش تا اعمال نهایی نسخه اصلی را تغییر نمی‌دهد");
            // بازگرداندن وضعیت اولیه تا بستن پنجره، تأیید تعاملی نخواهد.
            field.Text = "#173657";
            apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        finally
        {
            window.Close();
            app.Shutdown();
            Dispatcher.CurrentDispatcher.InvokeShutdown();
        }
    }
}
