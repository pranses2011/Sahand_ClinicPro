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
    /// ViewModel صفحه‌ی «تصویربرداری» — مدیریت درخواست‌ها و تصاویر پزشکی
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/ImagingView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/ImagingView.html</c>
    /// 🎨 View متناظر:     <c>03-views/ImagingView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class ImagingViewModel : PageViewModelBase<ImagingItem>
    {
        public ImagingViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════



        private string _filterModality = null;

        /// <summary>فیلتر: روش</summary>
        public string FilterModality
        {
            get => _filterModality;
            set
            {
                if (SetProperty(ref _filterModality, value))
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

        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _newOrderCommand;

        /// <summary>+ درخواست جدید</summary>
        public IAsyncRelayCommand NewOrderCommand =>
            _newOrderCommand ??= new AsyncRelayCommand(NewOrderAsync);

        private IAsyncRelayCommand? _openViewerCommand;

        /// <summary>نمایش تصاویر</summary>
        public IAsyncRelayCommand OpenViewerCommand =>
            _openViewerCommand ??= new AsyncRelayCommand(OpenViewerAsync);

        private IAsyncRelayCommand? _uploadCommand;

        /// <summary>بارگذاری تصویر</summary>
        public IAsyncRelayCommand UploadCommand =>
            _uploadCommand ??= new AsyncRelayCommand(UploadAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<ImagingItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<ImagingItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a95470>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<ImagingItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<ImagingItem>();
            return new PagedResult<ImagingItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// + درخواست جدید
        /// </summary>
        private async Task newOrderAsync()
        {
            // TODO: باز کردن فرم ایجاد از طریق IDialogService
            await Task.CompletedTask;
        }

        /// <summary>
        /// نمایش تصاویر
        /// </summary>
        private async Task openViewerAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "نمایش تصاویر");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// بارگذاری تصویر
        /// </summary>
        private async Task uploadAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "بارگذاری تصویر");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «تصویربرداری»
    /// </summary>
    public sealed class ImagingItem
    {
        /// <summary>شماره</summary>
        public string StudyNumber { get; set; } = string.Empty;

        /// <summary>بیمار</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>روش</summary>
        public string Modality { get; set; } = string.Empty;

        /// <summary>عضو</summary>
        public string BodyPart { get; set; } = string.Empty;

        /// <summary>تاریخ</summary>
        public string StudyDate { get; set; } = string.Empty;

        /// <summary>وضعیت</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}
