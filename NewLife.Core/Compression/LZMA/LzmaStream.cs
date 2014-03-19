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

        ///// <summary>销毁</summary>
        ///// <param name="disposing"></param>
        //protected override void Dispose(bool disposing)
        //{
        //    if (_Encoder != null) _Encoder.TryDispose();

        //    base.Dispose(disposing);
        //}
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
        LzmaDecoder _Decoder;
        /// <summary>读取解压缩数据流</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_Mode != CompressionMode.Decompress) throw new InvalidOperationException();

            if (_Decoder == null)
            {
                _Decoder = new LzmaDecoder();
                var properties = BaseStream.ReadBytes(5);
                _Decoder.SetDecoderProperties(properties);

                // 8字节长度
                var len = BitConverter.ToInt64(BaseStream.ReadBytes(8), 0);
            }

            var ms = new MemoryStream(buffer, offset, count);
            _Decoder.Code(BaseStream, ms, -1, -1, null);

            return (Int32)ms.Position;
        }

        LzmaEncoder _Encoder;
        /// <summary>写入数据并压缩</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_Mode != CompressionMode.Compress) throw new InvalidOperationException();

            if (_Encoder == null)
            {
                _Encoder = new LzmaEncoder();

                #region 计算压缩等级
                CoderPropID[] propIDs = 
				{
					CoderPropID.DictionarySize,
					CoderPropID.PosStateBits,
					CoderPropID.LitContextBits,
					CoderPropID.LitPosBits,
					CoderPropID.Algorithm,
					CoderPropID.NumFastBytes,
					CoderPropID.MatchFinder,
					CoderPropID.EndMarker
				};
                object[] properties = 
				{
					(Int32)1<<Get(_Level, 0, 29),
					(Int32)2,
					(Int32)3,
					(Int32)0,
					(Int32)2,   // Algorithm
					(Int32)256, // NumFastBytes
					"bt4",  // MatchFinder MF
					false   // EndMarker EOF
				};
                //object[] properties = 
                //{
                //    (Int32)Get(_Level, 0, 29),
                //    (Int32)Get(_Level, 0, 4),
                //    (Int32)Get(_Level, 0, 8),
                //    (Int32)Get(_Level, 0, 4),
                //    (Int32)Get(_Level, 0, 2),   // Algorithm
                //    (Int32)Get(_Level, 0, 128), // NumFastBytes
                //    "bt4",  // MatchFinder MF
                //    false   // EndMarker EOF
                //};
                #endregion

                _Encoder.SetCoderProperties(propIDs, properties);
                _Encoder.WriteCoderProperties(BaseStream);

                // 8字节长度
                BaseStream.Write(BitConverter.GetBytes((Int64)0));
            }

            var ms = new MemoryStream(buffer, offset, count);
            _Encoder.Code(ms, BaseStream, -1, -1, null);
        }

        /// <summary>刷新数据流</summary>
        public override void Flush() { }
        #endregion

        #region 辅助
        static Int32 Get(Int32 level, Int32 min, Int32 max)
        {
            if (level < 0) level = 0;
            if (level > 10) level = 10;

            return min + (level * (max - min) + 5) / 10;
        }
        #endregion
    }
}