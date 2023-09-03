using NewLife.Collections;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Net;
using NewLife.Threading;

namespace NewLife.Remoting;

/// <summary>应用接口服务器</summary>
public class ApiServer : ApiHost, IServer
{
    #region 属性
    /// <summary>是否正在工作</summary>
    public Boolean Active { get; private set; }

    /// <summary>端口</summary>
    public Int32 Port { get; set; }

    /// <summary>处理器</summary>
    public IApiHandler Handler { get; set; }

    /// <summary>服务器</summary>
    public IApiServer Server { get; set; }

    /// <summary>连接复用。默认true，单个Tcp连接在处理某个请求未完成时，可以接收并处理新的请求</summary>
    public Boolean Multiplex { get; set; } = true;

    /// <summary>是否使用Http状态。默认false，使用json包装响应码</summary>
    public Boolean UseHttpStatus { get; set; }

    /// <summary>收到请求或响应时</summary>
    public event EventHandler<ApiReceivedEventArgs> Received;

    /// <summary>服务提供者。创建控制器实例时使用，可实现依赖注入。务必在注册控制器之前设置该属性</summary>
    public IServiceProvider ServiceProvider { get; set; } = ObjectContainer.Provider;

    /// <summary>处理统计</summary>
    public ICounter StatProcess { get; set; }

    /// <summary>性能跟踪器</summary>
    public ITracer Tracer { get; set; } = DefaultTracer.Instance;
    #endregion

    #region 构造
    /// <summary>实例化一个应用接口服务器</summary>
    public ApiServer()
    {
        var type = GetType();
        Name = type.GetDisplayName() ?? type.Name.TrimEnd("Server");

        Manager = new ApiManager(this);

        // 注册默认服务控制器
        Register(new ApiController { Host = this }, null);
    }

    /// <summary>使用指定端口实例化网络服务应用接口提供者</summary>
    /// <param name="port"></param>
    public ApiServer(Int32 port) : this() => Port = port;

    /// <summary>实例化</summary>
    /// <param name="uri"></param>
    public ApiServer(NetUri uri) : this() => Use(uri);

    /// <summary>销毁时停止服务</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _Timer.TryDispose();

        Stop(GetType().Name + (disposing ? "Dispose" : "GC"));
    }
    #endregion

    #region 控制器管理
    /// <summary>接口动作管理器</summary>
    public IApiManager Manager { get; }

    /// <summary>注册服务提供类。该类的所有公开方法将直接暴露</summary>
    /// <typeparam name="TService"></typeparam>
    public void Register<TService>() => Manager.Register<TService>();

    /// <summary>注册服务</summary>
    /// <param name="controller">控制器对象</param>
    /// <param name="method">动作名称。为空时遍历控制器所有公有成员方法</param>
    public void Register(Object controller, String method) => Manager.Register(controller, method);

    /// <summary>显示可用服务</summary>
    protected virtual void ShowService()
    {
        var ms = Manager.Services;
        if (ms.Count > 0)
        {
            Log.Info("可用服务{0}个：", ms.Count);
            var max = ms.Max(e => e.Key.Length);
            foreach (var item in ms)
            {
                Log.Info("\t{0,-" + (max + 1) + "}{1}\t{2}", item.Key, item.Value, item.Value.Type.FullName);
            }
        }
    }
    #endregion

    #region 启动停止
    /// <summary>添加服务器</summary>
    /// <param name="uri"></param>
    public IApiServer Use(NetUri uri)
    {
        var svr = uri.Type == NetType.Http ? new ApiHttpServer() : new ApiNetServer();

        if (!svr.Init(uri, this)) return null;

        Server = svr;

        return svr;
    }

    /// <summary>确保已创建服务器对象</summary>
    /// <returns></returns>
    public IApiServer EnsureCreate()
    {
        var svr = Server;
        if (svr != null) return svr;

        if (Port <= 0) throw new ArgumentNullException(nameof(Server), "未指定服务器Server，且未指定端口Port！");

        svr = new ApiNetServer
        {
            Host = this,
            Tracer = Tracer,
        };
        svr.Init(new NetUri(NetType.Unknown, "*", Port), this);

        return Server = svr;
    }

    /// <summary>开始服务</summary>
    public virtual void Start()
    {
        if (Active) return;

        Encoder ??= new JsonEncoder();
        //if (Encoder == null) Encoder = new BinaryEncoder();
        Handler ??= new ApiHandler { Host = this };

        Encoder.Log = EncoderLog;

        Log.Info("启动{0}，服务器 {1}", GetType().Name, Server);
        Log.Info("编码：{0}", Encoder);
        Log.Info("处理：{0}", Handler);

        var svr = EnsureCreate();

        svr.Host = this;
        svr.Log = Log;
        svr.Start();

        ShowService();

        var ms = StatPeriod * 1000;
        if (ms > 0)
        {
            //if (StatInvoke == null) StatInvoke = new PerfCounter();
            StatProcess ??= new PerfCounter();

            _Timer = new TimerX(DoStat, null, ms, ms) { Async = true };
        }

        Active = true;
    }

    /// <summary>停止服务</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    public virtual void Stop(String reason)
    {
        if (!Active) return;

        Log.Info("停止{0} {1}", GetType().Name, reason);
        Server.Stop(reason ?? (GetType().Name + "Stop"));

        Active = false;
    }
    #endregion

    #region 请求处理
    /// <summary>处理会话收到的消息，并返回结果消息</summary>
    /// <remarks>
    /// 这里是网络RPC的消息处理核心，目标协议只要能封装为IMessage，即可通过重载该方法得到支持
    /// </remarks>
    /// <param name="session">网络会话</param>
    /// <param name="msg">消息</param>
    /// <returns>要应答对方的消息，为空表示不应答</returns>
    internal protected virtual IMessage Process(IApiSession session, IMessage msg)
    {
        if (msg.Reply) return null;

        ApiMessage message = null;
        ISpan span = null;
        var st = StatProcess;
        var sw = st.StartCount();
        try
        {
            var enc = session["Encoder"] as IEncoder ?? Encoder;

            Object result;
            Packet args = null;
            try
            {
                message = enc.Decode(msg);
                if (message == null) return null;

                // 根据动作名，开始跟踪
                span = Tracer?.NewSpan("rps:" + message.Action, args);

                Received?.Invoke(this, new ApiReceivedEventArgs { Session = session, Message = msg, ApiMessage = message });

                result = OnProcess(session, message.Action, args, msg);
            }
            catch (Exception ex)
            {
                ex = ex.GetTrue();

                if (ShowError) WriteLog("{0}", ex);

                // 支持自定义错误
                if (ex is ApiException aex)
                {
                    message.Code = aex.Code;
                    result = ex?.Message;
                }
                else
                {
                    message.Code = 500;
                    result = ex?.Message;
                }

                // 跟踪异常
                span?.SetError(ex, args?.ToStr());
            }

            // 单向请求无需响应
            if (msg.OneWay) return null;

            // 处理http封包方式
            if (enc is HttpEncoder httpEncoder) httpEncoder.UseHttpStatus = UseHttpStatus;

            return enc.CreateResponse(msg, message.Action, message.Code, result);
        }
        finally
        {
            var msCost = st.StopCount(sw) / 1000;
            if (SlowTrace > 0 && msCost >= SlowTrace) WriteLog($"慢处理[{message.Action}]，Code={message.Code}，耗时{msCost:n0}ms");

            span?.Dispose();
        }
    }

    /// <summary>执行消息处理，交给Handler</summary>
    /// <param name="session">会话</param>
    /// <param name="action">动作</param>
    /// <param name="args">参数</param>
    /// <param name="msg">消息</param>
    /// <returns></returns>
    protected virtual Object OnProcess(IApiSession session, String action, Packet args, IMessage msg) => Handler.Execute(session, action, args, msg);
    #endregion

    #region 统计
    private TimerX _Timer;
    private String _Last;

    /// <summary>显示统计信息的周期。默认600秒，0表示不显示统计信息</summary>
    public Int32 StatPeriod { get; set; } = 600;

    private void DoStat(Object state)
    {
        var sb = Pool.StringBuilder.Get();
        var pf2 = StatProcess;
        if (pf2 != null && pf2.Value > 0) sb.AppendFormat("处理：{0} ", pf2);

        if (Server is NetServer ns)
            sb.Append(ns.GetStat());

        var msg = sb.Put(true);
        //var msg = this.GetStat();
        if (msg.IsNullOrEmpty() || msg == _Last) return;
        _Last = msg;

        WriteLog(msg);
    }
    #endregion
}