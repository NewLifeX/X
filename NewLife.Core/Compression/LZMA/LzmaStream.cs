using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace NewLife.Compression.LZMA
{
    /// <summary>LZMA数据流</summary>
    public class LzmaStream : Stream
    {
        #region 属性
        private Stream _BaseStream;
        /// <summary>基础数据流</summary>
        public Stream BaseStream { get { return _BaseStream; } }

        private CompressionMode _Mode;
        Int32 _Level;
        #endregion

        #region 构造
        /// <summary>实例化一个Lzma数据流</summary>
        /// <param name="stream"></param>
        /// <param name="mode"></param>
        /// <param name="level"></param>
        public LzmaStream(Stream stream, CompressionMode mode, Int32 level = 4)
        {
            _BaseStream = stream;
            _Mode = mode;
            _Level = level;
        }
        #endregion

        #region 数据流成员
        /// <summary>是否允许读取</summary>
        public override bool CanRead { get { return _Mode == CompressionMode.Decompress; } }

        /// <summary>是否允许搜索</summary>
        public override bool CanSeek { get { return false; } }

        /// <summary>是否允许写入</summary>
        public override bool CanWrite { get { return _Mode != CompressionMode.Decompress; } }

        /// <summary>长度</summary>
        public override long Length { get { throw new NotImplementedException(); } }

        /// <summary>位置</summary>
        public override long Position { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        /// <summary>搜索</summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }

        /// <summary>设定长度</summary>
        /// <param name="value"></param>
        public override void SetLength(long value) { throw new NotImplementedException(); }
        #endregion

        #region 读写成员
        Encoder _Encoder;
        /// <summary>读取解压缩数据流</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_Mode != CompressionMode.Decompress) throw new InvalidOperationException();

            if (_Encoder == null)
            {
                _Encoder = new Encoder();
            }

            var ms = new MemoryStream(buffer, offset, count);
            _Encoder.Code(BaseStream, ms, -1, -1, null);

            return (Int32)ms.Position;
        }

        Decoder _Decoder;
        /// <summary>写入数据并压缩</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_Mode != CompressionMode.Compress) throw new InvalidOperationException();

            if (_Decoder == null)
            {
                _Decoder = new Decoder();
            }

            var ms = new MemoryStream(buffer, offset, count);
            _Encoder.Code(ms, BaseStream, -1, -1, null);
        }

        /// <summary>刷新数据流</summary>
        public override void Flush() { }
        #endregion
    }
}