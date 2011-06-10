using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using BinaryWriterX2 = NewLife.Serialization.BinaryWriterX;
using BinaryReaderX2 = NewLife.Serialization.BinaryReaderX;

namespace NewLife.IO
{
    /// <summary>
    /// IO工具类
    /// </summary>
    public static class IOHelper
    {
        #region 压缩/解压缩 数据
        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流</param>
        public static void Compress(Stream inStream, Stream outStream)
        {
            Stream stream = new DeflateStream(outStream, CompressionMode.Compress, true);
            CopyTo(inStream, stream);
            stream.Flush();
            stream.Close();
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="inStream">输入流</param>
        /// <param name="outStream">输出流</param>
        public static void Decompress(Stream inStream, Stream outStream)
        {
            Stream stream = new DeflateStream(inStream, CompressionMode.Decompress, true);
            CopyTo(stream, outStream, 0);
            stream.Close();
        }

        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] Compress(Byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            Compress(new MemoryStream(data), ms);
            return ms.ToArray();

            //MemoryStream ms = new MemoryStream();
            //Stream stream = new DeflateStream(ms, CompressionMode.Compress, true);
            //stream.Write(data, 0, data.Length);
            //stream.Flush();
            //stream.Close();

            //data = ms.ToArray();
            //ms.Close();

            //return data;
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] Decompress(Byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            Decompress(new MemoryStream(data), ms);
            return ms.ToArray();

            //MemoryStream ms = new MemoryStream(data);
            //Stream stream = new DeflateStream(ms, CompressionMode.Decompress, true);

            //MemoryStream ms2 = new MemoryStream();
            //CopyTo(stream, ms2, 0);

            //data = ms2.ToArray();

            //stream.Close();
            //ms.Close();
            //ms2.Close();

            //return data;
        }
        #endregion

        #region 压缩/解压缩 文件
        /*
         * 我们希望单文件压缩得到的压缩包，用其它压缩工具可以解压缩。
         * 至于多文件压缩包，因为Zip格式实在不简单，无法做到轻量级，只好实现一个自定义的版本。
         * 于是就有了问题，解压缩的时候，需要自己判断是单文件还是多文件。
         */

        /// <summary>
        /// 压缩单个文件，纯文件流压缩
        /// </summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        public static void CompressFile(String src, String des)
        {
            if (String.IsNullOrEmpty(des)) des = src + ".gz";

            using (FileStream outStream = new FileStream(des, FileMode.Create, FileAccess.Write))
            using (Stream stream = new GZipStream(outStream, CompressionMode.Compress, true))
            using (FileStream inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                CopyTo(inStream, stream);
            }
        }

        /// <summary>
        /// 压缩目录
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static String CompressFile(String root)
        {
            return CompressFile(root, Directory.GetFiles(root, "*.*", SearchOption.AllDirectories));
        }

        /// <summary>
        /// 压缩多个文件
        /// </summary>
        /// <param name="root">根目录</param>
        /// <param name="files">文件集合</param>
        /// <returns>压缩的文件名</returns>
        public static String CompressFile(String root, String[] files)
        {
            String des = GetDesFile(root, files);
            CompressFile(root, files, des);
            return des;
        }

        static String GetDesFile(String root, String[] files)
        {
            String des = null;
            if (files != null && files.Length == 1)
                des = files[0];
            else if (!String.IsNullOrEmpty(root))
                des = new DirectoryInfo(root).Name;

            String f = des + ".gz";
            for (int i = 0; i < 100 && File.Exists(f); i++)
            {
                f = des + (i + 1) + ".gz";
            }
            // 占位
            File.Create(f).Close();
            return f;
        }

        /// <summary>
        /// 压缩多个文件
        /// </summary>
        /// <param name="root">根目录</param>
        /// <param name="files">文件集合</param>
        /// <param name="des">输出文件</param>
        public static void CompressFile(String root, String[] files, String des)
        {
            if (String.IsNullOrEmpty(root)) root = AppDomain.CurrentDomain.BaseDirectory;

            if (String.IsNullOrEmpty(des)) des = GetDesFile(root, files);
            if (!String.IsNullOrEmpty(root) && !root.StartsWith(@"\")) root += @"\";

            if (File.Exists(des)) File.Delete(des);
            using (FileStream outStream = new FileStream(des, FileMode.Create, FileAccess.Write))
            {
                CompressFile(root, files, outStream);
            }
        }

        /// <summary>
        /// 压缩多个文件，每个文件流之前都写入相对文件路径（包括相对于根目录）和文件长度等头部信息
        /// </summary>
        /// <param name="root">根目录</param>
        /// <param name="files">文件集合</param>
        /// <param name="outStream">目标</param>
        public static void CompressFile(String root, String[] files, Stream outStream)
        {
            if (String.IsNullOrEmpty(root)) root = AppDomain.CurrentDomain.BaseDirectory;

            using (Stream stream = new GZipStream(outStream, CompressionMode.Compress, true))
            {
                if (files.Length > 1)
                {
                    BinaryWriter writer = new BinaryWriter(stream);
                    //writer.Stream = stream;
                    // 幻数
                    writer.Write("XGZip".ToCharArray());
                    // 文件个数
                    writer.Write(files.Length);

                    // 写头部
                    foreach (String item in files)
                    {
                        String file = item;
                        if (!String.IsNullOrEmpty(root) && file.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                            file = file.Substring(root.Length);
                        else
                            file = Path.GetFileName(file);

                        writer.Write(file);

                        FileInfo fi = new FileInfo(item);
                        //writer.Write(fi.LastAccessTime);
                        writer.Write((Int32)fi.Length);
                    }
                }

                foreach (String item in files)
                {
                    using (FileStream inStream = new FileStream(item, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        CopyTo(inStream, stream);
                    }
                }
            }
        }

        /// <summary>
        /// 单个文件解压缩，纯文件流解压缩
        /// </summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        public static void DecompressFile(String src, String des)
        {
            if (String.IsNullOrEmpty(des)) des = Path.GetFileNameWithoutExtension(src);

            //using (FileStream inStream = new FileStream(src, FileMode.Create, FileAccess.Write))
            //using (Stream stream = new GZipStream(inStream, CompressionMode.Decompress, true))
            //using (FileStream outStream = new FileStream(des, FileMode.Open, FileAccess.Read, FileShare.Read))
            //{
            //    CopyTo(stream, outStream);
            //}
            using (FileStream inStream = new FileStream(src, FileMode.Create, FileAccess.Write))
            {
                DecompressFile(inStream, null, des);
            }
        }

        /// <summary>
        /// 解压缩多个文件
        /// </summary>
        /// <param name="inStream"></param>
        /// <param name="targetPath"></param>
        public static void DecompressFile(Stream inStream, String targetPath)
        {
            DecompressFile(inStream, targetPath, null);
        }

        static void DecompressFile(Stream inStream, String targetPath, String des)
        {
            using (Stream stream = new GZipStream(inStream, CompressionMode.Decompress, true))
            {
                BinaryReader reader = new BinaryReader(stream);
                Int64 pos = stream.Position;

                // 读幻数
                Byte[] bts = reader.ReadBytes(5);
                if (bts == Encoding.ASCII.GetBytes("XGZip"))
                {
                    // 文件个数
                    Int32 count = reader.ReadInt32();

                    String[] files = new String[count];
                    Int32[] sizes = new Int32[count];

                    // 读头部
                    for (int i = 0; i < count; i++)
                    {
                        files[i] = reader.ReadString();
                        sizes[i] = reader.ReadInt32();
                    }

                    if (String.IsNullOrEmpty(targetPath) && !String.IsNullOrEmpty(des)) targetPath = Path.GetDirectoryName(des);

                    for (int i = 0; i < count; i++)
                    {
                        String item = files[i];
                        if (!String.IsNullOrEmpty(targetPath)) item = Path.Combine(targetPath, item);
                        if (File.Exists(item)) File.Delete(item);
                        using (FileStream outStream = new FileStream(item, FileMode.Create, FileAccess.Write))
                        {
                            CopyTo(stream, outStream, 0, sizes[i]);
                        }
                    }
                }
                else
                {
                    // 特殊处理，要把那个bts当作数据写入到输出流里面去
                    using (FileStream outStream = new FileStream(des, FileMode.Create, FileAccess.Write))
                    {
                        outStream.Write(bts, 0, bts.Length);
                        CopyTo(stream, outStream, 0, 0);
                    }
                }
            }
        }
        #endregion

        #region 复制数据流
        /// <summary>
        /// 复制数据流
        /// </summary>
        /// <param name="src">源数据流</param>
        /// <param name="des">目的数据流</param>
        /// <returns>返回复制的总字节数</returns>
        public static Int32 CopyTo(Stream src, Stream des)
        {
            return CopyTo(src, des, 0, 0);
        }

        /// <summary>
        /// 复制数据流
        /// </summary>
        /// <param name="src">源数据流</param>
        /// <param name="des">目的数据流</param>
        /// <param name="bufferSize">缓冲区大小，也就是每次复制的大小</param>
        /// <returns>返回复制的总字节数</returns>
        public static Int32 CopyTo(Stream src, Stream des, Int32 bufferSize)
        {
            return CopyTo(src, des, bufferSize, 0);
        }

        /// <summary>
        /// 复制数据流
        /// </summary>
        /// <param name="src">源数据流</param>
        /// <param name="des">目的数据流</param>
        /// <param name="bufferSize">缓冲区大小，也就是每次复制的大小</param>
        /// <param name="max">最大复制字节数</param>
        /// <returns>返回复制的总字节数</returns>
        public static Int32 CopyTo(Stream src, Stream des, Int32 bufferSize, Int32 max)
        {
            if (bufferSize <= 0) bufferSize = 1024;

            Int32 total = 0;
            while (true)
            {
                if (max > 0)
                {
                    if (total >= max) break;

                    // 最后一次读取大小不同
                    if (total + bufferSize > max) bufferSize = max - total;
                }

                Byte[] buffer = new Byte[bufferSize];
                Int32 count = src.Read(buffer, 0, buffer.Length);
                if (count <= 0) break;
                total += count;

                des.Write(buffer, 0, count);
            }

            return total;
        }
        #endregion
    }
}