using System.Reflection;
using System.Runtime.Loader;
using NewLife;
using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

public class ConfigProviderTests
{
    [Fact]
    public void Test1()
    {
        var ini = new IniConfigProvider { FileName = "Config/core0.ini" };
        var xml = new XmlConfigProvider { FileName = "Config/core0.xml" };
        var json = new JsonConfigProvider { FileName = "Config/core0.json" };
        //var http = new HttpConfigProvider
        //{
        //    Server = "http://127.0.0.1:5000/config,http://10.0.0.4/config",
        //    AppId = "Test",
        //    Secret = "12345678",
        //    LocalCache = true,
        //};

        //var p = http["LogPath"];
        //http["LogPath"] = p;

        var cfg = xml.Load<Setting>();

        Assert.NotNull(cfg);
        Assert.True(cfg.Debug);
        Assert.NotEmpty(cfg.LogFileFormat);

        ini.Save(cfg);
        xml.Save(cfg);
        json.Save(cfg);
    }

    /// <summary>ConfigProvider 静态构造期间，程序集解析回调递归访问配置时不得缺失内建提供者</summary>
    /// <remarks>
    /// ConfigProvider 静态构造会初始化 Runtime，其中的 Type.GetType 会触发程序集解析回调；
    /// 回调若同步递归访问 Config&lt;T&gt;，注册表为空时将抛出“找不到内建提供者”，并永久毒化该配置类型。
    /// 测试进程内的静态状态早已初始化，无法重现该时序，故用独立 ALC 加载库的第二份副本复现。
    /// </remarks>
    [Fact(DisplayName = "初始化期的解析回调递归访问配置不缺失内建提供者")]
    public void StaticInit_ReentrantCreate_UsesBuiltInProvider()
    {
        var alc = new AssemblyLoadContext("ConfigProviderReentrancy", true);
        ResolveEventHandler? domainHandler = null;
        Func<AssemblyLoadContext, AssemblyName, Assembly?>? alcHandler = null;
        try
        {
            var fresh = alc.LoadFromAssemblyPath(typeof(Setting).Assembly.Location);
            var freshSetting = fresh.GetType("NewLife.Setting");
            var freshSysConfig = fresh.GetType("NewLife.Common.SysConfig");
            Assert.NotNull(freshSetting);
            Assert.NotNull(freshSysConfig);

            var flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            Object? nestedProvider = null;
            Exception? nestedError = null;
            var attempts = 0;
            var critical = false;

            // 副本 Runtime 静态构造的 Type.GetType("UnityEngine...") 会触发解析回调；
            // 在回调中递归访问另一配置类型，重现“静态构造中递归 Create”的时序
            Assembly? OnResolve(String? name)
            {
                if (critical && name != null && name.Contains("UnityEngine"))
                {
                    Interlocked.Increment(ref attempts);
                    try
                    {
                        nestedProvider = freshSysConfig.GetProperty("Provider", flags)?.GetValue(null);
                    }
                    catch (TargetInvocationException ex)
                    {
                        nestedError = ex.InnerException;
                    }
                }

                return null;
            }

            domainHandler = (s, e) => OnResolve(e.Name);
            alcHandler = (a, n) => OnResolve(n.Name);
            AppDomain.CurrentDomain.AssemblyResolve += domainHandler;
            alc.Resolving += alcHandler;

            // 首次访问副本配置，触发副本的 Config<T> → ConfigProvider → Runtime 静态构造链
            critical = true;
            Object? outerProvider;
            try
            {
                outerProvider = freshSetting.GetProperty("Provider", flags)?.GetValue(null);
            }
            finally
            {
                critical = false;
            }

            Assert.NotNull(outerProvider);
            Assert.True(attempts > 0, "未触发程序集解析回调，未覆盖静态构造递归访问场景");
            Assert.Null(nestedError);
            Assert.NotNull(nestedProvider);
        }
        finally
        {
            if (domainHandler != null) AppDomain.CurrentDomain.AssemblyResolve -= domainHandler;
            if (alcHandler != null) alc.Resolving -= alcHandler;
            alc.Unload();
        }
    }

    [Fact]
    public void Find()
    {
        var ini = new IniConfigProvider
        {
            FileName = "config/find.ini"
        };

        Assert.Null(ini["xxx"]);
        Assert.Null(ini["aaa"]);
        Assert.Null(ini["aaa:bbb:ccc"]);

        ini["xxx"] = "xyz";
        var ci = ini.GetSection("xxx");
        Assert.NotNull(ci);
        Assert.Equal("xxx", ci.Key);
        Assert.Equal("xyz", ci.Value);

        ini["aaa:bbb:ccc"] = "abc";
        var ci2 = ini.GetSection("aaa");
        Assert.NotNull(ci2);
        Assert.Equal("aaa", ci2.Key);
        Assert.Null(ci2.Value);
        Assert.NotNull(ci2.Childs);
        Assert.True(ci2.Childs.Count == 1);

        ci2 = ci2.Childs[0];
        Assert.NotNull(ci2);
        Assert.Equal("bbb", ci2.Key);
        Assert.Null(ci2.Value);
        Assert.NotNull(ci2.Childs);
        Assert.True(ci2.Childs.Count == 1);

        ci2 = ci2.Childs[0];
        Assert.NotNull(ci2);
        Assert.Equal("ccc", ci2.Key);
        Assert.Equal("abc", ci2.Value);
        Assert.Null(ci2.Childs);

        var ci3 = ini.GetSection("aaa:bbb:ccc");
        Assert.NotNull(ci3);
        Assert.Equal("ccc", ci3.Key);
        Assert.Equal("abc", ci3.Value);
        Assert.Equal(ci2, ci3);
    }

    //[Fact]
    //public void TestBind()
    //{
    //    var config = new ConfigProvider();
    //    config["orderRedis"] = "server=127.0.0.1:6379;password=pass;db=7";

    //    var rds = new Redis();

    //    config.Bind(rds, true, "orderRedis");

    //    Assert.Equal("127.0.0.1:6379", rds.Server);
    //    Assert.Equal("pass", rds.Password);
    //    Assert.Equal(7, rds.Db);

    //    config["orderRedis"] = "server=10.0.0.1:6379;password=word;db=13";
    //    config.SaveAll();

    //    Assert.Equal("10.0.0.1:6379", rds.Server);
    //    Assert.Equal("word", rds.Password);
    //    Assert.Equal(13, rds.Db);
    //}
}