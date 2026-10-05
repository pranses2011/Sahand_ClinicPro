using System.Text;
using FluentAssertions;
using Sahand.ClinicPro.Shared.Dicom;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests.Dicom;

/// <summary>
/// تست‌های تجزیه‌گر DICOM — گام R4
/// فایل‌های مصنوعی استاندارد در حافظه ساخته و تجزیه می‌شوند:
/// Explicit VR LE ۱۶بیتی، Implicit VR LE ۸بیتی و RGB
/// </summary>
public class DicomFileReaderTests
{
    /// <summary>سازنده فایل DICOM مصنوعی</summary>
    private static byte[] BuildDicom(
        bool explicitVr, ushort rows, ushort columns, ushort bitsAllocated,
        ushort samplesPerPixel, string photometric, byte[] pixels,
        string patientName = "TEST^PATIENT", double? windowCenter = null, double? windowWidth = null,
        double? rescaleSlope = null, double? rescaleIntercept = null)
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);

        // ── پیش‌درآمد و جادو ──
        writer.Write(new byte[128]);
        writer.Write(Encoding.ASCII.GetBytes("DICM"));

        // ── گروه فراداده — همیشه Explicit LE ──
        void MetaElement(ushort element, string vr, byte[] value)
        {
            writer.Write((ushort)0x0002);
            writer.Write(element);
            writer.Write(Encoding.ASCII.GetBytes(vr));
            if (DicomVrLong(vr)) { writer.Write((ushort)0); writer.Write((uint)value.Length); }
            else writer.Write((ushort)value.Length);
            writer.Write(value);
        }

        var transferSyntax = explicitVr
            ? "1.2.840.10008.1.2.1\0"
            : "1.2.840.10008.1.2\0";
        MetaElement(0x0010, "UI", Encoding.ASCII.GetBytes(transferSyntax));
        writer.Flush();
        var metaEnd = ms.Position;

        // ── مجموعه‌داده اصلی ──
        void Element(ushort group, ushort element, string vr, byte[] value)
        {
            writer.Write(group);
            writer.Write(element);
            if (explicitVr)
            {
                writer.Write(Encoding.ASCII.GetBytes(vr));
                if (DicomVrLong(vr)) { writer.Write((ushort)0); writer.Write((uint)value.Length); }
                else writer.Write((ushort)value.Length);
            }
            else
            {
                writer.Write((uint)value.Length);
            }
            writer.Write(value);
        }

        Element(0x0010, 0x0010, "PN", Encoding.ASCII.GetBytes(patientName));
        Element(0x0008, 0x0060, "CS", Encoding.ASCII.GetBytes("CT"));
        Element(0x0028, 0x0002, "US", BitConverter.GetBytes(samplesPerPixel));
        Element(0x0028, 0x0004, "CS", Encoding.ASCII.GetBytes(photometric));
        Element(0x0028, 0x0010, "US", BitConverter.GetBytes(rows));
        Element(0x0028, 0x0011, "US", BitConverter.GetBytes(columns));
        Element(0x0028, 0x0100, "US", BitConverter.GetBytes(bitsAllocated));
        if (windowCenter is not null)
            Element(0x0028, 0x1050, "DS", Encoding.ASCII.GetBytes(windowCenter.ToString()!));
        if (windowWidth is not null)
            Element(0x0028, 0x1051, "DS", Encoding.ASCII.GetBytes(windowWidth.ToString()!));
        if (rescaleSlope is not null)
            Element(0x0028, 0x1053, "DS", Encoding.ASCII.GetBytes(rescaleSlope.ToString()!));
        if (rescaleIntercept is not null)
            Element(0x0028, 0x1052, "DS", Encoding.ASCII.GetBytes(rescaleIntercept.ToString()!));

        // داده پیکسل — OW برای ۱۶بیتی و OB برای ۸بیتی
        Element(0x7FE0, 0x0010, bitsAllocated == 16 ? "OW" : "OB", pixels);
        writer.Flush();

        return ms.ToArray();

        // VR با طول بلوک ۳۲بیتی؟
        static bool DicomVrLong(string vr)
            => vr is "OB" or "OW" or "OF" or "SQ" or "UT" or "UN";
    }

    /// <summary>تصویر خاکستری ۱۶بیتی Explicit VR — مقادیر پیکسل صحیح</summary>
    [Fact]
    public void Load_ExplicitVrMono16_ReadsPixelsAndDimensions()
    {
        // گرادیان ۰..۱۵ روی شبکه ۴×۴
        var pixels = new ushort[16];
        for (var i = 0; i < 16; i++) pixels[i] = (ushort)(i * 1000);
        var raw = new byte[32];
        Buffer.BlockCopy(pixels, 0, raw, 0, 32);

        var file = BuildDicom(explicitVr: true, rows: 4, columns: 4, bitsAllocated: 16,
            samplesPerPixel: 1, photometric: "MONOCHROME2", raw);

        var image = DicomFileReader.Load(new MemoryStream(file));

        image.Width.Should().Be(4);
        image.Height.Should().Be(4);
        image.BitsAllocated.Should().Be(16);
        image.Gray16.Should().NotBeNull().And.HaveCount(16);
        image.Gray16![5].Should().Be((ushort)5000);
        image.Gray16![15].Should().Be((ushort)15000);
        image.Metadata.Should().Contain(m => m.Name == "نام بیمار" && m.Value == "TEST^PATIENT");
        image.Metadata.Should().Contain(m => m.Name == "مودالیته" && m.Value == "CT");
    }

    /// <summary>تصویر خاکستری ۸بیتی Implicit VR — بدون تگ VR</summary>
    [Fact]
    public void Load_ImplicitVrMono8_ReadsPixels()
    {
        var pixels = new byte[] { 0, 32, 64, 96, 128, 160, 192, 224, 255 };
        var file = BuildDicom(explicitVr: false, rows: 3, columns: 3, bitsAllocated: 8,
            samplesPerPixel: 1, photometric: "MONOCHROME2", pixels);

        var image = DicomFileReader.Load(new MemoryStream(file));

        image.BitsAllocated.Should().Be(8);
        image.Gray8.Should().NotBeNull().And.HaveCount(9);
        image.Gray8![4].Should().Be(128);
    }

    /// <summary>تصویر RGB درهم‌آمیخته ۸بیتی</summary>
    [Fact]
    public void Load_Rgb24_ReadsColorPixels()
    {
        // ۲×۱ پیکسل: قرمز + آبی
        var pixels = new byte[] { 255, 0, 0, 0, 0, 255 };
        var file = BuildDicom(explicitVr: true, rows: 1, columns: 2, bitsAllocated: 8,
            samplesPerPixel: 3, photometric: "RGB", pixels);

        var image = DicomFileReader.Load(new MemoryStream(file));

        image.Rgb24.Should().NotBeNull().And.HaveCount(6);
        image.Rgb24![0].Should().Be(255);
        image.Rgb24![5].Should().Be(255);
    }

    /// <summary>پنجره/سطح — بازه بیرون پنجره اشباع و درون پنجره خطی</summary>
    [Fact]
    public void RenderGray8_WindowLevel_MapsLinearly()
    {
        var pixels = new ushort[] { 0, 500, 1000, 1500, 2000 };
        var raw = new byte[10];
        Buffer.BlockCopy(pixels, 0, raw, 0, 10);
        var file = BuildDicom(true, 5, 1, 16, 1, "MONOCHROME2", raw);
        var image = DicomFileReader.Load(new MemoryStream(file));

        // پنجره ۱۰۰۰..۲۰۰۰ → مرکز ۱۵۰۰ عرض ۱۰۰۰
        var rendered = image.RenderGray8(1500, 1000);

        rendered[0].Should().Be(0);   // زیر پنجره → سیاه
        rendered[1].Should().Be(0);
        rendered[3].Should().Be(128); // روی مرکز → میانه (گرد)
        rendered[4].Should().Be(255); // بالای پنجره → سفید
    }

    /// <summary>Rescale واقعی CT — شیب ۱ و عرض −۱۰۲۴ (خطای کلاسیک CT)</summary>
    [Fact]
    public void RescaleIntercept_AppliedInRawRange()
    {
        var pixels = new ushort[] { 0, 1024 };
        var raw = new byte[4];
        Buffer.BlockCopy(pixels, 0, raw, 0, 4);
        var file = BuildDicom(true, 2, 1, 16, 1, "MONOCHROME2", raw,
            rescaleSlope: 1, rescaleIntercept: -1024);
        var image = DicomFileReader.Load(new MemoryStream(file));

        var (min, max) = image.RawRange();
        min.Should().Be(-1024);
        max.Should().Be(0);
    }

    /// <summary>پنجره از خود فایل خوانده می‌شود</summary>
    [Fact]
    public void WindowFrom_FileTags_Parsed()
    {
        var raw = new byte[4];
        Buffer.BlockCopy(new ushort[] { 100, 200 }, 0, raw, 0, 4);
        var file = BuildDicom(true, 2, 1, 16, 1, "MONOCHROME2", raw,
            windowCenter: 40, windowWidth: 80);
        var image = DicomFileReader.Load(new MemoryStream(file));

        image.WindowCenter.Should().Be(40);
        image.WindowWidth.Should().Be(80);
    }

    /// <summary>تشخیص سریع DICOM از جادو</summary>
    [Fact]
    public void LooksLikeDicom_DetectsMagic()
    {
        var pixels = new byte[] { 1, 2, 3, 4 };
        var file = BuildDicom(true, 2, 2, 8, 1, "MONOCHROME2", pixels);

        DicomFileReader.LooksLikeDicom(new MemoryStream(file)).Should().BeTrue();
        DicomFileReader.LooksLikeDicom(new MemoryStream(new byte[] { 1, 2, 3 })).Should().BeFalse();
    }

    /// <summary>فایل بدون جادو باید خطای فارسی بدهد</summary>
    [Fact]
    public void Load_NonDicomFile_ThrowsPersianError()
    {
        var act = () => DicomFileReader.Load(new MemoryStream(new byte[300]));
        act.Should().Throw<DicomParseException>()
            .WithMessage("*DICM*");
    }

    /// <summary>MONOCHROME1 باید در پیکسل ۸بیتی معکوس شود</summary>
    [Fact]
    public void Monochrome1_IsInvertedOnLoad()
    {
        var pixels = new byte[] { 10, 200 };
        var file = BuildDicom(true, 1, 2, 8, 1, "MONOCHROME1", pixels);
        var image = DicomFileReader.Load(new MemoryStream(file));

        image.Gray8![0].Should().Be(245); // ۲۵۵−۱۰
        image.Gray8![1].Should().Be(55);  // ۲۵۵−۲۰۰
    }
}
