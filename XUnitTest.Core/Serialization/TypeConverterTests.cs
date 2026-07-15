#if NETCOREAPP
using System.Text.Json;
using System.Text.Json.Serialization;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

/// <summary>类型转换器测试</summary>
public class TypeConverterTests
{
    [Fact(DisplayName = "JsonConverter接口到实现类转换")]
    public void JsonConverterInterfaceToImpl()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonConverter<ITestService, TestService>());

        var json = """{"Name":"Hello","Value":42}""";
        var obj = JsonSerializer.Deserialize<ITestService>(json, options);

        Assert.NotNull(obj);
        Assert.IsType<TestService>(obj);
        var impl = (TestService)obj;
        Assert.Equal("Hello", impl.Name);
        Assert.Equal(42, impl.Value);
    }

    [Fact(DisplayName = "TypeConverter序列化反序列化")]
    public void TypeConverterRoundTrip()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new TypeConverter());

        var type = typeof(String);
        var json = JsonSerializer.Serialize(type, options);
        Assert.Contains("System.String", json);

        var deserialized = JsonSerializer.Deserialize<Type>(json, options);
        Assert.Equal(type, deserialized);
    }

    [Fact(DisplayName = "TypeConverter序列化泛型类型")]
    public void TypeConverterGeneric()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new TypeConverter());

        var type = typeof(List<Int32>);
        var json = JsonSerializer.Serialize(type, options);
        Assert.NotNull(json);

        var deserialized = JsonSerializer.Deserialize<Type>(json, options);
        Assert.Equal(type, deserialized);
    }
}

/// <summary>测试服务接口</summary>
public interface ITestService
{
    String? Name { get; set; }
    Int32 Value { get; set; }
}

/// <summary>测试服务实现</summary>
public class TestService : ITestService
{
    public String? Name { get; set; }
    public Int32 Value { get; set; }
}
#endif
