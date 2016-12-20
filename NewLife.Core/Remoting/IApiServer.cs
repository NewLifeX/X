using System;
using NewLife.Log;

namespace NewLife.Remoting
{
    /// <summary>应用接口服务器接口</summary>
    public interface IApiServer: IServiceProvider
    {
        /// <summary>Api服务器主机</summary>
        IServiceProvider Host { get; set; }

        /// <summary>编码器</summary>
        IEncoder Encoder { get; set; }

        /// <summary>处理器</summary>
        IApiHandler Handler { get; set; }

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