using System.Security.Cryptography;
using System.Text.Json;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>اثر انگشت نسخهٔ قابل ویرایش؛ مانع بازنویسی نسخه‌ای که از زمان بازشدن فرم تغییر کرده است.</summary>
internal static class SurveyEditVersion
{
    public static string Create(Survey survey) => Create(survey.Id, survey.Title, survey.Description,
        survey.SurveyType, survey.IsActive, survey.Questions, survey.RowVersion);

    public static string Create(long id, string title, string? description, SurveyType type, bool active,
        string? questions, byte[]? rowVersion)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { id, title, description, type, active, questions, rowVersion });
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
