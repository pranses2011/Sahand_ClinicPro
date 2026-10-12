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
    /// ViewModel صفحه‌ی «اتاق‌ها» — مدیریت اتاق‌های معاینه
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/RoomsView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/RoomsView.html</c>
    /// 🎨 View متناظر:     <c>03-views/RoomsView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class RoomsViewModel : PageViewModelBase<RoomsItem>
    {
        public RoomsViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════




        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _newCommand;

        /// <summary>+ اتاق جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            _newCommand ??= new AsyncRelayCommand(NewAsync);

        private IAsyncRelayCommand? _manageCommand;

        /// <summary>مدیریت اتاق‌ها</summary>
        public IAsyncRelayCommand ManageCommand =>
            _manageCommand ??= new AsyncRelayCommand(ManageAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<RoomsItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<RoomsItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a95130>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<RoomsItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<RoomsItem>();
            return new PagedResult<RoomsItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// + اتاق جدید
        /// </summary>
        private async Task newAsync()
        {
            // TODO: باز کردن فرم ایجاد از طریق IDialogService
            await Task.CompletedTask;
        }

        /// <summary>
        /// مدیریت اتاق‌ها
        /// </summary>
        private async Task manageAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "مدیریت اتاق‌ها");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «اتاق‌ها»
    /// </summary>
    public sealed class RoomsItem
    {
        /// <summary>شماره</summary>
        public string RoomNumber { get; set; } = string.Empty;

        /// <summary>نام اتاق</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>پزشک مستقر</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>وضعیت</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}
