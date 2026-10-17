using NewLife;
using NewLife.Configuration;
using NewLife.Log;

namespace Mobile.Stardust;

/// <summary>移动端星尘代理。负责登录、心跳与指令处理，宿主只需提供平台适配</summary>
/// <example>
/// <code>
/// var agent = new MobileAgent(new AndroidPlatform(context));
/// agent.Start();
/// // iOS 后台唤醒时补一次心跳
/// await agent.PingOnceAsync();
/// </code>
/// </example>
public class MobileAgent : DisposeBase
{
    #region 静态
    private static readonly Object _initLock = new();
    private static Boolean _providerInited;

    /// <summary>初始化配置与日志目录。宿主可在创建代理前调用，以便尽早启用文件日志</summary>
    /// <param name="platform">平台适配</param>
    public static void Prepare(IMobilePlatform platform)
    {
        if (platform == null) throw new ArgumentNullException(nameof(platform));

        lock (_initLock)
        {
            if (_providerInited) return;

            // 配置与日志落到应用私有目录，避免移动端只读目录
            var dir = platform.GetDataPath();
            if (!dir.IsNullOrEmpty() && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Config<MobileAgentSetting>.Provider = new JsonConfigProvider { FileName = Path.Combine(dir, "staragent.json") };

            try
            {
                var core = NewLife.Setting.Current;
                if (core.LogPath.IsNullOrEmpty()) core.LogPath = Path.Combine(dir, "Log");
                if (core.DataPath.IsNullOrEmpty()) core.DataPath = Path.Combine(dir, "Data");
            }
            catch { }

            _providerInited = true;
        }
    }
    #endregion

    #region 属性
    /// <summary>平台适配</summary>
    public IMobilePlatform Platform { get; }

    /// <summary>代理配置</summary>
    public MobileAgentSetting Setting { get; }

    /// <summary>星尘客户端</summary>
    public MobileStarClient Client { get; }

    /// <summary>是否已启动</summary>
    public Boolean Started { get; private set; }

    /// <summary>最后心跳时间</summary>
    public DateTime LastPingTime { get; private set; }

    /// <summary>最后错误信息</summary>
    public String? LastError { get; private set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="platform">平台适配</param>
    public MobileAgent(IMobilePlatform platform)
    {
        Platform = platform ?? throw new ArgumentNullException(nameof(platform));

        Prepare(platform);

        Setting = MobileAgentSetting.Current;
        if (Setting.IsNew) Setting.Save();

        Client = new MobileStarClient(Setting, platform)
        {
            ProductCode = Setting.ProductCode.IsNullOrEmpty() ? "StarAgent-Mobile" : Setting.ProductCode,
            Log = XTrace.Log,
        };

        // 心跳定时器在登录成功后才创建，此时再应用自定义周期
        Client.OnLogined += (_, _) =>
        {
            Client.PingPeriod = GetPingPeriod();
            XTrace.WriteLine($"[{Platform.Name}] 登录成功，节点编码：{Client.Code}");
        };
    }
    #endregion

    #region 方法
    /// <summary>启动代理。内部完成登录并开启心跳定时器</summary>
    public void Start()
    {
        if (Started) return;
        Started = true;

        try
        {
            XTrace.WriteLine($"[{Platform.Name}] 启动代理，服务端：{Setting.Server}");
            Client.Open();
            LastError = null;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            XTrace.WriteLine($"[{Platform.Name}] 启动失败：{ex.Message}");
        }
    }

    /// <summary>立即执行一次心跳。适用于 iOS 后台唤醒等场景，同时会拉取并执行待处理指令</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<Boolean> PingOnceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var rs = await Client.Ping(cancellationToken).ConfigureAwait(false);
            if (rs != null)
            {
                LastPingTime = DateTime.Now;
                LastError = null;
                return true;
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }

        return false;
    }

    /// <summary>停止代理并注销会话</summary>
    public void Stop()
    {
        if (!Started) return;
        Started = false;

        try { Client.Logout("stop").Wait(3_000); } catch { }
        try { Client.TryDispose(); } catch { }
    }

    private Int32 GetPingPeriod() => Math.Max(10, Setting.HeartbeatPeriod) * 1000;
    #endregion

    #region 销毁
    /// <summary>销毁</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        if (disposing) Stop();

        base.Dispose(disposing);
    }
    #endregion
}