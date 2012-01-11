using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Reflection;

namespace NewLife.IO
{
    /// <summary>增强的数据流写入器</summary>
    /// <remarks>
    /// StreamWriter太恶心了，自动把流给关闭了，还没有地方设置。
    /// </remarks>
    public class StreamWriterX : StreamWriter
    {
        #region 属性
        private static FieldInfoX _Closable = FieldInfoX.Create(typeof(StreamWriter), "closable");

        /// <summary>是否在最后关闭流</summary>
        public Boolean Closable { get { return (Boolean)_Closable.GetValue(this); } set { _Closable.SetValue(this, value); } }
        #endregion

        #region 构造
        /// <summary>用 UTF-8 编码及默认缓冲区大小，为指定的流初始化 <see cref="T:System.IO.StreamWriter" /> 类的一个新实例。</summary>
        /// <param name="stream">要写入的流。</param>
        /// <exception cref="T:System.ArgumentException"><paramref name="stream" /> 不可写。</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="stream" /> 为 null。</exception>
        public StreamWriterX(Stream stream) : base(stream) { }

        /// <summary>使用默认编码和缓冲区大小，为指定路径上的指定文件初始化 <see cref="T:System.IO.StreamWriter" /> 类的新实例。</summary>
        /// <param name="path">要写入的完整文件路径。<paramref name="path" /> 可以是一个文件名。</param>
        /// <exception cref="T:System.UnauthorizedAccessException">访问被拒绝。</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="path" /> 为空字符串 ("")。- 或 -<paramref name="path" /> 包含系统设备的名称（com1、com2 等等）。</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="path" /> 为 null。</exception>
        /// <exception cref="T:System.IO.DirectoryNotFoundException">指定的路径无效，比如在未映射的驱动器上。</exception>
        /// <exception cref="T:System.IO.PathTooLongException">指定的路径、文件名或者两者都超出了系统定义的最大长度。例如，在基于 Windows 的平台上，路径必须小于 248 个字符，文件名必须小于 260 个字符。</exception>
        /// <exception cref="T:System.IO.IOException"><paramref name="path" /> 包含不正确或无效的文件名、目录名或卷标的语法。</exception>
        /// <exception cref="T:System.Security.SecurityException">调用方没有所要求的权限。</exception>
        public StreamWriterX(string path) : base(path) { }

        /// <summary>用指定的编码及默认缓冲区大小，为指定的流初始化 <see cref="T:System.IO.StreamWriter" /> 类的新实例。</summary>
        /// <param name="stream">要写入的流。</param>
        /// <param name="encoding">要使用的字符编码。</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="stream" /> 或 <paramref name="encoding" /> 为 null。</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="stream" /> 不可写。</exception>
        public StreamWriterX(Stream stream, Encoding encoding) : base(stream, encoding) { }

        /// <summary>用指定的编码及缓冲区大小，为指定的流初始化 <see cref="T:System.IO.StreamWriter" /> 类的新实例。</summary>
        /// <param name="stream">要写入的流。</param>
        /// <param name="encoding">要使用的字符编码。</param>
        /// <param name="closable">是否关闭数据流。</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="stream" /> 或 <paramref name="encoding" /> 为 null。</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="stream" /> 不可写。</exception>
        public StreamWriterX(Stream stream, Encoding encoding, bool closable)
            : base(stream, encoding)
        {
            Closable = closable;
        }
        #endregion
    }
}