using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Remoting
{
    /// <summary>应用接口服务器接口</summary>
    public interface IApiServer
    {
        /// <summary>初始化</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        Boolean Init(String config);

        /// <summary>开始</summary>
        void Start();

        /// <summary>停止</summary>
        void Stop();

        /// <summary>日志</summary>
        ILog Log { get; set; }
    }
}