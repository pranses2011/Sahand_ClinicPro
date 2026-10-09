using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel صفحهٔ مستقل مدیریت پرسنل — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
/// «در منوی کناری، گزینهٔ پرسنل صفحهٔ تنظیمات و مدیریت سامانه را باز می‌کند و
/// پرسنل در تب پرسنل در آن صفحه باز می‌شود که اشتباه است — پرسنل باید یک
/// صفحهٔ جدا باشد و این صفحه هم گزینهٔ جداگانه باشد».
/// ─────────────────────────────────────────────────────────────────────
/// پیش‌تر این ViewModel فرزند AdminViewModel بود و قالب نمایشش AdminView
/// (صفحهٔ تنظیمات چندزبانه‌ای) بود؛ اکنون نمای اختصاصی PersonnelView دارد و:
///   • دادهٔ پرسنل‌ها در همان سازندهٔ خودِ صفحه بارگذاری می‌شود
///   • وابستگی به AdminViewModel فقط برای استفادهٔ مجدد از فرمان‌ها/داده‌های
///     مشترک پرسنل است (NewEmployee/LoadEmployees/EditSalary/عکس‌ها)؛ زبانه‌ای
///     وجود ندارد و TabIndex نقشی در این صفحه ندارد
/// </summary>
public partial class PersonnelViewModel : AdminViewModel
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان فارسی صفحه — نوار بالا و Breadcrumb از همین می‌خوانند</summary>
    public override string Title => "مدیریت پرسنل";

    /// <summary>
    /// سازنده — بارگذاری مستقیم فهرست پرسنل (بدون زبانه)؛ سازندهٔ پایه فقط
    /// ساخت اولیه است و TabIndex پیش‌فرض صفر می‌ماند چون نمای مستقل، زبانه ندارد.
    /// چرخهٔ بیست‌ونهم: snackbar و api تزریق محلی شدند تا فرمان «مشخصات کاربری»
    /// بازخورد شناور بدهد (AdminViewModel فقط StatusText داخل‌صفحه‌ای دارد).
    /// </summary>
    public PersonnelViewModel(IApiClient api, ISnackbarService snackbar) : base(api)
    {
        _api = api;
        _snackbar = snackbar;
        // بارگذاری فهرست پرسنل برای صفحهٔ مستقل — پیش‌تر از طریق تغییر زبانه انجام می‌شد
        _ = LoadEmployeesAsync();
    }

    /// <summary>
    /// مشخصات کاربری پرسنل — چرخهٔ بیست‌ونهم (گزارش هجدهم مالک):
    /// «کلید مشخصات کاربری در لیست پرسنل لازم است تا حساب ورودِ متصل به همین
    /// رکورد ساخته یا ویرایش شود». حسابِ موجود برای ویرایش باز می‌شود؛
    /// بدون حساب، فرم ثبتِ جدید با نام کامل/موبایل پیش‌پر و اتصال دقیق
    /// با شناسهٔ همین پرسنل باز می‌شود.
    /// </summary>
    [RelayCommand]
    private async Task ManageAccountAsync(EmployeeRow? row)
    {
        var employee = row ?? SelectedEmployee;
        if (employee is null) { _snackbar.Info("ابتدا یک پرسنل انتخاب کنید"); return; }

        IsBusy = true;
        await AccountTools.OpenForEmployeeAsync(_api, _snackbar, employee.Id, employee.FullName, employee.Mobile);
        IsBusy = false;
    }
}
