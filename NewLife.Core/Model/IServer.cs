using System;

namespace NewLife.Model
{
    /// <summary>服务接口。</summary>
    /// <remarks>服务代理XAgent可以附加代理实现了IServer接口的服务。</remarks>
    public interface IServer
    {
        /// <summary>开始</summary>
        void Start();

        /// <summary>停止</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        void Stop(String reason);
    }
}