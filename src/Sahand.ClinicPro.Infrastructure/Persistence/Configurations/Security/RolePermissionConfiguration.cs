using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول RolePermissions — رابطه چند‌به‌چند نقش و دسترسی - کلید مرکب</summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions", t => t.HasComment("رابطه چند‌به‌چند نقش و دسترسی - کلید مرکب"));
        builder.HasKey(x => new { x.RoleId, x.PermissionId });


    }
}
