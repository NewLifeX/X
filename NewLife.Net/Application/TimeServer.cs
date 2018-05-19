using System;

namespace NewLife.Net.Application
{
    /// <summary>Time服务器</summary>
    public class TimeServer : NetServer
    {
        /// <summary>实例化一个Time服务。向请求者返回1970年1月1日以来的所有秒数</summary>
        public TimeServer()
        {
            // 默认37端口
            Port = 37;

            Name = "Time服务";
        }

        /// <summary>已重载。</summary>
        /// <param name="session"></param>
        protected override INetSession OnNewSession(ISocketSession session)
        {
            WriteLog("Time {0}", session.Remote);

            var s = DateTime.Now.ToInt();
            var buf = s.GetBytes(false);
            session.Send(buf);

            return null;
        }
    }
}