namespace NewLife.Messaging;

/// <summary>帧长度超过上限。帧泵在整帧模式下发现单帧超过 <see cref="MessagePump.MaxFrameSize"/> 时抛出</summary>
/// <remarks>
/// <para>继承 <see cref="InvalidOperationException" />：既有按 <see cref="InvalidOperationException" /> 捕获的调用方行为不变，
/// 协议层据此把「消息过大」与「帧损坏」区分开来——WebSocket 前者的关闭码是 1009（Message Too Big），后者是 1002（Protocol Error）。</para>
/// </remarks>
public class FrameTooLargeException : InvalidOperationException
{
    /// <summary>帧长度</summary>
    public Int64 FrameLength { get; }

    /// <summary>长度上限</summary>
    public Int32 MaxSize { get; }

    /// <summary>实例化</summary>
    /// <param name="frameLength">帧长度</param>
    /// <param name="maxSize">长度上限</param>
    public FrameTooLargeException(Int64 frameLength, Int32 maxSize)
        : base(L.T("Frame length {0} exceeds the limit of {1}; refusing to buffer an oversized frame indefinitely.", frameLength, maxSize))
    {
        FrameLength = frameLength;
        MaxSize = maxSize;
    }
}
