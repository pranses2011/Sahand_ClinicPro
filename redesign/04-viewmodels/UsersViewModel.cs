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
    /// ViewModel صفحه‌ی «کاربران» — مدیریت حساب‌های کاربری
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/UsersView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/UsersView.html</c>
    /// 🎨 View متناظر:     <c>03-views/UsersView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class UsersViewModel : PageViewModelBase<UsersItem>
    {
        public UsersViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════



        private string _filterRole = null;

        /// <summary>فیلتر: نقش</summary>
        public string FilterRole
        {
            get => _filterRole;
            set
            {
                if (SetProperty(ref _filterRole, value))
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

        /// <summary>+ کاربر جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            _newCommand ??= new AsyncRelayCommand(NewAsync);

        private IAsyncRelayCommand? _resetPasswordCommand;

        /// <summary>بازنشانی رمز</summary>
        public IAsyncRelayCommand ResetPasswordCommand =>
            _resetPasswordCommand ??= new AsyncRelayCommand(ResetPasswordAsync);

        private IAsyncRelayCommand? _deactivateCommand;

        /// <summary>غیرفعال‌سازی</summary>
        public IAsyncRelayCommand DeactivateCommand =>
            _deactivateCommand ??= new AsyncRelayCommand(DeactivateAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<UsersItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<UsersItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a955f0>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<UsersItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<UsersItem>();
            return new PagedResult<UsersItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// + کاربر جدید
        /// </summary>
        private async Task newAsync()
        {
            // TODO: باز کردن فرم ایجاد از طریق IDialogService
            await Task.CompletedTask;
        }

        /// <summary>
        /// بازنشانی رمز
        /// </summary>
        private async Task resetPasswordAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "بازنشانی رمز");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// غیرفعال‌سازی
        /// </summary>
        private async Task deactivateAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "غیرفعال‌سازی");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «کاربران»
    /// </summary>
    public sealed class UsersItem
    {
        /// <summary>نام کاربری</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>نام کامل</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>نقش</summary>
        public string RoleName { get; set; } = string.Empty;

        /// <summary>ایمیل</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>آخرین ورود</summary>
        public string LastLoginAt { get; set; } = string.Empty;

        /// <summary>فعال</summary>
        public string IsActive { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}
