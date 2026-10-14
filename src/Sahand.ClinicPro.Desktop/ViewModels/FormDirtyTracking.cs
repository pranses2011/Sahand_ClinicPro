using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// قرارداد «فرم با تغییرات ذخیره‌نشده» — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
/// «در تمامی فرم‌هایی که باز می‌شوند کلید کنسل یا ESC را تنظیم کردی برای
/// بستن کادرها و فرم‌ها ولی باید قبل از بستن بررسی کنه که تغییری داده نشده
/// باشه و اگه تغییری باشه، با تایید کاربر فرم رو ببنده».
/// هر ViewModel فرمی که این قرارداد را پیاده کند، هنگام ESC/بستن پنجره
/// پیش از بستن بررسی می‌شود و در صورت داشتن تغییراتِ ذخیره‌نشده، تأیید
/// فارسی خود برنامه پرسیده می‌شود (AppDialog — نه کادر پیام ویندوز).
/// </summary>
public interface IHasUnsavedChanges
{
    /// <summary>آیا فرم تغییرات ذخیره‌نشده دارد؟ true یعنی پیش از بستن تأیید بگیر</summary>
    bool HasUnsavedChanges { get; }

    /// <summary>عنوان فرم برای پیام تأیید — پیش‌فرض: همین فرم</summary>
    string UnsavedChangesTitle => "فرم جاری";
}

/// <summary>
/// ═══ اسنپ‌شات عمومی فرم — چرخهٔ ۷۵ (گزارش مالک) ═══
/// «قانون بررسی تغییر شدن فرم که برای کلید ESC بود عمل نمیکنه — چک نمیکنه
/// ببینه که اطلاعات فرم تغییر کرده یا نه و در هر صورت خارج میشه بدون تایید
/// کردن». ریشه: ردیاب FormViewModelBase فقط در ۲ فرم (پرسنل/تعرفه) نشسته
/// بود و بقیهٔ فرم‌ها (بیمار/پذیرش/فاکتور/نوبت/…) هیچ ردیابی نداشتند. این
/// اسنپ‌شاتِ ایستا همان منطق بازتابی را برای «هر ViewModel» فراهم می‌کند تا
/// پوسته (App.xaml.cs) بدون وابستگی به قرارداد، همهٔ فرم‌ها را خودکار
/// بپاید: اسنپ‌شات هنگام باز شدن + مقایسه هنگام ESC/بستن.
/// </summary>
public static class FormSnapshot
{
    /// <summary>
    /// نام‌های پراپرتیِ «پرنوسان» — وضعیت‌های گذرای فرم که تغییرشان یعنی
    /// کارِ پس‌زمینه (در حال ذخیره/بارگذاری/پیام‌ها) نه ورودی کاربر؛ از
    /// اسنپ‌شات کنار گذاشته می‌شوند تا هشدار کاذب بسته‌شدن نیاید.
    /// </summary>
    private static readonly HashSet<string> VolatileNames = new(StringComparer.Ordinal)
    {
        "IsBusy", "IsLoading", "IsSaving", "BusyText", "IsInitialized",
        "ErrorMessage", "StatusMessage", "SuccessMessage", "LoadFailed",
        "HasErrors", "HasSavedResult", "SavedFlagText", "SavedFlagBrush",
        "HasSavedResult", "UnsavedChangesTitle", "HasUnsavedChanges", "Title",
    };

    /// <summary>
    /// گرفتن اسنپ‌شات JSON مرتب از پراپرتی‌های داده‌ای سادهٔ یک ViewModel —
    /// رشته/عدد/بول/enum (+ تهی‌پذیرها) و «شمار» مجموعه‌ها؛ خروجی برای دو
    /// وضعیت هم‌ارز دقیقاً یکسان است.
    /// </summary>
    /// <param name="viewModel">هر شیء — معمولاً DataContext فرم</param>
    public static string? Capture(object? viewModel)
    {
        if (viewModel is null) return null;
        try
        {
            var bag = new SortedDictionary<string, object?>();
            foreach (var prop in viewModel.GetType()
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0) continue;
                if (VolatileNames.Contains(prop.Name)) continue;

                object? value;
                try { value = prop.GetValue(viewModel); }
                catch { continue; } // پراپرتی محاسباتی خطادار — نادیده

                var type = prop.PropertyType;
                var isSimple = type == typeof(string)
                               || type == typeof(decimal) || type == typeof(decimal?)
                               || type == typeof(int) || type == typeof(int?)
                               || type == typeof(long) || type == typeof(long?)
                               || type == typeof(bool) || type == typeof(bool?)
                               || type == typeof(double) || type == typeof(double?)
                               || type == typeof(DateTime) || type == typeof(DateTime?)
                               || type.IsEnum
                               || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                                   && Nullable.GetUnderlyingType(type)!.IsEnum);
                if (isSimple)
                {
                    bag[prop.Name] = value;
                    continue;
                }

                // مجموعه‌ها — فقط شمار اقلام (افزودن/حذف سریع ردیابی می‌شود)
                if (value is System.Collections.ICollection col)
                    bag[prop.Name] = $"#{col.Count}";
            }

            return JsonSerializer.Serialize(bag);
        }
        catch
        {
            return null; // بازتاب ناکام — بی‌خطر: فرم «بدون ردیابی» تلقی می‌شود
        }
    }

    /// <summary>
    /// آیا دو اسنپ‌شات هم‌ارزند؟ (تهی = بدون اسنپ‌شات = همیشه «بدون تغییر»)
    /// </summary>
    public static bool IsSame(string? baseline, string? current)
        => string.Equals(baseline, current, StringComparison.Ordinal);
}

/// <summary>
/// ردیاب عمومی تغییرات فرم با «اسنپ‌شات بازتابی» — چرخهٔ بیست‌وششم.
/// در لحظهٔ آماده‌شدن فرم (پایان بارگذاری ویرایش یا فرم خالی جدید) یک
/// اسنپ‌شات از همهٔ پراپرتی‌های داده‌ای سادهٔ ViewModel گرفته می‌شود و در
/// هر لحظه با مقایسهٔ اسنپ‌شات تازه، «آیا چیزی تغییر کرده؟» پاسخ داده
/// می‌شود — بدون نیاز به کد ردیابی دستی برای هر فیلد در هر فرم.
/// </summary>
public abstract class FormViewModelBase : ObservableObject, IHasUnsavedChanges
{
    /// <summary>اسنپ‌شات مبنا — وضعیت فرم هنگام «تمیز» بودن (بارگذاری/پس از ذخیره)</summary>
    private string? _baselineSnapshot;

    /// <summary>در حال حاضر در حال بارگذاری اولیه است؟ اسنپ‌شات مبنا هنوز معتبر نیست</summary>
    private bool _isInitializing = true;

    /// <summary>عنوان فرم برای پیام تأیید تغییرات ذخیره‌نشده — کلاس فرزست بازنویسی می‌کند</summary>
    public virtual string UnsavedChangesTitle => "فرم جاری";

    /// <summary>
    /// آیا فرم تغییرات ذخیره‌نشده دارد؟ — مقایسهٔ اسنپ‌شات فعلی با مبنا؛
    /// تا پایان بارگذاری اولیه همیشه false است (فرم در حال پرشدن است،
    /// نه ویرایش کاربر)
    /// </summary>
    public bool HasUnsavedChanges =>
        !_isInitializing
        && _baselineSnapshot is not null
        && !string.Equals(_baselineSnapshot, SnapshotFields(), StringComparison.Ordinal);

    /// <summary>
    /// ثابت‌کردن وضعیت فعلی به‌عنوان «بدون تغییر» — پس از پایان بارگذاری
    /// اولیه و پس از هر ذخیرهٔ موفق فراخوانی می‌شود تا دوباره از صفر شمارش شود
    /// </summary>
    protected void MarkClean()
    {
        _baselineSnapshot = SnapshotFields();
        _isInitializing = false;
    }

    /// <summary>نشانه‌گذاری شروع بارگذاری — تغییرات این بازه نادیده گرفته می‌شوند</summary>
    protected void BeginInit()
    {
        _isInitializing = true;
    }

    /// <summary>
    /// اسنپ‌شات بازتابی از پراپرتی‌های داده‌ای سادهٔ عمومی — رشته/عدد/بول/enum
    /// و نسخه‌های تهی‌پذیرشان؛ خروجی JSON مرتب‌شده با نام پراپرتی است تا دو
    /// اسنپ‌شات هم‌ارز دقیقاً یک رشته شوند. مجموعه‌ها (فهرست‌ها) با شمارشان
    /// وارد می‌شوند تا اقلام پویا (مثل اقلام نسخه) هم در ردیابی بیایند.
    /// </summary>
    private string SnapshotFields()
    {
        var bag = new SortedDictionary<string, object?>();
        foreach (var prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length > 0) continue;
            if (prop.Name is nameof(HasUnsavedChanges) or nameof(UnsavedChangesTitle)) continue;

            var type = prop.PropertyType;
            object? value;
            try { value = prop.GetValue(this); }
            catch { continue; } // پراپرتی محاسباتی خطادار — نادیده

            // انواع داده‌ای ساده — سایر انواع (Brush و مانند آن) بی‌ربط‌اند
            var isSimple = type == typeof(string)
                           || type == typeof(decimal) || type == typeof(decimal?)
                           || type == typeof(int) || type == typeof(int?)
                           || type == typeof(long) || type == typeof(long?)
                           || type == typeof(bool) || type == typeof(bool?)
                           || type == typeof(double) || type == typeof(double?)
                           || type.IsEnum
                           || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                               && Nullable.GetUnderlyingType(type)!.IsEnum);
            if (isSimple)
            {
                bag[prop.Name] = value;
                continue;
            }

            // مجموعه‌ها — فقط «شمار» اقلام برای ردیابی افزودن/حذف سریع
            if (value is System.Collections.ICollection col)
                bag[prop.Name] = $"#{col.Count}";
        }

        return JsonSerializer.Serialize(bag);
    }
}
