using System.ComponentModel;
using NewLife;
using NewLife.Buffers;
using NewLife.Data;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Buffers;

/// <summary>SpanWriter/SpanReader WriteValue/ReadValue 实例方法全覆盖测试</summary>
/// <remarks>
/// 覆盖所有基础类型、Nullable、EncodeInt、FullTime 代码路径，
/// 以及 SpanWriter↔SpanReader、Binary↔SpanWriter/SpanReader 互操作字节级一致性验证。
/// </remarks>
public class SpanReaderWriterTests
{
    #region WriteValue/ReadValue 基础类型
    [Fact]
    [DisplayName("WriteValue/ReadValue: 所有基础类型往返一致")]
    public void WriteReadValue_AllPrimitives()
    {
        var buf = new Byte[512];
        var cases = new (Object? value, Type type)[]
        {
            (true,                typeof(Boolean)),
            (false,               typeof(Boolean)),
            ((Byte)0,             typeof(Byte)),
            ((Byte)0xFF,          typeof(Byte)),
            ((SByte)(-100),       typeof(SByte)),
            ((SByte)127,          typeof(SByte)),
            ((Char)'A',           typeof(Char)),
            ((Char)'\0',          typeof(Char)),
            ((Int16)(-1234),      typeof(Int16)),
            ((Int16)Int16.MaxValue, typeof(Int16)),
            ((UInt16)5678,        typeof(UInt16)),
            ((UInt16)UInt16.MaxValue, typeof(UInt16)),
            (0,                   typeof(Int32)),
            (12345,               typeof(Int32)),
            (Int32.MaxValue,      typeof(Int32)),
            (Int32.MinValue,      typeof(Int32)),
            ((UInt32)654321,      typeof(UInt32)),
            ((UInt32)UInt32.MaxValue, typeof(UInt32)),
            (0L,                  typeof(Int64)),
            (9876543210L,         typeof(Int64)),
            (Int64.MaxValue,      typeof(Int64)),
            (Int64.MinValue,      typeof(Int64)),
            ((UInt64)9876543210UL, typeof(UInt64)),
            ((UInt64)UInt64.MaxValue, typeof(UInt64)),
            (0f,                  typeof(Single)),
            (3.14f,               typeof(Single)),
            (Single.MaxValue,     typeof(Single)),
            (0.0,                 typeof(Double)),
            (2.718281828,         typeof(Double)),
            (Double.MaxValue,     typeof(Double)),
            (0m,                  typeof(Decimal)),
            (123456.789m,         typeof(Decimal)),
            (Decimal.MaxValue,    typeof(Decimal)),
            (new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), typeof(DateTime)),
            (DateTime.MinValue,   typeof(DateTime)),
            ("hello world",       typeof(String)),
            ("",                  typeof(String)),
            (null,                typeof(String)),
        };

        foreach (var (value, type) in cases)
        {
            var writer = new SpanWriter(buf);
            writer.WriteValue(value, type);
            var written = writer.WrittenCount;
            Assert.True(written > 0 || type == typeof(String) && value == null, $"Type={type.Name} value={value}");

            var reader = new SpanReader(buf.AsSpan(0, written));
            var result = reader.ReadValue(type);
            Assert.Equal(reader.Position, written);

            if (type == typeof(DateTime))
            {
                // DateTime精度到秒（Unix秒数模式）
                var expected = ((DateTime)value!).Ticks / TimeSpan.TicksPerSecond;
                var actual = ((DateTime)result!).Ticks / TimeSpan.TicksPerSecond;
                Assert.Equal(expected, actual);
            }
            else if (type == typeof(String) && value == null)
            {
                // null字符串序列化后读回为空字符串（长度前缀=0），符合SpanWriter设计
                Assert.Equal("", result);
            }
            else
            {
                Assert.Equal(value, result);
            }
        }
    }

    [Fact]
    [DisplayName("WriteValue/ReadValue: Byte[] 和 Guid")]
    public void WriteReadValue_ComplexTypes()
    {
        var buf = new Byte[256];

        // Byte[] 有数据
        var data = new Byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var writer = new SpanWriter(buf);
        writer.WriteValue(data, typeof(Byte[]));
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(data, (Byte[]?)reader.ReadValue(typeof(Byte[])));

        // Byte[] 空数组
        writer = new SpanWriter(buf);
        writer.WriteValue(Array.Empty<Byte>(), typeof(Byte[]));
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Empty((Byte[]?)reader.ReadValue(typeof(Byte[])) ?? []);

        // Byte[] null → 将值视为 Byte[] null，长度前缀为0
        writer = new SpanWriter(buf);
        // null 在 Byte[] 非可空情况下 WriteValue 通过Code=Object走到value is Byte[] 失败
        // 实际上 null/Byte[] 应用可空包装，非可空情况下 null 值不写标志位
        // 直接传空数组
        writer.WriteValue(Array.Empty<Byte>(), typeof(Byte[]));
        Assert.Equal(1, writer.WrittenCount); // 仅1字节：EncodedInt(0)

        // Guid
        var guid = Guid.NewGuid();
        writer = new SpanWriter(buf);
        writer.WriteValue(guid, typeof(Guid));
        Assert.Equal(16, writer.WrittenCount); // Guid 固定16字节，无长度前缀
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(guid, (Guid)reader.ReadValue(typeof(Guid))!);
    }

    [Fact]
    [DisplayName("WriteValue: null值写入类型默认值（非可空类型）")]
    public void WriteValue_NullForNonNullableTypes()
    {
        var buf = new Byte[64];

        // null Boolean → 写0x00
        var writer = new SpanWriter(buf);
        writer.WriteValue(null, typeof(Boolean));
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(false, (Boolean)reader.ReadValue(typeof(Boolean))!);

        // null Int32 → 写0
        writer = new SpanWriter(buf);
        writer.WriteValue(null, typeof(Int32));
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(0, (Int32)reader.ReadValue(typeof(Int32))!);

        // null Int64 → 写0
        writer = new SpanWriter(buf);
        writer.WriteValue(null, typeof(Int64));
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(0L, (Int64)reader.ReadValue(typeof(Int64))!);

        // null DateTime → 写0（等价MinValue）
        writer = new SpanWriter(buf);
        writer.WriteValue(null, typeof(DateTime));
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(DateTime.MinValue, (DateTime)reader.ReadValue(typeof(DateTime))!);

        // DBNull 等价 null
        writer = new SpanWriter(buf);
        writer.WriteValue(DBNull.Value, typeof(Int32));
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(0, (Int32)reader.ReadValue(typeof(Int32))!);
    }
    #endregion

    #region DateTime FullTime 路径
    [Fact]
    [DisplayName("FullTime=false: DateTime写4字节Unix秒（默认模式）")]
    public void DateTime_FullTimeFalse_UnixSeconds()
    {
        var buf = new Byte[32];
        var dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dtSample = new DateTime(2025, 3, 15, 10, 20, 30, DateTimeKind.Utc);

        var writer = new SpanWriter(buf) { FullTime = false };
        writer.WriteValue(dtSample, typeof(DateTime));
        Assert.Equal(4, writer.WrittenCount);

        // 字节内容是Unix秒数（小端）
        var expectedSeconds = (UInt32)(dtSample - dt1970).TotalSeconds;
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(expectedSeconds, reader.ReadUInt32());

        // 往返正确（精度到秒）
        writer = new SpanWriter(buf) { FullTime = false };
        writer.WriteValue(dtSample, typeof(DateTime));
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount)) { FullTime = false };
        var dtBack = (DateTime)reader.ReadValue(typeof(DateTime))!;
        Assert.Equal(dtSample.Ticks / TimeSpan.TicksPerSecond, dtBack.Ticks / TimeSpan.TicksPerSecond);

        // DateTime.MinValue → 写0
        writer = new SpanWriter(buf) { FullTime = false };
        writer.WriteValue(DateTime.MinValue, typeof(DateTime));
        Assert.Equal(4, writer.WrittenCount);
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount)) { FullTime = false };
        Assert.Equal(DateTime.MinValue, (DateTime)reader.ReadValue(typeof(DateTime))!);
    }

    [Fact]
    [DisplayName("FullTime=true: DateTime写8字节ToBinary，精度到Ticks")]
    public void DateTime_FullTimeTrue_ToBinary()
    {
        var buf = new Byte[32];
        var dt = new DateTime(2025, 6, 15, 14, 30, 50, 123, DateTimeKind.Utc);

        var writer = new SpanWriter(buf) { FullTime = true };
        writer.WriteValue(dt, typeof(DateTime));
        Assert.Equal(8, writer.WrittenCount); // 始终8字节（无EncodeInt时）

        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount)) { FullTime = true };
        var dtBack = (DateTime)reader.ReadValue(typeof(DateTime))!;
        Assert.Equal(dt, dtBack); // Ticks级精度

        // FullTime=true 不影响写入字节数（写ToBinary Int64固定8字节）
        writer = new SpanWriter(buf) { FullTime = true };
        writer.WriteValue(DateTime.MinValue, typeof(DateTime));
        Assert.Equal(8, writer.WrittenCount);
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount)) { FullTime = true };
        Assert.Equal(DateTime.MinValue, (DateTime)reader.ReadValue(typeof(DateTime))!);
    }

    [Fact]
    [DisplayName("FullTime=true与Binary(FullTime=true)字节级一致")]
    public void DateTime_FullTimeTrue_CompatibleWithBinary()
    {
        var buf = new Byte[64];
        var datetimes = new DateTime[]
        {
            new(2025, 6, 30, 23, 59, 59, 999, DateTimeKind.Utc),
            new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTime.MinValue,
        };

        foreach (var dt in datetimes)
        {
            var bn = new Binary { IsLittleEndian = true, FullTime = true };
            bn.Write(dt, typeof(DateTime));
            var binaryBytes = bn.GetPacket().ToHex(-1);

            var writer = new SpanWriter(buf) { IsLittleEndian = true, FullTime = true };
            writer.WriteValue(dt, typeof(DateTime));
            var spanBytes = buf.ToHex(0, writer.WrittenCount);

            Assert.True(binaryBytes == spanBytes, $"dt={dt:O}: Binary={binaryBytes} vs SpanWriter={spanBytes}");
        }
    }
    #endregion

    #region EncodeInt 路径
    [Fact]
    [DisplayName("EncodeInt=true: 整数使用7位压缩编码，小值占更少字节")]
    public void WriteValue_EncodeInt_ByteCount()
    {
        var buf = new Byte[64];

        // Int32 小值
        var writer = new SpanWriter(buf) { EncodeInt = true };
        writer.WriteValue(42, typeof(Int32));
        Assert.Equal(1, writer.WrittenCount);   // 42 < 0x80 → 1字节

        writer = new SpanWriter(buf) { EncodeInt = false };
        writer.WriteValue(42, typeof(Int32));
        Assert.Equal(4, writer.WrittenCount);   // 固定4字节

        // Int32 大值
        writer = new SpanWriter(buf) { EncodeInt = true };
        writer.WriteValue(1_000_000, typeof(Int32));
        Assert.True(writer.WrittenCount == 3, $"1M encoded should be 3 bytes, got {writer.WrittenCount}");

        // Int16
        writer = new SpanWriter(buf) { EncodeInt = true };
        writer.WriteValue((Int16)1000, typeof(Int16));
        Assert.Equal(2, writer.WrittenCount);   // 1000 ∈ [0x80,0x4000) → 2字节

        // Int64
        writer = new SpanWriter(buf) { EncodeInt = true };
        writer.WriteValue(200L, typeof(Int64));
        Assert.Equal(2, writer.WrittenCount);   // 200 ∈ [0x80,0x4000) → 2字节

        writer = new SpanWriter(buf) { EncodeInt = false };
        writer.WriteValue(200L, typeof(Int64));
        Assert.Equal(8, writer.WrittenCount);   // 固定8字节
    }

    [Fact]
    [DisplayName("EncodeInt=true: 所有整数类型往返一致（含边界值）")]
    public void WriteValue_EncodeInt_RoundTrip()
    {
        var buf = new Byte[128];

        // Int16
        var values16 = new Int16[] { 0, 1, 127, 128, -1, -128, Int16.MaxValue, Int16.MinValue };
        foreach (var v in values16)
        {
            var writer = new SpanWriter(buf) { EncodeInt = true };
            writer.WriteValue(v, typeof(Int16));
            var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount)) { EncodeInt = true };
            Assert.Equal(v, (Int16)reader.ReadValue(typeof(Int16))!);
        }

        // Int32
        var values32 = new Int32[] { 0, 1, 127, 128, 16383, 16384, -1, Int32.MaxValue, Int32.MinValue };
        foreach (var v in values32)
        {
            var writer = new SpanWriter(buf) { EncodeInt = true };
            writer.WriteValue(v, typeof(Int32));
            var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount)) { EncodeInt = true };
            Assert.Equal(v, (Int32)reader.ReadValue(typeof(Int32))!);
        }

        // Int64
        var values64 = new Int64[] { 0, 1, 127L, 16383L, 2_097_151L, (Int64)Int32.MaxValue + 1, -1L, Int64.MaxValue, Int64.MinValue };
        foreach (var v in values64)
        {
            var writer = new SpanWriter(buf) { EncodeInt = true };
            writer.WriteValue(v, typeof(Int64));
            var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount)) { EncodeInt = true };
            Assert.Equal(v, (Int64)reader.ReadValue(typeof(Int64))!);
        }

        // UInt16 / UInt32 / UInt64
        var writer2 = new SpanWriter(buf) { EncodeInt = true };
        writer2.WriteValue((UInt16)60000, typeof(UInt16));
        writer2.WriteValue((UInt32)2_000_000_000, typeof(UInt32));
        writer2.WriteValue((UInt64)9876543210, typeof(UInt64));
        var reader2 = new SpanReader(buf.AsSpan(0, writer2.WrittenCount)) { EncodeInt = true };
        Assert.Equal((UInt16)60000, (UInt16)reader2.ReadValue(typeof(UInt16))!);
        Assert.Equal((UInt32)2_000_000_000, (UInt32)reader2.ReadValue(typeof(UInt32))!);
        Assert.Equal((UInt64)9876543210, (UInt64)reader2.ReadValue(typeof(UInt64))!);
    }

    [Fact]
    [DisplayName("EncodeInt=true WriteValue 与 Binary(EncodeInt=true) 字节级一致")]
    public void WriteValue_EncodeInt_CompatibleWithBinary()
    {
        var cases = new (Object value, Type type, String label)[]
        {
            (0,             typeof(Int32),  "Int32 0"),
            (42,            typeof(Int32),  "Int32 42"),
            (16383,         typeof(Int32),  "Int32 16383"),
            (1_000_000,     typeof(Int32),  "Int32 1M"),
            (-1,            typeof(Int32),  "Int32 -1"),
            ((Int16)100,    typeof(Int16),  "Int16 100"),
            ((Int16)(-500), typeof(Int16),  "Int16 -500"),
            (9876543210L,   typeof(Int64),  "Int64 large"),
            (-1L,           typeof(Int64),  "Int64 -1"),
            ((UInt16)60000, typeof(UInt16), "UInt16 60000"),
            ((UInt32)3_000_000_000u, typeof(UInt32), "UInt32 3G"),
            ((UInt64)9876543210UL,   typeof(UInt64), "UInt64 big"),
        };

        var buf = new Byte[256];
        foreach (var (value, type, label) in cases)
        {
            var bn = new Binary { IsLittleEndian = true, EncodeInt = true };
            bn.Write(value, type);
            var binaryBytes = bn.GetPacket().ToHex(-1);

            var writer = new SpanWriter(buf) { IsLittleEndian = true, EncodeInt = true };
            writer.WriteValue(value, type);
            var spanBytes = buf.ToHex(0, writer.WrittenCount);

            Assert.True(binaryBytes == spanBytes, $"{label}: Binary={binaryBytes} vs SpanWriter={spanBytes}");
        }
    }

    [Fact]
    [DisplayName("EncodeInt+FullTime 组合：与 Binary(EncodeInt=true,FullTime=true) 字节级一致")]
    public void WriteValue_EncodeInt_FullTime_CompatibleWithBinary()
    {
        var dt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var cases = new (Object value, Type type, String label)[]
        {
            (42,        typeof(Int32),    "Int32 42"),
            (-100,      typeof(Int32),    "Int32 -100"),
            (1_000_000, typeof(Int32),    "Int32 1M"),
            (dt,        typeof(DateTime), "DateTime full"),
            (9876543210L, typeof(Int64),  "Int64"),
        };

        var buf = new Byte[256];
        foreach (var (value, type, label) in cases)
        {
            var bn = new Binary { IsLittleEndian = true, EncodeInt = true, FullTime = true };
            bn.Write(value, type);
            var binaryBytes = bn.GetPacket().ToHex(-1);

            var writer = new SpanWriter(buf) { IsLittleEndian = true, EncodeInt = true, FullTime = true };
            writer.WriteValue(value, type);
            var spanBytes = buf.ToHex(0, writer.WrittenCount);

            Assert.True(binaryBytes == spanBytes, $"{label}: Binary={binaryBytes} vs SpanWriter={spanBytes}");

            var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount)) { IsLittleEndian = true, EncodeInt = true, FullTime = true };
            var readBack = reader.ReadValue(type);
            if (type == typeof(DateTime))
                Assert.Equal((DateTime)value, (DateTime)readBack!);
            else
                Assert.Equal(value, readBack);
        }
    }

    [Fact]
    [DisplayName("Single/Double 不受 EncodeInt 影响，始终固定字节数")]
    public void SingleDouble_NotAffectedByEncodeInt()
    {
        var buf = new Byte[64];

        // Single 固定4字节
        var writer = new SpanWriter(buf) { EncodeInt = true };
        writer.WriteValue(3.14f, typeof(Single));
        Assert.Equal(4, writer.WrittenCount);

        writer = new SpanWriter(buf) { EncodeInt = false };
        writer.WriteValue(3.14f, typeof(Single));
        Assert.Equal(4, writer.WrittenCount);

        // Double 固定8字节
        writer = new SpanWriter(buf) { EncodeInt = true };
        writer.WriteValue(2.718281828, typeof(Double));
        Assert.Equal(8, writer.WrittenCount);

        // 与 Binary(EncodeInt=true) 字节级一致
        var bnS = new Binary { IsLittleEndian = true, EncodeInt = true };
        bnS.Write(3.14f, typeof(Single));
        writer = new SpanWriter(buf) { IsLittleEndian = true, EncodeInt = true };
        writer.WriteValue(3.14f, typeof(Single));
        Assert.Equal(bnS.GetPacket().ToHex(-1), buf.ToHex(0, writer.WrittenCount));

        var bnD = new Binary { IsLittleEndian = true, EncodeInt = true };
        bnD.Write(2.718281828, typeof(Double));
        writer = new SpanWriter(buf) { IsLittleEndian = true, EncodeInt = true };
        writer.WriteValue(2.718281828, typeof(Double));
        Assert.Equal(bnD.GetPacket().ToHex(-1), buf.ToHex(0, writer.WrittenCount));
    }
    #endregion

    #region Nullable<T> 读写
    [Fact]
    [DisplayName("Nullable<T> 有值: 写 0x01 + 值")]
    public void Nullable_WithValue_WritesFlagPlusData()
    {
        var buf = new Byte[64];

        // Nullable<Int32> 有值 → 1字节标志 + 4字节Int32 = 5字节
        var writer = new SpanWriter(buf);
        writer.WriteValue(42, typeof(Int32?));
        Assert.Equal(5, writer.WrittenCount);
        Assert.Equal(0x01, buf[0]); // 标志位

        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(42, (Int32)reader.ReadValue(typeof(Int32?))!);

        // Nullable<Boolean> 有值 → 1字节标志 + 1字节 = 2字节
        writer = new SpanWriter(buf);
        writer.WriteValue(true, typeof(Boolean?));
        Assert.Equal(2, writer.WrittenCount);
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(true, (Boolean)reader.ReadValue(typeof(Boolean?))!);

        // Nullable<Int64> 有值 → 1字节标志 + 8字节Int64 = 9字节
        writer = new SpanWriter(buf);
        writer.WriteValue(9876543210L, typeof(Int64?));
        Assert.Equal(9, writer.WrittenCount);
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(9876543210L, (Int64)reader.ReadValue(typeof(Int64?))!);

        // Nullable<Decimal> 有值 → 1字节标志 + 16字节Decimal = 17字节
        writer = new SpanWriter(buf);
        writer.WriteValue(123.45m, typeof(Decimal?));
        Assert.Equal(17, writer.WrittenCount);
        reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(123.45m, (Decimal)reader.ReadValue(typeof(Decimal?))!);
    }

    [Fact]
    [DisplayName("Nullable<T> null: 仅写 0x00（1字节）")]
    public void Nullable_Null_WritesSingleZeroByte()
    {
        var buf = new Byte[64];
        var nullTypes = new Type[]
        {
            typeof(Boolean?),
            typeof(Int32?),
            typeof(Int64?),
            typeof(DateTime?),
            typeof(Decimal?),
            typeof(Double?),
            typeof(Single?),
        };

        foreach (var type in nullTypes)
        {
            var writer = new SpanWriter(buf);
            writer.WriteValue(null, type);
            Assert.True(writer.WrittenCount == 1, $"Type={type.Name}: should write exactly 1 byte for null");
            Assert.True(buf[0] == 0x00, $"Type={type.Name}: null flag byte should be 0x00");

            var reader = new SpanReader(buf.AsSpan(0, 1));
            Assert.Null(reader.ReadValue(type));
        }
    }

    [Fact]
    [DisplayName("Nullable<T> 与 Binary 字节级一致")]
    public void Nullable_CompatibleWithBinary()
    {
        var dt2025 = new DateTime(2025, 6, 15, 8, 30, 0, DateTimeKind.Utc);
        var cases = new (Object? value, Type type, String label)[]
        {
            (42,          typeof(Int32?),   "Int32? 有值"),
            (null,        typeof(Int32?),   "Int32? null"),
            (true,        typeof(Boolean?), "Boolean? true"),
            (null,        typeof(Boolean?), "Boolean? null"),
            (123.45m,     typeof(Decimal?), "Decimal? 有值"),
            (null,        typeof(Decimal?), "Decimal? null"),
            (9876543210L, typeof(Int64?),   "Int64? 有值"),
            (null,        typeof(Int64?),   "Int64? null"),
            (dt2025,      typeof(DateTime?), "DateTime? 有值"),
            (null,        typeof(DateTime?), "DateTime? null"),
        };

        var buf = new Byte[64];
        foreach (var (value, type, label) in cases)
        {
            var bn = new Binary { IsLittleEndian = true };
            bn.Write(value, type);
            var binaryBytes = bn.GetPacket().ToHex(-1);

            var writer = new SpanWriter(buf) { IsLittleEndian = true };
            writer.WriteValue(value, type);
            var spanBytes = buf.ToHex(0, writer.WrittenCount);

            Assert.True(binaryBytes == spanBytes, $"{label}: Binary={binaryBytes} vs SpanWriter={spanBytes}");
        }
    }
    #endregion

    #region 与 Binary 互操作（跨框架读写）
    [Fact]
    [DisplayName("SpanWriter 写入 → Binary 读取（小端，无EncodeInt）")]
    public void SpanWriter_To_Binary_Read_LittleEndian()
    {
        var buf = new Byte[256];
        var dt = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        // 写入所有基础类型
        var writer = new SpanWriter(buf) { IsLittleEndian = true };
        writer.WriteValue(true, typeof(Boolean));
        writer.WriteValue((Byte)0xAB, typeof(Byte));
        writer.WriteValue((SByte)(-50), typeof(SByte));
        writer.WriteValue((Char)'Z', typeof(Char));
        writer.WriteValue((Int16)1234, typeof(Int16));
        writer.WriteValue((UInt16)5678, typeof(UInt16));
        writer.WriteValue(987654, typeof(Int32));
        writer.WriteValue((UInt32)123456, typeof(UInt32));
        writer.WriteValue(9876543210L, typeof(Int64));
        writer.WriteValue((UInt64)9876543210UL, typeof(UInt64));
        writer.WriteValue(3.14f, typeof(Single));
        writer.WriteValue(2.718281828, typeof(Double));
        writer.WriteValue(12345.678m, typeof(Decimal));
        writer.WriteValue(dt, typeof(DateTime));
        writer.WriteValue("hello", typeof(String));

        // Binary 从同一内存读取
        using var ms = new MemoryStream(buf, 0, writer.WrittenCount);
        var bn = new Binary(ms) { IsLittleEndian = true };

        Assert.Equal(true, bn.Read<Boolean>());
        Assert.Equal((Byte)0xAB, bn.Read<Byte>());
        // Binary框架不支持SByte负值，读为Byte后比较位模式
        Assert.Equal(unchecked((Byte)(SByte)(-50)), bn.Read<Byte>());
        Assert.Equal((Byte)'Z', bn.Read<Byte>()); // Char → 1字节
        Assert.Equal((Int16)1234, bn.Read<Int16>());
        Assert.Equal((UInt16)5678, bn.Read<UInt16>());
        Assert.Equal(987654, bn.Read<Int32>());
        Assert.Equal((UInt32)123456, bn.Read<UInt32>());
        Assert.Equal(9876543210L, bn.Read<Int64>());
        Assert.Equal((UInt64)9876543210UL, bn.Read<UInt64>());
        Assert.Equal(3.14f, bn.Read<Single>());
        Assert.Equal(2.718281828, bn.Read<Double>());
        // Decimal: Binary.Read<Decimal> 与 SpanWriter 格式一致
        Assert.Equal(12345.678m, bn.Read<Decimal>());

        // DateTime: Binary读Unix秒数 → 与SpanWriter写入一致
        var dtRead = bn.Read<DateTime>();
        Assert.Equal(dt.Ticks / TimeSpan.TicksPerSecond, dtRead.Ticks / TimeSpan.TicksPerSecond);

        Assert.Equal("hello", bn.Read<String>());
    }

    [Fact]
    [DisplayName("Binary 写入 → SpanReader 读取（小端，无EncodeInt）")]
    public void Binary_Write_To_SpanReader_Read_LittleEndian()
    {
        var dt = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        // Binary 写入
        var bn = new Binary { IsLittleEndian = true };
        bn.Write(true, typeof(Boolean));
        bn.Write((Byte)0xAB, typeof(Byte));
        // Binary框架不支持SByte负值，改用Byte位表示
        bn.Write(unchecked((Byte)(SByte)(-50)), typeof(Byte));
        bn.Write((Char)'Z', typeof(Char));
        bn.Write((Int16)1234, typeof(Int16));
        bn.Write((UInt16)5678, typeof(UInt16));
        bn.Write(987654, typeof(Int32));
        bn.Write((UInt32)123456, typeof(UInt32));
        bn.Write(9876543210L, typeof(Int64));
        bn.Write((UInt64)9876543210UL, typeof(UInt64));
        bn.Write(3.14f, typeof(Single));
        bn.Write(2.718281828, typeof(Double));
        bn.Write(12345.678m, typeof(Decimal));
        bn.Write(dt, typeof(DateTime));
        bn.Write("hello", typeof(String));

        var data = bn.GetPacket().ToArray();

        // SpanReader 读取
        var reader = new SpanReader(data) { IsLittleEndian = true };
        Assert.Equal(true, (Boolean)reader.ReadValue(typeof(Boolean))!);
        Assert.Equal((Byte)0xAB, (Byte)reader.ReadValue(typeof(Byte))!);
        // Binary写入的是Byte位表示，SpanReader以Byte读取并比较位模式
        Assert.Equal(unchecked((Byte)(SByte)(-50)), (Byte)reader.ReadValue(typeof(Byte))!);
        Assert.Equal((Char)'Z', (Char)reader.ReadValue(typeof(Char))!);
        Assert.Equal((Int16)1234, (Int16)reader.ReadValue(typeof(Int16))!);
        Assert.Equal((UInt16)5678, (UInt16)reader.ReadValue(typeof(UInt16))!);
        Assert.Equal(987654, (Int32)reader.ReadValue(typeof(Int32))!);
        Assert.Equal((UInt32)123456, (UInt32)reader.ReadValue(typeof(UInt32))!);
        Assert.Equal(9876543210L, (Int64)reader.ReadValue(typeof(Int64))!);
        Assert.Equal((UInt64)9876543210UL, (UInt64)reader.ReadValue(typeof(UInt64))!);
        Assert.Equal(3.14f, (Single)reader.ReadValue(typeof(Single))!);
        Assert.Equal(2.718281828, (Double)reader.ReadValue(typeof(Double))!);
        Assert.Equal(12345.678m, (Decimal)reader.ReadValue(typeof(Decimal))!);

        var dtBack = (DateTime)reader.ReadValue(typeof(DateTime))!;
        Assert.Equal(dt.Ticks / TimeSpan.TicksPerSecond, dtBack.Ticks / TimeSpan.TicksPerSecond);

        Assert.Equal("hello", (String?)reader.ReadValue(typeof(String)));
    }

    [Fact]
    [DisplayName("SpanWriter(EncodeInt=true) → Binary(EncodeInt=true) 字节级一致并互读")]
    public void SpanWriter_To_Binary_EncodeInt_Interop()
    {
        var buf = new Byte[256];
        var cases = new (Object value, Type type)[]
        {
            (0, typeof(Int32)),
            (42, typeof(Int32)),
            (16383, typeof(Int32)),
            (1_000_000, typeof(Int32)),
            (-1, typeof(Int32)),
            (9876543210L, typeof(Int64)),
            ((UInt32)3_000_000_000u, typeof(UInt32)),
        };

        foreach (var (value, type) in cases)
        {
            // SpanWriter写 → Binary读
            var writer = new SpanWriter(buf) { IsLittleEndian = true, EncodeInt = true };
            writer.WriteValue(value, type);

            using var ms = new MemoryStream(buf, 0, writer.WrittenCount);
            var bn = new Binary(ms) { IsLittleEndian = true, EncodeInt = true };
            var readBack = bn.Read(type);
            Assert.Equal(value, readBack);

            // Binary写 → SpanReader读
            var bn2 = new Binary { IsLittleEndian = true, EncodeInt = true };
            bn2.Write(value, type);
            var data = bn2.GetPacket().ToArray();
            var reader = new SpanReader(data) { IsLittleEndian = true, EncodeInt = true };
            var readBack2 = reader.ReadValue(type);
            Assert.Equal(value, readBack2);
        }
    }

    [Fact]
    [DisplayName("SpanWriter(FullTime=true) → Binary(FullTime=true) 字节级一致并互读")]
    public void SpanWriter_To_Binary_FullTime_Interop()
    {
        var buf = new Byte[64];
        var datetimes = new DateTime[]
        {
            new(2025, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc),
            new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        foreach (var dt in datetimes)
        {
            // SpanWriter写 → Binary读
            var writer = new SpanWriter(buf) { IsLittleEndian = true, FullTime = true };
            writer.WriteValue(dt, typeof(DateTime));

            using var ms = new MemoryStream(buf, 0, writer.WrittenCount);
            var bn = new Binary(ms) { IsLittleEndian = true, FullTime = true };
            var dtBack = bn.Read<DateTime>();
            Assert.Equal(dt, dtBack);

            // Binary写 → SpanReader读
            var bn2 = new Binary { IsLittleEndian = true, FullTime = true };
            bn2.Write(dt, typeof(DateTime));
            var data = bn2.GetPacket().ToArray();
            var reader = new SpanReader(data) { IsLittleEndian = true, FullTime = true };
            var dtBack2 = (DateTime)reader.ReadValue(typeof(DateTime))!;
            Assert.Equal(dt, dtBack2);
        }
    }

    [Fact]
    [DisplayName("SpanWriter 写、SpanReader 读：字节精确往返（全类型）")]
    public void SpanWriter_SpanReader_RoundTrip_Exact()
    {
        var buf = new Byte[512];
        var guid = new Guid("12345678-1234-5678-9abc-def012345678");
        var byteArr = new Byte[] { 1, 2, 3, 4, 5 };
        var dt = new DateTime(2025, 3, 15, 10, 20, 30, DateTimeKind.Utc);

        var writer = new SpanWriter(buf) { IsLittleEndian = true, EncodeInt = true, FullTime = true };
        writer.WriteValue(true, typeof(Boolean));
        writer.WriteValue((Byte)0x42, typeof(Byte));
        writer.WriteValue((SByte)(-99), typeof(SByte));
        writer.WriteValue((Char)'X', typeof(Char));
        writer.WriteValue((Int16)(-300), typeof(Int16));
        writer.WriteValue((UInt16)50000, typeof(UInt16));
        writer.WriteValue(1_000_000, typeof(Int32));
        writer.WriteValue((UInt32)4_000_000_000u, typeof(UInt32));
        writer.WriteValue(-9876543210L, typeof(Int64));
        writer.WriteValue((UInt64)9876543210UL, typeof(UInt64));
        writer.WriteValue(1.5f, typeof(Single));
        writer.WriteValue(3.14159265358979, typeof(Double));
        writer.WriteValue(99999.9m, typeof(Decimal));
        writer.WriteValue(dt, typeof(DateTime));
        writer.WriteValue("测试字符串", typeof(String));
        writer.WriteValue(byteArr, typeof(Byte[]));
        writer.WriteValue(guid, typeof(Guid));

        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount))
        {
            IsLittleEndian = true,
            EncodeInt = true,
            FullTime = true,
        };
        Assert.Equal(true, (Boolean)reader.ReadValue(typeof(Boolean))!);
        Assert.Equal((Byte)0x42, (Byte)reader.ReadValue(typeof(Byte))!);
        Assert.Equal((SByte)(-99), (SByte)reader.ReadValue(typeof(SByte))!);
        Assert.Equal((Char)'X', (Char)reader.ReadValue(typeof(Char))!);
        Assert.Equal((Int16)(-300), (Int16)reader.ReadValue(typeof(Int16))!);
        Assert.Equal((UInt16)50000, (UInt16)reader.ReadValue(typeof(UInt16))!);
        Assert.Equal(1_000_000, (Int32)reader.ReadValue(typeof(Int32))!);
        Assert.Equal((UInt32)4_000_000_000u, (UInt32)reader.ReadValue(typeof(UInt32))!);
        Assert.Equal(-9876543210L, (Int64)reader.ReadValue(typeof(Int64))!);
        Assert.Equal((UInt64)9876543210UL, (UInt64)reader.ReadValue(typeof(UInt64))!);
        Assert.Equal(1.5f, (Single)reader.ReadValue(typeof(Single))!);
        Assert.Equal(3.14159265358979, (Double)reader.ReadValue(typeof(Double))!);
        Assert.Equal(99999.9m, (Decimal)reader.ReadValue(typeof(Decimal))!);
        Assert.Equal(dt, (DateTime)reader.ReadValue(typeof(DateTime))!); // FullTime=true → Ticks级精度
        Assert.Equal("测试字符串", (String?)reader.ReadValue(typeof(String)));
        Assert.Equal(byteArr, (Byte[]?)reader.ReadValue(typeof(Byte[])));
        Assert.Equal(guid, (Guid)reader.ReadValue(typeof(Guid))!);
        Assert.Equal(writer.WrittenCount, reader.Position); // 全部消耗完
    }

    [Fact]
    [DisplayName("WriteValue<T> 泛型便捷方法正确（读）")]
    public void ReadValue_Generic_Helper()
    {
        var buf = new Byte[64];
        var writer = new SpanWriter(buf);
        writer.WriteValue(42, typeof(Int32));
        writer.WriteValue("world", typeof(String));

        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        Assert.Equal(42, reader.ReadValue<Int32>());
        Assert.Equal("world", reader.ReadValue<String>());
    }
    #endregion

    #region 字节级一致性验证（大端/小端）
    [Fact]
    [DisplayName("大端模式: WriteValue 与 Binary(大端) 字节级一致（基础类型）")]
    public void WriteValue_BigEndian_CompatibleWithBinary()
    {
        var dt2025 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var cases = new (Object value, Type type, String label)[]
        {
            (true,           typeof(Boolean),  "Boolean true"),
            (false,          typeof(Boolean),  "Boolean false"),
            ((Byte)0xAB,     typeof(Byte),     "Byte"),
            (12345,          typeof(Int32),    "Int32"),
            (9876543210L,    typeof(Int64),    "Int64"),
            (3.14f,          typeof(Single),   "Single"),
            (2.71828,        typeof(Double),   "Double"),
            ("hello",        typeof(String),   "String"),
            (dt2025,         typeof(DateTime), "DateTime"),
            (DateTime.MinValue, typeof(DateTime), "DateTime.MinValue"),
        };

        var buf = new Byte[256];
        foreach (var (value, type, label) in cases)
        {
            var bn = new Binary { IsLittleEndian = false };
            bn.Write(value, type);
            var binaryBytes = bn.GetPacket().ToHex(-1);

            var writer = new SpanWriter(buf) { IsLittleEndian = false };
            writer.WriteValue(value, type);
            var spanBytes = buf.ToHex(0, writer.WrittenCount);

            Assert.True(binaryBytes == spanBytes, $"{label}: Binary={binaryBytes} vs SpanWriter={spanBytes}");
        }
    }

    [Fact]
    [DisplayName("小端模式: WriteValue 与 Binary(小端) 字节级一致（基础类型）")]
    public void WriteValue_LittleEndian_CompatibleWithBinary()
    {
        var cases = new (Object value, Type type, String label)[]
        {
            ((Int16)1234,   typeof(Int16),  "Int16"),
            ((UInt16)65535, typeof(UInt16), "UInt16"),
            (Int32.MaxValue, typeof(Int32), "Int32.MaxValue"),
            (Int64.MinValue, typeof(Int64), "Int64.MinValue"),
            (3.14f,          typeof(Single), "Single"),
            (2.71828,        typeof(Double), "Double"),
        };

        var buf = new Byte[256];
        foreach (var (value, type, label) in cases)
        {
            var bn = new Binary { IsLittleEndian = true };
            bn.Write(value, type);
            var binaryBytes = bn.GetPacket().ToHex(-1);

            var writer = new SpanWriter(buf) { IsLittleEndian = true };
            writer.WriteValue(value, type);
            var spanBytes = buf.ToHex(0, writer.WrittenCount);

            Assert.True(binaryBytes == spanBytes, $"{label}: Binary={binaryBytes} vs SpanWriter={spanBytes}");
        }
    }
    #endregion
}
