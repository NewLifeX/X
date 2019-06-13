using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    /// <summary>API控制器</summary>
    //[AllowAnonymous]
    public class ApiController
    {
        /// <summary>主机</summary>
        public IApiHost Host { get; set; }

        private String[] _all;
        /// <summary>获取所有接口</summary>
        /// <returns></returns>
        public String[] All()
        {
            // 加上10ms延迟来模拟业务损耗，测试消耗占95.63%。没加睡眠时，Json损耗占55.5%
            //System.Threading.Thread.Sleep(1000);
            if (_all != null) return _all;

            var list = new List<String>();
            foreach (var item in Host.Manager.Services)
            {
                var act = item.Value;

                var mi = act.Method;

                var sb = Pool.StringBuilder.Get();
                sb.AppendFormat("{0} {1}", mi.ReturnType.Name, act.Name);
                sb.Append("(");

                var pis = mi.GetParameters();
                for (var i = 0; i < pis.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.AppendFormat("{0} {1}", pis[i].ParameterType.Name, pis[i].Name);
                }

                sb.Append(")");

                var des = mi.GetDescription();
                if (!des.IsNullOrEmpty()) sb.AppendFormat(" {0}", des);

                list.Add(sb.Put(true));
            }

            return _all = list.ToArray();
        }

        private readonly static String _OS = Environment.OSVersion + "";
        private readonly static String _MachineName = Environment.MachineName;
        private readonly static String _UserName = Environment.UserName;
        private readonly static String _LocalIP = NetHelper.MyIP() + "";
        /// <summary>服务器信息，用户健康检测</summary>
        /// <param name="state">状态信息</param>
        /// <returns></returns>
        public Object Info(String state)
        {
            var ctx = ControllerContext.Current;
            var ns = ctx?.Session as INetSession;
            var asmx = AssemblyX.Entry;
            var asmx2 = AssemblyX.Create(Assembly.GetExecutingAssembly());

            var rs = new
            {
                Server = asmx?.Name,
                asmx?.Version,
                OS = _OS,
                MachineName = _MachineName,
                UserName = _UserName,
                ApiVersion = asmx2?.Version,

                LocalIP = _LocalIP,
                Remote = ns?.Remote?.EndPoint + "",
                State = state,
                Time = DateTime.Now,
            };

            // 转字典
            var dic = rs.ToDictionary();

            // 时间和连接数
            if (Host is ApiHost ah) dic["Uptime"] = (DateTime.Now - ah.StartTime).ToString();
            if (Host is ApiServer svr && svr.Server is NetServer nsvr)
            {
                dic["Port"] = nsvr.Port;
                dic["Online"] = nsvr.SessionCount;
                dic["MaxOnline"] = nsvr.MaxSessionCount;
            }

            // 进程
            dic["Process"] = GetProcess();

            // 加上统计信息
            dic["Stat"] = GetStat();

            return dic;
        }

        private Object GetProcess()
        {
            var proc = Process.GetCurrentProcess();

            return new
            {
                Environment.ProcessorCount,
                ProcessId = proc.Id,
                Threads = proc.Threads.Count,
                Handles = proc.HandleCount,
                WorkingSet = proc.WorkingSet64,
                PrivateMemory = proc.PrivateMemorySize64,
                GCMemory = GC.GetTotalMemory(false),
                GC0 = GC.GetGeneration(0),
                GC1 = GC.GetGeneration(1),
                GC2 = GC.GetGeneration(2),
            };
        }

        private Object GetStat()
        {
            var dic = new Dictionary<String, Object>
            {
                ["_Total"] = Host.StatProcess + ""
            };
            foreach (var item in Host.Manager.Services)
            {
                var api = item.Value;
                dic[item.Key] = api.StatProcess + " " + api.LastSession;
            }

            return dic;
        }

        private static Packet _myInfo;
        /// <summary>服务器信息，用户健康检测，二进制压测</summary>
        /// <param name="state">状态信息</param>
        /// <returns></returns>
        public Packet Info2(Packet state)
        {
            if (_myInfo == null)
            {
                // 不包含时间和远程地址
                var rs = new
                {
                    MachineNam = _MachineName,
                    UserName = _UserName,
                    LocalIP = _LocalIP,
                };
                _myInfo = new Packet(rs.ToJson().GetBytes());
            }

            var pk = _myInfo.Slice(0, -1);
            pk.Append(state);

            return pk;
        }
    }
}