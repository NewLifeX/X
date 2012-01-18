using System;
using System.IO;
using System.Text;
using NewLife.Reflection;

namespace NewLife.IO
{
    /// <summary>增强的数据流读取器</summary>
    /// <remarks>
    /// StreamReader太恶心了，自动把流给关闭了，还没有地方设置。
    /// </remarks>
    public class StreamReaderX : StreamReader
    {
        #region 属性
        private static FieldInfoX _Closable = FieldInfoX.Create(typeof(StreamReader), "_closable");
        private static FieldInfoX _CharPosition = FieldInfoX.Create(typeof(StreamReader), "charPos");

        /// <summary>是否在最后关闭流</summary>
        public Boolean Closable { get { return (Boolean)_Closable.GetValue(this); } set { _Closable.SetValue(this, value); } }

        /// <summary>字符位置。因为涉及字符编码，所以跟流位置可能不同。对于ASCII编码没有问题。</summary>
        public Int32 CharPosition { get { return (Int32)_CharPosition.GetValue(this); } set { _CharPosition.SetValue(this, value); } }
        #endregion

        #region 构造
        /// <summary>为指定的流初始化 <see cref="T:System.IO.StreamReader" /> 类的新实例。</summary>
        /// <param name="stream">要读取的流。</param>
        /// <exception cref="T:System.ArgumentException"><paramref name="stream" /> 不支持读取。</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="stream" /> 为 null。</exception>
        public StreamReaderX(Stream stream) : base(stream) { }

        /// <summary>用指定的字符编码为指定的流初始化 <see cref="T:System.IO.StreamReader" /> 类的一个新实例。</summary>
        /// <param name="stream">要读取的流。</param>
        /// <param name="encoding">要使用的字符编码。</param>
        /// <exception cref="T:System.ArgumentException"><paramref name="stream" /> 不支持读取。</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="stream" /> 或 <paramref name="encoding" /> 为 null。</exception>
        public StreamReaderX(Stream stream, Encoding encoding) : base(stream, encoding) { }

        /// <summary>为指定的流初始化 <see cref="T:System.IO.StreamReader" /> 类的新实例，带有指定的字符编码、字节顺序标记检测选项和缓冲区大小。</summary>
        /// <param name="stream">要读取的流。</param>
        /// <param name="encoding">要使用的字符编码。</param>
        /// <param name="closable">是否关闭数据流。</param>
        /// <exception cref="T:System.ArgumentException">流不支持读取。</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="stream" /> 或 <paramref name="encoding" /> 为 null。</exception>
        public StreamReaderX(Stream stream, Encoding encoding, bool closable)
            : base(stream, encoding)
        {
            Closable = closable;
        }
        #endregion
    }
}