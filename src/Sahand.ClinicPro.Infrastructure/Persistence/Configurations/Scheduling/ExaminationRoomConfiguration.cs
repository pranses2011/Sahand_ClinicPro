using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Scheduling;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ExaminationRooms — اتاق ویزیت - ظرفیت فیزیکی درمانگاه</summary>
public class ExaminationRoomConfiguration : IEntityTypeConfiguration<ExaminationRoom>
{
    public void Configure(EntityTypeBuilder<ExaminationRoom> builder)
    {
        builder.ToTable("ExaminationRooms", t => t.HasComment("اتاق ویزیت - ظرفیت فیزیکی درمانگاه"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoomNumber).HasMaxLength(10).HasComment("شماره اتاق - یکتا");
        builder.Property(x => x.RoomName).HasMaxLength(50).HasComment("نام اتاق");
        builder.Property(x => x.RoomType).HasConversion<byte>().HasComment("نوع اتاق");

        builder.HasIndex(x => new { x.RoomNumber }).IsUnique().HasDatabaseName("IX_ExaminationRoom_RoomNumber").HasFilter("[IsDeleted] = 0");
    }
}
