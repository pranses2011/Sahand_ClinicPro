using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف سوابق ویزیت در پرونده بیمار</summary>
public record PatientVisitRow(string DateShamsi, string DoctorName, string Diagnosis, string Status);

/// <summary>ردیف نسخه‌های بیمار</summary>
public record PatientPrescriptionRow(string DateShamsi, string DoctorName, int ItemsCount, string Status);

/// <summary>ردیف نوبت‌های بیمار — چرخه بیست‌وسوم (گزارش شانزدهم مالک): تب نوبت‌ها واقعی شد</summary>
public record PatientAppointmentRow(string DateShamsi, string Time, string DoctorName, string Specialty, string Status);

/// <summary>ردیف فاکتورهای بیمار — چرخه بیست‌وسوم: تب فاکتورها واقعی شد (مبالغ با جداکنندهٔ ۳ رقمی)</summary>
public record PatientInvoiceRow(string InvoiceNumber, string DateShamsi, string TotalAmount, string PaidAmount, string Status);

/// <summary>ردیف آزمایش‌های بیمار — چرخه بیست‌وسوم: تب آزمایش‌ها واقعی شد</summary>
public record PatientLabRow(string OrderNumber, string DateShamsi, string DoctorName, int ItemsCount, string Status);

/// <summary>ردیف اسناد/سوابق آرشیوی بیمار — چرخه بیست‌وسوم: تب اسناد به آرشیو سرور وصل شد</summary>
public record PatientDocumentRow(string SourceTable, string Title, string DateShamsi, string Summary);

/// <summary>ردیف نوار نتایج غیرعادی پاراکلینیک — چرخه ۱۳ (درخواست ۱-۴)</summary>
public record AbnormalResultRow(
    string Title,
    string Value,
    string Range,
    string FlagText,
    string FlagColor,
    string DateShamsi,
    string Kind);

/// <summary>ردیف رویداد خط زمان — چرخه ۱۳ (درخواست ۱-۵)</summary>
public record TimelineRow(
    string Kind,
    string KindLabel,
    string Title,
    string Subtitle,
    string DateShamsi,
    string TimeShamsi,
    string DotColor);

/// <summary>
/// ViewModel پرونده بیمار — گام U2 (فرم ۵): کارت سربرگ + هشدار آلرژی برجسته +
/// تب‌های سوابق (ویزیت/نسخه از endpointهای موجود + پیام گام‌های بعدی برای بقیه)
/// </summary>
public partial class PatientDetailViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /* ── کارت سربرگ ── */
    [ObservableProperty] private string _fullName = "بارگذاری…";
    [ObservableProperty] private string _medicalRecordNo = string.Empty;
    [ObservableProperty] private string _nationalCode = string.Empty;
    [ObservableProperty] private string _mobile = string.Empty;
    [ObservableProperty] private string _ageText = "—";
    [ObservableProperty] private string _insuranceName = "—";
    [ObservableProperty] private string _bloodType = "—";
    [ObservableProperty] private string _genderText = "—";
    [ObservableProperty] private string _phone = "—";
    [ObservableProperty] private string _email = "—";
    [ObservableProperty] private string _city = "—";
    [ObservableProperty] private string _address = "—";
    [ObservableProperty] private string _insuranceNumber = "—";

    /// <summary>تگ‌های آلرژی — هشدار قرمز</summary>
    public ObservableCollection<string> Allergies { get; } = new();

    /// <summary>تگ‌های بیماری مزمن — نارنجی</summary>
    public ObservableCollection<string> ChronicDiseases { get; } = new();

    /// <summary>تگ‌های داروهای فعلی — آبی</summary>
    public ObservableCollection<string> CurrentMedications { get; } = new();

    /// <summary>سوابق ویزیت — از endpoint visits/patient/{id}</summary>
    public ObservableCollection<PatientVisitRow> Visits { get; } = new();

    /// <summary>نسخه‌های بیمار — از endpoint prescriptions?patientId=</summary>
    public ObservableCollection<PatientPrescriptionRow> Prescriptions { get; } = new();

    /// <summary>نوبت‌های بیمار — از endpoint appointments/search با فیلتر patientId — چرخه بیست‌وسوم</summary>
    public ObservableCollection<PatientAppointmentRow> Appointments { get; } = new();

    /// <summary>فاکتورهای بیمار — از endpoint invoices?patientId= — چرخه بیست‌وسوم</summary>
    public ObservableCollection<PatientInvoiceRow> Invoices { get; } = new();

    /// <summary>آزمایش‌های بیمار — از endpoint lab/orders?patientId= — چرخه بیست‌وسوم</summary>
    public ObservableCollection<PatientLabRow> Labs { get; } = new();

    /// <summary>اسناد/سوابق آرشیوی بیمار — از endpoint admin/archive/records — چرخه بیست‌وسوم</summary>
    public ObservableCollection<PatientDocumentRow> Documents { get; } = new();

    /// <summary>
    /// خطای بارگذاری — چرخه بیست‌وسوم: پیش‌تر هر شکست شبکه بی‌صدا قورت داده می‌شد و
    /// پنجره کاملاً خالی باز می‌ماند؛ اکنون نخستین خطا در بالای پنجره دیده می‌شود
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>نوار نتایج غیرعادی پاراکلینیک — چرخه ۱۳</summary>
    public ObservableCollection<AbnormalResultRow> AbnormalResults { get; } = new();

    /// <summary>تعداد نتایج غیرعادی — برای نمایش نوار</summary>
    [ObservableProperty]
    private int _abnormalCount;

    /// <summary>متن خلاصه نوار — مثل «۳ آزمایش غیرطبیعی + ۱ یافته تصویری»</summary>
    [ObservableProperty]
    private string _abnormalSummaryText = string.Empty;

    /// <summary>رویدادهای خط زمان — چرخه ۱۳ (درخواست ۱-۵)</summary>
    public ObservableCollection<TimelineRow> Timeline { get; } = new();

    /// <summary>فیلترهای فعال خط زمان — رشته نوع‌ها با کاما</summary>
    [ObservableProperty]
    private string? _timelineFilterKinds;

    /// <summary>سال شمسی فیلتر خط زمان — صفر یعنی همه</summary>
    [ObservableProperty]
    private int _timelineYearFilter;

    /// <summary>سال‌های موجود برای فیلتر خط زمان</summary>
    public ObservableCollection<int> TimelineYears { get; } = new();

    /// <summary>چک‌باکس‌های فیلتر نوع خط زمان</summary>
    [ObservableProperty] private bool _tlVisits = true;
    [ObservableProperty] private bool _tlLabs = true;
    [ObservableProperty] private bool _tlImaging = true;
    [ObservableProperty] private bool _tlPrescriptions = true;
    [ObservableProperty] private bool _tlReceptions = true;
    [ObservableProperty] private bool _tlInvoices = true;
    [ObservableProperty] private bool _tlPayments = true;

    partial void OnTlVisitsChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlLabsChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlImagingChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlPrescriptionsChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlReceptionsChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlInvoicesChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlPaymentsChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTimelineYearFilterChanged(int value) => _ = LoadTimelineAsync(_patientIdCache);

    private long _patientIdCache;

    /// <summary>سازنده با شناسه بیمار</summary>
    public PatientDetailViewModel(IApiClient api, long patientId)
    {
        _api = api;
        _patientIdCache = patientId;
        _ = LoadAsync(patientId);
    }

    /// <summary>بارگذاری پرونده + سوابق</summary>
    private async Task LoadAsync(long patientId)
    {
        // ۱) پرونده اصلی — چرخه بیستم (گزارش چهاردهم مالک): پاسخ سرور در پاکت
        // استاندارد ApiResponse (isSuccess/message/data) پیچیده می‌شود؛ پیش‌تر
        // مستقیم روی PatientDetailDto نگاشت می‌شد، فیلد Data خوانده نمی‌شد و
        // همهٔ مشخصات خالی می‌ماند (پروندهٔ بیمار بدون مشخصات باز می‌شد)
        var envelope = await _api.GetAsync<PatientDetailEnvelope>($"patients/{patientId}");
        var patient = envelope?.Data;
        if (patient is null)
            return;

        FullName = $"{patient.FirstName} {patient.LastName}".Trim();
        MedicalRecordNo = patient.MedicalRecordNo;
        NationalCode = patient.NationalCode;
        Mobile = patient.Mobile;
        AgeText = patient.Age is { } age ? $"{age} سال" : "—";
        InsuranceName = patient.InsuranceName ?? "آزاد";
        BloodType = patient.BloodType ?? "نامشخص";
        GenderText = patient.Gender == 2 ? "زن" : "مرد";
        Phone = patient.Phone ?? "—";
        Email = patient.Email ?? "—";
        City = string.IsNullOrWhiteSpace(patient.City) ? "—" : patient.City;
        Address = string.IsNullOrWhiteSpace(patient.Address) ? "—" : patient.Address;
        InsuranceNumber = patient.InsuranceNumber ?? "—";

        FillTags(Allergies, patient.Allergies);
        FillTags(ChronicDiseases, patient.ChronicDiseases);
        FillTags(CurrentMedications, patient.CurrentMedications);

        // چرخه بیست‌وسوم: هر بخش در try/catch مستقل خودش است تا شکست یک endpoint
        // بقیهٔ تب‌ها را خالی نکند (پیش‌تر نخستین خطا کل بارگذاری را قطع می‌کرد —
        // علامت «هیچ‌کدام از تب‌ها اطلاعاتی نشان نمی‌دهد» همین بود) و پیام خطای
        // نخستین بخشِ ناموفق بالای پنجره دیده می‌شود.

        // ۲) سوابق ویزیت
        {
            var visits = await SectionAsync<VisitsEnvelope>("سوابق ویزیت", $"visits/patient/{patientId}");
            if (visits?.Data is not null)
            {
                foreach (var v in visits.Data)
                {
                    Visits.Add(new PatientVisitRow(
                        v.VisitDateShamsi ?? "—",
                        v.DoctorName ?? "—",
                        v.ChiefComplaint ?? "—",
                        v.Status ?? "—"));
                }
            }
        }

        // ۳) نسخه‌ها
        {
            var prescriptions = await SectionAsync<PrescriptionsEnvelope>("نسخه‌ها",
                $"prescriptions?patientId={patientId}&pageSize=50");
            if (prescriptions?.Data is not null)
            {
                foreach (var p in prescriptions.Data)
                {
                    Prescriptions.Add(new PatientPrescriptionRow(
                        p.PrescriptionDateShamsi ?? "—",
                        p.DoctorName ?? "—",
                        p.ItemsCount,
                        p.Status ?? "—"));
                }
            }
        }

        // ۳-ب) نوبت‌ها — چرخه بیست‌وسوم (گزارش شانزدهم مالک): این تب پیش‌تر جای‌خالی
        // «در گام U4 متصل می‌شود» بود؛ اکنون از جستجوی پیشرفتهٔ نوبت‌ها با فیلتر
        // شناسهٔ بیمار (پارامتر تازهٔ سرور) همهٔ نوبت‌های بیمار را می‌گیرد
        {
            var appts = await PostSectionAsync<AppointmentsSearchPage>("نوبت‌ها",
                "appointments/search",
                new { patientId = patientId, pageNumber = 1, pageSize = 50 });
            if (appts?.Data is not null)
            {
                foreach (var a in appts.Data)
                {
                    Appointments.Add(new PatientAppointmentRow(
                        a.DateShamsi ?? "—",
                        a.Time ?? "—",
                        a.DoctorName ?? "—",
                        a.SpecialtyName ?? "—",
                        AppointmentStatusText(a.Status)));
                }
            }
        }

        // ۳-پ) فاکتورها — چرخه بیست‌وسوم: اتصال به فاکتورهای بیمار با فرمت مبالغ ۳ رقمی
        {
            var invoices = await SectionAsync<InvoicesPage>("فاکتورها",
                $"invoices?patientId={patientId}&pageSize=50");
            if (invoices?.Data is not null)
            {
                foreach (var i in invoices.Data)
                {
                    Invoices.Add(new PatientInvoiceRow(
                        i.InvoiceNumber ?? "—",
                        i.DateShamsi ?? "—",
                        Money.Format(i.TotalAmount),
                        Money.Format(i.PaidAmount),
                        InvoiceStatusText(i.PaymentStatus, i.Status)));
                }
            }
        }

        // ۳-ت) آزمایش‌ها — چرخه بیست‌وسوم: اتصال به درخواست‌های آزمایش بیمار
        {
            var labs = await SectionAsync<LabsEnvelope>("آزمایش‌ها", $"lab/orders?patientId={patientId}");
            if (labs?.Data is not null)
            {
                foreach (var l in labs.Data)
                {
                    Labs.Add(new PatientLabRow(
                        l.OrderNumber ?? "—",
                        l.DateShamsi ?? "—",
                        l.DoctorName ?? "—",
                        l.ItemsCount,
                        LabStatusText(l.Status)));
                }
            }
        }

        // ۳-ث) اسناد — چرخه بیست‌وسوم: سوابق آرشیوی سرور برای این بیمار
        // (اگر نقش کاربر اجازهٔ settings.view را نداشته باشد، تب با پیام مناسب خالی می‌ماند)
        {
            var archive = await SectionAsync<ArchiveEnvelope>("اسناد",
                $"admin/archive/records?patientId={patientId}&take=50");
            if (archive?.Data is not null)
            {
                foreach (var d in archive.Data)
                {
                    Documents.Add(new PatientDocumentRow(
                        ArchiveSourceText(d.SourceTable),
                        d.PatientName ?? "—",
                        d.RecordDate is { } rd ? PersianDateHelper.ToShamsiFa(rd) : "—",
                        Truncate(d.SummaryLine, 80)));
                }
            }
        }

        // ۴) نوار نتایج غیرعادی پاراکلینیک — چرخه ۱۳ (درخواست ۱-۴)
        try
        {
            var abnormal = await _api.GetAsync<AbnormalEnvelope>($"patients/{patientId}/abnormal-summary");
            if (abnormal?.Data is { HasAbnormal: true })
            {
                AbnormalCount = abnormal.Data.TotalCount;
                var parts = new List<string>();
                if (abnormal.Data.LabCount > 0)
                    parts.Add($"{abnormal.Data.LabCount} آزمایش غیرطبیعی");
                if (abnormal.Data.ImagingCount > 0)
                    parts.Add($"{abnormal.Data.ImagingCount} یافته تصویری هشدار");
                AbnormalSummaryText = string.Join(" + ", parts);
                foreach (var a in abnormal.Data.Items ?? new List<AbnormalItemApi>())
                {
                    var value = a.Value is not null
                        ? $"{a.Value}{(string.IsNullOrWhiteSpace(a.Unit) ? "" : " " + a.Unit)}"
                        : (a.ResultText is { Length: > 46 } ? a.ResultText[..46] + "…" : a.ResultText ?? "—");
                    AbnormalResults.Add(new AbnormalResultRow(
                        a.Title ?? "—", value,
                        string.IsNullOrWhiteSpace(a.NormalRange) ? "—" : $"نرمال: {a.NormalRange}",
                        string.IsNullOrWhiteSpace(a.FlagText) ? "خارج از محدوده" : a.FlagText,
                        string.IsNullOrWhiteSpace(a.FlagColor) ? "#FFF3E0" : a.FlagColor,
                        a.DateShamsi ?? "—",
                        a.Kind == "imaging" ? "🩻 تصویر" : "🧪 آزمایش"));
                }
            }
            else
            {
                AbnormalCount = 0;
            }
        }
        catch (Exception ex)
        {
            RecordSectionError("نتایج پاراکلینیک", ex);
        }

        // ۵) خط زمان رویدادها — چرخه ۱۳ (درخواست ۱-۵)
        try
        {
            await LoadTimelineAsync(patientId);
        }
        catch (Exception ex)
        {
            RecordSectionError("خط زمان", ex);
        }
    }

    /// <summary>
    /// ثبت خطای یک بخش — فقط نخستین خطا نمایش داده می‌شود (از شلوغی جلوگیری می‌شود)
    /// و استثنا فقط در خروجی دیباگ ثبت می‌گردد تا جریان کار نگیرد
    /// </summary>
    private void RecordSectionError(string sectionName, Exception ex)
    {
        if (string.IsNullOrWhiteSpace(ErrorMessage))
            ErrorMessage = $"بارگذاری «{sectionName}» ناموفق بود: {_api.LastError ?? ex.Message}";
        System.Diagnostics.Debug.WriteLine($"[PatientDetail] {sectionName}: {ex.Message}");
    }

    /// <summary>
    /// درخواست GET امن یک بخش — چرخه بیست‌وسوم: کلاینت API در خطا استثنا نمی‌پرتابد بلکه
    /// null برمی‌گرداند و LastError را می‌نهد؛ این هلپر نتیجهٔ تهی را به خطای بخش ترجمه
    /// می‌کند تا شکست یک endpoint بقیهٔ تب‌ها را خالی نگذارد و علتش هم دیده شود
    /// </summary>
    /// <typeparam name="T">نوع پاکت پاسخ</typeparam>
    /// <param name="sectionName">نام فارسی بخش برای پیام خطا</param>
    /// <param name="endpoint">مسیر endpoint</param>
    /// <returns>پاکت یا null</returns>
    private async Task<T?> SectionAsync<T>(string sectionName, string endpoint) where T : class
    {
        var result = await _api.GetAsync<T>(endpoint);
        if (result is null)
            RecordSectionError(sectionName,
                new InvalidOperationException(_api.LastError ?? "پاسخ تهی بود"));
        return result;
    }

    /// <summary>درخواست POST امن یک بخش — همتای SectionAsync برای مسیرهای POST</summary>
    private async Task<T?> PostSectionAsync<T>(string sectionName, string endpoint, object body) where T : class
    {
        var result = await _api.PostAsync<T>(endpoint, body);
        if (result is null)
            RecordSectionError(sectionName,
                new InvalidOperationException(_api.LastError ?? "پاسخ تهی بود"));
        return result;
    }

    /// <summary>متن فارسی وضعیت نوبت — شمارندهٔ سرور: ۱=زمان‌بندی ۲=تأییدشده ۳=حاضر ۴=تکمیل ۵=لغو ۶=عدم حضور</summary>
    private static string AppointmentStatusText(int? status) => status switch
    {
        1 => "زمان‌بندی",
        2 => "تأییدشده",
        3 => "حاضر",
        4 => "تکمیل‌شده",
        5 => "لغو",
        6 => "عدم حضور",
        _ => "—"
    };

    /// <summary>متن فارسی وضعیت پرداخت/فاکتور — چرخه بیست‌وسوم</summary>
    private static string InvoiceStatusText(int? paymentStatus, int? status)
    {
        // paymentStatus: 0=پرداخت‌نشده 1=پرداخت‌جزئی 2=پرداخت‌شده (شمارش enum PaymentStatus)
        var pay = paymentStatus switch
        {
            1 => "پرداخت جزئی",
            2 => "پرداخت‌شده",
            _ => "پرداخت‌نشده"
        };
        // status فاکتور: 0=پیش‌نویس 1=نهایی 2=باطل (شمارش enum InvoiceStatus)
        var st = status switch
        {
            0 => "پیش‌نویس",
            2 => "باطل‌شده",
            _ => "نهایی"
        };
        return $"{st} — {pay}";
    }

    /// <summary>متن فارسی وضعیت درخواست آزمایش — چرخه بیست‌وسوم</summary>
    private static string LabStatusText(int? status) => status switch
    {
        0 => "در انتظار",
        1 => "در حال انجام",
        2 => "نتیجه‌شده",
        3 => "تأییدشده",
        _ => "—"
    };

    /// <summary>عنوان فارسی جدول منبع آرشیو — چرخه بیست‌وسوم</summary>
    private static string ArchiveSourceText(string? source) => source switch
    {
        "Visits" => "ویزیت",
        "Prescriptions" => "نسخه",
        "LabOrders" => "آزمایش",
        "ImagingOrders" => "تصویربرداری",
        "Receptions" => "پذیرش",
        "Invoices" => "فاکتور",
        _ => source ?? "—"
    };

    /// <summary>کوتاه‌کردن متن خلاصه برای ستون باریک — با سه‌نقطهٔ فارسی</summary>
    private static string Truncate(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "—";
        return text.Length <= max ? text : text[..max] + "…";
    }

    /// <summary>بارگذاری خط زمان با فیلترهای فعال</summary>
    private async Task LoadTimelineAsync(long patientId)
    {
        var parts = new List<string>();
        if (TlVisits) parts.Add("visit");
        if (TlLabs) parts.Add("lab");
        if (TlImaging) parts.Add("imaging");
        if (TlPrescriptions) parts.Add("prescription");
        if (TlReceptions) parts.Add("reception");
        if (TlInvoices) parts.Add("invoice");
        if (TlPayments) parts.Add("payment");
        var qs = parts.Count == 7 ? "" : $"?kinds={string.Join(",", parts)}";
        if (TimelineYearFilter > 0)
            qs += (qs.Length == 0 ? "?" : "&") + $"yearShamsi={TimelineYearFilter}";

        var tl = await _api.GetAsync<TimelineEnvelope>($"patients/{patientId}/timeline{qs}");
        if (tl?.Data is null) return;

        Timeline.Clear();
        TimelineYears.Clear();
        TimelineYears.Add(0); // همه
        foreach (var y in tl.Data.YearsShamsi ?? new List<int>())
            TimelineYears.Add(y);
        foreach (var e in tl.Data.Events ?? new List<TimelineEventApi>())
            Timeline.Add(new TimelineRow(
                e.Kind ?? "",
                e.KindLabel ?? "",
                e.Title ?? "—",
                e.Subtitle ?? "",
                e.DateShamsi ?? "—",
                e.TimeShamsi ?? "",
                DotColorOf(e.Kind)));
    }

    /// <summary>رنگ نقطه رویداد بر اساس نوع</summary>
    private static string DotColorOf(string? kind) => kind switch
    {
        "visit" => "#1E88E5",
        "lab" => "#8E24AA",
        "imaging" => "#00897B",
        "prescription" => "#43A047",
        "reception" => "#FB8C00",
        "invoice" => "#E53935",
        "payment" => "#3949AB",
        _ => "#78909C"
    };

    /// <summary>پر کردن مجموعه تگ از رشته JSON</summary>
    private static void FillTags(ObservableCollection<string> target, string? json)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(json);
            if (items is not null)
                foreach (var item in items)
                    target.Add(item);
        }
        catch
        {
            target.Add(json);
        }
    }

    /* ── DTOهای سبک ── */

    /// <summary>پاسخ نوار نتایج غیرعادی — چرخه ۱۳</summary>
    public sealed class AbnormalEnvelope
    {
        public bool IsSuccess { get; set; }
        public AbnormalData? Data { get; set; }
    }

    /// <summary>داده نوار نتایج غیرعادی</summary>
    public sealed class AbnormalData
    {
        public int TotalCount { get; set; }
        public int LabCount { get; set; }
        public int ImagingCount { get; set; }
        public bool HasAbnormal { get; set; }
        public List<AbnormalItemApi>? Items { get; set; }
    }

    /// <summary>ردیف نوار نتیجه غیرعادی</summary>
    public sealed class AbnormalItemApi
    {
        public string? Title { get; set; }
        public decimal? Value { get; set; }
        public string? ResultText { get; set; }
        public string? NormalRange { get; set; }
        public string? Unit { get; set; }
        public string? FlagText { get; set; }
        public string? FlagColor { get; set; }
        public string? DateShamsi { get; set; }
        public string? Kind { get; set; }
    }

    /// <summary>پاسخ خط زمان — چرخه ۱۳</summary>
    public sealed class TimelineEnvelope
    {
        public bool IsSuccess { get; set; }
        public TimelineData? Data { get; set; }
    }

    /// <summary>داده خط زمان</summary>
    public sealed class TimelineData
    {
        public int TotalCount { get; set; }
        public List<int>? YearsShamsi { get; set; }
        public List<TimelineEventApi>? Events { get; set; }
    }

    /// <summary>رویداد خط زمان</summary>
    public sealed class TimelineEventApi
    {
        public string? Kind { get; set; }
        public string? KindLabel { get; set; }
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? DateShamsi { get; set; }
        public string? TimeShamsi { get; set; }
    }

    /// <summary>پاسخ فهرست ویزیت‌های بیمار</summary>
    public sealed class VisitsEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<VisitRow>? Data { get; set; }
    }

    /// <summary>ردیف ویزیت</summary>
    public sealed class VisitRow
    {
        public string? VisitDateShamsi { get; set; }
        public string? DoctorName { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>پاسخ فهرست نسخه‌های بیمار</summary>
    public sealed class PrescriptionsEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<PrescriptionRowDto>? Data { get; set; }
    }

    /// <summary>ردیف نسخه</summary>
    public sealed class PrescriptionRowDto
    {
        public string? PrescriptionDateShamsi { get; set; }
        public string? DoctorName { get; set; }
        public int ItemsCount { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>پاکت استاندارد پاسخ جزئیات بیمار — چرخه بیستم</summary>
    public sealed class PatientDetailEnvelope
    {
        public bool IsSuccess { get; set; }
        public PatientDetailDto? Data { get; set; }
    }

    /// <summary>
    /// پاسخ صفحه‌بندی‌شدهٔ جستجوی نوبت‌ها — چرخه بیست‌وسوم.
    /// کنترلر نوبت‌ها از ToPagedResponse تک‌لایه استفاده می‌کند:
    /// { isSuccess, message, data: [...], pageNumber, pageSize, totalCount, ... }
    /// </summary>
    public sealed class AppointmentsSearchPage
    {
        public bool IsSuccess { get; set; }
        public List<AppointmentSearchItem>? Data { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>ردیف نتیجهٔ جستجوی نوبت — منطبق بر AppointmentSearchItemDto سرور</summary>
    public sealed class AppointmentSearchItem
    {
        public long Id { get; set; }
        public string? AppointmentCode { get; set; }
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public string? SpecialtyName { get; set; }
        public string? DateShamsi { get; set; }
        public string? Time { get; set; }
        public int? Status { get; set; }
        public decimal EstimatedFee { get; set; }
    }

    /// <summary>
    /// پاسخ صفحه‌بندی‌شدهٔ فاکتورها — چرخه بیست‌وسوم (تک‌لایه مطابق ToPagedResponse)
    /// </summary>
    public sealed class InvoicesPage
    {
        public bool IsSuccess { get; set; }
        public List<InvoiceRow>? Data { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>ردیف فاکتور — منطبق بر InvoiceListDto سرور</summary>
    public sealed class InvoiceRow
    {
        public long Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? DateShamsi { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public int PaymentStatus { get; set; }
        public int Status { get; set; }
    }

    /// <summary>پاسخ فهرست درخواست‌های آزمایش — چرخه بیست‌وسوم (پاکت استاندارد ApiResponse)</summary>
    public sealed class LabsEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<LabOrderRow>? Data { get; set; }
    }

    /// <summary>ردیف درخواست آزمایش — منطبق بر LabOrderListDto سرور</summary>
    public sealed class LabOrderRow
    {
        public long Id { get; set; }
        public string? OrderNumber { get; set; }
        public string? DoctorName { get; set; }
        public string? DateShamsi { get; set; }
        public int Status { get; set; }
        public int ItemsCount { get; set; }
    }

    /// <summary>پاسخ سوابق آرشیوی بیمار — چرخه بیست‌وسوم (پاکت استاندارد ApiResponse)</summary>
    public sealed class ArchiveEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<ArchiveRecordRow>? Data { get; set; }
    }

    /// <summary>ردیف سند آرشیوی — منطبق بر ArchiveRow سرور (Id، SourceTable، SourceId، PatientId، PatientName، RecordDate، YearJalali، SummaryLine)</summary>
    public sealed class ArchiveRecordRow
    {
        public long Id { get; set; }
        public string? SourceTable { get; set; }
        public long SourceId { get; set; }
        public long? PatientId { get; set; }
        public string? PatientName { get; set; }
        public DateTime? RecordDate { get; set; }
        public int? YearJalali { get; set; }
        public string? SummaryLine { get; set; }
    }

    /// <summary>DTO پرونده بیمار</summary>
    public sealed class PatientDetailDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MedicalRecordNo { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string? BloodType { get; set; }
        public int Gender { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? InsuranceName { get; set; }
        public string? InsuranceNumber { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? CurrentMedications { get; set; }
    }
}
