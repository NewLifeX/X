using System.IO;
using System;

namespace NewLife.IO
{
    /// <summary>
    /// 读写流。读写不在一起。
    /// </summary>
    public abstract class ReadWriteStream : Stream
    {
        #region 属性
        private Stream _InputStream;
        /// <summary>输入流</summary>
        public Stream InputStream
        {
            get { return _InputStream; }
            set { _InputStream = value; }
        }

        private Stream _OutputStream;
        /// <summary>输出流</summary>
        public Stream OutputStream
        {
            get { return _OutputStream; }
            set { _OutputStream = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        public ReadWriteStream(Stream inputStream, Stream outputStream)
        {
            InputStream = inputStream;
            OutputStream = outputStream;
        }
        #endregion

        #region 抽象实现

        /// <summary>
        /// 输入流是否可读
        /// </summary>
        public override bool CanRead
        {
            get { return InputStream.CanRead; }
        }

        /// <summary>
        /// 输入流是否可移动
        /// </summary>
        public override bool CanSeek
        {
            get { return InputStream.CanRead; }
        }

        /// <summary>
        /// 输出流是否可写
        /// </summary>
        public override bool CanWrite
        {
            get { return OutputStream.CanWrite; }
        }

        /// <summary>
        /// 刷新输出流写入的数据
        /// </summary>
        public override void Flush()
        {
            OutputStream.Flush();
        }

        /// <summary>
        /// 输入流总长度
        /// </summary>
        public override long Length
        {
            get { return InputStream.Length; }
        }

        /// <summary>
        /// 输入流位置
        /// </summary>
        public override long Position
        {
            get { return InputStream.Position; }
            set { InputStream.Position = value; }
        }

        /// <summary>
        /// 从输入流中读取数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return InputStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// 在输入流中搜索
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return InputStream.Seek(offset, origin);
        }

        /// <summary>
        /// 设置输出流的长度
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            OutputStream.SetLength(value);
        }

        /// <summary>
        /// 把数据写入到输出流中
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            OutputStream.Write(buffer, offset, count);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 读取一个字节，不移动指针
        /// </summary>
        /// <returns></returns>
        public Byte Peek()
        {
            Byte b = (Byte)ReadByte();
            Seek(-1, SeekOrigin.Current);
            return b;
        }
        #endregion
    }
}
