using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Remoting
{
    /// <summary>API控制器</summary>
    //[AllowAnonymous]
    public class ApiController
    {
        /// <summary>主机</summary>
        public IApiHost Host { get; set; }

        /// <summary>获取所有接口</summary>
        /// <returns></returns>
        public String[] All()
        {
            var list = new List<String>();
            //var sb = new StringBuilder();
            foreach (var item in Host.Manager.Services)
            {
                var act = item.Value;

                //if (sb.Length > 0) sb.Append("#");

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

            return list.ToArray();
        }
    }
}