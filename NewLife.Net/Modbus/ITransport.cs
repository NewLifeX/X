using System;

namespace NewLife.Net
{
    /// <summary>帧数据传输接口</summary>
    /// <remarks>实现者确保数据以包的形式传输，屏蔽数据的粘包和拆包</remarks>
    public interface ITransport : IDisposable
    {
        /// <summary>读取的期望帧长度，小于该长度为未满一帧，读取不做返回</summary>
        Int32 FrameSize { get; set; }

        /// <summary>打开</summary>
        void Open();

        /// <summary>关闭</summary>
        void Close();

        /// <summary>写入数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        void Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

        /// <summary>读取指定长度的数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

        /// <summary>开始异步接收，数据将在<see cref="Received"/>中返回</summary>
        void ReceiveAsync();

        /// <summary>数据到达事件</summary>
        event TransportEventHandler Received;
    }

    /// <summary>传输口数据到达委托</summary>
    /// <param name="transport">传输口</param>
    /// <param name="data">收到的数据</param>
    /// <returns>要发回去的数据</returns>
    public delegate Byte[] TransportEventHandler(ITransport transport, Byte[] data);
}