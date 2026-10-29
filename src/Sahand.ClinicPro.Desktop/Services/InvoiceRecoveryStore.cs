using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.CreateInvoice;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>درخواست و پیش‌نمایش بازبینی‌شده برای بازیابی صدور.</summary>
public sealed record PendingInvoice(CreateInvoiceDto Request, InvoicePreviewDto Quote, string Server, long? BranchId);

public interface IInvoiceRecoveryStore
{
    PendingInvoice? Load(string server, long patientId);
    void Reserve(PendingInvoice pending);
    void Clear(PendingInvoice pending);
}

/// <summary>یک درخواست حل‌نشده به ازای هر بیمار و سرور در حساب ویندوز.</summary>
public sealed class InvoiceRecoveryStore : IInvoiceRecoveryStore
{
    private readonly string _directory;
    public InvoiceRecoveryStore(string? directory = null) => _directory = directory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SahandClinicPro", "pending-invoices");

    private string PathFor(string server, long patientId)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(server + "\n" + patientId)));
        return Path.Combine(_directory, key + ".dat");
    }

    private IDisposable Lock(string server, long patientId)
    {
        var name = Path.GetFileNameWithoutExtension(PathFor(server, patientId));
        var mutex = new Mutex(false, @"Global\SahandClinicPro-Invoice-" + name);
        try
        {
            try { if (!mutex.WaitOne(TimeSpan.FromSeconds(10))) throw new IOException("قفل بازیابی فاکتور در دسترس نیست."); }
            catch (AbandonedMutexException) { }
            return new MutexLease(mutex);
        }
        catch { mutex.Dispose(); throw; }
    }
    private sealed class MutexLease(Mutex mutex) : IDisposable
    {
        public void Dispose() { mutex.ReleaseMutex(); mutex.Dispose(); }
    }

    public PendingInvoice? Load(string server, long patientId)
    {
        using var guard = Lock(server, patientId);
        return LoadUnlocked(server, patientId);
    }

    private PendingInvoice? LoadUnlocked(string server, long patientId)
    {
        byte[] encrypted;
        try { encrypted = File.ReadAllBytes(PathFor(server, patientId)); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        var pending = JsonSerializer.Deserialize<PendingInvoice>(bytes) ?? throw new InvalidDataException("درخواست فاکتور خوانده نشد.");
        if (pending.Server != server || pending.Request is null || pending.Quote is null || pending.Request.PatientId != patientId ||
            pending.Request.RequestId is null || pending.Request.RequestId == Guid.Empty || !InvoiceAmountSafety.Valid(pending.Request) ||
            string.IsNullOrWhiteSpace(pending.Quote.QuoteVersion) || pending.Request.ExpectedQuote != pending.Quote.QuoteVersion)
            throw new InvalidDataException("درخواست فاکتور ذخیره‌شده معتبر نیست.");
        return pending;
    }

    public void Reserve(PendingInvoice pending)
    {
        using var guard = Lock(pending.Server, pending.Request.PatientId);
        Directory.CreateDirectory(_directory);
        var bytes = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(pending), null, DataProtectionScope.CurrentUser);
        using var stream = new FileStream(PathFor(pending.Server, pending.Request.PatientId), FileMode.CreateNew,
            FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }

    public void Clear(PendingInvoice pending)
    {
        using var guard = Lock(pending.Server, pending.Request.PatientId);
        var current = LoadUnlocked(pending.Server, pending.Request.PatientId);
        if (current is null || current.Request.RequestId != pending.Request.RequestId)
            throw new InvalidDataException("درخواست فاکتور محلی تغییر کرده است.");
        File.Delete(PathFor(pending.Server, pending.Request.PatientId));
    }
}
