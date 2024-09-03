using System.Buffers;
using System.Runtime.CompilerServices;

namespace NewLife.Buffers;

internal sealed class BufferSegment : ReadOnlySequenceSegment<Byte>
{
    private IMemoryOwner<Byte>? _memoryOwner;

    private Byte[]? _array;

    private BufferSegment? _next;

    private Int32 _end;

    public Int32 End
    {
        get
        {
            return _end;
        }
        set
        {
            _end = value;
            base.Memory = AvailableMemory[..value];
        }
    }

    public BufferSegment? NextSegment
    {
        get
        {
            return _next;
        }
        set
        {
            base.Next = value;
            _next = value;
        }
    }

    internal Object? MemoryOwner => ((Object)_memoryOwner) ?? ((Object)_array);

    public Memory<Byte> AvailableMemory { get; private set; }

    public Int32 Length => End;

    public Int32 WritableBytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AvailableMemory.Length - End;
    }

    public void SetOwnedMemory(IMemoryOwner<Byte> memoryOwner)
    {
        _memoryOwner = memoryOwner;
        AvailableMemory = memoryOwner.Memory;
    }

    public void SetOwnedMemory(Byte[] arrayPoolBuffer)
    {
        _array = arrayPoolBuffer;
        AvailableMemory = arrayPoolBuffer;
    }

    public void Reset()
    {
        ResetMemory();
        base.Next = null;
        base.RunningIndex = 0L;
        _next = null;
    }

    public void ResetMemory()
    {
        var memoryOwner = _memoryOwner;
        if (memoryOwner != null)
        {
            _memoryOwner = null;
            memoryOwner.Dispose();
        }
        else if (_array != null)
        {
            ArrayPool<Byte>.Shared.Return(_array);
            _array = null;
        }
        base.Memory = default;
        _end = 0;
        AvailableMemory = default;
    }

    public void SetNext(BufferSegment segment)
    {
        NextSegment = segment;
        segment = this;
        while (segment.Next != null)
        {
            segment.NextSegment.RunningIndex = segment.RunningIndex + segment.Length;
            segment = segment.NextSegment;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Int64 GetLength(BufferSegment startSegment, Int32 startIndex, BufferSegment endSegment, Int32 endIndex) => endSegment.RunningIndex + (UInt32)endIndex - (startSegment.RunningIndex + (UInt32)startIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Int64 GetLength(Int64 startPosition, BufferSegment endSegment, Int32 endIndex) => endSegment.RunningIndex + (UInt32)endIndex - startPosition;
}
