using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NewLife.IP
{
    static class IOHelper
    {

        #region 压缩/解压缩 数据
        /// <summary>解压缩数据流</summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流</param>
        public static void Decompress(Stream inStream, Stream outStream)
        {
            // 第三个参数为true，保持数据流打开，内部不应该干涉外部，不要关闭外部的数据流
            using (var stream = new GZipStream(inStream, CompressionMode.Decompress, true))
            {
                CopyTo(stream, outStream);
                stream.Close();
            }
        }
        #endregion

        #region 复制数据流
        /// <summary>复制数据流</summary>
        /// <param name="src">源数据流</param>
        /// <param name="des">目的数据流</param>
        /// <param name="bufferSize">缓冲区大小，也就是每次复制的大小</param>
        /// <param name="max">最大复制字节数</param>
        /// <returns>返回复制的总字节数</returns>
        public static Int32 CopyTo(Stream src, Stream des, Int32 bufferSize = 0, Int32 max = 0)
        {
            if (bufferSize <= 0) bufferSize = 1024;
            var buffer = new Byte[bufferSize];

            Int32 total = 0;
            while (true)
            {
                var count = bufferSize;
                if (max > 0)
                {
                    if (total >= max) break;

                    // 最后一次读取大小不同
                    if (count > max - total) count = max - total;
                }

                count = src.Read(buffer, 0, count);
                if (count <= 0) break;
                total += count;

                des.Write(buffer, 0, count);
            }

            return total;
        }
        #endregion
    }
}
