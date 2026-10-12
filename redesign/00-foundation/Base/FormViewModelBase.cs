using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Sahand.ClinicPro.Desktop.Base
{
    /// <summary>
    /// پایه‌ی ViewModel برای فرم‌ها و پنجره‌های «ایجاد / ویرایش»
    /// </summary>
    /// <remarks>
    /// امکانات:
    /// <list type="bullet">
    /// <item>ردیابی تغییرات (Dirty Tracking) با پیام تأیید هنگام بستن</item>
    /// <item>حالت فقط‌خواندنی</item>
    /// <item>اعتبارسنجی پیش از ذخیره</item>
    /// <item>بستن پنجره با نتیجه</item>
    /// </remarks>
    public abstract class FormViewModelBase : ViewModelBase
    {
        private bool _isDirty;
        private bool _isReadOnly;
        private bool _isEditMode;

        protected FormViewModelBase(ILogger? logger = null) : base(logger) { }

        /// <summary>آیا تغییری نسبت به مقدار اولیه ایجاد شده است</summary>
        public bool IsDirty
        {
            get => _isDirty;
            protected set
            {
                if (SetProperty(ref _isDirty, value))
                    SaveCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>آیا فرم فقط‌خواندنی است</summary>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            protected set
            {
                if (SetProperty(ref _isReadOnly, value))
                    OnPropertiesChanged(nameof(IsEditable), nameof(WindowTitle));
            }
        }

        /// <summary>معکوسِ <see cref="IsReadOnly"/></summary>
        public bool IsEditable => !IsReadOnly;

        /// <summary>آیا در حالت ویرایش هستیم (در غیر این صورت ایجاد)</summary>
        public bool IsEditMode
        {
            get => _isEditMode;
            protected set
            {
                if (SetProperty(ref _isEditMode, value))
                    OnPropertyChanged(nameof(WindowTitle));
            }
        }

        /// <summary>عنوان پنجره — در کلاس فرزند بازنویسی شود</summary>
        public virtual string WindowTitle => IsEditMode ? "ویرایش" : "جدید";

        /// <summary>عملِ بستن پنجره — از View تنظیم می‌شود</summary>
        public Action<bool>? CloseAction { get; set; }

        // ══════════ فرمان‌ها ══════════

        /// <summary>ذخیره‌ی فرم</summary>
        public IAsyncRelayCommand SaveCommand =>
            new AsyncRelayCommand(SaveAsync, () => IsEditable && !HasErrors);

        /// <summary>انصراف و بستن</summary>
        public IRelayCommand CancelCommand => new RelayCommand(() => CloseAction?.Invoke(false));

        // ══════════ رفتار ══════════

        /// <summary>
        /// ذخیره با بررسی تغییرات و اعتبارسنجی
        /// </summary>
        protected async Task SaveAsync()
        {
            ClearAllErrors();
            if (!Validate())
            {
                ShowError("لطفاً خطاهای فرم را اصلاح کنید");
                return;
            }

            var ok = await RunAsync(async ct =>
            {
                await PersistAsync(ct);
                IsDirty = false;
            }, "در حال ذخیره…");

            if (!ok) return;

            ShowSuccess("اطلاعات با موفقیت ذخیره شد");
            CloseAction?.Invoke(true);
        }

        /// <summary>
        /// علامت‌گذاری فرم به‌عنوان تغییریافته
        /// </summary>
        protected void MarkDirty() => IsDirty = true;

        /// <summary>
        /// اعتبارسنجی سمت کلاینت — در کلاس فرزند پیاده‌سازی می‌شود
        /// </summary>
        protected virtual bool Validate() => true;

        /// <summary>
        /// ذخیره‌ی واقعی داده‌ها — در کلاس فرزند پیاده‌سازی می‌شود
        /// </summary>
        protected abstract Task PersistAsync(System.Threading.CancellationToken ct);
    }
}
