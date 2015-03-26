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

        private Int32 _ReadPosition;
        /// <summary>_ms中当前数据包开始的位置</summary>
        private Int32 ReadPosition { get { return _ReadPosition; } set { _ReadPosition = value; } }

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
            // 首先必须得到数据包大小，直接从内部数据流读取。这里认为表示长度的几个自己会一起过来
            //while (Size == 0) Size = _s.ReadEncodedInt();
            while (Size == 0)
            {
                var n = 0;
                if (!_s.TryReadEncodedInt(out n)) return 0;
                Size = n;
            }

            // 先读取本地，本地用完了再向下读取
            if (Size > _ms.Length)
            {
                // 还差多少数据不够当前数据包Size - _ms.Length
                //var buf = new Byte[count - (Size - _ms.Length)];
                //var count2 = _s.Read(buf, 0, buf.Length);
                //if (count2 <= 0) return 0;

                //_ms.Write(buf, 0, count2);

                // 检查内存流空间大小，至少能放下当前数据包
                if (_ms.Capacity < Size) _ms.Capacity = Size;
                // 从内部流读取数据
                _ms.Write(_s);
            }
            // 数据还是不够一个包，走吧
            if (Size == 0 || Size > _ms.Length) return 0;

            // 这里开始，说明数据足够一个包，开始读出来
            _ms.Position = 0;
            // 如果缓冲区大小不够，则需要特殊处理
            var rs = _ms.Read(buffer, offset, Size <= count ? Size : count);
            // 扔掉多余部分
            if (Size > count) _ms.ReadBytes(Size - count);
            // 清空包大小，准备下一次包
            Size = 0;
            // 如果后面没有数据，直接把长度设为0，否则需要拷贝
            if (_ms.Position == _ms.Length)
                _ms.SetLength(0);
            else
            {
                // 构造新的缓冲区
                var ns = new MemoryStream();
                ns.Write(_ms);
                _ms.SetLength(0);

                // 剩余数据先读取大小，再重新写入缓冲区
                ns.Position = 0;
                Size = ns.ReadEncodedInt();
                _ms.Write(ns);
          }
            return rs;
        }
        #endregion

        #region 重载
        /// <summary>是否可以读取</summary>
        public override bool CanRead { get { return true; } }

        /// <summary>是否可以查找</summary>
        public override bool CanSeek { get { return false; } }

        /// <summary>是否可以写入</summary>
        public override bool CanWrite { get { return true; } }

        /// <summary>长度</summary>
        public override long Length { get { return _ms.Length; } }

        /// <summary>位置</summary>
        public override long Position { get { return _s.Position; } set { _s.Position = value; } }

        /// <summary>设置当前流中的位置</summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }

        /// <summary>设置长度</summary>
        /// <param name="value"></param>
        public override void SetLength(long value) { throw new NotSupportedException(); }

        #endregion
    }
}