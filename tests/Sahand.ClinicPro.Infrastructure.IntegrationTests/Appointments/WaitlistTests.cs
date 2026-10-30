using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Application.Features.Appointments.Waitlist;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests.Appointments;

/// <summary>
/// چرخه سیزدهم (C1): لیست انتظار — ثبت + پیشنهاد خودکار جای خالی + اولویت‌بندی
/// </summary>
public class WaitlistTests
{
    private static (NotifyWaitlistForFreedSlotCommandHandler Handler, ClinicProDbContext Ctx) MakeNotifier(
        Mock<Sahand.ClinicPro.Application.Interfaces.ISmsService>? sms = null)
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"waitlist-{Guid.NewGuid():N}")
            .Options;
        var ctx = new ClinicProDbContext(options);

        static Mock<IRepository<T>> Repo<T>(ClinicProDbContext c) where T : Sahand.ClinicPro.Domain.Common.BaseEntity
        {
            var mock = new Mock<IRepository<T>>();
            mock.Setup(r => r.QueryableNoTracking).Returns(c.Set<T>().AsNoTracking());
            mock.Setup(r => r.QueryableTracking).Returns(c.Set<T>());
            return mock;
        }

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<WaitlistEntry>()).Returns(Repo<WaitlistEntry>(ctx).Object);
        uow.Setup(u => u.Repository<Doctor>()).Returns(Repo<Doctor>(ctx).Object);
        uow.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async _ => await ctx.SaveChangesAsync(_) > 0);

        // سرویس پیامک بی‌اثر — تست منطق پیشنهاد است نه ارسال فیزیکی.
        if (sms is null)
        {
            sms = new Mock<Sahand.ClinicPro.Application.Interfaces.ISmsService>();
            sms.Setup(s => s.EnqueueRawAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<byte>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        return (new NotifyWaitlistForFreedSlotCommandHandler(uow.Object, sms.Object), ctx);
    }

    private static WaitlistEntry Entry(long id, long patientId, long? doctorId,
        DateOnly from, DateOnly to, Priority priority = Priority.Normal) => new()
    {
        Id = id,
        PatientId = patientId,
        DoctorId = doctorId,
        PreferredFrom = from,
        PreferredTo = to,
        Priority = priority,
        Status = WaitlistStatus.Waiting,
        ContactMobile = "09140000000"
    };

    /// <summary>متناظرترین منتظر — اولویت بالاتر برنده می‌شود</summary>
    [Fact]
    public async Task Offer_HigherPriorityWins()
    {
        var (handler, ctx) = MakeNotifier();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        ctx.WaitlistEntries.AddRange(
            Entry(1, 10, 5, date.AddDays(-1), date.AddDays(3), Priority.Normal),
            Entry(2, 11, 5, date, date, Priority.High));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new NotifyWaitlistForFreedSlotCommand(900, 5, date, new TimeOnly(10, 30)),
            CancellationToken.None)).Value;

        result.Should().Be(2); // اولویت High
        var winner = await ctx.WaitlistEntries.AsNoTracking().FirstAsync(w => w.Id == 2);
        winner.Status.Should().Be(WaitlistStatus.Notified);
        winner.NotifiedAt.Should().NotBeNull();
        winner.OfferedAppointmentId.Should().Be(900);
    }

    /// <summary>بازه تاریخ باید شامل تاریخ نوبت آزاد باشد</summary>
    [Fact]
    public async Task Offer_DateRangeMustIncludeAppointmentDate()
    {
        var (handler, ctx) = MakeNotifier();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        ctx.WaitlistEntries.AddRange(
            Entry(1, 10, 5, date.AddDays(2), date.AddDays(6)), // بازه بعد از نوبت
            Entry(2, 11, 5, date.AddDays(-6), date.AddDays(-2))); // بازه قبل از نوبت
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new NotifyWaitlistForFreedSlotCommand(901, 5, date, new TimeOnly(9, 0)),
            CancellationToken.None)).Value;

        result.Should().Be(0); // هیچ‌کدام واجد شرایط نیست
    }

    /// <summary>پزشک متفاعت یا «هر پزشک» — منتظر پزشک دیگر پیشنهاد نمی‌گیرد</summary>
    [Fact]
    public async Task Offer_DoctorFilter_Honored()
    {
        var (handler, ctx) = MakeNotifier();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        ctx.WaitlistEntries.AddRange(
            Entry(1, 10, 6, date, date),   // پزشک ۶ — نوبت پزشک ۵ آزاد شده
            Entry(2, 11, null, date, date)); // هر پزشک — واجد شرایط
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new NotifyWaitlistForFreedSlotCommand(902, 5, date, new TimeOnly(16, 0)),
            CancellationToken.None)).Value;

        result.Should().Be(2); // «هر پزشک» برنده می‌شود
    }

    /// <summary>فقط وضعیت Waiting — ردیف Notified/Cancelled دوباره پیشنهاد نمی‌گیرد</summary>
    [Fact]
    public async Task Offer_OnlyWaitingEntries()
    {
        var (handler, ctx) = MakeNotifier();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var notified = Entry(1, 10, 5, date, date);
        notified.Status = WaitlistStatus.Notified;
        var cancelled = Entry(2, 11, 5, date, date);
        cancelled.Status = WaitlistStatus.Cancelled;
        ctx.WaitlistEntries.AddRange(notified, cancelled);
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new NotifyWaitlistForFreedSlotCommand(903, 5, date, new TimeOnly(8, 0)),
            CancellationToken.None)).Value;

        result.Should().Be(0);
    }

    [Fact]
    public async Task Offer_skips_missing_mobile_and_queues_instead_of_sending_before_commit()
    {
        var sms = new Mock<Sahand.ClinicPro.Application.Interfaces.ISmsService>(MockBehavior.Strict);
        sms.Setup(s => s.EnqueueRawAsync("09140000000", It.IsAny<string>(), null, 3, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var (handler, ctx) = MakeNotifier(sms);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var noMobile = Entry(1, 10, 5, date, date, Priority.High);
        noMobile.ContactMobile = " ";
        ctx.WaitlistEntries.AddRange(noMobile, Entry(2, 11, 5, date, date));
        await ctx.SaveChangesAsync();

        var result = await handler.Handle(new NotifyWaitlistForFreedSlotCommand(900, 5, date, new TimeOnly(10, 30)), default);
        result.Value.Should().Be(2);
        noMobile.Status.Should().Be(WaitlistStatus.Waiting);
        sms.Verify(s => s.EnqueueRawAsync("09140000000", It.IsAny<string>(), null, 3, It.IsAny<CancellationToken>()), Times.Once);
        sms.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public async Task Offer_matches_selected_specialty_or_subspecialty(long selectedSpecialty)
    {
        var (handler, ctx) = MakeNotifier();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        ctx.Doctors.Add(new Doctor { Id = 5, SpecialtyId = 10, SubSpecialtyId = 20 });
        var wrongSpecialty = Entry(1, 10, null, date, date, Priority.Emergency);
        wrongSpecialty.SpecialtyId = 99;
        var matchingSpecialty = Entry(2, 11, null, date, date, Priority.Normal);
        matchingSpecialty.SpecialtyId = selectedSpecialty;
        ctx.WaitlistEntries.AddRange(wrongSpecialty, matchingSpecialty);
        await ctx.SaveChangesAsync();

        var result = await handler.Handle(new NotifyWaitlistForFreedSlotCommand(900, 5, date, new TimeOnly(10, 30)), default);
        result.Value.Should().Be(2);
        wrongSpecialty.Status.Should().Be(WaitlistStatus.Waiting);
    }

    [Fact]
    public async Task Offer_skips_invalid_numbers_even_beyond_ten_higher_priority_candidates()
    {
        var (handler, ctx) = MakeNotifier();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        for (var i = 1; i <= 12; i++)
        {
            var invalid = Entry(i, i, 5, date, date, Priority.Emergency);
            invalid.ContactMobile = "09xxxxxxxxx";
            ctx.WaitlistEntries.Add(invalid);
        }
        ctx.WaitlistEntries.Add(Entry(13, 13, 5, date, date));
        await ctx.SaveChangesAsync();

        var result = await handler.Handle(new NotifyWaitlistForFreedSlotCommand(900, 5, date, new TimeOnly(10, 30)), default);
        result.Value.Should().Be(13);
        (await ctx.WaitlistEntries.CountAsync(w => w.Status == WaitlistStatus.Notified)).Should().Be(1);
    }

    [Fact]
    public async Task Offer_does_not_hide_failure_to_enqueue_message()
    {
        var sms = new Mock<Sahand.ClinicPro.Application.Interfaces.ISmsService>(MockBehavior.Strict);
        sms.Setup(s => s.EnqueueRawAsync(It.IsAny<string>(), It.IsAny<string>(), null, 3, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("queue unavailable"));
        var (handler, ctx) = MakeNotifier(sms);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        ctx.WaitlistEntries.Add(Entry(1, 10, 5, date, date));
        await ctx.SaveChangesAsync();
        var action = () => handler.Handle(new NotifyWaitlistForFreedSlotCommand(900, 5, date, new TimeOnly(10, 30)), default);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>ردیف‌های هم‌اولویت — قدیمی‌ترین اول</summary>
    [Fact]
    public async Task Offer_SamePriority_OldestFirst()
    {
        var (handler, ctx) = MakeNotifier();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4));
        var older = Entry(1, 10, 5, date, date);
        older.CreatedAt = DateTime.UtcNow.AddHours(-5);
        var newer = Entry(2, 11, 5, date, date);
        newer.CreatedAt = DateTime.UtcNow;
        ctx.WaitlistEntries.AddRange(older, newer);
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new NotifyWaitlistForFreedSlotCommand(904, 5, date, new TimeOnly(11, 0)),
            CancellationToken.None)).Value;

        result.Should().Be(1); // قدیمی‌تر
    }
}

/// <summary>
/// تست‌های رگرسیون رابطه‌ای چرخهٔ ۷۸ — ریشه‌یابی NullReferenceException واقعی مالک:
/// «ثبت در لیست انتظار می‌گوید ثبت شد ولی لیست نشان نمی‌دهد و خطای
/// NullReferenceException می‌دهد» (لاگ سرور: GetWaitlistQueryHandler خط ۱۸۷).
/// ریشه: dereference سمت کلاینتِ ناوبری Employee در سلکتور عنصر ToDictionaryAsync
/// روی موجودیت‌های متریالزهٔ provider رابطه‌ای (بدون Include و بدون lazy-loading).
/// بازتولید این سناریو فقط روی provider رابطه‌ای ممکن است — InMemory ارجاع‌های
/// ناوبری ذخیره‌شده را عیناً برمی‌گرداند و باگ را می‌پوشاند؛ SQLite درون‌حافظه‌ای
/// مثل SQL Server رفتار متریال‌سازی بدون ناوبری دارد (الگوی اثبات‌شدهٔ چرخهٔ ۴۸/۶۸).
/// </summary>
public class WaitlistRelationalRegressionTests
{
    /// <summary>ساخت واحد کار روی SQLite درون‌حافظه‌ای با اتصال باز — الگوی چرخهٔ ۴۸</summary>
    private static (UnitOfWork UoW, ClinicProDbContext Context, SqliteConnection Connection) CreateSqliteUnitOfWork()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open(); // اتصال تا پایان تست باز می‌ماند وگرنه دیتابیس نابود می‌شود
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new SqliteWaitlistDbContext(options);
        context.Database.EnsureCreated();
        var mediator = new Mock<MediatR.IMediator>().Object;
        return (new UnitOfWork(context, mediator), context, connection);
    }

    /// <summary>
    /// کانتکست ClinicPro روی SQLite — همان اصلاحات اثبات‌شدهٔ چرخهٔ ۴۸/۶۸:
    /// حذف سکانس‌ها، پیش‌فرض‌های T-SQL سکانس‌دار، nvarchar(max) و تبدیل decimal.
    /// </summary>
    private sealed class SqliteWaitlistDbContext(DbContextOptions<ClinicProDbContext> options)
        : ClinicProDbContext(options)
    {
        /// <summary>اعمال مدل کامل + حذف سازه‌های ناسازگار با SQLite</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var seq in modelBuilder.Model.GetSequences().ToList())
                modelBuilder.Model.RemoveSequence(seq.Name, seq.Schema);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                foreach (var property in entityType.GetProperties())
                {
                    if (!string.IsNullOrEmpty(property.GetDefaultValueSql()))
                        property.SetDefaultValueSql(null);

                    var columnType = property.GetColumnType();
                    if (!string.IsNullOrEmpty(columnType) &&
                        columnType.Contains("(max)", StringComparison.OrdinalIgnoreCase))
                        property.SetColumnType(null);

                    if (property.ClrType == typeof(decimal))
                    {
                        property.SetColumnType("REAL");
                        modelBuilder.Entity(entityType.ClrType).Property(property.Name)
                            .HasConversion<double>();
                    }
                    else if (property.ClrType == typeof(decimal?))
                    {
                        property.SetColumnType("REAL");
                        modelBuilder.Entity(entityType.ClrType).Property(property.Name)
                            .HasConversion<double?>(); // تهی در SQL همان NULL می‌ماند
                    }
                }
        }
    }

    /// <summary>
    /// فهرست لیست انتظار با ردیفِ «همراه با پزشک» نباید NRE بدهد —
    /// پیش از رفع چرخهٔ ۷۸ همین فراخوانی روی provider رابطه‌ای
    /// NullReferenceException می‌داد و اندپوینت ۵۰۰ برمی‌گشت؛
    /// در نتیجه پنل پس از هر ثبت موفق خالی می‌ماند.
    /// </summary>
    [Fact]
    public async Task GetWaitlist_WithDoctorEntry_ShouldNotThrowAndShowDoctorName()
    {
        var (uow, ctx, connection) = CreateSqliteUnitOfWork();
        try
        {
            // بذر: کارمند + پزشکِ او + بیمار + ردیف لیست انتظار با DoctorId پر
            // (فیلدهای متنی اجباری Patient هم مقدار می‌گیرند — ستون‌های NOT NULL در SQLite)
            var employee = new Employee { Id = 501, FirstName = "سهند", LastName = "احمدی" };
            var doctor = new Doctor { Id = 502, EmployeeId = 501, IsActive = true };
            var patient = new Patient
            {
                Id = 503,
                MedicalRecordNo = "MR-0778",
                NationalCode = "1234567890",
                FirstName = "زهرا",
                LastName = "کریمی",
                Mobile = "09140000001"
            };
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            ctx.Employees.Add(employee);
            ctx.Doctors.Add(doctor);
            ctx.Patients.Add(patient);
            ctx.WaitlistEntries.Add(new WaitlistEntry
            {
                Id = 504,
                PatientId = 503,
                DoctorId = 502,
                PreferredFrom = today.AddDays(-1),
                PreferredTo = today.AddDays(5),
                Priority = Priority.Normal,
                Status = WaitlistStatus.Waiting,
                ContactMobile = "09140000001"
            });
            await ctx.SaveChangesAsync();

            var handler = new GetWaitlistQueryHandler(uow);
            var act = () => handler.Handle(new GetWaitlistQuery(), CancellationToken.None);

            // با کد قدیمی همین‌جا NRE پرواز می‌کرد؛ اکنون باید موفق برگردد
            var result = await act();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            // نام پزشکِ سمت سرور از Employee خوانده شد — نه «هر پزشک»
            result.Value[0].DoctorName.Should().Be("دکتر سهند احمدی");
            result.Value[0].PatientName.Should().Be("زهرا کریمی");
        }
        finally
        {
            // بستن اتصال درون‌حافظه‌ای — آزادسازی منبع
            await ctx.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
