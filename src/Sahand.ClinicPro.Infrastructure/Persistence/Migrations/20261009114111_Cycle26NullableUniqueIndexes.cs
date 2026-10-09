using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — رفع خطای ۲۶۰۱ اس‌کیوال‌سرور:
    /// «Cannot insert duplicate key row … duplicate key value is (&lt;NULL&gt;)».
    /// ایندکس‌های یکتای تک‌ستونی روی ستون‌های تهی‌پذیر (مانند PersonnelCode
    /// پرسنل و NationalCode) فقط یک رکورد NULL را مجاز می‌شماردند و ثبت
    /// دومین کاربر/پرسنلِ بدون کد، کل تراکنش (نقش‌ها و دسترسی‌ها را هم) با
    /// شکست مواجه می‌کرد. در این مهاجرت ۷۲ ایندکس یکتا با فیلتر استاندارد
    /// «[ستون] IS NOT NULL AND [IsDeleted] = 0» بازسازی می‌شوند تا سطرهای
    /// بدون مقدار از دامنهٔ یکتایی خارج شوند و تعداد نامحدود رکورد NULL
    /// قابل ثبت باشد. این قاعده از این پس در ClinicProDbContext به‌صورت
    /// سراسری و خودکار روی همهٔ موجودیت‌ها اعمال می‌شود.
    /// </summary>
    public partial class Cycle26NullableUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visit_VisitCode",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Supplier_Code",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Specialty_Code",
                table: "Specialties");

            migrationBuilder.DropIndex(
                name: "IX_SmsTemplate_Code",
                table: "SmsTemplates");

            migrationBuilder.DropIndex(
                name: "IX_SmsProvider_ProviderCode",
                table: "SmsProviders");

            migrationBuilder.DropIndex(
                name: "IX_Shift_ShiftCode",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Service_ServiceCode",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_ServiceCategory_Code",
                table: "ServiceCategories");

            migrationBuilder.DropIndex(
                name: "IX_ReportDefinition_Code",
                table: "ReportDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ReferralRecord_ReferralCode",
                table: "ReferralSystem");

            migrationBuilder.DropIndex(
                name: "IX_Reception_ReceptionNumber",
                table: "Receptions");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrder_OrderNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Prescription_PrescriptionCode",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Position_Code",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyTransaction_TransactionNumber",
                table: "PharmacyTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Payment_PaymentNumber",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_SystemModule_Code",
                table: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Medication_IrcCode",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_LabTest_TestCode",
                table: "LabTests");

            migrationBuilder.DropIndex(
                name: "IX_LabOrder_OrderNumber",
                table: "LabOrders");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntry_EntryNumber",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceCompany_Code",
                table: "InsuranceCompanies");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceClaim_ClaimNumber",
                table: "InsuranceClaims");

            migrationBuilder.DropIndex(
                name: "IX_ImagingOrder_OrderNumber",
                table: "ImagingOrders");

            migrationBuilder.DropIndex(
                name: "IX_Expense_ExpenseNumber",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_ExaminationRoom_RoomNumber",
                table: "ExaminationRooms");

            migrationBuilder.DropIndex(
                name: "IX_Employee_NationalCode",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employee_PersonnelCode",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_ElectronicPrescription_TrackingCode",
                table: "ElectronicPrescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Doctor_MedicalLicenseNo",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Doctor_NezamPezeshkiCode",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Department_Code",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_PersonnelContract_ContractNumber",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Branch_BranchCode",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Appointment_AppointmentCode",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Visit_VisitCode",
                table: "Visits",
                column: "VisitCode",
                unique: true,
                filter: "[VisitCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Code",
                table: "Suppliers",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Specialty_Code",
                table: "Specialties",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SmsTemplate_Code",
                table: "SmsTemplates",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SmsProvider_ProviderCode",
                table: "SmsProviders",
                column: "ProviderCode",
                unique: true,
                filter: "[ProviderCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_ShiftCode",
                table: "Shifts",
                column: "ShiftCode",
                unique: true,
                filter: "[ShiftCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Service_ServiceCode",
                table: "Services",
                column: "ServiceCode",
                unique: true,
                filter: "[ServiceCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategory_Code",
                table: "ServiceCategories",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinition_Code",
                table: "ReportDefinitions",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralRecord_ReferralCode",
                table: "ReferralSystem",
                column: "ReferralCode",
                unique: true,
                filter: "[ReferralCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Reception_ReceptionNumber",
                table: "Receptions",
                column: "ReceptionNumber",
                unique: true,
                filter: "[ReceptionNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true,
                filter: "[OrderNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_PrescriptionCode",
                table: "Prescriptions",
                column: "PrescriptionCode",
                unique: true,
                filter: "[PrescriptionCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Position_Code",
                table: "Positions",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyTransaction_TransactionNumber",
                table: "PharmacyTransactions",
                column: "TransactionNumber",
                unique: true,
                filter: "[TransactionNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_PaymentNumber",
                table: "Payments",
                column: "PaymentNumber",
                unique: true,
                filter: "[PaymentNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SystemModule_Code",
                table: "Modules",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Medication_IrcCode",
                table: "Medications",
                column: "IrcCode",
                unique: true,
                filter: "[IrcCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LabTest_TestCode",
                table: "LabTests",
                column: "TestCode",
                unique: true,
                filter: "[TestCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrder_OrderNumber",
                table: "LabOrders",
                column: "OrderNumber",
                unique: true,
                filter: "[OrderNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_EntryNumber",
                table: "JournalEntries",
                column: "EntryNumber",
                unique: true,
                filter: "[EntryNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true,
                filter: "[InvoiceNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompany_Code",
                table: "InsuranceCompanies",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaim_ClaimNumber",
                table: "InsuranceClaims",
                column: "ClaimNumber",
                unique: true,
                filter: "[ClaimNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ImagingOrder_OrderNumber",
                table: "ImagingOrders",
                column: "OrderNumber",
                unique: true,
                filter: "[OrderNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Expense_ExpenseNumber",
                table: "Expenses",
                column: "ExpenseNumber",
                unique: true,
                filter: "[ExpenseNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExaminationRoom_RoomNumber",
                table: "ExaminationRooms",
                column: "RoomNumber",
                unique: true,
                filter: "[RoomNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_NationalCode",
                table: "Employees",
                column: "NationalCode",
                unique: true,
                filter: "[NationalCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_PersonnelCode",
                table: "Employees",
                column: "PersonnelCode",
                unique: true,
                filter: "[PersonnelCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicPrescription_TrackingCode",
                table: "ElectronicPrescriptions",
                column: "TrackingCode",
                unique: true,
                filter: "[TrackingCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_MedicalLicenseNo",
                table: "Doctors",
                column: "MedicalLicenseNo",
                unique: true,
                filter: "[MedicalLicenseNo] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_NezamPezeshkiCode",
                table: "Doctors",
                column: "NezamPezeshkiCode",
                unique: true,
                filter: "[NezamPezeshkiCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Department_Code",
                table: "Departments",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelContract_ContractNumber",
                table: "Contracts",
                column: "ContractNumber",
                unique: true,
                filter: "[ContractNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_BranchCode",
                table: "Branches",
                column: "BranchCode",
                unique: true,
                filter: "[BranchCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_AppointmentCode",
                table: "Appointments",
                column: "AppointmentCode",
                unique: true,
                filter: "[AppointmentCode] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visit_VisitCode",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Supplier_Code",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Specialty_Code",
                table: "Specialties");

            migrationBuilder.DropIndex(
                name: "IX_SmsTemplate_Code",
                table: "SmsTemplates");

            migrationBuilder.DropIndex(
                name: "IX_SmsProvider_ProviderCode",
                table: "SmsProviders");

            migrationBuilder.DropIndex(
                name: "IX_Shift_ShiftCode",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Service_ServiceCode",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_ServiceCategory_Code",
                table: "ServiceCategories");

            migrationBuilder.DropIndex(
                name: "IX_ReportDefinition_Code",
                table: "ReportDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ReferralRecord_ReferralCode",
                table: "ReferralSystem");

            migrationBuilder.DropIndex(
                name: "IX_Reception_ReceptionNumber",
                table: "Receptions");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrder_OrderNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Prescription_PrescriptionCode",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Position_Code",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyTransaction_TransactionNumber",
                table: "PharmacyTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Payment_PaymentNumber",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_SystemModule_Code",
                table: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Medication_IrcCode",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_LabTest_TestCode",
                table: "LabTests");

            migrationBuilder.DropIndex(
                name: "IX_LabOrder_OrderNumber",
                table: "LabOrders");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntry_EntryNumber",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceCompany_Code",
                table: "InsuranceCompanies");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceClaim_ClaimNumber",
                table: "InsuranceClaims");

            migrationBuilder.DropIndex(
                name: "IX_ImagingOrder_OrderNumber",
                table: "ImagingOrders");

            migrationBuilder.DropIndex(
                name: "IX_Expense_ExpenseNumber",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_ExaminationRoom_RoomNumber",
                table: "ExaminationRooms");

            migrationBuilder.DropIndex(
                name: "IX_Employee_NationalCode",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employee_PersonnelCode",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_ElectronicPrescription_TrackingCode",
                table: "ElectronicPrescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Doctor_MedicalLicenseNo",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Doctor_NezamPezeshkiCode",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Department_Code",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_PersonnelContract_ContractNumber",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Branch_BranchCode",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Appointment_AppointmentCode",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Visit_VisitCode",
                table: "Visits",
                column: "VisitCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Code",
                table: "Suppliers",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Specialty_Code",
                table: "Specialties",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SmsTemplate_Code",
                table: "SmsTemplates",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SmsProvider_ProviderCode",
                table: "SmsProviders",
                column: "ProviderCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_ShiftCode",
                table: "Shifts",
                column: "ShiftCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Service_ServiceCode",
                table: "Services",
                column: "ServiceCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategory_Code",
                table: "ServiceCategories",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinition_Code",
                table: "ReportDefinitions",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralRecord_ReferralCode",
                table: "ReferralSystem",
                column: "ReferralCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Reception_ReceptionNumber",
                table: "Receptions",
                column: "ReceptionNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_PrescriptionCode",
                table: "Prescriptions",
                column: "PrescriptionCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Position_Code",
                table: "Positions",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyTransaction_TransactionNumber",
                table: "PharmacyTransactions",
                column: "TransactionNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_PaymentNumber",
                table: "Payments",
                column: "PaymentNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SystemModule_Code",
                table: "Modules",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Medication_IrcCode",
                table: "Medications",
                column: "IrcCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LabTest_TestCode",
                table: "LabTests",
                column: "TestCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrder_OrderNumber",
                table: "LabOrders",
                column: "OrderNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_EntryNumber",
                table: "JournalEntries",
                column: "EntryNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompany_Code",
                table: "InsuranceCompanies",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaim_ClaimNumber",
                table: "InsuranceClaims",
                column: "ClaimNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ImagingOrder_OrderNumber",
                table: "ImagingOrders",
                column: "OrderNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Expense_ExpenseNumber",
                table: "Expenses",
                column: "ExpenseNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExaminationRoom_RoomNumber",
                table: "ExaminationRooms",
                column: "RoomNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_NationalCode",
                table: "Employees",
                column: "NationalCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_PersonnelCode",
                table: "Employees",
                column: "PersonnelCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicPrescription_TrackingCode",
                table: "ElectronicPrescriptions",
                column: "TrackingCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_MedicalLicenseNo",
                table: "Doctors",
                column: "MedicalLicenseNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_NezamPezeshkiCode",
                table: "Doctors",
                column: "NezamPezeshkiCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Department_Code",
                table: "Departments",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelContract_ContractNumber",
                table: "Contracts",
                column: "ContractNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_BranchCode",
                table: "Branches",
                column: "BranchCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_AppointmentCode",
                table: "Appointments",
                column: "AppointmentCode",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
