using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// جستجوی سریع بیمار برای صفحات تخصصی — چرخهٔ چهل‌وسوم
/// </summary>
/// <remarks>
/// «دندانپزشکی و چشم پزشکی و تصویربرداری و فزیوتراپی در یک صفحه و تب های
/// جداگانه هستند. برای هر کدوم یک صفحه مستقل طراحی بکن تا با همدیگه
/// تداخلی نداشته باشن» — با جداسازی صفحات، منطق مشترک «جستجوی بیمار و
/// ساخت QuickPatient» که پیش‌تر درون ParaclinicalViewModel زندگی می‌کرد به
/// این هلپر ایستا منتقل شد تا هر چهار ViewModel مستقل از همان یک پیاده‌سازی
/// استفاده کنند (هیچ منطق دومی — قانون هم‌کلاسی‌ها).
/// </remarks>
public static class PatientQuickSearch
{
    /// <summary>
    /// جستجوی بیمار با کد ملی/موبایل/نام — نخستین نتیجه؛ پیام‌های فارسی
    /// توست خودِ هلپر نمایش می‌دهد (همان رفتار پیشین پاراکلینیک).
    /// </summary>
    /// <param name="api">کلاینت HTTP برنامه</param>
    /// <param name="snackbar">سرویس پیام شناور</param>
    /// <param name="term">عبارت جستجو — حداقل ۴ نویسه</param>
    /// <returns>بیمار یافته‌شده یا تهی</returns>
    public static async Task<QuickPatient?> FindAsync(IApiClient api, ISnackbarService snackbar, string? term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 4)
        {
            snackbar.Error("کد ملی یا موبایل را وارد کنید");
            return null;
        }

        var response = await api.GetAsync<ViewModels.PatientFormViewModel.SearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(term.Trim())}");
        var p = response?.Data?.FirstOrDefault();
        if (p is null)
        {
            snackbar.Error("بیمار یافت نشد");
            return null;
        }

        var patient = new QuickPatient(p.Id, $"{p.FirstName} {p.LastName}", p.NationalCode, p.Mobile,
            p.InsuranceName, p.Allergies, null, null, p.Gender);
        snackbar.Success($"بیمار: {p.FirstName} {p.LastName}");
        return patient;
    }
}
