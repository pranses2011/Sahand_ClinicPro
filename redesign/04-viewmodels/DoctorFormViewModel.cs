using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Desktop.Base;
using Sahand.ClinicPro.Desktop.Localization;
using Sahand.ClinicPro.Desktop.Themes;
namespace Sahand.ClinicPro.Desktop.ViewModels.Redesign
{
    /// <summary>
    /// ViewModel صفحه‌ی «فرم پزشک» — ایجاد یا ویرایش اطلاعات پزشک
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/DoctorFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/DoctorFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/DoctorFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class DoctorFormViewModel : FormViewModelBase
    {
        public DoctorFormViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private string _firstName = string.Empty;

        /// <summary>نام (الزامی)</summary>
        public string FirstName
        {
            get => _firstName;
            set
            {
                if (SetProperty(ref _firstName, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _lastName = string.Empty;

        /// <summary>نام خانوادگی (الزامی)</summary>
        public string LastName
        {
            get => _lastName;
            set
            {
                if (SetProperty(ref _lastName, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _nationalCode = string.Empty;

        /// <summary>کد ملی (الزامی)</summary>
        public string NationalCode
        {
            get => _nationalCode;
            set
            {
                if (SetProperty(ref _nationalCode, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _gender = string.Empty;

        /// <summary>جنسیت</summary>
        public string Gender
        {
            get => _gender;
            set
            {
                if (SetProperty(ref _gender, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private DateTime? _birthDate = null;

        /// <summary>تاریخ تولد</summary>
        public DateTime? BirthDate
        {
            get => _birthDate;
            set
            {
                if (SetProperty(ref _birthDate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _specialtyId = null;

        /// <summary>تخصص (الزامی)</summary>
        public Guid? SpecialtyId
        {
            get => _specialtyId;
            set
            {
                if (SetProperty(ref _specialtyId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _medicalLicenseNumber = string.Empty;

        /// <summary>شماره نظام پزشکی (الزامی)</summary>
        public string MedicalLicenseNumber
        {
            get => _medicalLicenseNumber;
            set
            {
                if (SetProperty(ref _medicalLicenseNumber, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _degree = string.Empty;

        /// <summary>مدرک</summary>
        public string Degree
        {
            get => _degree;
            set
            {
                if (SetProperty(ref _degree, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _university = string.Empty;

        /// <summary>دانشگاه محل تحصیل</summary>
        public string University
        {
            get => _university;
            set
            {
                if (SetProperty(ref _university, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _contractType = string.Empty;

        /// <summary>نوع همکاری</summary>
        public string ContractType
        {
            get => _contractType;
            set
            {
                if (SetProperty(ref _contractType, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _commissionPercent = 0m;

        /// <summary>درصد کارمزد</summary>
        public decimal CommissionPercent
        {
            get => _commissionPercent;
            set
            {
                if (SetProperty(ref _commissionPercent, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _mobileNumber = string.Empty;

        /// <summary>موبایل</summary>
        public string MobileNumber
        {
            get => _mobileNumber;
            set
            {
                if (SetProperty(ref _mobileNumber, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _phoneNumber = string.Empty;

        /// <summary>تلفن مطب</summary>
        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (SetProperty(ref _phoneNumber, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _email = string.Empty;

        /// <summary>ایمیل</summary>
        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _address = string.Empty;

        /// <summary>نشانی مطب</summary>
        public string Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _visitDurationMinutes = 0m;

        /// <summary>مدت هر ویزیت</summary>
        public decimal VisitDurationMinutes
        {
            get => _visitDurationMinutes;
            set
            {
                if (SetProperty(ref _visitDurationMinutes, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _maxPatientsPerDay = 0m;

        /// <summary>حداکثر بیمار</summary>
        public decimal MaxPatientsPerDay
        {
            get => _maxPatientsPerDay;
            set
            {
                if (SetProperty(ref _maxPatientsPerDay, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _workingDays = string.Empty;

        /// <summary>روزهای کاری</summary>
        public string WorkingDays
        {
            get => _workingDays;
            set
            {
                if (SetProperty(ref _workingDays, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private bool _isActive = false;

        /// <summary>فعال</summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════


        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (string.IsNullOrWhiteSpace(FirstName))
                AddError("نام الزامی است", nameof(FirstName));
            if (string.IsNullOrWhiteSpace(LastName))
                AddError("نام خانوادگی الزامی است", nameof(LastName));
            if (string.IsNullOrWhiteSpace(NationalCode))
                AddError("کد ملی الزامی است", nameof(NationalCode));
            if (SpecialtyId is null)
                AddError("تخصص الزامی است", nameof(SpecialtyId));
            if (string.IsNullOrWhiteSpace(MedicalLicenseNumber))
                AddError("شماره نظام پزشکی الزامی است", nameof(MedicalLicenseNumber));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<DoctorFormDto>("api/v1/<built-in method lower of str object at 0x7f5384a94ef0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════


    }
}
