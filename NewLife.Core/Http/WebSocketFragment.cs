using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Http;

/// <summary>WebSocket 分片重组器（RFC 6455 §5.4）。累积 FIN=0 数据帧，末片（FIN=1）合并为完整消息交付</summary>
/// <remarks>
/// 每个连接持有一个实例；控制帧（Close/Ping/Pong）不可分片，不经过本重组器。
/// 首片为文本或二进制帧（opcode=1/2，FIN=0），后续续片为附加数据帧（opcode=0）。
/// </remarks>
internal sealed class WebSocketFragment
{
    private readonly List<Byte[]> _fragments = [];
    private WebSocketMessageType _type;
    private Int32 _length;

    /// <summary>分片重组上限（字节）。超过丢弃当前分片序列，避免内存无界增长。默认 16M</summary>
    public Int32 MaxFragments { get; set; } = 16 * 1024 * 1024;

    /// <summary>是否正在累积分片</summary>
    public Boolean Active => _fragments.Count > 0;

    /// <summary>开始累积新的分片序列。新首片到来时丢弃未完成的旧序列（协议错误或对端中断）</summary>
    /// <param name="type">首片类型（文本或二进制）</param>
    /// <param name="payload">首片负载（内部拷贝）</param>
    public void Begin(WebSocketMessageType type, IPacket? payload)
    {
        _fragments.Clear();
        _type = type;

        // 首片同样受上限约束：不检查会让一个超大 FIN=0 首片直接分配并绕过上限（Append 侧已有同样检查）。
        // 先按长度判定再拷贝，避免为注定被丢弃的分片白拷贝一大块
        var total = payload?.Total ?? 0;
        if (total > MaxFragments)
        {
            _length = 0;
            return;
        }

        var data = payload?.ToArray() ?? [];
        _fragments.Add(data);
        _length = data.Length;
    }

    /// <summary>追加续片；末片（FIN=1）时合并为完整消息</summary>
    /// <param name="fin">是否末片</param>
    /// <param name="payload">续片负载（内部拷贝）</param>
    /// <returns>合并后的完整消息；未到末片、超限或无首片的孤立续片返回 null</returns>
    public WsMessage? Append(Boolean fin, IPacket? payload)
    {
        if (_fragments.Count == 0) return null;

        var data = payload?.ToArray() ?? [];
        _length += data.Length;
        if (_length > MaxFragments)
        {
            // 超限：丢弃整段分片序列
            _fragments.Clear();
            return null;
        }

        _fragments.Add(data);
        if (!fin) return null;

        // 末片：合并为完整消息
        var buf = new Byte[_length];
        var offset = 0;
        foreach (var item in _fragments)
        {
            item.CopyTo(buf, offset);
            offset += item.Length;
        }
        _fragments.Clear();

        var msg = new WsMessage { Fin = true, Type = _type };
        msg.SetBody(new ArrayPacket(buf));

        return msg;
    }
}
