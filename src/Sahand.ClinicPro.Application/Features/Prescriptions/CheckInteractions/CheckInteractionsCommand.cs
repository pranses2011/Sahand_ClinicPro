using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;

namespace Sahand.ClinicPro.Application.Features.Prescriptions.CheckInteractions;

/// <summary>بررسی خطر تشخیصی جامع — چرخه ۱۳ (درخواست ۱-۱ سند ایده‌ها)</summary>
public record CheckInteractionsCommand(CheckInteractionsRequest Request)
    : IRequest<Result<CheckInteractionsResult>>;

/// <summary>
/// گرداننده الگوریتم خطر تشخیصی — ۷ پرامپت ایمنی بالینی:
/// ۱) تداخل دارو↔دارو از جدول مرجع ۵۲۳ جفت‌ای
/// ۲) حساسیت دارویی مستقیم (تطبیق نام) + تعرض خانواده دارویی (پیشوند ATC ۴ نویسه)
/// ۳) رده بارداری FDA — زن در سنین باروری یا باردار اعلام‌شده + رده C/D/X
/// ۴) شیردهی — داروهای دارای پرچم خطر شیردهی
/// ۵) سن — کودک زیر ۱۲ / سالمند بالای ۶۵ (Beers Criteria)
/// ۶) نارسایی اندام — احتیاط کلیوی/کبدی
/// ۷) تشخیص زمینه‌ای — تطبیق کلیدواژه تشخیص‌های اخیر با منع مصرف دارو
/// به‌علاوه هشدارهای دوز/عوارض (حداکثر دوز روزانه، خواب‌آلودگی، وابستگی)
/// </summary>
public class CheckInteractionsCommandHandler
    : IRequestHandler<CheckInteractionsCommand, Result<CheckInteractionsResult>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public CheckInteractionsCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — ارزیابی کامل خطر نسخه</summary>
    public async Task<Result<CheckInteractionsResult>> Handle(CheckInteractionsCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;
        var result = new List<InteractionDto>();
        var medIds = dto.MedicationIds.Distinct().ToList();
        if (medIds.Count == 0)
            return Result<CheckInteractionsResult>.Success(new CheckInteractionsResult(
                result, false, false));

        // ═══ بارگذاری بیمار + محاسبه سن ═══
        var patient = await _unitOfWork.Repository<Patient>().QueryableNoTracking
            .FirstOrDefaultAsync(p => p.Id == dto.PatientId, cancellationToken);
        int? age = null;
        if (patient?.BirthDate is { } bd)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            age = today.Year - bd.Year - (today.DayOfYear < bd.DayOfYear ? 1 : 0);
            if (age < 0 || age > 130) age = null;
        }

        // ═══ بارگذاری داروها با همه فیلدهای خطر چرخه ۱۳ ═══
        var meds = await _unitOfWork.Repository<Medication>().QueryableNoTracking
            .Where(m => medIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m, cancellationToken);
        var nameOf = new Func<long, string>(id =>
            meds.GetValueOrDefault(id) is { } m
                ? $"{(string.IsNullOrWhiteSpace(m.GenericNameFa) ? m.GenericName : m.GenericNameFa)}{(string.IsNullOrWhiteSpace(m.BrandName) ? "" : $" ({m.BrandName})")}"
                : "؟");

        // ═══ ۱) تداخل دارو-دارو از جدول مرجع ═══
        var pairs = await _unitOfWork.Repository<DrugInteraction>().QueryableNoTracking
            .Where(d => medIds.Contains(d.Medication1Id) && medIds.Contains(d.Medication2Id))
            .ToListAsync(cancellationToken);
        foreach (var pair in pairs)
        {
            result.Add(new InteractionDto(
                nameOf(pair.Medication1Id), nameOf(pair.Medication2Id),
                (int)pair.Severity, SeverityText(pair.Severity), SeverityColor(pair.Severity),
                pair.Mechanism, pair.Recommendation, pair.Description,
                false, RiskKind.DrugInteraction, "💊", "تداخل دارویی"));
        }

        // ═══ ۲) حساسیت دارویی — تطبیق مستقیم + تعرض خانواده ATC ═══
        var allergies = ParseAllergies(patient?.Allergies);
        if (allergies.Count > 0)
        {
            foreach (var (medId, med) in meds)
            {
                var name = nameOf(medId);
                var haystacks = new[] { med.GenericName, med.GenericNameFa, med.BrandName }
                    .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()).ToList();

                // ۲-الف) تطبیق مستقیم نام
                var direct = allergies.FirstOrDefault(a =>
                    haystacks.Any(h => h.Contains(a, StringComparison.OrdinalIgnoreCase)));
                if (direct is not null)
                {
                    result.Add(new InteractionDto(
                        name, $"آلرژی بیمار: {direct}",
                        4, "ممنوع مصرف — حساسیت شناخته‌شده", "#B71C1C",
                        "بیمار به این ماده حساسیت دارویی ثبت‌شده دارد",
                        "حذف دارو از نسخه الزامی است — در صورت اصرار پزشک تأیید صریح و ثبت دلیل لازم است",
                        $"تعارض با حساسیت ثبت‌شده پرونده ({direct})",
                        true, RiskKind.Allergy, "🚫", "حساسیت دارویی"));
                    continue;
                }

                // ۲-ب) تعرض خانواده دارویی — پیشوند ATC ۴ نویسه (مثل J01C پنی‌سیلین‌ها)
                if (!string.IsNullOrWhiteSpace(med.AtcCode) && med.AtcCode!.Length >= 4)
                {
                    var family = med.AtcCode[..4];
                    var familyAllergy = allergies.FirstOrDefault(a =>
                        a.Length >= 4 && AtcFamilyOf(a) is { } fa && fa == family);
                    if (familyAllergy is not null)
                    {
                        result.Add(new InteractionDto(
                            name, $"حساسیت خانواده دارویی: {familyAllergy}",
                            3, "احتیاط — هم‌خانواده ماده حساسیت‌زا", "#FFEBEE",
                            $"این دارو و «{familyAllergy}» در یک خانواده دارویی ATC ({family}) هستند — احتمال حساسیت متقاطع",
                            "انتخاب داروی خانواده دیگر یا مشاوره آلرژیست توصیه می‌شود",
                            "تعارض احتمالی خانواده دارویی با حساسیت بیمار",
                            true, RiskKind.Allergy, "⚠️", "حساسیت متقاطع"));
                    }
                }
            }
        }

        // ═══ ۳) بارداری — رده FDA ═══
        var female = patient?.Gender == Gender.Female;
        var childbearing = female && age is >= 12 and <= 55; // سنین باروری — بدون اعلام بارداری هم رده X/D هشدار می‌گیرد
        var pregnant = dto.IsPregnant;
        if (female)
        {
            foreach (var (medId, med) in meds)
            {
                if (med.PregnancyCategory is PregnancyCategory.D or PregnancyCategory.X)
                {
                    // رده X در سنین باروری هم منع (خطر بارداری ناخواسته)؛ رده D فقط با اعلام بارداری منع
                    var block = med.PregnancyCategory == PregnancyCategory.X
                                ? (pregnant || childbearing)
                                : pregnant;
                    result.Add(new InteractionDto(
                        nameOf(medId), $"رده بارداری {med.PregnancyCategory}",
                        block ? 4 : 3,
                        block ? "منع مصرف در بارداری" : "خطر بالا در بارداری",
                        block ? "#B71C1C" : "#FFEBEE",
                        "رده‌بندی FDA دارو در بارداری",
                        med.PregnancyCategory == PregnancyCategory.X
                            ? "در بارداری مطلقاً منع مصرف — جایگزین بی‌خطر انتخاب شود"
                            : "فقط با توجیه قوی پزشک و اطلاع بیمار — جایگزین رده A/B ارجح است",
                        $"بیمار زن {(pregnant ? "باردار" : "در سنین باروری")} است و دارو رده {med.PregnancyCategory} دارد",
                        false, RiskKind.Pregnancy, "🤰", "خطر بارداری"));
                }
                else if (pregnant && med.PregnancyCategory == PregnancyCategory.C)
                {
                    result.Add(new InteractionDto(
                        nameOf(medId), "رده بارداری C",
                        2, "احتیاط در بارداری", "#FFF3E0",
                        "مطالعه حیوانی عارضه‌دار — داده انسانی ناکافی",
                        "در صورت امکان داروی رده A/B جایگزین شود",
                        "بیمار باردار است و دارو رده C دارد",
                        false, RiskKind.Pregnancy, "🤰", "خطر بارداری"));
                }
            }
        }

        // ═══ ۴) شیردهی ═══
        if (female && dto.IsBreastfeeding)
        {
            foreach (var (medId, med) in meds.Where(m => m.Value.IsLactationRisk))
            {
                result.Add(new InteractionDto(
                    nameOf(medId), "شیردهی فعال",
                    3, "احتیاط در شیردهی", "#FFF3E0",
                    "انتقال دارو به شیر مادر محتمل است",
                    "پایش نوزاد برای خواب‌آلودگی/بی‌اشتهایی یا انتخاب جایگزین بی‌خطر",
                    "بیمار شیردهی اعلام کرده و دارو دارای پرچم خطر شیردهی است",
                    false, RiskKind.Lactation, "🍼", "خطر شیردهی"));
            }
        }

        // ═══ ۵) سن — کودک/سالمند ═══
        foreach (var (medId, med) in meds)
        {
            if (age is < 12 && med.NeedsPediatricCaution)
            {
                result.Add(new InteractionDto(
                    nameOf(medId), $"کودک {age} ساله",
                    2, "احتیاط اطفال", "#FFF3E0",
                    "دوز و فرم دارو برای کودکان باید تنظیم شود",
                    "محاسبه دوز بر اساس وزن (mg/kg) و انتخاب فرم مناسب شربت/قطره",
                    $"سن بیمار {age} سال است و دارو نیاز به احتیاط اطفال دارد",
                    false, RiskKind.AgeCaution, "🧒", "احتیاط سنی"));
            }
            if (age is >= 65 && med.NeedsGeriatricCaution)
            {
                result.Add(new InteractionDto(
                    nameOf(medId), $"سالمند {age} ساله",
                    2, "احتیاط سالمندی (Beers)", "#FFF3E0",
                    "خطر عوارض در سالمندان بالاتر است (سقوط، خونریزی، تداخل چنددارویی)",
                    "شروع با کمترین دوز مؤثر و پایش عوارض",
                    $"سن بیمار {age} سال است و دارو در فهرست احتیاط سالمندان است",
                    false, RiskKind.AgeCaution, "🧓", "احتیاط سنی"));
            }
        }

        // ═══ ۶) نارسایی اندام ═══
        foreach (var (medId, med) in meds)
        {
            if (med.NeedsRenalCaution)
                result.Add(new InteractionDto(
                    nameOf(medId), "کارکرد کلیه",
                    2, "تعدیل دوز کلیوی", "#FFF9C4",
                    "دفع دارو وابسته به کارکرد کلیه است",
                    "بررسی CrCl و تعدیل دوز در صورت کاهش کارکرد",
                    "دارو نیاز به تعدیل دوز کلیوی دارد",
                    false, RiskKind.OrganCaution, "🫀", "احتیاط کلیوی"));
            if (med.NeedsHepaticCaution)
                result.Add(new InteractionDto(
                    nameOf(medId), "کارکرد کبد",
                    2, "احتیاط کبدی", "#FFF9C4",
                    "متابولیسم کبدی دارو — خطر تجمع در بیماری کبدی",
                    "پایش آنزیم‌های کبدی و کاهش دوز در سیروز/هپاتیت",
                    "دارو نیاز به احتیاط کبدی دارد",
                    false, RiskKind.OrganCaution, "🫁", "احتیاط کبدی"));
        }

        // ═══ ۷) تشخیص زمینه‌ای — کلیدواژه‌های منع مصرف ═══
        var recentDiagnoses = await _unitOfWork.Repository<Visit>().QueryableNoTracking
            .Where(v => v.PatientId == dto.PatientId && v.VisitDate >= DateTime.UtcNow.AddYears(-2))
            .OrderByDescending(v => v.VisitDate)
            .Select(v => new { v.ChiefComplaint, v.SoapAssessment, DxFa = v.PrimaryDiagnosis!.DescriptionFa, DxEn = v.PrimaryDiagnosis!.Description })
            .Take(30)
            .ToListAsync(cancellationToken);
        var diagnosisText = string.Join(" | ", recentDiagnoses
            .Select(d => $"{d.ChiefComplaint} {d.SoapAssessment} {d.DxFa} {d.DxEn}"));
        if (!string.IsNullOrWhiteSpace(diagnosisText))
        {
            foreach (var (medId, med) in meds)
            {
                if (string.IsNullOrWhiteSpace(med.Contraindications)) continue;
                var hit = DiseaseKeywordHits(diagnosisText, med.Contraindications!);
                if (hit is not null)
                {
                    result.Add(new InteractionDto(
                        nameOf(medId), $"زمینه: {hit.Label}",
                        3, "تعارض با زمینه بیمار", "#FFEBEE",
                        $"سابقه اخیر بیمار شامل «{hit.Label}» است و دارو منع مصرف در آن دارد",
                        "بازبینی ضروری — در صورت نیاز مشاوره متخصص",
                        $"تشخیص‌های اخیر بیمار با منع مصرف دارو تعارض دارد: {hit.Label}",
                        false, RiskKind.DiseaseConflict, "📋", "تعارض زمینه‌ای"));
                }
            }
        }

        // ═══ ۸) هشدارهای دوز/عوارض ═══
        foreach (var (medId, med) in meds)
        {
            if (!string.IsNullOrWhiteSpace(med.MaxDailyDose))
                result.Add(new InteractionDto(
                    nameOf(medId), med.MaxDailyDose!,
                    1, "حداکثر دوز روزانه", "#ECEFF1",
                    "عبور از سقف دوز روزانه خطر سمیت دارد",
                    $"دوز روزانه نباید از «{med.MaxDailyDose}» عبور کند",
                    "سقف دوز روزانه دارو — پایش مجموع دوز اقلام هم‌نام نسخه",
                    false, RiskKind.DoseCaution, "⚖️", "حداکثر دوز"));
            if (med.CausesSedation)
                result.Add(new InteractionDto(
                    nameOf(medId), "خواب‌آلودگی",
                    1, "هشدار رانندگی", "#ECEFF1",
                    "دارو باعث خواب‌آلودگی و کاهش تمرکز می‌شود",
                    "توصیه پرهیز از رانندگی و کار با ماشین‌آلات تا مشخص‌شدن پاسخ دارو",
                    "دارو دارای عارضه خواب‌آلودگی است",
                    false, RiskKind.DoseCaution, "😴", "خواب‌آلودگی"));
            if (med.HasAbusePotential)
                result.Add(new InteractionDto(
                    nameOf(medId), "پتانسیل وابستگی",
                    2, "داروی تحت کنترل", "#FFF9C4",
                    "خطر وابستگی و اعتیاد با مصرف طولانی",
                    "نسخه دوره کوتاه + ثبت در دفتر مواد مخدر در صورت لزوم",
                    "دارو دارای پتانسیل وابستگی است",
                    false, RiskKind.DoseCaution, "🔒", "وابستگی"));
        }

        // ═══ خلاصه نتیجه ═══
        var hasSevere = result.Any(r => r.Severity >= 3);
        var hasAllergy = result.Any(r => r.IsAllergyConflict);
        var hasPregnancy = result.Any(r => r.Kind == RiskKind.Pregnancy);
        var hasAbsoluteBlock = result.Any(r => r.Severity >= 4);

        return Result<CheckInteractionsResult>.Success(new CheckInteractionsResult(
            result.OrderByDescending(r => r.Severity).ThenBy(r => r.Kind).ToList(),
            hasSevere, hasAllergy, hasPregnancy, hasAbsoluteBlock));
    }

    /// <summary>تفسیر JSON یا متن آزاد حساسیت‌ها به فهرست</summary>
    private static List<string> ParseAllergies(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(raw);
            if (list is not null) return list.Where(a => !string.IsNullOrWhiteSpace(a)).ToList()!;
        }
        catch { /* متن آزاد */ }
        return raw.Split(new[] { ',', ';', '،', '؛' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim()).Where(a => a.Length > 1).ToList();
    }

    /// <summary>نقشه ماده حساسیت‌زا → پیشوند خانواده ATC (۴ نویسه)</summary>
    private static readonly Dictionary<string, string> AtcFamilyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["پنی‌سیلین"] = "J01C", ["پنی سیلین"] = "J01C", ["Penicillin"] = "J01C",
        ["آموکسی‌سیلین"] = "J01C", ["آموکسی سیلین"] = "J01C", ["Amoxicillin"] = "J01C",
        ["آمپی‌سیلین"] = "J01C", ["Ampicillin"] = "J01C",
        ["سفالوسپورین"] = "J01D", ["سفالوسپورین‌ها"] = "J01D", ["Cephalosporin"] = "J01D",
        ["سفیکسیم"] = "J01D", ["سفتریاکسون"] = "J01D",
        ["سولفا"] = "J01E", ["سولفونامید"] = "J01E", ["کوتریموکسازول"] = "J01E",
        ["آسپرین"] = "N02BA", ["ASA"] = "N02BA", ["سالسیلات"] = "N02BA",
        ["ایبوپروفن"] = "M01AE", ["دیکلوفناک"] = "M01AB", ["ندوستا"] = "M01AE",
        ["NSAID"] = "M01A", ["مضاد التهاب غیراستروئیدی"] = "M01A",
        ["کدئین"] = "N02AA", ["مورفین"] = "N02AA", ["ترامادول"] = "N02AX",
        ["وانکومایسین"] = "J01XA", ["مترونیدازول"] = "J01XD",
        ["سیپروفلوکساسین"] = "J01MA", ["فلوروکینولون"] = "J01M",
        ["دکسامتازون"] = "H02AB", ["پردنیزولون"] = "H02AB", ["کورتون"] = "H02AB",
        ["هپارین"] = "B01AB", ["لیدوکائین"] = "N01BB", ["ید"] = "V08",
        ["آیودی"] = "V08", ["کنتراست یددار"] = "V08"
    };

    /// <summary>یافتن خانواده ATC برای ماده حساسیت‌زا — جستجو در نقشه</summary>
    private static string? AtcFamilyOf(string allergen)
    {
        foreach (var (key, family) in AtcFamilyMap)
            if (allergen.Contains(key, StringComparison.OrdinalIgnoreCase)) return family;
        return null;
    }

    /// <summary>کلیدواژه‌های تشخیص زمینه‌ای برای تطبیق منع مصرف</summary>
    private sealed record DiseaseKeyword(string[] Keywords, string Label);

    private static readonly DiseaseKeyword[] DiseaseKeywords =
    {
        new(new[] { "دیابت", "قند خون", "دیابت" }, "دیابت"),
        new(new[] { "آسم", "تنگی نفس", "برونش" }, "آسم/بیماری تنفسی"),
        new(new[] { "صرع", "تشنج" }, "صرع"),
        new(new[] { "قلب", "آنژین", "نارسایی قلبی", "آریتمی", "MI" }, "بیماری قلبی"),
        new(new[] { "فشار خون", "هیپرتانسیون" }, "فشار خون"),
        new(new[] { "کلیه", "رنال", "دیالیز" }, "نارسایی کلیه"),
        new(new[] { "کبد", "سیروز", "هپاتیت" }, "بیماری کبدی"),
        new(new[] { "تیروئید", "هیپرتیروئید", "هیپوتیروئید" }, "بیماری تیروئید"),
        new(new[] { "گلوکوم", "فشار چشم" }, "گلوکوم"),
        new(new[] { "زخم معده", "اولسر", " التهاب معده", "گاستریت" }, "زخم گوارشی"),
        new(new[] { "مینینیت", "مینیر" }, "سرگیجه می‌نیر"),
        new(new[] { "پورفیری" }, "پورفیری"),
        new(new[] { "میاستنی", "میاستنی گراویس" }, "میاستنی گراویس")
    };

    /// <summary>تطبیق متن تشخیص‌ها با کلیدواژه‌های منع مصرف — فقط وقتی منع مصرف هم‌قلم کلیدواژه را ذکر کند</summary>
    private static DiseaseKeyword? DiseaseKeywordHits(string diagnosisText, string contraindications)
    {
        foreach (var dk in DiseaseKeywords)
        {
            var inDiagnosis = dk.Keywords.Any(k =>
                diagnosisText.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (!inDiagnosis) continue;
            var labelRoot = dk.Label.Split('/')[0];
            var inContra = ContraindicationMentions(contraindications, dk, labelRoot);
            if (inContra) return dk;
        }
        return null;
    }

    /// <summary>بررسی ذکر بیماری در متن منع مصرف دارو</summary>
    private static bool ContraindicationMentions(string contra, DiseaseKeyword dk, string labelRoot)
    {
        _ = labelRoot;
        if (dk.Label == "دیابت" && contra.Contains("دیابت", StringComparison.OrdinalIgnoreCase)) return true;
        if (dk.Label == "آسم/بیماری تنفسی" && (contra.Contains("آسم", StringComparison.OrdinalIgnoreCase) || contra.Contains("تنفسی", StringComparison.OrdinalIgnoreCase))) return true;
        if (dk.Label == "صرع" && contra.Contains("صرع", StringComparison.OrdinalIgnoreCase)) return true;
        if (dk.Label == "بیماری قلبی" && (contra.Contains("قلب", StringComparison.OrdinalIgnoreCase) || contra.Contains("کاردیاک", StringComparison.OrdinalIgnoreCase))) return true;
        if (dk.Label == "فشار خون" && contra.Contains("فشار خون", StringComparison.OrdinalIgnoreCase)) return true;
        if (dk.Label == "نارسایی کلیه" && (contra.Contains("کلیه", StringComparison.OrdinalIgnoreCase) || contra.Contains("رنال", StringComparison.OrdinalIgnoreCase))) return true;
        if (dk.Label == "بیماری کبدی" && (contra.Contains("کبد", StringComparison.OrdinalIgnoreCase) || contra.Contains("هپات", StringComparison.OrdinalIgnoreCase))) return true;
        if (dk.Label == "بیماری تیروئید" && contra.Contains("تیروئید", StringComparison.OrdinalIgnoreCase)) return true;
        if (dk.Label == "گلوکوم" && contra.Contains("گلوکوم", StringComparison.OrdinalIgnoreCase)) return true;
        if (dk.Label == "زخم گوارشی" && (contra.Contains("زخم", StringComparison.OrdinalIgnoreCase) || contra.Contains("گوارش", StringComparison.OrdinalIgnoreCase))) return true;
        return false;
    }

    /// <summary>متن شدت</summary>
    private static string SeverityText(DrugInteractionSeverity severity) => severity switch
    {
        DrugInteractionSeverity.Minor => "خفیف",
        DrugInteractionSeverity.Moderate => "متوسط",
        DrugInteractionSeverity.Major => "شدید",
        DrugInteractionSeverity.Contraindicated => "ممنوع مصرف",
        _ => "نامشخص"
    };

    /// <summary>رنگ سطح</summary>
    private static string SeverityColor(DrugInteractionSeverity severity) => severity switch
    {
        DrugInteractionSeverity.Minor => "#FFF9C4",
        DrugInteractionSeverity.Moderate => "#FFF3E0",
        DrugInteractionSeverity.Major => "#FFEBEE",
        DrugInteractionSeverity.Contraindicated => "#B71C1C",
        _ => "#ECEFF1"
    };
}
