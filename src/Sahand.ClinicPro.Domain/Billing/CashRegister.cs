using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// صندوق - کارکرد روزانه صندوقدار با موجودی
/// </summary>
public class CashRegister : AuditableEntity, ISoftDeletable
{
    /// <summary>نام صندوق</summary>
    public string RegisterName { get; set; } = null!;

    /// <summary>شناسه صندوقدار</summary>
    public long? CashierId { get; set; }

    /// <summary>زمان باز شدن صندوق</summary>
    public DateTime? OpenedAt { get; set; }

    /// <summary>زمان بسته شدن</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>موجودی اولیه</summary>
    public decimal OpeningBalance { get; set; } = 0m;

    /// <summary>موجودی پایانی</summary>
    public decimal ClosingBalance { get; set; } = 0m;

    /// <summary>جمع نقدی</summary>
    public decimal TotalCash { get; set; } = 0m;

    /// <summary>جمع کارتی</summary>
    public decimal TotalCard { get; set; } = 0m;

    /// <summary>جمع آنلاین</summary>
    public decimal TotalOnline { get; set; } = 0m;

    /// <summary>اختلاف شمارش</summary>
    public decimal Difference { get; set; } = 0m;

    /// <summary>وضعیت صندوق</summary>
    public CashRegisterStatus Status { get; set; } = Domain.Enums.CashRegisterStatus.Open;

    /// <summary>شمارش فیزیکی نقد صندوقدار (ریال) — چرخه ۱۳ (درخواست ۲-۲)</summary>
    public decimal CountedCash { get; set; } = 0m;

    /// <summary>شمارش فیزیکی کارت (ریال) — تراکنش‌های کارتخوان</summary>
    public decimal CountedCard { get; set; } = 0m;

    /// <summary>شماره رسید تحویل شیفت — مثل SHR-0001</summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>شناسه صندوقدار تحویل‌گیرنده شیفت بعد</summary>
    public long? HandoverToUserId { get; set; }

    /// <summary>یادداشت تحویل شیفت</summary>
    public string? HandoverNote { get; set; }

    /// <summary>شناسه کاربر بستن‌دهنده شیفت</summary>
    public long? ClosedByUserId { get; set; }

    /// <summary>نام بستن‌دهنده — عکس‌فوری برای گزارش</summary>
    public string? ClosedByName { get; set; }

    /// <summary>پرداخت‌های ثبت‌شده در این صندوق</summary>
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
