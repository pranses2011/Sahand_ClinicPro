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
    /// ViewModel صفحه‌ی «پزشکان» — مدیریت پزشکان، تخصص‌ها و برنامه‌ی کاری
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/DoctorsView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/DoctorsView.html</c>
    /// 🎨 View متناظر:     <c>03-views/DoctorsView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class DoctorsViewModel : PageViewModelBase<DoctorsItem>
    {
        public DoctorsViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════



        private string _filterSpecialty = null;

        /// <summary>فیلتر: تخصص</summary>
        public string FilterSpecialty
        {
            get => _filterSpecialty;
            set
            {
                if (SetProperty(ref _filterSpecialty, value))
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

        private IAsyncRelayCommand? _newCommand;

        /// <summary>+ پزشک جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            _newCommand ??= new AsyncRelayCommand(NewAsync);

        private IAsyncRelayCommand? _scheduleCommand;

        /// <summary>برنامه کاری</summary>
        public IAsyncRelayCommand ScheduleCommand =>
            _scheduleCommand ??= new AsyncRelayCommand(ScheduleAsync);

        private IAsyncRelayCommand? _commissionCommand;

        /// <summary>کارمزد</summary>
        public IAsyncRelayCommand CommissionCommand =>
            _commissionCommand ??= new AsyncRelayCommand(CommissionAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<DoctorsItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<DoctorsItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a95170>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<DoctorsItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<DoctorsItem>();
            return new PagedResult<DoctorsItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// + پزشک جدید
        /// </summary>
        private async Task newAsync()
        {
            // TODO: باز کردن فرم ایجاد از طریق IDialogService
            await Task.CompletedTask;
        }

        /// <summary>
        /// برنامه کاری
        /// </summary>
        private async Task scheduleAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "برنامه کاری");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// کارمزد
        /// </summary>
        private async Task commissionAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "کارمزد");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «پزشکان»
    /// </summary>
    public sealed class DoctorsItem
    {
        /// <summary>کد</summary>
        public string DoctorCode { get; set; } = string.Empty;

        /// <summary>نام پزشک</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>تخصص</summary>
        public string Specialty { get; set; } = string.Empty;

        /// <summary>نظام پزشکی</summary>
        public string MedicalLicense { get; set; } = string.Empty;

        /// <summary>تلفن</summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>وضعیت</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}
