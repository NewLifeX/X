using NewLife.Data;

namespace NewLife.Net;

/// <summary>流式会话。连接型会话（如 TCP）面向字节流，接收数据同时投递到数据管道供流式消费，编解码器经本接口接入管道</summary>
/// <remarks>报文式协议（如 UDP）每包即一帧，不需要流式管道，不实现本接口。</remarks>
public interface IStreamSession
{
    /// <summary>数据管道。按需创建：首次访问后，接收数据同时投递到管道供流式消费</summary>
    Pipe Pipe { get; }

    /// <summary>获取已创建的数据管道。未访问过 <see cref="Pipe"/> 时返回 null（不触发创建）</summary>
    /// <returns>数据管道；未创建时为 null</returns>
    Pipe? GetPipe();
}
