using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.API.Controllers;

// ═══════════════════════════════════════════════════════════════════════
// بخش گزارش‌های نموداری — چرخه یازدهم (گام F2)
// هر گزارش علاوه بر کارت‌های خلاصه و جدول، بلاک «chart» برمی‌گرداند تا
// کلاینت نمودارِ متناسب با ماهیت داده رندر کند:
//   روند زمانی → area | مقایسه مقوله‌ای → column
//   سهم از کل  → doughnut | رتبه‌بندی برترین‌ها → hbar
// قاعده انتخاب نوع، مصوب گزارش‌های BI استاندارد است.
// ═══════════════════════════════════════════════════════════════════════
public partial class ReportsController
{
    /// <summary>۱) روند درآمد روزانه — ماهیت روند زمانی → نمودار سطحی</summary>
    private async Task<IActionResult> ChartRevenueTrendAsync(DateTime from, DateTime to)
    {
        var invoices = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && !i.IsDeleted && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Refunded)
            .Select(i => new { i.InvoiceDate, i.TotalAmount })
            .ToListAsync();

        var daily = invoices
            .GroupBy(i => (i.InvoiceDate + IranOffset).Date)
            .Select(g => new { label = PersianDateHelper.ToShamsi(g.Key), value = g.Sum(x => (double)x.TotalAmount) })
            .OrderBy(p => p.label)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = (decimal)daily.Sum(d => d.value),
                days = daily.Count,
                dailyAvg = daily.Count > 0 ? (decimal)(daily.Sum(d => d.value) / daily.Count) : 0
            },
            columns = new[] { "روز", "درآمد (ریال)" },
            rows = daily.Select(d => new { d.label, value = (decimal)d.value }).ToList(),
            chart = new
            {
                type = "area",
                title = "روند درآمد روزانه",
                unit = "ریال",
                points = daily
            }
        }));
    }

    /// <summary>۲) درآمد ۱۲ ماه شمسی اخیر — ماهیت مقایسه ماه‌به‌ماه → ستونی</summary>
    private async Task<IActionResult> ChartRevenueMonthlyAsync()
    {
        var iranToday = (DateTime.UtcNow + IranOffset).Date;
        var jalaliToday = PersianDateHelper.ToJalali(iranToday);
        var firstMonthIndex = jalaliToday.Year * 12 + jalaliToday.Month - 1 - 11;
        var firstYear = firstMonthIndex / 12;
        var firstMonth = firstMonthIndex % 12 + 1;
        var rangeStart = IranDayStartUtc(PersianDateHelper.ToGregorian(firstYear, firstMonth, 1));
        var rangeEnd = IranDayEndUtc(iranToday);

        var invoices = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => i.InvoiceDate >= rangeStart && i.InvoiceDate <= rangeEnd && !i.IsDeleted &&
                        i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Refunded)
            .Select(i => new { i.InvoiceDate, i.TotalAmount })
            .ToListAsync();

        // گروه‌بندی ماه شمسی در حافظه (EF تقویم جلالی را نمی‌فهمد)،
        // سپس ساخت ۱۲ نقطهٔ کامل تا ماه جاری؛ ماه‌های بی‌درآمد هم صفر می‌گیرند.
        var totalsByMonth = invoices
            .Select(i =>
            {
                var jalali = PersianDateHelper.ToJalali(i.InvoiceDate + IranOffset);
                return (Year: jalali.Year, Month: jalali.Month, Amount: (double)i.TotalAmount);
            })
            .GroupBy(x => (x.Year, x.Month))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        var monthly = Enumerable.Range(0, 12)
            .Select(offset =>
            {
                var monthIndex = firstMonthIndex + offset;
                var year = monthIndex / 12;
                var month = monthIndex % 12 + 1;
                return new
                {
                    label = PersianDateHelper.MonthNames[month - 1] + " " + year,
                    value = totalsByMonth.GetValueOrDefault((year, month))
                };
            })
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = (decimal)monthly.Sum(m => m.value),
                months = monthly.Count,
                monthAvg = monthly.Count > 0 ? (decimal)(monthly.Sum(m => m.value) / monthly.Count) : 0
            },
            columns = new[] { "ماه", "درآمد (ریال)" },
            rows = monthly.Select(m => new { m.label, value = (decimal)m.value }).ToList(),
            chart = new
            {
                type = "column",
                title = "درآمد ماهانه (۱۲ ماه اخیر)",
                unit = "ریال",
                points = monthly
            }
        }));
    }

    /// <summary>۳) سهم پزشکان از ویزیت — ماهیت سهم از کل → دونات</summary>
    private async Task<IActionResult> ChartDoctorShareAsync(DateTime from, DateTime to)
    {
        var visits = await _reporting.Set<Domain.Entities.Clinical.Visit>().AsNoTracking()
            .Where(v => v.VisitDate >= from && v.VisitDate <= to && v.Doctor != null)
            .Select(v => new
            {
                Name = v.Doctor!.Employee != null
                    ? v.Doctor.Employee.FirstName + " " + v.Doctor.Employee.LastName
                    : "پزشک #" + v.DoctorId
            })
            .ToListAsync();

        var shares = visits
            .GroupBy(v => v.Name)
            .Select(g => new { label = g.Key, value = (double)g.Count() })
            .OrderByDescending(s => s.value)
            .Take(8)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { doctors = shares.Count, visits = (decimal)visits.Count },
            columns = new[] { "پزشک", "تعداد ویزیت" },
            rows = shares.Select(s => new { doctor = s.label, visits = (int)s.value }).ToList(),
            chart = new
            {
                type = "doughnut",
                title = "سهم پزشکان از ویزیت",
                unit = "ویزیت",
                points = shares
            }
        }));
    }

    /// <summary>۴) ترکیب خدمات از درآمد — ماهیت سهم از کل → دونات</summary>
    private async Task<IActionResult> ChartServiceMixAsync(DateTime from, DateTime to)
    {
        var items = await _reporting.Set<Domain.Entities.Billing.InvoiceItem>().AsNoTracking()
            .Where(it => it.Invoice!.InvoiceDate >= from && it.Invoice!.InvoiceDate <= to
                         && !it.Invoice!.IsDeleted && it.Invoice.Status != InvoiceStatus.Cancelled &&
                         it.Invoice.Status != InvoiceStatus.Draft && it.Invoice.Status != InvoiceStatus.Refunded &&
                         it.Service != null)
            .Select(it => new { Name = it.Service!.ServiceName, Price = it.TotalPrice })
            .ToListAsync();

        var mix = items
            .GroupBy(it => it.Name)
            .Select(g => new { label = g.Key, value = (double)g.Sum(x => x.Price) })
            .OrderByDescending(s => s.value)
            .Take(8)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { services = mix.Count, revenue = (decimal)mix.Sum(m => m.value) },
            columns = new[] { "خدمت", "درآمد (ریال)" },
            rows = mix.Select(m => new { service = m.label, revenue = (decimal)m.value }).ToList(),
            chart = new
            {
                type = "doughnut",
                title = "ترکیب خدمات از درآمد",
                unit = "ریال",
                points = mix
            }
        }));
    }

    /// <summary>۵) الگوی ساعتی مراجعات — ماهیت مقایسه ساعت‌ها → ستونی</summary>
    private async Task<IActionResult> ChartVisitHourlyAsync(DateTime from, DateTime to)
    {
        var appointments = await _reporting.Set<Domain.Entities.Scheduling.Appointment>().AsNoTracking()
            .Where(a => a.AppointmentDate >= IranDateOf(from)
                     && a.AppointmentDate <= IranDateOf(to)
                     && !a.IsDeleted)
            .Select(a => new { a.AppointmentTime })
            .ToListAsync();

        var hourly = appointments
            .GroupBy(a => a.AppointmentTime.Hour)
            .Select(g => new { label = g.Key.ToString("00") + ":00", value = (double)g.Count() })
            .OrderBy(p => p.label)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = (decimal)appointments.Count,
                peak = hourly.OrderByDescending(h => h.value).FirstOrDefault()?.label ?? "—"
            },
            columns = new[] { "ساعت", "تعداد نوبت" },
            rows = hourly.Select(h => new { hour = h.label, count = (int)h.value }).ToList(),
            chart = new
            {
                type = "column",
                title = "الگوی ساعتی مراجعات",
                unit = "نوبت",
                points = hourly
            }
        }));
    }

    /// <summary>۶) الگوی روزهای هفته — مقایسه روزها → ستونی (هفته از شنبه)</summary>
    private async Task<IActionResult> ChartVisitWeekdayAsync(DateTime from, DateTime to)
    {
        var appointments = await _reporting.Set<Domain.Entities.Scheduling.Appointment>().AsNoTracking()
            .Where(a => a.AppointmentDate >= IranDateOf(from)
                     && a.AppointmentDate <= IranDateOf(to)
                     && !a.IsDeleted)
            .Select(a => new { a.AppointmentDate })
            .ToListAsync();

        // ترتیب هفته ایرانی: شنبه تا جمعه
        var order = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday,
            DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var names = new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Saturday] = "شنبه", [DayOfWeek.Sunday] = "یکشنبه", [DayOfWeek.Monday] = "دوشنبه",
            [DayOfWeek.Tuesday] = "سه‌شنبه", [DayOfWeek.Wednesday] = "چهارشنبه",
            [DayOfWeek.Thursday] = "پنجشنبه", [DayOfWeek.Friday] = "جمعه",
        };

        var byDay = appointments
            .GroupBy(a => a.AppointmentDate.DayOfWeek)
            .ToDictionary(g => g.Key, g => (double)g.Count());

        var weekday = order
            .Select(d => new { label = names[d], value = byDay.TryGetValue(d, out var v) ? v : 0 })
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = (decimal)appointments.Count,
                busiest = weekday.OrderByDescending(w => w.value).First().label
            },
            columns = new[] { "روز هفته", "تعداد نوبت" },
            rows = weekday.Select(w => new { day = w.label, count = (int)w.value }).ToList(),
            chart = new
            {
                type = "column",
                title = "الگوی مراجعات در روزهای هفته",
                unit = "نوبت",
                points = weekday
            }
        }));
    }

    /// <summary>۷) سهم روش‌های پرداخت — ماهیت سهم از کل → دونات</summary>
    private async Task<IActionResult> ChartPaymentMethodsAsync(DateTime from, DateTime to)
    {
        var payments = await _reporting.Set<Domain.Entities.Billing.Payment>().AsNoTracking()
            .Where(p => p.PaymentDate >= from && p.PaymentDate <= to && !p.IsDeleted)
            .Select(p => new { p.Method, p.Amount, p.IsReturned })
            .ToListAsync();

        var byMethod = payments
            .GroupBy(p => p.Method)
            .Select(g => new { label = PaymentMethodName(g.Key), value = (double)g.Sum(x => x.IsReturned ? -x.Amount : x.Amount) })
            .OrderByDescending(s => s.value)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { total = (decimal)byMethod.Sum(b => b.value), methods = byMethod.Count },
            columns = new[] { "روش پرداخت", "مبلغ (ریال)" },
            rows = byMethod.Select(b => new { method = b.label, amount = (decimal)b.value }).ToList(),
            chart = new
            {
                type = "doughnut",
                title = "سهم روش‌های پرداخت",
                unit = "ریال",
                points = byMethod
            }
        }));
    }

    /// <summary>۸) توزیع جنسیت بیماران — ماهیت سهم از کل → دونات</summary>
    private async Task<IActionResult> ChartGenderMixAsync()
    {
        var patients = await _reporting.Set<Domain.Entities.People.Patient>().AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new { p.Gender })
            .ToListAsync();

        var byGender = patients
            .GroupBy(p => p.Gender)
            .Select(g => new
            {
                label = g.Key == Domain.Enums.Gender.Male ? "مرد" : "زن",
                value = (double)g.Count()
            })
            .OrderByDescending(s => s.value)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { total = (decimal)patients.Count, male = byGender.FirstOrDefault(b => b.label == "مرد")?.value ?? 0, female = byGender.FirstOrDefault(b => b.label == "زن")?.value ?? 0 },
            columns = new[] { "جنسیت", "تعداد بیمار" },
            rows = byGender.Select(b => new { gender = b.label, count = (int)b.value }).ToList(),
            chart = new
            {
                type = "doughnut",
                title = "توزیع جنسیت بیماران",
                unit = "بیمار",
                points = byGender
            }
        }));
    }

    /// <summary>۹) توزیع سنی بیماران — مقایسه رده‌های سنی → ستونی</summary>
    private async Task<IActionResult> ChartAgeBracketsAsync()
    {
        var patients = await _reporting.Set<Domain.Entities.People.Patient>().AsNoTracking()
            .Where(p => !p.IsDeleted && p.BirthDate != null)
            .Select(p => new { Birth = p.BirthDate!.Value })
            .ToListAsync();

        var brackets = new (int Min, int Max, string Label)[]
        {
            (0, 9, "۰-۹ کودک"), (10, 19, "۱۰-۱۹ نوجوان"), (20, 29, "۲۰-۲۹ جوان"),
            (30, 44, "۳۰-۴۴ میانسال"), (45, 59, "۴۵-۵۹ بزرگسال"), (60, 150, "۶۰+ سالمند"),
        };

        var counts = brackets.ToDictionary(b => b.Label, _ => 0);
        foreach (var p in patients)
        {
            var age = PersianDateHelper.CalculateAge(p.Birth.ToDateTime(TimeOnly.MinValue));
            var bracket = brackets.FirstOrDefault(b => age >= b.Min && age <= b.Max);
            if (bracket.Label is not null) counts[bracket.Label]++;
        }

        var points = brackets.Select(b => new { label = b.Label, value = (double)counts[b.Label] }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { total = (decimal)patients.Count },
            columns = new[] { "رده سنی", "تعداد بیمار" },
            rows = points.Select(p => new { bracket = p.label, count = (int)p.value }).ToList(),
            chart = new
            {
                type = "column",
                title = "توزیع سنی بیماران",
                unit = "بیمار",
                points
            }
        }));
    }

    /// <summary>۱۰) پرمصرف‌ترین خدمات — ماهیت رتبه‌بندی → میله‌ای افقی</summary>
    private async Task<IActionResult> ChartTopServicesAsync(DateTime from, DateTime to, int top)
    {
        var items = await _reporting.Set<Domain.Entities.Billing.InvoiceItem>().AsNoTracking()
            .Where(it => it.Invoice!.InvoiceDate >= from && it.Invoice!.InvoiceDate <= to
                         && !it.Invoice!.IsDeleted && it.Invoice.Status != InvoiceStatus.Cancelled &&
                         it.Invoice.Status != InvoiceStatus.Draft && it.Invoice.Status != InvoiceStatus.Refunded &&
                         it.Service != null)
            .Select(it => new { Name = it.Service!.ServiceName, Price = it.TotalPrice })
            .ToListAsync();

        var ranked = items
            .GroupBy(it => it.Name)
            .Select(g => new { label = g.Key, value = (double)g.Sum(x => x.Price) })
            .OrderByDescending(r => r.value)
            .Take(Math.Clamp(top, 1, 100))
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { services = ranked.Count, revenue = (decimal)ranked.Sum(r => r.value) },
            columns = new[] { "خدمت", "درآمد (ریال)" },
            rows = ranked.Select(r => new { service = r.label, revenue = (decimal)r.value }).ToList(),
            chart = new
            {
                type = "hbar",
                title = "پردرآمدترین خدمات",
                unit = "ریال",
                points = ranked
            }
        }));
    }
}
