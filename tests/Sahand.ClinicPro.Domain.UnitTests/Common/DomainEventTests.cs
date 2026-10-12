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

        entity.DomainEvents.Should().ContainInOrder(first, second, third,
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
    public void Records_With_Same_Payload_Should_Be_Equal()
    {
        // دو ویزیت یکسان (شناسه‌های برابر) — رویدادهای هم‌ارزش‌اند؛
        // این برابری پایهٔ دی‌دوپلیکیشن مصرف‌کننده‌ها در آینده است
        var a = new VisitCompletedEvent(visitId: 10, patientId: 20, doctorId: 30);
        var b = new VisitCompletedEvent(visitId: 10, patientId: 20, doctorId: 30);

        a.Should().Be(b);
        a.Should().NotBe(new VisitCompletedEvent(visitId: 11, patientId: 20, doctorId: 30));
    }

    [Fact]
    public void Events_Of_Different_Types_Should_Not_Be_Equal()
    {
        DomainEventBase a = new TestEventA(1);
        DomainEventBase b = new TestEventB("x");

        a.Should().NotBe(b);
    }
}
