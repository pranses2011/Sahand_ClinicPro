# -*- coding: utf-8 -*-
"""گام ۱۰ — بخش ۲: App.xaml کامل (DI + فونت) + MainWindow پوسته ناوبری + ViewModelBase"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
DESK = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Desktop"))


def write(rel, content):
    full = os.path.join(DESK, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ App.xaml — تم و فونت ═══════════
write("App.xaml", """
<Application x:Class="Sahand.ClinicPro.Desktop.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:local="clr-namespace:Sahand.ClinicPro.Desktop"
             ShutdownMode="OnMainWindowClose">
    <!-- ═══════════════════════════════════════════════════════════
         نقطه ورود کلاینت دسکتاپ درمانگاه سهند — گام ۱۰
         • تم Material Design 3 (روشن/تیره با ThemeService)
         • رنگ سازمانی: فیروزه‌ای + نیلی
         • فونت وزیرمتن با جایگزین Segoe UI/Tahoma
         • ناوبری و سرویس‌ها در App.xaml.cs ثبت می‌شوند
         ═══════════════════════════════════════════════════════════ -->
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <materialDesign:BundledTheme BaseTheme="Light"
                                             PrimaryColor="Teal"
                                             SecondaryColor="Indigo" />
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign3.Defaults.xaml" />
            </ResourceDictionary.MergedDictionaries>

            <!-- فونت سراسری — وزیرمتن در گام انتشار کنار برنامه قرار می‌گیرد -->
            <Style TargetType="Window">
                <Setter Property="FontFamily" Value="Vazirmatn, Segoe UI, Tahoma" />
                <Setter Property="FlowDirection" Value="RightToLeft" />
                <Setter Property="TextOptions.TextFormattingMode" Value="Ideal" />
            </Style>

            <!-- سبک متن عنوان بخش‌ها -->
            <Style x:Key="SectionTitle" TargetType="TextBlock">
                <Setter Property="FontSize" Value="20" />
                <Setter Property="FontWeight" Value="Bold" />
                <Setter Property="Margin" Value="0,4,0,12" />
                <Setter Property="Foreground" Value="{DynamicResource MaterialDesign.Brush.Primary}" />
            </Style>

            <!-- سبک کارت آمار داشبورد -->
            <Style x:Key="StatCard" TargetType="materialDesign:Card">
                <Setter Property="Padding" Value="20" />
                <Setter Property="Margin" Value="6" />
                <Setter Property="UniformCornerRadius" Value="14" />
            </Style>
        </ResourceDictionary>
    </Application.Resources>
</Application>
""")

# ═══════════ App.xaml.cs — DI و راه‌اندازی ═══════════
write("App.xaml.cs", """
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop;

/// <summary>
/// کلاس برنامه کلاینت دسکتاپ درمانگاه سهند — راه‌اندازی DI و انتخاب پنجره آغازین
/// اگر توکن معتبر ذخیره‌شده باشد مستقیم پنجره اصلی و در غیر این صورت پنجره ورود باز می‌شود.
/// </summary>
/// <remarks>
/// نکته فنی: نام پایه به‌صورت کامل «System.Windows.Application» نوشته شده است زیرا
/// فضای‌نام Sahand.ClinicPro.Application (لایه کاربرد) با نام کلاس Application در WPF تداخل دارد.
/// </remarks>
public partial class App : System.Windows.Application
{
    /// <summary>ارائه‌دهنده سرویس سراسری برنامه دسکتاپ</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>رویداد راه‌اندازی — ساخت کانتینر DI و انتخاب پنجره آغازین</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // پیکربندی — appsettings.json کنار برنامه یا پوشه خروجی
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        Services = services.BuildServiceProvider();

        // انتخاب پنجره آغازین بر اساس وضعیت توکن
        var tokens = Services.GetRequiredService<ITokenManager>();
        if (tokens.HasValidToken)
        {
            var main = Services.GetRequiredService<MainWindow>();
            MainWindow = main;
            main.Show();
        }
        else
        {
            var login = Services.GetRequiredService<LoginWindow>();
            MainWindow = login;
            login.Show();
        }
    }

    /// <summary>ثبت سرویس‌ها و پنجره‌ها در کانتینر</summary>
    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // پیکربندی
        services.AddSingleton<IConfiguration>(configuration);

        // سرویس‌های زیرساخت کلاینت
        services.AddSingleton<ITokenManager, TokenManager>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // HttpClient سفارشی کلاینت API
        services.AddHttpClient<ApiClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
            {
                // گواهی خودامضای محیط توسعه
                ServerCertificateCustomValidationCallback =
                    System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        services.AddSingleton<IApiClient>(sp =>
            sp.GetRequiredService<ApiClient>());

        // پنجره‌ها و ViewModelها
        services.AddSingleton<LoginWindow>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<LoginViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PatientsViewModel>();
    }
}
""")

# ═══════════ ViewModelBase ═══════════
write("ViewModels/ViewModelBase.cs", """
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// کلاس پایه همه ViewModelها — ObservableObject با پرچم بارگذاری
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    /// <summary>آیا عملیات در جریان است — برای غیرفعال‌کردن دکمه‌ها</summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>پیام خطای فارسی برای نمایش در UI</summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>عنوان صفحه — در هدر پوسته نمایش داده می‌شود</summary>
    public abstract string Title { get; }
}
""")

# ═══════════ MainWindow — پوسته ناوبری ═══════════
write("MainWindow.xaml", """
<Window x:Class="Sahand.ClinicPro.Desktop.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        xmlns:views="clr-namespace:Sahand.ClinicPro.Desktop.Views"
        mc:Ignorable="d"
        Title="سامانه جامع مدیریت درمانگاه سهند"
        Height="780" Width="1280" MinHeight="620" MinWidth="1000"
        WindowStartupLocation="CenterScreen"
        TextElement.Foreground="{DynamicResource MaterialDesign.Brush.Foreground}"
        Background="{DynamicResource MaterialDesign.Brush.Background}"
        FontFamily="Vazirmatn, Segoe UI, Tahoma">
    <!-- ═══════════════════════════════════════════════════════════
         پوسته اصلی — گام ۱۰
         ساختار: ستون کنار (ناوبری) + هدر (کاربر/تم/خروج) + میزکار
         همه صفحات در ContentControl میانی با DataTemplate نگاشت می‌شوند
         ═══════════════════════════════════════════════════════════ -->
    <Grid>
        <Grid.ColumnDefinitions>
            <!-- ستون کنار راست — ناوبری (RTL) -->
            <ColumnDefinition Width="240" />
            <!-- میزکار اصلی -->
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <!-- ═══ ستون کنار ═══ -->
        <Border Grid.Column="0"
                Background="{DynamicResource MaterialDesign.Brush.Primary.Container}">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                    <RowDefinition Height="Auto" />
                </Grid.RowDefinitions>

                <!-- نام سامانه -->
                <StackPanel Grid.Row="0" Margin="16,20,16,16">
                    <materialDesign:PackIcon Kind="Hospital"
                                             Width="40" Height="40"
                                             Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                    <TextBlock Text="درمانگاه سهند"
                               FontSize="20" FontWeight="Bold" Margin="0,10,0,0"
                               Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                    <TextBlock Text="سامانه جامع مدیریت"
                               FontSize="12" Opacity="0.7"
                               Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                </StackPanel>

                <!-- فهرست ناوبری -->
                <ListBox Grid.Row="1"
                         ItemsSource="{Binding NavItems}"
                         SelectedItem="{Binding SelectedNav}"
                         Background="Transparent"
                         BorderThickness="0"
                         ScrollViewer.HorizontalScrollBarVisibility="Disabled">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal" Margin="8,10">
                                <materialDesign:PackIcon Kind="{Binding Icon}"
                                                         Width="22" Height="22"
                                                         VerticalAlignment="Center" />
                                <TextBlock Text="{Binding Title}"
                                           FontSize="14"
                                           Margin="10,0,0,0"
                                           VerticalAlignment="Center" />
                            </StackPanel>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>

                <!-- خروج -->
                <Button Grid.Row="2"
                        Command="{Binding LogoutCommand}"
                        Style="{StaticResource MaterialDesignOutlinedButton}"
                        Margin="16,8,16,16"
                        HorizontalAlignment="Stretch">
                    <StackPanel Orientation="Horizontal">
                        <materialDesign:PackIcon Kind="Logout" VerticalAlignment="Center" />
                        <TextBlock Text="خروج از حساب" Margin="8,0,0,0" VerticalAlignment="Center" />
                    </StackPanel>
                </Button>
            </Grid>
        </Border>

        <!-- ═══ میزکار ═══ -->
        <Grid Grid.Column="1">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>

            <!-- هدر — عنوان صفحه + کاربر + تم -->
            <Border Grid.Row="0"
                    Padding="24,12"
                    BorderBrush="{DynamicResource MaterialDesign.Brush.Background.Variant}"
                    BorderThickness="0,0,0,1">
                <DockPanel>
                    <StackPanel DockPanel.Dock="Left" Orientation="Horizontal">
                        <TextBlock Text="{Binding CurrentTitle}"
                                   FontSize="20" FontWeight="Bold"
                                   VerticalAlignment="Center" />
                    </StackPanel>

                    <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" HorizontalAlignment="Right">
                        <ToggleButton Command="{Binding ToggleThemeCommand}"
                                      Style="{StaticResource MaterialDesignFlatToggleButton}"
                                      ToolTip="تغییر تم روشن و تیره"
                                      Margin="0,0,16,0">
                            <materialDesign:PackIcon Kind="ThemeLightDark" />
                        </ToggleButton>

                        <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                            <materialDesign:PackIcon Kind="AccountCircle"
                                                     Width="28" Height="28"
                                                     VerticalAlignment="Center" />
                            <StackPanel Margin="8,0,0,0" VerticalAlignment="Center">
                                <TextBlock Text="{Binding CurrentUser}"
                                           FontWeight="SemiBold" />
                                <TextBlock Text="{Binding CurrentDate}"
                                           FontSize="11" Opacity="0.7" />
                            </StackPanel>
                        </StackPanel>
                    </StackPanel>
                </DockPanel>
            </Border>

            <!-- میزکار صفحات -->
            <ContentControl Grid.Row="1"
                            Content="{Binding CurrentPage}" />
        </Grid>
    </Grid>
</Window>
""")

write("MainWindow.xaml.cs", """
using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop;

/// <summary>
/// کد پشت پنجره اصلی — فقط اتصال به ViewModel تزریق‌شده
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>سازنده با تزریق ViewModel پوسته</summary>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
""")

print("✅ گام ۱۰ — بخش ۲ (App + پوسته اصلی) کامل شد")
