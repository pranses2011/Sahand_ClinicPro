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
    /// ViewModel صفحه‌ی «فرم قلم دارویی» — ایجاد یا ویرایش قلم دارویی
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/PharmacyItemFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/PharmacyItemFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/PharmacyItemFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class PharmacyItemFormViewModel : FormViewModelBase
    {
        public PharmacyItemFormViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private string _drugName = string.Empty;

        /// <summary>نام دارو (الزامی)</summary>
        public string DrugName
        {
            get => _drugName;
            set
            {
                if (SetProperty(ref _drugName, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _genericName = string.Empty;

        /// <summary>نام ژنریک</summary>
        public string GenericName
        {
            get => _genericName;
            set
            {
                if (SetProperty(ref _genericName, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _drugCode = string.Empty;

        /// <summary>کد دارو (الزامی)</summary>
        public string DrugCode
        {
            get => _drugCode;
            set
            {
                if (SetProperty(ref _drugCode, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _category = string.Empty;

        /// <summary>دسته‌بندی (الزامی)</summary>
        public string Category
        {
            get => _category;
            set
            {
                if (SetProperty(ref _category, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _dosageForm = string.Empty;

        /// <summary>شکل دارویی</summary>
        public string DosageForm
        {
            get => _dosageForm;
            set
            {
                if (SetProperty(ref _dosageForm, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _strength = string.Empty;

        /// <summary>دوز/قدرت</summary>
        public string Strength
        {
            get => _strength;
            set
            {
                if (SetProperty(ref _strength, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _currentStock = 0m;

        /// <summary>موجودی فعلی</summary>
        public decimal CurrentStock
        {
            get => _currentStock;
            set
            {
                if (SetProperty(ref _currentStock, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _reorderPoint = 0m;

        /// <summary>نقطه سفارش</summary>
        public decimal ReorderPoint
        {
            get => _reorderPoint;
            set
            {
                if (SetProperty(ref _reorderPoint, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _maxStock = 0m;

        /// <summary>حداکثر موجودی</summary>
        public decimal MaxStock
        {
            get => _maxStock;
            set
            {
                if (SetProperty(ref _maxStock, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _unit = string.Empty;

        /// <summary>واحد</summary>
        public string Unit
        {
            get => _unit;
            set
            {
                if (SetProperty(ref _unit, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _unitPrice = 0m;

        /// <summary>قیمت واحد</summary>
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

        private decimal _lastPurchasePrice = 0m;

        /// <summary>آخرین قیمت خرید</summary>
        public decimal LastPurchasePrice
        {
            get => _lastPurchasePrice;
            set
            {
                if (SetProperty(ref _lastPurchasePrice, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private DateTime? _expiryDate = null;

        /// <summary>تاریخ انقضا</summary>
        public DateTime? ExpiryDate
        {
            get => _expiryDate;
            set
            {
                if (SetProperty(ref _expiryDate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _supplierId = null;

        /// <summary>تأمین‌کننده</summary>
        public Guid? SupplierId
        {
            get => _supplierId;
            set
            {
                if (SetProperty(ref _supplierId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _barcode = string.Empty;

        /// <summary>بارکد</summary>
        public string Barcode
        {
            get => _barcode;
            set
            {
                if (SetProperty(ref _barcode, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _storageConditions = string.Empty;

        /// <summary>شرایط نگهداری</summary>
        public string StorageConditions
        {
            get => _storageConditions;
            set
            {
                if (SetProperty(ref _storageConditions, value))
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
            if (string.IsNullOrWhiteSpace(DrugName))
                AddError("نام دارو الزامی است", nameof(DrugName));
            if (string.IsNullOrWhiteSpace(DrugCode))
                AddError("کد دارو الزامی است", nameof(DrugCode));
            if (string.IsNullOrWhiteSpace(Category))
                AddError("دسته‌بندی الزامی است", nameof(Category));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<PharmacyItemFormDto>("api/v1/<built-in method lower of str object at 0x7f5384c8b320>s", payload, ct);

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
