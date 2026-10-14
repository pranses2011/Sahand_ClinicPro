using System.Runtime.InteropServices;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>مشخصات یک نمایشگر متصل — نام دستگاه، پریمری‌بودن و مستطیل پیکسلی</summary>
public record MonitorInfo(string DeviceName, bool Primary, int X, int Y, int Width, int Height)
{
    /// <summary>عنوان نمایشی برای انتخابگر تنظیمات</summary>
    public string Title => $"{(Primary ? "مانیتور اصلی" : "نمایشگر")} — {Width}×{Height}";
}

/// <summary>
/// شمارش نمایشگرهای متصل ویندوز — چرخهٔ ۷۴ (نمایشگر سالن انتظار).
/// گزارش مالک: «میخوام طوری پیاده سازیش بکنی که بتونم روی یک مانیتور دیگه و
/// یا یک تلویزیون بندازمش». از EnumDisplayMonitors/GetMonitorInfo خود ویندوز
/// استفاده می‌شود (WPF فهرست نمایشگر ندارد و افزودن WinForms به پروژه با
/// usingهای سراسری‌اش ابهام نوع در کل برنامه می‌سازد — P/Invoke سبک‌تر است).
/// مختصات پیکسلی واقعی است و مستقل از DPI — همان چیزی که MoveWindow می‌خواهد.
/// </summary>
public static class Monitors
{
    /// <summary>همهٔ نمایشگرهای متصل — به ترتیب سیستم؛ خالی در هر خطای native</summary>
    public static List<MonitorInfo> All()
    {
        var list = new List<MonitorInfo>();
        try
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr monitor, IntPtr hdc, ref RECT rect, IntPtr data) =>
                {
                    var info = new MONITORINFOEX();
                    info.Size = Marshal.SizeOf<MONITORINFOEX>();
                    if (GetMonitorInfo(monitor, ref info))
                    {
                        list.Add(new MonitorInfo(
                            info.DeviceName,
                            (info.Flags & 1) != 0, // MONITORINFOF_PRIMARY
                            info.Monitor.Left, info.Monitor.Top,
                            info.Monitor.Right - info.Monitor.Left,
                            info.Monitor.Bottom - info.Monitor.Top));
                    }
                    return true; // ادامهٔ شمارش
                }, IntPtr.Zero);
        }
        catch
        {
            // خطای native — فراخواننده با فهرست خالی به مانیتور اصلی برمی‌گردد
        }
        return list;
    }

    /// <summary>یافتن مانیتور ذخیره‌شدهٔ تنظیمات — تهی = مانیتور اصلی (یا اولین)</summary>
    public static MonitorInfo? Find(string deviceName)
    {
        var all = All();
        if (all.Count == 0)
            return null;
        return all.FirstOrDefault(m => string.Equals(m.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
               ?? all.FirstOrDefault(m => m.Primary)
               ?? all[0];
    }

    #region Native — ساختارها و توابع ویندوز

    /// <summary>مستطیل پیکسلی</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>اطلاعات کامل مانیتور با نام دستگاه</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public int Size;
        public RECT Monitor;
        public RECT Work;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    /// <summary>شمارش نمایشگرها با تابع callback</summary>
    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clipRect,
        MonitorEnumProc proc, IntPtr data);

    /// <summary>خواندن مشخصات یک مانیتور</summary>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX info);

    /// <summary>امضای callback شمارش</summary>
    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref RECT rect, IntPtr data);

    #endregion
}
