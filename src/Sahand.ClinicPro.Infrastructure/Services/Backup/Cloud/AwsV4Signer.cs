using System.Security.Cryptography;
using System.Text;

namespace Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud;

/// <summary>
/// امضاساز AWS Signature Version 4 برای سرویس‌های ذخیره‌سازی سازگار با S3
/// (AWS S3، آروان کلود، ابستور، مین‌آی‌او و…) — چرخه سیزدهم (درخواست ۷-۳).
/// پیاده‌سازی خالص بدون وابستگی به AWS SDK — فقط PUT/GET روی bucket قالب path-style.
/// مرجع الگوریتم: مستندات رسمی AWS «Signature Version 4 — S3».
/// </summary>
public static class AwsV4Signer
{
    /// <summary>ساخت هدر Authorization امضاشده برای یک درخواست S3</summary>
    /// <param name="method">متد HTTP — PUT/GET/HEAD/DELETE</param>
    /// <param name="uri">آدرس کامل شامل اسکیم/هاست/بکت/کلید — بدون رشته پرس‌وجو</param>
    /// <param name="query">رشته پرس‌وجو کدشده یا تهی</param>
    /// <param name="payloadHash">هش SHA-256 بدنه (هش رشته خالی برای بدنه بدون داده)</param>
    /// <param name="accessKey">کلید دسترسی</param>
    /// <param name="secretKey">کلید مخفی</param>
    /// <param name="region">ناحیه — برای سرویس‌های سازگار معمولاً us-east-1</param>
    /// <param name="now">زمان امضا — UTC</param>
    /// <returns>مقدار هدر Authorization</returns>
    public static string Sign(HttpMethod method, Uri uri, string? query, string payloadHash,
        string accessKey, string secretKey, string region, DateTime now)
    {
        var host = uri.Authority;
        var canonicalUri = uri.AbsolutePath.TrimStart('/');

        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

        // درخواست استاندارد: متد\nمسیر\nپرس‌وجو\nهدرها\nهدرهای‌امضاشده\nهش بدنه
        var canonicalHeaders =
            $"host:{host}\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{amzDate}\n";
        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        var canonicalRequest = string.Join('\n',
            method.Method, "/" + canonicalUri, query ?? "", canonicalHeaders, signedHeaders, payloadHash);

        var scope = $"{dateStamp}/{region}/s3/aws4_request";
        var stringToSign = string.Join('\n',
            "AWS4-HMAC-SHA256", amzDate, scope, Sha256Hex(Encoding.UTF8.GetBytes(canonicalRequest)));

        var signingKey = Hmac(Hmac(Hmac(Hmac(
            Encoding.UTF8.GetBytes($"AWS4{secretKey}"), Encoding.UTF8.GetBytes(dateStamp)),
            Encoding.UTF8.GetBytes(region)),
            Encoding.UTF8.GetBytes("s3")),
            Encoding.UTF8.GetBytes("aws4_request"));

        var signature = Hex(Hmac(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        return $"AWS4-HMAC-SHA256 Credential={accessKey}/{scope}, " +
               $"SignedHeaders={signedHeaders}, Signature={signature}";
    }

    /// <summary>هش SHA-256 یک بدنه به‌صورت هگز کوچک</summary>
    public static string Sha256Hex(byte[] payload)
    {
        var hash = SHA256.HashData(payload);
        return Hex(hash);
    }

    /// <summary>هش رشته خالی — برای درخواست‌های GET/HEAD</summary>
    public static string EmptyPayloadHash => Sha256Hex(Array.Empty<byte>());

    /// <summary>HMAC-SHA256</summary>
    private static byte[] Hmac(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    /// <summary>بایت‌ها به هگز کوچک</summary>
    private static string Hex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
