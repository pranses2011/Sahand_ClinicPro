using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی دعوت عمومی یک‌بارمصرف نظرسنجی.</summary>
public class SurveyInvitationConfiguration : IEntityTypeConfiguration<SurveyInvitation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SurveyInvitation> builder)
    {
        builder.ToTable("SurveyInvitations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UsedAt).IsConcurrencyToken();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasOne<Survey>().WithMany().HasForeignKey(x => x.SurveyId).OnDelete(DeleteBehavior.NoAction);
    }
}
