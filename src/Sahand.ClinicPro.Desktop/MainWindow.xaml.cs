using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop;

/// <summary>
/// کد پشت پنجره اصلی — اتصال به ViewModel تزریق‌شده
/// ═══ چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک) ═══
/// «برای خروج از برنامه تاییدیه بگیره و در اون تاییدیه چک باکس پشتیبان‌گیری
/// قبل از خروج هم باشه»: رویداد Closing پنجرهٔ اصلی (دکمهٔ X یا Alt+F4)
/// ابتدا لغو می‌شود؛ دیالوگ فارسی خود برنامه با چک‌باکس پشتیبان‌گیری نمایش
/// داده می‌شود (منطق در MainWindowViewModel.ConfirmExitAsync) و فقط پس از
/// تأیید کاربر، بستن مجاز می‌گردد. مسیر «خروج از حساب → صفحهٔ ورود»
/// (لاگ‌اوت/انقضای نشست) از CloseForLoginSwitch می‌گذرد و تأییدیهٔ خروج
/// نمی‌گیرد — آن مسیر خروج از برنامه نیست.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>پرچم بستن مجاز — پس از تأیید کاربر یا در مسیر تعویض نشست</summary>
    private bool _closeAllowed;

    /// <summary>سازنده با تزریق ViewModel پوسته</summary>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // تأیید خروج از برنامه — دکمهٔ X / Alt+F4 / Close از هر جای دیگر
        Closing += async (_, ce) =>
        {
            if (_closeAllowed)
                return;

            // بستن را فعلاً لغو می‌کنیم؛ پس از تأیید کاربر دوباره (مجاز) بسته می‌شود
            ce.Cancel = true;

            if (DataContext is not MainWindowViewModel vm)
            {
                // بدون ViewModel منطق تأیید نداریم — بستن مستقیم
                _closeAllowed = true;
                Close();
                return;
            }

            var proceed = await vm.ConfirmExitAsync();
            if (proceed)
            {
                _closeAllowed = true;
                Close();
            }
            // انصراف کاربر → پنجره باز می‌ماند (Cancel همین حالا فعال است)
        };
    }

    /// <summary>
    /// بستن بی‌تأیید برای تعویض نشست — بازگشت به صفحهٔ ورود (لاگ‌اوت یا
    /// انقضای توکن). این مسیر «خروج از برنامه» نیست؛ کاربر به صفحهٔ ورود
    /// می‌رود و تأییدیه/پشتیبان‌گیری خروج نباید پرسیده شود.
    /// </summary>
    public void CloseForLoginSwitch()
    {
        _closeAllowed = true;
        Close();
    }
}
