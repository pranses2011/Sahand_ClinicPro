using System.Windows;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>پیش‌نمایش محلی داده نمونه؛ این پنجره دکمه چاپ ندارد.</summary>
public partial class FreeTemplateSampleWindow : Window
{
    public FreeTemplateSampleWindow(string html)
    {
        InitializeComponent();
        Browser.NavigateToString(html);
        Closed += (_, _) => Browser.Dispose();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
