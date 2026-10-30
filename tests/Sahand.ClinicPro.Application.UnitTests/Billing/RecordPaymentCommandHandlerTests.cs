using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.RecordPayment;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Accounting;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Domain.Interfaces;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests.Billing;

/// <summary>
/// تست‌های هندلر ثبت پرداخت — چرخهٔ ۵۶ (ضلع گمشدهٔ مالی):
/// RecordPayment پرریسک‌ترین عملیات مالی روزانهٔ درمانگاه است (صندوق)؛
/// این‌جا پنج گارد کسب‌وکار (وجود بیمار/فاکتور، ممنوعیت پرداخت اضافه،
/// تشخیص PartiallyPaid/Paid)، رویداد InvoicePaid، سند حسابداری متوازن،
/// و رد امن «نبود کدینگ» پوشش داده می‌شود — با Mock، بدون دیتابیس.
/// </summary>
public class RecordPaymentCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly List<Payment> _addedPayments = new();
    private readonly List<JournalEntry> _addedJournals = new();
    private readonly List<AccountingAccount> _accounts = new();
    private readonly Invoice _invoice;
    private const long PatientId = 7;

    /// <summary>هندلر آماده با فاکتور ۱٬۰۰۰٬۰۰۰ ریال که ۴۰۰٬۰۰۰ آن پرداخت شده</summary>
    public RecordPaymentCommandHandlerTests()
    {
        _accounts.Add(new AccountingAccount { Id = 101, AccountCode = "1101", AccountName = "صندوق", IsDetail = true });
        _accounts.Add(new AccountingAccount { Id = 102, AccountCode = "1103", AccountName = "مطالبات بیماران", IsDetail = true });
        _invoice = new Invoice
        {
            Id = 55,
            InvoiceNumber = "INV-55",
            PatientId = PatientId,
            TotalAmount = 1_000_000m,
            PaidAmount = 400_000m,
            RemainingAmount = 600_000m,
            PaymentStatus = PaymentStatus.PartiallyPaid,
        };

        var patients = new Mock<IRepository<Patient>>();
        var patient = new Patient { Id = PatientId, FirstName = "سارا", LastName = "محمدی" };
        // هندلر برای جلوگیری از عبور از فیلتر شعبه ابتدا AnyAsync می‌زند؛
        // ماک باید IQueryable ناهمگام، نه فقط GetByIdAsync، فراهم کند.
        patients.Setup(r => r.QueryableNoTracking).Returns(new AsyncEnumerable<Patient>(new[] { patient }));
        patients.Setup(r => r.GetByIdAsync(PatientId, It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        var invoices = new Mock<IRepository<Invoice>>();
        invoices.Setup(r => r.QueryableNoTracking).Returns(new AsyncEnumerable<Invoice>(new[] { _invoice }));
        invoices.Setup(r => r.GetByIdAsync(55L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_invoice);
        invoices.Setup(r => r.GetByIdAsync(It.Is<long>(id => id != 55), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice?)null);

        var payments = new Mock<IRepository<Payment>>();
        payments.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => _addedPayments.Add(p))
            .ReturnsAsync((Payment p, CancellationToken _) => p);

        var journals = new Mock<IRepository<JournalEntry>>();
        journals.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((j, _) => _addedJournals.Add(j))
            .ReturnsAsync((JournalEntry j, CancellationToken _) => j);

        var accounts = new Mock<IRepository<AccountingAccount>>();
        accounts.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<AccountingAccount, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<AccountingAccount, bool>> pred, CancellationToken _) =>
                _accounts.FirstOrDefault(pred.Compile()));

        _uow.Setup(u => u.Repository<Patient>()).Returns(patients.Object);
        _uow.Setup(u => u.Repository<Invoice>()).Returns(invoices.Object);
        _uow.Setup(u => u.Repository<Payment>()).Returns(payments.Object);
        _uow.Setup(u => u.Repository<JournalEntry>()).Returns(journals.Object);
        _uow.Setup(u => u.Repository<AccountingAccount>()).Returns(accounts.Object);
        _uow.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private RecordPaymentCommandHandler MakeHandler() =>
        new(_uow.Object,
            new CurrentUserStub(42),
            NullLogger<RecordPaymentCommandHandler>.Instance);

    private static RecordPaymentCommand Pay(decimal amount, long patientId = PatientId, long invoiceId = 55) =>
        new(new RecordPaymentDto(InvoiceId: invoiceId, PatientId: patientId,
            Amount: amount, Method: PaymentMethod.Card));

    /// <summary>کاربر جاری آزمایشی</summary>
    private sealed class CurrentUserStub(long userId) : ICurrentUserService
    {
        public long? UserId { get; } = userId;
        public string? Username { get; } = "cashier-test";
        public bool IsAuthenticated => true;
        public string? IpAddress { get; } = "127.0.0.1";
        public long? BranchId { get; }
    }

    // ═══════════════ گاردهای کسب‌وکار ═══════════════

    [Fact]
    public async Task Unknown_Patient_Should_Fail_Without_SideEffects()
    {
        var result = await MakeHandler().Handle(Pay(100m, patientId: 999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        // Error.NotFound با نام فارسی موجودیت کد می‌سازد: «بیمار_NOT_FOUND»
        result.Error.Code.Should().Be("بیمار_NOT_FOUND");
        _addedPayments.Should().BeEmpty(because: "هیچ پرداختی ثبت نشده است");
        _uow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unknown_Invoice_Should_Fail_With_InvoiceNotFound()
    {
        var result = await MakeHandler().Handle(Pay(100m, invoiceId: 404), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("فاکتور_NOT_FOUND");
    }

    [Fact]
    public async Task Overpayment_Should_Be_Rejected_With_Persian_Remaining()
    {
        // مانده ۶۰۰٬۰۰۰ — پرداخت ۶۰۰٬۰۰۱ باید رد شود
        var result = await MakeHandler().Handle(Pay(600_001m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OVERPAYMENT");
        // بخش متنی پیام را می‌سنجم نه قالب عددی را — قالب N0 وابسته به Culture است
        // (روی ویندوز فارسی «۶۰۰٬۰۰۰» می‌آید) و نباید شکننده باشد
        result.Error.Message.Should().Contain("بیش از مانده فاکتور");
        _addedPayments.Should().BeEmpty(because: "پرداخت اضافه هرگز ثبت نمی‌شود");
    }

    [Fact]
    public async Task Partial_Payment_Should_Update_Invoice_Without_PaidEvent()
    {
        var result = await MakeHandler().Handle(Pay(200_000m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _invoice.PaidAmount.Should().Be(600_000m);
        _invoice.RemainingAmount.Should().Be(400_000m);
        _invoice.PaymentStatus.Should().Be(PaymentStatus.PartiallyPaid);
        _invoice.DomainEvents.Should().NotContain(e => e is InvoicePaidEvent,
            because: "رویداد تسویهٔ کامل فقط با صفر شدن مانده می‌آید");
    }

    [Fact]
    public async Task Full_Payment_Should_Mark_Paid_And_Raise_InvoicePaidEvent()
    {
        var result = await MakeHandler().Handle(Pay(600_000m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _invoice.PaymentStatus.Should().Be(PaymentStatus.Paid);
        _invoice.RemainingAmount.Should().Be(0m);
        var evt = _invoice.DomainEvents.OfType<InvoicePaidEvent>().Should().HaveCount(1).And.Subject.First();
        evt.InvoiceId.Should().Be(55L);
        evt.PatientId.Should().Be(PatientId);
        evt.PaidAmount.Should().Be(1_000_000m, because: "مبلغ کلِ پرداخت‌شده در رویداد می‌آید");
    }

    // ═══════════════ سند حسابداری ═══════════════

    [Fact]
    public async Task Receipt_Journal_Should_Be_Balanced_With_Cash_Debit()
    {
        await MakeHandler().Handle(Pay(600_000m), CancellationToken.None);

        _addedJournals.Should().HaveCount(1);
        var entry = _addedJournals[0];
        entry.ReferenceType.Should().Be(JournalReferenceType.Payment);
        entry.TotalDebit.Should().Be(600_000m);
        entry.TotalCredit.Should().Be(600_000m, because: "سند دریافتی باید متوازن باشد");
        entry.Lines.Should().HaveCount(2);
        entry.Lines.Single(l => l.AccountId == 101).Debit.Should().Be(600_000m,
            because: "بدهکار صندوق (1101)");
        entry.Lines.Single(l => l.AccountId == 102).Credit.Should().Be(600_000m,
            because: "بستانکار مطالبات بیمار (1103)");
    }

    [Fact]
    public async Task Missing_Accounting_Coding_Must_Not_Record_A_Payment()
    {
        _accounts.Clear();

        var result = await MakeHandler().Handle(Pay(600_000m), CancellationToken.None);

        result.Error.Code.Should().Be("PAYMENT.ACCOUNTS_UNAVAILABLE");
        _addedJournals.Should().BeEmpty();
        _addedPayments.Should().BeEmpty();
        _invoice.PaidAmount.Should().Be(400_000m);
        _invoice.RemainingAmount.Should().Be(600_000m);
        _uow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Inactive_Or_NonDetail_Account_Must_Not_Record_A_Payment(bool active, bool detail)
    {
        _accounts[0].IsActive = active;
        _accounts[0].IsDetail = detail;
        var result = await MakeHandler().Handle(Pay(200_000m), CancellationToken.None);
        result.Error.Code.Should().Be("PAYMENT.ACCOUNTS_UNAVAILABLE");
        _addedPayments.Should().BeEmpty();
        _addedJournals.Should().BeEmpty();
        _invoice.RemainingAmount.Should().Be(600_000m);
    }

    [Fact]
    public async Task Payment_Record_Should_Carry_Cashier_Method_And_Status()
    {
        await MakeHandler().Handle(Pay(200_000m), CancellationToken.None);

        _addedPayments.Should().HaveCount(1);
        var payment = _addedPayments[0];
        payment.CashierId.Should().Be(42, because: "صندوقدار از سرویس کاربر جاری می‌آید");
        payment.Method.Should().Be(PaymentMethod.Card);
        payment.Status.Should().Be(PaymentRecordStatus.Completed);
        payment.PatientId.Should().Be(PatientId);
        payment.InvoiceId.Should().Be(55L);
    }

    // ═══════════════ اعتبارسنج ═══════════════

    [Fact]
    public void Validator_Should_Reject_Zero_Or_Negative_Amount()
    {
        var validator = new RecordPaymentCommandValidator();

        var result = validator.Validate(Pay(0m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("بیشتر از صفر"));
    }

    [Fact]
    public void Validator_Should_Reject_Zero_PatientId()
    {
        var validator = new RecordPaymentCommandValidator();

        var result = validator.Validate(Pay(100m, patientId: 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("شناسه بیمار"));
    }

    [Fact]
    public void Validator_Should_Reject_Null_InvoiceId()
    {
        var validator = new RecordPaymentCommandValidator();

        var result = validator.Validate(
            new RecordPaymentCommand(new RecordPaymentDto(InvoiceId: null, PatientId: 1,
                Amount: 100m, Method: PaymentMethod.Cash)));

        result.IsValid.Should().BeFalse(because: "پرداخت بدون فاکتور مقصد معنا ندارد");
    }

    private sealed class AsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public AsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public AsyncEnumerable(Expression expression) : base(expression) { }
        IQueryProvider IQueryable.Provider => new AsyncProvider<T>(this);
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    private sealed class AsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
        public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
    }

    private sealed class AsyncProvider<T>(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => new AsyncEnumerable<T>(expression);
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new AsyncEnumerable<TElement>(expression);
        public object? Execute(Expression expression) => inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments().Single();
            var execute = typeof(IQueryProvider).GetMethods()
                .Single(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                .MakeGenericMethod(resultType);
            var value = execute.Invoke(inner, new object[] { expression });
            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType).Invoke(null, new[] { value })!;
        }
    }
}
