using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PhysiotherapySessions — جلسه فیزیوتراپی - تکرارهای دوره درمان</summary>
public class PhysiotherapySessionConfiguration : IEntityTypeConfiguration<PhysiotherapySession>
{
    public void Configure(EntityTypeBuilder<PhysiotherapySession> builder)
    {
        builder.ToTable("PhysiotherapySessions", t => t.HasComment("جلسه فیزیوتراپی - تکرارهای دوره درمان"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت جلسه");

        builder.HasIndex(x => new { x.PatientId, x.SessionDate }).HasDatabaseName("IX_PhysiotherapySession_PatientId_SessionDate");
    }
}
