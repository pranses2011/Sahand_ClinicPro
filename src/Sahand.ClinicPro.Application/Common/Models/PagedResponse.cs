namespace Sahand.ClinicPro.Application.Common.Models;

/// <summary>
/// پاسخ صفحه‌بندی‌شده — فرم استاندارد فهرست‌ها با فراداده صفحه
/// </summary>
/// <typeparam name="T">نوع آیتم‌های فهرست</typeparam>
public class PagedResponse<T> : ApiResponse<List<T>>
{
    /// <summary>شماره صفحه جاری — از ۱ شروع می‌شود</summary>
    public int PageNumber { get; init; }

    /// <summary>اندازه صفحه</summary>
    public int PageSize { get; init; }

    /// <summary>کل رکوردهای منطبق</summary>
    public int TotalCount { get; init; }

    /// <summary>کل صفحات</summary>
    public int TotalPages { get; init; }

    /// <summary>آیا صفحه بعدی وجود دارد</summary>
    public bool HasNext { get; init; }

    /// <summary>آیا صفحه قبلی وجود دارد</summary>
    public bool HasPrevious { get; init; }

    /// <summary>ساخت پاسخ صفحه‌بندی‌شده</summary>
    public static PagedResponse<T> Create(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
        return new PagedResponse<T>
        {
            IsSuccess = true,
            StatusCode = 200,
            Message = "فهرست با موفقیت دریافت شد",
            Data = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNext = pageNumber < totalPages,
            HasPrevious = pageNumber > 1
        };
    }
}
