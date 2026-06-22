using System.ComponentModel;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

public class JsonWriterTests
{
    [Fact]
    public void Utc_Time()
    {
        var writer = new JsonWriter();

        var dt = DateTime.UtcNow;
        writer.Write(new { time = dt });

        var str = writer.GetString();
        Assert.NotEmpty(str);

        var js = new JsonParser(str);
        var dic = js.Decode() as IDictionary<String, Object>;
        Assert.NotNull(dic);

        var str2 = dic["time"];
        Assert.EndsWith("Z", str2 + "");
        Assert.Equal(dt.ToString("yyyy-MM-ddTHH:mm:ss") + "Z", str2);

        var dt2 = dic["time"].ToDateTime();
        Assert.Equal(DateTimeKind.Utc, dt2.Kind);
        Assert.Equal(dt.Trim(), dt2.Trim());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseUtc_Setting(Boolean useUTCDateTime)
    {
        var writer = new JsonWriter { UseUTCDateTime = useUTCDateTime };

        var dt = DateTime.Now;
        writer.Write(new { time = dt });

        var str = writer.GetString();
        Assert.NotEmpty(str);

        var js = new JsonParser(str);
        var dic = js.Decode() as IDictionary<String, Object>;
        Assert.NotNull(dic);

        if (useUTCDateTime)
        {
            var str2 = dic["time"];
            Assert.EndsWith("Z", str2 + "");
            Assert.Equal(dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss") + "Z", str2);

            var dt2 = dic["time"].ToDateTime();
            Assert.Equal(DateTimeKind.Utc, dt2.Kind);
            Assert.Equal(dt.ToUniversalTime().Trim(), dt2.Trim());
        }
        else
        {
            var str2 = dic["time"];
            Assert.False((str2 + "").EndsWith("Z"));
            Assert.Equal(dt.ToString("yyyy-MM-ddTHH:mm:ss"), str2);

            var dt2 = dic["time"].ToDateTime();
            Assert.NotEqual(DateTimeKind.Utc, dt2.Kind);
            Assert.Equal(dt.Trim(), dt2.Trim());
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FullTime_Setting(Boolean fullTime)
    {
        var writer = new JsonWriter();
        writer.Options.FullTime = fullTime;

        var dt = DateTime.Now;
        writer.Write(new { time = dt });

        var str = writer.GetString();
        Assert.NotEmpty(str);

        var js = new JsonParser(str);
        var dic = js.Decode() as IDictionary<String, Object>;
        Assert.NotNull(dic);

        if (fullTime)
        {
            Assert.Contains("T", str);
            Assert.Contains("+", str);

            var dto = DateTimeOffset.Now;

            var str2 = dic["time"];
            // +08:00
            Assert.EndsWith(dto.Offset.ToString(), str2 + ":00");
            Assert.Equal($"{dt:yyyy-MM-ddTHH:mm:ss.fffffff}+{dto.Offset.Hours:00}:00", str2);

            var dt2 = dic["time"].ToDateTime();
            Assert.Equal(DateTimeKind.Local, dt2.Kind);
            Assert.Equal(dt.Trim(), dt2.Trim());
        }
        else
        {
            var str2 = dic["time"];
            Assert.False((str2 + "").EndsWith("Z"));
            Assert.Equal(dt.ToString("yyyy-MM-ddTHH:mm:ss"), str2);

            var dt2 = dic["time"].ToDateTime();
            Assert.Equal(DateTimeKind.Unspecified, dt2.Kind);
            Assert.Equal(dt.Trim(), dt2.Trim());
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LowerCase_Setting(Boolean lowerCase)
    {
        var writer = new JsonWriter { LowerCase = lowerCase };

        writer.Write(new { UserName = "Stone" });

        var str = writer.GetString();
        var js = new JsonParser(str);
        var dic = js.Decode() as IDictionary<String, Object>;

        var key = dic.Keys.First();
        if (lowerCase)
            Assert.Equal("username", key);
        else
            Assert.Equal("UserName", key);
        Assert.Equal("Stone", dic["UserName"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CamelCase_Setting(Boolean camelCase)
    {
        var writer = new JsonWriter();
        writer.Options.CamelCase = camelCase;

        writer.Write(new { UserName = "Stone" });

        var str = writer.GetString();
        var js = new JsonParser(str);
        var dic = js.Decode() as IDictionary<String, Object>;

        var key = dic.Keys.First();
        if (camelCase)
            Assert.Equal("userName", key);
        else
            Assert.Equal("UserName", key);
        Assert.Equal("Stone", dic["UserName"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IgnoreNullValues_Setting(Boolean ignoreNullValues)
    {
        var writer = new JsonWriter();
        writer.Options.IgnoreNullValues = ignoreNullValues;

        writer.Write(new { Name = "", UserName = "Stone" });

        var str = writer.GetString();
        var js = new JsonParser(str);
        var dic = js.Decode() as IDictionary<String, Object>;

        var key = dic.Keys.First();
        if (ignoreNullValues)
            Assert.Equal("UserName", key);
        else
            Assert.Equal("Name", key);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IgnoreReadOnlyProperties_Setting(Boolean ignoreReadOnlyProperties)
    {
        var writer = new JsonWriter { IgnoreReadOnlyProperties = ignoreReadOnlyProperties };

        writer.Write(new Model("Stone", "PPP"));

        var str = writer.GetString();
        var js = new JsonParser(str);
        var dic = js.Decode() as IDictionary<String, Object>;

        var key = dic.Keys.Last();
        if (ignoreReadOnlyProperties)
            Assert.Equal("Name", key);
        else
            Assert.Equal("Password", key);
    }

    class Model(String name, String pass)
    {
        public String Name { get; set; } = name;
        public String Password { get; } = pass;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnumTest(Boolean enumString)
    {
        // 字符串
        var writer = new JsonWriter();
        writer.Options.EnumString = enumString;

        var data = new { Level = LogLevel.Fatal };
        writer.Write(data);

        var js = new JsonParser(writer.GetString());
        var dic = js.Decode() as IDictionary<String, Object>;
        Assert.NotNull(dic);

        if (enumString)
            Assert.Equal("Fatal", dic["Level"]);
        else
        {
            Assert.Equal(5, dic["Level"]);
            Assert.Equal((Int32)LogLevel.Fatal, dic["Level"].ToInt());
        }
    }

    [Fact]
    public void ArrayTest()
    {
        var arr = new[] { 12, 34, 56, 78 };
        var str = JsonWriter.ToJson(arr);
        Assert.Equal("[12,34,56,78]", str);
    }

    [Fact]
    public void Array_匿名()
    {
        var arr = new[] { 12, 34, 56, 78 };
        var str = JsonWriter.ToJson(arr.Select(e => e + 100));
        Assert.Equal("[112,134,156,178]", str);
    }

    [Fact]
    public void Array_DbTable()
    {
        var dt = new DbTable
        {
            Columns = new[] { "id1", "id1", "id1", "id1" },
            Rows = new List<Object[]>(),
            Total = 1234,
        };
        dt.Rows.Add(new Object[] { 12, 34, 56, 78 });
        dt.Rows.Add(new Object[] { 87, 65, 43, 32 });

        var str = JsonWriter.ToJson(dt);
        Assert.Equal("{\"Columns\":[\"id1\",\"id1\",\"id1\",\"id1\"],\"Rows\":[[12,34,56,78],[87,65,43,32]],\"Total\":1234}", str);
    }

    [Fact]
    [DisplayName("数组 null 元素必须保留（JSON 标准）")]
    public void Array_NullElements_Preserved()
    {
        var arr = new Object?[] { 1, null, 3 };
        var str = JsonWriter.ToJson(arr, new JsonOptions { IgnoreNullValues = true });
        // JSON 标准：null 是合法 JSON 值，数组中的 null 必须保留
        Assert.Equal("[1,null,3]", str);
    }

    [Fact]
    [DisplayName("数组 null 元素保留（缩进模式）")]
    public void Array_NullElements_Indented()
    {
        var arr = new Object?[] { 1, null, 3 };
        var str = JsonWriter.ToJson(arr, new JsonOptions { IgnoreNullValues = true, WriteIndented = true });
        // 缩进模式下 null 也能正常输出
        Assert.Contains("null", str);
        Assert.Contains("1", str);
        Assert.Contains("3", str);
    }

    [Fact]
    [DisplayName("数组 null 元素保留（IgnoreNullValues=false 保持原行为）")]
    public void Array_NullElements_IgnoreOff()
    {
        var arr = new Object?[] { 1, null, 3 };
        var str = JsonWriter.ToJson(arr, new JsonOptions { IgnoreNullValues = false });
        Assert.Equal("[1,null,3]", str);
    }

    [Fact]
    [DisplayName("字典键含特殊字符时正确转义")]
    public void DictionaryKey_Escaping()
    {
        var dic = new Dictionary<String, Object?>
        {
            ["key\"with\"quotes"] = 1,
            ["back\\slash"] = 2,
            ["new\nline"] = 3,
        };
        var str = JsonWriter.ToJson(dic);
        // 必须可被 JsonParser 正确解析
        var js = new JsonParser(str);
        var parsed = js.Decode() as IDictionary<String, Object>;
        Assert.NotNull(parsed);
        Assert.Equal(3, parsed.Count);
        Assert.Equal(1, parsed["key\"with\"quotes"]);
        Assert.Equal(2, parsed["back\\slash"]);
        Assert.Equal(3, parsed["new\nline"]);
    }

    [Fact]
    [DisplayName("WriteString 输出 \\b 和 \\f 简写转义")]
    public void String_Escape_Bell_FormFeed()
    {
        var writer = new JsonWriter();
        writer.Write("a\bb\fc");
        var str = writer.GetString();
        // \b (0x08) 和 \f (0x0C) 应使用简写而非 \\u0008/\\u000C
        Assert.Equal("\"a\\bb\\fc\"", str);
    }

    [Fact]
    [DisplayName("DateTime 默认使用 ISO 8601 格式")]
    public void DateTime_ISO8601()
    {
        var dt = new DateTime(2026, 6, 22, 15, 30, 0, DateTimeKind.Utc);
        var writer = new JsonWriter();
        writer.Write(dt);
        var str = writer.GetString();
        // ISO 8601: T 分隔符，UTC 用 Z 后缀（非 " UTC"）
        Assert.Equal("\"2026-06-22T15:30:00Z\"", str);
    }

    [Fact]
    [DisplayName("DateTime 本地时间使用 ISO 8601 格式（无后缀）")]
    public void DateTime_ISO8601_Local()
    {
        var dt = new DateTime(2026, 6, 22, 15, 30, 0, DateTimeKind.Local);
        var writer = new JsonWriter();
        writer.Write(dt);
        var str = writer.GetString();
        // 应包含 T 分隔符
        Assert.Contains("T", str);
        // 不应包含 " UTC" 后缀
        Assert.DoesNotContain(" UTC", str);
    }

    [Fact]
    [DisplayName("IgnoreComment=false 时使用 #key 机制（合法 JSON）")]
    public void Comment_ValidJson()
    {
        var writer = new JsonWriter
        {
            IgnoreComment = false,
            Options = new JsonOptions { WriteIndented = true },
        };
        var obj = new { Name = "Stone" };
        writer.Write(obj);
        var str = writer.GetString();
        // 不应包含 // 注释（非法 JSON）
        Assert.DoesNotContain("//", str);
        // 应可被 JsonParser 正确解析
        var js = new JsonParser(str);
        var parsed = js.Decode() as IDictionary<String, Object>;
        Assert.NotNull(parsed);
        Assert.Equal("Stone", parsed["Name"]);
    }

    [Fact]
    public void UnicodeEncode()
    {
        var writer = new JsonWriter();

        writer.Write(new Model("Hello\u0001World", "智能Stone"));

        var str = writer.GetString();
        Assert.Equal("""{"Name":"Hello\u0001World","Password":"智能Stone"}""", str);

        var js = new JsonParser(str);
        var dic = js.Decode() as IDictionary<String, Object>;
        Assert.NotNull(dic);
        Assert.Equal("Hello\u0001World", dic["Name"]);
        Assert.Equal("智能Stone", dic["Password"]);
    }
}