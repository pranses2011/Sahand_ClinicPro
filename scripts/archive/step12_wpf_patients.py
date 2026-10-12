# -*- coding: utf-8 -*-
"""گام ۱۲ — ماژول بیماران WPF: فهرست + فرم + جزئیات"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
DESK = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Desktop"))


def write(rel, content):
    full = os.path.join(DESK, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


write("Views/PatientsView.xaml", """
<UserControl x:Class="Sahand.ClinicPro.Desktop.Views.PatientsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             FontFamily="Vazirmatn, Segoe UI, Tahoma">
    <!-- ═══════════════════════════════════════════════════════════
         فهرست بیماران — گام ۱۲
         جستجو + جدول + صفحه‌بندی + دکمه بیمار جدید
         ═══════════════════════════════════════════════════════════ -->
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <!-- سربرگ: عنوان + دکمه بیمار جدید -->
        <DockPanel Grid.Row="0" Margin="0,0,0,12">
            <Button DockPanel.Dock="Right"
                    Command="{Binding AddPatientCommand}"
                    Style="{StaticResource MaterialDesignFilledButton}"
                    Content="بیمار جدید"
                    VerticalAlignment="Center">
                <Button.ContentTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal">
                            <materialDesign:PackIcon Kind="AccountPlus" VerticalAlignment="Center" />
                            <TextBlock Text="بیمار جدید" Margin="8,0,0,0" VerticalAlignment="Center" />
                        </StackPanel>
                    </DataTemplate>
                </Button.ContentTemplate>
            </Button>
            <TextBlock Text="مدیریت بیماران"
                       Style="{StaticResource SectionTitle}"
                       VerticalAlignment="Center" />
        </DockPanel>

        <!-- جستجو -->
        <TextBox Grid.Row="1"
                 materialDesign:HintAssist.Hint="جستجو: نام یا کد ملی یا موبایل یا شماره پرونده"
                 Text="{Binding SearchTerm, UpdateSourceTrigger=PropertyChanged}"
                 Margin="0,0,0,12">
            <TextBox.InputBindings>
                <KeyBinding Key="Enter" Command="{Binding SearchCommand}" />
            </TextBox.InputBindings>
        </TextBox>

        <!-- جدول بیماران -->
        <DataGrid Grid.Row="2"
                  ItemsSource="{Binding Patients}"
                  SelectedItem="{Binding SelectedPatient}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True"
                  SelectionMode="Single"
                  CanUserAddRows="False"
                  HeadersVisibility="Column"
                  GridLinesVisibility="Horizontal"
                  materialDesign:DataGridAssist.CellPadding="8,6">
            <DataGrid.Columns>
                <DataGridTextColumn Header="شماره پرونده" Binding="{Binding MedicalRecordNo}" Width="130" />
                <DataGridTextColumn Header="نام" Binding="{Binding FirstName}" Width="*" />
                <DataGridTextColumn Header="نام خانوادگی" Binding="{Binding LastName}" Width="*" />
                <DataGridTextColumn Header="کد ملی" Binding="{Binding NationalCode}" Width="110" />
                <DataGridTextColumn Header="موبایل" Binding="{Binding Mobile}" Width="120" />
                <DataGridTextColumn Header="بیمه" Binding="{Binding InsuranceName}" Width="130" />
            </DataGrid.Columns>
            <DataGrid.InputBindings>
                <MouseBinding MouseAction="LeftDoubleClick"
                              Command="{Binding ViewPatientCommand}" />
            </DataGrid.InputBindings>
        </DataGrid>

        <!-- صفحه‌بندی -->
        <DockPanel Grid.Row="3" Margin="0,12,0,0">
            <Button DockPanel.Dock="Right"
                    Command="{Binding NextPageCommand}"
                    Style="{StaticResource MaterialDesignFlatButton}"
                    Content="صفحه بعد" />
            <Button DockPanel.Dock="Right"
                    Command="{Binding PrevPageCommand}"
                    Style="{StaticResource MaterialDesignFlatButton}"
                    Content="صفحه قبل"
                    Margin="0,0,8,0" />
            <TextBlock VerticalAlignment="Center"
                       FontSize="13" Opacity="0.8">
                <Run Text="صفحه " />
                <Run Text="{Binding PageNumber, Mode=OneWay}" />
                <Run Text=" از " />
                <Run Text="{Binding TotalPages, Mode=OneWay}" />
                <Run Text=" — جمع: " />
                <Run Text="{Binding TotalCount, Mode=OneWay}" />
                <Run Text=" بیمار" />
            </TextBlock>
        </DockPanel>
    </Grid>
</UserControl>
""")

write("Views/PatientsView.xaml.cs", """
using System.Windows.Controls;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فهرست بیماران
/// </summary>
public partial class PatientsView : UserControl
{
    /// <summary>سازنده پیش‌فرض</summary>
    public PatientsView()
    {
        InitializeComponent();
    }
}
""")

write("Views/PatientFormWindow.xaml", """
<Window x:Class="Sahand.ClinicPro.Desktop.Views.PatientFormWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        Title="{Binding FormTitle}"
        Height="640" Width="560"
        WindowStartupLocation="CenterOwner"
        FontFamily="Vazirmatn, Segoe UI, Tahoma">
    <!-- ═══════════════════════════════════════════════════════════
         فرم ثبت و ویرایش بیمار — گام ۱۲
         فیلدهای الزامی با اعتبارسنجی سمت سرور و نمایش پیام فارسی
         ═══════════════════════════════════════════════════════════ -->
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="28">
            <TextBlock Text="{Binding FormTitle}"
                       Style="{StaticResource SectionTitle}" />

            <TextBox materialDesign:HintAssist.Hint="نام *"
                     Text="{Binding FirstName, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,0,0,12" />
            <TextBox materialDesign:HintAssist.Hint="نام خانوادگی *"
                     Text="{Binding LastName, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,0,0,12" />
            <TextBox materialDesign:HintAssist.Hint="کد ملی * (۱۰ رقم)"
                     Text="{Binding NationalCode, UpdateSourceTrigger=PropertyChanged}"
                     MaxLength="10"
                     Margin="0,0,0,12" />
            <TextBox materialDesign:HintAssist.Hint="موبایل * (09XXXXXXXXX)"
                     Text="{Binding Mobile, UpdateSourceTrigger=PropertyChanged}"
                     MaxLength="11"
                     Margin="0,0,0,12" />
            <TextBox materialDesign:HintAssist.Hint="نام پدر"
                     Text="{Binding FatherName, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,0,0,12" />
            <TextBox materialDesign:HintAssist.Hint="تاریخ تولد شمسی (مثال 1370/05/15)"
                     Text="{Binding BirthDateShamsi, UpdateSourceTrigger=PropertyChanged}"
                     MaxLength="10"
                     Margin="0,0,0,12" />

            <ComboBox materialDesign:HintAssist.Hint="جنسیت"
                      SelectedIndex="{Binding GenderIndex}"
                      Margin="0,0,0,12">
                <ComboBoxItem Content="مرد" />
                <ComboBoxItem Content="زن" />
            </ComboBox>

            <TextBox materialDesign:HintAssist.Hint="آدرس"
                     Text="{Binding Address, UpdateSourceTrigger=PropertyChanged}"
                     TextWrapping="Wrap"
                     AcceptsReturn="False"
                     Margin="0,0,0,12" />

            <TextBox materialDesign:HintAssist.Hint="شهر"
                     Text="{Binding City, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,0,0,12" />

            <ComboBox materialDesign:HintAssist.Hint="بیمه اصلی"
                      ItemsSource="{Binding InsuranceCompanies}"
                      DisplayMemberPath="Name"
                      SelectedValuePath="Id"
                      SelectedValue="{Binding InsuranceCompanyId}"
                      Margin="0,0,0,20" />

            <!-- پیام خطا -->
            <TextBlock Text="{Binding ErrorMessage}"
                       Foreground="{DynamicResource MaterialDesign.Brush.Error}"
                       FontSize="12" TextWrapping="Wrap" Margin="0,0,0,8" />

            <!-- دکمه‌ها -->
            <UniformGrid Columns="2">
                <Button Command="{Binding SaveCommand}"
                        Style="{StaticResource MaterialDesignFilledButton}"
                        Content="ذخیره" Height="42" Margin="4" />
                <Button Command="{Binding CancelCommand}"
                        Style="{StaticResource MaterialDesignOutlinedButton}"
                        Content="انصراف" Height="42" Margin="4" />
            </UniformGrid>

            <ProgressBar IsIndeterminate="True" Height="4" Margin="0,16,0,0"
                         Visibility="{Binding IsBusy, Converter={StaticResource BoolToVis}}" />
        </StackPanel>
    </ScrollViewer>
</Window>
""")

write("Views/PatientFormWindow.xaml.cs", """
using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم بیمار — ثبت و ویرایش
/// </summary>
public partial class PatientFormWindow : Window
{
    /// <summary>سازنده با ViewModel فرم</summary>
    public PatientFormWindow(PatientFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        viewModel.SaveSucceeded += (_, _) => Close();
    }
}
""")

write("ViewModels/PatientDisplayModels.cs", """
using System.Collections.ObjectModel;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// مدل نمایش بیمار در فهرست — منطبق با PatientListDto سرور
/// </summary>
public record PatientRow(
    long Id,
    string MedicalRecordNo,
    string FirstName,
    string LastName,
    string NationalCode,
    string Mobile,
    string? InsuranceName);

/// <summary>
/// مدل کمکی بیمه در ComboBox
/// </summary>
public record InsuranceOption(long Id, string Name);

/// <summary>
/// مدل نمایش ردیف صفحه‌بندی
/// </summary>
public class PageState : ObservableObjectBase
{
}
""")

write("ViewModels/ObservableObjectBase.cs", """
using System.ComponentModel;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// کلاس کمکی برای مدل‌های نمایشی — اعلان تغییر ویژگی
/// </summary>
public class ObservableObjectBase : INotifyPropertyChanged
{
    /// <summary>رویداد تغییر ویژگی</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>اعلان تغییر ویژگی</summary>
    protected void OnProp(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
""")

print("✅ گام ۱۲ — بخش ۱ کامل شد")
