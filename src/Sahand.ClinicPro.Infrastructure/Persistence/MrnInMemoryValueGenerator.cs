using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// مولد شماره پرونده پزشکی در حالت درون‌حافظه‌ای — شبیه‌سازی سکانس SeqPatientMrn
/// با شمارنده ایستا؛ فقط برای تست‌ها و هرگز در SQL Server (رفتار تولید در آنجا سمت سرور است)
/// </summary>
public sealed class MrnInMemoryValueGenerator : ValueGenerator<string>
{
    private static long _counter;

    /// <summary>مقادیر پایدارند — نه موقت</summary>
    public override bool GeneratesTemporaryValues => false;

    /// <summary>تولید مقدار بعدی با قالب MRN-XXXXXX</summary>
    public override string Next(EntityEntry entry)
        => $"MRN-{Interlocked.Increment(ref _counter):D6}";
}