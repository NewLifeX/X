using System.ComponentModel;

namespace NewLife.Data;

/// <summary>数据包接口。基于内存共享理念，统一提供数据包处理能力</summary>
/// <remarks>
/// <para>常用于网络编程和协议解析，通过对象池复用内存避免大量分配和拷贝。</para>
/// <para>数据包接口一般由结构体实现以降低 GC 压力。</para>
/// <para><b>内存管理权转移规则</b>：调用栈上层（获得包的一方）负责最终释放。</para>
/// <list type="bullet">
/// <item>非阻塞 Socket：接收方申请与释放；解析逻辑只消费不负责释放</item>
/// <item>阻塞 Socket：接收函数申请，外部使用方释放，管理权可进一步传递</item>
/// </list>
/// <para>切片 <see cref="Slice(Int32, Int32)"/> 共享底层缓冲区；<see cref="OwnerPacket"/> 统一为引用计数共享语义——切片返回独立句柄，各自释放，最后一个归还内存池。</para>
/// <para><b>重要</b>：所有临时获得的 <see cref="Span{T}"/>/<see cref="Memory{T}"/> 仅在当前所有权生命周期内短暂使用，禁止缓存到异步/长期结构中。</para>
/// </remarks>
public interface IPacket
{
    /// <summary>数据长度。仅当前数据包，不包括 <see cref="Next"/></summary>
    Int32 Length { get; }

    /// <summary>下一个链式包</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    IPacket? Next { get; set; }

    /// <summary>总长度。包括 <see cref="Next"/> 链的长度</summary>
    Int32 Total { get; }

    /// <summary>获取/设置 指定绝对位置的字节（跨越链式包）</summary>
    /// <param name="index">0 基起始的全局位置</param>
    Byte this[Int32 index] { get; set; }

    /// <summary>获取分片视图（仅当前数据包，不包括 <see cref="Next"/> 链）。在管理权生命周期内短暂使用，禁止长期保存</summary>
    Span<Byte> GetSpan();

    /// <summary>获取内存块（仅当前数据包，不包括 <see cref="Next"/> 链）。在管理权生命周期内短暂使用，禁止长期保存</summary>
    Memory<Byte> GetMemory();

    /// <summary>切片得到新数据包，共享底层缓冲区以减少分配</summary>
    /// <remarks>
    /// <para><see cref="OwnerPacket"/> 实现为引用计数共享：返回独立句柄，与原包同时可用；各自 <c>Dispose</c>，最后一个释放时归还内存池。</para>
    /// <para>取出子窗口后不再使用原句柄时应随即释放（如拆帧后丢弃帧容器），避免句柄引用残留导致缓冲无法归池。</para>
    /// <para>结构体实现（<see cref="ArrayPacket"/> 等）返回无所有权的视图：无需释放，仅可在原数据生命周期内短暂使用。</para>
    /// </remarks>
    /// <param name="offset">相对当前包起始偏移</param>
    /// <param name="count">个数。默认 -1 表示到末尾</param>
    IPacket Slice(Int32 offset, Int32 count = -1);

    /// <summary>切片得到新数据包（兼容重载），共享底层缓冲区以减少分配</summary>
    /// <remarks>
    /// <para>为兼容基于三参签名编译的旧版库（历史版本的 Remoting/WebSocket 等）而保留，行为直接转发到两参重载。</para>
    /// <para>引用计数共享模型下不再区分“转移”与“借用”，新代码请使用 <see cref="Slice(Int32, Int32)"/>。</para>
    /// </remarks>
    /// <param name="offset">相对当前包起始偏移</param>
    /// <param name="count">个数。默认 -1 表示到末尾</param>
    /// <param name="transferOwner">是否转移内存管理权。兼容参数，忽略</param>
    [Obsolete("引用计数共享模型下切片自动共享所有权，请改用 Slice(Int32 offset, Int32 count)。")]
    IPacket Slice(Int32 offset, Int32 count, Boolean transferOwner);

    /// <summary>尝试获取当前片段的 <see cref="ArraySegment{T}"/>（不含链式后续）</summary>
    Boolean TryGetArray(out ArraySegment<Byte> segment);
}

/// <summary>拥有管理权的数据包。使用完以后需要释放</summary>
public interface IOwnerPacket : IPacket, IDisposable;
