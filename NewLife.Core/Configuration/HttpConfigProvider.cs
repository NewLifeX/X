using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Threading;

namespace NewLife.Configuration;

/// <summary>配置中心提供者</summary>
public class HttpConfigProvider : ConfigProvider
{
    #region 属性
    /// <summary>服务器</summary>
    public String Server { get; set; } = null!;

    /// <summary>服务操作 默认:Config/GetAll</summary>
    public String Action { get; set; } = "Config/GetAll";

    /// <summary>应用标识</summary>
    public String AppId { get; set; } = null!;

    /// <summary>应用密钥</summary>
    public String? Secret { get; set; }

    /// <summary>实例。应用可能多实例部署，ip@proccessid</summary>
    public String? ClientId { get; set; }

    /// <summary>作用域。获取指定作用域下的配置值，生产、开发、测试 等</summary>
    public String? Scope { get; set; }

    /// <summary>本地缓存配置数据。即使网络断开，仍然能够加载使用本地数据，默认Encrypted</summary>
    public ConfigCacheLevel CacheLevel { get; set; } = ConfigCacheLevel.Encrypted;

    /// <summary>更新周期。默认60秒，0秒表示不做自动更新</summary>
    public Int32 Period { get; set; } = 60;

    /// <summary>Api客户端</summary>
    public IApiClient? Client { get; set; }

    /// <summary>服务器信息。配置中心最后一次接口响应，包含配置数据以外的其它内容</summary>
    public IDictionary<String, Object?>? Info { get; set; }

    /// <summary>需要忽略改变的键。这些键的改变不产生改变事件</summary>
    public ICollection<String> IgnoreChangedKeys { get; } = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

    private Int32 _version;
    private IDictionary<String, Object?>? _cache;
    #endregion

    #region 构造
    /// <summary>实例化Http配置提供者，对接星尘和阿波罗等配置中心</summary>
    public HttpConfigProvider()
    {
        try
        {
            var executing = AssemblyX.Create(Assembly.GetExecutingAssembly());
            var asm = AssemblyX.Entry ?? executing;
            if (asm != null) AppId = asm.Name;

            ValidClientId();
        }
        catch { }
    }

    private void ValidClientId()
    {
        try
        {
            // 刚启动时可能还没有拿到本地IP
            if (ClientId.IsNullOrEmpty() || ClientId[0] == '@')
                ClientId = $"{NetHelper.MyIP()}@{Process.GetCurrentProcess().Id}";
        }
        catch { }
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _timer.TryDispose();
        Client.TryDispose();
    }

    /// <summary>已重载。输出友好信息</summary>
    /// <returns></returns>
    public override String ToString() => $"{GetType().Name} AppId={AppId} Server={Server}";
    #endregion

    #region 方法
    /// <summary>获取客户端</summary>
    /// <returns></returns>
    protected IApiClient GetClient()
    {
        if (Server.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Server));

        Client ??= new ApiHttpClient(Server)
        {
            Timeout = 3_000
        };

        return Client;
    }

    /// <summary>获取所有配置</summary>
    /// <returns></returns>
    protected virtual IDictionary<String, Object?>? GetAll()
    {
        var client = GetClient() as ApiHttpClient ?? throw new ArgumentNullException(nameof(Client));

        ValidClientId();

        try
        {
            var rs = client.Post<IDictionary<String, Object?>>(Action, new
            {
                appId = AppId,
                secret = Secret,
                clientId = ClientId,
                scope = Scope,
                version = _version,
                usedKeys = UsedKeys.Join(),
                missedKeys = MissedKeys.Join(),
            });
            Info = rs;

            // 增强版返回
            if (rs != null && rs.TryGetValue("configs", out var obj))
            {
                var ver = rs["version"].ToInt(-1);
                if (ver > 0) _version = ver;

                return obj as IDictionary<String, Object?>;
            }

            return rs;
        }
        catch (Exception ex)
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);

            return null;
        }
    }

    /// <summary>设置配置项，保存到服务端</summary>
    /// <param name="configs"></param>
    /// <returns></returns>
    protected virtual Int32 SetAll(IDictionary<String, Object?> configs)
    {
        ValidClientId();

        var client = GetClient() as ApiHttpClient ?? throw new ArgumentNullException(nameof(Client));
        return client.Post<Int32>("Config/SetAll", new
        {
            appId = AppId,
            secret = Secret,
            clientId = ClientId,
            configs,
        });
    }

    /// <summary>初始化提供者，如有必要，此时加载缓存文件</summary>
    /// <param name="value"></param>
    public override void Init(String? value)
    {
        // 本地缓存
        var file = (value.IsNullOrWhiteSpace() ? $"Config/httpConfig_{AppId}.json" : $"{value}_{AppId}.json").GetBasePath();
        if ((Root == null || Root.Childs == null || Root.Childs.Count == 0) && CacheLevel > ConfigCacheLevel.NoCache && File.Exists(file))
        {
            var json = File.ReadAllText(file);

            // 加密存储
            if (CacheLevel == ConfigCacheLevel.Encrypted) json = Aes.Create().Decrypt(json.ToBase64(), AppId.GetBytes()).ToStr();

            var dic = json.DecodeJson();
            if (dic != null) Root = Build(dic);
        }
    }

    /// <summary>加载配置字典为配置树</summary>
    /// <param name="configs"></param>
    /// <returns></returns>
    public virtual IConfigSection Build(IDictionary<String, Object?> configs)
    {
        // 换个对象，避免数组元素在多次加载后重叠
        var root = new ConfigSection { };
        foreach (var item in configs)
        {
            var section = root;
            if (section == null) continue;

            var ks = item.Key.Split(':');
            for (var i = 0; i < ks.Length; i++)
            {
                section = section?.GetOrAddChild(ks[i]) as ConfigSection;
            }
            if (section != null)
            {
                //var section = root.GetOrAddChild(key);
                if (item.Value is IDictionary<String, Object?> dic)
                    section.Childs = Build(dic).Childs;
                else
                    section.Value = item.Value + "";
            }
        }
        return root;
    }

    private Int32 _inited;
    /// <summary>加载配置</summary>
    public override Boolean LoadAll()
    {
        try
        {
            // 首次访问，加载配置
            if (_inited == 0 && Interlocked.CompareExchange(ref _inited, 1, 0) == 0)
                Init(null);
        }
        catch { }

        try
        {
            IsNew = true;

            var dic = GetAll();
            if (dic != null)
            {
                if (dic.Count > 0) IsNew = false;

                Root = Build(dic);

                // 缓存
                SaveCache(dic);
            }

            // 自动更新
            if (Period > 0) InitTimer();

            return true;
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);

            return false;
        }
    }

    private void SaveCache(IDictionary<String, Object?> configs)
    {
        // 缓存
        _cache = configs;

        // 本地缓存
        if (CacheLevel > ConfigCacheLevel.NoCache)
        {
            var file = $"Config/httpConfig_{AppId}.json".GetBasePath();
            var json = configs.ToJson();

            // 加密存储
            if (CacheLevel == ConfigCacheLevel.Encrypted) json = Aes.Create().Encrypt(json.GetBytes(), AppId.GetBytes()).ToBase64();

            File.WriteAllText(file.EnsureDirectory(true), json);
        }
    }

    /// <summary>保存配置树到数据源</summary>
    public override Boolean SaveAll()
    {
        if (Root.Childs == null) return false;

        var dic = new Dictionary<String, Object?>();
        foreach (var item in Root.Childs.ToArray())
        {
            if (item.Childs == null || item.Childs.Count == 0)
            {
                // 只提交修改过的设置
                var key = item.Key ?? String.Empty;
                if (_cache == null || !_cache.TryGetValue(key, out var v) || v + "" != item.Value + "")
                {
                    if (item.Comment.IsNullOrEmpty())
                        dic[key] = item.Value;
                    else
                        dic[key] = new { item.Value, item.Comment };
                }
            }
            else
            {
                foreach (var elm in item.Childs.ToArray())
                {
                    // 最多只支持两层
                    if (elm.Childs != null && elm.Childs.Count > 0) continue;

                    var key = $"{item.Key}:{elm.Key}";

                    // 只提交修改过的设置
                    if (_cache == null || !_cache.TryGetValue(key, out var v) || v + "" != elm.Value + "")
                    {
                        if (elm.Comment.IsNullOrEmpty())
                            dic[key] = elm.Value;
                        else
                            dic[key] = new { elm.Value, elm.Comment };
                    }
                }
            }
        }

        if (dic.Count > 0) return SetAll(dic) >= 0;

        return true;
    }
    #endregion

    #region 绑定
    /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
    /// <typeparam name="T">模型</typeparam>
    /// <param name="model">模型实例</param>
    /// <param name="autoReload">是否自动更新。默认true</param>
    /// <param name="path">路径。配置树位置，配置中心等多对象混合使用时</param>
    public override void Bind<T>(T model, Boolean autoReload = true, String? path = null)
    {
        base.Bind<T>(model, autoReload, path);

        if (autoReload) InitTimer();
    }
    #endregion

    #region 定时
    /// <summary>定时器</summary>
    protected TimerX? _timer;
    private void InitTimer()
    {
        if (_timer != null) return;
        lock (this)
        {
            if (_timer != null) return;

            var p = Period;
            if (p <= 0) p = 60;
            _timer = new TimerX(DoRefresh, null, p * 1000, p * 1000) { Async = true };
        }
    }

    /// <summary>定时刷新配置</summary>
    /// <param name="state"></param>
    protected void DoRefresh(Object? state)
    {
        var dic = GetAll();
        if (dic == null) return;

        var changed = new Dictionary<String, Object?>();
        if (_cache != null)
        {
            if (_cache.TryGetValue("configs", out var dic1) && dic1 is IDictionary<String, Object?> configs1 &&
                dic.TryGetValue("configs", out var dic2) && dic2 is IDictionary<String, Object?> configs2)
            {
                foreach (var item in configs2)
                {
                    if (IgnoreChangedKeys.Contains(item.Key)) continue;

                    if (!configs1.TryGetValue(item.Key, out var v) || v + "" != item.Value + "")
                    {
                        changed.Add(item.Key, item.Value);
                    }
                }
            }
            else
            {
                foreach (var item in dic)
                {
                    if (IgnoreChangedKeys.Contains(item.Key)) continue;

                    if (!_cache.TryGetValue(item.Key, out var v) || v + "" != item.Value + "")
                    {
                        changed.Add(item.Key, item.Value);
                    }
                }
            }
        }

        if (changed.Count > 0)
        {
            XTrace.WriteLine("[{0}]配置改变，重新加载如下键：{1}", AppId, changed.ToJson());

            Root = Build(dic);

            // 缓存
            SaveCache(dic);

            NotifyChange();
        }
    }
    #endregion
}