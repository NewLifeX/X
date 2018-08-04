using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Data;
using NewLife.Net;

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
            //System.Threading.Thread.Sleep(10);
            if (_all != null) return _all;

            var list = new List<String>();
            foreach (var item in Host.Manager.Services)
            {
                var act = item.Value;

                var mi = act.Method;

                var sb = new StringBuilder();
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

                list.Add(sb.ToString());
            }

            return _all = list.ToArray();
        }

        /// <summary>服务器信息，用户健康检测</summary>
        /// <returns></returns>
        public Object Info()
        {
            var ctx = ControllerContext.Current;
            var ns = ctx?.Session as INetSession;

            var rs = new
            {
                Environment.MachineName,
                Environment.UserName,
                Time = DateTime.Now,
                LocalIP = NetHelper.MyIP() + "",
                Remote = ns?.Remote?.EndPoint + "",
            };
            return rs;
        }

#if DEBUG
        ///// <summary>获取指定种类的环境信息</summary>
        ///// <param name="kind"></param>
        ///// <returns></returns>
        //public String Info(String kind)
        //{
        //    switch ((kind + "").ToLower())
        //    {
        //        case "machine": return Environment.MachineName;
        //        case "user": return Environment.UserName;
        //        case "ip": return NetHelper.MyIP() + "";
        //        case "time": return DateTime.Now.ToFullString();
        //        default:
        //            throw new ApiException(505, "不支持类型" + kind);
        //    }
        //}

        ///// <summary>加密数据</summary>
        ///// <param name="data"></param>
        ///// <returns></returns>
        //public Packet Encrypt(Packet data)
        //{
        //    //Log.XTrace.WriteLine("加密数据{0:n0}字节", data.Total);

        //    var buf = Security.RC4.Encrypt(data.ToArray(), "NewLife".GetBytes());

        //    return buf;
        //}
#endif
    }
}