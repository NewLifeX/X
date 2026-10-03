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

    /// <summary>本视图之前的字节数（同一缓冲区内）。组装内容时若已预留头部，即为可零拷贝借位写入协议头的空间</summary>
    /// <remarks>
    /// <para>组装方分配时预留（如 <c>new OwnerPacket(size, reserve)</c>，参考 <see cref="Serialization.SpanSerializer.HeaderReserve"/>），或由预留区切片/借位派生。</para>
    /// <para>组装帧头统一走 <see cref="PacketHelper.PrepareHeader"/>：拥有句柄且空间足够时借位共享，其余新头节点挂接负载链。</para>
    /// <para><b>前提</b>：借位会写入本视图之前的字节，仅当这些字节确实空闲（预留区，或解析所得帧头已消费）时才安全；从帧中间切片得到的负载不满足该前提。</para>
    /// </remarks>
    Int32 FreeHeader { get; }

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
    /// <para>拥有句柄切片后仍是拥有句柄，具体类型调用返回 <see cref="OwnerPacket"/>、经 <see cref="IOwnerPacket"/> 访问返回其自身，可自然使用 <c>using</c> 释放。</para>
    /// <para>取出子窗口后不再使用原句柄时应随即释放（如拆帧后丢弃帧容器），避免句柄引用残留导致缓冲无法归池。</para>
    /// <para>结构体实现（<see cref="ArrayPacket"/> 等）返回无所有权的视图：无需释放，仅可在原数据生命周期内短暂使用。</para>
    /// </remarks>
    /// <param name="offset">相对当前包起始偏移</param>
    /// <param name="count">个数。默认 -1 表示到末尾</param>
    IPacket Slice(Int32 offset, Int32 count = -1);

    /// <summary>切片得到新数据包（三参重载，仅为兼容旧二进制保留）</summary>
    /// <param name="offset">相对当前包起始偏移</param>
    /// <param name="count">个数。默认 -1 表示到末尾</param>
    /// <param name="transferOwner">是否转移所有权。<b>已忽略</b></param>
    /// <returns>共享底层缓冲区的新数据包</returns>
    /// <remarks>
    /// <para>切片语义已统一为引用计数共享：新旧句柄同时可用、各自释放，最后一个释放时归还内存池。
    /// 因此旧版 <c>transferOwner=true</c>（转移所有权、作废源句柄）的语义不再存在，参数被忽略。</para>
    /// <para><b>旧二进制注意</b>：按旧语义“只释放新句柄”的调用方不会释放新句柄的引用计数，缓冲会被延迟到 GC 回收，
    /// 属内存滞留而非错误。升级源码后请改用 <see cref="Slice(Int32, Int32)"/>。</para>
    /// </remarks>
    [Obsolete("请改用 Slice(offset, count)；切片已统一为引用计数共享语义")]
    IPacket Slice(Int32 offset, Int32 count, Boolean transferOwner);

    /// <summary>尝试获取当前片段的 <see cref="ArraySegment{T}"/>（不含链式后续）</summary>
    Boolean TryGetArray(out ArraySegment<Byte> segment);
}

/// <summary>拥有管理权的数据包。使用完以后需要释放</summary>
/// <remarks>
/// <para>切片返回同为拥有句柄的 <see cref="IOwnerPacket"/>：新句柄与原句柄各自 <c>Dispose</c>，最后一个释放时归还内存池。</para>
/// <para>返回类型为可释放的拥有句柄，因此可以自然使用 <c>using</c> 释放：<c>using var view = owner.Slice(offset, count);</c></para>
/// </remarks>
public interface IOwnerPacket : IPacket, IDisposable
{
    /// <summary>切片得到新数据包，共享底层缓冲区（引用计数）。返回拥有句柄，可直接 <c>using</c> 释放</summary>
    /// <remarks>
    /// <para>与 <see cref="IPacket.Slice(Int32, Int32)"/> 是同一操作的协变返回版本：经本接口访问返回 <see cref="IOwnerPacket"/>，
    /// 具体类型上的公开方法返回更具体的 <see cref="OwnerPacket"/>，经 <see cref="IPacket"/> 访问返回 <see cref="IPacket"/>；三者是同一实现，切片结果的实际类型相同。</para>
    /// <para><c>new</c> 仅用于隐藏基接口的同名成员（同名同参、返回类型更具体，与 BCL 的 <c>IEnumerator&lt;T&gt;.Current</c> 属同一模式）。
    /// 接口调用按最具体声明解析：<see cref="IOwnerPacket"/> 变量得到 <see cref="IOwnerPacket"/>，<see cref="IPacket"/> 变量得到 <see cref="IPacket"/>，不存在歧义。</para>
    /// <para>新句柄与原句柄同时可用，各自释放，最后一个释放时归还内存池；取出子窗口后不再使用原句柄时应随即释放。</para>
    /// </remarks>
    /// <param name="offset">相对当前包起始偏移</param>
    /// <param name="count">个数。默认 -1 表示到末尾</param>
    new IOwnerPacket Slice(Int32 offset, Int32 count = -1);
}
