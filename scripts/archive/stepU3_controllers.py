# -*- coding: utf-8 -*-
"""گام U3 — اتصال کنترلرها: DoctorsController + LookupsController
ایدمپوتنت: اگر قبلاً ویرایش شده باشد رد می‌شود
"""
import io

ROOT = "/home/z/my-project/Sahand_ClinicPro"
DOCS = f"{ROOT}/src/Sahand.ClinicPro.API/Controllers/DoctorsController.cs"
LOOK = f"{ROOT}/src/Sahand.ClinicPro.Application/Features/Lookups/LookupQueries.cs"
LOOKC = f"{ROOT}/src/Sahand.ClinicPro.API/Controllers/LookupsController.cs"

# ─────────── 1) DoctorsController ───────────
with io.open(DOCS, encoding="utf-8-sig") as f:
    src = f.read()

if "GetDoctorScheduleQuery" in src:
    print("SKIP DoctorsController — قبلاً ویرایش شده")
else:
    NEW_ENDPOINTS = '''
    /// <summary>برنامه کاری هفتگی پزشک — هفت ردیف شنبه تا جمعه</summary>
    [HttpGet("{id:long}/schedule")]
    public async Task<IActionResult> GetSchedule(long id)
    {
        var result = await _mediator.Send(new GetDoctorScheduleQuery(id));
        return ToResponse(result);
    }

    /// <summary>ذخیره برنامه کاری هفتگی — جایگزینی کامل</summary>
    [HttpPut("{id:long}/schedule")]
    public async Task<IActionResult> SaveSchedule(long id, [FromBody] SaveScheduleRequest request)
    {
        var result = await _mediator.Send(new SaveDoctorScheduleCommand(id, request));
        return ToResponse(result);
    }

    /// <summary>غیبت‌های آینده پزشک</summary>
    [HttpGet("{id:long}/absences")]
    public async Task<IActionResult> GetAbsences(long id)
    {
        var result = await _mediator.Send(new GetDoctorAbsencesQuery(id));
        return ToResponse(result);
    }

    /// <summary>ثبت غیبت جدید — با شمارش نوبت‌های درگیر</summary>
    [HttpPost("{id:long}/absences")]
    public async Task<IActionResult> CreateAbsence(long id, [FromBody] CreateAbsenceRequest request)
    {
        var result = await _mediator.Send(new CreateDoctorAbsenceCommand(id, request));
        return ToResponse(result);
    }

    /// <summary>حذف غیبت</summary>
    [HttpDelete("absences/{absenceId:long}")]
    public async Task<IActionResult> DeleteAbsence(long absenceId)
    {
        var result = await _mediator.Send(new DeleteDoctorAbsenceCommand(absenceId));
        return ToResponse(result);
    }

    /// <summary>آمار عملکرد پزشک — ماه جاری و امروز</summary>
    [HttpGet("{id:long}/performance")]
    public async Task<IActionResult> GetPerformance(long id)
    {
        var result = await _mediator.Send(new GetDoctorPerformanceQuery(id));
        return ToResponse(result);
    }
}'''
    idx = src.rstrip().rfind("}")
    src = src.rstrip()[:idx] + NEW_ENDPOINTS + "\n"

    NEED_USINGS = [
        "using Sahand.ClinicPro.Application.DTOs.Doctors;",
        "using Sahand.ClinicPro.Application.Features.Doctors.Commands.CreateDoctorAbsence;",
        "using Sahand.ClinicPro.Application.Features.Doctors.Commands.DeleteDoctorAbsence;",
        "using Sahand.ClinicPro.Application.Features.Doctors.Commands.SaveDoctorSchedule;",
        "using Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorAbsences;",
        "using Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorPerformance;",
        "using Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorSchedule;",
    ]
    lines = src.split("\n")
    for u in NEED_USINGS:
        if u not in lines:
            last_using = max(i for i, l in enumerate(lines) if l.startswith("using "))
            lines.insert(last_using + 1, u)
    src = "\n".join(lines)

    with io.open(DOCS, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(src)
    print("OK DoctorsController +6 endpoint")

# ─────────── 2) LookupQueries.cs ───────────
with io.open(LOOK, encoding="utf-8-sig") as f:
    look_src = f.read()

if "GetSpecialtiesQuery" in look_src:
    print("SKIP LookupQueries — قبلاً ویرایش شده")
else:
    LOOKUP_ADD = '''

/// <summary>آیتم تخصص برای لیست انتخاب</summary>
public class SpecialtyItemDto
{
    /// <summary>شناسه</summary>
    public long Id { get; set; }

    /// <summary>نام تخصص</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>شناسه والد — فوق‌تخصص‌ها بر اساس والد دسته می‌شوند</summary>
    public long? ParentId { get; set; }
}

/// <summary>آیتم اتاق معاینه</summary>
public class RoomItemDto
{
    /// <summary>شناسه</summary>
    public long Id { get; set; }

    /// <summary>نام نمایشی</summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>آیتم پرسنل برای انتخاب در فرم پزشک</summary>
public class EmployeeItemDto
{
    /// <summary>شناسه</summary>
    public long Id { get; set; }

    /// <summary>نام کامل</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>شماره پرسنلی</summary>
    public string? PersonnelCode { get; set; }
}

/// <summary>پرس‌وجوی تخصص‌های فعال</summary>
public record GetSpecialtiesQuery(bool OnlyParents = false) : IRequest<Result<List<SpecialtyItemDto>>>;

/// <summary>گرداننده تخصص‌ها</summary>
public class GetSpecialtiesQueryHandler : IRequestHandler<GetSpecialtiesQuery, Result<List<SpecialtyItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetSpecialtiesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<SpecialtyItemDto>>> Handle(GetSpecialtiesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Specialty>().QueryableNoTracking
            .Where(s => s.IsActive);
        if (request.OnlyParents)
            query = query.Where(s => s.ParentId == null);

        var items = await query.OrderBy(s => s.Name)
            .Select(s => new SpecialtyItemDto { Id = s.Id, Name = s.Name, ParentId = s.ParentId })
            .ToListAsync(cancellationToken);

        return Result<List<SpecialtyItemDto>>.Success(items);
    }
}

/// <summary>پرس‌وجوی اتاق‌های معاینه فعال</summary>
public record GetRoomsQuery : IRequest<Result<List<RoomItemDto>>>;

/// <summary>گرداننده اتاق‌ها</summary>
public class GetRoomsQueryHandler : IRequestHandler<GetRoomsQuery, Result<List<RoomItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetRoomsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<RoomItemDto>>> Handle(GetRoomsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<ExaminationRoom>().QueryableNoTracking
            .Where(r => r.IsActive)
            .OrderBy(r => r.Id)
            .Select(r => new RoomItemDto
            {
                Id = r.Id,
                DisplayName = r.RoomName != null && r.RoomName != "" ? r.RoomName :
                    ("اتاق " + (r.RoomNumber != null ? r.RoomNumber : r.Id.ToString()))
            })
            .ToListAsync(cancellationToken);

        return Result<List<RoomItemDto>>.Success(items);
    }
}

/// <summary>پرس‌وجوی پرسنل فعال برای Combobox فرم پزشک</summary>
public record GetEmployeesQuery : IRequest<Result<List<EmployeeItemDto>>>;

/// <summary>گرداننده پرسنل</summary>
public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, Result<List<EmployeeItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetEmployeesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<EmployeeItemDto>>> Handle(GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<Employee>().QueryableNoTracking
            .Where(e => e.IsActive)
            .OrderBy(e => e.LastName)
            .Select(e => new EmployeeItemDto
            {
                Id = e.Id,
                FullName = e.FirstName + " " + e.LastName,
                PersonnelCode = e.PersonnelCode
            })
            .ToListAsync(cancellationToken);

        return Result<List<EmployeeItemDto>>.Success(items);
    }
}'''
    look_src = look_src.rstrip() + "\n" + LOOKUP_ADD + "\n"

    lines = look_src.split("\n")
    for u in ["using Sahand.ClinicPro.Domain.Entities.People;",
              "using Sahand.ClinicPro.Domain.Entities.Scheduling;"]:
        if u not in lines:
            last_using = max(i for i, l in enumerate(lines) if l.startswith("using "))
            lines.insert(last_using + 1, u)
    look_src = "\n".join(lines)

    with io.open(LOOK, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(look_src)
    print("OK LookupQueries +3 query")

# ─────────── 3) LookupsController ───────────
with io.open(LOOKC, encoding="utf-8-sig") as f:
    lookc_src = f.read()

if "GetSpecialties" in lookc_src:
    print("SKIP LookupsController — قبلاً ویرایش شده")
else:
    LOOKC_ADD = '''
    /// <summary>فهرست تخصص‌های فعال — با گزینه فقط تخصص‌های اصلی</summary>
    [HttpGet("specialties")]
    public async Task<IActionResult> GetSpecialties([FromQuery] bool onlyParents = false)
    {
        var result = await _mediator.Send(new GetSpecialtiesQuery(onlyParents));
        return ToResponse(result);
    }

    /// <summary>فهرست اتاق‌های معاینه فعال</summary>
    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        var result = await _mediator.Send(new GetRoomsQuery());
        return ToResponse(result);
    }

    /// <summary>فهرست پرسنل فعال — برای انتخاب در فرم پزشک</summary>
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var result = await _mediator.Send(new GetEmployeesQuery());
        return ToResponse(result);
    }
}'''
    idx = lookc_src.rstrip().rfind("}")
    lookc_src = lookc_src.rstrip()[:idx] + LOOKC_ADD + "\n"

    with io.open(LOOKC, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(lookc_src)
    print("OK LookupsController +3 endpoint")
