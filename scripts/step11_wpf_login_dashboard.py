# -*- coding: utf-8 -*-
"""گام ۱۱ — WPF Login + Dashboard + کنترلر داشبورد API"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
DESK = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Desktop"))
API = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.API"))


def write(rel, content, base=DESK):
    full = os.path.join(base, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ کنترلر داشبورد (سمت API) ═══════════
write("Controllers/DashboardController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر داشبورد — آمار روزانه برای صفحه آغازین کلاینت دسکتاپ
/// مسیرها: /api/v1/dashboard
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public DashboardController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// آمار خلاصه امروز — بیماران ثبت‌شده، نوبت‌ها، ویزیت‌ها و درآمد
    /// </summary>
    [HttpGet("stats")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetTodayStats(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var newPatients = await _unitOfWork.Repository<Patient>()
            .QueryableNoTracking
            .CountAsync(p => p.RegistrationDate != null
                          && DateOnly.FromDateTime(p.RegistrationDate.Value) == today, ct);

        var todayAppointments = await _unitOfWork.Repository<Appointment>()
            .QueryableNoTracking
            .CountAsync(a => a.AppointmentDate == today, ct);

        var activeAppointments = await _unitOfWork.Repository<Appointment>()
            .QueryableNoTracking
            .CountAsync(a => a.AppointmentDate == today
                          && (a.Status == AppointmentStatus.Booked
                              || a.Status == AppointmentStatus.Confirmed
                              || a.Status == AppointmentStatus.Arrived), ct);

        var todayRevenue = await _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking
            .Where(i => DateOnly.FromDateTime(i.InvoiceDate) == today
                     && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)i.PaidAmount, ct) ?? 0m;

        var todayVisits = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>()
            .QueryableNoTracking
            .CountAsync(v => DateOnly.FromDateTime(v.VisitDate) == today, ct);

        var stats = new
        {
            newPatients,
            todayAppointments,
            activeAppointments,
            todayVisits,
            todayRevenue,
            todayDateShamsi = Application.Common.Helpers.PersianDateHelper.ToShamsi(DateTime.UtcNow),
            serverTime = DateTime.UtcNow
        };

        return Ok(Application.Common.Models.ApiResponse<object>.Success(stats,
            "آمار امروز دریافت شد"));
    }
}
""", API)

# ═══════════ پنجره ورود ═══════════
write("Views/LoginWindow.xaml", """
<Window x:Class="Sahand.ClinicPro.Desktop.Views.LoginWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        Title="ورود به سامانه درمانگاه سهند"
        Height="560" Width="440"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize"
        TextElement.Foreground="{DynamicResource MaterialDesign.Brush.Foreground}"
        Background="{DynamicResource MaterialDesign.Brush.Background}"
        FontFamily="Vazirmatn, Segoe UI, Tahoma">
    <!-- ═══════════════════════════════════════════════════════════
         پنجره ورود — گام ۱۱
         کارت ورود با نام کاربری و رمز عبور و پیام خطای فارسی
         ═══════════════════════════════════════════════════════════ -->
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <StackPanel Grid.Row="0" VerticalAlignment="Center" Margin="40,0">
            <!-- نشان درمانگاه -->
            <materialDesign:PackIcon Kind="Hospital"
                                     Width="64" Height="64"
                                     HorizontalAlignment="Center"
                                     Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
            <TextBlock Text="سامانه جامع مدیریت درمانگاه سهند"
                       FontSize="20" FontWeight="Bold"
                       HorizontalAlignment="Center" Margin="0,16,0,4"
                       TextAlignment="Center" />
            <TextBlock Text="برای ورود نام کاربری و رمز عبور خود را وارد کنید"
                       FontSize="13" Opacity="0.7"
                       HorizontalAlignment="Center" Margin="0,0,0,28" />

            <!-- کارت ورود -->
            <materialDesign:Card UniformCornerRadius="16" Padding="28">
                <StackPanel>
                    <!-- نام کاربری -->
                    <TextBox x:Name="UsernameBox"
                             materialDesign:HintAssist.Hint="نام کاربری یا ایمیل"
                             materialDesign:TextFieldAssist.HasClearButton="True"
                             Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}"
                             FontSize="15" Margin="0,0,0,16">
                        <TextBox.InputBindings>
                            <KeyBinding Key="Enter" Command="{Binding LoginCommand}" />
                        </TextBox.InputBindings>
                    </TextBox>

                    <!-- رمز عبور -->
                    <PasswordBox x:Name="PasswordBox"
                                 materialDesign:HintAssist.Hint="رمز عبور"
                                 materialDesign:PasswordBoxAssist.Password="{Binding Password, Mode=TwoWay}"
                                 FontSize="15" Margin="0,0,0,8">
                        <PasswordBox.InputBindings>
                            <KeyBinding Key="Enter" Command="{Binding LoginCommand}" />
                        </PasswordBox.InputBindings>
                    </PasswordBox>

                    <!-- پیام خطا -->
                    <TextBlock Text="{Binding ErrorMessage}"
                               Foreground="{DynamicResource MaterialDesign.Brush.Error}"
                               FontSize="12" TextWrapping="Wrap"
                               Margin="0,8,0,0"
                               Visibility="{Binding HasError, Converter={StaticResource BoolToVis}}" />

                    <!-- دکمه ورود -->
                    <Button Command="{Binding LoginCommand}"
                            Style="{StaticResource MaterialDesignFilledButton}"
                            Height="46" Margin="0,20,0,0"
                            Content="ورود به سامانه"
                            FontSize="15" />

                    <!-- نوار انتظار -->
                    <ProgressBar IsIndeterminate="True"
                                 Height="4" Margin="0,16,0,0"
                                 Visibility="{Binding IsBusy, Converter={StaticResource BoolToVis}}" />
                </StackPanel>
            </materialDesign:Card>
        </StackPanel>

        <TextBlock Grid.Row="1"
                   Text="نسخه ۱.۰.۰ — تیم توسعه سهند کلینیک‌پرو"
                   FontSize="11" Opacity="0.5"
                   HorizontalAlignment="Center" Margin="0,0,0,16" />
    </Grid>

    <Window.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVis" />
    </Window.Resources>
</Window>
""")

write("Views/LoginWindow.xaml.cs", """
using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجره ورود — اتصال ViewModel و انتقال رمز عبور امن
/// </summary>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    /// <summary>سازنده با تزریق ViewModel ورود</summary>
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // PasswordBox به‌صورت امن با VM همگام می‌شود (بدون دوطرفه‌سازی XAML)
        PasswordBox.PasswordChanged += (_, _) => viewModel.Password = PasswordBox.Password;
        viewModel.LoginSucceeded += OnLoginSucceeded;

        Loaded += (_, _) => UsernameBox.Focus();
    }

    /// <summary>پس از ورود موفق — باز کردن پنجره اصلی</summary>
    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        var main = App.Services.GetRequiredService(typeof(MainWindow)) as MainWindow;
        if (main is null) return;

        System.Windows.Application.Current.MainWindow = main;
        main.Show();
        Close();
    }
}
""")

write("ViewModels/LoginViewModel.cs", """
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel ورود — فراخوانی ورود API و مدیریت پیام خطا
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly IApiClient _api;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>رویداد ورود موفق — پنجره را عوض می‌کند</summary>
    public event EventHandler? LoginSucceeded;

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public LoginViewModel(IApiClient api) => _api = api;

    /// <summary>آیا پیام خطا نمایش داده شود</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>دکمه ورود</summary>
    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        LoginCommand.NotifyCanExecuteChanged();

        var ok = await _api.LoginAsync(Username.Trim(), Password);

        IsBusy = false;
        LoginCommand.NotifyCanExecuteChanged();

        if (ok)
        {
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ErrorMessage = _api.LastError ?? "ورود ناموفق بود";
            OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>امکان ورود — نام کاربری و رمز پر شده باشند</summary>
    private bool CanLogin() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrEmpty(Password);

    /// <summary>به‌روزرسانی امکان دکمه با تغییر فیلدها</summary>
    partial void OnUsernameChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnPasswordChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
}
""")

# ═══════════ داشبورد ═══════════
write("Views/DashboardView.xaml", """
<UserControl x:Class="Sahand.ClinicPro.Desktop.Views.DashboardView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             FontFamily="Vazirmatn, Segoe UI, Tahoma">
    <!-- ═══════════════════════════════════════════════════════════
         داشبورد مدیریت — گام ۱۱
         چهار کارت آمار امروز + وضعیت سامانه
         ═══════════════════════════════════════════════════════════ -->
    <ScrollViewer VerticalScrollBarVisibility="Auto" Padding="24">
        <StackPanel>
            <TextBlock Text="وضعیت امروز درمانگاه"
                       Style="{StaticResource SectionTitle}" />

            <!-- چهار کارت آمار -->
            <UniformGrid Columns="4" Rows="1">
                <materialDesign:Card Style="{StaticResource StatCard}">
                    <StackPanel>
                        <materialDesign:PackIcon Kind="AccountPlus"
                                                 Width="36" Height="36"
                                                 Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                        <TextBlock Text="{Binding NewPatients}"
                                   FontSize="30" FontWeight="Bold" Margin="0,10,0,2" />
                        <TextBlock Text="بیمار جدید امروز" Opacity="0.7" />
                    </StackPanel>
                </materialDesign:Card>

                <materialDesign:Card Style="{StaticResource StatCard}">
                    <StackPanel>
                        <materialDesign:PackIcon Kind="CalendarClock"
                                                 Width="36" Height="36"
                                                 Foreground="{DynamicResource MaterialDesign.Brush.Secondary}" />
                        <TextBlock Text="{Binding TodayAppointments}"
                                   FontSize="30" FontWeight="Bold" Margin="0,10,0,2" />
                        <TextBlock Text="نوبت‌های امروز" Opacity="0.7" />
                    </StackPanel>
                </materialDesign:Card>

                <materialDesign:Card Style="{StaticResource StatCard}">
                    <StackPanel>
                        <materialDesign:PackIcon Kind="Stethoscope"
                                                 Width="36" Height="36"
                                                 Foreground="#43A047" />
                        <TextBlock Text="{Binding TodayVisits}"
                                   FontSize="30" FontWeight="Bold" Margin="0,10,0,2" />
                        <TextBlock Text="ویزیت‌های انجام‌شده" Opacity="0.7" />
                    </StackPanel>
                </materialDesign:Card>

                <materialDesign:Card Style="{StaticResource StatCard}">
                    <StackPanel>
                        <materialDesign:PackIcon Kind="CashMultiple"
                                                 Width="36" Height="36"
                                                 Foreground="#FB8C00" />
                        <TextBlock Text="{Binding TodayRevenueText}"
                                   FontSize="24" FontWeight="Bold" Margin="0,14,0,4" />
                        <TextBlock Text="درآمد امروز (ریال)" Opacity="0.7" />
                    </StackPanel>
                </materialDesign:Card>
            </UniformGrid>

            <!-- تاریخ سرور و وضعیت اتصال -->
            <materialDesign:Card UniformCornerRadius="14" Padding="20" Margin="6,16,6,6">
                <DockPanel>
                    <materialDesign:PackIcon Kind="Server" Width="26" Height="26"
                                             VerticalAlignment="Center" />
                    <StackPanel Margin="14,0,0,0" VerticalAlignment="Center">
                        <TextBlock FontSize="14" FontWeight="SemiBold">
                            <Run Text="تاریخ امروز: " />
                            <Run Text="{Binding TodayDateShamsi, Mode=OneWay}" />
                        </TextBlock>
                        <TextBlock FontSize="12" Opacity="0.7"
                                   Text="{Binding ConnectionStatus}" />
                    </StackPanel>
                </DockPanel>
            </materialDesign:Card>

            <!-- پیام خطا در نبود سرور -->
            <TextBlock Text="{Binding ErrorMessage}"
                       Foreground="{DynamicResource MaterialDesign.Brush.Error}"
                       TextWrapping="Wrap" Margin="6,12,6,0" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
""")

write("Views/DashboardView.xaml.cs", """
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت داشبورد
/// </summary>
public partial class DashboardView : UserControl
{
    /// <summary>سازنده پیش‌فرض برای XAML</summary>
    public DashboardView()
    {
        InitializeComponent();
    }
}
""")

write("ViewModels/DashboardViewModel.cs", """
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel داشبورد — بارگذاری آمار امروز از API
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IApiClient _api;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "داشبورد";

    [ObservableProperty] private int _newPatients;
    [ObservableProperty] private int _todayAppointments;
    [ObservableProperty] private int _todayVisits;
    [ObservableProperty] private decimal _todayRevenue;
    [ObservableProperty] private string _todayDateShamsi = string.Empty;
    [ObservableProperty] private string _connectionStatus = "در حال بررسی اتصال...";

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public DashboardViewModel(IApiClient api)
    {
        _api = api;
        _ = LoadStatsAsync();
    }

    /// <summary>درآمد به‌صورت متنی جدا شده با کاما</summary>
    public string TodayRevenueText => TodayRevenue.ToString("N0");

    /// <summary>بارگذاری آمار امروز</summary>
    public async Task LoadStatsAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var stats = await _api.GetAsync<DashboardStats>("dashboard/stats");

        if (stats is not null)
        {
            NewPatients = stats.NewPatients;
            TodayAppointments = stats.TodayAppointments;
            TodayVisits = stats.TodayVisits;
            TodayRevenue = stats.TodayRevenue;
            TodayDateShamsi = stats.TodayDateShamsi;
            ConnectionStatus = "اتصال به سرور برقرار است";
        }
        else
        {
            ConnectionStatus = "اتصال به سرور برقرار نیست";
            ErrorMessage = _api.LastError ?? "دریافت آمار ناموفق بود";
        }

        IsBusy = false;
        OnPropertyChanged(nameof(TodayRevenueText));
    }

    /// <summary>ساختار پاسخ آمار داشبورد — منطبق با API</summary>
    public sealed record DashboardStats(
        int NewPatients,
        int TodayAppointments,
        int ActiveAppointments,
        int TodayVisits,
        decimal TodayRevenue,
        string TodayDateShamsi);
}
""")

print("✅ گام ۱۱ — Login + Dashboard کامل شد")
