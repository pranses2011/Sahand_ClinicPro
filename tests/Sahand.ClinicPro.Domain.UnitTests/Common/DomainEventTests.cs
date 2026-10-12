using FluentAssertions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Events;
using Xunit;

namespace Sahand.ClinicPro.Domain.UnitTests.Common;
/// <summary>
/// تست‌های مکانیزم رویدادهای دامنه — چرخهٔ ۵۱:
/// صف رویداد BaseEntity (افزودن/حذف/پاک‌سازی/ترتیب)، مُهر زمانی UTC و
/// برابری مقداری رکوردهای رویداد. رویدادها پس از Commit توسط
/// DispatchDomainEventsInterceptor منتشر می‌شوند — سلامت صفِ خودِ دامنه
/// پیش‌نیاز صحت آن انتشار است.
/// </summary>
public class DomainEventTests
{
    /// <summary>موجودیت آزمایشی با دسترسی به مکانیزم رویداد پایه</summary>
    private sealed class TestEntity : BaseEntity;

    /// <summary>رویداد آزمایشی ۱</summary>
    private sealed record TestEventA(long Id) : DomainEventBase;

    /// <summary>رویداد آزمایشی ۲</summary>
    private sealed record TestEventB(string Code) : DomainEventBase;

    // ═══════════════ صف رویداد ═══════════════

    [Fact]
    public void AddDomainEvent_Should_Enqueue_And_Be_Visible()
    {
        var entity = new TestEntity();
        var evt = new TestEventA(1);

        entity.AddDomainEvent(evt);

        entity.DomainEvents.Should().Contain(evt);
        entity.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void AddDomainEvent_Three_Times_Should_Preserve_Insertion_Order()
    {
        var entity = new TestEntity();
        var first = new TestEventA(1);
        var second = new TestEventB("x");
        var third = new TestEventA(2);

        entity.AddDomainEvent(first);
        entity.AddDomainEvent(second);
        entity.AddDomainEvent(third);

        // روایت IEnumerable (نه params) تا because هم بگیرد
        entity.DomainEvents.Should().ContainInOrder(
            new IDomainEvent[] { first, second, third },
            because: "ترتیب انتشار رویدادها باید همان ترتیب وقوعشان باشد");
    }

    [Fact]
    public void RemoveDomainEvent_Should_Remove_Only_That_Event()
    {
        var entity = new TestEntity();
        var first = new TestEventA(1);
        var second = new TestEventB("x");
        entity.AddDomainEvent(first);
        entity.AddDomainEvent(second);

        entity.RemoveDomainEvent(first);

        entity.DomainEvents.Should().HaveCount(1).And.Contain(second);
    }

    [Fact]
    public void ClearDomainEvents_Should_Empty_The_Queue()
    {
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestEventA(1));
        entity.AddDomainEvent(new TestEventB("x"));

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty(
            because: "پس از انتشار/Dispatch، صف باید برای ردیف بعدی پاک شود (idempotency انتشار)");
    }

    [Fact]
    public void DomainEvents_On_Fresh_Entity_Should_Be_Empty()
    {
        new TestEntity().DomainEvents.Should().BeEmpty();
    }

    // ═══════════════ مُهر زمانی ═══════════════

    [Fact]
    public void OccurredOn_Should_Be_Recent_Utc()
    {
        var before = DateTime.UtcNow.AddSeconds(-5);
        var evt = new TestEventA(1);
        var after = DateTime.UtcNow.AddSeconds(5);

        evt.OccurredOn.Should().BeOnOrAfter(before);
        evt.OccurredOn.Should().BeOnOrBefore(after,
            because: "مُهر زمانی رویداد باید لحظهٔ ساختش باشد");
    }

    // ═══════════════ برابری مقداری رکورد رویداد ═══════════════

    [Fact]
    public void Records_With_Same_Payload_Should_Differ_By_Unique_Timestamp()
    {
        // بار مساوی، مُهر زمانی متفاوت: OccurredOn بخشی از برابریِ رکورد است —
        // یعنی هر «رخداد» یکتاست حتی با دامنهٔ یکسان (قراردار طراحی درست:
        // مصرف‌کننده‌ها هرگز نباید دو رخداد مجزا را «همان یک» بپندارند)
        var a = new VisitCompletedEvent(VisitId: 10, PatientId: 20, DoctorId: 30);
        var b = new VisitCompletedEvent(VisitId: 10, PatientId: 20, DoctorId: 30);

        a.Should().NotBe(b, because: "OccurredOn هر نمونه در ساختش مُهر می‌خورد");
        a.VisitId.Should().Be(b.VisitId);
        a.PatientId.Should().Be(b.PatientId);
    }

    [Fact]
    public void Events_Of_Different_Types_Should_Not_Be_Equal()
    {
        DomainEventBase a = new TestEventA(1);
        DomainEventBase b = new TestEventB("x");

        a.Should().NotBe(b);
    }
}
