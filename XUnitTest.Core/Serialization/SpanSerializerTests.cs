using System.ComponentModel;
using NewLife;
using NewLife.Buffers;
using NewLife.Data;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

public class SpanSerializerTests
{
    #region 基础类型
    [Fact]
    [DisplayName("基础类型序列化与反序列化")]
    public void BasicTypes()
    {
        var model = new BasicModel
        {
            Flag = true,
            ByteVal = 0xAB,
            Int16Val = -1234,
            UInt16Val = 5678,
            Int32Val = 123456,
            UInt32Val = 654321,
            Int64Val = -9876543210L,
            UInt64Val = 9876543210UL,
            FloatVal = 3.14f,
            DoubleVal = 2.718281828,
            Name = "Stone",
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        Assert.True(writer.WrittenCount > 0);

        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (BasicModel)SpanSerializer.ReadObject(ref reader, typeof(BasicModel));
        Assert.Equal(model.Flag, model2.Flag);
        Assert.Equal(model.ByteVal, model2.ByteVal);
        Assert.Equal(model.Int16Val, model2.Int16Val);
        Assert.Equal(model.UInt16Val, model2.UInt16Val);
        Assert.Equal(model.Int32Val, model2.Int32Val);
        Assert.Equal(model.UInt32Val, model2.UInt32Val);
        Assert.Equal(model.Int64Val, model2.Int64Val);
        Assert.Equal(model.UInt64Val, model2.UInt64Val);
        Assert.Equal(model.FloatVal, model2.FloatVal);
        Assert.Equal(model.DoubleVal, model2.DoubleVal);
        Assert.Equal(model.Name, model2.Name);
    }

    [Fact]
    [DisplayName("SpanWriter/SpanReader方式序列化")]
    public void WriteAndRead()
    {
        var model = new BasicModel
        {
            Flag = true,
            ByteVal = 0x42,
            Int32Val = 1234,
            Name = "Hello",
        };

        var buffer = new Byte[256];
        var writer = new SpanWriter(buffer);
        SpanSerializer.WriteObject(ref writer, model, typeof(BasicModel));
        var written = writer.WrittenCount;

        var reader = new SpanReader(buffer.AsSpan(0, written));
        var model2 = (BasicModel)SpanSerializer.ReadObject(ref reader, typeof(BasicModel));

        Assert.Equal(model.Flag, model2.Flag);
        Assert.Equal(model.ByteVal, model2.ByteVal);
        Assert.Equal(model.Int32Val, model2.Int32Val);
        Assert.Equal(model.Name, model2.Name);
    }

    [Fact]
    [DisplayName("Span方式序列化全程零分配")]
    public void SerializeToSpan()
    {
        var model = new BasicModel
        {
            Flag = true,
            ByteVal = 0x42,
            Int32Val = 1234,
            Name = "Hello",
        };

        var buffer = new Byte[256];
        var writer2 = new SpanWriter(buffer);
        SpanSerializer.WriteObject(ref writer2, model, model.GetType());
        var written = writer2.WrittenCount;
        Assert.True(written > 0);

        var reader2 = new SpanReader(buffer.AsSpan(0, written));
        var model2 = (BasicModel)SpanSerializer.ReadObject(ref reader2, typeof(BasicModel));
        Assert.Equal(model.Flag, model2.Flag);
        Assert.Equal(model.ByteVal, model2.ByteVal);
        Assert.Equal(model.Int32Val, model2.Int32Val);
        Assert.Equal(model.Name, model2.Name);
    }
    #endregion

    #region 嵌套对象
    [Fact]
    [DisplayName("嵌套引用类型序列化")]
    public void NestedObject()
    {
        var model = new OrderModel
        {
            OrderId = 10001,
            Customer = "张三",
            Address = new AddressModel
            {
                Province = "广东",
                City = "深圳",
                Detail = "南山区科技园",
            },
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (OrderModel)SpanSerializer.ReadObject(ref reader, typeof(OrderModel));

        Assert.Equal(model.OrderId, model2.OrderId);
        Assert.Equal(model.Customer, model2.Customer);
        Assert.NotNull(model2.Address);
        Assert.Equal(model.Address.Province, model2.Address!.Province);
        Assert.Equal(model.Address.City, model2.Address.City);
        Assert.Equal(model.Address.Detail, model2.Address.Detail);
    }

    [Fact]
    [DisplayName("嵌套对象为null时序列化")]
    public void NestedNull()
    {
        var model = new OrderModel
        {
            OrderId = 10002,
            Customer = "李四",
            Address = null,
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (OrderModel)SpanSerializer.ReadObject(ref reader, typeof(OrderModel));

        Assert.Equal(model.OrderId, model2.OrderId);
        Assert.Equal(model.Customer, model2.Customer);
        // 与Binary一致，null嵌套对象不写入数据，数据流不足时跳过后续属性
        Assert.Null(model2.Address);
    }
    #endregion

    #region ISpanSerializable接口
    [Fact]
    [DisplayName("ISpanSerializable接口零反射路径")]
    public void SpanSerializableInterface()
    {
        var model = new FastMessage
        {
            Id = 42,
            Action = "Login",
            Timestamp = new DateTime(2025, 7, 1, 12, 0, 0),
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (FastMessage)SpanSerializer.ReadObject(ref reader, typeof(FastMessage));

        Assert.Equal(model.Id, model2.Id);
        Assert.Equal(model.Action, model2.Action);
        Assert.Equal(model.Timestamp, model2.Timestamp);
    }

    [Fact]
    [DisplayName("ISpanSerializable嵌套在普通类中")]
    public void SpanSerializableNested()
    {
        var model = new WrapperModel
        {
            Name = "Test",
            Message = new FastMessage
            {
                Id = 99,
                Action = "Ping",
                Timestamp = new DateTime(2025, 1, 1),
            },
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (WrapperModel)SpanSerializer.ReadObject(ref reader, typeof(WrapperModel));

        Assert.Equal(model.Name, model2.Name);
        Assert.NotNull(model2.Message);
        Assert.Equal(model.Message.Id, model2.Message!.Id);
        Assert.Equal(model.Message.Action, model2.Message.Action);
        Assert.Equal(model.Message.Timestamp, model2.Message.Timestamp);
    }
    #endregion

    #region 引用类型Write/Read泛型
    [Fact]
    [DisplayName("Write/Read泛型含null标记")]
    public void WriteReadGenericWithNull()
    {
        var buffer = new Byte[256];

        // 写入null
        var writer = new SpanWriter(buffer);
        SpanSerializer.Write<FastMessage>(ref writer, null);
        var nullLen = writer.WrittenCount;
        Assert.Equal(1, nullLen); // 仅1字节null标记

        // 读取null
        var reader = new SpanReader(buffer.AsSpan(0, nullLen));
        var result = SpanSerializer.Read<FastMessage>(ref reader);
        Assert.Null(result);

        // 写入非null
        var msg = new FastMessage { Id = 7, Action = "Test", Timestamp = DateTime.MinValue };
        var writer2 = new SpanWriter(buffer);
        SpanSerializer.Write(ref writer2, msg);
        var msgLen = writer2.WrittenCount;
        Assert.True(msgLen > 1);

        // 读取非null
        var reader2 = new SpanReader(buffer.AsSpan(0, msgLen));
        var result2 = SpanSerializer.Read<FastMessage>(ref reader2);
        Assert.NotNull(result2);
        Assert.Equal(7, result2!.Id);
        Assert.Equal("Test", result2.Action);
    }
    #endregion

    #region 枚举与特殊类型
    [Fact]
    [DisplayName("枚举属性序列化")]
    public void EnumProperty()
    {
        var model = new EnumModel
        {
            Status = MyStatus.Running,
            Level = 5,
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (EnumModel)SpanSerializer.ReadObject(ref reader, typeof(EnumModel));

        Assert.Equal(MyStatus.Running, model2.Status);
        Assert.Equal(5, model2.Level);
    }

    [Fact]
    [DisplayName("DateTime和Guid属性序列化")]
    public void DateTimeAndGuid()
    {
        var now = new DateTime(2025, 7, 1, 8, 30, 0, DateTimeKind.Utc);
        var guid = Guid.NewGuid();
        var model = new SpecialModel
        {
            Id = guid,
            CreatedAt = now,
            Data = [0x01, 0x02, 0x03, 0x04],
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (SpecialModel)SpanSerializer.ReadObject(ref reader, typeof(SpecialModel));

        Assert.Equal(guid, model2.Id);
        // DateTime以UInt32秒存储（与Binary兼容），精度为秒级
        Assert.Equal(now.Date, model2.CreatedAt.Date);
        Assert.Equal(now.Hour, model2.CreatedAt.Hour);
        Assert.Equal(now.Minute, model2.CreatedAt.Minute);
        Assert.Equal(now.Second, model2.CreatedAt.Second);
        Assert.Equal(model.Data, model2.Data);
    }

    [Fact]
    [DisplayName("Byte数组为null和空")]
    public void ByteArrayNullAndEmpty()
    {
        // null
        var model = new SpecialModel { Id = Guid.Empty, CreatedAt = DateTime.MinValue, Data = null };
        var buf = new Byte[4096];
        var w1 = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref w1, model, model.GetType());
        var r1 = new SpanReader(buf.AsSpan(0, w1.WrittenCount));
        var model2 = (SpecialModel)SpanSerializer.ReadObject(ref r1, typeof(SpecialModel));
        Assert.Empty(model2.Data!);

        // empty
        model.Data = [];
        var w2 = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref w2, model, model.GetType());
        var r2 = new SpanReader(buf.AsSpan(0, w2.WrittenCount));
        model2 = (SpecialModel)SpanSerializer.ReadObject(ref r2, typeof(SpecialModel));
        Assert.Empty(model2.Data!);

        // has data
        model.Data = [0xFF, 0xFE];
        var w3 = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref w3, model, model.GetType());
        var r3 = new SpanReader(buf.AsSpan(0, w3.WrittenCount));
        model2 = (SpecialModel)SpanSerializer.ReadObject(ref r3, typeof(SpecialModel));
        Assert.Equal(new Byte[] { 0xFF, 0xFE }, model2.Data);
    }

    [Fact]
    [DisplayName("Nullable值类型属性序列化")]
    public void NullableValueTypes()
    {
        var time = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var model = new NullableModel
        {
            Score = 99,
            Count = null,
            Time = time,
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (NullableModel)SpanSerializer.ReadObject(ref reader, typeof(NullableModel));

        Assert.Equal(99, model2.Score);
        Assert.Null(model2.Count);
        Assert.NotNull(model2.Time);
        Assert.Equal(time.Date, model2.Time!.Value.Date);
    }
    #endregion

    #region 补充类型覆盖
    [Fact]
    [DisplayName("SByte/Char/Decimal属性序列化")]
    public void SByteCharDecimal()
    {
        var model = new ExtendedModel
        {
            SByteVal = -100,
            CharVal = 'A',
            DecimalVal = 123456.789m,
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (ExtendedModel)SpanSerializer.ReadObject(ref reader, typeof(ExtendedModel));

        Assert.Equal(model.SByteVal, model2.SByteVal);
        Assert.Equal(model.CharVal, model2.CharVal);
        Assert.Equal(model.DecimalVal, model2.DecimalVal);
    }

    [Fact]
    [DisplayName("值类型结构体属性序列化")]
    public void StructProperty()
    {
        var model = new StructHostModel
        {
            Name = "Host",
            Point = new PointStruct { X = 10, Y = 20 },
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (StructHostModel)SpanSerializer.ReadObject(ref reader, typeof(StructHostModel));

        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(model.Point.X, model2.Point.X);
        Assert.Equal(model.Point.Y, model2.Point.Y);
    }

    [Fact]
    [DisplayName("Nullable枚举属性序列化")]
    public void NullableEnum()
    {
        var model = new NullableEnumModel
        {
            Status = MyStatus.Paused,
            Kind = null,
        };

        var buf = new Byte[4096];
        var w1 = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref w1, model, model.GetType());
        var r1 = new SpanReader(buf.AsSpan(0, w1.WrittenCount));
        var model2 = (NullableEnumModel)SpanSerializer.ReadObject(ref r1, typeof(NullableEnumModel));

        Assert.Equal(MyStatus.Paused, model2.Status);
        Assert.Null(model2.Kind);

        // 非湿Nullable枚举
        model.Kind = MyStatus.Running;
        var w2 = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref w2, model, model.GetType());
        var r2 = new SpanReader(buf.AsSpan(0, w2.WrittenCount));
        var model3 = (NullableEnumModel)SpanSerializer.ReadObject(ref r2, typeof(NullableEnumModel));
        Assert.Equal(MyStatus.Running, model3.Kind);
    }

    [Fact]
    [DisplayName("String为null时反序列化为空串")]
    public void StringNullProperty()
    {
        var model = new BasicModel { Flag = false, Name = null };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (BasicModel)SpanSerializer.ReadObject(ref reader, typeof(BasicModel));

        // null字符串反序列化后为空串（与Binary一致）
        Assert.Equal(String.Empty, model2.Name);
    }

    [Fact]
    [DisplayName("ISpanSerializable嵌套为null")]
    public void SpanSerializableNestedNull()
    {
        var model = new WrapperModel
        {
            Name = "NoMessage",
            Message = null,
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (WrapperModel)SpanSerializer.ReadObject(ref reader, typeof(WrapperModel));

        Assert.Equal("NoMessage", model2.Name);
        // 与Binary一致，null嵌套对象不写入数据，数据流不足时跳过后续属性
        Assert.Null(model2.Message);
    }
    #endregion

    #region 非泛型与异常
    [Fact]
    [DisplayName("非泛型Deserialize按Type反序列化")]
    public void DeserializeByType()
    {
        var model = new BasicModel { Int32Val = 42, Name = "TypeTest" };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var obj = SpanSerializer.ReadObject(ref reader, typeof(BasicModel));

        Assert.IsType<BasicModel>(obj);
        var model2 = (BasicModel)obj;
        Assert.Equal(42, model2.Int32Val);
        Assert.Equal("TypeTest", model2.Name);
    }

    [Fact]
    [DisplayName("WriteObject传null抛异常")]
    public void WriteObjectNullThrows()
    {
        Exception? ex = null;
        try
        {
            var buf = new Byte[64];
            var writer = new SpanWriter(buf);
            SpanSerializer.WriteObject(ref writer, null!, typeof(BasicModel));
        }
        catch (Exception e)
        {
            ex = e;
        }
        Assert.NotNull(ex);
    }
    #endregion

    #region HeaderReserve
    [Fact]
    [DisplayName("HeaderReserve预留空间可向前扩展头部")]
    public void HeaderReserveExpand()
    {
        var model = new BasicModel { Int32Val = 1234 };
        var reserve = SpanSerializer.HeaderReserve;

        // 手动写入数据（在 reserve 偏移后），构造 OwnerPacket 模拟 Serialize() 内部布局
        var rawBuf = new Byte[4096];
        var writer = new SpanWriter(rawBuf.AsSpan(reserve));
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var dataLen = writer.WrittenCount;
        Assert.True(dataLen > 0);

        // hasOwner=false：rawBuf 是普通数组，Dispose 时无需归还池
        var pk = new OwnerPacket(rawBuf, reserve, dataLen, false);

        // 验证 OwnerPacket 有足够的前方预留空间
        Assert.True(pk.Offset >= SpanSerializer.HeaderReserve);

        // 向前扩展8字节协议头
        var expanded = new OwnerPacket(pk, 8);
        Assert.Equal(dataLen + 8, expanded.Length);

        // 扩展区域可写入协议头
        var span = expanded.GetSpan();
        span[0] = 0xAA;
        span[7] = 0xBB;
        Assert.Equal(0xAA, expanded.Buffer[expanded.Offset]);

        // 原有数据从offset=8开始，仍可正确反序列化
        var dataSpan = expanded.GetSpan().Slice(8);
        var reader = new SpanReader(dataSpan.Slice(0, dataLen));
        var model2 = (BasicModel)SpanSerializer.ReadObject(ref reader, typeof(BasicModel));
        Assert.Equal(1234, model2.Int32Val);
    }
    #endregion

    #region ToPacket/Serialize 双路径
    [Fact]
    [DisplayName("SpanWriter序列化CompatModel完整往返（小数据）")]
    public void SerializeSmallDataPath()
    {
        var model = new CompatModel
        {
            Code = 1234,
            Name = "SmallTest",
            Flag = true,
            Score = 3.14f,
            Count = 999999L,
            Level = 10,
            Time = new DateTime(2025, 7, 1, 8, 0, 0, DateTimeKind.Utc),
            Tag = 'Z',
            Amount = 12.34m,
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        Assert.True(writer.WrittenCount > 0);

        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (CompatModel)SpanSerializer.ReadObject(ref reader, typeof(CompatModel));
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(model.Flag, model2.Flag);
        Assert.Equal(model.Score, model2.Score);
        Assert.Equal(model.Count, model2.Count);
        Assert.Equal(model.Level, model2.Level);
        Assert.Equal(model.Time.Date, model2.Time.Date);
        Assert.Equal(model.Tag, model2.Tag);
        Assert.Equal(model.Amount, model2.Amount);
    }

    [Fact]
    [DisplayName("SpanWriter序列化CompatModel完整往返（大数据）")]
    public void SerializeLargeDataPath()
    {
        var model = new CompatModel
        {
            Code = 5678,
            Name = "LargeDataOverflow",
            Flag = true,
            Score = 2.718f,
            Count = 88888888L,
            Level = 30,
            Time = new DateTime(2025, 7, 1, 8, 0, 0, DateTimeKind.Utc),
            Tag = 'W',
            Amount = 99.99m,
        };

        var buf = new Byte[4096];
        var writer = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        Assert.True(writer.WrittenCount > 0);

        var reader = new SpanReader(buf.AsSpan(0, writer.WrittenCount));
        var model2 = (CompatModel)SpanSerializer.ReadObject(ref reader, typeof(CompatModel));
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(model.Flag, model2.Flag);
        Assert.Equal(model.Score, model2.Score);
        Assert.Equal(model.Count, model2.Count);
        Assert.Equal(model.Level, model2.Level);
        Assert.Equal(model.Time.Date, model2.Time.Date);
        Assert.Equal(model.Tag, model2.Tag);
        Assert.Equal(model.Amount, model2.Amount);
    }

    [Fact]
    [DisplayName("SpanWriter两次序列化结果二进制一致")]
    public void SerializeSmallAndLargePathIdentical()
    {
        var model = new CompatModel
        {
            Code = 42,
            Name = "Identity",
            Flag = true,
            Score = 1.5f,
            Count = 100L,
            Level = 5,
            Time = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            Tag = 'A',
            Amount = 50.0m,
        };

        var buf1 = new Byte[4096];
        var w1 = new SpanWriter(buf1);
        SpanSerializer.WriteObject(ref w1, model, model.GetType());

        var buf2 = new Byte[4096];
        var w2 = new SpanWriter(buf2);
        SpanSerializer.WriteObject(ref w2, model, model.GetType());

        // 两次序列化产出完全相同的字节序列
        Assert.Equal(w1.WrittenCount, w2.WrittenCount);
        Assert.True(buf1.AsSpan(0, w1.WrittenCount).SequenceEqual(buf2.AsSpan(0, w2.WrittenCount)));
    }

    [Fact]
    [DisplayName("ToPacket小数据走缓冲区路径")]
    public void ToPacketSmallDataPath()
    {
        var time = new DateTime(2025, 1, 1, 12, 0, 0);
        var msg = new FastMessage { Id = 99, Action = "SmallMsg", Timestamp = time };

        // 默认8192缓冲区 → 小数据路径
        var pk = msg.ToPacket();
        var ownerPk = Assert.IsType<OwnerPacket>(pk);

        // 小数据路径通过 Slice 返回，Offset 等于 reserve(32)
        Assert.Equal(32, ownerPk.Offset);

        // 完整反序列化验证
        var reader = new SpanReader(pk.GetSpan());
        var msg2 = new FastMessage();
        msg2.Read(ref reader);

        Assert.Equal(99, msg2.Id);
        Assert.Equal("SmallMsg", msg2.Action);
        Assert.Equal(time, msg2.Timestamp);

        pk?.Dispose();
    }

    [Fact]
    [DisplayName("ToPacket大数据溢出到流路径")]
    public void ToPacketLargeDataPath()
    {
        var time = new DateTime(2025, 7, 1, 15, 30, 0);
        var msg = new FastMessage { Id = 42, Action = "Overflow", Timestamp = time };

        // 用极小缓冲区：42 - reserve(32) = 10字节可用，总数据约20字节 → 必定溢出到流
        var pk = msg.ToPacket(bufferSize: 42);
        var ownerPk = Assert.IsType<OwnerPacket>(pk);
        Assert.True(ownerPk.Length > 0);

        // 大数据路径窃取池化 MemoryStream 缓冲区，Offset 等于 reserve(32)
        Assert.Equal(32, ownerPk.Offset);

        // 完整反序列化验证所有字段
        var reader = new SpanReader(pk.GetSpan());
        var msg2 = new FastMessage();
        msg2.Read(ref reader);

        Assert.Equal(42, msg2.Id);
        Assert.Equal("Overflow", msg2.Action);
        Assert.Equal(time, msg2.Timestamp);

        pk?.Dispose();
    }

    [Fact]
    [DisplayName("Serialize头部预留空间可向前扩展")]
    public void SerializeHeaderReserveExpandable()
    {
        var model = new BasicModel { Int32Val = 42, Name = "Reserve" };
        var reserve = SpanSerializer.HeaderReserve;

        // 手动写入数据后构造具有预留偏移的 OwnerPacket
        var rawBuf = new Byte[4096];
        var writer = new SpanWriter(rawBuf.AsSpan(reserve));
        SpanSerializer.WriteObject(ref writer, model, model.GetType());
        var dataLen = writer.WrittenCount;

        var pk = new OwnerPacket(rawBuf, reserve, dataLen, false);

        // Offset 精确等于 HeaderReserve，可向前扩展全部预留空间
        Assert.Equal(SpanSerializer.HeaderReserve, pk.Offset);

        var expanded = new OwnerPacket(pk, SpanSerializer.HeaderReserve);
        Assert.Equal(dataLen + SpanSerializer.HeaderReserve, expanded.Length);

        // 扩展区域可写入协议头
        var span = expanded.GetSpan();
        span[0] = 0xDE;
        span[1] = 0xAD;
        Assert.Equal(0xDE, expanded.Buffer[expanded.Offset]);
        Assert.Equal(0xAD, expanded.Buffer[expanded.Offset + 1]);

        // 原有数据仍可正确反序列化
        var dataSpan = span.Slice(SpanSerializer.HeaderReserve, dataLen);
        var reader = new SpanReader(dataSpan);
        var model2 = (BasicModel)SpanSerializer.ReadObject(ref reader, typeof(BasicModel));
        Assert.Equal(42, model2.Int32Val);
        Assert.Equal("Reserve", model2.Name);
    }
    #endregion

    #region Binary兼容性
    /// <remarks>
    /// SpanSerializer与Binary在所有基础类型上二进制格式完全一致（字节序匹配时）：
    /// Boolean、Byte、SByte、Char、Int16/UInt16、Int32/UInt32、Int64/UInt64、Single、Double、Decimal、
    /// DateTime、String、枚举、Nullable基础类型、嵌套引用类型。
    /// </remarks>
    [Fact]
    [DisplayName("Binary写入SpanSerializer读取（大端）")]
    public void BinaryWriteSpanRead_BigEndian()
    {
        var model = new CompatModel { Code = 1234, Name = "Stone", Flag = true, Score = 3.14f, Time = new DateTime(2025, 7, 1, 8, 0, 0, DateTimeKind.Utc), Tag = 'X', Amount = 99.99m };

        // Binary默认大端写入
        var bn = new Binary();
        bn.Write(model);
        var pk = bn.GetPacket();

        // SpanSerializer大端读取
        var reader = new SpanReader(pk.GetSpan()) { IsLittleEndian = false };
        var model2 = (CompatModel)SpanSerializer.ReadObject(ref reader, typeof(CompatModel));

        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(model.Flag, model2.Flag);
        Assert.Equal(model.Score, model2.Score);
        Assert.Equal(model.Count, model2.Count);
        Assert.Equal(model.Level, model2.Level);
        Assert.Equal(model.Time.Date, model2.Time.Date);
        Assert.Equal(model.Tag, model2.Tag);
        Assert.Equal(model.Amount, model2.Amount);
    }

    [Fact]
    [DisplayName("SpanSerializer写入Binary读取（大端）")]
    public void SpanWriteBinaryRead_BigEndian()
    {
        var model = new CompatModel { Code = 1234, Name = "Stone", Flag = true, Score = 3.14f, Time = new DateTime(2025, 7, 1, 8, 0, 0, DateTimeKind.Utc), Tag = 'X', Amount = 99.99m };

        // SpanSerializer大端写入
        var buffer = new Byte[256];
        var writer = new SpanWriter(buffer) { IsLittleEndian = false };
        SpanSerializer.WriteObject(ref writer, model, typeof(CompatModel));

        // Binary默认大端读取
        var ms = new MemoryStream(buffer, 0, writer.WrittenCount);
        var bn = new Binary { Stream = ms };
        var model2 = bn.Read<CompatModel>();

        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(model.Flag, model2.Flag);
        Assert.Equal(model.Score, model2.Score);
        Assert.Equal(model.Count, model2.Count);
        Assert.Equal(model.Level, model2.Level);
        Assert.Equal(model.Time.Date, model2.Time.Date);
        Assert.Equal(model.Tag, model2.Tag);
        Assert.Equal(model.Amount, model2.Amount);
    }

    [Fact]
    [DisplayName("Binary写入SpanSerializer读取（小端）")]
    public void BinaryWriteSpanRead_LittleEndian()
    {
        var model = new CompatModel { Code = 1234, Name = "Stone", Flag = true, Score = 3.14f, Time = new DateTime(2025, 7, 1, 8, 0, 0, DateTimeKind.Utc), Tag = 'X', Amount = 99.99m };

        // Binary小端写入
        var bn = new Binary { IsLittleEndian = true };
        bn.Write(model);
        var pk = bn.GetPacket();

        // SpanSerializer默认小端读取
        var reader = new SpanReader(pk.GetSpan());
        var model2 = (CompatModel)SpanSerializer.ReadObject(ref reader, typeof(CompatModel));

        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(model.Flag, model2.Flag);
        Assert.Equal(model.Score, model2.Score);
        Assert.Equal(model.Count, model2.Count);
        Assert.Equal(model.Level, model2.Level);
        Assert.Equal(model.Time.Date, model2.Time.Date);
        Assert.Equal(model.Tag, model2.Tag);
        Assert.Equal(model.Amount, model2.Amount);
    }

    [Fact]
    [DisplayName("SpanSerializer写入Binary读取（小端）")]
    public void SpanWriteBinaryRead_LittleEndian()
    {
        var model = new CompatModel { Code = 1234, Name = "Stone", Flag = true, Score = 3.14f, Time = new DateTime(2025, 7, 1, 8, 0, 0, DateTimeKind.Utc), Tag = 'X', Amount = 99.99m };

        // SpanSerializer默认小端写入
        var buffer = new Byte[256];
        var writer = new SpanWriter(buffer);
        SpanSerializer.WriteObject(ref writer, model, typeof(CompatModel));

        // Binary小端读取
        var ms = new MemoryStream(buffer, 0, writer.WrittenCount);
        var bn = new Binary { Stream = ms, IsLittleEndian = true };
        var model2 = bn.Read<CompatModel>();

        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(model.Flag, model2.Flag);
        Assert.Equal(model.Score, model2.Score);
        Assert.Equal(model.Count, model2.Count);
        Assert.Equal(model.Level, model2.Level);
        Assert.Equal(model.Time.Date, model2.Time.Date);
        Assert.Equal(model.Tag, model2.Tag);
        Assert.Equal(model.Amount, model2.Amount);
    }

    [Fact]
    [DisplayName("Binary与SpanSerializer字节级一致（大端）")]
    public void ByteIdentical_BigEndian()
    {
        var model = new CompatModel { Code = 1234, Name = "Stone", Flag = true, Score = 3.14f, Time = new DateTime(2025, 7, 1, 8, 0, 0, DateTimeKind.Utc), Tag = 'X', Amount = 99.99m };

        // Binary大端写入
        var bn = new Binary();
        bn.Write(model);
        var binaryHex = bn.GetPacket().ToHex(-1);

        // SpanSerializer大端写入
        var buffer = new Byte[256];
        var writer = new SpanWriter(buffer) { IsLittleEndian = false };
        SpanSerializer.WriteObject(ref writer, model, typeof(CompatModel));
        var spanHex = buffer.ToHex(0, writer.WrittenCount);

        // 两者产出完全相同的字节序列
        Assert.Equal(binaryHex, spanHex);
    }

    [Fact]
    [DisplayName("Binary与SpanSerializer字节级一致（小端）")]
    public void ByteIdentical_LittleEndian()
    {
        var model = new CompatModel { Code = 1234, Name = "Stone", Flag = true, Score = 3.14f, Time = new DateTime(2025, 7, 1, 8, 0, 0, DateTimeKind.Utc), Tag = 'X', Amount = 99.99m };

        // Binary小端写入
        var bn = new Binary { IsLittleEndian = true };
        bn.Write(model);
        var binaryHex = bn.GetPacket().ToHex(-1);

        // SpanSerializer小端写入
        var buffer = new Byte[256];
        var writer = new SpanWriter(buffer);
        SpanSerializer.WriteObject(ref writer, model, typeof(CompatModel));
        var spanHex = buffer.ToHex(0, writer.WrittenCount);

        // 两者产出完全相同的字节序列
        Assert.Equal(binaryHex, spanHex);
    }
    #endregion

    #region 测试模型
    private class BasicModel
    {
        public Boolean Flag { get; set; }
        public Byte ByteVal { get; set; }
        public Int16 Int16Val { get; set; }
        public UInt16 UInt16Val { get; set; }
        public Int32 Int32Val { get; set; }
        public UInt32 UInt32Val { get; set; }
        public Int64 Int64Val { get; set; }
        public UInt64 UInt64Val { get; set; }
        public Single FloatVal { get; set; }
        public Double DoubleVal { get; set; }
        public String? Name { get; set; }
    }

    private class OrderModel
    {
        public Int32 OrderId { get; set; }
        public String? Customer { get; set; }
        public AddressModel? Address { get; set; }
    }

    private class AddressModel
    {
        public String? Province { get; set; }
        public String? City { get; set; }
        public String? Detail { get; set; }
    }

    private class FastMessage : ISpanSerializable
    {
        public Int32 Id { get; set; }
        public String? Action { get; set; }
        public DateTime Timestamp { get; set; }

        public void Write(ref SpanWriter writer)
        {
            writer.Write(Id);
            writer.Write(Action, 0);
            writer.Write(Timestamp.Ticks);
        }

        public void Read(ref SpanReader reader)
        {
            Id = reader.ReadInt32();
            Action = reader.ReadString();
            Timestamp = new DateTime(reader.ReadInt64());
        }
    }

    private class WrapperModel
    {
        public String? Name { get; set; }
        public FastMessage? Message { get; set; }
    }

    private enum MyStatus
    {
        Stopped = 0,
        Running = 1,
        Paused = 2,
    }

    private class EnumModel
    {
        public MyStatus Status { get; set; }
        public Int32 Level { get; set; }
    }

    private class SpecialModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Byte[]? Data { get; set; }
    }

    private class NullableModel
    {
        public Int32? Score { get; set; }
        public Int64? Count { get; set; }
        public DateTime? Time { get; set; }
    }

    private class ExtendedModel
    {
        public SByte SByteVal { get; set; }
        public Char CharVal { get; set; }
        public Decimal DecimalVal { get; set; }
    }

    private struct PointStruct
    {
        public Int32 X { get; set; }
        public Int32 Y { get; set; }
    }

    private class StructHostModel
    {
        public String? Name { get; set; }
        public PointStruct Point { get; set; }
    }

    private class NullableEnumModel
    {
        public MyStatus Status { get; set; }
        public MyStatus? Kind { get; set; }
    }

    /// <summary>跨序列化器兼容模型，覆盖Binary和SpanSerializer共同支持的所有类型</summary>
    private class CompatModel
    {
        public Int32 Code { get; set; }
        public String? Name { get; set; }
        public Boolean Flag { get; set; }
        public Single Score { get; set; }
        public Int64 Count { get; set; }
        public Int16 Level { get; set; }
        public DateTime Time { get; set; }
        public Char Tag { get; set; }
        public Decimal Amount { get; set; }
    }
    #endregion
}
