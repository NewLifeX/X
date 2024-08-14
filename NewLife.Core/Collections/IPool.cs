using System.Text;
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
using System.Buffers;
#endif

namespace NewLife.Collections;

/// <summary>对象池接口</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/object_pool
/// </remarks>
/// <typeparam name="T"></typeparam>
public interface IPool<T>
{
    /// <summary>对象池大小</summary>
    Int32 Max { get; set; }

    /// <summary>获取</summary>
    /// <returns></returns>
    T Get();

    /// <summary>归还</summary>
    /// <param name="value"></param>
    Boolean Put(T value);

    /// <summary>归还</summary>
    /// <param name="value"></param>
    Boolean Return(T value);

    /// <summary>清空</summary>
    Int32 Clear();
}

/// <summary>对象池扩展</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/object_pool
/// </remarks>
public static class Pool
{
    #region 扩展
    #endregion

    #region StringBuilder
    /// <summary>字符串构建器池</summary>
    public static IPool<StringBuilder> StringBuilder { get; set; } = new StringBuilderPool();

    /// <summary>归还一个字符串构建器到对象池</summary>
    /// <param name="sb"></param>
    /// <param name="requireResult">是否需要返回结果</param>
    /// <returns></returns>
    //[Obsolete("Please use Return from 2024-02-01")]
    public static String Put(this StringBuilder sb, Boolean requireResult = false)
    {
        //if (sb == null) return null;

        var str = requireResult ? sb.ToString() : String.Empty;

        Pool.StringBuilder.Put(sb);

        return str;
    }

    /// <summary>归还一个字符串构建器到对象池</summary>
    /// <param name="sb"></param>
    /// <param name="returnResult">是否需要返回结果</param>
    /// <returns></returns>
    public static String Return(this StringBuilder sb, Boolean returnResult = true)
    {
        //if (sb == null) return null;

        var str = returnResult ? sb.ToString() : String.Empty;

        Pool.StringBuilder.Put(sb);

        return str;
    }

    /// <summary>字符串构建器池</summary>
    public class StringBuilderPool : Pool<StringBuilder>
    {
        /// <summary>初始容量。默认100个</summary>
        public Int32 InitialCapacity { get; set; } = 100;

        /// <summary>最大容量。超过该大小时不进入池内，默认4k</summary>
        public Int32 MaximumCapacity { get; set; } = 4 * 1024;

        /// <summary>创建</summary>
        /// <returns></returns>
        protected override StringBuilder OnCreate() => new(InitialCapacity);

        /// <summary>归还</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean Put(StringBuilder value)
        {
            if (value.Capacity > MaximumCapacity) return false;

            value.Clear();

            return base.Put(value);
        }

        /// <summary>归还</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean Return(StringBuilder value)
        {
            if (value.Capacity > MaximumCapacity) return false;

            value.Clear();

            return base.Return(value);
        }
    }
    #endregion

    #region MemoryStream
    private static Byte[] _empty = [];
    /// <summary>内存流池</summary>
    public static IPool<MemoryStream> MemoryStream { get; set; } = new MemoryStreamPool();

    /// <summary>归还一个内存流到对象池</summary>
    /// <param name="ms"></param>
    /// <param name="requireResult">是否需要返回结果</param>
    /// <returns></returns>
    //[Obsolete("Please use Return from 2024-02-01")]
    public static Byte[] Put(this MemoryStream ms, Boolean requireResult = false)
    {
        //if (ms == null) return null;

        var buf = requireResult ? ms.ToArray() : _empty;

        Pool.MemoryStream.Put(ms);

        return buf;
    }

    /// <summary>归还一个内存流到对象池</summary>
    /// <param name="ms"></param>
    /// <param name="returnResult">是否需要返回结果</param>
    /// <returns></returns>
    public static Byte[] Return(this MemoryStream ms, Boolean returnResult = true)
    {
        //if (ms == null) return null;

        var buf = returnResult ? ms.ToArray() : _empty;

        Pool.MemoryStream.Put(ms);

        return buf;
    }

    /// <summary>内存流池</summary>
    public class MemoryStreamPool : Pool<MemoryStream>
    {
        /// <summary>初始容量。默认1024个</summary>
        public Int32 InitialCapacity { get; set; } = 1024;

        /// <summary>最大容量。超过该大小时不进入池内，默认64k</summary>
        public Int32 MaximumCapacity { get; set; } = 64 * 1024;

        /// <summary>创建</summary>
        /// <returns></returns>
        protected override MemoryStream OnCreate() => new(InitialCapacity);

        /// <summary>归还</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean Put(MemoryStream value)
        {
            if (value.Capacity > MaximumCapacity) return false;

            value.Position = 0;
            value.SetLength(0);

            return base.Put(value);
        }

        /// <summary>归还</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean Return(MemoryStream value)
        {
            if (value.Capacity > MaximumCapacity) return false;

            value.Position = 0;
            value.SetLength(0);

            return base.Return(value);
        }
    }
    #endregion

    #region ByteArray
#if NETFRAMEWORK || NETSTANDARD2_0
    /// <summary>字节数组共享存储</summary>
    public static IArrayPool<Byte> Shared { get; set; } = ArrayPool<Byte>.Shared;
#else
    /// <summary>字节数组共享存储</summary>
    public static IArrayPool<Byte> Shared { get; set; } = new MyArrayPool(ArrayPool<Byte>.Shared);

    class MyArrayPool(ArrayPool<Byte> pool) : ArrayPool<Byte>, IArrayPool<Byte>
    {
        public ArrayPool<Byte> Pool { get; set; } = pool;

        public override Byte[] Rent(Int32 minimumLength) => Pool.Rent(minimumLength);

        public override void Return(Byte[] array, Boolean clearArray = false) => Pool.Return(array, clearArray);
    }
#endif
    #endregion
}