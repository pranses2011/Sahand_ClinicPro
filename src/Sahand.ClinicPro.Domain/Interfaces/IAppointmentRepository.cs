using System;
using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Entities.Scheduling;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// ریپازیتوری تخصصی نوبت‌ها
/// </summary>
public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>شمارش نوبت‌های فعال یک پزشک در یک تاریخ (برای کنترل ظرفیت)</summary>
    Task<int> CountActiveByDoctorAndDateAsync(long doctorId, DateOnly date, CancellationToken cancellationToken = default);
}
