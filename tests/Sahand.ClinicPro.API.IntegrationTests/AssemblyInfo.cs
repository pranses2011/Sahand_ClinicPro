using Xunit;

// گام V12 — غیرفعال‌سازی موازی‌سازی مجموعه‌ها:
// دو کارخانه WebApplicationFactory (Api و Flow) روی لاگر استاتیک Serilog
// دو‌مرحله‌ای انجماد می‌بینند؛ اجرای ترتیبی تداخل را حذف می‌کند.
[assembly: CollectionBehavior(DisableTestParallelization = true)]