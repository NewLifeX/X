using System;
using System.Collections.Generic;
using NewLife.Collections;
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

        private readonly String _MachineName = Environment.MachineName;
        private readonly String _UserName = Environment.UserName;
        /// <summary>服务器信息，用户健康检测</summary>
        /// <param name="state">状态信息</param>
        /// <returns></returns>
        public Object Info(String state)
        {
            var ctx = ControllerContext.Current;
            var ns = ctx?.Session as INetSession;

            var rs = new
            {
                MachineNam = _MachineName,
                UserName = _UserName,
                Time = DateTime.Now,
                LocalIP = NetHelper.MyIP() + "",
                Remote = ns?.Remote?.EndPoint + "",
                State = state,
            };
            return rs;
        }
    }
}