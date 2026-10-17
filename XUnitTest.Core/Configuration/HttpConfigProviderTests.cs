using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using NewLife;
using NewLife.Common;
using NewLife.Configuration;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Configuration;

public class HttpConfigProviderTests
{
    private static HttpConfigProvider CreateProvider(ConfigServerStub stub, String appId = "Test") => new()
    {
        Server = stub.Server,
        AppId = appId,
        CacheLevel = ConfigCacheLevel.NoCache,
        Period = 0,
    };

    [Fact(DisplayName = "Apollo 格式：多命名空间合并拉取与绑定")]
    public void TestApollo()
    {
        using var stub = new ConfigServerStub();
        stub.SetNameSpace("application", new Dictionary<String, Object?>
        {
            ["appapiurl"] = "http://api.newlifex.com",
            ["radius"] = "15",
            ["MySqlServer"] = "server=.;database=test",
        });
        stub.SetNameSpace("share", new Dictionary<String, Object?>
        {
            ["appapiurl"] = "http://other.newlifex.com",
            ["Title"] = "NewLife开发团队",
        });

        using var provider = new ApolloConfigProvider
        {
            Server = stub.Server,
            AppId = "testapi",
            CacheLevel = ConfigCacheLevel.NoCache,
            Period = 0,
        };
        provider.SetApollo("application,share");

        Assert.True(provider.LoadAll());

        // 命名空间按顺序合并，先到先得
        var url = provider["appapiurl"];
        Assert.Equal("http://api.newlifex.com", url);
        Assert.Equal("NewLife开发团队", provider["Title"]);

        var keys = provider.Keys.ToArray();
        Assert.NotNull(keys);
        Assert.Contains("MySqlServer", keys);

        // 服务器信息保留合并后的配置
        Assert.NotNull(provider.Info);
        Assert.True(provider.Info.ContainsKey("Title"));

        var model = provider.Load<Model>();
        Assert.NotNull(model);
        Assert.NotEmpty(model.AppApiUrl);
        Assert.Equal(url, model.AppApiUrl);
        Assert.True(model.Radius > 0);
        Assert.NotEmpty(model.MySqlServer);

        var model2 = new Model();
        provider.Bind(model2, false);
        Assert.Equal(url, model2.AppApiUrl);
        Assert.True(model2.Radius > 0);
        Assert.NotEmpty(model2.MySqlServer);

        // Apollo 不支持写回
        provider["Title"] = "changed";
        Assert.Throws<NotSupportedException>(() => provider.SaveAll());
    }

    private class Model
    {
        public Int32 Radius { get; set; }

        public String MySqlServer { get; set; }

        public String AppApiUrl { get; set; }
    }

    [Fact(DisplayName = "星尘格式：GetAll 拉取分层配置并绑定模型")]
    public void TestStardust()
    {
        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["test1"] = "测试",
            ["conn_Shop"] = "商店",
            ["Title"] = "NewLife开发团队",
            ["cls:Server"] = "http://127.0.0.1",
            ["cls:Topic"] = "mytopic",
        });

        using var provider = CreateProvider(stub);

        Assert.True(provider.LoadAll());
        Assert.False(provider.IsNew);

        // 增强信封中的版本号被记录
        Assert.NotNull(provider.Info);
        Assert.True(provider.Info["version"].ToInt() > 0);

        var str = provider["test1"];
        Assert.NotEmpty(str);

        var keys = provider.Keys.ToArray();
        Assert.NotNull(keys);
        Assert.Contains("cls", keys);

        var model = provider.Load<Model2>();
        Assert.NotNull(model);
        Assert.NotEmpty(model.Test);
        Assert.Equal(str, model.Test);
        Assert.NotEmpty(model.Shop);
        Assert.NotEmpty(model.Title);
        Assert.Equal("NewLife开发团队", model.Title);

        var model2 = new Model2();
        provider.Bind(model2, false);
        Assert.Equal(str, model2.Test);
        Assert.NotEmpty(model2.Shop);
        Assert.Equal("NewLife开发团队", model2.Title);

        // 分层路径绑定
        var cls = provider.Load<MyCls>("cls");
        Assert.NotNull(cls);
        Assert.Equal("http://127.0.0.1", cls.Server);
        Assert.Equal("mytopic", cls.Topic);
    }

    [Fact(DisplayName = "星尘格式：GetAll 支持嵌套字典形态")]
    public void TestGetAllNested()
    {
        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["name"] = "stone",
            ["cls"] = new Dictionary<String, Object?>
            {
                ["server"] = "http://127.0.0.1",
                ["topic"] = "mytopic",
            },
            ["deep:level:key"] = "v",
        });

        using var provider = CreateProvider(stub);
        Assert.True(provider.LoadAll());

        Assert.Equal("stone", provider["name"]);
        Assert.Equal("http://127.0.0.1", provider["cls:server"]);
        Assert.Equal("mytopic", provider["cls:topic"]);
        Assert.Equal("v", provider["deep:level:key"]);

        var cls = provider.Load<MyCls>("cls");
        Assert.NotNull(cls);
        Assert.Equal("http://127.0.0.1", cls.Server);
    }

    [Fact(DisplayName = "星尘格式：SaveAll 仅提交变更项并回写服务端")]
    public void TestSaveAll()
    {
        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["test1"] = "old",
            ["Title"] = "团队",
        });

        using var provider = CreateProvider(stub);
        Assert.True(provider.LoadAll());
        Assert.Equal("old", provider["test1"]);

        // 未变更时 SaveAll 不发起提交
        Assert.True(provider.SaveAll());
        Assert.Equal(0, stub.SetAllCalls);

        provider.GetSection("test1")!.Comment = "备注";
        provider["test1"] = "测试";
        Assert.True(provider.SaveAll());

        Assert.Equal(1, stub.SetAllCalls);
        Assert.Equal("测试", stub.GetConfig("test1"));

        // 只有变更项被提交，且评论随值一起提交
        Assert.True(stub.LastSaved.ContainsKey("test1"));
        Assert.False(stub.LastSaved.ContainsKey("Title"));
        var raw = stub.LastSaved["test1"] as IDictionary<String, Object?>;
        Assert.NotNull(raw);
        var comment = raw.FirstOrDefault(e => e.Key.EqualIgnoreCase("comment")).Value;
        Assert.Equal("备注", comment + "");

        // 服务端数据已更新，重新拉取的提供者可读到新值
        using var provider2 = CreateProvider(stub);
        Assert.True(provider2.LoadAll());
        Assert.Equal("测试", provider2["test1"]);
    }

    [Fact(DisplayName = "星尘格式：DoRefresh 检测变更并热更新配置树与绑定模型")]
    public void TestDoRefresh()
    {
        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["test1"] = "old",
            ["Title"] = "团队",
        });

        using var provider = new TestHttpConfigProvider(stub.Server);
        Assert.True(provider.LoadAll());

        var model = new Model2();
        provider.Bind(model, true);
        Assert.Equal("old", model.Test);

        var changed = 0;
        provider.Changed += (s, e) => changed++;

        // 服务端无变化，不触发变更通知
        provider.Refresh();
        Assert.Equal(0, changed);
        Assert.Equal("old", provider["test1"]);

        // 服务端变更后再次拉取，配置树与绑定模型同步更新
        stub.SetConfig("test1", "新值");
        provider.Refresh();
        Assert.Equal(1, changed);
        Assert.Equal("新值", provider["test1"]);
        Assert.Equal("新值", model.Test);

        // 忽略键不产生变更事件（被忽略的键不触发重新加载，配置树保持旧值）
        provider.IgnoreChangedKeys.Add("Title");
        stub.SetConfig("Title", "新团队");
        provider.Refresh();
        Assert.Equal(1, changed);
        Assert.Equal("团队", provider["Title"]);
    }

    [Fact(DisplayName = "远端 500：LoadAll 返回 false，恢复后重新加载成功")]
    public void TestLoadAll_ServerError()
    {
        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["test1"] = "v1",
        });
        using var provider = CreateProvider(stub);

        Assert.True(provider.LoadAll());
        Assert.Equal("v1", provider["test1"]);

        // 远端故障：返回 false，配置树保留上次成功加载的值
        stub.GetAllStatus = HttpStatusCode.InternalServerError;
        Assert.False(provider.LoadAll());
        Assert.Equal("v1", provider["test1"]);

        // 服务恢复后重新加载成功
        stub.GetAllStatus = HttpStatusCode.OK;
        stub.SetConfig("test1", "v2");
        Assert.True(provider.LoadAll());
        Assert.Equal("v2", provider["test1"]);
    }

    [Fact(DisplayName = "连接拒绝：LoadAll 返回 false，切换服务地址后恢复成功")]
    public void TestLoadAll_ConnectionRefused()
    {
        // 先启动再释放，取得一个已关闭的本地端口，确保连接被拒绝
        String deadServer;
        using (var temp = new ConfigServerStub()) deadServer = temp.Server;

        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["test1"] = "ok",
        });
        using var provider = new HttpConfigProvider
        {
            Server = deadServer,
            AppId = "Test",
            CacheLevel = ConfigCacheLevel.NoCache,
            Period = 0,
        };

        Assert.False(provider.LoadAll());

        provider.Server = stub.Server;
        provider.Client = null;
        Assert.True(provider.LoadAll());
        Assert.Equal("ok", provider["test1"]);
    }

    [Fact(DisplayName = "请求超时：LoadAll 返回 false，恢复后重新加载成功")]
    public void TestLoadAll_Timeout()
    {
        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["test1"] = "ok",
        });
        stub.GetAllDelay = 3_000;

        using var provider = new HttpConfigProvider
        {
            Server = stub.Server,
            AppId = "Test",
            CacheLevel = ConfigCacheLevel.NoCache,
            Period = 0,
            Client = new ApiHttpClient(stub.Server) { Timeout = 300 },
        };

        Assert.False(provider.LoadAll());

        stub.GetAllDelay = 0;
        Assert.True(provider.LoadAll());
        Assert.Equal("ok", provider["test1"]);
    }

    [Fact(DisplayName = "空配置：LoadAll 返回 true 且标记为新配置")]
    public void TestLoadAll_EmptyConfig()
    {
        using var stub = new ConfigServerStub();
        using var provider = CreateProvider(stub);

        Assert.True(provider.LoadAll());
        Assert.True(provider.IsNew);
        Assert.Empty(provider.Keys);
    }

    [Fact(DisplayName = "懒加载失败：访问不抛异常并返回默认值，服务恢复后重试加载成功")]
    public void TestEnsureLoad_FailureRetries()
    {
        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["test1"] = "v1",
        });
        using var provider = CreateProvider(stub);

        // 首次访问触发懒加载，远端故障时不抛异常，返回默认值
        stub.GetAllStatus = HttpStatusCode.InternalServerError;
        Assert.Null(provider["test1"]);
        Assert.True(stub.GetAllCalls > 0);
        Assert.Null(provider["test2"]);

        // 失败后保持未加载状态，下次访问仍会重试
        var calls = stub.GetAllCalls;
        Assert.Null(provider["test1"]);
        Assert.True(stub.GetAllCalls > calls);

        // 服务恢复后再次访问即可加载成功，无需新建提供者
        stub.GetAllStatus = HttpStatusCode.OK;
        var beforeRecover = stub.GetAllCalls;
        Assert.Equal("v1", provider["test1"]);
        Assert.True(stub.GetAllCalls > beforeRecover);
    }

    [Fact(DisplayName = "懒加载成功：只加载一次，后续访问不再命中服务端")]
    public void TestEnsureLoad_LoadsOnceOnSuccess()
    {
        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["test1"] = "v1",
        });
        using var provider = CreateProvider(stub);

        // 首次访问触发懒加载
        Assert.Equal("v1", provider["test1"]);
        Assert.Equal(1, stub.GetAllCalls);

        // 已加载成功：后续访问与键枚举均不再拉取
        stub.SetConfig("test1", "v2");
        Assert.Equal("v1", provider["test1"]);
        Assert.Null(provider["test2"]);
        Assert.NotNull(provider.Keys);
        Assert.Equal(1, stub.GetAllCalls);
    }

    [Fact(DisplayName = "刷新失败：DoRefresh 保留现有配置并在恢复后热更新")]
    public void TestDoRefresh_FailureRecovers()
    {
        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["test1"] = "old",
        });
        using var provider = new TestHttpConfigProvider(stub.Server);
        Assert.True(provider.LoadAll());

        var changed = 0;
        provider.Changed += (s, e) => changed++;

        // 远端故障：刷新不抛异常、不产生变更通知、保留旧配置
        stub.GetAllStatus = HttpStatusCode.InternalServerError;
        provider.Refresh();
        Assert.Equal(0, changed);
        Assert.Equal("old", provider["test1"]);

        // 恢复后正常热更新
        stub.GetAllStatus = HttpStatusCode.OK;
        stub.SetConfig("test1", "new");
        provider.Refresh();
        Assert.Equal(1, changed);
        Assert.Equal("new", provider["test1"]);
    }

    [Fact(DisplayName = "星尘格式：SetAll 返回影响行数，Bind 按路径绑定")]
    public void TestSetAllAndBind()
    {
        using var stub = new ConfigServerStub();
        using var provider = new TestHttpConfigProvider(stub.Server);

        var n = provider.SetAllDirect(new Dictionary<String, Object?>
        {
            ["a"] = "1",
            ["b"] = "2",
        });
        Assert.Equal(2, n);
        Assert.Equal("1", stub.GetConfig("a"));
        Assert.Equal("2", stub.GetConfig("b"));

        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["cls:Server"] = "http://127.0.0.1",
            ["cls:Topic"] = "mytopic",
        });

        var model = new MyCls();
        provider.Bind(model, false, "cls");
        Assert.Equal("http://127.0.0.1", model.Server);
        Assert.Equal("mytopic", model.Topic);
    }

    [Fact(DisplayName = "HttpConfigAttribute：特性指定的模型从配置中心加载")]
    public void TestHttpConfigAttribute()
    {
        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["test1"] = "1234",
            ["Title"] = "NewLife开发团队",
        });

        var original = Config<HttpConfigModel>.Provider;
        try
        {
            Config<HttpConfigModel>.Provider = CreateProvider(stub);
            HttpConfigModel.Current = null;

            var c = HttpConfigModel.Current;
            Assert.Equal("1234", c.Test);
            Assert.Equal("NewLife开发团队", c.Title);
        }
        finally
        {
            HttpConfigModel.Current = null;
            Config<HttpConfigModel>.Provider = original;
        }
    }

    private class Model2
    {
        [DataMember(Name = "test1")]
        public String Test { get; set; }

        [DataMember(Name = "conn_Shop")]
        public String Shop { get; set; }

        public String Title { get; set; }
    }

    [Fact]
    public void TestLayers()
    {
        var dic = new Dictionary<String, Object>
        {
            ["name"] = "stone",
            ["cls:server"] = "http://127.0.0.1",
            ["cls:topic"] = "mytopic"
        };

        var prv = new HttpConfigProvider();
        var rs = prv.Build(dic);

        Assert.Equal(2, rs.Childs.Count);
        Assert.Equal("name", rs.Childs[0].Key);
        Assert.Equal("stone", rs.Childs[0].Value);

        var section = rs.Childs[1];
        Assert.Equal("cls", section.Key);
        Assert.Null(section.Value);
        Assert.Equal(2, section.Childs.Count);
        Assert.Equal("server", section.Childs[0].Key);
        Assert.Equal("http://127.0.0.1", section.Childs[0].Value);
        Assert.Equal("topic", section.Childs[1].Key);
        Assert.Equal("mytopic", section.Childs[1].Value);

        prv.Root = rs;

        var cls = prv.Load<MyCls>("cls");
        Assert.NotNull(cls);
        Assert.Equal("http://127.0.0.1", cls.Server);
        Assert.Equal("mytopic", cls.Topic);

        Assert.Equal("http://127.0.0.1", prv["cls:Server"]);
        Assert.Equal("mytopic", prv["cls:Topic"]);
    }

    [Fact(DisplayName = "星尘格式：分层路径绑定模型")]
    public void TestStardustLayers()
    {
        using var stub = new ConfigServerStub();
        stub.SetConfigs(new Dictionary<String, Object?>
        {
            ["cls:Server"] = "http://127.0.0.1",
            ["cls:Topic"] = "mytopic",
        });

        using var prv = CreateProvider(stub);

        var cls = prv.Load<MyCls>("cls");
        Assert.NotNull(cls);
        Assert.Equal("http://127.0.0.1", cls.Server);
        Assert.Equal("mytopic", cls.Topic);

        Assert.Equal("http://127.0.0.1", prv["cls:Server"]);
        Assert.Equal("mytopic", prv["cls:Topic"]);
    }

    class MyCls
    {
        public String Server { get; set; }

        public String Topic { get; set; }
    }

    /// <summary>暴露受保护刷新与提交入口的提供者</summary>
    private sealed class TestHttpConfigProvider : HttpConfigProvider
    {
        public TestHttpConfigProvider(String server)
        {
            Server = server;
            AppId = "Test";
            CacheLevel = ConfigCacheLevel.NoCache;
            Period = 0;
        }

        /// <summary>执行一次配置刷新（定时器回调的同步入口）</summary>
        public void Refresh() => DoRefresh(null);

        /// <summary>直接提交配置字典</summary>
        /// <param name="configs">配置字典</param>
        /// <returns>影响的配置数</returns>
        public Int32 SetAllDirect(IDictionary<String, Object?> configs) => SetAll(configs);
    }

    #region 配置中心桩
    /// <summary>配置中心契约桩</summary>
    /// <remarks>
    /// 用 NewLife.Http 的 HttpServer 在动态端口上复刻配置中心的必要行为：
    /// 星尘 Config/GetAll 返回增强信封 {code,data:{configs,version}}；Config/SetAll 合并提交的配置并返回影响行数；
    /// Apollo 的 /configfiles/json/{appId}/default/{ns} 按命名空间返回扁平字典。
    /// 配置保存在内存中，随实例释放。
    /// </remarks>
    private sealed class ConfigServerStub : IDisposable
    {
        private readonly HttpServer _server;
        private readonly Dictionary<String, Object?> _configs = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<String, Dictionary<String, Object?>> _nameSpaces = new(StringComparer.OrdinalIgnoreCase);
        private readonly Object _sync = new();
        private Int32 _version;

        /// <summary>服务地址</summary>
        public String Server { get; }

        /// <summary>SetAll 调用次数</summary>
        public Int32 SetAllCalls { get; private set; }

        /// <summary>GetAll 命中次数，用于验证懒加载只执行一次</summary>
        public Int32 GetAllCalls { get; private set; }

        /// <summary>最近一次 SetAll 提交的原始配置</summary>
        public Dictionary<String, Object?> LastSaved { get; private set; } = [];

        /// <summary>GetAll 响应状态码，用于故障注入</summary>
        public HttpStatusCode GetAllStatus { get; set; } = HttpStatusCode.OK;

        /// <summary>GetAll 响应延迟毫秒数，用于超时故障注入</summary>
        public Int32 GetAllDelay { get; set; }

        public ConfigServerStub()
        {
            // 动态端口由系统分配
            _server = new HttpServer { Port = 0 };
            _server.MapGet("/Config/GetAll", GetAll);
            _server.MapPost("/Config/SetAll", SetAll);
            _server.MapGet("/configfiles/json/{appId}/default/{ns}", GetNameSpace);
            _server.Start();

            Server = $"http://127.0.0.1:{_server.Port}";
        }

        /// <summary>批量写入配置</summary>
        /// <param name="configs">配置字典</param>
        public void SetConfigs(IDictionary<String, Object?> configs)
        {
            lock (_sync)
            {
                foreach (var item in configs) _configs[item.Key] = item.Value;
                _version++;
            }
        }

        /// <summary>写入单个配置</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public void SetConfig(String key, Object? value)
        {
            lock (_sync)
            {
                _configs[key] = value;
                _version++;
            }
        }

        /// <summary>读取配置</summary>
        /// <param name="key">键</param>
        /// <returns>配置值，不存在时返回 null</returns>
        public Object? GetConfig(String key)
        {
            lock (_sync) return _configs.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>写入 Apollo 命名空间</summary>
        /// <param name="name">命名空间</param>
        /// <param name="configs">配置字典</param>
        public void SetNameSpace(String name, IDictionary<String, Object?> configs)
        {
            lock (_sync) _nameSpaces[name] = new Dictionary<String, Object?>(configs, StringComparer.OrdinalIgnoreCase);
        }

        private void GetAll(IHttpContext context)
        {
            var delay = GetAllDelay;
            if (delay > 0) Thread.Sleep(delay);

            lock (_sync)
            {
                GetAllCalls++;
                context.WriteJson(new
                {
                    code = 0,
                    data = new
                    {
                        configs = new Dictionary<String, Object?>(_configs),
                        version = _version,
                    },
                }, GetAllStatus);
            }
        }

        private void SetAll(IHttpContext context)
        {
            var body = context.Request.Body?.ToStr();
            var args = body.IsNullOrEmpty() ? null : body.DecodeJson();
            var configs = args?["configs"] as IDictionary<String, Object?>;
            if (configs == null)
            {
                context.WriteJson(new { code = 500, data = "缺少 configs" });
                return;
            }

            var raw = new Dictionary<String, Object?>();
            lock (_sync)
            {
                SetAllCalls++;
                foreach (var item in configs)
                {
                    raw[item.Key] = item.Value;
                    _configs[item.Key] = Unwrap(item.Value);
                }
                LastSaved = raw;
                _version++;
            }

            context.WriteJson(new { code = 0, data = configs.Count });
        }

        private void GetNameSpace(IHttpContext context)
        {
            var ns = context.Parameters["ns"] + "";
            lock (_sync)
            {
                if (!_nameSpaces.TryGetValue(ns, out var configs))
                {
                    context.WriteJson(new { code = 404, data = $"命名空间不存在 {ns}" });
                    return;
                }

                context.WriteJson(new { code = 0, data = configs });
            }
        }

        /// <summary>兼容 SaveAll 提交的 {value,comment} 形态</summary>
        private static Object? Unwrap(Object? value)
        {
            if (value is IDictionary<String, Object?> dic)
            {
                foreach (var item in dic)
                {
                    if (item.Key.EqualIgnoreCase("value")) return item.Value;
                }
            }

            return value;
        }

        public void Dispose() => _server.TryDispose();
    }
    #endregion
}