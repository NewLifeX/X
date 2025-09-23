using System.Net;

namespace NewLife.Data;

/// <summary>数据帧接口。用于网络通信领域，定义数据帧的必要字段</summary>
/// <remarks>
/// 该接口抽象了一次数据收发过程中的核心载体：
/// - <see cref="Packet"/> 表示尚未解码的原始二进制数据包；
/// - <see cref="Message"/> 表示对 <see cref="Packet"/> 解码后的高阶消息对象；
/// - <see cref="Remote"/> 标识对应的远端终结点（来源/目的地址）；
/// - <see cref="UserState"/> 可携带跨层透传的用户自定义上下文数据。
///
/// 约定与注意：
/// - 同一实例在不同阶段可仅设置其中一项（例如仅有 <see cref="Packet"/> 或仅有 <see cref="Message"/>）。
/// - 是否线程安全由具体实现决定，默认不保证线程安全。
/// </remarks>
public interface IData
{
    #region 属性
    /// <summary>原始数据包</summary>
    /// <remarks>网络层收到的原始二进制数据；解码后可转存为 <see cref="Message"/>。允许为空。</remarks>
    IPacket? Packet { get; set; }

    /// <summary>远程地址</summary>
    /// <remarks>接收方向：报文来源；发送方向：目标地址。允许为空。</remarks>
    IPEndPoint? Remote { get; set; }

    /// <summary>解码后的消息</summary>
    /// <remarks>从 <see cref="Packet"/> 解码得到的业务对象，或待编码发送的对象。允许为空。</remarks>
    Object? Message { get; set; }

    /// <summary>用户自定义数据</summary>
    /// <remarks>用于跨层传递额外上下文信息（如追踪标识、调用参数等）。实现不应对其内容做假设。允许为空。</remarks>
    Object? UserState { get; set; }
    #endregion
}
