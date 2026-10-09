using Sahand.ClinicPro.Desktop.ViewModels;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// ابزار مشترک «مشخصات کاربری» پرسنل/پزشکان — چرخهٔ بیست‌ونهم (گزارش هجدهم مالک):
/// «کلید مشخصات کاربری در لیست پزشکان/پرسنل لازم است تا حساب ورود متصل ساخته
/// یا ویرایش شود». کلیدِ ردیف در هر دو صفحه، حسابِ کاربریِ متصل به رکورد پرسنل
/// را برای ویرایش باز می‌کند و اگر پرسنل هنوز حساب ندارد، فرم ثبتِ کاربرِ
/// جدیدِ متصل به «همان» رکورد را با نام کامل و موبایل پیش‌پر نشان می‌دهد —
/// اتصال با شناسهٔ دقیق پرسنل انجام می‌شود، نه تطبیق نامیِ خطاپذیر.
/// </summary>
public static class AccountTools
{
    /// <summary>پاسخ نقطهٔ پایانی users/by-employee</summary>
    private sealed class LinkedUserResponse
    {
        public bool IsSuccess { get; set; }

        /// <summary>تهی یعنی این پرسنل هنوز حساب ورود ندارد</summary>
        public LinkedUserDto? Data { get; set; }
    }

    /// <summary>حساب کاربری متصل به پرسنل</summary>
    private sealed class LinkedUserDto
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// گشودن فرم مشخصات کاربریِ متصل به یک پرسنل — حساب موجود ویرایش می‌شود
    /// یا فرم ثبتِ حساب جدیدِ متصل به همان رکورد پرسنل باز می‌شود.
    /// پارامتر mobile فقط برای پیش‌پر فرم ثبت جدید است.
    /// </summary>
    public static async Task OpenForEmployeeAsync(IApiClient api, ISnackbarService snackbar,
        long employeeId, string fullName, string? mobile)
    {
        // حساب متصل به این پرسنل — یک پرسنل نباید چند حساب موازی داشته باشد
        var linked = await api.GetAsync<LinkedUserResponse>($"users/by-employee/{employeeId}");
        if (linked is null && !string.IsNullOrWhiteSpace(api.LastError))
        {
            snackbar.Error(api.LastError);
            return;
        }

        var window = new UserFormWindow(new UserFormViewModel(api, snackbar))
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (linked?.Data is { } user)
        {
            // حساب موجود — حالت ویرایش؛ نقش‌ها و مشخصات بارگذاری می‌شود
            await window.ViewModel.LoadForEdit(user.Id);
            snackbar.Info($"حساب «{user.Username}» متصل به «{fullName}» باز شد");
        }
        else
        {
            // بدون حساب — فرم ثبتِ جدید با اتصال دقیق به همین رکورد پرسنل
            window.ViewModel.PrepareForEmployee(employeeId, fullName, mobile);
        }
        window.ShowDialog();
    }
}
