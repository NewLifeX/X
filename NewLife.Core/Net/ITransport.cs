using System;
using NewLife.Data;

namespace NewLife.Net
{
    /// <summary>帧数据传输接口</summary>
    /// <remarks>实现者确保数据以包的形式传输，屏蔽数据的粘包和拆包</remarks>
    public interface ITransport : IDisposable
    {
        /// <summary>超时</summary>
        Int32 Timeout { get; set; }

        /// <summary>打开</summary>
        Boolean Open();

        /// <summary>关闭</summary>
        Boolean Close();

        /// <summary>写入数据</summary>
        /// <param name="data">数据包</param>
        Int32 Send(Packet data);

        /// <summary>读取数据</summary>
        /// <returns></returns>
        Packet Receive();

        /// <summary>数据到达事件</summary>
        event EventHandler<ReceivedEventArgs> Received;
    }
}