using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewLife.Net
{
    /// <summary>消息包数据流。以包的形式读写数据流，解决粘包问题</summary>
    public class PacketStream : Stream
    {
        #region 属性
        private Int32 _Size;
        /// <summary>包大小</summary>
        public Int32 Size { get { return _Size; } set { _Size = value; } }

        Stream _s;
        MemoryStream _ms = new MemoryStream();
        #endregion

        #region 构造
        /// <summary>构造一个消息包数据流</summary>
        /// <param name="stream"></param>
        public PacketStream(Stream stream) { _s = stream; }
        #endregion

        #region 写入
        /// <summary>写入</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count < 0) count = buffer.Length - offset;

            var ms = new MemoryStream(4 + count);
            ms.WriteEncodedInt(count);
            ms.Write(buffer, offset, count);
            ms.Position = 0;

            _s.Write(ms);
        }

        /// <summary>忽略缓存立马向下一层写入所有数据</summary>
        public override void Flush() { _s.Flush(); }
        #endregion

        #region 读取
        /// <summary>读取</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // 先读取本地，本地用完了再向下读取
            if (Size == 0 || Size > _ms.Length)
            {
                var buf = new Byte[count - (Size - _ms.Length)];
                var count2 = _s.Read(buf, 0, buf.Length);
                if (count2 <= 0) return 0;

                _ms.Write(buf, 0, count2);
                while (Size == 0) Size = _ms.ReadEncodedInt();
            }
            if (Size == 0 || Size > _ms.Length) return 0;

            _ms.Position = 0;
            // 如果缓冲区大小不够，则需要特殊处理
            var rs = _ms.Read(buffer, offset, Size <= count ? Size : count);
            // 扔掉多余部分
            if (Size > count) _ms.ReadBytes(Size - count);
            // 清空包大小，准备下一次包
            Size = 0;
            if (_ms.Position == _ms.Length)
                _ms.SetLength(0);
            else
            {
                // 构造新的缓冲区
                var buf = _ms.ReadBytes();
                _ms.SetLength(0);
                _ms.Write(buf);
                Size = _ms.ReadEncodedInt();
            }
            return rs;
        }
        #endregion

        #region 重载
        /// <summary>是否可以读取</summary>
        public override bool CanRead { get { return _s.CanRead; } }

        /// <summary>是否可以查找</summary>
        public override bool CanSeek { get { return _s.CanSeek; } }

        /// <summary>是否可以写入</summary>
        public override bool CanWrite { get { return _s.CanWrite; } }

        /// <summary>长度</summary>
        public override long Length { get { return _s.Length; } }

        /// <summary>位置</summary>
        public override long Position { get { return _s.Position; } set { _s.Position = value; } }

        /// <summary>设置当前流中的位置</summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) { return _s.Seek(offset, origin); }

        /// <summary>设置长度</summary>
        /// <param name="value"></param>
        public override void SetLength(long value) { _s.SetLength(value); }

        #endregion
    }
}