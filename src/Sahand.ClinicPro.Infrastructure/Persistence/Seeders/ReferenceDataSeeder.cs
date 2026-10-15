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
    /// <remarks>
    /// ═══ چرخهٔ ۷۲ (گزارش مالک) — بازه‌های ساخت‌یافته هنگام نصب ═══
    /// «میخوام هنگام نصب داخل برنامه باشن و در صورت نیاز بتونم اصلاحشون بکنم»:
    /// پس از بذرِ کاتالوگ (یا وجودش از قبل)، بازه‌های مرجع عددی و حدود بحرانی
    /// از LabReferenceRangeSeedData روی ردیف‌های «بدون هیچ بازهٔ ثبت‌شده»
    /// بک‌فیل می‌شوند. چون Seeder در هر راه‌اندازی سرور اجرا می‌شود، نصبِ
    /// بستهٔ آپدیت + ری‌استارت API همانِِ «هنگام نصب» مالک است.
    /// گارد مالکیت: فقط وقتی «هر ۹ ستون ساخت‌یافته تهی» است نوشته می‌شود —
    /// اگر مالک حتی یک فیلد یک تست را ویرایش کرده باشد، آن تست دست‌نخورده
    /// می‌ماند (بذرِ ویرایش مالک را هرگز بازنویسی نمی‌کند).
    /// </remarks>
    private async Task SeedLabTestsAsync(CancellationToken ct)
    {
        if (await _context.LabTests.AsNoTracking().AnyAsync(ct))
        {
            // کاتالوگ از قبل هست — فقط بک‌فیل بازه‌های ساخت‌یافتهٔ چرخهٔ ۷۲
            await BackfillLabReferenceRangesAsync(ct);
            return;
        }

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
        await _context.SaveChangesAsync(ct);

        // نصب تازه — بازه‌ها همان جلسهٔ نخست روی ردیف‌های تازه نشسته می‌شوند
        await BackfillLabReferenceRangesAsync(ct);
    }

    /// <summary>
    /// بک‌فیل بازه‌های مرجع ساخت‌یافته و حدود بحرانی — چرخهٔ ۷۲.
    /// ردیف‌های «تازه‌بذرفشان‌شده» و «موجودِ بدون بازه» هر دو پوشش داده
    /// می‌شوند؛ ردیفِ دارای حتی یک ستون پرشده (یعنی ویرایش‌شده توسط مالک
    /// از «آزمایشگاه ← کاتالوگ ← ویرایش») عمداً نادیده گرفته می‌شود.
    /// ═══ چرخهٔ ۷۸ (گزارش مالک: «برای همه بیماران میگه حد طبیعی منفی میباشد») ═══
    /// دو گسترش تعیین‌کننده:
    ///  ۱) تطبیق دو مرحله‌ای — اول TestCode؛ اگر نبود یا برخورد نداشت،
    ///     «نام فارسی نرمال‌شده» تست با نام کاتالوگ بذر تطبیق می‌شود؛ تست‌های
    ///     دست‌ساز مالک بدون کد هم به این ترتیب بازهٔ عددی می‌گیرند؛
    ///  ۲) گارد مالکیت فقط روی هشت ستونِ «بازه و حد بحرانی» — سقف سن کودک
    ///     پرشده به‌تنهایی دیگر مانع بک‌فیل نمی‌شود (پیش‌تر کل ردیف را برای
    ///     همیشه قفل می‌کرد).
    /// نرمال‌سازی نام: حذف فاصله‌های تکراری + یکسان‌سازی ی/ک عربی و نیم‌فاصله.
    /// </summary>
    /// <returns>شمار تست‌هایی که بازهٔ ساخت‌یافته گرفتند (برای لاگ)</returns>
    private async Task<int> BackfillLabReferenceRangesAsync(CancellationToken ct)
    {
        // نگاشت کد→بازه با «اولین ردیف» (هم‌الگوی GroupBy کاتالوگ — کد تکراری نداریم اما گارد است)
        var byCode = new Dictionary<string, LabReferenceRangeRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in LabReferenceRangeSeedData.Rows)
            if (!byCode.ContainsKey(row.TestCode))
                byCode[row.TestCode] = row;

        if (byCode.Count == 0) return 0;

        // ═══ چرخهٔ ۷۸ — نگاشت نام فارسی نرمال‌شده → کد تست (از کاتالوگ بذر) ═══
        var nameToCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in LabTestSeedData.Rows)
        {
            var key = NormalizeFa(t.TestNameFa);
            if (key.Length > 0 && !nameToCode.ContainsKey(key))
                nameToCode[key] = t.TestCode;
        }

        // ردیف‌های ردیابی‌شده — به‌روزرسانی درجا؛ فقط ستون‌های بازه خوانده می‌شوند
        var tests = await _context.LabTests.ToListAsync(ct);
        var applied = 0;

        foreach (var test in tests)
        {
            // تطبیق مرحلهٔ ۱ — کد تست؛ مرحلهٔ ۲ — نام فارسی نرمال‌شده (چرخهٔ ۷۸)
            LabReferenceRangeRow? range = null;
            if (!string.IsNullOrWhiteSpace(test.TestCode) &&
                byCode.TryGetValue(test.TestCode, out var byCodeRow))
            {
                range = byCodeRow;
            }
            else
            {
                var nameKey = NormalizeFa(test.TestName);
                if (nameKey.Length > 0 && nameToCode.TryGetValue(nameKey, out var code) &&
                    byCode.TryGetValue(code, out var byNameRow))
                    range = byNameRow;
            }
            if (range is null) continue;

            // گارد مالکیت (چرخهٔ ۷۸) — فقط هشت ستون بازه/بحرانی «همه تهی» یعنی
            // دست‌نخورده؛ سقف سن کودک از گارد خارج شد تا ردیف‌های نیمه‌پرِ قفل‌شده
            // هم بازه بگیرند. ویرایش مالک روی هر ستون بازه = احترام کامل.
            var untouched = test.RefMinAdultMale is null && test.RefMaxAdultMale is null
                            && test.RefMinAdultFemale is null && test.RefMaxAdultFemale is null
                            && test.RefMinChild is null && test.RefMaxChild is null
                            && test.CriticalLow is null && test.CriticalHigh is null;
            if (!untouched) continue;

            test.RefMinAdultMale = range.MinMale;
            test.RefMaxAdultMale = range.MaxMale;
            test.RefMinAdultFemale = range.MinFemale;
            test.RefMaxAdultFemale = range.MaxFemale;
            test.RefMinChild = range.MinChild;
            test.RefMaxChild = range.MaxChild;
            test.ChildAgeMaxYears = range.ChildAgeMaxYears;
            test.CriticalLow = range.CriticalLow;
            test.CriticalHigh = range.CriticalHigh;
            applied++;
        }

        if (applied > 0)
            await _context.SaveChangesAsync(ct);

        return applied;
    }

    /// <summary>
    /// نرمال‌سازی نام فارسی برای تطبیق — چرخهٔ ۷۸: ی و ک عربی به فارسی،
    /// نیم‌فاصله/تب به فاصله، حذف فاصله‌های تکراری و حاشیه؛ حروف نیز کوچک
    /// می‌شوند تا اختلاف بزرگ/کوچک انگلیسی هم بی‌اثر باشد.
    /// </summary>
    private static string NormalizeFa(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var s = text.Trim()
            .Replace('ي', 'ی')   // ی عربی → فارسی
            .Replace('ك', 'ک')   // ک عربی → فارسی
            .Replace('\u200c', ' ')  // نیم‌فاصله → فاصله (تطبیق آسان‌تر)
            .Replace('\t', ' ');
        while (s.Contains("  "))
            s = s.Replace("  ", " ");
        return s.ToLowerInvariant();
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
        // ═══ چرخهٔ سی‌امین — رفع کرش «An item with the same key has already
        // been added. Key: Isosorbide Dinitrate» ═══
        // ریشه: کاتالوگ داروها به‌طور مشروع چند ردیف با نام ژنریک یکسان دارد
        // (یک مولکول با دوز/فرم/سازندهٔ متفاوت — مثل Isosorbide Dinitrate
        // قرص ۵ و ۱۰ میلی‌گرمی). ترکیب «GroupBy سمت سرور + ToDictionaryAsync»
        // در مواجهه با این تکرارِ مشروع — یا هر اختلاف جزئی نام‌ها مثل فاصلهٔ
        // انتهایی/بزرگی حروف میان ردیف‌ها — استثنای کلید تکراری می‌انداخت و
        // کل بذرافشانِ داده‌های مرجع (دارو + آزمایش + تعرفه) را از کار
        // می‌انداخت؛ در نتیجه بخش بزرگی از داده‌های پایه هرگز بذر نمی‌شد.
        // الگوی امن: بارگذاری سبکِ فقط (شناسه + نام ژنریک) با ToListAsync،
        // سپس ساخت دیکشنری در حافظه با مقایسه‌گر نادیده‌گرفتنِ بزرگی حروف؛
        // «اولین ردیفِ هر نام» ملاک است و تکرارها بی‌صدا نادیده گرفته
        // می‌شوند — نه کرش، نه از دست رفتن داده. مقایسه‌گر بدون حساسیت حروف
        // بونوس هم دارد: نگاشت‌های ResolveMedicationIds دیگر با اختلاف
        // بزرگی حروفِ رکوردهای قدیمی (مثل Ferrous Sulfate / Ferrous
        // sulfate) هم تطبیق پیدا می‌کنند.
        var medRows = await _context.Medications.AsNoTracking()
            .Select(m => new { m.Id, m.GenericName })
            .ToListAsync(ct);

        var medIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in medRows)
        {
            var name = row.GenericName?.Trim();
            if (!string.IsNullOrEmpty(name) && !medIds.ContainsKey(name))
                medIds[name] = row.Id;
        }

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

    /// <summary>
    /// نگاشت نام ژنریک یا نام کلاس به شناسه‌های دارو.
    /// چرخهٔ سی‌امین: دیکشنری ورودی با مقایسه‌گر بدون حساسیت به بزرگی حروف
    /// ساخته می‌شود (رشتهٔ «Ferrous Sulfate» با رکورد «Ferrous sulfate» هم
    /// تطبیق پیدا می‌کند) — بخشی از رفع کرش کلید تکراری بذرافشان.
    /// </summary>
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

            // ═══ چرخهٔ بیست‌ونهم (گزارش هجدهم مالک) — فعال‌سازی تداخل‌های خفته ═══
            // تحلیل پوشش نشان داد ده‌ها جفت تداخلِ از قبل ثبت‌شده در بذر هرگز
            // اعمال نمی‌شدند چون نام یک طرف در سطر تداخل با نام کاتالوگ یکی
            // نبود (Valproate در برابر Sodium Valproate، Lithium در برابر
            // Lithium Carbonate و…) یا کلاسِ بدون نگاشت بود (Beta blockers،
            // ACE inhibitors، Nitrates و…). این نگاشت‌های مستعار آن جفت‌ها را
            // با تغییری در دادهٔ بالینی فعال می‌کنند؛ سقف Take(3) خودکار
            // تعداد اعضا را مهار می‌کند.
            "Valproate" or "Valproic Acid" or "Divalproex" => new[]
                { "Sodium Valproate", "Sodium valproate" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Lithium" when medIds.ContainsKey("Lithium Carbonate") => new[] { medIds["Lithium Carbonate"] },
            "Meperidine" when medIds.ContainsKey("Pethidine") => new[] { medIds["Pethidine"] },
            "Glyburide" when medIds.ContainsKey("Glibenclamide") => new[] { medIds["Glibenclamide"] },
            "Acetylsalicylic Acid" when medIds.ContainsKey("Aspirin") => new[] { medIds["Aspirin"] },
            "Insulin" => new[] { "Insulin regular", "Insulin NPH", "Insulin glargine", "Insulin Aspart" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Penicillin" => new[] { "Penicillin G (Benzylpenicillin)", "Penicillin V" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Thyroxine" when medIds.ContainsKey("Levothyroxine") => new[] { medIds["Levothyroxine"] },
            "Morphine" when medIds.ContainsKey("Morphine Sulfate") => new[] { medIds["Morphine Sulfate"] },
            "Diclofenac" when medIds.ContainsKey("Diclofenac Sodium") => new[] { medIds["Diclofenac Sodium"] },
            "Warfarin (weekly)" when medIds.ContainsKey("Warfarin") => new[] { medIds["Warfarin"] },
            "Digoxin (weekly)" when medIds.ContainsKey("Digoxin") => new[] { medIds["Digoxin"] },
            "Acetaminophen (high-dose)" when medIds.ContainsKey("Acetaminophen") => new[] { medIds["Acetaminophen"] },
            "Metronidazole (course)" when medIds.ContainsKey("Metronidazole") => new[] { medIds["Metronidazole"] },
            "Fluconazole (single)" when medIds.ContainsKey("Fluconazole") => new[] { medIds["Fluconazole"] },
            "Clarithromycin (course)" when medIds.ContainsKey("Clarithromycin") => new[] { medIds["Clarithromycin"] },
            "Lamotrigine (start)" when medIds.ContainsKey("Lamotrigine") => new[] { medIds["Lamotrigine"] },
            "Isotretinoin" when medIds.ContainsKey("Isotretinoin (Oral)") => new[] { medIds["Isotretinoin (Oral)"] },
            "Iron" or "Iron (Ferrous sulfate)" or "Iron supplements" => new[]
                { "Ferrous Sulfate", "Ferrous sulfate", "Ferrous Fumarate" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Vitamin K" when medIds.ContainsKey("Vitamin K1") => new[] { medIds["Vitamin K1"] },
            "Pyridoxine" when medIds.ContainsKey("Vitamin B6 (Pyridoxine)") => new[] { medIds["Vitamin B6 (Pyridoxine)"] },
            "Ascorbic Acid" when medIds.ContainsKey("Vitamin C (Ascorbic Acid)") => new[] { medIds["Vitamin C (Ascorbic Acid)"] },
            "Thiamine" => new[] { "Vitamin B1 (Thiamine)", "Thiamine (Ampoule)" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Oral Contraceptive (Levonorgestrel/EE)" or "Oral contraceptives" or "Combined Oral Contraceptive"
                => new[] { "Combined Oral Contraceptive (Levonorgestrel/EE)", "Ethinylestradiol" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Miconazole (Oral Gel)" when medIds.ContainsKey("Miconazole") => new[] { medIds["Miconazole"] },
            "Amoxicillin/Clavulanate" when medIds.ContainsKey("Amoxicillin/Clavulanic acid")
                => new[] { medIds["Amoxicillin/Clavulanic acid"] },
            "Trimethoprim" or "Sulfamethoxazole" when medIds.ContainsKey("Trimethoprim/Sulfamethoxazole")
                => new[] { medIds["Trimethoprim/Sulfamethoxazole"] },
            "Azole antifungals" => new[] { "Fluconazole", "Ketoconazole", "Itraconazole" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Beta blockers" or "Beta-blocker" => new[] { "Propranolol", "Metoprolol", "Bisoprolol", "Carvedilol" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "ACE inhibitors" => new[] { "Lisinopril", "Enalapril", "Captopril", "Ramipril" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Diuretics" => new[] { "Furosemide", "Hydrochlorothiazide", "Torasemide" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Loop Diuretics" => new[] { "Furosemide", "Torasemide" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Corticosteroids" => new[] { "Prednisolone", "Dexamethasone" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Benzodiazepines" => new[] { "Diazepam", "Alprazolam", "Lorazepam" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "MAO inhibitors" => new[] { "Selegiline", "Phenelzine" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "SSRIs" => new[] { "Sertraline", "Fluoxetine", "Paroxetine", "Citalopram" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Antidepressants" => new[] { "Sertraline", "Fluoxetine", "Amitriptyline" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Aminoglycoside" => new[] { "Gentamicin", "Amikacin" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Nitrates" => new[] { "Nitroglycerin", "Isosorbide Dinitrate", "Isosorbide Mononitrate" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Proton pump inhibitors" or "PPIs long-term" => new[]
                { "Omeprazole", "Pantoprazole", "Lansoprazole", "Esomeprazole" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "NSAIDs" => new[]
                { "Ibuprofen", "Diclofenac Sodium", "Naproxen", "Aspirin", "Mefenamic Acid", "Aceclofenac", "Piroxicam", "Tenoxicam" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Strong CYP3A4 inhibitors" => new[] { "Ketoconazole", "Clarithromycin", "Ritonavir" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "CYP3A4 inducers" => new[] { "Rifampin", "Carbamazepine", "Phenytoin" }
                .Where(medIds.ContainsKey).Select(m => medIds[m]).ToList(),
            "Thiazolidinediones" when medIds.ContainsKey("Pioglitazone") => new[] { medIds["Pioglitazone"] },
            "Lactated Ringer" when medIds.ContainsKey("Ringer Lactate") => new[] { medIds["Ringer Lactate"] },
            "Calcium carbonate" when medIds.ContainsKey("Calcium Carbonate") => new[] { medIds["Calcium Carbonate"] },
            "Potassium chloride" when medIds.ContainsKey("Potassium Chloride") => new[] { medIds["Potassium Chloride"] },
            "Antacid" when medIds.ContainsKey("Aluminum Hydroxide + Magnesium Hydroxide")
                => new[] { medIds["Aluminum Hydroxide + Magnesium Hydroxide"] },
            // موارد صرفاً مستندشده — دارویی در کاتالوگ ندارند و فقط حاشیهٔ آموزشی‌اند
            "Grapefruit juice" or "St John's Wort" or "Live vaccines"
                or "Smoking cessation" or "Kaolin-pectin" or "Contrast agents" or "Ginkgo biloba" or "Ginseng"
                or "Enteric Coated Tablets" or "Neomycin" or "Castor Oil" or "Psyllium"
                => Array.Empty<long>(),
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
            // ═══ چرخهٔ سی‌ودوم (گزارش مالک) ═══ «در تعرفه‌ها برای آزاد (بدون
            // بیمه) تمامی مبالغ بصورت پیش‌فرض سهم بیمار باید باشد» — این ردیف
            // تعرفه «آزاد» است (بدون InsuranceCompanyId)؛ پیش‌تر درصد پوشش
            // بیمهٔ پیش‌فرض (مثلاً ۷۰/۳۰) رویش ذخیره می‌شد و در پذیرشِ آزاد
            // سهم بیمهٔ غیرواقعی می‌ساخت. تعرفهٔ آزاد همیشه ۰٪ بیمه / ۱۰۰٪
            // بیمار است؛ درصد پوشش بذر صرفاً برای تعرفه‌های بیمه‌ای معنا دارد.
            service.Tariffs.Add(new Tariff
            {
                Year = year,
                Price = row.PriceRial,
                InsuranceShare = 0m,
                PatientShare = 100m,
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
