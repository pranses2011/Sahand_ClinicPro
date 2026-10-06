using System.Net.Http.Headers;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud;

/// <summary>
/// ذخیره‌سازی ابری سازگار با S3 (آروان/ابستور/AWS/مین‌آی‌او) با REST امضادار V4 —
/// چرخه سیزدهم (درخواست ۷-۳). آدرس‌دهی قالب path-style:
/// {Endpoint}/{Bucket}/{Key} — سازگار با ارائه‌دهنده‌های ایرانی.
/// </summary>
public class S3CompatibleCloudStorage : ICloudBackupStorage
{
    private readonly HttpClient _http;
    private readonly string _endpoint;
    private readonly string _bucket;
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _region;
    private readonly string _prefix;
    private readonly ILogger? _logger;

    /// <summary>سازنده با پیکربندی اتصال</summary>
    public S3CompatibleCloudStorage(HttpClient http, string endpoint, string bucket,
        string accessKey, string secretKey, string region = "us-east-1", string prefix = "backups/",
        ILogger<S3CompatibleCloudStorage>? logger = null)
    {
        _http = http;
        _endpoint = endpoint.Trim().TrimEnd('/');
        _bucket = bucket.Trim().Trim('/');
        _accessKey = accessKey;
        _secretKey = secretKey;
        _region = string.IsNullOrWhiteSpace(region) ? "us-east-1" : region;
        _prefix = string.IsNullOrWhiteSpace(prefix) ? "backups/" : prefix.TrimStart('/');
        if (!_prefix.EndsWith('/')) _prefix += "/";
        _logger = logger;
    }

    /// <summary>آیا پیکربندی کامل است؟</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_endpoint) &&
        !string.IsNullOrWhiteSpace(_bucket) &&
        !string.IsNullOrWhiteSpace(_accessKey) &&
        !string.IsNullOrWhiteSpace(_secretKey);

    /// <summary>نام ارائه‌دهنده</summary>
    public string ProviderName => "S3-سازگار";

    /// <summary>بارگذاری فایل — PUT امضاشده</summary>
    public async Task<CloudObjectInfo> UploadAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        EnsureConfigured();
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var payload = ms.ToArray();

        var key = _prefix + fileName;
        var (request, _) = BuildSignedRequest(HttpMethod.Put, key, null, payload);
        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"بارگذاری ابری ناموفق بود ({(int)response.StatusCode}) — {Truncate(body)}");
        }

        return new CloudObjectInfo(key, payload.Length, DateTime.UtcNow);
    }

    /// <summary>دانلود فایل — GET امضاشده</summary>
    public async Task<Stream> DownloadAsync(string fileName, CancellationToken ct = default)
    {
        EnsureConfigured();
        var key = _prefix + fileName;
        var (request, _) = BuildSignedRequest(HttpMethod.Get, key, null, null);
        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"دانلود از ابر ناموفق بود ({(int)response.StatusCode})");
        return await response.Content.ReadAsStreamAsync(ct);
    }

    /// <summary>فهرست فایل‌های پوشه پشتیبان — GET ?list-type=2</summary>
    public async Task<IReadOnlyList<CloudObjectInfo>> ListAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        var query = $"list-type=2&prefix={Uri.EscapeDataString(_prefix)}&max-keys=100";
        var (request, encodedQuery) = BuildSignedRequest(HttpMethod.Get, "", query, null);
        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"فهرست‌گیری ابری ناموفق بود ({(int)response.StatusCode}) — {Truncate(body)}");
        }

        var xml = await response.Content.ReadAsStringAsync(ct);
        var doc = XDocument.Parse(xml);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var items = doc.Descendants(ns + "Contents")
            .Select(e => new CloudObjectInfo(
                e.Element(ns + "Key")?.Value ?? "",
                long.TryParse(e.Element(ns + "Size")?.Value, out var s) ? s : 0,
                DateTime.TryParse(e.Element(ns + "LastModified")?.Value,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal, out var d)
                    ? d.ToLocalTime() : DateTime.MinValue))
            .ToList();
        return items;
    }

    /// <summary>تست اتصال — فهرست‌گیری با یک کلید</summary>
    public async Task<(bool Ok, string Message)> TestConnectionAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
            return (false, "پیکربندی ابری ناقص است — Endpoint/Bucket/کلیدها را کامل کنید");

        try
        {
            var query = $"list-type=2&prefix={Uri.EscapeDataString(_prefix)}&max-keys=1";
            var (request, _) = BuildSignedRequest(HttpMethod.Get, "", query, null);
            var response = await _http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return (true, $"اتصال به «{_endpoint}/{_bucket}» برقرار است");

            var body = await response.Content.ReadAsStringAsync(ct);
            return (false, $"اتصال ناموفق ({(int)response.StatusCode}) — {Truncate(body)}");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "تست اتصال ابری ناموفق");
            return (false, $"اتصال به سرور ابر ممکن نشد — {ex.Message}");
        }
    }

    /// <summary>ساخت درخواست امضاشده</summary>
    private (HttpRequestMessage Request, string EncodedQuery) BuildSignedRequest(
        HttpMethod method, string key, string? query, byte[]? payload)
    {
        var payloadHash = payload is null
            ? AwsV4Signer.EmptyPayloadHash
            : AwsV4Signer.Sha256Hex(payload);

        var pathAndQuery = "/" + _bucket + "/" + key;
        if (!string.IsNullOrEmpty(query)) pathAndQuery += "?" + query;
        var uri = new Uri(_endpoint + pathAndQuery);

        var now = DateTime.UtcNow;
        var authorization = AwsV4Signer.Sign(method, uri, query, payloadHash,
            _accessKey, _secretKey, _region, now);

        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("AWS4-HMAC-SHA256",
            authorization["AWS4-HMAC-SHA256 ".Length..]);
        request.Headers.Add("x-amz-date",
            now.ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.Add("x-amz-content-sha256", payloadHash);
        if (payload is not null)
            request.Content = new ByteArrayContent(payload);
        return (request, query ?? "");
    }

    /// <summary>اطمینان از پیکربندی کامل</summary>
    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException("پیکربندی ذخیره‌سازی ابری ناقص است");
    }

    /// <summary>کوتاه‌سازی پیام خطا</summary>
    private static string Truncate(string message, int max = 200)
        => string.IsNullOrEmpty(message) ? "" : (message.Length <= max ? message : message[..max] + "…");
}
