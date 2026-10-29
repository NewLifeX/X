using System.Text;
using NewLife;
using NewLife.Common;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Configuration;

public class JsonConfigProviderTests
{
    readonly IConfigProvider _provider;

    public JsonConfigProviderTests() => _provider = new JsonConfigProvider { FileName = "Config/core1.json" };

    [Fact]
    public void TestLoadAndSave()
    {
        var set = new ConfigModel
        {
            Debug = true,
            LogLevel = LogLevel.Fatal,
            LogPath = "xxx",
            NetworkLog = "255.255.255.255:514",
            TempPath = "yyy",

            Sys = new SysConfig
            {
                Name = "NewLife.Cube",
                DisplayName = "魔方平台",
                Company = "新生命开发团队",
            },
        };

        _provider.Save(set);

        var prv = _provider;
        Assert.NotNull(prv);
        Assert.Equal(set.Debug.ToString().ToLower(), prv["Debug"]);
        Assert.Equal(set.LogLevel + "", prv["LogLevel"]);
        Assert.Equal(set.LogPath, prv["LogPath"]);
        Assert.Equal(set.NetworkLog, prv["NetworkLog"]);
        Assert.Equal(set.LogFileFormat, prv["LogFileFormat"]);
        Assert.Equal(set.TempPath, prv["TempPath"]);
        Assert.Equal(set.PluginPath, prv["PluginPath"]);
        Assert.Equal(set.PluginServer, prv["PluginServer"]);

        //Assert.Equal("全局调试。XTrace.Debug", prv.GetSection("Debug").Comment);
        //Assert.Equal("系统配置", prv.GetSection("Sys").Comment);
        //Assert.Equal("用于标识系统的英文名", prv.GetSection("Sys:Name").Comment);

        var sys = set.Sys;
        Assert.Equal(sys.Name, prv["Sys:Name"]);
        Assert.Equal(sys.DisplayName, prv["Sys:DisplayName"]);
        Assert.Equal(sys.Company, prv["Sys:Company"]);

        var prv2 = new JsonConfigProvider { FileName = (_provider as FileConfigProvider).FileName };
        var set2 = prv2.Load<ConfigModel>();

        Assert.NotNull(set2);
        Assert.Equal(set.Debug, set2.Debug);
        Assert.Equal(set.LogLevel, set2.LogLevel);
        Assert.Equal(set.LogPath, set2.LogPath);
        Assert.Equal(set.NetworkLog, set2.NetworkLog);
        Assert.Equal(set.LogFileFormat, set2.LogFileFormat);
        Assert.Equal(set.TempPath, set2.TempPath);
        Assert.Equal(set.PluginPath, set2.PluginPath);
        Assert.Equal(set.PluginServer, set2.PluginServer);

        //Assert.Equal("全局调试。XTrace.Debug", prv2.GetSection("Debug").Comment);
        //Assert.Equal("系统配置", prv2.GetSection("Sys").Comment);
        //Assert.Equal("用于标识系统的英文名", prv2.GetSection("Sys:Name").Comment);

        var sys2 = set2.Sys;
        Assert.NotNull(sys2);
        Assert.Equal(sys.Name, sys2.Name);
        Assert.Equal(sys.DisplayName, sys2.DisplayName);
        Assert.Equal(sys.Company, sys2.Company);
    }

    [Fact]
    public void TestBind()
    {
        var json = @"{
    ""Debug"":  ""True"",
    ""LogLevel"":  ""Fatal"",
    ""LogPath"":  ""xxx"",
    ""NetworkLog"":  ""255.255.255.255:514"",
    ""LogFileFormat"":  ""{0:yyyy_MM_dd}.log"",
    ""TempPath"":  """",
    ""PluginPath"":  ""Plugins"",
    ""PluginServer"":  ""http://x.newlifex.com/"",
    ""Sys"":  {
        ""Name"":  ""NewLife.Cube"",
        ""Version"":  """",
        ""DisplayName"":  ""魔方平台"",
        ""Company"":  ""新生命开发团队"",
        ""Develop"":  ""True"",
        ""Enable"":  ""True"",
        ""InstallTime"":  ""2019-12-30 21:05:09"",
        ""xxx"": {
            ""yyy"": ""zzz""
        }
    }
}";

        var prv = new JsonConfigProvider { FileName = "Config/core2.json" };
        var file = prv.FileName.GetFullPath().EnsureDirectory(true);
        File.WriteAllText(file, json);
        prv.LoadAll();

        var set = new ConfigModel();
        prv.Bind(set);

        Assert.NotNull(set);
        Assert.True(set.Debug);
        Assert.Equal(LogLevel.Fatal, set.LogLevel);
        Assert.Equal("xxx", set.LogPath);
        Assert.Equal("255.255.255.255:514", set.NetworkLog);

        var sys = set.Sys;
        Assert.NotNull(sys);
        Assert.Equal("NewLife.Cube", sys.Name);
        Assert.Equal("魔方平台", sys.DisplayName);
        Assert.Equal("新生命开发团队", sys.Company);

        // 三层
        Assert.Equal("zzz", prv["Sys:xxx:yyy"]);

        // 保存
        prv.Save(set);
    }

    [Fact]
    public void ArrayTest()
    {
        var prv = new JsonConfigProvider { FileName = "Config/OAuth.json" };

        //var cfg = OAuthConfig.Current;
        var cfg = new OAuthConfig();
        cfg.Invoke("OnLoaded");

        Assert.NotNull(cfg.Apps);
        Assert.Equal(4, cfg.Apps.Length);
        Assert.NotNull(cfg.Items);
        Assert.Equal(8, cfg.Items.Length);

        // 修改其中一项
        var ti = cfg.Items.FirstOrDefault();
        ti.Secret = Rand.NextString(16);

        //cfg.Save();
        prv.Save(cfg);

        var txt = File.ReadAllText(prv.FileName.GetBasePath());
        Assert.NotEmpty(txt);
        Assert.DoesNotContain("Items: []", txt);
        Assert.Contains($"\"Secret\": \"{ti.Secret}\"", txt);
        //Assert.Contains("{ \"Name\": \"Baidu\", \"Server\": null, \"AccessServer\": null, \"AppID\": null, \"Secret\": null, \"Scope\": null },", txt);

        var prv2 = new JsonConfigProvider { FileName = prv.FileName };
        var cfg2 = prv2.Load<OAuthConfig>();
        Assert.NotNull(cfg2);

        Assert.NotNull(cfg2.Apps);
        Assert.Equal(4, cfg2.Apps.Length);
        Assert.NotNull(cfg2.Apps[0]);
        Assert.NotNull(cfg2.Apps[1]);

        Assert.NotNull(cfg2.Items);
        Assert.Equal(8, cfg2.Items.Length);
        Assert.Equal(ti.Secret, cfg2.Items[0].Secret);
    }

    [Fact]
    public void ArrayTest2()
    {
        var prv = new JsonConfigProvider { FileName = "Config/OAuth2.json" };

        var cfg = new OAuthConfig();
        cfg.Items = System.Array.Empty<OAuthItem>();

        prv.Save(cfg);

        var txt = File.ReadAllText(prv.FileName.GetBasePath());
        Assert.NotEmpty(txt);
        Assert.Contains("\"Items\": []", txt);

        var prv2 = new JsonConfigProvider { FileName = prv.FileName };
        var cfg2 = prv2.Load<OAuthConfig>();

        Assert.NotNull(cfg2);
        Assert.NotNull(cfg2.Items);
        Assert.Empty(cfg2.Items);
    }

    [Fact]
    public void LoadAppSettings()
    {
        var json = JsonConfigProvider.LoadAppSettings();

        Assert.NotNull(json);

        var url = json["Url"];
        Assert.Equal("https://newlifex.com/", url);

        var section = json.GetSection("ConnectionStrings");
        Assert.NotNull(section);
        Assert.Equal("ConnectionStrings", section.Key);
        Assert.Null(section.Value);
        Assert.Null(section.Comment);
        Assert.Equal(4, section.Childs.Count);

        {
            var connStr = json["ConnectionStrings:sqlserver:ConnectionString"];
            Assert.NotNull(connStr);
            Assert.Equal("Server=127.0.0.1;Database=Membership;Uid=root;Pwd=root;", connStr);

            var provider = section["sqlserver:providerName"];
            Assert.NotNull(provider);
            Assert.Equal("SqlServer", provider);

            var sec = json.GetSection("ConnectionStrings:sqlserver");
            Assert.Equal("sqlserver", sec.Key);
            Assert.Null(sec.Value);
            Assert.Null(sec.Comment);
            Assert.Equal(2, sec.Childs.Count);
        }

        {
            var connStr = json["ConnectionStrings:MySQL.AppSettings:ConnectionString"];
            Assert.NotNull(connStr);
            Assert.Equal("Server=.;Port=3306;Database=mysql;Uid=MySQL.default;Pwd=;", connStr);

            var sec = json.GetSection("ConnectionStrings:MySQL.AppSettings");
            Assert.Equal("MySQL.AppSettings", sec.Key);
            Assert.Null(sec.Value);
            Assert.Null(sec.Comment);
            Assert.Equal(2, sec.Childs.Count);
        }

        {
            var sec = section.Find("sqlserver");
            Assert.NotNull(sec);
        }
    }

    //[Fact]
    //public void LoadAppSettings2()
    //{
    //    var json = JsonConfigProvider.LoadAppSettings("../appsettings.json");
    //    json.LoadAll();

    //    Assert.NotNull(json);
    //}

    [Fact]
    public void TestPeriodProperty()
    {
        var prv = new JsonConfigProvider { FileName = "Config/period_test.json" };

        // 默认值应为5
        Assert.Equal(5, prv.Period);

        // 可以设置
        prv.Period = 10;
        Assert.Equal(10, prv.Period);

        // 设置为0表示不做自动更新
        prv.Period = 0;
        Assert.Equal(0, prv.Period);
    }

    [Fact(DisplayName = "保存时数字与布尔按原生Json类型写出")]
    public void WriteNativeTypes()
    {
        var prv = new JsonConfigProvider { FileName = "Config/typed.json" };

        var set = new TypedModel
        {
            Port = 3306,
            Ratio = 1.5,
            Amount = 12.34m,
            Enable = true,
            Code = "0012",
            Level = LogLevel.Warn,
            Numbers = [1, 2, 3],
        };
        prv.Save(set);

        var txt = File.ReadAllText(prv.FileName.GetBasePath());
        Assert.Contains("\"Port\": 3306", txt);
        Assert.Contains("\"Enable\": true", txt);
        Assert.Contains("\"Code\": \"0012\"", txt);
        Assert.Contains("\"Level\": \"Warn\"", txt);

        // 反解析校验类型：数字仍是数字，字符串仍是字符串
        var dic = txt.DecodeJson();
        Assert.NotNull(dic);
        Assert.Equal(3306, dic["Port"]);
        Assert.Equal(1.5d, dic["Ratio"]);
        Assert.Equal(12.34d, Convert.ToDouble(dic["Amount"]));
        Assert.Equal(true, dic["Enable"]);
        Assert.Equal("0012", dic["Code"]);
        Assert.Equal("Warn", dic["Level"]);

        var nums = dic["Numbers"] as IList<Object>;
        Assert.NotNull(nums);
        Assert.Equal(3, nums.Count);
        Assert.Equal(1, nums[0]);

        // 回读一致
        var set2 = new JsonConfigProvider { FileName = prv.FileName }.Load<TypedModel>();
        Assert.NotNull(set2);
        Assert.Equal(set.Port, set2.Port);
        Assert.Equal(set.Ratio, set2.Ratio);
        Assert.Equal(set.Amount, set2.Amount);
        Assert.Equal(set.Enable, set2.Enable);
        Assert.Equal(set.Code, set2.Code);
        Assert.Equal(set.Level, set2.Level);
        Assert.Equal(set.Numbers, set2.Numbers);
    }

    [Fact(DisplayName = "读取已有文件后再输出，保持原有值类型")]
    public void KeepFileValueTypes()
    {
        var prv = new JsonConfigProvider { FileName = "Config/keeptypes.json" };
        var file = prv.FileName.GetFullPath().EnsureDirectory(true);
        File.WriteAllText(file, """
            {
              "port": 3306,
              "ratio": 1.25,
              "enable": true,
              "code": "0012",
              "nums": [1, 2, 3],
              "names": ["a", "b"]
            }
            """);

        prv.LoadAll();

        var txt = prv.GetString();
        Assert.Contains("\"port\": 3306", txt);
        Assert.Contains("\"enable\": true", txt);

        var dic = txt.DecodeJson();
        Assert.NotNull(dic);
        Assert.Equal(3306, dic["port"]);
        Assert.Equal(1.25d, dic["ratio"]);
        Assert.Equal(true, dic["enable"]);
        // 形如数字的字符串仍保持字符串
        Assert.Equal("0012", dic["code"]);

        var nums = dic["nums"] as IList<Object>;
        Assert.NotNull(nums);
        Assert.Equal(1, nums[0]);

        var names = dic["names"] as IList<Object>;
        Assert.NotNull(names);
        Assert.Equal("a", names[0]);
    }

    [Fact(DisplayName = "下划线键无法匹配属性时输出告警")]
    public void UnderscoreKeyWarn()
    {
        var json = """
            {
              "token_key": "abc",
              "connect_timeout": 30
            }
            """;

        var prv = new JsonConfigProvider { FileName = "Config/underscore.json" };
        var file = prv.FileName.GetFullPath().EnsureDirectory(true);
        File.WriteAllText(file, json);
        prv.LoadAll();

        var model = new SnakeModel();
        var old = XTrace.Log;
        var log = new CaptureLog();
        try
        {
            XTrace.Log = log;

            prv.Bind(model, false);
        }
        finally
        {
            XTrace.Log = old;
        }

        // 命名风格差异的键不会被采纳，模型保持默认值
        Assert.Null(model.TokenKey);
        Assert.Equal(0, model.ConnectTimeout);
        // 但不再静默，日志给出告警与改名建议
        var text = log.GetText();
        Assert.Contains("token_key", text);
        Assert.Contains("connect_timeout", text);
    }

    /// <summary>捕获日志文本的日志实现</summary>
    private sealed class CaptureLog : Logger
    {
        private readonly StringBuilder _sb = new();

        public String GetText()
        {
            lock (_sb) return _sb.ToString();
        }

        protected override void OnWrite(LogLevel level, String format, params Object?[] args)
        {
            var msg = args == null || args.Length == 0 ? format : String.Format(format, args);
            lock (_sb) _sb.AppendLine(msg);
        }
    }

    private class TypedModel
    {
        public Int32 Port { get; set; }
        public Double Ratio { get; set; }
        public Decimal Amount { get; set; }
        public Boolean Enable { get; set; }
        public String? Code { get; set; }
        public LogLevel Level { get; set; }
        public Int32[] Numbers { get; set; } = [];
    }

    private class SnakeModel
    {
        public String? TokenKey { get; set; }
        public Int32 ConnectTimeout { get; set; }
    }
}