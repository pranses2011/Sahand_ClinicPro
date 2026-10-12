# -*- coding: utf-8 -*-
"""گام ۲ — بخش ۲: همه شمارشی‌ها (۳۵+)"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from step02_lib import enum_file

# ─── مردم ───
enum_file("Gender", "جنسیت", [(1, "Male", "مرد"), (2, "Female", "زن")])
enum_file("MaritalStatus", "وضعیت تأهل", [(1, "Single", "مجرد"), (2, "Married", "متأهل"), (3, "Widowed", "بیوه"), (4, "Divorced", "مطلقه")])
enum_file("PatientType", "نوع بیمار", [(1, "Normal", "عادی"), (2, "Vip", "ویژه"), (3, "Staff", "کارمند")])
enum_file("EducationLevel", "سطح تحصیلات", [(1, "Illiterate", "بی‌سواد"), (2, "Elementary", "ابتدایی"), (3, "MiddleSchool", "سیکل"), (4, "HighSchool", "دیپلم"), (5, "Associate", "کاردانی"), (6, "Bachelor", "کارشناسی"), (7, "Master", "کارشناسی ارشد"), (8, "Phd", "دکتری")])
enum_file("HistoryType", "نوع سابقه بیمار", [(1, "Surgical", "جراحی"), (2, "Medical", "بیماری"), (3, "Allergy", "آلرژی"), (4, "Family", "خانوادگی"), (5, "Hospitalization", "بستری"), (6, "Medication", "دارویی")])

# ─── نوبت‌دهی ───
enum_file("AppointmentStatus", "وضعیت نوبت", [
    (1, "Booked", "رزرو شده"), (2, "Confirmed", "تأیید شده"), (3, "Arrived", "حاضر"),
    (4, "InProgress", "در حال ویزیت"), (5, "Completed", "تکمیل شده"),
    (6, "Cancelled", "لغو شده"), (7, "NoShow", "عدم مراجعه")])
enum_file("AppointmentType", "نوع نوبت", [(1, "Initial", "اولیه"), (2, "FollowUp", "پیگیری"), (3, "Emergency", "اورژانس")])
enum_file("AppointmentSource", "منبع ثبت نوبت", [(1, "InPerson", "حضوری"), (2, "Phone", "تلفنی"), (3, "Online", "آنلاین")])
enum_file("Priority", "اولویت", [(1, "Emergency", "اورژانس"), (2, "High", "بالا"), (3, "Normal", "عادی")])
enum_file("ReceptionStatus", "وضعیت پذیرش", [
    (1, "Admitted", "پذیرش شده"), (2, "InQueue", "در صف"), (3, "InVisit", "در حال ویزیت"),
    (4, "Visited", "ویزیت شده"), (5, "Discharged", "ترخیص"), (6, "Cancelled", "لغو")])
enum_file("RoomType", "نوع اتاق", [(1, "Examination", "معاینه"), (2, "Procedure", "عمل"), (3, "Consultation", "مشاوره"), (4, "Ultrasound", "سونوگرافی"), (5, "Laboratory", "آزمایشگاه"), (6, "Other", "سایر")])
enum_file("ShiftType", "نوع شیفت", [(1, "Morning", "صبح"), (2, "Evening", "عصر"), (3, "Night", "شب"), (4, "Rotating", "چرخشی")])
enum_file("QueueStatus", "وضعیت صف انتظار", [(1, "Waiting", "در انتظار"), (2, "Called", "فراخوانده شده"), (3, "InProgress", "در حال انجام"), (4, "Completed", "تکمیل"), (5, "Skipped", "جدا شده")])
enum_file("AbsenceType", "نوع غیبت پزشک", [(1, "Vacation", "مرخصی"), (2, "SickLeave", "استعلاجی"), (3, "Conference", "کنگره"), (4, "Other", "سایر")])

# ─── ویزیت و نسخه ───
enum_file("VisitType", "نوع ویزیت", [(1, "Initial", "اولیه"), (2, "FollowUp", "پیگیری"), (3, "Emergency", "اورژانس"), (4, "Consultation", "مشاوره")])
enum_file("VisitStatus", "وضعیت ویزیت", [(1, "Started", "شروع شده"), (2, "InProgress", "در حال انجام"), (3, "Completed", "تکمیل شده"), (4, "Draft", "ذخیره موقت")])
enum_file("PrescriptionType", "نوع نسخه", [(1, "Medication", "دارویی"), (2, "Paraclinical", "پاراکلینیک"), (3, "Combined", "ترکیبی")])
enum_file("PrescriptionStatus", "وضعیت نسخه", [
    (1, "Issued", "صادر شده"), (2, "SentToInsurance", "ارسال به بیمه"), (3, "InsuranceApproved", "تأیید بیمه"),
    (4, "Delivered", "تحویل شده"), (5, "Cancelled", "لغو"), (6, "Expired", "منقضی")])
enum_file("InsuranceSystemType", "سامانه نسخه الکترونیک بیمه", [(1, "Salamat", "سلامت"), (2, "Tamin", "تأمین اجتماعی"), (3, "Mosallah", "مسلح"), (4, "Other", "سایر")])
enum_file("DrugInteractionSeverity", "شدت تداخل دارویی", [(1, "Minor", "خفیف"), (2, "Moderate", "متوسط"), (3, "Major", "شدید"), (4, "Contraindicated", "ممنوع مصرف")])
enum_file("MedicationRoute", "مسیر مصرف دارو", [(1, "Oral", "خوراکی"), (2, "Injection", "تزریقی"), (3, "Topical", "موضعی"), (4, "Inhalation", "استنشاقی"), (5, "Rectal", "مقعدی"), (6, "Ophthalmic", "چشمی"), (7, "Otic", "گوشی"), (8, "Nasal", "بینی"), (9, "Sublingual", "زیرزبانی")])

# ─── مالی ───
enum_file("InvoiceType", "نوع فاکتور", [(1, "Visit", "ویزیت"), (2, "Lab", "آزمایشگاه"), (3, "Imaging", "تصویربرداری"), (4, "Pharmacy", "داروخانه"), (5, "Dental", "دندانپزشکی"), (6, "Physiotherapy", "فیزیوتراپی"), (7, "Other", "سایر")])
enum_file("InvoiceStatus", "وضعیت فاکتور", [(1, "Draft", "پیش‌نویس"), (2, "Issued", "صادر شده"), (3, "PartiallyPaid", "پرداخت جزئی"), (4, "Paid", "پرداخت شده"), (5, "Cancelled", "ابطال"), (6, "Refunded", "برگشت هزینه")])
enum_file("PaymentStatus", "وضعیت پرداخت", [(1, "Unpaid", "پرداخت‌نشده"), (2, "PartiallyPaid", "پرداخت جزئی"), (3, "Paid", "تسویه شده"), (4, "Overpaid", "پرداخت اضافه")])
enum_file("PaymentMethod", "روش پرداخت", [(1, "Cash", "نقدی"), (2, "Card", "کارت‌خوان"), (3, "Online", "آنلاین"), (4, "Cheque", "چک"), (5, "BankTransfer", "انتقال بانکی"), (6, "Insurance", "بیمه")])
enum_file("PaymentRecordStatus", "وضعیت رکورد پرداخت", [(1, "Pending", "در انتظار"), (2, "Completed", "تکمیل شده"), (3, "Failed", "ناموفق"), (4, "Refunded", "برگشتی")])
enum_file("CashRegisterStatus", "وضعیت صندوق", [(1, "Open", "باز"), (2, "Closed", "بسته")])
enum_file("InsuranceType", "نوع شرکت بیمه", [(1, "Public", "عمومی - تأمین اجتماعی"), (2, "Salamat", "سلامت"), (3, "Artesh", "ارتش"), (4, "Private", "خصوصی"), (5, "Supplementary", "تکمیلی")])
enum_file("ClaimStatus", "وضعیت مطالبه بیمه", [(1, "Draft", "پیش‌نویس"), (2, "Submitted", "ارسال شده"), (3, "Approved", "تأیید شده"), (4, "PartiallyApproved", "تأیید جزئی"), (5, "Rejected", "رد شده"), (6, "Paid", "دریافت شده")])
enum_file("ServiceType", "نوع خدمت", [(1, "Visit", "ویزیت"), (2, "Lab", "آزمایش"), (3, "Imaging", "تصویربرداری"), (4, "Procedure", "عمل"), (5, "Consultation", "مشاوره"), (6, "Other", "سایر")])
enum_file("ExpenseCategoryType", "دسته هزینه", [(1, "Purchase", "خرید"), (2, "Administrative", "اداری"), (3, "Utility", "قبوض"), (4, "Salary", "حقوق"), (5, "Rent", "اجاره"), (6, "Maintenance", "تعمیرات"), (7, "Other", "سایر")])
enum_file("DoctorPaymentStatus", "وضعیت کارانه پزشک", [(1, "Calculated", "محاسبه شده"), (2, "Approved", "تأیید شده"), (3, "Paid", "پرداخت شده")])

# ─── حسابداری ───
enum_file("AccountType", "نوع حساب (طبیعت)", [(1, "Asset", "دارایی"), (2, "Liability", "بدهکاری"), (3, "Equity", "حقوق صاحبان سهام"), (4, "Revenue", "درآمد"), (5, "Expense", "هزینه")])
enum_file("AccountLevel", "سطح حساب چهارسطحی", [(1, "Group", "گروه"), (2, "Ledger", "کل"), (3, "Subsidiary", "معین"), (4, "Detail", "تفصیلی")])
enum_file("AccountNature", "طبیعت حساب", [(1, "Debit", "بدهکار"), (2, "Credit", "بستانکار")])
enum_file("JournalEntryStatus", "وضعیت سند حسابداری", [(1, "Draft", "پیش‌نویس"), (2, "Approved", "تأیید شده"), (3, "Voided", "ابطال شده")])
enum_file("JournalReferenceType", "منشأ سند حسابداری", [(1, "Invoice", "فاکتور"), (2, "Payment", "پرداخت"), (3, "Expense", "هزینه"), (4, "DoctorPayment", "کارانه"), (5, "Manual", "دستی")])

# ─── پاراکلینیک ───
enum_file("LabOrderStatus", "وضعیت درخواست آزمایش", [
    (1, "Ordered", "ثبت شده"), (2, "SampleCollected", "نمونه گرفته شده"), (3, "InProgress", "در حال آزمایش"),
    (4, "ResultReady", "نتیجه آماده"), (5, "Verified", "تأیید شده"), (6, "Delivered", "تحویل شده"), (7, "Cancelled", "لغو")])
enum_file("LabItemStatus", "وضعیت قلم آزمایش", [(1, "Pending", "در انتظار"), (2, "InProgress", "در حال انجام"), (3, "Resulted", "نتیجه‌دار"), (4, "Verified", "تأیید شده")])
enum_file("AbnormalFlag", "پرچم غیرطبیعی نتیجه", [(1, "Low", "پایین‌تر از حد"), (2, "High", "بالاتر از حد"), (3, "VeryLow", "خیلی پایین"), (4, "VeryHigh", "خیلی بالا")])
enum_file("ImagingModality", "نوع تصویربرداری", [(1, "Ultrasound", "سونوگرافی"), (2, "XRay", "رادیوگرافی"), (3, "CT", "سی‌تی‌اسکن"), (4, "MRI", "ام‌آر‌آی"), (5, "Mammography", "ماموگرافی")])
enum_file("ImagingOrderStatus", "وضعیت درخواست تصویربرداری", [(1, "Ordered", "ثبت شده"), (2, "Scheduled", "زمان‌بندی شده"), (3, "Performed", "انجام شده"), (4, "Reported", "گزارش شده"), (5, "Cancelled", "لغو")])
enum_file("DentalCondition", "وضعیت دندان", [(1, "Healthy", "سالم"), (2, "Carious", "پوسیده"), (3, "Missing", "کشیده شده"), (4, "Filled", "ترمیم شده"), (5, "Crowned", "روکش شده"), (6, "Implant", "ایمپلنت"), (7, "RootCanal", "عصب‌کشی شده")])
enum_file("TreatmentPlanStatus", "وضعیت طرح درمان", [(1, "Proposed", "پیشنهادی"), (2, "Approved", "تأیید شده"), (3, "InProgress", "در حال انجام"), (4, "Completed", "تکمیل شده"), (5, "Cancelled", "لغو")])
enum_file("SessionStatus", "وضعیت جلسه درمانی", [(1, "Scheduled", "زمان‌بندی شده"), (2, "Completed", "انجام شده"), (3, "Cancelled", "لغو"), (4, "Missed", "غیبت بیمار")])

# ─── داروخانه ───
enum_file("PharmacyTransactionType", "نوع تراکنش داروخانه", [(1, "StockIn", "ورود موجودی"), (2, "StockOut", "خروج موجودی"), (3, "Return", "برگشت"), (4, "Adjustment", "اصلاح"), (5, "Dispense", "تحویل نسخه")])
enum_file("PharmacyReferenceType", "منشأ تراکنش داروخانه", [(1, "Prescription", "نسخه"), (2, "PurchaseOrder", "سفارش خرید"), (3, "StockTaking", "انبارگردانی"), (4, "Manual", "دستی")])
enum_file("SupplierType", "نوع تأمین‌کننده", [(1, "Manufacturer", "شرکت سازنده"), (2, "Distributor", "پخش"), (3, "Importer", "واردات"), (4, "Other", "سایر")])
enum_file("PurchaseOrderStatus", "وضعیت سفارش خرید", [(1, "Draft", "پیش‌نویس"), (2, "Submitted", "ارسال شده"), (3, "PartiallyReceived", "دریافت جزئی"), (4, "Received", "دریافت کامل"), (5, "Cancelled", "لغو")])
enum_file("StockTakingStatus", "وضعیت انبارگردانی", [(1, "InProgress", "در حال شمارش"), (2, "Completed", "تکمیل"), (3, "Approved", "تأیید شده")])

# ─── پرسنل و RBAC ───
enum_file("EmploymentType", "نوع استخدام", [(1, "FullTime", "تمام‌وقت"), (2, "PartTime", "پاره‌وقت"), (3, "Contractor", "پیمانی"), (4, "Intern", "کارآموز")])
enum_file("ContractType", "نوع قرارداد", [(1, "Permanent", "دائم"), (2, "Temporary", "موقت"), (3, "Project", "پروژه‌ای")])
enum_file("ContractStatus", "وضعیت قرارداد", [(1, "Active", "فعال"), (2, "Expired", "منقضی"), (3, "Terminated", "فسخ شده")])
enum_file("AttendanceStatus", "وضعیت حضور", [(1, "Present", "حاضر"), (2, "Absent", "غایب"), (3, "Leave", "مرخصی"), (4, "Mission", "مأموریت"), (5, "Late", "تأخیر")])
enum_file("LeaveType", "نوع مرخصی", [(1, "Vacation", "استحقاقی"), (2, "SickLeave", "استعلاجی"), (3, "Unpaid", "بدون حقوق"), (4, "Mission", "مأموریت")])
enum_file("LeaveStatus", "وضعیت مرخصی", [(1, "Pending", "در انتظار"), (2, "Approved", "تأیید شده"), (3, "Rejected", "رد شده"), (4, "Cancelled", "لغو")])
enum_file("SalaryStatus", "وضعیت حقوق", [(1, "Calculated", "محاسبه شده"), (2, "Approved", "تأیید شده"), (3, "Paid", "پرداخت شده")])
enum_file("UserStatus", "وضعیت کاربر", [(1, "Active", "فعال"), (2, "Inactive", "غیرفعال"), (3, "Suspended", "معلق")])
enum_file("DeviceType", "نوع دستگاه نشست کاربر", [(1, "Desktop", "دسکتاپ"), (2, "Laptop", "لپ‌تاپ"), (3, "Tablet", "تبلت"), (4, "Other", "سایر")])

# ─── ارتباطات ───
enum_file("SmsStatus", "وضعیت پیامک", [(1, "Queued", "در صف"), (2, "Sent", "ارسال شده"), (3, "Delivered", "تحویل شده"), (4, "Failed", "ناموفق")])
enum_file("SmsMessageType", "نوع پیامک", [(1, "AppointmentReminder", "یادآوری نوبت"), (2, "Welcome", "خوش‌آمدگویی"), (3, "LabResult", "نتیجه آزمایش"), (4, "PaymentReceipt", "رسید پرداخت"), (5, "Marketing", "تبلیغاتی"), (6, "System", "سیستمی")])
enum_file("SmsRecipientType", "نوع گیرنده پیامک", [(1, "Patient", "بیمار"), (2, "Doctor", "پزشک"), (3, "Staff", "کارمند"), (4, "Other", "سایر")])
enum_file("SmsReferenceType", "منشأ پیامک", [(1, "Appointment", "نوبت"), (2, "Visit", "ویزیت"), (3, "LabOrder", "آزمایش"), (4, "Invoice", "فاکتور"), (5, "Manual", "دستی")])
enum_file("SmsTemplateType", "نوع قالب پیامک", [(1, "Reminder", "یادآوری"), (2, "Confirmation", "تأیید"), (3, "Result", "نتیجه"), (4, "Receipt", "رسید"), (5, "Custom", "سفارشی")])
enum_file("CallDirection", "جهت تماس", [(1, "Inbound", "ورودی"), (2, "Outbound", "خروجی")])
enum_file("CallResult", "نتیجه تماس", [(1, "Answered", "پاسخ داده شد"), (2, "NoAnswer", "پاسخ نداد"), (3, "Busy", "مشغول"), (4, "Callback", "بازگشت تماس")])
enum_file("SurveyType", "نوع نظرسنجی", [(1, "Satisfaction", "رضایت"), (2, "Service", "خدمات"), (3, "Doctor", "پزشک")])
enum_file("NotificationType", "نوع اعلان", [(1, "Info", "اطلاع"), (2, "Warning", "هشدار"), (3, "Error", "خطا"), (4, "Success", "موفقیت"), (5, "License", "لایسنس"), (6, "Stock", "موجودی")])
enum_file("NotificationPriority", "اولویت اعلان", [(1, "Low", "کم"), (2, "Normal", "عادی"), (3, "High", "زیاد"), (4, "Critical", "بحرانی")])

# ─── زیرساخت ───
enum_file("DocumentReferenceType", "منشأ سند/فایل", [(1, "Patient", "بیمار"), (2, "Visit", "ویزیت"), (3, "LabOrder", "آزمایش"), (4, "ImagingOrder", "تصویربرداری"), (5, "Invoice", "فاکتور"), (6, "Employee", "کارمند"), (7, "Other", "سایر")])
enum_file("AuditActionType", "نوع عملیات حسابرسی", [(1, "Create", "ایجاد"), (2, "Update", "ویرایش"), (3, "Delete", "حذف"), (4, "View", "مشاهده"), (5, "Login", "ورود"), (6, "Logout", "خروج"), (7, "Print", "چاپ"), (8, "Export", "خروجی")])
enum_file("AuditSeverity", "شدت رخداد حسابرسی", [(1, "Info", "اطلاع"), (2, "Warning", "هشدار"), (3, "Critical", "بحرانی")])
enum_file("ErrorLogLevel", "سطح لاگ خطا", [(1, "Debug", "عیب‌یابی"), (2, "Info", "اطلاع"), (3, "Warning", "هشدار"), (4, "Error", "خطا"), (5, "Fatal", "مرگبار")])
enum_file("BackupType", "نوع پشتیبان‌گیری", [(1, "Full", "کامل"), (2, "Differential", "افزایشی"), (3, "Log", "لاگ تراکنش")])
enum_file("BackupStatus", "وضعیت پشتیبان", [(1, "InProgress", "در حال انجام"), (2, "Success", "موفق"), (3, "Failed", "ناموفق")])
enum_file("StorageType", "محل ذخیره‌سازی", [(1, "Local", "محلی"), (2, "NetworkShare", "شبکه"), (3, "Cloud", "ابر")])
enum_file("SettingDataType", "نوع داده تنظیمات", [(1, "String", "متن"), (2, "Number", "عدد"), (3, "Boolean", "منطقی"), (4, "Date", "تاریخ"), (5, "Json", "جیسون")])

# ─── لایسنس ───
enum_file("LicenseType", "نوع لایسنس", [(1, "Subscription", "اشتراکی"), (2, "Perpetual", "دائمی")])
enum_file("LicenseStatus", "وضعیت لایسنس", [(1, "Active", "فعال"), (2, "Suspended", "معلق"), (3, "Expired", "منقضی"), (4, "Revoked", "ابطال‌شده")])
enum_file("LicenseValidationResult", "نتیجه اعتبارسنجی لایسنس", [(1, "Valid", "معتبر"), (2, "Expired", "منقضی"), (3, "InvalidSignature", "امضای نامعتبر"), (4, "HardwareMismatch", "عدم تطابق سخت‌افزار"), (5, "Revoked", "ابطال‌شده از راه دور"), (6, "NotFound", "یافت نشد")])

# ─── یکپارچه‌سازی دولتی ───
enum_file("SepasTransactionType", "نوع تراکنش سپاس", [(1, "EncounterRegistration", "ثبت ویزیت"), (2, "EncounterUpdate", "به‌روزرسانی ویزیت"), (3, "PatientRegistration", "ثبت بیمار")])
enum_file("SyncStatus", "وضعیت همگام‌سازی", [(1, "Pending", "در انتظار"), (2, "Synced", "همگام شده"), (3, "Failed", "ناموفق"), (4, "RetryScheduled", "بازتلاش زمان‌بندی‌شده")])
enum_file("SibServiceType", "نوع خدمت سیب", [(1, "Referral", "ارجاع"), (2, "Prescription", "نسخه"), (3, "LabResult", "نتیجه آزمایش")])
enum_file("ReferralType", "نوع ارجاع", [(1, "Inbound", "وارده"), (2, "Outbound", "صادره")])
enum_file("ReferralStatus", "وضعیت ارجاع", [(1, "Created", "ایجاد شده"), (2, "Referred", "ارجاع شده"), (3, "Completed", "تکمیل"), (4, "Cancelled", "لغو")])
enum_file("ElectronicPrescriptionStatus", "وضعیت نسخه الکترونیک", [(1, "Pending", "در انتظار ارسال"), (2, "Sent", "ارسال شده"), (3, "Registered", "ثبت‌شده در سامانه"), (4, "Rejected", "رد شده"), (5, "Cancelled", "لغو")])

# ─── گزارش‌ها ───
enum_file("ReportFrequency", "دوره تکرار گزارش", [(1, "Daily", "روزانه"), (2, "Weekly", "هفتگی"), (3, "Monthly", "ماهانه"), (4, "Quarterly", "فصلی"), (5, "Yearly", "سالانه")])

print("✅ بخش ۲: شمارشی‌ها — کامل شد")
