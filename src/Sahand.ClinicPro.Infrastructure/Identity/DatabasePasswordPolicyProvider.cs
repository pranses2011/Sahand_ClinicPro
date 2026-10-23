using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Security;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Identity;

/// <summary>سیاست رمز را مستقیم از تنظیمات جاری پایگاه‌داده می‌خواند.</summary>
public sealed class DatabasePasswordPolicyProvider : IPasswordPolicyProvider
{
    private const string Category = "Security";
    private readonly IUnitOfWork _unitOfWork;

    public DatabasePasswordPolicyProvider(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<PasswordPolicy> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.Repository<SystemSetting>().QueryableNoTracking
            .Where(s => !s.IsDeleted && s.Category == Category)
            .Select(s => new { s.Key, s.Value })
            .ToListAsync(cancellationToken);
        return PasswordPolicy.FromSettings(settings.Select(s =>
            new KeyValuePair<string, string?>(s.Key, s.Value)));
    }
}
