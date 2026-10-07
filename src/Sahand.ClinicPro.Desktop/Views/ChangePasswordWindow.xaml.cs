using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجره تغییر رمز عبور الزامی — چرخه چهاردهم (گزارش پنجم مالک).
/// این پنجره دورزدنی‌ناپذیر است: بستن بدون ثبت موفق یعنی خروج از برنامه؛
/// توکن ورود نیز پاک می‌شود تا با اجرای دوباره، دوباره ورود و دوباره الزام
/// تغییر رمز مطالبه شود (راهنمای SQL قدم ۷ همین را قول داده بود).
/// </summary>
public partial class ChangePasswordWindow : Window
{
    private readonly ITokenManager _tokens;
    private bool _changeCompleted;

    /// <summary>ViewModel تزریق‌شده — برای مقداردهی اولیه از سمت پنجره ورود</summary>
    public ChangePasswordViewModel ViewModel { get; }

    /// <summary>
    /// سازنده با تزریق ViewModel و مدیریت توکن
    /// </summary>
    public ChangePasswordWindow(ChangePasswordViewModel viewModel, ITokenManager tokens)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _tokens = tokens;
        DataContext = viewModel;
        // چرخه هفدهم (گزارش یازدهم مالک): همگام‌سازی تک‌مسیری کادرهای ماسک —
        // پس از حذف بایندینگ PasswordBoxAssist (ریشهٔ وارونگی رمز)، مقدار هر سه
        // کادر فقط از همین رویداد به ViewModel می‌رسد؛ هیچ مسیری به داخل کادرِ
        // در حال تایپ بازنویسی نمی‌کند تا مکان‌نما هرگز به ابتدای رشته نپرد
        CurrentPasswordBox.PasswordChanged += (_, _) => viewModel.CurrentPassword = CurrentPasswordBox.Password;
        NewPasswordBox.PasswordChanged += (_, _) => viewModel.NewPassword = NewPasswordBox.Password;
        ConfirmPasswordBox.PasswordChanged += (_, _) => viewModel.ConfirmPassword = ConfirmPasswordBox.Password;

        viewModel.ChangeSucceeded += OnChangeSucceeded;

        // رمز فعلی از قبل پر است — فوکوس روی رمز جدید
        Loaded += (_, _) => NewPasswordBox.Focus();
    }

    /// <summary>
    /// مقداردهی اولیه از پنجره ورود — نام کاربری برای سربرگ و رمز فعلیِ همان لحظه.
    /// چرخه هفدهم (گزارش یازدهم): کادر ماسک هم مثل گذشته نقش‌های رمز پیش‌پر را نشان
    /// می‌دهد؛ پس از مقداردهی برنامه‌ای، SelectAll تا اگر کاربر تایپ کند «همه‌گزینی
    /// جایگزین» شود نه درج در ابتدای رشته (مکان‌نمای کادر پس از مقداردهی برنامه‌ای
    /// در ابتداست — درج آنجا یعنی وارونگی همان‌گونه که در پنجره ورود دیده می‌شد)
    /// </summary>
    public void Initialize(string username, string currentPassword)
    {
        ViewModel.Username = username;
        ViewModel.CurrentPassword = currentPassword;
        CurrentPasswordBox.Password = currentPassword;
        CurrentPasswordBox.SelectAll();
        NewPasswordBox.Focus();
    }

    /// <summary>کشیدن پنجره بدون نوار عنوان — دکمه ماوس روی پس‌زمینه</summary>
    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    /// <summary>
    /// دکمه بستن — چون تغییر رمز الزامی است، بستن یعنی خروج از برنامه
    /// </summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// نمایش یا مخفی‌کردن رمز فعلی/جدید/تکرار — جابه‌جایی PasswordBox و TextBox بر اساس Tag.
    /// گزارش هفتم مالک: چشمِ «رمز فعلی» نیز اضافه شد — هر سه کادر همین الگو را دارند.
    /// چرخه هفدهم (گزارش یازدهم): هنگام مخفی‌شدن، کادر ماسک با آخرین مقدار ViewModel
    /// همگام می‌شود تا ویرایش‌های کادر آشکار از بین نرود؛ SelectAll پس از مقداردهی
    /// برنامه‌ای، تایپِ بعدی را «جایگزینِ همه‌گزینی» می‌کند نه درج در ابتدای رشته
    /// </summary>
    private void OnEyeToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Primitives.ToggleButton toggle) return;
        var show = toggle.IsChecked == true;

        if ((string?)toggle.Tag == "Current")
        {
            CurrentPasswordRevealBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            CurrentPasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            if (show)
            {
                CurrentPasswordRevealBox.CaretIndex = CurrentPasswordRevealBox.Text.Length;
                CurrentPasswordRevealBox.Focus();
            }
            else SyncMaskedBox(CurrentPasswordBox, ViewModel.CurrentPassword);
        }
        else if ((string?)toggle.Tag == "New")
        {
            NewPasswordRevealBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            NewPasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            if (show)
            {
                NewPasswordRevealBox.CaretIndex = NewPasswordRevealBox.Text.Length;
                NewPasswordRevealBox.Focus();
            }
            else SyncMaskedBox(NewPasswordBox, ViewModel.NewPassword);
        }
        else if ((string?)toggle.Tag == "Confirm")
        {
            ConfirmPasswordRevealBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ConfirmPasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            if (show)
            {
                ConfirmPasswordRevealBox.CaretIndex = ConfirmPasswordRevealBox.Text.Length;
                ConfirmPasswordRevealBox.Focus();
            }
            else SyncMaskedBox(ConfirmPasswordBox, ViewModel.ConfirmPassword);
        }
    }

    /// <summary>
    /// همگام‌سازی امن کادر ماسک با مقدار ViewModel — فقط وقتی متفاوت باشد؛
    /// پس از نوشتن برنامه‌ای، همه‌گزینی تا تایپ بعدی کل مقدار را جایگزین کند
    /// </summary>
    private static void SyncMaskedBox(System.Windows.Controls.PasswordBox box, string vmValue)
    {
        if (box.Password != vmValue)
        {
            box.Password = vmValue;
            box.SelectAll();
        }
    }

    /// <summary>
    /// پس از تغییر موفق — باز شدن پنجره اصلی و بستن این پنجره
    /// </summary>
    private void OnChangeSucceeded(object? sender, EventArgs e)
    {
        _changeCompleted = true;

        var main = App.Services.GetRequiredService(typeof(MainWindow)) as MainWindow;
        if (main is null) return;

        System.Windows.Application.Current.MainWindow = main;
        main.Show();
        Close();
    }

    /// <summary>
    /// بسته‌شدن پنجره — اگر رمز عوض نشده باشد توکن پاک و برنامه بسته می‌شود
    /// تا الزامِ تغییر رمز با اجرای بعدی دوباره مطالبه شود (دورزدنی‌ناپذیر)
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        if (!_changeCompleted)
        {
            _tokens.Clear();
            System.Windows.Application.Current.Shutdown();
        }
        base.OnClosed(e);
    }
}
