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
