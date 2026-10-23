# -*- coding: utf-8 -*-
"""
╔══════════════════════════════════════════════════════════════════════════╗
║  مشخصاتِ ۸۰ فرم و صفحه‌ی نرم‌افزار سهند  (Sahand ClinicPro)               ║
║  این فایل «تنها منبعِ حقیقت» برای مولدِ redesign/tools/generate.py است.    ║
╚══════════════════════════════════════════════════════════════════════════╝

قالبِ فیلد:  "prop:type:label[:*]"
    type ∈ text | number | money | date | combo | check | textarea
            | lookup | phone | national | password | file | time | barcode

قالبِ ستون:  (binding, header, width)   عرض: عدد یا "*" برای ستونِ کش‌دار
قالبِ فیلتر: (name, label, control)     control ∈ search | combo | date | check
قالبِ اقدام: (command, label, style)    style ∈ Primary | Secondary | Ghost | Danger | Success
قالبِ شاخص:  (icon, label, value)
"""

FORMS = []


def F(name, title, icon, kind, subtitle="", group="عمومی",
      kpis=None, filters=None, cols=None, sections=None,
      actions=None, tabs=None, notes=None):
    """ثبت مشخصات یک فرم در فهرست سراسری."""
    FORMS.append(dict(
        name=name, title=title, icon=icon, kind=kind, subtitle=subtitle,
        group=group,
        kpis=kpis or [],
        filters=filters or [],
        cols=cols or [],
        sections=sections or [],
        actions=actions or [],
        tabs=tabs or [],
        notes=notes or "",
    ))


# ══════════════════════════════════════════════════════════════════════
#  🏠 پیشخوان و ناوبری
# ══════════════════════════════════════════════════════════════════════

F("DashboardView", "پیشخوان", "🏠", "dashboard",
  "نمای کلی وضعیت امروز درمانگاه", "پیشخوان",
  kpis=[("👥", "بیماران امروز", "۰"), ("📅", "نوبت‌های امروز", "۰"),
        ("💵", "درآمد امروز", "۰"), ("⏱", "میانگین انتظار", "۰")],
  actions=[("NewReception", "پذیرش جدید", "Primary"),
           ("NewAppointment", "نوبت جدید", "Secondary"),
           ("Refresh", "تازه‌سازی", "Ghost")],
  notes="چهار کارت شاخص در بالا، صف انتظار زنده در وسط، نمودار درآمد ۳۰ روزه و نوار هشدارها در پایین.")

F("LoginWindow", "ورود به برنامه", "🔐", "login",
  "احراز هویت کاربر", "پیشخوان",
  sections=[("ورود", "🔐", [
      "UserName:text:نام کاربری:*",
      "Password:password:رمز عبور:*",
      "RememberMe:check:مرا به خاطر بسپار",
  ])],
  actions=[("Login", "ورود", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("SplashWindow", "در حال بارگذاری", "🏥", "splash",
  "صفحه‌ی آغازین برنامه", "پیشخوان")

F("MainWindow", "پنجره‌ی اصلی", "🖥", "shell",
  "قاب اصلی برنامه شامل ناوبری و ناحیه‌ی محتوا", "پیشخوان",
  notes="شامل نوار کناری (Rail)، نوار بالا با جستجوی یکپارچه، اعلان‌ها و انتخاب‌گر تم و زبان.")

F("ChangePasswordWindow", "تغییر رمز عبور", "🔑", "form",
  "تغییر رمز عبور کاربر جاری", "پیشخوان",
  sections=[("رمز عبور", "🔑", [
      "CurrentPassword:password:رمز فعلی:*",
      "NewPassword:password:رمز جدید:*",
      "ConfirmPassword:password:تکرار رمز جدید:*",
  ])],
  actions=[("Save", "تغییر رمز", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("SettingsView", "تنظیمات", "⚙", "settings",
  "تنظیمات سراسری برنامه", "پیشخوان",
  tabs=[("عمومی", "🏥"), ("ظاهر", "🎨"), ("چاپ", "🖨"),
        ("اعلان‌ها", "🔔"), ("پیامک", "📨"), ("پشتیبان", "💾"),
        ("پیشرفته", "🧩")],
  actions=[("Save", "ذخیره تنظیمات", "Primary"), ("Reset", "بازگشت به پیش‌فرض", "Secondary")])

F("ShortcutPickerWindow", "انتخاب میانبر", "⌨", "dialog",
  "تعریف میانبر صفحه‌کلید", "پیشخوان",
  sections=[("میانبر", "⌨", [
      "CommandName:text:فرمان",
      "Key:text:کلید ترکیبی",
  ])],
  actions=[("Save", "ثبت", "Primary"), ("Clear", "پاک کردن", "Secondary")])

F("NotificationsView", "اعلان‌ها", "🔔", "list",
  "مرکز اعلان‌های سیستم", "پیشخوان",
  filters=[("search", "جستجو", "search"), ("type", "نوع", "combo"), ("read", "وضعیت", "combo")],
  cols=[("Icon", "", "40"), ("Title", "عنوان", "*"), ("Body", "متن", "*"),
        ("CreatedAt", "زمان", "140"), ("Priority", "اولویت", "90")],
  actions=[("MarkAllRead", "خواندن همه", "Secondary"), ("Refresh", "تازه‌سازی", "Ghost")])

F("GuidedTourWindow", "راهنمای تعاملی", "🧭", "dialog",
  "آموزش گام‌به‌گام محیط برنامه", "پیشخوان",
  actions=[("Next", "بعدی", "Primary"), ("Skip", "رد کردن", "Ghost")])

F("TrainingCenterView", "مرکز آموزش", "🎓", "list",
  "محتوای آموزشی و دوره‌ها", "پیشخوان",
  filters=[("search", "جستجو", "search"), ("category", "دسته", "combo")],
  cols=[("Title", "عنوان", "*"), ("Category", "دسته", "140"),
        ("Duration", "مدت", "90"), ("Progress", "پیشرفت", "120")],
  actions=[("Open", "مشاهده", "Primary")])

# ══════════════════════════════════════════════════════════════════════
#  👥 بیماران
# ══════════════════════════════════════════════════════════════════════

F("PatientsView", "بیماران", "👥", "list",
  "مدیریت پرونده‌ها و جستجوی بیماران", "بیماران",
  kpis=[("👥", "کل بیماران", "۰"), ("🆕", "جدید این ماه", "۰"), ("✅", "فعال", "۰")],
  filters=[("search", "جستجوی نام/کد ملی/موبایل", "search"),
           ("branch", "شعبه", "combo"), ("status", "وضعیت", "combo"),
           ("gender", "جنسیت", "combo"), ("fromDate", "از تاریخ", "date")],
  cols=[("Code", "کد", "90"), ("FullName", "نام بیمار", "*"),
        ("NationalCode", "کد ملی", "130"), ("MobileNumber", "موبایل", "130"),
        ("Age", "سن", "60"), ("Gender", "جنسیت", "80"),
        ("LastVisitDate", "آخرین ویزیت", "130"), ("Balance", "مانده", "110")],
  actions=[("New", "+ بیمار جدید", "Primary"), ("Export", "خروجی Excel", "Secondary"),
           ("Print", "چاپ کارت", "Secondary")],
  notes="جستجو با تأخیر ۳۵۰ میلی‌ثانیه انجام می‌شود و امکان فیلتر پیشرفته هم وجود دارد.")

F("PatientFormWindow", "فرم بیمار", "👤", "form",
  "ایجاد یا ویرایش پرونده‌ی بیمار", "بیماران",
  sections=[
      ("اطلاعات شناسایی", "🪪", [
          "FirstName:text:نام:*", "LastName:text:نام خانوادگی:*",
          "NationalCode:national:کد ملی:*", "Gender:combo:جنسیت:*",
          "BirthDate:date:تاریخ تولد", "MaritalStatus:combo:وضعیت تأهل",
      ]),
      ("اطلاعات تماس", "📞", [
          "MobileNumber:phone:موبایل:*", "PhoneNumber:phone:تلفن ثابت",
          "Email:text:ایمیل", "City:combo:شهر", "Address:textarea:نشانی",
      ]),
      ("اطلاعات تکمیلی", "📋", [
          "BloodType:combo:گروه خون", "Allergies:textarea:حساسیت‌ها",
          "ChronicConditions:textarea:بیماری‌های زمینه‌ای",
          "InsuranceId:combo:بیمه", "InsuranceNumber:text:شماره بیمه",
          "ReferredBy:text:معرف",
      ]),
      ("تماس اضطراری", "🚨", [
          "EmergencyContactName:text:نام فرد", "EmergencyContactPhone:phone:شماره تماس",
          "EmergencyRelation:combo:نسبت",
      ]),
  ],
  actions=[("Save", "ذخیره", "Primary"), ("SaveAndNew", "ذخیره و جدید", "Secondary"),
           ("Cancel", "انصراف", "Ghost")])

F("PatientDetailWindow", "پرونده‌ی بیمار", "📁", "detail",
  "نمای جامع پرونده‌ی بالینی بیمار", "بیماران",
  kpis=[("🩺", "تعداد ویزیت", "۰"), ("💊", "نسخه‌ها", "۰"),
        ("🧪", "آزمایش‌ها", "۰"), ("💵", "مجموع پرداخت", "۰")],
  tabs=[("خلاصه", "📋"), ("ویزیت‌ها", "🩺"), ("نسخه‌ها", "💊"),
        ("آزمایش‌ها", "🧪"), ("تصویربرداری", "🩻"),
        ("مالی", "💵"), ("مستندات", "📎"), ("زمان‌خط", "🕐")],
  actions=[("NewVisit", "ویزیت جدید", "Primary"), ("Edit", "ویرایش", "Secondary"),
           ("PrintRecord", "چاپ پرونده", "Secondary")])

F("AdvancedEmrView", "پرونده بالینی پیشرفته", "❤", "detail",
  "نمودارهای حیاتی، روندها و محاسبات بالینی", "بیماران",
  kpis=[("💓", "فشار خون", "—"), ("🌡", "دما", "—"),
        ("⚖", "BMI", "—"), ("🩸", "قند خون", "—")],
  tabs=[("علائم حیاتی", "💓"), ("نمودار رشد", "📈"),
        ("محاسبه‌گرها", "🧮"), ("هشدارها", "⚠")],
  actions=[("AddVital", "ثبت علائم", "Primary"), ("Refresh", "تازه‌سازی", "Ghost")])

F("PhotoViewerWindow", "نمایش تصویر", "🖼", "dialog",
  "مشاهده‌ی تصویر بیمار یا مدرک", "بیماران",
  actions=[("ZoomIn", "بزرگ‌نمایی", "Ghost"), ("ZoomOut", "کوچک‌نمایی", "Ghost"),
           ("Close", "بستن", "Secondary")])

# ══════════════════════════════════════════════════════════════════════
#  📅 نوبت‌دهی
# ══════════════════════════════════════════════════════════════════════

F("AppointmentsView", "نوبت‌دهی", "📅", "list",
  "زمان‌بندی و مدیریت نوبت‌ها", "نوبت‌دهی",
  kpis=[("📅", "نوبت امروز", "۰"), ("⏳", "در انتظار", "۰"),
        ("✅", "تأیید شده", "۰"), ("❌", "لغو شده", "۰")],
  filters=[("search", "جستجوی بیمار", "search"), ("doctor", "پزشک", "combo"),
           ("date", "تاریخ", "date"), ("status", "وضعیت", "combo"), ("room", "اتاق", "combo")],
  cols=[("Time", "ساعت", "80"), ("PatientName", "بیمار", "*"),
        ("DoctorName", "پزشک", "160"), ("Service", "خدمت", "160"),
        ("Room", "اتاق", "80"), ("Status", "وضعیت", "110"), ("Duration", "مدت", "80")],
  actions=[("New", "+ نوبت جدید", "Primary"), ("TodayView", "نمای روز", "Secondary"),
           ("WeekView", "نمای هفته", "Secondary"), ("SendReminder", "ارسال یادآوری", "Secondary")],
  notes="نمای تقویمی با شبکه‌ی زمانی؛ تغییر وضعیت با کشیدن و رها کردن (Drag & Drop).")

F("AppointmentFormWindow", "فرم نوبت", "🗓", "form",
  "ایجاد یا ویرایش نوبت", "نوبت‌دهی",
  sections=[
      ("بیمار و پزشک", "👤", [
          "PatientId:lookup:بیمار:*", "DoctorId:combo:پزشک:*",
          "ServiceId:combo:خدمت:*",
      ]),
      ("زمان‌بندی", "🕐", [
          "AppointmentDate:date:تاریخ نوبت:*", "StartTime:time:ساعت شروع:*",
          "DurationMinutes:number:مدت (دقیقه)", "RoomId:combo:اتاق",
      ]),
      ("توضیحات", "📝", [
          "Reason:text:علت مراجعه", "Notes:textarea:یادداشت",
          "IsFirstVisit:check:ویزیت اول",
      ]),
  ],
  actions=[("Save", "ذخیره نوبت", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("DoctorVisitView", "ویزیت پزشک", "🩺", "detail",
  "صفحه‌ی کاری روزانه‌ی پزشک", "نوبت‌دهی",
  kpis=[("👥", "نوبت‌های امروز", "۰"), ("✅", "ویزیت شده", "۰"), ("⏳", "باقی‌مانده", "۰")],
  cols=[("Time", "ساعت", "80"), ("PatientName", "بیمار", "*"),
        ("Reason", "علت", "*"), ("Status", "وضعیت", "110")],
  actions=[("StartVisit", "شروع ویزیت", "Primary"), ("CallNext", "فراخوان بعدی", "Success")])

# ══════════════════════════════════════════════════════════════════════
#  🛎 پذیرش و صف
# ══════════════════════════════════════════════════════════════════════

F("ReceptionsView", "پذیرش و صف", "🛎", "list",
  "پذیرش بیماران و مدیریت صف انتظار", "پذیرش",
  kpis=[("🛎", "در صف", "۰"), ("✅", "پذیرش شده", "۰"),
        ("⏱", "میانگین انتظار", "۰"), ("💵", "دریافت امروز", "۰")],
  filters=[("search", "جستجوی بیمار", "search"), ("doctor", "پزشک", "combo"),
           ("status", "وضعیت", "combo"), ("date", "تاریخ", "date")],
  cols=[("QueueNumber", "نوبت", "70"), ("PatientName", "بیمار", "*"),
        ("DoctorName", "پزشک", "150"), ("Service", "خدمت", "150"),
        ("ArrivalTime", "زمان ورود", "110"), ("WaitMinutes", "انتظار", "90"),
        ("Status", "وضعیت", "110"), ("Amount", "مبلغ", "120")],
  actions=[("NewReception", "+ پذیرش جدید", "Primary"), ("CallNext", "فراخوان بعدی", "Success"),
           ("Pay", "دریافت وجه", "Secondary"), ("CancelReception", "لغو پذیرش", "Danger")],
  notes="صف به‌صورت بلادرنگ با SignalR به‌روزرسانی می‌شود.")

F("ReceptionFormWindow", "فرم پذیرش", "📝", "form",
  "ثبت پذیرش جدید", "پذیرش",
  sections=[
      ("بیمار", "👤", [
          "PatientId:lookup:بیمار:*", "DoctorId:combo:پزشک:*",
      ]),
      ("خدمات", "🩺", [
          "ServiceId:combo:خدمت اصلی:*", "AdditionalServices:lookup:خدمات تکمیلی",
          "InsuranceId:combo:بیمه", "ReferralNumber:text:شماره ارجاع",
      ]),
      ("پرداخت", "💵", [
          "TotalAmount:money:مبلغ کل", "InsuranceShare:money:سهم بیمه",
          "PatientShare:money:سهم بیمار", "DiscountAmount:money:تخفیف",
      ]),
  ],
  actions=[("Save", "ثبت پذیرش", "Primary"), ("SaveAndPay", "ثبت و دریافت", "Success"),
           ("Cancel", "انصراف", "Ghost")])

F("ReceptionPayWindow", "دریافت وجه", "💳", "form",
  "ثبت پرداختِ پذیرش", "پذیرش",
  sections=[
      ("مبالغ", "💵", [
          "TotalAmount:money:مبلغ قابل پرداخت", "PaidAmount:money:مبلغ پرداختی:*",
          "RemainingAmount:money:باقی‌مانده",
      ]),
      ("روش پرداخت", "💳", [
          "PaymentMethod:combo:روش پرداخت:*", "ReferenceNumber:text:شماره مرجع",
          "CardNumber:text:شماره کارت",
      ]),
  ],
  actions=[("Confirm", "تأیید پرداخت", "Success"), ("Print", "چاپ رسید", "Secondary"),
           ("Cancel", "انصراف", "Ghost")])

F("PaymentWindow", "پرداخت", "💰", "form",
  "دریافت وجه عمومی", "پذیرش",
  sections=[("پرداخت", "💰", [
      "Amount:money:مبلغ:*", "PaymentMethod:combo:روش پرداخت:*",
      "Description:textarea:توضیحات",
  ])],
  actions=[("Confirm", "تأیید", "Success"), ("Cancel", "انصراف", "Ghost")])

F("ShiftsView", "شیفت‌ها", "🕐", "list",
  "مدیریت شیفت‌های کاری", "پذیرش",
  filters=[("date", "تاریخ", "date"), ("user", "کاربر", "combo"), ("status", "وضعیت", "combo")],
  cols=[("ShiftName", "شیفت", "120"), ("UserName", "کاربر", "160"),
        ("StartTime", "شروع", "110"), ("EndTime", "پایان", "110"),
        ("TotalAmount", "مبلغ کل", "140"), ("Status", "وضعیت", "110")],
  actions=[("OpenShift", "گشایش شیفت", "Primary"), ("CloseShift", "بستن شیفت", "Secondary")])

F("ShiftCloseWindow", "بستن شیفت", "🔒", "form",
  "تسویه و بستن شیفت جاری", "پذیرش",
  kpis=[("💵", "مبلغ مورد انتظار", "۰"), ("💰", "مبلغ واقعی", "۰"), ("⚖", "اختلاف", "۰")],
  sections=[("تسویه", "🔒", [
      "ExpectedAmount:money:مبلغ مورد انتظار",
      "CountedAmount:money:مبلغ شمارش‌شده:*",
      "DifferenceAmount:money:اختلاف",
      "ToleranceReason:textarea:علت اختلاف",
  ])],
  actions=[("Confirm", "بستن شیفت", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("ShiftReceiptPreviewWindow", "پیش‌نمایش رسید شیفت", "🧾", "dialog",
  "پیش‌نمایش رسید تسویه‌ی شیفت", "پذیرش",
  actions=[("Print", "چاپ", "Primary"), ("Close", "بستن", "Secondary")])

F("RoomsView", "اتاق‌ها", "🚪", "list",
  "مدیریت اتاق‌های معاینه", "پذیرش",
  cols=[("RoomNumber", "شماره", "90"), ("Name", "نام اتاق", "*"),
        ("DoctorName", "پزشک مستقر", "180"), ("Status", "وضعیت", "120")],
  actions=[("New", "+ اتاق جدید", "Primary"), ("Manage", "مدیریت اتاق‌ها", "Secondary")])

F("RoomsManagerWindow", "مدیریت اتاق‌ها", "🗂", "dialog",
  "تخصیص پزشکان به اتاق‌ها", "پذیرش",
  actions=[("Save", "ذخیره", "Primary"), ("Cancel", "انصراف", "Ghost")])

# ══════════════════════════════════════════════════════════════════════
#  🩺 ویزیت و درمان
# ══════════════════════════════════════════════════════════════════════

F("VisitWindow", "ویزیت (SOAP)", "🩻", "form",
  "ثبت شرح‌حال، معاینه، تشخیص و طرح درمان", "ویزیت",
  sections=[
      ("شرح‌حال (Subjective)", "💬", [
          "ChiefComplaint:textarea:شکایت اصلی:*",
          "HistoryOfPresentIllness:textarea:شرح بیماری فعلی",
          "PastMedicalHistory:textarea:سوابق پزشکی",
          "DrugHistory:textarea:سوابق دارویی",
          "FamilyHistory:textarea:سوابق خانوادگی",
          "SocialHistory:textarea:سوابق اجتماعی",
      ]),
      ("معاینه (Objective)", "🔍", [
          "BloodPressure:text:فشار خون", "Pulse:number:نبض",
          "Temperature:number:دما", "RespiratoryRate:number:تعداد تنفس",
          "OxygenSaturation:number:اشباع اکسیژن", "Weight:number:وزن (کیلوگرم)",
          "Height:number:قد (سانتی‌متر)", "Bmi:text:BMI",
          "PhysicalExam:textarea:یافته‌های معاینه",
      ]),
      ("تشخیص (Assessment)", "🎯", [
          "PrimaryDiagnosisCode:lookup:تشخیص اصلی (ICD-10):*",
          "SecondaryDiagnoses:lookup:تشخیص‌های فرعی",
          "DifferentialDiagnosis:textarea:تشخیص‌های افتراقی",
          "AssessmentNotes:textarea:یادداشت ارزیابی",
      ]),
      ("طرح درمان (Plan)", "📋", [
          "TreatmentPlan:textarea:طرح درمان:*",
          "Prescription:textarea:داروها",
          "LabOrders:lookup:درخواست آزمایش",
          "ImagingOrders:lookup:درخواست تصویربرداری",
          "Referral:lookup:ارجاع",
          "FollowUpDate:date:تاریخ پیگیری",
          "PatientEducation:textarea:آموزش به بیمار",
      ]),
  ],
  actions=[("Save", "ذخیره ویزیت", "Primary"),
           ("SaveAndPrescribe", "ذخیره و نسخه", "Success"),
           ("SaveAndLab", "ذخیره و آزمایش", "Secondary"),
           ("CancelVisit", "لغو ویزیت", "Danger")])

F("VisitsView", "فهرست ویزیت‌ها", "📋", "list",
  "تاریخچه‌ی ویزیت‌های انجام‌شده", "ویزیت",
  filters=[("search", "جستجوی بیمار", "search"), ("doctor", "پزشک", "combo"),
           ("fromDate", "از تاریخ", "date"), ("toDate", "تا تاریخ", "date")],
  cols=[("VisitDate", "تاریخ", "120"), ("PatientName", "بیمار", "*"),
        ("DoctorName", "پزشک", "160"), ("Diagnosis", "تشخیص", "*"),
        ("Status", "وضعیت", "110")],
  actions=[("New", "ویزیت جدید", "Primary"), ("Open", "مشاهده", "Secondary")])

F("DentistryView", "دندانپزشکی", "🦷", "detail",
  "چارت دندانی و خدمات دندانپزشکی", "ویزیت",
  kpis=[("🦷", "دندان‌های درمان‌شده", "۰"), ("⚠", "نیازمند درمان", "۰")],
  tabs=[("چارت دندانی", "🦷"), ("خدمات", "🔧"), ("تصاویر", "🩻")],
  actions=[("Save", "ذخیره چارت", "Primary"), ("Print", "چاپ چارت", "Secondary")],
  notes="چارت تعاملی ۳۲ دندان با استاندارد FDI و ثبت وضعیت هر دندان.")

F("OphthalmologyView", "چشم‌پزشکی", "👁", "detail",
  "معاینات و اندازه‌گیری‌های چشمی", "ویزیت",
  sections=[("معاینه", "👁", [
      "RightEyeVision:text:دید چشم راست", "LeftEyeVision:text:دید چشم چپ",
      "RightEyePressure:number:فشار چشم راست", "LeftEyePressure:number:فشار چشم چپ",
      "Notes:textarea:یادداشت",
  ])],
  actions=[("Save", "ذخیره", "Primary")])

F("PhysiotherapyView", "فیزیوتراپی", "🏃", "detail",
  "برنامه‌ی تمرینات و جلسات فیزیوتراپی", "ویزیت",
  kpis=[("🏃", "جلسات انجام‌شده", "۰"), ("📅", "جلسات باقی‌مانده", "۰")],
  cols=[("SessionNumber", "جلسه", "80"), ("Date", "تاریخ", "120"),
        ("Exercises", "تمرینات", "*"), ("Duration", "مدت", "90")],
  actions=[("NewSession", "جلسه جدید", "Primary")])

F("ComplementaryView", "خدمات تکمیلی", "➕", "list",
  "خدمات پاراکلینیکی و ارجاعات", "ویزیت",
  filters=[("search", "جستجو", "search"), ("type", "نوع خدمت", "combo")],
  cols=[("ServiceName", "خدمت", "*"), ("Provider", "مرکز ارائه‌دهنده", "200"),
        ("RequestDate", "تاریخ درخواست", "130"), ("Status", "وضعیت", "110")],
  actions=[("New", "درخواست جدید", "Primary")])

F("ConsumablesView", "لوازم مصرفی", "🧰", "list",
  "ثبت مصرف لوازم در اقدامات", "ویزیت",
  cols=[("ItemName", "کالا", "*"), ("Quantity", "مقدار", "90"),
        ("Unit", "واحد", "80"), ("VisitDate", "تاریخ", "120")],
  actions=[("Add", "ثبت مصرف", "Primary")])

# ══════════════════════════════════════════════════════════════════════
#  💊 نسخه و دارو
# ══════════════════════════════════════════════════════════════════════

F("PrescriptionsView", "نسخه‌ها", "💊", "list",
  "فهرست نسخه‌های صادرشده", "نسخه",
  filters=[("search", "جستجوی بیمار", "search"), ("doctor", "پزشک", "combo"),
           ("date", "تاریخ", "date"), ("status", "وضعیت", "combo")],
  cols=[("PrescriptionNumber", "شماره", "100"), ("PatientName", "بیمار", "*"),
        ("DoctorName", "پزشک", "150"), ("IssueDate", "تاریخ", "120"),
        ("DrugCount", "تعداد دارو", "90"), ("Status", "وضعیت", "110")],
  actions=[("New", "+ نسخه جدید", "Primary"), ("Print", "چاپ", "Secondary"),
           ("SendEPrescription", "ارسال به نسخه الکترونیک", "Secondary")])

F("PrescriptionFormWindow", "فرم نسخه", "💊", "form",
  "صدور نسخه با بررسی تداخل دارویی", "نسخه",
  sections=[
      ("بیمار", "👤", [
          "PatientId:lookup:بیمار:*", "VisitId:lookup:ویزیت مرتبط",
          "Diagnosis:text:تشخیص",
      ]),
      ("اقلام نسخه", "💊", [
          "DrugId:lookup:دارو:*", "Dosage:text:دوز",
          "Frequency:text:دفعات مصرف", "DurationDays:number:تعداد روز",
          "Quantity:number:تعداد", "Instructions:textarea:دستور مصرف",
      ]),
      ("بررسی ایمنی", "⚠", [
          "InteractionWarnings:check:هشدار تداخل",
          "AllergyWarnings:check:هشدار حساسیت",
          "OverrideReason:textarea:علت نادیده‌گرفتن هشدار",
      ]),
  ],
  actions=[("Save", "صدور نسخه", "Primary"), ("SaveAndPrint", "صدور و چاپ", "Success"),
           ("Cancel", "انصراف", "Ghost")],
  notes="هشدار تداخل داروییِ بحرانی به‌صورت دیالوگِ مسدودکننده نمایش داده می‌شود.")

F("PrescriptionDetailWindow", "جزئیات نسخه", "📄", "detail",
  "مشاهده‌ی جزئیات و چاپ نسخه", "نسخه",
  actions=[("Print", "چاپ نسخه", "Primary"), ("Edit", "ویرایش", "Secondary"),
           ("Close", "بستن", "Ghost")])

F("EPrescriptionView", "نسخه الکترونیک", "📲", "list",
  "ارسال و پیگیری نسخه‌های الکترونیک", "نسخه",
  kpis=[("📲", "ارسال‌شده", "۰"), ("✅", "تأیید شده", "۰"), ("❌", "رد شده", "۰")],
  filters=[("search", "جستجو", "search"), ("status", "وضعیت", "combo"), ("date", "تاریخ", "date")],
  cols=[("TrackingCode", "کد رهگیری", "150"), ("PatientName", "بیمار", "*"),
        ("SentAt", "زمان ارسال", "140"), ("Status", "وضعیت", "110")],
  actions=[("Send", "ارسال", "Primary"), ("RefreshStatus", "به‌روزرسانی وضعیت", "Secondary")])

F("PharmacyView", "داروخانه", "🏥", "list",
  "موجودی داروها و اقلام دارویی", "داروخانه",
  kpis=[("💊", "تعداد اقلام", "۰"), ("⚠", "رو به اتمام", "۰"),
        ("⏰", "نزدیک انقضا", "۰"), ("📦", "ارزش موجودی", "۰")],
  filters=[("search", "جستجوی دارو", "search"), ("category", "دسته", "combo"),
           ("stockStatus", "وضعیت موجودی", "combo"), ("expiry", "انقضا", "combo")],
  cols=[("DrugCode", "کد", "100"), ("DrugName", "نام دارو", "*"),
        ("GenericName", "نام ژنریک", "180"), ("Category", "دسته", "120"),
        ("CurrentStock", "موجودی", "90"), ("ReorderPoint", "نقطه سفارش", "100"),
        ("UnitPrice", "قیمت واحد", "120"), ("ExpiryDate", "تاریخ انقضا", "120")],
  actions=[("NewItem", "+ قلم جدید", "Primary"), ("StockIn", "ورود کالا", "Success"),
           ("Stocktaking", "انبارگردانی", "Secondary"),
           ("Export", "خروجی Excel", "Secondary")],
  notes="سطرهای دارای موجودیِ کمتر از نقطه‌ی سفارش با رنگ هشدار و انقضای نزدیک با رنگ قرمز نمایش داده می‌شوند.")

F("PharmacyItemFormWindow", "فرم قلم دارویی", "💉", "form",
  "ایجاد یا ویرایش قلم دارویی", "داروخانه",
  sections=[
      ("اطلاعات پایه", "💊", [
          "DrugName:text:نام دارو:*", "GenericName:text:نام ژنریک",
          "DrugCode:text:کد دارو:*", "Category:combo:دسته‌بندی:*",
          "DosageForm:combo:شکل دارویی", "Strength:text:دوز/قدرت",
      ]),
      ("موجودی و قیمت", "📦", [
          "CurrentStock:number:موجودی فعلی", "ReorderPoint:number:نقطه سفارش",
          "MaxStock:number:حداکثر موجودی", "Unit:combo:واحد",
          "UnitPrice:money:قیمت واحد", "LastPurchasePrice:money:آخرین قیمت خرید",
      ]),
      ("انقضا و تأمین", "⏰", [
          "ExpiryDate:date:تاریخ انقضا", "SupplierId:combo:تأمین‌کننده",
          "Barcode:barcode:بارکد", "StorageConditions:text:شرایط نگهداری",
          "IsActive:check:فعال",
      ]),
  ],
  actions=[("Save", "ذخیره", "Primary"), ("SaveAndNew", "ذخیره و جدید", "Secondary"),
           ("Cancel", "انصراف", "Ghost")])

F("StockInFormWindow", "ورود کالا", "📥", "form",
  "ثبت ورود دارو به انبار", "داروخانه",
  sections=[
      ("فاکتور", "🧾", [
          "SupplierId:combo:تأمین‌کننده:*", "InvoiceNumber:text:شماره فاکتور:*",
          "InvoiceDate:date:تاریخ فاکتور:*", "TotalAmount:money:مبلغ کل",
      ]),
      ("اقلام", "📦", [
          "DrugId:lookup:دارو:*", "Quantity:number:تعداد:*",
          "UnitPrice:money:قیمت واحد:*", "BatchNumber:text:شماره بچ",
          "ExpiryDate:date:تاریخ انقضا:*",
      ]),
  ],
  actions=[("Save", "ثبت ورود", "Success"), ("AddRow", "افزودن ردیف", "Secondary"),
           ("Cancel", "انصراف", "Ghost")])

F("StocktakingView", "انبارگردانی", "🔢", "list",
  "شمارش و تطبیق موجودی انبار", "داروخانه",
  kpis=[("🔢", "تعداد اقلام", "۰"), ("⚖", "مغایرت‌ها", "۰"), ("✅", "تطبیق داده‌شده", "۰")],
  filters=[("search", "جستجو", "search"), ("category", "دسته", "combo"),
           ("status", "وضعیت", "combo")],
  cols=[("DrugName", "نام دارو", "*"), ("SystemStock", "موجودی سیستم", "120"),
        ("CountedStock", "موجودی شمارش‌شده", "130"), ("Difference", "مغایرت", "100"),
        ("Status", "وضعیت", "110")],
  actions=[("NewCount", "شمارش جدید", "Primary"), ("ApplyDiff", "اعمال مغایرت‌ها", "Success"),
           ("Export", "خروجی Excel", "Secondary")])

# ══════════════════════════════════════════════════════════════════════
#  🧪 آزمایشگاه و تصویربرداری
# ══════════════════════════════════════════════════════════════════════

F("LabView", "آزمایشگاه", "🧪", "list",
  "مدیریت درخواست‌ها و نتایج آزمایش", "آزمایشگاه",
  kpis=[("🧪", "درخواست امروز", "۰"), ("⏳", "در انتظار", "۰"),
        ("⚠", "مقادیر بحرانی", "۰"), ("✅", "نتایج آماده", "۰")],
  filters=[("search", "جستجوی بیمار", "search"), ("status", "وضعیت", "combo"),
           ("date", "تاریخ", "date"), ("testType", "نوع آزمایش", "combo")],
  cols=[("LabNumber", "شماره", "110"), ("PatientName", "بیمار", "*"),
        ("TestName", "آزمایش", "180"), ("RequestDate", "تاریخ درخواست", "130"),
        ("ResultDate", "تاریخ پاسخ", "130"), ("Status", "وضعیت", "110"),
        ("IsCritical", "بحرانی", "80")],
  actions=[("NewOrder", "+ درخواست جدید", "Primary"), ("EnterResult", "ثبت نتیجه", "Success"),
           ("PrintReport", "چاپ نتیجه", "Secondary")],
  notes="مقادیر بحرانی با رنگ قرمز و هشدار فوری به پزشک نمایش داده می‌شوند.")

F("LabOrderFormWindow", "درخواست آزمایش", "🧫", "form",
  "ثبت درخواست آزمایش برای بیمار", "آزمایشگاه",
  sections=[
      ("بیمار", "👤", [
          "PatientId:lookup:بیمار:*", "VisitId:lookup:ویزیت مرتبط",
          "RequestingDoctorId:combo:پزشک درخواست‌کننده:*",
      ]),
      ("آزمایش‌ها", "🧪", [
          "TestIds:lookup:آزمایش‌های درخواستی:*",
          "Priority:combo:اولویت", "FastingRequired:check:نیاز به ناشتایی",
      ]),
      ("نمونه و توضیحات", "📝", [
          "SampleType:combo:نوع نمونه", "CollectionDate:date:تاریخ نمونه‌گیری",
          "ClinicalNotes:textarea:توضیحات بالینی",
      ]),
  ],
  actions=[("Save", "ثبت درخواست", "Primary"), ("SaveAndPrint", "ثبت و چاپ برچسب", "Success"),
           ("Cancel", "انصراف", "Ghost")])

F("LabResultWindow", "ثبت نتیجه آزمایش", "📊", "form",
  "ورود و تأیید نتایج آزمایش", "آزمایشگاه",
  sections=[("نتایج", "📊", [
      "TestName:text:آزمایش", "ResultValue:text:مقدار نتیجه:*",
      "Unit:combo:واحد", "ReferenceRange:text:محدوده مرجع",
      "IsAbnormal:check:غیرطبیعی", "IsCritical:check:مقدار بحرانی",
      "TechnicianNote:textarea:یادداشت تکنسین",
  ])],
  actions=[("Save", "ذخیره", "Primary"), ("Approve", "تأیید نهایی", "Success"),
           ("Cancel", "انصراف", "Ghost")])

F("ImagingView", "تصویربرداری", "🩻", "list",
  "مدیریت درخواست‌ها و تصاویر پزشکی", "تصویربرداری",
  filters=[("search", "جستجوی بیمار", "search"), ("modality", "روش", "combo"),
           ("status", "وضعیت", "combo"), ("date", "تاریخ", "date")],
  cols=[("StudyNumber", "شماره", "110"), ("PatientName", "بیمار", "*"),
        ("Modality", "روش", "120"), ("BodyPart", "عضو", "120"),
        ("StudyDate", "تاریخ", "120"), ("Status", "وضعیت", "110")],
  actions=[("NewOrder", "+ درخواست جدید", "Primary"), ("OpenViewer", "نمایش تصاویر", "Success"),
           ("Upload", "بارگذاری تصویر", "Secondary")])

F("DicomViewerWindow", "نمایشگر تصاویر پزشکی", "🖼", "viewer",
  "مشاهده و بررسی تصاویر DICOM", "تصویربرداری",
  kpis=[("🖼", "تعداد تصاویر", "۰")],
  actions=[("ZoomIn", "بزرگ‌نمایی", "Ghost"), ("ZoomOut", "کوچک‌نمایی", "Ghost"),
           ("Pan", "جابه‌جایی", "Ghost"), ("Measure", "اندازه‌گیری", "Ghost"),
           ("WindowLevel", "روش/کنتراست", "Ghost"), ("Report", "گزارش", "Primary")],
  notes="پنجره‌ی اختصاصی با ابزارهای اندازه‌گیری، تنظیم روشنایی/کنتراست و پیمایش بین تصاویر.")

# ══════════════════════════════════════════════════════════════════════
#  💰 مالی و حسابداری
# ══════════════════════════════════════════════════════════════════════

F("BillingView", "صورتحساب", "🧾", "list",
  "مدیریت صورتحساب‌ها و دریافت‌ها", "مالی",
  kpis=[("🧾", "صورتحساب امروز", "۰"), ("💵", "دریافت امروز", "۰"),
        ("⏳", "پرداخت‌نشده", "۰"), ("↩", "استردادها", "۰")],
  filters=[("search", "جستجوی بیمار", "search"), ("status", "وضعیت", "combo"),
           ("fromDate", "از تاریخ", "date"), ("toDate", "تا تاریخ", "date")],
  cols=[("InvoiceNumber", "شماره", "110"), ("PatientName", "بیمار", "*"),
        ("IssueDate", "تاریخ", "120"), ("TotalAmount", "مبلغ کل", "130"),
        ("PaidAmount", "پرداختی", "130"), ("Balance", "مانده", "130"),
        ("Status", "وضعیت", "110")],
  actions=[("NewInvoice", "+ صورتحساب جدید", "Primary"), ("Pay", "دریافت وجه", "Success"),
           ("Refund", "استرداد", "Danger"), ("Print", "چاپ", "Secondary")])

F("InvoiceFormWindow", "فرم صورتحساب", "📃", "form",
  "ایجاد یا ویرایش صورتحساب", "مالی",
  sections=[
      ("بیمار", "👤", [
          "PatientId:lookup:بیمار:*", "VisitId:lookup:ویزیت",
          "IssueDate:date:تاریخ صدور:*", "DueDate:date:سررسید",
      ]),
      ("اقلام", "📋", [
          "ServiceId:lookup:خدمت:*", "Quantity:number:تعداد:*",
          "UnitPrice:money:قیمت واحد:*", "DiscountPercent:number:درصد تخفیف",
          "TaxAmount:money:مالیات",
      ]),
      ("بیمه و جمع", "🏥", [
          "InsuranceId:combo:بیمه", "InsuranceShare:money:سهم بیمه",
          "PatientShare:money:سهم بیمار", "TotalAmount:money:جمع کل",
      ]),
  ],
  actions=[("Save", "صدور صورتحساب", "Primary"), ("SaveAndPay", "صدور و دریافت", "Success"),
           ("Cancel", "انصراف", "Ghost")])

F("AccountingView", "حسابداری", "📒", "detail",
  "دفتر کل، اسناد حسابداری و تراز", "مالی",
  kpis=[("📒", "تعداد اسناد", "۰"), ("💰", "جمع بدهکار", "۰"),
        ("💸", "جمع بستانکار", "۰"), ("⚖", "اختلاف", "۰")],
  tabs=[("اسناد حسابداری", "📄"), ("دفتر کل", "📒"),
        ("تراز آزمایشی", "⚖"), ("سود و زیان", "📊")],
  filters=[("fromDate", "از تاریخ", "date"), ("toDate", "تا تاریخ", "date"),
           ("account", "حساب", "combo")],
  cols=[("VoucherNumber", "شماره سند", "110"), ("VoucherDate", "تاریخ", "120"),
        ("Description", "شرح", "*"), ("Debit", "بدهکار", "130"),
        ("Credit", "بستانکار", "130")],
  actions=[("NewVoucher", "سند جدید", "Primary"), ("TrialBalance", "تراز آزمایشی", "Secondary"),
           ("CloseYear", "بستن سال مالی", "Danger")])

F("JournalEntryDetailWindow", "جزئیات سند حسابداری", "📄", "dialog",
  "مشاهده و ویرایش ردیف‌های سند", "مالی",
  cols=[("AccountCode", "کد حساب", "100"), ("AccountName", "نام حساب", "*"),
        ("Debit", "بدهکار", "130"), ("Credit", "بستانکار", "130"),
        ("Description", "شرح", "*")],
  actions=[("Save", "ذخیره", "Primary"), ("Approve", "تأیید سند", "Success")])

F("TariffFormWindow", "تعرفه خدمات", "🏷", "form",
  "تعریف و ویرایش تعرفه‌ی خدمات", "مالی",
  sections=[("تعرفه", "🏷", [
      "ServiceId:lookup:خدمت:*", "InsuranceId:combo:بیمه",
      "BasePrice:money:قیمت پایه:*", "InsurancePrice:money:قیمت بیمه",
      "CashPrice:money:قیمت نقدی", "EffectiveDate:date:تاریخ اجرا:*",
      "IsActive:check:فعال",
  ])],
  actions=[("Save", "ذخیره", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("CompanyFormWindow", "فرم شرکت بیمه", "🏢", "form",
  "ایجاد یا ویرایش شرکت بیمه", "مالی",
  sections=[("شرکت بیمه", "🏢", [
      "CompanyName:text:نام شرکت:*", "CompanyCode:text:کد شرکت",
      "ContractNumber:text:شماره قرارداد", "PhoneNumber:phone:تلفن",
      "Address:textarea:نشانی", "IsActive:check:فعال",
  ])],
  actions=[("Save", "ذخیره", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("InsuranceView", "بیمه و مطالبات", "🏥", "detail",
  "مدیریت بیمه‌ها، اسناد و پیگیری مطالبات", "مالی",
  kpis=[("🏥", "مطالبات جاری", "۰"), ("⏰", "سررسید گذشته", "۰"),
        ("📊", "نرخ وصول", "۰")],
  tabs=[("شرکت‌های بیمه", "🏢"), ("اسناد ارسالی", "📤"),
        ("پیری مطالبات", "📊"), ("پیگیری", "🔔")],
  filters=[("company", "شرکت بیمه", "combo"), ("status", "وضعیت", "combo"),
           ("fromDate", "از تاریخ", "date")],
  cols=[("ClaimNumber", "شماره", "110"), ("CompanyName", "شرکت بیمه", "160"),
        ("PatientName", "بیمار", "*"), ("Amount", "مبلغ", "130"),
        ("SubmittedAt", "تاریخ ارسال", "120"), ("Status", "وضعیت", "110")],
  actions=[("NewClaim", "سند جدید", "Primary"), ("Submit", "ارسال به بیمه", "Success"),
           ("FollowUp", "پیگیری", "Secondary")])

F("PayrollView", "حقوق و دستمزد", "💵", "list",
  "محاسبه و مدیریت حقوق پرسنل", "مالی",
  kpis=[("👥", "تعداد پرسنل", "۰"), ("💵", "جمع حقوق", "۰"),
        ("✅", "تأیید شده", "۰")],
  filters=[("period", "دوره", "combo"), ("department", "بخش", "combo"),
           ("status", "وضعیت", "combo")],
  cols=[("EmployeeCode", "کد", "90"), ("EmployeeName", "نام کارمند", "*"),
        ("BaseSalary", "حقوق پایه", "130"), ("Overtime", "اضافه‌کاری", "120"),
        ("Deductions", "کسورات", "120"), ("NetSalary", "خالص پرداختی", "140"),
        ("Status", "وضعیت", "110")],
  actions=[("Calculate", "محاسبه حقوق", "Primary"), ("Approve", "تأیید نهایی", "Success"),
           ("PrintPayslips", "چاپ فیش‌ها", "Secondary")])

F("PayslipWindow", "فیش حقوقی", "🧾", "dialog",
  "مشاهده و چاپ فیش حقوقی", "مالی",
  actions=[("Print", "چاپ فیش", "Primary"), ("ExportPdf", "خروجی PDF", "Secondary"),
           ("Close", "بستن", "Ghost")])

F("CommissionReportWindow", "گزارش کارمزد", "📈", "dialog",
  "گزارش کارمزد پزشکان", "مالی",
  filters=[("doctor", "پزشک", "combo"), ("fromDate", "از تاریخ", "date"),
           ("toDate", "تا تاریخ", "date")],
  cols=[("DoctorName", "پزشک", "*"), ("VisitCount", "تعداد ویزیت", "120"),
        ("GrossAmount", "ناخالص", "130"), ("CommissionRate", "نرخ", "90"),
        ("CommissionAmount", "کارمزد", "130")],
  actions=[("Generate", "تهیه گزارش", "Primary"), ("Export", "خروجی Excel", "Secondary")])

F("DoctorPerformanceWindow", "عملکرد پزشکان", "📊", "dialog",
  "شاخص‌های عملکردی پزشکان", "مالی",
  filters=[("doctor", "پزشک", "combo"), ("fromDate", "از تاریخ", "date"),
           ("toDate", "تا تاریخ", "date")],
  cols=[("DoctorName", "پزشک", "*"), ("PatientCount", "تعداد بیمار", "120"),
        ("AvgWaitTime", "میانگین انتظار", "130"), ("Revenue", "درآمد", "140"),
        ("Satisfaction", "رضایت", "100")],
  actions=[("Generate", "تهیه گزارش", "Primary"), ("Export", "خروجی", "Secondary")])

# ══════════════════════════════════════════════════════════════════════
#  👔 منابع انسانی
# ══════════════════════════════════════════════════════════════════════

F("PersonnelView", "پرسنل", "👔", "list",
  "مدیریت کارکنان و قراردادها", "منابع انسانی",
  kpis=[("👔", "تعداد پرسنل", "۰"), ("✅", "فعال", "۰"),
        ("⏰", "قراردادهای رو به اتمام", "۰")],
  filters=[("search", "جستجو", "search"), ("department", "بخش", "combo"),
           ("position", "سمت", "combo"), ("status", "وضعیت", "combo")],
  cols=[("EmployeeCode", "کد", "90"), ("FullName", "نام", "*"),
        ("Position", "سمت", "150"), ("Department", "بخش", "130"),
        ("HireDate", "تاریخ استخدام", "130"), ("Status", "وضعیت", "110")],
  actions=[("New", "+ کارمند جدید", "Primary"), ("Edit", "ویرایش", "Secondary"),
           ("Contract", "قرارداد", "Secondary")])

F("EmployeeFormWindow", "فرم کارمند", "👤", "form",
  "ایجاد یا ویرایش اطلاعات کارمند", "منابع انسانی",
  sections=[
      ("اطلاعات فردی", "🪪", [
          "FirstName:text:نام:*", "LastName:text:نام خانوادگی:*",
          "NationalCode:national:کد ملی:*", "BirthDate:date:تاریخ تولد",
          "Gender:combo:جنسیت", "MaritalStatus:combo:وضعیت تأهل",
      ]),
      ("اطلاعات شغلی", "💼", [
          "EmployeeCode:text:کد پرسنلی:*", "PositionId:combo:سمت:*",
          "DepartmentId:combo:بخش:*", "HireDate:date:تاریخ استخدام:*",
          "EmploymentType:combo:نوع استخدام", "ManagerId:combo:مدیر مستقیم",
      ]),
      ("اطلاعات تماس", "📞", [
          "MobileNumber:phone:موبایل", "Email:text:ایمیل", "Address:textarea:نشانی",
      ]),
      ("حقوق و مزایا", "💵", [
          "BaseSalary:money:حقوق پایه", "Allowances:money:مزایا",
          "InsuranceNumber:text:شماره بیمه", "BankAccount:text:شماره حساب",
      ]),
  ],
  actions=[("Save", "ذخیره", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("EmployeeSalaryEditWindow", "ویرایش حقوق", "💵", "form",
  "تنظیم حقوق و مزایای کارمند", "منابع انسانی",
  sections=[("حقوق", "💵", [
      "BaseSalary:money:حقوق پایه:*", "HousingAllowance:money:حق مسکن",
      "FamilyAllowance:money:حق عائله‌مندی", "TransportAllowance:money:حق ایاب‌وذهاب",
      "OtherAllowance:money:سایر مزایا", "EffectiveDate:date:تاریخ اجرا:*",
  ])],
  actions=[("Save", "ذخیره", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("AttendanceView", "حضور و غیاب", "🕐", "list",
  "تردد پرسنل و همگام‌سازی با دستگاه", "منابع انسانی",
  kpis=[("🕐", "حاضر", "۰"), ("⏰", "تأخیر", "۰"),
        ("🚫", "غایب", "۰"), ("📊", "ناهنجاری", "۰")],
  filters=[("employee", "کارمند", "combo"), ("department", "بخش", "combo"),
           ("fromDate", "از تاریخ", "date"), ("toDate", "تا تاریخ", "date")],
  cols=[("EmployeeName", "کارمند", "*"), ("Date", "تاریخ", "120"),
        ("CheckIn", "ورود", "100"), ("CheckOut", "خروج", "100"),
        ("WorkHours", "ساعات کار", "100"), ("Overtime", "اضافه‌کاری", "100"),
        ("Status", "وضعیت", "110")],
  actions=[("Sync", "همگام‌سازی دستگاه", "Primary"), ("ManualEntry", "ثبت دستی", "Secondary"),
           ("Report", "گزارش ماهانه", "Secondary")])

F("DoctorsView", "پزشکان", "👨‍⚕️", "list",
  "مدیریت پزشکان، تخصص‌ها و برنامه‌ی کاری", "منابع انسانی",
  filters=[("search", "جستجو", "search"), ("specialty", "تخصص", "combo"),
           ("status", "وضعیت", "combo")],
  cols=[("DoctorCode", "کد", "90"), ("FullName", "نام پزشک", "*"),
        ("Specialty", "تخصص", "170"), ("MedicalLicense", "نظام پزشکی", "130"),
        ("PhoneNumber", "تلفن", "130"), ("Status", "وضعیت", "110")],
  actions=[("New", "+ پزشک جدید", "Primary"), ("Schedule", "برنامه کاری", "Secondary"),
           ("Commission", "کارمزد", "Secondary")])

F("DoctorFormWindow", "فرم پزشک", "🩺", "form",
  "ایجاد یا ویرایش اطلاعات پزشک", "منابع انسانی",
  sections=[
      ("اطلاعات فردی", "🪪", [
          "FirstName:text:نام:*", "LastName:text:نام خانوادگی:*",
          "NationalCode:national:کد ملی:*", "Gender:combo:جنسیت",
          "BirthDate:date:تاریخ تولد",
      ]),
      ("اطلاعات حرفه‌ای", "🩺", [
          "SpecialtyId:combo:تخصص:*", "MedicalLicenseNumber:text:شماره نظام پزشکی:*",
          "Degree:combo:مدرک", "University:text:دانشگاه محل تحصیل",
          "ContractType:combo:نوع همکاری", "CommissionPercent:number:درصد کارمزد",
      ]),
      ("اطلاعات تماس", "📞", [
          "MobileNumber:phone:موبایل", "PhoneNumber:phone:تلفن مطب",
          "Email:text:ایمیل", "Address:textarea:نشانی مطب",
      ]),
      ("برنامه‌ی کاری", "📅", [
          "VisitDurationMinutes:number:مدت هر ویزیت", "MaxPatientsPerDay:number:حداکثر بیمار",
          "WorkingDays:combo:روزهای کاری", "IsActive:check:فعال",
      ]),
  ],
  actions=[("Save", "ذخیره", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("UsersView", "کاربران", "👤", "list",
  "مدیریت حساب‌های کاربری", "منابع انسانی",
  filters=[("search", "جستجو", "search"), ("role", "نقش", "combo"),
           ("status", "وضعیت", "combo")],
  cols=[("UserName", "نام کاربری", "160"), ("FullName", "نام کامل", "*"),
        ("RoleName", "نقش", "150"), ("Email", "ایمیل", "200"),
        ("LastLoginAt", "آخرین ورود", "140"), ("IsActive", "فعال", "80")],
  actions=[("New", "+ کاربر جدید", "Primary"), ("ResetPassword", "بازنشانی رمز", "Secondary"),
           ("Deactivate", "غیرفعال‌سازی", "Danger")])

F("UserFormWindow", "فرم کاربر", "👤", "form",
  "ایجاد یا ویرایش حساب کاربری", "منابع انسانی",
  sections=[
      ("حساب کاربری", "👤", [
          "UserName:text:نام کاربری:*", "Password:password:رمز عبور:*",
          "ConfirmPassword:password:تکرار رمز:*", "Email:text:ایمیل",
          "IsActive:check:فعال", "MustChangePassword:check:تغییر رمز در اولین ورود",
      ]),
      ("نقش‌ها و دسترسی", "🔐", [
          "RoleId:combo:نقش اصلی:*", "BranchId:combo:شعبه",
          "EmployeeId:combo:پرسنل مرتبط",
      ]),
  ],
  actions=[("Save", "ذخیره", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("RolePermissionView", "نقش‌ها و مجوزها", "🔐", "detail",
  "تعریف نقش‌ها و تخصیص دسترسی‌ها", "منابع انسانی",
  kpis=[("🔐", "تعداد نقش‌ها", "۰"), ("🔑", "تعداد مجوزها", "۰")],
  filters=[("search", "جستجو", "search"), ("module", "ماژول", "combo")],
  cols=[("PermissionName", "مجوز", "*"), ("Module", "ماژول", "150"),
        ("Description", "توضیح", "*")],
  actions=[("NewRole", "نقش جدید", "Primary"), ("Save", "ذخیره دسترسی‌ها", "Success"),
           ("CopyRole", "کپی از نقش", "Secondary")])

F("AdminView", "مدیریت سیستم", "🛠", "detail",
  "تنظیمات پیشرفته‌ی مدیریتی", "منابع انسانی",
  tabs=[("کاربران", "👤"), ("نقش‌ها", "🔐"), ("شعب", "🏘"),
        ("لاگ سیستم", "📜"), ("لایسنس", "🔑")],
  actions=[("Save", "ذخیره", "Primary")])

# ══════════════════════════════════════════════════════════════════════
#  🏘 سازمانی و ارتباطات
# ══════════════════════════════════════════════════════════════════════

F("BranchesView", "شعب", "🏘", "list",
  "مدیریت شعب و مراکز درمانی", "سازمانی",
  filters=[("search", "جستجو", "search"), ("status", "وضعیت", "combo")],
  cols=[("BranchCode", "کد", "90"), ("BranchName", "نام شعبه", "*"),
        ("City", "شهر", "130"), ("PhoneNumber", "تلفن", "130"),
        ("ManagerName", "مدیر", "160"), ("IsActive", "فعال", "80")],
  actions=[("New", "+ شعبه جدید", "Primary")])

F("SmsView", "پیامک", "📨", "detail",
  "ارسال پیامک و مدیریت قالب‌ها", "ارتباطات",
  kpis=[("📨", "ارسال امروز", "۰"), ("✅", "تحویل شده", "۰"),
        ("❌", "ناموفق", "۰"), ("💰", "هزینه", "۰")],
  tabs=[("ارسال جدید", "📤"), ("قالب‌ها", "📋"), ("گزارش ارسال", "📊")],
  sections=[("ارسال", "📤", [
      "Recipients:lookup:گیرندگان:*", "TemplateId:combo:قالب",
      "MessageText:textarea:متن پیام:*", "SendAt:date:زمان ارسال",
      "RespectOptOut:check:احترام به انصراف",
  ])],
  actions=[("Send", "ارسال", "Primary"), ("Preview", "پیش‌نمایش", "Secondary"),
           ("SaveTemplate", "ذخیره قالب", "Secondary")])

F("SurveyView", "نظرسنجی", "📝", "detail",
  "طراحی پرسش‌نامه و تحلیل پاسخ‌ها", "ارتباطات",
  kpis=[("📝", "تعداد پاسخ", "۰"), ("⭐", "میانگین امتیاز", "۰"), ("📊", "NPS", "۰")],
  tabs=[("پرسش‌نامه‌ها", "📋"), ("پاسخ‌ها", "💬"), ("تحلیل", "📊")],
  actions=[("NewSurvey", "پرسش‌نامه جدید", "Primary"), ("Send", "ارسال", "Success")])

F("SepasView", "سامانه سپاس", "🏛", "detail",
  "یکپارچه‌سازی با پرونده الکترونیک سلامت", "سازمانی",
  kpis=[("📤", "ارسال‌شده", "۰"), ("✅", "تأیید شده", "۰"), ("❌", "خطا", "۰")],
  tabs=[("ارسال‌ها", "📤"), ("وضعیت اتصال", "🔌"), ("خطاها", "⚠")],
  filters=[("fromDate", "از تاریخ", "date"), ("toDate", "تا تاریخ", "date"),
           ("status", "وضعیت", "combo")],
  cols=[("TransactionId", "شناسه", "150"), ("PatientName", "بیمار", "*"),
        ("SentAt", "زمان ارسال", "140"), ("Status", "وضعیت", "110")],
  actions=[("Send", "ارسال", "Primary"), ("Refresh", "به‌روزرسانی وضعیت", "Secondary")])

# ══════════════════════════════════════════════════════════════════════
#  📊 گزارشات
# ══════════════════════════════════════════════════════════════════════

F("ReportsView", "گزارشات", "📊", "detail",
  "گزارش‌های آماده و استاندارد", "گزارشات",
  tabs=[("مالی", "💰"), ("بالینی", "🩺"), ("عملیاتی", "⚙"),
        ("دارویی", "💊"), ("پرسنلی", "👔")],
  filters=[("fromDate", "از تاریخ", "date"), ("toDate", "تا تاریخ", "date"),
           ("branch", "شعبه", "combo")],
  actions=[("Generate", "تهیه گزارش", "Primary"), ("ExportExcel", "خروجی Excel", "Secondary"),
           ("ExportPdf", "خروجی PDF", "Secondary"), ("Print", "چاپ", "Secondary")])

F("ReportBrowserView", "مرورگر گزارشات", "📑", "detail",
  "مرور و اجرای گزارشات تعریف‌شده", "گزارشات",
  filters=[("search", "جستجو", "search"), ("category", "دسته", "combo")],
  cols=[("ReportName", "نام گزارش", "*"), ("Category", "دسته", "140"),
        ("Description", "توضیح", "*"), ("LastRun", "آخرین اجرا", "140")],
  actions=[("Run", "اجرا", "Primary"), ("Schedule", "زمان‌بندی", "Secondary"),
           ("Export", "خروجی", "Secondary")])

F("AdhocReportWindow", "گزارش‌ساز پویا", "🧩", "dialog",
  "ساخت گزارش دلخواه با انتخاب فیلدها", "گزارشات",
  filters=[("entity", "موجودیت", "combo")],
  cols=[("FieldName", "فیلد", "*"), ("DisplayName", "عنوان", "180"),
        ("Include", "نمایش", "80"), ("Aggregate", "تجمیع", "120")],
  actions=[("Run", "اجرای گزارش", "Primary"), ("SaveTemplate", "ذخیره الگو", "Secondary"),
           ("Export", "خروجی Excel", "Secondary")])

F("AuditLogView", "گزارش حسابرسی", "📜", "list",
  "ردیابی تغییرات و دسترسی کاربران", "گزارشات",
  filters=[("user", "کاربر", "combo"), ("entity", "موجودیت", "combo"),
           ("action", "عملیات", "combo"), ("fromDate", "از تاریخ", "date")],
  cols=[("Timestamp", "زمان", "150"), ("UserName", "کاربر", "150"),
        ("EntityName", "موجودیت", "150"), ("Action", "عملیات", "120"),
        ("Description", "توضیح", "*")],
  actions=[("Export", "خروجی Excel", "Secondary"), ("VerifyChain", "بررسی زنجیره", "Secondary")])

# ══════════════════════════════════════════════════════════════════════
#  🔧 سیستم و پشتیبانی
# ══════════════════════════════════════════════════════════════════════

F("BackupView", "پشتیبان‌گیری", "💾", "detail",
  "تهیه، بازیابی و زمان‌بندی نسخه‌های پشتیبان", "سیستم",
  kpis=[("💾", "تعداد نسخه‌ها", "۰"), ("📦", "حجم کل", "۰"),
        ("🕐", "آخرین نسخه", "—"), ("☁", "وضعیت ابری", "—")],
  tabs=[("نسخه‌های پشتیبان", "💾"), ("زمان‌بندی", "⏰"), ("بازیابی", "♻"), ("تنظیمات ابری", "☁")],
  filters=[("type", "نوع", "combo"), ("fromDate", "از تاریخ", "date")],
  cols=[("FileName", "نام فایل", "*"), ("CreatedAt", "تاریخ", "150"),
        ("SizeBytes", "حجم", "110"), ("Type", "نوع", "110"), ("Status", "وضعیت", "110")],
  actions=[("BackupNow", "پشتیبان‌گیری فوری", "Primary"), ("Restore", "بازیابی", "Danger"),
           ("UploadCloud", "ارسال به ابر", "Secondary"), ("Verify", "بررسی سلامت", "Secondary")])

F("LicenseStatusView", "وضعیت لایسنس", "🔑", "detail",
  "نمایش وضعیت مجوز و ماژول‌های فعال", "سیستم",
  kpis=[("🔑", "نوع لایسنس", "—"), ("📅", "تاریخ انقضا", "—"),
        ("⏳", "روزهای باقی‌مانده", "۰"), ("🧩", "ماژول‌های فعال", "۰")],
  tabs=[("خلاصه", "📋"), ("ماژول‌ها", "🧩"), ("تاریخچه", "🕐")],
  actions=[("Activate", "فعال‌سازی", "Primary"), ("Refresh", "به‌روزرسانی", "Secondary")])

F("LicenseActivationWindow", "فعال‌سازی لایسنس", "🔐", "form",
  "وارد کردن کلید لایسنس", "سیستم",
  sections=[("فعال‌سازی", "🔐", [
      "LicenseKey:textarea:کلید لایسنس:*",
      "CustomerName:text:نام مشتری",
      "Email:text:ایمیل",
  ])],
  actions=[("Activate", "فعال‌سازی", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("ColorPickerWindow", "انتخاب رنگ", "🎨", "dialog",
  "انتخاب رنگ برای تنظیمات شخصی", "سیستم",
  actions=[("Select", "انتخاب", "Primary"), ("Cancel", "انصراف", "Ghost")])

F("ThemedDialog", "دیالوگ یکپارچه", "💬", "dialog",
  "دیالوگ عمومی با تم یکپارچه", "سیستم",
  actions=[("Ok", "تأیید", "Primary"), ("Cancel", "انصراف", "Secondary")],
  notes="جایگزین MessageBox بومی برای حفظ یکپارچگیِ بصری و پشتیبانی از تم و زبان.")
