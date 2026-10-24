using NewLife;
using NewLife.Log;

namespace Mobile.Stardust;

/// <summary>星尘移动代理入口。默认连接 star.newlifex.com:6600，供界面开关调用</summary>
public static class MobileStardust
{
    private static readonly Object _lock = new();
    private static MobileAgent? _agent;
    private static String _status = "未连接";

    /// <summary>是否已启动</summary>
    public static Boolean IsRunning => _agent?.Started == true;

    /// <summary>当前状态</summary>
    public static String Status => _status;

    /// <summary>当前代理</summary>
    public static MobileAgent? Agent => _agent;

    /// <summary>启动代理。连接过程在后台线程完成</summary>
    /// <param name="log">状态日志回调</param>
    public static void Start(Action<String>? log = null)
    {
        lock (_lock)
        {
            if (_agent != null) return;

            try
            {
                var agent = new MobileAgent(new CoreMobilePlatform());
                if (agent.Setting.Server.IsNullOrEmpty()) agent.Setting.Server = "http://star.newlifex.com:6600";

                agent.Client.OnLogined += (_, _) =>
                {
                    _status = "已登录：" + agent.Client.Code;
                    Write(log, _status);
                };

                _agent = agent;
                _status = "连接中：" + agent.Setting.Server;
                Write(log, _status);

                Task.Run(async () =>
                {
                    try
                    {
                        agent.Start();

                        // 登录是异步过程，稍后检查一次结果；成功后由 OnLogined 更新状态
                        await Task.Delay(30_000).ConfigureAwait(false);
                        if (_agent == agent && !agent.Client.Logined)
                        {
                            _status = "登录超时：" + (agent.LastError ?? "仍在重试");
                            Write(log, _status);
                        }
                    }
                    catch (Exception ex)
                    {
                        _status = "连接失败：" + ex.Message;
                        Write(log, _status);
                    }
                });
            }
            catch (Exception ex)
            {
                _status = "初始化失败：" + ex.Message;
                Write(log, _status);
            }
        }
    }

    /// <summary>停止代理</summary>
    /// <param name="log">状态日志回调</param>
    public static void Stop(Action<String>? log = null)
    {
        MobileAgent? agent;
        lock (_lock)
        {
            agent = _agent;
            _agent = null;
        }
        if (agent == null) return;

        try { agent.Stop(); } catch { }
        try { agent.TryDispose(); } catch { }

        _status = "已断开";
        Write(log, _status);
    }

    /// <summary>切换连接状态</summary>
    /// <param name="log">状态日志回调</param>
    /// <returns>切换后是否处于启动状态</returns>
    public static Boolean Toggle(Action<String>? log = null)
    {
        if (_agent != null)
        {
            Stop(log);
            return false;
        }

        Start(log);
        return true;
    }

    private static void Write(Action<String>? log, String message)
    {
        var line = "[Stardust] " + message;
        try { XTrace.WriteLine(line); } catch { }
        try { log?.Invoke(line); } catch { }
    }
}
