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
    /// <summary>عنوان فارسی صفحه — نوار بالا و Breadcrumb از همین می‌خوانند</summary>
    public override string Title => "مدیریت پرسنل";

    /// <summary>
    /// سازنده — بارگذاری مستقیم فهرست پرسنل (بدون زبانه)؛ سازندهٔ پایه فقط
    /// ساخت اولیه است و TabIndex پیش‌فرض صفر می‌ماند چون نمای مستقل، زبانه ندارد.
    /// </summary>
    public PersonnelViewModel(IApiClient api) : base(api)
    {
        // بارگذاری فهرست پرسنل برای صفحهٔ مستقل — پیش‌تر از طریق تغییر زبانه انجام می‌شد
        _ = LoadEmployeesAsync();
    }
}
