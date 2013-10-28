using System;
using System.IO;

namespace NewLife.IO
{
    /// <summary>读写流。内部包含输入流和输出流两个流，实际读取从输入流读取，写入则写入到输出流</summary>
    public class ReadWriteStream : Stream
    {
        #region 属性
        private Stream _InputStream;
        /// <summary>输入流</summary>
        public Stream InputStream { get { return _InputStream; } set { _InputStream = value; } }

        private Stream _OutputStream;
        /// <summary>输出流</summary>
        public Stream OutputStream { get { return _OutputStream; } set { _OutputStream = value; } }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        public ReadWriteStream(Stream inputStream, Stream outputStream)
        {
            InputStream = inputStream;
            OutputStream = outputStream;
        }
        #endregion

        #region 抽象实现
        /// <summary>输入流是否可读</summary>
        public override bool CanRead { get { return InputStream.CanRead; } }

        /// <summary>输入流是否可移动</summary>
        public override bool CanSeek { get { return InputStream.CanRead; } }

        /// <summary>输出流是否可写</summary>
        public override bool CanWrite { get { return OutputStream.CanWrite; } }

        /// <summary>刷新输出流写入的数据</summary>
        public override void Flush() { OutputStream.Flush(); }

        /// <summary>输入流总长度</summary>
        public override long Length { get { return InputStream.Length; } }

        /// <summary>输入流位置</summary>
        public override long Position { get { return InputStream.Position; } set { InputStream.Position = value; } }

        /// <summary>从输入流中读取数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckArgument(buffer, offset, count);

            return InputStream.Read(buffer, offset, count);
        }

        /// <summary>在输入流中搜索</summary>
        /// <param name="offset">偏移</param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) { return InputStream.Seek(offset, origin); }

        /// <summary>设置输出流的长度</summary>
        /// <param name="value">数值</param>
        public override void SetLength(long value) { OutputStream.SetLength(value); }

        /// <summary>把数据写入到输出流中</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckArgument(buffer, offset, count);

            OutputStream.Write(buffer, offset, count);
        }
        #endregion

        #region 异步
        /// <summary>开始异步读操作</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            CheckArgument(buffer, offset, count);

            return InputStream.BeginRead(buffer, offset, count, callback, state);
        }

        /// <summary>等待挂起的异步读完成</summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        public override int EndRead(IAsyncResult asyncResult) { return InputStream.EndRead(asyncResult); }

        /// <summary>开始异步写操作</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            CheckArgument(buffer, offset, count);

            return OutputStream.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <summary>等待挂起的异步写完成</summary>
        /// <param name="asyncResult"></param>
        public override void EndWrite(IAsyncResult asyncResult) { OutputStream.EndWrite(asyncResult); }
        #endregion

        #region 方法
        ///// <summary>
        ///// 读取一个字节，不移动指针
        ///// </summary>
        ///// <returns></returns>
        //public Byte Peek()
        //{
        //    Byte b = (Byte)ReadByte();
        //    Seek(-1, SeekOrigin.Current);
        //    return b;
        //}
        #endregion

        #region 辅助函数
        /// <summary>检查参数</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        protected static void CheckArgument(byte[] buffer, int offset, int count)
        {
            if (buffer == null || buffer.Length < 0) throw new ArgumentNullException("buffer");
            if (offset < 0 || offset >= buffer.Length) throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException("count");
        }
        #endregion
    }
}