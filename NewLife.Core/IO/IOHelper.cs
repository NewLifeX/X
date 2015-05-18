using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using NewLife.Log;
using NewLife.Reflection;

namespace System
{
    /// <summary>IO工具类</summary>
    public static class IOHelper
    {
        #region 压缩/解压缩 数据
        /// <summary>压缩数据流</summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流。如果不指定，则内部实例化一个内存流</param>
        /// <remarks>返回输出流，注意此时指针位于末端</remarks>
        public static Stream Compress(this Stream inStream, Stream outStream = null)
        {
            if (outStream == null) outStream = new MemoryStream();

            // 第三个参数为true，保持数据流打开，内部不应该干涉外部，不要关闭外部的数据流
#if NET45
            using (var stream = new DeflateStream(outStream, (CompressionLevel)9, true))
#else
            using (var stream = new DeflateStream(outStream, CompressionMode.Compress, true))
#endif
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
        public static Stream Decompress(this Stream inStream, Stream outStream = null)
        {
            if (outStream == null) outStream = new MemoryStream();

            // 第三个参数为true，保持数据流打开，内部不应该干涉外部，不要关闭外部的数据流
            using (var stream = new DeflateStream(inStream, CompressionMode.Decompress, true))
            {
                stream.CopyTo(outStream);
                stream.Close();
            }

            return outStream;
        }

        /// <summary>压缩字节数组</summary>
        /// <param name="data">字节数组</param>
        /// <returns></returns>
        public static Byte[] Compress(this Byte[] data)
        {
            var ms = new MemoryStream();
            Compress(new MemoryStream(data), ms);
            return ms.ToArray();
        }

        /// <summary>解压缩字节数组</summary>
        /// <param name="data">字节数组</param>
        /// <returns></returns>
        public static Byte[] Decompress(this Byte[] data)
        {
            var ms = new MemoryStream();
            Decompress(new MemoryStream(data), ms);
            return ms.ToArray();
        }

        /// <summary>压缩数据流</summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流。如果不指定，则内部实例化一个内存流</param>
        /// <remarks>返回输出流，注意此时指针位于末端</remarks>
        public static Stream CompressGZip(this Stream inStream, Stream outStream = null)
        {
            if (outStream == null) outStream = new MemoryStream();

            // 第三个参数为true，保持数据流打开，内部不应该干涉外部，不要关闭外部的数据流
#if NET45
            using (var stream = new DeflateStream(outStream, CompressionLevel.Optimal, true))
#else
            using (var stream = new GZipStream(outStream, CompressionMode.Compress, true))
#endif
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
        public static Stream DecompressGZip(this Stream inStream, Stream outStream = null)
        {
            if (outStream == null) outStream = new MemoryStream();

            // 第三个参数为true，保持数据流打开，内部不应该干涉外部，不要关闭外部的数据流
            using (var stream = new GZipStream(inStream, CompressionMode.Decompress, true))
            {
                stream.CopyTo(outStream);
                stream.Close();
            }

            return outStream;
        }
        #endregion

        #region 复制数据流
        /// <summary>复制数据流</summary>
        /// <param name="src">源数据流</param>
        /// <param name="des">目的数据流</param>
        /// <param name="bufferSize">缓冲区大小，也就是每次复制的大小</param>
        /// <param name="max">最大复制字节数</param>
        /// <returns>返回复制的总字节数</returns>
        public static Int32 CopyTo(this Stream src, Stream des, Int32 bufferSize = 0, Int32 max = 0)
        {
            // 优化处理内存流，直接拿源内存流缓冲区往目标数据流里写
            if (src is MemoryStream)
            {
                var ms = src as MemoryStream;
                // 如果指针位于开头，并且要读完整个缓冲区，则直接使用WriteTo
                var count = (Int32)(ms.Length - ms.Position);
                if (ms.Position == 0 && (max <= 0 || count <= max))
                {
                    ms.WriteTo(des);
                    ms.Position = ms.Length;
                    return count;
                }

                // 反射读取内存流中数据的原始位置，然后直接把数据拿出来用
                Object obj = 0;
                if (ms.TryGetValue("_origin", out obj))
                {
                    var _origin = (Int32)obj;
                    // 其实地址不为0时，一般不能直接访问缓冲区，因为可能被限制访问
                    var buf = ms.GetValue("_buffer") as Byte[];

                    if (max > 0 && count > max) count = max;
                    des.Write(buf, _origin, count);
                    ms.Position += count;
                    return count;
                }

                // 一次读完
                bufferSize = count;
            }
            // 优化处理目标内存流，直接拿目标内存流缓冲区去源数据流里面读取数据
            if (des is MemoryStream)
            {
                var ms = des as MemoryStream;
                // 
                Object obj = 0;
                if (ms.TryGetValue("_origin", out obj))
                {
                    var _origin = (Int32)obj;
                    // 缓冲区还剩下多少空间
                    var count = (Int32)(ms.Length - ms.Position);
                    // 有可能是全新的内存流
                    if (count == 0)
                    {
                        if (max > 0)
                            count = max;
                        else if (src.CanSeek)
                        {
                            try { count = (Int32)(src.Length - src.Position); }
                            catch { count = 256; }
                        }
                        else
                            count = 256;
                        ms.Capacity += count;
                    }
                    else if (max > 0 && count > max)
                        count = max;

                    // 其实地址不为0时，一般不能直接访问缓冲区，因为可能被限制访问
                    var buf = ms.GetValue("_buffer") as Byte[];

                    // 先把长度设为较大值，为后面设定长度做准备，因为直接使用SetLength会清空缓冲区
                    var len = ms.Length;
                    ms.SetLength(ms.Position + count);
                    // 直接从源数据流往这个缓冲区填充数据
                    var rs = src.Read(buf, _origin, count);
                    if (rs > 0)
                    {
                        // 直接使用SetLength会清空缓冲区
                        ms.SetLength(ms.Position + rs);
                        ms.Position += rs;
                    }
                    else
                        ms.SetLength(len);
                    // 如果得到的数据没有达到预期，说明读完了
                    if (rs <= count) return rs;

                    // 如果还有数据，说明是目标数据流缓冲区不够大
                    XTrace.WriteLine("目标数据流缓冲区不够大，设计上建议加大（>{0}）以提升性能！", count);
                }

            }

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

        /// <summary>把一个数据流写入到另一个数据流</summary>
        /// <param name="des">目的数据流</param>
        /// <param name="src">源数据流</param>
        /// <param name="bufferSize">缓冲区大小，也就是每次复制的大小</param>
        /// <param name="max">最大复制字节数</param>
        /// <returns></returns>
        public static Stream Write(this Stream des, Stream src, Int32 bufferSize = 0, Int32 max = 0)
        {
            src.CopyTo(des, bufferSize, max);
            return des;
        }

        /// <summary>把一个字节数组写入到一个数据流</summary>
        /// <param name="des">目的数据流</param>
        /// <param name="src">源数据流</param>
        /// <returns></returns>
        public static Stream Write(this Stream des, params Byte[] src)
        {
            if (src != null && src.Length > 0) des.Write(src, 0, src.Length);
            return des;
        }

        /// <summary>写入字节数组，先写入压缩整数表示的长度</summary>
        /// <param name="des"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Stream WriteArray(this Stream des, params Byte[] src)
        {
            if (src == null || src.Length == 0)
            {
                des.WriteByte(0);
                return des;
            }

            des.WriteEncodedInt(src.Length);
            return des.Write(src);
        }

        /// <summary>读取字节数组，先读取压缩整数表示的长度</summary>
        /// <param name="des"></param>
        /// <returns></returns>
        public static Byte[] ReadArray(this Stream des)
        {
            var len = des.ReadEncodedInt();
            if (len == 0) return new Byte[0];

            return des.ReadBytes(len);
        }

        /// <summary>复制数组</summary>
        /// <param name="src">源数组</param>
        /// <param name="offset">起始位置</param>
        /// <param name="count">复制字节数</param>
        /// <returns>返回复制的总字节数</returns>
        public static Byte[] ReadBytes(this Byte[] src, Int32 offset = 0, Int32 count = -1)
        {
            if (count == 0) return new Byte[0];

            // 即使是全部，也要复制一份，而不只是返回原数组，因为可能就是为了复制数组
            if (count < 0) count = src.Length - offset;

            var bts = new Byte[count];
            Buffer.BlockCopy(src, offset, bts, 0, bts.Length);
            return bts;
        }

        /// <summary>向字节数组写入一片数据</summary>
        /// <param name="dst">目标数组</param>
        /// <param name="dstOffset">目标偏移</param>
        /// <param name="src">源数组</param>
        /// <param name="srcOffset">源数组偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public static Byte[] Write(this Byte[] dst, Int32 dstOffset, Byte[] src, Int32 srcOffset = 0, Int32 count = -1)
        {
            if (count <= 0) count = src.Length - srcOffset;

#if MF
            Array.Copy(src, srcOffset, dst, dstOffset, count);
#else
            Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);
#endif
            return dst;
        }

        /// <summary>合并两个数组</summary>
        /// <param name="src">源数组</param>
        /// <param name="des">目标数组</param>
        /// <param name="offset">起始位置</param>
        /// <param name="count">字节数</param>
        /// <returns></returns>
        public static Byte[] Combine(this Byte[] src, Byte[] des, Int32 offset = 0, Int32 count = -1)
        {
            if (count < 0) count = src.Length - offset;

            var buf = new Byte[src.Length + count];
            Buffer.BlockCopy(src, 0, buf, 0, src.Length);
            Buffer.BlockCopy(des, offset, buf, src.Length, count);
            return buf;
        }
        #endregion

        #region 数据流转换
        /// <summary>数据流转为字节数组</summary>
        /// <remarks>
        /// 针对MemoryStream进行优化。内存流的Read实现是一个个字节复制，而ToArray是调用内部内存复制方法
        /// 如果要读完数据，又不支持定位，则采用内存流搬运
        /// 如果指定长度超过数据流长度，就让其报错，因为那是调用者所期望的值
        /// </remarks>
        /// <param name="stream">数据流</param>
        /// <param name="length">长度，0表示读到结束</param>
        /// <returns></returns>
        public static Byte[] ReadBytes(this Stream stream, Int64 length = -1)
        {
            if (stream == null) return null;
            if (length == 0) return new Byte[0];

            // 针对MemoryStream进行优化。内存流的Read实现是一个个字节复制，而ToArray是调用内部内存复制方法
            var ms = stream as MemoryStream;
            if (ms != null && ms.Position == 0 && (length <= 0 || length == ms.Length))
            {
                ms.Position = ms.Length;
                // 如果MemoryStream(byte[] buffer,...)构造生成的实例，是不允许访问MemoryStream内部的_buffer数组的
                try
                {
                    // 如果长度一致
                    var buf = ms.GetBuffer();
                    if (buf.Length == ms.Length) return buf;
                }
                catch { }
                // ToArray带有复制，效率稍逊
                return ms.ToArray();
            }

            if (length > 0)
            {
                var bytes = new Byte[length];
                stream.Read(bytes, 0, bytes.Length);
                return bytes;
            }

            // 如果要读完数据，又不支持定位，则采用内存流搬运
            if (!stream.CanSeek)
            {
                ms = new MemoryStream();
                while (true)
                {
                    var buffer = new Byte[1024];
                    Int32 count = stream.Read(buffer, 0, buffer.Length);
                    if (count <= 0) break;

                    ms.Write(buffer, 0, count);
                    if (count < buffer.Length) break;
                }

                return ms.ToArray();
            }
            else
            {
                //if (length <= 0 || stream.CanSeek && stream.Position + length > stream.Length) length = (Int32)(stream.Length - stream.Position);
                // 如果指定长度超过数据流长度，就让其报错，因为那是调用者所期望的值
                length = (Int32)(stream.Length - stream.Position);

                var bytes = new Byte[length];
                stream.Read(bytes, 0, bytes.Length);
                return bytes;
            }
        }

        /// <summary>数据流转为字节数组，从0开始，无视数据流的当前位置</summary>
        /// <param name="stream">数据流</param>
        /// <returns></returns>
        public static Byte[] ToArray(this Stream stream)
        {
            if (stream is MemoryStream) return (stream as MemoryStream).ToArray();

            stream.Position = 0;
            return stream.ReadBytes();
        }

        /// <summary>从数据流中读取字节数组，直到遇到指定字节数组</summary>
        /// <param name="stream">数据流</param>
        /// <param name="buffer">字节数组</param>
        /// <param name="offset">字节数组中的偏移</param>
        /// <param name="length">字节数组中的查找长度</param>
        /// <returns>未找到时返回空，0位置范围大小为0的字节数组</returns>
        public static Byte[] ReadTo(this Stream stream, Byte[] buffer, Int64 offset = 0, Int64 length = -1)
        {
            //if (!stream.CanSeek) throw new XException("流不支持查找！");

            if (length == 0) return new Byte[0];
            if (length < 0) length = buffer.Length - offset;

            var ori = stream.Position;
            var p = stream.IndexOf(buffer, offset, length);
            stream.Position = ori;
            if (p < 0) return null;
            if (p == 0) return new Byte[0];

            return stream.ReadBytes(p);
        }

        /// <summary>从数据流中读取字节数组，直到遇到指定字节数组</summary>
        /// <param name="stream">数据流</param>
        /// <param name="str"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static Byte[] ReadTo(this Stream stream, String str, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            return stream.ReadTo(encoding.GetBytes(str));
        }

        /// <summary>从数据流中读取一行，直到遇到换行</summary>
        /// <param name="stream">数据流</param>
        /// <param name="encoding"></param>
        /// <returns>未找到返回null，0位置返回String.Empty</returns>
        public static String ReadLine(this Stream stream, Encoding encoding = null)
        {
            var bts = stream.ReadTo(Environment.NewLine, encoding);
            //if (bts == null || bts.Length < 1) return null;
            if (bts == null) return null;

            stream.Seek(encoding.GetByteCount(Environment.NewLine), SeekOrigin.Current);
            if (bts.Length == 0) return String.Empty;

            return encoding.GetString(bts);
        }

        /// <summary>流转换为字符串</summary>
        /// <param name="stream">目标流</param>
        /// <param name="encoding">编码格式</param>
        /// <returns></returns>
        public static String ToStr(this Stream stream, Encoding encoding = null)
        {
            if (stream == null) return null;
            if (encoding == null) encoding = Encoding.UTF8;

            var buf = stream.ReadBytes();
            if (buf == null || buf.Length < 1) return null;

            // 可能数据流前面有编码字节序列，需要先去掉
            var idx = 0;
            var preamble = encoding.GetPreamble();
            if (preamble != null && preamble.Length > 0)
            {
                if (buf.StartsWith(preamble)) idx = preamble.Length;
            }

            return encoding.GetString(buf, idx, buf.Length - idx);
        }

        /// <summary>字节数组转换为字符串</summary>
        /// <param name="buf">字节数组</param>
        /// <param name="encoding">编码格式</param>
        /// <param name="offset">字节数组中的偏移</param>
        /// <param name="count">字节数组中的查找长度</param>
        /// <returns></returns>
        public static String ToStr(this Byte[] buf, Encoding encoding = null, Int32 offset = 0, Int32 count = -1)
        {
            if (buf == null || buf.Length < 1 || offset >= buf.Length) return null;
            if (encoding == null) encoding = Encoding.UTF8;

            var size = buf.Length - offset;
            if (count < 0 || count > size) count = size;

            // 可能数据流前面有编码字节序列，需要先去掉
            var idx = 0;
            var preamble = encoding.GetPreamble();
            if (preamble != null && preamble.Length > 0 && buf.Length >= preamble.Length)
            {
                if (buf.ReadBytes(offset, preamble.Length).StartsWith(preamble)) idx = preamble.Length;
            }

            return encoding.GetString(buf, offset + idx, count - idx);
        }
        #endregion

        #region 数据转整数
        /// <summary>从字节数据指定位置读取一个无符号16位整数</summary>
        /// <param name="data"></param>
        /// <param name="offset">偏移</param>
        /// <param name="isLittleEndian">是否小端字节序</param>
        /// <returns></returns>
        public static UInt16 ToUInt16(this Byte[] data, Int32 offset = 0, Boolean isLittleEndian = true)
        {
            if (isLittleEndian)
                return (UInt16)((data[offset + 1] << 8) | data[offset]);
            else
                return (UInt16)((data[offset] << 8) | data[offset + 1]);
        }

        /// <summary>从字节数据指定位置读取一个无符号32位整数</summary>
        /// <param name="data"></param>
        /// <param name="offset">偏移</param>
        /// <param name="isLittleEndian">是否小端字节序</param>
        /// <returns></returns>
        public static unsafe UInt32 ToUInt32(this Byte[] data, Int32 offset = 0, Boolean isLittleEndian = true)
        {
            if (isLittleEndian) return BitConverter.ToUInt32(data, offset);

            // BitConverter得到小端，如果不是小端字节顺序，则倒序
            fixed (byte* numRef = &(data[offset]))
            {
                //if (offset % 4 == 0) return *(((UInt32*)numRef));
                if (isLittleEndian)
                    return (UInt32)(numRef[0] | numRef[1] << 8 | numRef[2] << 0x10 | numRef[3] << 0x18);
                else
                    return (UInt32)(numRef[0] << 0x18 | numRef[1] << 0x10 | numRef[2] << 8 | numRef[3]);
            }
        }

        /// <summary>从字节数据指定位置读取一个无符号64位整数</summary>
        /// <param name="data"></param>
        /// <param name="offset">偏移</param>
        /// <param name="isLittleEndian">是否小端字节序</param>
        /// <returns></returns>
        public static unsafe UInt64 ToUInt64(this Byte[] data, Int32 offset = 0, Boolean isLittleEndian = true)
        {
            if (isLittleEndian) return BitConverter.ToUInt64(data, offset);

            fixed (byte* numRef = &(data[offset]))
            {
                //if (offset % 8 == 0) return *(((UInt64*)numRef));
                if (isLittleEndian)
                {
                    int num1 = numRef[0] | numRef[1] << 8 | numRef[2] << 0x10 | numRef[3] << 0x18;
                    int num2 = numRef[4] | numRef[5] << 8 | numRef[6] << 0x10 | numRef[7] << 0x18;
                    return (UInt32)num1 | (UInt64)num2 << 0x20;
                }
                else
                {
                    int num3 = numRef[0] << 0x18 | numRef[1] << 0x10 | numRef[2] << 8 | numRef[3];
                    int num4 = numRef[4] << 0x18 | numRef[5] << 0x10 | numRef[6] << 8 | numRef[7];
                    return (UInt32)num4 | (UInt64)num3 << 0x20;
                }
            }
        }

        /// <summary>向字节数组的指定位置写入一个无符号16位整数</summary>
        /// <param name="data"></param>
        /// <param name="n">数字</param>
        /// <param name="offset">偏移</param>
        /// <param name="isLittleEndian">是否小端字节序</param>
        /// <returns></returns>
        public static Byte[] Write(this Byte[] data, UInt16 n, Int32 offset = 0, Boolean isLittleEndian = true)
        {
            // STM32单片机是小端
            // Modbus协议规定大端

            if (isLittleEndian)
            {
                data[offset] = (Byte)(n & 0xFF);
                data[offset + 1] = (Byte)(n >> 8);
            }
            else
            {
                data[offset] = (Byte)(n >> 8);
                data[offset + 1] = (Byte)(n & 0xFF);
            }

            return data;
        }

        /// <summary>向字节数组的指定位置写入一个无符号32位整数</summary>
        /// <param name="data"></param>
        /// <param name="n">数字</param>
        /// <param name="offset">偏移</param>
        /// <param name="isLittleEndian">是否小端字节序</param>
        /// <returns></returns>
        public static Byte[] Write(this Byte[] data, UInt32 n, Int32 offset = 0, Boolean isLittleEndian = true)
        {
            if (isLittleEndian)
            {
                for (int i = 0; i < 4; i++)
                {
                    data[offset++] = (Byte)n;
                    n >>= 8;
                }
            }
            else
            {
                for (int i = 4 - 1; i >= 0; i--)
                {
                    data[offset + i] = (Byte)n;
                    n >>= 8;
                }
            }

            return data;
        }

        /// <summary>向字节数组的指定位置写入一个无符号64位整数</summary>
        /// <param name="data"></param>
        /// <param name="n">数字</param>
        /// <param name="offset">偏移</param>
        /// <param name="isLittleEndian">是否小端字节序</param>
        /// <returns></returns>
        public static Byte[] Write(this Byte[] data, UInt64 n, Int32 offset = 0, Boolean isLittleEndian = true)
        {
            if (isLittleEndian)
            {
                for (int i = 0; i < 8; i++)
                {
                    data[offset++] = (Byte)n;
                    n >>= 8;
                }
            }
            else
            {
                for (int i = 8 - 1; i >= 0; i--)
                {
                    data[offset + i] = (Byte)n;
                    n >>= 8;
                }
            }

            return data;
        }

        /// <summary>整数转为字节数组，注意大小端字节序</summary>
        /// <param name="value"></param>
        /// <param name="isLittleEndian"></param>
        /// <returns></returns>
        public static Byte[] GetBytes(this UInt16 value, Boolean isLittleEndian = true)
        {
            var buf = new Byte[2];
            return buf.Write(value, 0, isLittleEndian);
        }

        /// <summary>整数转为字节数组，注意大小端字节序</summary>
        /// <param name="value"></param>
        /// <param name="isLittleEndian"></param>
        /// <returns></returns>
        public static Byte[] GetBytes(this Int16 value, Boolean isLittleEndian = true)
        {
            var buf = new Byte[2];
            return buf.Write((UInt16)value, 0, isLittleEndian);
        }

        /// <summary>整数转为字节数组，注意大小端字节序</summary>
        /// <param name="value"></param>
        /// <param name="isLittleEndian"></param>
        /// <returns></returns>
        public static Byte[] GetBytes(this UInt32 value, Boolean isLittleEndian = true)
        {
            var buf = new Byte[4];
            return buf.Write(value, 0, isLittleEndian);
        }

        /// <summary>整数转为字节数组，注意大小端字节序</summary>
        /// <param name="value"></param>
        /// <param name="isLittleEndian"></param>
        /// <returns></returns>
        public static Byte[] GetBytes(this Int32 value, Boolean isLittleEndian = true)
        {
            var buf = new Byte[4];
            return buf.Write((UInt32)value, 0, isLittleEndian);
        }

        /// <summary>整数转为字节数组，注意大小端字节序</summary>
        /// <param name="value"></param>
        /// <param name="isLittleEndian"></param>
        /// <returns></returns>
        public static Byte[] GetBytes(this UInt64 value, Boolean isLittleEndian = true)
        {
            var buf = new Byte[8];
            return buf.Write(value, 0, isLittleEndian);
        }

        /// <summary>整数转为字节数组，注意大小端字节序</summary>
        /// <param name="value"></param>
        /// <param name="isLittleEndian"></param>
        /// <returns></returns>
        public static Byte[] GetBytes(this Int64 value, Boolean isLittleEndian = true)
        {
            var buf = new Byte[8];
            return buf.Write((UInt64)value, 0, isLittleEndian);
        }
        #endregion

        #region 7位压缩编码整数
        /// <summary>以压缩格式读取32位整数</summary>
        /// <param name="stream">数据流</param>
        /// <returns></returns>
        public static Int32 ReadEncodedInt(this Stream stream)
        {
            Byte b;
            Int32 rs = 0;
            Byte n = 0;
            while (true)
            {
                var bt = stream.ReadByte();
                if (bt < 0) throw new Exception("数据流超出范围！");
                b = (Byte)bt;

                // 必须转为Int32，否则可能溢出
                rs += (Int32)((b & 0x7f) << n);
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 32) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        /// <summary>尝试读取压缩编码整数</summary>
        /// <param name="stream"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Boolean TryReadEncodedInt(this Stream stream, out Int32 value)
        {
            Byte b;
            value = 0;
            Byte n = 0;
            while (true)
            {
                var bt = stream.ReadByte();
                if (bt < 0) return false;
                b = (Byte)bt;

                // 必须转为Int32，否则可能溢出
                value += (Int32)((b & 0x7f) << n);
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 32) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return true;
        }

        /// <summary>
        /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="stream">数据流</param>
        /// <param name="value">数值</param>
        /// <returns>实际写入字节数</returns>
        public static Stream WriteEncodedInt(this Stream stream, Int32 value)
        {
            var list = new List<Byte>();

            Int32 count = 1;
            UInt32 num = (UInt32)value;
            while (num >= 0x80)
            {
                list.Add((byte)(num | 0x80));
                num = num >> 7;

                count++;
            }
            list.Add((byte)num);

            stream.Write(list.ToArray(), 0, list.Count);

            return stream;
        }
        #endregion

        #region 数据流查找
        /// <summary>在数据流中查找字节数组的位置，流指针会移动到结尾</summary>
        /// <param name="stream">数据流</param>
        /// <param name="buffer">字节数组</param>
        /// <param name="offset">字节数组中的偏移</param>
        /// <param name="length">字节数组中的查找长度</param>
        /// <returns></returns>
        public static Int64 IndexOf(this Stream stream, Byte[] buffer, Int64 offset = 0, Int64 length = 0)
        {
            if (length <= 0) length = buffer.Length - offset;

            // 位置
            Int64 p = -1;

            for (Int64 i = 0; i < length; )
            {
                Int32 c = stream.ReadByte();
                if (c == -1) return -1;

                p++;
                if (c == buffer[offset + i])
                {
                    i++;

                    // 全部匹配，退出
                    if (i >= length) return p - length + 1;
                }
                else
                {
                    //i = 0; // 只要有一个不匹配，马上清零
                    // 不能直接清零，那样会导致数据丢失，需要逐位探测，窗口一个个字节滑动
                    // 上一次匹配的其实就是j=0那个，所以这里从j=1开始
                    Int64 n = i;
                    i = 0;
                    for (int j = 1; j < n; j++)
                    {
                        // 在字节数组前(j,n)里面找自己(0,n-j)
                        if (CompareTo(buffer, j, n, buffer, 0, n - j) == 0)
                        {
                            // 前面(0,n-j)相等，窗口退回到这里
                            i = n - j;
                            break;
                        }
                    }
                }
            }

            return -1;
        }

        /// <summary>在字节数组中查找另一个字节数组的位置，不存在则返回-1</summary>
        /// <param name="source">字节数组</param>
        /// <param name="buffer">另一个字节数组</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">查找长度</param>
        /// <returns></returns>
        public static Int64 IndexOf(this Byte[] source, Byte[] buffer, Int64 offset = 0, Int64 length = 0) { return IndexOf(source, 0, 0, buffer, offset, length); }

        /// <summary>在字节数组中查找另一个字节数组的位置，不存在则返回-1</summary>
        /// <param name="source">字节数组</param>
        /// <param name="start">源数组起始位置</param>
        /// <param name="count">查找长度</param>
        /// <param name="buffer">另一个字节数组</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">查找长度</param>
        /// <returns></returns>
        public static Int64 IndexOf(this Byte[] source, Int64 start, Int64 count, Byte[] buffer, Int64 offset = 0, Int64 length = 0)
        {
            if (start < 0) start = 0;
            if (count <= 0 || count > source.Length - start) count = source.Length;
            if (length <= 0 || length > buffer.Length - offset) length = buffer.Length - offset;

            // 已匹配字节数
            Int64 win = 0;
            for (Int64 i = start; i + length - win <= count; i++)
            {
                if (source[i] == buffer[offset + win])
                {
                    win++;

                    // 全部匹配，退出
                    if (win >= length) return i - length + 1 - start;
                }
                else
                {
                    //win = 0; // 只要有一个不匹配，马上清零
                    // 不能直接清零，那样会导致数据丢失，需要逐位探测，窗口一个个字节滑动
                    i = i - win;
                    win = 0;
                }
            }

            return -1;
        }

        /// <summary>比较两个字节数组大小。相等返回0，不等则返回不等的位置，如果位置为0，则返回1。</summary>
        /// <param name="source"></param>
        /// <param name="buffer">缓冲区</param>
        /// <returns></returns>
        public static Int32 CompareTo(this Byte[] source, Byte[] buffer) { return CompareTo(source, 0, 0, buffer, 0, 0); }

        /// <summary>比较两个字节数组大小。相等返回0，不等则返回不等的位置，如果位置为0，则返回1。</summary>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="count">数量</param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Int32 CompareTo(this Byte[] source, Int64 start, Int64 count, Byte[] buffer, Int64 offset = 0, Int64 length = 0)
        {
            if (source == buffer) return 0;

            if (start < 0) start = 0;
            if (count <= 0 || count > source.Length - start) count = source.Length - start;
            if (length <= 0 || length > buffer.Length - offset) length = buffer.Length - offset;

            // 逐字节比较
            for (int i = 0; i < count && i < length; i++)
            {
                Int32 rs = source[start + i].CompareTo(buffer[offset + i]);
                if (rs != 0) return i > 0 ? i : 1;
            }

            // 比较完成。如果长度不相等，则较长者较大
            if (count != length) return count > length ? 1 : -1;

            return 0;
        }

        /// <summary>字节数组分割</summary>
        /// <param name="buf"></param>
        /// <param name="sps"></param>
        /// <returns></returns>
        public static IEnumerable<Byte[]> Split(this Byte[] buf, Byte[] sps)
        {
            var p = 0;
            var idx = 0;
            while (true)
            {
                p = (Int32)buf.IndexOf(idx, 0, sps);
                if (p < 0) break;

                yield return buf.ReadBytes(idx, p);

                idx += p + sps.Length;

            }
            if (idx < buf.Length)
            {
                p = buf.Length - idx;
                yield return buf.ReadBytes(idx, p);
            }
        }

        /// <summary>一个数据流是否以另一个数组开头。如果成功，指针移到目标之后，否则保持指针位置不变。</summary>
        /// <param name="source"></param>
        /// <param name="buffer">缓冲区</param>
        /// <returns></returns>
        public static Boolean StartsWith(this Stream source, Byte[] buffer)
        {
            var p = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                var b = source.ReadByte();
                if (b == -1) { source.Seek(-p, SeekOrigin.Current); return false; }
                p++;

                if (b != buffer[i]) { source.Seek(-p, SeekOrigin.Current); return false; }
            }
            return true;
        }

        /// <summary>一个数据流是否以另一个数组结尾。如果成功，指针移到目标之后，否则保持指针位置不变。</summary>
        /// <param name="source"></param>
        /// <param name="buffer">缓冲区</param>
        /// <returns></returns>
        public static Boolean EndsWith(this Stream source, Byte[] buffer)
        {
            if (source.Length < buffer.Length) return false;

            var p = source.Length - buffer.Length;
            source.Seek(p, SeekOrigin.Current);
            if (source.StartsWith(buffer)) return true;

            source.Seek(-p, SeekOrigin.Current);
            return false;
        }

        /// <summary>一个数组是否以另一个数组开头</summary>
        /// <param name="source"></param>
        /// <param name="buffer">缓冲区</param>
        /// <returns></returns>
        public static Boolean StartsWith(this Byte[] source, Byte[] buffer)
        {
            if (source.Length < buffer.Length) return false;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (source[i] != buffer[i]) return false;
            }
            return true;
        }

        /// <summary>一个数组是否以另一个数组结尾</summary>
        /// <param name="source"></param>
        /// <param name="buffer">缓冲区</param>
        /// <returns></returns>
        public static Boolean EndsWith(this Byte[] source, Byte[] buffer)
        {
            if (source.Length < buffer.Length) return false;

            var p = source.Length - buffer.Length;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (source[p + i] != buffer[i]) return false;
            }
            return true;
        }
        #endregion

        #region 倒序、更换字节序
        /// <summary>倒序、更换字节序</summary>
        /// <param name="buf">字节数组</param>
        /// <returns></returns>
        public static unsafe Byte[] Reverse(this Byte[] buf)
        {
            if (buf == null || buf.Length < 2) return buf;

            if (buf.Length > 100)
            {
                Array.Reverse(buf);
                return buf;
            }

            // 小数组使用指针更快
            fixed (Byte* p = buf)
            {
                Byte* pStart = p;
                Byte* pEnd = p + buf.Length - 1;
                for (var i = buf.Length / 2; i > 0; i--)
                {
                    var temp = *pStart;
                    *pStart++ = *pEnd;
                    *pEnd-- = temp;
                }
            }
            return buf;
        }
        #endregion

        #region 十六进制编码
        /// <summary>把字节数组编码为十六进制字符串</summary>
        /// <param name="data">字节数组</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量。超过实际数量时，使用实际数量</param>
        /// <returns></returns>
        public static String ToHex(this Byte[] data, Int32 offset = 0, Int32 count = 0)
        {
            if (data == null || data.Length < 1) return null;
            if (count <= 0)
                count = data.Length - offset;
            else if (offset + count > data.Length)
                count = data.Length - offset;

            //return BitConverter.ToString(data).Replace("-", null);
            // 上面的方法要替换-，效率太低
            var cs = new Char[count * 2];
            // 两个索引一起用，避免乘除带来的性能损耗
            for (int i = 0, j = 0; i < count; i++, j += 2)
            {
                Byte b = data[offset + i];
                cs[j] = GetHexValue(b / 0x10);
                cs[j + 1] = GetHexValue(b % 0x10);
            }
            return new String(cs);
        }

        /// <summary>把字节数组编码为十六进制字符串，带有分隔符和分组功能</summary>
        /// <param name="data">字节数组</param>
        /// <param name="separate">分隔符</param>
        /// <param name="groupSize">分组大小，为0时对每个字节应用分隔符，否则对每个分组使用</param>
        /// <returns></returns>
        public static String ToHex(this Byte[] data, String separate, Int32 groupSize = 0)
        {
            if (data == null || data.Length < 1) return null;
            if (groupSize < 0) groupSize = 0;

            if (groupSize == 0)
            {
                // 没有分隔符
                if (String.IsNullOrEmpty(separate)) return data.ToHex();

                // 特殊处理
                if (separate == "-") return BitConverter.ToString(data);
            }

            var count = data.Length;
            var len = count * 2;
            if (!String.IsNullOrEmpty(separate)) len += (count - 1) * separate.Length;
            if (groupSize > 0)
            {
                // 计算分组个数
                var g = (count - 1) / groupSize;
                len += g * 2;
                // 扣除间隔
                if (!String.IsNullOrEmpty(separate)) len -= g * separate.Length;
            }
            var sb = new StringBuilder(len);
            for (int i = 0; i < count; i++)
            {
                if (sb.Length > 0)
                {
                    if (i % groupSize == 0)
                        sb.AppendLine();
                    else
                        sb.Append(separate);
                }

                Byte b = data[i];
                sb.Append(GetHexValue(b / 0x10));
                sb.Append(GetHexValue(b % 0x10));
            }

            return sb.ToString();
        }

        private static char GetHexValue(int i)
        {
            if (i < 10) return (char)(i + 0x30);
            return (char)(i - 10 + 0x41);
        }

        /// <summary>把十六进制字符串解码字节数组</summary>
        /// <param name="data">字节数组</param>
        /// <param name="startIndex">起始位置</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        [Obsolete("ToHex")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Byte[] FromHex(this String data, Int32 startIndex = 0, Int32 length = 0) { return ToHex(data, startIndex, length); }

        /// <summary>解密</summary>
        /// <param name="data">Hex编码的字符串</param>
        /// <param name="startIndex">起始位置</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        public static Byte[] ToHex(this String data, Int32 startIndex = 0, Int32 length = 0)
        {
            if (String.IsNullOrEmpty(data)) return null;

            // 过滤特殊字符
            data = data.Trim()
                .Replace("-", null)
                .Replace("0x", null)
                .Replace("0X", null)
                .Replace(" ", null)
                .Replace("\r", null)
                .Replace("\n", null)
                .Replace(",", null);

            if (length <= 0) length = data.Length - startIndex;

            var bts = new Byte[length / 2];
            for (int i = 0; i < bts.Length; i++)
            {
                bts[i] = Byte.Parse(data.Substring(startIndex + 2 * i, 2), NumberStyles.HexNumber);
            }
            return bts;
        }
        #endregion

        #region BASE64编码
        /// <summary>字节数组转为Base64编码</summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="lineBreak">是否换行显示</param>
        /// <returns></returns>
        public static String ToBase64(this Byte[] data, Int32 offset = 0, Int32 count = 0, Boolean lineBreak = false)
        {
            if (data == null || data.Length < 1) return null;
            if (count <= 0)
                count = data.Length - offset;
            else if (offset + count > data.Length)
                count = data.Length - offset;

            return Convert.ToBase64String(data, offset, count, lineBreak ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None);
        }

        /// <summary>Base64字符串转为字节数组</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] ToBase64(this String data)
        {
            if (String.IsNullOrEmpty(data)) return null;

            //// 过滤特殊字符
            //data = data.Trim()
            //    .Replace("\r", null)
            //    .Replace("\n", null);

            return Convert.FromBase64String(data);
        }
        #endregion
    }
}