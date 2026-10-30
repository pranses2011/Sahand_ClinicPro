using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sahand.ClinicPro.Application.DTOs.Receptions;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>درخواست اولیه پذیرش و اطلاعات کافی برای بازبینی کاربر.</summary>
public sealed record PendingReception(CreateReceptionDto Request, string PatientName, string TargetName, string Server, long? BranchId, string InsuranceName = "نامشخص");

public interface IReceptionRecoveryStore
{
    PendingReception? Load(string server, long patientId);
    void Reserve(PendingReception pending);
    void Clear(PendingReception pending);
    IDisposable AcquireWorkflow(string server, long patientId);
}

/// <summary>رزرو پایدار پذیرش با رمزگذاری حساب ویندوز و قفل بین فرایندها.</summary>
public sealed class ReceptionRecoveryStore : IReceptionRecoveryStore
{
    private readonly string _directory;
    public ReceptionRecoveryStore(string? directory = null) => _directory = directory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SahandClinicPro", "pending-receptions");

    private string PathFor(string server, long patientId)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(server + "\n" + patientId)));
        return Path.Combine(_directory, key + ".dat");
    }

    /// <summary>تا پایان زنجیره یا بازیابی نگه داشته می‌شود؛ پس از قطع فرایند سیستم عامل آزادش می‌کند.</summary>
    public IDisposable AcquireWorkflow(string server, long patientId)
    {
        Directory.CreateDirectory(_directory);
        return new FileStream(PathFor(server, patientId) + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    private IDisposable Lock(string server, long patientId)
    {
        var name = Path.GetFileNameWithoutExtension(PathFor(server, patientId));
        var mutex = new Mutex(false, @"Global\SahandClinicPro-Reception-" + name);
        try
        {
            try { if (!mutex.WaitOne(TimeSpan.FromSeconds(10))) throw new IOException("قفل بازیابی پذیرش در دسترس نیست."); }
            catch (AbandonedMutexException) { }
            return new MutexLease(mutex);
        }
        catch { mutex.Dispose(); throw; }
    }
    private sealed class MutexLease(Mutex mutex) : IDisposable
    {
        public void Dispose() { mutex.ReleaseMutex(); mutex.Dispose(); }
    }

    public PendingReception? Load(string server, long patientId)
    {
        using var guard = Lock(server, patientId);
        return LoadUnlocked(server, patientId);
    }
    private PendingReception? LoadUnlocked(string server, long patientId)
    {
        byte[] encrypted;
        try { encrypted = File.ReadAllBytes(PathFor(server, patientId)); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        var pending = JsonSerializer.Deserialize<PendingReception>(bytes) ?? throw new InvalidDataException("درخواست پذیرش خوانده نشد.");
        if (pending.Server != server || pending.Request is null || pending.Request.PatientId != patientId || patientId <= 0 ||
            pending.Request.RequestId is null || pending.Request.RequestId == Guid.Empty ||
            (pending.Request.DoctorId is null && pending.Request.ServiceId is null))
            throw new InvalidDataException("درخواست پذیرش ذخیره‌شده معتبر نیست.");
        return pending;
    }
    public void Reserve(PendingReception pending)
    {
        using var guard = Lock(pending.Server, pending.Request.PatientId);
        Directory.CreateDirectory(_directory);
        var bytes = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(pending), null, DataProtectionScope.CurrentUser);
        using var stream = new FileStream(PathFor(pending.Server, pending.Request.PatientId), FileMode.CreateNew,
            FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }
    public void Clear(PendingReception pending)
    {
        using var guard = Lock(pending.Server, pending.Request.PatientId);
        var current = LoadUnlocked(pending.Server, pending.Request.PatientId);
        if (current is null || current.Request.RequestId != pending.Request.RequestId)
            throw new InvalidDataException("درخواست پذیرش محلی تغییر کرده است.");
        File.Delete(PathFor(pending.Server, pending.Request.PatientId));
    }
}
