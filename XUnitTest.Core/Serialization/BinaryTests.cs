using NewLife;
using NewLife.Data;
using NewLife.Serialization;
using NewLife.Serialization.Interface;
using Xunit;

namespace XUnitTest.Serialization;

public class BinaryTests
{
    [Fact]
    public void Normal()
    {
        var model = new MyModel { Code = 1234, Name = "Stone" };
        var bn = new Binary();
        Assert.Equal(5, bn.Handlers.Count);

        bn.Write(model);

        var pk = bn.GetPacket();
        Assert.Equal(10, pk.Length);
        Assert.Equal("000004D20553746F6E65", pk.ToHex());
        Assert.Equal(bn.Total, pk.Length);

        var bn2 = new Binary { Stream = pk.GetStream() };
        var model2 = bn2.Read<MyModel>();
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(bn2.Total, pk.Length);
    }

    [Fact]
    public void EncodeInt()
    {
        var model = new MyModel { Code = 1234, Name = "Stone" };
        var bn = new Binary { EncodeInt = true, };
        Assert.Equal(5, bn.Handlers.Count);

        bn.Write(model);

        var pk = bn.GetPacket();
        Assert.Equal(8, pk.Length);
        Assert.Equal("D2090553746F6E65", pk.ToHex());
        Assert.Equal(bn.Total, pk.Length);

        var bn2 = new Binary { Stream = pk.GetStream(), EncodeInt = true };
        var model2 = bn2.Read<MyModel>();
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(bn2.Total, pk.Length);
    }

    [Fact]
    public void IsLittleEndian()
    {
        var model = new MyModel { Code = 1234, Name = "Stone" };
        var bn = new Binary { IsLittleEndian = true, };
        Assert.Equal(5, bn.Handlers.Count);

        bn.Write(model);

        var pk = bn.GetPacket();
        Assert.Equal(10, pk.Length);
        Assert.Equal("D20400000553746F6E65", pk.ToHex());
        Assert.Equal(bn.Total, pk.Length);

        var bn2 = new Binary { Stream = pk.GetStream(), IsLittleEndian = true };
        var model2 = bn2.Read<MyModel>();
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(bn2.Total, pk.Length);
    }

    [Fact]
    public void UseFieldSize()
    {
        var model = new MyModelWithFieldSize { Name = "Stone" };
        var bn = new Binary { UseFieldSize = true, };
        Assert.Equal(5, bn.Handlers.Count);

        bn.Write(model);

        var pk = bn.GetPacket();
        Assert.Equal(6, pk.Length);
        Assert.Equal("0553746F6E65", pk.ToHex());
        Assert.Equal(bn.Total, pk.Length);

        var bn2 = new Binary { Stream = pk.GetStream(), UseFieldSize = true };
        var model2 = bn2.Read<MyModelWithFieldSize>();
        Assert.Equal(model.Length, model2.Length);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(bn2.Total, pk.Length);
    }

    [Fact]
    public void SizeWidth()
    {
        var model = new MyModel { Code = 1234, Name = "Stone" };
        var bn = new Binary { SizeWidth = 2, };
        Assert.Equal(5, bn.Handlers.Count);

        bn.Write(model);

        var pk = bn.GetPacket();
        Assert.Equal(11, pk.Length);
        Assert.Equal("000004D2000553746F6E65", pk.ToHex());
        Assert.Equal(bn.Total, pk.Length);

        var bn2 = new Binary { Stream = pk.GetStream(), SizeWidth = 2 };
        var model2 = bn2.Read<MyModel>();
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(bn2.Total, pk.Length);
    }

    [Fact]
    public void UseProperty()
    {
        var model = new MyModel { Code = 1234, Name = "Stone" };
        var bn = new Binary { UseProperty = false, };
        Assert.Equal(5, bn.Handlers.Count);

        bn.Write(model);

        var pk = bn.GetPacket();
        Assert.Equal(10, pk.Length);
        Assert.Equal("000004D20553746F6E65", pk.ToHex());
        Assert.Equal(bn.Total, pk.Length);

        var bn2 = new Binary { Stream = pk.GetStream(), UseProperty = false };
        var model2 = bn2.Read<MyModel>();
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(bn2.Total, pk.Length);
    }

    [Fact]
    public void IgnoreMembers()
    {
        var model = new MyModel { Code = 1234, Name = "Stone" };
        var bn = new Binary();
        bn.IgnoreMembers.Add("Code");
        Assert.Equal(5, bn.Handlers.Count);

        bn.Write(model);

        var pk = bn.GetPacket();
        Assert.Equal(6, pk.Length);
        Assert.Equal("0553746F6E65", pk.ToHex());
        Assert.Equal(bn.Total, pk.Length);

        var bn2 = new Binary { Stream = pk.GetStream() };
        bn2.IgnoreMembers.Add("Code");
        var model2 = bn2.Read<MyModel>();
        Assert.Equal(0, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(bn2.Total, pk.Length);
    }

    [Fact]
    public void Fast()
    {
        var model = new MyModel { Code = 1234, Name = "Stone" };
        var pk = Binary.FastWrite(model);
        Assert.Equal(8, pk.Length);
        Assert.Equal("D2090553746F6E65", pk.ToHex());
        Assert.Equal("0gkFU3RvbmU=", pk.ToArray().ToBase64());

        var model2 = Binary.FastRead<MyModel>(pk.GetStream());
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);

        var ms = new MemoryStream();
        var total = Binary.FastWrite(model, ms);
        Assert.Equal("D2090553746F6E65", ms.ToArray().ToHex());
        Assert.Equal(total, ms.Length);
    }

    private class MyModel
    {
        public Int32 Code { get; set; }

        public String Name { get; set; }
    }

    [Fact]
    public void Accessor()
    {
        var model = new MyModelWithAccessor { Code = 1234, Name = "Stone" };
        var pk = Binary.FastWrite(model);
        Assert.Equal(10, pk.Length);
        Assert.Equal("D20400000553746F6E65", pk.ToHex());
        Assert.Equal("0gQAAAVTdG9uZQ==", pk.ToArray().ToBase64());

        var model2 = Binary.FastRead<MyModelWithAccessor>(pk.GetStream());
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);

        var ms = new MemoryStream();
        var total = Binary.FastWrite(model, ms);
        Assert.Equal("D20400000553746F6E65", ms.ToArray().ToHex());
        Assert.Equal(total, ms.Length);
    }

    private class MyModelWithAccessor : IAccessor
    {
        public Int32 Code { get; set; }

        public String Name { get; set; }

        public Boolean Read(Stream stream, Object context)
        {
            var reader = new BinaryReader(stream);
            Code = reader.ReadInt32();
            Name = reader.ReadString();

            if (context is Binary bn)
            {
                bn.Total += 4 + 1 + Name.GetBytes(bn.Encoding).Length;
            }

            return true;
        }

        public Boolean Write(Stream stream, Object context)
        {
            var writer = new BinaryWriter(stream);
            writer.Write(Code);
            writer.Write(Name);

            if (context is Binary bn)
            {
                bn.Total += 4 + 1 + Name.GetBytes(bn.Encoding).Length;
            }

            return true;
        }
    }

    [Fact]
    public void MemberAccessor()
    {
        var model = new MyModelWithMemberAccessor { Code = 1234, Name = "Stone" };
        var pk = Binary.FastWrite(model);
        Assert.Equal(8, pk.Length);
        Assert.Equal("D2090553746F6E65", pk.ToHex());
        Assert.Equal("0gkFU3RvbmU=", pk.ToArray().ToBase64());

        var model2 = Binary.FastRead<MyModelWithMemberAccessor>(pk.GetStream());
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);

        var ms = new MemoryStream();
        var total = Binary.FastWrite(model, ms);
        Assert.Equal("D2090553746F6E65", ms.ToArray().ToHex());
        Assert.Equal(total, ms.Length);
    }

    private class MyModelWithMemberAccessor : IMemberAccessor
    {
        public Int32 Code { get; set; }

        public String Name { get; set; }

        public Boolean Read(IFormatterX formatter, AccessorContext context)
        {
            var bn = formatter as Binary;

            switch (context.Member.Name)
            {
                case "Code": Code = bn.Read<Int32>(); break;
                case "Name": Name = bn.Read<String>(); break;
            }
            //Code = bn.Read<Int32>();
            //Name = bn.Read<String>();

            return true;
        }
        public Boolean Write(IFormatterX formatter, AccessorContext context)
        {
            var bn = formatter as Binary;

            switch (context.Member.Name)
            {
                case "Code": bn.Write(Code); break;
                case "Name": bn.Write(Name); break;
            }

            return true;
        }
    }

    [Fact]
    public void FieldSize()
    {
        var model = new MyModelWithFieldSize { Name = "Stone" };
        Assert.Equal(0, model.Length);

        var bn = new Binary { EncodeInt = true, UseFieldSize = true };
        bn.Write(model);
        var pk = new Packet(bn.GetBytes());
        Assert.Equal(5, model.Length);
        Assert.Equal(6, pk.Total);
        Assert.Equal("0553746F6E65", pk.ToHex());
        Assert.Equal(bn.Total, pk.Total);

        var bn2 = new Binary { Stream = pk.GetStream(), EncodeInt = true, UseFieldSize = true };
        var model2 = bn2.Read<MyModelWithFieldSize>();
        Assert.Equal(model.Length, model2.Length);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(bn2.Total, pk.Total);
    }

    private class MyModelWithFieldSize
    {
        public Byte Length { get; set; }

        [FieldSize(nameof(Length))]
        public String Name { get; set; }
    }

    [Fact]
    public void MemberAccessorAtt()
    {
        var model = new MyModelWithMemberAccessorAtt { Code = 1234, Name = "Stone" };
        var pk = Binary.FastWrite(model);
        Assert.Equal(8, pk.Length);
        Assert.Equal("D2090553746F6E65", pk.ToHex());

        var model2 = Binary.FastRead<MyModelWithMemberAccessorAtt>(pk.GetStream());
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
    }

    private class MyModelWithMemberAccessorAtt
    {
        public Int32 Code { get; set; }

        [MyAccessor]
        public String Name { get; set; }
    }

    private class MyAccessorAttribute : AccessorAttribute
    {
        public override Boolean Read(IFormatterX formatter, AccessorContext context)
        {
            Assert.Equal("Name", context.Member.Name);

            var v = context.Value as MyModelWithMemberAccessorAtt;

            var bn = formatter as Binary;
            v.Name = bn.Read<String>();

            return true;
        }

        public override Boolean Write(IFormatterX formatter, AccessorContext context)
        {
            Assert.Equal("Name", context.Member.Name);

            var v = context.Value as MyModelWithMemberAccessorAtt;

            var bn = formatter as Binary;
            bn.Write(v.Name);

            return true;
        }
    }

    [Fact]
    public void FixedString()
    {
        var model = new MyModelWithFixed { Code = 1234, Name = "Stone" };

        var bn = new Binary { EncodeInt = true };
        bn.Write(model);
        var pk = bn.GetPacket();
        Assert.Equal(10, pk.Length);
        Assert.Equal("D20953746F6E65000000", pk.ToHex());
        Assert.Equal(bn.Total, pk.Total);

        var bn2 = new Binary { Stream = pk.GetStream(), EncodeInt = true };
        var model2 = bn2.Read<MyModelWithFixed>();
        Assert.Equal(model.Code, model2.Code);
        Assert.Equal(model.Name, model2.Name);
        Assert.Equal(bn2.Total, pk.Total);
    }

    private class MyModelWithFixed
    {
        public Int32 Code { get; set; }

        [FixedString(8)]
        public String Name { get; set; }
    }

    [Fact]
    public void ReadDateTime()
    {
        var dt = DateTime.Now;
        var n1 = dt.ToInt();

        // 默认压缩整数，反而用了5字节
        var pk = Binary.FastWrite(dt);
        Assert.Equal(5, pk.Length);

        var dt2 = Binary.FastRead<DateTime>(pk.GetStream());
        var n2 = dt2.ToInt();

        Assert.Equal(dt.Trim(), dt2);
        Assert.Equal(n1, n2);

        // 不用压缩整数，只要4字节
        var bn = new Binary { EncodeInt = false };
        bn.Write(dt);
        pk = bn.GetPacket();
        Assert.Equal(4, pk.Length);
        Assert.Equal(bn.Total, pk.Total);

        bn = new Binary { EncodeInt = false, Stream = pk.GetStream() };
        dt2 = bn.Read<DateTime>();
        n2 = dt2.ToInt();

        Assert.Equal(dt.Trim(), dt2);
        Assert.Equal(n1, n2);
        Assert.Equal(bn.Total, pk.Total);
    }

    [Fact]
    public void ReadDateTime2()
    {
        // 刚好超过Int32.MaxValue，最高可达UInt32.MaxValue
        var dt = new DateTime(2038, 12, 31);
        var n1 = dt.ToLong();

        var pk = Binary.FastWrite(dt);
        Assert.Equal(5, pk.Length);

        var dt2 = Binary.FastRead<DateTime>(pk.GetStream());
        var n2 = dt2.ToLong();

        Assert.Equal(dt, dt2);
        Assert.Equal(n1, n2);
    }

    [Fact]
    public void WriteMinTime()
    {
        var dt = DateTime.MinValue;
        var n1 = dt.ToLong();

        var pk = Binary.FastWrite(dt);
        Assert.Equal(1, pk.Length);

        var dt2 = Binary.FastRead<DateTime>(pk.GetStream());
        var n2 = dt2.ToLong();

        Assert.Equal(dt, dt2);
        Assert.Equal(n1, n2);
    }

    [Fact]
    public void WriteMaxTime()
    {
        // 刚好超过UInt32.MaxValue
        var dt = new DateTime(2106, 12, 31);
        var n1 = dt.ToLong();

        var ex = Assert.Throws<InvalidDataException>(() => Binary.FastWrite(dt));
    }

    [Theory]
    [InlineData("0001-01-01 00:00:00")]
    [InlineData("9999-12-31 23:59:59")]
    [InlineData("9999-12-31 23:59:59.9999999")]
    [InlineData("2024-07-01")]
    [InlineData("1970-01-01")]
    [InlineData("2038-12-31")]
    [InlineData("2106-12-31")]
    public void WriteFullTime(String str)
    {
        var dt = str.ToDateTime();
        var n1 = dt.ToLong();

        // 使用完整时间序列化
        var bn = new Binary { FullTime = true };
        bn.Write(dt);
        var pk = bn.GetPacket();
        Assert.Equal(8, pk.Length);
        Assert.Equal(bn.Total, pk.Total);

        // 反序列化
        bn = new Binary { FullTime = true, Stream = pk.GetStream() };
        var dt2 = bn.Read<DateTime>();
        var n2 = dt2.ToLong();

        Assert.Equal(dt, dt2);
        Assert.Equal(n1, n2);
        Assert.Equal(bn.Total, pk.Total);
    }

    private static void WriteFullTimeByKind(DateTime dt)
    {
        var n1 = dt.ToLong();

        // 使用完整时间序列化
        var bn = new Binary { FullTime = true };
        bn.Write(dt);
        var pk = bn.GetPacket();
        Assert.Equal(8, pk.Length);
        Assert.Equal(bn.Total, pk.Total);

        // 反序列化
        bn = new Binary { FullTime = true, Stream = pk.GetStream() };
        var dt2 = bn.Read<DateTime>();
        var n2 = dt2.ToLong();

        Assert.Equal(dt.Kind, dt2.Kind);
        Assert.Equal(dt, dt2);
        Assert.Equal(n1, n2);
        Assert.Equal(bn.Total, pk.Total);
    }

    [Fact]
    public void WriteFullTimeByLocal()
    {
        var dt = DateTime.Now;
        WriteFullTimeByKind(dt);

        dt = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Local);
        WriteFullTimeByKind(dt);

        dt = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Local);
        WriteFullTimeByKind(dt);
    }

    [Fact]
    public void WriteFullTimeByUTC()
    {
        var dt = DateTime.UtcNow;
        WriteFullTimeByKind(dt);

        dt = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        WriteFullTimeByKind(dt);

        dt = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        WriteFullTimeByKind(dt);
    }

    [Fact]
    public void WriteFullTimeByUnspecified()
    {
        var dt = new DateTime(2106, 12, 31);
        WriteFullTimeByKind(dt);

        dt = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        WriteFullTimeByKind(dt);

        dt = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Unspecified);
        WriteFullTimeByKind(dt);

        dt = DateTime.MinValue;
        WriteFullTimeByKind(dt);

        dt = DateTime.MaxValue;
        WriteFullTimeByKind(dt);
    }
}