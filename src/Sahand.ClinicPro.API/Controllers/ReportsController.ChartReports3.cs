using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.API.Controllers;

// ═══════════════════════════════════════════════════════════════════════
// گزارش‌های هوش تجاری — دسته سوم (چرخه سیزدهم)
// D1: مقایسه دوره‌ای MoM/YoY با نمودار دوقلو | D4: تحلیل RFM بیماران
// D5: پیش‌بینی روند درآمد با رگرسیون خطی
// همان قرارداد chart فاز F2 چرخه یازدهم: روند→area، مقایسه→column،
// سهم→doughnut، رتبه→hbar
// ═══════════════════════════════════════════════════════════════════════
public partial class ReportsController
{
    /// <summary>۲۲) رشد ماه‌به‌ماه (MoM) — ماهیت مقایسه دوره‌ای → ستونی دوقلو (چرخه ۱۳، D1)</summary>
    private async Task<IActionResult> ChartGrowthMomAsync(DateTime from, DateTime to)
    {
        var invoices = await _unitOfWork.Repository<Domain.Entities.Billing.Invoice>().QueryableNoTracking
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && !i.IsDeleted)
            .Select(i => new { i.InvoiceDate, i.TotalAmount })
            .ToListAsync();

        // گروه‌بندی ماه شمسی
        var byMonth = invoices
            .GroupBy(i =>
            {
                var j = PersianDateHelper.ToJalali(i.InvoiceDate);
                return (j.Year, j.Month);
            })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Label = $"{PersianDateHelper.MonthNames[g.Key.Month - 1]} {g.Key.Year}",
                Revenue = (double)g.Sum(x => x.TotalAmount)
            })
            .ToList();

        var rows = new List<object>();
        var points = new List<object>();
        for (var i = 0; i < byMonth.Count; i++)
        {
            var current = byMonth[i];
            var prevRevenue = i > 0 ? byMonth[i - 1].Revenue : (double?)null;
            var growthPercent = prevRevenue is > 0
                ? Math.Round((current.Revenue - prevRevenue.Value) / prevRevenue.Value * 100.0, 1)
                : (double?)null;

            rows.Add(new
            {
                month = current.Label,
                revenue = (decimal)current.Revenue,
                prevRevenue = prevRevenue is null ? (decimal?)null : (decimal)prevRevenue.Value,
                growthPercent,
                direction = growthPercent == null ? "—" : growthPercent >= 0 ? "📈 صعودی" : "📉 نزولی"
            });

            // نمودار دوقلو: ستون این ماه + ستون ماه قبل کنار هم
            points.Add(new
            {
                label = current.Label,
                value = Math.Round(current.Revenue),
                value2 = prevRevenue is null ? 0 : Math.Round(prevRevenue.Value),
                growth = growthPercent
            });
        }

        var lastGrowth = rows.Count > 0
            ? ((dynamic)rows[rows.Count - 1]).growthPercent as double?
            : null;

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                months = byMonth.Count,
                total = (decimal)byMonth.Sum(m => m.Revenue),
                lastMonthGrowth = lastGrowth,
                bestMonth = byMonth.OrderByDescending(m => m.Revenue).FirstOrDefault()?.Label ?? "—",
                avgGrowth = rows.Count(r => ((dynamic)r).growthPercent != null) > 0
                    ? Math.Round(rows.Where(r => ((dynamic)r).growthPercent != null)
                        .Average(r => (double)((dynamic)r).growthPercent), 1)
                    : (double?)null
            },
            columns = new[] { "ماه", "درآمد (ریال)", "ماه قبل", "رشد ٪", "جهت" },
            rows,
            chart = new
            {
                type = "column",
                title = "رشد ماه‌به‌ماه درآمد (MoM)",
                unit = "ریال",
                dual = true,
                points
            }
        }));
    }

    /// <summary>۲۳) رشد سال‌به‌سال (YoY) — مقایسه همان ماهِ سال جاری و قبل (چرخه ۱۳، D1)</summary>
    private async Task<IActionResult> ChartGrowthYoyAsync(DateTime from, DateTime to)
    {
        // بازه را به کل ماه‌های شمسی درون‌داده گسترش می‌دهیم (تا سال قبل هم پوشش داده شود)
        var extendedFrom = from.AddYears(-1);
        var invoices = await _unitOfWork.Repository<Domain.Entities.Billing.Invoice>().QueryableNoTracking
            .Where(i => i.InvoiceDate >= extendedFrom && i.InvoiceDate <= to && !i.IsDeleted)
            .Select(i => new { i.InvoiceDate, i.TotalAmount })
            .ToListAsync();

        var byMonth = invoices
            .Select(i => new
            {
                J = PersianDateHelper.ToJalali(i.InvoiceDate),
                Amount = (double)i.TotalAmount
            })
            .GroupBy(x => (x.J.Year, x.J.Month))
            .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(x => x.Amount) })
            .ToList();

        var years = byMonth.Select(m => m.Year).Distinct().OrderBy(y => y).ToList();
        var currentYear = years.Count > 0 ? years.Max() : PersianDateHelper.ToJalali(DateTime.UtcNow).Year;

        var rows = new List<object>();
        var points = new List<object>();
        for (var month = 1; month <= 12; month++)
        {
            var current = byMonth.FirstOrDefault(m => m.Year == currentYear && m.Month == month)?.Revenue ?? 0;
            var previous = byMonth.FirstOrDefault(m => m.Year == currentYear - 1 && m.Month == month)?.Revenue ?? 0;
            var growth = previous > 0 ? Math.Round((current - previous) / previous * 100.0, 1) : (double?)null;

            if (current == 0 && previous == 0) continue;

            rows.Add(new
            {
                month = PersianDateHelper.MonthNames[month - 1],
                thisYear = (decimal)current,
                lastYear = (decimal)previous,
                growthPercent = growth,
                direction = growth == null ? "—" : growth >= 0 ? "📈 صعودی" : "📉 نزولی"
            });
            points.Add(new
            {
                label = PersianDateHelper.MonthNames[month - 1],
                value = Math.Round(current),
                value2 = Math.Round(previous),
                growth
            });
        }

        var totalThis = byMonth.Where(m => m.Year == currentYear).Sum(m => m.Revenue);
        var totalLast = byMonth.Where(m => m.Year == currentYear - 1).Sum(m => m.Revenue);

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                currentYear,
                totalThisYear = (decimal)totalThis,
                totalLastYear = (decimal)totalLast,
                yoyGrowth = totalLast > 0
                    ? Math.Round((totalThis - totalLast) / totalLast * 100.0, 1)
                    : (double?)null
            },
            columns = new[] { "ماه", $"سال {currentYear}", $"سال {currentYear - 1}", "رشد ٪", "جهت" },
            rows,
            chart = new
            {
                type = "column",
                title = $"مقایسه سال‌به‌سال — {currentYear} در برابر {currentYear - 1} (YoY)",
                unit = "ریال",
                dual = true,
                points
            }
        }));
    }

    /// <summary>۲۴) تحلیل RFM بیماران — رده‌بندی ۸گانه برای پیامک هدفمند (چرخه ۱۳، D4)</summary>
    private async Task<IActionResult> ChartRfmAnalysisAsync(DateTime from, DateTime to)
    {
        var invoices = await _unitOfWork.Repository<Domain.Entities.Billing.Invoice>().QueryableNoTracking
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && !i.IsDeleted)
            .Select(i => new { i.PatientId, i.InvoiceDate, i.TotalAmount })
            .ToListAsync();

        if (invoices.Count == 0)
            return Ok(ApiResponse<object>.Success(new { available = false, message = "در این بازه فاکتوری ثبت نشده است" }));

        var now = DateTime.UtcNow;
        var perPatient = invoices
            .GroupBy(i => i.PatientId)
            .Select(g => new
            {
                PatientId = g.Key,
                RecencyDays = (int)(now - g.Max(x => x.InvoiceDate)).TotalDays,
                Frequency = g.Count(),
                Monetary = (double)g.Sum(x => x.TotalAmount)
            })
            .ToList();

        // امتیاز ۱-۵ هر بُعد با پنجک‌ها
        var recencySorted = perPatient.Select(p => p.RecencyDays).OrderBy(x => x).ToList();
        var freqSorted = perPatient.Select(p => p.Frequency).OrderBy(x => x).ToList();
        var monSorted = perPatient.Select(p => p.Monetary).OrderBy(x => x).ToList();

        var scored = perPatient.Select(p => new
        {
            p.PatientId,
            p.RecencyDays,
            p.Frequency,
            p.Monetary,
            // R: روزِ کمتر = مراجعه تازه‌تر = امتیاز بالاتر → وارونه پنجک
            R = 6 - Quintile(recencySorted, p.RecencyDays),
            F = Quintile(freqSorted, p.Frequency),
            M = Quintile(monSorted, p.Monetary)
        }).ToList();

        var patients = await _unitOfWork.Repository<Domain.Entities.People.Patient>().QueryableNoTracking
            .Where(p => scored.Select(s => s.PatientId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => $"{p.FirstName} {p.LastName}".Trim());

        // رده‌بندی ۸گانه RFM
        var segments = new (string Key, string Label, string Color, Func<dynamic, bool> Match)[]
        {
            ("champions", "🏆 قهرمانان", "#2E7D32", s => s.R >= 4 && s.F >= 4 && s.M >= 4),
            ("loyal", "💚 وفادار", "#43A047", s => s.R >= 3 && s.F >= 4),
            ("potential", "🌟 بالقوه", "#1E88E5", s => s.R >= 4 && s.F >= 2 && s.F < 4),
            ("new", "🌱 تازه‌وارد", "#00ACC1", s => s.R >= 4 && s.F <= 1),
            ("atrisk", "⚠️ در معرض خطر", "#FB8C00", s => s.R is >= 2 and <= 3 && s.F >= 3),
            ("losing", "📉 در حال از دست دادن", "#F4511E", s => s.R >= 2 && s.R < 4 && s.F <= 2),
            ("hibernating", "💤 بیهوش", "#8E24AA", s => s.R <= 2 && s.F <= 2),
            ("lost", "❌ از دست رفته", "#C62828", s => s.R <= 2 && s.F >= 3 && s.M >= 3)
        };

        var segmentCounts = segments
            .Select(seg => new
            {
                label = seg.Label,
                value = (double)scored.Count(s => seg.Match(s))
            })
            .ToList();
        // باقی‌مانده — دسته‌بندی نشده (پیش‌فرض «عادی»)
        var assigned = segmentCounts.Sum(s => s.value);
        segmentCounts.Add(new { label = "⚪ سایر", value = (double)(scored.Count - assigned) });

        var topChampions = scored
            .Where(s => s.R >= 4 && s.F >= 4)
            .OrderByDescending(s => s.Monetary)
            .Take(15)
            .Select(s => new
            {
                patient = patients.GetValueOrDefault(s.PatientId, "بیمار #" + s.PatientId),
                recencyDays = s.RecencyDays,
                frequency = s.Frequency,
                monetary = (decimal)s.Monetary,
                score = $"{s.R}{s.F}{s.M}"
            }).ToList();

        var atRisk = scored
            .Where(s => s.R <= 3 && s.F >= 3)
            .OrderByDescending(s => s.Monetary)
            .Take(15)
            .Select(s => new
            {
                patient = patients.GetValueOrDefault(s.PatientId, "بیمار #" + s.PatientId),
                recencyDays = s.RecencyDays,
                frequency = s.Frequency,
                monetary = (decimal)s.Monetary,
                score = $"{s.R}{s.F}{s.M}"
            }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                patients = scored.Count,
                champions = scored.Count(s => s.R >= 4 && s.F >= 4 && s.M >= 4),
                atRisk = scored.Count(s => s.R <= 3 && s.F >= 3),
                totalRevenue = (decimal)scored.Sum(s => s.Monetary),
                hint = "رده «قهرمانان» برای کمپین وفادارسازی و «در معرض خطر» برای پیامک بازگشت مناسب‌اند — از دکمه ارسال پیامک در ماژول پیامک استفاده کنید"
            },
            columns = new[] { "رده", "تعداد بیمار" },
            rows = segmentCounts.Select(s => new { segment = s.label, count = (int)s.value }).ToList(),
            champions = topChampions,
            atRisk,
            chart = new
            {
                type = "doughnut",
                title = "ترکیب رده‌های RFM بیماران",
                unit = "بیمار",
                points = segmentCounts
            }
        }));
    }

    /// <summary>۲۵) پیش‌بینی روند درآمد — رگرسیون خطی ۶ ماه اخیر + ماه بعد (چرخه ۱۳، D5)</summary>
    private async Task<IActionResult> ChartRevenueForecastAsync(DateTime from, DateTime to)
    {
        // ماه‌های ۶گانه اخیر — مستقل از بازه انتخابی؛ بازه فقط برای خوانایی رابط کاربری است
        var now = DateTime.UtcNow;
        var start = now.AddMonths(-6);
        var invoices = await _unitOfWork.Repository<Domain.Entities.Billing.Invoice>().QueryableNoTracking
            .Where(i => i.InvoiceDate >= start && i.InvoiceDate <= now && !i.IsDeleted)
            .Select(i => new { i.InvoiceDate, i.TotalAmount })
            .ToListAsync();

        var monthly = invoices
            .Select(i => new { J = PersianDateHelper.ToJalali(i.InvoiceDate), Amount = (double)i.TotalAmount })
            .GroupBy(x => (x.J.Year, x.J.Month))
            .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Revenue = g.Sum(x => x.Amount) })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        if (monthly.Count < 2)
            return Ok(ApiResponse<object>.Success(new
            { available = false, message = "برای پیش‌بینی حداقل ۲ ماه داده لازم است" }));

        // رگرسیون خطی حداقل مربعات: y = a + b·x
        var n = (double)monthly.Count;
        var xs = Enumerable.Range(0, monthly.Count).Select(x => (double)x).ToList();
        var ys = monthly.Select(m => m.Revenue).ToList();
        var meanX = xs.Average();
        var meanY = ys.Average();
        var b = xs.Select((x, i) => (x - meanX) * (ys[i] - meanY)).Sum()
                / (xs.Select(x => Math.Pow(x - meanX, 2)).Sum() + 1e-10);
        var a = meanY - b * meanX;

        // پیش‌بینی ماه بعد + بازه اطمینان ۸۰٪ (±۱.۲۸σ از باقیمانده‌ها)
        var residuals = ys.Select((y, i) => y - (a + b * xs[i])).ToList();
        var sigma = Math.Sqrt(residuals.Sum(r => r * r) / Math.Max(1, n - 2));
        var nextIndex = monthly.Count;
        var forecast = a + b * nextIndex;
        if (forecast < 0) forecast = 0;
        var lower = Math.Max(0, forecast - 1.28 * sigma);
        var upper = forecast + 1.28 * sigma;

        var nextJalali = PersianDateHelper.ToJalali(now.AddMonths(1));
        var nextLabel = $"{PersianDateHelper.MonthNames[nextJalali.Month - 1]} {nextJalali.Year}";

        var points = monthly.Select((m, i) => new
        {
            label = $"{PersianDateHelper.MonthNames[m.Month - 1]} {m.Year}",
            value = Math.Round(m.Revenue),
            forecast = false
        }).ToList();
        points.Add(new { label = nextLabel, value = Math.Round(forecast), forecast = true });

        var firstHalf = ys.Take(ys.Count / 2).DefaultIfEmpty(0).Average();
        var secondHalf = ys.Skip(ys.Count / 2).DefaultIfEmpty(0).Average();
        var trendPercent = firstHalf > 0 ? Math.Round((secondHalf - firstHalf) / firstHalf * 100.0, 1) : (double?)null;

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                monthsAnalyzed = monthly.Count,
                forecastNextMonth = (decimal)Math.Round(forecast),
                confidenceLow = (decimal)Math.Round(lower),
                confidenceHigh = (decimal)Math.Round(upper),
                trend = trendPercent,
                trendText = trendPercent == null ? "—" : trendPercent >= 0 ? $"روند صعودی {trendPercent}٪" : $"روند نزولی {Math.Abs(trendPercent ?? 0)}٪",
                method = "رگرسیون خطی حداقل مربعات روی ۶ ماه اخیر + بازه اطمینان ۸۰٪"
            },
            columns = new[] { "ماه", "درآمد (ریال)", "نوع" },
            rows = points.Select(p => new
            {
                month = p.label,
                revenue = (decimal)p.value,
                kind = p.forecast ? "پیش‌بینی" : "واقعی"
            }).ToList(),
            chart = new
            {
                type = "area",
                title = "روند درآمد و پیش‌بینی ماه بعد",
                unit = "ریال",
                points,
                forecastPoint = points.Count - 1,
                confidence = new { low = Math.Round(lower), high = Math.Round(upper) }
            }
        }));
    }

    /// <summary>رتبه پنجکی ۱-۵ — مقدار کوچک‌تر/بزرگ‌تر بسته به صعودی بودن مرتب‌سازی</summary>
    private static int Quintile<T>(List<T> sorted, T value) where T : IComparable<T>
    {
        var idx = sorted.BinarySearch(value);
        if (idx < 0) idx = ~idx;
        var quintile = (int)Math.Floor(idx * 5.0 / sorted.Count) + 1;
        return Math.Clamp(quintile, 1, 5);
    }
}
