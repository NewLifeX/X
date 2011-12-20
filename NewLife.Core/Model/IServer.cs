using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Model
{
    /// <summary>服务接口。</summary>
    /// <remarks>服务代理XAgent可以附加代理实现了IServer接口的服务。</remarks>
    public interface IServer
    {
        /// <summary>开始</summary>
        void Start();

        /// <summary>停止</summary>
        void Stop();
    }
}