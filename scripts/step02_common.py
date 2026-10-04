# -*- coding: utf-8 -*-
"""گام ۲ — بخش ۱: کلاس‌های پایه، Value Objects، Exceptions، Interfaces، Events"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from step02_lib import write

# ═══════════════════════ Common ═══════════════════════

write("Common/BaseEntity.cs", """
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// کلاس پایه همه موجودیت‌های دامنه
/// شامل کلید اصلی و مکانیزم رویدادهای دامنه است
/// </summary>
public abstract class BaseEntity
{
    /// <summary>کلید اصلی جداول (bigint IDENTITY)</summary>
    public long Id { get; set; }

    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>رویدادهای دامنه که باید پس از ذخیره انتشار یابند</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>افزودن رویداد دامنه به صف انتشار</summary>
    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>حذف رویداد دامنه از صف انتشار</summary>
    public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

    /// <summary>پاک‌سازی صف رویدادهای دامنه</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
""")

write("Common/AuditableEntity.cs", """
using System;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// موجودیت قابل ردگیری — فیلدهای حسابرسی و حذف نرم و چندشعبه‌ای را اضافه می‌کند
/// فیلدهای ایجاد و ویرایش توسط AuditableInterceptor به‌صورت خودکار پر می‌شوند
/// </summary>
public abstract class AuditableEntity : BaseEntity, ISoftDeletable
{
    /// <summary>تاریخ و ساعت ایجاد رکورد (UTC)</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>شناسه کاربر ایجادکننده</summary>
    public long? CreatedBy { get; set; }

    /// <summary>تاریخ و ساعت آخرین ویرایش (UTC)</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>شناسه کاربر ویرایش‌کننده</summary>
    public long? UpdatedBy { get; set; }

    /// <summary>آیا رکورد حذف (نرم) شده است</summary>
    public bool IsDeleted { get; set; }

    /// <summary>تاریخ حذف نرم</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>شناسه کاربر حذف‌کننده</summary>
    public long? DeletedBy { get; set; }

    /// <summary>شناسه شعبه برای چندشعبه‌ای آینده</summary>
    public long? BranchId { get; set; }

    /// <summary>نسخه ردیف برای کنترل همزمانی خوش‌بینانه (Concurrency Token)</summary>
    public byte[]? RowVersion { get; set; }
}
""")

write("Common/ISoftDeletable.cs", """
using System;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// اینترفیس حذف نرم — هیچ رکوردی واقعاً حذف نمی‌شود
/// Global Query Filter به‌صورت خودکار رکوردهای حذف‌شده را پنهان می‌کند
/// </summary>
public interface ISoftDeletable
{
    /// <summary>آیا رکورد حذف (نرم) شده است</summary>
    bool IsDeleted { get; set; }

    /// <summary>تاریخ حذف نرم</summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>شناسه کاربر حذف‌کننده</summary>
    public long? DeletedBy { get; set; }
}
""")

write("Common/IDomainEvent.cs", """
using System;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// نشانگر رویداد دامنه — رویدادهای کسب‌وکار که پس از Commit منتشر می‌شوند
/// </summary>
public interface IDomainEvent
{
    /// <summary>زمان وقوع رویداد</summary>
    DateTime OccurredOn { get; }
}
""")

write("Common/DomainEventBase.cs", """
using System;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// کلاس پایه رویدادهای دامنه
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    /// <summary>زمان وقوع رویداد (UTC)</summary>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
""")

write("Common/ValueObject.cs", """
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// کلاس پایه اشیاء ارزشی — برابری بر اساس تمام اجزاء
/// </summary>
public abstract class ValueObject
{
    /// <summary>اجزاء تشکیل‌دهنده شیء ارزشی برای مقایسه</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x is null ? 0 : x.GetHashCode())
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !(left == right);
}
""")

write("Common/Error.cs", """
namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// مدل خطای استاندارد دامنه
/// </summary>
/// <param name="Code">کد خطا (مثال: PATIENT_DUPLICATE)</param>
/// <param name="Message">پیام فارسی قابل نمایش به کاربر</param>
public sealed record Error(string Code, string Message)
{
    /// <summary>خطای تهی — نبود خطا</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>ساخت خطای عدم یافتن</summary>
    public static Error NotFound(string entity, long id) =>
        new($"{entity.ToUpperInvariant()}_NOT_FOUND", $".موجودیت {entity} با شناسه {id} یافت نشد");
}
""")

write("Common/Result.cs", """
using System;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// الگوی Result — برگشت موفقیت یا خطا بدون استثنا
/// </summary>
public class Result
{
    /// <summary>آیا عملیات موفق بود</summary>
    public bool IsSuccess { get; }

    /// <summary>آیا عملیات شکست خورد</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>خطای رخ‌داده در صورت شکست</summary>
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>ساخت نتیجه موفق</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>ساخت نتیجه شکست</summary>
    public static Result Failure(Error error) => new(false, error);
}

/// <summary>
/// الگوی Result ژنریک — همراه با داده خروجی
/// </summary>
/// <typeparam name="T">نوع داده خروجی</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, Error error, T? value) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>مقدار خروجی در صورت موفقیت</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("دسترسی به مقدار نتیجه ناموفق مجاز نیست");

    /// <summary>ساخت نتیجه موفق با مقدار</summary>
    public static Result<T> Success(T value) => new(true, Error.None, value);

    /// <summary>ساخت نتیجه شکست</summary>
    public static new Result<T> Failure(Error error) => new(false, error, default);
}
""")

# ═══════════════════════ Exceptions ═══════════════════════

write("Exceptions/DomainException.cs", """
namespace Sahand.ClinicPro.Domain.Exceptions;

/// <summary>
/// استثنای پایه دامنه — همه استثناهای کسب‌وکار از این کلاس ارث می‌برند
/// </summary>
public class DomainException : Exception
{
    /// <summary>کد خطا برای پایش</summary>
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "DOMAIN_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
""")

write("Exceptions/BusinessRuleException.cs", """
namespace Sahand.ClinicPro.Domain.Exceptions;

/// <summary>
/// نقض قاعده کسب‌وکار (مثال: ظرفیت نوبت تکمیل است)
/// </summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message, "BUSINESS_RULE_VIOLATION") { }
}
""")

write("Exceptions/NotFoundException.cs", """
namespace Sahand.ClinicPro.Domain.Exceptions;

/// <summary>
/// یافت نشدن موجودیت درخواستی
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($".موجودیت {entityName} با کلید {key} یافت نشد", "NOT_FOUND") { }
}
""")

write("Exceptions/ConflictException.cs", """
namespace Sahand.ClinicPro.Domain.Exceptions;

/// <summary>
/// تعارض داده (مثال: کد ملی تکراری)
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message, "CONFLICT") { }
}
""")

write("Exceptions/UnbalancedJournalEntryException.cs", """
namespace Sahand.ClinicPro.Domain.Exceptions;

/// <summary>
/// نقض قانون طلایی حسابداری دوبل — در هر سند جمع بدهکار باید با جمع بستانکار برابر باشد
/// </summary>
public class UnbalancedJournalEntryException : DomainException
{
    public UnbalancedJournalEntryException(decimal totalDebit, decimal totalCredit)
        : base($"سند حسابداری نامتوازن است — جمع بدهکار {totalDebit:N0} و جمع بستانکار {totalCredit:N0} ریال", "UNBALANCED_JOURNAL")
    {
    }
}
""")

# ═══════════════════════ Interfaces ═══════════════════════

write("Interfaces/IRepository.cs", """
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// ریپازیتوری عمومی — قرارداد دسترسی به داده موجودیت‌ها
/// </summary>
/// <typeparam name="T">نوع موجودیت</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>دسترسی به کوئری‌پذیر برای سناریوهای پیشرفته (بدون ردیابی)</summary>
    IQueryable<T> QueryableNoTracking { get; }

    /// <summary>دسترسی به کوئری‌پذیر با ردیابی تغییرات</summary>
    IQueryable<T> QueryableTracking { get; }

    /// <summary>یافتن بر اساس کلید اصلی</summary>
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>یافتن بر اساس شرط</summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>آیا حداقل یک رکورد با شرط وجود دارد</summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>شمارش رکوردهای منطبق با شرط</summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>دریافت همه رکوردها (با فیلتر اختیاری)</summary>
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>افزودن موجودیت جدید</summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>ثبت موجودیت برای ویرایش</summary>
    void Update(T entity);

    /// <summary>ثبت موجودیت برای حذف نرم (توسط اینترسپتور انجام می‌شود)</summary>
    void Remove(T entity);
}
""")

write("Interfaces/IUnitOfWork.cs", """
using System;
using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// واحد کار — هماهنگ‌سازی ذخیره‌سازی و انتشار رویدادهای دامنه
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>دریافت ریپازیتوری موجودیت موردنظر</summary>
    IRepository<T> Repository<T>() where T : BaseEntity;

    /// <summary>ذخیره تغییرات و انتشار رویدادهای دامنه — تراکنش توسط Pipeline مدیریت می‌شود</summary>
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}
""")

write("Interfaces/IPatientRepository.cs", """
using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// ریپازیتوری تخصصی بیماران — متدهای خاص ماژول بیمار
/// </summary>
public interface IPatientRepository : IRepository<Patient>
{
    /// <summary>یافتن بیمار بر اساس کد ملی</summary>
    Task<Patient?> GetByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default);

    /// <summary>یافتن بیمار بر اساس شماره پرونده</summary>
    Task<Patient?> GetByMedicalRecordNoAsync(string medicalRecordNo, CancellationToken cancellationToken = default);

    /// <summary>آیا بیماری با این کد ملی ثبت شده است</summary>
    Task<bool> ExistsByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default);
}
""")

write("Interfaces/IDoctorRepository.cs", """
using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// ریپازیتوری تخصصی پزشکان
/// </summary>
public interface IDoctorRepository : IRepository<Doctor>
{
    /// <summary>یافتن پزشک بر اساس کد نظام پزشکی</summary>
    Task<Doctor?> GetByNezamCodeAsync(string nezamCode, CancellationToken cancellationToken = default);
}
""")

write("Interfaces/IAppointmentRepository.cs", """
using System;
using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Entities.Scheduling;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// ریپازیتوری تخصصی نوبت‌ها
/// </summary>
public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>شمارش نوبت‌های فعال یک پزشک در یک تاریخ (برای کنترل ظرفیت)</summary>
    Task<int> CountActiveByDoctorAndDateAsync(long doctorId, DateOnly date, CancellationToken cancellationToken = default);
}
""")

write("Interfaces/ILicenseRepository.cs", """
using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Entities.Licensing;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// ریپازیتوری لایسنس سمت کلاینت — خواندن رکورد رمزنگاری‌شده لایسنس محلی
/// </summary>
public interface ILicenseRepository : IRepository<ClinicLicense>
{
    /// <summary>دریافت تنها رکورد لایسنس فعال درمانگاه</summary>
    Task<ClinicLicense?> GetActiveAsync(CancellationToken cancellationToken = default);
}
""")

# ═══════════════════════ Events ═══════════════════════

write("Events/PatientEvents.cs", """
using System.Collections.Generic;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد ثبت بیمار جدید — برای ارسال پیامک خوش‌آمدگویی</summary>
public sealed record PatientCreatedEvent(long PatientId, string MedicalRecordNo, string Mobile) : DomainEventBase;

/// <summary>رویداد ویرایش اطلاعات بیمار</summary>
public sealed record PatientUpdatedEvent(long PatientId) : DomainEventBase;
""")

write("Events/AppointmentEvents.cs", """
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد رزرو نوبت — برای ارسال پیامک تأیید</summary>
public sealed record AppointmentBookedEvent(long AppointmentId, long PatientId, string Mobile) : DomainEventBase;

/// <summary>رویداد لغو نوبت</summary>
public sealed record AppointmentCancelledEvent(long AppointmentId, long PatientId, string Reason) : DomainEventBase;

/// <summary>رویداد یادآوری نوبت فردا — برای صف پیامک</summary>
public sealed record AppointmentReminderDueEvent(long AppointmentId, long PatientId, string Mobile) : DomainEventBase;
""")

write("Events/VisitEvents.cs", """
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد تکمیل ویزیت</summary>
public sealed record VisitCompletedEvent(long VisitId, long PatientId, long DoctorId) : DomainEventBase;
""")

write("Events/PrescriptionEvents.cs", """
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد صدور نسخه</summary>
public sealed record PrescriptionCreatedEvent(long PrescriptionId, long PatientId, long DoctorId, bool IsElectronic) : DomainEventBase;
""")

write("Events/BillingEvents.cs", """
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد صدور فاکتور</summary>
public sealed record InvoiceCreatedEvent(long InvoiceId, long PatientId, decimal TotalAmount) : DomainEventBase;

/// <summary>رویداد تسویه کامل فاکتور</summary>
public sealed record InvoicePaidEvent(long InvoiceId, long PatientId, decimal PaidAmount) : DomainEventBase;

/// <summary>رویداد ثبت پرداخت</summary>
public sealed record PaymentReceivedEvent(long PaymentId, long InvoiceId, decimal Amount) : DomainEventBase;
""")

write("Events/PharmacyEvents.cs", """
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد هشدار کمبود موجودی دارو — برای نوتیفیکیشن و پیامک انباردار</summary>
public sealed record LowStockAlertEvent(long InventoryId, long MedicationId, string MedicationName, decimal Quantity, decimal ReorderLevel) : DomainEventBase;

/// <summary>رویداد نزدیک بودن تاریخ انقضای دارو</summary>
public sealed record MedicationExpiringEvent(long InventoryId, string MedicationName, int DaysRemaining) : DomainEventBase;
""")

write("Events/LicenseEvents.cs", """
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد انقضای لایسنس</summary>
public sealed record LicenseExpiredEvent(string LicenseKey) : DomainEventBase;

/// <summary>رویداد تمدید/فعال‌سازی موفق لایسنس</summary>
public sealed record LicenseActivatedEvent(string LicenseKey, bool IsOffline) : DomainEventBase;
""")

write("Events/LabEvents.cs", """
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد آماده شدن نتیجه آزمایش</summary>
public sealed record LabResultReadyEvent(long LabOrderId, long PatientId, string Mobile) : DomainEventBase;
""")

# ═══════════════════════ Value Objects ═══════════════════════

write("ValueObjects/NationalCode.cs", """
using System;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Exceptions;

namespace Sahand.ClinicPro.Domain.ValueObjects;

/// <summary>
/// کد ملی ۱۰ رقمی ایرانی — با اعتبارسنجی الگوریتم رقم کنترلی
/// </summary>
public sealed class NationalCode : ValueObject
{
    /// <summary>مقدار کد ملی</summary>
    public string Value { get; }

    private NationalCode(string value) => Value = value;

    /// <summary>ساخت کد ملی با اعتبارسنجی کامل</summary>
    public static NationalCode Of(string value)
    {
        if (!IsValid(value))
            throw new BusinessRuleException("کد ملی نامعتبر است");
        return new NationalCode(Normalize(value));
    }

    /// <summary>اعتبارسنجی الگوریتم رقم کنترلی کد ملی</summary>
    public static bool IsValid(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        code = Normalize(code);
        if (code.Length != 10 || !long.TryParse(code, out _)) return false;
        if (new string(code[0], 10) == code) return false; // ارقام تکراری
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (code[i] - '0') * (10 - i);
        int remainder = sum % 11;
        int check = code[9] - '0';
        return remainder < 2 ? check == remainder : check == 11 - remainder;
    }

    private static string Normalize(string code) => code.Trim();

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;
}
""")

write("ValueObjects/PhoneNumber.cs", """
using System;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Exceptions;

namespace Sahand.ClinicPro.Domain.ValueObjects;

/// <summary>
/// شماره موبایل ایرانی ۱۱ رقمی (09XXXXXXXXX)
/// </summary>
public sealed class PhoneNumber : ValueObject
{
    /// <summary>مقدار شماره موبایل</summary>
    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    /// <summary>ساخت شماره موبایل با اعتبارسنجی</summary>
    public static PhoneNumber Of(string value)
    {
        if (!IsValid(value))
            throw new BusinessRuleException("شماره موبایل نامعتبر است — الگوی صحیح: 09XXXXXXXXX");
        return new PhoneNumber(value.Trim());
    }

    /// <summary>اعتبارسنجی الگوی موبایل ایران</summary>
    public static bool IsValid(string? number)
    {
        if (string.IsNullOrWhiteSpace(number)) return false;
        number = number.Trim();
        return number.Length == 11 && number.StartsWith("09") && long.TryParse(number, out _);
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;
}
""")

write("ValueObjects/Money.cs", """
using System;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.ValueObjects;

/// <summary>
/// مبلغ پولی به ریال — با عملیات جمع، تفریق و ضرب و مدیریت گرد کردن
/// </summary>
public sealed class Money : ValueObject
{
    /// <summary>مقدار به ریال</summary>
    public decimal Amount { get; }

    /// <summary>واحد پول (پیش‌فرض ریال ایران)</summary>
    public string Currency { get; } = "IRR";

    private Money(decimal amount, string currency = "IRR")
    {
        Amount = decimal.Round(amount, 0);
        Currency = currency;
    }

    /// <summary>ساخت مبلغ</summary>
    public static Money Of(decimal amount) => new(amount);

    /// <summary>صفر ریال</summary>
    public static Money Zero => new(0m);

    /// <summary>جمع دو مبلغ</summary>
    public Money Add(Money other) => new(Amount + other.Amount, Currency);

    /// <summary>تفریق دو مبلغ</summary>
    public Money Subtract(Money other) => new(Amount - other.Amount, Currency);

    /// <summary>ضرب در ضریب</summary>
    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    /// <summary>درصدی از مبلغ</summary>
    public Money Percentage(decimal percent) => new(Amount * percent / 100m, Currency);

    /// <summary>آیا مبلغ منفی است</summary>
    public bool IsNegative => Amount < 0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N0} {Currency}";
}
""")

write("ValueObjects/Address.cs", """
using System.Collections.Generic;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.ValueObjects;

/// <summary>
/// آدرس پستی ایران — داده حساس که با AES-256 رمزنگاری می‌شود
/// </summary>
public sealed class Address : ValueObject
{
    /// <summary>متن آدرس</summary>
    public string Street { get; }

    /// <summary>شهر</summary>
    public string? City { get; }

    /// <summary>استان</summary>
    public string? Province { get; }

    /// <summary>کد پستی ۱۰ رقمی</summary>
    public string? PostalCode { get; }

    private Address(string street, string? city, string? province, string? postalCode)
    {
        Street = street;
        City = city;
        Province = province;
        PostalCode = postalCode;
    }

    /// <summary>ساخت آدرس</summary>
    public static Address Of(string street, string? city = null, string? province = null, string? postalCode = null)
        => new(street, city, province, postalCode);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Province;
        yield return PostalCode;
    }

    public override string ToString()
        => $"{Province ?? string.Empty} {City ?? string.Empty} {Street}".Trim();
}
""")

print("✅ بخش ۱: Common / Exceptions / Interfaces / Events / ValueObjects — کامل شد")
