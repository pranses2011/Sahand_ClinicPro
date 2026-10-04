namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع تأمین‌کننده
/// </summary>
public enum SupplierType : byte
{
    /// <summary>شرکت سازنده</summary>
    Manufacturer = 1,
    /// <summary>پخش</summary>
    Distributor = 2,
    /// <summary>واردات</summary>
    Importer = 3,
    /// <summary>سایر</summary>
    Other = 4,
}
