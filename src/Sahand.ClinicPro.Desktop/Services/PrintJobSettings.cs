namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// کاتالوگ یکپارچهٔ سندهای چاپی برنامه — بازطراحی تأییدشدهٔ مالک:
/// «همهٔ فرم‌ها و صفحه‌هایی که دکمهٔ چاپ دارند یا به چاپ نیاز دارند، با
/// عنوان خودشان فهرست شوند تا برای هر کدام چاپگر جداگانه انتخاب شود».
/// هر سند: چاپگر اختصاصی، اندازهٔ کاغذ، تعداد نسخه، پیش‌نمایش و چاپ
/// خودکارِ خودش را دارد؛ مقدارها در ذخیرهٔ محلی با کلید
/// Printer.Job.{Key}.* نگه داشته می‌شوند و در نبود مقدار، زنجیرهٔ
/// سازگاری با کلیدهای قدیمی (Receipt/Ticket/Default و…) پیموده می‌شود
/// تا رفتار نصب‌های فعلی بی‌تغییر بماند.
/// </summary>
public static class PrintJobSettings
{
    /// <summary>مشخصات یک سند چاپی در کاتالوگ</summary>
    /// <param name="Key">کلید پایدار سند — در کلیدهای ذخیره و فراخوانی چاپ</param>
    /// <param name="Title">عنوان فارسی سند — همان نامی که کاربر در برنامه می‌بیند</param>
    /// <param name="Description">این سند کجا و کی چاپ می‌شود</param>
    /// <param name="Icon">ایموجی عنوان سند در فهرست تنظیمات</param>
    /// <param name="DefaultPaper">اندازهٔ کاغذ پیش‌فرض سند</param>
    /// <param name="LegacyKeys">کلیدهای قدیمی ذخیرهٔ محلی برای سازگاری عقب‌رو</param>
    public sealed record PrintJobInfo(string Key, string Title, string Description, string Icon,
        string DefaultPaper, string[] LegacyKeys);

    /// <summary>همهٔ سندهای چاپی برنامه — به ترتیب بخش‌های کاری</summary>
    public static readonly IReadOnlyList<PrintJobInfo> Jobs = new[]
    {
        new PrintJobInfo("ReceptionReceipt", "قبض پذیرش و پرداخت", "صفحهٔ پذیرش — پس از ثبت و پرداخت", "🧾", "حرارتی ۸۰", new[] { "Printer.Receipt", "Printer.Ticket", "Printer.TicketPrinter" }),
        new PrintJobInfo("AppointmentTicket", "قبض نوبت", "نوبت‌دهی — برگهٔ راهنمای بیمار", "🎫", "حرارتی ۸۰", new[] { "Printer.Ticket", "Printer.TicketPrinter" }),
        new PrintJobInfo("Invoice", "فاکتور", "مالی — فاکتور خدمات و فروش", "📄", "A5", new[] { "Printer.InvoicePrinter" }),
        new PrintJobInfo("PhysioHomeProgram", "تمرین خانگی فیزیوتراپی", "فیزیوتراپی — پیش‌نویس برنامهٔ بیمار", "🏠", "A4", new[] { "Printer.DefaultPrinter" }),
        new PrintJobInfo("PhysioReferralReport", "گزارش ارجاع فیزیوتراپی", "فیزیوتراپی — پیش‌نویس گزارش برای ارجاع‌دهنده", "📄", "A4", new[] { "Printer.DefaultPrinter" }),
        new PrintJobInfo("DentalChart", "چارت دندان", "دندان‌پزشکی — وضعیت ثبت‌شدهٔ دندان‌ها", "🦷", "A4", new[] { "Printer.DefaultPrinter" }),
        new PrintJobInfo("DentalTreatmentPlan", "طرح درمان دندان", "دندان‌پزشکی — پیش‌نویس اقلام طرح", "📋", "A4", new[] { "Printer.DefaultPrinter" }),
        new PrintJobInfo("GlassesPrescription", "پیش‌نویس نسخهٔ عینک", "چشم‌پزشکی — پیش‌نویس نیازمند امضای دستی", "👓", "A5", new[] { "Printer.PrescriptionPrinter" }),
        new PrintJobInfo("Prescription", "نسخهٔ پزشک", "ویزیت و پروندهٔ بیمار", "💊", "A5", new[] { "Printer.PrescriptionPrinter" }),
        new PrintJobInfo("LabResult", "نتیجهٔ آزمایش", "آزمایشگاه — گزارش جواب بیمار", "🧪", "A4", new[] { "Printer.DefaultPrinter" }),
        new PrintJobInfo("SpecimenLabel", "برچسب نمونه (QR)", "آزمایشگاه — برچسب ظرف نمونه", "🏷️", "برچسب ۴۰×۳۰", new[] { "Printer.DefaultPrinter" }),
        new PrintJobInfo("ImagingReport", "گزارش تصویربرداری", "رادیولوژی — متن گزارش مطالعه", "🩻", "A4", new[] { "Printer.DefaultPrinter" }),
        new PrintJobInfo("ShiftReceipt", "رسید بستن شیفت", "صندوق — گزارش پایان شیفت", "💼", "حرارتی ۸۰", new[] { "Printer.Receipt", "Printer.Ticket" }),
        new PrintJobInfo("Payslip", "فیش حقوقی", "پرسنل و حقوق", "💰", "A5", new[] { "Printer.DefaultPrinter" }),
        new PrintJobInfo("MedicalCertificate", "گواهی پزشکی", "ویزیت — استراحت و سلامت", "📋", "A5", new[] { "Printer.PrescriptionPrinter" }),
        new PrintJobInfo("PatientCard", "کارت بیمار", "پروندهٔ بیمار — کارت شناسایی", "🪪", "A5", new[] { "Printer.DefaultPrinter" }),
        new PrintJobInfo("Report", "گزارش‌های مدیریتی", "گزارش‌ها — چاپ جدولی و مدیریتی", "📊", "A4", new[] { "Printer.DefaultPrinter" }),
    };

    /// <summary>یافتن مشخصات سند با کلید</summary>
    public static PrintJobInfo? Find(string key) => Jobs.FirstOrDefault(j => j.Key == key);

    /// <summary>کلید محلی تنظیم سند برای ثبت گروهی.</summary>
    public static string StorageKey(string jobKey, string part) => $"Printer.Job.{jobKey}.{part}";

    /// <summary>
    /// چاپگر اختصاصی سند — اول مقدار اختصاصی همین سند، بعد کلیدهای قدیمی
    /// همان خانواده، و در پایان تهی یعنی «پیش‌فرض ویندوز».
    /// </summary>
    public static string? PrinterFor(string jobKey)
        => ResolvePrinter(PrinterOverrideFor(jobKey), LegacyPrintersFor(jobKey));

    /// <summary>فقط انتخاب اختصاصی؛ خالی به معنای ارث از تنظیمات قدیمی است.</summary>
    public static string PrinterOverrideFor(string jobKey) => LocalSettingsStore.Get(StorageKey(jobKey, "Printer"));

    /// <summary>چاپگر ارثی، بدون لحاظ انتخاب اختصاصی ذخیره‌شده.</summary>
    public static string? FallbackPrinterFor(string jobKey) => ResolvePrinter(null, LegacyPrintersFor(jobKey));

    private static IEnumerable<string> LegacyPrintersFor(string jobKey)
        => (Find(jobKey)?.LegacyKeys ?? Array.Empty<string>()).Select(LocalSettingsStore.Get);

    /// <summary>انتخاب اختصاصی مقدم است؛ در نبود آن اولین مقدار قدیمی و سپس پیش‌فرض ویندوز.</summary>
    public static string? ResolvePrinter(string? own, IEnumerable<string?> legacy)
        => !string.IsNullOrWhiteSpace(own) ? own : legacy.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    /// <summary>ذخیرهٔ چاپگر اختصاصی سند</summary>
    public static void SetPrinter(string jobKey, string? printer)
        => LocalSettingsStore.Set(StorageKey(jobKey, "Printer"), printer ?? string.Empty);

    /// <summary>اندازهٔ کاغذ سند — پیش‌فرض از کاتالوگ</summary>
    public static string PaperFor(string jobKey)
    {
        var own = LocalSettingsStore.Get(StorageKey(jobKey, "Paper"));
        return !string.IsNullOrWhiteSpace(own) ? own : Find(jobKey)?.DefaultPaper ?? "A4";
    }

    /// <summary>ذخیرهٔ اندازهٔ کاغذ سند</summary>
    public static void SetPaper(string jobKey, string? paper)
        => LocalSettingsStore.Set(StorageKey(jobKey, "Paper"), paper ?? string.Empty);

    /// <summary>تعداد نسخهٔ سند — حداقل ۱</summary>
    public static int CopiesFor(string jobKey)
        => int.TryParse(LocalSettingsStore.Get(StorageKey(jobKey, "Copies")), out var c)
            ? Math.Clamp(c, 1, 9) : 1;

    /// <summary>ذخیرهٔ تعداد نسخهٔ سند</summary>
    public static void SetCopies(string jobKey, int copies)
        => LocalSettingsStore.Set(StorageKey(jobKey, "Copies"), Math.Clamp(copies, 1, 9).ToString());

    /// <summary>
    /// پیش‌نمایش اختصاصی سند — «inherit» یعنی از تنظیم عمومی (PrintFlow)؛
    /// true/false یعنی همین سند همیشه/هرگز پیش‌نمایش داشته باشد.
    /// </summary>
    public static bool? PreviewFor(string jobKey)
    {
        var own = LocalSettingsStore.Get(StorageKey(jobKey, "Preview"));
        return own switch { "true" => true, "false" => false, _ => null };
    }

    /// <summary>ذخیرهٔ پیش‌نمایش اختصاصی سند — تهی یعنی ارث از عمومی</summary>
    public static void SetPreview(string jobKey, bool? preview)
        => LocalSettingsStore.Set(StorageKey(jobKey, "Preview"),
            preview is null ? "inherit" : preview.Value ? "true" : "false");

    /// <summary>چاپ خودکار سند (مثلاً قبض نوبت پس از ثبت)</summary>
    public static bool AutoFor(string jobKey)
        => LocalSettingsStore.Get(StorageKey(jobKey, "Auto")) == "true";

    /// <summary>ذخیرهٔ چاپ خودکار سند</summary>
    public static void SetAuto(string jobKey, bool auto)
        => LocalSettingsStore.Set(StorageKey(jobKey, "Auto"), auto ? "true" : "false");
}
