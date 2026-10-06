using FluentAssertions;
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
    private static (NotifyWaitlistForFreedSlotCommandHandler Handler, ClinicProDbContext Ctx) MakeNotifier()
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
        uow.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async _ => await ctx.SaveChangesAsync(_) > 0);

        // سرویس پیامک بی‌اثر — تست منطق پیشنهاد است نه ارسال
        var sms = new Mock<Sahand.ClinicPro.Application.Interfaces.ISmsService>();
        sms.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sahand.ClinicPro.Application.Interfaces.SmsSendResult.Ok("test", "1"));

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
