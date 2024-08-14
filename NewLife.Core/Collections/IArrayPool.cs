using System.Diagnostics.CodeAnalysis;

namespace NewLife.Collections;

/// <summary>数组缓冲池</summary>
/// <typeparam name="T"></typeparam>
public interface IArrayPool<T>
{
    /// <summary>借出</summary>
    /// <param name="minimumLength"></param>
    /// <returns></returns>
    T[] Rent(Int32 minimumLength);

    /// <summary>归还</summary>
    /// <param name="array"></param>
    /// <param name="clearArray"></param>
    void Return(T[] array, Boolean clearArray = false);
}

/// <summary>数组池</summary>
public class ArrayPool
{
    /// <summary>空数组</summary>
    public static Byte[] Empty { get; } = [];
}

#if NETFRAMEWORK || NETSTANDARD2_0
/// <summary>数组池</summary>
/// <typeparam name="T"></typeparam>
public abstract class ArrayPool<T> : IArrayPool<T>
{
    private static readonly ArrayPool<T> s_shared = Create();

    /// <summary>共享实例</summary>
    public static ArrayPool<T> Shared => s_shared;

    /// <summary>创建数组池</summary>
    /// <returns></returns>
    public static ArrayPool<T> Create() => new ConfigurableArrayPool<T>();

    /// <summary>创建数组池</summary>
    /// <param name="maxArrayLength"></param>
    /// <param name="maxArraysPerBucket"></param>
    /// <returns></returns>
    public static ArrayPool<T> Create(Int32 maxArrayLength, Int32 maxArraysPerBucket) => new ConfigurableArrayPool<T>(maxArrayLength, maxArraysPerBucket);

    /// <summary>借出</summary>
    /// <param name="minimumLength"></param>
    /// <returns></returns>
    public abstract T[] Rent(Int32 minimumLength);

    /// <summary>归还</summary>
    /// <param name="array"></param>
    /// <param name="clearArray"></param>
    public abstract void Return(T[] array, Boolean clearArray = false);
}

class ConfigurableArrayPool<T> : ArrayPool<T>
{
    #region 属性
    public Int32 MaxArrayLength { get; }

    public Int32 MaxArraysPerBucket { get; }

    private static T[] _empty = [];
    private Bucket[]? _buckets;
    private T[]? _current;

    struct Bucket
    {
        public T[]? Value;
    }
    #endregion

    #region 构造
    public ConfigurableArrayPool() : this(1048576, 50) { }

    public ConfigurableArrayPool(Int32 maxArrayLength, Int32 maxArraysPerBucket)
    {
        if (maxArrayLength > 1073741824)
            maxArrayLength = 1073741824;
        else if (maxArrayLength < 16)
            maxArrayLength = 16;

        MaxArrayLength = maxArrayLength;
        MaxArraysPerBucket = maxArraysPerBucket;
    }
    #endregion

    #region 方法
    [MemberNotNull(nameof(_buckets))]
    private void Init()
    {
        if (_buckets != null) return;
        lock (this)
        {
            if (_buckets != null) return;

            _buckets = new Bucket[MaxArraysPerBucket];
        }
    }

    /// <summary>借出</summary>
    /// <param name="minimumLength"></param>
    /// <returns></returns>
    public override T[] Rent(Int32 minimumLength)
    {
        if (minimumLength < 0 || minimumLength > MaxArrayLength) throw new ArgumentOutOfRangeException(nameof(minimumLength));
        if (minimumLength == 0) return _empty;

        // 最热的一个对象在外层，便于快速存取
        var val = _current;
        if (val != null && val.Length >= minimumLength && Interlocked.CompareExchange(ref _current, null, val) == val) return val;

        Init();

        var items = _buckets;
        for (var i = 0; i < items.Length; i++)
        {
            val = items[i].Value;
            if (val != null && val.Length >= minimumLength && Interlocked.CompareExchange(ref items[i].Value, null, val) == val) return val;
        }

        var rs = OnCreate(minimumLength);
        if (rs == null) throw new InvalidOperationException($"Unable to create an instance of {typeof(T).FullName}[]");

        return rs;
    }

    /// <summary>创建实例</summary>
    /// <returns></returns>
    protected virtual T[]? OnCreate(Int32 minimumLength) => new T[minimumLength];

    /// <summary>归还</summary>
    /// <param name="array"></param>
    /// <param name="clearArray"></param>
    public override void Return(T[] array, Boolean clearArray = false)
    {
        if (array == null || array.Length == 0) return;

        if (clearArray) Array.Clear(array, 0, array.Length);

        // 最热的一个对象在外层，便于快速存取
        if (_current == null && Interlocked.CompareExchange(ref _current, array, null) == null) return;

        Init();

        var items = _buckets;
        for (var i = 0; i < items.Length; ++i)
        {
            if (Interlocked.CompareExchange(ref items[i].Value, array, null) == null) return;
        }
    }
    #endregion
}
#endif