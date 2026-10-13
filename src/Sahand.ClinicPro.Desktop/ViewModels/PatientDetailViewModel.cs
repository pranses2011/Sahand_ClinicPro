using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف سوابق ویزیت در پرونده بیمار — شناسه برای گشودن جزئیات با دابل‌کلیک (چرخهٔ ۴۸)</summary>
public record PatientVisitRow(long Id, string DateShamsi, string DoctorName, string Diagnosis, string Status);

/// <summary>
/// ردیف یادداشت سریع بیمار — چرخهٔ ۶۹ (فاز ۳ / F-17 «یادداشت سریع»)
/// </summary>
public record QuickNoteRow(long Id, string Text, string DateShamsi);

/// <summary>ردیف نسخه‌های بیمار — چرخه بیست‌وچهارم: ستون «تعداد اقلام» با
/// «کد نسخه» جایگزین شد زیرا PrescriptionListDto سرور شمارش قلم ندارد؛
/// نام فیلدها هم عیناً منطبق بر سرور شد (نامقبستگی → خطای «پاسخ سرور
/// قابل تفسیر نبود» در گزارش هفدهم مالک)</summary>
public record PatientPrescriptionRow(long Id, string DateShamsi, string DoctorName, string Code, string Status);

/// <summary>ردیف نوبت‌های بیمار — چرخه بیست‌وسوم (گزارش شانزدهم مالک): تب نوبت‌ها واقعی شد</summary>
public record PatientAppointmentRow(string DateShamsi, string Time, string DoctorName, string Specialty, string Status);

/// <summary>ردیف فاکتورهای بیمار — چرخه بیست‌وسوم: تب فاکتورها واقعی شد (مبالغ با جداکنندهٔ ۳ رقمی)</summary>
public record PatientInvoiceRow(string InvoiceNumber, string DateShamsi, string TotalAmount, string PaidAmount, string Status);

/// <summary>ردیف آزمایش‌های بیمار — چرخه بیست‌وسوم: تب آزمایش‌ها واقعی شد</summary>
public record PatientLabRow(long Id, string OrderNumber, string DateShamsi, string DoctorName, int ItemsCount, string Status);

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

/// <summary>
/// ردیف سوابق پاراکلینیک پرونده (تصویربرداری/دندانپزشکی/چشم‌پزشکی/فیزیوتراپی) —
/// چرخهٔ ۴۸ (گزارش مالک: «برای همشون تب جداگانه بزار و با دابل کلیک جزئیات
/// کاملشون نمایش داده بشه»)
/// </summary>
public record PatientParaRow(
    long RefId,
    string DateShamsi,
    string Title,
    string Subtitle,
    string KindLabel,
    string DotColor);

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

    /// <summary>سوابق تصویربرداری بیمار — تب اختصاصی چرخهٔ ۴۸</summary>
    public ObservableCollection<PatientParaRow> ImagingHistory { get; } = new();

    /// <summary>سوابق دندانپزشکی بیمار — تب اختصاصی چرخهٔ ۴۸</summary>
    public ObservableCollection<PatientParaRow> DentalHistory { get; } = new();

    /// <summary>سوابق چشم‌پزشکی بیمار — تب اختصاصی چرخهٔ ۴۸</summary>
    public ObservableCollection<PatientParaRow> EyeHistory { get; } = new();

    /// <summary>سوابق فیزیوتراپی بیمار — تب اختصاصی چرخهٔ ۴۸</summary>
    public ObservableCollection<PatientParaRow> PhysioHistory { get; } = new();

    /// <summary>رویدادهای خط زمان — چرخه ۱۳ (درخواست ۱-۵)</summary>
    public ObservableCollection<TimelineRow> Timeline { get; } = new();

    /* ═══ چرخهٔ ۶۹ (فاز ۳ / F-17+F-18) — برچسب بیمار و یادداشت سریع ═══ */

    /// <summary>برچسب‌های بیمار — چیپ‌های رنگی کنار نام (F-18)</summary>
    public ObservableCollection<string> Tags { get; } = new();

    /// <summary>متن برچسب‌ها برای ویرایش — CSV؛ خالی = بدون برچسب</summary>
    [ObservableProperty]
    private string _tagsEditText = string.Empty;

    /// <summary>حالت ویرایش برچسب‌ها باز است؟</summary>
    [ObservableProperty]
    private bool _isEditingTags;

    /// <summary>پیشنهادهای آمادهٔ برچسب — VIP/مزمن/باردار (F-18)</summary>
    public string[] TagSuggestions { get; } = { "VIP", "مزمن", "باردار", "دیابت", "فشارخون" };

    /// <summary>متن یادداشت سریع در حال تایپ (F-17)</summary>
    [ObservableProperty]
    private string _quickNoteText = string.Empty;

    /// <summary>در حال ثبت یادداشت</summary>
    [ObservableProperty]
    private bool _isSavingNote;

    /// <summary>یادداشت‌های سریع بیمار — جدیدترین اول (F-17)</summary>
    public ObservableCollection<QuickNoteRow> QuickNotes { get; } = new();

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
    // چرخهٔ ۴۸ — سه نوع تازهٔ پاراکلینیک در فیلتر خط زمان
    [ObservableProperty] private bool _tlDental = true;
    [ObservableProperty] private bool _tlEye = true;
    [ObservableProperty] private bool _tlPhysio = true;

    partial void OnTlVisitsChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlLabsChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlImagingChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlPrescriptionsChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlReceptionsChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlInvoicesChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlPaymentsChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlDentalChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlEyeChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTlPhysioChanged(bool value) => _ = LoadTimelineAsync(_patientIdCache);
    partial void OnTimelineYearFilterChanged(int value) => _ = LoadTimelineAsync(_patientIdCache);

    private long _patientIdCache;

    /// <summary>سازنده با شناسه بیمار — توست اختیاری برای بازخورد برچسب/یادداشت (چرخهٔ ۶۹)</summary>
    public PatientDetailViewModel(IApiClient api, long patientId,
        Services.ISnackbarService? snackbar = null)
    {
        _api = api;
        _snackbar = snackbar;
        _patientIdCache = patientId;
        _ = LoadAsync(patientId);
    }

    /// <summary>توست بازخورد — اختیاری (چرخهٔ ۶۹: برچسب/یادداشت سریع)</summary>
    private readonly Services.ISnackbarService? _snackbar;

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

        // ═══ چرخهٔ ۶۹ (فاز ۳ / F-18) — برچسب‌های بیمار: چیپ‌های رنگی کنار نام ═══
        FillTagsFromCsv(Tags, patient.Tags);
        TagsEditText = patient.Tags ?? string.Empty;

        // ═══ چرخهٔ ۶۹ (فاز ۳ / F-17) — یادداشت‌های سریع ═══
        _ = LoadQuickNotesAsync(patientId);

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
                        v.Id,
                        v.VisitDateShamsi ?? "—",
                        v.DoctorName ?? "—",
                        v.ChiefComplaint ?? "—",
                        v.Status ?? "—"));
                }
            }
        }

        // ۳) نسخه‌ها — چرخه بیست‌وچهارم (گزارش هفدهم مالک): ریشهٔ «بارگذاری
        // نسخه‌ها ناموفق: پاسخ سرور قابل تفسیر نبود» این بود که DTO این تب با
        // PrescriptionListDto سرور هم‌خوان نبود: نام فیلد تاریخ (PrescriptionDateShamsi
        // به‌جای DateShamsi)، وضعیتِ رشته‌ای به‌جای عدد enum (بدون مبدل رشته‌ای
        // JSON، enum سرور به‌صورت عدد می‌آید و خواندنش در string استثنای
        // JsonSerializer می‌اندازد) و فیلد ItemsCount که سرور اصلاً نمی‌فرستد.
        {
            var prescriptions = await SectionAsync<PrescriptionsEnvelope>("نسخه‌ها",
                $"prescriptions?patientId={patientId}&pageSize=50");
            if (prescriptions?.Data is not null)
            {
                foreach (var p in prescriptions.Data)
                {
                    Prescriptions.Add(new PatientPrescriptionRow(
                        p.Id,
                        p.DateShamsi ?? "—",
                        p.DoctorName ?? "—",
                        string.IsNullOrWhiteSpace(p.PrescriptionCode) ? "—" : p.PrescriptionCode,
                        PrescriptionStatusText(p.Status)));
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
                        l.Id,
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

        // ═══ ۶) سوابق پاراکلینیک اختصاصی — چرخهٔ ۴۸ (گزارش مالک: «همهٔ موارد
        //     شامل دندانپزشکی/چشم‌پزشکی/فیزیوتراپی/تصویربرداری هم در پروندهٔ
        //     بیمار ثبت بشن و تب جداگانه داشته باشن») — از خط زمان گسترش‌یافتهٔ
        //     سرور (سه نوع تازه) پر می‌شوند ═══
        try
        {
            await LoadParaclinicalHistoryAsync(patientId);
        }
        catch (Exception ex)
        {
            RecordSectionError("سوابق پاراکلینیک", ex);
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

    /// <summary>متن فارسی وضعیت نسخه — چرخه بیست‌وچهارم: سرور enum را عددی
    /// می‌فرستد (PrescriptionStatus)؛ این نگاشت عیناً هم‌ارز برچسب‌های صفحهٔ
    /// اصلی نسخه‌هاست تا واژه‌ها یکدست بماند</summary>
    private static string PrescriptionStatusText(int status) => status is >= 1 and <= 5
        ? new[] { "صادر شده", "ارسال به بیمه", "تأیید بیمه", "تحویل شده", "ابطال شده" }[status - 1]
        : "نامشخص";

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
        if (TlDental) parts.Add("dental");
        if (TlEye) parts.Add("eye");
        if (TlPhysio) parts.Add("physio");
        var qs = parts.Count == 10 ? "" : $"?kinds={string.Join(",", parts)}";
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

    /// <summary>
    /// بارگذاری سوابق پاراکلینیک تب‌های اختصاصی — چرخهٔ ۴۸: یک درخواست از خط
    /// زمانِ گسترش‌یافته با فیلتر چهار نوع، و تفکیک ردیف‌ها به کلکسیون هر تب.
    /// </summary>
    private async Task LoadParaclinicalHistoryAsync(long patientId)
    {
        var tl = await _api.GetAsync<TimelineEnvelope>(
            $"patients/{patientId}/timeline?kinds=imaging,dental,eye,physio");
        if (tl?.Data is null) return;

        ImagingHistory.Clear();
        DentalHistory.Clear();
        EyeHistory.Clear();
        PhysioHistory.Clear();
        foreach (var e in tl.Data.Events ?? new List<TimelineEventApi>())
        {
            var row = new PatientParaRow(
                e.RefId,
                e.DateShamsi ?? "—",
                e.Title ?? "—",
                e.Subtitle ?? "",
                e.KindLabel ?? "",
                DotColorOf(e.Kind));
            switch (e.Kind)
            {
                case "imaging": ImagingHistory.Add(row); break;
                case "dental": DentalHistory.Add(row); break;
                case "eye": EyeHistory.Add(row); break;
                case "physio": PhysioHistory.Add(row); break;
            }
        }
    }

    /* ═══════════ دابل‌کلیک → جزئیات کامل — چرخهٔ ۴۸ (گزارش مالک) ═══════════ */

    /// <summary>
    /// جزئیات ویزیت با دابل‌کلیک — همان فرم کامل ویزیت (شرح حال/SOAP/حیاتی/
    /// تشخیص/ارجاع‌ها) برای مرور و ویرایش باز می‌شود.
    /// </summary>
    [RelayCommand]
    private void OpenVisitDetails(PatientVisitRow? row)
    {
        if (row is null || row.Id <= 0) return;
        var snackbar = System.Windows.Application.Current is { } app
            ? App.Services.GetRequiredService<ISnackbarService>()
            : null;
        if (snackbar is null) return;
        var window = new Views.VisitWindow(new VisitEditViewModel(_api, snackbar, row.Id))
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    /// <summary>جزئیات آزمایش با دابل‌کلیک — پنجرهٔ ثبت/مشاهدهٔ نتایج قلم‌به‌قلم</summary>
    [RelayCommand]
    private void OpenLabDetails(PatientLabRow? row)
    {
        if (row is null || row.Id <= 0) return;
        var window = new Views.LabResultWindow(new LabResultViewModel(_api, row.Id))
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    /// <summary>جزئیات نسخه با دابل‌کلیک — پنجرهٔ کامل اقلام و توضیحات نسخه</summary>
    [RelayCommand]
    private void OpenPrescriptionDetails(PatientPrescriptionRow? row)
    {
        if (row is null || row.Id <= 0) return;
        var window = new Views.PrescriptionDetailWindow(new PrescriptionDetailViewModel(_api, row.Id))
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    /// <summary>
    /// جزئیات ردیف‌های پاراکلینیک (تصویربرداری/دندان/چشم/فیزیو) با دابل‌کلیک —
    /// چشم: معاینهٔ کامل از سابقهٔ سرور؛ دندان: چارت و ردیف‌های طرح درمان؛
    /// بقیه: دیالوگ فارسی خود برنامه با همهٔ اطلاعات موجود همان رکورد.
    /// </summary>
    [RelayCommand]
    private async Task OpenParaDetailsAsync(PatientParaRow? row)
    {
        if (row is null) return;

        if (row.KindLabel.Contains("چشم"))
        {
            var full = await TryBuildEyeExamDetailsAsync(row.RefId);
            if (full is not null)
            {
                AppDialog.Info($"جزئیات معاینه چشم — {row.DateShamsi}", full);
                return;
            }
        }

        if (row.KindLabel.Contains("دندان"))
        {
            var full = await TryBuildDentalDetailsAsync();
            if (full is not null)
            {
                AppDialog.Info($"جزئیات طرح درمان دندان — {row.DateShamsi}", full);
                return;
            }
        }

        // تصویربرداری / فیزیوتراپی (و بازگشتی‌های تهی چشم/دندان) — همهٔ اطلاعات موجود
        AppDialog.Info(
            $"جزئیات {row.KindLabel.Trim()}",
            $"عنوان: {row.Title}\n\nشرح: {row.Subtitle}\n\nتاریخ: {row.DateShamsi}");
    }

    /// <summary>
    /// ساخت متن کامل معاینهٔ چشم از سابقهٔ سرور — یافتن رکورد با شناسه و
    /// قالب‌بندی همهٔ فیلدهای تخصصی (عدسی/استوانه/محور/IOP/VA/PD/یافته‌ها).
    /// </summary>
    private async Task<string?> TryBuildEyeExamDetailsAsync(long examId)
    {
        try
        {
            var envelope = await _api.GetAsync<EyeExamsEnvelope>(
                $"eye/exams?patientId={_patientIdCache}");
            var exam = envelope?.Data?.FirstOrDefault(e => e.Id == examId);
            if (exam is null) return null;

            static string Side(EyeSideDto? s) => s is null
                ? "—"
                : $"عدسی {s.Sphere?.ToString() ?? "—"} / استوانه {s.Cylinder?.ToString() ?? "—"} / محور {s.Axis?.ToString() ?? "—"}";

            return
                $"چشم راست (OD):\n  {Side(exam.Od)}\n  حدت بینایی: {exam.Od?.Acuity ?? "—"} • فشار چشم: {exam.Od?.Iop?.ToString() ?? "—"}\n\n" +
                $"چشم چپ (OS):\n  {Side(exam.Os)}\n  حدت بینایی: {exam.Os?.Acuity ?? "—"} • فشار چشم: {exam.Os?.Iop?.ToString() ?? "—"}\n\n" +
                $"فاصلهٔ مردمک (PD): {exam.Pd?.ToString() ?? "—"}\n" +
                $"دید رنگی: {exam.ColorVision ?? "—"}\n" +
                (string.IsNullOrWhiteSpace(exam.Findings) ? "" : $"\nیافته‌ها:\n{exam.Findings}") +
                (string.IsNullOrWhiteSpace(exam.EyewearPrescription) ? "" : $"\n\nنسخهٔ عینک:\n{exam.EyewearPrescription}");
        }
        catch
        {
            return null; // خطای شبکه → دیالوگ خلاصه‌ای پاراکلینیک
        }
    }

    /// <summary>
    /// ساخت متن کامل چارت دندان از آخرین طرح درمان سرور — برچسب وضعیت هر
    /// دندان غیرسالم + ردیف‌های طرح (دندان/عملیات/قیمت/وضعیت).
    /// </summary>
    private async Task<string?> TryBuildDentalDetailsAsync()
    {
        try
        {
            var envelope = await _api.GetAsync<DentalChartEnvelope>(
                $"dental/chart?patientId={_patientIdCache}");
            var plan = envelope?.Data;
            if (plan is null) return null;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"هزینهٔ کل طرح: {Money.Format(plan.TotalCost)} — جلسات برآوردی: {plan.EstimatedSessions?.ToString() ?? "—"}");
            sb.AppendLine();

            // چارت وضعیت دندان‌ها از PlanDetails (JSON ذخیره‌شدهٔ دسکتاپ دندانپزشکی)
            if (!string.IsNullOrWhiteSpace(plan.PlanDetails))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(plan.PlanDetails);
                    if (doc.RootElement.TryGetProperty("teeth", out var teeth)
                        && teeth.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        var labels = new Dictionary<string, string>
                        {
                            ["1"] = "پوسیدگی", ["2"] = "ترمیم‌شده", ["3"] = "درمان‌شده",
                            ["4"] = "روکش", ["5"] = "کشیده‌شده", ["6"] = "ایمپلنت"
                        };
                        var affected = teeth.EnumerateObject()
                            .Where(t => t.Value.ValueKind == System.Text.Json.JsonValueKind.Number
                                     && t.Value.GetRawText() != "0"
                                     && labels.TryGetValue(t.Value.GetRawText(), out _))
                            .Select(t => $"{t.Name} ({labels[t.Value.GetRawText()]})")
                            .ToList();
                        sb.AppendLine(affected.Count > 0
                            ? "دندان‌های دارای وضعیت ثبت‌شده:"
                            : "همهٔ دندان‌ها سالم‌اند.");
                        foreach (var a in affected)
                            sb.AppendLine($"  • دندان {a}");
                        sb.AppendLine();
                    }

                    if (doc.RootElement.TryGetProperty("rows", out var rows)
                        && rows.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        sb.AppendLine("ردیف‌های طرح درمان:");
                        foreach (var r in rows.EnumerateArray())
                        {
                            var tooth = r.TryGetProperty("tooth", out var t2) ? t2.ToString() : "—";
                            var proc = r.TryGetProperty("procedure", out var p2) ? p2.ToString() : "—";
                            var price = r.TryGetProperty("price", out var pr2) ? pr2.GetDecimal() : 0m;
                            var status = r.TryGetProperty("status", out var s2) ? s2.ToString() : "";
                            sb.AppendLine($"  • دندان {tooth} — {proc} — {Money.Format(price)}{(string.IsNullOrWhiteSpace(status) ? "" : $" ({status})")}");
                        }
                    }
                }
                catch
                {
                    // JSON ناقص/قدیمی — بخش متنی همان‌طور نمایش داده می‌شود
                }
            }

            sb.AppendLine();
            sb.AppendLine($"تاریخ ثبت طرح: {plan.CreatedShamsi ?? "—"}");
            return sb.ToString();
        }
        catch
        {
            return null;
        }
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
        "dental" => "#F4511E",
        "eye" => "#039BE5",
        "physio" => "#7CB342",
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

    /* ═══ چرخهٔ ۶۹ (فاز ۳ / F-17+F-18) — فرمان‌های برچسب و یادداشت سریع ═══ */

    /// <summary>پر کردن برچسب‌ها از CSV فارسی — «VIP، دیابت» (F-18)</summary>
    private static void FillTagsFromCsv(ObservableCollection<string> target, string? csv)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(csv)) return;
        foreach (var tag in csv.Split(new[] { ',', '،', ';' },
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            target.Add(tag);
    }

    /// <summary>باز/بستن حالت ویرایش برچسب‌ها (F-18)</summary>
    [RelayCommand]
    private void ToggleTagEditing() => IsEditingTags = !IsEditingTags;

    /// <summary>افزودن یک پیشنهاد آماده به متن ویرایش (F-18)</summary>
    [RelayCommand]
    private void AddTagSuggestion(string tag)
    {
        var parts = TagsEditText.Split(new[] { ',', '،' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (parts.Contains(tag, StringComparer.Ordinal))
            return; // تکراری — چیپ دوباره اضافه نمی‌شود
        parts.Add(tag);
        TagsEditText = string.Join("، ", parts);
    }

    /// <summary>ذخیرهٔ برچسب‌ها در سرور و تازه‌سازی چیپ‌ها (F-18)</summary>
    [RelayCommand]
    private async Task SaveTagsAsync()
    {
        var ok = await _api.PutAsync($"patients/{_patientIdCache}/tags",
            new { tags = TagsEditText });
        if (!ok)
        {
            _snackbar?.Error(_api.LastError ?? "ذخیرهٔ برچسب‌ها ناموفق بود");
            return;
        }

        FillTagsFromCsv(Tags, TagsEditText);
        IsEditingTags = false;
        _snackbar?.Success("برچسب‌های بیمار ذخیره شد");
    }

    /// <summary>بارگذاری یادداشت‌های سریع بیمار (F-17)</summary>
    private async Task LoadQuickNotesAsync(long patientId)
    {
        try
        {
            var envelope = await _api.GetAsync<QuickNotesEnvelope>($"patients/{patientId}/notes");
            QuickNotes.Clear();
            if (envelope?.Data is null) return;
            foreach (var n in envelope.Data)
                QuickNotes.Add(new QuickNoteRow(n.Id, n.Text ?? "", n.DateShamsi ?? ""));
        }
        catch
        {
            // یادداشت‌ها بخش جانبی‌اند — خطای شبکه پرونده را خراب نمی‌کند
        }
    }

    /// <summary>ثبت یادداشت سریع — بدون ساخت ویزیت کامل (F-17)</summary>
    [RelayCommand]
    private async Task AddQuickNoteAsync()
    {
        var text = QuickNoteText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            _snackbar?.Warning("متن یادداشت را بنویسید");
            return;
        }

        IsSavingNote = true;
        var ok = await _api.PostAsync($"patients/{_patientIdCache}/notes", new { text });
        IsSavingNote = false;

        if (!ok)
        {
            _snackbar?.Error(_api.LastError ?? "ثبت یادداشت ناموفق بود");
            return;
        }

        QuickNoteText = string.Empty;
        await LoadQuickNotesAsync(_patientIdCache);
        _snackbar?.Success("یادداشت ثبت شد");
    }

    /* ── DTOهای سبک ── */

    /// <summary>پاکت یادداشت‌های سریع بیمار — چرخهٔ ۶۹ (F-17)</summary>
    public sealed class QuickNotesEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<QuickNoteDto>? Data { get; set; }
    }

    /// <summary>قلم یادداشت سریع — منطبق بر patients/{id}/notes</summary>
    public sealed class QuickNoteDto
    {
        public long Id { get; set; }
        public string? Text { get; set; }
        public string? DateShamsi { get; set; }
    }

    /// <summary>پاسخ سابقهٔ معاینه‌های چشم — چرخهٔ ۴۸ برای جزئیات دابل‌کلیک</summary>
    public sealed class EyeExamsEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<EyeExamApi>? Data { get; set; }
    }

    /// <summary>معاینهٔ چشم از سرور — همان شکل صفحهٔ چشم‌پزشکی</summary>
    public sealed class EyeExamApi
    {
        public long Id { get; set; }
        public string? DateShamsi { get; set; }
        public EyeSideDto? Od { get; set; }
        public EyeSideDto? Os { get; set; }
        public decimal? Pd { get; set; }
        public string? ColorVision { get; set; }
        public string? Findings { get; set; }
        public string? EyewearPrescription { get; set; }
    }

    /// <summary>یک سمت معاینهٔ چشم</summary>
    public sealed class EyeSideDto
    {
        public decimal? Sphere { get; set; }
        public decimal? Cylinder { get; set; }
        public int? Axis { get; set; }
        public string? Acuity { get; set; }
        public int? Iop { get; set; }
    }

    /// <summary>پاسخ چارت دندان — چرخهٔ ۴۸ برای جزئیات دابل‌کلیک</summary>
    public sealed class DentalChartEnvelope
    {
        public bool IsSuccess { get; set; }
        public DentalPlanApi? Data { get; set; }
    }

    /// <summary>طرح درمان دندان از سرور</summary>
    public sealed class DentalPlanApi
    {
        public long Id { get; set; }
        public decimal TotalCost { get; set; }
        public int? EstimatedSessions { get; set; }
        public string? PlanDetails { get; set; }
        public string? CreatedShamsi { get; set; }
    }

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

        // چرخهٔ ۴۸ — شناسهٔ مرجع رکورد برای دابل‌کلیک جزئیات (ویزیت/آزمایش/نسخه/دندان/چشم/فیزیو…)
        public long RefId { get; set; }
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
        // چرخهٔ ۴۸ — شناسهٔ ویزیت برای گشودن فرم کامل با دابل‌کلیک
        public long Id { get; set; }
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

    /// <summary>ردیف نسخه — منطبق عیناً بر PrescriptionListDto سرور:
    /// وضعیت/نوع enum هستند و بدون JsonStringEnumConverter به‌صورت «عدد»
    /// سریال می‌شوند؛ بنابراین اینجا هم int خوانده می‌شود نه string</summary>
    public sealed class PrescriptionRowDto
    {
        public long Id { get; set; }
        public string? PrescriptionCode { get; set; }
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public string? DateShamsi { get; set; }
        public int Type { get; set; }
        public int Status { get; set; }
        public bool IsElectronic { get; set; }
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

        /// <summary>برچسب‌های بیمار CSV — چرخهٔ ۶۹ (فاز ۳ / F-18)</summary>
        public string? Tags { get; set; }
    }
}
