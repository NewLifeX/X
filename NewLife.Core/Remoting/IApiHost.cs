using System;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Remoting
{
    /// <summary>Api主机</summary>
    public interface IApiHost
    {
        /// <summary>编码器</summary>
        IEncoder Encoder { get; set; }

        ///// <summary>处理器</summary>
        //IApiHandler Handler { get; set; }

        ///// <summary>接口动作管理器</summary>
        //IApiManager Manager { get; }

        /// <summary>获取消息编码器。重载以指定不同的封包协议</summary>
        /// <returns></returns>
        IHandler GetMessageCodec();

        ///// <summary>处理消息</summary>
        ///// <param name="session"></param>
        ///// <param name="msg"></param>
        ///// <returns></returns>
        //IMessage Process(IApiSession session, IMessage msg);

        ///// <summary>调用统计</summary>
        //ICounter StatInvoke { get; set; }

        ///// <summary>处理统计</summary>
        //ICounter StatProcess { get; set; }

        ///// <summary>慢追踪。远程调用或处理时间超过该值时，输出慢调用日志，默认5000ms</summary>
        //Int32 SlowTrace { get; set; }

        /// <summary>日志</summary>
        ILog Log { get; set; }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void WriteLog(String format, params Object[] args);
    }

    ///// <summary>Api主机助手</summary>
    //public static class ApiHostHelper
    //{
    //    /// <summary>创建控制器实例</summary>
    //    /// <param name="host"></param>
    //    /// <param name="session"></param>
    //    /// <param name="api"></param>
    //    /// <returns></returns>
    //    public static Object CreateController(this IApiHost host, IApiSession session, ApiAction api)
    //    {
    //        var controller = api.Controller;
    //        if (controller != null) return controller;

    //        controller = api.Type.CreateInstance();

    //        return controller;
    //    }

    //    #region 统计
    //    /// <summary>获取统计信息</summary>
    //    /// <param name="host"></param>
    //    /// <returns></returns>
    //    public static String GetStat(this IApiHost host)
    //    {
    //        if (host == null) return null;

    //        var sb = Pool.StringBuilder.Get();
    //        var pf1 = host.StatInvoke;
    //        var pf2 = host.StatProcess;
    //        if (pf1 != null && pf1.Value > 0) sb.AppendFormat("请求：{0} ", pf1);
    //        if (pf2 != null && pf2.Value > 0) sb.AppendFormat("处理：{0} ", pf2);

    //        if (host is ApiServer svr && svr.Server is NetServer ns)
    //            sb.Append(ns.GetStat());
    //        else if (host is ApiClient ac)
    //        {
    //            var st1 = ac.StatSend;
    //            var st2 = ac.StatReceive;
    //            if (st1 != null && st1.Value > 0) sb.AppendFormat("发送：{0} ", st1);
    //            if (st2 != null && st2.Value > 0) sb.AppendFormat("接收：{0} ", st2);
    //        }

    //        return sb.Put(true);
    //    }
    //    #endregion
    //}
}