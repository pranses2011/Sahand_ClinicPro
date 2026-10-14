using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.API.Controllers;

// ═══════════════════════════════════════════════════════════════════════
// گزارش‌های نموداری — دسته دوم (چرخه دوازدهم، فاز H)
// همان قرارداد chart فاز F2 چرخه یازدهم: روند→area، مقایسه→column،
// سهم→doughnut، رتبه→hbar. ۱۱ گزارش تازه → مجموع ۲۱ گزارش نموداری.
// ═══════════════════════════════════════════════════════════════════════
public partial class ReportsController
{
    /// <summary>۱۱) درآمد به تفکیک شعبه — ماهیت مقایسه شعبه‌ها → ستونی</summary>
    private async Task<IActionResult> ChartBranchRevenueAsync(DateTime from, DateTime to)
    {
        var invoices = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && !i.IsDeleted)
            .Select(i => new { i.BranchId, i.TotalAmount })
            .ToListAsync();

        var branches = await _reporting.Set<Domain.Entities.People.Branch>().AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Select(b => new { b.Id, b.BranchName })
            .ToDictionaryAsync(b => b.Id, b => b.BranchName);

        var byBranch = invoices
            .GroupBy(i => i.BranchId ?? 0L)
            .Select(g => new
            {
                label = g.Key == 0 ? "بدون شعبه" : branches.GetValueOrDefault(g.Key, "شعبه #" + g.Key),
                value = (double)g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(b => b.value)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = (decimal)byBranch.Sum(b => b.value),
                branches = byBranch.Count,
                top = byBranch.FirstOrDefault()?.label ?? "—"
            },
            columns = new[] { "شعبه", "درآمد (ریال)" },
            rows = byBranch.Select(b => new { branch = b.label, revenue = (decimal)b.value }).ToList(),
            chart = new
            {
                type = "column",
                title = "درآمد به تفکیک شعبه",
                unit = "ریال",
                points = byBranch
            }
        }));
    }

    /// <summary>۱۲) سهم بیمه‌گران از فاکتورها — ماهیت سهم از کل → دونات</summary>
    private async Task<IActionResult> ChartInsuranceMixAsync(DateTime from, DateTime to)
    {
        var invoices = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && !i.IsDeleted
                        && i.InsuranceCompanyId != null)
            .Select(i => new { i.InsuranceCompanyId, i.TotalAmount })
            .ToListAsync();

        var companies = await _reporting.Set<Domain.Entities.Billing.InsuranceCompany>().AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var mix = invoices
            .GroupBy(i => i.InsuranceCompanyId!.Value)
            .Select(g => new
            {
                label = companies.GetValueOrDefault(g.Key, "بیمه #" + g.Key),
                value = (double)g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(s => s.value)
            .Take(8)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { total = (decimal)mix.Sum(m => m.value), insurers = mix.Count },
            columns = new[] { "بیمه‌گر", "مبلغ فاکتور (ریال)" },
            rows = mix.Select(m => new { insurer = m.label, amount = (decimal)m.value }).ToList(),
            chart = new
            {
                type = "doughnut",
                title = "سهم بیمه‌گران از فاکتورها",
                unit = "ریال",
                points = mix
            }
        }));
    }

    /// <summary>۱۳) سن بدهی مطالبات — ماهیت مقایسه رده‌های زمانی → ستونی</summary>
    private async Task<IActionResult> ChartReceivableAgingAsync()
    {
        var today = DateTime.UtcNow.Date;
        var open = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Cancelled && i.RemainingAmount > 0)
            .Select(i => new { i.InvoiceDate, i.RemainingAmount })
            .ToListAsync();

        var buckets = new (int Min, int Max, string Label)[]
        {
            (0, 30, "۰ تا ۳۰ روز"), (31, 60, "۳۱ تا ۶۰ روز"),
            (61, 90, "۶۱ تا ۹۰ روز"), (91, 100000, "بیش از ۹۰ روز"),
        };

        var points = buckets.Select(b => new
        {
            label = b.Label,
            value = (double)open.Where(i =>
            {
                var age = (today - i.InvoiceDate.Date).Days;
                return age >= b.Min && age <= b.Max;
            }).Sum(i => i.RemainingAmount)
        }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = (decimal)points.Sum(p => p.value),
                invoices = open.Count,
                worst = points.Last().value > 0 ? "بیش از ۹۰ روز" : "—"
            },
            columns = new[] { "رده زمانی", "مانده بدهی (ریال)" },
            rows = points.Select(p => new { bucket = p.label, remaining = (decimal)p.value }).ToList(),
            chart = new
            {
                type = "column",
                title = "سن بدهی مطالبات",
                unit = "ریال",
                points
            }
        }));
    }

    /// <summary>۱۴) سهم ویزیت اول از کل نوبت‌ها — ماهیت سهم از کل → دونات</summary>
    private async Task<IActionResult> ChartNewVsReturningAsync(DateTime from, DateTime to)
    {
        var appointments = await _reporting.Set<Domain.Entities.Scheduling.Appointment>().AsNoTracking()
            .Where(a => a.AppointmentDate >= IranDateOf(from)
                     && a.AppointmentDate <= IranDateOf(to)
                     && !a.IsDeleted)
            .Select(a => new { a.IsFirstVisit })
            .ToListAsync();

        var first = appointments.Count(a => a.IsFirstVisit);
        var returning = appointments.Count - first;
        var points = new List<(string Label, double Value)>();
        if (first > 0) points.Add(("بیمار جدید (ویزیت اول)", first));
        if (returning > 0) points.Add(("بیمار بازگشتی", returning));

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = (decimal)appointments.Count,
                firstVisit = first,
                returning,
                firstRatio = appointments.Count > 0 ? Math.Round(first * 100m / appointments.Count, 1) : 0
            },
            columns = new[] { "نوع بیمار", "تعداد نوبت" },
            rows = points.Select(p => new { kind = p.Label, count = (int)p.Value }).ToList(),
            chart = new
            {
                type = "doughnut",
                title = "سهم بیمار جدید از کل نوبت‌ها",
                unit = "نوبت",
                points = points.Select(p => new { label = p.Label, value = p.Value })
            }
        }));
    }

    /// <summary>۱۵) روند ماهانه ثبت بیمار جدید — ماهیت روند زمانی → سطحی</summary>
    private async Task<IActionResult> ChartNewPatientsTrendAsync(DateTime from, DateTime to)
    {
        var patients = await _reporting.Set<Domain.Entities.People.Patient>().AsNoTracking()
            .Where(p => !p.IsDeleted && p.CreatedAt >= from && p.CreatedAt <= to)
            .Select(p => new { p.CreatedAt })
            .ToListAsync();

        // گروه‌بندی ماه شمسی در حافظه
        var monthly = patients
            .Select(p => PersianDateHelper.ToJalali(p.CreatedAt))
            .GroupBy(j => new { Y = j.Year, M = j.Month })
            .Select(g => new { ym = (g.Key.Y, g.Key.M), value = (double)g.Count() })
            .OrderBy(x => x.ym.Y).ThenBy(x => x.ym.M)
            .TakeLast(12)
            .Select(x => new { label = PersianDateHelper.MonthNames[x.ym.M - 1] + " " + x.ym.Y, value = x.value })
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = (decimal)monthly.Sum(m => m.value),
                months = monthly.Count,
                monthAvg = monthly.Count > 0 ? Math.Round((decimal)(monthly.Sum(m => m.value) / monthly.Count), 1) : 0
            },
            columns = new[] { "ماه (شمسی)", "بیمار جدید" },
            rows = monthly.Select(m => new { month = m.label, patients = (int)m.value }).ToList(),
            chart = new
            {
                type = "area",
                title = "روند ماهانه ثبت بیمار جدید",
                unit = "بیمار",
                points = monthly
            }
        }));
    }

    /// <summary>۱۶) بدهکاران برتر — ماهیت رتبه‌بندی → میله‌ای افقی</summary>
    private async Task<IActionResult> ChartTopDebtorsAsync(DateTime from, DateTime to, int top)
    {
        var invoices = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && !i.IsDeleted
                        && i.Status != InvoiceStatus.Cancelled && i.RemainingAmount > 0)
            .Select(i => new { i.PatientId, i.RemainingAmount, Patient = i.Patient })
            .ToListAsync();

        var ranked = invoices
            .Where(i => i.Patient != null)
            .GroupBy(i => new { i.PatientId, Name = i.Patient!.FirstName + " " + i.Patient.LastName })
            .Select(g => new { label = g.Key.Name, value = (double)g.Sum(x => x.RemainingAmount) })
            .OrderByDescending(r => r.value)
            .Take(Math.Clamp(top, 3, 15))
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { debtors = ranked.Count, total = (decimal)ranked.Sum(r => r.value) },
            columns = new[] { "بدهکار", "مانده (ریال)" },
            rows = ranked.Select(r => new { patient = r.label, remaining = (decimal)r.value }).ToList(),
            chart = new
            {
                type = "hbar",
                title = "بدهکاران برتر",
                unit = "ریال",
                points = ranked
            }
        }));
    }

    /// <summary>۱۷) ترکیب وضعیت نوبت‌ها — ماهیت سهم از کل → دونات</summary>
    private async Task<IActionResult> ChartAppointmentStatusAsync(DateTime from, DateTime to)
    {
        var appointments = await _reporting.Set<Domain.Entities.Scheduling.Appointment>().AsNoTracking()
            .Where(a => a.AppointmentDate >= IranDateOf(from)
                     && a.AppointmentDate <= IranDateOf(to)
                     && !a.IsDeleted)
            .Select(a => new { a.Status })
            .ToListAsync();

        var names = new Dictionary<AppointmentStatus, string>
        {
            [AppointmentStatus.Booked] = "رزرو شده", [AppointmentStatus.Confirmed] = "تأیید شده",
            [AppointmentStatus.Arrived] = "حاضر", [AppointmentStatus.InProgress] = "در حال ویزیت",
            [AppointmentStatus.Completed] = "تکمیل شده", [AppointmentStatus.Cancelled] = "لغو شده",
            [AppointmentStatus.NoShow] = "عدم مراجعه",
        };

        var byStatus = appointments
            .GroupBy(a => a.Status)
            .Select(g => new { label = names.GetValueOrDefault(g.Key, g.Key.ToString()), value = (double)g.Count() })
            .OrderByDescending(s => s.value)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = (decimal)appointments.Count,
                completed = byStatus.FirstOrDefault(s => s.label == "تکمیل شده")?.value ?? 0,
                cancelled = byStatus.FirstOrDefault(s => s.label == "لغو شده")?.value ?? 0
            },
            columns = new[] { "وضعیت", "تعداد نوبت" },
            rows = byStatus.Select(s => new { status = s.label, count = (int)s.value }).ToList(),
            chart = new
            {
                type = "doughnut",
                title = "ترکیب وضعیت نوبت‌ها",
                unit = "نوبت",
                points = byStatus
            }
        }));
    }

    /// <summary>۱۸) پرتکرارترین تست‌های آزمایشگاه — ماهیت رتبه‌بندی → میله‌ای افقی</summary>
    private async Task<IActionResult> ChartLabTopTestsAsync(DateTime from, DateTime to, int top)
    {
        var items = await _reporting.Set<Domain.Entities.Paraclinical.LabOrderItem>().AsNoTracking()
            .Where(it => it.LabOrder!.OrderDate >= from && it.LabOrder!.OrderDate <= to
                         && !it.IsDeleted && it.Test != null)
            .Select(it => new { Name = it.Test!.TestName })
            .ToListAsync();

        var ranked = items
            .GroupBy(it => it.Name)
            .Select(g => new { label = g.Key, value = (double)g.Count() })
            .OrderByDescending(r => r.value)
            .Take(Math.Clamp(top, 3, 15))
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { tests = ranked.Count, orders = items.Count },
            columns = new[] { "آزمون", "تعداد تکرار" },
            rows = ranked.Select(r => new { test = r.label, count = (int)r.value }).ToList(),
            chart = new
            {
                type = "hbar",
                title = "پرتکرارترین آزمون‌های آزمایشگاه",
                unit = "بار",
                points = ranked
            }
        }));
    }

    /// <summary>۱۹) پرمصرف‌ترین داروهای نسخه — ماهیت رتبه‌بندی → میله‌ای افقی</summary>
    private async Task<IActionResult> ChartTopMedicationsAsync(DateTime from, DateTime to, int top)
    {
        var items = await _reporting.Set<Domain.Entities.Clinical.PrescriptionItem>().AsNoTracking()
            .Where(it => it.Prescription!.PrescriptionDate >= from && it.Prescription!.PrescriptionDate <= to
                         && !it.IsDeleted && it.Medication != null)
            .Select(it => new { Name = it.Medication!.GenericNameFa ?? it.Medication.BrandName ?? it.Medication.GenericName, it.Quantity })
            .ToListAsync();

        var ranked = items
            .GroupBy(it => it.Name)
            .Select(g => new { label = g.Key, value = (double)g.Sum(x => x.Quantity) })
            .OrderByDescending(r => r.value)
            .Take(Math.Clamp(top, 3, 15))
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { medications = ranked.Count, items = items.Count },
            columns = new[] { "دارو", "مصرف کل" },
            rows = ranked.Select(r => new { medication = r.label, consumed = (decimal)r.value }).ToList(),
            chart = new
            {
                type = "hbar",
                title = "پرمصرف‌ترین داروهای نسخه",
                unit = "واحد",
                points = ranked
            }
        }));
    }

    /// <summary>۲۰) سهم پرسنل از حقوق درصدی آخرین دوره — ماهیت سهم از کل → دونات</summary>
    private async Task<IActionResult> ChartSalaryShareAsync()
    {
        var salaries = await _reporting.Set<Domain.Entities.Personnel.EmployeeSalary>().AsNoTracking()
            .Where(s => !s.IsDeleted && s.ServiceCommission > 0)
            .Select(s => new { s.EmployeeId, s.PeriodYear, s.PeriodMonth, s.ServiceCommission })
            .ToListAsync();

        if (salaries.Count == 0)
            return Ok(ApiResponse<object>.Success(new { available = true, message = "هنوز حقوق درصدی ثبت نشده است", summary = new { total = 0m, personnel = 0, period = "—" }, columns = new[] { "پرسنل", "حقوق درصدی (ریال)" }, rows = new List<object>(), chart = new { type = "doughnut", title = "سهم پرسنل از حقوق درصدی", unit = "ریال", points = new List<object>() } }));

        // آخرین دوره حقوق
        var latest = salaries.OrderByDescending(s => s.PeriodYear).ThenByDescending(s => s.PeriodMonth).First();
        var period = $"{"۱۴" + latest.PeriodYear}/{latest.PeriodMonth}"; // نمایش دوره
        var periodRows = salaries.Where(s => s.PeriodYear == latest.PeriodYear && s.PeriodMonth == latest.PeriodMonth).ToList();

        var employees = await _reporting.Set<Domain.Entities.People.Employee>().AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Select(e => new { e.Id, e.FirstName, e.LastName })
            .ToDictionaryAsync(e => e.Id);

        var shares = periodRows
            .GroupBy(s => s.EmployeeId)
            .Select(g => new
            {
                label = employees.TryGetValue(g.Key, out var e) ? $"{e.FirstName} {e.LastName}" : "پرسنل #" + g.Key,
                value = (double)g.Sum(x => x.ServiceCommission)
            })
            .OrderByDescending(s => s.value)
            .Take(10)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { total = (decimal)shares.Sum(s => s.value), personnel = shares.Count, period },
            columns = new[] { "پرسنل", "حقوق درصدی (ریال)" },
            rows = shares.Select(s => new { employee = s.label, commission = (decimal)s.value }).ToList(),
            chart = new
            {
                type = "doughnut",
                title = "سهم پرسنل از حقوق درصدی",
                unit = "ریال",
                points = shares
            }
        }));
    }

    /// <summary>۲۱) روند روزانه صدور نسخه — ماهیت روند زمانی → سطحی</summary>
    private async Task<IActionResult> ChartPrescriptionTrendAsync(DateTime from, DateTime to)
    {
        var prescriptions = await _reporting.Set<Domain.Entities.Clinical.Prescription>().AsNoTracking()
            .Where(p => p.PrescriptionDate >= from && p.PrescriptionDate <= to && !p.IsDeleted)
            .Select(p => new { Date = p.PrescriptionDate!.Value })
            .ToListAsync();

        var daily = prescriptions
            .GroupBy(p => p.Date.Date)
            .Select(g => new { label = PersianDateHelper.ToShamsi(g.Key), value = (double)g.Count() })
            .OrderBy(p => p.label)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = (decimal)daily.Sum(d => d.value),
                days = daily.Count,
                dailyAvg = daily.Count > 0 ? Math.Round((decimal)(daily.Sum(d => d.value) / daily.Count), 1) : 0
            },
            columns = new[] { "روز", "تعداد نسخه" },
            rows = daily.Select(d => new { day = d.label, prescriptions = (int)d.value }).ToList(),
            chart = new
            {
                type = "area",
                title = "روند روزانه صدور نسخه",
                unit = "نسخه",
                points = daily
            }
        }));
    }
}
