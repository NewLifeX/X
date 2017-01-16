using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewLife.Remoting
{
    /// <summary>API控制器</summary>
    public class ApiController
    {
        /// <summary>主机</summary>
        public IApiHost Host { get; set; }

        /// <summary>获取所有接口</summary>
        /// <returns></returns>
        public String[] All()
        {
            return Host.Manager.Services.Keys.ToArray();
        }

        /// <summary>接口详细信息</summary>
        /// <param name="api"></param>
        /// <returns></returns>
        public String Detail(String api)
        {
            var sb = new StringBuilder();
            foreach (var item in api.Split(","))
            {
                ApiAction act = null;
                if (!Host.Manager.Services.TryGetValue(item, out act)) return null;

                if (sb.Length > 0) sb.Append("#");

                var mi = act.Method;

                sb.AppendFormat("{0} {1}", mi.ReturnType.Name, act.Name);
                sb.Append("(");

                var pis = mi.GetParameters();
                for (int i = 0; i < pis.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.AppendFormat("{0} {1}", pis[i].ParameterType.Name, pis[i].Name);
                }

                sb.Append(")");

                var des = mi.GetDescription();
                if (!des.IsNullOrEmpty()) sb.AppendFormat(" {0}", des);
            }

            return sb.ToString();
        }
    }
}