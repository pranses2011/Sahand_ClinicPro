using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی ارجاع مستقل به فیزیوتراپی.</summary>
public class PhysiotherapyReferralConfiguration : IEntityTypeConfiguration<PhysiotherapyReferral>
{
    public void Configure(EntityTypeBuilder<PhysiotherapyReferral> builder)
    {
        builder.ToTable("PhysiotherapyReferrals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.RequestId).IsUnique();
        builder.HasIndex(x => new { x.PatientId, x.Id });
        builder.HasOne(x => x.Visit).WithMany().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.NoAction);
    }
}
