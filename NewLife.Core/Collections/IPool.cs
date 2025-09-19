using System.Text;
using System.Buffers;

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
    [Obsolete("Please use Return from 2024-02-01")]
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
    #region StringBuilder
    /// <summary>字符串构建器池</summary>
    public static IPool<StringBuilder> StringBuilder { get; set; } = new StringBuilderPool();

    /// <summary>归还一个字符串构建器到对象池</summary>
    /// <param name="sb">待归还实例</param>
    /// <param name="requireResult">是否返回结果字符串。true 将导致一次 <see cref="StringBuilder.ToString()"/> 分配</param>
    /// <returns>当 <paramref name="requireResult"/> 为 true 时返回构建结果，否则返回空字符串</returns>
    [Obsolete("Please use Return from 2024-02-01")]
    public static String Put(this StringBuilder sb, Boolean requireResult = false)
    {
        //if (sb == null) return null;

        var str = requireResult ? sb.ToString() : String.Empty;

        Pool.StringBuilder.Return(sb);

        return str;
    }

    /// <summary>归还一个字符串构建器到对象池</summary>
    /// <param name="sb">待归还实例</param>
    /// <param name="returnResult">是否返回结果字符串。true 将导致一次 <see cref="StringBuilder.ToString()"/> 分配</param>
    /// <returns>当 <paramref name="returnResult"/> 为 true 时返回构建结果，否则返回空字符串</returns>
    public static String Return(this StringBuilder sb, Boolean returnResult = true)
    {
        //if (sb == null) return null;

        var str = returnResult ? sb.ToString() : String.Empty;

        Pool.StringBuilder.Return(sb);

        return str;
    }

    /// <summary>字符串构建器池</summary>
    public class StringBuilderPool : Pool<StringBuilder>
    {
        /// <summary>初始容量。默认100个</summary>
        public Int32 InitialCapacity { get; set; } = 100;

        /// <summary>最大容量。超过该大小时不进入池内，默认4k</summary>
        public Int32 MaximumCapacity { get; set; } = 4 * 1024;

        /// <summary>实例化字符串构建器池。GC2时回收</summary>
        public StringBuilderPool() : base(0, true) { }

        /// <summary>创建</summary>
        /// <returns></returns>
        protected override StringBuilder OnCreate() => new(InitialCapacity);

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
    /// <summary>内存流池</summary>
    public static IPool<MemoryStream> MemoryStream { get; set; } = new MemoryStreamPool();

    /// <summary>归还一个内存流到对象池</summary>
    /// <param name="ms">待归还实例</param>
    /// <param name="requireResult">是否返回字节数组。true 将执行 <see cref="MemoryStream.ToArray()"/> 产生新数组</param>
    /// <returns>当 <paramref name="requireResult"/> 为 true 时返回当前内容的副本，否则返回空数组</returns>
    [Obsolete("Please use Return from 2024-02-01")]
    public static Byte[] Put(this MemoryStream ms, Boolean requireResult = false) => Return(ms, requireResult);

    /// <summary>归还一个内存流到对象池</summary>
    /// <param name="ms">待归还实例</param>
    /// <param name="returnResult">是否返回字节数组。true 将执行 <see cref="MemoryStream.ToArray()"/> 产生新数组</param>
    /// <returns>当 <paramref name="returnResult"/> 为 true 时返回当前内容的副本，否则返回空数组</returns>
    public static Byte[] Return(this MemoryStream ms, Boolean returnResult = true)
    {
        //if (ms == null) return null;

        var buf = returnResult ? ms.ToArray() : Empty;

        Pool.MemoryStream.Return(ms);

        return buf;
    }

    /// <summary>内存流池</summary>
    public class MemoryStreamPool : Pool<MemoryStream>
    {
        /// <summary>初始容量。默认1024个</summary>
        public Int32 InitialCapacity { get; set; } = 1024;

        /// <summary>最大容量。超过该大小时不进入池内，默认64k</summary>
        public Int32 MaximumCapacity { get; set; } = 64 * 1024;

        /// <summary>实例化内存流池。GC2时回收</summary>
        public MemoryStreamPool() : base(0, true) { }

        /// <summary>创建</summary>
        /// <returns></returns>
        protected override MemoryStream OnCreate() => new(InitialCapacity);

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
    /// <summary>字节数组共享存储</summary>
    public static ArrayPool<Byte> Shared { get; set; } = ArrayPool<Byte>.Shared;

    /// <summary>空数组</summary>
    public static Byte[] Empty { get; } = [];
    #endregion
}
