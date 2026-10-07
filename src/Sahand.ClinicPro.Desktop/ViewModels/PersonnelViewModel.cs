using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel صفحهٔ مدیریت پرسنل — چرخه بیستم (گزارش چهاردهم مالک).
/// پیش‌تر آیتم «پرسنل» در منوی کناری به همان ViewModel تنظیمات
/// (AdminViewModel) می‌رفت و همیشه زبانهٔ نخست (تنظیمات سامانه) باز
/// می‌شد؛ کاربر انتظار داشت مستقیم مدیریت پرسنل دیده شود.
/// این ViewModel فرزند AdminViewModel است اما:
///   • عنوان صفحه «مدیریت پرسنل» است نه «تنظیمات»
///   • زبانهٔ پیش‌فرض ۲ (فهرست پرسنل) است و در همان سازنده،
///     دادهٔ پرسنل‌ها بارگذاری می‌شود
/// قالب نمایش همان AdminView است (نگاشت DataTemplate از نوع پایه
/// برای فرزند هم به‌کار می‌افتد) چون چیدمان دو صفحه یکسان است.
/// </summary>
public partial class PersonnelViewModel : AdminViewModel
{
    /// <summary>عنوان فارسی صفحه — نوار بالا و Breadcrumb از همین می‌خوانند</summary>
    public override string Title => "مدیریت پرسنل";

    /// <summary>
    /// سازنده — زبانهٔ ۲ (پرسنل) انتخاب و بارگذاری می‌شود؛
    /// سازندهٔ پایه بارگذاری تنظیمات را شروع می‌کند که بی‌ضرر است.
    /// </summary>
    public PersonnelViewModel(IApiClient api) : base(api)
    {
        // انتخاب زبانهٔ پرسنل — تغییر TabIndex به‌صورت خودکار
        // LoadEmployeesAsync را از طریق OnTabIndexChanged راه می‌اندازد
        TabIndex = 2;
    }
}
