using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول UserRoles — رابطه چند‌به‌چند کاربر و نقش - کلید مرکب</summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", t => t.HasComment("رابطه چند‌به‌چند کاربر و نقش - کلید مرکب"));
        builder.HasKey(x => new { x.UserId, x.RoleId });


    }
}
