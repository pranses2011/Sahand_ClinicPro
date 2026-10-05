using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Personnel;

/// <summary>
/// فیش حقوقی - محاسبه ماهانه حقوق پرسنل
/// </summary>
public class EmployeeSalary : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه کارمند</summary>
    public long EmployeeId { get; set; }

    /// <summary>سال دوره</summary>
    public int PeriodYear { get; set; }

    /// <summary>ماه دوره</summary>
    public int PeriodMonth { get; set; }

    /// <summary>حقوق پایه</summary>
    public decimal BaseSalary { get; set; } = 0m;

    /// <summary>
    /// عکس‌فوری درصد حقوق از سود خدمات در لحظه محاسبه فیش
    /// (برای شفافیت و مقایسه تاریخی حتی اگر بعداً درصد پرسنل تغییر کند)
    /// </summary>
    public decimal CommissionPercent { get; set; } = 0m;

    /// <summary>
    /// جمع سود خدمات انجام‌شده توسط این پرسنل در دوره (ریال) —
    /// مبنای محاسبه حقوق درصدی؛ «نه کل دریافتی و نه کل فروش»
    /// </summary>
    public decimal ServiceProfitBase { get; set; } = 0m;

    /// <summary>
    /// حقوق درصدی دوره (ریال) = ServiceProfitBase × CommissionPercent ÷ ۱۰۰.
    /// فقط از سود خدمات انجامی خودِ پرسنل؛ در صورت سود منفی یا صفر، صفر است.
    /// </summary>
    public decimal ServiceCommission { get; set; } = 0m;

    /// <summary>اضافه‌کاری</summary>
    public decimal Overtime { get; set; } = 0m;

    /// <summary>پاداش</summary>
    public decimal Bonus { get; set; } = 0m;

    /// <summary>کسورات</summary>
    public decimal Deductions { get; set; } = 0m;

    /// <summary>بیمه</summary>
    public decimal Insurance { get; set; } = 0m;

    /// <summary>مالیات</summary>
    public decimal Tax { get; set; } = 0m;

    /// <summary>خالص پرداختی</summary>
    public decimal NetAmount { get; set; } = 0m;

    /// <summary>زمان پرداخت</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>وضعیت</summary>
    public SalaryStatus Status { get; set; }

    /// <summary>کارمند</summary>
    public Employee? Employee { get; set; }
}
