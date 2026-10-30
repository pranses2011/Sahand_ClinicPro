using CommunityToolkit.Mvvm.ComponentModel;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// پنج عدد کارتهای آماری بالای صفحات بازطراحیشده — همان چیدمان سبک
/// تصویربرداری که مالک تأیید کرد.
/// </summary>
/// <remarks>
/// مقادیر پیشفرض «۰» هستند تا در اجرای واقعی (بدون سرور/داده) چیزی
/// خلاف واقع نشان داده نشود. هر ViewModel میتواند این اعداد را از دادههای
/// واقعیش پر کند؛ حالت راستیآزمایی تصویری (App.xaml.cs) هم همان مقادیر
/// نمونهای طرح تأییدی را روی آنها مینشاند تا اسکرینشات WPF با رندر HTML
/// طرح، پیکسلبهپیکسل قابلمقایسه باشد. V1 سمت راست و V5 سمت چپ است.
/// </remarks>
public sealed class DesignKpis : ObservableObject
{
    private string _v1 = "۰";
    private string _v2 = "۰";
    private string _v3 = "۰";
    private string _v4 = "۰";
    private string _v5 = "۰";

    /// <summary>کارت سبز — راستترین کارت.</summary>
    public string V1 { get => _v1; set => SetProperty(ref _v1, value); }
    /// <summary>کارت قرمز.</summary>
    public string V2 { get => _v2; set => SetProperty(ref _v2, value); }
    /// <summary>کارت بنفش.</summary>
    public string V3 { get => _v3; set => SetProperty(ref _v3, value); }
    /// <summary>کارت نارنجی.</summary>
    public string V4 { get => _v4; set => SetProperty(ref _v4, value); }
    /// <summary>کارت آبی — چپترین کارت.</summary>
    public string V5 { get => _v5; set => SetProperty(ref _v5, value); }
}
