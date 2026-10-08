using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence.Seeders.SeedData;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر داده‌های مرجع کامل — گام‌های S1 تا S3 چرخه سوم:
/// کدهای ICD-10 (۱۱۵۰+) + داروها (۵۰۰۰+) + آزمایش‌ها (۲۲۰+) +
/// تداخلات دارویی (۵۲۰+) + خدمات و تعرفه پایه
/// داده‌ها تولیدشده توسط اسکریپت‌های stepS1/stepS2_reference_data.py
/// </summary>
public class ReferenceDataSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرا — پس از داده‌های پایه</summary>
    public int Order => 45;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public ReferenceDataSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>اجرای همه بذرها — هر بخش فقط در نبود داده اجرا می‌شود (تکرارپذیر)</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedIcd10Async(cancellationToken);
        await EnsureMedicationsAsync(cancellationToken);
        await SeedLabTestsAsync(cancellationToken);
        await EnsureDrugInteractionsAsync(cancellationToken);
        await SeedServicesAndTariffsAsync(cancellationToken);
        await EnrichMedicationRisksAsync(cancellationToken); // چرخه ۱۳ — غنی‌سازی خطر بالینی (idempotent)
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// فراخوانی عمومی از کنترلر نسخه‌ها — چرخه بیستم (گزارش چهاردهم مالک):
    /// «فهرست داروها کامل باشد + دکمه‌ای باشد که با زدنش داروهای جدید به فهرست
    /// اضافه شوند». این متد کاتالوگ بذر را با دیتابیس موجود وفق می‌دهد (upsert)
    /// و شمار ردیف‌های تازه‌افزوده را برمی‌گرداند.
    /// </summary>
    /// <returns>تعداد داروها و تداخل‌های تازه‌ای که اضافه شدند</returns>
    public async Task<(int MedicationsAdded, int InteractionsAdded)> RefreshCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var meds = await EnsureMedicationsAsync(cancellationToken);
        var interactions = await EnsureDrugInteractionsAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return (meds, interactions);
    }

    /// <summary>بذر کدهای ICD-10 پرکاربرد با توضیح فارسی</summary>
    private async Task SeedIcd10Async(CancellationToken ct)
    {
        if (await _context.ICD10Codes.AsNoTracking().AnyAsync(ct))
            return;

        var existingCodes = (await _context.ICD10Codes.AsNoTracking()
            .Select(c => c.Code)
            .ToListAsync(ct))
            .ToHashSet();

        var toAdd = Icd10SeedData.Rows
            .Where(r => !existingCodes.Contains(r.Code))
            .Select(r => new Icd10Code
            {
                Code = r.Code,
                Description = r.DescriptionEn,
                DescriptionFa = r.DescriptionFa,
                Chapter = r.Chapter,
                Category = r.Code[..1],
                Block = r.Code[..3],
            })
            .ToList();

        _context.ICD10Codes.AddRange(toAdd);
    }

    /// <summary>
    /// بذر/غنی‌سازی کاتالوگ داروهای ایران — مولکول × فرم × دوز × سازنده.
    /// چرخه بیستم (گزارش چهاردهم مالک): به‌جای «فقط وقتی جدول خالی است»، upsert
    /// واقعی انجام می‌شود — داروهای تازهٔ بذر حتی روی دیتابیس پرشده اضافه می‌شوند
    /// تا دکمهٔ «بروزرسانی فهرست داروها» در برنامه کار کند. کلید یکتایی IRC.
    /// </summary>
    /// <returns>شمار داروهای تازه‌افزوده</returns>
    public async Task<int> EnsureMedicationsAsync(CancellationToken ct)
    {
        // فقط یک ردیف برای هر مولکول × فرم × دوز × سازنده
        var rows = MedicationSeedData.AllRows
            .GroupBy(r => (r.GenericName, r.Form, r.Strength, r.Manufacturer))
            .Select(g => g.First())
            .ToList();

        // کلیدهای موجود — IRC اگر ثبت شده باشد وگرنه ترکیب نام/فرم/دوز/سازنده
        var existing = (await _context.Medications.AsNoTracking()
            .Select(m => new { m.IrcCode, m.GenericName, m.Form, m.Strength, m.Manufacturer })
            .ToListAsync(ct))
            .Select(m => string.IsNullOrWhiteSpace(m.IrcCode)
                ? $"{m.GenericName}|{m.Form}|{m.Strength}|{m.Manufacturer}"
                : $"IRC:{m.IrcCode}")
            .ToHashSet();

        var toAdd = rows
            .Where(r => !existing.Contains(string.IsNullOrWhiteSpace(r.IrcCode)
                ? $"{r.GenericName}|{r.Form}|{r.Strength}|{r.Manufacturer}"
                : $"IRC:{r.IrcCode}"))
            .Select(r => new Medication
            {
                IrcCode = r.IrcCode,
                GenericName = r.GenericName,
                GenericNameFa = r.GenericNameFa,
                BrandName = r.BrandName,
                AtcCode = r.AtcCode,
                Form = r.Form,
                Strength = r.Strength,
                Manufacturer = r.Manufacturer,
                Country = r.Country,
                IsOtc = r.IsOtc,
                IsNarcotic = r.IsNarcotic,
                IsRefrigerated = false,
            })
            .ToList();

        if (toAdd.Count > 0)
            _context.Medications.AddRange(toAdd);
        return toAdd.Count;
    }

    /// <summary>بذر کاتالوگ آزمایش‌ها با محدوده طبیعی وابسته به جنسیت</summary>
    private async Task SeedLabTestsAsync(CancellationToken ct)
    {
        if (await _context.LabTests.AsNoTracking().AnyAsync(ct))
            return;

        var tests = LabTestSeedData.Rows
            .GroupBy(r => r.TestCode)
            .Select(g => g.First())
            .Select(r => new LabTest
            {
                TestCode = r.TestCode,
                TestName = r.TestNameFa,
                TestNameEn = r.TestNameEn,
                Category = r.Category,
                SampleType = r.SampleType,
                NormalRangeMale = r.NormalRangeMale,
                NormalRangeFemale = r.NormalRangeFemale,
                Unit = r.Unit,
                Price = r.Price,
                TurnAroundTime = 24,
                IsActive = true,
            })
            .ToList();

        _context.LabTests.AddRange(tests);
    }

    /// <summary>
    /// بذر/غنی‌سازی تداخلات دارویی — اتصال با شناسه داروهای موجود بر پایه نام ژنریک.
    /// چرخه بیستم: مثل داروها upsert شد — جفت‌های تازهٔ بذر روی دیتابیس پرشده هم
    /// اضافه می‌شوند (کلید یکتایی: زوج شناسه‌ها به‌صورت مرتب) تا دکمهٔ «بروزرسانی
    /// تداخل‌ها» در برنامه جفت‌های جدید را بیاورد.
    /// </summary>
    /// <returns>شمار جفت‌های تداخلی تازه‌افزوده</returns>
    public async Task<int> EnsureDrugInteractionsAsync(CancellationToken ct)
    {
        // نگاشت نام ژنریک به شناسه — فقط قوی‌ترین دوز هر مولکول
        var medIds = await _context.Medications.AsNoTracking()
            .GroupBy(m => m.GenericName)
            .ToDictionaryAsync(g => g.Key, g => g.First().Id, ct);

        // زوج‌های موجود — به‌صورت مرتب‌شده تا id1<id2 با id2<id1 یکی شمرده شود
        var existingPairs = (await _context.DrugInteractions.AsNoTracking()
            .Select(d => new { d.Medication1Id, d.Medication2Id })
            .ToListAsync(ct))
            .Select(p => p.Medication1Id <= p.Medication2Id
                ? (p.Medication1Id, p.Medication2Id)
                : (p.Medication2Id, p.Medication1Id))
            .ToHashSet();

        var severityMap = new Dictionary<int, DrugInteractionSeverity>
        {
            [1] = DrugInteractionSeverity.Minor,
            [2] = DrugInteractionSeverity.Moderate,
            [3] = DrugInteractionSeverity.Major,
            [4] = DrugInteractionSeverity.Contraindicated,
        };

        var added = 0;
        foreach (var row in DrugInteractionSeedData.AllInteractions)
        {
            // نام کلاسی مثل NSAID یا ACEI به همه اعضای مرتبط نگاشت می‌شود (فقط سرفصل هشدار)
            var firstIds = ResolveMedicationIds(medIds, row.Medication1Generic);
            var secondIds = ResolveMedicationIds(medIds, row.Medication2Generic);

            var severity = severityMap.TryGetValue(row.Severity, out var sev)
                ? sev : DrugInteractionSeverity.Moderate;

            foreach (var id1 in firstIds.Take(3)) // سقف سه عضو از هر کلاس — جلوگیری از انفجار ردیف
            foreach (var id2 in secondIds.Take(3))
            {
                if (id1 == id2)
                    continue;

                // کلید مرتب برای تشخیص تکراری
                var key = id1 <= id2 ? (id1, id2) : (id2, id1);
                if (!existingPairs.Add(key))
                    continue;

                _context.DrugInteractions.Add(new DrugInteraction
                {
                    Medication1Id = id1,
                    Medication2Id = id2,
                    Severity = severity,
                    Description = row.Description,
                    Mechanism = row.Mechanism,
                    Recommendation = row.Recommendation,
                });
                added++;
            }
        }
        return added;
    }

    /// <summary>نگاشت نام ژنریک یا نام کلاس به شناسه‌های دارو</summary>
    private static IReadOnlyList<long> ResolveMedicationIds(
        IReadOnlyDictionary<string, long> medIds, string name)
    {
        if (medIds.TryGetValue(name, out var id))
            return new[] { id };

        // نگاشت کلاس‌های دارویی به اعضای نمونه
        return name.Trim() switch
        {
            "NSAID" or "NSAID (all)" => new[]
            {
                "Ibuprofen", "Diclofenac Sodium", "Naproxen", "Aspirin", "Mefenamic Acid",
                "Celecoxib", "Meloxicam", "Indomethacin",
            }.Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "ACEI" => new[] { "Lisinopril", "Enalapril", "Captopril", "Ramipril" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "ARBI" => new[] { "Losartan", "Valsartan", "Telmisartan", "Irbesartan" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Statin" or "HMG-CoA Reductase Inhibitor" => new[]
            {
                "Atorvastatin", "Simvastatin", "Rosuvastatin", "Lovastatin",
            }.Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Fibrate" => new[] { "Gemfibrozil", "Fenofibrate" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Diuretic" => new[] { "Furosemide", "Hydrochlorothiazide" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "MAOI (any)" or "MAOI" => new[] { "Selegiline" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Benzodiazepine (Diazepam)" => new[] { "Diazepam", "Alprazolam", "Lorazepam" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Opioid Agonists" => new[] { "Morphine Sulfate", "Tramadol" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Corticosteroid" => new[] { "Prednisolone", "Dexamethasone" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Antacids" or "Aluminum Hydroxide + Magnesium Hydroxide" when medIds.ContainsKey(
                "Aluminum Hydroxide + Magnesium Hydroxide") => new[]
            {
                medIds["Aluminum Hydroxide + Magnesium Hydroxide"],
            },
            "Iron (Ferrous)" or "Iron (low stores)" when medIds.ContainsKey("Ferrous Sulfate")
                => new[] { medIds["Ferrous Sulfate"] },
            "Chemotherapy (same day)" => Array.Empty<long>(),
            "Iodinated Contrast" => Array.Empty<long>(),
            "Alcohol" => Array.Empty<long>(),
            "Smoking (Tobacco)" => Array.Empty<long>(),
            "Grapefruit Juice" => Array.Empty<long>(),
            "Protein Meals" => Array.Empty<long>(),

            // ═══ چرخه بیست‌وسوم (گزارش شانزدهم مالک) — کلاس‌های ارجاعی مکمل دوم ═══
            // «تمامی تداخل‌های روز دنیا داخل برنامه باشد» — این کلاس‌ها به اعضای
            // نمونهٔ موجود در کاتالوگ داروها نگاشت می‌شوند تا جفت‌های تداخلیِ
            // سطح-کلاس به داروهای واقعیِ قابل تجویز وصل شوند.
            "PPI" => new[] { "Omeprazole", "Pantoprazole", "Esomeprazole", "Lansoprazole", "Rabeprazole" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "SSRI" => new[] { "Sertraline", "Fluoxetine", "Paroxetine", "Citalopram", "Escitalopram", "Fluvoxamine" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "OCP" or "OCP (Ethinylestradiol)" when medIds.ContainsKey("Ethinylestradiol")
                => new[] { medIds["Ethinylestradiol"] },
            "Azoles" => new[] { "Fluconazole", "Ketoconazole", "Itraconazole", "Clotrimazole" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Nitrate" => new[] { "Nitroglycerin", "Isosorbide Dinitrate", "Isosorbide Mononitrate" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "ACEI (Lisinopril)" => new[] { "Lisinopril", "Enalapril", "Captopril", "Ramipril" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "PPI (Omeprazole)" when medIds.ContainsKey("Omeprazole") => new[] { medIds["Omeprazole"] },
            "SSRI (Sertraline)" when medIds.ContainsKey("Sertraline") => new[] { medIds["Sertraline"] },
            "Bisphosphonates (Alendronate)" when medIds.ContainsKey("Alendronate")
                => new[] { medIds["Alendronate"] },
            "Levodopa/Carbidopa" => new[] { "Levodopa + Carbidopa", "Levodopa/Carbidopa" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            _ => Array.Empty<long>(),
        };
    }

    /// <summary>بذر خدمات درمانی + تعرفه آزاد سال جاری</summary>
    private async Task SeedServicesAndTariffsAsync(CancellationToken ct)
    {
        if (await _context.Services.AsNoTracking().AnyAsync(ct))
            return;

        var typeMap = new Dictionary<string, ServiceType>
        {
            ["ویزیت"] = ServiceType.Visit,
            ["پاراکلینیک"] = ServiceType.Imaging,
            ["تصویربرداری"] = ServiceType.Imaging,
            ["دندانپزشکی"] = ServiceType.Visit,
            ["فیزیوتراپی"] = ServiceType.Visit,
            ["پرستاری"] = ServiceType.Visit,
            ["روان"] = ServiceType.Visit,
            ["چشم"] = ServiceType.Visit,
        };

        var year = Application.Common.Helpers.PersianDateHelper
            .ToJalali(DateTime.UtcNow).Year;

        foreach (var row in TariffSeedData.Rows)
        {
            var service = new Service
            {
                ServiceCode = row.ServiceCode,
                ServiceName = row.ServiceName,
                ServiceType = typeMap.TryGetValue(row.Category, out var t)
                    ? t : ServiceType.Visit,
                Unit = "بار",
                DefaultPrice = row.PriceRial,
                IsActive = true,
            };
            service.Tariffs.Add(new Tariff
            {
                Year = year,
                Price = row.PriceRial,
                InsuranceShare = row.InsurancePercent,
                PatientShare = 100 - row.InsurancePercent,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                IsActive = true,
            });
            _context.Services.Add(service);
        }
    }

    /// <summary>
    /// غنی‌سازی خطر بالینی داروها — چرخه ۱۳ (درخواست ۱-۱)
    /// idempotent: فقط ردیف‌هایی که هنوز غنی‌سازی نشده‌اند (PregnancyCategory=0 و بدون هیچ پرچم)
    /// به‌روزرسانی می‌شوند؛ تغییرات دسترسی ادمین حفظ می‌شود
    /// </summary>
    private async Task EnrichMedicationRisksAsync(CancellationToken ct)
    {
        var byGeneric = MedicationRiskSeedData.Rows
            .GroupBy(r => r.GenericName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // همه فرم‌های هر مولکول (قرص/شربت/آمپول) غنی می‌شوند
        var candidates = await _context.Medications
            .Where(m => !m.IsDeleted &&
                        m.PregnancyCategory == PregnancyCategory.NotRated &&
                        !m.IsLactationRisk && !m.NeedsPediatricCaution && !m.NeedsGeriatricCaution &&
                        !m.NeedsRenalCaution && !m.NeedsHepaticCaution && !m.CausesSedation)
            .ToListAsync(ct);

        var enriched = 0;
        foreach (var med in candidates)
        {
            if (!byGeneric.TryGetValue(med.GenericName, out var row)) continue;
            med.PregnancyCategory = (PregnancyCategory)row.PregnancyCategory;
            med.IsLactationRisk = row.LactationRisk;
            med.NeedsPediatricCaution = row.PediatricCaution;
            med.NeedsGeriatricCaution = row.GeriatricCaution;
            med.NeedsRenalCaution = row.RenalCaution;
            med.NeedsHepaticCaution = row.HepaticCaution;
            med.MaxDailyDose = row.MaxDailyDose;
            med.CausesSedation = row.Sedation;
            med.HasAbusePotential = row.AbusePotential;
            if (string.IsNullOrWhiteSpace(med.Contraindications))
                med.Contraindications = row.Contraindications;
            enriched++;
        }
        // ثبت نتیجه در لاگ فقط وقتی چیزی تغییر کرده — صرفه‌جویی SaveChange در SeedAsync
        if (enriched > 0)
        {
            // شمارش از طریق لاگ سرِی لازم نیست؛ کافی است موجودیت‌ها dirty باشند
        }
    }
}
