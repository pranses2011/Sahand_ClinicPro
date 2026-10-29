using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>درخواست دقیق پرداخت؛ اطلاعات فقط در فایل رمزگذاری‌شده محلی حفظ می‌شوند.</summary>
public sealed record PendingPayment(long InvoiceId, long PatientId, decimal Amount, int Method,
    string? ReferenceNumber, string? CardNumber, Guid RequestId, string Server, long? BranchId);

/// <summary>رزرو پیش از ارسال و نگهداری تا دریافت پاسخ موفق.</summary>
public interface IPaymentRecoveryStore
{
    PendingPayment? Load(string server, long invoiceId);
    void Reserve(PendingPayment payment);
    void Clear(PendingPayment payment);
}

/// <summary>حفاظت با DPAPI کاربر ویندوز و فایل یکتا به ازای هر سرور و فاکتور.</summary>
public sealed class PaymentRecoveryStore : IPaymentRecoveryStore
{
    private readonly string _directory;
    public PaymentRecoveryStore(string? directory = null) => _directory = directory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SahandClinicPro", "pending-payments");

    private string PathFor(string server, long invoiceId)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(server + "\n" + invoiceId)));
        return Path.Combine(_directory, key + ".dat");
    }

    private IDisposable Lock(string server, long invoiceId)
    {
        var hash = Path.GetFileNameWithoutExtension(PathFor(server, invoiceId));
        var mutex = new Mutex(false, @"Global\SahandClinicPro-Payment-" + hash);
        try
        {
            try { if (!mutex.WaitOne(TimeSpan.FromSeconds(10))) throw new IOException("قفل بازیابی پرداخت در دسترس نیست."); }
            catch (AbandonedMutexException) { /* قفل رهاشده اکنون در اختیار ماست. */ }
            return new MutexLease(mutex);
        }
        catch { mutex.Dispose(); throw; }
    }

    private sealed class MutexLease(Mutex mutex) : IDisposable
    {
        public void Dispose() { mutex.ReleaseMutex(); mutex.Dispose(); }
    }

    public PendingPayment? Load(string server, long invoiceId)
    {
        using var guard = Lock(server, invoiceId);
        return LoadUnlocked(server, invoiceId);
    }

    private PendingPayment? LoadUnlocked(string server, long invoiceId)
    {
        var path = PathFor(server, invoiceId);
        if (!File.Exists(path)) return null;
        var plain = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
        var payment = JsonSerializer.Deserialize<PendingPayment>(plain) ?? throw new InvalidDataException("درخواست پرداخت خوانده نشد.");
        if (payment.Server != server || payment.InvoiceId != invoiceId || payment.RequestId == Guid.Empty ||
            payment.PatientId <= 0 || payment.Amount <= 0 || payment.Method is < 1 or > 5)
            throw new InvalidDataException("درخواست پرداخت ذخیره‌شده معتبر نیست.");
        return payment;
    }

    public void Reserve(PendingPayment payment)
    {
        using var guard = Lock(payment.Server, payment.InvoiceId);
        Directory.CreateDirectory(_directory);
        var bytes = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(payment), null, DataProtectionScope.CurrentUser);
        // CreateNew مانع جایگزینی درخواست حل‌نشده در پنجره یا فرایند دیگر می‌شود.
        using var stream = new FileStream(PathFor(payment.Server, payment.InvoiceId), FileMode.CreateNew,
            FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }

    public void Clear(PendingPayment payment)
    {
        using var guard = Lock(payment.Server, payment.InvoiceId);
        var current = LoadUnlocked(payment.Server, payment.InvoiceId);
        if (current is null || current.RequestId != payment.RequestId)
            throw new InvalidDataException("درخواست پرداخت محلی تغییر کرده است.");
        File.Delete(PathFor(payment.Server, payment.InvoiceId));
    }
}
