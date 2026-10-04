# -*- coding: utf-8 -*-
"""گام ۳ — بخش ۱: تولید Configurations برای تمام موجودیت‌ها + ClinicProDbContext + مبدل UTC"""
import os, re, glob

ROOT = os.path.join(os.path.dirname(__file__), "..")
DOMAIN = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Domain"))
INFRA = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Infrastructure"))
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))

EMAP = {
    "Patient": ("People", "Patients"), "PatientHistory": ("People", "PatientHistories"),
    "Doctor": ("People", "Doctors"), "Specialty": ("People", "Specialties"),
    "Employee": ("People", "Employees"), "Department": ("People", "Departments"),
    "Position": ("People", "Positions"), "Branch": ("People", "Branches"),
    "Appointment": ("Scheduling", "Appointments"), "Reception": ("Scheduling", "Receptions"),
    "DoctorSchedule": ("Scheduling", "DoctorSchedules"), "DoctorAbsence": ("Scheduling", "DoctorAbsences"),
    "ExaminationRoom": ("Scheduling", "ExaminationRooms"), "Shift": ("Scheduling", "Shifts"),
    "Queue": ("Scheduling", "Queues"),
    "Visit": ("Clinical", "Visits"), "VitalSign": ("Clinical", "VitalSigns"),
    "Icd10Code": ("Clinical", "ICD10Codes"), "Prescription": ("Clinical", "Prescriptions"),
    "PrescriptionItem": ("Clinical", "PrescriptionItems"), "Medication": ("Clinical", "Medications"),
    "DrugInteraction": ("Clinical", "DrugInteractions"), "PrescriptionTemplate": ("Clinical", "PrescriptionTemplates"),
    "Invoice": ("Billing", "Invoices"), "InvoiceItem": ("Billing", "InvoiceItems"),
    "Payment": ("Billing", "Payments"), "CashRegister": ("Billing", "CashRegisters"),
    "Service": ("Billing", "Services"), "ServiceCategory": ("Billing", "ServiceCategories"),
    "Tariff": ("Billing", "Tariffs"), "InsuranceCompany": ("Billing", "InsuranceCompanies"),
    "InsuranceClaim": ("Billing", "InsuranceClaims"), "InsuranceInquiry": ("Billing", "InsuranceInquiries"),
    "Expense": ("Billing", "Expenses"), "DoctorPayment": ("Billing", "DoctorPayments"),
    "AccountingAccount": ("Accounting", "AccountingAccounts"), "JournalEntry": ("Accounting", "JournalEntries"),
    "JournalEntryLine": ("Accounting", "JournalEntryLines"), "FiscalYear": ("Accounting", "FiscalYears"),
    "CostCenter": ("Accounting", "CostCenters"),
    "LabTest": ("Paraclinical", "LabTests"), "LabOrder": ("Paraclinical", "LabOrders"),
    "LabOrderItem": ("Paraclinical", "LabOrderItems"), "ImagingOrder": ("Paraclinical", "ImagingOrders"),
    "ImagingFile": ("Paraclinical", "ImagingFiles"), "DentalRecord": ("Paraclinical", "DentalRecords"),
    "DentalTreatmentPlan": ("Paraclinical", "DentalTreatmentPlans"), "DentalProcedure": ("Paraclinical", "DentalProcedures"),
    "EyeExamRecord": ("Paraclinical", "EyeExamRecords"), "PhysiotherapySession": ("Paraclinical", "PhysiotherapySessions"),
    "PharmacyInventory": ("Pharmacy", "PharmacyInventory"), "PharmacyTransaction": ("Pharmacy", "PharmacyTransactions"),
    "Supplier": ("Pharmacy", "Suppliers"), "PurchaseOrder": ("Pharmacy", "PurchaseOrders"),
    "PurchaseOrderItem": ("Pharmacy", "PurchaseOrderItems"), "StockTaking": ("Pharmacy", "StockTakings"),
    "EmployeeAttendance": ("Personnel", "EmployeeAttendances"), "EmployeeSalary": ("Personnel", "EmployeeSalaries"),
    "EmployeeLeave": ("Personnel", "EmployeeLeaves"), "PersonnelContract": ("Personnel", "Contracts"),
    "User": ("Security", "Users"), "Role": ("Security", "Roles"), "Permission": ("Security", "Permissions"),
    "UserRole": ("Security", "UserRoles"), "RolePermission": ("Security", "RolePermissions"),
    "RefreshToken": ("Security", "RefreshTokens"), "UserSession": ("Security", "UserSessions"),
    "PasswordHistory": ("Security", "PasswordHistory"), "UserModulePermission": ("Security", "UserModulePermissions"),
    "SmsMessage": ("Communication", "SmsMessages"), "SmsTemplate": ("Communication", "SmsTemplates"),
    "SmsProvider": ("Communication", "SmsProviders"), "SmsQueueItem": ("Communication", "SmsQueue"),
    "PhoneCall": ("Communication", "PhoneCalls"), "Survey": ("Communication", "Surveys"),
    "SurveyResponse": ("Communication", "SurveyResponses"), "Notification": ("Communication", "Notifications"),
    "Document": ("Infrastructure", "Documents"), "DocumentCategory": ("Infrastructure", "DocumentCategories"),
    "BackupHistory": ("Infrastructure", "BackupHistory"), "AuditLog": ("Infrastructure", "AuditLogs"),
    "ErrorLog": ("Infrastructure", "ErrorLogs"), "SystemSetting": ("Infrastructure", "SystemSettings"),
    "ClinicInfo": ("Infrastructure", "ClinicInfo"), "SystemModule": ("Infrastructure", "Modules"),
    "PerformanceMetric": ("Infrastructure", "PerformanceMetrics"),
    "ClinicLicense": ("Licensing", "ClinicLicense"), "LicenseCheckLog": ("Licensing", "LicenseCheckLog"),
    "SepasTransaction": ("Integration", "SepasTransactions"), "ElectronicPrescription": ("Integration", "ElectronicPrescriptions"),
    "SibSystemRecord": ("Integration", "SibSystemRecords"), "ApiCallLog": ("Integration", "ApiCallLogs"),
    "ApiCredential": ("Integration", "ApiCredentials"), "ReferralRecord": ("Integration", "ReferralSystem"),
    "ReportDefinition": ("Reporting", "ReportDefinitions"), "ReportExecution": ("Reporting", "ReportExecutions"),
    "ReportSchedule": ("Reporting", "ReportSchedules"),
}

ENUMS = {os.path.splitext(os.path.basename(f))[0] for f in glob.glob(os.path.join(DOMAIN, "Enums", "*.cs"))}

MAXCOL = {
    "Notes", "Instructions", "Biography", "ClinicalInfo", "Technique", "Findings", "Impression",
    "Recommendation", "Response", "ResponseBody", "RequestBody", "Questions", "Answers", "Comments",
    "Content", "Terms", "PlanDetails", "Treatments", "ProgressNote", "VisualField", "Prescription",
    "EyewearPrescription", "ChiefComplaint", "HistoryOfPresentIllness", "PastMedicalHistory",
    "FamilyHistory", "SocialHistory", "ReviewOfSystems", "PhysicalExamination", "SoapSubjective",
    "SoapObjective", "SoapAssessment", "SoapPlan", "SecondaryDiagnoses", "TreatmentPlan",
    "FollowUpInstructions", "SideEffects", "Contraindications", "Allergies", "ChronicDiseases",
    "CurrentMedications", "Equipment", "ChangedFields", "OldValues", "NewValues", "Variables",
    "ErrorMessage", "Exception", "StackTrace", "UserAgent", "EnabledModules", "SqlQuery",
    "Parameters", "Recipients", "Context", "Data", "RequestData", "ResponseData", "Message",
    "Description",
}

EXACT_LEN = {
    "NationalCode": 10, "PostalCode": 10, "Mobile": 11, "Recipient": 11, "EmergencyContactPhone": 11,
    "Phone": 15, "ContactPhone": 15, "Fax": 15, "CallerNumber": 15, "EmergencyPhone": 15,
    "Sheba": 26, "Email": 100, "Address": 500, "ProfilePicture": 500, "LogoPath": 500,
    "FirstName": 50, "LastName": 50, "FatherName": 50, "ContactPerson": 100,
    "BloodType": 5, "AbnormalFlag": 5, "Method": 10,
    "InsuranceApprovalCode": 50, "ReferralCode": 50, "ApprovalCode": 50,
    "City": 50, "Province": 50, "Occupation": 100, "Nationality": 50, "EmergencyContactRelation": 50,
    "InsuranceType": 50, "TemplateName": 100, "Surface": 20, "Unit": 20, "Dosage": 100,
    "Frequency": 100, "Duration": 50, "Route": 50, "BankName": 50, "BankAccount": 50,
    "SessionToken": 200, "Token": 500, "JwtId": 100, "SecurityStamp": 100,
    "Username": 50, "PasswordHash": 500, "RecordingPath": 500, "FilePath": 500,
    "ThumbnailPath": 500, "DocumentPath": 500, "FileName": 200, "FileType": 20,
    "MimeType": 100, "Hash": 100, "Tags": 500, "ProviderMessageId": 100,
    "SenderNumber": 20, "ProviderCode": 20, "ProviderName": 50, "RegisterName": 50,
    "RoomName": 50, "RoomNumber": 10, "IconPath": 200, "Website": 100, "ManagerName": 100,
    "Barcode": 50, "BatchNumber": 50, "Location": 50, "TerminalId": 20, "CardNumber": 20,
    "TraceNumber": 50, "ReferenceNumber": 50, "RegistrationCode": 50, "SepasReferenceId": 50,
    "SepasPatientId": 50, "SibRecordId": 50, "TrackingCode": 50, "ServiceName": 50,
    "ReportName": 200, "ExportFormat": 20, "NormalRange": 100, "Version": 20,
    "Ip": 50, "IpAddress": 50, "LastLoginIp": 50, "FieldOfStudy": 100,
    "RightVisualAcuity": 10, "LeftVisualAcuity": 10, "ColorVision": 100, "MaterialUsed": 100,
    "ReferringDoctor": 100, "AccountName": 200, "ReportFile": None, "Value": None,
}

DECIMAL_MAP = {
    "ResultNumeric": (18, 4), "Amount": (18, 0),
    "DiscountPercent": (5, 2), "InsurancePercent": (5, 2), "DoctorSharePercent": (5, 2),
    "DoctorPercentage": (5, 2), "DefaultPatientShare": (5, 2), "DefaultInsuranceShare": (5, 2),
    "InsuranceShare": (5, 2), "PatientShare": (5, 2), "Rating": (3, 2), "Bmi": (4, 2),
    "Temperature": (4, 1), "PupilDistance": (4, 1), "Cost": (10, 2), "KValue": (10, 2),
    "KCoefficient": (10, 2), "Days": (4, 1), "Quantity": (10, 2), "ReceivedQuantity": (10, 2),
    "DeliveredQuantity": (10, 2), "Weight": (5, 2), "Height": (5, 2), "HeadCircumference": (5, 2),
    "RightSphere": (4, 2), "LeftSphere": (4, 2), "RightCylinder": (4, 2), "LeftCylinder": (4, 2),
    "WorkedHours": (5, 2), "OvertimeHours": (5, 2), "UnitPrice": (18, 0), "Price": (18, 0),
    "OpeningBalance": (18, 0), "ClosingBalance": (18, 0), "TotalCash": (18, 0),
}

IDX = {
    "Patient": [("i", ["NationalCode"], True, True), ("i", ["MedicalRecordNo"], True, True),
                 ("i", ["Mobile"], False, False), ("i", ["LastName"], False, False)],
    "Doctor": [("i", ["NezamPezeshkiCode"], True, True), ("i", ["MedicalLicenseNo"], True, True)],
    "Specialty": [("i", ["Code"], True, True)],
    "Employee": [("i", ["PersonnelCode"], True, True), ("i", ["NationalCode"], True, True)],
    "Appointment": [("i", ["AppointmentCode"], True, True), ("i", ["AppointmentDate", "DoctorId"], False, False),
                     ("i", ["PatientId"], False, False), ("i", ["Status"], False, False)],
    "Reception": [("i", ["ReceptionNumber"], True, True), ("i", ["PatientId"], False, False)],
    "Visit": [("i", ["VisitCode"], True, True), ("i", ["PatientId", "VisitDate"], False, False),
               ("i", ["DoctorId", "VisitDate"], False, False)],
    "Prescription": [("i", ["PrescriptionCode"], True, True), ("i", ["PatientId"], False, False)],
    "Invoice": [("i", ["InvoiceNumber"], True, True), ("i", ["InvoiceDate"], False, False),
                 ("i", ["PatientId"], False, False), ("i", ["PaymentStatus"], False, False)],
    "Payment": [("i", ["PaymentNumber"], True, True), ("i", ["InvoiceId"], False, False)],
    "JournalEntry": [("i", ["EntryNumber"], True, True)],
    "AccountingAccount": [("i", ["AccountCode"], True, True)],
    "InsuranceCompany": [("i", ["Code"], True, True)],
    "InsuranceClaim": [("i", ["ClaimNumber"], True, True)],
    "LabOrder": [("i", ["OrderNumber"], True, True), ("i", ["PatientId"], False, False)],
    "ImagingOrder": [("i", ["OrderNumber"], True, True), ("i", ["PatientId"], False, False)],
    "PharmacyInventory": [("i", ["ExpiryDate"], False, False),
                           ("i", ["Quantity"], False, "([Quantity] < [ReorderLevel] AND [IsDeleted] = 0)")],
    "PharmacyTransaction": [("i", ["TransactionNumber"], True, True)],
    "PurchaseOrder": [("i", ["OrderNumber"], True, True)],
    "Supplier": [("i", ["Code"], True, True)],
    "User": [("i", ["Username"], True, True), ("i", ["Email"], True, True)],
    "Role": [("i", ["Name"], True, True)],
    "Permission": [("i", ["Name"], True, True)],
    "RefreshToken": [("i", ["Token"], True, False)],
    "SmsTemplate": [("i", ["Code"], True, True)],
    "SmsProvider": [("i", ["ProviderCode"], True, True)],
    "AuditLog": [("i", ["UserId", "Timestamp"], False, False), ("i", ["EntityName", "EntityId"], False, False)],
    "SystemSetting": [("i", ["Key"], True, False)],
    "FiscalYear": [("i", ["Year"], True, False)],
    "Service": [("i", ["ServiceCode"], True, True)],
    "ServiceCategory": [("i", ["Code"], True, True)],
    "Tariff": [("i", ["ServiceId", "InsuranceCompanyId", "Year"], False, False)],
    "Icd10Code": [("i", ["Code"], True, False)],
    "Medication": [("i", ["IrcCode"], True, True)],
    "LabTest": [("i", ["TestCode"], True, True)],
    "Position": [("i", ["Code"], True, True)],
    "Department": [("i", ["Code"], True, True)],
    "Branch": [("i", ["BranchCode"], True, True)],
    "Shift": [("i", ["ShiftCode"], True, True)],
    "ExaminationRoom": [("i", ["RoomNumber"], True, True)],
    "Expense": [("i", ["ExpenseNumber"], True, True)],
    "PersonnelContract": [("i", ["ContractNumber"], True, True)],
    "DoctorPayment": [("i", ["DoctorId", "PeriodStart"], False, False)],
    "SepasTransaction": [("i", ["SepasReferenceId"], False, False)],
    "ElectronicPrescription": [("i", ["TrackingCode"], True, True)],
    "ReferralRecord": [("i", ["ReferralCode"], True, True)],
    "ApiCredential": [("i", ["ServiceName"], True, False)],
    "ReportDefinition": [("i", ["Code"], True, True)],
    "SystemModule": [("i", ["Code"], True, True)],
    "Survey": [("i", ["Title"], False, False)],
    "CashRegister": [("i", ["RegisterName"], False, False)],
    "EyeExamRecord": [("i", ["PatientId"], False, False)],
    "PhysiotherapySession": [("i", ["PatientId", "SessionDate"], False, False)],
    "DentalRecord": [("i", ["PatientId", "ToothNumber"], False, False)],
}

SEQUENCES = [
    ("Patient", "MedicalRecordNo", "SeqPatientMrn", "MRN-"),
    ("Appointment", "AppointmentCode", "SeqAppointmentCode", "APT-"),
    ("Reception", "ReceptionNumber", "SeqReceptionNumber", "RCP-"),
    ("Visit", "VisitCode", "SeqVisitCode", "VST-"),
    ("Prescription", "PrescriptionCode", "SeqPrescriptionCode", "RX-"),
    ("Invoice", "InvoiceNumber", "SeqInvoiceNumber", "INV-"),
    ("Payment", "PaymentNumber", "SeqPaymentNumber", "PAY-"),
    ("JournalEntry", "EntryNumber", "SeqJournalEntryNumber", "JE-"),
    ("LabOrder", "OrderNumber", "SeqLabOrderNumber", "LAB-"),
    ("ImagingOrder", "OrderNumber", "SeqImagingOrderNumber", "IMG-"),
    ("PharmacyTransaction", "TransactionNumber", "SeqPharmacyTransactionNumber", "PHT-"),
    ("InsuranceClaim", "ClaimNumber", "SeqClaimNumber", "CLM-"),
    ("Expense", "ExpenseNumber", "SeqExpenseNumber", "EXP-"),
    ("PersonnelContract", "ContractNumber", "SeqContractNumber", "CTR-"),
    ("PurchaseOrder", "OrderNumber", "SeqPurchaseOrderNumber", "PO-"),
]

# پراپرتی‌های AuditableEntity که مرکزی پیکربندی می‌شوند
SKIP = {"Id", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted",
        "DeletedAt", "DeletedBy", "BranchId", "RowVersion"}

PROP_RE = re.compile(r'/// <summary>(.*?)</summary>\s*\n\s*public ([\w\.\?<>\[\]]+) (\w+) \{ get; set; \}')
CLS_RE = re.compile(r'/// <summary>\s*\n/// (.*?)\s*\n/// </summary>.*?public class ', re.DOTALL)


def maxlen(prop):
    if prop in EXACT_LEN:
        return EXACT_LEN[prop]
    if prop in MAXCOL:
        return None
    if prop.endswith("Path"):
        return 500
    if prop.endswith("Number") or prop.endswith("Code"):
        return 20
    if prop.endswith("Name") or prop in ("Title", "Name"):
        return 200
    if prop == "Form":
        return 50
    return 200


def parse_entity(folder, name):
    path = os.path.join(DOMAIN, folder, name + ".cs")
    src = open(path, encoding="utf-8-sig").read()
    m = CLS_RE.search(src)
    cls_sum = m.group(1) if m else name
    props = []
    for pm in PROP_RE.finditer(src):
        comment, ctype, pname = pm.group(1), pm.group(2), pm.group(3)
        if ctype.startswith("ICollection"):
            continue
        base = ctype.rstrip("?").split(".")[-1]
        if base in ("string", "int", "long", "bool", "DateTime", "DateOnly", "TimeOnly", "decimal", "byte", "byte[]") or base in ENUMS:
            props.append((pname, ctype, base, comment))
    return cls_sum, props


def gen_config(name, folder, table):
    cls_sum, props = parse_entity(folder, name)
    cls_sum = cls_sum.replace('"', "'")
    lines = []
    lines.append(f"/// <summary>پیکربندی EF Core برای جدول {table} — {cls_sum}</summary>")
    lines.append(f"public class {name}Configuration : IEntityTypeConfiguration<{name}>")
    lines.append("{")
    lines.append(f"    public void Configure(EntityTypeBuilder<{name}> builder)")
    lines.append("    {")
    lines.append(f'        builder.ToTable("{table}", t => t.HasComment("{cls_sum}"));')
    if name == "UserRole":
        lines.append("        builder.HasKey(x => new { x.UserId, x.RoleId });")
    elif name == "RolePermission":
        lines.append("        builder.HasKey(x => new { x.RoleId, x.PermissionId });")
    else:
        lines.append("        builder.HasKey(x => x.Id);")
    lines.append("")
    seq_map = {s[1]: s for s in SEQUENCES if s[0] == name}
    for pname, ctype, base, comment in props:
        if pname in SKIP:
            continue
        cfgs = []
        if base == "string":
            ml = maxlen(pname)
            if ml is None:
                if not ctype.endswith("?"):
                    cfgs.append("IsRequired()")
            else:
                cfgs.append(f"HasMaxLength({ml})")
                if not ctype.endswith("?"):
                    cfgs.append("IsRequired()")
        elif base == "decimal":
            prec = DECIMAL_MAP.get(pname, (18, 0))
            cfgs.append(f"HasPrecision({prec[0]}, {prec[1]})")
        elif base in ENUMS:
            cfgs.append("HasConversion<byte>()")
        if pname in seq_map:
            _, _, seq, prefix = seq_map[pname]
            cfgs.append(f"HasDefaultValueSql(\"CONCAT('{prefix}', RIGHT('000000' + CAST(NEXT VALUE FOR {seq} AS varchar(10)), 6))\")")
        if cfgs:
            joined = "." + ".".join(cfgs)
            cmt = (comment or pname).replace('"', "'")
            lines.append(f'        builder.Property(x => x.{pname}){joined}.HasComment("{cmt}");')
    lines.append("")
    for idx in IDX.get(name, []):
        kind, cols, unique, filtered = idx
        if isinstance(filtered, str):
            filt = filtered
        else:
            filt = "[IsDeleted] = 0" if filtered else None
        names = ", ".join(f"x.{c}" for c in cols)
        uniq = ".IsUnique()" if unique else ""
        iname = "_".join(cols)
        fpart = f'.HasFilter("{filt}")' if filt else ""
        lines.append(f'        builder.HasIndex(x => new {{ {names} }}){uniq}.HasDatabaseName("IX_{name}_{iname}"){fpart};')
    lines.append("    }")
    lines.append("}")
    return "\n".join(lines)


def write(rel, content, base):
    full = os.path.join(base, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")


for name, (folder, table) in EMAP.items():
    body = gen_config(name, folder, table)
    ns = f"Sahand.ClinicPro.Domain.Entities.{folder}"
    content = f"""using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {ns};

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

{body}
"""
    write(f"Persistence/Configurations/{folder}/{name}Configuration.cs", content, INFRA)
    print(f"  + Configurations/{folder}/{name}Configuration.cs")

# نام پراپرتی DbSet = نام جدول (جمع صحیح انگلیسی)
dbsets = []
for name, (folder, table) in sorted(EMAP.items()):
    prop = table
    dbsets.append(f"    /// <summary>مجموعه {table}</summary>\n    public DbSet<{name}> {prop} {{ get; set; }}")
dbsets_txt = "\n\n".join(dbsets)

ctx = f"""using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// DbContext اصلی سامانه — ۸۷ موجودیت در ۱۵ پوشه ماژولار
/// </summary>
/// <remarks>
/// قواعد سراسری مدل:
/// ۱. همه جداول دارای فیلدهای حسابرسی و حذف نرم هستند
/// ۲. فیلتر پرس‌وجوی سراسری IsDeleted روی همه موجودیت‌های قابل حذف نرم
/// ۳. رفتار حذف همه کلیدهای خارجی NoAction است (برای حذف نرم)
/// ۴. RowVersion به‌عنوان نشانگر همزمانی روی همه جدول‌ها
/// ۵. Sequenceهای SQL Server برای شماره‌های یکتای کسب‌وکار
/// </remarks>
public class ClinicProDbContext : DbContext
{{
{dbsets_txt}

    public ClinicProDbContext(DbContextOptions<ClinicProDbContext> options) : base(options)
    {{
    }}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {{
        base.OnModelCreating(modelBuilder);

        // اعمال همه پیکربندی‌های موجودیت از این اسمبلی
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicProDbContext).Assembly);

        // روابط صریح موارد چند‌ناوبری — بیمار دو بیمه و پزشک دو تخصص دارد
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.InsuranceCompany)
            .WithMany(i => i.Patients)
            .HasForeignKey(p => p.InsuranceCompanyId);
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.SupplementaryInsurance)
            .WithMany()
            .HasForeignKey(p => p.SupplementaryInsuranceId);
        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.Specialty)
            .WithMany(s => s.Doctors)
            .HasForeignKey(d => d.SpecialtyId);
        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.SubSpecialty)
            .WithMany()
            .HasForeignKey(d => d.SubSpecialtyId);

        // سکانس‌های شماره یکتا — ۱۵ سکانس کسب‌وکاری با شروع از ۱۰۰۰۰۱
        modelBuilder.HasSequence<long>("SeqPatientMrn").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqAppointmentCode").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqReceptionNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqVisitCode").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPrescriptionCode").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqInvoiceNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPaymentNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqJournalEntryNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqLabOrderNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqImagingOrderNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPharmacyTransactionNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqClaimNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqExpenseNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqContractNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPurchaseOrderNumber").StartsAt(100001).IncrementsBy(1);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {{
            var clrType = entityType.ClrType;

            // فیلتر سراسری حذف نرم — رکوردهای حذف‌شده از پرس‌وجوها پنهان می‌شوند
            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {{
                var parameter = Expression.Parameter(clrType, "e");
                var filter = Expression.Lambda(
                    Expression.Not(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted))),
                    parameter);
                modelBuilder.Entity(clrType).HasQueryFilter(filter);
            }}

            // رفتار حذف همه کلیدهای خارجی NoAction — الزام حذف نرم
            foreach (var foreignKey in entityType.GetForeignKeys())
            {{
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
            }}

            // RowVersion به‌عنوان نشانگر همزمانی خوش‌بینانه
            var rowVersion = entityType.FindProperty(nameof(AuditableEntity.RowVersion));
            if (rowVersion is not null)
            {{
                modelBuilder.Entity(clrType).Property(nameof(AuditableEntity.RowVersion)).IsRowVersion();
            }}
        }}
    }}
}}
"""
write("Persistence/ClinicProDbContext.cs", ctx, INFRA)
print("  + Persistence/ClinicProDbContext.cs")

conv = """using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// مبدل DateTime به UTC — همه زمان‌ها به‌صورت جهانی ذخیره می‌شوند
/// </summary>
public class DateTimeToUtcConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>ساخت مبدل با اعمال Kind از نوع Utc</summary>
    public DateTimeToUtcConverter() : this(null) { }

    /// <summary>ساخت مبدل با نگاشت مشخص</summary>
    public DateTimeToUtcConverter(ConverterMappingHints? mappingHints)
        : base(v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
               v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
               mappingHints)
    {
    }
}

/// <summary>
/// مبدل DateTime تهی‌پذیر به UTC
/// </summary>
public class NullableDateTimeToUtcConverter : ValueConverter<DateTime?, DateTime?>
{
    /// <summary>ساخت مبدل با اعمال Kind از نوع Utc</summary>
    public NullableDateTimeToUtcConverter() : this(null) { }

    /// <summary>ساخت مبدل با نگاشت مشخص</summary>
    public NullableDateTimeToUtcConverter(ConverterMappingHints? mappingHints)
        : base(v => v.HasValue
               ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime())
               : v,
               v => v.HasValue
               ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
               : v,
               mappingHints)
    {
    }
}
"""
write("Persistence/DateTimeUtcConverters.cs", conv, INFRA)
print("  + Persistence/DateTimeUtcConverters.cs")

cur = """namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// سرویس کاربر فعلی — شناسه و نام کاربر احراز هویت‌شده جاری را فراهم می‌کند
/// برای پر کردن خودکار فیلدهای حسابرسی استفاده می‌شود
/// </summary>
public interface ICurrentUserService
{
    /// <summary>شناسه کاربر فعلی — تهی یعنی کاربر احراز هویت نشده</summary>
    long? UserId { get; }

    /// <summary>نام کاربری فعلی</summary>
    string? Username { get; }

    /// <summary>آیا کاربر احراز هویت شده است</summary>
    bool IsAuthenticated { get; }

    /// <summary>نشانی IP درخواست جاری</summary>
    string? IpAddress { get; }
}
"""
write("Interfaces/ICurrentUserService.cs", cur, APP)
print("  + Application/Interfaces/ICurrentUserService.cs")

notif = """using MediatR;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Common.Events;

/// <summary>
/// پوشش رویداد دامنه برای انتشار از طریق MediatR — لایه دامنه به MediatR وابسته نیست
/// </summary>
/// <typeparam name="TDomainEvent">نوع رویداد دامنه</typeparam>
/// <param name="DomainEvent">رویداد دامنه اصلی</param>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent)
    : INotification where TDomainEvent : IDomainEvent;
"""
write("Common/Events/DomainEventNotification.cs", notif, APP)
print("  + Application/Common/Events/DomainEventNotification.cs")

print("✅ بخش ۱ گام ۳ کامل شد")
