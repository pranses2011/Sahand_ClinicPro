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
    /// ViewModel صفحه‌ی «فرم صورتحساب» — ایجاد یا ویرایش صورتحساب
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/InvoiceFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/InvoiceFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/InvoiceFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class InvoiceFormViewModel : FormViewModelBase
    {
        public InvoiceFormViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private Guid? _patientId = null;

        /// <summary>بیمار (الزامی)</summary>
        public Guid? PatientId
        {
            get => _patientId;
            set
            {
                if (SetProperty(ref _patientId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _visitId = null;

        /// <summary>ویزیت</summary>
        public Guid? VisitId
        {
            get => _visitId;
            set
            {
                if (SetProperty(ref _visitId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private DateTime? _issueDate = null;

        /// <summary>تاریخ صدور (الزامی)</summary>
        public DateTime? IssueDate
        {
            get => _issueDate;
            set
            {
                if (SetProperty(ref _issueDate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private DateTime? _dueDate = null;

        /// <summary>سررسید</summary>
        public DateTime? DueDate
        {
            get => _dueDate;
            set
            {
                if (SetProperty(ref _dueDate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _serviceId = null;

        /// <summary>خدمت (الزامی)</summary>
        public Guid? ServiceId
        {
            get => _serviceId;
            set
            {
                if (SetProperty(ref _serviceId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _quantity = 0m;

        /// <summary>تعداد (الزامی)</summary>
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _unitPrice = 0m;

        /// <summary>قیمت واحد (الزامی)</summary>
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _discountPercent = 0m;

        /// <summary>درصد تخفیف</summary>
        public decimal DiscountPercent
        {
            get => _discountPercent;
            set
            {
                if (SetProperty(ref _discountPercent, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _taxAmount = 0m;

        /// <summary>مالیات</summary>
        public decimal TaxAmount
        {
            get => _taxAmount;
            set
            {
                if (SetProperty(ref _taxAmount, value))
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

        private decimal _insuranceShare = 0m;

        /// <summary>سهم بیمه</summary>
        public decimal InsuranceShare
        {
            get => _insuranceShare;
            set
            {
                if (SetProperty(ref _insuranceShare, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _patientShare = 0m;

        /// <summary>سهم بیمار</summary>
        public decimal PatientShare
        {
            get => _patientShare;
            set
            {
                if (SetProperty(ref _patientShare, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _totalAmount = 0m;

        /// <summary>جمع کل</summary>
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                if (SetProperty(ref _totalAmount, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _saveAndPayCommand;

        /// <summary>صدور و دریافت</summary>
        public IAsyncRelayCommand SaveAndPayCommand =>
            _saveAndPayCommand ??= new AsyncRelayCommand(SaveAndPayAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (PatientId is null)
                AddError("بیمار الزامی است", nameof(PatientId));
            if (IssueDate is null)
                AddError("تاریخ صدور الزامی است", nameof(IssueDate));
            if (ServiceId is null)
                AddError("خدمت الزامی است", nameof(ServiceId));
            if (Quantity <= 0)
                AddError("تعداد باید بزرگ‌تر از صفر باشد", nameof(Quantity));
            if (UnitPrice <= 0)
                AddError("قیمت واحد باید بزرگ‌تر از صفر باشد", nameof(UnitPrice));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<InvoiceFormDto>("api/v1/<built-in method lower of str object at 0x7f5384a950f0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// صدور و دریافت
        /// </summary>
        private async Task saveAndPayAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "صدور و دریافت");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}
