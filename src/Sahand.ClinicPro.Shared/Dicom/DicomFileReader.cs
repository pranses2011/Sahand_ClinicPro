using System.Text;

namespace Sahand.ClinicPro.Shared.Dicom;

/// <summary>
/// نتیجه تجزیه فایل DICOM — داده مستقل از UI برای نمایش در WPF
/// </summary>
public sealed class DicomImage
{
    /// <summary>عرض تصویر (پیکسل)</summary>
    public int Width { get; init; }

    /// <summary>ارتفاع تصویر (پیکسل)</summary>
    public int Height { get; init; }

    /// <summary>بیت‌های تخصیص‌یافته به هر پیکسل (۸ یا ۱۶)</summary>
    public int BitsAllocated { get; init; }

    /// <summary>آیا داده پیکسل علامت‌دار است</summary>
    public bool IsSigned { get; init; }

    /// <summary>تفسیر فتومتریک — MONOCHROME2 / RGB و...</summary>
    public string PhotometricInterpretation { get; init; } = "MONOCHROME2";

    /// <summary>پیکسل‌های خام ۱۶بیتی بدون علامت — فقط تصاویر خاکستری ۱۶بیتی</summary>
    public ushort[]? Gray16 { get; init; }

    /// <summary>پیکسل‌های ۸بیتی خاکستری — تصاویر ۸بیتی</summary>
    public byte[]? Gray8 { get; init; }

    /// <summary>پیکسل‌های RGB درهم‌آمیخته ۲۴بیتی — فقط تصاویر رنگی</summary>
    public byte[]? Rgb24 { get; init; }

    /// <summary>مرکز پنجره پیش‌فرض از فایل — تهی یعنی نامشخص</summary>
    public double? WindowCenter { get; init; }

    /// <summary>عرض پنجره پیش‌فرض از فایل — تهی یعنی نامشخص</summary>
    public double? WindowWidth { get; init; }

    /// <summary>شیب مقیاس‌بندی مجدد (RescaleSlope)</summary>
    public double RescaleSlope { get; init; } = 1.0;

    /// <summary>عرض مقیاس‌بندی مجدد (RescaleIntercept)</summary>
    public double RescaleIntercept { get; init; }

    /// <summary>متادیتای خوانده‌شده — عنوان فارسی + مقدار</summary>
    public IReadOnlyList<(string Name, string Value)> Metadata { get; init; }
        = Array.Empty<(string, string)>();

    /// <summary>
    /// خروجی ۸بیتی خاکستری آماده نمایش با پنجره/سطح دلخواه —
    /// تصاویر ۸بیتی مستقیم و ۱۶بیتی با نگاشت خطی بازه پنجره
    /// </summary>
    /// <param name="windowCenter">مرکز پنجره</param>
    /// <param name="windowWidth">عرض پنجره</param>
    /// <exception cref="InvalidOperationException">نوع پیکسل پشتیبانی نمی‌شود</exception>
    public byte[] RenderGray8(double windowCenter, double windowWidth)
    {
        if (Gray8 is not null) return Gray8;

        if (Gray16 is null)
            throw new InvalidOperationException(
                "این تصویر رنگی است — از RenderRgb استفاده کنید");

        var pixels = new byte[Gray16.Length];
        var half = Math.Max(0.5, windowWidth / 2.0);
        var low = windowCenter - half;
        var high = windowCenter + half;
        var range = high - low;
        if (range < 1) range = 1;

        for (var i = 0; i < Gray16.Length; i++)
        {
            // مقیاس‌بندی مجدد هنگام تفسیر (مثل CT با واحد Hounsfield)
            var physical = Gray16[i] * RescaleSlope + RescaleIntercept;

            if (physical <= low) { pixels[i] = 0; continue; }
            if (physical >= high) { pixels[i] = 255; continue; }
            pixels[i] = (byte)Math.Round((physical - low) * 255.0 / range);
        }

        return pixels;
    }

    /// <summary>خروجی RGB — تصاویر رنگی مستقیم، خاکستری تکرار سه‌کاناله</summary>
    public byte[] RenderRgb()
    {
        if (Rgb24 is not null) return Rgb24;

        var gray = RenderGray8(WindowCenter ?? 0, WindowWidth ?? 1);
        var rgb = new byte[gray.Length * 3];
        for (var i = 0; i < gray.Length; i++)
        {
            rgb[i * 3] = gray[i];
            rgb[i * 3 + 1] = gray[i];
            rgb[i * 3 + 2] = gray[i];
        }
        return rgb;
    }

    /// <summary>حداقل/حداکثر مقدار فیزیکی — برای پنجره خودکار</summary>
    public (double Min, double Max) RawRange()
    {
        if (Gray16 is { } g16 && g16.Length > 0)
        {
            ushort min = g16[0], max = g16[0];
            foreach (var v in g16)
            {
                if (v < min) min = v;
                if (v > max) max = v;
            }
            var pmin = min * RescaleSlope + RescaleIntercept;
            var pmax = max * RescaleSlope + RescaleIntercept;
            return pmin <= pmax ? (pmin, pmax) : (pmax, pmin);
        }
        return (0, 255);
    }
}

/// <summary>
/// تجزیه‌گر خالص فایل DICOM — گام R4 (چرخه چهارم)
/// </summary>
/// <remarks>
/// بدون هیچ وابستگی خارجی. پشتیبانی:
/// ۱. پیش‌درآمد استاندارد DICOM P10 (۱۲۸ بایت + DICM)
/// ۲. Transfer Syntax: Implicit VR LE و Explicit VR LE
/// ۳. پیکسل بدون فشرده‌سازی: MONOCHROME1/2 (۸ و ۱۶ بیت) + RGB درهم‌آمیخته
/// ۴. داده پیکسل قطعه‌قطعه‌شده (fragmented) با آیتم‌های undefined-length
/// ۵. دنباله‌ها (SQ) با طول تعریف‌شده و نامعین به‌صورت تودرتو رد می‌شوند
/// فرمت‌های فشرده (JPEG/JPEG-LS/JPEG2000) و Big Endian پشتیبانی نمی‌شوند و
/// خطای فارسی روشن می‌دهند.
/// </remarks>
public static class DicomFileReader
{
    /// <summary>جادوی فایل DICOM پس از پیش‌درآمد ۱۲۸ بایتی</summary>
    private const uint Magic = 0x4D434944; // "DICM" در خوانش Little Endian

    /// <summary>برچسب داده پیکسل — (7FE0,0010)؛ گروه در ۱۶ بیت پایین</summary>
    private const uint PixelDataTag = 0x00107FE0;

    /// <summary>برچسب پایان دنباله — (FFFE,E0DD)</summary>
    private const uint SequenceDelimitationTag = 0xE0DDFFFE;

    /// <summary>برچسب پایان آیتم — (FFFE,E00D)</summary>
    private const uint ItemDelimitationTag = 0xE00DFFFE;

    /// <summary>برچسب آغاز آیتم — (FFFE,E000)</summary>
    private const uint ItemTag = 0xE000FFFE;

    /// <summary>
    /// تشخیص سریع DICOM — جادوی DICM بدون تجزیه کامل
    /// </summary>
    public static bool LooksLikeDicom(Stream stream)
    {
        if (stream is null || !stream.CanRead || stream.Length < 132) return false;
        var origin = stream.Position;
        try
        {
            stream.Position = 128;
            var buffer = new byte[4];
            var read = stream.Read(buffer, 0, 4);
            return read == 4 && buffer[0] == (byte)'D' && buffer[1] == (byte)'I'
                              && buffer[2] == (byte)'C' && buffer[3] == (byte)'M';
        }
        finally
        {
            stream.Position = origin;
        }
    }

    /// <summary>
    /// تجزیه کامل فایل DICOM از مسیر
    /// </summary>
    /// <exception cref="DicomParseException">خطای ساختاری با پیام فارسی</exception>
    public static DicomImage Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    /// <summary>تجزیه کامل از جریان بایت</summary>
    public static DicomImage Load(Stream stream)
    {
        var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        // ── پیش‌درآمد و جادو ──
        var preamble = reader.ReadBytes(128);
        if (preamble.Length < 128) throw new DicomParseException("فایل کوتاه‌تر از حداقل DICOM است");
        var magic = reader.ReadUInt32();
        if (magic != Magic)
            throw new DicomParseException("جادوی DICM یافت نشد — فایل DICOM معتبر نیست");

        // ── گروه فراداده — همیشه Explicit VR Little Endian ──
        var tags = new Dictionary<uint, byte[]>();
        string transferSyntax = "1.2.840.10008.1.2.1"; // پیش‌فرض: Explicit LE

        while (stream.Position < stream.Length)
        {
            var tag = reader.ReadUInt32();
            if ((tag & 0xFFFF) != 0x0002) // گروه در ۱۶ بیت پایین
            {
                stream.Position -= 4; // شروع مجموعه‌داده اصلی
                break;
            }

            var vr = new string(reader.ReadChars(2));
            uint length;
            if (IsLongVr(vr)) { reader.ReadUInt16(); length = reader.ReadUInt32(); }
            else length = reader.ReadUInt16();

            var value = reader.ReadBytes((int)length);
            tags[tag] = value;
            if (tag == 0x00100002) // (0002,0010) Transfer Syntax UID
                transferSyntax = TrimUid(Encoding.ASCII.GetString(value));
        }

        // ── انتخاب قواعد خواندن از Transfer Syntax ──
        var explicitVr = transferSyntax switch
        {
            "1.2.840.10008.1.2" => false,
            "1.2.840.10008.1.2.1" => true,
            "1.2.840.10008.1.2.2" => throw new DicomParseException(
                "قالب Big Endian پشتیبانی نمی‌شود — فایل را با ابزار پذیرش DICOM به Little Endian تبدیل کنید"),
            var ts when ts.StartsWith("1.2.840.10008.1.2.4", StringComparison.Ordinal) => throw new DicomParseException(
                "تصویر DICOM فشرده است (JPEG/JPEG-2000) — نسخه بدون فشرده‌سازی مورد نیاز است"),
            _ => true
        };

        // ── پیمایش مجموعه‌داده اصلی ──
        byte[]? pixelData = null;
        while (stream.Position < stream.Length)
        {
            var tag = reader.ReadUInt32();

            // آیتم‌های ویژه — قطعه‌قطعه شدن داده پیکسل
            if (tag == ItemTag || tag == ItemDelimitationTag || tag == SequenceDelimitationTag)
            {
                var itemLength = reader.ReadUInt32();
                if (pixelData is not null && tag == ItemTag && itemLength is > 0 and not 0xFFFFFFFF)
                {
                    // قطعه بعدی داده پیکسل — به انتهای بافر موجود می‌چسبد
                    var fragment = reader.ReadBytes((int)itemLength);
                    pixelData = Concat(pixelData, fragment);
                }
                else if (itemLength is > 0 and not 0xFFFFFFFF)
                {
                    stream.Position += itemLength; // جدول آفست یا آیتم بی‌ربط
                }
                continue;
            }

            string vr;
            uint length;
            if (explicitVr)
            {
                vr = new string(reader.ReadChars(2));
                if (IsLongVr(vr)) { reader.ReadUInt16(); length = reader.ReadUInt32(); }
                else length = reader.ReadUInt16();
            }
            else
            {
                vr = string.Empty;
                length = reader.ReadUInt32();
            }

            // طول نامعین → دنباله یا داده پیکسل قطعه‌قطعه
            if (length == 0xFFFFFFFF)
            {
                if (tag == PixelDataTag)
                    pixelData = ReadPixelFragments(reader);
                else
                    SkipUndefinedLengthSequence(reader);
                continue;
            }

            if (length > (uint)(stream.Length - stream.Position))
                throw new DicomParseException("طول عنصر از باقی فایل فراتر است — فایل خراب است");

            var elementValue = reader.ReadBytes((int)length);
            if (tag == PixelDataTag)
            {
                pixelData = elementValue;
            }
            else if ((tag & 0xFFFF) != 0xFFFE)
            {
                // آیتم‌های معمولی درون دنباله‌های طول‌دار نادیده گرفته می‌شوند
                tags[tag] = elementValue;
            }
        }

        if (pixelData is null)
            throw new DicomParseException("داده پیکسل (7FE0,0010) در فایل یافت نشد");

        return BuildImage(tags, pixelData);
    }

    /// <summary>قطعه‌های داده پیکسل — آیتم نخست معمولاً جدول آفست است</summary>
    private static byte[] ReadPixelFragments(BinaryReader reader)
    {
        byte[] data = Array.Empty<byte>();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = reader.ReadUInt32();
            var length = reader.ReadUInt32();

            if (tag == SequenceDelimitationTag)
                break; // پایان قطعه‌ها
            if (tag == ItemDelimitationTag)
                continue;
            if (tag != ItemTag)
                throw new DicomParseException("ساختار قطعه داده پیکسل نامعتبر است");

            if (length == 0xFFFFFFFF) continue;

            var fragment = reader.ReadBytes((int)length);
            var isFirstItem = data.Length == 0;

            // آیتم نخست در چند-فریمی جدول آفست است؛ در تک‌فریمی ممکن است خود داده باشد.
            // راهبرد: اگر قطعه دومی آمد و نخستی بسیار کوچک بود (جدول)، نخست را می‌افکنیم.
            if (isFirstItem && fragment.Length > 8)
            {
                data = fragment;
            }
            else if (isFirstItem)
            {
                data = Array.Empty<byte>(); // جدول آفست — نادیده
            }
            else
            {
                data = Concat(data, fragment);
            }
        }
        return data;
    }

    /// <summary>رد دنباله با طول نامعین — پیمایش تودرتو تا جداکننده پایان</summary>
    private static void SkipUndefinedLengthSequence(BinaryReader reader)
    {
        var depth = 1;
        while (depth > 0 && reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = reader.ReadUInt32();
            var length = reader.ReadUInt32();

            if (tag == SequenceDelimitationTag)
            {
                depth--;
                continue;
            }
            if (tag == ItemDelimitationTag)
                continue;
            if (length == 0xFFFFFFFF)
            {
                depth++; // آیتم یا دنباله تودرتو با طول نامعین
                continue;
            }
            reader.BaseStream.Position += length; // محتوای تعریف‌شده — پرش
        }
    }

    /// <summary>ساخت تصویر از تگ‌های گردآوری‌شده</summary>
    private static DicomImage BuildImage(Dictionary<uint, byte[]> tags, byte[] pixelData)
    {
        var rows = GetUShort(tags, 0x00100028) ?? 0;          // (0028,0010)
        var columns = GetUShort(tags, 0x00110028) ?? 0;       // (0028,0011)
        var bitsAllocated = GetUShort(tags, 0x01000028) ?? 8; // (0028,0100)
        var pixelRepresentation = GetUShort(tags, 0x01030028) ?? 0; // (0028,0103)
        var samplesPerPixel = GetUShort(tags, 0x00020028) ?? 1;     // (0028,0002)
        var photometric = GetString(tags, 0x00040028);              // (0028,0004)
        if (string.IsNullOrEmpty(photometric)) photometric = "MONOCHROME2";
        var planarConfig = GetUShort(tags, 0x00060028) ?? 0;        // (0028,0006)
        var rescaleSlope = GetDs(tags, 0x10530028) ?? 1.0;         // (0028,1053)
        var rescaleIntercept = GetDs(tags, 0x10520028) ?? 0.0;     // (0028,1052)
        var windowCenter = GetDs(tags, 0x10500028);                 // (0028,1050)
        var windowWidth = GetDs(tags, 0x10510028);                  // (0028,1051)

        if (rows == 0 || columns == 0)
            throw new DicomParseException("ابعاد تصویر (Rows/Columns) در فایل ناقص است");

        // ── تصاویر رنگی RGB ──
        if (samplesPerPixel == 3)
        {
            if (!photometric.Contains("RGB", StringComparison.OrdinalIgnoreCase))
                throw new DicomParseException(
                    $"تفسیر فتومتریک «{photometric}» با ۳ نمونه پیکسل پشتیبانی نمی‌شود");
            if (planarConfig != 0)
                throw new DicomParseException("چیدمان planar پشتیبانی نمی‌شود (فقط درهم‌آمیخته)");
            if (bitsAllocated != 8)
                throw new DicomParseException("RGB با بیت‌بندی غیر از ۸ پشتیبانی نمی‌شود");

            var expected = (int)(rows * (long)columns * 3);
            if (pixelData.Length < expected)
                throw new DicomParseException("حجم داده پیکسل کمتر از ابعاد تصویر است");

            return new DicomImage
            {
                Width = columns, Height = rows, BitsAllocated = 8,
                IsSigned = false, PhotometricInterpretation = photometric,
                Rgb24 = pixelData[..expected],
                WindowCenter = windowCenter, WindowWidth = windowWidth,
                RescaleSlope = rescaleSlope, RescaleIntercept = rescaleIntercept,
                Metadata = CollectMetadata(tags)
            };
        }

        if (samplesPerPixel != 1)
            throw new DicomParseException($"تعداد نمونه در پیکسل {samplesPerPixel} پشتیبانی نمی‌شود");

        // ── تصاویر خاکستری ──
        var pixelCount = rows * (long)columns;
        if (bitsAllocated == 8)
        {
            if (pixelData.Length < pixelCount)
                throw new DicomParseException("حجم داده پیکسل کمتر از ابعاد تصویر است");

            // MONOCHROME1 معکوس است — در زمان بارگذاری برعکس می‌شود
            var gray = pixelData[..(int)pixelCount];
            if (photometric.Contains("MONOCHROME1", StringComparison.OrdinalIgnoreCase))
                for (var i = 0; i < gray.Length; i++)
                    gray[i] = (byte)(255 - gray[i]);

            return new DicomImage
            {
                Width = columns, Height = rows, BitsAllocated = 8,
                IsSigned = pixelRepresentation == 1,
                PhotometricInterpretation = photometric,
                Gray8 = gray,
                WindowCenter = windowCenter, WindowWidth = windowWidth,
                RescaleSlope = rescaleSlope, RescaleIntercept = rescaleIntercept,
                Metadata = CollectMetadata(tags)
            };
        }

        if (bitsAllocated == 16)
        {
            if (pixelData.Length < pixelCount * 2)
                throw new DicomParseException("حجم داده پیکسل کمتر از ابعاد تصویر است");

            var gray16 = new ushort[pixelCount];
            Buffer.BlockCopy(pixelData, 0, gray16, 0, (int)(pixelCount * 2));

            return new DicomImage
            {
                Width = columns, Height = rows, BitsAllocated = 16,
                IsSigned = pixelRepresentation == 1,
                PhotometricInterpretation = photometric,
                Gray16 = gray16,
                WindowCenter = windowCenter, WindowWidth = windowWidth,
                RescaleSlope = rescaleSlope, RescaleIntercept = rescaleIntercept,
                Metadata = CollectMetadata(tags)
            };
        }

        throw new DicomParseException($"بیت‌بندی {bitsAllocated} پشتیبانی نمی‌شود (فقط ۸ و ۱۶)");
    }

    /// <summary>متادیتای خوانا — عنوان فارسی + مقدار برای پنجره نمایشگر</summary>
    private static List<(string, string)> CollectMetadata(Dictionary<uint, byte[]> tags)
    {
        var metadata = new List<(string, string)>();
        void Add(string name, uint tag)
        {
            if (GetString(tags, tag) is { } value && value.Length > 0)
                metadata.Add((name, value));
        }

        Add("نام بیمار", 0x00100010);      // (0010,0010)
        Add("شناسه بیمار", 0x00200010);    // (0010,0020)
        Add("تاریخ مطالعه", 0x00200008);   // (0008,0020)
        Add("مودالیته", 0x00600008);        // (0008,0060)
        Add("شرح مطالعه", 0x10300008);      // (0008,1030)
        Add("شرح سری", 0x103E0008);         // (0008,103E)
        Add("نام دستگاه", 0x10900008);      // (0008,1090)
        if (tags.ContainsKey(0x00110028) && tags.ContainsKey(0x00100028))
            metadata.Add(("ابعاد",
                $"{GetUShort(tags, 0x00110028)}×{GetUShort(tags, 0x00100028)}"));
        Add("مرکز پنجره", 0x10500028);      // (0028,1050)
        Add("عرض پنجره", 0x10510028);       // (0028,1051)
        return metadata;
    }

    /// <summary>VRهای با بلوک طول ۳۲بیتی</summary>
    private static bool IsLongVr(string vr)
        => vr is "OB" or "OW" or "OF" or "SQ" or "UT" or "UN" or "OV" or "SV" or "UV";

    /// <summary>متن ASCII با حذف فضای انتهایی و تهی‌ها — رشته تهی یعنی تگ غایب</summary>
    private static string GetString(Dictionary<uint, byte[]> tags, uint tag)
    {
        if (!tags.TryGetValue(tag, out var value)) return string.Empty;
        return Encoding.ASCII.GetString(value).TrimEnd('\0', ' ');
    }

    /// <summary>عدد بدون علامت ۱۶بیتی از تگ US</summary>
    private static ushort? GetUShort(Dictionary<uint, byte[]> tags, uint tag)
        => tags.TryGetValue(tag, out var value) && value.Length >= 2
            ? BitConverter.ToUInt16(value, 0)
            : null;

    /// <summary>عدد اعشاری از تگ IS/DS — اولین مقدار از لیست چندمقداری</summary>
    private static double? GetDs(Dictionary<uint, byte[]> tags, uint tag)
    {
        var text = GetString(tags, tag);
        if (string.IsNullOrWhiteSpace(text)) return null;
        var first = text.Split('\\')[0].Trim();
        return double.TryParse(first, out var value) ? value : null;
    }

    /// <summary>پاک‌سازی UID از فضای انتهایی</summary>
    private static string TrimUid(string uid) => uid.TrimEnd('\0', ' ').Trim();

    /// <summary>الحاق دو آرایه</summary>
    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        a.CopyTo(result, 0);
        b.CopyTo(result, a.Length);
        return result;
    }
}

/// <summary>خطای تجزیه DICOM با پیام فارسی</summary>
public sealed class DicomParseException : Exception
{
    /// <summary>سازنده با پیام فارسی</summary>
    public DicomParseException(string message) : base(message) { }
}
