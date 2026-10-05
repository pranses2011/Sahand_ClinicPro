using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// شرکت بیمه - اطلاعات قرارداد و اتصال API
/// </summary>
public class InsuranceCompany : AuditableEntity, ISoftDeletable
{
    /// <summary>نام شرکت بیمه</summary>
    public string Name { get; set; } = null!;

    /// <summary>کد یکتای بیمه</summary>
    public string? Code { get; set; }

    /// <summary>نوع بیمه</summary>
    public InsuranceType Type { get; set; }

    /// <summary>مسیر لوگو</summary>
    public string? LogoPath { get; set; }

    /// <summary>شماره قرارداد</summary>
    public string? ContractNumber { get; set; }

    /// <summary>شروع قرارداد</summary>
    public DateOnly? ContractStartDate { get; set; }

    /// <summary>پایان قرارداد</summary>
    public DateOnly? ContractEndDate { get; set; }

    /// <summary>سهم پیش‌فرض بیمار</summary>
    public decimal DefaultPatientShare { get; set; } = 0m;

    /// <summary>سهم پیش‌فرض بیمه</summary>
    public decimal DefaultInsuranceShare { get; set; } = 0m;

    /// <summary>نشانی API استعلام</summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>نام کاربری API</summary>
    public string? ApiUsername { get; set; }

    /// <summary>رمز عبور رمزنگاری‌شده با AES</summary>
    public byte[]? ApiPasswordEncrypted { get; set; }

    /// <summary>توکن پایدار API</summary>
    public string? ApiToken { get; set; }

    /// <summary>استعلام آنلاین پشتیبانی می‌شود</summary>
    public bool SupportsOnlineInquiry { get; set; } = false;

    /// <summary>نسخه الکترونیک پشتیبانی می‌شود</summary>
    public bool SupportsElectronicRx { get; set; } = false;

    /// <summary>تلفن رابط</summary>
    public string? ContactPhone { get; set; }

    /// <summary>نشانی</summary>
    public string? Address { get; set; }

    /// <summary>فعال</summary>
/// <summary>درصد پوشش پیش‌فرض — برای محاسبه سهم بیمه در پذیرش</summary>
    public decimal DefaultCoveragePercent { get; set; } = 70m;

        public bool IsActive { get; set; } = true;

    /// <summary>بیماران با این بیمه اصلی</summary>
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();

    /// <summary>تعرفه‌های این بیمه</summary>
    public ICollection<Tariff> Tariffs { get; set; } = new List<Tariff>();

    /// <summary>مطالبات این بیمه</summary>
    public ICollection<InsuranceClaim> Claims { get; set; } = new List<InsuranceClaim>();

    /// <summary>استعلام‌های این بیمه</summary>
    public ICollection<InsuranceInquiry> Inquiries { get; set; } = new List<InsuranceInquiry>();
}
