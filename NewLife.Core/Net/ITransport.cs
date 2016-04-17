using System;

namespace NewLife.Net
{
    /// <summary>帧数据传输接口</summary>
    /// <remarks>实现者确保数据以包的形式传输，屏蔽数据的粘包和拆包</remarks>
    public interface ITransport : IDisposable
    {
        /// <summary>打开</summary>
        Boolean Open();

        /// <summary>关闭</summary>
        Boolean Close();

        /// <summary>写入数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        Boolean Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

        /// <summary>读取指定长度的数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

        /// <summary>开始异步接收，数据将在<see cref="Received"/>中返回</summary>
        Boolean ReceiveAsync();

        /// <summary>数据到达事件</summary>
        event EventHandler<ReceivedEventArgs> Received;
    }
}