using System.Text;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.HR;

/// <summary>
/// حل سروری «سمت» و «دپارتمان» تایپشده در فرم پرسنل — چرخهٔ جاری
/// (گزارش مالک: «در تعریف و ویرایش پرسنل، فیلد سمت رو که دستی تایپ
/// میکنم در دیتابیس ذخیره نمیشه و همون مقدار قبلی روش میمونه»).
///
/// چرا لازم شد:
///  ۱) کلاینت WPF قبلاً ایندکس کهنهٔ کامبو را بر متن تایپشده مقدم میداشت؛
///  ۲) مسیر قدیمی ساخت مقدار تازه (<c>POST lookups/positions</c>) فقط برای
///     نقشهای Admin/Manager مجاز بود، در حالی که ذخیرهٔ پرسنل با دسترسی
///     <c>patients.create</c> انجام میشود؛ در نتیجه کاربران معمولی نمیتوانستند
///     سمت تازهای تعریف کنند.
///
/// قرارداد تازه: کلاینت نام نرمالشده را در <c>positionName</c> /
/// <c>departmentName</c> میفرستد. اگر نام رسیده باشد، <b>نام مقدم بر شناسه
/// است</b> و سرور رکورد مرجع را پیدا یا ایجاد میکند؛ اگر نامی نرسد (کلاینت
/// قدیمی)، همان شناسهٔ ارسالی معتبر است. نام تهی/«—» یعنی پاککردن آگاهانه.
/// </summary>
public static class EmployeeLookupResolver
{
    /// <summary>نشانگر «بدون مقدار» در فهرستها</summary>
    public const string EmptyMarker = "—";

    /// <summary>
    /// نرمالسازی نام: یکدستکردن حروف عربی/فارسی، حذف نیمفاصلهٔ مجازی و
    /// کاراکترهای نامرئی، و فشردهسازی فاصلهها — همان قرارداد کلاینت WPF.
    /// </summary>
    public static string? NormalizeName(string? value) => value?.Trim();

    /// <summary>آیا نام نرمالشده نشانگر پاککردن مقدار است؟</summary>
    public static bool IsEmptyMarker(string? normalized)
        => string.IsNullOrEmpty(normalized) || normalized == EmptyMarker;

    /// <summary>مقایسهٔ بیتفاوت به نویسههای عربی/فارسی و حالت حروف</summary>
    public static bool NamesEqual(string? left, string? right)
        => string.Equals(NormalizeName(left), NormalizeName(right),
            StringComparison.OrdinalIgnoreCase);

    /// <summary>تصمیم نهایی حل یک مقدار مرجع</summary>
    public enum Resolution
    {
        /// <summary>نامی نرسیده (کلاینت قدیمی) — همان شناسه معتبر است</summary>
        UseId,

        /// <summary>کاربر آگاهانه مقدار را پاک کرده است</summary>
        Clear,

        /// <summary>نام تایپشده مقدم است — پیدا یا ساخت رکورد مرجع</summary>
        FindOrCreate
    }

    /// <summary>
    /// قانون تقدم — همان چیزی که باگ «همون مقدار قبلی روش میمونه» را میبندد:
    /// تا وقتی نامی از طرف کلاینت رسیده باشد، شناسهٔ کهنه هیچ اعتباری ندارد.
    /// </summary>
    public static (Resolution Kind, string? Name) Decide(long? id, string? name)
    {
        if (name is null) return (Resolution.UseId, null);
        var normalized = NormalizeName(name);
        if (IsEmptyMarker(normalized)) return (Resolution.Clear, null);
        return (Resolution.FindOrCreate, normalized);
    }

    /// <summary>
    /// حل سمت: نام (در صورت وجود) مقدم است؛ در نبود نام، شناسهٔ کلاینتهای
    /// قدیمی معتبر میماند. مقدار تازه در همان واحد کار ساخته و ذخیره میشود.
    /// </summary>
    public static async Task<long?> ResolvePositionIdAsync(
        IUnitOfWork unitOfWork, long? id, string? name, CancellationToken ct)
    {
        var (kind, title) = Decide(id, name);
        if (kind == Resolution.UseId) return id;
        if (kind == Resolution.Clear || title is null) return null;

        // مقایسه در حافظه انجام میشود تا collation نامشخص SQL Server
        // (فارسی/عربی/حساس به حالت) روی نتیجه اثر نگذارد.
        var existing = await unitOfWork.Repository<Position>().QueryableNoTracking
            .Select(p => new { p.Id, p.Title })
            .ToListAsync(ct);
        var hit = existing.FirstOrDefault(p => NamesEqual(p.Title, title));
        if (hit is not null) return hit.Id;

        var created = new Position { Title = title, CreatedAt = DateTime.UtcNow };
        await unitOfWork.Repository<Position>().AddAsync(created, ct);
        await unitOfWork.SaveEntitiesAsync(ct);
        return created.Id;
    }

    /// <summary>حل دپارتمان — همان قرارداد <see cref="ResolvePositionIdAsync"/>.</summary>
    public static async Task<long?> ResolveDepartmentIdAsync(
        IUnitOfWork unitOfWork, long? id, string? name, CancellationToken ct)
    {
        var (kind, deptName) = Decide(id, name);
        if (kind == Resolution.UseId) return id;
        if (kind == Resolution.Clear || deptName is null) return null;

        var existing = await unitOfWork.Repository<Department>().QueryableNoTracking
            .Select(d => new { d.Id, d.Name })
            .ToListAsync(ct);
        var hit = existing.FirstOrDefault(d => NamesEqual(d.Name, deptName));
        if (hit is not null) return hit.Id;

        var created = new Department { Name = deptName, CreatedAt = DateTime.UtcNow };
        await unitOfWork.Repository<Department>().AddAsync(created, ct);
        await unitOfWork.SaveEntitiesAsync(ct);
        return created.Id;
    }
}
