using System;
using System.Collections.Generic;
using NewLife;
using NewLife.Data;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Data;

public class IPacketEncoderTests
{
    [Fact(DisplayName = "默认编码器-基础类型编码解码")]
    public void DefaultEncoder_BasicTypes()
    {
        var encoder = new DefaultPacketEncoder();

        // 字符串
        var str = "Hello World";
        var packet = encoder.Encode(str);
        Assert.NotNull(packet);
        var decoded = encoder.Decode<String>(packet);
        Assert.Equal(str, decoded);

        // 整数
        var num = 42;
        packet = encoder.Encode(num);
        Assert.NotNull(packet);
        var decodedNum = encoder.Decode<Int32>(packet);
        Assert.Equal(num, decodedNum);

        // 布尔值
        var flag = true;
        packet = encoder.Encode(flag);
        Assert.NotNull(packet);
        var decodedFlag = encoder.Decode<Boolean>(packet);
        Assert.Equal(flag, decodedFlag);
    }

    [Fact(DisplayName = "默认编码器-DateTime类型")]
    public void DefaultEncoder_DateTime()
    {
        var encoder = new DefaultPacketEncoder();
        var dt = new DateTime(2023, 12, 25, 10, 30, 45, 123);
        
        var packet = encoder.Encode(dt);
        Assert.NotNull(packet);
        
        var decoded = encoder.Decode<DateTime>(packet);
        Assert.Equal(dt.ToString("yyyy-MM-dd HH:mm:ss.fff"), decoded.ToString("yyyy-MM-dd HH:mm:ss.fff"));
    }

    [Fact(DisplayName = "默认编码器-null值处理")]
    public void DefaultEncoder_NullValue()
    {
        var encoder = new DefaultPacketEncoder();
        
        // 编码null应返回null
        var packet = encoder.Encode(null);
        Assert.Null(packet);
        
        // 解码空包到可空类型应返回null  
        var emptyPacket = new ArrayPacket([]);
        var decoded = encoder.Decode<Int32?>(emptyPacket);
        Assert.Null(decoded);
    }

    [Fact(DisplayName = "默认编码器-字节数组")]
    public void DefaultEncoder_ByteArray()
    {
        var encoder = new DefaultPacketEncoder();
        var bytes = new Byte[] { 1, 2, 3, 4, 5 };
        
        var packet = encoder.Encode(bytes);
        Assert.NotNull(packet);
        
        var decoded = encoder.Decode<Byte[]>(packet);
        Assert.Equal(bytes, decoded);
    }

    [Fact(DisplayName = "默认编码器-IPacket类型")]
    public void DefaultEncoder_IPacketType()
    {
        var encoder = new DefaultPacketEncoder();
        var originalPacket = new ArrayPacket("test".GetBytes());
        
        // 编码已经是IPacket的对象应直接返回  
        var encoded = encoder.Encode(originalPacket);
        Assert.Equal(originalPacket.Total, encoded!.Total);
        
        // 解码为IPacket类型应直接返回
        var decoded = encoder.Decode<IPacket>(originalPacket);
        Assert.Equal(originalPacket.Total, decoded!.Total);
    }

    [Fact(DisplayName = "默认编码器-复杂对象JSON序列化")]
    public void DefaultEncoder_ComplexObject()
    {
        var encoder = new DefaultPacketEncoder();
        var obj = new { Name = "Test", Value = 123, Flag = true };
        
        var packet = encoder.Encode(obj);
        Assert.NotNull(packet);
        
        // 解码为字典验证JSON序列化
        var decoded = encoder.Decode<Dictionary<String, Object>>(packet);
        Assert.NotNull(decoded);
        Assert.True(decoded.ContainsKey("Name"));
        Assert.Equal("Test", decoded["Name"].ToString());
    }

    [Fact(DisplayName = "默认编码器-异常处理配置")]
    public void DefaultEncoder_ErrorHandling()
    {
        var encoder = new DefaultPacketEncoder { ThrowOnError = false };
        var invalidPacket = new ArrayPacket("invalid_json_for_complex_type".GetBytes());
        
        // 不抛异常，返回null
        var result = encoder.Decode<Dictionary<String, Object>>(invalidPacket);
        Assert.Null(result);
        
        // 配置抛异常
        encoder.ThrowOnError = true;
        Assert.ThrowsAny<Exception>(() => encoder.Decode<Dictionary<String, Object>>(invalidPacket));
    }

    [Fact(DisplayName = "默认编码器-自定义JsonHost")]
    public void DefaultEncoder_CustomJsonHost()
    {
        var customJsonHost = JsonHelper.Default;
        var encoder = new DefaultPacketEncoder { JsonHost = customJsonHost };
        
        Assert.Same(customJsonHost, encoder.JsonHost);
        
        // 验证使用自定义JsonHost的编码解码
        var obj = new { Test = "Value" };
        var packet = encoder.Encode(obj);
        var decoded = encoder.Decode<Dictionary<String, Object>>(packet!);
        
        Assert.NotNull(decoded);
        Assert.Equal("Value", decoded["Test"].ToString());
    }

    [Fact(DisplayName = "扩展方法-泛型Decode")]
    public void ExtensionMethod_GenericDecode()
    {
        var encoder = new DefaultPacketEncoder();
        var value = "test string";
        var packet = encoder.Encode(value);
        
        // 测试扩展方法
        var decoded = encoder.Decode<String>(packet!);
        Assert.Equal(value, decoded);
    }

    [Fact(DisplayName = "默认编码器-Packet类型转换")]
    public void DefaultEncoder_PacketTypeConversion()
    {
        var encoder = new DefaultPacketEncoder();
        var arrayPacket = new ArrayPacket("test".GetBytes());
        
        // 解码为Packet类型
        var packet = encoder.Decode<Packet>(arrayPacket);
        Assert.NotNull(packet);
        Assert.Equal("test", packet.ToStr());
    }

    [Fact(DisplayName = "默认编码器-基础类型字符串转换")]
    public void DefaultEncoder_BaseTypeStringConversion()
    {
        var encoder = new DefaultPacketEncoder();
        
        // 测试各种基础类型的字符串编码解码
        var testCases = new Dictionary<Object, Type>
        {
            { (Byte)255, typeof(Byte) },
            { (Int16)32767, typeof(Int16) },
            { (UInt16)65535, typeof(UInt16) },
            { (UInt32)4294967295, typeof(UInt32) },
            { (Int64)9223372036854775807, typeof(Int64) },
            { (Single)3.14f, typeof(Single) },
            { (Double)3.14159, typeof(Double) },
            { (Decimal)123.456m, typeof(Decimal) }
        };

        foreach (var (value, type) in testCases)
        {
            var packet = encoder.Encode(value);
            Assert.NotNull(packet);
            
            var decoded = encoder.Decode(packet, type);
            Assert.NotNull(decoded);
            
            // 对于浮点数，使用字符串比较避免精度问题
            if (type == typeof(Single) || type == typeof(Double))
            {
                Assert.Equal(value.ToString(), decoded.ToString());
            }
            else
            {
                Assert.Equal(value, decoded);
            }
        }
    }
}