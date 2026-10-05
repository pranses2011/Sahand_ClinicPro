using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست سازگاری امضای لایسنس بین سرور PHP (openssl_sign با SHA256/PKCS1)
/// و اعتبارسنج سمت دات‌نت (RSA.VerifyData)
/// بردارهای آزمون با اسکریپت gen_license_compat_vector.py تولید شده‌اند
/// (جفت‌کلید RSA-2048 مستقل — فقط برای اثبات هم‌ارزی الگوریتم)
/// </summary>
public class LicenseSignatureCompatibilityTests
{
    // ── بردار آزمون — لایسنس اشتراکی با انقضای ۲۰۲۷-۱۰-۰۵ ──
    private const string LicenseKey = "SCPRO-TEST1-TEST2-TEST3";
    private const string Expiry = "2027-10-05";
    private const int MaxUsers = 5;

    private const string SignatureBase64 =
        "i6KWJ9SvAbqzbxUFjYz3WF/HiE6nsVsw9Uw+82pF+VzrjjZbLelhjw6fsmOlFh8Xcvm4XK4rpZg327p6xWU/eGNC94mcJR5gzMXVUgpoGlc4fGAfXiR+19ZJrFUp8BabWh+DKFTORkdvsR/ch4l0Xn95eYp6uhvZGdtLKgBuQrdM//Y3XDY0dsn+eFMmiMhfobRTZUHwH8ei8pGHleLJMBDwRyxeboX2SzENCp5H9t7+fHzsbcieEj8HDrHM0HbBee1ThqAkh7ak/wuxW43R4/0D9tHoG+E6Deze7jLxz15MTjxteMa7Bf4CL6lPe2nUUNyLw3/34i4NdhByjhw+Tw==";

    // ── امضای لایسنس دائمی — payload با انقضای تهی: «کلید||حداکثر» ──
    private const string SignaturePerpetualBase64 =
        "q7gj73rAVPlN3+Om1U6MWPiQX/VKwUEQFiTjVBGkX8V0HbsIrDHqcT87gjKeQPhmdqxv+kbmZugmsTDX0dO+M+tduQ15Mg1X1Ms9BtT6BjYDBx9zl/R3MZ2tlfvqui1wwMK8FZnpYjSWTQlwRfBntcw8tSEpBQoEq2IRhILtr1o7DzLDu+R8OQYwHENSNprSJDglrjNCHyb1TdiWa6mHb+k48RpxbOgMuxzCjFi07yziIYjcLh0HfUc8bt8ylS6mzjx4Q52h8atV6XPrDsgei7StY2Ul7IaZi1f9tVic7opk0g0WnbZODhVXr5kJD7/olzi6Yq7xCcPfPkDSMteKeg==";

    // ── کلید عمومی قالب XML — همان خروجی ls_public_key_xml در PHP ──
    private const string PublicKeyXml =
        "<RSAKeyValue><Modulus>s0NeRJPv96GSmZcTeqwLC70+kYiTh9zQ3AJceF+QMYGcpgxV6fIJjpUhnPLQw1Tcv5xyU0o+3dVGds5bzXg+m6cQVRUatgRzsnSLquFKZktGrmXJ7D2R5XLlBRujvyXEIBDp4n07XpRBJ67BpnYMeX4oCuW612RT9inNpqqCLydWqnzxMNloCwoXnfNFjoo84qx6AW0C9mGBK1nUv0xeggVaNYmXc674Q/4N7c2vsd4tTbFQnli974NzKjjvEpW9J28vFWlbq4Q4EfkQVv8Xi7pBe9pxtn9gqyXtKlg87opCMbHtCiI1i1ECdkG7r0NMStEg8E1ijMRL3pLYpx2n7Q==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    // ── همان کلید عمومی در قالب PEM (برای ImportFromPem) ──
    private const string PublicKeyPem =
        "-----BEGIN PUBLIC KEY-----\n"
        + "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAs0NeRJPv96GSmZcTeqwL\n"
        + "C70+kYiTh9zQ3AJceF+QMYGcpgxV6fIJjpUhnPLQw1Tcv5xyU0o+3dVGds5bzXg+\n"
        + "m6cQVRUatgRzsnSLquFKZktGrmXJ7D2R5XLlBRujvyXEIBDp4n07XpRBJ67BpnYM\n"
        + "eX4oCuW612RT9inNpqqCLydWqnzxMNloCwoXnfNFjoo84qx6AW0C9mGBK1nUv0xe\n"
        + "ggVaNYmXc674Q/4N7c2vsd4tTbFQnli974NzKjjvEpW9J28vFWlbq4Q4EfkQVv8X\n"
        + "i7pBe9pxtn9gqyXtKlg87opCMbHtCiI1i1ECdkG7r0NMStEg8E1ijMRL3pLYpx2n\n"
        + "7QIDAQAB\n"
        + "-----END PUBLIC KEY-----";

    /// <summary>امضای تولیدشده با openssl_sign باید با VerifyData دات‌نت تأیید شود (قالب XML)</summary>
    [Fact]
    public void Openssl_signature_verifies_with_dotnet_rsa_xml()
    {
        var payload = Encoding.UTF8.GetBytes($"{LicenseKey}|{Expiry}|{MaxUsers}");
        var signature = Convert.FromBase64String(SignatureBase64);

        using var rsa = RSA.Create();
        rsa.FromXmlString(PublicKeyXml);

        Assert.True(rsa.VerifyData(payload, signature,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    /// <summary>همان امضا با کلید قالب PEM هم باید تأیید شود</summary>
    [Fact]
    public void Openssl_signature_verifies_with_dotnet_rsa_pem()
    {
        var payload = Encoding.UTF8.GetBytes($"{LicenseKey}|{Expiry}|{MaxUsers}");
        var signature = Convert.FromBase64String(SignatureBase64);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(PublicKeyPem);

        Assert.True(rsa.VerifyData(payload, signature,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    /// <summary>لایسنس دائمی — انقضای تهی در payload به صورت رشته خالی می‌آید</summary>
    [Fact]
    public void Perpetual_license_with_empty_expiry_verifies()
    {
        var payload = Encoding.UTF8.GetBytes($"{LicenseKey}||{MaxUsers}");
        var signature = Convert.FromBase64String(SignaturePerpetualBase64);

        using var rsa = RSA.Create();
        rsa.FromXmlString(PublicKeyXml);

        Assert.True(rsa.VerifyData(payload, signature,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    /// <summary>دستکاری payload باید رد شود — تغییر حتی یک رقم</summary>
    [Fact]
    public void Tampered_payload_is_rejected()
    {
        var payload = Encoding.UTF8.GetBytes($"{LicenseKey}|{Expiry}|{MaxUsers + 1}");
        var signature = Convert.FromBase64String(SignatureBase64);

        using var rsa = RSA.Create();
        rsa.FromXmlString(PublicKeyXml);

        Assert.False(rsa.VerifyData(payload, signature,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }
}
