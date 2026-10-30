using System.Security.Cryptography;
using System.Text.Json;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Application.Features.Visits.Commands.SaveVisit;

/// <summary>اثر انگشت داده ویزیت و آخرین علائم حیاتی برای جلوگیری از بازنویسی نسخه قدیمی.</summary>
public static class VisitEditVersion
{
    public static string Create(Visit v, VitalSign? vital)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            v.Id, v.PatientId, v.DoctorId, v.ReceptionId, v.Status, v.VisitType,
            v.ChiefComplaint, v.HistoryOfPresentIllness, v.PastMedicalHistory,
            v.PhysicalExamination, v.SoapSubjective, v.SoapObjective, v.SoapAssessment, v.SoapPlan,
            v.PrimaryDiagnosisId, v.TreatmentPlan, v.FollowUpDate, v.FamilyHistory, v.SocialHistory,
            v.ReviewOfSystems, v.SecondaryDiagnoses, v.FollowUpInstructions, v.RowVersion,
            vital = vital is null ? null : new { vital.Id, vital.PatientId, vital.MeasuredAt, vital.BloodPressureSystolic,
                vital.BloodPressureDiastolic, vital.HeartRate, vital.RespiratoryRate, vital.Temperature, vital.OxygenSaturation,
                vital.Weight, vital.Height, vital.BloodSugar, vital.Bmi, vital.HeadCircumference, vital.MeasuredBy, vital.RowVersion }
        });
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
