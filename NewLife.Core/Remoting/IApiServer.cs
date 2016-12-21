using System;
using NewLife.Log;

namespace NewLife.Remoting
{
    /// <summary>应用接口服务器接口</summary>
    public interface IApiServer: IServiceProvider
    {
        /// <summary>服务提供者</summary>
        IServiceProvider Provider { get; set; }

        /// <summary>编码器</summary>
        IEncoder Encoder { get; set; }

        /// <summary>处理器</summary>
        IApiHandler Handler { get; set; }

        /// <summary>当前服务器所有会话</summary>
        IApiSession[] AllSessions { get; }

        /// <summary>初始化</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        bool Init(string config);

        /// <summary>开始</summary>
        void Start();

        /// <summary>停止</summary>
        void Stop();

        /// <summary>日志</summary>
        ILog Log { get; set; }
    }
}