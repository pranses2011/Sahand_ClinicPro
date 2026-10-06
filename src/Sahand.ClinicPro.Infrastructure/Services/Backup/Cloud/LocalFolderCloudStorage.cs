using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud;

/// <summary>
/// ذخیره‌سازی ابری پوشه‌محلی — چرخه سیزدهم (درخواست ۷-۳).
/// جایگزین بدون وابستگی برای شبکه/سرور فایل مشترک (SMB/NFS)؛
/// همان قرارداد ICloudBackupStorage را پیاده می‌کند تا BackupJob و endpointها بی‌تغییر بمانند.
/// </summary>
public class LocalFolderCloudStorage : ICloudBackupStorage
{
    private readonly string _folder;

    /// <summary>سازنده با مسیر پوشه مقصد</summary>
    public LocalFolderCloudStorage(string folder)
        => _folder = folder;

    /// <summary>همیشه آماده — پوشه در نخستین استفاده ساخته می‌شود</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_folder);

    /// <summary>نام ارائه‌دهنده</summary>
    public string ProviderName => "پوشه شبکه‌ای";

    /// <summary>بارگذاری فایل — کپی به پوشه مقصد</summary>
    public async Task<CloudObjectInfo> UploadAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        EnsureConfigured();
        Directory.CreateDirectory(_folder);
        var target = Path.Combine(_folder, Path.GetFileName(fileName));
        await using (var file = File.Create(target))
        {
            await content.CopyToAsync(file, ct);
            await file.FlushAsync(ct);
        }
        var info = new FileInfo(target);
        return new CloudObjectInfo(Path.GetFileName(fileName), info.Length, info.LastWriteTimeUtc);
    }

    /// <summary>دانلود فایل</summary>
    public Task<Stream> DownloadAsync(string fileName, CancellationToken ct = default)
    {
        EnsureConfigured();
        var source = Path.Combine(_folder, Path.GetFileName(fileName));
        if (!File.Exists(source))
            throw new InvalidOperationException($"فایل «{fileName}» در پوشه ابری پیدا نشد");
        return Task.FromResult<Stream>(File.OpenRead(source));
    }

    /// <summary>فهرست فایل‌های پشتیبان</summary>
    public Task<IReadOnlyList<CloudObjectInfo>> ListAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        if (!Directory.Exists(_folder)) return Task.FromResult<IReadOnlyList<CloudObjectInfo>>(new List<CloudObjectInfo>());
        var items = new DirectoryInfo(_folder)
            .GetFiles("*.bak*")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Select(f => new CloudObjectInfo(f.Name, f.Length, f.LastWriteTimeUtc))
            .ToList();
        return Task.FromResult<IReadOnlyList<CloudObjectInfo>>(items);
    }

    /// <summary>تست اتصال — دسترسی نوشتن به پوشه</summary>
    public Task<(bool Ok, string Message)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            EnsureConfigured();
            Directory.CreateDirectory(_folder);
            // پروب نوشتن — اطمینان از دسترسی واقعی
            var probe = Path.Combine(_folder, $".probe_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return Task.FromResult((true, $"پوشه «{_folder}» در دسترس نوشتن است"));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, $"دسترسی به پوشه ابری ممکن نشد — {ex.Message}"));
        }
    }

    /// <summary>اطمینان از مسیر</summary>
    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_folder))
            throw new InvalidOperationException("مسیر پوشه ابری تنظیم نشده است");
    }
}
