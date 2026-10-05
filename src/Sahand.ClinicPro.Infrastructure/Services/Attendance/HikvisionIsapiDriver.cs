using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Attendance;

/// <summary>
/// درایور Hikvision با ISAPI — HTTP با احراز هویت Digest
/// مسیر پیش‌فرض گزارش حوادث: /ISAPI/AccessControl/InOutInfo?format=json
/// مسیر قابل بازتنظیم از فیلد Location دستگاه
/// </summary>
public class HikvisionIsapiDriver : IAttendanceDeviceDriver
{
    /// <summary>مسیر ISAPI برای خواندن گزارش حوادث دسترسی</summary>
    private const string DefaultEventsPath = "/ISAPI/AccessControl/InOutInfo?format=json";

    private readonly ILogger<HikvisionIsapiDriver> _logger;

    /// <summary>برند درایور</summary>
    public DeviceBrand Brand => DeviceBrand.Hikvision;

    /// <summary>سازنده با لاگر</summary>
    public HikvisionIsapiDriver(ILogger<HikvisionIsapiDriver> logger) => _logger = logger;

    /// <summary>آزمون اتصال — GET مسیر وضعیت دستگاه</summary>
    public async Task<bool> TestConnectionAsync(AttendanceDevice device, CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient(device);
            using var response = await client.GetAsync(BuildUri(device, "/ISAPI/System/status"), ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "آزمون اتصال Hikvision {Ip} ناموفق", device.IpAddress);
            return false;
        }
    }

    /// <summary>خواندن سریال — ازDeviceInfo</summary>
    public async Task<string?> GetSerialAsync(AttendanceDevice device, CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient(device);
            using var response = await client.GetAsync(BuildUri(device, "/ISAPI/System/deviceInfo?format=json"), ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("deviceInfo", out var info)
                && info.TryGetProperty("serialNumber", out var serial)
                ? serial.GetString()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "خواندن سریال Hikvision {Ip} ناموفق", device.IpAddress);
            return null;
        }
    }

    /// <summary>کشیدن ضربه‌های جدید — گزارش حوادث با فیلتر زمانی</summary>
    public async Task<IReadOnlyList<DevicePunch>> PullNewPunchesAsync(
        AttendanceDevice device, DateTime fromUtc, CancellationToken ct = default)
    {
        var punches = new List<DevicePunch>();
        using var client = CreateClient(device);
        using var response = await client.GetAsync(
            BuildUri(device, device.Location ?? DefaultEventsPath), ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        // ساختار پاسخ: { "AccessController": { "InOutInfo": [ { ... } ] } }
        // ساختار مشابه با تحمل تفاوت نسخه‌های ISAPI
        JsonElement records = default;
        if (doc.RootElement.TryGetProperty("AccessController", out var controller)
            && controller.TryGetProperty("InOutInfo", out var inOut))
        {
            records = inOut;
        }
        else if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            records = doc.RootElement;
        }

        if (records.ValueKind != JsonValueKind.Array)
            return punches;

        foreach (var item in records.EnumerateArray())
        {
            // شناسه کاربر — employeeNo
            if (!item.TryGetProperty("employeeNo", out var empNo))
                continue;
            var enroll = empNo.GetString();
            if (string.IsNullOrWhiteSpace(enroll))
                continue;

            // زمان — ISO با منطقه
            if (!item.TryGetProperty("time", out var timeEl)
                || !DateTime.TryParse(timeEl.GetString(), out var localTime))
                continue;

            var utc = DateTime.SpecifyKind(localTime, DateTimeKind.Utc) > fromUtc
                ? DateTime.SpecifyKind(localTime, DateTimeKind.Utc)
                : default;
            if (utc == default)
                continue;

            // نوع رویداد
            var state = PunchState.Undefined;
            if (item.TryGetProperty("eventType", out var ev)
                && ev.GetString() is { } evName)
            {
                state = evName.Contains("checkIn", StringComparison.OrdinalIgnoreCase)
                    ? PunchState.CheckIn
                    : evName.Contains("checkOut", StringComparison.OrdinalIgnoreCase)
                        ? PunchState.CheckOut
                        : PunchState.Undefined;
            }

            byte verify = 2; // پیش‌فرض کارت
            if (item.TryGetProperty("verifyMode", out var vm) && vm.ValueKind == JsonValueKind.String)
            {
                var mode = vm.GetString();
                if (mode?.Contains("finger", StringComparison.OrdinalIgnoreCase) == true) verify = 1;
                else if (mode?.Contains("face", StringComparison.OrdinalIgnoreCase) == true) verify = 15;
            }

            punches.Add(new DevicePunch(enroll, utc, state, verify));
        }

        _logger.LogInformation("کشیدن {Count} ضربه از Hikvision {Ip}", punches.Count, device.IpAddress);
        return punches;
    }

    /// <summary>پیشانی کاربران — ISAPI UserInfo/records با PUT؛ در این نسخه فقط اتصال</summary>
    public Task<bool> SyncUsersAsync(AttendanceDevice device, IReadOnlyList<DeviceUserMapping> mappings, CancellationToken ct = default)
        => TestConnectionAsync(device, ct);

    /// <summary>ساخت HttpClient با احراز هویت Digest — credential per-device</summary>
    private static HttpClient CreateClient(AttendanceDevice device)
    {
        // CommunicationKey به قالب «user:pass» یا فقط pass
        var user = "admin";
        var pass = device.CommunicationKey ?? "admin";
        if (device.CommunicationKey is { } key && key.Contains(':'))
        {
            var parts = key.Split(':', 2);
            user = parts[0];
            pass = parts[1];
        }

        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential(user, pass),
            PreAuthenticate = true,
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
    }

    /// <summary>ساخت URI کامل</summary>
    private static Uri BuildUri(AttendanceDevice device, string path) =>
        new($"http://{device.IpAddress}:{device.Port}{path}");
}
