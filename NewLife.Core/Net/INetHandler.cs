using NewLife.Data;

namespace NewLife.Net;

/// <summary>网络数据处理器。可作为业务处理实现，也可以作为前置协议解析</summary>
public interface INetHandler
{
    /// <summary>建立连接时初始化会话</summary>
    /// <param name="session">会话</param>
    void Init(INetSession session);

    /// <summary>处理客户端发来的数据</summary>
    /// <param name="data"></param>
    void Process(IData data);
}
