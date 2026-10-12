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
    /// ViewModel صفحه‌ی «شیفت‌ها» — مدیریت شیفت‌های کاری
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/ShiftsView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/ShiftsView.html</c>
    /// 🎨 View متناظر:     <c>03-views/ShiftsView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class ShiftsViewModel : PageViewModelBase<ShiftsItem>
    {
        public ShiftsViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════



        private DateTime? _filterDate = null;

        /// <summary>فیلتر: تاریخ</summary>
        public DateTime? FilterDate
        {
            get => _filterDate;
            set
            {
                if (SetProperty(ref _filterDate, value))
                    _ = LoadAsync();
            }
        }

        private string _filterUser = null;

        /// <summary>فیلتر: کاربر</summary>
        public string FilterUser
        {
            get => _filterUser;
            set
            {
                if (SetProperty(ref _filterUser, value))
                    _ = LoadAsync();
            }
        }

        private string _filterStatus = null;

        /// <summary>فیلتر: وضعیت</summary>
        public string FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (SetProperty(ref _filterStatus, value))
                    _ = LoadAsync();
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _openShiftCommand;

        /// <summary>گشایش شیفت</summary>
        public IAsyncRelayCommand OpenShiftCommand =>
            _openShiftCommand ??= new AsyncRelayCommand(OpenShiftAsync);

        private IAsyncRelayCommand? _closeShiftCommand;

        /// <summary>بستن شیفت</summary>
        public IAsyncRelayCommand CloseShiftCommand =>
            _closeShiftCommand ??= new AsyncRelayCommand(CloseShiftAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<ShiftsItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<ShiftsItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a951f0>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<ShiftsItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<ShiftsItem>();
            return new PagedResult<ShiftsItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// گشایش شیفت
        /// </summary>
        private async Task openShiftAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "گشایش شیفت");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// بستن شیفت
        /// </summary>
        private async Task closeShiftAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "بستن شیفت");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «شیفت‌ها»
    /// </summary>
    public sealed class ShiftsItem
    {
        /// <summary>شیفت</summary>
        public string ShiftName { get; set; } = string.Empty;

        /// <summary>کاربر</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>شروع</summary>
        public string StartTime { get; set; } = string.Empty;

        /// <summary>پایان</summary>
        public string EndTime { get; set; } = string.Empty;

        /// <summary>مبلغ کل</summary>
        public string TotalAmount { get; set; } = string.Empty;

        /// <summary>وضعیت</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}
