using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Data;
using NewLife.Remoting;
using NewLife.Security;

namespace XApi
{
    /// <summary>API控制器</summary>
    //[AllowAnonymous]
    public class MyApiController
    {
        /// <summary>获取指定种类的环境信息</summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public String Info(String kind)
        {
            switch ((kind + "").ToLower())
            {
                case "machine": return Environment.MachineName;
                case "user": return Environment.UserName;
                case "ip": return NetHelper.MyIP() + "";
                case "time": return DateTime.Now.ToFullString();
                default:
                    throw new ApiException(505, "不支持类型" + kind);
            }
        }

        /// <summary>加密数据</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Packet Encrypt(Packet data)
        {
            //Log.XTrace.WriteLine("加密数据{0:n0}字节", data.Total);

            var buf = data.ToArray().RC4("NewLife".GetBytes());

            return buf;
        }
    }
}