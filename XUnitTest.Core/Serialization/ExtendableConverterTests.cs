using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewLife.Data;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

/// <summary>ExtendableConverter 单元测试</summary>
public class ExtendableConverterTests
{
    #region 测试模型

    class SimpleModel : IExtend
    {
        public String Name { get; set; } = String.Empty;
        public Int32 Age { get; set; }
        public IDictionary<String, Object?> Items { get; } = new Dictionary<String, Object?>();
        public Object? this[String key] { get => Items.TryGetValue(key, out var v) ? v : null; set => Items[key] = value; }
    }

    class ReadOnlyPropModel : IExtend
    {
        public String Name { get; set; } = String.Empty;
        public String ReadOnly => "fixed";
        public IDictionary<String, Object?> Items { get; } = new Dictionary<String, Object?>();
        public Object? this[String key] { get => Items.TryGetValue(key, out var v) ? v : null; set => Items[key] = value; }
    }

    class AddressModel
    {
        public String City { get; set; } = String.Empty;
        public String Street { get; set; } = String.Empty;
    }

    class ComplexModel : IExtend
    {
        public String Name { get; set; } = String.Empty;
        public AddressModel? Address { get; set; }
        public IDictionary<String, Object?> Items { get; } = new Dictionary<String, Object?>();
        public Object? this[String key] { get => Items.TryGetValue(key, out var v) ? v : null; set => Items[key] = value; }
    }

    #endregion

    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new ExtendableConverter() }
    };

    #region CanConvert

    [Fact(DisplayName = "CanConvert 对 IExtend 实现类返回 true")]
    public void CanConvert_IExtendType_ReturnsTrue()
    {
        var converter = new ExtendableConverter();
        Assert.True(converter.CanConvert(typeof(SimpleModel)));
        Assert.True(converter.CanConvert(typeof(ComplexModel)));
    }

    [Fact(DisplayName = "CanConvert 对非 IExtend 类返回 false")]
    public void CanConvert_NonIExtendType_ReturnsFalse()
    {
        var converter = new ExtendableConverter();
        Assert.False(converter.CanConvert(typeof(String)));
        Assert.False(converter.CanConvert(typeof(AddressModel)));
        Assert.False(converter.CanConvert(typeof(Object)));
    }

    #endregion

    #region Write

    [Fact(DisplayName = "Write 正确输出基础属性")]
    public void Write_BasicProperties_Serialized()
    {
        var model = new SimpleModel { Name = "Alice", Age = 30 };
        var json = JsonSerializer.Serialize(model, _options);

        Assert.Contains("\"Name\"", json);
        Assert.Contains("\"Alice\"", json);
        Assert.Contains("\"Age\"", json);
        Assert.Contains("30", json);
    }

    [Fact(DisplayName = "Write 不将 Items 属性本身输出为嵌套对象")]
    public void Write_ItemsPropertyItself_NotIncluded()
    {
        var model = new SimpleModel { Name = "Alice" };
        model.Items["extra"] = "value";

        var json = JsonSerializer.Serialize(model, _options);
        var normalized = json.Replace(" ", "");

        // Items 属性本身不应出现为 key，扩展字段应平铺
        Assert.DoesNotContain("\"Items\":{", normalized);
    }

    [Fact(DisplayName = "Write 将 IExtend.Items 平铺到 JSON 根层级")]
    public void Write_ExtendItems_SpreadAtRootLevel()
    {
        var model = new SimpleModel { Name = "Alice" };
        model.Items["City"] = "Shanghai";
        model.Items["Score"] = 99;

        var json = JsonSerializer.Serialize(model, _options);

        Assert.Contains("\"City\"", json);
        Assert.Contains("\"Shanghai\"", json);
        Assert.Contains("\"Score\"", json);
        Assert.Contains("99", json);
        Assert.DoesNotContain("\"Items\":{", json.Replace(" ", ""));
    }

    [Fact(DisplayName = "Write 正确序列化复杂嵌套对象属性")]
    public void Write_ComplexNestedProperty_SerializedCorrectly()
    {
        var model = new ComplexModel
        {
            Name = "Bob",
            Address = new AddressModel { City = "Beijing", Street = "Main St" }
        };

        var json = JsonSerializer.Serialize(model, _options);

        // 复杂属性应被完整序列化，而不是输出 {}
        Assert.Contains("\"City\"", json);
        Assert.Contains("\"Beijing\"", json);
        Assert.Contains("\"Street\"", json);
        Assert.Contains("\"Main St\"", json);
    }

    [Fact(DisplayName = "Write 属性值为 null 时输出 null")]
    public void Write_NullPropertyValue_WrittenAsNull()
    {
        var model = new ComplexModel { Name = "Bob", Address = null };
        var json = JsonSerializer.Serialize(model, _options);

        Assert.Contains("\"Address\":null", json.Replace(" ", ""));
    }

    [Fact(DisplayName = "Write WhenWritingDefault 跳过 null 和默认值属性")]
    public void Write_WhenWritingDefault_SkipsDefaultValues()
    {
        var opts = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Converters = { new ExtendableConverter() }
        };

        // Name 为空字符串（非 null，不是默认值）、Age 为 0（默认值）、Address 为 null（默认值）
        var model = new ComplexModel { Name = "Bob", Address = null };
        model.Items["extra"] = "value";
        model.Items["empty"] = null;

        var json = JsonSerializer.Serialize(model, opts);

        // Name 有值应保留
        Assert.Contains("\"Name\"", json);
        // Address 为 null（引用类型默认值）应跳过
        Assert.DoesNotContain("\"Address\"", json);
        // Items 中有值的保留
        Assert.Contains("\"extra\"", json);
        // Items 中 null 值应跳过
        Assert.DoesNotContain("\"empty\"", json);
    }

    [Fact(DisplayName = "Write WhenWritingDefault 跳过值类型默认值")]
    public void Write_WhenWritingDefault_SkipsValueTypeDefaults()
    {
        var opts = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Converters = { new ExtendableConverter() }
        };

        var model = new SimpleModel { Name = "Alice", Age = 0 };

        var json = JsonSerializer.Serialize(model, opts);

        // Name 有值应保留
        Assert.Contains("\"Name\"", json);
        // Age 为 0（Int32 默认值）应跳过
        Assert.DoesNotContain("\"Age\"", json);
    }

    [Fact(DisplayName = "Write WhenWritingNull 仅跳过 null 不跳过值类型默认值")]
    public void Write_WhenWritingNull_SkipsOnlyNull()
    {
        var opts = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new ExtendableConverter() }
        };

        var model = new ComplexModel { Name = "Bob", Address = null };

        var json = JsonSerializer.Serialize(model, opts);

        // Name 有值应保留
        Assert.Contains("\"Name\"", json);
        // Address 为 null 应跳过
        Assert.DoesNotContain("\"Address\"", json);
    }

    [Fact(DisplayName = "Write 遵循 PropertyNamingPolicy 命名策略")]
    public void Write_NamingPolicy_Applied()
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new ExtendableConverter() }
        };
        var model = new SimpleModel { Name = "Alice", Age = 25 };
        var json = JsonSerializer.Serialize(model, opts);

        Assert.Contains("\"name\"", json);
        Assert.Contains("\"age\"", json);
        Assert.DoesNotContain("\"Name\"", json);
        Assert.DoesNotContain("\"Age\"", json);
    }

    [Fact(DisplayName = "Write 对非 IExtend 对象使用默认序列化")]
    public void Write_NonIExtendValue_DefaultSerialization()
    {
        var converter = new ExtendableConverter();
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var addr = new AddressModel { City = "Hangzhou", Street = "West Lake" };
        converter.Write(writer, addr, JsonSerializerOptions.Default);
        writer.Flush();

        var json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("\"City\"", json);
        Assert.Contains("\"Hangzhou\"", json);
        Assert.Contains("\"West Lake\"", json);
    }

    #endregion

    #region Read

    [Fact(DisplayName = "Read 已知属性映射到成员，Items 为空")]
    public void Read_KnownProperties_MappedToMembers()
    {
        var json = "{\"Name\":\"Alice\",\"Age\":30}";
        var model = JsonSerializer.Deserialize<SimpleModel>(json, _options);

        Assert.NotNull(model);
        Assert.Equal("Alice", model.Name);
        Assert.Equal(30, model.Age);
        Assert.Empty(model.Items);
    }

    [Fact(DisplayName = "Read 未知字段写入 IExtend.Items")]
    public void Read_UnknownFields_GoToItems()
    {
        var json = "{\"Name\":\"Alice\",\"City\":\"Shanghai\",\"Score\":99}";
        var model = JsonSerializer.Deserialize<SimpleModel>(json, _options);

        Assert.NotNull(model);
        Assert.Equal("Alice", model.Name);
        Assert.Equal(2, model.Items.Count);

        var city = Assert.IsType<JsonElement>(model.Items["City"]);
        Assert.Equal("Shanghai", city.GetString());

        var score = Assert.IsType<JsonElement>(model.Items["Score"]);
        Assert.Equal(99, score.GetInt32());
    }

    [Fact(DisplayName = "Read 已知与未知字段混合时分别处理")]
    public void Read_Mixed_KnownAndUnknownFields()
    {
        var json = "{\"Name\":\"Bob\",\"Age\":25,\"Tag\":\"vip\",\"Level\":3}";
        var model = JsonSerializer.Deserialize<SimpleModel>(json, _options);

        Assert.NotNull(model);
        Assert.Equal("Bob", model.Name);
        Assert.Equal(25, model.Age);
        Assert.Equal(2, model.Items.Count);
        Assert.True(model.Items.ContainsKey("Tag"));
        Assert.True(model.Items.ContainsKey("Level"));
    }

    [Fact(DisplayName = "Read 只读属性无法赋值，对应字段写入 Items")]
    public void Read_ReadOnlyProperty_GoesToItems()
    {
        var json = "{\"Name\":\"Alice\",\"ReadOnly\":\"ignored\"}";
        var model = JsonSerializer.Deserialize<ReadOnlyPropModel>(json, _options);

        Assert.NotNull(model);
        Assert.Equal("Alice", model.Name);
        Assert.Equal("fixed", model.ReadOnly);  // 原值未被覆盖

        Assert.True(model.Items.ContainsKey("ReadOnly"));
        var val = Assert.IsType<JsonElement>(model.Items["ReadOnly"]);
        Assert.Equal("ignored", val.GetString());
    }

    [Fact(DisplayName = "Read 支持 CamelCase 命名策略匹配 PascalCase 属性")]
    public void Read_CamelCaseNamingPolicy_MatchesPascalCaseProperty()
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new ExtendableConverter() }
        };
        var json = "{\"name\":\"Alice\",\"age\":30}";
        var model = JsonSerializer.Deserialize<SimpleModel>(json, opts);

        Assert.NotNull(model);
        Assert.Equal("Alice", model.Name);
        Assert.Equal(30, model.Age);
        Assert.Empty(model.Items);
    }

    [Fact(DisplayName = "Read 支持大小写不敏感匹配属性")]
    public void Read_CaseInsensitive_MatchesProperty()
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new ExtendableConverter() }
        };
        var json = "{\"NAME\":\"Alice\",\"age\":30}";
        var model = JsonSerializer.Deserialize<SimpleModel>(json, opts);

        Assert.NotNull(model);
        Assert.Equal("Alice", model.Name);
        Assert.Equal(30, model.Age);
        Assert.Empty(model.Items);
    }

    [Fact(DisplayName = "Read 对非 IExtend 类型委托给默认反序列化")]
    public void Read_NonIExtendType_DefaultDeserialization()
    {
        var converter = new ExtendableConverter();
        var json = "{\"City\":\"Hangzhou\",\"Street\":\"West Lake\"}";
        var bytes = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(bytes);
        reader.Read();

        var result = converter.Read(ref reader, typeof(AddressModel), new JsonSerializerOptions()) as AddressModel;

        Assert.NotNull(result);
        Assert.Equal("Hangzhou", result.City);
        Assert.Equal("West Lake", result.Street);
    }

    #endregion

    #region 往返序列化

    [Fact(DisplayName = "往返序列化保留所有基础属性和扩展字段")]
    public void RoundTrip_AllDataPreserved()
    {
        var original = new SimpleModel { Name = "Alice", Age = 30 };
        original.Items["Tag"] = "vip";
        original.Items["Level"] = 5;

        var json = JsonSerializer.Serialize(original, _options);
        var restored = JsonSerializer.Deserialize<SimpleModel>(json, _options);

        Assert.NotNull(restored);
        Assert.Equal("Alice", restored.Name);
        Assert.Equal(30, restored.Age);

        var tag = Assert.IsType<JsonElement>(restored.Items["Tag"]);
        Assert.Equal("vip", tag.GetString());

        var level = Assert.IsType<JsonElement>(restored.Items["Level"]);
        Assert.Equal(5, level.GetInt32());
    }

    [Fact(DisplayName = "往返序列化复杂属性与扩展字段共存")]
    public void RoundTrip_ComplexPropertyAndExtendItems_Coexist()
    {
        var original = new ComplexModel
        {
            Name = "Bob",
            Address = new AddressModel { City = "Beijing", Street = "Main St" }
        };
        original.Items["Flag"] = true;

        var json = JsonSerializer.Serialize(original, _options);
        var restored = JsonSerializer.Deserialize<ComplexModel>(json, _options);

        Assert.NotNull(restored);
        Assert.Equal("Bob", restored.Name);
        Assert.NotNull(restored.Address);
        Assert.Equal("Beijing", restored.Address.City);

        Assert.True(restored.Items.ContainsKey("Flag"));
    }

    #endregion
}
