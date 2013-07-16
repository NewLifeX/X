using System;

namespace NewLife.Net.Modbus
{
    /// <summary>传输接口</summary>
    public interface ITransport : IDisposable
    {
        /// <summary>打开</summary>
        void Open();

        /// <summary>关闭</summary>
        void Close();

        /// <summary>写入数据</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        void Write(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

        /// <summary>读取指定长度的数据</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        Int32 Read(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

        /// <summary>开始监听</summary>
        void Listen();

        /// <summary>数据到达事件</summary>
        event TransportEventHandler Received;
    }

    /// <summary>传输口数据到达委托</summary>
    /// <param name="transport">传输口</param>
    /// <param name="data">收到的数据</param>
    /// <returns>要发回去的数据</returns>
    public delegate Byte[] TransportEventHandler(ITransport transport, Byte[] data);
}