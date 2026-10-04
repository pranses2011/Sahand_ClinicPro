using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Repositories;

/// <summary>
/// ریپازیتوری تخصصی نوبت‌ها — کنترل ظرفیت پزشک در هر روز
/// </summary>
public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    /// <summary>سازنده با تزریق کانتکست</summary>
    public AppointmentRepository(ClinicProDbContext context) : base(context) { }

    /// <summary>شمارش نوبت‌های فعال یک پزشک در یک تاریخ</summary>
    public async Task<int> CountActiveByDoctorAndDateAsync(long doctorId, DateOnly date, CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(
            a => a.DoctorId == doctorId
                 && a.AppointmentDate == date
                 && a.Status != AppointmentStatus.Cancelled
                 && a.Status != AppointmentStatus.NoShow,
            cancellationToken);
}
