using System;
using System.Collections.Generic;
using NewLife;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

public class FastJsonTest : JsonTestBase
{
    FastJson _json = new FastJson();

    [Fact(DisplayName = "基础测试")]
    public void Test1()
    {
        var set = new Setting
        {
            LogLevel = LogLevel.Error,
            LogPath = "xxx",
        };

        var js = _json.Write(set, true, false, false);
        Assert.True(js.StartsWith("{") && js.EndsWith("}"));

        var set2 = _json.Read(js, typeof(Setting)) as Setting;

        Assert.Equal(LogLevel.Error, set2.LogLevel);
        Assert.Equal("xxx", set2.LogPath);
    }

    [Fact]
    public void DateTimeTest()
    {
        var model = new Model();
        Rand.Fill(model);
        model.Roles = ["admin", "user"];
        model.Scores = [1, 2, 3];
        var js = _json.Write(model, true);

        var models = _json.Read(_json_value, typeof(Model[])) as Model[];
        Assert.Equal(2, models.Length);

        CheckModel(models);
    }

    [Fact]
    public void InterfaceTest()
    {
        var list = new List<IDuck>
        {
            new DuckB { Name = "123" },
            new DuckB { Name = "456" },
            new DuckB { Name = "789" }
        };
        var model = new ModelA
        {
            ID = 2233,
            Childs = list,
        };

        var js = new FastJson();
        var services = new ObjectContainer();
        js.ServiceProvider = services.BuildServiceProvider();

        //var json = model.ToJson();
        var json = js.Write(model);

        // 直接反序列化会抛出异常
        Assert.Throws<Exception>(() => js.Read(json, typeof(ModelA)));

        // 上对象容器。随时可以注册服务
        services.AddTransient<IDuck, DuckB>();

        // 再来一次反序列化
        //var model2 = json.ToJsonEntity<ModelA>();
        var model2 = js.Read(json, typeof(ModelA)) as ModelA;
        Assert.NotNull(model2);
        Assert.Equal(2233, model2.ID);
        Assert.Equal(3, model2.Childs.Count);
        Assert.Equal("123", model.Childs[0].Name);
        Assert.Equal("456", model.Childs[1].Name);
        Assert.Equal("789", model.Childs[2].Name);
    }

    interface IDuck
    {
        public String Name { get; set; }
    }

    class ModelA
    {
        public Int32 ID { get; set; }

        public IList<IDuck> Childs { get; set; }

        public Object Body { get; set; }
    }

    class DuckB : IDuck
    {
        public String Name { get; set; }
    }

    [Fact]
    public void ObjectTest()
    {
        var model = new ModelB
        {
            ID = 2233,
            Body = new
            {
                aaa = 123,
                bbb = 456,
                ccc = 789,
            },
        };

        // 序列化
        //var json = model.ToJson();
        var json = _json.Write(model);

        // 反序列化
        //var model2 = json.ToJsonEntity<ModelB>();
        var model2 = _json.Read(json, typeof(ModelA)) as ModelA;
        Assert.NotNull(model2);
        Assert.Equal(2233, model2.ID);

        var dic = model2.Body as IDictionary<String, Object>;
        Assert.Equal(3, dic.Count);
        Assert.Equal(123, dic["aaa"]);
        Assert.Equal(456, dic["bbb"]);
        Assert.Equal(789, dic["ccc"]);
    }

    class ModelB
    {
        public Int32 ID { get; set; }

        public Object Body { get; set; }
    }

    [Fact]
    public void Decode()
    {
        var model = new ModelB
        {
            ID = 2233,
            Body = new
            {
                aaa = 123,
                bbb = 456,
                ccc = 789,
            },
        };

        // 序列化
        var json = _json.Write(model);

        var dic = _json.Decode(json);
        Assert.NotNull(dic);

        Assert.Equal(2233, dic["id"]);

        dic = dic["body"] as IDictionary<String, Object>;
        Assert.NotNull(dic);
        Assert.Equal(3, dic.Count);
        Assert.Equal(123, dic["aaa"]);
        Assert.Equal(456, dic["bbb"]);
        Assert.Equal(789, dic["ccc"]);
    }

    #region PropertyNaming 序列化与反序列化
    class NamingModel
    {
        public Int32 UserId { get; set; }

        public String UserName { get; set; }

        public DateTime CreateTime { get; set; }
    }

    [Fact(DisplayName = "CamelCase序列化与反序列化")]
    public void CamelCaseRoundTrip()
    {
        var model = new NamingModel { UserId = 42, UserName = "Stone", CreateTime = new DateTime(2025, 1, 1) };

        var opt = new JsonOptions { PropertyNaming = PropertyNaming.CamelCase };
        var json = _json.Write(model, opt);

        // 序列化后键名应为 camelCase
        Assert.Contains("\"userId\"", json);
        Assert.Contains("\"userName\"", json);

        // 反序列化回来
        var model2 = _json.Read(json, typeof(NamingModel), opt) as NamingModel;
        Assert.NotNull(model2);
        Assert.Equal(42, model2.UserId);
        Assert.Equal("Stone", model2.UserName);
        Assert.Equal(new DateTime(2025, 1, 1), model2.CreateTime);
    }

    [Fact(DisplayName = "SnakeCaseLower序列化与反序列化")]
    public void SnakeCaseLowerRoundTrip()
    {
        var model = new NamingModel { UserId = 99, UserName = "Test", CreateTime = new DateTime(2024, 6, 15) };

        var opt = new JsonOptions { PropertyNaming = PropertyNaming.SnakeCaseLower };
        var json = _json.Write(model, opt);

        // 序列化后键名应为 snake_case
        Assert.Contains("\"user_id\"", json);
        Assert.Contains("\"user_name\"", json);
        Assert.Contains("\"create_time\"", json);

        // 反序列化回来
        var model2 = _json.Read(json, typeof(NamingModel), opt) as NamingModel;
        Assert.NotNull(model2);
        Assert.Equal(99, model2.UserId);
        Assert.Equal("Test", model2.UserName);
        Assert.Equal(new DateTime(2024, 6, 15), model2.CreateTime);
    }

    [Fact(DisplayName = "KebabCaseLower序列化与反序列化")]
    public void KebabCaseLowerRoundTrip()
    {
        var model = new NamingModel { UserId = 7, UserName = "Kebab", CreateTime = new DateTime(2023, 3, 20) };

        var opt = new JsonOptions { PropertyNaming = PropertyNaming.KebabCaseLower };
        var json = _json.Write(model, opt);

        // 序列化后键名应为 kebab-case
        Assert.Contains("\"user-id\"", json);
        Assert.Contains("\"user-name\"", json);

        // 反序列化回来
        var model2 = _json.Read(json, typeof(NamingModel), opt) as NamingModel;
        Assert.NotNull(model2);
        Assert.Equal(7, model2.UserId);
        Assert.Equal("Kebab", model2.UserName);
        Assert.Equal(new DateTime(2023, 3, 20), model2.CreateTime);
    }

    [Fact(DisplayName = "SnakeCaseUpper序列化与反序列化")]
    public void SnakeCaseUpperRoundTrip()
    {
        var model = new NamingModel { UserId = 88, UserName = "Upper" };

        var opt = new JsonOptions { PropertyNaming = PropertyNaming.SnakeCaseUpper };
        var json = _json.Write(model, opt);

        Assert.Contains("\"USER_ID\"", json);
        Assert.Contains("\"USER_NAME\"", json);

        var model2 = _json.Read(json, typeof(NamingModel), opt) as NamingModel;
        Assert.NotNull(model2);
        Assert.Equal(88, model2.UserId);
        Assert.Equal("Upper", model2.UserName);
    }

    [Fact(DisplayName = "KebabCaseUpper序列化与反序列化")]
    public void KebabCaseUpperRoundTrip()
    {
        var model = new NamingModel { UserId = 66, UserName = "KebabUp" };

        var opt = new JsonOptions { PropertyNaming = PropertyNaming.KebabCaseUpper };
        var json = _json.Write(model, opt);

        Assert.Contains("\"USER-ID\"", json);
        Assert.Contains("\"USER-NAME\"", json);

        var model2 = _json.Read(json, typeof(NamingModel), opt) as NamingModel;
        Assert.NotNull(model2);
        Assert.Equal(66, model2.UserId);
        Assert.Equal("KebabUp", model2.UserName);
    }

    [Fact(DisplayName = "无命名策略直接反序列化CamelCase")]
    public void NoneNaming_CamelCase_Fallback()
    {
        // 即使没有指定命名策略，大小写不敏感兜底也应工作
        var json = """{"userId":123,"userName":"Fallback"}""";
        var model = _json.Read(json, typeof(NamingModel)) as NamingModel;
        Assert.NotNull(model);
        Assert.Equal(123, model.UserId);
        Assert.Equal("Fallback", model.UserName);
    }

    [Fact(DisplayName = "JsonHelper扩展方法支持Options反序列化")]
    public void ToJsonEntity_WithOptions()
    {
        var model = new NamingModel { UserId = 55, UserName = "Helper" };

        var opt = new JsonOptions { PropertyNaming = PropertyNaming.SnakeCaseLower };
        var json = model.ToJson(opt);

        Assert.Contains("\"user_id\"", json);

        var model2 = json.ToJsonEntity<NamingModel>(opt);
        Assert.NotNull(model2);
        Assert.Equal(55, model2.UserId);
        Assert.Equal("Helper", model2.UserName);
    }
    #endregion
}