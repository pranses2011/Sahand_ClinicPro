using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول UserModulePermissions — دسترسی ماژولی کاربر - سطح دسترسی دقیق‌تر از نقش</summary>
public class UserModulePermissionConfiguration : IEntityTypeConfiguration<UserModulePermission>
{
    public void Configure(EntityTypeBuilder<UserModulePermission> builder)
    {
        builder.ToTable("UserModulePermissions", t => t.HasComment("دسترسی ماژولی کاربر - سطح دسترسی دقیق‌تر از نقش"));
        builder.HasKey(x => x.Id);


    }
}
