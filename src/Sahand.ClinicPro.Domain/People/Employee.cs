using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.People;

/// <summary>
/// کارمند درمانگاه - پرسنل اداری و درمانی
/// </summary>
public class Employee : AuditableEntity, ISoftDeletable
{
    /// <summary>کد پرسنلی - یکتا</summary>
    public string? PersonnelCode { get; set; }

    /// <summary>کد ملی - یکتا</summary>
    public string? NationalCode { get; set; }

    /// <summary>نام</summary>
    public string FirstName { get; set; } = null!;

    /// <summary>نام خانوادگی</summary>
    public string LastName { get; set; } = null!;

    /// <summary>نام پدر</summary>
    public string? FatherName { get; set; }

    /// <summary>جنسیت</summary>
    public Gender Gender { get; set; }

    /// <summary>تاریخ تولد</summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>وضعیت تأهل</summary>
    public MaritalStatus? MaritalStatus { get; set; }

    /// <summary>موبایل</summary>
    public string? Mobile { get; set; }

    /// <summary>تلفن ثابت</summary>
    public string? Phone { get; set; }

    /// <summary>ایمیل</summary>
    public string? Email { get; set; }

    /// <summary>آدرس</summary>
    public string? Address { get; set; }

    /// <summary>شناسه بخش</summary>
    public long? DepartmentId { get; set; }

    /// <summary>شناسه سمت</summary>
    public long? PositionId { get; set; }

    /// <summary>نوع استخدام</summary>
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>تاریخ استخدام</summary>
    public DateOnly? HireDate { get; set; }

    /// <summary>نوع قرارداد</summary>
    public ContractType? ContractType { get; set; }

    /// <summary>شروع قرارداد</summary>
    public DateOnly? ContractStartDate { get; set; }

    /// <summary>پایان قرارداد</summary>
    public DateOnly? ContractEndDate { get; set; }

    /// <summary>حقوق پایه به ریال</summary>
    public decimal BaseSalary { get; set; } = 0m;

    /// <summary>نام بانک</summary>
    public string? BankName { get; set; }

    /// <summary>شماره حساب بانکی</summary>
    public string? BankAccount { get; set; }

    /// <summary>شماره شبا ۲۶ کاراکتری</summary>
    public string? Sheba { get; set; }

    /// <summary>سطح تحصیلات</summary>
    public EducationLevel? EducationLevel { get; set; }

    /// <summary>رشته تحصیلی</summary>
    public string? FieldOfStudy { get; set; }

    /// <summary>نام تماس اضطراری</summary>
    public string? EmergencyContact { get; set; }

    /// <summary>تلفن اضطراری</summary>
    public string? EmergencyPhone { get; set; }

    /// <summary>مسیر عکس</summary>
    public string? ProfilePicture { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>بخش محل خدمت</summary>
    public Department? Department { get; set; }

    /// <summary>سمت شغلی</summary>
    public Position? Position { get; set; }

    /// <summary>سوابق حضور و غیاب</summary>
    public ICollection<EmployeeAttendance> Attendances { get; set; } = new List<EmployeeAttendance>();

    /// <summary>فیش‌های حقوقی</summary>
    public ICollection<EmployeeSalary> Salaries { get; set; } = new List<EmployeeSalary>();

    /// <summary>مرخصی‌ها</summary>
    public ICollection<EmployeeLeave> Leaves { get; set; } = new List<EmployeeLeave>();

    /// <summary>قراردادها</summary>
    public ICollection<PersonnelContract> Contracts { get; set; } = new List<PersonnelContract>();

    /// <summary>حساب‌های کاربری مرتبط</summary>
    public ICollection<User> UserAccounts { get; set; } = new List<User>();
}
