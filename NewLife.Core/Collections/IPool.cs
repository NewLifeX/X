using System;
using System.IO;
using System.Text;

namespace NewLife.Collections
{
    /// <summary>对象池接口</summary>
    /// <typeparam name="T"></typeparam>
    public interface IPool<T> where T : class
    {
        /// <summary>对象池大小</summary>
        Int32 Max { get; set; }

        /// <summary>获取</summary>
        /// <returns></returns>
        T Get();

        /// <summary>归还</summary>
        /// <param name="value"></param>
        Boolean Put(T value);

        /// <summary>清空</summary>
        Int32 Clear();
    }

    /// <summary>对象池扩展</summary>
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
        public static String Put(this StringBuilder sb, Boolean requireResult = false)
        {
            if (sb == null) return null;

            var str = requireResult ? sb.ToString() : null;

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
            protected override StringBuilder OnCreate() => new StringBuilder(InitialCapacity);

            /// <summary>归还</summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public override Boolean Put(StringBuilder value)
            {
                if (value.Capacity > MaximumCapacity) return false;

                value.Clear();

                return true;
            }
        }
        #endregion

        #region MemoryStream
        /// <summary>内存流池</summary>
        public static IPool<MemoryStream> MemoryStream { get; set; } = new MemoryStreamPool();

        /// <summary>归还一个内存流到对象池</summary>
        /// <param name="ms"></param>
        /// <param name="requireResult">是否需要返回结果</param>
        /// <returns></returns>
        public static Byte[] Put(this MemoryStream ms, Boolean requireResult = false)
        {
            if (ms == null) return null;

            var buf = requireResult ? ms.ToArray() : null;

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
            protected override MemoryStream OnCreate() => new MemoryStream(InitialCapacity);

            /// <summary>归还</summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public override Boolean Put(MemoryStream value)
            {
                if (value.Capacity > MaximumCapacity) return false;

                value.Position = 0;
                value.SetLength(0);

                return true;
            }
        }
        #endregion
    }
}