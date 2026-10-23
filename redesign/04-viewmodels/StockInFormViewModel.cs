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
    /// ViewModel صفحه‌ی «ورود کالا» — ثبت ورود دارو به انبار
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/StockInFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/StockInFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/StockInFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class StockInFormViewModel : FormViewModelBase
    {
        public StockInFormViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private Guid? _supplierId = null;

        /// <summary>تأمین‌کننده (الزامی)</summary>
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

        private string _invoiceNumber = string.Empty;

        /// <summary>شماره فاکتور (الزامی)</summary>
        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set
            {
                if (SetProperty(ref _invoiceNumber, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private DateTime? _invoiceDate = null;

        /// <summary>تاریخ فاکتور (الزامی)</summary>
        public DateTime? InvoiceDate
        {
            get => _invoiceDate;
            set
            {
                if (SetProperty(ref _invoiceDate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _totalAmount = 0m;

        /// <summary>مبلغ کل</summary>
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

        private Guid? _drugId = null;

        /// <summary>دارو (الزامی)</summary>
        public Guid? DrugId
        {
            get => _drugId;
            set
            {
                if (SetProperty(ref _drugId, value))
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

        private string _batchNumber = string.Empty;

        /// <summary>شماره بچ</summary>
        public string BatchNumber
        {
            get => _batchNumber;
            set
            {
                if (SetProperty(ref _batchNumber, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private DateTime? _expiryDate = null;

        /// <summary>تاریخ انقضا (الزامی)</summary>
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


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _addRowCommand;

        /// <summary>افزودن ردیف</summary>
        public IAsyncRelayCommand AddRowCommand =>
            _addRowCommand ??= new AsyncRelayCommand(AddRowAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (SupplierId is null)
                AddError("تأمین‌کننده الزامی است", nameof(SupplierId));
            if (string.IsNullOrWhiteSpace(InvoiceNumber))
                AddError("شماره فاکتور الزامی است", nameof(InvoiceNumber));
            if (InvoiceDate is null)
                AddError("تاریخ فاکتور الزامی است", nameof(InvoiceDate));
            if (DrugId is null)
                AddError("دارو الزامی است", nameof(DrugId));
            if (Quantity <= 0)
                AddError("تعداد باید بزرگ‌تر از صفر باشد", nameof(Quantity));
            if (UnitPrice <= 0)
                AddError("قیمت واحد باید بزرگ‌تر از صفر باشد", nameof(UnitPrice));
            if (ExpiryDate is null)
                AddError("تاریخ انقضا الزامی است", nameof(ExpiryDate));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<StockInFormDto>("api/v1/<built-in method lower of str object at 0x7f5384a952f0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// افزودن ردیف
        /// </summary>
        private async Task addRowAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "افزودن ردیف");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}
