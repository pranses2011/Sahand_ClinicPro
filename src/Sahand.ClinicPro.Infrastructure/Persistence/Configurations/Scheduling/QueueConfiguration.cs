using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Scheduling;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Queues — صف انتظار پویا - فراخوان بیماران نزد پزشک</summary>
public class QueueConfiguration : IEntityTypeConfiguration<Queue>
{
    public void Configure(EntityTypeBuilder<Queue> builder)
    {
        builder.ToTable("Queues", t => t.HasComment("صف انتظار پویا - فراخوان بیماران نزد پزشک"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت در صف");
        builder.Property(x => x.Priority).HasConversion<byte>().HasComment("اولویت");

    }
}
