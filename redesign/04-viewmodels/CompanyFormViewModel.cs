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
    /// ViewModel صفحه‌ی «فرم شرکت بیمه» — ایجاد یا ویرایش شرکت بیمه
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/CompanyFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/CompanyFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/CompanyFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class CompanyFormViewModel : FormViewModelBase
    {
        public CompanyFormViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private string _companyName = string.Empty;

        /// <summary>نام شرکت (الزامی)</summary>
        public string CompanyName
        {
            get => _companyName;
            set
            {
                if (SetProperty(ref _companyName, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _companyCode = string.Empty;

        /// <summary>کد شرکت</summary>
        public string CompanyCode
        {
            get => _companyCode;
            set
            {
                if (SetProperty(ref _companyCode, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _contractNumber = string.Empty;

        /// <summary>شماره قرارداد</summary>
        public string ContractNumber
        {
            get => _contractNumber;
            set
            {
                if (SetProperty(ref _contractNumber, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _phoneNumber = string.Empty;

        /// <summary>تلفن</summary>
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
            if (string.IsNullOrWhiteSpace(CompanyName))
                AddError("نام شرکت الزامی است", nameof(CompanyName));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<CompanyFormDto>("api/v1/<built-in method lower of str object at 0x7f5384a951b0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════


    }
}
