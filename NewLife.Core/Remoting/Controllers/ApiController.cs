using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Http;
using NewLife.Net;
using NewLife.Reflection;

namespace NewLife.Remoting;

/// <summary>API控制器</summary>
public class ApiController : IApi
{
    /// <summary>主机</summary>
    public IApiHost Host { get; set; }

    /// <summary>会话</summary>
    public IApiSession Session { get; set; }

    static ApiController()
    {
        RefreshLocalIP();

        NetworkChange.NetworkAddressChanged += (s, e) => RefreshLocalIP();
        NetworkChange.NetworkAvailabilityChanged += (s, e) => RefreshLocalIP();
    }

    static void RefreshLocalIP() => _LocalIP = NetHelper.GetIPs().Where(e => e.IsIPv4()).Join();

    private String[] _all;
    /// <summary>获取所有接口</summary>
    /// <returns></returns>
    public String[] All()
    {
        // 加上10ms延迟来模拟业务损耗，测试消耗占95.63%。没加睡眠时，Json损耗占55.5%
        //System.Threading.Thread.Sleep(1000);
        if (_all != null) return _all;

        var svc = Host as ApiServer;
        var list = new List<String>();
        foreach (var item in svc.Manager.Services)
        {
            var act = item.Value;

            var mi = act.Method;

            var sb = Pool.StringBuilder.Get();
            sb.AppendFormat("{0} {1}", mi.ReturnType.Name, act.Name);
            sb.Append('(');

            var pis = mi.GetParameters();
            for (var i = 0; i < pis.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.AppendFormat("{0} {1}", pis[i].ParameterType.Name, pis[i].Name);
            }

            sb.Append(')');

            var des = mi.GetDescription();
            if (!des.IsNullOrEmpty()) sb.AppendFormat(" {0}", des);

            list.Add(sb.Put(true));
        }

        return _all = list.ToArray();
    }

    private static readonly Int32 _pid = Process.GetCurrentProcess().Id;
    //private readonly static String _OS = Environment.OSVersion + "";
    private static readonly String _MachineName = Environment.MachineName;
    //private readonly static String _UserName = Environment.UserName;
    private static String _LocalIP;
    /// <summary>服务器信息，用户健康检测</summary>
    /// <param name="state">状态信息</param>
    /// <returns></returns>
    public Object Info(String state)
    {
        var ctx = ControllerContext.Current;
        var ps = ctx?.Parameters;
        var ns = ctx?.Session as INetSession;
        if (ns == null && DefaultHttpContext.Current is IHttpContext http)
        {
            ps = http.Parameters;
            ns = http.Connection;
        }

        var asmx = AssemblyX.Entry;
        var asmx2 = AssemblyX.Create(Assembly.GetExecutingAssembly());
        var mi = MachineInfo.Current;

        var rs = new
        {
            Id = _pid,
            asmx?.Name,
            asmx?.Title,
            asmx?.FileVersion,
            asmx?.Compile,
            OS = mi?.OSName,
            MachineName = _MachineName,
            //UserName = _UserName,
            ApiVersion = asmx2?.FileVersion,

            LocalIP = _LocalIP,
            Remote = ns?.Remote?.EndPoint + "",
            State = state,
            //LastState = Session?["State"],
            Time = DateTime.Now,
        };

        // 记录上一次状态
        if (Session != null) Session["State"] = state;

        // 转字典
        var dic = rs.ToDictionary();

        // 令牌
        if (Session != null && !Session.Token.IsNullOrEmpty())
        {
            dic["Token"] = Session.Token;

            // 时间和连接数
            if (Host is ApiHost ah) dic["Uptime"] = (DateTime.Now - ah.StartTime).ToString();
            if (Host is ApiServer svr && svr.Server is NetServer nsvr)
            {
                dic["Port"] = nsvr.Port;
                dic["Online"] = nsvr.SessionCount;
                dic["MaxOnline"] = nsvr.MaxSessionCount;
            }

            //// 进程
            //dic["Process"] = GetProcess();

            // 加上统计信息
            dic["Stat"] = GetStat();
        }
        else if (ps != null && ps.TryGetValue("Token", out var token) && token + "" != "")
            dic["Token"] = token;

        return dic;
    }

    private Object GetStat()
    {
        if (Host is not ApiServer svc) return null;

        var dic = new Dictionary<String, Object>
        {
            ["_Total"] = svc.StatProcess + ""
        };
        foreach (var item in svc.Manager.Services)
        {
            var api = item.Value;
            dic[item.Key] = api.StatProcess + " " + api.LastSession;
        }

        return dic;
    }
}