using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// دیالوگ پیام فارسی راست‌چین تابع تم — چرخه بیست‌ویکم (گزارش پانزدهم مالک).
/// جایگزین MessageBox پیش‌فرض ویندوز؛ ظاهر کاملاً هماهنگ با تم فعال
/// (رنگ‌ها و گوشه‌ها از منابع پویای برنامه) با آیکون بزرگ رنگی و دکمه‌های فارسی.
/// این کلاس فقط ظاهر است؛ API عمومی از <see cref="AppDialog"/> استفاده کنید.
/// </summary>
public partial class ThemedDialog : Window
{
    /// <summary>نتیجهٔ انتخاب کاربر</summary>
    private bool? _result;

    /// <summary>سازنده — قالب بار می‌شود؛ محتوا با ShowDialog تنظیم می‌شود</summary>
    public ThemedDialog()
    {
        InitializeComponent();
        KeyDown += (_, e) =>
        {
            // Enter = تأیید/بله؛ Escape = انصراف/خیر — میانبرهای کیبورد
            if (e.Key == Key.Escape)
            {
                _result = false;
                Close();
            }
        };
    }

    /// <summary>
    /// نمایش مودال دیالوگ — تمام موارد نمایش (آیکون/رنگ/دکمه‌ها) از همین متد تنظیم می‌شود.
    /// </summary>
    /// <param name="owner">پنجرهٔ مالک (برای مرکزچینی)</param>
    /// <param name="title">عنوان پیام</param>
    /// <param name="message">متن پیام — خط‌خوردگی با \n پشتیبانی می‌شود</param>
    /// <param name="icon">نوع آیکون و رنگ</param>
    /// <param name="buttons">ترکیب دکمه‌ها</param>
    /// <returns>true=تأیید/بله، false=خیر/انصراف، null=بستن پنجره</returns>
    public static bool? ShowDialog(Window? owner, string title, string message,
        AppDialogIcon icon, AppDialogButtons buttons)
    {
        var dialog = new ThemedDialog
        {
            Owner = owner ?? (System.Windows.Application.Current?.Windows
                .OfType<Window>().FirstOrDefault(w => w.IsActive)),
        };
        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;

        // ─── آیکون و رنگ دایرهٔ آیکون مطابق نوع پیام ───
        var (glyph, color) = icon switch
        {
            AppDialogIcon.Success => ("CheckCircle", Color.FromRgb(0x2E, 0x7D, 0x32)),
            AppDialogIcon.Warning => ("Alert", Color.FromRgb(0xF5, 0x7C, 0x00)),
            AppDialogIcon.Error => ("CloseCircle", Color.FromRgb(0xC6, 0x28, 0x28)),
            AppDialogIcon.Question => ("HelpCircle", Color.FromRgb(0x15, 0x65, 0xC0)),
            _ => ("Information", Color.FromRgb(0x15, 0x65, 0xC0)),
        };
        dialog.IconGlyph.Kind = (MaterialDesignThemes.Wpf.PackIconKind)System.Enum.Parse(
            typeof(MaterialDesignThemes.Wpf.PackIconKind), glyph);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        dialog.IconCircle.Background = brush;

        // ─── دکمه‌ها بر اساس ترکیب خواسته‌شده ───
        var panel = dialog.ButtonsPanel;
        panel.Children.Clear();

        void AddButton(string text, bool? value, bool isPrimary, string? background = null)
        {
            var btn = new Button
            {
                Content = text,
                MinWidth = 104,
                Height = 38,
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = Cursors.Hand,
                FontSize = 13,
                Style = (Style)dialog.FindResource(isPrimary
                    ? "MaterialDesignRaisedButton"
                    : "MaterialDesignOutlinedButton"),
            };
            if (background is not null && isPrimary)
            {
                var primary = new SolidColorBrush((Color)ColorConverter.ConvertFromString(background));
                primary.Freeze();
                btn.Background = primary;
                btn.BorderBrush = primary;
            }
            btn.Click += (_, _) =>
            {
                dialog._result = value;
                dialog.Close();
            };
            panel.Children.Add(btn);
        }

        switch (buttons)
        {
            case AppDialogButtons.Ok:
                AddButton("تأیید", true, isPrimary: true);
                break;
            case AppDialogButtons.YesNo:
                // بله (اصلی/رنگی) سپس خیر
                AddButton("بله", true, isPrimary: true, background: "#2E7D32");
                AddButton("خیر", false, isPrimary: false);
                break;
            case AppDialogButtons.YesNoCancel:
                AddButton("بله", true, isPrimary: true, background: "#2E7D32");
                AddButton("خیر", false, isPrimary: false);
                AddButton("انصراف", null, isPrimary: false);
                break;
        }

        dialog.Loaded += (_, _) =>
        {
            // اگر دکمهٔ اول فوکوس بگیرد Enter هم کار می‌کند
            if (panel.Children.Count > 0)
                ((Button)panel.Children[0]).Focus();
        };

        // بستن پنجره بدون انتخاب (ضربدر/Escape) → null
        dialog._result = null;
        dialog.ShowDialog();
        return dialog._result;
    }

    /// <summary>کلیک روی پوشش تیره = انصراف (رفتار استاندارد دیالوگ‌ها)</summary>
    private void OnDimClick(object sender, MouseButtonEventArgs e)
    {
        _result = null;
        Close();
    }
}

/// <summary>نوع آیکون دیالوگ — رنگ دایره هم مطابق همین ست می‌شود</summary>
public enum AppDialogIcon
{
    /// <summary>اطلاع‌رسانی — آبی</summary>
    Info,
    /// <summary>هشدار — نارنجی</summary>
    Warning,
    /// <summary>خطا — قرمز</summary>
    Error,
    /// <summary>موفقیت — سبز</summary>
    Success,
    /// <summary>پرسش — آبی</summary>
    Question,
}

/// <summary>ترکیب دکمه‌های دیالوگ</summary>
public enum AppDialogButtons
{
    /// <summary>فقط تأیید</summary>
    Ok,
    /// <summary>بله / خیر</summary>
    YesNo,
    /// <summary>بله / خیر / انصراف</summary>
    YesNoCancel,
}

/// <summary>
/// API عمومی دیالوگ‌های برنامه — چرخه بیست‌ویکم (گزارش پانزدهم مالک).
/// همهٔ فراخوانی‌های MessageBox ویندوز به این کلاس منتقل شده‌اند تا
/// کادر پیام فارسی، راست‌چین، آیکون‌دار و تابع تم باشد.
/// </summary>
public static class AppDialog
{
    /// <summary>دیالوگ اطلاع‌رسانی با دکمهٔ تأیید</summary>
    public static bool Info(string title, string message)
        => ShowDialog(null, title, message, AppDialogIcon.Info, AppDialogButtons.Ok) == true;

    /// <summary>دیالوگ موفقیت با دکمهٔ تأیید</summary>
    public static bool Success(string title, string message)
        => ShowDialog(null, title, message, AppDialogIcon.Success, AppDialogButtons.Ok) == true;

    /// <summary>دیالوگ هشدار با دکمهٔ تأیید</summary>
    public static bool Warn(string title, string message)
        => ShowDialog(null, title, message, AppDialogIcon.Warning, AppDialogButtons.Ok) == true;

    /// <summary>دیالوگ خطا با دکمهٔ تأیید</summary>
    public static bool Error(string title, string message)
        => ShowDialog(null, title, message, AppDialogIcon.Error, AppDialogButtons.Ok) == true;

    /// <summary>
    /// دیالوگ پرسش بله/خیر — بازگشت true یعنی «بله».
    /// هم‌ارزِ <c>MessageBox.Show(...) == MessageBoxResult.Yes</c> قدیم.
    /// </summary>
    public static bool Confirm(string title, string message, AppDialogIcon icon = AppDialogIcon.Question)
        => ThemedDialog.ShowDialog(null, title, message, icon, AppDialogButtons.YesNo) == true;
}
