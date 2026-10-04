namespace Sahand.ClinicPro.Application.Common.Models;

/// <summary>
/// درخواست صفحه‌بندی استاندارد همه فهرست‌ها — پارامترهای ?page= و pageSize= و sortBy= و sortDir=
/// </summary>
public class PagedRequest
{
    private const int MaxPageSize = 100;

    private int _pageNumber = 1;
    private int _pageSize = 20;

    /// <summary>شماره صفحه — از ۱</summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>اندازه صفحه — حداکثر ۱۰۰</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 20 : (value > MaxPageSize ? MaxPageSize : value);
    }

    /// <summary>مرتب‌سازی بر اساس ستون</summary>
    public string? SortBy { get; set; }

    /// <summary>جهت مرتب‌سازی — asc یا desc</summary>
    public string? SortDir { get; set; }

    /// <summary>عبارت جستجوی عمومی</summary>
    public string? Search { get; set; }
}
