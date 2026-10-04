using System.Collections.Generic;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد ثبت بیمار جدید — برای ارسال پیامک خوش‌آمدگویی</summary>
public sealed record PatientCreatedEvent(long PatientId, string MedicalRecordNo, string Mobile) : DomainEventBase;

/// <summary>رویداد ویرایش اطلاعات بیمار</summary>
public sealed record PatientUpdatedEvent(long PatientId) : DomainEventBase;
