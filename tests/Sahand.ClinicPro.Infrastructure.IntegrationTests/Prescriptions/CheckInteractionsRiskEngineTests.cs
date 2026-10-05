using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;
using Sahand.ClinicPro.Application.Features.Prescriptions.CheckInteractions;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests.Prescriptions;

/// <summary>
/// چرخه سیزدهم (A1): الگوریتم خطر تشخیصی — ۷ بُعد ایمنی بالینی
/// تداخل دارو↔دارو، حساسیت مستقیم و خانواده ATC، رده بارداری، شیردهی، سن، اندام، تشخیص زمینه‌ای
/// </summary>
public class CheckInteractionsRiskEngineTests
{
    private static (CheckInteractionsCommandHandler Handler, ClinicProDbContext Ctx) MakeHandler()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"risk-engine-{Guid.NewGuid():N}")
            .Options;
        var ctx = new ClinicProDbContext(options);

        static Mock<IRepository<T>> RepoMock<T>(ClinicProDbContext c) where T : Sahand.ClinicPro.Domain.Common.BaseEntity
        {
            var mock = new Mock<IRepository<T>>();
            mock.Setup(r => r.QueryableNoTracking).Returns(c.Set<T>().AsNoTracking());
            mock.Setup(r => r.QueryableTracking).Returns(c.Set<T>());
            return mock;
        }

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<Patient>()).Returns(RepoMock<Patient>(ctx).Object);
        uow.Setup(u => u.Repository<Medication>()).Returns(RepoMock<Medication>(ctx).Object);
        uow.Setup(u => u.Repository<DrugInteraction>()).Returns(RepoMock<DrugInteraction>(ctx).Object);
        uow.Setup(u => u.Repository<Visit>()).Returns(RepoMock<Visit>(ctx).Object);

        return (new CheckInteractionsCommandHandler(uow.Object), ctx);
    }

    private static Medication Med(long id, string generic, string? fa = null, string atc = "N02BE01",
        PregnancyCategory preg = PregnancyCategory.NotRated, bool lactation = false,
        bool pediatric = false, bool geriatric = false, bool renal = false,
        string? maxDose = null, bool sedation = false, string? contra = null) => new()
    {
        Id = id,
        GenericName = generic,
        GenericNameFa = fa ?? generic,
        AtcCode = atc,
        PregnancyCategory = preg,
        IsLactationRisk = lactation,
        NeedsPediatricCaution = pediatric,
        NeedsGeriatricCaution = geriatric,
        NeedsRenalCaution = renal,
        MaxDailyDose = maxDose,
        CausesSedation = sedation,
        Contraindications = contra
    };

    private static Patient FemalePatient(long id, int age, string? allergies = null) => new()
    {
        Id = id,
        MedicalRecordNo = $"MRN-{id:D8}",
        NationalCode = $"00{id:D9}"[..10],
        Mobile = $"0912000{id:D4}"[..11],
        FirstName = "زهرا",
        LastName = "محمدی",
        Gender = Gender.Female,
        BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-age)),
        Allergies = allergies
    };

    private static Patient MalePatient(long id, int age) => new()
    {
        Id = id,
        MedicalRecordNo = $"MRN-{id:D8}",
        NationalCode = $"00{id:D9}"[..10],
        Mobile = $"0912000{id:D4}"[..11],
        FirstName = "رضا",
        LastName = "کریمی",
        Gender = Gender.Male,
        BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-age))
    };

    /// <summary>رده X در زن باردار → منع مطلق (شدت ۴) + پرچم بارداری</summary>
    [Fact]
    public async Task Pregnancy_CategoryX_WithPregnantFlag_Blocks()
    {
        var (handler, ctx) = MakeHandler();
        ctx.Patients.Add(FemalePatient(1, 30));
        ctx.Medications.Add(Med(10, "Methotrexate", "متوترکسات", "L01BA01", PregnancyCategory.X));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CheckInteractionsCommand(new CheckInteractionsRequest(1, new List<long> { 10 }, IsPregnant: true)),
            CancellationToken.None)).Value;

        result.HasPregnancyRisk.Should().BeTrue();
        result.HasAbsoluteBlock.Should().BeTrue();
        result.Interactions.Should().Contain(i =>
            i.Kind == RiskKind.Pregnancy && i.Severity == 4);
    }

    /// <summary>رده X در زن سنین باروری بدون اعلام بارداری → منع (خطر بارداری ناخواسته)</summary>
    [Fact]
    public async Task Pregnancy_CategoryX_ChildbearingAge_AlsoBlocks()
    {
        var (handler, ctx) = MakeHandler();
        ctx.Patients.Add(FemalePatient(1, 27));
        ctx.Medications.Add(Med(10, "Warfarin", "وارفارین", "B01AA03", PregnancyCategory.X));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CheckInteractionsCommand(new CheckInteractionsRequest(1, new List<long> { 10 })),
            CancellationToken.None)).Value;

        result.HasPregnancyRisk.Should().BeTrue();
        result.HasAbsoluteBlock.Should().BeTrue();
    }

    /// <summary>حساسیت مستقیم — تطبیق نام ژنریک فارسی با آلرژی پرونده</summary>
    [Fact]
    public async Task Allergy_DirectMatch_FlagsConflict()
    {
        var (handler, ctx) = MakeHandler();
        ctx.Patients.Add(FemalePatient(1, 40,
            """["آموکسی‌سیلین"]"""));
        ctx.Medications.Add(Med(10, "Amoxicillin", "آموکسی‌سیلین", "J01CA04"));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CheckInteractionsCommand(new CheckInteractionsRequest(1, new List<long> { 10 })),
            CancellationToken.None)).Value;

        result.HasAllergyConflict.Should().BeTrue();
        result.HasAbsoluteBlock.Should().BeTrue();
        result.Interactions.Should().Contain(i => i.Kind == RiskKind.Allergy && i.Severity == 4);
    }

    /// <summary>حساسیت متقاطع خانواده ATC — آلرژی پنی‌سیلین + آموکسی‌سیلین J01C → هشدار</summary>
    [Fact]
    public async Task Allergy_AtcFamilyCrossMatch_Warns()
    {
        var (handler, ctx) = MakeHandler();
        ctx.Patients.Add(FemalePatient(1, 35, """["پنی‌سیلین"]"""));
        ctx.Medications.Add(Med(10, "Piperacillin", "پیپراسیلین", "J01CA12"));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CheckInteractionsCommand(new CheckInteractionsRequest(1, new List<long> { 10 })),
            CancellationToken.None)).Value;

        result.Interactions.Should().Contain(i => i.Kind == RiskKind.Allergy);
    }

    /// <summary>کودک زیر ۱۲ سال + پرچم احتیاط اطفال → هشدار سنی</summary>
    [Fact]
    public async Task PediatricCaution_Under12_Warns()
    {
        var (handler, ctx) = MakeHandler();
        ctx.Patients.Add(MalePatient(1, 8));
        ctx.Medications.Add(Med(10, "Tramadol", "ترامادول", "N02AX02", pediatric: true, sedation: true));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CheckInteractionsCommand(new CheckInteractionsRequest(1, new List<long> { 10 })),
            CancellationToken.None)).Value;

        result.Interactions.Should().Contain(i => i.Kind == RiskKind.AgeCaution);
        result.Interactions.Should().Contain(i => i.Kind == RiskKind.DoseCaution); // خواب‌آلودگی
    }

    /// <summary>سالمند ۷۰ ساله + پرچم Beers → هشدار سالمندی</summary>
    [Fact]
    public async Task GeriatricCaution_Over65_Warns()
    {
        var (handler, ctx) = MakeHandler();
        ctx.Patients.Add(MalePatient(1, 70));
        ctx.Medications.Add(Med(10, "Diazepam", "دیازپام", "N05BA01", geriatric: true));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CheckInteractionsCommand(new CheckInteractionsRequest(1, new List<long> { 10 })),
            CancellationToken.None)).Value;

        result.Interactions.Should().Contain(i =>
            i.Kind == RiskKind.AgeCaution && i.Description!.Contains("سالمند"));
    }

    /// <summary>شیردهی اعلام‌شده + داروی دارای پرچم شیردهی → هشدار شیردهی</summary>
    [Fact]
    public async Task LactationFlag_WithLactationRiskMed_Warns()
    {
        var (handler, ctx) = MakeHandler();
        ctx.Patients.Add(FemalePatient(1, 29));
        ctx.Medications.Add(Med(10, "Codeine", "کدئین", "N02AA59", lactation: true));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CheckInteractionsCommand(new CheckInteractionsRequest(1, new List<long> { 10 }, IsBreastfeeding: true)),
            CancellationToken.None)).Value;

        result.Interactions.Should().Contain(i => i.Kind == RiskKind.Lactation);
    }

    /// <summary>تشخیص زمینه‌ای صرع + منع مصرف دارو در صرع → تعارض زمینه‌ای</summary>
    [Fact]
    public async Task DiseaseConflict_EpilepsyInHistory_Warns()
    {
        var (handler, ctx) = MakeHandler();
        ctx.Patients.Add(FemalePatient(1, 33));
        ctx.Visits.Add(new Visit
        {
            Id = 1, PatientId = 1, DoctorId = 1,
            VisitDate = DateTime.UtcNow.AddDays(-30),
            ChiefComplaint = "تشنج مکرر", SoapAssessment = "صرع کنترل‌نشده"
        });
        ctx.Medications.Add(Med(10, "Ciprofloxacin", "سیپروفلوکساسین", "J01MA02",
            contra: "منع مصرف در صرع و نارسایی کلیه"));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CheckInteractionsCommand(new CheckInteractionsRequest(1, new List<long> { 10 })),
            CancellationToken.None)).Value;

        result.Interactions.Should().Contain(i => i.Kind == RiskKind.DiseaseConflict);
    }

    /// <summary>تداخل دارو-دارو از جدول مرجع — وارفارین × آسپرین شدید</summary>
    [Fact]
    public async Task DrugDrugInteraction_FromReferenceTable_FlagsSevere()
    {
        var (handler, ctx) = MakeHandler();
        ctx.Patients.Add(MalePatient(1, 60));
        ctx.Medications.AddRange(
            Med(10, "Warfarin", "وارفارین", "B01AA03"),
            Med(11, "Aspirin", "آسپرین", "N02BA01"));
        ctx.DrugInteractions.Add(new DrugInteraction
        {
            Medication1Id = 10, Medication2Id = 11,
            Severity = DrugInteractionSeverity.Major,
            Description = "افزایش خطر خونریزی"
        });
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CheckInteractionsCommand(new CheckInteractionsRequest(1, new List<long> { 10, 11 })),
            CancellationToken.None)).Value;

        result.HasSevere.Should().BeTrue();
        result.Interactions.Should().Contain(i => i.Kind == RiskKind.DrugInteraction && i.Severity == 3);
    }

    /// <summary>بدون خطر — پیام سبز و هیچ پرچمی (فقط اطلاع‌رسانی سقف دوز)</summary>
    [Fact]
    public async Task SafeCombination_NoFlags()
    {
        var (handler, ctx) = MakeHandler();
        ctx.Patients.Add(FemalePatient(1, 45));
        ctx.Medications.Add(Med(10, "Acetaminophen", "استامینوفن", "N02BE01",
            preg: PregnancyCategory.B, maxDose: "۴۰۰۰mg"));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CheckInteractionsCommand(new CheckInteractionsRequest(1, new List<long> { 10 })),
            CancellationToken.None)).Value;

        result.HasSevere.Should().BeFalse();
        result.HasAllergyConflict.Should().BeFalse();
        result.HasPregnancyRisk.Should().BeFalse();
        result.HasAbsoluteBlock.Should().BeFalse();
        result.Interactions.Should().OnlyContain(i => i.Severity == 1);
    }
}
