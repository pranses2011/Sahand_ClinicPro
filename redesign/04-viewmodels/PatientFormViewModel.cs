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
    /// ViewModel صفحه‌ی «فرم بیمار» — ایجاد یا ویرایش پرونده‌ی بیمار
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/PatientFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/PatientFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/PatientFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class PatientFormViewModel : FormViewModelBase
    {
        public PatientFormViewModel(ILogger? logger = null) : base(logger)
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

        /// <summary>جنسیت (الزامی)</summary>
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

        private string _maritalStatus = string.Empty;

        /// <summary>وضعیت تأهل</summary>
        public string MaritalStatus
        {
            get => _maritalStatus;
            set
            {
                if (SetProperty(ref _maritalStatus, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _mobileNumber = string.Empty;

        /// <summary>موبایل (الزامی)</summary>
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

        /// <summary>تلفن ثابت</summary>
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

        private string _city = string.Empty;

        /// <summary>شهر</summary>
        public string City
        {
            get => _city;
            set
            {
                if (SetProperty(ref _city, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _address = string.Empty;

        /// <summary>نشانی</summary>
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

        private string _bloodType = string.Empty;

        /// <summary>گروه خون</summary>
        public string BloodType
        {
            get => _bloodType;
            set
            {
                if (SetProperty(ref _bloodType, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _allergies = string.Empty;

        /// <summary>حساسیت‌ها</summary>
        public string Allergies
        {
            get => _allergies;
            set
            {
                if (SetProperty(ref _allergies, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _chronicConditions = string.Empty;

        /// <summary>بیماری‌های زمینه‌ای</summary>
        public string ChronicConditions
        {
            get => _chronicConditions;
            set
            {
                if (SetProperty(ref _chronicConditions, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _insuranceId = null;

        /// <summary>بیمه</summary>
        public Guid? InsuranceId
        {
            get => _insuranceId;
            set
            {
                if (SetProperty(ref _insuranceId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _insuranceNumber = string.Empty;

        /// <summary>شماره بیمه</summary>
        public string InsuranceNumber
        {
            get => _insuranceNumber;
            set
            {
                if (SetProperty(ref _insuranceNumber, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _referredBy = string.Empty;

        /// <summary>معرف</summary>
        public string ReferredBy
        {
            get => _referredBy;
            set
            {
                if (SetProperty(ref _referredBy, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _emergencyContactName = string.Empty;

        /// <summary>نام فرد</summary>
        public string EmergencyContactName
        {
            get => _emergencyContactName;
            set
            {
                if (SetProperty(ref _emergencyContactName, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _emergencyContactPhone = string.Empty;

        /// <summary>شماره تماس</summary>
        public string EmergencyContactPhone
        {
            get => _emergencyContactPhone;
            set
            {
                if (SetProperty(ref _emergencyContactPhone, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _emergencyRelation = string.Empty;

        /// <summary>نسبت</summary>
        public string EmergencyRelation
        {
            get => _emergencyRelation;
            set
            {
                if (SetProperty(ref _emergencyRelation, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _saveAndNewCommand;

        /// <summary>ذخیره و جدید</summary>
        public IAsyncRelayCommand SaveAndNewCommand =>
            _saveAndNewCommand ??= new AsyncRelayCommand(SaveAndNewAsync);

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
            if (string.IsNullOrWhiteSpace(Gender))
                AddError("جنسیت الزامی است", nameof(Gender));
            if (string.IsNullOrWhiteSpace(MobileNumber))
                AddError("موبایل الزامی است", nameof(MobileNumber));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<PatientFormDto>("api/v1/<built-in method lower of str object at 0x7f5384a94bb0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// ذخیره و جدید
        /// </summary>
        private async Task saveAndNewAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "ذخیره و جدید");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}
