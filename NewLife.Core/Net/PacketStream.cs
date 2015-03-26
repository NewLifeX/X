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
            var p = ReadPosition + Size;
            // 一个全新的包，或者还没完全的包，都要从底层读取更多数据
            if (Size == 0 || p > _ms.Length)
            {
                // 检查内存流空间大小，至少能放下当前数据包
                if (_ms.Capacity < p) _ms.Capacity = p;
                // 从内部流读取数据，写到最后。这里必须移动指针，因为以前可能移动到别的地方去了
                _ms.Position = _ms.Length;
                _ms.Write(_s);

                // 尝试读取长度
                if (Size == 0)
                {
                    _ms.Position = ReadPosition;
                    while (Size == 0)
                    {
                        var n = 0;
                        if (!_ms.TryReadEncodedInt(out n)) return 0;
                        Size = n;
                    }
                    // 读取长度后，指针移到新的位置
                    ReadPosition = (Int32)_ms.Position;
                    p = ReadPosition + Size;
                }
            }
            // 数据还是不够一个包，走吧
            if (Size == 0 || p > _ms.Length) return 0;

            // 这里开始，说明数据足够一个包，开始读出来
            _ms.Position = ReadPosition;
            // 如果缓冲区大小不够，则需要特殊处理
            var rs = _ms.Read(buffer, offset, Size <= count ? Size : count);
            // 扔掉多余部分
            if (Size > count) _ms.ReadBytes(Size - count);
            ReadPosition += Size;

            // 清空包大小，准备下一次包
            Size = 0;
            // 如果后面没有数据，直接把长度设为0重用缓冲区，否则缓冲区过大时需要拷贝
            if (_ms.Position == _ms.Length)
            {
                _ms.SetLength(0);
                ReadPosition = 0;
            }
            else
            {
                // 读取下一次大小
                Size = _ms.ReadEncodedInt();
                ReadPosition = (Int32)_ms.Position;

                if (_ms.Position > 8 * 1024)
                {
                    // 构造新的缓冲区
                    var ns = new MemoryStream();
                    ns.Write(_ms);
                    _ms.SetLength(0);
                    ReadPosition = 0;

                    // 剩余数据重新写入缓冲区
                    ns.Position = 0;
                    _ms.Write(ns);
                }
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