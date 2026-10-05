﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Appointments;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Queries.SearchAppointments;

/// <summary>
/// پرس‌وجوی جستجوی پیشرفته نوبت‌ها — فرم ۹ با همه فیلترها و صفحه‌بندی
/// </summary>
public record SearchAppointmentsQuery(AppointmentSearchFilter Filter)
    : IRequest<Result<PagedResponse<AppointmentSearchItemDto>>>;

/// <summary>
/// گرداننده جستجو
/// </summary>
public class SearchAppointmentsQueryHandler : IRequestHandler<SearchAppointmentsQuery, Result<PagedResponse<AppointmentSearchItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public SearchAppointmentsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<PagedResponse<AppointmentSearchItemDto>>> Handle(SearchAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var f = request.Filter;
        IQueryable<Appointment> query = _unitOfWork.Repository<Appointment>().QueryableNoTracking
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.Employee)
            .Include(a => a.Specialty);

        var from = PersianDateHelper.ToGregorian(f.FromDateShamsi);
        var to = PersianDateHelper.ToGregorian(f.ToDateShamsi);
        if (from.HasValue)
            query = query.Where(a => a.AppointmentDate >= DateOnly.FromDateTime(from.Value));
        if (to.HasValue)
            query = query.Where(a => a.AppointmentDate <= DateOnly.FromDateTime(to.Value));
        if (f.DoctorId.HasValue)
            query = query.Where(a => a.DoctorId == f.DoctorId.Value);
        if (f.Status.HasValue && Enum.IsDefined(typeof(AppointmentStatus), (byte)f.Status.Value))
            query = query.Where(a => a.Status == (AppointmentStatus)f.Status.Value);
        if (f.Type.HasValue && Enum.IsDefined(typeof(AppointmentType), (byte)f.Type.Value))
            query = query.Where(a => a.AppointmentType == (AppointmentType)f.Type.Value);
        if (f.Source.HasValue && Enum.IsDefined(typeof(AppointmentSource), (byte)f.Source.Value))
            query = query.Where(a => a.Source == (AppointmentSource)f.Source.Value);
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = f.Search.Trim();
            query = query.Where(a =>
                (a.Patient!.FirstName + " " + a.Patient.LastName).Contains(term) ||
                a.Patient.NationalCode.Contains(term) ||
                a.Patient.Mobile.Contains(term) ||
                a.AppointmentCode != null && a.AppointmentCode.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime)
            .Skip((f.PageNumber - 1) * f.PageSize)
            .Take(f.PageSize)
            .Select(a => new AppointmentSearchItemDto(
                a.Id,
                a.AppointmentCode,
                a.Patient!.FirstName + " " + a.Patient.LastName,
                a.Patient.Mobile,
                a.Doctor!.Employee!.FirstName + " " + a.Doctor.Employee.LastName,
                a.Specialty!.Name,
                a.AppointmentDateShamsi ?? PersianDateHelper.ToShamsi(a.AppointmentDate.ToDateTime(TimeOnly.MinValue)),
                a.AppointmentTime.ToString("HH:mm"),
                (int)a.Status,
                (int)a.AppointmentType,
                (int)a.Source,
                a.EstimatedFee))
            .ToListAsync(cancellationToken);

        var paged = PagedResponse<AppointmentSearchItemDto>.Create(items, f.PageNumber, f.PageSize, totalCount);
        return Result<PagedResponse<AppointmentSearchItemDto>>.Success(paged);
    }
}
