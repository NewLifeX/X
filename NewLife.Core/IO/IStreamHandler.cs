using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.IO
{
    /// <summary>
    /// 数据流处理器接口
    /// </summary>
    public interface IStreamHandler
    {
        //event EventHandler<EventArgs<Stream>> Received;

        //void Send(Byte[] buffer, Int32 offset, Int32 size);

        /// <summary>
        /// 处理数据流
        /// </summary>
        /// <param name="stream"></param>
        void Process(Stream stream);

        /// <summary>
        /// 是否可以重用
        /// </summary>
        Boolean IsReusable { get; }
    }
}