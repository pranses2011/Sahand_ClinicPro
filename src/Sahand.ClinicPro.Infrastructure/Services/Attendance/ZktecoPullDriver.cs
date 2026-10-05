using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Attendance;

/// <summary>
/// درایور ZKTeco با پروتکل PULL روی TCP/4370
/// پیاده‌سازی وفادار به مرجع متن‌باز pyzk:
/// هدر بسته: [command u16][checksum u16][session u16][reply u16] (Little-Endian)
/// پوشش TCP: [0x5050][0x7282][طول u32]
/// رکورد حضور: ۴۰ بایت — [uid u16][userId 24B][status u8][epoch u32][punch u8][8B]
/// </summary>
public class ZktecoPullDriver : IAttendanceDeviceDriver
{
    /// <summary>فرمان اتصال</summary>
    private const ushort CmdConnect = 1000;

    /// <summary>فرمان قطع اتصال</summary>
    private const ushort CmdExit = 1001;

    /// <summary>فرمان خواندن زمان دستگاه</summary>
    private const ushort CmdGetTime = 201;

    /// <summary>فرمان خواندن همه رکوردهای حضور</summary>
    private const ushort CmdAttlogRrq = 13;

    /// <summary>فرمان آماده‌سازی انتقال داده — پاسخ دستگاه با حجم</summary>
    private const ushort CmdPrepareData = 1500;

    /// <summary>درخواست جریان داده</summary>
    private const ushort CmdData = 1501;

    /// <summary>رها کردن بافر دستگاه</summary>
    private const ushort CmdFreeData = 1502;

    /// <summary>پاسخ موفق</summary>
    private const ushort CmdAckOk = 2000;

    /// <summary>پاسخ ناموفق</summary>
    private const ushort CmdAckError = 2001;

    /// <summary>پاسخ حاوی داده</summary>
    private const ushort CmdAckData = 2002;

    /// <summary>سقف u16</summary>
    private const ushort UshortMax = 65535;

    /// <summary>شناسه TCP نخست</summary>
    private const ushort TcpTop1 = 0x5050;

    /// <summary>شناسه TCP دوم</summary>
    private const ushort TcpTop2 = 0x7282;

    /// <summary>طول هدر بسته — ۴×u16</summary>
    private const int HeaderSize = 8;

    /// <summary>طول پوشش TCP — ۲×u16 + u32</summary>
    private const int TcpTopSize = 8;

    /// <summary>طول هر رکورد حضور</summary>
    private const int RecordSize = 40;

    private readonly ILogger<ZktecoPullDriver> _logger;

    /// <summary>برند درایور</summary>
    public DeviceBrand Brand => DeviceBrand.ZKTeco;

    /// <summary>سازنده با لاگر</summary>
    public ZktecoPullDriver(ILogger<ZktecoPullDriver> logger) => _logger = logger;

    /// <summary>آزمون اتصال — اتصال، خواندن زمان و قطع</summary>
    public async Task<bool> TestConnectionAsync(AttendanceDevice device, CancellationToken ct = default)
    {
        try
        {
            using var client = await ConnectAsync(device, ct);
            _ = await ExchangeAsync(client, CmdGetTime, [], 1032, ct);
            await DisconnectAsync(client, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "آزمون اتصال ZKTeco {Ip} ناموفق", device.IpAddress);
            return false;
        }
    }

    /// <summary>خواندن سریال — مهر زمانی دستگاه به‌عنوان شناسه زنده بودن</summary>
    public async Task<string?> GetSerialAsync(AttendanceDevice device, CancellationToken ct = default)
    {
        try
        {
            using var client = await ConnectAsync(device, ct);
            var (command, data, _) = await ExchangeAsync(client, CmdGetTime, [], 1032, ct);
            await DisconnectAsync(client, ct);
            return command == CmdAckOk && data.Length >= 4
                ? $"epoch:{BinaryPrimitives.ReadInt32LittleEndian(data)}"
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "خواندن سریال ZKTeco {Ip} ناموفق", device.IpAddress);
            return null;
        }
    }

    /// <summary>کشیدن ضربه‌های جدید پس از زمان داده‌شده</summary>
    public async Task<IReadOnlyList<DevicePunch>> PullNewPunchesAsync(
        AttendanceDevice device, DateTime fromUtc, CancellationToken ct = default)
    {
        var punches = new List<DevicePunch>();

        using var client = await ConnectAsync(device, ct);

        // درخواست همه رکوردها — فیلتر زمانی سمت نرم‌افزار
        var (command, data, _) = await ExchangeAsync(client, CmdAttlogRrq, [], 1024 + TcpTopSize, ct);

        if (command == CmdPrepareData && data.Length >= 4)
        {
            var totalSize = BinaryPrimitives.ReadInt32LittleEndian(data);

            // درخواست جریان داده و دریافت تا تکمیل حجم اعلام‌شده
            var buffer = await ReadDataBlocksAsync(client, totalSize, ct);

            // رها کردن بافر دستگاه
            await ExchangeAsync(client, CmdFreeData, [], 1024 + TcpTopSize, ct);

            // تجزیه رکوردهای ۴۰ بایتی — در متد جدا تا Span مجاز باشد
            punches = ParseRecords(buffer, fromUtc);
        }

        await DisconnectAsync(client, ct);
        _logger.LogInformation("کشیدن {Count} ضربه از دستگاه {Ip}", punches.Count, device.IpAddress);
        return punches;
    }

    /// <summary>پیشانی فهرست کاربران — ثبت اثر انگشت روی خود دستگاه انجام می‌شود؛ اینجا فقط اتصال آزموده می‌شود</summary>
    public Task<bool> SyncUsersAsync(AttendanceDevice device, IReadOnlyList<DeviceUserMapping> mappings, CancellationToken ct = default)
        => TestConnectionAsync(device, ct);

    /// <summary>تجزیه رکوردهای ۴۰ بایتی حضور — [uid u16][userId 24B][status u8][epoch u32][punch u8][8B]</summary>
    private static List<DevicePunch> ParseRecords(byte[] buffer, DateTime fromUtc)
    {
        var punches = new List<DevicePunch>();

        for (var offset = 0; offset + RecordSize <= buffer.Length; offset += RecordSize)
        {
            var userId = Encoding.ASCII
                .GetString(buffer, offset + 2, 24)
                .TrimEnd('\0', ' ');
            if (userId.Length == 0)
                continue;

            var status = buffer[offset + 26];
            var epoch = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset + 27, 4));
            var punch = buffer[offset + 31];

            var punchTime = EpochToDeviceUtc(epoch);
            if (punchTime < fromUtc)
                continue;

            punches.Add(new DevicePunch(
                userId,
                punchTime,
                MapState(status, punch),
                VerifyMode: 1));
        }

        return punches;
    }

    // ═══════════════ لایه پروتکل ═══════════════

    /// <summary>وضعیت نشست جاری اتصال — per call بازتنظیم می‌شود</summary>
    private ushort _session;

    /// <summary>شناسه پاسخ جاری</summary>
    private ushort _reply = UshortMax - 1;

    /// <summary>اتصال TCP و دست‌دادن CONNECT</summary>
    private async Task<TcpClient> ConnectAsync(AttendanceDevice device, CancellationToken ct)
    {
        var client = new TcpClient();
        using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
            await client.ConnectAsync(device.IpAddress, device.Port, timeoutCts.Token);
        }

        client.ReceiveTimeout = 10_000;
        client.SendTimeout = 10_000;

        var (command, _, header) = await ExchangeRawAsync(
            client, CmdConnect, [], 0, UshortMax - 1, 1024, ct);

        if (command != CmdAckOk)
            throw new InvalidOperationException("دستگاه دست‌دادن اتصال را نپذیرفت");

        // نشست و پاسخ از هدر پاسخ تازه می‌شوند
        _session = header[2];
        _reply = header[3];
        return client;
    }

    /// <summary>قطع اتصال مؤدبانه</summary>
    private async Task DisconnectAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            await ExchangeRawAsync(client, CmdExit, [], _session, _reply, 8 + TcpTopSize, ct);
        }
        catch
        {
            // قطع اتصال بهترین تلاش است — خطا مهم نیست
        }
    }

    /// <summary>تبادل کامل با نشست جاری</summary>
    private async Task<(ushort Command, byte[] Data, ushort[] Header)> ExchangeAsync(
        TcpClient client, ushort command, byte[] payload, int responseSize, CancellationToken ct)
    {
        var result = await ExchangeRawAsync(client, command, payload, _session, _reply, responseSize, ct);
        _session = result.Header[2];
        _reply = result.Header[3];
        return result;
    }

    /// <summary>ارسال بسته و دریافت پاسخ — ساخت هدر، چک‌سام و پوشش TCP مطابق zkemsdk</summary>
    private static async Task<(ushort Command, byte[] Data, ushort[] Header)> ExchangeRawAsync(
        TcpClient client, ushort command, byte[] payload, ushort session, ushort reply, int responseSize, CancellationToken ct)
    {
        // بسته خام برای چک‌سام: command + 0 + session + reply + data
        var forChecksum = new byte[HeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(forChecksum.AsSpan(0, 2), command);
        BinaryPrimitives.WriteUInt16LittleEndian(forChecksum.AsSpan(4, 2), session);
        BinaryPrimitives.WriteUInt16LittleEndian(forChecksum.AsSpan(6, 2), reply);
        payload.CopyTo(forChecksum, HeaderSize);

        var checksum = ComputeChecksum(forChecksum);

        // reply یک واحد جلو می‌رود — مطابق مرجع
        var nextReply = (ushort)(reply >= UshortMax ? 1 : reply + 1);

        var packet = new byte[HeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), command);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), checksum);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(4, 2), session);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(6, 2), nextReply);
        payload.CopyTo(packet, HeaderSize);

        // پوشش TCP
        var wire = new byte[TcpTopSize + packet.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(0, 2), TcpTop1);
        BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(2, 2), TcpTop2);
        BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(4, 4), (uint)packet.Length);
        packet.CopyTo(wire, TcpTopSize);

        var stream = client.GetStream();
        await stream.WriteAsync(wire, ct);
        await stream.FlushAsync(ct);

        return await ReadPacketAsync(stream, ct);
    }

    /// <summary>خواندن یک بسته پاسخ — پوشش TCP + هدر + داده</summary>
    private static async Task<(ushort Command, byte[] Data, ushort[] Header)> ReadPacketAsync(NetworkStream stream, CancellationToken ct)
    {
        var top = await ReadExactlyAsync(stream, TcpTopSize, ct);
        if (BinaryPrimitives.ReadUInt16LittleEndian(top) != TcpTop1)
            throw new InvalidOperationException("بسته پاسخ دستگاه نامعتبر است (شناسه TCP)");

        var length = BinaryPrimitives.ReadInt32LittleEndian(top.AsSpan(4, 4));
        if (length is < HeaderSize or > 16 * 1024 * 1024)
            throw new InvalidOperationException("طول بسته پاسخ خارج از دامنه مجاز است");

        var rest = await ReadExactlyAsync(stream, length, ct);
        var header = new ushort[4];
        for (var i = 0; i < 4; i++)
            header[i] = BinaryPrimitives.ReadUInt16LittleEndian(rest.AsSpan(i * 2, 2));

        var data = rest[HeaderSize..];
        return (header[0], data, header);
    }

    /// <summary>خواندن جریان داده تا حجم اعلام‌شده — بسته‌های ACK_DATA با تصدیق آزاد</summary>
    private async Task<byte[]> ReadDataBlocksAsync(TcpClient client, int totalSize, CancellationToken ct)
    {
        var stream = client.GetStream();
        var result = new MemoryStream(Math.Max(totalSize, 0));

        // نخست درخواست جریان
        await WriteSimpleAsync(stream, CmdData, _session, _reply, ct);

        while (result.Length < totalSize)
        {
            var top = await ReadExactlyAsync(stream, TcpTopSize, ct);
            var length = BinaryPrimitives.ReadInt32LittleEndian(top.AsSpan(4, 4));
            var rest = await ReadExactlyAsync(stream, length, ct);

            var command = BinaryPrimitives.ReadUInt16LittleEndian(rest);
            _reply = BinaryPrimitives.ReadUInt16LittleEndian(rest.AsSpan(6, 2));

            if (command is CmdAckOk or CmdAckError)
                break;

            if (rest.Length > HeaderSize)
                await result.WriteAsync(rest.AsMemory(HeaderSize), ct);

            // هر بسته داده با FREE_DATA تصدیق می‌شود — مطابق مرجع
            if (result.Length < totalSize)
                await WriteSimpleAsync(stream, CmdFreeData, _session, _reply, ct);
        }

        return result.ToArray();
    }

    /// <summary>نوشتن بسته ساده بدون data</summary>
    private static async Task WriteSimpleAsync(NetworkStream stream, ushort command, ushort session, ushort reply, CancellationToken ct)
    {
        // چک‌سام با reply فعلی محاسبه و سپس reply یک واحد جلو می‌رود — مطابق مرجع
        var header = new byte[HeaderSize];
        BinaryPrimitives.WriteUInt16LittleEndian(header, command);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(4, 2), session);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(6, 2), reply);
        var checksum = ComputeChecksum(header);

        var packet = new byte[HeaderSize];
        BinaryPrimitives.WriteUInt16LittleEndian(packet, command);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), checksum);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(4, 2), session);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(6, 2), (ushort)(reply >= UshortMax ? 1 : reply + 1));

        var wire = new byte[TcpTopSize + HeaderSize];
        BinaryPrimitives.WriteUInt16LittleEndian(wire, TcpTop1);
        BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(2, 2), TcpTop2);
        BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(4, 4), HeaderSize);
        packet.CopyTo(wire, TcpTopSize);

        await stream.WriteAsync(wire, ct);
        await stream.FlushAsync(ct);
    }

    /// <summary>چک‌سام مرجع zkemsdk — جمع u16ها با wrap-around</summary>
    private static ushort ComputeChecksum(ReadOnlySpan<byte> packet)
    {
        uint checksum = 0;
        for (var i = 0; i + 1 < packet.Length; i += 2)
        {
            checksum += (uint)(packet[i] | (packet[i + 1] << 8));
            if (checksum > UshortMax)
                checksum -= UshortMax;
        }

        if ((packet.Length & 1) == 1)
        {
            checksum += packet[^1];
            if (checksum > UshortMax)
                checksum -= UshortMax;
        }

        return (ushort)checksum;
    }

    /// <summary>خواندن دقیقاً n بایت از جریان</summary>
    private static async Task<byte[]> ReadExactlyAsync(NetworkStream stream, int count, CancellationToken ct)
    {
        var buffer = new byte[count];
        var read = 0;
        while (read < count)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(read, count - read), ct);
            if (n == 0)
                throw new IOException("اتصال دستگاه پیش از تکمیل بسته قطع شد");
            read += n;
        }
        return buffer;
    }

    /// <summary>دوره epoch دستگاه (زمان محلی) به UTC — منطقه زمانی ایران</summary>
    private static DateTime EpochToDeviceUtc(uint epoch)
    {
        var assumedLocal = DateTime.UnixEpoch.AddSeconds(epoch);
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(assumedLocal, DateTimeKind.Unspecified), IranTimeZone);
    }

    /// <summary>منطقه زمانی ایران — یک‌بار یافته می‌شود</summary>
    private static readonly TimeZoneInfo IranTimeZone = CreateIranTz();

    private static TimeZoneInfo CreateIranTz()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
    }

    /// <summary>نگاشت وضعیت/punch خام ZKTeco به نوع ضربه سامانه</summary>
    private static PunchState MapState(byte status, byte punch)
    {
        // status استاندارد ZKTeco: 0=ورود 1=خروج 2=خروج استراحت 3=بازگشت 4/5=اضافه‌کاری
        return status switch
        {
            0 => PunchState.CheckIn,
            1 => PunchState.CheckOut,
            2 => PunchState.BreakOut,
            3 => PunchState.BreakIn,
            4 => PunchState.OvertimeIn,
            5 => PunchState.OvertimeOut,
            255 => punch switch
            {
                0 => PunchState.CheckIn,
                1 => PunchState.CheckOut,
                4 => PunchState.OvertimeIn,
                5 => PunchState.OvertimeOut,
                _ => PunchState.Undefined,
            },
            _ => PunchState.Undefined,
        };
    }
}
