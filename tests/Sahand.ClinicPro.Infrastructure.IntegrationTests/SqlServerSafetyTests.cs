using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sahand.ClinicPro.Application.Common.Behaviors;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Application.Features.Billing.Commands.CreateInvoice;
using Sahand.ClinicPro.Application.Features.Billing.Commands.RecordPayment;
using Sahand.ClinicPro.Application.Features.Appointments.Waitlist;
using Sahand.ClinicPro.Application.Features.Appointments.Commands.ChangeAppointmentStatus;
using Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.Infrastructure.Persistence.Migrations;
using Sahand.ClinicPro.Infrastructure.Persistence.Seeders;
using Sahand.ClinicPro.Infrastructure.Services.SMS;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>اجرای اختیاری فقط روی سرور آزمایشی؛ هر تست پایگاه‌داده یکتای خود را می‌سازد و حذف می‌کند.</summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLINICPRO_TEST_SQLSERVER")))
            Skip = "برای این تست به SQL Server آزمایشی و CLINICPRO_TEST_SQLSERVER نیاز است.";
    }
}

[CollectionDefinition("SQL Server safety", DisableParallelization = true)]
public sealed class SqlServerSafetyCollection { }

[Collection("SQL Server safety")]
[Trait("Category", "SqlServer")]
public sealed class SqlServerSafetyTests
{
    [SqlServerFact]
    public async Task Fresh_database_applies_every_migration()
    {
        await WithTemporaryDatabaseAsync(async (db, _) =>
        {
            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Contains("20261103120000_ReceptionRequestId", await db.Database.GetAppliedMigrationsAsync());
        });
    }

    [SqlServerFact]
    public async Task Upgrade_preserves_data_and_replays_financial_requests_on_real_sql_server()
    {
        await WithTemporaryDatabaseAsync(async (db, connectionString) =>
        {
            // ارتقا از نسخه‌ای پیش از سه مهاجرت RequestId، نه فقط نصب تمیز.
            await db.GetService<IMigrator>().MigrateAsync("20261027120000_UnidentifiedPatient");
            db.SystemSettings.Add(new SystemSetting { Key = "sql-upgrade-sentinel", Value = "preserved" });
            await db.SaveChangesAsync();
            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Equal("preserved", (await db.SystemSettings.SingleAsync(x => x.Key == "sql-upgrade-sentinel")).Value);

            await new AccountingAccountSeeder(db).SeedAsync();
            var patient = new Patient { FirstName = "Test", LastName = "Patient", Mobile = "09123456789" };
            var service = new Service { ServiceCode = "SQL-TEST", ServiceName = "Test service", DefaultPrice = 100m };
            db.AddRange(patient, service);
            await db.SaveChangesAsync();

            var receptionDto = new CreateReceptionDto(patient.Id, null, null, null, null, null,
                ServiceId: service.Id, RequestId: Guid.NewGuid());
            var reception = await CreateReceptionAsync(connectionString, receptionDto);
            Assert.True(reception.IsSuccess, reception.Error?.Message);
            Assert.Equal(reception.Value, (await CreateReceptionAsync(connectionString, receptionDto)).Value);

            var invoiceDto = new CreateInvoiceDto(patient.Id, reception.Value, null, InvoiceType.Other, null, 0,
                new List<InvoiceItemDto> { new(null, "Test", 1, 100m, 0, null, 0) }, RequestId: Guid.NewGuid());
            var income = await db.AccountingAccounts.SingleAsync(a => a.AccountCode == "4101");
            income.IsActive = false;
            await db.SaveChangesAsync();
            var blockedInvoice = await CreateInvoiceAsync(connectionString, invoiceDto);
            Assert.Equal("BILLING.ACCOUNTS_UNAVAILABLE", blockedInvoice.Error.Code);
            Assert.False(await db.Invoices.AnyAsync(i => i.RequestId == invoiceDto.RequestId));
            income.IsActive = true;
            await db.SaveChangesAsync();
            var invoice = await CreateInvoiceAsync(connectionString, invoiceDto);
            Assert.True(invoice.IsSuccess, invoice.Error?.Message);
            Assert.Equal(invoice.Value, (await CreateInvoiceAsync(connectionString, invoiceDto)).Value);

            var paymentDto = new RecordPaymentDto(invoice.Value, patient.Id, 100m, PaymentMethod.Cash, RequestId: Guid.NewGuid());
            var cash = await db.AccountingAccounts.SingleAsync(a => a.AccountCode == "1101");
            cash.IsDetail = false;
            await db.SaveChangesAsync();
            var blockedPayment = await RecordPaymentAsync(connectionString, paymentDto);
            Assert.Equal("PAYMENT.ACCOUNTS_UNAVAILABLE", blockedPayment.Error.Code);
            Assert.False(await db.Payments.AnyAsync(p => p.RequestId == paymentDto.RequestId));
            cash.IsDetail = true;
            await db.SaveChangesAsync();
            var payment = await RecordPaymentAsync(connectionString, paymentDto);
            Assert.True(payment.IsSuccess, payment.Error?.Message);
            // پاسخ اول را گم‌شده فرض می‌کنیم: بازپخش در کانتکست جدید همان نتیجه است.
            var replay = await RecordPaymentAsync(connectionString, paymentDto);
            Assert.True(replay.IsSuccess, replay.Error?.Message);
            Assert.Equal(payment.Value, replay.Value);
            var changed = await RecordPaymentAsync(connectionString, paymentDto with { Amount = 99m });
            Assert.True(changed.IsFailure);
            Assert.Equal("PAYMENT.REQUEST_CONFLICT", changed.Error?.Code);

            await using var verify = CreateContext(connectionString);
            Assert.Equal(1, await verify.Receptions.CountAsync(r => r.RequestId == receptionDto.RequestId));
            Assert.Equal(1, await verify.Invoices.CountAsync(i => i.RequestId == invoiceDto.RequestId));
            Assert.Equal(1, await verify.Payments.CountAsync(p => p.RequestId == paymentDto.RequestId));
            var stored = await verify.Invoices.SingleAsync(i => i.Id == invoice.Value);
            Assert.Equal(100m, stored.PaidAmount);
            Assert.Equal(0m, stored.RemainingAmount);
            Assert.Equal(1, await verify.JournalEntries.CountAsync(j => j.ReferenceType == JournalReferenceType.Payment && j.ReferenceId == payment.Value));

            await VerifyWaitlistQueueTransactionAsync(connectionString, patient.Id);

            // حتی پس از حذف نرم، همان کلید نباید برای رکورد جدید قابل استفاده شود.
            await AssertUniqueIndexesIncludeDeletedRowsAsync(connectionString, patient.Id, service.Id,
                receptionDto.RequestId!.Value, invoiceDto.RequestId!.Value, paymentDto.RequestId!.Value);
        });
    }

    [SqlServerFact]
    public async Task Concurrent_same_payment_key_commits_once_and_can_be_replayed()
    {
        await WithTemporaryDatabaseAsync(async (db, connectionString) =>
        {
            await db.Database.MigrateAsync();
            await new AccountingAccountSeeder(db).SeedAsync();
            var patient = new Patient { FirstName = "Test", LastName = "Concurrency", Mobile = "09123456789" };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            var invoice = new Invoice { PatientId = patient.Id, InvoiceDate = DateTime.UtcNow,
                TotalAmount = 100m, RemainingAmount = 100m, Status = InvoiceStatus.Issued };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            var dto = new RecordPaymentDto(invoice.Id, patient.Id, 100m, PaymentMethod.Cash, RequestId: Guid.NewGuid());
            var gate = new SaveGate();
            var attempts = await Task.WhenAll(AttemptPaymentAsync(connectionString, dto, gate),
                AttemptPaymentAsync(connectionString, dto, gate));
            Assert.Single(attempts.Where(a => a.Result?.IsSuccess == true));
            var failure = Assert.Single(attempts.Where(a => a.Exception is not null));
            Assert.IsAssignableFrom<DbUpdateException>(failure.Exception);

            var replay = await RecordPaymentAsync(connectionString, dto);
            Assert.True(replay.IsSuccess, replay.Error?.Message);
            await using var verify = CreateContext(connectionString);
            Assert.Single(await verify.Payments.Where(p => p.RequestId == dto.RequestId).ToListAsync());
            var stored = await verify.Invoices.SingleAsync(i => i.Id == invoice.Id);
            Assert.Equal(100m, stored.PaidAmount);
            Assert.Equal(0m, stored.RemainingAmount);
        });
    }

    private static async Task VerifyWaitlistQueueTransactionAsync(string connectionString, long patientId)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);
        long appointmentId, doctorId, waitlistId, restrictedId;
        await using (var seed = CreateContext(connectionString))
        {
            var doctor = new Doctor { IsActive = true };
            seed.Doctors.Add(doctor);
            await seed.SaveChangesAsync();
            var appointment = new Appointment { PatientId = patientId, DoctorId = doctor.Id,
                AppointmentDate = date, AppointmentTime = new TimeOnly(10, 30), Status = AppointmentStatus.Booked };
            var entry = new WaitlistEntry { PatientId = patientId, DoctorId = doctor.Id, PreferredFrom = date,
                PreferredTo = date, ContactMobile = "09123456789", Status = WaitlistStatus.Waiting };
            var restricted = new WaitlistEntry { PatientId = patientId, DoctorId = doctor.Id, PreferredFrom = date,
                PreferredTo = date, ContactMobile = "09123456789", Status = WaitlistStatus.Waiting,
                PreferredTimeOfDay = "صبح", Priority = Priority.Emergency };
            seed.AddRange(appointment, entry, restricted);
            await seed.SaveChangesAsync();
            (appointmentId, doctorId, waitlistId, restrictedId) = (appointment.Id, doctor.Id, entry.Id, restricted.Id);
        }

        async Task RunOfferAsync(bool commit)
        {
            await using var context = CreateContext(connectionString);
            var manager = new TransactionManager(context, NullLogger<TransactionManager>.Instance);
            var uow = new UnitOfWork(context, Mock.Of<IMediator>());
            var sms = new SmsService(new SmsProviderFactory(Array.Empty<ISmsProvider>()), context, NullLogger<SmsService>.Instance);
            var clock = Mock.Of<IDateTimeService>(x => x.NowIran == date.AddDays(-1).ToDateTime(new TimeOnly(12, 0)));
            var handler = new NotifyWaitlistForFreedSlotCommandHandler(uow, sms, clock);
            var command = new NotifyWaitlistForFreedSlotCommand(appointmentId, doctorId, date, new TimeOnly(10, 30));
            var inner = new TransactionBehavior<NotifyWaitlistForFreedSlotCommand, Result<long>>(manager,
                NullLogger<TransactionBehavior<NotifyWaitlistForFreedSlotCommand, Result<long>>>.Instance);
            await manager.BeginAsync();
            (await context.Appointments.SingleAsync(a => a.Id == appointmentId)).Status = AppointmentStatus.Cancelled;
            await uow.SaveEntitiesAsync();
            var result = await inner.Handle(command, _ => handler.Handle(command, CancellationToken.None), CancellationToken.None);
            Assert.Equal(waitlistId, result.Value);
            Assert.Equal(1, await context.SmsQueue.CountAsync());
            if (commit) await manager.CommitAsync();
            else await manager.RollbackAsync();
        }

        await RunOfferAsync(commit: false);
        await using (var check = CreateContext(connectionString))
        {
            Assert.Equal(AppointmentStatus.Booked, (await check.Appointments.SingleAsync(a => a.Id == appointmentId)).Status);
            Assert.Equal(WaitlistStatus.Waiting, (await check.WaitlistEntries.SingleAsync(w => w.Id == waitlistId)).Status);
            Assert.Equal(0, await check.SmsQueue.CountAsync());
        }
        await RunOfferAsync(commit: true);
        await using (var check = CreateContext(connectionString))
        {
            Assert.Equal(AppointmentStatus.Cancelled, (await check.Appointments.SingleAsync(a => a.Id == appointmentId)).Status);
            Assert.Equal(WaitlistStatus.Notified, (await check.WaitlistEntries.SingleAsync(w => w.Id == waitlistId)).Status);
            Assert.Equal(WaitlistStatus.Waiting, (await check.WaitlistEntries.SingleAsync(w => w.Id == restrictedId)).Status);
            Assert.Equal(1, await check.SmsQueue.CountAsync());
        }

        // بازپخش لغو ثبت‌شده در کانتکست مستقل نباید پیامک دیگری بسازد.
        await using (var replay = CreateContext(connectionString))
        {
            var notifier = new Mock<IWaitlistNotifier>(MockBehavior.Strict);
            var clock = Mock.Of<IDateTimeService>(x => x.NowIran == date.AddDays(-1).ToDateTime(new TimeOnly(12, 0)));
            var manager = new TransactionManager(replay, NullLogger<TransactionManager>.Instance);
            var uow = new UnitOfWork(replay, Mock.Of<IMediator>());
            var handler = new ChangeAppointmentStatusCommandHandler(uow, Mock.Of<ICurrentUserService>(),
                NullLogger<ChangeAppointmentStatusCommandHandler>.Instance, notifier.Object, clock);
            var behavior = new TransactionBehavior<ChangeAppointmentStatusCommand, Result>(manager,
                NullLogger<TransactionBehavior<ChangeAppointmentStatusCommand, Result>>.Instance);
            var command = new ChangeAppointmentStatusCommand(appointmentId, AppointmentStatus.Cancelled, "تکرار");
            var result = await behavior.Handle(command, _ => handler.Handle(command, CancellationToken.None), CancellationToken.None);
            Assert.True(result.IsSuccess);
            notifier.VerifyNoOtherCalls();
            Assert.Equal(1, await replay.SmsQueue.CountAsync());
        }
    }

    private static async Task AssertUniqueIndexesIncludeDeletedRowsAsync(string connectionString, long patientId, long serviceId,
        Guid receptionKey, Guid invoiceKey, Guid paymentKey)
    {
        await using (var deleted = CreateContext(connectionString))
        {
            (await deleted.Receptions.SingleAsync(r => r.RequestId == receptionKey)).IsDeleted = true;
            (await deleted.Invoices.SingleAsync(i => i.RequestId == invoiceKey)).IsDeleted = true;
            (await deleted.Payments.SingleAsync(p => p.RequestId == paymentKey)).IsDeleted = true;
            await deleted.SaveChangesAsync();
        }
        await using (var duplicate = CreateContext(connectionString))
        {
            duplicate.Receptions.Add(new Reception { PatientId = patientId, ServiceId = serviceId,
                ReceptionDate = DateTime.UtcNow, RequestId = receptionKey });
            AssertSqlIndex(await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync()), "IX_Reception_RequestId");
        }
        await using (var duplicate = CreateContext(connectionString))
        {
            duplicate.Invoices.Add(new Invoice { PatientId = patientId, InvoiceDate = DateTime.UtcNow, RequestId = invoiceKey });
            AssertSqlIndex(await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync()), "IX_Invoice_RequestId");
        }
        await using (var duplicate = CreateContext(connectionString))
        {
            duplicate.Payments.Add(new Payment { PatientId = patientId, PaymentDate = DateTime.UtcNow,
                Amount = 1m, Method = PaymentMethod.Cash, RequestId = paymentKey });
            AssertSqlIndex(await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync()), "IX_Payment_RequestId");
        }
    }

    private static void AssertSqlIndex(DbUpdateException exception, string index)
    {
        var sql = Assert.IsType<SqlException>(exception.InnerException);
        Assert.True(sql.Errors.Cast<SqlError>().Any(e => (e.Number is 2601 or 2627) &&
            e.Message.Contains($"'{index}'", StringComparison.OrdinalIgnoreCase)), sql.Message);
    }

    private static async Task<(Result<long>? Result, Exception? Exception)> AttemptPaymentAsync(
        string connectionString, RecordPaymentDto dto, SaveGate gate)
    {
        try { return (await RecordPaymentAsync(connectionString, dto, gate), null); }
        catch (Exception ex) { return (null, ex); }
    }

    private static async Task<Result<long>> CreateReceptionAsync(string connectionString, CreateReceptionDto dto)
    {
        await using var db = CreateContext(connectionString);
        var uow = new UnitOfWork(db, Mock.Of<IMediator>());
        var handler = new CreateReceptionCommandHandler(uow, NullLogger<CreateReceptionCommandHandler>.Instance, Mock.Of<IRealtimeNotifier>());
        var command = new CreateReceptionCommand(dto);
        var behavior = new TransactionBehavior<CreateReceptionCommand, Result<long>>(
            new TransactionManager(db, NullLogger<TransactionManager>.Instance), NullLogger<TransactionBehavior<CreateReceptionCommand, Result<long>>>.Instance);
        return await behavior.Handle(command, _ => handler.Handle(command, CancellationToken.None), CancellationToken.None);
    }

    private static async Task<Result<long>> CreateInvoiceAsync(string connectionString, CreateInvoiceDto dto)
    {
        await using var db = CreateContext(connectionString);
        var uow = new UnitOfWork(db, Mock.Of<IMediator>());
        var handler = new CreateInvoiceCommandHandler(uow, NullLogger<CreateInvoiceCommandHandler>.Instance);
        var command = new CreateInvoiceCommand(dto);
        var behavior = new TransactionBehavior<CreateInvoiceCommand, Result<long>>(
            new TransactionManager(db, NullLogger<TransactionManager>.Instance), NullLogger<TransactionBehavior<CreateInvoiceCommand, Result<long>>>.Instance);
        return await behavior.Handle(command, _ => handler.Handle(command, CancellationToken.None), CancellationToken.None);
    }

    private static async Task<Result<long>> RecordPaymentAsync(string connectionString, RecordPaymentDto dto, SaveGate? gate = null)
    {
        await using var db = CreateContext(connectionString, gate);
        var uow = new UnitOfWork(db, Mock.Of<IMediator>());
        var handler = new RecordPaymentCommandHandler(uow, Mock.Of<ICurrentUserService>(), NullLogger<RecordPaymentCommandHandler>.Instance);
        var command = new RecordPaymentCommand(dto);
        var behavior = new TransactionBehavior<RecordPaymentCommand, Result<long>>(
            new TransactionManager(db, NullLogger<TransactionManager>.Instance), NullLogger<TransactionBehavior<RecordPaymentCommand, Result<long>>>.Instance);
        return await behavior.Handle(command, _ => handler.Handle(command, CancellationToken.None), CancellationToken.None);
    }

    private static ClinicProDbContext CreateContext(string connectionString, SaveGate? gate = null)
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseSqlServer(connectionString, sql => sql.CommandTimeout(180))
            .ReplaceService<IMigrationsSqlGenerator, ClinicProMigrationsSqlGenerator>();
        if (gate is not null) options.AddInterceptors(gate);
        return new ClinicProDbContext(options.Options);
    }

    private static async Task WithTemporaryDatabaseAsync(Func<ClinicProDbContext, string, Task> test)
    {
        var master = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CLINICPRO_TEST_SQLSERVER")) { InitialCatalog = "master" };
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var connection = new SqlConnection(master.ConnectionString);
                await connection.OpenAsync();
                break;
            }
            catch (SqlException) when (attempt < 59) { await Task.Delay(TimeSpan.FromSeconds(2)); }
        }
        var temporary = new SqlConnectionStringBuilder(master.ConnectionString)
        {
            InitialCatalog = "ClinicPro_CI_" + Guid.NewGuid().ToString("N")
        };
        await using var db = CreateContext(temporary.ConnectionString);
        try { await test(db, temporary.ConnectionString); }
        finally
        {
            SqlConnection.ClearAllPools();
            await db.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>هر دو فرمان پس از خواندن مانده و قبل از اولین ذخیره به هم می‌رسند.</summary>
    private sealed class SaveGate : SaveChangesInterceptor
    {
        private int _arrivals;
        private readonly TaskCompletionSource _bothReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _arrivals) == 2) _bothReady.TrySetResult();
            await _bothReady.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            return result;
        }
    }
}
