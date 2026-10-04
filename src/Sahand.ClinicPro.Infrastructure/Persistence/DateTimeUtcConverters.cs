using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// مبدل DateTime به UTC — همه زمان‌ها به‌صورت جهانی ذخیره می‌شوند
/// </summary>
public class DateTimeToUtcConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>ساخت مبدل با اعمال Kind از نوع Utc</summary>
    public DateTimeToUtcConverter() : this(null) { }

    /// <summary>ساخت مبدل با نگاشت مشخص</summary>
    public DateTimeToUtcConverter(ConverterMappingHints? mappingHints)
        : base(v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
               v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
               mappingHints)
    {
    }
}

/// <summary>
/// مبدل DateTime تهی‌پذیر به UTC
/// </summary>
public class NullableDateTimeToUtcConverter : ValueConverter<DateTime?, DateTime?>
{
    /// <summary>ساخت مبدل با اعمال Kind از نوع Utc</summary>
    public NullableDateTimeToUtcConverter() : this(null) { }

    /// <summary>ساخت مبدل با نگاشت مشخص</summary>
    public NullableDateTimeToUtcConverter(ConverterMappingHints? mappingHints)
        : base(v => v.HasValue
               ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime())
               : v,
               v => v.HasValue
               ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
               : v,
               mappingHints)
    {
    }
}
