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
    /// ViewModel صفحه‌ی «لوازم مصرفی» — ثبت مصرف لوازم در اقدامات
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/ConsumablesView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/ConsumablesView.html</c>
    /// 🎨 View متناظر:     <c>03-views/ConsumablesView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class ConsumablesViewModel : PageViewModelBase<ConsumablesItem>
    {
        public ConsumablesViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════




        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _addCommand;

        /// <summary>ثبت مصرف</summary>
        public IAsyncRelayCommand AddCommand =>
            _addCommand ??= new AsyncRelayCommand(AddAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<ConsumablesItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<ConsumablesItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a952f0>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<ConsumablesItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<ConsumablesItem>();
            return new PagedResult<ConsumablesItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// ثبت مصرف
        /// </summary>
        private async Task addAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "ثبت مصرف");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «لوازم مصرفی»
    /// </summary>
    public sealed class ConsumablesItem
    {
        /// <summary>کالا</summary>
        public string ItemName { get; set; } = string.Empty;

        /// <summary>مقدار</summary>
        public string Quantity { get; set; } = string.Empty;

        /// <summary>واحد</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>تاریخ</summary>
        public string VisitDate { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}
