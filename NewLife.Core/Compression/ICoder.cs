using System;
using System.IO;
using NewLife.Compression.LZMA;
using System.IO.Compression;

namespace NewLife.Compression
{
    /// <summary>解码输入流引发异常</summary>
    class DataErrorException : ApplicationException
    {
        public DataErrorException() : base("Data Error") { }
    }

    /// <summary>无效参数范围</summary>
    class InvalidParamException : ApplicationException
    {
        public InvalidParamException() : base("Invalid Parameter") { }
    }

    /// <summary>编码进度</summary>
    public interface ICodeProgress
    {
        /// <summary>设置进度</summary>
        /// <param name="inSize">输入大小，-1表示未知</param>
        /// <param name="outSize">输出大小，-1表示未知</param>
        void SetProgress(Int64 inSize, Int64 outSize);
    };

    /// <summary>编码接口</summary>
    public interface ICoder
    {
        /// <summary>编码数据流</summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流</param>
        /// <param name="inSize">输入大小，-1表示未知</param>
        /// <param name="outSize">输出大小，-1表示未知</param>
        /// <param name="progress">进度引用委托</param>
        /// <exception cref="NewLife.Compression.DataErrorException">输入流无效</exception>
        void Code(Stream inStream, Stream outStream, Int64 inSize, Int64 outSize, ICodeProgress progress);
    };

    /// <summary>编码属性</summary>
    public enum CoderPropID
    {
        /// <summary>默认属性</summary>
        DefaultProp = 0,

        /// <summary>字典大小</summary>
        DictionarySize,

        /// <summary>已使用的PPM内存大小</summary>
        UsedMemorySize,

        /// <summary>PPM方法顺序</summary>
        Order,

        /// <summary>块大小</summary>
        BlockSize,

        /// <summary>LZMA位置状态位数量(0&lt;=x&lt;=4)</summary>
        PosStateBits,

        /// <summary>
        /// Specifies number of literal context bits for LZMA (0 &lt;= x &lt;= 8).
        /// </summary>
        LitContextBits,

        /// <summary>
        /// Specifies number of literal position bits for LZMA (0 &lt;= x &lt;= 4).
        /// </summary>
        LitPosBits,

        /// <summary>LZ快字节数</summary>
        NumFastBytes,

        /// <summary>匹配查找方式 LZMA: "BT2", "BT4", "BT4B"</summary>
        MatchFinder,

        /// <summary>匹配查找循环次数</summary>
        MatchFinderCycles,

        /// <summary>密码数量</summary>
        NumPasses,

        /// <summary>算法数量</summary>
        Algorithm,

        /// <summary>线程数</summary>
        NumThreads,

        /// <summary>结束标记模式</summary>
        EndMarker
    };

    /// <summary>设置编码属性接口</summary>
    public interface ISetCoderProperties
    {
        /// <summary>设置编码属性</summary>
        /// <param name="propIDs"></param>
        /// <param name="properties"></param>
        void SetCoderProperties(CoderPropID[] propIDs, object[] properties);
    };

    /// <summary>写入编码属性</summary>
    public interface IWriteCoderProperties
    {
        /// <summary>写入编码属性</summary>
        /// <param name="outStream"></param>
        void WriteCoderProperties(Stream outStream);
    }

    /// <summary>设置解码属性</summary>
    public interface ISetDecoderProperties
    {
        /// <summary>设置解码属性</summary>
        /// <param name="properties"></param>
        void SetDecoderProperties(byte[] properties);
    }

    /// <summary>Lzma助手</summary>
    public static class LzmaHelper
    {
        /// <summary>压缩数据流</summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流。如果不指定，则内部实例化一个内存流</param>
        /// <param name="level">压缩等级</param>
        /// <remarks>返回输出流，注意此时指针位于末端</remarks>
        public static Stream CompressLzma(this Stream inStream, Stream outStream = null, Int32 level = 4)
        {
            if (outStream == null) outStream = new MemoryStream();

            // 第三个参数为true，保持数据流打开，内部不应该干涉外部，不要关闭外部的数据流
            using (var stream = new LzmaStream(outStream, CompressionMode.Compress, level))
            {
                inStream.CopyTo(stream);
                stream.Flush();
                stream.Close();
            }

            return outStream;
        }

        /// <summary>解压缩数据流</summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流。如果不指定，则内部实例化一个内存流</param>
        /// <remarks>返回输出流，注意此时指针位于末端</remarks>
        public static Stream DecompressLzma(this Stream inStream, Stream outStream = null)
        {
            if (outStream == null) outStream = new MemoryStream();

            // 第三个参数为true，保持数据流打开，内部不应该干涉外部，不要关闭外部的数据流
            using (var stream = new LzmaStream(inStream, CompressionMode.Decompress))
            {
                stream.CopyTo(outStream);
                stream.Close();
            }

            return outStream;
        }
    }
}