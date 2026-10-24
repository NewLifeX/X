using System.Runtime.InteropServices;
using NewLife;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Remoting.Clients;
using NewLife.Remoting.Models;
using Stardust;
using Stardust.Models;

namespace Mobile.Stardust;

/// <summary>移动端星尘客户端。登录与心跳只上报平台相关信息，避免枚举进程、磁盘等移动端受限能力</summary>
public class MobileStarClient : StarClient
{
    private readonly MobileAgentSetting _setting;

    /// <summary>平台适配</summary>
    public IMobilePlatform Platform { get; }

    /// <summary>实例化</summary>
    /// <param name="setting">代理配置</param>
    /// <param name="platform">平台适配</param>
    public MobileStarClient(MobileAgentSetting setting, IMobilePlatform platform) : base(setting)
    {
        _setting = setting;
        Platform = platform;
        ProductCode = setting.ProductCode.IsNullOrEmpty() ? "StarAgent-Mobile" : setting.ProductCode;
    }

    /// <summary>构建登录请求</summary>
    /// <returns></returns>
    public override ILoginRequest BuildLoginRequest()
    {
        // 确保密码提供者已初始化，避免直接调用时以明文发送密钥
        Init();

        var request = new LoginInfo();
        FillLoginRequest(request);

        request.ProductCode = ProductCode;
        request.Node = BuildNodeInfo();

        return request;
    }

    /// <summary>登录。服务端密钥校验失败时清空本地密钥重试一次，触发动态注册</summary>
    /// <param name="source">登录来源</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录响应</returns>
    public override async Task<ILoginResponse?> Login(String? source = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.Login(source, cancellationToken).ConfigureAwait(false);
        }
        catch (ApiException ex) when (IsSecretError(ex) && !Secret.IsNullOrEmpty())
        {
            XTrace.WriteLine($"[{Name}]服务端密钥校验失败，清空本地密钥后重试：{ex.Message}");

            Secret = null;
            _setting.Secret = null;
            try { _setting.Save(); } catch { }

            return await base.Login(source, cancellationToken).ConfigureAwait(false);
        }
    }

    private static Boolean IsSecretError(ApiException ex) =>
        ex.Message.Contains("密钥") || ex.Message.Contains("密码") || ex.Message.Contains("鉴权");

    /// <summary>构建心跳请求。只采集移动端可用的指标</summary>
    /// <returns></returns>
    public override IPingRequest BuildPingRequest()
    {
        var request = new PingInfo
        {
            IP = AgentInfo.GetIps(),
            Uptime = (Int32)(Runtime.TickCount64 / 1000),
            Time = DateTime.UtcNow.ToLong(),
            Framework = RuntimeInformation.FrameworkDescription,
        };

        try { request.Gateway = AgentInfo.GetGateway(); } catch { }

        try { Platform.FillPingInfo(request); } catch { }

        // 移动端只有一个存储卷，按协议格式补充分区信息
        if (request.TotalSize > 0)
            request.DriveInfo = $"/={request.AvailableFreeSpace.ToGMK()}/{request.TotalSize.ToGMK()}";

        return request;
    }

    /// <summary>打开客户端并注册移动端支持的指令</summary>
    public override void Open()
    {
        this.RegisterCommand("node/restart", Restart);
        this.RegisterCommand("node/setchannel", SetChannel);
        this.RegisterCommand("node/reboot", Unsupported);
        this.RegisterCommand("node/upgrade", Unsupported);
        this.RegisterCommand("node/synctime", Unsupported);
        this.RegisterCommand("bash", Unsupported);
        this.RegisterCommand("cmd", Unsupported);

        base.Open();
    }

    private NodeInfo BuildNodeInfo()
    {
        var asm = AssemblyX.Create(typeof(MobileStarClient).Assembly);
        var osName = Platform.OSName?.Trim() ?? "";
        var osTag = osName.StartsWithIgnoreCase("iOS") ? "os:ios" :
            osName.StartsWithIgnoreCase("Android") ? "os:android" : "os:" + osName.ToLowerInvariant();

        var info = new NodeInfo
        {
            Version = asm?.FileVersion ?? typeof(MobileStarClient).Assembly.GetName().Version + "",
            Compile = asm?.Compile ?? DateTime.MinValue,
            OSName = Platform.OSName,
            OSVersion = Platform.OSVersion,
            Architecture = Platform.Architecture,
            MachineName = Platform.DeviceName,
            UserName = Environment.UserName,
            IP = AgentInfo.GetIps(),
            ProcessorCount = Environment.ProcessorCount,
            Runtime = Environment.Version + "",
            Framework = RuntimeInformation.FrameworkDescription,
            Time = DateTime.UtcNow,
            UUID = Platform.DeviceId,
            MachineGuid = Platform.DeviceId,
            InstallPath = AppContext.BaseDirectory,
            Plugins = ["agent:dotnet-mobile", "cap:exec=false", "cap:upgrade=false", "cap:deploy=false", osTag],
        };

        try { info.Gateway = AgentInfo.GetGateway(); } catch { }

        try
        {
            var tz = TimeZoneInfo.Local;
            info.TimeZone = tz.Id;
        }
        catch { }

        try { Platform.FillNodeInfo(info); } catch { }

        if (info.TotalSize > 0)
            info.DriveInfo = $"/={info.AvailableFreeSpace.ToGMK()}/{info.TotalSize.ToGMK()}";

        return info;
    }

    private String? Restart(String? argument)
    {
        // 先返回结果，再由平台异步重启，避免指令回包丢失
        _ = Task.Run(async () =>
        {
            await Task.Delay(500).ConfigureAwait(false);
            Platform.RestartApp();
        });

        return "success";
    }

    private String? SetChannel(String? argument)
    {
        if (argument.IsNullOrEmpty()) return "参数为空";

        _setting.Channel = argument;
        _setting.Save();

        return "success " + argument;
    }

    private String? Unsupported(String? argument) => $"当前平台（{Platform.Name}）不支持该指令";
}